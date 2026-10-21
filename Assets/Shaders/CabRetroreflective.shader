Shader "RailwayRenaissance/RetroreflectiveSheet"
{
    Properties { _BaseColor("Sheet colour",Color)=(1,.8,.2,1) _ReturnGain("Return gain",Range(0,4))=2 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _ReturnGain;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half fog:TEXCOORD2; };
            V Vert(A a)
            {
                V o;o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half3 ReturnLobe(Light light,half3 normal,half3 view)
            {
                half alignment=pow(saturate(dot(light.direction,view)),64);
                return _BaseColor.rgb*light.color*light.distanceAttenuation*light.shadowAttenuation
                    *saturate(dot(normal,light.direction))*alignment*_ReturnGain;
            }
            half4 Frag(V i):SV_Target
            {
                InputData inputData=(InputData)0;
                inputData.positionWS=i.positionWS;inputData.normalWS=normalize(i.normalWS);
                inputData.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                inputData.bakedGI=SampleSH(inputData.normalWS);inputData.shadowMask=half4(1,1,1,1);
                inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                SurfaceData surface=(SurfaceData)0;surface.albedo=_BaseColor.rgb;surface.alpha=1;surface.occlusion=1;surface.smoothness=.25;
                half4 colour=UniversalFragmentPBR(inputData,surface);
                colour.rgb+=ReturnLobe(GetMainLight(inputData.shadowCoord),inputData.normalWS,inputData.viewDirectionWS);
                #if defined(_ADDITIONAL_LIGHTS)
                uint count=GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    colour.rgb+=ReturnLobe(GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1)),inputData.normalWS,inputData.viewDirectionWS);
                LIGHT_LOOP_END
                #endif
                colour.rgb=MixFog(colour.rgb,i.fog);return colour;
            }
            ENDHLSL
        }
    }
}
