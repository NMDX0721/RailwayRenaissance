using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>Main Story 主线剧情面板（独立 UIDocument，参照 BA 设计）。</summary>
public class MainStoryUI : MonoBehaviour
{
    private static MainStoryUI Instance;
    private UIDocument uiDoc;
    private VisualElement overlay;
    private Font gameFont;
    private FontDefinition Fd() => new FontDefinition { font = gameFont };

    /// <summary>序章下属的 10 个节（Episode）。</summary>
    private static readonly (string script, string title, string type)[] Episodes =
    {
        ("prologue_01_news",   "广播里的时代",     "Story"),
        ("prologue_02_day0",   "启程之日",         "Story"),
        ("prologue_03_journey","边境危机",         "Story"),
        ("prologue_04_arrival","抵达雾峰",         "Story"),
        ("prologue_05_inspection", "线路巡视",     "Story"),
        ("prologue_06_team",   "旧人重逢",         "Story"),
        ("prologue_07_first_repair", "第一次检修",  "Story"),
        ("prologue_08_first_run", "首班车",        "Story"),
        ("prologue_09_funding","三条来路",         "Story"),
        ("prologue_10_transition", "序章落幕",     "Story"),
    };

    public static void Show()
    {
        if (Instance == null)
        {
            var go = new GameObject("MainStoryUI");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<MainStoryUI>();
            Instance.Init();
        }
        Instance.overlay.style.display = DisplayStyle.Flex;
    }

    private void Init()
    {
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildDocument();
        BuildUI();
    }

