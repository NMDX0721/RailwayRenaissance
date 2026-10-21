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

    // 每个角色的默认slot尺寸（像素）——基于实际精灵的有效内容区域（1.25x 放大，贴底显示）
    private readonly Dictionary<string, (int w, int h)> charSlotSize = new Dictionary<string, (int, int)>
    {
        { "lin_biaohan", (450, 675) },   // 2:3 ratio
        { "suiyue",      (450, 675) },   // 2:3 ratio
        { "jiaying_xu",  (550, 606) },   // 接近1:1，需要更宽
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
        if (chars == null || chars.Length == 0)
            return;

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
                if (spriteCache.TryGetValue(path, out tex) && tex != null) break;
                tex = Resources.Load<Texture2D>(path);
                if (tex != null) { spriteCache[path] = tex; break; }
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
                PositionSlot(slot, entry.pos, chars.Length);
                continue;
            }

            slot.style.backgroundColor = new Color(0, 0, 0, 0);
            slot.style.borderTopWidth = 0; slot.style.borderBottomWidth = 0;
            slot.style.borderLeftWidth = 0; slot.style.borderRightWidth = 0;
            slot.Clear();
            slot.style.backgroundImage = new StyleBackground(tex);
            slot.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);
            PositionSlot(slot, entry.pos, chars.Length);
        }
    }

    private void PositionSlot(VisualElement slot, string pos, int totalChars)
    {
        slot.style.display = DisplayStyle.Flex;

        switch (pos)
        {
            case "left":
                slot.style.left = new Length(15, LengthUnit.Percent);
                slot.style.right = Length.Auto();
                break;
            case "center":
                slot.style.left = Length.Auto();
                slot.style.right = Length.Auto();
                slot.style.alignSelf = Align.Center;
                break;
            case "right":
                slot.style.left = Length.Auto();
                slot.style.right = new Length(15, LengthUnit.Percent);
                break;
            default:
                slot.style.left = Length.Auto();
                slot.style.right = Length.Auto();
                slot.style.alignSelf = Align.Center;
                break;
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