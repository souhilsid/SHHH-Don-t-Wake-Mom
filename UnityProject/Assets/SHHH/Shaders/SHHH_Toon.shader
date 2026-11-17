// SHHH! stylized toon shading for Quest (single pass, no shadows, no depth texture):
// 2-band cel lighting from the main light, soft-banded additional lights (lamps), SH ambient for colour bleed,
// warm/cool shade tint, rim light for silhouettes. Single-pass-instanced stereo safe. ~1 texture sample.
Shader "SHHH/Toon"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ShadeTint ("Shade Tint (cool)", Color) = (0.55,0.52,0.78,1)
        _BandCenter ("Band Center", Range(-1,1)) = 0.05
        _BandSoftness ("Band Softness", Range(0.001,0.5)) = 0.06
        _RimColor ("Rim Color", Color) = (1,0.93,0.82,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3.5
        _RimStrength ("Rim Strength", Range(0,1)) = 0.35
        _AmbientBoost ("Ambient Boost", Range(0,2)) = 1.0
        [HDR]_EmissionColor ("Emission", Color) = (0,0,0,1)
        _Smoothness ("Smoothness (unused, compat)", Range(0,1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; half4 _ShadeTint; half _BandCenter; half _BandSoftness;
                half4 _RimColor; half _RimPower; half _RimStrength; half _AmbientBoost; half4 _EmissionColor; half _Smoothness;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2;
                half fog : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half Band(half ndl) { return smoothstep(_BandCenter - _BandSoftness, _BandCenter + _BandSoftness, ndl); }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half3 n = normalize(i.normalWS);
                half3 v = SafeNormalize(GetWorldSpaceViewDir(i.positionWS));
                Light ml = GetMainLight();
                half lit = Band(dot(n, ml.direction));
                half3 shade = albedo.rgb * _ShadeTint.rgb;
                half3 col = lerp(shade, albedo.rgb, lit) * ml.color;
                // ambient (SH) keeps the night scene readable; boosted for a cozy cartoon fill
                col += albedo.rgb * SampleSH(n) * _AmbientBoost;
            #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; ++li)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    half b = Band(dot(n, l.direction)) * l.distanceAttenuation;
                    col += albedo.rgb * l.color * b;
                }
            #endif
                half rim = pow(saturate(1.0h - dot(n, v)), _RimPower) * _RimStrength * (0.35h + 0.65h * lit);
                col += _RimColor.rgb * rim;
                col += _EmissionColor.rgb;
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex v2
            #pragma fragment f2
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            V v2(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS = TransformObjectToHClip(a.positionOS.xyz); return o; }
            half f2(V i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
