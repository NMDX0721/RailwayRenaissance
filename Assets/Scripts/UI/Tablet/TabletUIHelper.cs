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

        // ===== 小米澎湃OS (HyperOS Pad) 美学系统 Tokens =====
        public const float RadiusPill = 24f;
        public const float RadiusCard = 18f;
        public const float RadiusSquircle = 10f;
        public const float RadiusSmallPill = 16f;
        public const float RadiusSlider = 32f;

        public static readonly Color GlassControlPanel = new Color(0.08f, 0.09f, 0.12f, 0.90f);
        public static readonly Color GlassHighlightBorder = new Color(1f, 1f, 1f, 0.14f);
        public static readonly Color GlassInactiveTile = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color GlassInactiveTileBorder = new Color(1f, 1f, 1f, 0.11f);
        public static readonly Color GlassActivePill = new Color(0.85f, 0.45f, 0.15f, 0.95f);
        public static readonly Color GlassActiveGreen = new Color(0.18f, 0.62f, 0.36f, 0.95f);
        public static readonly Color GlassActiveBlue = new Color(0.22f, 0.52f, 0.95f, 0.95f);
        public static readonly Color InsetCardBg = new Color(1f, 1f, 1f, 1f);
        public static readonly Color InsetCardBorder = new Color(0f, 0f, 0f, 0.04f);
        public static readonly Color InsetDivider = new Color(0f, 0f, 0f, 0.06f);

        public static Color GetHyperOSCategoryColor(int id, string group)
        {
            switch (id)
            {
                case 1: return new Color(0.23f, 0.51f, 0.96f, 1f); // 账号: 亮蓝
                case 4: case 5: case 6: case 7: return new Color(0.05f, 0.68f, 0.88f, 1f); // 连接: 青蓝
                case 8: return new Color(0.55f, 0.32f, 0.95f, 1f); // 小米超级AI: 紫罗兰
                case 9: return new Color(0.92f, 0.28f, 0.60f, 1f); // 个性化: 玫瑰粉
                case 11: return new Color(0.96f, 0.62f, 0.10f, 1f); // 通知: 暖金
                case 12: return new Color(0.12f, 0.72f, 0.50f, 1f); // 桌面: 薄荷绿
                case 13: return new Color(0.20f, 0.55f, 0.98f, 1f); // 显示: 天蓝
                case 14: return new Color(0.98f, 0.45f, 0.12f, 1f); // 声音: 阳光橙
                case 16: case 17: return new Color(0.15f, 0.70f, 0.42f, 1f); // 密码/隐私: 安全绿
                case 19: return new Color(0.22f, 0.78f, 0.35f, 1f); // 电池: 能量绿
                case 99: return new Color(0.85f, 0.45f, 0.15f, 1f); // 开发者: 琥珀金
                default:
                    switch (group)
                    {
                        case "account": return new Color(0.23f, 0.51f, 0.96f, 1f);
                        case "device": return new Color(0.15f, 0.58f, 0.85f, 1f);
                        case "connect": return new Color(0.05f, 0.68f, 0.88f, 1f);
                        case "security": return new Color(0.15f, 0.70f, 0.42f, 1f);
                        case "other": return new Color(0.48f, 0.42f, 0.88f, 1f);
                        case "feedback": return new Color(0.92f, 0.55f, 0.12f, 1f);
                        default: return new Color(0.52f, 0.56f, 0.62f, 1f);
                    }
            }
        }

        public static VisualElement BuildSquircleBadge(string icon, Color badgeColor, float size = 30f)
        {
            var badge = new VisualElement();
            badge.style.width = size;
            badge.style.height = size;
            badge.style.borderTopLeftRadius = RadiusSquircle;
            badge.style.borderTopRightRadius = RadiusSquircle;
            badge.style.borderBottomLeftRadius = RadiusSquircle;
            badge.style.borderBottomRightRadius = RadiusSquircle;
            badge.style.backgroundColor = badgeColor;
            badge.style.alignItems = Align.Center;
            badge.style.justifyContent = Justify.Center;
            badge.style.flexShrink = 0;
            badge.pickingMode = PickingMode.Ignore;

            var label = new Label(icon);
            label.style.fontSize = Mathf.RoundToInt(size * 0.56f);
            label.style.color = Color.white;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontDefinition = Fd();
            label.pickingMode = PickingMode.Ignore;
            badge.Add(label);

            return badge;
        }

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

        // ===== 现代图形化组件：胶囊标签 (Tag Pill) =====
        public static VisualElement BuildTagPill(string text, Color bgColor, Color textColor, float fontSize = 11f)
        {
            var pill = new VisualElement();
            pill.style.flexDirection = FlexDirection.Row;
            pill.style.alignItems = Align.Center;
            pill.style.justifyContent = Justify.Center;
            pill.style.backgroundColor = bgColor;
            pill.style.borderTopLeftRadius = RadiusPill;
            pill.style.borderTopRightRadius = RadiusPill;
            pill.style.borderBottomLeftRadius = RadiusPill;
            pill.style.borderBottomRightRadius = RadiusPill;
            pill.style.paddingLeft = 8;
            pill.style.paddingRight = 8;
            pill.style.paddingTop = 2;
            pill.style.paddingBottom = 2;
            pill.style.flexShrink = 0;
            pill.pickingMode = PickingMode.Ignore;

            var lbl = new Label(text);
            lbl.style.fontSize = fontSize;
            lbl.style.color = textColor;
            lbl.style.unityFontDefinition = Fd();
            lbl.style.whiteSpace = WhiteSpace.NoWrap;
            lbl.pickingMode = PickingMode.Ignore;
            pill.Add(lbl);

            return pill;
        }

        // ===== 现代图形化组件：胶囊进度条 (Progress Bar) =====
        public static VisualElement BuildProgressBar(float progress01, Color fillColor, float height = 8f, Color? trackColor = null)
        {
            float clamped = Mathf.Clamp01(progress01);
            var track = new VisualElement();
            track.style.height = height;
            track.style.backgroundColor = trackColor ?? new Color(0f, 0f, 0f, 0.06f);
            track.style.borderTopLeftRadius = height * 0.5f;
            track.style.borderTopRightRadius = height * 0.5f;
            track.style.borderBottomLeftRadius = height * 0.5f;
            track.style.borderBottomRightRadius = height * 0.5f;
            track.style.overflow = Overflow.Hidden;
            track.style.flexDirection = FlexDirection.Row;
            track.pickingMode = PickingMode.Ignore;

            var fill = new VisualElement();
            fill.style.width = new Length(clamped * 100f, LengthUnit.Percent);
            fill.style.height = height;
            fill.style.backgroundColor = fillColor;
            fill.style.borderTopLeftRadius = height * 0.5f;
            fill.style.borderTopRightRadius = height * 0.5f;
            fill.style.borderBottomLeftRadius = height * 0.5f;
            fill.style.borderBottomRightRadius = height * 0.5f;
            fill.pickingMode = PickingMode.Ignore;
            track.Add(fill);

            return track;
        }

        // ===== 现代图形化组件：多段色彩存储占用条 (Segmented Bar) =====
        public static VisualElement BuildSegmentedBar(float height = 12f, params (float ratio, Color color)[] segments)
        {
            var bar = new VisualElement();
            bar.style.height = height;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.borderTopLeftRadius = height * 0.5f;
            bar.style.borderTopRightRadius = height * 0.5f;
            bar.style.borderBottomLeftRadius = height * 0.5f;
            bar.style.borderBottomRightRadius = height * 0.5f;
            bar.style.overflow = Overflow.Hidden;
            bar.style.backgroundColor = new Color(0f, 0f, 0f, 0.08f);
            bar.pickingMode = PickingMode.Ignore;

            float total = 0f;
            foreach (var seg in segments) total += Mathf.Max(0f, seg.ratio);
            if (total <= 0.001f) total = 1f;

            foreach (var seg in segments)
            {
                if (seg.ratio <= 0.001f) continue;
                var segEl = new VisualElement();
                segEl.style.width = new Length((seg.ratio / total) * 100f, LengthUnit.Percent);
                segEl.style.height = height;
                segEl.style.backgroundColor = seg.color;
                segEl.style.marginRight = 1;
                segEl.pickingMode = PickingMode.Ignore;
                bar.Add(segEl);
            }

            return bar;
        }

        // ===== 现代图形化组件：搜索胶囊框 (Search Capsule) =====
        public static VisualElement BuildSearchInput(string placeholder, System.Action<string> onValueChanged)
        {
            var box = new VisualElement();
            box.style.flexDirection = FlexDirection.Row;
            box.style.alignItems = Align.Center;
            box.style.backgroundColor = SurfaceApp;
            box.style.borderTopLeftRadius = RadiusPill;
            box.style.borderTopRightRadius = RadiusPill;
            box.style.borderBottomLeftRadius = RadiusPill;
            box.style.borderBottomRightRadius = RadiusPill;
            box.style.borderTopWidth = 1; box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1; box.style.borderRightWidth = 1;
            box.style.borderTopColor = BorderCard; box.style.borderBottomColor = BorderCard;
            box.style.borderLeftColor = BorderCard; box.style.borderRightColor = BorderCard;
            box.style.paddingLeft = 12; box.style.paddingRight = 12;
            box.style.height = 36;

            var icon = new Label("🔍");
            icon.style.fontSize = 13;
            icon.style.color = InkTertiary;
            icon.style.marginRight = 6;
            icon.style.unityFontDefinition = Fd();
            box.Add(icon);

            var field = new TextField();
            field.style.flexGrow = 1;
            field.style.fontSize = 13;
            field.style.color = InkPrimary;
            field.style.backgroundColor = Color.clear;
            field.style.borderTopWidth = 0; field.style.borderBottomWidth = 0;
            field.style.borderLeftWidth = 0; field.style.borderRightWidth = 0;
            field.style.paddingTop = 0; field.style.paddingBottom = 0;
            field.style.unityFontDefinition = Fd();
            if (!string.IsNullOrEmpty(placeholder))
            {
                var inputChild = field.Q("unity-text-input");
                if (inputChild != null)
                {
                    inputChild.style.backgroundColor = Color.clear;
                    inputChild.style.borderTopWidth = 0; inputChild.style.borderBottomWidth = 0;
                    inputChild.style.borderLeftWidth = 0; inputChild.style.borderRightWidth = 0;
                }
            }
            field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
            box.Add(field);

            return box;
        }
    }
}
