Shader "URP/BrushShader"
{
    Properties {
        _MainTex ("Canvas", 2D) = "white" {}
        _BrushPos ("Brush UV", Vector) = (0,0,0,0)
        _BrushColor ("Color", Color) = (1,0,0,1)
        _BrushSize ("Size", Float) = 0.01
    }
    SubShader {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass {
            ZTest Always 
            Cull Off 
        ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _BrushPos, _BrushColor;
            float _BrushSize;

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            /*
            half4 frag(Varyings input) : SV_Target {
                half4 canvasColor = tex2D(_MainTex, input.uv);
                float dist = distance(input.uv, _BrushPos.xy);
                // Use smoothstep for soft brush edges
                float edge = smoothstep(_BrushSize, _BrushSize * 0.8, dist);
                return lerp(canvasColor, _BrushColor, edge);
            }
            */
            
            half4 frag(Varyings input) : SV_Target {
                // 1. Sample the CURRENT canvas (contains your base shape)
                half4 canvasColor = tex2D(_MainTex, input.uv);
                
                // 2. Calculate the brush "circle"
                float dist = distance(input.uv, _BrushPos.xy);
                float brushStrength = smoothstep(_BrushSize, _BrushSize * 0.8, dist);

                // 3. THE MASK: Brush only works where the canvas alpha is > 0
                // This is the line that prevents painting on transparent areas
                float effectivePaint = brushStrength * canvasColor.a;

                // 4. Mix the colors based on the masked strength
                half3 finalRGB = lerp(canvasColor.rgb, _BrushColor.rgb, effectivePaint);

                // 5. Keep the original alpha so the shape never changes size
                return half4(finalRGB, canvasColor.a);
            }
            ENDHLSL
        }
    }
}