Shader "Custom/PlexusGeometryURP" {
    Properties {
        [HDR] _BaseColor("Color", Color) = (1,1,1,1)
        _Emission("Emission", Float) = 2.0
        _VisibleLinesProgress("Visible Segments Progress", Float) = 0.0
        _GlobalFadeOutMultiplier("Global Fade Out Multiplier", Float) = 1.0
        _FadeSmoothness("Fade Smoothness", Float) = 1.0
    }
    SubShader {
        Tags { "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" "Queue"="Transparent" }
        
        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On 
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Segment {
                float3 start;
                float3 end;
                float thickness;
                float segmentID;
            };

            StructuredBuffer<Segment> _SegmentData;
            
            float4 _BaseColor;
            float _Emission;
            float _VisibleLinesProgress;
            float _GlobalFadeOutMultiplier;
            float _FadeSmoothness;

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float segmentAlpha : TEXCOORD3;
            };

            Varyings vert(Attributes input) {
                Varyings output;
    
                Segment seg = _SegmentData[input.instanceID];
    
                float3 startPos = seg.start;
                float3 endPos = seg.end;
                float3 dir = endPos - startPos;
                float segLength = length(dir);
                float3 forward = dir / max(segLength, 0.0001);

                // Kreiramo stabilnu bazu (Up i Right)
                // Koristimo fiksni vektor da izbegnemo "skakanje" quada
                float3 arbitraryUp = abs(forward.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 right = normalize(cross(arbitraryUp, forward));
                float3 up = cross(forward, right);
    
                float3 posOS = input.positionOS.xyz;
    
                // VAŽNO: Quad je u Unity-ju 1x1 u X i Y ravni. 
                // Mi želimo da X bude debljina, a Y dužina koja ide od 0 do 1.
                float3 worldPos = startPos + 
                                 right * (posOS.x * seg.thickness) + 
                                 forward * ((posOS.y + 0.5) * segLength); // Pomeramo pivot na dno

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;

                float fadeDelta = _VisibleLinesProgress - seg.segmentID;
                output.segmentAlpha = saturate(fadeDelta / _FadeSmoothness);
    
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                if (input.segmentAlpha <= 0.01) discard;

                half3 color = _BaseColor.rgb * _Emission;
                float flicker = sin(_Time.y * 10.0 + input.positionWS.y * 2.0) * 0.1 + 0.9;
                color *= flicker;

                float finalAlpha = _BaseColor.a * input.segmentAlpha * _GlobalFadeOutMultiplier;
                if (finalAlpha <= 0.01) discard;

                return half4(color, finalAlpha);
            }
            ENDHLSL
        }
    }
}