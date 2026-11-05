using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public sealed partial class RailwayMusicTerminalView
    {
        // The career: what the player has listened to, and how often.
        void RenderCareer()
        {
            var scroll = new ScrollView { name = "music-track-list" }; scroll.AddToClassList("music-track-list"); content.Add(scroll);
            var body = scroll.contentContainer; body.AddToClassList("music-home-content");
            Text(body, "沿线声音档案  /  CAREER", "music-eyebrow");
            Text(body, "生涯", "music-section-title");
            var stats = MusicLibrarySession.PlayStats();
            if (stats.Count == 0) { Text(body, "还没有播放记录。听几首歌，听过的曲目和次数会记在这里。", "music-empty"); return; }
            int total = stats.Sum(s => s.count);
            var cards = Box(body, "music-career-cards");
            void Card(string value, string label, string name)
            {
                var card = Box(cards, "music-career-card"); card.name = name;
                Text(card, value, "music-career-value"); Text(card, label, "music-career-label");
            }
            Card(total.ToString(), "累计播放（次）", "music-career-total");
            Card(stats.Count.ToString(), "听过的曲目（首）", "music-career-songs");
            Text(body, "最常播放", "music-eyebrow");
            int top = stats[0].count;
            for (int i = 0; i < Math.Min(30, stats.Count); i++)
            {
                var stat = stats[i]; string id = stat.id;
                var track = MusicLibrarySession.Find(id);
                var row = new Button(() => { if (MusicLibrarySession.Available(MusicLibrarySession.Find(id))) MusicLibrarySession.Play(id); else ShowToast("这首歌现在无法播放"); }) { name = "music-career-" + i, tooltip = "播放" };
                row.AddToClassList("music-career-row"); body.Add(row);
                Text(row, (i + 1).ToString("D2"), "music-track-number").pickingMode = PickingMode.Ignore;
                var cover = Cover(row, id, track?.category ?? "BGM", 42); cover.pickingMode = PickingMode.Ignore;
                var info = Box(row, "music-track-info"); info.pickingMode = PickingMode.Ignore;
                Text(info, track?.title ?? stat.title ?? id, "music-track-title").pickingMode = PickingMode.Ignore;
                string last = DateTimeOffset.FromUnixTimeSeconds(stat.last).ToLocalTime().ToString("M月d日 HH:mm");
                Text(info, (string.IsNullOrEmpty(track?.artist ?? stat.artist) ? "" : (track?.artist ?? stat.artist) + " · ") + "最近 " + last, "music-track-subtitle").pickingMode = PickingMode.Ignore;
                var bar = Box(row, "music-career-bar"); bar.pickingMode = PickingMode.Ignore;
                var fill = Box(bar, "music-career-fill"); fill.style.width = Length.Percent(Mathf.Max(6f, 100f * stat.count / top)); fill.pickingMode = PickingMode.Ignore;
                Text(row, stat.count + " 次", "music-career-count").pickingMode = PickingMode.Ignore;
            }
        }
    }
}
