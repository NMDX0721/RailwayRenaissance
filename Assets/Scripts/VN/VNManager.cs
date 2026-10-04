using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

public class VNManager : MonoBehaviour
{
    public static VNManager Instance { get; private set; }

    private DialogueBox dialogueBox;
    private BackgroundManager backgroundManager;
    private CharacterSpriteManager characterSpriteManager;
    private VNBacklog vnBacklog;
    private VNSaveSystem saveSystem;
    private VNSaveLoadUI saveLoadUI;
    private FullScreenNews fullScreenNews;
    private JSONParser jsonParser;
    private ScriptData currentScript;
    private string currentScriptName;
    private int currentSceneIndex;
    private int currentDialogueIndex;
    private bool isScriptRunning;
    private VisualElement optionsContainer;
    private VisualElement optionsOverlay;
    private Font gameFont;

    // Variable system
    private Dictionary<string, bool> variables = new Dictionary<string, bool>();

    // Auto-play
    private bool isAutoPlay;
    private float autoPlayDelay = 2.0f;
    private Coroutine autoPlayCoroutine;
    private UnityEngine.UIElements.Button autoBtn;
    // 长按空格加速（0.35s 后进入；Shift 仍是跳过式快进）
    private bool spaceHeld;
    private float spaceHoldStart;
    private bool spaceSpeedActive;
    private const float SpaceSpeedThreshold = 0.35f;
    private const float SpaceSpeedInterval = 0.4f;
    private float spaceSpeedNextAt;
    // Shift 倍速模式
    private VisualElement fastFwdBadge;
    private float ffNextAt;
    // 地点/时间标注居中卡片
    private VisualElement locCard;
    private Label locCardText;
    private Coroutine locCardHideCo;

    // Confirm dialog
    private VisualElement confirmDialog;
    private VisualElement exitDialog;
    private VisualElement bootScreen;
    private VisualElement cgScreen;
    private bool VN_ReplayInjected;
    private Label bookmarkToast;
    private Coroutine bookmarkToastCoroutine;
    private VisualElement episodeClearOverlay;
    private Coroutine autoAdvanceCoroutine;
    private bool clickedButtonThisFrame; // 按钮点击标记，防止 root BubbleUp 误推进
    private Label musicToast;
    private Coroutine musicToastCoroutine;
    private VNPrologueQuiz prologueQuiz; // 序章跳过答题（Day0 前）

    // Menu bar
    private VisualElement menuBar;

