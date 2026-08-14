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

        var titleLabel = new Label("对话回顾（点击跳转）");
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
            var entryElement = new VisualElement();
            entryElement.style.marginBottom = 16;
            entryElement.style.paddingBottom = 16;
            entryElement.style.borderBottomWidth = 1;
            entryElement.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.15f);
            entryElement.style.paddingTop = 8;
            entryElement.style.paddingLeft = 8;
            entryElement.style.paddingRight = 8;
            entryElement.style.borderTopLeftRadius = 4;
            entryElement.style.borderTopRightRadius = 4;
            entryElement.style.borderBottomLeftRadius = 4;
            entryElement.style.borderBottomRightRadius = 4;

            // 点击条目跳转
            int capturedIdx = i;
            entryElement.RegisterCallback<ClickEvent>(e =>
            {
                JumpToEntry(capturedIdx);
            });

            // 鼠标悬停效果
            entryElement.RegisterCallback<PointerEnterEvent>(e =>
            {
                entryElement.style.backgroundColor = new Color(1f, 1f, 1f, 0.05f);
                if (handCursorTex != null)
                    UnityEngine.Cursor.SetCursor(handCursorTex, new Vector2(0, 0), UnityEngine.CursorMode.ForceSoftware);
            });
            entryElement.RegisterCallback<PointerLeaveEvent>(e =>
            {
                entryElement.style.backgroundColor = Color.clear;
                if (LoginManager.cursorTexture != null)
                    UnityEngine.Cursor.SetCursor(LoginManager.cursorTexture, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
            });

            bool isNarration = string.IsNullOrEmpty(entry.speaker) || entry.speaker == "n";

            if (!isNarration)
            {
                var speakerLabel = new Label(entry.speaker);
                speakerLabel.style.fontSize = 24;
                speakerLabel.style.color = new Color(1f, 200f / 255f, 100f / 255f, 0.9f);
                speakerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                speakerLabel.style.marginBottom = 4;
                speakerLabel.style.unityFontDefinition = fontDef;
                entryElement.Add(speakerLabel);
            }

            var textLabel = new Label(entry.text);
            textLabel.style.fontSize = 26;
            textLabel.style.color = new Color(1f, 1f, 1f, 0.85f);
            textLabel.style.whiteSpace = WhiteSpace.Normal;
            textLabel.style.unityFontDefinition = fontDef;
            entryElement.Add(textLabel);

            backlogScroll.Add(entryElement);
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
            backlogScroll.scrollOffset = new Vector2(0, backlogScroll.contentContainer.worldBound.height);
        }
    }

    public bool IsOpen => isOpen;

    public void Clear()
    {
        entries.Clear();
        backlogScroll.Clear();
    }
}