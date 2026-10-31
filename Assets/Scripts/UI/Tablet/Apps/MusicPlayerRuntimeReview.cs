#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;

// Opt-in isolated Windows Player review; normal players do not execute this path.
public sealed class MusicPlayerRuntimeReview : MonoBehaviour
{
    static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartReview()
    {
        if (!Environment.GetCommandLineArgs().Contains("-music-player-review")) return;
        if (Application.companyName != "RailwayRenaissanceReview" || Application.productName != "MusicTerminalReview20261028")
        { Debug.LogError("MUSIC_PLAYER_REVIEW_WRONG_IDENTITY"); Application.Quit(2); return; }
        new GameObject("MusicPlayerRuntimeReview").AddComponent<MusicPlayerRuntimeReview>();
    }
    static void Capture(RenderTexture target, string path)
    {
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(path, image.EncodeToPNG());
        Destroy(image);
    }
    IEnumerator Start()
    {
        string reviewRoot = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "review-state");
        Directory.CreateDirectory(reviewRoot);
        MusicLibrarySession.UseReviewStorage(Path.Combine(reviewRoot, "library-v1.json"));
        LocalMusicFileImporter.ReviewDirectory = Path.Combine(reviewRoot, "local");
        GameMainUI.Show();
        var ui = GameMainUI.Instance;
        if (ui == null) { Debug.LogError("MUSIC_PLAYER_REVIEW_NO_TABLET"); Application.Quit(2); yield break; }
        typeof(GameMainUI).GetMethod("EnsureDesktopBuiltSync", Hidden).Invoke(ui, null);
        var document = (UIDocument)typeof(GameMainUI).GetField("uiDoc", Hidden).GetValue(ui);
        document.rootVisualElement.Q("tablet-lock").style.display = DisplayStyle.None;
        ui.unlocked = true;
        typeof(GameMainUI).GetMethod("LaunchApp", Hidden).Invoke(ui, new object[] { "音乐" });
        for (int i = 0; i < 30; i++) yield return null;
        var root = document.rootVisualElement.Q("railway-music-terminal");
        if (root == null) { Debug.LogError("MUSIC_PLAYER_REVIEW_NO_TERMINAL"); Application.Quit(2); yield break; }
        bool restore = Environment.GetCommandLineArgs().Contains("-music-player-restore");
        var state = MusicLibrarySession.State;
        if (restore)
        {
            bool ok = state.currentId == "platform" && !TabletMusicAppUI.tabletIsPlaying && state.favorites.Contains("platform") && Math.Abs(state.volume - .42f) < .01f && state.position > 1 && state.mode == 2 && state.queue.Contains("iron_and_ash") && state.playlists.Any(p => p.title == "验收歌单" && p.tracks.Contains("platform"));
            float savedPosition = state.position;
            var playButton = root.Q<Button>("music-play-toggle");
            if (playButton == null) { Debug.LogError("MUSIC_PLAYER_RESUME_NO_BUTTON"); Application.Quit(2); yield break; }
            using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = playButton; playButton.SendEvent(evt); }
            for (int i = 0; i < 30; i++) yield return null;
            bool resumed = TabletMusicAppUI.tabletIsPlaying && TabletMusicAppUI.tabletCurrentTrackId == "platform" && TabletMusicAppUI.CurrentPosition >= savedPosition - .3f && TabletMusicAppUI.CurrentPosition < savedPosition + 5f;
            Debug.Log("MUSIC_PLAYER_RESTORE_DETAIL state=" + ok + " resumed=" + resumed + " saved=" + savedPosition + " now=" + TabletMusicAppUI.CurrentPosition);
            Debug.Log(ok && resumed ? "MUSIC_PLAYER_RESUME_OK" : "MUSIC_PLAYER_RESUME_FAILED");
            Application.Quit(ok && resumed ? 0 : 2); yield break;
        }
        var settings = Instantiate(document.panelSettings);
        settings.scaleMode = PanelScaleMode.ConstantPixelSize;
        settings.scale = 1;
        var target = new RenderTexture(1440, 900, 24);
        target.Create(); settings.targetTexture = target;
        settings.clearColor = true; settings.colorClearValue = new Color(.06f, .07f, .06f, 1);
        document.panelSettings = settings;
        typeof(GameMainUI).GetMethod("LaunchApp", Hidden).Invoke(ui, new object[] { "音乐" });
        for (int i = 0; i < 60; i++) yield return null;
        root = document.rootVisualElement.Q("railway-music-terminal");
        Capture(target, Path.Combine(reviewRoot, "player-library-1440.png"));
        var search = root.Q<TextField>("music-search");
        search.value = "Platform";
        bool searchOk = root.Query<VisualElement>(className: "music-track-row").ToList().Count == 1;
        search.value = "";
        MusicLibrarySession.Play("platform");
        for (int i = 0; i < 30; i++) yield return null;
        bool playbackOk = TabletMusicAppUI.tabletCurrentTrackId == "platform" && TabletMusicAppUI.tabletIsPlaying && TabletMusicAppUI.tabletCurrentClip != null;
        var detailButton = root.Q<Button>("music-detail");
        using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = detailButton; detailButton.SendEvent(evt); }
        for (int i = 0; i < 30; i++) yield return null;
        yield return new WaitForSecondsRealtime(.4f);
        Capture(target, Path.Combine(reviewRoot, "player-detail-1440.png"));
        using (var escape = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None)) { escape.target = root; root.SendEvent(escape); }
        settings.targetTexture = null; target.Release(); Destroy(target);
        target = new RenderTexture(1024, 768, 24); target.Create(); settings.targetTexture = target;
        for (int i = 0; i < 60; i++) yield return null;
        bool compactOk = root.worldBound.width > 900 && root.Q("music-player").worldBound.yMax <= root.worldBound.yMax + 1;
        var slider = root.Q<Slider>("music-volume");
        var rail = slider.Q(className: "unity-base-slider__tracker");
        var thumb = slider.Q(className: "unity-base-slider__dragger");
        compactOk = compactOk && Mathf.Abs(rail.worldBound.center.y - thumb.worldBound.center.y) < 1;
        Capture(target, Path.Combine(reviewRoot, "player-library-1024.png"));
        var queueButton = root.Q<Button>("music-queue");
        using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = queueButton; queueButton.SendEvent(evt); }
        for (int i = 0; i < 30; i++) yield return null;
        yield return new WaitForSecondsRealtime(.4f);
        Capture(target, Path.Combine(reviewRoot, "player-queue-1024.png"));
        Debug.Log("MUSIC_PLAYER_COMPACT_GEOMETRY " + compactOk);
        if (!state.favorites.Contains("platform")) MusicLibrarySession.ToggleFavorite("platform");
        var playlist = MusicLibrarySession.CreatePlaylist("验收歌单");
        bool playlistOk = playlist != null && MusicLibrarySession.AddToPlaylist(playlist, "platform") && !MusicLibrarySession.AddToPlaylist(playlist, "platform");
        MusicLibrarySession.Enqueue("iron_and_ash");
        bool queueOk = state.queue.Contains("iron_and_ash");
        TabletMusicAppUI.SetVolume(.42f);
        TabletMusicAppUI.SetPlayMode(2);
        if (TabletMusicAppUI.tabletCurrentClip != null) TabletMusicAppUI.SeekTabletMusic(10f / TabletMusicAppUI.tabletCurrentClip.length);
        if (TabletMusicAppUI.tabletIsPlaying) TabletMusicAppUI.ToggleTabletMusicPlayPause();
        MusicLibrarySession.Save();
        bool okAll = searchOk && playbackOk && playlistOk && queueOk && compactOk && !TabletMusicAppUI.tabletIsPlaying;
        Debug.Log("MUSIC_PLAYER_REVIEW_DETAIL search=" + searchOk + " playback=" + playbackOk + " playlist=" + playlistOk + " queue=" + queueOk + " path=" + reviewRoot);
        Debug.Log(okAll ? "MUSIC_PLAYER_REVIEW_OK" : "MUSIC_PLAYER_REVIEW_FAILED");
        Application.Quit(okAll ? 0 : 2);
    }
}
#endif
