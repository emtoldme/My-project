Shader "Reverb/Signal Ribbon"
{
    Properties
    {
        [HDR] _BaseColor("Signal",Color)=(0.25,2,1.8,1)
        _Softness("Edge softness",Range(0.01,0.5))=0.3
        _Etching("Frequency etching",Range(0,1))=0
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
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float3 w:TEXCOORD1;half fog:TEXCOORD2;};
            float4 _ReverbWeather;
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Softness,_Etching;
            CBUFFER_END
            V vert(A a) {V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.uv=a.uv;o.color=a.color;o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 frag(V i):SV_Target
            {
                float edge=1-abs(i.uv.y*2-1);
                float core=smoothstep(0,_Softness,edge);
                float tick=smoothstep(.16,.22,abs(frac(i.uv.x*24)-.5));
                float carrier=.7+.3*sin(i.uv.x*47-_ReverbWeather.z*7);
                float engraving=lerp(1,.28+.72*tick*carrier,_Etching);
                half3 rgb=MixFogColor(_BaseColor.rgb*i.color.rgb,half3(0,0,0),i.fog);
                return half4(rgb,_BaseColor.a*i.color.a*core*engraving);
            }
            ENDHLSL
        }
    }
}
