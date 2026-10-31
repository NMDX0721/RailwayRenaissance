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
        readonly Label previous = new Label(), upcoming = new Label(), translation = new Label();
        IStageStyle style;
        MusicTimedDocument shownDocument; int shownLine = -2; bool shownTranslation;

        public string StyleId => style?.Id;

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
            if (style != null && style.Id == id) return;
            style?.Root.RemoveFromHierarchy();
            style = StageStyles.Create(id);
            Clear();
            Add(previous); Add(style.Root); Add(translation); Add(upcoming);
            shownDocument = null;   // the subtitle layer is refreshed on the next Tick, which also applies OwnsSubtitles
        }

        /// <summary>Settings changed: rebuild the current line.</summary>
        public void Invalidate() => style?.Invalidate();

        public void Tick(MusicTimedDocument document, int active, double clock, float dt, bool calm, bool estimate, float fontScale,
            Color accent, float energy, bool showTranslation, StageTuning tuning, float low, float mid, float high)
        {
            if (!ReferenceEquals(document, shownDocument) || active != shownLine || showTranslation != shownTranslation) RefreshSubtitles(document, active, showTranslation);
            style.Tick(new StageContext
            {
                document = document, active = active, clock = clock, dt = dt, calm = calm, estimate = estimate, showTranslation = showTranslation,
                fontScale = fontScale, energy = energy, low = low, mid = mid, high = high, accent = accent, tuning = tuning
            });
        }

        void RefreshSubtitles(MusicTimedDocument document, int active, bool showTranslation)
        {
            shownDocument = document; shownLine = active; shownTranslation = showTranslation;
            // Some styles draw their own neighbouring lines and translation; the shared layer stays out of their way.
            var own = style.OwnsSubtitles ? DisplayStyle.None : DisplayStyle.Flex;
            previous.style.display = own; upcoming.style.display = own;
            if (style.OwnsSubtitles) { translation.style.display = DisplayStyle.None; return; }
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
