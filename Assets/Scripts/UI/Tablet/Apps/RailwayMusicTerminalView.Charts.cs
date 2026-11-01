using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // Charts, hot searches and the search box's drop-down. The data is NetEase's public charts, read anonymously (see MusicCharts).
    public sealed partial class RailwayMusicTerminalView
    {
        VisualElement suggestions;

        void OnChartsChanged()
        {
            if (root == null || root.panel == null || query.Length > 0) return;
            if (page == "榜单" || page == "首页") Refresh();
            RefreshSuggestions();
        }

        // The home page's chart row: the first few official charts, then the hot searches.
        void HomeCharts(VisualElement parent)
        {
            var bar = Box(parent, "music-section-bar"); Text(bar, "网易云热榜", "music-section-title music-section-small");
            Button(Box(bar, "music-section-actions"), "全部榜单", () => Navigate("榜单"), "music-home-all-charts");
            var charts = MusicCharts.Charts;
            var grid = Box(parent, "music-card-grid");
            if (charts == null)
            {
                Text(grid, MusicCharts.ChartsFailed ? "榜单暂时无法读取（需要网络）" : "正在读取榜单…", "music-muted");
                if (MusicCharts.ChartsFailed) Button(grid, "重试", () => { MusicCharts.Ensure(); Refresh(); });
            }
            else foreach (var chart in charts.Take(6)) AddChartCard(grid, chart);
            HotSearchChips(parent);
        }

        void AddChartCard(VisualElement grid, NeteaseLocalBridge.RemotePlaylist chart)
        {
            string id = chart.id, title = chart.title;
            string caption = string.IsNullOrEmpty(chart.subtitle) ? chart.count + " 首" : chart.subtitle;
            var card = CollectionCard(grid, title, caption, () => OpenOnlinePlaylist("netease", id, title), null, "chart:" + id, chart.cover);
            if (chart.preview != null && chart.preview.Length > 0) card.tooltip = title + "\n" + string.Join("\n", chart.preview);
        }

        void HotSearchChips(VisualElement parent)
        {
            var hots = MusicCharts.HotSearches;
            if (hots == null || hots.Length == 0) return;
            Text(parent, "热搜", "music-eyebrow");
            var row = Box(parent, "music-hot-row");
            foreach (string word in hots.Take(12)) { string captured = word; Button(row, captured, () => RunSearch(captured), "music-hot-" + captured).AddToClassList("music-hot-chip"); }
        }

        void RunSearch(string term) { HideSuggestions(); search.value = term; }

        void RenderCharts()
        {
            MusicCharts.Ensure();
            var scroll = new ScrollView { name = "music-track-list" }; scroll.AddToClassList("music-track-list"); content.Add(scroll);
            var body = scroll.contentContainer; body.AddToClassList("music-home-content");
            Text(body, "榜单", "music-section-title");
            Text(body, "网易云官方榜单与热搜 · 无需登录 · 打开榜单即可播放与收藏其中的曲目", "music-muted");
            HotSearchChips(body);
            var charts = MusicCharts.Charts;
            var grid = Box(body, "music-card-grid");
            if (charts == null)
            {
                Text(grid, MusicCharts.ChartsFailed ? "榜单暂时无法读取，请检查网络后重试" : "正在读取榜单…", "music-muted");
                if (MusicCharts.ChartsFailed) Button(grid, "重试", () => { MusicCharts.Ensure(); Refresh(); });
                return;
            }
            foreach (var chart in charts) AddChartCard(grid, chart);
        }

        // The drop-down under the search box while it is focused and empty: what was played last, and what is being searched for.
        void ShowSuggestions()
        {
            if (query.Length > 0 || root == null) return;
            MusicCharts.Ensure();
            if (suggestions == null)
            {
                suggestions = Box(root, "music-suggest");
                suggestions.style.position = Position.Absolute;
                suggestions.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            }
            suggestions.style.display = DisplayStyle.Flex;
            RefreshSuggestions();
        }

        void HideSuggestions() { if (suggestions != null) suggestions.style.display = DisplayStyle.None; }

        void RefreshSuggestions()
        {
            if (suggestions == null || suggestions.style.display == DisplayStyle.None || query.Length > 0) return;
            suggestions.Clear();
            var box = search.worldBound; var origin = root.worldBound;
            suggestions.style.left = box.x - origin.x; suggestions.style.top = box.yMax - origin.y + 4; suggestions.style.width = Mathf.Max(260f, box.width);
            var recent = MusicLibrarySession.RecentTracks(5);
            if (recent.Count > 0)
            {
                Text(suggestions, "最近在听", "music-eyebrow");
                foreach (var track in recent)
                {
                    string id = track.id;
                    var row = Button(suggestions, track.title + (string.IsNullOrEmpty(track.artist) ? "" : " · " + track.artist), () => { HideSuggestions(); MusicLibrarySession.Play(id); }, "music-suggest-" + id);
                    row.AddToClassList("music-suggest-row"); row.tooltip = track.title;
                }
            }
            var hots = MusicCharts.HotSearches;
            if (hots != null && hots.Length > 0)
            {
                Text(suggestions, "热搜", "music-eyebrow");
                var chips = Box(suggestions, "music-hot-row");
                foreach (string word in hots.Take(10)) { string captured = word; Button(chips, captured, () => RunSearch(captured)).AddToClassList("music-hot-chip"); }
            }
            if (recent.Count == 0 && (hots == null || hots.Length == 0)) Text(suggestions, "输入曲名、作者或分类", "music-muted");
        }
    }
}
