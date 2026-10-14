Shader "Reverb/Red Ghost Drapery"
{
    Properties
    {
        _BaseColor("Crimson cloth",Color)=(.66,.055,.055,1)
        _VisualTime("Run clock",Float)=0
        _SerialPhase("Echo phase",Float)=0
        _Emerge("Arrival",Range(0,1))=1
        _Dissolve("Departure",Range(0,1))=0
        _Hit("Hit",Range(0,1))=0
        _Flow("Velocity",Float)=0
        _Face("Face void",Range(0,1))=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
        Pass
        {
            Name "GhostForward"
            Tags {"LightMode"="UniversalForward"}
            Blend One OneMinusSrcAlpha
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _VisualTime,_SerialPhase,_Emerge,_Dissolve,_Hit,_Flow,_Face;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
            Varyings Vert(Attributes i)
            {
                Varyings o;float3 p=i.positionOS.xyz;float h=saturate(p.y/1.8);
                float tail=pow(1-h,2);float phase=_VisualTime*3.3+_SerialPhase;
                p.x+=sin(phase+h*9+i.uv.x*6.28)*(.025+tail*.075);
                p.z+=cos(phase*.83+h*7+i.uv.x*9)*tail*.08-tail*_Flow*.035;
                p.y+=sin(i.uv.x*18.85+phase)*tail*.035+_Dissolve*.3*(1-h);
                p.y*=lerp(.72,1,smoothstep(0,1,_Emerge));
                o.positionWS=TransformObjectToWorld(p);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float h=saturate(i.uv.y);
                float folds=.76+.24*sin(i.uv.x*50.26+sin(h*8+_VisualTime*2+_SerialPhase)*.6);
                float noise=frac(sin(dot(floor(i.uv*float2(48,32)),float2(12.9898,78.233)))*43758.5453);
                float fringe=smoothstep(.015,.19,h+.035*sin(i.uv.x*75));
                float fade=saturate((1-_Dissolve)*1.3)*smoothstep(_Dissolve-.1,_Dissolve+.14,h+noise*.17);
                half alpha=lerp(.62,.98,smoothstep(.12,.45,h))*fringe*_Emerge*fade;
                // Dense cloth writes depth, so the far side cannot draw over the
                // near side as horizontal transparency bands. Empty fringe stays empty.
                clip(alpha-.025);
                Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl=abs(dot(normalize(i.normalWS),light.direction));
                half3 illumination=SampleSH(normalize(i.normalWS))*.7+light.color*(.19+.55*ndl*light.shadowAttenuation);
                half3 red=_BaseColor.rgb*folds*(max(illumination,half3(.3,.3,.3)))+_BaseColor.rgb*.17;
                // Face is an opaque dark opening, with two restrained, red slit eyes.
                float eyes=exp(-pow((i.uv.x-.34)*35,2)-pow((i.uv.y-.56)*30,2))
                          +exp(-pow((i.uv.x-.66)*35,2)-pow((i.uv.y-.56)*30,2));
                red=lerp(red,half3(.018,.004,.007)+half3(.7,.045,.025)*eyes,_Face);
                red+=_Hit*half3(.22,.035,.015);red+=_Dissolve*.12*_BaseColor.rgb;
                red=MixFog(red,i.fog);return half4(red*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
