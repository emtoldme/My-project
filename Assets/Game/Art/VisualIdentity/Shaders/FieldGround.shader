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
            float4 _BaseColor,_LayerScale,_BaseMap_ST;float _Cull,_Cutoff;
            CBUFFER_END
            float4 _ReverbWeather;
            struct A{float4 p:POSITION;half4 color:COLOR;};
            struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 weights:TEXCOORD1;half fog:TEXCOORD2;};
            V Vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.weights=a.color.rgb;o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 Frag(V i):SV_Target
            {
                half3 weights=max(i.weights,0);weights/=max(dot(weights,1),.001);
                float2 a=i.w.xz/max(_LayerScale.x,.1),b=i.w.xz/max(_LayerScale.y,.1),c=i.w.xz/max(_LayerScale.z,.1);
                half3 albedo=SAMPLE_TEXTURE2D(_Soil,sampler_Soil,a).rgb*weights.x+SAMPLE_TEXTURE2D(_Meadow,sampler_Meadow,b).rgb*weights.y+SAMPLE_TEXTURE2D(_Straw,sampler_Straw,c).rgb*weights.z;
                half3 relief=UnpackNormal(SAMPLE_TEXTURE2D(_SoilNormal,sampler_SoilNormal,a))*weights.x+UnpackNormal(SAMPLE_TEXTURE2D(_MeadowNormal,sampler_MeadowNormal,b))*weights.y+UnpackNormal(SAMPLE_TEXTURE2D(_StrawNormal,sampler_StrawNormal,c))*weights.z;
                half wet=_ReverbWeather.y*(.45+.25*sin(i.w.x*.7+sin(i.w.z*.45))+.25*sin(i.w.z*.9));
                SurfaceData s=(SurfaceData)0;s.albedo=albedo*_BaseColor.rgb*lerp(1,.8,wet);s.alpha=1;s.occlusion=1;s.normalTS=half3(0,0,1);s.smoothness=.13+wet*.26;
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
