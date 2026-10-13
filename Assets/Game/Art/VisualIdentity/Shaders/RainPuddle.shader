Shader "Reverb/Rain Puddle"
{
    Properties
    {
        _BaseColor("Water body",Color)=(.08,.12,.14,1)
        _Smoothness("Water polish",Range(0,1))=.94
        _Seed("Shape seed",Float)=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor; half _Smoothness; float _Seed;
            CBUFFER_END
            float4 _ReverbWeather;
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float2 uv:TEXCOORD1;half fog:TEXCOORD2;};
            V vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.uv=a.uv;o.fog=ComputeFogFactor(o.p.z);return o;}
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            half4 frag(V i):SV_Target
            {
                float2 uv=i.uv*2-1;
                float edge=length(uv)+.085*sin(uv.x*9+_Seed)*sin(uv.y*7-_Seed);
                half coverage=(1-smoothstep(.65,.97,edge))*smoothstep(.12,.5,_ReverbWeather.y);
                clip(coverage-.008);
                float2 cell=floor(i.w.xz*2.2), local=frac(i.w.xz*2.2)-.5;
                float random=Hash(cell);
                float age=frac(_ReverbWeather.z*(1.1+random*.8)+random*11);
                float2 delta=local-(float2(Hash(cell+3),Hash(cell+7))-.5)*.45;
                float radius=length(delta);
                float ring=sin((radius-age*.65)*58)*exp(-abs(radius-age*.65)*22)*(1-age)*_ReverbWeather.x;
                float2 normal=normalize(delta+1e-4)*ring*.08;
                normal+=float2(sin(i.w.z*7+_ReverbWeather.z*1.3),cos(i.w.x*6-_ReverbWeather.z))* .012;
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=_BaseColor.rgb;surface.alpha=coverage*.62;surface.normalTS=half3(0,0,1);
                surface.metallic=.08;surface.smoothness=_Smoothness;surface.occlusion=1;
                InputData data=(InputData)0;
                data.positionWS=i.w;data.normalWS=normalize(float3(normal.x,1,normal.y));
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);
                data.shadowCoord=TransformWorldToShadowCoord(i.w);data.bakedGI=SampleSH(data.normalWS);
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);data.shadowMask=half4(1,1,1,1);
                half4 c=UniversalFragmentPBR(data,surface);
                half fresnel=.09+.5*pow(1-saturate(dot(data.normalWS,data.viewDirectionWS)),5);
                half3 reflection=GlossyEnvironmentReflection(reflect(-data.viewDirectionWS,data.normalWS),i.w,.06,1,data.normalizedScreenSpaceUV);
                c.rgb+=reflection*fresnel;
                c.rgb=MixFog(c.rgb,i.fog);c.a=coverage*.42;return c;
            }
            ENDHLSL
        }
    }
}
