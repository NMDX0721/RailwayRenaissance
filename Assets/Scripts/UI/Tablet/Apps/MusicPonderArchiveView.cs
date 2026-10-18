using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听 Ponder 深度叙事时间轴与世界观秘档卡片 (MusicPonderArchiveView)
    /// 借鉴 Folia ponder (PonderFocusFrame, PonderChapterTimeline)
    /// 让每一首音轨都承载荒原铁道的历史记忆、工务机车手记与章节关键帧。
    /// </summary>
    public static class MusicPonderArchiveView
    {
        public class ChapterCue
        {
            public float timeSec;
            public string title;
            public string caption;
        }

        private static readonly Dictionary<string, List<ChapterCue>> s_cuesDatabase = new Dictionary<string, List<ChapterCue>>
        {
            {
                "platform", new List<ChapterCue>
                {
                    new ChapterCue { timeSec = 0f, title = "00:00 晨雾启程", caption = "站台调度铃声回荡，蒸汽锅炉开始缓慢升压。" },
                    new ChapterCue { timeSec = 35f, title = "00:35 进路开通", caption = "闭塞信号机由红转绿，列车徐徐驶离斑驳的水泥月台。" },
                    new ChapterCue { timeSec = 75f, title = "01:15 荒原长线", caption = "轮轨撞击道岔的节奏加快，两侧绿植渐渐被漫天黄沙取代。" }
                }
            },
            {
                "iron_and_ash", new List<ChapterCue>
                {
                    new ChapterCue { timeSec = 0f, title = "00:00 熔渣炉膛", caption = "先民重工业的咆哮，高炉火光映红了半边夜空。" },
                    new ChapterCue { timeSec = 40f, title = "00:40 钢轨淬火", caption = "重锤锻打声与机油味弥漫在整座旧时代工务机械所。" },
                    new ChapterCue { timeSec = 80f, title = "01:20 钢铁脉动", caption = "老调度员留下的手记字迹：只要齿轮还在咬合，文明就不会熄灭。" }
                }
            },
            {
                "ambient_nature", new List<ChapterCue>
                {
                    new ChapterCue { timeSec = 0f, title = "00:00 戈壁风起", caption = "干燥的狂风掠过无边红柳与风蚀雅丹地貌。" },
                    new ChapterCue { timeSec = 45f, title = "00:45 远雷沉闷", caption = "沙尘暴在前方的地平线上隆起一道紫金色的幕墙。" },
                    new ChapterCue { timeSec = 90f, title = "01:30 旷野呼吸", caption = "风声渐歇，万籁俱寂，只听见温度计玻璃管内水银的微鸣。" }
                }
            }
        };

        private static readonly Color GoldColor = new Color(0.96f, 0.82f, 0.45f, 1f);
        private static readonly Color DarkBorder = new Color(0.35f, 0.30f, 0.22f, 0.8f);

        /// <summary>
        /// 构建深度 Ponder 聚焦视窗（覆盖在中央视口上，带呼吸光泽与工务印章）
        /// </summary>
        public static VisualElement BuildPonderModal(
            string trackId,
            string trackTitle,
            string trackDesc,
            float currentPlaySec,
            Action<float> onSeekToTime,
            Action onClose)
        {
            var modalRoot = new VisualElement();
            modalRoot.style.position = Position.Absolute;
            modalRoot.style.left = 0; modalRoot.style.right = 0;
            modalRoot.style.top = 0; modalRoot.style.bottom = 0;
            modalRoot.style.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 0.94f);
            modalRoot.style.backgroundImage = new StyleBackground(MusicIconHelper.GetFrostedGlassTexture());
            modalRoot.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            modalRoot.style.alignItems = Align.Center;
            modalRoot.style.justifyContent = Justify.Center;
            modalRoot.style.paddingLeft = 20; modalRoot.style.paddingRight = 20;
            modalRoot.style.paddingTop = 16; modalRoot.style.paddingBottom = 16;

            var frame = new VisualElement();
            frame.style.width = Length.Percent(92);
            frame.style.maxHeight = Length.Percent(90);
            frame.style.backgroundColor = new Color(0.14f, 0.13f, 0.12f, 0.98f);
            frame.style.borderTopLeftRadius = 10; frame.style.borderTopRightRadius = 10;
            frame.style.borderBottomLeftRadius = 10; frame.style.borderBottomRightRadius = 10;
            frame.style.borderTopWidth = 1.5f; frame.style.borderBottomWidth = 1.5f;
            frame.style.borderLeftWidth = 1.5f; frame.style.borderRightWidth = 1.5f;
            frame.style.borderTopColor = GoldColor;
            frame.style.borderBottomColor = frame.style.borderTopColor;
            frame.style.borderLeftColor = frame.style.borderTopColor;
            frame.style.borderRightColor = frame.style.borderTopColor;
            frame.style.paddingLeft = 20; frame.style.paddingRight = 20;
            frame.style.paddingTop = 16; frame.style.paddingBottom = 16;
            frame.style.flexDirection = FlexDirection.Column;
            MusicIconHelper.ApplyCardShadow(frame, 8f, 20f, 0.8f);
            modalRoot.Add(frame);

            // 顶栏：先民工务段档案编号与关闭按键
            var topBar = new VisualElement();
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.justifyContent = Justify.SpaceBetween;
            topBar.style.alignItems = Align.Center;
            topBar.style.borderBottomWidth = 1f;
            topBar.style.borderBottomColor = DarkBorder;
            topBar.style.paddingBottom = 10;
            topBar.style.marginBottom = 12;
            frame.Add(topBar);

            var docTag = new Label($"❖ 工务调度绝密留声档 · ARCHIVE-NO.{Math.Abs(trackId.GetHashCode()) % 8999 + 1000}");
            docTag.style.fontSize = 11;
            docTag.style.unityFontStyleAndWeight = FontStyle.Bold;
            docTag.style.color = GoldColor;
            docTag.style.unityFontDefinition = Fd();
            topBar.Add(docTag);

            var closeBtn = new Button(onClose) { text = "✕ 关闭档案" };
            closeBtn.style.height = 24; closeBtn.style.paddingLeft = 10; closeBtn.style.paddingRight = 10;
            closeBtn.style.fontSize = 10; closeBtn.style.unityFontDefinition = Fd();
            closeBtn.style.backgroundColor = new Color(0.22f, 0.20f, 0.18f, 1f);
            closeBtn.style.color = new Color(0.92f, 0.88f, 0.82f, 1f);
            closeBtn.style.borderTopLeftRadius = 12; closeBtn.style.borderTopRightRadius = 12;
            closeBtn.style.borderBottomLeftRadius = 12; closeBtn.style.borderBottomRightRadius = 12;
            closeBtn.style.borderTopWidth = 1; closeBtn.style.borderBottomWidth = 1;
            closeBtn.style.borderLeftWidth = 1; closeBtn.style.borderRightWidth = 1;
            closeBtn.style.borderTopColor = DarkBorder;
            closeBtn.style.borderBottomColor = closeBtn.style.borderTopColor;
            closeBtn.style.borderLeftColor = closeBtn.style.borderTopColor;
            closeBtn.style.borderRightColor = closeBtn.style.borderTopColor;
            MusicIconHelper.ApplyPhysicalMicroInteraction(closeBtn, onClick: onClose, enableFloat: false, isBrass: false);
            topBar.Add(closeBtn);

            // 中部内容区：左大封面 + 右叙事与章节时间轴
            var bodyRow = new VisualElement();
            bodyRow.style.flexDirection = FlexDirection.Row;
            bodyRow.style.flexGrow = 1;
            frame.Add(bodyRow);

            var leftCoverBox = new VisualElement();
            leftCoverBox.style.width = 120;
            leftCoverBox.style.marginRight = 18;
            bodyRow.Add(leftCoverBox);

            var bigArt = new VisualElement();
            bigArt.style.width = 120; bigArt.style.height = 120;
            bigArt.style.borderTopLeftRadius = 6; bigArt.style.borderTopRightRadius = 6;
            bigArt.style.borderBottomLeftRadius = 6; bigArt.style.borderBottomRightRadius = 6;
            bigArt.style.borderTopWidth = 1.5f; bigArt.style.borderBottomWidth = 1.5f;
            bigArt.style.borderLeftWidth = 1.5f; bigArt.style.borderRightWidth = 1.5f;
            bigArt.style.borderTopColor = GoldColor;
            bigArt.style.borderBottomColor = bigArt.style.borderTopColor;
            bigArt.style.borderLeftColor = bigArt.style.borderTopColor;
            bigArt.style.borderRightColor = bigArt.style.borderTopColor;
            bigArt.style.backgroundImage = new StyleBackground(MusicAlbumArtResolver.GetAlbumCover(trackId, ""));
            bigArt.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            bigArt.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            MusicIconHelper.ApplyCardShadow(bigArt, 4f, 10f, 0.6f);
            leftCoverBox.Add(bigArt);

            var stampLbl = new Label("【先民工务局 · 绝密验证】");
            stampLbl.style.fontSize = 8;
            stampLbl.style.color = new Color(0.85f, 0.40f, 0.30f, 1f);
            stampLbl.style.unityFontDefinition = Fd();
            stampLbl.style.marginTop = 8;
            stampLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            leftCoverBox.Add(stampLbl);

            var rightInfoCol = new VisualElement();
            rightInfoCol.style.flexGrow = 1;
            bodyRow.Add(rightInfoCol);

            var titleLbl = new Label(trackTitle);
            titleLbl.style.fontSize = 16;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.color = new Color(0.98f, 0.95f, 0.90f, 1f);
            titleLbl.style.unityFontDefinition = Fd();
            rightInfoCol.Add(titleLbl);

            string lore = string.IsNullOrEmpty(trackDesc) ? "沙海孤轨漫漫，此音轨承载着旧时代开拓者的无尽执念。" : trackDesc;
            var descLbl = new Label(lore + "。随身听磁带记录下每一节车厢与风沙的抗争，钢轨深处至今仍回荡着那个时代的律动。");
            descLbl.style.fontSize = 10;
            descLbl.style.color = new Color(0.78f, 0.75f, 0.70f, 1f);
            descLbl.style.unityFontDefinition = Fd();
            descLbl.style.whiteSpace = WhiteSpace.Normal;
            descLbl.style.marginTop = 4;
            descLbl.style.marginBottom = 12;
            rightInfoCol.Add(descLbl);

            // 章节时间轴节点 (Chapter Timeline)
            var timelineHead = new Label("⏱ 叙事章节关键帧 (点击可直接跳转进度)");
            timelineHead.style.fontSize = 10;
            timelineHead.style.unityFontStyleAndWeight = FontStyle.Bold;
            timelineHead.style.color = GoldColor;
            timelineHead.style.unityFontDefinition = Fd();
            timelineHead.style.marginBottom = 6;
            rightInfoCol.Add(timelineHead);

            List<ChapterCue> cues;
            if (!s_cuesDatabase.TryGetValue(trackId, out cues))
            {
                cues = new List<ChapterCue>
                {
                    new ChapterCue { timeSec = 0f, title = "00:00 电波接入", caption = "调频信号锁定，背景磁带底噪轻微起伏。" },
                    new ChapterCue { timeSec = 40f, title = "00:40 旋律高潮", caption = "黄沙掠过玻璃车窗，先民的旋律响彻机车操纵台。" },
                    new ChapterCue { timeSec = 90f, title = "01:30 余波渐隐", caption = "列车驶入长隧道，电波归于低沉的电报盲音。" }
                };
            }

            foreach (var cue in cues)
            {
                var cueItem = new VisualElement();
                cueItem.style.flexDirection = FlexDirection.Row;
                cueItem.style.alignItems = Align.Center;
                cueItem.style.backgroundColor = new Color(0.18f, 0.17f, 0.15f, 0.9f);
                cueItem.style.borderTopLeftRadius = 4; cueItem.style.borderTopRightRadius = 4;
                cueItem.style.borderBottomLeftRadius = 4; cueItem.style.borderBottomRightRadius = 4;
                cueItem.style.paddingLeft = 8; cueItem.style.paddingRight = 8;
                cueItem.style.paddingTop = 4; cueItem.style.paddingBottom = 4;
                cueItem.style.marginBottom = 4;
                cueItem.pickingMode = PickingMode.Position;
                rightInfoCol.Add(cueItem);

                var cueTime = new Label(cue.title);
                cueTime.style.fontSize = 9;
                cueTime.style.unityFontStyleAndWeight = FontStyle.Bold;
                cueTime.style.color = GoldColor;
                cueTime.style.unityFontDefinition = Fd();
                cueTime.style.width = 110;
                cueItem.Add(cueTime);

                var cueCap = new Label(cue.caption);
                cueCap.style.fontSize = 9;
                cueCap.style.color = new Color(0.85f, 0.82f, 0.76f, 1f);
                cueCap.style.unityFontDefinition = Fd();
                cueCap.style.flexGrow = 1;
                cueItem.Add(cueCap);

                MusicIconHelper.ApplyPhysicalMicroInteraction(
                    cueItem,
                    onClick: () =>
                    {
                        onSeekToTime?.Invoke(cue.timeSec);
                        onClose?.Invoke();
                    },
                    enableFloat: true,
                    normalBg: new Color(0.18f, 0.17f, 0.15f, 0.9f),
                    hoverBg: new Color(0.25f, 0.22f, 0.18f, 1f),
                    activeBg: new Color(0.12f, 0.11f, 0.10f, 1f),
                    isBrass: true);
            }

            return modalRoot;
        }
    }
}
