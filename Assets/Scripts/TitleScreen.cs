using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.IO;

[RequireComponent(typeof(UIDocument))]
public class TitleScreen : MonoBehaviour
{
    private Label usernameLabel;
    private Button btnNewGame;
    private Button btnContinue;
    private Button btnArchive;
    private Button btnSettings;
    private Button btnExit;
    private Button announcementBtn;
    private VideoPlayer videoPlayer;
    private NewGameSetupUI newGameSetup;
    private TitleArchiveUI archiveUI;
    private StationBulletinUI bulletinUI;

    private readonly Color glassBg = new Color(40f/255f, 25f/255f, 15f/255f, 0.35f);
    private readonly Color glassBgHover = new Color(40f/255f, 25f/255f, 15f/255f, 0.55f);
    private readonly Color glassBgPress = new Color(40f/255f, 25f/255f, 15f/255f, 0.70f);
    private readonly Color borderNormal = new Color(1f, 220f/255f, 150f/255f, 0.55f);
    private readonly Color borderHover = new Color(1f, 230f/255f, 170f/255f, 0.85f);
    private readonly Color textNormal = new Color(1f, 215f/255f, 0f, 0.95f);
    private readonly Color textHover = new Color(1f, 230f/255f, 100f/255f, 1f);
    private static Texture2D handCursor;
    private Texture2D arrowCursor;

    void Start()
    {
        SetupVideoBackground();
        SetupBGM();
        SetupGameCursor();
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0.55f, 0.38f, 0.32f, 1f);
        Camera.main.transform.position = new Vector3(0f, 0f, -10f);

        var root = GetComponent<UIDocument>().rootVisualElement;

        var styleSheet = Resources.Load<StyleSheet>("UI/TitleScreenStyles");
        if (styleSheet != null) root.styleSheets.Add(styleSheet);

        LoadAvatarImages(root);

        usernameLabel = root.Q<Label>("username");
        btnNewGame = root.Q<Button>("btn-new-game");
        btnContinue = root.Q<Button>("btn-continue");
        btnArchive = root.Q<Button>("btn-archive");
        btnSettings = root.Q<Button>("btn-settings");
        btnExit = root.Q<Button>("btn-exit");
        announcementBtn = root.Q<Button>("announcement-btn");

        SetupGlassButton(btnNewGame);
        SetupGlassButton(btnContinue);
        SetupGlassButton(btnArchive);
        SetupGlassButton(btnSettings);
        SetupGlassButton(btnExit);

