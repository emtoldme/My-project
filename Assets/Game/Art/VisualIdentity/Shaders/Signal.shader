Shader "Reverb/Signal Ribbon"
{
    Properties
    {
        [HDR] _BaseColor("Signal",Color)=(0.25,2,1.8,1)
        _Softness("Edge softness",Range(0.01,0.5))=0.3
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Softness;
            CBUFFER_END
            V vert(A a) {V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.color=a.color;return o;}
            half4 frag(V i):SV_Target
            {
                float edge=1-abs(i.uv.y*2-1);
                float core=smoothstep(0,_Softness,edge);
                return half4(_BaseColor.rgb*i.color.rgb,_BaseColor.a*i.color.a*core);
            }
            ENDHLSL
        }
    }
}
