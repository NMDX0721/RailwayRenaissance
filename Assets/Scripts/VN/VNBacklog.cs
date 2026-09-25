using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class VNBacklog : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;

    private VisualElement backlogPanel;
    private VisualElement backlogHeader;
    private ScrollView backlogScroll;
    private bool isOpen;

    private const int MaxEntries = 500;
    private readonly List<BacklogEntry> entries = new List<BacklogEntry>();

    private struct BacklogEntry
    {
        public string speaker;
        public string text;
        public int sceneIndex;
        public int dialogueIndex;
    }

    public Action<int, int> OnEntryClicked;

    public void Init(UIDocument document)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildUI();
    }

    private FontDefinition GetFontDef()
    {
        return new FontDefinition { font = gameFont };
    }

    private void BuildUI()
    {
        var root = uiDoc.rootVisualElement;
        var fontDef = GetFontDef();

        backlogPanel = new VisualElement { name = "backlog-panel" };
        backlogPanel.style.position = Position.Absolute;
        backlogPanel.style.top = 0;
        backlogPanel.style.left = 0;
        backlogPanel.style.right = 0;
        backlogPanel.style.bottom = 0;
        backlogPanel.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.85f);
        // 磨砂：半透明底 + USS backdrop-filter（引擎不支持该属性时静默忽略）
        var frosted = Resources.Load<StyleSheet>("UI/frosted");
        if (frosted != null)
        {
            backlogPanel.styleSheets.Add(frosted);
            backlogPanel.AddToClassList("frosted");
        }
        backlogPanel.style.display = DisplayStyle.None;
        backlogPanel.pickingMode = PickingMode.Position;
        // 面板内任意点击都不冒泡到 VN root（避免关历史/点条目时推进对话）
        backlogPanel.RegisterCallback<PointerDownEvent>(evt =>
        {
            evt.StopImmediatePropagation();
            VNManager.Instance?.SuppressNextAdvance();
        });
        backlogPanel.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopImmediatePropagation();
            VNManager.Instance?.SuppressNextAdvance();
        });
        root.Add(backlogPanel);

        backlogHeader = new VisualElement { name = "backlog-header" };
        backlogHeader.style.flexDirection = FlexDirection.Row;
        backlogHeader.style.justifyContent = Justify.SpaceBetween;
        backlogHeader.style.alignItems = Align.Center;
        backlogHeader.style.paddingLeft = 40;
        backlogHeader.style.paddingRight = 40;
        backlogHeader.style.paddingTop = 20;
        backlogHeader.style.paddingBottom = 20;
        backlogHeader.style.borderBottomWidth = 2;
        backlogHeader.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        backlogPanel.Add(backlogHeader);

        var titleLabel = new Label("对话回顾");
        titleLabel.style.fontSize = 32;
        titleLabel.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityFontDefinition = fontDef;
        backlogHeader.Add(titleLabel);

        var closeBtn = new UnityEngine.UIElements.Button(() => ToggleBacklog()) { text = "✕" };
        closeBtn.focusable = false;
        UiCursor.Bind(closeBtn);
        // 阻止冒泡到 VN root：否则关闭瞬间会被当成「推进下一句」
        // 并通知 VNManager 吞掉同一次点击（IsOpen 已 false 后仍可能 Bubble 到 OnRootClick）
        closeBtn.RegisterCallback<PointerDownEvent>(evt =>
        {
            evt.StopImmediatePropagation();
            VNManager.Instance?.SuppressNextAdvance();
        });
        closeBtn.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopImmediatePropagation();
            VNManager.Instance?.SuppressNextAdvance();
        });
        closeBtn.RegisterCallback<PointerUpEvent>(evt => evt.StopImmediatePropagation());
        // 40×40 金边圆角关闭按钮（深色底 + 金色 ✕）
        var closeBg = new Color(0.14f, 0.09f, 0.05f, 0.9f);
        var closeBorder = new Color(0.82f, 0.62f, 0.35f, 0.7f);
        closeBtn.style.width = 40;
        closeBtn.style.height = 40;
        closeBtn.style.borderTopLeftRadius = 8;
        closeBtn.style.borderTopRightRadius = 8;
        closeBtn.style.borderBottomLeftRadius = 8;
        closeBtn.style.borderBottomRightRadius = 8;
        closeBtn.style.backgroundColor = closeBg;
        closeBtn.style.borderTopWidth = 1;
        closeBtn.style.borderBottomWidth = 1;
        closeBtn.style.borderLeftWidth = 1;
        closeBtn.style.borderRightWidth = 1;
        closeBtn.style.borderTopColor = closeBorder;
        closeBtn.style.borderBottomColor = closeBorder;
        closeBtn.style.borderLeftColor = closeBorder;
        closeBtn.style.borderRightColor = closeBorder;
        closeBtn.style.fontSize = 20;
        closeBtn.style.color = new Color(1f, 0.86f, 0.59f, 1f);
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = fontDef;
        // hover：提亮背景与金边，移出恢复
        closeBtn.RegisterCallback<PointerEnterEvent>(evt =>
        {
            closeBtn.style.backgroundColor = new Color(0.3f, 0.18f, 0.09f, 0.95f);
            var bright = new Color(1f, 0.85f, 0.5f, 1f);
            closeBtn.style.borderTopColor = bright;
            closeBtn.style.borderBottomColor = bright;
            closeBtn.style.borderLeftColor = bright;
            closeBtn.style.borderRightColor = bright;
        });
        closeBtn.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            closeBtn.style.backgroundColor = closeBg;
            closeBtn.style.borderTopColor = closeBorder;
            closeBtn.style.borderBottomColor = closeBorder;
            closeBtn.style.borderLeftColor = closeBorder;
            closeBtn.style.borderRightColor = closeBorder;
        });
        backlogHeader.Add(closeBtn);

        backlogScroll = new ScrollView(ScrollViewMode.Vertical);
        backlogScroll.name = "backlog-scroll";
        backlogScroll.style.flexGrow = 1;
        backlogScroll.style.paddingLeft = 40;
        backlogScroll.style.paddingRight = 40;
        backlogScroll.style.paddingTop = 20;
        backlogScroll.style.paddingBottom = 20;
        backlogPanel.Add(backlogScroll);
        StyleBacklogScrollbar(backlogScroll);
    }

    /// <summary>把 UI Toolkit 原生滚动条刷成金色主题：快路径按名直查 + 遍历兜底，不依赖 USS 内部名。</summary>
    private static void StyleBacklogScrollbar(ScrollView scroll)
    {
        var track = new Color(0.12f, 0.08f, 0.05f, 0.9f);          // 轨道底
        var trackEdge = new Color(0.8f, 0.62f, 0.35f, 0.6f);       // 轨道金边
        var thumb = new Color(0.85f, 0.66f, 0.38f, 0.95f);         // 滑块
        var arrowBg = new Color(0.18f, 0.12f, 0.07f, 0.95f);       // 箭头钮底
        var glyphGold = new Color(1f, 0.86f, 0.59f, 1f);           // 三角 glyph

        // 轨道：10px 宽、深色底、1px 金边、全圆角
        void StyleTrack(VisualElement e)
        {
            e.style.width = 10;
            e.style.backgroundColor = track;
            e.style.borderTopWidth = 1;
            e.style.borderBottomWidth = 1;
            e.style.borderLeftWidth = 1;
            e.style.borderRightWidth = 1;
            e.style.borderTopColor = trackEdge;
            e.style.borderBottomColor = trackEdge;
            e.style.borderLeftColor = trackEdge;
            e.style.borderRightColor = trackEdge;
            e.style.borderTopLeftRadius = 5;
            e.style.borderTopRightRadius = 5;
            e.style.borderBottomLeftRadius = 5;
            e.style.borderBottomRightRadius = 5;
        }

        // 滑块：金色、圆角 4、左右 margin 2 让它比轨道细
        void StyleThumb(VisualElement e)
        {
            e.style.backgroundColor = thumb;
            e.style.borderTopLeftRadius = 4;
            e.style.borderTopRightRadius = 4;
            e.style.borderBottomLeftRadius = 4;
            e.style.borderBottomRightRadius = 4;
            e.style.marginLeft = 2;
            e.style.marginRight = 2;
        }

        // 上下箭头钮：10×16、深底金边；三角 glyph Label 刷金
        void StyleArrowButton(VisualElement e)
        {
            e.style.width = 10;
            e.style.height = 16;
            e.style.backgroundColor = arrowBg;
            e.style.borderTopWidth = 1;
            e.style.borderBottomWidth = 1;
            e.style.borderLeftWidth = 1;
            e.style.borderRightWidth = 1;
            e.style.borderTopColor = trackEdge;
            e.style.borderBottomColor = trackEdge;
            e.style.borderLeftColor = trackEdge;
            e.style.borderRightColor = trackEdge;
            foreach (var label in e.Query<Label>().ToList())
            {
                // 名字或文字带三角 glyph（▲▼△▽）的 Label 子元素
                bool isGlyph = (label.name != null && label.name.IndexOfAny(new[] { '▲', '▼', '△', '▽' }) >= 0)
                            || (label.text != null && label.text.IndexOfAny(new[] { '▲', '▼', '△', '▽' }) >= 0);
                if (isGlyph)
                    label.style.color = glyphGold;
            }
        }

        // 按 name 分类（arrow 优先，避免被 slider/scroller 误吞）
        bool Classify(VisualElement e)
        {
            var n = e.name;
            if (string.IsNullOrEmpty(n)) return false;
            var lower = n.ToLowerInvariant();
            if (lower.Contains("up-button") || lower.Contains("down-button")) { StyleArrowButton(e); return true; }
            if (lower.Contains("dragger") || lower.Contains("thumb")) { StyleThumb(e); return true; }
            if (lower.Contains("slider") || lower.Contains("scroller")) { StyleTrack(e); return true; }
            return false;
        }

        // 快路径：按常见 USS 内部名直查
        var slider = scroll.Q<Slider>("unity-scrollview-vertical-slider")
                  ?? scroll.Q<Slider>("unity-scroll-view-vertical-slider");
        if (slider != null) StyleTrack(slider);
        var dragger = scroll.Q("unity-dragger") ?? scroll.Q("unity-thumb");
        if (dragger != null) StyleThumb(dragger);
        var scroller = scroll.Q<Scroller>("unity-scrollview-vertical-scroller")
                    ?? scroll.Q<Scroller>("unity-scroll-view-vertical-scroller");
        if (scroller != null) StyleTrack(scroller);
        foreach (var btnName in new[] { "unity-up-button", "unity-down-button" })
        {
            var b = scroller?.Q(btnName) ?? scroll.Q(btnName);
            if (b != null) StyleArrowButton(b);
        }

        // 兜底：遍历全部子元素按名分类（与快路径重复也无妨，着色幂等）
        foreach (var e in scroll.Query().ToList())
            Classify(e);
    }

    public void AddEntry(string speaker, string text, int sceneIndex, int dialogueIndex)
    {
        entries.Add(new BacklogEntry { speaker = speaker, text = text, sceneIndex = sceneIndex, dialogueIndex = dialogueIndex });
        if (entries.Count > MaxEntries)
            entries.RemoveAt(0);
        UpdateBacklogDisplay();
    }

    private void UpdateBacklogDisplay()
    {
        if (backlogScroll == null) return;
        backlogScroll.Clear();
        var fontDef = GetFontDef();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var entryRow = new VisualElement();
            entryRow.style.flexDirection = FlexDirection.Row;
            entryRow.style.alignItems = Align.Stretch;
            entryRow.style.marginBottom = 26;
            entryRow.style.paddingBottom = 18;
            entryRow.style.borderBottomWidth = 1;
            entryRow.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.15f);
            entryRow.style.paddingTop = 12;
            entryRow.style.paddingLeft = 8;
            entryRow.style.paddingRight = 8;
            entryRow.style.borderTopLeftRadius = 4;
            entryRow.style.borderTopRightRadius = 4;
            entryRow.style.borderBottomLeftRadius = 4;
            entryRow.style.borderBottomRightRadius = 4;

            // 文本内容容器（占满剩余宽度）
            var contentBox = new VisualElement();
            contentBox.style.flexGrow = 1;
            contentBox.style.paddingRight = 12;

            bool isNarration = string.IsNullOrEmpty(entry.speaker) || entry.speaker == "n";

            if (!isNarration)
            {
                var speakerLabel = new Label(entry.speaker);
                speakerLabel.style.fontSize = 24;
                speakerLabel.style.letterSpacing = 2;
                speakerLabel.style.color = new Color(1f, 200f / 255f, 100f / 255f, 0.9f);
                speakerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                speakerLabel.style.marginBottom = 10;
                speakerLabel.style.unityFontDefinition = fontDef;
                speakerLabel.style.backgroundColor = new Color(0.15f, 0.08f, 0.04f, 0.6f);
                speakerLabel.style.paddingLeft = 8;
                speakerLabel.style.paddingRight = 8;
                speakerLabel.style.paddingTop = 4;
                speakerLabel.style.paddingBottom = 4;
                speakerLabel.style.borderTopLeftRadius = 4;
                speakerLabel.style.borderTopRightRadius = 4;
                speakerLabel.style.borderBottomLeftRadius = 4;
                speakerLabel.style.borderBottomRightRadius = 4;
                speakerLabel.style.borderLeftWidth = 3;
                speakerLabel.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
                contentBox.Add(speakerLabel);
            }

            var textLabel = new Label(entry.text);
            textLabel.style.fontSize = 26;
            textLabel.style.letterSpacing = 2;
            textLabel.style.color = new Color(1f, 1f, 1f, 0.85f);
            textLabel.style.whiteSpace = WhiteSpace.Normal;
            textLabel.style.marginBottom = 8;
            textLabel.style.unityFontDefinition = fontDef;
            contentBox.Add(textLabel);
            entryRow.Add(contentBox);

            // 右侧"跳转"按钮：专门跳转到该对话
            int capturedIdx = i;
            var jumpBtn = new UnityEngine.UIElements.Button(() => JumpToEntry(capturedIdx)) { text = "跳转" };
            jumpBtn.style.width = 90;
            jumpBtn.style.height = 40;
            jumpBtn.style.alignSelf = Align.FlexEnd;
            jumpBtn.style.flexShrink = 0;
            jumpBtn.style.marginLeft = 8;
            jumpBtn.style.marginBottom = 8;
            jumpBtn.style.fontSize = 22;
            jumpBtn.style.color = new Color(1f, 1f, 1f, 0.9f);
            jumpBtn.style.backgroundColor = new Color(0.25f, 0.14f, 0.08f, 0.9f);
            jumpBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            jumpBtn.style.unityFontDefinition = fontDef;
            jumpBtn.style.borderTopWidth = 1;
            jumpBtn.style.borderBottomWidth = 1;
            jumpBtn.style.borderLeftWidth = 1;
            jumpBtn.style.borderRightWidth = 1;
            jumpBtn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
            jumpBtn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
            jumpBtn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
            jumpBtn.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
            jumpBtn.style.borderTopLeftRadius = 5;
            jumpBtn.style.borderTopRightRadius = 5;
            jumpBtn.style.borderBottomLeftRadius = 5;
            jumpBtn.style.borderBottomRightRadius = 5;
            jumpBtn.RegisterCallback<PointerEnterEvent>(e =>
            {
                jumpBtn.style.backgroundColor = new Color(0.35f, 0.2f, 0.1f, 0.95f);
                jumpBtn.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
                UiCursor.ShowHand();
            });
            jumpBtn.RegisterCallback<PointerLeaveEvent>(e =>
            {
                jumpBtn.style.backgroundColor = new Color(0.25f, 0.14f, 0.08f, 0.9f);
                jumpBtn.style.color = new Color(1f, 1f, 1f, 0.9f);
                UiCursor.ShowArrow();
            });
            entryRow.Add(jumpBtn);

            backlogScroll.Add(entryRow);
        }
    }

    private void JumpToEntry(int index)
    {
        if (index < 0 || index >= entries.Count) return;
        var entry = entries[index];
        // 跳转到该条目的位置（sceneIndex, dialogueIndex）
        // 这样点击后显示的是该条目之后的下一句
        OnEntryClicked?.Invoke(entry.sceneIndex, entry.dialogueIndex);
        ToggleBacklog();
    }

    public void ToggleBacklog()
    {
        isOpen = !isOpen;
        backlogPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if (isOpen)
        {
            // 延迟一帧后滚动到最底部（最新对话）
            backlogScroll.schedule.Execute(() =>
            {
                backlogScroll.scrollOffset = new Vector2(0, backlogScroll.contentContainer.layout.height);
            }).StartingIn(50);
        }
        else
        {
            // 关闭即恢复箭头：跳转/关闭瞬间鼠标可能仍停在按钮上，Leave 不会触发
            UiCursor.ShowArrow();
        }
    }

    public bool IsOpen => isOpen;

    public void Clear()
    {
        entries.Clear();
        backlogScroll.Clear();
    }
}