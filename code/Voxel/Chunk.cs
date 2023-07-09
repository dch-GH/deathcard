namespace DeathCard;

public class Chunk
{
	public const ushort DEFAULT_WIDTH = 8;
	public const ushort DEFAULT_DEPTH = 8;
	public const ushort DEFAULT_HEIGHT = 8;

	private VoxelEntity parent;
	private Voxel?[,,] voxels;

	public ushort x;
	public ushort y;
	public ushort z;

	public ushort Width => (ushort)voxels.GetLength( 0 );
	public ushort Depth => (ushort)voxels.GetLength( 1 );
	public ushort Height => (ushort)voxels.GetLength( 2 );

	public Chunk( ushort x, ushort y, ushort z, ushort width = DEFAULT_WIDTH, ushort depth = DEFAULT_DEPTH, ushort height = DEFAULT_HEIGHT, VoxelEntity entity = null )
	{
		this.x = x;
		this.y = y;
		this.z = z;

		parent = entity;
		voxels = new Voxel?[width, depth, height];
	}

	public Voxel? GetVoxel( ushort x, ushort y, ushort z )
	{
		return voxels[x, y, z];
	}

	public Voxel? GetVoxelByOffset( short x, short y, short z )
	{
		if ( parent == null )
			return null;

		// Offset our chunk depending if we are on the border or not.
		var offset = (
			x: (float)(x + 1) / Width,
			y: (float)(y + 1) / Depth,
			z: (float)(z + 1) / Height
		);

		var chunkPosition = (
			x: (ushort)(this.x + (offset.x > 1 || offset.x <= 0 ? (x <= 1 ? -1 : 0) + offset.x : 0)),
			y: (ushort)(this.y + (offset.y > 1 || offset.y <= 0 ? (y <= 1 ? -1 : 0) + offset.y : 0)),
			z: (ushort)(this.z + (offset.z > 1 || offset.z <= 0 ? (z <= 1 ? -1 : 0) + offset.z : 0))
		);
		
		// Are we out of chunk bounds?
		var chunks = parent.Chunks;
		if ( chunkPosition.x >= parent.Size.x
			|| chunkPosition.y >= parent.Size.y
			|| chunkPosition.z >= parent.Size.z ) return null;
		var newChunk = chunks[chunkPosition.x, chunkPosition.y, chunkPosition.z];

		// Calculate new voxel position.
		return newChunk?.voxels[ 
			(ushort)((x % Width + Width) % Width),
			(ushort)((y % Depth + Depth) % Depth),
			(ushort)((z % Height + Height) % Height)];
	}

	public void SetVoxel( ushort x, ushort y, ushort z, Voxel? voxel = null )
	{
		voxels[x, y, z] = voxel;
	}

	public IReadOnlyList<Chunk> TrySetVoxel( ushort x, ushort y, ushort z, Voxel? voxel = null, bool generating = false )
	{
		var chunks = new List<Chunk>();
		if ( parent == null || x < 0 || y < 0 || z < 0
		  || x >= Width || y >= Depth || z >= Height ) return chunks;

		voxels[x, y, z] = voxel;
		chunks.Add( this );

		if ( x >= Width - 1 && this.x + 1 < parent.Chunks.GetLength( 0 ) )
			chunks.Add( parent.Chunks[this.x + 1, this.y, this.z] );
		else if ( x == 0 && this.x - 1 >= 0 )
			chunks.Add( parent.Chunks[this.x - 1, this.y, this.z] );

		if ( y >= Depth - 1 && this.y + 1 < parent.Chunks.GetLength( 1 ) )
			chunks.Add( parent.Chunks[this.x, this.y + 1, this.z] );
		else if ( y == 0 && this.y - 1 >= 0 )
			chunks.Add( parent.Chunks[this.x, this.y - 1, this.z] );

		if ( z >= Height - 1 && this.z + 1 < parent.Chunks.GetLength( 2 ) )
			chunks.Add( parent.Chunks[this.x, this.y, this.z + 1] );
		else if ( z == 0 && this.z - 1 >= 0 )
			chunks.Add( parent.Chunks[this.x, this.y, this.z - 1] );

		if ( !generating )
			foreach ( var chunk in chunks )
				parent?.GenerateChunk( chunk );

		return chunks;
	}

	public void Bind( VoxelEntity ent )
	{
		if ( ent == null || !ent.IsValid )
			return;

		parent = ent;
	}
}
