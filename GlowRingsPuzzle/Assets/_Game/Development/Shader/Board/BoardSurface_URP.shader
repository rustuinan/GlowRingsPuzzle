Shader "GlowRings/URP/Premium Metallic Surface"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.08, 0.10, 0.28, 1)
        _TopTint ("Top Tint", Color) = (0.42, 0.52, 0.85, 1)
        _BottomTint ("Bottom Tint", Color) = (0.02, 0.03, 0.10, 1)

        _MetallicTint ("Metallic Tint", Color) = (0.85, 0.94, 1.0, 1)
        _MetallicStrength ("Metallic Strength", Range(0, 1)) = 0.55

        _SpecColor ("Specular Color", Color) = (0.95, 0.98, 1.0, 1)
        _SpecStrength ("Specular Strength", Range(0, 2)) = 0.75
        _SpecPower ("Specular Power", Range(8, 256)) = 96

        _SecondarySpecStrength ("Secondary Spec Strength", Range(0, 1)) = 0.18
        _SecondarySpecPower ("Secondary Spec Power", Range(8, 256)) = 32

        _FresnelColor ("Fresnel Color", Color) = (0.65, 0.92, 1.0, 1)
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 2.6
        _FresnelStrength ("Fresnel Strength", Range(0, 2)) = 0.38

        _ReflectionColor ("Fake Reflection Color", Color) = (0.70, 0.88, 1.0, 1)
        _ReflectionStrength ("Fake Reflection Strength", Range(0, 1)) = 0.24
        _ReflectionSharpness ("Fake Reflection Sharpness", Range(1, 12)) = 5.5
        _ReflectionOffset ("Fake Reflection Offset", Range(-1, 1)) = 0.15

        [HDR]_EdgeGlowColor ("Edge Glow Color", Color) = (0.12, 0.82, 1.0, 1)
        _EdgeGlowStrength ("Edge Glow Strength", Range(0, 2)) = 0.10

        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.18
        _DiffuseStrength ("Diffuse Strength", Range(0, 2)) = 0.55

        _VerticalTintStrength ("Vertical Tint Strength", Range(0, 1)) = 0.32
        _CenterDarkenStrength ("Center Darken Strength", Range(0, 1)) = 0.18
        _ColorPower ("Color Power", Range(0.5, 2)) = 1.0
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

            #pragma target 2.0
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
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float fogCoord    : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TopTint;
                float4 _BottomTint;

                float4 _MetallicTint;
                float _MetallicStrength;

                float4 _SpecColor;
                float _SpecStrength;
                float _SpecPower;

                float _SecondarySpecStrength;
                float _SecondarySpecPower;

                float4 _FresnelColor;
                float _FresnelPower;
                float _FresnelStrength;

                float4 _ReflectionColor;
                float _ReflectionStrength;
                float _ReflectionSharpness;
                float _ReflectionOffset;

                float4 _EdgeGlowColor;
                float _EdgeGlowStrength;

                float _AmbientStrength;
                float _DiffuseStrength;

                float _VerticalTintStrength;
                float _CenterDarkenStrength;
                float _ColorPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nor = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = normalize(nor.normalWS);
                OUT.uv = IN.uv;
                OUT.fogCoord = ComputeFogFactor(OUT.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);

                float ndotl = saturate(dot(N, L));
                float wrappedLight = saturate((ndotl + 0.22) / 1.22);

                float verticalMask = saturate(IN.uv.y);

                float3 tint = lerp(_BottomTint.rgb, _TopTint.rgb, verticalMask);

                float2 centeredUV = IN.uv * 2.0 - 1.0;
                float centerMask = saturate(1.0 - length(centeredUV));
                centerMask = smoothstep(0.0, 1.0, centerMask);

                float3 baseColor = _BaseColor.rgb;
                baseColor = lerp(baseColor, tint, _VerticalTintStrength);

                float darken = lerp(1.0 - _CenterDarkenStrength, 1.0, centerMask);
                baseColor *= darken;

                baseColor = pow(saturate(baseColor), _ColorPower);

                float3 metallicBase = lerp(baseColor, baseColor * _MetallicTint.rgb, _MetallicStrength);

                float3 ambient = metallicBase * _AmbientStrength;
                float3 diffuse = metallicBase * wrappedLight * mainLight.color * _DiffuseStrength;

                float3 H = normalize(L + V);

                float mainSpec = pow(saturate(dot(N, H)), _SpecPower);
                float secondarySpec = pow(saturate(dot(N, H)), _SecondarySpecPower);

                float3 specular =
                    _SpecColor.rgb * mainSpec * _SpecStrength * mainLight.color +
                    _SpecColor.rgb * secondarySpec * _SecondarySpecStrength * mainLight.color;

                float fresnel = 1.0 - saturate(dot(N, V));
                fresnel = pow(fresnel, _FresnelPower);

                float3 fresnelLight = _FresnelColor.rgb * fresnel * _FresnelStrength;

                float reflectionBand = saturate(dot(reflect(-V, N), float3(0.0, 1.0, 0.35)) + _ReflectionOffset);
                reflectionBand = pow(reflectionBand, _ReflectionSharpness);

                float3 fakeReflection = _ReflectionColor.rgb * reflectionBand * _ReflectionStrength;

                float edgeGlow = fresnel * fresnel;
                float3 glow = _EdgeGlowColor.rgb * edgeGlow * _EdgeGlowStrength;

                float3 finalColor =
                    ambient +
                    diffuse +
                    specular +
                    fresnelLight +
                    fakeReflection +
                    glow;

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();

                for (uint i = 0u; i < lightCount; i++)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);

                    float addNdotL = saturate(dot(N, light.direction));
                    float3 addDiffuse = metallicBase * addNdotL * light.color * light.distanceAttenuation * 0.05;

                    finalColor += addDiffuse;
                }
                #endif

                finalColor = MixFog(finalColor, IN.fogCoord);

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}