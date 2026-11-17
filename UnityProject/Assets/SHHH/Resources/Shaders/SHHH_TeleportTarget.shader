// SHHH! teleport landing spot (V12). Drawn on a flat quad (uv 0..1, centre 0.5): a soft glowing disc, a crisp rim, a ring that
// keeps rippling outward, and a faint dotted inner circle - "you will stand here". _Bad = 1 turns it into a crossed-out warning.
// Unlit, alpha-blended, no texture, single-pass-instanced stereo safe.
Shader "SHHH/TeleportTarget"
{
    Properties
    {
        _Color ("Colour", Color) = (0.40, 0.90, 0.88, 1)
        _Rim ("Rim colour", Color) = (1, 1, 1, 1)
        _Alpha ("Opacity", Range(0, 1)) = 1
        _Bad ("Not allowed", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "TeleportTarget"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color; half4 _Rim; half _Alpha; half _Bad;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p = i.uv * 2.0 - 1.0; float r = length(p);
                if (r > 1.0) discard;
                float disc = (1.0 - smoothstep(0.55, 0.85, r)) * 0.28;                       // soft filled disc
                float rim = smoothstep(0.80, 0.86, r) * (1.0 - smoothstep(0.90, 0.97, r));    // crisp rim
                float t = frac(_Time.y * 0.7);
                float ripple = (1.0 - smoothstep(0.0, 0.06, abs(r - lerp(0.25, 0.95, t)))) * (1.0 - t) * 0.8;   // ring travelling outward
                float ang = atan2(p.y, p.x);
                float dots = step(0.5, frac(ang * 24.0 / 6.2831853)) * (1.0 - smoothstep(0.0, 0.025, abs(r - 0.42))) * 0.7;
                float xmark = 0.0;
                if (_Bad > 0.5)
                {
                    float2 q = abs(float2(p.x + p.y, p.x - p.y)) * 0.7071;
                    xmark = (1.0 - smoothstep(0.07, 0.11, min(q.x, q.y))) * step(r, 0.62);
                    ripple = 0.0; dots = 0.0;
                }
                half3 col = lerp(_Color.rgb, _Rim.rgb, saturate(rim + xmark * 0.2));
                half a = saturate(disc + rim + ripple + dots + xmark) * _Alpha;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
