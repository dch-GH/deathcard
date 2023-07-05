namespace DeathCard;

[StructLayout( LayoutKind.Sequential )]
public struct VoxelVertex
{
	public Vector3 position;
	public Color32 color;
	public Vector3 normal;

	public static readonly VertexAttribute[] Layout = new VertexAttribute[3]
	{
		new VertexAttribute( VertexAttributeType.Position, VertexAttributeFormat.Float32 ),
		new VertexAttribute( VertexAttributeType.Color, VertexAttributeFormat.UInt8, 4 ),
		new VertexAttribute( VertexAttributeType.Normal, VertexAttributeFormat.Float32 )
	};

	public VoxelVertex( Vector3 position, Vector3 normal, Color32 color )
	{
		this.position = position;
		this.normal = normal;
		this.color = color;
	}
}
