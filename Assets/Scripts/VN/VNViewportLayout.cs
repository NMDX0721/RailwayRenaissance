using UnityEngine;
using UnityEngine.UIElements;

public sealed class VNViewportLayout : MonoBehaviour
{
    public bool UseScreenMetrics = true;
    public DialogueBox ReadingBox;
    private UIDocument document;
    private Rect lastSafeArea;
    private Vector2 lastSize;
    private int lastChildren;

    public void Init(UIDocument value)
    {
        document = value;
        document.rootVisualElement.RegisterCallback<GeometryChangedEvent>(_ => Refresh());
        Refresh();
    }

    private void LateUpdate()
    {
        if (!UseScreenMetrics || document == null) return;
        if (lastSafeArea != Screen.safeArea || lastSize != new Vector2(Screen.width, Screen.height) ||
            lastChildren != document.rootVisualElement.childCount) Refresh();
    }

    public void Refresh()
    {
        if (UseScreenMetrics)
            Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height), Screen.dpi, Application.isMobilePlatform);
    }

    public void Apply(Rect safeArea, Vector2 pixels, float dpi, bool touch)
    {
        if (document == null || pixels.x <= 0 || pixels.y <= 0) return;
        var root = document.rootVisualElement;
        float width = root.contentRect.width, height = root.contentRect.height;
        if (width <= 0 || height <= 0) return;
        lastSafeArea = safeArea; lastSize = pixels; lastChildren = root.childCount;
        float left = Mathf.Max(0, safeArea.xMin) * width / pixels.x;
        float right = Mathf.Max(0, pixels.x - safeArea.xMax) * width / pixels.x;
        float bottom = Mathf.Max(0, safeArea.yMin) * height / pixels.y;
        float top = Mathf.Max(0, pixels.y - safeArea.yMax) * height / pixels.y;
        float hit = touch ? Mathf.Clamp(48 * (dpi > 0 ? dpi / 160 : 2) * height / pixels.y, 64, 112) : 40;

        Bounds("character-container", left, right, top, bottom);
        float capsuleHeight = touch ? 24 : 18;
        float bandHeight = capsuleHeight + 8;
        Bounds("dialogue-box", left, right, null, bottom + bandHeight);
        Bounds("progress-bar", left, right, null, bottom);
        var progress = root.Q<VisualElement>("progress-bar");
        if (progress != null)
        {
            progress.style.height = bandHeight;
            float radius=touch?12:9;
            var capsule = progress.Q("progress-capsule");
            if (capsule != null)
            {
                capsule.style.borderTopLeftRadius = capsule.style.borderTopRightRadius = radius;
                capsule.style.borderBottomLeftRadius = capsule.style.borderBottomRightRadius = radius;
            }
            var body=progress.Q("progress-capsule-body");if(body!=null)body.style.left=body.style.right=radius;
            foreach(var cap in progress.Query<VNCapsuleEnd>().ToList())cap.style.width=radius;
        }
        Bounds("menu-bar", null, right + 10, top + 10, null);
        Bounds("menu-expanded", null, right + 10, top + 64, null);
        Bounds("vn-story-signal", left + 14, null, top + 16, null);
        ReadingBox?.SetReadingHitSize(touch ? hit : 44);
        Bounds("phone-bar", left, right, top + 18, null);
        Bounds("options-container", left, right, top, bottom);
        foreach (string name in new[] { "backlog-panel", "saveload-panel", "archive-panel" })
            Bounds(name, left, right, top, bottom);
        // Dynamic buttons can be added without changing the root geometry.
        foreach (var button in root.Query<Button>().ToList())
        {
            bool reading = button.name == "vn-voice-replay" || button.name == "vn-bookmark-btn";
            bool toolbar = button.name == "vn-auto-btn" || button.name == "vn-menu-btn";
            if (toolbar || button.parent?.name == "menu-expanded") { button.style.minHeight = 0; button.style.minWidth = 0; }
            else if (touch && !reading) { button.style.minHeight = hit; button.style.minWidth = hit; }
        }

        void Bounds(string name, float? l, float? r, float? t, float? b)
        {
            var element = root.Q<VisualElement>(name);
            if (element == null) return;
            if (l.HasValue) element.style.left = l.Value;
            if (r.HasValue) element.style.right = r.Value;
            if (t.HasValue) element.style.top = t.Value;
            if (b.HasValue) element.style.bottom = b.Value;
        }
    }
}
