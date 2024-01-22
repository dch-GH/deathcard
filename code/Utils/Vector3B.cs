namespace Deathcard;

public struct Vector3B : IEquatable<Vector3B>
{
	public byte x;
	public byte y;
	public byte z;

	public byte this[int i]
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

	public Vector3B( int x, int y, int z )
	{
		this.x = (byte)x;
		this.y = (byte)y;
		this.z = (byte)z;
	}

	public Vector3B( byte x, byte y, byte z )
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public static implicit operator Vector3( Vector3B v )
		=> new Vector3( v.x, v.y, v.z );

	public static implicit operator Vector3B( Vector3 v )
	=> new Vector3B( (byte)v.x.FloorToInt(), (byte)v.y.FloorToInt(), (byte)v.z.FloorToInt() );

	public static implicit operator Vector3B( (byte x, byte y, byte z) v )
		=> new Vector3B( v.x, v.y, v.z );

	public static Vector3B operator +( Vector3B a, Vector3B b )
		=> new Vector3B( a.x + b.x, a.y + b.y, a.z + b.z );

	public bool Equals( Vector3B other )
	{
		return x == other.x 
			&& y == other.y 
			&& z == other.z;
	}

	public override bool Equals( object obj )
	{
		return obj is Vector3B other
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
