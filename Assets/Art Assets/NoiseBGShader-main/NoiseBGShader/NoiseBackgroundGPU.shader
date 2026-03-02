Shader "Noise/BackgroundGPU"
{
    Properties
    {
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
        _ScrollSpeed ("Scroll Speed", Float) = 1


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
            "RenderType"="Opaque"
            // IMPORTANT: "Background" renders before the skybox in URP, so a skybox can overwrite it.
            // Render after skybox but before most geometry instead.
            "Queue"="Geometry-100"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionOS  : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
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
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            // ---- cheap hash / value noise ----
            float hash12(float2 p)
            {
                // stable, cheap hash
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

                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p, float octaves, float lacunarity, float gain)
            {
                float sum = 0.0;
                float amp = 0.5;
                float2 pp = p;

                // unrolled-ish: max 6 octaves
                [unroll] for (int i = 0; i < 6; i++)
                {
                    if (i >= (int)round(clamp(octaves, 1.0, 6.0)))
                        break;
                    sum += amp * valueNoise(pp);
                    pp *= max(1.01, lacunarity);
                    amp *= saturate(gain);
                }

                return sum; // ~0..1
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

            half4 frag(Varyings IN) : SV_Target
            {
                float t = (_T != 0.0) ? _T : _Time.y;

                float2 res = max(_Resolution.xy, 1.0.xx);
                float2 uv = IN.uv;

                // Match CPU generator's coordinate space: centered "pixel" coords.
                // Optionally use object-space XY to avoid UV seams (useful for sphere preview / meshes).
                float2 tiling = max(_Tiling.xy, 0.0001.xx);

                float2 baseUV = ((uv * tiling) - (0.5 * tiling)) * res;
                float2 baseOS = (IN.positionOS.xy * tiling) * res; // object space (ignores transform scale)
                float2 baseWS = (IN.positionWS.xy * _WorldCoordScale) * tiling; // world space (scales with quad)

                float2 base;
                if (_CoordMode > 1.5)
                    base = baseWS;
                else if (_CoordMode > 0.5)
                    base = baseOS;
                else
                    base = baseUV;

                // Back-compat: old toggle still works if used.
                if (_UseObjectSpace > 0.5)
                    base = baseOS;

                // Optional global spin (same for all pixels).
                if (_SpinEnabled > 0.5 && abs(_SpinDegPerSec) > 0.0001)
                {
                    float ang = radians(_SpinDegPerSec) * t;
                    float s = sin(ang);
                    float c = cos(ang);
                    base = rotate2(base, s, c);
                }

                // Optional swirl (twist increases with radius).
                if (_SwirlEnabled > 0.5 && abs(_SwirlDegrees) > 0.0001)
                {
                    float2 swirlBase = float2(base.x * _Aspect, base.y);
                    float r = length(swirlBase);
                    float maxR = length(float2(res.x * 0.5 * _Aspect, res.y * 0.5));
                    float r01 = (maxR > 0.0) ? saturate(r / maxR) : 0.0;

                    float edgeRad = radians(_SwirlDegrees);
                    float ang = edgeRad * pow(r01, max(0.0001, _SwirlFalloff));
                    float s = sin(ang);
                    float c = cos(ang);
                    base = rotate2(base, s, c);
                }

                float2 scrollDirRaw = _ScrollDir.xy;
                float scrollLen = length(scrollDirRaw);
                float2 scrollDir = (scrollLen > 1e-5) ? (scrollDirRaw / scrollLen) : float2(0.0, 0.0);
                float2 scroll = scrollDir * (_ScrollSpeed * t);

                float2 coordPx = base + (res * 0.5) + float2(scroll.x * res.x, scroll.y * res.y);

                // Convert "pixel coords" to noise coords using CPU-like scale: freq = 1/scale.
                float freq = 1.0 / max(0.0001, _Scale);
                float2 p = coordPx * freq;

                // seed offsets
                p += (_Seed * 0.001) * float2(37.0, 91.0);

                // time evolution: approximate "3D noise z" by sliding the plane.
                float timeZ = t * _Speed;
                p += float2(timeZ, -timeZ) * 0.25;

                // Domain warp (cheap): warp p by two fbm fields.
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

                float n = fbm(p, _Octaves, _Lacunarity, _Gain); // 0..1

                // contrast/brightness around mid-point
                float v = (n - 0.5) * _Contrast + 0.5 + _Brightness;
                v = saturate(v);

                half4 col;
                if (_UsePalette > 0.5)
                    col = palette4(v);
                else
                    col = half4(v, v, v, 1.0);

                col.a = 1.0;
                return col;
            }
            ENDHLSL
        }
    }
}

