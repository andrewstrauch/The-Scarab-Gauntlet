texture baseTexture;

sampler BaseTextureSampler = sampler_state
{
    Texture = <baseTexture>;
    MipFilter = POINT;
    MinFilter = POINT;
    MagFilter = POINT;
};

float bloomThreshold;

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
    float4 c = tex2D(BaseTextureSampler, input.texCoord);
    return saturate((c - bloomThreshold) / (1 - bloomThreshold));
}

technique BloomExtract
{
    pass Pass1
    {
        VertexShader = compile vs_1_1 VertexShader();
        PixelShader = compile ps_2_0 PixelShader();
    }
}
