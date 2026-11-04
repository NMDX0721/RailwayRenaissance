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
        sealed class Leaving { public VisualElement element; public float age, duration, endScale; }

        readonly VisualElement root = new VisualElement(), lineHost = new VisualElement();
        readonly Label dots = new Label("· · ·");
        readonly List<StageWord> units = new List<StageWord>();
        readonly List<Leaving> leaving = new List<Leaving>();
        VisualElement line;
        MusicTimedDocument shownDocument; int shownLine = -2; float shownScale, shownWidth; bool shownEstimate, shownCalm; StageTuning shownTuning;
        StageReveal reveal; double lineEnd = double.PositiveInfinity, nextStart = double.PositiveInfinity;
        bool chorus, blank, spaced, legacy; float builtAt, fontSize = 40f, enterDuration, enterScale = 1, enterOpacity = 1, exitScale = 1.1f, exitDuration = .3f;
        float floatDistance = 14f, floatPeriod = 7f;

        public string Id => "luminous";
        public string Title => "Luminous";
        public bool OwnsSubtitles => false;
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

            foreach (var u in units) u.Tick(c.clock, dt, c.calm, reveal, lineEnd, chorus, tuning.rotation, chaotic, baseColor, activeColor, c.accent, c.high, lineFade);

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
            // A new line pushes in: the stage starts a little close and low, and settles over 0.7 s (a slow camera move on every line change).
            float push = c.calm ? 0f : 1f - Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - builtAt) / .7f));
            if (m <= 0) { lineHost.style.translate = new Translate(0, 12f * push); lineHost.style.scale = new Scale(new Vector3(1 + .04f * push, 1 + .04f * push, 1)); }
            else
            {
                float s = Mathf.Sin(now * Mathf.PI * 2f / floatPeriod);
                float y = (s > 0 ? -floatDistance * s : -floatDistance * .45f * s) * m - c.low * 4f * m + 12f * push;
                float sc = (1 + (s > 0 ? .01f * s : .005f * s) * m) * (1 + .04f * push);
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

        // The pixel font's Latin letters measure about 0.65 em, CJK characters 1 em.
        static float Advance(string text, float px)
        {
            float total = 0;
            foreach (char ch in text) total += (ch >= 0x2E80 ? 1f : ch == ' ' || ch == ' ' ? .4f : .65f) * px;
            return Mathf.Max(px * .35f, total);
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
            units.Clear(); chorus = false; blank = false; lineEnd = nextStart = double.PositiveInfinity;
            line = new VisualElement { pickingMode = PickingMode.Ignore }; line.AddToClassList("music-stage-line");
            line.style.width = Length.Percent(100); line.style.minHeight = 150;
            lineHost.Add(line);
            enterDuration = 0; enterScale = 1; enterOpacity = 1;
            if (document == null || active < 0 || active >= document.lines.Count) return;
            var data = document.lines[active];
            nextStart = active + 1 < document.lines.Count ? document.lines[active + 1].start : double.PositiveInfinity;
            lineEnd = !double.IsInfinity(data.end) && data.end > data.start ? data.end : nextStart;
            if (string.IsNullOrWhiteSpace(data.text)) { blank = true; return; }

            // A line sung more than once is treated as a chorus.
            string key = Normalize(data.text); int sung = 0;
            if (key.Length >= 4) foreach (var other in document.lines) if (Normalize(other.text) == key) sung++;
            chorus = sung >= 2;

            var glyphs = MusicLyricUnits.Timeline(data, nextStart, c.estimate, out _);
            if (glyphs.Count == 0) glyphs.Add(new LyricGlyph { text = data.text, start = data.start, end = double.IsInfinity(lineEnd) ? data.start + 4 : lineEnd });
            var grouped = legacy ? MusicLyricUnits.GroupLegacy(glyphs) : MusicLyricUnits.Group(glyphs);

            reveal = StageWord.RevealFor(grouped);
            if (reveal == StageReveal.Normal) { enterDuration = .3f; enterScale = .9f; enterOpacity = 0f; exitDuration = .3f; exitScale = 1.1f; }
            else if (reveal == StageReveal.Fast) { enterDuration = .16f; enterScale = .96f; enterOpacity = .35f; exitDuration = .16f; exitScale = 1.04f; }
            else { enterDuration = 0; exitDuration = .12f; exitScale = 1.02f; }

            bool chaotic = tuning.intensity == 2, still = tuning.intensity == 0;
            floatDistance = still ? 10f : chaotic ? 18f : 14f; floatPeriod = still ? 8.5f : chaotic ? 5.8f : 7f;
            int length = Mathf.Max(1, data.text.Length), seed = active * 7919 + length * 131;
            // Fit the line to the stage: one row for short lines, two for long ones. Magnified words need room, so the base size is smaller.
            float avail = Mathf.Max(240f, (width < 120 ? 760f : width) - 96f);
            float size = Mathf.Clamp(avail * (length <= 14 ? 1 : 2) / (length * 1.15f), 26f, 52f) * c.fontScale * (legacy ? 1f : .82f);
            size = Mathf.Min(size, avail / 3f);
            // A long line wraps into several rows and every row needs its own height; on a short stage the type is made smaller until the rows fit
            // (about 2.1 times the type size per row: the line, the room above and below it, and the magnification).
            float stageHeight = (root.parent ?? root).resolvedStyle.height; if (float.IsNaN(stageHeight) || stageHeight < 120) stageHeight = 420f;
            float allowed = stageHeight * (stageHeight >= 480f ? .72f : .62f);   // a short stage also has to keep the top row clear of the progress rail
            for (int pass = 0; pass < 4; pass++)
            {
                float lineWidth = 0;
                foreach (var unit in grouped) { string t = ""; foreach (var g in unit.glyphs) t += g.text; lineWidth += Advance(t, size) * 1.2f + size * .3f; }
                int rowCount = Mathf.Max(1, Mathf.CeilToInt(lineWidth / (avail * .95f)));
                float need = rowCount * size * (chaotic ? 2.5f : 2.1f);
                if (need <= allowed || size <= 18f) break;
                size = Mathf.Max(18f, size * Mathf.Max(.6f, allowed / need));
            }
            fontSize = size;

            // Each line gets its own alignment, picked from the seed so it does not reshuffle when the screen redraws. A short line on its own
            // row stays centred (a line pushed to one edge looks like a mistake next to the centred neighbours).
            bool shortLine = grouped.Count <= 3;
            Justify[] justify = still || shortLine ? new[] { Justify.Center } : new[] { Justify.FlexStart, Justify.Center, Justify.FlexEnd, Justify.SpaceAround, Justify.SpaceBetween };
            Align[] align = still || shortLine ? new[] { Align.Center } : new[] { Align.FlexStart, Align.Center, Align.FlexEnd };
            line.style.minHeight = shortLine ? Mathf.Max(96f, size * 2.4f) : 150f;
            line.style.justifyContent = justify[seed % justify.Length]; line.style.alignItems = align[(seed * 2) % align.Length];
            line.style.alignContent = Align.Center;
            line.style.paddingTop = size * .6f; line.style.paddingBottom = size * .6f;   // words scatter upward and downward by up to a row's margin
            // Folia scatters up to 60 px at the wildest setting; on a wrapped line that put whole words on top of each other, so it is held to 40.
            float spread = chaotic ? 28f : still ? 0f : 20f, tilt = chaotic ? 30f : still ? 0f : 5f;

            for (int i = 0; i < grouped.Count; i++)
            {
                // Folia's wildest setting varies a word's size from 0.8 to 1.4 times; held to 0.8 to 1.2 so a wrapped line stays readable.
                var word = StageWord.Create(grouped[i], size, c.accent, chorus, seed + i, spread, tilt, chaotic ? .8f : 1.1f, chaotic ? .4f : .2f, tuning.rotation, c.calm, chaotic);
                // Wrapped rows need room above and below: words are magnified and scattered up and down, and without it one row lands on the next.
                float rowMargin = size * (chaotic ? .6f : .34f); word.body.style.marginTop = rowMargin; word.body.style.marginBottom = rowMargin;
                // Letters sit flush now, so words are narrower than before: they need their own gap, or a tilted, magnified word lands on its neighbour.
                float sideMargin = size * (chaotic ? .3f : .12f); word.body.style.marginLeft = sideMargin; word.body.style.marginRight = sideMargin;
                line.Add(word.body); units.Add(word);
            }
        }
    }
}
