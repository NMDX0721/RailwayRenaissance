Shader "Hidden/NF5K/ReviewDownsample"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 frag(v2f_img input) : SV_Target
            {
                float4 result=0;
                [unroll] for(int y=-1;y<=1;y++)
                    [unroll] for(int x=-1;x<=1;x++)
                        result+=tex2D(_MainTex,input.uv+float2(x,y)*_MainTex_TexelSize.xy);
                return result/9.0;
            }
            ENDHLSL
        }
    }
}
