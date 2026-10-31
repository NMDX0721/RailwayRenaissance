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
        string page = "首页", query = "", detailId, lastId, neteaseUserId;
        MusicUserPlaylist selected;
        bool queueOpen, scrubbing;
        float rotation;
        int qrGeneration, cloudRequestGeneration;
        string cloudProvider = "netease", searchProvider = "auto";
        int catalogGeneration;
        string requestedCloudPlaylist;
        bool cloudLoggedIn;
        MusicLyricsView lyrics;
        string renderedPage;
        VisualElement modalReturnFocus;
        VisualElement drawerScrim;
        Button clearSearch;
        Vector2 pendingScroll;
        readonly Dictionary<string, NeteaseLocalBridge.Response> homeLists = new Dictionary<string, NeteaseLocalBridge.Response>();
        readonly HashSet<string> homeListRequests = new HashSet<string>();
        readonly Dictionary<string, Label> indicators = new Dictionary<string, Label>();

        public static void Build(VisualElement window, Action close)
        {
            if (current != null) { MusicLibrarySession.Changed -= current.Refresh; NeteaseLocalBridge.SessionChanged -= current.Refresh; }
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
            var label = new Label(text); foreach (var part in css.Split(' ')) if (part.Length > 0) label.AddToClassList(part); parent.Add(label); return label;
        }
        static Button Button(VisualElement parent, string text, Action action, string name = null)
        {
            var button = new Button(action) { name = name }; button.AddToClassList("music-button"); parent.Add(button);
            bool iconOnly = parent.ClassListContains("music-track-row") || parent.ClassListContains("music-queue-row") ||
                parent.ClassListContains("music-player-buttons") || parent.ClassListContains("music-tools") ||
                parent.ClassListContains("music-modal-header") || parent.ClassListContains("music-drawer-header") || parent.ClassListContains("music-header");
            MusicTerminalIcons.Apply(button, text, iconOnly); return button;
        }
        static bool sessionRestoreStarted;
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
                NeteaseLocalBridge.SessionChanged -= Refresh;
                MusicLibrarySession.Save();
                if (current == this) current = null;
            });
            // The tablet shell moves a task's view between layers, which fires detach and then attach again:
            // subscriptions dropped on detach must come back on attach, or the page stops updating for good.
            root.RegisterCallback<AttachToPanelEvent>(_ => {
                MusicLibrarySession.Changed -= Refresh; MusicLibrarySession.Changed += Refresh;
                NeteaseLocalBridge.SessionChanged -= Refresh; NeteaseLocalBridge.SessionChanged += Refresh;
                current = this;
                Refresh();
            });
            root.RegisterCallback<GeometryChangedEvent>(_ => {
                root.EnableInClassList("music-compact", root.resolvedStyle.width < 1050);
                root.EnableInClassList("music-overlay-queue", root.resolvedStyle.width < 1280);
                if (drawerScrim != null) drawerScrim.style.display = queueOpen && root.resolvedStyle.width < 1280 ? DisplayStyle.Flex : DisplayStyle.None;
            });
            var header = Box(root, "music-header");
            Button(header, "返回桌面", close, "music-close");
            var navToggle = Button(header, "侧栏", ToggleNavigation, "music-nav-toggle"); navToggle.tooltip = "收起 / 展开侧栏";
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
            NeteaseLocalBridge.SessionChanged += Refresh;
            // Ask the bridge once per run: it restores a stored (encrypted) login and reports whether it is still valid.
            if (!sessionRestoreStarted) { sessionRestoreStarted = true; NeteaseLocalBridge.ForProvider("netease").Send("status", null, _ => { }); }
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
            // Entries arrive one after another only when the page itself changes, not on every playback refresh or keystroke.
            string arrivalKey = page + ":" + selected?.id;
            arriving = arrivalKey != renderedArrival; renderedArrival = arrivalKey;
            if (!keepCloud) RenderContent();
            arriving = false;
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
            var panel = Modal("随身听操作"); panel.AddToClassList("music-help-sheet");
            var body = new ScrollView(); body.style.flexShrink = 1; panel.Add(body);
            // Short rows instead of one wrapped paragraph: wrapped lines of the pixel font sit too close together to read.
            void Section(string heading, params (string key, string text)[] rows)
            {
                Text(body.contentContainer, heading, "music-help-heading");
                foreach (var row in rows)
                {
                    var line = Box(body.contentContainer, "music-help-row");
                    Text(line, row.key, "music-help-key"); Text(line, row.text, "music-help-text");
                }
            }
            Section("浏览与搜索",
                ("搜索", "顶部搜索同时查曲库和所选在线来源"),
                ("来源", "切换来源标签即可换平台"),
                ("Ctrl+F", "跳到搜索框"),
                ("菜单键", "顶栏菜单键收起 / 展开侧栏"));
            Section("播放",
                ("播放", "点击三角按钮，停留可看按钮名称"),
                ("空格", "播放 / 暂停"),
                ("← →", "前进 / 后退 5 秒"),
                ("队列", "更多菜单追加或设为下一首"),
                ("排序", "拖动六点手柄或点上下箭头"));
            Section("歌词与字幕",
                ("L", "打开歌词 / 字幕"),
                ("格式", "LRC、增强 LRC、SRT、WebVTT"),
                ("逐词", "含 word/start/end/line_index 的 JSON"),
                ("跟随", "手动滚动后，点“跟随当前句”恢复"),
                ("Esc", "关闭当前弹层"));
            Text(body.contentContainer, "按钮或滑块获得焦点时，空格和方向键由控件自己处理。", "music-help-note");
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
            foreach (string item in new[] { "首页", "原声", "本地音乐", "收藏", "云端" })
            {
                string target = item;
                var b = Button(navigation, item, () => Navigate(target), "music-nav-" + target);
                b.EnableInClassList("is-active", page == item);
            }
            Text(navigation, "私人磁带 / 歌单", "music-eyebrow");
            var lists = new ScrollView(); lists.AddToClassList("music-playlists"); navigation.Add(lists);
            foreach (var list in MusicLibrarySession.State.playlists)
            {
                var b = Button(lists, list.title, () => Navigate("歌单", list));
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
                else if (page == "在线歌单") items = (onlineIds ?? new List<string>()).Select(MusicLibrarySession.Find).Where(t => t != null);
                else items = items.Where(t => !t.local && string.IsNullOrEmpty(t.provider));
            }
            else items = items.Where(t => ((t.title ?? "") + " " + (t.artist ?? "") + " " + t.category).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            return items.ToList();
        }
        void RenderContent()
        {
            ++catalogGeneration;
            content.Clear(); indicators.Clear();
            if (query.Length > 0) { RenderSearch(); return; }
            if (page == "首页") { RenderHome(); return; }
            if (page == "搜索") { RenderSearch(); return; }
            if (page == "云端" && query.Length == 0) { RenderCloud(); return; }
            string heading = query.Length > 0 ? "搜索结果" : page == "在线歌单" ? onlineTitle : selected != null ? selected.title : page;
            var hero = Box(content, "music-library-heading");
            hero.Add(new VisualElement { pickingMode = PickingMode.Ignore, name = "music-route-engraving" });
            var sleeve = Cover(hero, query.Length > 0 ? "railway_track" : "platform", "BGM", 88);
            sleeve.AddToClassList("music-hero-cover");
            var headingText = Box(hero, "music-heading-text");
            Text(headingText, page == "在线歌单" ? MusicCatalogSearch.Label(onlineSource) + " · 歌单" : "沿线声音档案  /  SOUND ARCHIVE", "music-eyebrow");
            sectionTitle = Text(headingText, heading, "music-section-title");
            var items = VisibleTracks();
            Text(headingText, query.Length > 0 ? $"已收录曲库 · {items.Count} 个结果" : page == "在线歌单" ? (onlineLoading ? "正在读取曲目…" : onlineError ?? MusicCatalogSearch.Label(onlineSource) + $" · {items.Count} 首") : page == "原声" ? "沿线原声 · 随旅途逐步解锁" : $"私人收藏 · {items.Count} 首", "music-muted");
            var actions = Box(content, "music-list-actions");
            Button(actions, "播放全部", () => PlayAll(items), "music-play-all");
            if (page == "在线歌单")
            {
                Button(actions, "刷新", () => OpenOnlinePlaylist(onlineSource, onlineId, onlineTitle), "music-online-refresh");
                Button(actions, "返回", () => Navigate("首页"), "music-online-back");
            }
            if (page == "本地音乐")
            {
                Button(actions, "导入音乐", () => LocalMusicFileImporter.PromptImportAudioFiles(count => {
                    MusicLibrarySession.RefreshLocal();
                    ShowToast(count > 0 ? $"已导入 {count} 个音频文件" : "未导入文件；支持 MP3 / WAV / OGG");
                }), "music-import");
                if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                    Button(actions, "自动搜索", StartLocalScan, "music-scan-local").tooltip = "在音乐、下载、桌面、文档等常用位置查找音频（后台进行，可随时停止）";
                Button(actions, manageMode ? "完成" : "管理", () => { manageMode = !manageMode; chosen.Clear(); Refresh(); }, "music-manage-local").tooltip = "选择多首本地音乐，批量移出随身听";
                Button moreLocal = null;
                moreLocal = Button(actions, "更多", () => {
                    var menu = MenuAt(moreLocal, "本地音乐");
                    PopoverItem(menu, "刷新文件", false, () => { CloseModal(); MusicLibrarySession.RefreshLocal(); });
                    PopoverItem(menu, "打开文件夹", false, () => { CloseModal(); LocalMusicFileImporter.OpenCustomMusicFolder(); });
                }, "music-more-local");
            }
            if (selected != null && query.Length == 0)
            {
                Button(actions, "重命名", () => EditName(selected));
                Button(actions, "删除歌单", () => Confirm("删除「" + selected.title + "」？音频文件和收藏会保留。", () => {
                    MusicLibrarySession.State.playlists.Remove(selected); selected = null; page = "原声"; MusicLibrarySession.Commit();
                }));
            }
            if (page == "本地音乐" && query.Length == 0 && manageMode) BuildManageBar(content, items);
            if (page == "本地音乐" && query.Length == 0) BuildScanPanel(content);
            var tableHead = Box(content, "music-table-head");
            Text(tableHead, "档案 / 曲目", "music-grow"); Text(tableHead, "时长", "music-duration"); Text(tableHead, "操作", "music-action-caption");
            var scroll = new ScrollView { name = "music-track-list" }; scroll.AddToClassList("music-track-list"); content.Add(scroll);
            if (items.Count == 0)
            {
                string emptyMessage = query.Length > 0 ? "没有找到曲目，试试曲名、作者或分类。" :
                    selected != null ? "歌单还是空的。从曲目的“更多”菜单加入歌曲。" :
                    page == "在线歌单" ? (onlineLoading ? "正在读取曲目…" : onlineError ?? "这个歌单里还没有曲目。") :
                    page == "本地音乐" ? "还没有本地音乐。点“自动搜索”扫描电脑，或“导入音乐”选择文件。" :
                    page == "收藏" ? "还没有收藏的曲目。点击曲目旁的心形图标即可收藏。" :
                    "这里还没有曲目。";
                Text(scroll, emptyMessage, "music-empty");
            }
            for (int i = 0; i < items.Count; i++)
            {
                var track = items[i];
                AddTrackRow(scroll.contentContainer, track, i, selected != null && query.Length == 0);
            }
        }
        void AddTrackRow(VisualElement parent, MusicLibraryTrack track, int i, bool reorder = false, bool resume = false)
        {
            var row = Box(parent, "music-track-row"); row.userData = track.id;
            Arrive(row, i);
            if (reorder) AddReorder(row, selected.tracks, i);
            if (manageMode && track.local)
            {
                string checkId = track.id;
                var box = new Button(() => { if (!chosen.Remove(checkId)) chosen.Add(checkId); Refresh(); }) { name = "music-check-" + checkId, tooltip = "选择" };
                box.AddToClassList("music-check"); box.EnableInClassList("is-checked", chosen.Contains(checkId));
                var mark = new VisualElement { pickingMode = PickingMode.Ignore }; mark.AddToClassList("music-check-mark"); box.Add(mark); row.Add(box);
            }
            var indicator = Text(row, (i + 1).ToString("D2"), "music-track-number"); indicators[track.id] = indicator;
            Cover(row, track.id, track.category, 42);
            var info = Box(row, "music-track-info");
            var titleBar = Box(info, "music-track-titlebar");
            var trackTitle = Text(titleBar, track.title, "music-track-title"); trackTitle.tooltip = string.IsNullOrEmpty(track.titleTranslation) ? track.title : track.title + " · " + track.titleTranslation;
            if (!string.IsNullOrEmpty(track.titleTranslation)) Text(titleBar, track.titleTranslation, "music-track-translation");
            bool unlocked = MusicLibrarySession.Unlocked(track);
            bool available = unlocked && MusicLibrarySession.Available(track);
            string status = !unlocked ? "未解锁 · " + track.condition : !available ? (!string.IsNullOrEmpty(track.provider) ? "在线曲目暂不可用" : "文件不可用 · 请检查或重新导入") : track.local ? (string.IsNullOrEmpty(track.artist) ? "私人磁带 · 本地音乐" : "私人磁带 · " + track.artist) : !string.IsNullOrEmpty(track.provider) ? track.category + " · " + (track.artist ?? "") : track.category + " · " + track.condition;
            Text(info, status, "music-track-subtitle").tooltip = status;
            row.EnableInClassList("is-unavailable", !available);
            Text(row, track.duration > 0 ? TabletMusicAppUI.FormatAudioTime(track.duration) : "--:--", "music-duration");
            var start = Button(row, "播放", () => MusicLibrarySession.Play(track.id, resume), "music-play-" + track.id); start.SetEnabled(available);
            Button(row, MusicLibrarySession.State.favorites.Contains(track.id) ? "已藏" : "收藏", () => MusicLibrarySession.ToggleFavorite(track.id), "music-favorite-" + track.id);
            Button moreButton = null;
            moreButton = Button(row, "更多", () => TrackActions(track, moreButton), "music-more-" + track.id);
            row.RegisterCallback<ClickEvent>(e => { if (e.clickCount == 2 && e.target is not UnityEngine.UIElements.Button) MusicLibrarySession.Play(track.id, resume); });
        }

        void Navigate(string target, MusicUserPlaylist playlist = null)
        {
#if MUSIC_RUNTIME_REVIEW
            Debug.Log("MUSIC_NAVIGATE " + target);
#endif
            page = target; selected = playlist; query = ""; search.SetValueWithoutNotify(""); clearSearch.style.display = DisplayStyle.None; Refresh();
        }
        void OpenSource(string source, bool account)
        {
            if (account) { cloudProvider = source; cloudLoggedIn = false; neteaseUserId = null; ++qrGeneration; Navigate("云端"); }
            else { requestedCloudPlaylist = null; searchProvider = source; Navigate("搜索"); search.Focus(); }
        }
        void RenderHome()
        {
            var scroll = new ScrollView { name = "music-track-list" }; scroll.AddToClassList("music-track-list"); content.Add(scroll);
            int hour = DateTime.Now.Hour;
            Text(scroll.contentContainer, (hour < 5 ? "夜深了" : hour < 11 ? "早上好" : hour < 14 ? "中午好" : hour < 18 ? "下午好" : "晚上好") + " · 雾峰站", "music-home-greeting");
            scroll.contentContainer.AddToClassList("music-home-content");
            var hero = Box(scroll.contentContainer, "music-listening-hero", "music-home");
            var last = MusicLibrarySession.Find(MusicLibrarySession.State.currentId);
            var lead = last ?? MusicLibrarySession.State.favorites.Select(MusicLibrarySession.Find).FirstOrDefault(t => t != null)
                ?? MusicLibrarySession.Tracks.FirstOrDefault(t => string.IsNullOrEmpty(t.provider) && !t.local && MusicLibrarySession.Unlocked(t));
            if (lead != null) Cover(hero, lead.id, lead.category, 92).AddToClassList("music-home-lead-cover");
            var intro = Box(hero, "music-home-lead-text");
            Text(intro, last != null ? "接着收听" : "沿线的声音", "music-eyebrow");
            Text(intro, lead?.title ?? "让旅途接着播放", "music-home-lead-title");
            if (lead != null && !string.IsNullOrEmpty(lead.titleTranslation)) Text(intro, lead.titleTranslation, "music-home-lead-translation");
            Text(intro, lead == null ? "原声 · 私人磁带 · 在线曲库" : string.IsNullOrEmpty(lead.artist) ? lead.category : lead.artist + " · " + lead.category, "music-muted");
            var actions = Box(intro, "music-list-actions", "music-home-sources"); actions.AddToClassList("music-home-actions");
            if (lead != null)
            {
                var start = Button(actions, last != null ? "继续播放" : "播放", () => MusicLibrarySession.Play(lead.id, last != null), "music-home-play"); start.AddToClassList("music-primary");
                Button(actions, "唱片档案", () => ShowDetail(lead.id));
            }
            // Only connected sources get a shortcut, sharing the hero's action row; the rest live behind "来源".
            foreach (string source in MusicCatalogSearch.Sources.Where(NeteaseLocalBridge.HasSession))
            {
                string id = source;
                var chip = Button(actions, "● " + MusicCatalogSearch.Label(id), () => OpenSource(id, true), "music-home-search-" + id);
                chip.AddToClassList("is-active"); chip.tooltip = MusicCatalogSearch.Label(id) + "：打开我的歌单";
            }
            var manage = Button(actions, "来源", () => OpenSource(cloudProvider, true), "music-home-manage-sources");
            manage.tooltip = "登录和管理网易云、QQ 音乐、酷狗、波点、Spotify、Subsonic、WebDAV";
            var tiles = Box(scroll.contentContainer, "music-quick-grid");
            QuickTile(tiles, "收藏", MusicLibrarySession.State.favorites.Count + " 首", () => Navigate("收藏"), "收藏");
            QuickTile(tiles, "本地音乐", MusicLibrarySession.Tracks.Count(t => t.local) + " 首", () => Navigate("本地音乐"), "本地音乐");
            QuickTile(tiles, "原声", MusicLibrarySession.Tracks.Count(t => !t.local && string.IsNullOrEmpty(t.provider) && MusicLibrarySession.Unlocked(t)) + " 首", () => Navigate("原声"), "原声");
            int linkedSources = MusicCatalogSearch.Sources.Count(NeteaseLocalBridge.HasSession);
            QuickTile(tiles, "云端", linkedSources > 0 ? linkedSources + " 个来源已连接" : "登录与搜索", () => OpenSource(cloudProvider, true), "云端");
            var favorites = MusicLibrarySession.State.favorites.Select(MusicLibrarySession.Find).Where(t => t != null).Take(6).ToList();
            if (favorites.Count > 0) HomeSongs(scroll.contentContainer, "你的收藏", favorites);
            if (MusicLibrarySession.State.playlists.Count > 0)
            {
                Text(scroll, "私人歌单", "music-section-title music-section-small");
                var grid = Box(scroll.contentContainer, "music-card-grid");
                foreach (var list in MusicLibrarySession.State.playlists.Take(6))
                {
                    var captured = list;
                    var artTrack = list.tracks.Select(MusicLibrarySession.Find).FirstOrDefault(t => t != null);
                    CollectionCard(grid, list.title, list.tracks.Count + " 首 · 本机", () => Navigate("歌单", captured), artTrack, "local-list:" + list.id, null);
                }
            }
            foreach (string source in MusicCatalogSearch.Sources.Where(NeteaseLocalBridge.HasSession)) HomeCloudLists(scroll.contentContainer, source);
            HomeSongs(scroll.contentContainer, "沿线原声", MusicLibrarySession.Tracks.Where(t => !t.local && string.IsNullOrEmpty(t.provider) && MusicLibrarySession.Unlocked(t)).Take(6).ToList());
            var online = MusicLibrarySession.Tracks.Where(t => !string.IsNullOrEmpty(t.provider)).Reverse().Take(6).ToList();
            if (online.Count > 0) HomeSongs(scroll.contentContainer, "已收录的在线曲目", online);
        }
        void QuickTile(VisualElement parent, string title, string subtitle, Action open, string icon)
        {
            var tile = Button(parent, "播放", open, "music-quick-" + title); tile.Clear(); tile.AddToClassList("music-quick-tile");
            Arrive(tile, parent.childCount - 1);
            var glyph = Box(tile, "music-quick-glyph");
            var mark = new Button { pickingMode = PickingMode.Ignore }; MusicTerminalIcons.Apply(mark, icon, true); mark.AddToClassList("music-quick-icon"); glyph.Add(mark);
            var text = Box(tile, "music-quick-text"); Text(text, title, "music-quick-title"); Text(text, subtitle, "music-quick-subtitle");
            tile.tooltip = title;
        }
        void StartLocalScan()
        {
            if (LocalMusicScanner.Current != null && LocalMusicScanner.Current.Running) { ShowToast("正在搜索本地音乐"); return; }
            LocalMusicScanner.Begin(LocalMusicScanner.DefaultRoots(), LocalMusicFileImporter.GetCustomMusicDirectory());
            Refresh();
        }
        void BuildScanPanel(VisualElement parent)
        {
            var scan = LocalMusicScanner.Current; if (scan == null) return;
            var panel = Box(parent, "music-scan-panel", "music-scan-panel");
            var status = Text(panel, "", "music-scan-status");
            var row = Box(panel, "music-scan-actions");
            var stop = Button(row, "停止", () => { scan.Cancel(); importCancel = true; }, "music-scan-stop");
            var import = Button(row, "导入找到的音乐", () => StartImportScan(scan), "music-scan-import"); import.AddToClassList("music-primary");
            var close = Button(row, "收起", () => { scan.Cancel(); LocalMusicScanner.Current = null; Refresh(); }, "music-scan-close");
            void Tick()
            {
                if (panel.panel == null) return;
                int found = scan.FoundCount;
                status.text = importing ? "正在导入 " + importDone + " / " + importTotal + " …"
                    : scan.Running ? "正在搜索 · 已查看 " + scan.Folders + " 个文件夹 · 找到 " + found + " 首（后台进行，可随时停止）"
                    : scan.Cancelled ? "已停止 · 找到 " + found + " 首新音乐" : "搜索完成 · 找到 " + found + " 首新音乐";
                stop.style.display = scan.Running || importing ? DisplayStyle.Flex : DisplayStyle.None;
                import.style.display = !scan.Running && !importing && found > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                close.style.display = !scan.Running && !importing ? DisplayStyle.Flex : DisplayStyle.None;
            }
            Tick(); panel.schedule.Execute(Tick).Every(300);
        }
        void StartImportScan(LocalMusicScanner scan)
        {
            var files = scan.Snapshot(); if (files.Count == 0 || importing) return;
            importing = true; importCancel = false; importDone = 0; importTotal = files.Count;
            int added = 0, index = 0;
            IVisualElementScheduledItem job = null;
            // A couple of files per tick keeps the window responsive; Stop takes effect at the next tick.
            job = root.schedule.Execute(() =>
            {
                if (root.panel == null) { importing = false; job.Pause(); return; }
                if (importCancel || index >= files.Count)
                {
                    job.Pause(); importing = false; MusicLibrarySession.RefreshLocal();
                    ShowToast(importCancel ? "已停止，导入了 " + added + " 首" : "已导入 " + added + " 首本地音乐");
                    if (!importCancel) LocalMusicScanner.Current = null;
                    Refresh(); return;
                }
                for (int n = 0; n < 2 && index < files.Count; n++) added += LocalMusicFileImporter.ImportAudioFiles(new[] { files[index++] });
                importDone = index;
            }).Every(30);
        }
        void ToggleNavigation()
        {
            MusicLibrarySession.State.navCollapsed = !MusicLibrarySession.State.navCollapsed;
            MusicLibrarySession.Save(); root.EnableInClassList("music-nav-collapsed", MusicLibrarySession.State.navCollapsed);
        }
        void HomeSongs(VisualElement parent, string heading, List<MusicLibraryTrack> tracks)
        {
            if (tracks.Count == 0) return;
            var bar = Box(parent, "music-section-bar"); Text(bar, heading, "music-section-title music-section-small");
            Button(Box(bar, "music-section-actions"), "播放全部", () => PlayAll(tracks));
            var grid = Box(parent, "music-card-grid");
            foreach (var track in tracks)
                CollectionCard(grid, track.title, string.IsNullOrEmpty(track.artist) ? track.category : track.artist, () => MusicLibrarySession.Play(track.id), track, track.id, null);
        }
        Button CollectionCard(VisualElement parent, string heading, string subtitle, Action open, MusicLibraryTrack track, string key, string cover)
        {
            var card = Button(parent, "播放", open); var glyph = card[0]; card.Clear(); card.AddToClassList("music-collection-card");
            Arrive(card, parent.childCount - 1);
            card.tooltip = heading; card.name = "music-card-" + key;
            var art = track != null ? Cover(card, track.id, track.category, 112) : PlaylistCover(card, key, cover, 112);
            if (track != null) { var mark = Box(art, "music-card-play"); mark.Add(glyph); }
            Text(card, heading, "music-card-title"); Text(card, subtitle, "music-card-subtitle"); return card;
        }
        VisualElement PlaylistCover(VisualElement parent, string key, string url, int size)
        {
            var sleeve = Box(parent, "music-sleeve"); sleeve.style.width = size; sleeve.style.height = size;
            var image = Box(sleeve, "music-cover-image"); image.style.backgroundImage = new StyleBackground(MusicAlbumArtResolver.GetAlbumCover(key, "歌单"));
            if (!string.IsNullOrEmpty(url)) MusicAlbumArtResolver.LoadRemoteImage(key, url, texture => { if (image.panel != null) image.style.backgroundImage = new StyleBackground(texture); });
            return sleeve;
        }
        void HomeCloudLists(VisualElement parent, string source)
        {
            var bridge = NeteaseLocalBridge.ForProvider(source); string key = source + ":" + bridge.SessionUserId;
            var bar = Box(parent, "music-section-bar"); Text(bar, MusicCatalogSearch.Label(source) + " · 我的歌单", "music-section-title music-section-small");
            var barActions = Box(bar, "music-section-actions");
            Button(barActions, "查看全部", () => OpenSource(source, true));
            Button(barActions, "刷新", () => { homeLists.Remove(key); Refresh(); });
            var grid = Box(parent, "music-card-grid");
            if (homeLists.TryGetValue(key, out var result))
            {
                if (!result.ok || result.result == null)
                {
                    Text(grid, "歌单暂时无法读取", "music-muted");
                    Button(grid, "重试", () => { homeLists.Remove(key); Refresh(); }); return;
                }
                var lists = result.result.playlists ?? Array.Empty<NeteaseLocalBridge.RemotePlaylist>();
                if (lists.Length == 0) Text(grid, "这里还没有歌单", "music-muted");
                foreach (var list in lists.Take(6)) CollectionCard(grid, list.title, list.count + " 首 · " + MusicCatalogSearch.Label(source), () => OpenOnlinePlaylist(source, list.id, list.title), null, "playlist:" + source + ":" + list.id, list.cover);
                return;
            }
            Text(grid, "正在读取歌单…", "music-muted");
            if (!homeListRequests.Add(key)) return;
            bridge.Send("playlists", new NeteaseLocalBridge.Parameters { userId = bridge.SessionUserId }, result => {
                homeListRequests.Remove(key); homeLists[key] = result;
                if (root.panel != null && page == "首页" && query.Length == 0 && NeteaseLocalBridge.HasSession(source)) Refresh();
            });
        }
        void RenderSearch()
        {
            int generation = catalogGeneration;
            string term = query, scope = searchProvider;
            Text(content, "搜索", "music-section-title");
            var tabs = Box(content, "music-list-actions"); tabs.AddToClassList("music-channel-selector");
            // "Smart" already includes the collected library; the library-only list is reached from "view all" and shows its own chip while active.
            foreach (string value in new[] { "auto" }.Concat(scope == "library" ? new[] { "library" } : new string[0]).Concat(MusicCatalogSearch.Sources))
            {
                string id = value;
                string caption = id == "auto" ? "智能" : id == "library" ? "已收录 · 全部" : MusicCatalogSearch.Label(id) + (NeteaseLocalBridge.HasSession(id) ? " · 已连接" : "");
                var tab = Button(tabs, caption, () => { searchProvider = id; RenderContent(); }, "music-search-source-" + id);
                tab.EnableInClassList("is-active", scope == id);
            }
            var scroll = new ScrollView { name = "music-track-list" }; scroll.AddToClassList("music-track-list"); content.Add(scroll);
            if (term.Length == 0) { Text(scroll, "在顶部输入曲名或作者；自动搜索曲库、网易云和本应用已登录的来源。", "music-empty"); return; }
            var matches = VisibleTracks();
            Text(scroll, "已收录曲库 / " + matches.Count + " 首", "music-eyebrow");
            int visible = scope == "library" ? matches.Count : Math.Min(12, matches.Count);
            for (int i = 0; i < visible; i++) AddTrackRow(scroll.contentContainer, matches[i], i);
            if (matches.Count == 0) Text(scroll, "已收录曲库没有匹配结果。", "music-muted");
            if (visible < matches.Count) Button(scroll, "查看全部已收录结果", () => { searchProvider = "library"; RenderContent(); });
            if (scope == "library") return;
            var sources = scope == "auto" ? new[] { "netease" }.Concat(MusicCatalogSearch.Sources.Where(NeteaseLocalBridge.HasSession)).Distinct().ToArray() : new[] { scope };
            if (scope == "auto") Text(scroll, "自动合并：曲库 + " + string.Join("、", sources.Select(MusicCatalogSearch.Label)), "music-muted");
            var seen = new HashSet<string>(matches.Take(visible).Select(t => t.id));
            foreach (string source in sources) RenderOnlineSearch(scroll.contentContainer, source, term, scope, generation, seen);
        }
        void RenderOnlineSearch(VisualElement parent, string source, string term, string scope, int generation, HashSet<string> seen)
        {
            var remote = Box(parent, "music-unified-results"); remote.name = "music-unified-results-" + source;
            Text(remote, MusicCatalogSearch.Label(source) + (NeteaseLocalBridge.HasSession(source) ? " / 使用已登录连接" : " / 在线搜索"), "music-eyebrow");
            var status = Text(remote, "正在搜索…", "music-muted"); status.name = "music-unified-search-status-" + source;
            var rows = new VisualElement { name = "music-unified-search-rows-" + source }; remote.Add(rows);
            void Run()
            {
                if (remote.panel == null || generation != catalogGeneration) return;
                status.text = "正在搜索 " + MusicCatalogSearch.Label(source) + "…";
                MusicCatalogSearch.Search(source, term, response => {
                    if (remote.panel == null || generation != catalogGeneration || query != term || searchProvider != scope) return;
                    rows.Clear();
                    if (!response.ok || response.result == null)
                    {
                        status.text = response.error ?? "在线搜索失败，请重试";
                        var actions = Box(rows, "music-list-actions");
                        Button(actions, "重试搜索", Run, "music-unified-retry-" + source);
                        Button(actions, "账户与连接", () => OpenSource(source, true)); return;
                    }
                    var found = response.result.tracks ?? Array.Empty<NeteaseLocalBridge.RemoteTrack>();
                    int count = 0;
                    foreach (var item in found)
                    {
                        var track = MusicLibrarySession.RegisterRemote(item, source);
                        if (track == null || !seen.Add(track.id)) continue;
                        AddTrackRow(rows, track, count++);
                    }
                    status.text = found.Length == 0 ? "没有找到在线曲目，试试其他关键词或来源。" : MusicCatalogSearch.Label(source) + " 返回 " + found.Length + " 首" + (count == 0 ? "，已在上方曲库显示。" : " · 播放以账号与版权为准");
                    MusicLibrarySession.Save();
                });
            }
            remote.schedule.Execute(Run).StartingIn(350);
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
        // Option lists open as a small popover beside the player controls, without dimming the page.
        VisualElement Popover(string heading)
        {
            if (overlay == null) modalReturnFocus = root.panel?.focusController.focusedElement as VisualElement;
            overlay?.RemoveFromHierarchy(); disc = null; lyrics = null;
            overlay = Box(root, "music-overlay"); overlay.AddToClassList("music-popover-overlay");
            var panel = Box(overlay, "music-popover");
            panel.AddToClassList("music-arriving"); panel.schedule.Execute(() => panel.RemoveFromClassList("music-arriving")).StartingIn(30);
            Text(panel, heading, "music-popover-title");
            overlay.RegisterCallback<PointerDownEvent>(e => { if (e.target == overlay) CloseModal(); });
            overlay.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) CloseModal(); });
            return panel;
        }
        static Button PopoverItem(VisualElement panel, string text, bool selected, Action action)
        {
            var item = new Button(action) { text = text }; item.AddToClassList("music-menu-item"); item.EnableInClassList("is-selected", selected); panel.Add(item); return item;
        }
        void CloseModal()
        {
            overlay?.RemoveFromHierarchy(); overlay = null; disc = null; detailId = null; lyrics = null;
            if (modalReturnFocus?.panel == root.panel && modalReturnFocus.enabledInHierarchy) modalReturnFocus.Focus();
            else root.Focus();
            modalReturnFocus = null;
        }
        bool arriving; string renderedArrival;
        bool importing, importCancel; int importDone, importTotal;
        bool manageMode; readonly HashSet<string> chosen = new HashSet<string>();
        string onlineSource, onlineId, onlineTitle, onlineError; List<string> onlineIds; bool onlineLoading; int onlineGeneration;
        // Folia-style arrival: entries come in 34 ms apart (at most nine steps) and covers are uncovered a little
        // later; with reduced motion they only fade in place.
        void Arrive(VisualElement element, int order)
        {
            if (!arriving) return;
            bool calm = MusicLibrarySession.State.reduceMotion;
            long delay = 40 + Math.Min(order, 9) * (calm ? 0 : 34);
            string hidden = calm ? "music-arrive-calm" : "music-arrive";
            element.AddToClassList(hidden); element.AddToClassList("music-arrive-anim");
            element.schedule.Execute(() => element.RemoveFromClassList(hidden)).StartingIn(delay);
            element.schedule.Execute(() => element.RemoveFromClassList("music-arrive-anim")).StartingIn(delay + 800);
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
        void EditName(MusicUserPlaylist list, string thenAdd = null)
        {
            var panel = Modal(list == null ? "新建私人歌单" : "重命名歌单");
            var field = new TextField("名称") { value = list?.title ?? "", maxLength = 60 }; panel.Add(field); field.Focus();
            Button(panel, "保存", () => {
                if (string.IsNullOrWhiteSpace(field.value)) { ShowToast("请填写歌单名称"); return; }
                if (list == null) { selected = MusicLibrarySession.CreatePlaylist(field.value); if (thenAdd != null) MusicLibrarySession.AddToPlaylist(selected, thenAdd); }
                else { list.title = field.value.Trim(); selected = list; }
                page = "歌单"; CloseModal(); MusicLibrarySession.Commit();
            }, "music-save-playlist");
        }
        // Menus open under their button; deeper choices (playlists) replace the content instead of stacking dialogs.
        VisualElement MenuAt(VisualElement anchor, string heading)
        {
            if (overlay == null) modalReturnFocus = root.panel?.focusController.focusedElement as VisualElement;
            overlay?.RemoveFromHierarchy(); disc = null; lyrics = null;
            overlay = Box(root, "music-overlay"); overlay.AddToClassList("music-menu-overlay");
            var menu = Box(overlay, "music-menu"); menu.AddToClassList("music-popover"); menu.style.position = Position.Absolute;
            if (!string.IsNullOrEmpty(heading)) Text(menu, heading, "music-popover-title");
            overlay.RegisterCallback<PointerDownEvent>(e => { if (e.target == overlay) CloseModal(); });
            overlay.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) CloseModal(); });
            menu.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                var area = root.worldBound; float width = menu.resolvedStyle.width, height = menu.resolvedStyle.height;
                if (float.IsNaN(width) || float.IsNaN(height) || area.width <= 0) return;
                float left, top;
                if (anchor != null && anchor.panel != null)
                {
                    var at = anchor.worldBound; left = at.xMax - width - area.x; top = at.yMax - area.y + 4;
                    if (top + height > area.height - 8) top = at.y - area.y - height - 4;
                }
                else { left = (area.width - width) / 2; top = (area.height - height) / 2; }
                left = Mathf.Clamp(left, 8, Mathf.Max(8, area.width - width - 8)); top = Mathf.Clamp(top, 8, Mathf.Max(8, area.height - height - 8));
                if (Mathf.Abs(menu.resolvedStyle.left - left) > .5f) menu.style.left = left;
                if (Mathf.Abs(menu.resolvedStyle.top - top) > .5f) menu.style.top = top;
            });
            return menu;
        }
        void TrackActions(MusicLibraryTrack track, VisualElement anchor = null)
        {
            var menu = MenuAt(anchor, track.title);
            void Main()
            {
                menu.Clear(); Text(menu, track.title, "music-popover-title");
                PopoverItem(menu, "查看唱片档案", false, () => ShowDetail(track.id));
                PopoverItem(menu, "下一首播放", false, () => { MusicLibrarySession.Enqueue(track.id, true); CloseModal(); ShowToast("已放到队列最前"); });
                PopoverItem(menu, "加入播放队列", false, () => { MusicLibrarySession.Enqueue(track.id); CloseModal(); ShowToast("已加入播放队列"); });
                PopoverItem(menu, "加入歌单…", false, Playlists).name = "music-menu-playlists";
                if (selected != null && query.Length == 0)
                    PopoverItem(menu, "从当前歌单移除", false, () => { selected.tracks.Remove(track.id); CloseModal(); MusicLibrarySession.Commit(); });
                if (track.local)
                {
                    Box(menu, "music-menu-divider");
                    PopoverItem(menu, "从随身听移出…", false, () => { CloseModal(); ConfirmDelete(new List<string> { track.id }); }).AddToClassList("music-menu-danger");
                }
            }
            void Playlists()
            {
                menu.Clear(); Text(menu, "加入歌单", "music-popover-title");
                foreach (var list in MusicLibrarySession.State.playlists)
                {
                    var target = list;
                    PopoverItem(menu, target.title, false, () => {
                        bool added = MusicLibrarySession.AddToPlaylist(target, track.id); CloseModal(); ShowToast(added ? "已加入歌单" : "歌单中已有这首曲目");
                    });
                }
                PopoverItem(menu, "新建歌单并加入", false, () => { CloseModal(); EditName(null, track.id); });
                Box(menu, "music-menu-divider");
                PopoverItem(menu, "返回", false, Main);
            }
            Main();
        }
        void ConfirmDelete(List<string> ids)
        {
            if (ids.Count == 0) return;
            Confirm(ids.Count == 1 ? "把这首从随身听移出？只清理随身听里的这份拷贝（放进回收站，可恢复），你电脑上原来的文件不受影响。"
                : "把所选 " + ids.Count + " 首从随身听移出？只清理随身听里的拷贝（放进回收站，可恢复），你电脑上原来的文件不受影响。", () => {
                int removed = MusicLibrarySession.RemoveLocalTracks(ids, true, out int kept);
                foreach (string id in ids) chosen.Remove(id);
                ShowToast(kept > 0 ? "已移出 " + removed + " 首；" + kept + " 首无法处理，已保留" : "已从随身听移出 " + removed + " 首");
                Refresh();
            });
        }
        void BuildManageBar(VisualElement parent, List<MusicLibraryTrack> items)
        {
            var bar = Box(parent, "music-manage-bar", "music-manage-bar");
            Text(bar, chosen.Count == 0 ? "点击曲目前的方框来选择" : "已选择 " + chosen.Count + " 首", "music-manage-count");
            var row = Box(bar, "music-manage-actions");
            Button(row, "全选", () => { foreach (var t in items) if (t.local) chosen.Add(t.id); Refresh(); }, "music-manage-all");
            Button(row, "清除", () => { chosen.Clear(); Refresh(); }, "music-manage-clear");
            var delete = Button(row, "移出所选", () => ConfirmDelete(new List<string>(chosen)), "music-manage-delete");
            delete.AddToClassList("music-danger"); delete.SetEnabled(chosen.Count > 0);
        }
        // A playlist from the home page opens its own page with the tracks; playing is a choice made there.
        void OpenOnlinePlaylist(string source, string id, string title)
        {
            onlineSource = source; onlineId = id; onlineTitle = title; onlineIds = null; onlineError = null; onlineLoading = true;
            int generation = ++onlineGeneration;
            Navigate("在线歌单");
            NeteaseLocalBridge.ForProvider(source).Send("playlist_tracks", new NeteaseLocalBridge.Parameters { id = id }, result => {
                if (generation != onlineGeneration) return;
                onlineLoading = false;
                if (!result.ok || result.result == null || result.result.tracks == null) onlineError = result.error ?? "歌单暂时无法读取";
                else
                {
                    onlineIds = new List<string>();
                    foreach (var remote in result.result.tracks) { var track = MusicLibrarySession.RegisterRemote(remote, source); if (track != null) onlineIds.Add(track.id); }
                }
                if (root != null && root.panel != null && page == "在线歌单") Refresh();
            });
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
            MusicAudioEffectsEngine.SetPlaybackSpeed(MusicLibrarySession.State.playbackSpeed <= 0 ? 1f : MusicLibrarySession.State.playbackSpeed);
            var speed = new Button { name = "music-speed" }; speed.AddToClassList("music-speed-chip");
            void ShowSpeed() { speed.text = MusicAudioEffectsEngine.PlaybackSpeed.ToString("0.##") + "x"; speed.EnableInClassList("is-active", Mathf.Abs(MusicAudioEffectsEngine.PlaybackSpeed - 1f) > 0.01f); }
            speed.tooltip = "播放速度（同时改变音高）";
            speed.clicked += () => {
                var panel = Popover("播放速度 · 音高随速度变化");
                foreach (float option in new[] { 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f })
                {
                    float chosen = option;
                    PopoverItem(panel, chosen.ToString("0.##") + "x" + (chosen == 1f ? "  正常" : ""), Mathf.Abs(MusicAudioEffectsEngine.PlaybackSpeed - chosen) < 0.01f, () => {
                        MusicAudioEffectsEngine.SetPlaybackSpeed(chosen); MusicLibrarySession.State.playbackSpeed = chosen; MusicLibrarySession.Save(); ShowSpeed(); CloseModal();
                    });
                }
            };
            effects.Add(speed); ShowSpeed();
            Button(effects, "音色", () => {
                var panel = Popover("收听音色");
                foreach (var pair in new[] { ("纯净原声", 0), ("工务电台 · 轻微抖晃", 1), ("旧收音机 · 温暖抖晃", 2) })
                    PopoverItem(panel, pair.Item1, (int)MusicAudioEffectsEngine.CurrentFilterMode == pair.Item2, () => { MusicAudioEffectsEngine.CurrentFilterMode = (MusicAudioEffectsEngine.RadioFilterMode)pair.Item2; CloseModal(); });
            });
            Button(effects, "定时", () => {
                var panel = Popover("睡眠定时");
                foreach (int minutes in new[] { 0, 15, 30, 45, 999 })
                    PopoverItem(panel, minutes == 0 ? "关闭定时" : minutes == 999 ? "播完本曲暂停" : minutes + " 分钟后暂停", false, () => {
                        MusicAudioEffectsEngine.SetSleepTimer((MusicAudioEffectsEngine.SleepTimerOption)minutes,
                            TabletMusicAppUI.tabletCurrentClip == null ? 0 : TabletMusicAppUI.tabletCurrentClip.length - TabletMusicAppUI.CurrentPosition);
                        CloseModal(); ShowToast("睡眠定时已更新");
                    });
            });
            var queueButton = Button(effects, "队列", () => { queueOpen = !queueOpen; RenderDrawer(); }, "music-queue");
            var queueBadgeLabel = new Label("") { name = "music-queue-badge", pickingMode = PickingMode.Ignore }; queueBadgeLabel.AddToClassList("music-badge"); queueButton.Add(queueBadgeLabel);
            Button(effects, "歌词 / 字幕", OpenLyrics, "music-lyrics");
        }
        void ShowDetail(string id)
        {
            var track = MusicLibrarySession.Find(id);
            if (track == null) { ShowToast("先选择一首曲目"); return; }
            bool online = !string.IsNullOrEmpty(track.provider);
            var panel = Modal("唱片档案"); panel.AddToClassList("music-record-sheet"); detailId = id;
            var body = new ScrollView { verticalScrollerVisibility = ScrollerVisibility.Hidden }; body.style.flexShrink = 1; panel.Add(body);
            var top = Box(body.contentContainer, "music-record-top");
            var art = Box(top, "music-detail-art");
            disc = MusicIconHelper.CreateVinylDiscLarge(170); disc.AddToClassList("music-detail-disc"); art.Add(disc);
            Cover(art, track.id, track.category, 180).AddToClassList("music-detail-cover");
            var information = new VisualElement { name = "music-record-information" }; information.AddToClassList("music-record-information"); top.Add(information);
            void Field(string key, string value) { var row = Box(information, "music-record-row"); Text(row, key, "music-record-key"); Text(row, value, "music-record-value"); }
            void Populate()
            {
                information.Clear();
                Text(information, track.title, "music-detail-title");
                if (!string.IsNullOrEmpty(track.titleTranslation)) Field("译名", track.titleTranslation);
                if (!string.IsNullOrWhiteSpace(track.artist)) Field("歌手", track.artist);
                if (!string.IsNullOrWhiteSpace(track.album)) Field("专辑", track.album);
                Field("来源", track.category);
                Field("时长", track.duration > 0 ? TabletMusicAppUI.FormatAudioTime(track.duration) : "--:--");
                if (track.local) Field("文件", track.fileName);
                else if (!online)
                {
                    if (!string.IsNullOrEmpty(track.condition)) Text(information, track.condition, "music-wrap");
                    if (!string.IsNullOrEmpty(track.description)) Text(information, track.description, "music-wrap");
                }
                if (online) Field("编号", track.remoteId);
            }
            Populate();
            var actions = Box(panel, "music-list-actions");
            Button(actions, "播放", () => MusicLibrarySession.Play(track.id)).SetEnabled(MusicLibrarySession.Available(track));
            Button(actions, MusicLibrarySession.State.favorites.Contains(id) ? "已藏" : "收藏", () => { MusicLibrarySession.ToggleFavorite(id); ShowDetail(id); });
            Button(actions, "下一首播放", () => { MusicLibrarySession.Enqueue(id, true); ShowToast("已放到队列最前"); });
            Button(actions, "加入播放队列", () => { MusicLibrarySession.Enqueue(id); ShowToast("已加入播放队列"); });
            Button(actions, "加入歌单", () => TrackActions(track));
            if (online)
            {
                Button(actions, "刷新曲目信息", () => {
                    MusicAlbumArtResolver.InvalidateRemote(id);
                    MusicLibrarySession.EnsureRemoteMetadata(id, ok => {
                        if (overlay != null && detailId == id) { Refresh(); ShowDetail(id); ShowToast(ok ? "曲目信息已更新" : "暂时无法更新，已保留现有信息"); }
                    }, true);
                }, "music-record-refresh");
                MusicLibrarySession.EnsureRemoteMetadata(id, _ => { if (information.panel != null && detailId == id) Populate(); });
            }
        }
        void RenderCloud()
        {
            Text(content, "在线曲库", "music-section-title");
            var scroll = new ScrollView { name = "music-cloud-scroll" }; scroll.style.flexGrow = 1; content.Add(scroll);
            var channels = Box(scroll.contentContainer, "music-list-actions");
            channels.AddToClassList("music-channel-selector");
            void AddChannel(string label, string id)
            {
                var button = Button(channels, label, () => { requestedCloudPlaylist = null; ++cloudRequestGeneration; cloudProvider = id; cloudLoggedIn = false; neteaseUserId = null; ++qrGeneration; RenderContent(); }, "music-cloud-source-" + id);
                button.EnableInClassList("is-active", cloudProvider == id);
            }
            AddChannel("网易云", "netease"); AddChannel("QQ 音乐", "qq"); AddChannel("酷狗", "kugou");
            AddChannel("波点", "bodian"); AddChannel("Subsonic", "subsonic"); AddChannel("WebDAV", "webdav"); AddChannel("Spotify", "spotify");
            string provider = cloudProvider;
            string providerName = provider == "qq" ? "QQ 音乐" : provider == "kugou" ? "酷狗" : provider == "bodian" ? "波点" : provider == "subsonic" ? "Subsonic" : provider == "webdav" ? "WebDAV" : provider == "spotify" ? "Spotify" : "网易云";
            var bridge = NeteaseLocalBridge.ForProvider(provider);
            cloudLoggedIn = NeteaseLocalBridge.HasSession(provider);
            neteaseUserId = cloudLoggedIn ? bridge.SessionUserId : null;
            Action readLibrary = null;
            var connection = new Foldout { text = "账户与连接", value = !cloudLoggedIn, name = "music-cloud-account" }; connection.AddToClassList("music-account-foldout");
            var controls = connection.contentContainer;
            var cloudStatus = Text(scroll, providerName + (cloudLoggedIn ? " / 已连接：" + bridge.SessionNickname : " / 尚未登录或连接"), "music-muted"); cloudStatus.name = "music-cloud-status";
            var qrImage = new Image { name = "music-cloud-qr", scaleMode = ScaleMode.ScaleToFit };
            qrImage.style.width = 150; qrImage.style.height = 150; qrImage.style.flexShrink = 0; qrImage.style.display = DisplayStyle.None;
            controls.Add(qrImage);
            qrImage.RegisterCallback<DetachFromPanelEvent>(_ => {
                if (qrImage.image is Texture2D texture) { qrImage.image = null; UnityEngine.Object.Destroy(texture); }
            });
            Action checkStatus = () => bridge.Send("status", null, reply => {
                if (root.panel == null || page != "云端" || cloudStatus.panel == null || provider != cloudProvider) return;
                cloudLoggedIn = reply.ok && reply.result != null && reply.result.loggedIn;
                neteaseUserId = cloudLoggedIn ? reply.result.userId : null;
                cloudStatus.text = reply.ok && reply.result != null && reply.result.loggedIn
                    ? providerName + (reply.result.sessionOnly ? " / 本机会话：" : " / 已登录：") + reply.result.nickname : providerName + " / " + (reply.ok ? "尚未登录" : reply.error);
                if (cloudLoggedIn) { connection.value = false; readLibrary?.Invoke(); }
            });
            Button(controls, "检查登录状态", checkStatus);
            Button qrButton = null;
            qrButton = Button(controls, "扫码登录", () => {
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
                        qrImage.schedule.Execute(() => { if (qrImage.panel != null) scroll.ScrollTo(connection); });
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
            if (provider == "spotify")
            {
                qrButton.style.display = DisplayStyle.None;
                Text(controls, "登录前须知：Spotify 仅向 Premium 账号提供完整播放，免费账号可以搜索和读取歌单，但无法播放。本入口为不发行的本机测试，使用 Spotify 桌面客户端标识，不是本项目注册的官方应用，账号使用风险由使用者自行承担，建议使用测试账号。", "music-wrap");
                var spotifyConfirm = Box(controls, "music-list-actions"); spotifyConfirm.style.display = DisplayStyle.None;
                Button spotifyLogin = null, spotifyProceed = null;
                spotifyLogin = Button(controls, "浏览器登录 Spotify", () => {
                    spotifyLogin.style.display = DisplayStyle.None; spotifyConfirm.style.display = DisplayStyle.Flex;
                    cloudStatus.text = "请确认已阅读上方须知：需要 Premium，且这是不发行的本机测试";
                }, "music-spotify-login");
                Button(spotifyConfirm, "取消", () => { spotifyConfirm.style.display = DisplayStyle.None; spotifyLogin.style.display = DisplayStyle.Flex; cloudStatus.text = "Spotify / 尚未登录"; }, "music-spotify-cancel");
                spotifyProceed = Button(spotifyConfirm, "我已了解，继续登录", () => {
                    spotifyConfirm.style.display = DisplayStyle.None; spotifyLogin.style.display = DisplayStyle.Flex;
                    int generation = ++qrGeneration;
                    spotifyLogin.SetEnabled(false); cloudStatus.text = "正在准备 Spotify 浏览器授权…";
                    bridge.Send("login_start", null, response => {
                        if (cloudStatus.panel == null || generation != qrGeneration) return;
                        spotifyLogin.SetEnabled(true);
                        if (!response.ok || response.result == null || !Uri.TryCreate(response.result.url, UriKind.Absolute, out var authorize) ||
                            authorize.Scheme != "https" || authorize.Host != "accounts.spotify.com" || authorize.AbsolutePath != "/authorize")
                        { cloudStatus.text = response.error ?? "Spotify 授权地址不可用"; return; }
                        Application.OpenURL(authorize.AbsoluteUri);
                        cloudStatus.text = "请在浏览器完成 Spotify 授权，再回到随身听（10 分钟内）";
                        Action poll = null;
                        poll = () => {
                            if (cloudStatus.panel == null || generation != qrGeneration) return;
                            bridge.Send("login_check", null, reply => {
                                if (cloudStatus.panel == null || generation != qrGeneration) return;
                                if (!reply.ok || reply.result == null) { cloudStatus.text = reply.error ?? "Spotify 登录失败，请重试"; return; }
                                if (reply.result.code == 803) checkStatus();
                                else if (reply.result.code == 800) cloudStatus.text = "Spotify 授权已过期，请重新登录";
                                else root.schedule.Execute(poll).StartingIn(2500);
                            });
                        };
                        root.schedule.Execute(poll).StartingIn(2500);
                    });
                }, "music-spotify-proceed"); spotifyProceed.AddToClassList("music-primary");
                Text(controls, "浏览器授权后读取搜索、歌单和已点赞歌曲；播放需要 Premium。本轮登录仅在本次程序运行期间保留。", "music-wrap");
            }
            if (provider == "subsonic" || provider == "webdav")
            {
                qrButton.style.display = DisplayStyle.None;
                var hostField = new TextField("服务器 HTTPS 地址"); controls.Add(hostField);
                var userField = new TextField("用户名"); controls.Add(userField);
                var passwordField = new TextField("密码") { isPasswordField = true }; controls.Add(passwordField);
                Button(controls, "连接服务器", () => {
                    var parameters = new NeteaseLocalBridge.Parameters { host = hostField.value, username = userField.value, password = passwordField.value };
                    passwordField.value = "";
                    cloudStatus.text = "正在验证服务器…";
                    bridge.Send("connect", parameters, response => {
                        if (cloudStatus.panel == null || provider != cloudProvider) return;
                        cloudLoggedIn = response.ok && response.result != null && response.result.loggedIn;
                        cloudStatus.text = cloudLoggedIn ? providerName + " / 已连接：" + response.result.nickname : providerName + " / " + response.error;
                        if (cloudLoggedIn) { connection.value = false; readLibrary?.Invoke(); }
                    });
                });
                Text(controls, provider == "webdav" ? "搜索根目录；歌单入口显示直接子目录。密码仅在本次进程内。" : "密码仅保存在本次进程内，关闭程序后需重新连接。", "music-muted");
            }
            Text(controls, "来源说明", "music-eyebrow");
            Text(controls, provider != "netease" ? providerName + "接入为实验功能，扫码、歌单与播放尚需真实账号验证。" : "网易云曲目可通过搜索或歌单加入曲库；播放能力取决于账号与版权。", "music-wrap");
            var searchBar = Box(scroll.contentContainer, "music-cloud-search-bar");
            var cloudSearch = new TextField { maxLength = 200, name = "music-cloud-query" }; cloudSearch.label = "搜 " + providerName; searchBar.Add(cloudSearch);
            var cloudResults = new VisualElement { name = "music-cloud-results" }; cloudResults.AddToClassList("music-cloud-results");
            var seenTracks = new HashSet<string>();
            string selectedCollection = null;
            var displayedTracks = new List<MusicLibraryTrack>();
            Action<NeteaseLocalBridge.RemoteTrack[], bool> showTracks = (remoteTracks, append) => {
                if (cloudResults.panel == null) return;
                if (!append)
                {
                    cloudResults.Clear(); seenTracks.Clear(); displayedTracks.Clear();
                    var bar = Box(cloudResults, "music-section-bar"); Text(bar, selectedCollection ?? "搜索结果", "music-section-title music-section-small");
                    Button(bar, "播放全部", () => PlayAll(displayedTracks));
                    if (selectedCollection != null) Button(bar, "返回歌单", () => { selectedCollection = null; readLibrary?.Invoke(); });
                }
                var list = remoteTracks ?? Array.Empty<NeteaseLocalBridge.RemoteTrack>();
                if (list.Length == 0 && !append) Text(cloudResults, "没有找到曲目", "music-muted");
                foreach (var remote in list)
                {
                    var track = MusicLibrarySession.RegisterRemote(remote, provider);
                    if (track == null || !seenTracks.Add(track.id)) continue;
                    displayedTracks.Add(track);
                    AddTrackRow(cloudResults, track, displayedTracks.Count - 1);
                }
                MusicLibrarySession.Save();
            };
            Action searchCloud = () => {
                selectedCollection = null;
                int searchGeneration = ++cloudRequestGeneration;
                cloudStatus.text = "正在搜索 " + providerName + "…";
                bridge.Send("search", new NeteaseLocalBridge.Parameters { keywords = cloudSearch.value }, result => {
                    if (cloudResults.panel == null || searchGeneration != cloudRequestGeneration) return;
                    cloudStatus.text = result.ok ? providerName + " / 搜索完成" : providerName + " / " + result.error;
                    if (result.ok && result.result != null) showTracks(result.result.tracks, false);
                });
            };
            Button(searchBar, "搜索音乐", searchCloud, "music-cloud-search");
            readLibrary = () => {
                if (!cloudLoggedIn) { cloudStatus.text = provider == "webdav" || provider == "subsonic" ? "请先连接服务器" : provider == "spotify" ? "请先在浏览器登录 Spotify 并检查状态" : "请先扫码登录并检查状态"; return; }
                selectedCollection = null;
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
                            Action openCollection = () => {
                                selectedCollection = list.title;
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
                            };
                            var grid = cloudResults.Q("music-cloud-playlists");
                            if (grid == null) grid = Box(cloudResults, "music-card-grid", "music-cloud-playlists");
                            CollectionCard(grid, list.title, list.count + " 首 · " + providerName, openCollection, null, "playlist:" + provider + ":" + list.id, list.cover);
                            if (requestedCloudPlaylist == list.id) { requestedCloudPlaylist = null; openCollection(); return; }
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
            };
            var libraryActions = Box(scroll.contentContainer, "music-list-actions");
            Button(libraryActions, provider == "webdav" ? "查看子目录" : "我的歌单", readLibrary, "music-cloud-library");
            Button(libraryActions, "账户与连接", () => { connection.value = !connection.value; if (connection.value) scroll.ScrollTo(connection); });
            if (cloudLoggedIn) scroll.schedule.Execute(() => { if (scroll.panel != null) readLibrary(); }).StartingIn(100);
            Button(controls, "退出登录", () => bridge.Send("logout", null, result => {
                if (cloudStatus.panel == null || provider != cloudProvider) return;
                ++qrGeneration; ++cloudRequestGeneration; qrImage.style.display = DisplayStyle.None; cloudLoggedIn = false; neteaseUserId = null; cloudResults.Clear(); connection.value = true; cloudStatus.text = result.ok ? providerName + " / 已退出" : providerName + " / " + result.error;
            }));
            scroll.Add(cloudResults);
            scroll.Add(connection);
            var otherSources = new Foldout { text = "其他来源", value = false }; scroll.Add(otherSources);
            Text(otherSources, "YouTube Music：需要符合平台要求的可见播放器。\nApple Music 和 SoundCloud：需要开发者应用凭据与用户授权。\nB 站：需要进一步核实开放平台权限和播放能力。", "music-wrap");
            foreach (var pair in new[] { ("Apple Music 歌单链接", "AppleMusic_Url"), ("YouTube Music 歌单链接", "Ytm_Playlist_Url"),
                ("SoundCloud 链接", "SoundCloud_Url") })
            {
                var field = new TextField(pair.Item1) { value = PlayerPrefs.GetString(pair.Item2, "") }; otherSources.Add(field);
                Button(otherSources, "保存配置", () => { PlayerPrefs.SetString(pair.Item2, field.value.Trim()); PlayerPrefs.Save(); ShowToast("配置已保存，尚未验证连接"); });
            }
        }
        static VisualElement Cover(VisualElement parent, string id, string category, int size)
        {
            var sleeve = Box(parent, "music-sleeve"); sleeve.style.width = size; sleeve.style.height = size;
            var image = Box(sleeve, "music-cover-image");
            image.style.backgroundImage = new StyleBackground(MusicAlbumArtResolver.GetAlbumCover(id, category));
            void ApplyCover() => MusicAlbumArtResolver.LoadRemoteCover(id, cover => { if (image.panel != null) image.style.backgroundImage = new StyleBackground(cover); });
            ApplyCover();
            return sleeve;
        }
        void Update()
        {
            if (root == null || play == null) return;
            root.EnableInClassList("music-reduce-motion", MusicLibrarySession.State.reduceMotion);
            root.EnableInClassList("music-nav-collapsed", MusicLibrarySession.State.navCollapsed);
            var queueBadge = root.Q<Label>("music-queue-badge");
            if (queueBadge != null)
            {
                int queued = MusicLibrarySession.State.queue.Count;
                queueBadge.text = queued > 99 ? "99+" : queued.ToString(); queueBadge.style.display = queued > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
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

