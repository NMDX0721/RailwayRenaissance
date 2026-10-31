using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// Stage style "Still", after Folia's low-resource style: three static lines, the line before and after dimmed around the current one,
    /// each with its translation underneath. Nothing animates between lines, so it costs almost nothing and reads calmly.
    /// </summary>
    public sealed class MusicStageStill : IStageStyle
    {
        readonly VisualElement root = new VisualElement();
        MusicTimedDocument shownDocument; int shownIndex = -2; float shownScale; bool shownTranslation;
        int effective;

        public string Id => "still";
        public string Title => "Still";
        public bool OwnsSubtitles => true;
        public VisualElement Root => root;

        public MusicStageStill()
        {
            root.AddToClassList("music-stage-still"); root.pickingMode = PickingMode.Ignore;
        }

        public void Invalidate() { shownDocument = null; shownIndex = -3; }

        public void Tick(in StageContext c)
        {
            var document = c.document;
            // Between lines (before the first, in a long gap) the last line shown stays on stage instead of going blank.
            if (c.active >= 0) effective = c.active;
            else if (document == null || effective >= document.lines.Count) effective = 0;
            if (ReferenceEquals(document, shownDocument) && effective == shownIndex && Mathf.Abs(c.fontScale - shownScale) < .01f && c.showTranslation == shownTranslation) return;
            shownDocument = document; shownIndex = effective; shownScale = c.fontScale; shownTranslation = c.showTranslation;
            root.Clear();
            if (document == null || document.lines.Count == 0) return;
            for (int offset = -1; offset <= 1; offset++)
            {
                int index = effective + offset;
                if (index < 0 || index >= document.lines.Count) { root.Add(new VisualElement { pickingMode = PickingMode.Ignore, style = { height = 40 } }); continue; }
                var data = document.lines[index]; bool current = offset == 0;
                var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.AddToClassList("music-stage-still-row");
                if (!string.IsNullOrWhiteSpace(data.text))
                {
                    var text = new Label(data.text) { pickingMode = PickingMode.Ignore };
                    text.AddToClassList(current ? "music-stage-still-current" : "music-stage-still-side");
                    text.style.fontSize = (current ? 40f : 30f) * c.fontScale; StageFonts.Apply(text);
                    row.Add(text);
                }
                if (c.showTranslation && !string.IsNullOrEmpty(data.translation))
                {
                    var translation = new Label(data.translation) { pickingMode = PickingMode.Ignore };
                    translation.AddToClassList(current ? "music-stage-still-translation" : "music-stage-still-translation-side");
                    translation.style.fontSize = (current ? 22f : 18f) * c.fontScale; StageFonts.Apply(translation);
                    row.Add(translation);
                }
                root.Add(row);
            }
        }
    }
}
