using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public sealed class RailwayMusicTerminalView
    {
        static RailwayMusicTerminalView current;
        VisualElement root, content, navigation, drawer, overlay, disc;
        Label title, time, toast, playingTitle, sectionTitle;
        Button play, mode, favorite;
        Slider progress;
        TextField search;
        string page = "原声", query = "", detailId, lastId, neteaseUserId;
        MusicUserPlaylist selected;
        bool queueOpen, scrubbing;
        float rotation;
        int qrGeneration, cloudRequestGeneration;
        string cloudProvider = "netease";
        bool cloudLoggedIn;
        MusicLyricsView lyrics;
        string renderedPage;
        VisualElement modalReturnFocus;
        VisualElement drawerScrim;
        Button clearSearch;
        Vector2 pendingScroll;
        readonly Dictionary<string, Label> indicators = new Dictionary<string, Label>();

        public static void Build(VisualElement window, Action close)
        {
            if (current != null) MusicLibrarySession.Changed -= current.Refresh;
            window.Clear();
            current = new RailwayMusicTerminalView();
            current.Create(window, close);
        }
        static VisualElement Box(VisualElement parent, string css, string name = null)
        {
            var box = new VisualElement { name = name }; box.AddToClassList(css); parent.Add(box);
            if (css == "music-modal" || name == "music-lyrics-overlay")
            {
                box.AddToClassList("music-arriving");
                box.schedule.Execute(() => box.RemoveFromClassList("music-arriving")).StartingIn(30);
            }
            return box;
        }
        static Label Text(VisualElement parent, string text, string css = "music-label")
        {
            var label = new Label(text); label.AddToClassList(css); parent.Add(label); return label;
        }
        static Button Button(VisualElement parent, string text, Action action, string name = null)
        {
            var button = new Button(action) { name = name }; button.AddToClassList("music-button"); parent.Add(button);
            bool iconOnly = parent.ClassListContains("music-track-row") || parent.ClassListContains("music-queue-row") ||
                parent.ClassListContains("music-player-buttons") || parent.ClassListContains("music-tools") ||
                parent.ClassListContains("music-modal-header") || parent.ClassListContains("music-drawer-header") || parent.ClassListContains("music-header");
            MusicTerminalIcons.Apply(button, text, iconOnly); return button;
        }
        void Create(VisualElement window, Action close)
        {
            root = Box(window, "music-terminal", "railway-music-terminal");
            root.style.unityFontDefinition = Fd();
            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(HandleKey, TrickleDown.TrickleDown);
            var sheet = Resources.Load<StyleSheet>("UI/MusicTerminal");
            if (sheet != null) root.styleSheets.Add(sheet);
            root.RegisterCallback<DetachFromPanelEvent>(_ => {
                MusicLibrarySession.Changed -= Refresh;
                MusicLibrarySession.Save();
                if (current == this) current = null;
            });
            root.RegisterCallback<GeometryChangedEvent>(_ => {
                root.EnableInClassList("music-compact", root.resolvedStyle.width < 1050);
                root.EnableInClassList("music-overlay-queue", root.resolvedStyle.width < 1280);
                if (drawerScrim != null) drawerScrim.style.display = queueOpen && root.resolvedStyle.width < 1280 ? DisplayStyle.Flex : DisplayStyle.None;
            });
            var header = Box(root, "music-header");
            Button(header, "返回桌面", close, "music-close");
            var brand = Box(header, "music-brand");
            Text(brand, "随身听", "music-brand-title"); Text(brand, "铁路复兴 / 沿线声音档案", "music-muted");
            Text(header, "雾峰站  ·  声音收藏\nWUFENG / SOUND ARCHIVE", "music-header-code");
            var body = Box(root, "music-body");
            navigation = Box(body, "music-navigation");
            var main = Box(body, "music-main");
            var tools = Box(main, "music-tools");
            search = new TextField { name = "music-search" }; search.AddToClassList("music-search");
            search.label = "搜索"; tools.Add(search);
            search.RegisterValueChangedCallback(e => { query = e.newValue.Trim(); clearSearch.style.display = search.value.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None; RenderContent(); });
            clearSearch = Button(tools, "关闭", () => { search.value = ""; search.Focus(); }, "music-clear-search");
            clearSearch.tooltip = "清空搜索"; clearSearch.style.display = DisplayStyle.None;
            var queueButton = Button(tools, "队列", () => { queueOpen = !queueOpen; RenderDrawer(); }, "music-queue");
            MusicTerminalIcons.Apply(queueButton, "队列", false);
            var lyricsButton = Button(tools, "歌词 / 字幕", OpenLyrics, "music-lyrics");
            MusicTerminalIcons.Apply(lyricsButton, "歌词 / 字幕", false);
            Button(tools, "操作帮助", ShowHelp, "music-help");
            content = Box(main, "music-content");
            drawerScrim = Box(body, "music-queue-scrim", "music-queue-scrim");
            drawerScrim.style.display = DisplayStyle.None;
            drawerScrim.RegisterCallback<PointerDownEvent>(e => { queueOpen = false; RenderDrawer(); root.Q<Button>("music-queue")?.Focus(); e.StopImmediatePropagation(); });
            drawer = Box(body, "music-drawer", "music-queue-drawer");
            drawer.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            BuildPlayer();
            toast = Text(root, "", "music-toast"); toast.style.display = DisplayStyle.None;
            toast.pickingMode = PickingMode.Ignore;
            MusicLibrarySession.Changed += Refresh;
            Refresh(); Update();
        }
        void Refresh()
        {
            if (root == null) return;
            string pageKey = page + ":" + selected?.id + ":" + query;
            var oldScroll = content.Q<ScrollView>("music-track-list");
            var position = pageKey == renderedPage ? oldScroll != null && oldScroll.contentViewport.layout.height > 0 ? oldScroll.scrollOffset : pendingScroll : Vector2.zero;
            pendingScroll = position;
            var drawerScroll = drawer.Q<ScrollView>(); var drawerPosition = drawerScroll?.scrollOffset ?? Vector2.zero;
            var navScroll = navigation.Q<ScrollView>(); var navPosition = navScroll?.scrollOffset ?? Vector2.zero;
            string focused = (root.panel?.focusController.focusedElement as VisualElement)?.name;
            lastId = null;
            RenderNavigation();
            // Keep an in-flight QR login and cloud search results when playback or favorites change.
            bool keepCloud = page == "云端" && query.Length == 0 && renderedPage == pageKey && content.Q<ScrollView>("music-cloud-scroll") != null;
            if (!keepCloud) RenderContent();
            RenderDrawer(); Update();
            if (renderedPage != pageKey) AnimateEntry(content);
            renderedPage = pageKey;
            var next = content.Q<ScrollView>("music-track-list");
            next?.schedule.Execute(() => { if (next.panel != null) next.scrollOffset = position; });
            var nextDrawer = drawer.Q<ScrollView>();
            nextDrawer?.schedule.Execute(() => { if (nextDrawer.panel != null) nextDrawer.scrollOffset = drawerPosition; });
            var nextNav = navigation.Q<ScrollView>();
            nextNav?.schedule.Execute(() => { if (nextNav.panel != null) nextNav.scrollOffset = navPosition; });
            if (!string.IsNullOrEmpty(focused)) root.Q<Button>(focused)?.Focus();
        }
        void HandleKey(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape && overlay != null) { CloseModal(); e.StopImmediatePropagation(); return; }
            if (e.keyCode == KeyCode.Escape && queueOpen) { queueOpen = false; RenderDrawer(); root.Q<Button>("music-queue")?.Focus(); e.StopImmediatePropagation(); return; }
            var target = e.target as VisualElement;
            bool editing = target is TextField || target?.GetFirstAncestorOfType<TextField>() != null;
            if ((e.ctrlKey || e.commandKey) && e.keyCode == KeyCode.F)
            { CloseModal(); search.Focus(); e.StopImmediatePropagation(); return; }
            if (editing || target is Slider || target?.GetFirstAncestorOfType<Slider>() != null) return;
            // Leave arrow keys to focused buttons and other native controls.
            if ((e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.RightArrow) &&
                (target is Button || target?.GetFirstAncestorOfType<Button>() != null)) return;
            if (overlay != null && lyrics == null) return;
            bool handled = true;
            switch (e.keyCode)
            {
                case KeyCode.Space:
                    if (target is Button || target?.GetFirstAncestorOfType<Button>() != null) return;
                    TabletMusicAppUI.ToggleTabletMusicPlayPause(); break;
                case KeyCode.LeftArrow: SeekSeconds(-5); break;
                case KeyCode.RightArrow: SeekSeconds(5); break;
                case KeyCode.L: if (lyrics != null) CloseModal(); else OpenLyrics(); break;
                default: handled = false; break;
            }
            if (handled) e.StopImmediatePropagation();
        }
        void SeekSeconds(float delta)
        {
            var clip = TabletMusicAppUI.tabletCurrentClip;
            if (clip != null && clip.length > 0) TabletMusicAppUI.SeekTabletMusic((TabletMusicAppUI.CurrentPosition + delta) / clip.length);
        }
        void ShowHelp()
        {
            var panel = Modal("随身听操作");
            Text(panel, "播放：点击三角按钮；鼠标停留可查看按钮名称。\n队列：曲目更多菜单可追加或设为下一首；拖动六点手柄或点击上下箭头排序。\n字幕：顶部字幕按钮或 L；支持本地 LRC、增强 LRC、SRT、WebVTT，以及含 word/start/end/line_index 的逐词 JSON。\n空格：播放/暂停（按钮获得焦点时按原生按钮操作）。\n左右方向键：前后 5 秒（按钮或滑块聚焦时使用控件自身操作）；Ctrl+F：搜索；Esc：关闭详情。\n字幕滚动后停止自动跟随，点击“跟随当前句”恢复。", "music-wrap");
        }
        void OpenLyrics()
        {
            if (MusicLibrarySession.Find(TabletMusicAppUI.tabletCurrentTrackId) == null) { ShowToast("先选择一首曲目，再打开歌词 / 字幕"); return; }
            var returnFocus = root.panel?.focusController.focusedElement as VisualElement;
            CloseModal(); modalReturnFocus = returnFocus;
            overlay = Box(root, "music-overlay", "music-lyrics-overlay");
            lyrics = new MusicLyricsView(overlay, CloseModal);
            overlay.focusable = true; overlay.Focus();
        }
        void RenderNavigation()
        {
            navigation.Clear();
            var plaque = Box(navigation, "music-station-plaque");
            Text(plaque, "雾峰", "music-station-name");
            Text(plaque, "WUFENG  /  随身终端", "music-station-code");
            Text(navigation, "沿线收听", "music-eyebrow");
            foreach (string item in new[] { "原声", "本地音乐", "收藏", "云端" })
            {
                string target = item;
                var b = Button(navigation, item, () => { page = target; selected = null; Refresh(); });
                b.EnableInClassList("is-active", page == item);
            }
            Text(navigation, "私人磁带 / 歌单", "music-eyebrow");
            var lists = new ScrollView(); lists.AddToClassList("music-playlists"); navigation.Add(lists);
            foreach (var list in MusicLibrarySession.State.playlists)
            {
                var b = Button(lists, list.title, () => { page = "歌单"; selected = list; Refresh(); });
                b.tooltip = list.title; b.EnableInClassList("is-active", selected == list);
            }
            Button(navigation, "新建歌单", () => EditName(null), "music-new-playlist");
            Text(navigation, "拾起沿线的声音\n留住旅途的回响", "music-nav-note");
        }
        List<MusicLibraryTrack> VisibleTracks()
        {
            IEnumerable<MusicLibraryTrack> items = MusicLibrarySession.Tracks;
            if (query.Length == 0)
            {
                if (page == "本地音乐") items = items.Where(t => t.local);
                else if (page == "收藏") items = items.Where(t => MusicLibrarySession.State.favorites.Contains(t.id));
                else if (page == "歌单" && selected != null) items = selected.tracks.Select(MusicLibrarySession.Find).Where(t => t != null);
                else items = items.Where(t => !t.local && string.IsNullOrEmpty(t.provider));
            }
            else items = items.Where(t => ((t.title ?? "") + " " + (t.artist ?? "") + " " + t.category).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            return items.ToList();
        }
        void RenderContent()
        {
            content.Clear(); indicators.Clear();
            if (page == "云端" && query.Length == 0) { RenderCloud(); return; }
            string heading = query.Length > 0 ? "搜索结果" : selected != null ? selected.title : page;
            var hero = Box(content, "music-library-heading");
            hero.Add(new VisualElement { pickingMode = PickingMode.Ignore, name = "music-route-engraving" });
            var sleeve = Cover(hero, query.Length > 0 ? "railway_track" : "platform", "BGM", 88);
            sleeve.AddToClassList("music-hero-cover");
            var headingText = Box(hero, "music-heading-text");
            Text(headingText, "沿线声音档案  /  SOUND ARCHIVE", "music-eyebrow");
            sectionTitle = Text(headingText, heading, "music-section-title");
            var items = VisibleTracks();
            Text(headingText, query.Length > 0 ? $"已收录曲库 · {items.Count} 个结果" : page == "原声" ? "沿线原声 · 随旅途逐步解锁" : $"私人收藏 · {items.Count} 首", "music-muted");
            var actions = Box(content, "music-list-actions");
            Button(actions, "播放全部", () => PlayAll(items), "music-play-all");
            if (page == "本地音乐")
            {
                Button(actions, "导入音乐", () => LocalMusicFileImporter.PromptImportAudioFiles(count => {
                    MusicLibrarySession.RefreshLocal();
                    ShowToast(count > 0 ? $"已导入 {count} 个音频文件" : "未导入文件；支持 MP3 / WAV / OGG");
                }), "music-import");
                Button(actions, "刷新文件", () => MusicLibrarySession.RefreshLocal());
                Button(actions, "打开文件夹", LocalMusicFileImporter.OpenCustomMusicFolder);
            }
            if (selected != null && query.Length == 0)
            {
                Button(actions, "重命名", () => EditName(selected));
                Button(actions, "删除歌单", () => Confirm("删除「" + selected.title + "」？音频文件和收藏会保留。", () => {
                    MusicLibrarySession.State.playlists.Remove(selected); selected = null; page = "原声"; MusicLibrarySession.Commit();
                }));
            }
            var tableHead = Box(content, "music-table-head");
            Text(tableHead, "档案 / 曲目", "music-grow"); Text(tableHead, "时长", "music-duration"); Text(tableHead, "操作", "music-action-caption");
            var scroll = new ScrollView { name = "music-track-list" }; scroll.AddToClassList("music-track-list"); content.Add(scroll);
            if (items.Count == 0)
            {
                string emptyMessage = query.Length > 0 ? "没有找到曲目，试试曲名、作者或分类。" :
                    selected != null ? "歌单还是空的。从曲目的“更多”菜单加入歌曲。" :
                    page == "本地音乐" ? "还没有本地音乐。点击上方“导入音乐”选择音频文件。" :
                    page == "收藏" ? "还没有收藏的曲目。点击曲目旁的心形图标即可收藏。" :
                    "这里还没有曲目。";
                Text(scroll, emptyMessage, "music-empty");
            }
            for (int i = 0; i < items.Count; i++)
            {
                var track = items[i];
                var row = Box(scroll.contentContainer, "music-track-row"); row.userData = track.id;
                if (selected != null && query.Length == 0) AddReorder(row, selected.tracks, i);
                var indicator = Text(row, (i + 1).ToString("D2"), "music-track-number"); indicators[track.id] = indicator;
                Cover(row, track.id, track.category, 42);
                var info = Box(row, "music-track-info");
                var trackTitle = Text(info, track.title, "music-track-title"); trackTitle.tooltip = track.title;
                bool unlocked = MusicLibrarySession.Unlocked(track);
                bool available = unlocked && MusicLibrarySession.Available(track);
                string status = !unlocked ? "未解锁 · " + track.condition : !available ? (!string.IsNullOrEmpty(track.provider) ? "在线曲目暂不可用" : "文件不可用 · 请检查或重新导入") : track.local ? (string.IsNullOrEmpty(track.artist) ? "私人磁带 · 本地音乐" : "私人磁带 · " + track.artist) : !string.IsNullOrEmpty(track.provider) ? track.category + " · " + (track.artist ?? "") : track.category + " · " + track.condition;
                Text(info, status, "music-track-subtitle").tooltip = status;
                row.EnableInClassList("is-unavailable", !available);
                Text(row, track.duration > 0 ? TabletMusicAppUI.FormatAudioTime(track.duration) : "--:--", "music-duration");
                var start = Button(row, "播放", () => MusicLibrarySession.Play(track.id), "music-play-" + track.id); start.SetEnabled(available);
                Button(row, MusicLibrarySession.State.favorites.Contains(track.id) ? "已藏" : "收藏", () => MusicLibrarySession.ToggleFavorite(track.id), "music-favorite-" + track.id);
                Button(row, "更多", () => TrackActions(track), "music-more-" + track.id);
                row.RegisterCallback<ClickEvent>(e => { if (e.clickCount == 2 && e.target is not UnityEngine.UIElements.Button) MusicLibrarySession.Play(track.id); });
            }
        }
        void PlayAll(List<MusicLibraryTrack> items)
        {
            var playable = items.Where(MusicLibrarySession.Available).ToList();
            if (playable.Count == 0) { ShowToast("当前列表没有可播放的曲目"); return; }
            var queue = MusicLibrarySession.State.queue; queue.Clear(); queue.AddRange(playable.Skip(1).Select(t => t.id));
            TabletMusicAppUI.SetPlayMode((int)TabletMusicAppUI.TabletPlayMode.Sequential);
            MusicLibrarySession.Play(playable[0].id);
        }
        VisualElement Modal(string heading)
        {
            if (overlay == null) modalReturnFocus = root.panel?.focusController.focusedElement as VisualElement;
            overlay?.RemoveFromHierarchy(); disc = null; lyrics = null;
            overlay = Box(root, "music-overlay");
            var panel = Box(overlay, "music-modal");
            var bar = Box(panel, "music-modal-header"); Text(bar, heading, "music-grow");
            Button(bar, "关闭", CloseModal);
            overlay.RegisterCallback<PointerDownEvent>(e => { if (e.target == overlay) CloseModal(); });
            overlay.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) CloseModal(); });
            return panel;
        }
        void CloseModal()
        {
            overlay?.RemoveFromHierarchy(); overlay = null; disc = null; detailId = null; lyrics = null;
            if (modalReturnFocus?.panel == root.panel && modalReturnFocus.enabledInHierarchy) modalReturnFocus.Focus();
            else root.Focus();
            modalReturnFocus = null;
        }
        static void AnimateEntry(VisualElement element)
        {
            element.AddToClassList("music-panel-entering");
            element.schedule.Execute(() => element.RemoveFromClassList("music-panel-entering")).StartingIn(30);
        }
        void Confirm(string message, Action action)
        {
            var panel = Modal("确认操作"); Text(panel, message, "music-wrap");
            Button(panel, "确认删除", () => { CloseModal(); action(); });
        }
        void EditName(MusicUserPlaylist list)
        {
            var panel = Modal(list == null ? "新建私人歌单" : "重命名歌单");
            var field = new TextField("名称") { value = list?.title ?? "", maxLength = 60 }; panel.Add(field); field.Focus();
            Button(panel, "保存", () => {
                if (string.IsNullOrWhiteSpace(field.value)) { ShowToast("请填写歌单名称"); return; }
                if (list == null) selected = MusicLibrarySession.CreatePlaylist(field.value);
                else { list.title = field.value.Trim(); selected = list; }
                page = "歌单"; CloseModal(); MusicLibrarySession.Commit();
            }, "music-save-playlist");
        }
        void TrackActions(MusicLibraryTrack track)
        {
            var panel = Modal(track.title);
            Button(panel, "查看唱片档案", () => ShowDetail(track.id));
            Button(panel, "下一首播放", () => { MusicLibrarySession.Enqueue(track.id, true); CloseModal(); ShowToast("已放到队列最前"); });
            Button(panel, "加入播放队列", () => { MusicLibrarySession.Enqueue(track.id); CloseModal(); ShowToast("已加入播放队列"); });
            Text(panel, "加入歌单", "music-eyebrow");
            var lists = new ScrollView(); lists.style.maxHeight = 180; panel.Add(lists);
            foreach (var list in MusicLibrarySession.State.playlists)
                Button(lists, list.title, () => {
                    bool added = MusicLibrarySession.AddToPlaylist(list, track.id); CloseModal(); ShowToast(added ? "已加入歌单" : "歌单中已有这首曲目");
                });
            if (MusicLibrarySession.State.playlists.Count == 0) Text(lists, "先在左侧新建一个歌单。", "music-muted");
            if (selected != null && query.Length == 0)
                Button(panel, "从当前歌单移除", () => { selected.tracks.Remove(track.id); CloseModal(); MusicLibrarySession.Commit(); });
        }
        void AddReorder(VisualElement row, List<string> list, int index)
        {
            var grip = Button(row, "≡", () => { }); grip.AddToClassList("music-grip"); grip.tooltip = "拖动排序";
            bool dragging = false;
            grip.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { dragging = true; grip.CapturePointer(e.pointerId); e.StopImmediatePropagation(); } }, TrickleDown.TrickleDown);
            grip.RegisterCallback<PointerUpEvent>(e => {
                if (!dragging) return;
                dragging = false;
                if (grip.HasPointerCapture(e.pointerId)) grip.ReleasePointer(e.pointerId);
                var rows = row.parent.Children().Where(c => c.ClassListContains("music-track-row") || c.ClassListContains("music-queue-row")).ToList();
                int target = rows.Count - 1;
                float distance = float.MaxValue;
                for (int i = 0; i < rows.Count; i++) if (Mathf.Abs(e.position.y - rows[i].worldBound.center.y) < distance)
                { target = i; distance = Mathf.Abs(e.position.y - rows[i].worldBound.center.y); }
                MusicLibrarySession.Move(list, index, target); e.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
            var moveUp = Button(row, "↑", () => MusicLibrarySession.Move(list, index, index - 1));
            moveUp.tooltip = "向上移动"; moveUp.SetEnabled(index > 0);
            var moveDown = Button(row, "↓", () => MusicLibrarySession.Move(list, index, index + 1));
            moveDown.tooltip = "向下移动"; moveDown.SetEnabled(index + 1 < list.Count);
        }
        void RenderDrawer()
        {
            bool opening = queueOpen && drawer.resolvedStyle.display == DisplayStyle.None;
            drawer.Clear(); drawer.style.display = queueOpen ? DisplayStyle.Flex : DisplayStyle.None;
            drawerScrim.style.display = queueOpen && root.resolvedStyle.width < 1280 ? DisplayStyle.Flex : DisplayStyle.None;
            root.Q<Button>("music-queue")?.EnableInClassList("is-active", queueOpen);
            if (!queueOpen) return;
            if (opening) AnimateEntry(drawer);
            var header = Box(drawer, "music-drawer-header"); Text(header, "接续播放", "music-grow");
            Button(header, "收起", () => { queueOpen = false; RenderDrawer(); });
            Text(drawer, "下一首从这里出发", "music-muted");
            var queue = MusicLibrarySession.State.queue;
            Button(drawer, "清空队列", () => { queue.Clear(); MusicLibrarySession.Commit(); }).SetEnabled(queue.Count > 0);
            var scroll = new ScrollView(); scroll.style.flexGrow = 1; scroll.style.minHeight = 0; drawer.Add(scroll);
            if (queue.Count == 0) Text(scroll, "队列为空。可从曲目的“更多”菜单追加。", "music-wrap");
            for (int i = 0; i < queue.Count; i++)
            {
                int index = i; string id = queue[i]; var track = MusicLibrarySession.Find(id);
                var item = Box(scroll.contentContainer, "music-queue-item");
                Text(item, track?.title ?? "未知曲目", "music-wrap");
                if (!MusicLibrarySession.Available(track)) Text(item, "不可用 / 连播时跳过", "music-muted");
                var row = Box(item, "music-queue-row");
                // Queue items have variable heights, so drag targets use the containing item bounds.
                AddQueueReorder(row, item, queue, index);
                Button(row, "播放", () => {
                    var queuedTrack = MusicLibrarySession.Find(id);
                    if (!MusicLibrarySession.Available(queuedTrack))
                    {
                        ShowToast(MusicLibrarySession.Unlocked(queuedTrack) ? "音频文件不可用，请检查文件或重新导入" : "该曲目尚未解锁或不存在");
                        return;
                    }
                    MusicLibrarySession.PlayQueued(index);
                });
                Button(row, "移除", () => { queue.RemoveAt(index); MusicLibrarySession.Commit(); });
            }
        }
        void AddQueueReorder(VisualElement row, VisualElement item, List<string> queue, int index)
        {
            var grip = Button(row, "≡", () => { }); grip.tooltip = "拖动排序";
            bool dragging = false;
            grip.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { dragging = true; grip.CapturePointer(e.pointerId); e.StopImmediatePropagation(); } }, TrickleDown.TrickleDown);
            grip.RegisterCallback<PointerUpEvent>(e => {
                if (!dragging) return;
                dragging = false;
                if (grip.HasPointerCapture(e.pointerId)) grip.ReleasePointer(e.pointerId);
                var rows = item.parent.Children().ToList(); int target = rows.Count - 1;
                float distance = float.MaxValue;
                for (int i = 0; i < rows.Count; i++) if (Mathf.Abs(e.position.y - rows[i].worldBound.center.y) < distance)
                { target = i; distance = Mathf.Abs(e.position.y - rows[i].worldBound.center.y); }
                MusicLibrarySession.Move(queue, index, target); e.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
            var moveUp = Button(row, "↑", () => MusicLibrarySession.Move(queue, index, index - 1));
            moveUp.tooltip = "向上移动"; moveUp.SetEnabled(index > 0);
            var moveDown = Button(row, "↓", () => MusicLibrarySession.Move(queue, index, index + 1));
            moveDown.tooltip = "向下移动"; moveDown.SetEnabled(index + 1 < queue.Count);
        }
        void BuildPlayer()
        {
            var player = Box(root, "music-player", "music-player");
            var info = Box(player, "music-player-info");
            playingTitle = Text(info, "尚未选择曲目", "music-track-title");
            Button(info, "唱片档案", () => ShowDetail(TabletMusicAppUI.tabletCurrentTrackId), "music-detail");
            var controls = Box(player, "music-player-controls");
            var buttons = Box(controls, "music-player-buttons");
            buttons.AddToClassList("music-transport");
            mode = Button(buttons, "顺序", () => TabletMusicAppUI.SetPlayMode(((int)TabletMusicAppUI.tabletPlayMode + 1) % 3));
            Button(buttons, "上一首", TabletMusicAppUI.PlayPrevTabletTrack);
            play = Button(buttons, "播放", TabletMusicAppUI.ToggleTabletMusicPlayPause, "music-play-toggle"); play.AddToClassList("music-primary");
            Button(buttons, "下一首", TabletMusicAppUI.PlayNextTabletTrack);
            favorite = Button(buttons, "收藏", () => {
                if (MusicLibrarySession.Find(TabletMusicAppUI.tabletCurrentTrackId) != null) MusicLibrarySession.ToggleFavorite(TabletMusicAppUI.tabletCurrentTrackId);
            });
            var timeline = Box(controls, "music-timeline");
            progress = new Slider(0, 1) { name = "music-progress" }; timeline.Add(progress); progress.style.flexGrow = 1;
            progress.RegisterCallback<PointerDownEvent>(_ => scrubbing = true, TrickleDown.TrickleDown);
            progress.RegisterCallback<PointerUpEvent>(_ => scrubbing = false, TrickleDown.TrickleDown);
            progress.RegisterCallback<PointerCaptureOutEvent>(_ => scrubbing = false);
            progress.RegisterValueChangedCallback(e => { if (TabletMusicAppUI.tabletCurrentClip != null) TabletMusicAppUI.SeekTabletMusic(e.newValue); });
            time = Text(timeline, "0:00 / --:--", "music-player-time");
            var extras = Box(player, "music-player-extras");
            var volume = new Slider("音量", 0, 1) { value = MusicLibrarySession.State.volume, name = "music-volume" }; extras.Add(volume);
            volume.tooltip = "音量：拖动或点击滑轨；聚焦后可用方向键微调";
            volume.RegisterValueChangedCallback(e => TabletMusicAppUI.SetVolume(e.newValue));
            var effects = Box(extras, "music-player-buttons");
            effects.AddToClassList("music-utility-buttons");
            Button(effects, "音色", () => {
                var panel = Modal("收听音色");
                foreach (var pair in new[] { ("纯净原声", 0), ("工务电台 · 轻微抖晃", 1), ("旧收音机 · 温暖抖晃", 2) })
                    Button(panel, pair.Item1, () => { MusicAudioEffectsEngine.CurrentFilterMode = (MusicAudioEffectsEngine.RadioFilterMode)pair.Item2; CloseModal(); });
            });
            Button(effects, "定时", () => {
                var panel = Modal("睡眠定时");
                foreach (int minutes in new[] { 0, 15, 30, 45, 999 })
                    Button(panel, minutes == 0 ? "关闭定时" : minutes == 999 ? "播完本曲暂停" : minutes + " 分钟后暂停", () => {
                        MusicAudioEffectsEngine.SetSleepTimer((MusicAudioEffectsEngine.SleepTimerOption)minutes,
                            TabletMusicAppUI.tabletCurrentClip == null ? 0 : TabletMusicAppUI.tabletCurrentClip.length - TabletMusicAppUI.CurrentPosition);
                        CloseModal(); ShowToast("睡眠定时已更新");
                    });
            });
        }
        void ShowDetail(string id)
        {
            var track = MusicLibrarySession.Find(id);
            if (track == null) { ShowToast("先选择一首曲目"); return; }
            var panel = Modal("唱片档案 / 沿线原声"); detailId = id;
            var art = Box(panel, "music-detail-art");
            disc = MusicIconHelper.CreateVinylDiscLarge(170); disc.AddToClassList("music-detail-disc"); art.Add(disc);
            Cover(art, track.id, track.category, 180).AddToClassList("music-detail-cover");
            Text(panel, track.title, "music-detail-title");
            Text(panel, "档案编号 / " + track.id, "music-muted");
            Text(panel, track.local ? "私人磁带 / " + track.fileName : track.condition, "music-wrap");
            if (!string.IsNullOrEmpty(track.description)) Text(panel, track.description, "music-wrap");
            Text(panel, "曲目档案说明 · 非同步歌词", "music-muted");
            Button(panel, "播放这首", () => MusicLibrarySession.Play(track.id));
        }
        void RenderCloud()
        {
            Text(content, "云端", "music-section-title");
            var scroll = new ScrollView { name = "music-cloud-scroll" }; scroll.style.flexGrow = 1; content.Add(scroll);
            var channels = Box(scroll.contentContainer, "music-list-actions");
            channels.AddToClassList("music-channel-selector");
            void AddChannel(string label, string id)
            {
                var button = Button(channels, label, () => { cloudProvider = id; cloudLoggedIn = false; neteaseUserId = null; ++qrGeneration; RenderContent(); });
                button.EnableInClassList("is-active", cloudProvider == id);
            }
            AddChannel("网易云", "netease"); AddChannel("QQ 音乐", "qq"); AddChannel("酷狗", "kugou");
            AddChannel("波点", "bodian"); AddChannel("Subsonic", "subsonic"); AddChannel("WebDAV", "webdav");
            string provider = cloudProvider;
            string providerName = provider == "qq" ? "QQ 音乐" : provider == "kugou" ? "酷狗" : provider == "bodian" ? "波点" : provider == "subsonic" ? "Subsonic" : provider == "webdav" ? "WebDAV" : "网易云";
            var bridge = NeteaseLocalBridge.ForProvider(provider);
            var cloudStatus = Text(scroll, providerName + " / 尚未验证连接", "music-muted"); cloudStatus.name = "music-cloud-status";
            var qrImage = new Image { name = "music-cloud-qr", scaleMode = ScaleMode.ScaleToFit };
            qrImage.style.width = 150; qrImage.style.height = 150; qrImage.style.flexShrink = 0; qrImage.style.display = DisplayStyle.None;
            scroll.Add(qrImage);
            qrImage.RegisterCallback<DetachFromPanelEvent>(_ => {
                if (qrImage.image is Texture2D texture) { qrImage.image = null; UnityEngine.Object.Destroy(texture); }
            });
            Action checkStatus = () => bridge.Send("status", null, reply => {
                if (root.panel == null || page != "云端" || cloudStatus.panel == null || provider != cloudProvider) return;
                cloudLoggedIn = reply.ok && reply.result != null && reply.result.loggedIn;
                neteaseUserId = cloudLoggedIn ? reply.result.userId : null;
                cloudStatus.text = reply.ok && reply.result != null && reply.result.loggedIn
                    ? providerName + (reply.result.sessionOnly ? " / 本机会话：" : " / 已登录：") + reply.result.nickname : providerName + " / " + (reply.ok ? "尚未登录" : reply.error);
            });
            Button(scroll, "检查登录状态", checkStatus);
            Button qrButton = null;
            qrButton = Button(scroll, "扫码登录", () => {
                int generation = ++qrGeneration;
                qrImage.style.display = DisplayStyle.None;
                cloudStatus.text = "正在生成二维码…（最多等待 45 秒）";
                qrButton.SetEnabled(false);
                bridge.Send("qr_start", null, reply => {
                    if (root.panel == null || page != "云端" || generation != qrGeneration || qrImage.panel == null) return;
                    qrButton.SetEnabled(true);
                    if (!reply.ok || reply.result == null) { cloudStatus.text = providerName + " / " + (reply.error ?? "生成失败，请重试"); return; }
                    try {
                        string image = reply.result.image;
                        int comma = image.IndexOf(',');
                        var texture = new Texture2D(2, 2);
                        if (comma < 0 || !texture.LoadImage(Convert.FromBase64String(image.Substring(comma + 1)))) throw new FormatException();
                        if (qrImage.image is Texture2D previous) UnityEngine.Object.Destroy(previous);
                        qrImage.image = texture; qrImage.style.display = DisplayStyle.Flex;
                        cloudStatus.text = "请使用 " + providerName + " 扫描二维码";
                        qrImage.schedule.Execute(() => { if (qrImage.panel != null) scroll.ScrollTo(qrImage); });
                        Action poll = null;
                        poll = () => {
                            if (root.panel == null || page != "云端" || qrImage.panel == null || generation != qrGeneration) return;
                            bridge.Send("qr_check", null, result => {
                                if (root.panel == null || page != "云端" || qrImage.panel == null || generation != qrGeneration) return;
                                if (!result.ok || result.result == null) { cloudStatus.text = providerName + " / " + result.error; return; }
                                int code = result.result.code;
                                if (code == 803) { qrImage.style.display = DisplayStyle.None; checkStatus(); }
                                else if (code == 800) { cloudStatus.text = "二维码已过期，请重新生成"; qrImage.style.display = DisplayStyle.None; }
                                else { cloudStatus.text = code == 802 ? "请在手机上确认登录" : "请使用 " + providerName + " 扫描二维码"; root.schedule.Execute(poll).StartingIn(2500); }
                            });
                        };
                        root.schedule.Execute(poll).StartingIn(2500);
                    } catch (Exception) { cloudStatus.text = "二维码无法显示，请重试"; }
                });
            });
            qrButton.name = "music-cloud-qr-start";
            if (provider == "subsonic" || provider == "webdav")
            {
                qrButton.style.display = DisplayStyle.None;
                var hostField = new TextField("服务器 HTTPS 地址"); scroll.Add(hostField);
                var userField = new TextField("用户名"); scroll.Add(userField);
                var passwordField = new TextField("密码") { isPasswordField = true }; scroll.Add(passwordField);
                Button(scroll, "连接服务器", () => {
                    var parameters = new NeteaseLocalBridge.Parameters { host = hostField.value, username = userField.value, password = passwordField.value };
                    passwordField.value = "";
                    cloudStatus.text = "正在验证服务器…";
                    bridge.Send("connect", parameters, response => {
                        if (cloudStatus.panel == null || provider != cloudProvider) return;
                        cloudLoggedIn = response.ok && response.result != null && response.result.loggedIn;
                        cloudStatus.text = cloudLoggedIn ? providerName + " / 已连接：" + response.result.nickname : providerName + " / " + response.error;
                    });
                });
                Text(scroll, provider == "webdav" ? "搜索根目录；歌单入口显示直接子目录。密码仅在本次进程内。" : "密码仅保存在本次进程内，关闭程序后需重新连接。", "music-muted");
            }
            Text(scroll, "连接配置 / 音乐来源", "music-eyebrow");
            Text(scroll, provider != "netease" ? providerName + "接入为实验功能，扫码、歌单与播放尚需真实账号验证。" : "网易云曲目可通过搜索或歌单加入曲库；播放能力取决于账号与版权。", "music-wrap");
            var cloudSearch = new TextField(providerName + "搜索") { maxLength = 200, name = "music-cloud-query" }; scroll.Add(cloudSearch);
            var cloudResults = new VisualElement { name = "music-cloud-results" }; cloudResults.AddToClassList("music-cloud-results");
            var seenTracks = new HashSet<string>();
            Action<NeteaseLocalBridge.RemoteTrack[], bool> showTracks = (remoteTracks, append) => {
                if (cloudResults.panel == null) return;
                if (!append) { cloudResults.Clear(); seenTracks.Clear(); }
                var list = remoteTracks ?? Array.Empty<NeteaseLocalBridge.RemoteTrack>();
                if (list.Length == 0 && !append) Text(cloudResults, "没有找到曲目", "music-muted");
                foreach (var remote in list)
                {
                    var track = MusicLibrarySession.RegisterRemote(remote, provider);
                    if (track == null || !seenTracks.Add(track.id)) continue;
                    var row = Box(cloudResults, "music-track-row");
                    Cover(row, track.id, track.category, 42);
                    var info = Box(row, "music-track-info");
                    Text(info, track.title, "music-track-title");
                    Text(info, track.artist ?? "", "music-track-subtitle");
                    Button(row, "播放", () => MusicLibrarySession.Play(track.id));
                    Button(row, "收藏", () => {
                        MusicLibrarySession.ToggleFavorite(track.id);
                        cloudStatus.text = MusicLibrarySession.State.favorites.Contains(track.id) ? "已收藏 / " + track.title : "已取消收藏 / " + track.title;
                    });
                    Button(row, "更多", () => TrackActions(track));
                }
                MusicLibrarySession.Save();
            };
            Action searchCloud = () => {
                int searchGeneration = ++cloudRequestGeneration;
                cloudStatus.text = "正在搜索 " + providerName + "…";
                bridge.Send("search", new NeteaseLocalBridge.Parameters { keywords = cloudSearch.value }, result => {
                    if (cloudResults.panel == null || searchGeneration != cloudRequestGeneration) return;
                    cloudStatus.text = result.ok ? providerName + " / 搜索完成" : providerName + " / " + result.error;
                    if (result.ok && result.result != null) showTracks(result.result.tracks, false);
                });
            };
            Button(scroll, provider == "webdav" ? "搜索 / 浏览根目录" : "搜索音乐", searchCloud, "music-cloud-search");
            Button(scroll, provider == "webdav" ? "查看子目录" : "读取我的歌单", () => {
                if (!cloudLoggedIn) { cloudStatus.text = provider == "webdav" || provider == "subsonic" ? "请先连接服务器" : "请先扫码登录并检查状态"; return; }
                cloudResults.Clear();
                int listGeneration = ++cloudRequestGeneration;
                var seenLists = new HashSet<string>();
                Action<int> loadLists = null;
                loadLists = offset => {
                    cloudStatus.text = "正在读取歌单…";
                    bridge.Send("playlists", new NeteaseLocalBridge.Parameters { userId = neteaseUserId, offset = offset }, result => {
                        if (cloudResults.panel == null || listGeneration != cloudRequestGeneration) return;
                        if (!result.ok || result.result == null) { cloudStatus.text = providerName + " / " + result.error; cloudResults.Q<Button>("music-more-lists")?.SetEnabled(true); return; }
                        cloudResults.Q<Button>("music-more-lists")?.RemoveFromHierarchy();
                        cloudStatus.text = providerName + " / 选择歌单";
                        var lists = result.result.playlists ?? Array.Empty<NeteaseLocalBridge.RemotePlaylist>();
                        if (lists.Length == 0 && offset == 0) Text(cloudResults, "没有可读取的歌单", "music-muted");
                        foreach (var list in lists)
                        {
                            if (list == null || !seenLists.Add(list.id)) continue;
                            Button(cloudResults, list.title + " · " + list.count + " 首", () => {
                                int songGeneration = ++cloudRequestGeneration;
                                Action<int, bool> loadSongs = null;
                                loadSongs = (songOffset, append) => {
                                    cloudStatus.text = "正在读取曲目…";
                                    bridge.Send("playlist_tracks", new NeteaseLocalBridge.Parameters { id = list.id, offset = songOffset }, songs => {
                                        if (cloudResults.panel == null || songGeneration != cloudRequestGeneration) return;
                                        if (!songs.ok || songs.result == null) { cloudStatus.text = providerName + " / " + songs.error; cloudResults.Q<Button>("music-more-songs")?.SetEnabled(true); return; }
                                        cloudResults.Q<Button>("music-more-songs")?.RemoveFromHierarchy();
                                        cloudStatus.text = providerName + " / " + list.title;
                                        showTracks(songs.result.tracks, append);
                                        if (songs.result.more && songs.result.nextOffset > songOffset)
                                        {
                                            Button more = null;
                                            more = Button(cloudResults, "加载更多曲目", () => { more.SetEnabled(false); loadSongs(songs.result.nextOffset, true); }, "music-more-songs");
                                        }
                                        else if (songs.result.more) cloudStatus.text += "（部分曲目暂无法继续读取）";
                                    });
                                };
                                loadSongs(0, false);
                            });
                        }
                        if (result.result.more && result.result.nextOffset > offset)
                        {
                            Button more = null;
                            more = Button(cloudResults, "加载更多歌单", () => { more.SetEnabled(false); loadLists(result.result.nextOffset); }, "music-more-lists");
                        }
                        else if (result.result.more) cloudStatus.text += "（部分歌单暂无法继续读取）";
                    });
                };
                loadLists(0);
            });
            Button(scroll, "退出登录", () => bridge.Send("logout", null, result => {
                if (cloudStatus.panel == null || provider != cloudProvider) return;
                ++qrGeneration; ++cloudRequestGeneration; qrImage.style.display = DisplayStyle.None; cloudLoggedIn = false; neteaseUserId = null; cloudResults.Clear(); cloudStatus.text = result.ok ? providerName + " / 已退出" : providerName + " / " + result.error;
            }));
            scroll.Add(cloudResults);
            Text(scroll, "其他来源 / 尚未接通", "music-eyebrow");
            Text(scroll, "Spotify：游戏内使用受平台政策限制。\nYouTube Music：需要符合平台要求的可见播放器。\nApple Music 和 SoundCloud：需要开发者应用凭据与用户授权。\nB 站：需要进一步核实开放平台权限和播放能力。", "music-wrap");
            foreach (var pair in new[] { ("Spotify 歌单链接", "Spotify_Playlist_Url"),
                ("Apple Music 歌单链接", "AppleMusic_Url"), ("YouTube Music 歌单链接", "Ytm_Playlist_Url"),
                ("SoundCloud 链接", "SoundCloud_Url") })
            {
                var field = new TextField(pair.Item1) { value = PlayerPrefs.GetString(pair.Item2, "") }; scroll.Add(field);
                Button(scroll, "保存配置", () => { PlayerPrefs.SetString(pair.Item2, field.value.Trim()); PlayerPrefs.Save(); ShowToast("配置已保存，尚未验证连接"); });
            }
        }
        static VisualElement Cover(VisualElement parent, string id, string category, int size)
        {
            var sleeve = Box(parent, "music-sleeve"); sleeve.style.width = size; sleeve.style.height = size;
            var image = Box(sleeve, "music-cover-image");
            image.style.backgroundImage = new StyleBackground(MusicAlbumArtResolver.GetAlbumCover(id, category));
            MusicAlbumArtResolver.LoadRemoteCover(id, cover => { if (image.panel != null) image.style.backgroundImage = new StyleBackground(cover); });
            return sleeve;
        }
        void Update()
        {
            if (root == null || play == null) return;
            root.EnableInClassList("music-reduce-motion", MusicLibrarySession.State.reduceMotion);
            var queueLabel = root.Q<Button>("music-queue")?.Q<Label>();
            if (queueLabel != null) queueLabel.text = "队列 · " + MusicLibrarySession.State.queue.Count;
            lyrics?.Tick();
            string id = TabletMusicAppUI.tabletCurrentTrackId;
            playingTitle.text = TabletMusicAppUI.tabletCurrentTrackTitle ?? "尚未选择曲目"; playingTitle.tooltip = playingTitle.text;
            MusicTerminalIcons.Apply(play, MusicLibrarySession.Loading ? "加载中" : TabletMusicAppUI.tabletIsPlaying ? "暂停" : "播放", true);
            MusicTerminalIcons.Apply(mode, TabletMusicAppUI.tabletPlayMode == TabletMusicAppUI.TabletPlayMode.Sequential ? "顺序" : TabletMusicAppUI.tabletPlayMode == TabletMusicAppUI.TabletPlayMode.Shuffle ? "随机" : "单曲", true);
            MusicTerminalIcons.Apply(favorite, MusicLibrarySession.State.favorites.Contains(id) ? "已藏" : "收藏", true);
            var volume = root.Q<Slider>("music-volume");
            volume?.SetValueWithoutNotify(MusicLibrarySession.State.volume);
            UpdateSliderFill(volume, MusicLibrarySession.State.volume);
            var clip = TabletMusicAppUI.tabletCurrentClip;
            float position = clip == null ? MusicLibrarySession.State.position : TabletMusicAppUI.CurrentPosition;
            progress.SetEnabled(clip != null);
            if (!scrubbing) progress.SetValueWithoutNotify(clip != null && clip.length > 0 ? position / clip.length : 0);
            UpdateSliderFill(progress, progress.value);
            time.text = TabletMusicAppUI.FormatAudioTime(position) + " / " + (clip != null ? TabletMusicAppUI.FormatAudioTime(clip.length) : "--:--");
            if (lastId != id)
            {
                foreach (var pair in indicators) pair.Value.parent.EnableInClassList("is-playing", pair.Key == id);
                lastId = id;
            }
            if (disc != null && detailId == id && TabletMusicAppUI.tabletIsPlaying && !MusicLibrarySession.State.reduceMotion)
            {
                rotation = (rotation + Time.unscaledDeltaTime * 36) % 360;
                disc.style.rotate = new Rotate(Angle.Degrees(rotation));
            }
        }
        internal static void UpdateSliderFill(Slider slider, float value)
        {
            slider?.AddToClassList("music-horizontal-slider");
            var track = slider?.Q(className: "unity-base-slider__tracker");
            if (track == null) return;
            var fill = track.Q("music-slider-fill");
            if (fill == null)
            {
                fill = new VisualElement { name = "music-slider-fill", pickingMode = PickingMode.Ignore };
                fill.AddToClassList("music-slider-fill"); track.Add(fill);
            }
            fill.style.width = Length.Percent(Mathf.Clamp01(value) * 100);
        }
        public static void UpdatePlayback() => current?.Update();
        public static bool ShowToast(string message)
        {
            if (current?.toast == null || current.root.panel == null) return false;
            var label = current.toast; label.text = message; label.style.display = DisplayStyle.Flex; label.BringToFront();
            label.schedule.Execute(() => label.style.display = DisplayStyle.None).StartingIn(3000); return true;
        }
    }
}

