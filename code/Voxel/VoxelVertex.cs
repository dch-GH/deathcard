namespace DeathCard;

[StructLayout( LayoutKind.Sequential )]
public struct VoxelVertex
{
	// x = 5 bits
	// y = 5 bits
	// z = 5 bits
	// Face = 3 bits
	// AO = 2 bits
	// Texture Index = 12 bits
	private readonly uint data;

	// R = 4 bits
	// G = 4 bits
	// B = 4 bits
	// A = 4 bits
	private readonly uint tint;

	public static readonly VertexAttribute[] Layout = new VertexAttribute[1]
	{
		new VertexAttribute( VertexAttributeType.TexCoord, VertexAttributeFormat.UInt32, 2, 10 ),
	};

	public VoxelVertex( Vector3B position, byte face, byte ao, Color32 tint = default, ushort textureIndex = 0 )
	{
		var data = ((textureIndex & 0xFFF) << 20)
			| ((ao & 0x3) << 18)
			| ((face & 0x7) << 15)
			| ((position.z & 0x1F) << 10) | ((position.y & 0x1F) << 5) | (position.x & 0x1F);

		this.data = (uint)data;
		this.tint = tint.RawInt;
	}
}
