namespace DeathCard;

[StructLayout( LayoutKind.Sequential )]
public struct VoxelVertex
{
	// x = 4 bits
	// y = 4 bits
	// z = 4 bits
	// Face = 3 bits
	// AO = 2 bits
	// Vertex Index = 3 bits
	// Texture Index = 12 bits
	private readonly uint data;

	// R = 4 bits
	// G = 4 bits
	// B = 4 bits
	// A = 4 bits
	private readonly uint data2;

	public static readonly VertexAttribute[] Layout = new VertexAttribute[1]
	{
		new VertexAttribute( VertexAttributeType.TexCoord, VertexAttributeFormat.UInt32, 2, 10 ),
	};

	public VoxelVertex( Vector3B position, byte vertex, byte face, byte ao, Color32 tint = default, ushort textureIndex = 0 )
	{
		var data = ((textureIndex & 0xFFF) << 20)
			| ((vertex & 0x7) << 17)
			| ((ao & 0x3) << 15)
			| ((face & 0x7) << 12)
			| ((position.z & 0xF) << 8) | ((position.y & 0xF) << 4) | (position.x & 0xF);

		this.data = (uint)data;
		this.data2 = tint.RawInt;
	}
}
