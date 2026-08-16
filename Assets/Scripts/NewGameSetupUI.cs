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

    private readonly string[] difficultyKeys = { "easy", "normal", "hard", "custom" };
    private readonly string[] difficultyLabels = { "司炉", "副司机", "司机", "指导司机" };
    private readonly string[] difficultyDescs = { "宽松开局，适合新手", "标准体验，平衡适中", "高难度，资源紧张", "自定义参数" };
    private readonly Button[] difficultyButtons = new Button[4];
    private int selectedDifficulty = 1;

    private readonly string[] presetSeeds = { "雾峰标准", "资源危机", "政治风暴", "自由市场", "废土重生" };
    private readonly string[] presetSeedCodes = { "RR-7A3F-B2C9", "RR-042-D9E1", "RR-077-F5A2", "RR-113-C8D4", "RR-999-E0B7" };
    private readonly Button[] seedButtons = new Button[5];
    private int selectedSeed = -1;

    private VisualElement customParamsBox;
    private readonly Dictionary<string, Slider> paramSliders = new Dictionary<string, Slider>();
    private readonly Dictionary<string, Label> paramValueLabels = new Dictionary<string, Label>();

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
        var fontDef = Fd();

        panel = new VisualElement { name = "new-game-panel" };
        panel.style.position = Position.Absolute;
        panel.style.top = 0; panel.style.left = 0; panel.style.right = 0; panel.style.bottom = 0;
        panel.style.backgroundColor = new Color(0.06f, 0.04f, 0.025f, 0.98f);
        panel.pickingMode = PickingMode.Position;
        panel.style.display = DisplayStyle.None;
        root.Add(panel);

        // ScrollView for the whole content
        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.style.flexGrow = 1;
        scroll.style.paddingLeft = 160;
        scroll.style.paddingRight = 160;
        scroll.style.paddingTop = 40;
        scroll.style.paddingBottom = 40;
        scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
        panel.Add(scroll);

        var main = new VisualElement();
        main.style.alignItems = Align.Center;
        main.pickingMode = PickingMode.Ignore;
        scroll.Add(main);

        // ── Title ──
        var title = new Label("开始新游戏");
        title.style.fontSize = 44;
        title.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = fontDef;
        title.style.marginBottom = 10;
        main.Add(title);

        var subtitle = new Label("选择你的初始条件，每个世界由种子码唯一确定");
        subtitle.style.fontSize = 18;
        subtitle.style.color = new Color(1f, 1f, 1f, 0.45f);
        subtitle.style.unityFontDefinition = fontDef;
        subtitle.style.marginBottom = 30;
        main.Add(subtitle);

        // ── Section: 角色 ──
        AddSection(main, "角色设定", fontDef);

        var aliasLabel = new Label("为林彪悍取一个「字」（表字，可不填）");
        aliasLabel.style.fontSize = 18;
        aliasLabel.style.color = new Color(1f, 1f, 1f, 0.75f);
        aliasLabel.style.unityFontDefinition = fontDef;
        aliasLabel.style.marginBottom = 6;
        main.Add(aliasLabel);

        aliasField = new TextField();
        aliasField.maxLength = 12;
        aliasField.style.width = 420;
        aliasField.style.height = 42;
        aliasField.style.fontSize = 24;
        aliasField.style.marginBottom = 24;
        aliasField.Q<TextElement>().style.fontSize = 24;
        aliasField.Q<TextElement>().style.unityFontDefinition = fontDef;
        main.Add(aliasField);

        // ── Section: 世界种子 ──
        AddSection(main, "世界种子", fontDef);

        var seedDesc = new Label("选择预设世界，或手动输入种子码（格式 RR-XXXXX-YYYYY）");
        seedDesc.style.fontSize = 18;
        seedDesc.style.color = new Color(1f, 1f, 1f, 0.75f);
        seedDesc.style.unityFontDefinition = fontDef;
        seedDesc.style.marginBottom = 10;
        main.Add(seedDesc);

        // Preset seed buttons
        var seedRow = new VisualElement();
        seedRow.style.flexDirection = FlexDirection.Row;
        seedRow.style.flexWrap = Wrap.Wrap;
        seedRow.style.marginBottom = 10;
        main.Add(seedRow);

        for (int i = 0; i < presetSeeds.Length; i++)
        {
            int idx = i;
            var btn = new Button(() => SelectPresetSeed(idx)) { text = presetSeeds[idx] };
            btn.style.width = 130;
            btn.style.height = 44;
            btn.style.marginRight = 8;
            btn.style.marginBottom = 8;
            btn.style.fontSize = 18;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.unityFontDefinition = fontDef;
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6;
            btn.style.borderBottomRightRadius = 6;
            btn.style.backgroundColor = new Color(0.2f, 0.12f, 0.08f, 0.85f);
            btn.style.color = new Color(1f, 1f, 1f, 0.8f);
            btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new Color(200f/255f, 150f/255f, 80f/255f, 0.3f);
            btn.style.borderBottomColor = new Color(200f/255f, 150f/255f, 80f/255f, 0.3f);
            btn.style.borderLeftColor = new Color(200f/255f, 150f/255f, 80f/255f, 0.3f);
            btn.style.borderRightColor = new Color(200f/255f, 150f/255f, 80f/255f, 0.3f);
            seedRow.Add(btn);
            seedButtons[idx] = btn;
        }

        // Manual seed input
        var manualSeedRow = new VisualElement();
        manualSeedRow.style.flexDirection = FlexDirection.Row;
        manualSeedRow.style.alignItems = Align.Center;
        manualSeedRow.style.marginBottom = 24;
        main.Add(manualSeedRow);

        var manualLabel = new Label("或手动输入：");
        manualLabel.style.fontSize = 18;
        manualLabel.style.color = new Color(1f, 1f, 1f, 0.6f);
        manualLabel.style.unityFontDefinition = fontDef;
        manualLabel.style.marginRight = 10;
        manualSeedRow.Add(manualLabel);

        seedField = new TextField();
        seedField.maxLength = 17;
        seedField.value = "RR-";
        seedField.style.width = 260;
        seedField.style.height = 38;
        seedField.style.fontSize = 20;
        seedField.Q<TextElement>().style.fontSize = 20;
        seedField.Q<TextElement>().style.unityFontDefinition = fontDef;
        seedField.RegisterValueChangedCallback(e =>
        {
            if (selectedSeed >= 0) ClearSeedSelection();
        });
        manualSeedRow.Add(seedField);

        // ── Section: 难度 ──
        AddSection(main, "难度选择", fontDef);

        var diffRow = new VisualElement();
        diffRow.style.flexDirection = FlexDirection.Row;
        diffRow.style.marginBottom = 6;
        main.Add(diffRow);

        for (int i = 0; i < difficultyKeys.Length; i++)
        {
            int idx = i;
            var btn = new Button(() => SelectDifficulty(idx)) { text = difficultyLabels[idx] };
            btn.style.width = 150;
            btn.style.height = 50;
            btn.style.marginRight = 8;
            btn.style.fontSize = 18;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.unityFontDefinition = fontDef;
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6;
            btn.style.borderBottomRightRadius = 6;
            btn.style.backgroundColor = new Color(0.2f, 0.12f, 0.08f, 0.85f);
            btn.style.color = new Color(1f, 1f, 1f, 0.8f);
            diffRow.Add(btn);
            difficultyButtons[idx] = btn;
        }

        var diffDesc = new Label(difficultyDescs[selectedDifficulty]);
        diffDesc.name = "diff-desc";
        diffDesc.style.fontSize = 16;
        diffDesc.style.color = new Color(1f, 1f, 1f, 0.5f);
        diffDesc.style.unityFontDefinition = fontDef;
        diffDesc.style.marginBottom = 8;
        main.Add(diffDesc);

        // Custom parameters
        customParamsBox = new VisualElement();
        customParamsBox.style.display = DisplayStyle.None;
        customParamsBox.style.marginBottom = 14;
        customParamsBox.style.paddingLeft = 20;
        customParamsBox.style.paddingRight = 20;
        customParamsBox.style.backgroundColor = new Color(0.1f, 0.06f, 0.04f, 0.5f);
        customParamsBox.style.borderTopLeftRadius = 6;
        customParamsBox.style.borderTopRightRadius = 6;
        customParamsBox.style.borderBottomLeftRadius = 6;
        customParamsBox.style.borderBottomRightRadius = 6;
        customParamsBox.style.paddingTop = 10;
        customParamsBox.style.paddingBottom = 10;
        main.Add(customParamsBox);

        foreach (var (key, label, min, max, def) in ParamDefs)
        {
            AddParamRow(key, label, min, max, def);
        }

        // ── Action buttons ──
        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        btnRow.style.marginTop = 20;
        main.Add(btnRow);

        var confirmBtn = new Button(OnConfirm) { text = "确认出发" };
        StylizeActionBtn(confirmBtn, new Color(0.35f, 0.2f, 0.1f, 0.95f));
        btnRow.Add(confirmBtn);

        var backBtn = new Button(Hide) { text = "返回" };
        StylizeActionBtn(backBtn, new Color(0.16f, 0.1f, 0.07f, 0.9f));
        btnRow.Add(backBtn);

        // Load saved config
        var config = GameConfig.Load();
        aliasField.value = config.playerAlias;
        if (paramSliders.TryGetValue("startMoney", out var sm)) sm.value = config.startMoney / 10000f;
        if (paramSliders.TryGetValue("incomeMultiplier", out var im)) im.value = config.incomeMultiplier;
        if (paramSliders.TryGetValue("costMultiplier", out var cm)) cm.value = config.costMultiplier;
        if (paramSliders.TryGetValue("subsidyMultiplier", out var sb)) sb.value = config.subsidyMultiplier;
        if (paramSliders.TryGetValue("sandPriceMultiplier", out var sp)) sp.value = config.sandPriceMultiplier;
        if (paramSliders.TryGetValue("passengerMultiplier", out var pg)) pg.value = config.passengerMultiplier;
        if (paramSliders.TryGetValue("cargoMultiplier", out var cg)) cg.value = config.cargoMultiplier;
        if (paramSliders.TryGetValue("eventFrequency", out var ef)) ef.value = config.eventFrequency;
        int savedIdx = System.Array.IndexOf(difficultyKeys, config.difficulty);
        SelectDifficulty(savedIdx < 0 ? 1 : savedIdx);
    }

    private void AddSection(VisualElement parent, string title, FontDefinition fontDef)
    {
        var label = new Label(title);
        label.style.fontSize = 22;
        label.style.color = new Color(1f, 200f / 255f, 100f / 255f, 0.9f);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityFontDefinition = fontDef;
        label.style.marginBottom = 10;
        label.style.marginTop = 10;
        label.style.borderBottomWidth = 1;
        label.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.2f);
        label.style.paddingBottom = 6;
        parent.Add(label);
    }

    private void SelectPresetSeed(int idx)
    {
        if (idx < 0 || idx >= seedButtons.Length) return;
        selectedSeed = idx;
        seedField.value = presetSeedCodes[idx];
        for (int i = 0; i < seedButtons.Length; i++)
        {
            bool active = i == idx;
            seedButtons[i].style.backgroundColor = active
                ? new Color(0.55f, 0.32f, 0.12f, 0.95f)
                : new Color(0.2f, 0.12f, 0.08f, 0.85f);
            seedButtons[i].style.color = active
                ? new Color(1f, 0.9f, 0.6f, 1f)
                : new Color(1f, 1f, 1f, 0.8f);
        }
    }

    private void ClearSeedSelection()
    {
        selectedSeed = -1;
        for (int i = 0; i < seedButtons.Length; i++)
        {
            seedButtons[i].style.backgroundColor = new Color(0.2f, 0.12f, 0.08f, 0.85f);
            seedButtons[i].style.color = new Color(1f, 1f, 1f, 0.8f);
        }
    }

    private void AddParamRow(string key, string label, float min, float max, float def)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 4;
        customParamsBox.Add(row);

        var name = new Label(label);
        name.style.width = 130;
        name.style.fontSize = 17;
        name.style.color = new Color(1f, 1f, 1f, 0.85f);
        name.style.unityFontDefinition = Fd();
        row.Add(name);

        var slider = new Slider(min, max);
        slider.value = def;
        slider.style.width = 220;
        slider.style.flexGrow = 1;
        row.Add(slider);

        var val = new Label(def.ToString("0.00"));
        val.style.width = 64;
        val.style.fontSize = 17;
        val.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        val.style.unityFontDefinition = Fd();
        row.Add(val);

        slider.RegisterValueChangedCallback(e => val.text = e.newValue.ToString("0.00"));
        paramSliders[key] = slider;
        paramValueLabels[key] = val;
    }

    private void SelectDifficulty(int idx)
    {
        if (idx < 0 || idx >= difficultyButtons.Length) return;
        selectedDifficulty = idx;
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            bool active = i == idx;
            difficultyButtons[i].style.backgroundColor = active
                ? new Color(0.55f, 0.32f, 0.12f, 0.95f)
                : new Color(0.2f, 0.12f, 0.08f, 0.85f);
            difficultyButtons[i].style.color = active
                ? new Color(1f, 0.9f, 0.6f, 1f)
                : new Color(1f, 1f, 1f, 0.8f);
        }
        // Update description
        var desc = panel.Q<Label>("diff-desc");
        if (desc != null) desc.text = difficultyDescs[idx];
        customParamsBox.style.display = difficultyKeys[idx] == "custom"
            ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void StylizeActionBtn(Button btn, Color bg)
    {
        btn.style.width = 170;
        btn.style.height = 54;
        btn.style.marginRight = 16;
        btn.style.fontSize = 22;
        btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.backgroundColor = bg;
        btn.style.color = new Color(1f, 1f, 1f, 1f);
        btn.style.borderTopLeftRadius = 8;
        btn.style.borderTopRightRadius = 8;
        btn.style.borderBottomLeftRadius = 8;
        btn.style.borderBottomRightRadius = 8;
    }

    private void OnConfirm()
    {
        var config = GameConfig.Load();
        config.playerAlias = aliasField.value.Trim();
        config.ApplyDifficultyPreset(difficultyKeys[selectedDifficulty]);

        if (difficultyKeys[selectedDifficulty] == "custom")
        {
            config.startMoney = ValueOf("startMoney") * 10000f;
            config.incomeMultiplier = ValueOf("incomeMultiplier");
            config.costMultiplier = ValueOf("costMultiplier");
            config.subsidyMultiplier = ValueOf("subsidyMultiplier");
            config.sandPriceMultiplier = ValueOf("sandPriceMultiplier");
            config.passengerMultiplier = ValueOf("passengerMultiplier");
            config.cargoMultiplier = ValueOf("cargoMultiplier");
            config.eventFrequency = ValueOf("eventFrequency");
        }

        // Save seed code
        config.seedCode = seedField.value.Trim();

        config.Save();
        Hide();
        onConfirmed?.Invoke();
    }

    private float ValueOf(string key) => paramSliders.TryGetValue(key, out var s) ? s.value : 1f;

    public void Show()
    {
        if (panel != null) panel.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (panel != null) panel.style.display = DisplayStyle.None;
    }
}