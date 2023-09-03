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
    VrForward();
	Depth( "depth_only.vfx" );
	ToolsVis( S_MODE_TOOLS_VIS );
	ToolsShadingComplexity( "vr_tools_shading_complexity.vfx" );
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
    float2 vTexCoord : TEXCOORD9;	
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

    static const float2 uvTable[6][8] = 
    {
        // +z, correct
        {
            float2( 1, 1 ),
            float2( 0, 1 ),
            float2( 0, 0 ),
            float2( 1, 0 ),
            float2( 0, 0 ),
            float2( 0, 0 ),
            float2( 0, 0 ),
            float2( 0, 0 )
        },

        // -z, correct
        {
            float2( 1, 0 ),
            float2( 1, 0 ),
            float2( 1, 0 ),
            float2( 1, 0 ),
            float2( 1, 1 ),
            float2( 2, 1 ),
            float2( 2, 0 ),
            float2( 1, 0 )
        },

        // -x, correct
        {
            float2( 1, 1 ),
            float2( 0, 1 ),
            float2( 0, 1 ),
            float2( 0, 1 ),
            float2( 1, 2 ),
            float2( 0, 2 ),
            float2( 0, 1 ),
            float2( 0, 1 )
        },

        // +y, correct
        {
            float2( 1, 1 ),
            float2( 2, 1 ),
            float2( 1, 1 ),
            float2( 1, 1 ),
            float2( 1, 1 ),
            float2( 2, 2 ),
            float2( 1, 2 ),
            float2( 1, 1 )
        },

        // +x, correct
        {
            float2( 2, 1 ),
            float2( 2, 1 ),
            float2( 3, 1 ),
            float2( 2, 1 ),
            float2( 2, 1 ),
            float2( 2, 1 ),
            float2( 3, 2 ),
            float2( 2, 2 )
        },

        // -y, correct
        {
            float2( 3, 1 ),
            float2( 3, 1 ),
            float2( 3, 1 ),
            float2( 4, 1 ),
            float2( 3, 2 ),
            float2( 3, 1 ),
            float2( 3, 1 ),
            float2( 4, 2 )
        },
    };

    float2 getTexturePos( uint textureIndex, uint face, uint vertexIndex )
    {
        float2 vertexOffset = uvTable[face][vertexIndex];

        float x = (textureIndex * 4 + vertexOffset.x) * g_vTextureSize.x;
        float y = vertexOffset.y * g_vTextureSize.y;

        return float2( x, y );
    }

    PixelInput MainVs( INSTANCED_SHADER_PARAMS( VertexInput i ) )
	{
        // Turn our uint32s back to the actual data.
        int3 position = int3(i.vData.x & 0xF, (i.vData.x >> 4) & 0xF, (i.vData.x >> 8) & 0xF);

        uint textureIndex = (i.vData.x >> 20) & 0xFFF;
        uint vertexIndex = (i.vData.x >> 17) & 0x7;

        float ao = pow(0.75, (i.vData.x >> 15) & 0x3);

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
        o.vTexCoord = getTexturePos( textureIndex, face, vertexIndex ) / g_vAtlasSize.xy;
        o.vColor = color * faceMultipliers[face];

        return FinalizeVertex( o );
    }
}

PS
{
    #include "common/pixel.hlsl"

    CreateTexture2D( g_tAlbedo ) < Attribute( "Albedo" ); SrgbRead( true ); Filter( POINT ); AddressU( CLAMP ); AddressV( CLAMP ); > ;    
    CreateTexture2D( g_tRAE ) < Attribute( "RAE" ); SrgbRead( false ); Filter( POINT ); AddressU( CLAMP ); AddressV( CLAMP ); > ;    

    RenderState( CullMode, DEFAULT );

    float4 MainPs( PixelInput i ) : SV_Target0
	{   
        float3 albedo = Tex2D( g_tAlbedo, i.vTexCoord.xy ).rgb;
        float3 rae = Tex2D( g_tRAE, i.vTexCoord.xy ).rgb;

        Material m;
        m.Albedo = albedo.rgb * i.vColor.rgb * i.fOcclusion;
        m.Normal = 1;
        m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = 0;
		m.Transmission = 1;

        float4 result = ShadingModelStandard::Shade( i, m );
        return result;
    }
}