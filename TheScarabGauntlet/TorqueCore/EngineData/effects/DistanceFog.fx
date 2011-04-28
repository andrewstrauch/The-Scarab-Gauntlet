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

// from fog material:
float3 fogColor;
float fogNearDist;
float fogFarDist;
uniform float3 camPos;

//------------------------------------------------------------
// IO structs
//------------------------------------------------------------

// VS Input
struct VertData
{
    float4 position			 : POSITION;
};

// VS Output
struct VertOutput
{
   float4 position			 : POSITION;
   float4 color              : COLOR0;
};

// PS Input
struct VertColor
{
   float4 color              : COLOR0;
};

// PS Output
struct PixColor
{
   float4 color				 : COLOR;
};

//------------------------------------------------------------
// Shaders
//------------------------------------------------------------

// Vertex Shader ---------------------------
VertOutput VertDistanceFog(VertData IN)
{
	VertOutput OUT;

    OUT.position = mul(IN.position, worldViewProjection);

    // copy the fog color
    OUT.color.xyz = fogColor;

    // set the alpha of the fog color based on the distance of the vert from the camera
    OUT.color.w = (distance(mul(IN.position, worldMatrix), camPos) - fogNearDist) / (fogFarDist - fogNearDist);

	return OUT;
}

// Pixel Shader ---------------------------
PixColor PixDistanceFog(VertColor IN)
{
	PixColor OUT;
	
	// just copy the vertex color
	OUT.color = IN.color;
	return OUT;
}

//------------------------------------------------------------
// Techniques
//------------------------------------------------------------

technique DistanceFog
{
	pass Pass0
	{
		VertexShader = compile vs_1_1 VertDistanceFog();
		PixelShader = compile ps_1_1 PixDistanceFog();
	}
}