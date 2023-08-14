namespace DeathCard;

public class Chunk : IEquatable<Chunk>
{
	public const ushort DEFAULT_WIDTH = 16;
	public const ushort DEFAULT_DEPTH = 16;
	public const ushort DEFAULT_HEIGHT = 16;

	private Chunk[,,] chunks;
	private Voxel?[,,] voxels;

	public ushort x;
	public ushort y;
	public ushort z;

	public ushort Width;
	public ushort Height;
	public ushort Depth;

	public Vector3I Position => new( x, y, z );

	public Chunk( ushort x, ushort y, ushort z, ushort width = DEFAULT_WIDTH, ushort depth = DEFAULT_DEPTH, ushort height = DEFAULT_HEIGHT, Chunk[,,] chunks = null )
	{
		this.x = x;
		this.y = y;
		this.z = z;

		Width = width;
		Height = height;
		Depth = depth;

		this.chunks = chunks;
		voxels = new Voxel?[width, depth, height];
	}

	public Voxel? GetVoxel( ushort x, ushort y, ushort z )
		=> voxels[x, y, z];

	public Voxel?[,,] GetVoxels()
		=> voxels;

	public (Chunk Chunk, Voxel? Voxel) GetDataByOffset( int x, int y, int z )
	{
		if ( chunks == null )
			return (null, null);

		// Get the new chunk's position based on the offset.
		var position = (
			x: this.x + ((x + 1) / (float)Width - 1).CeilToInt(),
			y: this.y + ((y + 1) / (float)Depth - 1).CeilToInt(),
			z: this.z + ((z + 1) / (float)Height - 1).CeilToInt()
		);

		var size = (
			x: chunks.GetLength( 0 ),
			y: chunks.GetLength( 1 ),
			z: chunks.GetLength( 2 )
		);

		// Are we out of chunk bounds?
		if ( position.x >= size.x || position.y >= size.y || position.z >= size.z
		  || position.x < 0 || position.y < 0 || position.z < 0 ) return (null, null);

		// Calculate new voxel position.
		var chunk = chunks[position.x, position.y, position.z];
		return (
			Chunk: chunk,
			Voxel: chunk?.voxels[ 
				(ushort)((x % Width + Width) % Width),
				(ushort)((y % Depth + Depth) % Depth),
				(ushort)((z % Height + Height) % Height)] 
		);
	}

	public void SetVoxel( ushort x, ushort y, ushort z, Voxel? voxel = null )
		=> voxels[x, y, z] = voxel;

	public IEnumerable<Chunk> GetNeighbors( ushort x, ushort y, ushort z, bool includeSelf = true )
	{
		// Find all neighbors.
		var size = (
			x: chunks.GetLength( 0 ),
			y: chunks.GetLength( 1 ),
			z: chunks.GetLength( 2 )
		);

		var corner = new int[3] { this.x, this.y, this.z };

		// Let's include this chunk too if we want.
		if ( includeSelf )
			yield return this;

		// Yield return affected neighbors.
		if ( x >= Width - 1 && this.x + 1 < size.x )
		{
			corner[0] = this.x + 1;
			yield return chunks[this.x + 1, this.y, this.z];
		}
		else if ( x == 0 && this.x - 1 >= 0 )
		{
			corner[0] = this.x - 1;
			yield return chunks[this.x - 1, this.y, this.z];
		}

		if ( y >= Depth - 1 && this.y + 1 < size.y )
		{
			corner[1] = corner[0] != this.x ? this.y : this.y + 1;
			yield return chunks[this.x, this.y + 1, this.z];
		}
		else if ( y == 0 && this.y - 1 >= 0 )
		{
			corner[1] = corner[0] != this.x ? this.y : this.y - 1;
			yield return chunks[this.x, this.y - 1, this.z];
		}

		if ( z >= Height - 1 && this.z + 1 < size.z )
		{
			corner[2] = corner[1] != this.y ? this.z : this.z + 1;
			yield return chunks[this.x, this.y, this.z + 1];
		}
		else if ( z == 0 && this.z - 1 >= 0 )
		{
			corner[2] = corner[1] != this.y ? this.z : this.z - 1;
			yield return chunks[this.x, this.y, this.z - 1];
		}

		// Check last corner.
		if ( corner[0] < size.x && corner[1] < size.y && corner[2] < size.z
		  && corner[0] >= 0 && corner[1] >= 0 && corner[2] >= 0 ) yield return chunks?[corner[0], corner[1], corner[2]];
	}

	public bool Equals( Chunk other )
	{
		return other.x == Position.x
			&& other.y == Position.y
			&& other.z == Position.z;
	}

	public override bool Equals( object obj )
	{
		return obj is Chunk other
			&& Equals ( other );
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode();
	}
}
