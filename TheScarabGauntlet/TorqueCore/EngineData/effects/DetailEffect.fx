//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

//------------------------------------------------------------
// Input parameters
//------------------------------------------------------------

// from base effect:
float4x4 worldViewProjection;
float4x4 worldMatrix;

texture detailTex;
float detailTexRepeat;
float3 detailCenter;
float detailDistance;

uniform sampler2D detailMap = 
{
	sampler_state
	{
		Texture = <detailTex>;
		MipFilter = LINEAR;
		MinFilter = LINEAR;
		MagFilter = LINEAR;
	}
};

//------------------------------------------------------------
// IO structs
//------------------------------------------------------------

// VS Input
struct VertData
{
    float4 position			 : POSITION;
    float2 texCoord			 : TEXCOORD;
};

// VS Output for 1 base texture & detail pass
struct VertDetailStruct
{
   float4 position           : POSITION0;
   float4 color              : COLOR0;
   float2 texCoord			 : TEXCOORD0;
};


// PS Iutput for 1 base texture & detail pass
struct PixDetailStruct
{
   float4 color              : COLOR0;
   float2 texCoord			 : TEXCOORD0;
};

// PS Output
struct FragOut
{
   float4 color				 : COLOR;
};


//------------------------------------------------------------
// Detail shaders
//------------------------------------------------------------

// vertex shader
VertDetailStruct VertDetail(VertData IN)
{
	VertDetailStruct OUT;
	
	OUT.texCoord = IN.texCoord * detailTexRepeat;
	OUT.position = mul(IN.position, worldViewProjection);
	OUT.color.xyz = float3(0, 0, 0);
	OUT.color.w = clamp(distance(detailCenter, mul(IN.position, worldMatrix)) / detailDistance, 0, 1);
	
	return OUT;
}

// pixel shader
FragOut PixDetail(PixDetailStruct IN)
{
	FragOut OUT;
	
	OUT.color = lerp(tex2D(detailMap, IN.texCoord.xy), float4(0.5,0.5,0.5,0.5), IN.color.w);

	return OUT;
}

//------------------------------------------------------------
// Techniques
//------------------------------------------------------------

// detail map technique
technique DetailEffect
{	
	pass Pass0
	{
		VertexShader = compile vs_1_1 VertDetail();
		PixelShader = compile ps_1_1 PixDetail();
	}
}