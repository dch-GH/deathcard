namespace Deathcard;

public struct VoxelTraceResult
{
	public Vector3B LocalPosition;
	public Vector3S GlobalPosition;

	public Chunk Chunk;
	public IVoxel Voxel;
	public bool Hit;

	public Vector3 Normal;
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
			(byte)((x % Chunk.DEFAULT_WIDTH + Chunk.DEFAULT_WIDTH ) % Chunk.DEFAULT_WIDTH ),
			(byte)((y % Chunk.DEFAULT_DEPTH + Chunk.DEFAULT_DEPTH ) % Chunk.DEFAULT_DEPTH ),
			(byte)((z % Chunk.DEFAULT_HEIGHT + Chunk.DEFAULT_HEIGHT) % Chunk.DEFAULT_HEIGHT ) );
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
	public (Chunk Chunk, IVoxel Voxel) GetByOffset( int x, int y, int z, Chunk relative = null )
	{
		// Get the new chunk's position based on the offset.
		var position = new Vector3S(
			(relative?.x ?? 0) + ((x + 1) / (float)Chunk.DEFAULT_WIDTH - 1).CeilToInt(),
			(relative?.y ?? 0) + ((y + 1) / (float)Chunk.DEFAULT_DEPTH - 1).CeilToInt(),
			(relative?.z ?? 0) + ((z + 1) / (float)Chunk.DEFAULT_HEIGHT - 1).CeilToInt()
		);

		// Calculate new voxel position.
		_ = Chunks.TryGetValue( position, out var chunk );
		return (
			Chunk: chunk,
			Voxel: chunk?.GetVoxel(
				(byte)((x % Chunk.DEFAULT_WIDTH + Chunk.DEFAULT_WIDTH ) % Chunk.DEFAULT_WIDTH),
				(byte)((y % Chunk.DEFAULT_DEPTH + Chunk.DEFAULT_DEPTH ) % Chunk.DEFAULT_DEPTH),
				(byte)((z % Chunk.DEFAULT_HEIGHT + Chunk.DEFAULT_HEIGHT) % Chunk.DEFAULT_HEIGHT) )
		);
	}

	/// <summary>
	/// Traces a ray in our VoxelWorld, used mostly for editor stuff.
	/// </summary>
	/// <param name="ray"></param>
	/// <param name="distance"></param>
	/// <param name="precision"></param>
	/// <returns></returns>
	public VoxelTraceResult Trace( Ray ray, float distance, float? precision = null ) // todo @ceitine: this doesn't actually take any rotations / scales into account.
	{
		var result = new VoxelTraceResult();
		var stepSize = precision ?? VoxelScale / 4f;

		var start = ray.Position;
		var position = start;
		var direction = ray.Forward;

		while ( position.Distance( start ) <= distance )
		{
			var pos = WorldToVoxel( position );
			var local = GetLocalSpace( pos.x, pos.y, pos.z, out var chunk );
			var voxel = chunk?.GetVoxel( local.x, local.y, local.z );

			if ( voxel != null ) // We had a collision.
				return result with
				{
					Chunk = chunk,
					LocalPosition = local,
					GlobalPosition = pos,
					Hit = true,
					Voxel = voxel
				};

			// Keep moving forward, we didn't hit anything.
			position += direction * stepSize;
		}

		return result;
	}
}
