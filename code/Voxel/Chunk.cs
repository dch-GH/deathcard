namespace DeathCard;

public class Chunk
{
	public const ushort DEFAULT_WIDTH = 16;
	public const ushort DEFAULT_DEPTH = 16;
	public const ushort DEFAULT_HEIGHT = 16;

	private VoxelEntity parent;
	private Voxel?[,,] voxels;

	public ushort x;
	public ushort y;
	public ushort z;

	public Vector3I Position => new( x, y, z );

	public Chunk( ushort x, ushort y, ushort z, ushort width = DEFAULT_WIDTH, ushort depth = DEFAULT_DEPTH, ushort height = DEFAULT_HEIGHT, VoxelEntity entity = null )
	{
		this.x = x;
		this.y = y;
		this.z = z;

		parent = entity;
		voxels = new Voxel?[width, depth, height];
	}

	public Voxel? GetVoxel( ushort x, ushort y, ushort z )
		=> voxels[x, y, z];

	public Voxel?[,,] GetVoxels()
		=> voxels;

	public Voxel? GetVoxelByOffset( int x, int y, int z )
	{
		if ( parent == null )
			return null;

		var width = parent.ChunkSize.x;
		var depth = parent.ChunkSize.y;
		var height = parent.ChunkSize.z;

		// Get the new chunk's position based on the offset.
		var position = (
			x: (ushort)(this.x + ((x + 1) / (float)width - 1).CeilToInt()),
			y: (ushort)(this.y + ((y + 1) / (float)depth - 1).CeilToInt()),
			z: (ushort)(this.z + ((z + 1) / (float)height - 1).CeilToInt())
		);
		
		// Are we out of chunk bounds?
		if ( position.x >= parent.Size.x
			|| position.y >= parent.Size.y
			|| position.z >= parent.Size.z ) return null;

		// Calculate new voxel position.
		return parent.Chunks[position.x, position.y, position.z]?.voxels[ 
			(ushort)((x % width + width) % width),
			(ushort)((y % depth + depth) % depth),
			(ushort)((z % height + height) % height)];
	}

	public Voxel? GetVoxelByOffset( Vector3I vec )
		=> GetVoxelByOffset( vec.x, vec.y, vec.z );

	public void SetVoxel( ushort x, ushort y, ushort z, Voxel? voxel = null )
		=> voxels[x, y, z] = voxel;

	public IEnumerable<Chunk> TrySetVoxel( int x, int y, int z, Voxel? voxel = null )
	{
		if ( parent == null )
			yield break;

		var width = parent.ChunkSize.x;
		var depth = parent.ChunkSize.y;
		var height = parent.ChunkSize.z;

		// Get the new chunk's position based on the offset.
		var position = (
			x: (ushort)(this.x + ((x + 1) / (float)width - 1).CeilToInt()),
			y: (ushort)(this.y + ((y + 1) / (float)depth - 1).CeilToInt()),
			z: (ushort)(this.z + ((z + 1) / (float)height - 1).CeilToInt())
		);

		// Are we out of chunk bounds?
		if ( position.x >= parent.Size.x
			|| position.y >= parent.Size.y
			|| position.z >= parent.Size.z ) yield break;

		var chunk = parent.Chunks[position.x, position.y, position.z];
		if ( chunk == null )
			yield break;

		chunk.voxels[
			(ushort)((x % width + width) % width),
			(ushort)((y % depth + depth) % depth),
			(ushort)((z % height + height) % height)] = voxel;

		yield return this;

		// If we are just setting a new voxel, we don't have to update neighboring chunks.
		if ( voxel != null )
			yield break;

		// Yield return affected neighbors.
		if ( x >= width - 1 && chunk.x + 1 < parent.Size.x )
			yield return parent.Chunks[chunk.x + 1, chunk.y, chunk.z];
		else if ( x == 0 && chunk.x - 1 >= 0 )
			yield return parent.Chunks[chunk.x - 1, chunk.y, chunk.z];

		if ( y >= depth - 1 && chunk.y + 1 < parent.Size.y )
			yield return parent.Chunks[chunk.x, chunk.y + 1, chunk.z];
		else if ( y == 0 && chunk.y - 1 >= 0 )
			yield return parent.Chunks[chunk.x, chunk.y - 1, chunk.z];

		if ( z >= height - 1 && chunk.z + 1 < parent.Size.z )
			yield return parent.Chunks[chunk.x, chunk.y, chunk.z + 1];
		else if ( z == 0 && chunk.z - 1 >= 0 )
			yield return parent.Chunks[chunk.x, chunk.y, chunk.z - 1];
	}

	public void Bind( VoxelEntity ent )
	{
		if ( ent == null || !ent.IsValid )
			return;

		parent = ent;
	}
}
