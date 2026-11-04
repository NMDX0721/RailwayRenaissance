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
        Button speedChip, toneChip, timerChip, eqChip;   // optional bar buttons
        VisualElement barButtons;                         // the row they sit in, with the fixed buttons after them
        Button barAnchor;                                 // the first fixed button; pinned ones are placed in front of it, in the player's order
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
                Button eqRow = new Button(() => { CloseModal(); OpenSettings("sound"); }) { name = "music-setting-eq", tooltip = "均衡器与统一响度" }; eqRow.AddToClassList("music-setting-row"); menu.Add(eqRow);
                eqRow.Add(new Label("音效 · 均衡器") { pickingMode = PickingMode.Ignore, enableRichText = false, name = null }); eqRow[0].AddToClassList("music-setting-title");
                var eqValue = new Label(MusicEqualizer.SummaryText()) { pickingMode = PickingMode.Ignore, enableRichText = false }; eqValue.AddToClassList("music-setting-value"); eqRow.Add(eqValue);
                var eqArrow = new Label("›") { pickingMode = PickingMode.Ignore }; eqArrow.AddToClassList("music-setting-arrow"); eqRow.Add(eqArrow);
                CategoryRow(menu, "tone", "收听音色", ToneNames[Mathf.Clamp((int)MusicAudioEffectsEngine.CurrentFilterMode, 0, 3)]);
                CategoryRow(menu, "timer", "睡眠定时", TimerText());
                if (hasTrack) CategoryRow(menu, "timing", "歌词时间校准", TimingText(id));
                var pins = BarSlots();
                CategoryRow(menu, "pins", "放到播放栏", pins.Count > 0 ? string.Join(" ", pins.Select(k => SlotNames[Array.IndexOf(SlotKeys, k)])) : "无");
                ToggleRow(menu, "crossfade", "智能过渡 · 换歌淡入淡出", !state.noCrossfade, () => { state.noCrossfade = !state.noCrossfade; MusicAudioEffectsEngine.CrossfadeEnabled = !state.noCrossfade; });
                ToggleRow(menu, "cover", "封面取色 · 强调色跟着封面", !state.noCoverTheme, () => { state.noCoverTheme = !state.noCoverTheme; themeId = "<reload>"; });
                var links = Box(menu, "music-option-row"); links.AddToClassList("music-option-links");
                Chip(links, "电台", () => { CloseModal(); TabletMusicAppUI.StartRadioAuto(); }, "music-radio", "先民时代电台：顺序全自动轮播").AddToClassList("music-option-link");
                Chip(links, "唱片档案", () => { CloseModal(); ShowDetail(TabletMusicAppUI.tabletCurrentTrackId); }, "music-option-detail").AddToClassList("music-option-link");
                Chip(links, "歌词 / 字幕", () => { CloseModal(); OpenLyrics(); }, "music-option-lyrics").AddToClassList("music-option-link");
                Chip(links, "全部设置", () => { CloseModal(); OpenSettings(); }, "music-option-allsettings", "音乐设置页").AddToClassList("music-option-link");
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
                    Chip(row, "标尺", () => OpenOffsetRuler(), "music-option-ruler", "用标尺拖动校准，可作用于所有歌曲");
                    Text(menu, "歌词比声音晚就点“提前”，早就点“延后”，每次 100 毫秒；每首歌单独记住。", "music-setting-hint");
                    break;
                }
                case "pins":
                {
                    BackBar(menu, "放到播放栏");
                    for (int i = 0; i < SlotKeys.Length; i++)
                    {
                        string key = SlotKeys[i];
                        ChoiceRow(menu, "pin", i, SlotNames[i], SlotNotes[i], BarSlots().Contains(key), () => ToggleSlot(key), false);
                    }
                    Text(menu, "顺序和更多选项在 全部设置 › 播放栏。", "music-setting-hint");
                    break;
                }
            }
        }

        // A chip is a plain text button: no icon, so the text can use the whole width.
        static Button Chip(VisualElement parent, string text, Action action, string name, string tip = null)
        {
            var button = new Button(action) { text = text, tooltip = tip ?? text, name = name }; button.AddToClassList("music-option-chip"); parent.Add(button); return button;
        }

        // ---- The buttons the player pinned to the bar, in the player's own order ----
        static readonly string[] SlotKeys = { "speed", "tone", "timer", "eq" };
        static readonly string[] SlotNames = { "倍速", "音色", "定时", "音效" };
        static readonly string[] SlotNotes = { "在播放栏上显示当前倍速", "在播放栏上加音色按钮", "在播放栏上加定时按钮", "在播放栏上加均衡器按钮" };

        // The pinned buttons in order. Older saves only have the three flags; they give the same set in the default order.
        static List<string> BarSlots()
        {
            var state = MusicLibrarySession.State;
            var list = new List<string>();
            if (string.IsNullOrEmpty(state.barSlots))
            {
                if (state.barSpeed) list.Add("speed");
                if (state.barTone) list.Add("tone");
                if (state.barTimer) list.Add("timer");
                return list;
            }
            foreach (string part in state.barSlots.Split(',')) if (Array.IndexOf(SlotKeys, part) >= 0 && !list.Contains(part)) list.Add(part);
            return list;
        }
        static void SetBarSlots(List<string> list)
        {
            var state = MusicLibrarySession.State;
            state.barSlots = list.Count == 0 ? "-" : string.Join(",", list);   // "-" marks "chosen: none", so the old flags no longer apply
            state.barSpeed = state.barTone = state.barTimer = false;
            MusicLibrarySession.Save();
        }
        static void ToggleSlot(string key)
        {
            var list = BarSlots(); if (!list.Remove(key)) { list.Add(key); SetFolded(key, false); } SetBarSlots(list);
        }
        static void MoveSlot(string key, int delta)
        {
            var list = BarSlots(); int at = list.IndexOf(key), to = at + delta;
            if (at < 0 || to < 0 || to >= list.Count) return;
            list.RemoveAt(at); list.Insert(to, key); SetBarSlots(list);
        }

        Button SlotChip(string key) { switch (key) { case "speed": return speedChip; case "tone": return toneChip; case "timer": return timerChip; default: return eqChip; } }

        // ---- Every bar button can also be folded into the "more" menu at the end of the bar ----
        static readonly string[] BarKeys = { "volume", "speed", "tone", "timer", "eq", "queue", "lyrics", "detail", "settings" };
        static readonly string[] BarKeyNames = { "音量", "倍速", "音色", "定时", "音效", "队列", "歌词", "唱片档案", "播放设置" };
        static readonly string[] BarKeyIcons = { "音量", "倍速", "音色", "定时", "音效", "队列", "歌词 / 字幕", "唱片档案", "播放设置" };
        static readonly string[] BarKeyNotes = { "点开后竖向调节音量", "当前倍速", "收听音色", "睡眠定时", "均衡器与统一响度", "接续播放的队列", "歌词与舞台", "当前曲目的唱片档案", "倍速、音色、定时、歌词校准" };

        static List<string> FoldedKeys()
        {
            var list = new List<string>(); string raw = MusicLibrarySession.State.barFolded;
            if (!string.IsNullOrEmpty(raw)) foreach (string part in raw.Split(',')) if (Array.IndexOf(BarKeys, part) >= 0 && !list.Contains(part)) list.Add(part);
            return list;
        }
        // Buttons that are not shown on the bar are never lost: they wait in the "more" menu. An optional button that is not pinned counts as folded.
        static bool IsFolded(string key) => FoldedKeys().Contains(key) || (IsOptionalKey(key) && !BarSlots().Contains(key));

        // Every bar button in the order the player chose; anything not mentioned keeps its default place after them.
        static List<string> BarOrder()
        {
            var list = new List<string>(); string raw = MusicLibrarySession.State.barOrder;
            if (!string.IsNullOrEmpty(raw)) foreach (string part in raw.Split(',')) if (Array.IndexOf(BarKeys, part) >= 0 && !list.Contains(part)) list.Add(part);
            foreach (string key in BarKeys) if (!list.Contains(key)) list.Add(key);
            return list;
        }
        static void SetBarOrder(List<string> order) { MusicLibrarySession.State.barOrder = string.Join(",", order); MusicLibrarySession.Save(); }
        static void MoveBarKey(string key, int to)
        {
            var order = BarOrder(); if (!order.Remove(key)) return;
            order.Insert(Mathf.Clamp(to, 0, order.Count), key); SetBarOrder(order);
        }
        static void SetFolded(string key, bool folded)
        {
            var list = FoldedKeys(); list.Remove(key); if (folded) list.Add(key);
            MusicLibrarySession.State.barFolded = string.Join(",", list); MusicLibrarySession.Save();
        }
        static bool IsOptionalKey(string key) => Array.IndexOf(SlotKeys, key) >= 0;

        Button BarButton(string key)
        {
            switch (key)
            {
                case "volume": return volumeButton;
                case "speed": return speedChip;
                case "tone": return toneChip;
                case "timer": return timerChip;
                case "eq": return eqChip;
                case "queue": return root?.Q<Button>("music-queue");
                case "lyrics": return root?.Q<Button>("music-lyrics");
                case "detail": return root?.Q<Button>("music-detail");
                default: return root?.Q<Button>("music-settings");
            }
        }

        // The keys that are on the bar's side at all (optional ones only when pinned), in bar order.
        static bool BarPresent(string key) => true;

        void OpenBarMore()
        {
            var menu = MenuAt(barMoreButton, "更多"); var more = barMoreButton;
            foreach (string key in BarOrder())
            {
                if (!IsFolded(key)) continue;
                string k = key; int at = Array.IndexOf(BarKeys, k);
                string label = BarKeyNames[at];
                if (k == "speed") label += " " + SpeedText(); else if (k == "queue" && MusicLibrarySession.State.queue.Count > 0) label += "（" + MusicLibrarySession.State.queue.Count + " 首）";
                PopoverItem(menu, label, false, () =>
                {
                    CloseModal();
                    switch (k)
                    {
                        case "volume": OpenVolumePopover(more); break;
                        case "speed": case "tone": case "timer": case "settings": OpenControlsPanel(more); break;
                        case "eq": OpenSettings("sound"); break;
                        case "queue": queueOpen = !queueOpen; RenderDrawer(); break;
                        case "lyrics": OpenLyrics(); break;
                        default: ShowDetail(TabletMusicAppUI.tabletCurrentTrackId); break;
                    }
                }).name = "music-bar-more-" + k;
            }
        }

        void UpdateBarChips()
        {
            bool anyFolded = false; var order = BarOrder();
            foreach (string key in order)
            {
                var button = BarButton(key); if (button == null) continue;
                bool fold = IsFolded(key);
                button.style.display = fold ? DisplayStyle.None : DisplayStyle.Flex;
                anyFolded |= fold;
            }
            if (barMoreButton != null) barMoreButton.style.display = anyFolded ? DisplayStyle.Flex : DisplayStyle.None;
            if (barButtons != null)
            {
                int place = 0;
                foreach (string key in order)
                {
                    var button = BarButton(key); if (button == null || button.parent != barButtons) continue;
                    if (barButtons.IndexOf(button) != place) { button.RemoveFromHierarchy(); barButtons.Insert(place, button); }
                    place++;
                }
                if (barMoreButton != null && barMoreButton.parent == barButtons && barButtons.IndexOf(barMoreButton) != place) { barMoreButton.RemoveFromHierarchy(); barButtons.Insert(place, barMoreButton); }
            }
            if (speedChip != null)
            {
                speedChip.text = SpeedText();
                speedChip.EnableInClassList("is-active", Mathf.Abs(MusicAudioEffectsEngine.PlaybackSpeed - 1f) > .01f);
            }
            if (eqChip != null) eqChip.EnableInClassList("is-active", MusicEqualizer.Enabled || MusicEqualizer.ReplayGain);
        }
    }
}
