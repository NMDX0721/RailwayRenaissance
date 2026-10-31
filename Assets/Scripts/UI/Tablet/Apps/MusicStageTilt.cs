using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage style "Tilt", after Folia's typographic style. The current line is split into one to four rows (longer lines more often),
    /// each row set in plain horizontal type, except that one row may be picked at random and set in large light italic in the accent colour,
    /// its characters alternately lifted and dropped. Rows come in one after another when their words are about to be sung, their
    /// characters fading in a few hundredths of a second apart; every character swells a little while it is sung and keeps a faint afterglow.
    /// The split, the italic row and every pose come from the line and the audio clock, so pausing freezes it and seeking rebuilds it.
    /// Independent implementation of the behaviour.
    /// </summary>
    public sealed class MusicStageTilt : IStageStyle
    {
        // Folia's defaults: how eagerly a line is split, and how often one row is set in italic.
        const float SplitProbability = .75f, TiltProbability = .35f;

        /// <summary>Review seam: -1 leaves the seeded pick alone; 0 or more forces that row (clamped to the last row) to be the italic one.</summary>
        public static int TiltRowOverride = -1;

        sealed class Segment
        {
            public VisualElement row;
            public readonly List<Label> labels = new List<Label>();
            public readonly List<int> order = new List<int>();          // reveal order of each label (spaces share the previous one)
            public readonly List<bool> space = new List<bool>();
            public readonly List<float> start = new List<float>(), end = new List<float>();
            public double revealAt;
            public bool tilt;
            public float lift;
        }

        sealed class Leaving { public VisualElement element; public float age, duration, endScale; }

        readonly VisualElement root = new VisualElement(), lineHost = new VisualElement();
        readonly Label dots = new Label("· · ·");
        readonly List<Segment> segments = new List<Segment>();
        readonly List<Leaving> leaving = new List<Leaving>();
        VisualElement column;
        MusicTimedDocument shownDocument; int shownLine = -2; float shownScale, shownWidth; bool shownEstimate, shownCalm; StageTuning shownTuning;
        double lineEnd = double.PositiveInfinity, nextStart = double.PositiveInfinity;
        bool blank; float builtAt, enterDuration = .3f, enterScale = .92f, exitScale = 1.06f, exitDuration = .3f;

        public string Id => "tilt";
        public string Title => "Tilt";
        public bool OwnsSubtitles => false;
        public VisualElement Root => root;

        public MusicStageTilt()
        {
            root.AddToClassList("music-stage-luminous"); root.pickingMode = PickingMode.Ignore;
            dots.AddToClassList("music-stage-dots"); dots.pickingMode = PickingMode.Ignore;
            lineHost.AddToClassList("music-stage-line-host"); lineHost.pickingMode = PickingMode.Ignore;
            root.Add(dots); root.Add(lineHost);
        }

        public void Invalidate() { shownDocument = null; shownLine = -3; }

        // How many rows a line is cut into: the log of its length against a 20-character reference, times a seeded jitter and the split tunable.
        static int LineCount(int chars, int seed)
        {
            float normalized = Mathf.Log(chars + 4f) / Mathf.Log(24f);
            float jitter = StageWord.Random(seed, 1) * .6f + .7f;
            float score = normalized * jitter * SplitProbability;
            return score < .45f ? 1 : score < 1.05f ? 2 : score < 1.7f ? 3 : 4;
        }

        // Cut the units into n rows of about equal length, preferring to break after punctuation.
        static List<List<LyricUnit>> Split(List<LyricUnit> units, int rows)
        {
            rows = Mathf.Clamp(rows, 1, units.Count);
            var cum = new List<int>(); int total = 0;
            foreach (var unit in units) { total += unit.glyphs.Count; cum.Add(total); }
            var cuts = new List<int>(); int previousCut = 0;
            for (int k = 1; k < rows; k++)
            {
                float ideal = total * (float)k / rows; int best = -1; float bestScore = float.MaxValue;
                for (int i = previousCut; i < units.Count - (rows - k); i++)
                {
                    char last = units[i].glyphs[units[i].glyphs.Count - 1].text.TrimEnd().Length > 0 ? units[i].glyphs[units[i].glyphs.Count - 1].text.TrimEnd()[0] : ' ';
                    float score = Mathf.Abs(cum[i] - ideal) - (char.IsPunctuation(last) ? 2f : 0f);
                    if (score < bestScore) { bestScore = score; best = i; }
                }
                if (best < previousCut) best = previousCut;
                cuts.Add(best + 1); previousCut = best + 1;
            }
            var result = new List<List<LyricUnit>>(); int from = 0;
            foreach (int cut in cuts) { if (cut > from) { result.Add(units.GetRange(from, cut - from)); from = cut; } }
            if (from < units.Count) result.Add(units.GetRange(from, units.Count - from));
            return result;
        }

        // A character swells while it is sung (a sine over its duration, 0.2 to 0.9 s) and keeps a quarter of that as a fading afterglow.
        static float Pulse(double clock, float start, float end)
        {
            float duration = Mathf.Clamp(Mathf.Max(end - start, .05f), .2f, .9f), elapsed = (float)(clock - start);
            if (elapsed < 0) return 0;
            if (elapsed <= duration) return Mathf.Sin(elapsed / duration * Mathf.PI);
            float after = elapsed - duration, ramp = duration * 1.2f;
            return after >= ramp ? .25f : .25f * (1 - after / ramp) + .25f * (after / ramp) * 0f + (after >= ramp ? 0 : .25f * (1 - after / ramp)) * 0f;
        }

        static float EaseOut(float t) { t = Mathf.Clamp01(t); return 1 - (1 - t) * (1 - t); }

        public void Tick(in StageContext c)
        {
            var frame = root.parent ?? root;
            float width = float.IsNaN(frame.resolvedStyle.width) ? 0 : frame.resolvedStyle.width;
            if (!ReferenceEquals(c.document, shownDocument) || c.active != shownLine || Mathf.Abs(c.fontScale - shownScale) > .01f ||
                Mathf.Abs(width - shownWidth) > 24f || c.estimate != shownEstimate || c.calm != shownCalm || !c.tuning.Equals(shownTuning))
                Rebuild(c, width);

            float dt = Mathf.Clamp(c.dt, 0, 1f / 20f), now = Time.realtimeSinceStartup;
            Color baseColor = new Color(.93f, .9f, .8f), accent = Color.Lerp(Color.white, c.accent, .55f);
            bool finished = !double.IsInfinity(lineEnd) && c.clock > lineEnd + 2.5 && nextStart - c.clock > 3;
            bool interlude = blank || finished || shownLine < 0;
            dots.style.display = interlude ? DisplayStyle.Flex : DisplayStyle.None;
            dots.style.opacity = c.calm ? .5f : .35f + .3f * (.5f + .5f * Mathf.Sin(now * 2.6f));
            float lineFade = finished ? 0f : 1f;

            foreach (var segment in segments)
            {
                // The row comes in when its first word is about to be sung: a short rise and fade, and for the italic row a little growth.
                float since = (float)(c.clock - segment.revealAt);
                float segmentProgress = c.calm ? (since >= 0 ? 1f : .3f) : EaseOut(since / (segment.tilt ? .6f : .55f));
                segment.row.style.opacity = segmentProgress * lineFade;
                segment.row.style.translate = new Translate(0, c.calm ? 0 : (1 - segmentProgress) * (segment.tilt ? 24f : 20f));
                float segmentScale = segment.tilt && !c.calm ? .92f + .08f * segmentProgress : 1f;
                segment.row.style.scale = new Scale(new Vector3(segmentScale, segmentScale, 1));
                for (int i = 0; i < segment.labels.Count; i++)
                {
                    var label = segment.labels[i];
                    if (segment.space[i]) continue;
                    // Characters fade in one after another (0.04 s apart, 0.05 s in the italic row) once the row has come in.
                    float step = segment.tilt ? .05f : .04f;
                    float p = c.calm ? (since >= 0 ? 1f : 0f) : EaseOut((since - segment.order[i] * step) / .5f);
                    label.style.opacity = p;
                    float stagger = segment.tilt ? ((segment.order[i] % 2 == 0) ? -1f : 1f) * segment.lift : 0f;
                    label.style.translate = new Translate(0, c.calm ? stagger : stagger * (2 - p));
                    float intensity = c.calm ? 0 : Pulse(c.clock, segment.start[i], segment.end[i]);
                    float swell = 1 + intensity * (segment.tilt ? .18f : .15f);
                    label.style.scale = new Scale(new Vector3(swell, swell, 1));
                    label.style.color = segment.tilt ? accent : baseColor;
                }
            }

            float intro = c.calm || enterDuration <= 0 ? 1f : Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - builtAt) / enterDuration));
            if (column != null)
            {
                column.style.opacity = Mathf.Lerp(0f, 1f, intro) * lineFade;
                float introScale = Mathf.Lerp(enterScale, 1f, intro);
                column.style.scale = new Scale(new Vector3(introScale, introScale, 1));
            }
            for (int i = leaving.Count - 1; i >= 0; i--)
            {
                var l = leaving[i]; l.age += dt;
                float t = Mathf.Clamp01(l.age / l.duration), ease = 1 - (1 - t) * (1 - t);
                l.element.style.opacity = 1 - ease;
                l.element.style.translate = new Translate(0, -12f * ease);
                if (t >= 1) { l.element.RemoveFromHierarchy(); leaving.RemoveAt(i); }
            }
        }

        static string Normalize(string text)
        {
            var letters = new System.Text.StringBuilder();
            foreach (char c in text ?? "") if (char.IsLetterOrDigit(c)) letters.Append(char.ToLowerInvariant(c));
            return letters.ToString();
        }

        static float EstimateWidth(List<LyricUnit> cut, float size, float letterSpacing)
        {
            float total = 0;
            foreach (var unit in cut) foreach (var glyph in unit.glyphs) foreach (char ch in glyph.text) total += (ch >= 0x2E80 ? 1f : ch == ' ' ? .3f : .55f) * size + letterSpacing;
            return total;
        }

        void Rebuild(in StageContext c, float width)
        {
            var document = c.document; int active = c.active; var tuning = c.tuning;
            shownWidth = width; shownDocument = document; shownLine = active; shownScale = c.fontScale; shownEstimate = c.estimate; shownCalm = c.calm; shownTuning = tuning;
            builtAt = Time.realtimeSinceStartup;
            if (column != null && segments.Count > 0 && !c.calm && leaving.Count < 3)
            {
                var old = column; old.style.position = Position.Absolute; old.style.left = 0; old.style.right = 0; old.style.top = 0;
                leaving.Add(new Leaving { element = old, duration = exitDuration, endScale = exitScale });
            }
            else column?.RemoveFromHierarchy();
            segments.Clear(); blank = false; lineEnd = nextStart = double.PositiveInfinity;
            column = new VisualElement { pickingMode = PickingMode.Ignore }; column.AddToClassList("music-stage-tilt-column");
            lineHost.Add(column);
            if (document == null || active < 0 || active >= document.lines.Count) return;
            var data = document.lines[active];
            nextStart = active + 1 < document.lines.Count ? document.lines[active + 1].start : double.PositiveInfinity;
            lineEnd = !double.IsInfinity(data.end) && data.end > data.start ? data.end : nextStart;
            if (string.IsNullOrWhiteSpace(data.text)) { blank = true; return; }

            var glyphs = MusicLyricUnits.Timeline(data, nextStart, c.estimate, out _);
            if (glyphs.Count == 0) glyphs.Add(new LyricGlyph { text = data.text, start = data.start, end = double.IsInfinity(lineEnd) ? data.start + 4 : lineEnd });
            var units = tuning.legacyLayout ? MusicLyricUnits.GroupLegacy(glyphs) : MusicLyricUnits.Group(glyphs);

            int chars = data.text.Trim().Length, seed = active * 7919 + chars * 131;
            bool ellipsisOnly = System.Text.RegularExpressions.Regex.IsMatch(data.text.Trim(), "^[\\s.…·。]+$");
            var cuts = ellipsisOnly ? new List<List<LyricUnit>> { units } : Split(units, LineCount(chars, seed));

            // One row at most is set in italic: each row rolls against the tilt probability, and one of the winners is picked.
            var candidates = new List<int>();
            for (int i = 0; i < cuts.Count; i++) if (StageWord.Random(seed, 100 + i) < TiltProbability) candidates.Add(i);
            int tiltIndex = candidates.Count > 0 ? candidates[Mathf.Min(candidates.Count - 1, Mathf.FloorToInt(StageWord.Random(seed, 200) * candidates.Count))] : -1;
            if (TiltRowOverride >= 0) tiltIndex = Mathf.Min(TiltRowOverride, cuts.Count - 1);

            // Type size follows the stage width (6.875% of it, between 50 and 90 px); the widest row sets the shrink so nothing leaves the stage.
            float stageWidth = width < 120 ? 760f : width, avail = Mathf.Max(240f, stageWidth * .85f);
            float basePx = Mathf.Clamp(stageWidth * .06875f, 50f, 90f) * c.fontScale;
            float widest = 0;
            for (int i = 0; i < cuts.Count; i++) widest = Mathf.Max(widest, EstimateWidth(cuts[i], basePx, basePx * (i == tiltIndex ? .15f : .08f)));
            float shrink = widest > avail ? Mathf.Max(.5f, avail / widest) : 1f;
            // A very short last row after a long one is set a little larger so it does not look like a leftover.
            for (int i = 0; i < cuts.Count; i++)
            {
                var cut = cuts[i]; bool tilt = i == tiltIndex;
                int rowChars = 0; foreach (var u in cut) rowChars += u.glyphs.Count;
                bool shortLast = false;
                if (i == cuts.Count - 1 && i > 0 && !tilt && rowChars <= 2) { int prev = 0; foreach (var u in cuts[i - 1]) prev += u.glyphs.Count; shortLast = rowChars * 2 <= prev; }
                float size = basePx * shrink * (shortLast ? 1.18f : 1f), spacing = size * (tilt ? .15f : .08f);
                var segment = new Segment { tilt = tilt, lift = size / 6f, revealAt = cut[0].start - .15 };
                segment.row = new VisualElement { pickingMode = PickingMode.Ignore }; segment.row.AddToClassList("music-stage-tilt-row");
                // The italic row's characters are lifted and dropped by a sixth of their size; leave that much room above and below so a dropped
                // character never touches the row beneath it.
                if (tilt) { segment.row.style.marginTop = segment.lift + 2f; segment.row.style.marginBottom = segment.lift + 2f; }
                int order = 0;
                foreach (var unit in cut)
                {
                    foreach (var glyph in unit.glyphs)
                    {
                        bool isSpace = string.IsNullOrWhiteSpace(glyph.text);
                        var label = new Label(glyph.text.Replace(' ', ' ')) { pickingMode = PickingMode.Ignore };
                        label.AddToClassList("music-stage-tilt-char"); if (tilt) label.AddToClassList("music-stage-tilt-italic");
                        label.style.fontSize = size; label.style.letterSpacing = spacing; StageFonts.Apply(label);
                        label.style.opacity = 0;
                        segment.row.Add(label); segment.labels.Add(label); segment.space.Add(isSpace);
                        segment.order.Add(isSpace ? Mathf.Max(0, order - 1) : order); segment.start.Add((float)glyph.start); segment.end.Add((float)glyph.end);
                        if (!isSpace) order++;
                    }
                }
                column.Add(segment.row); segments.Add(segment);
            }
        }
    }
}
