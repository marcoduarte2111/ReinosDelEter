Shader "Custom/WaterShader"
{
    Properties
    {
        _DeepColor("Deep Water Color", Color) = (0,0.25,0.8,1)
        _ShallowColor("Shallow Water Color", Color) = (0.4,0.8,1,1)
        _FresnelColor("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(0.1,5.0)) = 2.0
        _Transparency("Transparency", Range(0,1)) = 0.8
        _Amplitude1("Amplitude1", Range(0,1)) = 0.1
        _Frequency1("Frequency1", Range(0,10)) = 3
        _Speed1("Speed1", Range(0,10)) = 1
        _Direction1("Direction1 (X,Z)", Vector) = (1,0,0,0)
        _Amplitude2("Amplitude2", Range(0,1)) = 0.06
        _Frequency2("Frequency2", Range(0,10)) = 2
        _Speed2("Speed2", Range(0,10)) = 1.3
        _Direction2("Direction2 (X,Z)", Vector) = (0,1,0,0)
        _Amplitude3("Amplitude3", Range(0,1)) = 0.08
        _Frequency3("Frequency3", Range(0,10)) = 4
        _Speed3("Speed3", Range(0,10)) = 1.8
        _Direction3("Direction3 (X,Z)", Vector) = (0.7,0.7,0,0)
        _FoamColor("Foam Color", Color) = (1,1,1,1)
        _FoamTexture("Foam Texture (R)", 2D) = "white" {}
        _FoamScale("Foam Texture Scale", Range(0,10)) = 3.0
        _FoamIntensity("Foam Intensity", Range(0,2)) = 1.0
        _SeaLevel("Base Sea Level", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
        }
        LOD 200

        // ────────────────────────────────────────────────────────────────
        //  PASS — UniversalForward (transparente, unlit con fresnel + espuma)
        // ────────────────────────────────────────────────────────────────
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── CBUFFER (SRP Batcher compatible) ─────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FresnelColor;
                float  _FresnelPower;
                float  _Transparency;
                float  _Amplitude1, _Frequency1, _Speed1;
                float4 _Direction1;
                float  _Amplitude2, _Frequency2, _Speed2;
                float4 _Direction2;
                float  _Amplitude3, _Frequency3, _Speed3;
                float4 _Direction3;
                float4 _FoamColor;
                float4 _FoamTexture_ST;
                float  _FoamScale;
                float  _FoamIntensity;
                float  _SeaLevel;
            CBUFFER_END

            TEXTURE2D(_FoamTexture);
            SAMPLER(sampler_FoamTexture);

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

            // ── Gerstner Wave (igual que el shader Built-in original) ─────
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

                // .xy a propósito: preserva las direcciones afinadas en el material
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
                OUT.waveHeight = posWS.y - _SeaLevel;
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetCameraPositionWS() - IN.positionWS);

                float fresnelFactor = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
                float shallowFactor = saturate(IN.positionWS.y * 0.5 + 0.5);
                float3 waterColor   = lerp(_DeepColor.rgb, _ShallowColor.rgb, shallowFactor);
                float3 finalColor   = lerp(waterColor, _FresnelColor.rgb, fresnelFactor);

                float foamFactorHeight = smoothstep(0.02, 0.10, IN.waveHeight);
                float slopeFactor      = (1.0 - saturate(dot(N, float3(0, 1, 0)))) * 0.5;
                float foamTex          = SAMPLE_TEXTURE2D(_FoamTexture, sampler_FoamTexture, IN.uv * _FoamScale).r;
                float foamFactor       = saturate((foamFactorHeight + slopeFactor) * foamTex * _FoamIntensity);
                finalColor = lerp(finalColor, _FoamColor.rgb, foamFactor);

                finalColor = MixFog(finalColor, IN.fogFactor);
                return half4(finalColor, _Transparency);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
