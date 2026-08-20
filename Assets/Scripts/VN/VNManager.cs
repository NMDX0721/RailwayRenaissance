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
    private static Texture2D handCursorTex;

    // Variable system
    private Dictionary<string, bool> variables = new Dictionary<string, bool>();

    // Auto-play
    private bool isAutoPlay;
    private float autoPlayDelay = 2.0f;
    private Coroutine autoPlayCoroutine;
    private UnityEngine.UIElements.Button autoBtn;

    // Confirm dialog
    private VisualElement confirmDialog;
    private VisualElement bootScreen;
    private VisualElement cgScreen;
    private bool VN_ReplayInjected;
    private Label bookmarkToast;
    private Coroutine bookmarkToastCoroutine;
    private VisualElement episodeClearOverlay;

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
    }

    private void Start()
    {
        SetupGameCursor();
        SetupCanvas();
        GameData.ApplyVolume();
        SetupEventSystem();

        // 标题界面"继续运营" → 直接显示读档界面，不播序章
        if (PlayerPrefs.GetInt("VN_ShowLoadUI", 0) == 1)
        {
            PlayerPrefs.SetInt("VN_ShowLoadUI", 0);
            PlayerPrefs.Save();
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
            resumeDialog = new VisualElement { name = "resume-dialog" };
            resumeDialog.style.position = Position.Absolute;
            resumeDialog.style.top = 0; resumeDialog.style.left = 0;
            resumeDialog.style.right = 0; resumeDialog.style.bottom = 0;
            resumeDialog.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            resumeDialog.style.alignItems = Align.Center;
            resumeDialog.style.justifyContent = Justify.Center;
            root.Add(resumeDialog);
        }
        resumeDialog.Clear();
        resumeDialog.style.display = DisplayStyle.Flex;

        var panel = new VisualElement();
        panel.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.97f);
        panel.style.paddingLeft = 32; panel.style.paddingRight = 32;
        panel.style.paddingTop = 24; panel.style.paddingBottom = 24;
        panel.style.alignItems = Align.Center;
        panel.style.borderTopWidth = 2; panel.style.borderBottomWidth = 2;
        panel.style.borderLeftWidth = 2; panel.style.borderRightWidth = 2;
        panel.style.borderTopColor = new Color(0.8f, 0.62f, 0.35f, 0.8f);
        panel.style.borderBottomColor = new Color(0.8f, 0.62f, 0.35f, 0.8f);
        panel.style.borderLeftColor = new Color(0.8f, 0.62f, 0.35f, 0.8f);
        panel.style.borderRightColor = new Color(0.8f, 0.62f, 0.35f, 0.8f);
        resumeDialog.Add(panel);

        var title = new Label("未完成的剧情");
        title.style.fontSize = 24;
        title.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = new FontDefinition { font = gameFont };
        title.style.marginBottom = 10;
        panel.Add(title);

        var info = new Label(bm.name);
        info.style.fontSize = 18;
        info.style.color = new Color(1f, 1f, 1f, 0.9f);
        info.style.unityFontDefinition = new FontDefinition { font = gameFont };
        info.style.marginBottom = 6;
        panel.Add(info);

        var preview = new Label(bm.previewText ?? "");
        preview.style.fontSize = 14;
        preview.style.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        preview.style.unityFontDefinition = new FontDefinition { font = gameFont };
        preview.style.marginBottom = 20;
        panel.Add(preview);

        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        panel.Add(btnRow);

        var yesBtn = new UnityEngine.UIElements.Button(() => { resumeDialog.style.display = DisplayStyle.None; pendingResumeBookmark = null; BookmarkManager.JumpToBookmark(bm); UnityEngine.SceneManagement.SceneManager.LoadScene("VN_Test"); }) { text = "继续观看" };
        yesBtn.style.width = 140; yesBtn.style.height = 40;
        yesBtn.style.fontSize = 18; yesBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        yesBtn.style.unityFontDefinition = new FontDefinition { font = gameFont };
        yesBtn.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.8f);
        yesBtn.style.color = new Color(0.8f, 0.9f, 1f, 1f);
        btnRow.Add(yesBtn);

        var noBtn = new UnityEngine.UIElements.Button(() =>
        {
            resumeDialog.style.display = DisplayStyle.None;
            // 放弃恢复，从第一章开始
            pendingResumeBookmark = null;
            BookmarkManager.ClearAutoBookmark(bm.scriptName);
            StartScript("prologue_01_news");
        }) { text = "从头开始" };
        noBtn.style.width = 140; noBtn.style.height = 40;
        noBtn.style.fontSize = 18; noBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        noBtn.style.unityFontDefinition = new FontDefinition { font = gameFont };
        noBtn.style.backgroundColor = new Color(0.3f, 0.18f, 0.12f, 0.8f);
        noBtn.style.color = new Color(0.9f, 0.8f, 0.7f, 1f);
        noBtn.style.marginLeft = 12;
        btnRow.Add(noBtn);
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
        if (handCursorTex == null)
            handCursorTex = LoginManager.LoadCursorTexture("Cursors/cursor_hand", 3);
        UnityEngine.Cursor.SetCursor(LoginManager.cursorTexture, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && LoginManager.cursorTexture != null)
            UnityEngine.Cursor.SetCursor(LoginManager.cursorTexture, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
    }

    private void AddCursorHover(VisualElement element)
    {
        element.RegisterCallback<PointerEnterEvent>(e =>
        {
            if (handCursorTex != null)
                UnityEngine.Cursor.SetCursor(handCursorTex, new Vector2(0, 0), UnityEngine.CursorMode.ForceSoftware);
        });
        element.RegisterCallback<PointerLeaveEvent>(e =>
        {
            if (LoginManager.cursorTexture != null)
                UnityEngine.Cursor.SetCursor(LoginManager.cursorTexture, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
        });
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

        // 根元素不捕获点击，只有交互元素（按钮等）才捕获。
        // 鼠标推进由 root 的 TrickleDown PointerDownEvent 统一处理：
        // TrickleDown 先于按钮自己的处理执行，可在此拦截"点按钮误推进"（帧序无关）
        uiDoc.rootVisualElement.pickingMode = PickingMode.Ignore;
        uiDoc.rootVisualElement.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);

        // 背景必须先初始化（在最底层）
        backgroundManager = gameObject.AddComponent<BackgroundManager>();
        backgroundManager.Init(uiDoc);

        dialogueBox = gameObject.AddComponent<DialogueBox>();
        dialogueBox.Init(uiDoc);
        dialogueBox.OnTypewriterComplete += OnDialogueTypewriterComplete;

        characterSpriteManager = gameObject.AddComponent<CharacterSpriteManager>();
        characterSpriteManager.Init(uiDoc);

        vnBacklog = gameObject.AddComponent<VNBacklog>();
        vnBacklog.Init(uiDoc);
        vnBacklog.OnEntryClicked += OnBacklogEntryClicked;

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
        uiDoc.rootVisualElement.Add(optionsOverlay);

        optionsContainer = new VisualElement();
        optionsContainer.name = "options-container";
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
        esObj.AddComponent<StandaloneInputModule>();
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
        autoBtn.RegisterCallback<PointerDownEvent>(evt => { evt.StopPropagation(); });
        autoBtn.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
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
        menuBar.Add(autoBtn);

        // Menu 按钮（点击展开/收起子菜单）
        var menuBtn = new UnityEngine.UIElements.Button(() => ToggleMenuExpanded()) { text = "Menu" };
        menuBtn.RegisterCallback<PointerDownEvent>(evt => { evt.StopPropagation(); });
        menuBtn.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
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
        menuBar.Add(menuBtn);
        this.menuBtn = menuBtn;

        // 子菜单（BA 风格展开底板：深棕半透明面板 + 金边 + 内衬，按钮依附于板）
        menuExpandedContainer = new VisualElement { name = "menu-expanded" };
        menuExpandedContainer.style.position = Position.Absolute;
        menuExpandedContainer.style.top = 52;   // 紧贴 Auto/Menu 行下方
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
        menuExpandedContainer.style.paddingLeft = 8; menuExpandedContainer.style.paddingRight = 8;
        menuExpandedContainer.style.paddingTop = 8; menuExpandedContainer.style.paddingBottom = 8;
        menuExpandedContainer.pickingMode = PickingMode.Position;
        root.Add(menuExpandedContainer);

        // 子菜单项：像素图标（去独立边框，按钮融于底板——hover 才显边框）
        var menuItemDefs = new (Texture2D, System.Action)[]
        {
            (PixelIconHelper.SaveIcon(), () => OpenSaveMenu()),
            (PixelIconHelper.LoadIcon(), () => OpenLoadMenu()),
            (PixelIconHelper.BacklogIcon(), () => ToggleBacklog()),
            (PixelIconHelper.SkipIcon(), () => SkipToNext()),
            (PixelIconHelper.BookmarkIcon(), () => AddBookmark()),
            (PixelIconHelper.ReturnIcon(), () => ShowConfirmDialog()),
        };

        foreach (var (itemIcon, itemAction) in menuItemDefs)
        {
            var btn = new UnityEngine.UIElements.Button(() => itemAction()) { text = "" };
            btn.RegisterCallback<PointerDownEvent>(evt => { evt.StopPropagation(); });
            btn.style.width = 48;
            btn.style.height = 42;
            btn.style.backgroundColor = Color.clear; // 透明底，融于底板
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.backgroundImage = new StyleBackground(itemIcon);
            btn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
            // 初始无边框；hover 时金色细边（状态反馈）
            btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = Color.clear; btn.style.borderBottomColor = Color.clear;
            btn.style.borderLeftColor = Color.clear; btn.style.borderRightColor = Color.clear;
            btn.style.borderTopLeftRadius = 4; btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4; btn.style.borderBottomRightRadius = 4;
            btn.style.marginLeft = 2; btn.style.marginRight = 2;
            btn.RegisterCallback<PointerEnterEvent>(evt =>
            {
                btn.style.backgroundColor = new Color(0.28f, 0.18f, 0.09f, 0.9f);
                btn.style.borderTopColor = new Color(1f, 0.85f, 0.5f, 0.7f);
                btn.style.borderBottomColor = new Color(1f, 0.85f, 0.5f, 0.7f);
                btn.style.borderLeftColor = new Color(1f, 0.85f, 0.5f, 0.7f);
                btn.style.borderRightColor = new Color(1f, 0.85f, 0.5f, 0.7f);
            });
            btn.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                btn.style.backgroundColor = Color.clear;
                btn.style.borderTopColor = Color.clear;
                btn.style.borderBottomColor = Color.clear;
                btn.style.borderLeftColor = Color.clear;
                btn.style.borderRightColor = Color.clear;
            });
            // 像素图缩放模式（新 background API，替代废弃的 unityBackgroundScaleMode）
            btn.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            btn.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            btn.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            btn.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            menuExpandedContainer.Add(btn);
        }

        UpdateAutoPlayButtonVisual();
        SetupConfirmDialog(uiDoc);
    }

    private bool menuExpanded;
    private VisualElement menuExpandedContainer;
    private UnityEngine.UIElements.Button menuBtn;
    private Label autoIcon;
    private Label menuIcon;

    private void ToggleMenuExpanded()
    {
        menuExpanded = !menuExpanded;
        menuExpandedContainer.style.display = menuExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        UpdateMenuButtonVisual();
    }

    private void UpdateMenuButtonVisual()
    {
        if (menuBtn == null) return;
        if (menuExpanded)
        {
            // 展开：网格像素图标 + 棋盘格暖金底 + 双层金边
            menuBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.ActivePattern());
            menuBtn.style.unityBackgroundImageTintColor = Color.white;
            menuBtn.style.backgroundColor = new Color(0.45f, 0.28f, 0.12f, 0.95f);
            menuBtn.style.color = new Color(1f, 0.9f, 0.55f, 1f);
            menuIcon.style.display = DisplayStyle.Flex;
            menuIcon.style.backgroundImage = new StyleBackground(PixelIconHelper.MenuGridIcon());
            menuIcon.style.unityBackgroundImageTintColor = new Color(1f, 0.85f, 0.5f, 1f);
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

    /// <summary>Skip 跳转：快速推进到下一选项/下一场景。</summary>
    private void SkipToNext()
    {
        CloseMenuExpanded();
        CloseMenuExpanded();
        // 跳过当前打字
        if (dialogueBox != null && dialogueBox.IsTyping())
            dialogueBox.SkipTyping();
        // 快速推进到下一选项
        int maxSkip = 200;
        int safety = 0;
        while (safety < maxSkip && isScriptRunning && currentScript != null)
        {
            var scene = currentScript.scenes[currentSceneIndex];
            if (currentDialogueIndex < scene.d.Length)
            {
                var entry = scene.d[currentDialogueIndex];
                if (entry.t == "c" || entry.t == "special")
                    break;
            }
            NextDialogue();
            safety++;
            // 检查是否到达选项
            if (currentSceneIndex < currentScript.scenes.Length)
            {
                var nextScene = currentScript.scenes[currentSceneIndex];
                if (currentDialogueIndex < nextScene.d.Length && nextScene.d[currentDialogueIndex].t == "c")
                    break;
            }
        }
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

    private void SetupConfirmDialog(UIDocument uiDoc)
    {
        var root = uiDoc.rootVisualElement;
        var fontDef = new FontDefinition { font = gameFont };

        confirmDialog = new VisualElement { name = "confirm-dialog" };
        confirmDialog.style.position = Position.Absolute;
        confirmDialog.style.top = 0;
        confirmDialog.style.left = 0;
        confirmDialog.style.right = 0;
        confirmDialog.style.bottom = 0;
        confirmDialog.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        confirmDialog.style.display = DisplayStyle.None;
        confirmDialog.style.alignItems = Align.Center;
        confirmDialog.style.justifyContent = Justify.Center;
        confirmDialog.pickingMode = PickingMode.Position;
        root.Add(confirmDialog);

        var dialogBox = new VisualElement();
        dialogBox.style.width = 500;
        dialogBox.style.height = 260;
        dialogBox.style.backgroundColor = new Color(0.15f, 0.1f, 0.06f, 0.95f);
        dialogBox.style.borderTopWidth = 2;
        dialogBox.style.borderBottomWidth = 2;
        dialogBox.style.borderLeftWidth = 2;
        dialogBox.style.borderRightWidth = 2;
        dialogBox.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        dialogBox.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        dialogBox.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        dialogBox.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        dialogBox.style.borderTopLeftRadius = 8;
        dialogBox.style.borderTopRightRadius = 8;
        dialogBox.style.borderBottomLeftRadius = 8;
        dialogBox.style.borderBottomRightRadius = 8;
        dialogBox.style.flexDirection = FlexDirection.Column;
        dialogBox.style.alignItems = Align.Center;
        dialogBox.style.justifyContent = Justify.Center;
        dialogBox.style.paddingLeft = 40;
        dialogBox.style.paddingRight = 40;
        dialogBox.style.paddingTop = 30;
        dialogBox.style.paddingBottom = 30;
        confirmDialog.Add(dialogBox);

        var titleLabel = new Label("返回标题界面？");
        titleLabel.style.fontSize = 36;
        titleLabel.style.color = new Color(200f / 255f, 150f / 255f, 80f / 255f, 1f);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityFontDefinition = fontDef;
        titleLabel.style.marginBottom = 15;
        dialogBox.Add(titleLabel);

        var descLabel = new Label("当前进度将丢失");
        descLabel.style.fontSize = 26;
        descLabel.style.color = new Color(1f, 1f, 1f, 0.6f);
        descLabel.style.unityFontDefinition = fontDef;
        descLabel.style.marginBottom = 40;
        dialogBox.Add(descLabel);

        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        dialogBox.Add(btnRow);

        var yesBtn = new UnityEngine.UIElements.Button(() => ReturnToTitle()) { text = "确认" };
        yesBtn.style.width = 150;
        yesBtn.style.height = 55;
        yesBtn.style.marginRight = 30;
        yesBtn.style.fontSize = 28;
        yesBtn.style.color = new Color(1f, 1f, 1f, 0.9f);
        yesBtn.style.backgroundColor = new Color(0.3f, 0.18f, 0.1f, 0.9f);
        yesBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        yesBtn.style.unityFontDefinition = fontDef;
        yesBtn.style.borderTopLeftRadius = 6;
        yesBtn.style.borderTopRightRadius = 6;
        yesBtn.style.borderBottomLeftRadius = 6;
        yesBtn.style.borderBottomRightRadius = 6;
        btnRow.Add(yesBtn);

        var noBtn = new UnityEngine.UIElements.Button(() => confirmDialog.style.display = DisplayStyle.None) { text = "取消" };
        noBtn.style.width = 150;
        noBtn.style.height = 55;
        noBtn.style.fontSize = 28;
        noBtn.style.color = new Color(1f, 1f, 1f, 0.8f);
        noBtn.style.backgroundColor = new Color(0.2f, 0.12f, 0.08f, 0.8f);
        noBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        noBtn.style.unityFontDefinition = fontDef;
        noBtn.style.borderTopLeftRadius = 6;
        noBtn.style.borderTopRightRadius = 6;
        noBtn.style.borderBottomLeftRadius = 6;
        noBtn.style.borderBottomRightRadius = 6;
        btnRow.Add(noBtn);
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
        if (confirmDialog != null)
            confirmDialog.style.display = DisplayStyle.Flex;
    }

    private void ReturnToTitle()
    {
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
        confirmDialog.style.display = DisplayStyle.None;
        PlayerPrefs.SetInt("VN_AutoLoad", 0);
        PlayerPrefs.Save();

        // 从经营主界面进入的剧情回看 → 返回 GameMainUI（毛胚主界面）
        if (PlayerPrefs.GetInt("VN_FromGameMain", 0) == 1)
        {
            PlayerPrefs.SetInt("VN_FromGameMain", 0);
            PlayerPrefs.Save();
            TitleArchiveUI.EnsureInstance()?.Hide();
            MainStoryUI.HideStatic();
            GameMainUI.Show();
            return;
        }

        try
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScreen");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VN] Failed to load TitleScreen: " + e.Message);
        }
    }

    private void OpenSaveMenu()
    {
        if (string.IsNullOrEmpty(currentScriptName) || !isScriptRunning) return;
        CloseAllPanels();
        menuBar.style.display = DisplayStyle.None;
        saveLoadUI.OpenSavePanel(slot =>
        {
            var scene = currentScript.scenes[currentSceneIndex];
            string bgName = scene.bg;
            string bgmName = scene.bgm;
            saveSystem.SaveGame(slot, currentScriptName, currentSceneIndex, currentDialogueIndex, bgName, bgmName);
        });
    }

    private void OpenLoadMenu()
    {
        CloseAllPanels();
        menuBar.style.display = DisplayStyle.None;
        saveLoadUI.OpenLoadPanel(slot =>
        {
            var saveData = saveSystem.LoadGame(slot);
            if (saveData != null)
            {
                LoadFromSave(saveData);
            }
        });
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

    private void Update()
    {
        // 右键：隐藏/恢复全部 VN UI（背景保留），隐藏状态下点击仍可推进
        if (Input.GetMouseButtonDown(1))
        {
            if (uiHidden)
                RestoreVNUI();
            else
                HideVNUI();
            return;
        }

        // ESC：全屏新闻时关闭新闻，其余按面板优先级处理
        if (Input.GetKeyDown(KeyCode.Escape) || KeyBindings.IsDown(KeyBindings.Action.OpenMenu))
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
            if (vnBacklog != null && vnBacklog.IsOpen)
            {
                vnBacklog.ToggleBacklog();
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
            ShowConfirmDialog();
            return;
        }

        // 全屏新闻显示时忽略其他输入
        if (fullScreenNews != null && fullScreenNews.IsActive) return;

        // 面板关闭时恢复菜单栏显示（右键隐藏态/读档模式不恢复——避免 VN UI 残留）
        if (menuBar != null && menuBar.style.display == DisplayStyle.None && !uiHidden && !isFromTitleScreenMode)
        {
            bool anyPanelOpen = (vnBacklog != null && vnBacklog.IsOpen) ||
                                (saveLoadUI != null && saveLoadUI.IsOpen) ||
                                (confirmDialog != null && confirmDialog.style.display == DisplayStyle.Flex);
            if (!anyPanelOpen)
                menuBar.style.display = DisplayStyle.Flex;
        }

        if (!isScriptRunning) return;

        // F5：切换Auto模式
        if (Input.GetKeyDown(KeyCode.F5) || KeyBindings.IsDown(KeyBindings.Action.ToggleAuto))
        {
            ToggleAutoPlay();
            return;
        }

        // Shift：快进（打字时加速，已完成时自动推进到下一句）
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || KeyBindings.IsDown(KeyBindings.Action.SkipForward))
            {
                NextDialogue();
                return;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || KeyBindings.IsDown(KeyBindings.Action.SkipBack))
            {
                PrevDialogue();
                return;
            }
        }

        // 鼠标推进已迁移到 OnRootPointerDown（UI Toolkit 事件驱动，帧序安全）。
        // 此处仅保留键盘推进 + 按钮点击已经过 root TrickleDown 拦截。
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || KeyBindings.IsDown(KeyBindings.Action.Advance))
        {
            if (confirmDialog != null && confirmDialog.style.display == DisplayStyle.Flex)
                return;
            if (optionsContainer != null && optionsContainer.style.display == DisplayStyle.Flex)
                return;
            if (vnBacklog != null && vnBacklog.IsOpen)
                return;
            if (saveLoadUI != null && saveLoadUI.IsOpen)
                return;

            StopAutoPlay();

            if (dialogueBox != null && dialogueBox.IsTyping())
                dialogueBox.SkipTyping();
            else
                NextDialogue();
        }
    }

    /// <summary>
    /// 鼠标推进唯一入口（TrickleDown：先于按钮自身 handler 执行，帧序无关）。
    /// 点击落在交互元素（按钮/菜单/选项/日志/存档）内 → 不推进，事件继续通向按钮；
    /// 点击空白处 → 推进对话（打字中跳过，否则下一句）。
    /// </summary>
    private void OnRootPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0) return; // 仅左键
        // UI 隐藏状态下（右键隐藏）点击推进
        if (uiHidden)
        {
            AdvanceOnClick();
            return;
        }
        if (confirmDialog != null && confirmDialog.style.display == DisplayStyle.Flex) return;
        if (optionsContainer != null && optionsContainer.style.display == DisplayStyle.Flex) return;
        if (vnBacklog != null && vnBacklog.IsOpen) return;
        if (saveLoadUI != null && saveLoadUI.IsOpen) return;
        if (fullScreenNews != null && fullScreenNews.IsActive) return;
        if (resumeDialog != null && resumeDialog.style.display == DisplayStyle.Flex) return;

        // 目标在按钮或菜单栏内 → 不推进（按钮自己的 handler 处理）
        var t = evt.target as VisualElement;
        while (t != null)
        {
            if (t is UnityEngine.UIElements.Button) return;
            if (t == menuBar || t == menuExpandedContainer) return;
            t = t.parent;
        }

        // 允许推进前二次防御：此刻指针若在交互面板内也不推进
        if (IsPointerOverAnyUI())
            return;

        AdvanceOnClick();
    }

    private void AdvanceOnClick()
    {
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
        var local = RuntimePanelUtils.ScreenToPanel(panel, Input.mousePosition);
        var picked = panel.Pick(local);
        while (picked != null)
        {
            if (picked is UnityEngine.UIElements.Button) return true;
            if (picked == menuBar || picked == menuExpandedContainer) return true;
            picked = picked.parent;
        }
        return false;
    }

    /// <summary>
    /// 判断鼠标指针当前是否落在菜单栏（或其按钮）上方。
    /// 用于拦截"点击菜单按钮时对话被意外推进"：UI Toolkit 的 clicked 在
    /// PointerUp 才触发，而 Update 中 Input.GetMouseButtonDown(0) 在按下帧先执行。
    /// </summary>
    private void PrevDialogue()
    {
        if (currentDialogueIndex > 0)
        {
            currentDialogueIndex--;
            ShowCurrentDialogue();
        }
        else if (currentSceneIndex > 0)
        {
            currentSceneIndex--;
            // Skip to last dialogue of previous scene
            var prevScene = currentScript.scenes[currentSceneIndex];
            if (prevScene.d.Length > 0)
                currentDialogueIndex = prevScene.d.Length - 1;
            ShowCurrentDialogue();
        }
    }

    public void StartScript(string scriptName)
    {
        if (string.IsNullOrEmpty(scriptName)) return;
        currentScript = jsonParser.LoadScript(scriptName);
        if (currentScript == null) { Debug.LogError("Failed to load: " + scriptName); return; }
        currentScriptName = scriptName;
        currentSceneIndex = 0;
        currentDialogueIndex = 0;
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

        ShowCurrentDialogue();
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
        if (currentDialogueIndex >= scene.d.Length) return;
        var entry = scene.d[currentDialogueIndex];

        // Check condition - skip this entry if condition not met
        if (!EvaluateCondition(entry.condition))
        {
            NextDialogue();
            return;
        }

        // Apply setValue when entering this dialogue
        ApplySetValue(entry.setValue);

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
            // CG 插画——剧情中全屏展示，点击继续；text=Resources/cg/ 下的图片名，展示即解锁鉴赏
            dialogueBox?.Hide();
            HideOptions();
            if (menuBar != null) menuBar.style.display = DisplayStyle.None;
            ShowCgScreen(entry.text);
            TitleArchiveUI.UnlockCG(entry.text);
            vnBacklog?.AddEntry(entry.s, entry.text, currentSceneIndex, currentDialogueIndex);
            return; // 不显示常规对话，由CgScreen处理点击
        }
        else if (entry.t == "special")
        {
            // 特殊游戏操作指令
            HandleSpecialCommand(entry.text);
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
                    characterSpriteManager?.UpdateDisplay(entryChars, entryEmotion);
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

        var btnSprite = Resources.Load<Sprite>("UI/Login/button_primary");

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
                ShowCurrentDialogue();
            });
            btn.text = opt.text;
            btn.AddToClassList("vn-option-btn");
            btn.style.width = new Length(700, LengthUnit.Pixel);
            btn.style.height = new Length(120, LengthUnit.Pixel);
            btn.style.marginBottom = new Length(70, LengthUnit.Pixel);
            btn.style.fontSize = 36;
            btn.style.color = new Color(1f, 1f, 1f, 0.95f);
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.alignItems = Align.Stretch;
            btn.style.justifyContent = Justify.FlexStart;
            btn.style.unityTextOutlineWidth = 2;
            btn.style.unityTextOutlineColor = new Color(0.24f, 0.15f, 0.09f, 0.9f);
            btn.style.backgroundColor = Color.clear;
            btn.style.borderTopWidth = 0;
            btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0;
            btn.style.borderRightWidth = 0;
            if (btnSprite != null)
                btn.style.backgroundImage = new StyleBackground(btnSprite);
            if (gameFont != null)
                btn.style.unityFontDefinition = new FontDefinition { font = gameFont };
            AddCursorHover(btn);
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
                if (e.target == bootScreen)
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

    /// <summary>CG 插画全屏展示（Resources/cg/ 下图片，contain 缩放黑边），点击继续剧情。</summary>
    private void ShowCgScreen(string cgName)
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
            cgScreen.style.backgroundColor = new Color(0, 0, 0, 1f);
            cgScreen.pickingMode = PickingMode.Position;
            cgScreen.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target == cgScreen)
                    NextDialogue();
            });
            root.Add(cgScreen);
        }

        cgScreen.Clear();
        cgScreen.style.display = DisplayStyle.Flex;

        if (!string.IsNullOrEmpty(cgName))
        {
            var tex = Resources.Load<Texture2D>("cg/" + cgName);
            if (tex != null)
            {
                var img = new VisualElement();
                img.name = "cg-image";
                img.style.flexGrow = 1;
                img.style.maxWidth = new Length(100, LengthUnit.Percent);
                img.style.maxHeight = new Length(100, LengthUnit.Percent);
                img.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
                img.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
                cgScreen.Add(img);
            }
            else
            {
                Debug.LogWarning("[VN] CG image not found: cg/" + cgName);
                var placeholder = new Label("CG 待生成：「" + cgName + "」");
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
            // 话完成：若有新增解锁 → 先展示奖励弹窗，再显示节过渡
            var newUnlocks = TitleArchiveUI.TakePendingUnlocks();
            if (newUnlocks != null && newUnlocks.Length > 0)
            {
                ShowRewardPopup(newUnlocks, () => ShowEpisodeClear(curName, next));
            }
            else
            {
                ShowEpisodeClear(curName, next);
            }
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

    /// <summary>节结束过渡：渐暗→To be continued→下一话横幅→点击继续。</summary>
    private void ShowEpisodeClear(string completedScript, string nextScript)
    {
        if (episodeClearOverlay == null)
        {
            var root = uiDoc.rootVisualElement;
            episodeClearOverlay = new VisualElement { name = "episode-clear" };
            episodeClearOverlay.style.position = Position.Absolute;
            episodeClearOverlay.style.top = 0; episodeClearOverlay.style.left = 0;
            episodeClearOverlay.style.right = 0; episodeClearOverlay.style.bottom = 0;
            episodeClearOverlay.style.backgroundColor = new Color(0, 0, 0, 0.6f); // 半透明，场景隐约可见
            episodeClearOverlay.style.alignItems = Align.Center;
            episodeClearOverlay.style.justifyContent = Justify.Center;
            episodeClearOverlay.pickingMode = PickingMode.Position;
            episodeClearOverlay.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target == episodeClearOverlay)
                {
                    episodeClearOverlay.style.display = DisplayStyle.None;
                    StartScript(nextScript);
                }
            });
            root.Add(episodeClearOverlay);
        }
        episodeClearOverlay.Clear();

        // —— 横幅深层：枕头纹理底 + 双层边框（旧玻璃质感） ——
        var banner = new VisualElement();
        banner.style.backgroundColor = new Color(0.14f, 0.09f, 0.05f, 0.96f);
        banner.style.minWidth = 520;
        banner.style.paddingLeft = 60;
        banner.style.paddingRight = 60;
        banner.style.paddingTop = 32;
        banner.style.paddingBottom = 32;
        banner.style.alignItems = Align.Center;
        // 双层边框：外细金 + 内暗
        banner.style.borderTopWidth = 2; banner.style.borderBottomWidth = 2;
        banner.style.borderLeftWidth = 2; banner.style.borderRightWidth = 2;
        banner.style.borderTopColor = new Color(0.8f, 0.62f, 0.35f, 0.9f);
        banner.style.borderBottomColor = new Color(0.8f, 0.62f, 0.35f, 0.9f);
        banner.style.borderLeftColor = new Color(0.8f, 0.62f, 0.35f, 0.9f);
        banner.style.borderRightColor = new Color(0.8f, 0.62f, 0.35f, 0.9f);
        episodeClearOverlay.Add(banner);

        // —— 四角信号角标（铁路信号机符号，细线 2px） ——
        AddCornerMark(banner, "▲", "top", "left");
        AddCornerMark(banner, "■", "top", "right");
        AddCornerMark(banner, "◀", "bottom", "left");
        AddCornerMark(banner, "●", "bottom", "right");

        // —— 织光芒晕（横幅后深层光晕） ——
        var glow = new VisualElement();
        glow.style.position = Position.Absolute;
        glow.style.top = 6; glow.style.left = 6; glow.style.right = 6; glow.style.bottom = 6;
        glow.style.backgroundColor = new Color(1f, 0.85f, 0.5f, 0.05f);
        glow.pickingMode = PickingMode.Ignore;
        banner.Add(glow);

        // —— "下一话" 标签 + 金色渐变下划线 ——
        var nextLabel = new Label("下一话");
        nextLabel.style.fontSize = 20;
        nextLabel.style.color = new Color(0.9f, 0.72f, 0.45f, 0.95f);
        nextLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nextLabel.style.unityFontDefinition = new FontDefinition { font = gameFont };
        nextLabel.style.marginBottom = 4;
        banner.Add(nextLabel);

        // 金色下划线：渐变淡出（细线 + 两端透明）
        var underline = new VisualElement();
        underline.style.width = 90;
        underline.style.height = 2;
        underline.style.marginBottom = 18;
        underline.style.backgroundColor = new Color(1f, 0.78f, 0.4f, 0.35f); // 实际可做渐变，UI Toolkit 简化
        banner.Add(underline);

        // —— 话数徽章 ——
        string nextTitle = GetEpisodeTitle(nextScript);
        int epNum = GetEpisodeNumber(nextScript);
        var epBadge = new Label("第" + epNum + "话  " + nextTitle);
        epBadge.style.fontSize = 30;
        epBadge.style.color = new Color(1f, 0.95f, 0.85f, 1f);
        epBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
        epBadge.style.unityFontDefinition = new FontDefinition { font = gameFont };
        epBadge.style.marginBottom = 6;
        banner.Add(epBadge);

        // —— 话标题副标签 — —
        var hint = new Label("[ 点击继续 ]");
        hint.style.fontSize = 16;
        hint.style.color = new Color(1f, 1f, 1f, 0.35f);
        hint.style.unityFontDefinition = new FontDefinition { font = gameFont };
        banner.Add(hint);

        episodeClearOverlay.style.display = DisplayStyle.Flex;
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
        if (string.IsNullOrEmpty(speaker)) return speaker;
        if (speaker == "林彪悍")
        {
            var config = GameConfig.Load();
            return config.PlayerDisplayName;
        }
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
            StartAutoPlayTimer();
        else
            StopAutoPlay();
    }

    private void UpdateAutoPlayButtonVisual()
    {
        if (autoBtn == null) return;
        if (isAutoPlay)
        {
            // 激活：播放像素图标 + 棋盘格暖金底 + 双层金边
            autoBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.ActivePattern());
            autoBtn.style.unityBackgroundImageTintColor = Color.white;
            autoBtn.style.backgroundColor = new Color(0.45f, 0.28f, 0.12f, 0.95f);
            autoBtn.style.color = new Color(1f, 0.9f, 0.55f, 1f);
            autoBtn.text = "Auto";
            autoIcon.style.display = DisplayStyle.Flex;
            autoIcon.style.backgroundImage = new StyleBackground(PixelIconHelper.PlayIcon());
            autoIcon.style.unityBackgroundImageTintColor = new Color(1f, 0.85f, 0.5f, 1f);
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
        StopAutoPlay();
        if (isAutoPlay && isScriptRunning)
            autoPlayCoroutine = StartCoroutine(AutoPlayTimer());
    }

    private IEnumerator AutoPlayTimer()
    {
        yield return new WaitForSeconds(autoPlayDelay);

        if (!isAutoPlay || !isScriptRunning) yield break;

        if (dialogueBox != null && dialogueBox.IsTyping())
            dialogueBox.SkipTyping();
        else
            NextDialogue();
    }

    private void StopAutoPlay()
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
    }
}
