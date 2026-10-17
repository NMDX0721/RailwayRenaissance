using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet;
using RailwayRenaissance.UI.Tablet.Apps;

/// <summary>小米平板系统桌面（模拟经营主界面）。澎湃32·朝鲜特供版 像素演绎。
/// 锁屏(扫脸)→桌面(状态栏+App网格+任务栏)→App窗口。</summary>
public class GameMainUI : MonoBehaviour
{
    public static GameMainUI Instance { get; private set; }
    private UIDocument uiDoc;
    private VisualElement root;
    private VisualElement lockScreen;
    private VisualElement desktop;
    private Font gameFont;
    private FontDefinition Fd() => new FontDefinition { font = gameFont };

    private float clockTimer;

    /// <summary>设备真实电量（0-100）；读不到返回 -1（台式机/Editor 无电池）。</summary>
    public static int GetDeviceBatteryPercent()
    {
        // 本机环境下 kernel32 GetSystemPowerStatus 的 P/Invoke 触发 native 崩（exit_trace 已定位 wallpaper→battery 区间），
        // 统一改走 Unity API——台式机 batteryLevel=-1，显示「⚡ 电源」
        float lvl = SystemInfo.batteryLevel;
        if (lvl >= 0f) return Mathf.RoundToInt(lvl * 100f);
        return -1;
    }

    private static string FormatBatt(int pct)
    {
        if (pct < 0) return "⚡ 电源";
        int filled = Mathf.Clamp(Mathf.RoundToInt(pct / 20f), 0, 5);
        return new string('▮', filled) + new string('▯', 5 - filled) + $" {pct}%";
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
    }
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(ref SYSTEM_POWER_STATUS sps);
#endif

    public static void Show()
    {
        if (Instance == null)
        {
            var go = new GameObject("GameMainUI");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameMainUI>();
            // 首次构建很重且与 VN 退出路径相关：托管异常兜住并留日志，避免整局卡退无痕迹
            try
            {
                Instance.Init();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GameMainUI] Init failed: " + ex);
                Instance = null;
                Object.Destroy(go);
                return;
            }
        }
        if (Instance != null)
        {
            // 显示即同步时钟（此路径不经过 ShowLockScreen）
            var now = System.DateTime.Now.ToString("HH:mm");
            var lc = Instance.lockScreen?.Q<Label>("lock-clock");
            if (lc != null) lc.text = now;
            if (Instance.statusClock != null) Instance.statusClock.text = now;
        }
        // 每次进入从锁屏开始（首次进入体验扫脸）
        if (Instance.lockScreen != null && !Instance.unlocked)
            Instance.lockScreen.style.display = DisplayStyle.Flex;
        Instance.root.style.display = DisplayStyle.Flex;
        // 锁屏已显示 → 后台错帧加载桌面（中间页先行，性能与体验）
        Instance.EnsureDesktopBuilt();
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
    private static readonly Color GlassTextMuted = new Color(1f, 1f, 1f, 0.35f);           // 弱化说明文字
    private static readonly Color GlassMask = new Color(0f, 0f, 0f, 0.55f);               // 遮罩层

    // HyperOS / 安卓原生浅色设计体系 Tokens（T298 规范）
    private static readonly Color SurfaceApp = new Color(0.97f, 0.97f, 0.98f, 1f);        // #F7F7F9 应用内容区纯白/极浅底
    private static readonly Color SurfaceTopBar = new Color(1f, 1f, 1f, 1f);               // #FFFFFF 顶栏白底
    private static readonly Color SurfaceCard = new Color(1f, 1f, 1f, 1f);                 // #FFFFFF 卡片纯白底
    private static readonly Color SurfaceCardSoft = new Color(0.94f, 0.94f, 0.96f, 0.85f); // #F0F0F5 浅灰软卡片
    private static readonly Color SurfaceHover = new Color(0.93f, 0.93f, 0.95f, 1f);       // #EDEDEF 悬浮或选中浅灰
    private static readonly Color BorderCard = new Color(0.90f, 0.90f, 0.92f, 1f);         // #E5E5EB 1px 卡片边框
    private static readonly Color BorderDivider = new Color(0.92f, 0.92f, 0.94f, 1f);      // #EBEBEF 分隔线
    private static readonly Color InkPrimary = new Color(0.10f, 0.10f, 0.10f, 1f);         // #1A1A1A 主文字
    private static readonly Color InkSecondary = new Color(0.42f, 0.42f, 0.44f, 1f);       // #6B6B6B 次文字
    private static readonly Color InkTertiary = new Color(0.61f, 0.61f, 0.63f, 1f);        // #9B9B9B 弱化文字/占位
    private static readonly Color SectionHeaderColor = new Color(0.54f, 0.56f, 0.60f, 1f); // #8A8F98 分组标题浅灰蓝
    private static readonly Color AccentSystem = new Color(0.72f, 0.52f, 0.23f, 1f);       // #B8843A 本作琥珀强调色（开关/选中态）

    // 归途与传统质感调色板（借蔚蓝档案两级结构，使用本作深棕调色体系）
    private static readonly Color BrownDark = new Color(0.23f, 0.13f, 0.06f, 0.95f);    // #3A2010 深棕
    private static readonly Color BrownWarm = new Color(0.42f, 0.27f, 0.14f, 0.9f);     // #6B4423 暖棕
    private static readonly Color AmberAccent = new Color(0.72f, 0.52f, 0.23f, 1f);      // #B8843A 琥珀
    private static readonly Color RicePaperLight = new Color(0.91f, 0.85f, 0.75f, 1f);   // #E8D8C0 米白
    private static readonly Color CrimsonSeal = new Color(0.55f, 0.23f, 0.16f, 1f);     // #8B3A2A 暗红印章
    private static readonly Color GoldTrimColor = new Color(0.83f, 0.66f, 0.26f, 1f);    // #D4A843 金
    private static readonly Color DeepRail = new Color(0.10f, 0.16f, 0.23f, 0.95f);      // #1A2A3A 深蓝底衬

    // 按键重绑定缓存与支持
    private System.Action<KeyCode> _tabletRebindAction;
    private static readonly KeyCode[] AllKeyCodes = AllKeyCodesInit();
    private static KeyCode[] AllKeyCodesInit()
    {
        var values = System.Enum.GetValues(typeof(KeyCode));
        var list = new System.Collections.Generic.List<KeyCode>();
        foreach (var v in values)
        {
            var k = (KeyCode)v;
            if (k != KeyCode.None) list.Add(k);
        }
        return list.ToArray();
    }

    /// <summary>阶段一（轻量，退出关键路径）：文档+锁屏——document/lock 段经日志验证从未崩；桌面/应用层错帧后台加载。</summary>
    private void Init()
    {
        MigrateUsername();
        TraceLog.L("[GameMainUI] Init: migrate ok");
        gameFont = Resources.Load<Font>("Fonts/zpix");
        TraceLog.L("[GameMainUI] Init: font loaded=" + (gameFont != null));
        BuildDocument();
        TraceLog.L("[GameMainUI] Init: document ok");
        BuildLockScreen();
        TraceLog.L("[GameMainUI] Init: lock ok");
        ShowLockScreen();
        TraceLog.L("[GameMainUI] Init: phase1 done (desktop pending)");
    }

    private bool desktopBuilt;
    private bool desktopBuilding;

    /// <summary>确保桌面/应用层已构建：锁屏先显示一帧，再于后台构建（用户方案：中间页先行，桌面后台加载）。</summary>
    public void EnsureDesktopBuilt()
    {
        if (desktopBuilt || desktopBuilding) return;
        desktopBuilding = true;
        StartCoroutine(BuildDesktopAsync());
    }

    private System.Collections.IEnumerator BuildDesktopAsync()
    {
        yield return null; // 先让锁屏渲染出来（用户立刻看到中间页，而非黑屏）
        if (desktopBuilt) { desktopBuilding = false; yield break; } // 图鉴页可能已同步构建完
        TraceLog.L("[GameMainUI] DesktopAsync: start (background)");
        BuildDesktop();
        TraceLog.L("[GameMainUI] DesktopAsync: desktop ok");
        yield return null;
        if (desktopBuilt) { desktopBuilding = false; yield break; } // 同上，App层已由同步路径补齐
        BuildAppLayer();
        TraceLog.L("[GameMainUI] DesktopAsync: apps ok");
        desktopBuilt = true;
        desktopBuilding = false;
    }

    /// <summary>预热：进 VN 时提前构建平板（隐藏态），退出剧情时只 Show 不再 Init——把重型构建挪出退出关键路径。</summary>
    public static void Preload()
    {
        if (Instance != null) return;
        var go = new GameObject("GameMainUI");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GameMainUI>();
        try
        {
            Instance.Init();
            TraceLog.L("[GameMainUI] Preload ok (root hidden)");
        }
        catch (System.Exception ex)
        {
            TraceLog.L("[GameMainUI] Preload FAILED: " + ex);
            Instance = null;
            UnityEngine.Object.Destroy(go);
        }
    }

