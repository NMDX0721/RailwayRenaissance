using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public sealed class MusicLyricsView
    {
        readonly VisualElement host;
        readonly Action close;
        readonly List<Button> rows = new List<Button>();
        readonly List<(int line, MusicTimedWord word, VisualElement fill, VisualElement body, Label light)> words = new List<(int, MusicTimedWord, VisualElement, VisualElement, Label)>();
        MusicTimedDocument document;
        MusicStageLyrics stage;
        ScrollView scroll;
        Label heading, offsetLabel, help;
        StatusLines status;
        // Source name and detail on separate labels: the pixel font has no line spacing, so one two-line label overlaps.
        sealed class StatusLines : VisualElement
        {
            readonly Label first = new Label(), second = new Label();
            public StatusLines() { first.AddToClassList("music-lyrics-source"); second.AddToClassList("music-lyrics-detail"); Add(first); Add(second); }
            public string text
            {
                get => first.text + (second.text.Length > 0 ? "\n" + second.text : "");
                set { var parts = (value ?? "").Split(new[] { '\n' }, 2); first.text = parts[0]; second.text = parts.Length > 1 ? parts[1] : ""; second.style.display = second.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None; }
            }
        }
        VisualElement stageProgress;
        int filteredLines;
        MusicReactiveStageView reactiveStage;
        MusicIconHelper.VuMeterHandle vu;
        Button followButton;
        Slider progress;
        Label playTime;
        string trackId; float chorusLevel;
        bool instrumental;
        bool following = true, scrubbing;
        bool focused;
        int active = -2;
        float targetY;
        public MusicLyricsView(VisualElement parent, Action onClose)
        {
            host = parent; close = onClose;
            host.AddToClassList("music-lyrics-surface");
            Build();
        }
        static Label Text(VisualElement parent, string text, string css)
        {
            var label = new Label(text) { enableRichText = false }; label.AddToClassList(css);
            if (ContainsHangul(text))
            {
                var font = Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
                if (font != null) label.style.unityFontDefinition = new FontDefinition { font = font };
            }
            parent.Add(label); return label;
        }
        static bool ContainsHangul(string value)
        {
            if (value == null) return false;
            foreach (char c in value) if ((c >= '\uAC00' && c <= '\uD7AF') || (c >= '\u1100' && c <= '\u11FF') || (c >= '\u3130' && c <= '\u318F')) return true;
            return false;
        }
        static MusicLyricsView openView;
        /// <summary>The settings page changes stage options while the lyrics page is open behind it; this makes the stage pick them up.</summary>
        public static void InvalidateStage() { openView?.stage?.Invalidate(); openView?.RenderRows(); }
        /// <summary>Lyric options changed on the settings page: re-read the lyrics (filters) or only redraw the rows.</summary>
        public static void Refresh(bool reload) { if (openView == null) return; if (reload) openView.Reload(); else { openView.stage?.Invalidate(); openView.RenderRows(); } }
        static Texture2D topFade;
        static Texture2D TopFadeTexture()
        {
            if (topFade != null) return topFade;
            const int rows = 48;
            var texture = new Texture2D(1, rows, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < rows; y++)
            {
                float t = y / (rows - 1f);                  // texture y = 0 is the bottom edge
                float alpha = Mathf.Pow(t, 1.6f);
                texture.SetPixel(0, y, new Color(0.085f, 0.09f, 0.085f, alpha));
            }
            texture.Apply();
            return topFade = texture;
        }
        static VisualElement Box(VisualElement parent, string css)
        {
            var element = new VisualElement(); element.AddToClassList(css); parent.Add(element); return element;
        }
        static Button Action(VisualElement parent, string text, System.Action action, string name = null, bool icon = false)
        {
            var button = new Button(action) { name = name }; button.AddToClassList("music-button"); parent.Add(button);
            MusicTerminalIcons.Apply(button, text, icon); return button;
        }
        void Build()
        {
            openView = this;
            host.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                if (evt.target == host && openView == this) openView = null;
            });
            var previousStage = reactiveStage;
            previousStage?.RemoveFromHierarchy();
            host.Clear();
            reactiveStage = previousStage ?? new MusicReactiveStageView(); host.Add(reactiveStage);
            // The tablet's dark status strip sits right above this page: fade the top edge into it instead of meeting it with a hard line.
            var topFade = Box(host, "music-lyrics-topfade"); topFade.pickingMode = PickingMode.Ignore;
            topFade.style.backgroundImage = new StyleBackground(TopFadeTexture());
            var header = Box(host, "music-lyrics-header");
            heading = Text(header, "歌词 / 字幕", "music-grow");
            Action(header, "专注阅读", () => {
                focused = !focused; host.EnableInClassList("music-lyrics-focused", focused);
                MusicTerminalIcons.Apply(host.Q<Button>("music-lyrics-focus"), focused ? "显示设置" : "专注阅读");
                active = -2;
            }, "music-lyrics-focus");
            var motion = Action(header, MotionTitle(), () => {
                MusicLibrarySession.State.lyricMotion = (MusicLibrarySession.State.lyricMotion + 1) % 3;
                MusicLibrarySession.Save();
                ApplyMotionStyle(); RenderRows();
                MusicTerminalIcons.Apply(host.Q<Button>("music-lyrics-motion"), MotionTitle());
                host.Q<Button>("music-lyrics-motion").tooltip = "切换歌词表现：静读（平铺阅读）/ 随行（当前句放大，远处渐隐）/ 舞台（居中单句，随音乐起伏）；减少动态效果优先";
            }, "music-lyrics-motion");
            motion.tooltip = "切换歌词表现：静读（平铺阅读）/ 随行（当前句放大，远处渐隐）/ 舞台（居中单句，随音乐起伏）；减少动态效果优先";
            // One style switch for every lyric look: 静读, 随行, then the stage styles. (The mode button above stays as the programmatic switch but is not shown.)
            motion.style.display = DisplayStyle.None;
            var quickStyle = new Button { name = "music-lyrics-style", text = "样式：" + StyleTitle() };
            quickStyle.AddToClassList("music-button"); quickStyle.AddToClassList("music-lyrics-style-button"); header.Add(quickStyle);
            quickStyle.tooltip = "切换歌词样式：静读 / 随行 / 各种舞台样式（背景也随样式变化）";
            quickStyle.clicked += () =>
            {
                CycleStyle(); ApplyMotionStyle(); RenderRows(); active = -2;
                quickStyle.text = "样式：" + StyleTitle();
                var inner = host.Q<Button>("music-stage-style"); if (inner != null) inner.text = "样式：" + StageStyles.TitleOf(MusicLibrarySession.State.stageStyle);
            };
            Action(header, "关闭", close, "music-lyrics-close", true);
            var body = Box(host, "music-lyrics-body");
            var art = Box(body, "music-lyrics-art");
            var track = MusicLibrarySession.Find(TabletMusicAppUI.tabletCurrentTrackId);
            reactiveStage.SetTrack(track);
            var cover = Box(art, "music-lyrics-cover");
            Texture2D coverTexture = MusicAlbumArtResolver.GetAlbumCover(track?.id, track?.category);
            cover.style.backgroundImage = new StyleBackground(coverTexture);
            cover.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            void FitCover()
            {
                if (cover.panel == null || coverTexture == null || coverTexture.width <= 0) return;
                float room = art.resolvedStyle.width - art.resolvedStyle.paddingLeft - art.resolvedStyle.paddingRight;
                if (float.IsNaN(room) || room <= 0) return;
                bool compact = false;
                for (var ancestor = host; ancestor != null; ancestor = ancestor.parent) if (ancestor.ClassListContains("music-compact")) { compact = true; break; }
                // On a short column (the column itself is under 470 px high, which is what a 700 px high tablet gives it) the cover shrinks and the
                // atmosphere line goes, so the information, the source line and the record archive button all stay inside the column.
                bool shortScreen = art.resolvedStyle.height > 0 && art.resolvedStyle.height < 470;
                art.EnableInClassList("music-lyrics-short", shortScreen);
                float width = Mathf.Min(room, compact ? 140f : shortScreen ? 100f : 220f), ratio = Mathf.Clamp((float)coverTexture.height / coverTexture.width, .6f, 1.4f);
                cover.style.width = width; cover.style.height = width * ratio;
            }
            art.RegisterCallback<GeometryChangedEvent>(_ => FitCover());
            if (track != null) MusicAlbumArtResolver.LoadRemoteCover(track.id, image => {
                if (cover.panel == null || reactiveStage.TrackId != track.id) return;
                coverTexture = image; cover.style.backgroundImage = new StyleBackground(image); FitCover(); reactiveStage.SetTrack(track, true);
            });
            Text(art, track?.title ?? "尚未选择曲目", "music-detail-title");
            if (track != null && !string.IsNullOrEmpty(track.titleTranslation)) Text(art, track.titleTranslation, "music-muted");
            Text(art, "沿线声音档案 / RAILWAY SOUND", "music-eyebrow");
            // Song information, from the same track data as the record archive: who sings it, the album, where it comes from and how long it is;
            // a soundtrack also shows how it is unlocked and its line of atmosphere. The settings below stay folded away.
            // Record archive and lyric settings are two small icon buttons under the title, so the column never runs under the player bar.
            var quick = Box(art, "music-lyrics-quick"); Button settingsOpen;
            var record = Action(quick, "唱片档案", () => RailwayMusicTerminalView.OpenRecord(trackId), "music-lyrics-record", true);
            record.tooltip = "唱片档案：打开这首曲目的完整档案（盖在歌词页上，关闭后回到歌词）";
            var info = Box(art, "music-lyrics-info");
            void InfoRow(string key, string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var row = Box(info, "music-lyrics-info-row"); Text(row, key, "music-lyrics-info-key"); Text(row, value, "music-lyrics-info-value");
            }
            if (track != null)
            {
                InfoRow("歌手", track.artist); InfoRow("专辑", track.album); InfoRow("来源", track.category);
                float length = track.duration > 0 ? track.duration : TabletMusicAppUI.tabletCurrentTrackId == track.id && TabletMusicAppUI.tabletCurrentClip != null ? TabletMusicAppUI.tabletCurrentClip.length : 0;
                InfoRow("时长", length > 0 ? TabletMusicAppUI.FormatAudioTime(length) : null);
                if (track.local) InfoRow("文件", track.fileName);
                if (!track.local && string.IsNullOrEmpty(track.provider))
                {
                    InfoRow("解锁", track.condition);
                    if (!string.IsNullOrEmpty(track.description)) Text(info, track.description, "music-lyrics-info-note");
                }
            }
            status = new StatusLines(); status.AddToClassList("music-lyrics-status"); art.Add(status);
            // The lyric settings live in their own sheet over the page, not in the narrow column under the cover where the player bar used to cover them.
            var sheet = Box(host, "music-lyrics-sheet"); sheet.name = "music-lyrics-sheet"; sheet.style.display = DisplayStyle.None;
            sheet.RegisterCallback<PointerDownEvent>(e => { if (e.target == sheet) sheet.style.display = DisplayStyle.None; });
            var sheetPanel = Box(sheet, "music-lyrics-sheet-panel"); sheetPanel.AddToClassList("music-lyrics-art");
            var sheetHead = Box(sheetPanel, "music-modal-header"); Text(sheetHead, "这首歌的歌词", "music-grow");
            Action(sheetHead, "关闭", () => sheet.style.display = DisplayStyle.None, "music-lyrics-sheet-close", true);
            var sheetScroll = new ScrollView { verticalScrollerVisibility = ScrollerVisibility.Auto, name = "music-lyrics-sheet-scroll" }; sheetScroll.AddToClassList("music-lyrics-sheet-scroll"); sheetPanel.Add(sheetScroll);
            settingsOpen = Action(quick, "歌词设置", () => { sheet.style.display = DisplayStyle.Flex; sheet.BringToFront(); }, "music-lyrics-settings-open", true); settingsOpen.tooltip = "歌词设置";
            Action(sheetPanel, "全部设置", () => { sheet.style.display = DisplayStyle.None; RailwayMusicTerminalView.OpenSettingsPage("lyrics"); }, "music-lyrics-all-settings");
            var more = new Foldout { text = "歌词设置", value = true, name = "music-lyrics-more" }; more.AddToClassList("music-lyrics-more"); more.AddToClassList("music-lyrics-more-open"); sheetScroll.Add(more);
            var imports = Box(more, "music-lyrics-actions");
            Action(imports, "导入字幕", () => Import(false), "music-lyrics-import");
            Action(imports, "导入翻译", () => Import(true), "music-lyrics-import-translation");
            Action(more, "重新读取", Reload, "music-lyrics-reload");
            Text(more, "字号、翻译、过滤和舞台样式等通用选项在 全部设置 › 歌词。", "music-lyrics-hint");
            Text(more, "时间校准", "music-lyrics-section");
            var timing = Box(more, "music-player-buttons");
            timing.name = "music-lyrics-timing";
            Action(timing, "延后 100ms", () => Adjust(-100), "music-lyrics-later", true);
            offsetLabel = Text(timing, "0 ms", "music-lyric-offset");
            Action(timing, "提前 100ms", () => Adjust(100), "music-lyrics-earlier", true);
            Action(more, "重置字幕时间", () => { var p = MusicTimedText.Preference(trackId); p.offsetMs = 0; MusicLibrarySession.Save(); active = -2; }, "music-lyrics-reset");
            var textArea = Box(body, "music-lyrics-text-area");
            // The lyric subtitles are always set in the project's pixel font, whatever font the tablet system UI uses around them.
            var pixelFont = StageFonts.Pixel();
            if (pixelFont != null) textArea.style.unityFontDefinition = new FontDefinition { font = pixelFont };
            var cueRail = Box(textArea, "music-cue-rail");
            stageProgress = Box(cueRail, "music-cue-progress");
            cueRail.tooltip = "当前句播放进度（按真实字幕时间）";
            followButton = Action(textArea, "跟随当前句", () => { following = true; active = -2; }, "music-lyrics-follow");
            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "music-lyrics-scroll", horizontalScrollerVisibility = ScrollerVisibility.Hidden }; scroll.AddToClassList("music-lyrics-scroll"); textArea.Add(scroll);
            scroll.RegisterCallback<WheelEvent>(_ => following = false, TrickleDown.TrickleDown);
            scroll.verticalScroller.RegisterCallback<PointerDownEvent>(_ => following = false, TrickleDown.TrickleDown);
            stage = new MusicStageLyrics { pickingMode = PickingMode.Ignore, name = "music-stage-lyrics" }; stage.style.display = DisplayStyle.None; textArea.Add(stage);
            var footer = Box(host, "music-lyrics-footer");
            Action(footer, "上一首", TabletMusicAppUI.PlayPrevTabletTrack, icon: true);
            var play = Action(footer, "播放", TabletMusicAppUI.ToggleTabletMusicPlayPause, "music-lyrics-play", true); play.AddToClassList("music-primary");
            Action(footer, "下一首", TabletMusicAppUI.PlayNextTabletTrack, icon: true);
            progress = new Slider(0, 1) { name = "music-lyrics-progress" }; progress.style.flexGrow = 1; footer.Add(progress);
            progress.RegisterCallback<PointerDownEvent>(_ => scrubbing = true, TrickleDown.TrickleDown);
            progress.RegisterCallback<PointerUpEvent>(_ => scrubbing = false, TrickleDown.TrickleDown);
            progress.RegisterCallback<PointerCaptureOutEvent>(_ => scrubbing = false);
            progress.RegisterValueChangedCallback(e => TabletMusicAppUI.SeekTabletMusic(e.newValue));
            playTime = Text(footer, "0:00 / --:--", "music-player-time");
            // The meters moved here from the player bar: left and right needles follow the music's low and high bands.
            vu = MusicIconHelper.CreateDualVuMeter(168f, 40f); vu.root.AddToClassList("music-lyrics-vu"); footer.Add(vu.root);
            help = Text(host, "", "music-lyrics-help");
            trackId = TabletMusicAppUI.tabletCurrentTrackId;
            ApplyMotionStyle();
            Reload();
            // A new song fades the lyric layer in over the backdrop (which already blends to the new cover's colours), instead of switching hard.
            if (previousStage != null)
            {
                var fading = new System.Collections.Generic.List<VisualElement>();
                foreach (var child in host.Children()) if (child != reactiveStage) { child.AddToClassList("music-lyrics-entering"); fading.Add(child); }
                host.schedule.Execute(() => { foreach (var child in fading) child.RemoveFromClassList("music-lyrics-entering"); }).StartingIn(60);
            }
        }
        // Unified lyric style: 0 静读, 1 随行, then every listed stage style in order.
        static string StyleTitle() { int m = MusicLibrarySession.State.lyricMotion; return m == 0 ? "静读" : m == 1 ? "随行" : StageStyles.TitleOf(MusicLibrarySession.State.stageStyle); }
        static string LookKey() { int m = MusicLibrarySession.State.lyricMotion; return m == 0 ? "static" : m == 1 ? "follow" : MusicLibrarySession.State.stageStyle; }
        static void CycleStyle()
        {
            var state = MusicLibrarySession.State;
            if (state.lyricMotion == 0) state.lyricMotion = 1;
            else if (state.lyricMotion == 1) { state.lyricMotion = 2; state.stageStyle = StageStyles.IdAt(0); }
            else if (StageStyles.IndexOf(state.stageStyle) >= StageStyles.Count - 1) state.lyricMotion = 0;
            else state.stageStyle = StageStyles.Next(state.stageStyle);
            MusicLibrarySession.Save();
        }
        static string MotionTitle() => new[] { "静读", "随行", "舞台" }[Mathf.Clamp(MusicLibrarySession.State.lyricMotion, 0, 2)];
        void ApplyMotionStyle()
        {
            host.EnableInClassList("music-motion-static", MusicLibrarySession.State.lyricMotion == 0);
            host.EnableInClassList("music-motion-follow", MusicLibrarySession.State.lyricMotion == 1);
            host.EnableInClassList("music-motion-stage", MusicLibrarySession.State.lyricMotion == 2);
            reactiveStage?.SetLook(LookKey());
            var styleButton = host.Q<Button>("music-lyrics-style"); if (styleButton != null) styleButton.text = "样式：" + StyleTitle();
            var tuningBox = host.Q("music-lyrics-stage-tuning");
            if (tuningBox != null) tuningBox.style.display = MusicLibrarySession.State.lyricMotion == 2 ? DisplayStyle.Flex : DisplayStyle.None;
        }
        void Adjust(int milliseconds)
        {
            var pref = MusicTimedText.Preference(trackId); pref.offsetMs = Mathf.Clamp(pref.offsetMs + milliseconds, -10000, 10000);
            MusicLibrarySession.Save(); active = -2;
        }
        void Import(bool translation)
        {
            string selectedId = trackId;
            LocalMusicFileImporter.PromptImportLyrics(path => {
                MusicTimedText.Import(selectedId, path, translation, out string message);
                TabletMusicAppUI.ShowToast(message);
                if (selectedId == trackId) Reload();
            });
        }
        void Reload()
        {
            var track = MusicLibrarySession.Find(trackId);
            instrumental = false;
            document = MusicTimedText.Load(track);
            filteredLines = MusicLyricFilter.Apply(document, MusicLibrarySession.State.lyricFilter, MusicLibrarySession.State.lyricFilterWords);
            following = true; RenderRows();
            if (track == null || track.provider != "netease" && track.provider != "bodian" && track.provider != "spotify" || !string.IsNullOrEmpty(MusicTimedText.Preference(track.id).fileName)) return;
            string requestedId = track.id;
            status.text = "正在读取来源歌词…";
            NeteaseLocalBridge.ForProvider(track.provider).Send("lyric", new NeteaseLocalBridge.Parameters { id = track.remoteId }, result => {
                if (host.panel == null || trackId != requestedId || !string.IsNullOrEmpty(MusicTimedText.Preference(requestedId).fileName)) return;
                if (result.ok && result.result != null && result.result.pureMusic) { instrumental = true; RenderRows(); return; }
                if (!result.ok || result.result == null || string.IsNullOrWhiteSpace(result.result.lyric))
                { status.text = result.ok ? track.category + "暂无同步歌词；可导入字幕。" : "歌词读取失败，可重试或导入字幕。"; return; }
                // Prefer the source's own word-timed lyrics; fall back to line timing when absent or unusable.
                var fetched = string.IsNullOrWhiteSpace(result.result.wordLyric) ? null : MusicTimedText.Parse(result.result.wordLyric, ".lrc");
                if (fetched == null || fetched.lines.Count == 0 || !fetched.lines.Exists(l => l.words.Count > 0)) fetched = MusicTimedText.Parse(result.result.lyric, ".lrc");
                filteredLines = MusicLyricFilter.Apply(fetched, MusicLibrarySession.State.lyricFilter, MusicLibrarySession.State.lyricFilterWords);
                if (fetched.lines.Count == 0) { status.text = track.category + "未提供可用的同步歌词。"; return; }
                fetched.source = track.category + "歌词";
                if (!string.IsNullOrEmpty(result.result.translation) && string.IsNullOrEmpty(MusicTimedText.Preference(requestedId).translationFileName))
                {
                    var translated = MusicTimedText.Parse(result.result.translation, ".lrc");
                    MusicLyricFilter.Apply(translated, MusicLibrarySession.State.lyricFilter, MusicLibrarySession.State.lyricFilterWords);
                    // Each translated line goes to the nearest original line (within 1.5 s); a closer one replaces a farther one.
                    var gaps = new double[fetched.lines.Count];
                    foreach (var candidate in translated.lines)
                    {
                        if (string.IsNullOrWhiteSpace(candidate.text)) continue;
                        int best = -1; double bestGap = 1.5;
                        for (int n = 0; n < fetched.lines.Count; n++)
                        {
                            double gap = Math.Abs(candidate.start - fetched.lines[n].start);
                            if (gap < bestGap && (string.IsNullOrEmpty(fetched.lines[n].translation) || gap < gaps[n])) { best = n; bestGap = gap; }
                        }
                        if (best >= 0) { fetched.lines[best].translation = candidate.text; gaps[best] = bestGap; }
                    }
                }
                MusicTimedText.ApplyImportedTranslation(requestedId, fetched);
                document = fetched; RenderRows();
            });
        }
        void RenderRows()
        {
            if (scroll == null || document == null) return;
            scroll.Clear(); rows.Clear(); words.Clear(); active = -2;
            scroll.contentContainer.style.paddingTop = 0;
            scroll.contentContainer.style.paddingBottom = 0;
            bool timed = document.lines.Count > 0;
            host.Q("music-lyrics-timing")?.SetEnabled(timed);
            host.Q<Button>("music-lyrics-reset")?.SetEnabled(timed);
            help.text = (timed ? "点击句子定位" : "滚动阅读歌词 · 导入 LRC 后开启同步") + " · 空格播放/暂停 · 左右方向键 ±5 秒 · Esc 返回";
            if (document.lines.Count == 0)
            {
                if (!string.IsNullOrEmpty(document.plainText))
                {
                    status.text = document.source + "\n歌词阅读 · 尚无同步时间";
                    Text(scroll, "歌词原文 / 阅读模式", "music-eyebrow");
                    Text(scroll, "导入这首歌的 LRC 可开启逐句同步。", "music-muted");
                    var plain = Text(scroll, document.plainText.Replace("\n", "\n\n"), "music-plain-lyrics");
                    plain.style.fontSize = MusicLibrarySession.State.lyricFontSize;
                    scroll.schedule.Execute(() => scroll.scrollOffset = Vector2.zero).StartingIn(50);
                    return;
                }
                if (!string.IsNullOrEmpty(document.error)) { Text(scroll, document.error, "music-lyric-empty"); status.text = "暂无字幕"; return; }
                // Instrumental tracks (a source flag, or a built-in original with no lyric sheet) say so, like NetEase does.
                var current = MusicLibrarySession.Find(trackId);
                if (instrumental || current != null && !current.local && string.IsNullOrEmpty(current.provider))
                { Text(scroll, "纯音乐，请欣赏", "music-lyric-pure"); status.text = "纯音乐\n这首曲目没有歌词"; return; }
                Text(scroll, "暂无歌词", "music-lyric-pure"); Text(scroll, "可导入 LRC、SRT、WebVTT 或逐词 JSON", "music-lyric-hint");
                status.text = "暂无字幕"; return;
            }
            status.text = document.source + "\n" + document.lines.FindAll(l => !string.IsNullOrWhiteSpace(l.text)).Count + " 行 · " + (document.lines.Exists(l => l.words.Count > 0) ? "含逐词时间" : MusicLibrarySession.State.estimateWords ? "逐词（按行估算）" : "逐行同步") + (filteredLines > 0 ? " · 已过滤 " + filteredLines + " 行制作信息" : "");
            for (int i = 0; i < document.lines.Count; i++)
            {
                var line = document.lines[i]; int index = i;
                var row = new Button(() => {
                    var clip = TabletMusicAppUI.tabletCurrentClip;
                    if (clip == null || TabletMusicAppUI.tabletCurrentTrackId != trackId)
                    { TabletMusicAppUI.ShowToast("先播放当前曲目，再点击字幕定位"); return; }
                    var position = MusicTimedText.SeekPosition(line.start, MusicTimedText.EffectiveOffsetMs(trackId), clip.length);
                    if (!position.HasValue) { TabletMusicAppUI.ShowToast("这一句超出当前音频时长，未跳转"); return; }
                    TabletMusicAppUI.SeekTabletMusic((float)(position.Value / clip.length));
                    following = true; active = -2;
                }) { name = "music-lyric-line-" + index, text = "" };
                row.AddToClassList("music-lyric-line"); row.tooltip = "定位到 " + TabletMusicAppUI.FormatAudioTime((float)line.start); scroll.Add(row); rows.Add(row);
                if (string.IsNullOrWhiteSpace(line.text)) row.AddToClassList("music-lyric-break");
                var lineWords = line.words;
                if (lineWords.Count == 0 && MusicLibrarySession.State.estimateWords && !string.IsNullOrWhiteSpace(line.text))
                {
                    var guess = MusicLyricEstimate.Words(line, index + 1 < document.lines.Count ? document.lines[index + 1].start : double.PositiveInfinity);
                    if (guess.Count > 0) lineWords = guess;
                }
                if (lineWords.Count == 0)
                {
                    var label = Text(row, string.IsNullOrEmpty(line.text) ? "· · ·" : line.text, "music-lyric-text");
                    label.style.fontSize = string.IsNullOrWhiteSpace(line.text) ? 16 : MusicLibrarySession.State.lyricFontSize;
                }
                else
                {
                    var flow = Box(row, "music-lyric-words");
                    foreach (var word in lineWords)
                    {
                        var span = Box(flow, "music-lyric-word-span");
                        var label = Text(span, word.text, "music-lyric-word"); label.style.fontSize = MusicLibrarySession.State.lyricFontSize;
                        var fill = Box(span, "music-lyric-word-fill");
                        var light = Text(fill, word.text, "music-lyric-word-light"); light.style.fontSize = MusicLibrarySession.State.lyricFontSize;
                        words.Add((index, word, fill, span, light));
                    }
                }
                if (MusicLibrarySession.State.lyricTranslation && !string.IsNullOrEmpty(line.translation)) Text(row, line.translation, "music-lyric-translation");
            }
            scroll.contentContainer.style.paddingTop = 140; scroll.contentContainer.style.paddingBottom = 180;
        }
        public void Tick()
        {
            string id = TabletMusicAppUI.tabletCurrentTrackId;
            if (trackId != id) { Build(); return; }
            if (document == null) return;
            if (vu != null)
            {
                bool playing = TabletMusicAppUI.tabletIsPlaying && reactiveStage != null;
                float left = playing ? Mathf.Clamp01(reactiveStage.BandLow * .7f + reactiveStage.BandMid * .5f) : 0f;
                float right = playing ? Mathf.Clamp01(reactiveStage.BandMid * .5f + reactiveStage.BandHigh * .9f) : 0f;
                vu.UpdateLevels(left, right, Time.unscaledDeltaTime);
            }
            var preference = MusicTimedText.Preference(trackId);
            int totalOffset = MusicTimedText.EffectiveOffsetMs(trackId);
            offsetLabel.text = (totalOffset > 0 ? "+" : "") + totalOffset + " ms";
            // The stage never scrolls, so the "back to current line" banner has nothing to return to there.
            followButton.style.display = following || document.lines.Count == 0 || MusicLibrarySession.State.lyricMotion == 2 ? DisplayStyle.None : DisplayStyle.Flex;
            heading.text = (TabletMusicAppUI.tabletCurrentTrackTitle ?? "歌词 / 字幕") + " / " + (TabletMusicAppUI.tabletIsPlaying ? "正在播放" : "已暂停");
            if (document.source != null && document.source.Contains("草稿")) heading.text += " · 待听校";
            heading.tooltip = document.source;
            var clip = TabletMusicAppUI.tabletCurrentClip;
            playTime.text = TabletMusicAppUI.FormatAudioTime(TabletMusicAppUI.CurrentPosition) + " / " + (clip == null ? "--:--" : TabletMusicAppUI.FormatAudioTime(clip.length));
            progress.SetEnabled(clip != null);
            if (!scrubbing) progress.SetValueWithoutNotify(clip != null && clip.length > 0 ? TabletMusicAppUI.CurrentPosition / clip.length : 0);
            RailwayMusicTerminalView.UpdateSliderFill(progress, progress.value);
            MusicTerminalIcons.Apply(host.Q<Button>("music-lyrics-play"), TabletMusicAppUI.tabletIsPlaying ? "暂停" : "播放", true);
            double clock = TabletMusicAppUI.CurrentPosition + totalOffset / 1000d;
            int next = document.Active(clock);
            bool lineChanged = active != next;
            if (lineChanged)
            {
                active = next;
                for (int i = 0; i < rows.Count; i++)
                {
                    rows[i].EnableInClassList("is-current", i == active);
                    rows[i].EnableInClassList("is-near", Mathf.Abs(i - active) == 1);
                    rows[i].EnableInClassList("is-distant", Mathf.Abs(i - active) > 1);
                    // Reading: flat list. Follow: the current line grows and the rest fade with distance. Stage: one centred line, neighbours barely there.
                    int mode = MusicLibrarySession.State.lyricMotion; bool calm = MusicLibrarySession.State.reduceMotion;
                    int d = active < 0 ? i + 1 : Mathf.Abs(i - active);
                    rows[i].style.opacity = mode == 0 ? (i == active ? 1f : .8f)
                        : mode == 1 ? (d == 0 ? 1f : d == 1 ? .62f : d == 2 ? .42f : .26f)
                        : (d == 0 ? 1f : d == 1 ? .46f : d == 2 ? .16f : 0f);
                    rows[i].style.scale = !calm && i == active && mode != 0 ? new StyleScale(new Scale(new Vector3(mode == 2 ? 1.16f : 1.05f, mode == 2 ? 1.16f : 1.05f, 1))) : new StyleScale(StyleKeyword.Null);
                    rows[i].style.backgroundColor = mode == 1 && i == active && !string.IsNullOrWhiteSpace(document.lines[i].text)
                        ? new StyleColor(Color.Lerp(new Color(.06f, .07f, .075f), reactiveStage.PrimaryColor, .2f)) : new StyleColor(StyleKeyword.Null);
                }
            }
            bool validCue = active >= 0 && active < document.lines.Count && !string.IsNullOrWhiteSpace(document.lines[active].text);
            double cueLength = validCue ? document.lines[active].end - document.lines[active].start : 0;
            float cueFraction = validCue && !double.IsInfinity(cueLength) && cueLength > 0
                ? Mathf.Clamp01((float)((clock - document.lines[active].start) / cueLength)) : 0;
            stageProgress.style.width = Length.Percent(cueFraction * 100);
            bool inChorus = active >= 0 && document.IsChorus(active) && TabletMusicAppUI.tabletIsPlaying;
            chorusLevel = MusicLibrarySession.State.reduceMotion ? 0f : Mathf.Lerp(chorusLevel, inChorus ? 1f : 0f, 1 - Mathf.Exp(-Time.unscaledDeltaTime * (inChorus ? 2.5f : 1.2f)));
            reactiveStage.ChorusBoost = chorusLevel;
            reactiveStage.Tick(MusicAudioEffectsEngine.GetActiveDeck(), TabletMusicAppUI.tabletIsPlaying,
                MusicLibrarySession.State.reduceMotion || MusicLibrarySession.State.lyricMotion == 0);
            // Stage mode draws the current line on its own layer (Folia-style word pop); the list stays underneath, hidden.
            bool stageOn = MusicLibrarySession.State.lyricMotion == 2 && document.lines.Count > 0;
            stage.style.display = stageOn ? DisplayStyle.Flex : DisplayStyle.None;
            scroll.style.opacity = stageOn ? 0f : 1f; scroll.pickingMode = stageOn ? PickingMode.Ignore : PickingMode.Position;
            if (stageOn)
            {
                stage.Select(MusicLibrarySession.State.stageStyle);
                stage.Tick(document, active, clock, Time.unscaledDeltaTime, MusicLibrarySession.State.reduceMotion, MusicLibrarySession.State.estimateWords,
                    Mathf.Clamp(MusicLibrarySession.State.lyricFontSize / 24f, .75f, 1.5f), reactiveStage.PrimaryColor, reactiveStage.VisualEnergy, MusicLibrarySession.State.lyricTranslation,
                    StageTuning.FromState(MusicLibrarySession.State), reactiveStage.BandLow, reactiveStage.BandMid, reactiveStage.BandHigh, TabletMusicAppUI.tabletCurrentTrackTitle, chorusLevel);
            }
            foreach (var pair in words)
            {
                if (!lineChanged && pair.line != active) continue;
                double length = pair.word.end - pair.word.start;
                float fraction = clock <= pair.word.start ? 0 : double.IsInfinity(length) ? 1 : Mathf.Clamp01((float)((clock - pair.word.start) / Math.Max(.001, length)));
                pair.fill.style.width = Length.Percent(100 * fraction);
                // Pose is a function of real audio time: pause freezes it; seeking reconstructs it.
                bool stage = MusicLibrarySession.State.lyricMotion == 2 && !MusicLibrarySession.State.reduceMotion;
                float lift = stage && pair.line == active && !double.IsInfinity(length) ? Mathf.Sin(Mathf.PI * fraction) : 0;
                pair.body.style.translate = new Translate(0, -3 * lift);
                pair.body.style.scale = new Scale(new Vector3(1 + .025f * lift, 1 + .025f * lift, 1));
                pair.light.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = stage ? 4 * lift : 0, color = new Color(.95f, .73f, .32f, .45f * lift) };
            }
            if (following && active >= 0 && active < rows.Count)
            {
                bool centred = MusicLibrarySession.State.lyricMotion == 2;
                float anchor = scroll.contentViewport.layout.height * (centred ? .5f : .38f), lineCentre = centred ? rows[active].layout.height * .5f : 0;
                targetY = Mathf.Clamp(rows[active].layout.y + lineCentre - anchor, 0, scroll.verticalScroller.highValue);
                float y = MusicLibrarySession.State.reduceMotion || MusicLibrarySession.State.lyricMotion == 0 ? targetY : Mathf.Lerp(scroll.scrollOffset.y, targetY, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 10));
                if (!float.IsNaN(y)) scroll.scrollOffset = new Vector2(0, y);
            }
        }
    }
}
