using DeathCard.Formats;

namespace DeathCard;

public struct VoxelModel
{
	public Model Model { get; private set; }
	
	private string file;
	private bool physics;
	private float scale;
	private float? depth;

	private static Dictionary<string, BaseFormat> cache = TypeLibrary
		.GetTypes<BaseFormat>()
		.Where( type => !type.IsAbstract )
		.Select( type => type.Create<BaseFormat>() )
		.ToDictionary( instance => instance.Extension );

	public static VoxelModel FromFile( string file )
	{
		return new VoxelModel()
		{
			file = file,
			scale = Utility.Scale
		};
	}

	public VoxelModel WithPhysics( bool physics = true )
	{
		return this with 
		{ 
			physics = true 
		};
	}

	public VoxelModel WithDepth( float? depth = Utility.Scale )
	{
		return this with
		{
			depth = depth
		};
	}

	public VoxelModel WithScale( float scale )
	{
		return this with
		{
			scale = scale
		};
	}

	public async Task<Model> BuildAsync( bool occlusion = true, bool center = true )
	{
		// Get our format if it's supported.
		var extension = Path.GetExtension( file );
		if ( !cache.TryGetValue( extension, out var format ) )
		{
			Log.Error( $"Voxel format for extension type '{extension}' not found." );
			return null;
		}

		// Build the voxel chunks.
		var chunks = await format.Build( file );
		if ( chunks == null )
			return null;

		// Build our model.
		var builder = Model.Builder;

		var material = Material.FromShader( "shaders/voxel.shader" );
		var mesh = new Mesh( material );
		var vertices = new List<VoxelVertex>();
		var indices = new List<int>();
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
			var chunkPosition = (Vector3)chunk.Position
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
				for ( var i = 0; i < Utility.Faces; i++ )
				{
					var direction = Utility.Neighbors[i];
					var neighbour = chunk.GetVoxelByOffset( x + direction.x, y + direction.y, z + direction.z );
					if ( neighbour != null )
						continue;

					var faceColor = Utility.FaceMultiply[i];
					for ( var j = 0; j < 4; ++j )
					{
						var vertexIndex = Utility.FaceIndices[(i * 4) + j];
						var pos = Utility.Positions[vertexIndex] * scale
							+ new Vector3( x, y, z ) * scale
							+ chunkPosition
							- centerOffset;

						var ao = occlusion 
							? Utility.BuildAO( chunk, position, i, j )
							: 1;
						var color = voxel.Value.Color.Multiply( ao * faceColor );
						vertices.Add( new VoxelVertex( pos * new Vector3( 1, depth ?? 1, 1 ), color ) );
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
