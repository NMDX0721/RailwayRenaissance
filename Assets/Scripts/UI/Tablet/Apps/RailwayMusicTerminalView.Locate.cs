using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // Clicking the title on the player bar finds the playing track in the lists: it stays on the current page when the track is listed there,
    // otherwise opens the page that holds it (local music, original soundtrack, or a private playlist), scrolls to its row and lights it up for a moment.
    public sealed partial class RailwayMusicTerminalView
    {
        VisualElement RowOf(string id) => content?.Query(className: "music-track-row").Where(r => r.userData as string == id).First();

        void LocateCurrent()
        {
            string id = TabletMusicAppUI.tabletCurrentTrackId;
            var track = MusicLibrarySession.Find(id);
            if (track == null) { ShowToast("先选择一首曲目"); return; }
            CloseModal();
            if (RowOf(id) == null)
            {
                MusicUserPlaylist list = selected != null && selected.tracks.Contains(id) ? selected : MusicLibrarySession.State.playlists.FirstOrDefault(p => p.tracks.Contains(id));
                if (track.local) Navigate("本地音乐");
                else if (string.IsNullOrEmpty(track.provider)) Navigate("原声");
                else if (list != null) Navigate("歌单", list);
                else if (MusicLibrarySession.State.favorites.Contains(id)) Navigate("收藏");
                else { ShowToast("这首歌来自在线搜索，不在任何列表里"); return; }
            }
            root.schedule.Execute(() => FlashRow(id)).StartingIn(60);
        }

        void FlashRow(string id)
        {
            var row = RowOf(id);
            if (row == null) { ShowToast("列表里没有找到这首歌"); return; }
            row.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(row);
            row.AddToClassList("is-located");
            row.schedule.Execute(() => row.RemoveFromClassList("is-located")).StartingIn(1600);
        }
    }
}
