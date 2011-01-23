float4x4 worldViewProjection;  
texture baseTexture;   
float4 Tint;  
  
sampler2D baseTextureSampler = sampler_state  
{  
Texture = <baseTexture>; 
magfilter = LINEAR;
minfilter = LINEAR;
mipfilter = LINEAR;
AddressU = mirror;
AddressV = mirror; 
};    
  
struct VertexShaderInput  
{  
   float4 position : POSITION;    
   float4 color : COLOR;    
   float2 texCoord : TEXCOORD0;    
};  
  
struct VertexShaderOutput  
{  
    float4 position : POSITION;    
    float4 color : COLOR0;    
    float2 texCoord : TEXCOORD0;   
}; 

struct PixelShaderOutput
{
	float4 Color	: COLOR0;
}; 
  
VertexShaderOutput DefaultVertexShader(VertexShaderInput input)  
{  
    VertexShaderOutput output = (VertexShaderOutput)0;  
  
    output.position = mul(input.position, worldViewProjection);    
    output.texCoord = input.texCoord;    
    output.color = input.color;    
    
    return output;    
}  
  
float4 DefaultPixelShader(VertexShaderInput input) : COLOR0  
{  
    float4 color = tex2D(baseTextureSampler, input.texCoord);  
    return color;   
}  
  
PixelShaderOutput TintPixelShader(VertexShaderOutput input) : COLOR0  
{  
    PixelShaderOutput output = (PixelShaderOutput)0;
    
    output.Color = tex2D(baseTextureSampler, input.texCoord);
    
    //output.Color = Tint;
	
	return output; 
}  
  
technique DefaultTechnique  
{  
    pass p0  
    {  
        VertexShader = compile vs_1_1 DefaultVertexShader();  
        PixelShader = compile ps_1_1 DefaultPixelShader();  
    }  
}  
  
technique TintTechnique  
{  
     pass p0  
     {  
    VertexShader = compile vs_1_1 DefaultVertexShader();  
    PixelShader = compile ps_1_1 TintPixelShader();  
     }  
} 
