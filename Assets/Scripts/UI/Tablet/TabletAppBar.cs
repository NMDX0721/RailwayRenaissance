using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet
{
    /// <summary>应用顶栏的参数。所有平板应用共用同一个顶栏，不再各自摆“返回/退出”按钮。</summary>
    public sealed class TabletAppBarSpec
    {
        public string Title;
        public string Subtitle;
        public bool Dark;
        /// <summary>左上角圆形返回键的动作（子页面返回上一级，首页回桌面）。</summary>
        public Action OnBack;
        /// <summary>右侧操作图标（最多 3 个）。</summary>
        public readonly List<(TabletIconKind icon, string tooltip, Action onClick)> Actions = new List<(TabletIconKind, string, Action)>();

        public TabletAppBarSpec Action(TabletIconKind icon, string tooltip, Action onClick)
        {
            if (Actions.Count < 3) Actions.Add((icon, tooltip, onClick));
            return this;
        }
    }

    public static class TabletAppBar
    {
        public const float Height = 56f;

        public static VisualElement Build(TabletAppBarSpec spec)
        {
            var bg = spec.Dark ? new Color(0.08f, 0.09f, 0.11f, 1f) : new Color(0.97f, 0.97f, 0.98f, 1f);
            var chip = spec.Dark ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.06f);
            var chipHover = spec.Dark ? new Color(1f, 1f, 1f, 0.15f) : new Color(0f, 0f, 0f, 0.11f);
            var ink = spec.Dark ? new Color(0.96f, 0.94f, 0.88f, 1f) : new Color(0.10f, 0.10f, 0.12f, 1f);
            var sub = spec.Dark ? new Color(0.83f, 0.66f, 0.26f, 0.85f) : new Color(0.45f, 0.45f, 0.50f, 1f);

            var bar = new VisualElement { name = "tablet-app-bar" };
            bar.style.height = Height;
            bar.style.flexShrink = 0;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.paddingLeft = 14; bar.style.paddingRight = 14;
            bar.style.backgroundColor = bg;

            var back = IconButton(TabletIconKind.Back, 22, ink, chip, chipHover, spec.OnBack);
            back.name = "tablet-app-back";
            back.tooltip = "返回";
            bar.Add(back);

            var title = new Label(spec.Title) { name = "tablet-app-title" };
            title.style.fontSize = TabletType.AppTitle;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = ink;
            title.style.unityFontDefinition = Fd();
            title.style.marginLeft = 12; title.style.marginTop = 0; title.style.marginBottom = 0;
            bar.Add(title);

            if (!string.IsNullOrEmpty(spec.Subtitle))
            {
                var s = new Label(spec.Subtitle);
                s.style.fontSize = TabletType.Small;
                s.style.color = sub;
                s.style.unityFontDefinition = Fd();
                s.style.marginLeft = 10; s.style.marginTop = 3; s.style.marginBottom = 0;
                s.style.flexShrink = 1;
                s.style.overflow = Overflow.Hidden;
                s.style.whiteSpace = WhiteSpace.NoWrap;
                bar.Add(s);
            }

            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.style.flexGrow = 1;
            bar.Add(spacer);

            for (int i = 0; i < spec.Actions.Count; i++)
            {
                var a = spec.Actions[i];
                var b = IconButton(a.icon, 20, ink, chip, chipHover, a.onClick);
                b.tooltip = a.tooltip;
                b.style.marginLeft = 8;
                bar.Add(b);
            }
            return bar;
        }

        /// <summary>圆形图标按钮：按下轻微缩小，悬停加深底色。</summary>
        public static Button IconButton(TabletIconKind kind, float iconSize, Color ink, Color bg, Color hover, Action onClick)
        {
            var btn = new Button(() => onClick?.Invoke());
            btn.style.width = 40; btn.style.height = 40;
            btn.style.flexShrink = 0;
            btn.style.paddingLeft = btn.style.paddingRight = btn.style.paddingTop = btn.style.paddingBottom = 0;
            btn.style.marginLeft = btn.style.marginRight = btn.style.marginTop = btn.style.marginBottom = 0;
            btn.style.borderTopWidth = btn.style.borderBottomWidth = btn.style.borderLeftWidth = btn.style.borderRightWidth = 0;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 20;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 20;
            btn.style.backgroundColor = bg;
            btn.style.alignItems = Align.Center; btn.style.justifyContent = Justify.Center;
            btn.Add(new TabletIconElement(kind, iconSize, ink) { pickingMode = PickingMode.Ignore });
            btn.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("scale"), new StylePropertyName("background-color") };
            btn.style.transitionDuration = new List<TimeValue> { new TimeValue(TabletTheme.MotionMs(120), TimeUnit.Millisecond), new TimeValue(TabletTheme.MotionMs(120), TimeUnit.Millisecond) };
            btn.RegisterCallback<PointerEnterEvent>(_ => btn.style.backgroundColor = hover);
            btn.RegisterCallback<PointerLeaveEvent>(_ => { btn.style.backgroundColor = bg; btn.style.scale = new Scale(Vector3.one); });
            btn.RegisterCallback<PointerDownEvent>(_ => btn.style.scale = new Scale(new Vector3(0.9f, 0.9f, 1f)));
            btn.RegisterCallback<PointerUpEvent>(_ => btn.style.scale = new Scale(Vector3.one));
            return btn;
        }
    }
}
