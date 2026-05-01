Shader "Custom/URP_Blur_With_Depth"
{
    Properties
    {
        _BlurSize ("Blur Strength", Range(0, 0.05)) = 0.01
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        
        Pass
        {
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct appdata {
                float4 positionOS : POSITION;
            };

            struct v2f {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            float _BlurSize;

            v2f vert(appdata v) {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(v2f i) : SV_Target {
                // 1. Normalizacija koordinata ekrana
                float2 uv = i.screenPos.xy / i.screenPos.w;

                half4 col = 0;
                float spread = _BlurSize;
    
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(spread, spread));
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(-spread, spread));
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(spread, -spread));
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(-spread, -spread));

                return col / 4;
            }
            ENDHLSL
        }
    }
}