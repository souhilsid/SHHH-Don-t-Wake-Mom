// SHHH! V10 toon foam / suds (original): soft, opaque-looking white-lilac blobs with a cel band and a painted cool shade, eroding
// with _Dissolve (rinsed away) using a cheap procedural noise - no textures. Alpha-clipped (no blending sort issues, low overdraw).
// Single-pass-instanced stereo safe. Used for the splash crown at the stream impact, suds on sponge/plate, soap droplets.
Shader "SHHH/ToonFoam"
{
    Properties
    {
        _BaseColor ("Foam colour", Color) = (1, 0.99, 0.97, 1)
        _ShadeColor ("Painted shade", Color) = (0.72, 0.78, 0.95, 1)
        _Dissolve ("Dissolve (0 = full foam, 1 = gone)", Range(0,1)) = 0
        _NoiseScale ("Bubble scale", Float) = 22
        _BandCenter ("Cel band", Range(-1,1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }
        Pass
        {
            Name "ToonFoam"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor; half4 _ShadeColor; half _Dissolve; float _NoiseScale; half _BandCenter;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionOS : TEXCOORD1; half fog : TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            float H(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float VN(float3 x) { float3 i = floor(x), f = frac(x); f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(H(i), H(i + float3(1,0,0)), f.x), lerp(H(i + float3(0,1,0)), H(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(H(i + float3(0,0,1)), H(i + float3(1,0,1)), f.x), lerp(H(i + float3(0,1,1)), H(i + float3(1,1,1)), f.x), f.y), f.z); }
            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.normalWS = TransformObjectToWorldNormal(v.normalOS); o.positionOS = v.positionOS.xyz; o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float nz = VN(i.positionOS * _NoiseScale + _Time.y * 0.15) * 0.65 + VN(i.positionOS * _NoiseScale * 2.7) * 0.35;
                clip(nz - _Dissolve * 1.05 + 0.02);
                half3 n = normalize(i.normalWS); Light ml = GetMainLight();
                half band = smoothstep(_BandCenter - 0.08h, _BandCenter + 0.08h, dot(n, ml.direction));
                half3 col = lerp(_ShadeColor.rgb, _BaseColor.rgb, saturate(band * 0.7h + 0.3h)) * saturate(0.6h + SampleSH(n) * 1.4h);
                col = lerp(col, _ShadeColor.rgb, smoothstep(0.55h, 0.35h, nz) * 0.35h); // painted bubble outlines
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
