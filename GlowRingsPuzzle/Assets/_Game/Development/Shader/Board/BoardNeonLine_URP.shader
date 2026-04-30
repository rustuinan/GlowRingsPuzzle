Shader "GlowRings/URP/Board Neon Line"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.12, 0.10, 0.35, 1)
        [HDR]_EmissionColor ("Emission Color", Color) = (0.10, 0.85, 1.0, 1)
        _EmissionIntensity ("Emission Intensity", Range(0,5)) = 1.6

        _PulseStrength ("Pulse Strength", Range(0,1)) = 0.12
        _PulseSpeed ("Pulse Speed", Range(0,5)) = 1.1

        _FresnelColor ("Fresnel Color", Color) = (0.70, 0.35, 1.0, 1)
        _FresnelPower ("Fresnel Power", Range(0.5,8)) = 3.5
        _FresnelStrength ("Fresnel Strength", Range(0,2)) = 0.45

        _SpecularStrength ("Specular Strength", Range(0,1)) = 0.18
        _SpecularPower ("Specular Power", Range(8,128)) = 64
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
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionIntensity;

                float _PulseStrength;
                float _PulseSpeed;

                float4 _FresnelColor;
                float _FresnelPower;
                float _FresnelStrength;

                float _SpecularStrength;
                float _SpecularPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nor = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = normalize(nor.normalWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);

                float ndotl = saturate(dot(N, L));
                float3 diffuse = _BaseColor.rgb * (0.25 + ndotl * 0.35);

                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), _SpecularPower);
                float3 specular = spec * _SpecularStrength * mainLight.color;

                float fresnel = 1.0 - saturate(dot(N, V));
                fresnel = pow(fresnel, _FresnelPower);
                float3 fresnelLight = _FresnelColor.rgb * fresnel * _FresnelStrength;

                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                pulse = lerp(1.0 - _PulseStrength, 1.0, pulse);

                float3 emission = _EmissionColor.rgb * _EmissionIntensity * pulse;

                float3 finalColor = diffuse + specular + fresnelLight + emission;
                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}