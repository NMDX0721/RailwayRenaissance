using System.Collections;
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

    // ============ 设置 App：严格照截图 24 分类 + 老设置全量迁入（T298 规范）============
    private const string BackupJsonKey = "SUIYUE_Backup_Json";
    private const string BackupTimeKey = "SUIYUE_Backup_Time";

    private int currentSettingsCategory = 1; // 默认选中 1 (当前账号) 或 13 (显示)
    private string settingsSearchFilter = "";
    private int accessibilityTab = 0; // 0:常规 1:视觉 2:听觉 3:肢体
    private static bool isTabletDevModeUnlocked = false;
    private static int devModeClickCount = 0;

    private static readonly (int id, string name, string icon, string group)[] SettingsCategories = {
        (1, "当前账号", "👤", "account"),
        (2, "我的设备", "📱", "device"),
        (3, "平板特色功能", "🧩", "device"),
        (4, "WLAN", "📶", "connect"),
        (5, "蓝牙", "ᛒ", "connect"),
        (6, "互联互通", "🔗", "connect"),
        (7, "更多连接方式", "🌐", "connect"),
        (8, "小米超级AI", "🔮", "system"),
        (9, "个性化", "🎨", "system"),
        (10, "锁屏", "🔒", "system"),
        (11, "通知与状态栏", "🔔", "system"),
        (12, "桌面", "🏠", "system"),
        (13, "显示与亮度", "☀", "system"),
        (14, "声音与振动", "🔊", "system"),
        (15, "触控笔与键盘", "⌨", "system"),
        (16, "人脸与锁屏密码", "😊", "security"),
        (17, "隐私与安全", "🛡", "security"),
        (18, "应用", "▦", "security"),
        (19, "电池", "🔋", "security"),
        (20, "健康使用设备", "💚", "other"),
        (21, "儿童模式", "🧒", "other"),
        (22, "无障碍", "♿", "other"),
        (23, "更多设置", "⋯", "other"),
        (24, "反馈", "💬", "feedback")
    };

    private void BuildSettingsPage(VisualElement win)
    {
        win.Clear();

        var rootRow = new VisualElement();
        rootRow.style.flexDirection = FlexDirection.Row;
        rootRow.style.flexGrow = 1;
        win.Add(rootRow);

        // 左栏：300px 纯白/极浅灰导航条，含搜索框与 24 项
        var leftNav = new VisualElement();
        leftNav.style.width = 300;
        leftNav.style.backgroundColor = SurfaceCard;
        leftNav.style.borderRightWidth = 1;
        leftNav.style.borderRightColor = BorderDivider;
        leftNav.style.flexDirection = FlexDirection.Column;
        leftNav.style.paddingLeft = 14; leftNav.style.paddingRight = 14;
        leftNav.style.paddingTop = 16; leftNav.style.paddingBottom = 16;
        rootRow.Add(leftNav);

        var titleLbl = new Label("设置");
        titleLbl.style.fontSize = 24; titleLbl.style.color = InkPrimary;
        titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLbl.style.unityFontDefinition = Fd();
        titleLbl.style.marginBottom = 12;
        leftNav.Add(titleLbl);

        // 搜索框
        var searchBox = new VisualElement();
        searchBox.style.height = 36;
        searchBox.style.backgroundColor = SurfaceApp;
        searchBox.style.borderTopLeftRadius = 18; searchBox.style.borderTopRightRadius = 18;
        searchBox.style.borderBottomLeftRadius = 18; searchBox.style.borderBottomRightRadius = 18;
        searchBox.style.flexDirection = FlexDirection.Row;
        searchBox.style.alignItems = Align.Center;
        searchBox.style.paddingLeft = 12; searchBox.style.paddingRight = 12;
        searchBox.style.marginBottom = 12;
        leftNav.Add(searchBox);

        var searchIcon = new Label("🔍");
        searchIcon.style.fontSize = 13; searchIcon.style.color = InkTertiary;
        searchIcon.style.unityFontDefinition = Fd(); searchIcon.style.marginRight = 6;
        searchBox.Add(searchIcon);

        var searchInput = new TextField { value = settingsSearchFilter };
        searchInput.style.flexGrow = 1;
        searchInput.style.borderTopWidth = 0; searchInput.style.borderBottomWidth = 0;
        searchInput.style.borderLeftWidth = 0; searchInput.style.borderRightWidth = 0;
        searchInput.style.backgroundColor = Color.clear;
        searchInput.style.fontSize = 13; searchInput.style.color = InkPrimary;
        searchInput.style.unityFontDefinition = Fd();
        searchInput.RegisterValueChangedCallback(evt =>
        {
            settingsSearchFilter = evt.newValue;
            BuildSettingsPage(win);
        });
        searchBox.Add(searchInput);

        // 分类滚动列表（隐藏滚动条）
        var navScroll = new ScrollView();
        navScroll.style.flexGrow = 1;
        navScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        navScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        leftNav.Add(navScroll);

        string lastGroup = "";
        for (int i = 0; i < SettingsCategories.Length; i++)
        {
            var cat = SettingsCategories[i];
            if (!string.IsNullOrEmpty(settingsSearchFilter) && !cat.name.Contains(settingsSearchFilter))
                continue;

            if (cat.group != lastGroup && i > 0)
            {
                lastGroup = cat.group;
                var div = new VisualElement();
                div.style.height = 1; div.style.backgroundColor = BorderDivider;
                div.style.marginTop = 6; div.style.marginBottom = 6;
                navScroll.Add(div);
            }
            else if (i == 0) lastGroup = cat.group;

            int catId = cat.id;
            bool isSelected = (catId == currentSettingsCategory);

            var itemBtn = new Button(() =>
            {
                currentSettingsCategory = catId;
                BuildSettingsPage(win);
            });
            itemBtn.style.height = 42;
            itemBtn.style.flexDirection = FlexDirection.Row;
            itemBtn.style.alignItems = Align.Center;
            itemBtn.style.paddingLeft = 12; itemBtn.style.paddingRight = 12;
            itemBtn.style.marginBottom = 3;
            itemBtn.style.backgroundColor = isSelected ? new Color(0.93f, 0.93f, 0.95f, 1f) : Color.clear;
            itemBtn.style.borderTopLeftRadius = 10; itemBtn.style.borderTopRightRadius = 10;
            itemBtn.style.borderBottomLeftRadius = 10; itemBtn.style.borderBottomRightRadius = 10;
            itemBtn.style.borderTopWidth = 0; itemBtn.style.borderBottomWidth = 0;
            itemBtn.style.borderLeftWidth = 0; itemBtn.style.borderRightWidth = 0;

            var icLbl = new Label(cat.icon);
            icLbl.style.fontSize = 16; icLbl.style.marginRight = 10;
            icLbl.style.unityFontDefinition = Fd();
            itemBtn.Add(icLbl);

            var nameLbl = new Label(cat.name);
            nameLbl.style.fontSize = 14;
            nameLbl.style.color = isSelected ? AccentSystem : InkPrimary;
            if (isSelected) nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.unityFontDefinition = Fd();
            itemBtn.Add(nameLbl);

            navScroll.Add(itemBtn);
        }

        // 若已解锁开发者模式，渲染开发者选项入口
        bool devUnlocked = isTabletDevModeUnlocked || PlayerPrefs.GetInt("TabletDevModeUnlocked", 0) == 1;
        if (devUnlocked && (string.IsNullOrEmpty(settingsSearchFilter) || "开发者选项".Contains(settingsSearchFilter)))
        {
            var devDiv = new VisualElement();
            devDiv.style.height = 1; devDiv.style.backgroundColor = BorderDivider;
            devDiv.style.marginTop = 6; devDiv.style.marginBottom = 6;
            navScroll.Add(devDiv);

            bool isDevSelected = (currentSettingsCategory == 99);
            var devBtn = new Button(() =>
            {
                currentSettingsCategory = 99;
                BuildSettingsPage(win);
            });
            devBtn.style.height = 42;
            devBtn.style.flexDirection = FlexDirection.Row;
            devBtn.style.alignItems = Align.Center;
            devBtn.style.paddingLeft = 12; devBtn.style.paddingRight = 12;
            devBtn.style.marginBottom = 3;
            devBtn.style.backgroundColor = isDevSelected ? new Color(0.93f, 0.93f, 0.95f, 1f) : Color.clear;
            devBtn.style.borderTopLeftRadius = 10; devBtn.style.borderTopRightRadius = 10;
            devBtn.style.borderBottomLeftRadius = 10; devBtn.style.borderBottomRightRadius = 10;
            devBtn.style.borderTopWidth = 0; devBtn.style.borderBottomWidth = 0;
            devBtn.style.borderLeftWidth = 0; devBtn.style.borderRightWidth = 0;

            var devIc = new Label("🛠️");
            devIc.style.fontSize = 16; devIc.style.marginRight = 10;
            devIc.style.unityFontDefinition = Fd();
            devBtn.Add(devIc);

            var devName = new Label("开发者选项");
            devName.style.fontSize = 14;
            devName.style.color = isDevSelected ? AccentSystem : new Color(0.85f, 0.45f, 0.15f, 1f);
            if (isDevSelected) devName.style.unityFontStyleAndWeight = FontStyle.Bold;
            devName.style.unityFontDefinition = Fd();
            devBtn.Add(devName);

            navScroll.Add(devBtn);
        }

        // 右栏：内容详情区（隐藏滚动条）
        var rightPane = new ScrollView();
        rightPane.style.flexGrow = 1;
        rightPane.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        rightPane.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        rightPane.style.paddingLeft = 32; rightPane.style.paddingRight = 32;
        rightPane.style.paddingTop = 20; rightPane.style.paddingBottom = 30;
        rightPane.style.backgroundColor = SurfaceApp;
        rootRow.Add(rightPane);

        RenderSettingsDetail(rightPane, win);
    }

    private void RenderSettingsDetail(ScrollView rightPane, VisualElement win)
    {
        switch (currentSettingsCategory)
        {
            case 1: RenderAccountSettings(rightPane); break;
            case 2: RenderMyDeviceSettings(rightPane, win); break;
            case 10: RenderLockScreenSettings(rightPane); break;
            case 12: RenderHomeScreenSettings(rightPane); break;
            case 13: RenderDisplaySettings(rightPane, win); break;
            case 14: RenderAudioSettings(rightPane, win); break;
            case 15: RenderControlSettings(rightPane, win); break;
            case 18: RenderAppsSettings(rightPane); break;
            case 22: RenderAccessibilitySettings(rightPane, win); break;
            case 23: RenderAdditionalSettings(rightPane); break;
            case 99: RenderDeveloperOptions(rightPane, win); break;
            default: RenderPlaceholderCategory(rightPane, currentSettingsCategory); break;
        }
    }

    // 形态 A · 账号页（对照 xiaomi.account 截图）
    private void RenderAccountSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "Xiaomi 账号");

        // 大头像卡
        var profileCard = BuildWhiteCard();
        profileCard.style.flexDirection = FlexDirection.Row;
        profileCard.style.alignItems = Align.Center;
        profileCard.style.justifyContent = Justify.SpaceBetween;
        profileCard.style.paddingTop = 18; profileCard.style.paddingBottom = 18;

        var userLeft = new VisualElement();
        userLeft.style.flexDirection = FlexDirection.Row;
        userLeft.style.alignItems = Align.Center;
        profileCard.Add(userLeft);

        var avatar = new Label("👤");
        avatar.style.fontSize = 32; avatar.style.width = 54; avatar.style.height = 54;
        avatar.style.backgroundColor = SurfaceApp;
        avatar.style.unityTextAlign = TextAnchor.MiddleCenter;
        avatar.style.borderTopLeftRadius = 27; avatar.style.borderTopRightRadius = 27;
        avatar.style.borderBottomLeftRadius = 27; avatar.style.borderBottomRightRadius = 27;
        avatar.style.unityFontDefinition = Fd();
        userLeft.Add(avatar);

        var userMeta = new VisualElement();
        userMeta.style.marginLeft = 16;
        userLeft.Add(userMeta);

        string curUser = PlayerPrefs.GetString("XiaomiAccount", "旅人 (先民特等铁道运营官)");
        var nameLbl = new Label(curUser);
        nameLbl.style.fontSize = 18; nameLbl.style.color = InkPrimary;
        nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold; nameLbl.style.unityFontDefinition = Fd();
        userMeta.Add(nameLbl);

        var idLbl = new Label("ID: 0721-DPRK · 阿里郎量子加密专线已连接 · 全功能已解锁");
        idLbl.style.fontSize = 12; idLbl.style.color = new Color(0.2f, 0.65f, 0.35f, 1f); idLbl.style.unityFontDefinition = Fd();
        idLbl.style.marginTop = 4;
        userMeta.Add(idLbl);

        var ar = new Label("›");
        ar.style.fontSize = 20; ar.style.color = InkTertiary; ar.style.unityFontDefinition = Fd();
        profileCard.Add(ar);
        rightPane.Add(profileCard);

        // 卡片网格（左大卡 + 右两小卡）
        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.justifyContent = Justify.SpaceBetween;
        grid.style.marginBottom = 14;
        rightPane.Add(grid);

        var cloudCard = BuildWhiteCard();
        cloudCard.style.width = new Length(48, LengthUnit.Percent);
        cloudCard.style.marginBottom = 0;
        cloudCard.Add(new Label("☁ 云空间") { style = { fontSize = 16, color = InkPrimary, unityFontDefinition = Fd() } });
        cloudCard.Add(new Label("已分配 512 GB 阿里郎卫星云存储 (本地量子加密缓存)") { style = { fontSize = 12, color = InkSecondary, unityFontDefinition = Fd(), marginTop = 6, whiteSpace = WhiteSpace.Normal } });
        grid.Add(cloudCard);

        var rightSubCards = new VisualElement();
        rightSubCards.style.width = new Length(48, LengthUnit.Percent);
        rightSubCards.style.flexDirection = FlexDirection.Column;
        grid.Add(rightSubCards);

        var familyCard = BuildWhiteCard();
        familyCard.style.marginBottom = 10;
        familyCard.Add(new Label("👨‍👩‍👧 家庭服务") { style = { fontSize = 15, color = InkPrimary, unityFontDefinition = Fd() } });
        familyCard.Add(new Label("已共享雾峰村先民铁道运营排班表") { style = { fontSize = 12, color = InkSecondary, unityFontDefinition = Fd(), marginTop = 4 } });
        rightSubCards.Add(familyCard);

        var findCard = BuildWhiteCard();
        findCard.style.marginBottom = 0;
        findCard.Add(new Label("📍 查找设备") { style = { fontSize = 15, color = InkPrimary, unityFontDefinition = Fd() } });
        findCard.Add(new Label("0721号机车车载终端实时在线 · 信号极佳") { style = { fontSize = 12, color = InkSecondary, unityFontDefinition = Fd(), marginTop = 4 } });
        rightSubCards.Add(findCard);

        // 分组列表
        AddSectionHeader(rightPane, "我的账号安全与权限");
        var listCard = BuildWhiteCard();
        listCard.Add(BuildWhiteRow("账号安全等级", "最高特级 · 物理硬件密匙与3D生物结构光已认证", () => ShowBackupToast("账号处于先民军工特级保护状态")));
        listCard.Add(BuildWhiteRow("个人人事档案", "金日成综合大学荣誉毕业生 · 铁道部特聘工程师", () => ShowBackupToast("档案已同步至人事与调度中心")));
        listCard.Add(BuildWhiteRow("系统权限授权", "已授权系统管理 22 个车载及内网应用", () => ShowBackupToast("车载应用沙箱权限正常运行中")));
        rightPane.Add(listCard);
    }

    // 2 我的设备：并入老设置关于 8 条 + 真实设定硬件配置（Xiaomi Pad 36 Ultra DPRK Edition）
    private void RenderMyDeviceSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "我的设备");

        var devCard = BuildWhiteCard();
        devCard.style.alignItems = Align.Center;
        devCard.style.paddingTop = 18; devCard.style.paddingBottom = 18;

        var dName = new Label("Xiaomi Pad 36 Ultra (DPRK Edition)");
        dName.style.fontSize = 20; dName.style.color = InkPrimary;
        dName.style.unityFontStyleAndWeight = FontStyle.Bold; dName.style.unityFontDefinition = Fd();
        devCard.Add(dName);

        var osName = new Label("Xiaomi HyperOS 32.0.4.1 (DPRK Edition · 先民军工特供版)");
        osName.style.fontSize = 13; osName.style.color = AccentSystem;
        osName.style.unityFontDefinition = Fd(); osName.style.marginTop = 4;
        devCard.Add(osName);

        var devCode = new Label("代号: chollima-ultra · 先民军工遗留巅峰旗舰 · 0721号机车专属配件");
        devCode.style.fontSize = 11; devCode.style.color = InkSecondary;
        devCode.style.unityFontDefinition = Fd(); devCode.style.marginTop = 3;
        devCard.Add(devCode);
        rightPane.Add(devCard);

        AddSectionHeader(rightPane, "硬件规格与军工系统参数");
        var specsCard = BuildWhiteCard();
        specsCard.Add(BuildWhiteRow("处理器 (CPU)", "先民超导纳米核 3.6GHz × 16 (32位精简指令集)", null, false));
        specsCard.Add(BuildWhiteRow("运行内存 / 存储", "64 GB LPDDR9 Ultra / 2 TB 先民固态量子闪存", null, false));
        specsCard.Add(BuildWhiteRow("屏幕规格", "14.4 英寸 Super AMOLED Ultra (3840×2160 · 144Hz · 防眩光涂层)", null, false));
        specsCard.Add(BuildWhiteRow("电池与供电", "24,000 mAh 双电芯超级固态沙能电池 (支持120W快充 / 机车逆变回充)", null, false));
        specsCard.Add(BuildWhiteRow("设备序列号", "SN-20530721-CHL-001 (0721号专属配件 · 军工固化)", null, false));
        specsCard.Add(BuildWhiteRow("安全防护固件", "白头疫苗 SiliVaccine 8.2 (军工高权限固化)", null, false));
        specsCard.Add(BuildWhiteRow("卫星通讯基带", "阿里郎军工量子加密卫星专线 (全天候链路已连接)", null, false));
        rightPane.Add(specsCard);

        AddSectionHeader(rightPane, "项目与版本信息");
        var aboutCard = BuildWhiteCard();
        aboutCard.Add(BuildWhiteRow("游戏名称", "铁路复兴：沙能冲击", null, false));

        // 当前版本行：连续点击 5 次进入开发者选项
        aboutCard.Add(BuildWhiteRow("系统版本", LoginManager.GAME_VERSION + " · HyperOS 32.0.4.1 (点击 5 次进入开发者选项)", () =>
        {
            devModeClickCount++;
            if (devModeClickCount >= 5)
            {
                devModeClickCount = 0;
                isTabletDevModeUnlocked = true;
                PlayerPrefs.SetInt("TabletDevModeUnlocked", 1);
                PlayerPrefs.Save();
                currentSettingsCategory = 99;
                ShowBackupToast("🎉 已进入开发者模式！已跳转至开发者选项。");
                BuildSettingsPage(win);
            }
            else
            {
                ShowBackupToast($"再点击 {5 - devModeClickCount} 次开启开发者选项");
            }
        }));

        aboutCard.Add(BuildWhiteRow("引擎版本", "Unity 6000.5.8f1", null, false));
        aboutCard.Add(BuildWhiteRow("渲染管线", "Universal Render Pipeline", null, false));
        aboutCard.Add(BuildWhiteRow("开发状态", "核心系统已完成，资产生成进行中", null, false));
        aboutCard.Add(BuildWhiteRow("开源许可", "MIT License", null, false));
        aboutCard.Add(BuildWhiteRow("作者", "NMDX0721", null, false));
        aboutCard.Add(BuildWhiteRow("灵感来源", "爱上火车-Last Run!!- / Stardew Valley", null, false));
        aboutCard.Add(BuildWhiteRow("字体", "Zpix (最像素)", null, false));
        aboutCard.Add(BuildWhiteRow("底层技术", "Roslyn C# / UI Toolkit / UGUI 混合渲染", null, false));
        rightPane.Add(aboutCard);
    }

    // 99 开发者选项：支持重置游戏数据与调试测试
    private void RenderDeveloperOptions(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "开发者选项");

        var noticeCard = BuildWhiteCard();
        noticeCard.style.backgroundColor = new Color(0.99f, 0.95f, 0.90f, 1f);
        noticeCard.style.borderTopColor = AccentSystem; noticeCard.style.borderBottomColor = AccentSystem;
        noticeCard.style.borderLeftColor = AccentSystem; noticeCard.style.borderRightColor = AccentSystem;
        var noticeTitle = new Label("⚠️ 开发者调试模式已激活");
        noticeTitle.style.fontSize = 15; noticeTitle.style.color = AccentSystem;
        noticeTitle.style.unityFontStyleAndWeight = FontStyle.Bold; noticeTitle.style.unityFontDefinition = Fd();
        noticeCard.Add(noticeTitle);
        var noticeDesc = new Label("此页面提供存档重置、经济数据回滚与全剧情解锁等高权限调试指令，请谨慎操作。");
        noticeDesc.style.fontSize = 12; noticeDesc.style.color = InkSecondary;
        noticeDesc.style.unityFontDefinition = Fd(); noticeDesc.style.marginTop = 4;
        noticeCard.Add(noticeDesc);
        rightPane.Add(noticeCard);

        AddSectionHeader(rightPane, "主线剧情与存档重置");
        var storyResetCard = BuildWhiteCard();
        storyResetCard.Add(BuildWhiteRow("重置主线剧情进度", "清空所有已通关剧本与幕次记录，重置回序章第01幕", () =>
        {
            for (int i = 0; i < PrologueEpisodes.Length; i++)
            {
                PlayerPrefs.DeleteKey("ArchiveStory_" + PrologueEpisodes[i].scriptId);
                PlayerPrefs.DeleteKey("ArchiveScene_" + PrologueEpisodes[i].scriptId);
            }
            PlayerPrefs.DeleteKey("VN_Got0721");
            PlayerPrefs.DeleteKey("LastReadScript");
            PlayerPrefs.DeleteKey("DebugUnlockAllChapters");
            PlayerPrefs.Save();
            ShowBackupToast("🔄 主线剧情进度已重置至序章第 01 幕！");
        }));

        storyResetCard.Add(BuildWhiteRow("一键解锁全部剧情与CG (调试用)", "解锁序章全部 10 幕剧本及所有已收录 CG 资源", () =>
        {
            PlayerPrefs.SetInt("DebugUnlockAllChapters", 1);
            for (int i = 0; i < PrologueEpisodes.Length; i++)
            {
                PlayerPrefs.SetInt("ArchiveStory_" + PrologueEpisodes[i].scriptId, 1);
                PlayerPrefs.SetInt("ArchiveScene_" + PrologueEpisodes[i].scriptId, 1);
            }
            PlayerPrefs.SetInt("VN_Got0721", 1);
            PlayerPrefs.Save();
            ShowBackupToast("✨ 已解锁全部主线章节、剧幕与剧情 CG！");
        }));
        rightPane.Add(storyResetCard);

        AddSectionHeader(rightPane, "经营与经济核数据重置");
        var ecoResetCard = BuildWhiteCard();
        ecoResetCard.Add(BuildWhiteRow("重置经济模拟数据", "恢复初始状态：资金 50,000 沙币，信任 62，车况 70，第 1 天", () =>
        {
            GameData.ResetState();
            ShowBackupToast("💰 经济核与车况数据已重置为初始运营状态！");
        }));

        ecoResetCard.Add(BuildWhiteRow("注入调试测试资金", "为当前账务追加 100,000 沙币并恢复信任", () =>
        {
            GameData.AddMoney(100000);
            GameData.AddTrust(30);
            ShowBackupToast("💳 已注资 100,000 沙币，信任已提升！");
        }));
        rightPane.Add(ecoResetCard);

        AddSectionHeader(rightPane, "出厂重置与开发者模式");
        var devDangerCard = BuildWhiteCard();
        devDangerCard.Add(BuildWhiteRow("退出开发者模式", "隐藏开发者选项并锁定调试入口", () =>
        {
            isTabletDevModeUnlocked = false;
            PlayerPrefs.DeleteKey("TabletDevModeUnlocked");
            PlayerPrefs.Save();
            currentSettingsCategory = 2;
            ShowBackupToast("🔒 已退出开发者模式。");
            BuildSettingsPage(win);
        }));

        devDangerCard.Add(BuildWhiteRow("恢复出厂设置 (清空全部本地数据)", "清空所有 PlayerPrefs 数据并重新加载系统", () =>
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            isTabletDevModeUnlocked = false;
            currentSettingsCategory = 1;
            ShowBackupToast("⚠️ 已清除所有本地存储，恢复系统出厂设置。");
            BuildSettingsPage(win);
        }));
        rightPane.Add(devDangerCard);
    }

    // 10 锁屏
    private void RenderLockScreenSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "锁屏");

        AddSectionHeader(rightPane, "息屏与锁屏样式");
        var card = BuildWhiteCard();
        card.Add(BuildWhiteRow("锁屏时钟样式", "主体历 / 公历 双排时钟", () => ShowBackupToast("已使用默认时钟样式")));
        card.Add(BuildWhiteRow("人脸数据录入", "已通过 3D 结构光特征认证", () => ShowBackupToast("面部特征已注册")));
        card.Add(BuildToggleRow("抬起唤醒", "拿起平板时自动点亮屏幕", true, null));
        card.Add(BuildToggleRow("双击亮屏与熄屏", "在锁屏界面空白处双击熄屏", true, null));
        rightPane.Add(card);
    }

    // 12 桌面：包含复古画风
    private void RenderHomeScreenSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "桌面");

        AddSectionHeader(rightPane, "布局与渲染模式");
        var card = BuildWhiteCard();
        bool retroOn = PlayerPrefs.GetInt("RetroLowResBG", 0) == 1;
        card.Add(BuildToggleRow("复古画风（低颗粒背景）", "开启后降低背景分辨率，模拟复古像素质感", retroOn, v =>
        {
            PlayerPrefs.SetInt("RetroLowResBG", v ? 1 : 0);
            PlayerPrefs.Save();
            ShowBackupToast(v ? "复古画风已开启，下次进入剧情生效" : "已恢复原始高清背景");
        }));
        card.Add(BuildWhiteRow("桌面图标排布", "双页网格 + 底部液态玻璃 Dock 栏", null, false));
        card.Add(BuildWhiteRow("岁月 AI 桌面小部件", "已固定于桌面右下角 · 闲暇交互气泡", () => ShowBackupToast("小部件运行中")));
        rightPane.Add(card);
    }

    // 13 显示与亮度：老设置「显示」8条全量并入
    private void RenderDisplaySettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "显示与亮度");

        AddSectionHeader(rightPane, "画面渲染");
        var card = BuildWhiteCard();

        card.Add(BuildToggleRow("全屏模式", "以全屏方式运行游戏", Screen.fullScreen, v => Screen.fullScreen = v));

        bool vsyncOn = QualitySettings.vSyncCount > 0;
        card.Add(BuildToggleRow("垂直同步", "与自定义帧率互斥，开启后帧率与显示器刷新率同步", vsyncOn, v =>
        {
            QualitySettings.vSyncCount = v ? 1 : 0;
            PlayerPrefs.SetInt("VSync", v ? 1 : 0);
            PlayerPrefs.SetInt("CustomFPS", v ? 0 : 1);
            PlayerPrefs.Save();
            if (v) Application.targetFrameRate = -1;
            else Application.targetFrameRate = PlayerPrefs.GetInt("TargetFPS", 60);
            BuildSettingsPage(win);
        }));

        bool customFpsOn = PlayerPrefs.GetInt("CustomFPS", 1) == 1;
        card.Add(BuildToggleRow("自定义帧率", "关闭垂直同步后手动限制帧率", customFpsOn, v =>
        {
            PlayerPrefs.SetInt("CustomFPS", v ? 1 : 0);
            if (v)
            {
                QualitySettings.vSyncCount = 0;
                PlayerPrefs.SetInt("VSync", 0);
                Application.targetFrameRate = PlayerPrefs.GetInt("TargetFPS", 60);
            }
            else
            {
                QualitySettings.vSyncCount = 1;
                PlayerPrefs.SetInt("VSync", 1);
                Application.targetFrameRate = -1;
            }
            PlayerPrefs.Save();
            BuildSettingsPage(win);
        }));

        card.Add(BuildSliderRow("帧率上限", 30, 240, PlayerPrefs.GetInt("TargetFPS", 60), v =>
        {
            Application.targetFrameRate = v;
            PlayerPrefs.SetInt("TargetFPS", v);
            PlayerPrefs.Save();
        }, " FPS", customFpsOn));

        bool showFpsOn = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
        card.Add(BuildToggleRow("显示帧率", "在角落显示当前 FPS", showFpsOn, v =>
        {
            PlayerPrefs.SetInt("ShowFPS", v ? 1 : 0);
            PlayerPrefs.Save();
            FPSDisplay.SetActive(v);
        }));

        card.Add(BuildSliderRow("文字速度", 1, 10, PlayerPrefs.GetInt("TextSpeed", 5), v =>
        {
            PlayerPrefs.SetInt("TextSpeed", v);
            PlayerPrefs.Save();
        }, " 档"));

        card.Add(BuildSliderRow("对话框透明度", 0, 100, PlayerPrefs.GetInt("DialogAlpha", 80), v =>
        {
            PlayerPrefs.SetInt("DialogAlpha", v);
            PlayerPrefs.Save();
        }, " %"));

        card.Add(BuildWhiteRow("界面缩放方案", "2.5K/4K 屏幕自适应对齐（ScaleMode=2）", null, false));

        var resetBtn = new Button(() =>
        {
            Screen.fullScreen = true;
            QualitySettings.vSyncCount = 0;
            PlayerPrefs.SetInt("VSync", 0);
            PlayerPrefs.SetInt("CustomFPS", 1);
            PlayerPrefs.SetInt("TargetFPS", 60);
            PlayerPrefs.SetInt("ShowFPS", 0);
            PlayerPrefs.SetInt("TextSpeed", 5);
            PlayerPrefs.SetInt("DialogAlpha", 80);
            PlayerPrefs.Save();
            Application.targetFrameRate = 60;
            FPSDisplay.SetActive(false);
            ShowBackupToast("显示设置已恢复默认");
            BuildSettingsPage(win);
        }) { text = "恢复显示默认设置" };
        StyleActionBtn(resetBtn);
        card.Add(resetBtn);

        rightPane.Add(card);
    }

    // 14 声音与振动：老设置「音频」全量并入
    private void RenderAudioSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "声音与振动");

        AddSectionHeader(rightPane, "音量调节");
        var card = BuildWhiteCard();

        card.Add(BuildSliderRow("主音量", 0, 100, Mathf.RoundToInt(GameData.MasterVolume * 100), v =>
        {
            GameData.MasterVolume = v / 100f;
        }, " %"));

        card.Add(BuildSliderRow("背景音乐", 0, 100, Mathf.RoundToInt(GameData.BGMVolume * 100), v =>
        {
            GameData.BGMVolume = v / 100f;
        }, " %"));

        card.Add(BuildSliderRow("音效", 0, 100, Mathf.RoundToInt(GameData.SFXVolume * 100), v =>
        {
            GameData.SFXVolume = v / 100f;
        }, " %"));

        card.Add(BuildSliderRow("打字机音效", 0, 100, Mathf.RoundToInt(GameData.TypewriterVolume * 100), v =>
        {
            GameData.TypewriterVolume = v / 100f;
        }, " %"));

        card.Add(BuildWhiteRow("铁道音效模拟", "已启用（先民柴油机车鸣笛与道岔金属撞击）", null, false));

        var resetBtn = new Button(() =>
        {
            GameData.MasterVolume = 1f;
            GameData.BGMVolume = 0.8f;
            GameData.SFXVolume = 1f;
            GameData.TypewriterVolume = 0.12f;
            ShowBackupToast("音频设置已恢复默认");
            BuildSettingsPage(win);
        }) { text = "恢复默认音量设置" };
        StyleActionBtn(resetBtn);
        card.Add(resetBtn);

        rightPane.Add(card);
    }

    // 15 触控笔与键盘：老设置「操作」按键重绑定全量并入
    private void RenderControlSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "触控笔与键盘");

        AddSectionHeader(rightPane, "按键映射（点击按钮后按下新按键更改，Escape 取消）");
        var card = BuildWhiteCard();

        foreach (KeyBindings.Action action in System.Enum.GetValues(typeof(KeyBindings.Action)))
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 10; row.style.paddingBottom = 10;
            row.style.borderBottomWidth = 1; row.style.borderBottomColor = BorderDivider;

            var lbl = new Label(KeyBindings.GetActionName(action));
            lbl.style.fontSize = 15; lbl.style.color = InkPrimary;
            lbl.style.unityFontDefinition = Fd();
            row.Add(lbl);

            Button keyBtn = null;
            var capturedAction = action;
            keyBtn = new Button(() => StartTabletRebind(capturedAction, keyBtn)) { text = KeyBindings.GetKeyName(action) };
            keyBtn.style.width = 130; keyBtn.style.height = 34; keyBtn.style.fontSize = 14;
            keyBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            keyBtn.style.unityFontDefinition = Fd();
            keyBtn.style.backgroundColor = SurfaceApp;
            keyBtn.style.color = AccentSystem;
            keyBtn.style.borderTopLeftRadius = 6; keyBtn.style.borderTopRightRadius = 6;
            keyBtn.style.borderBottomLeftRadius = 6; keyBtn.style.borderBottomRightRadius = 6;
            keyBtn.style.borderTopWidth = 1; keyBtn.style.borderBottomWidth = 1;
            keyBtn.style.borderLeftWidth = 1; keyBtn.style.borderRightWidth = 1;
            keyBtn.style.borderTopColor = BorderCard; keyBtn.style.borderBottomColor = BorderCard;
            keyBtn.style.borderLeftColor = BorderCard; keyBtn.style.borderRightColor = BorderCard;
            row.Add(keyBtn);

            card.Add(row);
        }

        var resetBtn = new Button(() =>
        {
            foreach (KeyBindings.Action action in System.Enum.GetValues(typeof(KeyBindings.Action)))
                KeyBindings.SetKey(action, KeyBindings.GetKey(action));
            ShowBackupToast("按键映射已恢复默认设置");
            BuildSettingsPage(win);
        }) { text = "恢复默认按键配置" };
        StyleActionBtn(resetBtn);
        card.Add(resetBtn);

        rightPane.Add(card);
    }

    private void StartTabletRebind(KeyBindings.Action action, Button btn)
    {
        btn.text = "按下新键...";
        btn.style.color = new Color(0.9f, 0.45f, 0.1f, 1f);
        System.Action<KeyCode> onKey = (key) =>
        {
            if (key != KeyCode.None && key != KeyCode.Escape)
            {
                KeyBindings.SetKey(action, key);
                btn.text = KeyBindings.GetKeyName(action);
                btn.style.color = AccentSystem;
            }
            else
            {
                btn.text = KeyBindings.GetKeyName(action);
                btn.style.color = AccentSystem;
            }
            _tabletRebindAction = null;
        };
        _tabletRebindAction = onKey;
    }

    // 18 应用：系统应用管控入口
    private void RenderAppsSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "应用设置");

        AddSectionHeader(rightPane, "应用与组件管理");
        var card = BuildWhiteCard();
        card.Add(BuildWhiteRow("进入应用管控", "已禁用的 4 个系统组件与恢复彩蛋", ShowAppManagementPage));
        card.Add(BuildWhiteRow("默认应用设置", "默认浏览器：光明内网浏览器 · 默认播放器：随身听", null, false));
        card.Add(BuildWhiteRow("特殊权限设置", "全部 22 个应用沙箱隔离权限", () => ShowBackupToast("权限运转正常")));
        rightPane.Add(card);
    }

    // 形态 C · 无障碍页（4 胶囊 Tab + 开关行，对照 Accessibility 截图）
    private void RenderAccessibilitySettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "无障碍");

        // 4 个胶囊 Tab
        var tabRow = new VisualElement();
        tabRow.style.flexDirection = FlexDirection.Row;
        tabRow.style.justifyContent = Justify.Center;
        tabRow.style.marginBottom = 16;
        rightPane.Add(tabRow);

        string[] tabs = { "常规", "视觉", "听觉", "肢体" };
        for (int i = 0; i < tabs.Length; i++)
        {
            int idx = i;
            var tabBtn = new Button(() =>
            {
                accessibilityTab = idx;
                BuildSettingsPage(win);
            }) { text = tabs[i] };
            tabBtn.style.height = 34; tabBtn.style.paddingLeft = 20; tabBtn.style.paddingRight = 20;
            tabBtn.style.fontSize = 14; tabBtn.style.unityFontDefinition = Fd();
            tabBtn.style.marginRight = 8;
            tabBtn.style.backgroundColor = (i == accessibilityTab) ? new Color(0.88f, 0.89f, 0.92f, 1f) : SurfaceCard;
            tabBtn.style.color = (i == accessibilityTab) ? InkPrimary : InkSecondary;
            tabBtn.style.borderTopLeftRadius = 17; tabBtn.style.borderTopRightRadius = 17;
            tabBtn.style.borderBottomLeftRadius = 17; tabBtn.style.borderBottomRightRadius = 17;
            tabBtn.style.borderTopWidth = 1; tabBtn.style.borderBottomWidth = 1;
            tabBtn.style.borderLeftWidth = 1; tabBtn.style.borderRightWidth = 1;
            tabBtn.style.borderTopColor = BorderCard; tabBtn.style.borderBottomColor = BorderCard;
            tabBtn.style.borderLeftColor = BorderCard; tabBtn.style.borderRightColor = BorderCard;
            tabRow.Add(tabBtn);
        }

        AddSectionHeader(rightPane, tabs[accessibilityTab] + "辅助服务");
        var card = BuildWhiteCard();
        card.Add(BuildToggleRow("无障碍快捷方式", "同时按住音量加减键 3 秒开启", false, _ => ShowBackupToast("此功能尚未开放")));
        card.Add(BuildToggleRow("高对比度文本", "增强浅色界面的文字辨识度", false, _ => ShowBackupToast("此功能尚未开放")));
        card.Add(BuildWhiteRow("TalkBack 语音助手", "屏幕内容读屏朗读服务（未安装语言包）", () => ShowBackupToast("此功能尚未开放")));
        rightPane.Add(card);
    }

    // 23 更多设置：老设置「游戏」6条全量 + 备份 + 术语图鉴
    private void RenderAdditionalSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "更多设置");

        AddSectionHeader(rightPane, "游戏与推进行为（老设置游戏项）");
        var gameCard = BuildWhiteCard();
        gameCard.Add(BuildToggleRow("自动播放模式", "开场后自动推进对话", PlayerPrefs.GetInt("AutoPlay", 0) == 1, v =>
        {
            PlayerPrefs.SetInt("AutoPlay", v ? 1 : 0);
            PlayerPrefs.Save();
        }));
        gameCard.Add(BuildToggleRow("跳过已读文本", "已读过的对话自动跳过", PlayerPrefs.GetInt("SkipRead", 1) == 1, v =>
        {
            PlayerPrefs.SetInt("SkipRead", v ? 1 : 0);
            PlayerPrefs.Save();
        }));
        gameCard.Add(BuildToggleRow("点击对话框推进", "点击对话框区域触发下一句", PlayerPrefs.GetInt("ClickAdvance", 1) == 1, v =>
        {
            PlayerPrefs.SetInt("ClickAdvance", v ? 1 : 0);
            PlayerPrefs.Save();
        }));
        gameCard.Add(BuildToggleRow("确认对话框", "关闭游戏或退出章节时显示确认", PlayerPrefs.GetInt("ConfirmDialog", 1) == 1, v =>
        {
            PlayerPrefs.SetInt("ConfirmDialog", v ? 1 : 0);
            PlayerPrefs.Save();
        }));
        gameCard.Add(BuildSliderRow("自动播放间隔", 1, 10, PlayerPrefs.GetInt("AutoInterval", 3), v =>
        {
            PlayerPrefs.SetInt("AutoInterval", v);
            PlayerPrefs.Save();
        }, " 秒"));
        rightPane.Add(gameCard);

        AddSectionHeader(rightPane, "数据管理与知识图鉴");
        var dataCard = BuildWhiteCard();
        dataCard.Add(BuildWhiteRow("本地数据备份与恢复", "快照经营数据与剧目存档（参考小米备份）", ShowBackupRestorePage));
        dataCard.Add(BuildWhiteRow("铁路百科与专有名词图鉴", "30 条完整术语词条 · 角色口吻释义", () => OpenTipsPage(null)));
        rightPane.Add(dataCard);
    }

    // 形态 B · 普通详情页 / 规范占位页
    private void RenderPlaceholderCategory(ScrollView rightPane, int catId)
    {
        string catName = "设置项";
        foreach (var c in SettingsCategories) if (c.id == catId) { catName = c.name; break; }

        AddCenterHeader(rightPane, catName);
        AddSectionHeader(rightPane, catName + " 选项");

        var card = BuildWhiteCard();
        card.Add(BuildWhiteRow(catName + " 基本配置", "此功能尚未开放", () => ShowBackupToast("此功能尚未开放")));
        card.Add(BuildWhiteRow("高级选项与网络策略", "此功能尚未开放", () => ShowBackupToast("此功能尚未开放")));
        card.Add(BuildWhiteRow("权限与策略说明", "此功能尚未开放", () => ShowBackupToast("此功能尚未开放")));
        rightPane.Add(card);
    }

    // 辅助控件工具函数
    private void AddCenterHeader(VisualElement parent, string title)
    {
        var h = new Label(title);
        h.style.fontSize = 22; h.style.color = InkPrimary;
        h.style.unityFontStyleAndWeight = FontStyle.Bold; h.style.unityFontDefinition = Fd();
        h.style.alignSelf = Align.Center; h.style.marginBottom = 16;
        parent.Add(h);
    }

    private void AddSectionHeader(VisualElement parent, string title)
    {
        var h = new Label(title);
        h.style.fontSize = 13; h.style.color = SectionHeaderColor;
        h.style.unityFontStyleAndWeight = FontStyle.Bold; h.style.unityFontDefinition = Fd();
        h.style.marginBottom = 8; h.style.marginLeft = 6;
        parent.Add(h);
    }

    private VisualElement BuildWhiteCard()
    {
        var card = new VisualElement();
        card.style.backgroundColor = SurfaceCard;
        card.style.borderTopLeftRadius = 14; card.style.borderTopRightRadius = 14;
        card.style.borderBottomLeftRadius = 14; card.style.borderBottomRightRadius = 14;
        card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
        card.style.borderTopColor = BorderCard; card.style.borderBottomColor = BorderCard;
        card.style.borderLeftColor = BorderCard; card.style.borderRightColor = BorderCard;
        card.style.paddingLeft = 16; card.style.paddingRight = 16;
        card.style.paddingTop = 8; card.style.paddingBottom = 8;
        card.style.marginBottom = 14;
        return card;
    }

    private VisualElement BuildWhiteRow(string title, string desc = null, System.Action onClick = null, bool hasArrow = true)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 10; row.style.paddingBottom = 10;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = BorderDivider;
        row.pickingMode = onClick != null ? PickingMode.Position : PickingMode.Ignore;

        if (onClick != null)
        {
            row.RegisterCallback<PointerEnterEvent>(_ => row.style.backgroundColor = SurfaceHover);
            row.RegisterCallback<PointerLeaveEvent>(_ => row.style.backgroundColor = Color.clear);
            row.RegisterCallback<ClickEvent>(_ => onClick());
        }

        var left = new VisualElement();
        left.style.flexGrow = 1;
        row.Add(left);

        var t = new Label(title);
        t.style.fontSize = 15; t.style.color = InkPrimary;
        t.style.unityFontDefinition = Fd();
        left.Add(t);

        if (!string.IsNullOrEmpty(desc))
        {
            var d = new Label(desc);
            d.style.fontSize = 12; d.style.color = InkSecondary;
            d.style.unityFontDefinition = Fd(); d.style.marginTop = 2;
            left.Add(d);
        }

        if (hasArrow)
        {
            var ar = new Label("›");
            ar.style.fontSize = 18; ar.style.color = InkTertiary; ar.style.unityFontDefinition = Fd();
            row.Add(ar);
        }
        return row;
    }

    private VisualElement BuildToggleRow(string title, string desc, bool initialVal, System.Action<bool> onChanged)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 10; row.style.paddingBottom = 10;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = BorderDivider;

        var left = new VisualElement();
        left.style.flexGrow = 1;
        row.Add(left);

        var t = new Label(title);
        t.style.fontSize = 15; t.style.color = InkPrimary;
        t.style.unityFontDefinition = Fd();
        left.Add(t);

        if (!string.IsNullOrEmpty(desc))
        {
            var d = new Label(desc);
            d.style.fontSize = 12; d.style.color = InkSecondary;
            d.style.unityFontDefinition = Fd(); d.style.marginTop = 2;
            left.Add(d);
        }

        // 胶囊开关
        bool val = initialVal;
        var toggle = new VisualElement();
        toggle.style.width = 46; toggle.style.height = 26;
        toggle.style.borderTopLeftRadius = 13; toggle.style.borderTopRightRadius = 13;
        toggle.style.borderBottomLeftRadius = 13; toggle.style.borderBottomRightRadius = 13;
        toggle.style.backgroundColor = val ? AccentSystem : new Color(0.85f, 0.85f, 0.87f, 1f);
        toggle.style.justifyContent = Justify.Center;
        toggle.pickingMode = PickingMode.Position;
        row.Add(toggle);

        var thumb = new VisualElement();
        thumb.style.width = 20; thumb.style.height = 20;
        thumb.style.borderTopLeftRadius = 10; thumb.style.borderTopRightRadius = 10;
        thumb.style.borderBottomLeftRadius = 10; thumb.style.borderBottomRightRadius = 10;
        thumb.style.backgroundColor = Color.white;
        thumb.style.marginLeft = val ? 23 : 3;
        toggle.Add(thumb);

        toggle.RegisterCallback<ClickEvent>(_ =>
        {
            val = !val;
            toggle.style.backgroundColor = val ? AccentSystem : new Color(0.85f, 0.85f, 0.87f, 1f);
            thumb.style.marginLeft = val ? 23 : 3;
            if (onChanged != null) onChanged(val);
        });

        return row;
    }

    private VisualElement BuildSliderRow(string title, int min, int max, int current, System.Action<int> onChanged, string unit = "", bool enabled = true)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 10; row.style.paddingBottom = 10;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = BorderDivider;

        var t = new Label(title);
        t.style.fontSize = 15; t.style.color = enabled ? InkPrimary : InkTertiary;
        t.style.unityFontDefinition = Fd();
        row.Add(t);

        var rightCol = new VisualElement();
        rightCol.style.flexDirection = FlexDirection.Row;
        rightCol.style.alignItems = Align.Center;
        row.Add(rightCol);

        var valLbl = new Label(current + unit);
        valLbl.style.fontSize = 13; valLbl.style.color = enabled ? AccentSystem : InkTertiary;
        valLbl.style.unityFontDefinition = Fd(); valLbl.style.width = 54;
        valLbl.style.unityTextAlign = TextAnchor.MiddleRight;
        rightCol.Add(valLbl);

        var slider = new SliderInt(min, max) { value = current };
        slider.style.width = 160; slider.style.marginLeft = 10;
        slider.SetEnabled(enabled);
        slider.RegisterValueChangedCallback(evt =>
        {
            valLbl.text = evt.newValue + unit;
            if (onChanged != null) onChanged(evt.newValue);
        });
        rightCol.Add(slider);

        return row;
    }

    private void StyleActionBtn(Button btn)
    {
        btn.style.height = 36; btn.style.marginTop = 14;
        btn.style.backgroundColor = SurfaceApp;
        btn.style.color = AccentSystem;
        btn.style.fontSize = 14; btn.style.unityFontDefinition = Fd();
        btn.style.borderTopLeftRadius = 8; btn.style.borderTopRightRadius = 8;
        btn.style.borderBottomLeftRadius = 8; btn.style.borderBottomRightRadius = 8;
        btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
        btn.style.borderTopColor = BorderCard; btn.style.borderBottomColor = BorderCard;
        btn.style.borderLeftColor = BorderCard; btn.style.borderRightColor = BorderCard;
        btn.style.alignSelf = Align.Center;
        btn.style.paddingLeft = 20; btn.style.paddingRight = 20;
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

        var cloudRow = BuildWhiteRow("云备份", "阿里郎内网账户（未开放）", null, false);
        win.Add(cloudRow);

        // 版本信息
        var verLabel = new Label("澎湃OS 32 · DPRK Edition");
        verLabel.style.fontSize = 12;
        verLabel.style.color = GlassTextMuted;
        verLabel.style.unityFontDefinition = Fd();
        verLabel.style.marginTop = 20;
        win.Add(verLabel);
    }

    // ============ 术语图鉴（TIPS）============
    [System.Serializable] private class TipsJsonData { public TipsTerm[] terms; }
    [System.Serializable] private class TipsTerm { public string id; public string name; public string py; public TipsExplanation[] explanations; }
    [System.Serializable] private class TipsExplanation { public string speaker; public string text; }

    // 说话者 → Resources/characters 立绘前缀（与 CharacterSpriteManager.SpeakerAlias 同源；无贴图的角色仅显示名字 chip）
    private static readonly System.Collections.Generic.Dictionary<string, string> TipsPortraitPrefix =
        new System.Collections.Generic.Dictionary<string, string>
        {
            { "男大学生", "lin_biaohan" },
            { "林彪悍", "lin_biaohan" },
            { "老陈", "laochen" },
            { "岁月", "suiyue" },
            { "嘉颖徐", "jiaying_xu" },
            { "张工", "zhanggong" },
        };

    /// <summary>术语图鉴入口（静态）：确保桌面/App层就绪、跳过锁屏直达图鉴。
    /// termId 非空 → 右侧详情定位该词条；null → 浏览模式（提示从左侧选择）。</summary>
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
        Instance.ShowTipsPage(termId);
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

    private static Texture2D LoadTipsPortrait(string speaker)
    {
        if (string.IsNullOrEmpty(speaker)) return null;
        if (!TipsPortraitPrefix.TryGetValue(speaker, out var prefix)) return null;
        return Resources.Load<Texture2D>("characters/" + prefix);
    }

    /// <summary>Terms.json 全量词条（带 py），按拼音首字母、再按名称排序。</summary>
    private static System.Collections.Generic.List<TipsTerm> LoadTipsTerms()
    {
        var list = new System.Collections.Generic.List<TipsTerm>();
        var asset = Resources.Load<TextAsset>("Terms");
        if (asset == null) return list;
        try
        {
            var data = JsonUtility.FromJson<TipsJsonData>(asset.text);
            if (data != null && data.terms != null) list.AddRange(data.terms);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Tips] Terms.json parse failed: " + ex.Message);
        }
        list.Sort((a, b) =>
        {
            string pa = string.IsNullOrEmpty(a.py) ? "~" : a.py;
            string pb = string.IsNullOrEmpty(b.py) ? "~" : b.py;
            int c = string.CompareOrdinal(pa, pb);
            return c != 0 ? c : string.CompareOrdinal(a.name, b.name);
        });
        return list;
    }

    /// <summary>左索引右详情（Maitetsu TIPS 式，平板玻璃风）。</summary>
    private void ShowTipsPage(string termId)
    {
        var win = BuildInnerPanel("术语图鉴");
        win.style.width = 900;
        win.style.height = 560;

        var terms = LoadTipsTerms();

        var body = new VisualElement();
        body.style.flexDirection = FlexDirection.Row;
        body.style.flexGrow = 1;
        body.style.marginTop = 12;
        win.Add(body);

        // 左：按拼音首字母分组的可滚动索引
        var indexScroll = new ScrollView();
        indexScroll.style.width = 230;
        indexScroll.style.flexShrink = 0;
        indexScroll.style.marginRight = 16;
        indexScroll.style.backgroundColor = GlassBgSoft;
        indexScroll.style.borderTopLeftRadius = 10; indexScroll.style.borderTopRightRadius = 10;
        indexScroll.style.borderBottomLeftRadius = 10; indexScroll.style.borderBottomRightRadius = 10;
        indexScroll.style.paddingLeft = 8; indexScroll.style.paddingRight = 8;
        indexScroll.style.paddingTop = 8; indexScroll.style.paddingBottom = 8;
        body.Add(indexScroll);

        // 右：详情（可滚动）
        var detail = new ScrollView();
        detail.style.flexGrow = 1;
        detail.style.paddingLeft = 14; detail.style.paddingRight = 10;
        body.Add(detail);

        var placeholder = new Label("← 点击左侧词条，查看角色口吻详解");
        placeholder.style.fontSize = 14;
        placeholder.style.color = GlassTextMuted;
        placeholder.style.unityFontDefinition = Fd();
        placeholder.style.marginTop = 24;
        detail.Add(placeholder);

        Label selectedRow = null;
        TipsTerm initial = null;
        string curPy = null;
        foreach (var t in terms)
        {
            string py = string.IsNullOrEmpty(t.py) ? "#" : t.py;
            if (py != curPy)
            {
                curPy = py;
                var head = new Label(py);
                head.style.fontSize = 13;
                head.style.color = GlassAccent;
                head.style.unityFontDefinition = Fd();
                head.style.marginTop = 10;
                head.style.marginBottom = 2;
                indexScroll.Add(head);
            }

            var row = new Label(t.name);
            row.style.fontSize = 15;
            row.style.color = GlassText;
            row.style.unityFontDefinition = Fd();
            row.style.paddingLeft = 10; row.style.paddingRight = 10;
            row.style.paddingTop = 6; row.style.paddingBottom = 6;
            row.style.borderTopLeftRadius = 6; row.style.borderTopRightRadius = 6;
            row.style.borderBottomLeftRadius = 6; row.style.borderBottomRightRadius = 6;
            var captured = t;
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (selectedRow != null) selectedRow.style.backgroundColor = StyleKeyword.Null;
                selectedRow = row;
                row.style.backgroundColor = GlassBgHover;
                detail.Clear();
                RenderTipsDetail(detail, captured);
            });
            indexScroll.Add(row);
            if (termId != null && t.id == termId)
            {
                initial = t;
                selectedRow = row;
                row.style.backgroundColor = GlassBgHover;
            }
        }

        if (initial != null)
        {
            RenderTipsDetail(detail, initial);
            var scrollTo = selectedRow;
            indexScroll.schedule.Execute(() => indexScroll.ScrollTo(scrollTo));
        }
    }

    /// <summary>右侧详情：词条名标题条 + 每段（立绘小图+角色 chip + 释义全文，文中其它术语包金）。</summary>
    private void RenderTipsDetail(ScrollView detail, TipsTerm t)
    {
        var titleBar = new VisualElement();
        titleBar.style.flexDirection = FlexDirection.Row;
        titleBar.style.alignItems = Align.Center;
        titleBar.style.backgroundColor = new Color(0.16f, 0.10f, 0.06f, 0.9f);
        titleBar.style.borderTopLeftRadius = 6; titleBar.style.borderTopRightRadius = 6;
        titleBar.style.borderBottomLeftRadius = 6; titleBar.style.borderBottomRightRadius = 6;
        titleBar.style.paddingLeft = 12; titleBar.style.paddingRight = 12;
        titleBar.style.paddingTop = 6; titleBar.style.paddingBottom = 6;
        var title = new Label(t.name);
        title.style.fontSize = 20;
        title.style.color = new Color(1f, 0.86f, 0.59f, 1f);
        title.style.unityFontDefinition = Fd();
        titleBar.Add(title);
        if (!string.IsNullOrEmpty(t.py))
        {
            var pyTag = new Label(t.py);
            pyTag.style.fontSize = 12;
            pyTag.style.color = new Color(1f, 0.86f, 0.59f, 0.7f);
            pyTag.style.unityFontDefinition = Fd();
            pyTag.style.marginLeft = 10;
            titleBar.Add(pyTag);
        }
        detail.Add(titleBar);

        if (t.explanations == null) return;
        foreach (var ex in t.explanations)
        {
            if (ex == null || string.IsNullOrEmpty(ex.text)) continue;
            if (!string.IsNullOrEmpty(ex.speaker))
            {
                var chipRow = new VisualElement();
                chipRow.style.flexDirection = FlexDirection.Row;
                chipRow.style.alignItems = Align.Center;
                chipRow.style.marginTop = 12;

                var tex = LoadTipsPortrait(ex.speaker);
                if (tex != null)
                {
                    // 立绘小图：按原始宽高比给宽，避免拉伸变形
                    var img = new VisualElement();
                    int h = 44;
                    int w = Mathf.Clamp(Mathf.RoundToInt(h * tex.width / (float)tex.height), 24, 72);
                    img.style.width = w; img.style.height = h;
                    img.style.backgroundImage = new StyleBackground(tex);
                    img.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);
                    img.style.borderTopLeftRadius = 6; img.style.borderTopRightRadius = 6;
                    img.style.borderBottomLeftRadius = 6; img.style.borderBottomRightRadius = 6;
                    img.style.borderTopWidth = 1; img.style.borderBottomWidth = 1;
                    img.style.borderLeftWidth = 1; img.style.borderRightWidth = 1;
                    img.style.borderTopColor = GlassBorderSoft; img.style.borderBottomColor = GlassBorderSoft;
                    img.style.borderLeftColor = GlassBorderSoft; img.style.borderRightColor = GlassBorderSoft;
                    img.style.marginRight = 8;
                    chipRow.Add(img);
                }

                var chip = new Label(ex.speaker);
                chip.style.fontSize = 13;
                chip.style.color = new Color(1f, 0.86f, 0.59f, 1f);
                chip.style.backgroundColor = new Color(0.16f, 0.10f, 0.06f, 0.9f);
                chip.style.paddingLeft = 8; chip.style.paddingRight = 8;
                chip.style.paddingTop = 3; chip.style.paddingBottom = 3;
                chip.style.borderTopLeftRadius = 4; chip.style.borderTopRightRadius = 4;
                chip.style.borderBottomLeftRadius = 4; chip.style.borderBottomRightRadius = 4;
                chip.style.unityFontDefinition = Fd();
                chipRow.Add(chip);
                detail.Add(chipRow);
            }

            var bodyText = new Label(TermHighlightSystem.HighlightTerms(ex.text));
            bodyText.style.fontSize = 14;
            bodyText.style.color = new Color(1f, 1f, 1f, 0.92f);
            bodyText.style.unityFontDefinition = Fd();
            bodyText.style.whiteSpace = WhiteSpace.Normal;
            bodyText.style.marginTop = 5;
            bodyText.style.marginBottom = 4;
            detail.Add(bodyText);
        }
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
            ShowAppToast($"⚠ {app.PopupMessage}\n→ 跳转至『{app.JumpTarget}』");
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
        // 全屏双栏调度工作台
        var columns = new VisualElement();
        columns.style.flexDirection = FlexDirection.Row;
        columns.style.flexGrow = 1;
        win.Add(columns);

        // 左栏：42% 宽度，线路与列车状态、资金与客流、操作快捷键
        var leftCol = new VisualElement();
        leftCol.style.width = new Length(42, LengthUnit.Percent);
        leftCol.style.flexDirection = FlexDirection.Column;
        leftCol.style.marginRight = 16;
        columns.Add(leftCol);

        // 线路状态大卡片
        var lineCard = new VisualElement();
        lineCard.style.backgroundColor = GlassBgSoft;
        lineCard.style.borderTopLeftRadius = 16; lineCard.style.borderTopRightRadius = 16;
        lineCard.style.borderBottomLeftRadius = 16; lineCard.style.borderBottomRightRadius = 16;
        lineCard.style.borderTopWidth = 1; lineCard.style.borderBottomWidth = 1;
        lineCard.style.borderLeftWidth = 1; lineCard.style.borderRightWidth = 1;
        lineCard.style.borderTopColor = GlassBorderSoft; lineCard.style.borderBottomColor = GlassBorderSoft;
        lineCard.style.borderLeftColor = GlassBorderSoft; lineCard.style.borderRightColor = GlassBorderSoft;
        lineCard.style.paddingLeft = 18; lineCard.style.paddingRight = 18;
        lineCard.style.paddingTop = 14; lineCard.style.paddingBottom = 14;
        lineCard.style.marginBottom = 14;
        leftCol.Add(lineCard);

        var lineTitle = new Label("当前支线 · 雾峰村 - 矿区线");
        lineTitle.style.fontSize = 17; lineTitle.style.color = GlassAccent; lineTitle.style.unityFontDefinition = Fd();
        lineCard.Add(lineTitle);

        var lineDesc = new Label("全长 23.0 km · 4站设防 · 主要运能：茶叶 / 原煤 / 村民通勤");
        lineDesc.style.fontSize = 12; lineDesc.style.color = GlassTextDim; lineDesc.style.unityFontDefinition = Fd();
        lineDesc.style.marginTop = 4;
        lineCard.Add(lineDesc);

        var trainInfo = new Label("机车：0721 号「沙子飞猪」先民内燃机车 · 2节客厢");
        trainInfo.style.fontSize = 13; trainInfo.style.color = GlassText; trainInfo.style.unityFontDefinition = Fd();
        trainInfo.style.marginTop = 10;
        lineCard.Add(trainInfo);

        // 车况进度条
        var condRow = new VisualElement();
        condRow.style.flexDirection = FlexDirection.Row;
        condRow.style.justifyContent = Justify.SpaceBetween;
        condRow.style.marginTop = 8;
        lineCard.Add(condRow);

        var condText = new Label($"车况健康度：{GameData.TrainCondition}%");
        condText.style.fontSize = 12; condText.style.color = GlassText; condText.style.unityFontDefinition = Fd();
        condRow.Add(condText);

        var condStatus = new Label(GameData.TrainCondition >= 70 ? "状态良好" : (GameData.TrainCondition >= 40 ? "注意损耗" : "亟待大修"));
        condStatus.style.fontSize = 12;
        condStatus.style.color = GameData.TrainCondition >= 70 ? new Color(0.4f, 0.85f, 0.4f, 0.9f) : new Color(0.9f, 0.6f, 0.2f, 0.9f);
        condStatus.style.unityFontDefinition = Fd();
        condRow.Add(condStatus);

        var condBar = new VisualElement();
        condBar.style.height = 6; condBar.style.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
        condBar.style.borderTopLeftRadius = 3; condBar.style.borderTopRightRadius = 3;
        condBar.style.borderBottomLeftRadius = 3; condBar.style.borderBottomRightRadius = 3;
        condBar.style.marginTop = 4;
        lineCard.Add(condBar);

        var condFill = new VisualElement();
        condFill.style.width = new Length(Mathf.Clamp(GameData.TrainCondition, 0, 100), LengthUnit.Percent);
        condFill.style.height = 6;
        condFill.style.backgroundColor = GameData.TrainCondition >= 70 ? new Color(0.4f, 0.85f, 0.4f, 0.9f) : new Color(0.9f, 0.6f, 0.2f, 0.9f);
        condFill.style.borderTopLeftRadius = 3; condFill.style.borderTopRightRadius = 3;
        condFill.style.borderBottomLeftRadius = 3; condFill.style.borderBottomRightRadius = 3;
        condBar.Add(condFill);

        // 核心数值卡片
        var statsCard = new VisualElement();
        statsCard.style.backgroundColor = GlassBgSoft;
        statsCard.style.borderTopLeftRadius = 16; statsCard.style.borderTopRightRadius = 16;
        statsCard.style.borderBottomLeftRadius = 16; statsCard.style.borderBottomRightRadius = 16;
        statsCard.style.borderTopWidth = 1; statsCard.style.borderBottomWidth = 1;
        statsCard.style.borderLeftWidth = 1; statsCard.style.borderRightWidth = 1;
        statsCard.style.borderTopColor = GlassBorderSoft; statsCard.style.borderBottomColor = GlassBorderSoft;
        statsCard.style.borderLeftColor = GlassBorderSoft; statsCard.style.borderRightColor = GlassBorderSoft;
        statsCard.style.paddingLeft = 18; statsCard.style.paddingRight = 18;
        statsCard.style.paddingTop = 14; statsCard.style.paddingBottom = 14;
        statsCard.style.marginBottom = 14;
        leftCol.Add(statsCard);

        var stat1 = new Label($"运营资金：{GameData.Money:N0} 沙币");
        stat1.style.fontSize = 15; stat1.style.color = GlassAccent; stat1.style.unityFontDefinition = Fd();
        statsCard.Add(stat1);

        var stat2 = new Label($"民众信任度：{GameData.Trust} / 100（基准平稳）");
        stat2.style.fontSize = 14; stat2.style.color = GlassText; stat2.style.unityFontDefinition = Fd();
        stat2.style.marginTop = 6;
        statsCard.Add(stat2);

        var stat3 = new Label($"预期客流量：{GameData.ExpectedPassengers} 人次 / 日");
        stat3.style.fontSize = 14; stat3.style.color = GlassText; stat3.style.unityFontDefinition = Fd();
        stat3.style.marginTop = 6;
        statsCard.Add(stat3);

        // 快捷操作区
        var actCard = new VisualElement();
        actCard.style.flexGrow = 1;
        actCard.style.justifyContent = Justify.FlexEnd;
        leftCol.Add(actCard);

        var btnStory = new Button(() => { appLayer.style.display = DisplayStyle.None; ShowStoryPage(); }) { text = "进入剧情 · Main Story" };
        btnStory.style.height = 46;
        btnStory.style.fontSize = 16;
        btnStory.style.unityFontDefinition = Fd();
        btnStory.style.backgroundColor = new Color(0.18f, 0.35f, 0.58f, 0.75f);
        btnStory.style.color = Color.white;
        btnStory.style.borderTopLeftRadius = 12; btnStory.style.borderTopRightRadius = 12;
        btnStory.style.borderBottomLeftRadius = 12; btnStory.style.borderBottomRightRadius = 12;
        btnStory.style.marginBottom = 10;
        actCard.Add(btnStory);

        var btnAffairs = new Button(() => { appLayer.style.display = DisplayStyle.None; ShowAffairsPage(); }) { text = "站务大厅 · 事务管理" };
        btnAffairs.style.height = 46;
        btnAffairs.style.fontSize = 16;
        btnAffairs.style.unityFontDefinition = Fd();
        btnAffairs.style.backgroundColor = new Color(0.24f, 0.25f, 0.32f, 0.65f);
        btnAffairs.style.color = GlassText;
        btnAffairs.style.borderTopLeftRadius = 12; btnAffairs.style.borderTopRightRadius = 12;
        btnAffairs.style.borderBottomLeftRadius = 12; btnAffairs.style.borderBottomRightRadius = 12;
        actCard.Add(btnAffairs);

        // 右栏：58% 宽度，运行方案选择 + 待办与事件
        var rightCol = new VisualElement();
        rightCol.style.width = new Length(58, LengthUnit.Percent);
        rightCol.style.flexDirection = FlexDirection.Column;
        columns.Add(rightCol);

        // 方案选择卡片
        var planBox = new VisualElement();
        planBox.style.backgroundColor = GlassBgSoft;
        planBox.style.borderTopLeftRadius = 16; planBox.style.borderTopRightRadius = 16;
        planBox.style.borderBottomLeftRadius = 16; planBox.style.borderBottomRightRadius = 16;
        planBox.style.borderTopWidth = 1; planBox.style.borderBottomWidth = 1;
        planBox.style.borderLeftWidth = 1; planBox.style.borderRightWidth = 1;
        planBox.style.borderTopColor = GlassBorderSoft; planBox.style.borderBottomColor = GlassBorderSoft;
        planBox.style.borderLeftColor = GlassBorderSoft; planBox.style.borderRightColor = GlassBorderSoft;
        planBox.style.paddingLeft = 18; planBox.style.paddingRight = 18;
        planBox.style.paddingTop = 14; planBox.style.paddingBottom = 14;
        planBox.style.marginBottom = 14;
        rightCol.Add(planBox);

        var planTitle = new Label("今日运行方案制定");
        planTitle.style.fontSize = 16; planTitle.style.color = GlassAccent; planTitle.style.unityFontDefinition = Fd();
        planBox.Add(planTitle);

        for (int i = 0; i < GameData.DispatchPlans.Length; i++)
        {
            var plan = GameData.DispatchPlans[i];
            bool isSelected = (GameData.SelectedDispatchPlanIndex == i);

            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.justifyContent = Justify.SpaceBetween;
            item.style.alignItems = Align.Center;
            item.style.backgroundColor = isSelected ? new Color(0.2f, 0.35f, 0.5f, 0.45f) : new Color(0.15f, 0.15f, 0.2f, 0.35f);
            item.style.borderTopLeftRadius = 10; item.style.borderTopRightRadius = 10;
            item.style.borderBottomLeftRadius = 10; item.style.borderBottomRightRadius = 10;
            item.style.borderTopWidth = 1; item.style.borderBottomWidth = 1;
            item.style.borderLeftWidth = 1; item.style.borderRightWidth = 1;
            item.style.borderTopColor = isSelected ? GlassAccent : Color.clear;
            item.style.borderBottomColor = isSelected ? GlassAccent : Color.clear;
            item.style.borderLeftColor = isSelected ? GlassAccent : Color.clear;
            item.style.borderRightColor = isSelected ? GlassAccent : Color.clear;
            item.style.paddingLeft = 12; item.style.paddingRight = 12;
            item.style.paddingTop = 10; item.style.paddingBottom = 10;
            item.style.marginTop = 8;
            item.pickingMode = PickingMode.Position;
            planBox.Add(item);

            var leftPart = new VisualElement();
            item.Add(leftPart);

            var pName = new Label((isSelected ? "● " : "○ ") + plan.Name);
            pName.style.fontSize = 15; pName.style.color = isSelected ? GlassAccent : GlassText;
            pName.style.unityFontDefinition = Fd();
            leftPart.Add(pName);

            var pDesc = new Label(plan.Summary);
            pDesc.style.fontSize = 12; pDesc.style.color = GlassTextDim; pDesc.style.unityFontDefinition = Fd();
            pDesc.style.marginTop = 2;
            leftPart.Add(pDesc);

            var rightPart = new Label($"资金 {(plan.MoneyDelta >= 0 ? "+" : "")}{plan.MoneyDelta}   车况 {(plan.TrainConditionDelta >= 0 ? "+" : "")}{plan.TrainConditionDelta}");
            rightPart.style.fontSize = 12; rightPart.style.color = GlassTextDim; rightPart.style.unityFontDefinition = Fd();
            item.Add(rightPart);

            item.RegisterCallback<ClickEvent>(_ =>
            {
                ShowBackupToast($"当前方案：{plan.Name}");
            });
        }

        // 简报与待办清单
        var eventBox = new VisualElement();
        eventBox.style.flexGrow = 1;
        eventBox.style.backgroundColor = GlassBgSoft;
        eventBox.style.borderTopLeftRadius = 16; eventBox.style.borderTopRightRadius = 16;
        eventBox.style.borderBottomLeftRadius = 16; eventBox.style.borderBottomRightRadius = 16;
        eventBox.style.borderTopWidth = 1; eventBox.style.borderBottomWidth = 1;
        eventBox.style.borderLeftWidth = 1; eventBox.style.borderRightWidth = 1;
        eventBox.style.borderTopColor = GlassBorderSoft; eventBox.style.borderBottomColor = GlassBorderSoft;
        eventBox.style.borderLeftColor = GlassBorderSoft; eventBox.style.borderRightColor = GlassBorderSoft;
        eventBox.style.paddingLeft = 18; eventBox.style.paddingRight = 18;
        eventBox.style.paddingTop = 14; eventBox.style.paddingBottom = 14;
        rightCol.Add(eventBox);

        var eventTitle = new Label("值班简报与待办清单");
        eventTitle.style.fontSize = 16; eventTitle.style.color = GlassAccent; eventTitle.style.unityFontDefinition = Fd();
        eventBox.Add(eventTitle);

        var todos = GameData.Todos;
        if (todos != null && todos.Count > 0)
        {
            foreach (var td in todos)
            {
                var row = new Label("▫ " + td);
                row.style.fontSize = 13; row.style.color = GlassText; row.style.unityFontDefinition = Fd();
                row.style.marginTop = 6;
                eventBox.Add(row);
            }
        }
        else
        {
            string[] defaultNotices = {
                "▫ 早间道尺巡查完成：雾峰村道口几何尺寸在允差内",
                "▫ 0721号水箱水位正常，已加注沙能过滤液",
                "▫ 松桥站临时道口通行许可已向村委会备案"
            };
            foreach (var dn in defaultNotices)
            {
                var row = new Label(dn);
                row.style.fontSize = 13; row.style.color = GlassText; row.style.unityFontDefinition = Fd();
                row.style.marginTop = 6;
                eventBox.Add(row);
            }
        }

        var footer = new Label("阿里郎内网 RDA-v4.2 认证通过 · 嘉颖徐铁路基金技术支持");
        footer.style.fontSize = 11; footer.style.color = GlassTextMuted; footer.style.unityFontDefinition = Fd();
        footer.style.marginTop = 12;
        eventBox.Add(footer);
    }

    // ============ App: 归途（蔚蓝档案两级架构 · 本作深棕像素调色）============
    private int returnJourneyCurrentChapter = 0;
    private bool returnJourneyInDetail = false;

    private static readonly (string id, string chapterNum, string title, string subTitle, string desc, string coverBg)[] StoryChapters = {
        ("prologue", "序章", "归乡", "雾峰村线重开纪事 · 破晓到明天", "带着金日成综合大学毕业行囊回到沉寂四年的雾峰村，拾起万能道尺，重组先民班组，唤醒0721号动力机车。", "bg/station"),
        ("chapter1", "第一章", "求生", "沙能风暴逼近 · 荒野生机", "沙能载具步步紧逼，在严酷的沙币结算体系下，维持雾峰村线基本运力与村民信任。", "bg/platform_evening"),
        ("chapter2", "第二章", "扩张", "联通乌金岭 · 铁轨向前", "勘探荒废的重载矿区支线，克服设施老化与政治压力，打通区域经济大动脉。", "bg/railway_track"),
        ("chapter3", "第三章", "对抗", "沙能铁龙暗面 · 基金博弈", "沙能巨头以保护之名行垄断之实，嘉颖徐铁路基金携手地方守护铁道尊严。", "bg/hangar"),
        ("chapter4", "终章", "复兴", "汽笛破长空 · 岁月如歌", "全线鸣笛通车，先民铁道科技与沙本位经济终迎曙光，重塑归途传奇。", "bg/songqiao_station")
    };

    private static readonly (string scriptId, string episode, string title, string desc)[] PrologueEpisodes = {
        ("prologue_01_news", "01", "破晓之行", "金日成综合大学荣誉毕业，带着行囊回到雾峰村。"),
        ("prologue_02_day0", "02", "归乡旧站", "老旧的站房与铁栅售票口，四年前停运的日历仍未翻动。"),
        ("prologue_03_journey", "03", "荒草长轨", "铁轨长满杂草，锈迹斑斑的双孔石拱桥跨过溪水。"),
        ("prologue_04_arrival", "04", "雾峰暮色", "老陈守在月台，茶园梯田笼罩在金黄色的晚霞里。"),
        ("prologue_05_inspection", "05", "铁轨与道尺 ★样板", "拿起爷爷留下的万能道尺，摔尺、正尺，排查轨距绝不妥协。"),
        ("prologue_06_team", "06", "故人相见", "聚集村中的老机修工张工与调度员，重组先民班组。"),
        ("prologue_07_first_repair", "07", "首试扳手", "机库深处的0721号机车，拧紧轴承的第一声金属撞击。"),
        ("prologue_08_first_run", "08", "汽笛重响", "沙能点火，雾峰村线上空响起了沉寂四年的第一声鸣笛。"),
        ("prologue_09_funding", "09", "筹措前路", "嘉颖徐铁路基金的援助与来自乌金岭矿区的猜忌。"),
        ("prologue_10_transition", "10", "走向明天", "支线正式通车，准备迎接新的沙本位经济挑战。")
    };

    private bool IsStoryChapterUnlocked(int chIdx)
    {
        if (PlayerPrefs.GetInt("DebugUnlockAllChapters", 0) == 1) return true;
        // 主线剧情当前仅开放序章，后续章节打问号锁定
        return chIdx == 0;
    }

    private bool IsStoryChapterCompleted(int chIdx)
    {
        if (chIdx == 0)
        {
            return PlayerPrefs.GetInt("ArchiveScene_prologue_10_transition", 0) == 1
                || PlayerPrefs.GetInt("ArchiveStory_prologue_10_transition", 0) == 1
                || PlayerPrefs.GetInt("VN_Got0721", 0) == 1;
        }
        return false;
    }

    private bool IsStoryEpisodeUnlocked(int epIdx)
    {
        if (PlayerPrefs.GetInt("DebugUnlockAllChapters", 0) == 1) return true;
        if (epIdx == 0) return true; // 第 1 幕默认解锁
        string prevScript = PrologueEpisodes[epIdx - 1].scriptId;
        return PlayerPrefs.GetInt("ArchiveScene_" + prevScript, 0) == 1
            || PlayerPrefs.GetInt("ArchiveStory_" + prevScript, 0) == 1
            || PlayerPrefs.GetInt("VN_Got0721", 0) == 1;
    }

    private bool IsStoryEpisodeCompleted(int epIdx)
    {
        string curScript = PrologueEpisodes[epIdx].scriptId;
        return PlayerPrefs.GetInt("ArchiveScene_" + curScript, 0) == 1
            || PlayerPrefs.GetInt("ArchiveStory_" + curScript, 0) == 1
            || PlayerPrefs.GetInt("VN_Got0721", 0) == 1;
    }

    private void BuildReturnJourneyApp(VisualElement win)
    {
        win.Clear();

        if (!returnJourneyInDetail)
        {
            // ===== 一级视图：主线剧情 · 铁路线时间轴（上下错落 + 斜切卡片 + 仅序章开放）=====
            var rootCol = new VisualElement();
            rootCol.style.flexGrow = 1;
            rootCol.style.backgroundColor = SurfaceApp;
            rootCol.style.paddingLeft = 20; rootCol.style.paddingRight = 20;
            rootCol.style.paddingTop = 14; rootCol.style.paddingBottom = 16;
            win.Add(rootCol);

            // 顶栏：标题、名言与运营状态
            var topHeader = new VisualElement();
            topHeader.style.flexDirection = FlexDirection.Row;
            topHeader.style.justifyContent = Justify.SpaceBetween;
            topHeader.style.alignItems = Align.Center;
            topHeader.style.marginBottom = 10;
            rootCol.Add(topHeader);

            var titleLeft = new VisualElement();
            titleLeft.style.flexDirection = FlexDirection.Row;
            titleLeft.style.alignItems = Align.Center;
            topHeader.Add(titleLeft);

            var mainTitle = new Label("主线剧情 · Main Story");
            mainTitle.style.fontSize = 22;
            mainTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            mainTitle.style.color = InkPrimary;
            mainTitle.style.unityFontDefinition = Fd();
            titleLeft.Add(mainTitle);

            var act2Btn = new Button(() => ShowBackupToast("第二部主线正在规划中，敬请期待！")) { text = "切换至第二部 ❯" };
            act2Btn.style.height = 28;
            act2Btn.style.fontSize = 11; act2Btn.style.unityFontDefinition = Fd();
            act2Btn.style.backgroundColor = SurfaceCard;
            act2Btn.style.color = InkSecondary;
            act2Btn.style.borderTopLeftRadius = 8; act2Btn.style.borderBottomRightRadius = 8;
            act2Btn.style.borderTopWidth = 1; act2Btn.style.borderBottomWidth = 1;
            act2Btn.style.borderLeftWidth = 1; act2Btn.style.borderRightWidth = 1;
            act2Btn.style.borderTopColor = BorderDivider; act2Btn.style.borderBottomColor = BorderDivider;
            act2Btn.style.borderLeftColor = BorderDivider; act2Btn.style.borderRightColor = BorderDivider;
            act2Btn.style.marginLeft = 16;
            titleLeft.Add(act2Btn);

            var quoteLbl = new Label("『铁路最动人的不是出发，是回来。每一趟归途，都是一次胜利。』");
            quoteLbl.style.fontSize = 12; quoteLbl.style.color = InkTertiary; quoteLbl.style.unityFontDefinition = Fd();
            topHeader.Add(quoteLbl);

            // 主体分栏：左侧水平滚动时间轴 + 右侧固定章节明细面板
            var mainRow = new VisualElement();
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.flexGrow = 1;
            rootCol.Add(mainRow);

            // 左侧：横向滚动时间轴
            var timelineScrollView = new ScrollView(ScrollViewMode.Horizontal);
            timelineScrollView.style.flexGrow = 1;
            timelineScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            timelineScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            timelineScrollView.style.marginRight = 16;
            mainRow.Add(timelineScrollView);

            // 时间轴容器（宽 1480px，高 350px）
            var timelineContainer = new VisualElement();
            timelineContainer.style.width = 1480;
            timelineContainer.style.height = 350;
            timelineContainer.style.position = Position.Relative;
            timelineScrollView.Add(timelineContainer);

            // 铁路线中心轴（双轨 + 枕木工业手绘）
            var trackAxis = new VisualElement();
            trackAxis.style.position = Position.Absolute;
            trackAxis.style.left = 20; trackAxis.style.right = 20; trackAxis.style.top = 170;
            trackAxis.style.height = 8;
            trackAxis.style.backgroundColor = new Color(0.24f, 0.22f, 0.20f, 0.85f);
            trackAxis.style.borderTopLeftRadius = 4; trackAxis.style.borderBottomRightRadius = 4;
            timelineContainer.Add(trackAxis);

            // 轨面高亮双线（琥珀金色铁轨）
            var railTop = new VisualElement();
            railTop.style.position = Position.Absolute;
            railTop.style.left = 0; railTop.style.right = 0; railTop.style.top = 0; railTop.style.height = 2;
            railTop.style.backgroundColor = AccentSystem;
            trackAxis.Add(railTop);

            var railBottom = new VisualElement();
            railBottom.style.position = Position.Absolute;
            railBottom.style.left = 0; railBottom.style.right = 0; railBottom.style.bottom = 0; railBottom.style.height = 2;
            railBottom.style.backgroundColor = AccentSystem;
            trackAxis.Add(railBottom);

            // 枕木线条
            for (int s = 30; s < 1440; s += 36)
            {
                var sleeper = new VisualElement();
                sleeper.style.position = Position.Absolute;
                sleeper.style.left = s; sleeper.style.top = 166;
                sleeper.style.width = 3; sleeper.style.height = 16;
                sleeper.style.backgroundColor = new Color(0.38f, 0.32f, 0.26f, 0.7f);
                timelineContainer.Add(sleeper);
            }

            // 章节卡片上下交错排列（偶数在轴上，奇数在轴下，斜切平行四边形）
            for (int i = 0; i < StoryChapters.Length; i++)
            {
                int chIdx = i;
                var ch = StoryChapters[i];
                bool unlocked = IsStoryChapterUnlocked(i);
                bool completed = IsStoryChapterCompleted(i);
                bool isSelected = (returnJourneyCurrentChapter == chIdx);

                float cardLeft = 40 + i * 280;
                bool isTop = (i % 2 == 0); // 偶数在上，奇数在下

                // 连接线与站标圆钮
                var connLine = new VisualElement();
                connLine.style.position = Position.Absolute;
                connLine.style.left = cardLeft + 120;
                connLine.style.width = 2;
                connLine.style.backgroundColor = isSelected ? AccentSystem : (unlocked ? new Color(0.6f, 0.5f, 0.4f, 0.8f) : new Color(0.7f, 0.7f, 0.7f, 0.5f));
                if (isTop)
                {
                    connLine.style.top = 150; connLine.style.height = 22;
                }
                else
                {
                    connLine.style.top = 176; connLine.style.height = 22;
                }
                timelineContainer.Add(connLine);

                // 站标圆纽
                var stationNode = new VisualElement();
                stationNode.style.position = Position.Absolute;
                stationNode.style.left = cardLeft + 114; stationNode.style.top = 167;
                stationNode.style.width = 14; stationNode.style.height = 14;
                stationNode.style.borderTopLeftRadius = 7; stationNode.style.borderTopRightRadius = 7;
                stationNode.style.borderBottomLeftRadius = 7; stationNode.style.borderBottomRightRadius = 7;
                stationNode.style.backgroundColor = isSelected ? AccentSystem : (unlocked ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f));
                stationNode.style.borderTopWidth = 2; stationNode.style.borderBottomWidth = 2;
                stationNode.style.borderLeftWidth = 2; stationNode.style.borderRightWidth = 2;
                stationNode.style.borderTopColor = isSelected ? CrimsonSeal : AccentSystem;
                stationNode.style.borderBottomColor = isSelected ? CrimsonSeal : AccentSystem;
                stationNode.style.borderLeftColor = isSelected ? CrimsonSeal : AccentSystem;
                stationNode.style.borderRightColor = isSelected ? CrimsonSeal : AccentSystem;
                timelineContainer.Add(stationNode);

                // 斜切卡片本体
                var card = new VisualElement();
                card.style.position = Position.Absolute;
                card.style.left = cardLeft;
                card.style.top = isTop ? 14 : 196;
                card.style.width = 250; card.style.height = 138;
                card.style.borderTopLeftRadius = 24; card.style.borderBottomRightRadius = 24;
                card.style.borderTopWidth = isSelected ? 2.5f : 1.5f;
                card.style.borderBottomWidth = isSelected ? 2.5f : 1.5f;
                card.style.borderLeftWidth = isSelected ? 2.5f : 1.5f;
                card.style.borderRightWidth = isSelected ? 2.5f : 1.5f;
                card.style.borderTopColor = isSelected ? AccentSystem : (unlocked ? BorderDivider : new Color(0.85f, 0.85f, 0.88f, 1f));
                card.style.borderBottomColor = isSelected ? AccentSystem : (unlocked ? BorderDivider : new Color(0.85f, 0.85f, 0.88f, 1f));
                card.style.borderLeftColor = isSelected ? AccentSystem : (unlocked ? BorderDivider : new Color(0.85f, 0.85f, 0.88f, 1f));
                card.style.borderRightColor = isSelected ? AccentSystem : (unlocked ? BorderDivider : new Color(0.85f, 0.85f, 0.88f, 1f));
                card.style.backgroundColor = unlocked ? (isSelected ? new Color(1f, 0.98f, 0.94f, 1f) : SurfaceCard) : new Color(0.93f, 0.93f, 0.95f, 0.95f);
                card.style.overflow = Overflow.Hidden;
                card.style.justifyContent = Justify.SpaceBetween;
                card.pickingMode = PickingMode.Position;
                timelineContainer.Add(card);

                // 卡片右上斜切徽章
                var badge = new VisualElement();
                badge.style.position = Position.Absolute;
                badge.style.top = 8; badge.style.right = 8;
                badge.style.paddingLeft = 8; badge.style.paddingRight = 8;
                badge.style.paddingTop = 3; badge.style.paddingBottom = 3;
                badge.style.borderTopLeftRadius = 8; badge.style.borderBottomRightRadius = 8;
                badge.style.backgroundColor = completed ? CrimsonSeal : (unlocked ? AccentSystem : new Color(0.6f, 0.6f, 0.65f, 1f));
                card.Add(badge);

                var badgeTxt = new Label(completed ? "Clear ★" : (unlocked ? "探索中" : "待开放 🔒"));
                badgeTxt.style.fontSize = 10; badgeTxt.style.color = Color.white; badgeTxt.style.unityFontDefinition = Fd();
                badge.Add(badgeTxt);

                // 卡片正文
                var cardBody = new VisualElement();
                cardBody.style.paddingLeft = 14; cardBody.style.paddingRight = 14; cardBody.style.paddingTop = 10;
                card.Add(cardBody);

                var chNumLbl = new Label(unlocked ? ch.chapterNum : (ch.chapterNum + " · ???"));
                chNumLbl.style.fontSize = 12; chNumLbl.style.color = unlocked ? AccentSystem : InkTertiary;
                chNumLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                chNumLbl.style.unityFontDefinition = Fd();
                cardBody.Add(chNumLbl);

                var chDescLbl = new Label(unlocked ? ch.desc : "后续章节正在规划中，完成前序章节方可开放。");
                chDescLbl.style.fontSize = 11; chDescLbl.style.color = unlocked ? InkSecondary : InkTertiary;
                chDescLbl.style.whiteSpace = WhiteSpace.Normal;
                chDescLbl.style.marginTop = 4; chDescLbl.style.height = 48;
                cardBody.Add(chDescLbl);

                // 卡底实色斜切标题条（纯白实底 + 深色大字）
                var bottomBar = new VisualElement();
                bottomBar.style.height = 36;
                bottomBar.style.backgroundColor = unlocked ? (isSelected ? AccentSystem : new Color(0.96f, 0.96f, 0.98f, 1f)) : new Color(0.88f, 0.88f, 0.90f, 1f);
                bottomBar.style.borderTopWidth = 1; bottomBar.style.borderTopColor = BorderDivider;
                bottomBar.style.justifyContent = Justify.Center;
                bottomBar.style.alignItems = Align.Center;
                card.Add(bottomBar);

                var titleLbl = new Label(unlocked ? (ch.chapterNum + " · " + ch.title) : "???");
                titleLbl.style.fontSize = 13;
                titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLbl.style.color = unlocked ? (isSelected ? Color.white : InkPrimary) : InkTertiary;
                titleLbl.style.unityFontDefinition = Fd();
                bottomBar.Add(titleLbl);

                // 交互点击
                if (unlocked)
                {
                    card.RegisterCallback<ClickEvent>(_ =>
                    {
                        returnJourneyCurrentChapter = chIdx;
                        BuildReturnJourneyApp(win);
                    });
                }
                else
                {
                    card.RegisterCallback<ClickEvent>(_ => ShowBackupToast("后续章节尚未开放，请先完成前置序章！"));
                }
            }

            // 右侧：固定章节明细面板（270px 宽，斜切风格）
            var sidePanel = new VisualElement();
            sidePanel.style.width = 270;
            sidePanel.style.backgroundColor = SurfaceCard;
            sidePanel.style.borderTopLeftRadius = 18; sidePanel.style.borderBottomRightRadius = 18;
            sidePanel.style.borderTopWidth = 1; sidePanel.style.borderBottomWidth = 1;
            sidePanel.style.borderLeftWidth = 1; sidePanel.style.borderRightWidth = 1;
            sidePanel.style.borderTopColor = BorderDivider; sidePanel.style.borderBottomColor = BorderDivider;
            sidePanel.style.borderLeftColor = BorderDivider; sidePanel.style.borderRightColor = BorderDivider;
            sidePanel.style.paddingLeft = 16; sidePanel.style.paddingRight = 16;
            sidePanel.style.paddingTop = 16; sidePanel.style.paddingBottom = 16;
            sidePanel.style.flexDirection = FlexDirection.Column;
            sidePanel.style.justifyContent = Justify.SpaceBetween;
            mainRow.Add(sidePanel);

            var panelTopBox = new VisualElement();
            sidePanel.Add(panelTopBox);

            var curChData = StoryChapters[returnJourneyCurrentChapter];
            bool curUnlocked = IsStoryChapterUnlocked(returnJourneyCurrentChapter);

            var panelHead = new Label(curUnlocked ? ($"▼ {curChData.chapterNum} · {curChData.title}") : ($"▼ {curChData.chapterNum} · ??? 🔒"));
            panelHead.style.fontSize = 16; panelHead.style.color = AccentSystem;
            panelHead.style.unityFontStyleAndWeight = FontStyle.Bold;
            panelHead.style.unityFontDefinition = Fd();
            panelTopBox.Add(panelHead);

            var panelSub = new Label(curUnlocked ? curChData.subTitle : "前置剧目尚未全部完成");
            panelSub.style.fontSize = 12; panelSub.style.color = InkSecondary; panelSub.style.unityFontDefinition = Fd();
            panelSub.style.marginTop = 4; panelSub.style.marginBottom = 12;
            panelTopBox.Add(panelSub);

            // 剧目预览列表
            if (curUnlocked && returnJourneyCurrentChapter == 0)
            {
                var actScroll = new ScrollView();
                actScroll.style.height = 170;
                actScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                panelTopBox.Add(actScroll);

                for (int p = 0; p < PrologueEpisodes.Length; p++)
                {
                    var pep = PrologueEpisodes[p];
                    bool pUnlocked = IsStoryEpisodeUnlocked(p);
                    bool pDone = IsStoryEpisodeCompleted(p);

                    var pRow = new VisualElement();
                    pRow.style.flexDirection = FlexDirection.Row;
                    pRow.style.justifyContent = Justify.SpaceBetween;
                    pRow.style.alignItems = Align.Center;
                    pRow.style.paddingLeft = 8; pRow.style.paddingRight = 8;
                    pRow.style.paddingTop = 4; pRow.style.paddingBottom = 4;
                    pRow.style.marginBottom = 3;
                    pRow.style.backgroundColor = pUnlocked ? new Color(0.96f, 0.96f, 0.98f, 1f) : new Color(0.92f, 0.92f, 0.94f, 0.6f);
                    pRow.style.borderTopLeftRadius = 6; pRow.style.borderBottomRightRadius = 6;
                    actScroll.Add(pRow);

                    var pTitle = new Label($"{pep.episode}. {pep.title}");
                    pTitle.style.fontSize = 11;
                    pTitle.style.color = pUnlocked ? InkPrimary : InkTertiary;
                    pTitle.style.unityFontDefinition = Fd();
                    pRow.Add(pTitle);

                    var pTag = new Label(pDone ? "★" : (pUnlocked ? "▶" : "🔒"));
                    pTag.style.fontSize = 10;
                    pTag.style.color = pDone ? CrimsonSeal : (pUnlocked ? AccentSystem : InkTertiary);
                    pTag.style.unityFontDefinition = Fd();
                    pRow.Add(pTag);
                }
            }
            else
            {
                var lockBox = new VisualElement();
                lockBox.style.height = 170;
                lockBox.style.justifyContent = Justify.Center;
                lockBox.style.alignItems = Align.Center;
                panelTopBox.Add(lockBox);

                var lockIc = new Label("🔒");
                lockIc.style.fontSize = 28; lockIc.style.unityFontDefinition = Fd();
                lockBox.Add(lockIc);

                var lockTxt = new Label("本章节尚未开放探索");
                lockTxt.style.fontSize = 12; lockTxt.style.color = InkTertiary; lockTxt.style.unityFontDefinition = Fd();
                lockTxt.style.marginTop = 6;
                lockBox.Add(lockTxt);
            }

            // 右侧面板底端：斜切进入按钮
            var enterChapterBtn = new Button(() =>
            {
                if (curUnlocked)
                {
                    returnJourneyInDetail = true;
                    BuildReturnJourneyApp(win);
                }
                else
                {
                    ShowBackupToast("本章节尚未开放！");
                }
            }) { text = curUnlocked ? "前往幕次列表 · Enter ❯" : "尚未开放 🔒" };
            enterChapterBtn.style.height = 40;
            enterChapterBtn.style.fontSize = 13; enterChapterBtn.style.unityFontDefinition = Fd();
            enterChapterBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            enterChapterBtn.style.backgroundColor = curUnlocked ? AccentSystem : new Color(0.8f, 0.8f, 0.85f, 1f);
            enterChapterBtn.style.color = Color.white;
            enterChapterBtn.style.borderTopLeftRadius = 12; enterChapterBtn.style.borderBottomRightRadius = 12;
            enterChapterBtn.style.borderTopWidth = 0; enterChapterBtn.style.borderBottomWidth = 0;
            enterChapterBtn.style.borderLeftWidth = 0; enterChapterBtn.style.borderRightWidth = 0;
            sidePanel.Add(enterChapterBtn);

            // 底部栏：通关进度条 + 最终幕特别篇按钮
            var bottomActionRow = new VisualElement();
            bottomActionRow.style.flexDirection = FlexDirection.Row;
            bottomActionRow.style.justifyContent = Justify.SpaceBetween;
            bottomActionRow.style.alignItems = Align.Center;
            bottomActionRow.style.marginTop = 12;
            rootCol.Add(bottomActionRow);

            int doneEps = 0;
            for (int k = 0; k < PrologueEpisodes.Length; k++) if (IsStoryEpisodeCompleted(k)) doneEps++;
            var progBox = new VisualElement();
            progBox.style.flexDirection = FlexDirection.Row;
            progBox.style.alignItems = Align.Center;
            bottomActionRow.Add(progBox);

            var progLbl = new Label($"全线通关进度：{doneEps} / {PrologueEpisodes.Length} 幕");
            progLbl.style.fontSize = 12; progLbl.style.color = InkSecondary; progLbl.style.unityFontDefinition = Fd();
            progBox.Add(progLbl);

            var progBarBg = new VisualElement();
            progBarBg.style.width = 160; progBarBg.style.height = 8;
            progBarBg.style.backgroundColor = BorderDivider;
            progBarBg.style.borderTopLeftRadius = 4; progBarBg.style.borderBottomRightRadius = 4;
            progBarBg.style.marginLeft = 10;
            progBox.Add(progBarBg);

            var progFill = new VisualElement();
            progFill.style.height = 8;
            progFill.style.width = Length.Percent((float)doneEps / PrologueEpisodes.Length * 100f);
            progFill.style.backgroundColor = AccentSystem;
            progFill.style.borderTopLeftRadius = 4; progFill.style.borderBottomRightRadius = 4;
            progBarBg.Add(progFill);

            var finalEpBtn = new Button(() => ShowBackupToast("序章终局通关后开放特别篇。")) { text = "Final Ep. · 终章特别篇" };
            finalEpBtn.style.height = 32;
            finalEpBtn.style.fontSize = 12; finalEpBtn.style.unityFontDefinition = Fd();
            finalEpBtn.style.backgroundColor = SurfaceCard;
            finalEpBtn.style.color = InkSecondary;
            finalEpBtn.style.borderTopLeftRadius = 8; finalEpBtn.style.borderBottomRightRadius = 8;
            finalEpBtn.style.borderTopWidth = 1; finalEpBtn.style.borderBottomWidth = 1;
            finalEpBtn.style.borderLeftWidth = 1; finalEpBtn.style.borderRightWidth = 1;
            finalEpBtn.style.borderTopColor = BorderDivider; finalEpBtn.style.borderBottomColor = BorderDivider;
            finalEpBtn.style.borderLeftColor = BorderDivider; finalEpBtn.style.borderRightColor = BorderDivider;
            bottomActionRow.Add(finalEpBtn);
        }
        else
        {
            // ===== 二级视图：明细页（左固定大插画 + 右侧从下往上显示节 + 自动定位最新解锁项）=====
            var curCh = StoryChapters[returnJourneyCurrentChapter];
            bool isCompleted = IsStoryChapterCompleted(returnJourneyCurrentChapter);

            var rootCol = new VisualElement();
            rootCol.style.flexGrow = 1;
            rootCol.style.backgroundColor = SurfaceApp;
            rootCol.style.paddingLeft = 20; rootCol.style.paddingRight = 20;
            rootCol.style.paddingTop = 14; rootCol.style.paddingBottom = 16;
            win.Add(rootCol);

            // 顶部导航栏
            var topNav = new VisualElement();
            topNav.style.flexDirection = FlexDirection.Row;
            topNav.style.justifyContent = Justify.SpaceBetween;
            topNav.style.alignItems = Align.Center;
            topNav.style.marginBottom = 12;
            rootCol.Add(topNav);

            var backBtn = new Button(() =>
            {
                returnJourneyInDetail = false;
                BuildReturnJourneyApp(win);
            }) { text = "❮ 返回章节总览" };
            backBtn.style.height = 34;
            backBtn.style.fontSize = 13; backBtn.style.unityFontDefinition = Fd();
            backBtn.style.backgroundColor = SurfaceCard;
            backBtn.style.color = InkPrimary;
            backBtn.style.borderTopLeftRadius = 10; backBtn.style.borderBottomRightRadius = 10;
            backBtn.style.borderTopWidth = 1; backBtn.style.borderBottomWidth = 1;
            backBtn.style.borderLeftWidth = 1; backBtn.style.borderRightWidth = 1;
            backBtn.style.borderTopColor = BorderDivider; backBtn.style.borderBottomColor = BorderDivider;
            backBtn.style.borderLeftColor = BorderDivider; backBtn.style.borderRightColor = BorderDivider;
            topNav.Add(backBtn);

            var statusBanner = new VisualElement();
            statusBanner.style.backgroundColor = isCompleted ? CrimsonSeal : AccentSystem;
            statusBanner.style.paddingLeft = 14; statusBanner.style.paddingRight = 14;
            statusBanner.style.paddingTop = 6; statusBanner.style.paddingBottom = 6;
            statusBanner.style.borderTopLeftRadius = 12; statusBanner.style.borderBottomRightRadius = 12;
            topNav.Add(statusBanner);

            var statusBannerTxt = new Label(isCompleted ? "★ 全部剧幕已通关 (All Episodes Cleared)" : "◆ 剧幕正在探索中 (In Progress)");
            statusBannerTxt.style.fontSize = 12; statusBannerTxt.style.color = Color.white; statusBannerTxt.style.unityFontDefinition = Fd();
            statusBanner.Add(statusBannerTxt);

            // 明细主体分栏
            var detailRow = new VisualElement();
            detailRow.style.flexDirection = FlexDirection.Row;
            detailRow.style.flexGrow = 1;
            rootCol.Add(detailRow);

            // 左侧：画报插画面板（320px，斜切 + 印章）
            var leftPoster = new VisualElement();
            leftPoster.style.width = 320;
            leftPoster.style.backgroundColor = SurfaceCard;
            leftPoster.style.borderTopLeftRadius = 20; leftPoster.style.borderBottomRightRadius = 20;
            leftPoster.style.borderTopWidth = 1; leftPoster.style.borderBottomWidth = 1;
            leftPoster.style.borderLeftWidth = 1; leftPoster.style.borderRightWidth = 1;
            leftPoster.style.borderTopColor = BorderDivider; leftPoster.style.borderBottomColor = BorderDivider;
            leftPoster.style.borderLeftColor = BorderDivider; leftPoster.style.borderRightColor = BorderDivider;
            leftPoster.style.marginRight = 16;
            leftPoster.style.overflow = Overflow.Hidden;
            leftPoster.style.position = Position.Relative;
            detailRow.Add(leftPoster);

            var coverSp = Resources.Load<Sprite>(curCh.coverBg);
            if (coverSp != null)
            {
                var coverImg = new Image { sprite = coverSp };
                coverImg.style.position = Position.Absolute;
                coverImg.style.top = 0; coverImg.style.left = 0; coverImg.style.right = 0; coverImg.style.bottom = 0;
                coverImg.scaleMode = ScaleMode.ScaleAndCrop;
                leftPoster.Add(coverImg);

                var posterMask = new VisualElement();
                posterMask.style.position = Position.Absolute;
                posterMask.style.top = 0; posterMask.style.left = 0; posterMask.style.right = 0; posterMask.style.bottom = 0;
                posterMask.style.backgroundColor = new Color(0.10f, 0.08f, 0.06f, 0.55f);
                leftPoster.Add(posterMask);
            }

            var posterInfo = new VisualElement();
            posterInfo.style.position = Position.Absolute;
            posterInfo.style.left = 18; posterInfo.style.right = 18; posterInfo.style.bottom = 20;
            leftPoster.Add(posterInfo);

            var postChNum = new Label(curCh.chapterNum);
            postChNum.style.fontSize = 14; postChNum.style.color = AccentSystem;
            postChNum.style.unityFontStyleAndWeight = FontStyle.Bold;
            postChNum.style.unityFontDefinition = Fd();
            posterInfo.Add(postChNum);

            var postTitle = new Label(curCh.title);
            postTitle.style.fontSize = 24; postTitle.style.color = Color.white; postTitle.style.unityFontDefinition = Fd();
            postTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            posterInfo.Add(postTitle);

            var postSub = new Label(curCh.subTitle);
            postSub.style.fontSize = 12; postSub.style.color = new Color(0.9f, 0.9f, 0.9f, 0.9f); postSub.style.unityFontDefinition = Fd();
            postSub.style.marginTop = 4;
            posterInfo.Add(postSub);

            var postDesc = new Label(curCh.desc);
            postDesc.style.fontSize = 11; postDesc.style.color = new Color(0.85f, 0.85f, 0.85f, 0.85f);
            postDesc.style.whiteSpace = WhiteSpace.Normal;
            postDesc.style.marginTop = 8;
            posterInfo.Add(postDesc);

            // COMPLETE! 圆形公文印章
            var seal = new VisualElement();
            seal.style.position = Position.Absolute;
            seal.style.right = 14; seal.style.bottom = 14;
            seal.style.width = 72; seal.style.height = 72;
            seal.style.borderTopLeftRadius = 36; seal.style.borderTopRightRadius = 36;
            seal.style.borderBottomLeftRadius = 36; seal.style.borderBottomRightRadius = 36;
            seal.style.borderTopWidth = 2; seal.style.borderBottomWidth = 2;
            seal.style.borderLeftWidth = 2; seal.style.borderRightWidth = 2;
            seal.style.borderTopColor = isCompleted ? CrimsonSeal : AccentSystem;
            seal.style.borderBottomColor = isCompleted ? CrimsonSeal : AccentSystem;
            seal.style.borderLeftColor = isCompleted ? CrimsonSeal : AccentSystem;
            seal.style.borderRightColor = isCompleted ? CrimsonSeal : AccentSystem;
            seal.style.backgroundColor = isCompleted ? new Color(0.55f, 0.23f, 0.16f, 0.35f) : new Color(0.72f, 0.52f, 0.23f, 0.25f);
            seal.style.alignItems = Align.Center;
            seal.style.justifyContent = Justify.Center;
            leftPoster.Add(seal);

            var sealStar = new Label(isCompleted ? "★ ★ ★" : "◆ ◆ ◆");
            sealStar.style.fontSize = 10; sealStar.style.color = isCompleted ? CrimsonSeal : AccentSystem; sealStar.style.unityFontDefinition = Fd();
            seal.Add(sealStar);

            var sealTxt = new Label(isCompleted ? "COMPLETE!" : "IN PROGRESS");
            sealTxt.style.fontSize = 9; sealTxt.style.color = isCompleted ? CrimsonSeal : AccentSystem;
            sealTxt.style.unityFontStyleAndWeight = FontStyle.Bold;
            sealTxt.style.unityFontDefinition = Fd();
            seal.Add(sealTxt);

            // 右侧：从下往上编号条目列表（斜切平行四边形 + 默认定位到最新解锁节）
            var epScrollView = new ScrollView();
            epScrollView.style.flexGrow = 1;
            epScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            detailRow.Add(epScrollView);

            // 计算最新解锁的剧目索引（用于默认自动滚动定位）
            int latestUnlockedIdx = 0;
            for (int k = 0; k < PrologueEpisodes.Length; k++)
            {
                if (IsStoryEpisodeUnlocked(k)) latestUnlockedIdx = k;
            }

            VisualElement targetFocusElement = null;

            // 倒序遍历：10 幕倒序生成，使最底部的为第 01 幕，从下往上逐节推进
            for (int i = PrologueEpisodes.Length - 1; i >= 0; i--)
            {
                int epIdx = i;
                var ep = PrologueEpisodes[i];
                bool unlocked = IsStoryEpisodeUnlocked(i);
                bool epDone = IsStoryEpisodeCompleted(i);

                // 外层条目容器（包含条外大号序号 + 斜切条本体）
                var itemRow = new VisualElement();
                itemRow.style.flexDirection = FlexDirection.Row;
                itemRow.style.alignItems = Align.Center;
                itemRow.style.marginBottom = 10;
                epScrollView.Add(itemRow);

                // 条外大号序号（蔚蓝档案原图：左下两位序号 01, 02...）
                var numLbl = new Label(ep.episode);
                numLbl.style.width = 38;
                numLbl.style.fontSize = 22;
                numLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                numLbl.style.color = unlocked ? (epDone ? AccentSystem : InkPrimary) : InkTertiary;
                numLbl.style.unityFontDefinition = Fd();
                numLbl.style.unityTextAlign = TextAnchor.MiddleRight;
                numLbl.style.marginRight = 10;
                itemRow.Add(numLbl);

                // 斜切条本体（平行四边形切角）
                var item = new VisualElement();
                item.style.flexGrow = 1;
                item.style.flexDirection = FlexDirection.Row;
                item.style.justifyContent = Justify.SpaceBetween;
                item.style.alignItems = Align.Center;
                item.style.backgroundColor = unlocked ? SurfaceCard : new Color(0.93f, 0.93f, 0.95f, 0.8f);
                item.style.borderTopLeftRadius = 14; item.style.borderBottomRightRadius = 14;
                item.style.borderTopWidth = 1; item.style.borderBottomWidth = 1;
                item.style.borderLeftWidth = unlocked ? (epDone ? 1 : 2.5f) : 1; item.style.borderRightWidth = 1;
                item.style.borderTopColor = unlocked ? (epDone ? BorderDivider : AccentSystem) : BorderDivider;
                item.style.borderBottomColor = unlocked ? (epDone ? BorderDivider : AccentSystem) : BorderDivider;
                item.style.borderLeftColor = unlocked ? (epDone ? BorderDivider : AccentSystem) : BorderDivider;
                item.style.borderRightColor = unlocked ? (epDone ? BorderDivider : AccentSystem) : BorderDivider;
                item.style.paddingLeft = 14; item.style.paddingRight = 14;
                item.style.paddingTop = 10; item.style.paddingBottom = 10;
                itemRow.Add(item);

                if (i == latestUnlockedIdx)
                {
                    targetFocusElement = itemRow;
                }

                // 左半部分：图标 + 标题 + 描述
                var leftPart = new VisualElement();
                leftPart.style.flexDirection = FlexDirection.Row;
                leftPart.style.alignItems = Align.Center;
                item.Add(leftPart);

                var docIcon = new Label(unlocked ? (epDone ? "📜" : "📐") : "🔒");
                docIcon.style.fontSize = 16; docIcon.style.marginRight = 10;
                docIcon.style.unityFontDefinition = Fd();
                leftPart.Add(docIcon);

                var infoBox = new VisualElement();
                leftPart.Add(infoBox);

                var epTitleLbl = new Label("第 " + ep.episode + " 幕 · " + ep.title);
                epTitleLbl.style.fontSize = 14;
                epTitleLbl.style.color = unlocked ? InkPrimary : InkTertiary;
                epTitleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                epTitleLbl.style.unityFontDefinition = Fd();
                infoBox.Add(epTitleLbl);

                var epDescLbl = new Label(unlocked ? ep.desc : "前置剧目尚未通关，完成前一幕即可解锁。");
                epDescLbl.style.fontSize = 11;
                epDescLbl.style.color = unlocked ? InkSecondary : InkTertiary;
                epDescLbl.style.unityFontDefinition = Fd();
                epDescLbl.style.marginTop = 2;
                infoBox.Add(epDescLbl);

                // 右半部分：状态 + 斜切进入按钮
                var rightPart = new VisualElement();
                rightPart.style.flexDirection = FlexDirection.Row;
                rightPart.style.alignItems = Align.Center;
                item.Add(rightPart);

                var stLbl = new Label(epDone ? "已通关 ★" : (unlocked ? "可开始 ▶" : "未解锁 🔒"));
                stLbl.style.fontSize = 11;
                stLbl.style.color = epDone ? new Color(0.2f, 0.65f, 0.35f, 1f) : (unlocked ? AccentSystem : InkTertiary);
                stLbl.style.unityFontDefinition = Fd();
                stLbl.style.marginRight = 12;
                rightPart.Add(stLbl);

                string btnText = unlocked ? (epDone ? "重温本幕" : "开始本幕") : "未解锁";
                var replayBtn = new Button(() =>
                {
                    if (unlocked)
                    {
                        appLayer.style.display = DisplayStyle.None;
                        MainStoryUI.StartEpisodeStatic(ep.scriptId);
                    }
                    else
                    {
                        ShowBackupToast("本幕尚未解锁，请先完成前置剧目。");
                    }
                }) { text = btnText };
                replayBtn.style.height = 32;
                replayBtn.style.paddingLeft = 14; replayBtn.style.paddingRight = 14;
                replayBtn.style.fontSize = 12; replayBtn.style.unityFontDefinition = Fd();
                replayBtn.style.backgroundColor = unlocked ? (epDone ? new Color(0.92f, 0.92f, 0.95f, 1f) : AccentSystem) : new Color(0.85f, 0.85f, 0.88f, 0.6f);
                replayBtn.style.color = unlocked ? (epDone ? InkPrimary : Color.white) : InkTertiary;
                replayBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                replayBtn.style.borderTopLeftRadius = 8; replayBtn.style.borderBottomRightRadius = 8;
                replayBtn.style.borderTopWidth = 0; replayBtn.style.borderBottomWidth = 0;
                replayBtn.style.borderLeftWidth = 0; replayBtn.style.borderRightWidth = 0;
                replayBtn.SetEnabled(unlocked);
                rightPart.Add(replayBtn);
            }

            // 默认定位到最新解锁的地方（如果仅第 1 幕则定位在最下面）
            epScrollView.schedule.Execute(() =>
            {
                if (targetFocusElement != null)
                {
                    epScrollView.ScrollTo(targetFocusElement);
                }
                else
                {
                    epScrollView.scrollOffset = new Vector2(0, 99999f);
                }
            });
        }
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

    // ============ App: 笔记（三栏可编辑 + 真实数据自动化联动）============
    private int currentNoteCategoryIndex = 0; // 0:全部笔记, 1:剧情书签, 2:运营备忘, 3:乘务日志, 4:机车定检, 5:成就里程碑, 6:待办事项
    private int currentSelectedNoteId = -1; // 选中的笔记 ID
    private TextField noteTitleField;
    private TextField noteBodyField;
    private Label noteMetaLabel;

    [System.Serializable]
    private class TabletNoteItem
    {
        public int id;
        public string category;
        public string title;
        public string date;
        public string author;
        public string content;
        public bool isSystemGenerated;
    }

    [System.Serializable]
    private class TabletNotesContainer
    {
        public System.Collections.Generic.List<TabletNoteItem> notes = new System.Collections.Generic.List<TabletNoteItem>();
    }

    private System.Collections.Generic.List<TabletNoteItem> userTabletNotes = null;

    private void LoadUserTabletNotes()
    {
        userTabletNotes = new System.Collections.Generic.List<TabletNoteItem>();
        string json = PlayerPrefs.GetString("TabletNotes", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var container = JsonUtility.FromJson<TabletNotesContainer>(json);
                if (container != null && container.notes != null)
                {
                    userTabletNotes = container.notes;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[TabletNotes] Load failed: " + ex.Message);
            }
        }

        // 首次加载或没有预置笔记时，植入默认初始化笔记
        if (userTabletNotes.Count == 0)
        {
            userTabletNotes.Add(new TabletNoteItem
            {
                id = 1,
                category = "运营备忘",
                title = "雾峰村线重开财务备忘与调度复盘",
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                author = "林彪悍",
                content = $"今天是我们正式重启雾峰村线的第 {GameData.Day} 天。\n\n" +
                          $"根据调度系统的核算，今天沿线茶农与通勤客流达到了 {GameData.ExpectedPassengers} 人次。\n" +
                          $"当前账面可用沙币为 {GameData.Money:N0}，车况保持在 {GameData.TrainCondition}%。\n\n" +
                          "老陈说很多年没有在月台上听见这么响亮的汽笛声了。虽然沙能集团在周边城市的渗透仍在扩张，但只要我们守住这条铁道，这片土地的温度就不会熄灭。\n\n" +
                          "明日待办：检查松桥站水泵供水管道，核验道尺水平度。",
                isSystemGenerated = false
            });
            userTabletNotes.Add(new TabletNoteItem
            {
                id = 2,
                category = "机车定检",
                title = "0721号机车初次点火定检记录",
                date = "昨日 18:30",
                author = "张工",
                content = "沙能点火器压力维持在正常范围，但轴瓦润滑油有轻微渗漏，建议在第3天前重新注润滑油。\n锅炉安全阀已校验，蒸汽管路密封垫已换新。",
                isSystemGenerated = false
            });
            userTabletNotes.Add(new TabletNoteItem
            {
                id = 3,
                category = "乘务日志",
                title = "松桥道口道尺检定与道钉加固",
                date = "前天 09:12",
                author = "老陈",
                content = "松桥站前后枕木有轻微松动，已打入新道钉8根。道尺测量轨距为1435mm标准轨距，水平误差在允差±2mm以内。",
                isSystemGenerated = false
            });
            SaveUserTabletNotes();
        }

        // 动态数据化联动（自动追加，不重复）：
        // 1. 自动抓取 AchievementManager 解锁列表
        var allAchs = AchievementManager.GetAll();
        if (allAchs != null)
        {
            foreach (var ach in allAchs)
            {
                if (ach.unlocked)
                {
                    string achTitle = $"★ 成就解锁：{ach.title}";
                    if (!userTabletNotes.Exists(n => n.title == achTitle))
                    {
                        userTabletNotes.Add(new TabletNoteItem
                        {
                            id = 1000 + ach.GetHashCode() % 9000,
                            category = "成就里程碑",
                            title = achTitle,
                            date = string.IsNullOrEmpty(ach.unlockedDate) ? System.DateTime.Now.ToString("yyyy-MM-dd") : ach.unlockedDate,
                            author = "系统记录",
                            content = $"【荣誉成就已达成】\n\n成就名称：{ach.title}\n稀有度：{ach.rarity}\n达成描述：{ach.description}\n\n铁道总署特发此状，以资鼓励！",
                            isSystemGenerated = true
                        });
                    }
                }
            }
        }

        // 2. 自动抓取 BookmarkManager 剧情书签
        var autoBms = BookmarkManager.GetAllAuto();
        if (autoBms != null)
        {
            foreach (var bm in autoBms)
            {
                if (bm != null && !string.IsNullOrEmpty(bm.scriptName))
                {
                    string bmTitle = $"🔖 剧情书签：第{bm.episodeNum}话 {bm.episodeTitle}";
                    if (!userTabletNotes.Exists(n => n.title == bmTitle))
                    {
                        userTabletNotes.Add(new TabletNoteItem
                        {
                            id = 2000 + bm.GetHashCode() % 8000,
                            category = "剧情书签",
                            title = bmTitle,
                            date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                            author = "岁月AI同步",
                            content = $"【剧目断点存盘】\n\n剧目：第{bm.episodeNum}话 {bm.episodeTitle}\n脚本：{bm.scriptName} (场景{bm.sceneIndex}, 对话{bm.dialogueIndex})\n摘抄：{bm.previewText}\n\n随时可在归途或VN中重温。",
                            isSystemGenerated = true
                        });
                    }
                }
            }
        }

        // 3. 自动抓取 GameData 当日运营周报摘要
        string weeklyTitle = $"📊 运营第 {GameData.Day} 天运营简报";
        if (!userTabletNotes.Exists(n => n.title == weeklyTitle))
        {
            userTabletNotes.Add(new TabletNoteItem
            {
                id = 3000 + GameData.Day,
                category = "运营备忘",
                title = weeklyTitle,
                date = System.DateTime.Now.ToString("yyyy-MM-dd"),
                author = "调度中心",
                content = $"【雾峰村线每日运营摘要】\n\n" +
                          $"• 运营天数：第 {GameData.Day} 天\n" +
                          $"• 账面可用沙币：{GameData.Money:N0} 沙币\n" +
                          $"• 0721机车车况：{GameData.TrainCondition}%\n" +
                          $"• 沿线居民信任度：{GameData.Trust}%\n" +
                          $"• 今日预计客流：{GameData.ExpectedPassengers} 人次\n" +
                          $"• 票价方案：35 沙币/人\n\n" +
                          $"调度评语：运行平稳，注意在沙能风暴前储备足够滤芯。",
                isSystemGenerated = true
            });
        }
    }

    private void SaveUserTabletNotes()
    {
        if (userTabletNotes == null) return;
        var container = new TabletNotesContainer();
        foreach (var n in userTabletNotes)
        {
            if (!n.isSystemGenerated) // 仅持久化用户自建与编辑的笔记
                container.notes.Add(n);
        }
        string json = JsonUtility.ToJson(container);
        PlayerPrefs.SetString("TabletNotes", json);
        PlayerPrefs.Save();
    }

    private void BuildNotesApp(VisualElement win)
    {
        win.Clear();
        if (userTabletNotes == null) LoadUserTabletNotes();

        // 统一浅色背景应用主体（三栏架构：左导航 200px + 中列表 290px + 右正文 flexGrow）
        var noteCols = new VisualElement();
        noteCols.style.flexDirection = FlexDirection.Row;
        noteCols.style.flexGrow = 1;
        noteCols.style.backgroundColor = SurfaceApp;
        win.Add(noteCols);

        // ===== 左栏：分类树（200px，严格照截图与需求排布）=====
        var catCol = new VisualElement();
        catCol.style.width = 200;
        catCol.style.backgroundColor = SurfaceCard;
        catCol.style.borderRightWidth = 1;
        catCol.style.borderRightColor = BorderDivider;
        catCol.style.paddingLeft = 12; catCol.style.paddingRight = 12;
        catCol.style.paddingTop = 16; catCol.style.paddingBottom = 16;
        catCol.style.flexDirection = FlexDirection.Column;
        catCol.style.justifyContent = Justify.SpaceBetween;
        noteCols.Add(catCol);

        var catList = new ScrollView();
        catList.style.flexGrow = 1;
        catList.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        catList.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        catCol.Add(catList);

        var catHeader = new Label("便签与笔记");
        catHeader.style.fontSize = 18; catHeader.style.color = InkPrimary;
        catHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        catHeader.style.unityFontDefinition = Fd();
        catHeader.style.marginBottom = 14;
        catList.Add(catHeader);

        // 分类定义：0=全部笔记, 1=剧情书签 (严格第2位), 2..6=笔记本分类
        string[] cats = { "全部笔记", "剧情书签", "运营备忘", "乘务日志", "机车定检", "成就里程碑", "待办事项" };
        string[] catIcons = { "📝", "🔖", "📁", "📁", "📁", "📁", "📁" };

        // 顶部核心两项：全部笔记 + 剧情书签
        for (int i = 0; i <= 1; i++)
        {
            int catIdx = i;
            bool isSelected = (catIdx == currentNoteCategoryIndex);
            var catBtn = new Button(() =>
            {
                currentNoteCategoryIndex = catIdx;
                BuildNotesApp(win);
            });
            catBtn.style.height = 38;
            catBtn.style.flexDirection = FlexDirection.Row;
            catBtn.style.alignItems = Align.Center;
            catBtn.style.paddingLeft = 10; catBtn.style.paddingRight = 10;
            catBtn.style.marginBottom = 4;
            catBtn.style.backgroundColor = isSelected ? new Color(0.12f, 0.12f, 0.14f, 1f) : Color.clear;
            catBtn.style.borderTopLeftRadius = 10; catBtn.style.borderTopRightRadius = 10;
            catBtn.style.borderBottomLeftRadius = 10; catBtn.style.borderBottomRightRadius = 10;
            catBtn.style.borderTopWidth = 0; catBtn.style.borderBottomWidth = 0;
            catBtn.style.borderLeftWidth = 0; catBtn.style.borderRightWidth = 0;

            var icLbl = new Label(catIcons[i]);
            icLbl.style.fontSize = 15; icLbl.style.marginRight = 8;
            icLbl.style.unityFontDefinition = Fd();
            catBtn.Add(icLbl);

            var nameLbl = new Label(cats[i]);
            nameLbl.style.fontSize = 13;
            nameLbl.style.color = isSelected ? Color.white : InkPrimary;
            if (isSelected) nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.unityFontDefinition = Fd();
            catBtn.Add(nameLbl);

            catList.Add(catBtn);
        }

        // 分割线与【笔记本分类】专区标题
        var divider = new VisualElement();
        divider.style.height = 1; divider.style.backgroundColor = BorderDivider;
        divider.style.marginTop = 10; divider.style.marginBottom = 12;
        catList.Add(divider);

        var folderHeader = new Label("笔记本分类");
        folderHeader.style.fontSize = 12; folderHeader.style.color = InkTertiary;
        folderHeader.style.unityFontDefinition = Fd();
        folderHeader.style.paddingLeft = 4;
        folderHeader.style.marginBottom = 8;
        catList.Add(folderHeader);

        // 下部文件夹分类区 (索引 2..6)
        for (int i = 2; i < cats.Length; i++)
        {
            int catIdx = i;
            bool isSelected = (catIdx == currentNoteCategoryIndex);
            var catBtn = new Button(() =>
            {
                currentNoteCategoryIndex = catIdx;
                BuildNotesApp(win);
            });
            catBtn.style.height = 36;
            catBtn.style.flexDirection = FlexDirection.Row;
            catBtn.style.alignItems = Align.Center;
            catBtn.style.paddingLeft = 10; catBtn.style.paddingRight = 10;
            catBtn.style.marginBottom = 3;
            catBtn.style.backgroundColor = isSelected ? new Color(0.12f, 0.12f, 0.14f, 1f) : Color.clear;
            catBtn.style.borderTopLeftRadius = 8; catBtn.style.borderTopRightRadius = 8;
            catBtn.style.borderBottomLeftRadius = 8; catBtn.style.borderBottomRightRadius = 8;
            catBtn.style.borderTopWidth = 0; catBtn.style.borderBottomWidth = 0;
            catBtn.style.borderLeftWidth = 0; catBtn.style.borderRightWidth = 0;

            var icLbl = new Label(catIcons[i]);
            icLbl.style.fontSize = 14; icLbl.style.marginRight = 8;
            icLbl.style.unityFontDefinition = Fd();
            catBtn.Add(icLbl);

            var nameLbl = new Label(cats[i]);
            nameLbl.style.fontSize = 13;
            nameLbl.style.color = isSelected ? Color.white : InkPrimary;
            if (isSelected) nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.unityFontDefinition = Fd();
            catBtn.Add(nameLbl);

            catList.Add(catBtn);
        }

        // ===== 中栏：笔记卡列表（290px 纯白卡片流 + 高对比度）=====
        var listColBox = new VisualElement();
        listColBox.style.width = 290;
        listColBox.style.backgroundColor = SurfaceApp;
        listColBox.style.paddingLeft = 14; listColBox.style.paddingRight = 14;
        listColBox.style.paddingTop = 16; listColBox.style.paddingBottom = 16;
        listColBox.style.borderRightWidth = 1;
        listColBox.style.borderRightColor = BorderDivider;
        listColBox.style.flexDirection = FlexDirection.Column;
        noteCols.Add(listColBox);

        // 过滤当前分类的笔记
        string selectedCat = cats[currentNoteCategoryIndex];
        var filteredNotes = userTabletNotes.FindAll(n => currentNoteCategoryIndex == 0 || n.category == selectedCat);

        var listHeaderRow = new VisualElement();
        listHeaderRow.style.flexDirection = FlexDirection.Row;
        listHeaderRow.style.justifyContent = Justify.SpaceBetween;
        listHeaderRow.style.alignItems = Align.Center;
        listHeaderRow.style.marginBottom = 12;
        listColBox.Add(listHeaderRow);

        var listTitleLbl = new Label(selectedCat);
        listTitleLbl.style.fontSize = 17; listTitleLbl.style.color = InkPrimary;
        listTitleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        listTitleLbl.style.unityFontDefinition = Fd();
        listHeaderRow.Add(listTitleLbl);

        var countLbl = new Label($"共 {filteredNotes.Count} 篇");
        countLbl.style.fontSize = 12; countLbl.style.color = InkTertiary;
        countLbl.style.unityFontDefinition = Fd();
        listHeaderRow.Add(countLbl);

        var listCol = new ScrollView();
        listCol.style.flexGrow = 1;
        listCol.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        listCol.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        listColBox.Add(listCol);

        // 若当前未选或选中笔记不在列表中，默认选中第一篇
        if (filteredNotes.Count > 0 && !filteredNotes.Exists(n => n.id == currentSelectedNoteId))
        {
            currentSelectedNoteId = filteredNotes[0].id;
        }

        foreach (var n in filteredNotes)
        {
            bool isSelected = (n.id == currentSelectedNoteId);
            var nCard = new VisualElement();
            nCard.style.backgroundColor = isSelected ? new Color(1f, 0.98f, 0.94f, 1f) : SurfaceCard;
            nCard.style.borderTopLeftRadius = 12; nCard.style.borderTopRightRadius = 12;
            nCard.style.borderBottomLeftRadius = 12; nCard.style.borderBottomRightRadius = 12;
            nCard.style.borderTopWidth = 1; nCard.style.borderBottomWidth = 1;
            nCard.style.borderLeftWidth = isSelected ? 3 : 1; nCard.style.borderRightWidth = 1;
            nCard.style.borderTopColor = isSelected ? AccentSystem : BorderDivider;
            nCard.style.borderBottomColor = isSelected ? AccentSystem : BorderDivider;
            nCard.style.borderLeftColor = isSelected ? AccentSystem : BorderDivider;
            nCard.style.borderRightColor = isSelected ? AccentSystem : BorderDivider;
            nCard.style.paddingLeft = 14; nCard.style.paddingRight = 14;
            nCard.style.paddingTop = 12; nCard.style.paddingBottom = 12;
            nCard.style.marginBottom = 10;
            nCard.pickingMode = PickingMode.Position;
            listCol.Add(nCard);

            var nt = new Label(n.title);
            nt.style.fontSize = 14; nt.style.color = InkPrimary;
            nt.style.unityFontStyleAndWeight = FontStyle.Bold;
            nt.style.unityFontDefinition = Fd();
            nCard.Add(nt);

            var nd = new Label(n.date + " · " + n.author);
            nd.style.fontSize = 11; nd.style.color = InkTertiary; nd.style.unityFontDefinition = Fd();
            nd.style.marginTop = 3;
            nCard.Add(nd);

            var snippet = n.content.Length > 36 ? n.content.Substring(0, 36) + "..." : n.content;
            var ns = new Label(snippet);
            ns.style.fontSize = 12; ns.style.color = InkSecondary; nd.style.unityFontDefinition = Fd();
            ns.style.whiteSpace = WhiteSpace.Normal;
            ns.style.marginTop = 5;
            nCard.Add(ns);

            int noteId = n.id;
            nCard.RegisterCallback<ClickEvent>(_ =>
            {
                currentSelectedNoteId = noteId;
                BuildNotesApp(win);
            });
        }

        // 中栏底端：新建笔记按钮（纯实色高对比度按钮）
        var newNoteBtn = new Button(() =>
        {
            string catToAssign = currentNoteCategoryIndex == 0 ? "运营备忘" : (currentNoteCategoryIndex == 1 ? "剧情书签" : cats[currentNoteCategoryIndex]);
            var newNote = new TabletNoteItem
            {
                id = (int)(System.DateTime.UtcNow.Ticks % 1000000),
                category = catToAssign,
                title = "新备忘录 " + System.DateTime.Now.ToString("MM-dd HH:mm"),
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                author = "林彪悍",
                content = "在此输入新的笔记与运营日志内容...",
                isSystemGenerated = false
            };
            userTabletNotes.Insert(0, newNote);
            currentSelectedNoteId = newNote.id;
            SaveUserTabletNotes();
            BuildNotesApp(win);
            ShowBackupToast("已新建一条备忘录。");
        }) { text = "＋ 新建笔记" };
        newNoteBtn.style.height = 38;
        newNoteBtn.style.fontSize = 13; newNoteBtn.style.unityFontDefinition = Fd();
        newNoteBtn.style.backgroundColor = AccentSystem;
        newNoteBtn.style.color = Color.white;
        newNoteBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        newNoteBtn.style.borderTopLeftRadius = 10; newNoteBtn.style.borderTopRightRadius = 10;
        newNoteBtn.style.borderBottomLeftRadius = 10; newNoteBtn.style.borderBottomRightRadius = 10;
        newNoteBtn.style.borderTopWidth = 0; newNoteBtn.style.borderBottomWidth = 0;
        newNoteBtn.style.borderLeftWidth = 0; newNoteBtn.style.borderRightWidth = 0;
        newNoteBtn.style.marginTop = 10;
        listColBox.Add(newNoteBtn);

        // ===== 右栏：纯白编辑器卡片（大标题 + 元信息 + 正文 + 保存）=====
        var detailColWrapper = new VisualElement();
        detailColWrapper.style.flexGrow = 1;
        detailColWrapper.style.paddingLeft = 20; detailColWrapper.style.paddingRight = 20;
        detailColWrapper.style.paddingTop = 16; detailColWrapper.style.paddingBottom = 16;
        noteCols.Add(detailColWrapper);

        var detailCol = new VisualElement();
        detailCol.style.flexGrow = 1;
        detailCol.style.backgroundColor = SurfaceCard;
        detailCol.style.borderTopLeftRadius = 14; detailCol.style.borderTopRightRadius = 14;
        detailCol.style.borderBottomLeftRadius = 14; detailCol.style.borderBottomRightRadius = 14;
        detailCol.style.borderTopWidth = 1; detailCol.style.borderBottomWidth = 1;
        detailCol.style.borderLeftWidth = 1; detailCol.style.borderRightWidth = 1;
        detailCol.style.borderTopColor = BorderDivider; detailCol.style.borderBottomColor = BorderDivider;
        detailCol.style.borderLeftColor = BorderDivider; detailCol.style.borderRightColor = BorderDivider;
        detailCol.style.paddingLeft = 24; detailCol.style.paddingRight = 24;
        detailCol.style.paddingTop = 20; detailCol.style.paddingBottom = 20;
        detailColWrapper.Add(detailCol);

        var curNote = userTabletNotes.Find(n => n.id == currentSelectedNoteId);
        if (curNote == null && userTabletNotes.Count > 0) curNote = userTabletNotes[0];

        if (curNote != null)
        {
            // 顶栏操作行：大标题 TextField + 保存/删除按钮
            var topActionRow = new VisualElement();
            topActionRow.style.flexDirection = FlexDirection.Row;
            topActionRow.style.alignItems = Align.Center;
            topActionRow.style.justifyContent = Justify.SpaceBetween;
            topActionRow.style.marginBottom = 8;
            detailCol.Add(topActionRow);

            noteTitleField = new TextField { value = curNote.title };
            noteTitleField.style.flexGrow = 1;
            noteTitleField.style.height = 36;
            noteTitleField.style.fontSize = 20;
            noteTitleField.style.unityFontStyleAndWeight = FontStyle.Bold;
            noteTitleField.style.unityFontDefinition = Fd();
            noteTitleField.style.color = InkPrimary;
            noteTitleField.style.backgroundColor = Color.clear;
            noteTitleField.style.borderTopWidth = 0; noteTitleField.style.borderBottomWidth = 0;
            noteTitleField.style.borderLeftWidth = 0; noteTitleField.style.borderRightWidth = 0;
            noteTitleField.style.marginRight = 12;
            topActionRow.Add(noteTitleField);

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            topActionRow.Add(btnRow);

            var saveBtn = new Button(() =>
            {
                curNote.title = noteTitleField.value;
                curNote.content = noteBodyField.value;
                curNote.date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                SaveUserTabletNotes();
                ShowBackupToast("💾 笔记已保存到平板闪存。");
                BuildNotesApp(win);
            }) { text = "💾 保存笔记" };
            saveBtn.style.height = 34;
            saveBtn.style.paddingLeft = 14; saveBtn.style.paddingRight = 14;
            saveBtn.style.fontSize = 12; saveBtn.style.unityFontDefinition = Fd();
            saveBtn.style.backgroundColor = AccentSystem;
            saveBtn.style.color = Color.white;
            saveBtn.style.borderTopLeftRadius = 8; saveBtn.style.borderTopRightRadius = 8;
            saveBtn.style.borderBottomLeftRadius = 8; saveBtn.style.borderBottomRightRadius = 8;
            saveBtn.style.borderTopWidth = 0; saveBtn.style.borderBottomWidth = 0;
            saveBtn.style.borderLeftWidth = 0; saveBtn.style.borderRightWidth = 0;
            btnRow.Add(saveBtn);

            // 删除按钮（仅允许删除用户自建笔记）
            if (!curNote.isSystemGenerated)
            {
                var delBtn = new Button(() =>
                {
                    userTabletNotes.Remove(curNote);
                    SaveUserTabletNotes();
                    currentSelectedNoteId = userTabletNotes.Count > 0 ? userTabletNotes[0].id : 0;
                    ShowBackupToast("🗑️ 笔记已删除。");
                    BuildNotesApp(win);
                }) { text = "🗑 删除" };
                delBtn.style.height = 34;
                delBtn.style.paddingLeft = 10; delBtn.style.paddingRight = 10;
                delBtn.style.fontSize = 12; delBtn.style.unityFontDefinition = Fd();
                delBtn.style.backgroundColor = new Color(0.95f, 0.95f, 0.96f, 1f);
                delBtn.style.color = InkSecondary;
                delBtn.style.borderTopLeftRadius = 8; delBtn.style.borderTopRightRadius = 8;
                delBtn.style.borderBottomLeftRadius = 8; delBtn.style.borderBottomRightRadius = 8;
                delBtn.style.borderTopWidth = 0; delBtn.style.borderBottomWidth = 0;
                delBtn.style.borderLeftWidth = 0; delBtn.style.borderRightWidth = 0;
                delBtn.style.marginLeft = 8;
                btnRow.Add(delBtn);
            }

            noteMetaLabel = new Label($"记录时间：{curNote.date} · 记录人：{curNote.author} · 分类：{curNote.category}");
            noteMetaLabel.style.fontSize = 12; noteMetaLabel.style.color = InkTertiary; noteMetaLabel.style.unityFontDefinition = Fd();
            noteMetaLabel.style.marginBottom = 12;
            detailCol.Add(noteMetaLabel);

            var noteDivider = new VisualElement();
            noteDivider.style.height = 1; noteDivider.style.backgroundColor = BorderDivider;
            noteDivider.style.marginBottom = 14;
            detailCol.Add(noteDivider);

            noteBodyField = new TextField { value = curNote.content, multiline = true };
            noteBodyField.style.flexGrow = 1;
            noteBodyField.style.fontSize = 14;
            noteBodyField.style.unityFontDefinition = Fd();
            noteBodyField.style.color = InkPrimary;
            noteBodyField.style.backgroundColor = Color.clear;
            noteBodyField.style.borderTopWidth = 0; noteBodyField.style.borderBottomWidth = 0;
            noteBodyField.style.borderLeftWidth = 0; noteBodyField.style.borderRightWidth = 0;
            detailCol.Add(noteBodyField);
        }
        else
        {
            var emptyLbl = new Label("暂无笔记内容。点击左侧「＋ 新建笔记」开始记录。");
            emptyLbl.style.fontSize = 15; emptyLbl.style.color = InkTertiary; emptyLbl.style.unityFontDefinition = Fd();
            detailCol.Add(emptyLbl);
        }
    }

    // ============ App: 音乐（13 首全量曲目 + 毛玻璃悬浮播放器 · 完整迁移）============
    private class TabletMusicEntry
    {
        public string id;
        public string title;
        public string category; // "BGM" 或 "歌曲"
        public string clipName;
        public string condition;
    }

    private static readonly TabletMusicEntry[] TabletMusicList = {
        new TabletMusicEntry { id = "iron_and_ash", title = "Iron & Ash 铁与灰", category = "BGM", clipName = "iron_and_ash", condition = "标题画面自动解锁" },
        new TabletMusicEntry { id = "cloud_rail", title = "Cloud-Rail 云轨", category = "BGM", clipName = "cloud_rail", condition = "启程飞行途中解锁" },
        new TabletMusicEntry { id = "embers", title = "Embers 余烬", category = "BGM", clipName = "embers", condition = "抵达雾峰站时解锁" },
        new TabletMusicEntry { id = "night_cargo", title = "Night Cargo 夜行货", category = "BGM", clipName = "night_cargo", condition = "边境夜航时解锁" },
        new TabletMusicEntry { id = "first_light", title = "First Light 晨光", category = "BGM", clipName = "first_light", condition = "飞越华北平原时解锁" },
        new TabletMusicEntry { id = "platform", title = "Platform 站台", category = "BGM", clipName = "platform", condition = "到站后解锁" },
        new TabletMusicEntry { id = "borderline", title = "Borderline 国境线", category = "BGM", clipName = "borderline", condition = "边境危机时解锁" },
        new TabletMusicEntry { id = "wheels_joke", title = "The Wheel's Joke 方向盘在笑", category = "BGM", clipName = "wheels_joke", condition = "统一便当店购物" },
        new TabletMusicEntry { id = "train_through_keys", title = "Train Through Keys 旧曲", category = "BGM", clipName = "train_through_keys", condition = "已废弃，保留试听" },
        new TabletMusicEntry { id = "south_wind", title = "남풍 南风", category = "歌曲", clipName = "south_wind", condition = "统一便当店购物后解锁" },
        new TabletMusicEntry { id = "starlit_rails", title = "별빛 철길 星光铁轨", category = "歌曲", clipName = "starlit_rails", condition = "边境危机时解锁夜航" },
        new TabletMusicEntry { id = "chollima_ride", title = "천리마 신시대에 달리다 千里马驰骋新时代", category = "歌曲", clipName = "chollima_ride", condition = "边境途中播放新闻时解锁" },
        new TabletMusicEntry { id = "sleepers", title = "Sleepers 铁轨沉睡者", category = "歌曲", clipName = "sleepers", condition = "片尾曲（待生成）" }
    };

    private string tabletMusicFilter = "全部"; // "全部", "BGM", "歌曲"
    private int tabletMusicNavTab = 0; // 0: 推荐/乐库, 1: 我的/收藏
    private AudioSource tabletMusicSource;
    private AudioClip tabletCurrentClip;
    private string tabletCurrentTrackId;
    private string tabletCurrentTrackTitle;
    private string tabletCurrentTrackCategory;
    private bool tabletIsPlaying;
    private enum TabletPlayMode { SingleRepeat, Sequential, Shuffle }
    private TabletPlayMode tabletPlayMode = TabletPlayMode.Sequential;
    private VisualElement tabletProgressTrack;
    private VisualElement tabletProgressFill;
    private VisualElement tabletProgressKnob;
    private Label tabletCurTimeLbl;
    private Label tabletTotalTimeLbl;
    private Label tabletPlayerTitleLbl;
    private Label tabletPlayerSubLbl;
    private Button tabletPlayToggleBtn;
    private Button tabletModeToggleBtn;
    private Button tabletFavToggleBtn;
    private bool tabletProgressScrubbing;
    private float tabletMusicProgressTimer;

    private bool IsTabletMusicUnlocked(TabletMusicEntry m)
    {
        if (m.id == "iron_and_ash" || m.id == "cloud_rail" || m.id == "embers" || m.id == "platform") return true;
        return PlayerPrefs.GetInt("ArchiveMusic_" + m.id, 0) == 1 || PlayerPrefs.GetInt("VN_Got0721", 0) == 1;
    }

    private bool IsTabletMusicFavorited(string trackId)
    {
        return PlayerPrefs.GetInt("MusicFav_" + trackId, 0) == 1;
    }

    private void ToggleTabletMusicFavorite(string trackId)
    {
        bool cur = IsTabletMusicFavorited(trackId);
        PlayerPrefs.SetInt("MusicFav_" + trackId, cur ? 0 : 1);
        PlayerPrefs.Save();
    }

    private int GetTabletFavoriteCount()
    {
        int count = 0;
        foreach (var m in TabletMusicList)
        {
            if (IsTabletMusicFavorited(m.id)) count++;
        }
        return count;
    }

    private void PlayTabletTrack(TabletMusicEntry m)
    {
        var clip = Resources.Load<AudioClip>("bgm/" + m.clipName);
        if (clip == null)
        {
            ShowBackupToast("音频资源缺失：bgm/" + m.clipName);
            return;
        }

        if (tabletMusicSource == null)
        {
            var go = new GameObject("TabletMusicPlayer");
            UnityEngine.Object.DontDestroyOnLoad(go);
            tabletMusicSource = go.AddComponent<AudioSource>();
        }

        tabletMusicSource.clip = clip;
        tabletMusicSource.volume = 0.65f;
        tabletMusicSource.loop = (tabletPlayMode == TabletPlayMode.SingleRepeat);
        tabletMusicSource.Play();

        tabletCurrentClip = clip;
        tabletCurrentTrackId = m.id;
        tabletCurrentTrackTitle = m.title;
        tabletCurrentTrackCategory = m.category;
        tabletIsPlaying = true;

        if (tabletPlayerTitleLbl != null) tabletPlayerTitleLbl.text = m.title;
        if (tabletPlayerSubLbl != null) tabletPlayerSubLbl.text = m.category + " · 铁道原声品质";
        if (tabletTotalTimeLbl != null) tabletTotalTimeLbl.text = FormatAudioTime(clip.length);
        if (tabletPlayToggleBtn != null) tabletPlayToggleBtn.text = "❚❚";
        if (tabletFavToggleBtn != null)
        {
            bool isFav = IsTabletMusicFavorited(m.id);
            tabletFavToggleBtn.text = isFav ? "♥" : "♡";
            tabletFavToggleBtn.style.color = isFav ? new Color(0.92f, 0.25f, 0.30f, 1f) : new Color(0.6f, 0.6f, 0.65f, 1f);
        }
    }

    private void ToggleTabletMusicPlayPause()
    {
        if (tabletMusicSource == null || tabletCurrentClip == null)
        {
            // 若当前未播，播放第一首已解锁曲目
            foreach (var m in TabletMusicList)
            {
                if (IsTabletMusicUnlocked(m))
                {
                    PlayTabletTrack(m);
                    return;
                }
            }
            return;
        }

        if (tabletMusicSource.isPlaying)
        {
            tabletMusicSource.Pause();
            tabletIsPlaying = false;
            if (tabletPlayToggleBtn != null) tabletPlayToggleBtn.text = "▶";
        }
        else
        {
            tabletMusicSource.UnPause();
            tabletIsPlaying = true;
            if (tabletPlayToggleBtn != null) tabletPlayToggleBtn.text = "❚❚";
        }
    }

    private void PlayNextTabletTrack()
    {
        if (tabletPlayMode == TabletPlayMode.Shuffle)
        {
            var unlocked = new System.Collections.Generic.List<TabletMusicEntry>();
            foreach (var m in TabletMusicList) if (IsTabletMusicUnlocked(m)) unlocked.Add(m);
            if (unlocked.Count > 0)
            {
                int r = UnityEngine.Random.Range(0, unlocked.Count);
                PlayTabletTrack(unlocked[r]);
            }
            return;
        }

        int curIdx = -1;
        for (int i = 0; i < TabletMusicList.Length; i++)
        {
            if (TabletMusicList[i].id == tabletCurrentTrackId) { curIdx = i; break; }
        }

        for (int i = curIdx + 1; i < TabletMusicList.Length; i++)
        {
            if (IsTabletMusicUnlocked(TabletMusicList[i]))
            {
                PlayTabletTrack(TabletMusicList[i]);
                return;
            }
        }
        for (int i = 0; i < curIdx; i++)
        {
            if (IsTabletMusicUnlocked(TabletMusicList[i]))
            {
                PlayTabletTrack(TabletMusicList[i]);
                return;
            }
        }
    }

    private void PlayPrevTabletTrack()
    {
        int curIdx = -1;
        for (int i = 0; i < TabletMusicList.Length; i++)
        {
            if (TabletMusicList[i].id == tabletCurrentTrackId) { curIdx = i; break; }
        }

        for (int i = curIdx - 1; i >= 0; i--)
        {
            if (IsTabletMusicUnlocked(TabletMusicList[i]))
            {
                PlayTabletTrack(TabletMusicList[i]);
                return;
            }
        }
        for (int i = TabletMusicList.Length - 1; i > curIdx; i--)
        {
            if (IsTabletMusicUnlocked(TabletMusicList[i]))
            {
                PlayTabletTrack(TabletMusicList[i]);
                return;
            }
        }
    }

    private string FormatAudioTime(float sec)
    {
        if (float.IsNaN(sec) || sec < 0) sec = 0;
        int m = (int)(sec / 60f);
        int s = (int)(sec % 60f);
        return $"{m}:{s:D2}";
    }

    private void BuildMusicApp(VisualElement win)
    {
        win.Clear();

        // ===== 整体结构：外层容器（晨雾玉浅灰背景 + 双栏 Navigation Rail + 悬浮播放条）=====
        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.style.flexGrow = 1;
        root.style.backgroundColor = new Color(0.96f, 0.97f, 0.98f, 1f); // 柔浅玉灰底，杜绝纯白生硬
        win.Add(root);

        // ===== 左侧导航 Rail（固定 86px 宽，参考 Pad 端音乐）=====
        var navRail = new VisualElement();
        navRail.style.width = 86;
        navRail.style.backgroundColor = Color.white;
        navRail.style.borderRightWidth = 1;
        navRail.style.borderRightColor = new Color(0.90f, 0.91f, 0.93f, 1f);
        navRail.style.alignItems = Align.Center;
        navRail.style.paddingTop = 20;
        navRail.style.paddingBottom = 20;
        root.Add(navRail);

        // 顶部品牌黑胶 Logo
        var brandIconBox = new VisualElement();
        brandIconBox.style.width = 44; brandIconBox.style.height = 44;
        brandIconBox.style.borderTopLeftRadius = 22; brandIconBox.style.borderTopRightRadius = 22;
        brandIconBox.style.borderBottomLeftRadius = 22; brandIconBox.style.borderBottomRightRadius = 22;
        brandIconBox.style.backgroundColor = new Color(0.18f, 0.72f, 0.42f, 1f); // 翡翠绿
        brandIconBox.style.alignItems = Align.Center;
        brandIconBox.style.justifyContent = Justify.Center;
        brandIconBox.style.marginBottom = 6;
        navRail.Add(brandIconBox);

        var brandIconLbl = new Label("♫");
        brandIconLbl.style.fontSize = 20; brandIconLbl.style.color = Color.white;
        brandIconLbl.style.unityFontDefinition = Fd();
        brandIconBox.Add(brandIconLbl);

        var brandTitle = new Label("随身听");
        brandTitle.style.fontSize = 11;
        brandTitle.style.color = new Color(0.35f, 0.35f, 0.40f, 1f);
        brandTitle.style.unityFontDefinition = Fd();
        brandTitle.style.marginBottom = 24;
        navRail.Add(brandTitle);

        // 导航项目构建辅助方法
        System.Action<int, string, string> addNavTab = (tabIdx, iconSymbol, labelText) =>
        {
            bool isSelected = (tabletMusicNavTab == tabIdx);
            var tabBtn = new VisualElement();
            tabBtn.style.width = 70; tabBtn.style.height = 54;
            tabBtn.style.alignItems = Align.Center;
            tabBtn.style.justifyContent = Justify.Center;
            tabBtn.style.marginBottom = 12;
            tabBtn.style.borderTopLeftRadius = 12; tabBtn.style.borderTopRightRadius = 12;
            tabBtn.style.borderBottomLeftRadius = 12; tabBtn.style.borderBottomRightRadius = 12;
            tabBtn.style.backgroundColor = isSelected ? new Color(0.92f, 0.97f, 0.94f, 1f) : Color.clear;
            tabBtn.pickingMode = PickingMode.Position;

            var tabIcon = new Label(iconSymbol);
            tabIcon.style.fontSize = 18;
            tabIcon.style.color = isSelected ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.55f, 0.55f, 0.60f, 1f);
            tabIcon.style.unityFontDefinition = Fd();
            tabBtn.Add(tabIcon);

            var tabTxt = new Label(labelText);
            tabTxt.style.fontSize = 11;
            tabTxt.style.unityFontStyleAndWeight = isSelected ? FontStyle.Bold : FontStyle.Normal;
            tabTxt.style.color = isSelected ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.55f, 0.55f, 0.60f, 1f);
            tabTxt.style.unityFontDefinition = Fd();
            tabTxt.style.marginTop = 2;
            tabBtn.Add(tabTxt);

            tabBtn.RegisterCallback<ClickEvent>(_ =>
            {
                tabletMusicNavTab = tabIdx;
                BuildMusicApp(win);
            });
            navRail.Add(tabBtn);
        };

        addNavTab(0, "☵", "推荐");
        addNavTab(1, "♥", "我的");

        // ===== 右侧主内容区域（flexGrow=1）=====
        var mainArea = new VisualElement();
        mainArea.style.flexGrow = 1;
        mainArea.style.flexDirection = FlexDirection.Column;
        mainArea.style.position = Position.Relative;
        root.Add(mainArea);

        // 主体滚动内容区（留出底部 78px 避免被悬浮播放栏遮挡）
        var contentScroll = new ScrollView();
        contentScroll.style.flexGrow = 1;
        contentScroll.style.paddingLeft = 24; contentScroll.style.paddingRight = 24;
        contentScroll.style.paddingTop = 18; contentScroll.style.paddingBottom = 82;
        mainArea.Add(contentScroll);

        if (tabletMusicNavTab == 0)
        {
            // ==================== TAB 0: 推荐 / 乐库视图（参考图二）====================
            // 1. 金刚区（4 大方圆快捷功能卡片）
            var quickGrid = new VisualElement();
            quickGrid.style.flexDirection = FlexDirection.Row;
            quickGrid.style.justifyContent = Justify.SpaceBetween;
            quickGrid.style.marginBottom = 18;
            contentScroll.Add(quickGrid);

            System.Action<string, string, Color, System.Action> addQuickCard = (name, iconText, iconBg, onClick) =>
            {
                var qCard = new VisualElement();
                qCard.style.width = 175; qCard.style.height = 70;
                qCard.style.flexDirection = FlexDirection.Row;
                qCard.style.alignItems = Align.Center;
                qCard.style.backgroundColor = Color.white;
                qCard.style.borderTopLeftRadius = 14; qCard.style.borderTopRightRadius = 14;
                qCard.style.borderBottomLeftRadius = 14; qCard.style.borderBottomRightRadius = 14;
                qCard.style.borderTopWidth = 1; qCard.style.borderBottomWidth = 1;
                qCard.style.borderLeftWidth = 1; qCard.style.borderRightWidth = 1;
                qCard.style.borderTopColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                qCard.style.borderBottomColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                qCard.style.borderLeftColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                qCard.style.borderRightColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                qCard.style.paddingLeft = 12; qCard.style.paddingRight = 12;
                qCard.pickingMode = PickingMode.Position;

                var qIcon = new VisualElement();
                qIcon.style.width = 40; qIcon.style.height = 40;
                qIcon.style.borderTopLeftRadius = 20; qIcon.style.borderTopRightRadius = 20;
                qIcon.style.borderBottomLeftRadius = 20; qIcon.style.borderBottomRightRadius = 20;
                qIcon.style.backgroundColor = iconBg;
                qIcon.style.alignItems = Align.Center; qIcon.style.justifyContent = Justify.Center;
                qIcon.style.marginRight = 10;
                qCard.Add(qIcon);

                var qIconTxt = new Label(iconText);
                qIconTxt.style.fontSize = 16; qIconTxt.style.color = Color.white; qIconTxt.style.unityFontDefinition = Fd();
                qIcon.Add(qIconTxt);

                var qInfo = new VisualElement();
                qCard.Add(qInfo);

                var qName = new Label(name);
                qName.style.fontSize = 13; qName.style.unityFontStyleAndWeight = FontStyle.Bold;
                qName.style.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                qName.style.unityFontDefinition = Fd();
                qInfo.Add(qName);

                var qSub = new Label("铁道甄选");
                qSub.style.fontSize = 10; qSub.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
                qSub.style.unityFontDefinition = Fd();
                qInfo.Add(qSub);

                qCard.RegisterCallback<ClickEvent>(_ => onClick?.Invoke());
                quickGrid.Add(qCard);
            };

            addQuickCard("猜你想听", "♪", new Color(0.18f, 0.72f, 0.42f, 1f), () =>
            {
                // 随机播放一首已解锁曲目
                var unlocked = new System.Collections.Generic.List<TabletMusicEntry>();
                foreach (var m in TabletMusicList) if (IsTabletMusicUnlocked(m)) unlocked.Add(m);
                if (unlocked.Count > 0)
                {
                    int r = UnityEngine.Random.Range(0, unlocked.Count);
                    PlayTabletTrack(unlocked[r]);
                    BuildMusicApp(win);
                }
            });

            addQuickCard("每日精选", "★", new Color(0.25f, 0.65f, 0.90f, 1f), () =>
            {
                if (TabletMusicList.Length > 0 && IsTabletMusicUnlocked(TabletMusicList[0]))
                {
                    PlayTabletTrack(TabletMusicList[0]);
                    BuildMusicApp(win);
                }
            });

            addQuickCard("铁道原声", "❖", new Color(0.85f, 0.55f, 0.20f, 1f), () =>
            {
                tabletMusicFilter = "BGM";
                BuildMusicApp(win);
            });

            addQuickCard("先民歌谣", "🎙", new Color(0.75f, 0.32f, 0.45f, 1f), () =>
            {
                tabletMusicFilter = "歌曲";
                BuildMusicApp(win);
            });

            // 2. 「大家都在听」横条 + 高对比度分类筛选胶囊
            var sectionHeader = new VisualElement();
            sectionHeader.style.flexDirection = FlexDirection.Row;
            sectionHeader.style.justifyContent = Justify.SpaceBetween;
            sectionHeader.style.alignItems = Align.Center;
            sectionHeader.style.marginBottom = 14;
            contentScroll.Add(sectionHeader);

            var secLeft = new VisualElement();
            secLeft.style.flexDirection = FlexDirection.Row;
            secLeft.style.alignItems = Align.Center;
            sectionHeader.Add(secLeft);

            var secTitle = new Label("大家都在听 · 广播台音轨");
            secTitle.style.fontSize = 17; secTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            secTitle.style.color = new Color(0.10f, 0.10f, 0.12f, 1f);
            secTitle.style.unityFontDefinition = Fd();
            secTitle.style.marginRight = 12;
            secLeft.Add(secTitle);

            var playAllPill = new Button(() =>
            {
                // 播放全部
                foreach (var m in TabletMusicList)
                {
                    if (IsTabletMusicUnlocked(m))
                    {
                        PlayTabletTrack(m);
                        BuildMusicApp(win);
                        return;
                    }
                }
            }) { text = "▶ 播放全部" };
            playAllPill.style.height = 26; playAllPill.style.paddingLeft = 10; playAllPill.style.paddingRight = 10;
            playAllPill.style.fontSize = 11; playAllPill.style.unityFontDefinition = Fd();
            playAllPill.style.backgroundColor = new Color(0.92f, 0.97f, 0.94f, 1f);
            playAllPill.style.color = new Color(0.12f, 0.65f, 0.36f, 1f);
            playAllPill.style.borderTopLeftRadius = 13; playAllPill.style.borderTopRightRadius = 13;
            playAllPill.style.borderBottomLeftRadius = 13; playAllPill.style.borderBottomRightRadius = 13;
            playAllPill.style.borderTopWidth = 1; playAllPill.style.borderBottomWidth = 1;
            playAllPill.style.borderLeftWidth = 1; playAllPill.style.borderRightWidth = 1;
            playAllPill.style.borderTopColor = new Color(0.70f, 0.90f, 0.78f, 1f);
            playAllPill.style.borderBottomColor = new Color(0.70f, 0.90f, 0.78f, 1f);
            playAllPill.style.borderLeftColor = new Color(0.70f, 0.90f, 0.78f, 1f);
            playAllPill.style.borderRightColor = new Color(0.70f, 0.90f, 0.78f, 1f);
            secLeft.Add(playAllPill);

            // 右侧高对比度分类胶囊 Tab（完全解决白底看不清的问题）
            var filterGroup = new VisualElement();
            filterGroup.style.flexDirection = FlexDirection.Row;
            sectionHeader.Add(filterGroup);

            string[] categories = { "全部", "BGM", "歌曲" };
            foreach (var cat in categories)
            {
                string cName = cat;
                bool isSelected = (tabletMusicFilter == cName);
                var fBtn = new Button(() =>
                {
                    tabletMusicFilter = cName;
                    BuildMusicApp(win);
                }) { text = cName };
                fBtn.style.height = 28; fBtn.style.paddingLeft = 12; fBtn.style.paddingRight = 12;
                fBtn.style.fontSize = 12; fBtn.style.unityFontDefinition = Fd();
                fBtn.style.marginLeft = 6;
                fBtn.style.borderTopLeftRadius = 14; fBtn.style.borderTopRightRadius = 14;
                fBtn.style.borderBottomLeftRadius = 14; fBtn.style.borderBottomRightRadius = 14;
                fBtn.style.borderTopWidth = 1; fBtn.style.borderBottomWidth = 1;
                fBtn.style.borderLeftWidth = 1; fBtn.style.borderRightWidth = 1;
                if (isSelected)
                {
                    fBtn.style.backgroundColor = new Color(0.10f, 0.10f, 0.12f, 1f); // 纯黑底
                    fBtn.style.color = Color.white; // 纯白高对比字
                    fBtn.style.borderTopColor = new Color(0.10f, 0.10f, 0.12f, 1f);
                    fBtn.style.borderBottomColor = new Color(0.10f, 0.10f, 0.12f, 1f);
                    fBtn.style.borderLeftColor = new Color(0.10f, 0.10f, 0.12f, 1f);
                    fBtn.style.borderRightColor = new Color(0.10f, 0.10f, 0.12f, 1f);
                }
                else
                {
                    fBtn.style.backgroundColor = Color.white; // 纯白底
                    fBtn.style.color = new Color(0.25f, 0.25f, 0.28f, 1f); // 墨黑字
                    fBtn.style.borderTopColor = new Color(0.85f, 0.85f, 0.88f, 1f);
                    fBtn.style.borderBottomColor = new Color(0.85f, 0.85f, 0.88f, 1f);
                    fBtn.style.borderLeftColor = new Color(0.85f, 0.85f, 0.88f, 1f);
                    fBtn.style.borderRightColor = new Color(0.85f, 0.85f, 0.88f, 1f);
                }
                filterGroup.Add(fBtn);
            }

            // 3. 歌曲卡片网格
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.SpaceBetween;
            contentScroll.Add(grid);

            foreach (var m in TabletMusicList)
            {
                if (tabletMusicFilter != "全部" && m.category != tabletMusicFilter) continue;

                bool unlocked = IsTabletMusicUnlocked(m);
                bool isCurrentPlaying = (m.id == tabletCurrentTrackId && tabletIsPlaying);
                bool isFav = IsTabletMusicFavorited(m.id);

                var card = new VisualElement();
                card.style.width = 360; card.style.height = 74;
                card.style.marginBottom = 10;
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems = Align.Center;
                card.style.justifyContent = Justify.SpaceBetween;
                card.style.paddingLeft = 10; card.style.paddingRight = 12;
                card.style.backgroundColor = Color.white;
                card.style.borderTopLeftRadius = 12; card.style.borderTopRightRadius = 12;
                card.style.borderBottomLeftRadius = 12; card.style.borderBottomRightRadius = 12;
                card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
                card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
                card.style.borderTopColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                card.style.borderBottomColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                card.style.borderLeftColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                card.style.borderRightColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                grid.Add(card);

                // 左侧：黑胶唱片封套视觉（代码绘图方块）
                var coverBox = new VisualElement();
                coverBox.style.width = 50; coverBox.style.height = 50;
                coverBox.style.borderTopLeftRadius = 8; coverBox.style.borderTopRightRadius = 8;
                coverBox.style.borderBottomLeftRadius = 8; coverBox.style.borderBottomRightRadius = 8;
                coverBox.style.backgroundColor = isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.18f, 0.18f, 0.22f, 1f);
                coverBox.style.alignItems = Align.Center; coverBox.style.justifyContent = Justify.Center;
                coverBox.style.marginRight = 10;
                card.Add(coverBox);

                var discLabel = new Label(isCurrentPlaying ? "♪" : "◎");
                discLabel.style.fontSize = 18; discLabel.style.color = isCurrentPlaying ? Color.white : new Color(0.85f, 0.75f, 0.5f, 1f);
                discLabel.style.unityFontDefinition = Fd();
                coverBox.Add(discLabel);

                // 中间：歌名与副标
                var midInfo = new VisualElement();
                midInfo.style.flexGrow = 1;
                midInfo.style.overflow = Overflow.Hidden;
                card.Add(midInfo);

                var titleRow = new VisualElement();
                titleRow.style.flexDirection = FlexDirection.Row;
                titleRow.style.alignItems = Align.Center;
                midInfo.Add(titleRow);

                var tLbl = new Label(unlocked ? m.title : "??? 铁道保密曲目");
                tLbl.style.fontSize = 13; tLbl.style.unityFontStyleAndWeight = isCurrentPlaying ? FontStyle.Bold : FontStyle.Normal;
                tLbl.style.color = unlocked ? (isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.12f, 0.12f, 0.14f, 1f)) : new Color(0.6f, 0.6f, 0.65f, 1f);
                tLbl.style.unityFontDefinition = Fd();
                titleRow.Add(tLbl);

                var subRow = new VisualElement();
                subRow.style.flexDirection = FlexDirection.Row;
                subRow.style.alignItems = Align.Center;
                subRow.style.marginTop = 3;
                midInfo.Add(subRow);

                var tagLbl = new Label(m.category);
                tagLbl.style.fontSize = 9; tagLbl.style.unityFontDefinition = Fd();
                tagLbl.style.paddingLeft = 4; tagLbl.style.paddingRight = 4;
                tagLbl.style.paddingTop = 1; tagLbl.style.paddingBottom = 1;
                tagLbl.style.borderTopLeftRadius = 4; tagLbl.style.borderTopRightRadius = 4;
                tagLbl.style.borderBottomLeftRadius = 4; tagLbl.style.borderBottomRightRadius = 4;
                tagLbl.style.backgroundColor = m.category == "BGM" ? new Color(0.90f, 0.94f, 0.98f, 1f) : new Color(0.98f, 0.93f, 0.88f, 1f);
                tagLbl.style.color = m.category == "BGM" ? new Color(0.20f, 0.50f, 0.80f, 1f) : new Color(0.80f, 0.45f, 0.15f, 1f);
                tagLbl.style.marginRight = 6;
                subRow.Add(tagLbl);

                var condLbl = new Label(unlocked ? (isCurrentPlaying ? "正在播放中..." : "轻触试听") : "解锁: " + m.condition);
                condLbl.style.fontSize = 10;
                condLbl.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
                condLbl.style.unityFontDefinition = Fd();
                subRow.Add(condLbl);

                // 右侧：爱心收藏 + 播放圆钮
                var rightActions = new VisualElement();
                rightActions.style.flexDirection = FlexDirection.Row;
                rightActions.style.alignItems = Align.Center;
                card.Add(rightActions);

                if (unlocked)
                {
                    // 收藏心形按钮
                    var heartBtn = new Button(() =>
                    {
                        ToggleTabletMusicFavorite(m.id);
                        BuildMusicApp(win);
                    }) { text = isFav ? "♥" : "♡" };
                    heartBtn.style.width = 28; heartBtn.style.height = 28;
                    heartBtn.style.fontSize = 15; heartBtn.style.unityFontDefinition = Fd();
                    heartBtn.style.backgroundColor = Color.clear;
                    heartBtn.style.color = isFav ? new Color(0.92f, 0.25f, 0.30f, 1f) : new Color(0.65f, 0.65f, 0.70f, 1f);
                    heartBtn.style.borderTopWidth = 0; heartBtn.style.borderBottomWidth = 0;
                    heartBtn.style.borderLeftWidth = 0; heartBtn.style.borderRightWidth = 0;
                    heartBtn.style.marginRight = 4;
                    rightActions.Add(heartBtn);

                    // 播放圆钮
                    var pBtn = new Button(() =>
                    {
                        if (isCurrentPlaying)
                        {
                            ToggleTabletMusicPlayPause();
                        }
                        else
                        {
                            PlayTabletTrack(m);
                        }
                        BuildMusicApp(win);
                    }) { text = isCurrentPlaying ? "❚❚" : "▶" };
                    pBtn.style.width = 30; pBtn.style.height = 30;
                    pBtn.style.fontSize = 11; pBtn.style.unityFontDefinition = Fd();
                    pBtn.style.borderTopLeftRadius = 15; pBtn.style.borderTopRightRadius = 15;
                    pBtn.style.borderBottomLeftRadius = 15; pBtn.style.borderBottomRightRadius = 15;
                    pBtn.style.backgroundColor = isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.12f, 0.12f, 0.14f, 1f);
                    pBtn.style.color = Color.white;
                    rightActions.Add(pBtn);
                }
                else
                {
                    var lockLbl = new Label("🔒");
                    lockLbl.style.fontSize = 13; lockLbl.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
                    lockLbl.style.unityFontDefinition = Fd();
                    rightActions.Add(lockLbl);
                }
            }
        }
        else
        {
            // ==================== TAB 1: 我的 / 收藏视图（参考图三）====================
            // 1. 用户/会员横幅卡片
            var userProfileCard = new VisualElement();
            userProfileCard.style.flexDirection = FlexDirection.Row;
            userProfileCard.style.alignItems = Align.Center;
            userProfileCard.style.backgroundColor = Color.white;
            userProfileCard.style.borderTopLeftRadius = 16; userProfileCard.style.borderTopRightRadius = 16;
            userProfileCard.style.borderBottomLeftRadius = 16; userProfileCard.style.borderBottomRightRadius = 16;
            userProfileCard.style.borderTopWidth = 1; userProfileCard.style.borderBottomWidth = 1;
            userProfileCard.style.borderLeftWidth = 1; userProfileCard.style.borderRightWidth = 1;
            userProfileCard.style.borderTopColor = new Color(0.90f, 0.91f, 0.94f, 1f);
            userProfileCard.style.borderBottomColor = new Color(0.90f, 0.91f, 0.94f, 1f);
            userProfileCard.style.borderLeftColor = new Color(0.90f, 0.91f, 0.94f, 1f);
            userProfileCard.style.borderRightColor = new Color(0.90f, 0.91f, 0.94f, 1f);
            userProfileCard.style.paddingLeft = 18; userProfileCard.style.paddingRight = 18;
            userProfileCard.style.paddingTop = 14; userProfileCard.style.paddingBottom = 14;
            userProfileCard.style.marginBottom = 16;
            contentScroll.Add(userProfileCard);

            var avatarCircle = new VisualElement();
            avatarCircle.style.width = 46; avatarCircle.style.height = 46;
            avatarCircle.style.borderTopLeftRadius = 23; avatarCircle.style.borderTopRightRadius = 23;
            avatarCircle.style.borderBottomLeftRadius = 23; avatarCircle.style.borderBottomRightRadius = 23;
            avatarCircle.style.backgroundColor = new Color(0.20f, 0.55f, 0.85f, 1f);
            avatarCircle.style.alignItems = Align.Center; avatarCircle.style.justifyContent = Justify.Center;
            avatarCircle.style.marginRight = 14;
            userProfileCard.Add(avatarCircle);

            var avatarLetter = new Label("林");
            avatarLetter.style.fontSize = 20; avatarLetter.style.color = Color.white;
            avatarLetter.style.unityFontDefinition = Fd();
            avatarCircle.Add(avatarLetter);

            var userInfoBox = new VisualElement();
            userProfileCard.Add(userInfoBox);

            var uNameRow = new VisualElement();
            uNameRow.style.flexDirection = FlexDirection.Row;
            uNameRow.style.alignItems = Align.Center;
            userInfoBox.Add(uNameRow);

            var uName = new Label("林彪悍 · 首席调度员");
            uName.style.fontSize = 15; uName.style.unityFontStyleAndWeight = FontStyle.Bold;
            uName.style.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            uName.style.unityFontDefinition = Fd();
            uName.style.marginRight = 8;
            uNameRow.Add(uName);

            var vipBadge = new Label("VIP 铁道特权");
            vipBadge.style.fontSize = 10; vipBadge.style.color = Color.white;
            vipBadge.style.backgroundColor = new Color(0.18f, 0.72f, 0.42f, 1f);
            vipBadge.style.paddingLeft = 6; vipBadge.style.paddingRight = 6;
            vipBadge.style.paddingTop = 1; vipBadge.style.paddingBottom = 1;
            vipBadge.style.borderTopLeftRadius = 4; vipBadge.style.borderTopRightRadius = 4;
            vipBadge.style.borderBottomLeftRadius = 4; vipBadge.style.borderBottomRightRadius = 4;
            vipBadge.style.unityFontDefinition = Fd();
            uNameRow.Add(vipBadge);

            var uSub = new Label("母带原声特权生效中 · 13 首铁道音轨已就绪");
            uSub.style.fontSize = 11; uSub.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
            uSub.style.unityFontDefinition = Fd();
            uSub.style.marginTop = 3;
            userInfoBox.Add(uSub);

            // 2. 快捷四数据卡片（参考图三：喜欢 / 最近 / 本地 / 已购）
            var myGrid = new VisualElement();
            myGrid.style.flexDirection = FlexDirection.Row;
            myGrid.style.justifyContent = Justify.SpaceBetween;
            myGrid.style.marginBottom = 18;
            contentScroll.Add(myGrid);

            int favCount = GetTabletFavoriteCount();
            int unlockedCount = 0;
            foreach (var m in TabletMusicList) if (IsTabletMusicUnlocked(m)) unlockedCount++;

            System.Action<string, string, string, Color> addStatCard = (title, countStr, iconText, iconCol) =>
            {
                var sCard = new VisualElement();
                sCard.style.width = 175; sCard.style.height = 76;
                sCard.style.backgroundColor = Color.white;
                sCard.style.borderTopLeftRadius = 14; sCard.style.borderTopRightRadius = 14;
                sCard.style.borderBottomLeftRadius = 14; sCard.style.borderBottomRightRadius = 14;
                sCard.style.borderTopWidth = 1; sCard.style.borderBottomWidth = 1;
                sCard.style.borderLeftWidth = 1; sCard.style.borderRightWidth = 1;
                sCard.style.borderTopColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                sCard.style.borderBottomColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                sCard.style.borderLeftColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                sCard.style.borderRightColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                sCard.style.alignItems = Align.Center; sCard.style.justifyContent = Justify.Center;

                var sIcon = new Label(iconText);
                sIcon.style.fontSize = 18; sIcon.style.color = iconCol; sIcon.style.unityFontDefinition = Fd();
                sCard.Add(sIcon);

                var sTitle = new Label(title);
                sTitle.style.fontSize = 12; sTitle.style.color = new Color(0.2f, 0.2f, 0.22f, 1f);
                sTitle.style.unityFontDefinition = Fd();
                sTitle.style.marginTop = 2;
                sCard.Add(sTitle);

                var sCount = new Label(countStr);
                sCount.style.fontSize = 10; sCount.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
                sCount.style.unityFontDefinition = Fd();
                sCard.Add(sCount);

                myGrid.Add(sCard);
            };

            addStatCard("喜欢", favCount + " 首", "♥", new Color(0.92f, 0.25f, 0.30f, 1f));
            addStatCard("最近播放", (string.IsNullOrEmpty(tabletCurrentTrackTitle) ? "暂无" : "1 首"), "◷", new Color(0.18f, 0.72f, 0.42f, 1f));
            addStatCard("本地原声", "13 首", "💾", new Color(0.25f, 0.65f, 0.90f, 1f));
            addStatCard("已收录", unlockedCount + " 首", "✓", new Color(0.85f, 0.55f, 0.20f, 1f));

            // 3. 我喜欢的音乐列表
            var favSectionHead = new VisualElement();
            favSectionHead.style.flexDirection = FlexDirection.Row;
            favSectionHead.style.justifyContent = Justify.SpaceBetween;
            favSectionHead.style.alignItems = Align.Center;
            favSectionHead.style.marginBottom = 12;
            contentScroll.Add(favSectionHead);

            var favSecTitle = new Label("我喜欢的音乐 (" + favCount + ")");
            favSecTitle.style.fontSize = 16; favSecTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            favSecTitle.style.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            favSecTitle.style.unityFontDefinition = Fd();
            favSectionHead.Add(favSecTitle);

            var favList = new System.Collections.Generic.List<TabletMusicEntry>();
            foreach (var m in TabletMusicList)
            {
                if (IsTabletMusicFavorited(m.id) && IsTabletMusicUnlocked(m)) favList.Add(m);
            }

            if (favList.Count == 0)
            {
                // 空状态卡片
                var emptyCard = new VisualElement();
                emptyCard.style.paddingTop = 30; emptyCard.style.paddingBottom = 30;
                emptyCard.style.alignItems = Align.Center; emptyCard.style.justifyContent = Justify.Center;
                emptyCard.style.backgroundColor = Color.white;
                emptyCard.style.borderTopLeftRadius = 14; emptyCard.style.borderTopRightRadius = 14;
                emptyCard.style.borderBottomLeftRadius = 14; emptyCard.style.borderBottomRightRadius = 14;
                emptyCard.style.borderTopWidth = 1; emptyCard.style.borderBottomWidth = 1;
                emptyCard.style.borderLeftWidth = 1; emptyCard.style.borderRightWidth = 1;
                emptyCard.style.borderTopColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                emptyCard.style.borderBottomColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                emptyCard.style.borderLeftColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                emptyCard.style.borderRightColor = new Color(0.92f, 0.92f, 0.95f, 1f);
                contentScroll.Add(emptyCard);

                var emptyIcon = new Label("♡");
                emptyIcon.style.fontSize = 32; emptyIcon.style.color = new Color(0.75f, 0.75f, 0.80f, 1f);
                emptyIcon.style.unityFontDefinition = Fd();
                emptyCard.Add(emptyIcon);

                var emptyText = new Label("暂无收藏曲目。轻触乐库中的爱心即可将歌曲加入这里！");
                emptyText.style.fontSize = 13; emptyText.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
                emptyText.style.unityFontDefinition = Fd();
                emptyText.style.marginTop = 8;
                emptyCard.Add(emptyText);

                var goExploreBtn = new Button(() =>
                {
                    tabletMusicNavTab = 0;
                    BuildMusicApp(win);
                }) { text = "前往乐库发现音乐" };
                goExploreBtn.style.height = 30; goExploreBtn.style.paddingLeft = 14; goExploreBtn.style.paddingRight = 14;
                goExploreBtn.style.fontSize = 12; goExploreBtn.style.unityFontDefinition = Fd();
                goExploreBtn.style.backgroundColor = new Color(0.12f, 0.65f, 0.36f, 1f);
                goExploreBtn.style.color = Color.white;
                goExploreBtn.style.borderTopLeftRadius = 15; goExploreBtn.style.borderTopRightRadius = 15;
                goExploreBtn.style.borderBottomLeftRadius = 15; goExploreBtn.style.borderBottomRightRadius = 15;
                goExploreBtn.style.marginTop = 12;
                emptyCard.Add(goExploreBtn);
            }
            else
            {
                var favGrid = new VisualElement();
                favGrid.style.flexDirection = FlexDirection.Row;
                favGrid.style.flexWrap = Wrap.Wrap;
                favGrid.style.justifyContent = Justify.SpaceBetween;
                contentScroll.Add(favGrid);

                foreach (var m in favList)
                {
                    bool isCurrentPlaying = (m.id == tabletCurrentTrackId && tabletIsPlaying);

                    var card = new VisualElement();
                    card.style.width = 360; card.style.height = 68;
                    card.style.marginBottom = 10;
                    card.style.flexDirection = FlexDirection.Row;
                    card.style.alignItems = Align.Center;
                    card.style.justifyContent = Justify.SpaceBetween;
                    card.style.paddingLeft = 10; card.style.paddingRight = 12;
                    card.style.backgroundColor = Color.white;
                    card.style.borderTopLeftRadius = 12; card.style.borderTopRightRadius = 12;
                    card.style.borderBottomLeftRadius = 12; card.style.borderBottomRightRadius = 12;
                    card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
                    card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
                    card.style.borderTopColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                    card.style.borderBottomColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                    card.style.borderLeftColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                    card.style.borderRightColor = isCurrentPlaying ? new Color(0.18f, 0.72f, 0.42f, 0.8f) : new Color(0.92f, 0.92f, 0.95f, 1f);
                    favGrid.Add(card);

                    var coverBox = new VisualElement();
                    coverBox.style.width = 46; coverBox.style.height = 46;
                    coverBox.style.borderTopLeftRadius = 8; coverBox.style.borderTopRightRadius = 8;
                    coverBox.style.borderBottomLeftRadius = 8; coverBox.style.borderBottomRightRadius = 8;
                    coverBox.style.backgroundColor = isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.92f, 0.25f, 0.30f, 0.9f);
                    coverBox.style.alignItems = Align.Center; coverBox.style.justifyContent = Justify.Center;
                    coverBox.style.marginRight = 10;
                    card.Add(coverBox);

                    var discLabel = new Label("♥");
                    discLabel.style.fontSize = 18; discLabel.style.color = Color.white;
                    discLabel.style.unityFontDefinition = Fd();
                    coverBox.Add(discLabel);

                    var midInfo = new VisualElement();
                    midInfo.style.flexGrow = 1;
                    card.Add(midInfo);

                    var tLbl = new Label(m.title);
                    tLbl.style.fontSize = 13; tLbl.style.unityFontStyleAndWeight = isCurrentPlaying ? FontStyle.Bold : FontStyle.Normal;
                    tLbl.style.color = isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.12f, 0.12f, 0.14f, 1f);
                    tLbl.style.unityFontDefinition = Fd();
                    midInfo.Add(tLbl);

                    var subLbl = new Label(m.category + " · 已加入红心收藏");
                    subLbl.style.fontSize = 10; subLbl.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
                    subLbl.style.unityFontDefinition = Fd();
                    midInfo.Add(subLbl);

                    var rightActions = new VisualElement();
                    rightActions.style.flexDirection = FlexDirection.Row;
                    rightActions.style.alignItems = Align.Center;
                    card.Add(rightActions);

                    var unFavBtn = new Button(() =>
                    {
                        ToggleTabletMusicFavorite(m.id);
                        BuildMusicApp(win);
                    }) { text = "♥" };
                    unFavBtn.style.width = 28; unFavBtn.style.height = 28;
                    unFavBtn.style.fontSize = 15; unFavBtn.style.unityFontDefinition = Fd();
                    unFavBtn.style.backgroundColor = Color.clear;
                    unFavBtn.style.color = new Color(0.92f, 0.25f, 0.30f, 1f);
                    unFavBtn.style.borderTopWidth = 0; unFavBtn.style.borderBottomWidth = 0;
                    unFavBtn.style.borderLeftWidth = 0; unFavBtn.style.borderRightWidth = 0;
                    rightActions.Add(unFavBtn);

                    var pBtn = new Button(() =>
                    {
                        if (isCurrentPlaying) ToggleTabletMusicPlayPause();
                        else PlayTabletTrack(m);
                        BuildMusicApp(win);
                    }) { text = isCurrentPlaying ? "❚❚" : "▶" };
                    pBtn.style.width = 30; pBtn.style.height = 30;
                    pBtn.style.fontSize = 11; pBtn.style.unityFontDefinition = Fd();
                    pBtn.style.borderTopLeftRadius = 15; pBtn.style.borderTopRightRadius = 15;
                    pBtn.style.borderBottomLeftRadius = 15; pBtn.style.borderBottomRightRadius = 15;
                    pBtn.style.backgroundColor = isCurrentPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.12f, 0.12f, 0.14f, 1f);
                    pBtn.style.color = Color.white;
                    pBtn.style.marginLeft = 4;
                    rightActions.Add(pBtn);
                }
            }
        }

        // ===== 底部悬浮流媒体播放条（参考图二图三下方悬浮胶囊栏）=====
        var bar = new VisualElement();
        bar.style.position = Position.Absolute;
        bar.style.bottom = 10;
        bar.style.left = 16;
        bar.style.right = 16;
        bar.style.height = 66;
        bar.style.flexDirection = FlexDirection.Row;
        bar.style.alignItems = Align.Center;
        bar.style.justifyContent = Justify.SpaceBetween;
        bar.style.backgroundColor = new Color(1f, 1f, 1f, 0.98f);
        bar.style.borderTopLeftRadius = 16; bar.style.borderTopRightRadius = 16;
        bar.style.borderBottomLeftRadius = 16; bar.style.borderBottomRightRadius = 16;
        bar.style.borderTopWidth = 1; bar.style.borderBottomWidth = 1;
        bar.style.borderLeftWidth = 1; bar.style.borderRightWidth = 1;
        bar.style.borderTopColor = new Color(0.86f, 0.87f, 0.90f, 1f);
        bar.style.borderBottomColor = new Color(0.86f, 0.87f, 0.90f, 1f);
        bar.style.borderLeftColor = new Color(0.86f, 0.87f, 0.90f, 1f);
        bar.style.borderRightColor = new Color(0.86f, 0.87f, 0.90f, 1f);
        bar.style.paddingLeft = 14; bar.style.paddingRight = 16;
        mainArea.Add(bar);

        // 左部：曲目封面 + 标题 + 收藏心标
        var barLeft = new VisualElement();
        barLeft.style.width = 230;
        barLeft.style.flexDirection = FlexDirection.Row;
        barLeft.style.alignItems = Align.Center;
        bar.Add(barLeft);

        var miniCover = new VisualElement();
        miniCover.style.width = 40; miniCover.style.height = 40;
        miniCover.style.borderTopLeftRadius = 8; miniCover.style.borderTopRightRadius = 8;
        miniCover.style.borderBottomLeftRadius = 8; miniCover.style.borderBottomRightRadius = 8;
        miniCover.style.backgroundColor = tabletIsPlaying ? new Color(0.12f, 0.65f, 0.36f, 1f) : new Color(0.18f, 0.18f, 0.22f, 1f);
        miniCover.style.alignItems = Align.Center; miniCover.style.justifyContent = Justify.Center;
        miniCover.style.marginRight = 10;
        barLeft.Add(miniCover);

        var miniDiscIcon = new Label(tabletIsPlaying ? "♫" : "◎");
        miniDiscIcon.style.fontSize = 16; miniDiscIcon.style.color = Color.white;
        miniDiscIcon.style.unityFontDefinition = Fd();
        miniCover.Add(miniDiscIcon);

        var barTextGroup = new VisualElement();
        barTextGroup.style.flexGrow = 1;
        barTextGroup.style.overflow = Overflow.Hidden;
        barLeft.Add(barTextGroup);

        tabletPlayerTitleLbl = new Label(string.IsNullOrEmpty(tabletCurrentTrackTitle) ? "未选择播放曲目" : tabletCurrentTrackTitle);
        tabletPlayerTitleLbl.style.fontSize = 13; tabletPlayerTitleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        tabletPlayerTitleLbl.style.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        tabletPlayerTitleLbl.style.unityFontDefinition = Fd();
        barTextGroup.Add(tabletPlayerTitleLbl);

        tabletPlayerSubLbl = new Label(string.IsNullOrEmpty(tabletCurrentTrackCategory) ? "轻触上方曲目即可收听" : tabletCurrentTrackCategory + " · 铁道原声品质");
        tabletPlayerSubLbl.style.fontSize = 10; tabletPlayerSubLbl.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
        tabletPlayerSubLbl.style.unityFontDefinition = Fd();
        tabletPlayerSubLbl.style.marginTop = 2;
        barTextGroup.Add(tabletPlayerSubLbl);

        bool currentIsFav = !string.IsNullOrEmpty(tabletCurrentTrackId) && IsTabletMusicFavorited(tabletCurrentTrackId);
        tabletFavToggleBtn = new Button(() =>
        {
            if (!string.IsNullOrEmpty(tabletCurrentTrackId))
            {
                ToggleTabletMusicFavorite(tabletCurrentTrackId);
                BuildMusicApp(win);
            }
        }) { text = currentIsFav ? "♥" : "♡" };
        tabletFavToggleBtn.style.width = 30; tabletFavToggleBtn.style.height = 30;
        tabletFavToggleBtn.style.fontSize = 16; tabletFavToggleBtn.style.unityFontDefinition = Fd();
        tabletFavToggleBtn.style.backgroundColor = Color.clear;
        tabletFavToggleBtn.style.color = currentIsFav ? new Color(0.92f, 0.25f, 0.30f, 1f) : new Color(0.60f, 0.60f, 0.65f, 1f);
        tabletFavToggleBtn.style.borderTopWidth = 0; tabletFavToggleBtn.style.borderBottomWidth = 0;
        tabletFavToggleBtn.style.borderLeftWidth = 0; tabletFavToggleBtn.style.borderRightWidth = 0;
        tabletFavToggleBtn.style.marginLeft = 6;
        barLeft.Add(tabletFavToggleBtn);

        // 中部：控制按钮组 + 灵敏点击/拖动跳转进度条
        var barCenter = new VisualElement();
        barCenter.style.flexGrow = 1;
        barCenter.style.flexDirection = FlexDirection.Column;
        barCenter.style.alignItems = Align.Center;
        barCenter.style.justifyContent = Justify.Center;
        barCenter.style.marginLeft = 16; barCenter.style.marginRight = 16;
        bar.Add(barCenter);

        // 控制按钮排（上一首 / 播放暂停圆钮 / 下一首）
        var ctrlRow = new VisualElement();
        ctrlRow.style.flexDirection = FlexDirection.Row;
        ctrlRow.style.alignItems = Align.Center;
        ctrlRow.style.marginBottom = 4;
        barCenter.Add(ctrlRow);

        var prevBtn = new Button(() => { PlayPrevTabletTrack(); BuildMusicApp(win); }) { text = "|◀" };
        prevBtn.style.width = 28; prevBtn.style.height = 28;
        prevBtn.style.fontSize = 11; prevBtn.style.unityFontDefinition = Fd();
        prevBtn.style.backgroundColor = Color.clear;
        prevBtn.style.color = new Color(0.3f, 0.3f, 0.35f, 1f);
        prevBtn.style.borderTopWidth = 0; prevBtn.style.borderBottomWidth = 0;
        prevBtn.style.borderLeftWidth = 0; prevBtn.style.borderRightWidth = 0;
        ctrlRow.Add(prevBtn);

        tabletPlayToggleBtn = new Button(ToggleTabletMusicPlayPause) { text = (tabletIsPlaying ? "❚❚" : "▶") };
        tabletPlayToggleBtn.style.width = 34; tabletPlayToggleBtn.style.height = 34;
        tabletPlayToggleBtn.style.fontSize = 13; tabletPlayToggleBtn.style.unityFontDefinition = Fd();
        tabletPlayToggleBtn.style.borderTopLeftRadius = 17; tabletPlayToggleBtn.style.borderTopRightRadius = 17;
        tabletPlayToggleBtn.style.borderBottomLeftRadius = 17; tabletPlayToggleBtn.style.borderBottomRightRadius = 17;
        tabletPlayToggleBtn.style.backgroundColor = new Color(0.12f, 0.65f, 0.36f, 1f); // 翡翠绿大圆钮
        tabletPlayToggleBtn.style.color = Color.white;
        tabletPlayToggleBtn.style.borderTopWidth = 0; tabletPlayToggleBtn.style.borderBottomWidth = 0;
        tabletPlayToggleBtn.style.borderLeftWidth = 0; tabletPlayToggleBtn.style.borderRightWidth = 0;
        tabletPlayToggleBtn.style.marginLeft = 12; tabletPlayToggleBtn.style.marginRight = 12;
        ctrlRow.Add(tabletPlayToggleBtn);

        var nextBtn = new Button(() => { PlayNextTabletTrack(); BuildMusicApp(win); }) { text = "▶|" };
        nextBtn.style.width = 28; nextBtn.style.height = 28;
        nextBtn.style.fontSize = 11; nextBtn.style.unityFontDefinition = Fd();
        nextBtn.style.backgroundColor = Color.clear;
        nextBtn.style.color = new Color(0.3f, 0.3f, 0.35f, 1f);
        nextBtn.style.borderTopWidth = 0; nextBtn.style.borderBottomWidth = 0;
        nextBtn.style.borderLeftWidth = 0; nextBtn.style.borderRightWidth = 0;
        ctrlRow.Add(nextBtn);

        // 灵敏进度条排（当前时间 + 自定义点击/滑动轨道 + 总时间）
        var progRow = new VisualElement();
        progRow.style.width = Length.Percent(100);
        progRow.style.flexDirection = FlexDirection.Row;
        progRow.style.alignItems = Align.Center;
        barCenter.Add(progRow);

        tabletCurTimeLbl = new Label(tabletMusicSource != null ? FormatAudioTime(tabletMusicSource.time) : "0:00");
        tabletCurTimeLbl.style.fontSize = 10; tabletCurTimeLbl.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
        tabletCurTimeLbl.style.unityFontDefinition = Fd();
        tabletCurTimeLbl.style.width = 30;
        progRow.Add(tabletCurTimeLbl);

        // 高灵敏交互轨道：高度 16px 充当点击碰撞区，中间 4px 细滑轨
        tabletProgressTrack = new VisualElement();
        tabletProgressTrack.style.flexGrow = 1;
        tabletProgressTrack.style.height = 16;
        tabletProgressTrack.style.marginLeft = 8; tabletProgressTrack.style.marginRight = 8;
        tabletProgressTrack.style.justifyContent = Justify.Center;
        tabletProgressTrack.pickingMode = PickingMode.Position;
        progRow.Add(tabletProgressTrack);

        var trackBg = new VisualElement();
        trackBg.style.height = 4;
        trackBg.style.backgroundColor = new Color(0.88f, 0.89f, 0.92f, 1f);
        trackBg.style.borderTopLeftRadius = 2; trackBg.style.borderTopRightRadius = 2;
        trackBg.style.borderBottomLeftRadius = 2; trackBg.style.borderBottomRightRadius = 2;
        trackBg.style.position = Position.Relative;
        trackBg.pickingMode = PickingMode.Ignore;
        tabletProgressTrack.Add(trackBg);

        float initialPct = 0f;
        if (tabletMusicSource != null && tabletCurrentClip != null && tabletCurrentClip.length > 0.1f)
        {
            initialPct = Mathf.Clamp01(tabletMusicSource.time / tabletCurrentClip.length) * 100f;
        }

        tabletProgressFill = new VisualElement();
        tabletProgressFill.style.height = Length.Percent(100);
        tabletProgressFill.style.width = Length.Percent(initialPct);
        tabletProgressFill.style.backgroundColor = new Color(0.12f, 0.65f, 0.36f, 1f);
        tabletProgressFill.style.borderTopLeftRadius = 2; tabletProgressFill.style.borderTopRightRadius = 2;
        tabletProgressFill.style.borderBottomLeftRadius = 2; tabletProgressFill.style.borderBottomRightRadius = 2;
        tabletProgressFill.pickingMode = PickingMode.Ignore;
        trackBg.Add(tabletProgressFill);

        tabletProgressKnob = new VisualElement();
        tabletProgressKnob.style.width = 10; tabletProgressKnob.style.height = 10;
        tabletProgressKnob.style.borderTopLeftRadius = 5; tabletProgressKnob.style.borderTopRightRadius = 5;
        tabletProgressKnob.style.borderBottomLeftRadius = 5; tabletProgressKnob.style.borderBottomRightRadius = 5;
        tabletProgressKnob.style.backgroundColor = Color.white;
        tabletProgressKnob.style.borderTopWidth = 1.5f; tabletProgressKnob.style.borderBottomWidth = 1.5f;
        tabletProgressKnob.style.borderLeftWidth = 1.5f; tabletProgressKnob.style.borderRightWidth = 1.5f;
        tabletProgressKnob.style.borderTopColor = new Color(0.12f, 0.65f, 0.36f, 1f);
        tabletProgressKnob.style.borderBottomColor = new Color(0.12f, 0.65f, 0.36f, 1f);
        tabletProgressKnob.style.borderLeftColor = new Color(0.12f, 0.65f, 0.36f, 1f);
        tabletProgressKnob.style.borderRightColor = new Color(0.12f, 0.65f, 0.36f, 1f);
        tabletProgressKnob.style.position = Position.Absolute;
        tabletProgressKnob.style.top = -3;
        tabletProgressKnob.style.left = Length.Percent(initialPct);
        tabletProgressKnob.pickingMode = PickingMode.Ignore;
        trackBg.Add(tabletProgressKnob);

        // 核心：彻底支持点击跳转 + 滑动跳转
        System.Action<Vector2> handleSeek = (localPos) =>
        {
            if (tabletMusicSource == null || tabletCurrentClip == null) return;
            float trackWidth = tabletProgressTrack.resolvedStyle.width;
            if (trackWidth <= 10f) return;
            float ratio = Mathf.Clamp01(localPos.x / trackWidth);
            tabletMusicSource.time = ratio * tabletCurrentClip.length;
            if (tabletCurTimeLbl != null) tabletCurTimeLbl.text = FormatAudioTime(tabletMusicSource.time);
            if (tabletProgressFill != null) tabletProgressFill.style.width = Length.Percent(ratio * 100f);
            if (tabletProgressKnob != null) tabletProgressKnob.style.left = Length.Percent(ratio * 100f);
        };

        tabletProgressTrack.RegisterCallback<PointerDownEvent>(evt =>
        {
            tabletProgressScrubbing = true;
            handleSeek(evt.localPosition);
            tabletProgressTrack.CapturePointer(evt.pointerId);
        });
        tabletProgressTrack.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (tabletProgressScrubbing) handleSeek(evt.localPosition);
        });
        tabletProgressTrack.RegisterCallback<PointerUpEvent>(evt =>
        {
            tabletProgressScrubbing = false;
            if (tabletProgressTrack.HasPointerCapture(evt.pointerId))
                tabletProgressTrack.ReleasePointer(evt.pointerId);
        });
        tabletProgressTrack.RegisterCallback<PointerCaptureOutEvent>(_ => tabletProgressScrubbing = false);

        tabletTotalTimeLbl = new Label(tabletCurrentClip != null ? FormatAudioTime(tabletCurrentClip.length) : "0:00");
        tabletTotalTimeLbl.style.fontSize = 10; tabletTotalTimeLbl.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
        tabletTotalTimeLbl.style.unityFontDefinition = Fd();
        tabletTotalTimeLbl.style.width = 30;
        progRow.Add(tabletTotalTimeLbl);

        // 右部：播放模式切换 + 音量快捷控制
        var barRight = new VisualElement();
        barRight.style.width = 170;
        barRight.style.flexDirection = FlexDirection.Row;
        barRight.style.alignItems = Align.Center;
        barRight.style.justifyContent = Justify.FlexEnd;
        bar.Add(barRight);

        string modeStr = (tabletPlayMode == TabletPlayMode.SingleRepeat ? "单曲循环" : (tabletPlayMode == TabletPlayMode.Sequential ? "顺序播放" : "随机播放"));
        tabletModeToggleBtn = new Button(() =>
        {
            tabletPlayMode = (TabletPlayMode)(((int)tabletPlayMode + 1) % 3);
            if (tabletMusicSource != null) tabletMusicSource.loop = (tabletPlayMode == TabletPlayMode.SingleRepeat);
            BuildMusicApp(win);
        }) { text = modeStr };
        tabletModeToggleBtn.style.height = 26; tabletModeToggleBtn.style.paddingLeft = 8; tabletModeToggleBtn.style.paddingRight = 8;
        tabletModeToggleBtn.style.fontSize = 11; tabletModeToggleBtn.style.unityFontDefinition = Fd();
        tabletModeToggleBtn.style.backgroundColor = new Color(0.95f, 0.95f, 0.97f, 1f);
        tabletModeToggleBtn.style.color = new Color(0.25f, 0.25f, 0.28f, 1f);
        tabletModeToggleBtn.style.borderTopLeftRadius = 13; tabletModeToggleBtn.style.borderTopRightRadius = 13;
        tabletModeToggleBtn.style.borderBottomLeftRadius = 13; tabletModeToggleBtn.style.borderBottomRightRadius = 13;
        tabletModeToggleBtn.style.borderTopWidth = 1; tabletModeToggleBtn.style.borderBottomWidth = 1;
        tabletModeToggleBtn.style.borderLeftWidth = 1; tabletModeToggleBtn.style.borderRightWidth = 1;
        tabletModeToggleBtn.style.borderTopColor = new Color(0.88f, 0.88f, 0.92f, 1f);
        tabletModeToggleBtn.style.borderBottomColor = new Color(0.88f, 0.88f, 0.92f, 1f);
        tabletModeToggleBtn.style.borderLeftColor = new Color(0.88f, 0.88f, 0.92f, 1f);
        tabletModeToggleBtn.style.borderRightColor = new Color(0.88f, 0.88f, 0.92f, 1f);
        barRight.Add(tabletModeToggleBtn);

        var volBtn = new Button(() =>
        {
            if (tabletMusicSource != null)
            {
                tabletMusicSource.volume = (tabletMusicSource.volume > 0.05f) ? 0f : 0.65f;
                ShowBackupToast(tabletMusicSource.volume > 0.05f ? "音量已恢复" : "已静音");
            }
        }) { text = "🔊" };
        volBtn.style.width = 28; volBtn.style.height = 28;
        volBtn.style.fontSize = 13; volBtn.style.unityFontDefinition = Fd();
        volBtn.style.backgroundColor = Color.clear;
        volBtn.style.borderTopWidth = 0; volBtn.style.borderBottomWidth = 0;
        volBtn.style.borderLeftWidth = 0; volBtn.style.borderRightWidth = 0;
        volBtn.style.marginLeft = 6;
        barRight.Add(volBtn);
    }

    // ============ App: 相册（分类筛选 + 真实存档解锁）============
    private int currentGalleryCategory = 0; // 0: 全部, 1: 历史车站, 2: 暮色与晨曦, 3: 车厢与机库

    private static readonly (string resId, string title, string desc, int cat)[] MasterGalleryItems = {
        ("songqiao_station", "松桥站 · 午后", "剥落红砖、生苔灰瓦、斜插狗尾草的旧铁牌。", 1),
        ("platform_evening", "雾峰站 · 暮色站台", "夕阳余晖斜照在石板上，远山如黛。", 2),
        ("platform_morning", "雾峰站 · 晨曦", "破晓的第一缕阳光穿透薄雾照耀钢轨。", 2),
        ("station", "雾峰站 · 正面全景", "老旧但依然挺立的车站主房，四年前的车站牌。", 1),
        ("station_sunset", "雾峰站 · 日落金辉", "金黄色的晚霞泼洒在梯田与铁路线上。", 2),
        ("station_office", "站务室 · 调度图", "木头老算盘、泛黄的日历与墙上的手绘时刻表。", 1),
        ("railway_track", "铁道延伸 · 枕木", "单线铁轨锈蚀严重，枕木缝里长出杂草与小黄花。", 1),
        ("depot_morning", "机务段车库 · 晨雾", "机修车间弥漫着机油与水汽的温润气息。", 3),
        ("hangar", "机车库 · 0721号", "停放着沉睡23年的先民动力机车。", 3),
        ("cabin_interior", "客厢白天 · 阳光", "暖阳靠窗木制长椅，绿皮革椅面有些磨损。", 3),
        ("tea_house", "茶室 · 梯田远眺", "山坡茶园梯田，茶树深绿弧形条带层层叠叠。", 1)
    };

    private bool IsGalleryPhotoUnlocked(string resId)
    {
        if (resId == "songqiao_station") return true; // 序章默认解锁
        return PlayerPrefs.GetInt("ArchiveScene_" + resId, 0) == 1
            || PlayerPrefs.GetInt("ArchiveCG_" + resId, 0) == 1
            || PlayerPrefs.GetInt("VN_Got0721", 0) == 1;
    }

    private void BuildGalleryApp(VisualElement win)
    {
        win.Clear();

        // 全屏画廊（左侧分类 + 右侧缩略图卡片网格 + 全屏灯箱预览）
        var rootRow = new VisualElement();
        rootRow.style.flexDirection = FlexDirection.Row;
        rootRow.style.flexGrow = 1;
        win.Add(rootRow);

        // 左侧分类导航
        var sideNav = new VisualElement();
        sideNav.style.width = 180;
        sideNav.style.flexDirection = FlexDirection.Column;
        sideNav.style.marginRight = 18;
        rootRow.Add(sideNav);

        string[] cats = { "全部相片", "历史车站", "暮色与晨曦", "车厢与机库" };
        var catBtns = new System.Collections.Generic.List<Button>();
        for (int i = 0; i < cats.Length; i++)
        {
            int catIdx = i;
            var btn = new Button(() =>
            {
                currentGalleryCategory = catIdx;
                BuildGalleryApp(win);
            }) { text = cats[i] };
            btn.style.height = 40;
            btn.style.fontSize = 14; btn.style.unityFontDefinition = Fd();
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            btn.style.paddingLeft = 14;
            btn.style.backgroundColor = (i == currentGalleryCategory) ? BrownWarm : GlassBgSoft;
            btn.style.color = (i == currentGalleryCategory) ? GoldTrimColor : GlassText;
            btn.style.borderTopLeftRadius = 10; btn.style.borderBottomRightRadius = 10;
            btn.style.marginBottom = 8;
            sideNav.Add(btn);
            catBtns.Add(btn);
        }

        // 动态统计已收录场景数量
        int unlockedCount = 0;
        foreach (var item in MasterGalleryItems)
        {
            if (IsGalleryPhotoUnlocked(item.resId)) unlockedCount++;
        }

        var counter = new Label($"已收录场景：{unlockedCount} / {MasterGalleryItems.Length} 张");
        counter.style.fontSize = 12; counter.style.color = RicePaperLight; counter.style.unityFontDefinition = Fd();
        counter.style.marginTop = 12;
        sideNav.Add(counter);

        // 右侧网格（ScrollView滚动）
        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        rootRow.Add(scroll);

        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        scroll.Add(grid);

        foreach (var item in MasterGalleryItems)
        {
            // 分类过滤
            if (currentGalleryCategory > 0 && item.cat != currentGalleryCategory) continue;

            bool isUnlocked = IsGalleryPhotoUnlocked(item.resId);

            var card = new VisualElement();
            card.style.width = 240; card.style.height = 150;
            card.style.marginRight = 14; card.style.marginBottom = 14;
            card.style.borderTopLeftRadius = 14; card.style.borderBottomRightRadius = 14;
            card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor = isUnlocked ? AmberAccent : GlassBorderSoft;
            card.style.borderBottomColor = isUnlocked ? AmberAccent : GlassBorderSoft;
            card.style.borderLeftColor = isUnlocked ? AmberAccent : GlassBorderSoft;
            card.style.borderRightColor = isUnlocked ? AmberAccent : GlassBorderSoft;
            card.style.backgroundColor = isUnlocked ? BrownDark : new Color(0.10f, 0.08f, 0.08f, 0.85f);
            card.style.overflow = Overflow.Hidden;
            card.style.justifyContent = Justify.FlexEnd;
            card.pickingMode = PickingMode.Position;
            grid.Add(card);

            if (isUnlocked)
            {
                var sp = Resources.Load<Sprite>("bg/" + item.resId);
                if (sp != null)
                {
                    var img = new Image { sprite = sp };
                    img.style.position = Position.Absolute;
                    img.style.top = 0; img.style.left = 0; img.style.right = 0; img.style.bottom = 0;
                    img.scaleMode = ScaleMode.ScaleAndCrop;
                    card.Add(img);
                }
            }
            else
            {
                // 未解锁锁态占位
                var lockBox = new VisualElement();
                lockBox.style.position = Position.Absolute;
                lockBox.style.top = 0; lockBox.style.left = 0; lockBox.style.right = 0; lockBox.style.bottom = 42;
                lockBox.style.alignItems = Align.Center;
                lockBox.style.justifyContent = Justify.Center;
                card.Add(lockBox);

                var lockIcon = new Label("🔒");
                lockIcon.style.fontSize = 28;
                lockBox.Add(lockIcon);

                var lockHint = new Label("通关剧情解锁");
                lockHint.style.fontSize = 11; lockHint.style.color = GlassTextMuted;
                lockHint.style.unityFontDefinition = Fd();
                lockHint.style.marginTop = 4;
                lockBox.Add(lockHint);
            }

            var bottomBar = new VisualElement();
            bottomBar.style.height = 42;
            bottomBar.style.backgroundColor = new Color(0.06f, 0.04f, 0.04f, 0.85f);
            bottomBar.style.paddingLeft = 10; bottomBar.style.paddingRight = 10;
            bottomBar.style.justifyContent = Justify.Center;
            card.Add(bottomBar);

            var cardTitle = new Label(isUnlocked ? item.title : "？？？");
            cardTitle.style.fontSize = 12;
            cardTitle.style.color = isUnlocked ? GoldTrimColor : GlassTextMuted;
            cardTitle.style.unityFontDefinition = Fd();
            bottomBar.Add(cardTitle);

            var cardSub = new Label(isUnlocked ? "点击查看大图" : "推进剧情解锁");
            cardSub.style.fontSize = 10;
            cardSub.style.color = isUnlocked ? RicePaperLight : GlassTextMuted;
            cardSub.style.unityFontDefinition = Fd();
            bottomBar.Add(cardSub);

            if (isUnlocked)
            {
                card.RegisterCallback<PointerEnterEvent>(_ => card.style.scale = new Scale(new Vector2(1.04f, 1.04f)));
                card.RegisterCallback<PointerLeaveEvent>(_ => card.style.scale = new Scale(new Vector2(1f, 1f)));

                string targetResId = item.resId;
                string targetTitle = item.title;
                string targetDesc = item.desc;
                card.RegisterCallback<ClickEvent>(_ => ShowGalleryLightbox(targetResId, targetTitle, targetDesc));
            }
            else
            {
                card.RegisterCallback<ClickEvent>(_ => ShowBackupToast("该回忆场景尚未解锁，请在「归途」中推进剧情。"));
            }
        }
    }

    private void ShowGalleryLightbox(string resId, string title, string desc)
    {
        var lightbox = new VisualElement { name = "gallery-lightbox" };
        lightbox.style.position = Position.Absolute;
        lightbox.style.top = 0; lightbox.style.left = 0; lightbox.style.right = 0; lightbox.style.bottom = 0;
        lightbox.style.backgroundColor = new Color(0f, 0f, 0f, 0.88f);
        lightbox.style.alignItems = Align.Center;
        lightbox.style.justifyContent = Justify.Center;
        appLayer.Add(lightbox);

        lightbox.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == lightbox) appLayer.Remove(lightbox);
        });

        var sp = Resources.Load<Sprite>("bg/" + resId);
        if (sp != null)
        {
            var bigImg = new Image { sprite = sp };
            bigImg.style.width = 760; bigImg.style.height = 430;
            bigImg.scaleMode = ScaleMode.ScaleToFit;
            bigImg.style.borderTopLeftRadius = 14; bigImg.style.borderTopRightRadius = 14;
            bigImg.style.borderBottomLeftRadius = 14; bigImg.style.borderBottomRightRadius = 14;
            lightbox.Add(bigImg);
        }

        var infoCard = new VisualElement();
        infoCard.style.width = 760;
        infoCard.style.backgroundColor = new Color(0.1f, 0.1f, 0.14f, 0.9f);
        infoCard.style.borderTopLeftRadius = 12; infoCard.style.borderTopRightRadius = 12;
        infoCard.style.borderBottomLeftRadius = 12; infoCard.style.borderBottomRightRadius = 12;
        infoCard.style.paddingLeft = 18; infoCard.style.paddingRight = 18;
        infoCard.style.paddingTop = 10; infoCard.style.paddingBottom = 10;
        infoCard.style.marginTop = 12;
        infoCard.style.flexDirection = FlexDirection.Row;
        infoCard.style.justifyContent = Justify.SpaceBetween;
        infoCard.style.alignItems = Align.Center;
        lightbox.Add(infoCard);

        var textCol = new VisualElement();
        infoCard.Add(textCol);

        var t = new Label(title);
        t.style.fontSize = 16; t.style.color = GlassAccent; t.style.unityFontDefinition = Fd();
        textCol.Add(t);

        var d = new Label(desc);
        d.style.fontSize = 12; d.style.color = GlassTextDim; d.style.unityFontDefinition = Fd();
        d.style.marginTop = 2;
        textCol.Add(d);

        var close = new Button(() => appLayer.Remove(lightbox)) { text = "关闭预览" };
        close.style.height = 32; close.style.paddingLeft = 14; close.style.paddingRight = 14;
        close.style.fontSize = 12; close.style.unityFontDefinition = Fd();
        close.style.backgroundColor = new Color(0.25f, 0.25f, 0.32f, 0.7f);
        close.style.color = GlassText;
        close.style.borderTopLeftRadius = 8; close.style.borderTopRightRadius = 8;
        close.style.borderBottomLeftRadius = 8; close.style.borderBottomRightRadius = 8;
        infoCard.Add(close);
    }

    // ============ 统一 App 浅色卡片骨架（T298 规范）============
    private VisualElement BuildAppBody(VisualElement win, string pageTitle, string pageSub = null)
    {
        win.Clear();
        var bodyScroll = new ScrollView();
        bodyScroll.style.flexGrow = 1;
        bodyScroll.style.paddingLeft = 24; bodyScroll.style.paddingRight = 24;
        bodyScroll.style.paddingTop = 16; bodyScroll.style.paddingBottom = 24;
        bodyScroll.style.backgroundColor = SurfaceApp;
        win.Add(bodyScroll);

        if (!string.IsNullOrEmpty(pageTitle))
        {
            var titleBox = new VisualElement();
            titleBox.style.marginBottom = 16;
            bodyScroll.Add(titleBox);

            var t = new Label(pageTitle);
            t.style.fontSize = 22; t.style.color = InkPrimary;
            t.style.unityFontStyleAndWeight = FontStyle.Bold;
            t.style.unityFontDefinition = Fd();
            titleBox.Add(t);

            if (!string.IsNullOrEmpty(pageSub))
            {
                var s = new Label(pageSub);
                s.style.fontSize = 13; s.style.color = InkSecondary;
                s.style.unityFontDefinition = Fd();
                s.style.marginTop = 4;
                titleBox.Add(s);
            }
        }
        return bodyScroll;
    }

    private VisualElement BuildAppCard(string header = null)
    {
        var card = new VisualElement();
        card.style.backgroundColor = SurfaceCard;
        card.style.borderTopLeftRadius = 14; card.style.borderTopRightRadius = 14;
        card.style.borderBottomLeftRadius = 14; card.style.borderBottomRightRadius = 14;
        card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
        card.style.borderTopColor = BorderCard; card.style.borderBottomColor = BorderCard;
        card.style.borderLeftColor = BorderCard; card.style.borderRightColor = BorderCard;
        card.style.paddingLeft = 16; card.style.paddingRight = 16;
        card.style.paddingTop = 14; card.style.paddingBottom = 14;
        card.style.marginBottom = 14;

        if (!string.IsNullOrEmpty(header))
        {
            var h = new Label(header);
            h.style.fontSize = 14; h.style.color = SectionHeaderColor;
            h.style.unityFontStyleAndWeight = FontStyle.Bold;
            h.style.unityFontDefinition = Fd();
            h.style.marginBottom = 10;
            card.Add(h);
        }
        return card;
    }

    private VisualElement BuildLightRow(string title, string desc = null, System.Action onClick = null, bool hasArrow = true)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingLeft = 14; row.style.paddingRight = 14;
        row.style.paddingTop = 12; row.style.paddingBottom = 12;
        row.style.marginBottom = 4;
        row.style.borderTopLeftRadius = 8; row.style.borderTopRightRadius = 8;
        row.style.borderBottomLeftRadius = 8; row.style.borderBottomRightRadius = 8;
        row.pickingMode = onClick != null ? PickingMode.Position : PickingMode.Ignore;

        if (onClick != null)
        {
            row.RegisterCallback<PointerEnterEvent>(_ => row.style.backgroundColor = SurfaceHover);
            row.RegisterCallback<PointerLeaveEvent>(_ => row.style.backgroundColor = Color.clear);
            row.RegisterCallback<ClickEvent>(_ => onClick());
        }

        var left = new VisualElement();
        left.style.flexGrow = 1;
        row.Add(left);

        var t = new Label(title);
        t.style.fontSize = 15; t.style.color = InkPrimary;
        t.style.unityFontDefinition = Fd();
        left.Add(t);

        if (!string.IsNullOrEmpty(desc))
        {
            var d = new Label(desc);
            d.style.fontSize = 12; d.style.color = InkSecondary;
            d.style.unityFontDefinition = Fd();
            d.style.marginTop = 2;
            left.Add(d);
        }

        if (hasArrow)
        {
            var arrow = new Label("›");
            arrow.style.fontSize = 18; arrow.style.color = InkTertiary;
            arrow.style.unityFontDefinition = Fd();
            row.Add(arrow);
        }

        return row;
    }

    // ============ App: 时钟 ============
    private void BuildClockApp(VisualElement win)
    {
        var body = BuildAppBody(win, "时钟", "阿里郎高精度授时中心 · 运行授时服务");

        // 大表盘卡片
        var clockCard = BuildAppCard();
        clockCard.style.alignItems = Align.Center;
        clockCard.style.paddingTop = 24; clockCard.style.paddingBottom = 24;

        var time = new Label(System.DateTime.Now.ToString("HH:mm:ss"));
        time.style.fontSize = 54; time.style.color = InkPrimary;
        time.style.unityFontStyleAndWeight = FontStyle.Bold;
        time.style.unityFontDefinition = Fd();
        clockCard.Add(time);

        var date = new Label($"运营第 {GameData.Day} 天 · 主体历 105 年 1 月 1 日");
        date.style.fontSize = 14; date.style.color = AccentSystem;
        date.style.unityFontDefinition = Fd(); date.style.marginTop = 8;
        clockCard.Add(date);
        body.Add(clockCard);

        // 分组卡片：功能Tab（闹钟/世界时钟/秒表/倒计时）
        var funcCard = BuildAppCard("时钟功能");
        funcCard.Add(BuildLightRow("⏰ 晨间出车早擎闹钟", "已设定 05:30（工作日）", () => ShowBackupToast("闹钟已同步到先民排班系统")));
        funcCard.Add(BuildLightRow("🌐 平壤内网主时钟同步", "偏移量 +0.002s · 阿里郎1号卫星校准", () => ShowBackupToast("授时服务器连接稳定")));
        funcCard.Add(BuildLightRow("⏱ 运行耗时秒表", "用于测定雾峰站至茶山站实际单程区间时分", () => ShowBackupToast("秒表功能就绪")));
        body.Add(funcCard);
    }

    // ============ App: 日历 ============
    private void BuildCalendarApp(VisualElement win)
    {
        var body = BuildAppBody(win, "日历", "主体历 105 年 1 月（公历 2076 年 1 月）");

        var calCard = BuildAppCard("月度运行视图");
        calCard.style.alignSelf = Align.Center;
        calCard.style.width = 680;

        string[] days = { "一", "二", "三", "四", "五", "六", "日" };
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.marginBottom = 10;
        calCard.Add(header);
        foreach (var d in days)
        {
            var dl = new Label(d);
            dl.style.width = 85; dl.style.fontSize = 13; dl.style.color = InkSecondary;
            dl.style.unityFontDefinition = Fd(); dl.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(dl);
        }

        // 5周网格，完整呈现 31 天
        for (int w = 0; w < 5; w++)
        {
            var week = new VisualElement();
            week.style.flexDirection = FlexDirection.Row;
            week.style.justifyContent = Justify.SpaceBetween;
            week.style.marginTop = 6;
            calCard.Add(week);
            for (int d = 0; d < 7; d++)
            {
                int day = w * 7 + d + 1;
                var dl = new Label(day <= 31 ? day.ToString() : "");
                dl.style.width = 85; dl.style.height = 38; dl.style.fontSize = 14;
                dl.style.unityFontDefinition = Fd(); dl.style.unityTextAlign = TextAnchor.MiddleCenter;

                if (day <= 31)
                {
                    bool isCurrentDay = (day == GameData.Day || (GameData.Day > 31 && day == 1));
                    dl.style.color = isCurrentDay ? Color.white : InkPrimary;
                    if (isCurrentDay)
                    {
                        dl.style.backgroundColor = AccentSystem;
                        dl.style.borderTopLeftRadius = 8; dl.style.borderTopRightRadius = 8;
                        dl.style.borderBottomLeftRadius = 8; dl.style.borderBottomRightRadius = 8;
                    }
                }
                else
                {
                    dl.style.color = Color.clear;
                }
                week.Add(dl);
            }
        }
        body.Add(calCard);

        var scheduleCard = BuildAppCard("今日排程备忘");
        scheduleCard.Add(BuildLightRow("08:00 晨间第一班客运列车出库", "线路巡道工已完成万能道尺轨距巡检", null, false));
        scheduleCard.Add(BuildLightRow("12:30 乌金岭矿区沙币结算单核验", "嘉颖徐铁路基金派员进驻", null, false));
        body.Add(scheduleCard);
    }

    // ============ App: 天气 ============
    private void BuildWeatherApp(VisualElement win)
    {
        var body = BuildAppBody(win, "气象监视", "雾峰村气象站 · 实时气象与水文参数");

        var weatherCard = BuildAppCard();
        weatherCard.style.alignItems = Align.Center;
        weatherCard.style.paddingTop = 20; weatherCard.style.paddingBottom = 20;

        var temp = new Label("18°C");
        temp.style.fontSize = 54; temp.style.color = InkPrimary;
        temp.style.unityFontStyleAndWeight = FontStyle.Bold;
        temp.style.unityFontDefinition = Fd();
        weatherCard.Add(temp);

        var cond = new Label("☁ 多云 · 东北风 2级 · 湿度 62%");
        cond.style.fontSize = 15; cond.style.color = InkSecondary;
        cond.style.unityFontDefinition = Fd(); cond.style.marginTop = 6;
        weatherCard.Add(cond);
        body.Add(weatherCard);

        var impactCard = BuildAppCard("铁路运营与沙能影响评估");
        impactCard.Add(BuildLightRow("轨面附着系数", "良好 · 干燥无沙尘暴影响", null, false));
        impactCard.Add(BuildLightRow("沙能载具影响", "沙粒流转阻力 0.12 · 竞争对手运力平稳", null, false));
        impactCard.Add(BuildLightRow("未来24小时预警", "明日午后有薄雾，建议提早开启0721机车大灯", null, false));
        body.Add(impactCard);

        var forecastCard = BuildAppCard("多日天气预报");
        forecastCard.Add(BuildLightRow("周一（今日）", "多云 18°C / 9°C · 优", null, false));
        forecastCard.Add(BuildLightRow("周二（明日）", "晴间多云 21°C / 11°C · 优", null, false));
        forecastCard.Add(BuildLightRow("周三（后日）", "小雨转阴 15°C / 8°C · 轨面湿滑注意制动", null, false));
        body.Add(forecastCard);
    }

    // ============ App: 计算器 ============
    private void BuildCalculatorApp(VisualElement win)
    {
        var body = BuildAppBody(win, "运价计算器", "客货运里程运价核算与沙币换算");

        var card = BuildAppCard();
        card.style.width = 420; card.style.alignSelf = Align.Center;

        var display = new Label("0");
        display.style.fontSize = 36; display.style.color = InkPrimary;
        display.style.unityFontStyleAndWeight = FontStyle.Bold;
        display.style.unityFontDefinition = Fd();
        display.style.height = 54; display.style.unityTextAlign = TextAnchor.MiddleRight;
        display.style.paddingRight = 10;
        display.style.marginBottom = 12;
        display.style.backgroundColor = SurfaceApp;
        display.style.borderTopLeftRadius = 8; display.style.borderTopRightRadius = 8;
        display.style.borderBottomLeftRadius = 8; display.style.borderBottomRightRadius = 8;
        card.Add(display);

        string[][] rows = {
            new[]{"7","8","9","÷"},
            new[]{"4","5","6","×"},
            new[]{"1","2","3","-"},
            new[]{"0",".","=","+"},
        };
        foreach (var row in rows)
        {
            var btnRow = new VisualElement(); btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.marginTop = 6; btnRow.style.justifyContent = Justify.SpaceBetween;
            card.Add(btnRow);
            foreach (var b in row)
            {
                var btn = new Button(() => { }) { text = b };
                btn.style.width = 88; btn.style.height = 46; btn.style.fontSize = 18;
                btn.style.unityTextAlign = TextAnchor.MiddleCenter; btn.style.unityFontDefinition = Fd();
                btn.style.backgroundColor = b == "=" ? AccentSystem : SurfaceHover;
                btn.style.color = b == "=" ? Color.white : InkPrimary;
                btn.style.borderTopLeftRadius = 8; btn.style.borderTopRightRadius = 8;
                btn.style.borderBottomLeftRadius = 8; btn.style.borderBottomRightRadius = 8;
                btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
                btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
                btnRow.Add(btn);
            }
        }
        body.Add(card);
    }

    // ============ App: 米家（0721遥测）============
    private void BuildMijiaApp(VisualElement win)
    {
        var body = BuildAppBody(win, "机车遥测", "米家物联网 · 0721型「沙子飞猪号」机车网关");

        var statusCard = BuildAppCard("动力单元遥测总览");
        statusCard.Add(BuildLightRow("沙能储箱余量", "1,200 / 1,500 kg (80%)", null, false));
        statusCard.Add(BuildLightRow("动力电池组蓄电量", "78% · 正常放电", null, false));
        statusCard.Add(BuildLightRow("预估安全运行里程", "340 km", null, false));
        statusCard.Add(BuildLightRow("本月累计沙耗", "312 kg 先民高纯度动力沙", null, false));
        body.Add(statusCard);

        var deviceCard = BuildAppCard("机载智能节点");
        deviceCard.Add(BuildLightRow("万能道尺无线遥测模块", "已连接 · 精度 ±0.5mm", () => ShowBackupToast("道尺信号正常")));
        deviceCard.Add(BuildLightRow("机车行车记录仪", "循环录像中 · 存储剩余 18.4GB", () => ShowBackupToast("录像文件保护已开启")));
        deviceCard.Add(BuildLightRow("轴承振动监视传感器", "全轴正常 · 无偏磨", () => ShowBackupToast("机修工张工巡检记录已同步")));
        body.Add(deviceCard);
    }

    // ============ App: 安全中心 ============
    private void BuildSecurityApp(VisualElement win)
    {
        var body = BuildAppBody(win, "安全中心", "阿里郎终端安全与内网防护系统");

        var scoreCard = BuildAppCard();
        scoreCard.style.alignItems = Align.Center;
        scoreCard.style.paddingTop = 16; scoreCard.style.paddingBottom = 16;

        var shield = new Label("🛡");
        shield.style.fontSize = 44; shield.style.unityFontDefinition = Fd();
        scoreCard.Add(shield);

        var score = new Label("100 分 · 系统处于安全状态");
        score.style.fontSize = 18; score.style.color = new Color(0.2f, 0.65f, 0.3f, 1f);
        score.style.unityFontStyleAndWeight = FontStyle.Bold;
        score.style.unityFontDefinition = Fd(); score.style.marginTop = 6;
        scoreCard.Add(score);

        var scanBtn = new Button(() => ShowBackupToast("系统已全面扫描：未发现安全漏洞")) { text = "一键优化与垃圾清理" };
        scanBtn.style.marginTop = 12; scanBtn.style.height = 36;
        scanBtn.style.paddingLeft = 18; scanBtn.style.paddingRight = 18;
        scanBtn.style.backgroundColor = AccentSystem; scanBtn.style.color = Color.white;
        scanBtn.style.borderTopLeftRadius = 18; scanBtn.style.borderTopRightRadius = 18;
        scanBtn.style.borderBottomLeftRadius = 18; scanBtn.style.borderBottomRightRadius = 18;
        scanBtn.style.fontSize = 14; scanBtn.style.unityFontDefinition = Fd();
        scoreCard.Add(scanBtn);
        body.Add(scoreCard);

        var listCard = BuildAppCard("安全防御组件");
        listCard.Add(BuildLightRow("病毒与木马查杀", "病毒库版本：2076.01.10 最新", () => ShowBackupToast("病毒库为最新版")));
        listCard.Add(BuildLightRow("应用权限防护", "22 个应用已通过安全沙箱审核", () => ShowBackupToast("权限配置安全")));
        listCard.Add(BuildLightRow("骚扰拦截与通信防火墙", "外部未知频段电波已被屏蔽", () => ShowBackupToast("防火墙运行中")));
        body.Add(listCard);
    }

    // ============ App: 浏览器 ============
    private void BuildBrowserApp(VisualElement win)
    {
        var body = BuildAppBody(win, "光明浏览器", "阿里郎光明内网 · 铁路文库与门户");

        var searchCard = BuildAppCard();
        var searchRow = new VisualElement();
        searchRow.style.flexDirection = FlexDirection.Row;
        searchRow.style.alignItems = Align.Center;
        searchRow.style.backgroundColor = SurfaceApp;
        searchRow.style.paddingLeft = 12; searchRow.style.paddingRight = 12;
        searchRow.style.height = 38;
        searchRow.style.borderTopLeftRadius = 19; searchRow.style.borderTopRightRadius = 19;
        searchRow.style.borderBottomLeftRadius = 19; searchRow.style.borderBottomRightRadius = 19;
        searchCard.Add(searchRow);

        var searchTxt = new Label("🔒 https://jiayingxu.railway-foundation.org");
        searchTxt.style.fontSize = 13; searchTxt.style.color = InkSecondary; searchTxt.style.unityFontDefinition = Fd();
        searchRow.Add(searchTxt);
        body.Add(searchCard);

        var homeCard = BuildAppCard("推荐门户与馆藏");
        homeCard.Add(BuildLightRow("🏛 嘉颖徐铁路基金官方站点", "全球铁路文化遗产与沙能冲击研究论文库", () => ShowBackupToast("已连接嘉颖徐基金知识库")));
        homeCard.Add(BuildLightRow("📖 金日成综合大学铁道系期刊", "0721型机车先民内燃机图纸与热效率分析", () => ShowBackupToast("论文文献已加载")));
        homeCard.Add(BuildLightRow("🗺 雾峰至乌金岭矿区综合勘察志", "地质构造、废弃线路复旧方案纪实", () => ShowBackupToast("勘察志阅读就绪")));
        body.Add(homeCard);
    }

    // ============ App: 应用商店 ============
    private void BuildAppStoreApp(VisualElement win)
    {
        var body = BuildAppBody(win, "应用市场", "精选工具与经过审计的应用");

        var listCard = BuildAppCard("已上架应用");
        listCard.Add(BuildLightRow("铁路工程计算器 v2.1", "已安装 · 专用于道岔曲率与轨距计算", () => ShowBackupToast("应用已是最新版")));
        listCard.Add(BuildLightRow("列车时刻表 v1.5", "已安装 · 雾峰村线专用发车排班表", () => ShowBackupToast("应用已是最新版")));
        listCard.Add(BuildLightRow("轨道检测工具 v3.0", "已安装 · 万能道尺蓝牙配对应用", () => ShowBackupToast("应用已是最新版")));
        listCard.Add(BuildLightRow("铁路英语词典 v1.0", "已安装 · 先民机车术语英汉对照", () => ShowBackupToast("应用已是最新版")));
        body.Add(listCard);
    }

    // ============ App: 阿里郎商店 ============
    private void BuildArirangStoreApp(VisualElement win)
    {
        var body = BuildAppBody(win, "阿里郎商店", "国营物资订购与官方数字签名应用市场");

        var listCard = BuildAppCard("认证应用列表");
        listCard.Add(BuildLightRow("阿里郎新闻 v1.0", "官方媒体每日通报 · ★★★★★", () => ShowBackupToast("已核验数字证书")));
        listCard.Add(BuildLightRow("主体百科 v2.0", "先民百科与工农知识库 · ★★★★★", () => ShowBackupToast("已核验数字证书")));
        listCard.Add(BuildLightRow("朝鲜音乐 v1.3", "革命进行曲与铁路工人联欢曲 · ★★★★★", () => ShowBackupToast("已核验数字证书")));
        listCard.Add(BuildLightRow("白头教育 v3.0", "铁路工程技术培训与考试系统 · ★★★★★", () => ShowBackupToast("已核验数字证书")));
        body.Add(listCard);
    }

    // ============ App: 未来网 ============
    private void BuildMiraeNetApp(VisualElement win)
    {
        var body = BuildAppBody(win, "未来网", "mirae.kp · 科教文卫国家内网接入端");

        var listCard = BuildAppCard("国家网络节点");
        listCard.Add(BuildLightRow("金日成综合大学 · 校园网接入", "林彪悍的研究生学术档案与导师信箱", () => ShowBackupToast("导师信箱当前无新邮件")));
        listCard.Add(BuildLightRow("沙能会社门户 · USET企业网", "沙本位能源每日挂牌交易牌价与行情", () => ShowBackupToast("沙币牌价挂牌中")));
        listCard.Add(BuildLightRow("平壤铁道省技术总局", "铁路支线特许经营准入核验系统", () => ShowBackupToast("特许准入证书有效")));
        body.Add(listCard);
    }

    // ============ App: 白头疫苗 ============
    private void BuildSiliVaccineApp(VisualElement win)
    {
        var body = BuildAppBody(win, "白头疫苗", "内网终极防御系统 · 驱动级沙盒引擎");

        var statCard = BuildAppCard("防护引擎状态");
        statCard.Add(BuildLightRow("防护模式", "实时防御已开启（锁定不可关闭）", null, false));
        statCard.Add(BuildLightRow("特征码库大小", "0 KB（内网完全免疫未知外来攻击）", null, false));
        statCard.Add(BuildLightRow("扫描引擎校验", "通过 · 阿里郎OS系统级深度挂钩", () => ShowBackupToast("防护引擎运转正常")));
        body.Add(statCard);
    }

    // ============ App: 文件管理 ============
    private void BuildFileManagerApp(VisualElement win)
    {
        var body = BuildAppBody(win, "文件管理", "内部存储空间 128GB · 剩余可用 92.4GB");

        var catCard = BuildAppCard("文件分类");
        catCard.Add(BuildLightRow("📁 文档 (12 项)", "包含站务日志、道尺检修表与班组排班表", () => ShowBackupToast("文档目录就绪")));
        catCard.Add(BuildLightRow("🖼 图片 (38 项)", "车站风景、通车剪彩与相册剧照", () => ShowBackupToast("图片目录就绪")));
        catCard.Add(BuildLightRow("🎵 音频 (8 项)", "机车鸣笛声、铁道广播与随身听曲目", () => ShowBackupToast("音频目录就绪")));
        catCard.Add(BuildLightRow("💾 存档 (6 槽位)", "本地游戏进度快照与剧情回顾标记", () => ShowBackupToast("存档目录正常")));
        body.Add(catCard);
    }

    // ============ App: 地图 ============
    private void BuildMapApp(VisualElement win)
    {
        var body = BuildAppBody(win, "铁路地图", "雾峰支线与周边干线走向规划图");

        var mapCard = BuildAppCard("铁路线路总览");
        var map = new Label(
            "  雾峰村 ─── 8km ─── 茶山站 ─── 7km ─── 松桥站 ─── 8km ─── 矿区站\n" +
            "  │\n" +
            "  └── 45km ─── 青溪镇（规划中）\n" +
            "       └── 80km ─── 云渡港（待解锁）");
        map.style.fontSize = 13; map.style.color = InkPrimary;
        map.style.unityFontDefinition = Fd();
        map.style.whiteSpace = WhiteSpace.Pre;
        map.style.paddingTop = 10; map.style.paddingBottom = 10;
        mapCard.Add(map);
        body.Add(mapCard);

        var infoCard = BuildAppCard("区域沙能渗透监视");
        infoCard.Add(BuildLightRow("雾峰村", "沙能渗透率 15% · 传统铁路基本盘坚固", null, false));
        infoCard.Add(BuildLightRow("乌金岭矿区", "沙能渗透率 20% · 矿商倾向重载货运", null, false));
        infoCard.Add(BuildLightRow("青溪镇", "沙能渗透率 25% · 沙能公路客运竞争激烈", null, false));
        body.Add(infoCard);
    }

    // ============ App: 占位符 ============
    private void BuildPlaceholderApp(VisualElement win, string name)
    {
        var body = BuildAppBody(win, name, "系统官方规划应用");
        var card = BuildAppCard();
        card.style.alignItems = Align.Center;
        card.style.paddingTop = 32; card.style.paddingBottom = 32;

        var icon = new Label("⏳");
        icon.style.fontSize = 40; icon.style.unityFontDefinition = Fd();
        card.Add(icon);

        var t = new Label($"{name} · 即将推出");
        t.style.fontSize = 18; t.style.color = InkPrimary;
        t.style.unityFontStyleAndWeight = FontStyle.Bold;
        t.style.unityFontDefinition = Fd(); t.style.marginTop = 10;
        card.Add(t);

        var sub = new Label("该模块正由阿里郎工程师适配中，请随着经营天数推进解锁。");
        sub.style.fontSize = 13; sub.style.color = InkSecondary;
        sub.style.unityFontDefinition = Fd(); sub.style.marginTop = 6;
        card.Add(sub);

        body.Add(card);
    }

    private VisualElement BuildInnerPanel(string title)
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

        // 随身听音乐播放器进度同步
        if (tabletIsPlaying && tabletMusicSource != null && tabletCurrentClip != null)
        {
            tabletMusicProgressTimer += Time.unscaledDeltaTime;
            if (tabletMusicProgressTimer >= 0.1f)
            {
                tabletMusicProgressTimer = 0f;
                float curTime = tabletMusicSource.time;
                float totalLen = tabletCurrentClip.length;
                if (!tabletProgressScrubbing && totalLen > 0.1f)
                {
                    float pct = Mathf.Clamp01(curTime / totalLen) * 100f;
                    if (tabletProgressFill != null) tabletProgressFill.style.width = Length.Percent(pct);
                    if (tabletProgressKnob != null) tabletProgressKnob.style.left = Length.Percent(pct);
                }
                if (tabletCurTimeLbl != null) tabletCurTimeLbl.text = FormatAudioTime(curTime);

                // 播放结束自动切下一首
                if (curTime >= totalLen - 0.2f && totalLen > 0.1f)
                {
                    if (tabletPlayMode == TabletPlayMode.SingleRepeat)
                    {
                        tabletMusicSource.time = 0;
                        tabletMusicSource.Play();
                    }
                    else
                    {
                        PlayNextTabletTrack();
                    }
                }
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