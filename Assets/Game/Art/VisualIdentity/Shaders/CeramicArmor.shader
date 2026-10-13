Shader "Reverb/Ceramic Armor"
{
    Properties
    {
        _BaseMap("Original authored atlas",2D)="white"{}
        _BaseColor("Armor palette",Color)=(0.55,0.78,0.77,1)
        _Metallic("Metal",Range(0,1))=0.22
        _Smoothness("Smoothness",Range(0,1))=0.27
        [HDR] _EmissionColor("Hit light",Color)=(0,0,0,1)
        _Cull("Cull",Float)=2
        _Cutoff("Cutoff",Float)=0.5
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            float4 _ReverbWeather;
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;half4 _BaseColor,_EmissionColor;half _Metallic,_Smoothness,_Cutoff,_Cull;
            CBUFFER_END
            struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
            struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
            V vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 frag(V i):SV_Target
            {
                half3 atlas=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                half luminance=dot(atlas,half3(.22,.65,.13));
                half painted=smoothstep(.025,.18,luminance);
                half isGold=smoothstep(1.05,1.7,atlas.r/max(atlas.b,.015))*smoothstep(.08,.2,atlas.r);
                half isGreen=saturate((atlas.g-atlas.r*.85)*5);
                half3 panel=lerp(half3(.7,.75,.72),_BaseColor.rgb,isGreen);
                panel=lerp(panel,half3(.5,.29,.09),isGold*.65);
                panel*=lerp(.76,1,saturate(luminance*2.4));
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=lerp(half3(.055,.075,.08),panel,painted);
                surface.alpha=1;surface.normalTS=half3(0,0,1);surface.occlusion=1;
                half wet=_ReverbWeather.y*saturate(normalize(i.n).y*.7+.3);
                float grain=frac(sin(dot(floor(i.w*85),float3(12.9898,78.233,37.719)))*43758.5453);
                float patina=.5+.5*sin(i.w.x*3.7+sin(i.w.z*4.3)+i.w.y*6.1);
                half grime=saturate((1-luminance)*.28+patina*.12);
                surface.albedo*=lerp(.8,1,patina)*lerp(1,.9,wet);
                surface.metallic=lerp(.78,min(_Metallic,.08),painted);
                surface.metallic=lerp(surface.metallic,.78,isGold);
                surface.smoothness=clamp(_Smoothness+grain*.09-patina*.15+wet*.18,.12,.53);
                surface.occlusion=lerp(.65,1,painted)*(1-grime*.2);
                surface.emission=_EmissionColor.rgb;
                InputData data=(InputData)0;
                data.positionWS=i.w;data.normalWS=normalize(i.n);data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);
                data.shadowCoord=TransformWorldToShadowCoord(i.w);data.bakedGI=SampleSH(data.normalWS);
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);data.shadowMask=half4(1,1,1,1);
                data.vertexLighting=VertexLighting(i.w,data.normalWS);
                half4 color=UniversalFragmentPBR(data,surface);color.rgb=MixFog(color.rgb,i.fog);return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
