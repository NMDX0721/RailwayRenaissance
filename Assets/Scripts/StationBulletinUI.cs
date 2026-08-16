using UnityEngine;
using UnityEngine.UIElements;

public class StationBulletinUI : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;
    private VisualElement overlay;
    private VisualElement panel;
    private Slider bgmVolumeSlider;
    private Slider sfxVolumeSlider;
    private Toggle autoModeToggle;
    private Toggle skipReadToggle;

    public void Init(UIDocument document)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildUI();
    }

    private FontDefinition GetFontDef()
    {
        return new FontDefinition { font = gameFont };
    }

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
        overlay.RegisterCallback<ClickEvent>(e =>
        {
            if (e.target == overlay) Hide();
        });
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
        panel.style.width = 520;
        panel.style.paddingLeft = 30; panel.style.paddingRight = 30;
        panel.style.paddingTop = 20; panel.style.paddingBottom = 20;
        overlay.Add(panel);

        // Header
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 20;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
        header.style.paddingBottom = 10;

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
        panel.Add(header);

        // Content scroll
        var content = new ScrollView(ScrollViewMode.Vertical);
        content.style.flexGrow = 1;
        content.style.maxHeight = 450;

        // ── 音频设置 ──
        AddSectionTitle(content, "音频", fontDef);
        AddSliderRow(content, "BGM 音量", 0, 100, 80, out bgmVolumeSlider, fontDef);
        AddSliderRow(content, "SFX 音量", 0, 100, 100, out sfxVolumeSlider, fontDef);

        // ── 游戏设置 ──
        AddSectionTitle(content, "游戏", fontDef);
        AddToggleRow(content, "自动模式默认开启", false, out autoModeToggle, fontDef);
        AddToggleRow(content, "跳过已读文本", true, out skipReadToggle, fontDef);

        // ── 关于 ──
        AddSectionTitle(content, "关于", fontDef);
        AddInfoRow(content, "游戏版本", "v1.0.0", fontDef);
        AddInfoRow(content, "引擎版本", "Unity 6000.5.8f1", fontDef);
        AddInfoRow(content, "渲染管线", "Universal Render Pipeline", fontDef);

        panel.Add(content);

        // Footer
        var footer = new Label("© 2026 NMDX0721 — MIT License");
        footer.style.fontSize = 14;
        footer.style.color = new Color(153f / 255f, 153f / 255f, 153f / 255f, 0.5f);
        footer.style.unityTextAlign = TextAnchor.MiddleCenter;
        footer.style.unityFontDefinition = fontDef;
        footer.style.marginTop = 15;
        footer.style.paddingTop = 10;
        footer.style.borderTopWidth = 1;
        footer.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.2f);
        panel.Add(footer);
    }

    private void AddSectionTitle(ScrollView container, string text, FontDefinition fontDef)
    {
        var label = new Label(text);
        label.style.fontSize = 22;
        label.style.color = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.9f);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityFontDefinition = fontDef;
        label.style.marginTop = 12;
        label.style.marginBottom = 8;
        label.style.borderBottomWidth = 1;
        label.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.2f);
        container.Add(label);
    }

    private void AddSliderRow(ScrollView container, string label, int min, int max, int defaultValue, out Slider slider, FontDefinition fontDef)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 8;
        row.style.justifyContent = Justify.SpaceBetween;

        var lbl = new Label(label);
        lbl.style.fontSize = 20;
        lbl.style.color = new Color(1f, 1f, 1f, 0.85f);
        lbl.style.unityFontDefinition = fontDef;
        lbl.style.width = 120;
        row.Add(lbl);

        slider = new Slider("", min, max, SliderDirection.Horizontal, 1f);
        slider.value = defaultValue;
        slider.style.flexGrow = 1;
        slider.style.height = 24;
        row.Add(slider);

        container.Add(row);
    }

    private void AddToggleRow(ScrollView container, string label, bool defaultValue, out Toggle toggle, FontDefinition fontDef)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 8;
        row.style.justifyContent = Justify.SpaceBetween;

        var lbl = new Label(label);
        lbl.style.fontSize = 20;
        lbl.style.color = new Color(1f, 1f, 1f, 0.85f);
        lbl.style.unityFontDefinition = fontDef;
        lbl.style.width = 200;
        row.Add(lbl);

        toggle = new Toggle();
        toggle.value = defaultValue;
        row.Add(toggle);

        container.Add(row);
    }

    private void AddInfoRow(ScrollView container, string label, string value, FontDefinition fontDef)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginBottom = 6;

        var lbl = new Label(label);
        lbl.style.width = 120;
        lbl.style.fontSize = 20;
        lbl.style.color = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.8f);
        lbl.style.unityFontDefinition = fontDef;
        row.Add(lbl);

        var val = new Label(value);
        val.style.fontSize = 20;
        val.style.color = new Color(1f, 1f, 1f, 0.85f);
        val.style.whiteSpace = WhiteSpace.Normal;
        val.style.unityFontDefinition = fontDef;
        row.Add(val);

        container.Add(row);
    }

    public void Show() { overlay.style.display = DisplayStyle.Flex; }
    public void Hide() { overlay.style.display = DisplayStyle.None; }
    public bool IsOpen => overlay.style.display == DisplayStyle.Flex;
}