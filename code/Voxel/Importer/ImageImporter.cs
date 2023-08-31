namespace DeathCard.Importer;

public class ImageImporter : BaseImporter
{
	public override string Extension => ".png";

	public override async Task<Dictionary<Vector3S, Chunk>> BuildAsync( VoxelBuilder builder )
	{
		// Get our texture.
		var texture = await Texture.LoadAsync( FileSystem.Mounted, builder.File, false );
		if ( texture == null )
			return null;

		// Initialize our voxel chunk.
		var width = (ushort)texture.Width;
		var height = (ushort)texture.Height;
		var chunks = new Dictionary<Vector3S, Chunk>();

		// Generate voxels based on the pixels of our image.
		var pixels = texture.GetPixels();
		for ( ushort x = 0; x < width; x++ )
		for ( ushort y = 0; y < height; y++ )
		{
			var index = x % width + (height - y - 1) * width;
			var color = pixels[index];

			// Skip transparent pixels.
			if ( color.a == 0 )
				continue;

			var chunkPosition = new Vector3S( (x / 16f).FloorToInt(), 0, (y / 16f).FloorToInt() );
			if ( !chunks.TryGetValue( chunkPosition, out var chunk ) )
				chunks.Add( chunkPosition, chunk = new Chunk( (short)x, 0, (short)y, chunks ) );

			chunk.SetVoxel( x, 0, y, new Voxel( color ) );
		}

		return chunks;
	}
}
