using UnityEngine;
using UnityEngine.UIElements;

public class StationBulletinUI : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;
    private VisualElement overlay;
    private VisualElement panel;
    private VisualElement contentPanel;
    private VisualElement menuList;
    private string[] menuItems = { "音频", "游戏", "显示", "操作", "关于" };
    private string[] menuIcons = { "\u266B", "\u2699", "\u2600", "\u2328", "\u24D8" };
    private int selectedIndex = 0;
    private System.Action<KeyCode> _rebindAction;
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

    private static readonly Color CGold = new Color(1f, 200f / 255f, 100f / 255f, 1f);
    private static readonly Color CGoldDim = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
    private static readonly Color CBg = new Color(0.12f, 0.08f, 0.05f, 0.97f);
    private static readonly Color CBtn = new Color(0.2f, 0.12f, 0.08f, 0.85f);
    private static readonly Color CText = new Color(1f, 1f, 1f, 0.85f);
    private static readonly Color CTextDim = new Color(1f, 1f, 1f, 0.4f);

    public void Init(UIDocument document)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildUI();
        ShowCategory(0);
    }

    private FontDefinition GetFontDef() => new FontDefinition { font = gameFont };

    private void BuildUI()
    {
        var root = uiDoc.rootVisualElement;
        var fd = GetFontDef();

        overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0; overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        overlay.style.display = DisplayStyle.None;
        overlay.RegisterCallback<ClickEvent>(e => { if (e.target == overlay) Hide(); });
        root.Add(overlay);

        panel = new VisualElement();
        panel.style.backgroundColor = CBg;
        panel.style.borderTopWidth = 2; panel.style.borderBottomWidth = 2;
        panel.style.borderLeftWidth = 2; panel.style.borderRightWidth = 2;
        panel.style.borderTopColor = CGoldDim; panel.style.borderBottomColor = CGoldDim;
        panel.style.borderLeftColor = CGoldDim; panel.style.borderRightColor = CGoldDim;
        panel.style.borderTopLeftRadius = 12; panel.style.borderTopRightRadius = 12;
        panel.style.borderBottomLeftRadius = 12; panel.style.borderBottomRightRadius = 12;
        panel.style.width = 850;
        panel.style.height = 640;
        panel.style.flexDirection = FlexDirection.Column;
        overlay.Add(panel);

        // Header
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.paddingLeft = 24; header.style.paddingRight = 20;
        header.style.paddingTop = 18; header.style.paddingBottom = 14;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = CGoldDim;
        panel.Add(header);

        var title = new Label("站务公告");
        title.style.fontSize = 28;
        title.style.color = CGold;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = fd;
        header.Add(title);

        var closeBtn = new UnityEngine.UIElements.Button(() => Hide()) { text = "\u2715" };
        closeBtn.style.width = 38; closeBtn.style.height = 30;
        closeBtn.style.fontSize = 18;
        closeBtn.style.color = new Color(1f, 1f, 1f, 0.6f);
        closeBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.4f);
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = fd;
        closeBtn.style.borderTopLeftRadius = 6; closeBtn.style.borderTopRightRadius = 6;
        closeBtn.style.borderBottomLeftRadius = 6; closeBtn.style.borderBottomRightRadius = 6;
        closeBtn.RegisterCallback<PointerEnterEvent>(e => closeBtn.style.backgroundColor = new Color(0.5f, 0.2f, 0.1f, 0.6f));
        closeBtn.RegisterCallback<PointerLeaveEvent>(e => closeBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.4f));
        header.Add(closeBtn);

        // Body
        var body = new VisualElement();
        body.style.flexDirection = FlexDirection.Row;
        body.style.flexGrow = 1;
        panel.Add(body);

        // Left menu
        menuList = new VisualElement();
        menuList.style.width = 150;
        menuList.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.5f);
        menuList.style.paddingTop = 14;
        menuList.style.paddingBottom = 14;
        body.Add(menuList);

        for (int i = 0; i < menuItems.Length; i++)
        {
            int idx = i;
            var btn = new UnityEngine.UIElements.Button(() => SelectCategory(idx)) { text = menuIcons[i] + " " + menuItems[i] };
            btn.name = "settings-menu-" + i;
            btn.style.width = 140;
            btn.style.height = 44;
            btn.style.fontSize = 18;
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            btn.style.unityFontDefinition = fd;
            btn.style.marginLeft = 5;
            btn.style.marginBottom = 4;
            btn.style.paddingLeft = 12;
            btn.style.borderTopLeftRadius = 8;
            btn.style.borderBottomLeftRadius = 8;
            btn.style.borderTopRightRadius = 0;
            btn.style.borderBottomRightRadius = 0;
            btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 0;
            btn.style.borderTopColor = CGoldDim; btn.style.borderBottomColor = CGoldDim;
            btn.style.borderLeftColor = CGoldDim;
            menuList.Add(btn);
        }

        // Right content panel with scroll
        var scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        scrollView.style.paddingLeft = 20;
        scrollView.style.paddingRight = 20;
        scrollView.style.paddingTop = 14;
        scrollView.style.paddingBottom = 14;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        body.Add(scrollView);

        contentPanel = new VisualElement();
        scrollView.Add(contentPanel);

        SelectCategory(0);
    }

    private void SelectCategory(int idx)
    {
        selectedIndex = idx;
        for (int i = 0; i < menuItems.Length; i++)
        {
            var btn = menuList.Q<UnityEngine.UIElements.Button>("settings-menu-" + i);
            if (btn == null) continue;
            if (i == idx)
            {
                btn.style.backgroundColor = new Color(0.25f, 0.12f, 0.06f, 0.95f);
                btn.style.color = CGold;
                btn.style.borderLeftColor = new Color(1f, 200f / 255f, 100f / 255f, 0.8f);
                btn.style.borderLeftWidth = 2;
            }
            else
            {
                btn.style.backgroundColor = new Color(0.08f, 0.05f, 0.03f, 0.6f);
                btn.style.color = new Color(1f, 1f, 1f, 0.55f);
                btn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.2f);
                btn.style.borderLeftWidth = 1;
            }
        }
        ShowCategory(idx);
    }

    private void ShowCategory(int idx)
    {
        contentPanel.Clear();
        switch (idx)
        {
            case 0: BuildAudioSettings(); break;
            case 1: BuildGameSettings(); break;
            case 2: BuildDisplaySettings(); break;
            case 3: BuildControlSettings(); break;
            case 4: BuildAboutSettings(); break;
        }
    }

    private void BuildAudioSettings()
    {
        AddSectionTitle("音频", "主音量、音乐、音效独立调节");
        AddSliderWithCallback("主音量", 0, 100, Mathf.RoundToInt(GameData.MasterVolume * 100), v => GameData.MasterVolume = v / 100f);
        AddSliderWithCallback("背景音乐", 0, 100, Mathf.RoundToInt(GameData.BGMVolume * 100), v => GameData.BGMVolume = v / 100f);
        AddSliderWithCallback("音效", 0, 100, Mathf.RoundToInt(GameData.SFXVolume * 100), v => GameData.SFXVolume = v / 100f);
        AddSliderWithCallback("打字机音效", 0, 100, Mathf.RoundToInt(GameData.TypewriterVolume * 100), v => GameData.TypewriterVolume = v / 100f);
        AddResetButton(ResetAudioDefaults);
    }

    private void ResetAudioDefaults()
    {
        GameData.MasterVolume = 1f; GameData.BGMVolume = 0.8f; GameData.SFXVolume = 1f; GameData.TypewriterVolume = 0.5f;
        ShowCategory(0);
    }

    private void BuildGameSettings()
    {
        AddSectionTitle("游戏", "自动播放、跳过等游戏行为设置");
        AddToggle("自动播放模式", "开场后自动推进对话", false);
        AddToggle("跳过已读文本", "已读过的对话自动跳过", true);
        AddToggle("点击对话框推进", "点击对话框区域触发下一句", true);
        AddToggle("确认对话框", "关闭游戏时显示确认", true);
        AddSlider("自动播放间隔", 1, 10, 3, "秒");
        AddResetButton(ResetGameDefaults);
    }

    private void ResetGameDefaults() { ShowCategory(1); }

    private void BuildDisplaySettings()
    {
        AddSectionTitle("显示", "画面和文字显示相关的设置");
        AddToggle("全屏模式", "以全屏方式运行游戏", Screen.fullScreen, v => Screen.fullScreen = v);
        AddToggle("垂直同步", "与自定义帧率互斥，开启后帧率与显示器刷新率同步", QualitySettings.vSyncCount > 0, v =>
        {
            QualitySettings.vSyncCount = v ? 1 : 0;
            PlayerPrefs.SetInt("VSync", v ? 1 : 0);
            PlayerPrefs.SetInt("CustomFPS", v ? 0 : 1);
            PlayerPrefs.Save();
            if (v) Application.targetFrameRate = -1;
            else Application.targetFrameRate = PlayerPrefs.GetInt("TargetFPS", 60);
            ShowCategory(2);
        });
        AddToggle("自定义帧率", "关闭垂直同步后手动限制帧率", PlayerPrefs.GetInt("CustomFPS", 1) == 1, v =>
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
            ShowCategory(2);
        });
        bool fpsCapEnabled = PlayerPrefs.GetInt("CustomFPS", 1) == 1;
        AddSliderWithCallback("帧率上限", 30, 240, PlayerPrefs.GetInt("TargetFPS", 60), v =>
        {
            Application.targetFrameRate = v;
            PlayerPrefs.SetInt("TargetFPS", v);
            PlayerPrefs.Save();
        }, "FPS", fpsCapEnabled);
        AddToggle("显示帧率", "在角落显示当前 FPS", PlayerPrefs.GetInt("ShowFPS", 0) == 1, v => FPSDisplay.SetActive(v));
        AddSlider("文字速度", 1, 10, 5, "档");
        AddSlider("对话框透明度", 0, 100, 80, "%");
        AddResetButton(ResetDisplayDefaults);
    }

    private void ResetDisplayDefaults() { ShowCategory(2); }

    private void BuildControlSettings()
    {
        AddSectionTitle("操作", "点击按键绑定，然后按下新键以更改");
        foreach (KeyBindings.Action action in System.Enum.GetValues(typeof(KeyBindings.Action)))
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 10;
            row.style.paddingLeft = 10; row.style.paddingRight = 10;
            row.style.backgroundColor = new Color(0.08f, 0.05f, 0.03f, 0.4f);
            row.style.borderTopLeftRadius = 6; row.style.borderTopRightRadius = 6;
            row.style.borderBottomLeftRadius = 6; row.style.borderBottomRightRadius = 6;
            row.style.paddingTop = 6; row.style.paddingBottom = 6;

            var lbl = new Label(KeyBindings.GetActionName(action));
            lbl.style.fontSize = 20; lbl.style.color = CText;
            lbl.style.unityFontDefinition = GetFontDef();
            row.Add(lbl);

            UnityEngine.UIElements.Button keyBtn = null;
            keyBtn = new UnityEngine.UIElements.Button(() => StartRebind(action, keyBtn)) { text = KeyBindings.GetKeyName(action) };
            keyBtn.style.width = 120; keyBtn.style.height = 36; keyBtn.style.fontSize = 18;
            keyBtn.style.color = new Color(1f, 0.8f, 0.5f, 0.9f);
            keyBtn.style.backgroundColor = new Color(0.2f, 0.12f, 0.08f, 0.7f);
            keyBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            keyBtn.style.unityFontDefinition = GetFontDef();
            keyBtn.style.borderTopLeftRadius = 6; keyBtn.style.borderTopRightRadius = 6;
            keyBtn.style.borderBottomLeftRadius = 6; keyBtn.style.borderBottomRightRadius = 6;
            keyBtn.style.borderTopWidth = 1; keyBtn.style.borderBottomWidth = 1;
            keyBtn.style.borderLeftWidth = 1; keyBtn.style.borderRightWidth = 1;
            keyBtn.style.borderTopColor = CGoldDim; keyBtn.style.borderBottomColor = CGoldDim;
            keyBtn.style.borderLeftColor = CGoldDim; keyBtn.style.borderRightColor = CGoldDim;
            keyBtn.RegisterCallback<PointerEnterEvent>(e => keyBtn.style.backgroundColor = new Color(0.3f, 0.18f, 0.1f, 0.85f));
            keyBtn.RegisterCallback<PointerLeaveEvent>(e => keyBtn.style.backgroundColor = new Color(0.2f, 0.12f, 0.08f, 0.7f));
            row.Add(keyBtn);
            contentPanel.Add(row);
        }
        AddResetButton(ResetControlDefaults);
    }

    private void ResetControlDefaults()
    {
        foreach (KeyBindings.Action action in System.Enum.GetValues(typeof(KeyBindings.Action)))
            KeyBindings.SetKey(action, KeyBindings.GetKey(action));
        ShowCategory(3);
    }

    private void StartRebind(KeyBindings.Action action, UnityEngine.UIElements.Button btn)
    {
        btn.text = "按下新键...";
        btn.style.color = new Color(1f, 1f, 0.5f, 1f);
        System.Action<KeyCode> onKey = null;
        onKey = (key) =>
        {
            if (key != KeyCode.None && key != KeyCode.Escape)
            {
                KeyBindings.SetKey(action, key);
                btn.text = KeyBindings.GetKeyName(action);
                btn.style.color = new Color(1f, 0.8f, 0.5f, 0.9f);
            }
            else
            {
                btn.text = KeyBindings.GetKeyName(action);
                btn.style.color = new Color(1f, 0.8f, 0.5f, 0.9f);
            }
            _rebindAction = null;
        };
        _rebindAction = onKey;
    }

    private void Update()
    {
        if (_rebindAction != null)
        {
            foreach (KeyCode code in AllKeyCodes)
            {
                if (Input.GetKeyDown(code))
                {
                    var cb = _rebindAction;
                    _rebindAction = null;
                    cb(code);
                    return;
                }
            }
        }
    }

    private void BuildAboutSettings()
    {
        AddSectionTitle("关于", "游戏版本和项目信息");
        AddInfoRow("游戏名称", "铁路复兴：沙能冲击");
        // 版本号行：点击 5 次进入调试面板
        var versionRow = new VisualElement();
        versionRow.style.flexDirection = FlexDirection.Row;
        versionRow.style.marginBottom = 6;
        versionRow.style.paddingLeft = 4;
        versionRow.pickingMode = PickingMode.Position;
        contentPanel.Add(versionRow);

        var verLabel = new Label("当前版本");
        verLabel.style.width = 100; verLabel.style.fontSize = 20;
        verLabel.style.color = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.8f);
        verLabel.style.unityFontDefinition = GetFontDef();
        versionRow.Add(verLabel);

        var verValue = new Label(LoginManager.GAME_VERSION);
        verValue.style.fontSize = 20; verValue.style.color = CText;
        verValue.style.whiteSpace = WhiteSpace.Normal;
        verValue.style.unityFontDefinition = GetFontDef();
        versionRow.Add(verValue);

        int verClick = 0;
        versionRow.RegisterCallback<ClickEvent>(evt =>
        {
            verClick++;
            if (verClick >= 5) { verClick = 0; DebugPanel.Show(); }
        });

        AddInfoRow("引擎版本", "Unity 6000.5.8f1");
        AddInfoRow("渲染管线", "Universal Render Pipeline");
        AddInfoRow("开发状态", "核心系统已完成，资产生成进行中");
        AddInfoRow("开源许可", "MIT License");
        AddInfoRow("作者", "NMDX0721");

        var sep = new Label("\u2500\u2500 鸣谢 \u2500\u2500");
        sep.style.fontSize = 18; sep.style.color = CGoldDim;
        sep.style.unityTextAlign = TextAnchor.MiddleCenter;
        sep.style.unityFontDefinition = GetFontDef();
        sep.style.marginTop = 24; sep.style.marginBottom = 12;
        contentPanel.Add(sep);

        AddInfoRow("灵感来源", "爱上火车-Last Run!!- / Stardew Valley");
        AddInfoRow("字体", "Zpix (最像素)");
    }

    private void AddSectionTitle(string title, string desc)
    {
        var label = new Label(title);
        label.style.fontSize = 24; label.style.color = CGold;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityFontDefinition = GetFontDef();
        label.style.marginBottom = 4;
        contentPanel.Add(label);

        if (!string.IsNullOrEmpty(desc))
        {
            var d = new Label(desc);
            d.style.fontSize = 15; d.style.color = CTextDim;
            d.style.unityFontDefinition = GetFontDef();
            d.style.marginBottom = 18;
            d.style.borderBottomWidth = 1;
            d.style.borderBottomColor = CGoldDim;
            d.style.paddingBottom = 8;
            contentPanel.Add(d);
        }
    }

    private void AddToggle(string label, string desc, bool defaultValue)
    {
        AddToggle(label, desc, defaultValue, null);
    }

    private void AddToggle(string label, string desc, bool defaultValue, System.Action<bool> onChanged)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 10;
        row.style.paddingLeft = 4; row.style.paddingRight = 4;

        var textGroup = new VisualElement();
        textGroup.style.flexGrow = 1;
        var lbl = new Label(label);
        lbl.style.fontSize = 20; lbl.style.color = CText;
        lbl.style.unityFontDefinition = GetFontDef();
        textGroup.Add(lbl);
        if (!string.IsNullOrEmpty(desc))
        {
            var d = new Label(desc);
            d.style.fontSize = 14; d.style.color = CTextDim;
            d.style.unityFontDefinition = GetFontDef();
            textGroup.Add(d);
        }
        row.Add(textGroup);

        var toggle = new Toggle();
        toggle.value = defaultValue;
        if (onChanged != null)
            toggle.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
        row.Add(toggle);
        contentPanel.Add(row);
    }

    private void AddSlider(string label, int min, int max, int defaultValue, string unit)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 10;
        row.style.paddingLeft = 4; row.style.paddingRight = 4;

        var lbl = new Label(label);
        lbl.style.width = 120; lbl.style.fontSize = 20; lbl.style.color = CText;
        lbl.style.unityFontDefinition = GetFontDef();
        row.Add(lbl);

        var slider = new Slider("", min, max, SliderDirection.Horizontal, 1f);
        slider.value = defaultValue; slider.style.flexGrow = 1;
        row.Add(slider);

        var valLabel = new Label(defaultValue + " " + unit);
        valLabel.style.width = 60; valLabel.style.fontSize = 16;
        valLabel.style.color = CTextDim;
        valLabel.style.unityFontDefinition = GetFontDef();
        valLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        valLabel.style.backgroundColor = new Color(0.1f, 0.06f, 0.04f, 0.5f);
        valLabel.style.borderTopLeftRadius = 4; valLabel.style.borderTopRightRadius = 4;
        valLabel.style.borderBottomLeftRadius = 4; valLabel.style.borderBottomRightRadius = 4;
        valLabel.style.paddingLeft = 6; valLabel.style.paddingRight = 6;
        valLabel.style.paddingTop = 2; valLabel.style.paddingBottom = 2;
        valLabel.RegisterCallback<PointerEnterEvent>(e => valLabel.style.backgroundColor = new Color(0.2f, 0.12f, 0.06f, 0.7f));
        valLabel.RegisterCallback<PointerLeaveEvent>(e => valLabel.style.backgroundColor = new Color(0.1f, 0.06f, 0.04f, 0.5f));
        valLabel.RegisterCallback<ClickEvent>(e => ShowNumericInput(slider, valLabel, min, max, unit));
        slider.RegisterValueChangedCallback(evt => valLabel.text = Mathf.RoundToInt(evt.newValue) + " " + unit);
        row.Add(valLabel);

        contentPanel.Add(row);
    }

    private void AddSliderWithCallback(string label, int min, int max, int defaultValue, System.Action<int> onChanged, string unit = "%", bool enabled = true)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
        row.style.marginBottom = 12;
        row.style.paddingLeft = 4; row.style.paddingRight = 4;
        var lbl = new Label(label);
        lbl.style.width = 100; lbl.style.fontSize = 20; lbl.style.color = CText;
        lbl.style.unityFontDefinition = GetFontDef();
        row.Add(lbl);

        Label valLabel = null;
        var slider = new Slider("", min, max, SliderDirection.Horizontal, 1f);
        slider.value = defaultValue;
        slider.style.flexGrow = 1;
        if (!enabled) slider.SetEnabled(false);
        slider.RegisterValueChangedCallback(evt =>
        {
            int v = Mathf.RoundToInt(evt.newValue);
            onChanged(v);
            if (valLabel != null) valLabel.text = v + " " + unit;
        });
        slider.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button == 0)
            {
                var rect = slider.worldBound;
                if (rect.width > 0)
                {
                    float ratio = (evt.position.x - rect.x) / rect.width;
                    slider.value = Mathf.Lerp(min, max, Mathf.Clamp01(ratio));
                }
            }
        }, TrickleDown.TrickleDown);
        row.Add(slider);

        valLabel = new Label(defaultValue + " " + unit);
        valLabel.style.width = unit.Length > 3 ? 70 : 55; valLabel.style.fontSize = 16;
        valLabel.style.color = CTextDim;
        valLabel.style.unityFontDefinition = GetFontDef();
        valLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        valLabel.style.overflow = Overflow.Visible;
        valLabel.style.whiteSpace = WhiteSpace.Normal;
        valLabel.style.backgroundColor = new Color(0.1f, 0.06f, 0.04f, 0.5f);
        valLabel.style.borderTopLeftRadius = 4; valLabel.style.borderTopRightRadius = 4;
        valLabel.style.borderBottomLeftRadius = 4; valLabel.style.borderBottomRightRadius = 4;
        valLabel.style.paddingLeft = 6; valLabel.style.paddingRight = 6;
        valLabel.style.paddingTop = 2; valLabel.style.paddingBottom = 2;
        valLabel.RegisterCallback<PointerEnterEvent>(e => {
            if (enabled) valLabel.style.backgroundColor = new Color(0.2f, 0.12f, 0.06f, 0.7f);
        });
        valLabel.RegisterCallback<PointerLeaveEvent>(e => valLabel.style.backgroundColor = new Color(0.1f, 0.06f, 0.04f, 0.5f));
        valLabel.RegisterCallback<ClickEvent>(e => { if (enabled) ShowNumericInput(slider, valLabel, min, max, unit); });
        row.Add(valLabel);

        if (!enabled)
        {
            row.style.opacity = 0.5f;
        }

        contentPanel.Add(row);
    }

    private void ShowNumericInput(Slider slider, Label valLabel, int min, int max, string unit)
    {
        var input = new TextField();
        input.value = Mathf.RoundToInt(slider.value).ToString();
        input.multiline = false;
        input.style.width = 68; input.style.height = 34;
        input.style.flexGrow = 0; input.style.flexShrink = 0;
        input.style.marginLeft = 6; input.style.marginRight = 4;
        input.style.unityFontDefinition = GetFontDef();
        ApplyInputStyle(input);

        void ApplyInputStyle(TextField tf)
        {
            // 内外所有元素：背景透明、边框清零（干掉默认白色输入区+蓝色聚焦圈）、去掉内边距
            foreach (var ve in tf.Query<VisualElement>().ToList())
            {
                ve.style.backgroundColor = Color.clear;
                ve.style.borderTopWidth = 0; ve.style.borderBottomWidth = 0;
                ve.style.borderLeftWidth = 0; ve.style.borderRightWidth = 0;
                ve.style.overflow = Overflow.Visible;
                ve.style.paddingTop = 0; ve.style.paddingBottom = 0;
                ve.style.paddingLeft = 0; ve.style.paddingRight = 0;
                ve.style.height = StyleKeyword.Auto;
            }
            foreach (var te in tf.Query<TextElement>().ToList())
            {
                te.style.unityFontDefinition = GetFontDef();
                te.style.fontSize = 22;
                te.style.color = new Color(1f, 0.97f, 0.85f, 1f); // 亮白金色，高对比
                te.style.unityTextAlign = TextAnchor.MiddleCenter;
                te.style.overflow = Overflow.Visible;
                te.style.whiteSpace = WhiteSpace.Normal;
                te.style.unityFontStyleAndWeight = FontStyle.Bold;
                te.style.marginTop = 0; te.style.marginBottom = 0;
            }
            // 外层：深棕底 + 单层金色边框，垂直居中
            tf.style.backgroundColor = new Color(0.16f, 0.1f, 0.05f, 0.95f);
            tf.style.borderTopWidth = 1; tf.style.borderBottomWidth = 1;
            tf.style.borderLeftWidth = 1; tf.style.borderRightWidth = 1;
            tf.style.borderTopLeftRadius = 6; tf.style.borderTopRightRadius = 6;
            tf.style.borderBottomLeftRadius = 6; tf.style.borderBottomRightRadius = 6;
            tf.style.borderTopColor = new Color(1f, 200f / 255f, 100f / 255f, 0.7f);
            tf.style.borderBottomColor = new Color(1f, 200f / 255f, 100f / 255f, 0.7f);
            tf.style.borderLeftColor = new Color(1f, 200f / 255f, 100f / 255f, 0.7f);
            tf.style.borderRightColor = new Color(1f, 200f / 255f, 100f / 255f, 0.7f);
            tf.style.alignItems = Align.Center;
            tf.style.justifyContent = Justify.Center;
            tf.style.fontSize = 22;
            tf.style.color = new Color(1f, 0.97f, 0.85f, 1f);
        }

        input.RegisterCallback<GeometryChangedEvent>(e => ApplyInputStyle(input));
        // 聚焦时默认主题会加蓝色光圈，显式重新压回金色边框
        input.RegisterCallback<FocusInEvent>(e => ApplyInputStyle(input));

        input.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                int v;
                if (int.TryParse(input.text, out v))
                {
                    v = Mathf.Clamp(v, min, max);
                    slider.value = v;
                    valLabel.text = v + " " + unit;
                }
                input.parent.Remove(input);
                valLabel.style.display = DisplayStyle.Flex;
            }
            if (evt.keyCode == KeyCode.Escape)
            {
                input.parent.Remove(input);
                valLabel.style.display = DisplayStyle.Flex;
            }
        });
        // 点击其他地方自动提交
        input.RegisterCallback<BlurEvent>(evt =>
        {
            int v;
            if (int.TryParse(input.text, out v))
            {
                v = Mathf.Clamp(v, min, max);
                slider.value = v;
                valLabel.text = v + " " + unit;
            }
            input.parent.Remove(input);
            valLabel.style.display = DisplayStyle.Flex;
        });

        valLabel.style.display = DisplayStyle.None;
        valLabel.parent.Add(input);
        input.Focus();
    }

    private void AddResetButton(System.Action onReset)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.FlexEnd;
        row.style.marginTop = 20;
        row.style.borderTopWidth = 1;
        row.style.borderTopColor = CGoldDim;
        row.style.paddingTop = 12;

        var btn = new UnityEngine.UIElements.Button(() => ShowResetConfirm(onReset)) { text = "恢复默认" };
        btn.style.width = 130; btn.style.height = 36; btn.style.fontSize = 18;
        btn.style.color = new Color(1f, 0.8f, 0.5f, 0.8f);
        btn.style.backgroundColor = new Color(0.3f, 0.15f, 0.08f, 0.5f);
        btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = GetFontDef();
        btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
        btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
        btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
        btn.style.borderTopColor = CGoldDim; btn.style.borderBottomColor = CGoldDim;
        btn.style.borderLeftColor = CGoldDim; btn.style.borderRightColor = CGoldDim;
        btn.RegisterCallback<PointerEnterEvent>(e => btn.style.backgroundColor = new Color(0.4f, 0.2f, 0.1f, 0.7f));
        btn.RegisterCallback<PointerLeaveEvent>(e => btn.style.backgroundColor = new Color(0.3f, 0.15f, 0.08f, 0.5f));
        row.Add(btn);
        contentPanel.Add(row);
    }

    private void ShowResetConfirm(System.Action onConfirm)
    {
        var confirmPanel = new VisualElement();
        confirmPanel.style.position = Position.Absolute;
        confirmPanel.style.top = 0; confirmPanel.style.left = 0; confirmPanel.style.right = 0; confirmPanel.style.bottom = 0;
        confirmPanel.style.backgroundColor = new Color(0, 0, 0, 0.6f);
        confirmPanel.style.alignItems = Align.Center;
        confirmPanel.style.justifyContent = Justify.Center;
        overlay.Add(confirmPanel);

        var box = new VisualElement();
        box.style.backgroundColor = CBg;
        box.style.borderTopWidth = 1; box.style.borderBottomWidth = 1;
        box.style.borderLeftWidth = 1; box.style.borderRightWidth = 1;
        box.style.borderTopColor = CGoldDim; box.style.borderBottomColor = CGoldDim;
        box.style.borderLeftColor = CGoldDim; box.style.borderRightColor = CGoldDim;
        box.style.borderTopLeftRadius = 12; box.style.borderTopRightRadius = 12;
        box.style.borderBottomLeftRadius = 12; box.style.borderBottomRightRadius = 12;
        box.style.width = 440; box.style.height = 220;
        box.style.flexDirection = FlexDirection.Column;
        box.style.alignItems = Align.Center;
        box.style.justifyContent = Justify.Center;
        confirmPanel.Add(box);

        var msg = new Label("确定恢复默认设置\uFF1F");
        msg.style.fontSize = 24; msg.style.color = CText;
        msg.style.unityFontDefinition = GetFontDef();
        msg.style.marginBottom = 32;
        box.Add(msg);

        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        btnRow.style.alignItems = Align.Center;
        btnRow.style.justifyContent = Justify.Center;
        box.Add(btnRow);

        // 左侧：确认；右侧：取消
        var confirmBtn = new UnityEngine.UIElements.Button(() => { overlay.Remove(confirmPanel); onConfirm(); }) { text = "确定" };
        confirmBtn.style.width = 120; confirmBtn.style.height = 44; confirmBtn.style.fontSize = 20;
        confirmBtn.style.color = new Color(1f, 0.8f, 0.5f, 1f);
        confirmBtn.style.backgroundColor = new Color(0.4f, 0.2f, 0.08f, 0.6f);
        confirmBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        confirmBtn.style.unityFontDefinition = GetFontDef();
        confirmBtn.style.borderTopLeftRadius = 8; confirmBtn.style.borderTopRightRadius = 8;
        confirmBtn.style.borderBottomLeftRadius = 8; confirmBtn.style.borderBottomRightRadius = 8;
        confirmBtn.style.marginRight = 24;
        confirmBtn.RegisterCallback<PointerEnterEvent>(e => confirmBtn.style.backgroundColor = new Color(0.5f, 0.25f, 0.1f, 0.8f));
        confirmBtn.RegisterCallback<PointerLeaveEvent>(e => confirmBtn.style.backgroundColor = new Color(0.4f, 0.2f, 0.08f, 0.6f));
        btnRow.Add(confirmBtn);

        var cancelBtn = new UnityEngine.UIElements.Button(() => overlay.Remove(confirmPanel)) { text = "取消" };
        cancelBtn.style.width = 120; cancelBtn.style.height = 44; cancelBtn.style.fontSize = 20;
        cancelBtn.style.color = new Color(1f, 1f, 1f, 0.7f);
        cancelBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.5f);
        cancelBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        cancelBtn.style.unityFontDefinition = GetFontDef();
        cancelBtn.style.borderTopLeftRadius = 8; cancelBtn.style.borderTopRightRadius = 8;
        cancelBtn.style.borderBottomLeftRadius = 8; cancelBtn.style.borderBottomRightRadius = 8;
        cancelBtn.RegisterCallback<PointerEnterEvent>(e => cancelBtn.style.backgroundColor = new Color(0.4f, 0.2f, 0.1f, 0.7f));
        cancelBtn.RegisterCallback<PointerLeaveEvent>(e => cancelBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.5f));
        btnRow.Add(cancelBtn);
    }

    private void AddInfoRow(string label, string value)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginBottom = 6;
        row.style.paddingLeft = 4;

        var lbl = new Label(label);
        lbl.style.width = 100; lbl.style.fontSize = 20;
        lbl.style.color = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.8f);
        lbl.style.unityFontDefinition = GetFontDef();
        row.Add(lbl);

        var val = new Label(value);
        val.style.fontSize = 20; val.style.color = CText;
        val.style.whiteSpace = WhiteSpace.Normal;
        val.style.unityFontDefinition = GetFontDef();
        row.Add(val);

        contentPanel.Add(row);
    }

    public void Show() { SelectCategory(0); overlay.style.display = DisplayStyle.Flex; }
    public void Hide() { overlay.style.display = DisplayStyle.None; }
    public bool IsOpen => overlay.style.display == DisplayStyle.Flex;
}