HEADER
{
	Description = "Voxel Shader";
}

FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
    Default();
    VrForward();
    ToolsVis( S_MODE_TOOLS_VIS );
    Depth( "depth_only.shader" );
}

COMMON
{
    #include "common/shared.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"

    float4 vColor : COLOR0 < Semantic( Color ); >;
};


struct PixelInput
{
	#include "common/pixelinput.hlsl"

    float4 vColor : COLOR0;
};

VS
{
	#include "common/vertex.hlsl"

    PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
        o.vColor = i.vColor;

        return FinalizeVertex( o );
    }
}

PS
{
    #include "common/pixel.hlsl"

    RenderState( CullMode, DEFAULT );

    float4 MainPs( PixelInput i ) : SV_Target0
	{   
        /*Material m;
        m.Albedo = i.vColor.rgb;
        m.Normal = 1;
        m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1.0f;
		m.Emission = 0;
		m.Transmission = 1;

        float4 result = ShadingModelStandard::Shade( i, m );
        return result;*/
        return float4( i.vColor.rgb, 1 );
    }
}