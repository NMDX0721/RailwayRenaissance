using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class BackgroundManager : MonoBehaviour
{
    private VisualElement bgContainer;
    private VisualElement bgOld;
    private VisualElement bgNew;
    private Coroutine transitionCoroutine;
    private bool isTransitioning;

    public void Init(UIDocument document)
    {
        var root = document.rootVisualElement;

        bgContainer = new VisualElement { name = "vn-background-container" };
        bgContainer.style.position = Position.Absolute;
        bgContainer.style.top = 0;
        bgContainer.style.left = 0;
        bgContainer.style.right = 0;
        bgContainer.style.bottom = 0;
        bgContainer.style.overflow = Overflow.Hidden;
        root.Add(bgContainer);

        bgOld = CreateBgLayer("vn-bg-old");
        bgNew = CreateBgLayer("vn-bg-new");
        bgContainer.Add(bgOld);
        bgContainer.Add(bgNew);

        // 不要 BringToFront，让背景保持在底层
    }

    private VisualElement CreateBgLayer(string name)
    {
        var layer = new VisualElement { name = name };
        layer.style.position = Position.Absolute;
        layer.style.top = 0;
        layer.style.left = 0;
        layer.style.right = 0;
        layer.style.bottom = 0;
        layer.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
        layer.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0);
        return layer;
    }

    public void SetBackground(string bgName)
    {
        SetBackground(bgName, TransitionType.Fade);
    }

    public void SetBackground(string bgName, TransitionType transition)
    {
        if (string.IsNullOrEmpty(bgName)) return;

        var tex = Resources.Load<Texture2D>("bg/" + bgName);
        if (tex == null)
        {
            Debug.LogWarning("[VN BG] Background not found: " + bgName);
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        switch (transition)
        {
            case TransitionType.SlideLeft:
                transitionCoroutine = StartCoroutine(TransitionSlide(tex, true));
                break;
            case TransitionType.SlideRight:
                transitionCoroutine = StartCoroutine(TransitionSlide(tex, false));
                break;
            case TransitionType.Fade:
            default:
                transitionCoroutine = StartCoroutine(TransitionFade(tex));
                break;
        }
    }

    public void SetBackgroundImmediate(string bgName)
    {
        if (string.IsNullOrEmpty(bgName)) return;
        var tex = Resources.Load<Texture2D>("bg/" + bgName);
        if (tex == null) return;

        bgOld.style.backgroundImage = new StyleBackground(tex);
        bgOld.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);
        bgNew.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0);
    }

    private IEnumerator TransitionFade(Texture2D newTex)
    {
        isTransitioning = true;

        // Fade out old
        float duration = 0.4f;
        float elapsed = 0f;
        float startAlpha = bgOld.style.unityBackgroundImageTintColor.value.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            bgOld.style.unityBackgroundImageTintColor = new Color(1, 1, 1, alpha);
            yield return null;
        }

        // Swap
        bgNew.style.backgroundImage = new StyleBackground(newTex);
        bgNew.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0);

        // Fade in new
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            bgNew.style.unityBackgroundImageTintColor = new Color(1, 1, 1, alpha);
            yield return null;
        }

        // Finalize: new becomes old
        bgOld.style.backgroundImage = new StyleBackground(newTex);
        bgOld.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);
        bgNew.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0);

        isTransitioning = false;
        transitionCoroutine = null;
    }

    private IEnumerator TransitionSlide(Texture2D newTex, bool slideLeft)
    {
        isTransitioning = true;
        float duration = 0.5f;

        // Prepare new layer off-screen
        bgNew.style.backgroundImage = new StyleBackground(newTex);
        bgNew.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);

        float screenWidth = Screen.width > 0 ? Screen.width : 1920f;
        float startX = slideLeft ? screenWidth : -screenWidth;
        float endX = 0f;
        float oldEndX = slideLeft ? -screenWidth : screenWidth;

        bgNew.style.translate = new Translate(new Length(startX, LengthUnit.Pixel), 0);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float newX = Mathf.Lerp(startX, endX, t);
            float oldX = Mathf.Lerp(0, oldEndX, t);

            bgNew.style.translate = new Translate(new Length(newX, LengthUnit.Pixel), 0);
            bgOld.style.translate = new Translate(new Length(oldX, LengthUnit.Pixel), 0);
            yield return null;
        }

        // Finalize
        bgNew.style.translate = new Translate(0, 0);
        bgOld.style.translate = new Translate(0, 0);
        bgOld.style.backgroundImage = new StyleBackground(newTex);
        bgOld.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);
        bgNew.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0);

        isTransitioning = false;
        transitionCoroutine = null;
    }

    public bool IsTransitioning => isTransitioning;
}
