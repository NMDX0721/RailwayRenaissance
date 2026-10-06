using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueBox : MonoBehaviour
{
    private UIDocument uiDoc;
    private VisualElement dialogueBox;
    private VisualElement nameArea;
    private Label speakerName;
    private Label dialogueText;
    private Label continueIndicator;
    private VisualElement phoneBar;
    private Label phoneLabel;
    private VisualElement progressBar;
    /// <summary>进度条 seek 回调：参数为轨道比例 0~1（点击/拖动均触发）。</summary>
    public System.Action<float> OnProgressSeek;
    /// <summary>进度条拖动状态（true=按下拖动中）——用于抑制期间的日期地点卡。</summary>
    public System.Action<bool> OnProgressDragChanged;
    private VisualElement progressThumb;
    private TypewriterEffect typewriterEffect;
    private Font gameFont;
    private Coroutine continueAnimCoroutine;

    public Action OnTypewriterComplete;

    // —— 术语高亮交互（Phase1：黄下划线 + 单击小卡 + 进度条随卡透明）——
    private VisualElement rootVisual;                       // 根（卡片/拦截层挂点）
    private VisualElement textAreaField;                    // 文本区（下划线命中框挂点）
    private readonly System.Collections.Generic.List<VisualElement> _termHits = new();
    private VisualElement termCard;
    private VisualElement cardCatcher;
    private string _lastWrapped = "";
    private int _lastFs = 36;
    /// <summary>小卡「术语图鉴」跳转（参数 termId）；由 VNManager 接到平板 TIPS 页后才显示按钮。</summary>
    public Action<string> OnOpenTips;

    public void Init(UIDocument document)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildUI();
        typewriterEffect = gameObject.AddComponent<TypewriterEffect>();
        typewriterEffect.Initialize(dialogueText);
    }

    private FontDefinition GetFontDef()
    {
        return new FontDefinition { font = gameFont };
    }

    private void BuildUI()
    {
        var root = uiDoc.rootVisualElement;
        rootVisual = root;
        var fontDef = GetFontDef();

        dialogueBox = new VisualElement { name = "dialogue-box" };
        dialogueBox.pickingMode = PickingMode.Position;
        dialogueBox.style.position = Position.Absolute;
        dialogueBox.style.left = 0;
        dialogueBox.style.right = 0;
        dialogueBox.style.bottom = 16; // 贴近屏幕底，只留一点呼吸空间
        dialogueBox.style.height = 120; // 固定高度：边框4 + 上下padding16 + 内容区100（折行容量84居中，留白加厚）
        dialogueBox.style.flexDirection = FlexDirection.Row;
        dialogueBox.style.backgroundColor = new Color(25f / 255f, 17f / 255f, 12f / 255f, 0.92f);
        dialogueBox.style.borderTopWidth = 2;
        dialogueBox.style.borderRightWidth = 2;
        dialogueBox.style.borderBottomWidth = 2;
        dialogueBox.style.borderLeftWidth = 2;
        dialogueBox.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        dialogueBox.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        dialogueBox.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        dialogueBox.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.6f);
        // 四角统一圆弧
        dialogueBox.style.borderTopLeftRadius = 8;
        dialogueBox.style.borderTopRightRadius = 8;
        dialogueBox.style.borderBottomLeftRadius = 8;
        dialogueBox.style.borderBottomRightRadius = 8;
        dialogueBox.style.paddingLeft = 100;
        dialogueBox.style.display = DisplayStyle.None;
        root.Add(dialogueBox);

        nameArea = new VisualElement { name = "name-area" };
        nameArea.style.width = 100;
        nameArea.style.flexShrink = 0;
        nameArea.style.position = Position.Relative;
        dialogueBox.Add(nameArea);

        speakerName = new Label { name = "speaker-name" };
        speakerName.style.fontSize = 28;
        speakerName.style.color = new Color(1f, 200f / 255f, 100f / 255f, 1f);
        speakerName.style.unityFontStyleAndWeight = FontStyle.Bold;
        speakerName.style.whiteSpace = WhiteSpace.NoWrap;
        speakerName.style.unityTextAlign = TextAnchor.MiddleRight;
        speakerName.style.unityFontDefinition = fontDef;
        speakerName.style.position = Position.Absolute;
        speakerName.style.left = 0;
        speakerName.style.right = 16;
        speakerName.style.top = 0;
        speakerName.style.bottom = 0;
        nameArea.Add(speakerName);

        var textArea = new VisualElement { name = "text-area" };
        textAreaField = textArea;
        textArea.style.flexGrow = 1;
        textArea.style.paddingRight = 30;
        textArea.style.paddingTop = 8;
        textArea.style.paddingBottom = 8;
        // 默认顶部对齐（不竖向居中）：文字从上往下长，多出的空间留在底部
        textArea.style.overflow = Overflow.Hidden;
        dialogueBox.Add(textArea);

        dialogueText = new Label { name = "dialogue-text" };
        dialogueText.style.fontSize = 32;
        dialogueText.style.color = new Color(1f, 1f, 1f, 0.95f);
        dialogueText.style.whiteSpace = WhiteSpace.Normal;
        dialogueText.style.unityTextAlign = TextAnchor.UpperLeft;
        dialogueText.style.unityFontDefinition = fontDef;
        textArea.Add(dialogueText);

        continueIndicator = new Label { name = "continue-indicator", text = "\u25BC" };
        continueIndicator.style.position = Position.Absolute;
        continueIndicator.style.bottom = 16;
        continueIndicator.style.right = 24;
        continueIndicator.style.fontSize = 20;
        continueIndicator.style.color = new Color(1f, 1f, 1f, 0.5f);
        continueIndicator.style.display = DisplayStyle.None;
        continueIndicator.style.unityFontDefinition = fontDef;
        dialogueBox.Add(continueIndicator);

        // 顶部通话条：表明当前是电话场景
        phoneBar = new VisualElement { name = "phone-bar" };
        phoneBar.style.position = Position.Absolute;
        phoneBar.style.top = 18;
        phoneBar.style.left = 0;
        phoneBar.style.right = 0;
        phoneBar.style.height = 40;
        phoneBar.style.alignItems = Align.Center;
        phoneBar.style.justifyContent = Justify.Center;
        phoneBar.style.backgroundColor = new Color(0.05f, 0.08f, 0.12f, 0.78f);
        phoneBar.style.borderTopWidth = 1;
        phoneBar.style.borderBottomWidth = 1;
        phoneBar.style.borderTopColor = new Color(0.4f, 0.85f, 1f, 0.55f);
        phoneBar.style.borderBottomColor = new Color(0.4f, 0.85f, 1f, 0.55f);
        phoneBar.style.display = DisplayStyle.None;
        phoneBar.pickingMode = PickingMode.Ignore;
        root.Add(phoneBar);

        phoneLabel = new Label { name = "phone-label" };
        phoneLabel.style.fontSize = 22;
        phoneLabel.style.color = new Color(0.55f, 0.9f, 1f, 1f);
        phoneLabel.style.unityFontDefinition = fontDef;
        phoneLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        phoneBar.Add(phoneLabel);

        // 底部剧情进度条：细线轨道 + 米色滑块 + 两端箭头（贴屏幕最底，随对话框显隐）
        // 支持：在已解锁进度内拖动/点击轨道跳转（回调 VNManager 反查句索引）
        progressBar = new VisualElement { name = "progress-bar" };
        progressBar.style.position = Position.Absolute;
        progressBar.style.left = 16;
        progressBar.style.right = 16;
        progressBar.style.bottom = 0;
        progressBar.style.height = 10;
        progressBar.style.flexDirection = FlexDirection.Row;
        progressBar.style.alignItems = Align.Center;
        progressBar.pickingMode = PickingMode.Position;
        progressBar.style.display = DisplayStyle.None;
        root.Add(progressBar);

        var pPrev = new Label { text = "◀" };
        pPrev.style.fontSize = 8;
        pPrev.style.color = new Color(1f, 1f, 1f, 0.35f);
        pPrev.style.unityFontDefinition = fontDef;
        pPrev.style.marginLeft = 4;
        progressBar.Add(pPrev);

        var track = new VisualElement { name = "progress-track" };
        track.style.flexGrow = 1;
        track.style.height = 3;
        track.style.marginLeft = 6;
        track.style.marginRight = 6;
        track.style.backgroundColor = new Color(0.38f, 0.2f, 0.17f, 0.9f);
        track.style.borderTopLeftRadius = 2;
        track.style.borderTopRightRadius = 2;
        track.style.borderBottomLeftRadius = 2;
        track.style.borderBottomRightRadius = 2;
        track.style.position = Position.Relative;
        track.pickingMode = PickingMode.Position;
        progressBar.Add(track);

        // 点击/拖动 → 回调（挂在整个进度条上：track 仅 3px 高难点中；已解锁判定在 VNManager 侧）
        // OnProgressDragChanged(true/false) 抑制拖动期间的日期地点卡反复弹出
        progressBar.RegisterCallback<PointerDownEvent>(e =>
        {
            OnProgressDragChanged?.Invoke(true);
            SeekAtPointer(e.localPosition.x);
            progressBar.CapturePointer(e.pointerId);
            e.StopImmediatePropagation();
        });
        progressBar.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (progressBar.HasPointerCapture(e.pointerId)) SeekAtPointer(e.localPosition.x);
        });
        progressBar.RegisterCallback<PointerUpEvent>(e =>
        {
            if (progressBar.HasPointerCapture(e.pointerId)) progressBar.ReleasePointer(e.pointerId);
            OnProgressDragChanged?.Invoke(false);
            e.StopImmediatePropagation();
        });
        progressBar.RegisterCallback<PointerCaptureOutEvent>(e => OnProgressDragChanged?.Invoke(false));
        progressBar.RegisterCallback<ClickEvent>(e => e.StopImmediatePropagation());
        // 可交互：悬停切换手型
        progressBar.RegisterCallback<PointerEnterEvent>(e => UiCursor.ShowHand());
        progressBar.RegisterCallback<PointerLeaveEvent>(e => UiCursor.ShowArrow());

        void SeekAtPointer(float xInBar)
        {
            // 换算到轨道区域（扣掉两端箭头占位）
            float xInTrack = xInBar - track.layout.x;
            float w = track.contentRect.width;
            if (w <= 0f) return;
            OnProgressSeek?.Invoke(Mathf.Clamp01(xInTrack / w));
        }

        progressThumb = new VisualElement { name = "progress-thumb" };
        progressThumb.style.position = Position.Absolute;
        progressThumb.style.width = 16;
        progressThumb.style.height = 7;
        progressThumb.style.top = -2;
        progressThumb.style.left = new Length(0f, LengthUnit.Percent);
        progressThumb.style.translate = new Translate(new Length(-50f, LengthUnit.Percent), 0);
        progressThumb.style.backgroundColor = new Color(0.93f, 0.87f, 0.72f, 0.95f);
        progressThumb.style.borderTopLeftRadius = 4;
        progressThumb.style.borderTopRightRadius = 4;
        progressThumb.style.borderBottomLeftRadius = 4;
        progressThumb.style.borderBottomRightRadius = 4;
        track.Add(progressThumb);

        var pNext = new Label { text = "▶" };
        pNext.style.fontSize = 8;
        pNext.style.color = new Color(1f, 1f, 1f, 0.35f);
        pNext.style.unityFontDefinition = fontDef;
        pNext.style.marginRight = 4;
        progressBar.Add(pNext);
    }

    /// <summary>剧情进度（0~1），滑块按百分比定位。</summary>
    public void SetProgress(float p)
    {
        if (progressThumb == null) return;
        progressThumb.style.left = new Length(Mathf.Clamp01(p) * 100f, LengthUnit.Percent);
    }

    /// <summary>通话条：对方名为空则显示通用「通话中」。</summary>
    public void SetPhoneCall(string peer)
    {
        if (phoneBar == null || phoneLabel == null) return;
        if (string.IsNullOrEmpty(peer))
        {
            phoneLabel.text = "☎  通话中…";
            phoneBar.style.display = DisplayStyle.Flex;
        }
        else if (peer == "__off__")
        {
            phoneBar.style.display = DisplayStyle.None;
        }
        else
        {
            phoneLabel.text = "☎  通话中 · " + peer;
            phoneBar.style.display = DisplayStyle.Flex;
        }
    }

    public VisualElement RootElement => dialogueBox;

    public void ShowDialogue(string speaker, string text)
    {
        if (dialogueBox == null) return;
        dialogueBox.style.display = DisplayStyle.Flex;
        if (progressBar != null) progressBar.style.display = DisplayStyle.Flex;

        StopContinueAnimation();
        CloseTermCard();      // 换行即收卡
        ClearTermHits();

        bool isNarration = string.IsNullOrEmpty(speaker) || speaker == "n";
        nameArea.style.visibility = isNarration ? Visibility.Hidden : Visibility.Visible;

        // 内心活动：整句 [xxx] → 蓝色斜体富文本（独白不叠术语高亮：嵌套 color 会截断外层色）
        bool isThought = !string.IsNullOrEmpty(text) && text.Length > 2 &&
                         text.StartsWith("[") && text.EndsWith("]");
        string displayText = ConvertInnerThought(text);
        if (!isThought)
            displayText = TermHighlightSystem.HighlightTerms(displayText);
        // zpix 的 ——（U+2014）字形是两段短横；换 U+2500 框线横双连＝2em 整段长横、无缝
        displayText = displayText.Replace("——", "──");

        if (!isNarration)
        {
            speakerName.text = speaker;
            displayText = "\u300C" + displayText + "\u300D";
        }

        continueIndicator.style.display = DisplayStyle.None;
        if (typewriterEffect != null)
        {
            // 固定高度框：动态字号（4步进）+ 手动折行；折行后 blockH 超内容区就降档重折
            float avail = AvailableTextWidth();
            string wrapped = FitAndWrap(displayText, avail, out int fs);
            _lastWrapped = wrapped;
            _lastFs = fs;
            int rows = wrapped.Split('\n').Length;
            dialogueText.style.fontSize = fs;
            dialogueText.style.unityParagraphSpacing = LineGap;
            dialogueText.style.minHeight = rows * fs + (rows - 1) * LineGap;

            typewriterEffect.StartTypewriter(wrapped, () =>
            {
                BuildTermHits();   // 打字完成 → 铺术语下划线/命中框
                StartContinueAnimation();
                OnTypewriterComplete?.Invoke();
            });
        }
        else
        {
            dialogueText.text = displayText;
            _lastWrapped = displayText;
            _lastFs = (int)dialogueText.style.fontSize.value.value;
            BuildTermHits();
        }
    }

    /// <summary>整句以 [ ] 包裹的旁白转为蓝色斜体，标记不显示。</summary>
    private string ConvertInnerThought(string text)
    {
        if (!string.IsNullOrEmpty(text) && text.StartsWith("[") && text.EndsWith("]") && text.Length > 2)
            return "<color=#7EC8E3><i>" + text.Substring(1, text.Length - 2) + "</i></color>";
        return text;
    }

    // ================= 术语交互 Phase1：黄下划线 + 单击小卡 + 进度条随卡透明 =================

    private void ClearTermHits()
    {
        foreach (var h in _termHits) h.RemoveFromHierarchy();
        _termHits.Clear();
    }

    /// <summary>按已折行文本铺术语命中框：框底 2px 黄线＝选中态，点击弹小卡（打字完成后调用）。</summary>
    private void BuildTermHits()
    {
        ClearTermHits();
        if (textAreaField == null || string.IsNullOrEmpty(_lastWrapped)) return;
        float fs = _lastFs;

        // 逐字符行走（跳过富文本标签）：记录每个 raw 下标的行号与行内 x
        int n = _lastWrapped.Length;
        var inTag = new bool[n];
        var lineOf = new int[n];
        var xOf = new float[n];
        int line = 0;
        float x = 0f;
        for (int i = 0; i < n; i++)
        {
            char c = _lastWrapped[i];
            if (c == '<')
            {
                int close = _lastWrapped.IndexOf('>', i);
                if (close != -1)
                {
                    for (int k = i; k <= close; k++) inTag[k] = true;
                    i = close;
                    continue;
                }
            }
            if (c == '\n') { lineOf[i] = line; xOf[i] = 0f; line++; x = 0f; continue; }
            lineOf[i] = line;
            xOf[i] = x;
            x += CharWidthEm(c) * fs;
        }

        // 共享匹配器（与黄字高亮完全同判）：含 skipWhen 抑制，防「铁路基本」误中「路基」
        foreach (var (s, len, id) in TermHighlightSystem.FindOccurrences(_lastWrapped))
        {
            int e = s + len;
            // 术语可能跨行：按行切成若干段，逐段落框
            int curLine = -1;
            float min = float.MaxValue, max = float.MinValue;
            for (int r = s; r < e && r < n; r++)
            {
                if (inTag[r]) continue;
                char c = _lastWrapped[r];
                if (c == '\n') continue;
                int ln = lineOf[r];
                float x0 = xOf[r], x1 = x0 + CharWidthEm(c) * fs;
                if (ln != curLine)
                {
                    FlushTermSegment(id, curLine, min, max, fs);
                    curLine = ln; min = x0; max = x1;
                }
                else
                {
                    if (x0 < min) min = x0;
                    if (x1 > max) max = x1;
                }
            }
            FlushTermSegment(id, curLine, min, max, fs);
        }

        void FlushTermSegment(string termId, int ln, float min, float max, float fontSize)
        {
            if (ln < 0 || max <= min) return;
            var hit = new VisualElement();
            hit.style.position = Position.Absolute;
            hit.style.left = dialogueText.layout.x + min;
            hit.style.top = dialogueText.layout.y + ln * (fontSize + LineGap);
            hit.style.width = max - min;
            hit.style.height = fontSize;
            hit.style.borderBottomWidth = 2;                       // 黄下划线＝选中态
            hit.style.borderBottomColor = new Color(1f, 0.85f, 0.15f, 1f);
            hit.pickingMode = PickingMode.Position;
            hit.RegisterCallback<PointerDownEvent>(ev =>
            {
                VNManager.Instance?.SuppressNextAdvance();
                ev.StopImmediatePropagation();
                OpenTermCard(termId, hit);
            });
            hit.RegisterCallback<PointerEnterEvent>(_ => UiCursor.ShowHand());
            hit.RegisterCallback<PointerLeaveEvent>(_ => UiCursor.ShowArrow());
            textAreaField.Add(hit);
            _termHits.Add(hit);
        }
    }

    /// <summary>术语小卡（样式自设计，参考 Maitetsu 功能：标题+角色口吻解释+MORE；点外关闭）。</summary>
    private void OpenTermCard(string termId, VisualElement hit)
    {
        CloseTermCard();
        var entry = TermHighlightSystem.GetEntry(termId);
        if (entry == null || rootVisual == null) return;
        float rootW = rootVisual.layout.width > 0f ? rootVisual.layout.width : 1280f;
        var fontDef = GetFontDef();
        var gold = new Color(1f, 0.85f, 0.25f, 1f);
        var goldEdge = new Color(0.82f, 0.62f, 0.35f, 0.9f);

        // 拦截层：点卡片以外任意处收卡（并吞掉这一下，不推进对话）
        cardCatcher = new VisualElement { name = "term-card-catcher" };
        cardCatcher.style.position = Position.Absolute;
        cardCatcher.style.left = 0; cardCatcher.style.right = 0;
        cardCatcher.style.top = 0; cardCatcher.style.bottom = 0;
        cardCatcher.pickingMode = PickingMode.Position;
        cardCatcher.RegisterCallback<PointerDownEvent>(ev =>
        {
            VNManager.Instance?.SuppressNextAdvance();
            ev.StopImmediatePropagation();
            CloseTermCard();
        });
        rootVisual.Add(cardCatcher);   // 先拦（下层）
        // 进度条随卡透明（Maitetsu 行为）
        if (progressBar != null) progressBar.style.opacity = 0f;

        termCard = new VisualElement { name = "term-card" };
        termCard.style.position = Position.Absolute;
        termCard.style.width = Mathf.Min(640f, rootW - 40f);
        termCard.style.backgroundColor = new Color(0.09f, 0.06f, 0.04f, 0.97f);
        termCard.style.borderTopWidth = 2;
        termCard.style.borderBottomWidth = 2;
        termCard.style.borderLeftWidth = 2;
        termCard.style.borderRightWidth = 2;
        termCard.style.borderTopColor = goldEdge;
        termCard.style.borderBottomColor = goldEdge;
        termCard.style.borderLeftColor = goldEdge;
        termCard.style.borderRightColor = goldEdge;
        termCard.style.borderTopLeftRadius = 8;
        termCard.style.borderTopRightRadius = 8;
        termCard.style.borderBottomLeftRadius = 8;
        termCard.style.borderBottomRightRadius = 8;
        termCard.style.paddingLeft = 18;
        termCard.style.paddingRight = 18;
        termCard.style.paddingTop = 12;
        termCard.style.paddingBottom = 14;
        termCard.pickingMode = PickingMode.Position;
        termCard.RegisterCallback<PointerDownEvent>(ev =>
        {
            VNManager.Instance?.SuppressNextAdvance();
            ev.StopImmediatePropagation();
        });

        // 标题行：词条名 + 关闭
        var titleRow = new VisualElement();
        titleRow.style.flexDirection = FlexDirection.Row;
        titleRow.style.justifyContent = Justify.SpaceBetween;
        titleRow.style.alignItems = Align.Center;
        titleRow.style.marginBottom = 8;
        var title = new Label(entry.name);
        title.style.fontSize = 28;
        title.style.color = gold;
        title.style.unityFontDefinition = fontDef;
        titleRow.Add(title);
        var close = new Label("✕");
        close.style.fontSize = 22;
        close.style.color = new Color(1f, 1f, 1f, 0.7f);
        close.style.unityFontDefinition = fontDef;
        close.pickingMode = PickingMode.Position;
        close.RegisterCallback<PointerDownEvent>(ev =>
        {
            VNManager.Instance?.SuppressNextAdvance();
            ev.StopImmediatePropagation();
            CloseTermCard();
        });
        close.RegisterCallback<PointerEnterEvent>(_ => UiCursor.ShowHand());
        close.RegisterCallback<PointerLeaveEvent>(_ => UiCursor.ShowArrow());
        titleRow.Add(close);
        termCard.Add(titleRow);

        // 解释块：角色 chip + 口吻正文（文中其它术语金色引用）
        if (entry.explanations != null)
        {
            foreach (var ex in entry.explanations)
            {
                if (ex == null || string.IsNullOrEmpty(ex.text)) continue;
                if (!string.IsNullOrEmpty(ex.speaker))
                {
                    var chip = new Label(ex.speaker);
                    chip.style.alignSelf = Align.FlexStart;
                    chip.style.fontSize = 20;
                    chip.style.color = new Color(1f, 0.86f, 0.59f, 1f);
                    chip.style.backgroundColor = new Color(0.16f, 0.10f, 0.06f, 0.9f);
                    chip.style.paddingLeft = 8; chip.style.paddingRight = 8;
                    chip.style.paddingTop = 3; chip.style.paddingBottom = 3;
                    chip.style.borderTopLeftRadius = 4; chip.style.borderTopRightRadius = 4;
                    chip.style.borderBottomLeftRadius = 4; chip.style.borderBottomRightRadius = 4;
                    chip.style.marginBottom = 5;
                    chip.style.unityFontDefinition = fontDef;
                    termCard.Add(chip);
                }
                var body = new Label(TermHighlightSystem.HighlightTerms(ex.text));
                body.style.fontSize = 24;
                body.style.color = new Color(1f, 1f, 1f, 0.92f);
                body.style.whiteSpace = WhiteSpace.Normal;
                body.style.marginBottom = 10;
                body.style.unityFontDefinition = fontDef;
                termCard.Add(body);
            }
        }

        // MORE → 术语图鉴（Phase2 接线后才显示）
        var more = new Button(() =>
        {
            VNManager.Instance?.SuppressNextAdvance();
            if (OnOpenTips == null) return;
            OnOpenTips.Invoke(termId);
            CloseTermCard();
        }) { text = "术语图鉴 →" };
        more.style.alignSelf = Align.FlexEnd;
        more.style.width = 170;
        more.style.height = 44;
        more.style.marginTop = 4;
        more.style.backgroundColor = new Color(0.20f, 0.14f, 0.08f, 1f);
        more.style.borderTopWidth = 1; more.style.borderBottomWidth = 1;
        more.style.borderLeftWidth = 1; more.style.borderRightWidth = 1;
        var moreEdge = new Color(0.82f, 0.62f, 0.35f, 0.7f);
        more.style.borderTopColor = moreEdge; more.style.borderBottomColor = moreEdge;
        more.style.borderLeftColor = moreEdge; more.style.borderRightColor = moreEdge;
        more.style.borderTopLeftRadius = 6; more.style.borderTopRightRadius = 6;
        more.style.borderBottomLeftRadius = 6; more.style.borderBottomRightRadius = 6;
        more.style.color = gold;
        more.style.fontSize = 22;
        more.style.unityTextAlign = TextAnchor.MiddleCenter;
        more.style.unityFontDefinition = fontDef;
        more.style.display = OnOpenTips != null ? DisplayStyle.Flex : DisplayStyle.None;
        termCard.Add(more);

        rootVisual.Add(termCard);   // 卡在拦截层之上

        // 布局后定位：卡贴在命中词上方、水平跟随（夹在屏内），先藏后放
        termCard.style.visibility = Visibility.Hidden;
        termCard.schedule.Execute(() =>
        {
            if (termCard == null || hit == null) return;
            var rb = rootVisual.worldBound;
            var hb = hit.worldBound;
            float left = (hb.x - rb.x) - 30f;
            float maxLeft = Mathf.Max(12f, rootW - termCard.layout.width - 12f);
            termCard.style.left = Mathf.Clamp(left, 12f, maxLeft);
            float top = (dialogueBox.worldBound.y - rb.y) - termCard.layout.height - 14f;
            termCard.style.top = Mathf.Max(12f, top);
            termCard.style.visibility = Visibility.Visible;
        }).StartingIn(0);
    }

    private void CloseTermCard()
    {
        if (termCard != null) { termCard.RemoveFromHierarchy(); termCard = null; }
        if (cardCatcher != null) { cardCatcher.RemoveFromHierarchy(); cardCatcher = null; }
        if (progressBar != null) progressBar.style.opacity = 1f;   // 进度条恢复
    }

    private const float LineGap = 12f;        // 行距（unityParagraphSpacing，配合手动折行）
    private const float BoxContentZone = 100f; // 内容区（120 - 边框4 - padding16）：字号降档的容量门槛

    private float AvailableTextWidth()
    {
        float rootW = uiDoc?.rootVisualElement?.contentRect.width ?? 0f;
        if (rootW <= 0f) rootW = 1280f; // 布局未就绪时兜底
        return rootW - 234f;            // paddingLeft100 + nameArea100 + paddingRight30 + border4
    }

    /// <summary>zpix 实测字宽（em）：CJK 1.08、字母数字 0.58、空格 0.42、其余ASCII 0.33。</summary>
    private static float CharWidthEm(char c)
    {
        if (c > 0x2000) return 1.08f;
        if (char.IsLetterOrDigit(c)) return 0.58f;
        if (c == ' ') return 0.42f;
        return 0.33f;
    }

    /// <summary>字号策略：两行装得下=36不变；装不下才降档，直到三行装进内容区（下限20）。</summary>
    private string FitAndWrap(string text, float avail, out int fs)
    {
        fs = 36;
        while (true)
        {
            string wrapped = WrapText(text, fs, avail);
            int rows = wrapped.Split('\n').Length;
            float blockH = rows * fs + (rows - 1) * LineGap;
            if (blockH <= BoxContentZone || fs <= 20)
                return wrapped;
            fs -= 4;
        }
    }

    private static bool IsNoStart(char c) => "，。、；：？！）」』】》〉…—～·‰℃”’.,!?;:)]".IndexOf(c) >= 0;
    private static bool IsNoEnd(char c) => "「『（【《〈“‘([".IndexOf(c) >= 0;

    /// <summary>按可用宽度贪心折行（标签原子不可拆），含简易避头尾。</summary>
    private static string WrapText(string text, int fs, float avail)
    {
        float limit = avail - fs; // 留一行安全边距
        var sb = new StringBuilder(text.Length + 16);
        string[] paras = text.Split('\n');
        for (int p = 0; p < paras.Length; p++)
        {
            if (p > 0) sb.Append('\n');
            string para = paras[p];
            if (para.Length == 0) continue; // 空段落保留为一行
            var line = new StringBuilder();
            float lw = 0f;
            for (int i = 0; i < para.Length; i++)
            {
                char c = para[i];
                if (c == '<')
                {
                    int close = para.IndexOf('>', i);
                    if (close != -1) { line.Append(para, i, close - i + 1); i = close; continue; }
                }
                float cw = CharWidthEm(c) * fs;
                if (lw + cw > limit && line.Length > 0)
                {
                    if (IsNoStart(c))
                    {
                        // 行首禁则：标点硬挤进当前行尾（宁可超宽一点点），绝不允许换到新行行首
                        line.Append(c);
                        lw += cw;
                        continue;
                    }
                    char last = line[line.Length - 1];
                    if (IsNoEnd(last))
                    {
                        // 行尾禁则：把该字符挪到下一行
                        sb.Append(line.ToString(), 0, line.Length - 1);
                        sb.Append('\n');
                        line.Length = 0;
                        line.Append(last);
                        lw = CharWidthEm(last) * fs;
                    }
                    else
                    {
                        sb.Append(line);
                        sb.Append('\n');
                        line.Length = 0;
                        lw = 0f;
                    }
                }
                line.Append(c);
                lw += cw;
            }
            sb.Append(line);
        }
        return sb.ToString();
    }

    public void Hide()
    {
        dialogueBox.style.display = DisplayStyle.None;
        if (progressBar != null) progressBar.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        dialogueBox.style.display = DisplayStyle.Flex;
        if (progressBar != null) progressBar.style.display = DisplayStyle.Flex;
    }

    public void HideContinueIndicator()
    {
        StopContinueAnimation();
        if (continueIndicator != null)
            continueIndicator.style.display = DisplayStyle.None;
    }

    public bool IsTyping() => typewriterEffect != null && typewriterEffect.IsTyping;

    public void SkipTyping() => typewriterEffect?.SkipTyping();

    private void StartContinueAnimation()
    {
        continueIndicator.style.display = DisplayStyle.Flex;
        if (continueAnimCoroutine != null)
            StopCoroutine(continueAnimCoroutine);
        continueAnimCoroutine = StartCoroutine(AnimateContinueIndicator());
    }

    private void StopContinueAnimation()
    {
        if (continueAnimCoroutine != null)
        {
            StopCoroutine(continueAnimCoroutine);
            continueAnimCoroutine = null;
        }
    }

    private IEnumerator AnimateContinueIndicator()
    {
        float baseBottom = 16f;
        float bounceRange = 4f;
        float speed = 2.5f;
        float time = 0f;

        continueIndicator.style.opacity = 1f;

        while (true)
        {
            time += Time.deltaTime * speed;
            float sinVal = Mathf.Sin(time);

            float alpha = Mathf.Lerp(0.3f, 1f, (sinVal + 1f) * 0.5f);
            continueIndicator.style.opacity = alpha;

            float yOffset = sinVal * bounceRange;
            continueIndicator.style.bottom = baseBottom + yOffset;

            yield return null;
        }
    }

    private void OnDestroy()
    {
        StopContinueAnimation();
        typewriterEffect?.StopTyping();
    }
}
