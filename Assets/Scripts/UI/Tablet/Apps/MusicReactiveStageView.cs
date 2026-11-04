using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // Independent native renderer: cover palette + audio-clock geometry + measured PCM energy.
    public sealed class MusicReactiveStageView : VisualElement
    {
        static readonly Dictionary<Texture2D, Color[]> Palettes = new Dictionary<Texture2D, Color[]>();
        readonly float[] output = new float[512];
        float[] decoded;
        Color primary, secondary, fromPrimary, fromSecondary, targetPrimary, targetSecondary;
        float blend = 1, energy, transient, lastRaw, nextFrame, clock, seed, seedTarget;
        int composition;
        bool initialized;
        public string TrackId { get; private set; }
        public Color PrimaryColor => targetPrimary;
        public float MeasuredPower { get; private set; }
        public bool UsedDecodedSamples { get; private set; }
        public int Composition => composition;
        public float RenderClock => clock;
        public float VisualEnergy => energy;
        // Smoothed low / mid / high band levels (0..1) from the playing source's spectrum; they follow the audio, not a timer.
        public float BandLow => bandLow;
        public float BandMid => bandMid;
        public float BandHigh => bandHigh;
        readonly float[] spectrum = new float[512];
        float bandLow, bandMid, bandHigh;

        // The backdrop is drawn by a shader into a small RenderTexture (see Resources/Shaders/MusicStageBackdrop.shader); when the shader
        // cannot be used the older painter fields below are the whole backdrop.
        static Shader backdropShader;
        Material backdropMaterial; RenderTexture backdropTexture; bool backdropFailed; float flowClock;
        public bool HasShaderBackdrop => backdropTexture != null;

        void EnsureBackdrop()
        {
            if (backdropFailed || backdropTexture != null) return;
            try
            {
                if (backdropShader == null) backdropShader = Resources.Load<Shader>("Shaders/MusicStageBackdrop");
                if (backdropShader == null || !backdropShader.isSupported) { backdropFailed = true; return; }
                backdropMaterial = new Material(backdropShader) { hideFlags = HideFlags.HideAndDontSave };
                backdropTexture = new RenderTexture(512, 320, 0, RenderTextureFormat.ARGB32) { name = "MusicStageBackdrop", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                backdropTexture.Create();
                style.backgroundImage = new StyleBackground(Background.FromRenderTexture(backdropTexture));
            }
            catch (Exception) { backdropFailed = true; ReleaseBackdrop(); }
        }

        void ReleaseBackdrop()
        {
            if (backdropTexture != null) { style.backgroundImage = new StyleBackground(StyleKeyword.None); backdropTexture.Release(); UnityEngine.Object.Destroy(backdropTexture); backdropTexture = null; }
            if (backdropMaterial != null) { UnityEngine.Object.Destroy(backdropMaterial); backdropMaterial = null; }
        }

        void RenderBackdrop()
        {
            EnsureBackdrop();
            if (backdropTexture == null || backdropMaterial == null) return;
            if (!backdropTexture.IsCreated()) backdropTexture.Create();
            var m = backdropMaterial; float w = Mathf.Max(contentRect.width, 1f), h = Mathf.Max(contentRect.height, 1f);
            m.SetColor("_ColA", primary); m.SetColor("_ColB", secondary); m.SetColor("_Base", new Color(.035f, .042f, .05f));
            m.SetFloat("_Clock", flowClock); m.SetFloat("_Low", bandLow); m.SetFloat("_Mid", bandMid); m.SetFloat("_High", bandHigh);
            m.SetFloat("_Energy", energy); m.SetFloat("_Aspect", w / h); m.SetFloat("_Seed", seed);
            Graphics.Blit(null, backdropTexture, m);
        }

        public MusicReactiveStageView()
        {
            RegisterCallback<DetachFromPanelEvent>(_ => ReleaseBackdrop());
            name = "music-reactive-stage";
            pickingMode = PickingMode.Ignore;
            AddToClassList("music-reactive-stage");
            generateVisualContent += Draw;
        }

        public void SetTrack(MusicLibraryTrack track, bool refreshCover = false)
        {
            string id = track?.id ?? "unknown";
            if (id == TrackId && !refreshCover) return;
            TrackId = id;
            uint hash = 2166136261;
            foreach (char c in id) hash = (hash ^ c) * 16777619;
            seedTarget = (hash % 1000) / 1000f; if (!initialized) seed = seedTarget;
            composition = id == "south_wind" ? 0 : id == "starlit_rails" ? 1 : id == "chollima_ride" ? 2 : (int)(hash % 3);
            var palette = Palette(MusicAlbumArtResolver.GetAlbumCover(id, track?.category));
            fromPrimary = initialized ? primary : palette[0];
            fromSecondary = initialized ? secondary : palette[1];
            targetPrimary = Tint(palette[0], (seedTarget - .5f) * .08f);
            targetSecondary = Tint(palette[1], (.5f - seedTarget) * .08f);
            // Art direction keeps the four original vocal songs visually distinct, including shared sleeves.
            if (id == "south_wind") { targetPrimary = Color.Lerp(targetPrimary, new Color(.64f, .5f, .2f), .65f); targetSecondary = new Color(.22f, .49f, .36f); }
            if (id == "starlit_rails") { targetPrimary = new Color(.23f, .35f, .65f); targetSecondary = new Color(.53f, .28f, .46f); }
            if (id == "chollima_ride") { targetPrimary = new Color(.65f, .23f, .12f); targetSecondary = new Color(.67f, .48f, .17f); }
            if (id == "sleepers") { targetPrimary = new Color(.25f, .42f, .52f); targetSecondary = new Color(.66f, .4f, .2f); }
            blend = initialized ? 0 : 1;
            primary = initialized ? primary : targetPrimary;
            secondary = initialized ? secondary : targetSecondary;
            initialized = true; transient = lastRaw = 0;
            style.backgroundColor = Color.Lerp(new Color(.045f, .055f, .06f), primary, .12f);
            MarkDirtyRepaint();
        }

        static Color Tint(Color color, float shift)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return Color.HSVToRGB(Mathf.Repeat(h + shift, 1), Mathf.Clamp(s, .35f, .78f), Mathf.Clamp(v, .4f, .72f));
        }

        /// <summary>The app accent for a cover: its strongest colour, lifted and saturated enough to read on the dark interface.</summary>
        public static Color AccentFor(Texture2D cover)
        {
            Color.RGBToHSV(Palette(cover)[0], out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Clamp(s, .42f, .8f), Mathf.Clamp(v + .2f, .78f, .95f));
        }

        static Color[] Palette(Texture2D cover)
        {
            if (cover == null) return new[] { new Color(.55f, .4f, .2f), new Color(.2f, .4f, .4f) };
            if (Palettes.TryGetValue(cover, out var cached)) return cached;
            var previous = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(24, 24, 0, RenderTextureFormat.ARGB32);
            var sample = new Texture2D(24, 24, TextureFormat.RGB24, false);
            var sums = new Vector3[18]; var weights = new float[18];
            try
            {
                Graphics.Blit(cover, rt); RenderTexture.active = rt;
                sample.ReadPixels(new Rect(0, 0, 24, 24), 0, 0); sample.Apply();
                foreach (var c in sample.GetPixels())
                {
                    Color.RGBToHSV(c, out float h, out float s, out float v);
                    if (v < .12f || s < .15f) continue;
                    int bin = Mathf.Min(17, (int)(h * 18));
                    float weight = s * Mathf.Sqrt(v);
                    sums[bin] += new Vector3(c.r, c.g, c.b) * weight; weights[bin] += weight;
                }
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.Destroy(sample); }
            int first = 0, second = 9;
            for (int i = 1; i < 18; i++) if (weights[i] > weights[first]) first = i;
            float score = -1;
            for (int i = 0; i < 18; i++)
            {
                int distance = Math.Min(Math.Abs(i - first), 18 - Math.Abs(i - first));
                float candidate = weights[i] * distance;
                if (candidate > score) { score = candidate; second = i; }
            }
            Color Mean(int index, Color fallback) => weights[index] > 0
                ? new Color(sums[index].x / weights[index], sums[index].y / weights[index], sums[index].z / weights[index]) : fallback;
            cached = new[] { Tint(Mean(first, new Color(.55f, .4f, .2f)), 0), Tint(Mean(second, new Color(.2f, .4f, .4f)), 0) };
            Palettes[cover] = cached; return cached;
        }

        // PCM fallback measures the current decoded sample window, never a random or timer-driven level.
        public float ReadPower(AudioSource source)
        {
            UsedDecodedSamples = false;
            if (source == null || source.clip == null) return 0;
            source.GetOutputData(output, 0);
            double sum = 0;
            foreach (float value in output) sum += value * value;
            if (sum < .0000001 && source.clip.loadType == AudioClipLoadType.DecompressOnLoad && source.clip.loadState == AudioDataLoadState.Loaded)
            {
                int size = 512 * source.clip.channels;
                if (decoded == null || decoded.Length != size) decoded = new float[size];
                if (source.clip.GetData(decoded, Mathf.Clamp(source.timeSamples, 0, source.clip.samples - 1)))
                {
                    sum = 0; foreach (float value in decoded) sum += value * value;
                    UsedDecodedSamples = true;
                    return Mathf.Sqrt((float)(sum / decoded.Length));
                }
            }
            return Mathf.Sqrt((float)(sum / output.Length));
        }

        // Bins are ~43 Hz wide at 44.1 kHz: low ≈ 40–260 Hz, mid ≈ 260 Hz–3 kHz, high ≈ 3–11 kHz. When the engine hands out no spectrum
        // (decoded-only fallback) the bands follow the measured power instead, so they never invent a level.
        void ReadBands(AudioSource source, float dt)
        {
            float low = 0, mid = 0, high = 0;
            source.GetSpectrumData(spectrum, 0, FFTWindow.Hanning);
            double sumLow = 0, sumMid = 0, sumHigh = 0;
            for (int i = 1; i <= 6; i++) sumLow += spectrum[i] * spectrum[i];
            for (int i = 7; i <= 70; i++) sumMid += spectrum[i] * spectrum[i];
            for (int i = 71; i <= 255; i++) sumHigh += spectrum[i] * spectrum[i];
            low = Mathf.Clamp01(Mathf.Sqrt((float)(sumLow / 6)) * 9f);
            mid = Mathf.Clamp01(Mathf.Sqrt((float)(sumMid / 64)) * 22f);
            high = Mathf.Clamp01(Mathf.Sqrt((float)(sumHigh / 185)) * 60f);
            if (low + mid + high <= 0.0001f) { low = mid = high = Mathf.Clamp01(MeasuredPower * 4); }
            float rise = 1 - Mathf.Exp(-dt * 16), fall = 1 - Mathf.Exp(-dt * 5);
            bandLow = Mathf.Lerp(bandLow, low, low > bandLow ? rise : fall);
            bandMid = Mathf.Lerp(bandMid, mid, mid > bandMid ? rise : fall);
            bandHigh = Mathf.Lerp(bandHigh, high, high > bandHigh ? rise : fall);
        }

        public void Tick(AudioSource source, bool playing, bool reducedMotion)
        {
            if (Time.unscaledTime < nextFrame) return;
            float dt = Mathf.Clamp(Time.unscaledTime - (nextFrame - 1f / 30), .001f, .1f);
            nextFrame = Time.unscaledTime + 1f / 30;
            bool changed = blend < 1;
            blend = reducedMotion ? 1 : Mathf.Min(1, blend + dt / .9f);
            seed = reducedMotion ? seedTarget : Mathf.MoveTowards(seed, seedTarget, dt / 1.4f);
            if (seed != seedTarget && !changed) MarkDirtyRepaint();
            float eased = blend * blend * (3 - 2 * blend);
            primary = Color.Lerp(fromPrimary, targetPrimary, eased);
            secondary = Color.Lerp(fromSecondary, targetSecondary, eased);
            if (changed) style.backgroundColor = Color.Lerp(new Color(.045f, .055f, .06f), primary, .12f);
            if (reducedMotion)
            {
                changed |= energy != 0 || clock != 0;
                clock = energy = transient = 0;
                bandLow = bandMid = bandHigh = 0;
            }
            else if (playing && source != null && source.clip != null)
            {
                MeasuredPower = ReadPower(source);
                float raw = Mathf.Clamp01(MeasuredPower * 4);
                energy = Mathf.Lerp(energy, raw, 1 - Mathf.Exp(-dt * (raw > energy ? 15 : 4)));
                transient = Mathf.Lerp(transient, Mathf.Clamp01((raw - lastRaw) * 5), 1 - Mathf.Exp(-dt * 12));
                lastRaw = raw; clock = source.time; changed = true;
                ReadBands(source, dt);
            }
            else if (bandLow + bandMid + bandHigh > 0)
            {
                // Paused: let the bands settle instead of freezing mid-pulse.
                float fall = 1 - Mathf.Exp(-dt * 6);
                bandLow = Mathf.Lerp(bandLow, 0, fall); bandMid = Mathf.Lerp(bandMid, 0, fall); bandHigh = Mathf.Lerp(bandHigh, 0, fall);
            }
            if (!reducedMotion) { flowClock += dt; changed = true; }
            if (changed || backdropTexture == null) RenderBackdrop();
            if (changed) MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            float w = contentRect.width, h = contentRect.height;
            if (w < 2 || h < 2) return;
            var painter = context.painter2D;
            float t = clock * .06f + seed * 6;
            float pulse = 1 + energy * .13f + transient * .05f;
            for (int i = 0; i < 4; i++)
            {
                float x = composition == 0 ? .12f + i * .27f : composition == 1 ? .5f + .36f * Mathf.Sin(i * 1.6f + seed * 4) : .12f + i * .28f;
                float y = composition == 0 ? .25f + .48f * (i % 2) : composition == 1 ? .18f + i * .23f : .8f - i * .2f;
                x += .08f * Mathf.Sin(t + i * 1.7f); y += .08f * Mathf.Cos(t * .7f + i);
                SoftField(context, new Vector2(w * x, h * y), w * (.35f + seed * .12f) * pulse,
                    h * (composition == 2 ? .3f : .5f) * pulse, i % 2 == 0 ? primary : secondary, (.32f + energy * .18f) * (backdropTexture != null ? .4f : 1f));
            }
            if (backdropTexture != null) return;
            painter.lineWidth = 1;
            var ink = Color.Lerp(primary, secondary, .5f); ink.a = .12f + energy * .12f;
            painter.strokeColor = ink;
            for (int i = 0; i < 5; i++)
            {
                float y = h * (.65f + i * .055f);
                painter.BeginPath(); painter.MoveTo(new Vector2(0, y));
                painter.BezierCurveTo(new Vector2(w * .3f, y - h * (.18f + energy * .05f)), new Vector2(w * .7f, y + h * .12f * Mathf.Sin(t)), new Vector2(w, y - h * .08f)); painter.Stroke();
            }
        }

        static void SoftField(MeshGenerationContext context, Vector2 centre, float rx, float ry, Color color, float opacity)
        {
            const int segments = 40, rings = 6;
            var mesh = context.Allocate(1 + rings * segments, (segments + (rings - 1) * segments * 2) * 3);
            var vertex = new Vertex { position = new Vector3(centre.x, centre.y, Vertex.nearZ), tint = new Color(color.r, color.g, color.b, opacity) };
            mesh.SetNextVertex(vertex);
            for (int ring = 1; ring <= rings; ring++)
            {
                float radius = ring / (float)rings;
                float alpha = opacity * Mathf.Pow(1 - radius * radius, 2);
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / segments;
                    vertex.position = new Vector3(centre.x + Mathf.Cos(angle) * rx * radius, centre.y + Mathf.Sin(angle) * ry * radius, Vertex.nearZ);
                    vertex.tint = new Color(color.r, color.g, color.b, alpha); mesh.SetNextVertex(vertex);
                }
            }
            void Triangle(int a, int b, int c) { mesh.SetNextIndex((ushort)a); mesh.SetNextIndex((ushort)b); mesh.SetNextIndex((ushort)c); }
            for (int i = 0; i < segments; i++) Triangle(0, 1 + i, 1 + (i + 1) % segments);
            for (int ring = 1; ring < rings; ring++) for (int i = 0; i < segments; i++)
            {
                int a = 1 + (ring - 1) * segments + i, b = 1 + (ring - 1) * segments + (i + 1) % segments;
                int c = a + segments, d = b + segments; Triangle(a, c, b); Triangle(b, c, d);
            }
        }
    }
}
