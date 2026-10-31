#if DEVELOPMENT_BUILD && !UNITY_EDITOR
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
        string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "netease-review-state");
        Directory.CreateDirectory(directory);
        MusicLibrarySession.UseReviewStorage(Path.Combine(directory, "library.json"));
        LocalMusicFileImporter.ReviewDirectory = Path.Combine(directory, "local");
        if (FindAnyObjectByType<AudioListener>() == null) new GameObject("NeteaseReviewListener").AddComponent<AudioListener>();
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
        var cloud = root?.Query<Button>().ToList().FirstOrDefault(b => b.tooltip == "云端");
        if (cloud != null) using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = cloud; cloud.SendEvent(evt); }
        for (int i = 0; i < 10; i++) yield return null;
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
        bool subsonic = false, webdav = false;
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
            }
            try { if (!fixture.HasExited) fixture.Kill(); } catch (Exception) { }
            fixture.Dispose();
        }
        Debug.Log("PRIVATE_PLAYER_REVIEW subsonic=" + subsonic + " webdav=" + webdav);
        Debug.Log("NETEASE_PLAYER_REVIEW ui=" + cloudUi + " search=" + search + " playback=" + playback + " persisted=" + stored);
        Debug.Log(qrVisible && cloudUi && search && playback && stored && qqQr && kugouSearch && bodianSearch && bodianPlayback && subsonic && webdav ? "NETEASE_PLAYER_REVIEW_OK" : "NETEASE_PLAYER_REVIEW_FAILED");
        Application.Quit(qrVisible && cloudUi && search && playback && stored && qqQr && kugouSearch && bodianSearch && bodianPlayback && subsonic && webdav ? 0 : 2);
    }
}
#endif
