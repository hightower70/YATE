// This shader creates a CRT-like matrix inspired by this article by Svyatoslav Cherkasov:
// http://www.gamasutra.com/blogs/SvyatoslavCherkasov/20140531/218753/Shader_tutorial_CRT_emulation.php
// 
// A pixel matrix is overlaid on the input image. Each pixel is assigned a color: red, green or blue
// in a repeating pattern.
// The color of a pixel is inherited directly from the red, green or blue color component of
// the underlying input image.
//
// In addition, there is the following functionality:
// 1. The R/G/B pixels can be shifted horizontally with each row so that we get a 3x3 matrix
//     like this:
//     R G B
//     B R G
//     G B R
// 2. An "overspill" can be added to each pixel to get a brighter and softer image.
//    Normally, if we have for example a red input color, we would get a 100 % red pixel followed
//    by a 0 % green and a 0 % blue pixel. If we add some red to the green and blue pixels,
//    the overall brightness is increased and the colors will look more natural.
// 3. To achieve a scanline effect, pixel rows are grouped in three, where the second and
//    third row in each group can have a lower brightness.

// Sampler
sampler2D TexSampler : register(S0);

// TextureSize
float2 TextureSize : register(C0);

// Brightness factor for darker scanlines
float1 BrightnessFactorRow2 : register(C1);
float1 BrightnessFactorRow3 : register(C2);

// Overspill from the primary colors.
float1 Overspill : register(C3);

// Diagonal or vertical raster.
float1 Diagonal : register(C4);

// Shader
float4 main(float2 texCoord : TEXCOORD) : COLOR 
{    
        // Scale to int texture size. Row and col are the current coordinates in the bitmap from
        // the upper left corner.
        int row = texCoord.y * TextureSize.y;
        int col = texCoord.x * TextureSize.x;
  
        // Pick up the color at the current position and add some brightness.
        float4 color = tex2D(TexSampler, texCoord) + 0.1f;
        
        float4 outColor = float4(0, 0, 0, 1);
        float4 multiplier = float4(0, 0, 0, 1);

        // Get the pixel position within a 3 x 3 matrix.
        int row_check = (int)row % 3;
        int col_check = (int)col % 3;
        
        // The pixel color is handled by setting a R/G/B multiplier vector.
        // First check if a diagonal raster should be implemted.
        if(Diagonal == 1)
                // Process the pixels, shifting the colors one step to the right for every row
                // within the 3 x 3 matrix.
    {
        if(row_check == col_check)
                {multiplier.r = 1; multiplier.g = Overspill;multiplier.b = Overspill;}
                else if ((row_check == 0 && col_check == 1) || (row_check == 1 && col_check == 2) || (row_check == 2 && col_check == 0))
                {multiplier.g = 1; multiplier.b = Overspill;multiplier.r = Overspill;}
                else
                {multiplier.b = 1; multiplier.r = Overspill;multiplier.g = Overspill;}
    } 
    else
    // For a vertical raster, process the pixels without shifting.
    {
    if (col_check == 0)
                        {
                                multiplier.r = 1; multiplier.g = Overspill;multiplier.b = Overspill;
                        }
                else if (col_check == 1)
                {
                        multiplier.g = 1; multiplier.b = Overspill;multiplier.r = Overspill;
                }
                else
                {
                        multiplier.b = 1; multiplier.r = Overspill;multiplier.g = Overspill;
                }
    }

        // Add scanlines.
        if (row_check == 1)
                {
                // Make the second of the three rows a bit darker to simulate a scan line.
                multiplier = multiplier * BrightnessFactorRow2;
                } 
        
        if (row_check == 2)
                {
                // Make the last of the three rows a bit darker to simulate a scan line.
                multiplier = multiplier * BrightnessFactorRow3;
                } 
        
        // Apply the multiplier to set the final color.
        outColor = color * multiplier;
          
        // The Alpha channel needs to be restored to 1 after all operations.
        outColor.a = 1;
                
        return outColor;
}

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

/*
float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 sp;
    float4 s;
    float4 sn;
    float y = uv.y * Height / 2;
    
    if ( frac(y) >= 0.5 )
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
*/