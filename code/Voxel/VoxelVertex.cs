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

	// R = 8 bits
	// G = 8 bits
	// B = 8 bits
	// A = 8 bits
	// TODO: Get rid of alpha channel on tint, use last 8 bits for higlight texture index.
	private readonly uint data2;

	public static readonly VertexAttribute[] Layout = new VertexAttribute[1]
	{
		new VertexAttribute( VertexAttributeType.TexCoord, VertexAttributeFormat.UInt32, 2, 10 ),
	};

	// Once we update to greedy meshing, we wont have to use vertexIndex, we will only need position to determine where vertex should be placed.
	public VoxelVertex( Vector3B position, byte vertex, byte face, byte ao, Color32 tint = default, ushort textureIndex = 0 )
	{
		var data = ((textureIndex & 0xFFF) << 20)
			| ((vertex & 0x7) << 17)
			| ((ao & 0x3) << 15)
			| ((face & 0x7) << 12)
			| ((position.z & 0xF) << 8) | ((position.y & 0xF) << 4) | (position.x & 0xF);

		/*var data2 = (highlight << 24) 
			| (tint.b << 16) 
			| (tint.g << 8) 
			| tint.r;*/

		this.data = (uint)data;
		this.data2 = (uint)tint.RawInt;
	}
}
