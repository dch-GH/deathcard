using System.Runtime.InteropServices;
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
	public Model Model { get; private set; }

	/// <summary>
	/// Is the model loaded already?
	/// </summary>
	[HideInEditor, JsonIgnore]
	public bool Loaded { get; private set; }

	/// <summary>
	/// Path to the file that this model is created from.
	/// </summary>
	public string Path { get; set; }

	/// <summary>
	/// Scale of this voxel model.
	/// </summary>
	public float Scale { get; set; } = Utility.Scale;

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
	/// Should the model be centered on every axis?
	/// </summary>
	public bool Center { get; set; } = true;

	protected override void PostLoad()
	{
		if ( all.ContainsKey( ResourcePath ) || Game.IsServer )
			return;

		all.Add( ResourcePath, this );
		
		new Action( async () => 
		{
			var mdl = await VoxelModel.FromFile( Path )
				.WithScale( Scale )
				.WithDepth( HasDepth
					? Depth 
					: null )
				.BuildAsync( center: Center );

			Loaded = true;
			Model = mdl;
		} ).Invoke();
	}

	public static VoxelResource Get( string path )
	{
		if ( !all.TryGetValue( path, out var resource ) )
			return null;

		return resource;
	}
}
