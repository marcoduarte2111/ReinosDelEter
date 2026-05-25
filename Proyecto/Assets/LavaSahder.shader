Shader "Custom/LavaShader"
{
    Properties
    {
        // ── Colores de temperatura ───────────────────────────────────────
        _DeepColor      ("Deep Lava Color",    Color) = (0.05, 0.01, 0.0,  1)
        _MidColor       ("Mid Lava Color",     Color) = (0.75, 0.08, 0.0,  1)
        _HotColor       ("Hot Lava Color",     Color) = (1.0,  0.72, 0.02, 1)
        _EmissionColor  ("Emission Color",     Color) = (1.0,  0.38, 0.0,  1)

        // ── Fresnel (borde incandescente) ────────────────────────────────
        _FresnelColor   ("Fresnel Color",      Color) = (1.0,  0.25, 0.0,  1)
        _FresnelPower   ("Fresnel Power",      Range(0.1, 5.0)) = 2.5

        // ── Emisión ──────────────────────────────────────────────────────
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 3.5

        // ── Ondas Gerstner ───────────────────────────────────────────────
        _Amplitude1  ("Amplitude 1",       Range(0, 1))  = 0.18
        _Frequency1  ("Frequency 1",       Range(0, 10)) = 2.5
        _Speed1      ("Speed 1",           Range(0, 10)) = 0.8
        _Direction1  ("Direction 1 (X,Z)", Vector)       = (1, 0, 0, 0)

        _Amplitude2  ("Amplitude 2",       Range(0, 1))  = 0.10
        _Frequency2  ("Frequency 2",       Range(0, 10)) = 4.0
        _Speed2      ("Speed 2",           Range(0, 10)) = 1.1
        _Direction2  ("Direction 2 (X,Z)", Vector)       = (0.6, 0.8, 0, 0)

        _Amplitude3  ("Amplitude 3",       Range(0, 1))  = 0.07
        _Frequency3  ("Frequency 3",       Range(0, 10)) = 6.0
        _Speed3      ("Speed 3",           Range(0, 10)) = 1.6
        _Direction3  ("Direction 3 (X,Z)", Vector)       = (-0.7, 0.7, 0, 0)

        // ── Flujo procedural UV ──────────────────────────────────────────
        _FlowSpeed  ("Flow Speed (UV)", Range(0, 5))   = 0.5
        _NoiseScale ("Noise Scale",     Range(0.1, 8)) = 3.0

        // ── Nivel base ───────────────────────────────────────────────────
        _LavaLevel  ("Base Lava Level", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }
        LOD 300

        // ────────────────────────────────────────────────────────────────
        //  PASS — UniversalForward (unlit + emisión)
        // ────────────────────────────────────────────────────────────────
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor, _MidColor, _HotColor;
                float4 _EmissionColor, _FresnelColor;
                float  _FresnelPower, _EmissionStrength;
                float  _Amplitude1, _Frequency1, _Speed1; float4 _Direction1;
                float  _Amplitude2, _Frequency2, _Speed2; float4 _Direction2;
                float  _Amplitude3, _Frequency3, _Speed3; float4 _Direction3;
                float  _FlowSpeed, _NoiseScale, _LavaLevel;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  waveHeight : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
            };

            // ── Ruido procedural ─────────────────────────────────────────
            float hash(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i),               hash(i + float2(1,0)), u.x),
                            lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), u.x), u.y);
            }

            float fbm(float2 uv)
            {
                float v = 0.0, a = 0.5, f = 1.0;
                v += a * valueNoise(uv * f);                      a *= 0.5; f *= 2.1;
                v += a * valueNoise(uv * f + float2(1.7, 9.2));   a *= 0.5; f *= 2.1;
                v += a * valueNoise(uv * f + float2(8.3, 2.8));   a *= 0.5; f *= 2.1;
                v += a * valueNoise(uv * f + float2(4.1, 6.5));
                return v;
            }

            // ── Gerstner Wave ────────────────────────────────────────────
            float3 GerstnerWave(float3 pos, float amplitude, float frequency,
                                float speed, float2 direction)
            {
                float theta = dot(direction, pos.xz) * frequency + (_Time.y * speed);
                float3 offset;
                offset.x = amplitude * cos(theta) * direction.x;
                offset.z = amplitude * cos(theta) * direction.y;
                offset.y = amplitude * sin(theta);
                return offset;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 norWS = TransformObjectToWorldNormal(IN.normalOS);

                float2 d1 = normalize(_Direction1.xy);
                float2 d2 = normalize(_Direction2.xy);
                float2 d3 = normalize(_Direction3.xy);

                posWS += GerstnerWave(posWS, _Amplitude1, _Frequency1, _Speed1, d1);
                posWS += GerstnerWave(posWS, _Amplitude2, _Frequency2, _Speed2, d2);
                posWS += GerstnerWave(posWS, _Amplitude3, _Frequency3, _Speed3, d3);

                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS   = norWS;
                OUT.uv         = IN.uv;
                OUT.waveHeight = posWS.y - _LavaLevel;
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetCameraPositionWS() - IN.positionWS);
                float  t = _Time.y;

                // Textura procedural animada
                float2 uvA    = IN.uv * _NoiseScale + float2(t * _FlowSpeed * 0.5, t * _FlowSpeed * 0.3);
                float  noiseA = fbm(uvA);
                float2 uvB    = IN.uv * _NoiseScale * 0.8 + float2(-t * _FlowSpeed * 0.4, t * _FlowSpeed * 0.6);
                float  noiseB = fbm(uvB + noiseA * 0.35);
                float  lavaPattern = noiseA * 0.4 + noiseB * 0.6;

                float heightFactor = saturate(IN.waveHeight * 1.8 + 0.3);
                float heatValue    = saturate(heightFactor * 0.55 + lavaPattern * 0.45);

                float t1 = smoothstep(0.0,  0.45, heatValue);
                float t2 = smoothstep(0.45, 1.0,  heatValue);
                float3 lavaColor = lerp(_DeepColor.rgb, _MidColor.rgb, t1);
                       lavaColor = lerp(lavaColor,      _HotColor.rgb, t2);

                float fresnelFactor = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
                lavaColor = lerp(lavaColor, _FresnelColor.rgb, fresnelFactor * 0.45);

                float  emissionMask = smoothstep(0.55, 1.0, heatValue);
                float3 emission     = _EmissionColor.rgb * _EmissionStrength * emissionMask;
                       emission    += _FresnelColor.rgb  * fresnelFactor * (_EmissionStrength * 0.35);

                float3 finalColor = lavaColor + emission;

                finalColor = MixFog(finalColor, IN.fogFactor);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // ────────────────────────────────────────────────────────────────
        //  PASS — ShadowCaster URP
        // ────────────────────────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert_shadow
            #pragma fragment frag_shadow
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor, _MidColor, _HotColor;
                float4 _EmissionColor, _FresnelColor;
                float  _FresnelPower, _EmissionStrength;
                float  _Amplitude1, _Frequency1, _Speed1; float4 _Direction1;
                float  _Amplitude2, _Frequency2, _Speed2; float4 _Direction2;
                float  _Amplitude3, _Frequency3, _Speed3; float4 _Direction3;
                float  _FlowSpeed, _NoiseScale, _LavaLevel;
            CBUFFER_END

            float3 GerstnerWave(float3 pos, float a, float fr, float sp, float2 dir)
            {
                float th = dot(dir, pos.xz) * fr + _Time.y * sp;
                return float3(a*cos(th)*dir.x, a*sin(th), a*cos(th)*dir.y);
            }

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings   { float4 positionCS:SV_POSITION; };

            Varyings vert_shadow(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 norWS = TransformObjectToWorldNormal(IN.normalOS);

                posWS += GerstnerWave(posWS, _Amplitude1, _Frequency1, _Speed1, normalize(_Direction1.xy));
                posWS += GerstnerWave(posWS, _Amplitude2, _Frequency2, _Speed2, normalize(_Direction2.xy));
                posWS += GerstnerWave(posWS, _Amplitude3, _Frequency3, _Speed3, normalize(_Direction3.xy));

                float3 lightDir = normalize(_MainLightPosition.xyz);
                float4 posCS    = TransformWorldToHClip(ApplyShadowBias(posWS, norWS, lightDir));

                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = posCS;
                return OUT;
            }

            half4 frag_shadow(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // ────────────────────────────────────────────────────────────────
        //  PASS — DepthOnly URP
        // ────────────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert_depth
            #pragma fragment frag_depth

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor, _MidColor, _HotColor;
                float4 _EmissionColor, _FresnelColor;
                float  _FresnelPower, _EmissionStrength;
                float  _Amplitude1, _Frequency1, _Speed1; float4 _Direction1;
                float  _Amplitude2, _Frequency2, _Speed2; float4 _Direction2;
                float  _Amplitude3, _Frequency3, _Speed3; float4 _Direction3;
                float  _FlowSpeed, _NoiseScale, _LavaLevel;
            CBUFFER_END

            float3 GerstnerWave(float3 pos, float a, float fr, float sp, float2 dir)
            {
                float th = dot(dir, pos.xz) * fr + _Time.y * sp;
                return float3(a*cos(th)*dir.x, a*sin(th), a*cos(th)*dir.y);
            }

            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings   { float4 positionCS:SV_POSITION; };

            Varyings vert_depth(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                posWS += GerstnerWave(posWS, _Amplitude1, _Frequency1, _Speed1, normalize(_Direction1.xy));
                posWS += GerstnerWave(posWS, _Amplitude2, _Frequency2, _Speed2, normalize(_Direction2.xy));
                posWS += GerstnerWave(posWS, _Amplitude3, _Frequency3, _Speed3, normalize(_Direction3.xy));
                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 frag_depth(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
