Shader "UI/TabletGaussianBlur"
{
    Properties
    {
        [HideInInspector] _MainTex ("Source Texture", 2D) = "white" {}
        [HideInInspector] _BlurDir ("Blur Direction and Step", Vector) = (0.001, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        LOD 100
        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            Name "SeparableGaussianBlur9Tap"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _BlurDir;
            float4 _Weights0; // x: tap 0 (center), y: tap 1, z: tap 2, w: tap 3
            float _Weight4;   // tap 4

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0.0)
                {
                    o.uv.y = 1.0 - o.uv.y;
                }
                #endif
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 stepDir = _BlurDir.xy;

                // Center sample
                float4 color = tex2D(_MainTex, i.uv) * _Weights0.x;

                // Symmetrical 9-tap accumulation (4 offsets positive, 4 offsets negative)
                color += (tex2D(_MainTex, i.uv + stepDir * 1.0) + tex2D(_MainTex, i.uv - stepDir * 1.0)) * _Weights0.y;
                color += (tex2D(_MainTex, i.uv + stepDir * 2.0) + tex2D(_MainTex, i.uv - stepDir * 2.0)) * _Weights0.z;
                color += (tex2D(_MainTex, i.uv + stepDir * 3.0) + tex2D(_MainTex, i.uv - stepDir * 3.0)) * _Weights0.w;
                color += (tex2D(_MainTex, i.uv + stepDir * 4.0) + tex2D(_MainTex, i.uv - stepDir * 4.0)) * _Weight4;

                color.a = saturate(color.a);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
