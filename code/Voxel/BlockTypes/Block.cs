﻿namespace Deathcard;

/* TODO:
 * Different block types will need a different kind of VoxelVertex, and shader instructions.
 * For example stairs will need a vertex index and direction. We can make the vertices based on 1 direction, then rotate by direction * 90.
 * It'll be difficult to do greedy meshing for stuff like stairs, so maybe we shouldn't do it for them.
 */

public struct Block : IVoxel
{
	public ushort TextureId;

	static BlockType IVoxel.Type => BlockType.Block;

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

				vertices.Add( new VoxelVertex( position, vertexIndex, (byte)i, ao, Color32.White, TextureId ) );
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
		writer.Write( TextureId );
	}

	static IVoxel IVoxel.Read( BinaryReader reader )
	{
		var block = new Block()
		{
			TextureId = reader.ReadUInt16()
		};

		return block;
	}
}
