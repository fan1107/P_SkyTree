Shader "SkyTree/Lobby Cloud Drift"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cloud painting", 2D) = "white" {}
        _FarSpeed ("Far drift (widths per second)", Float) = 0.00055
        _MidSpeed ("Middle drift (widths per second)", Float) = 0.0013
        _NearSpeed ("Near drift (widths per second)", Float) = 0.0024
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _FarSpeed, _MidSpeed, _NearSpeed;
            CBUFFER_END

            struct VertexInput { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(VertexInput input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half3 SampleLayer(float2 uv, float speed)
            {
                // Reflect the painted edges so the bank can drift indefinitely without a seam.
                uv.x = 1.0 - abs(frac((uv.x - _Time.y * speed) * 0.5) * 2.0 - 1.0);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 farClouds = SampleLayer(input.uv, _FarSpeed);
                half3 middleClouds = SampleLayer(input.uv, _MidSpeed);
                half3 nearClouds = SampleLayer(input.uv, _NearSpeed);
                // Follow the diagonal of the painted banks. Lit cloud tops extend into the haze.
                float depth = 1.0 - input.uv.y + (input.uv.x - 0.5) * 0.16;
                float middle = smoothstep(0.31, 0.53, depth + 0.08 * (middleClouds.g - 0.5));
                float near = smoothstep(0.61, 0.80, depth + 0.08 * (nearClouds.g - 0.5));
                return half4(lerp(lerp(farClouds, middleClouds, middle), nearClouds, near), 1.0);
            }
            ENDHLSL
        }
    }
}
