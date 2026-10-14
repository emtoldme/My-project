Shader "Reverb/Volumetric Wheat"
{
    Properties
    {
        _StemColor("Dry stem",Color)=(.31,.32,.13,1)
        _LeafColor("Thin leaf",Color)=(.49,.46,.19,1)
        _EarColor("Harvest grain",Color)=(.76,.58,.28,1)
        _TransmissionColor("Filtered sunlight",Color)=(1,.76,.34,1)
        _Transmission("Thin surface transmission",Range(0,2))=.72
        _WindDirection("Wind XZ",Vector)=(.85,0,.5,0)
        _WindStrength("Bend radians",Range(0,.6))=.24
        _WindSpeed("Wave speed",Float)=1.1
        _UseExternalTime("Rule clock",Float)=1
        _VisualTime("Time",Float)=0
        _ProceduralField("Generated dressing",Float)=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        float4 _ReverbPlayer,_ReverbWeather,_ReverbWind;
        float4 _ReverbPickupReveal[8];int _ReverbPickupCount;
        CBUFFER_START(UnityPerMaterial)
        half4 _StemColor,_LeafColor,_EarColor,_TransmissionColor;
        float4 _WindDirection;
        float _Transmission,_WindStrength,_WindSpeed,_UseExternalTime,_VisualTime,_ProceduralField;
        CBUFFER_END
        struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 root:TEXCOORD1;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
        struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;float2 uv:TEXCOORD2;half3 paint:TEXCOORD3;float fog:TEXCOORD4;float2 root:TEXCOORD5;};
        float3 RotateBend(float3 p,float2 direction,float angle)
        {
            float sine,cosine;sincos(angle,sine,cosine);
            float parallel=dot(p.xz,direction);
            return float3(p.x+direction.x*(parallel*(cosine-1)+p.y*sine),p.y*cosine-parallel*sine,p.z+direction.y*(parallel*(cosine-1)+p.y*sine));
        }
        void Deform(A a,out float3 w,out float3 normal,out float2 rootXZ)
        {
            float3 root=TransformObjectToWorld(float3(a.root.x,0,a.root.y));rootXZ=root.xz;
            float3 relative=TransformObjectToWorld(a.p.xyz)-root;
            float t=lerp(_Time.y,_VisualTime,_UseExternalTime);
            float h=saturate(relative.y/max(a.root.z,.1));
            float2 direction=normalize(lerp(_WindDirection.xz,_ReverbWind.xy,_ReverbWind.w)+float2(.0001,0));
            // Shared world coordinates: fronts travel across chunk seams rather than shaking each plant independently.
            float phase=dot(root.xz,direction)*.43-t*_WindSpeed;
            float front=.5+.5*sin(dot(root.xz,direction)*.12-t*.63);
            float swell=sin(phase)*(.48+.4*front)+.22*sin(phase*.53+t*.24);
            float angle=_WindStrength*(.24+swell)*(1+_ReverbWind.z*2.1)*h;
            float2 away=root.xz-_ReverbPlayer.xz;
            float reveal=(1-smoothstep(1.2,3.8,length(away)))*_ReverbPlayer.w;
            [loop]for(int j=0;j<min(_ReverbPickupCount,8);j++)
                reveal=max(reveal,(1-smoothstep(.35,1.1,distance(root.xz,_ReverbPickupReveal[j].xz)))*.88);
            relative.y*=lerp(.18,1,a.color.r)*(1-reveal*.7);
            direction=normalize(direction+normalize(away+float2(.0001,.0001))*reveal*.9);
            angle+=reveal*.36*h;
            relative=RotateBend(relative,direction,angle);
            float flutter=sin(t*3.8+dot(root.xz,float2(1.7,2.1))+a.root.w*6.28)*a.uv.y*.014*(1+_ReverbWind.z)*h;
            relative.xz+=float2(-direction.y,direction.x)*flutter;
            w=root+relative;
            normal=normalize(RotateBend(TransformObjectToWorldNormal(a.n),direction,angle*(1+h*.65)));
        }
        V Vertex(A a)
        {
            UNITY_SETUP_INSTANCE_ID(a);V o;float3 n;
            Deform(a,o.w,n,o.root);o.n=n;o.p=TransformWorldToHClip(o.w);o.uv=a.uv;o.paint=a.color.rgb;o.fog=ComputeFogFactor(o.p.z);return o;
        }
        void RevealClip(float2 root)
        {
            // Whole-shoot stochastic fade only beside the player. Ordinary foliage is fully opaque geometry.
            float visible=lerp(1,smoothstep(.65,1.6,distance(root,_ReverbPlayer.xz)),_ReverbPlayer.w);
            float noise=frac(sin(dot(floor(root*73),float2(12.9898,78.233)))*43758.5453);
            clip(visible-noise);
        }
        half3 Illuminate(half3 albedo,half3 n,half3 view,half thin,Light light)
        {
            half diffuse=saturate(dot(n,light.direction)*.78+.22);
            half back=pow(saturate(dot(-view,light.direction)),4);
            half through=(.18+.82*back)*saturate(dot(-n,light.direction)+.35)*thin*_Transmission;
            return (albedo*diffuse+albedo*_TransmissionColor.rgb*through)*light.color*light.distanceAttenuation*light.shadowAttenuation;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma editor_sync_compilation
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            half4 Fragment(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                RevealClip(i.root);
                half3 n=normalize(i.n)*IS_FRONT_VFACE(face,1,-1);
                half leaf=step(.2,i.uv.x)*(1-step(.65,i.uv.x));
                half ear=step(.65,i.uv.x),awn=step(.95,i.uv.x);
                half3 albedo=lerp(_StemColor.rgb,_LeafColor.rgb,leaf);
                albedo=lerp(albedo,_EarColor.rgb,ear);
                albedo*=lerp(half3(1,1,1),half3(1.16,1.02,.8),leaf*smoothstep(.45,1,i.uv.y));
                albedo*=lerp(half3(.72,.87,.72),half3(1.05,1,.88),i.paint.g);
                albedo*=lerp(.88,1.08,i.paint.b)*lerp(1,.78,_ReverbWeather.y);
                half thin=lerp(.1,.9,leaf);thin=lerp(thin,.22,ear);thin=lerp(thin,1,awn);
                half3 view=GetWorldSpaceNormalizeViewDir(i.w);
                half3 sky=max(SampleSH(n),half3(.09,.105,.12));
                half3 color=albedo*sky*.9;
                Light main=GetMainLight(TransformWorldToShadowCoord(i.w),i.w,half4(1,1,1,1));
                color+=Illuminate(albedo,n,view,thin,main);
                InputData inputData=(InputData)0;inputData.positionWS=i.w;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);
                uint count=GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    Light extra=GetAdditionalLight(lightIndex,i.w,half4(1,1,1,1));
                    color+=Illuminate(albedo,n,view,thin,extra);
                LIGHT_LOOP_END
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
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
                o.p=TransformWorldToHClip(ApplyShadowBias(o.w,o.n,lightDirection));
                o.p=ApplyShadowClamping(o.p);
                return o;
            }
            half4 ShadowFragment(V i):SV_Target {RevealClip(i.root);return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ColorMask R
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment DepthFragment
            #pragma editor_sync_compilation
            #pragma multi_compile_instancing
            half4 DepthFragment(V i):SV_Target {RevealClip(i.root);return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags {"LightMode"="DepthNormals"}
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment NormalsFragment
            #pragma editor_sync_compilation
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 NormalsFragment(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                RevealClip(i.root);half3 n=normalize(i.n)*IS_FRONT_VFACE(face,1,-1);
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
