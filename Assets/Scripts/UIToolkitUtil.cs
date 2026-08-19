using UnityEngine;
using UnityEngine.UIElements;

public static class UIToolkitUtil
{
    /// <summary>
    /// 修复 TextField：单边框（内部 TextInput 统一）+ 透明内部 + 亮白金字。
    /// 解决"双边框（一方一圆弧）"和"文字底部裁切"问题。
    /// </summary>
    public static void StyleDarkTextField(TextField field, Font font, int fontSize = 20, bool center = true)
    {
        // 根元素：深棕底，去圆角边框（避免与内部 TextInput 边框叠加成双边框）
        field.style.backgroundColor = new Color(0.16f, 0.1f, 0.05f, 0.95f);
        field.style.borderTopWidth = 0; field.style.borderBottomWidth = 0;
        field.style.borderLeftWidth = 0; field.style.borderRightWidth = 0;
        field.style.borderTopLeftRadius = 0; field.style.borderTopRightRadius = 0;
        field.style.borderBottomLeftRadius = 0; field.style.borderBottomRightRadius = 0;
        field.style.paddingLeft = 0; field.style.paddingRight = 0;
        field.style.paddingTop = 0; field.style.paddingBottom = 0;

        // 布局后：内部 TextInput 统一样式（单一边框 + 高度 + 无裁剪）
        field.RegisterCallback<GeometryChangedEvent>(e =>
        {
            foreach (var ve in field.Query<VisualElement>().ToList())
            {
                // 所有内部元素透明背景
                ve.style.backgroundColor = Color.clear;
            }

            // 找到实际输入容器（TextInput 类有 unity-text-field__input / unity-base-text-field__input）
            var inputs = field.Query<VisualElement>().
                Where(ve => ve.ClassListContains("unity-text-field__input")
                         || ve.ClassListContains("unity-base-text-field__input")
                         || ve.ClassListContains("unity-base-text-field__text-input"))
                .ToList();

            foreach (var inp in inputs)
            {
                // 单一圆角边框（不再叠加根边框）
                inp.style.borderTopWidth = 1; inp.style.borderBottomWidth = 1;
                inp.style.borderLeftWidth = 1; inp.style.borderRightWidth = 1;
                inp.style.borderTopColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
                inp.style.borderBottomColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
                inp.style.borderLeftColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
                inp.style.borderRightColor = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
                inp.style.borderTopLeftRadius = 6; inp.style.borderTopRightRadius = 6;
                inp.style.borderBottomLeftRadius = 6; inp.style.borderBottomRightRadius = 6;
                inp.style.borderTopRightRadius = 6; inp.style.borderBottomRightRadius = 6;
                // 高度与根一致 + 不透裁剪
                inp.style.height = new Length(100, LengthUnit.Percent);
                inp.style.minHeight = 34;
                inp.style.overflow = Overflow.Visible;
                inp.style.paddingTop = 4; inp.style.paddingBottom = 4;
            }

            foreach (var te in field.Query<TextElement>().ToList())
            {
                te.style.unityFontDefinition = new FontDefinition { font = font };
                te.style.fontSize = fontSize;
                te.style.color = new Color(1f, 0.97f, 0.85f, 1f);
                te.style.unityTextAlign = center ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                te.style.overflow = Overflow.Visible;
                te.style.whiteSpace = WhiteSpace.Normal;
                // 防底部裁切：给文字轻微上移 + 行高
                te.style.paddingTop = 0; te.style.paddingBottom = 2;
            }

            field.style.fontSize = fontSize;
            field.style.color = new Color(1f, 0.97f, 0.85f, 1f);
            field.style.backgroundColor = new Color(0.16f, 0.1f, 0.05f, 0.95f);
        });
    }
}