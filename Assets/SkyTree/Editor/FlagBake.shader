Shader "Hidden/SkyTree/FlagBake"
{
    Properties { _MainTex("Cloth",2D)="white"{} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Cull Off ZWrite Off Blend One Zero
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            V vert(A a){V o;o.p=float4(a.p.x/.8666667,-a.p.y/.65,0,1);o.uv=a.uv;o.color=a.color;return o;}
            half4 frag(V v):SV_Target {half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv);c.rgb*=v.color.rgb;return c;}
            ENDHLSL
        }
    }
}
