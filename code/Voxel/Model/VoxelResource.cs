using System.Text.Json.Serialization;

namespace DeathCard;

[GameResource( "Voxel Model", "vxmdl", "Contains information of a model, models get generated automatically." )]
public class VoxelResource : GameResource
{
	private static Dictionary<string, VoxelResource> all = new();
	
	/// <summary>
	/// The generated model of this resource.
	/// </summary>
	[HideInEditor, JsonIgnore]
	public Model Model { get; set; }

	/// <summary>
	/// Is the model loaded already?
	/// </summary>
	[HideInEditor, JsonIgnore]
	public bool Loaded { get; set; } = false;

	/// <summary>
	/// Path to the file that this model is created from.
	/// </summary>
	public string Path { get; set; }

	/// <summary>
	/// Scale of this voxel model.
	/// </summary>
	public Vector3 Scale { get; set; } = new Vector3( Utility.Scale );

	/// <summary>
	/// Should the model use a fixed depth value?
	/// </summary>
	public bool HasDepth { get; set; }

	/// <summary>
	/// Depth of this voxel model, used only for image formats.
	/// </summary>
	[ShowIf( "HasDepth", true )]
	public float Depth { get; set; } = 1f;

	/// <summary>
	/// Centers a model based on this Vector3, 1 is centered, 0 is not.
	/// </summary>
	public Vector3 Center { get; set; } = Vector3.One;

	private async void rebuildModel()
	{
		Log.Info( $"Building VoxelModel \"{ResourceName}\"!" );

		var mdl = await VoxelModel.FromFile( Path )
			.WithScale( Scale )
			.WithDepth( HasDepth
				? Depth
				: null )
			.BuildAsync( center: Center );

		Model = mdl;
		Loaded = true;	
	}

	protected override void PostLoad()
	{
		if ( all.ContainsKey( ResourcePath ) || !Game.IsClient )
			return;

		all.Add( ResourcePath, this );
		rebuildModel();
	}

	protected override void PostReload()
	{
		if ( !Game.IsClient )
			return;

		rebuildModel();
	}

	public static VoxelResource Get( string path )
	{
		if ( !all.TryGetValue( path, out var resource ) )
			return null;

		return resource;
	}
}
