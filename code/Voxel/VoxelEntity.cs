using DeathCard.Importer;

namespace DeathCard;

public struct VoxelData
{
	public Voxel? Voxel;
	public Chunk Chunk;

	public ushort x;
	public ushort y;
	public ushort z;
}

public class ChunkEntity : ModelEntity
{
	public new VoxelEntity Parent { get; set; }
}

public partial class VoxelEntity : ModelEntity
{
	public Dictionary<Chunk, ModelEntity> Entities = new();

	public float VoxelScale { get; set; } = 1f / 0.0254f;

	public Chunk[,,] Chunks { get; private set; }

	public Vector3I Size { get; private set; }
	public Vector3I ChunkSize { get; private set; }

	public VoxelEntity() { }

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
		if ( !Entities.TryGetValue( chunk, out chunkEntity ) )
			Entities.Add( chunk, chunkEntity = new ChunkEntity() { Parent = this } );

		var positions = new Vector3[]
		{
			new Vector3( -0.5f, -0.5f, 0.5f ) * VoxelScale,
			new Vector3( -0.5f, 0.5f, 0.5f ) * VoxelScale,
			new Vector3( 0.5f, 0.5f, 0.5f ) * VoxelScale,
			new Vector3( 0.5f, -0.5f, 0.5f ) * VoxelScale,
			new Vector3( -0.5f, -0.5f, -0.5f ) * VoxelScale,
			new Vector3( -0.5f, 0.5f, -0.5f ) * VoxelScale,
			new Vector3( 0.5f, 0.5f, -0.5f ) * VoxelScale,
			new Vector3( 0.5f, -0.5f, -0.5f ) * VoxelScale,
		};

		var faceIndices = new int[]
		{
			0, 1, 2, 3,
			7, 6, 5, 4,
			0, 4, 5, 1,
			1, 5, 6, 2,
			2, 6, 7, 3,
			3, 7, 4, 0,
		};

		var normals = new Vector3[]
		{
			Vector3.Up,
			Vector3.Down,
			Vector3.Backward,
			Vector3.Left,
			Vector3.Forward,
			Vector3.Right,
		};

		var neighbors = new (short x, short y, short z)[]
		{
			(0, 0, 1),
			(0, 0, -1),
			(-1, 0, 0),
			(0, 1, 0),
			(1, 0, 0),
			(0, -1, 0),
		};

		// Let's create a mesh.
		const int faces = 6;
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

			// Generate all visible faces for our voxel.
			var drawCount = 0;
			for ( var i = 0; i < faces; i++ )
			{
				var direction = neighbors[i];
				var neighbour = chunk.GetVoxelByOffset( x + direction.x, y + direction.y, z + direction.z );
				if ( neighbour != null )
					continue;

				var normal = normals[i];
				for ( var j = 0; j < 4; ++j )
				{
					var vertexIndex = faceIndices[(i * 4) + j];
					var pos = positions[vertexIndex]
						+ new Vector3( x, y, z ) * VoxelScale;

					var col = voxel.Value.Color;
					vertices.Add( new VoxelVertex( pos, normal, col ) );
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
			x = voxel.x,
			y = voxel.y,
			z = voxel.z
		};
	}

	[GameEvent.Client.Frame]
	private static void tick()
	{
		if ( Game.LocalPawn is not Pawn pawn )
			return;

		var ray = new Ray( pawn.Position, pawn.ViewAngles.Forward );
		var tr = Trace.Ray( ray, 10000f )
			.StaticOnly()	
			.IncludeClientside()
			.Run();

		DebugOverlay.TraceResult( tr );

		var parent = (tr.Entity as ChunkEntity)?.Parent;
		var voxelData = parent?.GetClosestVoxel( tr.EndPosition - tr.Normal * parent.VoxelScale / 2f );
		
		if ( voxelData?.Voxel != null )
		{
			var data = voxelData.Value;
			var chunks = (IEnumerable<Chunk>)null;
			var withPhysics = true;

			if ( Input.Down( "attack1" ) )
				chunks = data.Chunk.TrySetVoxel( data.x, data.y, data.z, null );
			else if ( Input.Down( "attack2" ) )
				chunks = data.Chunk.TrySetVoxel( data.x, data.y, data.z, new Voxel( Color32.Black ) );

			if ( chunks != null )
			{
				foreach ( var chunk in chunks )
					parent.GenerateChunk( chunk, withPhysics );
			}
		}
	}

	static VoxelEntity entity;

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
			foreach ( var child in entity.Entities.Values )
				child.Delete();

			entity.Delete();
		}

		entity = new VoxelEntity();
		entity.ChunkSize = new( Chunk.DEFAULT_WIDTH, Chunk.DEFAULT_DEPTH, Chunk.DEFAULT_HEIGHT );

		var chunks = new Chunk[2, 2, 2];
		for ( ushort x = 0; x < chunks.GetLength( 0 ); x++ )
		for ( ushort y = 0; y < chunks.GetLength( 1 ); y++ )
		for ( ushort z = 0; z < chunks.GetLength( 2 ); z++ )
		{
			var chunk = new Chunk( x, y, z, entity.ChunkSize.x, entity.ChunkSize.y, entity.ChunkSize.z, entity: entity );
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
			foreach ( var child in entity.Entities.Values )
				child.Delete();

			entity.Delete();
		}

		entity = new VoxelEntity();
		entity.ChunkSize = new( Chunk.DEFAULT_WIDTH, Chunk.DEFAULT_DEPTH, Chunk.DEFAULT_HEIGHT );

		var chunks = await Importer.VoxImporter.Load( path, entity.ChunkSize.x, entity.ChunkSize.y, entity.ChunkSize.z, entity: entity );
		entity.Size = new( chunks.GetLength( 0 ), chunks.GetLength( 1 ), chunks.GetLength( 2 ) );
		entity.Chunks = chunks;

		foreach ( var chunk in entity.Chunks )
			entity.GenerateChunk( chunk );
	}
}
