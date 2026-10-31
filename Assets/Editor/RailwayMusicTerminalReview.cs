using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;

// Runs through GameMainUI.LaunchApp with a separate library and local audio directory.
[InitializeOnLoad]
public static class RailwayMusicTerminalReview
{
    const string Pending = "RailwayMusicReview.Pending";
    const string Restore = "RailwayMusicReview.Restore";
    const string Output = "tools/compile/music_terminal_review";
    static string Storage => Path.GetFullPath("Library/MusicTerminalReview/library.json");
    static string Local => Path.GetFullPath("Library/MusicTerminalReview/local");
    static int stage, frame, decodeCheck, queueBeforeDecodeSuccess;
    static double timeout;
    static UIDocument document;
    static VisualElement root;
    static RenderTexture target;
    static bool failed;
    static int restoreStep;
    static double checkAfter;
    static AudioClip previewPreviousClip;
    static float previewPreviousPosition;
    static Color previousStageColor;
    static float pausedStageClock, pausedStageEnergy;
    static int stageMotionFrame;
    static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static RailwayMusicTerminalReview()
    {
        if (SessionState.GetBool(Pending, false))
        {
            EditorApplication.update += Check;
            timeout = EditorApplication.timeSinceStartup + 150;
        }
    }
    public static void Batch() => Begin(false);
    public static void BatchRestore() => Begin(true);
    static void Begin(bool restore)
    {
        Directory.CreateDirectory(Output); Directory.CreateDirectory(Local);
        if (!restore)
        {
            ValidateStore();
            ValidateTimedText();
            if (File.Exists(Storage)) File.Delete(Storage);
            WriteWave(Path.Combine(Local, "沿线试音.wav"));
        }
        SessionState.SetBool(Pending, true); SessionState.SetBool(Restore, restore);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.update -= Check; EditorApplication.update += Check;
        timeout = EditorApplication.timeSinceStartup + 150;
        EditorApplication.isPlaying = true;
    }
    static void Assert(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
    }
    static void ValidateControlGeometry()
    {
        ValidateScrollerGeometry();
        foreach (string name in new[] { "music-volume", "music-progress" })
        {
            var slider = root.Q<Slider>(name);
            var track = slider.Q(className: "unity-base-slider__tracker");
            var thumb = slider.Q(className: "unity-base-slider__dragger");
            Assert(Mathf.Abs(track.worldBound.center.y - thumb.worldBound.center.y) < 1,
                name + " thumb is not centred on its rail");
        }
        var transport = root.Q(className: "music-transport");
        Assert(Mathf.Abs(transport.worldBound.center.x - root.Q("music-play-toggle").worldBound.center.x) < 1,
            "Primary play control is not centred in its group");
        Debug.Log("MUSIC_CONTROLS_GEOMETRY_OK: centred rails and symmetric transport at " + root.worldBound.width);
    }
    static void ValidateScrollerGeometry()
    {
        foreach (var scroll in root.Query<ScrollView>().ToList())
        {
            var bar = scroll.verticalScroller;
            if (bar.resolvedStyle.display == DisplayStyle.None || bar.worldBound.height < 2) continue;
            var rail = bar.Q(className: "unity-base-slider__tracker");
            var thumb = bar.Q(className: "unity-base-slider__dragger");
            Debug.Log("MUSIC_SCROLL_BOUNDS: " + scroll.name + " rail=" + rail.worldBound + " thumb=" + thumb.worldBound);
            Assert(Mathf.Abs(rail.worldBound.center.x - thumb.worldBound.center.x) < 1, "Vertical thumb is off centre: " + scroll.name);
            Assert(thumb.worldBound.yMin >= rail.worldBound.yMin - 1 && thumb.worldBound.yMax <= rail.worldBound.yMax + 1,
                "Vertical thumb exceeds rail: " + scroll.name);
            Debug.Log("MUSIC_SCROLL_DIAGNOSTIC: " + scroll.name + " rail=" + rail.worldBound + " thumb=" + thumb.worldBound);
        }
    }
    static void ValidateStore()
    {
        Assert(MusicSourceId.Local("Song.WAV") == "local:song.wav", "Legacy local identity changed");
        Assert(MusicSourceId.Online("NetEase", "42") == "cloud:netease:42", "Online identity was not source-qualified");
        Assert(MusicSourceId.Online("qq", "42") != MusicSourceId.Online("netease", "42"), "Different sources collide");
        bool rejected = false;
        try { MusicSourceId.Online("netease", "x:y"); } catch (ArgumentException) { rejected = true; }
        Assert(rejected, "Ambiguous online identity was accepted");
        Debug.Log("MUSIC_SOURCE_ID_OK: legacy local IDs and source-qualified online IDs");
        string importRoot = Path.GetFullPath(Path.Combine("Library/MusicTerminalReview", "import-" + Guid.NewGuid().ToString("N")));
        string previousDirectory = LocalMusicFileImporter.ReviewDirectory;
        try
        {
            string first = Path.Combine(importRoot, "a"), second = Path.Combine(importRoot, "b"), destination = Path.Combine(importRoot, "library");
            Directory.CreateDirectory(first); Directory.CreateDirectory(second);
            string a = Path.Combine(first, "same.wav"), b = Path.Combine(second, "same.wav");
            File.WriteAllText(a, "one"); File.WriteAllText(b, "two"); File.WriteAllText(Path.Combine(first, "same.lrc"), "first"); File.WriteAllText(Path.Combine(second, "same.lrc"), "second");
            File.WriteAllText(Path.Combine(first, "same.png"), "first cover"); File.WriteAllText(Path.Combine(second, "same.png"), "second cover");
            LocalMusicFileImporter.ReviewDirectory = destination;
            Assert(LocalMusicFileImporter.ImportAudioFiles(new[] { a, b, a }) == 2, "Distinct files were overwritten or duplicates imported");
            Assert(File.ReadAllText(Path.Combine(destination, "same.wav")) == "one" && File.ReadAllText(Path.Combine(destination, "same (2).wav")) == "two", "Same-name import replaced existing audio");
            Assert(File.ReadAllText(Path.Combine(destination, "same.lrc")) == "first" && File.ReadAllText(Path.Combine(destination, "same (2).lrc")) == "second", "Subtitle did not follow renamed audio");
            Assert(File.ReadAllText(Path.Combine(destination, "same.png")) == "first cover" && File.ReadAllText(Path.Combine(destination, "same (2).png")) == "second cover", "Cover did not follow renamed audio");
            Debug.Log("MUSIC_IMPORT_COLLISION_OK: existing audio, subtitles and covers preserved");
            var frames = new System.Collections.Generic.List<byte>();
            Action<string, string> addFrame = (id, text) => {
                byte[] encoded = System.Text.Encoding.UTF8.GetBytes(text);
                frames.AddRange(System.Text.Encoding.ASCII.GetBytes(id));
                int length = encoded.Length + 1;
                frames.AddRange(new byte[] { 0, 0, 0, (byte)length, 0, 0, 3 });
                frames.AddRange(encoded);
            };
            addFrame("TIT2", "沿线回声"); addFrame("TPE1", "铁路乐队");
            var image = new Texture2D(2, 2); image.SetPixel(0, 0, Color.red); image.Apply();
            byte[] png = image.EncodeToPNG(); UnityEngine.Object.DestroyImmediate(image);
            var picture = new System.Collections.Generic.List<byte> { 3 };
            picture.AddRange(System.Text.Encoding.ASCII.GetBytes("image/png")); picture.Add(0); picture.Add(3); picture.Add(0); picture.AddRange(png);
            frames.AddRange(System.Text.Encoding.ASCII.GetBytes("APIC"));
            int imageLength = picture.Count;
            frames.AddRange(new byte[] { (byte)(imageLength >> 24), (byte)(imageLength >> 16), (byte)(imageLength >> 8), (byte)imageLength, 0, 0 });
            frames.AddRange(picture);
            var id3 = new System.Collections.Generic.List<byte> { (byte)'I', (byte)'D', (byte)'3', 3, 0, 0,
                (byte)((frames.Count >> 21) & 127), (byte)((frames.Count >> 14) & 127), (byte)((frames.Count >> 7) & 127), (byte)(frames.Count & 127) };
            id3.AddRange(frames);
            string tagged = Path.Combine(first, "tagged.mp3"); File.WriteAllBytes(tagged, id3.ToArray());
            var tags = MusicLocalMetadata.Read(tagged);
            Assert(tags.title == "沿线回声" && tags.artist == "铁路乐队" && tags.cover != null && tags.cover.SequenceEqual(png), "ID3 title, artist or embedded image was not read");
            File.WriteAllBytes(tagged, new byte[] { 73, 68, 51, 3, 0, 0, 127, 127, 127, 127 });
            Assert(MusicLocalMetadata.Read(tagged).title == null, "Malformed ID3 did not safely fall back");
            Debug.Log("MUSIC_LOCAL_METADATA_OK: ID3 title/artist and malformed fallback");
        }
        finally { LocalMusicFileImporter.ReviewDirectory = previousDirectory; }
        string path = Path.GetFullPath("Library/MusicTerminalReview/store-check.json");
        var store = new MusicLibraryStore(path);
        var state = new MusicLibraryState { currentId = "platform", position = 12.5f, volume = .42f, mode = 2, lyricMotion = 2 };
        state.localTracks.Add(new MusicLibraryTrack { id = "local:test.wav", fileName = "test.wav", title = "test", local = true });
        state.cloudTracks.Add(new MusicLibraryTrack { id = "cloud:netease:139774", provider = "netease", remoteId = "139774", title = "Cloud test" });
        state.cloudTracks.Add(new MusicLibraryTrack { id = "cloud:qq:abc123", provider = "qq", remoteId = "abc123", title = "QQ test" });
        state.cloudTracks.Add(new MusicLibraryTrack { id = "cloud:kugou:0123456789abcdef0123456789abcdef~3~4", provider = "kugou", remoteId = "0123456789abcdef0123456789abcdef~3~4", title = "Kugou test" });
        state.cloudTracks.Add(new MusicLibraryTrack { id = "cloud:bodian:1234", provider = "bodian", remoteId = "1234", title = "Bodian test" });
        state.cloudTracks.Add(new MusicLibraryTrack { id = "cloud:subsonic:abcdef0123456789~c29uZw", provider = "subsonic", remoteId = "abcdef0123456789~c29uZw", title = "Subsonic test" });
        state.cloudTracks.Add(new MusicLibraryTrack { id = "cloud:webdav:abcdef0123456789~c29uZw", provider = "webdav", remoteId = "abcdef0123456789~c29uZw", title = "WebDAV test" });
        state.favorites.Add("platform"); state.queue.Add("local:test.wav");
        state.playlists.Add(new MusicUserPlaylist { title = "沿线收藏", tracks = { "platform", "local:test.wav" } });
        Assert(store.Save(state), "Initial store save failed");
        var loaded = store.Load();
        Assert(loaded.lyricMotion == 2, "Lyric motion preference was not restored");
        Assert(loaded.cloudTracks.Count == 6 && loaded.cloudTracks[0].id == "cloud:netease:139774" && loaded.cloudTracks[0].remoteId == "139774" && loaded.cloudTracks[1].id == "cloud:qq:abc123", "Cloud metadata round trip failed");
        Assert(!File.ReadAllText(path).Contains("MUSIC_U") && !File.ReadAllText(path).Contains("https://"), "Cloud credentials or temporary URL leaked into library");
        Debug.Log("MUSIC_CLOUD_STORE_OK: source metadata restored without credentials or temporary URLs");
        Assert(loaded.currentId == "platform" && loaded.position == 12.5f && loaded.volume == .42f && loaded.mode == 2 &&
            loaded.playlists[0].tracks.Count == 2 && loaded.queue[0] == "local:test.wav" && loaded.favorites[0] == "platform", "Store round trip lost data");
        Assert(store.Save(loaded) && File.Exists(path + ".bak"), "Atomic replacement/backup failed");
        File.WriteAllText(path, "{broken");
        var corrupt = new MusicLibraryStore(path); corrupt.Load();
        Assert(corrupt.ReadOnly && !corrupt.Save(state) && File.ReadAllText(path) == "{broken", "Unreadable store was overwritten");
        File.WriteAllText(path, "{\"version\":99}");
        var future = new MusicLibraryStore(path); future.Load();
        Assert(future.ReadOnly && !future.Save(state), "Future schema was overwritten");
        Debug.Log("MUSIC_STORE_OK: roundtrip, atomic backup, corrupt/future preservation");
    }
    static void PreviewReactive(string id, float seconds)
    {
        if (root.Q("music-lyrics-overlay") != null) Submit(root.Q<Button>("music-lyrics-close"));
        MusicAudioEffectsEngine.Pause();
        var source = MusicAudioEffectsEngine.GetActiveDeck();
        source.clip = Resources.Load<AudioClip>("bgm/" + id);
        source.Play(); source.Pause(); source.time = seconds;
        TabletMusicAppUI.tabletCurrentClip = source.clip;
        TabletMusicAppUI.tabletCurrentTrackId = id;
        TabletMusicAppUI.tabletCurrentTrackTitle = MusicLibrarySession.Find(id).title;
        TabletMusicAppUI.tabletIsPlaying = false;
        Submit(root.Q<Button>("music-lyrics"));
        Submit(root.Q<Button>("music-lyrics-focus"));
    }
    static void ValidateStageEnvelope(MusicReactiveStageView stage)
    {
        var clip = AudioClip.Create("Stage envelope review", 16000, 1, 8000, false);
        var pcm = new float[16000];
        for (int i = 8000; i < pcm.Length; i++) pcm[i] = .4f * Mathf.Sin(i * Mathf.PI * 2 * 200 / 8000);
        clip.SetData(pcm, 0);
        var go = new GameObject("StageEnvelopeReview"); var source = go.AddComponent<AudioSource>();
        source.clip = clip; source.volume = 0; source.Play(); source.Pause(); source.time = .2f;
        float silence = stage.ReadPower(source);
        source.time = 1.4f; float loud = stage.ReadPower(source);
        Assert(silence < .001f && loud > .2f && stage.UsedDecodedSamples, "Stage energy is not measured from actual PCM windows");
        UnityEngine.Object.Destroy(go); UnityEngine.Object.Destroy(clip);
    }
    static void ValidateTimedText()
    {
        const string wordJson = "[{\"word\":\"沿线\",\"start\":1,\"end\":1.5,\"line_index\":0},{\"word\":\"回声\",\"start\":2,\"end\":3,\"line_index\":0},{\"word\":\"归途\",\"start\":4,\"end\":5,\"line_index\":1}]";
        var jsonWords = MusicTimedText.Parse(wordJson, ".json");
        Assert(jsonWords.error == null && jsonWords.lines.Count == 2 && jsonWords.lines[0].words.Count == 2 &&
            jsonWords.lines[0].words[0].end == 1.5 && jsonWords.lines[0].words[1].start == 2 &&
            jsonWords.lines[0].text == "沿线 回声" && jsonWords.Active(3.5) == -1,
            "Native JSON word ends, internal silence or line gaps were lost");
        foreach (string invalid in new[] {
            "[{\"word\":\"x\",\"end\":2,\"line_index\":0}]",
            "[{\"word\":\"x\",\"start\":1,\"end\":2}]",
            wordJson.Replace("\"start\":2", "\"start\":1.4"),
            wordJson.Replace("\"line_index\":1", "\"line_index\":3"),
            "{\"words\":[]}", "[]" })
            Assert(MusicTimedText.Parse(invalid, ".json").error != null, "Malformed JSON word timing was accepted");
        string nativePath = "tools/music/alignment_review/lyrictimestamps/native/chollima_ride (标准).json";
        if (File.Exists(nativePath))
        {
            var native = MusicTimedText.Parse(File.ReadAllText(nativePath), ".json");
            Assert(native.error == null && native.lines.Count == 49 && native.lines.Sum(l => l.words.Count) == 97 &&
                Math.Abs(native.lines[0].words[0].end - 14.44) < .000001 &&
                Math.Abs(native.lines[0].words[1].start - 14.46) < .000001,
                "Actual downloaded JSON lost source word timing");
            Debug.Log("MUSIC_NATIVE_JSON_OK: actual 49-line/97-word export preserves explicit ends and silence; accuracy unverified");
        }
        var lrc = MusicTimedText.Parse("[offset:500]\n[00:01.25][00:03.125]沿线回声\n[00:05.00]", ".lrc");
        Assert(lrc.lines.Count == 3 && Math.Abs(lrc.lines[0].start - 1.75) < .0001 && lrc.Active(0) == -1 && lrc.Active(2) == 0, "LRC offset/multiple timestamp parsing failed");
        var enhanced = MusicTimedText.Parse("[00:01.00]<00:01.00>沿线<00:02.00>回声<00:04.00>", ".lrc");
        Assert(enhanced.lines[0].words.Count == 2 && enhanced.lines[0].words[0].end == 2 && enhanced.lines[0].end == 4, "Enhanced LRC word boundaries failed");
        var bilingual = MusicTimedText.Parse("[00:01.00]归途\n[00:01.00]Homeward\n[00:02.00]", ".lrc");
        Assert(bilingual.lines.Count == 2 && bilingual.lines[0].translation == "Homeward", "Same-timestamp translation failed");
        var vtt = MusicTimedText.Parse("WEBVTT\n\n00:00:01.000 --> 00:00:02.000 align:start\n<b>沿线</b>\n回声\n\n00:00:03.000 --> 00:00:04.000\n归途", ".vtt");
        Assert(vtt.lines.Count == 2 && vtt.lines[0].text == "沿线\n回声" && vtt.Active(2.5) == -1, "VTT cue gaps/markup/multiline failed");
        var srt = MusicTimedText.Parse("1\n00:00:01,250 --> 00:00:02,500\n沿线\n\n2\n00:00:04,000 --> 00:00:05,000\n归途", ".srt");
        Assert(srt.lines.Count == 2 && srt.lines[0].start == 1.25 && srt.Active(3) == -1, "SRT parsing failed");
        Assert(MusicTimedText.Parse("[00:99.00]invalid", ".lrc").lines.Count == 0 && MusicTimedText.Parse("not timed", ".lrc").error != null, "Malformed subtitles were accepted");
        Assert(MusicTimedText.SeekPosition(2, 100, 10) == 1.9 && MusicTimedText.SeekPosition(0, 100, 10) == 0 && MusicTimedText.SeekPosition(10, 0, 10) == null,
            "Lyric seek did not invert offset or reject end-of-media target");
        Debug.Log("MUSIC_TIMED_TEXT_OK: LRC/enhanced words/offset/translation, VTT/SRT/gaps, malformed and out-of-range seek");
    }
    static void Submit(Button button)
    {
        Assert(button != null && button.enabledInHierarchy, "Missing or disabled button");
        using var evt = NavigationSubmitEvent.GetPooled(); evt.target = button; button.SendEvent(evt);
    }
    static Button FindButton(string text) => root.Query<Button>().ToList().FirstOrDefault(b => b.tooltip == text);
    static void Setup()
    {
        if (UnityEngine.Object.FindAnyObjectByType<AudioListener>() == null)
            new GameObject("MusicReviewListener").AddComponent<AudioListener>();
        MusicLibrarySession.UseReviewStorage(Storage);
        LocalMusicFileImporter.ReviewDirectory = Local;
        foreach (string id in new[] { "south_wind", "starlit_rails", "chollima_ride", "sleepers" })
        {
            var sheet = MusicTimedText.Load(MusicLibrarySession.Find(id));
            if (id == "sleepers" || id == "south_wind")
            {
                Assert(sheet.lines.Count(line => !string.IsNullOrEmpty(line.text)) == (id == "sleepers" ? 35 : 39) && sheet.source.Contains("草稿"), "Sample sync draft or its review label is missing");
                Assert(sheet.lines.All(line => line.words.Count == 0), "Unverified word timing was published");
                continue;
            }
            Assert(!string.IsNullOrWhiteSpace(sheet.plainText) && sheet.lines.Count == 0 && sheet.Active(60) == -1,
                "Original untimed lyrics were missing or given invented timing: " + id);
            int authoredLines = id == "south_wind" ? 39 : id == "starlit_rails" ? 47 : 49;
            Assert(sheet.plainText.Split('\n').Length == authoredLines && sheet.source.Contains("提词原稿"),
                "Authored prompt lyrics did not reach runtime reading view: " + id);
            Assert(!sheet.plainText.Contains("[Verse") && !sheet.plainText.Contains("[Chorus"), "Generation cues leaked into lyric reading");
        }
        Debug.Log("MUSIC_OST_SHEETS_OK: four actual embedded lyric sheets, stable IDs, no fabricated timestamps");
        if (!SessionState.GetBool(Restore, false))
        {
            Assert(LocalMusicFileImporter.ImportAudioFiles(new[] { Path.Combine(Local, "沿线试音.wav"), Path.Combine(Local, "missing.wav"), Storage }) == 1,
                "File import did not accept valid audio and reject missing/unsupported files");
        }
        GameMainUI.Show(); Assert(GameMainUI.Instance != null, "Actual tablet entry failed");
        var ui = GameMainUI.Instance;
        typeof(GameMainUI).GetMethod("EnsureDesktopBuiltSync", Hidden).Invoke(ui, null);
        document = (UIDocument)typeof(GameMainUI).GetField("uiDoc", Hidden).GetValue(ui);
        document.rootVisualElement.Q("tablet-lock").style.display = DisplayStyle.None;
        ui.unlocked = true;
        typeof(GameMainUI).GetMethod("LaunchApp", Hidden).Invoke(ui, new object[] { "音乐" });
        root = document.rootVisualElement.Q("railway-music-terminal");
        Assert(root != null, "LaunchApp did not build the railway terminal");
        var settings = UnityEngine.Object.Instantiate(document.panelSettings);
        settings.scaleMode = PanelScaleMode.ConstantPixelSize; settings.scale = 1;
        target = new RenderTexture(1440, 900, 24); target.Create(); settings.targetTexture = target;
        settings.clearColor = true; settings.colorClearValue = new Color(.06f, .07f, .06f, 1);
        document.panelSettings = settings;
        // Panel replacement can detach/recreate the document; re-enter using the real app path.
        typeof(GameMainUI).GetMethod("LaunchApp", Hidden).Invoke(ui, new object[] { "音乐" });
        root = document.rootVisualElement.Q("railway-music-terminal");
        Assert(!TabletMusicAppUI.tabletIsPlaying, "Opening the window auto-started audio");
        Debug.Log("MUSIC_TABLET_ENTRY_OK: real GameMainUI app entry; no autoplay");
    }
    static void Capture(string name)
    {
        root.Q<Label>(className: "music-toast").style.display = DisplayStyle.None;
        var previous = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
        UnityEngine.Object.Destroy(image);
    }
    static void Check()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > timeout) throw new TimeoutException("Music runtime review timed out");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (stage > 0 && Time.frameCount - frame < 20) return;
            if (stage == 0) { Setup(); stage++; frame = Time.frameCount; return; }
            if (SessionState.GetBool(Restore, false)) { CheckRestore(); return; }
            if (EditorApplication.timeSinceStartup < checkAfter) return;
            switch (stage)
            {
                case 1:
                    Assert(root.worldBound.width > 1000 && root.Q("music-player").worldBound.height >= 100, "Runtime layout did not resolve");
                    ValidateControlGeometry();
                    Capture("01-library");
                    var search = root.Q<TextField>("music-search"); search.value = "Platform";
                    Assert(root.Query<VisualElement>(className: "music-track-row").ToList().Count == 1, "Cross-library search mismatch");
                    search.value = "no-match-451982";
                    Assert(root.Query<VisualElement>(className: "music-track-row").ToList().Count == 0, "Empty search did not clear rows");
                    search.value = "";
                    // Legacy preferences can seed the isolated store; make the empty case explicit.
                    MusicLibrarySession.State.favorites.Clear();
                    MusicLibrarySession.Commit();
                    Submit(FindButton("收藏"));
                    Assert(root.Q<Label>(className: "music-empty")?.text.Contains("心形图标") == true, "Empty favorites guidance is missing");
                    Submit(FindButton("原声"));
                    var localForEmptyCheck = MusicLibrarySession.Tracks.Where(t => t.local).ToList();
                    try
                    {
                        foreach (var track in localForEmptyCheck) track.local = false;
                        Submit(FindButton("本地音乐"));
                        Assert(root.Q<Label>(className: "music-empty")?.text.Contains("导入音乐") == true, "Empty local library guidance is missing");
                    }
                    finally { foreach (var track in localForEmptyCheck) track.local = true; }
                    Submit(FindButton("原声"));
                    Debug.Log("MUSIC_LOCAL_EMPTY_GUIDANCE_OK: import instruction shown");
                    var locked = MusicLibrarySession.Tracks.FirstOrDefault(t => !MusicLibrarySession.Unlocked(t));
                    if (locked != null) { string before = TabletMusicAppUI.tabletCurrentTrackId; MusicLibrarySession.Play(locked.id); Assert(TabletMusicAppUI.tabletCurrentTrackId == before, "Story lock was bypassed"); }
                    Submit(root.Q<Button>("music-new-playlist"));
                    root.Q(className: "music-modal").Q<TextField>().value = "雾峰沿线收藏";
                    Submit(root.Q<Button>("music-save-playlist"));
                    var list = MusicLibrarySession.State.playlists.Single();
                    Assert(root.Q<Label>(className: "music-empty")?.text.Contains("更多") == true, "Empty playlist guidance is missing");
                    Debug.Log("MUSIC_EMPTY_GUIDANCE_OK: favorites and playlist instructions");
                    Assert(MusicLibrarySession.AddToPlaylist(list, "platform") && !MusicLibrarySession.AddToPlaylist(list, "platform"), "Playlist duplicate guard failed");
                    MusicLibrarySession.AddToPlaylist(list, "iron_and_ash");
                    MusicLibrarySession.Move(list.tracks, 0, 1);
                    Assert(list.tracks[0] == "iron_and_ash", "Playlist reorder failed");
                    if (!MusicLibrarySession.State.favorites.Contains("platform")) MusicLibrarySession.ToggleFavorite("platform");
                    Assert(MusicLibrarySession.State.favorites.Contains("platform"), "Favorite toggle failed");
                    MusicLibrarySession.Enqueue("iron_and_ash"); MusicLibrarySession.Enqueue("platform", true);
                    Assert(MusicLibrarySession.State.queue[0] == "platform", "Play-next insertion failed");
                    MusicLibrarySession.Move(MusicLibrarySession.State.queue, 0, 1);
                    Assert(MusicLibrarySession.State.queue[0] == "iron_and_ash", "Queue reorder failed");
                    Submit(root.Q<Button>("music-queue"));
                    MusicLibrarySession.Play("platform");
                    Debug.Log("MUSIC_LIBRARY_ACTIONS_OK: search/empty/lock, UI-created playlist, duplicate guard, reorder, favorite, queue");
                    break;
                case 2:
                    Assert(TabletMusicAppUI.tabletIsPlaying && MusicAudioEffectsEngine.IsPlaying(), "OST did not start playing");
                    Submit(root.Q<Button>("music-play-toggle"));
                    Assert(!TabletMusicAppUI.tabletIsPlaying && !MusicAudioEffectsEngine.IsPlaying(), "Pause did not pause both decks");
                    TabletMusicAppUI.SeekTabletMusic(.2f);
                    Assert(Math.Abs(TabletMusicAppUI.CurrentPosition - TabletMusicAppUI.tabletCurrentClip.length * .2f) < .2, "Seek mismatch");
                    Submit(root.Q<Button>("music-play-toggle"));
                    Assert(TabletMusicAppUI.tabletIsPlaying, "Resume failed");
                    MusicLibrarySession.State.queue.Clear(); MusicLibrarySession.Enqueue("iron_and_ash");
                    TabletMusicAppUI.PlayNextTabletTrack();
                    Assert(TabletMusicAppUI.tabletCurrentTrackId == "iron_and_ash" && MusicAudioEffectsEngine.GetActiveDeck().clip == TabletMusicAppUI.tabletCurrentClip, "Crossfade deck targets the old song");
                    TabletMusicAppUI.PlayPrevTabletTrack();
                    Assert(TabletMusicAppUI.tabletCurrentTrackId == "platform", "Previous did not return to actual playback history");
                    TabletMusicAppUI.PlayNextTabletTrack();
                    Assert(TabletMusicAppUI.tabletCurrentTrackId == "iron_and_ash", "Next did not restore forward playback history");
                    TabletMusicAppUI.SeekTabletMusic(4f / TabletMusicAppUI.tabletCurrentClip.length); TabletMusicAppUI.PlayPrevTabletTrack();
                    Assert(TabletMusicAppUI.tabletCurrentTrackId == "iron_and_ash" && TabletMusicAppUI.CurrentPosition < .1f, "Previous after 3 seconds did not restart current track");
                    Capture("02-queue");
                    break;
                case 3:
                    Submit(root.Q<Button>("music-detail"));
                    Capture("03-record-details");
                    // Check a later frame after the detail modal has completed layout.
                    break;
                case 4:
                    Capture("03-record-details");
                    Submit(FindButton("关闭"));
                    float sleep = 100;
                    MusicAudioEffectsEngine.SetSleepTimer(MusicAudioEffectsEngine.SleepTimerOption.Min15);
                    MusicAudioEffectsEngine.RemainingSleepSeconds = sleep;
                    TabletMusicAppUI.UpdateAudioProgress(); float remaining = MusicAudioEffectsEngine.RemainingSleepSeconds;
                    TabletMusicAppUI.UpdateAudioProgress(); TabletMusicAppUI.UpdateAudioProgress();
                    Assert(MusicAudioEffectsEngine.RemainingSleepSeconds == remaining, "Repeated calls advanced the audio clock twice");
                    MusicAudioEffectsEngine.SetSleepTimer(MusicAudioEffectsEngine.SleepTimerOption.Off);
                    float position = TabletMusicAppUI.CurrentPosition;
                    Submit(root.Q<Button>("music-close"));
                    Assert(TabletMusicAppUI.tabletIsPlaying, "Closing the music window stopped playback");
                    typeof(GameMainUI).GetMethod("LaunchApp", Hidden).Invoke(GameMainUI.Instance, new object[] { "音乐" });
                    root = document.rootVisualElement.Q("railway-music-terminal");
                    Assert(TabletMusicAppUI.tabletIsPlaying && TabletMusicAppUI.CurrentPosition >= position - .1, "Reopening restarted playback");
                    Submit(FindButton("本地音乐"));
                    var local = MusicLibrarySession.Tracks.Single(t => t.local);
                    MusicLibrarySession.Play(local.id);
                    Debug.Log("MUSIC_PLAYBACK_OK: OST, pause/resume, seek, queue/crossfade, one tick per frame, window close/reopen");
                    break;
                case 5:
                    if (MusicLibrarySession.Loading) return;
                    if (decodeCheck == 0)
                    {
                        var badTrack = MusicLibrarySession.Tracks.Single(t => t.local);
                        string badPath = Path.Combine(Local, badTrack.fileName);
                        File.Move(badPath, badPath + ".decodebackup");
                        File.WriteAllText(badPath, "not a valid wave file");
                        MusicLibrarySession.Enqueue(badTrack.id, true);
                        MusicLibrarySession.PlayQueued(0);
                        decodeCheck = 1;
                        return;
                    }
                    if (decodeCheck == 1)
                    {
                        var badTrack = MusicLibrarySession.Tracks.Single(t => t.local);
                        string badPath = Path.Combine(Local, badTrack.fileName);
                        try
                        {
                            Assert(MusicLibrarySession.State.queue.Count > 0 && MusicLibrarySession.State.queue[0] == badTrack.id && !MusicLibrarySession.Available(badTrack), "Decode failure removed queue item or failed to mark it unavailable");
                            Debug.Log("MUSIC_BAD_DECODE_QUEUE_RETAIN_OK: actual corrupt WAV retained and marked unavailable");
                            MusicLibrarySession.State.queue.RemoveAt(0);
                        }
                        finally
                        {
                            File.Delete(badPath);
                            File.Move(badPath + ".decodebackup", badPath);
                            MusicLibrarySession.RefreshLocal();
                        }
                        queueBeforeDecodeSuccess = MusicLibrarySession.State.queue.Count;
                        MusicLibrarySession.Enqueue(badTrack.id, true);
                        MusicLibrarySession.PlayQueued(0);
                        decodeCheck = 2;
                        return;
                    }
                    if (decodeCheck == 2)
                    {
                        Assert(MusicLibrarySession.State.queue.Count == queueBeforeDecodeSuccess && TabletMusicAppUI.tabletCurrentTrackId.StartsWith("local:"), "Successful local queue playback did not consume exactly one entry");
                        Debug.Log("MUSIC_GOOD_DECODE_QUEUE_CONSUME_OK: actual WAV starts before queue entry is consumed");
                        var pendingTrack = MusicLibrarySession.Tracks.Single(t => t.local);
                        MusicLibrarySession.Enqueue(pendingTrack.id, true);
                        MusicLibrarySession.PlayQueued(0);
                        Assert(MusicLibrarySession.Loading, "Local track did not enter async loading");
                        MusicLibrarySession.Enqueue("platform");
                        decodeCheck = 3;
                        return;
                    }
                    if (decodeCheck == 3)
                    {
                        var pendingTrack = MusicLibrarySession.Tracks.Single(t => t.local);
                        Assert(MusicLibrarySession.State.queue.Count == queueBeforeDecodeSuccess + 2 && MusicLibrarySession.State.queue[0] == pendingTrack.id && MusicLibrarySession.State.queue[MusicLibrarySession.State.queue.Count - 1] == "platform", "Queue edited while loading was consumed");
                        MusicLibrarySession.State.queue.RemoveAt(0);
                        MusicLibrarySession.State.queue.RemoveAt(MusicLibrarySession.State.queue.Count - 1);
                        Debug.Log("MUSIC_QUEUE_EDIT_DURING_LOAD_OK: changed queue retained");
                        MusicLibrarySession.Enqueue(pendingTrack.id, true);
                        MusicLibrarySession.PlayQueued(0);
                        Assert(MusicLibrarySession.Loading, "Cancellation test did not enter loading");
                        MusicLibrarySession.CancelPending();
                        decodeCheck = 4;
                        return;
                    }
                    if (decodeCheck == 4)
                    {
                        var pendingTrack = MusicLibrarySession.Tracks.Single(t => t.local);
                        Assert(!MusicLibrarySession.Loading && MusicLibrarySession.State.queue.Count == queueBeforeDecodeSuccess + 1 && MusicLibrarySession.State.queue[0] == pendingTrack.id, "Cancelled load consumed queue item");
                        MusicLibrarySession.State.queue.RemoveAt(0);
                        MusicLibrarySession.Commit();
                        Debug.Log("MUSIC_QUEUE_CANCEL_LOAD_OK: cancelled entry retained");
                        decodeCheck = 5;
                    }
                    Assert(TabletMusicAppUI.tabletCurrentTrackId.StartsWith("local:") && TabletMusicAppUI.tabletCurrentClip != null, "Local WAV did not load");
                    var waveform = new float[256];
                    Assert(TabletMusicAppUI.tabletCurrentClip.GetData(waveform, 0) && waveform.Any(sample => Mathf.Abs(sample) > .00001f), "Local WAV decoded to empty PCM");
                    var output = new float[256]; TabletMusicAppUI.tabletMusicSource.GetOutputData(output, 0);
                    Debug.Log("MUSIC_LOCAL_PCM_OK: decoded tone has nonzero PCM; batch output samples observed=" + output.Any(sample => Mathf.Abs(sample) > .00001f) + "; hardware listening is not asserted");
                    TabletMusicAppUI.SetPlayMode(0);
                    TabletMusicAppUI.SeekTabletMusic(.3f);
                    TabletMusicAppUI.ToggleTabletMusicPlayPause();
                    MusicLibrarySession.Save();
                    var localTrack = MusicLibrarySession.Tracks.Single(t => t.local);
                    File.Move(Path.Combine(Local, localTrack.fileName), Path.Combine(Local, localTrack.fileName + ".missing"));
                    Assert(!MusicLibrarySession.Available(localTrack), "Missing local file was not detected");
                    MusicLibrarySession.Enqueue(localTrack.id, true); MusicLibrarySession.Enqueue("platform");
                    Submit(root.Q<Button>("music-queue"));
                    var missingItem = root.Query<VisualElement>(className: "music-queue-item").First();
                    Submit(missingItem.Query<Button>().ToList().First(b => b.tooltip == "播放"));
                    Assert(MusicLibrarySession.State.queue[0] == localTrack.id, "Playing a missing queue item removed it");
                    Debug.Log("MUSIC_MISSING_QUEUE_RETAIN_OK: unavailable item remains after direct play");
                    Submit(root.Q<Button>("music-queue"));
                    TabletMusicAppUI.PlayNextTabletTrack();
                    Assert(TabletMusicAppUI.tabletCurrentTrackId == "platform", "Queue did not skip unavailable local audio");
                    Assert(MusicLibrarySession.State.queue.Contains(localTrack.id), "Automatic advance removed unavailable queue item");
                    MusicLibrarySession.State.queue.Remove(localTrack.id);
                    Debug.Log("MUSIC_AUTO_QUEUE_RETAIN_OK: automatic skip retains missing item");
                    File.Move(Path.Combine(Local, localTrack.fileName + ".missing"), Path.Combine(Local, localTrack.fileName));
                    TabletMusicAppUI.SetVolume(.42f); TabletMusicAppUI.SetPlayMode(2);
                    TabletMusicAppUI.SeekTabletMusic(.15f); TabletMusicAppUI.ToggleTabletMusicPlayPause();
                    MusicLibrarySession.Save();
                    document.panelSettings.targetTexture = null; target.Release();
                    target = new RenderTexture(1024, 768, 24); target.Create(); document.panelSettings.targetTexture = target;
                    Debug.Log("MUSIC_LOCAL_AND_SAVE_OK: real WAV decode/play, missing-file queue skip, saved resume position/preferences");
                    break;
                case 6:
                    ValidateControlGeometry();
                    Capture("04-compact");
                    Assert(root.Q("music-player").worldBound.yMax <= root.worldBound.yMax + 1, "Compact player outside screen");
                    Assert(root.Q<Button>("music-play-toggle").text == "" && root.Q<Button>("music-play-toggle").Q(className: "music-vector-icon") != null, "Playback button is still plain text");
                    Assert(root.Q<Button>("music-queue").tooltip == "队列", "Icon button lost its tooltip");
                    Submit(root.Q<Button>("music-new-playlist"));
                    root.Q(className: "music-modal").Q<TextField>().value = "临时歌单";
                    Submit(root.Q<Button>("music-save-playlist"));
                    Submit(FindButton("重命名")); root.Q(className: "music-modal").Q<TextField>().value = "改名后的歌单";
                    Submit(root.Q<Button>("music-save-playlist"));
                    Assert(MusicLibrarySession.State.playlists.Any(p => p.title == "改名后的歌单"), "UI rename failed");
                    Submit(FindButton("删除歌单")); Submit(FindButton("确认删除"));
                    Assert(MusicLibrarySession.State.playlists.Count == 1, "UI deletion did not preserve the other playlist");
                    Submit(FindButton("雾峰沿线收藏"));
                    break;
                case 7:
                    // Send actual UI Toolkit pointer events through the drag handle, rather than only moving the model.
                    var rows = root.Query<VisualElement>(className: "music-track-row").ToList();
                    var grip = rows[0].Q<Button>(className: "music-grip");
                    var from = grip.worldBound.center; var to = rows[1].worldBound.center;
                    using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = from }))
                    { down.target = grip; grip.SendEvent(down); }
                    using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = to }))
                    { up.target = grip; grip.SendEvent(up); }
                    Assert(MusicLibrarySession.State.playlists.Single().tracks[0] == "platform", "Pointer drag did not reorder the playlist");
                    MusicLibrarySession.Enqueue("platform"); MusicLibrarySession.Enqueue("iron_and_ash");
                    Submit(root.Q<Button>("music-queue"));
                    break;
                case 8:
                    var queueRows = root.Query<VisualElement>(className: "music-queue-item").ToList();
                    var handle = queueRows[0].Q<Button>();
                    using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = handle.worldBound.center }))
                    { down.target = handle; handle.SendEvent(down); }
                    using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = queueRows[1].worldBound.center }))
                    { up.target = handle; handle.SendEvent(up); }
                    Assert(MusicLibrarySession.State.queue[0] == "iron_and_ash", "Pointer drag did not reorder the queue");
                    Submit(FindButton("清空队列")); Assert(MusicLibrarySession.State.queue.Count == 0, "Queue clear failed");
                    Assert(TabletMusicAppUI.tabletCurrentClip != null, "Deleting a playlist affected the playing clip");
                    Debug.Log("MUSIC_UI_EXTENDED_OK: icon controls/tooltips, UI rename/delete, actual pointer drag in playlist/queue, clear, import filtering");
                    string jsonPath = Path.Combine(Local, "review.json");
                    File.WriteAllText(jsonPath, "[{\"word\":\"along\",\"start\":1,\"end\":1.5,\"line_index\":0},{\"word\":\"rails\",\"start\":2,\"end\":3,\"line_index\":0}]");
                    Assert(MusicTimedText.Import("platform", jsonPath, false, out _) &&
                        MusicTimedText.Load(MusicLibrarySession.Find("platform")).lines[0].words[0].end == 1.5,
                        "JSON import/reload did not preserve the explicit word end");
                    string jsonPreference = MusicTimedText.Preference("platform").fileName;
                    File.WriteAllText(jsonPath, "[]");
                    Assert(!MusicTimedText.Import("platform", jsonPath, false, out _) &&
                        MusicTimedText.Preference("platform").fileName == jsonPreference,
                        "Invalid JSON import replaced a working lyric preference");
                    Debug.Log("MUSIC_JSON_IMPORT_OK: independent import/reload and invalid-file preservation");
                    string lrcPath = Path.Combine(Local, "review.lrc");
                    File.WriteAllText(lrcPath, "[00:00.00]<00:00.00>沿线<00:01.00>试音<00:02.00>\n[00:02.00]<00:02.00>归途<00:04.00>回声<00:06.00>\n[00:06.00]", System.Text.Encoding.UTF8);
                    string translationPath = Path.Combine(Local, "review-translation.srt");
                    File.WriteAllText(translationPath, "1\n00:00:00,000 --> 00:00:02,000\nAlong the line\n\n2\n00:00:02,000 --> 00:00:06,000\nHomeward echoes", System.Text.Encoding.UTF8);
                    Assert(MusicTimedText.Import("platform", lrcPath, false, out _) && MusicTimedText.Import("platform", translationPath, true, out _), "Timed-text file import failed");
                    TabletMusicAppUI.SeekTabletMusic(.5f / TabletMusicAppUI.tabletCurrentClip.length);
                    document.panelSettings.targetTexture = null; target.Release();
                    target = new RenderTexture(1440, 900, 24); target.Create(); document.panelSettings.targetTexture = target;
                    Submit(root.Q<Button>("music-lyrics"));
                    break;
                case 9:
                    Assert(root.Q("music-lyrics-overlay") != null && root.Q<Button>("music-lyric-line-0").ClassListContains("is-current"), "Lyric panel did not follow actual playback position");
                    Submit(root.Q<Button>("music-lyrics-motion"));
                    Assert(MusicLibrarySession.State.lyricMotion == 2 && root.Q("music-lyrics-overlay").ClassListContains("music-motion-stage"), "Stage mode did not activate");
                    Submit(root.Q<Button>("music-lyric-line-1"));
                    Assert(Math.Abs(TabletMusicAppUI.CurrentPosition - 2) < .15, "Clicking lyric did not seek");
                    Submit(root.Q<Button>("music-lyrics-earlier"));
                    Submit(root.Q<Button>("music-lyric-line-1"));
                    Assert(MusicTimedText.Preference("platform").offsetMs == 100 && Math.Abs(TabletMusicAppUI.CurrentPosition - 1.9) < .15, "Per-song offset was not applied to lyric seek");
                    root.Q<Slider>("music-lyrics-font").value = 30;
                    TabletMusicAppUI.SeekTabletMusic(3f / TabletMusicAppUI.tabletCurrentClip.length);
                    break;
                case 10:
                    ValidateScrollerGeometry();
                    Assert(root.Q("music-lyric-line-1").Q(className: "music-lyric-word-span").style.translate.value.y.value < -1,
                        "Real timed word did not lift in stage mode");
                    Assert(root.Q<Button>("music-lyric-line-1").ClassListContains("is-current"), "Clicked cue highlight stayed on the previous line due to rounding");
                    Capture("05-lyrics");
                    var lyricScroll = root.Q<ScrollView>("music-lyrics-scroll");
                    using (var wheel = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, delta = new Vector2(0, 3), mousePosition = lyricScroll.worldBound.center }))
                    { wheel.target = lyricScroll; lyricScroll.SendEvent(wheel); }
                    document.panelSettings.targetTexture = null; target.Release();
                    target = new RenderTexture(1024, 768, 24); target.Create(); document.panelSettings.targetTexture = target;
                    break;
                case 11:
                    Assert(root.Q<ScrollView>("music-lyrics-scroll").horizontalScroller.resolvedStyle.display == DisplayStyle.None, "Stage lyrics exposed a horizontal scrollbar");
                    Capture("06-lyrics-compact");
                    Assert(root.Q<Button>("music-lyrics-reset").worldBound.yMax < root.Q("music-lyrics-progress").worldBound.yMin, "Compact lyric settings overlap playback controls");
                    Assert(root.Q<Button>("music-lyrics-follow").resolvedStyle.display != DisplayStyle.None, "Manual scroll did not suspend lyric follow");
                    Submit(root.Q<Button>("music-lyrics-follow"));
                    using (var escape = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None))
                    { escape.target = root.Q("music-lyrics-overlay"); root.SendEvent(escape); }
                    Assert(root.Q("music-lyrics-overlay") == null, "Escape did not close lyrics");
                    var input = root.Q<TextField>("music-search"); bool playbackBeforeText = TabletMusicAppUI.tabletIsPlaying;
                    using (var space = KeyDownEvent.GetPooled(' ', KeyCode.Space, EventModifiers.None)) { space.target = input; input.SendEvent(space); }
                    Assert(playbackBeforeText == TabletMusicAppUI.tabletIsPlaying, "Typing space in search changed playback");
                    float buttonPosition = TabletMusicAppUI.CurrentPosition;
                    var focusedButton = root.Q<Button>("music-play-toggle"); focusedButton.Focus();
                    using (var buttonArrow = KeyDownEvent.GetPooled('\0', KeyCode.RightArrow, EventModifiers.None))
                    { buttonArrow.target = focusedButton; focusedButton.SendEvent(buttonArrow); }
                    Assert(Math.Abs(TabletMusicAppUI.CurrentPosition - buttonPosition) < .2f, "Arrow on focused button unexpectedly sought audio");
                    var focusedSlider = root.Q<Slider>("music-volume"); focusedSlider.Focus();
                    float sliderPosition = TabletMusicAppUI.CurrentPosition;
                    using (var sliderArrow = KeyDownEvent.GetPooled('\0', KeyCode.RightArrow, EventModifiers.None))
                    { sliderArrow.target = focusedSlider; focusedSlider.SendEvent(sliderArrow); }
                    Assert(Math.Abs(TabletMusicAppUI.CurrentPosition - sliderPosition) < .2f, "Arrow on focused slider unexpectedly sought audio");
                    Debug.Log("MUSIC_KEYBOARD_FOCUS_OK: button and slider arrows do not seek");
                    float at = TabletMusicAppUI.CurrentPosition;
                    using (var right = KeyDownEvent.GetPooled('\0', KeyCode.RightArrow, EventModifiers.None)) { right.target = root; root.SendEvent(right); }
                    Assert(Math.Abs(TabletMusicAppUI.CurrentPosition - at - 5) < .2, "Focused music seek shortcut failed");
                    string bad = Path.Combine(Local, "invalid.lrc"); File.WriteAllText(bad, "no timestamps");
                    string original = MusicTimedText.Preference("platform").fileName;
                    Assert(!MusicTimedText.Import("platform", bad, false, out _) && MusicTimedText.Preference("platform").fileName == original, "Bad import replaced working lyrics");
                    Assert(MusicTimedText.Load(MusicLibrarySession.Find("iron_and_ash")).lines.Count == 0, "Missing lyrics were fabricated");
                    TabletMusicAppUI.SeekTabletMusic(.15f);
                    MusicLibrarySession.Save();
                    Debug.Log("MUSIC_LYRICS_UI_OK: imported word timing + translation, audio-clock highlight, click seek/inverse offset, font, manual scroll/follow, Escape, typing-safe shortcuts, invalid import preservation");
                    // Preview an actual original lyric sheet without playing or unlocking a story song.
                    TabletMusicAppUI.tabletCurrentTrackId = "starlit_rails";
                    TabletMusicAppUI.tabletCurrentTrackTitle = "星光铁轨";
                    Submit(root.Q<Button>("music-lyrics"));
                    break;
                case 12:
                    Assert(root.Q(className: "music-plain-lyrics") is Label sheet && sheet.text.Contains("검문소"), "Authored lyrics did not reach the reading view");
                    Assert(root.Q<Button>("music-lyric-line-0") == null && root.Q<Button>("music-lyrics-follow").resolvedStyle.display == DisplayStyle.None,
                        "Untimed sheet offered fake seeking or auto-follow");
                    Capture("07-original-lyrics");
                    root.Q<Toggle>("music-lyrics-reduce-motion").value = true;
                    break;
                case 13:
                    Assert(root.ClassListContains("music-reduce-motion"), "Reduce-motion setting did not reach terminal effects");
                    Submit(root.Q<Button>("music-lyrics-close"));
                    TabletMusicAppUI.tabletCurrentTrackId = "platform";
                    TabletMusicAppUI.tabletCurrentTrackTitle = "Platform 站台";
                    MusicLibrarySession.State.reduceMotion = false;
                    MusicLibrarySession.Save();
                    Debug.Log("MUSIC_ORIGINAL_LYRICS_UI_OK: real embedded text, reading mode, no false seek/follow, reduced-motion controls");
                    previewPreviousClip = TabletMusicAppUI.tabletCurrentClip;
                    previewPreviousPosition = TabletMusicAppUI.CurrentPosition;
                    var sampleSource = MusicAudioEffectsEngine.GetActiveDeck();
                    sampleSource.Pause();
                    sampleSource.clip = Resources.Load<AudioClip>("bgm/sleepers");
                    sampleSource.Play();
                    sampleSource.Pause();
                    TabletMusicAppUI.tabletCurrentClip = sampleSource.clip;
                    TabletMusicAppUI.tabletCurrentTrackId = "sleepers";
                    TabletMusicAppUI.tabletCurrentTrackTitle = "Sleepers 铁轨沉睡者";
                    TabletMusicAppUI.tabletIsPlaying = false;
                    sampleSource.time = 160;
                    Submit(root.Q<Button>("music-lyrics"));
                    break;
                case 14:
                    var sample = MusicTimedText.Load(MusicLibrarySession.Find("sleepers"));
                    int bridge = sample.lines.FindIndex(line => line.text.StartsWith("I don't know"));
                    Assert(bridge >= 0 && sample.lines[bridge].start > 155, "Missing bridge was incorrectly aligned into the preceding instrumental gap");
                    Submit(root.Q<Button>("music-lyric-line-" + bridge));
                    Assert(Math.Abs(TabletMusicAppUI.CurrentPosition - sample.lines[bridge].start) < .1 && !TabletMusicAppUI.tabletIsPlaying,
                        "Original song cue did not seek the actual paused sample clip: actual=" + TabletMusicAppUI.CurrentPosition + " expected=" + sample.lines[bridge].start + " playing=" + TabletMusicAppUI.tabletIsPlaying);
                    break;
                case 15:
                    Capture("08-original-sync-draft");
                    Assert(root.Query<Label>().ToList().Any(label => label.text.Contains("自动对齐草稿")), "Automatic draft is presented as fully reviewed lyrics");
                    Submit(root.Q<Button>("music-lyrics-close"));
                    var previousSource = MusicAudioEffectsEngine.GetActiveDeck();
                    previousSource.clip = previewPreviousClip;
                    previousSource.Play();
                    previousSource.Pause();
                    previousSource.time = previewPreviousPosition;
                    TabletMusicAppUI.tabletCurrentClip = previewPreviousClip;
                    TabletMusicAppUI.tabletCurrentTrackId = "platform";
                    TabletMusicAppUI.tabletCurrentTrackTitle = "Platform 站台";
                    MusicLibrarySession.Save();
                    Debug.Log("MUSIC_OST_SYNC_DRAFT_OK: actual sample audio, 35 vocal lines, corrected bridge, click seek, paused clock, explicit automatic draft label");
                    File.WriteAllText(Path.Combine(Output, "runtime-result.json"), "{\"actualTabletEntry\":true,\"runtimeActions\":true,\"localWav\":true,\"screenshots\":true,\"restartCheck\":\"run BatchRestore\"}");
                    Submit(root.Q<Button>("music-lyrics"));
                    break;
                case 16:
                    Submit(root.Q<Button>("music-lyrics-motion"));
                    Assert(MusicLibrarySession.State.lyricMotion == 0, "Static mode cycle failed");
                    root.Q<ScrollView>("music-lyrics-scroll").verticalScroller.value = 0;
                    break;
                case 17:
                    ValidateScrollerGeometry();
                    var middleScroll = root.Q<ScrollView>("music-lyrics-scroll");
                    middleScroll.verticalScroller.value = middleScroll.verticalScroller.highValue * .5f;
                    break;
                case 18:
                    ValidateScrollerGeometry();
                    var endScroll = root.Q<ScrollView>("music-lyrics-scroll");
                    endScroll.verticalScroller.value = endScroll.verticalScroller.highValue;
                    break;
                case 19:
                    ValidateScrollerGeometry();
                    Submit(root.Q<Button>("music-lyrics-motion"));
                    Assert(MusicLibrarySession.State.lyricMotion == 1, "Follow mode cycle failed");
                    Submit(root.Q<Button>("music-lyrics-motion"));
                    root.Q<Toggle>("music-lyrics-reduce-motion").value = true;
                    TabletMusicAppUI.SeekTabletMusic(3f / TabletMusicAppUI.tabletCurrentClip.length);
                    break;
                case 20:
                    Assert(Mathf.Abs(root.Q("music-lyric-line-1").Q(className: "music-lyric-word-span").style.translate.value.y.value) < .01f,
                        "Reduced motion retained word lift");
                    root.Q<Toggle>("music-lyrics-reduce-motion").value = false;
                    Submit(root.Q<Button>("music-lyrics-close"));
                    TabletMusicAppUI.SeekTabletMusic(.15f);
                    MusicLibrarySession.Save();
                    Debug.Log("MUSIC_MOTION_SCROLL_OK: three modes, timed word lift, reduced-motion override, top/middle/end scroll containment");
                    root.Q<Button>("music-lyrics").Focus();
                    Submit(root.Q<Button>("music-lyrics"));
                    Submit(root.Q<Button>("music-lyrics-focus"));
                    TabletMusicAppUI.SeekTabletMusic(3f / TabletMusicAppUI.tabletCurrentClip.length);
                    break;
                case 21:
                    Assert(root.Q(className: "music-lyrics-art").resolvedStyle.display == DisplayStyle.None, "Focused stage did not hide settings");
                    Capture("09-focused-stage");
                    Submit(root.Q<Button>("music-lyrics-close"));
                    Assert(root.panel.focusController.focusedElement == root.Q<Button>("music-lyrics"), "Closing lyrics did not restore launcher focus");
                    var searchField = root.Q<TextField>("music-search");
                    searchField.value = "no-match";
                    Submit(root.Q<Button>("music-clear-search"));
                    Assert(searchField.value == "" && root.Q<Button>("music-clear-search").resolvedStyle.display == DisplayStyle.None, "Search clear failed");
                    if (root.Q("music-queue-drawer").resolvedStyle.display == DisplayStyle.None) Submit(root.Q<Button>("music-queue"));
                    break;
                case 22:
                    var queueBounds = root.Q("music-queue-drawer").worldBound;
                    var bodyBounds = root.Q(className: "music-body").worldBound;
                    Assert(Mathf.Abs(queueBounds.yMin - bodyBounds.yMin) < 1 && Mathf.Abs(queueBounds.yMax - bodyBounds.yMax) < 1,
                        "Compact queue does not fill app body");
                    Assert(queueBounds.yMax <= root.Q("music-player").worldBound.yMin + 1, "Queue covers player controls");
                    Capture("10-compact-queue");
                    using (var escapeQueue = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None)) { escapeQueue.target = root; root.SendEvent(escapeQueue); }
                    Assert(root.Q("music-queue-drawer").style.display == DisplayStyle.None, "Escape did not close queue");
                    TabletMusicAppUI.SeekTabletMusic(.15f); MusicLibrarySession.Save();
                    Assert(TabletMusicAppUI.CurrentPosition > 1, "UI review lost the initialized paused audio position");
                    Debug.Log("MUSIC_ERGONOMICS_OK: focused stage, restored keyboard focus, clear search, compact queue bounds/Escape");
                    previewPreviousClip = TabletMusicAppUI.tabletCurrentClip;
                    previewPreviousPosition = TabletMusicAppUI.CurrentPosition;
                    PreviewReactive("south_wind", 45);
                    break;
                case 23:
                    var southStage = root.Q<MusicReactiveStageView>();
                    var calibratedSouth = MusicTimedText.Load(MusicLibrarySession.Find("south_wind"));
                    Assert(calibratedSouth.lines.Count(line => !string.IsNullOrWhiteSpace(line.text)) == 39 && calibratedSouth.lines.All(line => line.words.Count == 0), "Calibrated original has missing lines or unverified word timing");
                    Assert(calibratedSouth.lines[calibratedSouth.Active(134.25)].text.Contains("통일") && calibratedSouth.lines[calibratedSouth.Active(143.4)].text.Contains("왠지"), "Corrected outro cue boundaries did not reach playback");
                    Assert(string.IsNullOrWhiteSpace(calibratedSouth.lines[calibratedSouth.Active(120)].text), "Instrumental gap still displays outro lyrics");
                    Debug.Log("MUSIC_SOUTH_CALIBRATION_OK: 39 authored lines, original-audio phonetic clock, corrected outro and instrumental gap, no synthesized word timing");
                    Assert(southStage.TrackId == "south_wind" && southStage.Composition == 0, "Track identity did not reach stage");
                    previousStageColor = southStage.PrimaryColor;
                    ValidateStageEnvelope(southStage);
                    Capture("11-south-wind-reactive-stage");
                    PreviewReactive("starlit_rails", 80);
                    break;
                case 24:
                    var nightStage = root.Q<MusicReactiveStageView>();
                    Assert(Vector3.Distance(new Vector3(previousStageColor.r, previousStageColor.g, previousStageColor.b), new Vector3(nightStage.PrimaryColor.r, nightStage.PrimaryColor.g, nightStage.PrimaryColor.b)) > .2f && nightStage.Composition == 1,
                        "Different songs share the same stage palette/composition");
                    Capture("12-starlit-reactive-stage");
                    PreviewReactive("chollima_ride", 60);
                    break;
                case 25:
                    Assert(root.Q<MusicReactiveStageView>().Composition == 2, "Third stage composition did not change");
                    Capture("13-chollima-reactive-stage");
                    TabletMusicAppUI.ToggleTabletMusicPlayPause();
                    break;
                case 26:
                    var runningStage = root.Q<MusicReactiveStageView>();
                    Assert(runningStage.MeasuredPower > .001f && runningStage.RenderClock > 59, "Real song does not drive stage energy/clock");
                    if (stageMotionFrame < 16)
                    {
                        Capture("14-stage-motion-" + stageMotionFrame.ToString("D2")); stageMotionFrame++;
                        checkAfter = EditorApplication.timeSinceStartup + .08; frame = Time.frameCount; return;
                    }
                    TabletMusicAppUI.ToggleTabletMusicPlayPause();
                    pausedStageClock = runningStage.RenderClock; pausedStageEnergy = runningStage.VisualEnergy;
                    break;
                case 27:
                    var pausedStage = root.Q<MusicReactiveStageView>();
                    Assert(pausedStage.RenderClock == pausedStageClock && pausedStage.VisualEnergy == pausedStageEnergy, "Paused stage kept moving");
                    root.Q<Toggle>("music-lyrics-reduce-motion").value = true;
                    break;
                case 28:
                    Assert(root.Q<MusicReactiveStageView>().RenderClock == 0 && root.Q<MusicReactiveStageView>().VisualEnergy == 0, "Reduce motion did not stop stage geometry");
                    Submit(root.Q<Button>("music-lyrics-close"));
                    MusicLibrarySession.State.reduceMotion = false;
                    var restoreStageSource = MusicAudioEffectsEngine.GetActiveDeck();
                    restoreStageSource.clip = previewPreviousClip; restoreStageSource.Play(); restoreStageSource.Pause(); restoreStageSource.time = previewPreviousPosition;
                    TabletMusicAppUI.tabletCurrentClip = previewPreviousClip;
                    TabletMusicAppUI.tabletCurrentTrackId = "platform"; TabletMusicAppUI.tabletCurrentTrackTitle = "Platform 站台";
                    TabletMusicAppUI.tabletIsPlaying = false; MusicLibrarySession.Save();
                    Debug.Log("MUSIC_REACTIVE_STAGE_OK: three actual song palettes/compositions, PCM silence/loud contrast, real audio clock/energy, pause freeze, reduced motion");
                    PreviewReactive("south_wind", 134.5f);
                    break;
                case 29:
                    var southDocument = MusicTimedText.Load(MusicLibrarySession.Find("south_wind"));
                    int lastSouthLine = southDocument.Active(147.5);
                    Submit(root.Q<Button>("music-lyric-line-" + lastSouthLine));
                    Assert(Math.Abs(TabletMusicAppUI.CurrentPosition - southDocument.lines[lastSouthLine].start) < .1 && !TabletMusicAppUI.tabletIsPlaying,
                        "Calibrated South Wind cue did not seek actual paused original audio");
                    break;
                case 30:
                    var finalSouthDocument = MusicTimedText.Load(MusicLibrarySession.Find("south_wind"));
                    Assert(root.Query<Label>().ToList().Any(label => label.text.Contains("待听校")), "Focused stage hides automatic draft provenance");
                    Assert(root.Q<Button>("music-lyric-line-" + finalSouthDocument.Active(147.5)).ClassListContains("is-current"), "Calibrated last line did not highlight after seek");
                    Capture("15-south-calibrated-outro");
                    Submit(root.Q<Button>("music-lyrics-close"));
                    var endSource = MusicAudioEffectsEngine.GetActiveDeck();
                    endSource.clip = previewPreviousClip; endSource.Play(); endSource.Pause(); endSource.time = previewPreviousPosition;
                    TabletMusicAppUI.tabletCurrentClip = previewPreviousClip; TabletMusicAppUI.tabletCurrentTrackId = "platform";
                    TabletMusicAppUI.tabletCurrentTrackTitle = "Platform 站台"; TabletMusicAppUI.tabletIsPlaying = false;
                    MusicLibrarySession.Save();
                    Debug.Log("MUSIC_SOUTH_CUE_SEEK_OK: corrected outro click seeks paused actual original clip and highlights last authored line");
                    Finish(); return;
            }
            stage++; frame = Time.frameCount;
            // Batch frames can advance before a 180ms entrance transition has finished.
            checkAfter = EditorApplication.timeSinceStartup + .3;
        }
        catch (Exception ex) { failed = true; Debug.LogException(ex); Finish(); }
    }
    static void CheckRestore()
    {
        if (EditorApplication.timeSinceStartup < checkAfter || MusicLibrarySession.Loading) return;
        if (restoreStep == 1)
        {
            Assert(TabletMusicAppUI.tabletCurrentTrackId == "platform" && TabletMusicAppUI.tabletIsPlaying &&
                TabletMusicAppUI.CurrentPosition < TabletMusicAppUI.tabletCurrentClip.length * .5f, "Single repeat did not loop at the actual audio boundary");
            MusicLibrarySession.Enqueue("iron_and_ash"); TabletMusicAppUI.SetPlayMode(1); TabletMusicAppUI.SeekTabletMusic(.9999f);
            restoreStep++; checkAfter = EditorApplication.timeSinceStartup + .75; return;
        }
        if (restoreStep == 2)
        {
            Assert(TabletMusicAppUI.tabletCurrentTrackId == "iron_and_ash", "Natural track end did not advance to the queue");
            TabletMusicAppUI.SetPlayMode(2); TabletMusicAppUI.PlayNextTabletTrack();
            restoreStep++; checkAfter = EditorApplication.timeSinceStartup + .5; return;
        }
        if (restoreStep == 3)
        {
            Assert(TabletMusicAppUI.tabletCurrentTrackId != "iron_and_ash" && TabletMusicAppUI.tabletIsPlaying, "Shuffle repeated the same track or failed to play");
            MusicAudioEffectsEngine.SetSleepTimer(MusicAudioEffectsEngine.SleepTimerOption.Min15);
            MusicAudioEffectsEngine.RemainingSleepSeconds = .05f;
            restoreStep++; checkAfter = EditorApplication.timeSinceStartup + .3; return;
        }
        if (restoreStep == 4)
        {
            Assert(!TabletMusicAppUI.tabletIsPlaying && !MusicAudioEffectsEngine.IsPlaying(), "Sleep timer did not pause both decks");
            Debug.Log("MUSIC_MODES_AND_SLEEP_OK: real single-repeat boundary, natural sequential queue advance, shuffle and timer pause");
            File.WriteAllText(Path.Combine(Output, "restart-result.json"), "{\"separateProcessRestore\":true,\"noAutoplay\":true,\"resumeAtSavedPosition\":true,\"singleRepeat\":true,\"naturalQueueAdvance\":true,\"shuffle\":true,\"sleep\":true}");
            Finish(); return;
        }
        var state = MusicLibrarySession.State;
        Assert(state.currentId == "platform" && state.position > 1 && Mathf.Abs(state.volume - .42f) < .001f && state.mode == 2 &&
            state.playlists.Count == 1 && state.favorites.Contains("platform"), "Cross-process state restore failed");
        Assert(!TabletMusicAppUI.tabletIsPlaying && !MusicAudioEffectsEngine.IsPlaying(), "Restored state auto-played");
        Assert(MusicTimedText.Preference("platform").offsetMs == 100 && state.lyricFontSize == 30 && state.lyricMotion == 2 && MusicTimedText.Load(MusicLibrarySession.Find("platform")).lines.Count == 3,
            "Cross-process lyric source/offset/font did not restore");
        float saved = state.position;
        Submit(root.Q<Button>("music-play-toggle"));
        Assert(TabletMusicAppUI.tabletIsPlaying && Math.Abs(TabletMusicAppUI.CurrentPosition - saved) < .25, "Resume did not seek to saved position");
        Debug.Log("MUSIC_RESTART_OK: separate Unity process restored library/preferences/position without autoplay; user play resumed at saved position");
        TabletMusicAppUI.SetPlayMode(0); TabletMusicAppUI.SeekTabletMusic(.9999f);
        restoreStep = 1; checkAfter = EditorApplication.timeSinceStartup + .75;
    }
    static void Finish()
    {
        EditorApplication.update -= Check; SessionState.SetBool(Pending, false);
        MusicLibrarySession.Save();
        if (File.Exists(Storage)) File.Copy(Storage, Path.Combine(Output, "library-before-exit.json"), true);
        Debug.Log(failed ? "MUSIC_REVIEW_FAILED" : "MUSIC_REVIEW_OK");
        EditorApplication.Exit(failed ? 1 : 0);
    }
    static void WriteWave(string path)
    {
        using var writer = new BinaryWriter(File.Create(path));
        const int rate = 22050, samples = rate * 8, bytes = samples * 2;
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1);
        writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
        for (int i = 0; i < samples; i++) writer.Write((short)(Math.Sin(i * 440 * Math.PI * 2 / rate) * 200));
    }
}

