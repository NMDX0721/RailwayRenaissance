using UnityEngine;
using UnityEngine.Profiling;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听的真实内存占用（字节，来自 Unity Profiler 对每个纹理 / 音频 / 渲染纹理的实测），
    /// 以及被系统回收时的瘦身。设置 App 等系统界面读 Measure() 即可显示，不需要知道音乐内部缓存。
    /// </summary>
    public static class MusicMemory
    {
        public struct Usage
        {
            public long covers, audio, stage, icons;   // 封面缓存、已解码的曲目音频、舞台背景渲染纹理、矢量图标缓存
            public int coverCount;
            public long Total => covers + audio + stage + icons;
        }

        public static Usage Measure()
        {
            var u = new Usage();
            u.covers = MusicAlbumArtResolver.CacheBytes(out u.coverCount);
            u.audio = MusicAudioEffectsEngine.ClipBytes();
            u.stage = MusicReactiveStageView.BackdropBytes;
            u.icons = MusicIconHelper.IconCacheBytes();
            return u;
        }

        /// <summary>被系统回收后丢掉可重建的缓存：封面和主题色表。仍被界面引用的资源由 Unity 保留，其余在卸载未使用资源时释放。</summary>
        public static void Trim()
        {
            MusicAlbumArtResolver.TrimCache();
            MusicReactiveStageView.TrimPalettes();
            Resources.UnloadUnusedAssets();
        }

        public static long Bytes(Object o) => o == null ? 0 : Profiler.GetRuntimeMemorySizeLong(o);

        public static string Format(long bytes)
        {
            if (bytes >= 1L << 30) return (bytes / (double)(1L << 30)).ToString("0.00") + " GB";
            if (bytes >= 1L << 20) return (bytes / (double)(1L << 20)).ToString("0.0") + " MB";
            return (bytes / 1024.0).ToString("0") + " KB";
        }
    }
}
