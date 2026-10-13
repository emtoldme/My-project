Shader "Reverb/Weather Particles"
{
    Properties
    {
        [HDR] _BaseColor("Rain tint", Color)=(.55,.7,.85,.65)
        _Kind("0 rain / 1 ring / 2 mist", Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor; float _Kind;
            CBUFFER_END
            float4 _ReverbWeather;
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float3 w:TEXCOORD1; float fog:TEXCOORD2; };
            V vert(A a) { V o; o.w=TransformObjectToWorld(a.p.xyz); o.p=TransformWorldToHClip(o.w); o.uv=a.uv; o.color=a.color; o.fog=ComputeFogFactor(o.p.z); return o; }
            half4 frag(V i):SV_Target
            {
                float2 uv=i.uv*2-1;
                float radial=length(uv);
                half rain=pow(saturate(1-abs(uv.x)),2)*saturate(1-abs(uv.y));
                half ring=(1-smoothstep(.035,.12,abs(radial-.76)))*saturate(1-radial);
                half fog=pow(saturate(1-dot(uv,uv)),3)*.25;
                half alpha=_Kind<.5?rain:_Kind<1.5?ring:fog;
                float2 screenUV=GetNormalizedScreenSpaceUV(i.p);
                float raw=SampleSceneDepth(screenUV);
                float3 sceneWorld=ComputeWorldSpacePosition(screenUV,raw,UNITY_MATRIX_I_VP);
                float separation=distance(sceneWorld,_WorldSpaceCameraPos)-distance(i.w,_WorldSpaceCameraPos);
                alpha*=saturate(separation/(_Kind>1.5?.7:.12));
                Light light=GetMainLight();
                half3 color=_BaseColor.rgb*i.color.rgb*(.5+light.color*.45+_ReverbWeather.w*.8);
                return half4(MixFog(color,i.fog),alpha*_BaseColor.a*i.color.a);
            }
            ENDHLSL
        }
    }
}
