Shader "GlowRings/UI/Theme Atmosphere"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _TopColor ("Top", Color) = (0.1,0.1,0.2,1)
        _BottomColor ("Bottom", Color) = (0.01,0.01,0.05,1)
        _Accent ("Accent", Color) = (0.5,0.8,1,1)
        _Secondary ("Secondary", Color) = (0.6,0.5,1,1)
        _ThemeStyle ("Atmosphere", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "ThemeAtmosphere.hlsl"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; float4 color : COLOR; };
            float4 _TopColor, _BottomColor, _Accent, _Secondary, _ClipRect;
            float _ThemeStyle;
            v2f vert(appdata v) { v2f o; o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 color = lerp(_BottomColor.rgb, _TopColor.rgb, i.uv.y);
                color += ThemeAtmosphere(i.uv, _ThemeStyle, 0, _Accent.rgb, _Secondary.rgb);
                fixed4 output = fixed4(color, i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                output.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return output;
            }
            ENDCG
        }
    }
}
