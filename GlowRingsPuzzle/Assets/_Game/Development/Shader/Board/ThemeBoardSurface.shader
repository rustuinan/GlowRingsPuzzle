Shader "GlowRings/URP/Theme Board"
{
    Properties
    {
        _BaseColor ("Theme Surface", Color) = (0.3, 0.55, 0.65, 1)
        _RimColor ("Theme Edge", Color) = (0.5, 0.9, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ThemeSurface"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS; output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 light = normalize(half3(-0.35, 0.9, -0.25));
                half diffuse = saturate(dot(normal, light));
                half highlight = saturate(dot(normal, normalize(light + view)));
                half specular = pow(highlight, 40.0) * 0.065 + pow(highlight, 8.0) * 0.035;
                half rim = pow(1.0 - saturate(dot(normal, view)), 3.0) * 0.06;
                // A stable ambient floor keeps cells readable in every theme, including
                // isolated store renders, while normals still describe the board's depth.
                half3 color = _BaseColor.rgb * (0.76 + diffuse * 0.24) + specular + _RimColor.rgb * rim;
                return half4(color, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            float4 vert(Attributes input) : SV_POSITION { return TransformObjectToHClip(input.positionOS.xyz); }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