    // UI Document reference
    private UIDocument uiDoc;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var go in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude))
        {
            if (go != gameObject && go.name == "VN_TestManager")
                Destroy(go);
            if (go.name == "UIDocument" && go.GetComponent<UIDocument>() != null)
                Destroy(go);
        }

        jsonParser = new JSONParser();
        gameFont = Resources.Load<Font>("Fonts/zpix");

        if (VNAudioManager.Instance == null)
        {
            var audioObj = new GameObject("VN_AudioManager");
            audioObj.AddComponent<VNAudioManager>();
        }
        // 订阅 BGM 播放事件，显示歌曲名 Toast
        VNAudioManager.Instance.OnBGMStarted += ShowMusicToast;
    }

    private void OnDestroy()
    {
        if (VNAudioManager.Instance != null)
            VNAudioManager.Instance.OnBGMStarted -= ShowMusicToast;
    }

    private void Start()
    {
        SetupGameCursor();
        SetupCanvas();
        GameData.ApplyVolume();
        SetupEventSystem();
        // 预热阶段一（文档+锁屏，日志验证安全段）；桌面由 Show 时后台错帧加载
        StartCoroutine(PreloadTabletSoon());

        // 标题界面"继续运营" → 直接显示读档界面，不播序章
        if (PlayerPrefs.GetInt("VN_ShowLoadUI", 0) == 1)
        {
            PlayerPrefs.SetInt("VN_ShowLoadUI", 0);
            PlayerPrefs.Save();
            // 停止标题场景的全局 BGM（DontDestroyOnLoad 的 "BGM" 物体）避免与 VN BGM 重叠
            StopGlobalTitleBGM();
            // 立即隐藏 VN 界面元素，不等下一帧
            HideVnForLoadUI();
            isFromTitleScreenMode = true;
            StartCoroutine(ShowLoadUIDelayed());
            return;
        }

        // 从读档界面选择存档后重载场景 → 自动加载存档
        if (PlayerPrefs.HasKey("VN_LoadSaveSlot"))
        {
            int slot = PlayerPrefs.GetInt("VN_LoadSaveSlot");
            PlayerPrefs.DeleteKey("VN_LoadSaveSlot");
            PlayerPrefs.Save();
            var saveData = saveSystem.LoadGame(slot);
            if (saveData != null)
            {
                LoadFromSave(saveData);
            }
            return;
        }

        // 标题界面自动加载（旧逻辑，保留兼容）
        if (PlayerPrefs.GetInt("VN_AutoLoad", 0) == 1)
        {
            PlayerPrefs.SetInt("VN_AutoLoad", 0);
            PlayerPrefs.Save();
            VNSaveData save = LoadLatestSave();
            if (save != null)
            {
                LoadFromSave(save);
                return;
            }
        }

        // 站长日志·故事回看：跳播指定章节脚本（支持书签跳转——指定场景/对话索引）
        string replay = PlayerPrefs.GetString("VN_ReplayScript", "");
        if (!string.IsNullOrEmpty(replay))
        {
            PlayerPrefs.DeleteKey("VN_ReplayScript");
            PlayerPrefs.Save();

            // 书签跳转：若带场景/对话索引，则从该位置开始
            VN_ReplayInjected = true;
            if (PlayerPrefs.HasKey("VN_ReplayScene"))
            {
                int bookmarkScene = PlayerPrefs.GetInt("VN_ReplayScene");
                int bookmarkDialogue = PlayerPrefs.GetInt("VN_ReplayDialogue", 0);
                PlayerPrefs.DeleteKey("VN_ReplayScene");
                PlayerPrefs.DeleteKey("VN_ReplayDialogue");
                PlayerPrefs.Save();
                StartBookmarkScript(replay, bookmarkScene, bookmarkDialogue);
            }
            else
            {
                StartScript(replay);
            }
            return;
        }

        // 强制退出恢复：存在未完成的自动书签，且非读档/继续运营/回看进入 → 提示继续观看
        bool isLoadFlow = PlayerPrefs.HasKey("VN_AutoLoad") || PlayerPrefs.HasKey("VN_ShowLoadUI") || PlayerPrefs.HasKey("VN_ReplayScript");
        var incomplete = BookmarkManager.FindIncompleteAuto();
        if (incomplete != null && !isLoadFlow)
        {
            pendingResumeBookmark = incomplete;
            ShowResumePrompt(incomplete);
            return; // 等待玩家选择
        }

        StartScript("prologue_01_news");
    }

    private BookmarkManager.Bookmark pendingResumeBookmark;

    /// <summary>强制退出恢复提示："检测到未完成的剧情，是否继续观看？"</summary>
    private void ShowResumePrompt(BookmarkManager.Bookmark bm)
    {
        if (resumeDialog == null)
        {
            var root = uiDoc.rootVisualElement;
            resumeDialog = DialogStyle.CreateOverlay(new FontDefinition { font = gameFont });
            resumeDialog.name = "resume-dialog";
            resumeDialog.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            root.Add(resumeDialog);
        }
        resumeDialog.Clear();
        resumeDialog.style.display = DisplayStyle.Flex;

        var fontDef = new FontDefinition { font = gameFont };
        var panel = DialogStyle.CreateBox(480);
        resumeDialog.Add(panel);

        panel.Add(DialogStyle.CreateTitle("未完成的剧情", fontDef));
        panel.Add(DialogStyle.CreateUnderline());

        var info = new Label(bm.name);
        info.style.fontSize = 18;
        info.style.color = new Color(1f, 1f, 1f, 0.9f);
        info.style.unityFontDefinition = fontDef;
        info.style.unityTextAlign = TextAnchor.MiddleCenter;
        info.style.marginBottom = 6;
        panel.Add(info);

        var preview = new Label(bm.previewText ?? "");
        preview.style.fontSize = 14;
        preview.style.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        preview.style.unityFontDefinition = fontDef;
        preview.style.whiteSpace = WhiteSpace.Normal;
        preview.style.unityTextAlign = TextAnchor.MiddleCenter;
        preview.style.marginBottom = 20;
        panel.Add(preview);

        DialogStyle.AddButtonRow(panel, fontDef, "从头开始", "继续观看",
            () =>
            {
                resumeDialog.style.display = DisplayStyle.None;
                pendingResumeBookmark = null;
                BookmarkManager.ClearAutoBookmark(bm.scriptName);
                StartScript("prologue_01_news");
            },
            () =>
            {
                resumeDialog.style.display = DisplayStyle.None;
                pendingResumeBookmark = null;
                BookmarkManager.JumpToBookmark(bm);
                UnityEngine.SceneManagement.SceneManager.LoadScene("VN_Test");
            });
    }

    private VisualElement resumeDialog;

    /// <summary>从书签指定的场景/对话位置开始播放脚本。</summary>
    private void StartBookmarkScript(string scriptName, int sceneIndex, int dialogueIndex)
    {
        StartScript(scriptName);
        // StartScript 已设 sceneIndex=0/dialogueIndex=0，重定位到书签
        if (currentScript == null || currentSceneIndex >= currentScript.scenes.Length) return;
        int targetScene = Mathf.Clamp(sceneIndex, 0, currentScript.scenes.Length - 1);
        StartScriptAt(scriptName, targetScene, dialogueIndex);
    }

    private void StartScriptAt(string scriptName, int sceneIndex, int dialogueIndex)
    {
        if (string.IsNullOrEmpty(scriptName)) return;
        currentScript = jsonParser.LoadScript(scriptName);
        if (currentScript == null) { Debug.LogError("Failed to load: " + scriptName); return; }
        currentScriptName = scriptName;
        currentSceneIndex = sceneIndex;
        currentDialogueIndex = dialogueIndex;
        ResetProgressShown();
        isScriptRunning = true;

        TitleArchiveUI.AutoUnlock(scriptName);

        vnBacklog?.Clear();
        characterSpriteManager?.ClearAll();
        variables.Clear();

        var scene = currentScript.scenes[Mathf.Min(sceneIndex, currentScript.scenes.Length - 1)];
        if (!string.IsNullOrEmpty(scene.bg))
            backgroundManager?.SetBackgroundImmediate(scene.bg);
        if (!string.IsNullOrEmpty(scene.bgm))
            VNAudioManager.Instance?.PlayBGM(scene.bgm);
        characterSpriteManager?.ClearAll();
        ShowCurrentDialogue();
    }

    private VNSaveData LoadLatestSave()
    {
        VNSaveData latest = null;
        for (int i = 0; i < saveSystem.MaxSlotCount; i++)
        {
            var data = saveSystem.LoadGame(i);
            if (data != null && (latest == null || string.CompareOrdinal(data.timestamp, latest.timestamp) > 0))
                latest = data;
        }
        return latest;
    }

    private void SetupGameCursor()
    {
        if (LoginManager.cursorTexture == null)
            LoginManager.cursorTexture = LoginManager.LoadCursorTexture("Cursors/cursor_arrow", 3);
        UnityEngine.Cursor.SetCursor(LoginManager.cursorTexture, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && LoginManager.cursorTexture != null)
            UnityEngine.Cursor.SetCursor(LoginManager.cursorTexture, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
    }

    private void AddCursorHover(VisualElement element)
    {
        UiCursor.Bind(element);
    }

    private void SetupCanvas()
    {
        var canvasObj = new GameObject("VN_Canvas");
        canvasObj.transform.SetParent(transform);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");

        uiDoc = canvasObj.AddComponent<UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.visualTreeAsset = null;

        // 鼠标推进 = root ClickEvent（按下+释放后触发，避免 PointerDown 双击竞态）
        uiDoc.rootVisualElement.RegisterCallback<ClickEvent>(OnRootClick);

        // 背景必须先初始化（在最底层）
        backgroundManager = gameObject.AddComponent<BackgroundManager>();
        backgroundManager.Init(uiDoc);

        // 立绘先于对话框初始化：人物 bottom=0 贴屏底，必须处于对话框下层避免遮字
        characterSpriteManager = gameObject.AddComponent<CharacterSpriteManager>();
        characterSpriteManager.Init(uiDoc);

        dialogueBox = gameObject.AddComponent<DialogueBox>();
        dialogueBox.Init(uiDoc);
        dialogueBox.OnTypewriterComplete += OnDialogueTypewriterComplete;

        vnBacklog = gameObject.AddComponent<VNBacklog>();
        vnBacklog.Init(uiDoc);
        vnBacklog.OnEntryClicked += OnBacklogEntryClicked;
        dialogueBox.OnProgressSeek = HandleProgressSeek; // 底部进度条点击/拖动跳转
        dialogueBox.OnProgressDragChanged = dragging =>
        {
            progressSeeking = dragging;
            if (!dragging) RefreshAfterSeek(); // 松手：若停在日期地点句，补显示日历卡
        };

        prologueQuiz = gameObject.AddComponent<VNPrologueQuiz>();
        prologueQuiz.Init(uiDoc);

        saveSystem = new VNSaveSystem();
        saveLoadUI = gameObject.AddComponent<VNSaveLoadUI>();
        saveLoadUI.Init(uiDoc, saveSystem);

        fullScreenNews = gameObject.AddComponent<FullScreenNews>();
        fullScreenNews.Init(uiDoc);
        fullScreenNews.OnClosed += OnFullScreenNewsClosed;

        SetupMenuButtons(uiDoc);

        optionsOverlay = new VisualElement();
        optionsOverlay.name = "options-overlay";
        optionsOverlay.style.position = Position.Absolute;
        optionsOverlay.style.top = 0;
        optionsOverlay.style.left = 0;
        optionsOverlay.style.right = 0;
        optionsOverlay.style.bottom = 0;
        optionsOverlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        optionsOverlay.style.display = DisplayStyle.None;
        optionsOverlay.RegisterCallback<PointerDownEvent>(evt => { clickedButtonThisFrame = true; });
        uiDoc.rootVisualElement.Add(optionsOverlay);

        optionsContainer = new VisualElement();
        optionsContainer.name = "options-container";
        optionsContainer.RegisterCallback<PointerDownEvent>(evt => { clickedButtonThisFrame = true; });
        optionsContainer.style.position = Position.Absolute;
        optionsContainer.style.top = 0;
        optionsContainer.style.bottom = 0;
        optionsContainer.style.left = new Length(0, LengthUnit.Pixel);
        optionsContainer.style.right = new Length(0, LengthUnit.Pixel);
        optionsContainer.style.flexDirection = FlexDirection.Column;
        optionsContainer.style.alignItems = Align.Center;
        optionsContainer.style.justifyContent = Justify.Center;
        optionsContainer.style.paddingBottom = 80;
        optionsContainer.style.display = DisplayStyle.None;

        var vnUss = Resources.Load<StyleSheet>("UI/VN/DialogueBox");
        if (vnUss != null) optionsContainer.styleSheets.Add(vnUss);

        uiDoc.rootVisualElement.Add(optionsContainer);
    }

    private void SetupEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var esObj = new GameObject("EventSystem");
        esObj.transform.SetParent(transform);
        esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        esObj.AddComponent<StandaloneInputModule>();
#endif
    }

    private void SetupMenuButtons(UIDocument uiDoc)
    {
        var root = uiDoc.rootVisualElement;
        var fontDef = new FontDefinition { font = gameFont };

        // 主菜单栏：始终显示 Auto + Menu
        menuBar = new VisualElement { name = "menu-bar" };
        menuBar.pickingMode = PickingMode.Position;
        menuBar.style.position = Position.Absolute;
        menuBar.style.top = 10;
        menuBar.style.right = 10;
        menuBar.style.flexDirection = FlexDirection.Row;
        menuBar.style.alignItems = Align.Center;
        root.Add(menuBar);

        // Auto 按钮
        autoBtn = new UnityEngine.UIElements.Button(() => ToggleAutoPlay()) { text = "Auto" };
        autoBtn.focusable = false; // 防止 Space 激活聚焦按钮导致误开 Auto
        autoBtn.RegisterCallback<PointerDownEvent>(evt => { clickedButtonThisFrame = true; evt.StopImmediatePropagation(); });
        autoBtn.RegisterCallback<ClickEvent>(evt => { clickedButtonThisFrame = true; evt.StopImmediatePropagation(); });
        autoBtn.RegisterCallback<PointerUpEvent>(evt => evt.StopImmediatePropagation());
        autoBtn.style.width = 88;
        autoBtn.style.height = 40;
        autoBtn.style.flexShrink = 0;
        autoBtn.style.fontSize = 20;
        autoBtn.style.color = new Color(1f, 1f, 1f, 0.8f);
        autoBtn.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.85f);
        autoBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        autoBtn.style.marginRight = 6;
        autoBtn.style.unityFontDefinition = fontDef;
        // 激活态像素图标（Label 子元素，避免 Unicode ▶ 渲染问题）
        autoIcon = new Label();
        autoIcon.style.width = 16;
        autoIcon.style.height = 16;
        autoIcon.style.flexShrink = 0;
        autoIcon.style.marginRight = 6;
        autoIcon.style.display = DisplayStyle.None;
        autoIcon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        autoIcon.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        autoIcon.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        autoIcon.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        autoIcon.pickingMode = PickingMode.Ignore;
        autoBtn.Add(autoIcon);
        // 像素风格边框（2px 粗框）
        autoBtn.style.borderTopWidth = 2; autoBtn.style.borderBottomWidth = 2;
        autoBtn.style.borderLeftWidth = 2; autoBtn.style.borderRightWidth = 2;
        autoBtn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        autoBtn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        autoBtn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        autoBtn.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        autoBtn.style.borderTopLeftRadius = 4; autoBtn.style.borderTopRightRadius = 4;
        autoBtn.style.borderBottomLeftRadius = 4; autoBtn.style.borderBottomRightRadius = 4;
        AddCursorHover(autoBtn);
        menuBar.Add(autoBtn);

        // Menu 按钮（点击展开/收起子菜单）
        var menuBtn = new UnityEngine.UIElements.Button(() => ToggleMenuExpanded()) { text = "Menu" };
        menuBtn.focusable = false;
        menuBtn.RegisterCallback<PointerDownEvent>(evt => { clickedButtonThisFrame = true; evt.StopImmediatePropagation(); });
        menuBtn.RegisterCallback<ClickEvent>(evt => { clickedButtonThisFrame = true; evt.StopImmediatePropagation(); });
        menuBtn.RegisterCallback<PointerUpEvent>(evt => evt.StopImmediatePropagation());
        menuBtn.style.width = 88;
        menuBtn.style.height = 40;
        menuBtn.style.flexShrink = 0;
        menuBtn.style.fontSize = 20;
        menuBtn.style.color = new Color(1f, 1f, 1f, 0.8f);
        menuBtn.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.85f);
        menuBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        menuBtn.style.unityFontDefinition = fontDef;
        // 展开态像素图标（网格点）
        menuIcon = new Label();
        menuIcon.style.width = 16;
        menuIcon.style.height = 16;
        menuIcon.style.flexShrink = 0;
        menuIcon.style.marginRight = 6;
        menuIcon.style.display = DisplayStyle.None;
        menuIcon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        menuIcon.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        menuIcon.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        menuIcon.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        menuIcon.pickingMode = PickingMode.Ignore;
        menuBtn.Add(menuIcon);
        menuBtn.style.borderTopWidth = 2; menuBtn.style.borderBottomWidth = 2;
        menuBtn.style.borderLeftWidth = 2; menuBtn.style.borderRightWidth = 2;
        menuBtn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        menuBtn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        menuBtn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        menuBtn.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        menuBtn.style.borderTopLeftRadius = 4; menuBtn.style.borderTopRightRadius = 4;
        menuBtn.style.borderBottomLeftRadius = 4; menuBtn.style.borderBottomRightRadius = 4;
        menuBtn.name = "vn-menu-btn";
        AddCursorHover(menuBtn);
        menuBar.Add(menuBtn);
        this.menuBtn = menuBtn;

        // 子菜单（BA 风格展开底板：深棕半透明面板 + 金边 + 内衬，按钮依附于板）
        menuExpandedContainer = new VisualElement { name = "menu-expanded" };
        menuExpandedContainer.style.position = Position.Absolute;
        menuExpandedContainer.style.top = 64;   // Auto/Menu 行下方留一档间距
        menuExpandedContainer.style.right = 10; // 右缘与 Menu 按钮对齐
        menuExpandedContainer.style.flexDirection = FlexDirection.Row;
        menuExpandedContainer.style.alignItems = Align.Center;
        menuExpandedContainer.style.display = DisplayStyle.None;
        // 底板：深棕半透明 + 金色双层描边 + 圆角 + 内边距
        menuExpandedContainer.style.backgroundColor = new Color(0.08f, 0.05f, 0.03f, 0.96f);
        menuExpandedContainer.style.borderTopWidth = 2; menuExpandedContainer.style.borderBottomWidth = 2;
        menuExpandedContainer.style.borderLeftWidth = 2; menuExpandedContainer.style.borderRightWidth = 2;
        menuExpandedContainer.style.borderTopColor = new Color(0.82f, 0.62f, 0.35f, 0.85f);
        menuExpandedContainer.style.borderBottomColor = new Color(0.82f, 0.62f, 0.35f, 0.85f);
        menuExpandedContainer.style.borderLeftColor = new Color(0.82f, 0.62f, 0.35f, 0.85f);
        menuExpandedContainer.style.borderRightColor = new Color(0.82f, 0.62f, 0.35f, 0.85f);
        menuExpandedContainer.style.borderTopRightRadius = 6; menuExpandedContainer.style.borderBottomRightRadius = 6;
        menuExpandedContainer.style.borderTopLeftRadius = 6; menuExpandedContainer.style.borderBottomLeftRadius = 6;
        // 紧凑内边距（框不要太大）
        menuExpandedContainer.style.paddingLeft = 6; menuExpandedContainer.style.paddingRight = 6;
        menuExpandedContainer.style.paddingTop = 6; menuExpandedContainer.style.paddingBottom = 6;
        menuExpandedContainer.pickingMode = PickingMode.Position;
        root.Add(menuExpandedContainer);

        // 子菜单项：回顾 / 跳过 / 书签 / 退出（Shift+→ 仍可场景内快进，不再占菜单位）
        var menuItemDefs = new (Texture2D icon, System.Action action)[]
        {
            (PixelIconHelper.MenuHamburgerIcon(), () => ToggleBacklog()), // 回顾
            (PixelIconHelper.SkipFastIcon(), () => RequestSkip()),     // 跳过（Day0 前答题 / 注册后确认）
            (PixelIconHelper.BookmarkTabIcon(), () => AddBookmark()), // 书签
            (PixelIconHelper.ExitDoorIcon(), () => ShowExitDialog()), // 退出
        };

        foreach (var (icon, itemAction) in menuItemDefs)
        {
            var btn = new UnityEngine.UIElements.Button(() => itemAction());
            // 图标用 Label 背景渲染（避免 Unicode 字形在不同系统字体下的不一致）
            var iconLabel = new Label();
            iconLabel.style.width = 24;
            iconLabel.style.height = 24;
            iconLabel.style.flexShrink = 0;
            iconLabel.pickingMode = PickingMode.Ignore;
            iconLabel.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            iconLabel.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            iconLabel.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            iconLabel.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            iconLabel.style.backgroundImage = new StyleBackground(icon);
            iconLabel.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.95f);
            btn.Add(iconLabel);
            // Click 必须停冒泡：否则松开时 Bubble 到 root 会推进下一句（书签/跳过误推进的根因）
            btn.RegisterCallback<PointerDownEvent>(evt =>
            {
                clickedButtonThisFrame = true;
                SuppressNextAdvance();
                evt.StopImmediatePropagation();
            });
            btn.RegisterCallback<ClickEvent>(evt =>
            {
                clickedButtonThisFrame = true;
                SuppressNextAdvance();
                evt.StopImmediatePropagation();
            });
            // 居中图标：去掉 Button 默认 padding，显式 flex 居中（框小、图标大）
            btn.style.width = 48;
            btn.style.height = 40;
            btn.style.paddingLeft = 0; btn.style.paddingRight = 0;
            btn.style.paddingTop = 0; btn.style.paddingBottom = 0;
            btn.style.justifyContent = Justify.Center;
            btn.style.alignItems = Align.Center;
            btn.style.backgroundColor = new Color(0.14f, 0.09f, 0.05f, 0.8f);
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.color = new Color(1f, 0.86f, 0.59f, 0.95f);
            // 每项独立淡金边框（常显）
            btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new Color(0.82f, 0.62f, 0.35f, 0.5f);
            btn.style.borderBottomColor = new Color(0.82f, 0.62f, 0.35f, 0.5f);
            btn.style.borderLeftColor = new Color(0.82f, 0.62f, 0.35f, 0.5f);
            btn.style.borderRightColor = new Color(0.82f, 0.62f, 0.35f, 0.5f);
            btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
            btn.style.marginLeft = 3; btn.style.marginRight = 3;
            btn.RegisterCallback<PointerEnterEvent>(evt =>
            {
                btn.style.backgroundColor = new Color(0.28f, 0.18f, 0.09f, 0.95f);
                btn.style.borderTopColor = new Color(1f, 0.85f, 0.5f, 1f);
                btn.style.borderBottomColor = new Color(1f, 0.85f, 0.5f, 1f);
                btn.style.borderLeftColor = new Color(1f, 0.85f, 0.5f, 1f);
                btn.style.borderRightColor = new Color(1f, 0.85f, 0.5f, 1f);
            });
            btn.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                btn.style.backgroundColor = new Color(0.14f, 0.09f, 0.05f, 0.8f);
                btn.style.borderTopColor = new Color(0.82f, 0.62f, 0.35f, 0.5f);
                btn.style.borderBottomColor = new Color(0.82f, 0.62f, 0.35f, 0.5f);
                btn.style.borderLeftColor = new Color(0.82f, 0.62f, 0.35f, 0.5f);
                btn.style.borderRightColor = new Color(0.82f, 0.62f, 0.35f, 0.5f);
            });
            menuExpandedContainer.Add(btn);
            AddCursorHover(btn);
        }

        UpdateAutoPlayButtonVisual();
        CreateFastForwardBadge(root);
        SetupConfirmDialog(uiDoc);
        SetupExitDialog(uiDoc);
    }

    /// <summary>Shift 倍速指示：右上角小徽标，按住 Shift 时显示。</summary>
    private void CreateFastForwardBadge(VisualElement root)
    {
        fastFwdBadge = new VisualElement { name = "fastfwd-badge" };
        fastFwdBadge.style.position = Position.Absolute;
        fastFwdBadge.style.top = 112;
        fastFwdBadge.style.right = 10;
        fastFwdBadge.style.flexDirection = FlexDirection.Row;
        fastFwdBadge.style.alignItems = Align.Center;
        fastFwdBadge.style.paddingLeft = 8; fastFwdBadge.style.paddingRight = 10;
        fastFwdBadge.style.paddingTop = 4; fastFwdBadge.style.paddingBottom = 4;
        fastFwdBadge.style.backgroundColor = new Color(0.08f, 0.05f, 0.03f, 0.9f);
        fastFwdBadge.style.borderTopWidth = 1; fastFwdBadge.style.borderBottomWidth = 1;
        fastFwdBadge.style.borderLeftWidth = 1; fastFwdBadge.style.borderRightWidth = 1;
        var gold = new Color(1f, 0.78f, 0.4f, 0.9f);
        fastFwdBadge.style.borderTopColor = gold; fastFwdBadge.style.borderBottomColor = gold;
        fastFwdBadge.style.borderLeftColor = gold; fastFwdBadge.style.borderRightColor = gold;
        fastFwdBadge.style.borderTopLeftRadius = 6; fastFwdBadge.style.borderTopRightRadius = 6;
        fastFwdBadge.style.borderBottomLeftRadius = 6; fastFwdBadge.style.borderBottomRightRadius = 6;
        fastFwdBadge.pickingMode = PickingMode.Ignore;
        fastFwdBadge.style.display = DisplayStyle.None;

        var icon = new Label();
        icon.style.width = 18; icon.style.height = 18;
        icon.style.backgroundImage = new StyleBackground(PixelIconHelper.SkipFastIcon());
        icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        icon.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 1f);
        icon.pickingMode = PickingMode.Ignore;
        fastFwdBadge.Add(icon);

        var txt = new Label(" ×2");
        txt.style.fontSize = 16;
        txt.style.color = new Color(1f, 0.86f, 0.59f, 1f);
        txt.style.unityFontDefinition = new FontDefinition { font = gameFont };
        txt.pickingMode = PickingMode.Ignore;
        fastFwdBadge.Add(txt);

        root.Add(fastFwdBadge);
    }

    /// <summary>除 Shift 外是否还有其他按键按住——纯 Shift 倍速时徽标置顶用。</summary>
    private static readonly KeyCode[] _otherKeyScan =
    {
        KeyCode.Space, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Escape, KeyCode.Tab,
        KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow,
        KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt,
        KeyCode.LeftWindows, KeyCode.RightWindows,
        KeyCode.Backspace, KeyCode.Delete,
    };

    /// <summary>Win 键按住（Win+S 等系统组合）——禁用 Shift 倍速用。</summary>
    private static bool WinHeld() =>
        LegacyInputAdapter.GetKey(KeyCode.LeftWindows) || LegacyInputAdapter.GetKey(KeyCode.RightWindows);

    private static bool AnyOtherHeldKey()
    {
        for (int i = 0; i < _otherKeyScan.Length; i++)
            if (LegacyInputAdapter.GetKey(_otherKeyScan[i])) return true;
        // 字母/数字：防 Shift+字母类快捷键被误判为纯 Shift
        for (int k = (int)KeyCode.A; k <= (int)KeyCode.Z; k++)
            if (LegacyInputAdapter.GetKey((KeyCode)k)) return true;
        for (int k = (int)KeyCode.Alpha0; k <= (int)KeyCode.Alpha9; k++)
            if (LegacyInputAdapter.GetKey((KeyCode)k)) return true;
        return false;
    }

    /// <summary>时间/地点标注的居中卡片。</summary>
    private void ShowLocationCard(string text)
    {
        if (locCard == null)
        {
            locCard = new VisualElement { name = "location-card" };
            locCard.style.position = Position.Absolute;
            locCard.style.top = 0; locCard.style.left = 0; locCard.style.right = 0; locCard.style.bottom = 0;
            locCard.style.alignItems = Align.Center;
            locCard.style.justifyContent = Justify.Center;
            locCard.pickingMode = PickingMode.Ignore;

            var box = new VisualElement();
            box.style.backgroundColor = new Color(0.06f, 0.04f, 0.02f, 0.88f);
            box.style.borderTopWidth = 2; box.style.borderBottomWidth = 2;
            box.style.borderLeftWidth = 2; box.style.borderRightWidth = 2;
            var gold = new Color(0.8f, 0.62f, 0.35f, 0.85f);
            box.style.borderTopColor = gold; box.style.borderBottomColor = gold;
            box.style.borderLeftColor = gold; box.style.borderRightColor = gold;
            box.style.borderTopLeftRadius = 12; box.style.borderTopRightRadius = 12;
            box.style.borderBottomLeftRadius = 12; box.style.borderBottomRightRadius = 12;
            box.style.paddingLeft = 44; box.style.paddingRight = 44;
            box.style.paddingTop = 22; box.style.paddingBottom = 22;

            locCardText = new Label();
            locCardText.style.fontSize = 30;
            locCardText.style.color = new Color(1f, 0.9f, 0.7f, 1f);
            locCardText.style.unityFontDefinition = new FontDefinition { font = gameFont };
            locCardText.style.unityTextAlign = TextAnchor.MiddleCenter;
            locCardText.style.whiteSpace = WhiteSpace.Normal;
            box.Add(locCardText);

            locCard.Add(box);
            uiDoc.rootVisualElement.Add(locCard);
        }
        locCardText.text = text;
        locCard.style.display = DisplayStyle.Flex;
        // 自动隐退：2.2s 后淡出隐藏，并自动推进到下一句（loc 句不占对话框，藏了必须接上）
        locCardAutoAdvance = true;
        if (locCardHideCo != null) StopCoroutine(locCardHideCo);
        locCardHideCo = StartCoroutine(HideLocationCardAfter(2.2f));
    }

    private void HideLocationCard()
    {
        if (locCard != null)
            locCard.style.display = DisplayStyle.None;
    }

    /// <summary>日期地点卡自动隐退 + 自动推进（无需点击）。</summary>
    private IEnumerator HideLocationCardAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        HideLocationCard();
        locCardHideCo = null;
        // 若玩家已手动推进则不动；仍停在 loc 句则自动接下一句
        if (locCardAutoAdvance && isScriptRunning && currentScript?.scenes != null &&
            currentSceneIndex >= 0 && currentSceneIndex < currentScript.scenes.Length)
        {
            var scene = currentScript.scenes[currentSceneIndex];
            if (scene.d != null && currentDialogueIndex >= 0 &&
                currentDialogueIndex < scene.d.Length && scene.d[currentDialogueIndex].t == "loc")
            {
                locCardAutoAdvance = false;
                NextDialogue();
            }
        }
        locCardAutoAdvance = false;
    }

    private bool menuExpanded;
    private VisualElement menuExpandedContainer;
    private UnityEngine.UIElements.Button menuBtn;
    private Label autoIcon;
    private Label menuIcon;

    private void ToggleMenuExpanded()
    {
        // 打开菜单：暂停自动协程但不关掉 Auto 模式；关闭时若仍开着则重启
        if (!menuExpanded)
        {
            StopAutoPlay(false);
            menuExpanded = true;
            menuExpandedContainer.style.display = DisplayStyle.Flex;
            UpdateMenuButtonVisual();
        }
        else
        {
            menuExpanded = false;
            menuExpandedContainer.style.display = DisplayStyle.None;
            UpdateMenuButtonVisual();
            if (isAutoPlay && isScriptRunning)
                StartAutoPlayTimer();
        }
    }

    private void UpdateMenuButtonVisual()
    {
        if (menuBtn == null) return;
        if (menuExpanded)
        {
            // 展开：棋盘格暖金底 + 双层金边（不再显示小图标——曾在 M 字处投出杂影）
            menuBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.ActivePattern());
            menuBtn.style.unityBackgroundImageTintColor = Color.white;
            menuBtn.style.backgroundColor = new Color(0.45f, 0.28f, 0.12f, 0.95f);
            menuBtn.style.color = new Color(1f, 0.9f, 0.55f, 1f);
            menuIcon.style.display = DisplayStyle.None;
            SetGoldBorder(menuBtn, 2, new Color(1f, 0.8f, 0.4f, 0.9f), new Color(1f, 0.9f, 0.6f, 0.45f));
        }
        else
        {
            menuBtn.style.backgroundImage = null;
            menuBtn.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.85f);
            menuBtn.style.color = new Color(1f, 1f, 1f, 0.8f);
            menuIcon.style.display = DisplayStyle.None;
            SetGoldBorder(menuBtn, 2, new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f), new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.1f));
        }
    }

    private void CloseMenuExpanded()
    {
        if (menuExpanded)
        {
            menuExpanded = false;
            menuExpandedContainer.style.display = DisplayStyle.None;
            UpdateMenuButtonVisual();
        }
    }

    /// <summary>Backlog/面板关闭同帧点击：吞掉下一次 root 推进。</summary>
    public void SuppressNextAdvance()
    {
        clickedButtonThisFrame = true;
        lastAdvanceTime = Time.unscaledTime;
    }

    /// <summary>
    /// 跳过入口（菜单双箭头）：
    /// Day0 前（序章01 或未拿 0721）→ 10 题答题（≥7 才可跳到 prologue_02）；
    /// 之后 → Summary 确认弹窗（与退出同壳）。
    /// </summary>
    private void RequestSkip()
    {
        CloseMenuExpanded();
        if (dialogueBox != null && dialogueBox.IsTyping())
            dialogueBox.SkipTyping();

        if (NeedsQuizForSkip())
        {
            if (prologueQuiz == null)
            {
                Debug.LogError("[VN] prologueQuiz is null — cannot open quiz");
                return;
            }
            if (!prologueQuiz.IsOpen)
            {
                prologueQuiz.Show(passed =>
                {
                    if (passed)
                    {
                        PlayerPrefs.SetInt("VN_Got0721", 1);
                        PlayerPrefs.Save();
                        JumpToDay0();
                    }
                });
            }
            return;
        }

        // Day0 后：Summary 跳过确认
        ShowConfirmDialog();
    }

    /// <summary>序章01 永远算 Day0 前（必答题）；其余看 VN_Got0721。</summary>
    private bool NeedsQuizForSkip()
    {
        if (!string.IsNullOrEmpty(currentScriptName) && currentScriptName.StartsWith("prologue_01"))
            return true;
        return PlayerPrefs.GetInt("VN_Got0721", 0) != 1;
    }

    private static bool HasPassedDay0Gate()
    {
        return !NeedsQuizForSkipStatic();
    }

    private static bool NeedsQuizForSkipStatic()
    {
        // static 路径仅用于无 script 上下文；ShowConfirmDialog 走实例方法
        return PlayerPrefs.GetInt("VN_Got0721", 0) != 1;
    }

    /// <summary>答题通过：停当前脚本，跳到 Day0（prologue_02）起点。</summary>
    private void JumpToDay0()
    {
        StopAutoPlay();
        HideOptions();
        if (confirmDialog != null)
            confirmDialog.style.display = DisplayStyle.None;
        vnBacklog?.Clear();
        StartScript("prologue_02_day0");
    }

    /// <summary>添加书签（手动）：Menu → 书签，自动获取话数/话标题/台词预览。</summary>
    private void AddBookmark()
    {
        CloseMenuExpanded();
        if (currentScript == null || string.IsNullOrEmpty(currentScriptName)) return;
        var scene = currentScript.scenes[currentSceneIndex];
        var entry = currentDialogueIndex < scene.d.Length ? scene.d[currentDialogueIndex] : null;
        string preview = entry != null ? entry.text : "(无文本)";
        BookmarkManager.AddManual(
            currentScriptName,
            GetEpisodeTitle(currentScriptName),
            GetEpisodeNumber(currentScriptName),
            currentSceneIndex,
            currentDialogueIndex,
            preview);
        // 顶部浮层提示
        ShowBookmarkToast();
    }

    private void ShowBookmarkToast()
    {
        if (bookmarkToast == null)
        {
            bookmarkToast = new Label("已添加书签");
            bookmarkToast.style.position = Position.Absolute;
            bookmarkToast.style.top = 60;
            bookmarkToast.style.left = 0; bookmarkToast.style.right = 0;
            bookmarkToast.style.fontSize = 20;
            bookmarkToast.style.color = new Color(1f, 0.85f, 0.5f, 1f);
            bookmarkToast.style.unityTextAlign = TextAnchor.MiddleCenter;
            bookmarkToast.style.unityFontDefinition = new FontDefinition { font = gameFont };
            bookmarkToast.pickingMode = PickingMode.Ignore;
            uiDoc.rootVisualElement.Add(bookmarkToast);
        }
        bookmarkToast.style.display = DisplayStyle.Flex;
        if (bookmarkToastCoroutine != null) StopCoroutine(bookmarkToastCoroutine);
        bookmarkToastCoroutine = StartCoroutine(HideBookmarkToastDelayed());
    }

    private System.Collections.IEnumerator HideBookmarkToastDelayed()
    {
        yield return new UnityEngine.WaitForSeconds(1.5f);
        if (bookmarkToast != null) bookmarkToast.style.display = DisplayStyle.None;
    }

    // ———— 音乐 Toast（左上角显示歌曲名，悬停后消失） ————
    private void ShowMusicToast(string songName)
    {
        if (musicToast == null)
        {
            musicToast = new Label("");
            musicToast.style.position = Position.Absolute;
            musicToast.style.top = 50;
            musicToast.style.left = 16;
            musicToast.style.maxWidth = 300;
            musicToast.style.fontSize = 16;
            musicToast.style.color = new Color(1f, 0.92f, 0.78f, 0.9f);
            musicToast.style.unityTextAlign = TextAnchor.MiddleLeft;
            musicToast.style.unityFontDefinition = new FontDefinition { font = gameFont };
            musicToast.pickingMode = PickingMode.Ignore;
            musicToast.style.display = DisplayStyle.None;
            uiDoc.rootVisualElement.Add(musicToast);
        }
        // 显示歌曲名（从 BGM 名映射到可读名称）
        string displayName = MapBGMDisplayName(songName);
        musicToast.text = "♪ " + displayName;
        musicToast.style.display = DisplayStyle.Flex;
        if (musicToastCoroutine != null) StopCoroutine(musicToastCoroutine);
        musicToastCoroutine = StartCoroutine(HideMusicToastDelayed());
    }

    private System.Collections.IEnumerator HideMusicToastDelayed()
    {
        yield return new UnityEngine.WaitForSeconds(3f);
        if (musicToast != null) musicToast.style.display = DisplayStyle.None;
    }

    private string MapBGMDisplayName(string clipName)
    {
        switch (clipName)
        {
            case "iron_and_ash": return "铁与灰";
            case "cloud_rail": return "云轨";
            case "embers": return "余烬";
            case "night_cargo": return "夜行货";
            case "first_light": return "晨光";
            case "platform": return "站台";
            case "borderline": return "国境线";
            case "wheels_joke": return "方向盘在笑";
            case "train_through_keys": return "旧曲";
            case "south_wind": return "南风";
            case "starlit_rails": return "星光铁轨";
            case "chollima_ride": return "千里马驰骋新时代";
            case "calm": return "平静";
            default: return clipName;
        }
    }

    private void SetupConfirmDialog(UIDocument uiDoc)
    {
        var root = uiDoc.rootVisualElement;
        var fontDef = new FontDefinition { font = gameFont };

        confirmDialog = DialogStyle.CreateOverlay(fontDef);
        confirmDialog.name = "confirm-dialog";
        root.Add(confirmDialog);

        // 版式对齐参考图（BA Summary 弹窗）：大盒 + 大标题金线 + 右上✕ + 话名副标题 + 浅色内容框 + 问题行 + 两大按钮
        var dialogBox = DialogStyle.CreateBox(640);
        dialogBox.style.paddingTop = 40;
        dialogBox.style.paddingBottom = 36;
        dialogBox.style.position = Position.Relative;
        confirmDialog.Add(dialogBox);

        var title = DialogStyle.CreateTitle("跳过剧情？", fontDef);
        title.style.fontSize = 34;
        title.style.marginBottom = 2;
        dialogBox.Add(title);
        dialogBox.Add(DialogStyle.CreateUnderline());

        // 右上角 ✕（=取消）
        var closeX = new Label("✕");
        closeX.pickingMode = PickingMode.Position;
        closeX.style.position = Position.Absolute;
        closeX.style.top = 14; closeX.style.right = 18;
        closeX.style.fontSize = 24;
        closeX.style.color = new Color(1f, 1f, 1f, 0.7f);
        closeX.style.unityFontDefinition = fontDef;
        closeX.RegisterCallback<PointerDownEvent>(e =>
        {
            clickedButtonThisFrame = true;
            SuppressNextAdvance();
            e.StopImmediatePropagation();
            confirmDialog.style.display = DisplayStyle.None;
        });
        dialogBox.Add(closeX);

        // 话名副标题（动态填充）
        var epHead = new Label();
        epHead.name = "confirm-eptitle";
        epHead.style.fontSize = 22;
        epHead.style.color = new Color(1f, 1f, 1f, 0.95f);
        epHead.style.unityFontStyleAndWeight = FontStyle.Bold;
        epHead.style.unityFontDefinition = fontDef;
        epHead.style.unityTextAlign = TextAnchor.MiddleCenter;
        epHead.style.marginBottom = 14;
        dialogBox.Add(epHead);

        // 进度总结内容框（跳过专用；退出无此框）
        var summary = new Label();
        summary.name = "confirm-summary";
        summary.style.fontSize = 17;
        summary.style.color = new Color(1f, 1f, 1f, 0.7f);
        summary.style.unityFontDefinition = fontDef;
        summary.style.whiteSpace = WhiteSpace.Normal;
        summary.style.unityTextAlign = TextAnchor.UpperLeft;
        summary.style.paddingLeft = 20; summary.style.paddingRight = 20;
        summary.style.paddingTop = 16; summary.style.paddingBottom = 16;
        summary.style.backgroundColor = new Color(1f, 1f, 1f, 0.07f);
        summary.style.borderTopWidth = 1; summary.style.borderBottomWidth = 1;
        summary.style.borderLeftWidth = 1; summary.style.borderRightWidth = 1;
        var sumB = new Color(0.8f, 0.62f, 0.35f, 0.35f);
        summary.style.borderTopColor = sumB; summary.style.borderBottomColor = sumB;
        summary.style.borderLeftColor = sumB; summary.style.borderRightColor = sumB;
        summary.style.borderTopLeftRadius = 10; summary.style.borderTopRightRadius = 10;
        summary.style.borderBottomLeftRadius = 10; summary.style.borderBottomRightRadius = 10;
        summary.style.minHeight = 110;
        summary.style.alignSelf = Align.Stretch;
        summary.style.marginBottom = 20;
        dialogBox.Add(summary);

        var desc = DialogStyle.CreateBody("跳过后将返回桌面，可从书签继续。\n确认跳过？", fontDef);
        desc.style.fontSize = 18;
        desc.style.marginBottom = 28;
        dialogBox.Add(desc);

        var (cancelBtn, primaryBtn) = DialogStyle.AddButtonRow(dialogBox, fontDef, "取消", "确认跳过",
            () => { clickedButtonThisFrame = true; confirmDialog.style.display = DisplayStyle.None; },
            () => { clickedButtonThisFrame = true; confirmDialog.style.display = DisplayStyle.None; ReturnToTitle(); });
        foreach (var b in new[] { cancelBtn, primaryBtn })
        {
            b.style.width = 190;
            b.style.height = 58;
            b.style.fontSize = 20;
        }
    }

    /// <summary>
    /// 退出确认：取消 / 退出剧情（自动存书签回桌面）/ 退出游戏。
    /// 与跳过确认共用 DialogStyle 壳；退出无总结框。
    /// </summary>
    private void SetupExitDialog(UIDocument uiDoc)
    {
        var root = uiDoc.rootVisualElement;
        var fontDef = new FontDefinition { font = gameFont };

        exitDialog = DialogStyle.CreateOverlay(fontDef);
        exitDialog.name = "exit-dialog";
        exitDialog.RegisterCallback<PointerDownEvent>(e => { e.StopImmediatePropagation(); SuppressNextAdvance(); });
        exitDialog.RegisterCallback<ClickEvent>(e => { e.StopImmediatePropagation(); SuppressNextAdvance(); });
        root.Add(exitDialog);

        var box = DialogStyle.CreateBox(480);
        exitDialog.Add(box);

        box.Add(DialogStyle.CreateTitle("退出", fontDef));
        box.Add(DialogStyle.CreateUnderline());

        var ask = DialogStyle.CreateBody("退出剧情将自动保存当前进度书签。", fontDef);
        ask.style.marginBottom = 24;
        box.Add(ask);

        // 三键一行，同高同边框（取消 / 退出剧情 / 退出游戏）
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.Center;
        row.style.alignItems = Align.Center;
        box.Add(row);

        void ExitBtn(string text, Color bg, Color fg, System.Action act, float mr)
        {
            var b = DialogStyle.MakeButton(text, bg, fg, fontDef, act);
            b.style.width = 120;
            b.style.height = 44;
            b.style.fontSize = 15;
            // 强制金边，避免 Unity 默认白边
            b.style.borderTopColor = DialogStyle.GoldBorder;
            b.style.borderBottomColor = DialogStyle.GoldBorder;
            b.style.borderLeftColor = DialogStyle.GoldBorder;
            b.style.borderRightColor = DialogStyle.GoldBorder;
            if (mr > 0) b.style.marginRight = mr;
            row.Add(b);
        }

        ExitBtn("取消", DialogStyle.CancelBg, DialogStyle.CancelFg,
            () => { clickedButtonThisFrame = true; exitDialog.style.display = DisplayStyle.None; }, 12);
        ExitBtn("退出剧情", DialogStyle.PrimaryBg, DialogStyle.PrimaryFg,
            () => { clickedButtonThisFrame = true; exitDialog.style.display = DisplayStyle.None; StartCoroutine(ExitStoryNextFrame()); }, 12);
        ExitBtn("退出游戏", new Color(0.35f, 0.16f, 0.12f, 0.95f), new Color(1f, 0.75f, 0.6f, 0.95f),
            () => { clickedButtonThisFrame = true; ExitGame(); }, 0);
    }

    private void ShowExitDialog()
    {
        CloseMenuExpanded();
        if (exitDialog != null)
            exitDialog.style.display = DisplayStyle.Flex;
    }

    /// <summary>退出剧情：先写自动书签，再走 ReturnToTitle（回平板桌面）。</summary>
    private void ExitStoryWithBookmark()
    {
        AutoBookmarkTrack(); // 覆盖当前进度书签
        ReturnToTitle();
    }

    /// <summary>退出剧情挪出点击事件栈再执行——清理与建平板拆到不同帧，避开同帧 panel 竞态（native 崩根因）。</summary>
    private IEnumerator ExitStoryNextFrame()
    {
        try { AutoBookmarkTrack(); }
        catch (System.Exception ex) { TraceLog.L("[VN] ExitStory bookmark failed: " + ex); }
        yield return null;
        yield return null;
        TraceLog.L("[VN] ExitStory: cleanup phase");
        try { CleanupForTablet(); }
        catch (System.Exception ex) { TraceLog.L("[VN] CleanupForTablet failed: " + ex); }
        yield return null;
        yield return null; // 关键：让 VN 的 UIDocument 隐藏完全 settle，再创建平板 panel
        TraceLog.L("[VN] ExitStory: show-tablet phase");
        try { ShowTablet(); }
        catch (System.Exception ex) { TraceLog.L("[VN] ShowTablet failed: " + ex); }
        TraceLog.L("[VN] ExitStory: all done");
    }

    /// <summary>回平板第一步：清 VN 运行态并隐藏 VN UI（不动 GameMainUI）。</summary>
    private void CleanupForTablet()
    {
        Debug.Log("[VN] ReturnToTitle step: cleanup-start");
        StopAutoPlay();
        isScriptRunning = false;
        currentScript = null;
        currentScriptName = null;
        variables.Clear();
        HideOptions();
        vnBacklog?.Clear();
        saveLoadUI?.ClosePanel();
        dialogueBox?.Hide();
        HideCgScreen();
        HideBootScreen();
        characterSpriteManager?.ClearAll();
        VNAudioManager.Instance?.StopBGM();
        if (confirmDialog != null) confirmDialog.style.display = DisplayStyle.None;
        if (exitDialog != null) exitDialog.style.display = DisplayStyle.None;
        PlayerPrefs.SetInt("VN_AutoLoad", 0);
        PlayerPrefs.Save();

        if (PlayerPrefs.GetInt("VN_FromGameMain", 0) == 1)
        {
            PlayerPrefs.SetInt("VN_FromGameMain", 0);
            PlayerPrefs.Save();
        }
        Debug.Log("[VN] ReturnToTitle step: hide-vn-ui");
        // 只隐藏已存在的图鉴——绝不 EnsureInstance（退出路径凭空创建 2000+ 行 UITK 构建是 native 崩点）
        try { TitleArchiveUI.Instance?.Hide(); }
        catch (System.Exception ex) { Debug.LogError("[VN] archive hide failed: " + ex); }
        Debug.Log("[VN] ReturnToTitle step: archive-ok");
        try { MainStoryUI.HideStatic(); }
        catch (System.Exception ex) { Debug.LogError("[VN] mainstory hide failed: " + ex); }
        Debug.Log("[VN] ReturnToTitle step: mainstory-ok");
        try
        {
            if (uiDoc != null)
                uiDoc.rootVisualElement.style.display = DisplayStyle.None;
        }
        catch (System.Exception ex) { Debug.LogError("[VN] vn root hide failed: " + ex); }
        Debug.Log("[VN] ReturnToTitle step: cleanup-done");
    }

    /// <summary>回平板第二步：显示平板桌面（GameMainUI 首次会同步 Init，native 崩点已布日志）。</summary>
    private void ShowTablet()
    {
        TraceLog.L("[VN] ReturnToTitle step: show-game-main");
        GameMainUI.Show();
        TraceLog.L("[VN] ReturnToTitle step: done");
    }

    private void ReturnToTitle()
    {
        CleanupForTablet();
        ShowTablet();
    }

    private void ExitGame()
    {
        StopAutoPlay();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>隐藏全部 VN UI（保留背景与立绘），截图式纯净画面。</summary>
    private void HideVNUI()
    {
        if (uiHidden || uiDoc == null) return;
        uiHidden = true;
        hiddenUiSnapshots.Clear();

        foreach (var child in new List<VisualElement>(uiDoc.rootVisualElement.Children()))
        {
            // 背景容器保留（纯背景版立绘仍显示）
            if (child.name == "vn-background-container") continue;
            // 人物立绘容器保留（右键只藏 UI，不动立绘）
            if (child.name == "character-container") continue;
            // 菜单展开容器也隐藏
            hiddenUiSnapshots[child] = child.style.display;
            child.style.display = DisplayStyle.None;
        }
        // 关闭展开的子菜单
        menuExpanded = false;
        if (menuExpandedContainer != null)
            menuExpandedContainer.style.display = DisplayStyle.None;
    }

    /// <summary>恢复被隐藏的 VN UI。</summary>
    private void RestoreVNUI()
    {
        if (!uiHidden || uiDoc == null) return;
        uiHidden = false;

        foreach (var kv in hiddenUiSnapshots)
        {
            if (kv.Key == null) continue;
            kv.Key.style.display = kv.Value;
        }
        hiddenUiSnapshots.Clear();
    }

    /// <summary>打开站长日志（独立面板，标题/VN 通用）。暂停 VN 场景 BGM 交给日志面板自行处理。</summary>
    private void OpenArchivePanel()
    {
        CloseAllPanels();
        if (menuBar != null) menuBar.style.display = DisplayStyle.None;
        // 暂停 VN 场景 BGM，避免与日志氛围曲叠放
        VNAudioManager.Instance?.StopBGM(0.3f);
        var archive = TitleArchiveUI.EnsureInstance();
        archive.OnClosed -= ResumeVNSceneAudio;
        archive.OnClosed += ResumeVNSceneAudio;
        archive.Show();
    }

    /// <summary>日志关闭后：恢复当前场景 BGM。</summary>
    private void ResumeVNSceneAudio()
    {
        if (currentScript != null && currentSceneIndex >= 0 && currentSceneIndex < currentScript.scenes.Length)
        {
            var scene = currentScript.scenes[currentSceneIndex];
            if (!string.IsNullOrEmpty(scene.bgm))
                VNAudioManager.Instance?.PlayBGM(scene.bgm, 0.3f);
        }
        // 恢复菜单栏
        if (menuBar != null && !uiHidden) menuBar.style.display = DisplayStyle.Flex;
    }

    private void ShowConfirmDialog()
    {
        CloseMenuExpanded();
        // Day0 前不弹标准确认——先答题
        if (NeedsQuizForSkip())
        {
            if (prologueQuiz == null || !prologueQuiz.IsOpen)
                RequestSkip();
            return;
        }
        FillDialogSummary(confirmDialog, "confirm-summary");
        if (confirmDialog != null)
            confirmDialog.style.display = DisplayStyle.Flex;
    }

    /// <summary>写入副标题（第N话 · 标题）与内容框（当前台词预览），跳过确认专用。</summary>
    private void FillDialogSummary(VisualElement dialog, string summaryName)
    {
        if (dialog == null) return;
        var epHead = dialog.Q<Label>("confirm-eptitle");
        var sum = dialog.Q<Label>(summaryName);
        if (sum == null) return;
        if (currentScript != null && !string.IsNullOrEmpty(currentScriptName))
        {
            int ep = GetEpisodeNumber(currentScriptName);
            string title = GetEpisodeTitle(currentScriptName);
            if (epHead != null) epHead.text = "第" + ep + "话 · " + title;
            string preview = "";
            if (currentSceneIndex >= 0 && currentSceneIndex < currentScript.scenes.Length)
            {
                var scene = currentScript.scenes[currentSceneIndex];
                if (currentDialogueIndex >= 0 && currentDialogueIndex < scene.d.Length)
                    preview = scene.d[currentDialogueIndex].text ?? "";
            }
            if (string.IsNullOrEmpty(preview)) preview = "（本幕暂无台词）";
            if (preview.Length > 80) preview = preview.Substring(0, 80) + "…";
            sum.text = preview;
        }
        else
        {
            if (epHead != null) epHead.text = "";
            sum.text = "未在剧情中。";
        }
    }

    private void ToggleBacklog()
    {
        if (vnBacklog.IsOpen)
        {
            vnBacklog.ToggleBacklog();
            menuBar.style.display = DisplayStyle.Flex;
        }
        else
        {
            CloseAllPanels();
            menuBar.style.display = DisplayStyle.None;
            vnBacklog.ToggleBacklog();
        }
    }

    private void CloseAllPanels()
    {
        if (vnBacklog != null && vnBacklog.IsOpen)
            vnBacklog.ToggleBacklog();
        if (saveLoadUI != null && saveLoadUI.IsOpen)
            saveLoadUI.ClosePanel();
        CloseMenuExpanded();
        menuBar.style.display = DisplayStyle.Flex;
    }

    private void LoadFromSave(VNSaveData saveData)
    {
        StopAutoPlay();

        currentScriptName = saveData.scriptName;
        currentScript = jsonParser.LoadScript(currentScriptName);
        if (currentScript == null)
        {
            Debug.LogError("[VN] Failed to load script for save: " + saveData.scriptName);
            return;
        }

        currentSceneIndex = saveData.sceneIndex;
        currentDialogueIndex = saveData.dialogueIndex;
        ResetProgressShown();
        isScriptRunning = true;

        vnBacklog?.Clear();

        if (currentSceneIndex < currentScript.scenes.Length)
        {
            var scene = currentScript.scenes[currentSceneIndex];
            if (!string.IsNullOrEmpty(saveData.bgName))
                backgroundManager?.SetBackgroundImmediate(saveData.bgName);
            else if (!string.IsNullOrEmpty(scene.bg))
                backgroundManager?.SetBackgroundImmediate(scene.bg);

            if (!string.IsNullOrEmpty(saveData.bgmName))
                VNAudioManager.Instance?.PlayBGM(saveData.bgmName);
            else if (!string.IsNullOrEmpty(scene.bgm))
                VNAudioManager.Instance?.PlayBGM(scene.bgm);
        }

        characterSpriteManager?.ClearAll();
        ShowCurrentDialogue();
    }

    private bool uiHidden;
    private readonly Dictionary<VisualElement, StyleEnum<DisplayStyle>> hiddenUiSnapshots = new Dictionary<VisualElement, StyleEnum<DisplayStyle>>();

    /// <summary>进 VN 1 秒后预热平板（隐藏态）：把 GameMainUI 重型构建挪出退出关键路径（退出卡死根治）。</summary>
    private System.Collections.IEnumerator PreloadTabletSoon()
    {
        yield return new WaitForSecondsRealtime(1f);
        GameMainUI.Preload();
    }

    /// <summary>分镜强制播放中（开场/话标题卡）：输入一律屏蔽——所有拦截点统一走此判断。</summary>
    private bool IsCinematicBlocking() => openingLogoActive || episodeCardActive;

    private void Update()
    {
        clickedButtonThisFrame = false; // 每帧重置按钮标记

        // 应用失焦（如 Win+S 截图、切窗口）时不处理键鼠，避免 Shift 被系统快捷键误触发倍速
        if (!Application.isFocused)
        {
            spaceHeld = false;
            spaceSpeedActive = false;
            if (fastFwdBadge != null) fastFwdBadge.style.display = DisplayStyle.None;
            return;
        }

        // 面板关闭时恢复菜单栏（放在各 return 之前：开场/Win/新闻等提前返回后也保证自愈）
        if (menuBar != null && menuBar.style.display == DisplayStyle.None && !uiHidden && !isFromTitleScreenMode)
        {
            bool anyPanelOpen = (vnBacklog != null && vnBacklog.IsOpen) ||
                                (saveLoadUI != null && saveLoadUI.IsOpen) ||
                                (prologueQuiz != null && prologueQuiz.IsOpen) ||
                                (confirmDialog != null && confirmDialog.style.display == DisplayStyle.Flex) ||
                                (exitDialog != null && exitDialog.style.display == DisplayStyle.Flex);
            if (!anyPanelOpen)
                menuBar.style.display = DisplayStyle.Flex;
        }

        // 开场/话标题卡分镜强制播放：期间屏蔽一切输入（点击/按键均不跳过，播完自动进入）
        if (IsCinematicBlocking())
        {
            return;
        }

        // Win 键（Win+S 截图等系统组合）按住期间：禁用 Shift 倍速，防止系统快捷键误触
        if (WinHeld())
        {
            if (fastFwdBadge != null) fastFwdBadge.style.display = DisplayStyle.None;
            ffNextAt = 0f;
            return;
        }

        // 右键：隐藏/恢复全部 VN UI（背景保留），隐藏状态下点击仍可推进
        if (LegacyInputAdapter.GetMouseButtonDown(1))
        {
            if (uiHidden)
                RestoreVNUI();
            else
                HideVNUI();
            return;
        }

        // ESC：全屏新闻时关闭新闻，其余按面板优先级处理
        if (LegacyInputAdapter.GetKeyDown(KeyCode.Escape) || KeyBindings.IsDown(KeyBindings.Action.OpenMenu))
        {
            if (fullScreenNews != null && fullScreenNews.IsActive)
            {
                fullScreenNews.Close();
                return;
            }
            if (confirmDialog != null && confirmDialog.style.display == DisplayStyle.Flex)
            {
                confirmDialog.style.display = DisplayStyle.None;
                return;
            }
            if (exitDialog != null && exitDialog.style.display == DisplayStyle.Flex)
            {
                exitDialog.style.display = DisplayStyle.None;
                return;
            }
            if (prologueQuiz != null && prologueQuiz.IsOpen)
            {
                // 答题中 ESC = 放弃跳过（不通过）
                prologueQuiz.CancelFromOutside();
                return;
            }
            if (vnBacklog != null && vnBacklog.IsOpen)
            {
                vnBacklog.ToggleBacklog();
                SuppressNextAdvance();
                return;
            }
            if (saveLoadUI != null && saveLoadUI.IsOpen)
            {
                saveLoadUI.ClosePanel();
                // 从标题界面进入的读档模式：按下ESC直接返回标题
                if (isFromTitleScreenMode)
                {
                    isFromTitleScreenMode = false;
                    if (Camera.main != null) Camera.main.backgroundColor = Color.black;
                    UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScreen");
                }
                return;
            }
            // ESC 默认 = 退出确认（跳过走菜单「跳过」按钮）
            ShowExitDialog();
            return;
        }

        // 全屏新闻显示时忽略其他输入
        if (fullScreenNews != null && fullScreenNews.IsActive) return;

        if (!isScriptRunning) return;

        // F5：切换Auto模式
        if (LegacyInputAdapter.GetKeyDown(KeyCode.F5) || KeyBindings.IsDown(KeyBindings.Action.ToggleAuto))
        {
            ToggleAutoPlay();
            return;
        }

        // Shift+→：快进下一句（快退已按需求移除）
        bool shiftHeld = LegacyInputAdapter.GetKey(KeyCode.LeftShift) || LegacyInputAdapter.GetKey(KeyCode.RightShift);
        if (shiftHeld)
        {
            if (LegacyInputAdapter.GetKeyDown(KeyCode.RightArrow) || KeyBindings.IsDown(KeyBindings.Action.SkipForward))
            {
                NextDialogue();
                return;
            }
        }

        // Shift 倍速模式：打字加速由 TypewriterEffect 负责，这里做句间自动快进 + 倍速标
        bool ffActive = shiftHeld && !AdvanceBlockedByPanels() &&
                        !(fullScreenNews != null && fullScreenNews.IsActive);
        if (fastFwdBadge != null)
        {
            fastFwdBadge.style.display = ffActive ? DisplayStyle.Flex : DisplayStyle.None;
            if (ffActive)
            {
                // 纯 Shift（只按住 Shift、无其他键同时按下）：徽标移到屏幕顶部中央；
                // Shift+其他键（如 Shift+→ 快进）保持右上原位
                if (!AnyOtherHeldKey())
                {
                    fastFwdBadge.style.top = 10;
                    fastFwdBadge.style.right = StyleKeyword.Auto;
                    fastFwdBadge.style.left = new Length(50f, LengthUnit.Percent);
                    fastFwdBadge.style.translate = new Translate(new Length(-50f, LengthUnit.Percent), 0f);
                }
                else
                {
                    fastFwdBadge.style.top = 112;
                    fastFwdBadge.style.left = StyleKeyword.Auto;
                    fastFwdBadge.style.right = 10;
                    fastFwdBadge.style.translate = new Translate(0f, 0f);
                }
            }
        }
        if (ffActive)
        {
            bool typing = dialogueBox != null && dialogueBox.IsTyping();
            if (!typing && Time.unscaledTime >= ffNextAt)
            {
                NextDialogue();
                ffNextAt = Time.unscaledTime + 0.15f;
            }
        }
        else
        {
            ffNextAt = 0f;
        }

        // 鼠标推进已迁移到 OnRootPointerDown（UI Toolkit 事件驱动，帧序安全）。
        // 键盘：空格短按推进 / 长按加速；Shift+方向 = 快进/回退（上方已处理）
        bool spaceDown = LegacyInputAdapter.GetKey(KeyCode.Space);
        if (spaceDown && !spaceHeld)
        {
            spaceHeld = true;
            spaceHoldStart = Time.unscaledTime;
            spaceSpeedActive = false;
            spaceSpeedNextAt = 0f;
            if (!AdvanceBlockedByPanels())
            {
                StopAutoPlay();
                if (dialogueBox != null && dialogueBox.IsTyping())
                    dialogueBox.SkipTyping();
                else
                    NextDialogue();
            }
        }
        else if (spaceDown && spaceHeld && !spaceSpeedActive &&
                 Time.unscaledTime - spaceHoldStart >= SpaceSpeedThreshold)
        {
            spaceSpeedActive = true;
            spaceSpeedNextAt = Time.unscaledTime; // 到点立即推进，不留空窗
        }
        if (!spaceDown && spaceHeld)
        {
            spaceHeld = false;
            spaceSpeedActive = false;
        }
        if (spaceSpeedActive && isScriptRunning && Time.unscaledTime >= spaceSpeedNextAt)
        {
            if (!AdvanceBlockedByPanels())
            {
                if (dialogueBox != null && dialogueBox.IsTyping())
                {
                    dialogueBox.SkipTyping();
                    // skip 后短停顿就推进——消掉「句子显示完干等一拍」的顿挫
                    spaceSpeedNextAt = Time.unscaledTime + 0.12f;
                }
                else
                {
                    NextDialogue();
                    spaceSpeedNextAt = Time.unscaledTime + SpaceSpeedInterval;
                }
            }
        }
    }

    private bool AdvanceBlockedByPanels()
    {
        if (IsCinematicBlocking()) return true; // 分镜强制播放：空格/长按/点击不可穿透
        if (confirmDialog != null && confirmDialog.style.display == DisplayStyle.Flex) return true;
        if (exitDialog != null && exitDialog.style.display == DisplayStyle.Flex) return true;
        if (optionsContainer != null && optionsContainer.style.display == DisplayStyle.Flex) return true;
        if (vnBacklog != null && vnBacklog.IsOpen) return true;
        if (saveLoadUI != null && saveLoadUI.IsOpen) return true;
        if (prologueQuiz != null && prologueQuiz.IsOpen) return true;
        return false;
    }

    /// <summary>
    /// 鼠标推进唯一入口（事件 Bubble 阶段：按钮自身 handler 先执行并 StopImmediatePropagation，
    /// 因此只有"空白区域点击"才会 Bubble 到这里。彻底消除命中链误判）。
    /// </summary>
    private float lastAdvanceTime;
    private const float AdvanceCooldown = 0.15f; // 防抖间隔（秒）

    private void OnRootClick(ClickEvent evt)
    {
        if (evt.button != 0) return; // 仅左键

        // 开场/话标题卡分镜强制播放：左键点击冒泡到这里也不推进
        if (IsCinematicBlocking()) return;

        // 防抖：避免快速双击导致打字机被跳过
        if (Time.unscaledTime - lastAdvanceTime < AdvanceCooldown) return;

        // 按钮已先处理并设了标记 → 检查标记跳过
        if (clickedButtonThisFrame)
        {
            clickedButtonThisFrame = false;
            return;
        }

        // 菜单展开时点击空白：仅收起菜单，不推进对话
        if (menuExpanded)
        {
            CloseMenuExpanded();
            return;
        }

        // UI 隐藏状态下（右键隐藏）点击推进
        if (uiHidden)
        {
            AdvanceOnClick();
            return;
        }
        if (confirmDialog != null && confirmDialog.style.display == DisplayStyle.Flex) return;
        if (exitDialog != null && exitDialog.style.display == DisplayStyle.Flex) return;
        if (optionsContainer != null && optionsContainer.style.display == DisplayStyle.Flex) return;
        if (vnBacklog != null && vnBacklog.IsOpen) return;
        if (saveLoadUI != null && saveLoadUI.IsOpen) return;
        if (prologueQuiz != null && prologueQuiz.IsOpen) return;
        if (fullScreenNews != null && fullScreenNews.IsActive) return;
        if (resumeDialog != null && resumeDialog.style.display == DisplayStyle.Flex) return;

        AdvanceOnClick();
    }

    private void AdvanceOnClick()
    {
        lastAdvanceTime = Time.unscaledTime;
        StopAutoPlay();
        if (dialogueBox != null && dialogueBox.IsTyping())
            dialogueBox.SkipTyping();
        else
            NextDialogue();
    }

    /// <summary>实时 Pick 检测指针是否在任意交互 UI（按钮/菜单/面板）上方。</summary>
    private bool IsPointerOverAnyUI()
    {
        if (uiDoc == null) return false;
        var panel = uiDoc.rootVisualElement.panel;
        if (panel == null) return false;
        var local = RuntimePanelUtils.ScreenToPanel(panel, LegacyInputAdapter.MousePosition);
        var picked = panel.Pick(local);
        while (picked != null)
        {
            if (picked is UnityEngine.UIElements.Button) return true;
            if (picked == menuBar || picked == menuExpandedContainer) return true;
            picked = picked.parent;
        }
        return false;
    }

    public void StartScript(string scriptName)
    {
        if (string.IsNullOrEmpty(scriptName)) return;
        if (jsonParser == null)
        {
            Debug.LogError("[VN] jsonParser is null — cannot start " + scriptName);
            return;
        }
        currentScript = jsonParser.LoadScript(scriptName);
        if (currentScript == null) { Debug.LogError("Failed to load: " + scriptName); return; }
        currentScriptName = scriptName;
        currentSceneIndex = 0;
        currentDialogueIndex = 0;
        ResetProgressShown();
        isScriptRunning = true;

        // 站长日志解锁绑定：进入序章脚本时解锁对应 CG/角色/列车
        TitleArchiveUI.AutoUnlock(scriptName);

        vnBacklog?.Clear();
        characterSpriteManager?.ClearAll();
        variables.Clear();

        if (currentScript.scenes.Length > 0)
        {
            var firstScene = currentScript.scenes[0];
            if (!string.IsNullOrEmpty(firstScene.bg))
                backgroundManager?.SetBackgroundImmediate(firstScene.bg);
            if (!string.IsNullOrEmpty(firstScene.bgm))
                VNAudioManager.Instance?.PlayBGM(firstScene.bgm);
        }

        // 序章首次：先全屏 Logo+制作信息（非 VN 对话），再进话标题卡
        if (scriptName == "prologue_01_news" && !openingLogoShown)
        {
            openingLogoShown = true;
            ShowOpeningLogo(() => ShowEpisodeTitleCard(scriptName, () => ShowCurrentDialogue()));
        }
        else
        {
            ShowEpisodeTitleCard(scriptName, () => ShowCurrentDialogue());
        }

        // 自然到达 Day0（获得0721 起点）后，后续跳过走标准确认而非答题
        if (scriptName == "prologue_02_day0" || scriptName.StartsWith("prologue_03") ||
            scriptName.StartsWith("prologue_04") || scriptName.StartsWith("prologue_05") ||
            scriptName.StartsWith("prologue_06") || scriptName.StartsWith("prologue_07") ||
            scriptName.StartsWith("prologue_08") || scriptName.StartsWith("prologue_09") ||
            scriptName.StartsWith("prologue_10") || scriptName.StartsWith("post_"))
        {
            if (PlayerPrefs.GetInt("VN_Got0721", 0) != 1)
            {
                PlayerPrefs.SetInt("VN_Got0721", 1);
                PlayerPrefs.Save();
            }
        }
    }

    private bool openingLogoShown;
    private bool openingLogoActive; // 分镜进行中：强制播放，挡住一切输入

    /// <summary>
    /// 正式进入序章前：全屏分镜（Logo 图 → 金标题+金线 → 制作信息），逐条淡入淡出。
    /// 暖深棕底，非 BA 冷蓝；任意点击跳过整段。
    /// </summary>
    private void ShowOpeningLogo(System.Action onDone)
    {
        if (uiDoc == null) { onDone?.Invoke(); return; }
        openingLogoActive = true;
        var root = uiDoc.rootVisualElement;
        var overlay = new VisualElement { name = "opening-logo" };
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0;
        overlay.style.right = 0; overlay.style.bottom = 0;
        // 暖深棕近黑（本作）
        overlay.style.backgroundColor = new Color(0.039f, 0.027f, 0.02f, 1f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        overlay.pickingMode = PickingMode.Position;
        root.Add(overlay);
        overlay.style.opacity = 0f; // 显式初始化：开场黑幕从无到有淡入（也保证 opacity.value 恒可读）

        var fontDef = new FontDefinition { font = gameFont };
        bool finished = false;
        Coroutine running = null;

        void FinishAll()
        {
            if (finished) return;
            finished = true;
            // openingLogoActive 保持 true 直到淡出结束——淡出期间同样屏蔽一切输入
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(FadeOutAndFinish(overlay, onDone));
        }
        // 强制播放：不注册点击/按键跳过，全程播完自动进入

        // 分镜舞台：始终居中同一槽位，内容逐条换
        var stage = new VisualElement();
        stage.style.alignItems = Align.Center;
        stage.style.justifyContent = Justify.Center;
        stage.style.opacity = 0f;
        stage.pickingMode = PickingMode.Ignore;
        overlay.Add(stage);

        running = StartCoroutine(OpeningLogoSequence(overlay, stage, fontDef, FinishAll));
    }

    private IEnumerator OpeningLogoSequence(VisualElement overlay, VisualElement stage, FontDefinition fontDef, System.Action done)
    {
        // —— 分镜 0：黑幕整体淡入（从上一画面平滑切入，不再硬切）——
        yield return FadeElement(overlay, 0f, 1f, 0.45f);

        // —— 分镜 1：Logo 图（有图则跳过文字标题镜）——
        stage.Clear();
        // 资源按 Sprite 导入（textureType=8），与 LoginManager 一致先 Load<Sprite>
        var logoSprite = Resources.Load<Sprite>("UI/Login/title_logo");
        Texture2D logoTex = logoSprite != null ? logoSprite.texture : Resources.Load<Texture2D>("UI/Login/title_logo");
        if (logoTex != null)
        {
            var logo = new VisualElement();
            logo.style.backgroundImage = new StyleBackground(Background.FromTexture2D(logoTex));
            // 源图 1280x720，居中偏上、留呼吸空间
            logo.style.width = 640;
            logo.style.height = 360;
            logo.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            logo.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            logo.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            logo.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            logo.pickingMode = PickingMode.Ignore;
            stage.Add(logo);

            yield return FadeStage(stage, 0f, 1f, 0.7f);
            yield return new WaitForSecondsRealtime(1.4f);
            yield return FadeStage(stage, 1f, 0f, 0.55f);
            // 有 logo：不再播「金标题」文字镜
        }
        else
        {
            // 无图：直接进金标题 + 金线
            yield return FadeStage(stage, 0f, 1f, 0.4f);
            yield return FadeStage(stage, 1f, 0f, 0.3f);

            stage.Clear();
            var title = new Label("铁路复兴：沙能冲击");
            title.style.fontSize = 52;
            title.style.color = new Color(1f, 0.88f, 0.62f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityFontDefinition = fontDef;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.pickingMode = PickingMode.Ignore;
            stage.Add(title);

            var gold = new VisualElement();
            gold.style.width = 220;
            gold.style.height = 3;
            gold.style.backgroundColor = new Color(1f, 0.78f, 0.4f, 0.95f);
            gold.style.marginTop = 16;
            gold.pickingMode = PickingMode.Ignore;
            stage.Add(gold);

            yield return FadeStage(stage, 0f, 1f, 0.65f);
            yield return new WaitForSecondsRealtime(1.3f);
            yield return FadeStage(stage, 1f, 0f, 0.55f);
        }

        // —— 制作信息（强制播完，自动进入剧情）——
        stage.Clear();
        var credit = new Label("由 NMDX0721 开发\nRailway Renaissance: Sand Energy Impact");
        credit.style.fontSize = 30;
        credit.style.color = new Color(1f, 1f, 1f, 0.6f);
        credit.style.unityFontDefinition = fontDef;
        credit.style.unityTextAlign = TextAnchor.MiddleCenter;
        credit.style.whiteSpace = WhiteSpace.Normal;
        credit.pickingMode = PickingMode.Ignore;
        stage.Add(credit);

        yield return FadeStage(stage, 0f, 1f, 0.65f);
        yield return new WaitForSecondsRealtime(1.6f);

        done?.Invoke();
    }

    private IEnumerator FadeStage(VisualElement stage, float from, float to, float dur)
    {
        yield return FadeElement(stage, from, to, dur);
    }

    /// <summary>开场 overlay 整体淡出后移除（分镜/credit 退场统一走这里）。</summary>
    private IEnumerator FadeOutAndFinish(VisualElement overlay, System.Action onDone)
    {
        // overlay 已在创建时显式初始化 opacity（=0），此后一直由 FadeElement 维护，.value 恒为当前值
        yield return FadeElement(overlay, overlay.style.opacity.value, 0f, 0.45f);
        overlay.RemoveFromHierarchy();
        openingLogoActive = false;
        // 吞掉跨越开场按住的空格等：置为「已按住」，用户松开后重新按下才推进对话
        spaceHeld = true;
        spaceSpeedActive = false;
        onDone?.Invoke();
    }

    private IEnumerator FadeElement(VisualElement el, float from, float to, float dur)
    {
        if (el == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            el.style.opacity = a;
            yield return null;
        }
        el.style.opacity = to;
    }

    /// <summary>底部进度条：当前句在全本中的位置（0~1）。</summary>
    private float progressShown = 0f;
    private bool progressSeeking; // 进度条拖动中：抑制日期地点卡
    private bool episodeCardActive; // 话标题卡强制播放中：屏蔽一切输入
    private bool locCardAutoAdvance; // 日期地点卡：隐退后自动推进

    private void ResetProgressShown() { progressShown = 0f; }

    private void UpdateProgress()
    {
        if (dialogueBox == null || currentScript?.scenes == null) return;
        int total = 0;
        foreach (var s in currentScript.scenes) total += s.d?.Length ?? 0;
        if (total <= 0) { dialogueBox.SetProgress(0f); return; }
        int cur = 0;
        for (int i = 0; i < currentSceneIndex && i < currentScript.scenes.Length; i++)
            cur += currentScript.scenes[i].d?.Length ?? 0;
        cur += currentDialogueIndex + 1; // 计入当前正在显示的句（否则末句停在 99%）
        if (cur > total) cur = total;
        float p = (float)cur / total;
        if (p < progressShown) p = progressShown; // 正常推进单调不减（读档/跳转/重播处已 ResetProgressShown）
        progressShown = p;
        dialogueBox.SetProgress(p);
    }

    /// <summary>进度条 seek：只允许跳到已解锁部分（progressShown=历史到达上限），反查句索引后复用回顾跳转。</summary>
    private void HandleProgressSeek(float ratio)
    {
        if (!isScriptRunning || currentScript?.scenes == null) return;
        int total = 0;
        foreach (var s in currentScript.scenes) total += s.d?.Length ?? 0;
        if (total <= 0) return;
        ratio = Mathf.Clamp01(ratio);
        if (ratio > progressShown + 0.005f) return; // 未解锁区域不响应
        int target = Mathf.Clamp(Mathf.RoundToInt(ratio * total) - 1, 0, total - 1);
        int acc = 0;
        for (int si = 0; si < currentScript.scenes.Length; si++)
        {
            int len = currentScript.scenes[si].d?.Length ?? 0;
            if (target < acc + len)
            {
                OnBacklogEntryClicked(si, target - acc);
                return;
            }
            acc += len;
        }
        int last = currentScript.scenes.Length - 1;
        var d = currentScript.scenes[last].d;
        OnBacklogEntryClicked(last, Mathf.Max(0, (d?.Length ?? 1) - 1));
    }

    /// <summary>进度条松手后补显示：当前句若是日期地点卡则补弹（拖动期间被抑制）。</summary>
    private void RefreshAfterSeek()
    {
        if (!isScriptRunning || currentScript?.scenes == null) return;
        if (currentSceneIndex < 0 || currentSceneIndex >= currentScript.scenes.Length) return;
        var scene = currentScript.scenes[currentSceneIndex];
        if (scene.d == null || currentDialogueIndex < 0 || currentDialogueIndex >= scene.d.Length) return;
        var entry = scene.d[currentDialogueIndex];
        if (entry.t == "loc")
        {
            dialogueBox?.Hide();
            ShowLocationCard(entry.text);
        }
    }

    /// <summary>话标题卡：黑底 + 话数 + 标题，2秒后淡出。</summary>
    private void ShowEpisodeTitleCard(string scriptName, System.Action onDone)
    {
        if (uiDoc == null) { onDone?.Invoke(); return; }
        var root = uiDoc.rootVisualElement;
        var overlay = new VisualElement { name = "episode-title-card" };
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0;
        overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.92f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        overlay.pickingMode = PickingMode.Position;
        root.Add(overlay);

        // BA风格标题条：半透明浅色带
        var band = new VisualElement();
        band.style.width = new Length(80, LengthUnit.Percent);
        band.style.paddingTop = 30; band.style.paddingBottom = 30;
        band.style.alignItems = Align.Center;
        band.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
        overlay.Add(band);

        int epNum = GetEpisodeNumber(scriptName);
        string epTitle = GetEpisodeTitle(scriptName);

        var numLabel = new Label("第" + epNum + "话");
        numLabel.style.fontSize = 18;
        numLabel.style.color = new Color(1f, 0.78f, 0.4f, 0.8f);
        numLabel.style.unityFontDefinition = new FontDefinition { font = gameFont };
        numLabel.style.marginBottom = 8;
        band.Add(numLabel);

        var titleLabel = new Label(epTitle);
        titleLabel.style.fontSize = 32;
        titleLabel.style.color = new Color(1f, 0.95f, 0.88f, 1f);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityFontDefinition = new FontDefinition { font = gameFont };
        band.Add(titleLabel);

        // 强制播放：不注册点击跳过（同开场策略），期间 episodeCardActive 屏蔽一切输入
        episodeCardActive = true;

        // 2秒后自动消失
        StartCoroutine(FadeOutTitleCard(overlay, 2f, () =>
        {
            overlay.RemoveFromHierarchy();
            episodeCardActive = false;
            // 吞掉跨卡按住的空格：置为已按住，松开重按才推进
            spaceHeld = true;
            spaceSpeedActive = false;
            onDone?.Invoke();
        }));
    }

    private IEnumerator FadeOutTitleCard(VisualElement el, float delay, System.Action onDone)
    {
        yield return new WaitForSeconds(delay);
        // 简单淡出：0.5秒
        float t = 0;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float a = 1f - (t / 0.5f);
            el.style.backgroundColor = new Color(0, 0, 0, 0.92f * a);
            yield return null;
        }
        onDone?.Invoke();
    }

    public void NextDialogue()
    {
        if (!isScriptRunning || currentScript == null) return;
        dialogueBox?.HideContinueIndicator();
        currentDialogueIndex++;

        if (currentSceneIndex >= currentScript.scenes.Length) { EndScript(); return; }

        var scene = currentScript.scenes[currentSceneIndex];
        if (currentDialogueIndex >= scene.d.Length)
        {
            currentSceneIndex++;
            currentDialogueIndex = 0;
            if (currentSceneIndex >= currentScript.scenes.Length) { EndScript(); return; }

            var newScene = currentScript.scenes[currentSceneIndex];
            if (!string.IsNullOrEmpty(newScene.bg))
            {
                var transType = ParseTransition(newScene.transition);
                backgroundManager?.SetBackground(newScene.bg, transType);
            }
            if (!string.IsNullOrEmpty(newScene.bgm))
                VNAudioManager.Instance?.PlayBGM(newScene.bgm);

            characterSpriteManager?.ClearAll();
        }

        // 自动书签推进：记录当前位置（仅正常播放入口，非跳转）
        if (!string.IsNullOrEmpty(currentScriptName) && VN_ReplayInjected == false)
            AutoBookmarkTrack();
        ShowCurrentDialogue();
    }

    /// <summary>更新当前话的自动书签位置。</summary>
    private void AutoBookmarkTrack()
    {
        if (currentScript == null || string.IsNullOrEmpty(currentScriptName)) return;
        var scene = currentScript.scenes[currentSceneIndex];
        if (scene == null || currentDialogueIndex >= scene.d.Length) return;
        var entry = scene.d[currentDialogueIndex];
        string preview = entry != null ? entry.text : "";
        if (entry != null && entry.t == "cg" && string.IsNullOrEmpty(entry.text))
            preview = "(CG)";
        BookmarkManager.UpdateAutoBookmark(
            currentScriptName,
            GetEpisodeTitle(currentScriptName),
            GetEpisodeNumber(currentScriptName),
            currentSceneIndex,
            currentDialogueIndex,
            preview);
    }

    private void ShowCurrentDialogue()
    {
        if (currentScript == null || currentSceneIndex >= currentScript.scenes.Length) return;
        var scene = currentScript.scenes[currentSceneIndex];
        // 通话条：按场景 phone 字段开关
        if (dialogueBox != null)
        {
            if (string.IsNullOrEmpty(scene.phone))
                dialogueBox.SetPhoneCall("__off__");
            else
                dialogueBox.SetPhoneCall(scene.phone == "1" || scene.phone == "true" ? "" : scene.phone);
        }
        if (currentDialogueIndex >= scene.d.Length) return;
        var entry = scene.d[currentDialogueIndex];

        // 底部剧情进度条：当前句/全本句数（所有推进路径都汇聚到这里，单点刷新）
        UpdateProgress();

        // 切换对话时始终隐藏 boot screen，boot 类型会在下面重新显示
        HideBootScreen();
        HideLocationCard();

        // Check condition - skip this entry if condition not met
        if (!EvaluateCondition(entry.condition))
        {
            NextDialogue();
            return;
        }

        // Apply setValue when entering this dialogue
        ApplySetValue(entry.setValue);

        // 空旁白（标题卡占位行）：不弹对话框，直接下一条
        if ((entry.t == "n" || string.IsNullOrEmpty(entry.s)) && string.IsNullOrWhiteSpace(entry.text))
        {
            NextDialogue();
            return;
        }

        if (entry.t == "c")
        {
            ShowOptions(entry.opts);
        }
        else if (entry.t == "scroll")
        {
            // 全屏滚动新闻模式
            dialogueBox?.Hide();
            HideOptions();
            if (menuBar != null) menuBar.style.display = DisplayStyle.None;
            fullScreenNews?.Show(entry.text);
            vnBacklog?.AddEntry(entry.s, entry.text, currentSceneIndex, currentDialogueIndex);
        }
        else if (entry.t == "boot")
        {
            // 系统启动画面——居中科技感显示
            dialogueBox?.Hide();
            HideOptions();
            if (menuBar != null) menuBar.style.display = DisplayStyle.None;
            ShowBootScreen(entry.text, entry.s);
            vnBacklog?.AddEntry(entry.s, entry.text, currentSceneIndex, currentDialogueIndex);
            return; // 不显示常规对话，由BootScreen处理点击
        }
        else if (entry.t == "cg")
        {
            // CG 插画——size="full"全屏展示 / size="small"居中小插画; text=Resources/cg/ 下的图片名
            dialogueBox?.Hide();
            HideOptions();
            if (menuBar != null) menuBar.style.display = DisplayStyle.None;
            bool isSmall = entry.size == "small";
            ShowCgScreen(entry.text, isSmall);
            TitleArchiveUI.UnlockCG(entry.text);
            vnBacklog?.AddEntry(entry.s, entry.text, currentSceneIndex, currentDialogueIndex);
            return;
        }
        else if (entry.t == "special")
        {
            // 特殊游戏操作指令
            HandleSpecialCommand(entry.text);
        }
        else if (entry.t == "loc")
        {
            // 时间/地点标注：居中卡片显示，不用对话框；点击/空格照常推进
            vnBacklog?.AddEntry(entry.s, entry.text, currentSceneIndex, currentDialogueIndex);
            if (progressSeeking) return; // 拖动扫过：不弹卡不切界面（松手后由 RefreshAfterSeek 补显示）
            dialogueBox?.Hide();
            HideOptions();
            ShowLocationCard(entry.text);
        }
        else
        {
            HideOptions();
            // 说话者显示名：主角替换为别名（如果设置了）
            var displaySpeaker = ResolveSpeakerName(entry.s);
            dialogueBox?.ShowDialogue(displaySpeaker, entry.text);

            bool isNarration = entry.t == "n" || string.IsNullOrEmpty(entry.s);
            if (isNarration)
            {
                // 旁白不显示立绘
                characterSpriteManager?.ClearAll();
            }
            else
            {
                // 条目级chars/e优先，回退到场景级默认值
                var entryChars = entry.chars != null && entry.chars.Length > 0 ? entry.chars : scene.chars;
                var entryEmotion = !string.IsNullOrEmpty(entry.e) ? entry.e : scene.e;
                if (entryChars != null && entryChars.Length > 0)
                    characterSpriteManager?.UpdateDisplay(entryChars, entryEmotion, entry.s, entry.reactions);
            }

            vnBacklog?.AddEntry(displaySpeaker, entry.text, currentSceneIndex, currentDialogueIndex);
        }
    }

    private void ShowOptions(OptionData[] options)
    {
        if (optionsContainer == null || options == null) return;
        optionsContainer.Clear();
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.Flex;
        optionsContainer.style.display = DisplayStyle.Flex;
        dialogueBox?.Hide();
        StopAutoPlay();

        foreach (var opt in options)
        {
            if (!EvaluateCondition(opt.condition)) continue;

            var optCopy = opt;
            var btn = new UnityEngine.UIElements.Button(() =>
            {
                ApplySetValue(optCopy.setValue);
                HideOptions();
                currentSceneIndex = optCopy.next;
                currentDialogueIndex = 0;
                ResetProgressShown();
                ShowCurrentDialogue();
            })
            { text = opt.text };
            // 纯代码绘制：不用图片素材/不用 USS 类（用户要求）
            btn.pickingMode = PickingMode.Position;
            AddCursorHover(btn);
            btn.style.width = new Length(700, LengthUnit.Pixel);
            btn.style.height = new Length(76, LengthUnit.Pixel);
            btn.style.marginBottom = new Length(16, LengthUnit.Pixel);
            btn.style.fontSize = 32;
            btn.style.color = new Color(1f, 0.95f, 0.85f, 1f);
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.unityTextOutlineWidth = 2;
            btn.style.unityTextOutlineColor = new Color(0.15f, 0.08f, 0.05f, 0.9f);
            // 深棕底 + 金色边框（像素风单选卡）
            btn.style.backgroundColor = new Color(0.14f, 0.09f, 0.05f, 0.94f);
            btn.style.borderTopWidth = 2; btn.style.borderBottomWidth = 2;
            btn.style.borderLeftWidth = 2; btn.style.borderRightWidth = 2;
            btn.style.borderTopColor = new Color(0.82f, 0.62f, 0.35f, 0.7f);
            btn.style.borderBottomColor = new Color(0.82f, 0.62f, 0.35f, 0.7f);
            btn.style.borderLeftColor = new Color(0.82f, 0.62f, 0.35f, 0.7f);
            btn.style.borderRightColor = new Color(0.82f, 0.62f, 0.35f, 0.7f);
            btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
            // hover 反馈
            btn.RegisterCallback<PointerEnterEvent>(evt =>
            {
                btn.style.backgroundColor = new Color(0.26f, 0.17f, 0.09f, 0.98f);
                btn.style.borderTopColor = new Color(1f, 0.85f, 0.5f, 1f);
                btn.style.borderBottomColor = new Color(1f, 0.85f, 0.5f, 1f);
                btn.style.borderLeftColor = new Color(1f, 0.85f, 0.5f, 1f);
                btn.style.borderRightColor = new Color(1f, 0.85f, 0.5f, 1f);
            });
            btn.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                btn.style.backgroundColor = new Color(0.14f, 0.09f, 0.05f, 0.94f);
                btn.style.borderTopColor = new Color(0.82f, 0.62f, 0.35f, 0.7f);
                btn.style.borderBottomColor = new Color(0.82f, 0.62f, 0.35f, 0.7f);
                btn.style.borderLeftColor = new Color(0.82f, 0.62f, 0.35f, 0.7f);
                btn.style.borderRightColor = new Color(0.82f, 0.62f, 0.35f, 0.7f);
            });
            if (gameFont != null)
                btn.style.unityFontDefinition = new FontDefinition { font = gameFont };
            optionsContainer.Add(btn);
        }
    }

    private void HideOptions()
    {
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.None;
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.None;
    }

    private void ShowBootScreen(string text, string speaker)
    {
        if (bootScreen == null)
        {
            var root = uiDoc.rootVisualElement;
            bootScreen = new VisualElement { name = "boot-screen" };
            bootScreen.style.position = Position.Absolute;
            bootScreen.style.top = 0; bootScreen.style.left = 0;
            bootScreen.style.right = 0; bootScreen.style.bottom = 0;
            bootScreen.style.alignItems = Align.Center;
            bootScreen.style.justifyContent = Justify.Center;
            bootScreen.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            bootScreen.pickingMode = PickingMode.Position;
            bootScreen.RegisterCallback<ClickEvent>(e =>
            {
                HideBootScreen();
                NextDialogue();
            });
            root.Add(bootScreen);
        }
        bootScreen.Clear();

        var container = new VisualElement();
        container.style.alignItems = Align.Center;
        container.style.justifyContent = Justify.Center;
        bootScreen.Add(container);

        if (!string.IsNullOrEmpty(speaker))
        {
            var nameLabel = new Label(speaker);
            nameLabel.style.fontSize = 28;
            nameLabel.style.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.marginBottom = 16;
            nameLabel.style.unityFontDefinition = new FontDefinition { font = gameFont };
            container.Add(nameLabel);
        }

        var textLabel = new Label(text);
        textLabel.style.fontSize = 24;
        textLabel.style.color = new Color(0.6f, 0.9f, 1f, 1f);
        textLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        textLabel.style.whiteSpace = WhiteSpace.Normal;
        textLabel.style.unityFontDefinition = new FontDefinition { font = gameFont };
        container.Add(textLabel);

        var continueHint = new Label("点击继续");
        continueHint.style.fontSize = 16;
        continueHint.style.color = new Color(0.3f, 0.8f, 1f, 0.3f);
        continueHint.style.marginTop = 30;
        continueHint.style.unityFontDefinition = new FontDefinition { font = gameFont };
        container.Add(continueHint);

        bootScreen.style.display = DisplayStyle.Flex;
    }

    private void HideBootScreen()
    {
        if (bootScreen != null)
            bootScreen.style.display = DisplayStyle.None;
    }

    /// <summary>CG 插画展示：isSmall=false 全屏黑底 / isSmall=true 居中小插画半透明底。</summary>
    private void ShowCgScreen(string cgName, bool isSmall = false)
    {
        var root = uiDoc.rootVisualElement;

        if (cgScreen == null)
        {
            cgScreen = new VisualElement { name = "cg-screen" };
            cgScreen.style.position = Position.Absolute;
            cgScreen.style.top = 0; cgScreen.style.left = 0;
            cgScreen.style.right = 0; cgScreen.style.bottom = 0;
            cgScreen.style.alignItems = Align.Center;
            cgScreen.style.justifyContent = Justify.Center;
            cgScreen.pickingMode = PickingMode.Position;
            cgScreen.RegisterCallback<ClickEvent>(e =>
            {
                // 任意子元素（含「CG 待生成」Label）点击都关闭
                e.StopImmediatePropagation();
                NextDialogue();
            });
            root.Add(cgScreen);
        }

        cgScreen.Clear();
        cgScreen.style.display = DisplayStyle.Flex;

        // 全屏 vs 小插画：背景色不同
        cgScreen.style.backgroundColor = isSmall
            ? new Color(0, 0, 0, 0.6f)
            : new Color(0, 0, 0, 1f);

        if (!string.IsNullOrEmpty(cgName))
        {
            var tex = Resources.Load<Texture2D>("cg/" + cgName);
            if (tex != null)
            {
                var img = new VisualElement();
                img.name = "cg-image";
                img.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));

                if (isSmall)
                {
                    // 小插画：居中，最大60%宽，保持比例，圆角，阴影
                    img.style.maxWidth = new Length(60, LengthUnit.Percent);
                    img.style.maxHeight = new Length(60, LengthUnit.Percent);
                    img.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
                    img.style.borderTopLeftRadius = 12;
                    img.style.borderTopRightRadius = 12;
                    img.style.borderBottomLeftRadius = 12;
                    img.style.borderBottomRightRadius = 12;
                    img.style.borderTopWidth = 2; img.style.borderTopColor = new Color(1, 1, 1, 0.15f);
                    img.style.borderBottomWidth = 2; img.style.borderBottomColor = new Color(1, 1, 1, 0.15f);
                    img.style.borderLeftWidth = 2; img.style.borderLeftColor = new Color(1, 1, 1, 0.15f);
                    img.style.borderRightWidth = 2; img.style.borderRightColor = new Color(1, 1, 1, 0.15f);
                }
                else
                {
                    // 全屏：contain缩放
                    img.style.flexGrow = 1;
                    img.style.maxWidth = new Length(100, LengthUnit.Percent);
                    img.style.maxHeight = new Length(100, LengthUnit.Percent);
                    img.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
                }
                cgScreen.Add(img);
            }
            else
            {
                Debug.LogWarning("[VN] CG image not found: cg/" + cgName);
                var placeholder = new Label("CG 待生成：『" + cgName + "』");
                placeholder.style.fontSize = 28;
                placeholder.style.color = new Color(1f, 0.8f, 0.4f, 0.9f);
                placeholder.style.unityFontDefinition = new FontDefinition { font = gameFont };
                cgScreen.Add(placeholder);
            }
        }

        var hint = new Label("点击继续");
        hint.style.position = Position.Absolute;
        hint.style.bottom = 24;
        hint.style.right = 36;
        hint.style.fontSize = 16;
        hint.style.color = new Color(1f, 1f, 1f, 0.35f);
        hint.style.unityFontDefinition = new FontDefinition { font = gameFont };
        cgScreen.Add(hint);
    }

    private void HideCgScreen()
    {
        if (cgScreen != null)
            cgScreen.style.display = DisplayStyle.None;
    }

    private void EndScript()
    {
        StopAutoPlay();
        isScriptRunning = false;
        dialogueBox?.SetPhoneCall("__off__");

        // 话完成：销毁当前话的自动书签（从头回看不需书签）
        if (!string.IsNullOrEmpty(currentScriptName))
            BookmarkManager.ClearAutoBookmark(currentScriptName);

        // 检查是否有下一个剧本需要自动加载（序章链）
        if (currentScript != null && !string.IsNullOrEmpty(currentScript.nextScript))
        {
            string next = currentScript.nextScript;
            string curName = currentScriptName;
            currentScript = null;
            currentScriptName = null;
            // 话完成：剧情中不弹重奖励窗，直接节过渡（解锁静默入档）
            var newUnlocks = TitleArchiveUI.TakePendingUnlocks();
            _ = newUnlocks; // 已入库，不在剧情中打断
            ShowEpisodeClear(curName, next);
            return;
        }

        currentScript = null;
        currentScriptName = null;
        HideOptions();
        vnBacklog?.Clear();
        variables.Clear();
        saveLoadUI?.ClosePanel();
        dialogueBox?.Hide();
        characterSpriteManager?.ClearAll();
        VNAudioManager.Instance?.StopBGM();
    }

    /// <summary>奖励弹窗：解锁完成（REWARD ACQUIRED 铁路版）。深棕底 + 金色斜体 + 沙能青发光线。</summary>
    private void ShowRewardPopup(string[] unlocks, System.Action onClose)
    {
        var overlay = new VisualElement { name = "reward-popup" };
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0;
        overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.78f); // 深棕黑遮罩
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        overlay.pickingMode = PickingMode.Position;
        overlay.RegisterCallback<ClickEvent>(e =>
        {
            if (e.target == overlay)
            {
                overlay.RemoveFromHierarchy();
                onClose?.Invoke();
            }
        });
        uiDoc.rootVisualElement.Add(overlay);

        // —— 中央光晕（径向暗→亮） ——
        var glow = new VisualElement();
        glow.style.position = Position.Absolute;
        glow.style.width = 520; glow.style.height = 420;
        glow.style.alignSelf = Align.Center;
        glow.style.backgroundColor = new Color(1f, 0.85f, 0.5f, 0.04f);
        glow.pickingMode = PickingMode.Ignore;
        overlay.Add(glow);

        // —— 主面板：深棕底 + 金色双层边框 ——
        var panel = new VisualElement();
        panel.style.backgroundColor = new Color(0.10f, 0.06f, 0.03f, 0.98f);
        panel.style.minWidth = 520;
        panel.style.paddingLeft = 56; panel.style.paddingRight = 56;
        panel.style.paddingTop = 40; panel.style.paddingBottom = 40;
        panel.style.flexDirection = FlexDirection.Column;
        panel.style.alignItems = Align.Center;
        panel.style.borderTopWidth = 2; panel.style.borderBottomWidth = 2;
        panel.style.borderLeftWidth = 2; panel.style.borderRightWidth = 2;
        panel.style.borderTopColor = new Color(0.82f, 0.66f, 0.4f, 0.9f);
        panel.style.borderBottomColor = new Color(0.82f, 0.66f, 0.4f, 0.9f);
        panel.style.borderLeftColor = new Color(0.82f, 0.66f, 0.4f, 0.9f);
        panel.style.borderRightColor = new Color(0.82f, 0.66f, 0.4f, 0.9f);
        overlay.Add(panel);

        // —— "解锁完成！" 金色斜体大字 ——
        var title = new Label("解锁完成！");
        title.style.fontSize = 36;
        title.style.color = new Color(1f, 0.82f, 0.4f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = new FontDefinition { font = gameFont };
        title.style.marginBottom = 6;
        // 金色描边模拟（text-shadow 在 UI Toolkit 支持有限，用双层 Label 覆盖）
        panel.Add(title);

        // 装饰 ✦ 星光行
        var sparkle = new Label("✦ ✦ ✦");
        sparkle.style.fontSize = 20;
        sparkle.style.color = new Color(1f, 0.85f, 0.5f, 0.5f);
        sparkle.style.unityFontDefinition = new FontDefinition { font = gameFont };
        sparkle.style.marginBottom = 10;
        panel.Add(sparkle);

        // —— 新增解锁清单 ——
        var listLabel = new Label(string.Join("\n", unlocks));
        listLabel.style.fontSize = 22;
        listLabel.style.color = new Color(0.95f, 0.9f, 0.8f, 0.95f);
        listLabel.style.whiteSpace = WhiteSpace.Normal;
        listLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        listLabel.style.unityFontDefinition = new FontDefinition { font = gameFont };
        listLabel.style.marginBottom = 18;
        panel.Add(listLabel);

        // —— 沙能青色发光线（上下各一条） ——
        AddGlowLine(panel);
        panel.Add(new Label(""));
        AddGlowLine(panel);

        // —— 点击继续 ——
        var hint = new Label("点击继续");
        hint.style.fontSize = 16;
        hint.style.color = new Color(0.6f, 0.9f, 1f, 0.7f); // 沙能青
        hint.style.unityFontDefinition = new FontDefinition { font = gameFont };
        hint.style.marginTop = 14;
        panel.Add(hint);
    }

    /// <summary>沙能青色发光横线。</summary>
    private void AddGlowLine(VisualElement parent)
    {
        var line = new VisualElement();
        line.style.width = 300;
        line.style.height = 2;
        line.style.backgroundColor = new Color(0.35f, 0.75f, 1f, 0.7f);
        parent.Add(line);
    }

    /// <summary>
    /// 话末：BA 风两段——①压暗背景 + 右下 To Be Continued… → ②浅色横条 Next Episode + 话标题。
    /// 点击任意处跳过到下一话；否则约 4.5s 自动进入。
    /// </summary>
    private void ShowEpisodeClear(string completedScript, string nextScript)
    {
        if (episodeClearOverlay == null)
        {
            var root = uiDoc.rootVisualElement;
            episodeClearOverlay = new VisualElement { name = "episode-clear" };
            episodeClearOverlay.style.position = Position.Absolute;
            episodeClearOverlay.style.top = 0; episodeClearOverlay.style.left = 0;
            episodeClearOverlay.style.right = 0; episodeClearOverlay.style.bottom = 0;
            // 近黑暖棕半透明，保留底层场景轮廓
            episodeClearOverlay.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.88f);
            episodeClearOverlay.pickingMode = PickingMode.Position;
            root.Add(episodeClearOverlay);
        }
        episodeClearOverlay.Clear();
        episodeClearOverlay.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.88f);

        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        var fontDef = new FontDefinition { font = gameFont };
        string nextTitle = GetEpisodeTitle(nextScript);
        int epNum = GetEpisodeNumber(nextScript);

        // —— 阶段1：To Be Continued…（右下，白字，无衬线感）——
        var tbc = new Label("To Be Continued...");
        tbc.style.position = Position.Absolute;
        tbc.style.right = 48;
        tbc.style.bottom = 56;
        tbc.style.fontSize = 34;
        tbc.style.color = new Color(1f, 1f, 1f, 0.92f);
        tbc.style.unityFontStyleAndWeight = FontStyle.Bold;
        tbc.style.unityFontDefinition = fontDef;
        tbc.style.unityTextAlign = TextAnchor.MiddleRight;
        tbc.pickingMode = PickingMode.Ignore;
        episodeClearOverlay.Add(tbc);

        // 点击：取消倒计时，直接进下一话
        episodeClearOverlay.RegisterCallback<ClickEvent>(e =>
        {
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }
            episodeClearOverlay.style.display = DisplayStyle.None;
            StartScript(nextScript);
        });

        episodeClearOverlay.style.display = DisplayStyle.Flex;
        // 阶段1停留 → 阶段2横条 → 自动进
        autoAdvanceCoroutine = StartCoroutine(EpisodeClearSequence(nextScript, epNum, nextTitle, fontDef));
    }

    private IEnumerator EpisodeClearSequence(string nextScript, int epNum, string nextTitle, FontDefinition fontDef)
    {
        // 阶段1：To Be Continued 约 1.8s
        yield return new WaitForSeconds(1.8f);
        if (episodeClearOverlay == null || episodeClearOverlay.style.display != DisplayStyle.Flex)
            yield break;

        // 阶段2：清空 → 暖金横条 Next Episode（BA 结构 + 本作配色）
        episodeClearOverlay.Clear();
        episodeClearOverlay.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.94f);
        // 重绑点击（Clear 会丢）
        episodeClearOverlay.RegisterCallback<ClickEvent>(e =>
        {
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }
            episodeClearOverlay.style.display = DisplayStyle.None;
            StartScript(nextScript);
        });

        var bandRow = new VisualElement();
        bandRow.style.position = Position.Absolute;
        bandRow.style.top = 0; bandRow.style.left = 0;
        bandRow.style.right = 0; bandRow.style.bottom = 0;
        bandRow.style.alignItems = Align.Center;
        bandRow.style.justifyContent = Justify.Center;
        bandRow.pickingMode = PickingMode.Ignore;
        episodeClearOverlay.Add(bandRow);

        // 横贯全宽横条：深棕底 + 金边（不是 BA 冷蓝白）
        var band = new VisualElement();
        band.style.width = Length.Percent(100);
        band.style.minHeight = 160;
        band.style.alignItems = Align.Center;
        band.style.justifyContent = Justify.Center;
        band.style.backgroundColor = new Color(0.14f, 0.09f, 0.05f, 0.97f);
        band.style.borderTopWidth = 2;
        band.style.borderBottomWidth = 2;
        band.style.borderTopColor = new Color(0.82f, 0.62f, 0.35f, 0.75f);
        band.style.borderBottomColor = new Color(0.82f, 0.62f, 0.35f, 0.75f);
        bandRow.Add(band);

        var stack = new VisualElement();
        stack.style.alignItems = Align.Center;
        stack.style.paddingTop = 28;
        stack.style.paddingBottom = 28;
        band.Add(stack);

        var nextEp = new Label("Next Episode");
        nextEp.style.fontSize = 26;
        nextEp.style.color = new Color(1f, 0.78f, 0.4f, 1f);
        nextEp.style.unityFontStyleAndWeight = FontStyle.Bold;
        nextEp.style.unityFontDefinition = fontDef;
        nextEp.style.marginBottom = 6;
        stack.Add(nextEp);

        // 暖金短下划线（BA 结构，本作金）
        var goldLine = new VisualElement();
        goldLine.style.width = 120;
        goldLine.style.height = 4;
        goldLine.style.backgroundColor = new Color(1f, 0.8f, 0.45f, 1f);
        goldLine.style.borderTopLeftRadius = 2;
        goldLine.style.borderTopRightRadius = 2;
        goldLine.style.borderBottomLeftRadius = 2;
        goldLine.style.borderBottomRightRadius = 2;
        goldLine.style.marginBottom = 18;
        stack.Add(goldLine);

        var title = new Label("第" + epNum + "话　" + nextTitle);
        title.style.fontSize = 40;
        title.style.color = new Color(1f, 0.95f, 0.85f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = fontDef;
        stack.Add(title);

        var hint = new Label("点击继续");
        hint.style.fontSize = 15;
        hint.style.color = new Color(1f, 1f, 1f, 0.45f);
        hint.style.marginTop = 16;
        hint.style.unityFontDefinition = fontDef;
        stack.Add(hint);

        // 阶段2 约 2.7s 后自动进
        float elapsed = 0f;
        const float seconds = 2.7f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (episodeClearOverlay != null && episodeClearOverlay.style.display == DisplayStyle.Flex)
        {
            episodeClearOverlay.style.display = DisplayStyle.None;
            StartScript(nextScript);
        }
        autoAdvanceCoroutine = null;
    }

    private IEnumerator AutoAdvanceCountdown(float seconds, VisualElement fill, System.Action onDone)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float pct = Mathf.Clamp01(elapsed / seconds);
            fill.style.width = new Length(200f * (1f - pct), LengthUnit.Pixel);
            yield return null;
        }
        autoAdvanceCoroutine = null;
        onDone?.Invoke();
    }

    /// <summary>横幅四角信号角标（细线 2px 符号，指向内）。</summary>
    private void AddCornerMark(VisualElement parent, string symbol, string vPos, string hPos)
    {
        var mark = new Label(symbol);
        mark.style.fontSize = 16;
        mark.style.color = new Color(1f, 0.8f, 0.4f, 0.55f);
        mark.style.unityTextAlign = TextAnchor.MiddleCenter;
        mark.style.unityFontDefinition = new FontDefinition { font = gameFont };
        mark.style.position = Position.Absolute;
        mark.style.width = 22; mark.style.height = 22;
        if (vPos == "top") mark.style.top = 2; else mark.style.bottom = 2;
        if (hPos == "left") mark.style.left = 4; else mark.style.right = 4;
        mark.pickingMode = PickingMode.Ignore;
        parent.Add(mark);
    }

    private string GetEpisodeTitle(string scriptName)
    {
        // 从 MainStoryUI 的章节数据获取标题
        var titles = new System.Collections.Generic.Dictionary<string, string>
        {
            {"prologue_01_news", "广播里的时代"},
            {"prologue_02_day0", "启程之日"},
            {"prologue_03_journey", "边境危机"},
            {"prologue_04_arrival", "抵达雾峰"},
            {"prologue_05_inspection", "线路巡视"},
            {"prologue_06_team", "旧人重逢"},
            {"prologue_07_first_repair", "第一次检修"},
            {"prologue_08_first_run", "首班车"},
            {"prologue_09_funding", "三条来路"},
            {"prologue_10_transition", "序章落幕"},
        };
        return titles.TryGetValue(scriptName, out var t) ? t : scriptName;
    }

    private int GetEpisodeNumber(string scriptName)
    {
        var nums = new System.Collections.Generic.Dictionary<string, int>
        {
            {"prologue_01_news", 1}, {"prologue_02_day0", 2}, {"prologue_03_journey", 3},
            {"prologue_04_arrival", 4}, {"prologue_05_inspection", 5}, {"prologue_06_team", 6},
            {"prologue_07_first_repair", 7}, {"prologue_08_first_run", 8},
            {"prologue_09_funding", 9}, {"prologue_10_transition", 10},
        };
        return nums.TryGetValue(scriptName, out var n) ? n : 0;
    }

    // Variable system methods
    public void SetVariable(string name, bool value)
    {
        variables[name] = value;
    }

    public bool GetVariable(string name)
    {
        return variables.TryGetValue(name, out bool val) && val;
    }

    public void ClearVariables()
    {
        variables.Clear();
    }

    private bool EvaluateCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition)) return true;
        condition = condition.Trim();

        if (condition.StartsWith("!"))
        {
            string varName = condition.Substring(1).Trim();
            return !GetVariable(varName);
        }

        return GetVariable(condition);
    }

    private void ApplySetValue(string setValue)
    {
        if (string.IsNullOrEmpty(setValue)) return;
        var parts = setValue.Split('=');
        if (parts.Length != 2) return;
        string varName = parts[0].Trim();
        string valStr = parts[1].Trim();
        if (valStr == "true" || valStr == "1")
        {
            SetVariable(varName, true);
            return;
        }
        if (valStr == "false" || valStr == "0")
        {
            SetVariable(varName, false);
            return;
        }
        // 字符串值：注册 varName_value flag，条件写 condition="varName_value"
        SetVariable(varName + "_" + valStr.ToLower(), true);
    }

    private TransitionType ParseTransition(string transition)
    {
        if (string.IsNullOrEmpty(transition)) return TransitionType.Fade;
        switch (transition.ToLower())
        {
            case "slideleft": return TransitionType.SlideLeft;
            case "slideright": return TransitionType.SlideRight;
            case "none": return TransitionType.None;
            case "fade":
            default: return TransitionType.Fade;
        }
    }

    /// <summary>主角说话者显示为别名（若设置了表字）。</summary>
    private string ResolveSpeakerName(string speaker)
    {
        // 名字输入已移除，直接使用JSON中的名字
        return speaker;
    }

    /// <summary>处理特殊游戏操作指令（t:"special"）。</summary>
    private void HandleSpecialCommand(string command)
    {
        switch (command)
        {
            case "TRANSITION_TO_GAMEPLAY":
                TransitionToGameplay();
                break;
            default:
                Debug.LogWarning("[VN] Unknown special command: " + command);
                NextDialogue();
                break;
        }
    }

    /// <summary>VN→经营场景过渡：构建 VNExitData 并切换场景。</summary>
    private void TransitionToGameplay()
    {
        StopAutoPlay();
        isScriptRunning = false;

        VNExitData exitData = new VNExitData();

        // 基础数据
        exitData.startMoney = 40000f;
        exitData.startTrust = 60;
        exitData.startTrainCondition = 70;
        exitData.difficulty = GameConfig.Load().difficulty;
        exitData.playerAlias = GameConfig.Load().PlayerDisplayName;

        // 员工数据（5名初始员工）
        exitData.crew = new CrewData[]
        {
            new CrewData { id = "laochen",     name = "老陈",   role = "driver",     skillLevel = 5, fatigue = 0f,   specialty = "safety" },
            new CrewData { id = "zhanggong",   name = "张工",   role = "mechanic",   skillLevel = 5, fatigue = 0.2f, specialty = "repair" },
            new CrewData { id = "liayi",       name = "李阿姨", role = "conductor",  skillLevel = 2, fatigue = 0f,   specialty = "service" },
            new CrewData { id = "zhaoshifu",   name = "赵师傅", role = "dispatcher", skillLevel = 4, fatigue = 0.1f, specialty = "management" },
            new CrewData { id = "xiaofang",    name = "小芳",   role = "attendant",  skillLevel = 1, fatigue = 0f,   specialty = "learning" }
        };

        // 完成标记与解锁区域
        exitData.completedFlags = new string[] { "prologue_complete" };
        exitData.unlockedRegions = new string[] { "wufeng_mine" };

        // 序列化保存过渡数据
        string json = JsonUtility.ToJson(exitData);
        PlayerPrefs.SetString("VNExitData", json);
        PlayerPrefs.Save();

        Debug.Log("[VN] TransitionToGameplay: saved VNExitData, loading StationSlice_V1");

        // 加载经营场景
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("StationSlice_V1");
    }

    // Auto-play
    private void ToggleAutoPlay()
    {
        isAutoPlay = !isAutoPlay;
        UpdateAutoPlayButtonVisual();

        if (isAutoPlay)
        {
            // 当前句已播完且无选项/面板挡着 → 立刻跳下一句，不等 autoPlayDelay
            bool typing = dialogueBox != null && dialogueBox.IsTyping();
            bool optionsShowing = optionsContainer != null && optionsContainer.style.display == DisplayStyle.Flex;
            if (!typing && !optionsShowing && !AdvanceBlockedByPanels())
                NextDialogue();
            StartAutoPlayTimer();
        }
        else
            StopAutoPlay();
    }

    private void UpdateAutoPlayButtonVisual()
    {
        if (autoBtn == null) return;
        if (isAutoPlay)
        {
            // 激活：棋盘格暖金底 + 双层金边（不加额外图标）
            autoBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.ActivePattern());
            autoBtn.style.unityBackgroundImageTintColor = Color.white;
            autoBtn.style.backgroundColor = new Color(0.45f, 0.28f, 0.12f, 0.95f);
            autoBtn.style.color = new Color(1f, 0.9f, 0.55f, 1f);
            autoBtn.text = "Auto";
            autoIcon.style.display = DisplayStyle.None;
            SetGoldBorder(autoBtn, 2, new Color(1f, 0.8f, 0.4f, 0.9f), new Color(1f, 0.9f, 0.6f, 0.45f));
        }
        else
        {
            autoBtn.style.backgroundImage = null;
            autoBtn.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.85f);
            autoBtn.style.color = new Color(1f, 1f, 1f, 0.8f);
            autoBtn.text = "Auto";
            autoIcon.style.display = DisplayStyle.None;
            SetGoldBorder(autoBtn, 2, new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f), new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.1f));
        }
    }

    /// <summary>双层金边：外层亮金边框 + 内层淡金虚线（用细内边框模拟层次，UI Toolkit 无 boxShadow）。</summary>
    private void SetGoldBorder(VisualElement ve, float w, Color outer, Color inner)
    {
        ve.style.borderTopWidth = w; ve.style.borderBottomWidth = w;
        ve.style.borderLeftWidth = w; ve.style.borderRightWidth = w;
        ve.style.borderTopColor = outer; ve.style.borderBottomColor = outer;
        ve.style.borderLeftColor = outer; ve.style.borderRightColor = outer;
        // 内部层次：子伪元素细金边（boxShadow 在 Unity 6.5 IStyle 不存在，改用独立内衬元素）
        var innerRing = ve.Q<VisualElement>("gold-inner-ring");
        if (innerRing == null)
        {
            innerRing = new VisualElement { name = "gold-inner-ring" };
            innerRing.pickingMode = PickingMode.Ignore;
            innerRing.style.position = Position.Absolute;
            innerRing.style.top = 2; innerRing.style.left = 2;
            innerRing.style.right = 2; innerRing.style.bottom = 2;
            innerRing.style.borderTopWidth = 1; innerRing.style.borderBottomWidth = 1;
            innerRing.style.borderLeftWidth = 1; innerRing.style.borderRightWidth = 1;
            ve.Add(innerRing);
        }
        innerRing.style.borderTopColor = inner; innerRing.style.borderBottomColor = inner;
        innerRing.style.borderLeftColor = inner; innerRing.style.borderRightColor = inner;
    }

    private void OnDialogueTypewriterComplete()
    {
        // 只有在Auto模式开启且没有选项显示时才启动自动播放
        if (isAutoPlay && isScriptRunning)
        {
            bool optionsShowing = optionsContainer != null && optionsContainer.style.display == DisplayStyle.Flex;
            if (!optionsShowing)
                StartAutoPlayTimer();
        }
    }

    private void OnFullScreenNewsClosed()
    {
        if (!isScriptRunning) return;
        if (menuBar != null) menuBar.style.display = DisplayStyle.Flex;
        NextDialogue();
    }

    private void OnBacklogEntryClicked(int sceneIndex, int dialogueIndex)
    {
        if (!isScriptRunning || currentScript == null) return;
        if (sceneIndex < 0 || sceneIndex >= currentScript.scenes.Length) return;

        StopAutoPlay();
        currentSceneIndex = sceneIndex;
        currentDialogueIndex = dialogueIndex;
        ResetProgressShown();

        // 恢复场景背景和BGM
        var scene = currentScript.scenes[sceneIndex];
        if (!string.IsNullOrEmpty(scene.bg))
            backgroundManager?.SetBackgroundImmediate(scene.bg);
        if (!string.IsNullOrEmpty(scene.bgm))
            VNAudioManager.Instance?.PlayBGM(scene.bgm);

        characterSpriteManager?.ClearAll();
        ShowCurrentDialogue();

        // 恢复菜单栏
        if (menuBar != null) menuBar.style.display = DisplayStyle.Flex;
    }

    private bool isFromTitleScreenMode;

    private void StopGlobalTitleBGM()
    {
        // 查找所有名为"BGM"的对象并销毁（包括重复创建的情况）
        var bgmObjs = GameObject.FindGameObjectsWithTag("Untagged");
        foreach (var obj in bgmObjs)
        {
            if (obj.name == "BGM")
            {
                var src = obj.GetComponent<AudioSource>();
                if (src != null) { src.Stop(); src.clip = null; }
                GameObject.Destroy(obj);
            }
        }
        // 额外清理：按名字查找（Find 只返回第一个）
        var bgmObj = GameObject.Find("BGM");
        if (bgmObj != null)
        {
            var src = bgmObj.GetComponent<AudioSource>();
            if (src != null) { src.Stop(); src.clip = null; }
            GameObject.Destroy(bgmObj);
        }
    }

    private void HideVnForLoadUI()
    {
        // 从标题界面继续：立即清空画面等待读档界面（隐藏全部 VN UI，无残留）
        uiHidden = true; // 复用隐藏态：阻止 Update 的任何恢复逻辑
        hiddenUiSnapshots.Clear();
        if (menuBar != null) menuBar.style.display = DisplayStyle.None;
        if (menuExpandedContainer != null) menuExpandedContainer.style.display = DisplayStyle.None;
        if (dialogueBox != null) dialogueBox.Hide();
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.None;
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.None;
        if (bootScreen != null) bootScreen.style.display = DisplayStyle.None;
        if (cgScreen != null) cgScreen.style.display = DisplayStyle.None;
        if (episodeClearOverlay != null) episodeClearOverlay.style.display = DisplayStyle.None;
        if (resumeDialog != null) resumeDialog.style.display = DisplayStyle.None;
        if (bookmarkToast != null) bookmarkToast.style.display = DisplayStyle.None;
        characterSpriteManager?.ClearAll();
        if (backgroundManager != null) backgroundManager.SetBackgroundImmediate("black");
    }

    private IEnumerator ShowLoadUIDelayed()
    {
        // 无延迟：同步开面板（避免中间帧 Update 恢复 menuBar 造成残留）
        if (saveLoadUI != null)
            saveLoadUI.OpenLoadPanelFromTitle((slotIndex) =>
            {
                // 保存槽位到PlayerPrefs，重载场景后自动加载
                PlayerPrefs.SetInt("VN_LoadSaveSlot", slotIndex);
                PlayerPrefs.Save();
                isFromTitleScreenMode = false;
                saveLoadUI.ClosePanel();
                saveLoadUI.SetIsFromTitleScreenFromTitle(false);
                UnityEngine.SceneManagement.SceneManager.LoadScene("VN_Test");
            });
        yield break;
    }

    private void StartAutoPlayTimer()
    {
        // 只清协程，不关 isAutoPlay（否则刚开启的 Auto 会被自己关掉）
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
        if (isAutoPlay && isScriptRunning)
            autoPlayCoroutine = StartCoroutine(AutoPlayTimer());
    }

    private IEnumerator AutoPlayTimer()
    {
        yield return new WaitForSeconds(autoPlayDelay);

        if (!isAutoPlay || !isScriptRunning) yield break;

        // 面板/菜单/对话框打开时暂停自动推进（防止点菜单后面板开着仍跳句）
        if (menuExpanded) yield break;
        if (confirmDialog != null && confirmDialog.style.display == DisplayStyle.Flex) yield break;
        if (optionsContainer != null && optionsContainer.style.display == DisplayStyle.Flex) yield break;
        if (vnBacklog != null && vnBacklog.IsOpen) yield break;
        if (saveLoadUI != null && saveLoadUI.IsOpen) yield break;

        if (dialogueBox != null && dialogueBox.IsTyping())
            dialogueBox.SkipTyping();
        else
            NextDialogue();
    }

    /// <summary>停止自动协程；默认同时关闭 Auto 模式与高亮（手动推进用）。</summary>
    private void StopAutoPlay(bool disableMode = true)
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
        if (disableMode && isAutoPlay)
        {
            isAutoPlay = false;
            UpdateAutoPlayButtonVisual();
        }
    }
}
