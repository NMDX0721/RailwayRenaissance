using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // 命令面板 (Ctrl+K or the button in the top bar): type to find an action or a song in the queue and run it with Enter, without hunting through menus.
    // After Folia's command palette, much smaller: one list, substring matching on the name and its keywords, and the ones used last come first.
    public sealed partial class RailwayMusicTerminalView
    {
        sealed class PaletteCommand
        {
            public string id, title, hint, words; public Action run;
        }

        VisualElement paletteList; TextField paletteInput; List<PaletteCommand> paletteShown = new List<PaletteCommand>(); int paletteIndex;

        List<PaletteCommand> PaletteCommands()
        {
            var state = MusicLibrarySession.State; var list = new List<PaletteCommand>();
            void Add(string id, string title, string hint, string words, Action run) => list.Add(new PaletteCommand { id = id, title = title, hint = hint, words = words, run = run });
            bool playing = TabletMusicAppUI.tabletIsPlaying;
            Add("play", playing ? "暂停" : "播放", "空格", "播放 暂停 play pause", TabletMusicAppUI.ToggleTabletMusicPlayPause);
            Add("next", "下一首", null, "下一首 切歌 next", TabletMusicAppUI.PlayNextTabletTrack);
            Add("prev", "上一首", null, "上一首 prev", TabletMusicAppUI.PlayPrevTabletTrack);
            Add("mode", "切换播放顺序", "顺序 / 随机 / 单曲", "播放顺序 随机 单曲 顺序 shuffle repeat", () => TabletMusicAppUI.SetPlayMode(((int)TabletMusicAppUI.tabletPlayMode + 1) % 3));
            Add("locate", "在列表中找到正在播放的歌", null, "定位 找到 当前 locate", LocateCurrent);
            Add("lyrics", "打开歌词页", "L", "歌词 字幕 舞台 lyrics stage", OpenLyrics);
            Add("ruler", "歌词时间标尺", "拖动校准歌词快慢", "歌词 时间 偏移 校准 标尺 延迟 offset sync", () => OpenOffsetRuler());
            Add("record","打开唱片档案", null, "唱片 档案 详情 record", () => ShowDetail(TabletMusicAppUI.tabletCurrentTrackId));
            Add("radio", "先民时代电台", "顺序全自动轮播", "电台 radio", TabletMusicAppUI.StartRadioAuto);
            foreach (string target in new[] { "首页", "榜单", "原声", "本地音乐", "收藏", "云端" })
            { string t = target; Add("go-" + t, "前往：" + t, null, "前往 打开 导航 " + t, () => Navigate(t)); }
            Add("search", "搜索", "Ctrl+F", "搜索 查找 search", () => search.Focus());
            Add("nav", "收起 / 展开侧栏", null, "侧栏 导航栏 菜单", ToggleNavigation);
            Add("settings", "音乐设置", null, "设置 选项 settings", () => OpenSettings());
            Add("eq", "均衡器", MusicEqualizer.SummaryText(), "均衡器 音效 eq equalizer 低音", () => OpenSettings("sound"));
            Add("eq-toggle", MusicEqualizer.Enabled ? "关闭均衡器" : "打开均衡器", null, "均衡器 音效 eq", () => { MusicEqualizer.Enabled = !MusicEqualizer.Enabled; MusicEqualizer.Store(state); MusicAudioEffectsEngine.RefreshEq(); UpdateBarChips(); MusicLibrarySession.Save(); ShowToast(MusicEqualizer.Enabled ? "均衡器已打开" : "均衡器已关闭"); });
            Add("gain-toggle", MusicEqualizer.ReplayGain ? "关闭统一响度" : "打开统一响度", null, "统一响度 回放增益 响度 replaygain", () => { MusicEqualizer.ReplayGain = !MusicEqualizer.ReplayGain; MusicEqualizer.Store(state); MusicAudioEffectsEngine.RefreshEq(); UpdateBarChips(); MusicLibrarySession.Save(); ShowToast(MusicEqualizer.ReplayGain ? "统一响度已打开" : "统一响度已关闭"); });
            Add("crossfade-toggle", state.noCrossfade ? "打开智能过渡" : "关闭智能过渡", null, "过渡 淡入淡出 crossfade", () => { state.noCrossfade = !state.noCrossfade; MusicAudioEffectsEngine.CrossfadeEnabled = !state.noCrossfade; MusicLibrarySession.Save(); ShowToast(state.noCrossfade ? "智能过渡已关闭" : "智能过渡已打开"); });
            Add("cover-toggle", state.noCoverTheme ? "打开封面取色" : "关闭封面取色", null, "封面 取色 主题色 强调色", () => { state.noCoverTheme = !state.noCoverTheme; themeId = "<reload>"; MusicLibrarySession.Save(); });
            Add("controls", "播放设置：倍速、音色、定时", null, "倍速 音色 定时 速度 speed tone timer", () => OpenControlsPanel(null));
            foreach (int minutes in new[] { 15, 30, 45 })
            { int m = minutes; Add("sleep-" + m, "睡眠定时 " + m + " 分钟", null, "睡眠 定时 sleep", () => { MusicAudioEffectsEngine.SetSleepTimer((MusicAudioEffectsEngine.SleepTimerOption)m); ShowToast("将在 " + m + " 分钟后暂停"); }); }
            if (MusicAudioEffectsEngine.IsSleepTimerActive) Add("sleep-off", "关闭睡眠定时", null, "睡眠 定时 取消", () => { MusicAudioEffectsEngine.SetSleepTimer(MusicAudioEffectsEngine.SleepTimerOption.Off); ShowToast("睡眠定时已关闭"); });
            for (int i = 0; i < StageStyles.Count; i++)
            { string style = StageStyles.IdAt(i); Add("stage-" + style, "舞台风格：" + StageStyles.TitleOf(style), style == state.stageStyle ? "当前" : null, "舞台 风格 样式 stage " + style, () => { state.stageStyle = style; MusicLibrarySession.Save(); OpenLyrics(); }); }
            Add("tour", "重新观看新手教学", null, "教学 帮助 引导 tour help", StartTutorial);
            Add("help", "操作帮助", null, "帮助 快捷键 help", ShowHelp);
            return list;
        }

        void OpenPalette(string initial = "")
        {
            var menu = MenuAt(null, null); menu.AddToClassList("music-palette");
            paletteInput = new TextField { name = "music-palette-input" }; paletteInput.AddToClassList("music-palette-input"); menu.Add(paletteInput);
            var scroll = new ScrollView { name = "music-palette-scroll", verticalScrollerVisibility = ScrollerVisibility.Auto }; scroll.AddToClassList("music-palette-scroll"); menu.Add(scroll);
            paletteList = scroll.contentContainer;
            var foot = new Label("↑ ↓ 选择 · Enter 执行 · Esc 关闭") { pickingMode = PickingMode.Ignore }; foot.AddToClassList("music-palette-foot"); menu.Add(foot);
            paletteInput.SetValueWithoutNotify(initial);
            paletteInput.RegisterValueChangedCallback(e => { if (e.target != paletteInput) return; paletteIndex = 0; FillPalette(); });
            menu.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.DownArrow) { MovePalette(1); e.StopImmediatePropagation(); }
                else if (e.keyCode == KeyCode.UpArrow) { MovePalette(-1); e.StopImmediatePropagation(); }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { RunPalette(paletteIndex); e.StopImmediatePropagation(); }
                else if (e.keyCode == KeyCode.Escape) { CloseModal(); e.StopImmediatePropagation(); }
            }, TrickleDown.TrickleDown);
            paletteIndex = 0; FillPalette();
            paletteInput.schedule.Execute(() => paletteInput.Focus());
        }

        static string PaletteKey(string text) => (text ?? "").ToLowerInvariant();

        void FillPalette()
        {
            if (paletteList == null) return;
            string query = PaletteKey(paletteInput.value).Trim();
            var all = PaletteCommands(); var recent = (MusicLibrarySession.State.paletteRecent ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            IEnumerable<PaletteCommand> found;
            if (query.Length == 0) found = recent.Select(id => all.FirstOrDefault(c => c.id == id)).Where(c => c != null).Concat(all.Where(c => !recent.Contains(c.id)));
            else
            {
                var terms = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                found = all.Where(c => terms.All(t => PaletteKey(c.title).Contains(t) || PaletteKey(c.words).Contains(t)))
                    .OrderBy(c => terms.Any(t => PaletteKey(c.title).StartsWith(t)) ? 0 : PaletteKey(c.title).Contains(terms[0]) ? 1 : 2)
                    .ThenBy(c => { int at = recent.IndexOf(c.id); return at < 0 ? 99 : at; });
            }
            paletteShown = found.Take(40).ToList();
            // Songs in the queue, when the text is long enough to mean a song.
            if (query.Length >= 2)
                foreach (string id in MusicLibrarySession.State.queue.Take(200))
                {
                    var track = MusicLibrarySession.Find(id);
                    if (track == null || !(PaletteKey(track.title).Contains(query) || PaletteKey(track.artist).Contains(query))) continue;
                    string playId = track.id;
                    paletteShown.Add(new PaletteCommand { id = "song-" + playId, title = "队列：" + track.title, hint = track.artist, run = () => MusicLibrarySession.Play(playId) });
                    if (paletteShown.Count >= 48) break;
                }
            paletteIndex = Mathf.Clamp(paletteIndex, 0, Mathf.Max(0, paletteShown.Count - 1));
            paletteList.Clear();
            if (paletteShown.Count == 0)
            { var empty = new Label("没有匹配的命令") { pickingMode = PickingMode.Ignore }; empty.AddToClassList("music-palette-empty"); paletteList.Add(empty); return; }
            for (int i = 0; i < paletteShown.Count; i++)
            {
                int index = i; var c = paletteShown[i];
                var row = new Button(() => RunPalette(index)) { name = "music-palette-row-" + i, tooltip = c.title }; row.AddToClassList("music-palette-row"); row.EnableInClassList("is-selected", i == paletteIndex); paletteList.Add(row);
                var title = new Label(c.title) { pickingMode = PickingMode.Ignore, enableRichText = false }; title.AddToClassList("music-palette-title"); row.Add(title);
                if (!string.IsNullOrEmpty(c.hint)) { var hint = new Label(c.hint) { pickingMode = PickingMode.Ignore, enableRichText = false }; hint.AddToClassList("music-palette-hint"); row.Add(hint); }
            }
        }

        void MovePalette(int delta)
        {
            if (paletteShown.Count == 0) return;
            paletteIndex = (paletteIndex + delta + paletteShown.Count) % paletteShown.Count;
            for (int i = 0; i < paletteList.childCount; i++) paletteList[i].EnableInClassList("is-selected", i == paletteIndex);
            (paletteList.parent.parent as ScrollView)?.ScrollTo(paletteList[paletteIndex]);
        }

        void RunPalette(int index)
        {
            if (index < 0 || index >= paletteShown.Count) return;
            var command = paletteShown[index];
            if (!command.id.StartsWith("song-"))
            {
                var recent = (MusicLibrarySession.State.paletteRecent ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                recent.Remove(command.id); recent.Insert(0, command.id);
                MusicLibrarySession.State.paletteRecent = string.Join(",", recent.Take(8)); MusicLibrarySession.Save();
            }
            CloseModal(); paletteList = null; paletteInput = null;
            command.run?.Invoke();
        }
    }
}
