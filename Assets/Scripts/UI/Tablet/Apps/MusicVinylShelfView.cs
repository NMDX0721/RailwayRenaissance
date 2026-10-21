using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;
using TabletMusicEntry = RailwayRenaissance.UI.Tablet.Apps.TabletMusicAppUI.TabletMusicEntry;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听原声大碟拟物黑胶唱片墙与展架模式 (MusicVinylShelfView)
    /// 借鉴 Folia wall (Vinyl Wall / Shelf View)
    /// 将 26 首铁道原声化作工务调度室展架上的实体唱片盒，直观展示未解锁封条与已解锁金标。
    /// </summary>
    public static class MusicVinylShelfView
    {
        private static readonly Color GoldColor = new Color(0.96f, 0.82f, 0.45f, 1f);
        private static readonly Color DarkBorder = new Color(0.35f, 0.30f, 0.22f, 0.8f);

        public static VisualElement BuildShelfGrid(
            TabletMusicAppUI.TabletMusicEntry[] tracks,
            string currentTrackId,
            bool isPlaying,
            Func<TabletMusicAppUI.TabletMusicEntry, bool> isUnlockedCheck,
            Action<TabletMusicAppUI.TabletMusicEntry> onPlayTrack,
            Action<string> onShowToast)
        {
            var gridContainer = new VisualElement();
            gridContainer.style.flexDirection = FlexDirection.Row;
            gridContainer.style.flexWrap = Wrap.Wrap;
            gridContainer.style.justifyContent = Justify.SpaceBetween;
            gridContainer.style.paddingTop = 8;
            gridContainer.style.paddingBottom = 16;

            foreach (var track in tracks)
            {
                bool unlocked = isUnlockedCheck(track);
                bool isCur = (track.id == currentTrackId && isPlaying);

                var shelfCard = new VisualElement();
                shelfCard.style.width = Length.Percent(23.5f);
                shelfCard.style.marginBottom = 14;
                shelfCard.style.borderTopLeftRadius = 10; shelfCard.style.borderTopRightRadius = 10;
                shelfCard.style.borderBottomLeftRadius = 10; shelfCard.style.borderBottomRightRadius = 10;
                shelfCard.style.paddingLeft = 8; shelfCard.style.paddingRight = 8;
                shelfCard.style.paddingTop = 8; shelfCard.style.paddingBottom = 8;

                MusicIconHelper.ApplyLiquidGlassCard(
                    shelfCard,
                    isPlaying: isCur,
                    isLocked: !unlocked,
                    onClick: () =>
                    {
                        if (unlocked) onPlayTrack(track);
                        else onShowToast("【调频加密】" + track.condition + " 方可解码！");
                    });

                gridContainer.Add(shelfCard);

                // 实体封套视口 (88px)
                var coverWrap = new VisualElement();
                coverWrap.style.height = 88;
                coverWrap.style.borderTopLeftRadius = 6; coverWrap.style.borderTopRightRadius = 6;
                coverWrap.style.borderBottomLeftRadius = 6; coverWrap.style.borderBottomRightRadius = 6;
                coverWrap.style.borderTopWidth = 1f; coverWrap.style.borderBottomWidth = 1f;
                coverWrap.style.borderLeftWidth = 1f; coverWrap.style.borderRightWidth = 1f;
                coverWrap.style.borderTopColor = isCur ? GoldColor : new Color(1f, 1f, 1f, 0.16f);
                coverWrap.style.borderBottomColor = new Color(0f, 0f, 0f, 0.5f);
                coverWrap.style.borderLeftColor = coverWrap.style.borderTopColor;
                coverWrap.style.borderRightColor = coverWrap.style.borderBottomColor;
                coverWrap.style.overflow = Overflow.Hidden;
                coverWrap.style.position = Position.Relative;
                coverWrap.style.marginBottom = 6;

                Texture2D coverTex = MusicAlbumArtResolver.GetAlbumCover(track.id, track.category);
                coverWrap.style.backgroundImage = new StyleBackground(coverTex);
                coverWrap.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                coverWrap.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                shelfCard.Add(coverWrap);

                // 未解锁：右上角精密机械挂锁徽章 + 半透微雾
                if (!unlocked)
                {
                    var lockBadge = MusicIconHelper.CreateIndustrialLockBadge(mini: true);
                    lockBadge.style.position = Position.Absolute;
                    lockBadge.style.right = 6;
                    lockBadge.style.top = 6;
                    coverWrap.Add(lockBadge);

                    var mist = new VisualElement();
                    mist.style.position = Position.Absolute;
                    mist.style.left = 0; mist.style.right = 0; mist.style.top = 0; mist.style.bottom = 0;
                    mist.style.backgroundColor = new Color(0.06f, 0.08f, 0.10f, 0.40f);
                    mist.pickingMode = PickingMode.Ignore;
                    coverWrap.Add(mist);
                }
                else
                {
                    // 已解锁：右下角常驻微型播放圆纽
                    var playBtn = MusicIconHelper.CreateLiquidPlayPill(isCur, () =>
                    {
                        onPlayTrack(track);
                    });
                    playBtn.style.position = Position.Absolute;
                    playBtn.style.right = 5; playBtn.style.bottom = 5;
                    coverWrap.Add(playBtn);
                }

                // 曲目标题与分类
                var titleLbl = new Label(track.title);
                titleLbl.style.fontSize = 11;
                titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLbl.style.color = isCur ? GoldColor : (unlocked ? new Color(0.96f, 0.94f, 0.90f, 1f) : new Color(0.60f, 0.58f, 0.52f, 1f));
                titleLbl.style.unityFontDefinition = Fd();
                titleLbl.style.whiteSpace = WhiteSpace.NoWrap;
                titleLbl.style.overflow = Overflow.Hidden;
                titleLbl.style.textOverflow = TextOverflow.Ellipsis;
                shelfCard.Add(titleLbl);

                // 底部图形化调频频段胶囊
                var subBadge = MusicIconHelper.CreateFrequencyBadge(
                    unlocked ? (track.category + " · 先民原声") : "加密信标 · 待解码",
                    unlocked ? (isCur ? GoldColor : new Color(0.55f, 0.72f, 0.82f, 1f)) : new Color(0.78f, 0.62f, 0.28f, 1f),
                    Fd());
                subBadge.style.marginTop = 3;
                shelfCard.Add(subBadge);
            }

            return gridContainer;
        }
    }
}
