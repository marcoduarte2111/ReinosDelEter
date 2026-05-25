Shader "Custom/WoodShader"
{
    Properties
    {
        _WoodLight     ("Wood Light",        Color) = (0.55, 0.36, 0.19, 1)
        _WoodDark      ("Wood Dark",         Color) = (0.30, 0.17, 0.08, 1)
        _SeamColor     ("Plank Seam Color",  Color) = (0.12, 0.06, 0.03, 1)
        _SpecColor2    ("Specular Color",    Color) = (0.40, 0.40, 0.40, 1)
        _Smoothness    ("Smoothness",        Range(0,1))    = 0.22
        _GrainScale    ("Grain Scale",       Range(0.2,10)) = 2.2
        _GrainStrength ("Grain Strength",    Range(0,1))    = 0.6
        _PlankWidth    ("Plank Width",       Range(0.3,8))  = 2.2
        _SeamWidth     ("Seam Width",        Range(0,0.4))  = 0.06
        _RingStretch   ("Grain Stretch",     Range(2,40))   = 14
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }
        LOD 250

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

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _WoodLight, _WoodDark, _SeamColor, _SpecColor2;
                float  _Smoothness, _GrainScale, _GrainStrength;
                float  _PlankWidth, _SeamWidth, _RingStretch;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            float hash(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float vnoise(float2 uv)
            {
                float2 i = floor(uv), f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i),               hash(i + float2(1,0)), u.x),
                            lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), u.x), u.y);
            }

            float fbm(float2 uv)
            {
                float v = 0.0, a = 0.5;
                v += a * vnoise(uv); a *= 0.5; uv *= 2.03;
                v += a * vnoise(uv); a *= 0.5; uv *= 2.03;
                v += a * vnoise(uv); a *= 0.5; uv *= 2.03;
                v += a * vnoise(uv);
                return v;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS  = posWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionCS  = TransformWorldToHClip(posWS);
                OUT.shadowCoord = TransformWorldToShadowCoord(posWS);
                OUT.fogFactor   = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);

                // Patrón de madera en espacio-mundo (grano continuo entre piezas).
                float2 wp = IN.positionWS.xz;

                // Veta alargada: estira en X para hacer líneas largas de grano.
                float2 gp    = float2(wp.x / _RingStretch, wp.y) * _GrainScale;
                float  grain = fbm(gp + fbm(gp * 0.5));

                // Líneas finas de veta.
                float streak = abs(frac(grain * 6.0) - 0.5) * 2.0;

                // Tablones a lo largo de Z, con juntas oscuras.
                float plankCoord = wp.y / _PlankWidth;
                float plankFrac  = frac(plankCoord);
                float seam = 1.0 - smoothstep(0.0, _SeamWidth, min(plankFrac, 1.0 - plankFrac));
                float plankTone = hash(float2(floor(plankCoord), 7.3));

                float  wood = saturate(grain * 0.7 + streak * 0.3 * _GrainStrength + plankTone * 0.2);
                float3 col  = lerp(_WoodDark.rgb, _WoodLight.rgb, wood);
                col = lerp(col, _SeamColor.rgb, seam);

                // Iluminación URP.
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                float3 lightCol = mainLight.color * mainLight.shadowAttenuation;
                float  NdotL    = saturate(dot(N, lightDir));
                float3 diffuse  = col * lightCol * NdotL;
                float3 ambient  = col * SampleSH(N);

                float3 V     = normalize(GetCameraPositionWS() - IN.positionWS);
                float3 H     = normalize(lightDir + V);
                float  NdotH = saturate(dot(N, H));
                float  shiny = max(1.0, _Smoothness * 96.0);
                float  spec  = (NdotH > 0.001) ? exp(log(NdotH) * shiny) * _Smoothness * (1.0 - seam) : 0.0;
                float3 specular = _SpecColor2.rgb * lightCol * spec;

                float3 finalColor = ambient + diffuse + specular;
                finalColor = MixFog(finalColor, IN.fogFactor);
                return half4(saturate(finalColor), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
