Shader "Reverb/Meadow Wind"
{
    Properties
    {
        _BaseMap("Atlas",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _WindDirection("Wind XZ",Vector)=(0.85,0,0.5,0)
        _WindStrength("Wind bend",Float)=0.11
        _WindSpeed("Wind speed",Float)=1.15
        _PlantHeight("Height",Float)=1.5
        _UseExternalTime("Rule clock",Float)=1
        _VisualTime("Time",Float)=0
        _Cutoff("Alpha cutoff",Range(0,1))=0.42
        _GroundMask("Painted terrain / foliage mask",2D)="white"{}
        _FieldBounds("Terrain X Z width depth",Vector)=(-48,-48,96,96)
        _Clearing("Clearing X Z radius",Vector)=(0,0,3.2,0)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        TEXTURE2D(_GroundMask);SAMPLER(sampler_GroundMask);
        float4 _ReverbPlayer;
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;half4 _BaseColor;float4 _WindDirection,_FieldBounds,_Clearing;
        float _WindStrength,_WindSpeed,_PlantHeight,_UseExternalTime,_VisualTime,_Cutoff;
        CBUFFER_END
        float3 Bend(float3 p)
        {
            float3 w=TransformObjectToWorld(p);
            float t=lerp(_Time.y,_VisualTime,_UseExternalTime);
            float h=pow(saturate(p.y/max(_PlantHeight,0.1)),1.8);
            float wave=sin(dot(w.xz,float2(0.72,0.49))+t*_WindSpeed)+0.35*sin(w.x*2.7+w.z*1.3+t*2.2);
            w.xz+=normalize(_WindDirection.xz)*wave*_WindStrength*h;
            return w;
        }
        void ClipLanes(float3 w)
        {
            float2 uv=(w.xz-_FieldBounds.xy)/max(_FieldBounds.zw,float2(1,1));
            clip(SAMPLE_TEXTURE2D_LOD(_GroundMask,sampler_GroundMask,uv,0).r-.35);
            clip(length(w.xz-_Clearing.xy)-_Clearing.z);
        }
        void ClipAlpha(half alpha,float3 w)
        {
            clip(alpha-_Cutoff);
            float visible=lerp(1,smoothstep(1.05,2.15,distance(w.xz,_ReverbPlayer.xz)),_ReverbPlayer.w);
            float noise=frac(sin(dot(floor(w.xz*160),float2(12.9898,78.233)))*43758.5453);
            clip(visible-noise);
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma editor_sync_compilation
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float2 uv:TEXCOORD1;float3 w:TEXCOORD2;float fog:TEXCOORD3;float height:TEXCOORD4;};
            V vert(A a){UNITY_SETUP_INSTANCE_ID(a);V o;o.w=Bend(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;o.fog=ComputeFogFactor(o.p.z);o.height=saturate(a.p.y/1.7);return o;}
            half4 frag(V i):SV_Target
            {
                ClipLanes(i.w);Light light=GetMainLight(TransformWorldToShadowCoord(i.w));
                half3 n=normalize(i.n);
                half wrap=abs(dot(n,light.direction));
                half3 sky=max(SampleSH(half3(0,1,0)),half3(0.1,0.13,0.16));
                half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);ClipAlpha(tex.a,i.w);
                half3 albedo=tex.rgb*_BaseColor.rgb;
                albedo*=lerp(half3(.6,.88,.62),half3(1.13,1.08,.78),i.height);
                half3 diffuse=sky*0.9+light.color*(0.4+wrap*0.6)*lerp(0.38,1,light.shadowAttenuation);
                half3 color=albedo*diffuse;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma editor_sync_compilation
            #pragma multi_compile_instancing
            float3 _LightDirection;
            struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float2 uv:TEXCOORD1;};
            V vert(A a)
            {
                UNITY_SETUP_INSTANCE_ID(a);V o;
                float3 w=Bend(a.p.xyz);
                float4 p=TransformWorldToHClip(ApplyShadowBias(w,TransformObjectToWorldNormal(a.n),_LightDirection));
                #if UNITY_REVERSED_Z
                p.z=min(p.z,UNITY_NEAR_CLIP_VALUE);
                #else
                p.z=max(p.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                o.p=p;o.w=w;o.uv=a.uv;return o;
            }
            half4 frag(V i):SV_Target{ClipLanes(i.w);ClipAlpha(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a,i.w);return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma editor_sync_compilation
            #pragma multi_compile_instancing
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float2 uv:TEXCOORD1;};
            V vert(A a){UNITY_SETUP_INSTANCE_ID(a);V o;o.w=Bend(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.uv=a.uv;return o;}
            half4 frag(V i):SV_Target{ClipLanes(i.w);ClipAlpha(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a,i.w);return 0;}
            ENDHLSL
        }
    }
}
