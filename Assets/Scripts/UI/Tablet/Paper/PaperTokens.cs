using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Paper
{
    /// <summary>
    /// 「站台纸本」设计语言的全部 token。任何 UI 只引用这里，不再自己写颜色/圆角/字号魔法数。
    /// 主题：2076 年山区小站的纸质办公物——车票、工牌、时刻表、公文、铁轨。暖纸底、深褐墨字、铁路琥珀强调。
    /// </summary>
    public static class PaperTokens
    {
        // ---- 色 ----
        public static readonly Color PaperBg = new Color(0.984f, 0.973f, 0.937f, 1f);      // #FBF8EF 面板
        public static readonly Color PaperGlass = new Color(0.985f, 0.972f, 0.935f, 0.94f); // 悬浮在场景上的面板
        public static readonly Color PaperDeep = new Color(0.945f, 0.914f, 0.847f, 1f);  // #F1E9D8 凹陷
        public static readonly Color Card = new Color(1f, 0.992f, 0.969f, 1f);           // #FFFDF7
        public static readonly Color Ink = new Color(0.169f, 0.141f, 0.110f, 1f);        // #2B241C
        public static readonly Color InkSoft = new Color(0.169f, 0.141f, 0.110f, 0.62f);
        public static readonly Color InkMute = new Color(0.169f, 0.141f, 0.110f, 0.40f);
        public static readonly Color Line = new Color(0.42f, 0.32f, 0.20f, 0.28f);       // 褐色描边
        public static readonly Color LineSoft = new Color(0.42f, 0.32f, 0.20f, 0.16f);
        public static readonly Color Amber = new Color(0.80f, 0.52f, 0.12f, 1f);         // #CC841F 主强调
        public static readonly Color AmberDeep = new Color(0.72f, 0.42f, 0.08f, 1f);
        public static readonly Color Good = new Color(0.184f, 0.561f, 0.357f, 1f);       // #2F8F5B
        public static readonly Color Warn = new Color(0.851f, 0.573f, 0.169f, 1f);       // #D9922B
        public static readonly Color Bad = new Color(0.753f, 0.224f, 0.169f, 1f);        // #C0392B
        public static readonly Color Stamp = new Color(0.72f, 0.16f, 0.14f, 0.85f);      // 印章红

        // 五个分类色（经营页签 / 图标 / 徽章）
        public static readonly Color Dispatch = new Color(0.220f, 0.420f, 0.722f, 1f);   // #386BB8 调度
        public static readonly Color Crew = new Color(0.239f, 0.549f, 0.361f, 1f);       // #3D8C5C 人员
        public static readonly Color Orders = new Color(0.722f, 0.478f, 0.102f, 1f);     // #B87A1A 订单
        public static readonly Color Market = new Color(0.522f, 0.341f, 0.702f, 1f);     // #8557B3 市场
        public static readonly Color Finance = new Color(0.180f, 0.502f, 0.541f, 1f);    // #2E808A 财务

        public static Color Tint(Color c, float a) => new Color(c.r, c.g, c.b, a);
        public static Color Darker(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        // ---- 圆角 ----
        public const float RadiusS = 10f, RadiusM = 16f, RadiusL = 24f, RadiusXL = 30f;

        // ---- 间距（8pt 网格）----
        public const float S1 = 8f, S2 = 12f, S3 = 16f, S4 = 24f, S5 = 32f;

        // ---- 字号 ----
        public const int FsNote = 11, FsSmall = 13, FsBody = 15, FsSub = 18, FsTitle = 22, FsNumber = 32;

        // ---- 动效 ----
        public const float FadeFast = 0.16f, FadeNormal = 0.2f, FadeSlow = 0.3f;
    }
}
