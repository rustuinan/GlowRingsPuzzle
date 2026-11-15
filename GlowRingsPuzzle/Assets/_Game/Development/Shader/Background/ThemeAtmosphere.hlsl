#ifndef GLOW_RINGS_THEME_ATMOSPHERE
#define GLOW_RINGS_THEME_ATMOSPHERE
float ThemeHash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
float3 ThemeAtmosphere(float2 uv, float style, float time, float3 accent, float3 secondary)
{
    float3 result = 0;
    float2 sampleOffset;
    if (style < 0.5) return result;
    if (style < 1.5)
    {
        float2 grid = uv * float2(12, 18);
        float seed = ThemeHash(floor(grid));
        float star = (1.0 - smoothstep(0.025, 0.07, length(frac(grid) - 0.5))) * step(0.88, seed);
        float mist = exp(-pow(uv.y - 0.60 - sin(uv.x * 4.0) * 0.1, 2.0) * 32.0);
        result = accent * star * 0.22 + secondary * mist * 0.025;
    }
    else if (style < 2.5)
    {
        float wave = 0;
        for (int i = 0; i < 3; i++)
        {
            float horizon = 0.13 + i * 0.08 + sin(uv.x * 6.0 + i * 1.2 + time * 0.08) * 0.04;
            wave += exp(-abs(uv.y - horizon) * 70.0);
        }
        result = accent * wave * 0.065;
    }
    else if (style < 3.5)
    {
        sampleOffset = (uv - float2(0.78, 0.78)) * float2(1.0, 1.2);
        float sun = 1.0 - smoothstep(0.085, 0.105, length(sampleOffset));
        float haze = exp(-dot(sampleOffset, sampleOffset) * 8.0);
        float horizon = exp(-abs(uv.y - 0.20 - sin(uv.x * 5.0) * 0.025) * 35.0);
        result = accent * (sun * 0.11 + haze * 0.04) + secondary * horizon * 0.04;
    }
    else if (style < 4.5)
    {
        float bokeh = 0;
        for (int i = 0; i < 5; i++)
        {
            float2 center = float2(i < 3 ? 0.08 + i * 0.09 : 0.84 + (i - 3) * 0.08, frac(i * 0.37 + 0.21));
            sampleOffset = (uv - center) * float2(1.0, 0.8);
            bokeh += exp(-dot(sampleOffset, sampleOffset) * (110.0 + i * 30.0));
        }
        float beam = exp(-pow(uv.x * 0.8 + uv.y - 0.9, 2.0) * 70.0);
        result = accent * bokeh * 0.07 + secondary * beam * 0.035;
    }
    else
    {
        float center = 0.67 + sin(uv.x * 5.0 + time * 0.04) * 0.12;
        float curtain = exp(-abs(uv.y - center) * 11.0) * pow(0.5 + 0.5 * sin(uv.x * 17.0 + uv.y * 4.0), 2.0);
        result = lerp(secondary, accent, uv.x) * curtain * 0.13;
    }
    return result;
}
#endif

