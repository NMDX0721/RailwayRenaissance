using UnityEngine;
using UnityEngine.UIElements;

public static class UIToolkitUtil
{
    /// <summary>
    /// 修复 TextField 对比度：内部所有元素背景透明（干掉默认白色输入区），
    /// 文字用亮白金色大号字，整体深棕背景 + 金色边框。
    /// </summary>
    public static void StyleDarkTextField(TextField field, Font font, int fontSize = 20, bool center = true)
    {
        field.style.backgroundColor = new Color(0.16f, 0.1f, 0.05f, 0.95f);
        field.style.unityFontDefinition = new FontDefinition { font = font };
        field.style.borderTopLeftRadius = 6; field.style.borderTopRightRadius = 6;
        field.style.borderBottomLeftRadius = 6; field.style.borderBottomRightRadius = 6;
        field.style.borderTopWidth = 1; field.style.borderBottomWidth = 1;
        field.style.borderLeftWidth = 1; field.style.borderRightWidth = 1;
        field.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        field.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        field.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        field.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
        field.style.paddingLeft = 8; field.style.paddingRight = 8;

        // 布局后统一样式：透明内部背景 + 亮白金字
        field.RegisterCallback<GeometryChangedEvent>(e =>
        {
            foreach (var ve in field.Query<VisualElement>().ToList())
                ve.style.backgroundColor = Color.clear;
            foreach (var te in field.Query<TextElement>().ToList())
            {
                te.style.unityFontDefinition = new FontDefinition { font = font };
                te.style.fontSize = fontSize;
                te.style.color = new Color(1f, 0.97f, 0.85f, 1f); // 亮白金色，高对比
                te.style.unityTextAlign = center ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                te.style.overflow = Overflow.Visible;
                te.style.whiteSpace = WhiteSpace.Normal;
            }
            field.style.fontSize = fontSize;
            field.style.color = new Color(1f, 0.97f, 0.85f, 1f);
            field.style.backgroundColor = new Color(0.16f, 0.1f, 0.05f, 0.95f);
        });
    }
}