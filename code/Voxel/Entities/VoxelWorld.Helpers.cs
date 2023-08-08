namespace DeathCard;

partial class VoxelWorld
{
	/// <summary>
	/// Converts a world position to VoxelWorld position.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public Vector3I WorldToVoxel( Vector3 position )
	{
		var relative = position - Position;

		return new(
			(ushort)(relative.x / VoxelScale).FloorToInt(),
			(ushort)(relative.y / VoxelScale).FloorToInt(),
			(ushort)(relative.z / VoxelScale).FloorToInt() );
	}

	/// <summary>
	/// Converts global VoxelWorld coordinates to local, also outs the chunk.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="chunk"></param>
	/// <param name="relative"></param>
	/// <returns></returns>
	public Vector3I GetLocalSpace( int x, int y, int z, out Chunk chunk, Chunk relative = null )
	{
		var position = new Vector3I(
			(ushort)((float)(x + (relative?.x ?? 0) * ChunkSize.x) / ChunkSize.x).FloorToInt(),
			(ushort)((float)(y + (relative?.y ?? 0) * ChunkSize.y) / ChunkSize.y).FloorToInt(),
			(ushort)((float)(z + (relative?.z ?? 0) * ChunkSize.z) / ChunkSize.z).FloorToInt()
		);

		chunk = null;
		if ( position.x >= 0 && position.y >= 0 && position.z >= 0
		  && position.x < ChunkSize.x
		  && position.y < ChunkSize.y
		  && position.z < ChunkSize.z ) chunk = Chunks[position.x, position.y, position.z];

		return new Vector3I(
			(ushort)((x % ChunkSize.x + ChunkSize.x) % ChunkSize.x),
			(ushort)((y % ChunkSize.y + ChunkSize.y) % ChunkSize.y),
			(ushort)((z % ChunkSize.z + ChunkSize.z) % ChunkSize.z) );
	}

	/// <summary>
	/// Converts local coordinates of a chunk to global coordinates in a VoxelWorld.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="relative"></param>
	/// <returns></returns>
	public Vector3I GetGlobalSpace( ushort x, ushort y, ushort z, Chunk relative )
	{
		return new Vector3I(
			(ushort)(x + (relative?.x ?? 0) * ChunkSize.x),
			(ushort)(y + (relative?.y ?? 0) * ChunkSize.y),
			(ushort)(z + (relative?.z ?? 0) * ChunkSize.z) );
	}

	/// <summary>
	/// Gets chunk and voxel by offset relative to a chunk, or Chunks[0, 0, 0]
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="relative"></param>
	/// <returns></returns>
	public (Chunk Chunk, Voxel? Voxel) GetByOffset( int x, int y, int z, Chunk relative = null )
	{
		var chunk = relative ?? Chunks[0, 0, 0];
		if ( chunk == null )
			return (null, null);

		return Chunks[0, 0, 0].GetDataByOffset( x, y, z );
	}
}
