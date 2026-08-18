using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class TypewriterEffect : MonoBehaviour
{
    private const float NormalSpeed = 0.03f;
    private const float FastSpeed = 0.005f;

    private Label dialogueText;
    private Coroutine typingCoroutine;
    private Action onCompleteCallback;
    private bool isTyping;
    private bool skipRequested;
    private readonly StringBuilder textBuilder = new StringBuilder();

    private bool useTypingSFX = true;
    private float sfxInterval = 0.05f;
    private float sfxTimer;

    public bool IsTyping => isTyping;

    public void Initialize(Label textLabel)
    {
        dialogueText = textLabel;
    }

    public void StartTypewriter(string text, Action onComplete)
    {
        StopTyping();

        onCompleteCallback = onComplete;
        skipRequested = false;
        sfxTimer = 0f;
        textBuilder.Clear();

        typingCoroutine = StartCoroutine(TypeText(text));
    }

    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
    }

    public void SkipTyping()
    {
        skipRequested = true;
    }

    private IEnumerator TypeText(string text)
    {
        if (dialogueText == null)
        {
            Debug.LogError("Dialogue text label is not assigned.");
            yield break;
        }

        isTyping = true;
        dialogueText.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            if (skipRequested)
            {
                dialogueText.text = text;
                break;
            }

            // 富文本标签整段吞入（如 <color=...> <i> </i>），避免逐字打出控制码
            if (text[i] == '<')
            {
                int closeTag = text.IndexOf('>', i);
                if (closeTag != -1)
                {
                    textBuilder.Append(text, i, closeTag - i + 1);
                    dialogueText.text = textBuilder.ToString();
                    i = closeTag;
                    continue;
                }
            }

            textBuilder.Append(text[i]);
            dialogueText.text = textBuilder.ToString();

            float delay = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? FastSpeed : NormalSpeed;

            if (useTypingSFX && text[i] != ' ' && GameData.TypewriterVolume > 0.01f)
            {
                sfxTimer += delay;
                if (sfxTimer >= sfxInterval)
                {
                    VNAudioManager.Instance?.PlayTypewriterSFX();
                    sfxTimer = 0f;
                }
            }

            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        typingCoroutine = null;

        onCompleteCallback?.Invoke();
    }

    private void OnDestroy()
    {
        StopTyping();
    }
}