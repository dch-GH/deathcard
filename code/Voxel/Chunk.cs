namespace DeathCard;

public class Chunk
{
	public const ushort DEFAULT_WIDTH = 16;
	public const ushort DEFAULT_DEPTH = 16;
	public const ushort DEFAULT_HEIGHT = 16;

	private VoxelEntity parent;
	private Voxel?[,,] voxels;

	public ushort X;
	public ushort Y;
	public ushort Z;

	public ushort Width => (ushort)voxels.GetLength( 0 );
	public ushort Depth => (ushort)voxels.GetLength( 1 );
	public ushort Height => (ushort)voxels.GetLength( 2 );

	public Chunk( ushort x, ushort y, ushort z, ushort width = DEFAULT_WIDTH, ushort depth = DEFAULT_DEPTH, ushort height = DEFAULT_HEIGHT, VoxelEntity entity = null )
	{
		this.X = x;
		this.Y = y;
		this.Z = z;

		parent = entity;
		voxels = new Voxel?[width, depth, height];

		for ( x = 0; x < width; x++ )
		for ( y = 0; y < depth; y++ )
		for ( z = 0; z < height; z++ )
		{
			SetVoxel( x, y, z, new Voxel( Color.Random.ToColor32() ), true );
		}

		if ( Game.IsClient )
			parent?.GenerateChunk( this );
	}

	public Voxel? GetVoxel( ushort x, ushort y, ushort z )
	{
		if ( x < 0 || y < 0 || z < 0
		  || x >= Width || y >= Depth || z >= Height ) return null;

		return voxels[x, y, z];
	}

	public void SetVoxel( ushort x, ushort y, ushort z, Voxel? voxel = null, bool generating = false )
	{
		if ( x < 0 || y < 0 || z < 0
		  || x >= Width || y >= Depth || z >= Height ) return;
	
		voxels[x, y, z] = voxel;

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
