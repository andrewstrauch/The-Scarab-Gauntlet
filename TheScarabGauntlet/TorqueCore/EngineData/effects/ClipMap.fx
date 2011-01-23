//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

// fade constant used in clip map level fade calculations
#define fadeConstant 30

//------------------------------------------------------------
// Input parameters
//------------------------------------------------------------

// from base effect:
float4x4 worldViewProjection;

// from clipmap
float4 mapInfo[4];

texture clipLevel1;
texture clipLevel2;
texture clipLevel3;
texture clipLevel4;

float4x4 worldMatrix;
int lightCount;
float3 lightPosition[8];
float3 lightDiffuse[80];
float3 lightAmbient[8];
float2 lightAttenuation[8];

// samplers for the 4 textures we're using
uniform sampler2D diffuseMap[4] = 
{
	sampler_state
	{
		Texture = <clipLevel1>;
		MipFilter = LINEAR;
		MinFilter = LINEAR;
		MagFilter = LINEAR;
	},
	sampler_state
	{
		Texture = <clipLevel2>;
		MipFilter = LINEAR;
		MinFilter = LINEAR;
		MagFilter = LINEAR;
	},
	sampler_state
	{
		Texture = <clipLevel3>;
		MipFilter = LINEAR;
		MinFilter = LINEAR;
		MagFilter = LINEAR;
	},
	sampler_state
	{
		Texture = <clipLevel4>;
		MipFilter = LINEAR;
		MinFilter = LINEAR;
		MagFilter = LINEAR;
	}
};

//------------------------------------------------------------
// IO structs for shader model 1
//------------------------------------------------------------

// VS 1 Input
struct VertData1
{
    float4 position			 : POSITION;
    float2 texCoord			 : TEXCOORD;
};

// VS 1 Output for 4 base textures
struct VertClipMapConnectData1
{
   float4 position           : POSITION0;
   float4 color              : COLOR;
   float2 texCoord[4]		 : TEXCOORD0;
};

// PS 1 Iutput for 4 base textures
struct PixClipMapConnectData1
{
   float4 color              : COLOR;
   float2 texCoord[4]		 : TEXCOORD0;
};

// PS Output
struct FragOut
{
   float4 color				 : COLOR;
};

//------------------------------------------------------------
// Clip map level blending for shader model 1
//------------------------------------------------------------

// Vertex Shader ---------------------------
VertClipMapConnectData1 VertClipMap1(VertData1 IN, uniform int levelsUsed)
{
	VertClipMapConnectData1 OUT;
	
	// Initialize OUT to some dummy values for the Xbox compiler
	OUT.position = float4(0.0f, 0.0f, 0.0f, 0.0f);
	OUT.color = float4(0.0f, 0.0f, 0.0f, 0.0f);
	OUT.texCoord[0] = float2(0.0f, 0.0f);
	OUT.texCoord[1] = float2(0.0f, 0.0f);
	OUT.texCoord[2] = float2(0.0f, 0.0f);
	OUT.texCoord[3] = float2(0.0f, 0.0f);
    
	// Do vertex transform...
	OUT.position = mul(IN.position, worldViewProjection);

	// Scale texcoords.
	int i=0;
	for(; i<levelsUsed; i++)
		OUT.texCoord[i] = IN.texCoord * mapInfo[i].z;
	for(; i<4; i++) // fill unused with 0
		OUT.texCoord[i] = 0.0f;

	// Do all the fade biasing in one go.
	i = 0;
	for(; i<levelsUsed; i++)
		OUT.color[i] = distance(mapInfo[i].xy, OUT.texCoord[i]) *  (2 * fadeConstant) - (fadeConstant - 1.0) / 1.3;
	for(; i<4; i++) // fill unused with 0
		OUT.color[i] = 0.0f;

	return OUT;
}

// Pixel Shader ---------------------------
FragOut PixClipMap1(PixClipMapConnectData1 IN, uniform int levelsUsed)
{
	FragOut OUT;

	// Do a layered blend into accumulator, so most detail when we have it will show through...
	OUT.color = tex2D(diffuseMap[0], IN.texCoord[0].xy);
	
	for(int i=1; i<levelsUsed; i++)
	{
		float  scaleFactor = saturate(IN.color[i]);
		float4 layer = tex2D(diffuseMap[i], IN.texCoord[i].xy);
		OUT.color = lerp(layer, OUT.color, scaleFactor);
	}

   return OUT;
}

//------------------------------------------------------------
// SM 1 Techniques
//------------------------------------------------------------

// fade between 4 base textures
technique ClipMap4_1
{
	pass Pass0
	{
		VertexShader = compile vs_1_1 VertClipMap1(4);
		PixelShader = compile ps_1_1 PixClipMap1(4);
	}
}

// fade between 3 base textures
technique ClipMap3_1
{
	pass Pass0
	{
		VertexShader = compile vs_1_1 VertClipMap1(3);
		PixelShader = compile ps_1_1 PixClipMap1(3);
	}
}

// fade between 2 clip levels
technique ClipMap2_1
{
	pass Pass0
	{
		VertexShader = compile vs_1_1 VertClipMap1(2);
		PixelShader = compile ps_1_1 PixClipMap1(2);
	}
}

// draw 1 clip level
technique ClipMap1_1
{
	pass Pass0
	{
		VertexShader = compile vs_1_1 VertClipMap1(1);
		PixelShader = compile ps_1_1 PixClipMap1(1);
	}
}

