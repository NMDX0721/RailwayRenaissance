Shader "Hidden/TabletGaussianBlur"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _BlurDir;
            float4 _Weights0;
            float _Weight4;
            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv) * _Weights0.x;
                color += (tex2D(_MainTex, i.uv + _BlurDir.xy) + tex2D(_MainTex, i.uv - _BlurDir.xy)) * _Weights0.y;
                color += (tex2D(_MainTex, i.uv + 2 * _BlurDir.xy) + tex2D(_MainTex, i.uv - 2 * _BlurDir.xy)) * _Weights0.z;
                color += (tex2D(_MainTex, i.uv + 3 * _BlurDir.xy) + tex2D(_MainTex, i.uv - 3 * _BlurDir.xy)) * _Weights0.w;
                color += (tex2D(_MainTex, i.uv + 4 * _BlurDir.xy) + tex2D(_MainTex, i.uv - 4 * _BlurDir.xy)) * _Weight4;
                return color;
            }
            ENDCG
        }
    }
}
