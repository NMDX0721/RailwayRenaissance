using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using RailwayRenaissance.UI.Tablet.Account;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板存储账本：设备页 / 存储页 / 应用页都读这一份数字。
    /// 设定：这台平板是 2053 年出厂的 Xiaomi Pad 36 Ultra，标称容量很大（64 TB），但这是朝鲜的设备——绝大部分空间是“国家战略储备”，
    /// 归最高领袖所有、锁定不可用，用户真正能用的只有一小块分区。
    /// 各应用占用为设定值；本游戏自己的占用是实测：安装目录（本体与资源）、persistentDataPath（存档、设置、日志）、临时缓存目录。
    /// 目录统计放在后台线程，避免遍历大目录卡住画面；统计完成前 RealReady 为 false。
    /// </summary>
    public static class TabletStorageLedger
    {
        public const long KB = 1000L;
        public const long MB = 1000L * KB;
        public const long GB = 1000L * MB;
        public const long TB = 1000L * GB;

        public static long NominalTotalBytes => 64L * TB;
        public static long SystemBytes => 120L * GB;
        public static long UserPartitionBytes => 256L * GB;
        public static long ReservedBytes => NominalTotalBytes - SystemBytes - UserPartitionBytes;

        public struct Entry
        {
            public string name;       // 应用名（与桌面一致）；非应用条目用其它文字
            public long bytes;
            public bool isApp;
            public bool isReal;       // true = 实测值
        }

        // 桌面应用的设定占用
        private static readonly (string name, long bytes)[] AppSizes =
        {
            ("驾驶台", 18600L * MB), ("相册", 22400L * MB), ("音乐", 14200L * MB), ("地图", 9700L * MB),
            ("铁路", 7500L * MB), ("归途", 6800L * MB), ("媒姬百科", 3100L * MB), ("RDA", 2400L * MB),
            ("未来网", 2200L * MB), ("浏览器", 1900L * MB), ("阿里郎商店", 1400L * MB), ("米家", 1100L * MB),
            ("安全中心", 800L * MB), ("应用商店", 600L * MB), ("白头疫苗", 500L * MB), ("天气", 400L * MB),
            ("文件管理", 400L * MB), ("笔记", 300L * MB), ("设置", 200L * MB), ("日历", 100L * MB),
            ("时钟", 50L * MB), ("计算器", 30L * MB),
        };

        private const long SystemDataBytes = 24L * GB;   // 用户分区内的“系统与其他”

        // ---- 本游戏的真实占用（后台线程统计，每 60 秒刷新一次） ----
        private static long s_installBytes, s_saveBytes, s_cacheBytes;
        private static volatile bool s_realReady;
        private static int s_measuring;
        private static float s_measuredAt = -999f;
        private static string s_installPath, s_savePath, s_cachePath;

        /// <summary>真实占用是否已统计完（首次统计在后台进行）。</summary>
        public static bool RealReady => s_realReady;

        /// <summary>游戏本体与资源：打包后是游戏目录（含可执行文件与 _Data），编辑器里是 Assets 目录。</summary>
        public static long InstallBytes { get { EnsureMeasured(); return Interlocked.Read(ref s_installBytes); } }
        /// <summary>存档、设置、日志：persistentDataPath。</summary>
        public static long SaveBytes { get { EnsureMeasured(); return Interlocked.Read(ref s_saveBytes); } }
        /// <summary>临时缓存：temporaryCachePath。</summary>
        public static long CacheBytes { get { EnsureMeasured(); return Interlocked.Read(ref s_cacheBytes); } }
        /// <summary>本游戏占用的全部空间。</summary>
        public static long GameDataBytes => InstallBytes + SaveBytes + CacheBytes;

        /// <summary>必须在主线程调用：读取路径并启动（或按 60 秒间隔重启）后台统计。</summary>
        public static void EnsureMeasured()
        {
            if (s_realReady && Time.realtimeSinceStartup - s_measuredAt < 60f) return;
            if (Interlocked.CompareExchange(ref s_measuring, 1, 0) != 0) return;
            try
            {
                s_savePath = Application.persistentDataPath;
                s_cachePath = Application.temporaryCachePath;
                string data = Application.dataPath;
                s_installPath = Application.isEditor ? data : (Directory.GetParent(data)?.FullName ?? data);
            }
            catch (Exception) { s_measuring = 0; return; }

            s_measuredAt = Time.realtimeSinceStartup;
            Task.Run(() =>
            {
                try
                {
                    Interlocked.Exchange(ref s_installBytes, MeasureDirectory(s_installPath));
                    Interlocked.Exchange(ref s_saveBytes, MeasureDirectory(s_savePath));
                    Interlocked.Exchange(ref s_cacheBytes, MeasureDirectory(s_cachePath));
                    s_realReady = true;
                }
                catch (Exception) { }
                finally { Interlocked.Exchange(ref s_measuring, 0); }
            });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootMeasure()
        {
            if (Application.isPlaying) EnsureMeasured();   // 开局就统计，用户打开设置时多半已经算完
        }

        /// <summary>用户分区内各项占用，按大小从大到小。</summary>
        public static List<Entry> UserEntries()
        {
            var list = new List<Entry>();
            foreach (var a in AppSizes) list.Add(new Entry { name = a.name, bytes = a.bytes, isApp = true });
            list.Add(new Entry { name = "游戏本体与资源", bytes = InstallBytes, isReal = true });
            list.Add(new Entry { name = "游戏存档与日志", bytes = SaveBytes, isReal = true });
            list.Add(new Entry { name = "临时缓存", bytes = CacheBytes, isReal = true });
            list.Add(new Entry { name = "系统与其他", bytes = SystemDataBytes });
            list.Sort((x, y) => y.bytes.CompareTo(x.bytes));
            return list;
        }

        public static long UserUsedBytes
        {
            get
            {
                long sum = SystemDataBytes + GameDataBytes;
                foreach (var a in AppSizes) sum += a.bytes;
                return Math.Min(sum, UserPartitionBytes);
            }
        }

        public static long UserFreeBytes => Math.Max(0L, UserPartitionBytes - UserUsedBytes);

        /// <summary>某个应用的占用；不在表里返回 0。</summary>
        public static long AppBytes(string appName)
        {
            foreach (var a in AppSizes) if (a.name == appName) return a.bytes;
            return 0L;
        }

        public static string Format(long bytes)
        {
            if (bytes >= TB) return (bytes / (double)TB).ToString("0.##") + " TB";
            if (bytes >= GB) return (bytes / (double)GB).ToString("0.#") + " GB";
            if (bytes >= MB) return Mathf.RoundToInt(bytes / (float)MB) + " MB";
            if (bytes >= KB) return Mathf.RoundToInt(bytes / (float)KB) + " KB";
            return bytes + " B";
        }

        // ---- 云空间：标称 512 GB，其中绝大部分被“国家征用”，用户只有一小块 ----
        private const string CloudSyncKey = "TabletCloudLastSync";

        public static long CloudNominalBytes => 512L * GB;
        public static long CloudUsableBytes => 8L * GB;
        public static long CloudRequisitionedBytes => CloudNominalBytes - CloudUsableBytes;

        /// <summary>云端已用（备份完成后的总量）：设定的相册/笔记备份 + 本机存档的一份镜像。</summary>
        public static long CloudFullUsedBytes => Math.Min(CloudUsableBytes, 2100L * MB + SaveBytes);

        /// <summary>当前账号云端实际已用：从未备份过（首次登录还没上传）是 0。</summary>
        public static long CloudUsedBytes => CloudLastSyncUnix > 0 ? CloudFullUsedBytes : 0L;

        // 备份记录跟账号绑定：换账号、新注册的账号都从“尚未备份”开始
        private static string CloudKey()
        {
            return AccountService.IsRegistered ? CloudSyncKey + "_" + AccountService.Profile.xiaomiId : null;
        }

        /// <summary>当前账号上次备份的 unix 秒；未登录或从未备份返回 0。</summary>
        public static long CloudLastSyncUnix
        {
            get
            {
                string key = CloudKey();
                if (key == null) return 0;
                long.TryParse(PlayerPrefs.GetString(key, "0"), out var v);
                return v;
            }
        }

        public static void MarkCloudSynced()
        {
            string key = CloudKey();
            if (key == null) return;
            PlayerPrefs.SetString(key, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            PlayerPrefs.Save();
        }

        /// <summary>清掉当前账号的备份记录（仅测试用）。</summary>
        public static void ClearCloudSyncForCurrentAccount()
        {
            string key = CloudKey();
            if (key == null) return;
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public static string CloudLastSyncText()
        {
            long t = CloudLastSyncUnix;
            if (t <= 0) return "尚未备份";
            long d = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - t;
            if (d < 60) return "刚刚备份";
            if (d < 3600) return (d / 60) + " 分钟前备份";
            if (d < 86400) return (d / 3600) + " 小时前备份";
            return (d / 86400) + " 天前备份";
        }

        // ---- 对国家储备空间的“申请”：次数累计，措辞逐次升级 ----
        private const string AttemptKey = "TabletStorageReservedAttempts";

        private static readonly string[] Denials =
        {
            "该分区属国家战略储备，你无权访问。",
            "申请已记录，并上报所属单位保卫部门。",
            "再次提醒：储备空间归最高领袖所有，禁止申请使用。",
            "你的积极性已被记录在案。",
            "请把心思放在铁路复兴上，同志。",
            "同志，这里没有你要找的东西。",
        };

        public static int ReservedAttempts => PlayerPrefs.GetInt(AttemptKey, 0);

        /// <summary>登记一次申请并返回这一次的拒绝语。</summary>
        public static string RegisterReservedAttempt()
        {
            int n = ReservedAttempts + 1;
            PlayerPrefs.SetInt(AttemptKey, n);
            PlayerPrefs.Save();
            int idx = n - 1 < Denials.Length
                ? n - 1
                : Denials.Length - 2 + (n - 1 - Denials.Length) % 2;   // 用完后在最后两句之间轮换
            return Denials[idx];
        }

        private static long MeasureDirectory(string path)
        {
            long total = 0;
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return 0;
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { total += new FileInfo(f).Length; }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            return total;
        }
    }
}
