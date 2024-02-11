namespace Deathcard;

public struct Vector3S : IEquatable<Vector3S>
{
	public short x;
	public short y;
	public short z;

	public short this[int i]
	{
		get => i switch
		{
			0 => x,
			1 => y,
			2 => z,
			_ => 0
		};
		set
		{
			switch ( i )
			{
				case 0:
					x = value;
					break;

				case 1:
					y = value;
					break;

				case 2:
					z = value;
					break;
			}
		}
	}

	public Vector3S( int x, int y, int z )
	{
		this.x = (short)x;
		this.y = (short)y;
		this.z = (short)z;
	}

	public Vector3S( short x, short y, short z )
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public static implicit operator Vector3( Vector3S v )
		=> new Vector3( v.x, v.y, v.z );

	public static implicit operator Vector3S( Vector3 v )
		=> new Vector3S( (short)v.x.FloorToInt(), (short)v.y.FloorToInt(), (short)v.z.FloorToInt() );

	public static implicit operator Vector3S( (short x, short y, short z) v )
		=> new Vector3S( v.x, v.y, v.z );

	public static Vector3S operator +( Vector3S a, Vector3S b )
		=> new Vector3S( a.x + b.x, a.y + b.y, a.z + b.z );

	public static bool operator ==( Vector3S a, Vector3S b )
		=> a.Equals( b );

	public static bool operator !=( Vector3S a, Vector3S b )
		=> !(a == b);

	public bool Equals( Vector3S other )
	{
		return x == other.x
			&& y == other.y
			&& z == other.z;
	}

	public override bool Equals( object obj )
	{
		return obj is Vector3S other
			&& other.Equals( this );
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
