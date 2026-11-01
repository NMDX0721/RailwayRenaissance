using System;
using UnityEngine;
using RailwayRenaissance.UI.Tablet.SystemShell;

namespace RailwayRenaissance.UI.Tablet
{
    /// <summary>
    /// 平板主题令牌：深色玻璃面（系统壳、超级岛）的语义色阶，以及随外观偏好变化的强调色、高对比度和减少动效状态。
    /// 浅色卡片体系的令牌仍在 <see cref="TabletUIHelper"/>；强调色通过 <see cref="Accent"/> / <see cref="AccentOnLight"/> 两处统一出口。
    /// </summary>
    public static class TabletTheme
    {
        // ===== 运行时外观状态（由 GameMainUI 在偏好变化时调用 Apply 写入） =====
        public static TabletAccent AccentKind { get; private set; } = TabletAccent.Amber;
        public static bool HighContrast { get; private set; }
        public static bool ReduceMotion { get; private set; }

        /// <summary>深色面上的强调色。</summary>
        public static Color Accent => AccentFor(AccentKind);

        /// <summary>浅色面上的强调色（对比度更高的同色相）。</summary>
        public static Color AccentOnLight => AccentOnLightFor(AccentKind);

        /// <summary>设备安全区的顶部缺口高度（刘海/挖孔/系统状态栏），仅在 Android/iOS 上可能大于 0。</summary>
        public static float SafeInsetTop;

        public static event Action Changed;

        public static void Apply(TabletAccent accent, bool highContrast, bool reduceMotion)
        {
            if (AccentKind == accent && HighContrast == highContrast && ReduceMotion == reduceMotion) return;
            AccentKind = accent;
            HighContrast = highContrast;
            ReduceMotion = reduceMotion;
            Changed?.Invoke();
        }

        public static Color AccentFor(TabletAccent accent)
        {
            switch (accent)
            {
                case TabletAccent.Teal: return new Color(0.19f, 0.66f, 0.57f, 1f);
                case TabletAccent.Blue: return new Color(0.32f, 0.60f, 0.92f, 1f);
                default: return new Color(0.86f, 0.65f, 0.27f, 1f);
            }
        }

        public static Color AccentOnLightFor(TabletAccent accent)
        {
            switch (accent)
            {
                case TabletAccent.Teal: return new Color(0.13f, 0.52f, 0.45f, 1f);
                case TabletAccent.Blue: return new Color(0.22f, 0.45f, 0.80f, 1f);
                default: return new Color(0.72f, 0.52f, 0.23f, 1f);
            }
        }

        /// <summary>动画时长（毫秒）；开启减少动效时返回 0。</summary>
        public static int MotionMs(int ms) => ReduceMotion ? 0 : ms;

        // ===== 深色面：表面 =====
        public static readonly Color IslandBlack = new Color(0f, 0f, 0f, 1f);
        public static readonly Color IslandPill = new Color(0.06f, 0.07f, 0.09f, 0.94f);
        public static readonly Color IslandExpanded = new Color(0.08f, 0.09f, 0.12f, 0.96f);
        public static readonly Color IslandScrim = new Color(0f, 0f, 0f, 0.25f);
        public static readonly Color DiscFill = new Color(0.12f, 0.12f, 0.14f, 1f);
        public static readonly Color DiscRim = new Color(0.30f, 0.30f, 0.35f, 0.8f);
        public static readonly Color VinylFill = new Color(0.15f, 0.15f, 0.18f, 1f);

        public static readonly Color ShellPanel = new Color(0.13f, 0.14f, 0.16f, 0.98f);
        public static readonly Color ShellCard = new Color(0.18f, 0.19f, 0.22f, 1f);
        public static readonly Color ShellCardDeep = new Color(0.12f, 0.13f, 0.16f, 0.95f);
        public static readonly Color ShellBorder = new Color(0.35f, 0.36f, 0.40f, 1f);
        public static readonly Color ShellScrim = new Color(0.05f, 0.05f, 0.07f, 0.28f);
        public static readonly Color ShellScrimHighContrast = new Color(0f, 0f, 0f, 0.85f);

        // ===== 模糊背景上的玻璃卡片（通知页 / 控制中心） =====
        public static readonly Color GlassTile = new Color(0.10f, 0.09f, 0.10f, 0.42f);
        public static readonly Color GlassTileActive = new Color(1f, 1f, 1f, 0.94f);
        public static readonly Color GlassCardLight = new Color(1f, 1f, 1f, 0.82f);
        public static readonly Color InkOnGlass = new Color(0.10f, 0.10f, 0.12f, 1f);
        public static readonly Color InkOnGlassSoft = new Color(0.34f, 0.34f, 0.40f, 1f);

        // ===== 深色面：白色叠层阶梯（填充与描边共用） =====
        public static readonly Color OverlaySubtle = new Color(1f, 1f, 1f, 0.06f);
        public static readonly Color OverlayFaint = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color OverlaySoft = new Color(1f, 1f, 1f, 0.12f);
        public static readonly Color OverlayMedium = new Color(1f, 1f, 1f, 0.16f);
        public static readonly Color OverlayStrong = new Color(1f, 1f, 1f, 0.20f);

        // ===== 深色面：文字 =====
        public static readonly Color TextStrong = new Color(0.88f, 0.88f, 0.92f, 1f);
        public static readonly Color TextSecondary = new Color(0.70f, 0.73f, 0.82f, 1f);
        public static readonly Color TextMuted = new Color(0.62f, 0.64f, 0.70f, 1f);

        // ===== 语义状态色 =====
        // ===== 铁道元素：信号灯色阶与黄铜 =====
        /// <summary>色灯信号：红（停车/警报/录音）、黄（注意/减速）、绿（正常/通行）。</summary>
        public static readonly Color SignalRed = new Color(0.92f, 0.22f, 0.20f, 1f);
        public static readonly Color SignalYellow = new Color(0.98f, 0.80f, 0.16f, 1f);
        public static readonly Color SignalGreen = new Color(0.20f, 0.80f, 0.35f, 1f);
        /// <summary>黄铜铭牌色，用于大岛左侧的“钢轨”细边与铭牌类细节。</summary>
        public static readonly Color Brass = new Color(0.83f, 0.66f, 0.26f, 0.90f);

        public static readonly Color Success = SignalGreen;
        public static readonly Color SuccessBright = new Color(0.35f, 0.85f, 0.55f, 1f);
        public static readonly Color SuccessButton = new Color(0.25f, 0.55f, 0.35f, 1f);
        public static readonly Color ActiveFill = new Color(0.24f, 0.38f, 0.28f, 1f);
        public static readonly Color Caution = SignalYellow;
        public static readonly Color SpeedGold = new Color(0.96f, 0.72f, 0.20f, 1f);
        public static readonly Color Info = new Color(0.35f, 0.75f, 1f, 1f);
        public static readonly Color InfoButton = new Color(0.20f, 0.65f, 0.95f, 1f);
        public static readonly Color Alert = SignalRed;
        public static readonly Color Danger = new Color(0.85f, 0.22f, 0.22f, 1f); // 紧急制动等动作按钮，比信号红略暗
        public static readonly Color Badge = new Color(0.92f, 0.22f, 0.22f, 1f);
    }
}
