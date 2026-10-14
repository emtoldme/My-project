Shader "Reverb/Exploration Ground"
{
    Properties
    {
        _Soil("Soil",2D)="white"{}
        _Meadow("Meadow ground",2D)="white"{}
        _Straw("Harvest stubble",2D)="white"{}
        _SoilNormal("Soil relief",2D)="bump"{}
        _MeadowNormal("Meadow relief",2D)="bump"{}
        _StrawNormal("Stubble relief",2D)="bump"{}
        _LayerScale("World tile sizes",Vector)=(8,8,8,0)
        _BaseColor("Tint",Color)=(1,1,1,1)
        _NormalStrength("Ground relief",Range(0,1))=.45
        _Roughness("Dry roughness",Range(.4,1))=.86
        _TrackStrength("Wheel ruts",Range(0,1))=.65
        _BaseMap("Depth pass compatibility",2D)="white"{}
        _Cull("Cull",Float)=2
        _Cutoff("Cutoff",Float)=.5
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_Soil);SAMPLER(sampler_Soil);TEXTURE2D(_Meadow);SAMPLER(sampler_Meadow);TEXTURE2D(_Straw);SAMPLER(sampler_Straw);
            TEXTURE2D(_SoilNormal);SAMPLER(sampler_SoilNormal);TEXTURE2D(_MeadowNormal);SAMPLER(sampler_MeadowNormal);TEXTURE2D(_StrawNormal);SAMPLER(sampler_StrawNormal);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor,_LayerScale,_BaseMap_ST;float _Cull,_Cutoff,_NormalStrength,_Roughness,_TrackStrength;
            CBUFFER_END
            float4 _ReverbWeather;float _ReverbFieldPhase;
            float Hash(float2 p){p=frac(p*float2(.1031,.11369));p+=dot(p,p.yx+19.19);return frac(p.x*p.y);}
            float Noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);}
            struct A{float4 p:POSITION;half4 color:COLOR;};
            struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 weights:TEXCOORD1;half fog:TEXCOORD2;};
            V Vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.weights=a.color.rgb;o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 Frag(V i):SV_Target
            {
                half3 weights=max(i.weights,0);weights/=max(dot(weights,1),.001);
                float2 a=i.w.xz/max(_LayerScale.x,.1),b=i.w.xz/max(_LayerScale.y,.1),c=i.w.xz/max(_LayerScale.z,.1);
                // Height-biased, normalized transition lets straw islands sit over soil.
                half3 soil=SAMPLE_TEXTURE2D(_Soil,sampler_Soil,a).rgb,meadow=SAMPLE_TEXTURE2D(_Meadow,sampler_Meadow,b).rgb,straw=SAMPLE_TEXTURE2D(_Straw,sampler_Straw,c).rgb;
                meadow*=half3(.88,.94,.81);
                half3 heights=half3(dot(soil,half3(.3,.5,.2)),dot(meadow,half3(.3,.5,.2)),dot(straw,half3(.3,.5,.2)));
                weights*=max(.05,heights*.55+weights);weights/=max(dot(weights,1),.001);
                half3 albedo=soil*weights.x+meadow*weights.y+straw*weights.z;
                half3 relief=UnpackNormal(SAMPLE_TEXTURE2D(_SoilNormal,sampler_SoilNormal,a))*weights.x+UnpackNormal(SAMPLE_TEXTURE2D(_MeadowNormal,sampler_MeadowNormal,b))*weights.y+UnpackNormal(SAMPLE_TEXTURE2D(_StrawNormal,sampler_StrawNormal,c))*weights.z;
                float macro=Noise(i.w.xz*.13),fine=Noise(i.w.xz*.73);
                float ns=round(i.w.x/72)*72+sin(i.w.z*.037+_ReverbFieldPhase)*4+sin(i.w.z*.011)*2;
                float ew=round(i.w.z/72)*72+sin(i.w.x*.041+_ReverbFieldPhase)*4;
                float road=min(abs(i.w.x-ns),abs(i.w.z-ew));
                float rut=exp2(-pow((road-.78)*6,2))*weights.x*_TrackStrength;
                // Authored home roads retain their painted layout, not procedural ruts.
                rut*=smoothstep(42,52,max(abs(i.w.x),abs(i.w.z)));
                half wet=_ReverbWeather.y*saturate(.32+macro*.4+rut*.3);
                half puddle=smoothstep(.68,.88,fine+rut*.22)*weights.x*_ReverbWeather.y;
                albedo*=lerp(.88,1.07,macro)*(1-rut*.16)*lerp(1,.74,wet);
                relief.xy*=_NormalStrength*lerp(1,.2,puddle);relief=normalize(relief);
                SurfaceData s=(SurfaceData)0;s.albedo=albedo*_BaseColor.rgb;s.alpha=1;s.occlusion=lerp(.87,1,heights.x)*(1-rut*.1);s.normalTS=half3(0,0,1);
                s.smoothness=lerp(1-_Roughness+wet*.10,.63,puddle);
                InputData d=(InputData)0;d.positionWS=i.w;d.normalWS=normalize(float3(relief.x,relief.z,relief.y));d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);
                d.shadowCoord=TransformWorldToShadowCoord(i.w);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=1;
                half4 color=UniversalFragmentPBR(d,s);color.rgb=MixFog(color.rgb,i.fog);return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
