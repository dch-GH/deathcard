namespace DeathCard;

/*
if ( voxel != null )
{
	var col = voxel.Value.Color.ToColor();
	var pos = parent.Position 
		+ position * parent.VoxelScale 
		+ (Vector3)data.Chunk.Position * parent.ChunkSize * parent.VoxelScale
		+ parent.VoxelScale / 2f;
	var particle = Particles.Create( "particles/voxel_break.vpcf", pos );
	particle.SetPosition( 2, col );
}
*/
public class ChunkEntity : ModelEntity
{
	public new VoxelWorld Parent { get; set; }

	public ChunkEntity()
	{
		Tags.Add( "chunk" );
		Transmit = TransmitType.Never;
	}
}

public partial class VoxelWorld : ModelEntity
{
	private Dictionary<Vector3I, ChunkEntity> entities = new();

	public float VoxelScale { get; set; } = Utility.Scale;
	public Chunk[,,] Chunks { get; private set; }

	[Net] public Vector3I Size { get; private set; }
	[Net] public Vector3I ChunkSize { get; private set; }

	public VoxelWorld() 
	{
		ChunkSize = new( Chunk.DEFAULT_WIDTH, Chunk.DEFAULT_DEPTH, Chunk.DEFAULT_HEIGHT );
		Transmit = TransmitType.Always;
	}

	protected override void OnDestroy()
	{
		foreach ( var (_, child) in entities )
			child.Delete();

		Chunks = null;
	}

	/// <summary>
	/// Generates a chunk.
	/// </summary>
	/// <param name="chunk"></param>
	public void GenerateChunk( Chunk chunk )
	{
		// Get our chunk's entity.
		if ( chunk == null )
			return;

		ChunkEntity chunkEntity;
		if ( !entities.TryGetValue( chunk.Position, out chunkEntity ) )
			entities.Add( chunk.Position, chunkEntity = new ChunkEntity() { Parent = this } );

		// Let's create a mesh.
		var builder = Model.Builder;
		var material = Material.FromShader( "shaders/voxel.shader" );
		var mesh = new Mesh( material );
		var vertices = new List<VoxelVertex>();
		var indices = new List<int>();
		var offset = 0;

		var tested = new bool[ChunkSize.x, ChunkSize.y, ChunkSize.z];
		var buffer = new CollisionBuffer();
		buffer.Init( true );

		for ( ushort x = 0; x < ChunkSize.x; x++ )
		for ( ushort y = 0; y < ChunkSize.y; y++ )
		for ( ushort z = 0; z < ChunkSize.z; z++ )
		{
			var voxel = chunk.GetVoxel( x, y, z );
			if ( voxel == null )
				continue;

			var position = new Vector3I( x, y, z );

			// Let's start checking for collisions.
			if ( !tested[x, y, z] )
			{
				tested[x, y, z] = true;

				var start = (x: x, y: y, z: z);
				var size = (x: 1, y: 1, z: 1);
				var canSpread = (x: true, y: true, z: true);

				// Calculate how much we can fill.
				while ( canSpread.x || canSpread.y || canSpread.z )
				{
					canSpread.x = trySpreadX( chunk, canSpread.x, ref tested, start, ref size );
					canSpread.y = trySpreadY( chunk, canSpread.y, ref tested, start, ref size );
					canSpread.z = trySpreadZ( chunk, canSpread.z, ref tested, start, ref size );
				}

				var scale = new Vector3( size.x, size.y, size.z ) * VoxelScale;
				var pos = new Vector3( start.x, start.y, start.z ) * VoxelScale
					+ scale / 2f
					- VoxelScale / 2f;

				buffer.AddCube( pos, scale, Rotation.Identity );
			}

			// Let's skip generating mesh for server, we only need collisions.
			if ( Game.IsServer )
				continue;

			// Generate all visible faces for our voxel.
			var drawCount = 0;
			for ( var i = 0; i < Utility.Faces; i++ )
			{
				var direction = Utility.Neighbors[i];
				var neighbour = chunk.GetDataByOffset( x + direction.x, y + direction.y, z + direction.z ).Voxel;
				if ( neighbour != null )
					continue;

				var faceColor = Utility.FaceMultiply[i];
				for ( var j = 0; j < 4; ++j )
				{
					var vertexIndex = Utility.FaceIndices[(i * 4) + j];
					var pos = Utility.Positions[vertexIndex] * VoxelScale
						+ new Vector3( x, y, z ) * VoxelScale;

					var ao = Utility.BuildAO( chunk, position, i, j );
					var color = voxel.Value.Color
						.Multiply( ao * faceColor );
					vertices.Add( new VoxelVertex( pos, color ) );
				}

				indices.Add( offset + drawCount * 4 + 0 );
				indices.Add( offset + drawCount * 4 + 2 );
				indices.Add( offset + drawCount * 4 + 1 );
				indices.Add( offset + drawCount * 4 + 2 );
				indices.Add( offset + drawCount * 4 + 0 );
				indices.Add( offset + drawCount * 4 + 3 );

				drawCount++;
			}

			offset += 4 * drawCount;
		}

		chunkEntity.Position = Position
			+ (Vector3)chunk.Position * ChunkSize * VoxelScale
			+ VoxelScale / 2f;

		// Check if we actually end up with vertices.
		if ( Game.IsClient && vertices.Count > 0 )
		{
			mesh.CreateVertexBuffer<VoxelVertex>( vertices.Count, VoxelVertex.Layout, vertices.ToArray() );
			mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );

			builder.AddMesh( mesh );
		}

