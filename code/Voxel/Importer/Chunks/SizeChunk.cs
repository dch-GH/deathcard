namespace DeathCard.Importer;

public class SizeChunk : VoxChunk
{
	public int x { get; set; }
	public int y { get; set; }
	public int z { get; set; }

	public SizeChunk( byte[] data )
	{
		using var stream = new MemoryStream( data );
		using var reader = new BinaryReader( stream );

		x = reader.ReadInt32();
		y = reader.ReadInt32();
		z = reader.ReadInt32();
	}
}
