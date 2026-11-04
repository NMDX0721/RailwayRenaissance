using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // 歌词时间标尺: drag along a ruler (after Folia's offset ruler, much smaller) to move the lyrics against the sound, for this track or for every track.
    // The ruler shows -3 s .. +3 s in 10 ms steps; the chips below it reach past that (up to 10 s) and nudge by 10 ms.
    public sealed partial class RailwayMusicTerminalView
    {
        const int RulerSpan = 3000, RulerStep = 10;
        bool rulerGlobal; MusicTimedDocument rulerDoc;
        Label rulerValue, rulerTotal, rulerLine; VisualElement rulerKnob, rulerFill; IVisualElementScheduledItem rulerTick;

        int RulerOffset() => rulerGlobal ? MusicLibrarySession.State.lyricGlobalOffsetMs : MusicTimedText.Preference(TabletMusicAppUI.tabletCurrentTrackId).offsetMs;

        void SetRulerOffset(int ms)
        {
            ms = Mathf.Clamp(ms, -10000, 10000);
            if (rulerGlobal) MusicLibrarySession.State.lyricGlobalOffsetMs = ms;
            else MusicTimedText.Preference(TabletMusicAppUI.tabletCurrentTrackId).offsetMs = ms;
            ShowRuler();
        }

        void OpenOffsetRuler(bool global = false)
        {
            string id = TabletMusicAppUI.tabletCurrentTrackId;
            if (string.IsNullOrEmpty(id)) { ShowToast("先播放一首歌，再校准歌词时间"); return; }
            rulerGlobal = global; rulerDoc = MusicTimedText.Load(MusicLibrarySession.Find(id));
            var menu = MenuAt(null, "歌词时间标尺"); menu.AddToClassList("music-ruler");
            var scope = Box(menu, "music-ruler-scope");
            var one = Chip(scope, "这首歌", () => { rulerGlobal = false; ShowRuler(); }, "music-ruler-scope-track");
            var all = Chip(scope, "所有歌曲", () => { rulerGlobal = true; ShowRuler(); }, "music-ruler-scope-global");
            one.AddToClassList("music-settings-chip"); all.AddToClassList("music-settings-chip");
            rulerValue = Text(menu, "", "music-ruler-value");
            rulerTotal = Text(menu, "", "music-setting-hint");

            var track = Box(menu, "music-ruler-track"); track.name = "music-ruler-track";
            for (int i = 0; i <= 60; i++)
            {
                var tick = Box(track, i % 10 == 0 ? "music-ruler-tick-major" : i % 5 == 0 ? "music-ruler-tick-mid" : "music-ruler-tick");
                tick.pickingMode = PickingMode.Ignore; tick.style.left = Length.Percent(i / 60f * 100f);
            }
            rulerFill = Box(track, "music-ruler-fill"); rulerFill.pickingMode = PickingMode.Ignore;
            rulerKnob = Box(track, "music-ruler-knob"); rulerKnob.pickingMode = PickingMode.Ignore;
            var marks = Box(menu, "music-ruler-marks");
            foreach (string label in new[] { "-3 s", "-1.5 s", "0", "+1.5 s", "+3 s" }) { var m = Text(marks, label, "music-ruler-mark"); m.pickingMode = PickingMode.Ignore; }
            void SetFrom(float x)
            {
                float width = track.resolvedStyle.width; if (width <= 1f) return;
                int ms = Mathf.RoundToInt((Mathf.Clamp01(x / width) * 2f - 1f) * RulerSpan / RulerStep) * RulerStep;
                SetRulerOffset(ms);
            }
            track.RegisterCallback<PointerDownEvent>(e => { track.CapturePointer(e.pointerId); SetFrom(e.localPosition.x); e.StopPropagation(); });
            track.RegisterCallback<PointerMoveEvent>(e => { if (track.HasPointerCapture(e.pointerId)) SetFrom(e.localPosition.x); });
            track.RegisterCallback<PointerUpEvent>(e => { if (track.HasPointerCapture(e.pointerId)) { track.ReleasePointer(e.pointerId); MusicLibrarySession.Save(); } });
            track.RegisterCallback<GeometryChangedEvent>(_ => ShowRuler());
            track.focusable = true;
            track.RegisterCallback<KeyDownEvent>(e =>
            {
                int step = e.keyCode == KeyCode.LeftArrow ? -RulerStep : e.keyCode == KeyCode.RightArrow ? RulerStep : 0;
                if (step == 0) return;
                SetRulerOffset(RulerOffset() + step * (e.shiftKey ? 10 : 1)); MusicLibrarySession.Save(); e.StopPropagation();
            });

            var nudges = Box(menu, "music-ruler-nudges");
            foreach (int delta in new[] { -1000, -100, -10, 10, 100, 1000 })
            {
                int d = delta;
                Chip(nudges, (d > 0 ? "+" : "") + (Math.Abs(d) >= 1000 ? d / 1000 + " s" : d + " ms"), () => { SetRulerOffset(RulerOffset() + d); MusicLibrarySession.Save(); }, "music-ruler-nudge" + (d > 0 ? "p" : "m") + Math.Abs(d));
            }
            rulerLine = Text(menu, "", "music-ruler-line");
            var actions = Box(menu, "music-ruler-nudges");
            Chip(actions, "重听这一句", ReplayRulerLine, "music-ruler-replay", "回到当前这一句的开头再播一遍，听字幕对不对得上");
            Chip(actions, "归零", () => { SetRulerOffset(0); MusicLibrarySession.Save(); }, "music-ruler-reset");
            Chip(actions, "完成", CloseModal, "music-ruler-done");
            Text(menu, "歌词比声音晚，就往右（提前）；早了，就往左（延后）。重听一句，边听边拖。", "music-setting-hint");
            ShowRuler();
            rulerTick = menu.schedule.Execute(UpdateRulerLine).Every(250);
        }

        void ShowRuler()
        {
            if (rulerValue == null || rulerKnob == null) return;
            var state = MusicLibrarySession.State; string id = TabletMusicAppUI.tabletCurrentTrackId;
            int ms = RulerOffset();
            rulerValue.text = (ms > 0 ? "+" : "") + ms + " ms" + (ms > 0 ? "  提前" : ms < 0 ? "  延后" : "");
            int own = MusicTimedText.Preference(id).offsetMs, global = state.lyricGlobalOffsetMs;
            rulerTotal.text = "这首歌 " + Signed(own) + " + 全局 " + Signed(global) + " = 实际 " + Signed(own + global);
            float t = Mathf.Clamp01((ms + RulerSpan) / (2f * RulerSpan));
            rulerKnob.style.left = Length.Percent(t * 100f);
            rulerFill.style.left = Length.Percent(Mathf.Min(t, .5f) * 100f); rulerFill.style.width = Length.Percent(Mathf.Abs(t - .5f) * 100f);
            var root = rulerKnob.panel?.visualTree;
            var one = root?.Q<Button>("music-ruler-scope-track"); var all = root?.Q<Button>("music-ruler-scope-global");
            one?.EnableInClassList("is-active", !rulerGlobal); all?.EnableInClassList("is-active", rulerGlobal);
            UpdateRulerLine();
        }

        static string Signed(int ms) => (ms > 0 ? "+" : "") + ms;

        // The line that is sounding now, by the corrected clock; shown so the effect of the drag is visible without leaving the ruler.
        void UpdateRulerLine()
        {
            if (rulerLine == null) return;
            var doc = rulerDoc;
            if (doc == null || doc.lines.Count == 0) { rulerLine.text = "这首歌没有同步歌词"; return; }
            int at = doc.Active(TabletMusicAppUI.CurrentPosition + MusicTimedText.EffectiveOffsetMs(TabletMusicAppUI.tabletCurrentTrackId) / 1000d);
            rulerLine.text = at >= 0 && !string.IsNullOrWhiteSpace(doc.lines[at].text) ? "现在唱到：" + doc.lines[at].text : TabletMusicAppUI.tabletIsPlaying ? "现在唱到：（间奏）" : "播放后这里显示当前字幕";
        }

        void ReplayRulerLine()
        {
            string id = TabletMusicAppUI.tabletCurrentTrackId; var clip = TabletMusicAppUI.tabletCurrentClip;
            if (clip == null) return;
            var doc = rulerDoc;
            if (doc == null || doc.lines.Count == 0) return;
            int at = doc.Active(TabletMusicAppUI.CurrentPosition + MusicTimedText.EffectiveOffsetMs(id) / 1000d);
            if (at < 0) at = 0;
            var position = MusicTimedText.SeekPosition(doc.lines[at].start, MusicTimedText.EffectiveOffsetMs(id), clip.length);
            if (!position.HasValue) return;
            TabletMusicAppUI.SeekTabletMusic((float)(position.Value / clip.length));
            if (!TabletMusicAppUI.tabletIsPlaying) TabletMusicAppUI.ToggleTabletMusicPlayPause();
        }
    }
}
