Shader "GlowRings/URP/ProceduralBeam"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.8, 0.1, 1)
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 10)) = 3
        _CoreWidth ("Core Width", Range(0.001, 0.5)) = 0.08
        _GlowWidth ("Glow Width", Range(0.001, 1)) = 0.35
        _EdgeSoftness ("Edge Softness", Range(0.1, 4)) = 1.4
        _LengthFade ("Length Fade", Range(0.1, 4)) = 1.2
        _Alpha ("Alpha", Range(0, 1)) = 1
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
            Name "ProceduralBeam"

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
                float _Intensity;
                float _CoreWidth;
                float _GlowWidth;
                float _EdgeSoftness;
                float _LengthFade;
                float _Alpha;
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
                float2 uv = input.uv;

                float x = abs(uv.x - 0.5) * 2.0;
                float y = abs(uv.y - 0.5) * 2.0;

                float lengthMask = saturate(1.0 - pow(x, _LengthFade));
                float core = exp(-pow(y / max(_CoreWidth, 0.001), 2.0));
                float glow = exp(-pow(y / max(_GlowWidth, 0.001), _EdgeSoftness));

                float endFade = smoothstep(1.0, 0.0, x);
                endFade *= endFade;

                float alpha = saturate((core * 0.95 + glow * 0.55) * lengthMask * endFade);
                alpha *= _Alpha * input.color.a;

                float3 color = lerp(_Color.rgb, _CoreColor.rgb, saturate(core));
                color *= _Intensity;
                color *= alpha;

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }
}