using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // "音乐设置": the settings page of the music app. The small playback panel on the bar keeps the quick things (speed, tone, timer);
    // everything else lives here in groups: sound (equalizer, loudness, transition), look (accent, base colour, motion), lyrics, the player bar, and help.
    public sealed partial class RailwayMusicTerminalView
    {
        VisualElement settingsContent, settingsNav;
        string settingsTab = "sound";

        static readonly string[] SettingsTabs = { "sound", "look", "lyrics", "bar", "help" };
        static readonly string[] SettingsTabNames = { "声音", "外观", "歌词", "播放栏", "帮助" };

        void OpenSettings(string tab = null)
        {
            if (tab != null && Array.IndexOf(SettingsTabs, tab) >= 0) settingsTab = tab;
            var panel = Modal("音乐设置"); panel.AddToClassList("music-settings-sheet");
            var body = Box(panel, "music-settings-body");
            settingsNav = Box(body, "music-settings-nav");
            var scroll = new ScrollView { verticalScrollerVisibility = ScrollerVisibility.Auto, name = "music-settings-scroll" }; scroll.AddToClassList("music-settings-scroll"); body.Add(scroll);
            settingsContent = scroll.contentContainer;
            FillSettings();
        }

        void FillSettings()
        {
            if (settingsContent == null || settingsContent.panel == null) { settingsContent = null; return; }
            settingsNav.Clear();
            for (int i = 0; i < SettingsTabs.Length; i++)
            {
                string key = SettingsTabs[i];
                var tab = new Button(() => { settingsTab = key; FillSettings(); }) { text = SettingsTabNames[i], name = "music-settings-tab-" + key };
                tab.AddToClassList("music-settings-tab"); tab.EnableInClassList("is-active", settingsTab == key); settingsNav.Add(tab);
            }
            settingsContent.Clear();
            switch (settingsTab)
            {
                case "sound": FillSoundSettings(); break;
                case "look": FillLookSettings(); break;
                case "lyrics": FillLyricSettings(); break;
                case "bar": FillBarSettings(); break;
                default: FillHelpSettings(); break;
            }
            ApplyAccent();
        }

        // Rebuild the page but keep where the user had scrolled to.
        void RefreshSettings()
        {
            var scroll = settingsContent?.parent?.parent as ScrollView; float at = scroll != null ? scroll.scrollOffset.y : 0;
            FillSettings();
            if (scroll != null) scroll.schedule.Execute(() => scroll.scrollOffset = new Vector2(0, at));
        }

        // ---- building blocks ----
        VisualElement Group(string heading)
        {
            Text(settingsContent, heading, "music-settings-heading");
            return Box(settingsContent, "music-settings-group");
        }

        // A row with a title and a note on the left and an on/off pill on the right.
        void SwitchRow(VisualElement group, string key, string title, string note, bool on, Action toggle)
        {
            var row = new Button(() => { toggle(); MusicLibrarySession.Save(); RefreshSettings(); }) { name = "music-set-" + key, tooltip = title };
            row.AddToClassList("music-setting-row"); row.AddToClassList("music-settings-row"); group.Add(row);
            var text = new VisualElement { pickingMode = PickingMode.Ignore }; text.AddToClassList("music-setting-text"); row.Add(text);
            var name = new Label(title) { pickingMode = PickingMode.Ignore, enableRichText = false }; name.AddToClassList("music-setting-title"); text.Add(name);
            if (!string.IsNullOrEmpty(note)) { var sub = new Label(note) { pickingMode = PickingMode.Ignore, enableRichText = false }; sub.AddToClassList("music-setting-note"); text.Add(sub); }
            var state = new Label(on ? "开" : "关") { pickingMode = PickingMode.Ignore }; state.AddToClassList("music-setting-switch"); state.EnableInClassList("is-on", on); row.Add(state);
        }

        // A row with a title and a note, and below it a group of chips to choose from.
        void ChipsRow(VisualElement group, string key, string title, string note, IList<string> options, int current, Action<int> pick)
        {
            var row = Box(group, "music-settings-row"); row.AddToClassList("music-settings-chiprow");
            Text(row, title, "music-setting-title").style.flexGrow = 0;
            if (!string.IsNullOrEmpty(note)) Text(row, note, "music-setting-note");
            var chips = Box(row, "music-settings-chips");
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var chip = Chip(chips, options[i], () => { pick(index); MusicLibrarySession.Save(); RefreshSettings(); }, "music-set-" + key + "-" + i);
                chip.AddToClassList("music-settings-chip"); chip.EnableInClassList("is-active", i == current);
            }
        }

        // ---- sound ----
        void FillSoundSettings()
        {
            var state = MusicLibrarySession.State;
            var eq = Group("均衡器");
            SwitchRow(eq, "eq", "启用均衡器", "十个频段，每段 ±12 dB；拖动推子会自动打开", MusicEqualizer.Enabled, () => { MusicEqualizer.Enabled = !MusicEqualizer.Enabled; ApplyEq(); });
            var presets = Box(eq, "music-settings-chips"); presets.AddToClassList("music-eq-presets");
            foreach (string preset in MusicEqualizer.PresetNames)
            {
                string chosenPreset = preset;
                var chip = Chip(presets, chosenPreset, () => { MusicEqualizer.ChoosePreset(chosenPreset); MusicEqualizer.Enabled = true; ApplyEq(); RefreshSettings(); }, "music-eq-preset-" + Array.IndexOf(MusicEqualizer.PresetNames, chosenPreset));
                chip.AddToClassList("music-settings-chip"); chip.EnableInClassList("is-active", MusicEqualizer.Enabled && MusicEqualizer.Preset == chosenPreset);
            }
            if (MusicEqualizer.Preset == MusicEqualizer.CustomName)
            { var custom = Chip(presets, MusicEqualizer.CustomName, () => { }, "music-eq-preset-custom"); custom.AddToClassList("music-settings-chip"); custom.EnableInClassList("is-active", MusicEqualizer.Enabled); }
            var faders = Box(eq, "music-eq-faders"); faders.EnableInClassList("is-off", !MusicEqualizer.Enabled);
            for (int band = 0; band < MusicEqualizer.BandCount; band++) Fader(faders, band);

            var loud = Group("响度");
            SwitchRow(loud, "gain", "统一响度", "按每首歌的响度自动补偿，避免换歌时忽大忽小", MusicEqualizer.ReplayGain, () => { MusicEqualizer.ReplayGain = !MusicEqualizer.ReplayGain; ApplyEq(); });

            var play = Group("播放");
            SwitchRow(play, "crossfade", "智能过渡", "换歌时淡入淡出", !state.noCrossfade, () => { state.noCrossfade = !state.noCrossfade; MusicAudioEffectsEngine.CrossfadeEnabled = !state.noCrossfade; });
            float[] speeds = { .5f, .75f, 1f, 1.25f, 1.5f, 2f };
            ChipsRow(play, "speed", "倍速", "音高随速度变化", speeds.Select(x => x.ToString("0.##") + "x").ToList(), Array.FindIndex(speeds, x => Mathf.Abs(x - MusicAudioEffectsEngine.PlaybackSpeed) < .01f),
                i => { MusicAudioEffectsEngine.SetPlaybackSpeed(speeds[i]); state.playbackSpeed = speeds[i]; UpdateBarChips(); });
            ChipsRow(play, "tone", "收听音色", ToneNotes[Mathf.Clamp((int)MusicAudioEffectsEngine.CurrentFilterMode, 0, 3)], ToneNames, Mathf.Clamp((int)MusicAudioEffectsEngine.CurrentFilterMode, 0, 3),
                i => { MusicAudioEffectsEngine.CurrentFilterMode = (MusicAudioEffectsEngine.RadioFilterMode)i; state.toneMode = i; });
            int[] minutes = { 0, 15, 30, 45, 999 };
            ChipsRow(play, "timer", "睡眠定时", MusicAudioEffectsEngine.IsSleepTimerActive ? "还剩 " + MusicAudioEffectsEngine.FormatSleepTimeRemaining() : null, new[] { "关", "15 分钟", "30 分钟", "45 分钟", "播完本曲" },
                MusicAudioEffectsEngine.IsSleepTimerActive ? Array.IndexOf(minutes, (int)MusicAudioEffectsEngine.CurrentSleepOption) : 0,
                i => MusicAudioEffectsEngine.SetSleepTimer((MusicAudioEffectsEngine.SleepTimerOption)minutes[i], TabletMusicAppUI.tabletCurrentClip == null ? 0 : TabletMusicAppUI.tabletCurrentClip.length - TabletMusicAppUI.CurrentPosition));
        }

        void ApplyEq()
        {
            MusicEqualizer.Store(MusicLibrarySession.State); MusicAudioEffectsEngine.RefreshEq(); UpdateBarChips();
        }

        // One vertical fader: drag or click anywhere on the rail; the value is in dB, in half steps.
        void Fader(VisualElement parent, int band)
        {
            var column = Box(parent, "music-eq-band");
            var value = Text(column, "", "music-eq-value");
            var rail = Box(column, "music-eq-rail"); rail.name = "music-eq-band-" + band;
            var middle = Box(rail, "music-eq-middle"); middle.pickingMode = PickingMode.Ignore;
            var fill = Box(rail, "music-eq-fill"); fill.pickingMode = PickingMode.Ignore;
            var knob = Box(rail, "music-eq-knob"); knob.pickingMode = PickingMode.Ignore;
            Text(column, MusicEqualizer.BandLabels[band], "music-eq-freq");
            void Show()
            {
                float db = MusicEqualizer.Gains[band], percent = (12f - db) / 24f * 100f;
                value.text = (db > 0 ? "+" : "") + db.ToString("0.#");
                knob.style.top = Length.Percent(percent);
                fill.style.top = Length.Percent(Mathf.Min(percent, 50f)); fill.style.height = Length.Percent(Mathf.Abs(percent - 50f));
            }
            void SetFrom(float y)
            {
                float height = rail.resolvedStyle.height; if (height <= 1f) return;
                MusicEqualizer.SetBand(band, 12f - Mathf.Clamp01(y / height) * 24f); MusicEqualizer.Enabled = true;
                MusicAudioEffectsEngine.RefreshEq(); Show();
            }
            rail.RegisterCallback<PointerDownEvent>(e => { rail.CapturePointer(e.pointerId); SetFrom(e.localPosition.y); e.StopPropagation(); });
            rail.RegisterCallback<PointerMoveEvent>(e => { if (rail.HasPointerCapture(e.pointerId)) SetFrom(e.localPosition.y); });
            rail.RegisterCallback<PointerUpEvent>(e => { if (!rail.HasPointerCapture(e.pointerId)) return; rail.ReleasePointer(e.pointerId); ApplyEq(); MusicLibrarySession.Save(); RefreshSettings(); });
            rail.RegisterCallback<GeometryChangedEvent>(_ => Show());
            rail.focusable = true;
            rail.RegisterCallback<KeyDownEvent>(e =>
            {
                float step = e.keyCode == KeyCode.UpArrow ? .5f : e.keyCode == KeyCode.DownArrow ? -.5f : 0f;
                if (step == 0f) return;
                MusicEqualizer.SetBand(band, MusicEqualizer.Gains[band] + step); MusicEqualizer.Enabled = true; ApplyEq(); Show(); e.StopPropagation();
            });
            Show();
        }

        // ---- look ----
        static readonly string[] AccentNames = { "黄铜", "青绿", "钴蓝", "绛红", "紫藤", "橄榄", "琥珀" };
        static readonly string[] AccentHex = { "#CDB070", "#6FB7A6", "#6C9BD8", "#C9606A", "#A98BD0", "#9DB45E", "#E0944F" };
        static readonly string[] BaseNames = { "苔灰", "墨黑", "暖棕" };

        void FillLookSettings()
        {
            var state = MusicLibrarySession.State;
            var color = Group("主题色");
            SwitchRow(color, "cover", "封面取色", "强调色跟着正在播放的封面变化", !state.noCoverTheme, () => { state.noCoverTheme = !state.noCoverTheme; themeId = "<reload>"; });
            var row = Box(color, "music-settings-row"); row.AddToClassList("music-settings-chiprow");
            Text(row, "固定强调色", "music-setting-title").style.flexGrow = 0;
            Text(row, string.IsNullOrEmpty(state.themeAccent) ? "未固定：跟随封面或黄铜" : "已固定，封面取色不再改变它", "music-setting-note");
            var swatches = Box(row, "music-settings-chips");
            var auto = Chip(swatches, "跟随", () => { state.themeAccent = ""; themeId = "<reload>"; MusicLibrarySession.Save(); RefreshSettings(); }, "music-accent-auto");
            auto.AddToClassList("music-settings-chip"); auto.EnableInClassList("is-active", string.IsNullOrEmpty(state.themeAccent));
            for (int i = 0; i < AccentHex.Length; i++)
            {
                string hex = AccentHex[i];
                var swatch = new Button(() => { state.themeAccent = hex; themeId = "<reload>"; MusicLibrarySession.Save(); RefreshSettings(); }) { name = "music-accent-" + i, tooltip = AccentNames[i] };
                swatch.AddToClassList("music-swatch"); swatch.EnableInClassList("is-active", string.Equals(state.themeAccent, hex, StringComparison.OrdinalIgnoreCase));
                ColorUtility.TryParseHtmlString(hex, out var c); swatch.style.backgroundColor = c; swatches.Add(swatch);
            }
            var page = Group("页面");
            ChipsRow(page, "base", "底色", "整个音乐应用的背景色调", BaseNames, Mathf.Clamp(state.themeBase, 0, BaseNames.Length - 1), i => { state.themeBase = i; ApplyAccent(); });
            SwitchRow(page, "motion", "减少动效", "关闭淡入、位移和大部分过渡", state.reduceMotion, () => { state.reduceMotion = !state.reduceMotion; root.EnableInClassList("music-reduce-motion", state.reduceMotion); });
        }

        // ---- lyrics ----
        void FillLyricSettings()
        {
            var state = MusicLibrarySession.State;
            var group = Group("歌词显示");
            SwitchRow(group, "filter", "过滤制作信息", "去掉作词、作曲、版权等行（额外过滤词在歌词页）", state.lyricFilter, () => state.lyricFilter = !state.lyricFilter);
            SwitchRow(group, "translation", "显示翻译", "有翻译时显示在原词下方", state.lyricTranslation, () => state.lyricTranslation = !state.lyricTranslation);
            SwitchRow(group, "estimate", "估算逐字时间", "没有逐字时间的歌词按字数估算", state.estimateWords, () => state.estimateWords = !state.estimateWords);
            int[] sizes = { 20, 24, 28, 32 };
            ChipsRow(group, "size", "字号", null, sizes.Select(x => x + " px").ToList(), Array.IndexOf(sizes, state.lyricFontSize), i => state.lyricFontSize = sizes[i]);
            var timing = Group("时间校准");
            var row = Box(timing, "music-settings-row"); row.AddToClassList("music-settings-chiprow");
            Text(row, "全局偏移", "music-setting-title").style.flexGrow = 0;
            Text(row, "当前 " + Signed(state.lyricGlobalOffsetMs) + " ms，加在每首歌自己的偏移之上", "music-setting-note");
            var buttons = Box(row, "music-settings-chips");
            var open = Chip(buttons, "打开标尺", () => OpenOffsetRuler(true), "music-set-ruler"); open.AddToClassList("music-settings-chip");
            var reset = Chip(buttons, "归零", () => { state.lyricGlobalOffsetMs = 0; MusicLibrarySession.Save(); RefreshSettings(); }, "music-set-ruler-reset"); reset.AddToClassList("music-settings-chip");
        }

        // ---- the player bar ----
        void FillBarSettings()
        {
            var group = Group("播放栏按钮");
            Text(group, "选择要放在播放栏上的按钮，并用箭头调整先后。队列、歌词、唱片档案和设置始终在栏上。", "music-setting-hint");
            var slots = BarSlots();
            var ordered = slots.Concat(SlotKeys.Where(k => !slots.Contains(k))).ToList();
            foreach (string key in ordered)
            {
                string slot = key; int index = Array.IndexOf(SlotKeys, slot); bool on = slots.Contains(slot);
                var row = Box(group, "music-settings-row"); row.AddToClassList("music-slot-row");
                var text = Box(row, "music-setting-text");
                Text(text, SlotNames[index], "music-setting-title");
                Text(text, SlotNotes[index], "music-setting-note");
                if (on)
                {
                    Chip(row, "▲", () => { MoveSlot(slot, -1); UpdateBarChips(); RefreshSettings(); }, "music-slot-up-" + slot, "前移").AddToClassList("music-slot-move");
                    Chip(row, "▼", () => { MoveSlot(slot, 1); UpdateBarChips(); RefreshSettings(); }, "music-slot-down-" + slot, "后移").AddToClassList("music-slot-move");
                }
                var toggle = Chip(row, on ? "已放入" : "放入", () => { ToggleSlot(slot); UpdateBarChips(); RefreshSettings(); }, "music-slot-" + slot);
                toggle.AddToClassList("music-settings-chip"); toggle.EnableInClassList("is-active", on);
            }
        }

        // ---- help ----
        void FillHelpSettings()
        {
            var group = Group("使用帮助");
            var row = Box(group, "music-settings-row"); row.AddToClassList("music-settings-chiprow");
            Text(row, "新手教学", "music-setting-title").style.flexGrow = 0;
            Text(row, "用几步带你认识搜索、播放栏、歌词页和设置。", "music-setting-note");
            var chips = Box(row, "music-settings-chips");
            Chip(chips, "重新观看", () => { CloseModal(); StartTutorial(); }, "music-tutorial-again").AddToClassList("music-settings-chip");
            var keys = Group("快捷键");
            foreach (string line in new[] { "空格  播放 / 暂停", "← →  快退 / 快进 5 秒", "L  打开歌词页", "Ctrl+F  搜索", "Esc  关闭当前面板" })
                Text(keys, line, "music-setting-note").AddToClassList("music-settings-key");
        }
    }
}
