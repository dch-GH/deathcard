namespace Deathcard;

public class Chunk : IEquatable<Chunk>
{
	public const byte DEFAULT_WIDTH = 16;
	public const byte DEFAULT_DEPTH = 16;
	public const byte DEFAULT_HEIGHT = 16;

	public static Vector3B Size 
		= new Vector3B( Chunk.DEFAULT_WIDTH, Chunk.DEFAULT_DEPTH, Chunk.DEFAULT_HEIGHT );

	private Dictionary<Vector3S, Chunk> chunks;
	private IVoxel[,,] voxels;

	public short x;
	public short y;
	public short z;
	public Vector3S Position => new( x, y, z );
	public bool Empty { get; set; }

	public Chunk( short x, short y, short z, Dictionary<Vector3S, Chunk> chunks = null )
	{
		this.x = x;
		this.y = y;
		this.z = z;

		this.chunks = chunks;
		voxels = new IVoxel[DEFAULT_WIDTH, DEFAULT_DEPTH, DEFAULT_HEIGHT];
	}

	public IVoxel GetVoxel( byte x, byte y, byte z )
		=> voxels[x, y, z];

	public IVoxel[,,] GetVoxels()
		=> voxels;

	public void SetParent( Dictionary<Vector3S, Chunk> parent )
		=> chunks = parent;

	public (Chunk Chunk, IVoxel Voxel) GetDataByOffset( int x, int y, int z )
	{
		if ( chunks == null )
			return (null, null);

		// Get the new chunk's position based on the offset.
		var position = new Vector3S(
			this.x + ((x + 1) / (float)Chunk.DEFAULT_WIDTH - 1).CeilToInt(),
			this.y + ((y + 1) / (float)Chunk.DEFAULT_DEPTH - 1).CeilToInt(),
			this.z + ((z + 1) / (float)Chunk.DEFAULT_HEIGHT - 1).CeilToInt()
		);

		// Calculate new voxel position.
		if ( !chunks.TryGetValue( position, out var chunk ) )
			return (null, null);

		return (
			Chunk: chunk,
			Voxel: chunk?.voxels[ 
				(byte)((x % Chunk.DEFAULT_WIDTH + Chunk.DEFAULT_WIDTH ) % Chunk.DEFAULT_WIDTH ),
				(byte)((y % Chunk.DEFAULT_DEPTH + Chunk.DEFAULT_DEPTH ) % Chunk.DEFAULT_DEPTH ),
				(byte)((z % Chunk.DEFAULT_HEIGHT + Chunk.DEFAULT_HEIGHT) % Chunk.DEFAULT_HEIGHT )] 
		);
	}

	public void SetVoxel( byte x, byte y, byte z, IVoxel voxel = null )
		=> voxels[x, y, z] = voxel;

	public IEnumerable<Chunk> GetNeighbors( byte x, byte y, byte z, bool includeSelf = true )
	{
		// Let's include this chunk too if we want.
		if ( includeSelf )
			yield return this;

		var neighbors = new Vector3S[]
		{
			new( 1, 0, 0 ),
			new( -1, 0, 0 ),
			new( 0, 1, 0 ),
			new( 0, -1, 0 ),
			new( 0, 0, 1 ),
			new( 0, 0, -1 ),
		};
		var skip = false;

		// Yield return affected neighbors.
		foreach ( var direction in neighbors )
		{
			// If know the direction, we can just skip.
			if ( skip )
			{
				skip = false;
				continue;
			}

			// Check if we should include the neighbor.
			if ( chunks.TryGetValue( Position + direction, out var result )
			 && ((direction.x == 1 && x >= Chunk.DEFAULT_WIDTH - 1) || (direction.x == -1 && x <= 0)
			  || (direction.y == 1 && y >= Chunk.DEFAULT_DEPTH - 1) || (direction.y == -1 && y <= 0)
			  || (direction.z == 1 && z >= Chunk.DEFAULT_HEIGHT - 1) || (direction.z == -1 && z <= 0) ) )
			{
				skip = true;

				yield return result;
				continue;
			}
		}

		// Check last corner.
		// (This is a hacky fix for AO...)
		var directions = new Vector3S(
			x: x <= 0
				? -1
				: x >= Chunk.DEFAULT_WIDTH - 1
					? 1
					: 0,
			y: y <= 0
				? -1
				: y >= Chunk.DEFAULT_DEPTH - 1
					? 1
					: 0,
			z: z <= 0
				? -1
				: z >= Chunk.DEFAULT_HEIGHT - 1
					? 1
					: 0
		);
		
		var corner = new Vector3S( this.x, this.y, this.z ) + directions;
		if ( !corner.Equals( Position ) && chunks.TryGetValue( corner, out var chunk ) )
			yield return chunk;
	}

	public bool Equals( Chunk other )
	{
		return other.Position.Equals( Position );
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
