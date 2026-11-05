Shader "RailRevival/BatteryLabel"
{
    Properties { _MainTex("Font",2D)="white"{} _Color("Color",Color)=(1,1,1,1) }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Pass {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.color=a.color*_Color;return o;}
            half4 frag(V i):SV_Target{half a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;clip(a-.45);return half4(i.color.rgb,1);}
            ENDHLSL
        }
    }
}
