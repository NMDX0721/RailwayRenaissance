using System;
using System.Collections;
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
        dialogueBox.style.bottom = 36; // 与屏幕底部留一点空，不贴边也不悬空
        dialogueBox.style.height = 150;
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
            typewriterEffect.StartTypewriter(displayText, () =>
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

    public void Hide() => dialogueBox.style.display = DisplayStyle.None;
    public void Show() => dialogueBox.style.display = DisplayStyle.Flex;

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
