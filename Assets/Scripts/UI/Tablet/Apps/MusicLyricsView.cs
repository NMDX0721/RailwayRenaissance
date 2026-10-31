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
        ScrollView scroll;
        Label heading, status, offsetLabel, help;
        VisualElement stageProgress;
        MusicReactiveStageView reactiveStage;
        Button followButton;
        Slider progress;
        Label playTime;
        string trackId;
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
                host.Q<Button>("music-lyrics-motion").tooltip = "切换歌词表现：静读 / 随行 / 舞台；减少动态效果优先";
            }, "music-lyrics-motion");
            motion.tooltip = "切换歌词表现：静读 / 随行 / 舞台；减少动态效果优先";
            Action(header, "关闭", close, "music-lyrics-close", true);
            var body = Box(host, "music-lyrics-body");
            var art = Box(body, "music-lyrics-art");
            var track = MusicLibrarySession.Find(TabletMusicAppUI.tabletCurrentTrackId);
            reactiveStage.SetTrack(track);
            var cover = Box(art, "music-lyrics-cover");
            cover.style.backgroundImage = new StyleBackground(MusicAlbumArtResolver.GetAlbumCover(track?.id, track?.category));
            Text(art, track?.title ?? "尚未选择曲目", "music-detail-title");
            Text(art, "沿线声音档案 / RAILWAY SOUND", "music-eyebrow");
            status = Text(art, "", "music-wrap");
            Action(art, "导入字幕", () => Import(false), "music-lyrics-import");
            Action(art, "导入翻译", () => Import(true), "music-lyrics-import-translation");
            Action(art, "重新读取", Reload, "music-lyrics-reload");
            var font = new Slider("字号", 18, 36) { value = MusicLibrarySession.State.lyricFontSize, name = "music-lyrics-font" }; art.Add(font);
            font.AddToClassList("music-horizontal-slider");
            font.RegisterValueChangedCallback(e => { MusicLibrarySession.State.lyricFontSize = Mathf.RoundToInt(e.newValue); MusicLibrarySession.Save(); RenderRows(); });
            var translation = new Toggle("显示翻译") { value = MusicLibrarySession.State.lyricTranslation, name = "music-lyrics-translation" }; art.Add(translation);
            translation.RegisterValueChangedCallback(e => { MusicLibrarySession.State.lyricTranslation = e.newValue; MusicLibrarySession.Save(); RenderRows(); });
            var reduce = new Toggle("减少动态效果") { value = MusicLibrarySession.State.reduceMotion, name = "music-lyrics-reduce-motion" }; art.Add(reduce);
            reduce.RegisterValueChangedCallback(e => { MusicLibrarySession.State.reduceMotion = e.newValue; MusicLibrarySession.Save(); });
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
            document = MusicTimedText.Load(MusicLibrarySession.Find(trackId));
            following = true; RenderRows();
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
                Text(scroll, document.error ?? "暂无字幕", "music-lyric-empty");
                status.text = "暂无字幕。可导入歌词文件，或继续收听音乐。"; return;
            }
            status.text = document.source + "\n" + document.lines.FindAll(l => !string.IsNullOrWhiteSpace(l.text)).Count + " 行 · " + (document.lines.Exists(l => l.words.Count > 0) ? "含逐词时间" : "逐行同步");
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
                if (line.words.Count == 0)
                {
                    var label = Text(row, string.IsNullOrEmpty(line.text) ? "· · ·" : line.text, "music-lyric-text");
                    label.style.fontSize = string.IsNullOrWhiteSpace(line.text) ? 16 : MusicLibrarySession.State.lyricFontSize;
                }
                else
                {
                    var flow = Box(row, "music-lyric-words");
                    foreach (var word in line.words)
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
                    rows[i].style.opacity = i == active ? 1 : Mathf.Abs(i - active) <= 1 ? .65f : .4f;
                    rows[i].style.backgroundColor = i == active && !string.IsNullOrWhiteSpace(document.lines[i].text)
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
                targetY = Mathf.Clamp(rows[active].layout.y - scroll.contentViewport.layout.height * .4f, 0, scroll.verticalScroller.highValue);
                float y = MusicLibrarySession.State.reduceMotion || MusicLibrarySession.State.lyricMotion == 0 ? targetY : Mathf.Lerp(scroll.scrollOffset.y, targetY, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 10));
                if (!float.IsNaN(y)) scroll.scrollOffset = new Vector2(0, y);
            }
        }
    }
}
