using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>How soon the words of a line are revealed: fast lines do not wait for every word.</summary>
    public enum StageReveal { Normal, Fast, Instant }

    /// <summary>
    /// One display unit on a stage ("手を", "unbelievable"): a body holding a glow layer and the characters, with the waiting / active / passed
    /// poses Folia's classic word has. Stage styles share it so every style animates a word the same way and differs only in layout.
    /// </summary>
    public sealed class StageWord
    {
        public VisualElement body, ripple;
        public Label glow, bloom, ghost;   // bloom: a wide soft halo behind the glow; ghost: an afterimage that grows and fades when the word lights
        public readonly List<Label> labels = new List<Label>();
        public readonly List<float> tones = new List<float>();
        public LyricUnit unit;
        public float baseScale, cx, cy, rot, passedRot, rippleScale;
        public float x, y, s = .5f, r, o, vx, vy, vs, vr, rippleTime = 1, glowTone, ghostTime = 1;
        public int state = -1;

        public static float Random(int seed, float offset)
        {
            float x = Mathf.Sin(seed * 12.9898f + offset * 78.233f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }

        /// <param name="spread">scatter in px; <paramref name="tilt"/> resting tilt in degrees; <paramref name="scaleBase"/> and <paramref name="scaleRange"/> pick the resting scale.</param>
        public static StageWord Create(LyricUnit unit, float size, Color accent, bool chorus, int seed, float spread, float tilt, float scaleBase, float scaleRange,
            bool rotation, bool calm, bool chaotic)
        {
            var w = new StageWord { unit = unit };
            w.baseScale = scaleBase + Random(seed, 4) * scaleRange;
            w.cx = (Random(seed, 1) - .5f) * spread * 2f; w.cy = (Random(seed, 2) - .5f) * spread * 2f;
            w.rot = rotation ? (Random(seed, 3) - .5f) * tilt * 2f : 0; w.passedRot = rotation ? (Random(seed, 8) - .5f) * 45f : 0;
            if (calm) { w.baseScale = 1; w.cx = w.cy = w.rot = w.passedRot = 0; }
            w.rippleScale = 1.5f + Random(seed, 9) * 2f;
            w.body = new VisualElement { pickingMode = PickingMode.Ignore }; w.body.AddToClassList("music-stage-word");
            w.body.style.marginRight = Mathf.Max(4f, size * .2f);
            if (chaotic && Random(seed, 6) > .7f) w.body.style.alignSelf = Random(seed, 7) > .5f ? Align.FlexStart : Align.FlexEnd;
            // The glow layer comes first so it sits behind the text.
            string text = ""; foreach (var glyph in unit.glyphs) text += glyph.text;
            w.glow = new Label(text) { pickingMode = PickingMode.Ignore, enableRichText = false }; w.glow.AddToClassList("music-stage-word-text"); w.glow.AddToClassList("music-stage-glow");
            w.glow.style.fontSize = size; w.glow.style.color = new Color(accent.r, accent.g, accent.b, .9f); w.glow.style.opacity = 0; StageFonts.Apply(w.glow);
            w.bloom = new Label(text) { pickingMode = PickingMode.Ignore, enableRichText = false }; w.bloom.AddToClassList("music-stage-word-text"); w.bloom.AddToClassList("music-stage-glow");
            w.bloom.style.fontSize = size; w.bloom.style.color = new Color(accent.r, accent.g, accent.b, .55f); w.bloom.style.opacity = 0; StageFonts.Apply(w.bloom);
            w.ghost = new Label(text) { pickingMode = PickingMode.Ignore, enableRichText = false }; w.ghost.AddToClassList("music-stage-word-text"); w.ghost.AddToClassList("music-stage-glow");
            w.ghost.style.fontSize = size; w.ghost.style.color = new Color(accent.r, accent.g, accent.b, .9f); w.ghost.style.opacity = 0; StageFonts.Apply(w.ghost);
            w.body.Add(w.bloom); w.body.Add(w.glow); w.body.Add(w.ghost);
            if (chorus)
            {
                w.ripple = new VisualElement { pickingMode = PickingMode.Ignore }; w.ripple.AddToClassList("music-stage-ripple");
                w.ripple.style.opacity = 0; w.body.Add(w.ripple);
            }
            foreach (var glyph in unit.glyphs)
            {
                var label = new Label(glyph.text) { pickingMode = PickingMode.Ignore, enableRichText = false }; label.AddToClassList("music-stage-word-text");
                label.style.fontSize = size; StageFonts.Apply(label); w.body.Add(label); w.labels.Add(label); w.tones.Add(0);
            }
            // Start in the waiting pose so the first frame does not flash.
            w.s = .5f; w.o = 0; w.r = w.rot + (rotation ? 20f : 0);
            w.x = w.cx + Mathf.Sin(w.cy) * 100f; w.y = w.cy + Mathf.Cos(w.cx) * 50f;
            return w;
        }

        public static StageReveal RevealFor(List<LyricUnit> units)
        {
            double span = Math.Max(.1, units[units.Count - 1].end - units[0].start), perUnit = span / units.Count;
            return perUnit < .10 ? StageReveal.Instant : perUnit < .18 ? StageReveal.Fast : StageReveal.Normal;
        }

        /// <summary>Which pose the word is in at <paramref name="clock"/>: 0 waiting, 1 active, 2 passed.</summary>
        public static int StateAt(LyricUnit unit, double clock, StageReveal reveal, double lineEnd)
        {
            double lookahead = reveal == StageReveal.Instant ? .03 : reveal == StageReveal.Fast ? .08 : .15;
            double end = reveal == StageReveal.Instant ? (double.IsInfinity(lineEnd) ? unit.end : lineEnd)
                : reveal == StageReveal.Fast ? Math.Min(double.IsInfinity(lineEnd) ? unit.end : lineEnd, Math.Max(unit.end, unit.start + .12)) : unit.end;
            return clock > end ? 2 : clock >= unit.start - lookahead ? 1 : 0;
        }

        public static void Spring(ref float value, ref float velocity, float target, float dt, float stiffness, float damping)
        {
            if (dt <= 0) return;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / .008f)); float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                velocity += (stiffness * (target - value) - damping * velocity) * h;
                value += velocity * h;
            }
        }

        /// <summary>Advance the word one frame. Every pose is a function of the audio clock, so pausing freezes it and seeking rebuilds it.</summary>
        public void Tick(double clock, float dt, bool calm, StageReveal reveal, double lineEnd, bool chorus, bool rotation, bool chaotic,
            Color baseColor, Color activeColor, Color accent, float high, float lineFade)
        {
            int newState = StateAt(unit, clock, reveal, lineEnd);
            if (newState == 1 && state != 1 && chorus && !calm) rippleTime = 0;
            if (newState == 1 && state != 1 && !calm) ghostTime = 0;
            state = newState;

            if (calm)
            {
                o = state == 0 ? .3f : 1f; s = 1; x = y = r = 0;
            }
            else
            {
                // Waiting is small, faded and displaced; active springs large; passed settles back with a little afterglow.
                float tx = state == 0 ? cx + Mathf.Sin(cy) * 100f : cx, ty = state == 0 ? cy + Mathf.Cos(cx) * 50f : cy;
                float ts = state == 0 ? .5f : state == 1 ? baseScale * 1.4f : baseScale;
                float tr = state == 0 ? rot + (rotation ? 20f : 0) : state == 1 ? rot : rot + passedRot;
                // Active uses the spring Folia gives it (stiffness 200, damping 20); entering and leaving settle in about half a second.
                float stiffness = state == 1 ? (reveal == StageReveal.Normal ? 200f : 300f) : 110f, damping = state == 1 ? (reveal == StageReveal.Normal ? 20f : 26f) : 21f;
                Spring(ref s, ref vs, ts, dt, stiffness, damping); Spring(ref x, ref vx, tx, dt, stiffness, damping);
                Spring(ref y, ref vy, ty, dt, stiffness, damping); Spring(ref r, ref vr, tr, dt, stiffness, damping);
                float targetOpacity = state == 0 ? 0f : state == 1 ? 1f : chaotic ? .9f : .82f;
                o = Mathf.Lerp(o, targetOpacity, 1 - Mathf.Exp(-dt * (state == 1 ? 24f : 6f)));
            }
            body.style.opacity = o * lineFade;
            body.style.scale = new Scale(new Vector3(s, s, 1));
            body.style.translate = new Translate(x, y);
            body.style.rotate = new Rotate(new Angle(r, AngleUnit.Degree));

            // Inside an active unit the characters light up one by one on their own times (Folia's grapheme timing).
            float lit = 0;
            for (int g = 0; g < labels.Count; g++)
            {
                float target = state == 1 ? (clock >= unit.glyphs[g].start ? 1f : .25f) : 0f;
                float rate = state == 1 ? 20f : state == 2 ? 2.5f : 8f;
                tones[g] = calm ? (state == 1 ? 1f : 0f) : Mathf.Lerp(tones[g], target, 1 - Mathf.Exp(-dt * rate));
                lit = Mathf.Max(lit, state == 1 && clock >= unit.glyphs[g].start ? 1f : 0f);
                labels[g].style.color = Color.Lerp(baseColor, activeColor, tones[g]);
            }
            // The glow sits on its own layer behind the text, so it can be pushed hard without making the glyphs unreadable.
            if (glow != null)
            {
                glowTone = calm ? 0 : Mathf.Lerp(glowTone, lit, 1 - Mathf.Exp(-dt * (lit > glowTone ? 16f : 3f)));
                glow.style.opacity = glowTone * lineFade;
                glow.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = 12f + 14f * Mathf.Clamp01(high), color = new Color(accent.r, accent.g, accent.b, .8f) };
            }

            if (bloom != null)
            {
                bloom.style.opacity = glowTone * lineFade * (.55f + .45f * Mathf.Clamp01(high));
                bloom.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = 34f + 26f * Mathf.Clamp01(high), color = new Color(accent.r, accent.g, accent.b, .65f) };
            }
            // The afterimage leaves the word as it lights: it grows by half and fades in about 0.7 s, drifting a little upward.
            if (ghost != null)
            {
                if (ghostTime < .7f && !calm)
                {
                    ghostTime += dt; float t = Mathf.Clamp01(ghostTime / .7f), ease = 1 - (1 - t) * (1 - t);
                    float gs = Mathf.Lerp(1f, 1.5f, ease);
                    ghost.style.scale = new Scale(new Vector3(gs, gs, 1)); ghost.style.translate = new Translate(0, -ease * 10f);
                    ghost.style.opacity = .5f * (1 - t) * lineFade;
                }
                else ghost.style.opacity = 0;
            }

            if (ripple != null)
            {
                if (rippleTime < .5f)
                {
                    rippleTime += dt; float t = Mathf.Clamp01(rippleTime / .5f), size = Mathf.Max(body.resolvedStyle.width, body.resolvedStyle.height) * 1.5f;
                    if (!float.IsNaN(size) && size > 0) { ripple.style.width = size; ripple.style.height = size; ripple.style.marginLeft = -size / 2; ripple.style.marginTop = -size / 2; }
                    float scale = Mathf.Lerp(.2f, rippleScale, 1 - (1 - t) * (1 - t));
                    ripple.style.scale = new Scale(new Vector3(scale, scale, 1)); ripple.style.opacity = .8f * (1 - t);
                    ripple.style.borderTopColor = ripple.style.borderBottomColor = ripple.style.borderLeftColor = ripple.style.borderRightColor = activeColor;
                }
                else ripple.style.opacity = 0;
            }
        }
    }
}
