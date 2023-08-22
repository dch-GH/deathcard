namespace DeathCard.Importer;

public static class VoxImporter
{
	static Dictionary<string, Type> keys = new()
	{
		["MAIN"] = typeof( VoxChunk ),
		["SIZE"] = typeof( SizeChunk ),
		["XYZI"] = typeof( XYZIChunk ),
		["RGBA"] = typeof( RGBAChunk )
	};

	private static VoxChunk readChunk( BinaryReader reader )
	{
		var id = new string( reader.ReadChars( 4 ) );
		if ( id.Length == 0 )
			return null;

		var size = reader.ReadInt32();
		var childSize = reader.ReadInt32();
		var data = reader.ReadBytes( size );
		if ( !keys.TryGetValue( id, out var type ) )
			return new VoxChunk();
		
		var chunk = (VoxChunk)TypeLibrary.Create( type.FullName, type, new object[] { data } );
		
		chunk.Bytes = size;
		chunk.ChunkID = id;

		var children = new List<VoxChunk>();
		var read = 0;
		while ( read < childSize )
		{
			var child = readChunk( reader );
			if ( child == null )
				break;

			children.Add( child );
			read += child.Bytes;
		}

		chunk.Children = children.ToArray();

		return chunk;
	}

	public static async Task<Dictionary<Vector3S, Chunk>> Load( string file, ushort width = Chunk.DEFAULT_WIDTH, ushort depth = Chunk.DEFAULT_DEPTH, ushort height = Chunk.DEFAULT_WIDTH, bool single = false )
	{
		var data = await FileSystem.Mounted.ReadAllBytesAsync( file );

		using var stream = new MemoryStream( data );
		using var reader = new BinaryReader( stream );

		// Let's read the root chunk.
		var id = new string( reader.ReadChars( 4 ) );
		var version = reader.ReadInt32();
		var main = readChunk( reader );

		// Get the chunks that we need to construct our voxel chunks.
		var size = main.GetChild<SizeChunk>();
		var voxelData = main.GetChild<XYZIChunk>();
		var palette = main.GetChild<RGBAChunk>().Palette // We might not have a palette.
			?? RGBAChunk.Default;

		var chunks = new Dictionary<Vector3S, Chunk>();
		var length = voxelData.Values.Length;

		// Calculate chunk size if needed.
		var chunkSize = new Vector3US( width, depth, height );
		for ( int i = 0; i < length && single; i++ )
		{
			var voxel = voxelData.Values[i];
			if ( voxel.x > chunkSize.x )
				chunkSize.x = voxel.x;

			if ( voxel.y > chunkSize.y )
				chunkSize.y = voxel.y;

			if ( voxel.z > chunkSize.z )
				chunkSize.z = voxel.z;
		}

		// Go through all voxels.
		for ( int i = 0; i < length; i++ )
		{
			var voxel = voxelData.Values[i];
			var color = palette[voxel.i];
			var position = !single
				? new Vector3S(
					voxel.x / chunkSize.x,
					voxel.y / chunkSize.y,
					voxel.z / chunkSize.z
				)
				: new Vector3S( 0, 0, 0 );

			if ( !chunks.TryGetValue( position, out var chunk ) || chunk == null )
				chunks.Add( position, chunk = new Chunk( 
					position.x, position.y, position.z, 
					chunkSize.x, chunkSize.y, chunkSize.z, 
					chunks ) 
				);

			chunk.SetVoxel( (ushort)(voxel.x % chunkSize.x), (ushort)(voxel.y % chunkSize.y), (ushort)(voxel.z % chunkSize.z), new Voxel( color ) );
		}

		stream.Close();
		stream.Dispose();
		
		return chunks;
	}
}
