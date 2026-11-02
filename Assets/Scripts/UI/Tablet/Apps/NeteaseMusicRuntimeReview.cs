#if (DEVELOPMENT_BUILD || MUSIC_RUNTIME_REVIEW) && !UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEngine;
using RailwayRenaissance.UI.Tablet.Apps;

public sealed class NeteaseMusicRuntimeReview : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartReview()
    {
        var args = Environment.GetCommandLineArgs();
        if (!args.Contains("-netease-review") && !args.Contains("-music-open-review")) return;
        if (Application.companyName != "RailwayRenaissanceReview" || Application.productName != "NeteaseMusicReview")
        { Debug.LogError("NETEASE_REVIEW_WRONG_IDENTITY"); Application.Quit(2); return; }
        new GameObject("NeteaseMusicRuntimeReview").AddComponent<NeteaseMusicRuntimeReview>();
    }
    IEnumerator CheckPrivate(string source, string host, Action<bool> complete)
    {
        var bridge = NeteaseLocalBridge.ForProvider(source);
        bool done = false; NeteaseLocalBridge.Response reply = null;
        bridge.Send("connect", new NeteaseLocalBridge.Parameters { host = host, username = "review", password = "review" }, value => { reply = value; done = true; });
        float deadline = Time.realtimeSinceStartup + 25;
        while (!done && Time.realtimeSinceStartup < deadline) yield return null;
        if (!done || reply == null || !reply.ok) { complete(false); yield break; }
        done = false; reply = null;
        bridge.Send("search", new NeteaseLocalBridge.Parameters { keywords = "review" }, value => { reply = value; done = true; });
        deadline = Time.realtimeSinceStartup + 25;
        while (!done && Time.realtimeSinceStartup < deadline) yield return null;
        var remote = done && reply != null && reply.ok ? reply.result?.tracks?.FirstOrDefault() : null;
        var track = MusicLibrarySession.RegisterRemote(remote, source);
        if (track == null) { complete(false); yield break; }
        MusicLibrarySession.Play(track.id);
        deadline = Time.realtimeSinceStartup + 45;
        while (MusicLibrarySession.Loading && Time.realtimeSinceStartup < deadline) yield return null;
        complete(TabletMusicAppUI.tabletCurrentTrackId == track.id && TabletMusicAppUI.tabletIsPlaying && TabletMusicAppUI.tabletCurrentClip != null);
    }
    IEnumerator Start()
    {
        string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, Environment.GetCommandLineArgs().Contains("-music-open-review") ? "netease-review-state" : "automated-review-state");
        Directory.CreateDirectory(directory);
        MusicLibrarySession.UseReviewStorage(Path.Combine(directory, "library.json"));
        if (Environment.GetCommandLineArgs().Contains("-music-open-review")) MusicLibrarySession.SharedAuth = true;
        LocalMusicFileImporter.ReviewDirectory = Path.Combine(directory, "local");
        if (FindAnyObjectByType<AudioListener>() == null) new GameObject("NeteaseReviewListener").AddComponent<AudioListener>();
        if (Environment.GetCommandLineArgs().Contains("-music-open-review")) Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        GameMainUI.Show();
        var ui = GameMainUI.Instance;
        var hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(GameMainUI).GetMethod("EnsureDesktopBuiltSync", hidden).Invoke(ui, null);
        var doc = (UIDocument)typeof(GameMainUI).GetField("uiDoc", hidden).GetValue(ui);
        doc.rootVisualElement.Q("tablet-lock").style.display = DisplayStyle.None; ui.unlocked = true;
        typeof(GameMainUI).GetMethod("LaunchApp", hidden).Invoke(ui, new object[] { "音乐" });
        for (int i = 0; i < 30; i++) yield return null;
        var root = doc.rootVisualElement.Q("railway-music-terminal");
        if (Environment.GetCommandLineArgs().Contains("-music-open-review")) yield break;
        Screen.SetResolution(1440, 900, false);
        float resolutionEnd = Time.realtimeSinceStartup + 3;
        while ((Screen.width != 1440 || Screen.height != 900) && Time.realtimeSinceStartup < resolutionEnd) yield return null;
        yield return new WaitForSecondsRealtime(.5f);
        bool homeUi = root?.Q("music-home") != null && root.Q<Button>("music-home-manage-sources") != null;
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "home-1440.png"));
        var mainSearch = root?.Q<TextField>("music-search");
        if (mainSearch != null) mainSearch.value = "no-match-before-typing";
        // Change the term before debounce; the old request must not render later.
        if (mainSearch != null) mainSearch.value = "钢琴";
        float unifiedEnd = Time.realtimeSinceStartup + 50;
        while ((root?.Q("music-unified-search-rows-netease")?.Query<VisualElement>(className: "music-track-row").ToList().Count ?? 0) == 0 && Time.realtimeSinceStartup < unifiedEnd) yield return null;
        var searchError = root?.Q<Label>("music-unified-search-status-netease")?.text;
        if ((root?.Q("music-unified-search-rows-netease")?.Query<VisualElement>(className: "music-track-row").ToList().Count ?? 0) == 0)
        {
            Debug.Log("MUSIC_UNIFIED_FIRST_ATTEMPT " + searchError);
            var retry = root?.Q<Button>("music-unified-retry-netease");
            if (retry != null) using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = retry; retry.SendEvent(evt); }
            unifiedEnd = Time.realtimeSinceStartup + 50;
            while ((root?.Q("music-unified-search-rows-netease")?.Query<VisualElement>(className: "music-track-row").ToList().Count ?? 0) == 0 && Time.realtimeSinceStartup < unifiedEnd) yield return null;
        }
        bool unifiedUi = (root?.Q("music-unified-search-rows-netease")?.Query<VisualElement>(className: "music-track-row").ToList().Count ?? 0) > 0 && mainSearch.value == "钢琴";
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "unified-search-1440.png"));
        bool selectedLibrary = false;
        {
            var viewType = typeof(RailwayMusicTerminalView); var live = viewType.GetField("current", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            viewType.GetField("searchProvider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(live, "library");
            viewType.GetMethod("RenderContent", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(live, null);
        }
        for (int i = 0; i < 4; i++) yield return null;
        selectedLibrary = root?.Q("music-unified-results-netease") == null;
        if (mainSearch != null) mainSearch.value = "";
        Screen.SetResolution(1024, 768, false);
        resolutionEnd = Time.realtimeSinceStartup + 3;
        while ((Screen.width != 1024 || Screen.height != 768) && Time.realtimeSinceStartup < resolutionEnd) yield return null;
        yield return new WaitForSecondsRealtime(.5f);
        bool compactHome = Screen.width == 1024 && root?.Q("music-home") != null && root.Q("music-home-sources").worldBound.width <= root.worldBound.width;
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "home-1024.png"));
        Debug.Log("MUSIC_HOME_SEARCH_OK home=" + homeUi + " unified=" + unifiedUi + " libraryScope=" + selectedLibrary + " compact=" + compactHome);
        var cloud = root?.Q<Button>("music-nav-云端");
        Debug.Log("MUSIC_NAV_PROBE cloud=" + (cloud != null) + " panel=" + (root?.panel != null) + " size=" + Screen.width + "x" + Screen.height);
        if (cloud == null) Debug.Log("MUSIC_NAV_IDS " + string.Join(";", root.Query<Button>().ToList().Take(20).Select(b => b.name + "=" + b.tooltip)));
        if (cloud != null) using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = cloud; cloud.SendEvent(evt); }
        yield return new WaitForSecondsRealtime(.5f);
        var qrButton = root?.Q<Button>("music-cloud-qr-start");
        if (qrButton != null) using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = qrButton; qrButton.SendEvent(evt); }
        float qrEnd = Time.realtimeSinceStartup + 55;
        var qrImage = root?.Q<Image>("music-cloud-qr");
        var qrStatus = root?.Q<Label>("music-cloud-status");
        while (qrImage?.image == null && qrStatus?.text != null && qrStatus.text.StartsWith("正在生成二维码") && Time.realtimeSinceStartup < qrEnd) yield return null;
        for (int i = 0; i < 5; i++) yield return null;
        bool qrVisible = qrImage?.image != null && qrImage.resolvedStyle.display != DisplayStyle.None && qrImage.resolvedStyle.width > 0 && qrImage.resolvedStyle.height > 0;
        Debug.Log("NETEASE_QR_UI visible=" + qrVisible + " size=" + qrImage?.resolvedStyle.width + "x" + qrImage?.resolvedStyle.height + " status=" + qrStatus?.text);
        var field = root?.Q<TextField>("music-cloud-query");
        var searchButton = root?.Q<Button>("music-cloud-search");
        if (field != null) field.value = "钢琴";
        if (searchButton != null) using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = searchButton; searchButton.SendEvent(evt); }
        float uiEnd = Time.realtimeSinceStartup + 50;
        while (root?.Q("music-cloud-results")?.Query<VisualElement>(className: "music-track-row").ToList().Count == 0 && Time.realtimeSinceStartup < uiEnd) yield return null;
        bool cloudUi = root?.Q("music-cloud-results")?.Query<VisualElement>(className: "music-track-row").ToList().Count > 0;
        bool spotifyDone = false; NeteaseLocalBridge.Response spotifyReply = null;
        var spotifyBridge = NeteaseLocalBridge.ForProvider("spotify");
        spotifyBridge.Send("login_start", null, value => { spotifyReply = value; spotifyDone = true; });
        float spotifyEnd = Time.realtimeSinceStartup + 45;
        while (!spotifyDone && Time.realtimeSinceStartup < spotifyEnd) yield return null;
        bool spotifyStart = spotifyDone && spotifyReply != null && spotifyReply.ok && Uri.TryCreate(spotifyReply.result?.url, UriKind.Absolute, out var spotifyUrl) && spotifyUrl.Host == "accounts.spotify.com";
        spotifyDone = false;
        spotifyBridge.Send("logout", null, value => { spotifyDone = value.ok; });
        spotifyEnd = Time.realtimeSinceStartup + 15;
        while (!spotifyDone && Time.realtimeSinceStartup < spotifyEnd) yield return null;
        var spotifyRow = MusicLibrarySession.RegisterRemote(new NeteaseLocalBridge.RemoteTrack { id = "1234567890abcdefghijkl", title = "Spotify fixture", artist = "Review" }, "spotify");
        bool spotifySource = spotifyRow != null && spotifyRow.id == "cloud:spotify:1234567890abcdefghijkl";
        var spotifyTab = root?.Q<Button>("music-cloud-source-spotify");
        if (spotifyTab != null) using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = spotifyTab; spotifyTab.SendEvent(evt); }
        for (int i = 0; i < 5; i++) yield return null;
        bool spotifyUi = root?.Q<Button>("music-spotify-login") != null;
        Debug.Log("SPOTIFY_PLAYER_REVIEW loginStart=" + spotifyStart + " logout=" + spotifyDone + " sourceId=" + spotifySource + " ui=" + spotifyUi + " realAccount=not-tested");
        bool done = false; NeteaseLocalBridge.Response response = null;
        NeteaseLocalBridge.Instance.Send("search", new NeteaseLocalBridge.Parameters { keywords = "钢琴" }, result => { response = result; done = true; });
        float end = Time.realtimeSinceStartup + 50;
        while (!done && Time.realtimeSinceStartup < end) yield return null;
        bool search = done && response != null && response.ok && response.result?.tracks != null && response.result.tracks.Length > 0;
        var track = MusicLibrarySession.RegisterNetease(new NeteaseLocalBridge.RemoteTrack { id = "139774", title = "Review", artist = "Review" });
        if (track != null) MusicLibrarySession.Play(track.id);
        end = Time.realtimeSinceStartup + 65;
        while (MusicLibrarySession.Loading && Time.realtimeSinceStartup < end) yield return null;
        bool playback = track != null && TabletMusicAppUI.tabletCurrentTrackId == track.id && TabletMusicAppUI.tabletIsPlaying && TabletMusicAppUI.tabletCurrentClip != null;
        if (track != null) { MusicLibrarySession.ToggleFavorite(track.id); MusicLibrarySession.Enqueue(track.id); MusicLibrarySession.Save(); }
        bool stored = File.Exists(Path.Combine(directory, "library.json")) && File.ReadAllText(Path.Combine(directory, "library.json")).Contains("cloud:netease:139774");
        bool metadataDone = false, metadataOk = false;
        MusicLibrarySession.EnsureRemoteMetadata(track.id, ok => { metadataDone = true; metadataOk = ok; }, true);
        end = Time.realtimeSinceStartup + 45;
        while (!metadataDone && Time.realtimeSinceStartup < end) yield return null;
        bool metadataFields = metadataOk && !string.IsNullOrEmpty(track.artist) && !string.IsNullOrEmpty(track.album) && !string.IsNullOrEmpty(track.coverUrl);
        bool remoteCover = false;
        MusicAlbumArtResolver.LoadRemoteCover(track.id, texture => remoteCover = texture != null);
        end = Time.realtimeSinceStartup + 20;
        while (!remoteCover && Time.realtimeSinceStartup < end) yield return null;
        if (!remoteCover)
        {
            MusicAlbumArtResolver.InvalidateRemote(track.id);
            MusicAlbumArtResolver.LoadRemoteCover(track.id, texture => remoteCover = texture != null);
            end = Time.realtimeSinceStartup + 20;
            while (!remoteCover && Time.realtimeSinceStartup < end) yield return null;
        }
        typeof(RailwayMusicTerminalView).GetMethod("ShowDetail", hidden).Invoke(typeof(RailwayMusicTerminalView).GetField("current", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), new object[] { track.id });
        yield return new WaitForSecondsRealtime(.3f);
        var detailInfo = root?.Q("music-record-information");
        bool recordFields = detailInfo != null && detailInfo.Query<Label>().ToList().Any(label => label.text.Contains(track.album ?? "no-album")) && detailInfo.Query<Label>().ToList().Any(label => label.text.Contains(track.artist ?? "no-artist"));
        yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory, "record-details-1024.png"));
        typeof(RailwayMusicTerminalView).GetMethod("CloseModal", hidden).Invoke(typeof(RailwayMusicTerminalView).GetField("current", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), null);
        Debug.Log("MUSIC_METADATA_COVER_DETAIL metadata=" + metadataFields + " cover=" + remoteCover + " detail=" + recordFields);

        bool qqDone = false; NeteaseLocalBridge.Response qqResponse = null;
        NeteaseLocalBridge.ForProvider("qq").Send("qr_start", null, result => { qqResponse = result; qqDone = true; });
        end = Time.realtimeSinceStartup + 45;
        while (!qqDone && Time.realtimeSinceStartup < end) yield return null;
        bool qqQr = qqDone && qqResponse != null && qqResponse.ok && !string.IsNullOrEmpty(qqResponse.result?.image);
        Debug.Log("QQ_PLAYER_QR " + qqQr);
        bool kugouDone = false; NeteaseLocalBridge.Response kugouResponse = null;
        NeteaseLocalBridge.ForProvider("kugou").Send("search", new NeteaseLocalBridge.Parameters { keywords = "钢琴" }, result => { kugouResponse = result; kugouDone = true; });
        end = Time.realtimeSinceStartup + 45;
        while (!kugouDone && Time.realtimeSinceStartup < end) yield return null;
        bool kugouSearch = kugouDone && kugouResponse != null && kugouResponse.ok && kugouResponse.result?.tracks?.Length > 0;
        bool bodianDone = false; NeteaseLocalBridge.Response bodianResponse = null;
        NeteaseLocalBridge.ForProvider("bodian").Send("search", new NeteaseLocalBridge.Parameters { keywords = "钢琴" }, result => { bodianResponse = result; bodianDone = true; });
        end = Time.realtimeSinceStartup + 45;
        while (!bodianDone && Time.realtimeSinceStartup < end) yield return null;
        bool bodianSearch = bodianDone && bodianResponse != null && bodianResponse.ok && bodianResponse.result?.tracks?.Length > 0;
        var bodianTrack = bodianSearch ? MusicLibrarySession.RegisterRemote(bodianResponse.result.tracks[0], "bodian") : null;
        if (bodianTrack != null) MusicLibrarySession.Play(bodianTrack.id);
        end = Time.realtimeSinceStartup + 65;
        while (MusicLibrarySession.Loading && Time.realtimeSinceStartup < end) yield return null;
        bool bodianPlayback = bodianTrack != null && TabletMusicAppUI.tabletCurrentTrackId == bodianTrack.id && TabletMusicAppUI.tabletIsPlaying && TabletMusicAppUI.tabletCurrentClip != null;
        Debug.Log("OTHER_PLAYER_REVIEW kugouSearch=" + kugouSearch + " bodianSearch=" + bodianSearch + " bodianPlayback=" + bodianPlayback);
        string buildRoot = Directory.GetParent(Application.dataPath).FullName;
        string fixtureDir = Path.Combine(buildRoot, "review-fixtures");
        string node = Path.Combine(Application.streamingAssetsPath, "MusicService", "node.exe");
        var start = new System.Diagnostics.ProcessStartInfo { FileName = node,
            Arguments = "\"" + Path.Combine(fixtureDir, "private-services-fixture.js") + "\" \"" + Path.Combine(fixtureDir, "review.mp3") + "\"",
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        System.Diagnostics.Process fixture = null;
        try { fixture = System.Diagnostics.Process.Start(start); } catch (Exception) { }
        bool subsonic = false, webdav = false, automaticConnected = false, cloudWall = false, cloudCollection = false;
        if (fixture != null)
        {
            var portTask = fixture.StandardOutput.ReadLineAsync();
            end = Time.realtimeSinceStartup + 15;
            while (!portTask.IsCompleted && Time.realtimeSinceStartup < end) yield return null;
            if (portTask.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && int.TryParse(portTask.Result, out int port))
            {
                string host = "http://127.0.0.1:" + port;
                yield return CheckPrivate("subsonic", host, value => subsonic = value);
                yield return CheckPrivate("webdav", host + "/dav/", value => webdav = value);
                var homeButton = root?.Q<Button>("music-nav-首页");
                if (homeButton != null) using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = homeButton; homeButton.SendEvent(evt); }
                yield return new WaitForSecondsRealtime(.2f);
                var integratedSearch = root?.Q<TextField>("music-search");
                if (integratedSearch != null) integratedSearch.value = "review";
                var autoButton = root?.Q<Button>("music-search-source-auto");
                if (autoButton != null) using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = autoButton; autoButton.SendEvent(evt); }
                end = Time.realtimeSinceStartup + 15;
                while (Time.realtimeSinceStartup < end && !(root?.Q<Label>("music-unified-search-status-subsonic")?.text.Contains("返回") == true && root?.Q<Label>("music-unified-search-status-webdav")?.text.Contains("返回") == true)) yield return null;
                automaticConnected = NeteaseLocalBridge.HasSession("subsonic") && NeteaseLocalBridge.HasSession("webdav") && root?.Q<Label>("music-unified-search-status-subsonic")?.text.Contains("返回") == true && root?.Q<Label>("music-unified-search-status-webdav")?.text.Contains("返回") == true;
                Debug.Log("MUSIC_AUTOMATIC_CONNECTED_SOURCES " + automaticConnected + " (private fixture sessions only)");
                if (integratedSearch != null) integratedSearch.value = "";
                var sourceButton = root?.Q<Button>("music-home-search-subsonic");
                if (sourceButton != null) using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = sourceButton; sourceButton.SendEvent(evt); }
                end = Time.realtimeSinceStartup + 15;
                while ((root?.Q("music-cloud-playlists")?.Query<Button>().ToList().Count ?? 0) == 0 && Time.realtimeSinceStartup < end) yield return null;
                cloudWall = (root?.Q("music-cloud-playlists")?.Query<Button>().ToList().Count ?? 0) > 0 && root.Q<Foldout>("music-cloud-account")?.value == false;
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory, "cloud-playlists-1024.png"));
                var collection = root?.Q("music-cloud-playlists")?.Query<Button>().ToList().FirstOrDefault();
                if (collection != null) using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = collection; collection.SendEvent(evt); }
                end = Time.realtimeSinceStartup + 15;
                while ((root?.Q("music-cloud-results")?.Query<VisualElement>(className: "music-track-row").ToList().Count ?? 0) == 0 && Time.realtimeSinceStartup < end) yield return null;
                cloudCollection = (root?.Q("music-cloud-results")?.Query<VisualElement>(className: "music-track-row").ToList().Count ?? 0) > 0;
                Debug.Log("MUSIC_CLOUD_PLAYER_UI wall=" + cloudWall + " collection=" + cloudCollection + " fixture=True");


            }
            try { if (!fixture.HasExited) fixture.Kill(); } catch (Exception) { }
            fixture.Dispose();
        }
        Debug.Log("PRIVATE_PLAYER_REVIEW subsonic=" + subsonic + " webdav=" + webdav);
        Debug.Log("NETEASE_PLAYER_REVIEW ui=" + cloudUi + " search=" + search + " playback=" + playback + " persisted=" + stored);
        Debug.Log(metadataFields && remoteCover && recordFields && cloudWall && cloudCollection && automaticConnected && homeUi && unifiedUi && selectedLibrary && compactHome && qrVisible && spotifyStart && spotifyDone && spotifySource && spotifyUi && cloudUi && search && playback && stored && qqQr && kugouSearch && bodianSearch && bodianPlayback && subsonic && webdav ? "NETEASE_PLAYER_REVIEW_OK" : "NETEASE_PLAYER_REVIEW_FAILED");
        Application.Quit(metadataFields && remoteCover && recordFields && cloudWall && cloudCollection && automaticConnected && homeUi && unifiedUi && selectedLibrary && compactHome && qrVisible && spotifyStart && spotifyDone && spotifySource && spotifyUi && cloudUi && search && playback && stored && qqQr && kugouSearch && bodianSearch && bodianPlayback && subsonic && webdav ? 0 : 2);
    }
}
#endif
