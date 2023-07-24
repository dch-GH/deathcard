namespace DeathCard;

partial class VoxelWorld
{
	private bool trySpreadX( Chunk chunk, bool canSpreadX, ref bool[,,] tested, (ushort x, ushort y, ushort z) start, ref (int x, int y, int z) size )
	{
		var yLimit = start.y + size.y;
		var zLimit = start.z + size.z;
		for ( ushort y = start.y; y < yLimit && canSpreadX; ++y )
		for ( ushort z = start.z; z < zLimit; ++z )
		{
			var newX = (ushort)(start.x + size.x);
			if ( newX >= ChunkSize.x || tested[newX, y, z] || chunk.GetVoxel( newX, y, z ) == null )
				canSpreadX = false;
		}

		if ( canSpreadX )
		{
			for ( ushort y = start.y; y < yLimit; ++y )
			for ( ushort z = start.z; z < zLimit; ++z )
			{
				var newX = (ushort)(start.x + size.x);
				tested[newX, y, z] = true;

				if ( chunk.GetVoxel( newX, y, z ) == null )
					return false;
			}

			++size.x;
		}

		return canSpreadX;
	}

	private bool trySpreadY( Chunk chunk, bool canSpreadY, ref bool[,,] tested, (ushort x, ushort y, ushort z) start, ref (int x, int y, int z) size )
	{
		var xLimit = start.x + size.x;
		var zLimit = start.z + size.z;
		for ( ushort x = start.x; x < xLimit && canSpreadY; ++x )
		for ( ushort z = start.z; z < zLimit; ++z )
		{
			var newY = (ushort)(start.y + size.y);
			if ( newY >= ChunkSize.y || tested[x, newY, z] || chunk.GetVoxel( x, newY, z ) == null )
				canSpreadY = false;
		}

		if ( canSpreadY )
		{
			for ( ushort x = start.x; x < xLimit; ++x )
			for ( ushort z = start.z; z < zLimit; ++z )
			{
				var newY = (ushort)(start.y + size.y);
				tested[x, newY, z] = true;

				if ( chunk.GetVoxel( x, newY, z ) == null )
					return false;
			}

			++size.y;
		}

		return canSpreadY;
	}

	private bool trySpreadZ( Chunk chunk, bool canSpreadZ, ref bool[,,] tested, (ushort x, ushort y, ushort z) start, ref (int x, int y, int z) size )
	{
		var xLimit = start.x + size.x;
		var yLimit = start.y + size.y;
		for ( ushort x = start.x; x < xLimit && canSpreadZ; ++x )
		for ( ushort y = start.y; y < yLimit; ++y )
		{
			var newZ = (ushort)(start.z + size.z);
			if ( newZ >= ChunkSize.z || tested[x, y, newZ] || chunk.GetVoxel( x, y, newZ ) == null )
				canSpreadZ = false;
		}

		if ( canSpreadZ )
		{
			for ( ushort x = start.x; x < xLimit; ++x )
			for ( ushort y = start.y; y < yLimit; ++y )
			{
				var newZ = (ushort)(start.z + size.z);
				tested[x, y, newZ] = true;

				if ( chunk.GetVoxel( x, y, newZ ) == null )
					return false;
			}

			++size.z;
		}

		return canSpreadZ;
	}
}
