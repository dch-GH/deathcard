namespace DeathCard;

[SceneCamera.AutomaticRenderHook]
public class FogRenderer : RenderHook
{
	private Material material => Material.FromShader( "shaders/fog.shader" );
	private RenderAttributes attributes = new RenderAttributes();

	private Color color;
	private float radius;

	public Color Color 
	{
		get => color;
		set 
		{ 
			color = value; 
			attributes.Set( "Color", color ); 
		}
	}
	
	public float Radius 
	{
		get => radius;
		set 
		{ 
			radius = value; 
			attributes.Set( "Radius", radius ); 
		} 
	}

	public override void OnStage( SceneCamera target, Stage stage )
	{
		if ( stage != Stage.AfterSkybox )
			return;

		Graphics.GrabFrameTexture( "ColorBuffer", attributes );
		Graphics.GrabDepthTexture( "DepthBuffer", attributes );

		Graphics.Blit( material, attributes );
	}
}