        if (btnNewGame != null) btnNewGame.clicked += OnNewGame;
        if (btnContinue != null) btnContinue.clicked += OnContinue;
        if (btnArchive != null) btnArchive.clicked += OnArchive;
        if (btnSettings != null) btnSettings.clicked += OnSettings;
        if (btnExit != null) btnExit.clicked += OnExit;
        if (announcementBtn != null)
        {
            announcementBtn.RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (handCursor != null)
                    UnityEngine.Cursor.SetCursor(handCursor, new Vector2(0, 0), UnityEngine.CursorMode.ForceSoftware);
                announcementBtn.style.borderTopColor = borderHover;
                announcementBtn.style.borderRightColor = borderHover;
                announcementBtn.style.borderBottomColor = borderHover;
                announcementBtn.style.borderLeftColor = borderHover;
            });
            announcementBtn.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                if (arrowCursor != null)
                    UnityEngine.Cursor.SetCursor(arrowCursor, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
                announcementBtn.style.borderTopColor = new Color(1f, 220f/255f, 150f/255f, 0.35f);
                announcementBtn.style.borderRightColor = new Color(1f, 220f/255f, 150f/255f, 0.35f);
                announcementBtn.style.borderBottomColor = new Color(1f, 220f/255f, 150f/255f, 0.35f);
                announcementBtn.style.borderLeftColor = new Color(1f, 220f/255f, 150f/255f, 0.35f);
            });
            announcementBtn.clicked += OnAnnouncement;
        }

        LoadUserInfo();
        CheckSaveData();

        newGameSetup = gameObject.AddComponent<NewGameSetupUI>();
        newGameSetup.Init(GetComponent<UIDocument>(), () =>
        {
            PlayerPrefs.SetInt("VN_AutoLoad", 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene("VN_Test");
        });

        archiveUI = gameObject.AddComponent<TitleArchiveUI>();
        archiveUI.Init(GetComponent<UIDocument>());

        bulletinUI = new GameObject("StationBulletinUI").AddComponent<StationBulletinUI>();
        bulletinUI.Init(GetComponent<UIDocument>());

        videoPlayer = FindAnyObjectByType<VideoPlayer>();
        AdaptQuadToScreen();
    }

    void AdaptQuadToScreen()
    {
        var videoBg = GameObject.Find("VideoBackground");
        if (videoBg == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        float visibleHeight = cam.orthographicSize * 2f;
        float visibleWidth = visibleHeight * (float)Screen.width / Screen.height;

        videoBg.transform.localScale = new Vector3(visibleWidth, visibleHeight, 1f);
        videoBg.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
    }

    void Update()
    {
        AdaptQuadToScreen();
    }

    void SetupGlassButton(Button btn)
    {
        if (btn == null) return;

        btn.RegisterCallback<PointerEnterEvent>(evt =>
        {
            if (btn.enabledSelf && handCursor != null)
                UnityEngine.Cursor.SetCursor(handCursor, new Vector2(0, 0), UnityEngine.CursorMode.ForceSoftware);
            btn.style.backgroundColor = glassBgHover;
            btn.style.borderTopColor = borderHover;
            btn.style.borderRightColor = borderHover;
            btn.style.borderBottomColor = borderHover;
            btn.style.borderLeftColor = borderHover;
            btn.style.color = textHover;
            btn.style.translate = new Translate(0, -2);
        });

        btn.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            if (arrowCursor != null)
                UnityEngine.Cursor.SetCursor(arrowCursor, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
            btn.style.backgroundColor = glassBg;
            btn.style.borderTopColor = borderNormal;
            btn.style.borderRightColor = borderNormal;
            btn.style.borderBottomColor = borderNormal;
            btn.style.borderLeftColor = borderNormal;
            btn.style.color = textNormal;
            btn.style.translate = new Translate(0, 0);
        });

        btn.RegisterCallback<MouseDownEvent>(evt =>
        {
            btn.style.backgroundColor = glassBgPress;
            btn.style.translate = new Translate(0, 0);
        });

        btn.RegisterCallback<MouseUpEvent>(evt =>
        {
            btn.style.backgroundColor = glassBgHover;
            btn.style.translate = new Translate(0, -2);
        });
    }

    void SetupBGM()
    {
        var existing = GameObject.Find("BGM");
        if (existing != null) return;

        var audioObj = new GameObject("BGM");
        audioObj.transform.SetParent(null);
        DontDestroyOnLoad(audioObj);
        var src = audioObj.AddComponent<AudioSource>();
        src.loop = true;
        src.volume = 0.3f;
        var clip = Resources.Load<AudioClip>("Audio/Train Through Keys");
        if (clip != null)
        {
            src.clip = clip;
            src.Play();
        }
    }

    void SetupGameCursor()
    {
        if (LoginManager.cursorTexture == null)
            LoginManager.cursorTexture = LoginManager.LoadCursorTexture("Cursors/cursor_arrow", 3);
        arrowCursor = LoginManager.cursorTexture;
        if (handCursor == null)
            handCursor = LoginManager.LoadCursorTexture("Cursors/cursor_hand", 3);
        UnityEngine.Cursor.SetCursor(arrowCursor, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
    }

    void LoadAvatarImages(VisualElement root)
    {
        var frame = root.Q<VisualElement>("avatar-frame");
        if (frame != null)
        {
            var tex = Resources.Load<Texture2D>("UI/AvatarFrame");
            if (tex != null) frame.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
        }

        var icon = root.Q<VisualElement>("avatar-icon");
        if (icon != null)
        {
            var tex = Resources.Load<Texture2D>("UI/DefaultAvatar");
            if (tex != null) icon.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
        }
    }

    void LoadUserInfo()
    {
        if (usernameLabel == null)
            usernameLabel = GetComponent<UIDocument>().rootVisualElement.Q<Label>("username");
        if (usernameLabel == null) return;

        string username = PlayerPrefs.GetString("Username", "");

        if (string.IsNullOrEmpty(username))
        {
            string authPath = Path.Combine(Application.persistentDataPath, "auth.json");
            if (File.Exists(authPath))
            {
                string json = File.ReadAllText(authPath);
                var auth = JsonUtility.FromJson<LoginManager.AuthData>(json);
                if (auth != null && !string.IsNullOrEmpty(auth.username))
                {
                    username = auth.username;
                    PlayerPrefs.SetString("Username", username);
                    PlayerPrefs.Save();
                }
            }
        }

        usernameLabel.text = string.IsNullOrEmpty(username) ? "未知用户" : username;
    }

    void CheckSaveData()
    {
        if (btnContinue == null) return;
        bool hasSaveData = false;
        for (int i = 0; i < 3; i++)
        {
            if (PlayerPrefs.HasKey("VN_Save_" + i)) { hasSaveData = true; break; }
        }
        if (!hasSaveData)
        {
            btnContinue.SetEnabled(false);
            btnContinue.style.backgroundColor = new Color(40f/255f, 25f/255f, 15f/255f, 0.15f);
            btnContinue.style.borderTopColor = new Color(1f, 220f/255f, 150f/255f, 0.15f);
            btnContinue.style.borderRightColor = new Color(1f, 220f/255f, 150f/255f, 0.15f);
            btnContinue.style.borderBottomColor = new Color(1f, 220f/255f, 150f/255f, 0.15f);
            btnContinue.style.borderLeftColor = new Color(1f, 220f/255f, 150f/255f, 0.15f);
            btnContinue.style.color = new Color(1f, 215f/255f, 0f, 0.3f);
        }
    }

    void OnNewGame()
    {
        newGameSetup?.Show();
    }

    void OnContinue()
    {
        PlayerPrefs.SetInt("VN_ShowLoadUI", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("VN_Test");
    }

    void OnArchive() { archiveUI?.Show(); }
    void OnSettings() { bulletinUI?.Show(); }
    void OnAnnouncement() { ShowNewsPanel(); }
    void OnExit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void ShowNewsPanel()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var gameFont = Resources.Load<Font>("Fonts/zpix");
        var fontDef = new FontDefinition { font = gameFont };

        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0; overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        overlay.RegisterCallback<ClickEvent>(e => { if (e.target == overlay) root.Remove(overlay); });
        root.Add(overlay);

        var panel = new VisualElement();
        panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.97f);
        panel.style.borderTopWidth = 2; panel.style.borderBottomWidth = 2;
        panel.style.borderLeftWidth = 2; panel.style.borderRightWidth = 2;
        panel.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderTopLeftRadius = 12; panel.style.borderTopRightRadius = 12;
        panel.style.borderBottomLeftRadius = 12; panel.style.borderBottomRightRadius = 12;
        panel.style.width = 600; panel.style.height = 500;
        panel.style.flexDirection = FlexDirection.Column;
        overlay.Add(panel);

        // Header
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.paddingLeft = 20; header.style.paddingRight = 16;
        header.style.paddingTop = 16; header.style.paddingBottom = 12;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
        panel.Add(header);

        var title = new Label("\u2605 快讯");
        title.style.fontSize = 26;
        title.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = fontDef;
        header.Add(title);

        var closeBtn = new UnityEngine.UIElements.Button(() => root.Remove(overlay)) { text = "\u2715" };
        closeBtn.style.width = 36; closeBtn.style.height = 28;
        closeBtn.style.fontSize = 16;
        closeBtn.style.color = new Color(1f, 1f, 1f, 0.6f);
        closeBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.4f);
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = fontDef;
        closeBtn.style.borderTopLeftRadius = 6; closeBtn.style.borderTopRightRadius = 6;
        closeBtn.style.borderBottomLeftRadius = 6; closeBtn.style.borderBottomRightRadius = 6;
        header.Add(closeBtn);

        // Version badge
        var versionRow = new VisualElement();
        versionRow.style.flexDirection = FlexDirection.Row;
        versionRow.style.alignItems = Align.Center;
        versionRow.style.paddingLeft = 20; versionRow.style.paddingRight = 20;
        versionRow.style.paddingTop = 12; versionRow.style.paddingBottom = 8;
        panel.Add(versionRow);

        var versionBadge = new Label("Beta v0.9.0");
        versionBadge.style.fontSize = 16;
        versionBadge.style.color = new Color(1f, 200f / 255f, 100f / 255f, 0.9f);
        versionBadge.style.backgroundColor = new Color(0.3f, 0.15f, 0.08f, 0.5f);
        versionBadge.style.borderTopLeftRadius = 4; versionBadge.style.borderTopRightRadius = 4;
        versionBadge.style.borderBottomLeftRadius = 4; versionBadge.style.borderBottomRightRadius = 4;
        versionBadge.style.paddingLeft = 10; versionBadge.style.paddingRight = 10;
        versionBadge.style.paddingTop = 4; versionBadge.style.paddingBottom = 4;
        versionBadge.style.unityFontDefinition = fontDef;
        versionRow.Add(versionBadge);

        var dateLabel = new Label("2026-08-18");
        dateLabel.style.fontSize = 14;
        dateLabel.style.color = new Color(1f, 1f, 1f, 0.35f);
        dateLabel.style.marginLeft = 12;
        dateLabel.style.unityFontDefinition = fontDef;
        versionRow.Add(dateLabel);

        // Scroll content
        var scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        scrollView.style.paddingLeft = 20; scrollView.style.paddingRight = 20;
        scrollView.style.paddingTop = 8; scrollView.style.paddingBottom = 8;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        panel.Add(scrollView);

        var gold = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        var goldDim = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        var textDim = new Color(1f, 1f, 1f, 0.65f);

        void AddSection(string sectionTitle, string[] items)
        {
            var st = new Label(sectionTitle);
            st.style.fontSize = 20; st.style.color = gold;
            st.style.unityFontStyleAndWeight = FontStyle.Bold;
            st.style.unityFontDefinition = fontDef;
            st.style.marginTop = 14; st.style.marginBottom = 6;
            scrollView.Add(st);

            foreach (var item in items)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 4;
                row.style.paddingLeft = 6;

                var dot = new Label("\u2022");
                dot.style.fontSize = 16; dot.style.color = goldDim;
                dot.style.width = 20;
                dot.style.unityFontDefinition = fontDef;
                row.Add(dot);

                var txt = new Label(item);
                txt.style.fontSize = 16; txt.style.color = textDim;
                txt.style.whiteSpace = WhiteSpace.Normal;
                txt.style.unityFontDefinition = fontDef;
                row.Add(txt);
                scrollView.Add(row);
            }
        }

        AddSection("\u2606 序章体验", new[] {
            "完整序章剧本（10个章节，1600+条对话/旁白）",
            "序章分支对话系统（7处选项，选择影响后续台词走向）",
            "系统启动画面（居中科技感显示，支持点击推进）",
            "边境检查站追逐战（4辆武装载具，3%逃脱率）",
            "统一便当店大采购（BBQ炸鸡、辛拉面、走私动漫周边）",
            "嘉颖徐资助剧情（黑金卡，每月一万沙币额度）",
        });

        AddSection("\u2606 核心系统", new[] {
            "沙本位经济核 v4.2（季节系统、容量约束、补贴入账）",
            "千里马创世核 v3.0（世界种子、确定性生成算法）",
            "岁月叙事引擎 v2.0（节奏控制、双AI线、疲劳对话）",
            "先民人事系统 v1.2（疲劳、忠诚度、技能成长、招聘）",
            "铁龙竞争系统 v1.0（USET竞争AI，关键节点收购）",
        });

        AddSection("\u2606 交互与UI", new[] {
            "VN引擎完整支持（选项、条件分支、场景跳转、链式脚本）",
            "全屏滚动新闻 / 系统启动画面 / 打字机效果",
            "存档管理器（左对齐布局，章节名映射，6槽位页面）",
            "设置系统（5选项卡：音频/游戏/显示/操作/关于）",
            "按键绑定自定义（推进/后退/快进/自动/菜单）",
            "标题界面（自动登录、继续运营、站务日志）",
        });

        AddSection("\u2606 美术与世界观", new[] {
            "沙子飞猪号0721完整设计（两舱布局，无方向盘）",
            "驾驶舱/客舱图片绑定（昼夜版本）",
            "朝鲜「有限开放」世界观（统一便当店、先富带动后富）",
            "角色立绘占位 + 表情映射",
            "像素风格UI（Zpix字体，暖棕色调金色边框）",
        });

        AddSection("\u2606 已知问题", new[] {
            "部分背景图片尚未生成（占位中）",
            "角色立绘为临时占位图",
            "VN回放功能待完善",
            "音频资源尚未全部到位",
        });
    }

    private void SetupVideoBackground()
    {
        // Find video file - try Resources first, then StreamingAssets
        VideoClip clip = null;
        string fallbackUrl = null;
        try { clip = Resources.Load<VideoClip>("Videos/cloud_sea_bg"); } catch { }
        if (clip == null)
        {
            string saPath = System.IO.Path.Combine(Application.streamingAssetsPath, "cloud_sea_long.mp4");
            if (System.IO.File.Exists(saPath))
                fallbackUrl = saPath;
        }
        if (clip == null && fallbackUrl == null) return;

        // Create a GameObject to hold VideoPlayer + Canvas
        var go = new GameObject("VideoBackground");
        go.transform.SetParent(transform);

        // RenderTexture
        var rt = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        rt.Create();

        // VideoPlayer
        var player = go.AddComponent<VideoPlayer>();
        player.isLooping = true;
        player.playOnAwake = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = rt;
        if (clip != null) { player.source = VideoSource.VideoClip; player.clip = clip; }
        else { player.source = VideoSource.Url; player.url = fallbackUrl; }

        // uGUI Canvas with RawImage (works in all pipelines)
        var canvasGO = new GameObject("VideoCanvas");
        canvasGO.transform.SetParent(go.transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10; // behind UI Toolkit
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var rawImageGO = new GameObject("VideoRawImage");
        rawImageGO.transform.SetParent(canvasGO.transform);
        var rawImage = rawImageGO.AddComponent<UnityEngine.UI.RawImage>();
        rawImage.texture = rt;
        rawImage.rectTransform.anchorMin = Vector2.zero;
        rawImage.rectTransform.anchorMax = Vector2.one;
        rawImage.rectTransform.offsetMin = Vector2.zero;
        rawImage.rectTransform.offsetMax = Vector2.zero;

        player.Play();
    }
}