//------------------------------------------------------------
// IO structs for shader model 2
//------------------------------------------------------------

// VS 2 Input
struct VertData2
{
    float4 position			 : POSITION;
    float2 texCoord			 : TEXCOORD;
    float4 normal			 : NORMAL;
};

// VS 2 Output for 4 base textures
struct VertClipMapConnectData2
{
   float4 position           : POSITION0;
   float4 color              : COLOR0;
   float2 texCoord[4]		 : TEXCOORD0;
   float3 lightDiffuse		 : TEXCOORD5;
   float3 lightAmbient		 : TEXCOORD6;
};

// PS 2 Iutput for 4 base textures
struct PixClipMapConnectData2
{
   float4 color              : COLOR;
   float2 texCoord[4]		 : TEXCOORD0;
   float3 lightDiffuse		 : TEXCOORD5;
   float3 lightAmbient		 : TEXCOORD6;
};

//------------------------------------------------------------
// Clip map level blending for shader model 2
//------------------------------------------------------------

// Vertex Shader ---------------------------
VertClipMapConnectData2 VertClipMap2(VertData2 IN, uniform int levelsUsed, uniform bool doLighting, uniform int maxLights)
{
	VertClipMapConnectData2 OUT;

	// Initialize OUT to some dummy values for the Xbox compiler
	OUT.position = float4(0.0f, 0.0f, 0.0f, 0.0f);
	OUT.color = float4(0.0f, 0.0f, 0.0f, 0.0f);
	OUT.texCoord[0] = float2(0.0f, 0.0f);
	OUT.texCoord[1] = float2(0.0f, 0.0f);
	OUT.texCoord[2] = float2(0.0f, 0.0f);
	OUT.texCoord[3] = float2(0.0f, 0.0f);
	OUT.lightDiffuse = float3(0.0f, 0.0f, 0.0f);
	OUT.lightAmbient = float3(0.0f, 0.0f, 0.0f);
    
	// Do vertex transform...
	OUT.position = mul(IN.position, worldViewProjection);

	// Scale texcoords.
	int i=0;
	for(; i<levelsUsed; i++)
		OUT.texCoord[i] = IN.texCoord * mapInfo[i].z;
	for(; i<4; i++) // fill unused with 0
		OUT.texCoord[i] = 0.0f;

	// Do all the fade biasing in one go.
	i = 0;
	for(; i<levelsUsed; i++)
		OUT.color[i] = distance(mapInfo[i].xy, OUT.texCoord[i]) *  (2 * fadeConstant) - (fadeConstant - 1.0) / 1.3;
	for(; i<4; i++) // fill unused with 0
		OUT.color[i] = 0.0f;

	if(doLighting)
	{
		float4 worldPosition = mul(IN.position, worldMatrix);
		float3 normal = mul(IN.normal, worldMatrix);
	
		float3 lightDiffuseTotal = 0.0;
		float3 lightAmbientTotal = 0.0;
		float3 lightDirection = 0.0;
		
		i = 0;
		for (; i < maxLights; i++)
		{
			if(i < lightCount)
			{
				float3 lightVector = lightPosition[i] - worldPosition;
				float distance = length(lightVector);
				lightVector /= distance;
				
				float attenuation = 1.0f / (lightAttenuation[i].x + ((lightAttenuation[i].y) * distance));
			
				lightDirection += lightVector * attenuation;
				
				float lightAmount = saturate(dot(normal, lightVector));
				lightDiffuseTotal += lightDiffuse[i] * lightAmount * attenuation;
				lightAmbientTotal += lightAmbient[i] * attenuation;
			}
		}
	
		OUT.lightDiffuse = lightDiffuseTotal;
		OUT.lightAmbient = lightAmbientTotal;
	}
	else
	{
		OUT.lightDiffuse = 0;
		OUT.lightAmbient = 0;
	}
	

	return OUT;
}

// Pixel Shader ---------------------------
FragOut PixClipMap2(PixClipMapConnectData2 IN, uniform int levelsUsed, uniform bool doLighting)
{
	FragOut OUT;

	// Do a layered blend into accumulator, so most detail when we have it will show through...
	OUT.color = tex2D(diffuseMap[0], IN.texCoord[0].xy);
	
	for(int i=1; i<levelsUsed; i++)
	{
		float  scaleFactor = saturate(IN.color[i]);
		float4 layer = tex2D(diffuseMap[i], IN.texCoord[i].xy);
		OUT.color = lerp(layer, OUT.color, scaleFactor);
	}

	if(doLighting)
		OUT.color.xyz *= IN.lightDiffuse + IN.lightAmbient;

    return OUT;
}

//------------------------------------------------------------
// SM 2 Techniques
//------------------------------------------------------------

// WITH LIGHTING ------------------------------------
// fade between 4 base textures
technique ClipMap4_2_lit
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 VertClipMap2(4, true, 8);
		PixelShader = compile ps_2_0 PixClipMap2(4, true);
	}
}

// fade between 3 base textures
technique ClipMap3_2_lit
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 VertClipMap2(3, true, 8);
		PixelShader = compile ps_2_0 PixClipMap2(3, true);
	}
}

// fade between 2 clip levels
technique ClipMap2_2_lit
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 VertClipMap2(2, true, 8);
		PixelShader = compile ps_2_0 PixClipMap2(2, true);
	}
}

// draw 1 clip level
technique ClipMap1_2_lit
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 VertClipMap2(1, true, 8);
		PixelShader = compile ps_2_0 PixClipMap2(1, true);
	}
}