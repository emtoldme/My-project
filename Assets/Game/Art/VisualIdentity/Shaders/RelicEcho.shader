Shader "Reverb/Relic Echo Accent"
{
    Properties { [HDR] _BaseColor("Warm ivory echo",Color)=(.75,.65,.4,.42) _Visibility("Visibility",Range(0,1))=1 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Blend One OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _BaseColor;float _Visibility;CBUFFER_END
            float4 _ReverbWeather;
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float fog:TEXCOORD1;};
            V Vert(A i)
            {
                V o;float time=_ReverbWeather.z;float3 p=i.p.xyz;
                p.y+=sin(time*1.6+i.uv.y*4)*.018;
                float a=time*.24,c=cos(a),s=sin(a);p.xz=mul(float2x2(c,-s,s,c),p.xz);
                o.p=TransformObjectToHClip(p);o.uv=i.uv;o.fog=ComputeFogFactor(o.p.z);return o;
            }
            half4 Frag(V i):SV_Target
            {
                float time=_ReverbWeather.z;
                half ripple=.32+.68*pow(saturate(.5+.5*cos(i.uv.x*9-time*1.6)),4);
                half alpha=_BaseColor.a*_Visibility*ripple*sin(saturate(i.uv.x)*PI);
                return half4(MixFog(_BaseColor.rgb,i.fog)*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
