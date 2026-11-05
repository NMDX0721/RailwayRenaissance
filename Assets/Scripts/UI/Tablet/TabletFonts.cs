using UnityEngine;

namespace RailwayRenaissance.UI.Tablet
{
    /// <summary>
    /// 平板系统界面字体入口。系统界面使用 MiSans（与小米 HyperOS 一致），游戏内容（剧情、像素风界面）仍使用像素字体。
    /// 找不到 MiSans 时退回像素字体，避免出现空白文字。
    /// </summary>
    public static class TabletFonts
    {
        private const string UiFontPath = "Fonts/MiSans-Regular";
        private const string PixelFontPath = "Fonts/zpix";

        private const string UiBoldFontPath = "Fonts/MiSans-Semibold";

        private static Font s_ui;
        private static Font s_uiBold;
        private static Font s_pixel;

        public static Font Ui()
        {
            if (s_ui == null)
            {
                s_ui = Resources.Load<Font>(UiFontPath);
                if (s_ui == null) s_ui = Resources.Load<Font>(PixelFontPath);
            }
            return s_ui;
        }

        /// <summary>标题和强调文字用的半粗体；缺失时退回常规字重。</summary>
        public static Font UiBold()
        {
            if (s_uiBold == null)
            {
                s_uiBold = Resources.Load<Font>(UiBoldFontPath);
                if (s_uiBold == null) s_uiBold = Ui();
            }
            return s_uiBold;
        }

        public static Font Pixel()
        {
            if (s_pixel == null) s_pixel = Resources.Load<Font>(PixelFontPath);
            return s_pixel;
        }
    }
}
