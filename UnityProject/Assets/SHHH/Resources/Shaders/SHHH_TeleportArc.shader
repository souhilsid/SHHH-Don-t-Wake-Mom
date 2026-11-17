// SHHH! teleport arc / beam (V12, owner: "please prepare a very nice teleportation shader for the ray and the area").
// A soft glowing tube: bright core, soft edges across the width, dashes that flow toward the landing spot, faded ends.
// Unlit, additive-ish alpha, no texture, single-pass-instanced stereo safe. uv.x runs along the line (LineRenderer Stretch),
// uv.y across it. _Dash = 0 gives a solid beam (used for the light column over the landing spot, uv.x = height there).
Shader "SHHH/TeleportArc"
{
    Properties
    {
        _Color ("Colour", Color) = (0.40, 0.90, 0.88, 1)
        _Core ("Core colour", Color) = (1, 1, 1, 1)
        _Dash ("Dashes along the line", Float) = 14
        _Speed ("Flow speed", Float) = 1.6
        _Alpha ("Opacity", Range(0, 1)) = 0.9
        _EndFade ("Fade at the ends", Range(0, 0.5)) = 0.08
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+20" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "TeleportArc"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color; half4 _Core; float _Dash; float _Speed; half _Alpha; half _EndFade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv; o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float across = abs(i.uv.y * 2.0 - 1.0);                    // 0 centre .. 1 edge
                float body = saturate(1.0 - across * across);              // soft round tube
                float core = pow(saturate(1.0 - across), 6.0);             // thin bright core
                float dash = 1.0;
                if (_Dash > 0.5)
                {
                    float f = frac(i.uv.x * _Dash - _Time.y * _Speed);
                    dash = 0.45 + 0.55 * smoothstep(0.0, 0.18, f) * (1.0 - smoothstep(0.55, 0.75, f));
                }
                float ends = smoothstep(0.0, _EndFade, i.uv.x) * (1.0 - smoothstep(1.0 - _EndFade, 1.0, i.uv.x));
                half3 col = lerp(_Color.rgb, _Core.rgb, core * 0.8);
                half a = saturate((body * 0.75 + core) * dash * ends) * _Alpha * i.color.a;
                return half4(col * i.color.rgb, a);
            }
            ENDHLSL
        }
    }
}
