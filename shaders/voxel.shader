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
        float3 normal = i.vNormalWs.xyz;
        float3 voxel = i.vColor.rgb;
        float3 dir = (cross(normal.xyz, voxel) + 1) * 0.05f;

	    return float4(voxel + dir, 1);
    }
}