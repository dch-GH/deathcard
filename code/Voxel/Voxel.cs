namespace DeathCard;

[Flags]
public enum Faces
{
	None = 0,
	Forward = 1 << 0,
	Backward = 1 << 1,
	Left = 1 << 2,
	Right = 1 << 3,
	Up = 1 << 4,
	Down = 1 << 5,
}

public struct Voxel
{
	public byte R;
	public byte G;
	public byte B;

	public Color32 Color => new Color32( R, G, B );

	public Voxel( Color32 col )
	{
		R = col.r;
		G = col.g;
		B = col.b;
	}
}
