namespace Deathcard.Importer;

public class VoxImporter : BaseImporter
{
	public override string Extension => ".vox";

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

	public override async Task<Dictionary<Vector3S, Chunk>> BuildAsync( string path )
	{
		var buffer = await FileSystem.Mounted.ReadAllBytesAsync( path );

		using var stream = new MemoryStream( buffer );
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

		// Go through all voxels.
		for ( int i = 0; i < length; i++ )
		{
			var voxel = voxelData.Values[i];
			var color = palette[voxel.i];
			var position = new Vector3S( voxel.x / Chunk.Size.x, voxel.y / Chunk.Size.y, voxel.z / Chunk.Size.z );

			if ( !chunks.TryGetValue( position, out var chunk ) || chunk == null )
				chunks.Add( position, chunk = new Chunk( position.x, position.y, position.z, chunks ) );

			chunk.SetVoxel( (ushort)(voxel.x % Chunk.Size.x), (ushort)(voxel.y % Chunk.Size.y), (ushort)(voxel.z % Chunk.Size.z), new Voxel( color ) );
		}

		stream.Close();
		stream.Dispose();

		return chunks;
	}
}
