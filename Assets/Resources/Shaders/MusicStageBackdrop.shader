// The music app's stage backdrop: a slow domain-warped colour flow with drifting light dust, drawn into a small RenderTexture with Graphics.Blit.
// Colours come from the cover; the three band levels (low, mid, high) of the playing audio move the flow, the glow and the dust.
Shader "Hidden/RailwayRenaissance/MusicStageBackdrop"
{
    Properties { _MainTex ("Unused", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _ColA, _ColB, _Base;
            float _Clock, _Low, _Mid, _High, _Energy, _Aspect, _Seed, _Beat;
            float _Warp, _DustAmt, _Lights, _Bright;   // the backdrop look of the current lyric style

            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i), b = hash21(i + float2(1, 0)), c = hash21(i + float2(0, 1)), d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float fbm(float2 p)
            {
                float v = 0, a = .5;
                [unroll] for (int i = 0; i < 4; i++) { v += a * noise(p); p = p * 2.03 + 17.1; a *= .5; }
                return v;
            }

            // A layer of drifting points: each cell holds one, which rises slowly and twinkles.
            float dust(float2 uv, float scale, float speed, float seed, float twinkle)
            {
                float2 p = uv * scale; p.y -= _Clock * speed;
                float2 cell = floor(p), local = frac(p) - .5;
                float h = hash21(cell + seed);
                float2 offset = (float2(hash21(cell + 3.1 + seed), hash21(cell + 7.7 + seed)) - .5) * .6;
                float d = length(local - offset);
                float size = .035 + h * .05;
                float spark = smoothstep(size, 0.0, d);
                float blink = .55 + .45 * sin(_Clock * (.7 + h * 1.8) * (1.0 + twinkle) + h * 40.0);
                return spark * blink * step(.45, h);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 p = float2(uv.x * _Aspect, uv.y) * 1.5 + _Seed * 9.0;
                float t = _Clock * .045;
                float warp = (1.6 + _Low * 1.4) * _Warp;
                float2 q = float2(fbm(p + t), fbm(p + float2(5.2, 1.3) - t * .8));
                float2 r = float2(fbm(p + warp * q + float2(1.7, 9.2) + t * .6), fbm(p + warp * q + float2(8.3, 2.8) - t * .5));
                float f = fbm(p + warp * r);

                float3 col = _Base.rgb;
                col = lerp(col, _ColA.rgb, smoothstep(.2, .8, f) * (.75 + _Energy * .4));
                col = lerp(col, _ColB.rgb, smoothstep(.3, .9, length(q)) * (.55 + _Mid * .4));
                col += _ColA.rgb * smoothstep(.55, .95, r.x) * (.12 + _Low * .22);

                // A soft light that sits low and breathes with the low band.
                float2 c = float2(.5 + .22 * sin(t * 3.0 + _Seed * 6.0), .12);
                float glow = exp(-dot((uv - c) * float2(1.0, 1.7), (uv - c) * float2(1.0, 1.7)) * 5.0);
                col += lerp(_ColA.rgb, _ColB.rgb, .4) * glow * (.12 + _Low * .3);

                // Two broad lights wander on slow, unequal paths (about a minute a lap), so no two moments of the song look alike; a beat lifts them.
                float tl = _Clock * .11;
                float2 l1 = float2(.5 + .36 * sin(tl * 1.0 + _Seed * 6.0), .5 + .28 * cos(tl * .83 + _Seed * 3.0));
                float2 l2 = float2(.5 + .4 * cos(tl * .71 + 2.0 + _Seed * 5.0), .5 + .32 * sin(tl * 1.13 + 1.0 + _Seed * 2.0));
                float2 d1 = (uv - l1) * float2(_Aspect, 1.0), d2 = (uv - l2) * float2(_Aspect, 1.0);
                col += _ColA.rgb * exp(-dot(d1, d1) * 5.5) * (.10 + _Mid * .22 + _Beat * .22) * _Lights;
                col += _ColB.rgb * exp(-dot(d2, d2) * 5.5) * (.10 + _Low * .2 + _Beat * .2) * _Lights;
                // The mix of the two cover colours drifts over time.
                float drift = .5 + .5 * sin(_Clock * .05 + _Seed * 6.0);
                col = lerp(col, lerp(_ColA.rgb, _ColB.rgb, drift) * (.5 + f * .7), smoothstep(.35, .9, f) * .22);

                // Dust in two depths; the high band makes it twinkle.
                float2 duv = float2(uv.x * _Aspect, uv.y);
                float d = dust(duv, 9.0, .035, _Seed, _High * 2.5) * .9 + dust(duv, 17.0, .06, _Seed + 11.0, _High * 3.5) * .6;
                col += lerp(float3(1, 1, 1), _ColA.rgb, .35) * d * (.25 + _High * .5) * _DustAmt;

                // Vignette and a ceiling so lyrics on top stay readable.
                float2 v = uv - .5;
                col *= saturate(1.15 - dot(v, v) * 1.5);
                col *= 1.3 * _Bright; col = col / (1.0 + col * .55);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
