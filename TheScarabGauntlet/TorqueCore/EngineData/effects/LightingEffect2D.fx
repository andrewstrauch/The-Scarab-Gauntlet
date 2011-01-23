//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

float4x4 _worldMatrix;
float4x4 _worldViewProjectionMatrix;

texture _baseTexture;
texture _normalMap;

float3 _cameraPosition;
float3 _specularColor;
float _specularPower;
float _specularIntensity;

float _opacity;

int _lightCount;
float3 _lightPosition[8];
float3 _lightDiffuse[8];
float3 _lightAmbient[8];
float2 _lightAttenuation[8];

sampler BaseTextureSampler = sampler_state
{
    Texture = <_baseTexture>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

sampler NormalMapSampler = sampler_state
{
    Texture = <_normalMap>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

//-----------------------------------------------------------------------------
// No Lighting
//-----------------------------------------------------------------------------

struct NoLightingInput
{
    float4 Position : POSITION;
    float2 TexCoord : TEXCOORD;
};

struct NoLightingOutput
{
    float4 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
};

NoLightingOutput NoLightingVS(NoLightingInput input)
{
    NoLightingOutput output;
   
    output.Position = mul(input.Position, _worldViewProjectionMatrix);
    output.TexCoord = input.TexCoord;
    
    return output;
}

float4 NoLightingPS(NoLightingOutput input) : COLOR
{
	float4 color = tex2D(BaseTextureSampler, input.TexCoord);
	color *= _opacity;
	return color;
}

technique NoLightingTechnique
{
	pass P0
	{
	    VertexShader = compile vs_1_1 NoLightingVS();
	    PixelShader  = compile ps_1_1 NoLightingPS();
	}
}

//-----------------------------------------------------------------------------
// Per Vertex Lighting
//-----------------------------------------------------------------------------

struct VertexLightingInput
{
    float4 Position : POSITION;
    float2 TexCoord : TEXCOORD;
};

struct VertexLightingOutput
{
    float4 Position     : POSITION;
    float3 LightDiffuse : COLOR0;
    float3 LightAmbient : COLOR1;
    float2 TexCoord     : TEXCOORD0;
    float2 TexCoord2    : TEXCOORD1;
    float3 LightDir     : TEXCOORD2;
    float3 WorldPos     : TEXCOORD3;
    float3 CamPos       : TEXCOORD4;
};

VertexLightingOutput VertexLightingVS(VertexLightingInput input, uniform int maxLights, uniform bool doNormalMap)
{
    VertexLightingOutput output;
    
    output.Position = mul(input.Position, _worldViewProjectionMatrix);
    float4 worldPosition = mul(input.Position, _worldMatrix);
    float3 normal = float3(0.0, 0.0, 1.0);
    
    output.TexCoord = input.TexCoord;
    output.TexCoord2 = input.TexCoord;
    
    float3 lightDirection = 0.0;
	float3 lightDiffuse = 0.0;
	float3 lightAmbient = 0.0;
	
	for (int i = 0; i < maxLights; i++)
	{
		if (i < _lightCount)
		{
			float3 lightVector = _lightPosition[i] - worldPosition;
			float distance = length(lightVector);
			lightVector /= distance;
			
			float attenuation = 1.0f / (_lightAttenuation[i].x + (_lightAttenuation[i].y * distance));
			
			lightDirection += lightVector * attenuation;
			
			float lightAmount = saturate(dot(normal, lightVector));
			lightDiffuse += _lightDiffuse[i] * lightAmount * attenuation;
			lightAmbient += _lightAmbient[i] * attenuation;
		}
	}
	
	output.LightDiffuse = lightDiffuse;
	output.LightAmbient = lightAmbient;
	output.LightDir = normalize(lightDirection) * 0.5 + 0.5;
	output.CamPos = _cameraPosition;
	output.WorldPos = worldPosition;
    
    return output;
}

float4 VertexLightingPS(VertexLightingOutput input, uniform bool doNormalMap, uniform bool doSpecular) : COLOR
{
	float4 color = tex2D(BaseTextureSampler, input.TexCoord);
	
	float3 normal;
	float specularScalar;
	if (doNormalMap)
	{
		float4 normalMap = tex2D(NormalMapSampler, input.TexCoord2);
		normal = normalMap.xyz * 2.0 - 1.0;
		specularScalar = normalMap.w * _specularIntensity;
	}
	else
	{
	    normal = float3(0.0, 0.0, 1.0);
	    specularScalar = _specularIntensity;
	}
	
	float lightAmount;
	float3 lightDir = input.LightDir * 2.0 - 1.0;
	if (doNormalMap)
		lightAmount = saturate(dot(normal, lightDir));
	else
		lightAmount = 1.0;
	
	color.xyz *= (input.LightDiffuse * lightAmount) + input.LightAmbient;
    color.a *= _opacity;
	
	if (doSpecular)
	{
		float3 reflectVec = reflect(-lightDir, normal);
		float3 cameraDir = normalize(input.CamPos - input.WorldPos);
		color.xyz += _specularColor * pow(saturate(dot(reflectVec, cameraDir)), _specularPower) * specularScalar;
	}
    
    return color;
}

technique VertexLightingTechnique
{
    pass P0
    {
        VertexShader = compile vs_1_1 VertexLightingVS(4, false);
        PixelShader  = compile ps_1_1 VertexLightingPS(false, false);
    }
}

technique VertexLightingNormalMapTechnique
{
    pass P0
    {
        VertexShader = compile vs_1_1 VertexLightingVS(4, true);
        PixelShader  = compile ps_1_1 VertexLightingPS(true, false);
    }
}

technique VertexLightingSpecularTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 VertexLightingVS(8, false);
        PixelShader  = compile ps_2_0 VertexLightingPS(false, true);
    }
}

technique VertexLightingSpecularNormalMapTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 VertexLightingVS(8, true);
        PixelShader  = compile ps_2_0 VertexLightingPS(true, true);
    }
}

//-----------------------------------------------------------------------------
// Per Pixel Lighting
//-----------------------------------------------------------------------------

struct PixelLightingInput
{
    float4 Position   : POSITION;
    float2 TexCoord   : TEXCOORD;
};

struct PixelLightingOutput
{
    float4 Position   : POSITION;
    float2 TexCoord   : TEXCOORD0;
    float3 WorldPos   : TEXCOORD1;
};

PixelLightingOutput PixelLightingVS(PixelLightingInput input, uniform bool doNormalMap)
{
    PixelLightingOutput output;
    
    output.Position = mul(input.Position, _worldViewProjectionMatrix);
    output.TexCoord = input.TexCoord;
    
    output.WorldPos.xy = mul(input.Position, _worldMatrix).xy;
    output.WorldPos.z = 0.0;

    return output;
}

float4 PixelLightingPS(PixelLightingOutput input, uniform int maxLights, uniform bool doNormalMap, uniform bool doSpecular) : COLOR
{
	float4 color = tex2D(BaseTextureSampler, input.TexCoord);
	
    float3 normal;
    float specularScalar;
    
    if (doNormalMap)
    {
		float4 normalMap = tex2D(NormalMapSampler, input.TexCoord);
		normal = normalMap.xyz * 2.0 - 1.0;
		specularScalar = normalMap.w * _specularIntensity;
	}
	else
	{
		normal = float3(0.0, 0.0, 1.0);
		specularScalar = _specularIntensity;
	}
	
    float3 lightDirection = 0.0;
	float3 lightColor = 0.0;
	
	for (int i = 0; i < maxLights; i++)
	{
		if (i < _lightCount)
		{
			float3 lightVector = _lightPosition[i] - input.WorldPos;
			float distance = length(lightVector);
			lightVector /= distance;
			
			float attenuation = 1.0f / (_lightAttenuation[i].x + (_lightAttenuation[i].y * distance));
			
			if (doSpecular)
				lightDirection += lightVector * attenuation;
			
			float lightAmount = saturate(dot(normal, lightVector));
			lightColor += ((_lightDiffuse[i] * lightAmount) + _lightAmbient[i]) * attenuation;
		}
	}

	color.xyz *= lightColor;
    color.a *= _opacity;

    if (doSpecular)
    {
		float3 reflectVec = reflect(-normalize(lightDirection), normal);
		float3 cameraDir = normalize(_cameraPosition - input.WorldPos);
		color.xyz += _specularColor * pow(saturate(dot(reflectVec, cameraDir)), _specularPower) * specularScalar;
    }
	
    return color;
}

technique PixelLightingTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 PixelLightingVS(false);
        PixelShader  = compile ps_2_0 PixelLightingPS(4, false, false);
    }
}

technique PixelLightingSpecularTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 PixelLightingVS(false);
        PixelShader  = compile ps_2_0 PixelLightingPS(3, false, true);
    }
}

technique PixelLightingNormalMapTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 PixelLightingVS(true);
        PixelShader  = compile ps_2_0 PixelLightingPS(4, true, false);
    }
}

technique PixelLightingSpecularNormalMapTechnique
{
    pass P0
    {
        VertexShader = compile vs_2_0 PixelLightingVS(true);
        PixelShader  = compile ps_2_0 PixelLightingPS(3, true, true);
    }
}

technique PixelLightingTechnique3_0
{
    pass P0
    {
        VertexShader = compile vs_3_0 PixelLightingVS(false);
        PixelShader  = compile ps_3_0 PixelLightingPS(8, false, false);
    }
}

technique PixelLightingSpecularTechnique3_0
{
    pass P0
    {
        VertexShader = compile vs_3_0 PixelLightingVS(false);
        PixelShader  = compile ps_3_0 PixelLightingPS(8, false, true);
    }
}

technique PixelLightingNormalMapTechnique3_0
{
    pass P0
    {
        VertexShader = compile vs_3_0 PixelLightingVS(true);
        PixelShader  = compile ps_3_0 PixelLightingPS(8, true, false);
    }
}

technique PixelLightingSpecularNormalMapTechnique3_0
{
    pass P0
    {
        VertexShader = compile vs_3_0 PixelLightingVS(true);
        PixelShader  = compile ps_3_0 PixelLightingPS(8, true, true);
    }
}
