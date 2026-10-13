Shader "Reverb/Soft Contact"
{
    Properties {_BaseColor("Shadow",Color)=(0.04,0.07,0.075,0.28)}
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-30" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
            half4 frag(V i):SV_Target{float r=length(i.uv*2-1);return half4(_BaseColor.rgb,_BaseColor.a*pow(saturate(1-r),1.8));}
            ENDHLSL
        }
    }
}
