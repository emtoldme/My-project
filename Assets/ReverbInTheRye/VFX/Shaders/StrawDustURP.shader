Shader "ReverbInTheRye/VFX/StrawDustURP"
{
    Properties
    {
        _Tint ("Particle Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "StrawDustUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                // Elliptical falloff: particle sizeX / sizeY creates narrow straw-like slivers.
                float2 p = abs(IN.uv * 2.0 - 1.0);
                float edge = 1.0 - smoothstep(0.67, 1.0, max(p.x, p.y));
                half4 c = IN.color * _Tint;
                c.a *= edge;
                return c;
            }
            ENDHLSL
        }
    }
}
