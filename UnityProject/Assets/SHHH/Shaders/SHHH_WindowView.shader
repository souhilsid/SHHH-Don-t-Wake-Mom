// SHHH! window view (2026-11-09 Gate 3): windows that are painted onto solid walls show the night panorama (sky + far town,
// DesignSources/UI_V8 sky_night_pano) sampled by the VIEW DIRECTION, like looking through glass at a far-away street: the view
// shifts correctly as you move your head (parallax at infinity) instead of a flat picture. Unlit, one texture sample,
// single-pass-instanced stereo safe.
Shader "SHHH/WindowView"
{
    Properties
    {
        _MainTex ("Equirect panorama", 2D) = "black" {}
        _Tint ("Tint", Color) = (0.9,0.92,1,1)
        _Rotation ("Rotation (deg)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "WindowView"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; half4 _Tint; float _Rotation;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 d = normalize(i.positionWS - GetCameraPositionWS());
                // same mapping as Unity's Skybox/Panoramic (lat-long), so the moon and the far town line up with the sky
                float r = radians(_Rotation); float s = sin(r), c = cos(r);
                d = float3(c * d.x - s * d.z, d.y, s * d.x + c * d.z);
                float2 uv = float2(0.5 - atan2(d.z, d.x) / (2.0 * PI), 1.0 - acos(clamp(d.y, -1.0, 1.0)) / PI);
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0) * _Tint;
            }
            ENDHLSL
        }
    }
}
