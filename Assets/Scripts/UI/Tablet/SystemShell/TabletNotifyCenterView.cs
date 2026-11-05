using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    /// <summary>通知中心列表：按应用分组，卡片逐张淡入上浮，可左右滑动清除，点击打开对应应用。</summary>
    public static class TabletNotifyCenterView
    {
        public static void Populate(ScrollView scroll, TabletShellState state, bool highContrast, Action closePanel)
        {
            var list = state.Notifications;
            if (list.Count == 0)
            {
                var empty = new Label("暂无通知");
                empty.style.marginTop = 24;
                empty.style.fontSize = TabletType.Title;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                empty.style.color = TabletTheme.TextStrong;
                empty.style.unityFontDefinition = Fd();
                empty.AddToClassList("shell-dark");
                scroll.Add(empty);
                return;
            }

            // 按应用分组，保持各组第一条通知出现的先后
            var order = new List<string>();
            var groups = new Dictionary<string, List<TabletNotification>>();
            for (int i = 0; i < list.Count; i++)
            {
                string key = TabletNotify.GetChannel(list[i].ChannelId).AppName;
                if (!groups.TryGetValue(key, out var g)) { g = new List<TabletNotification>(); groups[key] = g; order.Add(key); }
                g.Add(list[i]);
            }

            int index = 0;
            foreach (var key in order)
            {
                var items = groups[key];
                var ch = TabletNotify.GetChannel(items[0].ChannelId);

                var header = new VisualElement { pickingMode = PickingMode.Ignore };
                header.style.flexDirection = FlexDirection.Row;
                header.style.alignItems = Align.Center;
                header.style.marginLeft = 6; header.style.marginBottom = 6; header.style.marginTop = 4;
                header.Add(TabletNotifyCard.BuildAppBadge(ch, 18));
                var name = new Label(items.Count > 1 ? key + "  " + items.Count : key) { pickingMode = PickingMode.Ignore };
                name.style.fontSize = TabletType.Body;
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.color = Color.white;
                name.style.unityFontDefinition = Fd();
                name.style.marginLeft = 8; name.style.marginTop = 0; name.style.marginBottom = 0;
                header.Add(name);
                scroll.Add(header);

                foreach (var n in items)
                {
                    var item = n;
                    var wrap = new VisualElement();
                    wrap.style.marginBottom = 10;
                    var card = TabletNotifyCard.Build(item, TabletNotifyCardMode.Center, highContrast,
                        () => state.Dismiss(item.Id), null);
                    wrap.Add(card);
                    TabletNotifyCard.AttachSwipeDismiss(wrap, () => state.Dismiss(item.Id));
                    card.RegisterCallback<ClickEvent>(e =>
                    {
                        if (e.target is Button) return;
                        closePanel?.Invoke();
                        state.Dismiss(item.Id);
                        TabletNotify.Open(item);
                    });
                    scroll.Add(wrap);
                    AnimateIn(wrap, index++);
                }
            }
        }

        private static void AnimateIn(VisualElement e, int index)
        {
            int ms = TabletTheme.MotionMs(260);
            if (ms <= 0 || index > 8) return;
            e.style.opacity = 0f;
            e.style.translate = new Translate(0, 18);
            e.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity"), new StylePropertyName("translate") };
            e.style.transitionDuration = new List<TimeValue> { new TimeValue(ms, TimeUnit.Millisecond), new TimeValue(ms, TimeUnit.Millisecond) };
            e.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut), new EasingFunction(EasingMode.EaseOut) };
            e.schedule.Execute(() =>
            {
                e.style.opacity = 1f;
                e.style.translate = new Translate(0, 0);
            }).StartingIn(30 + index * 45);
            // 入场结束后恢复零时长，之后的滑动清除要跟手
            e.schedule.Execute(() =>
                e.style.transitionDuration = new List<TimeValue> { new TimeValue(0, TimeUnit.Millisecond), new TimeValue(0, TimeUnit.Millisecond) })
                .StartingIn(30 + index * 45 + ms + 40);
        }
    }
}
