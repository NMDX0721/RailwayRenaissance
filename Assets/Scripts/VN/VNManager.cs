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
        SetupEventSystem();
        StartScript("prologue_01_news");
    }

    private void SetupGameCursor()
    {
        if (LoginManager.cursorTexture == null)
            LoginManager.cursorTexture = LoginManager.LoadCursorTexture("Cursors/cursor_arrow", 3);
        if (handCursorTex == null)
            handCursorTex = LoginManager.LoadCursorTexture("Cursors/cursor_hand", 3);
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

        menuBar = new VisualElement { name = "menu-bar" };
        menuBar.pickingMode = PickingMode.Position;
        menuBar.style.position = Position.Absolute;
        menuBar.style.top = 10;
        menuBar.style.right = 10;
        menuBar.style.flexDirection = FlexDirection.Row;
        menuBar.style.alignItems = Align.Center;
        root.Add(menuBar);

        string[] menuLabels = { "回顾", "存档", "读档", "自动" };
        System.Action[] menuActions = {
            () => ToggleBacklog(),
            () => OpenSaveMenu(),
            () => OpenLoadMenu(),
            () => ToggleAutoPlay()
        };

        for (int i = 0; i < menuLabels.Length; i++)
        {
            int idx = i;
            var btn = new UnityEngine.UIElements.Button(() => menuActions[idx]()) { text = menuLabels[idx] };
            btn.style.width = 90;
            btn.style.height = 40;
            btn.style.flexShrink = 0;
            btn.style.fontSize = 20;
            btn.style.color = new Color(1f, 1f, 1f, 0.8f);
            btn.style.backgroundColor = new Color(0.2f, 0.1f, 0.08f, 0.7f);
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.marginLeft = 8;
            btn.style.marginRight = 8;
            btn.style.unityFontDefinition = fontDef;
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
            btn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
            btn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
            btn.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
            AddCursorHover(btn);
            menuBar.Add(btn);

            if (menuLabels[idx] == "自动")
                autoBtn = btn;
        }

        UpdateAutoPlayButtonVisual();
        SetupConfirmDialog(uiDoc);
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

    private void ShowConfirmDialog()
    {
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
        characterSpriteManager?.ClearAll();
        VNAudioManager.Instance?.StopBGM();
        confirmDialog.style.display = DisplayStyle.None;

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

    private void Update()
    {
        // 全屏新闻显示时忽略所有输入
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
        // ESC键返回标题界面
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
                return;
            }
            if (isScriptRunning)
            {
                ShowConfirmDialog();
                return;
            }
        }

        if (!isScriptRunning) return;

        // F5：开启Auto模式，打字中则等待，打完则跳下一页
        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (!isAutoPlay)
                ToggleAutoPlay();
            else if (dialogueBox != null && !dialogueBox.IsTyping())
                NextDialogue();
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
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

    public void StartScript(string scriptName)
    {
        if (string.IsNullOrEmpty(scriptName)) return;
        currentScript = jsonParser.LoadScript(scriptName);
        if (currentScript == null) { Debug.LogError("Failed to load: " + scriptName); return; }
        currentScriptName = scriptName;
        currentSceneIndex = 0;
        currentDialogueIndex = 0;
        isScriptRunning = true;

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
        else
        {
            HideOptions();
            dialogueBox?.ShowDialogue(entry.s, entry.text);

            if (entry.chars != null && entry.chars.Length > 0)
                characterSpriteManager?.UpdateDisplay(entry.chars);

            vnBacklog?.AddEntry(entry.s, entry.text, currentSceneIndex, currentDialogueIndex);
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

    private void EndScript()
    {
        StopAutoPlay();
        isScriptRunning = false;
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
        string valStr = parts[1].Trim().ToLower();
        bool value = valStr == "true" || valStr == "1";
        SetVariable(varName, value);
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
