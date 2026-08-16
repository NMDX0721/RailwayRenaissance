using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NewGameSetupUI : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;
    private VisualElement panel;
    private TextField aliasField;
    private TextField seedField;
    private System.Action onConfirmed;
    private int currentPage = 0;
    private VisualElement[] pages;
    private Label pageTitle;
    private Label pageSubtitle;
    private UnityEngine.UIElements.Button prevBtn;
    private UnityEngine.UIElements.Button nextBtn;
    private UnityEngine.UIElements.Button confirmBtn;

    private readonly string[] difficultyKeys = { "easy", "normal", "hard", "custom" };
    private readonly string[] difficultyLabels = { "司炉", "副司机", "司机", "指导司机" };
    private readonly string[] difficultyDescs = { "宽松开局，适合新手", "标准体验，平衡适中", "高难度，资源紧张", "自定义参数与种子" };
    private readonly Button[] difficultyButtons = new Button[4];
    private int selectedDifficulty = 1;

    private readonly string[] presetSeeds = { "雾峰标准", "资源危机", "政治风暴", "自由市场", "废土重生" };
    private readonly string[] presetSeedCodes = { "RR-7A3F-B2C9", "RR-042-D9E1", "RR-077-F5A2", "RR-113-C8D4", "RR-999-E0B7" };
    private readonly Button[] seedButtons = new Button[5];
    private int selectedSeed = -1;

    private VisualElement customParamsBox;
    private readonly Dictionary<string, Slider> paramSliders = new Dictionary<string, Slider>();

    private static readonly (string, string, float, float, float)[] ParamDefs =
    {
        ("startMoney",      "初始资金", 1f, 5f, 4f),
        ("incomeMultiplier", "收入倍率", 0.5f, 2.0f, 1.0f),
        ("costMultiplier",   "成本倍率", 0.5f, 2.0f, 1.0f),
        ("subsidyMultiplier","补贴倍率", 0.5f, 2.0f, 1.0f),
        ("sandPriceMultiplier", "沙价倍率", 0.5f, 2.0f, 1.0f),
        ("passengerMultiplier", "客运倍率", 0.5f, 2.0f, 1.0f),
        ("cargoMultiplier",  "货运倍率", 0.5f, 2.0f, 1.0f),
        ("eventFrequency",   "事件频率", 0.5f, 2.0f, 1.0f),
    };

    private static readonly Color BgColor = new Color(0.12f, 0.08f, 0.05f, 0.97f);
    private static readonly Color Gold = new Color(1f, 200f / 255f, 100f / 255f, 1f);
    private static readonly Color GoldDim = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
    private static readonly Color BtnNormal = new Color(0.2f, 0.12f, 0.08f, 0.85f);
    private static readonly Color BtnActive = new Color(0.55f, 0.32f, 0.12f, 0.95f);

    public void Init(UIDocument document, System.Action onConfirm)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        onConfirmed = onConfirm;
        BuildUI();
        ShowPage(0);
    }

    private FontDefinition Fd() => new FontDefinition { font = gameFont };

    private void BuildUI()
    {
        var root = uiDoc.rootVisualElement;
        var fontDef = Fd();

        panel = new VisualElement { name = "new-game-panel" };
        panel.style.position = Position.Absolute;
        panel.style.top = 0; panel.style.left = 0; panel.style.right = 0; panel.style.bottom = 0;
        panel.style.backgroundColor = BgColor;
        panel.style.display = DisplayStyle.None;
        root.Add(panel);

        // ── Header ──
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.paddingLeft = 60;
        header.style.paddingRight = 60;
        header.style.paddingTop = 24;
        header.style.paddingBottom = 16;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = GoldDim;
        panel.Add(header);

        var title = new Label("开始新游戏");
        title.style.fontSize = 32;
        title.style.color = Gold;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = fontDef;
        header.Add(title);

        var closeBtn = new UnityEngine.UIElements.Button(Hide) { text = "X" };
        closeBtn.style.width = 40; closeBtn.style.height = 30;
        closeBtn.style.fontSize = 20; closeBtn.style.color = new Color(1f, 1f, 1f, 0.6f);
        closeBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.5f);
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = fontDef;
        header.Add(closeBtn);

        // ── Content area ──
        var content = new VisualElement();
        content.style.flexGrow = 1;
        content.style.paddingLeft = 80;
        content.style.paddingRight = 80;
        content.style.paddingTop = 30;
        content.style.alignItems = Align.Center;
        panel.Add(content);

        // Page title
        pageTitle = new Label("");
        pageTitle.style.fontSize = 28;
        pageTitle.style.color = Gold;
        pageTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        pageTitle.style.unityFontDefinition = fontDef;
        pageTitle.style.marginBottom = 6;
        content.Add(pageTitle);

        pageSubtitle = new Label("");
        pageSubtitle.style.fontSize = 17;
        pageSubtitle.style.color = new Color(1f, 1f, 1f, 0.5f);
        pageSubtitle.style.unityFontDefinition = fontDef;
        pageSubtitle.style.marginBottom = 24;
        content.Add(pageSubtitle);

        // Pages container
        var pageBox = new VisualElement();
        pageBox.style.flexGrow = 1;
        pageBox.style.alignItems = Align.Center;
        pageBox.style.justifyContent = Justify.Center;
        content.Add(pageBox);

        // ── Page 1: 角色设定 ──
        var page1 = new VisualElement();
        page1.style.alignItems = Align.Center;
        pageBox.Add(page1);
        BuildPage1(page1, fontDef);

        // ── Page 2: 难度选择 ──
        var page2 = new VisualElement();
        page2.style.alignItems = Align.Center;
        pageBox.Add(page2);
        BuildPage2(page2, fontDef);

        // ── Page 3: 种子（仅指导司机） ──
        var page3 = new VisualElement();
        page3.style.alignItems = Align.Center;
        pageBox.Add(page3);
        BuildPage3(page3, fontDef);

        pages = new[] { page1, page2, page3 };

        // ── Footer navigation ──
        var footer = new VisualElement();
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.Center;
        footer.style.alignItems = Align.Center;
        footer.style.paddingTop = 20;
        footer.style.paddingBottom = 30;
        panel.Add(footer);

        prevBtn = new UnityEngine.UIElements.Button(OnPrev) { text = "\u25C0  上一步" };
        StyleNavBtn(prevBtn, fontDef);
        footer.Add(prevBtn);

        // Page dots
        var dots = new VisualElement();
        dots.style.flexDirection = FlexDirection.Row;
        dots.style.marginLeft = 20; dots.style.marginRight = 20;
        for (int i = 0; i < 3; i++)
        {
            var dot = new Label("\u25CF");
            dot.name = "dot-" + i;
            dot.style.fontSize = 14;
            dot.style.color = new Color(1f, 1f, 1f, 0.2f);
            dot.style.marginLeft = 6; dot.style.marginRight = 6;
            dot.style.unityFontDefinition = fontDef;
            dots.Add(dot);
        }
        footer.Add(dots);

        confirmBtn = new UnityEngine.UIElements.Button(OnConfirm) { text = "确认出发" };
        StyleNavBtn(confirmBtn, fontDef);
        confirmBtn.style.backgroundColor = new Color(0.35f, 0.2f, 0.1f, 0.95f);
        confirmBtn.style.color = new Color(1f, 0.9f, 0.6f, 1f);
        footer.Add(confirmBtn);

        nextBtn = new UnityEngine.UIElements.Button(OnNext) { text = "下一步  \u25B6" };
        StyleNavBtn(nextBtn, fontDef);
        footer.Add(nextBtn);

        // Load config
        var config = GameConfig.Load();
        aliasField.value = config.playerAlias;
        if (paramSliders.TryGetValue("startMoney", out var sm)) sm.value = config.startMoney / 10000f;
        if (paramSliders.TryGetValue("incomeMultiplier", out var im)) im.value = config.incomeMultiplier;
        int savedIdx = System.Array.IndexOf(difficultyKeys, config.difficulty);
        SelectDifficulty(savedIdx < 0 ? 1 : savedIdx);
        seedField.value = config.seedCode;
    }

    private void BuildPage1(VisualElement page, FontDefinition fontDef)
    {
        var icon = new Label("\uD83D\uDC64");
        icon.style.fontSize = 60;
        icon.style.marginBottom = 10;
        page.Add(icon);

        var aliasLabel = new Label("为林彪悍取一个「字」");
        aliasLabel.style.fontSize = 20;
        aliasLabel.style.color = new Color(1f, 1f, 1f, 0.8f);
        aliasLabel.style.unityFontDefinition = fontDef;
        aliasLabel.style.marginBottom = 6;
        page.Add(aliasLabel);

        var hint = new Label("表字，可不填。例如：子谦、明远");
        hint.style.fontSize = 15;
        hint.style.color = new Color(1f, 1f, 1f, 0.4f);
        hint.style.unityFontDefinition = fontDef;
        hint.style.marginBottom = 12;
        page.Add(hint);

        aliasField = new TextField();
        aliasField.maxLength = 12;
        aliasField.style.width = 360;
        aliasField.style.height = 50;
        aliasField.style.fontSize = 26;
        aliasField.Q<TextElement>().style.fontSize = 26;
        aliasField.Q<TextElement>().style.unityFontDefinition = fontDef;
        aliasField.Q<TextElement>().style.unityTextAlign = TextAnchor.MiddleCenter;
        page.Add(aliasField);
    }

    private void BuildPage2(VisualElement page, FontDefinition fontDef)
    {
        var diffLabel = new Label("选择难度");
        diffLabel.style.fontSize = 20;
        diffLabel.style.color = new Color(1f, 1f, 1f, 0.8f);
        diffLabel.style.unityFontDefinition = fontDef;
        diffLabel.style.marginBottom = 12;
        page.Add(diffLabel);

        var diffRow = new VisualElement();
        diffRow.style.flexDirection = FlexDirection.Row;
        diffRow.style.marginBottom = 8;
        page.Add(diffRow);

        for (int i = 0; i < difficultyKeys.Length; i++)
        {
            int idx = i;
            var btn = new Button(() => SelectDifficulty(idx)) { text = difficultyLabels[idx] };
            btn.style.width = 140; btn.style.height = 48;
            btn.style.marginRight = 8; btn.style.fontSize = 18;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.unityFontDefinition = fontDef;
            btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
            btn.style.backgroundColor = BtnNormal;
            btn.style.color = new Color(1f, 1f, 1f, 0.8f);
            diffRow.Add(btn);
            difficultyButtons[idx] = btn;
        }

        var diffDesc = new Label("");
        diffDesc.name = "diff-desc";
        diffDesc.style.fontSize = 16;
        diffDesc.style.color = new Color(1f, 1f, 1f, 0.5f);
        diffDesc.style.unityFontDefinition = fontDef;
        diffDesc.style.marginBottom = 16;
        page.Add(diffDesc);

        // Custom params box
        customParamsBox = new VisualElement();
        customParamsBox.style.display = DisplayStyle.None;
        customParamsBox.style.marginBottom = 10;
        customParamsBox.style.backgroundColor = new Color(0.1f, 0.06f, 0.04f, 0.5f);
        customParamsBox.style.borderTopLeftRadius = 6; customParamsBox.style.borderTopRightRadius = 6;
        customParamsBox.style.borderBottomLeftRadius = 6; customParamsBox.style.borderBottomRightRadius = 6;
        customParamsBox.style.paddingTop = 10; customParamsBox.style.paddingBottom = 10;
        customParamsBox.style.paddingLeft = 20; customParamsBox.style.paddingRight = 20;
        page.Add(customParamsBox);

        foreach (var (key, label, min, max, def) in ParamDefs)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;
            customParamsBox.Add(row);

            var name = new Label(label);
            name.style.width = 120; name.style.fontSize = 17;
            name.style.color = new Color(1f, 1f, 1f, 0.85f);
            name.style.unityFontDefinition = Fd();
            row.Add(name);

            var slider = new Slider(min, max);
            slider.value = def;
            slider.style.width = 220; slider.style.flexGrow = 1;
            row.Add(slider);

            var val = new Label(def.ToString("0.00"));
            val.style.width = 60; val.style.fontSize = 17;
            val.style.color = Gold; val.style.unityFontDefinition = Fd();
            row.Add(val);
            slider.RegisterValueChangedCallback(e => val.text = e.newValue.ToString("0.00"));
            paramSliders[key] = slider;
        }
    }

    private void BuildPage3(VisualElement page, FontDefinition fontDef)
    {
        var seedLabel = new Label("选择世界种子");
        seedLabel.style.fontSize = 20;
        seedLabel.style.color = new Color(1f, 1f, 1f, 0.8f);
        seedLabel.style.unityFontDefinition = fontDef;
        seedLabel.style.marginBottom = 6;
        page.Add(seedLabel);

        var seedHint = new Label("指导司机模式下，你可以选择预设世界或输入种子码");
        seedHint.style.fontSize = 15;
        seedHint.style.color = new Color(1f, 1f, 1f, 0.4f);
        seedHint.style.unityFontDefinition = fontDef;
        seedHint.style.marginBottom = 16;
        page.Add(seedHint);

        var seedRow = new VisualElement();
        seedRow.style.flexDirection = FlexDirection.Row;
        seedRow.style.flexWrap = Wrap.Wrap;
        seedRow.style.marginBottom = 12;
        page.Add(seedRow);

        for (int i = 0; i < presetSeeds.Length; i++)
        {
            int idx = i;
            var btn = new Button(() => SelectPresetSeed(idx)) { text = presetSeeds[idx] };
            btn.style.width = 120; btn.style.height = 40;
            btn.style.marginRight = 8; btn.style.marginBottom = 8;
            btn.style.fontSize = 17; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.unityFontDefinition = fontDef;
            btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
            btn.style.backgroundColor = BtnNormal;
            btn.style.color = new Color(1f, 1f, 1f, 0.8f);
            btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = GoldDim; btn.style.borderBottomColor = GoldDim;
            btn.style.borderLeftColor = GoldDim; btn.style.borderRightColor = GoldDim;
            seedRow.Add(btn);
            seedButtons[idx] = btn;
        }

        var manualRow = new VisualElement();
        manualRow.style.flexDirection = FlexDirection.Row;
        manualRow.style.alignItems = Align.Center;
        page.Add(manualRow);

        var manualLabel = new Label("种子码：");
        manualLabel.style.fontSize = 18;
        manualLabel.style.color = new Color(1f, 1f, 1f, 0.6f);
        manualLabel.style.unityFontDefinition = fontDef;
        manualLabel.style.marginRight = 8;
        manualRow.Add(manualLabel);

        seedField = new TextField();
        seedField.maxLength = 17;
        seedField.value = "RR-";
        seedField.style.width = 260; seedField.style.height = 44;
        seedField.style.fontSize = 20;
        seedField.Q<TextElement>().style.fontSize = 20;
        seedField.Q<TextElement>().style.unityFontDefinition = fontDef;
        seedField.Q<TextElement>().style.unityTextAlign = TextAnchor.MiddleCenter;
        seedField.RegisterValueChangedCallback(e => { if (selectedSeed >= 0) ClearSeedSelection(); });
        manualRow.Add(seedField);
    }

    private void ShowPage(int idx)
    {
        currentPage = idx;
        string[] titles = { "角色设定", "难度选择", "世界种子" };
        string[] subtitles = {
            "为你的角色取一个表字",
            "选择开局难度",
            "选择预设世界或输入种子码（仅指导司机模式可用）"
        };

        pageTitle.text = titles[idx];
        pageSubtitle.text = subtitles[idx];

        for (int i = 0; i < pages.Length; i++)
            pages[i].style.display = (i == idx) ? DisplayStyle.Flex : DisplayStyle.None;

        // Update dots
        for (int i = 0; i < 3; i++)
        {
            var dot = panel.Q<Label>("dot-" + i);
            if (dot != null) dot.style.color = (i == idx) ? Gold : new Color(1f, 1f, 1f, 0.2f);
        }

        prevBtn.style.display = (idx > 0) ? DisplayStyle.Flex : DisplayStyle.None;
        nextBtn.style.display = (idx < pages.Length - 1) ? DisplayStyle.Flex : DisplayStyle.None;
        confirmBtn.style.display = (idx == pages.Length - 1) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnPrev() { if (currentPage > 0) ShowPage(currentPage - 1); }
    private void OnNext() { if (currentPage < pages.Length - 1) ShowPage(currentPage + 1); }

    private void SelectPresetSeed(int idx)
    {
        selectedSeed = idx;
        seedField.value = presetSeedCodes[idx];
        for (int i = 0; i < seedButtons.Length; i++)
        {
            seedButtons[i].style.backgroundColor = (i == idx) ? BtnActive : BtnNormal;
            seedButtons[i].style.color = (i == idx) ? new Color(1f, 0.9f, 0.6f, 1f) : new Color(1f, 1f, 1f, 0.8f);
        }
    }

    private void ClearSeedSelection()
    {
        selectedSeed = -1;
        foreach (var btn in seedButtons) { btn.style.backgroundColor = BtnNormal; btn.style.color = new Color(1f, 1f, 1f, 0.8f); }
    }

    private void SelectDifficulty(int idx)
    {
        selectedDifficulty = idx;
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            difficultyButtons[i].style.backgroundColor = (i == idx) ? BtnActive : BtnNormal;
            difficultyButtons[i].style.color = (i == idx) ? new Color(1f, 0.9f, 0.6f, 1f) : new Color(1f, 1f, 1f, 0.8f);
        }
        var desc = panel.Q<Label>("diff-desc");
        if (desc != null) desc.text = difficultyDescs[idx];
        customParamsBox.style.display = (difficultyKeys[idx] == "custom") ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void StyleNavBtn(UnityEngine.UIElements.Button btn, FontDefinition fontDef)
    {
        btn.style.width = 140; btn.style.height = 44;
        btn.style.fontSize = 20; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = fontDef;
        btn.style.backgroundColor = BtnNormal;
        btn.style.color = new Color(1f, 1f, 1f, 0.8f);
        btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
        btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
        btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
        btn.style.borderTopColor = GoldDim; btn.style.borderBottomColor = GoldDim;
        btn.style.borderLeftColor = GoldDim; btn.style.borderRightColor = GoldDim;
    }

    private void OnConfirm()
    {
        var config = GameConfig.Load();
        config.playerAlias = aliasField.value.Trim();
        config.ApplyDifficultyPreset(difficultyKeys[selectedDifficulty]);

        if (difficultyKeys[selectedDifficulty] == "custom")
        {
            config.startMoney = ParamValue("startMoney") * 10000f;
            config.incomeMultiplier = ParamValue("incomeMultiplier");
            config.costMultiplier = ParamValue("costMultiplier");
            config.subsidyMultiplier = ParamValue("subsidyMultiplier");
            config.sandPriceMultiplier = ParamValue("sandPriceMultiplier");
            config.passengerMultiplier = ParamValue("passengerMultiplier");
            config.cargoMultiplier = ParamValue("cargoMultiplier");
            config.eventFrequency = ParamValue("eventFrequency");
            config.seedCode = seedField.value.Trim();
        }
        else
        {
            config.seedCode = "";
        }

        config.Save();
        Hide();
        onConfirmed?.Invoke();
    }

    private float ParamValue(string key) => paramSliders.TryGetValue(key, out var s) ? s.value : 1f;

    public void Show()
    {
        currentPage = 0;
        if (panel != null) { panel.style.display = DisplayStyle.Flex; ShowPage(0); }
    }

    public void Hide() { if (panel != null) panel.style.display = DisplayStyle.None; }
}