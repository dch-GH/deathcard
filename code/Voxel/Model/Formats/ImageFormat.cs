namespace DeathCard.Formats;

public class ImageFormat : BaseFormat
{
	public override string Extension => ".png";

	public override async Task<Chunk[,,]> Build( string path )
	{
		throw new NotImplementedException();
	}
}
