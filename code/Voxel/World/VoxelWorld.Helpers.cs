namespace Deathcard;

public struct VoxelQueryData
{
	public IVoxel Voxel;
	public Vector3B Position;
	public Chunk Chunk;
}

partial class VoxelWorld
{
	/// <summary>
	/// Converts a world position to VoxelWorld position.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public Vector3S WorldToVoxel( Vector3 position )
	{
		var relative = position - Transform.Position;

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
			((float)(x + (relative?.x ?? 0) * Chunk.DEFAULT_WIDTH) / Chunk.DEFAULT_WIDTH).FloorToInt(),
			((float)(y + (relative?.y ?? 0) * Chunk.DEFAULT_DEPTH) / Chunk.DEFAULT_DEPTH).FloorToInt(),
			((float)(z + (relative?.z ?? 0) * Chunk.DEFAULT_HEIGHT) / Chunk.DEFAULT_HEIGHT).FloorToInt()
		);

		_ = Chunks.TryGetValue( position, out chunk );

		return new Vector3B(
			(byte)((x % Chunk.DEFAULT_WIDTH + Chunk.DEFAULT_WIDTH) % Chunk.DEFAULT_WIDTH),
			(byte)((y % Chunk.DEFAULT_DEPTH + Chunk.DEFAULT_DEPTH) % Chunk.DEFAULT_DEPTH),
			(byte)((z % Chunk.DEFAULT_HEIGHT + Chunk.DEFAULT_HEIGHT) % Chunk.DEFAULT_HEIGHT) );
	}

	/// <summary>
	/// Converts local coordinates of a chunk to global coordinates in a VoxelWorld.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="relative"></param>
	/// <returns></returns>
	public Vector3S GetGlobalSpace( byte x, byte y, byte z, Chunk relative )
	{
		return new Vector3S(
			(short)(x + (relative?.x ?? 0) * Chunk.DEFAULT_WIDTH),
			(short)(y + (relative?.y ?? 0) * Chunk.DEFAULT_DEPTH),
			(short)(z + (relative?.z ?? 0) * Chunk.DEFAULT_HEIGHT) );
	}

	/// <summary>
	/// Gets chunk and voxel by offset relative to a chunk, or Chunks[0, 0, 0]
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="relative"></param>
	/// <returns></returns>
	public VoxelQueryData GetByOffset( int x, int y, int z, Chunk relative = null )
	{
		// Get the new chunk's position based on the offset.
		var position = new Vector3S(
			(relative?.x ?? 0) + ((x + 1) / (float)Chunk.DEFAULT_WIDTH - 1).CeilToInt(),
			(relative?.y ?? 0) + ((y + 1) / (float)Chunk.DEFAULT_DEPTH - 1).CeilToInt(),
			(relative?.z ?? 0) + ((z + 1) / (float)Chunk.DEFAULT_HEIGHT - 1).CeilToInt()
		);

		// Calculate new voxel position.
		_ = Chunks.TryGetValue( position, out var chunk );

		var vx = (byte)((x % Chunk.DEFAULT_WIDTH + Chunk.DEFAULT_WIDTH) % Chunk.DEFAULT_WIDTH);
		var vy = (byte)((y % Chunk.DEFAULT_DEPTH + Chunk.DEFAULT_DEPTH) % Chunk.DEFAULT_DEPTH);
		var vz = (byte)((z % Chunk.DEFAULT_HEIGHT + Chunk.DEFAULT_HEIGHT) % Chunk.DEFAULT_HEIGHT);

		return new VoxelQueryData
		{
			Chunk = chunk,
			Voxel = chunk?.GetVoxel( vx, vy, vz ),
			Position = new Vector3B( vx, vy, vz )
		};
	}

	/// <summary>
	/// Gets chunk and voxel by offset relative to a chunk, or Chunks[0, 0, 0]
	/// </summary>
	/// <param name="offset"></param>
	/// <param name="relative"></param>
	/// <returns></returns>
	public VoxelQueryData GetByOffset( Vector3S offset, Chunk relative = null )
	{
		return GetByOffset( (int)offset.x, (int)offset.y, (int)offset.z, relative );
	}

	private Chunk GetOrCreateChunk( int x, int y, int z, Vector3B? local = null, Chunk relative = null )
	{
		// Calculate new chunk position.
		var position = new Vector3S(
			((relative?.Position.x ?? 0) + (float)x / Chunk.Size.x - (float)(local?.x ?? 0) / Chunk.Size.x).CeilToInt(),
			((relative?.Position.y ?? 0) + (float)y / Chunk.Size.y - (float)(local?.y ?? 0) / Chunk.Size.y).CeilToInt(),
			((relative?.Position.z ?? 0) + (float)z / Chunk.Size.z - (float)(local?.z ?? 0) / Chunk.Size.z).CeilToInt()
		);

		// Check if we have a chunk already or are out of bounds.
		if ( Chunks.TryGetValue( position, out var chunk ) )
			return chunk;

		// Create new chunk.
		Chunks.Add(
			position,
			chunk = new Chunk( position.x, position.y, position.z, Chunks )
		);

		return chunk;
	}

	/// <summary>
	/// Sets voxel by offset, relative to the chunk parameter or Chunks[0, 0, 0].
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="voxel"></param>
	/// <param name="relative"></param>
	/// <returns></returns>
	public VoxelQueryData SetVoxel( int x, int y, int z, IVoxel voxel, Chunk relative = null )
	{
		// Convert to local space.
		var pos = GetLocalSpace( x, y, z, out var chunk, relative );

		// Create new chunk if needed.
		if ( chunk == null && voxel != null )
			chunk = GetOrCreateChunk( x, y, z, pos, relative );

		// Set voxel.
		chunk?.SetVoxel( pos.x, pos.y, pos.z, voxel );
		return new VoxelQueryData
		{
			Chunk = chunk,
			Position = pos,
			Voxel = voxel
		};
	}

	/// <summary>
	/// Apply our VoxelWorld's transform to a worldspace vector.
	/// </summary>
	/// <param name="vector"></param>
	/// <returns></returns>
	public Vector3 ApplyTransforms( Vector3 vector )
		=> new Vector3( (vector - Transform.Position) * Transform.Rotation.Inverse / Transform.Scale + Transform.Position );
}
