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
    private static Texture2D handCursorTex;

    public void Init(UIDocument document)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        if (handCursorTex == null)
            handCursorTex = Resources.Load<Texture2D>("Cursors/cursor_hand");
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
        backlogPanel.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.95f);
        backlogPanel.style.display = DisplayStyle.None;
        backlogPanel.pickingMode = PickingMode.Position;
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

        var closeBtn = new UnityEngine.UIElements.Button(() => ToggleBacklog()) { text = "X" };
        closeBtn.style.width = 60;
        closeBtn.style.height = 40;
        closeBtn.style.fontSize = 24;
        closeBtn.style.color = new Color(1f, 1f, 1f, 0.8f);
        closeBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.6f);
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = fontDef;
        closeBtn.style.borderTopWidth = 1;
        closeBtn.style.borderBottomWidth = 1;
        closeBtn.style.borderLeftWidth = 1;
        closeBtn.style.borderRightWidth = 1;
        closeBtn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        closeBtn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        closeBtn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        closeBtn.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        backlogHeader.Add(closeBtn);

        backlogScroll = new ScrollView(ScrollViewMode.Vertical);
        backlogScroll.name = "backlog-scroll";
        backlogScroll.style.flexGrow = 1;
        backlogScroll.style.paddingLeft = 40;
        backlogScroll.style.paddingRight = 40;
        backlogScroll.style.paddingTop = 20;
        backlogScroll.style.paddingBottom = 20;
        backlogPanel.Add(backlogScroll);
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
                if (handCursorTex != null)
                    UnityEngine.Cursor.SetCursor(handCursorTex, Vector2.zero, UnityEngine.CursorMode.Auto);
            });
            jumpBtn.RegisterCallback<PointerLeaveEvent>(e =>
            {
                jumpBtn.style.backgroundColor = new Color(0.25f, 0.14f, 0.08f, 0.9f);
                jumpBtn.style.color = new Color(1f, 1f, 1f, 0.9f);
                if (LoginManager.cursorTexture != null)
                    UnityEngine.Cursor.SetCursor(LoginManager.cursorTexture, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
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
    }

    public bool IsOpen => isOpen;

    public void Clear()
    {
        entries.Clear();
        backlogScroll.Clear();
    }
}