HEADER
{
	Description = "Voxel Model Shader";
}

//=========================================================================================================================
// Optional
//=========================================================================================================================
FEATURES
{
    #include "common/features.hlsl"
	Feature( F_TRANSPARENCY, 0..1, "Rendering" );
	Feature( F_EMISSIVE, 0..1, "Rendering" );
}

//=========================================================================================================================
// Optional
//=========================================================================================================================
MODES
{
	VrForward();
	ToolsVis( S_MODE_TOOLS_VIS );
	Depth( S_MODE_DEPTH );
}

//=========================================================================================================================
COMMON
{
	#define S_TRANSLUCENT 0
	#include "common/shared.hlsl"
}

//=========================================================================================================================

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

//=========================================================================================================================

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

//=========================================================================================================================

VS
{
	#include "common/vertex.hlsl"

	//
	// Main
	//
	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		return FinalizeVertex( o );
	}
}

//=========================================================================================================================

PS
{ 
	StaticCombo( S_TRANSPARENCY, F_TRANSPARENCY, Sys( ALL ) );
	
	#define CUSTOM_TEXTURE_FILTERING
    SamplerState Sampler < Filter( POINT ); AddressU( WRAP ); AddressV( WRAP ); >;
	SamplerState SamplerAniso < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >; 

	StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );
	StaticCombo( S_EMISSIVE, F_EMISSIVE, Sys( ALL ) );

	#define CUSTOM_MATERIAL_INPUTS
	CreateInputTexture2D( Color, Srgb, 8, "", "_color", "Material,10/10", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( ColorTintMask, Linear, 8, "", "_tint", "Material,10/20", Default3( 1.0, 1.0, 1.0 ) );
	float3 g_flColorTint < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Material,10/20" ); >;

	Texture2D g_tColor < Channel( RGB, Box( Color ), Srgb ); Channel( A, Box( ColorTintMask ), Linear ); OutputFormat( BC7 ); SrgbRead( true ); >;

    CreateInputTexture2D( Normal, Linear, 8, "NormalizeNormals", "_normal", "Material,10/30", Default3( 0.5, 0.5, 1.0 ) );
	Texture2D g_tNormal < Channel( RGB, Box( Normal ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;
	float NormalStrength < UiType( Slider ); Default( 1.0f ); Range ( 0, 50.0 ); UiGroup( "Material,10/30"); >; 

	CreateInputTexture2D( Roughness, Linear, 8, "", "_rough", "Material,10/40", Default( 1 ) );
	CreateInputTexture2D( Metalness, Linear, 8, "", "_metal",  "Material,10/50", Default( 1.0 ) );
	CreateInputTexture2D( AmbientOcclusion, Linear, 8, "", "_ao",  "Material,10/60", Default( 1.0 ) );
	float AmbientOcclusionStrength < UiType( Slider ); Default( 1.0f ); Range( 0, 10.0 ); UiGroup( "Material,10/60" ); >;

	Texture2D g_tRmo < Channel( R, Box( Roughness ), Linear ); Channel( G, Box( Metalness ), Linear ); Channel( B, Box( AmbientOcclusion ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;

	#if ( S_EMISSIVE )
		float EmissionStrength < UiType( Slider ); Default( 1.0f ); Range( 0, 5.0 ); UiGroup( "Emission,20/10" ); >;

		CreateInputTexture2D( Emission, Linear, 8, "", "", "Emission,20/20", Default3( 0, 0, 0 ) );
		Texture2D g_tEmission < Channel( RGB, Box( Emission ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
	#endif 

    #include "sbox_pixel.fxc"
    #include "common/pixel.hlsl"
    
	#if ( S_TRANSPARENCY )
		#if( !F_RENDER_BACKFACES )
			#define BLEND_MODE_ALREADY_SET
			RenderState( BlendEnable, true );
			RenderState( SrcBlend, SRC_ALPHA );
			RenderState( DstBlend, INV_SRC_ALPHA);
		#endif

		BoolAttribute( translucent, true );

		CreateInputTexture2D( TransparencyMask, Linear, 8, "", "_trans", "Transparency,30/10", Default( 1 ) );
		Texture2D g_tTransparencyMask < Channel( R, Box( TransparencyMask ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
		float TransparencyRounding< Default( 0.0f ); Range( 0.0f, 1.0f ); UiGroup( "Transparency,30/20" ); >;
	#endif

	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );

	#if ( S_MODE_DEPTH )
        #define MainPs Disabled
    #endif

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float2 UV = i.vTextureCoords.xy;
		float4 l_tColorMap = g_tColor.Sample( g_sPointWrap, UV ).rgba;

        Material m = Material::Init();

		m.Albedo = lerp( l_tColorMap.rgb, l_tColorMap.rgb * g_flColorTint, l_tColorMap.a );  

		float3 l_tNormalMap = DecodeNormal( g_tNormal.Sample( g_sAniso, UV ).rgb );
		m.Normal = TransformNormal( float3( l_tNormalMap.rg * NormalStrength, l_tNormalMap.b ), i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		float3 rmo = g_tRmo.Sample( g_sAniso, UV ).rgb;
        m.Roughness = rmo.r;
        m.Metalness = rmo.g;
        m.AmbientOcclusion = rmo.b / AmbientOcclusionStrength;
        m.TintMask = l_tColorMap.a;
        m.Opacity = 1;
		m.Emission = 0;
		#if( S_EMISSIVE )
       	 	m.Emission = g_tEmission.Sample( g_sAniso, UV ).rgb * EmissionStrength;
		#endif
        m.Transmission = 0;

		float4 result = ShadingModelStandard::Shade( i, m );
		#if( S_TRANSPARENCY )
			float alpha = g_tTransparencyMask.Sample( g_sPointWrap, UV ).r;
			result.a = max( alpha, floor( alpha + TransparencyRounding ) );
		#endif

		return result;
	}
}