// UI sprite outline. Based on Unity's UI-Default so Mask, RectMask2D and stencil keep working.
// The sprite needs a transparent margin at least as wide as _OutlineWidth, otherwise the outline gets clipped at the quad edge.
Shader "Custom/UIOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width (texels)", Range(0, 64)) = 30
        _OutlineSoftness ("Outline Softness", Range(0.01, 1)) = 0.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineSoftness;

            #define OUTLINE_DIRECTIONS 16

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // Highest alpha found on two rings (full and half width) around the pixel,
            // the inner ring keeps thin shapes like stems from leaving gaps in the outline.
            float SampleOutlineAlpha(float2 uv)
            {
                float2 texel = _MainTex_TexelSize.xy * _OutlineWidth;
                float alpha = 0;

                [unroll]
                for (int i = 0; i < OUTLINE_DIRECTIONS; i++)
                {
                    float angle = i * (6.2831853 / OUTLINE_DIRECTIONS);
                    float2 dir = float2(cos(angle), sin(angle));
                    alpha = max(alpha, tex2D(_MainTex, uv + dir * texel).a);
                    alpha = max(alpha, tex2D(_MainTex, uv + dir * texel * 0.5).a);
                }

                return alpha;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 sprite = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                float outlineAlpha = 0;
                if (_OutlineWidth > 0)
                    outlineAlpha = smoothstep(0, _OutlineSoftness, SampleOutlineAlpha(IN.texcoord)) * _OutlineColor.a * IN.color.a;

                // Sprite over outline, premultiplied so the blend mode stays One OneMinusSrcAlpha.
                half3 rgb = sprite.rgb * sprite.a + _OutlineColor.rgb * outlineAlpha * (1 - sprite.a);
                half alpha = sprite.a + outlineAlpha * (1 - sprite.a);
                half4 color = half4(rgb, alpha);

                #ifdef UNITY_UI_CLIP_RECT
                color *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
