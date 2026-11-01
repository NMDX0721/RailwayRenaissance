using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage style "Pendolo" (摆线): the lines hang on one large arc, like the end of a pendulum. The line being sung rests at the bottom of the
    /// arc, upright and largest; earlier lines swing away up the left side, coming lines wait up the right side, smaller and dimmer. When the
    /// line changes the arc swings round on an under-damped spring, so the new line settles at the bottom after a small overshoot, as a weight
    /// does. Every character follows the arc's tangent. Bass nudges the swing. Only the arc: no clockwork, no dial. The pose is a function
    /// of the audio clock apart from the swing, which settles on its spring. Independent implementation of the behaviour.
    /// </summary>
    public sealed class MusicStagePendolo : IStageStyle
    {
        sealed class ArcGlyph
        {
            public VisualElement box; public Label label;
            public float offset;            // distance along the arc from the middle of its line, in pixels
            public float start;
        }

        sealed class ArcLine
        {
            public int index; public float width, centre; public List<ArcGlyph> glyphs = new List<ArcGlyph>();
        }

        const int Behind = 2, Ahead = 3;                       // lines kept on the arc around the current one

        readonly VisualElement root = new VisualElement(), wheel = new VisualElement();
        readonly Label translation = new Label();
        readonly List<ArcLine> lines = new List<ArcLine>();
        MusicTimedDocument shownDocument; int shownCentre = -9; float shownScale, shownWidth, shownHeight; bool shownEstimate, shownCalm; StageTuning shownTuning;
        float swing, swingVelocity; bool swingKnown;
        float radius, pivotY, topAngle;

        public string Id => "pendolo";
        public string Title => "摆线";
        public bool OwnsSubtitles => true;
        public VisualElement Root => root;

        public MusicStagePendolo()
        {
            root.AddToClassList("music-stage-pendolo"); root.pickingMode = PickingMode.Ignore;
            wheel.AddToClassList("music-stage-pendolo-wheel"); wheel.pickingMode = PickingMode.Ignore;
            translation.AddToClassList("music-stage-pendolo-translation"); translation.pickingMode = PickingMode.Ignore;
            root.Add(wheel); root.Add(translation);
        }

        public void Invalidate() { shownDocument = null; shownCentre = -3; }

        static float Advance(string text, float size)
        {
            float total = 0;
            foreach (char ch in text) total += (ch >= 0x2E80 ? 1f : ch == (char)32 || ch == (char)160 ? .6f : .64f) * size;
            return Mathf.Max(size * .35f, total);
        }

        public void Tick(in StageContext c)
        {
            var frame = root.parent ?? root;
            float width = float.IsNaN(frame.resolvedStyle.width) ? 0 : frame.resolvedStyle.width, height = float.IsNaN(frame.resolvedStyle.height) ? 0 : frame.resolvedStyle.height;
            root.style.height = Mathf.Max(260f, (height < 120 ? 460f : height) * .78f);
            int centre = c.active;
            if (!ReferenceEquals(c.document, shownDocument) || centre != shownCentre || Mathf.Abs(c.fontScale - shownScale) > .01f || Mathf.Abs(width - shownWidth) > 24f ||
                Mathf.Abs(height - shownHeight) > 40f || c.estimate != shownEstimate || c.calm != shownCalm || !c.tuning.Equals(shownTuning))
                Rebuild(c, width, height);

            float dt = Mathf.Clamp(c.dt, 0, 1f / 20f);
            // The arc swings to bring the current line to the bottom. Under-damped, so it overshoots a little and settles: a weight, not a slide.
            float target = TargetFor(centre);
            if (!swingKnown || c.calm || Mathf.Abs(target - swing) > radius * 2.2f) { swing = target; swingVelocity = 0; swingKnown = true; }
            else StageWord.Spring(ref swing, ref swingVelocity, target, dt, 46f, c.tuning.intensity == 0 ? 11f : c.tuning.intensity == 2 ? 3.6f : 6.2f);

            float sway = c.calm ? 0f : (Mathf.Sin((float)c.clock * .8f) * .012f + Mathf.Clamp01(c.low) * .018f * (c.tuning.intensity + 1) * .5f);
            float cx = float.IsNaN(wheel.resolvedStyle.width) ? 0 : wheel.resolvedStyle.width * .5f;
            Color baseColor = new Color(.93f, .9f, .8f, .6f), sungColor = Color.Lerp(Color.white, c.accent, .5f);

            foreach (var line in lines)
            {
                var data = c.document.lines[line.index];
                bool upcoming = line.index > centre;
                float lineGap = Mathf.Abs(line.centre - swing), span = line.width * .5f + radius * .22f;
                float focus = Mathf.Clamp01(1f - lineGap / span);
                foreach (var g in line.glyphs)
                {
                    float arc = line.centre + g.offset - swing;
                    float theta = arc / radius + sway;
                    float x = cx + Mathf.Sin(theta) * radius, y = pivotY + Mathf.Cos(theta) * radius;
                    float angle = -theta * Mathf.Rad2Deg;
                    // Fade out before the arc leaves the stage at either end.
                    float edge = Mathf.Clamp01((topAngle - Mathf.Abs(theta)) / .16f);
                    float opacity = Mathf.Lerp(.28f, 1f, focus) * edge;
                    if (upcoming) opacity *= .45f + .55f * Mathf.Clamp01((float)((c.clock - (data.start - 1.2)) / 1.2));
                    float scale = Mathf.Lerp(.58f, 1.12f, focus);
                    bool sung = c.clock >= g.start;
                    g.box.style.opacity = c.calm ? Mathf.Clamp01(opacity * 1.15f) : opacity;
                    g.box.style.translate = new Translate(x, y);
                    g.box.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
                    g.box.style.scale = new Scale(new Vector3(scale, scale, 1));
                    g.label.style.color = sung ? Color.Lerp(baseColor, sungColor, .7f + .3f * focus) : baseColor;
                    g.label.style.textShadow = sung && focus > .5f
                        ? new TextShadow { offset = Vector2.zero, blurRadius = 12f * focus + 4f * c.high, color = new Color(c.accent.r, c.accent.g, c.accent.b, .7f * focus) }
                        : new TextShadow { offset = Vector2.zero, blurRadius = 0, color = Color.clear };
                }
            }

            bool hasTranslation = c.showTranslation && centre >= 0 && centre < c.document.lines.Count && !string.IsNullOrEmpty(c.document.lines[centre].translation);
            translation.style.display = hasTranslation ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasTranslation && translation.text != c.document.lines[centre].translation) { translation.text = c.document.lines[centre].translation; StageFonts.Apply(translation); }
        }

        // Where on the arc the swing rests for a given current line: that line's middle. Before the first line, just short of it.
        float TargetFor(int centre)
        {
            foreach (var line in lines) if (line.index == centre) return line.centre;
            if (lines.Count == 0) return 0;
            if (centre < lines[0].index) return lines[0].centre - lines[0].width * .5f - radius * .3f;
            return lines[lines.Count - 1].centre;
        }

        void Rebuild(in StageContext c, float width, float height)
        {
            shownWidth = width; shownHeight = height; shownDocument = c.document; shownCentre = c.active; shownScale = c.fontScale; shownEstimate = c.estimate; shownCalm = c.calm; shownTuning = c.tuning;
            wheel.Clear(); lines.Clear();
            var document = c.document; if (document == null || document.lines.Count == 0) return;
            float stageWidth = width < 120 ? 760f : width, stageHeight = height < 120 ? 460f : height, areaHeight = Mathf.Max(260f, stageHeight * .78f);
            radius = Mathf.Clamp(stageWidth * .8f, 380f, 820f);
            float glyphSize = Mathf.Clamp(radius * .07f, 24f, 44f) * c.fontScale;
            // The lowest point of the arc sits a little below the middle of the area; the arc rises from there on both sides.
            float bottom = areaHeight * .6f;
            pivotY = bottom - radius;
            float rise = bottom - 28f;
            topAngle = Mathf.Acos(Mathf.Clamp((radius - rise) / radius, -1f, 1f));

            int centre = Mathf.Max(-1, c.active);
            // Measure first, at one unit of type size, so that the current line can be shrunk to fit the stage in one step.
            var picked = new List<(int index, LyricGlyphSet set, float unit)>();
            float widest = 0;
            for (int index = centre - Behind; index <= centre + Ahead; index++)
            {
                if (index < 0 || index >= document.lines.Count) continue;
                var data = document.lines[index]; if (string.IsNullOrWhiteSpace(data.text)) continue;
                double nextStart = index + 1 < document.lines.Count ? document.lines[index + 1].start : double.PositiveInfinity;
                var glyphs = MusicLyricUnits.Timeline(data, nextStart, c.estimate, out _);
                if (glyphs.Count == 0) glyphs.Add(new LyricGlyph { text = data.text, start = data.start, end = double.IsInfinity(nextStart) ? data.start + 4 : nextStart });
                float unit = 0; foreach (var g in glyphs) unit += Advance(g.text.Replace(' ', ' '), 1f);
                picked.Add((index, new LyricGlyphSet { glyphs = glyphs }, unit));
                if (index == centre) widest = Mathf.Max(widest, unit);
            }
            if (widest > 0) glyphSize = Mathf.Min(glyphSize, stageWidth * .8f / (widest * 1.12f));
            glyphSize = Mathf.Max(glyphSize, 16f);

            float gap = glyphSize * 2.6f, cursor = 0;
            foreach (var entry in picked)
            {
                var line = new ArcLine { index = entry.index, width = entry.unit * glyphSize };
                cursor += lines.Count == 0 ? line.width * .5f : lines[lines.Count - 1].width * .5f + gap + line.width * .5f;
                line.centre = cursor;
                float at = -line.width * .5f;
                foreach (var g in entry.set.glyphs)
                {
                    string text = g.text.Replace(' ', ' ');
                    float advance = Advance(text, glyphSize);
                    var glyph = new ArcGlyph { offset = at + advance * .5f, start = (float)g.start };
                    glyph.box = new VisualElement { pickingMode = PickingMode.Ignore }; glyph.box.AddToClassList("music-stage-pendolo-glyph");
                    glyph.box.style.height = glyphSize * 1.5f; glyph.box.style.top = -glyphSize * .75f;
                    glyph.label = new Label(text) { pickingMode = PickingMode.Ignore }; glyph.label.AddToClassList("music-stage-pendolo-char"); glyph.label.style.fontSize = glyphSize; StageFonts.Apply(glyph.label);
                    glyph.box.Add(glyph.label); wheel.Add(glyph.box); line.glyphs.Add(glyph);
                    at += advance;
                }
                lines.Add(line);
            }
        }

        sealed class LyricGlyphSet { public List<LyricGlyph> glyphs; }
    }
}
