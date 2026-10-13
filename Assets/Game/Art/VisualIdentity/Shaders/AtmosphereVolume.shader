Shader "Reverb/Atmosphere Volume"
{
    Properties
    {
        [HDR] _BaseColor("Light / fog",Color)=(.7,.8,.9,.1)
        _Density("Density",Range(0,.5))=.06
        _Mode("0 mist / 1 light volume",Float)=0
        _Cull("Cull",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1" "RenderType"="Transparent"}
        Blend One OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull [_Cull]
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;float _Density,_Mode,_Cull;
            CBUFFER_END
            float4 _ReverbWeather;
            float Hash(float3 p) {p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
            float Noise(float3 p)
            {
                float3 c=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(Hash(c),Hash(c+float3(1,0,0)),f.x),lerp(Hash(c+float3(0,1,0)),Hash(c+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(Hash(c+float3(0,0,1)),Hash(c+float3(1,0,1)),f.x),lerp(Hash(c+float3(0,1,1)),Hash(c+1),f.x),f.y),f.z);
            }
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float2 uv:TEXCOORD1;half4 color:COLOR;float eye:TEXCOORD2;};
            V Vert(A a) {V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.uv=a.uv;o.color=a.color;o.eye=-TransformWorldToView(o.w).z;return o;}
            half4 Frag(V i):SV_Target
            {
                float depth=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.p)),_ZBufferParams);
                float alpha;
                if(_Mode<.5)
                {
                    float2 uv=i.uv*2-1;
                    float shape=pow(saturate(1-dot(uv,uv)),2);
                    float noise=Noise(float3(i.uv*4,_ReverbWeather.z*.11))*Noise(float3(i.uv*9+3,_ReverbWeather.z*.07));
                    alpha=shape*noise*3*_BaseColor.a*i.color.a*saturate((depth-i.eye)*1.2)*saturate(i.eye*.5);
                }
                else
                {
                    float3 origin=TransformWorldToObject(_WorldSpaceCameraPos),end=TransformWorldToObject(i.w);
                    float3 dir=end-origin,inv=rcp(dir+float3(.000001,.000001,.000001));
                    float3 t0=(float3(-1,-1,0)-origin)*inv,t1=(float3(1,1,1)-origin)*inv;
                    float3 nearT=min(t0,t1),farT=max(t0,t1);
                    float a=max(0,max(nearT.x,max(nearT.y,nearT.z)));
                    float b=min(1,min(farT.x,min(farT.y,farT.z)));
                    b=min(b,depth/max(i.eye,.001));
                    float step=max(0,b-a)/16,integral=0;
                    [unroll] for(int k=0;k<16;k++)
                    {
                        float t=a+(k+.5)*step;
                        float3 p=origin+dir*t;
                        float radial=length(p.xy)/max(p.z,.0001);
                        float density=saturate((1-radial)*3)*smoothstep(0,.08,p.z)*(1-p.z*.65);
                        float3 world=TransformObjectToWorld(p);
                        density*=.4+Noise(world*.65+float3(_ReverbWeather.z*.07,0,0))*.6;
                        integral+=density*step;
                    }
                    alpha=(1-exp(-integral*length(i.w-_WorldSpaceCameraPos)*_Density))*_BaseColor.a;
                }
                alpha=saturate(alpha);
                return half4(_BaseColor.rgb*i.color.rgb*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
