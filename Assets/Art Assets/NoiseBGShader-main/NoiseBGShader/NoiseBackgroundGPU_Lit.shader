Shader "Noise/BackgroundGPU Lit"
{
    Properties
    {
        [Header(Lit (URP))]
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5

        [HDR] _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionStrength("Emission Strength", Range(0, 5)) = 1

        [Header(Coordinates)]
        _Resolution ("Resolution (for coord space)", Vector) = (256, 144, 0, 0)
        _Aspect ("Aspect (x = width/height)", Float) = 1.7777778
        _CoordMode ("Coord Mode (0=UV 1=Object 2=World)", Float) = 0
        _Tiling ("Tiling (XY)", Vector) = (1, 1, 0, 0)
        _WorldCoordScale ("World Coord Scale", Float) = 1
        _UseObjectSpace ("Use Object Space (legacy) (0/1)", Float) = 0

        [Header(Animation)]
        _T ("Time (seconds, optional)", Float) = 0
        _Speed ("Evolve Speed", Float) = 0.35
        _ScrollDir ("Scroll Direction", Vector) = (0.7, 0.0, 0, 0)
        _ScrollSpeed ("Scroll Speed", Float) = 0.15

        [Header(Noise)]
        _Seed ("Seed", Float) = 1337
        _Scale ("Scale (bigger = larger blobs)", Float) = 80
        _Octaves ("Octaves (1..6)", Float) = 4
        _Lacunarity ("Lacunarity", Float) = 2
        _Gain ("Gain", Float) = 0.5

        [Header(Domain Warp)]
        _WarpEnabled ("Warp Enabled (0/1)", Float) = 1
        _WarpSeed ("Warp Seed", Float) = 1337
        _WarpScale ("Warp Scale", Float) = 55
        _WarpAmp ("Warp Amplitude", Float) = 18
        _WarpOctaves ("Warp Octaves (1..4)", Float) = 3

        [Header(Swirl)]
        _SwirlEnabled ("Swirl Enabled (0/1)", Float) = 0
        _SwirlDegrees ("Swirl Degrees (edge)", Float) = 0
        _SwirlFalloff ("Swirl Falloff", Float) = 1.6
        _SpinEnabled ("Spin Enabled (0/1)", Float) = 0
        _SpinDegPerSec ("Spin Deg/Sec", Float) = 30

        [Header(Stylization)]
        _Contrast ("Contrast", Float) = 1.25
        _Brightness ("Brightness", Float) = 0

        [Header(Palette Banding)]
        _UsePalette ("Use Palette (0/1)", Float) = 1
        _Palette0 ("Palette 0", Color) = (0.05, 0.05, 0.07, 1)
        _Palette1 ("Palette 1", Color) = (0.20, 0.16, 0.30, 1)
        _Palette2 ("Palette 2", Color) = (0.44, 0.18, 0.52, 1)
        _Palette3 ("Palette 3", Color) = (0.85, 0.72, 0.92, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType"="Lit"
            // Render after skybox but before most geometry.
            "Queue"="Geometry-100"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            // Make this behave like a standard opaque Lit material (no "see-through" from double-sided + no depth write).
            Cull Back
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            // URP lighting variants (keep it "real Lit")
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            // SSAO relies on URP's screen-space normal pipeline; for a background quad it can cause artifacts.
            // Keep it off to avoid "glitchy" screen-space sampling.
            // #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                half3  normalWS    : TEXCOORD2;
                half   fogFactor   : TEXCOORD3;
                float3 positionOS  : TEXCOORD4;
                float4 positionCS  : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half4 _EmissionColor;
                half _EmissionStrength;

                float4 _Resolution;
                float _Aspect;
                float _CoordMode;
                float4 _Tiling;
                float _WorldCoordScale;
                float _UseObjectSpace;

                float _T;
                float _Speed;
                float4 _ScrollDir;
                float _ScrollSpeed;

                float _Seed;
                float _Scale;
                float _Octaves;
                float _Lacunarity;
                float _Gain;

                float _WarpEnabled;
                float _WarpSeed;
                float _WarpScale;
                float _WarpAmp;
                float _WarpOctaves;

                float _SwirlEnabled;
                float _SwirlDegrees;
                float _SwirlFalloff;
                float _SpinEnabled;
                float _SpinDegPerSec;

                float _Contrast;
                float _Brightness;

                float _UsePalette;
                float4 _Palette0;
                float4 _Palette1;
                float4 _Palette2;
                float4 _Palette3;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.fogFactor = (half)ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            // ---- cheap hash / value noise ----
            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash12(i + 0.0);
                float b = hash12(i + float2(1.0, 0.0));
                float c = hash12(i + float2(0.0, 1.0));
                float d = hash12(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p, float octaves, float lacunarity, float gain)
            {
                float sum = 0.0;
                float amp = 0.5;
                float2 pp = p;

                [unroll] for (int i = 0; i < 6; i++)
                {
                    if (i >= (int)round(clamp(octaves, 1.0, 6.0)))
                        break;
                    sum += amp * valueNoise(pp);
                    pp *= max(1.01, lacunarity);
                    amp *= saturate(gain);
                }
                return sum;
            }

            float2 rotate2(float2 v, float s, float c)
            {
                return float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            half4 palette4(float v)
            {
                int idx = clamp((int)floor(v * 4.0), 0, 3);
                if (idx == 0) return (half4)_Palette0;
                if (idx == 1) return (half4)_Palette1;
                if (idx == 2) return (half4)_Palette2;
                return (half4)_Palette3;
            }

            half4 frag(Varyings IN, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float t = (_T != 0.0) ? _T : _Time.y;

                float2 res = max(_Resolution.xy, 1.0.xx);
                float2 uv = IN.uv;

                float2 tiling = max(_Tiling.xy, 0.0001.xx);

                float2 baseUV = ((uv * tiling) - (0.5 * tiling)) * res;
                float2 baseOS = (IN.positionOS.xy * tiling) * res;
                float2 baseWS = (IN.positionWS.xy * _WorldCoordScale) * tiling;

                float2 base;
                if (_CoordMode > 1.5)
                    base = baseWS;
                else if (_CoordMode > 0.5)
                    base = baseOS;
                else
                    base = baseUV;

                if (_UseObjectSpace > 0.5)
                    base = baseOS;

                if (_SpinEnabled > 0.5 && abs(_SpinDegPerSec) > 0.0001)
                {
                    float ang = radians(_SpinDegPerSec) * t;
                    base = rotate2(base, sin(ang), cos(ang));
                }

                if (_SwirlEnabled > 0.5 && abs(_SwirlDegrees) > 0.0001)
                {
                    float2 swirlBase = float2(base.x * _Aspect, base.y);
                    float r = length(swirlBase);
                    float maxR = length(float2(res.x * 0.5 * _Aspect, res.y * 0.5));
                    float r01 = (maxR > 0.0) ? saturate(r / maxR) : 0.0;

                    float edgeRad = radians(_SwirlDegrees);
                    float ang = edgeRad * pow(r01, max(0.0001, _SwirlFalloff));
                    base = rotate2(base, sin(ang), cos(ang));
                }

                float2 scrollDirRaw = _ScrollDir.xy;
                float scrollLen = length(scrollDirRaw);
                float2 scrollDir = (scrollLen > 1e-5) ? (scrollDirRaw / scrollLen) : float2(0.0, 0.0);
                float2 scroll = scrollDir * (_ScrollSpeed * t);
                float2 coordPx = base + (res * 0.5) + float2(scroll.x * res.x, scroll.y * res.y);

                float freq = 1.0 / max(0.0001, _Scale);
                float2 p = coordPx * freq;
                p += (_Seed * 0.001) * float2(37.0, 91.0);

                float timeZ = t * _Speed;
                p += float2(timeZ, -timeZ) * 0.25;

                if (_WarpEnabled > 0.5 && _WarpAmp > 0.0)
                {
                    float warpFreq = 1.0 / max(0.0001, _WarpScale);
                    float2 wp = coordPx * warpFreq;
                    wp += (_WarpSeed * 0.001) * float2(13.0, 57.0);
                    wp += float2(timeZ, timeZ) * 0.15;

                    float wx = fbm(wp + 11.7, _WarpOctaves, _Lacunarity, _Gain);
                    float wy = fbm(wp + 63.2, _WarpOctaves, _Lacunarity, _Gain);
                    float2 wv = (float2(wx, wy) * 2.0 - 1.0) * (_WarpAmp * freq);
                    p += wv;
                }

                float noise01 = fbm(p, _Octaves, _Lacunarity, _Gain); // 0..1
                float v = (noise01 - 0.5) * _Contrast + 0.5 + _Brightness;
                v = saturate(v);

                half4 noiseCol = (_UsePalette > 0.5) ? palette4(v) : half4(v, v, v, 1.0);

                // ---- URP Lit shading (robust Lambert) ----
                half3 albedo = saturate(noiseCol.rgb * _BaseColor.rgb);
                half3 emission = noiseCol.rgb * _EmissionColor.rgb * _EmissionStrength;

                half3 normalWS = NormalizeNormalPerPixel(IN.normalWS);
                // Two-sided: flip normal on backfaces so lighting stays consistent.
                normalWS = (facing > 0.0h) ? normalWS : -normalWS;

                // Ambient from probes (works even without any lights).
                half3 ambient = SampleSH(normalWS) * albedo;

                // Main + additional lights (diffuse only).
                half3 lit = ambient;

                Light mainLight = GetMainLight();
                half ndl = saturate(dot(normalWS, mainLight.direction));
                lit += albedo * mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation) * ndl;

                #if defined(_ADDITIONAL_LIGHTS)
                uint additionalCount = GetAdditionalLightsCount();
                for (uint li = 0u; li < additionalCount; li++)
                {
                    Light l = GetAdditionalLight(li, IN.positionWS);
                    half ndl2 = saturate(dot(normalWS, l.direction));
                    lit += albedo * l.color * (l.distanceAttenuation * l.shadowAttenuation) * ndl2;
                }
                #endif

                half3 finalRgb = lit + emission;
                finalRgb = MixFog(finalRgb, IN.fogFactor);
                return half4(finalRgb, 1.0);
            }
            ENDHLSL
        }
    }
}

