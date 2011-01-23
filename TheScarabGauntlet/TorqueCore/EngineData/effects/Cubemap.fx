//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

float4x4 rotation;
float4x4 worldViewProjection;
texture cubeTexture;

samplerCUBE CubeTextureSampler = sampler_state
{
    Texture = <cubeTexture>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

struct VSInput
{
	float4 position : POSITION;
};

struct VSOutput
{
    float4 position : POSITION;
    float3 texCoord : TEXCOORD0;
};

VSOutput CubemapVS(VSInput input)
{
    VSOutput output;
    
    output.position = mul(input.position, worldViewProjection);
    output.texCoord = mul(input.position.xyz, rotation);
    
    return output;
}

float4 CubemapPS(VSOutput input) : COLOR
{
    return texCUBE(CubeTextureSampler, input.texCoord);
}

technique CubemapReflection
{
    pass P0
    {
        VertexShader = compile vs_1_1 CubemapVS();
        PixelShader  = compile ps_2_0 CubemapPS(); 
    }
}
