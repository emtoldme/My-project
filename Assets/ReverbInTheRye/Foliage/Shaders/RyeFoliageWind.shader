Shader "ReverbInTheRye/FoliageWindURP"
{
    Properties
    {
        _BaseMap ("Foliage Atlas", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _WindDirection ("Wind Direction (XZ)", Vector) = (0.85,0,0.50,0)
        _WindStrength ("Wind Bend (meters)", Range(0,0.4)) = 0.13
        _WindSpeed ("Wind Speed", Range(0,6)) = 1.15
        _WindSpatialScale ("Wind Spatial Frequency", Range(0,4)) = 0.45
        _PlantHeight ("Plant Reference Height (meters)", Range(0.1,3)) = 2.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            float4 _WindDirection;
            float _WindStrength;
            float _WindSpeed;
            float _WindSpatialScale;
            float _PlantHeight;
        CBUFFER_END

        float3 BendFoliage(float3 posOS)
        {
            // The origin of the plant remains stationary. One direction is shared,
            // but each plant gets a different spatial phase based on world position.
            float3 pivotWS = TransformObjectToWorld(float3(0,0,0));
            float3 posWS = TransformObjectToWorld(posOS);
            float2 wind = _WindDirection.xz;
            wind = wind / max(length(wind),0.0001);
            float mask = pow(saturate(posOS.y / max(_PlantHeight,0.01)),1.7);
            float phase = _Time.y * _WindSpeed +
                          dot(pivotWS.xz,float2(0.73,1.37)) +
                          dot(posWS.xz,float2(0.83,0.56)) * _WindSpatialScale;
            float bend = (sin(phase) + .30*sin(phase*2.2 + pivotWS.z)) * _WindStrength * mask;
            posWS.xz += wind*bend;
            posWS.xz += float2(-wind.y,wind.x)*(.20*bend*sin(phase*.7+1.0));
            return posWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            Varyings Vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                UNITY_TRANSFER_INSTANCE_ID(v,o);
                float3 pWS=BendFoliage(v.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(pWS);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.shadowCoord=TransformWorldToShadowCoord(pWS);
                o.uv=TRANSFORM_TEX(v.uv,_BaseMap);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 n=normalize(i.normalWS);
                Light sun=GetMainLight(i.shadowCoord);
                // Thin grass blades use two-sided diffuse approximation.
                half ndl=abs(dot(n,sun.direction));
                half3 sky=max(SampleSH(n),half3(.20,.20,.20));
                half3 diffuse=sky + ndl*sun.color*sun.distanceAttenuation*sun.shadowAttenuation;
                half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                return half4(tex.rgb*_BaseColor.rgb*diffuse,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            float3 _LightDirection;
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings ShadowVert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                float3 pWS=BendFoliage(v.positionOS.xyz);
                float3 nWS=TransformObjectToWorldNormal(v.normalOS);
                float3 biased=ApplyShadowBias(pWS,nWS,_LightDirection);
                o.positionCS=TransformWorldToHClip(biased);
                #if UNITY_REVERSED_Z
                    o.positionCS.z=min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z=max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 ShadowFrag(Varyings i): SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings DepthVert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                o.positionCS=TransformWorldToHClip(BendFoliage(v.positionOS.xyz));
                return o;
            }
            half4 DepthFrag(Varyings i): SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
