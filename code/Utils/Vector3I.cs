namespace DeathCard;

public struct Vector3I : IEquatable<Vector3I>
{
	/// <summary>The X component of this Vector.</summary>
	public ushort x;

	/// <summary>The Y component of this Vector.</summary>
	public ushort y;

	/// <summary>The Z component of this Vector.</summary>
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
