Shader "GlowRings/URP/AdditiveGlow"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _Intensity ("Glow Intensity", Range(0, 10)) = 1
        _AlphaPower ("Alpha Power", Range(0.1, 4)) = 1
        _Softness ("Softness", Range(0.1, 3)) = 1
        _UseTextureColor ("Use Texture Color", Range(0, 1)) = 1
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
            Name "AdditiveGlow"

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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _TintColor;
                float _Intensity;
                float _AlphaPower;
                float _Softness;
                float _UseTextureColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                half alpha = saturate(tex.a * _TintColor.a * input.color.a);
                alpha = pow(alpha, _AlphaPower);
                alpha = saturate(alpha * _Softness);

                half3 textureColor = tex.rgb;
                half3 tintColor = _TintColor.rgb * input.color.rgb;

                half3 finalColor = lerp(tintColor, textureColor * tintColor, _UseTextureColor);
                finalColor *= _Intensity;
                finalColor *= alpha;

                return half4(finalColor, alpha);
            }

            ENDHLSL
        }
    }
}