    private void MigrateUsername()
    {
        // 迁移旧登录系统的用户名到小米账号体系
        if (!PlayerPrefs.HasKey("XiaomiAccount") && PlayerPrefs.HasKey("Username"))
        {
            PlayerPrefs.SetString("XiaomiAccount", PlayerPrefs.GetString("Username"));
            PlayerPrefs.Save();
        }
        if (!PlayerPrefs.HasKey("XiaomiAccount"))
        {
            PlayerPrefs.SetString("XiaomiAccount", "旅人");
            PlayerPrefs.Save();
        }
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
        if (TraceLog.ConsoleVerbose) Debug.Log("[GameMainUI] BuildDocument: canvas ok");

        var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
        if (TraceLog.ConsoleVerbose) Debug.Log("[GameMainUI] BuildDocument: panelSettings=" + (panelSettings != null));
        uiDoc = canvasObj.AddComponent<UIDocument>();
        if (TraceLog.ConsoleVerbose) Debug.Log("[GameMainUI] BuildDocument: uidoc added");
        uiDoc.panelSettings = panelSettings;
        uiDoc.visualTreeAsset = null;
        root = uiDoc.rootVisualElement;
        if (TraceLog.ConsoleVerbose) Debug.Log("[GameMainUI] BuildDocument: root=" + (root != null));
        root.style.display = DisplayStyle.None;
    }

    // ============ 锁屏（澎湃OS/HyperOS 风格） ============
    // ============ 锁屏（澎湃OS/HyperOS 水乡质感锁屏） ============
    private void BuildLockScreen()
    {
        lockScreen = new VisualElement { name = "tablet-lock" };
        lockScreen.style.position = Position.Absolute;
        lockScreen.style.top = 0; lockScreen.style.left = 0;
        lockScreen.style.right = 0; lockScreen.style.bottom = 0;
        lockScreen.pickingMode = PickingMode.Position;
        lockScreen.style.flexDirection = FlexDirection.Column;
        root.Add(lockScreen);

        // 江南水乡风景壁纸（真实背景图底图 + 深色半透明玻璃滤镜）
        var wallpaper = new VisualElement();
        wallpaper.style.position = Position.Absolute;
        wallpaper.style.top = 0; wallpaper.style.left = 0;
        wallpaper.style.right = 0; wallpaper.style.bottom = 0;
        lockScreen.Add(wallpaper);

        var bgSp = Resources.Load<Sprite>("bg/songqiao_station");
        if (bgSp != null)
        {
            var bgImg = new Image { sprite = bgSp };
            bgImg.style.position = Position.Absolute;
            bgImg.style.top = 0; bgImg.style.left = 0; bgImg.style.right = 0; bgImg.style.bottom = 0;
            bgImg.scaleMode = ScaleMode.ScaleAndCrop;
            wallpaper.Add(bgImg);

            // 半透明水乡深色薄暮遮罩
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.top = 0; overlay.style.left = 0; overlay.style.right = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0.04f, 0.07f, 0.12f, 0.52f);
            wallpaper.Add(overlay);
        }
        else
        {
            wallpaper.style.backgroundImage = new StyleBackground(GradientBackground());
        }

        // 上部留白 → 居中 HyperOS 96px 大时钟
        var spacerTop = new VisualElement();
        spacerTop.style.height = new Length(14, LengthUnit.Percent);
        lockScreen.Add(spacerTop);

        var clock = new Label(System.DateTime.Now.ToString("HH:mm"));
        clock.name = "lock-clock";
        clock.style.fontSize = 96;
        clock.style.unityFontStyleAndWeight = FontStyle.Bold;
        clock.style.color = Color.white;
        clock.style.unityFontDefinition = Fd();
        clock.style.unityTextAlign = TextAnchor.MiddleCenter;
        lockScreen.Add(clock);

        // 半透明日期与运营信息卡片
        var dateCard = new VisualElement();
        dateCard.style.alignSelf = Align.Center;
        dateCard.style.backgroundColor = new Color(0f, 0f, 0f, 0.38f);
        dateCard.style.borderTopLeftRadius = 14; dateCard.style.borderTopRightRadius = 14;
        dateCard.style.borderBottomLeftRadius = 14; dateCard.style.borderBottomRightRadius = 14;
        dateCard.style.paddingLeft = 18; dateCard.style.paddingRight = 18;
        dateCard.style.paddingTop = 6; dateCard.style.paddingBottom = 6;
        dateCard.style.marginTop = 6;
        dateCard.style.alignItems = Align.Center;
        lockScreen.Add(dateCard);

        var dateLine = new Label(System.DateTime.Now.ToString("M月d日 星期") + "日一二三四五六"[(int)System.DateTime.Now.DayOfWeek] + $" · 主体历 105 年（运营第 {GameData.Day} 天）");
        dateLine.name = "lock-date";
        dateLine.style.fontSize = 14;
        dateLine.style.color = new Color(1f, 1f, 1f, 0.88f);
        dateLine.style.unityFontDefinition = Fd();
        dateCard.Add(dateLine);

        var weather = new Label("雾峰村站 · 多云 22°C  ·  沙能运行正常");
        weather.style.fontSize = 12;
        weather.style.color = new Color(0.85f, 0.92f, 1f, 0.65f);
        weather.style.unityFontDefinition = Fd();
        weather.style.marginTop = 2;
        dateCard.Add(weather);

        var spacerMid = new VisualElement();
        spacerMid.style.flexGrow = 1;
        lockScreen.Add(spacerMid);

        // 扫脸认证指示圈
        var face = new VisualElement { name = "face-frame" };
        face.style.width = 60; face.style.height = 60;
        face.style.borderTopWidth = 1.5f; face.style.borderBottomWidth = 1.5f;
        face.style.borderLeftWidth = 1.5f; face.style.borderRightWidth = 1.5f;
        var faceC = new Color(0.85f, 0.92f, 1f, 0.5f);
        face.style.borderTopColor = faceC; face.style.borderBottomColor = faceC;
        face.style.borderLeftColor = faceC; face.style.borderRightColor = faceC;
        face.style.borderTopLeftRadius = 30; face.style.borderTopRightRadius = 30;
        face.style.borderBottomLeftRadius = 30; face.style.borderBottomRightRadius = 30;
        face.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
        face.style.alignItems = Align.Center;
        face.style.justifyContent = Justify.Center;
        face.style.alignSelf = Align.Center;
        lockScreen.Add(face);

        var beam = new Label("◎");
        beam.name = "face-beam";
        beam.style.fontSize = 24;
        beam.style.color = new Color(0.85f, 0.95f, 1f, 0.85f);
        beam.style.unityFontDefinition = Fd();
        face.Add(beam);

        var hint = new Label("轻触屏幕或按空格解锁");
        hint.style.fontSize = 13;
        hint.style.color = new Color(1f, 1f, 1f, 0.65f);
        hint.style.unityFontDefinition = Fd();
        hint.style.marginTop = 12;
        hint.style.alignSelf = Align.Center;
        lockScreen.Add(hint);

        var spacerBot = new VisualElement();
        spacerBot.style.height = 48;
        lockScreen.Add(spacerBot);

        var ver = new Label("澎湃OS 32 · DPRK Edition");
        ver.style.position = Position.Absolute;
        ver.style.bottom = 14;
        ver.style.left = 0; ver.style.right = 0;
        ver.style.fontSize = 12;
        ver.style.color = new Color(1f, 1f, 1f, 0.35f);
        ver.style.unityTextAlign = TextAnchor.MiddleCenter;
        ver.style.unityFontDefinition = Fd();
        lockScreen.Add(ver);

