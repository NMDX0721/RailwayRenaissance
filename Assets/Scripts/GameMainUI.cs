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

        // 居中大时钟
        var clock = new Label("12:00");
        clock.name = "lock-clock";
        clock.style.fontSize = 72;
        clock.style.color = Color.white;
        clock.style.unityFontDefinition = Fd();
        clock.style.unityTextOutlineWidth = 4;
        clock.style.unityTextOutlineColor = new Color(0, 0, 0, 0.5f);
        lockScreen.Add(clock);

        var dateLine = new Label("主体历 105 年 1 月 1 日 · 星期四");
        dateLine.style.fontSize = 22;
        dateLine.style.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        dateLine.style.unityFontDefinition = Fd();
        dateLine.style.marginBottom = 28;
        lockScreen.Add(dateLine);

        // 扫脸取景框
        var frame = new VisualElement { name = "face-frame" };
        frame.style.width = 220; frame.style.height = 150;
        frame.style.borderTopWidth = 2; frame.style.borderBottomWidth = 2;
        frame.style.borderLeftWidth = 2; frame.style.borderRightWidth = 2;
        frame.style.borderTopColor = new Color(0.6f, 0.85f, 1f, 0.7f);
        frame.style.borderBottomColor = new Color(0.6f, 0.85f, 1f, 0.7f);
        frame.style.borderLeftColor = new Color(0.6f, 0.85f, 1f, 0.7f);
        frame.style.borderRightColor = new Color(0.6f, 0.85f, 1f, 0.7f);
        frame.style.backgroundColor = new Color(0, 0.15f, 0.3f, 0.18f);
        frame.style.alignItems = Align.Center;
        frame.style.justifyContent = Justify.Center;
        lockScreen.Add(frame);

        // 像素光带（扫脸动画）
        var beam = new Label("▂▃▄▅▆▇");
        beam.name = "face-beam";
        beam.style.fontSize = 26;
        beam.style.color = new Color(0.55f, 0.9f, 1f, 0.9f);
        beam.style.unityFontDefinition = Fd();
        beam.style.position = Position.Absolute;
        beam.style.top = new Length(46, LengthUnit.Percent);
        beam.style.opacity = 0.6f;
        frame.Add(beam);

        var hint = new Label("[ 扫脸解锁 · 点击任意处 ]");
        hint.style.fontSize = 16;
        hint.style.color = new Color(1f, 1f, 1f, 0.6f);
        hint.style.unityFontDefinition = Fd();
        hint.style.marginTop = 18;
        lockScreen.Add(hint);

        var ver = new Label("澎湃OS 32 · DPRK Edition");
        ver.style.position = Position.Absolute;
        ver.style.bottom = 26;
        ver.style.left = 0; ver.style.right = 0;
        ver.style.fontSize = 15;
        ver.style.color = new Color(1f, 1f, 1f, 0.45f);
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

        // ---- 顶部状态栏 ----
        statusBar = new VisualElement();
        statusBar.style.flexDirection = FlexDirection.Row;
        statusBar.style.justifyContent = Justify.SpaceBetween;
        statusBar.style.paddingLeft = 18; statusBar.style.paddingRight = 18;
        statusBar.style.paddingTop = 8;
        statusBar.style.height = 34;
        desktop.Add(statusBar);

        statusClock = new Label("12:00");
        statusClock.style.fontSize = 18;
        statusClock.style.color = Ink;
        statusClock.style.unityFontDefinition = Fd();
        statusBar.Add(statusClock);

        var rightInfo = new VisualElement();
        rightInfo.style.flexDirection = FlexDirection.Row;
        rightInfo.style.alignItems = Align.Center;
        statusBar.Add(rightInfo);

        var netTag = new Label("阿里郎内网");
        netTag.style.fontSize = 15;
        netTag.style.color = Accent;
        netTag.style.unityFontDefinition = Fd();
        netTag.style.marginRight = 12;
        rightInfo.Add(netTag);

        var sig = new Label("●●●○○");
        sig.style.fontSize = 16; sig.style.color = Ink; sig.style.unityFontDefinition = Fd();
        sig.style.marginRight = 10;
        rightInfo.Add(sig);

        var batt = new Label("▮▮▮▮▯ 78%");
        batt.style.fontSize = 16; batt.style.color = Ink; batt.style.unityFontDefinition = Fd();
        rightInfo.Add(batt);

        // ---- 负一屏小部件（今日线路） ----
        var widget = new VisualElement();
        widget.style.width = 300;
        widget.style.height = 76;
        widget.style.marginTop = 6;
        widget.style.marginLeft = 18;
        widget.style.backgroundColor = new Color(1f, 1f, 1f, 0.55f);
        widget.style.borderTopLeftRadius = 10; widget.style.borderTopRightRadius = 10;
        widget.style.borderBottomLeftRadius = 10; widget.style.borderBottomRightRadius = 10;
        widget.style.paddingLeft = 14; widget.style.paddingTop = 8;
        widget.style.flexDirection = FlexDirection.Column;
        desktop.Add(widget);

        var wTitle = new Label("今日线路 · 雾峰村线");
        wTitle.style.fontSize = 16; wTitle.style.color = Ink; wTitle.style.unityFontDefinition = Fd();
        widget.Add(wTitle);
        var wData = new Label("运营第 1 天   资金 2,000 沙币   信任度 友好");
        wData.style.fontSize = 14; wData.style.color = new Color(0.3f, 0.3f, 0.35f, 1f);
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
            "铁路运营", "岁月", "RDA 助手", "阿里郎商店", "未来网",
            "白头疫苗", "米家能量", "相册", "音乐", "站务日志",
            "列车图鉴", "设置", "铁路", "", "",
        };
        for (int r = 0; r < 3; r++)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            grid.Add(row);
            for (int c = 0; c < 5; c++)
            {
                string name = iconNames[r * 5 + c];
                if (string.IsNullOrEmpty(name)) continue;
                var cell = BuildAppIcon(name);
                row.Add(cell);
                appIcons.Add(cell);
            }
        }

        // ---- 底部任务栏 ----
        var dock = new VisualElement();
        dock.style.height = 54;
        dock.style.flexDirection = FlexDirection.Row;
        dock.style.justifyContent = Justify.Center;
        dock.style.alignItems = Align.Center;
        dock.style.backgroundColor = new Color(0.1f, 0.1f, 0.14f, 0.55f);
        dock.style.borderTopLeftRadius = 12; dock.style.borderTopRightRadius = 12;
        dock.style.marginLeft = 80; dock.style.marginRight = 80;
        dock.style.marginBottom = 10;
        desktop.Add(dock);

        string[] dockApps = { "铁路运营", "岁月", "RDA 助手", "站务日志", "列车图鉴" };
        foreach (var d in dockApps)
        {
            var di = new Label(IconFor(d));
            di.style.fontSize = 30;
            di.style.color = new Color(0.95f, 0.95f, 0.9f, 0.95f);
            di.style.unityFontDefinition = Fd();
            di.style.marginLeft = 22; di.style.marginRight = 22;
            di.style.unityTextAlign = TextAnchor.MiddleCenter;
            // 点击同桌面图标
            di.style.width = 46; di.style.height = 40;
            di.RegisterCallback<ClickEvent>(_ => LaunchApp(d));
            dock.Add(di);
        }
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

        // 图标主体（适配各 App 的符号/像素字形）
        var icon = new Label(IconFor(name));
        icon.style.fontSize = 42;
        icon.style.width = 60; icon.style.height = 52;
        icon.style.unityTextAlign = TextAnchor.MiddleCenter;
        icon.style.color = Ink;
        icon.style.backgroundColor = new Color(1f, 1f, 1f, 0.75f);
        icon.style.borderTopLeftRadius = 12; icon.style.borderTopRightRadius = 12;
        icon.style.borderBottomLeftRadius = 12; icon.style.borderBottomRightRadius = 12;
        icon.style.borderTopWidth = 2; icon.style.borderBottomWidth = 2;
        icon.style.borderLeftWidth = 2; icon.style.borderRightWidth = 2;
        icon.style.borderTopColor = new Color(0.7f, 0.7f, 0.75f, 0.6f);
        icon.style.borderBottomColor = new Color(0.7f, 0.7f, 0.75f, 0.6f);
        icon.style.borderLeftColor = new Color(0.7f, 0.7f, 0.75f, 0.6f);
        icon.style.borderRightColor = new Color(0.7f, 0.7f, 0.75f, 0.6f);
        icon.style.unityFontDefinition = Fd();
        cell.Add(icon);

        var label = new Label(name);
        label.style.fontSize = 14;
        label.style.color = Ink;
        label.style.unityFontDefinition = Fd();
        label.style.marginTop = 6;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        cell.Add(label);

        // hover 放大
        cell.RegisterCallback<PointerEnterEvent>(_ => { icon.style.scale = new Scale(new Vector2(1.08f, 1.08f)); });
        cell.RegisterCallback<PointerLeaveEvent>(_ => { icon.style.scale = new Scale(new Vector2(1f, 1f)); });
        cell.RegisterCallback<ClickEvent>(_ => LaunchApp(name));
        return cell;
    }

    private string IconFor(string name)
    {
        switch (name)
        {
            case "铁路运营": return "鉄";       // 铁轨符号（像素下最像）
            case "岁月": return "岁";
            case "RDA 助手": return "R";
            case "阿里郎商店": return "购";
            case "未来网": return "網";
            case "白头疫苗": return "盾";
            case "米家能量": return "⚡";
            case "相册": return "山";
            case "音乐": return "♪";
            case "站务日志": return "志";
            case "列车图鉴": return "車";
            case "设置": return "⚙";
            case "铁路": return "夾";
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
        // 半透明遮罩
        var mask = new VisualElement();
        mask.style.position = Position.Absolute;
        mask.style.top = 0; mask.style.left = 0; mask.style.right = 0; mask.style.bottom = 0;
        mask.style.backgroundColor = new Color(0, 0, 0, 0.45f);
        mask.pickingMode = PickingMode.Position;
        mask.RegisterCallback<ClickEvent>(e => { if (e.target == mask) appLayer.style.display = DisplayStyle.None; });
        appLayer.Add(mask);

        // 窗口
        var win = new VisualElement();
        win.style.width = 720; win.style.height = 420;
        win.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.97f);
        win.style.borderTopWidth = 2; win.style.borderBottomWidth = 2;
        win.style.borderLeftWidth = 2; win.style.borderRightWidth = 2;
        win.style.borderTopColor = Accent; win.style.borderBottomColor = Accent;
        win.style.borderLeftColor = Accent; win.style.borderRightColor = Accent;
        win.style.borderTopLeftRadius = 8; win.style.borderTopRightRadius = 8;
        win.style.borderBottomLeftRadius = 8; win.style.borderBottomRightRadius = 8;
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
        title.style.fontSize = 24;
        title.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        title.style.unityFontDefinition = Fd();
        titleRow.Add(title);

        var closeBtn = new Button(() => { appLayer.style.display = DisplayStyle.None; }) { text = "×" };
        closeBtn.style.width = 34; closeBtn.style.height = 30;
        closeBtn.style.fontSize = 20;
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = Fd();
        closeBtn.style.backgroundColor = new Color(0.3f, 0.2f, 0.1f, 0.8f);
        closeBtn.style.color = new Color(1f, 0.8f, 0.6f, 1f);
        titleRow.Add(closeBtn);

        if (name == "铁路运营")
        {
            // 经营入口收纳
            var btnStory = new Button(ShowStoryPage) { text = "剧情 · Main Story" };
            StyleMenuBtn(btnStory);
            win.Add(btnStory);

            var btnAffairs = new Button(ShowAffairsPage) { text = "事务" };
            StyleMenuBtn(btnAffairs);
            win.Add(btnAffairs);
        }
        else
        {
            string msg = name switch
            {
                "岁月" => "岁月：我可是高性能AI！这个界面……还没接好，但已经够酷了吧？",
                "RDA 助手" => "根据数据，建议您先完成铁路运营首日的调度。",
                "阿里郎商店" => "该商店仅提供白名单应用。（首页维护中）",
                "白头疫苗" => "系统体检：各项指标正常。✓",
                "米家能量" => "沙能设备控制台（此处应显示 0721 的能源状态）。",
                "设置" => "本设备已锁定出厂配置。（想改？去找工程师吧。）",
                _ => $"{name}：该应用暂未接入。",
            };
            var body = new Label(msg);
            body.style.fontSize = 16;
            body.style.color = new Color(1f, 1f, 1f, 0.85f);
            body.style.whiteSpace = WhiteSpace.Normal;
            body.style.unityFontDefinition = Fd();
            body.style.marginTop = 60;
            win.Add(body);
        }

        appLayer.style.display = DisplayStyle.Flex;
    }

    private void StyleMenuBtn(Button btn)
    {
        btn.style.width = 240; btn.style.height = 52;
        btn.style.fontSize = 20; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.85f);
        btn.style.color = new Color(0.85f, 0.92f, 1f, 1f);
        btn.style.marginTop = 18;
        btn.style.alignSelf = Align.Center;
    }

    private void ShowAffairsPage()
    {
        // 事务页（简单占位）
        var panel = BuildInnerPanel("事务");
        var p = new Label("事务功能开发中（绑定经营系统后开放）。");
        p.style.fontSize = 16; p.style.color = new Color(1f, 1f, 1f, 0.8f);
        p.style.unityFontDefinition = Fd(); p.style.marginTop = 40;
        panel.Add(p);
    }

    private void ShowStoryPage()
    {
        var panel = BuildInnerPanel("剧情");
        var btn = new Button(() => { gameObject.SetActive(true); MainStoryUI.Show(); }) { text = "打开 Main Story" };
        btn.style.width = 240; btn.style.height = 52;
        btn.style.fontSize = 20; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.85f);
        btn.style.color = new Color(0.85f, 0.92f, 1f, 1f);
        btn.style.marginTop = 60;
        btn.style.alignSelf = Align.Center;
        panel.Add(btn);
    }

    private VisualElement BuildInnerPanel(string title)
    {
        appLayer.Clear();
        var mask = new VisualElement();
        mask.style.position = Position.Absolute;
        mask.style.top = 0; mask.style.left = 0; mask.style.right = 0; mask.style.bottom = 0;
        mask.style.backgroundColor = new Color(0, 0, 0, 0.6f);
        mask.pickingMode = PickingMode.Position;
        mask.RegisterCallback<ClickEvent>(e => { if (e.target == mask) appLayer.style.display = DisplayStyle.None; });
        appLayer.Add(mask);

        var panel = new VisualElement();
        panel.style.width = 640; panel.style.height = 360;
        panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.97f);
        panel.style.borderTopWidth = 2; panel.style.borderBottomWidth = 2;
        panel.style.borderLeftWidth = 2; panel.style.borderRightWidth = 2;
        panel.style.borderTopColor = Accent; panel.style.borderBottomColor = Accent;
        panel.style.borderLeftColor = Accent; panel.style.borderRightColor = Accent;
        panel.style.borderTopLeftRadius = 8; panel.style.borderTopRightRadius = 8;
        panel.style.borderBottomLeftRadius = 8; panel.style.borderBottomRightRadius = 8;
        panel.style.paddingLeft = 20; panel.style.paddingRight = 20;
        panel.style.paddingTop = 16; panel.style.paddingBottom = 16;
        appLayer.Add(panel);

        var t = new Label(title);
        t.style.fontSize = 24;
        t.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
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
    }
}