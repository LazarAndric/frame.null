Shader "Custom/PlexusLineURP" {
    Properties {
        [HDR] _BaseColor("Color", Color) = (1,1,1,1)
        _Emission("Emission", Float) = 2.0
        // NOVO: Parametri za animaciju (moraju se poklapati sa C# imenima)
        _VisibleLinesProgress("Visible Lines Progress", Float) = 0.0
        _GlobalFadeOutMultiplier("Global Fade Out Multiplier", Float) = 1.0 // 1 = Vidljivo, 0 = Nevidljivo
        _FadeSmoothness("Fade Smoothness", Float) = 5.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Geometry" }
        
        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Point { float3 position; };

            StructuredBuffer<Point> _Points;
            StructuredBuffer<int> _Indices;
            float4 _BaseColor;
            float _Emission;
            float _VisibleLinesProgress;
            float _GlobalFadeOutMultiplier; // NOVO: Primamo globalni multiplier
            float _FadeSmoothness;

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float lineAlpha : TEXCOORD1;
            };

            Varyings vert(uint vertexID : SV_VertexID) {
                Varyings output;
                
                float currentLineID = floor(vertexID / 2.0);
                int pIndex = _Indices[vertexID];
                Point p = _Points[pIndex];

                output.positionWS = p.position;
                output.positionCS = TransformWorldToHClip(p.position);
                
                // Pojedinacni Fade In (isto kao ranije)
                float fadeDelta = _VisibleLinesProgress - currentLineID;
                output.lineAlpha = saturate(fadeDelta / _FadeSmoothness);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // Ako je pojedinacna alpha nula, discard piksla (optimizacija)
                if (input.lineAlpha <= 0.01) discard;

                Light light = GetMainLight();
                
                // Boja sa Emission-om
                half3 color = _BaseColor.rgb * _Emission;
                float flicker = sin(_Time.y * 10.0 + input.positionWS.y * 2.0) * 0.2 + 0.8;
                color *= flicker;
                
                // --- KLJUCNI KORAK: KOMBINOVANJE ALFE ---
                // Krajnja providnost je: (Alpha Materijala) * (Pojedinacni FadeIn) * (Globalni FadeOut)
                float finalAlpha = _BaseColor.a * input.lineAlpha * _GlobalFadeOutMultiplier;
                
                // discard ako je globalni fadeout skoro gotov
                if (finalAlpha <= 0.01) discard;

                return half4(color, finalAlpha);
            }
            ENDHLSL
        }
    }
}