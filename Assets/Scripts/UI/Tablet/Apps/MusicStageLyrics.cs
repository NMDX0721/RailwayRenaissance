using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage mode, after Folia's classic visualizer. Only the current line is on stage. Its characters are grouped into display
    /// units ("手を", "unbelievable"); each unit waits small and faded, pops forward on a spring shortly before its time, and settles
    /// back once sung, while the characters inside light up one by one. Fast lines reveal sooner, repeated lines (choruses) ripple,
    /// gaps show breathing dots. Every pose is a function of the audio clock, so pausing freezes it and seeking rebuilds it.
    /// </summary>
    public sealed class MusicStageLyrics : VisualElement
    {
        sealed class StageUnit
        {
            public VisualElement body, ripple;
            public readonly List<Label> labels = new List<Label>();
            public readonly List<float> tones = new List<float>();
            public LyricUnit unit;
            public float baseScale, cx, cy, rot, passedRot, rippleScale;
            public float x, y, s = .5f, r, o, vx, vy, vs, vr, rippleTime = 1;
            public int state = -1;
        }

        enum Reveal { Normal, Fast, Instant }

        readonly Label previous = new Label(), upcoming = new Label(), translation = new Label(), dots = new Label("· · ·");
        readonly VisualElement line = new VisualElement();
        readonly List<StageUnit> units = new List<StageUnit>();
        MusicTimedDocument shownDocument; int shownLine = -2; float shownScale, shownWidth; bool shownEstimate, shownTranslation, shownCalm;
        Reveal reveal; double lineStart, lineEnd = double.PositiveInfinity, nextStart = double.PositiveInfinity;
        bool chorus, blank; float builtAt; float fontSize = 40f;

        public MusicStageLyrics()
        {
            AddToClassList("music-stage-lyrics");
            previous.AddToClassList("music-stage-previous"); upcoming.AddToClassList("music-stage-upcoming");
            translation.AddToClassList("music-stage-translation"); line.AddToClassList("music-stage-line"); dots.AddToClassList("music-stage-dots");
            Add(previous); Add(dots); Add(line); Add(translation); Add(upcoming);
            foreach (var label in new[] { previous, upcoming, translation, dots }) label.pickingMode = PickingMode.Ignore;
            line.pickingMode = PickingMode.Ignore;
        }

        public void Tick(MusicTimedDocument document, int active, double clock, float dt, bool calm, bool estimate, float fontScale,
            Color accent, float energy, bool showTranslation)
        {
            if (!ReferenceEquals(document, shownDocument) || active != shownLine || Mathf.Abs(fontScale - shownScale) > .01f ||
                Mathf.Abs(resolvedStyle.width - shownWidth) > 24f || estimate != shownEstimate || showTranslation != shownTranslation || calm != shownCalm)
                Rebuild(document, active, estimate, fontScale, showTranslation, calm);

            dt = Mathf.Clamp(dt, 0, 1f / 20f);
            float now = Time.realtimeSinceStartup;
            Color baseColor = new Color(.93f, .9f, .8f), activeColor = Color.Lerp(Color.white, accent, .35f);

            // Between lines: a long silence shows breathing dots instead of a finished line (Folia's interlude).
            bool finished = !double.IsInfinity(lineEnd) && clock > lineEnd + 2.5 && nextStart - clock > 3;
            bool interlude = blank || finished || shownLine < 0;
            dots.style.display = interlude ? DisplayStyle.Flex : DisplayStyle.None;
            dots.style.opacity = calm ? .5f : .35f + .3f * (.5f + .5f * Mathf.Sin(now * 2.6f));
            float intro = calm ? 1f : Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - builtAt) / .3f));
            float lineFade = finished ? 0f : 1f;

            foreach (var u in units)
            {
                double lookahead = reveal == Reveal.Instant ? .03 : reveal == Reveal.Fast ? .08 : .15;
                double end = reveal == Reveal.Instant ? (double.IsInfinity(lineEnd) ? u.unit.end : lineEnd)
                    : reveal == Reveal.Fast ? Math.Min(double.IsInfinity(lineEnd) ? u.unit.end : lineEnd, Math.Max(u.unit.end, u.unit.start + .12)) : u.unit.end;
                int state = clock > end ? 2 : clock >= u.unit.start - lookahead ? 1 : 0;
                if (state == 1 && u.state != 1 && chorus && !calm) u.rippleTime = 0;
                u.state = state;

                if (calm)
                {
                    u.o = state == 0 ? .3f : 1f; u.s = 1; u.x = u.y = u.r = 0;
                }
                else
                {
                    // The same targets Folia gives its three states: waiting is small and displaced, active pops large, passed settles.
                    float tx = state == 0 ? u.cx + Mathf.Sin(u.cy) * 28f : u.cx, ty = state == 0 ? u.cy + Mathf.Cos(u.cx) * 14f : u.cy;
                    float ts = state == 0 ? .5f * u.baseScale : state == 1 ? 1.12f * u.baseScale : u.baseScale;
                    float tr = state == 0 ? u.rot + 20f : state == 1 ? u.rot : u.rot + u.passedRot;
                    float stiffness = reveal == Reveal.Normal ? 220f : 320f, damping = reveal == Reveal.Normal ? 27f : 32f;
                    Spring(ref u.s, ref u.vs, ts, dt, stiffness, damping); Spring(ref u.x, ref u.vx, tx, dt, stiffness, damping);
                    Spring(ref u.y, ref u.vy, ty, dt, stiffness, damping); Spring(ref u.r, ref u.vr, tr, dt, stiffness, damping);
                    float targetOpacity = state == 0 ? 0f : state == 1 ? 1f : .82f;
                    u.o = Mathf.Lerp(u.o, targetOpacity, 1 - Mathf.Exp(-dt * (state == 1 ? 18f : 6f)));
                }
                u.body.style.opacity = u.o * lineFade;
                u.body.style.scale = new Scale(new Vector3(u.s, u.s, 1));
                u.body.style.translate = new Translate(u.x, u.y);
                u.body.style.rotate = new Rotate(new Angle(u.r, AngleUnit.Degree));

                // Inside an active unit the characters light up one by one on their own times (Folia's grapheme timing).
                for (int g = 0; g < u.labels.Count; g++)
                {
                    float target = state == 1 ? (clock >= u.unit.glyphs[g].start ? 1f : .25f) : state == 2 ? 0f : 0f;
                    float rate = state == 1 ? 20f : state == 2 ? 2.5f : 8f;
                    u.tones[g] = calm ? (state == 1 ? 1f : 0f) : Mathf.Lerp(u.tones[g], target, 1 - Mathf.Exp(-dt * rate));
                    float tone = u.tones[g];
                    u.labels[g].style.color = Color.Lerp(baseColor, activeColor, tone);
                    u.labels[g].style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = 12f * tone, color = new Color(accent.r, accent.g, accent.b, .6f * tone) };
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
            // The whole line comes in with a short scale/fade, and breathes a little more when the music is louder.
            line.style.opacity = intro * lineFade;
            float introScale = .92f + .08f * intro;
            float breathe = calm ? 0 : Mathf.Sin(now * .9f) * (2f + 5f * Mathf.Clamp01(energy));
            line.style.scale = new Scale(new Vector3(introScale, introScale, 1));
            line.style.translate = new Translate(0, breathe);
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

        void Rebuild(MusicTimedDocument document, int active, bool estimate, float fontScale, bool showTranslation, bool calm)
        {
            shownWidth = float.IsNaN(resolvedStyle.width) ? 0 : resolvedStyle.width;
            shownDocument = document; shownLine = active; shownScale = fontScale; shownEstimate = estimate; shownTranslation = showTranslation; shownCalm = calm;
            builtAt = Time.realtimeSinceStartup;
            line.Clear(); units.Clear(); chorus = false; blank = false; lineStart = 0; lineEnd = nextStart = double.PositiveInfinity;
            string Plain(int index) => document != null && index >= 0 && index < document.lines.Count ? document.lines[index].text : "";
            previous.text = Plain(active - 1); upcoming.text = Plain(active < 0 ? 0 : active + 1);
            translation.text = ""; translation.style.display = DisplayStyle.None;
            if (document == null || active < 0 || active >= document.lines.Count) return;
            var data = document.lines[active];
            nextStart = active + 1 < document.lines.Count ? document.lines[active + 1].start : double.PositiveInfinity;
            lineStart = data.start; lineEnd = !double.IsInfinity(data.end) && data.end > data.start ? data.end : nextStart;
            if (string.IsNullOrWhiteSpace(data.text)) { blank = true; return; }

            // A line sung more than once is treated as a chorus.
            string key = Normalize(data.text); int sung = 0;
            if (key.Length >= 4) foreach (var other in document.lines) if (Normalize(other.text) == key) sung++;
            chorus = sung >= 2;

            var glyphs = MusicLyricUnits.Timeline(data, nextStart, estimate, out _);
            if (glyphs.Count == 0) glyphs.Add(new LyricGlyph { text = data.text, start = data.start, end = double.IsInfinity(lineEnd) ? data.start + 4 : lineEnd });
            var grouped = MusicLyricUnits.Group(glyphs);

            // Fast lines do not wait for every word: the more words per second, the sooner each one is revealed.
            double span = Math.Max(.1, grouped[grouped.Count - 1].end - grouped[0].start), perUnit = span / grouped.Count;
            reveal = perUnit < .10 ? Reveal.Instant : perUnit < .18 ? Reveal.Fast : Reveal.Normal;

            int length = Mathf.Max(1, data.text.Length), seed = active * 7919 + length * 131;
            // Fit the line to the stage: one row for short lines, two for long ones, never smaller than readable.
            float width = float.IsNaN(resolvedStyle.width) || resolvedStyle.width < 120 ? 760f : resolvedStyle.width;
            float avail = Mathf.Max(240f, width - 96f);
            float size = Mathf.Clamp(avail * (length <= 14 ? 1 : 2) / (length * 1.15f), 26f, 52f) * fontScale;
            fontSize = size = Mathf.Min(size, avail / 3f);

            for (int i = 0; i < grouped.Count; i++)
            {
                var unit = grouped[i];
                var stage = new StageUnit { unit = unit, baseScale = 1f + Random(seed + i, 4) * .05f };
                stage.cx = (Random(seed + i, 1) - .5f) * 10f; stage.cy = (Random(seed + i, 2) - .5f) * 8f;
                stage.rot = calm ? 0 : (Random(seed + i, 3) - .5f) * 6f; stage.passedRot = calm ? 0 : (Random(seed + i, 8) - .5f) * 10f;
                stage.rippleScale = 1.5f + Random(seed + i, 9) * 2f;
                stage.body = new VisualElement { pickingMode = PickingMode.Ignore }; stage.body.AddToClassList("music-stage-word");
                stage.body.style.marginRight = Mathf.Max(4f, size * .12f);
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
                stage.s = .5f * stage.baseScale; stage.o = 0; stage.r = stage.rot + 20f;
                stage.x = stage.cx + Mathf.Sin(stage.cy) * 28f; stage.y = stage.cy + Mathf.Cos(stage.cx) * 14f;
                units.Add(stage);
            }
            if (showTranslation && !string.IsNullOrEmpty(data.translation)) { translation.text = data.translation; translation.style.display = DisplayStyle.Flex; }
        }
    }
}
