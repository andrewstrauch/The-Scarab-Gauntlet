float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    return float4(0, 0, 0, 1);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
