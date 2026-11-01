using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage style "Claddagh" (回环), after Folia's ring style. Lyrics sit on a slender ellipse tilted across the stage. The line being sung
    /// is on the front half of the ring and the ring turns as the line is sung, so the character being sung is always at the front, largest
    /// and brightest; the next line waits on the back half, smaller, dimmer and closer together. When the line changes the ring turns
    /// half a revolution on a spring. Characters follow the ring's tangent, so they tilt along its curve. Bass swells the ring.
    /// A function of the audio clock apart from the ring's turn, which settles on a spring. Independent implementation of the behaviour.
    /// </summary>
    public sealed class MusicStageCladdagh : IStageStyle
    {
        sealed class RingGlyph
        {
            public VisualElement box; public Label label;
            public float nominal;           // angle of the glyph along its line, in radians, 0 at the first glyph
            public float start, end;
        }

        sealed class RingLine
        {
            public int index; public List<RingGlyph> glyphs = new List<RingGlyph>();
            public float total;             // the angle at which the line ends
            public float[] starts;
        }

        const int Behind = 1, Ahead = 2;                 // lines kept on the ring around the centre line
        const float FocusScale = .65f, MinorRatio = .09f, SpacingBoost = 2f;
        // The ring lies along a diagonal; on a short stage the diagonal is flattened so the ring's ends stay on the stage (45 degrees on a tall one).
        float tiltDegrees = 45f;

        readonly VisualElement root = new VisualElement(), ring = new VisualElement();
        readonly Label translation = new Label();
        readonly List<RingLine> lines = new List<RingLine>();
        MusicTimedDocument shownDocument; int shownCentre = -9; float shownScale, shownWidth, shownHeight; bool shownEstimate, shownCalm; StageTuning shownTuning;
        float offset, offsetVelocity;      // ring rotation in radians (a multiple of PI when settled)
        bool offsetKnown;
        float rx, ry, glyphSize;

        public string Id => "claddagh";
        public string Title => "回环";
        public bool OwnsSubtitles => true;
        public VisualElement Root => root;

        public MusicStageCladdagh()
        {
            root.AddToClassList("music-stage-claddagh"); root.pickingMode = PickingMode.Ignore;
            ring.AddToClassList("music-stage-claddagh-ring"); ring.pickingMode = PickingMode.Ignore;
            translation.AddToClassList("music-stage-claddagh-translation"); translation.pickingMode = PickingMode.Ignore;
            root.Add(ring); root.Add(translation);
        }

        public void Invalidate() { shownDocument = null; shownCentre = -3; }

        static float Advance(string text, float size)
        {
            float total = 0;
            foreach (char ch in text) total += (ch >= 0x2E80 ? 1f : ch == ' ' || ch == ' ' ? .35f : .55f) * size;
            return Mathf.Max(size * .35f, total);
        }

        public void Tick(in StageContext c)
        {
            var frame = root.parent ?? root;
            float width = float.IsNaN(frame.resolvedStyle.width) ? 0 : frame.resolvedStyle.width, height = float.IsNaN(frame.resolvedStyle.height) ? 0 : frame.resolvedStyle.height;
            // The ring fills most of the stage; the host's size is stable, so rebuilding on it cannot loop.
            root.style.height = Mathf.Max(260f, (height < 120 ? 460f : height) * .78f);
            int centre = c.active;
            if (!ReferenceEquals(c.document, shownDocument) || centre != shownCentre || Mathf.Abs(c.fontScale - shownScale) > .01f || Mathf.Abs(width - shownWidth) > 24f ||
                Mathf.Abs(height - shownHeight) > 40f || c.estimate != shownEstimate || c.calm != shownCalm || !c.tuning.Equals(shownTuning))
                Rebuild(c, width, height);

            float dt = Mathf.Clamp(c.dt, 0, 1f / 20f);
            // The ring turns half a revolution per line on a soft spring; a long jump (seek) snaps.
            float target = Mathf.Max(-1, centre) * Mathf.PI;
            if (!offsetKnown || c.calm || Mathf.Abs(target - offset) > Mathf.PI * 1.6f) { offset = target; offsetVelocity = 0; offsetKnown = true; }
            else StageWord.Spring(ref offset, ref offsetVelocity, target, dt, 55f / .9f, 14f / .9f);

            float intensityMultiplier = c.tuning.intensity == 0 ? .08f : c.tuning.intensity == 2 ? .95f : .25f, maxScale = c.tuning.intensity == 0 ? 1.08f : c.tuning.intensity == 2 ? 1.95f : 1.25f;
            float swell = c.calm ? 1f : Mathf.Min(1f + Mathf.Clamp01(c.low) * intensityMultiplier, maxScale);
            float major = rx * swell, minor = major * MinorRatio;
            float tilt = -tiltDegrees * Mathf.Deg2Rad, cosT = Mathf.Cos(tilt), sinT = Mathf.Sin(tilt);
            Color baseColor = new Color(.93f, .9f, .8f, .55f), sungColor = Color.Lerp(Color.white, c.accent, .55f);
            float cx = float.IsNaN(ring.resolvedStyle.width) ? 0 : ring.resolvedStyle.width * .5f, cy = float.IsNaN(ring.resolvedStyle.height) ? 0 : ring.resolvedStyle.height * .5f;

            foreach (var rl in lines)
            {
                var data = c.document.lines[rl.index];
                bool upcoming = rl.index > centre;
                float lineDiff = Mathf.Abs(offset - rl.index * Mathf.PI) / Mathf.PI;
                // The ring turns to the character being sung: its angle along the line, interpolated between glyph start times.
                float wordOffset = 0;
                if (rl.index == centre) wordOffset = WordOffset(rl, c.clock);
                else if (rl.index < centre) wordOffset = rl.total;
                for (int i = 0; i < rl.glyphs.Count; i++)
                {
                    var g = rl.glyphs[i];
                    float theta = rl.index * Mathf.PI + g.nominal, psi = theta - offset - wordOffset;
                    float arc = psi * major, curve = arc / major, cos = Mathf.Cos(curve);
                    float depth = (cos + 1f) * .5f;                                           // 1 on the front of the ring, 0 at the back
                    float spacing = .55f + .45f * Mathf.Pow(depth, 1.2f);                       // characters gather closer together toward the back, but stay apart enough to read
                    float rawX = Mathf.Sin(curve) * major * spacing, rawY = cos * minor;
                    float x = rawX * cosT - rawY * sinT, y = rawX * sinT + rawY * cosT;
                    float tangentX = cos * major, tangentY = -Mathf.Sin(curve) * minor;
                    float angle = Mathf.Atan2(tangentX * sinT + tangentY * cosT, tangentX * cosT - tangentY * sinT) * Mathf.Rad2Deg;
                    if (angle > 90f) angle -= 180f; else if (angle < -90f) angle += 180f;       // never upside down

                    // Focus: 1 for the character being sung on the centre line, falling to 0 away from it and on the back of the ring.
                    float lineFocus = Mathf.Max(0f, 1f - lineDiff), reach = Mathf.Min(1f, Mathf.Abs(arc) / (major * .48f));
                    float focus = lineFocus * Mathf.Pow(1f - reach, 1.8f);
                    float opacity = (.35f + .65f * Mathf.Pow(depth, 1.5f) * (.35f + .65f * focus)) * (.68f + .32f * Mathf.Pow(depth, 1.9f));
                    opacity *= Mathf.Clamp01(2f - lineDiff);
                    if (rl.index < centre) opacity *= Mathf.Max(0f, 1f - lineDiff);
                    if (upcoming) opacity *= .35f + .65f * Mathf.Clamp01((float)((c.clock - (data.start - .8)) / .8));
                    // Away from the focused line, only the back half of the ring may show, so lines never overlap on the front.
                    if (lineDiff > .02f)
                    {
                        float progress = Mathf.Clamp01((lineDiff - .3f) / .6f), threshold = 1f - 1.2f * progress;
                        if (cos > threshold) opacity *= Mathf.Clamp01(1f - (cos - threshold) / .15f);
                    }
                    float scale = (.22f + .98f * Mathf.Pow(depth, 1.5f)) * (1f + FocusScale * focus);
                    bool sung = c.clock >= g.start;
                    g.box.style.opacity = c.calm ? Mathf.Clamp01(opacity * 1.2f) : opacity;
                    g.box.style.translate = new Translate(cx + x, cy + y);
                    g.box.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
                    g.box.style.scale = new Scale(new Vector3(scale, scale, 1));
                    g.label.style.color = sung ? Color.Lerp(baseColor, sungColor, .75f + .25f * focus) : baseColor;
                    g.label.style.textShadow = sung && focus > .15f
                        ? new TextShadow { offset = Vector2.zero, blurRadius = 14f * focus + 4f * c.high, color = new Color(c.accent.r, c.accent.g, c.accent.b, .8f * focus) }
                        : new TextShadow { offset = Vector2.zero, blurRadius = 0, color = Color.clear };
                }
            }

            bool hasTranslation = c.showTranslation && centre >= 0 && centre < c.document.lines.Count && !string.IsNullOrEmpty(c.document.lines[centre].translation);
            translation.style.display = hasTranslation ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasTranslation && translation.text != c.document.lines[centre].translation) { translation.text = c.document.lines[centre].translation; StageFonts.Apply(translation); }
        }

        // The angle the ring has turned through at <clock>: between glyph i and i+1 it is interpolated by time; after the last glyph it eases on.
        static float WordOffset(RingLine rl, double clock)
        {
            int n = rl.glyphs.Count; if (n == 0) return 0;
            float t = (float)clock;
            if (t <= rl.starts[0]) return 0;
            for (int i = 0; i < n - 1; i++)
            {
                if (t < rl.starts[i + 1])
                {
                    float span = Mathf.Max(.03f, rl.starts[i + 1] - rl.starts[i]), f = (t - rl.starts[i]) / span;
                    return Mathf.Lerp(rl.glyphs[i].nominal, rl.glyphs[i + 1].nominal, f);
                }
            }
            // After the last glyph starts, drift on for at most about one more character so the ring does not freeze.
            float tail = Mathf.Max(.2f, rl.glyphs[n - 1].end - rl.starts[n - 1]), after = Mathf.Min(1f, (t - rl.starts[n - 1]) / tail);
            return Mathf.Lerp(rl.glyphs[n - 1].nominal, rl.total, after);
        }

        void Rebuild(in StageContext c, float width, float height)
        {
            shownWidth = width; shownHeight = height; shownDocument = c.document; shownCentre = c.active; shownScale = c.fontScale; shownEstimate = c.estimate; shownCalm = c.calm; shownTuning = c.tuning;
            ring.Clear(); lines.Clear();
            var document = c.document; if (document == null || document.lines.Count == 0) return;
            float stageWidth = width < 120 ? 760f : width;
            // A little smaller than Folia's ring (44% of the width, up to 560 px): the pixel font is heavy and the translation sits below the ring.
            rx = Mathf.Min(stageWidth * .38f, 480f) * (c.tuning.intensity == 0 ? .9f : 1f); ry = rx * .707f;
            glyphSize = Mathf.Clamp(rx * .1f, 22f, 46f) * c.fontScale;
            // The ring's vertical reach is rx times sin(tilt); allow it a quarter more than the stage's half height (the far ends are small and faint).
            float areaHalf = Mathf.Max(260f, (height < 120 ? 460f : height) * .78f) * .5f;
            tiltDegrees = Mathf.Clamp(Mathf.Asin(Mathf.Clamp01((areaHalf * 1.25f - glyphSize) / rx)) * Mathf.Rad2Deg, 20f, 45f);
            int centre = Mathf.Max(-1, c.active);
            for (int index = centre - Behind; index <= centre + Ahead; index++)
            {
                if (index < 0 || index >= document.lines.Count) continue;
                var data = document.lines[index]; if (string.IsNullOrWhiteSpace(data.text)) continue;
                double nextStart = index + 1 < document.lines.Count ? document.lines[index + 1].start : double.PositiveInfinity;
                var glyphs = MusicLyricUnits.Timeline(data, nextStart, c.estimate, out _);
                if (glyphs.Count == 0) glyphs.Add(new LyricGlyph { text = data.text, start = data.start, end = double.IsInfinity(nextStart) ? data.start + 4 : nextStart });
                var rl = new RingLine { index = index, starts = new float[glyphs.Count] };
                float cursor = 0, firstCentre = 0;
                for (int i = 0; i < glyphs.Count; i++)
                {
                    string text = glyphs[i].text.Replace(' ', ' ');
                    // The focused character is magnified by up to 1.65, so characters sit about that far apart along the ring to stay clear of each other.
                    float advance = Advance(text, glyphSize) * SpacingBoost, centreX = cursor + advance * .5f;
                    if (i == 0) firstCentre = centreX;
                    var g = new RingGlyph { nominal = (centreX - firstCentre) / rx, start = (float)glyphs[i].start, end = (float)glyphs[i].end };
                    rl.starts[i] = g.start;
                    g.box = new VisualElement { pickingMode = PickingMode.Ignore }; g.box.AddToClassList("music-stage-claddagh-glyph");
                    g.box.style.height = glyphSize * 1.5f; g.box.style.top = -glyphSize * .75f;
                    g.label = new Label(text) { pickingMode = PickingMode.Ignore }; g.label.AddToClassList("music-stage-claddagh-char"); g.label.style.fontSize = glyphSize; StageFonts.Apply(g.label);
                    g.box.Add(g.label); ring.Add(g.box); rl.glyphs.Add(g);
                    cursor += advance;
                }
                rl.total = rl.glyphs.Count > 0 ? (cursor - firstCentre) / rx : 0;
                lines.Add(rl);
            }
        }
    }
}
