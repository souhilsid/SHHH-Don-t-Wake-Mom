// SHHH! V10 stylized toon water (original). One shader for falling streams (tube mesh, uv.y = metres along the fall), pooled water
// surfaces and container fills (uv = 0..1 across the surface, _Ripple > 0) and droplets.
// Look: hand-painted cyan->blue body, clear-ish core with painted deep edges (fresnel), soft warm rim, broad toon highlight band,
// scrolling white flow streaks that run DOWN the stream, ripple rings on surfaces. No depth/opaque texture, no refraction, no
// screen-space effects: Quest URP single-pass-instanced stereo safe, one pass, alpha blended, ZWrite off, ~0 texture samples.
Shader "SHHH/ToonWater"
{
    Properties
    {
        _ShallowColor ("Core colour (clear-ish)", Color) = (0.78, 0.95, 1.0, 0.38)
        _DeepColor ("Edge colour (painted depth)", Color) = (0.22, 0.56, 0.92, 0.86)
        _RimColor ("Rim (soft, warm moonlight)", Color) = (0.95, 0.98, 1.0, 1)
        _StreakColor ("Flow streak / foam colour", Color) = (1, 1, 1, 0.9)
        _FlowSpeed ("Flow speed (m/s along uv.y)", Float) = 1.6
        _StreakScale ("Streak frequency (per metre)", Float) = 9
        _StreakAmount ("Streak amount", Range(0,1)) = 0.45
        _HighlightWidth ("Toon highlight width", Range(0,1)) = 0.18
        _WobbleAmp ("Wobble amplitude (m)", Float) = 0.0012
        _WobbleFreq ("Wobble frequency (per metre)", Float) = 28
        _Ripple ("Surface ripple rings (0 = stream)", Range(0,1)) = 0
        _RippleSpeed ("Ripple speed", Float) = 1.2
        _Opacity ("Opacity", Range(0,1)) = 1
        _RippleCenter ("Ripple centre (uv)", Vector) = (0.5, 0.5, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "IgnoreProjector"="True" }
        Pass
        {
            Name "ToonWater"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor; half4 _DeepColor; half4 _RimColor; half4 _StreakColor;
                float _FlowSpeed; float _StreakScale; half _StreakAmount; half _HighlightWidth;
                float _WobbleAmp; float _WobbleFreq; half _Ripple; float _RippleSpeed; half _Opacity; float4 _RippleCenter;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2;
                half4 color : TEXCOORD3; half fog : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // cheap hash noise (no textures)
            float H(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float VN(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(H(i), H(i + float2(1, 0)), f.x), lerp(H(i + float2(0, 1)), H(i + float2(1, 1)), f.x), f.y); }

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 pos = v.positionOS.xyz;
                // stream: coherent bulges travelling down the fall (uv.y metres) + a little around-the-tube variation; vertex colour.a scales it
                float t = _Time.y;
                float w = sin(v.uv.y * _WobbleFreq - t * _FlowSpeed * 6.2831) * 0.6 + sin(v.uv.y * _WobbleFreq * 2.3 + v.uv.x * 12.566 - t * _FlowSpeed * 11.0) * 0.4;
                pos += v.normalOS * w * _WobbleAmp * v.color.a * (1 - _Ripple);
                VertexPositionInputs p = GetVertexPositionInputs(pos);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv; o.color = v.color; o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n = normalize(i.normalWS);
                half3 vdir = SafeNormalize(GetWorldSpaceViewDir(i.positionWS));
                half ndv = saturate(abs(dot(n, vdir)));
                half edge = 1.0h - ndv;                                   // 0 = facing the eye (clear core), 1 = silhouette (deep)
                half depthBand = smoothstep(0.25h, 0.75h, edge);         // painted two-tone, not a photographic gradient
                half4 body = lerp(_ShallowColor, _DeepColor, depthBand);
                float t = _Time.y;
                // flow streaks: stretched noise scrolling down the stream; banded into soft painted dashes
                float2 sp = float2(i.uv.x * 7.0, i.uv.y * _StreakScale - t * _FlowSpeed * _StreakScale * 0.35);
                half streak = smoothstep(0.62h, 0.78h, VN(sp)) * _StreakAmount * (1 - _Ripple);
                // surfaces (V10 rev 2, owner: "not a bullseye"): no concentric target rings. A few faint, noise-broken ripples only
                // around where the stream lands, painted sheen blobs drifting slowly, a froth patch at the impact, a thin irregular foam edge.
                float2 c = i.uv - 0.5; float r = length(c) * 2.0;
                float2 ci = i.uv - _RippleCenter.xy; float ri = length(ci) * 2.0;
                float wob = (VN(i.uv * 9.0 + t * 0.35) - 0.5) * 0.08;
                half ring = smoothstep(0.86h, 0.97h, sin((ri + wob) * 26.0 - t * _RippleSpeed * 6.2831) * 0.5h + 0.5h) * 0.16h * saturate(1.0 - ri * 2.2) * _Ripple;
                half sheen = smoothstep(0.70h, 0.80h, VN(i.uv * float2(5.0, 9.0) + float2(t * 0.06, -t * 0.04))) * 0.22h * _Ripple;
                half froth = smoothstep(0.30h, 0.05h, ri + (VN(ci * 30.0 + t * 1.5) - 0.5) * 0.18) * _Ripple;
                half rimFoam = smoothstep(0.90h, 0.99h, r + (VN(c * 22.0 + t * 0.25) - 0.5) * 0.10) * 0.6h * _Ripple;
                ring += sheen + froth * 0.85h;
                // light: toon highlight band from the main light (moonlight) + soft painted rim + SH ambient so it sits in the night room
                Light ml = GetMainLight();
                half3 refl = reflect(-vdir, n);
                half hl = smoothstep(1.0h - _HighlightWidth, 1.0h - _HighlightWidth * 0.6h, saturate(dot(refl, ml.direction)));
                half hl2 = smoothstep(0.9h, 0.97h, saturate(dot(refl, normalize(half3(0.3h, 0.9h, 0.3h))))) * 0.6h; // fixed painted sheen (lamps)
                half3 amb = SampleSH(n);
                half3 col = body.rgb * (0.55h + 0.45h * saturate(amb * 1.6h + ml.color * 0.35h));
                col = lerp(col, _StreakColor.rgb, saturate(streak + ring + rimFoam));
                col += _RimColor.rgb * pow(edge, 3.0h) * 0.35h;
                col += (hl + hl2) * _RimColor.rgb * 0.85h;
                half a = body.a + streak * 0.35h + (hl + hl2) * 0.4h + ring * 0.5h + rimFoam * 0.7h;
                a = saturate(a) * _Opacity * i.color.r;                   // vertex colour R = per-vertex fade (stream tail / breakup)
                col = MixFog(col, i.fog);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
