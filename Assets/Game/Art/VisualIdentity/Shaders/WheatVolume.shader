Shader "Reverb/Volumetric Wheat"
{
    Properties
    {
        _StemColor("Dry stem",Color)=(.25,.28,.11,1)
        _LeafColor("Thin leaf",Color)=(.31,.34,.12,1)
        _EarColor("Harvest husk",Color)=(.60,.44,.23,1)
        _FiberMap("R fiber / G roughness / B thickness / A wear",2D)="white"{}
        [Normal] _FiberNormal("Husk and leaf fibers",2D)="bump"{}
        _NormalStrength("Fiber relief",Range(0,1))=.4
        _Roughness("Dry roughness",Range(.5,1))=.84
        _Sheen("Dry fiber sheen",Range(0,.3))=.075
        _TransmissionColor("Filtered sunlight",Color)=(1,.84,.52,1)
        _Transmission("Thin surface transmission",Range(0,2))=.85
        _Absorption("Tissue absorption",Range(1,6))=3.2
        _WindDirection("Wind XZ",Vector)=(.85,0,.5,0)
        _WindStrength("Bend radians",Range(0,.6))=.24
        _WindSpeed("Wave speed",Float)=1.1
        _ContactStrength("Soft contact",Range(0,1.5))=1
        _UseExternalTime("Rule clock",Float)=1
        _VisualTime("Time",Float)=0
        _ProceduralField("Generated dressing",Float)=1
        _InstancedField("Shared mesh instances",Float)=0
        _DetailQuality("0 near / 1 middle / 2 far",Float)=0
        _PatchPaint("Patch ripeness",Vector)=(1,.6,.5,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Cull Off
        HLSLINCLUDE
        #define _SPECULAR_SETUP 1
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_FiberMap);SAMPLER(sampler_FiberMap);
        TEXTURE2D(_FiberNormal);SAMPLER(sampler_FiberNormal);
        TEXTURE2D(_ReverbWheatContact);SAMPLER(sampler_ReverbWheatContact);
        float4 _ReverbWheatContactRect,_ReverbWeather,_ReverbWind;
        CBUFFER_START(UnityPerMaterial)
        half4 _StemColor,_LeafColor,_EarColor,_TransmissionColor;
        float4 _WindDirection,_PatchPaint;
        float _Transmission,_Absorption,_NormalStrength,_Roughness,_Sheen,_ContactStrength;
        float _WindStrength,_WindSpeed,_UseExternalTime,_VisualTime,_ProceduralField,_InstancedField,_DetailQuality;
        CBUFFER_END
        struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 root:TEXCOORD1;float4 surface:TEXCOORD2;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
        struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;float2 uv:TEXCOORD2;half3 paint:TEXCOORD3;float fog:TEXCOORD4;float4 surface:TEXCOORD5;float3 rest:TEXCOORD6;};
        float3 RotateBend(float3 p,float2 direction,float angle)
        {
            float sine,cosine;sincos(angle,sine,cosine);float parallel=dot(p.xz,direction);
            return float3(p.x+direction.x*(parallel*(cosine-1)+p.y*sine),p.y*cosine-parallel*sine,p.z+direction.y*(parallel*(cosine-1)+p.y*sine));
        }
        void Deform(A a,out float3 w,out float3 normal)
        {
            float3 root=TransformObjectToWorld(float3(a.root.x,0,a.root.y));
            float3 relative=TransformObjectToWorld(a.p.xyz)-root;
            float rootHeight=a.root.z;
            if(_InstancedField>.5)rootHeight=length(mul((float3x3)GetObjectToWorldMatrix(),float3(0,a.root.z,0)));
            float t=lerp(_Time.y,_VisualTime,_UseExternalTime),h=saturate(relative.y/max(rootHeight,.1));
            float2 direction=normalize(lerp(_WindDirection.xz,_ReverbWind.xy,_ReverbWind.w)+float2(.0001,0));
            float phase=dot(root.xz,direction)*.43-t*_WindSpeed;
            float front=.5+.5*sin(dot(root.xz,direction)*.12-t*.63);
            float swell=sin(phase)*(.48+.4*front)+.22*sin(phase*.53+t*.24);
            float windAngle=_WindStrength*(.24+swell)*(1+_ReverbWind.z*2.1)*h;
            // Preserve stalk length. Contact stores bend vectors with damped recovery, never shrinking or clipping plants.
            float2 fieldUV=(root.xz-_ReverbWheatContactRect.xy)/max(_ReverbWheatContactRect.z,1);
            float edge=smoothstep(0,.04,min(min(fieldUV.x,fieldUV.y),min(1-fieldUV.x,1-fieldUV.y)));
            float2 contact=SAMPLE_TEXTURE2D_LOD(_ReverbWheatContact,sampler_ReverbWheatContact,fieldUV,0).rg*edge*_ReverbWheatContactRect.w*_ContactStrength;
            float contactAngle=length(contact)*h;
            float2 contactDirection=contact/max(length(contact),.0001);
            relative.y*=max(.08,a.color.r);
            relative=RotateBend(relative,direction,windAngle);
            relative=RotateBend(relative,contactDirection,contactAngle);
            float flutter=sin(t*3.8+dot(root.xz,float2(1.7,2.1))+a.root.w*6.28)*a.uv.y*.014*(1+_ReverbWind.z)*h;
            relative.xz+=float2(-direction.y,direction.x)*flutter;
            w=root+relative;
            normal=RotateBend(TransformObjectToWorldNormal(a.n),direction,windAngle*(1+h*.65));
            normal=normalize(RotateBend(normal,contactDirection,contactAngle*(1+h*.65)));
        }
        V Vertex(A a)
        {
            UNITY_SETUP_INSTANCE_ID(a);
            if(_InstancedField>.5)
            {
                float2 root=TransformObjectToWorld(float3(0,0,0)).xz;
                float variation=frac(sin(dot(root,float2(12.9898,78.233)))*43758.5453);
                a.color=half4(1,_PatchPaint.y,variation,1);
            }
            V o;float3 n;Deform(a,o.w,n);o.n=n;o.p=TransformWorldToHClip(o.w);
            o.uv=a.uv;o.paint=a.color.rgb;o.fog=ComputeFogFactor(o.p.z);o.surface=a.surface;o.rest=a.p.xyz;return o;
        }
        half3 FiberNormal(V i,half3 n)
        {
            half3 detail=UnpackNormalScale(SAMPLE_TEXTURE2D(_FiberNormal,sampler_FiberNormal,i.surface.xy),_NormalStrength);
            // Cotangent frame from real tissue coordinates, independent of wind and chunk position.
            float3 dx=ddx(i.w),dy=ddy(i.w);float2 tx=ddx(i.surface.xy),ty=ddy(i.surface.xy);
            float3 t=dx*ty.y-dy*tx.y,b=dy*tx.x-dx*ty.x;
            float orientation=dot(cross(t,b),n)<0?-1:1;
            t=t-n*dot(t,n);float3 fallback=abs(n.y)<.95?normalize(cross(n,float3(0,1,0))):float3(1,0,0);
            t=dot(t,t)>.00000001?normalize(t):fallback;b=normalize(cross(n,t))*orientation;
            return normalize(t*detail.x+b*detail.y+n*detail.z);
        }
        half3 Transmitted(half3 albedo,half3 n,half3 view,half thickness,Light light)
        {
            half back=saturate(dot(-n,light.direction));
            half forward=pow(saturate(dot(-view,light.direction)),3);
            half through=exp2(-_Absorption*thickness)*back*(.25+.75*forward)*_Transmission;
            half sheen=pow(1-saturate(dot(n,view)),5)*saturate(dot(n,light.direction)*.5+.5)*_Sheen*(1-_ReverbWeather.y);
            return albedo*(through*_TransmissionColor.rgb+sheen)*light.color*light.distanceAttenuation*light.shadowAttenuation;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma editor_sync_compilation
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            half4 Fragment(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                half3 geometric=normalize(i.n)*IS_FRONT_VFACE(face,1,-1),n=geometric;
                if(_DetailQuality<.5)n=FiberNormal(i,geometric);
                half leaf=step(.2,i.uv.x)*(1-step(.65,i.uv.x)),ear=step(.65,i.uv.x),awn=step(.95,i.uv.x);
                half4 fiber=half4(.5,.6,.5,1);
                if(_DetailQuality<1.5)fiber=SAMPLE_TEXTURE2D(_FiberMap,sampler_FiberMap,i.surface.xy);
                half3 albedo=lerp(_StemColor.rgb,_LeafColor.rgb,leaf);albedo=lerp(albedo,_EarColor.rgb,ear);
                half dryTip=leaf*smoothstep(.48,1,i.surface.y)*(.35+.65*i.paint.g);
                albedo=lerp(albedo,_EarColor.rgb*.88,dryTip*.55);
                half huskWear=(.5+.5*sin(i.rest.y*37+i.paint.b*14))*ear;
                albedo=lerp(albedo,half3(.68,.55,.34),huskWear*.18);
                half crease=pow(saturate(1-abs(i.surface.x-.5)*8),3)*ear;
                albedo*=1-crease*.1*sin(saturate(i.surface.y)*PI);
                albedo*=lerp(half3(.78,.88,.72),half3(1.06,1,.86),i.paint.g);
                albedo*=lerp(.88,1.06,i.paint.b)*lerp(.82,1.08,fiber.r)*lerp(1,.84,_ReverbWeather.y);
                half roughness=clamp(_Roughness+(fiber.g-.5)*.12-leaf*.045-_ReverbWeather.y*.14,.6,.97);
                half thickness=max(.025,i.surface.z*lerp(.85,1.15,fiber.b));
                // Every detail level responds to farm lamps, broadcast lights and
                // the same physical sun. Only surface microdetail is reduced.
                SurfaceData surface=(SurfaceData)0;surface.albedo=albedo;surface.specular=half3(.025,.025,.025);
                surface.metallic=0;surface.smoothness=1-roughness;surface.occlusion=saturate(i.surface.w*lerp(.92,1,fiber.a));surface.alpha=1;
                InputData inputData=(InputData)0;inputData.positionWS=i.w;inputData.normalWS=n;
                inputData.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);inputData.shadowCoord=TransformWorldToShadowCoord(i.w);
                inputData.bakedGI=SampleSH(n);inputData.shadowMask=half4(1,1,1,1);
                inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);
                half3 color=UniversalFragmentPBR(inputData,surface).rgb;
                Light main=GetMainLight(inputData.shadowCoord,i.w,half4(1,1,1,1));
                color+=Transmitted(albedo,geometric,inputData.viewDirectionWS,thickness,main)*surface.occlusion;
                #if defined(_ADDITIONAL_LIGHTS)
                #if USE_CLUSTER_LIGHT_LOOP
                [loop]for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light extra=GetAdditionalLight(lightIndex,i.w,half4(1,1,1,1));
                    color+=Transmitted(albedo,geometric,inputData.viewDirectionWS,thickness,extra)*surface.occlusion;
                }
                #endif
                uint count=GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    Light extra=GetAdditionalLight(lightIndex,i.w,half4(1,1,1,1));
                    color+=Transmitted(albedo,geometric,inputData.viewDirectionWS,thickness,extra)*surface.occlusion;
                LIGHT_LOOP_END
                #endif
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ColorMask 0
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowVertex
            #pragma fragment EmptyFragment
            #pragma editor_sync_compilation
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection,_LightPosition;
            V ShadowVertex(A a)
            {
                V o=Vertex(a);float3 lightDirection=_LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                lightDirection=normalize(_LightPosition-o.w);
                #endif
                o.p=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(o.w,o.n,lightDirection)));return o;
            }
            half4 EmptyFragment(V i):SV_Target {return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ColorMask R
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment EmptyFragment
            #pragma editor_sync_compilation
            #pragma multi_compile_instancing
            half4 EmptyFragment(V i):SV_Target {return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags {"LightMode"="DepthNormals"}
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment NormalsFragment
            #pragma editor_sync_compilation
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 NormalsFragment(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                half3 n=FiberNormal(i,normalize(i.n)*IS_FRONT_VFACE(face,1,-1));
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct=PackNormalOctQuadEncode(n);return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
                #else
                return half4(n,0);
                #endif
            }
            ENDHLSL
        }
    }
}
