texture baseTexture;

sampler BaseTextureSampler = sampler_state
{
    Texture = <baseTexture>;
    MipFilter = POINT;
    MinFilter = POINT;
    MagFilter = POINT;
};

texture baseTexture2;

sampler BaseTexture2Sampler = sampler_state
{
    Texture = <baseTexture2>;
    MipFilter = POINT;
    MinFilter = POINT;
    MagFilter = POINT;
};

float BloomIntensity;
float BaseIntensity;

float BloomSaturation;
float BaseSaturation;

float4 AdjustSaturation(float4 color, float saturation)
{
    float grey = dot(color, float3(0.3, 0.59, 0.11));
    return lerp(grey, color, saturation);
}

struct VSInput 
{
    float4 position : POSITION;
    float2 texCoord : TEXCOORD0;
};

struct VSOutput 
{
    float4 position : POSITION;
    float2 texCoord : TEXCOORD0;
};

VSOutput VertexShader(VSInput input)
{
    VSOutput output;
    output.position = input.position;
    output.texCoord = input.texCoord;
    return output;
}

float4 PixelShader(VSOutput input) : COLOR0
{
    float4 bloom = tex2D(BaseTexture2Sampler, input.texCoord);
    float4 base = tex2D(BaseTextureSampler, input.texCoord);
    
    bloom = AdjustSaturation(bloom, BloomSaturation) * BloomIntensity;
    base = AdjustSaturation(base, BaseSaturation) * BaseIntensity;
    
    base *= (1 - saturate(bloom));
    
    return base + bloom;
}

technique BloomCombine
{
    pass Pass1
    {
        VertexShader = compile vs_1_1 VertexShader();
        PixelShader = compile ps_2_0 PixelShader();
    }
}
