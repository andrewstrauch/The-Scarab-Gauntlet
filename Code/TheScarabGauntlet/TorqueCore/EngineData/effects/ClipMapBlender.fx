//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

#define lightScale 2.0

//------------------------------------------------------------
// Input parameters
//------------------------------------------------------------

float4x4 worldViewProjection;

texture opacityMap; // 4 channel opacity map
texture lightMap; // lightmap
texture baseTex1; // base texture 1
texture baseTex2; // base texture 2
texture baseTex3; // base texture 3
texture baseTex4; // base texture 4
float textureScale; // the number of times base textures should repeat
float2 opacityMapOffset;
// the offset of the opacity map (TGE legacy terrain opacity maps tend to be 
// off by (5/GridDim, 0.5/GridDim) .. this allows for correction)

// sampler for opacity map
sampler OpacityMapSampler = 
sampler_state
{
    Texture = <opacityMap>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

// sampler for light map
sampler LightMapSampler = 
sampler_state
{
    Texture = <lightMap>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

// sampler for base texture
sampler BaseTextureSampler1 = 
sampler_state
{
    Texture = <baseTex1>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

// sampler for base texture
sampler BaseTextureSampler2 = 
sampler_state
{
    Texture = <baseTex2>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

// sampler for base texture
sampler BaseTextureSampler3 = 
sampler_state
{
    Texture = <baseTex3>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

// sampler for base texture
sampler BaseTextureSampler4 = 
sampler_state
{
    Texture = <baseTex4>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

//------------------------------------------------------------
// IO structs
//------------------------------------------------------------

struct VertData1
{
   float4 position		: POSITION;
   float2 opacityCoord	: TEXCOORD0;
   float2 baseCoord1	: TEXCOORD1;
   float2 baseCoord2	: TEXCOORD2;
   float2 lightMapCoord : TEXCOORD3;
};

struct VertData2
{
   float4 position		: POSITION;
   float2 opacityCoord	: TEXCOORD0;
   float2 baseCoord		: TEXCOORD1;
   float2 lightMapCoord	: TEXCOORD2;
};

struct FragOut
{
   float4 color			: COLOR;
};

//------------------------------------------------------------
// Clip map blender image cache base texture blending
//------------------------------------------------------------

VertData1 ClipMapBlend_VS1( VertData1 IN )
{
	// do nothing!
	VertData1 OUT;
	
	OUT.position = mul(IN.position, worldViewProjection);
	OUT.opacityCoord = IN.opacityCoord + opacityMapOffset;
	OUT.baseCoord1 = IN.opacityCoord * textureScale;
	OUT.baseCoord2 = OUT.baseCoord1;
	OUT.lightMapCoord = IN.opacityCoord;
	
	return OUT;
}

FragOut ClipMapBlend0_PS1( VertData1 IN, uniform bool doLighting )
{
	FragOut OUT;
	
	float4 color;
    color = tex2D(OpacityMapSampler, IN.opacityCoord);
	
	OUT.color =
		(  (tex2D(BaseTextureSampler1, IN.baseCoord1) * color.b)
		 + (tex2D(BaseTextureSampler2, IN.baseCoord2) * color.g)
		);
		
	if(doLighting)
		OUT.color *= tex2D(LightMapSampler, IN.lightMapCoord) * lightScale;

	return OUT;
}

FragOut ClipMapBlend1_PS1( VertData1 IN, uniform bool doLighting )
{
	FragOut OUT;
	
	float4 color;
    color = tex2D(OpacityMapSampler, IN.opacityCoord);
	
	OUT.color = 
	    (   (tex2D(BaseTextureSampler3, IN.baseCoord1) * color.r)
		  + (tex2D(BaseTextureSampler4, IN.baseCoord2) * color.a)
		);

	if(doLighting)
		OUT.color *= tex2D(LightMapSampler, IN.lightMapCoord) * lightScale;
		
	return OUT;
}

VertData2 ClipMapBlend_VS2( VertData2 IN )
{
	// do nothing!
	VertData2 OUT;
	
	OUT.position = mul(IN.position, worldViewProjection);
	OUT.opacityCoord = IN.opacityCoord + opacityMapOffset;
	OUT.baseCoord = IN.opacityCoord * textureScale;
	OUT.lightMapCoord = IN.opacityCoord;
	
	return OUT;
}

FragOut ClipMapBlend_PS2( VertData2 IN, uniform bool doLighting )
{
	FragOut OUT;
	
	float4 color;
    color = tex2D(OpacityMapSampler, IN.opacityCoord);
	
	OUT.color =
		(  (tex2D(BaseTextureSampler1, IN.baseCoord) * color.b) 
		 + (tex2D(BaseTextureSampler2, IN.baseCoord) * color.g)
		 + (tex2D(BaseTextureSampler3, IN.baseCoord) * color.r)
		 + (tex2D(BaseTextureSampler4, IN.baseCoord) * color.a)
		);
		
	if(doLighting)
		OUT.color *= tex2D(LightMapSampler, IN.lightMapCoord) * lightScale;

	return OUT;
}


//------------------------------------------------------------
// Techniques
//------------------------------------------------------------

technique BlendWithoutLightMap_1_1
{
	pass Pass0
	{
		VertexShader = compile vs_1_1 ClipMapBlend_VS1();
		PixelShader = compile ps_1_1 ClipMapBlend0_PS1(false);
	}
	
	pass Pass1
	{
		PixelShader = compile ps_1_1 ClipMapBlend1_PS1(false);
	}
}

technique BlendWithLightMap_1_1
{
	pass Pass0
	{
		VertexShader = compile vs_1_1 ClipMapBlend_VS1();
		PixelShader = compile ps_1_1 ClipMapBlend0_PS1(true);
	}
	
	pass Pass1
	{
		PixelShader = compile ps_1_1 ClipMapBlend1_PS1(true);
	}
}

technique BlendWithoutLightMap_2_0
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 ClipMapBlend_VS2();
		PixelShader = compile ps_2_0 ClipMapBlend_PS2(false);
	}
}

technique BlendWithLightMap_2_0
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 ClipMapBlend_VS2();
		PixelShader = compile ps_2_0 ClipMapBlend_PS2(true);
	}
}