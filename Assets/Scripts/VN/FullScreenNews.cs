using System;
using UnityEngine;
using UnityEngine.UIElements;

public class FullScreenNews : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;
    private VisualElement newsPanel;
    private ScrollView newsScroll;
    private Label newsContent;
    private Label continueHint;
    private bool isActive;
    private float scrollSpeed = 20f;
    private float normalSpeed = 20f;
    private float fastSpeed = 80f;

    public Action OnClosed;

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

        newsPanel = new VisualElement { name = "news-panel" };
        newsPanel.style.position = Position.Absolute;
        newsPanel.style.top = 0;
        newsPanel.style.left = 0;
        newsPanel.style.right = 0;
        newsPanel.style.bottom = 0;
        newsPanel.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 1f);
        newsPanel.style.display = DisplayStyle.None;
        newsPanel.pickingMode = PickingMode.Position;
        newsPanel.RegisterCallback<ClickEvent>(e => Close());
        root.Add(newsPanel);

        newsScroll = new ScrollView(ScrollViewMode.Vertical);
        newsScroll.name = "news-scroll";
        newsScroll.style.flexGrow = 1;
        newsScroll.style.paddingLeft = 100;
        newsScroll.style.paddingRight = 100;
        newsScroll.style.paddingTop = 80;
        newsScroll.style.paddingBottom = 120;
        newsScroll.style.overflow = Overflow.Hidden;
        // 隐藏滚动条
        newsScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        newsScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        newsPanel.Add(newsScroll);

        newsContent = new Label { name = "news-content" };
        newsContent.style.fontSize = 36;
        newsContent.style.letterSpacing = 4;
        newsContent.style.color = new Color(0.9f, 0.85f, 0.7f, 1f);
        newsContent.style.whiteSpace = WhiteSpace.Normal;
        newsContent.style.unityFontDefinition = fontDef;
        newsContent.style.marginBottom = 60;
        newsContent.style.unityParagraphSpacing = 20;
        newsScroll.Add(newsContent);

        continueHint = new Label { name = "continue-hint", text = "点击任意处继续..." };
        continueHint.style.fontSize = 28;
        continueHint.style.color = new Color(1f, 1f, 1f, 0.35f);
        continueHint.style.unityTextAlign = TextAnchor.MiddleCenter;
        continueHint.style.unityFontDefinition = fontDef;
        continueHint.style.marginTop = 40;
        newsScroll.Add(continueHint);
    }

    private void Update()
    {
        if (!isActive) return;

        // Shift加速
        scrollSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? fastSpeed : normalSpeed;

        // 自动滚动
        var contentHeight = newsScroll.contentContainer.worldBound.height;
        var scrollHeight = newsScroll.worldBound.height;
        if (contentHeight > scrollHeight)
        {
            var maxOffset = contentHeight - scrollHeight;
            var current = newsScroll.scrollOffset.y;
            if (current < maxOffset - 1f)
            {
                newsScroll.scrollOffset = new Vector2(0, current + scrollSpeed * Time.deltaTime);
            }
        }
    }

    public void Show(string text)
    {
        if (newsPanel == null) return;
        isActive = true;
        newsContent.text = text;
        newsPanel.style.display = DisplayStyle.Flex;
        newsScroll.scrollOffset = Vector2.zero;
    }

    public void Close()
    {
        if (!isActive) return;
        isActive = false;
        newsPanel.style.display = DisplayStyle.None;
        OnClosed?.Invoke();
    }

    public bool IsActive => isActive;
}
