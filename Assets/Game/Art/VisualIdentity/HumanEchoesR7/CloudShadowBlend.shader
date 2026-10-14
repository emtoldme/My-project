Shader "Reverb/Continuous Cloud Cookie"
{
    Properties { _MainTex("Cloud source",2D)="white" {} _ClearTex("Clear cookie",2D)="white" {} _Cover("Cover",Range(0,1))=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline"}
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_ClearTex;float _Cover;
            half4 frag(v2f_img i):SV_Target
            {
                half source=tex2D(_MainTex,i.uv).r;
                half clear=tex2D(_ClearTex,i.uv).r;
                half value=lerp(clear,lerp(.43,1,source),_Cover);
                return half4(value,value,value,1);
            }
            ENDHLSL
        }
    }
}
