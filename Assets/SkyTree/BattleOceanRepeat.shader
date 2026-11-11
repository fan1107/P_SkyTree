Shader "SkyTree/Battle Ocean Repeat"
{
    Properties { [PerRendererData] _MainTex ("Ocean", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=frac(i.uv);
                // Replace only water at the outer edges with continuous interior water.
                // This makes both axes periodic without reflected wave patterns.
                float2 w=smoothstep(0,.065,uv)*smoothstep(0,.065,1-uv);
                fixed4 a=tex2D(_MainTex,uv);
                fixed4 b=tex2D(_MainTex,frac(uv+float2(.5,0)));
                fixed4 c=tex2D(_MainTex,frac(uv+float2(0,.5)));
                fixed4 d=tex2D(_MainTex,frac(uv+.5));
                return lerp(lerp(d,c,w.x),lerp(b,a,w.x),w.y)*i.color;
            }
            ENDCG
        }
    }
}
