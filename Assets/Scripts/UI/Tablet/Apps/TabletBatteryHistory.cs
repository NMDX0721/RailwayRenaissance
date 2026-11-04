using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 设备真实电量的历史采样：游戏运行期间每分钟读一次本机电量（Windows 系统电源状态 / Android 电池接口），
    /// 近 24 小时保存在 PlayerPrefs，供设置里的电池曲线绘制。游戏没运行的时段没有采样，曲线如实留空。
    /// </summary>
    public static class TabletBatteryHistory
    {
        public struct Sample
        {
            public long unix;
            public int percent;
            public bool charging;
        }

        private const string Key = "TabletBatteryHistory";
        private const int MaxSamples = 400;
        private const long WindowSeconds = 24L * 3600L;
        private const long MinGapSeconds = 120L;

        private static List<Sample> s_samples;

        public static IReadOnlyList<Sample> Samples
        {
            get { Load(); return s_samples; }
        }

        public static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>读取当前电量并追加一个采样（读不到电量时忽略；间隔过短且无变化时跳过）。</summary>
        public static void Record()
        {
            float level = GameMainUI.GetDeviceBatteryLevel01();
            if (level < 0f) return;
            Load();
            int pct = Mathf.RoundToInt(level * 100f);
            bool charging = GameMainUI.GetDeviceCharging();
            long now = NowUnix();
            if (s_samples.Count > 0)
            {
                var last = s_samples[s_samples.Count - 1];
                bool unchanged = last.percent == pct && last.charging == charging;
                if (now - last.unix < MinGapSeconds && (unchanged || now - last.unix < 20L)) return;
                if (now < last.unix) s_samples.Clear();   // 系统时间被调回，旧记录不可信
            }
            s_samples.Add(new Sample { unix = now, percent = pct, charging = charging });
            Trim(now);
            Save();
        }

        private static void Trim(long now)
        {
            s_samples.RemoveAll(s => now - s.unix > WindowSeconds);
            if (s_samples.Count > MaxSamples) s_samples.RemoveRange(0, s_samples.Count - MaxSamples);
        }

        private static void Load()
        {
            if (s_samples != null) return;
            s_samples = new List<Sample>();
            string raw = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var part in raw.Split(';'))
            {
                var f = part.Split(',');
                if (f.Length != 3) continue;
                if (!long.TryParse(f[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long unix)) continue;
                if (!int.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pct)) continue;
                s_samples.Add(new Sample { unix = unix, percent = Mathf.Clamp(pct, 0, 100), charging = f[2] == "1" });
            }
            Trim(NowUnix());
        }

        private static void Save()
        {
            var sb = new StringBuilder(s_samples.Count * 16);
            for (int i = 0; i < s_samples.Count; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(s_samples[i].unix.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(s_samples[i].percent.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(s_samples[i].charging ? '1' : '0');
            }
            PlayerPrefs.SetString(Key, sb.ToString());
            PlayerPrefs.Save();
        }

        // 播放时自动常驻的采样器
        private sealed class Sampler : MonoBehaviour
        {
            private float _next;

            private void Start()
            {
                Record();
                _next = Time.unscaledTime + 60f;
            }

            private void Update()
            {
                if (Time.unscaledTime < _next) return;
                _next = Time.unscaledTime + 60f;
                Record();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying) return;
            var go = new GameObject("TabletBatterySampler") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Sampler>();
        }
    }
}
