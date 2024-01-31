namespace Deathcard;

public enum BlockType : byte
{
	Block,
	TintedBlock,
	Sprite,
	Pane,
	Stair,
	Slab,
	Fence
}

public interface IVoxel
{
	/// <summary>
	/// All of our implemented BlockType readers.
	/// </summary>
	public static IReadOnlyDictionary<BlockType?, MethodDescription> Readers = TypeLibrary?
		.GetTypes<IVoxel>()?
		.Where( type => !type.IsInterface )
		.ToDictionary( 
			type => (BlockType?)type.GetProperty( "Deathcard.IVoxel.Type" ).GetValue( null ), // Key selector
			type => type.Methods?.FirstOrDefault( method => method.IsNamed( "Deathcard.IVoxel.Read" ) ) // Value selector
		);

	/// <summary>
	/// All of our BlockTypes.
	/// </summary>
	public static IReadOnlyDictionary<Type, BlockType?> BlockTypes = TypeLibrary?
		.GetTypes<IVoxel>()?
		.Where( type => !type.IsInterface )
		.ToDictionary(
			type => type.TargetType, // Key selector
			type => (BlockType?)type.GetProperty( "Deathcard.IVoxel.Type" ).GetValue( null ) // Value selector
		);

	/// <summary>
	/// The type that our block represents.
	/// </summary>
	public virtual static BlockType Type { get; }

	/// <summary>
	/// Builds vertices and other data based on the parameters.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="chunk"></param>
	/// <param name="position"></param>
	/// <param name="vertices"></param>
	/// <param name="indices"></param>
	/// <param name="offset"></param>
	/// <param name="buffer"></param>
	public abstract void Build( VoxelWorld world, Chunk chunk, Vector3B position,
		ref List<VoxelVertex> vertices, ref List<int> indices, ref int offset,
		CollisionBuffer buffer );

	// TODO: Include type specific functionality here too, overrides for changes on chunk, etc...

	/// <summary>
	/// Writes this IVoxel's data to a BinaryWriter.
	/// </summary>
	/// <param name="writer"></param>
	public abstract void Write( BinaryWriter writer );

	/// <summary>
	/// Reads an IVoxel from block type.
	/// </summary>
	/// <param name="reader"></param>
	/// <returns></returns>
	public virtual static IVoxel Read( BinaryReader reader )
	{
		throw new NotImplementedException();
	}
  
	/// <summary>
	/// Utility method for reading an IVoxel's data based on block type.
	/// </summary>
	/// <param name="reader"></param>
	/// <returns></returns>
	public static IVoxel TryRead( BinaryReader reader )
	{
		var type = (BlockType)reader.ReadByte();
		if ( !Readers.TryGetValue( type, out var method ) )
			return null;

		var voxel = method.InvokeWithReturn<IVoxel>( null, new[] { reader } );
		return voxel;
	}

	/// <summary>
	/// Gets BlockType from a type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static BlockType GetBlockType( Type type )
	{
		return 0; // TODO: Fix

		_ = BlockTypes.TryGetValue( type, out var block );
		return block ?? 0;
	}
}
