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

    /// <summary>复古画风选图：预渲染 bg_low/（tools/pixelate_bg.py v2 产物，零运行时开销）优先；
    /// 未预渲染的新图回退到动态降采样兜底。</summary>
    private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _retroCache = new();

    private static Texture2D LoadBgTex(string bgName)
    {
        if (PlayerPrefs.GetInt("RetroLowResBG", 0) != 1)
            return Resources.Load<Texture2D>("bg/" + bgName);

        // 1) 预渲染优先（目标宽度法低颗粒，离线生成）
        var prerendered = Resources.Load<Texture2D>("bg_low/" + bgName);
        if (prerendered != null) return prerendered;

        // 2) 兜底：新图未预渲染 → 动态降采样
        var src = Resources.Load<Texture2D>("bg/" + bgName);
        if (src == null) return null;

        string key = bgName + "@" + src.width + "x" + src.height;
        if (_retroCache.TryGetValue(key, out var cached) && cached != null) return cached;

        var retro = DownscalePoint(src, 4);
        if (retro != null)
        {
            _retroCache[key] = retro;
            return retro;
        }
        return src;
    }

    private static Texture2D DownscalePoint(Texture2D src, int factor)
    {
        try
        {
            int w = Mathf.Max(8, src.width / factor);
            int h = Mathf.Max(8, src.height / factor);
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var small = new Texture2D(w, h, TextureFormat.RGBA32, false);
            small.filterMode = FilterMode.Point;   // 硬像素块（复古颗粒的关键）
            small.wrapMode = TextureWrapMode.Clamp;
            small.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            small.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return small;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[VN BG] retro downscale failed: " + ex.Message);
            return null;
        }
    }

    /// <summary>设计即黑屏字卡的背景名（缺图队列标注"非缺图"）：缺图属正常，不打警告。</summary>
    private static bool IsDesignBlackBg(string name) => name == "news_2050" || name == "news_2072";

    public void SetBackground(string bgName, TransitionType transition)
    {
        if (string.IsNullOrEmpty(bgName)) return;

        // 纯黑 / 标题卡缺图：不加载图片，直接纯色（避免透出清屏紫）
        if (bgName == "black" || bgName == "title_card")
        {
            var missingTitle = bgName == "title_card" && Resources.Load<Texture2D>("bg/title_card") == null;
            if (bgName == "black" || missingTitle)
            {
                ApplySolidColor(bgName == "title_card" && !missingTitle ? Color.black : new Color(0.04f, 0.03f, 0.05f, 1f));
                if (missingTitle) Debug.LogWarning("[VN BG] title_card.png missing, solid fallback");
                return;
            }
        }

        var tex = LoadBgTex(bgName);
        if (tex == null)
        {
            // news_2050/news_2072 设计即黑屏字卡（缺图队列标注"非缺图"）：静默纯色兜底，不刷警告
            if (!IsDesignBlackBg(bgName))
                Debug.LogWarning("[VN BG] Background not found: " + bgName + " — solid black fallback");
            ApplySolidColor(new Color(0.04f, 0.03f, 0.05f, 1f));
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

    /// <summary>缓存 1x1 颜色纹理（解决 image=null 时 tint 不生效、透出清屏紫的问题）。</summary>
    private static Texture2D solidBlackTex;

    private static Texture2D GetSolidBlack()
    {
        if (solidBlackTex != null) return solidBlackTex;
        solidBlackTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        solidBlackTex.SetPixel(0, 0, Color.black);
        solidBlackTex.Apply();
        return solidBlackTex;
    }

    /// <summary>纯色背景（black 专用），用 1x1 纹理确保渲染。</summary>
    private void ApplySolidColor(Color color)
    {
        var tex = GetSolidBlack();
        bgOld.style.backgroundImage = new StyleBackground(tex);
        bgOld.style.unityBackgroundImageTintColor = color;
        bgNew.style.backgroundImage = null;
        bgNew.style.unityBackgroundImageTintColor = new Color(0, 0, 0, 0);
    }

    public void SetBackgroundImmediate(string bgName)
    {
        if (string.IsNullOrEmpty(bgName)) return;

        // 纯黑 / 缺图标题卡：用 1x1 纹理纯色（image=null + tint 不渲染，会透出清屏紫）
        if (bgName == "black")
        {
            var blackTex = GetSolidBlack();
            bgOld.style.backgroundImage = new StyleBackground(blackTex);
            bgOld.style.unityBackgroundImageTintColor = Color.black;
            bgNew.style.backgroundImage = null;
            bgNew.style.unityBackgroundImageTintColor = new Color(0, 0, 0, 0);
            return;
        }

        var tex = LoadBgTex(bgName);
        if (tex == null)
        {
            // 缺图（如 title_card 未生成）→ 深色纯底，不透紫
            var blackTex = GetSolidBlack();
            bgOld.style.backgroundImage = new StyleBackground(blackTex);
            bgOld.style.unityBackgroundImageTintColor = new Color(0.04f, 0.03f, 0.05f, 1f);
            bgNew.style.backgroundImage = null;
            bgNew.style.unityBackgroundImageTintColor = new Color(0, 0, 0, 0);
            if (!IsDesignBlackBg(bgName))
                Debug.LogWarning("[VN BG] missing " + bgName + ", solid fallback");
            return;
        }

        bgOld.style.backgroundImage = new StyleBackground(tex);
        bgOld.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);
        bgNew.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0);
    }

    private IEnumerator TransitionFade(Texture2D newTex)
    {
        if (newTex == null) { isTransitioning = false; yield break; }
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
