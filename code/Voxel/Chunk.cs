namespace DeathCard;

public class Chunk
{
	public const ushort DEFAULT_WIDTH = 12;
	public const ushort DEFAULT_DEPTH = 12;
	public const ushort DEFAULT_HEIGHT = 12;

	private VoxelEntity parent;
	private Voxel?[,,] voxels;

	public ushort x;
	public ushort y;
	public ushort z;

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

	public void SetVoxel( ushort x, ushort y, ushort z, Voxel? voxel = null )
		=> voxels[x, y, z] = voxel;

	public IEnumerable<Chunk> TrySetVoxel( ushort x, ushort y, ushort z, Voxel? voxel = null, bool generating = false )
	{
		if ( parent == null )
			yield break;

		var width = parent.ChunkSize.x;
		var depth = parent.ChunkSize.y;
		var height = parent.ChunkSize.z;

		if ( x < 0 || y < 0 || z < 0
		  || x >= width || y >= depth || z >= height ) yield break;

		voxels[x, y, z] = voxel;
		yield return this;

		if ( x >= width - 1 && this.x + 1 < parent.Size.x )
			yield return parent.Chunks[this.x + 1, this.y, this.z];
		else if ( x == 0 && this.x - 1 >= 0 )
			yield return parent.Chunks[this.x - 1, this.y, this.z];

		if ( y >= depth - 1 && this.y + 1 < parent.Size.y )
			yield return parent.Chunks[this.x, this.y + 1, this.z];
		else if ( y == 0 && this.y - 1 >= 0 )
			yield return parent.Chunks[this.x, this.y - 1, this.z];

		if ( z >= height - 1 && this.z + 1 < parent.Size.z )
			yield return parent.Chunks[this.x, this.y, this.z + 1];
		else if ( z == 0 && this.z - 1 >= 0 )
			yield return parent.Chunks[this.x, this.y, this.z - 1];
	}

	public void Bind( VoxelEntity ent )
	{
		if ( ent == null || !ent.IsValid )
			return;

		parent = ent;
	}
}