        lockScreen.RegisterCallback<ClickEvent>(e => Unlock());
        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Space && !unlocked && lockScreen.style.display == DisplayStyle.Flex)
            {
                Unlock();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape && appLayer != null && appLayer.style.display == DisplayStyle.Flex)
            {
                appLayer.style.display = DisplayStyle.None;
                evt.StopPropagation();
            }
        });
    }

    /// <summary>锁屏壁纸：深蓝黑纵向渐变（近似 HyperOS 冷调）。</summary>
    private static Texture2D GradientBackground()
    {
        const int w = 8, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var top = new Color(0.10f, 0.14f, 0.28f, 1f);
        var mid = new Color(0.06f, 0.08f, 0.16f, 1f);
        var bot = new Color(0.03f, 0.04f, 0.08f, 1f);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float u = y / (float)(h - 1);
            Color c = u < 0.5f ? Color.Lerp(top, mid, u * 2f) : Color.Lerp(mid, bot, (u - 0.5f) * 2f);
            for (int x = 0; x < w; x++) px[y * w + x] = c;
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    private void ShowLockScreen()
    {
        unlocked = false;
        // 显示即同步时钟（首次加载时 tick 尚未启动的窗口期）
        var now = System.DateTime.Now.ToString("HH:mm");
        var lc = lockScreen?.Q<Label>("lock-clock");
        if (lc != null) lc.text = now;
        if (statusClock != null) statusClock.text = now;
        lockScreen.style.display = DisplayStyle.Flex;
        // Phase1（预热/退出关键路径）时 desktop 尚未构建——它创建时本就默认 None，跳过即可
        if (desktop != null) desktop.style.display = DisplayStyle.None;
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
    private Label statusBatt;
    private int battTick;
    private readonly System.Collections.Generic.List<VisualElement> appIcons = new System.Collections.Generic.List<VisualElement>();
    private int currentDesktopPage = 0;
    private VisualElement desktopPage0;
    private VisualElement desktopPage1;
    private VisualElement pageDot0;
    private VisualElement pageDot1;
    private VisualElement folderOverlay;
    private bool isDesktopDragging = false;
    private Vector2 desktopDragStartPos;
    private float desktopDragDeltaX = 0f;
    private static readonly string[] MasterAllApps = {
        "RDA", "归途", "媒姬百科", "相册", "音乐", "铁路",
        "驾驶台", "笔记", "设置", "浏览器", "安全中心", "米家",
        "地图", "应用商店", "阿里郎商店", "未来网", "白头疫苗", "时钟",
        "日历", "天气", "计算器", "文件管理"
    };
    private static readonly string[] Page0SelectedApps = {
        "相册", "音乐", "驾驶台", "媒姬百科",
        "铁路", "浏览器", "安全中心", "未来网"
    };

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
        TraceLog.L("[GameMainUI] Desktop: begin");
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
        TraceLog.L("[GameMainUI] Desktop: wallpaper ok");

        // ---- 顶部状态栏（液态玻璃） ----
        statusBar = new VisualElement();
        statusBar.style.flexDirection = FlexDirection.Row;
        statusBar.style.justifyContent = Justify.SpaceBetween;
        statusBar.style.paddingLeft = 20; statusBar.style.paddingRight = 20;
        statusBar.style.paddingTop = 8; statusBar.style.paddingBottom = 6;
        statusBar.style.height = 38;
        statusBar.style.backgroundColor = new Color(0.06f, 0.05f, 0.07f, 0.45f);
        desktop.Add(statusBar);

        statusClock = new Label(System.DateTime.Now.ToString("HH:mm"));
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

        int battPct = -1;
        try { battPct = GetDeviceBatteryPercent(); }
        catch (System.Exception ex) { TraceLog.L("[GameMainUI] Desktop: battery EX " + ex.Message); }
        TraceLog.L("[GameMainUI] Desktop: battery ok pct=" + battPct);
        var batt = new Label(FormatBatt(battPct));
        batt.style.fontSize = 14; batt.style.color = GlassText; batt.style.unityFontDefinition = Fd();
        statusBatt = batt;
        rightInfo.Add(batt);

        // ---- 双页桌面容器 ----
        var pagesRoot = new VisualElement { name = "desktop-pages-root" };
        pagesRoot.style.flexGrow = 1;
        pagesRoot.style.position = Position.Relative;
        pagesRoot.style.overflow = Overflow.Hidden;
        desktop.Add(pagesRoot);

        // 注册指针拖拽横滑翻页（对齐 HyperOS 交互体验：横向位移超 60px 翻页，松手未过阈值弹回）
        // 关键修复：PointerDown 仅记录起点，位移超过 8px 才 CapturePointer，彻底避免截断子元素 ClickEvent
        bool desktopPointerCaptured = false;
        pagesRoot.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button == 0)
            {
                isDesktopDragging = true;
                desktopPointerCaptured = false;
                desktopDragStartPos = evt.position;
                desktopDragDeltaX = 0f;
            }
        });

        pagesRoot.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (isDesktopDragging)
            {
                desktopDragDeltaX = evt.position.x - desktopDragStartPos.x;
                if (!desktopPointerCaptured && Mathf.Abs(desktopDragDeltaX) > 8f)
                {
                    desktopPointerCaptured = true;
                    pagesRoot.CapturePointer(evt.pointerId);
                }
                float offset = Mathf.Clamp(desktopDragDeltaX * 0.45f, -48f, 48f);
                if (currentDesktopPage == 0 && desktopPage0 != null)
                {
                    desktopPage0.style.translate = new Translate(offset, 0);
                }
                else if (currentDesktopPage == 1 && desktopPage1 != null)
                {
                    desktopPage1.style.translate = new Translate(offset, 0);
                }
            }
        });

        System.Action<IPointerEvent> finishDrag = (evt) =>
        {
            if (!isDesktopDragging) return;
            isDesktopDragging = false;
            if (desktopPointerCaptured && pagesRoot.HasPointerCapture(evt.pointerId))
            {
                pagesRoot.ReleasePointer(evt.pointerId);
            }
            desktopPointerCaptured = false;

            if (desktopPage0 != null) desktopPage0.style.translate = new Translate(0, 0);
            if (desktopPage1 != null) desktopPage1.style.translate = new Translate(0, 0);

            if (desktopDragDeltaX < -60f && currentDesktopPage == 0)
            {
                SwitchDesktopPage(1);
            }
            else if (desktopDragDeltaX > 60f && currentDesktopPage == 1)
            {
                SwitchDesktopPage(0);
            }
        };

        pagesRoot.RegisterCallback<PointerUpEvent>(evt => finishDrag(evt));
        pagesRoot.RegisterCallback<PointerCancelEvent>(evt => finishDrag(evt));
        pagesRoot.RegisterCallback<PointerCaptureOutEvent>(evt =>
        {
            if (isDesktopDragging)
            {
                isDesktopDragging = false;
                if (desktopPage0 != null) desktopPage0.style.translate = new Translate(0, 0);
                if (desktopPage1 != null) desktopPage1.style.translate = new Translate(0, 0);
            }
        });

        // 仅在明确横向滚轮分量时辅助切页，上下纵向滚轮不再横向切页
        pagesRoot.RegisterCallback<WheelEvent>(evt =>
        {
            if (Mathf.Abs(evt.delta.x) > 12f)
            {
                if (evt.delta.x > 0) SwitchDesktopPage(1);
                else if (evt.delta.x < 0) SwitchDesktopPage(0);
            }
        });

        // Page 0: 小组件与常用应用页
        desktopPage0 = new VisualElement { name = "desktop-page-0" };
        desktopPage0.style.position = Position.Absolute;
        desktopPage0.style.top = 0; desktopPage0.style.left = 0; desktopPage0.style.right = 0; desktopPage0.style.bottom = 0;
        desktopPage0.style.flexDirection = FlexDirection.Column;
        desktopPage0.style.justifyContent = Justify.SpaceAround;
        desktopPage0.style.alignItems = Align.Center;
        desktopPage0.style.paddingTop = 10;
        pagesRoot.Add(desktopPage0);

        // Page 1: 全量应用网格页
        desktopPage1 = new VisualElement { name = "desktop-page-1" };
        desktopPage1.style.position = Position.Absolute;
        desktopPage1.style.top = 0; desktopPage1.style.left = 0; desktopPage1.style.right = 0; desktopPage1.style.bottom = 0;
        desktopPage1.style.flexDirection = FlexDirection.Column;
        desktopPage1.style.justifyContent = Justify.Center;
        desktopPage1.style.alignItems = Align.Center;
        desktopPage1.style.display = DisplayStyle.None;
        pagesRoot.Add(desktopPage1);

        // 构建 Page 0
        BuildDesktopPage0();

        // 构建 Page 1
        BuildDesktopPage1();

        // ---- 双页胶囊指示器 ----
        var indicatorRow = new VisualElement();
        indicatorRow.style.flexDirection = FlexDirection.Row;
        indicatorRow.style.justifyContent = Justify.Center;
        indicatorRow.style.alignItems = Align.Center;
        indicatorRow.style.height = 18;
        desktop.Add(indicatorRow);

        pageDot0 = new VisualElement();
        pageDot0.style.width = 20; pageDot0.style.height = 6;
        pageDot0.style.backgroundColor = Color.white;
        pageDot0.style.borderTopLeftRadius = 3; pageDot0.style.borderTopRightRadius = 3;
        pageDot0.style.borderBottomLeftRadius = 3; pageDot0.style.borderBottomRightRadius = 3;
        pageDot0.style.marginLeft = 4; pageDot0.style.marginRight = 4;
        pageDot0.RegisterCallback<ClickEvent>(_ => SwitchDesktopPage(0));
        indicatorRow.Add(pageDot0);

        pageDot1 = new VisualElement();
        pageDot1.style.width = 6; pageDot1.style.height = 6;
        pageDot1.style.backgroundColor = new Color(1f, 1f, 1f, 0.4f);
        pageDot1.style.borderTopLeftRadius = 3; pageDot1.style.borderTopRightRadius = 3;
        pageDot1.style.borderBottomLeftRadius = 3; pageDot1.style.borderBottomRightRadius = 3;
        pageDot1.style.marginLeft = 4; pageDot1.style.marginRight = 4;
        pageDot1.RegisterCallback<ClickEvent>(_ => SwitchDesktopPage(1));
        indicatorRow.Add(pageDot1);

        // ---- 底部任务栏（悬浮毛玻璃胶囊Dock） ----
        var dock = new VisualElement();
        dock.style.height = 60;
        dock.style.flexDirection = FlexDirection.Row;
        dock.style.justifyContent = Justify.Center;
        dock.style.alignItems = Align.Center;
        dock.style.backgroundColor = new Color(0.08f, 0.07f, 0.1f, 0.75f);
        dock.style.borderTopLeftRadius = 24; dock.style.borderTopRightRadius = 24;
        dock.style.borderBottomLeftRadius = 24; dock.style.borderBottomRightRadius = 24;
        dock.style.borderTopWidth = 1; dock.style.borderBottomWidth = 1;
        dock.style.borderLeftWidth = 1; dock.style.borderRightWidth = 1;
        dock.style.borderTopColor = GlassBorderSoft; dock.style.borderBottomColor = GlassBorderSoft;
        dock.style.borderLeftColor = GlassBorderSoft; dock.style.borderRightColor = GlassBorderSoft;
        dock.style.alignSelf = Align.Center;
        dock.style.paddingLeft = 20; dock.style.paddingRight = 20;
        dock.style.marginBottom = 12;
        desktop.Add(dock);

        string[] dockApps = { "RDA", "归途", "笔记", "设置" };
        foreach (var d in dockApps)
        {
            var btn = BuildDockIcon(d);
            dock.Add(btn);
        }

        // ---- 真实「铁路」文件夹浮层 ----
        BuildFolderOverlay();

        // ---- 岁月AI投影（桌面常驻浮窗）----
        BuildSuiyueWidget();
    }

    private void SwitchDesktopPage(int pageIndex)
    {
        currentDesktopPage = Mathf.Clamp(pageIndex, 0, 1);
        if (desktopPage0 != null)
        {
            desktopPage0.style.display = (currentDesktopPage == 0) ? DisplayStyle.Flex : DisplayStyle.None;
            desktopPage0.style.translate = new Translate(0, 0);
        }
        if (desktopPage1 != null)
        {
            desktopPage1.style.display = (currentDesktopPage == 1) ? DisplayStyle.Flex : DisplayStyle.None;
            desktopPage1.style.translate = new Translate(0, 0);
        }

        if (pageDot0 != null)
        {
            pageDot0.style.width = (currentDesktopPage == 0) ? 20 : 6;
            pageDot0.style.backgroundColor = (currentDesktopPage == 0) ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }
        if (pageDot1 != null)
        {
            pageDot1.style.width = (currentDesktopPage == 1) ? 20 : 6;
            pageDot1.style.backgroundColor = (currentDesktopPage == 1) ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }
    }

    private void BuildDesktopPage0()
    {
        // 上半部：小组件行
        var widgetRow = new VisualElement { name = "desktop-widget-row" };
        widgetRow.style.flexDirection = FlexDirection.Row;
        widgetRow.style.justifyContent = Justify.Center;
        widgetRow.style.alignItems = Align.Center;
        widgetRow.style.marginTop = 10;
        widgetRow.style.marginBottom = 6;
        desktopPage0.Add(widgetRow);

        widgetRow.Add(BuildClockWidget());
        widgetRow.Add(BuildWeatherWidget());
        widgetRow.Add(BuildScheduleWidget());
        widgetRow.Add(BuildGalleryWidget());
        widgetRow.Add(BuildCalendarWidget());

        // 下半部：精选常用应用网格（排除 Dock 与小组件重复项）
        var commonGrid = new VisualElement { name = "desktop-page0-grid" };
        commonGrid.style.flexDirection = FlexDirection.Column;
        commonGrid.style.justifyContent = Justify.Center;
        commonGrid.style.alignItems = Align.Center;
        commonGrid.style.marginTop = 12;
        desktopPage0.Add(commonGrid);

        var r1 = new VisualElement();
        r1.style.flexDirection = FlexDirection.Row;
        commonGrid.Add(r1);
        for (int i = 0; i < 4 && i < Page0SelectedApps.Length; i++)
        {
            string name = Page0SelectedApps[i];
            var cell = name == "铁路" ? BuildRailwayFolderIcon() : BuildAppIcon(name);
            r1.Add(cell);
            appIcons.Add(cell);
        }

        var r2 = new VisualElement();
        r2.style.flexDirection = FlexDirection.Row;
        r2.style.marginTop = 8;
        commonGrid.Add(r2);
        for (int i = 4; i < 8 && i < Page0SelectedApps.Length; i++)
        {
            string name = Page0SelectedApps[i];
            var cell = name == "铁路" ? BuildRailwayFolderIcon() : BuildAppIcon(name);
            r2.Add(cell);
            appIcons.Add(cell);
        }
    }

    private void BuildDesktopPage1()
    {
        var allGrid = new VisualElement { name = "desktop-page1-grid" };
        allGrid.style.flexDirection = FlexDirection.Column;
        allGrid.style.justifyContent = Justify.Center;
        allGrid.style.alignItems = Align.Center;
        desktopPage1.Add(allGrid);

        // 严格排除 Page 0 已出现的图标，计算差集避免重复（A2）
        var p0Set = new System.Collections.Generic.HashSet<string>(Page0SelectedApps);
        var page1Apps = new System.Collections.Generic.List<string>();
        foreach (var app in MasterAllApps)
        {
            if (!p0Set.Contains(app))
            {
                page1Apps.Add(app);
            }
        }

        int cols = 5;
        int rows = Mathf.CeilToInt((float)page1Apps.Count / cols);
        for (int r = 0; r < rows; r++)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = (r > 0) ? 8 : 0;
            allGrid.Add(row);
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                if (idx >= page1Apps.Count) continue;
                string name = page1Apps[idx];
                var cell = name == "铁路" ? BuildRailwayFolderIcon() : BuildAppIcon(name);
                row.Add(cell);
                appIcons.Add(cell);
            }
        }
    }

    private VisualElement BuildClockWidget()
    {
        var w = new VisualElement();
        w.style.width = 125; w.style.height = 115;
        w.style.marginLeft = 6; w.style.marginRight = 6;
        w.style.backgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.82f);
        w.style.borderTopLeftRadius = 20; w.style.borderTopRightRadius = 20;
        w.style.borderBottomLeftRadius = 20; w.style.borderBottomRightRadius = 20;
        w.style.borderTopWidth = 1; w.style.borderBottomWidth = 1;
        w.style.borderLeftWidth = 1; w.style.borderRightWidth = 1;
        w.style.borderTopColor = GlassBorderSoft; w.style.borderBottomColor = GlassBorderSoft;
        w.style.borderLeftColor = GlassBorderSoft; w.style.borderRightColor = GlassBorderSoft;
        w.style.paddingLeft = 12; w.style.paddingRight = 12;
        w.style.paddingTop = 14; w.style.paddingBottom = 12;
        w.style.justifyContent = Justify.SpaceBetween;
        w.style.overflow = Overflow.Hidden;
        w.pickingMode = PickingMode.Position;

        var timeLbl = new Label(System.DateTime.Now.ToString("HH:mm"));
        timeLbl.style.fontSize = 32;
        timeLbl.style.color = GlassText;
        timeLbl.style.unityFontDefinition = Fd();
        w.Add(timeLbl);

        var dateLbl = new Label(System.DateTime.Now.ToString("MM月dd日 星期") + "日一二三四五六"[(int)System.DateTime.Now.DayOfWeek]);
        dateLbl.style.fontSize = 12;
        dateLbl.style.color = GlassTextDim;
        dateLbl.style.unityFontDefinition = Fd();
        w.Add(dateLbl);

        var subLbl = new Label("阿里郎精准授时");
        subLbl.style.fontSize = 11;
        subLbl.style.color = GlassAccent;
        subLbl.style.unityFontDefinition = Fd();
        w.Add(subLbl);

        w.RegisterCallback<PointerEnterEvent>(_ => w.style.scale = new Scale(new Vector2(1.04f, 1.04f)));
        w.RegisterCallback<PointerLeaveEvent>(_ => w.style.scale = new Scale(new Vector2(1f, 1f)));
        w.RegisterCallback<ClickEvent>(_ => LaunchApp("时钟"));
        return w;
    }

    private VisualElement BuildWeatherWidget()
    {
        var w = new VisualElement();
        w.style.width = 135; w.style.height = 115;
        w.style.marginLeft = 6; w.style.marginRight = 6;
        w.style.backgroundColor = new Color(0.11f, 0.28f, 0.35f, 0.88f); // 深青
        w.style.borderTopLeftRadius = 20; w.style.borderTopRightRadius = 20;
        w.style.borderBottomLeftRadius = 20; w.style.borderBottomRightRadius = 20;
        w.style.borderTopWidth = 1; w.style.borderBottomWidth = 1;
        w.style.borderLeftWidth = 1; w.style.borderRightWidth = 1;
        w.style.borderTopColor = new Color(0.3f, 0.55f, 0.65f, 0.4f);
        w.style.borderBottomColor = new Color(0.3f, 0.55f, 0.65f, 0.4f);
        w.style.borderLeftColor = new Color(0.3f, 0.55f, 0.65f, 0.4f);
        w.style.borderRightColor = new Color(0.3f, 0.55f, 0.65f, 0.4f);
        w.style.paddingLeft = 14; w.style.paddingRight = 14;
        w.style.paddingTop = 12; w.style.paddingBottom = 12;
        w.style.justifyContent = Justify.SpaceBetween;
        w.style.overflow = Overflow.Hidden;
        w.pickingMode = PickingMode.Position;

        var locRow = new VisualElement();
        locRow.style.flexDirection = FlexDirection.Row;
        locRow.style.justifyContent = Justify.SpaceBetween;
        w.Add(locRow);

        var loc = new Label("雾峰站");
        loc.style.fontSize = 13;
        loc.style.color = new Color(0.85f, 0.95f, 1f, 0.9f);
        loc.style.unityFontDefinition = Fd();
        locRow.Add(loc);

        var icon = new Label("⛅");
        icon.style.fontSize = 16;
        locRow.Add(icon);

        var temp = new Label("22°C");
        temp.style.fontSize = 28;
        temp.style.color = Color.white;
        temp.style.unityFontDefinition = Fd();
        w.Add(temp);

        var desc = new Label("多云 · 适于出车");
        desc.style.fontSize = 11;
        desc.style.color = new Color(0.8f, 0.92f, 0.98f, 0.8f);
        desc.style.unityFontDefinition = Fd();
        desc.style.overflow = Overflow.Hidden;
        desc.style.textOverflow = TextOverflow.Ellipsis;
        w.Add(desc);

        w.RegisterCallback<PointerEnterEvent>(_ => w.style.scale = new Scale(new Vector2(1.04f, 1.04f)));
        w.RegisterCallback<PointerLeaveEvent>(_ => w.style.scale = new Scale(new Vector2(1f, 1f)));
        w.RegisterCallback<ClickEvent>(_ => LaunchApp("天气"));
        return w;
    }

    private VisualElement BuildScheduleWidget()
    {
        var w = new VisualElement();
        w.style.width = 265; w.style.height = 115;
        w.style.marginLeft = 6; w.style.marginRight = 6;
        w.style.backgroundColor = GlassBgLight;
        w.style.borderTopLeftRadius = 20; w.style.borderTopRightRadius = 20;
        w.style.borderBottomLeftRadius = 20; w.style.borderBottomRightRadius = 20;
        w.style.borderTopWidth = 1; w.style.borderBottomWidth = 1;
        w.style.borderLeftWidth = 1; w.style.borderRightWidth = 1;
        w.style.borderTopColor = GlassBorder; w.style.borderBottomColor = GlassBorder;
        w.style.borderLeftColor = GlassBorder; w.style.borderRightColor = GlassBorder;
        w.style.paddingLeft = 16; w.style.paddingRight = 16;
        w.style.paddingTop = 12; w.style.paddingBottom = 12;
        w.style.justifyContent = Justify.SpaceBetween;
        w.style.overflow = Overflow.Hidden;
        w.pickingMode = PickingMode.Position;

        var head = new VisualElement();
        head.style.flexDirection = FlexDirection.Row;
        head.style.justifyContent = Justify.SpaceBetween;
        head.style.alignItems = Align.Center;
        w.Add(head);

        var title = new Label("今日线路 · 雾峰村线");
        title.style.fontSize = 14; title.style.color = Ink; title.style.unityFontDefinition = Fd();
        title.style.overflow = Overflow.Hidden;
        title.style.textOverflow = TextOverflow.Ellipsis;
        head.Add(title);

        var badge = new Label("运营中");
        badge.style.fontSize = 11; badge.style.color = new Color(0.2f, 0.5f, 0.2f, 0.9f);
        badge.style.backgroundColor = new Color(0.3f, 0.8f, 0.3f, 0.2f);
        badge.style.borderTopLeftRadius = 6; badge.style.borderTopRightRadius = 6;
        badge.style.borderBottomLeftRadius = 6; badge.style.borderBottomRightRadius = 6;
        badge.style.paddingLeft = 6; badge.style.paddingRight = 6; badge.style.paddingTop = 2; badge.style.paddingBottom = 2;
        badge.style.unityFontDefinition = Fd();
        head.Add(badge);

        int planIdx = Mathf.Clamp(GameData.SelectedDispatchPlanIndex, 0, GameData.DispatchPlans.Length - 1);
        string planName = GameData.DispatchPlans[planIdx].Name;
        var planLbl = new Label($"运行方案：{planName}   客流：{GameData.ExpectedPassengers} 人次");
        planLbl.style.fontSize = 12; planLbl.style.color = new Color(0.3f, 0.35f, 0.4f, 0.95f);
        planLbl.style.unityFontDefinition = Fd();
        planLbl.style.overflow = Overflow.Hidden;
        planLbl.style.textOverflow = TextOverflow.Ellipsis;
        w.Add(planLbl);

        var finLbl = new Label($"运营第 {GameData.Day} 天   资金 {GameData.Money:N0} 沙币   车况 {GameData.TrainCondition}%");
        finLbl.style.fontSize = 11; finLbl.style.color = new Color(0.45f, 0.45f, 0.5f, 0.85f);
        finLbl.style.unityFontDefinition = Fd();
        finLbl.style.overflow = Overflow.Hidden;
        finLbl.style.textOverflow = TextOverflow.Ellipsis;
        w.Add(finLbl);

        w.RegisterCallback<PointerEnterEvent>(_ => w.style.scale = new Scale(new Vector2(1.03f, 1.03f)));
        w.RegisterCallback<PointerLeaveEvent>(_ => w.style.scale = new Scale(new Vector2(1f, 1f)));
        w.RegisterCallback<ClickEvent>(_ => LaunchApp("RDA"));
        return w;
    }

    private VisualElement BuildGalleryWidget()
    {
        var w = new VisualElement();
        w.style.width = 205; w.style.height = 115;
        w.style.marginLeft = 6; w.style.marginRight = 6;
        w.style.borderTopLeftRadius = 20; w.style.borderTopRightRadius = 20;
        w.style.borderBottomLeftRadius = 20; w.style.borderBottomRightRadius = 20;
        w.style.borderTopWidth = 1; w.style.borderBottomWidth = 1;
        w.style.borderLeftWidth = 1; w.style.borderRightWidth = 1;
        w.style.borderTopColor = GlassBorderSoft; w.style.borderBottomColor = GlassBorderSoft;
        w.style.borderLeftColor = GlassBorderSoft; w.style.borderRightColor = GlassBorderSoft;
        w.style.overflow = Overflow.Hidden;
        w.style.justifyContent = Justify.FlexEnd;
        w.pickingMode = PickingMode.Position;

        var bgSp = Resources.Load<Sprite>("bg/songqiao_station");
        if (bgSp != null)
        {
            var bgImg = new Image { sprite = bgSp };
            bgImg.style.position = Position.Absolute;
            bgImg.style.top = 0; bgImg.style.left = 0; bgImg.style.right = 0; bgImg.style.bottom = 0;
            bgImg.scaleMode = ScaleMode.ScaleAndCrop;
            w.Add(bgImg);
        }
        else
        {
            w.style.backgroundColor = new Color(0.2f, 0.18f, 0.22f, 0.85f);
        }

        var banner = new VisualElement();
        banner.style.height = 36;
        banner.style.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 0.75f);
        banner.style.flexDirection = FlexDirection.Column;
        banner.style.justifyContent = Justify.Center;
        banner.style.paddingLeft = 10; banner.style.paddingRight = 10;
        banner.style.overflow = Overflow.Hidden;
        w.Add(banner);

        var title = new Label("回忆 · 松桥午后");
        title.style.fontSize = 12; title.style.color = GlassAccent; title.style.unityFontDefinition = Fd();
        title.style.overflow = Overflow.Hidden;
        title.style.textOverflow = TextOverflow.Ellipsis;
        banner.Add(title);

        var subtitle = new Label("轻触打开相册回顾");
        subtitle.style.fontSize = 10; subtitle.style.color = GlassTextDim; subtitle.style.unityFontDefinition = Fd();
        subtitle.style.overflow = Overflow.Hidden;
        subtitle.style.textOverflow = TextOverflow.Ellipsis;
        banner.Add(subtitle);

        w.RegisterCallback<PointerEnterEvent>(_ => w.style.scale = new Scale(new Vector2(1.04f, 1.04f)));
        w.RegisterCallback<PointerLeaveEvent>(_ => w.style.scale = new Scale(new Vector2(1f, 1f)));
        w.RegisterCallback<ClickEvent>(_ => LaunchApp("相册"));
        return w;
    }

    private VisualElement BuildCalendarWidget()
    {
        var w = new VisualElement();
        w.style.width = 155; w.style.height = 115;
        w.style.marginLeft = 6; w.style.marginRight = 6;
        w.style.backgroundColor = new Color(0.96f, 0.95f, 0.92f, 0.95f); // 日历纸白
        w.style.borderTopLeftRadius = 20; w.style.borderTopRightRadius = 20;
        w.style.borderBottomLeftRadius = 20; w.style.borderBottomRightRadius = 20;
        w.style.borderTopWidth = 1; w.style.borderBottomWidth = 1;
        w.style.borderLeftWidth = 1; w.style.borderRightWidth = 1;
        w.style.borderTopColor = new Color(0.85f, 0.82f, 0.78f, 0.8f);
        w.style.borderBottomColor = new Color(0.85f, 0.82f, 0.78f, 0.8f);
        w.style.borderLeftColor = new Color(0.85f, 0.82f, 0.78f, 0.8f);
        w.style.borderRightColor = new Color(0.85f, 0.82f, 0.78f, 0.8f);
        w.style.overflow = Overflow.Hidden;
        w.style.justifyContent = Justify.SpaceBetween;
        w.pickingMode = PickingMode.Position;

        // 顶部红色日历横条
        var topBar = new VisualElement();
        topBar.style.height = 26;
        topBar.style.backgroundColor = new Color(0.82f, 0.22f, 0.22f, 0.95f);
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.justifyContent = Justify.SpaceBetween;
        topBar.style.alignItems = Align.Center;
        topBar.style.paddingLeft = 12; topBar.style.paddingRight = 12;
        w.Add(topBar);

        var monthLbl = new Label("10月 · 运营历");
        monthLbl.style.fontSize = 11; monthLbl.style.color = Color.white; monthLbl.style.unityFontDefinition = Fd();
        topBar.Add(monthLbl);

        var dayIcon = new Label("🗓");
        dayIcon.style.fontSize = 12;
        topBar.Add(dayIcon);

        // 中部大天数（居中撑开）
        var centerBox = new VisualElement();
        centerBox.style.flexGrow = 1;
        centerBox.style.alignItems = Align.Center;
        centerBox.style.justifyContent = Justify.Center;
        w.Add(centerBox);

        var dayNum = new Label(GameData.Day.ToString());
        dayNum.style.fontSize = 38; dayNum.style.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        dayNum.style.unityFontDefinition = Fd();
        centerBox.Add(dayNum);

        var eraLbl = new Label("主体历 105 年");
        eraLbl.style.fontSize = 11; eraLbl.style.color = new Color(0.45f, 0.45f, 0.5f, 0.85f);
        eraLbl.style.unityFontDefinition = Fd();
        eraLbl.style.paddingBottom = 8;
        eraLbl.style.alignSelf = Align.Center;
        w.Add(eraLbl);

        w.RegisterCallback<PointerEnterEvent>(_ => w.style.scale = new Scale(new Vector2(1.04f, 1.04f)));
        w.RegisterCallback<PointerLeaveEvent>(_ => w.style.scale = new Scale(new Vector2(1f, 1f)));
        w.RegisterCallback<ClickEvent>(_ => LaunchApp("日历"));
        return w;
    }

    private VisualElement BuildDockIcon(string name)
    {
        var box = new VisualElement();
        box.style.width = 46; box.style.height = 44;
        box.style.marginLeft = 16; box.style.marginRight = 16;
        box.style.alignItems = Align.Center;
        box.style.justifyContent = Justify.Center;
        box.style.backgroundColor = new Color(0.2f, 0.22f, 0.3f, 0.5f);
        box.style.borderTopLeftRadius = 14; box.style.borderTopRightRadius = 14;
        box.style.borderBottomLeftRadius = 14; box.style.borderBottomRightRadius = 14;
        box.pickingMode = PickingMode.Position;

        var lbl = new Label(IconFor(name));
        lbl.style.fontSize = 24;
        lbl.style.color = new Color(0.92f, 0.94f, 0.98f, 0.95f);
        lbl.style.unityFontDefinition = Fd();
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        box.Add(lbl);

        box.RegisterCallback<PointerEnterEvent>(_ =>
        {
            box.style.scale = new Scale(new Vector2(1.15f, 1.15f));
            box.style.backgroundColor = new Color(0.3f, 0.35f, 0.5f, 0.85f);
        });
        box.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            box.style.scale = new Scale(new Vector2(1f, 1f));
            box.style.backgroundColor = new Color(0.2f, 0.22f, 0.3f, 0.5f);
        });
        box.RegisterCallback<ClickEvent>(_ => LaunchApp(name));
        return box;
    }

    private VisualElement BuildRailwayFolderIcon()
    {
        var cell = new VisualElement();
        cell.style.width = 96; cell.style.height = 88;
        cell.style.alignItems = Align.Center;
        cell.style.justifyContent = Justify.FlexEnd;
        cell.style.marginLeft = 14; cell.style.marginRight = 14;
        cell.style.marginTop = 10; cell.style.marginBottom = 10;
        cell.style.paddingBottom = 10;
        cell.pickingMode = PickingMode.Position;

        // 文件夹外壳（半透明毛玻璃）
        var folderBox = new VisualElement();
        folderBox.style.width = 58; folderBox.style.height = 52;
        folderBox.style.backgroundColor = new Color(0.18f, 0.24f, 0.35f, 0.72f);
        folderBox.style.borderTopLeftRadius = 16; folderBox.style.borderTopRightRadius = 16;
        folderBox.style.borderBottomLeftRadius = 16; folderBox.style.borderBottomRightRadius = 16;
        folderBox.style.borderTopWidth = 1; folderBox.style.borderBottomWidth = 1;
        folderBox.style.borderLeftWidth = 1; folderBox.style.borderRightWidth = 1;
        folderBox.style.borderTopColor = GlassBorderSoft; folderBox.style.borderBottomColor = GlassBorderSoft;
        folderBox.style.borderLeftColor = GlassBorderSoft; folderBox.style.borderRightColor = GlassBorderSoft;
        folderBox.style.flexDirection = FlexDirection.Row;
        folderBox.style.flexWrap = Wrap.Wrap;
        folderBox.style.justifyContent = Justify.Center;
        folderBox.style.alignItems = Align.Center;
        folderBox.style.paddingLeft = 4; folderBox.style.paddingRight = 4;
        folderBox.style.paddingTop = 4; folderBox.style.paddingBottom = 4;
        cell.Add(folderBox);

        string[] miniIcons = { "归", "百", "♪", "R" };
        foreach (var mi in miniIcons)
        {
            var ml = new Label(mi);
            ml.style.fontSize = 11;
            ml.style.color = GlassAccent;
            ml.style.unityFontDefinition = Fd();
            ml.style.width = 22; ml.style.height = 18;
            ml.style.unityTextAlign = TextAnchor.MiddleCenter;
            folderBox.Add(ml);
        }

        var label = new Label("铁路");
        label.style.fontSize = 13;
        label.style.color = new Color(0.15f, 0.15f, 0.18f, 0.85f);
        label.style.unityFontDefinition = Fd();
        label.style.marginTop = 6;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        cell.Add(label);

        cell.RegisterCallback<PointerEnterEvent>(_ => folderBox.style.scale = new Scale(new Vector2(1.08f, 1.08f)));
        cell.RegisterCallback<PointerLeaveEvent>(_ => folderBox.style.scale = new Scale(new Vector2(1f, 1f)));
        cell.RegisterCallback<ClickEvent>(_ => OpenRailwayFolder());
        return cell;
    }

    private void BuildFolderOverlay()
    {
        folderOverlay = new VisualElement { name = "railway-folder-overlay" };
        folderOverlay.style.position = Position.Absolute;
        folderOverlay.style.top = 0; folderOverlay.style.left = 0;
        folderOverlay.style.right = 0; folderOverlay.style.bottom = 0;
        folderOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        folderOverlay.style.alignItems = Align.Center;
        folderOverlay.style.justifyContent = Justify.Center;
        folderOverlay.style.display = DisplayStyle.None;
        desktop.Add(folderOverlay);

        folderOverlay.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == folderOverlay)
                CloseRailwayFolder();
        });

        var card = new VisualElement();
        card.style.width = 380; card.style.height = 280;
        card.style.backgroundColor = SurfaceCard;
        card.style.borderTopLeftRadius = 24; card.style.borderTopRightRadius = 24;
        card.style.borderBottomLeftRadius = 24; card.style.borderBottomRightRadius = 24;
        card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
        card.style.borderTopColor = BorderCard; card.style.borderBottomColor = BorderCard;
        card.style.borderLeftColor = BorderCard; card.style.borderRightColor = BorderCard;
        card.style.paddingLeft = 24; card.style.paddingRight = 24;
        card.style.paddingTop = 18; card.style.paddingBottom = 20;
        folderOverlay.Add(card);

        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 16;
        card.Add(header);

        string savedFolderName = PlayerPrefs.GetString("TabletFolders", "铁路");
        var titleRow = new VisualElement();
        titleRow.style.flexDirection = FlexDirection.Row;
        titleRow.style.alignItems = Align.Center;
        header.Add(titleRow);

        var title = new Label(savedFolderName);
        title.style.fontSize = 20;
        title.style.color = InkPrimary;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = Fd();
        titleRow.Add(title);

        var editBtn = new Button { text = "✎" };
        editBtn.style.width = 24; editBtn.style.height = 24;
        editBtn.style.fontSize = 12; editBtn.style.marginLeft = 8;
        editBtn.style.backgroundColor = Color.clear; editBtn.style.color = InkSecondary;
        titleRow.Add(editBtn);

        var renameField = new TextField { value = savedFolderName };
        renameField.style.width = 120; renameField.style.height = 28;
        renameField.style.fontSize = 15; renameField.style.unityFontDefinition = Fd();
        renameField.style.display = DisplayStyle.None;
        titleRow.Add(renameField);

        editBtn.clicked += () =>
        {
            title.style.display = DisplayStyle.None;
            editBtn.style.display = DisplayStyle.None;
            renameField.style.display = DisplayStyle.Flex;
            renameField.Focus();
        };

        renameField.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (!string.IsNullOrEmpty(renameField.value.Trim()))
            {
                title.text = renameField.value.Trim();
                PlayerPrefs.SetString("TabletFolders", title.text);
                PlayerPrefs.Save();
            }
            renameField.style.display = DisplayStyle.None;
            title.style.display = DisplayStyle.Flex;
            editBtn.style.display = DisplayStyle.Flex;
        });

        renameField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                renameField.Blur();
            }
        });

        var closeBtn = new Button(() => CloseRailwayFolder()) { text = "✕" };
        closeBtn.style.width = 28; closeBtn.style.height = 28;
        closeBtn.style.fontSize = 14;
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = Fd();
        closeBtn.style.backgroundColor = new Color(0.92f, 0.93f, 0.95f, 1f);
        closeBtn.style.color = InkSecondary;
        closeBtn.style.borderTopLeftRadius = 14; closeBtn.style.borderTopRightRadius = 14;
        closeBtn.style.borderBottomLeftRadius = 14; closeBtn.style.borderBottomRightRadius = 14;
        header.Add(closeBtn);

        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        grid.style.justifyContent = Justify.SpaceAround;
        card.Add(grid);

        string[] folderApps = { "归途", "媒姬百科", "音乐", "RDA" };
        foreach (var fa in folderApps)
        {
            var item = BuildAppIcon(fa);
            item.style.marginLeft = 10; item.style.marginRight = 10;
            item.RegisterCallback<ClickEvent>(_ => CloseRailwayFolder());
            grid.Add(item);
        }
    }

    private void OpenRailwayFolder()
    {
        if (folderOverlay != null)
            folderOverlay.style.display = DisplayStyle.Flex;
    }

    private void CloseRailwayFolder()
    {
        if (folderOverlay != null)
            folderOverlay.style.display = DisplayStyle.None;
    }

    private void AddWallpaperRidge(VisualElement parent)
    {
        // T298: 清理字符远山，避免在像素字体下渲染为底部的4条水平虚线
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

        // 根据App类型选择背景色（HyperOS 原生实色不透明底）
        Color iconBg;
        Color iconFg;
        switch (name)
        {
            case "设置": case "时钟": case "日历": case "计算器": case "文件管理":
                iconBg = new Color(0.92f, 0.93f, 0.95f, 1f);  // 系统工具：浅灰白实底
                iconFg = new Color(0.18f, 0.18f, 0.22f, 1f);
                break;
            case "天气":
                iconBg = new Color(0.25f, 0.58f, 0.92f, 1f);  // 天气蓝
                iconFg = Color.white;
                break;
            case "笔记":
                iconBg = new Color(0.96f, 0.76f, 0.26f, 1f);  // 便签暖黄
                iconFg = new Color(0.2f, 0.15f, 0.05f, 1f);
                break;
            case "音乐":
                iconBg = new Color(0.22f, 0.75f, 0.45f, 1f);  // 音乐草绿
                iconFg = Color.white;
                break;
            case "相册":
                iconBg = new Color(0.95f, 0.42f, 0.32f, 1f);  // 相册珊瑚粉红
                iconFg = Color.white;
                break;
            case "地图":
                iconBg = new Color(0.22f, 0.68f, 0.85f, 1f);  // 地图天蓝
                iconFg = Color.white;
                break;
            case "铁路运营": case "RDA 助手": case "岁月": case "站务日志": case "列车图鉴":
            case "铁路": case "驾驶台":
                iconBg = new Color(0.18f, 0.38f, 0.65f, 1f);  // 铁路功能：深工装蓝实底
                iconFg = new Color(0.92f, 0.96f, 1f, 1f);
                break;
            case "阿里郎商店": case "未来网": case "白头疫苗":
                iconBg = new Color(0.78f, 0.22f, 0.22f, 1f);  // 朝鲜App：朝鲜国旗红实底
                iconFg = Color.white;
                break;
            case "应用商店":
                iconBg = new Color(0.95f, 0.52f, 0.18f, 1f);  // 应用市场：橙色实底
                iconFg = Color.white;
                break;
            case "安全中心":
                iconBg = new Color(0.28f, 0.72f, 0.42f, 1f);  // 安全中心：绿盾实底
                iconFg = Color.white;
                break;
            case "浏览器":
                iconBg = new Color(0.20f, 0.52f, 0.92f, 1f);  // 浏览器蓝实底
                iconFg = Color.white;
                break;
            case "米家":
                iconBg = new Color(0.95f, 0.45f, 0.20f, 1f);  // 米家橙
                iconFg = Color.white;
                break;
            default:
                iconBg = new Color(0.88f, 0.90f, 0.93f, 1f);
                iconFg = new Color(0.2f, 0.2f, 0.25f, 1f);
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
        label.style.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        label.style.unityFontDefinition = Fd();
        label.style.marginTop = 6;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        cell.Add(label);

        // hover 放大
        cell.RegisterCallback<PointerEnterEvent>(_ =>
        {
            icon.style.scale = new Scale(new Vector2(1.08f, 1.08f));
        });
        cell.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            icon.style.scale = new Scale(new Vector2(1f, 1f));
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
            case "驾驶台": return "🚂";
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

    private VisualElement BuildFullScreenAppContainer(string appTitle, string subtitle = null, System.Action onBack = null)
    {
        appLayer.Clear();
        // 全屏视窗（覆盖整个视口，HyperOS 真实安卓平板应用容器：白底/极浅灰）
        var win = new VisualElement { name = "app-fullscreen-window" };
        win.style.position = Position.Absolute;
        win.style.top = 0; win.style.left = 0; win.style.right = 0; win.style.bottom = 0;
        win.style.backgroundColor = SurfaceApp;
        win.style.flexDirection = FlexDirection.Column;
        appLayer.Add(win);

        // 系统级平板顶栏（高度 48px，纯白底 + 1px 细边线）
        var topBar = new VisualElement();
        topBar.style.height = 48;
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.justifyContent = Justify.SpaceBetween;
        topBar.style.alignItems = Align.Center;
        topBar.style.paddingLeft = 20; topBar.style.paddingRight = 20;
        topBar.style.backgroundColor = SurfaceTopBar;
        topBar.style.borderBottomWidth = 1;
        topBar.style.borderBottomColor = BorderDivider;
        win.Add(topBar);

        // 左侧：返回桌面胶囊按钮 + 应用名
        var leftGroup = new VisualElement();
        leftGroup.style.flexDirection = FlexDirection.Row;
        leftGroup.style.alignItems = Align.Center;
        topBar.Add(leftGroup);

        var backBtn = new Button(() =>
        {
            if (onBack != null) onBack();
            else appLayer.style.display = DisplayStyle.None;
        }) { text = "◀  返回桌面" };
        backBtn.style.height = 32;
        backBtn.style.paddingLeft = 14; backBtn.style.paddingRight = 14;
        backBtn.style.fontSize = 13;
        backBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        backBtn.style.unityFontDefinition = Fd();
        backBtn.style.backgroundColor = new Color(0.92f, 0.93f, 0.95f, 1f);
        backBtn.style.color = InkPrimary;
        backBtn.style.borderTopLeftRadius = 16; backBtn.style.borderTopRightRadius = 16;
        backBtn.style.borderBottomLeftRadius = 16; backBtn.style.borderBottomRightRadius = 16;
        backBtn.style.borderTopWidth = 1; backBtn.style.borderBottomWidth = 1;
        backBtn.style.borderLeftWidth = 1; backBtn.style.borderRightWidth = 1;
        backBtn.style.borderTopColor = BorderCard; backBtn.style.borderBottomColor = BorderCard;
        backBtn.style.borderLeftColor = BorderCard; backBtn.style.borderRightColor = BorderCard;
        backBtn.RegisterCallback<PointerEnterEvent>(_ => backBtn.style.backgroundColor = SurfaceHover);
        backBtn.RegisterCallback<PointerLeaveEvent>(_ => backBtn.style.backgroundColor = new Color(0.92f, 0.93f, 0.95f, 1f));
        leftGroup.Add(backBtn);

        var titleLabel = new Label(appTitle);
        titleLabel.style.fontSize = 17;
        titleLabel.style.color = InkPrimary;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityFontDefinition = Fd();
        titleLabel.style.marginLeft = 16;
        leftGroup.Add(titleLabel);

        if (!string.IsNullOrEmpty(subtitle))
        {
            var subLabel = new Label("· " + subtitle);
            subLabel.style.fontSize = 13;
            subLabel.style.color = InkSecondary;
            subLabel.style.unityFontDefinition = Fd();
            subLabel.style.marginLeft = 10;
            leftGroup.Add(subLabel);
        }

        // 右侧关闭按钮
        var closeBtn = new Button(() => { appLayer.style.display = DisplayStyle.None; }) { text = "✕" };
        closeBtn.style.width = 32; closeBtn.style.height = 32;
        closeBtn.style.fontSize = 15;
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = Fd();
        closeBtn.style.backgroundColor = new Color(0.94f, 0.94f, 0.96f, 1f);
        closeBtn.style.color = InkSecondary;
        closeBtn.style.borderTopLeftRadius = 16; closeBtn.style.borderTopRightRadius = 16;
        closeBtn.style.borderBottomLeftRadius = 16; closeBtn.style.borderBottomRightRadius = 16;
        closeBtn.style.borderTopWidth = 1; closeBtn.style.borderBottomWidth = 1;
        closeBtn.style.borderLeftWidth = 1; closeBtn.style.borderRightWidth = 1;
        closeBtn.style.borderTopColor = BorderCard; closeBtn.style.borderBottomColor = BorderCard;
        closeBtn.style.borderLeftColor = BorderCard; closeBtn.style.borderRightColor = BorderCard;
        closeBtn.RegisterCallback<PointerEnterEvent>(_ => { closeBtn.style.backgroundColor = new Color(0.95f, 0.4f, 0.4f, 0.2f); closeBtn.style.color = new Color(0.85f, 0.2f, 0.2f, 1f); });
        closeBtn.RegisterCallback<PointerLeaveEvent>(_ => { closeBtn.style.backgroundColor = new Color(0.94f, 0.94f, 0.96f, 1f); closeBtn.style.color = InkSecondary; });
        topBar.Add(closeBtn);

        // 主体内容区（供具体 App 渲染）
        var content = new VisualElement { name = "app-content" };
        content.style.flexGrow = 1;
        content.style.flexShrink = 1;
        content.style.paddingLeft = 24; content.style.paddingRight = 24;
        content.style.paddingTop = 18; content.style.paddingBottom = 18;
        win.Add(content);

        appLayer.style.display = DisplayStyle.Flex;
        return content;
    }

    private string AppSubtitle(string name) => name switch
    {
        "五线态势" => "宏观经营五线趋势看板",
        "RDA" => "铁路行车调度助手",
        "驾驶台" => "0721型机车仿真视界",
        "归途" => "剧情回顾 · Return Journey",
        "媒姬百科" => "铁路知识图鉴",
        "相册" => "岁月回忆与场景收集",
        "设置" => "系统偏好与本地备份",
        "笔记" => "运营备忘与乘务笔记",
        "音乐" => "内网铁道广播台",
        "时钟" => "阿里郎高精度授时",
        "日历" => "主体历与公历",
        "天气" => "气象水文监视",
        "计算器" => "客货运计价工具",
        "米家" => "0721机车遥测网关",
        "安全中心" => "内网防火墙",
        "浏览器" => "光明内网浏览",
        "应用商店" => "阿里郎应用市场",
        "阿里郎商店" => "国营物资订购",
        "未来网" => "科教内网",
        "白头疫苗" => "终端防御软件",
        "文件管理" => "存储卷与日志",
        "地图" => "铁路干线走向图",
        _ => "系统应用"
    };

    private void LaunchApp(string name)
    {
        if (name == "驾驶台")
        {
            OpenCabApp();
            return;
        }

        var win = BuildFullScreenAppContainer(name, AppSubtitle(name));

        switch (name)
        {
            case "五线态势": RailwayRenaissance.UI.TabletTrendDashboardUI.Build(win); break;
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

    private void OpenCabApp()
    {
        if (root != null) root.style.display = DisplayStyle.None;
        var cabUI = RailwayRenaissance.Cab.CabConsoleUI.Create();
        cabUI.OnCloseRequested = () =>
        {
            if (cabUI != null) Destroy(cabUI.gameObject);
            if (root != null) root.style.display = DisplayStyle.Flex;
        };
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

    // ============ App: 设置（已抽离至 TabletSettingsAppUI）============
    private void BuildSettingsPage(VisualElement win) => TabletSettingsAppUI.BuildSettingsPage(win, ShowBackupToast);
    private void ShowAffairsPage() => TabletSettingsAppUI.ShowAffairsPage();
    private void ShowStoryPage() => TabletSettingsAppUI.ShowStoryPage();

    public static void OpenTipsPage(string termId)
    {
        if (Instance == null)
        {
            var go = new GameObject("GameMainUI");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameMainUI>();
            try
            {
                Instance.Init();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GameMainUI] OpenTipsPage Init failed: " + ex);
                Instance = null;
                Object.Destroy(go);
                return;
            }
        }
        Instance.EnsureDesktopBuiltSync();
        // 图鉴直达：不走锁屏，直接进桌面态
        Instance.unlocked = true;
        if (Instance.lockScreen != null) Instance.lockScreen.style.display = DisplayStyle.None;
        if (Instance.desktop != null) Instance.desktop.style.display = DisplayStyle.Flex;
        Instance.root.style.display = DisplayStyle.Flex;
        TabletSettingsAppUI.ShowTipsPage(termId);
    }

    /// <summary>同步补建桌面/App层（后台错帧协程可能还没跑）；只补缺的部分，避免与协程重复构建。</summary>
    private void EnsureDesktopBuiltSync()
    {
        if (desktopBuilt) return;
        if (desktop == null) BuildDesktop();
        if (appLayer == null) BuildAppLayer();
        desktopBuilt = true;
        desktopBuilding = false;
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

    // ============ App: RDA（已抽离至 TabletRDAAppUI）============
    private void BuildRDAApp(VisualElement win) => TabletRDAAppUI.BuildRDAApp(win, LaunchApp, () => { if (appLayer != null) appLayer.style.display = DisplayStyle.None; ShowStoryPage(); }, () => { if (appLayer != null) appLayer.style.display = DisplayStyle.None; ShowAffairsPage(); }, ShowBackupToast);

    // ============ App: 归途（已抽离至 TabletReturnJourneyUI）============
    private static (string scriptId, string episode, string title, string desc)[] PrologueEpisodes => TabletReturnJourneyUI.PrologueEpisodes;
    private void BuildReturnJourneyApp(VisualElement win) => TabletReturnJourneyUI.BuildReturnJourneyApp(win, () => { if (appLayer != null) appLayer.style.display = DisplayStyle.None; }, ShowBackupToast);

    // ============ App: 媒姬百科与便签（已抽离至 TabletNotesAppUI）============
    private void BuildWikiApp(VisualElement win) => TabletNotesAppUI.BuildWikiApp(win);
    private void BuildNotesApp(VisualElement win) => TabletNotesAppUI.BuildNotesApp(win, ShowBackupToast);

    // ============ App: 音乐（已抽离至 TabletMusicAppUI）============
    private void BuildMusicApp(VisualElement win) => TabletMusicAppUI.BuildMusicApp(win, ShowBackupToast);
    private void PlayNextTabletTrack() => TabletMusicAppUI.PlayNextTabletTrack();
    private void PlayPrevTabletTrack() => TabletMusicAppUI.PlayPrevTabletTrack();

    // ============ App: 相册（已抽离至 TabletGalleryAppUI）============
    private void BuildGalleryApp(VisualElement win) => TabletGalleryAppUI.BuildGalleryApp(win, appLayer, ShowBackupToast);

    // ============ 统一 App 浅色卡片骨架（T298 规范，下沉至 TabletUIHelper）============
    private VisualElement BuildAppBody(VisualElement win, string pageTitle, string pageSub = null) => TabletUIHelper.BuildAppBody(win, pageTitle, pageSub);
    private VisualElement BuildAppCard(string header = null) => TabletUIHelper.BuildAppCard(header);
    private VisualElement BuildLightRow(string title, string desc = null, System.Action onClick = null, bool hasArrow = true) => TabletUIHelper.BuildLightRow(title, desc, onClick, hasArrow);

    // ============ App: 轻量系统工具集（已抽离至 TabletSystemToolsAppUI）============
    private void BuildClockApp(VisualElement win) => TabletSystemToolsAppUI.BuildClockApp(win);
    private void BuildCalendarApp(VisualElement win) => TabletSystemToolsAppUI.BuildCalendarApp(win);
    private void BuildWeatherApp(VisualElement win) => TabletSystemToolsAppUI.BuildWeatherApp(win);
    private void BuildCalculatorApp(VisualElement win) => TabletSystemToolsAppUI.BuildCalculatorApp(win);
    private void BuildMijiaApp(VisualElement win) => TabletSystemToolsAppUI.BuildMijiaApp(win);
    private void BuildSecurityApp(VisualElement win) => TabletSystemToolsAppUI.BuildSecurityApp(win);
    private void BuildBrowserApp(VisualElement win) => TabletSystemToolsAppUI.BuildBrowserApp(win);
    private void BuildAppStoreApp(VisualElement win) => TabletSystemToolsAppUI.BuildAppStoreApp(win);
    private void BuildArirangStoreApp(VisualElement win) => TabletSystemToolsAppUI.BuildArirangStoreApp(win);
    private void BuildMiraeNetApp(VisualElement win) => TabletSystemToolsAppUI.BuildMiraeNetApp(win);
    private void BuildSiliVaccineApp(VisualElement win) => TabletSystemToolsAppUI.BuildSiliVaccineApp(win);
    private void BuildFileManagerApp(VisualElement win) => TabletSystemToolsAppUI.BuildFileManagerApp(win);
    private void BuildMapApp(VisualElement win) => TabletSystemToolsAppUI.BuildMapApp(win);
    private void BuildPlaceholderApp(VisualElement win, string name) => TabletSystemToolsAppUI.BuildPlaceholderApp(win, name);

    public VisualElement BuildInnerPanel(string title)
    {
        return BuildFullScreenAppContainer(title);
    }

    private void Update()
    {
        if (root == null || root.style.display == DisplayStyle.None) return;

        // 触控笔与键盘：按键重绑定监听
        if (_tabletRebindAction != null)
        {
            foreach (KeyCode code in AllKeyCodes)
            {
                if (LegacyInputAdapter.GetKeyDown(code))
                {
                    var cb = _tabletRebindAction;
                    _tabletRebindAction = null;
                    cb(code);
                    return;
                }
            }
        }

        // 刷新状态栏时钟
        clockTimer += Time.unscaledDeltaTime;
        if (clockTimer > 1f)
        {
            clockTimer = 0;
            string hhmm = System.DateTime.Now.ToString("HH:mm");
            if (statusClock != null) statusClock.text = hhmm;
            var lc = lockScreen?.Q<Label>("lock-clock");
            if (lc != null) lc.text = hhmm;
            // 电量每 30 秒同步一次设备真实电量
            battTick++;
            if (battTick >= 30 && statusBatt != null)
            {
                battTick = 0;
                statusBatt.text = FormatBatt(GetDeviceBatteryPercent());
            }
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

        // 随身听音乐播放器进度同步（已下沉至 TabletMusicAppUI）
        TabletMusicAppUI.UpdateAudioProgress();
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