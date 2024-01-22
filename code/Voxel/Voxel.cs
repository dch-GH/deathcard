namespace Deathcard;

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
