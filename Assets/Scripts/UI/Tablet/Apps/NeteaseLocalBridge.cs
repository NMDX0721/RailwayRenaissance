using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // Owns one private child process. All callbacks are delivered on Unity's main thread.
    public sealed class NeteaseLocalBridge : MonoBehaviour
    {
        [Serializable] public sealed class Request { public int id; public string method; public Parameters @params; }
        [Serializable] public sealed class Parameters { public string id, userId, keywords, host, username, password; public int offset; }
        [Serializable] public sealed class Response { public int id; public bool ok; public string error; public Result result; }
        [Serializable] public sealed class Result
        {
            public string service, image, message, userId, nickname, url, type, lyric, translation, authorization;
            public int protocol, code, nextOffset;
            public bool loggedIn, trial, sessionOnly, localHttp, more;
            public RemoteTrack[] tracks;
            public RemotePlaylist[] playlists;
        }
        [Serializable] public sealed class RemoteTrack { public string id, title, artist, album, cover; public long durationMs; }
        [Serializable] public sealed class RemotePlaylist { public string id, title; public int count; }
        sealed class Pending { public Action<Response> callback; public float deadline; }
        static NeteaseLocalBridge instance, qqInstance, kugouInstance, bodianInstance, subsonicInstance, webdavInstance;
        string provider = "netease";
        readonly object sync = new object();
        readonly Queue<string> lines = new Queue<string>();
        readonly Dictionary<int, Pending> pending = new Dictionary<int, Pending>();
        Process process;
        int nextId;
        volatile bool exited;
        public static NeteaseLocalBridge Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("NeteaseLocalBridge");
                DontDestroyOnLoad(go);
                return instance = go.AddComponent<NeteaseLocalBridge>();
            }
        }
        public static NeteaseLocalBridge ForProvider(string source)
        {
            if (source != "qq" && source != "kugou" && source != "bodian" && source != "subsonic" && source != "webdav") return Instance;
            var existing = source == "qq" ? qqInstance : source == "kugou" ? kugouInstance : source == "bodian" ? bodianInstance : source == "subsonic" ? subsonicInstance : webdavInstance;
            if (existing != null) return existing;
            var go = new GameObject(source == "qq" ? "QqLocalBridge" : source == "kugou" ? "KugouLocalBridge" : source == "bodian" ? "BodianLocalBridge" : source == "subsonic" ? "SubsonicLocalBridge" : "WebdavLocalBridge"); DontDestroyOnLoad(go);
            var created = go.AddComponent<NeteaseLocalBridge>(); created.provider = source;
            if (source == "qq") qqInstance = created; else if (source == "kugou") kugouInstance = created; else if (source == "bodian") bodianInstance = created; else if (source == "subsonic") subsonicInstance = created; else webdavInstance = created;
            return created;
        }
        public bool StartService(out string error)
        {
            error = null;
            if (process != null && !exited && !process.HasExited) return true;
            StopService();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            string service = Path.Combine(Application.streamingAssetsPath, provider == "qq" ? "QQMusicService" : provider == "kugou" ? "KugouMusicService" : provider == "bodian" ? "BodianMusicService" : provider == "subsonic" ? "SubsonicMusicService" : provider == "webdav" ? "WebdavMusicService" : "MusicService", "bridge.js");
#if UNITY_EDITOR
            if (!File.Exists(service)) service = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "music", provider == "qq" ? "qq-service" : provider == "kugou" ? "kugou-service" : provider == "bodian" ? "bodian-service" : provider == "subsonic" ? "subsonic-service" : provider == "webdav" ? "webdav-service" : "netease-service", "bridge.js"));
