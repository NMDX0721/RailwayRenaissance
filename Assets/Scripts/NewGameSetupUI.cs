using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>开始新游戏设置面板：别名输入 + 难度选择 + 自定义参数。</summary>
public class NewGameSetupUI : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;
    private VisualElement panel;
    private TextField aliasField;
    private System.Action onConfirmed;

    private readonly string[] difficultyKeys = { "easy", "normal", "hard", "custom" };
    private readonly string[] difficultyLabels = { "司炉（简单）", "副司机（普通）", "司机（困难）", "指导司机（自定义）" };
    private readonly Button[] difficultyButtons = new Button[4];
    private int selectedDifficulty = 1;

    private VisualElement customParamsBox;
    private readonly Dictionary<string, Slider> paramSliders = new Dictionary<string, Slider>();
    private readonly Dictionary<string, Label> paramValueLabels = new Dictionary<string, Label>();

    // 指导司机自定义参数：键 → (显示名, 最小, 最大, 默认)
    private static readonly (string, string, float, float, float)[] ParamDefs =
    {
        ("startMoney",      "初始资金（万沙）", 1f, 5f, 4f),
        ("incomeMultiplier", "收入倍率",       0.5f, 2.0f, 1.0f),
        ("costMultiplier",   "成本倍率",       0.5f, 2.0f, 1.0f),
        ("subsidyMultiplier","补贴倍率",       0.5f, 2.0f, 1.0f),
        ("sandPriceMultiplier", "沙子价格倍率", 0.5f, 2.0f, 1.0f),
        ("passengerMultiplier", "客运量倍率",  0.5f, 2.0f, 1.0f),
        ("cargoMultiplier",  "货运量倍率",     0.5f, 2.0f, 1.0f),
        ("eventFrequency",   "事件频率",       0.5f, 2.0f, 1.0f),
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
        panel.style.backgroundColor = new Color(0.04f, 0.02f, 0.01f, 0.96f);
        panel.pickingMode = PickingMode.Position;
        panel.style.display = DisplayStyle.None;
        root.Add(panel);

        var main = new VisualElement();
        main.style.flexGrow = 1;
        main.style.alignItems = Align.Center;
        main.style.justifyContent = Justify.Center;
        main.style.paddingLeft = 140;
        main.style.paddingRight = 140;
        main.pickingMode = PickingMode.Ignore;
        panel.Add(main);

        var title = new Label("开始新游戏");
        title.style.fontSize = 44;
        title.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = fontDef;
        title.style.marginBottom = 26;
        main.Add(title);

        // 别名输入
        var aliasLabel = new Label("为林彪悍取一个「字」（表字，可不填）");
        aliasLabel.style.fontSize = 20;
        aliasLabel.style.color = new Color(1f, 1f, 1f, 0.8f);
        aliasLabel.style.unityFontDefinition = fontDef;
        aliasLabel.style.marginBottom = 6;
        main.Add(aliasLabel);

        aliasField = new TextField();
        aliasField.maxLength = 12;
        aliasField.style.width = 420;
        aliasField.style.height = 42;
        aliasField.style.fontSize = 24;
        aliasField.style.marginBottom = 22;
        aliasField.Q<TextElement>().style.fontSize = 24;
        aliasField.Q<TextElement>().style.unityFontDefinition = fontDef;
        main.Add(aliasField);

        // 难度选择
        var diffLabel = new Label("选择难度");
        diffLabel.style.fontSize = 20;
        diffLabel.style.color = new Color(1f, 1f, 1f, 0.8f);
        diffLabel.style.unityFontDefinition = fontDef;
        diffLabel.style.marginBottom = 8;
        main.Add(diffLabel);

        var diffRow = new VisualElement();
        diffRow.style.flexDirection = FlexDirection.Row;
        diffRow.style.marginBottom = 8;
        main.Add(diffRow);

        for (int i = 0; i < difficultyKeys.Length; i++)
        {
            int idx = i;
            var btn = new Button(() => SelectDifficulty(idx)) { text = difficultyLabels[idx] };
            btn.style.width = 160;
            btn.style.height = 50;
            btn.style.marginRight = 8;
            btn.style.fontSize = 18;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.unityFontDefinition = fontDef;
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6;
            btn.style.borderBottomRightRadius = 6;
            diffRow.Add(btn);
            difficultyButtons[idx] = btn;
        }

        // 自定义参数区
        customParamsBox = new VisualElement();
        customParamsBox.style.display = DisplayStyle.None;
        customParamsBox.style.marginBottom = 14;
        main.Add(customParamsBox);

        foreach (var (key, label, min, max, def) in ParamDefs)
        {
            AddParamRow(key, label, min, max, def);
        }

        // 操作按钮
        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        main.Add(btnRow);

        var confirmBtn = new Button(OnConfirm) { text = "确认出发" };
        StylizeActionBtn(confirmBtn, new Color(0.35f, 0.2f, 0.1f, 0.95f));
        btnRow.Add(confirmBtn);

        var backBtn = new Button(Hide) { text = "返回" };
        StylizeActionBtn(backBtn, new Color(0.16f, 0.1f, 0.07f, 0.9f));
        btnRow.Add(backBtn);

        // 载入已保存配置
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

    private void AddParamRow(string key, string label, float min, float max, float def)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 4;
        customParamsBox.Add(row);

        var name = new Label(label);
        name.style.width = 150;
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

        // 若是自定义难度，覆盖为滑块值
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

        config.Save();
        Hide();
        onConfirmed?.Invoke();
    }

    private float ValueOf(string key)
    {
        return paramSliders.TryGetValue(key, out var slider) ? slider.value : 1f;
    }

    public void Show()
    {
        if (panel != null) panel.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (panel != null) panel.style.display = DisplayStyle.None;
    }
}