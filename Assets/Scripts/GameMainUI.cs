using UnityEngine;
using UnityEngine.UIElements;

/// <summary>小米平板系统桌面（模拟经营主界面）。澎湃32·朝鲜特供版 像素演绎。
/// 锁屏(扫脸)→桌面(状态栏+App网格+任务栏)→App窗口。</summary>
public class GameMainUI : MonoBehaviour
{
    private static GameMainUI Instance;
    private UIDocument uiDoc;
    private VisualElement root;
    private VisualElement lockScreen;
    private VisualElement desktop;
    private Font gameFont;
    private FontDefinition Fd() => new FontDefinition { font = gameFont };

    private float clockTimer;

    public static void Show()
    {
        if (Instance == null)
        {
            var go = new GameObject("GameMainUI");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameMainUI>();
            Instance.Init();
        }
        // 每次进入从锁屏开始（首次进入体验扫脸）
        if (Instance.lockScreen != null && !Instance.unlocked)
            Instance.lockScreen.style.display = DisplayStyle.Flex;
        Instance.root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (root != null) root.style.display = DisplayStyle.None;
    }

    public bool unlocked;
    public bool IsLocked() => !unlocked;

    private static readonly Color Paper = new Color(0.95f, 0.93f, 0.88f, 1f);   // 壁纸基底（江南水乡淡纸色）
    private static readonly Color Ink = new Color(0.12f, 0.12f, 0.14f, 1f);     // 墨色
    private static readonly Color Accent = new Color(0.85f, 0.45f, 0.15f, 1f);  // 澎湃橙（像素演绎）

