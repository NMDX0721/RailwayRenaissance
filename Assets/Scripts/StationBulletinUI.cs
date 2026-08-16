using UnityEngine;
using UnityEngine.UIElements;

public class StationBulletinUI : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;
    private VisualElement panel;
    private VisualElement overlay;

    public void Init(UIDocument document)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = uiDoc.rootVisualElement;
        var fontDef = new FontDefinition { font = gameFont };

        overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0; overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.4f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        overlay.style.display = DisplayStyle.None;
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
        panel.style.width = 600;
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

        var title = new Label("站务公告");
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

        // Content
        var content = new ScrollView(ScrollViewMode.Vertical);
        content.style.flexGrow = 1;
        content.style.maxHeight = 400;

        AddInfoLine(content, "游戏版本", "v1.0.0 (2026)");
        AddInfoLine(content, "引擎版本", "Unity 6000.5.8f1");
        AddInfoLine(content, "渲染管线", "Universal Render Pipeline (URP)");
        AddInfoLine(content, "沙能时代", "2076年 — 世界向铁路谢幕的那一年");
        AddInfoLine(content, "开发状态", "核心系统已完成，资产生成进行中");

        // 空行 + 分隔
        var sep = new Label("── 运营简报 ──");
        sep.style.fontSize = 20;
        sep.style.color = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        sep.style.unityTextAlign = TextAnchor.MiddleCenter;
        sep.style.unityFontDefinition = fontDef;
        sep.style.marginTop = 15;
        sep.style.marginBottom = 15;
        content.Add(sep);

        AddInfoLine(content, "雾峰线", "运营中 — 日均客流稳步回升");
        AddInfoLine(content, "USET 动态", "渗透率监测中，暂无异常");
        AddInfoLine(content, "车辆状态", "NF-5 耕牛号 — 例行维护正常");

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

    private void AddInfoLine(ScrollView container, string label, string value)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginBottom = 8;

        var lbl = new Label(label);
        lbl.style.width = 120;
        lbl.style.fontSize = 20;
        lbl.style.color = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.8f);
        lbl.style.unityFontDefinition = new FontDefinition { font = gameFont };
        row.Add(lbl);

        var val = new Label(value);
        val.style.fontSize = 20;
        val.style.color = new Color(1f, 1f, 1f, 0.85f);
        val.style.whiteSpace = WhiteSpace.Normal;
        val.style.unityFontDefinition = new FontDefinition { font = gameFont };
        row.Add(val);

        container.Add(row);
    }

    public void Show() { overlay.style.display = DisplayStyle.Flex; }
    public void Hide() { overlay.style.display = DisplayStyle.None; }
    public bool IsOpen => overlay.style.display == DisplayStyle.Flex;
}