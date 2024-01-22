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
    Depth( "depth_only.shader" ); 
	ToolsVis( S_MODE_TOOLS_VIS );
    ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
    #include "common/shared.hlsl"

    float3 g_vVoxelScale < Attribute( "VoxelScale" ); Default3( 1.0, 1.0, 1.0 ); >;
    float2 g_vTextureSize < Attribute( "TextureSize" ); Default2( 32.0, 32.0 ); >;
    float2 g_vAtlasSize < Attribute( "AtlasSize" ); Default2( 32.0, 32.0 ); >;
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"

    uint2 vData : TEXCOORD10 < Semantic( None ); >;
};


struct PixelInput
{
	#include "common/pixelinput.hlsl"

    float3 vNormal : TEXCOORD15;
    float fOcclusion : TEXCOORD14;
    float3 vTexCoord : TEXCOORD9;	
	float4 vColor : TEXCOORD13;
};

VS
{
	#include "common/vertex.hlsl"

    static const float3 offsetTable[8] =
    {
        float3( -0.5f, -0.5f, 0.5f ),
		float3( -0.5f, 0.5f, 0.5f ),
		float3( 0.5f, 0.5f, 0.5f ),
		float3( 0.5f, -0.5f, 0.5f ),
		float3( -0.5f, -0.5f, -0.5f ),
		float3( -0.5f, 0.5f, -0.5f ),
		float3( 0.5f, 0.5f, -0.5f ),
		float3( 0.5f, -0.5f, -0.5f )
    };

    static const float faceMultipliers[6] = 
    {
        1.0f, 1.0f,
		0.85f, 0.7f,
		0.85f, 0.7f
    };

    static const int2 uvTable[6][8] = 
    {
        // +z, correct
        {
            int2( 1, 1 ),
            int2( 0, 1 ),
            int2( 0, 0 ),
            int2( 1, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 )
        },

        // -z, correct
        {
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 0, 1 ),
            int2( 1, 1 ),
            int2( 1, 0 ),
            int2( 0, 0 )
        },

        // -x, correct
        {
            int2( 1, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 1, 1 ),
            int2( 0, 1 ),
            int2( 0, 0 ),
            int2( 0, 0 )
        },

        // +y, correct
        {
            int2( 0, 0 ),
            int2( 1, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 1, 1 ),
            int2( 0, 1 ),
            int2( 0, 0 )
        },

        // +x, correct
        {
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 1, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 1, 1 ),
            int2( 0, 1 )
        },

        // -y, correct
        {
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 1, 0 ),
            int2( 0, 1 ),
            int2( 0, 0 ),
            int2( 0, 0 ),
            int2( 1, 1 )
        },
    };

    PixelInput MainVs( VertexInput i )
	{
        // Turn our 32-bit unsigned integers back to the actual data.
        int3 position = int3( i.vData.x & 0xF, (i.vData.x >> 4) & 0xF, (i.vData.x >> 8) & 0xF );

        uint textureIndex = (i.vData.x >> 20) & 0xFFF;
        uint vertexIndex = (i.vData.x >> 17) & 0x7;

        float ao = pow( 0.75, (i.vData.x >> 15) & 0x3 );

        float3 normal = float3( 0, 0, 0 );
        uint face = (i.vData.x >> 12) & 0x7;
        if ( face == 0 ) normal = float3( 0, 0, 1 );
        else if ( face == 1 ) normal = float3( 0, 0, -1 );
        else if ( face == 2 ) normal = float3( -1, 0, 0 );
        else if ( face == 3 ) normal = float3( 0, 1, 0 );
        else if ( face == 4 ) normal = float3( 1, 0, 0 );
        else if ( face == 5 ) normal = float3( 0, -1, 0 );

        float4 color = float4( 
            (i.vData.y & 0xFFu),
            ((i.vData.y >> 8) & 0xFFu),
            ((i.vData.y >> 16) & 0xFFu),
            ((i.vData.y >> 24) & 0xFFu)) / 255.0f;

        // Set object space position.
        i.vPositionOs = (position + offsetTable[vertexIndex]) * g_vVoxelScale;

        // Set our output data.
        PixelInput o = ProcessVertex( i );
        o.vPositionWs = i.vPositionOs;
        o.vNormal = normal;
        o.fOcclusion = ao;
        o.vTexCoord = float3( uvTable[face][vertexIndex].xy, textureIndex * 6 + face );
        o.vColor = color * faceMultipliers[face];

        return FinalizeVertex( o );
    }
}

PS
{
    #include "common/pixel.hlsl"

    CreateTexture2DArray( g_tAlbedo ) < Attribute( "Albedo" ); SrgbRead( true ); Filter( MIN_MAG_MIP_POINT ); AddressU( CLAMP ); AddressV( CLAMP ); > ;    
    CreateTexture2DArray( g_tRAE ) < Attribute( "RAE" ); SrgbRead( false ); Filter( MIN_MAG_MIP_POINT ); AddressU( CLAMP ); AddressV( CLAMP ); > ;    

    SamplerState g_sSampler < Filter( POINT ); AddressU( CLAMP ); AddressV( CLAMP ); >;

    RenderState( CullMode, DEFAULT );

    float4 MainPs( PixelInput i ) : SV_Target0
	{   
        float3 albedo = Tex2DArrayS( g_tAlbedo, g_sSampler, i.vTexCoord.xyz ).rgb;
        float3 rae = Tex2DArrayS( g_tRAE, g_sSampler, i.vTexCoord.xyz ).rgb;

        Material m = Material::Init();
        m.Albedo = albedo.rgb * i.vColor.rgb * i.fOcclusion;
        m.Normal = 1;
        m.Roughness = rae.r;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = rae.g; // rae.g
		m.Emission = rae.b;
		m.Transmission = 1;

        float4 result = ShadingModelStandard::Shade( i, m );
        return result;
    }
}