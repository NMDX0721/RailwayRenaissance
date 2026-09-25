using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 序章 Day0 前跳过闸门：10 题单选，≥7 通过才允许跳到 prologue_02_day0。
/// 题库见 参考资料/系统/平板系统设计.md §8.1。暖金/深棕本作配色。
/// </summary>
public class VNPrologueQuiz : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;

    private VisualElement panel;
    private Label progressLabel;
    private Label questionLabel;
    private VisualElement optionsBox;
    private Label resultLabel;
    private UnityEngine.UIElements.Button retryBtn;
    private UnityEngine.UIElements.Button quitBtn;
    private UnityEngine.UIElements.Button nextBtn;

    private Action<bool> onResult;
    private int index;
    private int correctCount;
    private int picked = -1;
    private bool finished;

    private struct QuizItem
    {
        public string q;
        public string[] opts; // 4 项，opts[0] 为正确答案（展示时打乱顺序固定表）
        public int correct;   // 正确项在 opts 中的下标（已按展示顺序）
    }

    // 展示顺序已手工打乱，correct 为展示下标
    private static readonly QuizItem[] Items =
    {
        new QuizItem { q = "铁路运输的核心优势是？", opts = new[] { "运量大", "速度最快", "造价最低", "完全不受天气影响" }, correct = 0 },
        new QuizItem { q = "沙能载具的最大弱点是？", opts = new[] { "噪音太大", "夜间效率低", "无法载人", "只能在水上行驶" }, correct = 1 },
        new QuizItem { q = "雾峰村线全长多少？", opts = new[] { "12公里", "23公里", "50公里", "100公里" }, correct = 1 },
        new QuizItem { q = "「铁龙计划」的本质是？", opts = new[] { "修建新高铁", "收购铁路", "出口沙能车", "裁撤客运线" }, correct = 1 },
        new QuizItem { q = "岁月是什么？", opts = new[] { "一款平板App", "0721搭载的AI", "铁路调度局代号", "嘉颖徐的化名" }, correct = 1 },
        new QuizItem { q = "0721号是什么？", opts = new[] { "沙能飞行器", "内燃机车头", "货运港口", "检查站编号" }, correct = 0 },
        new QuizItem { q = "沙能的能源来源是？", opts = new[] { "煤炭", "核燃料", "沙子", "氢能" }, correct = 2 },
        new QuizItem { q = "雾峰村位于哪里？", opts = new[] { "中国中部山区", "朝鲜半岛沿海", "西北戈壁", "东北平原" }, correct = 0 },
        new QuizItem { q = "爷爷林悍的死因？", opts = new[] { "病逝", "沙能飞行器事故", "铁路事故", "下落不明" }, correct = 1 },
        new QuizItem { q = "嘉颖徐的身份是？", opts = new[] { "铁路大亨", "大学导师", "边检军官", "便当店老板" }, correct = 0 },
    };

    private const int PassScore = 7;

    private static readonly Color Gold = new Color(1f, 0.78f, 0.4f, 1f);
    private static readonly Color GoldDim = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.55f);
    private static readonly Color PanelBg = new Color(0.12f, 0.08f, 0.05f, 0.97f);
    private static readonly Color OptBg = new Color(0.2f, 0.14f, 0.08f, 0.9f);
    private static readonly Color OptHover = new Color(0.32f, 0.2f, 0.1f, 0.95f);

    public bool IsOpen => panel != null && panel.style.display == DisplayStyle.Flex;

    public void Init(UIDocument document)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildUI();
    }

    private FontDefinition Fd() => new FontDefinition { font = gameFont };

    private void BuildUI()
    {
        var root = uiDoc.rootVisualElement;
        var fd = Fd();

        panel = new VisualElement { name = "prologue-quiz" };
        panel.style.position = Position.Absolute;
        panel.style.top = 0; panel.style.left = 0; panel.style.right = 0; panel.style.bottom = 0;
        panel.style.backgroundColor = new Color(0, 0, 0, 0.72f);
        panel.style.display = DisplayStyle.None;
        panel.style.alignItems = Align.Center;
        panel.style.justifyContent = Justify.Center;
        panel.pickingMode = PickingMode.Position;
        // 答题中不冒泡到 VN root
        panel.RegisterCallback<PointerDownEvent>(e => { e.StopImmediatePropagation(); VNManager.Instance?.SuppressNextAdvance(); });
        panel.RegisterCallback<ClickEvent>(e => { e.StopImmediatePropagation(); VNManager.Instance?.SuppressNextAdvance(); });
        root.Add(panel);

        var box = new VisualElement();
        box.style.width = 640;
        box.style.maxHeight = new Length(85, LengthUnit.Percent);
        box.style.paddingTop = 28; box.style.paddingBottom = 24;
        box.style.paddingLeft = 36; box.style.paddingRight = 36;
        box.style.backgroundColor = PanelBg;
        box.style.borderTopWidth = 2; box.style.borderBottomWidth = 2;
        box.style.borderLeftWidth = 2; box.style.borderRightWidth = 2;
        box.style.borderTopColor = GoldDim; box.style.borderBottomColor = GoldDim;
        box.style.borderLeftColor = GoldDim; box.style.borderRightColor = GoldDim;
        box.style.borderTopLeftRadius = 16; box.style.borderTopRightRadius = 16;
        box.style.borderBottomLeftRadius = 16; box.style.borderBottomRightRadius = 16;
        panel.Add(box);

        var title = new Label("序章跳过 · 知识确认");
        title.style.fontSize = 24;
        title.style.color = Gold;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = fd;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.marginBottom = 4;
        box.Add(title);

        var line = new VisualElement();
        line.style.width = 80; line.style.height = 2;
        line.style.backgroundColor = new Color(1f, 0.78f, 0.4f, 0.45f);
        line.style.marginBottom = 14;
        line.style.alignSelf = Align.Center;
        box.Add(line);

        progressLabel = new Label();
        progressLabel.style.fontSize = 15;
        progressLabel.style.color = new Color(1f, 1f, 1f, 0.45f);
        progressLabel.style.unityFontDefinition = fd;
        progressLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        progressLabel.style.marginBottom = 12;
        box.Add(progressLabel);

        questionLabel = new Label();
        questionLabel.style.fontSize = 20;
        questionLabel.style.color = new Color(1f, 0.95f, 0.88f, 1f);
        questionLabel.style.unityFontDefinition = fd;
        questionLabel.style.whiteSpace = WhiteSpace.Normal;
        questionLabel.style.marginBottom = 16;
        box.Add(questionLabel);

        optionsBox = new VisualElement();
        optionsBox.style.marginBottom = 14;
        box.Add(optionsBox);

        resultLabel = new Label();
        resultLabel.style.fontSize = 18;
        resultLabel.style.color = Gold;
        resultLabel.style.unityFontDefinition = fd;
        resultLabel.style.whiteSpace = WhiteSpace.Normal;
        resultLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        resultLabel.style.display = DisplayStyle.None;
        resultLabel.style.marginBottom = 16;
        box.Add(resultLabel);

        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        btnRow.style.justifyContent = Justify.Center;
        box.Add(btnRow);

        quitBtn = MakeBtn("体验序章", fd, OnQuitOrConfirm);
        quitBtn.style.marginRight = 16;
        btnRow.Add(quitBtn);

        retryBtn = MakeBtn("重新答题", fd, Restart);
        retryBtn.style.display = DisplayStyle.None;
        btnRow.Add(retryBtn);

        nextBtn = MakeBtn("下一题", fd, OnNext);
        btnRow.Add(nextBtn);
    }

    private UnityEngine.UIElements.Button MakeBtn(string text, FontDefinition fd, Action onClick)
    {
        var b = new UnityEngine.UIElements.Button(() => onClick()) { text = text };
        b.focusable = false;
        b.style.width = 140; b.style.height = 44;
        b.style.fontSize = 18;
        b.style.color = new Color(0.12f, 0.08f, 0.04f, 1f);
        b.style.backgroundColor = Gold;
        b.style.unityTextAlign = TextAnchor.MiddleCenter;
        b.style.unityFontDefinition = fd;
        b.style.borderTopLeftRadius = 8; b.style.borderTopRightRadius = 8;
        b.style.borderBottomLeftRadius = 8; b.style.borderBottomRightRadius = 8;
        b.RegisterCallback<PointerDownEvent>(e => { VNManager.Instance?.SuppressNextAdvance(); e.StopImmediatePropagation(); });
        b.RegisterCallback<ClickEvent>(e => { VNManager.Instance?.SuppressNextAdvance(); e.StopImmediatePropagation(); });
        return b;
    }

    public void Show(Action<bool> result)
    {
        onResult = result;
        Restart();
        panel.style.display = DisplayStyle.Flex;
    }

    private void Restart()
    {
        index = 0;
        correctCount = 0;
        picked = -1;
        finished = false;
        resultLabel.style.display = DisplayStyle.None;
        retryBtn.style.display = DisplayStyle.None;
        nextBtn.style.display = DisplayStyle.Flex;
        nextBtn.text = "下一题";
        quitBtn.text = "体验序章";
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        var item = Items[index];
        progressLabel.text = $"第 {index + 1} / {Items.Length} 题　·　答对 {correctCount}　·　通过线 {PassScore}";
        questionLabel.text = item.q;
        optionsBox.Clear();
        picked = -1;

        for (int i = 0; i < item.opts.Length; i++)
        {
            int capture = i;
            var opt = item.opts[i];
            var btn = new UnityEngine.UIElements.Button(() => Pick(capture)) { text = opt };
            btn.focusable = false;
            btn.style.width = new Length(100, LengthUnit.Percent);
            btn.style.height = 42;
            btn.style.marginBottom = 8;
            btn.style.fontSize = 17;
            btn.style.color = new Color(1f, 1f, 1f, 0.9f);
            btn.style.backgroundColor = OptBg;
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            btn.style.paddingLeft = 16;
            btn.style.unityFontDefinition = Fd();
            btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
            btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = GoldDim; btn.style.borderBottomColor = GoldDim;
            btn.style.borderLeftColor = GoldDim; btn.style.borderRightColor = GoldDim;
            btn.RegisterCallback<PointerDownEvent>(e => { VNManager.Instance?.SuppressNextAdvance(); e.StopImmediatePropagation(); });
            btn.RegisterCallback<ClickEvent>(e => { VNManager.Instance?.SuppressNextAdvance(); e.StopImmediatePropagation(); });
            btn.RegisterCallback<PointerEnterEvent>(e => btn.style.backgroundColor = OptHover);
            btn.RegisterCallback<PointerLeaveEvent>(e => btn.style.backgroundColor = OptBg);
            optionsBox.Add(btn);
        }

        nextBtn.style.display = DisplayStyle.None;
        quitBtn.style.display = DisplayStyle.Flex;
    }

    private void Pick(int i)
    {
        if (finished || picked >= 0) return;
        picked = i;
        // 选中即标色，锁定本题
        for (int c = 0; c < optionsBox.childCount; c++)
        {
            var child = optionsBox[c] as UnityEngine.UIElements.Button;
            if (child == null) continue;
            if (c == Items[index].correct)
            {
                child.style.backgroundColor = new Color(0.35f, 0.55f, 0.28f, 0.95f);
                child.style.color = Color.white;
            }
            else if (c == picked)
            {
                child.style.backgroundColor = new Color(0.55f, 0.22f, 0.15f, 0.95f);
                child.style.color = Color.white;
            }
        }
        if (picked == Items[index].correct) correctCount++;
        nextBtn.style.display = DisplayStyle.Flex;
        nextBtn.text = index == Items.Length - 1 ? "查看结果" : "下一题";
        quitBtn.style.display = DisplayStyle.None;
    }

    private void OnNext()
    {
        if (picked < 0) return;
        index++;
        if (index < Items.Length)
        {
            ShowQuestion();
            return;
        }
        ShowResult();
    }

    private void ShowResult()
    {
        finished = true;
        optionsBox.Clear();
        bool pass = correctCount >= PassScore;
        progressLabel.text = $"得分 {correctCount} / {Items.Length}";
        questionLabel.text = pass ? "通过！可以跳过 Day0 之前的内容。" : "未通过";
        resultLabel.style.display = DisplayStyle.Flex;
        resultLabel.style.color = pass ? Gold : new Color(1f, 0.55f, 0.4f, 1f);
        resultLabel.text = pass
            ? "即将从 Day0（获得0721）开始。"
            : "正确率不足70%，建议体验完整序章以了解故事背景。";

        nextBtn.style.display = DisplayStyle.None;
        retryBtn.style.display = pass ? DisplayStyle.None : DisplayStyle.Flex;
        quitBtn.style.display = DisplayStyle.Flex;
        quitBtn.text = pass ? "确认跳过" : "体验序章";
        quitBtn.style.backgroundColor = pass ? Gold : OptBg;
        quitBtn.style.color = pass ? new Color(0.12f, 0.08f, 0.04f, 1f) : new Color(1f, 0.85f, 0.6f, 0.9f);
        pendingPass = pass;
    }

    private bool pendingPass;

    private void Close(bool pass)
    {
        panel.style.display = DisplayStyle.None;
        var cb = onResult;
        onResult = null;
        cb?.Invoke(pass);
    }

    private void OnQuitOrConfirm()
    {
        if (finished)
            Close(pendingPass);
        else
            Close(false);
    }

    /// <summary>ESC 等外部取消：未出结果一律按未通过关闭。</summary>
    public void CancelFromOutside()
    {
        if (!IsOpen) return;
        Close(false);
    }
}
