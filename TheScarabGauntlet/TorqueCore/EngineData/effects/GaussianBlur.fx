texture baseTexture;

sampler BaseTextureSampler = sampler_state
{
    Texture = <baseTexture>;
    MipFilter = POINT;
    MinFilter = POINT;
    MagFilter = POINT;
};

#define SAMPLE_COUNT 15

float2 sampleOffsets[SAMPLE_COUNT];
float sampleWeights[SAMPLE_COUNT];

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
    float4 color = 0;
    
    for (int i = 0; i < SAMPLE_COUNT; i++)
        color += tex2D(BaseTextureSampler, input.texCoord + sampleOffsets[i]) * sampleWeights[i];
    
    return color;
}

technique GaussianBlur
{
    pass Pass1
    {
        VertexShader = compile vs_1_1 VertexShader();
        PixelShader = compile ps_2_0 PixelShader();
    }
}
