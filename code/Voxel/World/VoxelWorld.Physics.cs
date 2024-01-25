namespace Deathcard;

public struct VoxelTraceResult
{
	public Vector3 Position;
	public Vector3B LocalPosition;
	public Vector3S GlobalPosition;

	public Chunk Chunk;
	public IVoxel Voxel;
	public bool Hit;

	public Vector3 Normal;
}

partial class VoxelWorld
{
	private bool trySpreadX( Chunk chunk, bool canSpreadX, ref bool[,,] tested, (byte x, byte y, byte z) start, ref (int x, int y, int z) size )
	{
		var yLimit = start.y + size.y;
		var zLimit = start.z + size.z;
		for ( byte y = start.y; y < yLimit && canSpreadX; ++y )
		for ( byte z = start.z; z < zLimit; ++z )
		{
			var newX = (byte)(start.x + size.x);
			if ( newX >= Chunk.DEFAULT_WIDTH || tested[newX, y, z] || chunk.GetVoxel( newX, y, z ) == null )
				canSpreadX = false;
		}

		if ( canSpreadX )
		{
			for ( byte y = start.y; y < yLimit; ++y )
			for ( byte z = start.z; z < zLimit; ++z )
			{
				var newX = (byte)(start.x + size.x);
				tested[newX, y, z] = true;

				if ( chunk.GetVoxel( newX, y, z ) == null )
					return false;
			}

			++size.x;
		}

		return canSpreadX;
	}

	private bool trySpreadY( Chunk chunk, bool canSpreadY, ref bool[,,] tested, (byte x, byte y, byte z) start, ref (int x, int y, int z) size )
	{
		var xLimit = start.x + size.x;
		var zLimit = start.z + size.z;
		for ( byte x = start.x; x < xLimit && canSpreadY; ++x )
		for ( byte z = start.z; z < zLimit; ++z )
		{
			var newY = (byte)(start.y + size.y);
			if ( newY >= Chunk.DEFAULT_DEPTH || tested[x, newY, z] || chunk.GetVoxel( x, newY, z ) == null )
				canSpreadY = false;
		}

		if ( canSpreadY )
		{
			for ( byte x = start.x; x < xLimit; ++x )
			for ( byte z = start.z; z < zLimit; ++z )
			{
				var newY = (byte)(start.y + size.y);
				tested[x, newY, z] = true;

				if ( chunk.GetVoxel( x, newY, z ) == null )
					return false;
			}

			++size.y;
		}

		return canSpreadY;
	}

	private bool trySpreadZ( Chunk chunk, bool canSpreadZ, ref bool[,,] tested, (byte x, byte y, byte z) start, ref (int x, int y, int z) size )
	{
		var xLimit = start.x + size.x;
		var yLimit = start.y + size.y;
		for ( byte x = start.x; x < xLimit && canSpreadZ; ++x )
		for ( byte y = start.y; y < yLimit; ++y )
		{
			var newZ = (byte)(start.z + size.z);
			if ( newZ >= Chunk.DEFAULT_HEIGHT || tested[x, y, newZ] || chunk.GetVoxel( x, y, newZ ) == null )
				canSpreadZ = false;
		}

		if ( canSpreadZ )
		{
			for ( byte x = start.x; x < xLimit; ++x )
			for ( byte y = start.y; y < yLimit; ++y )
			{
				var newZ = (byte)(start.z + size.z);
				tested[x, y, newZ] = true;

				if ( chunk.GetVoxel( x, y, newZ ) == null )
					return false;
			}

			++size.z;
		}

		return canSpreadZ;
	}

	/// <summary>
	/// Traces a ray in our VoxelWorld, used mostly for editor stuff.
	/// </summary>
	/// <param name="ray"></param>
	/// <param name="distance"></param>
	/// <param name="precision"></param>
	/// <returns></returns>
	public VoxelTraceResult Trace( Ray ray, float distance, float precision = Utility.Scale / 4f )
	{
		var result = new VoxelTraceResult();

		var start = ray.Position;
		var position = start;
		var direction = ray.Forward;

		while ( position.Distance( start ) <= distance )
		{
			var transformed = ApplyTransforms( position );
			var pos = WorldToVoxel( transformed );
			var local = GetLocalSpace( pos.x, pos.y, pos.z, out var chunk );
			var voxel = chunk?.GetVoxel( local.x, local.y, local.z );

			if ( voxel != null ) // We had a collision.
				return result with // todo @ceitine: add normal aswell
				{
					Position = transformed,
					LocalPosition = local,
					GlobalPosition = pos,

					Chunk = chunk,
					Voxel = voxel,
					Hit = true,
				};

			// Keep moving forward, we didn't hit anything.
			position += direction * precision;
		}

		return result;
	}
}
