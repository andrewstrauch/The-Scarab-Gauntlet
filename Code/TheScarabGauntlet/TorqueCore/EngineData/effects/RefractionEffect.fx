//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

float4x4 worldViewProjection;

float4 refractionViewport = (0.5f, 0.5f, 0.5f, 0.5f);
float4 refractionUVBounds = (0.0f, 0.0f, 1.0f, 1.0f);
float refractionAmount = 0.03f;

texture refractedTexture;
texture normalMap;

struct VSInput
{
    float4 position   : POSITION;
    float2 texCoord   : TEXCOORD;
};

struct VSOutput
{
    float4 position   : POSITION;     
    float2 texCoord   : TEXCOORD0;
    float2 texCoord2  : TEXCOORD1;
};

sampler RefractedTextureSampler = sampler_state
{
    Texture = <refractedTexture>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};
sampler NormalMapSampler = sampler_state
{
    Texture = <normalMap>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

VSOutput VertexShader(VSInput input)
{
    VSOutput output;
    
    output.position = mul(input.position, worldViewProjection);

    output.texCoord.x = (output.position.x / output.position.w * refractionViewport.x) + refractionViewport.z;
    output.texCoord.y = (-output.position.y / output.position.w * refractionViewport.y) + refractionViewport.w;
    output.texCoord2 = input.texCoord;
    
    return output;
}

float4 PixelShader(VSOutput input) : COLOR
{
    float4 bumpNormal = tex2D(NormalMapSampler, input.texCoord2);
    bumpNormal.xyz = (bumpNormal.xyz * 2.0 - 1.0) * refractionAmount;

    float2 uv = clamp(input.texCoord.xy + bumpNormal.xy, refractionUVBounds.xy, refractionUVBounds.zw);
    float4 color = tex2D(RefractedTextureSampler, uv);
    color.w = bumpNormal.w;
    
    return color;
}

float4 PixelShader_1_1(VSOutput input) : COLOR
{
    return tex2D(RefractedTextureSampler, input.texCoord);
}

technique RefractionTechnique_1_1
{
    pass P0
    {
        VertexShader = compile vs_1_1 VertexShader();
        PixelShader  = compile ps_1_1 PixelShader_1_1(); 
    }
}

technique RefractionTechnique
{
    pass P0
    {
        VertexShader = compile vs_1_1 VertexShader();
        PixelShader  = compile ps_1_4 PixelShader(); 
    }
}
