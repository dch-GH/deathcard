namespace DeathCard;

public struct Vector3I : IEquatable<Vector3I>
{
	public ushort x;
	public ushort y;
	public ushort z;

	public Vector3I( int x, int y, int z )
	{
		this.x = (ushort)x;
		this.y = (ushort)y;
		this.z = (ushort)z;
	}

	public Vector3I( ushort x, ushort y, ushort z )
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public static implicit operator Vector3( Vector3I v )
		=> new Vector3( v.x, v.y, v.z );

	public bool Equals( Vector3I other )
	{
		return x == other.x && y == other.y && z == other.z;
	}

	public override bool Equals( object obj )
	{
		return obj is Vector3I other && Equals( other );
	}

	public override int GetHashCode()
	{
		return HashCode.Combine( x, y, z );
	}

	public override string ToString()
	{
		return $"({x}, {y}, {z})";
	}
}
