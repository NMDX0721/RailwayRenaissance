using System;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    /// <summary>
    /// 系统导航的细项偏好（站位接口）：设置页“系统导航”子页读写这里，导航本体（手势 / 三键）以后订阅 Changed 适配。
    /// 导航方式本身（手势 / 三键）仍由 TabletPresentationPreferences.NavigationMode 管，这里只管它的附加选项。
    /// 目前只保存偏好，尚未有界面消费；落地时：
    /// - HideIndicator：隐藏底部手势指示条（GameMainUI 的 navHint）。
    /// - GestureConfirm：游戏 / 视频等沉浸场景下，手势需重复一次才生效。
    /// - IgnoreMultiFinger：忽略三 / 四指手势。
    /// - MirrorButtons：三键模式下镜像返回键与菜单键。
    /// - BlurRecents：最近任务里模糊应用预览。
    /// - 快捷操作槽位：长按 / 双击指示条与三键的长按动作，取值为 Actions 的下标。
    /// </summary>
    public static class TabletNavigationOptions
    {
        public static readonly string[] Actions = { "无", "岁月 AI", "截屏", "手电筒" };

        public enum Slot
        {
            IndicatorLongPress,
            IndicatorDoubleTap,
            MenuLongPress,
            HomeLongPress,
            BackLongPress,
        }

        private static readonly int[] SlotDefaults = { 1, 0, 0, 1, 0 };

        public static event Action Changed;

        public static bool HideIndicator { get => GetBool("TabletNavHideIndicator", false); set => SetBool("TabletNavHideIndicator", false, value); }
        public static bool GestureConfirm { get => GetBool("TabletNavGestureConfirm", true); set => SetBool("TabletNavGestureConfirm", true, value); }
        public static bool IgnoreMultiFinger { get => GetBool("TabletNavIgnoreMultiFinger", false); set => SetBool("TabletNavIgnoreMultiFinger", false, value); }
        public static bool MirrorButtons { get => GetBool("TabletNavMirrorButtons", false); set => SetBool("TabletNavMirrorButtons", false, value); }
        public static bool BlurRecents { get => GetBool("TabletNavBlurRecents", true); set => SetBool("TabletNavBlurRecents", true, value); }

        public static int GetAction(Slot slot)
        {
            int v = PlayerPrefs.GetInt("TabletNavSlot" + slot, SlotDefaults[(int)slot]);
            return Mathf.Clamp(v, 0, Actions.Length - 1);
        }

        public static string GetActionName(Slot slot) => Actions[GetAction(slot)];

        public static void SetAction(Slot slot, int actionIndex)
        {
            actionIndex = Mathf.Clamp(actionIndex, 0, Actions.Length - 1);
            if (GetAction(slot) == actionIndex) return;
            PlayerPrefs.SetInt("TabletNavSlot" + slot, actionIndex);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>切到下一个动作并返回新的名字（设置页点一下轮换）。</summary>
        public static string CycleAction(Slot slot)
        {
            SetAction(slot, (GetAction(slot) + 1) % Actions.Length);
            return GetActionName(slot);
        }

        private static bool GetBool(string key, bool def) => PlayerPrefs.GetInt(key, def ? 1 : 0) == 1;

        private static void SetBool(string key, bool def, bool value)
        {
            if (GetBool(key, def) == value) return;   // 值没变
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
