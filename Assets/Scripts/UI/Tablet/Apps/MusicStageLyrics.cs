using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage mode, after Folia's classic visualizer: only the current line is on stage. Each word waits (small, faded,
    /// off to the side), pops forward on a spring shortly before its time, then settles back as "passed".
    /// Poses are functions of the audio clock, so pausing freezes them and seeking rebuilds them.
    /// </summary>
    public sealed class MusicStageLyrics : VisualElement
    {
        sealed class StageWord
        {
            public VisualElement body; public Label label; public MusicTimedWord word;
            public float baseScale, cx, cy, rot, passedRot;
            public float x, y, s = .5f, r, o, tone;
            public float vx, vy, vs, vr;
        }

        const double Lookahead = .15;
        readonly Label previous = new Label(), upcoming = new Label(), translation = new Label();
        readonly VisualElement line = new VisualElement();
        readonly List<StageWord> words = new List<StageWord>();
        MusicTimedDocument shownDocument; int shownLine = -2; float shownScale; bool shownEstimate, shownTranslation, shownCalm;

        public MusicStageLyrics()
        {
            AddToClassList("music-stage-lyrics");
            previous.AddToClassList("music-stage-previous"); upcoming.AddToClassList("music-stage-upcoming");
            translation.AddToClassList("music-stage-translation"); line.AddToClassList("music-stage-line");
            Add(previous); Add(line); Add(translation); Add(upcoming);
            foreach (var label in new[] { previous, upcoming, translation }) label.pickingMode = PickingMode.Ignore;
            line.pickingMode = PickingMode.Ignore;
        }

        public void Tick(MusicTimedDocument document, int active, double clock, float dt, bool calm, bool estimate, float fontScale,
            Color accent, float energy, bool showTranslation)
        {
            if (!ReferenceEquals(document, shownDocument) || active != shownLine || Mathf.Abs(fontScale - shownScale) > .01f ||
                estimate != shownEstimate || showTranslation != shownTranslation || calm != shownCalm)
                Rebuild(document, active, estimate, fontScale, showTranslation, calm);

            dt = Mathf.Clamp(dt, 0, 1f / 20f);
            Color baseColor = new Color(.93f, .9f, .8f), activeColor = Color.Lerp(Color.white, accent, .35f);
            foreach (var w in words)
            {
                double end = double.IsInfinity(w.word.end) ? w.word.start + 1 : w.word.end;
                int state = clock > end ? 2 : clock >= w.word.start - Lookahead ? 1 : 0;
                if (calm)
                {
                    w.o = state == 0 ? .3f : 1f; w.s = 1; w.x = w.y = w.r = 0; w.tone = state == 1 ? 1 : 0;
                }
                else
                {
                    // The same targets Folia gives its three states: waiting is small and displaced, active pops large, passed settles.
                    float tx = state == 0 ? w.cx + Mathf.Sin(w.cy) * 28f : w.cx, ty = state == 0 ? w.cy + Mathf.Cos(w.cx) * 14f : w.cy;
                    float ts = state == 0 ? .5f * w.baseScale : state == 1 ? 1.25f * w.baseScale : w.baseScale;
                    float tr = state == 0 ? w.rot + 20f : state == 1 ? w.rot : w.rot + w.passedRot;
                    Spring(ref w.s, ref w.vs, ts, dt); Spring(ref w.x, ref w.vx, tx, dt); Spring(ref w.y, ref w.vy, ty, dt); Spring(ref w.r, ref w.vr, tr, dt);
                    float targetOpacity = state == 0 ? 0f : state == 1 ? 1f : .82f;
                    w.o = Mathf.Lerp(w.o, targetOpacity, 1 - Mathf.Exp(-dt * (state == 1 ? 18f : 6f)));
                    w.tone = Mathf.Lerp(w.tone, state == 1 ? 1f : 0f, 1 - Mathf.Exp(-dt * (state == 1 ? 14f : 2.5f)));
                }
                w.body.style.opacity = w.o;
                w.body.style.scale = new Scale(new Vector3(w.s, w.s, 1));
                w.body.style.translate = new Translate(w.x, w.y);
                w.body.style.rotate = new Rotate(new Angle(w.r, AngleUnit.Degree));
                w.label.style.color = Color.Lerp(baseColor, activeColor, w.tone);
                w.label.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = 12f * w.tone, color = new Color(accent.r, accent.g, accent.b, .6f * w.tone) };
            }
            // The whole line breathes a little more when the music is louder.
            line.style.translate = calm ? new Translate(0, 0) : new Translate(0, Mathf.Sin(Time.realtimeSinceStartup * .9f) * (2f + 5f * Mathf.Clamp01(energy)));
        }

        static void Spring(ref float value, ref float velocity, float target, float dt)
        {
            if (dt <= 0) return;
            const float stiffness = 200f, damping = 20f;
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

        void Rebuild(MusicTimedDocument document, int active, bool estimate, float fontScale, bool showTranslation, bool calm)
        {
            shownDocument = document; shownLine = active; shownScale = fontScale; shownEstimate = estimate; shownTranslation = showTranslation; shownCalm = calm;
            line.Clear(); words.Clear();
            string Plain(int index) => document != null && index >= 0 && index < document.lines.Count ? document.lines[index].text : "";
            previous.text = Plain(active - 1); upcoming.text = Plain(active < 0 ? 0 : active + 1);
            translation.text = ""; translation.style.display = DisplayStyle.None;
            if (document == null || active < 0 || active >= document.lines.Count) return;
            var data = document.lines[active];
            if (string.IsNullOrWhiteSpace(data.text)) return;

            List<MusicTimedWord> source = data.words.Count > 0 ? data.words : null;
            if (source == null && estimate) source = MusicLyricEstimate.Words(data, active + 1 < document.lines.Count ? document.lines[active + 1].start : double.PositiveInfinity);
            if (source == null || source.Count == 0)
                source = new List<MusicTimedWord> { new MusicTimedWord { text = data.text, start = data.start, end = double.IsInfinity(data.end) ? data.start + 4 : data.end } };

            int length = Mathf.Max(1, data.text.Length), seed = active * 7919 + length * 131;
            float size = (length <= 12 ? 46f : length <= 22 ? 40f : length <= 34 ? 34f : 28f) * fontScale;
            for (int i = 0; i < source.Count; i++)
            {
                var word = source[i];
                var stageWord = new StageWord { word = word, baseScale = 1f + Random(seed + i, 4) * .15f };
                stageWord.cx = (Random(seed + i, 1) - .5f) * 24f; stageWord.cy = (Random(seed + i, 2) - .5f) * 16f;
                stageWord.rot = calm ? 0 : (Random(seed + i, 3) - .5f) * 10f; stageWord.passedRot = calm ? 0 : (Random(seed + i, 8) - .5f) * 20f;
                stageWord.body = new VisualElement { pickingMode = PickingMode.Ignore }; stageWord.body.AddToClassList("music-stage-word");
                stageWord.body.style.marginRight = Mathf.Max(3f, size * .06f);
                stageWord.label = new Label(word.text) { pickingMode = PickingMode.Ignore }; stageWord.label.AddToClassList("music-stage-word-text");
                stageWord.label.style.fontSize = size;
                stageWord.body.Add(stageWord.label); line.Add(stageWord.body);
                // Start in the waiting pose so the first frame does not flash.
                stageWord.s = .5f * stageWord.baseScale; stageWord.o = 0; stageWord.r = stageWord.rot + 20f;
                stageWord.x = stageWord.cx + Mathf.Sin(stageWord.cy) * 28f; stageWord.y = stageWord.cy + Mathf.Cos(stageWord.cx) * 14f;
                words.Add(stageWord);
            }
            if (showTranslation && !string.IsNullOrEmpty(data.translation)) { translation.text = data.translation; translation.style.display = DisplayStyle.Flex; }
        }
    }
}
