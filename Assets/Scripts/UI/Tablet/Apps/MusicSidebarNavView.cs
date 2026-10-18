using System;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听专业左侧导航栏视窗（MusicSidebarNavView）
    /// 遵循 Spotify 桌面端经典层级与《铁路复兴》沙海暖褐像素美学
    /// 彻底废除旧版横排药丸与文字截断大方块，保留「铁道调频」专属身份语汇
    /// </summary>
    public static class MusicSidebarNavView
    {
        // 导航顶级与播单统一视图路由枚举 (消除串台错位 Bug)
        public enum MusicViewId
        {
            Discover = 0,    // discover: 发现主页（FM-88.5 横幅 + 6 宫格快捷调频 + 精选横向卡片）
            Ost = 1,         // ost: 原声全集（官方大碟 · 26 轨完整曲目列表）
            Cloud = 2,       // cloud: 时代云端（全平台多源聚合与自动同步中心）
            Favorites = 3,   // favorites: 收藏曲目（已点亮红心列表或复古车票空状态）
            LocalTape = 4,   // local_tape: 车长本地磁带（车长私人磁带夹 · 离线音乐库）
            RadioAuto = 5,   // radio: 时代电台（全自动轮播）
            Scenery = 6,     // scenery: 沿线风貌原声
            Ambient = 7,     // ambient: 广播快讯与白噪音

            // 兼容易读别名
            CloudSync = Cloud,
            Radio = RadioAuto,
            WhiteNoise = Ambient
        }

        // 向下兼容别名
        public enum NavTab
        {
            Home = 0,
            Soundtracks = 1,
            CloudSync = 2,
            Favorites = 3
        }

        public class SidebarHandle
        {
            public VisualElement root;
            public Action<MusicViewId> onViewChanged;
            public System.Collections.Generic.Dictionary<MusicViewId, VisualElement> itemElements = new System.Collections.Generic.Dictionary<MusicViewId, VisualElement>();
            public System.Collections.Generic.Dictionary<MusicViewId, VisualElement> indicatorBars = new System.Collections.Generic.Dictionary<MusicViewId, VisualElement>();
            public System.Collections.Generic.Dictionary<MusicViewId, Label> titleLabels = new System.Collections.Generic.Dictionary<MusicViewId, Label>();
        }

        private static readonly Color WarmBg = new Color(0.14f, 0.12f, 0.11f, 0.98f);       // 深褐底色
        private static readonly Color HoverBg = new Color(0.24f, 0.19f, 0.15f, 0.85f);      // 悬浮微亮暖褐
        private static readonly Color ActiveBg = new Color(0.29f, 0.22f, 0.16f, 0.95f);     // 激活项暖褐
        private static readonly Color BrassGold = new Color(0.96f, 0.82f, 0.45f, 1f);       // 黄铜金光
        private static readonly Color MutedText = new Color(0.68f, 0.65f, 0.60f, 1f);       // 暗灰金副字
        private static readonly Color BorderLine = new Color(0.32f, 0.26f, 0.20f, 0.70f);    // 细黄铜线

        public static SidebarHandle BuildSidebar(
            VisualElement parent,
            MusicViewId currentView,
            Action<MusicViewId> onViewChanged)
        {
            var handle = new SidebarHandle
            {
                onViewChanged = onViewChanged
            };

            var sidebar = new VisualElement();
            sidebar.style.width = 220;
            sidebar.style.flexShrink = 0;
            sidebar.style.backgroundColor = WarmBg;
            sidebar.style.backgroundImage = new StyleBackground(MusicIconHelper.GetFrostedGlassTexture());
            sidebar.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            sidebar.style.backgroundSize = new BackgroundSize(Length.Pixels(128), Length.Pixels(128));
            sidebar.style.borderRightWidth = 1.2f;
            sidebar.style.borderRightColor = BorderLine;
            sidebar.style.paddingLeft = 10;
            sidebar.style.paddingRight = 10;
            sidebar.style.paddingTop = 14;
            sidebar.style.paddingBottom = 12;
            sidebar.style.flexDirection = FlexDirection.Column;
            sidebar.style.justifyContent = Justify.SpaceBetween;
            parent.Add(sidebar);
            handle.root = sidebar;

            // ==================== 顶部区：品牌与电台标识 ====================
            var topBox = new VisualElement();
            topBox.style.flexShrink = 0;
            sidebar.Add(topBox);

            var brandRow = new VisualElement();
            brandRow.style.flexDirection = FlexDirection.Row;
            brandRow.style.alignItems = Align.Center;
            brandRow.style.marginBottom = 14;
            brandRow.style.paddingLeft = 4;
            topBox.Add(brandRow);

            var radioIcon = MusicIconHelper.CreateCassetteIcon(18, 12, BrassGold);
            radioIcon.style.marginRight = 7;
            brandRow.Add(radioIcon);

            var brandTitleBox = new VisualElement();
            brandRow.Add(brandTitleBox);

            var brandTitle = new Label("随身听 · 铁道调频");
            brandTitle.style.fontSize = 13;
            brandTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            brandTitle.style.color = BrassGold;
            brandTitle.style.unityFontDefinition = Fd();
            brandTitleBox.Add(brandTitle);

            var brandSub = new Label("WALKMAN · 先民车载广播");
            brandSub.style.fontSize = 8.5f;
            brandSub.style.color = MutedText;
            brandSub.style.unityFontDefinition = Fd();
            brandSub.style.marginTop = 1;
            brandTitleBox.Add(brandSub);

            // ==================== 区段 1：调频与原声 · RADIO & SOUNDTRACK ====================
            AddSectionHeader(topBox, "调频与原声 · BROADCAST");
            AddSidebarRow(handle, topBox, MusicViewId.Discover, "发现与调频", "FM·88", "⌂", currentView);
            AddSidebarRow(handle, topBox, MusicViewId.Ost, "铁道原声全集", "26轨", "[FM]", currentView);
            AddSidebarRow(handle, topBox, MusicViewId.Scenery, "风貌与白噪音", "18轨", "[LINE]", currentView);

            AddSectionDivider(topBox);

            // ==================== 区段 2：资料库与云端 · LIBRARY & CLOUD ====================
            AddSectionHeader(topBox, "资料库与云端 · LIBRARY");
            AddSidebarRow(handle, topBox, MusicViewId.Favorites, "我的收藏", "♥", "♥", currentView);
            AddSidebarRow(handle, topBox, MusicViewId.LocalTape, "车长本地磁带", "离线", "[TAPE]", currentView, isCassetteIcon: true);
            AddSidebarRow(handle, topBox, MusicViewId.Cloud, "时代云端中心", "多源", "☁", currentView);

            // ==================== 底部区：FM-88 先民铭牌 ====================
            var bottomBox = new VisualElement();
            bottomBox.style.flexShrink = 0;
            bottomBox.style.backgroundColor = new Color(0.11f, 0.10f, 0.09f, 0.95f);
            bottomBox.style.borderTopLeftRadius = 6;
            bottomBox.style.borderTopRightRadius = 6;
            bottomBox.style.borderBottomLeftRadius = 6;
            bottomBox.style.borderBottomRightRadius = 6;
            bottomBox.style.borderTopWidth = 1;
            bottomBox.style.borderBottomWidth = 1;
            bottomBox.style.borderLeftWidth = 1;
            bottomBox.style.borderRightWidth = 1;
            bottomBox.style.borderTopColor = new Color(0.40f, 0.33f, 0.22f, 0.7f);
            bottomBox.style.borderBottomColor = bottomBox.style.borderTopColor;
            bottomBox.style.borderLeftColor = bottomBox.style.borderTopColor;
            bottomBox.style.borderRightColor = bottomBox.style.borderTopColor;
            bottomBox.style.paddingLeft = 8;
            bottomBox.style.paddingRight = 8;
            bottomBox.style.paddingTop = 7;
            bottomBox.style.paddingBottom = 7;
            bottomBox.style.flexDirection = FlexDirection.Row;
            bottomBox.style.alignItems = Align.Center;
            bottomBox.style.justifyContent = Justify.SpaceBetween;
            sidebar.Add(bottomBox);

            var fmLeft = new VisualElement();
            fmLeft.style.flexDirection = FlexDirection.Row;
            fmLeft.style.alignItems = Align.Center;
            bottomBox.Add(fmLeft);

            var dot = new VisualElement();
            dot.style.width = 6;
            dot.style.height = 6;
            dot.style.borderTopLeftRadius = 3;
            dot.style.borderTopRightRadius = 3;
            dot.style.borderBottomLeftRadius = 3;
            dot.style.borderBottomRightRadius = 3;
            dot.style.backgroundColor = new Color(0.96f, 0.58f, 0.15f, 1f); // 先民指示琥珀橙
            dot.style.marginRight = 6;
            fmLeft.Add(dot);

            var fmTextCol = new VisualElement();
            fmLeft.Add(fmTextCol);

            var fmName = new Label("FM·88 调频 · 先民标准型");
            fmName.style.fontSize = 9.5f;
            fmName.style.unityFontStyleAndWeight = FontStyle.Bold;
            fmName.style.color = new Color(0.94f, 0.90f, 0.82f, 1f);
            fmName.style.unityFontDefinition = Fd();
            fmTextCol.Add(fmName);

            var fmCode = new Label("DOC-RR-FM88 · 信号在线");
            fmCode.style.fontSize = 8;
            fmCode.style.color = new Color(0.60f, 0.58f, 0.52f, 1f);
            fmCode.style.unityFontDefinition = Fd();
            fmTextCol.Add(fmCode);

            return handle;
        }

        private static void AddSectionHeader(VisualElement parent, string title)
        {
            var header = new Label(title);
            header.style.fontSize = 8.5f;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.55f, 0.50f, 0.42f, 1f);
            header.style.unityFontDefinition = Fd();
            header.style.paddingLeft = 8;
            header.style.marginBottom = 3;
            header.style.marginTop = 3;
            parent.Add(header);
        }

        private static void AddSectionDivider(VisualElement parent)
        {
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = BorderLine;
            divider.style.marginTop = 4;
            divider.style.marginBottom = 5;
            divider.style.marginLeft = 4;
            divider.style.marginRight = 4;
            parent.Add(divider);
        }

        private static void AddSidebarRow(
            SidebarHandle handle,
            VisualElement parent,
            MusicViewId viewId,
            string title,
            string badgeText,
            string iconGlyph,
            MusicViewId currentView,
            bool isCassetteIcon = false)
        {
            bool isSelected = (currentView == viewId);

            var row = new VisualElement();
            row.style.height = 32;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingLeft = 6;
            row.style.paddingRight = 8;
            row.style.marginBottom = 2;
            row.style.borderTopLeftRadius = 4;
            row.style.borderTopRightRadius = 4;
            row.style.borderBottomLeftRadius = 4;
            row.style.borderBottomRightRadius = 4;
            row.style.backgroundColor = isSelected ? ActiveBg : Color.clear;
            row.pickingMode = PickingMode.Position;
            MusicIconHelper.SetHandCursor(row);
            parent.Add(row);
            handle.itemElements[viewId] = row;

            var leftBox = new VisualElement();
            leftBox.style.flexDirection = FlexDirection.Row;
            leftBox.style.alignItems = Align.Center;
            leftBox.style.flexGrow = 1;
            leftBox.style.overflow = Overflow.Hidden;
            row.Add(leftBox);

            // 1. 左指示条 (3px 金色光标)
            var indicator = new VisualElement();
            indicator.style.width = 3;
            indicator.style.height = 15;
            indicator.style.borderTopLeftRadius = 1.5f; indicator.style.borderTopRightRadius = 1.5f;
            indicator.style.borderBottomLeftRadius = 1.5f; indicator.style.borderBottomRightRadius = 1.5f;
            indicator.style.backgroundColor = isSelected ? BrassGold : Color.clear;
            indicator.style.marginRight = 7;
            leftBox.Add(indicator);
            handle.indicatorBars[viewId] = indicator;

            // 2. 统一图标区 (18px 居中对齐)
            var iconBox = new VisualElement();
            iconBox.style.width = 18;
            iconBox.style.alignItems = Align.Center;
            iconBox.style.justifyContent = Justify.Center;
            iconBox.style.marginRight = 6;
            leftBox.Add(iconBox);

            if (isCassetteIcon)
            {
                var miniCassette = MusicIconHelper.CreateCassetteMini(16, 12, isSelected);
                iconBox.Add(miniCassette);
            }
            else if (iconGlyph.StartsWith("["))
            {
                var tag = new Label(iconGlyph);
                tag.style.fontSize = 7.5f;
                tag.style.color = isSelected ? BrassGold : MutedText;
                tag.style.unityFontDefinition = Fd();
                iconBox.Add(tag);
            }
            else
            {
                var glyph = new Label(iconGlyph);
                glyph.style.fontSize = (iconGlyph == "♥") ? 11 : 12.5f;
                glyph.style.color = isSelected ? BrassGold : MutedText;
                glyph.style.unityFontDefinition = Fd();
                iconBox.Add(glyph);
            }

            // 3. 单行统一标题 (11px)
            var titleLbl = new Label(title);
            titleLbl.style.fontSize = 11f;
            titleLbl.style.unityFontStyleAndWeight = isSelected ? FontStyle.Bold : FontStyle.Normal;
            titleLbl.style.color = isSelected ? new Color(0.98f, 0.96f, 0.92f, 1f) : new Color(0.82f, 0.79f, 0.74f, 1f);
            titleLbl.style.unityFontDefinition = Fd();
            titleLbl.style.overflow = Overflow.Hidden;
            titleLbl.style.textOverflow = TextOverflow.Ellipsis;
            leftBox.Add(titleLbl);
            handle.titleLabels[viewId] = titleLbl;

            // 4. 右侧微型 Badge
            if (!string.IsNullOrEmpty(badgeText))
            {
                var badge = new Label(badgeText);
                badge.style.fontSize = 8f;
                badge.style.color = isSelected ? BrassGold : new Color(0.52f, 0.48f, 0.42f, 1f);
                badge.style.unityFontDefinition = Fd();
                row.Add(badge);
            }

            // 交互：悬浮高亮反馈
            row.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (currentView != viewId)
                {
                    row.style.backgroundColor = HoverBg;
                    titleLbl.style.color = BrassGold;
                }
            });
            row.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (currentView != viewId)
                {
                    row.style.backgroundColor = Color.clear;
                    titleLbl.style.color = new Color(0.82f, 0.79f, 0.74f, 1f);
                }
            });
            row.RegisterCallback<ClickEvent>(_ =>
            {
                handle.onViewChanged?.Invoke(viewId);
            });
        }
    }
}
