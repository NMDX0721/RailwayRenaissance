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

    public void UpdateDisplay(CharacterEntry[] chars, string emotion)
    {
        if (chars == null || chars.Length == 0)
            return;

        ClearAll();

        foreach (var entry in chars)
        {
            if (string.IsNullOrEmpty(entry.name)) continue;

            // 未指定表情时使用角色名直接加载，否则 {name}_{emotion}
            string cacheKey = string.IsNullOrEmpty(emotion) ? entry.name : entry.name + "_" + emotion;

            if (!spriteCache.TryGetValue(cacheKey, out var tex))
            {
                tex = Resources.Load<Texture2D>("characters/" + cacheKey);
                if (tex != null) spriteCache[cacheKey] = tex;
            }

            if (tex == null)
            {
                Debug.LogWarning("[VN Characters] Sprite not found: characters/" + cacheKey);
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
        ClearSlot(slotLeft);
        ClearSlot(slotCenter);
        ClearSlot(slotRight);
    }

    private void ClearSlot(VisualElement slot)
    {
        slot.style.display = DisplayStyle.None;
        slot.style.backgroundImage = StyleKeyword.Null;
        slot.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0);
        slot.style.alignSelf = Align.FlexEnd;
    }
}