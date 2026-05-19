Shader "Custom/LandShader"
{
    Properties
    {
        _DirtColor      ("Dirt / Dark Base",     Color) = (0.08, 0.06, 0.05, 1)
        _RockDarkColor  ("Rock Dark",            Color) = (0.18, 0.17, 0.16, 1)
        _RockMidColor   ("Rock Mid",             Color) = (0.40, 0.39, 0.37, 1)
        _RockLightColor ("Rock Light",           Color) = (0.68, 0.66, 0.62, 1)
        _MossColor      ("Moss / Lichen",        Color) = (0.20, 0.28, 0.12, 1)
        _WetColor       ("Wet Rock Tint",        Color) = (0.10, 0.12, 0.14, 1)
        _SpecColor2     ("Specular Color",       Color) = (0.55, 0.55, 0.55, 1)
        _Smoothness     ("Smoothness",           Range(0,1))   = 0.25
        _MossStrength   ("Moss Coverage",        Range(0,1))   = 0.38
        _CrackContrast  ("Crack Contrast",       Range(0,4))   = 2.2
        _GraniteScale   ("Granite Detail Scale", Range(1,20))  = 9.0
        _MacroScale     ("Macro Rock Scale",     Range(0.1,5)) = 1.8

        _Amplitude1  ("Amplitude 1",       Range(0,1))  = 0.03
        _Frequency1  ("Frequency 1",       Range(0,10)) = 2.5
        _Speed1      ("Speed 1",           Range(0,10)) = 0.30
        _Direction1  ("Direction 1 (X,Z)", Vector)      = (1, 0, 0, 0)

        _Amplitude2  ("Amplitude 2",       Range(0,1))  = 0.015
        _Frequency2  ("Frequency 2",       Range(0,10)) = 4.2
        _Speed2      ("Speed 2",           Range(0,10)) = 0.45
        _Direction2  ("Direction 2 (X,Z)", Vector)      = (0.6, 0.8, 0, 0)

        _Amplitude3  ("Amplitude 3",       Range(0,1))  = 0.008
        _Frequency3  ("Frequency 3",       Range(0,10)) = 7.0
        _Speed3      ("Speed 3",           Range(0,10)) = 0.65
        _Direction3  ("Direction 3 (X,Z)", Vector)      = (-0.7, 0.7, 0, 0)

        _TerrainLevel ("Base Terrain Level", Float) = 0.0
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
        //  PASS — UniversalForward (lit, sombras, GI)
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

            // URP feature keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            // URP core includes (Unity 6 paths)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // ── CBUFFER (SRP Batcher compatible) ─────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _DirtColor;
                float4 _RockDarkColor;
                float4 _RockMidColor;
                float4 _RockLightColor;
                float4 _MossColor;
                float4 _WetColor;
                float4 _SpecColor2;
                float  _Smoothness;
                float  _MossStrength;
                float  _CrackContrast;
                float  _GraniteScale;
                float  _MacroScale;
                float  _Amplitude1, _Frequency1, _Speed1;
                float4 _Direction1;
                float  _Amplitude2, _Frequency2, _Speed2;
                float4 _Direction2;
                float  _Amplitude3, _Frequency3, _Speed3;
                float4 _Direction3;
                float  _TerrainLevel;
            CBUFFER_END

            // ── Structs ──────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
            };

            // ================================================================
            //  RUIDO PROCEDURAL — octavas fijas, sin loops dinámicos
            // ================================================================
            float hash1(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float2 hash2(float2 p)
            {
                float2 h = float2(dot(p, float2(127.1, 311.7)),
                                  dot(p, float2(269.5, 183.3)));
                return frac(sin(h) * 43758.5453);
            }

            float vn(float2 uv)
            {
                float2 i = floor(uv), f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash1(i),             hash1(i + float2(1,0)), u.x),
                            lerp(hash1(i+float2(0,1)), hash1(i + float2(1,1)), u.x), u.y);
            }

            float gn(float2 uv)
            {
                float2 i = floor(uv), f = frac(uv);
                float2 u = f*f*f*(f*(f*6.0-15.0)+10.0);
                float2 ga = hash2(i)             *2.0-1.0;
                float2 gb = hash2(i+float2(1,0)) *2.0-1.0;
                float2 gc = hash2(i+float2(0,1)) *2.0-1.0;
                float2 gd = hash2(i+float2(1,1)) *2.0-1.0;
                float va = dot(ga, f);
                float vb = dot(gb, f-float2(1,0));
                float vc = dot(gc, f-float2(0,1));
                float vd = dot(gd, f-float2(1,1));
                return lerp(lerp(va,vb,u.x), lerp(vc,vd,u.x), u.y)*0.5+0.5;
            }

            float fbm3(float2 p)
            {
                float2 s = float2(1.7, 9.2);
                float v  = 0.500  * gn(p); p = p*2.07+s;
                       v += 0.250  * gn(p); p = p*2.07+s;
                       v += 0.125  * gn(p);
                return v * (1.0/0.875);
            }

            float fbm5(float2 p)
            {
                float2 s = float2(1.7, 9.2);
                float v  = 0.50000 * gn(p); p = p*2.07+s;
                       v += 0.25000 * gn(p); p = p*2.07+s;
                       v += 0.12500 * gn(p); p = p*2.07+s;
                       v += 0.06250 * gn(p); p = p*2.07+s;
                       v += 0.03125 * gn(p);
                return v * (1.0/0.96875);
            }

            // Domain-warped → estructura macro de roca irregular
            float rockMacro(float2 uv)
            {
                float2 q = float2(fbm3(uv), fbm3(uv + float2(5.2, 1.3)));
                return fbm5(uv + q * 3.5);
            }

            // Voronoi unrolled — celdas de fractura
            float voronoi(float2 uv)
            {
                float2 i = floor(uv), f = frac(uv);
                float md = 8.0;
                float2 nb, dv;
                nb=float2(-1,-1); dv=nb+hash2(i+nb)-f; md=min(md,dot(dv,dv));
                nb=float2( 0,-1); dv=nb+hash2(i+nb)-f; md=min(md,dot(dv,dv));
                nb=float2( 1,-1); dv=nb+hash2(i+nb)-f; md=min(md,dot(dv,dv));
                nb=float2(-1, 0); dv=nb+hash2(i+nb)-f; md=min(md,dot(dv,dv));
                nb=float2( 0, 0); dv=nb+hash2(i+nb)-f; md=min(md,dot(dv,dv));
                nb=float2( 1, 0); dv=nb+hash2(i+nb)-f; md=min(md,dot(dv,dv));
                nb=float2(-1, 1); dv=nb+hash2(i+nb)-f; md=min(md,dot(dv,dv));
                nb=float2( 0, 1); dv=nb+hash2(i+nb)-f; md=min(md,dot(dv,dv));
                nb=float2( 1, 1); dv=nb+hash2(i+nb)-f; md=min(md,dot(dv,dv));
                return sqrt(md);
            }

            // ── Gerstner Wave ────────────────────────────────────────────
            float3 Gerstner(float3 p, float a, float fr, float sp, float2 dir)
            {
                float th = dot(dir, p.xz) * fr + _Time.y * sp;
                return float3(a*cos(th)*dir.x, a*sin(th), a*cos(th)*dir.y);
            }

            // ================================================================
            //  VERTEX
            // ================================================================
            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                // Posición y normal en world space
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 norWS = TransformObjectToWorldNormal(IN.normalOS);

                // Ondas Gerstner (vibración geológica mínima)
                float2 d1 = normalize(_Direction1.xz);
                float2 d2 = normalize(_Direction2.xz);
                float2 d3 = normalize(_Direction3.xz);
                posWS += Gerstner(posWS, _Amplitude1, _Frequency1, _Speed1, d1);
                posWS += Gerstner(posWS, _Amplitude2, _Frequency2, _Speed2, d2);
                posWS += Gerstner(posWS, _Amplitude3, _Frequency3, _Speed3, d3);

                OUT.positionCS  = TransformWorldToHClip(posWS);
                OUT.positionWS  = posWS;
                OUT.normalWS    = norWS;
                OUT.uv          = IN.uv;
                OUT.shadowCoord = TransformWorldToShadowCoord(posWS);
                OUT.fogFactor   = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            // ================================================================
            //  FRAGMENT
            // ================================================================
            half4 frag(Varyings IN) : SV_Target
            {
                float3 N  = normalize(IN.normalWS);
                float3 V  = normalize(GetCameraPositionWS() - IN.positionWS);
                float2 uv = IN.uv;

                // ── Capas de ruido ────────────────────────────────────────
                float macro  = rockMacro(uv * _MacroScale);
                float cells  = voronoi(uv * _GraniteScale * 0.4);
                float detail = fbm5(uv * _GraniteScale);
                float micro  = vn(uv * _GraniteScale * 3.2);
                float micro2 = vn(uv * _GraniteScale * 6.1 + float2(4.4, 2.1));

                // Grietas — cuadrático NaN-safe (sin pow con base negativa)
                float cc    = saturate(cells * _CrackContrast);
                float crack = 1.0 - cc * cc;

                // Valor compuesto de roca [0..1]
                float rv = saturate(macro*0.40 + detail*0.35 + micro*0.15 + micro2*0.10);

                // ── Gradiente de color ────────────────────────────────────
                float t1  = smoothstep(0.25, 0.55, rv);
                float t2  = smoothstep(0.55, 0.82, rv);
                float3 col = lerp(_RockDarkColor.rgb,  _RockMidColor.rgb,  t1);
                       col = lerp(col,                  _RockLightColor.rgb, t2);

                // Granos de granito
                col *= lerp(0.82, 1.18, micro * micro2);

                // Grietas oscuras
                float3 crackCol = lerp(_DirtColor.rgb, _WetColor.rgb, micro);
                col = lerp(col, crackCol, crack * 0.82);

                // ── Musgo en grietas horizontales ─────────────────────────
                float slope    = saturate(1.0 - N.y * 1.3);
                float mossMask = saturate(crack * (1.0 - slope) * macro * _MossStrength * 2.6);
                float mossVar  = vn(uv * _GraniteScale * 0.9 + float2(7.3, 2.1));
                float3 mossCol = lerp(_MossColor.rgb, _MossColor.rgb * 1.4, mossVar);
                col = lerp(col, mossCol, mossMask);

                // Humedad
                float wet = saturate(crack * 0.55 + (1.0 - rv) * 0.35);
                col = lerp(col, col * 0.52, wet * 0.40);

                // ── Iluminación URP ───────────────────────────────────────
                // Luz principal con sombras
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                float3 lightCol = mainLight.color * mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                float  NdotL   = saturate(dot(N, lightDir));
                float3 diffuse = col * lightCol * NdotL;

                // Ambiente URP (SH)
                float3 ambient = col * SampleSH(N);

                // Specular Blinn-Phong NaN-safe
                float3 H     = normalize(lightDir + V);
                float  NdotH = saturate(dot(N, H));
                float  shiny = max(1.0, _Smoothness * 128.0);
                float  spec  = (NdotH > 0.001) ? exp(log(NdotH) * shiny) * _Smoothness : 0.0;
                spec *= (1.0 - mossMask * 0.92);
                spec *= (1.0 - crack    * 0.75);
                float3 specular = _SpecColor2.rgb * lightCol * spec;

                float3 finalColor = ambient + diffuse + specular;

                // Anti-tiling macro
                finalColor *= (0.93 + fbm3(uv * 0.33) * 0.13);

                // Niebla URP
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(saturate(finalColor), 1.0);
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
                float4 _DirtColor, _RockDarkColor, _RockMidColor, _RockLightColor;
                float4 _MossColor, _WetColor, _SpecColor2;
                float  _Smoothness, _MossStrength, _CrackContrast, _GraniteScale, _MacroScale;
                float  _Amplitude1, _Frequency1, _Speed1; float4 _Direction1;
                float  _Amplitude2, _Frequency2, _Speed2; float4 _Direction2;
                float  _Amplitude3, _Frequency3, _Speed3; float4 _Direction3;
                float  _TerrainLevel;
            CBUFFER_END

            float3 Gerstner(float3 p, float a, float fr, float sp, float2 dir)
            {
                float th = dot(dir, p.xz) * fr + _Time.y * sp;
                return float3(a*cos(th)*dir.x, a*sin(th), a*cos(th)*dir.y);
            }

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings   { float4 positionCS:SV_POSITION; };

            Varyings vert_shadow(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 norWS = TransformObjectToWorldNormal(IN.normalOS);

                posWS += Gerstner(posWS, _Amplitude1, _Frequency1, _Speed1, normalize(_Direction1.xz));
                posWS += Gerstner(posWS, _Amplitude2, _Frequency2, _Speed2, normalize(_Direction2.xz));
                posWS += Gerstner(posWS, _Amplitude3, _Frequency3, _Speed3, normalize(_Direction3.xz));

                // Offset de sombra estándar URP
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
        //  PASS — DepthOnly (para efectos de post-proceso URP)
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
                float4 _DirtColor, _RockDarkColor, _RockMidColor, _RockLightColor;
                float4 _MossColor, _WetColor, _SpecColor2;
                float  _Smoothness, _MossStrength, _CrackContrast, _GraniteScale, _MacroScale;
                float  _Amplitude1, _Frequency1, _Speed1; float4 _Direction1;
                float  _Amplitude2, _Frequency2, _Speed2; float4 _Direction2;
                float  _Amplitude3, _Frequency3, _Speed3; float4 _Direction3;
                float  _TerrainLevel;
            CBUFFER_END

            float3 Gerstner(float3 p, float a, float fr, float sp, float2 dir)
            {
                float th = dot(dir, p.xz) * fr + _Time.y * sp;
                return float3(a*cos(th)*dir.x, a*sin(th), a*cos(th)*dir.y);
            }

            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings   { float4 positionCS:SV_POSITION; };

            Varyings vert_depth(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                posWS += Gerstner(posWS, _Amplitude1, _Frequency1, _Speed1, normalize(_Direction1.xz));
                posWS += Gerstner(posWS, _Amplitude2, _Frequency2, _Speed2, normalize(_Direction2.xz));
                posWS += Gerstner(posWS, _Amplitude3, _Frequency3, _Speed3, normalize(_Direction3.xz));
                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 frag_depth(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}

// ============================================================
//  INSTRUCCIONES — Unity 6 + URP
// ============================================================
//
//  1. Reemplaza el contenido de Assets/LandShader.shader
//     con este archivo completo.
//
//  2. COLORES RECOMENDADOS EN EL INSPECTOR:
//     Dirt / Dark Base   → #141010
//     Rock Dark          → #2E2C29
//     Rock Mid           → #666560
//     Rock Light         → #ADAA9E
//     Moss / Lichen      → #334820
//     Wet Rock Tint      → #1A1E22
//     Specular Color     → #8C8C8C
//
//  3. Diferencias clave respecto al shader Built-in anterior:
//     - Usa HLSLPROGRAM en lugar de CGPROGRAM
//     - Incluye ShaderLibrary URP en lugar de UnityCG.cginc
//     - CBUFFER_START para compatibilidad con SRP Batcher
//     - TransformObjectToWorld / TransformWorldToHClip
//       en lugar de mul(unity_ObjectToWorld, ...)
//     - GetMainLight() + SampleSH() para iluminación URP
//     - Passes: UniversalForward + ShadowCaster + DepthOnly
//
//  4. Si aparece error de compilación:
//     Window → Analysis → Shader Compile Errors
// ============================================================
