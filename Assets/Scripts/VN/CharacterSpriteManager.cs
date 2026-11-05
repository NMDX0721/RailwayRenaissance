using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CharacterSpriteManager : MonoBehaviour
{
    // 显示名 → 贴图 ID（男大学生=林彪悍）；未映射名不得落到已有角色
    private static readonly System.Collections.Generic.Dictionary<string, string> SpeakerAlias =
        new System.Collections.Generic.Dictionary<string, string>
        {
            { "男大学生", "lin_biaohan" },
            { "林彪悍", "lin_biaohan" },
            { "老陈", "laochen" },
            { "岁月", "suiyue" },
            { "嘉颖徐", "jiaying_xu" },
            { "张工", "zhanggong" },
            { "李阿姨", "liaiyi" },
            { "王小弟", "wangxiaodi" },
            { "赵师傅", "zhaoshifu" },
            { "赵铁山", "zhaoshifu" },
            { "小芳", "xiaofang" },
            { "导师", "daoshi" },
            { "教授", "daoshi" },
            { "佯教授", "daoshi" },
            { "佯明远", "daoshi" },
        };

    private static string ToSpriteId(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return SpeakerAlias.TryGetValue(name, out var id) ? id : name;
    }

    // 缺贴图时的占位：用名字生成色块，绝不回退到他人立绘
    private static readonly Dictionary<string, Color> PlaceholderTint = new Dictionary<string, Color>
    {
        { "daoshi", new Color(0.45f, 0.55f, 0.75f, 0.35f) },
        { "zhanggong", new Color(0.55f, 0.45f, 0.35f, 0.35f) },
        { "liaiyi", new Color(0.75f, 0.55f, 0.55f, 0.35f) },
        { "wangxiaodi", new Color(0.45f, 0.65f, 0.55f, 0.35f) },
        { "zhaoshifu", new Color(0.55f, 0.55f, 0.45f, 0.35f) },
        { "xiaofang", new Color(0.65f, 0.55f, 0.70f, 0.35f) },
    };

    private VisualElement container;
    private VisualElement slotLeft;
    private VisualElement slotCenter;
    private VisualElement slotRight;
    private readonly Dictionary<string, Texture2D> spriteCache = new Dictionary<string, Texture2D>();
    private readonly HashSet<string> missingWarnings = new HashSet<string>();
    public int ResourceLookupCount { get; private set; }

    public void ClearSpriteCache()
    {
        spriteCache.Clear();
        missingWarnings.Clear();
        ResourceLookupCount = 0;
    }
    private readonly List<(VisualElement slot, float width, float height)> activeSlots =
        new List<(VisualElement, float, float)>();

    // 每个角色的默认slot尺寸（像素）——基于实际精灵的有效内容区域（1.25x 放大，贴底显示）
    private readonly Dictionary<string, (int w, int h)> charSlotSize = new Dictionary<string, (int, int)>
    {
        { "lin_biaohan", (450, 675) },   // 2:3 ratio
        { "suiyue",      (450, 675) },   // 2:3 ratio
        { "jiaying_xu",  (660, 727) },   // 半身构图；提高头部位置以接近其他成人的视线高度
        { "laochen",     (450, 450) },   // 1:1 ratio
    };
    private const int DefaultSlotW = 450;
    private const int DefaultSlotH = 675;

    public void Init(UIDocument document)
    {
        var root = document.rootVisualElement;

        container = new VisualElement { name = "character-container" };
        container.style.position = Position.Absolute;
        container.style.top = 0;
        container.style.left = 0;
        container.style.right = 0;
        container.style.bottom = 0; // 贴屏幕底端，脚不悬浮
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.FlexEnd;
        container.style.justifyContent = Justify.Center;
        container.pickingMode = PickingMode.Ignore;
        root.Add(container);
        container.RegisterCallback<GeometryChangedEvent>(_ => ReflowSlots());

        slotLeft = CreateCharSlot("char-left");
        slotCenter = CreateCharSlot("char-center");
        slotRight = CreateCharSlot("char-right");

        container.Add(slotLeft);
        container.Add(slotCenter);
        container.Add(slotRight);
    }

    private VisualElement CreateCharSlot(string slotName)
    {
        var slot = new VisualElement { name = slotName };
        slot.style.position = Position.Absolute;
        slot.style.bottom = 0;
        slot.style.width = 360;
        slot.style.height = 540;
        slot.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
        slot.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0);
        slot.style.alignSelf = Align.FlexEnd;
        return slot;
    }

    public void UpdateDisplay(CharacterEntry[] chars, string emotion, string speaker = null, string[] reactions = null)
    {
        ClearAll();
        if (chars == null || chars.Length == 0)
            return;

        // VN camera belongs to the player; keep legacy assets and script slots for migration.
        chars = System.Array.FindAll(chars, entry => entry != null &&
            !string.IsNullOrEmpty(entry.name) && ToSpriteId(entry.name) != "lin_biaohan");
        if (chars.Length == 0) return;
        System.Array.Sort(chars, (a, b) => PositionRank(a.pos).CompareTo(PositionRank(b.pos)));

        // 解析旁观者表情映射
        var reactionMap = new Dictionary<string, string>();
        if (reactions != null)
        {
            foreach (var r in reactions)
            {
                var parts = r.Split(':');
                if (parts.Length == 2)
                    reactionMap[parts[0].Trim()] = parts[1].Trim();
            }
        }

        foreach (var entry in chars)
        {
            if (string.IsNullOrEmpty(entry.name)) continue;

            // speaker 显示名（男大学生/林彪悍/老陈…）与贴图 ID 对齐
            string spriteId = ToSpriteId(entry.name);
            string speakerId = ToSpriteId(speaker);
            bool isSpeaker = string.IsNullOrEmpty(speaker) ||
                             entry.name == speaker ||
                             spriteId == speakerId;
            string emotionKey;

            if (isSpeaker && !string.IsNullOrEmpty(emotion))
            {
                emotionKey = emotion;
            }
            else if (reactionMap.TryGetValue(entry.name, out var reaction) ||
                     reactionMap.TryGetValue(spriteId, out reaction))
            {
                emotionKey = reaction;
            }
            else
            {
                emotionKey = "normal";
            }
            Texture2D tex = null;

            string[] tryPaths = {
                "characters/" + spriteId + "_" + emotionKey,
                "characters/" + spriteId + "/" + emotionKey,
            };
            if (emotionKey != "normal")
            {
                tryPaths = new[] {
                    "characters/" + spriteId + "_" + emotionKey,
                    "characters/" + spriteId + "/" + emotionKey,
                    "characters/" + spriteId + "_normal",
                    "characters/" + spriteId + "/normal",
                };
            }

            foreach (var path in tryPaths)
            {
                if (spriteCache.TryGetValue(path, out tex))
                {
                    if (tex != null) break;
                    continue;
                }
                ResourceLookupCount++;
                tex = Resources.Load<Texture2D>(path);
                spriteCache[path] = tex;
                if (tex != null) break;
                tex = null;
            }

            var slot = GetSlotForPosition(entry.pos);
            if (slot == null) continue;

            if (charSlotSize.TryGetValue(spriteId, out var size))
            {
                slot.style.width = size.w;
                slot.style.height = size.h;
            }
            else
            {
                slot.style.width = DefaultSlotW;
                slot.style.height = DefaultSlotH;
            }

            if (tex == null)
            {
                // 缺立绘：占位色块 + 名字，不把 emotion 写到别人身上
                if (missingWarnings.Add(spriteId + "/" + emotionKey))
                    Debug.LogWarning("[VN Characters] Sprite not found (placeholder): " + entry.name + " / " + emotionKey);
                slot.style.backgroundImage = StyleKeyword.Null;
                var tint = PlaceholderTint.TryGetValue(spriteId, out var c) ? c : new Color(0.5f, 0.5f, 0.5f, 0.3f);
                slot.style.backgroundColor = tint;
                slot.style.borderTopWidth = 2; slot.style.borderBottomWidth = 2;
                slot.style.borderLeftWidth = 2; slot.style.borderRightWidth = 2;
                slot.style.borderTopColor = new Color(1f, 1f, 1f, 0.25f);
                slot.style.borderBottomColor = new Color(1f, 1f, 1f, 0.25f);
                slot.style.borderLeftColor = new Color(1f, 1f, 1f, 0.25f);
                slot.style.borderRightColor = new Color(1f, 1f, 1f, 0.25f);
                slot.Clear();
                var ph = new Label(entry.name + "\n立绘待绘制");
                ph.style.unityTextAlign = TextAnchor.MiddleCenter;
                ph.style.fontSize = 18;
                ph.style.color = new Color(1f, 1f, 1f, 0.7f);
                ph.style.whiteSpace = WhiteSpace.Normal;
                ph.pickingMode = PickingMode.Ignore;
                slot.Add(ph);
                PositionSlot(slot);
                continue;
            }

            slot.style.backgroundColor = new Color(0, 0, 0, 0);
            slot.style.borderTopWidth = 0; slot.style.borderBottomWidth = 0;
            slot.style.borderLeftWidth = 0; slot.style.borderRightWidth = 0;
            slot.Clear();
            slot.style.backgroundImage = new StyleBackground(tex);
            slot.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);
            PositionSlot(slot);
        }
    }

    private void PositionSlot(VisualElement slot)
    {
        slot.style.display = DisplayStyle.Flex;
        activeSlots.Add((slot, slot.style.width.value.value, slot.style.height.value.value));
        ReflowSlots();
    }

    private static int PositionRank(string pos) => pos == "left" ? 0 : pos == "right" ? 2 : 1;

    private void ReflowSlots()
    {
        int count = activeSlots.Count;
        if (count == 0) return;
        float availableWidth = container?.contentRect.width ?? 0;
        if (availableWidth <= 0) availableWidth = 1200;
        float widest = 1;
        foreach (var item in activeSlots) widest = Mathf.Max(widest, item.width * FramingScale(item.slot));
        float scale = Mathf.Min(1, Mathf.Max(1, availableWidth / count - 40) / widest);
        for (int index = 0; index < count; index++)
        {
            var item = activeSlots[index];
            var slot = item.slot;
            float framing = FramingScale(slot);
            slot.style.width = item.width * scale * framing;
            slot.style.height = item.height * scale * framing;
            slot.style.right = Length.Auto();
            if (count == 1)
            {
                slot.style.left = Length.Auto();
                slot.style.marginLeft = 0;
                slot.style.alignSelf = Align.Center;
            }
            else
            {
                slot.style.left = Length.Percent((index + 0.5f) * 100 / count);
                slot.style.marginLeft = -item.width * scale * framing * 0.5f;
                slot.style.alignSelf = Align.FlexEnd;
            }
        }
        float FramingScale(VisualElement slot)
        {
            var texture = slot.style.backgroundImage.value.texture;
            return count > 1 && texture != null && texture.name.StartsWith("jiaying_xu") ? 0.8f : 1f;
        }
    }

    private VisualElement GetSlotForPosition(string pos)
    {
        switch (pos)
        {
            case "left": return slotLeft;
            case "center": return slotCenter;
            case "right": return slotRight;
            default: return slotCenter;
        }
    }

    public void ClearAll()
    {
        activeSlots.Clear();
        if (slotLeft != null) ClearSlot(slotLeft);
        if (slotCenter != null) ClearSlot(slotCenter);
        if (slotRight != null) ClearSlot(slotRight);
    }

    private void ClearSlot(VisualElement slot)
    {
        if (slot == null) return;
        slot.style.display = DisplayStyle.None;
        slot.style.backgroundImage = StyleKeyword.Null;
        slot.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0);
        slot.style.alignSelf = Align.FlexEnd;
    }
}
