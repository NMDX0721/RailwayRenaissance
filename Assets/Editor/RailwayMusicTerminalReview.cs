using System;
using System.Collections.Generic;
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
    // The wide review size: 1440x900 unless MUSIC_REVIEW_SIZE (for example "1109x625") asks for another, to try the music app on a smaller logical canvas.
    static readonly Vector2Int MainSize = ReadMainSize();
    static Vector2Int ReadMainSize()
    {
        string text = Environment.GetEnvironmentVariable("MUSIC_REVIEW_SIZE");
        if (!string.IsNullOrEmpty(text))
        {
            var parts = text.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h) && w >= 600 && h >= 400) return new Vector2Int(w, h);
        }
        return new Vector2Int(1440, 900);
    }
    static int stageMotionFrame, lumiSeq, styleStep, stressIndex;
    static readonly List<string> stressIssues = new List<string>();
    // Every style on both stress lines (measured at the end of each line), and Luminous again at the wildest intensity.
    static readonly string[] StressStyles = { "luminous", "luminous", "luminous", "luminous", "partita", "partita", "tilt", "tilt", "cadenza", "cadenza", "claddagh", "claddagh", "cappella", "cappella", "pendolo", "pendolo" };
    static readonly int[] StressIntensity = { 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
    // Shortly before each line ends (3.4 s and 9.4 s): every word has started, and the line is still the current one. The lyric clock is the playback
    // position plus the song's subtitle offset (+0.1 s left by an earlier step), so these are 0.3 s ahead of the line end, not 0.1 s.
    static readonly float[] StressTimes = { 3.1f, 9.1f, 3.1f, 9.1f, 3.1f, 9.1f, 3.1f, 9.1f, 3.1f, 9.1f, 3.1f, 9.1f, 3.1f, 9.1f, 3.1f, 9.1f };
    static readonly float[] lumiPose = new float[10], lumiLast = new float[10];
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
    // At the normal review size a failed check stops the run. When MUSIC_REVIEW_SIZE asks for another size (a pre-flight for a new canvas), a failed
    // check is logged as MUSIC_PREFLIGHT_ISSUE and the run goes on, so one run shows everything that does not fit instead of only the first thing.
    static bool Preflight => MainSize.x != 1440 || MainSize.y != 900;
    static void Assert(bool condition, string reason)
    {
        if (condition) return;
        if (Preflight) { Debug.Log("MUSIC_PREFLIGHT_ISSUE: " + reason); return; }
        throw new InvalidOperationException(reason);
    }
    static void ValidateControlGeometry()
    {
        ValidateScrollerGeometry();
        foreach (string name in new[] { "music-progress" })
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
        target = new RenderTexture(MainSize.x, MainSize.y, 24); target.Create(); settings.targetTexture = target;
        settings.clearColor = true; settings.colorClearValue = new Color(.06f, .07f, .06f, 1);
        document.panelSettings = settings;
        // Panel replacement can detach/recreate the document; re-enter using the real app path.
        typeof(GameMainUI).GetMethod("LaunchApp", Hidden).Invoke(ui, new object[] { "音乐" });
        root = document.rootVisualElement.Q("railway-music-terminal");
        Assert(!TabletMusicAppUI.tabletIsPlaying, "Opening the window auto-started audio");
        Debug.Log("MUSIC_TABLET_ENTRY_OK: real GameMainUI app entry; no autoplay");
    }
    // Eight English words and a 24-character Chinese line, every word timed, with a closing marker so the second line has an end.
    static string WriteStressLrc()
    {
        string Tag(float s) => ((int)(s / 60)).ToString("D2") + ":" + (s % 60).ToString("00.00", System.Globalization.CultureInfo.InvariantCulture);
        var text = new System.Text.StringBuilder();
        string[] english = { "The ", "rails ", "remember ", "every ", "little ", "thing ", "we ", "said" };
        float[] englishAt = { 0f, .4f, .8f, 1.4f, 1.8f, 2.2f, 2.6f, 2.9f };
        text.Append("[00:00.00]");
        for (int i = 0; i < english.Length; i++) text.Append("<" + Tag(englishAt[i]) + ">" + english[i]);
        text.Append("<" + Tag(3.4f) + ">\n");
        string[] chinese = { "沿着", "铁轨", "一路", "向北", "穿过", "群山", "与薄", "雾回", "到出", "发的", "那个", "站台" };
        text.Append("[00:04.00]");
        for (int i = 0; i < chinese.Length; i++) text.Append("<" + Tag(4f + i * .45f) + ">" + chinese[i]);
        text.Append("<" + Tag(9.4f) + ">\n[00:10.00]\n");
        string path = Path.Combine(Local, "review-stress.lrc");
        File.WriteAllText(path, text.ToString(), System.Text.Encoding.UTF8);
        return path;
    }

    // An element's opacity as it is seen: its own times every ancestor's, up to the stage; a hidden ancestor makes it invisible.
    static float EffectiveOpacity(VisualElement element, VisualElement stop)
    {
        float opacity = 1f;
        for (var e = element; e != null && e != stop; e = e.parent) { if (e.resolvedStyle.display == DisplayStyle.None) return 0f; opacity *= e.resolvedStyle.opacity; }
        return opacity;
    }

    // Measure one style on one stress line: every visible character must lie inside the stage, and words that are visible together must not cover each other.
    static void StressCheck(MusicStageLyrics host, int index)
    {
        string name = StressStyles[index] + "/intensity " + StressIntensity[index] + "/" + (StressTimes[index] < 5f ? "English" : "Chinese");
        // The lines are the current ones only just before they end; seeing nothing means the lyrics did not load, which is itself a failure.
        Rect stage = host.worldBound; const float slack = 8f;
        string[] charClasses = { "music-stage-word-text", "music-stage-tilt-char", "music-stage-claddagh-char", "music-stage-cappella-char", "music-stage-pendolo-char" };
        int seen = 0, outside = 0; string worst = "";
        foreach (string cls in charClasses)
            foreach (var label in host.Query<Label>(className: cls).ToList())
            {
                if (label.ClassListContains("music-stage-glow") || EffectiveOpacity(label, host) < .3f) continue;
                Rect r = label.worldBound; if (r.width <= 0 || r.height <= 0) continue;
                seen++;
                if (r.xMin < stage.xMin - slack || r.xMax > stage.xMax + slack || r.yMin < stage.yMin - slack || r.yMax > stage.yMax + slack) { outside++; worst = "'" + label.text + "' at " + r; }
            }
        if (seen == 0)
        {
            var doc = MusicTimedText.Load(MusicLibrarySession.Find("platform"));
            stressIssues.Add(name + ": nothing visible to measure");
            Debug.Log("MUSIC_STRESS_STATE: " + name + " style=" + host.StyleId + " hostDisplay=" + host.resolvedStyle.display + " hostSize=" + host.worldBound + " motion=" + MusicLibrarySession.State.lyricMotion +
                " position=" + TabletMusicAppUI.CurrentPosition.ToString("F2") + " docLines=" + doc.lines.Count + " activeAt=" + doc.Active(TabletMusicAppUI.CurrentPosition) + " labels=" + host.Query<Label>().ToList().Count);
        }
        if (outside > 0) stressIssues.Add(name + ": " + outside + " of " + seen + " characters outside the stage " + stage + " (e.g. " + worst + ")");
        if (StressStyles[index] == "claddagh" || StressStyles[index] == "cappella" || StressStyles[index] == "pendolo") return;
        string wordClass = StressStyles[index] == "tilt" ? "music-stage-tilt-row" : "music-stage-word";
        var words = host.Query(className: wordClass).ToList().FindAll(w => EffectiveOpacity(w, host) >= .3f);
        Rect TextRect(VisualElement word)
        {
            Rect union = Rect.zero; bool first = true;
            foreach (var label in word.Query<Label>().ToList())
            {
                if (label.ClassListContains("music-stage-glow")) continue;
                var r = label.worldBound; if (r.width <= 0) continue;
                union = first ? r : Rect.MinMaxRect(Mathf.Min(union.xMin, r.xMin), Mathf.Min(union.yMin, r.yMin), Mathf.Max(union.xMax, r.xMax), Mathf.Max(union.yMax, r.yMax)); first = false;
            }
            return union;
        }
        float worstOverlap = 0; string worstPair = "";
        for (int a = 0; a < words.Count; a++)
            for (int b = a + 1; b < words.Count; b++)
            {
                Rect ra = TextRect(words[a]), rb = TextRect(words[b]);
                float ox = Mathf.Max(0, Mathf.Min(ra.xMax, rb.xMax) - Mathf.Max(ra.xMin, rb.xMin)), oy = Mathf.Max(0, Mathf.Min(ra.yMax, rb.yMax) - Mathf.Max(ra.yMin, rb.yMin));
                float ratio = ox * oy / Mathf.Max(1f, Mathf.Min(ra.width * ra.height, rb.width * rb.height));
                if (ratio > worstOverlap) { worstOverlap = ratio; worstPair = a + "/" + b; }
            }
        // The wildest setting scatters words on purpose, so it is allowed more shared area; at the normal settings words must stay clear. (Rotated words are
        // measured by their bounding box, which is larger than the word, so these limits are on the safe side of "covers".)
        // Measured on the review frames: a magnified word being sung touching its neighbour reads fine at 40-50%, while the pairs that were really
        // unreadable (words on top of each other) were 69-97%. So half the smaller word is the line between touching and covering.
        float limit = .5f;
        if (worstOverlap > limit) stressIssues.Add(name + ": words " + worstPair + " cover " + (worstOverlap * 100).ToString("F0") + "% of the smaller one (" + words.Count + " words visible)");
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
                    Assert(root.worldBound.width > 1000 && root.Q("music-player").worldBound.height >= 80, "Runtime layout did not resolve");
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
                    Assert(root.Q<Label>(className: "music-empty")?.text.Contains("更多") == true, "Empty playlist guidance is missing; empty labels: [" +
                        string.Join(" | ", root.Query<Label>(className: "music-empty").ToList().Select(l => l.text)) + "] heading: " + root.Q<Label>("music-section-title")?.text +
                        " selected playlist: " + MusicLibrarySession.State.playlists.Count);
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
                    string missingPath = Path.Combine(Local, localTrack.fileName + ".missing");
                    // An earlier aborted run may have left this name behind; set it aside instead of deleting anything.
                    if (File.Exists(missingPath)) File.Move(missingPath, missingPath + "." + DateTime.Now.Ticks + ".stale");
                    File.Move(Path.Combine(Local, localTrack.fileName), missingPath);
                    Assert(!MusicLibrarySession.Available(localTrack), "Missing local file was not detected");
                    MusicLibrarySession.Enqueue(localTrack.id, true); MusicLibrarySession.Enqueue("platform");
                    Submit(root.Q<Button>("music-queue"));
                    // A reopened music task keeps its state, so the drawer may already have been open and the click closed it.
                    if (root.Query<VisualElement>(className: "music-queue-item").First() == null) Submit(root.Q<Button>("music-queue"));
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
                    Submit(root.Q<Button>("music-queue-more")); Submit(root.Q<Button>("music-clear-queue")); Assert(MusicLibrarySession.State.queue.Count == 0, "Queue clear failed");
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
                    target = new RenderTexture(MainSize.x, MainSize.y, 24); target.Create(); document.panelSettings.targetTexture = target;
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
                    if (lumiSeq == 0)
                    {
                        ValidateScrollerGeometry();
                        Assert(root.Q("music-lyric-line-1").Q(className: "music-lyric-word-span").style.translate.value.y.value < -1,
                            "Real timed word did not lift in stage mode");
                        Assert(root.Q<Button>("music-lyric-line-1").ClassListContains("is-current"), "Clicked cue highlight stayed on the previous line due to rounding");
                    }
                    var stageHost = root.Q<MusicStageLyrics>("music-stage-lyrics");
                    if (lumiSeq == 0)
                    {
                        // The left column shows the song's information, not just folded settings.
                        var infoRows = root.Query(className: "music-lyrics-info-row").ToList();
                        Assert(infoRows.Count >= 2, "The lyrics page's left column shows no song information (" + infoRows.Count + " rows)");
                        Assert(root.Q<Button>("music-lyrics-record") != null, "The lyrics page has no link to the record archive");
                        Assert(stageHost != null && stageHost.StyleId == "luminous", "Stage host did not select the Luminous style");
                        var fontProbe = stageHost.Q(className: "music-stage-word-text");
                        var probeFont = fontProbe?.resolvedStyle.unityFontDefinition.font;
                        Assert(probeFont != null && probeFont.name.ToLowerInvariant().Contains("zpix"), "Lyric subtitles are not set in the pixel font (got " + (probeFont != null ? probeFont.name : "none") + ")");
                        Assert(stageHost.Q(className: "music-stage-word") != null && stageHost.Q(className: "music-stage-glow") != null, "Luminous did not build word and glow layers");
                        Assert(root.Q("music-lyrics-stage-tuning").resolvedStyle.display != DisplayStyle.None, "Stage tuning controls are hidden in stage mode");
                    }
                    // Luminous poses are a function of the audio clock: step the clock forward and the words must actually move.
                    if (lumiSeq < 10)
                    {
                        if (lumiSeq == 0) Capture("05-lyrics");
                        // The second word of the line (回声, sung 4-6 s) must be invisible until the clock reaches it, then come forward.
                        var stageWords = stageHost.Query(className: "music-stage-word").ToList();
                        float poseSum = 0; foreach (var word in stageWords) poseSum += word.resolvedStyle.scale.value.x + word.resolvedStyle.opacity;
                        lumiPose[lumiSeq] = poseSum; lumiLast[lumiSeq] = stageWords.Count > 1 ? stageWords[stageWords.Count - 1].resolvedStyle.opacity : -1;
                        Capture("05s-stage-seq-" + lumiSeq.ToString("D2")); lumiSeq++;
                        // Stay inside the 2-6 s line: the last capture is at 5.8 s.
                        TabletMusicAppUI.SeekTabletMusic((3f + lumiSeq * .31f) / TabletMusicAppUI.tabletCurrentClip.length);
                        checkAfter = EditorApplication.timeSinceStartup + .45; frame = Time.frameCount; return;
                    }
                    float poseMin = lumiPose[0], poseMax = lumiPose[0], lastMax = lumiLast[0]; foreach (float pose in lumiPose) { poseMin = Mathf.Min(poseMin, pose); poseMax = Mathf.Max(poseMax, pose); }
                    foreach (float last in lumiLast) lastMax = Mathf.Max(lastMax, last);
                    Assert(poseMax - poseMin > .05f, "Luminous words did not move as the audio clock advanced");
                    Assert(lumiLast[0] >= 0 && lumiLast[0] < .2f && lastMax > .6f, "The second word did not wait hidden and then come forward when its time arrived (first " + lumiLast[0].ToString("F2") + ", best " + lastMax.ToString("F2") + ")");
                    Debug.Log("MUSIC_STAGE_LUMINOUS_OK: word and glow layers, tuning saved, poses follow the audio clock (" + poseMin.ToString("F2") + ".." + poseMax.ToString("F2") + ")");
                    // The other stage styles, on the same two-word line: Partita cuts it into rows with guide lines, Still draws three static lines.
                    if (styleStep < 17 + StressStyles.Length)
                    {
                        float styleClip = TabletMusicAppUI.tabletCurrentClip.length;
                        if (styleStep == 0) { MusicLibrarySession.State.stageStyle = "partita"; TabletMusicAppUI.SeekTabletMusic(3.2f / styleClip); }
                        else if (styleStep == 1)
                        {
                            Assert(stageHost.StyleId == "partita" && stageHost.Query(className: "music-stage-partita-row").ToList().Count >= 1, "Partita did not build its rows");
                            Assert(stageHost.Q(className: "music-stage-guide-h") != null && stageHost.Q(className: "music-stage-guide-v") != null, "Partita did not build its guide lines");
                            Capture("05p-partita-a"); TabletMusicAppUI.SeekTabletMusic(5.2f / styleClip);
                        }
                        else if (styleStep == 2)
                        {
                            Capture("05p-partita-b");
                            float guideBest = 0; foreach (var guide in stageHost.Query(className: "music-stage-guide-h").ToList()) guideBest = Mathf.Max(guideBest, guide.resolvedStyle.opacity);
                            Assert(guideBest > .5f, "Partita guide lines did not grow once their rows were sung (best opacity " + guideBest.ToString("F2") + ")");
                            MusicLibrarySession.State.stageStyle = "still"; TabletMusicAppUI.SeekTabletMusic(4f / styleClip);
                        }
                        else if (styleStep == 4)
                        {
                            Assert(stageHost.StyleId == "tilt" && stageHost.Query(className: "music-stage-tilt-row").ToList().Count >= 1, "Tilt did not build its rows");
                            var tiltChars = stageHost.Query<Label>(className: "music-stage-tilt-char").ToList();
                            Assert(tiltChars.Count >= 4, "Tilt did not build one label per character");
                            Capture("05u-tilt-a"); TabletMusicAppUI.SeekTabletMusic(5.4f / styleClip);
                        }
                        else if (styleStep == 5)
                        {
                            float tiltBest = 0; foreach (var tiltChar in stageHost.Query<Label>(className: "music-stage-tilt-char").ToList()) tiltBest = Mathf.Max(tiltBest, tiltChar.resolvedStyle.opacity);
                            Assert(tiltBest > .8f, "Tilt characters did not come in once their row was sung (best opacity " + tiltBest.ToString("F2") + ")");
                            Capture("05u-tilt-b");
                            // Force the first row into italic so the italic path is actually exercised.
                            MusicStageTilt.TiltRowOverride = 0; stageHost.Invalidate(); TabletMusicAppUI.SeekTabletMusic(5.4f / styleClip);
                            Debug.Log("MUSIC_STAGE_TILT_OK: Tilt rows and per-character labels, characters come in with the row");
                        }
                        else if (styleStep == 6)
                        {
                            var italics = stageHost.Query<Label>(className: "music-stage-tilt-italic").ToList();
                            Assert(italics.Count >= 2, "Forcing the first Tilt row did not produce italic characters");
                            Assert(italics[0].resolvedStyle.unityFontStyleAndWeight == FontStyle.Italic, "Italic Tilt characters are not italic");
                            float firstLift = italics[0].style.translate.value.y.value, secondLift = italics[1].style.translate.value.y.value;
                            Assert(firstLift * secondLift < 0 && Mathf.Abs(firstLift) > 1f && Mathf.Abs(secondLift) > 1f, "Italic Tilt characters are not alternately lifted and dropped (" + firstLift.ToString("F1") + ", " + secondLift.ToString("F1") + ")");
                            var plainChar = stageHost.Query<Label>(className: "music-stage-tilt-char").Where(l => !l.ClassListContains("music-stage-tilt-italic")).First();
                            Assert(plainChar != null && italics[0].resolvedStyle.color != plainChar.resolvedStyle.color, "The italic Tilt row does not use the accent colour");
                            Capture("05v-tilt-italic");
                            MusicStageTilt.TiltRowOverride = -1;
                            MusicLibrarySession.State.stageStyle = "claddagh"; TabletMusicAppUI.SeekTabletMusic(3.2f / styleClip);
                            Debug.Log("MUSIC_STAGE_TILT_ITALIC_OK: forced italic row, alternating lift " + firstLift.ToString("F1") + "/" + secondLift.ToString("F1") + ", accent colour");
                        }
                        else if (styleStep == 7)
                        {
                            var ringGlyphs = stageHost.Query(className: "music-stage-claddagh-glyph").ToList();
                            Assert(stageHost.StyleId == "claddagh" && ringGlyphs.Count >= 6, "Claddagh did not put the lines on its ring (" + ringGlyphs.Count + " glyphs)");
                            float ringMax = 0, ringMin = 1, xMin = float.MaxValue, xMax = float.MinValue, scaleMax = 0;
                            foreach (var glyph in ringGlyphs)
                            {
                                float o = glyph.resolvedStyle.opacity; ringMax = Mathf.Max(ringMax, o); if (o > .02f) ringMin = Mathf.Min(ringMin, o);
                                float gx = glyph.style.translate.value.x.value; xMin = Mathf.Min(xMin, gx); xMax = Mathf.Max(xMax, gx);
                                scaleMax = Mathf.Max(scaleMax, glyph.style.scale.value.value.x);
                            }
                            Assert(ringMax > .6f && ringMin < ringMax - .1f, "Claddagh has no front/back difference in opacity (" + ringMin.ToString("F2") + ".." + ringMax.ToString("F2") + ")");
                            Assert(xMax - xMin > 120f, "Claddagh glyphs are not spread along the ring (" + (xMax - xMin).ToString("F0") + " px)");
                            Assert(scaleMax > 1f, "Claddagh's focused glyph is not magnified (" + scaleMax.ToString("F2") + ")");
                            // Readable: glyphs that can be seen must not sit on top of each other.
                            var visibleCentres = new List<Vector2>();
                            foreach (var glyph in ringGlyphs) if (glyph.resolvedStyle.opacity > .5f) visibleCentres.Add(new Vector2(glyph.style.translate.value.x.value, glyph.style.translate.value.y.value));
                            float closest = float.MaxValue;
                            for (int a = 0; a < visibleCentres.Count; a++) for (int b = a + 1; b < visibleCentres.Count; b++) closest = Mathf.Min(closest, Vector2.Distance(visibleCentres[a], visibleCentres[b]));
                            Assert(visibleCentres.Count < 2 || closest > 24f, "Claddagh glyphs pile on top of each other (closest visible pair " + closest.ToString("F0") + " px)");
                            Capture("05w-claddagh-a"); TabletMusicAppUI.SeekTabletMusic(5.4f / styleClip);
                        }
                        else if (styleStep == 8)
                        {
                            Capture("05w-claddagh-b");
                            MusicLibrarySession.State.stageStyle = "cadenza"; TabletMusicAppUI.SeekTabletMusic(3.2f / styleClip);
                            Debug.Log("MUSIC_STAGE_CLADDAGH_OK: lines on a tilted ring, front glyph focused, glyphs spread along the ring");
                        }
                        else if (styleStep == 9)
                        {
                            var holders = stageHost.Query(className: "music-stage-cadenza-holder").ToList();
                            Assert(stageHost.StyleId == "cadenza" && holders.Count >= 2, "Cadenza did not place its words (" + holders.Count + " holders)");
                            // Hero in the middle, everything else clear of it and of each other.
                            // Real positions on screen, measured from the middle of the field, not the margin numbers the layout asked for.
                            var fieldMiddle = stageHost.Q(className: "music-stage-cadenza-field").worldBound.center;
                            float nearest = float.MaxValue, heroSpread = float.MaxValue;
                            for (int a = 0; a < holders.Count; a++)
                            {
                                var pa = holders[a].worldBound.center - fieldMiddle;
                                heroSpread = Mathf.Min(heroSpread, pa.magnitude);
                                for (int b = a + 1; b < holders.Count; b++) nearest = Mathf.Min(nearest, Vector2.Distance(holders[a].worldBound.center, holders[b].worldBound.center));
                            }
                            Assert(heroSpread < 4f, "Cadenza has no hero word in the middle of the stage (closest to centre " + heroSpread.ToString("F1") + " px)");
                            Assert(nearest > 40f, "Cadenza words were not pushed apart (closest pair " + nearest.ToString("F0") + " px)");
                            var halo = stageHost.Q(className: "music-stage-cadenza-halo");
                            Assert(halo != null && halo.resolvedStyle.opacity > .2f, "Cadenza's ring of light is not around the sung character");
                            Capture("05x-cadenza-a"); TabletMusicAppUI.SeekTabletMusic(5.4f / styleClip);
                        }
                        else if (styleStep == 10)
                        {
                            // Words that can be seen at the same time must not cover each other: the shared area of any two stays small.
                            var cadenzaWords = stageHost.Query(className: "music-stage-word").ToList();
                            float worstOverlap = 0;
                            for (int a = 0; a < cadenzaWords.Count; a++)
                                for (int b = a + 1; b < cadenzaWords.Count; b++)
                                {
                                    if (cadenzaWords[a].resolvedStyle.opacity < .5f || cadenzaWords[b].resolvedStyle.opacity < .5f) continue;
                                    // The text itself, not the word's wider box: the union of its character labels (the glow layer is skipped).
                                    Rect TextRect(VisualElement word)
                                    {
                                        Rect union = Rect.zero; bool first = true;
                                        foreach (var label in word.Query<Label>(className: "music-stage-word-text").ToList())
                                        {
                                            if (label.ClassListContains("music-stage-glow")) continue;
                                            var r = label.worldBound; union = first ? r : Rect.MinMaxRect(Mathf.Min(union.xMin, r.xMin), Mathf.Min(union.yMin, r.yMin), Mathf.Max(union.xMax, r.xMax), Mathf.Max(union.yMax, r.yMax)); first = false;
                                        }
                                        return union;
                                    }
                                    Rect ra = TextRect(cadenzaWords[a]), rb = TextRect(cadenzaWords[b]);
                                    float ox = Mathf.Max(0, Mathf.Min(ra.xMax, rb.xMax) - Mathf.Max(ra.xMin, rb.xMin)), oy = Mathf.Max(0, Mathf.Min(ra.yMax, rb.yMax) - Mathf.Max(ra.yMin, rb.yMin));
                                    float smaller = Mathf.Max(1f, Mathf.Min(ra.width * ra.height, rb.width * rb.height));
                                    worstOverlap = Mathf.Max(worstOverlap, ox * oy / smaller);
                                }
                            Assert(worstOverlap < .3f, "Cadenza words cover each other (worst shared area " + (worstOverlap * 100).ToString("F0") + "% of the smaller word)");
                            Capture("05x-cadenza-b");
                            MusicLibrarySession.State.stageStyle = "cappella"; TabletMusicAppUI.SeekTabletMusic(3.2f / styleClip);
                            Debug.Log("MUSIC_STAGE_CADENZA_OK: hero word in the middle, other words pushed clear, ring of light on the sung character");
                        }
                        else if (styleStep == 11)
                        {
                            var chatRows = stageHost.Query(className: "music-stage-cappella-row").ToList().FindAll(r => r.resolvedStyle.display != DisplayStyle.None);
                            Assert(stageHost.StyleId == "cappella" && chatRows.Count >= 3, "Cappella did not show the title and the lines sung so far (" + chatRows.Count + " rows)");
                            var rightRows = chatRows.FindAll(r => r.ClassListContains("music-stage-cappella-right")); var leftRows = chatRows.FindAll(r => !r.ClassListContains("music-stage-cappella-right"));
                            Assert(rightRows.Count >= 1 && leftRows.Count >= 1, "Cappella messages are not split between the left and right senders (" + leftRows.Count + " left, " + rightRows.Count + " right)");
                            // Real positions: right-hand bubbles sit to the right of left-hand ones, and rows follow each other down the stage without covering one another.
                            float leftMax = float.MinValue, rightMin = float.MaxValue;
                            foreach (var r in leftRows) leftMax = Mathf.Max(leftMax, r.Q(className: "music-stage-cappella-bubble").worldBound.center.x);
                            foreach (var r in rightRows) rightMin = Mathf.Min(rightMin, r.Q(className: "music-stage-cappella-bubble").worldBound.center.x);
                            Assert(rightMin > leftMax, "Right-hand Cappella bubbles are not to the right of the left-hand ones (" + rightMin.ToString("F0") + " vs " + leftMax.ToString("F0") + ")");
                            chatRows.Sort((x, y) => x.worldBound.yMin.CompareTo(y.worldBound.yMin));
                            for (int r = 0; r + 1 < chatRows.Count; r++) Assert(chatRows[r].worldBound.yMax <= chatRows[r + 1].worldBound.yMin + 3f, "Cappella rows cover each other (row " + r + ")");
                            var activeChars = chatRows[chatRows.Count - 1].Query<Label>(className: "music-stage-cappella-char").ToList();
                            Assert(activeChars.Count >= 2 && activeChars[0].resolvedStyle.opacity > .9f, "Cappella text did not fade in as it was sung");
                            var cappellaFont = activeChars[0].resolvedStyle.unityFontDefinition.font;
                            Assert(cappellaFont != null && cappellaFont.name.ToLowerInvariant().Contains("zpix"), "Cappella text is not in the pixel font");
                            Capture("05y-cappella-a"); TabletMusicAppUI.SeekTabletMusic(5.4f / styleClip);
                        }
                        else if (styleStep == 12)
                        {
                            var stamps = stageHost.Query(className: "music-stage-cappella-stamp").ToList().FindAll(s => s.resolvedStyle.display != DisplayStyle.None);
                            Assert(stamps.Count >= 1, "Cappella showed no timestamp once a line was complete");
                            Capture("05y-cappella-b");
                            MusicLibrarySession.State.stageStyle = "luminous"; TabletMusicAppUI.SeekTabletMusic(3f / styleClip);
                            Debug.Log("MUSIC_STAGE_CAPPELLA_OK: chat rows split between two senders, no overlap, text fades in, timestamps after the line");
                        }
                        else if (styleStep == 14 + StressStyles.Length)
                        {
                            // The link to the record archive: the button opens the archive sheet.
                            Submit(root.Q<Button>("music-lyrics-record"));
                        }
                        else if (styleStep == 15 + StressStyles.Length)
                        {
                            // The sheet is a modal and replaces the lyrics page by design; close it and reopen the lyrics for the steps that follow.
                            Assert(root.Q(className: "music-record-sheet") != null, "The record archive did not open from the lyrics page");
                            // The lyrics page is found by its name; it has no such class, so a class query would find nothing whether or not it is open.
                            Assert(root.Q("music-lyrics-overlay") == null, "The record archive should replace the lyrics page, but the lyrics page is still open");
                            Assert(RailwayMusicTerminalView.RecordReturnsToLyrics, "The record archive opened from the lyrics page did not remember to return to it");
                            Capture("08-record-from-lyrics");
                            Submit(root.Q(className: "music-modal-header").Q<Button>());
                        }
                        else if (styleStep == 16 + StressStyles.Length)
                        {
                            // Closing the record archive that was opened from the lyrics page returns to the lyrics page, not the library.
                            var overlaysNow = root.Query(className: "music-overlay").ToList();
                            string overlayList = string.Join(" | ", overlaysNow.ConvertAll(o => string.Join(".", o.GetClasses())));
                            Assert(root.Q("music-lyrics-overlay") != null && root.Q(className: "music-record-sheet") == null,
                                "Closing the record archive did not return to the lyrics page (overlays: " + overlaysNow.Count + " [" + overlayList + "], record sheet " + (root.Q(className: "music-record-sheet") != null ? "still open" : "closed") +
                                ", returns flag " + RailwayMusicTerminalView.RecordReturnsToLyrics + ", current track " + TabletMusicAppUI.tabletCurrentTrackId + ")");
                            Assert(root.Q<Button>("music-lyrics-record") != null, "The lyrics page that came back has no record archive link");
                            Debug.Log("MUSIC_LYRICS_INFO_OK: song information in the left column, the link opens the record archive, and closing it returns to the lyrics page");
                        }
                        else if (styleStep >= 13 && styleStep <= 13 + StressStyles.Length)
                        {
                            // Stress: harder lyrics (an eight-word English line, a 24-character Chinese line) on every style, measured at the end of
                            // each line, when every word has been sung and takes the most room. Issues are collected and reported together.
                            if (styleStep == 13)
                            {
                                Assert(MusicTimedText.Import("platform", WriteStressLrc(), false, out _), "Stress lyrics could not be imported");
                                Submit(root.Q<Button>("music-lyrics-reload")); stressIndex = 0; stressIssues.Clear();
                                var probe = MusicTimedText.Load(MusicLibrarySession.Find("platform"));
                                Debug.Log("MUSIC_STRESS_DOC: lines=" + probe.lines.Count + " error=" + (probe.error ?? "none") + " source=" + probe.source + " first=" + (probe.lines.Count > 0 ? probe.lines[0].text + " words=" + probe.lines[0].words.Count + " end=" + probe.lines[0].end : "-"));
                            }
                            else
                            {
                                Capture("stress-" + StressStyles[stressIndex] + "-i" + StressIntensity[stressIndex] + (StressTimes[stressIndex] < 5f ? "-en" : "-zh"));
                                StressCheck(stageHost, stressIndex); stressIndex++;
                            }
                            if (stressIndex < StressStyles.Length)
                            {
                                MusicLibrarySession.State.stageStyle = StressStyles[stressIndex]; MusicLibrarySession.State.stageIntensity = StressIntensity[stressIndex];
                                stageHost.Invalidate(); TabletMusicAppUI.SeekTabletMusic(StressTimes[stressIndex] / styleClip);
                            }
                            else
                            {
                                // Put the review lyrics back for the steps that follow, whatever the stress found.
                                MusicLibrarySession.State.stageStyle = "luminous"; MusicLibrarySession.State.stageIntensity = 1;
                                Assert(MusicTimedText.Import("platform", Path.Combine(Local, "review.lrc"), false, out _) && MusicTimedText.Import("platform", Path.Combine(Local, "review-translation.srt"), true, out _), "Review lyrics could not be restored");
                                Submit(root.Q<Button>("music-lyrics-reload")); TabletMusicAppUI.SeekTabletMusic(3f / styleClip);
                                foreach (var issue in stressIssues) Debug.Log("MUSIC_STRESS_ISSUE: " + issue);
                                Assert(stressIssues.Count == 0, stressIssues.Count + " stress issues: " + string.Join("; ", stressIssues));
                                Debug.Log("MUSIC_STAGE_STRESS_OK: " + StressStyles.Length + " style/line combinations stay inside the stage and clear of each other");
                            }
                        }
                        else if (styleStep == 3)
                        {
                            var stillCurrent = stageHost.Q<Label>(className: "music-stage-still-current");
                            Assert(stageHost.StyleId == "still" && stillCurrent != null && stillCurrent.text == "归途回声", "Still did not show the current line");
                            Assert(stageHost.Q(className: "music-stage-previous") == null || stageHost.Q(className: "music-stage-previous").resolvedStyle.display == DisplayStyle.None, "Shared subtitle layer was not hidden for Still");
                            Capture("05t-still");
                            MusicLibrarySession.State.stageStyle = "tilt"; TabletMusicAppUI.SeekTabletMusic(3.2f / styleClip);
                            Debug.Log("MUSIC_STAGE_STYLES_OK: Partita rows and guide lines, Still three static lines, shared subtitle layer yields");
                        }
                        styleStep++; checkAfter = EditorApplication.timeSinceStartup + .7; frame = Time.frameCount; return;
                    }
                    var lyricScroll = root.Q<ScrollView>("music-lyrics-scroll");
                    using (var wheel = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, delta = new Vector2(0, 3), mousePosition = lyricScroll.worldBound.center }))
                    { wheel.target = lyricScroll; lyricScroll.SendEvent(wheel); }
                    document.panelSettings.targetTexture = null; target.Release();
                    target = new RenderTexture(1024, 768, 24); target.Create(); document.panelSettings.targetTexture = target;
                    break;
                case 11:
                    Assert(root.Q<ScrollView>("music-lyrics-scroll").horizontalScroller.resolvedStyle.display == DisplayStyle.None, "Stage lyrics exposed a horizontal scrollbar");
                    Capture("06-lyrics-compact");
                    // Stage tuning is saved with the library and rebuilds the line; restore the defaults afterwards.
                    int intensityBefore = MusicLibrarySession.State.stageIntensity;
                    Submit(root.Q<Button>("music-stage-intensity"));
                    Assert(MusicLibrarySession.State.stageIntensity == (intensityBefore + 1) % 3, "Stage intensity button did not change the saved tuning");
                    root.Q<Toggle>("music-stage-rotation").value = false;
                    root.Q<Slider>("music-stage-spacing").value = 1.4f;
                    Assert(!MusicLibrarySession.State.stageRotation && Mathf.Approximately(MusicLibrarySession.State.stageSpacing, 1.4f), "Stage rotation/spacing were not saved");
                    MusicLibrarySession.State.stageIntensity = intensityBefore; MusicLibrarySession.State.stageRotation = true; MusicLibrarySession.State.stageSpacing = .7f;
                    root.Q<Toggle>("music-stage-rotation").SetValueWithoutNotify(true); root.Q<Slider>("music-stage-spacing").SetValueWithoutNotify(.7f);
                    Assert(root.Q<Button>("music-lyrics-reset").worldBound.yMax < root.Q("music-lyrics-progress").worldBound.yMin, "Compact lyric settings overlap playback controls");
                    Assert(root.Q<Button>("music-lyrics-follow").resolvedStyle.display == DisplayStyle.None, "The follow banner must not cover the stage, which never scrolls");
                    var settingsLabel = root.Q<Foldout>("music-lyrics-more").Q(className: "unity-foldout__text");
                    Assert(settingsLabel != null && settingsLabel.resolvedStyle.width > 36f, "The settings foldout title is squeezed out by the switch styling");
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
                    Submit(root.Q<Button>("music-volume-button")); var focusedSlider = root.Q("music-volume"); focusedSlider.Focus();
                    float sliderPosition = TabletMusicAppUI.CurrentPosition;
                    using (var sliderArrow = KeyDownEvent.GetPooled('\0', KeyCode.RightArrow, EventModifiers.None))
                    { sliderArrow.target = focusedSlider; focusedSlider.SendEvent(sliderArrow); }
                    Assert(Math.Abs(TabletMusicAppUI.CurrentPosition - sliderPosition) < .2f, "Arrow on focused slider unexpectedly sought audio");
                    Debug.Log("MUSIC_KEYBOARD_FOCUS_OK: button and slider arrows do not seek");
                    Assert(root.Q("music-volume-mute") != null && root.Q("music-volume") != null, "The volume popover is incomplete"); Capture("39-volume-popover");
                    using (var escape = KeyDownEvent.GetPooled(' ', KeyCode.Escape, EventModifiers.None)) { var ov = root.Q(className: "music-overlay"); escape.target = ov; ov.SendEvent(escape); }
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
                    var bodyBounds = root.Q(className: "music-main").worldBound;
                    Assert(Mathf.Abs(queueBounds.yMin - bodyBounds.yMin) < 1 && queueBounds.yMax > bodyBounds.yMax - 160f,
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
                    break;
                case 31:
                    // Collapsed sidebar must not hide the labels of the page content, and the help dialog stays readable.
                    Submit(root.Q<Button>("music-nav-云端"));
                    break;
                case 32:
                    // The narrow rail is the default; start from the wide sidebar so the toggle below still collapses it.
                    MusicLibrarySession.State.navCollapsed = false; root.EnableInClassList("music-nav-collapsed", false);
                    Submit(root.Q<Button>("music-nav-toggle"));
                    break;
                case 33:
                    Assert(root.ClassListContains("music-nav-collapsed"), "Sidebar did not collapse");
                    Assert(root.Q(className: "music-navigation").resolvedStyle.width < 90, "Collapsed sidebar is still wide");
                    Assert(root.Query<Label>(className: "music-button-label").ToList().Any(l => l.resolvedStyle.display != DisplayStyle.None && l.panel != null && !root.Q(className: "music-navigation").Contains(l)), "Collapsing the sidebar hid labels in the page content");
                    Capture("16-nav-collapsed");
                    Submit(root.Q<Button>("music-nav-toggle"));
                    break;
                case 34:
                    Assert(!root.ClassListContains("music-nav-collapsed"), "Sidebar did not expand again");
                    Submit(root.Q<Button>("music-help"));
                    break;
                case 35:
                    Assert(root.Q(className: "music-help-row") != null, "Help dialog has no rows");
                    Capture("17-help");
                    Debug.Log("MUSIC_NAV_HELP_OK: sidebar collapse keeps content labels, help dialog uses grouped rows");
                    root.Q<Button>("music-help")?.Focus(); CloseHelpForReview();
                    chartDeadline = EditorApplication.timeSinceStartup + 25;
                    Submit(root.Q<Button>("music-nav-榜单"));
                    break;
                case 36:
                    // The charts are read from the network; wait for the answer (or a failure) rather than a fixed time.
                    if (MusicCharts.Charts == null && !MusicCharts.ChartsFailed && EditorApplication.timeSinceStartup < chartDeadline) return;
                    break;
                case 37:
                    Debug.Log("MUSIC_CHARTS_STATE: charts=" + (MusicCharts.Charts?.Length ?? -1) + " hots=" + (MusicCharts.HotSearches?.Length ?? -1) + " failed=" + MusicCharts.ChartsFailed);
                    Capture("18-charts");
                    Submit(root.Q<Button>("music-nav-首页"));
                    break;
                case 38:
                    { var homeList = root.Q<ScrollView>("music-track-list"); if (homeList != null) homeList.scrollOffset = new Vector2(0, 330); }
                    break;
                case 39:
                    Capture("20-home-scrolled");
                    root.Q<TextField>("music-search")?.Focus();
                    break;
                case 40:
                    Capture("19-search-suggest");
                    Debug.Log("MUSIC_CHARTS_OK: chart page and search suggestions captured");
                    foreach (var toneMode in new[] { MusicAudioEffectsEngine.RadioFilterMode.Clean, MusicAudioEffectsEngine.RadioFilterMode.Locomotive, MusicAudioEffectsEngine.RadioFilterMode.Pioneer, MusicAudioEffectsEngine.RadioFilterMode.VintageTubeFM })
                    {
                        MusicAudioEffectsEngine.CurrentFilterMode = toneMode; MusicAudioEffectsEngine.RefreshTone(.5f);
                        var deck = MusicAudioEffectsEngine.GetActiveDeck(); var hp = deck.GetComponent<AudioHighPassFilter>(); var lp = deck.GetComponent<AudioLowPassFilter>(); var dist = deck.GetComponent<AudioDistortionFilter>(); var ch = deck.GetComponent<AudioChorusFilter>();
                        var noise = GameObject.Find("TabletMusic_Noise")?.GetComponent<AudioSource>();
                        Debug.Log("MUSIC_TONE: " + toneMode + " hp=" + (hp.enabled ? hp.cutoffFrequency : 0) + " lp=" + (lp.enabled ? lp.cutoffFrequency : 0) + " drive=" + (dist.enabled ? dist.distortionLevel : 0) + " chorus=" + ch.enabled + " noise=" + (noise != null && noise.isPlaying ? noise.volume : 0) + " pitch=" + deck.pitch.ToString("0.0000") + " playing=" + deck.isPlaying);
                    }
                    MusicAudioEffectsEngine.CurrentFilterMode = MusicAudioEffectsEngine.RadioFilterMode.Clean;
                    {
                        // Replacing a track puts the new one in the old one's place everywhere, and only removes the old one where the new one is already kept.
                        var st = MusicLibrarySession.State;
                        var favSave = new List<string>(st.favorites); var queueSave = new List<string>(st.queue); var recentSave = new List<string>(st.recent);
                        var listSave = new List<List<string>>(); foreach (var pl in st.playlists) listSave.Add(new List<string>(pl.tracks));
                        var probe = new MusicUserPlaylist { title = "replace-probe", tracks = new List<string> { "zz_b", "zz_old", "zz_new" } }; st.playlists.Add(probe);
                        st.favorites.Clear(); st.favorites.AddRange(new[] { "zz_old", "zz_b" }); st.queue.Clear(); st.queue.Add("zz_old"); st.recent.Clear(); st.recent.AddRange(new[] { "zz_b", "zz_old" });
                        int places = MusicLibrarySession.ReplaceTrack("zz_old", "zz_new");
                        Assert(places == 4 && st.favorites[0] == "zz_new" && st.queue[0] == "zz_new" && st.recent[1] == "zz_new" && probe.tracks.Count == 2 && !probe.tracks.Contains("zz_old"), "Track replacement missed a place (" + places + ")");
                        st.playlists.Remove(probe); st.favorites.Clear(); st.favorites.AddRange(favSave); st.queue.Clear(); st.queue.AddRange(queueSave); st.recent.Clear(); st.recent.AddRange(recentSave);
                        for (int n = 0; n < st.playlists.Count && n < listSave.Count; n++) { st.playlists[n].tracks.Clear(); st.playlists[n].tracks.AddRange(listSave[n]); }
                        Debug.Log("MUSIC_REPLACE_OK: a replaced track is swapped in favourites, queue, recent and playlists, and not duplicated");
                    }
                    {
                        string filterSample = "[00:00.00]作词：张三\n[00:01.00]作曲 : 李四\n[00:02.00]Produced by Foo\n[00:03.00]真正的歌词一行\n[00:08.00]Another real line: not a credit\n[00:10.00]带 remix 的一行\n[00:12.00]未经许可，不得翻唱";
                        var filtered = MusicTimedText.Parse(filterSample, ".lrc"); int removed = MusicLyricFilter.Apply(filtered, true, "remix");
                        Assert(removed == 5 && filtered.lines.Count == 2 && filtered.lines[0].text == "真正的歌词一行", "Lyric filter removed " + removed + " lines, left: " + string.Join(" / ", filtered.lines.ConvertAll(l => l.text)));
                        var untouched = MusicTimedText.Parse(filterSample, ".lrc"); Assert(MusicLyricFilter.Apply(untouched, false, "remix") == 0 && untouched.lines.Count == 7, "Lyric filter acted while switched off");
                        Debug.Log("MUSIC_LYRIC_FILTER_OK: credits, notices and custom words removed (" + removed + " of 7), real lines kept, off switch respected");
                    }
                    {
                        // The replacement sheet for an online track: register one, open the sheet and wait for candidates from the other sources.
                        var probeTrack = MusicLibrarySession.RegisterRemote(new NeteaseLocalBridge.RemoteTrack { id = "1901371647", title = "孤勇者", artist = "陈奕迅", durationMs = 256000 }, "netease");
                        Assert(probeTrack != null, "Could not register the probe online track");
                        if (probeTrack != null) { RailwayMusicTerminalView.OpenReplacementFor(probeTrack.id); chartDeadline = EditorApplication.timeSinceStartup + 25; }
                    }
                    break;
                case 41:
                    if (root.Query(className: "music-replace-row").ToList().Count == 0 && EditorApplication.timeSinceStartup < chartDeadline) return;
                    break;
                case 42:
                    Debug.Log("MUSIC_REPLACE_ROWS: " + root.Query(className: "music-replace-row").ToList().Count);
                    Capture("21-replace");
                    {
                        // The matching sheet for a local track, and the import path it uses for the lyrics it takes.
                        bool saved = RailwayMusicTerminalViewReviewAccess.ImportMatched("zz_match_probe", "[00:01.00]第一行\n[00:05.00]第二行", out string matchMessage);
                        Assert(saved && !string.IsNullOrEmpty(MusicTimedText.Preference("zz_match_probe").fileName), "Matched lyrics were not imported (" + matchMessage + ")");
                        var matchTrack = MusicLibrarySession.Tracks.FirstOrDefault(t => t.local);
                        Assert(matchTrack != null, "No local track to open the matching sheet for");
                        root.Query(className: "music-overlay").ToList().ForEach(o => o.RemoveFromHierarchy());
                        if (matchTrack != null) { RailwayMusicTerminalView.OpenMatchFor(matchTrack.id, "孤勇者 陈奕迅"); chartDeadline = EditorApplication.timeSinceStartup + 25; }
                    }
                    break;
                case 43:
                    if (root.Query(className: "music-replace-row").ToList().Count == 0 && EditorApplication.timeSinceStartup < chartDeadline) return;
                    break;
                case 44:
                    Debug.Log("MUSIC_MATCH_ROWS: " + root.Query(className: "music-replace-row").ToList().Count);
                    Capture("22-match");
                    root.Query(className: "music-overlay").ToList().ForEach(o => o.RemoveFromHierarchy());
                    Submit(root.Q<Button>("music-settings"));
                    break;
                case 45:
                    Assert(root.Q(className: "music-controls-panel") != null, "The playback settings panel did not open");
                    Assert(root.Q("music-option-speed") == null && root.Q<Button>("music-setting-speed") != null, "Level two should list categories, not spread every choice out");
                    Capture("23-controls-list");
                    Submit(root.Q<Button>("music-setting-speed"));
                    break;
                case 46:
                    Capture("24-controls-speed");
                    Submit(root.Q<Button>("music-opt-speed-4"));
                    break;
                case 47:
                    Assert(Mathf.Abs(MusicAudioEffectsEngine.PlaybackSpeed - 1.5f) < .01f, "The speed choice did not set the speed");
                    Assert(root.Q<Button>("music-setting-speed") != null && root.Q<Button>("music-setting-speed").Q<Label>(className: "music-setting-value").text == "1.5x", "Picking a speed did not go back to the list with the new value");
                    Submit(root.Q<Button>("music-setting-tone"));
                    break;
                case 48:
                    Submit(root.Q<Button>("music-opt-tone-1"));
                    break;
                case 49:
                    Assert(MusicAudioEffectsEngine.CurrentFilterMode == MusicAudioEffectsEngine.RadioFilterMode.Locomotive, "The tone choice did not set the tone");
                    Submit(root.Q<Button>("music-setting-pins"));
                    break;
                case 50:
                    Submit(root.Q<Button>("music-opt-pin-0"));
                    break;
                case 51:
                    Assert(MusicLibrarySession.State.barSlots == "speed" && root.Q<Button>("music-speed").resolvedStyle.display != DisplayStyle.None, "Pinning the speed did not put it on the bar");
                    Capture("25-controls-pins");
                    Submit(root.Q<Button>("music-setting-back"));
                    break;
                case 52:
                    Assert(root.Q<Button>("music-setting-speed") != null, "Back did not return to the category list");
                    // Put everything back for whatever runs after.
                    MusicAudioEffectsEngine.SetPlaybackSpeed(1f); MusicLibrarySession.State.playbackSpeed = 1f; MusicAudioEffectsEngine.CurrentFilterMode = MusicAudioEffectsEngine.RadioFilterMode.Clean;
                    MusicLibrarySession.State.toneMode = 0; MusicLibrarySession.State.barSlots = "";
                    root.Query(className: "music-overlay").ToList().ForEach(o => o.RemoveFromHierarchy());
                    Debug.Log("MUSIC_CONTROLS_PANEL_OK: three levels, speed and tone applied, pinning, back");
                    Submit(root.Q<Button>("music-nav-settings"));
                    break;
                case 53:
                    Assert(root.Q("music-settings-scroll") != null, "The settings page did not open from the rail");
                    Capture("26-settings-sound");
                    Submit(root.Q<Button>("music-eq-preset-5"));
                    break;
                case 54:
                    Assert(MusicEqualizer.Enabled && MusicEqualizer.Preset == "电台" && MusicEqualizer.Gains[0] < -11f, "The equalizer preset was not applied");
                    {
                        var deck = GameObject.Find("TabletMusic_DeckA");
                        Assert(deck == null || deck.GetComponent<MusicEqFilter>() != null, "The equalizer filter is not on the deck");
                    }
                    Capture("27-settings-eq");
                    Submit(root.Q<Button>("music-set-gain"));
                    break;
                case 55:
                    Assert(MusicEqualizer.ReplayGain, "Replay gain did not switch on");
                    Submit(root.Q<Button>("music-settings-tab-look"));
                    break;
                case 56:
                    Capture("28-settings-look");
                    Submit(root.Q<Button>("music-accent-2"));
                    break;
                case 57:
                    Assert(MusicLibrarySession.State.themeAccent == "#6C9BD8", "The fixed accent was not stored");
                    Submit(root.Q<Button>("music-settings-tab-bar"));
                    break;
                case 58:
                    Capture("29-settings-bar");
                    Submit(root.Q<Button>("music-slot-eq"));
                    break;
                case 59:
                    Assert(MusicLibrarySession.State.barSlots == "eq" && root.Q<Button>("music-eq").resolvedStyle.display != DisplayStyle.None, "Pinning the equalizer did not put it on the bar");
                    Submit(root.Q<Button>("music-settings-tab-lyrics"));
                    break;
                case 60:
                    Capture("30-settings-lyrics");
                    Submit(root.Q<Button>("music-settings-tab-help"));
                    break;
                case 61:
                    Submit(root.Q<Button>("music-tutorial-again"));
                    break;
                case 62:
                    Assert(root.Q("music-tour") != null && root.Q<Button>("music-tour-next") != null, "The tour did not start");
                    Capture("31-tour-1");
                    Submit(root.Q<Button>("music-tour-next"));
                    break;
                case 63:
                    Capture("32-tour-2");
                    Submit(root.Q<Button>("music-tour-skip"));
                    break;
                case 64:
                    Assert(root.Q("music-tour") == null && MusicLibrarySession.State.tutorialDone, "Skipping the tour did not end it");
                    // Back to the defaults for whatever runs after, then the title click that finds the track in the lists.
                    MusicEqualizer.Enabled = false; MusicEqualizer.ReplayGain = false; MusicEqualizer.ChoosePreset("平直"); MusicEqualizer.Store(MusicLibrarySession.State); MusicAudioEffectsEngine.RefreshEq();
                    MusicLibrarySession.State.themeAccent = ""; MusicLibrarySession.State.barSlots = "";
                    root.Query(className: "music-overlay").ToList().ForEach(o => o.RemoveFromHierarchy());
                    Submit(root.Q<Button>("music-nav-首页"));
                    break;
                case 65:
                    {
                        var title = root.Q<Label>("music-player-title") ?? root.Q("music-player").Q<Label>(className: "music-track-title");
                        using (var click = ClickEvent.GetPooled()) { click.target = title; title.SendEvent(click); }
                    }
                    break;
                case 66:
                    Assert(root.Query(className: "music-track-row").ToList().Any(r => r.ClassListContains("is-located")), "Clicking the title did not locate the track in a list");
                    Capture("33-located");
                    Debug.Log("MUSIC_SETTINGS_OK: settings page, equalizer, loudness, accent, bar slots, tour, locate");
                    Submit(root.Q<Button>("music-palette-open"));
                    break;
                case 67:
                    Assert(root.Q<TextField>("music-palette-input") != null, "The command palette did not open");
                    Capture("34-palette");
                    root.Q<TextField>("music-palette-input").value = "均衡";
                    break;
                case 68:
                    {
                        var first = root.Q<Button>("music-palette-row-0");
                        Assert(first != null && first.Q<Label>(className: "music-palette-title").text.Contains("均衡"), "The palette did not filter to the equalizer");
                        Capture("35-palette-filtered");
                        Submit(first);
                    }
                    break;
                case 69:
                    Assert(root.Q("music-settings-scroll") != null, "Running the palette command did not open the settings");
                    root.Query(className: "music-overlay").ToList().ForEach(o => o.RemoveFromHierarchy());
                    Debug.Log("MUSIC_PALETTE_OK: opened from the bar, filtered, ran a command");
                    Submit(root.Q<Button>("music-palette-open"));
                    break;
                case 70:
                    root.Q<TextField>("music-palette-input").value = "标尺";
                    break;
                case 71:
                    Assert(root.Q<Button>("music-palette-row-0") != null, "The palette did not find the ruler");
                    Submit(root.Q<Button>("music-palette-row-0"));
                    break;
                case 72:
                    {
                        Assert(root.Q("music-ruler-track") != null, "The ruler did not open");
                        MusicLibrarySession.State.lyricGlobalOffsetMs = 0;
                        var pref = MusicTimedText.Preference(TabletMusicAppUI.tabletCurrentTrackId); pref.offsetMs = 0;
                        Submit(root.Q<Button>("music-ruler-nudgep100"));
                    }
                    break;
                case 73:
                    Assert(MusicTimedText.Preference(TabletMusicAppUI.tabletCurrentTrackId).offsetMs == 100, "Nudging the ruler did not move the track offset");
                    Capture("36-ruler");
                    Submit(root.Q<Button>("music-ruler-scope-global"));
                    break;
                case 74:
                    Submit(root.Q<Button>("music-ruler-nudgem1000"));
                    break;
                case 75:
                    Assert(MusicLibrarySession.State.lyricGlobalOffsetMs == -1000 && MusicTimedText.EffectiveOffsetMs(TabletMusicAppUI.tabletCurrentTrackId) == -900, "The global offset did not add to the track offset");
                    Capture("37-ruler-global");
                    MusicLibrarySession.State.lyricGlobalOffsetMs = 0; MusicTimedText.Preference(TabletMusicAppUI.tabletCurrentTrackId).offsetMs = 0;
                    root.Query(className: "music-overlay").ToList().ForEach(o => o.RemoveFromHierarchy());
                    Debug.Log("MUSIC_RULER_OK: ruler opened from the palette, track and global offsets add");
                    {
                        var yrc = MusicTimedText.Parse("{\"t\":0,\"c\":[{\"tx\":\"作词\"}]}\n[12340,900](12340,400,0)你(12740,500,0)好\n[14000,800](14000,800,0)世界", ".yrc");
                        Assert(string.IsNullOrEmpty(yrc.error) && yrc.lines.Count == 2 && yrc.lines[0].text == "你好" && yrc.lines[0].words.Count == 2 && Math.Abs(yrc.lines[0].words[1].start - 12.74) < .001, "YRC parse failed: " + yrc.error);
                        var qrc = MusicTimedText.Parse("[ti:x]\n[12340,900]你(12340,400)好(12740,500)\n[14000,800]世界(14000,800)", ".qrc");
                        Assert(string.IsNullOrEmpty(qrc.error) && qrc.lines.Count == 2 && qrc.lines[1].text == "世界" && qrc.lines[0].words.Count == 2 && Math.Abs(qrc.lines[0].end - 13.24) < .001, "QRC parse failed: " + qrc.error);
                        {
                        var host = new MusicStageLyrics(); int plain = host.childCount; host.Select("tilt");
                        Assert(host.childCount == plain + 1 && host.StyleId == "tilt", "Switching style did not keep the old one for the hand-over");
                        Debug.Log("MUSIC_STAGE_HANDOVER_OK: old style stays while the new one grows in");
                    }
                    {
                        var song = MusicTimedText.Parse("[00:01.00]主歌一行\n[00:03.00]主歌二行\n[00:05.00]\n[00:06.00]回来吧回来\n[00:08.00]回来吧回来\n[00:10.00]别的一句\n[00:12.00]yeah\n[00:13.00]yeah\n[00:14.00]回来吧回来", ".lrc");
                        Assert(song.IsChorus(3) && song.IsChorus(4) && song.IsChorus(8) && !song.IsChorus(0) && !song.IsChorus(5) && !song.IsChorus(6), "Chorus detection picked the wrong lines");
                        {
                        var q = MusicLibrarySession.State.queue; q.Clear(); q.AddRange(new[] { "platform", "iron_and_ash", "platform" });
                        MusicLibrarySession.Enqueue("platform", true);
                        Assert(q.Count(x => x == "platform") == 1 && q[0] == "platform", "Enqueue should keep one copy and move it to the front");
                        q.Clear();
                    }
                    {
                        string Cut(string run) => string.Join(",", MusicChineseWords.Cut(run));
                        Assert(Cut("我们的世界") == "3,2", "Cut 我们的世界 gave " + Cut("我们的世界"));
                        Assert(Cut("不想回忆") == "2,2", "Cut 不想回忆 gave " + Cut("不想回忆"));
                        Assert(Cut("列车驶向远方") == "2,2,2", "Cut 列车驶向远方 gave " + Cut("列车驶向远方"));
                        Debug.Log("MUSIC_CN_WORDS_OK: Chinese words cut by list and particle rules");
                    }
                    Debug.Log("MUSIC_CHORUS_OK: repeated runs are chorus, short interjections are not");
                    }
                    Debug.Log("MUSIC_YRC_QRC_OK: word-timed YRC and QRC parse");
                    }
                    var q0 = MusicLibrarySession.State.queue; q0.Clear(); q0.AddRange(new[] { "platform", "iron_and_ash" }); MusicLibrarySession.Commit();
                    Submit(root.Q<Button>("music-queue"));
                    break;
                case 76:
                    Assert(root.Q<TextField>("music-queue-filter") != null && root.Q<Button>("music-queue-more") != null, "The queue tools are missing");
                    Submit(root.Q<Button>("music-queue-more"));
                    break;
                case 77:
                    Assert(root.Q<Button>("music-queue-pick") != null && root.Q<Button>("music-queue-dedupe") != null && root.Q<Button>("music-clear-queue") != null, "The queue's second menu is missing items");
                    Submit(root.Q<Button>("music-queue-pick"));
                    break;
                case 78:
                    Submit(root.Q<Button>("music-queue-check-0"));
                    break;
                case 79:
                    Capture("38-queue-picking");
                    Submit(root.Q<Button>("music-queue-batch-移到末尾"));
                    break;
                case 80:
                    Assert(MusicLibrarySession.State.queue.Count == 2 && MusicLibrarySession.State.queue[1] == "platform", "Batch move to end did not work");
                    MusicLibrarySession.State.queue.Clear(); MusicLibrarySession.Commit();
                    Debug.Log("MUSIC_QUEUE_OK: tools, batch move");
                    MusicLibrarySession.State.volume = .6f; TabletMusicAppUI.SetVolume(.6f);
                    Submit(root.Q<Button>("music-volume-button"));
                    break;
                case 81:
                    Assert(root.Q("music-volume") != null, "The volume popover did not open");
                    Capture("40-volume-popover");
                    Submit(root.Q<Button>("music-volume-mute"));
                    break;
                case 82:
                    Assert(MusicLibrarySession.State.volume < .005f, "Mute did not silence the volume");
                    Submit(root.Q<Button>("music-volume-mute"));
                    break;
                case 83:
                    Assert(Mathf.Abs(MusicLibrarySession.State.volume - .6f) < .02f, "Unmute did not restore the volume");
                    Debug.Log("MUSIC_VOLUME_OK: popover opens, mute and restore");
                    Finish(); return;
            }
            stage++; frame = Time.frameCount;
            // Batch frames can advance before a 180ms entrance transition has finished.
            checkAfter = EditorApplication.timeSinceStartup + .3;
        }
        catch (Exception ex) { failed = true; Debug.LogException(ex); Finish(); }
    }
    static double chartDeadline;
    static void CloseHelpForReview() { root?.Q(className: "music-overlay")?.RemoveFromHierarchy(); }
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

