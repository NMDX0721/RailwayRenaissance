using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板运行内存账本（和 TabletStorageLedger 同一思路）：
    /// 设定上这台平板是 64 GB LPDDR9 Ultra，但绝大部分被“国家战略运算”保留，用户只有一小块分区。
    /// 玩家可用内存与占用取自本机真实数据；本游戏（音乐、画面与剧情）之外的占用记为“系统与其他程序”。
    /// </summary>
    public static class TabletMemoryLedger
    {
        // 内存按二进制计：32 GB 就是 32 条 1 GiB
        public const long MB = 1024L * 1024L;
        public const long GB = 1024L * MB;

        public static string Format(long bytes)
        {
            if (bytes >= GB) return (bytes / (double)GB).ToString("0.#") + " GB";
            return Mathf.RoundToInt(bytes / (float)MB) + " MB";
        }

        public static long NominalBytes => 64L * GB;
        public static long SystemBytes => 3L * GB;         // 系统与军工基带
        /// <summary>玩家可用内存 = 本机真实物理内存。</summary>
        public static long UserPartitionBytes => Math.Max(1L, PhysicalTotalBytes);
        /// <summary>国家战略运算保留：标称容量里扣掉系统和玩家可用的部分，锁定。</summary>
        public static long ReservedBytes => Math.Max(0L, NominalBytes - SystemBytes - UserPartitionBytes);

        public struct Entry
        {
            public string name;
            public long bytes;
            public bool isApp;
            public bool isReal;
            public string note;
        }

        // ---- 本机内存（Windows 用系统接口取真实的总量与剩余；其他平台只知道总量） ----

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private class MemoryStatusEx
        {
            public uint dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MemoryStatusEx));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] MemoryStatusEx buffer);
#endif

        private static bool TryReadSystem(out long total, out long avail)
        {
            total = 0; avail = 0;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var m = new MemoryStatusEx();
                if (GlobalMemoryStatusEx(m)) { total = (long)m.ullTotalPhys; avail = (long)m.ullAvailPhys; return total > 0; }
            }
            catch (Exception) { }
#elif UNITY_ANDROID || UNITY_STANDALONE_LINUX
            // Android 8+ / Linux：/proc/meminfo 任何应用都能读
            try
            {
                long memTotal = 0, memAvail = -1, memFree = 0, cached = 0;
                foreach (var line in System.IO.File.ReadLines("/proc/meminfo"))
                {
                    if (line.StartsWith("MemTotal:")) memTotal = ParseKb(line);
                    else if (line.StartsWith("MemAvailable:")) memAvail = ParseKb(line);
                    else if (line.StartsWith("MemFree:")) memFree = ParseKb(line);
                    else if (line.StartsWith("Cached:")) cached = ParseKb(line);
                }
                if (memAvail < 0) memAvail = memFree + cached;   // 老内核没有 MemAvailable
                if (memTotal > 0) { total = memTotal; avail = Math.Min(memAvail, memTotal); return true; }
            }
            catch (Exception) { }
#endif
            return false;
        }

#if UNITY_ANDROID || UNITY_STANDALONE_LINUX
        private static long ParseKb(string line)
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 && long.TryParse(parts[1], out long kb) ? kb * 1024L : 0L;
        }
#endif

        /// <summary>系统报告的可用内存会比内存条标称少一点（硬件保留），取整到常见的内存条容量（如 31.2 → 32 GB）。</summary>
        public static long PhysicalTotalBytes
        {
            get
            {
                long raw = TryReadSystem(out long t, out _) ? t : SystemInfo.systemMemorySize * 1024L * 1024L;
                // 常见的整机内存规格（手机常有 6/12 GB，电脑多为 2 的幂）；差得太多就不取整
                foreach (long g in new long[] { 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192, 256 })
                {
                    long size = g * GB;
                    if (size >= raw) return raw > 0 && size - raw <= size / 8 ? size : raw;
                }
                return raw;
            }
        }

        /// <summary>本游戏进程实际占用的物理内存。</summary>
        public static long GameBytes
        {
            get
            {
                try
                {
                    long ws = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                    if (ws > 0) return ws;
                }
                catch (Exception) { }
#if UNITY_ANDROID || UNITY_STANDALONE_LINUX
                try
                {
                    foreach (var line in System.IO.File.ReadLines("/proc/self/status"))
                        if (line.StartsWith("VmRSS:")) { long v = ParseKb(line); if (v > 0) return v; }
                }
                catch (Exception) { }
#endif
                return Profiler.GetTotalReservedMemoryLong();
            }
        }

        public static long MusicBytes => MusicMemory.Measure().Total;
        /// <summary>游戏本体（画面、场景、剧情）占用：进程总量扣掉随身听。</summary>
        public static long GameBodyBytes => Math.Max(0L, GameBytes - MusicBytes);

        /// <summary>整机已用：本机真实已用；读不到时只算游戏自己的占用。</summary>
        public static long UserUsedBytes
        {
            get
            {
                if (TryReadSystem(out _, out long a)) return Math.Max(GameBytes, PhysicalTotalBytes - a);
                return GameBytes;
            }
        }

        /// <summary>游戏之外的占用，记为系统与其他程序。</summary>
        public static long OtherUsedBytes => Math.Max(0L, UserUsedBytes - GameBytes);

        public static long UserFreeBytes => Math.Max(0L, UserPartitionBytes - UserUsedBytes);

        public static List<Entry> UserEntries()
        {
            var list = new List<Entry>();
            list.Add(new Entry { name = "音乐", bytes = MusicBytes, isApp = true, isReal = true });
            list.Add(new Entry { name = "游戏画面与剧情", bytes = GameBodyBytes, isReal = true });
            list.Add(new Entry
            {
                name = "系统与其他程序", bytes = OtherUsedBytes,
                note = "先民军工固件、后台服务与其他程序，由系统统一调度",
            });
            list.Sort((x, y) => y.bytes.CompareTo(x.bytes));
            return list;
        }

        // ---- 对国家战略运算保留的“申请”：次数累计，措辞逐次升级 ----
        private const string AttemptKey = "TabletMemoryReservedAttempts";

        private static readonly string[] Denials =
        {
            "该内存属国家战略运算保留，你无权调用。",
            "申请已记录，并上报所属单位保卫部门。",
            "再次提醒：保留内存正在为卫星链路与国家算力调度服务。",
            "你的积极性已被记录在案。",
            "请把心思放在铁路复兴上，同志。",
            "同志，这里没有你要找的东西。",
        };

        public static int ReservedAttempts => PlayerPrefs.GetInt(AttemptKey, 0);

        public static string RegisterReservedAttempt()
        {
            int n = ReservedAttempts + 1;
            PlayerPrefs.SetInt(AttemptKey, n);
            PlayerPrefs.Save();
            int idx = n - 1 < Denials.Length ? n - 1 : Denials.Length - 2 + (n - 1 - Denials.Length) % 2;
            return Denials[idx];
        }
    }
}
