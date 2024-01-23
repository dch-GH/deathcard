namespace Deathcard;

public struct TintedBlock : IVoxel
{
	public byte R;
	public byte G;
	public byte B;

	public Color32 Color
	{
		get => new Color32( R, G, B );
		set
		{
			R = value.r;
			G = value.g; 
			B = value.b;
		}
	}

	static BlockType IVoxel.Type => BlockType.TintedBlock;

	void IVoxel.Build( VoxelWorld world, Chunk chunk, Vector3B position,
		ref List<VoxelVertex> vertices, ref List<int> indices, ref int offset,
		CollisionBuffer buffer )
	{
		// Generate all visible faces for our voxel.
		var drawCount = 0;

		for ( var i = 0; i < Utility.Faces; i++ )
		{
			var direction = Utility.Directions[i];
			var neighbour = chunk.GetDataByOffset( position.x + direction.x, position.y + direction.y, position.z + direction.z ).Voxel;
			if ( neighbour is Block or TintedBlock )
				continue;

			for ( var j = 0; j < 4; ++j )
			{
				var vertexIndex = Utility.FaceIndices[(i * 4) + j];
				var ao = Utility.BuildAO( chunk, position, i, j );

				vertices.Add( new VoxelVertex( position, vertexIndex, (byte)i, ao, Color, 0 ) );
			}

			indices.Add( offset + drawCount * 4 + 0 );
			indices.Add( offset + drawCount * 4 + 2 );
			indices.Add( offset + drawCount * 4 + 1 );
			indices.Add( offset + drawCount * 4 + 2 );
			indices.Add( offset + drawCount * 4 + 0 );
			indices.Add( offset + drawCount * 4 + 3 );

			drawCount++;
		}

		offset += 4 * drawCount;
	}

	void IVoxel.Write( BinaryWriter writer )
	{
		writer.Write( R );
		writer.Write( G );
		writer.Write( B );
	}

	static IVoxel IVoxel.Read( BinaryReader reader )
	{
		var block = new TintedBlock()
		{
			R = reader.ReadByte(),
			G = reader.ReadByte(),
			B = reader.ReadByte()
		};

		return block;
	}
}
