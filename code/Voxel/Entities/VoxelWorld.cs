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

public struct VoxelData
{
	public Voxel? Voxel;
	public Chunk Chunk;
	public Vector3I Position;
}

public class ChunkEntity : ModelEntity
{
	public new VoxelWorld Parent { get; set; }

	public override void Spawn()
	{
		Tags.Add( "chunk" );
	}
}

public partial class VoxelWorld : ModelEntity
{
	private Dictionary<Chunk, ModelEntity> entities = new();

	public float VoxelScale { get; set; } = Utility.Scale;

	public Chunk[,,] Chunks { get; private set; }

	public Vector3I Size { get; private set; }
	public Vector3I ChunkSize { get; private set; }

	public VoxelWorld() 
	{
		ChunkSize = new( Chunk.DEFAULT_WIDTH, Chunk.DEFAULT_DEPTH, Chunk.DEFAULT_HEIGHT );
	}

	/// <summary>
	/// Generates a chunk.
	/// </summary>
	/// <param name="chunk"></param>
	/// <param name="withPhysics"></param>
	public void GenerateChunk( Chunk chunk, bool withPhysics = true )
	{
		// Get our chunk's entity.
		if ( chunk == null )
			return;

		ModelEntity chunkEntity;
		if ( !entities.TryGetValue( chunk, out chunkEntity ) )
			entities.Add( chunk, chunkEntity = new ChunkEntity() { Parent = this } );

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
			if ( withPhysics && !tested[x, y, z] )
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

		mesh.CreateVertexBuffer<VoxelVertex>( vertices.Count, VoxelVertex.Layout, vertices.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );

		// Create a model for the mesh.
		builder.AddMesh( mesh );

		// Build collisions.
		if ( withPhysics )
			builder.AddCollisionMesh( buffer.Vertex.ToArray(), buffer.Index.ToArray() );

		chunkEntity.Model = builder.Create();
		chunkEntity.Position = Position
			+ (Vector3)chunk.Position * ChunkSize * VoxelScale
			+ VoxelScale / 2f;

		if ( withPhysics )
			chunkEntity.SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	/// <summary>
	/// Attempts to get VoxelData from an absolute world position.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public VoxelData? GetClosestVoxel( Vector3 position )
	{
		var width = ChunkSize.x;
		var depth = ChunkSize.y;
		var height = ChunkSize.z;
		
		var relative = position - Position;
		var chunkPosition = new Vector3( relative.x / (width * VoxelScale), relative.y / (depth * VoxelScale), relative.z / (height * VoxelScale) );

		if ( chunkPosition.x < 0 || chunkPosition.y < 0 || chunkPosition.z < 0 )
			return null;

		var voxelIndex = (
			x: (short)(chunkPosition.x * width).FloorToInt(), 
			y: (short)(chunkPosition.y * depth).FloorToInt(), 
			z: (short)(chunkPosition.z * height).FloorToInt()
		);
		var chunkIndex = (
			x: voxelIndex.x / width,
			y: voxelIndex.y / depth,
			z: voxelIndex.z / height
		);

		if ( chunkIndex.x < 0 || chunkIndex.y < 0 || chunkIndex.z < 0
		  || chunkIndex.x >= Size.x || chunkIndex.y >= Size.y || chunkIndex.z >= Size.z ) return null;

		var chunk = Chunks[chunkIndex.x, chunkIndex.y, chunkIndex.z];
		var voxel = (
			x: (ushort)(voxelIndex.x - chunkIndex.x * width),
			y: (ushort)(voxelIndex.y - chunkIndex.y * depth),
			z: (ushort)(voxelIndex.z - chunkIndex.z * height)
		);

		return new VoxelData
		{
			Chunk = chunk,
			Voxel = chunk?.GetVoxel( voxel.x, voxel.y, voxel.z ),
			Position = voxel
		};
	}

	/// <summary>
	/// Converts worldspace coordinates to voxel space.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public (int x, int y, int z) GetVoxelSpace( Vector3 position )
	{
		var width = ChunkSize.x;
		var depth = ChunkSize.y;
		var height = ChunkSize.z;

		var relative = position - Position;
		var chunkPosition = new Vector3( relative.x / (width * VoxelScale), relative.y / (depth * VoxelScale), relative.z / (height * VoxelScale) );

		var voxelIndex = (
			x: (chunkPosition.x * width).FloorToInt(),
			y: (chunkPosition.y * depth).FloorToInt(),
			z: (chunkPosition.z * height).FloorToInt()
		);

		return voxelIndex;
	}

	[GameEvent.Client.Frame]
	private static void destruction()
	{
		if ( Game.LocalPawn is not Pawn pawn )
			return;

		var ray = new Ray( pawn.Position, pawn.ViewAngles.Forward );
		var tr = Trace.Ray( ray, 10000f )
			.IncludeClientside()
			.WithTag( "chunk" )
			.Run();

		var parent = (tr.Entity as ChunkEntity)?.Parent;
		var voxelData = parent?.GetClosestVoxel( tr.EndPosition - tr.Normal * parent.VoxelScale / 2f );
		
		if ( voxelData?.Voxel != null )
		{
			var data = voxelData.Value;
			var chunks = (IEnumerable<Chunk>)null;
			var withPhysics = true;

			if ( Input.Pressed( "attack1" ) )
			{
				var size = 8;
				var result = new List<Chunk>();

				for ( int x = 0; x < size; x++ )
				for ( int y = 0; y < size; y++ )
				for ( int z = 0; z < size; z++ )
				{
					var center = (Vector3)data.Position;
					var position = center 
						+ new Vector3( x, y, z ) 
						- size / 2f;

					if ( position.Distance( center ) >= size / 2f )
						continue;

					var res = data.Chunk.TrySetVoxel(
						position.x.FloorToInt(),
						position.y.FloorToInt(),
						position.z.FloorToInt(), null );
					result.AddRange( res.Except( result ) );
				}

				chunks = result;
			}
			else if ( Input.Down( "attack2" ) )
				chunks = data.Chunk.TrySetVoxel( data.Position.x, data.Position.y, data.Position.z, new Voxel( Color32.Black ) );

			if ( chunks != null )
			{
				foreach ( var chunk in chunks )
					parent.GenerateChunk( chunk, withPhysics );
			}
		}
	}

	#region DEBUG
	static bool debugMode = false;
	static VoxelWorld entity;

	[Event.Hotload]
	private static void refresh()
	{
		if ( Game.IsServer )
			return;

		if ( entity == null )
			return;

		foreach ( var chunk in entity.Chunks )
			entity.GenerateChunk( chunk );
	}

	[ConCmd.Client( "testvox" )]
	public static void TestVoxel()
	{
		if ( entity != null )
		{
			foreach ( var child in entity.entities.Values )
				child.Delete();

			entity.Delete();
		}

		entity = new VoxelWorld();

		var chunks = new Chunk[2, 2, 2];
		for ( ushort x = 0; x < chunks.GetLength( 0 ); x++ )
		for ( ushort y = 0; y < chunks.GetLength( 1 ); y++ )
		for ( ushort z = 0; z < chunks.GetLength( 2 ); z++ )
		{
			var chunk = new Chunk( x, y, z, entity.ChunkSize.x, entity.ChunkSize.y, entity.ChunkSize.z, chunks );
			chunks[x, y, z] = chunk;

			for ( ushort i = 0; i < entity.ChunkSize.x; i++ )
			for ( ushort j = 0; j < entity.ChunkSize.y; j++ )
			for ( ushort k = 0; k < entity.ChunkSize.z; k++ )
				chunk.SetVoxel( i, j, k, new Voxel( Color.Random.ToColor32() ) );
		}

		entity.Chunks = chunks;
		entity.Size = new( chunks.GetLength( 0 ), chunks.GetLength( 1 ), chunks.GetLength( 2 ) );

		foreach ( var chunk in chunks )
			entity.GenerateChunk( chunk );
	}

	[ConCmd.Client( "loadvox" )]
	public static async void LoadModel( string path )
	{
		if ( entity != null )
		{
			foreach ( var child in entity.entities.Values )
				child.Delete();

			entity.Delete();
		}

		entity = new VoxelWorld();

		var chunks = await Importer.VoxImporter.Load( path, entity.ChunkSize.x, entity.ChunkSize.y, entity.ChunkSize.z );
		entity.Size = new( chunks.GetLength( 0 ), chunks.GetLength( 1 ), chunks.GetLength( 2 ) );
		entity.Chunks = chunks;

		foreach ( var chunk in entity.Chunks )
			entity.GenerateChunk( chunk );
	}

	[GameEvent.Client.Frame]
	private static void debug()
	{
		if ( Input.Pressed( "score" ) )
			debugMode = !debugMode;

		if ( Game.LocalPawn is not Pawn pawn || !debugMode )
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
		var ray = new Ray( pawn.Position, pawn.ViewAngles.Forward );
		var tr = Trace.Ray( ray, 10000f )
			.IncludeClientside()
			.WithTag( "chunk" )
			.Run();

		var parent = (tr.Entity as ChunkEntity)?.Parent;
		var voxelData = parent?.GetClosestVoxel( tr.EndPosition - tr.Normal * parent.VoxelScale / 2f );

		if ( voxelData?.Voxel != null )
		{
			var data = voxelData.Value;

			// Debug
			DebugOverlay.ScreenText( $"{voxelData?.Voxel?.Color ?? default}" );
			DebugOverlay.ScreenText( $"XYZ: {voxelData?.Position}", 1 );
			DebugOverlay.ScreenText( $"Chunk: {voxelData?.Chunk.Position}", 2 );

			var voxelCenter = (Vector3)data.Chunk.Position * parent.ChunkSize * parent.VoxelScale
				+ (Vector3)data.Position * parent.VoxelScale
				+ parent.VoxelScale / 2f
				+ parent.Position;

			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.LineThickness = 1;
			Gizmo.Draw.LineBBox( new BBox( voxelCenter, parent.VoxelScale ) );
		}
	}
	#endregion
}
