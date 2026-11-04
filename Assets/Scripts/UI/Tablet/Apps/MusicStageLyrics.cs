using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage host. It owns what every stage style shares, the way Folia's visualizer shell does: the previous and upcoming line and the
    /// translation around the stage. The lyric animation itself belongs to the selected <see cref="IStageStyle"/>, which only sees the
    /// per-frame <see cref="StageContext"/> (audio clock, line, tuning, audio bands).
    /// </summary>
    public sealed class MusicStageLyrics : VisualElement
    {
        readonly Label previous = new Label { enableRichText = false }, upcoming = new Label { enableRichText = false }, translation = new Label { enableRichText = false };
        IStageStyle stageStyle, leaving;
        float handover = 1; bool lastCalm;   // 0..1 progress of the cross-fade between two styles; 1 when none is running
        const float HandoverSeconds = .5f;
        MusicTimedDocument shownDocument; int shownLine = -2; bool shownTranslation;

        public string StyleId => stageStyle?.Id;

        public MusicStageLyrics()
        {
            AddToClassList("music-stage-lyrics");
            previous.AddToClassList("music-stage-previous"); upcoming.AddToClassList("music-stage-upcoming"); translation.AddToClassList("music-stage-translation");
            foreach (var label in new[] { previous, upcoming, translation }) label.pickingMode = PickingMode.Ignore;
            Select(StageStyles.Default);
        }

        /// <summary>Switch the stage look. The line is rebuilt in the new style on the next Tick.</summary>
        public void Select(string id)
        {
            if (!StageStyles.Has(id)) id = StageStyles.Default;
            if (stageStyle != null && stageStyle.Id == id) return;
            // The old style stays on stage, frozen, and dissolves while the new one grows in; reduced motion cuts instead.
            EndHandover();
            var old = stageStyle;
            stageStyle = StageStyles.Create(id);
            Clear();
            Add(previous); Add(stageStyle.Root); Add(translation); Add(upcoming);
            if (old != null && !lastCalm)
            {
                leaving = old; handover = 0;
                var root = leaving.Root; root.style.position = Position.Absolute; root.style.left = 0; root.style.right = 0; root.style.top = 0; root.style.bottom = 0; root.pickingMode = PickingMode.Ignore;
                Insert(IndexOf(stageStyle.Root), root);
                ApplyHandover();
            }
            shownDocument = null;   // the subtitle layer is refreshed on the next Tick, which also applies OwnsSubtitles
        }

        void EndHandover()
        {
            if (leaving != null) leaving.Root.RemoveFromHierarchy();
            leaving = null; handover = 1;
            if (stageStyle != null) { stageStyle.Root.style.opacity = StyleKeyword.Null; stageStyle.Root.style.scale = new StyleScale(StyleKeyword.Null); }
        }

        void ApplyHandover()
        {
            float t = handover * handover * (3 - 2 * handover);
            stageStyle.Root.style.opacity = t; stageStyle.Root.style.scale = new Scale(new Vector3(.96f + .04f * t, .96f + .04f * t, 1));
            if (leaving != null) { leaving.Root.style.opacity = 1 - t; leaving.Root.style.scale = new Scale(new Vector3(1 + .05f * t, 1 + .05f * t, 1)); }
        }

        /// <summary>Settings changed: rebuild the current line.</summary>
        public void Invalidate() => stageStyle?.Invalidate();

        public void Tick(MusicTimedDocument document, int active, double clock, float dt, bool calm, bool estimate, float fontScale,
            Color accent, float energy, bool showTranslation, StageTuning tuning, float low, float mid, float high, string title = null, float chorusLevel = 0)
        {
            lastCalm = calm;
            if (leaving != null)
            {
                handover = calm ? 1 : Mathf.Min(1, handover + dt / HandoverSeconds);
                if (handover >= 1) EndHandover(); else ApplyHandover();
            }
            if (!ReferenceEquals(document, shownDocument) || active != shownLine || showTranslation != shownTranslation) RefreshSubtitles(document, active, showTranslation);
            stageStyle.Tick(new StageContext
            {
                document = document, active = active, clock = clock, dt = dt, calm = calm, estimate = estimate, showTranslation = showTranslation,
                fontScale = fontScale, energy = Mathf.Clamp01(energy + chorusLevel * .2f), low = low, mid = mid, high = Mathf.Clamp01(high + chorusLevel * .45f), accent = accent, tuning = tuning, title = title
            });
        }

        void RefreshSubtitles(MusicTimedDocument document, int active, bool showTranslation)
        {
            shownDocument = document; shownLine = active; shownTranslation = showTranslation;
            // Some styles draw their own neighbouring lines and translation; the shared layer stays out of their way.
            var own = stageStyle.OwnsSubtitles ? DisplayStyle.None : DisplayStyle.Flex;
            previous.style.display = own; upcoming.style.display = own;
            if (stageStyle.OwnsSubtitles) { translation.style.display = DisplayStyle.None; return; }
            string Plain(int index) => document != null && index >= 0 && index < document.lines.Count ? document.lines[index].text : "";
            previous.text = Plain(active - 1); upcoming.text = Plain(active < 0 ? 0 : active + 1);
            translation.text = ""; translation.style.display = DisplayStyle.None;
            if (document == null || active < 0 || active >= document.lines.Count) return;
            var data = document.lines[active];
            if (showTranslation && !string.IsNullOrEmpty(data.translation) && !string.IsNullOrWhiteSpace(data.text))
            { translation.text = data.translation; translation.style.display = DisplayStyle.Flex; }
            StageFonts.Apply(previous); StageFonts.Apply(upcoming); StageFonts.Apply(translation);
        }
    }
}
