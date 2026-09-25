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
    private VisualElement progressThumb;
    private TypewriterEffect typewriterEffect;
    private Font gameFont;
    private Coroutine continueAnimCoroutine;

    public Action OnTypewriterComplete;

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
        textArea.style.flexGrow = 1;
        textArea.style.paddingRight = 30;
        textArea.style.paddingTop = 8;
        textArea.style.paddingBottom = 8;
        textArea.style.justifyContent = Justify.Center;
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
        progressBar = new VisualElement { name = "progress-bar" };
        progressBar.style.position = Position.Absolute;
        progressBar.style.left = 16;
        progressBar.style.right = 16;
        progressBar.style.bottom = 0;
        progressBar.style.height = 10;
        progressBar.style.flexDirection = FlexDirection.Row;
        progressBar.style.alignItems = Align.Center;
        progressBar.pickingMode = PickingMode.Ignore;
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
        progressBar.Add(track);

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

        bool isNarration = string.IsNullOrEmpty(speaker) || speaker == "n";
        nameArea.style.visibility = isNarration ? Visibility.Hidden : Visibility.Visible;

        // 内心活动：整句 [xxx] → 蓝色斜体富文本
        string displayText = ConvertInnerThought(text);

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
            int rows = wrapped.Split('\n').Length;
            dialogueText.style.fontSize = fs;
            dialogueText.style.unityParagraphSpacing = LineGap;
            dialogueText.style.minHeight = rows * fs + (rows - 1) * LineGap;

            typewriterEffect.StartTypewriter(wrapped, () =>
            {
                StartContinueAnimation();
                OnTypewriterComplete?.Invoke();
            });
        }
        else
        {
            dialogueText.text = displayText;
        }
    }

    /// <summary>整句以 [ ] 包裹的旁白转为蓝色斜体，标记不显示。</summary>
    private string ConvertInnerThought(string text)
    {
        if (!string.IsNullOrEmpty(text) && text.StartsWith("[") && text.EndsWith("]") && text.Length > 2)
            return "<color=#7EC8E3><i>" + text.Substring(1, text.Length - 2) + "</i></color>";
        return text;
    }

    private const float LineGap = 12f;        // 行距（unityParagraphSpacing，配合手动折行）
    private const float BoxContentZone = 84f; // 折行设计容量（2×36+行距12 或 3×20+2×12），小于实际内容区100，居中留白
    private const float WidthSafety = 42f;    // 字号估算的安全边距

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

    /// <summary>跳过富文本标签后的等效 em 宽。</summary>
    private static float WidthEm(string s)
    {
        float w = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '<')
            {
                int close = s.IndexOf('>', i);
                if (close != -1) { i = close; continue; }
            }
            w += CharWidthEm(s[i]);
        }
        return w;
    }

    /// <summary>动态字号（4步进 36→20）+ 手动折行；装不进内容区就降档重折。</summary>
    private string FitAndWrap(string text, float avail, out int fs)
    {
        float units = WidthEm(text);
        int est = units > 0.01f ? (int)(2f * (avail - WidthSafety) / units / 4f) * 4 : 36;
        fs = Mathf.Clamp(est, 20, 36);
        while (true)
        {
            string wrapped = WrapText(text, fs, avail);
            int rows = wrapped.Split('\n').Length;
            if (rows * fs + (rows - 1) * LineGap <= BoxContentZone || fs <= 20)
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
                    if (IsNoStart(c)) { line.Append(c); lw += cw; continue; }
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
