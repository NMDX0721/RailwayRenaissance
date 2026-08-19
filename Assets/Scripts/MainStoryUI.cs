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

        // —— 枕木纹理层（淡色横线，模拟木质枕木） ——
        AddSleeperTexture(root);

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
        prologueCard.style.width = 420;
        prologueCard.style.backgroundColor = new Color(0.15f, 0.10f, 0.06f, 0.95f);
        prologueCard.style.borderTopWidth = 2;
        prologueCard.style.borderBottomWidth = 2;
        prologueCard.style.borderLeftWidth = 2;
        prologueCard.style.borderRightWidth = 2;
        prologueCard.style.borderTopColor = new Color(0.82f, 0.66f, 0.4f, 0.9f);
        prologueCard.style.borderBottomColor = new Color(0.82f, 0.66f, 0.4f, 0.9f);
        prologueCard.style.borderLeftColor = new Color(0.82f, 0.66f, 0.4f, 0.9f);
        prologueCard.style.borderRightColor = new Color(0.82f, 0.66f, 0.4f, 0.9f);
        prologueCard.style.borderTopLeftRadius = 6;
        prologueCard.style.borderTopRightRadius = 6;
        prologueCard.style.borderBottomLeftRadius = 6;
        prologueCard.style.borderBottomRightRadius = 6;
        // 轻微旋转，仿票据随意搁置感
        prologueCard.style.rotate = new Rotate(new Angle(-1.2f)); // 反过来是 1.2° 逆时针
        prologueCard.style.paddingLeft = 24;
        prologueCard.style.paddingRight = 24;
        prologueCard.style.paddingTop = 18;
        prologueCard.style.paddingBottom = 18;
        leftPanel.Add(prologueCard);

        // 左上角"票号"戳记（金色标签条）
        var ticketStamp = new Label("NO.0721");
        ticketStamp.style.position = Position.Absolute;
        ticketStamp.style.top = -11;
        ticketStamp.style.right = 18;
        ticketStamp.style.fontSize = 13;
        ticketStamp.style.color = new Color(0.9f, 0.75f, 0.45f, 0.85f);
        ticketStamp.style.backgroundColor = new Color(0.18f, 0.12f, 0.06f, 1f);
        ticketStamp.style.paddingLeft = 8; ticketStamp.style.paddingRight = 8;
        ticketStamp.style.paddingTop = 2; ticketStamp.style.paddingBottom = 2;
        ticketStamp.style.borderTopLeftRadius = 3; ticketStamp.style.borderTopRightRadius = 3;
        ticketStamp.style.borderBottomLeftRadius = 3; ticketStamp.style.borderBottomRightRadius = 3;
        ticketStamp.style.unityFontDefinition = Fd();
        prologueCard.Add(ticketStamp);

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

        // —— 信号灯状态行（三色：绿=完成全部 / 黄=进行中 / 红=锁定） ——
        var lampRow = new VisualElement();
        lampRow.style.flexDirection = FlexDirection.Row;
        lampRow.style.alignItems = Align.Center;
        lampRow.style.marginTop = 14;
        prologueCard.Add(lampRow);

        int completed = 0;
        for (int i = 0; i < Episodes.Length; i++)
            if (PlayerPrefs.GetInt("ArchiveStory_" + Episodes[i].script, 0) == 1) completed++;

        // 画 10 盏信号灯
        for (int i = 0; i < Episodes.Length; i++)
        {
            bool done = PlayerPrefs.GetInt("ArchiveStory_" + Episodes[i].script, 0) == 1;
            var lamp = new VisualElement();
            lamp.style.width = 10; lamp.style.height = 10;
            lamp.style.borderTopLeftRadius = 5; lamp.style.borderTopRightRadius = 5;
            lamp.style.borderBottomLeftRadius = 5; lamp.style.borderBottomRightRadius = 5;
            lamp.style.marginRight = 6;
            // 绿=已完成 / 黄=当前进行 / 红=未解锁
            if (done)
                lamp.style.backgroundColor = new Color(0.3f, 0.85f, 0.4f, 1f);
            else if (i == completed)
                lamp.style.backgroundColor = new Color(1f, 0.8f, 0.2f, 1f);
            else
                lamp.style.backgroundColor = new Color(0.6f, 0.25f, 0.2f, 0.6f);
            lampRow.Add(lamp);
        }

        var lampStatus = new Label(completed + "/" + Episodes.Length);
        lampStatus.style.fontSize = 14;
        lampStatus.style.color = new Color(0.8f, 0.75f, 0.6f, 0.8f);
        lampStatus.style.unityFontDefinition = Fd();
        lampStatus.style.marginLeft = 4;
        lampRow.Add(lampStatus);

        // 右栏：Act 1 > Prologue 层级 + 话列表（旧站台玻璃质感）
        var rightPanel = new VisualElement();
        rightPanel.style.width = 460;
        rightPanel.style.paddingLeft = 14;
        rightPanel.style.paddingRight = 24;
        rightPanel.style.paddingTop = 20;
        // 旧玻璃质感：半透明暖棕 + 内侧细边
        rightPanel.style.backgroundColor = new Color(0.16f, 0.11f, 0.07f, 0.35f);
        rightPanel.style.borderTopLeftRadius = 10;
        rightPanel.style.borderTopRightRadius = 10;
        rightPanel.style.borderBottomLeftRadius = 10;
        rightPanel.style.borderBottomRightRadius = 10;
        body.Add(rightPanel);

        // 玻璃反光条（顶部斜向高光）
        var glassHighlight = new VisualElement();
        glassHighlight.style.position = Position.Absolute;
        glassHighlight.style.top = 0; glassHighlight.style.left = 0; glassHighlight.style.right = 0;
        glassHighlight.style.height = 2;
        glassHighlight.style.backgroundColor = new Color(0.9f, 0.8f, 0.6f, 0.15f);
        glassHighlight.pickingMode = PickingMode.Ignore;
        rightPanel.Add(glassHighlight);

        // Act 1 标题
        var actLabel = new Label("Act 1");
        actLabel.style.fontSize = 22;
        actLabel.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        actLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        actLabel.style.unityFontDefinition = Fd();
        actLabel.style.marginBottom = 2;
        rightPanel.Add(actLabel);

        // Act 1 金色下划线
        var actUnderline = new VisualElement();
        actUnderline.style.width = 80;
        actUnderline.style.height = 2;
        actUnderline.style.backgroundColor = new Color(1f, 0.78f, 0.4f, 0.5f);
        actUnderline.style.marginBottom = 4;
        rightPanel.Add(actUnderline);

        // Prologue 副标题
        var prologueLabel = new Label("Prologue");
        prologueLabel.style.fontSize = 18;
        prologueLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 0.85f);
        prologueLabel.style.unityFontDefinition = Fd();
        prologueLabel.style.marginBottom = 12;
        rightPanel.Add(prologueLabel);

        // 话列表
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
            row.style.backgroundColor = unlocked
                ? new Color(0.14f, 0.09f, 0.05f, 0.75f)
                : new Color(0.10f, 0.07f, 0.04f, 0.5f);
            row.style.paddingLeft = 14;
            row.style.paddingRight = 12;
            row.style.paddingTop = 9;
            row.style.paddingBottom = 9;
            row.style.marginBottom = 6;
            row.style.borderTopLeftRadius = 6;
            row.style.borderTopRightRadius = 6;
            row.style.borderBottomLeftRadius = 6;
            row.style.borderBottomRightRadius = 6;
            row.pickingMode = PickingMode.Position;
            scroll.Add(row);

            // 左侧信号灯（绿=解锁 / 红=锁定）
            var lamp = new VisualElement();
            lamp.style.width = 10; lamp.style.height = 10;
            lamp.style.borderTopLeftRadius = 5; lamp.style.borderTopRightRadius = 5;
            lamp.style.borderBottomLeftRadius = 5; lamp.style.borderBottomRightRadius = 5;
            lamp.style.marginRight = 10;
            lamp.style.flexShrink = 0;
            if (unlocked)
            {
                lamp.style.backgroundColor = new Color(0.3f, 0.85f, 0.4f, 1f);
                // 绿色外发光（双层圆模拟）
                lamp.style.borderTopWidth = 2; lamp.style.borderBottomWidth = 2;
                lamp.style.borderLeftWidth = 2; lamp.style.borderRightWidth = 2;
                lamp.style.borderTopColor = new Color(0.3f, 0.85f, 0.4f, 0.25f);
                lamp.style.borderBottomColor = new Color(0.3f, 0.85f, 0.4f, 0.25f);
                lamp.style.borderLeftColor = new Color(0.3f, 0.85f, 0.4f, 0.25f);
                lamp.style.borderRightColor = new Color(0.3f, 0.85f, 0.4f, 0.25f);
            }
            else
            {
                lamp.style.backgroundColor = new Color(0.55f, 0.22f, 0.18f, 0.65f);
            }
            row.Add(lamp);

            var numLabel = new Label("第" + (i + 1) + "话");
            numLabel.style.fontSize = 17;
            numLabel.style.color = unlocked ? new Color(1f, 200f / 255f, 100f / 255f, 1f) : new Color(0.6f, 0.6f, 0.6f, 0.8f);
            numLabel.style.unityFontDefinition = Fd();
            numLabel.style.width = 42;
            numLabel.style.marginRight = 8;
            numLabel.style.flexShrink = 0;
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
                enterBtn.style.width = 74;
                enterBtn.style.height = 30;
                enterBtn.style.fontSize = 15;
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

    /// <summary>枕木纹理层：8 条淡色横木，模拟木质枕木质感（不抢内容）。</summary>
    private void AddSleeperTexture(VisualElement parent)
    {
        var texLayer = new VisualElement { name = "sleeper-texture" };
        texLayer.style.position = Position.Absolute;
        texLayer.style.top = 0; texLayer.style.left = 0;
        texLayer.style.right = 0; texLayer.style.bottom = 0;
        texLayer.style.flexDirection = FlexDirection.Column;
        texLayer.style.justifyContent = Justify.SpaceBetween;
        texLayer.pickingMode = PickingMode.Ignore;
        parent.Add(texLayer);

        // 8 根枕木横条，模拟道床密度不均
        float[] alphas = { 0.05f, 0.04f, 0.06f, 0.03f, 0.05f, 0.04f, 0.06f, 0.03f };
        for (int i = 0; i < 8; i++)
        {
            var sleeper = new VisualElement();
            sleeper.style.height = 12;
            sleeper.style.marginTop = (i % 2 == 0) ? 30 : 46;
            sleeper.style.marginBottom = 0;
            // 枕木偏亮暖棕色，极淡
            sleeper.style.backgroundColor = new Color(0.45f, 0.35f, 0.2f, alphas[i]);
            texLayer.Add(sleeper);
        }

        // —— 轨道透视线（两侧斜向透视引导线） ——
        AddRailPerspective(texLayer);
    }

    /// <summary>轨道透视线：两条向中心汇聚的斜线（薄长条近似）。</summary>
    private void AddRailPerspective(VisualElement parent)
    {
        var railLeft = new VisualElement();
        railLeft.style.position = Position.Absolute;
        railLeft.style.left = -80; railLeft.style.top = 0; railLeft.style.bottom = 0;
        railLeft.style.width = 3;
        railLeft.style.backgroundColor = new Color(0.7f, 0.6f, 0.4f, 0.10f);
        railLeft.style.rotate = new Rotate(new Angle(12f));
        railLeft.pickingMode = PickingMode.Ignore;
        parent.Add(railLeft);

        var railRight = new VisualElement();
        railRight.style.position = Position.Absolute;
        railRight.style.right = -80; railRight.style.top = 0; railRight.style.bottom = 0;
        railRight.style.width = 3;
        railRight.style.backgroundColor = new Color(0.7f, 0.6f, 0.4f, 0.10f);
        railRight.style.rotate = new Rotate(new Angle(-12f));
        railRight.pickingMode = PickingMode.Ignore;
        parent.Add(railRight);
    }

    private void StartEpisode(string scriptName)
    {
        PlayerPrefs.SetString("VN_ReplayScript", scriptName);
        PlayerPrefs.SetInt("VN_AutoLoad", 0);
        // 从经营主界面（GameMainUI → 事务 → 剧情）进入 → 返回时回毛胚主界面
        PlayerPrefs.SetInt("VN_FromGameMain", 1);
        PlayerPrefs.Save();
        Hide();
        SceneManager.LoadScene("VN_Test");
    }

    /// <summary>静态隐藏（供 VN 返回时清空）。</summary>
    public static void HideStatic()
    {
        if (Instance != null) Instance.Hide();
    }

    public void Hide()
    {
        if (overlay != null) overlay.style.display = DisplayStyle.None;
    }
}