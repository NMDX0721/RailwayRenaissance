using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // The home page's layout after the streaming apps' tablet pages: a row of filter pills, compact cover tiles for what to play next,
    // and rows of large covers that scroll sideways.
    public sealed partial class RailwayMusicTerminalView
    {
        void HomeChips(VisualElement parent)
        {
            var row = Box(parent, "music-home-chips");
            foreach (string target in new[] { "首页", "榜单", "原声", "本地音乐", "收藏", "云端" })
            {
                string page = target;
                var chip = Button(row, target == "首页" ? "全部" : target, () => { if (page == "云端") OpenSource(cloudProvider, true); else Navigate(page); }, "music-chip-" + target);
                chip.AddToClassList("music-home-chip"); chip.EnableInClassList("is-active", target == "首页");
            }
        }

        // Eight tiles: what was played last, then favourites, then the unlocked soundtrack. Cover on the left, title on the right.
        void HomeCoverTiles(VisualElement parent)
        {
            var picks = new List<MusicLibraryTrack>();
            void Take(IEnumerable<MusicLibraryTrack> source) { foreach (var t in source) if (t != null && picks.Count < 8 && !picks.Contains(t) && MusicLibrarySession.Available(t)) picks.Add(t); }
            Take(MusicLibrarySession.RecentTracks(8));
            Take(MusicLibrarySession.State.favorites.Select(MusicLibrarySession.Find));
            Take(MusicLibrarySession.Tracks.Where(t => !t.local && string.IsNullOrEmpty(t.provider) && MusicLibrarySession.Unlocked(t)));
            if (picks.Count == 0) return;
            var grid = Box(parent, "music-cover-tiles");
            for (int i = 0; i < picks.Count; i++)
            {
                var track = picks[i];
                var tile = Button(grid, "播放", () => MusicLibrarySession.Play(track.id), "music-tile-" + track.id); tile.Clear(); tile.AddToClassList("music-cover-tile");
                Arrive(tile, i);
                Cover(tile, track.id, track.category, 56);
                var text = Box(tile, "music-cover-tile-text");
                Text(text, track.title, "music-cover-tile-title");
                Text(text, string.IsNullOrEmpty(track.artist) ? track.category : track.artist, "music-cover-tile-subtitle");
                tile.tooltip = track.title;
            }
        }

        // A large cover with its title on a dark gradient at the bottom and a play button, for rows that scroll sideways.
        VisualElement BigCard(VisualElement parent, string title, string subtitle, string key, string coverUrl, MusicLibraryTrack track, Action open)
        {
            var card = Button(parent, "播放", open, "music-big-" + key); var glyph = card[0]; card.Clear(); card.AddToClassList("music-big-card");
            card.tooltip = title;
            var art = Box(card, "music-big-art"); art.pickingMode = PickingMode.Ignore;
            if (track != null)
            {
                art.style.backgroundImage = new StyleBackground(MusicAlbumArtResolver.GetAlbumCover(track.id, track.category));
                MusicAlbumArtResolver.LoadRemoteCover(track.id, cover => { if (art.panel != null) art.style.backgroundImage = new StyleBackground(cover); });
            }
            else
            {
                art.style.backgroundImage = new StyleBackground(MusicAlbumArtResolver.GetAlbumCover(key, "歌单"));
                if (!string.IsNullOrEmpty(coverUrl)) MusicAlbumArtResolver.LoadRemoteImage(key, coverUrl, texture => { if (art.panel != null) art.style.backgroundImage = new StyleBackground(texture); });
            }
            var shade = Box(card, "music-big-shade"); shade.pickingMode = PickingMode.Ignore;
            var text = Box(card, "music-big-text"); text.pickingMode = PickingMode.Ignore;
            Text(text, title, "music-big-title"); Text(text, subtitle, "music-big-subtitle");
            var play = Box(card, "music-big-play"); play.pickingMode = PickingMode.Ignore; play.Add(glyph);
            return card;
        }

        ScrollView BigRow(VisualElement parent)
        {
            var row = new ScrollView(ScrollViewMode.Horizontal) { horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Hidden };
            row.AddToClassList("music-big-row"); parent.Add(row); return row;
        }
    }
}
