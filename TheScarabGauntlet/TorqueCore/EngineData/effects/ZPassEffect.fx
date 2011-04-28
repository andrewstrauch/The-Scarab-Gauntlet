//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

float4x4 worldViewProjection;
float opacity = 1.0;

texture baseTexture;

sampler2D baseTextureSampler = sampler_state
{
	Texture = <baseTexture>;
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
};

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

VSOutput ZPassVS(VSInput input)
{
	VSOutput output;
	output.position = mul(input.position, worldViewProjection);
	output.texCoord = input.texCoord;
	return output;
}

float4 ZPassPS(VSOutput input) : COLOR
{
	float4 color = tex2D(baseTextureSampler, input.texCoord);
	color.a *= opacity;
	
    return color;
}

technique ZPassTechnique
{
    pass P0
    {
        VertexShader = compile vs_1_1 ZPassVS();
        PixelShader  = compile ps_1_1 ZPassPS(); 
    }
}
