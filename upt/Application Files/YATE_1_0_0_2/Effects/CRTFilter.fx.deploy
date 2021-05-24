// Creative Commons Attribution 3.0 United States License
// http://creativecommons.org/licenses/by/3.0/us/
//
// Copyright (c) 2019 Laszlo Arvai

/// <description>Simple CRT like effect</description>
/// <profile>ps_3_0</profile>

sampler2D input : register(s0);
float Width : register(c0);
float Height : register(c1);
float4 EvenParam : register(c2);
float4 OddParam : register(c3);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 sp;
    float4 s;
    float4 sn;
    float y = uv.y * Height / 2;
    
    if (frac(y) >= 0.5)
    {
        sp = tex2D(input, uv.xy + float2(-1.0f / Width, -1.0f / Height));
        s = tex2D(input, uv.xy + float2(0, -1.0f / Height));
        sn = tex2D(input, uv.xy + float2(1.0f / Width, -1.0f / Height));
    
        return (sp * OddParam.x + s * OddParam.y + sn * OddParam.z) / EvenParam.w;
    }
    else
    {
        sp = tex2D(input, uv.xy + float2(-1.0f / Width, 0));
        s = tex2D(input, uv.xy);
        sn = tex2D(input, uv.xy + float2(1.0f / Width, 0));

        return (sp * EvenParam.x + s * EvenParam.y + sn * EvenParam.z) / EvenParam.w;
    }
}