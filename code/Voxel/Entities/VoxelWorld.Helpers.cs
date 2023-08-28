namespace DeathCard;

partial class VoxelWorld
{
	/// <summary>
	/// Converts a world position to VoxelWorld position.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public Vector3S WorldToVoxel( Vector3 position )
	{
		var relative = position - Position;

		return new Vector3S(
			(relative.x / VoxelScale).FloorToInt(),
			(relative.y / VoxelScale).FloorToInt(),
			(relative.z / VoxelScale).FloorToInt() );
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
	public Vector3US GetLocalSpace( int x, int y, int z, out Chunk chunk, Chunk relative = null )
	{
		var position = new Vector3S(
			((float)(x + (relative?.x ?? 0) * ChunkSize.x) / ChunkSize.x).FloorToInt(),
			((float)(y + (relative?.y ?? 0) * ChunkSize.y) / ChunkSize.y).FloorToInt(),
			((float)(z + (relative?.z ?? 0) * ChunkSize.z) / ChunkSize.z).FloorToInt()
		);

		_ = Chunks.TryGetValue( position, out chunk );

		return new Vector3US(
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
	public Vector3S GetGlobalSpace( ushort x, ushort y, ushort z, Chunk? relative )
	{
		return new Vector3S(
			(short)(x + (relative?.x ?? 0) * ChunkSize.x),
			(short)(y + (relative?.y ?? 0) * ChunkSize.y),
			(short)(z + (relative?.z ?? 0) * ChunkSize.z) );
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
		// Get the new chunk's position based on the offset.
		var position = new Vector3S(
			(relative?.x ?? 0) + ((x + 1) / (float)ChunkSize.x - 1).CeilToInt(),
			(relative?.y ?? 0) + ((y + 1) / (float)ChunkSize.y - 1).CeilToInt(),
			(relative?.z ?? 0) + ((z + 1) / (float)ChunkSize.z - 1).CeilToInt()
		);

		// Calculate new voxel position.
		_ = Chunks.TryGetValue( position, out var chunk );
		return (
			Chunk: chunk,
			Voxel: chunk?.GetVoxel(
				(ushort)((x % ChunkSize.x + ChunkSize.x) % ChunkSize.x),
				(ushort)((y % ChunkSize.y + ChunkSize.y) % ChunkSize.y),
				(ushort)((z % ChunkSize.z + ChunkSize.z) % ChunkSize.z) )
		);
	}
}
