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

        // 站长日志·故事回看：跳播指定章节脚本（从头播，不进读档）
        string replay = PlayerPrefs.GetString("VN_ReplayScript", "");
        if (!string.IsNullOrEmpty(replay))
        {
            PlayerPrefs.DeleteKey("VN_ReplayScript");
            PlayerPrefs.Save();
            StartScript(replay);
            return;
        }

        StartScript("prologue_01_news");
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

        // 根元素不捕获点击，只有交互元素（按钮等）才捕获
        uiDoc.rootVisualElement.pickingMode = PickingMode.Ignore;

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
        autoBtn.style.width = 80;
        autoBtn.style.height = 40;
        autoBtn.style.flexShrink = 0;
        autoBtn.style.fontSize = 20;
        autoBtn.style.color = new Color(1f, 1f, 1f, 0.8f);
        autoBtn.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.85f);
        autoBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        autoBtn.style.marginRight = 6;
        autoBtn.style.unityFontDefinition = fontDef;
        autoBtn.style.borderTopWidth = 1;
        autoBtn.style.borderBottomWidth = 1;
        autoBtn.style.borderLeftWidth = 1;
        autoBtn.style.borderRightWidth = 1;
        autoBtn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        autoBtn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        autoBtn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        autoBtn.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        autoBtn.style.borderTopLeftRadius = 6;
        autoBtn.style.borderTopRightRadius = 6;
        autoBtn.style.borderBottomLeftRadius = 6;
        autoBtn.style.borderBottomRightRadius = 6;
        menuBar.Add(autoBtn);

        // Menu 按钮（点击展开/收起子菜单）
        var menuBtn = new UnityEngine.UIElements.Button(() => ToggleMenuExpanded()) { text = "Menu" };
        menuBtn.style.width = 80;
        menuBtn.style.height = 40;
        menuBtn.style.flexShrink = 0;
        menuBtn.style.fontSize = 20;
        menuBtn.style.color = new Color(1f, 1f, 1f, 0.8f);
        menuBtn.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.85f);
        menuBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        menuBtn.style.unityFontDefinition = fontDef;
        menuBtn.style.borderTopWidth = 1;
        menuBtn.style.borderBottomWidth = 1;
        menuBtn.style.borderLeftWidth = 1;
        menuBtn.style.borderRightWidth = 1;
        menuBtn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        menuBtn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        menuBtn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        menuBtn.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
        menuBtn.style.borderTopLeftRadius = 6;
        menuBtn.style.borderTopRightRadius = 6;
        menuBtn.style.borderBottomLeftRadius = 6;
        menuBtn.style.borderBottomRightRadius = 6;
        menuBar.Add(menuBtn);

        // 子菜单（水平排列图标按钮，初始隐藏）
        menuExpandedContainer = new VisualElement { name = "menu-expanded" };
        menuExpandedContainer.style.position = Position.Absolute;
        menuExpandedContainer.style.top = 48;
        menuExpandedContainer.style.right = 10;
        menuExpandedContainer.style.flexDirection = FlexDirection.Row;
        menuExpandedContainer.style.alignItems = Align.Center;
        menuExpandedContainer.style.display = DisplayStyle.None;
        root.Add(menuExpandedContainer);

        // 子菜单项：简洁符号（非文字/非emoji）
        var menuItems = new (string icon, System.Action action)[]
        {
            ("\u25A1", () => OpenSaveMenu()),    // □ 存档
            ("\u25A3", () => OpenLoadMenu()),    // ▣ 取档
            ("\u2261", () => ToggleBacklog()),    // ≡ 三条线回顾
            ("\u25B8", () => SkipToNext()),      // ▸ 跳转
            ("\u2605", () => OpenArchivePanel()), // ★ 日志
            ("\u2715", () => ShowConfirmDialog()) // ✕ 返回
        };

        for (int i = 0; i < menuItems.Length; i++)
        {
            var (icon, action) = menuItems[i];
            var btn = new UnityEngine.UIElements.Button(() => action()) { text = icon };
            btn.style.width = 50;
            btn.style.height = 40;
            btn.style.fontSize = 18;
            btn.style.color = new Color(1f, 1f, 1f, 0.8f);
            btn.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.85f);
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.unityFontDefinition = fontDef;
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
            btn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
            btn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
            btn.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.25f);
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6;
            btn.style.borderBottomRightRadius = 6;
            btn.style.marginLeft = 4;
            menuExpandedContainer.Add(btn);
        }

        UpdateAutoPlayButtonVisual();
        SetupConfirmDialog(uiDoc);
    }

    private bool menuExpanded;
    private VisualElement menuExpandedContainer;

    private void ToggleMenuExpanded()
    {
        menuExpanded = !menuExpanded;
        menuExpandedContainer.style.display = menuExpanded ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void CloseMenuExpanded()
    {
        if (menuExpanded)
        {
            menuExpanded = false;
            menuExpandedContainer.style.display = DisplayStyle.None;
        }
    }

    /// <summary>Skip 跳转：快速推进到下一选项/下一场景。</summary>
    private void SkipToNext()
    {
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

        // 面板关闭时恢复菜单栏显示
        if (menuBar != null && menuBar.style.display == DisplayStyle.None)
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

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || KeyBindings.IsDown(KeyBindings.Action.Advance))
        {
            // UI Toolkit 按钮的 clicked 在 PointerUp 才触发，而 Input 按下检测先于它：
            // 若不拦截，点任何 UI 按钮的这次点击会把对话推进一句
            if (Input.GetMouseButtonDown(0) && IsPointerOverInteractiveUI())
                return;
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
    /// 判断鼠标指针当前是否落在菜单栏（或其按钮）上方。
    /// 用于拦截"点击菜单按钮时对话被意外推进"：UI Toolkit 的 clicked 在
    /// PointerUp 才触发，而 Update 中 Input.GetMouseButtonDown(0) 在按下帧先执行。
    /// </summary>
    private bool IsPointerOverMenuBar()
    {
        if (uiDoc == null || menuBar == null) return false;
        if (menuBar.style.display == DisplayStyle.None) return false;

        var panel = uiDoc.rootVisualElement.panel;
        var screenPos = Input.mousePosition;
        var localPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
        var picked = panel.Pick(localPos);
        var cur = picked;
        while (cur != null)
        {
            if (cur == menuBar) return true;
            cur = cur.parent;
        }
        return false;
    }

    private bool IsPointerOverInteractiveUI()
    {
        if (uiDoc == null) return false;
        var panel = uiDoc.rootVisualElement.panel;
        var screenPos = Input.mousePosition;
        var localPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
        var picked = panel.Pick(localPos);
        if (picked == null) return false;
        var cur = picked;
        while (cur != null)
        {
            if (cur is UnityEngine.UIElements.Button) return true;
            cur = cur.parent;
        }
        return false;
    }

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
        ShowCurrentDialogue();
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

        // 检查是否有下一个剧本需要自动加载（序章链）
        if (currentScript != null && !string.IsNullOrEmpty(currentScript.nextScript))
        {
            string next = currentScript.nextScript;
            string curName = currentScriptName;
            currentScript = null;
            currentScriptName = null;
            // 显示节过渡画面，然后加载下一节
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

    /// <summary>节结束过渡：渐暗→To be continued→Next Episode→点击继续。</summary>
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

        // "To be continued..." 右下角
        var tbc = new Label("To be continued...");
        tbc.style.position = Position.Absolute;
        tbc.style.bottom = 40;
        tbc.style.right = 40;
        tbc.style.fontSize = 22;
        tbc.style.color = new Color(1f, 200f / 255f, 0f, 0.9f); // 黄色
        tbc.style.unityFontStyleAndWeight = FontStyle.Bold;
        tbc.style.unityFontDefinition = new FontDefinition { font = gameFont };
        episodeClearOverlay.Add(tbc);

        // Next Episode 横幅
        var banner = new VisualElement();
        banner.style.backgroundColor = new Color(0.15f, 0.10f, 0.06f, 0.95f);
        banner.style.paddingLeft = 40;
        banner.style.paddingRight = 40;
        banner.style.paddingTop = 20;
        banner.style.paddingBottom = 20;
        banner.style.borderTopLeftRadius = 8;
        banner.style.borderTopRightRadius = 8;
        banner.style.borderBottomLeftRadius = 8;
        banner.style.borderBottomRightRadius = 8;
        banner.style.alignItems = Align.Center;
        episodeClearOverlay.Add(banner);

        var nextLabel = new Label("Next Episode");
        nextLabel.style.fontSize = 18;
        nextLabel.style.color = new Color(1f, 200f / 255f, 100f / 255f, 0.8f);
        nextLabel.style.unityFontDefinition = new FontDefinition { font = gameFont };
        nextLabel.style.marginBottom = 8;
        banner.Add(nextLabel);

        // 查找下一节标题
        string nextTitle = GetEpisodeTitle(nextScript);
        var epTitle = new Label(nextTitle);
        epTitle.style.fontSize = 28;
        epTitle.style.color = new Color(1f, 1f, 1f, 0.95f);
        epTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        epTitle.style.unityFontDefinition = new FontDefinition { font = gameFont };
        banner.Add(epTitle);

        var hint = new Label("Touch to continue");
        hint.style.fontSize = 16;
        hint.style.color = new Color(1f, 1f, 1f, 0.4f);
        hint.style.marginTop = 16;
        hint.style.unityFontDefinition = new FontDefinition { font = gameFont };
        banner.Add(hint);

        episodeClearOverlay.style.display = DisplayStyle.Flex;
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
            autoBtn.style.backgroundColor = new Color(0.6f, 0.35f, 0.15f, 0.9f);
            autoBtn.style.color = new Color(1f, 0.9f, 0.6f, 1f);
        }
        else
        {
            autoBtn.style.backgroundColor = new Color(0.2f, 0.1f, 0.08f, 0.7f);
            autoBtn.style.color = new Color(1f, 1f, 1f, 0.8f);
        }
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
        // 从标题界面继续：立即清空画面等待读档界面
        if (menuBar != null) menuBar.style.display = DisplayStyle.None;
        if (dialogueBox != null) dialogueBox.Hide();
        if (optionsContainer != null) optionsContainer.style.display = DisplayStyle.None;
        characterSpriteManager?.ClearAll();
        if (backgroundManager != null) backgroundManager.SetBackgroundImmediate("black");
    }

    private IEnumerator ShowLoadUIDelayed()
    {
        yield return null;
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
