using DeathCard.Importer;

namespace DeathCard.Formats;

public class VoxFormat : BaseFormat
{
	public override string Extension => ".vox";

	public override async Task<Dictionary<Vector3S, Chunk>> Build( string path )
	{
		return await VoxImporter.Load( path, single: true );
	}
}
