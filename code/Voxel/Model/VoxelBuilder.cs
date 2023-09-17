namespace DeathCard.Importer;

public struct VoxelBuilder
{
	public Model Model { get; private set; }

	// Fields
	public string File;
	public Vector3 Scale;
	public float? Depth;
	public Vector3? Center;
	
	private BaseImporter importer;

	public static VoxelBuilder FromFile( string file )
	{
		// Get our format if it's supported.
		var extension = Path.GetExtension( file );
		if ( !BaseImporter.Cache.TryGetValue( extension, out var importer ) )
			throw new Exception( $"Voxel format for extension type '{extension}' not found." );

		return new()
		{ 
			File = file,
			importer = importer
		};
	}

	public VoxelBuilder WithDepth( float? depth = Utility.Scale )
	{
		return this with
		{
			Depth = depth
		};
	}

	public VoxelBuilder WithScale( Vector3 scale )
	{
		return this with
		{
			Scale = scale
		};
	}

	public VoxelBuilder WithCenter( Vector3 dimensions )
	{
		return this with
		{
			Center = dimensions
		};
	}

	private async Task<Dictionary<Vector3S, Chunk>> GetChunks()
	{
		// Check if we have a importer.
		if ( importer == null )
			throw new Exception( $"BaseImporter was not initialized." );

		// Try building the chunks with out importer.
		return await importer.BuildAsync( this );
	}

	public async Task<Model> FinishAsync()
	{
		// Get our chunks.
		var chunks = await GetChunks();

		// Build our model.
		var builder = Model.Builder;

		var material = Material.FromShader( "shaders/voxel.shader" );
		var mesh = new Mesh( material );
		var vertices = new List<VoxelVertex>();
		var indices = new List<int>();
		var offset = 0;

		foreach ( var (_, chunk) in chunks )
		{
			if ( chunk == null )
				continue;

			var size = Chunk.Size * (Scale / 2f);
			var c = Center ?? Vector3.Zero;
			var centerOffset = c * size / 2f;
			var chunkPosition = (Vector3)chunk.Position
				* Scale
				+ Scale / 2f;

			for ( ushort x = 0; x < Chunk.DEFAULT_WIDTH; x++ )
			for ( ushort y = 0; y < Chunk.DEFAULT_DEPTH; y++ )
			for ( ushort z = 0; z < Chunk.DEFAULT_HEIGHT; z++ )
			{
				var voxel = chunk.GetVoxel( x, y, z );
				if ( voxel == null )
					continue;

				var position = new Vector3B( x, y, z );

				// Generate all visible faces for our voxel.
				var drawCount = 0;
				for ( var i = 0; i < Utility.Faces; i++ )
				{
					var direction = Utility.Directions[i];
					var neighbour = chunk.GetDataByOffset( x + direction.x, y + direction.y, z + direction.z ).Voxel;
					if ( neighbour != null )
						continue;

					for ( var j = 0; j < 4; ++j )
					{
						var vertexIndex = Utility.FaceIndices[(i * 4) + j];
						var ao = Utility.BuildAO( chunk, position, i, j );

						vertices.Add( new VoxelVertex( position * new Vector3( 1, Depth ?? 1, 1 ), (byte)vertexIndex, (byte)i, ao, voxel.Value.Color ) );
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
		}

		mesh.CreateVertexBuffer<VoxelVertex>( vertices.Count, VoxelVertex.Layout, vertices.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );

		// Create a model for the mesh.
		return Model = builder
			.AddMesh( mesh )
			.Create();
	}
}
