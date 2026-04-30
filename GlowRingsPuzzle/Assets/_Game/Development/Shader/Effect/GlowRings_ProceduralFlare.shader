Shader "GlowRings/URP/ProceduralFlare"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.8, 0.15, 1)
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _AccentColor ("Accent Color", Color) = (1, 0.25, 0.85, 1)
        _Intensity ("Intensity", Range(0, 12)) = 4
        _StarSharpness ("Star Sharpness", Range(2, 80)) = 28
        _GlowPower ("Glow Power", Range(0.2, 5)) = 1.8
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Rotation ("Rotation", Range(0, 6.28318)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "ProceduralFlare"

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _CoreColor;
                float4 _AccentColor;
                float _Intensity;
                float _StarSharpness;
                float _GlowPower;
                float _Alpha;
                float _Rotation;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;

                float s = sin(_Rotation);
                float c = cos(_Rotation);
                p = float2(c * p.x - s * p.y, s * p.x + c * p.y);

                float r = length(p);
                float angle = atan2(p.y, p.x);

                float radialGlow = pow(saturate(1.0 - r), _GlowPower);

                float horizontal = pow(saturate(1.0 - abs(p.y) * 7.0), _StarSharpness * 0.08) * pow(saturate(1.0 - abs(p.x)), 1.2);
                float vertical = pow(saturate(1.0 - abs(p.x) * 7.0), _StarSharpness * 0.08) * pow(saturate(1.0 - abs(p.y)), 1.2);

                float diagonalA = pow(abs(cos(angle - 0.785398)), _StarSharpness) * pow(saturate(1.0 - r), 2.4);
                float diagonalB = pow(abs(cos(angle + 0.785398)), _StarSharpness) * pow(saturate(1.0 - r), 2.4);

                float star = horizontal + vertical + diagonalA * 0.45 + diagonalB * 0.45;
                float core = exp(-r * r * 24.0);

                float alpha = saturate(radialGlow * 0.8 + star * 0.9 + core);
                alpha *= saturate(1.0 - r);
                alpha *= _Alpha * input.color.a;

                float3 color = _Color.rgb;
                color = lerp(color, _AccentColor.rgb, saturate((diagonalA + diagonalB) * 0.35));
                color = lerp(color, _CoreColor.rgb, saturate(core));

                color *= _Intensity;
                color *= alpha;

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }
}