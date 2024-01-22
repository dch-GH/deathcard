namespace Deathcard.Importer;

public class XYZIChunk : VoxChunk
{
	public int Voxels { get; set; }
	public (byte x, byte y, byte z, byte i)[] Values { get; set; }

	public XYZIChunk( byte[] data )
	{
		using var stream = new MemoryStream( data );
		using var reader = new BinaryReader( stream );

		var values = new List<(byte x, byte y, byte z, byte i)>();
		Voxels = reader.ReadInt32();
		for ( int i = 0; i < Voxels; i++ )
			values.Add((
				x: reader.ReadByte(),
				y: reader.ReadByte(),
				z: reader.ReadByte(),
				i: reader.ReadByte()
			) );

		Values = values.ToArray();
	}
}
