using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage style "Luminous", after Folia's classic style. Only the current line is on stage. Its characters are grouped into display
    /// units ("手を", "unbelievable"); each unit waits small and invisible, springs forward shortly before its time, and settles once sung,
    /// while the characters inside light up one by one on a glow layer behind them. Every line gets its own deterministic layout
    /// (alignment, scatter, tilt); the gap between words is measured so a magnified word never lands on its neighbour. Lines arrive and
    /// leave with a short scale/fade chosen by how fast the line is sung. Every pose is a function of the audio clock, so pausing
    /// freezes it and seeking rebuilds it. Independent implementation of the behaviour; no Folia source is used.
    /// </summary>
    public sealed class MusicStageLuminous : IStageStyle
    {
        sealed class StageUnit
        {
            public VisualElement body, ripple;
            public Label glow;
            public readonly List<Label> labels = new List<Label>();
            public readonly List<float> tones = new List<float>();
            public LyricUnit unit;
            public float baseScale, cx, cy, rot, passedRot, rippleScale;
            public float x, y, s = .5f, r, o, vx, vy, vs, vr, rippleTime = 1, glowTone;
            public int state = -1;
        }

        sealed class Leaving { public VisualElement element; public float age, duration, endScale; }

        enum Reveal { Normal, Fast, Instant }

        readonly VisualElement root = new VisualElement(), lineHost = new VisualElement();
        readonly Label dots = new Label("· · ·");
        readonly List<StageUnit> units = new List<StageUnit>();
        readonly List<Leaving> leaving = new List<Leaving>();
        VisualElement line;
        MusicTimedDocument shownDocument; int shownLine = -2; float shownScale, shownWidth; bool shownEstimate, shownCalm; StageTuning shownTuning;
        Reveal reveal; double lineStart, lineEnd = double.PositiveInfinity, nextStart = double.PositiveInfinity;
        bool chorus, blank, spaced, built, legacy; float builtAt, fontSize = 40f, enterDuration, enterScale = 1, enterOpacity = 1, exitScale = 1.1f, exitDuration = .3f;
        float floatDistance = 14f, floatPeriod = 7f;

        public string Id => "luminous";
        public string Title => "Luminous";
        public VisualElement Root => root;

        public MusicStageLuminous()
        {
            root.AddToClassList("music-stage-luminous"); root.pickingMode = PickingMode.Ignore;
            dots.AddToClassList("music-stage-dots"); dots.pickingMode = PickingMode.Ignore;
            lineHost.AddToClassList("music-stage-line-host"); lineHost.pickingMode = PickingMode.Ignore;
            root.Add(dots); root.Add(lineHost);
        }

        public void Invalidate() { shownDocument = null; shownLine = -3; }

        public void Tick(in StageContext c)
        {
            float width = float.IsNaN(root.resolvedStyle.width) ? 0 : root.resolvedStyle.width;
            if (!ReferenceEquals(c.document, shownDocument) || c.active != shownLine || Mathf.Abs(c.fontScale - shownScale) > .01f ||
                Mathf.Abs(width - shownWidth) > 24f || c.estimate != shownEstimate || c.calm != shownCalm || !c.tuning.Equals(shownTuning))
                Rebuild(c, width);

            float dt = Mathf.Clamp(c.dt, 0, 1f / 20f), now = Time.realtimeSinceStartup;
            var tuning = c.tuning; bool chaotic = tuning.intensity == 2;
            Color baseColor = new Color(.93f, .9f, .8f), activeColor = Color.Lerp(Color.white, c.accent, .35f);
            if (!spaced) MeasureSpacing(tuning);

            // Between lines: a long silence shows breathing dots instead of a finished line (Folia's interlude).
            bool finished = !double.IsInfinity(lineEnd) && c.clock > lineEnd + 2.5 && nextStart - c.clock > 3;
            bool interlude = blank || finished || shownLine < 0;
            dots.style.display = interlude ? DisplayStyle.Flex : DisplayStyle.None;
            dots.style.opacity = c.calm ? .5f : .35f + .3f * (.5f + .5f * Mathf.Sin(now * 2.6f));
            float lineFade = finished ? 0f : 1f;

            foreach (var u in units)
            {
                double lookahead = reveal == Reveal.Instant ? .03 : reveal == Reveal.Fast ? .08 : .15;
                double end = reveal == Reveal.Instant ? (double.IsInfinity(lineEnd) ? u.unit.end : lineEnd)
                    : reveal == Reveal.Fast ? Math.Min(double.IsInfinity(lineEnd) ? u.unit.end : lineEnd, Math.Max(u.unit.end, u.unit.start + .12)) : u.unit.end;
                int state = c.clock > end ? 2 : c.clock >= u.unit.start - lookahead ? 1 : 0;
                if (state == 1 && u.state != 1 && chorus && !c.calm) u.rippleTime = 0;
                u.state = state;

                if (c.calm)
                {
                    u.o = state == 0 ? .3f : 1f; u.s = 1; u.x = u.y = u.r = 0;
                }
                else
                {
                    // Waiting is small, faded and displaced; active springs large; passed settles back with a little afterglow.
                    float tx = state == 0 ? u.cx + Mathf.Sin(u.cy) * 100f : u.cx, ty = state == 0 ? u.cy + Mathf.Cos(u.cx) * 50f : u.cy;
                    float ts = state == 0 ? .5f : state == 1 ? u.baseScale * 1.4f : u.baseScale;
                    float tr = state == 0 ? u.rot + (tuning.rotation ? 20f : 0) : state == 1 ? u.rot : u.rot + u.passedRot;
                    // Active uses the spring Folia gives it (stiffness 200, damping 20); entering and leaving settle in about half a second.
                    float stiffness = state == 1 ? (reveal == Reveal.Normal ? 200f : 300f) : 110f, damping = state == 1 ? (reveal == Reveal.Normal ? 20f : 26f) : 21f;
                    Spring(ref u.s, ref u.vs, ts, dt, stiffness, damping); Spring(ref u.x, ref u.vx, tx, dt, stiffness, damping);
                    Spring(ref u.y, ref u.vy, ty, dt, stiffness, damping); Spring(ref u.r, ref u.vr, tr, dt, stiffness, damping);
                    float targetOpacity = state == 0 ? 0f : state == 1 ? 1f : chaotic ? .9f : .82f;
                    u.o = Mathf.Lerp(u.o, targetOpacity, 1 - Mathf.Exp(-dt * (state == 1 ? 24f : 6f)));
                }
                u.body.style.opacity = u.o * lineFade;
                u.body.style.scale = new Scale(new Vector3(u.s, u.s, 1));
                u.body.style.translate = new Translate(u.x, u.y);
                u.body.style.rotate = new Rotate(new Angle(u.r, AngleUnit.Degree));

                // Inside an active unit the characters light up one by one on their own times (Folia's grapheme timing).
                float lit = 0;
                for (int g = 0; g < u.labels.Count; g++)
                {
                    float target = state == 1 ? (c.clock >= u.unit.glyphs[g].start ? 1f : .25f) : 0f;
                    float rate = state == 1 ? 20f : state == 2 ? 2.5f : 8f;
                    u.tones[g] = c.calm ? (state == 1 ? 1f : 0f) : Mathf.Lerp(u.tones[g], target, 1 - Mathf.Exp(-dt * rate));
                    float tone = u.tones[g]; lit = Mathf.Max(lit, state == 1 && c.clock >= u.unit.glyphs[g].start ? 1f : 0f);
                    u.labels[g].style.color = Color.Lerp(baseColor, activeColor, tone);
                }
                // The glow sits on its own layer behind the text, so it can be pushed hard without making the glyphs unreadable.
                if (u.glow != null)
                {
                    u.glowTone = c.calm ? 0 : Mathf.Lerp(u.glowTone, lit, 1 - Mathf.Exp(-dt * (lit > u.glowTone ? 16f : 3f)));
                    float blur = 12f + 14f * Mathf.Clamp01(c.high);
                    u.glow.style.opacity = u.glowTone * lineFade;
                    u.glow.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = blur, color = new Color(c.accent.r, c.accent.g, c.accent.b, .8f) };
                }

                if (u.ripple != null)
                {
                    if (u.rippleTime < .5f)
                    {
                        u.rippleTime += dt; float t = Mathf.Clamp01(u.rippleTime / .5f), size = Mathf.Max(u.body.resolvedStyle.width, u.body.resolvedStyle.height) * 1.5f;
                        if (!float.IsNaN(size) && size > 0) { u.ripple.style.width = size; u.ripple.style.height = size; u.ripple.style.marginLeft = -size / 2; u.ripple.style.marginTop = -size / 2; }
                        float scale = Mathf.Lerp(.2f, u.rippleScale, 1 - (1 - t) * (1 - t));
                        u.ripple.style.scale = new Scale(new Vector3(scale, scale, 1)); u.ripple.style.opacity = .8f * (1 - t);
                        u.ripple.style.borderTopColor = u.ripple.style.borderBottomColor = u.ripple.style.borderLeftColor = u.ripple.style.borderRightColor = activeColor;
                    }
                    else u.ripple.style.opacity = 0;
                }
            }

            // The line arrives with a short scale/fade chosen by how fast it is sung (Folia's line transition: normal / fast / none).
            float intro = c.calm || enterDuration <= 0 ? 1f : Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - builtAt) / enterDuration));
            if (line != null)
            {
                line.style.opacity = Mathf.Lerp(enterOpacity, 1f, intro) * lineFade;
                float introScale = Mathf.Lerp(enterScale, 1f, intro);
                line.style.scale = new Scale(new Vector3(introScale, introScale, 1));
            }
            // Leaving lines grow slightly and fade out underneath the new one.
            for (int i = leaving.Count - 1; i >= 0; i--)
            {
                var l = leaving[i]; l.age += dt;
                float t = Mathf.Clamp01(l.age / l.duration), ease = 1 - (1 - t) * (1 - t);
                l.element.style.opacity = 1 - ease;
                float scale = Mathf.Lerp(1f, l.endScale, ease); l.element.style.scale = new Scale(new Vector3(scale, scale, 1));
                if (t >= 1) { l.element.RemoveFromHierarchy(); leaving.RemoveAt(i); }
            }

            // Whole-line float (Folia's breathing): distance and period by intensity, scaled by the tuning; low frequencies add a little push.
            float m = c.calm ? 0 : tuning.breathing;
            if (m <= 0) { lineHost.style.translate = new Translate(0, 0); lineHost.style.scale = new Scale(Vector3.one); }
            else
            {
                float s = Mathf.Sin(now * Mathf.PI * 2f / floatPeriod);
                float y = (s > 0 ? -floatDistance * s : -floatDistance * .45f * s) * m - c.low * 4f * m;
                float sc = 1 + (s > 0 ? .01f * s : .005f * s) * m;
                lineHost.style.translate = new Translate(0, y); lineHost.style.scale = new Scale(new Vector3(sc, sc, 1));
            }
        }

        // The gap after each word is what keeps a magnified word off its neighbour: half the overflow of both words, the scatter between them
        // and a small gap, times the spacing tunable (Folia's classic margin). Needs real widths, so it runs once layout exists.
        void MeasureSpacing(StageTuning tuning)
        {
            if (units.Count == 0) { spaced = true; return; }
            foreach (var u in units) { float w = u.body.resolvedStyle.width; if (float.IsNaN(w) || w <= 0) return; }
            bool chaotic = tuning.intensity == 2;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i]; float wi = u.body.resolvedStyle.width, si = u.baseScale * 1.4f;
                float margin;
                if (legacy) margin = fontSize * .25f * tuning.spacing;
                else
                {
                    float wn = 0, sn = 1, xn = 0;
                    if (i + 1 < units.Count) { var n = units[i + 1]; wn = n.body.resolvedStyle.width; sn = n.baseScale * 1.4f; xn = n.cx; }
                    float calculated = (wi * (si - 1) / 2 + wn * (sn - 1) / 2 + (u.cx - xn) + .05f * fontSize) * tuning.spacing;
                    float min = (chaotic ? .08f : .12f) * fontSize * tuning.spacing;
                    margin = Mathf.Max(min, calculated);
                }
                u.body.style.marginRight = margin;
            }
            spaced = true;
        }

        static void Spring(ref float value, ref float velocity, float target, float dt, float stiffness, float damping)
        {
            if (dt <= 0) return;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / .008f)); float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                velocity += (stiffness * (target - value) - damping * velocity) * h;
                value += velocity * h;
            }
        }

        static float Random(int seed, float offset)
        {
            float x = Mathf.Sin(seed * 12.9898f + offset * 78.233f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }

        static string Normalize(string text)
        {
            var letters = new System.Text.StringBuilder();
            foreach (char c in text ?? "") if (char.IsLetterOrDigit(c)) letters.Append(char.ToLowerInvariant(c));
            return letters.ToString();
        }

        void Rebuild(in StageContext c, float width)
        {
            var document = c.document; int active = c.active; var tuning = c.tuning;
            shownWidth = width; shownDocument = document; shownLine = active; shownScale = c.fontScale; shownEstimate = c.estimate; shownCalm = c.calm; shownTuning = tuning;
            builtAt = Time.realtimeSinceStartup; spaced = false; legacy = tuning.legacyLayout;
            // The line that was on stage leaves underneath the new one instead of vanishing.
            if (line != null && units.Count > 0 && !c.calm && leaving.Count < 3)
            {
                var old = line; old.style.position = Position.Absolute; old.style.left = 0; old.style.right = 0; old.style.top = 0;
                leaving.Add(new Leaving { element = old, duration = exitDuration, endScale = exitScale });
            }
            else line?.RemoveFromHierarchy();
            units.Clear(); chorus = false; blank = false; lineStart = 0; lineEnd = nextStart = double.PositiveInfinity;
            line = new VisualElement { pickingMode = PickingMode.Ignore }; line.AddToClassList("music-stage-line");
            line.style.width = Length.Percent(100); line.style.minHeight = 150;
            lineHost.Add(line);
            built = true;
            enterDuration = 0; enterScale = 1; enterOpacity = 1;
            if (document == null || active < 0 || active >= document.lines.Count) return;
            var data = document.lines[active];
            nextStart = active + 1 < document.lines.Count ? document.lines[active + 1].start : double.PositiveInfinity;
            lineStart = data.start; lineEnd = !double.IsInfinity(data.end) && data.end > data.start ? data.end : nextStart;
            if (string.IsNullOrWhiteSpace(data.text)) { blank = true; return; }

            // A line sung more than once is treated as a chorus.
            string key = Normalize(data.text); int sung = 0;
            if (key.Length >= 4) foreach (var other in document.lines) if (Normalize(other.text) == key) sung++;
            chorus = sung >= 2;

            var glyphs = MusicLyricUnits.Timeline(data, nextStart, c.estimate, out _);
            if (glyphs.Count == 0) glyphs.Add(new LyricGlyph { text = data.text, start = data.start, end = double.IsInfinity(lineEnd) ? data.start + 4 : lineEnd });
            var grouped = legacy ? MusicLyricUnits.GroupLegacy(glyphs) : MusicLyricUnits.Group(glyphs);

            // Fast lines do not wait for every word: the more words per second, the sooner each one is revealed.
            double span = Math.Max(.1, grouped[grouped.Count - 1].end - grouped[0].start), perUnit = span / grouped.Count;
            reveal = perUnit < .10 ? Reveal.Instant : perUnit < .18 ? Reveal.Fast : Reveal.Normal;
            if (reveal == Reveal.Normal) { enterDuration = .3f; enterScale = .9f; enterOpacity = 0f; exitDuration = .3f; exitScale = 1.1f; }
            else if (reveal == Reveal.Fast) { enterDuration = .16f; enterScale = .96f; enterOpacity = .35f; exitDuration = .16f; exitScale = 1.04f; }
            else { enterDuration = 0; exitDuration = .12f; exitScale = 1.02f; }

            bool chaotic = tuning.intensity == 2, still = tuning.intensity == 0;
            floatDistance = still ? 10f : chaotic ? 18f : 14f; floatPeriod = still ? 8.5f : chaotic ? 5.8f : 7f;
            int length = Mathf.Max(1, data.text.Length), seed = active * 7919 + length * 131;
            // Fit the line to the stage: one row for short lines, two for long ones. Magnified words need room, so the base size is smaller.
            float avail = Mathf.Max(240f, (width < 120 ? 760f : width) - 96f);
            float size = Mathf.Clamp(avail * (length <= 14 ? 1 : 2) / (length * 1.15f), 26f, 52f) * c.fontScale * (legacy ? 1f : .82f);
            fontSize = size = Mathf.Min(size, avail / 3f);

            // Each line gets its own alignment, picked from the seed so it does not reshuffle when the screen redraws.
            Justify[] justify = still ? new[] { Justify.Center } : new[] { Justify.FlexStart, Justify.Center, Justify.FlexEnd, Justify.SpaceAround, Justify.SpaceBetween };
            Align[] align = still ? new[] { Align.Center } : new[] { Align.FlexStart, Align.Center, Align.FlexEnd };
            line.style.justifyContent = justify[seed % justify.Length]; line.style.alignItems = align[(seed * 2) % align.Length];
            line.style.alignContent = Align.Center;
            float spread = chaotic ? 60f : still ? 0f : 20f, tilt = chaotic ? 30f : still ? 0f : 5f;

            for (int i = 0; i < grouped.Count; i++)
            {
                var unit = grouped[i];
                var stage = new StageUnit { unit = unit };
                stage.baseScale = chaotic ? .8f + Random(seed + i, 4) * .6f : 1.1f + Random(seed + i, 4) * .2f;
                stage.cx = (Random(seed + i, 1) - .5f) * spread * 2f; stage.cy = (Random(seed + i, 2) - .5f) * spread * 2f;
                stage.rot = tuning.rotation ? (Random(seed + i, 3) - .5f) * tilt * 2f : 0; stage.passedRot = tuning.rotation ? (Random(seed + i, 8) - .5f) * 45f : 0;
                if (c.calm) { stage.baseScale = 1; stage.cx = stage.cy = stage.rot = stage.passedRot = 0; }
                stage.rippleScale = 1.5f + Random(seed + i, 9) * 2f;
                stage.body = new VisualElement { pickingMode = PickingMode.Ignore }; stage.body.AddToClassList("music-stage-word");
                stage.body.style.marginRight = Mathf.Max(4f, size * .2f);
                if (chaotic && Random(seed + i, 6) > .7f) stage.body.style.alignSelf = Random(seed + i, 7) > .5f ? Align.FlexStart : Align.FlexEnd;
                // Glow layer first so it sits behind the text.
                string text = ""; foreach (var glyph in unit.glyphs) text += glyph.text;
                stage.glow = new Label(text) { pickingMode = PickingMode.Ignore }; stage.glow.AddToClassList("music-stage-word-text"); stage.glow.AddToClassList("music-stage-glow");
                stage.glow.style.fontSize = size; stage.glow.style.color = new Color(c.accent.r, c.accent.g, c.accent.b, .9f); stage.glow.style.opacity = 0;
                stage.body.Add(stage.glow);
                if (chorus)
                {
                    stage.ripple = new VisualElement { pickingMode = PickingMode.Ignore }; stage.ripple.AddToClassList("music-stage-ripple");
                    stage.ripple.style.opacity = 0; stage.body.Add(stage.ripple);
                }
                foreach (var glyph in unit.glyphs)
                {
                    var label = new Label(glyph.text) { pickingMode = PickingMode.Ignore }; label.AddToClassList("music-stage-word-text");
                    label.style.fontSize = size; stage.body.Add(label); stage.labels.Add(label); stage.tones.Add(0);
                }
                line.Add(stage.body);
                // Start in the waiting pose so the first frame does not flash.
                stage.s = .5f; stage.o = 0; stage.r = stage.rot + (tuning.rotation ? 20f : 0);
                stage.x = stage.cx + Mathf.Sin(stage.cy) * 100f; stage.y = stage.cy + Mathf.Cos(stage.cx) * 50f;
                units.Add(stage);
            }
        }
    }
}
