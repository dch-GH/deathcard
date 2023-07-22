namespace DeathCard;

[StructLayout( LayoutKind.Sequential )]
public struct VoxelVertex
{
	public Vector3 position;
	public Color32 color;

	public static readonly VertexAttribute[] Layout = new VertexAttribute[2]
	{
		new VertexAttribute( VertexAttributeType.Position, VertexAttributeFormat.Float32 ),
		new VertexAttribute( VertexAttributeType.Color, VertexAttributeFormat.UInt8, 4 )
	};

	public VoxelVertex( Vector3 position, Color32 color )
	{
		this.position = position;
		this.color = color;
	}
}