    private void BuildDocument()
    {
        var canvasObj = new GameObject("MainStoryCanvas");
        DontDestroyOnLoad(canvasObj);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
        uiDoc = canvasObj.AddComponent<UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.visualTreeAsset = null;
        uiDoc.rootVisualElement.pickingMode = PickingMode.Ignore;

        overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0;
        overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0.08f, 0.05f, 0.03f, 1f);
        overlay.style.display = DisplayStyle.None;
        uiDoc.rootVisualElement.Add(overlay);
    }

    private void BuildUI()
    {
        var root = overlay;

        // 顶部栏
        var topBar = new VisualElement();
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.alignItems = Align.Center;
        topBar.style.paddingLeft = 20;
        topBar.style.paddingRight = 20;
        topBar.style.paddingTop = 12;
        topBar.style.paddingBottom = 12;
        topBar.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.95f);
        topBar.style.borderBottomWidth = 1;
        topBar.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
        root.Add(topBar);

        var backBtn = new Button(() => Hide()) { text = "<" };
        backBtn.style.width = 40;
        backBtn.style.height = 34;
        backBtn.style.fontSize = 20;
        backBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        backBtn.style.unityFontDefinition = Fd();
        backBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.5f);
        backBtn.style.color = new Color(1f, 0.8f, 0.4f, 1f);
        topBar.Add(backBtn);

        var titleLabel = new Label("Main Story");
        titleLabel.style.fontSize = 24;
        titleLabel.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityFontDefinition = Fd();
        titleLabel.style.marginLeft = 16;
        titleLabel.style.flexGrow = 1;
        topBar.Add(titleLabel);

        // 主体：左右两栏
        var body = new VisualElement();
        body.style.flexDirection = FlexDirection.Row;
        body.style.flexGrow = 1;
        root.Add(body);

        // 左栏：Prologue 卡片
        var leftPanel = new VisualElement();
        leftPanel.style.flexGrow = 1;
        leftPanel.style.paddingLeft = 24;
        leftPanel.style.paddingRight = 12;
        leftPanel.style.paddingTop = 20;
        leftPanel.style.justifyContent = Justify.Center;
        leftPanel.style.alignItems = Align.Center;
        body.Add(leftPanel);

        var prologueCard = new VisualElement();
        prologueCard.style.width = 400;
        prologueCard.style.backgroundColor = new Color(0.15f, 0.10f, 0.06f, 0.9f);
        prologueCard.style.borderTopWidth = 1;
        prologueCard.style.borderBottomWidth = 1;
        prologueCard.style.borderLeftWidth = 1;
        prologueCard.style.borderRightWidth = 1;
        prologueCard.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        prologueCard.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        prologueCard.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        prologueCard.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        prologueCard.style.borderTopLeftRadius = 8;
        prologueCard.style.borderTopRightRadius = 8;
        prologueCard.style.borderBottomLeftRadius = 8;
        prologueCard.style.borderBottomRightRadius = 8;
        prologueCard.style.paddingLeft = 20;
        prologueCard.style.paddingRight = 20;
        prologueCard.style.paddingTop = 16;
        prologueCard.style.paddingBottom = 16;
        leftPanel.Add(prologueCard);

        // 卡片内的图片占位
        var cardImage = new VisualElement();
        cardImage.style.height = 160;
        cardImage.style.backgroundColor = new Color(0.25f, 0.16f, 0.10f, 0.9f);
        cardImage.style.marginBottom = 12;
        cardImage.style.borderTopLeftRadius = 4;
        cardImage.style.borderTopRightRadius = 4;
        cardImage.style.borderBottomLeftRadius = 4;
        cardImage.style.borderBottomRightRadius = 4;
        prologueCard.Add(cardImage);

        // 尝试加载 hangar 图片作为卡片图
        var tex = Resources.Load<Texture2D>("bg/hangar");
        if (tex != null)
        {
            cardImage.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
            cardImage.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
        }
        else
        {
            var placeholder = new Label("Prologue");
            placeholder.style.height = new Length(100, LengthUnit.Percent);
            placeholder.style.fontSize = 28;
            placeholder.style.color = new Color(1f, 200f / 255f, 100f / 255f, 0.8f);
            placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
            placeholder.style.unityFontDefinition = Fd();
            cardImage.Add(placeholder);
        }

        var prologueTitle = new Label("Prologue");
        prologueTitle.style.fontSize = 26;
        prologueTitle.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        prologueTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        prologueTitle.style.unityFontDefinition = Fd();
        prologueTitle.style.marginBottom = 6;
        prologueCard.Add(prologueTitle);

        var prologueDesc = new Label("一个归乡的故事。在被宣判死亡的铁路上，一个年轻人从平壤出发，去向一条被判了死刑的铁路。");
        prologueDesc.style.fontSize = 16;
        prologueDesc.style.color = new Color(0.8f, 0.8f, 0.8f, 0.85f);
        prologueDesc.style.whiteSpace = WhiteSpace.Normal;
        prologueDesc.style.unityFontDefinition = Fd();
        prologueCard.Add(prologueDesc);

        // 右栏：剧集列表
        var rightPanel = new VisualElement();
        rightPanel.style.width = 440;
        rightPanel.style.paddingLeft = 12;
        rightPanel.style.paddingRight = 24;
        rightPanel.style.paddingTop = 20;
        body.Add(rightPanel);

        var listHeader = new Label("Episodes");
        listHeader.style.fontSize = 20;
        listHeader.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        listHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        listHeader.style.unityFontDefinition = Fd();
        listHeader.style.marginBottom = 12;
        rightPanel.Add(listHeader);

        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.style.flexGrow = 1;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        rightPanel.Add(scroll);

        for (int i = 0; i < Episodes.Length; i++)
        {
            var (script, title, type) = Episodes[i];
            bool unlocked = PlayerPrefs.GetInt("ArchiveStory_" + script, 0) == 1;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.7f);
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            row.style.marginBottom = 6;
            row.style.borderTopLeftRadius = 6;
            row.style.borderTopRightRadius = 6;
            row.style.borderBottomLeftRadius = 6;
            row.style.borderBottomRightRadius = 6;
            scroll.Add(row);

            var numLabel = new Label((i + 1).ToString("D2"));
            numLabel.style.fontSize = 18;
            numLabel.style.color = unlocked ? new Color(1f, 200f / 255f, 100f / 255f, 1f) : new Color(0.6f, 0.6f, 0.6f, 0.8f);
            numLabel.style.unityFontDefinition = Fd();
            numLabel.style.width = 30;
            numLabel.style.marginRight = 10;
            row.Add(numLabel);

            var textCol = new VisualElement();
            textCol.style.flexGrow = 1;
            row.Add(textCol);

            var titleLabel2 = new Label(unlocked ? title : "???");
            titleLabel2.style.fontSize = 18;
            titleLabel2.style.color = unlocked ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.6f, 0.6f, 0.6f, 0.8f);
            titleLabel2.style.unityFontDefinition = Fd();
            textCol.Add(titleLabel2);

            var typeLabel = new Label(type);
            typeLabel.style.fontSize = 13;
            typeLabel.style.color = new Color(0.5f, 0.8f, 1f, 0.7f);
            typeLabel.style.unityFontDefinition = Fd();
            textCol.Add(typeLabel);

            if (unlocked)
            {
                var enterBtn = new Button(() => StartEpisode(script)) { text = "Enter" };
                enterBtn.style.width = 80;
                enterBtn.style.height = 32;
                enterBtn.style.fontSize = 16;
                enterBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
                enterBtn.style.unityFontDefinition = Fd();
                enterBtn.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.8f);
                enterBtn.style.color = new Color(0.8f, 0.9f, 1f, 1f);
                enterBtn.style.borderTopLeftRadius = 4;
                enterBtn.style.borderTopRightRadius = 4;
                enterBtn.style.borderBottomLeftRadius = 4;
                enterBtn.style.borderBottomRightRadius = 4;
                row.Add(enterBtn);
            }
        }
    }

    private void StartEpisode(string scriptName)
    {
        PlayerPrefs.SetString("VN_ReplayScript", scriptName);
        PlayerPrefs.SetInt("VN_AutoLoad", 0);
        PlayerPrefs.Save();
        Hide();
        SceneManager.LoadScene("VN_Test");
    }

    public void Hide()
    {
        if (overlay != null) overlay.style.display = DisplayStyle.None;
    }
}