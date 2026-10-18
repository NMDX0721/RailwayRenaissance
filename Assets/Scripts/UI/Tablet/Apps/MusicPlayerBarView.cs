using System;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听专业三段式底部播放控制台（MusicPlayerBarView）
    /// 包含：左段曲目封套与收藏、中段标准 5 键与内嵌时间细进度条、右段伪 VU 动圈表与音量滑块
    /// 彻底剔除单边月牙截断、滑块脱节与排版错位，对标 Spotify & Folia 标准
    /// </summary>
    public static class MusicPlayerBarView
    {
        public class PlayerBarHandle
        {
            public VisualElement root;
            public VisualElement miniCover;
            public Label miniCoverIcon;
            public Label trackTitle;
            public Label trackCategory;
            public Button favBtn;
            
            // 播控键
            public Button shuffleBtn;
            public Button prevBtn;
            public Button playToggleBtn;
            public Button nextBtn;
            public VisualElement modeBtn;

            // 进度条
            public Label curTimeLbl;
            public Label totalTimeLbl;
            public VisualElement progressTrack;
            public VisualElement progressFill;
            public VisualElement progressKnob;

            // 右侧工具
            public Button sleepTimerBtn;
            public Button radioFilterBtn;
            public Button lyricsToggleBtn;
            public VisualElement vuNeedle;
            public Button volBtn;
            public Slider volSlider;
            public VisualElement volTrack;
            public VisualElement volFill;
            public VisualElement volKnob;

            // 内部驱动状态
            public bool isScrubbing;
            public float scrubNormalized;
        }

        private static readonly Color BarBg = new Color(0.12f, 0.11f, 0.10f, 0.98f);
        private static readonly Color BrassGold = new Color(0.96f, 0.82f, 0.45f, 1f);
        private static readonly Color BrassDark = new Color(0.83f, 0.66f, 0.26f, 1f);
        private static readonly Color BorderLine = new Color(0.35f, 0.28f, 0.20f, 0.80f);
        private static readonly Color TextMuted = new Color(0.68f, 0.65f, 0.60f, 1f);

        public static PlayerBarHandle BuildPlayerBar(
            VisualElement parent,
            Action onPlayToggle,
            Action onPrev,
            Action onNext,
            Action onModeCycle,
            Action onShuffle,
            Action onFavToggle,
            Action<float> onSeek,
            Action<float> onVolChange,
            Action onVolMuteToggle,
            Action onLyricsToggle,
            Action onCycleSleepTimer = null,
            Action onCycleRadioFilter = null)
        {
            var h = new PlayerBarHandle();

            var bar = new VisualElement();
            bar.style.height = 72;
            bar.style.flexShrink = 0;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.justifyContent = Justify.SpaceBetween;
            bar.style.backgroundColor = BarBg;
            bar.style.backgroundImage = new StyleBackground(MusicIconHelper.GetFrostedGlassTexture());
            bar.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            bar.style.backgroundSize = new BackgroundSize(Length.Pixels(128), Length.Pixels(128));
            bar.style.borderTopWidth = 1.2f;
            bar.style.borderTopColor = BorderLine;
            bar.style.paddingLeft = 14;
            bar.style.paddingRight = 16;
            parent.Add(bar);
            h.root = bar;

            // ==================== 1. 左段：封套缩略图 + 曲名 + 艺术家/分类 + 收藏 (240px) ====================
            var leftGroup = new VisualElement();
            leftGroup.style.width = 240;
            leftGroup.style.flexShrink = 0;
            leftGroup.style.flexDirection = FlexDirection.Row;
            leftGroup.style.alignItems = Align.Center;
            bar.Add(leftGroup);

            // 42×42 像素封套缩略图（双层内嵌微光线）
            var coverBox = new VisualElement();
            coverBox.style.width = 42;
            coverBox.style.height = 42;
            coverBox.style.borderTopLeftRadius = 5;
            coverBox.style.borderTopRightRadius = 5;
            coverBox.style.borderBottomLeftRadius = 5;
            coverBox.style.borderBottomRightRadius = 5;
            coverBox.style.backgroundColor = new Color(0.18f, 0.15f, 0.12f, 1f);
            coverBox.style.borderTopWidth = 1;
            coverBox.style.borderBottomWidth = 1;
            coverBox.style.borderLeftWidth = 1;
            coverBox.style.borderRightWidth = 1;
            coverBox.style.borderTopColor = new Color(0.45f, 0.38f, 0.28f, 0.8f);
            coverBox.style.borderBottomColor = coverBox.style.borderTopColor;
            coverBox.style.borderLeftColor = coverBox.style.borderTopColor;
            coverBox.style.borderRightColor = coverBox.style.borderTopColor;
            coverBox.style.alignItems = Align.Center;
            coverBox.style.justifyContent = Justify.Center;
            coverBox.style.marginRight = 10;
            leftGroup.Add(coverBox);
            h.miniCover = coverBox;

            var coverIcon = new Label("♫");
            coverIcon.style.fontSize = 18;
            coverIcon.style.color = BrassGold;
            coverIcon.style.unityFontDefinition = Fd();
            coverBox.Add(coverIcon);
            h.miniCoverIcon = coverIcon;

            // 文字信息（单行不溢出，平滑截断）
            var textCol = new VisualElement();
            textCol.style.flexGrow = 1;
            textCol.style.overflow = Overflow.Hidden;
            textCol.style.marginRight = 8;
            leftGroup.Add(textCol);

            var titleLbl = new Label("未选择播放曲目");
            titleLbl.style.fontSize = 12;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.color = new Color(0.96f, 0.94f, 0.88f, 1f);
            titleLbl.style.unityFontDefinition = Fd();
            titleLbl.style.whiteSpace = WhiteSpace.NoWrap;
            titleLbl.style.overflow = Overflow.Hidden;
            titleLbl.style.textOverflow = TextOverflow.Ellipsis;
            textCol.Add(titleLbl);
            h.trackTitle = titleLbl;

            var catLbl = new Label("随身听已就绪 · 点击曲目开启调频");
            catLbl.style.fontSize = 9.5f;
            catLbl.style.color = TextMuted;
            catLbl.style.unityFontDefinition = Fd();
            catLbl.style.whiteSpace = WhiteSpace.NoWrap;
            catLbl.style.overflow = Overflow.Hidden;
            catLbl.style.textOverflow = TextOverflow.Ellipsis;
            catLbl.style.marginTop = 1;
            textCol.Add(catLbl);
            h.trackCategory = catLbl;

            // 收藏红心
            var favBtn = new Button(() => onFavToggle?.Invoke());
            favBtn.text = "♡";
            favBtn.style.width = 28;
            favBtn.style.height = 28;
            favBtn.style.fontSize = 16;
            favBtn.style.unityFontDefinition = Fd();
            favBtn.style.backgroundColor = Color.clear;
            favBtn.style.color = TextMuted;
            favBtn.style.borderTopWidth = 0;
            favBtn.style.borderBottomWidth = 0;
            favBtn.style.borderLeftWidth = 0;
            favBtn.style.borderRightWidth = 0;
            MusicIconHelper.SetHandCursor(favBtn);
            leftGroup.Add(favBtn);
            h.favBtn = favBtn;

            // ==================== 2. 中段：居中 5 键播控 + 细平滑进度轨 (自适应) ====================
            var centerGroup = new VisualElement();
            centerGroup.style.flexGrow = 1;
            centerGroup.style.minWidth = 320;
            centerGroup.style.flexDirection = FlexDirection.Column;
            centerGroup.style.alignItems = Align.Center;
            centerGroup.style.justifyContent = Justify.Center;
            centerGroup.style.marginLeft = 16;
            centerGroup.style.marginRight = 16;
            bar.Add(centerGroup);

            // 上排：4 键经典播控排（单键模式轮换 + 上一首 + 播放大圆纽 + 下一首，绝对居中）
            var ctrlRow = new VisualElement();
            ctrlRow.style.flexDirection = FlexDirection.Row;
            ctrlRow.style.alignItems = Align.Center;
            ctrlRow.style.marginBottom = 5;
            centerGroup.Add(ctrlRow);

            // 1. 🔁/🔂/🔀 播放模式循环切换键（单键三态：列表循环 -> 单曲循环 -> 随机播放）
            var modeBtn = MusicIconHelper.CreateUnifiedPlayModeControl(0, onModeCycle);
            modeBtn.style.marginRight = 14;
            MusicIconHelper.SetHandCursor(modeBtn);
            ctrlRow.Add(modeBtn);
            h.modeBtn = modeBtn;
            h.shuffleBtn = null; // 模式合并，淘汰独立随机键

            // 2. ⏮ 上一首按键 (纯 VisualElement 边框构建 |◀，绝对锐利不走形)
            var prevBtn = new Button(() => onPrev?.Invoke());
            prevBtn.style.width = 30;
            prevBtn.style.height = 30;
            prevBtn.style.backgroundColor = Color.clear;
            prevBtn.style.borderTopWidth = 0; prevBtn.style.borderBottomWidth = 0;
            prevBtn.style.borderLeftWidth = 0; prevBtn.style.borderRightWidth = 0;
            prevBtn.style.alignItems = Align.Center;
            prevBtn.style.justifyContent = Justify.Center;
            prevBtn.style.marginRight = 14;
            prevBtn.Add(MusicIconHelper.CreateTrackNavIcon(false, 13f, BrassGold));
            MusicIconHelper.SetHandCursor(prevBtn);
            prevBtn.RegisterCallback<MouseEnterEvent>(e => prevBtn.style.scale = new Scale(new Vector3(1.15f, 1.15f, 1f)));
            prevBtn.RegisterCallback<MouseLeaveEvent>(e => prevBtn.style.scale = new Scale(Vector3.one));
            ctrlRow.Add(prevBtn);
            h.prevBtn = prevBtn;

            // 3. ▶ / ⏸ 播放/暂停大圆纽（38×38 纯圆黄铜金纽，黑金对比工业核心）
            var playBtn = new Button();
            playBtn.style.width = 38;
            playBtn.style.height = 38;
            playBtn.style.borderTopLeftRadius = 19;
            playBtn.style.borderTopRightRadius = 19;
            playBtn.style.borderBottomLeftRadius = 19;
            playBtn.style.borderBottomRightRadius = 19;
            playBtn.style.borderTopWidth = 1;
            playBtn.style.borderBottomWidth = 1;
            playBtn.style.borderLeftWidth = 1;
            playBtn.style.borderRightWidth = 1;
            playBtn.style.borderTopColor = new Color(1f, 1f, 1f, 0.45f);
            playBtn.style.borderBottomColor = playBtn.style.borderTopColor;
            playBtn.style.borderLeftColor = playBtn.style.borderTopColor;
            playBtn.style.borderRightColor = playBtn.style.borderTopColor;
            playBtn.style.backgroundColor = BrassDark;
            playBtn.style.alignItems = Align.Center;
            playBtn.style.justifyContent = Justify.Center;
            playBtn.style.marginRight = 14;
            playBtn.Add(MusicIconHelper.CreatePlayIcon(13f, new Color(0.12f, 0.10f, 0.08f, 1f)));
            MusicIconHelper.SetHandCursor(playBtn);
            MusicIconHelper.ApplyPhysicalMicroInteraction(
                playBtn,
                onClick: onPlayToggle,
                enableFloat: true,
                normalBg: BrassDark,
                hoverBg: BrassGold,
                activeBg: new Color(0.70f, 0.55f, 0.20f, 1f),
                isBrass: true);
            ctrlRow.Add(playBtn);
            h.playToggleBtn = playBtn;

            // 4. ⏭ 下一首按键 (纯 VisualElement 边框构建 ▶|，绝对锐利不走形)
            var nextBtn = new Button(() => onNext?.Invoke());
            nextBtn.style.width = 30;
            nextBtn.style.height = 30;
            nextBtn.style.backgroundColor = Color.clear;
            nextBtn.style.borderTopWidth = 0; nextBtn.style.borderBottomWidth = 0;
            nextBtn.style.borderLeftWidth = 0; nextBtn.style.borderRightWidth = 0;
            nextBtn.style.alignItems = Align.Center;
            nextBtn.style.justifyContent = Justify.Center;
            nextBtn.style.marginRight = 0;
            nextBtn.Add(MusicIconHelper.CreateTrackNavIcon(true, 13f, BrassGold));
            MusicIconHelper.SetHandCursor(nextBtn);
            nextBtn.RegisterCallback<MouseEnterEvent>(e => nextBtn.style.scale = new Scale(new Vector3(1.15f, 1.15f, 1f)));
            nextBtn.RegisterCallback<MouseLeaveEvent>(e => nextBtn.style.scale = new Scale(Vector3.one));
            ctrlRow.Add(nextBtn);
            h.nextBtn = nextBtn;

            // 下排：时间轨（当前时间 + 平滑滑动条 + 总时长）
            var timeRow = new VisualElement();
            timeRow.style.width = Length.Percent(100);
            timeRow.style.maxWidth = 480;
            timeRow.style.flexDirection = FlexDirection.Row;
            timeRow.style.alignItems = Align.Center;
            centerGroup.Add(timeRow);

            var curTime = new Label("00:00");
            curTime.style.fontSize = 9.5f;
            curTime.style.color = TextMuted;
            curTime.style.unityFontDefinition = Fd();
            curTime.style.width = 38;
            curTime.style.unityTextAlign = TextAnchor.MiddleRight;
            curTime.style.marginRight = 8;
            timeRow.Add(curTime);
            h.curTimeLbl = curTime;

            var trackBox = new VisualElement();
            trackBox.style.flexGrow = 1;
            trackBox.style.height = 14;
            trackBox.style.justifyContent = Justify.Center;
            trackBox.style.position = Position.Relative;
            trackBox.pickingMode = PickingMode.Position;
            timeRow.Add(trackBox);
            h.progressTrack = trackBox;

            var trackBg = new VisualElement();
            trackBg.style.height = 3.5f;
            trackBg.style.borderTopLeftRadius = 2;
            trackBg.style.borderTopRightRadius = 2;
            trackBg.style.borderBottomLeftRadius = 2;
            trackBg.style.borderBottomRightRadius = 2;
            trackBg.style.backgroundColor = new Color(0.28f, 0.24f, 0.20f, 1f);
            trackBox.Add(trackBg);

            var fill = new VisualElement();
            fill.style.height = Length.Percent(100);
            fill.style.width = Length.Percent(0);
            fill.style.borderTopLeftRadius = 2;
            fill.style.borderBottomLeftRadius = 2;
            fill.style.backgroundColor = BrassDark;
            trackBg.Add(fill);
            h.progressFill = fill;

            var knob = new VisualElement();
            knob.style.width = 10;
            knob.style.height = 10;
            knob.style.borderTopLeftRadius = 5;
            knob.style.borderTopRightRadius = 5;
            knob.style.borderBottomLeftRadius = 5;
            knob.style.borderBottomRightRadius = 5;
            knob.style.backgroundColor = BrassGold;
            knob.style.position = Position.Absolute;
            knob.style.left = Length.Percent(0);
            knob.style.top = 2;
            knob.style.marginLeft = -5; // 锚点居中对齐百分比
            knob.style.display = DisplayStyle.None; // 默认悬停显现
            trackBox.Add(knob);
            h.progressKnob = knob;

            var totalTime = new Label("00:00");
            totalTime.style.fontSize = 9.5f;
            totalTime.style.color = TextMuted;
            totalTime.style.unityFontDefinition = Fd();
            totalTime.style.width = 38;
            totalTime.style.unityTextAlign = TextAnchor.MiddleLeft;
            totalTime.style.marginLeft = 8;
            timeRow.Add(totalTime);
            h.totalTimeLbl = totalTime;

            MusicIconHelper.SetHandCursor(trackBox);

            // 进度条交互：悬停加粗、点击即跳、平滑拖拽
            trackBox.RegisterCallback<MouseEnterEvent>(e =>
            {
                trackBg.style.height = 5f;
                fill.style.backgroundColor = BrassGold;
                knob.style.display = DisplayStyle.Flex;
            });
            trackBox.RegisterCallback<MouseLeaveEvent>(e =>
            {
                if (!h.isScrubbing)
                {
                    trackBg.style.height = 3.5f;
                    fill.style.backgroundColor = BrassDark;
                    knob.style.display = DisplayStyle.None;
                }
            });
            trackBox.RegisterCallback<PointerDownEvent>(e =>
            {
                h.isScrubbing = true;
                trackBox.CapturePointer(e.pointerId);
                float n = Mathf.Clamp01((e.position.x - trackBox.worldBound.xMin) / Mathf.Max(1f, trackBox.worldBound.width));
                h.scrubNormalized = n;
                fill.style.width = Length.Percent(n * 100f);
                knob.style.left = Length.Percent(n * 100f);
                knob.style.display = DisplayStyle.Flex;
            });
            trackBox.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (h.isScrubbing)
                {
                    float n = Mathf.Clamp01((e.position.x - trackBox.worldBound.xMin) / Mathf.Max(1f, trackBox.worldBound.width));
                    h.scrubNormalized = n;
                    fill.style.width = Length.Percent(n * 100f);
                    knob.style.left = Length.Percent(n * 100f);
                }
            });
            trackBox.RegisterCallback<PointerUpEvent>(e =>
            {
                if (h.isScrubbing)
                {
                    h.isScrubbing = false;
                    trackBox.ReleasePointer(e.pointerId);
                    onSeek?.Invoke(h.scrubNormalized);
                }
            });

            // ==================== 3. 右段：定时器 + 调频滤镜 + 歌词/档案 + 音量 (246px 紧凑不挤占时间轨) ====================
            var rightGroup = new VisualElement();
            rightGroup.style.width = 246;
            rightGroup.style.flexShrink = 0;
            rightGroup.style.flexDirection = FlexDirection.Row;
            rightGroup.style.alignItems = Align.Center;
            rightGroup.style.justifyContent = Justify.FlexEnd;
            bar.Add(rightGroup);

            // 1. 停站自熄定时器胶囊 (sleepTimerBtn)
            var sleepTimerBtn = new Button(() => onCycleSleepTimer?.Invoke());
            sleepTimerBtn.text = "⏱ 关";
            sleepTimerBtn.style.height = 22;
            sleepTimerBtn.style.paddingLeft = 6;
            sleepTimerBtn.style.paddingRight = 6;
            sleepTimerBtn.style.fontSize = 9.5f;
            sleepTimerBtn.style.unityFontDefinition = Fd();
            sleepTimerBtn.style.backgroundColor = new Color(0.20f, 0.17f, 0.14f, 1f);
            sleepTimerBtn.style.color = TextMuted;
            sleepTimerBtn.style.borderTopLeftRadius = 11;
            sleepTimerBtn.style.borderTopRightRadius = 11;
            sleepTimerBtn.style.borderBottomLeftRadius = 11;
            sleepTimerBtn.style.borderBottomRightRadius = 11;
            sleepTimerBtn.style.borderTopWidth = 1;
            sleepTimerBtn.style.borderBottomWidth = 1;
            sleepTimerBtn.style.borderLeftWidth = 1;
            sleepTimerBtn.style.borderRightWidth = 1;
            sleepTimerBtn.style.borderTopColor = new Color(0.35f, 0.30f, 0.22f, 0.6f);
            sleepTimerBtn.style.borderBottomColor = sleepTimerBtn.style.borderTopColor;
            sleepTimerBtn.style.borderLeftColor = sleepTimerBtn.style.borderTopColor;
            sleepTimerBtn.style.borderRightColor = sleepTimerBtn.style.borderTopColor;
            sleepTimerBtn.style.marginRight = 6;
            MusicIconHelper.SetHandCursor(sleepTimerBtn);
            rightGroup.Add(sleepTimerBtn);
            h.sleepTimerBtn = sleepTimerBtn;

            // 2. 机车调频滤镜胶囊 (radioFilterBtn)
            var radioFilterBtn = new Button(() => onCycleRadioFilter?.Invoke());
            radioFilterBtn.text = "📻 原声";
            radioFilterBtn.style.height = 22;
            radioFilterBtn.style.paddingLeft = 6;
            radioFilterBtn.style.paddingRight = 6;
            radioFilterBtn.style.fontSize = 9.5f;
            radioFilterBtn.style.unityFontDefinition = Fd();
            radioFilterBtn.style.backgroundColor = new Color(0.20f, 0.17f, 0.14f, 1f);
            radioFilterBtn.style.color = TextMuted;
            radioFilterBtn.style.borderTopLeftRadius = 11;
            radioFilterBtn.style.borderTopRightRadius = 11;
            radioFilterBtn.style.borderBottomLeftRadius = 11;
            radioFilterBtn.style.borderBottomRightRadius = 11;
            radioFilterBtn.style.borderTopWidth = 1;
            radioFilterBtn.style.borderBottomWidth = 1;
            radioFilterBtn.style.borderLeftWidth = 1;
            radioFilterBtn.style.borderRightWidth = 1;
            radioFilterBtn.style.borderTopColor = new Color(0.35f, 0.30f, 0.22f, 0.6f);
            radioFilterBtn.style.borderBottomColor = radioFilterBtn.style.borderTopColor;
            radioFilterBtn.style.borderLeftColor = radioFilterBtn.style.borderTopColor;
            radioFilterBtn.style.borderRightColor = radioFilterBtn.style.borderTopColor;
            radioFilterBtn.style.marginRight = 6;
            MusicIconHelper.SetHandCursor(radioFilterBtn);
            rightGroup.Add(radioFilterBtn);
            h.radioFilterBtn = radioFilterBtn;

            // 3. 歌词/档案视窗展开开关
            var archiveBtn = new Button(() => onLyricsToggle?.Invoke());
            archiveBtn.text = "📜 档案/歌词";
            archiveBtn.style.height = 22;
            archiveBtn.style.paddingLeft = 7;
            archiveBtn.style.paddingRight = 7;
            archiveBtn.style.fontSize = 9.5f;
            archiveBtn.style.unityFontDefinition = Fd();
            archiveBtn.style.backgroundColor = new Color(0.20f, 0.17f, 0.14f, 1f);
            archiveBtn.style.color = BrassGold;
            archiveBtn.style.borderTopLeftRadius = 11;
            archiveBtn.style.borderTopRightRadius = 11;
            archiveBtn.style.borderBottomLeftRadius = 11;
            archiveBtn.style.borderBottomRightRadius = 11;
            archiveBtn.style.borderTopWidth = 1;
            archiveBtn.style.borderBottomWidth = 1;
            archiveBtn.style.borderLeftWidth = 1;
            archiveBtn.style.borderRightWidth = 1;
            archiveBtn.style.borderTopColor = new Color(0.40f, 0.33f, 0.22f, 0.7f);
            archiveBtn.style.borderBottomColor = archiveBtn.style.borderTopColor;
            archiveBtn.style.borderLeftColor = archiveBtn.style.borderTopColor;
            archiveBtn.style.borderRightColor = archiveBtn.style.borderTopColor;
            archiveBtn.style.marginRight = 8;
            MusicIconHelper.SetHandCursor(archiveBtn);
            rightGroup.Add(archiveBtn);
            h.lyricsToggleBtn = archiveBtn;

            // 4. 喇叭图标（纯代码矢量图元，朝右展开小喇叭）
            var volBtn = new Button(() => onVolMuteToggle?.Invoke());
            volBtn.text = "";
            volBtn.style.backgroundColor = Color.clear;
            volBtn.style.borderTopWidth = 0;
            volBtn.style.borderBottomWidth = 0;
            volBtn.style.borderLeftWidth = 0;
            volBtn.style.borderRightWidth = 0;
            volBtn.style.width = 22;
            volBtn.style.height = 22;
            volBtn.style.marginRight = 4;
            volBtn.style.alignItems = Align.Center;
            volBtn.style.justifyContent = Justify.Center;
            volBtn.Add(MusicIconHelper.CreateSpeakerIcon(14f, TextMuted, false));
            MusicIconHelper.SetHandCursor(volBtn);
            rightGroup.Add(volBtn);
            h.volBtn = volBtn;

            // 5. 纯 VisualElement 构造的 3px 超细工业音量滑轨 (彻底消除原生粗白方块 Slider)
            var volTrackBox = new VisualElement();
            volTrackBox.style.width = 56;
            volTrackBox.style.height = 16;
            volTrackBox.style.justifyContent = Justify.Center;
            volTrackBox.style.position = Position.Relative;
            volTrackBox.pickingMode = PickingMode.Position;
            MusicIconHelper.SetHandCursor(volTrackBox);
            rightGroup.Add(volTrackBox);
            h.volTrack = volTrackBox;

            var volBg = new VisualElement();
            volBg.style.height = 3f;
            volBg.style.borderTopLeftRadius = 1.5f; volBg.style.borderTopRightRadius = 1.5f;
            volBg.style.borderBottomLeftRadius = 1.5f; volBg.style.borderBottomRightRadius = 1.5f;
            volBg.style.backgroundColor = new Color(0.28f, 0.24f, 0.20f, 1f);
            volTrackBox.Add(volBg);

            var volFill = new VisualElement();
            volFill.style.height = Length.Percent(100);
            volFill.style.width = Length.Percent(65);
            volFill.style.borderTopLeftRadius = 1.5f; volFill.style.borderBottomLeftRadius = 1.5f;
            volFill.style.backgroundColor = BrassDark;
            volBg.Add(volFill);
            h.volFill = volFill;

            var volKnob = new VisualElement();
            volKnob.style.width = 7;
            volKnob.style.height = 7;
            volKnob.style.borderTopLeftRadius = 3.5f; volKnob.style.borderTopRightRadius = 3.5f;
            volKnob.style.borderBottomLeftRadius = 3.5f; volKnob.style.borderBottomRightRadius = 3.5f;
            volKnob.style.backgroundColor = BrassGold;
            volKnob.style.position = Position.Absolute;
            volKnob.style.left = Length.Percent(65);
            volKnob.style.top = 4.5f;
            volKnob.style.marginLeft = -3.5f;
            volTrackBox.Add(volKnob);
            h.volKnob = volKnob;

            bool isVolDragging = false;
            Action<float> applyVolDrag = (screenX) =>
            {
                if (volTrackBox.worldBound.width > 1f)
                {
                    float v = Mathf.Clamp01((screenX - volTrackBox.worldBound.xMin) / volTrackBox.worldBound.width);
                    volFill.style.width = Length.Percent(v * 100f);
                    volKnob.style.left = Length.Percent(v * 100f);
                    onVolChange?.Invoke(v);
                }
            };

            volTrackBox.RegisterCallback<PointerDownEvent>(e =>
            {
                isVolDragging = true;
                volTrackBox.CapturePointer(e.pointerId);
                applyVolDrag(e.position.x);
            });
            volTrackBox.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (isVolDragging)
                {
                    applyVolDrag(e.position.x);
                }
            });
            volTrackBox.RegisterCallback<PointerUpEvent>(e =>
            {
                if (isVolDragging)
                {
                    isVolDragging = false;
                    volTrackBox.ReleasePointer(e.pointerId);
                }
            });
            volTrackBox.RegisterCallback<WheelEvent>(e =>
            {
                float cur = (volFill.style.width.value.value / 100f);
                float next = Mathf.Clamp01(cur - Mathf.Sign(e.delta.y) * 0.05f);
                volFill.style.width = Length.Percent(next * 100f);
                volKnob.style.left = Length.Percent(next * 100f);
                onVolChange?.Invoke(next);
                e.StopPropagation();
            });

            h.volSlider = null; // 已由纯 VisualElement 轨取代

            return h;
        }

        /// <summary>
        /// 驱动伪 VU 指针摆动（以播放进度与微噪声确定性模拟机械动圈，零音频引擎开销）
        /// </summary>
        public static void UpdateVuNeedle(VisualElement needle, bool isPlaying, float playTime)
        {
            if (needle == null) return;
            if (!isPlaying)
            {
                needle.style.rotate = new Rotate(Angle.Degrees(-35f));
                return;
            }

            // 正弦主波 + 确定性高频抖动
            float baseWave = Mathf.Sin(playTime * 6.28f * 1.5f) * 0.5f + 0.5f;
            float jitter = Mathf.PerlinNoise(playTime * 12f, 0.5f) * 0.4f;
            float total = Mathf.Clamp01(baseWave * 0.6f + jitter);
            float angle = Mathf.Lerp(-35f, 35f, total);
            needle.style.rotate = new Rotate(Angle.Degrees(angle));
        }

        /// <summary>
        /// 统一刷新底栏各控制键的高亮与模式状态（纯代码矢量构建，单键三态：0列表循环, 1单曲循环, 2随机播放）
        /// </summary>
        public static void UpdateControlsState(
            PlayerBarHandle h,
            bool isPlaying,
            bool isShuffleActive,
            int playMode) // 0: 顺序/列表循环, 1: 单曲循环, 2: 随机播放
        {
            if (h == null) return;

            // 1. 播放/暂停大圆纽 (38×38 黑金对比纯圆)
            if (h.playToggleBtn != null)
            {
                h.playToggleBtn.Clear();
                h.playToggleBtn.Add(isPlaying 
                    ? MusicIconHelper.CreatePauseIcon(3.2f, 13f, 4.5f, new Color(0.12f, 0.10f, 0.08f, 1f)) 
                    : MusicIconHelper.CreatePlayIcon(13f, new Color(0.12f, 0.10f, 0.08f, 1f)));
            }

            // 2. 播放模式单键轮转（0: 列表循环, 1: 单曲循环, 2: 随机播放）
            if (h.modeBtn != null)
            {
                h.modeBtn.Clear();
                Color activeCol = BrassGold;
                Color mutedCol = TextMuted;

                VisualElement icon;
                if (playMode == 1) // 单曲循环
                {
                    icon = MusicIconHelper.CreateLoopRepeatIcon(activeCol, true);
                }
                else if (playMode == 2) // 随机播放
                {
                    icon = MusicIconHelper.CreateCrossShuffleIcon(activeCol);
                }
                else // 0 列表循环
                {
                    icon = MusicIconHelper.CreateLoopRepeatIcon(mutedCol, false);
                }
                icon.pickingMode = PickingMode.Ignore;
                h.modeBtn.Add(icon);

                // 单曲或随机时底部点亮 3px 金色微型信标
                if (playMode != 0)
                {
                    var dot = new VisualElement();
                    dot.style.position = Position.Absolute;
                    dot.style.bottom = 2;
                    dot.style.width = 3; dot.style.height = 3;
                    dot.style.borderTopLeftRadius = 1.5f; dot.style.borderTopRightRadius = 1.5f;
                    dot.style.borderBottomLeftRadius = 1.5f; dot.style.borderBottomRightRadius = 1.5f;
                    dot.style.backgroundColor = activeCol;
                    dot.pickingMode = PickingMode.Ignore;
                    h.modeBtn.Add(dot);
                }
            }
        }

        public static void UpdateVolumeMuteVisuals(PlayerBarHandle h, bool isMuted, float volume)
        {
            if (h == null) return;
            if (h.volBtn != null)
            {
                h.volBtn.Clear();
                h.volBtn.Add(MusicIconHelper.CreateSpeakerIcon(14f, isMuted ? new Color(0.95f, 0.40f, 0.40f, 1f) : TextMuted, isMuted));
            }
            float v = isMuted ? 0f : Mathf.Clamp01(volume);
            if (h.volFill != null) h.volFill.style.width = Length.Percent(v * 100f);
            if (h.volKnob != null) h.volKnob.style.left = Length.Percent(v * 100f);
            if (h.volSlider != null) h.volSlider.value = volume;
        }
    }
}
