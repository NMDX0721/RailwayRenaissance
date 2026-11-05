using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class TypewriterEffect : MonoBehaviour
{
    private const float BaseNormalSpeed = 0.03f;   // 设置“文字速度”为默认值 5 时的每字间隔
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
    public event Action<string> TextUpdated;

    private void SetRenderedText(string text)
    {
        dialogueText.text = text;
        TextUpdated?.Invoke(text);
    }

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
        SetRenderedText("");

        // 设置页“文字速度”(1–10，默认 5)：速度越大每字间隔越短，默认值保持原来的 0.03s
        int speedSetting = Mathf.Clamp(PlayerPrefs.GetInt("TextSpeed", 5), 1, 10);
        float NormalSpeed = Mathf.Clamp(BaseNormalSpeed * 5f / speedSetting, 0.006f, 0.15f);

        // 时间步进而非逐字 WaitForSeconds：帧率会把短延迟抬到 1 帧下限，
        // 导致 Shift 加速（0.005s/字）被削成与正常速度几乎无差；按帧累计后一帧可吐多字。
        float acc = 0f;
        int i = 0;
        while (i < text.Length)
        {
            if (skipRequested)
            {
                SetRenderedText(text);
                break;
            }

            bool fast = LegacyInputAdapter.GetKey(KeyCode.LeftShift) || LegacyInputAdapter.GetKey(KeyCode.RightShift);
            float interval = fast ? FastSpeed : NormalSpeed;
            acc += Time.deltaTime;

            while (i < text.Length)
            {
                if (skipRequested) break;

                // 富文本标签整段吞入（如 <color=...> <i> </i>），不计时
                if (text[i] == '<')
                {
                    int closeTag = text.IndexOf('>', i);
                    if (closeTag != -1)
                    {
                        textBuilder.Append(text, i, closeTag - i + 1);
                        dialogueText.text = textBuilder.ToString();
                        i = closeTag + 1;
                        continue;
                    }
                }

                if (acc < interval) break;
                acc -= interval;

                if (useTypingSFX && text[i] != ' ' && text[i] != '\n' && GameData.TypewriterVolume > 0.01f)
                {
                    sfxTimer += interval;
                    if (sfxTimer >= sfxInterval)
                    {
                        VNAudioManager.Instance?.PlayTypewriterSFX();
                        sfxTimer = 0f;
                    }
                }

                textBuilder.Append(text[i]);
                SetRenderedText(textBuilder.ToString());
                i++;
            }

            if (skipRequested)
            {
                SetRenderedText(text);
                break;
            }
            yield return null;
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
