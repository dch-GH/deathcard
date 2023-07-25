using DeathCard.Importer;

namespace DeathCard.Formats;

public class VoxFormat : BaseFormat
{
	public override string Extension => ".vox";

	public override async Task<Chunk[,,]> Build( string path )
	{
		return await VoxImporter.Load( path, single: true );
	}
}
