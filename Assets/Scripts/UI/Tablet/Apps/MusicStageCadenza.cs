using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage style "Cadenza" (心象), after Folia's mindscape style. The line is laid out like running text, then one word is picked as the
    /// hero (longer Latin words and words near the middle score higher): it moves to the middle of the stage and grows. Every other word is
    /// pushed outwards from its place in the text until it clears the hero and its neighbours, so the line breaks up into a loose cloud.
    /// A ring of light sits around the character being sung. Words come forward, glow while sung and drift off turned a little once sung.
    /// Placement is seeded from the line, so it does not change when the screen redraws. Independent implementation of the behaviour.
    /// </summary>
    public sealed class MusicStageCadenza : IStageStyle
    {
        sealed class Placed
        {
            public StageWord word; public VisualElement holder;
            public float x, y, width, height, emphasis = 1f;
        }

        sealed class Leaving { public VisualElement element; public float age, duration, endScale; }

        readonly VisualElement root = new VisualElement(), field = new VisualElement(), halo = new VisualElement();
        readonly Label dots = new Label("· · ·");
        readonly List<Placed> placed = new List<Placed>();
        readonly List<Leaving> leaving = new List<Leaving>();
        MusicTimedDocument shownDocument; int shownLine = -2; float shownScale, shownWidth, shownHeight; bool shownEstimate, shownCalm; StageTuning shownTuning;
        StageReveal reveal; double lineEnd = double.PositiveInfinity, nextStart = double.PositiveInfinity;
        bool chorus, blank; float builtAt, enterDuration = .3f, enterScale = .9f, enterOpacity, exitScale = 1.1f, exitDuration = .3f, size = 40f;
        Vector2 haloAt; float haloAlpha;

        public string Id => "cadenza";
        public string Title => "心象";
        public bool OwnsSubtitles => false;
        public VisualElement Root => root;

        public MusicStageCadenza()
        {
            root.AddToClassList("music-stage-cadenza"); root.pickingMode = PickingMode.Ignore;
            field.AddToClassList("music-stage-cadenza-field"); field.pickingMode = PickingMode.Ignore;
            halo.AddToClassList("music-stage-cadenza-halo"); halo.pickingMode = PickingMode.Ignore; halo.style.opacity = 0;
            dots.AddToClassList("music-stage-dots"); dots.pickingMode = PickingMode.Ignore;
            root.Add(dots); root.Add(field);
        }

        public void Invalidate() { shownDocument = null; shownLine = -3; }

        static float Advance(string text, float px)
        {
            float total = 0;
            foreach (char ch in text) total += (ch >= 0x2E80 ? 1f : ch == ' ' || ch == ' ' ? .35f : .55f) * px;
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
            var frame = root.parent ?? root;
            float width = float.IsNaN(frame.resolvedStyle.width) ? 0 : frame.resolvedStyle.width, height = float.IsNaN(frame.resolvedStyle.height) ? 0 : frame.resolvedStyle.height;
            root.style.height = Mathf.Max(260f, (height < 120 ? 460f : height) * .72f);
            if (!ReferenceEquals(c.document, shownDocument) || c.active != shownLine || Mathf.Abs(c.fontScale - shownScale) > .01f || Mathf.Abs(width - shownWidth) > 24f ||
                Mathf.Abs(height - shownHeight) > 40f || c.estimate != shownEstimate || c.calm != shownCalm || !c.tuning.Equals(shownTuning))
                Rebuild(c, width, height);

            float dt = Mathf.Clamp(c.dt, 0, 1f / 20f), now = Time.realtimeSinceStartup;
            var tuning = c.tuning; bool chaotic = tuning.intensity == 2;
            Color baseColor = new Color(.93f, .9f, .8f), activeColor = Color.Lerp(Color.white, c.accent, .35f);
            bool finished = !double.IsInfinity(lineEnd) && c.clock > lineEnd + 2.5 && nextStart - c.clock > 3;
            bool interlude = blank || finished || shownLine < 0;
            dots.style.display = interlude ? DisplayStyle.Flex : DisplayStyle.None;
            dots.style.opacity = c.calm ? .5f : .35f + .3f * (.5f + .5f * Mathf.Sin(now * 2.6f));
            float lineFade = finished ? 0f : 1f;

            // The ring of light goes round the character being sung: it eases to that character and fades out when nothing is being sung.
            Vector2 target = haloAt; float targetAlpha = 0;
            foreach (var p in placed)
            {
                p.word.Tick(c.clock, dt, c.calm, reveal, lineEnd, chorus, tuning.rotation, chaotic, baseColor, activeColor, c.accent, c.high, lineFade);
                if (p.word.state != 1) continue;
                int glyph = 0; for (int g = 0; g < p.word.unit.glyphs.Count; g++) if (c.clock >= p.word.unit.glyphs[g].start) glyph = g;
                float step = p.width / Mathf.Max(1, p.word.unit.glyphs.Count);
                target = new Vector2(p.x + ((glyph + .5f) * step - p.width * .5f) * p.word.s, p.y + p.word.y);
                targetAlpha = 1f;
            }
            if (haloAlpha < .02f && targetAlpha > 0) haloAt = target;
            haloAt = Vector2.Lerp(haloAt, target, 1 - Mathf.Exp(-dt * 14f));
            haloAlpha = c.calm ? targetAlpha : Mathf.Lerp(haloAlpha, targetAlpha, 1 - Mathf.Exp(-dt * (targetAlpha > haloAlpha ? 18f : 5f)));
            float ringSize = size * 1.9f * (1f + .12f * Mathf.Clamp01(c.high) + (c.calm ? 0 : .04f * Mathf.Sin(now * 5f)));
            halo.style.width = ringSize; halo.style.height = ringSize; halo.style.marginLeft = haloAt.x - ringSize * .5f; halo.style.marginTop = haloAt.y - ringSize * .5f;
            halo.style.opacity = haloAlpha * lineFade * .85f;
            halo.style.borderTopColor = halo.style.borderBottomColor = halo.style.borderLeftColor = halo.style.borderRightColor = activeColor;

            float intro = c.calm || enterDuration <= 0 ? 1f : Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - builtAt) / enterDuration));
            field.style.opacity = Mathf.Lerp(enterOpacity, 1f, intro);
            float introScale = Mathf.Lerp(enterScale, 1f, intro);
            field.style.scale = new Scale(new Vector3(introScale, introScale, 1));
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
            // The words that were on stage fade out underneath instead of vanishing.
            if (placed.Count > 0 && !c.calm && leaving.Count < 3)
            {
                var clone = new VisualElement { pickingMode = PickingMode.Ignore };
                // Move the old holders onto a throwaway layer that fades; the field itself is reused for the new line.
                clone.AddToClassList("music-stage-cadenza-field"); clone.style.position = Position.Absolute; clone.style.left = 0; clone.style.right = 0; clone.style.top = 0; clone.style.bottom = 0;
                foreach (var p in placed) clone.Add(p.holder);
                root.Insert(0, clone);
                leaving.Add(new Leaving { element = clone, duration = exitDuration, endScale = exitScale });
            }
            else foreach (var p in placed) p.holder.RemoveFromHierarchy();
            placed.Clear(); chorus = false; blank = false; lineEnd = nextStart = double.PositiveInfinity;
            if (halo.parent != field) field.Add(halo);
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

            float stageWidth = width < 120 ? 760f : width, stageHeight = height < 120 ? 420f : height * .72f;
            size = Mathf.Clamp(stageWidth * .045f, 28f, 54f) * c.fontScale;
            float lineHeight = size * 1.4f, maxWidth = stageWidth * .8f;
            int seed = Mathf.RoundToInt((float)(data.start * 1000)) + active * 7919;

            // 1. Lay the words out as running text, wrapped to the stage width, each row centred; this is where each word would sit in plain text.
            var items = new List<Placed>(); var rows = new List<List<Placed>>(); var row = new List<Placed>(); float rowWidth = 0; float gap = size * .45f;
            for (int i = 0; i < units.Count; i++)
            {
                string text = ""; foreach (var g in units[i].glyphs) text += g.text;
                float w = Advance(text.Replace(' ', ' '), size);
                if (row.Count > 0 && rowWidth + gap + w > maxWidth) { rows.Add(row); row = new List<Placed>(); rowWidth = 0; }
                var p = new Placed { width = w, height = size * .95f };
                p.word = StageWord.Create(units[i], size, c.accent, chorus, seed + i, 0f, 0f, 1f, 0f, tuning.rotation, c.calm, false);
                row.Add(p); items.Add(p); rowWidth += (row.Count > 1 ? gap : 0) + w;
            }
            if (row.Count > 0) rows.Add(row);
            float totalHeight = rows.Count * lineHeight;
            for (int r = 0; r < rows.Count; r++)
            {
                float rw = 0; foreach (var p in rows[r]) rw += p.width; rw += gap * (rows[r].Count - 1);
                float cursor = -rw / 2, y = -totalHeight / 2 + (r + .5f) * lineHeight;
                foreach (var p in rows[r]) { p.x = cursor + p.width / 2; p.y = y; cursor += p.width + gap; }
            }

            // 2. Pick the hero: CJK words score .18, Latin words by length up to .36, plus a bonus near the middle of the line.
            Placed hero = null; float best = -1;
            for (int i = 0; i < items.Count; i++)
            {
                var unit = items[i].word.unit; bool cjk = unit.glyphs[0].text[0] >= 0x2E80;
                float semantic = cjk ? .18f : Mathf.Min(unit.glyphs.Count * .08f, .36f);
                float centreBias = 1f - Mathf.Abs(i - (items.Count - 1) / 2f) / Mathf.Max(items.Count, 1);
                float score = semantic + centreBias * .18f;
                if (score > best) { best = score; hero = items[i]; }
            }
            if (hero != null && items.Count > 1 && !calmLook)
            {
                hero.emphasis = 1.46f * (1f + Mathf.Clamp(best - .48f, 0f, .52f));
                hero.x = 0; hero.y = 0;
            }

            // 3. Push every other word away from the hero (and from words already placed) in small steps until it is clear of them.
            var occupied = new List<Rect>(); float pad = size * .12f;
            // A word is largest while it is sung (the shared word pose magnifies it 1.4 times), and the hero reaches its emphasis then, so the room
            // a word needs is its sung size, not its resting size.
            Rect RectOf(Placed p, float px, float py) { float grow = p.emphasis > 1f ? p.emphasis : 1.4f, w = p.width * grow, h = p.height * grow; return new Rect(px - w / 2 - pad, py - h / 2 - pad, w + pad * 2, h + pad * 2); }
            bool Clear(Rect rect) { foreach (var o in occupied) if (o.Overlaps(rect)) return false; return true; }
            if (hero != null) occupied.Add(RectOf(hero, hero.x, hero.y));
            float limitX = stageWidth * .46f, limitY = stageHeight * .46f, stepLength = Mathf.Max(8f, size * .14f);
            foreach (var p in items)
            {
                if (p == hero) continue;
                float px = p.x, py = p.y;
                if (!Clear(RectOf(p, px, py)))
                {
                    float angle0 = Mathf.Atan2(py, px == 0 && py == 0 ? 1f : px); if (px == 0 && py == 0) angle0 = StageWord.Random(seed, 5) * Mathf.PI * 2f;
                    bool found = false;
                    for (float radius = stepLength; radius < size * 7f && !found; radius += stepLength)
                        for (int k = 0; k < 12 && !found; k++)
                        {
                            float a = angle0 + (k % 2 == 0 ? 1 : -1) * (k / 2) * .35f;
                            float tx = Mathf.Clamp(p.x + Mathf.Cos(a) * radius, -limitX + p.width / 2, limitX - p.width / 2), ty = Mathf.Clamp(p.y + Mathf.Sin(a) * radius * .8f, -limitY, limitY);
                            if (Clear(RectOf(p, tx, ty))) { px = tx; py = ty; found = true; }
                        }
                }
                p.x = px; p.y = py; occupied.Add(RectOf(p, px, py));
            }

            // 4. Build the holders: a zero-size element at each word's place, with the word centred on it and scaled by its emphasis.
            foreach (var p in items)
            {
                p.holder = new VisualElement { pickingMode = PickingMode.Ignore }; p.holder.AddToClassList("music-stage-cadenza-holder");
                p.holder.style.marginLeft = p.x; p.holder.style.marginTop = p.y;
                var body = p.word.body; body.style.position = Position.Absolute; body.style.width = p.width + size; body.style.left = -(p.width + size) / 2; body.style.top = -size * .7f;
                body.style.justifyContent = Justify.Center; body.style.marginRight = 0;
                // The body is wider than its text, so the glow layer must be centred over it like the characters, or it shows as a ghost beside them.
                p.word.glow.style.right = 0; p.word.glow.style.unityTextAlign = TextAnchor.UpperCenter;
                // The hero's emphasis is the size it reaches when sung, and the word pose already magnifies a sung word 1.4 times.
                if (p.emphasis > 1f) p.word.baseScale = p.emphasis / 1.4f;
                p.holder.Add(body); field.Insert(field.childCount > 0 && field.IndexOf(halo) >= 0 ? field.IndexOf(halo) : field.childCount, p.holder); placed.Add(p);
            }
        }
    }
}
