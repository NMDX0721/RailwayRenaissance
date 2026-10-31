using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    [Serializable]
    public sealed class MusicLibraryTrack
    {
        public string id, title, artist, album, category, fileName, condition, description, titleTranslation;
        public int metaVersion;
        public bool local;
        public string provider, remoteId;
        public float duration;
        [NonSerialized] public string coverUrl;
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
        public List<MusicLibraryTrack> cloudTracks = new List<MusicLibraryTrack>();
        public List<string> favorites = new List<string>();
        public List<string> queue = new List<string>();
        public List<MusicUserPlaylist> playlists = new List<MusicUserPlaylist>();
        public List<MusicLyricPreference> lyrics = new List<MusicLyricPreference>();
        public int lyricFontSize = 24;
        public int lyricMotion = 1;
        public bool lyricTranslation = true, reduceMotion, navCollapsed;
        public float playbackSpeed = 1f;
        public bool estimateWords = true;
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
                state.cloudTracks ??= new List<MusicLibraryTrack>();
                state.cloudTracks.RemoveAll(t => t == null || !ValidRemote(t.provider, t.remoteId) || t.id != MusicSourceId.Online(t.provider, t.remoteId));
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
        public static bool ValidRemote(string provider, string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 320) return false;
            if (provider == "netease" || provider == "bodian") return long.TryParse(id, out var value) && value > 0;
            if (provider == "subsonic" || provider == "webdav")
            {
                string[] parts = id.Split('~');
                if (parts.Length != 2 || parts[0].Length != 16 || parts[1].Length == 0) return false;
                foreach (char c in parts[0]) if (!Uri.IsHexDigit(c)) return false;
                foreach (char c in parts[1]) if (!char.IsLetterOrDigit(c) && c != '-' && c != '_') return false;
                return true;
            }
            if (provider == "kugou")
            {
                string[] parts = id.Split('~');
                if (parts.Length != 3 || parts[0].Length != 32) return false;
                foreach (char c in parts[0]) if (!Uri.IsHexDigit(c)) return false;
                return long.TryParse(parts[1], out var mix) && mix >= 0 && long.TryParse(parts[2], out var album) && album >= 0;
            }
            if (provider == "spotify" && id.Length != 22) return false;
            if (provider != "qq" && provider != "spotify") return false;
            foreach (char c in id) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z')) return false;
            return true;
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
        public static event Action<string> TrackStarted;
        static MusicLibraryState state;
        static MusicLibraryStore store;
        static readonly Dictionary<string, MusicLibraryTrack> tracks = new Dictionary<string, MusicLibraryTrack>();
        static readonly HashSet<string> failed = new HashSet<string>();
        static int request;
        static float saveTimer;
        static readonly List<string> history = new List<string>();
        static int historyIndex = -1, pendingHistory = -1;
        static string reviewStorage;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || MUSIC_RUNTIME_REVIEW
        public static void UseReviewStorage(string path)
        {
            if (state != null) throw new InvalidOperationException("Review storage must be selected before initialization");
            reviewStorage = path;
        }
#endif
        public static bool Loading { get; private set; }
        public static MusicLibraryState State { get { Ensure(); return state; } }
        public static IEnumerable<MusicLibraryTrack> Tracks { get { Ensure(); return tracks.Values; } }
        // Encrypted provider sessions live beside the library file, so isolated review storage stays isolated.
        // Manual test builds share one encrypted login across versions; automated runs keep theirs beside the isolated library.
        public static bool SharedAuth;
        public static string AuthDirectory { get { Ensure(); return reviewStorage != null && !SharedAuth ? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(store.Path), "Auth") : System.IO.Path.Combine(Application.persistentDataPath, "MusicAuth"); } }
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
            foreach (var cloud in state.cloudTracks) tracks[cloud.id] = cloud;
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
                string id = MusicSourceId.Local(name);
                var track = state.localTracks.Find(t => t.id == id);
                if (track == null)
                {
                    track = new MusicLibraryTrack { id = id, title = local.title, category = "本地音乐", local = true, fileName = name };
                    state.localTracks.Add(track);
                    if (PlayerPrefs.GetInt("MusicFav_" + local.id, 0) == 1 && !state.favorites.Contains(id)) state.favorites.Add(id);
                }
                MusicAlbumArtResolver.InvalidateLocal(id);
                var metadata = MusicLocalMetadata.Read(local.filePath);
                track.title = string.IsNullOrWhiteSpace(metadata.title) ? local.title : metadata.title;
                track.artist = metadata.artist;
                failed.Remove(id);
            }
            foreach (var track in state.localTracks) tracks[track.id] = track;
            if (notify) Commit();
        }
        public static bool Unlocked(MusicLibraryTrack track)
        {
            if (track == null) return false;
            if (track.local || track.provider == "netease" || track.provider == "qq" || track.provider == "kugou" || track.provider == "bodian" || track.provider == "subsonic" || track.provider == "webdav" || track.provider == "spotify") return true;
            var entry = Array.Find(TabletMusicAppUI.TabletMusicList, t => t.id == track.id);
            return entry != null && TabletMusicAppUI.IsTabletMusicUnlocked(entry);
        }
        public static string LocalPath(MusicLibraryTrack track) => System.IO.Path.Combine(LocalMusicFileImporter.GetCustomMusicDirectory(), track.fileName);
        public static bool Available(MusicLibraryTrack track)
        {
            if (!Unlocked(track) || failed.Contains(track.id)) return false;
            if (track.local) return File.Exists(LocalPath(track));
            if (track.provider == "netease" || track.provider == "qq" || track.provider == "kugou" || track.provider == "bodian" || track.provider == "subsonic" || track.provider == "webdav" || track.provider == "spotify") return true;
            var clip = Resources.Load<AudioClip>("bgm/" + track.id);
            if (clip != null) track.duration = clip.length;
            return clip != null;
        }
        public static MusicLibraryTrack RegisterNetease(NeteaseLocalBridge.RemoteTrack remote) => RegisterRemote(remote, "netease");
        public static MusicLibraryTrack RegisterRemote(NeteaseLocalBridge.RemoteTrack remote, string provider)
        {
            Ensure();
            if (remote == null || !MusicLibraryStore.ValidRemote(provider, remote.id)) return null;
            string id = MusicSourceId.Online(provider, remote.id);
            var track = state.cloudTracks.Find(t => t.id == id);
            if (track == null) { track = new MusicLibraryTrack { id = id, provider = provider, remoteId = remote.id }; state.cloudTracks.Add(track); }
            if (!string.IsNullOrEmpty(remote.title)) track.title = remote.title;
            else if (string.IsNullOrEmpty(track.title)) track.title = "在线曲目";
            if (!string.IsNullOrWhiteSpace(remote.album)) track.album = remote.album;
            if (provider == "netease")
            {
                // NetEase lists a Chinese title ("tns") for many foreign songs; every netease reply carries the field.
                string translated = (remote.translated ?? "").Trim();
                track.titleTranslation = translated.Length > 0 && translated.Length <= 80 && translated != track.title ? translated : "";
                track.metaVersion = 1;
            }
            if (Uri.TryCreate(remote.cover, UriKind.Absolute, out var coverUri) && coverUri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(coverUri.UserInfo))
            {
                if (track.coverUrl != coverUri.AbsoluteUri) MusicAlbumArtResolver.InvalidateRemote(id);
                track.coverUrl = coverUri.AbsoluteUri;
            }
            if (!string.IsNullOrEmpty(remote.artist)) track.artist = remote.artist;
            track.category = provider == "qq" ? "QQ 音乐" : provider == "kugou" ? "酷狗音乐" : provider == "bodian" ? "波点音乐" : provider == "subsonic" ? "Subsonic" : provider == "webdav" ? "WebDAV" : provider == "spotify" ? "Spotify" : "网易云音乐";
            if (remote.durationMs > 0) track.duration = remote.durationMs / 1000f;
            tracks[id] = track; failed.Remove(id);
            return track;
        }
        static readonly Dictionary<string, List<Action<bool>>> metadataPending = new Dictionary<string, List<Action<bool>>>();
        static readonly List<string> metadataQueue = new List<string>();
        static float metadataReadyAt;
        static bool metadataBatchActive;
        static readonly Dictionary<string, float> metadataAttempt = new Dictionary<string, float>();
        public static void EnsureRemoteMetadata(string id, Action<bool> complete = null, bool force = false)
        {
            var track = Find(id);
            if (track == null || track.provider != "netease" && track.provider != "spotify") { complete?.Invoke(false); return; }
            if (!force && !string.IsNullOrEmpty(track.coverUrl) && !string.IsNullOrEmpty(track.album) && (track.provider != "netease" || track.metaVersion >= 1)) { complete?.Invoke(true); return; }
            if (metadataPending.TryGetValue(id, out var callbacks)) { if (complete != null) callbacks.Add(complete); return; }
            if (!force && metadataAttempt.TryGetValue(id, out float last) && Time.realtimeSinceStartup - last < 30) { complete?.Invoke(false); return; }
            callbacks = new List<Action<bool>>(); if (complete != null) callbacks.Add(complete); metadataPending[id] = callbacks;
            metadataAttempt[id] = Time.realtimeSinceStartup;
            if (track.provider == "netease")
            {
                if (metadataQueue.Count == 0) metadataReadyAt = Time.realtimeSinceStartup + .1f;
                metadataQueue.Add(id); return;
            }
            NeteaseLocalBridge.ForProvider(track.provider).Send("metadata", new NeteaseLocalBridge.Parameters { id = track.remoteId }, response => CompleteMetadata(new[] { id }, response));
        }
        static void CompleteMetadata(IEnumerable<string> ids, NeteaseLocalBridge.Response response)
        {
            foreach (string id in ids)
            {
                var track = Find(id);
                var remote = track == null || !response.ok || response.result?.tracks == null ? null : Array.Find(response.result.tracks, item => item.id == track.remoteId);
                bool ok = remote != null;
                if (ok) RegisterRemote(remote, track.provider);
                if (!metadataPending.TryGetValue(id, out var callbacks)) continue;
                metadataPending.Remove(id);
                foreach (var callback in callbacks)
                    try { callback(ok); } catch (Exception) { Debug.LogWarning("[随身听] 曲目信息界面刷新失败"); }
            }
            Save();
        }
        static void FlushMetadata()
        {
            if (metadataBatchActive || metadataQueue.Count == 0 || Time.realtimeSinceStartup < metadataReadyAt) return;
            var ids = metadataQueue.GetRange(0, Math.Min(50, metadataQueue.Count)); metadataQueue.RemoveRange(0, ids.Count);
            var remoteIds = new List<string>(); foreach (var id in ids) { var track = Find(id); if (track != null) remoteIds.Add(track.remoteId); }
            metadataBatchActive = true;
            NeteaseLocalBridge.Instance.Send("metadata", new NeteaseLocalBridge.Parameters { ids = remoteIds.ToArray() }, response => { metadataBatchActive = false; CompleteMetadata(ids, response); });
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
        /// <summary>
        /// Removes local tracks from the library. With deleteFiles their audio and subtitle/cover companions go to the recycle bin first;
        /// a file that cannot be recycled keeps its library entry (never deleted permanently).
        /// </summary>
        public static int RemoveLocalTracks(IEnumerable<string> ids, bool deleteFiles, out int kept)
        {
            Ensure(); int removed = 0; kept = 0;
            foreach (string id in new List<string>(ids))
            {
                var track = Find(id);
                if (track == null || !track.local) continue;
                if (deleteFiles)
                {
                    string path = LocalPath(track);
                    if (!LocalMusicFileImporter.MoveToRecycleBin(path)) { kept++; continue; }
                    foreach (string companion in LocalMusicFileImporter.CompanionFiles(path)) LocalMusicFileImporter.MoveToRecycleBin(companion);
                }
                state.localTracks.RemoveAll(t => t.id == id); tracks.Remove(id);
                state.favorites.Remove(id); state.queue.RemoveAll(q => q == id);
                foreach (var list in state.playlists) list.tracks.RemoveAll(t => t == id);
                if (state.currentId == id) { MusicAudioEffectsEngine.Pause(); TabletMusicAppUI.tabletIsPlaying = false; }
                removed++;
            }
            Commit(); return removed;
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
            TrackStarted?.Invoke(id);
        }
        public static void Play(string id, bool resume = false) { failed.Remove(id); PlayInternal(id, resume, -1); }
        public static void PlayQueued(int index) => PlayQueued(index, false);
        static void PlayQueued(int index, bool advanceOnError)
        {
            Ensure();
            if (index < 0 || index >= state.queue.Count) return;
            string id = state.queue[index];
            failed.Remove(id);
            string[] snapshot = state.queue.ToArray();
            PlayInternal(id, false, -1, () =>
            {
                // Do not remove a different occurrence if the user edited the queue while loading.
                if (state.queue.Count != snapshot.Length) return;
                for (int i = 0; i < snapshot.Length; i++) if (state.queue[i] != snapshot[i]) return;
                state.queue.RemoveAt(index);
                Commit();
            }, advanceOnError);
        }
        static void PlayInternal(string id, bool resume, int fromHistory, Action onStarted = null, bool advanceOnError = true)
        {
            Ensure();
            pendingHistory = fromHistory;
            int token = ++request;
            Loading = false;
            var track = Find(id);
            if (!Available(track)) { TabletMusicAppUI.ShowToast(Unlocked(track) ? "音频文件不可用，请检查文件或重新导入" : "该曲目尚未解锁"); return; }
            float position = resume && state.currentId == id ? state.position : 0;
            // Switching by hand: silence the old track now and show the new one, instead of letting it play on until the new file is ready.
            if (track.provider != null && track.provider.Length > 0 || track.local)
            {
                MusicAudioEffectsEngine.BeginQuickSwitch();
                TabletMusicAppUI.tabletCurrentTrackTitle = track.title; TabletMusicAppUI.tabletCurrentTrackCategory = track.category;
                TabletMusicAppUI.UpdatePlayerBarUI();
            }
            if (track.provider == "netease" || track.provider == "qq" || track.provider == "kugou" || track.provider == "bodian" || track.provider == "subsonic" || track.provider == "webdav" || track.provider == "spotify")
            {
                Loading = true; Changed?.Invoke();
                NeteaseLocalBridge.ForProvider(track.provider).Send("resolve", new NeteaseLocalBridge.Parameters { id = track.remoteId }, result =>
                {
                    if (token != request) return;
                    if (!result.ok || result.result == null || string.IsNullOrEmpty(result.result.url))
                    {
                        Loading = false; failed.Add(id);
                        if (result.error == "波点当前地区不可播放" || track.provider == "spotify" && !string.IsNullOrEmpty(result.error))
                        {
                            MusicAudioEffectsEngine.Pause(); TabletMusicAppUI.tabletIsPlaying = false;
                            TabletMusicAppUI.ShowToast(result.error); Commit(); return;
                        }
                        TabletMusicAppUI.ShowToast(track.category + "曲目暂不可播放，请检查登录、版权或网络");
                        if (advanceOnError) Advance(1); else Changed?.Invoke();
                        return;
                    }
                    if (!Uri.TryCreate(result.result.url, UriKind.Absolute, out var uri) ||
                        (uri.Scheme != Uri.UriSchemeHttps && !((track.provider == "subsonic" || track.provider == "webdav" || track.provider == "spotify") && result.result.localHttp && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
                    {
                        Loading = false; failed.Add(id); TabletMusicAppUI.ShowToast("播放地址不可用或不安全");
                        if (advanceOnError) Advance(1); else Changed?.Invoke(); return;
                    }
                    var type = result.result.type == "ogg" ? AudioType.OGGVORBIS : result.result.type == "wav" ? AudioType.WAV : AudioType.MPEG;
                    var web = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(uri.AbsoluteUri, type);
                    web.timeout = 45;
                    if (track.provider == "subsonic" || track.provider == "webdav" || track.provider == "spotify") web.redirectLimit = 0;
                    if (track.provider == "webdav" && !string.IsNullOrEmpty(result.result.authorization)) web.SetRequestHeader("Authorization", result.result.authorization);
                    UnityEngine.Networking.UnityWebRequestAsyncOperation operation;
                    try { operation = web.SendWebRequest(); }
                    catch (Exception)
                    {
                        web.Dispose(); Loading = false; failed.Add(id);
                        TabletMusicAppUI.ShowToast(track.category + "无法启动音频请求，请检查服务和连接设置");
                        if (advanceOnError) Advance(1); else Changed?.Invoke(); return;
                    }
                    operation.completed += _ =>
                    {
                        AudioClip clip = null;
                        try
                        {
                            if (web.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                                clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(web);
                        }
                        catch (Exception) { /* Invalid remote audio must not leave the player loading. */ }
                        finally { web.Dispose(); }
                        if (token != request) { if (clip != null) UnityEngine.Object.Destroy(clip); return; }
                        Loading = false;
                        if (clip == null)
                        {
                            failed.Add(id); TabletMusicAppUI.ShowToast(track.category + "音频下载或解码失败");
                            if (advanceOnError) Advance(1); else Changed?.Invoke(); return;
                        }
                        TabletMusicAppUI.PlayExternalClip(track.id, track.title, track.category, clip);
                        if (position > 0 && clip.length > 0) TabletMusicAppUI.SeekTabletMusic(position / clip.length);
                        if (result.result.trial) TabletMusicAppUI.ShowToast("当前账号仅可试听片段（约 30 秒），完整播放需要该平台的会员或购买");
                        onStarted?.Invoke();
                    };
                });
                return;
            }
            if (!track.local)
            {
                TabletMusicAppUI.PlayTabletTrack(Array.Find(TabletMusicAppUI.TabletMusicList, t => t.id == id));
                if (position > 0) TabletMusicAppUI.SeekTabletMusic(position / TabletMusicAppUI.tabletCurrentClip.length);
                onStarted?.Invoke();
                return;
            }
            Loading = true; Changed?.Invoke();
            LocalMusicFileImporter.LoadAudioClipAsync(LocalPath(track), clip =>
            {
                if (token != request) { UnityEngine.Object.Destroy(clip); return; }
                Loading = false;
                TabletMusicAppUI.PlayExternalClip(track.id, track.title, track.category, clip);
                if (position > 0) TabletMusicAppUI.SeekTabletMusic(position / clip.length);
                onStarted?.Invoke();
            }, error =>
            {
                if (token != request) return;
                Loading = false; failed.Add(id);
                TabletMusicAppUI.ShowToast(advanceOnError ? "本地音频无法读取，已跳过：" + track.title : "本地音频无法读取，已保留队列条目：" + track.title);
                if (advanceOnError) Advance(1); else Changed?.Invoke();
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
                for (int i = 0; i < state.queue.Count; i++)
                {
                    if (Available(Find(state.queue[i]))) { PlayQueued(i, true); return; }
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
            FlushMetadata();
            TabletMusicAppUI.UpdateAudioProgress();
            saveTimer += delta;
            if (saveTimer >= 5) { saveTimer = 0; Save(); }
        }
    }
}
