namespace Deathcard;

/* TODO:
 * Different block types will need a different kind of VoxelVertex, and shader instructions.
 * For example stairs will need a vertex index and direction. We can make the vertices based on 1 direction, then rotate by direction * 90.
 * It'll be difficult to do greedy meshing for stuff like stairs, so maybe we shouldn't do it for them.
 */

public interface IBlock
{
	bool ShouldDraw( VoxelWorld world, IBlock neighbor )
		=> (this is not Block self || !world.Atlas.Items[self.TextureId].Alpha)
		&& neighbor is Block block && world.Atlas.Items[block.TextureId].Alpha;

	public void BuildBlock( VoxelWorld world, Chunk chunk, Vector3B position,
		ref List<VoxelVertex> vertices, ref List<int> indices, ref int offset,
		CollisionBuffer buffer, Color32? color = null, ushort texId = 0 )
	{
		// Generate all visible faces for our voxel.
		var drawCount = 0;
		var col = color ?? Color32.White;

		for ( var i = 0; i < Utility.Faces; i++ )
		{
			var direction = Utility.Directions[i];
			var neighbour = chunk.GetDataByOffset( position.x + direction.x, position.y + direction.y, position.z + direction.z ).Voxel;

			if ( neighbour is IBlock block && !ShouldDraw( world, block ) )
				continue;

			for ( var j = 0; j < 4; ++j )
			{
				var vertexIndex = Utility.FaceIndices[(i * 4) + j];
				var ao = Utility.BuildAO( chunk, position, i, j );

				vertices.Add( new VoxelVertex( position, vertexIndex, (byte)i, ao, col, texId ) );
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
}

public struct Block : IBlock, IVoxel
{
	public ushort TextureId;

	static BlockType IVoxel.Type => BlockType.Block;

	void IVoxel.Build( VoxelWorld world, Chunk chunk, Vector3B position,
		ref List<VoxelVertex> vertices, ref List<int> indices, ref int offset,
		CollisionBuffer buffer ) => (this as IBlock).BuildBlock( world, chunk, position, ref vertices, ref indices, ref offset, buffer, texId: TextureId );

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

public struct TintedBlock : IBlock, IVoxel
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
		CollisionBuffer buffer ) => (this as IBlock).BuildBlock( world, chunk, position, ref vertices, ref indices, ref offset, buffer, color: Color );

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
