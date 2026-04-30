Shader "GlowRings/URP/Ring Lit Neon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0, 1, 1)
        _EmissionColor ("Emission Color", Color) = (1, 0, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 1.2

        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 4)) = 1

        _LightWrap ("Light Wrap", Range(0, 1)) = 0.35
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 0.65
        _SpecularStrength ("Specular Strength", Range(0, 2)) = 0.45
        _SpecularPower ("Specular Power", Range(8, 128)) = 48
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionIntensity;

                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;

                float _LightWrap;
                float _AmbientStrength;
                float _SpecularStrength;
                float _SpecularPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                output.fogCoord = ComputeFogFactor(output.positionHCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                Light mainLight = GetMainLight();

                float ndotl = dot(normalWS, mainLight.direction);
                float wrappedLight = saturate((ndotl + _LightWrap) / (1.0 + _LightWrap));

                float3 diffuse = _BaseColor.rgb * wrappedLight * mainLight.color;

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float specularTerm = pow(saturate(dot(normalWS, halfDir)), _SpecularPower);
                float3 specular = specularTerm * _SpecularStrength * mainLight.color;

                float3 ambient = _BaseColor.rgb * _AmbientStrength;

                float rim = 1.0 - saturate(dot(normalWS, viewDirWS));
                rim = pow(rim, _RimPower);
                float3 rimLight = _RimColor.rgb * rim * _RimIntensity;

                float3 emission = _EmissionColor.rgb * _EmissionIntensity;

                float3 finalColor = ambient + diffuse + specular + rimLight + emission;

                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();

                for (uint lightIndex = 0u; lightIndex < pixelLightCount; lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);

                    float addNdotL = dot(normalWS, light.direction);
                    float addWrapped = saturate((addNdotL + _LightWrap) / (1.0 + _LightWrap));

                    float3 addDiffuse = _BaseColor.rgb * addWrapped * light.color * light.distanceAttenuation;
                    finalColor += addDiffuse * 0.35;
                }
                #endif

                finalColor = MixFog(finalColor, input.fogCoord);

                return half4(finalColor, _BaseColor.a);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode"="ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }
}