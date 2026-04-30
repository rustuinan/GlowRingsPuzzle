Shader "Puzzle/URP/MobileSoftBackground_Unlit_Fixed"
{
    Properties
    {
        _TopColor("Top Color", Color) = (0.98, 0.96, 0.94, 1)
        _BottomColor("Bottom Color", Color) = (0.92, 0.88, 0.84, 1)

        _CenterGlowColor("Center Glow Color", Color) = (1.00, 0.98, 0.95, 1)
        _CenterGlowStrength("Center Glow Strength", Range(0,1)) = 0.22
        _CenterGlowSize("Center Glow Size", Range(0.1,3)) = 1.15

        _VignetteColor("Vignette Color", Color) = (0.82, 0.76, 0.70, 1)
        _VignetteStrength("Vignette Strength", Range(0,1)) = 0.12
        _VignetteSoftness("Vignette Softness", Range(0.1,4)) = 1.8

        _BlobColorA("Blob Color A", Color) = (1.00, 0.92, 0.88, 1)
        _BlobColorB("Blob Color B", Color) = (0.95, 0.89, 0.84, 1)
        _BlobStrength("Blob Strength", Range(0,1)) = 0.08
        _BlobScale("Blob Scale", Range(0.2,4)) = 1.2
        _BlobSpeed("Blob Speed", Range(0,2)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Background"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "UnlitBackground"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _BottomColor;

                float4 _CenterGlowColor;
                float _CenterGlowStrength;
                float _CenterGlowSize;

                float4 _VignetteColor;
                float _VignetteStrength;
                float _VignetteSoftness;

                float4 _BlobColorA;
                float4 _BlobColorB;
                float _BlobStrength;
                float _BlobScale;
                float _BlobSpeed;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float BlobField(float2 uv, float t, float scale)
            {
                float2 p = uv * scale;

                float b1 = sin(p.x * 2.1 + t * 0.9) * 0.5 + 0.5;
                float b2 = sin(p.y * 1.7 - t * 0.7) * 0.5 + 0.5;
                float b3 = sin((p.x + p.y) * 1.3 + t * 0.5) * 0.5 + 0.5;

                float mixVal = (b1 + b2 + b3) * 0.3333;
                return smoothstep(0.35, 0.75, mixVal);
            }

            float3 GetBackgroundBase(float2 uv)
            {
                float verticalT = saturate(uv.y);
                return lerp(_BottomColor.rgb, _TopColor.rgb, verticalT);
            }

            float3 ApplyCenterGlow(float3 color, float2 uv)
            {
                float2 centered = uv * 2.0 - 1.0;
                centered.x *= 0.82;

                float dist = length(centered) / max(_CenterGlowSize, 0.0001);
                float glow = saturate(1.0 - dist);
                glow *= glow;

                return color + _CenterGlowColor.rgb * glow * _CenterGlowStrength;
            }

            float3 ApplyVignette(float3 color, float2 uv)
            {
                float2 centered = uv * 2.0 - 1.0;
                float vignette = saturate(length(centered));
                vignette = pow(vignette, _VignetteSoftness);

                float3 vignetteTint = lerp(float3(1, 1, 1), _VignetteColor.rgb, vignette * _VignetteStrength);
                return color * vignetteTint;
            }

            float3 ApplyMovingBlobs(float3 color, float2 uv)
            {
                float t = _Time.y * _BlobSpeed;

                float fieldA = BlobField(uv + float2(0.07, 0.00), t, _BlobScale);
                float fieldB = BlobField(uv + float2(-0.11, 0.13), -t * 0.8, _BlobScale * 1.35);

                float3 blobTint =
                    lerp(float3(1,1,1), _BlobColorA.rgb, fieldA * _BlobStrength) *
                    lerp(float3(1,1,1), _BlobColorB.rgb, fieldB * _BlobStrength * 0.85);

                return color * blobTint;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 color = GetBackgroundBase(input.uv);

                color = ApplyCenterGlow(color, input.uv);
                color = ApplyMovingBlobs(color, input.uv);
                color = ApplyVignette(color, input.uv);

                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}