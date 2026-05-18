Shader "Custom/LavaShader"
{
    Properties
    {
        // ── Colores de temperatura (ajusta estos en el Inspector) ────────
        _DeepColor      ("Deep Lava Color",    Color) = (0.05, 0.01, 0.0,  1)   // negro-marrón (zonas hundidas)
        _MidColor       ("Mid Lava Color",     Color) = (0.75, 0.08, 0.0,  1)   // rojo intenso (calor activo)
        _HotColor       ("Hot Lava Color",     Color) = (1.0,  0.72, 0.02, 1)   // amarillo-naranja (crestas)
        _EmissionColor  ("Emission Color",     Color) = (1.0,  0.38, 0.0,  1)   // naranja brillante

        // ── Fresnel (borde incandescente) ────────────────────────────────
        _FresnelColor   ("Fresnel Color",      Color) = (1.0,  0.25, 0.0,  1)
        _FresnelPower   ("Fresnel Power",      Range(0.1, 5.0)) = 2.5

        // ── Emisión ──────────────────────────────────────────────────────
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 3.5

        // ── Ondas Gerstner (movimiento de vértices) ──────────────────────
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
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 clipPos    : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 normal     : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  waveHeight : TEXCOORD3;
            };

            // ── Variables ────────────────────────────────────────────────
            float4 _DeepColor, _MidColor, _HotColor;
            float4 _EmissionColor, _FresnelColor;
            float  _FresnelPower, _EmissionStrength;

            float  _Amplitude1, _Frequency1, _Speed1; float4 _Direction1;
            float  _Amplitude2, _Frequency2, _Speed2; float4 _Direction2;
            float  _Amplitude3, _Frequency3, _Speed3; float4 _Direction3;

            float  _FlowSpeed, _NoiseScale, _LavaLevel;

            // ================================================================
            //  RUIDO PROCEDURAL (reemplaza la FoamTexture sampler2D)
            // ================================================================

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
                return lerp(
                    lerp(hash(i),               hash(i + float2(1,0)), u.x),
                    lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), u.x),
                    u.y
                );
            }

            // FBM 4 octavas — reemplaza tex2D(_FoamTexture, foamUV)
            float fbm(float2 uv)
            {
                float v = 0.0, a = 0.5, f = 1.0;
                v += a * valueNoise(uv * f);                         a *= 0.5; f *= 2.1;
                v += a * valueNoise(uv * f + float2(1.7,  9.2));    a *= 0.5; f *= 2.1;
                v += a * valueNoise(uv * f + float2(8.3,  2.8));    a *= 0.5; f *= 2.1;
                v += a * valueNoise(uv * f + float2(4.1,  6.5));
                return v;
            }

            // ================================================================
            //  GERSTNER WAVE — misma función que WaterShader
            // ================================================================
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

            // ================================================================
            //  VERTEX SHADER
            // ================================================================
            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

                float2 dir1 = normalize(_Direction1.xy);
                float2 dir2 = normalize(_Direction2.xy);
                float2 dir3 = normalize(_Direction3.xy);

                // Suma 3 ondas Gerstner — misma lógica que WaterShader
                float3 totalOffset = GerstnerWave(worldPos, _Amplitude1, _Frequency1, _Speed1, dir1)
                                   + GerstnerWave(worldPos, _Amplitude2, _Frequency2, _Speed2, dir2)
                                   + GerstnerWave(worldPos, _Amplitude3, _Frequency3, _Speed3, dir3);

                worldPos += totalOffset;

                // waveHeight: altura sobre el nivel base → controla color y emisión
                // (mismo rol que en WaterShader, donde controlaba foam)
                o.waveHeight = worldPos.y - _LavaLevel;

                o.clipPos  = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.worldPos = worldPos;
                o.normal   = worldNormal;
                o.uv       = v.uv;
                return o;
            }

            // ================================================================
            //  FRAGMENT SHADER
            // ================================================================
            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.normal);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float  t = _Time.y;

                // ── Textura procedural animada ────────────────────────────
                // Reemplaza tex2D(_FoamTexture, foamUV) del WaterShader
                // Capa A: flujo principal
                float2 uvA    = i.uv * _NoiseScale + float2(t * _FlowSpeed * 0.5,
                                                             t * _FlowSpeed * 0.3);
                float  noiseA = fbm(uvA);

                // Capa B: flujo cruzado → turbulencia (análogo al slopeFactor del agua)
                float2 uvB    = i.uv * _NoiseScale * 0.8
                              + float2(-t * _FlowSpeed * 0.4,
                                        t * _FlowSpeed * 0.6);
                float  noiseB = fbm(uvB + noiseA * 0.35);

                float lavaPattern = noiseA * 0.4 + noiseB * 0.6;

                // ── Valor de calor: altura + patrón UV ────────────────────
                // Análogo a shallowFactor + foamFactorHeight del WaterShader
                float heightFactor = saturate(i.waveHeight * 1.8 + 0.3);
                float heatValue    = saturate(heightFactor * 0.55 + lavaPattern * 0.45);

                // ── Gradiente de color no lineal ──────────────────────────
                // deep → mid → hot con smoothstep (zonas frías anchas, crestas estrechas)
                float t1 = smoothstep(0.0,  0.45, heatValue);
                float t2 = smoothstep(0.45, 1.0,  heatValue);

                float3 lavaColor = lerp(_DeepColor.rgb, _MidColor.rgb, t1);
                       lavaColor = lerp(lavaColor,      _HotColor.rgb,  t2);

                // ── Fresnel — misma lógica que WaterShader ────────────────
                float fresnelFactor = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
                lavaColor = lerp(lavaColor, _FresnelColor.rgb, fresnelFactor * 0.45);

                // ── Emisión en crestas ────────────────────────────────────
                // Reemplaza foamFactor del WaterShader:
                // en agua las crestas tienen espuma blanca → aquí tienen brillo incandescente
                float  emissionMask = smoothstep(0.55, 1.0, heatValue);
                float3 emission     = _EmissionColor.rgb * _EmissionStrength * emissionMask;
                       emission    += _FresnelColor.rgb  * fresnelFactor * (_EmissionStrength * 0.35);

                float3 finalColor = lavaColor + emission;

                return fixed4(finalColor, 1.0);   // opaco (sin Blend ni ZWrite Off)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

// ============================================================
//  INSTRUCCIONES
// ============================================================
//
//  1. Reemplaza el contenido de Assets/LavaShader/LavaShader.shader
//     con este archivo completo.
//
//  2. COLORES RECOMENDADOS EN EL INSPECTOR:
//     Deep Lava Color   → #0D0200   (negro con tinte rojo)
//     Mid Lava Color    → #BF1400   (rojo intenso)
//     Hot Lava Color    →  #FFB800   (amarillo-naranja)
//     Emission Color    → #FF6000   (naranja brillante)
//     Fresnel Color     → #FF3300   (rojo en bordes)
//
//  3. El plano necesita mínimo 20x20 subdivisiones para que
//     las ondas Gerstner sean visibles en los vértices.
// ============================================================
