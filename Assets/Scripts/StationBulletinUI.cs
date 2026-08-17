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
    private string[] menuItems = { "音频", "游戏", "显示", "关于" };
    private int selectedIndex = 0;

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
        var fontDef = GetFontDef();

        overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0; overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.4f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        overlay.style.display = DisplayStyle.None;
        overlay.RegisterCallback<ClickEvent>(e => { if (e.target == overlay) Hide(); });
        root.Add(overlay);

        panel = new VisualElement();
        panel.style.backgroundColor = new Color(0.08f, 0.05f, 0.03f, 0.96f);
        panel.style.borderTopWidth = 2; panel.style.borderBottomWidth = 2;
        panel.style.borderLeftWidth = 2; panel.style.borderRightWidth = 2;
        panel.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        panel.style.borderTopLeftRadius = 10; panel.style.borderTopRightRadius = 10;
        panel.style.borderBottomLeftRadius = 10; panel.style.borderBottomRightRadius = 10;
        panel.style.width = 800;
        panel.style.height = 600;
        panel.style.flexDirection = FlexDirection.Column;
        overlay.Add(panel);

        // ── Header ──
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.paddingLeft = 20; header.style.paddingRight = 20;
        header.style.paddingTop = 16; header.style.paddingBottom = 16;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
        panel.Add(header);

        var title = new Label("设置");
        title.style.fontSize = 28;
        title.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = fontDef;
        header.Add(title);

        var closeBtn = new UnityEngine.UIElements.Button(() => Hide()) { text = "X" };
        closeBtn.style.width = 40; closeBtn.style.height = 30;
        closeBtn.style.fontSize = 20;
        closeBtn.style.color = new Color(1f, 1f, 1f, 0.7f);
        closeBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.5f);
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = fontDef;
        header.Add(closeBtn);

        // ── Body: left menu + right content ──
        var body = new VisualElement();
        body.style.flexDirection = FlexDirection.Row;
        body.style.flexGrow = 1;
        panel.Add(body);

        // Left menu
        menuList = new VisualElement();
        menuList.style.width = 140;
        menuList.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.5f);
        menuList.style.paddingTop = 10;
        menuList.style.paddingBottom = 10;
        body.Add(menuList);

        for (int i = 0; i < menuItems.Length; i++)
        {
            int idx = i;
            var menuBtn = new UnityEngine.UIElements.Button(() => SelectCategory(idx)) { text = menuItems[idx] };
            menuBtn.name = "settings-menu-" + i;
            menuBtn.style.width = 130;
            menuBtn.style.height = 42;
            menuBtn.style.fontSize = 20;
            menuBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            menuBtn.style.unityFontDefinition = fontDef;
            menuBtn.style.marginLeft = 5;
            menuBtn.style.marginBottom = 4;
            menuBtn.style.borderTopLeftRadius = 6;
            menuBtn.style.borderBottomLeftRadius = 6;
            menuBtn.style.borderTopRightRadius = 0;
            menuBtn.style.borderBottomRightRadius = 0;
            menuBtn.style.borderTopWidth = 1;
            menuBtn.style.borderBottomWidth = 1;
            menuBtn.style.borderLeftWidth = 1;
            menuBtn.style.borderRightWidth = 0;
            menuBtn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.2f);
            menuBtn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.2f);
            menuBtn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.2f);
            menuList.Add(menuBtn);
        }

        // Right content panel
        contentPanel = new VisualElement();
        contentPanel.style.flexGrow = 1;
        contentPanel.style.paddingLeft = 20;
        contentPanel.style.paddingRight = 20;
        contentPanel.style.paddingTop = 10;
        contentPanel.style.paddingBottom = 10;
        body.Add(contentPanel);

        // Footer
        var footer = new Label("© 2026 NMDX0721 — MIT License");
        footer.style.fontSize = 13;
        footer.style.color = new Color(153f / 255f, 153f / 255f, 153f / 255f, 0.4f);
        footer.style.unityTextAlign = TextAnchor.MiddleCenter;
        footer.style.unityFontDefinition = fontDef;
        footer.style.paddingTop = 8;
        footer.style.paddingBottom = 8;
        footer.style.borderTopWidth = 1;
        footer.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.15f);
        panel.Add(footer);
    }

    private void SelectCategory(int idx)
    {
        selectedIndex = idx;
        // Update menu button styles
        for (int i = 0; i < menuItems.Length; i++)
        {
            var btn = menuList.Q<UnityEngine.UIElements.Button>("settings-menu-" + i);
            if (btn == null) continue;
            if (i == idx)
            {
                btn.style.backgroundColor = new Color(0.2f, 0.1f, 0.06f, 0.9f);
                btn.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
            }
            else
            {
                btn.style.backgroundColor = new Color(0.08f, 0.05f, 0.03f, 0.6f);
                btn.style.color = new Color(1f, 1f, 1f, 0.6f);
            }
        }
        ShowCategory(idx);
    }

    private void ShowCategory(int idx)
    {
        contentPanel.Clear();
        var fontDef = GetFontDef();

        switch (idx)
        {
            case 0: BuildAudioSettings(); break;
            case 1: BuildGameSettings(); break;
            case 2: BuildDisplaySettings(); break;
            case 3: BuildAboutSettings(); break;
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
        GameData.MasterVolume = 1f;
        GameData.BGMVolume = 0.8f;
        GameData.SFXVolume = 1f;
        GameData.TypewriterVolume = 0.5f;
        ShowCategory(0);
    }

    private void AddResetButton(System.Action onReset)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.FlexEnd;
        row.style.marginTop = 20;
        row.style.borderTopWidth = 1;
        row.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.15f);
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
        btn.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
        btn.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
        btn.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
        btn.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
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
        box.style.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 0.97f);
        box.style.borderTopWidth = 1; box.style.borderBottomWidth = 1;
        box.style.borderLeftWidth = 1; box.style.borderRightWidth = 1;
        box.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        box.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        box.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        box.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        box.style.borderTopLeftRadius = 10; box.style.borderTopRightRadius = 10;
        box.style.borderBottomLeftRadius = 10; box.style.borderBottomRightRadius = 10;
        box.style.width = 400; box.style.height = 200;
        box.style.flexDirection = FlexDirection.Column;
        box.style.alignItems = Align.Center;
        box.style.justifyContent = Justify.Center;
        confirmPanel.Add(box);

        var msg = new Label("确定恢复默认设置？");
        msg.style.fontSize = 22;
        msg.style.color = new Color(1f, 1f, 1f, 0.9f);
        msg.style.unityFontDefinition = GetFontDef();
        msg.style.marginBottom = 20;
        box.Add(msg);

        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        btnRow.style.alignItems = Align.Center;
        btnRow.style.justifyContent = Justify.Center;
        box.Add(btnRow);

        var cancelBtn = new UnityEngine.UIElements.Button(() => overlay.Remove(confirmPanel)) { text = "取消" };
        cancelBtn.style.width = 100; cancelBtn.style.height = 36; cancelBtn.style.fontSize = 18;
        cancelBtn.style.color = new Color(1f, 1f, 1f, 0.7f);
        cancelBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.5f);
        cancelBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        cancelBtn.style.unityFontDefinition = GetFontDef();
        cancelBtn.style.marginRight = 16;
        cancelBtn.style.borderTopLeftRadius = 6; cancelBtn.style.borderTopRightRadius = 6;
        cancelBtn.style.borderBottomLeftRadius = 6; cancelBtn.style.borderBottomRightRadius = 6;
        btnRow.Add(cancelBtn);

        var confirmBtn = new UnityEngine.UIElements.Button(() => { overlay.Remove(confirmPanel); onConfirm(); }) { text = "确定" };
        confirmBtn.style.width = 100; confirmBtn.style.height = 36; confirmBtn.style.fontSize = 18;
        confirmBtn.style.color = new Color(1f, 0.8f, 0.5f, 1f);
        confirmBtn.style.backgroundColor = new Color(0.4f, 0.2f, 0.08f, 0.6f);
        confirmBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        confirmBtn.style.unityFontDefinition = GetFontDef();
        confirmBtn.style.borderTopLeftRadius = 6; confirmBtn.style.borderTopRightRadius = 6;
        confirmBtn.style.borderBottomLeftRadius = 6; confirmBtn.style.borderBottomRightRadius = 6;
        btnRow.Add(confirmBtn);
    }

    private void AddSliderWithCallback(string label, int min, int max, int defaultValue, System.Action<int> onChanged)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
        row.style.marginBottom = 12;
        var lbl = new Label(label);
        lbl.style.width = 100; lbl.style.fontSize = 20; lbl.style.color = new Color(1f, 1f, 1f, 0.85f);
        lbl.style.unityFontDefinition = GetFontDef();
        row.Add(lbl);
        var slider = new Slider("", min, max, SliderDirection.Horizontal, 1f);
        slider.value = defaultValue;
        slider.style.flexGrow = 1;
        slider.RegisterValueChangedCallback(evt => onChanged(Mathf.RoundToInt(evt.newValue)));
        // 点击滑条轨道直接跳转（TrickleDown 在内部 handler 之前触发）
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
        contentPanel.Add(row);
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

    private void ResetGameDefaults()
    {
        ShowCategory(1);
    }

    private void BuildDisplaySettings()
    {
        AddSectionTitle("显示", "画面和文字显示相关的设置");

        AddToggle("全屏模式", "以全屏方式运行游戏", true);
        AddToggle("垂直同步", "开启后减少画面撕裂", true);
        AddToggle("显示帧率", "在角落显示当前 FPS", false);
        AddSlider("文字速度", 1, 10, 5, "档");
        AddSlider("对话框透明度", 0, 100, 80, "%");
        AddResetButton(ResetDisplayDefaults);
    }

    private void ResetDisplayDefaults()
    {
        ShowCategory(2);
    }

    private void BuildAboutSettings()
    {
        AddSectionTitle("关于", "游戏版本和项目信息");

        AddInfoRow("游戏名称", "铁路复兴：沙能冲击");
        AddInfoRow("当前版本", "v1.0.0");
        AddInfoRow("引擎版本", "Unity 6000.5.8f1");
        AddInfoRow("渲染管线", "Universal Render Pipeline");
        AddInfoRow("开发状态", "核心系统已完成，资产生成进行中");
        AddInfoRow("开源许可", "MIT License");
        AddInfoRow("作者", "NMDX0721");

        var sep = new Label("── 鸣谢 ──");
        sep.style.fontSize = 18;
        sep.style.color = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        sep.style.unityTextAlign = TextAnchor.MiddleCenter;
        sep.style.unityFontDefinition = GetFontDef();
        sep.style.marginTop = 20; sep.style.marginBottom = 10;
        contentPanel.Add(sep);

        AddInfoRow("灵感来源", "爱上火车-Last Run!!- / Stardew Valley");
        AddInfoRow("字体", "Zpix (最像素)");
    }

    // ── Helpers ──

    private void AddSectionTitle(string title, string desc)
    {
        var label = new Label(title);
        label.style.fontSize = 24;
        label.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityFontDefinition = GetFontDef();
        label.style.marginBottom = 4;
        contentPanel.Add(label);

        if (!string.IsNullOrEmpty(desc))
        {
            var descLabel = new Label(desc);
            descLabel.style.fontSize = 15;
            descLabel.style.color = new Color(1f, 1f, 1f, 0.4f);
            descLabel.style.unityFontDefinition = GetFontDef();
            descLabel.style.marginBottom = 16;
            descLabel.style.borderBottomWidth = 1;
            descLabel.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.15f);
            descLabel.style.paddingBottom = 8;
            contentPanel.Add(descLabel);
        }
    }

    private void AddToggle(string label, string desc, bool defaultValue)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 10;

        var textGroup = new VisualElement();
        textGroup.style.flexGrow = 1;
        var lbl = new Label(label);
        lbl.style.fontSize = 20; lbl.style.color = new Color(1f, 1f, 1f, 0.85f);
        lbl.style.unityFontDefinition = GetFontDef();
        textGroup.Add(lbl);
        if (!string.IsNullOrEmpty(desc))
        {
            var d = new Label(desc);
            d.style.fontSize = 14; d.style.color = new Color(1f, 1f, 1f, 0.35f);
            d.style.unityFontDefinition = GetFontDef();
            textGroup.Add(d);
        }
        row.Add(textGroup);

        var toggle = new Toggle();
        toggle.value = defaultValue;
        row.Add(toggle);
        contentPanel.Add(row);
    }

    private void AddSlider(string label, int min, int max, int defaultValue, string unit)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 10;

        var lbl = new Label(label);
        lbl.style.width = 120; lbl.style.fontSize = 20; lbl.style.color = new Color(1f, 1f, 1f, 0.85f);
        lbl.style.unityFontDefinition = GetFontDef();
        row.Add(lbl);

        var slider = new Slider("", min, max, SliderDirection.Horizontal, 1f);
        slider.value = defaultValue; slider.style.flexGrow = 1;
        row.Add(slider);

        var unitLabel = new Label(unit);
        unitLabel.style.width = 30; unitLabel.style.fontSize = 16;
        unitLabel.style.color = new Color(1f, 1f, 1f, 0.5f);
        unitLabel.style.unityFontDefinition = GetFontDef();
        row.Add(unitLabel);

        contentPanel.Add(row);
    }

    private void AddInfoRow(string label, string value)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginBottom = 6;

        var lbl = new Label(label);
        lbl.style.width = 100; lbl.style.fontSize = 20;
        lbl.style.color = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.8f);
        lbl.style.unityFontDefinition = GetFontDef();
        row.Add(lbl);

        var val = new Label(value);
        val.style.fontSize = 20; val.style.color = new Color(1f, 1f, 1f, 0.85f);
        val.style.whiteSpace = WhiteSpace.Normal;
        val.style.unityFontDefinition = GetFontDef();
        row.Add(val);

        contentPanel.Add(row);
    }

    public void Show() { SelectCategory(0); overlay.style.display = DisplayStyle.Flex; }
    public void Hide() { overlay.style.display = DisplayStyle.None; }
    public bool IsOpen => overlay.style.display == DisplayStyle.Flex;
}