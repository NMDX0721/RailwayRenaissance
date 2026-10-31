using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // Debounced views share one in-flight request and the latest waiting term per source.
    public static class MusicCatalogSearch
    {
        sealed class Request
        {
            public string term;
            public readonly List<Action<NeteaseLocalBridge.Response>> callbacks = new List<Action<NeteaseLocalBridge.Response>>();
        }
        sealed class Cached { public float time; public NeteaseLocalBridge.Response response; }
        static readonly Dictionary<string, Request> active = new Dictionary<string, Request>();
        static readonly Dictionary<string, Request> waiting = new Dictionary<string, Request>();
        static readonly Dictionary<string, Cached> cache = new Dictionary<string, Cached>();
        static readonly Queue<string> order = new Queue<string>();
        public static readonly string[] Sources = { "netease", "qq", "kugou", "bodian", "spotify", "subsonic", "webdav" };
        public static string Label(string source) => source == "qq" ? "QQ 音乐" : source == "kugou" ? "酷狗" : source == "bodian" ? "波点" : source == "spotify" ? "Spotify" : source == "subsonic" ? "Subsonic" : source == "webdav" ? "WebDAV" : "网易云";
        public static void Invalidate() { cache.Clear(); order.Clear(); }
        public static void Search(string source, string term, Action<NeteaseLocalBridge.Response> callback)
        {
            term = (term ?? "").Trim();
            if (Array.IndexOf(Sources, source) < 0 || term.Length == 0) { callback(new NeteaseLocalBridge.Response { error = "请输入搜索内容" }); return; }
            string key = source + "\n" + term;
            if (cache.TryGetValue(key, out var saved) && Time.realtimeSinceStartup - saved.time < 60) { callback(saved.response); return; }
            if (active.TryGetValue(source, out var underway))
            {
                if (underway.term == term)
                {
                    if (waiting.TryGetValue(source, out var obsolete))
                    {
                        waiting.Remove(source);
                        foreach (var listener in obsolete.callbacks) listener(new NeteaseLocalBridge.Response { error = "搜索已更新" });
                    }
                    underway.callbacks.Add(callback); return;
                }
                if (waiting.TryGetValue(source, out var old))
                {
                    if (old.term == term) { old.callbacks.Add(callback); return; }
                    foreach (var listener in old.callbacks) listener(new NeteaseLocalBridge.Response { error = "搜索已更新" });
                }
                var next = new Request { term = term }; next.callbacks.Add(callback); waiting[source] = next; return;
            }
            var request = new Request { term = term }; request.callbacks.Add(callback); Start(source, request);
        }
        static void Start(string source, Request request)
        {
            active[source] = request;
            NeteaseLocalBridge.ForProvider(source).Send("search", new NeteaseLocalBridge.Parameters { keywords = request.term }, response => {
                if (response.ok && response.result != null)
                {
                    string key = source + "\n" + request.term;
                    if (!cache.ContainsKey(key)) order.Enqueue(key);
                    cache[key] = new Cached { time = Time.realtimeSinceStartup, response = response };
                    while (order.Count > 24) cache.Remove(order.Dequeue());
                }
                active.Remove(source);
                if (waiting.TryGetValue(source, out var next)) { waiting.Remove(source); Start(source, next); }
                foreach (var listener in request.callbacks) listener(response);
            });
        }
    }
}
