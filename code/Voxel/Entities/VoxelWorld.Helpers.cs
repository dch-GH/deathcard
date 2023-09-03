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
			(relative.x / VoxelScale.x).FloorToInt(),
			(relative.y / VoxelScale.y).FloorToInt(),
			(relative.z / VoxelScale.z).FloorToInt() );
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
	public Vector3B GetLocalSpace( int x, int y, int z, out Chunk chunk, Chunk relative = null )
	{
		var position = new Vector3S(
			((float)(x + (relative?.x ?? 0) * Chunk.Size.x) / Chunk.Size.x).FloorToInt(),
			((float)(y + (relative?.y ?? 0) * Chunk.Size.y) / Chunk.Size.y).FloorToInt(),
			((float)(z + (relative?.z ?? 0) * Chunk.Size.z) / Chunk.Size.z).FloorToInt()
		);

		_ = Chunks.TryGetValue( position, out chunk );

		return new Vector3B(
			(byte)((x % Chunk.Size.x + Chunk.Size.x) % Chunk.Size.x),
			(byte)((y % Chunk.Size.y + Chunk.Size.y) % Chunk.Size.y),
			(byte)((z % Chunk.Size.z + Chunk.Size.z) % Chunk.Size.z) );
	}

	/// <summary>
	/// Converts local coordinates of a chunk to global coordinates in a VoxelWorld.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="relative"></param>
	/// <returns></returns>
	public Vector3S GetGlobalSpace( byte x, byte y, byte z, Chunk? relative )
	{
		return new Vector3S(
			(short)(x + (relative?.x ?? 0) * Chunk.Size.x),
			(short)(y + (relative?.y ?? 0) * Chunk.Size.y),
			(short)(z + (relative?.z ?? 0) * Chunk.Size.z) );
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
			(relative?.x ?? 0) + ((x + 1) / (float)Chunk.Size.x - 1).CeilToInt(),
			(relative?.y ?? 0) + ((y + 1) / (float)Chunk.Size.y - 1).CeilToInt(),
			(relative?.z ?? 0) + ((z + 1) / (float)Chunk.Size.z - 1).CeilToInt()
		);

		// Calculate new voxel position.
		_ = Chunks.TryGetValue( position, out var chunk );
		return (
			Chunk: chunk,
			Voxel: chunk?.GetVoxel(
				(ushort)((x % Chunk.Size.x + Chunk.Size.x) % Chunk.Size.x),
				(ushort)((y % Chunk.Size.y + Chunk.Size.y) % Chunk.Size.y),
				(ushort)((z % Chunk.Size.z + Chunk.Size.z) % Chunk.Size.z) )
		);
	}
}
