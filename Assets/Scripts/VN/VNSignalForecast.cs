using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 剧情信号灯的"前方预告"，沿用日本五灯式色灯信号的递进逻辑：
/// 前方出现停止(红)信号时，往回依次是 警戒(黄黄) → 注意(黄) → 减速(黄绿) → 进行(绿)。
/// 剧情里人工标注的"危机/转折点"（台词的 sig:"stop"）就是前方的红灯；
/// 按估算阅读时间，距离危机 ≤60 秒＝警戒，≤150 秒＝注意，≤300 秒（约 5 分钟）＝减速，更远＝进行；
/// 读到危机段落内显示停止，过后恢复。接近话末时会继续向下一话看。
/// 只读取作者标注的 sig，不分析台词内容，所以不会剧透标注之外的东西。
/// </summary>
public static class VNSignalForecast
{
    public const string Stop = "stop";
    const float HoldSeconds = 40f;   // 危机段落持续（读到标注句之后多久仍显示红灯）
    const float Warn = 60f, Attention = 150f, Slow = 300f;

    sealed class Track
    {
        public float[] start;       // 每句开始的累计阅读时间（按剧本顺序摊平）
        public int[] sceneBase;     // 各场景第一句在摊平序列里的下标
        public float total;
        public List<float> stops = new List<float>();
    }

    static readonly Dictionary<ScriptData, Track> tracks = new Dictionary<ScriptData, Track>();
    static readonly Dictionary<string, ScriptData> loaded = new Dictionary<string, ScriptData>();

    /// <summary>本话（及后续话）没有任何标注时返回 false，调用方退回预设氛围段。</summary>
    public static bool TryAspect(ScriptData script, int scene, int dialogue, out VNSignalAspect aspect)
    {
        aspect = VNSignalAspect.Proceed;
        var track = TrackOf(script);
        if (track == null || scene < 0 || scene >= track.sceneBase.Length) return false;
        int flat = track.sceneBase[scene] + Mathf.Max(0, dialogue);
        if (flat >= track.start.Length) flat = track.start.Length - 1;
        float now = track.start[flat];

        float untilStop = float.MaxValue;
        bool any = track.stops.Count > 0;
        foreach (float stop in track.stops)
        {
            if (now >= stop + HoldSeconds) continue;      // 已读过
            if (now >= stop) { aspect = VNSignalAspect.Stop; return true; }
            untilStop = stop - now; break;
        }
        if (untilStop == float.MaxValue && !string.IsNullOrEmpty(script.nextScript))
        {
            var next = TrackOf(Load(script.nextScript));
            if (next != null && next.stops.Count > 0)
            {
                any = true;
                untilStop = (track.total - now) + next.stops[0];
            }
        }
        if (!any) return false;
        aspect = untilStop <= Warn ? VNSignalAspect.Restricted
               : untilStop <= Attention ? VNSignalAspect.Caution
               : untilStop <= Slow ? VNSignalAspect.Reduced
               : VNSignalAspect.Proceed;
        return true;
    }

    static ScriptData Load(string name)
    {
        if (loaded.TryGetValue(name, out var cached)) return cached;
        ScriptData script = null;
        var asset = Resources.Load<TextAsset>("Scripts/" + name);
        if (asset != null)
        {
            try { script = JsonUtility.FromJson<ScriptData>(asset.text); }
            catch { script = null; }
        }
        loaded[name] = script;
        return script;
    }

    static Track TrackOf(ScriptData script)
    {
        if (script?.scenes == null) return null;
        if (tracks.TryGetValue(script, out var track)) return track;
        track = new Track { sceneBase = new int[script.scenes.Length] };
        var starts = new List<float>();
        float time = 0f;
        for (int s = 0; s < script.scenes.Length; s++)
        {
            track.sceneBase[s] = starts.Count;
            var entries = script.scenes[s]?.d;
            if (entries == null) continue;
            foreach (var entry in entries)
            {
                starts.Add(time);
                if (entry != null && entry.sig == Stop) track.stops.Add(time);
                time += ReadSeconds(entry);
            }
        }
        if (starts.Count == 0) starts.Add(0f);
        track.start = starts.ToArray();
        track.total = time;
        tracks[script] = track;
        return track;
    }

    /// <summary>估算读完一句的秒数：约每秒 7.5 字 + 1.2 秒翻页；地点卡约 2.5 秒；选项给 8 秒思考。</summary>
    static float ReadSeconds(DialogueEntry entry)
    {
        if (entry == null) return 0f;
        if (entry.t == "loc") return 2.5f;
        if (entry.opts != null && entry.opts.Length > 0) return 8f;
        int len = string.IsNullOrEmpty(entry.text) ? 0 : entry.text.Length;
        return len == 0 ? 1.5f : 1.2f + len / 7.5f;
    }
}
