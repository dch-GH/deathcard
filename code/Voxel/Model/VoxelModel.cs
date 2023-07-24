using DeathCard.Formats;

namespace DeathCard;

public struct VoxelModel
{
	public Model Model { get; private set; }
	
	private string file;
	private bool physics;
	private float scale;
	private bool built;

	private static Dictionary<string, Model> modelCache = new();
	private static Dictionary<string, BaseFormat> cache = TypeLibrary
		.GetTypes<BaseFormat>()
		.Where( type => !type.IsAbstract )
		.Select( type => type.Create<BaseFormat>() )
		.ToDictionary( instance => instance.Extension );

	public static VoxelModel FromFile( string file )
	{
		var contains = modelCache.TryGetValue( file, out var model );

		return new VoxelModel()
		{
			file = file,
			scale = VoxelWorld.SCALE,
			built = contains,
			Model = model
		};
	}

	public VoxelModel WithPhysics( bool physics = true )
	{
		return this with 
		{ 
			physics = true 
		};
	}

	public VoxelModel WithScale( float scale )
	{
		return this with
		{
			scale = scale
		};
	}

	public async Task<Model> Build( bool occlusion = true, bool center = true )
	{
		// We already have the model.
		if ( built )
			return Model;

		// Get our format if it's supported.
		var extension = Path.GetExtension( file );
		if ( !cache.TryGetValue( extension, out var format ) )
		{
			Log.Error( $"Voxel format for extension type '{extension}' not found." );
			return null;
		}

		// Build the voxel chunks.
		var chunks = await format.Build( file );

		// Build our model.
		var builder = Model.Builder;

		var material = Material.FromShader( "shaders/voxel.shader" );
		var mesh = new Mesh( material );
		var vertices = new List<VoxelVertex>();
		var indices = new List<int>();
		var chunkPosition = Vector3.Zero;
		var offset = 0;

		var count = new Vector3(
			chunks.GetLength( 0 ),
			chunks.GetLength( 1 ),
			chunks.GetLength( 2 )
		);

		foreach ( var chunk in chunks )
		{
			var chunkSize = new Vector3( chunk.Width, chunk.Depth, chunk.Height );
			var centerOffset = center 
				? chunkSize * count * scale / 2f
				: 0;
			chunkPosition = (Vector3)chunk.Position
				* scale
				* chunkSize;

			for ( ushort x = 0; x < chunk.Width; x++ )
			for ( ushort y = 0; y < chunk.Depth; y++ )
			for ( ushort z = 0; z < chunk.Height; z++ )
			{
				var voxel = chunk.GetVoxel( x, y, z );
				if ( voxel == null )
					continue;

				var position = new Vector3I( x, y, z );

				// Generate all visible faces for our voxel.
				var drawCount = 0;
				for ( var i = 0; i < faces; i++ )
				{
					var direction = neighbors[i];
					var neighbour = chunk.GetVoxelByOffset( x + direction.x, y + direction.y, z + direction.z );
					if ( neighbour != null )
						continue;

					var faceColor = multiply[i];
					for ( var j = 0; j < 4; ++j )
					{
						var vertexIndex = faceIndices[(i * 4) + j];
						var pos = positions[vertexIndex] * scale
							+ new Vector3( x, y, z ) * scale
							+ chunkPosition
							- centerOffset;

						var ao = occlusion 
							? VoxelWorld.BuildAO( chunk, position, i, j )
							: 1;
						var col = voxel.Value.Color;
						var color = (Color.FromBytes( col.r, col.g, col.b ) * ao * faceColor)
							.ToColor32();
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
		}

		mesh.CreateVertexBuffer<VoxelVertex>( vertices.Count, VoxelVertex.Layout, vertices.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );

		// Create a model for the mesh.
		modelCache.Add( file, 
			Model = builder
				.AddMesh( mesh )
				.Create() );

		return Model;
	}

	#region Fields
	const int faces = 6;

	static readonly Vector3[]
		positions = new Vector3[8]
	{
		new Vector3( -0.5f, -0.5f, 0.5f ),
		new Vector3( -0.5f, 0.5f, 0.5f ),
		new Vector3( 0.5f, 0.5f, 0.5f ),
		new Vector3( 0.5f, -0.5f, 0.5f ),
		new Vector3( -0.5f, -0.5f, -0.5f ),
		new Vector3( -0.5f, 0.5f, -0.5f ),
		new Vector3( 0.5f, 0.5f, -0.5f ),
		new Vector3( 0.5f, -0.5f, -0.5f )
	};

	static readonly int[]
		faceIndices = new int[4 * faces]
	{
		0, 1, 2, 3,
		7, 6, 5, 4,
		0, 4, 5, 1,
		1, 5, 6, 2,
		2, 6, 7, 3,
		3, 7, 4, 0,
	};

	static readonly float[]
		multiply = new float[faces]
	{
		1f, 1f,
		0.85f, 0.7f,
		0.85f, 0.7f
	};

	static readonly (short x, short y, short z)[]
		neighbors = new (short, short, short)[faces]
	{
		(0, 0, 1),
		(0, 0, -1),
		(-1, 0, 0),
		(0, 1, 0),
		(1, 0, 0),
		(0, -1, 0),
	};
	#endregion
}
