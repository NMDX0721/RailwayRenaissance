using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听实体唱片与原声封套解析器 (MusicAlbumArtResolver)
    /// 纯代码绘制 16-bit 像素工业复古封套：包含 4x4 Bayer 像素抖动渐变天空、
    /// 远景山脊与地平线光晕、透视枕木与钢轨高光、专属世界观叙事主体、
    /// 左侧复古磁带布脊、四角黄铜铆钉护角与右下角防伪金箔印记。
    /// </summary>
    public static class MusicAlbumArtResolver
    {
        private static readonly Dictionary<string, Texture2D> s_coverCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, List<Action<Texture2D>>> s_remoteRequests = new Dictionary<string, List<Action<Texture2D>>>();
        private static readonly HashSet<string> s_remoteFailed = new HashSet<string>();
        private static readonly HashSet<string> s_ownedCovers = new HashSet<string>();
        private static readonly Dictionary<string, Color> s_paletteCache = new Dictionary<string, Color>();

        // 经典调频暖调工业色板
        private static readonly Color C_BrassGold = new Color(0.96f, 0.82f, 0.45f, 1f);
        private static readonly Color C_BrassDark = new Color(0.83f, 0.66f, 0.26f, 1f);
        private static readonly Color C_IronDark = new Color(0.14f, 0.15f, 0.18f, 1f);
        private static readonly Color C_VinylGroove = new Color(0.08f, 0.08f, 0.09f, 1f);
        private static readonly Color C_DesertSand = new Color(0.76f, 0.58f, 0.35f, 1f);
        private static readonly Color C_RustOrange = new Color(0.82f, 0.40f, 0.22f, 1f);
        private static readonly Color C_SkyDeep = new Color(0.10f, 0.12f, 0.20f, 1f);

        // 4x4 Bayer 像素抖动矩阵 (用于模拟 16-bit 经典像素阶梯过渡)
        private static readonly float[] Bayer4x4 = new float[16]
        {
             0f / 16f,  8f / 16f,  2f / 16f, 10f / 16f,
            12f / 16f,  4f / 16f, 14f / 16f,  6f / 16f,
             3f / 16f, 11f / 16f,  1f / 16f,  9f / 16f,
            15f / 16f,  7f / 16f, 13f / 16f,  5f / 16f
        };

        private static float GetBayer(int x, int y)
        {
            return Bayer4x4[(y & 3) * 4 + (x & 3)];
        }

        public static Texture2D GetAlbumCover(string trackId, string category)
        {
            string key = string.IsNullOrEmpty(trackId) ? "unknown" : trackId;
            if (s_coverCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var local = MusicLibrarySession.Find(trackId);
            if (local != null && local.local)
            {
                try
                {
                    string stem = System.IO.Path.Combine(LocalMusicFileImporter.GetCustomMusicDirectory(), System.IO.Path.GetFileNameWithoutExtension(local.fileName));
                    foreach (string extension in new[] { ".png", ".jpg", ".jpeg" })
                    {
                        string file = stem + extension;
                        if (!System.IO.File.Exists(file) || new System.IO.FileInfo(file).Length > 4 * 1024 * 1024) continue;
                        var cover = new Texture2D(2, 2);
                        if (cover.LoadImage(System.IO.File.ReadAllBytes(file)) && cover.width <= 4096 && cover.height <= 4096)
                        {
                            cover.wrapMode = TextureWrapMode.Clamp; cover.filterMode = FilterMode.Bilinear;
                            s_coverCache[key] = cover; s_ownedCovers.Add(key); s_paletteCache[key] = ExtractPalette(cover); return cover;
                        }
                        UnityEngine.Object.Destroy(cover);
                    }
                    var embedded = MusicLocalMetadata.Read(MusicLibrarySession.LocalPath(local)).cover;
                    if (embedded != null)
                    {
                        var cover = new Texture2D(2, 2);
                        if (cover.LoadImage(embedded) && cover.width <= 4096 && cover.height <= 4096)
                        {
                            cover.wrapMode = TextureWrapMode.Clamp; cover.filterMode = FilterMode.Bilinear;
                            s_coverCache[key] = cover; s_ownedCovers.Add(key); s_paletteCache[key] = ExtractPalette(cover); return cover;
                        }
                        UnityEngine.Object.Destroy(cover);
                    }
                }
                catch (Exception) { /* Fall back to the railway archive sleeve. */ }
            }

            string sleeve = trackId switch
            {
                "platform" or "calm" or "morning" => "platform",
                "iron_and_ash" or "chollima_ride" => "iron_and_ash",
                "cloud_rail" or "first_light" or "curious" => "cloud_rail",
                "embers" or "warm" or "silence" => "embers",
                "night_cargo" or "starlit_rails" or "borderline" or "mystery" => "night_cargo",
                "determination" or "train_ambient" or "ambient_nature" => "determination",
                "emotional" or "melancholy" or "train_through_keys" or "south_wind" or "sleepers" => "memory",
                "news" or "news_explosive" or "wheels_joke" => "radio",
                _ => "private_tape"
            };
            var existing = Resources.Load<Texture2D>("Music/Covers/" + sleeve);
            if (existing != null)
            {
                s_coverCache[key] = existing;
                return existing;
            }
            // Shared archive sleeve when there is no matching authored scene.
            var genTex = GenerateProceduralCover("railway_archive", category);
            if (genTex != null)
            {
                s_coverCache[key] = genTex;
                s_paletteCache[key] = ExtractPalette(genTex);
                return genTex;
            }

            return null;
        }

        public static void LoadRemoteCover(string id, Action<Texture2D> onReady)
        {
            var track = MusicLibrarySession.Find(id);
            if (track == null || string.IsNullOrEmpty(track.provider) || string.IsNullOrEmpty(track.coverUrl) || s_remoteFailed.Contains(id)) return;
            if (s_ownedCovers.Contains(id) && s_coverCache.TryGetValue(id, out var existing) && existing != null) { onReady?.Invoke(existing); return; }
            if (s_remoteRequests.TryGetValue(id, out var queued)) { if (onReady != null) queued.Add(onReady); return; }
            var callbacks = new List<Action<Texture2D>>(); if (onReady != null) callbacks.Add(onReady); s_remoteRequests[id] = callbacks;
            var request = UnityEngine.Networking.UnityWebRequest.Get(track.coverUrl);
            request.timeout = 15; request.redirectLimit = 0;
            request.SendWebRequest().completed += _ => {
                Texture2D cover = null;
                try
                {
                    var bytes = request.result == UnityEngine.Networking.UnityWebRequest.Result.Success ? request.downloadHandler.data : null;
                    if (bytes != null && bytes.Length > 0 && bytes.Length <= 4 * 1024 * 1024)
                    {
                        var candidate = new Texture2D(2, 2);
                        if (candidate.LoadImage(bytes) && candidate.width <= 4096 && candidate.height <= 4096)
                        {
                            candidate.wrapMode = TextureWrapMode.Clamp; candidate.filterMode = FilterMode.Bilinear;
                            cover = candidate; s_coverCache[id] = candidate; s_ownedCovers.Add(id); s_paletteCache[id] = ExtractPalette(candidate);
                        }
                        else UnityEngine.Object.Destroy(candidate);
                    }
                }
                catch (Exception) { }
                finally { request.Dispose(); s_remoteRequests.Remove(id); }
                if (cover == null) { s_remoteFailed.Add(id); return; }
                foreach (var callback in callbacks) callback?.Invoke(cover);
            };
        }

        public static void InvalidateLocal(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (s_ownedCovers.Remove(id) && s_coverCache.TryGetValue(id, out var texture) && texture != null) UnityEngine.Object.Destroy(texture);
            s_coverCache.Remove(id); s_paletteCache.Remove(id);
        }

        public static Color GetDominantColor(string trackId, string category)
        {
            string key = string.IsNullOrEmpty(trackId) ? "unknown" : trackId;
            if (s_paletteCache.TryGetValue(key, out var col))
            {
                return col;
            }

            Color dominant = new Color(0.35f, 0.25f, 0.16f, 0.65f);
            if (category == "白噪音" || (trackId != null && trackId.Contains("ambient")))
            {
                dominant = new Color(0.18f, 0.30f, 0.32f, 0.65f);
            }
            else if (trackId != null && (trackId.Contains("oasis") || trackId.Contains("spring") || trackId.Contains("south_wind")))
            {
                dominant = new Color(0.22f, 0.38f, 0.28f, 0.65f);
            }
            else if (trackId != null && (trackId.Contains("iron") || trackId.Contains("climax") || trackId.Contains("explosive")))
            {
                dominant = new Color(0.48f, 0.26f, 0.18f, 0.65f);
            }

            s_paletteCache[key] = dominant;
            return dominant;
        }

        /// <summary>
        /// 纯代码绘制精致的 16-bit 像素工业复古唱片封套 (128x128)
        /// 针对全量 26 首曲目，结合其特定氛围、剧情意象与环境色盘细腻刻画
        /// </summary>
        public static Texture2D GenerateProceduralCover(string trackId, string category)
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            string id = (trackId ?? "unknown").ToLowerInvariant();
            Color[] pixels = new Color[size * size];

            // 1. 设定当前曲目的三色调色基底 (天顶色、地平线暖光色、大地/基底色)
            Color skyTop, skyHorizon, groundBase, accentGlow;
            int sceneKind = 0;

            if (id == "platform")
            {
                sceneKind = 1; // 黄昏站台与雨棚煤油灯
                skyTop = new Color(0.18f, 0.10f, 0.15f, 1f); skyHorizon = new Color(0.85f, 0.42f, 0.20f, 1f);
                groundBase = new Color(0.16f, 0.13f, 0.12f, 1f); accentGlow = C_BrassGold;
            }
            else if (id == "iron_and_ash")
            {
                sceneKind = 2; // 蒸汽机车机头与灼热炉火
                skyTop = new Color(0.08f, 0.08f, 0.10f, 1f); skyHorizon = new Color(0.35f, 0.18f, 0.12f, 1f);
                groundBase = new Color(0.12f, 0.12f, 0.14f, 1f); accentGlow = new Color(0.98f, 0.40f, 0.15f, 1f);
            }
            else if (id == "cloud_rail")
            {
                sceneKind = 3; // 高空飞艇俯瞰云海
                skyTop = new Color(0.12f, 0.20f, 0.35f, 1f); skyHorizon = new Color(0.88f, 0.65f, 0.45f, 1f);
                groundBase = new Color(0.75f, 0.80f, 0.90f, 1f); accentGlow = C_BrassGold;
            }
            else if (id == "embers")
            {
                sceneKind = 4; // 雾峰站雪夜与工棚火光
                skyTop = new Color(0.08f, 0.10f, 0.16f, 1f); skyHorizon = new Color(0.22f, 0.24f, 0.32f, 1f);
                groundBase = new Color(0.65f, 0.70f, 0.78f, 1f); accentGlow = new Color(0.96f, 0.50f, 0.18f, 1f);
            }
            else if (id == "calm")
            {
                sceneKind = 5; // 破晓平原微风与信号立柱
                skyTop = new Color(0.15f, 0.20f, 0.28f, 1f); skyHorizon = new Color(0.78f, 0.82f, 0.75f, 1f);
                groundBase = new Color(0.22f, 0.26f, 0.20f, 1f); accentGlow = new Color(0.96f, 0.82f, 0.45f, 1f);
            }
            else if (id == "night_cargo")
            {
                sceneKind = 6; // 边境夜航与探照灯穿透
                skyTop = new Color(0.05f, 0.06f, 0.10f, 1f); skyHorizon = new Color(0.12f, 0.15f, 0.22f, 1f);
                groundBase = new Color(0.10f, 0.10f, 0.12f, 1f); accentGlow = new Color(0.95f, 0.92f, 0.65f, 1f);
            }
            else if (id == "first_light")
            {
                sceneKind = 7; // 金曦沙脊与远山
                skyTop = new Color(0.20f, 0.14f, 0.22f, 1f); skyHorizon = new Color(0.98f, 0.72f, 0.30f, 1f);
                groundBase = new Color(0.72f, 0.48f, 0.25f, 1f); accentGlow = new Color(1f, 0.90f, 0.55f, 1f);
            }
            else if (id == "morning")
            {
                sceneKind = 8; // 清晨出库绿灯信号机
                skyTop = new Color(0.18f, 0.25f, 0.32f, 1f); skyHorizon = new Color(0.70f, 0.85f, 0.80f, 1f);
                groundBase = new Color(0.20f, 0.22f, 0.24f, 1f); accentGlow = new Color(0.25f, 0.95f, 0.45f, 1f);
            }
            else if (id == "borderline")
            {
                sceneKind = 9; // 边境铁丝网与警戒灯
                skyTop = new Color(0.10f, 0.08f, 0.12f, 1f); skyHorizon = new Color(0.45f, 0.18f, 0.16f, 1f);
                groundBase = new Color(0.15f, 0.14f, 0.16f, 1f); accentGlow = new Color(0.95f, 0.25f, 0.20f, 1f);
            }
            else if (id == "south_wind")
            {
                sceneKind = 10; // 绿洲海风与穿山隧道
                skyTop = new Color(0.12f, 0.24f, 0.32f, 1f); skyHorizon = new Color(0.55f, 0.85f, 0.70f, 1f);
                groundBase = new Color(0.25f, 0.45f, 0.30f, 1f); accentGlow = C_BrassGold;
            }
            else if (id == "starlit_rails")
            {
                sceneKind = 11; // 璀璨银河与双线长轨
                skyTop = new Color(0.04f, 0.05f, 0.12f, 1f); skyHorizon = new Color(0.15f, 0.18f, 0.30f, 1f);
                groundBase = new Color(0.08f, 0.09f, 0.12f, 1f); accentGlow = new Color(0.60f, 0.85f, 1f, 1f);
            }
            else if (id == "chollima_ride")
            {
                sceneKind = 12; // 千里马动车组机头与金翼
                skyTop = new Color(0.16f, 0.12f, 0.18f, 1f); skyHorizon = new Color(0.85f, 0.55f, 0.25f, 1f);
                groundBase = new Color(0.18f, 0.16f, 0.18f, 1f); accentGlow = C_BrassGold;
            }
            else
            {
                sceneKind = 0; // 经典大荒原铁道日落
                skyTop = new Color(0.15f, 0.12f, 0.22f, 1f); skyHorizon = new Color(0.88f, 0.50f, 0.25f, 1f);
                groundBase = new Color(0.22f, 0.18f, 0.14f, 1f); accentGlow = C_BrassGold;
            }

            int horizonY = 56; // 地平线 Y 轴基准线

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    Color c;

                    // A. 天空区域 (y >= horizonY)
                    if (y >= horizonY)
                    {
                        float t = (float)(y - horizonY) / (size - horizonY);
                        // 应用 Bayer 4x4 像素抖动，消除平滑渐变，营造正统 16-bit 像素光晕
                        float dither = (GetBayer(x, y) - 0.5f) * 0.12f;
                        float skyRatio = Mathf.Clamp01(t + dither);
                        c = Color.Lerp(skyHorizon, skyTop, skyRatio);

                        // 远山剪影 (海拔高度根据 X 生成连绵山脉)
                        float mntH = horizonY + 8f + Mathf.Sin(x * 0.08f) * 6f + Mathf.Sin(x * 0.025f) * 10f;
                        if (y <= mntH)
                        {
                            // 远山本体与 1px 亮金/晚霞轮廓线
                            if (Mathf.Abs(y - mntH) <= 1.2f)
                            {
                                c = Color.Lerp(skyHorizon, Color.white, 0.35f);
                            }
                            else
                            {
                                c = Color.Lerp(skyTop * 0.65f, groundBase * 0.7f, 0.5f);
                            }
                        }

                        // 星空微粒 (对于夜景曲目)
                        if ((sceneKind == 4 || sceneKind == 6 || sceneKind == 11) && y > mntH + 6)
                        {
                            if ((x * 17 + y * 31) % 43 == 0)
                            {
                                c = Color.Lerp(c, Color.white, 0.85f);
                            }
                            else if ((x * 23 + y * 19) % 37 == 0)
                            {
                                c = Color.Lerp(c, accentGlow, 0.65f);
                            }
                        }
                    }
                    // B. 大地/路基区域 (y < horizonY)
                    else
                    {
                        float gRatio = Mathf.Clamp01((float)y / horizonY);
                        float dither = (GetBayer(x, y) - 0.5f) * 0.10f;
                        c = Color.Lerp(groundBase * 0.75f, groundBase, gRatio + dither);

                        // 透视铁轨两股道 (自中央向前方两侧展开)
                        float depth = (horizonY - y);
                        float spread = depth * 0.82f;
                        float leftRailX = 64f - spread;
                        float rightRailX = 64f + spread;

                        // 轨枕 (水平横木，带近大远小透视压缩)
                        int tieStep = Mathf.Max(3, (int)(depth * 0.18f) + 2);
                        if (y % tieStep == 0 && x >= leftRailX - 4 && x <= rightRailX + 4)
                        {
                            c = new Color(0.12f, 0.10f, 0.09f, 1f); // 沥青枕木
                        }

                        // 钢轨顶面金色/白钢反光条
                        if (Mathf.Abs(x - leftRailX) <= 1f || Mathf.Abs(x - rightRailX) <= 1f)
                        {
                            c = Color.Lerp(accentGlow, Color.white, 0.3f);
                        }
                        else if (Mathf.Abs(x - leftRailX) <= 2f || Mathf.Abs(x - rightRailX) <= 2f)
                        {
                            c = new Color(0.18f, 0.17f, 0.19f, 1f); // 轨身钢质暗影
                        }
                    }

                    // C. 专属前景叙事元素
                    switch (sceneKind)
                    {
                        case 1: // platform: 站台屋檐桁架与悬挂暖黄煤油灯
                            if (y >= 92 && (x <= 75 || Mathf.Abs(x - 36) < 3)) c = new Color(0.14f, 0.12f, 0.11f, 1f);
                            if (y >= 90 && y <= 95 && x <= 80) c = new Color(0.24f, 0.18f, 0.14f, 1f);
                            // 煤油灯光晕
                            float lampDist = Mathf.Sqrt((x - 45) * (x - 45) + (y - 78) * (y - 78));
                            if (lampDist < 12f)
                            {
                                c = Color.Lerp(accentGlow, c, lampDist / 12f);
                                if (lampDist < 3.5f) c = Color.white;
                            }
                            if (Mathf.Abs(x - 45) <= 1 && y >= 78 && y <= 92) c = C_BrassDark;
                            break;

                        case 2: // iron_and_ash: 黑色蒸汽机车机头剪影与前照灯巨束
                            float locDist = Mathf.Sqrt((x - 64) * (x - 64) + (y - 54) * (y - 54));
                            if (locDist < 26f && y >= 40)
                            {
                                c = new Color(0.10f, 0.10f, 0.11f, 1f);
                                if (Mathf.Abs(locDist - 25f) < 1.2f) c = C_BrassDark; // 铜饰前盖
                            }
                            // 炉膛下方向下倾泻的熊熊橙火
                            if (y >= 28 && y <= 44 && Mathf.Abs(x - 64) < 18)
                            {
                                c = Color.Lerp(accentGlow, c, (float)Mathf.Abs(x - 64) / 18f);
                            }
                            // 前照大灯
                            float hDist = Mathf.Sqrt((x - 64) * (x - 64) + (y - 66) * (y - 66));
                            if (hDist < 4.5f) c = Color.white;
                            break;

                        case 3: // cloud_rail: 飞艇艇身与金色观景舷窗
                            float ax = (x - 64f) / 30f;
                            float ay = (y - 82f) / 11f;
                            if (ax * ax + ay * ay < 1f)
                            {
                                c = new Color(0.14f, 0.16f, 0.20f, 1f);
                                if (Mathf.Abs(y - 82f) < 1.5f && Mathf.Abs(x - 64) < 22 && (x % 5) != 0)
                                {
                                    c = accentGlow; // 点亮的客舱舷窗
                                }
                            }
                            break;

                        case 4: // embers: 木屋工棚与飘落雪花
                            if (x >= 70 && x <= 118 && y >= 46 && y <= 82)
                            {
                                c = new Color(0.16f, 0.12f, 0.10f, 1f); // 木屋
                                if (x >= 84 && x <= 104 && y >= 54 && y <= 72)
                                {
                                    c = accentGlow; // 暖炉火光透过格子窗
                                    if ((x - 84) % 6 == 0 || (y - 54) % 6 == 0) c = new Color(0.10f, 0.08f, 0.06f, 1f);
                                }
                            }
                            // 雪花飘落
                            if ((x * 19 + y * 23) % 27 == 0 && y > 35) c = Color.white;
                            break;

                        case 5: // calm: 沿线木电杆与垂悬电报铜线
                            if (Mathf.Abs(x - 38) <= 1 && y >= 45 && y <= 98) c = new Color(0.18f, 0.15f, 0.12f, 1f);
                            if (y >= 90 && y <= 92 && x >= 26 && x <= 50) c = new Color(0.18f, 0.15f, 0.12f, 1f); // 绝缘担梁
                            float wireY = 88f - Mathf.Sin((x - 38) * 0.04f) * 6f;
                            if (Mathf.Abs(y - wireY) <= 0.8f) c = new Color(0.35f, 0.30f, 0.25f, 1f);
                            break;

                        case 6: // night_cargo: 探照灯锥形光束划破长夜
                            float bLeft = 64f - (124f - y) * 0.45f;
                            float bRight = 64f + (124f - y) * 0.45f;
                            if (y < 120 && x >= bLeft && x <= bRight)
                            {
                                c = Color.Lerp(c, accentGlow, 0.30f);
                            }
                            break;

                        case 7: // first_light: 晨曦硕大红日初升
                            float sunD = Mathf.Sqrt((x - 64) * (x - 64) + (y - horizonY) * (y - horizonY));
                            if (sunD < 18f && y >= horizonY - 4)
                            {
                                c = Color.Lerp(Color.white, accentGlow, sunD / 18f);
                            }
                            break;

                        case 8: // morning: 信号机高耸立柱与翠绿发车闭塞灯
                            if (Mathf.Abs(x - 88) <= 2 && y >= 35 && y <= 105) c = new Color(0.16f, 0.18f, 0.20f, 1f);
                            if (Mathf.Abs(x - 88) <= 6 && y >= 85 && y <= 104) c = new Color(0.08f, 0.08f, 0.10f, 1f);
                            float sigD = Mathf.Sqrt((x - 88) * (x - 88) + (y - 94) * (y - 94));
                            if (sigD < 4f) c = accentGlow; // 绿灯信号
                            break;

                        case 9: // borderline: 边境铁丝网与斜切警示黑黄带
                            if (y >= 44 && y <= 56)
                            {
                                bool stripe = ((x + y) / 6) % 2 == 0;
                                c = stripe ? accentGlow : new Color(0.10f, 0.10f, 0.12f, 1f);
                            }
                            if ((x + y) % 9 == 0 || (x - y + 128) % 9 == 0) c = Color.Lerp(c, Color.white, 0.40f);
                            break;
                    }

                    // D. 实体复古唱片封套装饰外壳 (Album Jacket Physical Crafting)
                    // 1. 左侧书脊布面压边 (Bookcloth Spine Tape 8px)
                    if (x <= 8)
                    {
                        c = new Color(0.12f, 0.10f, 0.08f, 1f);
                        if (x == 7 || x == 8) c = C_BrassDark; // 烫金分隔细线
                        if ((y % 4) == 0 && x <= 6) c = Color.Lerp(c, C_BrassGold, 0.4f); // 织物微纹
                    }

                    // 2. 封套边缘 1px 黄铜嵌线与四角护角
                    if (x == 9 || x == size - 2 || y == 1 || y == size - 2)
                    {
                        c = Color.Lerp(c, C_BrassGold, 0.75f);
                    }

                    // 3. 四角黄铜铆钉护角 (Corner Brackets 4x4)
                    bool isCorner = (x >= 9 && x <= 16 && y <= 8) ||
                                    (x >= 9 && x <= 16 && y >= size - 9) ||
                                    (x >= size - 9 && y <= 8) ||
                                    (x >= size - 9 && y >= size - 9);
                    if (isCorner)
                    {
                        c = C_BrassDark;
                        if ((x == 12 || x == size - 5) && (y == 4 || y == size - 5)) c = Color.white; // 银铆钉高光
                    }

                    // 4. 左上角工务段钢印印章代号 [工务特编]
                    if (x >= 14 && x <= 34 && y >= size - 20 && y <= size - 10)
                    {
                        if (x == 14 || x == 34 || y == size - 20 || y == size - 10)
                        {
                            c = C_BrassGold;
                        }
                        else
                        {
                            c = Color.Lerp(c, new Color(0.10f, 0.08f, 0.06f, 1f), 0.75f);
                        }
                    }

                    // 5. 右下角黑胶防伪金箔印记 (Gold Foil Hologram Seal)
                    float sealD = Mathf.Sqrt((x - (size - 18)) * (x - (size - 18)) + (y - 18) * (y - 18));
                    if (sealD <= 7.5f)
                    {
                        c = Color.Lerp(C_BrassGold, new Color(1f, 0.98f, 0.80f, 1f), (x + y) % 3 == 0 ? 0.9f : 0.3f);
                        if (sealD <= 2.5f) c = new Color(0.65f, 0.45f, 0.15f, 1f);
                    }

                    // 6. 岁月暗角 (Vignette Shadow)
                    float vDist = Mathf.Max(Mathf.Abs(x - 64), Mathf.Abs(y - 64)) / 64f;
                    if (vDist > 0.70f)
                    {
                        float vFactor = Mathf.Clamp01((vDist - 0.70f) / 0.30f);
                        c = Color.Lerp(c, Color.black, vFactor * 0.45f);
                    }

                    pixels[idx] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Color ExtractPalette(Texture2D tex)
        {
            if (tex == null) return new Color(0.35f, 0.25f, 0.16f, 0.65f);
            try
            {
                Color c = tex.GetPixel(tex.width / 2, tex.height / 2);
                c.a = 0.65f;
                return c;
            }
            catch
            {
                return new Color(0.35f, 0.25f, 0.16f, 0.65f);
            }
        }
    }
}
