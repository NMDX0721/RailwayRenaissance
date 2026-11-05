using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public enum TabletNotifyCardMode { Center, Banner }

    /// <summary>通知卡片的唯一模板：横幅和通知中心都用它，保证画风一致。</summary>
    public static class TabletNotifyCard
    {
        private static void Round(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;
        }

        /// <summary>应用小角标：圆角方块 + 矢量图标。</summary>
        public static VisualElement BuildAppBadge(TabletNotifyChannel ch, float size)
        {
            var badge = new VisualElement { pickingMode = PickingMode.Ignore };
            badge.style.width = size; badge.style.height = size;
            badge.style.flexShrink = 0;
            badge.style.alignItems = Align.Center; badge.style.justifyContent = Justify.Center;
            badge.style.backgroundColor = ch.AccentColor;
            Round(badge, size * 0.3f);
            badge.Add(new TabletIconElement(ch.Icon, size * 0.62f, Color.white) { pickingMode = PickingMode.Ignore });
            return badge;
        }

        public static Button BuildActionButton(TabletNotifyAction a, TabletNotifyChannel ch, Action afterClick)
        {
            var btn = new Button(() => { a.OnClick?.Invoke(); afterClick?.Invoke(); }) { text = a.Label };
            btn.AddToClassList("shell-keep");
            btn.style.height = 32;
            btn.style.paddingLeft = 14; btn.style.paddingRight = 14;
            btn.style.marginTop = 0; btn.style.marginBottom = 0; btn.style.marginLeft = 0; btn.style.marginRight = 8;
            btn.style.borderTopWidth = btn.style.borderBottomWidth = btn.style.borderLeftWidth = btn.style.borderRightWidth = 0;
            Round(btn, 16);
            btn.style.fontSize = TabletType.Small;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.unityFontDefinition = Fd();
            btn.style.backgroundColor = a.Primary ? ch.AccentColor : new Color(0f, 0f, 0f, 0.07f);
            btn.style.color = a.Primary ? Color.white : TabletTheme.InkOnGlass;
            return btn;
        }

        /// <summary>
        /// 构建卡片。dismiss 只在通知中心里使用（右上角小叉）；横幅靠上划收起。
        /// </summary>
        public static VisualElement Build(TabletNotification n, TabletNotifyCardMode mode, bool highContrast, Action dismiss, Action afterAction)
        {
            var ch = TabletNotify.GetChannel(n.ChannelId);
            bool banner = mode == TabletNotifyCardMode.Banner;

            var card = new VisualElement { name = banner ? "tablet-notify-banner-card" : "tablet-notify-card-" + n.Id };
            card.style.backgroundColor = highContrast ? Color.white : (banner ? new Color(1f, 1f, 1f, 0.96f) : TabletTheme.GlassCardLight);
            Round(card, banner ? 26 : 22);
            card.style.paddingTop = 12; card.style.paddingBottom = 14;
            card.style.paddingLeft = 16; card.style.paddingRight = 16;
            if (banner)
            {
                card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderLeftWidth = card.style.borderRightWidth = 1;
                var bc = new Color(0f, 0f, 0f, 0.08f);
                card.style.borderTopColor = card.style.borderBottomColor = card.style.borderLeftColor = card.style.borderRightColor = bc;
            }

            // 第一行：应用角标 + 应用名 · 时间 ……… 关闭
            var head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.style.flexDirection = FlexDirection.Row;
            head.style.alignItems = Align.Center;
            head.style.height = 24;
            head.Add(BuildAppBadge(ch, 20));

            var appName = new Label(ch.AppName) { pickingMode = PickingMode.Ignore };
            appName.style.fontSize = TabletType.Small;
            appName.style.color = TabletTheme.InkOnGlassSoft;
            appName.style.unityFontDefinition = Fd();
            appName.style.marginLeft = 8; appName.style.marginTop = 0; appName.style.marginBottom = 0;
            appName.AddToClassList("shell-dark");
            head.Add(appName);

            var time = new Label("·  " + TabletNotify.RelativeTime(n.CreatedAt)) { pickingMode = PickingMode.Ignore, name = "tablet-notify-time" };
            time.style.fontSize = TabletType.Small;
            time.style.color = TabletTheme.InkOnGlassSoft;
            time.style.unityFontDefinition = Fd();
            time.style.marginLeft = 6; time.style.marginTop = 0; time.style.marginBottom = 0;
            time.AddToClassList("shell-dark");
            head.Add(time);

            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.style.flexGrow = 1;
            head.Add(spacer);

            if (dismiss != null)
            {
                var x = new Button(dismiss) { name = "tablet-notification-dismiss-" + n.Id };
                x.AddToClassList("shell-keep");
                x.style.width = 44; x.style.height = 44;
                x.style.marginTop = -10; x.style.marginBottom = -10; x.style.marginRight = -14; x.style.marginLeft = 0;
                x.style.backgroundColor = Color.clear;
                x.style.borderTopWidth = x.style.borderBottomWidth = x.style.borderLeftWidth = x.style.borderRightWidth = 0;
                SetButtonIcon(x, TabletIconKind.Close, 14, TabletTheme.InkOnGlassSoft);
                head.Add(x);
            }
            card.Add(head);

            // 标题 + 正文
            var title = new Label(n.Title) { pickingMode = PickingMode.Ignore, name = "tablet-notify-title" };
            title.style.fontSize = TabletType.Title;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = TabletTheme.InkOnGlass;
            title.style.unityFontDefinition = Fd();
            title.style.whiteSpace = WhiteSpace.Normal;
            title.style.marginLeft = 0; title.style.marginRight = 0; title.style.marginTop = 6; title.style.marginBottom = 0;
            title.style.paddingLeft = 0; title.style.paddingRight = 0;
            title.AddToClassList("shell-dark");
            card.Add(title);

            if (!string.IsNullOrEmpty(n.Body))
            {
                var body = new Label(n.Body) { pickingMode = PickingMode.Ignore, name = "tablet-notify-body" };
                body.style.fontSize = TabletType.Body;
                body.style.color = TabletTheme.InkOnGlassSoft;
                body.style.unityFontDefinition = Fd();
                body.style.whiteSpace = WhiteSpace.Normal;
                body.style.marginLeft = 0; body.style.marginRight = 0; body.style.marginTop = 2; body.style.marginBottom = 0;
                body.style.paddingLeft = 0; body.style.paddingRight = 0;
                body.style.maxHeight = banner ? 38 : 56;
                body.style.overflow = Overflow.Hidden;
                body.AddToClassList("shell-dark");
                card.Add(body);
            }

            if (n.Progress >= 0f)
            {
                var bar = BuildProgressBar(n.Progress, ch.AccentColor, 4f, new Color(0f, 0f, 0f, 0.08f));
                bar.style.marginTop = 8;
                bar.pickingMode = PickingMode.Ignore;
                card.Add(bar);
            }

            if (n.Request != null && n.Request.Actions.Count > 0)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginTop = 10;
                foreach (var a in n.Request.Actions) row.Add(BuildActionButton(a, ch, afterAction));
                card.Add(row);
            }
            return card;
        }

        /// <summary>
        /// 水平滑动清除：拖过卡片宽度约 35% 松手即移出并回调；否则弹回。纵向滚动不受影响（只在横向位移占优时接管）。
        /// </summary>
        public static void AttachSwipeDismiss(VisualElement card, Action onDismissed)
        {
            bool down = false, dragging = false;
            Vector2 start = default;
            card.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("translate"), new StylePropertyName("opacity") };
            card.style.transitionDuration = new List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond) };

            card.RegisterCallback<PointerDownEvent>(e => { down = true; dragging = false; start = e.position; });
            card.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!down) return;
                float dx = e.position.x - start.x, dy = e.position.y - start.y;
                if (!dragging)
                {
                    if (Mathf.Abs(dx) > 10f && Mathf.Abs(dx) > Mathf.Abs(dy) * 1.5f)
                    {
                        dragging = true;
                        card.CapturePointer(e.pointerId);
                    }
                    else return;
                }
                float w = Mathf.Max(1f, card.resolvedStyle.width);
                card.style.translate = new Translate(dx, 0);
                card.style.opacity = Mathf.Clamp01(1f - Mathf.Abs(dx) / (w * 0.9f));
            });
            card.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!down) return;
                down = false;
                if (!dragging) return;
                dragging = false;
                if (card.HasPointerCapture(e.pointerId)) card.ReleasePointer(e.pointerId);
                float w = Mathf.Max(1f, card.resolvedStyle.width);
                float dx = e.position.x - start.x;
                int ms = TabletTheme.MotionMs(180);
                card.style.transitionDuration = new List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond) };
                if (Mathf.Abs(dx) > w * 0.35f)
                {
                    card.style.translate = new Translate(Mathf.Sign(dx) * w, 0);
                    card.style.opacity = 0f;
                    card.schedule.Execute(() => onDismissed?.Invoke()).StartingIn(ms + 20);
                }
                else
                {
                    card.style.translate = new Translate(0, 0);
                    card.style.opacity = 1f;
                }
            });
            // 拖动结束后紧跟的 ClickEvent 不应触发打开
            card.RegisterCallback<ClickEvent>(e => { if (dragging) e.StopImmediatePropagation(); }, TrickleDown.TrickleDown);
        }
    }
}
