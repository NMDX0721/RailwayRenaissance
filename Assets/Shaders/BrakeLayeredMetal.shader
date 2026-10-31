Shader "RailwayRenaissance/BrakeLayeredMetal"
{
    Properties
    {
        _BaseMap("Surface",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _MetallicGlossMap("Metal and smoothness",2D)="white"{}
        _BumpMap("Orange peel / machining",2D)="bump"{}
        _BumpScale("Normal strength",Float)=.5
        _PaintLayer("Paint layer",Float)=0
        _GripLayer("Grip layer",Float)=0
        _Cull("Cull",Float)=2
        _Cutoff("Cutoff",Float)=.5
        _ZWrite("Depth",Float)=1
        _Surface("Surface",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            Cull [_Cull] ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST,_BaseColor;
            float _BumpScale,_PaintLayer,_GripLayer,_Cull,_Cutoff,_ZWrite,_Surface;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;float3 grain:TEXCOORD1;float4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;half4 masks:TEXCOORD4;half fog:TEXCOORD5;float4 shadow:TEXCOORD6;float3 grain:TEXCOORD7;half3 normalOS:TEXCOORD8;};
            Varyings Vert(Attributes a)
            {
                Varyings o;VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);VertexNormalInputs n=GetVertexNormalInputs(a.normalOS,a.tangentOS);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=n.normalWS;o.tangentWS=half4(n.tangentWS,a.tangentOS.w*GetOddNegativeScale());
                o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.masks=a.color;o.fog=ComputeFogFactor(p.positionCS.z);o.shadow=GetShadowCoord(p);o.grain=a.grain;o.normalOS=a.normalOS;return o;
            }
            float Noise(float3 p){return frac(sin(dot(floor(p),float3(12.9898,78.233,37.719)))*43758.5453);}
            half4 Frag(Varyings i):SV_Target
            {
                half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                half4 ms=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,i.uv);
                half3 ts=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                half3 n=normalize(i.normalWS),t=normalize(i.tangentWS.xyz),b=cross(n,t)*i.tangentWS.w;
                n=NormalizeNormalPerPixel(t*ts.x+b*ts.y+n*ts.z);
                if(_PaintLayer>.5)
                {
                    // Baked local metre coordinates keep grain scale consistent without sliding as the train moves.
                    half3 geometric=normalize(i.normalOS),w=pow(abs(geometric),6);w/=max(.001,w.x+w.y+w.z);
                    float3 p=i.grain*12;
                    base=(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb*w.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*w.z)*_BaseColor.rgb;
                    ms=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,p.zy)*w.x+SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,p.xz)*w.y+SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,p.xy)*w.z;
                    half3 x=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.zy),_BumpScale);
                    half3 y=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.xz),_BumpScale);
                    half3 z=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.xy),_BumpScale);
                    n=NormalizeNormalPerPixel(TransformObjectToWorldNormal(geometric+half3(0,x.y,x.x)*w.x+half3(y.x,0,y.y)*w.y+half3(z.x,z.y,0)*w.z));
                }
                // Curvature mask follows the original mesh, rather than drawing wear in random UV locations.
                half chip=_PaintLayer*i.masks.r*smoothstep(.985,.999,Noise(i.grain*1750))*.35;
                half dirt=saturate(i.masks.g);
                base=lerp(base,half3(.26,.28,.29),chip*.8);
                base*=1-dirt*.48;
                // Restrained warm discoloration at the joint mask, not a uniform blue-steel tint.
                base=lerp(base,base*half3(.88,.76,.60),dirt*.65);
                SurfaceData s=(SurfaceData)0;s.albedo=base;s.metallic=lerp(ms.r,.88,chip);s.specular=half3(.04,.04,.04);
                s.smoothness=clamp(ms.a+chip*.12+_GripLayer*i.masks.b*.20-dirt*.10,.08,.85);
                s.occlusion=1-dirt*.25;s.normalTS=ts;s.alpha=1;
                InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord=i.shadow;d.fogCoord=i.fog;d.vertexLighting=VertexLighting(i.positionWS,n);d.bakedGI=SampleSH(n);
                d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);d.shadowMask=half4(1,1,1,1);
                half4 color=UniversalFragmentPBR(d,s);color.rgb=MixFog(color.rgb,i.fog);return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Lit"
}
