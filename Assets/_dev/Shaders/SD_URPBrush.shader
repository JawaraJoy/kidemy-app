Shader "Custom/BrushShader"
{
    Properties
    {
        _MainTex ("Canvas", 2D) = "white" {}
        _BrushPos ("Brush UV", Vector) = (0,0,0,0)
        _BrushColor ("Color", Color) = (1,0,0,1)
        _BrushSize ("Size", Float) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _BrushPos;
            float4 _BrushColor;
            float _BrushSize;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                // 1. Sample the CURRENT canvas
                fixed4 canvasColor = tex2D(_MainTex, input.uv);

                // 2. Calculate the brush "circle"
                float dist = distance(input.uv, _BrushPos.xy);
                float brushStrength = smoothstep(_BrushSize, _BrushSize * 0.8, dist);

                // 3. THE MASK: Brush only works where the canvas alpha is > 0
                float effectivePaint = brushStrength * canvasColor.a;

                // 4. Mix the colors based on masked strength
                fixed3 finalRGB = lerp(canvasColor.rgb, _BrushColor.rgb, effectivePaint);

                // 5. Keep original alpha
                return fixed4(finalRGB, canvasColor.a);
            }
            ENDCG
        }
    }
}