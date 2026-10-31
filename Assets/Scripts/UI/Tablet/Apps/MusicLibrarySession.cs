using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    [Serializable]
    public sealed class MusicLibraryTrack
    {
        public string id, title, artist, category, fileName, condition, description;
        public bool local;
        [NonSerialized] public float duration;
    }

    [Serializable]
    public sealed class MusicUserPlaylist
    {
        public string id = Guid.NewGuid().ToString("N");
        public string title;
        public List<string> tracks = new List<string>();
    }

    [Serializable]
    public sealed class MusicLibraryState
    {
        public int version = 1;
        public List<MusicLibraryTrack> localTracks = new List<MusicLibraryTrack>();
        public List<string> favorites = new List<string>();
        public List<string> queue = new List<string>();
        public List<MusicUserPlaylist> playlists = new List<MusicUserPlaylist>();
        public List<MusicLyricPreference> lyrics = new List<MusicLyricPreference>();
        public int lyricFontSize = 24;
        public int lyricMotion = 1;
        public bool lyricTranslation = true, reduceMotion;
        public string currentId;
        public float position, volume = 0.65f;
        public int mode = 1;
    }

    // Separate from economic save slots. The store also permits isolated editor acceptance checks.
    public sealed class MusicLibraryStore
    {
        public readonly string Path;
        public bool ReadOnly { get; private set; }
        public MusicLibraryStore(string path) { Path = path; }
        public MusicLibraryState Load()
        {
            if (!File.Exists(Path)) return new MusicLibraryState();
            try
            {
                var state = JsonUtility.FromJson<MusicLibraryState>(File.ReadAllText(Path));
                if (state == null || state.version != 1 || state.queue == null || state.favorites == null ||
                    state.localTracks == null || state.playlists == null) throw new InvalidDataException("Unsupported music library");
                state.position = float.IsNaN(state.position) || float.IsInfinity(state.position) ? 0 : Mathf.Max(0, state.position);
                state.volume = float.IsNaN(state.volume) ? 0.65f : Mathf.Clamp01(state.volume);
                state.mode = Mathf.Clamp(state.mode, 0, 2);
                state.lyrics ??= new List<MusicLyricPreference>();
                state.lyrics.RemoveAll(p => p == null || string.IsNullOrEmpty(p.id) ||
                    (!string.IsNullOrEmpty(p.fileName) && System.IO.Path.GetFileName(p.fileName) != p.fileName) ||
                    (!string.IsNullOrEmpty(p.translationFileName) && System.IO.Path.GetFileName(p.translationFileName) != p.translationFileName));
                foreach (var preference in state.lyrics) preference.offsetMs = Mathf.Clamp(preference.offsetMs, -10000, 10000);
                state.lyricFontSize = state.lyricFontSize == 0 ? 24 : Mathf.Clamp(state.lyricFontSize, 18, 36);
                state.lyricMotion = Mathf.Clamp(state.lyricMotion, 0, 2);
                state.playlists.RemoveAll(p => p == null || string.IsNullOrEmpty(p.id) || p.tracks == null);
                state.localTracks.RemoveAll(t => t == null || string.IsNullOrEmpty(t.id) || string.IsNullOrEmpty(t.fileName) ||
                    System.IO.Path.GetFileName(t.fileName) != t.fileName);
                return state;
            }
            catch (Exception ex)
            {
                // Never replace an unreadable/newer library with an empty one.
                ReadOnly = true;
                Debug.LogWarning("[随身听] 曲库文件无法读取，原文件已保留，本次改动仅在内存中生效：" + ex.Message);
                return new MusicLibraryState();
            }
        }
        public bool Save(MusicLibraryState state)
        {
            if (ReadOnly) return false;
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                File.WriteAllText(Path + ".tmp", JsonUtility.ToJson(state, true));
                if (File.Exists(Path)) File.Replace(Path + ".tmp", Path, Path + ".bak");
                else File.Move(Path + ".tmp", Path);
                return true;
            }
            catch (Exception ex) { Debug.LogWarning("[随身听] 曲库保存失败：" + ex.Message); return false; }
        }
    }

    public static class MusicLibrarySession
    {
        public static event Action Changed;
        static MusicLibraryState state;
        static MusicLibraryStore store;
        static readonly Dictionary<string, MusicLibraryTrack> tracks = new Dictionary<string, MusicLibraryTrack>();
        static readonly HashSet<string> failed = new HashSet<string>();
        static int request;
        static float saveTimer;
        static readonly List<string> history = new List<string>();
        static int historyIndex = -1, pendingHistory = -1;
        static string reviewStorage;
#if UNITY_EDITOR
        public static void UseReviewStorage(string path)
        {
            if (state != null) throw new InvalidOperationException("Review storage must be selected before initialization");
            reviewStorage = path;
        }
#endif
        public static bool Loading { get; private set; }
        public static MusicLibraryState State { get { Ensure(); return state; } }
        public static IEnumerable<MusicLibraryTrack> Tracks { get { Ensure(); return tracks.Values; } }
        public static string LyricsDirectory { get { Ensure(); return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(store.Path), "Lyrics"); } }
        public static void Ensure()
        {
            if (state != null) return;
            store = new MusicLibraryStore(reviewStorage ?? System.IO.Path.Combine(Application.persistentDataPath, "MusicLibrary", "library-v1.json"));
            bool migrating = !File.Exists(store.Path);
            state = store.Load();
            foreach (var entry in TabletMusicAppUI.TabletMusicList)
            {
                tracks[entry.id] = new MusicLibraryTrack { id = entry.id, title = entry.title, category = entry.category,
                    condition = entry.condition, description = entry.desc };
                if (migrating && PlayerPrefs.GetInt("MusicFav_" + entry.id, 0) == 1) state.favorites.Add(entry.id);
            }
            if (migrating) state.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("TabletMusic_Vol", 0.65f));
            RefreshLocal(false);
            TabletMusicAppUI.ConfigurePlayback(state.volume, state.mode);
            var driver = new GameObject("RailwayMusicSession");
            UnityEngine.Object.DontDestroyOnLoad(driver);
            driver.AddComponent<MusicSessionDriver>();
            var restored = Find(state.currentId);
            if (restored != null)
            {
                TabletMusicAppUI.tabletCurrentTrackId = restored.id;
                TabletMusicAppUI.tabletCurrentTrackTitle = restored.title;
                TabletMusicAppUI.tabletCurrentTrackCategory = restored.category;
            }
            if (migrating) Save();
        }
        public static MusicLibraryTrack Find(string id)
        {
            Ensure();
            return id != null && tracks.TryGetValue(id, out var track) ? track : null;
        }
        public static void RefreshLocal(bool notify = true)
        {
            if (state == null) { Ensure(); return; }
            foreach (var local in LocalMusicFileImporter.GetLocalMusicTracks())
            {
                string name = System.IO.Path.GetFileName(local.filePath);
                string id = "local:" + name.ToLowerInvariant();
                var track = state.localTracks.Find(t => t.id == id);
                if (track == null)
                {
                    track = new MusicLibraryTrack { id = id, title = local.title, category = "本地音乐", local = true, fileName = name };
                    state.localTracks.Add(track);
                    if (PlayerPrefs.GetInt("MusicFav_" + local.id, 0) == 1 && !state.favorites.Contains(id)) state.favorites.Add(id);
                }
                failed.Remove(id);
            }
            foreach (var track in state.localTracks) tracks[track.id] = track;
            if (notify) Commit();
        }
        public static bool Unlocked(MusicLibraryTrack track)
        {
            if (track == null) return false;
            if (track.local) return true;
            var entry = Array.Find(TabletMusicAppUI.TabletMusicList, t => t.id == track.id);
            return entry != null && TabletMusicAppUI.IsTabletMusicUnlocked(entry);
        }
        public static string LocalPath(MusicLibraryTrack track) => System.IO.Path.Combine(LocalMusicFileImporter.GetCustomMusicDirectory(), track.fileName);
        public static bool Available(MusicLibraryTrack track)
        {
            if (!Unlocked(track) || failed.Contains(track.id)) return false;
            if (track.local) return File.Exists(LocalPath(track));
            var clip = Resources.Load<AudioClip>("bgm/" + track.id);
            if (clip != null) track.duration = clip.length;
            return clip != null;
        }
        public static void ToggleFavorite(string id)
        {
            if (!State.favorites.Remove(id)) State.favorites.Add(id);
            Commit();
        }
        public static void Enqueue(string id, bool next = false)
        {
            if (Find(id) == null) return;
            if (next) State.queue.Insert(0, id); else State.queue.Add(id);
            Commit();
        }
        public static void Move(List<string> list, int from, int to)
        {
            if (from < 0 || from >= list.Count || to < 0 || to >= list.Count || from == to) return;
            string id = list[from]; list.RemoveAt(from); list.Insert(to, id); Commit();
        }
        public static MusicUserPlaylist CreatePlaylist(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;
            var list = new MusicUserPlaylist { title = title.Trim() }; State.playlists.Add(list); Commit(); return list;
        }
        public static bool AddToPlaylist(MusicUserPlaylist list, string id)
        {
            if (Find(id) == null || list.tracks.Contains(id)) return false;
            list.tracks.Add(id); Commit(); return true;
        }
        public static void Commit() { Save(); Changed?.Invoke(); }
        public static void Save()
        {
            if (state == null) return;
            if (TabletMusicAppUI.TryGetCurrentPosition(out float position))
            {
                state.currentId = TabletMusicAppUI.tabletCurrentTrackId;
                state.position = position;
            }
            state.mode = (int)TabletMusicAppUI.tabletPlayMode;
            store.Save(state);
        }
        public static void Started(string id, AudioClip clip)
        {
            Ensure();
            if (pendingHistory >= 0 && pendingHistory < history.Count && history[pendingHistory] == id) historyIndex = pendingHistory;
            else if (historyIndex < 0 || history[historyIndex] != id)
            {
                if (historyIndex + 1 < history.Count) history.RemoveRange(historyIndex + 1, history.Count - historyIndex - 1);
                history.Add(id); historyIndex = history.Count - 1;
                if (history.Count > 100) { history.RemoveAt(0); historyIndex--; }
            }
            pendingHistory = -1;
            state.currentId = id; state.position = 0;
            if (tracks.TryGetValue(id, out var track)) track.duration = clip.length;
            Commit();
        }
        public static void Play(string id, bool resume = false) => PlayInternal(id, resume, -1);
        static void PlayInternal(string id, bool resume, int fromHistory)
        {
            Ensure();
            pendingHistory = fromHistory;
            int token = ++request;
            Loading = false;
            var track = Find(id);
            if (!Available(track)) { TabletMusicAppUI.ShowToast(Unlocked(track) ? "音频文件不可用，请检查文件或重新导入" : "该曲目尚未解锁"); return; }
            float position = resume && state.currentId == id ? state.position : 0;
            if (!track.local)
            {
                TabletMusicAppUI.PlayTabletTrack(Array.Find(TabletMusicAppUI.TabletMusicList, t => t.id == id));
                if (position > 0) TabletMusicAppUI.SeekTabletMusic(position / TabletMusicAppUI.tabletCurrentClip.length);
                return;
            }
            Loading = true; Changed?.Invoke();
            LocalMusicFileImporter.LoadAudioClipAsync(LocalPath(track), clip =>
            {
                if (token != request) { UnityEngine.Object.Destroy(clip); return; }
                Loading = false;
                TabletMusicAppUI.PlayExternalClip(track.id, track.title, track.category, clip);
                if (position > 0) TabletMusicAppUI.SeekTabletMusic(position / clip.length);
            }, error =>
            {
                if (token != request) return;
                Loading = false; failed.Add(id);
                TabletMusicAppUI.ShowToast("本地音频无法读取，已跳过：" + track.title);
                Advance(1);
            });
        }
        public static void CancelPending()
        {
            ++request; pendingHistory = -1; Loading = false; Changed?.Invoke();
        }
        public static void Advance(int direction)
        {
            Ensure();
            if (Loading) CancelPending();
            if (direction < 0)
            {
                if (TabletMusicAppUI.CurrentPosition > 3) { TabletMusicAppUI.SeekTabletMusic(0); return; }
                for (int i = historyIndex - 1; i >= 0; i--)
                    if (Available(Find(history[i]))) { PlayInternal(history[i], false, i); return; }
            }
            if (direction > 0)
            {
                while (state.queue.Count > 0)
                {
                    string id = state.queue[0]; state.queue.RemoveAt(0);
                    if (Available(Find(id))) { Commit(); Play(id); return; }
                }
                for (int i = historyIndex + 1; i < history.Count; i++)
                    if (Available(Find(history[i]))) { PlayInternal(history[i], false, i); return; }
            }
            var playable = new List<MusicLibraryTrack>();
            foreach (var track in tracks.Values) if (Available(track)) playable.Add(track);
            if (playable.Count == 0)
            {
                MusicAudioEffectsEngine.Pause(); TabletMusicAppUI.tabletIsPlaying = false;
                Commit(); TabletMusicAppUI.ShowToast("没有可播放的曲目，请检查文件或解锁原声"); return;
            }
            int index = playable.FindIndex(t => t.id == TabletMusicAppUI.tabletCurrentTrackId);
            int next = (index + direction + playable.Count) % playable.Count;
            if (direction > 0 && TabletMusicAppUI.tabletPlayMode == TabletMusicAppUI.TabletPlayMode.Shuffle && playable.Count > 1)
                next = (Mathf.Max(0, index) + UnityEngine.Random.Range(1, playable.Count)) % playable.Count;
            Commit(); Play(playable[next].id);
        }
        public static void Tick(float delta)
        {
            if (state == null) return;
            TabletMusicAppUI.UpdateAudioProgress();
            saveTimer += delta;
            if (saveTimer >= 5) { saveTimer = 0; Save(); }
        }
    }
}
