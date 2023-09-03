using System.Text.Json.Serialization;

namespace DeathCard;

/// <summary>
/// A collection of Textures used by the Voxel system.
/// </summary>
[GameResource( "Texture Atlas", "atlas", "Deathcard Texture Atlas" )]
public class TextureAtlas : GameResource
{
	private static List<TextureAtlas> all = new();

	[HideInEditor, JsonIgnore]
	public Texture Albedo { get; private set; }

	[HideInEditor, JsonIgnore]
	public Texture RAE { get; private set; }

	/// <summary>
	/// The title of our atlas.
	/// </summary>
	public string Title { get; set; } = "Atlas";

	/// <summary>
	/// A Vector2 size of every single face.
	/// </summary>
	public Vector2 TextureSize { get; set; } = Vector2.One * 32f;

	/// <summary>
	/// List of all textures in this TextureAtlas.
	/// </summary>
	public List<SerializedTexture> Items { get; set; }

	/// <summary>
	/// Single pixel's size as a fraction.
	/// </summary>
	[HideInEditor, JsonIgnore]
	public Vector2 Step => 1f / TextureSize;

	/// <summary>
	/// The size of the whole TextureAtlas.
	/// </summary>
	[HideInEditor, JsonIgnore]
	public Vector2 Size => new Vector2(
		(int)(TextureSize.x * Items.Count * 4),
		(int)(TextureSize.y * 2) );

	// Take directly from GameResource code.
	private static string FixPath( string filename )
	{
		if ( filename == null )
		{
			return "";
		}

		filename = filename.NormalizeFilename( enforceInitialSlash: false );
		if ( filename.EndsWith( "_c" ) )
		{
			string text = filename;
			filename = text.Substring( 0, text.Length - 2 );
		}

		filename = filename.TrimStart( '/' );
		return filename;
	}

	/// <summary>
	/// Looks for a TextureAtlas from a path.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static TextureAtlas Get( string path )
	{
		path = FixPath( path );
		return all.FirstOrDefault( a => a.ResourcePath == path );
	}

	/// <summary>
	/// Gets the rectangle of a single texture by index.
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public Rect? GetRect( int index )
	{ 
		var x = index * 4 * TextureSize.x;
		if ( x >= Size.x )
			return null;

		return new Rect( x, 0, TextureSize.x * 4, TextureSize.y );
	}

	private void buildTextures()
	{
		// Don't generate atlas textures anywhere else but client.
		if ( !Game.IsClient )
			return;

		// Calculate required width and height.
		var width = (int)Size.x;
		var height = (int)Size.y;

		// Create our textures.
		Albedo?.Dispose();
		Albedo = Texture.Create( width, height )
			.WithName( $"{Title}_albedo" )
			.Finish();

		RAE?.Dispose();
		RAE = Texture.Create( width, height )
			.WithName( $"{Title}_rae" )
			.Finish();

		// Go through all serialized textures.
		var x = 0;
		var y = 0;
		var itemWidth = (int)(TextureSize.x * 4);
		var itemHeight = height;

		foreach ( var item in Items )
		{
			x += itemWidth;

			if ( item.Albedo == string.Empty ) 
				continue;

			var albedo = Texture.Load( FileSystem.Mounted, item.Albedo, false );
			if ( albedo == null
				|| albedo.Width != itemWidth
				|| albedo.Height != itemHeight )
			{
				Log.Error( $"[TextureAtlas] -> Failed to generate Albedo for {Title}." );
				continue;
			}

			// Read pixels and set for albedo.
			var pixels = albedo
				.GetPixels()
				.SelectMany( v => new[] { v.r, v.g, v.b, v.a } )
				.ToArray();

			Albedo.Update( pixels, x - itemWidth, y, itemWidth, itemHeight );
			
			// Check if we need to read RAE.
			if ( !item.HasRAE )
				continue;

			var rae = Texture.Load( FileSystem.Mounted, item.RAE, false );
			if ( rae == null
				|| rae.Width != itemWidth
				|| rae.Height != itemHeight )
			{
				Log.Error( $"[TextureAtlas] -> Failed to generate RAE for {Title}." );
				continue;
			}

			pixels = rae
				.GetPixels()
				.SelectMany( v => new[] { v.r, v.g, v.b, v.a } )
				.ToArray();

			RAE.Update( pixels, x - itemWidth, y, itemWidth, itemHeight );
		}

		DebugOverlay.Texture( Albedo, new Rect( 0, 0, width, height ), 15f );
		DebugOverlay.Texture( RAE, new Rect( width, 0, width, height ), 15f );
	}

	protected override void PostLoad()
	{
		if ( all.Contains( this ) )
			return;

		buildTextures();
		all.Add( this );
	}

	protected override void PostReload()
	{
		base.PostReload();
		buildTextures();
	}
}

public struct SerializedTexture
{
	public string Name { get; set; } = "My Texture";

	[ResourceType( "png" )]
	public string Albedo { get; set; }

	public bool Roughness { get; set; }
	public bool Alpha { get; set; }
	public bool Emission { get; set; }

	[ResourceType( "png" ), ShowIf( "HasRAE", true )]
	public string RAE { get; set; }

	[HideInEditor, JsonIgnore ]
	public bool HasRAE => Roughness || Alpha || Emission;

	public SerializedTexture() { }
}
