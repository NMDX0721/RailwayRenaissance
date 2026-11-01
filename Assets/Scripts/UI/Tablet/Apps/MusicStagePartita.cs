using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage style "Partita" (云阶), after Folia's score-like style. The current line is cut into a few uneven rows stacked in one column.
    /// Rows are pushed left and right alternately, like staves on a handwritten score; each row comes forward when its first word is
    /// about to be sung, and a guide line grows under it from alternating sides. The words inside a row animate like Luminous words.
    /// Row cuts and offsets are seeded from the line, so they stay put when the screen redraws. Independent implementation of the behaviour.
    /// </summary>
    public sealed class MusicStagePartita : IStageStyle
    {
        sealed class Chunk
        {
            public VisualElement row, guideH, guideV;
            public readonly List<StageWord> words = new List<StageWord>();
            public double start, end;
            public float cfgX, cfgY, cfgRot, x, y, s = .85f, r, o, vx, vy, vs, vr, guide;
            public bool left;
            public int state = -1;
        }

        sealed class Leaving { public VisualElement element; public float age, duration, endScale; }

        // Folia's defaults: rows are offset by 20..70 px, alternately to the left and right.
        const float StaggerMin = 20f, StaggerMax = 70f;

        readonly VisualElement root = new VisualElement(), lineHost = new VisualElement();
        readonly Label dots = new Label("· · ·");
        readonly List<Chunk> chunks = new List<Chunk>();
        readonly List<Leaving> leaving = new List<Leaving>();
        VisualElement column;
        MusicTimedDocument shownDocument; int shownLine = -2; float shownScale, shownWidth, shownHeight; bool shownEstimate, shownCalm; StageTuning shownTuning;
        StageReveal reveal; double lineEnd = double.PositiveInfinity, nextStart = double.PositiveInfinity;
        bool chorus, blank; float builtAt, enterDuration, enterScale = 1, enterOpacity = 1, exitScale = 1.1f, exitDuration = .3f;

        public string Id => "partita";
        public string Title => "云阶";
        public bool OwnsSubtitles => false;
        public VisualElement Root => root;

        public MusicStagePartita()
        {
            root.AddToClassList("music-stage-luminous"); root.pickingMode = PickingMode.Ignore;
            dots.AddToClassList("music-stage-dots"); dots.pickingMode = PickingMode.Ignore;
            lineHost.AddToClassList("music-stage-line-host"); lineHost.pickingMode = PickingMode.Ignore;
            root.Add(dots); root.Add(lineHost);
        }

        public void Invalidate() { shownDocument = null; shownLine = -3; }

        static float Advance(string text, float px)
        {
            float total = 0;
            // The pixel font's Latin letters measure about 0.65 em, wider than the 0.55 a proportional font would give; an estimate that is too small
            // under-shrinks long rows, which is what pushed the last letters of a long English row past the stage edge.
            foreach (char ch in text) total += (ch >= 0x2E80 ? 1f : ch == ' ' || ch == ' ' ? .4f : .65f) * px;
            return Mathf.Max(px * .35f, total);
        }

        static string Normalize(string text)
        {
            var letters = new System.Text.StringBuilder();
            foreach (char c in text ?? "") if (char.IsLetterOrDigit(c)) letters.Append(char.ToLowerInvariant(c));
            return letters.ToString();
        }

        public void Tick(in StageContext c)
        {
            // The stage host fills the panel, so its size is stable; the style's own root grows and shrinks with its rows, and measuring that
            // would make every rebuild change the measurement and trigger the next rebuild.
            var frame = root.parent ?? root;
            float width = float.IsNaN(frame.resolvedStyle.width) ? 0 : frame.resolvedStyle.width, height = float.IsNaN(frame.resolvedStyle.height) ? 0 : frame.resolvedStyle.height;
            if (!ReferenceEquals(c.document, shownDocument) || c.active != shownLine || Mathf.Abs(c.fontScale - shownScale) > .01f ||
                Mathf.Abs(width - shownWidth) > 24f || Mathf.Abs(height - shownHeight) > 40f || c.estimate != shownEstimate || c.calm != shownCalm || !c.tuning.Equals(shownTuning))
                Rebuild(c, width, height);

            float dt = Mathf.Clamp(c.dt, 0, 1f / 20f), now = Time.realtimeSinceStartup;
            var tuning = c.tuning; bool chaotic = tuning.intensity == 2;
            Color baseColor = new Color(.93f, .9f, .8f), activeColor = Color.Lerp(Color.white, c.accent, .35f);

            bool finished = !double.IsInfinity(lineEnd) && c.clock > lineEnd + 2.5 && nextStart - c.clock > 3;
            bool interlude = blank || finished || shownLine < 0;
            dots.style.display = interlude ? DisplayStyle.Flex : DisplayStyle.None;
            dots.style.opacity = c.calm ? .5f : .35f + .3f * (.5f + .5f * Mathf.Sin(now * 2.6f));
            float lineFade = finished ? 0f : 1f;

            foreach (var chunk in chunks)
            {
                // A row is active from just before its first word until its last word ends; it stays up afterwards (passed) for the rest of the line.
                double lookahead = reveal == StageReveal.Instant ? .03 : reveal == StageReveal.Fast ? .08 : .15;
                double end = reveal == StageReveal.Instant ? (double.IsInfinity(lineEnd) ? chunk.end : lineEnd) : chunk.end;
                int state = c.clock > end ? 2 : c.clock >= chunk.start - lookahead ? 1 : 0;
                chunk.state = state;
                if (c.calm) { chunk.o = state == 0 ? .3f : 1f; chunk.s = 1; chunk.x = chunk.cfgX; chunk.y = chunk.cfgY; chunk.r = 0; }
                else
                {
                    float tx = state == 0 ? chunk.cfgX + (chunk.left ? -40f : 40f) : chunk.cfgX;
                    float ts = state == 0 ? .85f : 1f;
                    float stiffness = state == 1 ? 200f : 110f, damping = state == 1 ? 20f : 21f;
                    StageWord.Spring(ref chunk.s, ref chunk.vs, ts, dt, stiffness, damping); StageWord.Spring(ref chunk.x, ref chunk.vx, tx, dt, stiffness, damping);
                    StageWord.Spring(ref chunk.y, ref chunk.vy, chunk.cfgY, dt, stiffness, damping); StageWord.Spring(ref chunk.r, ref chunk.vr, chunk.cfgRot, dt, stiffness, damping);
                    chunk.o = Mathf.Lerp(chunk.o, state == 0 ? 0f : 1f, 1 - Mathf.Exp(-dt * (state == 1 ? 24f : 8f)));
                }
                chunk.row.style.opacity = chunk.o * lineFade;
                chunk.row.style.scale = new Scale(new Vector3(chunk.s, chunk.s, 1));
                chunk.row.style.translate = new Translate(chunk.x, chunk.y);
                chunk.row.style.rotate = new Rotate(new Angle(chunk.r, AngleUnit.Degree));

                // The guide line grows when the row arrives and takes the word colour while the row is being sung.
                chunk.guide = c.calm ? (state == 0 ? 0 : 1) : Mathf.Lerp(chunk.guide, state == 0 ? 0 : 1, 1 - Mathf.Exp(-dt * 8f));
                Color guideColor = state == 1 ? activeColor : state == 2 ? new Color(1, 1, 1, .22f) : new Color(1, 1, 1, .14f);
                chunk.guideH.style.scale = new Scale(new Vector3(chunk.guide, 1, 1)); chunk.guideV.style.scale = new Scale(new Vector3(1, chunk.guide, 1));
                chunk.guideH.style.opacity = chunk.guideV.style.opacity = chunk.guide;
                chunk.guideH.style.backgroundColor = chunk.guideV.style.backgroundColor = guideColor;

                foreach (var word in chunk.words)
                    word.Tick(c.clock, dt, c.calm, reveal, lineEnd, chorus, tuning.rotation, chaotic, baseColor, activeColor, c.accent, c.high, lineFade);
            }

            float intro = c.calm || enterDuration <= 0 ? 1f : Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - builtAt) / enterDuration));
            if (column != null)
            {
                column.style.opacity = Mathf.Lerp(enterOpacity, 1f, intro) * lineFade;
                float introScale = Mathf.Lerp(enterScale, 1f, intro);
                column.style.scale = new Scale(new Vector3(introScale, introScale, 1));
            }
            for (int i = leaving.Count - 1; i >= 0; i--)
            {
                var l = leaving[i]; l.age += dt;
                float t = Mathf.Clamp01(l.age / l.duration), ease = 1 - (1 - t) * (1 - t);
                l.element.style.opacity = 1 - ease;
                float scale = Mathf.Lerp(1f, l.endScale, ease); l.element.style.scale = new Scale(new Vector3(scale, scale, 1));
                if (t >= 1) { l.element.RemoveFromHierarchy(); leaving.RemoveAt(i); }
            }
        }

        void Rebuild(in StageContext c, float width, float height)
        {
            var document = c.document; int active = c.active; var tuning = c.tuning;
            shownWidth = width; shownHeight = height; shownDocument = document; shownLine = active; shownScale = c.fontScale; shownEstimate = c.estimate; shownCalm = c.calm; shownTuning = tuning;
            builtAt = Time.realtimeSinceStartup;
            if (column != null && chunks.Count > 0 && !c.calm && leaving.Count < 3)
            {
                var old = column; old.style.position = Position.Absolute; old.style.left = 0; old.style.right = 0; old.style.top = 0;
                leaving.Add(new Leaving { element = old, duration = exitDuration, endScale = exitScale });
            }
            else column?.RemoveFromHierarchy();
            chunks.Clear(); chorus = false; blank = false; lineEnd = nextStart = double.PositiveInfinity;
            column = new VisualElement { pickingMode = PickingMode.Ignore }; column.AddToClassList("music-stage-partita-column");
            lineHost.Add(column);
            enterDuration = 0; enterScale = 1; enterOpacity = 1;
            if (document == null || active < 0 || active >= document.lines.Count) return;
            var data = document.lines[active];
            nextStart = active + 1 < document.lines.Count ? document.lines[active + 1].start : double.PositiveInfinity;
            lineEnd = !double.IsInfinity(data.end) && data.end > data.start ? data.end : nextStart;
            if (string.IsNullOrWhiteSpace(data.text)) { blank = true; return; }

            string key = Normalize(data.text); int sung = 0;
            if (key.Length >= 4) foreach (var other in document.lines) if (Normalize(other.text) == key) sung++;
            chorus = sung >= 2;

            var glyphs = MusicLyricUnits.Timeline(data, nextStart, c.estimate, out _);
            if (glyphs.Count == 0) glyphs.Add(new LyricGlyph { text = data.text, start = data.start, end = double.IsInfinity(lineEnd) ? data.start + 4 : lineEnd });
            var units = tuning.legacyLayout ? MusicLyricUnits.GroupLegacy(glyphs) : MusicLyricUnits.Group(glyphs);
            reveal = StageWord.RevealFor(units);
            if (reveal == StageReveal.Normal) { enterDuration = .3f; enterScale = .9f; enterOpacity = 0f; exitDuration = .3f; exitScale = 1.1f; }
            else if (reveal == StageReveal.Fast) { enterDuration = .16f; enterScale = .96f; enterOpacity = .35f; exitDuration = .16f; exitScale = 1.04f; }
            else { enterDuration = 0; exitDuration = .12f; exitScale = 1.02f; }

            bool chaotic = tuning.intensity == 2, calmLook = tuning.intensity == 0;
            // How many rows the stage can hold: about 100 px per row in 65% of the stage height, never more rows than units.
            float stageHeight = height < 120 ? 420f : height;
            int rows = Mathf.Min(units.Count, Mathf.Max(1, Mathf.FloorToInt(stageHeight * .65f / 100f)));
            int seedBase = Mathf.RoundToInt((float)(data.start * 1000)) + active * 7919;
            int seedCounter = seedBase;
            float Next() { float x = Mathf.Sin(seedCounter++) * 10000f; return x - Mathf.Floor(x); }

            // Row lengths are deliberately uneven: an even split looks mechanical and loses the handwritten-score feel.
            var cuts = new List<List<LyricUnit>>(); int unitIndex = 0, remainingUnits = units.Count, remainingRows = rows;
            for (int i = 0; i < rows; i++)
            {
                float avg = (float)remainingUnits / remainingRows; int length;
                if (i == rows - 1) length = remainingUnits;
                else length = Mathf.Max(1, Mathf.Min(Mathf.CeilToInt(avg * 1.5f), Mathf.RoundToInt(avg + (Next() - .5f) * avg)));
                length = Mathf.Max(1, Mathf.Min(length, remainingUnits - (remainingRows - 1)));
                cuts.Add(units.GetRange(unitIndex, length)); unitIndex += length; remainingUnits -= length; remainingRows--;
            }

            float avail = Mathf.Max(240f, (width < 120 ? 760f : width) - 96f);
            // Each row gets about 100 px. A word is magnified up to 1.3 times by its row and, while it is sung, 1.4 times more, so the type size is set
            // from that worst case: it must still fit its row, or rows land on each other.
            float rowHeight = stageHeight * .6f / rows;
            float size = Mathf.Clamp(Mathf.Min(avail / 3.2f, rowHeight / (1.3f * 1.4f * 1.15f)), 20f, 46f) * c.fontScale;
            for (int row = 0; row < cuts.Count; row++)
            {
                var cut = cuts[row];
                var chunk = new Chunk { left = row % 2 == 0, start = cut[0].start, end = cut[cut.Count - 1].end };
                float magnitude = StaggerMin + Next() * (StaggerMax - StaggerMin);
                // Folia varies a row's scale from 0.8 to 1.7; held to 0.8 to 1.3 so a long line's rows do not grow into each other.
                float rowBias = row - (cuts.Count - 1) / 2f, staggerScale = calmLook ? 1f : .8f + Next() * .5f;
                int graphemes = 0; foreach (var u in cut) graphemes += u.glyphs.Count;
                chunk.cfgX = chunk.left ? -magnitude : magnitude;
                chunk.cfgY = calmLook ? 0f : rowBias * 2.5f;
                chunk.cfgRot = chaotic ? (chunk.left ? -3f : 3f) : 0f;
                float scale = (chaotic ? 1f + Mathf.Min(graphemes * .01f, .05f) : 1f) * staggerScale;
                if (c.calm) { chunk.cfgY = 0; chunk.cfgRot = 0; scale = 1; }
                // The row is offset sideways by up to StaggerMax and its words are magnified by <scale>, so its width must fit the stage with both counted;
                // a long row is shrunk (never below 45%) instead of running off the edge.
                float estimate = 0;
                foreach (var u in cut) { string t = ""; foreach (var g in u.glyphs) t += g.text; estimate += Advance(t, size) * 1.3f + Mathf.Max(10f, size * .3f); }
                // Rotated words and the trailing space of a word reach a little past its estimated width, so keep a comfortable margin.
                float room = Mathf.Max(200f, avail - StaggerMax * 2f - 64f);
                // 1.15: the word being sung grows by 1.4 times, which widens the row a little more.
                if (estimate * scale * 1.15f > room) scale *= Mathf.Max(.45f, room / (estimate * scale * 1.15f));
                chunk.x = chunk.cfgX + (chunk.left ? -40f : 40f); chunk.y = chunk.cfgY; chunk.r = chunk.cfgRot;

                chunk.row = new VisualElement { pickingMode = PickingMode.Ignore }; chunk.row.AddToClassList("music-stage-partita-row");
                chunk.row.style.marginBottom = chaotic ? 6f : 10f;
                string side = chunk.left ? "left" : "right";
                chunk.guideH = new VisualElement { pickingMode = PickingMode.Ignore }; chunk.guideH.AddToClassList("music-stage-guide-h"); chunk.guideH.AddToClassList("music-stage-guide-" + side);
                chunk.guideV = new VisualElement { pickingMode = PickingMode.Ignore }; chunk.guideV.AddToClassList("music-stage-guide-v"); chunk.guideV.AddToClassList("music-stage-guide-" + side);
                chunk.guideH.style.opacity = chunk.guideV.style.opacity = 0;
                chunk.row.Add(chunk.guideH); chunk.row.Add(chunk.guideV);
                float spread = chaotic ? 15f : calmLook ? 0f : 6f, tilt = chaotic ? 8f : calmLook ? 0f : 3f;
                for (int i = 0; i < cut.Count; i++)
                {
                    var word = StageWord.Create(cut[i], size, c.accent, chorus, seedBase + row * 131 + i * 7, spread, tilt, scale, 0f, tuning.rotation, c.calm, false);
                    // The word being sung grows by 1.4 times, about a fifth of its own width on each side, so the gap follows the word's width instead
                    // of being fixed; otherwise the sung word lands on its neighbour.
                    string unitText = ""; foreach (var g in cut[i].glyphs) unitText += g.text;
                    word.body.style.marginRight = Mathf.Max(10f, size * .3f) + Advance(unitText, size) * .3f * scale;
                    chunk.row.Add(word.body); chunk.words.Add(word);
                }
                column.Add(chunk.row); chunks.Add(chunk);
            }
        }
    }
}
