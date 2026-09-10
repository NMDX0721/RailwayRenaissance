using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NewGameSetupUI : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;
    private VisualElement panel;
    private System.Action onConfirmed;
    private int currentPage = 0;
    private VisualElement[] pages;
    private Label pageTitle;
    private Label pageStep;
    private UnityEngine.UIElements.Button prevBtn;
    private UnityEngine.UIElements.Button nextBtn;
    private UnityEngine.UIElements.Button confirmBtn;

    private readonly string[] difficultyKeys = { "easy", "normal", "hard", "custom" };
    private readonly string[] difficultyLabels = { "司炉", "副司机", "司机", "指导司机" };
    private readonly string[] difficultyDescs = { "宽松开局，适合熟悉操作", "标准体验，难度适中", "资源紧张，挑战性较高", "完全自定义，自由调整参数" };
    private readonly Button[] difficultyButtons = new Button[4];
    private int selectedDifficulty = 1;

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

    private static readonly Color CGold = new Color(1f, 200f / 255f, 100f / 255f, 1f);
    private static readonly Color CGoldDim = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.3f);
    private static readonly Color CBg = new Color(0.12f, 0.08f, 0.05f, 0.97f);
    private static readonly Color CBtn = new Color(0.2f, 0.12f, 0.08f, 0.85f);
    private static readonly Color CBtnActive = new Color(0.55f, 0.32f, 0.12f, 0.95f);

    public void Init(UIDocument document, System.Action onConfirm)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        onConfirmed = onConfirm;
        BuildUI();
    }

    private FontDefinition Fd() => new FontDefinition { font = gameFont };

    private void BuildUI()
    {
        var root = uiDoc.rootVisualElement;
        var fd = Fd();

        panel = new VisualElement();
        panel.style.position = Position.Absolute;
        panel.style.top = 0; panel.style.left = 0; panel.style.right = 0; panel.style.bottom = 0;
        panel.style.backgroundColor = CBg;
        panel.style.display = DisplayStyle.None;
        // Background image
        var bgTex = Resources.Load<Texture2D>("bg/new_game_bg");
        if (bgTex != null)
            panel.style.backgroundImage = new StyleBackground(bgTex);
        root.Add(panel);

        // ── Header ──
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.paddingLeft = 60; header.style.paddingRight = 60;
        header.style.paddingTop = 20; header.style.paddingBottom = 16;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = CGoldDim;
        panel.Add(header);

        var titleGroup = new VisualElement();
        titleGroup.style.flexDirection = FlexDirection.Row;
        titleGroup.style.alignItems = Align.Center;

        pageStep = new Label("");
        pageStep.style.fontSize = 16;
        pageStep.style.color = new Color(1f, 1f, 1f, 0.4f);
        pageStep.style.unityFontDefinition = fd;
        pageStep.style.marginRight = 14;
        titleGroup.Add(pageStep);

        pageTitle = new Label("");
        pageTitle.style.fontSize = 28;
        pageTitle.style.color = CGold;
        pageTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        pageTitle.style.unityFontDefinition = fd;
        titleGroup.Add(pageTitle);
        header.Add(titleGroup);

        var closeBtn = new UnityEngine.UIElements.Button(Hide) { text = "✕" };
        closeBtn.style.width = 36; closeBtn.style.height = 30;
        closeBtn.style.fontSize = 18; closeBtn.style.color = new Color(1f, 1f, 1f, 0.5f);
        closeBtn.style.backgroundColor = new Color(0.3f, 0.15f, 0.1f, 0.4f);
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = fd;
        closeBtn.style.borderTopLeftRadius = 4; closeBtn.style.borderTopRightRadius = 4;
        closeBtn.style.borderBottomLeftRadius = 4; closeBtn.style.borderBottomRightRadius = 4;
        header.Add(closeBtn);

        // ── Content area ──
        var content = new VisualElement();
        content.style.flexGrow = 1;
        content.style.alignItems = Align.Center;
        content.style.justifyContent = Justify.Center;
        content.style.paddingLeft = 100;
        content.style.paddingRight = 100;
        panel.Add(content);

        var pageBox = new VisualElement();
        pageBox.style.width = new Length(100, LengthUnit.Percent);
        pageBox.style.maxWidth = 640;
        pageBox.style.alignItems = Align.Center;
        content.Add(pageBox);

        // ── Page 1: 角色与难度 ──
        var page1 = new VisualElement();
        page1.style.width = new Length(100, LengthUnit.Percent);
        pageBox.Add(page1);
        BuildPage1(page1, fd);

        // ── Page 2: 自定义参数 ──
        var page2 = new VisualElement();
        page2.style.width = new Length(100, LengthUnit.Percent);
        pageBox.Add(page2);
        BuildPage2(page2, fd);

        pages = new[] { page1, page2 };

        // ── Footer ──
        var footer = new VisualElement();
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.Center;
        footer.style.alignItems = Align.Center;
        footer.style.paddingTop = 24;
        footer.style.paddingBottom = 30;
        panel.Add(footer);

        prevBtn = MkBtn("◀  上一步", OnPrev, 130, fd);
        footer.Add(prevBtn);

        // Page indicator
        for (int i = 0; i < 2; i++)
        {
            var dot = new Label("●");
            dot.name = "dot-" + i;
            dot.style.fontSize = 12;
            dot.style.color = new Color(1f, 1f, 1f, 0.15f);
            dot.style.marginLeft = 6; dot.style.marginRight = 6;
            dot.style.unityFontDefinition = fd;
            footer.Add(dot);
        }

        confirmBtn = MkBtn("确认出发", OnConfirm, 140, fd);
        confirmBtn.style.backgroundColor = new Color(0.4f, 0.22f, 0.1f, 0.95f);
        confirmBtn.style.color = new Color(1f, 0.9f, 0.6f, 1f);
        footer.Add(confirmBtn);

        nextBtn = MkBtn("下一步  ▶", OnNext, 130, fd);
        footer.Add(nextBtn);

        // Load saved config
        var config = GameConfig.Load();
        int savedIdx = System.Array.IndexOf(difficultyKeys, config.difficulty);
        SelectDifficulty(savedIdx < 0 ? 1 : savedIdx);
        if (paramSliders.TryGetValue("startMoney", out var sm)) sm.value = config.startMoney / 10000f;
        foreach (var (key, _, _, _, def) in ParamDefs)
            if (paramSliders.TryGetValue(key, out var s)) s.value = def;
        ShowPage(0);
    }

    private UnityEngine.UIElements.Button MkBtn(string text, System.Action action, int width, FontDefinition fd)
    {
        var btn = new UnityEngine.UIElements.Button(action) { text = text };
        btn.style.width = width; btn.style.height = 42;
        btn.style.fontSize = 18; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = fd;
        btn.style.backgroundColor = CBtn;
        btn.style.color = new Color(1f, 1f, 1f, 0.8f);
        btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
        btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
        btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
        btn.style.borderTopColor = CGoldDim; btn.style.borderBottomColor = CGoldDim;
        btn.style.borderLeftColor = CGoldDim; btn.style.borderRightColor = CGoldDim;
        return btn;
    }

    private void BuildPage1(VisualElement page, FontDefinition fd)
    {
        // Title
        var t = new Label("角色与难度");
        t.style.fontSize = 26; t.style.color = CGold;
        t.style.unityFontStyleAndWeight = FontStyle.Bold;
        t.style.unityFontDefinition = fd; t.style.marginBottom = 4;
        page.Add(t);

        var st = new Label("设置你的角色和开局难度");
        st.style.fontSize = 16; st.style.color = new Color(1f, 1f, 1f, 0.4f);
        st.style.unityFontDefinition = fd; st.style.marginBottom = 28;
        page.Add(st);

        // ── Difficulty ──
        var diffGroup = new VisualElement();
        diffGroup.style.backgroundColor = new Color(0.1f, 0.06f, 0.04f, 0.5f);
        diffGroup.style.borderTopLeftRadius = 8; diffGroup.style.borderTopRightRadius = 8;
        diffGroup.style.borderBottomLeftRadius = 8; diffGroup.style.borderBottomRightRadius = 8;
        diffGroup.style.paddingLeft = 24; diffGroup.style.paddingRight = 24;
        diffGroup.style.paddingTop = 20; diffGroup.style.paddingBottom = 20;
        diffGroup.style.width = new Length(100, LengthUnit.Percent);
        page.Add(diffGroup);

        var diffTitle = new Label("难度选择");
        diffTitle.style.fontSize = 20; diffTitle.style.color = new Color(1f, 1f, 1f, 0.85f);
        diffTitle.style.unityFontDefinition = fd; diffTitle.style.marginBottom = 12;
        diffGroup.Add(diffTitle);

        var diffRow = new VisualElement();
        diffRow.style.flexDirection = FlexDirection.Row;
        diffRow.style.marginBottom = 10;
        diffGroup.Add(diffRow);

        for (int i = 0; i < difficultyKeys.Length; i++)
        {
            int idx = i;
            var btn = new Button(() => SelectDifficulty(idx)) { text = difficultyLabels[idx] };
            btn.style.flexGrow = 1; btn.style.height = 46;
            btn.style.marginRight = (i < 3) ? 8 : 0;
            btn.style.fontSize = 18; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.unityFontDefinition = fd;
            btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
            btn.style.backgroundColor = CBtn;
            btn.style.color = new Color(1f, 1f, 1f, 0.8f);
            diffRow.Add(btn);
            difficultyButtons[idx] = btn;
        }

        var diffDesc = new Label("");
        diffDesc.name = "diff-desc";
        diffDesc.style.fontSize = 15; diffDesc.style.color = new Color(1f, 1f, 1f, 0.45f);
        diffDesc.style.unityFontDefinition = fd;
        diffGroup.Add(diffDesc);
    }

    private void BuildPage2(VisualElement page, FontDefinition fd)
    {
        var t = new Label("自定义参数");
        t.style.fontSize = 26; t.style.color = CGold;
        t.style.unityFontStyleAndWeight = FontStyle.Bold;
        t.style.unityFontDefinition = fd; t.style.marginBottom = 4;
        page.Add(t);

        var st = new Label("仅「指导司机」难度可调整，其他难度使用默认参数");
        st.name = "custom-hint";
        st.style.fontSize = 16; st.style.color = new Color(1f, 1f, 1f, 0.4f);
        st.style.unityFontDefinition = fd; st.style.marginBottom = 24;
        page.Add(st);

        customParamsBox = new VisualElement();
        customParamsBox.style.backgroundColor = new Color(0.1f, 0.06f, 0.04f, 0.5f);
        customParamsBox.style.borderTopLeftRadius = 8; customParamsBox.style.borderTopRightRadius = 8;
        customParamsBox.style.borderBottomLeftRadius = 8; customParamsBox.style.borderBottomRightRadius = 8;
        customParamsBox.style.paddingLeft = 24; customParamsBox.style.paddingRight = 24;
        customParamsBox.style.paddingTop = 16; customParamsBox.style.paddingBottom = 16;
        customParamsBox.style.width = new Length(100, LengthUnit.Percent);
        page.Add(customParamsBox);

        foreach (var (key, label, min, max, def) in ParamDefs)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6;
            customParamsBox.Add(row);

            var name = new Label(label);
            name.style.width = 100; name.style.fontSize = 16;
            name.style.color = new Color(1f, 1f, 1f, 0.8f);
            name.style.unityFontDefinition = fd;
            row.Add(name);

            var slider = new Slider(min, max, SliderDirection.Horizontal, 0.1f);
            slider.value = def;
            slider.style.flexGrow = 1; slider.style.marginRight = 8;
            row.Add(slider);

            var val = new Label(def.ToString("0.0"));
            val.style.width = 40; val.style.fontSize = 16;
            val.style.color = CGold; val.style.unityFontDefinition = fd;
            val.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(val);
            slider.RegisterValueChangedCallback(e => val.text = e.newValue.ToString("0.0"));
            paramSliders[key] = slider;
        }
    }

    private void ShowPage(int idx)
    {
        currentPage = idx;
        string[] steps = { "第一步", "第二步" };
        string[] titles = { "难度选择", "自定义参数" };
        pageStep.text = steps[idx];
        pageTitle.text = titles[idx];

        // Hide/show pages
        for (int i = 0; i < pages.Length; i++)
            pages[i].style.display = (i == idx) ? DisplayStyle.Flex : DisplayStyle.None;

        // Update dots
        for (int i = 0; i < 2; i++)
        {
            var dot = panel.Q<Label>("dot-" + i);
            if (dot != null) dot.style.color = (i == idx) ? CGold : new Color(1f, 1f, 1f, 0.15f);
        }

        // Show/hide navigation
        bool isCustom = difficultyKeys[selectedDifficulty] == "custom";
        prevBtn.style.display = (idx > 0) ? DisplayStyle.Flex : DisplayStyle.None;
        nextBtn.style.display = (idx < pages.Length - 1) ? DisplayStyle.Flex : DisplayStyle.None;
        confirmBtn.style.display = (idx == pages.Length - 1) ? DisplayStyle.Flex : DisplayStyle.None;

        // Page 2: only editable in custom mode
        if (idx == 1)
        {
            bool editable = isCustom;
            customParamsBox.SetEnabled(editable);
            var hint = panel.Q<Label>("custom-hint");
            if (hint != null) hint.text = editable ? "调整各项参数数值" : "当前难度不支持自定义参数，请选择「指导司机」";
        }

    }

    private void OnPrev() { if (currentPage > 0) ShowPage(currentPage - 1); }
    private void OnNext()
    {
        int next = currentPage + 1;
        // 非指导司机难度跳过自定义参数页
        if (next == 1 && difficultyKeys[selectedDifficulty] != "custom")
            next = pages.Length - 1;
        if (next < pages.Length) ShowPage(next);
    }

    private void SelectDifficulty(int idx)
    {
        selectedDifficulty = idx;
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            difficultyButtons[i].style.backgroundColor = (i == idx) ? CBtnActive : CBtn;
            difficultyButtons[i].style.color = (i == idx) ? new Color(1f, 0.9f, 0.6f, 1f) : new Color(1f, 1f, 1f, 0.8f);
            // 高亮强化：选中项加金色描边（防止被 hover 效果淹没）
            bool active = (i == idx);
            float bw = active ? 2f : 0f;
            difficultyButtons[i].style.borderTopWidth = bw; difficultyButtons[i].style.borderBottomWidth = bw;
            difficultyButtons[i].style.borderLeftWidth = bw; difficultyButtons[i].style.borderRightWidth = bw;
            difficultyButtons[i].style.borderTopColor = new Color(1f, 0.85f, 0.5f, active ? 0.9f : 0f);
            difficultyButtons[i].style.borderBottomColor = new Color(1f, 0.85f, 0.5f, active ? 0.9f : 0f);
            difficultyButtons[i].style.borderLeftColor = new Color(1f, 0.85f, 0.5f, active ? 0.9f : 0f);
            difficultyButtons[i].style.borderRightColor = new Color(1f, 0.85f, 0.5f, active ? 0.9f : 0f);
        }
        var desc = panel.Q<Label>("diff-desc");
        if (desc != null) desc.text = difficultyDescs[idx];
        // 切换难度后立即刷新后续页的可编辑状态（若当前已在后续页）
        if (currentPage == 1) ShowPage(currentPage);
    }

    private static readonly string[] PresetSeeds = { "RR-7A3F-B2C9", "RR-042-D9E1", "RR-077-F5A2", "RR-113-C8D4", "RR-999-E0B7" };

    private void OnConfirm()
    {
        var config = GameConfig.Load();
        config.difficulty = difficultyKeys[selectedDifficulty];

        bool isCustom = difficultyKeys[selectedDifficulty] == "custom";
        if (isCustom)
        {
            config.startMoney = ParamValue("startMoney") * 10000f;
            config.incomeMultiplier = ParamValue("incomeMultiplier");
            config.costMultiplier = ParamValue("costMultiplier");
            config.subsidyMultiplier = ParamValue("subsidyMultiplier");
            config.sandPriceMultiplier = ParamValue("sandPriceMultiplier");
            config.passengerMultiplier = ParamValue("passengerMultiplier");
            config.cargoMultiplier = ParamValue("cargoMultiplier");
            config.eventFrequency = ParamValue("eventFrequency");
        }
        else
        {
            config.ApplyDifficultyPreset(difficultyKeys[selectedDifficulty]);
        }
        // 单档：每次新游戏随机种子
        config.seedCode = PresetSeeds[UnityEngine.Random.Range(0, PresetSeeds.Length)];

        config.Save();
        Hide();
        onConfirmed?.Invoke();
    }

    private float ParamValue(string key) => paramSliders.TryGetValue(key, out var s) ? s.value : 1f;

    public void Show()
    {
        if (panel != null) { panel.style.display = DisplayStyle.Flex; ShowPage(0); }
    }

    public void Hide() { if (panel != null) panel.style.display = DisplayStyle.None; }
}