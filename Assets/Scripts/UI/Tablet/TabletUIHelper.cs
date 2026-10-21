using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet
{
    /// <summary>
    /// [T306] 平板 UI 通用视觉样式、调色板与卡片骨架助手。
    /// 为所有 Tablet App 提供统一的字体、颜色、卡片与行布局。
    /// </summary>
    public static class TabletUIHelper
    {
        private static Font gameFont;

        public static FontDefinition Fd()
        {
            if (gameFont == null)
                gameFont = Resources.Load<Font>("Fonts/zpix");
            return new FontDefinition { font = gameFont };
        }

        // ===== 基础墨色与强调 =====
        public static readonly Color Ink = new Color(0.12f, 0.12f, 0.14f, 1f);
        public static readonly Color Accent = new Color(0.85f, 0.45f, 0.15f, 1f);

        // ===== 液态玻璃色彩系统 =====
        public static readonly Color GlassBg = new Color(0.06f, 0.05f, 0.07f, 0.75f);
        public static readonly Color GlassBgLight = new Color(1f, 1f, 1f, 0.35f);
        public static readonly Color GlassBgSoft = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color GlassBgHover = new Color(1f, 1f, 1f, 0.12f);
        public static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.2f);
        public static readonly Color GlassBorderSoft = new Color(1f, 1f, 1f, 0.12f);
        public static readonly Color GlassShadow = new Color(0f, 0f, 0f, 0.5f);
        public static readonly Color GlassAccent = new Color(0.7f, 0.85f, 1f, 0.9f);
        public static readonly Color GlassText = new Color(1f, 1f, 1f, 0.9f);
        public static readonly Color GlassTextDim = new Color(1f, 1f, 1f, 0.5f);
        public static readonly Color GlassTextMuted = new Color(1f, 1f, 1f, 0.35f);
        public static readonly Color GlassMask = new Color(0f, 0f, 0f, 0.55f);

        // ===== HyperOS 原生浅色卡片体系 Tokens（T298 规范） =====
        public static readonly Color SurfaceApp = new Color(0.97f, 0.97f, 0.98f, 1f);
        public static readonly Color SurfaceTopBar = new Color(1f, 1f, 1f, 1f);
        public static readonly Color SurfaceCard = new Color(1f, 1f, 1f, 1f);
        public static readonly Color SurfaceCardSoft = new Color(0.94f, 0.94f, 0.96f, 0.85f);
        public static readonly Color SurfaceHover = new Color(0.93f, 0.93f, 0.95f, 1f);
        public static readonly Color BorderCard = new Color(0.90f, 0.90f, 0.92f, 1f);
        public static readonly Color BorderDivider = new Color(0.92f, 0.92f, 0.94f, 1f);
        public static readonly Color InkPrimary = new Color(0.10f, 0.10f, 0.10f, 1f);
        public static readonly Color InkSecondary = new Color(0.42f, 0.42f, 0.44f, 1f);
        public static readonly Color InkTertiary = new Color(0.61f, 0.61f, 0.63f, 1f);
        public static readonly Color SectionHeaderColor = new Color(0.54f, 0.56f, 0.60f, 1f);
        public static readonly Color AccentSystem = new Color(0.72f, 0.52f, 0.23f, 1f);

        // ===== 归途与传统质感调色板 =====
        public static readonly Color BrownDark = new Color(0.23f, 0.13f, 0.06f, 0.95f);
        public static readonly Color BrownWarm = new Color(0.42f, 0.27f, 0.14f, 0.9f);
        public static readonly Color AmberAccent = new Color(0.72f, 0.52f, 0.23f, 1f);
        public static readonly Color RicePaperLight = new Color(0.91f, 0.85f, 0.75f, 1f);
        public static readonly Color CrimsonSeal = new Color(0.55f, 0.23f, 0.16f, 1f);
        public static readonly Color GoldTrimColor = new Color(0.83f, 0.66f, 0.26f, 1f);
        public static readonly Color DeepRail = new Color(0.10f, 0.16f, 0.23f, 0.95f);

        // ===== 统一 App 浅色卡片骨架 =====
        public static VisualElement BuildAppBody(VisualElement win, string pageTitle, string pageSub = null)
        {
            win.Clear();
            var bodyScroll = new ScrollView();
            bodyScroll.style.flexGrow = 1;
            bodyScroll.style.paddingLeft = 24; bodyScroll.style.paddingRight = 24;
            bodyScroll.style.paddingTop = 16; bodyScroll.style.paddingBottom = 24;
            bodyScroll.style.backgroundColor = SurfaceApp;
            win.Add(bodyScroll);

            if (!string.IsNullOrEmpty(pageTitle))
            {
                var titleBox = new VisualElement();
                titleBox.style.marginBottom = 16;
                bodyScroll.Add(titleBox);

                var t = new Label(pageTitle);
                t.style.fontSize = 22; t.style.color = InkPrimary;
                t.style.unityFontStyleAndWeight = FontStyle.Bold;
                t.style.unityFontDefinition = Fd();
                titleBox.Add(t);

                if (!string.IsNullOrEmpty(pageSub))
                {
                    var s = new Label(pageSub);
                    s.style.fontSize = 13; s.style.color = InkSecondary;
                    s.style.unityFontDefinition = Fd();
                    s.style.marginTop = 4;
                    titleBox.Add(s);
                }
            }
            return bodyScroll;
        }

        public static VisualElement BuildAppCard(string header = null)
        {
            var card = new VisualElement();
            card.style.backgroundColor = SurfaceCard;
            card.style.borderTopLeftRadius = 14; card.style.borderTopRightRadius = 14;
            card.style.borderBottomLeftRadius = 14; card.style.borderBottomRightRadius = 14;
            card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor = BorderCard; card.style.borderBottomColor = BorderCard;
            card.style.borderLeftColor = BorderCard; card.style.borderRightColor = BorderCard;
            card.style.paddingLeft = 16; card.style.paddingRight = 16;
            card.style.paddingTop = 14; card.style.paddingBottom = 14;
            card.style.marginBottom = 14;

            if (!string.IsNullOrEmpty(header))
            {
                var h = new Label(header);
                h.style.fontSize = 14; h.style.color = SectionHeaderColor;
                h.style.unityFontStyleAndWeight = FontStyle.Bold;
                h.style.unityFontDefinition = Fd();
                h.style.marginBottom = 10;
                card.Add(h);
            }
            return card;
        }

        public static VisualElement BuildLightRow(string title, string desc = null, System.Action onClick = null, bool hasArrow = true)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 14; row.style.paddingRight = 14;
            row.style.paddingTop = 12; row.style.paddingBottom = 12;
            row.style.marginBottom = 4;
            row.style.borderTopLeftRadius = 8; row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8; row.style.borderBottomRightRadius = 8;
            row.pickingMode = onClick != null ? PickingMode.Position : PickingMode.Ignore;

            if (onClick != null)
            {
                row.RegisterCallback<PointerEnterEvent>(_ => row.style.backgroundColor = SurfaceHover);
                row.RegisterCallback<PointerLeaveEvent>(_ => row.style.backgroundColor = Color.clear);
                row.RegisterCallback<ClickEvent>(_ => onClick());
            }

            var left = new VisualElement();
            left.style.flexGrow = 1;
            row.Add(left);

            var t = new Label(title);
            t.style.fontSize = 15; t.style.color = InkPrimary;
            t.style.unityFontDefinition = Fd();
            left.Add(t);

            if (!string.IsNullOrEmpty(desc))
            {
                var d = new Label(desc);
                d.style.fontSize = 12; d.style.color = InkSecondary;
                d.style.unityFontDefinition = Fd();
                d.style.marginTop = 2;
                left.Add(d);
            }

            if (hasArrow)
            {
                var arrow = new Label("›");
                arrow.style.fontSize = 18; arrow.style.color = InkTertiary;
                arrow.style.unityFontDefinition = Fd();
                row.Add(arrow);
            }

            return row;
        }
    }
}
