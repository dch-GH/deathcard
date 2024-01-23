namespace Deathcard;

/// <summary>
/// A collection of Textures used by the Voxel system.
/// </summary>
[GameResource( "Texture Atlas", "atlas", "Deathcard Texture Atlas" )]
public class TextureAtlas : GameResource
{
	private static List<TextureAtlas> all = new();

	[Hide, JsonIgnore]
	public Texture Albedo { get; private set; }

	[Hide, JsonIgnore]
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
	[Hide, JsonIgnore]
	public Vector2 Step => 1f / Size;

	/// <summary>
	/// The size of the whole TextureAtlas.
	/// </summary>
	[Hide, JsonIgnore]
	public Vector2 Size => new Vector2(
		(int)(TextureSize.x * Utility.Faces),
		(int)TextureSize.y );

	// Take directly from GameResource code.
	private static string FixPath( string filename )
	{
		if ( filename == null )
			return "";

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

	private void buildTextures()
	{
		// Calculate required width and height.
		var width = (int)TextureSize.x;
		var height = (int)TextureSize.y;
		var depth = Utility.Faces;
		
		// Create our textures.
		Albedo?.Dispose();
		Albedo = Texture.CreateArray( width, height, Items.Count * depth )
			.WithName( $"{Title}_albedo" )
			.Finish();

		RAE?.Dispose();
		RAE = Texture.CreateArray( width, height, Items.Count * depth )
			.WithName( $"{Title}_rae" )
			.Finish();

		// Go through all serialized textures.
		var z = 0;

		foreach ( var item in Items )
		{
			z += depth;

			if ( item.Albedo == string.Empty ) 
				continue;

			var albedo = Texture.Load( FileSystem.Mounted, item.Albedo, false );
			if ( albedo == null
				|| albedo.Width != (int)Size.x
				|| albedo.Height != (int)Size.y )
			{
				Log.Error( $"[TextureAtlas] -> Failed to generate Albedo for {Title}." );
				continue;
			}

			var rae = Texture.Load( FileSystem.Mounted, item.RAE, false );
			if ( item.HasRAE && (rae == null 
				|| rae.Width != (int)Size.x
				|| rae.Height != (int)Size.y) )
			{
				Log.Error( $"[TextureAtlas] -> Failed to generate RAE for {Title}." );
				continue;
			}

			// Read pixels and set for albedo.
			for ( int i = 0; i < depth; i++ )
			{
				var buffer = new Color32[width * height];
				var rect = (i * width, 0, width, height);

				// Update albedo.
				albedo.GetPixels( rect, 0, 0, buffer.AsSpan(), ImageFormat.RGBA8888 );
				var albedoPixels = buffer
					.SelectMany( v => new[] { v.r, v.g, v.b, v.a } )
					.ToArray();
				Albedo.Update3D( albedoPixels, 0, 0, z - depth + i, width, height, 1 );

				// Check if we need to read RAE.
				if ( !item.HasRAE )
					continue;

				// Update RAE.
				rae.GetPixels( rect, 0, 0, buffer.AsSpan(), ImageFormat.RGBA8888 );
				var raePixels = buffer
					.SelectMany( v => new[] { v.r, v.g, v.b, v.a } )
					.ToArray();
				RAE.Update3D( raePixels, 0, 0, z - depth + i, width, height, 1 );
			}
		}
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

	[Hide, JsonIgnore]
	public bool HasRAE => Roughness || Alpha || Emission;

	public SerializedTexture() { }
}
