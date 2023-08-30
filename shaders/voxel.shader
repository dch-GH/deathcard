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

    float g_flVoxelScale < Attribute( "VoxelScale" ); Default( 32.0 ); >;
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"

    uint2 vData : TEXCOORD10 < Semantic( None ); >;
};


struct PixelInput
{
	#include "common/pixelinput.hlsl"

    int3 vPositionOs : TEXCOORD11;
    float3 vNormal : TEXCOORD15;
    float fOcclusion : TEXCOORD14;
    float2 vTexCoord : TEXCOORD9;	
	float4 vColor : TEXCOORD13;
};

VS
{
	#include "common/vertex.hlsl"

    PixelInput MainVs( INSTANCED_SHADER_PARAMS( VertexInput i ) )
	{
        float3 position = float3(i.vData.x & 0x1F, (i.vData.x >> 5) & 0x1f, (i.vData.x >> 10) & 0x1f);

        uint textureIndex = i.vData.x >> 20;

        float ao = pow(0.75, (i.vData.x >> 18) & 0x3);

        float3 normal = float3( 0, 0, 0 );
        uint face = (i.vData.x >> 15) & 0x7;
        if ( face == 0 ) normal = float3( 0, 0, 1 );
        else if ( face == 1 ) normal = float3( 0, 0, -1 );
        else if ( face == 2 ) normal = float3( -1, 0, 0 );
        else if ( face == 3 ) normal = float3( 0, 1, 0 );
        else if ( face == 4 ) normal = float3( 1, 0, 0 );
        else if ( face == 5 ) normal = float3( 0, -1, 0 );

        float4 color = float4( 
            (i.vData.y >> 24), 
            ((i.vData.y >> 16) & 0xFF), 
            ((i.vData.y >> 8) & 0xFF), 
            (i.vData.y & 0xFF) ) / 255.0f;
        
        PixelInput o = ProcessVertex( i );
        
        o.vPositionOs = position * g_flVoxelScale;
        o.vNormal = normal;
        o.fOcclusion = ao;
        o.vTexCoord = float2( 0, 0 );
        o.vColor = color;

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