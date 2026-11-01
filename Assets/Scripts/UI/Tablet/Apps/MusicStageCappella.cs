using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage style "Cappella" (清唱), after Folia's chat style. The song plays out as a conversation: the title is the first message and every
    /// lyric line is a message from a sender on the left or the right. The senders follow a fixed pattern per song (a short line often
    /// stays with the same sender, and every few messages the right-hand sender, "you", speaks), so the same song always reads the same.
    /// A message appears when its line starts; its text fades in character by character as it is sung and the bubble widens with it.
    /// The line being sung is larger and brighter, earlier lines shrink and dim, and a timestamp shows once the line is complete.
    /// Older messages scroll off the top when the stage is full. No emoji replies: the project has no emoji art. Independent implementation.
    /// </summary>
    public sealed class MusicStageCappella : IStageStyle
    {
        readonly HashSet<VisualElement> shownHolders = new HashSet<VisualElement>();

        sealed class Piece { public VisualElement unit; public Label label; public float start, end; }

        sealed class Message
        {
            public int lineIndex = -1;                  // -1 for the title
            public bool right; public int avatar; public string text, translation;
            public double start, lastEnd;
            public VisualElement row, bubble, flow, avatarDisc; public Label stamp, translationLabel;
            public readonly List<Piece> pieces = new List<Piece>();
            public int state = -1;                      // 0 passed, 1 active
            public float scale = 1f, opacity = 1f;
        }

        // Folia's motion values per intensity (calm, normal, chaotic).
        struct Motion { public float enterY, enterScale, enterDuration, activeScale, passedScale, passedOpacity, activeFont, inactiveFont, activePadX, activePadY, inactivePadX, inactivePadY; }
        static Motion MotionFor(int intensity) => intensity == 0
            ? new Motion { enterY = 14, enterScale = .992f, enterDuration = .28f, activeScale = 1.07f, passedScale = .96f, passedOpacity = .88f, activeFont = 1.22f, inactiveFont = .96f, activePadX = 18, activePadY = 14, inactivePadX = 16, inactivePadY = 12 }
            : intensity == 2
                ? new Motion { enterY = 30, enterScale = .968f, enterDuration = .38f, activeScale = 1.18f, passedScale = .88f, passedOpacity = .76f, activeFont = 1.4f, inactiveFont = .92f, activePadX = 22, activePadY = 17, inactivePadX = 15, inactivePadY = 11 }
                : new Motion { enterY = 22, enterScale = .98f, enterDuration = .32f, activeScale = 1.12f, passedScale = .92f, passedOpacity = .82f, activeFont = 1.34f, inactiveFont = .94f, activePadX = 20, activePadY = 16, inactivePadX = 16, inactivePadY = 12 };

        const int ShortLineChars = 12;
        const float FadeSeconds = .22f, WidthLookahead = .2f, Gap = 12f;
        static readonly int[] LeftAvatars = { 0, 3, 6, 1, 4 };

        readonly VisualElement root = new VisualElement(), list = new VisualElement();
        readonly List<Message> messages = new List<Message>();
        MusicTimedDocument shownDocument; string shownTitle; float shownScale, shownWidth; bool shownEstimate, shownCalm, shownTranslation; StageTuning shownTuning;

        public string Id => "cappella";
        public string Title => "清唱";
        public bool OwnsSubtitles => true;
        public VisualElement Root => root;

        public MusicStageCappella()
        {
            root.AddToClassList("music-stage-cappella"); root.pickingMode = PickingMode.Ignore;
            list.AddToClassList("music-stage-cappella-list"); list.pickingMode = PickingMode.Ignore;
            root.Add(list);
        }

        public void Invalidate() { shownDocument = null; }

        static uint Hash(string text) { uint h = 2166136261; foreach (char ch in text) { h ^= ch; h *= 16777619; } return h; }
        static float Unit(params object[] parts) => Hash(string.Join("|", parts)) / (float)uint.MaxValue;
        static string Stamp(double seconds) { int total = Mathf.Max(0, (int)seconds); return (total / 60) + ":" + (total % 60).ToString("D2"); }

        public void Tick(in StageContext c)
        {
            var frame = root.parent ?? root;
            float width = float.IsNaN(frame.resolvedStyle.width) ? 0 : frame.resolvedStyle.width, height = float.IsNaN(frame.resolvedStyle.height) ? 0 : frame.resolvedStyle.height;
            root.style.height = Mathf.Max(260f, (height < 120 ? 460f : height) * .9f);
            if (!ReferenceEquals(c.document, shownDocument) || c.title != shownTitle || Mathf.Abs(c.fontScale - shownScale) > .01f || Mathf.Abs(width - shownWidth) > 24f ||
                c.estimate != shownEstimate || c.calm != shownCalm || c.showTranslation != shownTranslation || !c.tuning.Equals(shownTuning))
                Rebuild(c, width);
            var document = c.document; if (document == null) return;
            var motion = MotionFor(c.tuning.intensity);
            float dt = Mathf.Clamp(c.dt, 0, 1f / 20f), baseFont = Mathf.Clamp(18f * c.fontScale, 15f, 26f) * 1.15f;

            // The newest message that has started; the title shows from the start.
            int visibleLine = -1; for (int i = document.lines.Count - 1; i >= 0; i--) if (c.clock >= document.lines[i].start) { visibleLine = i; break; }
            int newest = visibleLine + 1;                  // messages[0] is the title
            Color cream = new Color(.93f, .9f, .8f), dark = new Color(.07f, .08f, .09f);
            for (int index = 0; index < messages.Count; index++)
            {
                var m = messages[index];
                bool started = index <= newest;
                if (!started) { m.row.style.display = DisplayStyle.None; continue; }
                m.row.style.display = DisplayStyle.Flex;
                int state = m.lineIndex < 0 ? (visibleLine < 0 ? 1 : 0) : (m.lineIndex == c.active ? 1 : 0);
                if (state != m.state) ApplyState(m, state, motion, baseFont);
                // Entering: a short rise and fade from when the line starts; earlier messages are already settled.
                float enter = m.lineIndex < 0 ? 1f : c.calm ? 1f : Mathf.Clamp01((float)((c.clock - m.start + .05) / motion.enterDuration));
                float ease = 1 - (1 - enter) * (1 - enter);
                float targetScale = state == 1 ? motion.activeScale : motion.passedScale, targetOpacity = state == 1 ? 1f : motion.passedOpacity;
                m.scale = c.calm ? 1f : Mathf.Lerp(m.scale, targetScale, 1 - Mathf.Exp(-dt * 10f)); m.opacity = Mathf.Lerp(m.opacity, targetOpacity, 1 - Mathf.Exp(-dt * 8f));
                m.row.style.opacity = m.opacity * ease;
                m.row.style.translate = new Translate(0, c.calm ? 0 : (1 - ease) * motion.enterY);
                m.bubble.style.scale = new Scale(new Vector3(m.scale, m.scale, 1));
                // Characters: shown from just before they are sung (so the bubble widens first) and faded in over a fifth of a second.
                shownHolders.Clear();
                foreach (var piece in m.pieces)
                {
                    bool show = m.lineIndex < 0 || c.clock >= piece.start - WidthLookahead;
                    // Pieces of one word group share a holder: it is shown as soon as its first piece is, and a later piece must not hide it again.
                    if (show) { piece.unit.style.display = DisplayStyle.Flex; shownHolders.Add(piece.unit); }
                    else if (!shownHolders.Contains(piece.unit)) piece.unit.style.display = DisplayStyle.None;
                    if (!show) continue;
                    piece.label.style.opacity = m.lineIndex < 0 ? 1f : Mathf.Clamp01((float)((c.clock - piece.start) / FadeSeconds));
                }
                bool done = m.lineIndex < 0 || c.clock >= m.lastEnd + FadeSeconds;
                m.stamp.style.display = done ? DisplayStyle.Flex : DisplayStyle.None;
            }
            FitToStage(newest, height < 120 ? 460f : height);
        }

        // Drop the oldest messages until the rest fit the stage, always keeping the newest two.
        void FitToStage(int newest, float stageHeight)
        {
            float available = Mathf.Max(220f, stageHeight * .9f - 24f), used = 0; int kept = 0;
            for (int i = Mathf.Min(newest, messages.Count - 1); i >= 0; i--)
            {
                var m = messages[i]; float h = m.row.resolvedStyle.height; if (float.IsNaN(h) || h <= 0) h = 60f;
                used += h + Gap;
                if (used > available && kept >= 2) { m.row.style.display = DisplayStyle.None; continue; }
                kept++;
            }
        }

        void ApplyState(Message m, int state, Motion motion, float baseFont)
        {
            m.state = state; bool active = state == 1;
            float font = baseFont * (active ? motion.activeFont : motion.inactiveFont);
            m.bubble.style.paddingLeft = m.bubble.style.paddingRight = active ? motion.activePadX : motion.inactivePadX;
            m.bubble.style.paddingTop = m.bubble.style.paddingBottom = active ? motion.activePadY : motion.inactivePadY;
            foreach (var piece in m.pieces) piece.label.style.fontSize = font;
            m.stamp.style.fontSize = Mathf.Max(11f, font * .5f);
            if (m.translationLabel != null) m.translationLabel.style.fontSize = Mathf.Max(12f, font * .62f);
        }

        void Rebuild(in StageContext c, float width)
        {
            shownDocument = c.document; shownTitle = c.title; shownScale = c.fontScale; shownWidth = width; shownEstimate = c.estimate; shownCalm = c.calm; shownTranslation = c.showTranslation; shownTuning = c.tuning;
            list.Clear(); messages.Clear();
            var document = c.document; if (document == null) return;
            int intensity = c.tuning.intensity;
            // Sender pattern per intensity: how often "you" speak, how often a short line keeps its sender, and the left/right sequence.
            int forceRightEvery = intensity == 0 ? 7 : intensity == 2 ? 3 : 5;
            float carryChance = intensity == 0 ? .92f : intensity == 2 ? .36f : .68f, flipChance = intensity == 0 ? .08f : intensity == 2 ? .42f : .18f;
            bool[] sequence = intensity == 0 ? new[] { false, false, true, false, true } : intensity == 2 ? new[] { false, true, true, false, true, false } : new[] { false, true, false, true, true };  // true = right

            string title = string.IsNullOrWhiteSpace(c.title) ? "沿线声音" : c.title.Trim();
            messages.Add(new Message { lineIndex = -1, right = true, avatar = 8, text = title, start = 0, lastEnd = 0 });
            int cursor = 0, leftCursor = 0; bool haveLast = false, lastRight = false; int lastAvatar = 0;
            for (int i = 0; i < document.lines.Count; i++)
            {
                var line = document.lines[i];
                bool blank = string.IsNullOrWhiteSpace(line.text);
                int chars = 0; foreach (char ch in line.text ?? "") if (!char.IsWhiteSpace(ch)) chars++;
                bool forceRight = (i + 1) % forceRightEvery == 0;
                bool carry = !forceRight && chars <= ShortLineChars && haveLast && Unit("carry", line.start, i) <= carryChance;
                bool baseRight = sequence[cursor % sequence.Length];
                bool flip = !forceRight && Unit("flip", line.start, i) < flipChance;
                bool right = forceRight ? true : flip ? !baseRight : baseRight;
                int avatar;
                if (forceRight) avatar = 8;
                else if (carry) { right = lastRight; avatar = lastAvatar; }
                else avatar = right ? 8 : LeftAvatars[leftCursor % LeftAvatars.Length];
                var m = new Message { lineIndex = i, right = right, avatar = avatar, text = blank ? "......" : line.text, translation = c.showTranslation ? line.translation : null, start = line.start };
                messages.Add(m);
                if (forceRight) { cursor = 0; haveLast = false; }
                else if (!carry) { if (!right) leftCursor++; cursor++; haveLast = true; lastRight = right; lastAvatar = avatar; }
            }
            foreach (var m in messages) Build(m, document, c);
        }

        void Build(Message m, MusicTimedDocument document, in StageContext c)
        {
            m.row = new VisualElement { pickingMode = PickingMode.Ignore }; m.row.AddToClassList("music-stage-cappella-row"); if (m.right) m.row.AddToClassList("music-stage-cappella-right");
            m.row.style.display = DisplayStyle.None;
            // Avatar: a disc with a head and shoulders, tinted by the sender; the right-hand sender uses the accent colour.
            m.avatarDisc = new VisualElement { pickingMode = PickingMode.Ignore }; m.avatarDisc.AddToClassList("music-stage-cappella-avatar");
            Color tone = m.right ? Color.Lerp(c.accent, Color.white, .25f) : Color.HSVToRGB((m.avatar * .137f) % 1f, .38f, .74f);
            m.avatarDisc.style.backgroundColor = tone;
            var shoulders = new VisualElement { pickingMode = PickingMode.Ignore }; shoulders.AddToClassList("music-stage-cappella-shoulders"); shoulders.style.backgroundColor = Color.Lerp(tone, Color.black, .35f);
            var head = new VisualElement { pickingMode = PickingMode.Ignore }; head.AddToClassList("music-stage-cappella-head"); head.style.backgroundColor = Color.Lerp(tone, Color.white, .5f);
            m.avatarDisc.Add(shoulders); m.avatarDisc.Add(head);
            m.bubble = new VisualElement { pickingMode = PickingMode.Ignore }; m.bubble.AddToClassList("music-stage-cappella-bubble"); m.bubble.AddToClassList(m.right ? "music-stage-cappella-bubble-right" : "music-stage-cappella-bubble-left");
            // Right bubbles take the accent colour with dark text; left bubbles are a dark tone tinted by the sender, with cream text.
            Color text;
            if (m.right) { m.bubble.style.backgroundColor = new Color(Mathf.Lerp(c.accent.r, .93f, .18f), Mathf.Lerp(c.accent.g, .9f, .18f), Mathf.Lerp(c.accent.b, .8f, .18f), .94f); text = new Color(.07f, .08f, .09f); }
            else { float mix = .18f + (m.avatar % 9) / 8f * .62f; m.bubble.style.backgroundColor = Color.Lerp(new Color(.2f, .23f, .25f), c.accent, mix * .5f); text = new Color(.93f, .9f, .8f); }
            m.flow = new VisualElement { pickingMode = PickingMode.Ignore }; m.flow.AddToClassList("music-stage-cappella-flow");

            // The text as display units (so a Latin word never breaks in the middle), each with a label per character that fades in on its own time.
            MusicTimedLine data = m.lineIndex >= 0 ? document.lines[m.lineIndex] : null;
            List<LyricUnit> units;
            if (data == null || string.IsNullOrWhiteSpace(data.text))
            {
                units = new List<LyricUnit>(); var unit = new LyricUnit(); unit.glyphs.Add(new LyricGlyph { text = m.text, start = m.start, end = m.start + .4 }); units.Add(unit);
            }
            else
            {
                double nextStart = m.lineIndex + 1 < document.lines.Count ? document.lines[m.lineIndex + 1].start : double.PositiveInfinity;
                var glyphs = MusicLyricUnits.Timeline(data, nextStart, c.estimate, out _);
                if (glyphs.Count == 0) glyphs.Add(new LyricGlyph { text = data.text, start = data.start, end = double.IsInfinity(nextStart) ? data.start + 4 : nextStart });
                units = c.tuning.legacyLayout ? MusicLyricUnits.GroupLegacy(glyphs) : MusicLyricUnits.Group(glyphs);
            }
            if (m.lineIndex < 0)
            {
                units = new List<LyricUnit>(); var unit = new LyricUnit(); unit.glyphs.Add(new LyricGlyph { text = m.text, start = 0, end = 0 }); units.Add(unit);
            }
            foreach (var unit in units)
            {
                var holder = new VisualElement { pickingMode = PickingMode.Ignore }; holder.AddToClassList("music-stage-cappella-unit");
                foreach (var glyph in unit.glyphs)
                {
                    var label = new Label(glyph.text.Replace(' ', ' ')) { pickingMode = PickingMode.Ignore, enableRichText = false }; label.AddToClassList("music-stage-cappella-char");
                    label.style.color = text; label.style.opacity = 0; StageFonts.Apply(label);
                    holder.Add(label); m.pieces.Add(new Piece { unit = holder, label = label, start = (float)glyph.start, end = (float)glyph.end });
                    m.lastEnd = Math.Max(m.lastEnd, glyph.end);
                }
                m.flow.Add(holder);
            }
            m.bubble.Add(m.flow);
            if (!string.IsNullOrEmpty(m.translation))
            {
                m.translationLabel = new Label(m.translation) { pickingMode = PickingMode.Ignore, enableRichText = false }; m.translationLabel.AddToClassList("music-stage-cappella-translation");
                m.translationLabel.style.color = new Color(text.r, text.g, text.b, .75f); StageFonts.Apply(m.translationLabel); m.bubble.Add(m.translationLabel);
            }
            m.stamp = new Label(Stamp(m.start)) { pickingMode = PickingMode.Ignore, enableRichText = false }; m.stamp.AddToClassList("music-stage-cappella-stamp");
            m.stamp.style.color = new Color(text.r, text.g, text.b, .55f); m.stamp.style.display = DisplayStyle.None; m.bubble.Add(m.stamp);
            m.row.Add(m.avatarDisc); m.row.Add(m.bubble); list.Add(m.row);
        }
    }
}
