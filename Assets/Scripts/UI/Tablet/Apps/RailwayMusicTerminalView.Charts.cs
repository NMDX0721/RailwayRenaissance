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
        Label searchHint; string hintCurrent; int hintIndex;

        // The search pill's hint text scrolls through what is being searched for (the hot searches, or the soundtrack until they arrive), like the
        // streaming apps' search boxes. Enter on an empty box searches the hint that is showing.
        void StartSearchHints()
        {
            searchHint = new Label("搜索曲目、作者、分类") { pickingMode = PickingMode.Ignore }; searchHint.AddToClassList("music-search-hint");
            search.Add(searchHint);
            search.RegisterValueChangedCallback(_ => searchHint.style.display = search.value.Length > 0 ? DisplayStyle.None : DisplayStyle.Flex);
            search.RegisterCallback<KeyDownEvent>(e => {
                if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && search.value.Length == 0 && !string.IsNullOrEmpty(hintCurrent) && hintCurrent != "搜索曲目、作者、分类")
                { RunSearch(hintCurrent); e.StopImmediatePropagation(); }
            }, TrickleDown.TrickleDown);
            search.schedule.Execute(CycleSearchHint).Every(4200);
            CycleSearchHint(true);
        }

        System.Collections.Generic.List<string> HintWords()
        {
            var words = new System.Collections.Generic.List<string>();
            var hots = MusicCharts.HotSearches;
            if (hots != null) words.AddRange(hots.Take(10));
            if (words.Count == 0) words.AddRange(MusicLibrarySession.Tracks.Where(t => !t.local && string.IsNullOrEmpty(t.provider) && MusicLibrarySession.Unlocked(t)).Take(6).Select(t => t.title));
            if (words.Count == 0) words.Add("搜索曲目、作者、分类");
            return words;
        }

        void CycleSearchHint() => CycleSearchHint(false);
        void CycleSearchHint(bool immediate)
        {
            if (searchHint == null || (searchHint.panel == null && !immediate)) return;
            searchHint.style.display = search.value.Length > 0 ? DisplayStyle.None : DisplayStyle.Flex;   // also when the text was set without a change event
            var words = HintWords(); hintIndex = (hintIndex + 1) % words.Count;
            string next = words[immediate ? 0 : hintIndex];
            if (next == hintCurrent) return;
            if (immediate || searchHint.panel == null) { hintCurrent = next; searchHint.text = next; return; }
            searchHint.AddToClassList("is-out");
            searchHint.schedule.Execute(() => { hintCurrent = next; searchHint.text = next; searchHint.RemoveFromClassList("is-out"); }).StartingIn(280);
        }

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
            if (charts == null)
            {
                var grid = Box(parent, "music-card-grid");
                Text(grid, MusicCharts.ChartsFailed ? "榜单暂时无法读取（需要网络）" : "正在读取榜单…", "music-muted");
                if (MusicCharts.ChartsFailed) Button(grid, "重试", () => { MusicCharts.Ensure(); Refresh(); });
            }
            else
            {
                var row = BigRow(parent);
                foreach (var chart in charts.Take(10))
                {
                    string id = chart.id, title = chart.title;
                    BigCard(row, title, string.IsNullOrEmpty(chart.subtitle) ? chart.count + " 首" : chart.subtitle, "chart:" + id, chart.cover, null, () => OpenOnlinePlaylist("netease", id, title));
                }
            }
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
            grid.AddToClassList("music-big-wrap");
            foreach (var chart in charts)
            {
                string id = chart.id, title = chart.title;
                string caption = string.IsNullOrEmpty(chart.subtitle) ? chart.count + " 首" : chart.subtitle + " · " + chart.count + " 首";
                var card = BigCard(grid, title, caption, "chart:" + id, chart.cover, null, () => OpenOnlinePlaylist("netease", id, title));
                if (chart.preview != null && chart.preview.Length > 0) card.tooltip = title + "\n" + string.Join("\n", chart.preview);
            }
        }

        // The drop-down under the search box while it is focused and empty: what was played last, and what is being searched for.
        void ShowSuggestions()
        {
            if (query.Length > 0 || search.value.Length > 0 || root == null) return;
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
            var recent = MusicLibrarySession.RecentTracks(4);
            if (recent.Count > 0)
            {
                Text(suggestions, "最近在听", "music-suggest-heading");
                foreach (var track in recent)
                {
                    string id = track.id;
                    var row = Button(suggestions, "播放", () => { HideSuggestions(); MusicLibrarySession.Play(id); }, "music-suggest-" + id); row.Clear(); row.AddToClassList("music-suggest-row");
                    Cover(row, track.id, track.category, 34);
                    var text = Box(row, "music-suggest-text");
                    Text(text, track.title, "music-suggest-title"); Text(text, string.IsNullOrEmpty(track.artist) ? track.category : track.artist, "music-suggest-sub");
                    row.tooltip = track.title;
                }
            }
            var hots = MusicCharts.HotSearches;
            if (hots != null && hots.Length > 0)
            {
                Text(suggestions, "热搜榜", "music-suggest-heading");
                var cols = Box(suggestions, "music-suggest-cols");
                int rank = 0;
                foreach (string word in hots.Take(10))
                {
                    string captured = word; rank++;
                    var item = Button(cols, "播放", () => RunSearch(captured), "music-hot-" + rank); item.Clear(); item.AddToClassList("music-suggest-hot");
                    var number = Text(item, rank.ToString(), "music-suggest-rank"); number.EnableInClassList("is-top", rank <= 3);
                    Text(item, captured, "music-suggest-word");
                    item.tooltip = captured;
                }
            }
            if (recent.Count == 0 && (hots == null || hots.Length == 0)) Text(suggestions, "输入曲名、作者或分类", "music-muted");
        }
    }
}
