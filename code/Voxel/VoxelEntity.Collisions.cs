namespace DeathCard;

partial class VoxelEntity
{
	private bool trySpreadX( Chunk chunk, bool canSpreadX, ref bool[,,] tested, (ushort x, ushort y, ushort z) boxStart, ref (int x, int y, int z) boxSize )
	{
		var yLimit = boxStart.y + boxSize.y;
		var zLimit = boxStart.z + boxSize.z;
		for ( ushort y = boxStart.y; y < yLimit && canSpreadX; ++y )
			for ( ushort z = boxStart.z; z < zLimit; ++z )
			{
				var newX = (ushort)(boxStart.x + boxSize.x);
				if ( newX >= ChunkSize.x || tested[newX, y, z] || chunk.GetVoxel( newX, y, z ) == null )
					canSpreadX = false;
			}

		if ( canSpreadX )
		{
			for ( ushort y = boxStart.y; y < yLimit; ++y )
				for ( ushort z = boxStart.z; z < zLimit; ++z )
				{
					var newX = (ushort)(boxStart.x + boxSize.x);
					tested[newX, y, z] = true;

					if ( chunk.GetVoxel( newX, y, z ) == null )
						return false;
				}

			++boxSize.x;
		}

		return canSpreadX;
	}

	private bool trySpreadY( Chunk chunk, bool canSpreadY, ref bool[,,] tested, (ushort x, ushort y, ushort z) boxStart, ref (int x, int y, int z) boxSize )
	{
		var xLimit = boxStart.x + boxSize.x;
		var zLimit = boxStart.z + boxSize.z;
		for ( ushort x = boxStart.x; x < xLimit && canSpreadY; ++x )
			for ( ushort z = boxStart.z; z < zLimit; ++z )
			{
				var newY = (ushort)(boxStart.y + boxSize.y);
				if ( newY >= ChunkSize.y || tested[x, newY, z] || chunk.GetVoxel( x, newY, z ) == null )
					canSpreadY = false;
			}

		if ( canSpreadY )
		{
			for ( ushort x = boxStart.x; x < xLimit; ++x )
				for ( ushort z = boxStart.z; z < zLimit; ++z )
				{
					var newY = (ushort)(boxStart.y + boxSize.y);
					tested[x, newY, z] = true;

					if ( chunk.GetVoxel( x, newY, z ) == null )
						return false;
				}

			++boxSize.y;
		}

		return canSpreadY;
	}

	private bool trySpreadZ( Chunk chunk, bool canSpreadZ, ref bool[,,] tested, (ushort x, ushort y, ushort z) boxStart, ref (int x, int y, int z) boxSize )
	{
		var xLimit = boxStart.x + boxSize.x;
		var yLimit = boxStart.y + boxSize.y;
		for ( ushort x = boxStart.x; x < xLimit && canSpreadZ; ++x )
			for ( ushort y = boxStart.y; y < yLimit; ++y )
			{
				var newZ = (ushort)(boxStart.z + boxSize.z);
				if ( newZ >= ChunkSize.z || tested[x, y, newZ] || chunk.GetVoxel( x, y, newZ ) == null )
					canSpreadZ = false;
			}

		if ( canSpreadZ )
		{
			for ( ushort x = boxStart.x; x < xLimit; ++x )
				for ( ushort y = boxStart.y; y < yLimit; ++y )
				{
					var newZ = (ushort)(boxStart.z + boxSize.z);
					tested[x, y, newZ] = true;

					if ( chunk.GetVoxel( x, y, newZ ) == null )
						return false;
				}

			++boxSize.z;
		}

		return canSpreadZ;
	}
}
