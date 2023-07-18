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

    //StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );

    RenderState( CullMode, DEFAULT );

    float4 MainPs( PixelInput i ) : SV_Target0
	{
        //float3 normal = i.vNormalWs.xyz;
        //float3 voxel = i.vColor.rgb;
        //float3 dir = (cross(normal.xyz, voxel) + 1) * 0.05f;

	    //return float4(voxel + dir, 1);

        Material m;
		m.Albedo = i.vColor.rgb;
		m.Normal = i.vNormalWs.xyz;
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
        
        return ShadingModelStandard::Shade( i, m );
    }
}