		// Create a model for the mesh.
		builder.AddCollisionMesh( buffer.Vertex.ToArray(), buffer.Index.ToArray() );

		chunkEntity.Model = builder.Create();
		chunkEntity.SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	#region DEBUG
	static bool debugMode = false;

	[GameEvent.Client.Frame]
	private static void debug()
	{
		if ( Input.Pressed( "score" ) )
			debugMode = !debugMode;

		if ( !debugMode )
			return;

		// Display all chunks.
		foreach ( var world in Entity.All.OfType<VoxelWorld>() )
		{
			if ( world?.Chunks == null )
				continue;

			foreach ( var chunk in world.Chunks )
			{
				if ( chunk == null )
					continue;

				var pos = world.Position
					+ (Vector3)chunk.Position * world.VoxelScale * world.ChunkSize;

				Gizmo.Draw.Color = Color.Yellow;
				Gizmo.Draw.LineThickness = 1;
				Gizmo.Draw.LineBBox( new BBox( pos, pos + (Vector3)world.ChunkSize * world.VoxelScale ) );
			}
		}

		// Focus on hovered VoxelWorld.
		var ray = new Ray( Camera.Position, Camera.Rotation.Forward );
		var tr = Trace.Ray( ray, 10000f )
			.IncludeClientside()
			.WithTag( "chunk" )
			.Run();

		DebugOverlay.TraceResult( tr );

		var parent = (tr.Entity as ChunkEntity)?.Parent;
		if ( parent == null )
			return;

		var position = parent.WorldToVoxel( tr.EndPosition - tr.Normal * parent.VoxelScale / 2f );
		var data = parent.GetByOffset( position.x, position.y, position.z );

		// Debug
		DebugOverlay.ScreenText( $"{data.Voxel?.Color ?? default}" );
		DebugOverlay.ScreenText( $"XYZ: {position}", 1 );
		DebugOverlay.ScreenText( $"Chunk: {data.Chunk.Position}", 2 );

		var voxelCenter = (Vector3)position * parent.VoxelScale
			+ parent.VoxelScale / 2f
			+ parent.Position;

		Gizmo.Draw.Color = Color.Black;
		Gizmo.Draw.LineThickness = 1;
		Gizmo.Draw.LineBBox( new BBox( voxelCenter, parent.VoxelScale ) );
	}
	#endregion
}
