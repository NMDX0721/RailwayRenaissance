using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public partial class DialogueBox
{
    private sealed class SentenceView
    {
        public VisualElement Group;
        public Label Primary;
        public Label Secondary;
        public string Wrapped;
        public int Offset, FontSize;
        public float Width, PrimaryHeight, SecondaryHeight;
    }
    private readonly List<SentenceView> sentenceViews = new List<SentenceView>();
    private ScrollView sentenceScroll;
    private int revealedSentence = -1;

    private bool ShowSentencePairs(VNLocalizedLine line, bool narration)
    {
        sentenceViews.Clear();revealedSentence = -1;
        sentenceScroll?.RemoveFromHierarchy();sentenceScroll = null;
        var pairs = line.Sentences();
        dialogueText.style.display = pairs.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        textAreaField.style.justifyContent = Justify.FlexStart;
        if (pairs.Count == 0) return false;

        sentenceScroll = new ScrollView { name = "subtitle-sentences" };
        sentenceScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        sentenceScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        sentenceScroll.style.flexShrink = 0;
        sentenceScroll.RegisterCallback<PointerDownEvent>(_ =>
        {
            if (sentenceScroll != null && sentenceScroll.verticalScroller.highValue > 0)
                VNManager.Instance?.SuppressNextAdvance();
        });
        textAreaField.Add(sentenceScroll);
        sentenceScroll.contentContainer.RegisterCallback<GeometryChangedEvent>(evt => FitSentencePanel(evt.newRect.height));
        float available = Mathf.Max(120, AvailableTextWidth() - 24);
        float totalHeight = 0;
        var master = new StringBuilder();
        bool thought = !string.IsNullOrEmpty(lastText) && lastText.StartsWith("[") && lastText.EndsWith("]");
        bool primaryChinese = VNSubtitleSettings.PrimaryLanguage == VNSubtitleSettings.Chinese || line.Text(VNSubtitleSettings.PrimaryLanguage) == null;
        const int mainSize = 32;
        for (int i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            var group = new VisualElement { name = "subtitle-sentence-" + i };
            group.style.position = Position.Relative;group.style.flexShrink = 0;
            sentenceScroll.Add(group);
            var sub = new Label(pair.Secondary) { name = "subtitle-sentence-secondary-" + i, pickingMode = PickingMode.Ignore };
            sub.enableRichText = false;
            sub.style.fontSize = VNSubtitleSettings.SecondaryFontSize;
            sub.style.color = new Color(0.90f, 0.78f, 0.54f, 1f);
            sub.style.unityFontDefinition = GetFontDef();sub.style.whiteSpace = WhiteSpace.Normal;
            sub.style.unityTextAlign = TextAnchor.UpperCenter;
            sub.style.marginLeft = sub.style.marginRight = 0;
            sub.style.paddingLeft = sub.style.paddingRight = 0;
            sub.style.marginBottom = 4;sub.style.flexShrink = 0;
            group.Add(sub);
            string primary = pair.Primary;
            if (thought) primary = "<color=#7EC8E3><i>" + primary.Trim('[', ']') + "</i></color>";
            else if (primaryChinese) primary = TermHighlightSystem.HighlightTerms(primary);
            if (!narration)
            {
                if (i == 0) primary = "「" + primary;
                if (i == pairs.Count - 1) primary += "」";
            }
            var main = new Label { name = "subtitle-sentence-primary-" + i, pickingMode = PickingMode.Ignore };
            main.style.fontSize = mainSize;main.style.color = new Color(1f, 0.98f, 0.94f);
            main.style.unityFontDefinition = GetFontDef();main.style.whiteSpace = WhiteSpace.Normal;
            main.style.marginLeft = main.style.marginRight = 0;
            main.style.paddingLeft = main.style.paddingRight = 0;
            group.Add(main);
            float preferredMain = main.MeasureTextSize(primary, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            float width = Mathf.Min(available, Mathf.Max(20, preferredMain));
            group.style.width = width;
            string wrapped = primary.Replace("——", "──");
            if (preferredMain > available) wrapped = WrapText(wrapped, mainSize, available, false);
            int rows = wrapped.Split('\n').Length;
            float height = rows * mainSize + (rows - 1) * LineGap;
            height = Mathf.Max(height, main.MeasureTextSize(wrapped, width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined).y);
            main.style.unityParagraphSpacing = LineGap;main.style.minHeight = height;main.style.flexShrink = 0;
            float subHeight = sub.MeasureTextSize(pair.Secondary, width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined).y;
            sentenceViews.Add(new SentenceView { Group = group, Primary = main, Secondary = sub, Wrapped = wrapped,
                Offset = master.Length, FontSize = mainSize, Width = width, PrimaryHeight = height, SecondaryHeight = subHeight });
            master.Append(wrapped);
            if (i + 1 < pairs.Count) master.Append('\n');
        }
        var rowViews = new List<SentenceView>();
        float rowWidth = 0;
        foreach (var view in sentenceViews)
        {
            if (rowViews.Count > 0 && rowWidth + view.Width > available)
            {
                totalHeight += AddSentenceRow(rowViews);rowViews.Clear();rowWidth = 0;
            }
            rowViews.Add(view);rowWidth += view.Width;
        }
        if (rowViews.Count > 0) totalHeight += AddSentenceRow(rowViews);
        float rootHeight = rootVisual.contentRect.height > 0 ? rootVisual.contentRect.height : 800;
        sentenceScroll.style.height = Mathf.Min(totalHeight, rootHeight * 0.48f);
        dialogueBox.style.height = Mathf.Max(160, Mathf.Min(totalHeight, rootHeight * 0.48f) + 24);
        speakerName.style.top = 0;speakerName.style.bottom = 0;speakerName.style.height = StyleKeyword.Auto;
        _lastWrapped = master.ToString();_lastFs = mainSize;
        typewriterEffect.Initialize(dialogueText);
        typewriterEffect.StartTypewriter(_lastWrapped, () =>
        {
            SyncSentenceText(_lastWrapped);
            sentenceScroll.scrollOffset = Vector2.zero;
            BuildTermHits();StartContinueAnimation();OnTypewriterComplete?.Invoke();
        });
        return true;
    }

    private float AddSentenceRow(List<SentenceView> views)
    {
        var row = new VisualElement { name = "subtitle-flow-line" };
        row.style.flexDirection = FlexDirection.Row;row.style.alignItems = Align.FlexStart;
        row.style.flexShrink = 0;row.style.marginBottom = 8;
        float subHeight = 0, primaryHeight = 0;
        foreach (var view in views) { subHeight = Mathf.Max(subHeight, view.SecondaryHeight); primaryHeight = Mathf.Max(primaryHeight, view.PrimaryHeight); }
        for (int i = 0; i < views.Count; i++)
        {
            views[i].Secondary.style.minHeight = subHeight;
            views[i].Group.style.marginRight = 0;
            row.Add(views[i].Group);
        }
        sentenceScroll.Add(row);
        return subHeight + primaryHeight + 4 + 8 + 12;
    }

    private void FitSentencePanel(float contentHeight)
    {
        if (sentenceScroll == null || contentHeight <= 0) return;
        float rootHeight = rootVisual.contentRect.height > 0 ? rootVisual.contentRect.height : 800;
        float height = Mathf.Min(contentHeight, rootHeight * 0.48f);
        sentenceScroll.style.height = height;
        dialogueBox.style.height = Mathf.Max(160, height + 24);
    }

    private void SyncSentenceText(string text)
    {
        if (sentenceViews.Count == 0) return;
        for (int i = 0; i < sentenceViews.Count; i++)
        {
            var view = sentenceViews[i];
            int count = Mathf.Clamp(text.Length - view.Offset, 0, view.Wrapped.Length);
            view.Primary.text = count == view.Wrapped.Length ? view.Wrapped : count == 0 ? "" : text.Substring(view.Offset, count);
            if (count > 0 && count < view.Wrapped.Length && revealedSentence != i)
            {
                revealedSentence = i;
                sentenceScroll.ScrollTo(view.Group);
            }
        }
    }
}
