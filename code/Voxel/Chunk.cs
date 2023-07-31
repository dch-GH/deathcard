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
			|| position.z >= size.z ) return (null, null);

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

		var vox = (
			x: (ushort)((x % Width + Width) % Width),
			y: (ushort)((y % Depth + Depth) % Depth),
			z: (ushort)((z % Height + Height) % Height)
		);

		chunk.voxels[vox.x, vox.y, vox.z] = voxel;

		yield return chunk;

		// If we are just setting a new voxel, we don't have to update neighboring chunks.
		if ( voxel != null )
			yield break;

		// Yield return affected neighbors.
		if ( vox.x >= Width - 1 && position.x + 1 < size.x )
			yield return chunks[position.x + 1, position.y, position.z];
		else if ( vox.x == 0 && position.x - 1 >= 0 )
			yield return chunks[position.x - 1, position.y, position.z];

		if ( vox.y >= Depth - 1 && position.y + 1 < size.y )
			yield return chunks[position.x, position.y + 1, position.z];
		else if ( vox.y == 0 && position.y - 1 >= 0 )
			yield return chunks[position.x, position.y - 1, position.z];

		if ( vox.z >= Height - 1 && position.z + 1 < size.z )
			yield return chunks[position.x, position.y, position.z + 1];
		else if ( vox.z == 0 && position.z - 1 >= 0 )
			yield return chunks[position.x, position.y, position.z - 1];
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
}
