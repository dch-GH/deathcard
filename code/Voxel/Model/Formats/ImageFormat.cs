namespace DeathCard.Formats;

public class ImageFormat : BaseFormat
{
	public override string Extension => ".png";

	public override async Task<Chunk[,,]> Build( string path )
	{
		// Get our texture.
		var texture = await Texture.LoadAsync( FileSystem.Mounted, path, false );
		if ( texture == null )
			return null;

		// Initialize our voxel chunk.
		var width = (ushort)texture.Width;
		var height = (ushort)texture.Height;
		var chunks = new Chunk[1, 1, 1];
		chunks[0, 0, 0] = new Chunk( 0, 0, 0, width, 1, height, chunks );

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

			chunks[0, 0, 0].SetVoxel( x, 0, y, new Voxel( color ) );
		}

		return chunks;
	}
}
