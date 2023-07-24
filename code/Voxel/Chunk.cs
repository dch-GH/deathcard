namespace DeathCard;

public class Chunk
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

	public Voxel? GetVoxelByOffset( int x, int y, int z )
	{
		if ( chunks == null )
			return null;

		// Get the new chunk's position based on the offset.
		var position = (
			x: (ushort)(this.x + ((x + 1) / (float)Width - 1).CeilToInt()),
			y: (ushort)(this.y + ((y + 1) / (float)Depth - 1).CeilToInt()),
			z: (ushort)(this.z + ((z + 1) / (float)Height - 1).CeilToInt())
		);

		var size = (
			x: chunks.GetLength( 0 ),
			y: chunks.GetLength( 1 ),
			z: chunks.GetLength( 2 )
		);

		// Are we out of chunk bounds?
		if ( position.x >= size.x
			|| position.y >= size.y
			|| position.z >= size.z ) return null;

		// Calculate new voxel position.
		return chunks[position.x, position.y, position.z]?.voxels[ 
			(ushort)((x % Width + Width) % Width),
			(ushort)((y % Depth + Depth) % Depth),
			(ushort)((z % Height + Height) % Height)];
	}

	public Voxel? GetVoxelByOffset( Vector3I vec )
		=> GetVoxelByOffset( vec.x, vec.y, vec.z );

	public void SetVoxel( ushort x, ushort y, ushort z, Voxel? voxel = null )
		=> voxels[x, y, z] = voxel;

	public IEnumerable<Chunk> TrySetVoxel( int x, int y, int z, Voxel? voxel = null )
	{
		if ( chunks == null )
			yield break;

		// Get the new chunk's position based on the offset.
		var position = (
			x: (ushort)(this.x + ((x + 1) / (float)Width - 1).CeilToInt()),
			y: (ushort)(this.y + ((y + 1) / (float)Depth - 1).CeilToInt()),
			z: (ushort)(this.z + ((z + 1) / (float)Height - 1).CeilToInt())
		);

		var size = (
			x: chunks.GetLength( 0 ),
			y: chunks.GetLength( 1 ),
			z: chunks.GetLength( 2 )
		);

		// Are we out of chunk bounds?
		if ( position.x >= size.x
			|| position.y >= size.y
			|| position.z >= size.z ) yield break;

		var chunk = chunks[position.x, position.y, position.z];
		if ( chunk == null )
			yield break;

		chunk.voxels[
			(ushort)((x % Width + Width) % Width),
			(ushort)((y % Depth + Depth) % Depth),
			(ushort)((z % Height + Height) % Height)] = voxel;

		yield return this;

		// If we are just setting a new voxel, we don't have to update neighboring chunks.
		if ( voxel != null )
			yield break;

		// Yield return affected neighbors.
		if ( x >= Width - 1 && chunk.x + 1 < size.x )
			yield return chunks[chunk.x + 1, chunk.y, chunk.z];
		else if ( x == 0 && chunk.x - 1 >= 0 )
			yield return chunks[chunk.x - 1, chunk.y, chunk.z];

		if ( y >= Depth - 1 && chunk.y + 1 < size.y )
			yield return chunks[chunk.x, chunk.y + 1, chunk.z];
		else if ( y == 0 && chunk.y - 1 >= 0 )
			yield return chunks[chunk.x, chunk.y - 1, chunk.z];

		if ( z >= Height - 1 && chunk.z + 1 < size.z )
			yield return chunks[chunk.x, chunk.y, chunk.z + 1];
		else if ( z == 0 && chunk.z - 1 >= 0 )
			yield return chunks[chunk.x, chunk.y, chunk.z - 1];
	}
}
