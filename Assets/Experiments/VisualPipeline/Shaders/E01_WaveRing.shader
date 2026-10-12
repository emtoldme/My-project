Shader "Waffle/Experiments/E01_WaveRing"
{
    Properties
    {
        [HDR] _BaseColor ("Color", Color) = (0.35, 0.90, 1, 1)
        _Radius ("Radius", Float) = 0.4
        _Width ("Width", Float) = 0.08
        _QuadMeters ("Quad Meters", Float) = 6
        _Cutoff ("Alpha Clip", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
            "IgnoreProjector" = "True"
        }
        Pass
        {
            Name "E01WaveClip"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Blend One Zero
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Radius;
                float _Width;
                float _QuadMeters;
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * _QuadMeters;
                float band = abs(length(p) - _Radius);
                float alpha = band < max(_Width, 0.0001) ? 1 : 0;
                clip(alpha - _Cutoff);
                return half4(_BaseColor.rgb, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
