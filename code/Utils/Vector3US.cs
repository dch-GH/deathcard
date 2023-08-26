namespace DeathCard;

public struct Vector3US : IEquatable<Vector3US>
{
	public ushort x;
	public ushort y;
	public ushort z;

	public ushort this[int i]
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

	public Vector3US( int x, int y, int z )
	{
		this.x = (ushort)x;
		this.y = (ushort)y;
		this.z = (ushort)z;
	}

	public Vector3US( ushort x, ushort y, ushort z )
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public static implicit operator Vector3( Vector3US v )
		=> new Vector3( v.x, v.y, v.z );

	public static implicit operator Vector3US( Vector3 v )
	=> new Vector3US( (ushort)v.x.FloorToInt(), (ushort)v.y.FloorToInt(), (ushort)v.z.FloorToInt() );

	public static implicit operator Vector3US( (ushort x, ushort y, ushort z) v )
		=> new Vector3US( v.x, v.y, v.z );

	public static Vector3US operator +( Vector3US a, Vector3US b )
		=> new Vector3US( a.x + b.x, a.y + b.y, a.z + b.z );

	public bool Equals( Vector3US other )
	{
		return x == other.x 
			&& y == other.y 
			&& z == other.z;
	}

	public override bool Equals( object obj )
	{
		return obj is Vector3US other
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
