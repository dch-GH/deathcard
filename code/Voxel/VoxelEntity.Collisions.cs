namespace DeathCard;

partial class VoxelEntity
{
	// TODO: Implement this in the mesh generation loop, we don't want to do it separately tbh.
	/*private (Vector3[] vertices, int[] indices) buildCollisions( Chunk chunk )
	{
		var vertices = new List<Vector3>();
		var indices = new List<int>();
		var size = ChunkSize.x > ChunkSize.y
			? ChunkSize.x > ChunkSize.z
				? ChunkSize.x
				: ChunkSize.z
			: ChunkSize.y;
		var offset = 0;

		// Loop for all dimensions X, Y and Z.
		for ( var d = 0; d < 3; ++d )
		{
			int i, j, k, l, w, h;
			var u = (d + 1) % 3;
			var v = (d + 2) % 3;
			var x = new int[3];
			var q = new int[3];
			var mask = new bool[size * size];
			q[d] = 1;

			for ( x[d] = -1; x[d] < size; )
			{
				// Compute the mask.
				var n = 0;
				for ( x[v] = 0; x[v] < size; ++x[v] )
				for ( x[u] = 0; x[u] < size; ++x[u] )
				{
					var blockCurrent = 0 <= x[d] 
						? chunk.GetVoxel( (ushort)x[0], (ushort)x[1], (ushort)x[2] )== null 
						: true;

					var blockCompare = x[d] < size - 1 
						? chunk.GetVoxel( (ushort)(x[0] + q[0]), (ushort)(x[1] + q[1]), (ushort)(x[2] + q[2]) ) == null 
						: true;

					mask[n++] = blockCurrent != blockCompare;
				}

				++x[d];
				n = 0;

				for ( j = 0; j < size; ++j )
				{
					for ( i = 0; i < size; )
					{
						if ( mask[n] )
						{
							// Compute width.
							for ( w = 1; i + w < size && mask[n + w]; w++ ) { }

							// Compute height.
							var done = false;
							for ( h = 1; j + h < size; h++ )
							{
								// Check each block next to this quad
								for ( k = 0; k < w; ++k )
								{
									// If there's a hole in the mask, exit
									if ( !mask[n + k + h * size] )
									{
										done = true;
										break;
									}
								}

								if ( done )
									break;
							}

							x[u] = i;
							x[v] = j;

							// Size and orientation.
							var du = new int[3];
							du[u] = w;

							var dv = new int[3];
							dv[v] = h;

							// Feed vertices and indices to list.
							vertices.Add( new Vector3( x[0], x[1], x[2] ) * VoxelScale - VoxelScale / 2f ); // Top-left
							vertices.Add( new Vector3( x[0] + du[0], x[1] + du[1], x[2] + du[2] ) * VoxelScale - VoxelScale / 2f ); // Top-right
							vertices.Add( new Vector3( x[0] + dv[0], x[1] + dv[1], x[2] + dv[2] ) * VoxelScale - VoxelScale / 2f ); // Bottom-left
							vertices.Add( new Vector3( x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2] ) * VoxelScale - VoxelScale / 2f ); // Bottom-right

							indices.Add( offset + 0 );
							indices.Add( offset + 1 );
							indices.Add( offset + 2 );
							indices.Add( offset + 3 );
							indices.Add( offset + 2 );
							indices.Add( offset + 1 );

							offset += 4;

							// Clear mask.
							for ( l = 0; l < h; ++l )
							for ( k = 0; k < w; ++k )
								mask[n + k + l * size] = false;

							i += w;
							n += w;
						}
						else
						{
							i++;
							n++;
						}
					}
				}
			}
		}

		return (vertices.ToArray(), indices.ToArray());
	}*/

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
