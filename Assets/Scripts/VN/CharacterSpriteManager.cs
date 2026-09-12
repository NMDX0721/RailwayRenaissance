using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CharacterSpriteManager : MonoBehaviour
{
    private VisualElement container;
    private VisualElement slotLeft;
    private VisualElement slotCenter;
    private VisualElement slotRight;
    private readonly Dictionary<string, Texture2D> spriteCache = new Dictionary<string, Texture2D>();
    // 每个角色独立追踪当前表情，旁观者保持上一次的表情
    private readonly Dictionary<string, string> lastEmotion = new Dictionary<string, string>();

    public void Init(UIDocument document)
    {
        var root = document.rootVisualElement;

        container = new VisualElement { name = "character-container" };
        container.style.position = Position.Absolute;
        container.style.top = 0;
        container.style.left = 0;
        container.style.right = 0;
        container.style.bottom = 180;
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

    public void UpdateDisplay(CharacterEntry[] chars, string emotion, string speaker = null)
    {
        if (chars == null || chars.Length == 0)
            return;

        // 不调用 ClearAll()，保留旁观者的现有表情
        foreach (var entry in chars)
        {
            if (string.IsNullOrEmpty(entry.name)) continue;

            bool isSpeaker = string.IsNullOrEmpty(speaker) || entry.name == speaker;
            string emotionKey;

            if (isSpeaker && !string.IsNullOrEmpty(emotion))
            {
                // 说话者：应用指定表情
                emotionKey = emotion;
                lastEmotion[entry.name] = emotion; // 记住
            }
            else
            {
                // 旁观者：保持上一次的表情，如果没有则用 normal
                lastEmotion.TryGetValue(entry.name, out emotionKey);
                if (string.IsNullOrEmpty(emotionKey)) emotionKey = "normal";
            }
            Texture2D tex = null;

            // 尝试加载精灵：flat格式 → 子目录格式 → fallback normal → fallback normal子目录
            string[] tryPaths = {
                "characters/" + entry.name + "_" + emotionKey,         // flat: characters/suiyue_normal
                "characters/" + entry.name + "/" + emotionKey,          // subdir: characters/suiyue/normal
            };
            if (emotionKey != "normal")
            {
                tryPaths = new[] {
                    "characters/" + entry.name + "_" + emotionKey,
                    "characters/" + entry.name + "/" + emotionKey,
                    "characters/" + entry.name + "_normal",
                    "characters/" + entry.name + "/normal",
                };
            }

            foreach (var path in tryPaths)
            {
                if (spriteCache.TryGetValue(path, out tex) && tex != null) break;
                tex = Resources.Load<Texture2D>(path);
                if (tex != null) { spriteCache[path] = tex; break; }
                tex = null;
            }

            if (tex == null)
            {
                Debug.LogWarning("[VN Characters] Sprite not found: " + entry.name + " / " + emotionKey);
                continue;
            }

            var slot = GetSlotForPosition(entry.pos);
            if (slot == null) continue;

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
        lastEmotion.Clear(); // 场景切换时重置所有角色表情
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