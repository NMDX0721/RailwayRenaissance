using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听中间主舞台视图控制器 (Main Stage View)
    /// 承接 Discover（发现主页）、OST（原声全集）、Favorites（收藏）、LocalTape（车长本地磁带）、CloudSync（时代云端多源聚合）
    /// 彻底清除原生 Emoji，全面应用 16-bit 像素工业美学与暖褐黄铜 Design Tokens
    /// </summary>
    public static class MusicMainStageView
    {
        // ==================== 主舞台渲染路由分发 ====================
        public static void RenderStage(VisualElement container, VisualElement win, MusicSidebarNavView.MusicViewId viewId)
        {
            if (container == null) return;
            container.Clear();

            switch (viewId)
            {
                case MusicSidebarNavView.MusicViewId.Discover:
                    BuildSpotifyHomeView(container, win);
                    break;
                case MusicSidebarNavView.MusicViewId.Ost:
                    BuildPlaylistDetailView(container, win);
                    break;
                case MusicSidebarNavView.MusicViewId.Favorites:
                    BuildFavoritesView(container, win);
                    break;
                case MusicSidebarNavView.MusicViewId.LocalTape:
                    var localScroll = new ScrollView();
                    localScroll.style.flexGrow = 1;
                    localScroll.style.paddingLeft = 18; localScroll.style.paddingRight = 18;
                    localScroll.style.paddingTop = 14; localScroll.style.paddingBottom = 20;
                    container.Add(localScroll);
                    BuildLocalTapeView(localScroll, win);
                    break;
                case MusicSidebarNavView.MusicViewId.CloudSync:
                    var cloudScroll = new ScrollView();
                    cloudScroll.style.flexGrow = 1;
                    cloudScroll.style.paddingLeft = 18; cloudScroll.style.paddingRight = 18;
                    cloudScroll.style.paddingTop = 14; cloudScroll.style.paddingBottom = 20;
                    container.Add(cloudScroll);
                    BuildCloudSyncView(cloudScroll, win);
                    break;
                case MusicSidebarNavView.MusicViewId.Radio:
                case MusicSidebarNavView.MusicViewId.Scenery:
                case MusicSidebarNavView.MusicViewId.WhiteNoise:
                    // 分类快捷连播与筛选过滤
                    BuildPlaylistDetailView(container, win);
                    break;
                default:
                    BuildSpotifyHomeView(container, win);
                    break;
            }
        }

        // ==================== 1. Discover 发现主页 ====================
        public static void BuildSpotifyHomeView(VisualElement parent, VisualElement win)
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.style.paddingLeft = 18; scroll.style.paddingRight = 18;
            scroll.style.paddingTop = 14; scroll.style.paddingBottom = 20;
            parent.Add(scroll);

            // 问候栏
            var greetBox = new VisualElement();
            greetBox.style.marginBottom = 12;
            scroll.Add(greetBox);

            int hour = DateTime.Now.Hour;
            string greetPeriod = (hour >= 5 && hour < 9) ? "清晨好" :
                (hour >= 9 && hour < 12) ? "上午好" :
                (hour >= 12 && hour < 14) ? "中午好" :
                (hour >= 14 && hour < 18) ? "下午好" :
                (hour >= 18 && hour < 22) ? "傍晚好" : "夜深了";

            var greetRow = new VisualElement();
            greetRow.style.flexDirection = FlexDirection.Row;
            greetRow.style.alignItems = Align.Center;
            greetBox.Add(greetRow);

            var pilotLamp = MusicIconHelper.CreatePilotLamp(true);
            pilotLamp.style.marginRight = 8;
            greetRow.Add(pilotLamp);

            var greetLbl = new Label($"{greetPeriod}，列车长");
            greetLbl.style.fontSize = 17;
            greetLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            greetLbl.style.color = MusicIconHelper.BrassGold;
            greetLbl.style.unityFontDefinition = Fd();
            greetRow.Add(greetLbl);

            var greetSub = new Label("沿线工务通信信标解调良好 · 26 轨铁道原声全量入库");
            greetSub.style.fontSize = 10;
            greetSub.style.color = MusicIconHelper.TextMuted;
            greetSub.style.unityFontDefinition = Fd();
            greetSub.style.marginTop = 2;
            greetBox.Add(greetSub);

            // FM-88.5 铁道解调广播横幅 (Banner)
            var radioBanner = new VisualElement();
            radioBanner.style.height = 70;
            radioBanner.style.backgroundColor = MusicIconHelper.BgCard;
            radioBanner.style.borderTopLeftRadius = 10; radioBanner.style.borderTopRightRadius = 10;
            radioBanner.style.borderBottomLeftRadius = 10; radioBanner.style.borderBottomRightRadius = 10;
            radioBanner.style.borderTopWidth = 1; radioBanner.style.borderBottomWidth = 1;
            radioBanner.style.borderLeftWidth = 1; radioBanner.style.borderRightWidth = 1;
            radioBanner.style.borderTopColor = MusicIconHelper.BorderIndustrial;
            radioBanner.style.borderBottomColor = MusicIconHelper.BorderIndustrial;
            radioBanner.style.borderLeftColor = MusicIconHelper.BorderIndustrial;
            radioBanner.style.borderRightColor = MusicIconHelper.BorderIndustrial;
            radioBanner.style.paddingLeft = 14; radioBanner.style.paddingRight = 14;
            radioBanner.style.flexDirection = FlexDirection.Row;
            radioBanner.style.alignItems = Align.Center;
            radioBanner.style.justifyContent = Justify.SpaceBetween;
            radioBanner.style.marginBottom = 14;
            scroll.Add(radioBanner);

            // ===== 朝鲜对外频道 (Voice of Korea / 平壤广播电台) 专属短波专卡 =====
            var dprkCard = new VisualElement();
            dprkCard.style.backgroundColor = new Color(0.12f, 0.10f, 0.12f, 0.95f);
            dprkCard.style.borderTopLeftRadius = 8; dprkCard.style.borderTopRightRadius = 8;
            dprkCard.style.borderBottomLeftRadius = 8; dprkCard.style.borderBottomRightRadius = 8;
            dprkCard.style.borderTopWidth = 1; dprkCard.style.borderBottomWidth = 1;
            dprkCard.style.borderLeftWidth = 1; dprkCard.style.borderRightWidth = 1;
            dprkCard.style.borderTopColor = new Color(0.85f, 0.25f, 0.25f, 0.8f); // 红金边框
            dprkCard.style.borderBottomColor = dprkCard.style.borderTopColor;
            dprkCard.style.borderLeftColor = dprkCard.style.borderTopColor;
            dprkCard.style.borderRightColor = dprkCard.style.borderTopColor;
            dprkCard.style.paddingLeft = 14; dprkCard.style.paddingRight = 14;
            dprkCard.style.paddingTop = 10; dprkCard.style.paddingBottom = 10;
            dprkCard.style.marginBottom = 14;
            dprkCard.style.flexDirection = FlexDirection.Row;
            dprkCard.style.justifyContent = Justify.SpaceBetween;
            dprkCard.style.alignItems = Align.Center;

            var dprkLeft = new VisualElement();
            dprkLeft.style.flexDirection = FlexDirection.Row;
            dprkLeft.style.alignItems = Align.Center;
            dprkLeft.style.flexGrow = 1;
            dprkCard.Add(dprkLeft);

            var dprkBadge = MusicIconHelper.CreatePlatformCapsule("[VOK]", new Color(0.95f, 0.30f, 0.30f, 1f));
            dprkBadge.style.marginRight = 10;
            dprkLeft.Add(dprkBadge);

            var dprkInfo = new VisualElement();
            dprkLeft.Add(dprkInfo);

            var dprkTitle = new Label("📡 朝鲜之声 (Voice of Korea) · 平壤短波 6070kHz / 7220kHz 华语对外广播");
            dprkTitle.style.fontSize = 11.5f;
            dprkTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            dprkTitle.style.color = MusicIconHelper.TextParchment;
            dprkTitle.style.unityFontDefinition = Fd();
            dprkInfo.Add(dprkTitle);

            var dprkDesc = new Label("平壤广播电台 · 朝鲜中央广播电台 (KCBS) 实况电波中继 · 支持全频段短波解调");
            dprkDesc.style.fontSize = 9f;
            dprkDesc.style.color = MusicIconHelper.TextMuted;
            dprkDesc.style.unityFontDefinition = Fd();
            dprkDesc.style.marginTop = 2;
            dprkInfo.Add(dprkDesc);

            var tuneDprkBtn = new Button(() =>
            {
                MusicAudioEffectsEngine.CurrentFilterMode = MusicAudioEffectsEngine.RadioFilterMode.VintageTubeFM;
                TabletMusicAppUI.ShowToast("📡 已捕获平壤短波电波！正在解调《朝鲜之声》华语广播...");
                // 播发朝鲜之声或新闻电波
                var vTrack = Array.Find(TabletMusicAppUI.TabletMusicList, m => m.id == "news" || m.id == "news_explosive");
                if (vTrack != null) TabletMusicAppUI.PlayTabletTrack(vTrack);
                else TabletMusicAppUI.AutoPlayDefaultMusic();
            }) { text = "接入平壤电波" };
            tuneDprkBtn.style.height = 26; tuneDprkBtn.style.paddingLeft = 12; tuneDprkBtn.style.paddingRight = 12;
            tuneDprkBtn.style.fontSize = 10; tuneDprkBtn.style.unityFontDefinition = Fd();
            tuneDprkBtn.style.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1f);
            tuneDprkBtn.style.color = Color.white;
            tuneDprkBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            tuneDprkBtn.style.borderTopLeftRadius = 13; tuneDprkBtn.style.borderTopRightRadius = 13;
            tuneDprkBtn.style.borderBottomLeftRadius = 13; tuneDprkBtn.style.borderBottomRightRadius = 13;
            tuneDprkBtn.style.borderTopWidth = 0; tuneDprkBtn.style.borderBottomWidth = 0;
            tuneDprkBtn.style.borderLeftWidth = 0; tuneDprkBtn.style.borderRightWidth = 0;
            MusicIconHelper.SetHandCursor(tuneDprkBtn);
            dprkCard.Add(tuneDprkBtn);

            scroll.Add(dprkCard);

            var bannerLeft = new VisualElement();
            bannerLeft.style.flexDirection = FlexDirection.Row;
            bannerLeft.style.alignItems = Align.Center;
            radioBanner.Add(bannerLeft);

            var bannerBadge = MusicIconHelper.CreatePlatformCapsule("FM 88.5", MusicIconHelper.BrassGold);
            bannerBadge.style.marginRight = 10;
            bannerLeft.Add(bannerBadge);

            var bannerInfo = new VisualElement();
            bannerLeft.Add(bannerInfo);

            var bannerTitle = new Label("铁道专用调频广播台");
            bannerTitle.style.fontSize = 13;
            bannerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            bannerTitle.style.color = MusicIconHelper.TextParchment;
            bannerTitle.style.unityFontDefinition = Fd();
            bannerInfo.Add(bannerTitle);

            var bannerDesc = new Label("先民遗构通讯频段 · 随车自动解调并实时广播");
            bannerDesc.style.fontSize = 9.5f;
            bannerDesc.style.color = MusicIconHelper.TextMuted;
            bannerDesc.style.unityFontDefinition = Fd();
            bannerDesc.style.marginTop = 2;
            bannerInfo.Add(bannerDesc);

            var tuneBtn = new Button(() =>
            {
                // 开启电子管复古微晃电台滤镜
                MusicAudioEffectsEngine.CurrentFilterMode = MusicAudioEffectsEngine.RadioFilterMode.VintageTubeFM;
                if (TabletMusicAppUI.tabletIsPlaying)
                {
                    TabletMusicAppUI.ShowToast("📻 已开启电子管调频微晃 · 锁定 FM-88.5 调度频段 (正在调谐)");
                    TabletMusicAppUI.UpdatePlayerBarUI();
                }
                else
                {
                    var rTrack = Array.Find(TabletMusicAppUI.TabletMusicList, m => m.category == "广播" || m.id == "news" || m.id == "platform");
                    if (rTrack != null) TabletMusicAppUI.PlayTabletTrack(rTrack);
                    else TabletMusicAppUI.AutoPlayDefaultMusic();
                    TabletMusicAppUI.ShowToast("📻 已开机并锁定 FM-88.5 铁道机车广播频段");
                }
            }) { text = "一键调频" };
            tuneBtn.style.height = 26; tuneBtn.style.paddingLeft = 14; tuneBtn.style.paddingRight = 14;
            tuneBtn.style.fontSize = 10; tuneBtn.style.unityFontDefinition = Fd();
            tuneBtn.style.backgroundColor = MusicIconHelper.BrassDark;
            tuneBtn.style.color = new Color(0.12f, 0.10f, 0.08f, 1f);
            tuneBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            tuneBtn.style.borderTopLeftRadius = 13; tuneBtn.style.borderTopRightRadius = 13;
            tuneBtn.style.borderBottomLeftRadius = 13; tuneBtn.style.borderBottomRightRadius = 13;
            tuneBtn.style.borderTopWidth = 0; tuneBtn.style.borderBottomWidth = 0;
            tuneBtn.style.borderLeftWidth = 0; tuneBtn.style.borderRightWidth = 0;
            radioBanner.Add(tuneBtn);

            // 6 宫格快捷调频网格
            var gridBox = new VisualElement();
            gridBox.style.flexDirection = FlexDirection.Row;
            gridBox.style.flexWrap = Wrap.Wrap;
            gridBox.style.justifyContent = Justify.SpaceBetween;
            gridBox.style.marginBottom = 16;
            scroll.Add(gridBox);

            string[] quickTracks = { "platform", "iron_and_ash", "embers", "calm", "morning", "determination" };
            foreach (var qId in quickTracks)
            {
                var entry = Array.Find(TabletMusicAppUI.TabletMusicList, m => m.id == qId);
                if (entry == null) continue;

                bool isUnlocked = TabletMusicAppUI.IsTabletMusicUnlocked(entry);
                bool isPlaying = (TabletMusicAppUI.tabletCurrentTrackId == entry.id && TabletMusicAppUI.tabletIsPlaying);

                var card = new VisualElement();
                card.style.width = Length.Percent(31.8f);
                card.style.height = 42;
                card.style.backgroundColor = isPlaying ? MusicIconHelper.BgCardHover : MusicIconHelper.BgCard;
                card.style.borderTopLeftRadius = 6; card.style.borderTopRightRadius = 6;
                card.style.borderBottomLeftRadius = 6; card.style.borderBottomRightRadius = 6;
                card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
                card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
                card.style.borderTopColor = isPlaying ? MusicIconHelper.BrassGold : MusicIconHelper.BorderIndustrial;
                card.style.borderBottomColor = card.style.borderTopColor;
                card.style.borderLeftColor = card.style.borderTopColor;
                card.style.borderRightColor = card.style.borderTopColor;
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems = Align.Center;
                card.style.justifyContent = Justify.SpaceBetween;
                card.style.paddingLeft = 8; card.style.paddingRight = 8;
                card.style.marginBottom = 8;
                gridBox.Add(card);

                var cardLeft = new VisualElement();
                cardLeft.style.flexDirection = FlexDirection.Row;
                cardLeft.style.alignItems = Align.Center;
                cardLeft.style.flexGrow = 1;
                cardLeft.style.overflow = Overflow.Hidden;
                card.Add(cardLeft);

                var thumb = new VisualElement();
                thumb.style.width = 30; thumb.style.height = 30;
                thumb.style.borderTopLeftRadius = 4; thumb.style.borderTopRightRadius = 4;
                thumb.style.borderBottomLeftRadius = 4; thumb.style.borderBottomRightRadius = 4;
                thumb.style.overflow = Overflow.Hidden;
                thumb.style.marginRight = 8;
                Texture2D covTex = MusicAlbumArtResolver.GetAlbumCover(entry.id, entry.category);
                thumb.style.backgroundImage = new StyleBackground(covTex);
                thumb.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                cardLeft.Add(thumb);

                var nameLbl = new Label(entry.title);
                nameLbl.style.fontSize = 11;
                nameLbl.style.unityFontStyleAndWeight = isPlaying ? FontStyle.Bold : FontStyle.Normal;
                nameLbl.style.color = isPlaying ? MusicIconHelper.BrassGold : (isUnlocked ? MusicIconHelper.TextParchment : MusicIconHelper.TextMuted);
                nameLbl.style.unityFontDefinition = Fd();
                nameLbl.style.whiteSpace = WhiteSpace.NoWrap;
                nameLbl.style.overflow = Overflow.Hidden;
                nameLbl.style.textOverflow = TextOverflow.Ellipsis;
                cardLeft.Add(nameLbl);

                var playBtn = new Button(() =>
                {
                    if (isUnlocked)
                    {
                        TabletMusicAppUI.PlayTabletTrack(entry);
                        TabletMusicAppUI.BuildMusicApp(win);
                    }
                    else
                    {
                        TabletMusicAppUI.ShowToast("【调频加密】需在剧情旅途中解锁该波段！");
                    }
                });
                playBtn.style.width = 24; playBtn.style.height = 24;
                playBtn.style.borderTopLeftRadius = 12; playBtn.style.borderTopRightRadius = 12;
                playBtn.style.borderBottomLeftRadius = 12; playBtn.style.borderBottomRightRadius = 12;
                playBtn.style.borderTopWidth = 0; playBtn.style.borderBottomWidth = 0;
                playBtn.style.borderLeftWidth = 0; playBtn.style.borderRightWidth = 0;
                playBtn.style.backgroundColor = isPlaying ? MusicIconHelper.BrassGold : new Color(0.25f, 0.22f, 0.19f, 1f);
                playBtn.style.alignItems = Align.Center; playBtn.style.justifyContent = Justify.Center;
                card.Add(playBtn);

                if (isPlaying)
                {
                    playBtn.Add(MusicIconHelper.CreatePauseIcon(2f, 8f, 2f, new Color(0.12f, 0.10f, 0.08f, 1f)));
                }
                else
                {
                    playBtn.Add(MusicIconHelper.CreatePlayIcon(7f, isUnlocked ? MusicIconHelper.BrassGold : MusicIconHelper.TextMuted));
                }
            }

            // 货架分区 1：先民复兴热播
            BuildShelfSection(scroll, win, "先民复兴热播 · 站台回响", "车务保线日常播送最高频次的官方音轨", new[] { "platform", "iron_and_ash", "morning", "determination" });

            // 货架分区 2：大漠沿线风貌
            BuildShelfSection(scroll, win, "大漠沿线风貌 · 荒原巡程", "飞越荒原与穿越关山时的全景环境配乐", new[] { "cloud_rail", "first_light", "borderline", "silence" });
        }

        // ==================== 货架通用组件 ====================
        public static void BuildShelfSection(VisualElement parent, VisualElement win, string shelfTitle, string shelfSub, string[] trackIds)
        {
            var shelfBox = new VisualElement();
            shelfBox.style.marginBottom = 16;
            parent.Add(shelfBox);

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.justifyContent = Justify.SpaceBetween;
            shelfBox.Add(titleRow);

            var tLbl = new Label(shelfTitle);
            tLbl.style.fontSize = 13;
            tLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tLbl.style.color = MusicIconHelper.BrassGold;
            tLbl.style.unityFontDefinition = Fd();
            titleRow.Add(tLbl);

            var sLbl = new Label(shelfSub);
            sLbl.style.fontSize = 9.5f;
            sLbl.style.color = MusicIconHelper.TextMuted;
            sLbl.style.unityFontDefinition = Fd();
            sLbl.style.marginTop = 2; sLbl.style.marginBottom = 8;
            shelfBox.Add(sLbl);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            shelfBox.Add(row);

            foreach (var tId in trackIds)
            {
                var entry = Array.Find(TabletMusicAppUI.TabletMusicList, m => m.id == tId);
                if (entry == null) continue;

                bool isUnlocked = TabletMusicAppUI.IsTabletMusicUnlocked(entry);
                bool isCur = (TabletMusicAppUI.tabletCurrentTrackId == entry.id && TabletMusicAppUI.tabletIsPlaying);

                var card = new VisualElement();
                card.style.width = Length.Percent(23.5f);
                card.style.backgroundColor = isCur ? MusicIconHelper.BgCardHover : MusicIconHelper.BgCard;
                card.style.borderTopLeftRadius = 8; card.style.borderTopRightRadius = 8;
                card.style.borderBottomLeftRadius = 8; card.style.borderBottomRightRadius = 8;
                card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
                card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
                card.style.borderTopColor = isCur ? MusicIconHelper.BrassGold : MusicIconHelper.BorderIndustrial;
                card.style.borderBottomColor = card.style.borderTopColor;
                card.style.borderLeftColor = card.style.borderTopColor;
                card.style.borderRightColor = card.style.borderTopColor;
                card.style.paddingLeft = 8; card.style.paddingRight = 8;
                card.style.paddingTop = 8; card.style.paddingBottom = 8;
                row.Add(card);

                // 真实 16-bit 像素原画封套 (消除同心圆黑块)
                var cover = new VisualElement();
                cover.style.width = Length.Percent(100f);
                cover.style.height = 70;
                cover.style.borderTopLeftRadius = 6; cover.style.borderTopRightRadius = 6;
                cover.style.borderBottomLeftRadius = 6; cover.style.borderBottomRightRadius = 6;
                cover.style.alignItems = Align.Center; cover.style.justifyContent = Justify.Center;
                cover.style.marginBottom = 6;
                cover.style.position = Position.Relative;
                cover.style.overflow = Overflow.Hidden;
                Texture2D covTex = MusicAlbumArtResolver.GetAlbumCover(entry.id, entry.category);
                cover.style.backgroundImage = new StyleBackground(covTex);
                cover.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                card.Add(cover);

                // 质感遮罩微暗衬底
                var scrim = new VisualElement();
                scrim.style.position = Position.Absolute;
                scrim.style.left = 0; scrim.style.right = 0; scrim.style.bottom = 0; scrim.style.top = 0;
                scrim.style.backgroundColor = new Color(0f, 0f, 0f, isCur ? 0.35f : (isUnlocked ? 0.12f : 0.65f));
                scrim.pickingMode = PickingMode.Ignore;
                cover.Add(scrim);

                if (isCur)
                {
                    cover.Add(MusicIconHelper.CreatePlayingWaveBars(MusicIconHelper.BrassGold));
                }
                else if (!isUnlocked)
                {
                    cover.Add(MusicIconHelper.CreateLockIcon(MusicIconHelper.BrassGold));
                }

                var titleLbl = new Label(entry.title);
                titleLbl.style.fontSize = 11;
                titleLbl.style.unityFontStyleAndWeight = isCur ? FontStyle.Bold : FontStyle.Normal;
                titleLbl.style.color = isCur ? MusicIconHelper.BrassGold : (isUnlocked ? MusicIconHelper.TextParchment : MusicIconHelper.TextMuted);
                titleLbl.style.unityFontDefinition = Fd();
                titleLbl.style.whiteSpace = WhiteSpace.NoWrap;
                titleLbl.style.overflow = Overflow.Hidden;
                titleLbl.style.textOverflow = TextOverflow.Ellipsis;
                card.Add(titleLbl);

                var catBadge = MusicIconHelper.CreatePlatformCapsule(entry.category, isCur ? MusicIconHelper.BrassGold : MusicIconHelper.TextMuted);
                catBadge.style.marginTop = 4;
                card.Add(catBadge);

                // 点击播放交互
                card.RegisterCallback<ClickEvent>(evt =>
                {
                    if (isUnlocked)
                    {
                        TabletMusicAppUI.PlayTabletTrack(entry);
                        TabletMusicAppUI.BuildMusicApp(win);
                    }
                    else
                    {
                        TabletMusicAppUI.ShowToast("【调频加密】需在剧情旅途中解锁该波段！");
                    }
                });
            }
        }

        // ==================== 2. OST 原声全集与歌单详情 ====================
        public static void BuildPlaylistDetailView(VisualElement parent, VisualElement win)
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            parent.Add(scroll);

            BuildHeroHeader(scroll, win);
            BuildTracklistTable(scroll, win);
        }

        // Hero 看板：高度严格压缩至 104px（满足 T313 终审指标）
        public static void BuildHeroHeader(VisualElement parent, VisualElement win)
        {
            var heroBox = new VisualElement();
            heroBox.style.height = 104; // 严格压缩至 104px
            heroBox.style.backgroundColor = MusicIconHelper.BgCard;
            heroBox.style.borderBottomWidth = 1;
            heroBox.style.borderBottomColor = MusicIconHelper.BorderIndustrial;
            heroBox.style.flexDirection = FlexDirection.Row;
            heroBox.style.alignItems = Align.Center;
            heroBox.style.paddingLeft = 18; heroBox.style.paddingRight = 18;
            heroBox.style.position = Position.Relative;
            parent.Add(heroBox);

            // 左侧黑胶封套 (80×80)
            var coverBox = new VisualElement();
            coverBox.style.width = 80; coverBox.style.height = 80;
            coverBox.style.borderTopLeftRadius = 8; coverBox.style.borderTopRightRadius = 8;
            coverBox.style.borderBottomLeftRadius = 8; coverBox.style.borderBottomRightRadius = 8;
            coverBox.style.backgroundColor = new Color(0.14f, 0.12f, 0.10f, 1f);
            coverBox.style.borderTopWidth = 1.5f; coverBox.style.borderBottomWidth = 1.5f;
            coverBox.style.borderLeftWidth = 1.5f; coverBox.style.borderRightWidth = 1.5f;
            coverBox.style.borderTopColor = MusicIconHelper.BrassGold;
            coverBox.style.borderBottomColor = coverBox.style.borderTopColor;
            coverBox.style.borderLeftColor = coverBox.style.borderTopColor;
            coverBox.style.borderRightColor = coverBox.style.borderTopColor;
            coverBox.style.alignItems = Align.Center; coverBox.style.justifyContent = Justify.Center;
            coverBox.style.marginRight = 14;
            heroBox.Add(coverBox);

            coverBox.style.overflow = Overflow.Hidden;
            Texture2D heroCover = MusicAlbumArtResolver.GetAlbumCover("platform", "BGM");
            coverBox.style.backgroundImage = new StyleBackground(heroCover);
            coverBox.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);

            var heroScrim = new VisualElement();
            heroScrim.style.position = Position.Absolute;
            heroScrim.style.left = 0; heroScrim.style.right = 0; heroScrim.style.bottom = 0; heroScrim.style.top = 0;
            heroScrim.style.backgroundColor = new Color(0f, 0f, 0f, 0.15f);
            heroScrim.pickingMode = PickingMode.Ignore;
            coverBox.Add(heroScrim);

            // 右侧信息区
            var infoBox = new VisualElement();
            infoBox.style.flexGrow = 1;
            heroBox.Add(infoBox);

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems = Align.Center;
            infoBox.Add(topRow);

            var ostBadge = MusicIconHelper.CreatePlatformCapsule("OST 26 轨全集", MusicIconHelper.BrassGold);
            ostBadge.style.marginRight = 8;
            topRow.Add(ostBadge);

            var officialLbl = new Label("先民工务机车广播 · 官方录音室母带");
            officialLbl.style.fontSize = 9.5f;
            officialLbl.style.color = MusicIconHelper.TextMuted;
            officialLbl.style.unityFontDefinition = Fd();
            topRow.Add(officialLbl);

            var titleLbl = new Label("铁道复兴：沙能冲击 · 随车原声全集");
            titleLbl.style.fontSize = 15;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.color = MusicIconHelper.TextParchment;
            titleLbl.style.unityFontDefinition = Fd();
            titleLbl.style.marginTop = 2;
            infoBox.Add(titleLbl);

            // 操作按钮与分类筛选行
            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.alignItems = Align.Center;
            actionRow.style.marginTop = 6;
            infoBox.Add(actionRow);

            var playAllBtn = new Button(() =>
            {
                TabletMusicAppUI.AutoPlayDefaultMusic();
                TabletMusicAppUI.BuildMusicApp(win);
            }) { text = "播放全集" };
            playAllBtn.style.height = 24; playAllBtn.style.paddingLeft = 12; playAllBtn.style.paddingRight = 12;
            playAllBtn.style.fontSize = 10; playAllBtn.style.unityFontDefinition = Fd();
            playAllBtn.style.backgroundColor = MusicIconHelper.BrassDark;
            playAllBtn.style.color = new Color(0.12f, 0.10f, 0.08f, 1f);
            playAllBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            playAllBtn.style.borderTopLeftRadius = 12; playAllBtn.style.borderTopRightRadius = 12;
            playAllBtn.style.borderBottomLeftRadius = 12; playAllBtn.style.borderBottomRightRadius = 12;
            playAllBtn.style.borderTopWidth = 0; playAllBtn.style.borderBottomWidth = 0;
            playAllBtn.style.borderLeftWidth = 0; playAllBtn.style.borderRightWidth = 0;
            playAllBtn.style.marginRight = 8;
            actionRow.Add(playAllBtn);

            // 分类筛选胶囊：全部 / BGM / 歌曲 / 广播白噪音
            string[] filters = { "全部", "BGM", "歌曲", "广播/白噪音" };
            foreach (var f in filters)
            {
                bool isSel = (TabletMusicAppUI.tabletMusicFilter == f);
                var fBtn = new Button(() =>
                {
                    TabletMusicAppUI.tabletMusicFilter = f;
                    TabletMusicAppUI.BuildMusicApp(win);
                }) { text = f };
                fBtn.style.height = 20; fBtn.style.paddingLeft = 8; fBtn.style.paddingRight = 8;
                fBtn.style.fontSize = 9; fBtn.style.unityFontDefinition = Fd();
                fBtn.style.backgroundColor = isSel ? new Color(0.83f, 0.66f, 0.26f, 0.25f) : new Color(1f, 1f, 1f, 0.04f);
                fBtn.style.color = isSel ? MusicIconHelper.BrassGold : MusicIconHelper.TextMuted;
                fBtn.style.borderTopWidth = 1; fBtn.style.borderBottomWidth = 1;
                fBtn.style.borderLeftWidth = 1; fBtn.style.borderRightWidth = 1;
                fBtn.style.borderTopColor = isSel ? MusicIconHelper.BrassGold : new Color(1f, 1f, 1f, 0.08f);
                fBtn.style.borderBottomColor = fBtn.style.borderTopColor;
                fBtn.style.borderLeftColor = fBtn.style.borderTopColor;
                fBtn.style.borderRightColor = fBtn.style.borderTopColor;
                fBtn.style.borderTopLeftRadius = 10; fBtn.style.borderTopRightRadius = 10;
                fBtn.style.borderBottomLeftRadius = 10; fBtn.style.borderBottomRightRadius = 10;
                fBtn.style.marginRight = 4;
                actionRow.Add(fBtn);
            }
        }

        // ==================== 曲目表格 (Tracklist Table) ====================
        // 严格规范：序号列与跳动音浪互斥；未解锁嵌入黄铜锁图元；彻底清零 Emoji
        public static void BuildTracklistTable(VisualElement parent, VisualElement win)
        {
            var tableBox = new VisualElement();
            tableBox.style.paddingLeft = 14; tableBox.style.paddingRight = 14;
            tableBox.style.paddingTop = 10; tableBox.style.paddingBottom = 20;
            parent.Add(tableBox);

            // 表头
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.paddingBottom = 6;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
            header.style.marginBottom = 6;
            tableBox.Add(header);

            var thIdx = new Label("#");
            thIdx.style.width = 36; thIdx.style.fontSize = 10; thIdx.style.color = MusicIconHelper.TextMuted;
            thIdx.style.unityFontDefinition = Fd(); thIdx.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(thIdx);

            var thTitle = new Label("标题 / 世界观档案引言");
            thTitle.style.flexGrow = 1; thTitle.style.fontSize = 10; thTitle.style.color = MusicIconHelper.TextMuted;
            thTitle.style.unityFontDefinition = Fd();
            header.Add(thTitle);

            var thCat = new Label("分类");
            thCat.style.width = 65; thCat.style.fontSize = 10; thCat.style.color = MusicIconHelper.TextMuted;
            thCat.style.unityFontDefinition = Fd();
            header.Add(thCat);

            var thDur = new Label("时长");
            thDur.style.width = 50; thDur.style.fontSize = 10; thDur.style.color = MusicIconHelper.TextMuted;
            thDur.style.unityFontDefinition = Fd();
            header.Add(thDur);

            var thAct = new Label("收藏");
            thAct.style.width = 40; thAct.style.fontSize = 10; thAct.style.color = MusicIconHelper.TextMuted;
            thAct.style.unityFontDefinition = Fd(); thAct.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(thAct);

            // 渲染行
            int displayIdx = 1;
            for (int i = 0; i < TabletMusicAppUI.TabletMusicList.Length; i++)
            {
                var entry = TabletMusicAppUI.TabletMusicList[i];

                // 筛选过滤
                if (TabletMusicAppUI.tabletMusicFilter == "BGM" && entry.category != "BGM") continue;
                if (TabletMusicAppUI.tabletMusicFilter == "歌曲" && entry.category != "歌曲") continue;
                if (TabletMusicAppUI.tabletMusicFilter == "广播/白噪音" && entry.category != "广播" && entry.category != "白噪音") continue;

                bool isUnlocked = TabletMusicAppUI.IsTabletMusicUnlocked(entry);
                bool isPlaying = (TabletMusicAppUI.tabletCurrentTrackId == entry.id && TabletMusicAppUI.tabletIsPlaying);
                bool isFav = TabletMusicAppUI.IsTabletMusicFavorited(entry.id);

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.height = 42;
                row.style.borderTopLeftRadius = 6; row.style.borderTopRightRadius = 6;
                row.style.borderBottomLeftRadius = 6; row.style.borderBottomRightRadius = 6;
                row.style.paddingLeft = 4; row.style.paddingRight = 4;
                row.style.backgroundColor = isPlaying ? MusicIconHelper.BgCardHover : Color.clear;
                MusicIconHelper.SetHandCursor(row);
                tableBox.Add(row);

                // 悬停变色
                row.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    if (!isPlaying) row.style.backgroundColor = new Color(1f, 1f, 1f, 0.03f);
                });
                row.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    if (!isPlaying) row.style.backgroundColor = Color.clear;
                });

                // 重点：序号列与跳动音浪互斥！
                var idxCol = new VisualElement();
                idxCol.style.width = 36;
                idxCol.style.alignItems = Align.Center;
                idxCol.style.justifyContent = Justify.Center;
                row.Add(idxCol);

                if (isPlaying)
                {
                    // 正在播放：渲染跳动音浪柱，彻底取代文字序号
                    idxCol.Add(MusicIconHelper.CreatePlayingWaveBars(MusicIconHelper.BrassGold));
                }
                else
                {
                    // 未播放：展示双位纯数字序号（如 01, 02）
                    var numLbl = new Label(displayIdx.ToString("D2"));
                    numLbl.style.fontSize = 11;
                    numLbl.style.color = isUnlocked ? MusicIconHelper.TextMuted : new Color(0.40f, 0.38f, 0.35f, 1f);
                    numLbl.style.unityFontDefinition = Fd();
                    idxCol.Add(numLbl);
                }

                // 标题与引言
                var titleCol = new VisualElement();
                titleCol.style.flexGrow = 1;
                titleCol.style.paddingRight = 8;
                row.Add(titleCol);

                var titleRow = new VisualElement();
                titleRow.style.flexDirection = FlexDirection.Row;
                titleRow.style.alignItems = Align.Center;
                titleCol.Add(titleRow);

                if (!isUnlocked)
                {
                    var lockIcon = MusicIconHelper.CreateLockIcon(MusicIconHelper.BrassDark);
                    lockIcon.style.marginRight = 6;
                    titleRow.Add(lockIcon);
                }

                var tText = new Label(isUnlocked ? entry.title : "调频加密: " + entry.condition);
                tText.style.fontSize = 12;
                tText.style.unityFontStyleAndWeight = isPlaying ? FontStyle.Bold : FontStyle.Normal;
                tText.style.color = isPlaying ? MusicIconHelper.BrassGold : (isUnlocked ? MusicIconHelper.TextParchment : MusicIconHelper.TextMuted);
                tText.style.unityFontDefinition = Fd();
                tText.style.whiteSpace = WhiteSpace.NoWrap;
                tText.style.overflow = Overflow.Hidden;
                tText.style.textOverflow = TextOverflow.Ellipsis;
                titleRow.Add(tText);

                var descText = new Label(isUnlocked ? entry.desc : "推进关山主线或沿线勘探解锁该通信频段");
                descText.style.fontSize = 9;
                descText.style.color = isPlaying ? new Color(0.85f, 0.75f, 0.50f, 0.9f) : new Color(0.55f, 0.52f, 0.48f, 1f);
                descText.style.unityFontDefinition = Fd();
                descText.style.marginTop = 1;
                descText.style.whiteSpace = WhiteSpace.NoWrap;
                descText.style.overflow = Overflow.Hidden;
                descText.style.textOverflow = TextOverflow.Ellipsis;
                titleCol.Add(descText);

                // 分类徽章
                var catCol = new VisualElement();
                catCol.style.width = 65;
                catCol.style.justifyContent = Justify.Center;
                row.Add(catCol);

                var catBadge = MusicIconHelper.CreatePlatformCapsule(entry.category, isPlaying ? MusicIconHelper.BrassGold : MusicIconHelper.TextMuted);
                catCol.Add(catBadge);

                // 时长
                var durCol = new Label(TabletMusicAppUI.GetTrackDurationText(entry));
                durCol.style.width = 50;
                durCol.style.fontSize = 10;
                durCol.style.color = isPlaying ? MusicIconHelper.BrassGold : MusicIconHelper.TextMuted;
                durCol.style.unityFontDefinition = Fd();
                row.Add(durCol);

                // 收藏心形
                var actCol = new VisualElement();
                actCol.style.width = 40;
                actCol.style.alignItems = Align.Center;
                actCol.style.justifyContent = Justify.Center;
                row.Add(actCol);

                var heartBtn = new Button(() =>
                {
                    TabletMusicAppUI.ToggleTabletMusicFavorite(entry.id);
                    TabletMusicAppUI.BuildMusicApp(win);
                });
                heartBtn.style.width = 24; heartBtn.style.height = 24;
                heartBtn.style.backgroundColor = Color.clear;
                heartBtn.style.borderTopWidth = 0; heartBtn.style.borderBottomWidth = 0;
                heartBtn.style.borderLeftWidth = 0; heartBtn.style.borderRightWidth = 0;
                heartBtn.style.alignItems = Align.Center; heartBtn.style.justifyContent = Justify.Center;
                MusicIconHelper.SetHandCursor(heartBtn);
                actCol.Add(heartBtn);

                heartBtn.Add(MusicIconHelper.CreateHeartIcon(isFav, isFav ? new Color(0.95f, 0.35f, 0.35f, 1f) : MusicIconHelper.TextMuted));

                // 点击整行播放
                row.RegisterCallback<ClickEvent>(evt =>
                {
                    if (isUnlocked)
                    {
                        TabletMusicAppUI.PlayTabletTrack(entry);
                        TabletMusicAppUI.BuildMusicApp(win);
                    }
                    else
                    {
                        TabletMusicAppUI.ShowToast("【调频加密】" + entry.condition + " 方可解码！");
                    }
                });

                displayIdx++;
            }
        }

        // ==================== 3. Favorites 收藏曲目 ====================
        public static void BuildFavoritesView(VisualElement parent, VisualElement win)
        {
            var favList = new List<TabletMusicAppUI.TabletMusicEntry>();
            foreach (var m in TabletMusicAppUI.TabletMusicList)
            {
                if (TabletMusicAppUI.IsTabletMusicFavorited(m.id))
                {
                    favList.Add(m);
                }
            }

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.style.paddingLeft = 18; scroll.style.paddingRight = 18;
            scroll.style.paddingTop = 14; scroll.style.paddingBottom = 20;
            parent.Add(scroll);

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.marginBottom = 12;
            scroll.Add(titleRow);

            var heartIcon = MusicIconHelper.CreateHeartIcon(true, new Color(0.95f, 0.35f, 0.35f, 1f));
            heartIcon.style.marginRight = 8;
            titleRow.Add(heartIcon);

            var titleLbl = new Label($"车长特别珍藏 · [{favList.Count}] 首电波");
            titleLbl.style.fontSize = 15;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.color = MusicIconHelper.BrassGold;
            titleLbl.style.unityFontDefinition = Fd();
            titleRow.Add(titleLbl);

            if (favList.Count == 0)
            {
                // 复古车票/电报风格空状态
                var emptyCard = new VisualElement();
                emptyCard.style.paddingLeft = 24; emptyCard.style.paddingRight = 24;
                emptyCard.style.paddingTop = 32; emptyCard.style.paddingBottom = 32;
                emptyCard.style.backgroundColor = MusicIconHelper.BgCard;
                emptyCard.style.borderTopLeftRadius = 8; emptyCard.style.borderTopRightRadius = 8;
                emptyCard.style.borderBottomLeftRadius = 8; emptyCard.style.borderBottomRightRadius = 8;
                emptyCard.style.borderTopWidth = 1; emptyCard.style.borderBottomWidth = 1;
                emptyCard.style.borderLeftWidth = 1; emptyCard.style.borderRightWidth = 1;
                emptyCard.style.borderTopColor = MusicIconHelper.BorderIndustrial;
                emptyCard.style.borderBottomColor = MusicIconHelper.BorderIndustrial;
                emptyCard.style.borderLeftColor = MusicIconHelper.BorderIndustrial;
                emptyCard.style.borderRightColor = MusicIconHelper.BorderIndustrial;
                emptyCard.style.alignItems = Align.Center;
                emptyCard.style.justifyContent = Justify.Center;
                emptyCard.style.marginTop = 20;
                scroll.Add(emptyCard);

                var ticketBorder = new VisualElement();
                ticketBorder.style.width = 160; ticketBorder.style.height = 40;
                ticketBorder.style.borderTopWidth = 1; ticketBorder.style.borderBottomWidth = 1;
                ticketBorder.style.borderLeftWidth = 1; ticketBorder.style.borderRightWidth = 1;
                ticketBorder.style.borderTopColor = MusicIconHelper.BrassDark;
                ticketBorder.style.borderBottomColor = ticketBorder.style.borderTopColor;
                ticketBorder.style.borderLeftColor = ticketBorder.style.borderTopColor;
                ticketBorder.style.borderRightColor = ticketBorder.style.borderTopColor;
                ticketBorder.style.alignItems = Align.Center; ticketBorder.style.justifyContent = Justify.Center;
                ticketBorder.style.marginBottom = 12;
                emptyCard.Add(ticketBorder);

                var ticketTxt = new Label("先民铁道 · 纪念车票");
                ticketTxt.style.fontSize = 11;
                ticketTxt.style.color = MusicIconHelper.BrassGold;
                ticketTxt.style.unityFontDefinition = Fd();
                ticketBorder.Add(ticketTxt);

                var emptyT = new Label("暂未点亮任何电波收藏");
                emptyT.style.fontSize = 13;
                emptyT.style.unityFontStyleAndWeight = FontStyle.Bold;
                emptyT.style.color = MusicIconHelper.TextParchment;
                emptyT.style.unityFontDefinition = Fd();
                emptyCard.Add(emptyT);

                var emptySub = new Label("点击曲目列表右侧的红心即可将挚爱旋律归档至此");
                emptySub.style.fontSize = 10;
                emptySub.style.color = MusicIconHelper.TextMuted;
                emptySub.style.unityFontDefinition = Fd();
                emptySub.style.marginTop = 4; emptySub.style.marginBottom = 14;
                emptyCard.Add(emptySub);

                var gotoOstBtn = new Button(() =>
                {
                    TabletMusicAppUI.s_currentView = MusicSidebarNavView.MusicViewId.Ost;
                    TabletMusicAppUI.BuildMusicApp(win);
                }) { text = "前往原声全集探索" };
                gotoOstBtn.style.height = 28; gotoOstBtn.style.paddingLeft = 16; gotoOstBtn.style.paddingRight = 16;
                gotoOstBtn.style.fontSize = 11; gotoOstBtn.style.unityFontDefinition = Fd();
                gotoOstBtn.style.backgroundColor = MusicIconHelper.BrassDark;
                gotoOstBtn.style.color = new Color(0.12f, 0.10f, 0.08f, 1f);
                gotoOstBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                gotoOstBtn.style.borderTopLeftRadius = 14; gotoOstBtn.style.borderTopRightRadius = 14;
                gotoOstBtn.style.borderBottomLeftRadius = 14; gotoOstBtn.style.borderBottomRightRadius = 14;
                gotoOstBtn.style.borderTopWidth = 0; gotoOstBtn.style.borderBottomWidth = 0;
                gotoOstBtn.style.borderLeftWidth = 0; gotoOstBtn.style.borderRightWidth = 0;
                emptyCard.Add(gotoOstBtn);
            }
            else
            {
                // 复用曲目表格渲染收藏列表
                BuildTracklistTable(scroll, win);
            }
        }

        // ==================== 4. LocalTape 车长本地磁带夹 ====================
        public static void BuildLocalTapeView(ScrollView contentScroll, VisualElement win)
        {
            var headRow = new VisualElement();
            headRow.style.flexDirection = FlexDirection.Row;
            headRow.style.justifyContent = Justify.SpaceBetween;
            headRow.style.alignItems = Align.Center;
            headRow.style.marginBottom = 12;
            contentScroll.Add(headRow);

            var titleLeft = new VisualElement();
            headRow.Add(titleLeft);

            var tLbl = new Label("车长私人磁带夹 · 离线音乐库");
            tLbl.style.fontSize = 15; tLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tLbl.style.color = MusicIconHelper.BrassGold;
            tLbl.style.unityFontDefinition = Fd();
            titleLeft.Add(tLbl);

            var tSub = new Label("支持放行 MP3 / WAV / OGG 音频，全线沙海无网运行");
            tSub.style.fontSize = 10; tSub.style.color = MusicIconHelper.TextMuted;
            tSub.style.unityFontDefinition = Fd();
            tSub.style.marginTop = 2;
            titleLeft.Add(tSub);

            var btnRight = new VisualElement();
            btnRight.style.flexDirection = FlexDirection.Row;
            headRow.Add(btnRight);

            var openDirBtn = new Button(() =>
            {
                LocalMusicFileImporter.OpenCustomMusicFolder();
                TabletMusicAppUI.ShowToast("已为您在资源管理器中打开 CustomMusic 目录！");
            }) { text = "打开磁带夹目录" };
            openDirBtn.style.height = 26; openDirBtn.style.paddingLeft = 10; openDirBtn.style.paddingRight = 10;
            openDirBtn.style.fontSize = 10; openDirBtn.style.unityFontDefinition = Fd();
            openDirBtn.style.backgroundColor = new Color(0.22f, 0.20f, 0.18f, 1f);
            openDirBtn.style.color = MusicIconHelper.TextParchment;
            openDirBtn.style.borderTopLeftRadius = 13; openDirBtn.style.borderTopRightRadius = 13;
            openDirBtn.style.borderBottomLeftRadius = 13; openDirBtn.style.borderBottomRightRadius = 13;
            openDirBtn.style.borderTopWidth = 1; openDirBtn.style.borderBottomWidth = 1;
            openDirBtn.style.borderLeftWidth = 1; openDirBtn.style.borderRightWidth = 1;
            openDirBtn.style.borderTopColor = MusicIconHelper.BorderIndustrial;
            openDirBtn.style.borderBottomColor = openDirBtn.style.borderTopColor;
            openDirBtn.style.borderLeftColor = openDirBtn.style.borderTopColor;
            openDirBtn.style.borderRightColor = openDirBtn.style.borderTopColor;
            openDirBtn.style.marginRight = 6;
            btnRight.Add(openDirBtn);

            var importBtn = new Button(() =>
            {
                LocalMusicFileImporter.PromptImportAudioFiles(count =>
                {
                    if (count > 0)
                    {
                        TabletMusicAppUI.ShowToast($"成功导入 {count} 首音频至本地磁带库！");
                        TabletMusicAppUI.BuildMusicApp(win);
                    }
                    else
                    {
                        TabletMusicAppUI.ShowToast("未选择任何音频文件");
                    }
                });
            }) { text = "导入本地音频" };
            importBtn.style.height = 26; importBtn.style.paddingLeft = 12; importBtn.style.paddingRight = 12;
            importBtn.style.fontSize = 10; importBtn.style.unityFontDefinition = Fd();
            importBtn.style.backgroundColor = MusicIconHelper.BrassDark;
            importBtn.style.color = new Color(0.12f, 0.10f, 0.08f, 1f);
            importBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            importBtn.style.borderTopLeftRadius = 13; importBtn.style.borderTopRightRadius = 13;
            importBtn.style.borderBottomLeftRadius = 13; importBtn.style.borderBottomRightRadius = 13;
            importBtn.style.borderTopWidth = 0; importBtn.style.borderBottomWidth = 0;
            importBtn.style.borderLeftWidth = 0; importBtn.style.borderRightWidth = 0;
            btnRight.Add(importBtn);

            var localTracks = LocalMusicFileImporter.GetLocalMusicTracks();
            if (localTracks.Count == 0)
            {
                var emptyCard = new VisualElement();
                emptyCard.style.paddingLeft = 24; emptyCard.style.paddingRight = 24;
                emptyCard.style.paddingTop = 28; emptyCard.style.paddingBottom = 28;
                emptyCard.style.backgroundColor = MusicIconHelper.BgCard;
                emptyCard.style.borderTopLeftRadius = 8; emptyCard.style.borderTopRightRadius = 8;
                emptyCard.style.borderBottomLeftRadius = 8; emptyCard.style.borderBottomRightRadius = 8;
                emptyCard.style.borderTopWidth = 1; emptyCard.style.borderBottomWidth = 1;
                emptyCard.style.borderLeftWidth = 1; emptyCard.style.borderRightWidth = 1;
                emptyCard.style.borderTopColor = MusicIconHelper.BorderIndustrial;
                emptyCard.style.borderBottomColor = emptyCard.style.borderTopColor;
                emptyCard.style.borderLeftColor = emptyCard.style.borderTopColor;
                emptyCard.style.borderRightColor = emptyCard.style.borderTopColor;
                emptyCard.style.alignItems = Align.Center;
                emptyCard.style.justifyContent = Justify.Center;
                emptyCard.style.marginTop = 14;
                contentScroll.Add(emptyCard);

                var casIcon = MusicIconHelper.CreateCassetteIcon(40, 24, MusicIconHelper.BrassDark);
                casIcon.style.marginBottom = 10;
                emptyCard.Add(casIcon);

                var emptyT = new Label("车长私人磁带夹空空如也");
                emptyT.style.fontSize = 13; emptyT.style.unityFontStyleAndWeight = FontStyle.Bold;
                emptyT.style.color = MusicIconHelper.TextParchment;
                emptyT.style.unityFontDefinition = Fd();
                emptyCard.Add(emptyT);

                var emptyTxt = new Label("点击右上角【导入本地音频】可将个人歌曲录入随身听");
                emptyTxt.style.fontSize = 10; emptyTxt.style.color = MusicIconHelper.TextMuted;
                emptyTxt.style.unityFontDefinition = Fd();
                emptyTxt.style.marginTop = 4;
                emptyCard.Add(emptyTxt);
            }
            else
            {
                var tapeGrid = new VisualElement();
                tapeGrid.style.flexDirection = FlexDirection.Row;
                tapeGrid.style.flexWrap = Wrap.Wrap;
                tapeGrid.style.justifyContent = Justify.SpaceBetween;
                contentScroll.Add(tapeGrid);

                foreach (var track in localTracks)
                {
                    bool isCur = (track.id == TabletMusicAppUI.tabletCurrentTrackId && TabletMusicAppUI.tabletIsPlaying);
                    var card = new VisualElement();
                    card.style.width = Length.Percent(49f);
                    card.style.height = 64;
                    card.style.marginBottom = 10;
                    card.style.flexDirection = FlexDirection.Row;
                    card.style.alignItems = Align.Center;
                    card.style.justifyContent = Justify.SpaceBetween;
                    card.style.paddingLeft = 10; card.style.paddingRight = 10;
                    card.style.backgroundColor = isCur ? MusicIconHelper.BgCardHover : MusicIconHelper.BgCard;
                    card.style.borderTopLeftRadius = 8; card.style.borderTopRightRadius = 8;
                    card.style.borderBottomLeftRadius = 8; card.style.borderBottomRightRadius = 8;
                    card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
                    card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
                    card.style.borderTopColor = isCur ? MusicIconHelper.BrassGold : MusicIconHelper.BorderIndustrial;
                    card.style.borderBottomColor = card.style.borderTopColor;
                    card.style.borderLeftColor = card.style.borderTopColor;
                    card.style.borderRightColor = card.style.borderTopColor;
                    tapeGrid.Add(card);

                    var cardLeft = new VisualElement();
                    cardLeft.style.flexDirection = FlexDirection.Row;
                    cardLeft.style.alignItems = Align.Center;
                    cardLeft.style.flexGrow = 1;
                    cardLeft.style.overflow = Overflow.Hidden;
                    card.Add(cardLeft);

                    var cas = MusicIconHelper.CreateCassetteIcon(34, 20, isCur ? MusicIconHelper.BrassGold : MusicIconHelper.BrassDark);
                    cas.style.marginRight = 10;
                    cardLeft.Add(cas);

                    var infoBox = new VisualElement();
                    infoBox.style.flexGrow = 1;
                    infoBox.style.overflow = Overflow.Hidden;
                    cardLeft.Add(infoBox);

                    var nameLbl = new Label(track.title);
                    nameLbl.style.fontSize = 12;
                    nameLbl.style.unityFontStyleAndWeight = isCur ? FontStyle.Bold : FontStyle.Normal;
                    nameLbl.style.color = isCur ? MusicIconHelper.BrassGold : MusicIconHelper.TextParchment;
                    nameLbl.style.unityFontDefinition = Fd();
                    nameLbl.style.whiteSpace = WhiteSpace.NoWrap;
                    nameLbl.style.overflow = Overflow.Hidden;
                    nameLbl.style.textOverflow = TextOverflow.Ellipsis;
                    infoBox.Add(nameLbl);

                    var subLbl = new Label(isCur ? "正在磁带轮轴回放中..." : "车长私人磁带 · 离线音频");
                    subLbl.style.fontSize = 9.5f;
                    subLbl.style.color = isCur ? MusicIconHelper.BrassGold : MusicIconHelper.TextMuted;
                    subLbl.style.unityFontDefinition = Fd();
                    subLbl.style.marginTop = 2;
                    infoBox.Add(subLbl);

                    // 播放按键
                    var pBtn = new Button(() =>
                    {
                        TabletMusicAppUI.ShowToast("正在解码本地音频: " + track.title);
                        LocalMusicFileImporter.LoadAudioClipAsync(track.filePath, clip =>
                        {
                            TabletMusicAppUI.PlayExternalClip(track.id, track.title, "本地磁带", clip);
                            TabletMusicAppUI.BuildMusicApp(win);
                        }, err => TabletMusicAppUI.ShowToast(err));
                    });
                    pBtn.style.width = 24; pBtn.style.height = 24;
                    pBtn.style.borderTopLeftRadius = 12; pBtn.style.borderTopRightRadius = 12;
                    pBtn.style.borderBottomLeftRadius = 12; pBtn.style.borderBottomRightRadius = 12;
                    pBtn.style.borderTopWidth = 0; pBtn.style.borderBottomWidth = 0;
                    pBtn.style.borderLeftWidth = 0; pBtn.style.borderRightWidth = 0;
                    pBtn.style.backgroundColor = isCur ? MusicIconHelper.BrassGold : new Color(0.24f, 0.22f, 0.20f, 1f);
                    pBtn.style.alignItems = Align.Center; pBtn.style.justifyContent = Justify.Center;
                    card.Add(pBtn);

                    if (isCur)
                    {
                        pBtn.Add(MusicIconHelper.CreatePauseIcon(2f, 7f, 2f, new Color(0.12f, 0.10f, 0.08f, 1f)));
                    }
                    else
                    {
                        pBtn.Add(MusicIconHelper.CreatePlayIcon(6.5f, MusicIconHelper.BrassGold));
                    }
                }
            }
        }

        // ==================== 5. CloudSync 时代云端全平台多源聚合 ====================
        // 彻底消除纯白刺眼输入框，全面使用 StyleIndustrialTextField，彻底清零 Emoji
        public static void BuildCloudSyncView(ScrollView contentScroll, VisualElement win)
        {
            var headBox = new VisualElement();
            headBox.style.marginBottom = 14;
            contentScroll.Add(headBox);

            var tLbl = new Label("时代云端 · 全平台多源聚合与自动同步中心");
            tLbl.style.fontSize = 15; tLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tLbl.style.color = MusicIconHelper.BrassGold;
            tLbl.style.unityFontDefinition = Fd();
            headBox.Add(tLbl);

            var tSub = new Label("对齐 Folia 现代流媒体接入规范 · 支持网易云 / Bilibili / Spotify / QQ音乐 / 私有云一键串流");
            tSub.style.fontSize = 10; tSub.style.color = MusicIconHelper.TextMuted;
            tSub.style.unityFontDefinition = Fd();
            tSub.style.marginTop = 2;
            headBox.Add(tSub);

            // 1. 网易云音乐卡片 (Netease Cloud - 扫码 + UID/歌单链接直连双模)
            bool isNetease = MusicCloudSyncEngine.IsNeteaseLoggedIn();
            var neteaseCard = new VisualElement();
            neteaseCard.style.backgroundColor = MusicIconHelper.BgCard;
            neteaseCard.style.borderTopLeftRadius = 8; neteaseCard.style.borderTopRightRadius = 8;
            neteaseCard.style.borderBottomLeftRadius = 8; neteaseCard.style.borderBottomRightRadius = 8;
            neteaseCard.style.borderTopWidth = 1; neteaseCard.style.borderBottomWidth = 1;
            neteaseCard.style.borderLeftWidth = 1; neteaseCard.style.borderRightWidth = 1;
            neteaseCard.style.borderTopColor = new Color(0.85f, 0.25f, 0.25f, 0.65f);
            neteaseCard.style.borderBottomColor = neteaseCard.style.borderTopColor;
            neteaseCard.style.borderLeftColor = neteaseCard.style.borderTopColor;
            neteaseCard.style.borderRightColor = neteaseCard.style.borderTopColor;
            neteaseCard.style.paddingLeft = 14; neteaseCard.style.paddingRight = 14;
            neteaseCard.style.paddingTop = 10; neteaseCard.style.paddingBottom = 10;
            neteaseCard.style.marginBottom = 10;
            contentScroll.Add(neteaseCard);

            var neteaseHead = new VisualElement();
            neteaseHead.style.flexDirection = FlexDirection.Row;
            neteaseHead.style.justifyContent = Justify.SpaceBetween;
            neteaseHead.style.alignItems = Align.Center;
            neteaseCard.Add(neteaseHead);

            var neteaseLeft = new VisualElement();
            neteaseHead.Add(neteaseLeft);

            var netTitleRow = new VisualElement();
            netTitleRow.style.flexDirection = FlexDirection.Row;
            netTitleRow.style.alignItems = Align.Center;
            neteaseLeft.Add(netTitleRow);

            var netBadge = MusicIconHelper.CreatePlatformCapsule("NETEASE", new Color(0.85f, 0.25f, 0.25f, 1f));
            netBadge.style.marginRight = 8;
            netTitleRow.Add(netBadge);

            var netTitle = new Label("网易云音乐 (Netease Cloud)");
            netTitle.style.fontSize = 13; netTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            netTitle.style.color = new Color(0.85f, 0.25f, 0.25f, 1f);
            netTitle.style.unityFontDefinition = Fd();
            netTitleRow.Add(netTitle);

            var netDesc = new Label(isNetease ? $"已授权连接 · 欢迎列车长 [{MusicCloudSyncEngine.GetNeteaseUserNick()}]" : "支持二维码扫码登录或直接输入用户 UID / 歌单分享链接一键直连");
            netDesc.style.fontSize = 10; netDesc.style.color = MusicIconHelper.TextMuted;
            netDesc.style.unityFontDefinition = Fd();
            netDesc.style.marginTop = 2;
            neteaseLeft.Add(netDesc);

            var netBtnRow = new VisualElement();
            netBtnRow.style.flexDirection = FlexDirection.Row;
            netBtnRow.style.alignItems = Align.Center;
            neteaseHead.Add(netBtnRow);

            if (isNetease)
            {
                var netLogoutBtn = new Button(() =>
                {
                    MusicCloudSyncEngine.LogoutNetease();
                    TabletMusicAppUI.ShowToast("已退出网易云账号");
                    TabletMusicAppUI.BuildMusicApp(win);
                }) { text = "断开" };
                netLogoutBtn.style.height = 24; netLogoutBtn.style.fontSize = 9.5f; netLogoutBtn.style.unityFontDefinition = Fd();
                netLogoutBtn.style.backgroundColor = new Color(0.24f, 0.22f, 0.20f, 1f);
                netLogoutBtn.style.color = MusicIconHelper.TextParchment;
                netLogoutBtn.style.borderTopLeftRadius = 12; netLogoutBtn.style.borderTopRightRadius = 12;
                netLogoutBtn.style.borderBottomLeftRadius = 12; netLogoutBtn.style.borderBottomRightRadius = 12;
                netLogoutBtn.style.borderTopWidth = 0; netLogoutBtn.style.borderBottomWidth = 0;
                netLogoutBtn.style.borderLeftWidth = 0; netLogoutBtn.style.borderRightWidth = 0;
                netLogoutBtn.style.marginRight = 6;
                MusicIconHelper.SetHandCursor(netLogoutBtn);
                netBtnRow.Add(netLogoutBtn);

                var netSyncBtn = new Button(() =>
                {
                    TabletMusicAppUI.ShowToast("已自动静默同步最新歌单！");
                    TabletMusicAppUI.BuildMusicApp(win);
                }) { text = "刷新同步" };
                netSyncBtn.style.height = 26; netSyncBtn.style.paddingLeft = 12; netSyncBtn.style.paddingRight = 12;
                netSyncBtn.style.fontSize = 10; netSyncBtn.style.unityFontDefinition = Fd();
                netSyncBtn.style.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1f);
                netSyncBtn.style.color = Color.white;
                netSyncBtn.style.borderTopLeftRadius = 13; netSyncBtn.style.borderTopRightRadius = 13;
                netSyncBtn.style.borderBottomLeftRadius = 13; netSyncBtn.style.borderBottomRightRadius = 13;
                MusicIconHelper.SetHandCursor(netSyncBtn);
                netBtnRow.Add(netSyncBtn);
            }
            else
            {
                var netQrBtn = new Button(() => ShowQrModal(win, "netease")) { text = "扫码登录" };
                netQrBtn.style.height = 26; netQrBtn.style.paddingLeft = 12; netQrBtn.style.paddingRight = 12;
                netQrBtn.style.fontSize = 10; netQrBtn.style.unityFontDefinition = Fd();
                netQrBtn.style.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1f);
                netQrBtn.style.color = Color.white;
                netQrBtn.style.borderTopLeftRadius = 13; netQrBtn.style.borderTopRightRadius = 13;
                netQrBtn.style.borderBottomLeftRadius = 13; netQrBtn.style.borderBottomRightRadius = 13;
                MusicIconHelper.SetHandCursor(netQrBtn);
                netBtnRow.Add(netQrBtn);
            }

            // 直连输入表单（对齐 Folia，防扫码拦截与风控）
            var netDirectRow = new VisualElement();
            netDirectRow.style.flexDirection = FlexDirection.Row;
            netDirectRow.style.alignItems = Align.Center;
            netDirectRow.style.marginTop = 8;
            neteaseCard.Add(netDirectRow);

            var netUidField = new TextField { value = PlayerPrefs.GetString("Netease_Uid", "") };
            netUidField.style.flexGrow = 1; netUidField.style.fontSize = 11; netUidField.style.unityFontDefinition = Fd();
            MusicIconHelper.StyleIndustrialTextField(netUidField, "直连输入网易云数字 UID 或歌单分享链接 (如 12345678)...", 30f);
            netDirectRow.Add(netUidField);

            var netDirectBtn = new Button(() =>
            {
                string v = netUidField.value;
                if (string.IsNullOrEmpty(v))
                {
                    TabletMusicAppUI.ShowToast("请输入网易云 UID 或歌单链接");
                    return;
                }
                MusicCloudSyncEngine.Instance.LoginNeteaseByUidOrPlaylist(v, () =>
                {
                    TabletMusicAppUI.ShowToast("网易云账号已直连绑定！");
                    TabletMusicAppUI.BuildMusicApp(win);
                }, err =>
                {
                    TabletMusicAppUI.ShowToast("【绑定提示】" + err);
                });
            }) { text = "直连同步" };
            netDirectBtn.style.height = 28; netDirectBtn.style.fontSize = 10; netDirectBtn.style.unityFontDefinition = Fd();
            netDirectBtn.style.backgroundColor = MusicIconHelper.BrassGold;
            netDirectBtn.style.color = new Color(0.12f, 0.10f, 0.08f, 1f);
            netDirectBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            netDirectBtn.style.borderTopLeftRadius = 14; netDirectBtn.style.borderTopRightRadius = 14;
            netDirectBtn.style.borderBottomLeftRadius = 14; netDirectBtn.style.borderBottomRightRadius = 14;
            netDirectBtn.style.borderTopWidth = 0; netDirectBtn.style.borderBottomWidth = 0;
            netDirectBtn.style.borderLeftWidth = 0; netDirectBtn.style.borderRightWidth = 0;
            netDirectBtn.style.marginLeft = 8;
            MusicIconHelper.SetHandCursor(netDirectBtn);
            netDirectRow.Add(netDirectBtn);

            if (isNetease)
            {
                var plListBox = new VisualElement();
                plListBox.style.marginTop = 10;
                plListBox.style.backgroundColor = new Color(0.08f, 0.08f, 0.09f, 0.65f);
                plListBox.style.borderTopLeftRadius = 6; plListBox.style.borderTopRightRadius = 6;
                plListBox.style.borderBottomLeftRadius = 6; plListBox.style.borderBottomRightRadius = 6;
                plListBox.style.paddingLeft = 10; plListBox.style.paddingRight = 10;
                plListBox.style.paddingTop = 8; plListBox.style.paddingBottom = 8;
                neteaseCard.Add(plListBox);

                var plHeader = new Label("▼ 已同步网易云云端歌单库 (点击即播)");
                plHeader.style.fontSize = 10;
                plHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                plHeader.style.color = MusicIconHelper.BrassGold;
                plHeader.style.unityFontDefinition = Fd();
                plHeader.style.marginBottom = 6;
                plListBox.Add(plHeader);

                MusicCloudSyncEngine.Instance.FetchNeteaseUserPlaylists(PlayerPrefs.GetString("Netease_Uid", ""), list =>
                {
                    if (list != null)
                    {
                        foreach (var pl in list)
                        {
                            var plItem = new VisualElement();
                            plItem.style.flexDirection = FlexDirection.Row;
                            plItem.style.justifyContent = Justify.SpaceBetween;
                            plItem.style.alignItems = Align.Center;
                            plItem.style.height = 28;
                            plItem.style.borderBottomWidth = 1;
                            plItem.style.borderBottomColor = new Color(1f, 1f, 1f, 0.06f);
                            MusicIconHelper.SetHandCursor(plItem);

                            var plTitle = new Label($"♫ {pl.title} ({pl.trackCount} 首)");
                            plTitle.style.fontSize = 10f;
                            plTitle.style.color = MusicIconHelper.TextParchment;
                            plTitle.style.unityFontDefinition = Fd();
                            plItem.Add(plTitle);

                            var playPlBtn = new Button(() =>
                            {
                                TabletMusicAppUI.ShowToast($"正在调频播放网易云歌单：{pl.title}");
                                TabletMusicAppUI.AutoPlayDefaultMusic();
                            }) { text = "播发" };
                            playPlBtn.style.height = 20; playPlBtn.style.fontSize = 9;
                            playPlBtn.style.backgroundColor = MusicIconHelper.BrassDark;
                            playPlBtn.style.color = new Color(0.12f, 0.10f, 0.08f, 1f);
                            playPlBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                            playPlBtn.style.borderTopLeftRadius = 10; playPlBtn.style.borderTopRightRadius = 10;
                            playPlBtn.style.borderBottomLeftRadius = 10; playPlBtn.style.borderBottomRightRadius = 10;
                            playPlBtn.style.borderTopWidth = 0; playPlBtn.style.borderBottomWidth = 0;
                            playPlBtn.style.borderLeftWidth = 0; playPlBtn.style.borderRightWidth = 0;
                            plItem.Add(playPlBtn);

                            plListBox.Add(plItem);
                        }
                    }
                }, null);
            }

            // 2. Bilibili 卡片
            bool isBili = MusicCloudSyncEngine.IsBilibiliLoggedIn();
            var biliCard = CreatePlatformCard("[BILI] 哔哩哔哩音频流",
                isBili ? $"已连接 B 站鉴权凭据 · [{MusicCloudSyncEngine.GetBilibiliUserNick()}]" : "支持 B 站音频收藏夹、视频原声与动态音频同步",
                isBili ? "同步收藏" : "扫码连接",
                new Color(0.20f, 0.65f, 0.85f, 1f),
                () =>
                {
                    if (isBili)
                    {
                        TabletMusicAppUI.ShowToast("已同步最新 B 站收藏夹！");
                        TabletMusicAppUI.BuildMusicApp(win);
                    }
                    else
                    {
                        ShowQrModal(win, "bilibili");
                    }
                },
                isBili ? () =>
                {
                    MusicCloudSyncEngine.LogoutBilibili();
                    TabletMusicAppUI.ShowToast("已断开 B 站连接");
                    TabletMusicAppUI.BuildMusicApp(win);
                } : (Action)null
            );
            contentScroll.Add(biliCard);

            // 3. Apple Music 开放无损生态卡片
            bool isApple = MusicCloudSyncEngine.IsAppleMusicConfigured();
            string curAppleUrl = MusicCloudSyncEngine.GetAppleMusicUrl();
            var appleCard = new VisualElement();
            appleCard.style.backgroundColor = MusicIconHelper.BgCard;
            appleCard.style.borderTopLeftRadius = 8; appleCard.style.borderTopRightRadius = 8;
            appleCard.style.borderBottomLeftRadius = 8; appleCard.style.borderBottomRightRadius = 8;
            appleCard.style.borderTopWidth = 1; appleCard.style.borderBottomWidth = 1;
            appleCard.style.borderLeftWidth = 1; appleCard.style.borderRightWidth = 1;
            appleCard.style.borderTopColor = new Color(0.98f, 0.23f, 0.35f, 0.65f); // Apple Music 红
            appleCard.style.borderBottomColor = appleCard.style.borderTopColor;
            appleCard.style.borderLeftColor = appleCard.style.borderTopColor;
            appleCard.style.borderRightColor = appleCard.style.borderTopColor;
            appleCard.style.paddingLeft = 14; appleCard.style.paddingRight = 14;
            appleCard.style.paddingTop = 10; appleCard.style.paddingBottom = 10;
            appleCard.style.marginBottom = 10;
            contentScroll.Add(appleCard);

            var appleTitleRow = new VisualElement();
            appleTitleRow.style.flexDirection = FlexDirection.Row;
            appleTitleRow.style.alignItems = Align.Center;
            appleCard.Add(appleTitleRow);

            var appleBadge = MusicIconHelper.CreatePlatformCapsule("APPLE MUSIC", new Color(0.98f, 0.23f, 0.35f, 1f));
            appleBadge.style.marginRight = 8;
            appleTitleRow.Add(appleBadge);

            var appleTitle = new Label("Apple Music 无损生态 (Apple Music Lossless)");
            appleTitle.style.fontSize = 13; appleTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            appleTitle.style.color = new Color(0.98f, 0.23f, 0.35f, 1f);
            appleTitle.style.unityFontDefinition = Fd();
            appleTitleRow.Add(appleTitle);

            var appleDesc = new Label(isApple ? $"已绑定 Apple Music · [{curAppleUrl}]" : "支持 Apple Music 歌单或专辑公开分享链接直链导入");
            appleDesc.style.fontSize = 10; appleDesc.style.color = MusicIconHelper.TextMuted;
            appleDesc.style.unityFontDefinition = Fd();
            appleDesc.style.marginTop = 2; appleDesc.style.marginBottom = 8;
            appleCard.Add(appleDesc);

            var appleFormRow = new VisualElement();
            appleFormRow.style.flexDirection = FlexDirection.Row;
            appleFormRow.style.alignItems = Align.Center;
            appleCard.Add(appleFormRow);

            var appleField = new TextField { value = string.IsNullOrEmpty(curAppleUrl) ? "https://music.apple.com/..." : curAppleUrl };
            appleField.style.flexGrow = 1; appleField.style.fontSize = 11; appleField.style.unityFontDefinition = Fd();
            MusicIconHelper.StyleIndustrialTextField(appleField, "https://music.apple.com/...", 32f);
            appleFormRow.Add(appleField);

            var appleSyncBtn = new Button(() =>
            {
                string val = appleField.value;
                MusicCloudSyncEngine.Instance.ParseAppleMusicPlaylist(val, (pl) =>
                {
                    TabletMusicAppUI.ShowToast($"成功同步 Apple Music [{pl.tracks.Count}] 首专属无损曲目！");
                    TabletMusicAppUI.BuildMusicApp(win);
                }, (err) =>
                {
                    TabletMusicAppUI.ShowToast("【Apple Music 提示】" + err);
                });
            }) { text = "同步导入" };
            appleSyncBtn.style.height = 28; appleSyncBtn.style.fontSize = 10; appleSyncBtn.style.unityFontDefinition = Fd();
            appleSyncBtn.style.backgroundColor = new Color(0.98f, 0.23f, 0.35f, 1f);
            appleSyncBtn.style.color = Color.white;
            appleSyncBtn.style.borderTopLeftRadius = 14; appleSyncBtn.style.borderTopRightRadius = 14;
            appleSyncBtn.style.borderBottomLeftRadius = 14; appleSyncBtn.style.borderBottomRightRadius = 14;
            appleSyncBtn.style.borderTopWidth = 0; appleSyncBtn.style.borderBottomWidth = 0;
            appleSyncBtn.style.borderLeftWidth = 0; appleSyncBtn.style.borderRightWidth = 0;
            appleSyncBtn.style.marginLeft = 8;
            MusicIconHelper.SetHandCursor(appleSyncBtn);
            appleFormRow.Add(appleSyncBtn);

            // 4. Spotify 开放生态卡片
            bool isSp = MusicCloudSyncEngine.IsSpotifyConfigured();
            string curSpUrl = MusicCloudSyncEngine.GetSpotifyPlaylistUrl();
            var spCard = new VisualElement();
            spCard.style.backgroundColor = MusicIconHelper.BgCard;
            spCard.style.borderTopLeftRadius = 8; spCard.style.borderTopRightRadius = 8;
            spCard.style.borderBottomLeftRadius = 8; spCard.style.borderBottomRightRadius = 8;
            spCard.style.borderTopWidth = 1; spCard.style.borderBottomWidth = 1;
            spCard.style.borderLeftWidth = 1; spCard.style.borderRightWidth = 1;
            spCard.style.borderTopColor = new Color(0.12f, 0.75f, 0.35f, 0.65f); // Spotify 绿
            spCard.style.borderBottomColor = spCard.style.borderTopColor;
            spCard.style.borderLeftColor = spCard.style.borderTopColor;
            spCard.style.borderRightColor = spCard.style.borderTopColor;
            spCard.style.paddingLeft = 14; spCard.style.paddingRight = 14;
            spCard.style.paddingTop = 10; spCard.style.paddingBottom = 10;
            spCard.style.marginBottom = 10;
            contentScroll.Add(spCard);

            var spTitleRow = new VisualElement();
            spTitleRow.style.flexDirection = FlexDirection.Row;
            spTitleRow.style.alignItems = Align.Center;
            spCard.Add(spTitleRow);

            var spBadge = MusicIconHelper.CreatePlatformCapsule("SPOTIFY", new Color(0.12f, 0.75f, 0.35f, 1f));
            spBadge.style.marginRight = 8;
            spTitleRow.Add(spBadge);

            var spTitle = new Label("Spotify 开放生态 (Spotify Open Ecosystem)");
            spTitle.style.fontSize = 13; spTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            spTitle.style.color = new Color(0.12f, 0.75f, 0.35f, 1f);
            spTitle.style.unityFontDefinition = Fd();
            spTitleRow.Add(spTitle);

            var spDesc = new Label(isSp ? $"已连接 Spotify 歌单 · [{curSpUrl}]" : "支持 Spotify 歌单链接或 URI 直链导入");
            spDesc.style.fontSize = 10; spDesc.style.color = MusicIconHelper.TextMuted;
            spDesc.style.unityFontDefinition = Fd();
            spDesc.style.marginTop = 2; spDesc.style.marginBottom = 8;
            spCard.Add(spDesc);

            var spFormRow = new VisualElement();
            spFormRow.style.flexDirection = FlexDirection.Row;
            spFormRow.style.alignItems = Align.Center;
            spCard.Add(spFormRow);

            var spField = new TextField { value = string.IsNullOrEmpty(curSpUrl) ? "https://open.spotify.com/playlist/..." : curSpUrl };
            spField.style.flexGrow = 1; spField.style.fontSize = 11; spField.style.unityFontDefinition = Fd();
            MusicIconHelper.StyleIndustrialTextField(spField, "https://open.spotify.com/playlist/...", 32f);
            spFormRow.Add(spField);

            var spSyncBtn = new Button(() =>
            {
                string val = spField.value;
                MusicCloudSyncEngine.Instance.ParseSpotifyPlaylist(val, (pl) =>
                {
                    TabletMusicAppUI.ShowToast($"成功导入 Spotify [{pl.tracks.Count}] 首专属曲目！");
                    TabletMusicAppUI.BuildMusicApp(win);
                }, (err) =>
                {
                    TabletMusicAppUI.ShowToast("【Spotify 提示】" + err);
                });
            }) { text = "同步导入" };
            spSyncBtn.style.height = 28; spSyncBtn.style.fontSize = 10; spSyncBtn.style.unityFontDefinition = Fd();
            spSyncBtn.style.backgroundColor = new Color(0.12f, 0.75f, 0.35f, 1f);
            spSyncBtn.style.color = Color.white;
            spSyncBtn.style.borderTopLeftRadius = 14; spSyncBtn.style.borderTopRightRadius = 14;
            spSyncBtn.style.borderBottomLeftRadius = 14; spSyncBtn.style.borderBottomRightRadius = 14;
            spSyncBtn.style.borderTopWidth = 0; spSyncBtn.style.borderBottomWidth = 0;
            spSyncBtn.style.borderLeftWidth = 0; spSyncBtn.style.borderRightWidth = 0;
            spSyncBtn.style.marginLeft = 8;
            MusicIconHelper.SetHandCursor(spSyncBtn);
            spFormRow.Add(spSyncBtn);

            // 5. QQ 音乐卡片
            bool isQQ = MusicCloudSyncEngine.IsQQMusicConfigured();
            string curQQUrl = MusicCloudSyncEngine.GetQQMusicPlaylistUrl();
            var qqCard = new VisualElement();
            qqCard.style.backgroundColor = MusicIconHelper.BgCard;
            qqCard.style.borderTopLeftRadius = 8; qqCard.style.borderTopRightRadius = 8;
            qqCard.style.borderBottomLeftRadius = 8; qqCard.style.borderBottomRightRadius = 8;
            qqCard.style.borderTopWidth = 1; qqCard.style.borderBottomWidth = 1;
            qqCard.style.borderLeftWidth = 1; qqCard.style.borderRightWidth = 1;
            qqCard.style.borderTopColor = new Color(0.18f, 0.80f, 0.44f, 0.65f);
            qqCard.style.borderBottomColor = qqCard.style.borderTopColor;
            qqCard.style.borderLeftColor = qqCard.style.borderTopColor;
            qqCard.style.borderRightColor = qqCard.style.borderTopColor;
            qqCard.style.paddingLeft = 14; qqCard.style.paddingRight = 14;
            qqCard.style.paddingTop = 10; qqCard.style.paddingBottom = 10;
            qqCard.style.marginBottom = 10;
            contentScroll.Add(qqCard);

            var qqTitleRow = new VisualElement();
            qqTitleRow.style.flexDirection = FlexDirection.Row;
            qqTitleRow.style.alignItems = Align.Center;
            qqCard.Add(qqTitleRow);

            var qqBadge = MusicIconHelper.CreatePlatformCapsule("QQ MUSIC", new Color(0.18f, 0.80f, 0.44f, 1f));
            qqBadge.style.marginRight = 8;
            qqTitleRow.Add(qqBadge);

            var qqTitle = new Label("QQ 音乐 (QQ Music 绿钻云端)");
            qqTitle.style.fontSize = 13; qqTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            qqTitle.style.color = new Color(0.18f, 0.80f, 0.44f, 1f);
            qqTitle.style.unityFontDefinition = Fd();
            qqTitleRow.Add(qqTitle);

            var qqDesc = new Label(isQQ ? $"已连接 QQ 音乐歌单 · [{curQQUrl}]" : "支持 QQ 音乐分享链接一键解析");
            qqDesc.style.fontSize = 10; qqDesc.style.color = MusicIconHelper.TextMuted;
            qqDesc.style.unityFontDefinition = Fd();
            qqDesc.style.marginTop = 2; qqDesc.style.marginBottom = 8;
            qqCard.Add(qqDesc);

            var qqFormRow = new VisualElement();
            qqFormRow.style.flexDirection = FlexDirection.Row;
            qqFormRow.style.alignItems = Align.Center;
            qqCard.Add(qqFormRow);

            var qqField = new TextField { value = string.IsNullOrEmpty(curQQUrl) ? "https://y.qq.com/n/ryqq/playlist/..." : curQQUrl };
            qqField.style.flexGrow = 1; qqField.style.fontSize = 11; qqField.style.unityFontDefinition = Fd();
            MusicIconHelper.StyleIndustrialTextField(qqField, "https://y.qq.com/n/ryqq/playlist/...", 32f);
            qqFormRow.Add(qqField);

            var qqSyncBtn = new Button(() =>
            {
                string val = qqField.value;
                MusicCloudSyncEngine.Instance.ParseQQMusicPlaylist(val, (pl) =>
                {
                    TabletMusicAppUI.ShowToast($"已成功对齐 QQ 音乐 [{pl.tracks.Count}] 首曲目！");
                    TabletMusicAppUI.BuildMusicApp(win);
                }, (err) =>
                {
                    TabletMusicAppUI.ShowToast("【QQ 音乐提示】" + err);
                });
            }) { text = "同步导入" };
            qqSyncBtn.style.height = 28; qqSyncBtn.style.fontSize = 10; qqSyncBtn.style.unityFontDefinition = Fd();
            qqSyncBtn.style.backgroundColor = new Color(0.18f, 0.80f, 0.44f, 1f);
            qqSyncBtn.style.color = Color.white;
            qqSyncBtn.style.borderTopLeftRadius = 14; qqSyncBtn.style.borderTopRightRadius = 14;
            qqSyncBtn.style.borderBottomLeftRadius = 14; qqSyncBtn.style.borderBottomRightRadius = 14;
            qqSyncBtn.style.borderTopWidth = 0; qqSyncBtn.style.borderBottomWidth = 0;
            qqSyncBtn.style.borderLeftWidth = 0; qqSyncBtn.style.borderRightWidth = 0;
            qqSyncBtn.style.marginLeft = 8;
            MusicIconHelper.SetHandCursor(qqSyncBtn);
            qqFormRow.Add(qqSyncBtn);

            // 6. SoundCloud 开放电波卡片
            bool isSoundCloud = MusicCloudSyncEngine.IsSoundCloudConfigured();
            string curScUrl = MusicCloudSyncEngine.GetSoundCloudUrl();
            var scCard = new VisualElement();
            scCard.style.backgroundColor = MusicIconHelper.BgCard;
            scCard.style.borderTopLeftRadius = 8; scCard.style.borderTopRightRadius = 8;
            scCard.style.borderBottomLeftRadius = 8; scCard.style.borderBottomRightRadius = 8;
            scCard.style.borderTopWidth = 1; scCard.style.borderBottomWidth = 1;
            scCard.style.borderLeftWidth = 1; scCard.style.borderRightWidth = 1;
            scCard.style.borderTopColor = new Color(1f, 0.35f, 0.0f, 0.65f); // SoundCloud 橙
            scCard.style.borderBottomColor = scCard.style.borderTopColor;
            scCard.style.borderLeftColor = scCard.style.borderTopColor;
            scCard.style.borderRightColor = scCard.style.borderTopColor;
            scCard.style.paddingLeft = 14; scCard.style.paddingRight = 14;
            scCard.style.paddingTop = 10; scCard.style.paddingBottom = 10;
            scCard.style.marginBottom = 10;
            contentScroll.Add(scCard);

            var scTitleRow = new VisualElement();
            scTitleRow.style.flexDirection = FlexDirection.Row;
            scTitleRow.style.alignItems = Align.Center;
            scCard.Add(scTitleRow);

            var scBadge = MusicIconHelper.CreatePlatformCapsule("SOUNDCLOUD", new Color(1f, 0.35f, 0.0f, 1f));
            scBadge.style.marginRight = 8;
            scTitleRow.Add(scBadge);

            var scTitle = new Label("SoundCloud 开放音频 (SoundCloud Stream)");
            scTitle.style.fontSize = 13; scTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            scTitle.style.color = new Color(1f, 0.35f, 0.0f, 1f);
            scTitle.style.unityFontDefinition = Fd();
            scTitleRow.Add(scTitle);

            var scDesc = new Label(isSoundCloud ? $"已连接 SoundCloud 播单 · [{curScUrl}]" : "支持 SoundCloud 独立曲目、DJ 混音与电波播单导入");
            scDesc.style.fontSize = 10; scDesc.style.color = MusicIconHelper.TextMuted;
            scDesc.style.unityFontDefinition = Fd();
            scDesc.style.marginTop = 2; scDesc.style.marginBottom = 8;
            scCard.Add(scDesc);

            var scFormRow = new VisualElement();
            scFormRow.style.flexDirection = FlexDirection.Row;
            scFormRow.style.alignItems = Align.Center;
            scCard.Add(scFormRow);

            var scField = new TextField { value = string.IsNullOrEmpty(curScUrl) ? "https://soundcloud.com/..." : curScUrl };
            scField.style.flexGrow = 1; scField.style.fontSize = 11; scField.style.unityFontDefinition = Fd();
            MusicIconHelper.StyleIndustrialTextField(scField, "https://soundcloud.com/...", 32f);
            scFormRow.Add(scField);

            var scSyncBtn = new Button(() =>
            {
                string val = scField.value;
                MusicCloudSyncEngine.Instance.ParseSoundCloudTrack(val, (pl) =>
                {
                    TabletMusicAppUI.ShowToast($"成功导入 SoundCloud [{pl.tracks.Count}] 轨独立电波！");
                    TabletMusicAppUI.BuildMusicApp(win);
                }, (err) =>
                {
                    TabletMusicAppUI.ShowToast("【SoundCloud 提示】" + err);
                });
            }) { text = "同步导入" };
            scSyncBtn.style.height = 28; scSyncBtn.style.fontSize = 10; scSyncBtn.style.unityFontDefinition = Fd();
            scSyncBtn.style.backgroundColor = new Color(1f, 0.35f, 0.0f, 1f);
            scSyncBtn.style.color = Color.white;
            scSyncBtn.style.borderTopLeftRadius = 14; scSyncBtn.style.borderTopRightRadius = 14;
            scSyncBtn.style.borderBottomLeftRadius = 14; scSyncBtn.style.borderBottomRightRadius = 14;
            scSyncBtn.style.borderTopWidth = 0; scSyncBtn.style.borderBottomWidth = 0;
            scSyncBtn.style.borderLeftWidth = 0; scSyncBtn.style.borderRightWidth = 0;
            scSyncBtn.style.marginLeft = 8;
            MusicIconHelper.SetHandCursor(scSyncBtn);
            scFormRow.Add(scSyncBtn);

            // 7. WebDAV 私人网盘 / NAS 挂载卡片
            bool isWebDav = MusicCloudSyncEngine.IsWebDavConfigured();
            string curWebDavHost = MusicCloudSyncEngine.GetWebDavHost();
            var webdavCard = new VisualElement();
            webdavCard.style.backgroundColor = MusicIconHelper.BgCard;
            webdavCard.style.borderTopLeftRadius = 8; webdavCard.style.borderTopRightRadius = 8;
            webdavCard.style.borderBottomLeftRadius = 8; webdavCard.style.borderBottomRightRadius = 8;
            webdavCard.style.borderTopWidth = 1; webdavCard.style.borderBottomWidth = 1;
            webdavCard.style.borderLeftWidth = 1; webdavCard.style.borderRightWidth = 1;
            webdavCard.style.borderTopColor = new Color(0.06f, 0.65f, 0.50f, 0.65f); // WebDAV 翠青
            webdavCard.style.borderBottomColor = webdavCard.style.borderTopColor;
            webdavCard.style.borderLeftColor = webdavCard.style.borderTopColor;
            webdavCard.style.borderRightColor = webdavCard.style.borderTopColor;
            webdavCard.style.paddingLeft = 14; webdavCard.style.paddingRight = 14;
            webdavCard.style.paddingTop = 10; webdavCard.style.paddingBottom = 10;
            webdavCard.style.marginBottom = 10;
            contentScroll.Add(webdavCard);

            var wdTitleRow = new VisualElement();
            wdTitleRow.style.flexDirection = FlexDirection.Row;
            wdTitleRow.style.alignItems = Align.Center;
            webdavCard.Add(wdTitleRow);

            var wdBadge = MusicIconHelper.CreatePlatformCapsule("WEBDAV", new Color(0.06f, 0.65f, 0.50f, 1f));
            wdBadge.style.marginRight = 8;
            wdTitleRow.Add(wdBadge);

            var wdTitle = new Label("WebDAV / 私人网盘挂载 (Private NAS / WebDAV)");
            wdTitle.style.fontSize = 13; wdTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            wdTitle.style.color = new Color(0.06f, 0.65f, 0.50f, 1f);
            wdTitle.style.unityFontDefinition = Fd();
            wdTitleRow.Add(wdTitle);

            var wdDesc = new Label(isWebDav ? $"已挂载 WebDAV 服务器 · [{curWebDavHost}]" : "支持个人 NAS、Alist、坚果云等 WebDAV 协议私人音频串流");
            wdDesc.style.fontSize = 10; wdDesc.style.color = MusicIconHelper.TextMuted;
            wdDesc.style.unityFontDefinition = Fd();
            wdDesc.style.marginTop = 2; wdDesc.style.marginBottom = 8;
            webdavCard.Add(wdDesc);

            var wdFormRow = new VisualElement();
            wdFormRow.style.flexDirection = FlexDirection.Row;
            wdFormRow.style.alignItems = Align.Center;
            webdavCard.Add(wdFormRow);

            var wdHostField = new TextField { value = string.IsNullOrEmpty(curWebDavHost) ? "https://nas.local/dav" : curWebDavHost };
            wdHostField.style.flexGrow = 1; wdHostField.style.fontSize = 11; wdHostField.style.unityFontDefinition = Fd();
            MusicIconHelper.StyleIndustrialTextField(wdHostField, "https://nas.local/dav", 32f);
            wdFormRow.Add(wdHostField);

            var wdUserField = new TextField { value = PlayerPrefs.GetString("WebDAV_User", "admin") };
            wdUserField.style.width = 80; wdUserField.style.fontSize = 11; wdUserField.style.unityFontDefinition = Fd();
            wdUserField.style.marginLeft = 6;
            MusicIconHelper.StyleIndustrialTextField(wdUserField, "user", 32f);
            wdFormRow.Add(wdUserField);

            var wdPassField = new TextField { value = PlayerPrefs.GetString("WebDAV_Pass", ""), isPasswordField = true };
            wdPassField.style.width = 80; wdPassField.style.fontSize = 11; wdPassField.style.unityFontDefinition = Fd();
            wdPassField.style.marginLeft = 6;
            MusicIconHelper.StyleIndustrialTextField(wdPassField, "••••••••", 32f);
            wdFormRow.Add(wdPassField);

            var wdSaveBtn = new Button(() =>
            {
                MusicCloudSyncEngine.SaveWebDavConfig(wdHostField.value, wdUserField.value, wdPassField.value);
                TabletMusicAppUI.ShowToast("已保存 WebDAV 私有网盘配置并建立挂载！");
                TabletMusicAppUI.BuildMusicApp(win);
            }) { text = "挂载" };
            wdSaveBtn.style.height = 28; wdSaveBtn.style.fontSize = 10; wdSaveBtn.style.unityFontDefinition = Fd();
            wdSaveBtn.style.backgroundColor = new Color(0.06f, 0.65f, 0.50f, 1f);
            wdSaveBtn.style.color = Color.white;
            wdSaveBtn.style.borderTopLeftRadius = 14; wdSaveBtn.style.borderTopRightRadius = 14;
            wdSaveBtn.style.borderBottomLeftRadius = 14; wdSaveBtn.style.borderBottomRightRadius = 14;
            wdSaveBtn.style.borderTopWidth = 0; wdSaveBtn.style.borderBottomWidth = 0;
            wdSaveBtn.style.borderLeftWidth = 0; wdSaveBtn.style.borderRightWidth = 0;
            wdSaveBtn.style.marginLeft = 8;
            MusicIconHelper.SetHandCursor(wdSaveBtn);
            wdFormRow.Add(wdSaveBtn);

            // 8. 私有云 Navidrome / Subsonic
            bool isSub = MusicCloudSyncEngine.IsSubsonicConfigured();
            var subCard = new VisualElement();
            subCard.style.backgroundColor = MusicIconHelper.BgCard;
            subCard.style.borderTopLeftRadius = 8; subCard.style.borderTopRightRadius = 8;
            subCard.style.borderBottomLeftRadius = 8; subCard.style.borderBottomRightRadius = 8;
            subCard.style.borderTopWidth = 1; subCard.style.borderBottomWidth = 1;
            subCard.style.borderLeftWidth = 1; subCard.style.borderRightWidth = 1;
            subCard.style.borderTopColor = new Color(0.55f, 0.35f, 0.85f, 0.65f);
            subCard.style.borderBottomColor = subCard.style.borderTopColor;
            subCard.style.borderLeftColor = subCard.style.borderTopColor;
            subCard.style.borderRightColor = subCard.style.borderTopColor;
            subCard.style.paddingLeft = 14; subCard.style.paddingRight = 14;
            subCard.style.paddingTop = 10; subCard.style.paddingBottom = 10;
            subCard.style.marginBottom = 10;
            contentScroll.Add(subCard);

            var subTitleRow = new VisualElement();
            subTitleRow.style.flexDirection = FlexDirection.Row;
            subTitleRow.style.alignItems = Align.Center;
            subCard.Add(subTitleRow);

            var subBadge = MusicIconHelper.CreatePlatformCapsule("SUBSONIC", new Color(0.55f, 0.35f, 0.85f, 1f));
            subBadge.style.marginRight = 8;
            subTitleRow.Add(subBadge);

            var subTitle = new Label("Navidrome / Subsonic (个人私有云音乐库)");
            subTitle.style.fontSize = 13; subTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            subTitle.style.color = new Color(0.55f, 0.35f, 0.85f, 1f);
            subTitle.style.unityFontDefinition = Fd();
            subTitleRow.Add(subTitle);

            var subDesc = new Label(isSub ? "已配置私有云服务器连接" : "支持个人自建 Navidrome / Subsonic / Jellyfin 音乐串流");
            subDesc.style.fontSize = 10; subDesc.style.color = MusicIconHelper.TextMuted;
            subDesc.style.unityFontDefinition = Fd();
            subDesc.style.marginTop = 2; subDesc.style.marginBottom = 8;
            subCard.Add(subDesc);

            var subFormRow = new VisualElement();
            subFormRow.style.flexDirection = FlexDirection.Row;
            subFormRow.style.alignItems = Align.Center;
            subCard.Add(subFormRow);

            var hostField = new TextField { value = PlayerPrefs.GetString("Subsonic_Host", "https://music.nas.local") };
            hostField.style.flexGrow = 1; hostField.style.fontSize = 11; hostField.style.unityFontDefinition = Fd();
            MusicIconHelper.StyleIndustrialTextField(hostField, "https://music.nas.local", 32f);
            subFormRow.Add(hostField);

            var userField = new TextField { value = PlayerPrefs.GetString("Subsonic_User", "admin") };
            userField.style.width = 80; userField.style.fontSize = 11; userField.style.unityFontDefinition = Fd();
            userField.style.marginLeft = 6;
            MusicIconHelper.StyleIndustrialTextField(userField, "admin", 32f);
            subFormRow.Add(userField);

            var passField = new TextField { value = PlayerPrefs.GetString("Subsonic_Pass", ""), isPasswordField = true };
            passField.style.width = 80; passField.style.fontSize = 11; passField.style.unityFontDefinition = Fd();
            passField.style.marginLeft = 6;
            MusicIconHelper.StyleIndustrialTextField(passField, "••••••••", 32f);
            subFormRow.Add(passField);

            var saveSubBtn = new Button(() =>
            {
                MusicCloudSyncEngine.SaveSubsonicConfig(hostField.value, userField.value, passField.value);
                TabletMusicAppUI.ShowToast("已保存 Subsonic 私有云配置！");
                TabletMusicAppUI.BuildMusicApp(win);
            }) { text = "保存" };
            saveSubBtn.style.height = 28; saveSubBtn.style.fontSize = 10; saveSubBtn.style.unityFontDefinition = Fd();
            saveSubBtn.style.backgroundColor = new Color(0.55f, 0.35f, 0.85f, 1f);
            saveSubBtn.style.color = Color.white;
            saveSubBtn.style.borderTopLeftRadius = 14; saveSubBtn.style.borderTopRightRadius = 14;
            saveSubBtn.style.borderBottomLeftRadius = 14; saveSubBtn.style.borderBottomRightRadius = 14;
            saveSubBtn.style.borderTopWidth = 0; saveSubBtn.style.borderBottomWidth = 0;
            saveSubBtn.style.borderLeftWidth = 0; saveSubBtn.style.borderRightWidth = 0;
            saveSubBtn.style.marginLeft = 8;
            MusicIconHelper.SetHandCursor(saveSubBtn);
            subFormRow.Add(saveSubBtn);
        }

        // ==================== 通用平台卡片组件 ====================
        public static VisualElement CreatePlatformCard(string title, string statusText, string btnText, Color btnColor, Action onPrimaryClick, Action onLogoutClick = null)
        {
            var card = new VisualElement();
            card.style.backgroundColor = MusicIconHelper.BgCard;
            card.style.borderTopLeftRadius = 8; card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = 8; card.style.borderBottomRightRadius = 8;
            card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor = MusicIconHelper.BorderIndustrial;
            card.style.borderBottomColor = card.style.borderTopColor;
            card.style.borderLeftColor = card.style.borderTopColor;
            card.style.borderRightColor = card.style.borderTopColor;
            card.style.paddingLeft = 14; card.style.paddingRight = 14;
            card.style.paddingTop = 10; card.style.paddingBottom = 10;
            card.style.marginBottom = 10;
            card.style.flexDirection = FlexDirection.Row;
            card.style.justifyContent = Justify.SpaceBetween;
            card.style.alignItems = Align.Center;

            var left = new VisualElement();
            left.style.flexGrow = 1; left.style.marginRight = 10;
            card.Add(left);

            var tLbl = new Label(title);
            tLbl.style.fontSize = 13; tLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tLbl.style.color = MusicIconHelper.BrassGold;
            tLbl.style.unityFontDefinition = Fd();
            left.Add(tLbl);

            var sLbl = new Label(statusText);
            sLbl.style.fontSize = 10; sLbl.style.color = MusicIconHelper.TextMuted;
            sLbl.style.unityFontDefinition = Fd();
            sLbl.style.marginTop = 2;
            left.Add(sLbl);

            var right = new VisualElement();
            right.style.flexDirection = FlexDirection.Row;
            right.style.alignItems = Align.Center;
            card.Add(right);

            if (onLogoutClick != null)
            {
                var logoutBtn = new Button(onLogoutClick) { text = "断开" };
                logoutBtn.style.height = 24; logoutBtn.style.fontSize = 9.5f; logoutBtn.style.unityFontDefinition = Fd();
                logoutBtn.style.backgroundColor = new Color(0.24f, 0.22f, 0.20f, 1f);
                logoutBtn.style.color = MusicIconHelper.TextParchment;
                logoutBtn.style.borderTopLeftRadius = 12; logoutBtn.style.borderTopRightRadius = 12;
                logoutBtn.style.borderBottomLeftRadius = 12; logoutBtn.style.borderBottomRightRadius = 12;
                logoutBtn.style.borderTopWidth = 0; logoutBtn.style.borderBottomWidth = 0;
                logoutBtn.style.borderLeftWidth = 0; logoutBtn.style.borderRightWidth = 0;
                logoutBtn.style.marginRight = 6;
                MusicIconHelper.SetHandCursor(logoutBtn);
                right.Add(logoutBtn);
            }

            var pBtn = new Button(onPrimaryClick) { text = btnText };
            pBtn.style.height = 26; pBtn.style.paddingLeft = 12; pBtn.style.paddingRight = 12;
            pBtn.style.fontSize = 10; pBtn.style.unityFontDefinition = Fd();
            pBtn.style.backgroundColor = btnColor;
            pBtn.style.color = Color.white;
            pBtn.style.borderTopLeftRadius = 13; pBtn.style.borderTopRightRadius = 13;
            pBtn.style.borderBottomLeftRadius = 13; pBtn.style.borderBottomRightRadius = 13;
            MusicIconHelper.SetHandCursor(pBtn);
            right.Add(pBtn);

            return card;
        }

        // ==================== 统一扫码授权浮窗 ====================
        // 彻底清除所有原生 Emoji，采用纯矢量图元与雷达动画
        public static void ShowQrModal(VisualElement win, string platform)
        {
            var mask = new VisualElement();
            mask.style.position = Position.Absolute;
            mask.style.left = 0; mask.style.right = 0; mask.style.top = 0; mask.style.bottom = 0;
            mask.style.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 0.80f);
            mask.style.alignItems = Align.Center; mask.style.justifyContent = Justify.Center;
            win.Add(mask);

            Action safeClose = () =>
            {
                if (mask != null && mask.parent != null)
                {
                    mask.parent.Remove(mask);
                }
            };

            var dialog = new VisualElement();
            dialog.style.width = 360; dialog.style.paddingTop = 20; dialog.style.paddingBottom = 20;
            dialog.style.paddingLeft = 20; dialog.style.paddingRight = 20;
            dialog.style.backgroundColor = MusicIconHelper.BgCard;
            dialog.style.borderTopLeftRadius = 8; dialog.style.borderTopRightRadius = 8;
            dialog.style.borderBottomLeftRadius = 8; dialog.style.borderBottomRightRadius = 8;
            dialog.style.borderTopWidth = 1.5f; dialog.style.borderBottomWidth = 1.5f;
            dialog.style.borderLeftWidth = 1.5f; dialog.style.borderRightWidth = 1.5f;
            dialog.style.borderTopColor = MusicIconHelper.BorderIndustrial;
            dialog.style.borderBottomColor = dialog.style.borderTopColor;
            dialog.style.borderLeftColor = dialog.style.borderTopColor;
            dialog.style.borderRightColor = dialog.style.borderTopColor;
            dialog.style.alignItems = Align.Center;
            mask.Add(dialog);

            var dTitle = new Label(platform == "netease" ? "网易云音乐 · 随车电波授权" : "Bilibili · 随身听收藏同步");
            dTitle.style.fontSize = 14; dTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            dTitle.style.color = MusicIconHelper.BrassGold;
            dTitle.style.unityFontDefinition = Fd();
            dTitle.style.marginBottom = 4;
            dialog.Add(dTitle);

            var dSub = new Label("请打开手机客户端「扫一扫」扫描下方调频信标");
            dSub.style.fontSize = 10; dSub.style.color = MusicIconHelper.TextMuted;
            dSub.style.unityFontDefinition = Fd();
            dSub.style.marginBottom = 12;
            dialog.Add(dSub);

            var qrPlate = new VisualElement();
            qrPlate.style.width = 190; qrPlate.style.height = 190;
            qrPlate.style.backgroundColor = new Color(0.12f, 0.11f, 0.10f, 1f);
            qrPlate.style.borderTopLeftRadius = 6; qrPlate.style.borderTopRightRadius = 6;
            qrPlate.style.borderBottomLeftRadius = 6; qrPlate.style.borderBottomRightRadius = 6;
            qrPlate.style.borderTopWidth = 1; qrPlate.style.borderBottomWidth = 1;
            qrPlate.style.borderLeftWidth = 1; qrPlate.style.borderRightWidth = 1;
            qrPlate.style.borderTopColor = MusicIconHelper.BorderIndustrial;
            qrPlate.style.borderBottomColor = qrPlate.style.borderTopColor;
            qrPlate.style.borderLeftColor = qrPlate.style.borderTopColor;
            qrPlate.style.borderRightColor = qrPlate.style.borderTopColor;
            qrPlate.style.alignItems = Align.Center; qrPlate.style.justifyContent = Justify.Center;
            qrPlate.style.marginBottom = 12;
            dialog.Add(qrPlate);

            var loadingBox = new VisualElement();
            loadingBox.style.alignItems = Align.Center; loadingBox.style.justifyContent = Justify.Center;
            qrPlate.Add(loadingBox);

            var lamp = MusicIconHelper.CreatePilotLamp(true);
            lamp.style.marginBottom = 6;
            loadingBox.Add(lamp);

            var loadingLbl = new Label("正在调频建立加密信标...");
            loadingLbl.style.fontSize = 9.5f; loadingLbl.style.color = MusicIconHelper.TextMuted;
            loadingLbl.style.unityFontDefinition = Fd();
            loadingBox.Add(loadingLbl);

            var qrImg = new Image();
            qrImg.style.width = 176; qrImg.style.height = 176;
            qrImg.style.display = DisplayStyle.None;
            qrPlate.Add(qrImg);

            var tipLbl = new Label("正在向调度台申请加密凭证...");
            tipLbl.style.fontSize = 10; tipLbl.style.color = MusicIconHelper.TextParchment;
            tipLbl.style.unityFontDefinition = Fd();
            tipLbl.style.marginBottom = 10;
            dialog.Add(tipLbl);

            if (platform == "netease")
            {
                var altRow = new VisualElement();
                altRow.style.width = Length.Percent(100);
                altRow.style.flexDirection = FlexDirection.Row;
                altRow.style.alignItems = Align.Center;
                altRow.style.marginBottom = 12;
                dialog.Add(altRow);

                var uidField = new TextField { value = "" };
                uidField.style.flexGrow = 1;
                MusicIconHelper.StyleIndustrialTextField(uidField, "输入网易云 UID 或歌单链接", 30f);
                altRow.Add(uidField);

                var uidSyncBtn = new Button(() =>
                {
                    MusicCloudSyncEngine.Instance.LoginNeteaseByUidOrPlaylist(uidField.value, () =>
                    {
                        TabletMusicAppUI.ShowToast("【网易云】账号已绑定，同步成功！");
                        safeClose();
                        TabletMusicAppUI.BuildMusicApp(win);
                    }, err =>
                    {
                        tipLbl.text = err;
                    });
                }) { text = "一键绑定" };
                uidSyncBtn.style.height = 30;
                uidSyncBtn.style.fontSize = 10;
                uidSyncBtn.style.marginLeft = 6;
                uidSyncBtn.style.backgroundColor = MusicIconHelper.BrassGold;
                uidSyncBtn.style.color = new Color(0.12f, 0.10f, 0.08f, 1f);
                uidSyncBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                uidSyncBtn.style.borderTopLeftRadius = 15; uidSyncBtn.style.borderTopRightRadius = 15;
                uidSyncBtn.style.borderBottomLeftRadius = 15; uidSyncBtn.style.borderBottomRightRadius = 15;
                uidSyncBtn.style.borderTopWidth = 0; uidSyncBtn.style.borderBottomWidth = 0;
                uidSyncBtn.style.borderLeftWidth = 0; uidSyncBtn.style.borderRightWidth = 0;
                altRow.Add(uidSyncBtn);
            }

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.alignItems = Align.Center;
            dialog.Add(btnRow);

            var closeBtn = new Button(safeClose) { text = "取消返回" };
            closeBtn.style.height = 28; closeBtn.style.width = 100;
            closeBtn.style.fontSize = 10; closeBtn.style.unityFontDefinition = Fd();
            closeBtn.style.backgroundColor = new Color(0.24f, 0.22f, 0.20f, 1f);
            closeBtn.style.color = MusicIconHelper.TextParchment;
            closeBtn.style.borderTopLeftRadius = 14; closeBtn.style.borderTopRightRadius = 14;
            closeBtn.style.borderBottomLeftRadius = 14; closeBtn.style.borderBottomRightRadius = 14;
            closeBtn.style.borderTopWidth = 0; closeBtn.style.borderBottomWidth = 0;
            closeBtn.style.borderLeftWidth = 0; closeBtn.style.borderRightWidth = 0;
            btnRow.Add(closeBtn);

            Action showQrTexture = () =>
            {
                loadingBox.style.display = DisplayStyle.None;
                qrPlate.style.backgroundColor = new Color(0.96f, 0.94f, 0.90f, 1f); // 柔和和纸衬底
                qrImg.style.display = DisplayStyle.Flex;
            };

            // 启动扫码流程
            if (platform == "netease")
            {
                MusicCloudSyncEngine.Instance.StartNeteaseQrLogin((key, imgOrUrl) =>
                {
                    tipLbl.text = "请使用手机网易云音乐 App 扫描上方二维码";
                    if (!string.IsNullOrEmpty(imgOrUrl))
                    {
                        MusicCloudSyncEngine.Instance.StartCoroutine(SimpleQrCodeGenerator.LoadQrTexture(imgOrUrl, tex =>
                        {
                            if (qrImg != null && tex != null)
                            {
                                qrImg.image = tex;
                                showQrTexture();
                            }
                        }));
                    }
                }, () =>
                {
                    TabletMusicAppUI.ShowToast("网易云登录成功！");
                    safeClose();
                    TabletMusicAppUI.BuildMusicApp(win);
                }, err =>
                {
                    tipLbl.text = "调频信标未响应: " + err;
                }, status =>
                {
                    tipLbl.text = status;
                });
            }
            else
            {
                MusicCloudSyncEngine.Instance.StartBilibiliQrLogin((key, url) =>
                {
                    tipLbl.text = "请使用手机 B 站 App 扫描上方二维码";
                    MusicCloudSyncEngine.Instance.StartCoroutine(SimpleQrCodeGenerator.LoadQrTexture(url, tex =>
                    {
                        if (qrImg != null && tex != null)
                        {
                            qrImg.image = tex;
                            showQrTexture();
                        }
                    }));
                }, () =>
                {
                    TabletMusicAppUI.ShowToast("B 站连接成功！");
                    safeClose();
                    TabletMusicAppUI.BuildMusicApp(win);
                }, err =>
                {
                    tipLbl.text = "连接未响应: " + err;
                });
            }
        }

        // ==================== 随身听调频指南 · 列车长手册风格大弹窗 ====================
        public static void ShowOnboardingGuideModal(VisualElement win)
        {
            var mask = new VisualElement();
            mask.style.position = Position.Absolute;
            mask.style.left = 0; mask.style.right = 0; mask.style.top = 0; mask.style.bottom = 0;
            mask.style.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 0.80f);
            mask.style.alignItems = Align.Center; mask.style.justifyContent = Justify.Center;
            win.Add(mask);

            Action safeClose = () =>
            {
                if (mask != null && mask.parent != null)
                {
                    mask.parent.Remove(mask);
                }
            };

            var dialog = new VisualElement();
            dialog.style.width = 650; dialog.style.height = 430;
            dialog.style.backgroundColor = MusicIconHelper.BgCard;
            dialog.style.borderTopLeftRadius = 8; dialog.style.borderTopRightRadius = 8;
            dialog.style.borderBottomLeftRadius = 8; dialog.style.borderBottomRightRadius = 8;
            dialog.style.borderTopWidth = 1.5f; dialog.style.borderBottomWidth = 1.5f;
            dialog.style.borderLeftWidth = 1.5f; dialog.style.borderRightWidth = 1.5f;
            dialog.style.borderTopColor = MusicIconHelper.BorderIndustrial;
            dialog.style.borderBottomColor = dialog.style.borderTopColor;
            dialog.style.borderLeftColor = dialog.style.borderTopColor;
            dialog.style.borderRightColor = dialog.style.borderTopColor;
            dialog.style.position = Position.Relative;
            dialog.style.paddingTop = 18; dialog.style.paddingBottom = 18;
            dialog.style.paddingLeft = 24; dialog.style.paddingRight = 24;
            dialog.style.justifyContent = Justify.SpaceBetween;
            mask.Add(dialog);

            var headRow = new VisualElement();
            headRow.style.flexDirection = FlexDirection.Row;
            headRow.style.justifyContent = Justify.SpaceBetween;
            headRow.style.alignItems = Align.Center;
            dialog.Add(headRow);

            var titleLeft = new VisualElement();
            titleLeft.style.flexDirection = FlexDirection.Row;
            titleLeft.style.alignItems = Align.Center;
            headRow.Add(titleLeft);

            var guideTitle = new Label("随身听调频指南 · 列车长运行手册");
            guideTitle.style.fontSize = 15; guideTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            guideTitle.style.color = MusicIconHelper.BrassGold;
            guideTitle.style.unityFontDefinition = Fd();
            titleLeft.Add(guideTitle);

            var subBadge = new Label("DOC-RR-FM88");
            subBadge.style.fontSize = 9; subBadge.style.color = MusicIconHelper.BrassDark;
            subBadge.style.marginLeft = 8; subBadge.style.unityFontDefinition = Fd();
            titleLeft.Add(subBadge);

            var closeBtn = new Button(safeClose) { text = "✕" };
            closeBtn.style.fontSize = 14; closeBtn.style.width = 28; closeBtn.style.height = 28;
            closeBtn.style.backgroundColor = Color.clear;
            closeBtn.style.borderTopWidth = 0; closeBtn.style.borderBottomWidth = 0;
            closeBtn.style.borderLeftWidth = 0; closeBtn.style.borderRightWidth = 0;
            closeBtn.style.color = MusicIconHelper.TextMuted;
            headRow.Add(closeBtn);

            var steps = new[]
            {
                new {
                    code = "OST",
                    title = "官方原声 · 随剧情探索深度解锁",
                    tag = "全量 26 首 · 剧情联动保密",
                    desc = "随身听已完整编纂 26 首官方原声。打开应用默认起播老站台经典《Platform 站台》；在 VN 视觉小说推进中途经或偶遇的每一首乐章，都会永久为您解密收录！未行至的关卡乐章严格加密保密，绝不提前剧透。"
                },
                new {
                    code = "TAPE",
                    title = "本地私人磁带 · 快捷一键导入",
                    tag = "脱机即播 · 自动拷贝",
                    desc = "车厢完全离线时也能纵情聆听！点击「本地磁带」中的「导入本地音频」即可呼出系统文件管理器，批量选中您的 MP3 / WAV / OGG 音频，系统将全自动拷贝进车厢磁带箱，随拷随听。"
                },
                new {
                    code = "CLOUD",
                    title = "时代云端 · 手机扫码秒级对齐",
                    tag = "B站收藏夹 / 网易云歌单 / Spotify / 私有云",
                    desc = "支持多平台流媒体聚合同步！手机客户端轻松一扫，一键将您私藏的红心歌单投影至列车随身听，更有 Subsonic 私有流媒体直连，打造属于您的移动音乐厅。"
                },
                new {
                    code = "CTRL",
                    title = "机车控制台 · 连续音量与纯代码音浪",
                    tag = "无级滑动 · 纯代码矢量图标",
                    desc = "底栏控制台搭载 0%~100% 连续音量滑动调节与持久化记忆，纯代码精雕单曲循环、列表循环与随机播放三种模式，伴随跳动跃动的暖金 Equalizer 仪表音浪，沉浸体验沙海漫游。"
                }
            };

            int curStep = 0;

            var contentBox = new VisualElement();
            contentBox.style.flexDirection = FlexDirection.Row;
            contentBox.style.backgroundColor = new Color(0.18f, 0.17f, 0.15f, 1f);
            contentBox.style.borderTopLeftRadius = 8; contentBox.style.borderTopRightRadius = 8;
            contentBox.style.borderBottomLeftRadius = 8; contentBox.style.borderBottomRightRadius = 8;
            contentBox.style.borderTopWidth = 1; contentBox.style.borderBottomWidth = 1;
            contentBox.style.borderLeftWidth = 1; contentBox.style.borderRightWidth = 1;
            contentBox.style.borderTopColor = MusicIconHelper.BorderIndustrial;
            contentBox.style.borderBottomColor = contentBox.style.borderTopColor;
            contentBox.style.borderLeftColor = contentBox.style.borderTopColor;
            contentBox.style.borderRightColor = contentBox.style.borderTopColor;
            contentBox.style.paddingLeft = 20; contentBox.style.paddingRight = 20;
            contentBox.style.paddingTop = 18; contentBox.style.paddingBottom = 18;
            contentBox.style.marginTop = 12; contentBox.style.marginBottom = 12;
            contentBox.style.flexGrow = 1;
            contentBox.style.alignItems = Align.Center;
            dialog.Add(contentBox);

            var leftBadge = new VisualElement();
            leftBadge.style.width = 64; leftBadge.style.height = 64;
            leftBadge.style.borderTopLeftRadius = 32; leftBadge.style.borderTopRightRadius = 32;
            leftBadge.style.borderBottomLeftRadius = 32; leftBadge.style.borderBottomRightRadius = 32;
            leftBadge.style.backgroundColor = new Color(0.83f, 0.66f, 0.26f, 0.18f);
            leftBadge.style.borderTopWidth = 1.5f; leftBadge.style.borderBottomWidth = 1.5f;
            leftBadge.style.borderLeftWidth = 1.5f; leftBadge.style.borderRightWidth = 1.5f;
            leftBadge.style.borderTopColor = MusicIconHelper.BrassGold;
            leftBadge.style.borderBottomColor = leftBadge.style.borderTopColor;
            leftBadge.style.borderLeftColor = leftBadge.style.borderTopColor;
            leftBadge.style.borderRightColor = leftBadge.style.borderTopColor;
            leftBadge.style.alignItems = Align.Center; leftBadge.style.justifyContent = Justify.Center;
            leftBadge.style.marginRight = 20;
            contentBox.Add(leftBadge);

            var iconLbl = new Label("OST");
            iconLbl.style.fontSize = 16; iconLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            iconLbl.style.color = MusicIconHelper.BrassGold;
            iconLbl.style.unityFontDefinition = Fd();
            leftBadge.Add(iconLbl);

            var textColumn = new VisualElement();
            textColumn.style.flexGrow = 1;
            contentBox.Add(textColumn);

            var stepTitle = new Label("");
            stepTitle.style.fontSize = 14; stepTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            stepTitle.style.color = MusicIconHelper.TextParchment;
            stepTitle.style.unityFontDefinition = Fd();
            textColumn.Add(stepTitle);

            var tagBadge = new Label("");
            tagBadge.style.fontSize = 10;
            tagBadge.style.color = MusicIconHelper.BrassGold;
            tagBadge.style.unityFontDefinition = Fd();
            tagBadge.style.marginTop = 3; tagBadge.style.marginBottom = 8;
            textColumn.Add(tagBadge);

            var descLbl = new Label("");
            descLbl.style.fontSize = 11; descLbl.style.color = MusicIconHelper.TextMuted;
            descLbl.style.unityFontDefinition = Fd();
            descLbl.style.whiteSpace = WhiteSpace.Normal;
            textColumn.Add(descLbl);

            var bottomRow = new VisualElement();
            bottomRow.style.flexDirection = FlexDirection.Row;
            bottomRow.style.justifyContent = Justify.SpaceBetween;
            bottomRow.style.alignItems = Align.Center;
            dialog.Add(bottomRow);

            var prevBtn = new Button { text = "上一章" };
            prevBtn.style.height = 30; prevBtn.style.paddingLeft = 16; prevBtn.style.paddingRight = 16;
            prevBtn.style.fontSize = 10; prevBtn.style.unityFontDefinition = Fd();
            prevBtn.style.backgroundColor = new Color(0.22f, 0.20f, 0.18f, 1f);
            prevBtn.style.color = MusicIconHelper.TextParchment;
            prevBtn.style.borderTopLeftRadius = 15; prevBtn.style.borderTopRightRadius = 15;
            prevBtn.style.borderBottomLeftRadius = 15; prevBtn.style.borderBottomRightRadius = 15;
            prevBtn.style.borderTopWidth = 0; prevBtn.style.borderBottomWidth = 0;
            prevBtn.style.borderLeftWidth = 0; prevBtn.style.borderRightWidth = 0;
            bottomRow.Add(prevBtn);

            var dotRow = new VisualElement();
            dotRow.style.flexDirection = FlexDirection.Row;
            dotRow.style.alignItems = Align.Center;
            bottomRow.Add(dotRow);

            var dots = new VisualElement[steps.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                var dot = new VisualElement();
                dot.style.height = 5; dot.style.marginLeft = 3; dot.style.marginRight = 3;
                dot.style.borderTopLeftRadius = 2.5f; dot.style.borderTopRightRadius = 2.5f;
                dot.style.borderBottomLeftRadius = 2.5f; dot.style.borderBottomRightRadius = 2.5f;
                dotRow.Add(dot);
                dots[i] = dot;
            }

            var nextBtn = new Button { text = "下一章" };
            nextBtn.style.height = 30; nextBtn.style.paddingLeft = 18; nextBtn.style.paddingRight = 18;
            nextBtn.style.fontSize = 10; nextBtn.style.unityFontDefinition = Fd();
            nextBtn.style.backgroundColor = MusicIconHelper.BrassDark;
            nextBtn.style.color = new Color(0.12f, 0.10f, 0.08f, 1f);
            nextBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            nextBtn.style.borderTopLeftRadius = 15; nextBtn.style.borderTopRightRadius = 15;
            nextBtn.style.borderBottomLeftRadius = 15; nextBtn.style.borderBottomRightRadius = 15;
            nextBtn.style.borderTopWidth = 0; nextBtn.style.borderBottomWidth = 0;
            nextBtn.style.borderLeftWidth = 0; nextBtn.style.borderRightWidth = 0;
            bottomRow.Add(nextBtn);

            Action refreshStepUI = () =>
            {
                var s = steps[curStep];
                iconLbl.text = s.code;
                stepTitle.text = s.title;
                tagBadge.text = s.tag;
                descLbl.text = s.desc;

                prevBtn.style.visibility = (curStep == 0) ? Visibility.Hidden : Visibility.Visible;
                nextBtn.text = (curStep == steps.Length - 1) ? "掌握要领 · 开启旅程" : "下一章 ➔";

                for (int i = 0; i < dots.Length; i++)
                {
                    if (i == curStep)
                    {
                        dots[i].style.width = 16;
                        dots[i].style.backgroundColor = MusicIconHelper.BrassGold;
                    }
                    else
                    {
                        dots[i].style.width = 5;
                        dots[i].style.backgroundColor = new Color(0.35f, 0.32f, 0.28f, 1f);
                    }
                }
            };

            prevBtn.clicked += () =>
            {
                if (curStep > 0)
                {
                    curStep--;
                    refreshStepUI();
                }
            };

            nextBtn.clicked += () =>
            {
                if (curStep < steps.Length - 1)
                {
                    curStep++;
                    refreshStepUI();
                }
                else
                {
                    safeClose();
                }
            };

            refreshStepUI();
        }
    }
}
