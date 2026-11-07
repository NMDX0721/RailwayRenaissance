using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    /// <summary>
    /// 系统级确认弹窗（白色圆角卡片 + 取消 / 确认）：盖住整个平板屏幕，点遮罩或再按 Esc 即取消，回车确认。
    /// 同一时间只有一个，重复调用返回已有的那个。
    /// </summary>
    public static class TabletConfirmDialog
    {
        private static VisualElement s_active;
        private static Action s_cancel;

        public static bool IsOpen => s_active != null && s_active.parent != null;

        /// <summary>Esc 等“返回”操作：弹窗开着就取消它，返回是否消耗了这次返回。</summary>
        public static bool HandleBack()
        {
            if (!IsOpen) return false;
            s_cancel?.Invoke();
            return true;
        }

        public static VisualElement Show(VisualElement host, string title, string message, string confirmLabel, Action onConfirm, Action onCancel = null)
        {
            if (IsOpen) return s_active;
            VisualElement root = host;
            while (root != null && root.parent != null && root.parent != root.panel?.visualTree) root = root.parent;
            if (root == null) return null;

            var font = TabletFonts.Ui();
            var fontBold = TabletFonts.UiBold();
            bool closed = false;

            var overlay = new VisualElement { name = "tablet-confirm-overlay" };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.top = 0; overlay.style.right = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.42f);
            overlay.style.alignItems = Align.Center; overlay.style.justifyContent = Justify.Center;
            overlay.pickingMode = PickingMode.Position;
            overlay.focusable = true;
            overlay.style.opacity = 0f;
            overlay.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
            overlay.style.transitionDuration = new List<TimeValue> { new TimeValue(TabletTheme.MotionMs(150), TimeUnit.Millisecond) };

            var card = new VisualElement { name = "tablet-confirm-window" };
            card.style.width = 360;
            card.style.backgroundColor = Color.white;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 28;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 28;
            card.style.paddingLeft = 28; card.style.paddingRight = 28; card.style.paddingTop = 30; card.style.paddingBottom = 22;
            card.style.scale = new Scale(new Vector3(0.94f, 0.94f, 1f));
            card.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("scale") };
            card.style.transitionDuration = new List<TimeValue> { new TimeValue(TabletTheme.MotionMs(190), TimeUnit.Millisecond) };
            card.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
            overlay.Add(card);

            void Close(bool ok)
            {
                if (closed) return;
                closed = true;
                s_active = null; s_cancel = null;
                overlay.style.opacity = 0f;
                card.style.scale = new Scale(new Vector3(0.96f, 0.96f, 1f));
                overlay.schedule.Execute(() => overlay.RemoveFromHierarchy()).StartingIn(Mathf.Max(30, TabletTheme.MotionMs(160)));
                if (ok) onConfirm?.Invoke(); else onCancel?.Invoke();
            }

            Label Text(string s, int size, bool bold, Color color, TextAnchor align)
            {
                var l = new Label(s) { pickingMode = PickingMode.Ignore };
                l.style.fontSize = size; l.style.color = color; l.style.unityTextAlign = align;
                l.style.whiteSpace = WhiteSpace.Normal;
                l.style.marginLeft = 0; l.style.marginRight = 0; l.style.marginTop = 0; l.style.marginBottom = 0;
                l.style.paddingLeft = 0; l.style.paddingRight = 0; l.style.paddingTop = 0; l.style.paddingBottom = 0;
                var f = bold ? fontBold : font;
                if (f != null) l.style.unityFontDefinition = FontDefinition.FromFont(f);
                return l;
            }

            card.Add(Text(title, 22, true, new Color(0.12f, 0.12f, 0.14f, 1f), TextAnchor.MiddleCenter));
            var msg = Text(message, 15, false, new Color(0.40f, 0.42f, 0.46f, 1f), TextAnchor.MiddleCenter);
            msg.style.marginTop = 10; msg.style.marginBottom = 26;
            card.Add(msg);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            Button Make(string label, bool primary, bool ok, string name)
            {
                var b = new Button(() => Close(ok)) { text = label, name = name };
                b.style.flexGrow = 1; b.style.height = 48;
                b.style.marginLeft = ok ? 10 : 0; b.style.marginRight = 0; b.style.marginTop = 0; b.style.marginBottom = 0;
                b.style.paddingLeft = b.style.paddingRight = b.style.paddingTop = b.style.paddingBottom = 0;
                b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
                b.style.borderTopLeftRadius = b.style.borderTopRightRadius = b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = 24;
                b.style.backgroundColor = primary ? new Color(0.93f, 0.27f, 0.25f, 1f) : new Color(0.93f, 0.94f, 0.95f, 1f);
                b.style.color = primary ? Color.white : new Color(0.12f, 0.12f, 0.14f, 1f);
                b.style.fontSize = 16;
                if (fontBold != null) b.style.unityFontDefinition = FontDefinition.FromFont(fontBold);
                return b;
            }
            row.Add(Make("取消", false, false, "tablet-confirm-cancel"));
            row.Add(Make(confirmLabel, true, true, "tablet-confirm-ok"));
            card.Add(row);

            overlay.RegisterCallback<ClickEvent>(e => { if (e.target == overlay) Close(false); });
            overlay.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape) { Close(false); e.StopPropagation(); }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { Close(true); e.StopPropagation(); }
            });

            s_active = overlay;
            s_cancel = () => Close(false);
            root.Add(overlay);
            overlay.BringToFront();
            overlay.schedule.Execute(() => { overlay.style.opacity = 1f; card.style.scale = new Scale(Vector3.one); }).StartingIn(16);
            return overlay;
        }
    }
}
