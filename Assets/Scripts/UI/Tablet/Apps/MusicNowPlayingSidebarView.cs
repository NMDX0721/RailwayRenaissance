using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听正在播放独立右侧视窗 (MusicNowPlayingSidebarView)
    /// 彻底废除黄色粗暴大圆盘与拥挤小方块，引入 Folia 歌词流 (Ponder Notes)、
    /// 16-bit 像素实体专辑封面、工业先民日志与精巧队列卡片。
    /// </summary>
    public static class MusicNowPlayingSidebarView
    {
        public class SidebarHandle
        {
            public VisualElement root;
            public VisualElement coverElement;
            public Label titleLabel;
            public Label categoryLabel;
            public VisualElement lyricsContainer;
            public Label nextTitleLabel;
            public VisualElement nextCard;
            public List<Label> lyricLineLabels = new List<Label>();
            public string[] activeLogs;
        }

        private static readonly Color BgDark = new Color(0.12f, 0.11f, 0.10f, 0.96f);
        private static readonly Color BrassGold = new Color(0.96f, 0.82f, 0.45f, 1f);
        private static readonly Color BrassDark = new Color(0.83f, 0.66f, 0.26f, 1f);
        private static readonly Color BorderLine = new Color(0.35f, 0.28f, 0.20f, 0.75f);
        private static readonly Color TextMuted = new Color(0.68f, 0.65f, 0.60f, 1f);

        /// <summary>
        /// 基于真实世界观档案与曲目工务出处动态构建备忘录（彻底淘汰打油诗）
        /// </summary>
        public static string[] GetTrackArchiveLogs(string trackId, string category)
        {
            var entry = Array.Find(TabletMusicAppUI.TabletMusicList, m => m.id == trackId);
            string cond = entry != null && !string.IsNullOrEmpty(entry.condition) ? entry.condition : "铁道全线自动解锁";
            string desc = entry != null && !string.IsNullOrEmpty(entry.desc) ? entry.desc : "残阳如血，锈蚀钢轨伸向旧时代沙海深处";
            string title = entry != null ? entry.title : "未命名曲目";

            if (category == "歌曲" || trackId == "south_wind" || trackId == "starlit_rails" || trackId == "chollima_ride" || trackId == "sleepers")
            {
                return new string[]
                {
                    $"【声乐典藏】{title} · 随车电波声轨",
                    $"【工务来历】{cond}",
                    $"【现场诗篇】{desc}",
                    "【先民留声】沙能机车调频广播 · 全带宽立体声传输"
                };
            }

            return new string[]
            {
                $"【编曲规制】纯器乐实况原声 (Instrumental OST) · 16-Bit / 44.1kHz",
                $"【工务出处】{cond}",
                $"【现场备忘】{desc}",
                "【机车遥测】沙能气压稳定 · 闭塞信号正常 · 随车电波在线"
            };
        }

        public static SidebarHandle BuildSidebar(
            VisualElement container,
            string currentTrackId,
            string currentTitle,
            string currentCategory,
            string nextTrackTitle,
            bool isPlaying,
            bool showLyrics,
            Action<float> onLyricSeek,
            Action onPlayNext,
            Action onOpenPonderArchive = null)
        {
            var h = new SidebarHandle();
            container.Clear();
            container.style.width = 236;
            container.style.flexShrink = 0;
            container.style.backgroundColor = BgDark;
            container.style.backgroundImage = new StyleBackground(MusicIconHelper.GetFrostedGlassTexture());
            container.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            container.style.backgroundSize = new BackgroundSize(Length.Pixels(128), Length.Pixels(128));
            container.style.borderLeftWidth = 1.2f;
            container.style.borderLeftColor = BorderLine;
            container.style.paddingLeft = 14;
            container.style.paddingRight = 14;
            container.style.paddingTop = 14;
            container.style.paddingBottom = 12;
            container.style.flexDirection = FlexDirection.Column;
            container.style.justifyContent = Justify.SpaceBetween;
            h.root = container;

            var upperBox = new VisualElement();
            upperBox.style.flexDirection = FlexDirection.Column;
            upperBox.style.flexGrow = 1;
            container.Add(upperBox);

            // 1. 顶栏：标题 + 呼吸电平指示灯
            var headRow = new VisualElement();
            headRow.style.flexDirection = FlexDirection.Row;
            headRow.style.justifyContent = Justify.SpaceBetween;
            headRow.style.alignItems = Align.Center;
            headRow.style.marginBottom = 10;
            upperBox.Add(headRow);

            var titleLbl = new Label("正在回响 (NOW PLAYING)");
            titleLbl.style.fontSize = 11;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.color = BrassGold;
            titleLbl.style.unityFontDefinition = Fd();
            headRow.Add(titleLbl);

            var pilot = MusicIconHelper.CreatePilotLamp(isPlaying);
            headRow.Add(pilot);

            // 2. 中央高质感 16-bit 像素实体唱片封套 (140x140 矩形封套 + 底部 16 段复古琥珀 LED 频谱条)
            var coverBox = new VisualElement();
            coverBox.style.alignItems = Align.Center;
            coverBox.style.justifyContent = Justify.Center;
            coverBox.style.marginBottom = 10;
            upperBox.Add(coverBox);

            var albumCover = new VisualElement();
            albumCover.style.width = 140;
            albumCover.style.height = 140;
            albumCover.style.borderTopLeftRadius = 6; albumCover.style.borderTopRightRadius = 6;
            albumCover.style.borderBottomLeftRadius = 6; albumCover.style.borderBottomRightRadius = 6;
            albumCover.style.borderTopWidth = 1.5f; albumCover.style.borderBottomWidth = 1.5f;
            albumCover.style.borderLeftWidth = 1.5f; albumCover.style.borderRightWidth = 1.5f;
            albumCover.style.borderTopColor = BrassDark;
            albumCover.style.borderBottomColor = albumCover.style.borderTopColor;
            albumCover.style.borderLeftColor = albumCover.style.borderTopColor;
            albumCover.style.borderRightColor = albumCover.style.borderTopColor;
            albumCover.style.overflow = Overflow.Hidden;
            albumCover.style.position = Position.Relative;
            Texture2D coverTex = MusicAlbumArtResolver.GetAlbumCover(currentTrackId, currentCategory);
            albumCover.style.backgroundImage = new StyleBackground(coverTex);
            albumCover.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            albumCover.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            MusicIconHelper.ApplyCardShadow(albumCover, 4f, 12f, 0.65f);
            coverBox.Add(albumCover);
            h.coverElement = albumCover;

            // 封面底部内嵌 16 段复古琥珀 LED 实时音频电平频谱条
            var ledBarRow = new VisualElement();
            ledBarRow.style.position = Position.Absolute;
            ledBarRow.style.bottom = 0; ledBarRow.style.left = 0; ledBarRow.style.right = 0;
            ledBarRow.style.height = 20;
            ledBarRow.style.backgroundColor = new Color(0.08f, 0.08f, 0.09f, 0.88f);
            ledBarRow.style.flexDirection = FlexDirection.Row;
            ledBarRow.style.alignItems = Align.FlexEnd;
            ledBarRow.style.justifyContent = Justify.SpaceAround;
            ledBarRow.style.paddingLeft = 4; ledBarRow.style.paddingRight = 4;
            ledBarRow.style.paddingBottom = 2;
            albumCover.Add(ledBarRow);

            Color ledColor = new Color(0.96f, 0.65f, 0.18f, 0.95f); // 琥珀金光
            for (int k = 0; k < 16; k++)
            {
                var bar = new VisualElement();
                bar.style.width = 4.5f;
                bar.style.height = isPlaying ? UnityEngine.Random.Range(4f, 16f) : 3f;
                bar.style.borderTopLeftRadius = 1; bar.style.borderTopRightRadius = 1;
                bar.style.backgroundColor = ledColor;
                ledBarRow.Add(bar);
            }

            // 3. 当前曲目大标题与分类
            h.titleLabel = new Label(string.IsNullOrEmpty(currentTitle) ? "未选择播放曲目" : currentTitle);
            h.titleLabel.style.fontSize = 13;
            h.titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            h.titleLabel.style.color = new Color(0.98f, 0.95f, 0.88f, 1f);
            h.titleLabel.style.unityFontDefinition = Fd();
            h.titleLabel.style.whiteSpace = WhiteSpace.NoWrap;
            h.titleLabel.style.overflow = Overflow.Hidden;
            h.titleLabel.style.textOverflow = TextOverflow.Ellipsis;
            upperBox.Add(h.titleLabel);

            h.categoryLabel = new Label(string.IsNullOrEmpty(currentCategory) ? "随车电波已就绪" : (currentCategory + " · 先民工业原声"));
            h.categoryLabel.style.fontSize = 9;
            h.categoryLabel.style.color = TextMuted;
            h.categoryLabel.style.unityFontDefinition = Fd();
            h.categoryLabel.style.marginTop = 2;
            h.categoryLabel.style.marginBottom = 8;
            upperBox.Add(h.categoryLabel);

            // 4. Folia 核心特性：逐行流光歌词 / 剧情日志流 (自动撑满中间纵向剩余空间)
            var lyricsBox = new VisualElement();
            lyricsBox.style.flexGrow = 1;
            lyricsBox.style.minHeight = 160;
            lyricsBox.style.backgroundColor = new Color(0.11f, 0.12f, 0.15f, 0.75f);
            lyricsBox.style.borderTopLeftRadius = 7; lyricsBox.style.borderTopRightRadius = 7;
            lyricsBox.style.borderBottomLeftRadius = 7; lyricsBox.style.borderBottomRightRadius = 7;
            lyricsBox.style.borderTopWidth = 1f; lyricsBox.style.borderBottomWidth = 1.2f;
            lyricsBox.style.borderLeftWidth = 1f; lyricsBox.style.borderRightWidth = 1.2f;
            lyricsBox.style.borderTopColor = new Color(1f, 1f, 1f, 0.16f);
            lyricsBox.style.borderLeftColor = lyricsBox.style.borderTopColor;
            lyricsBox.style.borderBottomColor = new Color(0.04f, 0.05f, 0.07f, 0.75f);
            lyricsBox.style.borderRightColor = lyricsBox.style.borderBottomColor;
            lyricsBox.style.paddingLeft = 10; lyricsBox.style.paddingRight = 10;
            lyricsBox.style.paddingTop = 8; lyricsBox.style.paddingBottom = 8;
            lyricsBox.style.marginBottom = 10;
            lyricsBox.style.overflow = Overflow.Hidden;
            MusicIconHelper.ApplyCardShadow(lyricsBox, 2f, 6f, 0.35f);
            upperBox.Add(lyricsBox);
            h.lyricsContainer = lyricsBox;

            h.activeLogs = GetTrackArchiveLogs(currentTrackId, currentCategory);

            var dHeadRow = new VisualElement();
            dHeadRow.style.flexDirection = FlexDirection.Row;
            dHeadRow.style.justifyContent = Justify.SpaceBetween;
            dHeadRow.style.alignItems = Align.Center;
            dHeadRow.style.marginBottom = 6;
            lyricsBox.Add(dHeadRow);

            var dHead = new Label("◆ 铁道工务备忘 · 曲目档案");
            dHead.style.fontSize = 9;
            dHead.style.unityFontStyleAndWeight = FontStyle.Bold;
            dHead.style.color = BrassGold;
            dHead.style.unityFontDefinition = Fd();
            dHead.pickingMode = PickingMode.Position;
            if (onOpenPonderArchive != null)
            {
                dHead.RegisterCallback<ClickEvent>(evt => onOpenPonderArchive());
            }
            dHeadRow.Add(dHead);

            // 展开全屏沉浸歌词按钮 (⛶)
            var expandBtn = new Label("⛶");
            expandBtn.style.fontSize = 11;
            expandBtn.style.color = BrassGold;
            expandBtn.style.unityFontDefinition = Fd();
            expandBtn.pickingMode = PickingMode.Position;
            if (onOpenPonderArchive != null)
            {
                expandBtn.RegisterCallback<ClickEvent>(evt => onOpenPonderArchive());
            }
            dHeadRow.Add(expandBtn);

            // 备忘录滚动容器（居中舒适排版，彻底解决文字粘连拥挤）
            var lyricScrollBox = new VisualElement();
            lyricScrollBox.style.flexGrow = 1;
            lyricScrollBox.style.justifyContent = Justify.Center;
            lyricsBox.Add(lyricScrollBox);

            for (int i = 0; i < 3; i++)
            {
                int lineIdx = i;
                string text = (h.activeLogs != null && i < h.activeLogs.Length) ? h.activeLogs[i] : "";
                var lineLbl = new Label(text);
                lineLbl.style.fontSize = (i == 1) ? 11f : 9.5f;
                lineLbl.style.color = (i == 1) ? new Color(0.96f, 0.78f, 0.28f, 1f) : new Color(0.68f, 0.65f, 0.60f, 0.40f);
                lineLbl.style.unityFontStyleAndWeight = (i == 1) ? FontStyle.Bold : FontStyle.Normal;
                lineLbl.style.unityFontDefinition = Fd();
                lineLbl.style.whiteSpace = WhiteSpace.Normal;
                lineLbl.style.marginTop = (i == 1) ? 6 : 2;
                lineLbl.style.marginBottom = (i == 1) ? 6 : 2;
                if (i == 1)
                {
                    lineLbl.style.textShadow = new StyleTextShadow(new TextShadow { offset = new Vector2(0, 1), blurRadius = 6f, color = new Color(0.96f, 0.78f, 0.28f, 0.55f) });
                }
                lineLbl.RegisterCallback<ClickEvent>(evt =>
                {
                    float mockProgress = 0.25f * (lineIdx + 1);
                    onLyricSeek?.Invoke(mockProgress);
                });
                lyricScrollBox.Add(lineLbl);
                h.lyricLineLabels.Add(lineLbl);
            }

            // 5. 底部：队列下一首预览卡片 (Next Track Card)
            var nextBox = new VisualElement();
            nextBox.style.backgroundColor = new Color(0.18f, 0.16f, 0.14f, 0.95f);
            nextBox.style.borderTopLeftRadius = 6; nextBox.style.borderTopRightRadius = 6;
            nextBox.style.borderBottomLeftRadius = 6; nextBox.style.borderBottomRightRadius = 6;
            nextBox.style.borderTopWidth = 1f; nextBox.style.borderBottomWidth = 1f;
            nextBox.style.borderLeftWidth = 1f; nextBox.style.borderRightWidth = 1f;
            nextBox.style.borderTopColor = new Color(0.35f, 0.30f, 0.22f, 0.8f);
            nextBox.style.borderBottomColor = nextBox.style.borderTopColor;
            nextBox.style.borderLeftColor = nextBox.style.borderTopColor;
            nextBox.style.borderRightColor = nextBox.style.borderTopColor;
            nextBox.style.paddingLeft = 8; nextBox.style.paddingRight = 8;
            nextBox.style.paddingTop = 6; nextBox.style.paddingBottom = 6;
            nextBox.pickingMode = PickingMode.Position;
            MusicIconHelper.ApplyCardShadow(nextBox, 2f, 4f, 0.35f);
            container.Add(nextBox);
            h.nextCard = nextBox;

            var nextTag = new Label("⏭ 队列下一首 (点击切歌)");
            nextTag.style.fontSize = 8;
            nextTag.style.color = TextMuted;
            nextTag.style.unityFontDefinition = Fd();
            nextBox.Add(nextTag);

            h.nextTitleLabel = new Label(string.IsNullOrEmpty(nextTrackTitle) ? "播放队列已尽" : nextTrackTitle);
            h.nextTitleLabel.style.fontSize = 10;
            h.nextTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            h.nextTitleLabel.style.color = BrassGold;
            h.nextTitleLabel.style.unityFontDefinition = Fd();
            h.nextTitleLabel.style.whiteSpace = WhiteSpace.NoWrap;
            h.nextTitleLabel.style.overflow = Overflow.Hidden;
            h.nextTitleLabel.style.textOverflow = TextOverflow.Ellipsis;
            nextBox.Add(h.nextTitleLabel);

            MusicIconHelper.ApplyPhysicalMicroInteraction(
                nextBox,
                onClick: onPlayNext,
                enableFloat: true,
                normalBg: new Color(0.18f, 0.16f, 0.14f, 0.95f),
                hoverBg: new Color(0.24f, 0.22f, 0.18f, 1f),
                activeBg: new Color(0.14f, 0.12f, 0.10f, 1f),
                isBrass: true);

            return h;
        }

        /// <summary>
        /// 随播放进度动态滚动工务备忘录与档案条目
        /// </summary>
        public static void UpdateLyricsProgress(SidebarHandle h, float normalizedProgress)
        {
            if (h == null || h.lyricLineLabels == null || h.lyricLineLabels.Count < 3) return;
            string[] logs = h.activeLogs ?? GetTrackArchiveLogs("", "");
            if (logs == null || logs.Length == 0) return;

            int activeIdx = Mathf.Clamp((int)(normalizedProgress * logs.Length), 0, logs.Length - 1);
            int prevIdx = Mathf.Max(0, activeIdx - 1);
            int nextIdx = Mathf.Min(logs.Length - 1, activeIdx + 1);

            h.lyricLineLabels[0].text = logs[prevIdx];
            h.lyricLineLabels[0].style.color = new Color(0.65f, 0.62f, 0.58f, 0.45f);

            h.lyricLineLabels[1].text = logs[activeIdx];
            h.lyricLineLabels[1].style.color = BrassGold;

            h.lyricLineLabels[2].text = logs[nextIdx];
            h.lyricLineLabels[2].style.color = new Color(0.65f, 0.62f, 0.58f, 0.45f);
        }
    }
}