#endif
            if (!File.Exists(service)) { error = (provider == "qq" ? "QQ 音乐" : provider == "kugou" ? "酷狗" : provider == "bodian" ? "波点" : provider == "subsonic" ? "Subsonic" : provider == "webdav" ? "WebDAV" : "网易云") + "服务未安装"; return false; }
            string node = Path.Combine(Path.GetDirectoryName(service), "node.exe");
            if (!File.Exists(node) && provider != "netease" && provider != "bodian") node = Path.Combine(Application.streamingAssetsPath, "MusicService", "node.exe");
            if (provider == "bodian" && (!File.Exists(node) || FileVersionInfo.GetVersionInfo(node).FileMajorPart < 24))
            {
                node = null;
                foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                {
                    try
                    {
                        string candidate = Path.Combine(directory.Trim().Trim('"'), "node.exe");
                        if (File.Exists(candidate) && FileVersionInfo.GetVersionInfo(candidate).FileMajorPart >= 24) { node = candidate; break; }
                    }
                    catch (Exception) { }
                }
                if (node == null) { error = "波点需要 Node 24 或更高版本"; return false; }
            }
            if (!File.Exists(node)) node = "node";
            try
            {
                var start = new ProcessStartInfo { FileName = node, Arguments = "\"" + service + "\"", UseShellExecute = false,
                    CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(service), StandardInputEncoding = new System.Text.UTF8Encoding(false),
                    StandardOutputEncoding = new System.Text.UTF8Encoding(false), StandardErrorEncoding = new System.Text.UTF8Encoding(false) };
                process = new Process { StartInfo = start, EnableRaisingEvents = true };
                process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (sync) lines.Enqueue(e.Data); };
                process.ErrorDataReceived += (_, e) => { }; // Never log provider output or session data.
                var startedProcess = process;
                process.Exited += (_, __) => { if (ReferenceEquals(process, startedProcess)) exited = true; };
                exited = false;
                if (!process.Start()) throw new InvalidOperationException();
                process.BeginOutputReadLine(); process.BeginErrorReadLine();
                return true;
            }
            catch (Exception) { StopService(); error = (provider == "qq" ? "QQ 音乐" : provider == "kugou" ? "酷狗" : provider == "bodian" ? "波点" : provider == "subsonic" ? "Subsonic" : provider == "webdav" ? "WebDAV" : "网易云") + "服务启动失败，请检查本机服务与 Node 运行时"; return false; }
#else
            error = "在线音乐服务目前仅支持 Windows"; return false;
#endif
        }
        public void Send(string method, Parameters args, Action<Response> callback)
        {
            if (!StartService(out var error)) { callback?.Invoke(new Response { error = error }); return; }
            int id = ++nextId;
            pending[id] = new Pending { callback = callback, deadline = Time.realtimeSinceStartup + (provider == "qq" && method == "resolve" ? 90f : 45f) };
            try { process.StandardInput.WriteLine(JsonUtility.ToJson(new Request { id = id, method = method, @params = args ?? new Parameters() })); process.StandardInput.Flush(); }
            catch (Exception) { Complete(id, new Response { id = id, error = "音乐服务通信失败，请重试" }); }
        }
        void Update()
        {
            while (true)
            {
                string line;
                lock (sync) { if (lines.Count == 0) break; line = lines.Dequeue(); }
                try { var response = JsonUtility.FromJson<Response>(line); if (response != null) Complete(response.id, response); }
                catch (Exception) { /* Ignore malformed child output. */ }
            }
            var expired = new List<int>();
            foreach (var pair in pending) if (exited || Time.realtimeSinceStartup >= pair.Value.deadline) expired.Add(pair.Key);
            if (expired.Count > 0)
            {
                string error = exited ? "音乐服务已退出" : "音乐服务请求超时，请重试";
                StopService();
                var remaining = new List<int>(pending.Keys);
                foreach (int id in remaining) Complete(id, new Response { id = id, error = error });
            }
        }
        void Complete(int id, Response response)
        {
            if (!pending.TryGetValue(id, out var item)) return;
            pending.Remove(id); item.callback?.Invoke(response);
        }
        void StopService()
        {
            var owned = process; process = null; exited = true;
            if (owned == null) return;
            try { if (!owned.HasExited) owned.Kill(); } catch (Exception) { }
            owned.Dispose();
        }
        void OnDestroy() { if (instance == this) instance = null; if (qqInstance == this) qqInstance = null; if (kugouInstance == this) kugouInstance = null; if (bodianInstance == this) bodianInstance = null; if (subsonicInstance == this) subsonicInstance = null; if (webdavInstance == this) webdavInstance = null; StopService(); }
        void OnApplicationQuit() => StopService();
    }
}

