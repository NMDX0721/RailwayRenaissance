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
    /// 玩家可用分区取自本机真实磁盘（容量、已用、剩余）；其中本游戏的占用是实测，其余记为“系统与其他程序”。
    /// 本游戏占用的三项：安装目录（本体与资源）、persistentDataPath（存档、设置、日志）、临时缓存目录。
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
        /// <summary>玩家可用分区 = 本机游戏所在磁盘的真实容量；还没统计出来时先按 256 GB 显示。</summary>
        public static long UserPartitionBytes => s_driveTotal > 0 ? s_driveTotal : 256L * GB;
        public static long ReservedBytes => Math.Max(0L, NominalTotalBytes - SystemBytes - UserPartitionBytes);

        public struct Entry
        {
            public string name;       // 应用名（与桌面一致）；非应用条目用其它文字
            public long bytes;
            public bool isApp;
            public bool isReal;       // true = 本游戏的实测值
            public string note;       // 点一下时的说明
        }

        // ---- 本游戏的真实占用（后台线程统计，每 60 秒刷新一次） ----
        private static long s_installBytes, s_saveBytes, s_cacheBytes;
        private static long s_driveTotal, s_driveFree;   // 游戏所在磁盘的真实容量与剩余
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
                    MeasureDrive(s_installPath);
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

        /// <summary>用户分区内各项占用，按大小从大到小。游戏之外的占用统一记为“系统与其他程序”。</summary>
        public static List<Entry> UserEntries()
        {
            var list = new List<Entry>();
            list.Add(new Entry { name = "游戏本体与资源", bytes = InstallBytes, isReal = true });
            list.Add(new Entry { name = "游戏存档与日志", bytes = SaveBytes, isReal = true });
            list.Add(new Entry { name = "临时缓存", bytes = CacheBytes, isReal = true });
            list.Add(new Entry
            {
                name = "系统与其他程序", bytes = OtherUsedBytes,
                note = "先民军工固件、各应用的数据与其他文件，由系统统一管理",
            });
            list.Sort((x, y) => y.bytes.CompareTo(x.bytes));
            return list;
        }

        /// <summary>分区已用：本机磁盘的真实已用量（统计出来之前只算游戏自己的占用）。</summary>
        public static long UserUsedBytes
        {
            get
            {
                if (s_driveTotal > 0) return Math.Max(GameDataBytes, s_driveTotal - Interlocked.Read(ref s_driveFree));
                return GameDataBytes;
            }
        }

        /// <summary>游戏之外的已用空间。</summary>
        public static long OtherUsedBytes => Math.Max(0L, UserUsedBytes - GameDataBytes);

        public static long UserFreeBytes => Math.Max(0L, UserPartitionBytes - UserUsedBytes);

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

        private static void MeasureDrive(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android 8+：DriveInfo 不可靠，用系统的 StatFs 读应用数据所在分区（后台线程要先挂接 JNI）
            try
            {
                AndroidJNI.AttachCurrentThread();
                using (var stat = new AndroidJavaObject("android.os.StatFs", s_savePath ?? path))
                {
                    long total = stat.Call<long>("getTotalBytes");
                    long free = stat.Call<long>("getAvailableBytes");
                    if (total > 0)
                    {
                        Interlocked.Exchange(ref s_driveTotal, total);
                        Interlocked.Exchange(ref s_driveFree, Math.Min(free, total));
                        return;
                    }
                }
            }
            catch (Exception) { }
#endif
            try
            {
                string root = string.IsNullOrEmpty(path) ? null : Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root)) return;
                var d = new DriveInfo(root);
                if (!d.IsReady) return;
                long total = d.TotalSize, free = d.AvailableFreeSpace;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                // 玩家说的“1T 硬盘”是整块物理盘，不是游戏所在的那个分区：取整盘容量，已用累加同一块盘上的所有分区
                int disk = DiskNumberOf(root);
                if (disk >= 0)
                {
                    long diskSize = 0, used = 0;
                    foreach (var v in DriveInfo.GetDrives())
                    {
                        try
                        {
                            if (v.DriveType != DriveType.Fixed || !v.IsReady) continue;
                            if (DiskNumberOf(v.Name) != disk) continue;
                            diskSize += v.TotalSize;
                            used += v.TotalSize - v.AvailableFreeSpace;
                        }
                        catch (Exception) { }
                    }
                    if (diskSize >= total && used > 0 && used <= diskSize) { total = diskSize; free = diskSize - used; }
                }
#endif
                Interlocked.Exchange(ref s_driveTotal, total);
                Interlocked.Exchange(ref s_driveFree, free);
            }
            catch (Exception) { }
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr sa, uint disp, uint flags, IntPtr tmpl);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(IntPtr h, uint code, IntPtr inBuf, uint inSize, byte[] outBuf, uint outSize, out uint ret, IntPtr ov);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr h);

        private static IntPtr OpenDevice(string path)
        {
            // access = 0：只查询信息，不需要管理员权限
            return CreateFileW(path, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
        }

        /// <summary>盘符所在的物理磁盘编号；查不到返回 -1。</summary>
        private static int DiskNumberOf(string root)
        {
            IntPtr h = new IntPtr(-1);
            try
            {
                h = OpenDevice(@"\\.\" + root.Substring(0, 2));
                if (h == new IntPtr(-1)) return -1;
                var buf = new byte[12];   // DeviceType, DeviceNumber, PartitionNumber
                if (!DeviceIoControl(h, 0x2D1080, IntPtr.Zero, 0, buf, 12, out _, IntPtr.Zero)) return -1;
                // 只认真正的磁盘分区（设备类型 7，且有分区号）；虚拟盘符不算
                if (BitConverter.ToInt32(buf, 0) != 7 || BitConverter.ToInt32(buf, 8) < 1) return -1;
                return BitConverter.ToInt32(buf, 4);
            }
            catch (Exception) { return -1; }
            finally { if (h != new IntPtr(-1)) CloseHandle(h); }
        }

        private static long PhysicalDiskSize(int disk)
        {
            IntPtr h = new IntPtr(-1);
            try
            {
                h = OpenDevice(@"\\.\PhysicalDrive" + disk);
                if (h == new IntPtr(-1)) return 0;
                var buf = new byte[8];
                if (!DeviceIoControl(h, 0x7405C, IntPtr.Zero, 0, buf, 8, out _, IntPtr.Zero)) return 0;
                return BitConverter.ToInt64(buf, 0);
            }
            catch (Exception) { return 0; }
            finally { if (h != new IntPtr(-1)) CloseHandle(h); }
        }
#endif

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
