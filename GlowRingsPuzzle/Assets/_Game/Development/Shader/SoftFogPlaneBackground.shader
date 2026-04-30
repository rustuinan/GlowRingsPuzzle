Shader "Puzzle/Background/URP_SoftFogTwinkle_OPAQUE"
{
    Properties
    {
        _TopColor("Top Color", Color) = (0.78, 0.88, 0.84, 1)
        _MiddleColor("Middle Color", Color) = (0.92, 0.70, 0.56, 1)
        _BottomColor("Bottom Color", Color) = (0.62, 0.82, 0.72, 1)

        _FogColor("Fog Color", Color) = (0.78, 0.90, 0.82, 1)
        _FogStrength("Fog Strength", Range(0,1)) = 0.55
        _FogHeight("Fog Height", Range(0,1)) = 0.35
        _FogSoftness("Fog Softness", Range(0.01,1)) = 0.45

        _StarColor("Star Color", Color) = (1, 0.96, 0.82, 1)
        _StarAmount("Star Amount", Range(0,1)) = 0.6
        _StarBrightness("Star Brightness", Range(0,3)) = 1.4
        _StarSize("Star Size", Range(20,250)) = 90
        _TwinkleSpeed("Twinkle Speed", Range(0,5)) = 1.2

        _VignetteStrength("Vignette Strength", Range(0,1)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _MiddleColor;
                half4 _BottomColor;

                half4 _FogColor;
                half _FogStrength;
                half _FogHeight;
                half _FogSoftness;

                half4 _StarColor;
                half _StarAmount;
                half _StarBrightness;
                half _StarSize;
                half _TwinkleSpeed;

                half _VignetteStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half hash21(float2 p)
            {
                p = frac(p * half2(234.34h, 435.345h));
                p += dot(p, p + 34.23h);
                return frac(p.x * p.y);
            }

            half starField(float2 uv)
{
    float2 scaled = uv * _StarSize;
    float2 cell = floor(scaled);
    float2 local = frac(scaled) - 0.5;

    half rnd = hash21(cell);
    half rnd2 = hash21(cell + 17.13);
    half rnd3 = hash21(cell + 41.71);

    half exists = step(1.0h - (_StarAmount * 0.14h), rnd);

    half dist = length(local);
    half shape = smoothstep(0.095h, 0.0h, dist);

    // Her yıldız farklı hızda ve farklı zamanda yanıp söner
    half speed = lerp(0.45h, 1.65h, rnd2);
    half phase = rnd3 * 6.283h;

    half twinkle = sin(_Time.y * _TwinkleSpeed * speed + phase);
    twinkle = saturate(twinkle * 0.5h + 0.5h);

    // Daha net oluşup yok olma
    twinkle = smoothstep(0.35h, 1.0h, twinkle);

    // Bazı yıldızlar doğal olarak daha sönük olsun
    half intensity = lerp(0.45h, 1.0h, rnd);

    return exists * shape * twinkle * intensity;
}

            half4 frag(Varyings IN) : SV_Target
            {
                half y = saturate(IN.uv.y);

                half3 bottomToMiddle = lerp(
                    _BottomColor.rgb,
                    _MiddleColor.rgb,
                    smoothstep(0.0h, 0.65h, y)
                );

                half3 middleToTop = lerp(
                    _MiddleColor.rgb,
                    _TopColor.rgb,
                    smoothstep(0.35h, 1.0h, y)
                );

                half3 color = lerp(
                    bottomToMiddle,
                    middleToTop,
                    smoothstep(0.35h, 0.85h, y)
                );

                half fog = 1.0h - smoothstep(_FogHeight, _FogHeight + _FogSoftness, y);
                color = lerp(color, _FogColor.rgb, fog * _FogStrength);

                half stars = starField(IN.uv);
                color += stars * _StarColor.rgb * _StarBrightness * 1.8h;

                float2 center = IN.uv * 2.0 - 1.0;
                half vignette = saturate(length(center));
                color *= 1.0h - vignette * _VignetteStrength;

                return half4(color, 1.0h);
            }

            ENDHLSL
        }
    }

    FallBack Off
}