    // 液态玻璃色彩系统
    private static readonly Color GlassBg = new Color(0.06f, 0.05f, 0.07f, 0.75f);       // 面板/窗口半透明背景
    private static readonly Color GlassBgLight = new Color(1f, 1f, 1f, 0.35f);            // 小部件半透明白
    private static readonly Color GlassBgSoft = new Color(1f, 1f, 1f, 0.08f);             // 设置项微透明
    private static readonly Color GlassBgHover = new Color(1f, 1f, 1f, 0.12f);            // 设置项hover
    private static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.2f);              // 半透明边框
    private static readonly Color GlassBorderSoft = new Color(1f, 1f, 1f, 0.12f);         // 柔和边框
    private static readonly Color GlassShadow = new Color(0f, 0f, 0f, 0.5f);              // 窗口阴影色
    private static readonly Color GlassAccent = new Color(0.7f, 0.85f, 1f, 0.9f);         // 蓝色强调
    private static readonly Color GlassText = new Color(1f, 1f, 1f, 0.9f);                // 主文字
    private static readonly Color GlassTextDim = new Color(1f, 1f, 1f, 0.5f);             // 次文字
    private static readonly Color GlassTextMuted = new Color(1f, 1f, 1f, 0.35f);          // 弱文字
    private static readonly Color GlassMask = new Color(0f, 0f, 0f, 0.55f);               // 遮罩层

    private void Init()
    {
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildDocument();
        BuildLockScreen();
        BuildDesktop();
        BuildAppLayer();
        ShowLockScreen();
    }

    private void BuildDocument()
    {
        var canvasObj = new GameObject("GameMainCanvas");
        DontDestroyOnLoad(canvasObj);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
        uiDoc = canvasObj.AddComponent<UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.visualTreeAsset = null;
        root = uiDoc.rootVisualElement;
        root.style.display = DisplayStyle.None;
    }

    // ============ 锁屏 ============
    private void BuildLockScreen()
    {
        lockScreen = new VisualElement { name = "tablet-lock" };
        lockScreen.style.position = Position.Absolute;
        lockScreen.style.top = 0; lockScreen.style.left = 0;
        lockScreen.style.right = 0; lockScreen.style.bottom = 0;
        lockScreen.style.alignItems = Align.Center;
        lockScreen.style.justifyContent = Justify.Center;
        lockScreen.pickingMode = PickingMode.Position;
        lockScreen.style.flexDirection = FlexDirection.Column;
        root.Add(lockScreen);

        // 半透明深蓝罩（云层感）
        var veil = new VisualElement();
        veil.style.position = Position.Absolute;
        veil.style.top = 0; veil.style.left = 0; veil.style.right = 0; veil.style.bottom = 0;
        veil.style.backgroundColor = new Color(0.1f, 0.12f, 0.2f, 0.62f);
        lockScreen.Add(veil);

        // 居中大时钟（液态玻璃风格）
        var clock = new Label("12:00");
        clock.name = "lock-clock";
        clock.style.fontSize = 96;
        clock.style.color = Color.white;
        clock.style.unityFontDefinition = Fd();
        clock.style.unityTextOutlineWidth = 3;
        clock.style.unityTextOutlineColor = new Color(0, 0, 0, 0.3f);
        lockScreen.Add(clock);

        // 日期卡片（半透明玻璃）
        var dateCard = new VisualElement();
        dateCard.style.backgroundColor = GlassBorder;
        dateCard.style.borderTopLeftRadius = 20;
        dateCard.style.borderTopRightRadius = 20;
        dateCard.style.borderBottomLeftRadius = 20;
        dateCard.style.borderBottomRightRadius = 20;
        dateCard.style.paddingLeft = 20; dateCard.style.paddingRight = 20;
        dateCard.style.paddingTop = 8; dateCard.style.paddingBottom = 8;
        dateCard.style.marginBottom = 28;
        lockScreen.Add(dateCard);

        var dateLine = new Label("主体历 105 年 1 月 1 日 · 星期四");
        dateLine.style.fontSize = 18;
        dateLine.style.color = GlassText;
        dateLine.style.unityFontDefinition = Fd();
        dateCard.Add(dateLine);

        // 扫脸取景框（圆形，液态玻璃风格）
        var frame = new VisualElement { name = "face-frame" };
        frame.style.width = 180; frame.style.height = 180;
        frame.style.borderTopWidth = 2; frame.style.borderBottomWidth = 2;
        frame.style.borderLeftWidth = 2; frame.style.borderRightWidth = 2;
        frame.style.borderTopColor = new Color(0.6f, 0.85f, 1f, 0.5f);
        frame.style.borderBottomColor = new Color(0.6f, 0.85f, 1f, 0.5f);
        frame.style.borderLeftColor = new Color(0.6f, 0.85f, 1f, 0.5f);
        frame.style.borderRightColor = new Color(0.6f, 0.85f, 1f, 0.5f);
        frame.style.borderTopLeftRadius = 90; frame.style.borderTopRightRadius = 90;
        frame.style.borderBottomLeftRadius = 90; frame.style.borderBottomRightRadius = 90;
        frame.style.backgroundColor = new Color(0, 0.15f, 0.3f, 0.12f);
        frame.style.alignItems = Align.Center;
        frame.style.justifyContent = Justify.Center;
        lockScreen.Add(frame);

        // 像素光带（扫脸动画）
        var beam = new Label("▂▃▄▅▆▇");
        beam.name = "face-beam";
        beam.style.fontSize = 22;
        beam.style.color = new Color(0.55f, 0.9f, 1f, 0.7f);
        beam.style.unityFontDefinition = Fd();
        beam.style.position = Position.Absolute;
        beam.style.top = new Length(46, LengthUnit.Percent);
        beam.style.opacity = 0.5f;
        frame.Add(beam);

        var hint = new Label("[ 扫脸解锁 · 点击任意处 ]");
        hint.style.fontSize = 14;
        hint.style.color = GlassTextDim;
        hint.style.unityFontDefinition = Fd();
        hint.style.marginTop = 24;
        lockScreen.Add(hint);

        var ver = new Label("澎湃OS 32 · DPRK Edition");
        ver.style.position = Position.Absolute;
        ver.style.bottom = 26;
        ver.style.left = 0; ver.style.right = 0;
        ver.style.fontSize = 15;
        ver.style.color = GlassTextMuted;
        ver.style.unityTextAlign = TextAnchor.MiddleCenter;
        ver.style.unityFontDefinition = Fd();
        lockScreen.Add(ver);

        // 点击扫脸解锁
        lockScreen.RegisterCallback<ClickEvent>(e => Unlock());
        // 空格也可解锁
        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Space && !unlocked && lockScreen.style.display == DisplayStyle.Flex)
            {
                Unlock();
                evt.StopPropagation();
            }
        });
    }

    private void ShowLockScreen()
    {
        unlocked = false;
        lockScreen.style.display = DisplayStyle.Flex;
        desktop.style.display = DisplayStyle.None;
    }

    private void Unlock()
    {
        if (unlocked) return;
        unlocked = true;
        // 光带扫一遍 → 桌面淡入
        var beam = lockScreen.Q<Label>("face-beam");
        if (beam != null)
        {
            beam.schedule.Execute(() =>
            {
                beam.style.top = new Length(70, LengthUnit.Percent);
                beam.style.opacity = 0;
            }).ExecuteLater(30);
        }
        lockScreen.schedule.Execute(() =>
        {
            lockScreen.style.display = DisplayStyle.None;
            desktop.style.display = DisplayStyle.Flex;
            desktop.style.opacity = 0;
            desktop.schedule.Execute(() => { desktop.style.opacity = 1; }).ExecuteLater(10);
        }).ExecuteLater(420);
    }

    // ============ 桌面 ============
    private VisualElement statusBar;
    private Label statusClock;
    private readonly System.Collections.Generic.List<VisualElement> appIcons = new System.Collections.Generic.List<VisualElement>();

    // ============ 岁月AI投影 ============
    private VisualElement suiyueWidget;
    private Label suiyueBubble;
    private float suiyueTimer;
    private int suiyueIdleIndex;
    private static readonly string[] SuiyueIdleLines = {
        "我可是高性能AI！",
        "数据库全都有哦！",
        "三秒就能算出来～",
        "在看什么呢？",
        "需要帮忙吗？",
        "今天天气不错呢。",
        "……别一直盯着我看啦。",
        "我的运算能力可是很强的！",
    };

    private void BuildDesktop()
    {
        desktop = new VisualElement { name = "tablet-desktop" };
        desktop.style.position = Position.Absolute;
        desktop.style.top = 0; desktop.style.left = 0;
        desktop.style.right = 0; desktop.style.bottom = 0;
        desktop.style.flexDirection = FlexDirection.Column;
        desktop.style.display = DisplayStyle.None;
        root.Add(desktop);

        // 壁纸层（软化纸色渐变模拟）
        var wallpaper = new VisualElement();
        wallpaper.style.position = Position.Absolute;
        wallpaper.style.top = 0; wallpaper.style.left = 0; wallpaper.style.right = 0; wallpaper.style.bottom = 0;
        wallpaper.style.backgroundColor = Paper;
        desktop.Add(wallpaper);
        // 远山像素条（壁纸点缀）
        AddWallpaperRidge(wallpaper);

        // ---- 顶部状态栏（液态玻璃） ----
        statusBar = new VisualElement();
        statusBar.style.flexDirection = FlexDirection.Row;
        statusBar.style.justifyContent = Justify.SpaceBetween;
        statusBar.style.paddingLeft = 20; statusBar.style.paddingRight = 20;
        statusBar.style.paddingTop = 8; statusBar.style.paddingBottom = 6;
        statusBar.style.height = 38;
        statusBar.style.backgroundColor = new Color(0.06f, 0.05f, 0.07f, 0.45f);
        desktop.Add(statusBar);

        statusClock = new Label("12:00");
        statusClock.style.fontSize = 17;
        statusClock.style.color = GlassText;
        statusClock.style.unityFontDefinition = Fd();
        statusBar.Add(statusClock);

        var rightInfo = new VisualElement();
        rightInfo.style.flexDirection = FlexDirection.Row;
        rightInfo.style.alignItems = Align.Center;
        statusBar.Add(rightInfo);

        var netTag = new Label("阿里郎内网");
        netTag.style.fontSize = 14;
        netTag.style.color = GlassAccent;
        netTag.style.unityFontDefinition = Fd();
        netTag.style.marginRight = 14;
        rightInfo.Add(netTag);

        var sig = new Label("●●●○○");
        sig.style.fontSize = 14; sig.style.color = GlassText; sig.style.unityFontDefinition = Fd();
        sig.style.marginRight = 12;
        rightInfo.Add(sig);

        var batt = new Label("▮▮▮▮▯ 78%");
        batt.style.fontSize = 14; batt.style.color = GlassText; batt.style.unityFontDefinition = Fd();
        rightInfo.Add(batt);

        // ---- 负一屏小部件（今日线路·液态玻璃） ----
        var widget = new VisualElement();
        widget.style.width = 320;
        widget.style.height = 80;
        widget.style.marginTop = 8;
        widget.style.marginLeft = 20;
        widget.style.backgroundColor = GlassBgLight;
        widget.style.borderTopLeftRadius = 16; widget.style.borderTopRightRadius = 16;
        widget.style.borderBottomLeftRadius = 16; widget.style.borderBottomRightRadius = 16;
        widget.style.borderTopWidth = 1; widget.style.borderBottomWidth = 1;
        widget.style.borderLeftWidth = 1; widget.style.borderRightWidth = 1;
        widget.style.borderTopColor = GlassBorder; widget.style.borderBottomColor = GlassBorder;
        widget.style.borderLeftColor = GlassBorder; widget.style.borderRightColor = GlassBorder;
        widget.style.paddingLeft = 16; widget.style.paddingTop = 10;
        widget.style.flexDirection = FlexDirection.Column;
        desktop.Add(widget);

        var wTitle = new Label("今日线路 · 雾峰村线");
        wTitle.style.fontSize = 15; wTitle.style.color = Ink; wTitle.style.unityFontDefinition = Fd();
        widget.Add(wTitle);
        var wData = new Label("运营第 1 天   资金 2,000 沙币   信任度 友好");
        wData.style.fontSize = 13; wData.style.color = new Color(0.3f, 0.3f, 0.35f, 0.8f);
        wData.style.unityFontDefinition = Fd();
        wData.style.marginTop = 4;
        widget.Add(wData);

        // ---- App 网格（3 行 × 5 列，不足补空位） ----
        var grid = new VisualElement();
        grid.style.flexGrow = 1;
        grid.style.flexDirection = FlexDirection.Column;
        grid.style.justifyContent = Justify.Center;
        grid.style.alignItems = Align.Center;
        desktop.Add(grid);

        string[] iconNames = {
            "RDA", "归途", "媒姬百科", "相册", "音乐",
            "笔记", "设置", "浏览器", "安全中心", "米家",
            "应用商店", "阿里郎商店", "未来网", "白头疫苗", "时钟",
            "日历", "天气", "计算器", "文件管理", "地图",
        };
        for (int r = 0; r < 4; r++)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            grid.Add(row);
            for (int c = 0; c < 5; c++)
            {
                int idx = r * 5 + c;
                if (idx >= iconNames.Length) continue;
                string name = iconNames[idx];
                if (string.IsNullOrEmpty(name)) continue;
                var cell = BuildAppIcon(name);
                row.Add(cell);
                appIcons.Add(cell);
            }
        }

        // ---- 底部任务栏（液态玻璃Dock） ----
        var dock = new VisualElement();
        dock.style.height = 58;
        dock.style.flexDirection = FlexDirection.Row;
        dock.style.justifyContent = Justify.Center;
        dock.style.alignItems = Align.Center;
        dock.style.backgroundColor = new Color(0.08f, 0.07f, 0.1f, 0.65f);
        dock.style.borderTopLeftRadius = 20; dock.style.borderTopRightRadius = 20;
        dock.style.borderTopWidth = 1;
        dock.style.borderTopColor = GlassBorderSoft;
        dock.style.marginLeft = 100; dock.style.marginRight = 100;
        dock.style.marginBottom = 12;
        desktop.Add(dock);

        string[] dockApps = { "RDA", "归途", "笔记", "设置" };
        foreach (var d in dockApps)
        {
            var di = new Label(IconFor(d));
            di.style.fontSize = 28;
            di.style.color = new Color(0.92f, 0.92f, 0.95f, 0.95f);
            di.style.unityFontDefinition = Fd();
            di.style.marginLeft = 24; di.style.marginRight = 24;
            di.style.unityTextAlign = TextAnchor.MiddleCenter;
            di.style.width = 46; di.style.height = 42;
            di.RegisterCallback<ClickEvent>(_ => LaunchApp(d));
            dock.Add(di);
        }

        // ---- 岁月AI投影（桌面常驻浮窗）----
        BuildSuiyueWidget();
    }

    private void AddWallpaperRidge(VisualElement parent)
    {
        // 江南水乡·远山+水面像素条（桌面壁纸点缀）
        var ridge = new VisualElement();
        ridge.style.position = Position.Absolute;
        ridge.style.left = 0; ridge.style.right = 0;
        ridge.style.bottom = 54;
        ridge.style.height = 90;
        ridge.pickingMode = PickingMode.Ignore;
        parent.Add(ridge);
        // 简化：淡墨远山三层
        for (int i = 0; i < 3; i++)
        {
            var layer = new Label(new string('▂', 40 - i * 8));
            layer.style.fontSize = 18 + i * 6;
            layer.style.color = new Color(0.3f, 0.35f, 0.4f, 0.25f + i * 0.1f);
            layer.style.unityFontDefinition = Fd();
            layer.style.position = Position.Absolute;
            layer.style.left = new Length(6 + i * 3, LengthUnit.Percent);
            layer.style.bottom = new Length(i * 7, LengthUnit.Pixel);
            ridge.Add(layer);
        }
    }

    // ============ 岁月AI投影构建 ============
    private void BuildSuiyueWidget()
    {
        suiyueWidget = new VisualElement { name = "suiyue-widget" };
        suiyueWidget.style.position = Position.Absolute;
        suiyueWidget.style.bottom = 80;
        suiyueWidget.style.right = 20;
        suiyueWidget.style.width = 110;
        suiyueWidget.style.alignItems = Align.Center;
        suiyueWidget.style.justifyContent = Justify.FlexEnd;
        suiyueWidget.pickingMode = PickingMode.Position;
        desktop.Add(suiyueWidget);

        // 气泡提示（Idle随机台词）
        suiyueBubble = new Label("");
        suiyueBubble.style.fontSize = 11;
        suiyueBubble.style.color = GlassText;
        suiyueBubble.style.unityFontDefinition = Fd();
        suiyueBubble.style.backgroundColor = GlassBg;
        suiyueBubble.style.borderTopLeftRadius = 8; suiyueBubble.style.borderTopRightRadius = 8;
        suiyueBubble.style.borderBottomLeftRadius = 8; suiyueBubble.style.borderBottomRightRadius = 8;
        suiyueBubble.style.paddingLeft = 6; suiyueBubble.style.paddingRight = 6;
        suiyueBubble.style.paddingTop = 4; suiyueBubble.style.paddingBottom = 4;
        suiyueBubble.style.whiteSpace = WhiteSpace.Normal;
        suiyueBubble.style.maxWidth = 100;
        suiyueBubble.style.display = DisplayStyle.None;
        suiyueBubble.pickingMode = PickingMode.Ignore;
        suiyueWidget.Add(suiyueBubble);

        // 角色卡片
        var card = new VisualElement();
        card.style.width = 100; card.style.height = 130;
        card.style.backgroundColor = new Color(0.06f, 0.05f, 0.07f, 0.6f);
        card.style.borderTopLeftRadius = 12; card.style.borderTopRightRadius = 12;
        card.style.borderBottomLeftRadius = 12; card.style.borderBottomRightRadius = 12;
        card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
        card.style.borderTopColor = GlassBorderSoft; card.style.borderBottomColor = GlassBorderSoft;
        card.style.borderLeftColor = GlassBorderSoft; card.style.borderRightColor = GlassBorderSoft;
        card.style.alignItems = Align.Center;
        card.style.justifyContent = Justify.Center;
        card.style.marginTop = 4;
        suiyueWidget.Add(card);

        // 角色名
        var nameLabel = new Label("岁 月");
        nameLabel.style.fontSize = 14;
        nameLabel.style.color = GlassAccent;
        nameLabel.style.unityFontDefinition = Fd();
        nameLabel.style.marginBottom = 4;
        card.Add(nameLabel);

        // 角色形象（用文字像素模拟二次元少女轮廓）
        var avatar = new Label("少女");
        avatar.style.fontSize = 28;
        avatar.style.color = new Color(0.85f, 0.45f, 0.15f, 0.7f); // 澎湃橙
        avatar.style.unityFontDefinition = Fd();
        card.Add(avatar);

        // 状态标签
        var status = new Label("在线");
        status.style.fontSize = 10;
        status.style.color = new Color(0.5f, 0.8f, 0.5f, 0.7f);
        status.style.unityFontDefinition = Fd();
        status.style.marginTop = 4;
        card.Add(status);

        // 点击展开对话
        card.RegisterCallback<ClickEvent>(_ => ShowSuiyueDialogue());

        // hover效果
        card.RegisterCallback<PointerEnterEvent>(_ =>
        {
            card.style.backgroundColor = new Color(0.1f, 0.08f, 0.12f, 0.7f);
            card.style.scale = new Scale(new Vector2(1.05f, 1.05f));
        });
        card.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            card.style.backgroundColor = new Color(0.06f, 0.05f, 0.07f, 0.6f);
            card.style.scale = new Scale(new Vector2(1f, 1f));
        });
    }

    private void ShowSuiyueDialogue()
    {
        var win = BuildInnerPanel("岁月 · AI对话");
        win.style.height = 400;

        // 对话历史区
        var chatArea = new VisualElement();
        chatArea.style.flexGrow = 1;
        chatArea.style.flexDirection = FlexDirection.Column;
        chatArea.style.marginTop = 8;
        win.Add(chatArea);

        // 初始对话
        AddChatBubble(chatArea, "岁月", "我可是高性能AI！有什么需要帮忙的吗？", GlassAccent);

        // 快捷问题按钮
        string[] quickQuestions = { "今天运营怎么样？", "沙能渗透率多少？", "讲个故事吧", "你是谁？" };
        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        btnRow.style.flexWrap = Wrap.Wrap;
        btnRow.style.marginTop = 8;
        win.Add(btnRow);

        foreach (var q in quickQuestions)
        {
            var qb = new Button(() =>
            {
                AddChatBubble(chatArea, "林彪悍", q, GlassText);
                // 岁月回复
                string reply = q switch
                {
                    "今天运营怎么样？" => "根据数据，今天客流稳定，收入比昨天增长了5%！继续加油哦！",
                    "沙能渗透率多少？" => "当前沙能渗透率15%，处于安全范围。但要注意乌金岭方向的广告攻势。",
                    "讲个故事吧" => "从前有一条铁路，它被全世界遗忘了。但有一个人，他没有忘记它……",
                    "你是谁？" => "我叫岁月！是0721号沙子飞猪号搭载的AI原型。2053年制造，沉睡了23年呢。",
                    _ => "嗯……让我想想。",
                };
                AddChatBubble(chatArea, "岁月", reply, GlassAccent);
            }) { text = q };
            qb.style.fontSize = 11; qb.style.height = 28;
            qb.style.unityTextAlign = TextAnchor.MiddleCenter; qb.style.unityFontDefinition = Fd();
            qb.style.backgroundColor = GlassBgSoft; qb.style.color = GlassText;
            qb.style.borderTopLeftRadius = 8; qb.style.borderTopRightRadius = 8;
            qb.style.borderBottomLeftRadius = 8; qb.style.borderBottomRightRadius = 8;
            qb.style.marginRight = 4; qb.style.marginBottom = 4;
            btnRow.Add(qb);
        }
    }

    private void AddChatBubble(VisualElement parent, string sender, string text, Color senderColor)
    {
        var bubble = new VisualElement();
        bubble.style.marginTop = 6;
        bubble.style.backgroundColor = GlassBgSoft;
        bubble.style.borderTopLeftRadius = 8; bubble.style.borderTopRightRadius = 8;
        bubble.style.borderBottomLeftRadius = 8; bubble.style.borderBottomRightRadius = 8;
        bubble.style.paddingLeft = 10; bubble.style.paddingRight = 10;
        bubble.style.paddingTop = 6; bubble.style.paddingBottom = 6;
        parent.Add(bubble);

        var name = new Label(sender);
        name.style.fontSize = 12; name.style.color = senderColor; name.style.unityFontDefinition = Fd();
        bubble.Add(name);

        var msg = new Label(text);
        msg.style.fontSize = 13; msg.style.color = GlassText; msg.style.unityFontDefinition = Fd();
        msg.style.whiteSpace = WhiteSpace.Normal; msg.style.marginTop = 2;
        bubble.Add(msg);
    }

    private VisualElement BuildAppIcon(string name)
    {
        var cell = new VisualElement();
        cell.style.width = 96; cell.style.height = 88;
        cell.style.alignItems = Align.Center;
        cell.style.justifyContent = Justify.FlexEnd;
        cell.style.marginLeft = 14; cell.style.marginRight = 14;
        cell.style.marginTop = 10; cell.style.marginBottom = 10;
        cell.style.paddingBottom = 10;
        cell.pickingMode = PickingMode.Position;

        // 图标主体（液态玻璃风格·渐变色半透明背景）
        var icon = new Label(IconFor(name));
        icon.style.fontSize = 38;
        icon.style.width = 58; icon.style.height = 52;
        icon.style.unityTextAlign = TextAnchor.MiddleCenter;

        // 根据App类型选择背景色
        Color iconBg;
        Color iconFg;
        switch (name)
        {
            case "设置": case "时钟": case "日历": case "天气": case "计算器":
            case "文件管理": case "地图": case "笔记":
                iconBg = new Color(0.25f, 0.25f, 0.3f, 0.6f);  // 系统App：深色半透明
                iconFg = new Color(0.9f, 0.9f, 0.95f, 0.95f);
                break;
            case "铁路运营": case "RDA 助手": case "岁月": case "站务日志": case "列车图鉴":
            case "铁路":
                iconBg = new Color(0.12f, 0.25f, 0.4f, 0.6f);  // 功能App：蓝色半透明
                iconFg = new Color(0.8f, 0.92f, 1f, 0.95f);
                break;
            case "阿里郎商店": case "未来网": case "白头疫苗":
                iconBg = new Color(0.4f, 0.12f, 0.12f, 0.6f);  // 朝鲜App：红色半透明
                iconFg = new Color(1f, 0.85f, 0.8f, 0.95f);
                break;
            default:
                iconBg = new Color(0.2f, 0.2f, 0.25f, 0.55f);
                iconFg = new Color(0.9f, 0.9f, 0.9f, 0.9f);
                break;
        }

        icon.style.color = iconFg;
        icon.style.backgroundColor = iconBg;
        icon.style.borderTopLeftRadius = 16; icon.style.borderTopRightRadius = 16;
        icon.style.borderBottomLeftRadius = 16; icon.style.borderBottomRightRadius = 16;
        icon.style.unityFontDefinition = Fd();
        cell.Add(icon);

        var label = new Label(name);
        label.style.fontSize = 13;
        label.style.color = new Color(0.15f, 0.15f, 0.18f, 0.85f);
        label.style.unityFontDefinition = Fd();
        label.style.marginTop = 6;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        cell.Add(label);

        // hover 放大 + 辉光
        cell.RegisterCallback<PointerEnterEvent>(_ =>
        {
            icon.style.scale = new Scale(new Vector2(1.08f, 1.08f));
            icon.style.backgroundColor = new Color(iconBg.r, iconBg.g, iconBg.b, iconBg.a + 0.15f);
        });
        cell.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            icon.style.scale = new Scale(new Vector2(1f, 1f));
            icon.style.backgroundColor = iconBg;
        });
        cell.RegisterCallback<ClickEvent>(_ => LaunchApp(name));
        return cell;
    }

    private string IconFor(string name)
    {
        switch (name)
        {
            // 第三方App（蓝色）
            case "RDA": return "R";
            case "归途": return "归";
            case "媒姬百科": return "百";
            // 系统App（深色）
            case "相册": return "山";
            case "音乐": return "♪";
            case "笔记": return "記";
            case "设置": return "⚙";
            case "浏览器": return "○";
            case "安全中心": return "盾";
            case "米家": return "⚡";
            case "应用商店": return "☆";
            // 朝鲜App（红色）
            case "阿里郎商店": return "购";
            case "未来网": return "網";
            case "白头疫苗": return "🛡";
            // 工具App（深色）
            case "时钟": return "時";
            case "日历": return "日";
            case "天气": return "天";
            case "计算器": return "計";
            case "文件管理": return "文";
            case "地图": return "地";
            default: return "·";
        }
    }

    // ============ App 层（窗口化） ============
    private VisualElement appLayer;

    private void BuildAppLayer()
    {
        appLayer = new VisualElement { name = "tablet-app-layer" };
        appLayer.style.position = Position.Absolute;
        appLayer.style.top = 0; appLayer.style.left = 0;
        appLayer.style.right = 0; appLayer.style.bottom = 0;
        appLayer.style.display = DisplayStyle.None;
        appLayer.style.alignItems = Align.Center;
        appLayer.style.justifyContent = Justify.Center;
        root.Add(appLayer);
    }

    private void LaunchApp(string name)
    {
        appLayer.Clear();
        // 半透明遮罩（液态玻璃）
        var mask = new VisualElement();
        mask.style.position = Position.Absolute;
        mask.style.top = 0; mask.style.left = 0; mask.style.right = 0; mask.style.bottom = 0;
        mask.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
        mask.pickingMode = PickingMode.Position;
        mask.RegisterCallback<ClickEvent>(e => { if (e.target == mask) appLayer.style.display = DisplayStyle.None; });
        appLayer.Add(mask);

        // 窗口（液态玻璃风格）
        var win = new VisualElement();
        win.style.width = 720; win.style.height = 420;
        win.style.backgroundColor = GlassBg;
        win.style.borderTopWidth = 1; win.style.borderBottomWidth = 1;
        win.style.borderLeftWidth = 1; win.style.borderRightWidth = 1;
        win.style.borderTopColor = GlassBorderSoft; win.style.borderBottomColor = GlassBorderSoft;
        win.style.borderLeftColor = GlassBorderSoft; win.style.borderRightColor = GlassBorderSoft;
        win.style.borderTopLeftRadius = 12; win.style.borderTopRightRadius = 12;
        win.style.borderBottomLeftRadius = 12; win.style.borderBottomRightRadius = 12;
        win.style.paddingLeft = 20; win.style.paddingRight = 20;
        win.style.paddingTop = 16; win.style.paddingBottom = 16;
        appLayer.Add(win);

        // 标题栏
        var titleRow = new VisualElement();
        titleRow.style.flexDirection = FlexDirection.Row;
        titleRow.style.justifyContent = Justify.SpaceBetween;
        titleRow.style.marginBottom = 14;
        win.Add(titleRow);

        var title = new Label(name);
        title.style.fontSize = 22;
        title.style.color = GlassAccent;
        title.style.unityFontDefinition = Fd();
        titleRow.Add(title);

        var closeBtn = new Button(() => { appLayer.style.display = DisplayStyle.None; }) { text = "×" };
        closeBtn.style.width = 32; closeBtn.style.height = 32;
        closeBtn.style.fontSize = 18;
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = Fd();
        closeBtn.style.backgroundColor = new Color(0.3f, 0.3f, 0.35f, 0.6f);
        closeBtn.style.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
        closeBtn.style.borderTopLeftRadius = 16; closeBtn.style.borderTopRightRadius = 16;
        closeBtn.style.borderBottomLeftRadius = 16; closeBtn.style.borderBottomRightRadius = 16;
        titleRow.Add(closeBtn);

        switch (name)
        {
            case "RDA": BuildRDAApp(win); break;
            case "归途": BuildReturnJourneyApp(win); break;
            case "媒姬百科": BuildWikiApp(win); break;
            case "设置": BuildSettingsPage(win); break;
            case "笔记": BuildNotesApp(win); break;
            case "音乐": BuildMusicApp(win); break;
            case "相册": BuildGalleryApp(win); break;
            case "时钟": BuildClockApp(win); break;
            case "日历": BuildCalendarApp(win); break;
            case "天气": BuildWeatherApp(win); break;
            case "计算器": BuildCalculatorApp(win); break;
            case "米家": BuildMijiaApp(win); break;
            case "安全中心": BuildSecurityApp(win); break;
            case "浏览器": BuildBrowserApp(win); break;
            case "应用商店": BuildAppStoreApp(win); break;
            case "阿里郎商店": BuildArirangStoreApp(win); break;
            case "未来网": BuildMiraeNetApp(win); break;
            case "白头疫苗": BuildSiliVaccineApp(win); break;
            case "文件管理": BuildFileManagerApp(win); break;
            case "地图": BuildMapApp(win); break;
            default: BuildPlaceholderApp(win, name); break;
        }

        appLayer.style.display = DisplayStyle.Flex;
    }

    private void StyleMenuBtn(Button btn)
    {
        btn.style.width = 240; btn.style.height = 52;
        btn.style.fontSize = 18; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.backgroundColor = new Color(0.15f, 0.3f, 0.5f, 0.55f);
        btn.style.color = GlassText;
        btn.style.borderTopLeftRadius = 12; btn.style.borderTopRightRadius = 12;
        btn.style.borderBottomLeftRadius = 12; btn.style.borderBottomRightRadius = 12;
        btn.style.marginTop = 16;
        btn.style.alignSelf = Align.Center;
    }

    // ============ 设置 App：备份与恢复（参考小米备份系统） ============
    private const string BackupJsonKey = "SUIYUE_Backup_Json";
    private const string BackupTimeKey = "SUIYUE_Backup_Time";

    private void BuildSettingsPage(VisualElement win)
    {
        var subTitle = new Label("设置");
        subTitle.style.fontSize = 22;
        subTitle.style.color = GlassAccent;
        subTitle.style.unityFontDefinition = Fd();
        win.Add(subTitle);

        var row1 = BuildSettingRow("备份与恢复", "本地备份 / 云备份", ShowBackupRestorePage);
        win.Add(row1);

        var row2 = BuildSettingRow("通用", "本设备已锁定出厂配置", null);
        win.Add(row2);

        // 系统管控彩蛋入口：隐藏条目，只有知道的人才会点
        var row3 = BuildSettingRow("应用管理", "已禁用的系统应用（4个）", ShowAppManagementPage);
        win.Add(row3);
    }

    private VisualElement BuildSettingRow(string title, string desc, System.Action onClick)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.backgroundColor = GlassBgSoft;
        row.style.borderTopLeftRadius = 8; row.style.borderTopRightRadius = 8;
        row.style.borderBottomLeftRadius = 8; row.style.borderBottomRightRadius = 8;
        row.style.paddingLeft = 16; row.style.paddingRight = 16;
        row.style.paddingTop = 12; row.style.paddingBottom = 12;
        row.style.marginTop = 8;
        row.pickingMode = onClick != null ? PickingMode.Position : PickingMode.Ignore;
        if (onClick != null)
        {
            row.RegisterCallback<ClickEvent>(_ => onClick());
            row.RegisterCallback<PointerEnterEvent>(_ => row.style.backgroundColor = GlassBgHover);
            row.RegisterCallback<PointerLeaveEvent>(_ => row.style.backgroundColor = GlassBgSoft);
        }

        var left = new Label(title);
        left.style.fontSize = 17;
        left.style.color = GlassText;
        left.style.unityFontDefinition = Fd();
        row.Add(left);

        var right = new Label(desc);
        right.style.fontSize = 13;
        right.style.color = GlassTextDim;
        right.style.unityFontDefinition = Fd();
        row.Add(right);
        return row;
    }

    private void ShowBackupRestorePage()
    {
        var win = BuildInnerPanel("备份与恢复");
        win.style.height = 480;

        var tip = new Label("参考小米备份系统：将阅读进度与经营数据备份到本机");
        tip.style.fontSize = 14;
        tip.style.color = GlassTextDim;
        tip.style.unityFontDefinition = Fd();
        tip.style.marginTop = 10;
        win.Add(tip);

        // 备份状态行
        var statusLabel = new Label(GetBackupStatusText());
        statusLabel.style.fontSize = 15;
        statusLabel.style.color = new Color(0.7f, 0.9f, 0.7f, 0.85f);
        statusLabel.style.unityFontDefinition = Fd();
        statusLabel.style.marginTop = 14;
        win.Add(statusLabel);

        var btnBackup = new Button(() =>
        {
            SaveBackup();
            statusLabel.text = GetBackupStatusText();
            ShowBackupToast("备份完成");
        }) { text = "立即备份" };
        StyleRowBtn(btnBackup);
        win.Add(btnBackup);

        var btnRestore = new Button(() =>
        {
            if (string.IsNullOrEmpty(PlayerPrefs.GetString(BackupJsonKey, "")))
            {
                ShowBackupToast("暂无备份可恢复");
                return;
            }
            RestoreBackup();
            ShowBackupToast("恢复完成");
        }) { text = "恢复上次备份" };
        StyleRowBtn(btnRestore);
        win.Add(btnRestore);

        var cloudRow = BuildSettingRow("云备份", "阿里郎内网账户（未开放）", null);
        win.Add(cloudRow);

        // 版本信息
        var verLabel = new Label("澎湃OS 32 · DPRK Edition");
        verLabel.style.fontSize = 12;
        verLabel.style.color = GlassTextMuted;
        verLabel.style.unityFontDefinition = Fd();
        verLabel.style.marginTop = 20;
        win.Add(verLabel);
    }

    private void StyleRowBtn(Button btn)
    {
        btn.style.width = 220; btn.style.height = 44;
        btn.style.fontSize = 16; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.backgroundColor = new Color(0.2f, 0.35f, 0.2f, 0.55f);
        btn.style.color = new Color(0.85f, 1f, 0.85f, 0.9f);
        btn.style.borderTopLeftRadius = 10; btn.style.borderTopRightRadius = 10;
        btn.style.borderBottomLeftRadius = 10; btn.style.borderBottomRightRadius = 10;
        btn.style.marginTop = 16;
        btn.style.alignSelf = Align.Center;
    }

    private Label backupToast;
    private void ShowBackupToast(string msg)
    {
        if (backupToast == null)
        {
            backupToast = new Label(msg);
            backupToast.style.position = Position.Absolute;
            backupToast.style.bottom = 90;
            backupToast.style.left = 100; backupToast.style.right = 100;
            backupToast.style.height = 44;
            backupToast.style.fontSize = 16;
            backupToast.style.color = new Color(1f, 0.9f, 0.6f, 0.95f);
            backupToast.style.unityTextAlign = TextAnchor.MiddleCenter;
            backupToast.style.unityFontDefinition = Fd();
            backupToast.style.backgroundColor = GlassBg;
            backupToast.style.borderTopLeftRadius = 12; backupToast.style.borderTopRightRadius = 12;
            backupToast.style.borderBottomLeftRadius = 12; backupToast.style.borderBottomRightRadius = 12;
            backupToast.pickingMode = PickingMode.Ignore;
            root.Add(backupToast);
        }
        else
        {
            backupToast.text = msg;
        }
        backupToast.style.display = DisplayStyle.Flex;
        backupToast.schedule.Execute(() => { if (backupToast != null) backupToast.style.display = DisplayStyle.None; }).ExecuteLater(1500);
    }

    private string GetBackupStatusText()
    {
        string t = PlayerPrefs.GetString(BackupTimeKey, "");
        return string.IsNullOrEmpty(t) ? "尚未备份" : "上次备份：" + t;
    }

    private void SaveBackup()
    {
        // 收集所有存档键（VN 存档 + 经营存档 + 书签），快照到单一备份 JSON
        var snapshot = new System.Text.StringBuilder();
        var keys = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 61; i++) keys.Add("VN_Save_" + i);
        for (int i = 0; i < 6; i++) keys.Add("SaveSlot_" + i);
        keys.Add("Achievements_Data");
        keys.Add("Bookmarks_Data");
        keys.Add("VN_AutoLoad");
        keys.Add("VNExitData");

        foreach (var k in keys)
        {
            if (PlayerPrefs.HasKey(k))
                snapshot.Append(k).Append("=").Append(PlayerPrefs.GetString(k)).Append("\n");
        }
        PlayerPrefs.SetString(BackupJsonKey, snapshot.ToString());
        PlayerPrefs.SetString(BackupTimeKey, System.DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
        PlayerPrefs.Save();
    }

    private void RestoreBackup()
    {
        string json = PlayerPrefs.GetString(BackupJsonKey, "");
        if (string.IsNullOrEmpty(json)) return;
        foreach (var line in json.Split('\n'))
        {
            if (string.IsNullOrEmpty(line)) continue;
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            string k = line.Substring(0, eq);
            string v = line.Substring(eq + 1);
            if (k.StartsWith("VN_Save_") || k.StartsWith("SaveSlot_"))
            {
                PlayerPrefs.SetString(k, v);
            }
        }
        PlayerPrefs.Save();
    }

    // ============ 应用管理：系统管控彩蛋 ============
    private void ShowAppManagementPage()
    {
        var win = BuildInnerPanel("应用管理");
        win.style.height = 480;

        // 说明文字
        var tip = new Label("已禁用的系统应用（4个）");
        tip.style.fontSize = 15;
        tip.style.color = GlassTextDim;
        tip.style.unityFontDefinition = Fd();
        tip.style.marginTop = 10;
        win.Add(tip);

        var tip2 = new Label("以下应用因系统管控已被替换。点击可尝试恢复原版。");
        tip2.style.fontSize = 13;
        tip2.style.color = GlassTextMuted;
        tip2.style.unityFontDefinition = Fd();
        tip2.style.marginTop = 4;
        win.Add(tip2);

        var apps = SystemAppsEasterEgg.Apps;
        for (int i = 0; i < apps.Length; i++)
        {
            int idx = i;
            var app = apps[i];
            bool restored = SystemAppsEasterEgg.IsAppRestored(idx);
            bool capsuleTriggered = SystemAppsEasterEgg.IsCapsuleTriggered();

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = GlassBgSoft;
            row.style.borderTopLeftRadius = 8; row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8; row.style.borderBottomRightRadius = 8;
            row.style.paddingLeft = 16; row.style.paddingRight = 16;
            row.style.paddingTop = 10; row.style.paddingBottom = 10;
            row.style.marginTop = 6;

            // 左侧：应用信息
            var left = new VisualElement();
            left.style.flexDirection = FlexDirection.Column;
            row.Add(left);

            var appName = new Label(app.OriginalName);
            appName.style.fontSize = 16;
            appName.style.color = restored
                ? new Color(0.5f, 0.8f, 0.5f, 0.9f)
                : GlassText;
            appName.style.unityFontDefinition = Fd();
            left.Add(appName);

            var appStatus = new Label(restored ? "已恢复" : $"当前：{app.ReplacedName}");
            appStatus.style.fontSize = 12;
            appStatus.style.color = GlassTextMuted;
            appStatus.style.unityFontDefinition = Fd();
            appStatus.style.marginTop = 2;
            left.Add(appStatus);

            // 右侧：操作按钮
            if (capsuleTriggered)
            {
                var locked = new Label("已重置");
                locked.style.fontSize = 13;
                locked.style.color = GlassTextMuted;
                locked.style.unityFontDefinition = Fd();
                row.Add(locked);
            }
            else if (restored)
            {
                var check = new Label("✓");
                check.style.fontSize = 20;
                check.style.color = new Color(0.5f, 0.8f, 0.5f, 0.85f);
                check.style.unityFontDefinition = Fd();
                row.Add(check);
            }
            else
            {
                var btn = new Button(() => OnRestoreApp(idx, win)) { text = "恢复" };
                btn.style.width = 76; btn.style.height = 34;
                btn.style.fontSize = 14;
                btn.style.unityTextAlign = TextAnchor.MiddleCenter;
                btn.style.unityFontDefinition = Fd();
                btn.style.backgroundColor = new Color(0.2f, 0.4f, 0.6f, 0.6f);
                btn.style.color = GlassAccent;
                btn.style.borderTopLeftRadius = 8; btn.style.borderTopRightRadius = 8;
                btn.style.borderBottomLeftRadius = 8; btn.style.borderBottomRightRadius = 8;
                row.Add(btn);
            }

            win.Add(row);
        }

        // 时间胶囊状态提示
        int restoredCount = SystemAppsEasterEgg.GetRestoredCount();
        if (restoredCount > 0 && !SystemAppsEasterEgg.IsCapsuleTriggered())
        {
            var hint = new Label($"已恢复 {restoredCount}/4 个应用");
            hint.style.fontSize = 14;
            hint.style.color = new Color(1f, 0.7f, 0.4f, 0.7f);
            hint.style.unityFontDefinition = Fd();
            hint.style.marginTop = 16;
            win.Add(hint);
        }
    }

    private void OnRestoreApp(int index, VisualElement settingsWin)
    {
        var result = SystemAppsEasterEgg.RestoreApp(index);

        switch (result)
        {
            case SystemAppsEasterEgg.RestoreResult.AlreadyRestored:
                ShowAppToast("该应用已恢复");
                break;

            case SystemAppsEasterEgg.RestoreResult.Success:
                // 播放闪现动画 → 弹窗 → 跳转
                StartCoroutine(PlayRestoreAnimation(index, settingsWin));
                break;

            case SystemAppsEasterEgg.RestoreResult.CapsuleTriggered:
                // 时间胶囊触发
                StartCoroutine(PlayTimeCapsule(settingsWin));
                break;
        }
    }

    private IEnumerator PlayRestoreAnimation(int index, VisualElement settingsWin)
    {
        var app = SystemAppsEasterEgg.Apps[index];

        // 第一阶段：闪现原版Logo 0.3秒
        ShowAppToast($"[闪现] {app.FlashText}...");
        yield return new WaitForSecondsRealtime(0.3f);

        // 第二阶段：黑屏0.5秒
        ShowAppToast("");
        yield return new WaitForSecondsRealtime(0.5f);

        // 第三阶段：弹窗
        if (app.JumpTarget != null)
        {
            ShowAppToast($"⚠ {app.PopupMessage}\n→ 跳转至「{app.JumpTarget}」");
        }
        else
        {
            ShowAppToast($"⚠ {app.PopupMessage}\n（无可用服务）");
        }
        yield return new WaitForSecondsRealtime(2f);

        // 刷新应用管理页面
        ShowAppToast("");
        ShowAppManagementPage();
    }

    private IEnumerator PlayTimeCapsule(VisualElement settingsWin)
    {
        // 黑屏
        ShowAppToast("");
        yield return new WaitForSecondsRealtime(1f);

        // 时间胶囊第一行字
        ShowAppToast(SystemAppsEasterEgg.TimeCapsule_Line1);
        yield return new WaitForSecondsRealtime(3f);

        // 第二行字
        ShowAppToast(SystemAppsEasterEgg.TimeCapsule_Line2);
        yield return new WaitForSecondsRealtime(3f);

        // 恢复出厂设置
        SystemAppsEasterEgg.FactoryReset();
        ShowAppToast("系统已恢复出厂设置。所有应用已被重新替换。");
        yield return new WaitForSecondsRealtime(2f);

        ShowAppToast("");
        // 刷新页面
        ShowAppManagementPage();
    }

    private Label appToast;
    private void ShowAppToast(string msg)
    {
        if (appToast == null)
        {
            appToast = new Label(msg);
            appToast.style.position = Position.Absolute;
            appToast.style.bottom = 120;
            appToast.style.left = 80; appToast.style.right = 80;
            appToast.style.height = 80;
            appToast.style.fontSize = 15;
            appToast.style.color = GlassText;
            appToast.style.unityFontDefinition = Fd();
            appToast.style.unityTextAlign = TextAnchor.MiddleCenter;
            appToast.style.backgroundColor = GlassBg;
            appToast.style.borderTopLeftRadius = 12;
            appToast.style.borderTopRightRadius = 12;
            appToast.style.borderBottomLeftRadius = 12;
            appToast.style.borderBottomRightRadius = 12;
            appToast.style.paddingLeft = 14;
            appToast.style.paddingRight = 14;
            appToast.style.paddingTop = 10;
            appToast.style.paddingBottom = 10;
            appToast.pickingMode = PickingMode.Ignore;
            root.Add(appToast);
        }
        appToast.text = msg;
        appToast.style.display = string.IsNullOrEmpty(msg) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void ShowAffairsPage()
    {
        // 事务页（简单占位）
        var panel = BuildInnerPanel("事务");
        var p = new Label("事务功能开发中（绑定经营系统后开放）。");
        p.style.fontSize = 15; p.style.color = GlassTextDim;
        p.style.unityFontDefinition = Fd(); p.style.marginTop = 40;
        panel.Add(p);
    }

    private void ShowStoryPage()
    {
        var panel = BuildInnerPanel("剧情");
        var btn = new Button(() => { gameObject.SetActive(true); MainStoryUI.Show(); }) { text = "打开 Main Story" };
        btn.style.width = 240; btn.style.height = 50;
        btn.style.fontSize = 18; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.backgroundColor = new Color(0.15f, 0.3f, 0.5f, 0.55f);
        btn.style.color = GlassText;
        btn.style.borderTopLeftRadius = 12; btn.style.borderTopRightRadius = 12;
        btn.style.borderBottomLeftRadius = 12; btn.style.borderBottomRightRadius = 12;
        btn.style.marginTop = 50;
        btn.style.alignSelf = Align.Center;
        panel.Add(btn);
    }

    // ============ App: RDA（铁路行车调度助手）============
    private void BuildRDAApp(VisualElement win)
    {
        var sub = new Label("铁路行车调度助手 · RDA v1.0");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        var btnStory = new Button(() => { appLayer.style.display = DisplayStyle.None; ShowStoryPage(); }) { text = "剧情 · Main Story" };
        StyleMenuBtn(btnStory); win.Add(btnStory);

        var btnAffairs = new Button(() => { appLayer.style.display = DisplayStyle.None; ShowAffairsPage(); }) { text = "事务管理" };
        StyleMenuBtn(btnAffairs); win.Add(btnAffairs);

        var tip = new Label("嘉颖徐铁路基金 · 内部分发版");
        tip.style.fontSize = 12; tip.style.color = GlassTextMuted; tip.style.unityFontDefinition = Fd();
        tip.style.marginTop = 16; win.Add(tip);
    }

    // ============ App: 归途（剧情回顾）============
    private void BuildReturnJourneyApp(VisualElement win)
    {
        var sub = new Label("Return Journey · 剧情回顾");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        string[] chapters = { "序章·归乡", "序章·启程", "第一章·求生", "第二章·扩张", "第三章·对抗", "终章·复兴" };
        for (int i = 0; i < chapters.Length; i++)
        {
            int idx = i;
            bool unlocked = idx <= 1; // 前两章已解锁
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            row.style.backgroundColor = GlassBgSoft; row.style.borderTopLeftRadius = 8; row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8; row.style.borderBottomRightRadius = 8;
            row.style.paddingLeft = 14; row.style.paddingRight = 14; row.style.paddingTop = 10; row.style.paddingBottom = 10;
            row.style.marginTop = 6;
            if (unlocked) row.pickingMode = PickingMode.Position;
            win.Add(row);

            var num = new Label($"#{idx + 1}");
            num.style.fontSize = 16; num.style.color = unlocked ? GlassAccent : GlassTextMuted;
            num.style.unityFontDefinition = Fd(); num.style.width = 40; row.Add(num);

            var ch = new Label(chapters[idx]);
            ch.style.fontSize = 15; ch.style.color = unlocked ? GlassText : GlassTextMuted;
            ch.style.unityFontDefinition = Fd(); row.Add(ch);

            if (!unlocked)
            {
                var lockIcon = new Label("🔒");
                lockIcon.style.fontSize = 14; lockIcon.style.marginLeft = 8; row.Add(lockIcon);
            }
        }

        var tip = new Label("嘉颖徐命名：「铁路最动人的不是出发，是回来。每一趟归途，都是一次胜利。」");
        tip.style.fontSize = 12; tip.style.color = GlassTextMuted; tip.style.unityFontDefinition = Fd();
        tip.style.marginTop = 12; tip.style.whiteSpace = WhiteSpace.Normal; win.Add(tip);
    }

    // ============ App: 媒姬百科（铁路百科）============
    private void BuildWikiApp(VisualElement win)
    {
        var sub = new Label("Meiji Wiki · 铁路知识百科");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        string[][] entries = {
            new[]{"林彪悍", "25岁，金日成综合大学荣誉研究生，智能调度系统方向"},
            new[]{"老陈", "68岁，雾峰村线最后的守护者，守线四年"},
            new[]{"岁月", "AI原型，2053年制造，沉睡23年后重启"},
            new[]{"NF-5耕牛", "1965年定型，1985年调至雾峰村线，2072年停运"},
            new[]{"雾峰村-矿区线", "23公里，4站，茶叶+煤矿运输"},
            new[]{"沙能", "2050年诞生，2072年全球铁路停运"},
            new[]{"铁龙计划", "USET暗面，以保护之名行吞并之实"},
        };
        foreach (var e in entries)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Column;
            row.style.backgroundColor = GlassBgSoft; row.style.borderTopLeftRadius = 8; row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8; row.style.borderBottomRightRadius = 8;
            row.style.paddingLeft = 14; row.style.paddingRight = 14; row.style.paddingTop = 8; row.style.paddingBottom = 8;
            row.style.marginTop = 6; win.Add(row);

            var title = new Label(e[0]);
            title.style.fontSize = 15; title.style.color = GlassAccent; title.style.unityFontDefinition = Fd();
            row.Add(title);
            var desc = new Label(e[1]);
            desc.style.fontSize = 13; desc.style.color = GlassTextDim; desc.style.unityFontDefinition = Fd();
            desc.style.marginTop = 2; desc.style.whiteSpace = WhiteSpace.Normal; row.Add(desc);
        }
    }

    // ============ App: 笔记 ============
    private void BuildNotesApp(VisualElement win)
    {
        var sub = new Label("笔记 · 书签 / 统计 / 待办");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        var note1 = BuildSettingRow("运营统计", "本月客流 1,247 人 · 收入 43,800 沙币", null);
        win.Add(note1);
        var note2 = BuildSettingRow("待办事项", "3 项未完成", null);
        win.Add(note2);
        var note3 = BuildSettingRow("成就里程碑", "首次发车 · 运营30天 · 首次盈利", null);
        win.Add(note3);
        var note4 = BuildSettingRow("快速笔记", "点击新建笔记...", null);
        win.Add(note4);
    }

    // ============ App: 音乐 ============
    private void BuildMusicApp(VisualElement win)
    {
        var sub = new Label("音乐 · BGM 播放器");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        var nowPlaying = new Label("♪ 正在播放：云铁（Cloud Rail）");
        nowPlaying.style.fontSize = 16; nowPlaying.style.color = GlassText; nowPlaying.style.unityFontDefinition = Fd();
        nowPlaying.style.marginTop = 12; win.Add(nowPlaying);

        // 进度条占位
        var progress = new VisualElement();
        progress.style.height = 4; progress.style.marginTop = 10;
        progress.style.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
        progress.style.borderTopLeftRadius = 2; progress.style.borderTopRightRadius = 2;
        win.Add(progress);
        var fill = new VisualElement();
        fill.style.width = new Length(65, LengthUnit.Percent); fill.style.height = 4;
        fill.style.backgroundColor = GlassAccent;
        fill.style.borderTopLeftRadius = 2; fill.style.borderTopRightRadius = 2;
        progress.Add(fill);

        var time = new Label("1:23 / 3:45");
        time.style.fontSize = 12; time.style.color = GlassTextMuted; time.style.unityFontDefinition = Fd();
        time.style.marginTop = 4; win.Add(time);

        string[] tracks = { "云铁 (Cloud Rail)", "第一缕光 (First Light)", "余烬 (Embers)", "月台 (Platform)", "决心 (Determination)" };
        foreach (var t in tracks)
        {
            var row = BuildSettingRow(t, "", null);
            win.Add(row);
        }
    }

    // ============ App: 相册 ============
    private void BuildGalleryApp(VisualElement win)
    {
        var sub = new Label("相册 · CG / 立绘 / 风景");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        string[] categories = { "CG收藏 (2/12)", "角色立绘 (5/20)", "风景场景 (3/8)", "截图 (0)" };
        foreach (var c in categories)
        {
            var row = BuildSettingRow(c, "", null);
            win.Add(row);
        }
    }

    // ============ App: 时钟 ============
    private void BuildClockApp(VisualElement win)
    {
        var time = new Label(System.DateTime.Now.ToString("HH:mm:ss"));
        time.style.fontSize = 48; time.style.color = GlassText; time.style.unityFontDefinition = Fd();
        time.style.marginTop = 20; time.style.alignSelf = Align.Center; win.Add(time);

        var date = new Label("运营第 1 天");
        date.style.fontSize = 18; date.style.color = GlassAccent; date.style.unityFontDefinition = Fd();
        date.style.marginTop = 8; date.style.alignSelf = Align.Center; win.Add(date);

        var cal = new Label("主体历 105 年 1 月 1 日");
        cal.style.fontSize = 14; cal.style.color = GlassTextDim; cal.style.unityFontDefinition = Fd();
        cal.style.marginTop = 4; cal.style.alignSelf = Align.Center; win.Add(cal);
    }

    // ============ App: 日历 ============
    private void BuildCalendarApp(VisualElement win)
    {
        var sub = new Label("日历 · 主体历 / 公历");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        var month = new Label("105年 1月（公历2076年1月）");
        month.style.fontSize = 16; month.style.color = GlassText; month.style.unityFontDefinition = Fd();
        month.style.marginTop = 10; win.Add(month);

        string[] days = { "一", "二", "三", "四", "五", "六", "日" };
        var header = new VisualElement(); header.style.flexDirection = FlexDirection.Row;
        header.style.marginTop = 8; win.Add(header);
        foreach (var d in days)
        {
            var dl = new Label(d);
            dl.style.width = 80; dl.style.fontSize = 13; dl.style.color = GlassTextMuted;
            dl.style.unityFontDefinition = Fd(); dl.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(dl);
        }

        for (int w = 0; w < 4; w++)
        {
            var week = new VisualElement(); week.style.flexDirection = FlexDirection.Row;
            week.style.marginTop = 2; win.Add(week);
            for (int d = 0; d < 7; d++)
            {
                int day = w * 7 + d + 1;
                if (day > 31) break;
                var dl = new Label(day.ToString());
                dl.style.width = 80; dl.style.height = 32; dl.style.fontSize = 14;
                dl.style.color = day == 1 ? GlassAccent : GlassText;
                dl.style.unityFontDefinition = Fd(); dl.style.unityTextAlign = TextAnchor.MiddleCenter;
                if (day == 1) { dl.style.backgroundColor = GlassBgSoft; dl.style.borderTopLeftRadius = 6; dl.style.borderTopRightRadius = 6; dl.style.borderBottomLeftRadius = 6; dl.style.borderBottomRightRadius = 6; }
                week.Add(dl);
            }
        }
    }

    // ============ App: 天气 ============
    private void BuildWeatherApp(VisualElement win)
    {
        var temp = new Label("18°C");
        temp.style.fontSize = 48; temp.style.color = GlassText; temp.style.unityFontDefinition = Fd();
        temp.style.marginTop = 10; temp.style.alignSelf = Align.Center; win.Add(temp);

        var cond = new Label("多云 · 雾峰村");
        cond.style.fontSize = 16; cond.style.color = GlassTextDim; cond.style.unityFontDefinition = Fd();
        cond.style.marginTop = 4; cond.style.alignSelf = Align.Center; win.Add(cond);

        var impact = new Label("天气影响：沙能载具性能正常，铁路运营无影响");
        impact.style.fontSize = 13; impact.style.color = GlassTextMuted; impact.style.unityFontDefinition = Fd();
        impact.style.marginTop = 12; impact.style.alignSelf = Align.Center; win.Add(impact);

        var forecast = new Label("明日：晴 21°C  ·  后日：小雨 15°C");
        forecast.style.fontSize = 13; forecast.style.color = GlassTextMuted; forecast.style.unityFontDefinition = Fd();
        forecast.style.marginTop = 6; forecast.style.alignSelf = Align.Center; win.Add(forecast);
    }

    // ============ App: 计算器 ============
    private void BuildCalculatorApp(VisualElement win)
    {
        var display = new Label("0");
        display.style.fontSize = 32; display.style.color = GlassText; display.style.unityFontDefinition = Fd();
        display.style.marginTop = 10; display.style.alignSelf = Align.End; display.style.marginRight = 10;
        display.style.height = 50; display.style.alignItems = Align.FlexEnd; win.Add(display);

        string[][] rows = {
            new[]{"7","8","9","÷"},
            new[]{"4","5","6","×"},
            new[]{"1","2","3","-"},
            new[]{"0",".","=","+"},
        };
        foreach (var row in rows)
        {
            var btnRow = new VisualElement(); btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.marginTop = 4; btnRow.style.justifyContent = Justify.Center; win.Add(btnRow);
            foreach (var b in row)
            {
                var btn = new Button(() => { }) { text = b };
                btn.style.width = 70; btn.style.height = 40; btn.style.fontSize = 18;
                btn.style.unityTextAlign = TextAnchor.MiddleCenter; btn.style.unityFontDefinition = Fd();
                btn.style.backgroundColor = b == "=" ? new Color(0.2f, 0.4f, 0.6f, 0.6f) : GlassBgSoft;
                btn.style.color = b == "=" ? GlassAccent : GlassText;
                btn.style.borderTopLeftRadius = 8; btn.style.borderTopRightRadius = 8;
                btn.style.borderBottomLeftRadius = 8; btn.style.borderBottomRightRadius = 8;
                btn.style.marginLeft = 4; btnRow.Add(btn);
            }
        }
    }

    // ============ App: 米家（0721状态）============
    private void BuildMijiaApp(VisualElement win)
    {
        var sub = new Label("米家 · 0721 沙子飞猪号");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        var rows = new (string label, string value, Color color)[] {
            ("沙子余量", "1,200 / 1,500 kg", GlassAccent),
            ("电池电量", "78%", new Color(0.5f, 0.8f, 0.5f, 0.9f)),
            ("剩余里程", "340 km", GlassText),
            ("本月消耗", "312 kg 沙子", GlassTextDim),
            ("上次维护", "2076.01.10", GlassTextDim),
        };
        foreach (var r in rows)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween; row.style.alignItems = Align.Center;
            row.style.backgroundColor = GlassBgSoft; row.style.borderTopLeftRadius = 8; row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8; row.style.borderBottomRightRadius = 8;
            row.style.paddingLeft = 14; row.style.paddingRight = 14; row.style.paddingTop = 8; row.style.paddingBottom = 8;
            row.style.marginTop = 6; win.Add(row);
            var l = new Label(r.label); l.style.fontSize = 14; l.style.color = GlassTextDim; l.style.unityFontDefinition = Fd(); row.Add(l);
            var v = new Label(r.value); v.style.fontSize = 14; v.style.color = r.color; v.style.unityFontDefinition = Fd(); row.Add(v);
        }
    }

    // ============ App: 安全中心 ============
    private void BuildSecurityApp(VisualElement win)
    {
        var shield = new Label("🛡");
        shield.style.fontSize = 48; shield.style.color = GlassAccent; shield.style.unityFontDefinition = Fd();
        shield.style.marginTop = 16; shield.style.alignSelf = Align.Center; win.Add(shield);

        var status = new Label("系统安全 · 无异常");
        status.style.fontSize = 16; status.style.color = new Color(0.5f, 0.8f, 0.5f, 0.9f);
        status.style.unityFontDefinition = Fd(); status.style.marginTop = 8;
        status.style.alignSelf = Align.Center; win.Add(status);

        var score = new Label("安全评分：100 分");
        score.style.fontSize = 14; score.style.color = GlassTextDim; score.style.unityFontDefinition = Fd();
        score.style.marginTop = 4; score.style.alignSelf = Align.Center; win.Add(score);

        var scanBtn = new Button(() => { status.text = "正在扫描..."; status.schedule.Execute(() => { status.text = "系统安全 · 无异常"; }).ExecuteLater(1500); }) { text = "立即扫描" };
        StyleMenuBtn(scanBtn); scanBtn.style.marginTop = 20; win.Add(scanBtn);
    }

    // ============ App: 浏览器 ============
    private void BuildBrowserApp(VisualElement win)
    {
        var bar = new VisualElement(); bar.style.flexDirection = FlexDirection.Row;
        bar.style.backgroundColor = GlassBgSoft; bar.style.borderTopLeftRadius = 8; bar.style.borderTopRightRadius = 8;
        bar.style.borderBottomLeftRadius = 8; bar.style.borderBottomRightRadius = 8;
        bar.style.paddingLeft = 10; bar.style.paddingRight = 10; bar.style.paddingTop = 6; bar.style.paddingBottom = 6;
        bar.style.marginTop = 8; win.Add(bar);
        var url = new Label("jiayingxu.railway基金会.org");
        url.style.fontSize = 13; url.style.color = GlassTextDim; url.style.unityFontDefinition = Fd(); bar.Add(url);

        var title = new Label("嘉颖徐铁路基金");
        title.style.fontSize = 18; title.style.color = GlassAccent; title.style.unityFontDefinition = Fd();
        title.style.marginTop = 12; win.Add(title);

        var desc = new Label("嘉颖徐铁路基金致力于保护和复兴全球铁路文化遗产。\n\n「铁路最动人的不是出发，是回来。」");
        desc.style.fontSize = 14; desc.style.color = GlassText; desc.style.unityFontDefinition = Fd();
        desc.style.marginTop = 8; desc.style.whiteSpace = WhiteSpace.Normal; win.Add(desc);

        var dlBtn = new Button(() => { appLayer.style.display = DisplayStyle.None; }) { text = "下载 RDA 助手" };
        StyleMenuBtn(dlBtn); dlBtn.style.marginTop = 16; win.Add(dlBtn);
    }

    // ============ App: 应用商店 ============
    private void BuildAppStoreApp(VisualElement win)
    {
        var sub = new Label("应用商店 · 仅提供经审核应用");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        string[] apps = { "铁路工程计算器 v2.1", "列车时刻表 v1.5", "轨道检测工具 v3.0", "铁路英语词典 v1.0" };
        foreach (var a in apps)
        {
            var row = BuildSettingRow(a, "已安装", null);
            win.Add(row);
        }

        var banner = new Label("本商店仅提供经审核的铁路相关应用");
        banner.style.fontSize = 11; banner.style.color = GlassTextMuted; banner.style.unityFontDefinition = Fd();
        banner.style.marginTop = 12; banner.style.alignSelf = Align.Center; win.Add(banner);
    }

    // ============ App: 阿里郎商店 ============
    private void BuildArirangStoreApp(VisualElement win)
    {
        var sub = new Label("阿里郎商店 · 数字签名验证");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        string[] apps = { "阿里郎新闻 v1.0", "主体百科 v2.0", "朝鲜音乐 v1.3", "平壤地图 v1.1", "白头教育 v3.0" };
        foreach (var a in apps)
        {
            var row = BuildSettingRow(a, "★★★★★", null);
            win.Add(row);
        }

        var warning = new Label("⚠ 所有应用均通过数字签名验证\n禁止安装未经验证的应用程序");
        warning.style.fontSize = 11; warning.style.color = new Color(0.8f, 0.4f, 0.3f, 0.7f);
        warning.style.unityFontDefinition = Fd(); warning.style.marginTop = 10;
        warning.style.whiteSpace = WhiteSpace.Normal; win.Add(warning);
    }

    // ============ App: 未来网 ============
    private void BuildMiraeNetApp(VisualElement win)
    {
        var bar = new VisualElement(); bar.style.flexDirection = FlexDirection.Row;
        bar.style.backgroundColor = GlassBgSoft; bar.style.borderTopLeftRadius = 8; bar.style.borderTopRightRadius = 8;
        bar.style.borderBottomLeftRadius = 8; bar.style.borderBottomRightRadius = 8;
        bar.style.paddingLeft = 10; bar.style.paddingRight = 10; bar.style.paddingTop = 6; bar.style.paddingBottom = 6;
        bar.style.marginTop = 8; win.Add(bar);
        var url = new Label("mirae.kp");
        url.style.fontSize = 13; url.style.color = GlassTextDim; url.style.unityFontDefinition = Fd(); bar.Add(url);

        string[] sites = { "阿里郎新闻 · 官方媒体", "沙能会社门户 · USET企业", "金日成综合大学 · 校园内网", "主体百科 · 知识平台" };
        foreach (var s in sites)
        {
            var row = BuildSettingRow(s, "可访问", null);
            win.Add(row);
        }

        var blocked = new Label("⚠ 外部网络连接已被防火墙拦截\n仅可访问白名单站点");
        blocked.style.fontSize = 11; blocked.style.color = new Color(0.8f, 0.4f, 0.3f, 0.7f);
        blocked.style.unityFontDefinition = Fd(); blocked.style.marginTop = 10;
        blocked.style.whiteSpace = WhiteSpace.Normal; win.Add(blocked);
    }

    // ============ App: 白头疫苗 ============
    private void BuildSiliVaccineApp(VisualElement win)
    {
        var shield = new Label("🛡");
        shield.style.fontSize = 40; shield.style.color = GlassAccent; shield.style.unityFontDefinition = Fd();
        shield.style.marginTop = 12; shield.style.alignSelf = Align.Center; win.Add(shield);

        var status = new Label("系统安全 · 未发现威胁");
        status.style.fontSize = 16; status.style.color = new Color(0.5f, 0.8f, 0.5f, 0.9f);
        status.style.unityFontDefinition = Fd(); status.style.marginTop = 8;
        status.style.alignSelf = Align.Center; win.Add(status);

        var info = new Label("病毒库大小：0 KB\n上次更新：2076.01.01\n实时防护：已开启（无法关闭）");
        info.style.fontSize = 12; info.style.color = GlassTextMuted; info.style.unityFontDefinition = Fd();
        info.style.marginTop = 10; info.style.whiteSpace = WhiteSpace.Normal;
        info.style.alignSelf = Align.Center; win.Add(info);

        var scanBtn = new Button(() => { status.text = "正在扫描..."; status.schedule.Execute(() => { status.text = "系统安全 · 未发现威胁"; }).ExecuteLater(1500); }) { text = "立即扫描" };
        StyleMenuBtn(scanBtn); scanBtn.style.marginTop = 14; win.Add(scanBtn);
    }

    // ============ App: 文件管理 ============
    private void BuildFileManagerApp(VisualElement win)
    {
        var sub = new Label("文件管理");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        string[] folders = { "文档 (12)", "图片 (38)", "音频 (8)", "视频 (3)", "存档 (6)" };
        foreach (var f in folders)
        {
            var row = BuildSettingRow(f, "", null);
            win.Add(row);
        }
    }

    // ============ App: 地图 ============
    private void BuildMapApp(VisualElement win)
    {
        var sub = new Label("地图 · 线路总览");
        sub.style.fontSize = 14; sub.style.color = GlassTextDim; sub.style.unityFontDefinition = Fd();
        win.Add(sub);

        // 简化的线路图
        var map = new Label(
            "  雾峰村 ─── 8km ─── 茶山站 ─── 7km ─── 松桥站 ─── 8km ─── 矿区站\n" +
            "  │\n" +
            "  └── 45km ─── 青溪镇（规划中）\n" +
            "       └── 80km ─── 云渡港（待解锁）");
        map.style.fontSize = 13; map.style.color = GlassText; map.style.unityFontDefinition = Fd();
        map.style.marginTop = 10; map.style.whiteSpace = WhiteSpace.Pre;
        win.Add(map);

        var tip = new Label("沙能渗透率：雾峰村 15% · 乌金岭 20% · 青溪镇 25%");
        tip.style.fontSize = 12; tip.style.color = GlassTextMuted; tip.style.unityFontDefinition = Fd();
        tip.style.marginTop = 12; win.Add(tip);
    }

    // ============ App: 占位符 ============
    private void BuildPlaceholderApp(VisualElement win, string name)
    {
        var body = new Label($"{name}：该应用暂未接入。");
        body.style.fontSize = 16; body.style.color = GlassText; body.style.unityFontDefinition = Fd();
        body.style.marginTop = 60; body.style.alignSelf = Align.Center; win.Add(body);
    }

    private VisualElement BuildInnerPanel(string title)
    {
        appLayer.Clear();
        var mask = new VisualElement();
        mask.style.position = Position.Absolute;
        mask.style.top = 0; mask.style.left = 0; mask.style.right = 0; mask.style.bottom = 0;
        mask.style.backgroundColor = GlassMask;
        mask.pickingMode = PickingMode.Position;
        mask.RegisterCallback<ClickEvent>(e => { if (e.target == mask) appLayer.style.display = DisplayStyle.None; });
        appLayer.Add(mask);

        var panel = new VisualElement();
        panel.style.width = 640; panel.style.height = 360;
        panel.style.backgroundColor = GlassBg;
        panel.style.borderTopWidth = 1; panel.style.borderBottomWidth = 1;
        panel.style.borderLeftWidth = 1; panel.style.borderRightWidth = 1;
        panel.style.borderTopColor = GlassBorderSoft; panel.style.borderBottomColor = GlassBorderSoft;
        panel.style.borderLeftColor = GlassBorderSoft; panel.style.borderRightColor = GlassBorderSoft;
        panel.style.borderTopLeftRadius = 16; panel.style.borderTopRightRadius = 16;
        panel.style.borderBottomLeftRadius = 16; panel.style.borderBottomRightRadius = 16;
        panel.style.paddingLeft = 24; panel.style.paddingRight = 24;
        panel.style.paddingTop = 18; panel.style.paddingBottom = 18;
        appLayer.Add(panel);

        var t = new Label(title);
        t.style.fontSize = 22;
        t.style.color = GlassAccent;
        t.style.unityFontDefinition = Fd();
        panel.Add(t);
        return panel;
    }

    private void Update()
    {
        if (root == null || root.style.display == DisplayStyle.None) return;
        // 刷新状态栏时钟
        clockTimer += Time.unscaledDeltaTime;
        if (clockTimer > 1f)
        {
            clockTimer = 0;
            string hhmm = System.DateTime.Now.ToString("HH:mm");
            if (statusClock != null) statusClock.text = hhmm;
            var lc = lockScreen?.Q<Label>("lock-clock");
            if (lc != null) lc.text = hhmm;
        }

        // 岁月Idle气泡（每30-60秒随机显示一句话）
        if (suiyueWidget != null && desktop.style.display == DisplayStyle.Flex)
        {
            suiyueTimer += Time.unscaledDeltaTime;
            if (suiyueTimer > 40f) // 约40秒
            {
                suiyueTimer = 0;
                ShowSuiyueIdleBubble();
            }
        }
    }

    private void ShowSuiyueIdleBubble()
    {
        if (suiyueBubble == null) return;
        suiyueBubble.text = SuiyueIdleLines[suiyueIdleIndex % SuiyueIdleLines.Length];
        suiyueIdleIndex++;
        suiyueBubble.style.display = DisplayStyle.Flex;
        suiyueBubble.style.opacity = 0;
        // 淡入
        suiyueBubble.schedule.Execute(() => { suiyueBubble.style.opacity = 1; }).ExecuteLater(100);
        // 4秒后淡出
        suiyueBubble.schedule.Execute(() =>
        {
            suiyueBubble.style.opacity = 0;
            suiyueBubble.schedule.Execute(() => { suiyueBubble.style.display = DisplayStyle.None; }).ExecuteLater(300);
        }).ExecuteLater(4000);
    }
}