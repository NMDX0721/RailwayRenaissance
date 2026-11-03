using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // "播放设置": the settings of the player in three levels instead of a row of small buttons and menus on the bar (after Folia's Controls page).
    //   level 1: the button on the player bar
    //   level 2: a list of categories, each showing its current value (speed, tone, sleep timer, lyric timing, what to pin to the bar)
    //   level 3: the choices of one category; picking one applies it and goes back to the list
    // Which of speed, tone and timer also keep a button on the bar is the player's own choice ("放到播放栏").
    public sealed partial class RailwayMusicTerminalView
    {
        Button speedChip, toneChip, timerChip;   // optional bar buttons
        VisualElement controlsPanel;
        string controlsPage;                     // null: the category list; otherwise the category being shown

        static readonly string[] ToneNames = { "纯净原声", "工务电台", "先民真空电台", "电子管调频立体声" };
        static readonly string[] ToneNotes = { "不做处理", "窄频、沙风、磁带抖晃", "温暖电子管、底噪", "宽频温润" };

        void OpenControlsPanel(VisualElement anchor)
        {
            controlsPage = null;
            var menu = MenuAt(anchor, null); menu.AddToClassList("music-controls-panel");
            controlsPanel = menu;
            FillControlsPanel();
        }

        void ShowControlsPage(string page) { controlsPage = page; FillControlsPanel(); }

        void RefreshControlsPanel()
        {
            if (controlsPanel == null || controlsPanel.panel == null) { controlsPanel = null; return; }
            FillControlsPanel(); UpdateBarChips();
        }

        static string SpeedText() => MusicAudioEffectsEngine.PlaybackSpeed.ToString("0.##") + "x";
        static string TimerText() => MusicAudioEffectsEngine.IsSleepTimerActive ? MusicAudioEffectsEngine.FormatSleepTimeRemaining() : "关";
        static string TimingText(string id) { int ms = MusicTimedText.Preference(id).offsetMs; return (ms > 0 ? "+" : "") + ms + " ms"; }

        // A row of the category list: the name on the left, the current value and an arrow on the right.
        Button CategoryRow(VisualElement parent, string key, string title, string value)
        {
            var row = new Button(() => ShowControlsPage(key)) { name = "music-setting-" + key, tooltip = title }; row.AddToClassList("music-setting-row"); parent.Add(row);
            var name = new Label(title) { pickingMode = PickingMode.Ignore, enableRichText = false }; name.AddToClassList("music-setting-title"); row.Add(name);
            var current = new Label(value) { pickingMode = PickingMode.Ignore, enableRichText = false }; current.AddToClassList("music-setting-value"); row.Add(current);
            var arrow = new Label("›") { pickingMode = PickingMode.Ignore }; arrow.AddToClassList("music-setting-arrow"); row.Add(arrow);
            return row;
        }

        // A switch in the category list: the state is in the row, and one tap changes it in place.
        Button ToggleRow(VisualElement parent, string key, string title, bool on, Action toggle)
        {
            var row = new Button(() => { toggle(); MusicLibrarySession.Save(); RefreshControlsPanel(); }) { name = "music-toggle-" + key, tooltip = title }; row.AddToClassList("music-setting-row"); parent.Add(row);
            var name = new Label(title) { pickingMode = PickingMode.Ignore, enableRichText = false }; name.AddToClassList("music-setting-title"); row.Add(name);
            var state = new Label(on ? "开" : "关") { pickingMode = PickingMode.Ignore }; state.AddToClassList("music-setting-switch"); state.EnableInClassList("is-on", on); row.Add(state);
            return row;
        }

        // A choice on a level-3 page: a check mark for the current one.
        Button ChoiceRow(VisualElement parent, string key, int index, string label, string note, bool current, Action pick, bool backAfter)
        {
            var row = new Button(() => { pick(); if (backAfter) controlsPage = null; RefreshControlsPanel(); }) { name = "music-opt-" + key + "-" + index, tooltip = label };
            row.AddToClassList("music-setting-row"); row.EnableInClassList("is-current", current); parent.Add(row);
            var text = new VisualElement { pickingMode = PickingMode.Ignore }; text.AddToClassList("music-setting-text"); row.Add(text);
            var title = new Label(label) { pickingMode = PickingMode.Ignore, enableRichText = false }; title.AddToClassList("music-setting-title"); text.Add(title);
            if (!string.IsNullOrEmpty(note)) { var sub = new Label(note) { pickingMode = PickingMode.Ignore, enableRichText = false }; sub.AddToClassList("music-setting-note"); text.Add(sub); }
            var check = new Label(current ? "✓" : "") { pickingMode = PickingMode.Ignore }; check.AddToClassList("music-setting-check"); row.Add(check);
            return row;
        }

        void BackBar(VisualElement parent, string title)
        {
            var bar = Box(parent, "music-setting-back");
            var back = new Button(() => { controlsPage = null; RefreshControlsPanel(); }) { name = "music-setting-back", tooltip = "返回" }; back.AddToClassList("music-setting-backbutton"); bar.Add(back);
            back.Add(new Label("‹") { pickingMode = PickingMode.Ignore });
            Text(bar, title, "music-setting-pagetitle");
        }

        void FillControlsPanel()
        {
            var menu = controlsPanel; menu.Clear();
            var state = MusicLibrarySession.State;
            string id = TabletMusicAppUI.tabletCurrentTrackId;
            bool hasTrack = MusicLibrarySession.Find(id) != null;

            if (controlsPage == null)
            {
                Text(menu, "播放设置", "music-popover-title");
                CategoryRow(menu, "speed", "倍速", SpeedText());
                CategoryRow(menu, "tone", "收听音色", ToneNames[Mathf.Clamp((int)MusicAudioEffectsEngine.CurrentFilterMode, 0, 3)]);
                CategoryRow(menu, "timer", "睡眠定时", TimerText());
                if (hasTrack) CategoryRow(menu, "timing", "歌词时间校准", TimingText(id));
                string pinned = string.Join(" ", new[] { state.barSpeed ? "倍速" : null, state.barTone ? "音色" : null, state.barTimer ? "定时" : null }.Where(x => x != null));
                CategoryRow(menu, "pins", "放到播放栏", pinned.Length > 0 ? pinned : "无");
                ToggleRow(menu, "crossfade", "智能过渡 · 换歌淡入淡出", !state.noCrossfade, () => { state.noCrossfade = !state.noCrossfade; MusicAudioEffectsEngine.CrossfadeEnabled = !state.noCrossfade; });
                ToggleRow(menu, "cover", "封面取色 · 强调色跟着封面", !state.noCoverTheme, () => { state.noCoverTheme = !state.noCoverTheme; themeId = "<reload>"; });
                var links = Box(menu, "music-option-row"); links.AddToClassList("music-option-links");
                Chip(links, "电台", () => { CloseModal(); TabletMusicAppUI.StartRadioAuto(); }, "music-radio", "先民时代电台：顺序全自动轮播").AddToClassList("music-option-link");
                Chip(links, "唱片档案", () => { CloseModal(); ShowDetail(TabletMusicAppUI.tabletCurrentTrackId); }, "music-option-detail").AddToClassList("music-option-link");
                Chip(links, "歌词 / 字幕", () => { CloseModal(); OpenLyrics(); }, "music-option-lyrics").AddToClassList("music-option-link");
                return;
            }

            switch (controlsPage)
            {
                case "speed":
                {
                    BackBar(menu, "倍速 · 音高随速度变化");
                    int index = 0;
                    foreach (float option in new[] { 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f })
                    {
                        float chosen = option;
                        ChoiceRow(menu, "speed", index++, chosen.ToString("0.##") + "x", chosen == 1f ? "正常" : null, Mathf.Abs(MusicAudioEffectsEngine.PlaybackSpeed - chosen) < .01f,
                            () => { MusicAudioEffectsEngine.SetPlaybackSpeed(chosen); state.playbackSpeed = chosen; MusicLibrarySession.Save(); }, true);
                    }
                    break;
                }
                case "tone":
                {
                    BackBar(menu, "收听音色");
                    for (int i = 0; i < ToneNames.Length; i++)
                    {
                        int mode = i;
                        ChoiceRow(menu, "tone", i, ToneNames[i], ToneNotes[i], (int)MusicAudioEffectsEngine.CurrentFilterMode == mode,
                            () => { MusicAudioEffectsEngine.CurrentFilterMode = (MusicAudioEffectsEngine.RadioFilterMode)mode; state.toneMode = mode; MusicLibrarySession.Save(); }, true);
                    }
                    break;
                }
                case "timer":
                {
                    BackBar(menu, MusicAudioEffectsEngine.IsSleepTimerActive ? "睡眠定时 · 还剩 " + MusicAudioEffectsEngine.FormatSleepTimeRemaining() : "睡眠定时");
                    int index = 0;
                    foreach (int minutes in new[] { 0, 15, 30, 45, 999 })
                    {
                        int chosen = minutes;
                        bool active = chosen == 0 ? !MusicAudioEffectsEngine.IsSleepTimerActive : MusicAudioEffectsEngine.IsSleepTimerActive && (int)MusicAudioEffectsEngine.CurrentSleepOption == chosen;
                        ChoiceRow(menu, "timer", index++, chosen == 0 ? "关闭定时" : chosen == 999 ? "播完本曲后暂停" : chosen + " 分钟后暂停", null, active,
                            () => MusicAudioEffectsEngine.SetSleepTimer((MusicAudioEffectsEngine.SleepTimerOption)chosen,
                                TabletMusicAppUI.tabletCurrentClip == null ? 0 : TabletMusicAppUI.tabletCurrentClip.length - TabletMusicAppUI.CurrentPosition), true);
                    }
                    break;
                }
                case "timing":
                {
                    BackBar(menu, "歌词时间校准");
                    if (!hasTrack) break;
                    var pref = MusicTimedText.Preference(id);
                    var row = Box(menu, "music-option-row"); row.AddToClassList("music-timing-row");
                    void Shift(int delta) { pref.offsetMs = Mathf.Clamp(pref.offsetMs + delta, -10000, 10000); MusicLibrarySession.Save(); RefreshControlsPanel(); }
                    Chip(row, "延后", () => Shift(-100), "music-option-later", "延后 100ms");
                    Text(row, TimingText(id), "music-option-value");
                    Chip(row, "提前", () => Shift(100), "music-option-earlier", "提前 100ms");
                    Chip(row, "重置", () => { pref.offsetMs = 0; MusicLibrarySession.Save(); RefreshControlsPanel(); }, "music-option-reset");
                    Text(menu, "歌词比声音晚就点“提前”，早就点“延后”，每次 100 毫秒；每首歌单独记住。", "music-setting-hint");
                    break;
                }
                case "pins":
                {
                    BackBar(menu, "放到播放栏");
                    ChoiceRow(menu, "pin", 0, "倍速", "在播放栏上显示当前倍速", state.barSpeed, () => { state.barSpeed = !state.barSpeed; MusicLibrarySession.Save(); }, false);
                    ChoiceRow(menu, "pin", 1, "音色", "在播放栏上加音色按钮", state.barTone, () => { state.barTone = !state.barTone; MusicLibrarySession.Save(); }, false);
                    ChoiceRow(menu, "pin", 2, "定时", "在播放栏上加定时按钮", state.barTimer, () => { state.barTimer = !state.barTimer; MusicLibrarySession.Save(); }, false);
                    break;
                }
            }
        }

        // A chip is a plain text button: no icon, so the text can use the whole width.
        static Button Chip(VisualElement parent, string text, Action action, string name, string tip = null)
        {
            var button = new Button(action) { text = text, tooltip = tip ?? text, name = name }; button.AddToClassList("music-option-chip"); parent.Add(button); return button;
        }

        // The bar's own buttons for speed, tone and timer exist only when pinned; they open the panel.
        void UpdateBarChips()
        {
            var state = MusicLibrarySession.State;
            if (speedChip != null)
            {
                speedChip.style.display = state.barSpeed ? DisplayStyle.Flex : DisplayStyle.None;
                speedChip.text = SpeedText();
                speedChip.EnableInClassList("is-active", Mathf.Abs(MusicAudioEffectsEngine.PlaybackSpeed - 1f) > .01f);
            }
            if (toneChip != null) toneChip.style.display = state.barTone ? DisplayStyle.Flex : DisplayStyle.None;
            if (timerChip != null) timerChip.style.display = state.barTimer ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
