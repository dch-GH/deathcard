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

		for ( x = 0; x < width; x++ )
		for ( y = 0; y < depth; y++ )
		for ( z = 0; z < height; z++ )
		{
			SetVoxel( x, y, z, new Voxel( Color.Random.ToColor32() ), true );
		}
	}

	public Voxel? GetVoxel( ushort x, ushort y, ushort z )
	{
		if ( x < 0 || y < 0 || z < 0
		  || x >= Width || y >= Depth || z >= Height ) return null;

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

		var chunkPosition = (x: this.x, y: this.y, z: this.z);
		if ( offset.x > 1 || offset.x <= 0 )
			chunkPosition.x = (ushort)(chunkPosition.x + (x <= 0 ? -1 : 0) + offset.x);
		if ( offset.y > 1 || offset.y <= 0 )
			chunkPosition.y = (ushort)(chunkPosition.y + (y <= 0 ? -1 : 0) + offset.y);
		if ( offset.z > 1 || offset.z <= 0 )
			chunkPosition.z = (ushort)(chunkPosition.z + (z <= 0 ? -1 : 0) + offset.z);

		// Are we out of chunk bounds?
		if ( chunkPosition.x < 0 || chunkPosition.y < 0 || chunkPosition.z < 0
		  || chunkPosition.x >= parent.Chunks.GetLength( 0 ) || chunkPosition.y >= parent.Chunks.GetLength( 1 ) || chunkPosition.z >= parent.Chunks.GetLength( 2 ) ) return null;

		var newChunk = parent.Chunks[chunkPosition.x, chunkPosition.y, chunkPosition.z];

		// Calculate new voxel position.
		var voxelPosition = (
			x: (ushort)(x >= Width ? x % Width : x < 0 ? Width + (x % Width) : x),
			y: (ushort)(y >= Depth ? y % Depth : y < 0 ? Depth + (y % Depth) : y),
			z: (ushort)(z >= Height ? z % Height : z < 0 ? Height + (z % Height) : z)
		);

		return GetVoxel( voxelPosition.x, voxelPosition.y, voxelPosition.z );
	}

	public void SetVoxel( ushort x, ushort y, ushort z, Voxel? voxel = null, bool generating = false )
	{
		if ( x < 0 || y < 0 || z < 0
		  || x >= Width || y >= Depth || z >= Height ) return;
	
		voxels[x, y, z] = voxel;

		// TODO: Update neighbor chunk, if change happens on chunk border.
		if ( !generating )
			parent?.OnChunkChanged( this, x, y, z );
	}

	public void Bind( VoxelEntity ent )
	{
		if ( ent == null || !ent.IsValid )
			return;

		parent = ent;
	}
}
