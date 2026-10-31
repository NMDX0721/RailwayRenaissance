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
        MusicReactiveStageView reactiveStage;
        Button followButton;
        Slider progress;
        Label playTime;
        string trackId;
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
            var previousStage = reactiveStage;
            previousStage?.RemoveFromHierarchy();
            host.Clear();
            reactiveStage = previousStage ?? new MusicReactiveStageView(); host.Add(reactiveStage);
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
            Action(header, "关闭", close, "music-lyrics-close", true);
            var body = Box(host, "music-lyrics-body");
            var art = Box(body, "music-lyrics-art");
            var track = MusicLibrarySession.Find(TabletMusicAppUI.tabletCurrentTrackId);
            reactiveStage.SetTrack(track);
            var cover = Box(art, "music-lyrics-cover");
            cover.style.backgroundImage = new StyleBackground(MusicAlbumArtResolver.GetAlbumCover(track?.id, track?.category));
            if (track != null) MusicAlbumArtResolver.LoadRemoteCover(track.id, image => {
                if (cover.panel == null || reactiveStage.TrackId != track.id) return;
                cover.style.backgroundImage = new StyleBackground(image); reactiveStage.SetTrack(track, true);
            });
            Text(art, track?.title ?? "尚未选择曲目", "music-detail-title");
            if (track != null && !string.IsNullOrEmpty(track.titleTranslation)) Text(art, track.titleTranslation, "music-muted");
            Text(art, "沿线声音档案 / RAILWAY SOUND", "music-eyebrow");
            status = new StatusLines(); status.AddToClassList("music-lyrics-status"); art.Add(status);
            var imports = Box(art, "music-lyrics-actions");
            Action(imports, "导入字幕", () => Import(false), "music-lyrics-import");
            Action(imports, "导入翻译", () => Import(true), "music-lyrics-import-translation");
            Action(art, "重新读取", Reload, "music-lyrics-reload");
            Text(art, "显示", "music-lyrics-section");
            var font = new Slider("字号", 18, 36) { value = MusicLibrarySession.State.lyricFontSize, name = "music-lyrics-font" }; art.Add(font);
            font.AddToClassList("music-horizontal-slider");
            font.RegisterValueChangedCallback(e => { MusicLibrarySession.State.lyricFontSize = Mathf.RoundToInt(e.newValue); MusicLibrarySession.Save(); RenderRows(); });
            var translation = new Toggle("显示翻译") { value = MusicLibrarySession.State.lyricTranslation, name = "music-lyrics-translation" }; art.Add(translation);
            translation.RegisterValueChangedCallback(e => { MusicLibrarySession.State.lyricTranslation = e.newValue; MusicLibrarySession.Save(); RenderRows(); });
            var estimate = new Toggle("估算逐词") { value = MusicLibrarySession.State.estimateWords, name = "music-lyrics-estimate" }; art.Add(estimate);
            estimate.tooltip = "没有逐词时间的歌词，按行内字数估算逐词填充（仅用于显示）";
            estimate.RegisterValueChangedCallback(e => { MusicLibrarySession.State.estimateWords = e.newValue; MusicLibrarySession.Save(); RenderRows(); });
            var reduce = new Toggle("减少动态效果") { value = MusicLibrarySession.State.reduceMotion, name = "music-lyrics-reduce-motion" }; art.Add(reduce);
            reduce.RegisterValueChangedCallback(e => { MusicLibrarySession.State.reduceMotion = e.newValue; MusicLibrarySession.Save(); });
            Text(art, "时间校准", "music-lyrics-section");
            var timing = Box(art, "music-player-buttons");
            timing.name = "music-lyrics-timing";
            Action(timing, "延后 100ms", () => Adjust(-100), "music-lyrics-later", true);
            offsetLabel = Text(timing, "0 ms", "music-lyric-offset");
            Action(timing, "提前 100ms", () => Adjust(100), "music-lyrics-earlier", true);
            Action(art, "重置字幕时间", () => { var p = MusicTimedText.Preference(trackId); p.offsetMs = 0; MusicLibrarySession.Save(); active = -2; }, "music-lyrics-reset");
            var textArea = Box(body, "music-lyrics-text-area");
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
            help = Text(host, "", "music-lyrics-help");
            trackId = TabletMusicAppUI.tabletCurrentTrackId;
            ApplyMotionStyle();
            Reload();
        }
        static string MotionTitle() => new[] { "静读", "随行", "舞台" }[Mathf.Clamp(MusicLibrarySession.State.lyricMotion, 0, 2)];
        void ApplyMotionStyle()
        {
            host.EnableInClassList("music-motion-static", MusicLibrarySession.State.lyricMotion == 0);
            host.EnableInClassList("music-motion-follow", MusicLibrarySession.State.lyricMotion == 1);
            host.EnableInClassList("music-motion-stage", MusicLibrarySession.State.lyricMotion == 2);
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
                MusicLyricEstimate.StripCredits(fetched);
                if (fetched.lines.Count == 0) { status.text = track.category + "未提供可用的同步歌词。"; return; }
                fetched.source = track.category + "歌词";
                if (!string.IsNullOrEmpty(result.result.translation) && string.IsNullOrEmpty(MusicTimedText.Preference(requestedId).translationFileName))
                {
                    var translated = MusicTimedText.Parse(result.result.translation, ".lrc");
                    MusicLyricEstimate.StripCredits(translated);
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
            status.text = document.source + "\n" + document.lines.FindAll(l => !string.IsNullOrWhiteSpace(l.text)).Count + " 行 · " + (document.lines.Exists(l => l.words.Count > 0) ? "含逐词时间" : MusicLibrarySession.State.estimateWords ? "逐词（按行估算）" : "逐行同步");
            for (int i = 0; i < document.lines.Count; i++)
            {
                var line = document.lines[i]; int index = i;
                var row = new Button(() => {
                    var clip = TabletMusicAppUI.tabletCurrentClip;
                    if (clip == null || TabletMusicAppUI.tabletCurrentTrackId != trackId)
                    { TabletMusicAppUI.ShowToast("先播放当前曲目，再点击字幕定位"); return; }
                    var position = MusicTimedText.SeekPosition(line.start, MusicTimedText.Preference(trackId).offsetMs, clip.length);
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
            var preference = MusicTimedText.Preference(trackId);
            offsetLabel.text = (preference.offsetMs > 0 ? "+" : "") + preference.offsetMs + " ms";
            followButton.style.display = following || document.lines.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            heading.text = (TabletMusicAppUI.tabletCurrentTrackTitle ?? "歌词 / 字幕") + " / " + (TabletMusicAppUI.tabletIsPlaying ? "正在播放" : "已暂停");
            if (document.source != null && document.source.Contains("草稿")) heading.text += " · 待听校";
            heading.tooltip = document.source;
            var clip = TabletMusicAppUI.tabletCurrentClip;
            playTime.text = TabletMusicAppUI.FormatAudioTime(TabletMusicAppUI.CurrentPosition) + " / " + (clip == null ? "--:--" : TabletMusicAppUI.FormatAudioTime(clip.length));
            progress.SetEnabled(clip != null);
            if (!scrubbing) progress.SetValueWithoutNotify(clip != null && clip.length > 0 ? TabletMusicAppUI.CurrentPosition / clip.length : 0);
            RailwayMusicTerminalView.UpdateSliderFill(progress, progress.value);
            MusicTerminalIcons.Apply(host.Q<Button>("music-lyrics-play"), TabletMusicAppUI.tabletIsPlaying ? "暂停" : "播放", true);
            double clock = TabletMusicAppUI.CurrentPosition + preference.offsetMs / 1000d;
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
            reactiveStage.Tick(MusicAudioEffectsEngine.GetActiveDeck(), TabletMusicAppUI.tabletIsPlaying,
                MusicLibrarySession.State.reduceMotion || MusicLibrarySession.State.lyricMotion == 0);
            // Stage mode draws the current line on its own layer (Folia-style word pop); the list stays underneath, hidden.
            bool stageOn = MusicLibrarySession.State.lyricMotion == 2 && document.lines.Count > 0;
            stage.style.display = stageOn ? DisplayStyle.Flex : DisplayStyle.None;
            scroll.style.opacity = stageOn ? 0f : 1f; scroll.pickingMode = stageOn ? PickingMode.Ignore : PickingMode.Position;
            if (stageOn)
                stage.Tick(document, active, clock, Time.unscaledDeltaTime, MusicLibrarySession.State.reduceMotion, MusicLibrarySession.State.estimateWords,
                    Mathf.Clamp(MusicLibrarySession.State.lyricFontSize / 24f, .75f, 1.5f), reactiveStage.PrimaryColor, reactiveStage.VisualEnergy, MusicLibrarySession.State.lyricTranslation);
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
