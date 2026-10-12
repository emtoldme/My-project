Shader "ReverbInTheRye/VFX/ResonancePulseURP"
{
    Properties
    {
        [HDR] _Color ("Primary Light", Color) = (0.47,0.85,0.83,1)
        [HDR] _EdgeColor ("Interference Edge", Color) = (0.59,0.50,0.76,1)
        _Opacity ("Opacity", Range(0,2)) = 1
        _RingRadius ("Normalized Ring Radius", Range(0.1,0.9)) = 0.62
        _RingWidth ("Ring Half Width", Range(0.005,0.3)) = 0.045
        _CoreMix ("Core Glow Mix", Range(0,1)) = 0
        _Distortion ("Analog Interference", Range(0,0.1)) = 0.025
        _Phase ("Phase Offset", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "ResonanceUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _EdgeColor;
                float _Opacity;
                float _RingRadius;
                float _RingWidth;
                float _CoreMix;
                float _Distortion;
                float _Phase;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv * 2.0 - 1.0;
                float theta = atan2(p.y, p.x);
                float phase = _Phase + _Time.y * 3.0;
                float radialJitter = sin(theta * 17.0 + sin(theta * 5.0 - phase) * 2.4 + phase) * 0.65;
                radialJitter += sin(theta * 53.0 - phase * 2.7) * 0.35;
                float radius = length(p) + radialJitter * _Distortion;

                float delta = abs(radius - _RingRadius);
                float ring = 1.0 - smoothstep(_RingWidth, _RingWidth + 0.068, delta);
                ring *= 0.78 + 0.22 * sin(theta * 24.0 + phase);
                float core = exp(-dot(p, p) * 12.0);
                float mask = saturate(lerp(ring, core, _CoreMix));
                float edgeFactor = saturate((radius - _RingRadius + _RingWidth) / max(_RingWidth * 2.0, 0.001));
                float3 lightColor = lerp(_Color.rgb, _EdgeColor.rgb, edgeFactor);
                // The shader contains no texture requirement and does not alter gameplay data.
                return half4(lightColor, saturate(mask * _Opacity));
            }
            ENDHLSL
        }
    }
}
