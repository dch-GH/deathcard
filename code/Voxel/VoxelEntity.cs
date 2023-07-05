namespace DeathCard;

public partial class VoxelEntity : ModelEntity
{
	public float VoxelScale { get; set; } = 1f / 0.0254f;
	public Chunk[,] Chunks;
	public Dictionary<Chunk, ModelEntity> Entities = new();

	public VoxelEntity( Vector3? position = null )
	{
		Position = position ?? Vector3.Zero;

		Chunks = new Chunk[2, 2];

		for ( ushort x = 0; x < 2; x++ )
		for ( ushort y = 0; y < 2; y++ )
			Chunks[x, y] = new( x, y, entity: this );
	}

	public void OnChunkChanged( Chunk chunk, ushort x, ushort y, ushort z )
	{
		GenerateChunk( chunk );
	}

	public void GenerateChunk( Chunk chunk )
	{
		// Get our chunk's entity.
		ModelEntity chunkEntity;
		if ( !Entities.TryGetValue( chunk, out chunkEntity ) )
			Entities.Add( chunk, chunkEntity = new ModelEntity() );

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

		var uAxis = new Vector3[]
		{
			Vector3.Forward,
			Vector3.Left,
			Vector3.Left,
			Vector3.Forward,
			Vector3.Right,
			Vector3.Backward,
		};

		var vAxis = new Vector3[]
		{
			Vector3.Left,
			Vector3.Forward,
			Vector3.Down,
			Vector3.Down,
			Vector3.Down,
			Vector3.Down,
		};

		// Let's create a mesh.
		var material = Material.FromShader( "shaders/voxel.shader" );
		var mesh = new Mesh( material );
		var vertices = new List<VoxelVertex>();
		var indices = new List<int>();
		var offset = 0;

		for ( ushort x = 0; x < chunk.Width; x++ )
		for ( ushort y = 0; y < chunk.Depth; y++ )
		for ( ushort z = 0; z < chunk.Height; z++ )
		{
			var voxel = chunk.GetVoxel( x, y, z );
			if ( voxel == null )
				continue;

			var faces = 6;
			var shouldHide = new bool[faces];
			/*var neighbors = new (short x, short y, short z)[]
			{
				(0, 0, 1),
				(0, 0, -1),
			};

			for( int i = 0; i < neighbors.Length; i++ )
			{
				var direction = neighbors[i];
				if ( x + direction.x < 0 || y + direction.y < 0 || z + direction.z < 0
				  || x + direction.x >= chunk.Width || y + direction.y >= chunk.Depth || z + direction.z >= chunk.Height )
					continue;

				var neighbor = chunk.GetVoxel( (ushort)(x + direction.x), (ushort)(y + direction.y), (ushort)(z + direction.z) );
				shouldHide[i] = neighbor != null;
			}*/
			
			var drawCount = 0;
			for ( var i = 0; i < faces; ++i )
			{
				if ( shouldHide[i] )
					continue;

				var tangent = uAxis[i];
				var binormal = vAxis[i];
				var normal = Vector3.Cross( tangent, binormal );

				for ( var j = 0; j < 4; ++j )
				{
					var vertexIndex = faceIndices[(i * 4) + j];
					var pos = positions[vertexIndex]
						+ new Vector3( x, y, z ) * VoxelScale;

					vertices.Add( new VoxelVertex( pos, normal, voxel.Value.Color ) );
				}

				indices.Add( offset + i * 4 + 0 );
				indices.Add( offset + i * 4 + 2 );
				indices.Add( offset + i * 4 + 1 );
				indices.Add( offset + i * 4 + 2 );
				indices.Add( offset + i * 4 + 0 );
				indices.Add( offset + i * 4 + 3 );

				drawCount++;
			}

			offset += 4 * drawCount;
		}

		mesh.CreateVertexBuffer<VoxelVertex>( vertices.Count, VoxelVertex.Layout, vertices.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );

		// Create a model for the mesh.
		var model = Model.Builder
			.AddMesh( mesh )
			.Create();

		chunkEntity.Model = model;
		chunkEntity.Position = Position + new Vector3( chunk.X * chunk.Width, chunk.Y * chunk.Depth ) * VoxelScale;
		chunkEntity.SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	static VoxelEntity ent;

	[ConCmd.Client( "testvox" )]
	public static void Test()
	{
		if ( ent != null )
		{
			foreach ( var child in ent.Entities.Values )
				child.Delete();

			ent.Delete();
		}

		ent = new VoxelEntity( Game.LocalPawn.Position );
	}
}
