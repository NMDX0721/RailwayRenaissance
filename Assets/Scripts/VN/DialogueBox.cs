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
        dialogueBox.style.bottom = 30;
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
        dialogueBox.style.borderTopLeftRadius = 8;
        dialogueBox.style.borderTopRightRadius = 8;
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
    }

    public VisualElement RootElement => dialogueBox;

    public void ShowDialogue(string speaker, string text)
    {
        if (dialogueBox == null) return;
        dialogueBox.style.display = DisplayStyle.Flex;

        StopContinueAnimation();

        bool isNarration = string.IsNullOrEmpty(speaker) || speaker == "n";
        nameArea.style.visibility = isNarration ? Visibility.Hidden : Visibility.Visible;

        if (!isNarration)
        {
            speakerName.text = speaker;
            dialogueText.text = "\u300C" + text + "\u300D";
        }
        else
        {
            dialogueText.text = text;
        }

        continueIndicator.style.display = DisplayStyle.None;
        if (typewriterEffect != null)
        {
            string displayText = isNarration ? text : "\u300C" + text + "\u300D";
            typewriterEffect.StartTypewriter(displayText, () =>
            {
                StartContinueAnimation();
                OnTypewriterComplete?.Invoke();
            });
        }
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
