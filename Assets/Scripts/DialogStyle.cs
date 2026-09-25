using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 全局确认类弹窗统一壳：暖深棕底 + 2px 金边 + 圆角16 + 金标题下划线 + 双按钮。
/// 跳过 / 退出 / 恢复观看 / 删档 / 恢复默认设置 共用。
/// </summary>
public static class DialogStyle
{
    public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.65f);
    public static readonly Color BoxBg = new Color(0.16f, 0.11f, 0.07f, 0.96f);
    public static readonly Color GoldBorder = new Color(0.8f, 0.62f, 0.35f, 0.7f);
    public static readonly Color TitleGold = new Color(1f, 0.78f, 0.4f, 1f);
    public static readonly Color Underline = new Color(1f, 0.78f, 0.4f, 0.4f);
    public static readonly Color BodyText = new Color(1f, 1f, 1f, 0.55f);
    public static readonly Color CancelBg = new Color(0.2f, 0.14f, 0.08f, 0.9f);
    public static readonly Color CancelFg = new Color(1f, 0.85f, 0.6f, 0.9f);
    public static readonly Color PrimaryBg = new Color(1f, 0.78f, 0.4f, 0.95f);
    public static readonly Color PrimaryFg = new Color(0.12f, 0.08f, 0.04f, 1f);

    /// <summary>全屏遮罩层（已关 display）。</summary>
    public static VisualElement CreateOverlay(FontDefinition fontDef)
    {
        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0;
        overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = Overlay;
        overlay.style.display = DisplayStyle.None;
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        overlay.pickingMode = PickingMode.Position;
        return overlay;
    }

    /// <summary>标准弹窗盒。</summary>
    public static VisualElement CreateBox(float width = 480f)
    {
        var box = new VisualElement();
        box.style.width = width;
        box.style.alignItems = Align.Center;
        box.style.justifyContent = Justify.Center;
        box.style.paddingTop = 36;
        box.style.paddingBottom = 30;
        box.style.paddingLeft = 40;
        box.style.paddingRight = 40;
        box.style.borderTopLeftRadius = 16;
        box.style.borderTopRightRadius = 16;
        box.style.borderBottomLeftRadius = 16;
        box.style.borderBottomRightRadius = 16;
        box.style.backgroundColor = BoxBg;
        box.style.borderTopWidth = 2; box.style.borderBottomWidth = 2;
        box.style.borderLeftWidth = 2; box.style.borderRightWidth = 2;
        box.style.borderTopColor = GoldBorder; box.style.borderBottomColor = GoldBorder;
        box.style.borderLeftColor = GoldBorder; box.style.borderRightColor = GoldBorder;
        return box;
    }

    public static Label CreateTitle(string text, FontDefinition fontDef)
    {
        var title = new Label(text);
        title.style.fontSize = 28;
        title.style.color = TitleGold;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = fontDef;
        title.style.marginBottom = 6;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        return title;
    }

    public static VisualElement CreateUnderline()
    {
        var line = new VisualElement();
        line.style.width = 80;
        line.style.height = 2;
        line.style.backgroundColor = Underline;
        line.style.marginBottom = 18;
        line.style.alignSelf = Align.Center;
        return line;
    }

    public static Label CreateBody(string text, FontDefinition fontDef)
    {
        var body = new Label(text);
        body.style.fontSize = 16;
        body.style.color = BodyText;
        body.style.unityFontDefinition = fontDef;
        body.style.whiteSpace = WhiteSpace.Normal;
        body.style.unityTextAlign = TextAnchor.MiddleCenter;
        return body;
    }

    /// <summary>标准双按钮行：左取消、右主操作。返回 (cancel, primary)。</summary>
    public static (UnityEngine.UIElements.Button cancel, UnityEngine.UIElements.Button primary)
        AddButtonRow(VisualElement box, FontDefinition fontDef,
            string cancelText, string primaryText,
            System.Action onCancel, System.Action onPrimary)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.Center;
        row.style.alignItems = Align.Center;
        box.Add(row);

        var cancel = MakeButton(cancelText, CancelBg, CancelFg, fontDef, onCancel);
        cancel.style.marginRight = 20;
        row.Add(cancel);

        var primary = MakeButton(primaryText, PrimaryBg, PrimaryFg, fontDef, onPrimary);
        row.Add(primary);
        return (cancel, primary);
    }

    public static UnityEngine.UIElements.Button MakeButton(
        string text, Color bg, Color fg, FontDefinition fontDef, System.Action onClick)
    {
        var b = new UnityEngine.UIElements.Button(() => onClick?.Invoke()) { text = text };
        b.focusable = false;
        b.style.width = 140;
        b.style.height = 48;
        b.style.fontSize = 18;
        b.style.color = fg;
        b.style.backgroundColor = bg;
        b.style.unityTextAlign = TextAnchor.MiddleCenter;
        b.style.unityFontDefinition = fontDef;
        b.style.borderTopLeftRadius = 8; b.style.borderTopRightRadius = 8;
        b.style.borderBottomLeftRadius = 8; b.style.borderBottomRightRadius = 8;
        b.style.borderTopWidth = 1; b.style.borderBottomWidth = 1;
        b.style.borderLeftWidth = 1; b.style.borderRightWidth = 1;
        b.style.borderTopColor = GoldBorder; b.style.borderBottomColor = GoldBorder;
        b.style.borderLeftColor = GoldBorder; b.style.borderRightColor = GoldBorder;
        b.RegisterCallback<PointerDownEvent>(e => e.StopImmediatePropagation());
        b.RegisterCallback<ClickEvent>(e => e.StopImmediatePropagation());
        return b;
    }
}
