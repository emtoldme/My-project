Shader "Reverb/Weathered Surface"
{
    Properties
    {
        _BaseMap("Albedo",2D)="white"{}
        _BaseColor("Surface",Color)=(.3,.32,.29,1)
        _PatinaColor("Patina",Color)=(.15,.18,.14,1)
        _Metallic("Metal",Range(0,1))=0
        _Smoothness("Smoothness",Range(0,1))=.3
        _Weathering("Wear",Range(0,1))=.5
        _Grain("Micro relief",Range(0,.02))=.003
        _Transmission("Thin fabric transmission",Range(0,1))=0
        _Cull("Cull",Float)=2
        _Cutoff("Cutoff",Float)=.5
        [HDR] _EmissionColor("Emission",Color)=(0,0,0,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Cull [_Cull]
        Pass
        {
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
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _ReverbWeather;
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST; half4 _BaseColor,_PatinaColor,_EmissionColor;
            float _Metallic,_Smoothness,_Weathering,_Grain,_Cull,_Cutoff,_Transmission;
            CBUFFER_END
            float Hash(float3 p) { p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z); }
            float Noise(float3 p)
            {
                float3 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(Hash(cell),Hash(cell+float3(1,0,0)),f.x),lerp(Hash(cell+float3(0,1,0)),Hash(cell+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(Hash(cell+float3(0,0,1)),Hash(cell+float3(1,0,1)),f.x),lerp(Hash(cell+float3(0,1,1)),Hash(cell+1),f.x),f.y),f.z);
            }
            struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
            struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
            V Vert(A a) { V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.fog=ComputeFogFactor(o.p.z);return o; }
            half4 Frag(V i, bool front:SV_IsFrontFace):SV_Target
            {
                float3 n=normalize(i.n)*(front?1:-1);
                float macro=Noise(i.w*2.1)*.65+Noise(i.w*8.7)*.35;
                float height=Noise(i.w*38)*_Grain;
                float3 dx=ddx(i.w),dy=ddy(i.w),r1=cross(dy,n),r2=cross(n,dx);
                float det=dot(dx,r1);
                n=normalize(n-sign(det)*(ddx(height)*r1+ddy(height)*r2)/max(abs(det),.00001));
                half dirt=smoothstep(.37,.73,macro)*_Weathering;
                SurfaceData s=(SurfaceData)0;
                s.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*lerp(_BaseColor.rgb,_PatinaColor.rgb,dirt);
                s.albedo*=.88+macro*.23;
                s.metallic=_Metallic*(1-dirt*.8);
                float wet=_ReverbWeather.y*saturate(n.y*.6+.35)*(1-dirt*.55);
                s.smoothness=clamp(_Smoothness+wet*.1-dirt*.2+(macro-.5)*.15,.08,.69);
                s.occlusion=1-dirt*.22;s.alpha=1;s.normalTS=half3(0,0,1);s.emission=_EmissionColor.rgb;
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.w));
                s.emission+=s.albedo*sun.color*_Transmission*(.3+pow(saturate(dot(-GetWorldSpaceNormalizeViewDir(i.w),sun.direction)),2));
                InputData d=(InputData)0;
                d.positionWS=i.w;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);
                d.shadowCoord=TransformWorldToShadowCoord(i.w);d.bakedGI=SampleSH(n);
                d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=1;d.vertexLighting=VertexLighting(i.w,n);
                half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
