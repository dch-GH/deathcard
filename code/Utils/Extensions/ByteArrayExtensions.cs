namespace Deathcard;

public static class ByteArrayExtensions
{
	public static byte[] Compress( this byte[] data )
	{
		using var output = new MemoryStream();
		using ( DeflateStream dstream = new DeflateStream( output, CompressionLevel.Optimal ) )
			dstream.Write( data, 0, data.Length );

		return output.ToArray();
	}

	public static byte[] Decompress( this byte[] data )
	{
		using var input = new MemoryStream( data );
		using var output = new MemoryStream();
		using ( DeflateStream dstream = new DeflateStream( input, CompressionMode.Decompress ) )
			dstream.CopyTo( output );

		return output.ToArray();
	}
}
