using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 成就解锁右下角弹窗（Steam 风）：滑入 → 停留 → 淡出，支持队列与全场景通用（OnGUI）。
/// </summary>
public class AchievementToast : MonoBehaviour
{
    private class ToastItem
    {
        public AchievementData data;
        public float enterTime;
    }

    private static AchievementToast _instance;
    public static AchievementToast Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("AchievementToast");
                _instance = go.AddComponent<AchievementToast>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private readonly Queue<ToastItem> queue = new Queue<ToastItem>();
    private ToastItem current;
    private float currentStart;

    private const float SlideInDuration = 0.35f;   // 滑入
    private const float HoldDuration = 3.0f;       // 停留
    private const float FadeOutDuration = 0.6f;    // 淡出
    private const float PanelWidth = 420f;
    private const float PanelHeight = 96f;
    private const float Margin = 24f;

    private GUIStyle titleStyle;
    private GUIStyle descStyle;
    private GUIStyle subtitleStyle;
    private Texture2D bgTex;
    private bool stylesReady;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        var font = Resources.Load<Font>("Fonts/zpix");

        bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0.10f, 0.07f, 0.04f, 0.96f));
        bgTex.Apply();

        titleStyle = new GUIStyle();
        titleStyle.fontSize = 22;
        titleStyle.font = font;
        titleStyle.normal.textColor = new Color(1f, 0.82f, 0.30f, 1f);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleLeft;

        subtitleStyle = new GUIStyle();
        subtitleStyle.fontSize = 14;
        subtitleStyle.font = font;
        subtitleStyle.normal.textColor = new Color(1f, 1f, 1f, 0.45f);
        subtitleStyle.alignment = TextAnchor.MiddleLeft;

        descStyle = new GUIStyle();
        descStyle.fontSize = 17;
        descStyle.font = font;
        descStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
        descStyle.alignment = TextAnchor.MiddleLeft;
        descStyle.wordWrap = true;
    }

    /// <summary>静态调用（AchievementManager 解锁时触发）。</summary>
    public static void ShowAchievement(AchievementData data)
    {
        var inst = Instance;
        inst.InitStyles();
        inst.Enqueue(data);
    }

    private void Enqueue(AchievementData data)
    {
        queue.Enqueue(new ToastItem { data = data });
    }

    private void Update()
    {
        if (current == null && queue.Count > 0)
        {
            current = queue.Dequeue();
            currentStart = Time.unscaledTime;
        }
        if (current != null)
        {
            // 停留 + 淡出后移除
            float totalLife = Time.unscaledTime - currentStart;
            if (totalLife > SlideInDuration + HoldDuration + FadeOutDuration)
                current = null;
        }
        // 计算新纪录的停留窗口（新纪录可延续当前）
        // （单条展示，队列仅顺序弹出）
    }

    private void OnGUI()
    {
        if (current == null) return;
        InitStyles();
        if (current.data == null) { current = null; return; }

        float life = Time.unscaledTime - currentStart;

        // 滑入 / 停留 / 淡出
        float slideT = Mathf.Clamp01(life / SlideInDuration);
        float fade = 1f;
        float fadeLife = life - (SlideInDuration + HoldDuration);
        if (fadeLife > 0)
        {
            fade = Mathf.Clamp01(1f - fadeLife / FadeOutDuration);
        }

        float eased = 1f - (1f - slideT) * (1f - slideT); // ease-out
        float offsetX = (1f - eased) * 80f;               // 从右侧滑入

        float x = Screen.width - Margin - PanelWidth - offsetX;
        float y = Screen.height - Margin - PanelHeight - 60f;

        GUI.depth = 0;
        GUI.color = new Color(1f, 1f, 1f, fade);
        var prev = GUI.color;

        // 背景
        var rect = new Rect(x, y, PanelWidth, PanelHeight);
        GUI.DrawTexture(rect, bgTex, ScaleMode.StretchToFill);

        // 金色左侧徽章
        var badgeRect = new Rect(rect.x + 14, rect.y + 14, 42, 42);
        var badgeTex = MakeBadgeTexture(current.data.rarity);
        GUI.DrawTexture(badgeRect, badgeTex, ScaleMode.StretchToFill);

        // 标题 "成就解锁"
        var subRect = new Rect(rect.x + 70, rect.y + 10, rect.width - 80, 22);
        GUI.Label(subRect, "成就解锁", subtitleStyle);

        // 成就标题
        var titleRect = new Rect(rect.x + 70, rect.y + 30, rect.width - 80, 26);
        GUI.Label(titleRect, current.data.title, titleStyle);

        // 描述
        var descRect = new Rect(rect.x + 14, rect.y + 62, rect.width - 28, 28);
        GUI.Label(descRect, current.data.description, descStyle);

        GUI.color = prev;
    }

    private Texture2D MakeBadgeTexture(AchievementRarity rarity)
    {
        var tex = new Texture2D(64, 64);
        Color c;
        switch (rarity)
        {
            case AchievementRarity.Common: c = new Color(0.75f, 0.75f, 0.75f, 1f); break;
            case AchievementRarity.Rare: c = new Color(0.35f, 0.72f, 1f, 1f); break;
            case AchievementRarity.Epic: c = new Color(0.72f, 0.45f, 1f, 1f); break;
            default: c = new Color(1f, 0.80f, 0.25f, 1f); break;
        }
        for (int i = 0; i < 64; i++)
        {
            for (int j = 0; j < 64; j++)
            {
                float dx = i - 32f, dy = j - 32f;
                tex.SetPixel(i, j, (dx * dx + dy * dy) < 32f * 32f ? c : Color.clear);
            }
        }
        tex.Apply();
        return tex;
    }
}