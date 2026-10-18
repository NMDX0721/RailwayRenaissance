using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听实体唱片与原声封套解析器 (MusicAlbumArtResolver)
    /// 彻底消灭“塑料黄同心圆”灾难，提供 16-bit 像素工业复古封套、黑胶同心微压纹、
    /// 缺图优雅兜底与 Folia 风格氛围色彩提取。
    /// </summary>
    public static class MusicAlbumArtResolver
    {
        private static readonly Dictionary<string, Texture2D> s_coverCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Color> s_paletteCache = new Dictionary<string, Color>();

        // 经典调频暖调工业色板
        private static readonly Color C_BrassGold = new Color(0.96f, 0.82f, 0.45f, 1f);
        private static readonly Color C_BrassDark = new Color(0.83f, 0.66f, 0.26f, 1f);
        private static readonly Color C_IronDark = new Color(0.16f, 0.17f, 0.20f, 1f);
        private static readonly Color C_VinylGroove = new Color(0.10f, 0.09f, 0.08f, 1f);
        private static readonly Color C_DesertSand = new Color(0.72f, 0.52f, 0.32f, 1f);
        private static readonly Color C_RustOrange = new Color(0.78f, 0.38f, 0.22f, 1f);
        private static readonly Color C_OasisGreen = new Color(0.28f, 0.48f, 0.38f, 1f);

        // 全量 26 首曲目与工程 16-bit 像素艺术背景精准映射表
        private static readonly Dictionary<string, string> s_trackToBgMap = new Dictionary<string, string>
        {
            { "platform", "bg/station_sunset" },
            { "iron_and_ash", "bg/depot_morning" },
            { "cloud_rail", "bg/aerial_view" },
            { "embers", "bg/station_night" },
            { "calm", "bg/platform_morning" },
            { "night_cargo", "bg/cabin_interior_night" },
            { "first_light", "bg/railway_track" },
            { "morning", "bg/platform_morning" },
            { "borderline", "bg/railway_map_aerial" },
            { "wheels_joke", "bg/tea_house" },
            { "determination", "bg/railway_track" },
            { "emotional", "bg/station_office" },
            { "melancholy", "bg/hangar" },
            { "mystery", "bg/cabin_interior_night" },
            { "curious", "bg/lab" },
            { "warm", "bg/car_interior" },
            { "silence", "bg/mountain_sky" },
            { "train_through_keys", "bg/professor_office" },
            { "south_wind", "bg/village_sunset" },
            { "starlit_rails", "bg/station_night" },
            { "chollima_ride", "bg/station" },
            { "sleepers", "bg/railway_track" },
            { "news", "bg/station_office" },
            { "news_explosive", "bg/home_banner" },
            { "ambient_nature", "bg/mountain_sky" },
            { "train_ambient", "bg/car_interior" }
        };

        /// <summary>
        /// 获取对应曲目的高清 16-bit 像素复古实体封套
        /// </summary>
        public static Texture2D GetAlbumCover(string trackId, string category)
        {
            string key = string.IsNullOrEmpty(trackId) ? "unknown" : trackId;
            if (s_coverCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            // 1. 优先使用专属代码绘制的 16-bit 像素工务唱片实体封套（紧扣曲目内容与氛围）
            var genTex = GenerateProceduralCover(trackId, category);
            if (genTex != null)
            {
                s_coverCache[key] = genTex;
                s_paletteCache[key] = ExtractPalette(genTex);
                return genTex;
            }

            // 2. 检查 Resources/album_covers/{trackId} 是否有美术手绘原画
            Texture2D resTex = Resources.Load<Texture2D>($"album_covers/{trackId}");
            if (resTex != null)
            {
                s_coverCache[key] = resTex;
                s_paletteCache[key] = ExtractPalette(resTex);
                return resTex;
            }

            // 3. 兜底对应曲目的工程背景
            if (s_trackToBgMap.TryGetValue(key, out string bgPath))
            {
                Texture2D bgTex = Resources.Load<Texture2D>(bgPath);
                if (bgTex != null)
                {
                    s_coverCache[key] = bgTex;
                    s_paletteCache[key] = ExtractPalette(bgTex);
                    return bgTex;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取当前曲目的氛围主色（用于 Folia 风格顶部渐变动态环境光）
        /// </summary>
        public static Color GetDominantColor(string trackId, string category)
        {
            string key = string.IsNullOrEmpty(trackId) ? "unknown" : trackId;
            if (s_paletteCache.TryGetValue(key, out var col))
            {
                return col;
            }

            // 根据分类给定的沉浸自然底色
            Color dominant = new Color(0.35f, 0.25f, 0.16f, 0.65f); // 默认铁道暖棕
            if (category == "白噪音" || (trackId != null && trackId.Contains("ambient")))
            {
                dominant = new Color(0.18f, 0.30f, 0.32f, 0.65f); // 沿线冷灰雾蓝
            }
            else if (trackId != null && (trackId.Contains("oasis") || trackId.Contains("spring")))
            {
                dominant = new Color(0.22f, 0.38f, 0.28f, 0.65f); // 绿洲晨曦橄榄绿
            }
            else if (trackId != null && (trackId.Contains("depart") || trackId.Contains("climax") || trackId.Contains("danger")))
            {
                dominant = new Color(0.48f, 0.26f, 0.18f, 0.65f); // 铁龙鸣笛赤铜赭石
            }

            s_paletteCache[key] = dominant;
            return dominant;
        }

        /// <summary>
        /// 程序化生成具有工务段钢印与 16-bit 像素艺术的专属实体唱片封面 (128x128)
        /// 彻底消除千篇一律，为全量 26 首曲目提供专属调色板与工业/铁路/风沙世界观图元
        /// </summary>
        private static Texture2D GenerateProceduralCover(string trackId, string category)
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            string id = (trackId ?? "unknown").ToLowerInvariant();
            Color[] pixels = new Color[size * size];
            float cx = size * 0.5f;
            float cy = size * 0.5f;

            // 专属工业世界观调色板与图案分类
            Color baseColor;
            Color accentColor;
            Color secondaryColor;
            int motifKind; // 0=站台煤油灯, 1=机车炉火, 2=云海飞艇, 3=工棚余烬, 4=晨风微光, 5=夜行货车, 6=破晓晨曦, 7=出库信号, 8=国境铁网, 9=便当店连杆, 10=工务道尺, 11=岁月茶案, 12=机库斜阳, 13=沙能矿脉, 14=实验微光, 15=车厢炉火, 16=极夜星空, 17=琴键留声, 18=南风绿洲, 19=星光双轨, 20=千里马翼, 21=枕木道床, 22=调频旋钮, 23=紧急号外, 24=风沙戈壁, 25=轮轨声波

            if (id == "platform") { motifKind = 0; baseColor = new Color(0.24f, 0.16f, 0.12f, 1f); accentColor = C_BrassGold; secondaryColor = new Color(0.85f, 0.45f, 0.22f, 1f); }
            else if (id == "iron_and_ash") { motifKind = 1; baseColor = new Color(0.14f, 0.13f, 0.15f, 1f); accentColor = new Color(0.95f, 0.35f, 0.18f, 1f); secondaryColor = new Color(0.40f, 0.40f, 0.45f, 1f); }
            else if (id == "cloud_rail") { motifKind = 2; baseColor = new Color(0.18f, 0.26f, 0.36f, 1f); accentColor = new Color(0.85f, 0.90f, 0.98f, 1f); secondaryColor = C_BrassGold; }
            else if (id == "embers") { motifKind = 3; baseColor = new Color(0.18f, 0.15f, 0.18f, 1f); accentColor = new Color(0.92f, 0.42f, 0.20f, 1f); secondaryColor = new Color(0.70f, 0.75f, 0.82f, 1f); }
            else if (id == "calm") { motifKind = 4; baseColor = new Color(0.16f, 0.22f, 0.26f, 1f); accentColor = new Color(0.75f, 0.88f, 0.85f, 1f); secondaryColor = new Color(0.92f, 0.78f, 0.55f, 1f); }
            else if (id == "night_cargo") { motifKind = 5; baseColor = new Color(0.08f, 0.10f, 0.15f, 1f); accentColor = new Color(0.35f, 0.65f, 0.90f, 1f); secondaryColor = new Color(0.95f, 0.90f, 0.60f, 1f); }
            else if (id == "first_light") { motifKind = 6; baseColor = new Color(0.22f, 0.16f, 0.14f, 1f); accentColor = new Color(0.98f, 0.75f, 0.35f, 1f); secondaryColor = new Color(0.85f, 0.38f, 0.22f, 1f); }
            else if (id == "morning") { motifKind = 7; baseColor = new Color(0.18f, 0.20f, 0.16f, 1f); accentColor = new Color(0.40f, 0.85f, 0.45f, 1f); secondaryColor = C_BrassGold; }
            else if (id == "borderline") { motifKind = 8; baseColor = new Color(0.15f, 0.14f, 0.16f, 1f); accentColor = new Color(0.95f, 0.28f, 0.22f, 1f); secondaryColor = new Color(0.78f, 0.72f, 0.30f, 1f); }
            else if (id == "wheels_joke") { motifKind = 9; baseColor = new Color(0.24f, 0.18f, 0.12f, 1f); accentColor = new Color(0.96f, 0.68f, 0.25f, 1f); secondaryColor = new Color(0.88f, 0.32f, 0.24f, 1f); }
            else if (id == "determination") { motifKind = 10; baseColor = new Color(0.20f, 0.17f, 0.13f, 1f); accentColor = C_BrassGold; secondaryColor = C_RustOrange; }
            else if (id == "emotional") { motifKind = 11; baseColor = new Color(0.22f, 0.17f, 0.15f, 1f); accentColor = new Color(0.88f, 0.70f, 0.52f, 1f); secondaryColor = new Color(0.48f, 0.30f, 0.22f, 1f); }
            else if (id == "melancholy") { motifKind = 12; baseColor = new Color(0.16f, 0.14f, 0.16f, 1f); accentColor = new Color(0.82f, 0.55f, 0.35f, 1f); secondaryColor = new Color(0.45f, 0.40f, 0.48f, 1f); }
            else if (id == "mystery") { motifKind = 13; baseColor = new Color(0.10f, 0.12f, 0.18f, 1f); accentColor = new Color(0.25f, 0.85f, 0.95f, 1f); secondaryColor = new Color(0.48f, 0.35f, 0.85f, 1f); }
            else if (id == "curious") { motifKind = 14; baseColor = new Color(0.12f, 0.16f, 0.15f, 1f); accentColor = new Color(0.35f, 0.92f, 0.65f, 1f); secondaryColor = new Color(0.22f, 0.65f, 0.85f, 1f); }
            else if (id == "warm") { motifKind = 15; baseColor = new Color(0.22f, 0.14f, 0.10f, 1f); accentColor = new Color(0.98f, 0.52f, 0.18f, 1f); secondaryColor = C_BrassGold; }
            else if (id == "silence") { motifKind = 16; baseColor = new Color(0.07f, 0.08f, 0.13f, 1f); accentColor = new Color(0.70f, 0.82f, 0.98f, 1f); secondaryColor = new Color(0.35f, 0.42f, 0.65f, 1f); }
            else if (id == "train_through_keys") { motifKind = 17; baseColor = new Color(0.12f, 0.12f, 0.14f, 1f); accentColor = new Color(0.92f, 0.90f, 0.85f, 1f); secondaryColor = C_BrassGold; }
            else if (id == "south_wind") { motifKind = 18; baseColor = new Color(0.14f, 0.22f, 0.18f, 1f); accentColor = new Color(0.45f, 0.82f, 0.60f, 1f); secondaryColor = new Color(0.85f, 0.78f, 0.40f, 1f); }
            else if (id == "starlit_rails") { motifKind = 19; baseColor = new Color(0.09f, 0.11f, 0.17f, 1f); accentColor = new Color(0.42f, 0.85f, 0.95f, 1f); secondaryColor = new Color(0.95f, 0.32f, 0.28f, 1f); }
            else if (id == "chollima_ride") { motifKind = 20; baseColor = new Color(0.22f, 0.15f, 0.10f, 1f); accentColor = C_BrassGold; secondaryColor = new Color(0.92f, 0.35f, 0.18f, 1f); }
            else if (id == "sleepers") { motifKind = 21; baseColor = new Color(0.16f, 0.15f, 0.14f, 1f); accentColor = new Color(0.65f, 0.48f, 0.32f, 1f); secondaryColor = new Color(0.82f, 0.72f, 0.52f, 1f); }
            else if (id == "news") { motifKind = 22; baseColor = new Color(0.15f, 0.17f, 0.18f, 1f); accentColor = new Color(0.55f, 0.85f, 0.90f, 1f); secondaryColor = C_BrassGold; }
            else if (id == "news_explosive") { motifKind = 23; baseColor = new Color(0.22f, 0.11f, 0.10f, 1f); accentColor = new Color(0.98f, 0.28f, 0.22f, 1f); secondaryColor = new Color(0.98f, 0.88f, 0.35f, 1f); }
            else if (id == "ambient_nature") { motifKind = 24; baseColor = new Color(0.15f, 0.18f, 0.20f, 1f); accentColor = new Color(0.65f, 0.78f, 0.85f, 1f); secondaryColor = C_DesertSand; }
            else if (id == "train_ambient") { motifKind = 25; baseColor = new Color(0.12f, 0.14f, 0.16f, 1f); accentColor = new Color(0.45f, 0.75f, 0.88f, 1f); secondaryColor = C_BrassGold; }
            else
            {
                motifKind = (Mathf.Abs(id.GetHashCode()) % 4);
                baseColor = C_IronDark;
                accentColor = C_BrassGold;
                secondaryColor = C_RustOrange;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // 1. 岁月暗角与哑光纸基底
                    float vignette = 1f - (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) / (size * 0.5f)) * 0.28f;
                    Color c = Color.Lerp(baseColor * 0.65f, baseColor, vignette);

                    // 2. 边框 2px 黄铜微嵌线与四角护角
                    if (x <= 1 || x >= size - 2 || y <= 1 || y >= size - 2)
                    {
                        c = Color.Lerp(c, C_BrassDark, 0.6f);
                    }
                    if ((x <= 5 || x >= size - 6) && (y <= 5 || y >= size - 6))
                    {
                        c = C_BrassGold; // 铆钉黄铜金角
                    }

                    // 3. 专属 16-bit 像素图元渲染
                    switch (motifKind)
                    {
                        case 0: // platform: 站台暖昏、雨棚与垂挂煤油灯
                            if (y >= 88 && (Mathf.Abs(x - 30) < 3 || Mathf.Abs(x - 98) < 3)) c = C_BrassDark; // 站台立柱
                            if (y >= 85 && y <= 92) c = new Color(0.20f, 0.15f, 0.12f, 1f); // 挑檐雨棚
                            if (Mathf.Abs(x - 64) <= 4 && y >= 60 && y <= 74) c = accentColor; // 煤油灯光团
                            if (Mathf.Abs(x - 64) <= 1 && y >= 74 && y <= 85) c = C_BrassDark; // 挂链
                            if (y < 42 && (x + y * 2) % 18 == 0) c = Color.Lerp(c, secondaryColor, 0.45f); // 月台铺石路基
                            break;

                        case 1: // iron_and_ash: 蒸汽活塞机车切面与烈火
                            if (dist < 42f)
                            {
                                c = Color.Lerp(new Color(0.18f, 0.17f, 0.16f, 1f), C_VinylGroove, dist / 42f);
                                if (dist < 22f) c = Color.Lerp(accentColor, secondaryColor, dist / 22f); // 炉膛烈火红心
                                if (dist < 6f) c = new Color(0.06f, 0.06f, 0.06f, 1f);
                            }
                            if (y > 90 && Mathf.Abs(dx) < 14) c = Color.Lerp(c, new Color(0.35f, 0.35f, 0.40f, 1f), 0.7f); // 烟囱排烟
                            break;

                        case 2: // cloud_rail: 飞空艇剪影与层层云海
                            float cloudY = 38f + Mathf.Sin(x * 0.12f) * 6f + Mathf.Cos(x * 0.05f) * 8f;
                            if (y < cloudY) c = Color.Lerp(accentColor * 0.4f, accentColor, y / cloudY);
                            // 飞空艇艇身剪影
                            float airshipDx = (x - 64f) / 32f;
                            float airshipDy = (y - 78f) / 12f;
                            if (airshipDx * airshipDx + airshipDy * airshipDy < 1f) c = new Color(0.12f, 0.14f, 0.18f, 1f);
                            if (y >= 74 && y <= 77 && Mathf.Abs(x - 64) < 16) c = secondaryColor; // 艇身黄铜饰带
                            break;

                        case 3: // embers: 雾峰暴风雪与工棚炉火
                            if (y < 48 && Mathf.Abs(x - 64) < 28) c = Color.Lerp(accentColor, baseColor, dist / 35f); // 炉火热浪
                            if ((x * 13 + y * 23) % 29 == 0 && y > 35) c = secondaryColor; // 漫天雪花微粒
                            break;

                        case 4: // calm: 破晓平原微风与双线钢轨
                            if (y > 70) c = Color.Lerp(baseColor, accentColor * 0.4f, (y - 70f) / 58f); // 晨曦光晕
                            if ((Mathf.Abs(x - (50 - y * 0.25f)) < 2 || Mathf.Abs(x - (78 + y * 0.25f)) < 2) && y < 70) c = C_BrassGold; // 延伸铁轨
                            if (y < 70 && y % 9 <= 1 && x >= 36 && x <= 92) c = new Color(0.32f, 0.24f, 0.18f, 1f); // 轨枕
                            break;

                        case 5: // night_cargo: 边境夜航与探照灯锥束
                            float beamLeft = 64f - (120f - y) * 0.4f;
                            float beamRight = 64f + (120f - y) * 0.4f;
                            if (y < 115 && x >= beamLeft && x <= beamRight) c = Color.Lerp(c, secondaryColor, 0.35f); // 探照灯锥形光束
                            if (Mathf.Abs(x - 64) < 8 && y >= 110 && y <= 120) c = secondaryColor; // 探照灯灯源
                            break;

                        case 6: // first_light: 金色晨曦刺破夜空
                            float sunDist = Mathf.Sqrt((x - 64) * (x - 64) + (y - 48) * (y - 48));
                            if (sunDist < 20f) c = Color.Lerp(accentColor, secondaryColor, sunDist / 20f); // 朝阳
                            float sandY = 40f + Mathf.Sin(x * 0.08f) * 6f;
                            if (y < sandY) c = Color.Lerp(new Color(0.25f, 0.16f, 0.10f, 1f), C_DesertSand, y / sandY); // 金色沙脊
                            break;

                        case 7: // morning: 出库绿灯信号机
                            if (Mathf.Abs(x - 64) <= 3 && y >= 25 && y <= 100) c = new Color(0.20f, 0.22f, 0.24f, 1f); // 信号立柱
                            if (Mathf.Abs(x - 64) <= 8 && y >= 80 && y <= 105) c = new Color(0.10f, 0.10f, 0.12f, 1f); // 机构盒
                            if (Mathf.Sqrt((x - 64) * (x - 64) + (y - 92) * (y - 92)) < 5.5f) c = accentColor; // 翠绿闭塞发车信号灯
                            break;

                        case 8: // borderline: 边境铁网与斜切警示带
                            if (y >= 48 && y <= 62)
                            {
                                bool stripe = ((x + y) / 7) % 2 == 0;
                                c = stripe ? accentColor : new Color(0.12f, 0.12f, 0.14f, 1f);
                            }
                            if ((x + y) % 11 == 0 || (x - y + 128) % 11 == 0) c = Color.Lerp(c, secondaryColor, 0.55f); // 菱形铁丝网
                            break;

                        case 9: // wheels_joke: 暖帘与连杆齿轮
                            if (y > 95 && (x / 16) % 2 == 0) c = secondaryColor; // 关东暖帘布条
                            if (dist < 28f)
                            {
                                float tooth = Mathf.Sin(Mathf.Atan2(dy, dx) * 8f);
                                if (dist < 22f + tooth * 3f) c = accentColor;
                                if (dist < 10f) c = baseColor;
                            }
                            break;

                        case 10: // determination: 金色道尺与交叉重锤
                            if (Mathf.Abs(dx - dy) < 3.5f && Mathf.Abs(dx) < 38) c = accentColor; // 斜向钢道尺
                            if (Mathf.Abs(dx + dy) < 3.5f && Mathf.Abs(dx) < 38) c = secondaryColor; // 交叉工务重锤
                            break;

                        case 11: // emotional: 站长茶案紫砂温润光晕
                            if (dist < 32f) c = Color.Lerp(secondaryColor, baseColor, dist / 32f);
                            if (Mathf.Abs(x - 64) < 14 && y >= 45 && y <= 65) c = accentColor; // 案头茶具剪影
                            break;

                        case 12: // melancholy: 机库破损顶棚斜阳光柱
                            float shaft = (x * 0.7f + y);
                            if (shaft >= 70 && shaft <= 110) c = Color.Lerp(c, accentColor, 0.45f); // 倾泻而下的尘埃暖光
                            break;

                        case 13: // mystery: 幽蓝沙能晶体矿脉
                            float crystal = Mathf.Abs(dx) * 1.5f + Mathf.Abs(dy);
                            if (crystal < 36f) c = Color.Lerp(accentColor, secondaryColor, (float)y / size);
                            if (dist < 12f) c = Color.white; // 能量核心
                            break;

                        case 14: // curious: 实验室烧瓶电弧
                            if (dist < 26f && y < 65) c = accentColor; // 烧瓶底部液面
                            if (Mathf.Abs(dx) < 5 && y >= 65 && y <= 88) c = secondaryColor; // 瓶颈
                            break;

                        case 15: // warm: 列车小煤炉与跳跃炭火
                            if (dist < 30f) c = Color.Lerp(accentColor, secondaryColor, dist / 30f);
                            if (dist < 12f) c = new Color(1f, 0.95f, 0.75f, 1f); // 白热炭火
                            break;

                        case 16: // silence: 极夜浩瀚星海与单线铁道
                            if ((x * 19 + y * 37) % 31 == 0 && y > 35) c = accentColor; // 银河璀璨星辰
                            if (Mathf.Abs(x - 64) < 2 && y <= 45) c = secondaryColor; // 地平线尽头的单轨
                            break;

                        case 17: // train_through_keys: 钢琴黑白键与唱针
                            if (y >= 40 && y <= 88)
                            {
                                bool isBlackKey = ((x / 9) % 3 != 0) && (y > 60);
                                c = isBlackKey ? new Color(0.10f, 0.10f, 0.12f, 1f) : accentColor;
                            }
                            break;

                        case 18: // south_wind: 暖湿海风与穿山隧道
                            float mountY = 50f + Mathf.Sin(x * 0.06f) * 16f;
                            if (y < mountY) c = Color.Lerp(accentColor * 0.5f, accentColor, y / mountY); // 青绿山丘
                            if (Mathf.Sqrt((x - 64) * (x - 64) + (y - 30) * (y - 30)) < 16f) c = new Color(0.08f, 0.10f, 0.12f, 1f); // 隧道拱口
                            break;

                        case 19: // starlit_rails: 星空下的双线红绿信号灯
                            if ((x * 17 + y * 29) % 37 == 0 && y > 50) c = accentColor;
                            if (Mathf.Sqrt((x - 48) * (x - 48) + (y - 45) * (y - 45)) < 6f) c = secondaryColor; // 红灯
                            if (Mathf.Sqrt((x - 80) * (x - 80) + (y - 45) * (y - 45)) < 6f) c = new Color(0.28f, 0.95f, 0.45f, 1f); // 绿灯
                            break;

                        case 20: // chollima_ride: 千里马流线金翼放射线
                            float angle = Mathf.Atan2(dy, dx);
                            if (Mathf.Abs(Mathf.Sin(angle * 6f)) > 0.65f) c = Color.Lerp(c, accentColor, 0.55f); // 金色动力辐射线
                            if (dist < 24f) c = secondaryColor;
                            break;

                        case 21: // sleepers: 坚固枕木与锰钢扣件螺栓
                            if (y >= 40 && y <= 88) c = accentColor; // 铁道枕木
                            if ((Mathf.Abs(x - 38) < 4 || Mathf.Abs(x - 90) < 4) && y >= 40 && y <= 88) c = C_BrassGold; // 扣件钢板
                            break;

                        case 22: // news: 调频收音机金属刻度盘
                            if (dist < 42f)
                            {
                                c = Color.Lerp(new Color(0.15f, 0.16f, 0.18f, 1f), C_VinylGroove, dist / 42f);
                                if ((int)dist % 5 == 0) c = accentColor; // 刻度线
                            }
                            break;

                        case 23: // news_explosive: 紧急快讯放射状电波
                            float pulse = Mathf.Sin(dist * 0.4f);
                            if (pulse > 0.6f) c = accentColor; // 红色脉冲波
                            if (dist < 14f) c = secondaryColor; // 喇叭核心
                            break;

                        case 24: // ambient_nature: 荒原风沙与蘑菇巨石
                            float sandWave = Mathf.Sin(x * 0.15f + y * 0.08f) * 6f;
                            if (Mathf.Abs(dy - sandWave) < 4f) c = accentColor;
                            break;

                        case 25: // train_ambient: 轮轨声波律动谱线
                            float rhythm = Mathf.Sin(x * 0.28f) * 14f + Mathf.Cos(x * 0.12f) * 8f;
                            if (Mathf.Abs(dy - rhythm) < 3.5f) c = accentColor;
                            break;

                        default: // 经典黑胶唱盘
                            if (dist < 44f)
                            {
                                c = Color.Lerp(C_VinylGroove, new Color(0.22f, 0.20f, 0.18f, 1f), (Mathf.Sin(dist * 1.8f) + 1f) * 0.15f);
                                if ((int)dist % 4 == 0) c *= 1.25f;
                                if (dist < 16f) c = Color.Lerp(accentColor, C_BrassDark, dist / 16f);
                                if (dist < 5f) c = new Color(0.06f, 0.06f, 0.06f, 1f);
                            }
                            break;
                    }

                    // 4. 左上角 16-bit 像素工务段调频钢印代号 (如 CR 印章)
                    if (x >= 8 && x <= 22 && y >= size - 20 && y <= size - 8)
                    {
                        if (x == 8 || x == 22 || y == size - 20 || y == size - 8)
                        {
                            c = C_BrassGold; // 印章外框
                        }
                        else
                        {
                            c = Color.Lerp(c, new Color(0.10f, 0.08f, 0.06f, 0.9f), 0.75f);
                        }
                    }

                    // 5. 右下角黑胶防伪金箔徽记（Brass Foil Hologram Seal）
                    if (x >= size - 22 && x <= size - 8 && y >= 8 && y <= 22)
                    {
                        float sealDist = Mathf.Sqrt((x - (size - 15)) * (x - (size - 15)) + (y - 15) * (y - 15));
                        if (sealDist <= 6.5f)
                        {
                            c = Color.Lerp(C_BrassGold, new Color(1f, 0.95f, 0.75f, 1f), (x + y) % 3 == 0 ? 0.8f : 0.2f);
                        }
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
