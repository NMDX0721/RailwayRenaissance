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

            // 对照 HyperOS：不分组，直接一张张卡片（卡片自己带应用图标）；超过 4 张的折进“更多通知”一行，点开再展开
            const int collapsedLimit = 4;
            bool collapse = list.Count > collapsedLimit && !s_expandedMore;
            int shown = collapse ? collapsedLimit : list.Count;
            int index = 0;
            for (int i = 0; i < shown; i++)
            {
                var item = list[i];
                var wrap = new VisualElement();
                wrap.AddToClassList("shell-no-pager");   // 卡片上的横滑是“滑动清除”，不当作切页
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

            if (collapse) scroll.Add(BuildMoreRow(list, collapsedLimit, state));
        }

        private static bool s_expandedMore;
        /// <summary>面板每次重新打开时回到折叠状态。</summary>
        public static void ResetExpansion() { s_expandedMore = false; }

        private static VisualElement BuildMoreRow(IReadOnlyList<TabletNotification> list, int from, TabletShellState state)
        {
            var row = new Button(() => { s_expandedMore = true; state.NotifyChanged(); }) { name = "tablet-notifications-more" };
            row.AddToClassList("shell-keep");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 60;
            row.style.marginLeft = 0; row.style.marginRight = 0; row.style.marginTop = 0; row.style.marginBottom = 10;
            row.style.paddingLeft = 20; row.style.paddingRight = 16;
            row.style.backgroundColor = TabletTheme.GlassCardLight;
            row.style.borderTopWidth = row.style.borderBottomWidth = row.style.borderLeftWidth = row.style.borderRightWidth = 0;
            row.style.borderTopLeftRadius = row.style.borderTopRightRadius = 24;
            row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 24;
            var label = new Label("更多通知") { pickingMode = PickingMode.Ignore };
            label.style.fontSize = TabletType.Title;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = TabletTheme.InkOnGlass;
            label.style.unityFontDefinition = Fd();
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.AddToClassList("shell-dark");
            row.Add(label);

            // 折起来的通知来自哪些应用：最多三枚小图标
            var seen = new List<string>();
            for (int i = from; i < list.Count && seen.Count < 3; i++)
            {
                var ch = TabletNotify.GetChannel(list[i].ChannelId);
                if (seen.Contains(ch.AppName)) continue;
                seen.Add(ch.AppName);
                var badge = TabletNotifyCard.BuildAppBadge(ch, 30);
                badge.style.marginLeft = 8;
                row.Add(badge);
            }
            var chevron = new Label("›") { pickingMode = PickingMode.Ignore };
            chevron.style.fontSize = 22; chevron.style.color = new Color(0.55f, 0.55f, 0.60f, 1f);
            chevron.style.marginLeft = 10; chevron.style.marginTop = 0; chevron.style.marginBottom = 0;
            chevron.style.unityFontDefinition = Fd();
            row.Add(chevron);
            return row;
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
