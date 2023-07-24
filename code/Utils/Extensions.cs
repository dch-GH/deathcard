namespace DeathCard;

public static class Extensions
{
	/// <summary>
	/// Sets the voxel model of an entity using the VoxelModel formats.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="file"></param>
	/// <param name="scale"></param>
	/// <param name="occlusion"></param>
	public static async Task<Model> SetVoxelModel( this ModelEntity entity, string file, float scale = VoxelWorld.SCALE, bool occlusion = true )
	{
		var mdl = await VoxelModel.FromFile( file )
			.WithScale( scale )
			.Build( occlusion );

		return entity.Model = mdl;
	}
}
