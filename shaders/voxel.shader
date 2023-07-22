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
        return float4( i.vColor.rgb, 1 );
    }
}