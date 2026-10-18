using System;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 随身听纯代码矢量图标绘制器
    /// 彻底告别粗劣 Unicode 字符与怪异字体方块，纯 VisualElement 构建高精度像素美学图标
    /// </summary>
    public static class MusicIconHelper
    {
        // ==================== 工业暖褐 Design Tokens 常量 ====================
        public static readonly Color BgVoid = new Color(0.08f, 0.07f, 0.06f, 1f); // #141211 沙海黑曜底色
        public static readonly Color BgCard = new Color(0.11f, 0.10f, 0.09f, 1f); // #1c1917 铸铁暖褐卡片
        public static readonly Color BgCardHover = new Color(0.15f, 0.13f, 0.11f, 1f); // #26211d 悬停微亮
        public static readonly Color BrassGold = new Color(0.96f, 0.82f, 0.45f, 1f); // #f5d173 黄铜暖金
        public static readonly Color BrassDark = new Color(0.83f, 0.66f, 0.26f, 1f); // #d4a843 深古铜金
        public static readonly Color TextParchment = new Color(0.91f, 0.85f, 0.75f, 1f); // #e8d8c0 和纸浅米
        public static readonly Color TextMuted = new Color(0.66f, 0.64f, 0.62f, 1f); // #a8a29e 锈迹浅灰
        public static readonly Color BorderIndustrial = new Color(0.32f, 0.28f, 0.24f, 0.85f); // 工业黄铜边框

        private static UnityEngine.UIElements.Cursor? s_handCursor;
        private static Texture2D s_handCursorTex;

        public static UnityEngine.UIElements.Cursor GetHandCursor()
        {
            if (!s_handCursor.HasValue)
            {
                if (s_handCursorTex == null)
                {
                    s_handCursorTex = CreatePixelHandTexture();
                }
                s_handCursor = new UnityEngine.UIElements.Cursor
                {
                    texture = s_handCursorTex,
                    hotspot = new Vector2(5, 1)
                };
            }
            return s_handCursor.Value;
        }

        private static Texture2D CreatePixelHandTexture()
        {
            // 经典 16×16 16-bit 像素指针手套（米白手套、墨黑描边、黄铜腕口，绝对正统）
            int w = 16, h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            string[] map = new string[]
            {
                ".....#..........",
                "....#X#.........",
                "....#X#.........",
                "....#X#.........",
                "....#X#...#.....",
                "....#X#..#X#....",
                "....#X#.#X#X#...",
                "....#X##X#X#X#..",
                "..###XXXXXXXX#..",
                ".#XXXXXXXXXXX#..",
                ".#XXXXXXXXXXX#..",
                "..#XXXXXXXXXX#..",
                "...#XXXXXXXX#...",
                "....#BBBBBB#....",
                "....#BBBBBB#....",
                ".....######....."
            };

            Color outline = new Color(0.10f, 0.08f, 0.06f, 1f);      // 墨黑描边
            Color glove = new Color(0.96f, 0.94f, 0.90f, 1f);        // 和纸米白手套
            Color brassCuff = new Color(0.85f, 0.68f, 0.28f, 1f);    // 黄铜腕扣

            for (int y = 0; y < h; y++)
            {
                string row = map[y];
                int texY = h - 1 - y; // 纹理坐标 Y 轴自底向上
                for (int x = 0; x < w && x < row.Length; x++)
                {
                    char c = row[x];
                    if (c == '#') pixels[texY * w + x] = outline;
                    else if (c == 'X') pixels[texY * w + x] = glove;
                    else if (c == 'B') pixels[texY * w + x] = brassCuff;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// 统一为可交互元素设置 16-bit 像素黄铜手套光标
        /// </summary>
        public static void SetHandCursor(this VisualElement el)
        {
            if (el == null) return;
            try
            {
                el.style.cursor = GetHandCursor();
            }
            catch {}
        }

        /// <summary>
        /// 纯代码绘制工业号角小喇叭（阶梯式向右展开，支持静音亮红斜杠，彻底废除原生 Emoji 🔊/🔇）
        /// </summary>
        public static VisualElement CreateSpeakerIcon(float size, Color color, bool isMuted)
        {
            var box = new VisualElement();
            box.style.width = size;
            box.style.height = size;
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            box.style.position = Position.Relative;
            box.pickingMode = PickingMode.Ignore;

            // 喇叭容器
            var speaker = new VisualElement();
            speaker.style.flexDirection = FlexDirection.Row;
            speaker.style.alignItems = Align.Center;
            speaker.style.justifyContent = Justify.Center;
            speaker.pickingMode = PickingMode.Ignore;
            box.Add(speaker);

            Color iconColor = isMuted ? new Color(color.r, color.g, color.b, 0.45f) : color;

            // 1. 左侧音箱矩形音腔
            var body = new VisualElement();
            body.style.width = Mathf.Max(2f, size * 0.20f);
            body.style.height = Mathf.Max(4f, size * 0.38f);
            body.style.backgroundColor = iconColor;
            body.style.borderTopLeftRadius = 1;
            body.style.borderBottomLeftRadius = 1;
            body.pickingMode = PickingMode.Ignore;
            speaker.Add(body);

            // 2. 中间阶梯号筒（从左向右阶梯放大，确保绝对朝右且像素锐利）
            var step1 = new VisualElement();
            step1.style.width = Mathf.Max(1.5f, size * 0.12f);
            step1.style.height = Mathf.Max(6f, size * 0.58f);
            step1.style.backgroundColor = iconColor;
            step1.pickingMode = PickingMode.Ignore;
            speaker.Add(step1);

            var step2 = new VisualElement();
            step2.style.width = Mathf.Max(2f, size * 0.16f);
            step2.style.height = Mathf.Max(8f, size * 0.82f);
            step2.style.backgroundColor = iconColor;
            step2.style.borderTopRightRadius = 1;
            step2.style.borderBottomRightRadius = 1;
            step2.pickingMode = PickingMode.Ignore;
            speaker.Add(step2);

            // 3. 右侧状态：静音斜杠 或 优雅同心音波
            if (isMuted)
            {
                var slash = new VisualElement();
                slash.style.position = Position.Absolute;
                slash.style.width = size * 0.95f;
                slash.style.height = 1.6f;
                slash.style.backgroundColor = new Color(0.95f, 0.35f, 0.35f, 1f); // 亮红静音警示
                slash.style.rotate = new Rotate(Angle.Degrees(-45f));
                slash.pickingMode = PickingMode.Ignore;
                box.Add(slash);
            }
            else
            {
                // 小音波（紧随号口右侧）
                var wave1 = new VisualElement();
                wave1.style.width = Mathf.Max(2f, size * 0.16f);
                wave1.style.height = Mathf.Max(5f, size * 0.45f);
                wave1.style.borderRightWidth = 1.4f;
                wave1.style.borderRightColor = color;
                wave1.style.borderTopWidth = 1f;
                wave1.style.borderTopColor = Color.clear;
                wave1.style.borderBottomWidth = 1f;
                wave1.style.borderBottomColor = Color.clear;
                wave1.style.borderTopRightRadius = size * 0.25f;
                wave1.style.borderBottomRightRadius = size * 0.25f;
                wave1.style.marginLeft = 2f;
                wave1.pickingMode = PickingMode.Ignore;
                speaker.Add(wave1);

                // 大音波（外圈）
                var wave2 = new VisualElement();
                wave2.style.width = Mathf.Max(2f, size * 0.18f);
                wave2.style.height = Mathf.Max(8f, size * 0.75f);
                wave2.style.borderRightWidth = 1.4f;
                wave2.style.borderRightColor = color;
                wave2.style.borderTopWidth = 1f;
                wave2.style.borderTopColor = Color.clear;
                wave2.style.borderBottomWidth = 1f;
                wave2.style.borderBottomColor = Color.clear;
                wave2.style.borderTopRightRadius = size * 0.40f;
                wave2.style.borderBottomRightRadius = size * 0.40f;
                wave2.style.marginLeft = 1.5f;
                wave2.pickingMode = PickingMode.Ignore;
                speaker.Add(wave2);
            }

            return box;
        }

        /// <summary>
        /// 纯代码微缩磁带盒图标（用于侧栏本地磁带夹与卡片）
        /// </summary>
        public static VisualElement CreateCassetteIcon(float width, float height, Color color)
        {
            var cas = new VisualElement();
            cas.style.width = width;
            cas.style.height = height;
            cas.style.backgroundColor = new Color(0.18f, 0.16f, 0.14f, 1f);
            cas.style.borderTopWidth = 1; cas.style.borderBottomWidth = 1;
            cas.style.borderLeftWidth = 1; cas.style.borderRightWidth = 1;
            cas.style.borderTopColor = color; cas.style.borderBottomColor = color;
            cas.style.borderLeftColor = color; cas.style.borderRightColor = color;
            cas.style.borderTopLeftRadius = 3; cas.style.borderTopRightRadius = 3;
            cas.style.borderBottomLeftRadius = 3; cas.style.borderBottomRightRadius = 3;
            cas.style.flexDirection = FlexDirection.Row;
            cas.style.alignItems = Align.Center;
            cas.style.justifyContent = Justify.SpaceAround;
            cas.style.paddingLeft = 2; cas.style.paddingRight = 2;
            cas.pickingMode = PickingMode.Ignore;

            var r1 = new VisualElement();
            r1.style.width = height * 0.45f; r1.style.height = height * 0.45f;
            r1.style.borderTopLeftRadius = height * 0.25f; r1.style.borderTopRightRadius = height * 0.25f;
            r1.style.borderBottomLeftRadius = height * 0.25f; r1.style.borderBottomRightRadius = height * 0.25f;
            r1.style.backgroundColor = color;
            r1.pickingMode = PickingMode.Ignore;
            cas.Add(r1);

            var win = new VisualElement();
            win.style.width = width * 0.28f; win.style.height = height * 0.35f;
            win.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
            win.pickingMode = PickingMode.Ignore;
            cas.Add(win);

            var r2 = new VisualElement();
            r2.style.width = height * 0.45f; r2.style.height = height * 0.45f;
            r2.style.borderTopLeftRadius = height * 0.25f; r2.style.borderTopRightRadius = height * 0.25f;
            r2.style.borderBottomLeftRadius = height * 0.25f; r2.style.borderBottomRightRadius = height * 0.25f;
            r2.style.backgroundColor = color;
            r2.pickingMode = PickingMode.Ignore;
            cas.Add(r2);

            return cas;
        }

        /// <summary>
        /// 纯代码绘制三根动态跳动音浪柱（播放中的序号列互斥替换图元）
        /// </summary>
        public static VisualElement CreatePlayingWaveBars(Color color)
        {
            var box = new VisualElement();
            box.style.width = 18;
            box.style.height = 14;
            box.style.flexDirection = FlexDirection.Row;
            box.style.alignItems = Align.FlexEnd;
            box.style.justifyContent = Justify.SpaceBetween;
            box.pickingMode = PickingMode.Ignore;

            var b1 = new VisualElement(); b1.style.width = 3.5f; b1.style.height = 7; b1.style.backgroundColor = color; b1.style.borderTopLeftRadius = 1; b1.style.borderTopRightRadius = 1; box.Add(b1);
            var b2 = new VisualElement(); b2.style.width = 3.5f; b2.style.height = 13; b2.style.backgroundColor = color; b2.style.borderTopLeftRadius = 1; b2.style.borderTopRightRadius = 1; box.Add(b2);
            var b3 = new VisualElement(); b3.style.width = 3.5f; b3.style.height = 9; b3.style.backgroundColor = color; b3.style.borderTopLeftRadius = 1; b3.style.borderTopRightRadius = 1; box.Add(b3);

            // 调度简单波浪起伏
            float t = 0f;
            box.schedule.Execute(() =>
            {
                t += 0.2f;
                b1.style.height = 4f + Mathf.PingPong(t * 12f, 9f);
                b2.style.height = 5f + Mathf.PingPong(t * 15f + 2f, 8f);
                b3.style.height = 4f + Mathf.PingPong(t * 10f + 4f, 9f);
            }).Every(80);

            return box;
        }

        /// <summary>
        /// 工业平台微徽章胶囊（干练大写字母缩写，如 [FM], [LINE], [DISP], [CLOUD], [NETEASE] 等）
        /// </summary>
        public static VisualElement CreatePlatformCapsule(string code, Color badgeColor)
        {
            var cap = new VisualElement();
            cap.style.height = 16;
            cap.style.paddingLeft = 5;
            cap.style.paddingRight = 5;
            cap.style.backgroundColor = new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.16f);
            cap.style.borderTopWidth = 1; cap.style.borderBottomWidth = 1;
            cap.style.borderLeftWidth = 1; cap.style.borderRightWidth = 1;
            cap.style.borderTopColor = new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.70f);
            cap.style.borderBottomColor = cap.style.borderTopColor;
            cap.style.borderLeftColor = cap.style.borderTopColor;
            cap.style.borderRightColor = cap.style.borderTopColor;
            cap.style.borderTopLeftRadius = 3; cap.style.borderTopRightRadius = 3;
            cap.style.borderBottomLeftRadius = 3; cap.style.borderBottomRightRadius = 3;
            cap.style.alignItems = Align.Center;
            cap.style.justifyContent = Justify.Center;
            cap.pickingMode = PickingMode.Ignore;

            var lbl = new Label(code);
            lbl.style.fontSize = 8.5f;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.color = badgeColor;
            lbl.style.paddingTop = 0; lbl.style.paddingBottom = 0;
            lbl.style.paddingLeft = 0; lbl.style.paddingRight = 0;
            lbl.pickingMode = PickingMode.Ignore;
            cap.Add(lbl);

            return cap;
        }

        /// <summary>
        /// 纯代码绘制精致红心（替换字符 ♥）
        /// </summary>
        public static VisualElement CreateHeartIcon(bool isFilled, Color color)
        {
            var box = new VisualElement();
            box.style.width = 14;
            box.style.height = 14;
            box.style.position = Position.Relative;
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            box.pickingMode = PickingMode.Ignore;

            var lbl = new Label(isFilled ? "♥" : "♡");
            lbl.style.fontSize = 13;
            lbl.style.color = color;
            lbl.style.paddingTop = 0; lbl.style.paddingBottom = 0;
            lbl.style.paddingLeft = 0; lbl.style.paddingRight = 0;
            lbl.pickingMode = PickingMode.Ignore;
            box.Add(lbl);

            return box;
        }

        /// <summary>
        /// 纯代码绘制精致实心三角形播放图标 ▶
        /// </summary>
        public static VisualElement CreatePlayIcon(float size, Color color)
        {
            var icon = new VisualElement();
            icon.style.width = 0;
            icon.style.height = 0;
            icon.style.borderTopWidth = size * 0.5f;
            icon.style.borderBottomWidth = size * 0.5f;
            icon.style.borderLeftWidth = size * 0.86f;
            icon.style.borderRightWidth = 0;
            icon.style.borderTopColor = Color.clear;
            icon.style.borderBottomColor = Color.clear;
            icon.style.borderLeftColor = color;
            icon.style.borderRightColor = Color.clear;
            icon.style.marginLeft = size * 0.15f; // 光学居中微调
            icon.pickingMode = PickingMode.Ignore;
            return icon;
        }

        /// <summary>
        /// 纯代码绘制标准等宽间距暂停图标 ⏸（彻底消除诡异方块）
        /// </summary>
        public static VisualElement CreatePauseIcon(float barWidth, float barHeight, float gap, Color color)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;
            container.style.width = barWidth * 2 + gap;
            container.style.height = barHeight;
            container.pickingMode = PickingMode.Ignore;

            var leftBar = new VisualElement();
            leftBar.style.width = barWidth;
            leftBar.style.height = barHeight;
            leftBar.style.backgroundColor = color;
            leftBar.style.borderTopLeftRadius = 1.5f; leftBar.style.borderTopRightRadius = 1.5f;
            leftBar.style.borderBottomLeftRadius = 1.5f; leftBar.style.borderBottomRightRadius = 1.5f;
            leftBar.pickingMode = PickingMode.Ignore;
            container.Add(leftBar);

            var space = new VisualElement();
            space.style.width = gap;
            space.pickingMode = PickingMode.Ignore;
            container.Add(space);

            var rightBar = new VisualElement();
            rightBar.style.width = barWidth;
            rightBar.style.height = barHeight;
            rightBar.style.backgroundColor = color;
            rightBar.style.borderTopLeftRadius = 1.5f; rightBar.style.borderTopRightRadius = 1.5f;
            rightBar.style.borderBottomLeftRadius = 1.5f; rightBar.style.borderBottomRightRadius = 1.5f;
            rightBar.pickingMode = PickingMode.Ignore;
            container.Add(rightBar);

            return container;
        }

        /// <summary>
        /// 纯代码绘制上一首 / 下一首图标 (|◀ 或 ▶|)
        /// </summary>
        public static VisualElement CreateTrackNavIcon(bool isNext, float size, Color color)
        {
            var box = new VisualElement();
            box.style.flexDirection = FlexDirection.Row;
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            box.pickingMode = PickingMode.Ignore;

            var bar = new VisualElement();
            bar.style.width = 2.5f;
            bar.style.height = size;
            bar.style.backgroundColor = color;
            bar.style.borderTopLeftRadius = 1; bar.style.borderTopRightRadius = 1;
            bar.style.borderBottomLeftRadius = 1; bar.style.borderBottomRightRadius = 1;
            bar.pickingMode = PickingMode.Ignore;

            var tri = new VisualElement();
            tri.style.width = 0; tri.style.height = 0;
            tri.pickingMode = PickingMode.Ignore;

            if (isNext)
            {
                tri.style.borderTopWidth = size * 0.45f;
                tri.style.borderBottomWidth = size * 0.45f;
                tri.style.borderLeftWidth = size * 0.75f;
                tri.style.borderTopColor = Color.clear;
                tri.style.borderBottomColor = Color.clear;
                tri.style.borderLeftColor = color;
                tri.style.marginRight = 2f;
                box.Add(tri);
                box.Add(bar);
            }
            else
            {
                tri.style.borderTopWidth = size * 0.45f;
                tri.style.borderBottomWidth = size * 0.45f;
                tri.style.borderRightWidth = size * 0.75f;
                tri.style.borderTopColor = Color.clear;
                tri.style.borderBottomColor = Color.clear;
                tri.style.borderRightColor = color;
                tri.style.marginLeft = 2f;
                box.Add(bar);
                box.Add(tri);
            }

            return box;
        }

        // ==================== 随身听现代高精度平滑矢量图标系统 (SDF 抗锯齿渲染) ====================
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> s_vectorIconCache = new System.Collections.Generic.Dictionary<string, Texture2D>();

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (a.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (a.x - a.x) * (p.y - a.y);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        private static float DistToTriangleEdge(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            if (PointInTriangle(p, a, b, c)) return 0f;
            float d1 = DistToSegment(p, a, b);
            float d2 = DistToSegment(p, b, c);
            float d3 = DistToSegment(p, c, a);
            return Mathf.Min(d1, Mathf.Min(d2, d3));
        }

        /// <summary>
        /// 获取超平滑交叉双箭头随机图标纹理 (48×48 RGBA32 抗锯齿)
        /// </summary>
        public static Texture2D GetShuffleTexture(Color color)
        {
            string key = $"shuffle_{color.r:F2}_{color.g:F2}_{color.b:F2}_{color.a:F2}";
            if (s_vectorIconCache.TryGetValue(key, out var tex) && tex != null) return tex;

            const int S = 48;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var cols = new Color32[S * S];
            Color32 clear = new Color32(0, 0, 0, 0);

            // 线条半径 1.35px (线宽约 2.7px)
            const float R = 1.35f;

            // 1. 上穿下主线 (8,34) -> (17,34) -> (31,14) -> (36,14)
            Vector2 p1A = new Vector2(8, 34), p1B = new Vector2(17, 34);
            Vector2 p1C = new Vector2(31, 14), p1D = new Vector2(36, 14);

            // 2. 下穿上主线 (8,14) -> (17,14) -> (31,34) -> (36,34)
            Vector2 p2A = new Vector2(8, 14), p2B = new Vector2(17, 14);
            Vector2 p2C = new Vector2(31, 34), p2D = new Vector2(36, 34);

            // 3. 右上箭头 (指向右方)
            Vector2 aTopTip = new Vector2(42, 34), aTopU = new Vector2(34, 40), aTopD = new Vector2(34, 28);
            // 4. 右下箭头 (指向右方)
            Vector2 aBotTip = new Vector2(42, 14), aBotU = new Vector2(34, 20), aBotD = new Vector2(34, 8);

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                    float dLine1 = Mathf.Min(DistToSegment(p, p1A, p1B), Mathf.Min(DistToSegment(p, p1B, p1C), DistToSegment(p, p1C, p1D)));
                    float dLine2 = Mathf.Min(DistToSegment(p, p2A, p2B), Mathf.Min(DistToSegment(p, p2B, p2C), DistToSegment(p, p2C, p2D)));
                    float dLine = Mathf.Min(dLine1, dLine2);

                    float dArrTop = DistToTriangleEdge(p, aTopTip, aTopU, aTopD);
                    float dArrBot = DistToTriangleEdge(p, aBotTip, aBotU, aBotD);
                    float dArr = Mathf.Min(dArrTop, dArrBot);

                    float dFinal = Mathf.Min(Mathf.Max(0f, dLine - R), dArr);

                    if (dFinal <= 0.05f)
                    {
                        cols[y * S + x] = color;
                    }
                    else if (dFinal <= 1.15f)
                    {
                        float alpha = Mathf.Clamp01(1f - (dFinal - 0.05f) / 1.10f) * color.a;
                        cols[y * S + x] = new Color(color.r, color.g, color.b, alpha);
                    }
                    else
                    {
                        cols[y * S + x] = clear;
                    }
                }
            }

            t.SetPixels32(cols);
            t.Apply();
            s_vectorIconCache[key] = t;
            return t;
        }

        /// <summary>
        /// 获取超平滑双向循环回路图标 (48×48 RGBA32 抗锯齿，可指定是否单曲循环带高清数字 1)
        /// </summary>
        public static Texture2D GetRepeatTexture(Color color, bool isSingleRepeat)
        {
            string key = $"repeat_{(isSingleRepeat ? "one" : "all")}_{color.r:F2}_{color.g:F2}_{color.b:F2}_{color.a:F2}";
            if (s_vectorIconCache.TryGetValue(key, out var tex) && tex != null) return tex;

            const int S = 48;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var cols = new Color32[S * S];
            Color32 clear = new Color32(0, 0, 0, 0);

            const float R = 1.35f;

            // 顺时针圆角矩形回路各控制点
            // 上平直段向右: (16, 35) -> (33, 35)
            Vector2 pTopL = new Vector2(16, 35), pTopR = new Vector2(33, 35);
            // 右竖直段向下: (38, 30) -> (38, 18)
            Vector2 pRightT = new Vector2(38, 30), pRightB = new Vector2(38, 18);
            // 下平直段向左: (33, 13) -> (16, 13)
            Vector2 pBotR = new Vector2(33, 13), pBotL = new Vector2(16, 13);
            // 左竖直段向上: (11, 18) -> (11, 30)
            Vector2 pLeftB = new Vector2(11, 18), pLeftT = new Vector2(11, 30);

            // 四角平滑折弯圆心与半径 (R_corner = 5.0)
            Vector2 cTR = new Vector2(33, 30);
            Vector2 cBR = new Vector2(33, 18);
            Vector2 cBL = new Vector2(16, 18);
            Vector2 cTL = new Vector2(16, 30);
            const float RCorn = 5.0f;

            // 右上顺时针箭头 (位于 (38, 26) 朝下)
            Vector2 aTopTip = new Vector2(38, 23), aTopL = new Vector2(33, 29), aTopR = new Vector2(43, 29);
            // 左下顺时针箭头 (位于 (11, 22) 朝上)
            Vector2 aBotTip = new Vector2(11, 25), aBotL = new Vector2(6, 19), aBotR = new Vector2(16, 19);

            // 数字 1 矢量路径 (回路正中央)
            Vector2 n1StemB = new Vector2(24.5f, 19), n1StemT = new Vector2(24.5f, 29);
            Vector2 n1HookL = new Vector2(21.5f, 26.5f), n1HookR = new Vector2(24.5f, 29);
            Vector2 n1BaseL = new Vector2(21.5f, 19), n1BaseR = new Vector2(27.5f, 19);

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                    // 计算回路骨架距离
                    float dTop = DistToSegment(p, pTopL, pTopR);
                    float dRight = DistToSegment(p, pRightT, pRightB);
                    float dBot = DistToSegment(p, pBotR, pBotL);
                    float dLeft = DistToSegment(p, pLeftB, pLeftT);

                    float dTR = (p.x >= cTR.x && p.y >= cTR.y) ? Mathf.Abs((p - cTR).magnitude - RCorn) : 99f;
                    float dBR = (p.x >= cBR.x && p.y <= cBR.y) ? Mathf.Abs((p - cBR).magnitude - RCorn) : 99f;
                    float dBL = (p.x <= cBL.x && p.y <= cBL.y) ? Mathf.Abs((p - cBL).magnitude - RCorn) : 99f;
                    float dTL = (p.x <= cTL.x && p.y >= cTL.y) ? Mathf.Abs((p - cTL).magnitude - RCorn) : 99f;

                    float dLoop = Mathf.Min(Mathf.Min(dTop, dRight), Mathf.Min(dBot, dLeft));
                    dLoop = Mathf.Min(dLoop, Mathf.Min(Mathf.Min(dTR, dBR), Mathf.Min(dBL, dTL)));

                    float dArr1 = DistToTriangleEdge(p, aTopTip, aTopL, aTopR);
                    float dArr2 = DistToTriangleEdge(p, aBotTip, aBotL, aBotR);
                    float dArr = Mathf.Min(dArr1, dArr2);

                    float dFinal = Mathf.Min(Mathf.Max(0f, dLoop - R), dArr);

                    if (isSingleRepeat)
                    {
                        float dStem = Mathf.Max(0f, DistToSegment(p, n1StemB, n1StemT) - 1.25f);
                        float dHook = Mathf.Max(0f, DistToSegment(p, n1HookL, n1HookR) - 1.05f);
                        float dBase = Mathf.Max(0f, DistToSegment(p, n1BaseL, n1BaseR) - 1.05f);
                        float dOne = Mathf.Min(dStem, Mathf.Min(dHook, dBase));
                        dFinal = Mathf.Min(dFinal, dOne);
                    }

                    if (dFinal <= 0.05f)
                    {
                        cols[y * S + x] = color;
                    }
                    else if (dFinal <= 1.15f)
                    {
                        float alpha = Mathf.Clamp01(1f - (dFinal - 0.05f) / 1.10f) * color.a;
                        cols[y * S + x] = new Color(color.r, color.g, color.b, alpha);
                    }
                    else
                    {
                        cols[y * S + x] = clear;
                    }
                }
            }

            t.SetPixels32(cols);
            t.Apply();
            s_vectorIconCache[key] = t;
            return t;
        }

        /// <summary>
        /// 获取超平滑上一首 / 下一首图标纹理 (48×48 RGBA32 抗锯齿)
        /// </summary>
        public static Texture2D GetTrackNavTexture(bool isNext, Color color)
        {
            string key = $"nav_{(isNext ? "next" : "prev")}_{color.r:F2}_{color.g:F2}_{color.b:F2}_{color.a:F2}";
            if (s_vectorIconCache.TryGetValue(key, out var tex) && tex != null) return tex;

            const int S = 48;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var cols = new Color32[S * S];
            Color32 clear = new Color32(0, 0, 0, 0);

            Vector2 triA, triB, triC;
            Vector2 barA, barB;
            const float barRadius = 1.4f;

            if (isNext)
            {
                // 朝右等边三角形 + 右侧竖直圆角挡板
                triA = new Vector2(28f, 24f); // 尖端
                triB = new Vector2(14f, 35f); // 左上
                triC = new Vector2(14f, 13f); // 左下
                barA = new Vector2(33f, 13f);
                barB = new Vector2(33f, 35f);
            }
            else
            {
                // 左侧竖直圆角挡板 + 朝左等边三角形
                barA = new Vector2(15f, 13f);
                barB = new Vector2(15f, 35f);
                triA = new Vector2(20f, 24f); // 尖端
                triB = new Vector2(34f, 35f); // 右上
                triC = new Vector2(34f, 13f); // 右下
            }

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                    float dTri = DistToTriangleEdge(p, triA, triB, triC);
                    float dBar = Mathf.Max(0f, DistToSegment(p, barA, barB) - barRadius);
                    float dFinal = Mathf.Min(dTri, dBar);

                    if (dFinal <= 0.05f)
                    {
                        cols[y * S + x] = color;
                    }
                    else if (dFinal <= 1.15f)
                    {
                        float alpha = Mathf.Clamp01(1f - (dFinal - 0.05f) / 1.10f) * color.a;
                        cols[y * S + x] = new Color(color.r, color.g, color.b, alpha);
                    }
                    else
                    {
                        cols[y * S + x] = clear;
                    }
                }
            }

            t.SetPixels32(cols);
            t.Apply();
            s_vectorIconCache[key] = t;
            return t;
        }



        /// <summary>
        /// 纯 VisualElement 几何构建双箭头环形循环回路（列表循环 / 单曲循环带 1）
        /// 绝对锐利、100% 矢量、零贴图依赖、永不走形！
        /// </summary>
        public static VisualElement CreateLoopRepeatIcon(Color color, bool isSingleRepeat)
        {
            var box = new VisualElement();
            box.style.width = 20;
            box.style.height = 16;
            box.style.position = Position.Relative;
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            box.pickingMode = PickingMode.Ignore;

            // 环形外框：带 3px 圆角的中空回路
            var loop = new VisualElement();
            loop.style.width = 15;
            loop.style.height = 11;
            loop.style.borderTopWidth = 1.6f;
            loop.style.borderBottomWidth = 1.6f;
            loop.style.borderLeftWidth = 1.6f;
            loop.style.borderRightWidth = 1.6f;
            loop.style.borderTopLeftRadius = 3f;
            loop.style.borderTopRightRadius = 3f;
            loop.style.borderBottomLeftRadius = 3f;
            loop.style.borderBottomRightRadius = 3f;
            loop.style.borderTopColor = color;
            loop.style.borderBottomColor = color;
            loop.style.borderLeftColor = color;
            loop.style.borderRightColor = color;
            loop.style.alignItems = Align.Center;
            loop.style.justifyContent = Justify.Center;
            loop.pickingMode = PickingMode.Ignore;
            box.Add(loop);

            // 右上箭头（朝右）：在右上角叠放一个朝右的实心小三角形
            var topArr = new VisualElement();
            topArr.style.position = Position.Absolute;
            topArr.style.top = 0;
            topArr.style.right = 0;
            topArr.style.width = 0;
            topArr.style.height = 0;
            topArr.style.borderTopWidth = 2.8f;
            topArr.style.borderBottomWidth = 2.8f;
            topArr.style.borderLeftWidth = 4.2f;
            topArr.style.borderRightWidth = 0;
            topArr.style.borderTopColor = Color.clear;
            topArr.style.borderBottomColor = Color.clear;
            topArr.style.borderLeftColor = color;
            topArr.style.borderRightColor = Color.clear;
            topArr.pickingMode = PickingMode.Ignore;
            box.Add(topArr);

            // 左下箭头（朝左）：在左下角叠放一个朝左的实心小三角形
            var botArr = new VisualElement();
            botArr.style.position = Position.Absolute;
            botArr.style.bottom = 0;
            botArr.style.left = 0;
            botArr.style.width = 0;
            botArr.style.height = 0;
            botArr.style.borderTopWidth = 2.8f;
            botArr.style.borderBottomWidth = 2.8f;
            botArr.style.borderRightWidth = 4.2f;
            botArr.style.borderLeftWidth = 0;
            botArr.style.borderTopColor = Color.clear;
            botArr.style.borderBottomColor = Color.clear;
            botArr.style.borderRightColor = color;
            botArr.style.borderLeftColor = Color.clear;
            botArr.pickingMode = PickingMode.Ignore;
            box.Add(botArr);

            if (isSingleRepeat)
            {
                var lbl = new Label("1");
                lbl.style.fontSize = 8.5f;
                lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                lbl.style.color = color;
                lbl.style.unityFontDefinition = Fd();
                lbl.style.position = Position.Absolute;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                lbl.pickingMode = PickingMode.Ignore;
                box.Add(lbl);
            }

            return box;
        }

        /// <summary>
        /// 纯 VisualElement 几何构建交叉双折线双箭头（随机播放 Shuffle）
        /// </summary>
        public static VisualElement CreateCrossShuffleIcon(Color color)
        {
            var box = new VisualElement();
            box.style.width = 20;
            box.style.height = 16;
            box.style.position = Position.Relative;
            box.pickingMode = PickingMode.Ignore;

            // 左上引入水平线
            var inTop = new VisualElement();
            inTop.style.position = Position.Absolute;
            inTop.style.left = 1; inTop.style.top = 3.5f;
            inTop.style.width = 4.5f; inTop.style.height = 1.6f;
            inTop.style.backgroundColor = color;
            inTop.pickingMode = PickingMode.Ignore;
            box.Add(inTop);

            // 左下引入水平线
            var inBot = new VisualElement();
            inBot.style.position = Position.Absolute;
            inBot.style.left = 1; inBot.style.bottom = 3.5f;
            inBot.style.width = 4.5f; inBot.style.height = 1.6f;
            inBot.style.backgroundColor = color;
            inBot.pickingMode = PickingMode.Ignore;
            box.Add(inBot);

            // 交叉斜线 1 (左上到右下)
            var slash1 = new VisualElement();
            slash1.style.position = Position.Absolute;
            slash1.style.left = 4.5f; slash1.style.top = 7f;
            slash1.style.width = 9.5f; slash1.style.height = 1.6f;
            slash1.style.backgroundColor = color;
            slash1.style.rotate = new Rotate(Angle.Degrees(38f));
            slash1.pickingMode = PickingMode.Ignore;
            box.Add(slash1);

            // 交叉斜线 2 (左下到右上)
            var slash2 = new VisualElement();
            slash2.style.position = Position.Absolute;
            slash2.style.left = 4.5f; slash2.style.top = 7f;
            slash2.style.width = 9.5f; slash2.style.height = 1.6f;
            slash2.style.backgroundColor = color;
            slash2.style.rotate = new Rotate(Angle.Degrees(-38f));
            slash2.pickingMode = PickingMode.Ignore;
            box.Add(slash2);

            // 右上朝右实心箭头
            var outTopArr = new VisualElement();
            outTopArr.style.position = Position.Absolute;
            outTopArr.style.right = 1; outTopArr.style.top = 1.5f;
            outTopArr.style.width = 0; outTopArr.style.height = 0;
            outTopArr.style.borderTopWidth = 2.8f; outTopArr.style.borderBottomWidth = 2.8f;
            outTopArr.style.borderLeftWidth = 4.2f; outTopArr.style.borderRightWidth = 0;
            outTopArr.style.borderTopColor = Color.clear; outTopArr.style.borderBottomColor = Color.clear;
            outTopArr.style.borderLeftColor = color; outTopArr.style.borderRightColor = Color.clear;
            outTopArr.pickingMode = PickingMode.Ignore;
            box.Add(outTopArr);

            // 右下朝右实心箭头
            var outBotArr = new VisualElement();
            outBotArr.style.position = Position.Absolute;
            outBotArr.style.right = 1; outBotArr.style.bottom = 1.5f;
            outBotArr.style.width = 0; outBotArr.style.height = 0;
            outBotArr.style.borderTopWidth = 2.8f; outBotArr.style.borderBottomWidth = 2.8f;
            outBotArr.style.borderLeftWidth = 4.2f; outBotArr.style.borderRightWidth = 0;
            outBotArr.style.borderTopColor = Color.clear; outBotArr.style.borderBottomColor = Color.clear;
            outBotArr.style.borderLeftColor = color; outBotArr.style.borderRightColor = Color.clear;
            outBotArr.pickingMode = PickingMode.Ignore;
            box.Add(outBotArr);

            return box;
        }

        public static VisualElement CreateSingleRepeatIcon(Color color) => CreateLoopRepeatIcon(color, true);
        public static VisualElement CreateSequentialIcon(Color color) => CreateLoopRepeatIcon(color, false);
        public static VisualElement CreateShuffleIcon(Color color) => CreateCrossShuffleIcon(color);

        /// <summary>
        /// 纯矢量无边框播放模式循环控制器（单键轮转：0:列表循环, 1:单曲循环, 2:随机播放）
        /// 彻底消除方块边框与模糊贴图，纯 VisualElement 构建！
        /// </summary>
        public static VisualElement CreateUnifiedPlayModeControl(int mode, System.Action onClick)
        {
            var btn = new Button(() => onClick?.Invoke());
            btn.style.width = 32;
            btn.style.height = 32;
            btn.style.alignItems = Align.Center;
            btn.style.justifyContent = Justify.Center;
            btn.style.backgroundColor = Color.clear;
            btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
            btn.style.position = Position.Relative;
            btn.pickingMode = PickingMode.Position;

            Color activeCol = new Color(0.96f, 0.82f, 0.45f, 1f); // 亮金
            Color mutedCol = new Color(0.70f, 0.65f, 0.58f, 1f);  // 黄铜灰

            VisualElement icon;
            if (mode == 1) // 单曲循环
            {
                icon = CreateLoopRepeatIcon(activeCol, true);
            }
            else if (mode == 2) // 随机播放
            {
                icon = CreateCrossShuffleIcon(activeCol);
            }
            else // 0 列表循环
            {
                icon = CreateLoopRepeatIcon(mutedCol, false);
            }
            icon.pickingMode = PickingMode.Ignore;
            btn.Add(icon);

            // 模式为单曲或随机时，在下方点亮 2.5px 极小金点信标
            var dot = new VisualElement();
            dot.style.position = Position.Absolute;
            dot.style.bottom = 2;
            dot.style.width = 3; dot.style.height = 3;
            dot.style.borderTopLeftRadius = 1.5f; dot.style.borderTopRightRadius = 1.5f;
            dot.style.borderBottomLeftRadius = 1.5f; dot.style.borderBottomRightRadius = 1.5f;
            dot.style.backgroundColor = activeCol;
            dot.style.display = (mode != 0) ? DisplayStyle.Flex : DisplayStyle.None;
            dot.pickingMode = PickingMode.Ignore;
            btn.Add(dot);

            // 无边框微悬停弹性微缩放
            btn.RegisterCallback<MouseEnterEvent>(e => {
                btn.style.scale = new Scale(new Vector3(1.12f, 1.12f, 1f));
            });
            btn.RegisterCallback<MouseLeaveEvent>(e => {
                btn.style.scale = new Scale(Vector3.one);
            });
            btn.RegisterCallback<PointerDownEvent>(e => {
                btn.style.scale = new Scale(new Vector3(0.92f, 0.92f, 1f));
            });
            btn.RegisterCallback<PointerUpEvent>(e => {
                btn.style.scale = new Scale(new Vector3(1.12f, 1.12f, 1f));
            });

            return btn;
        }

        /// <summary>
        /// 无边框通透图元按键专用微交互（彻底告别沉重方形边框与呆板底色）
        /// </summary>
        public static void ApplyGhostVectorMicroInteraction(
            Button btn,
            VisualElement iconElem,
            Texture2D normalTex,
            Texture2D hoverTex,
            Action onClick = null)
        {
            if (btn == null) return;
            btn.style.backgroundColor = Color.clear;
            btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;

            btn.RegisterCallback<MouseEnterEvent>(e =>
            {
                btn.style.scale = new Scale(new Vector3(1.12f, 1.12f, 1f));
                if (iconElem != null && hoverTex != null)
                {
                    iconElem.style.backgroundImage = new StyleBackground(hoverTex);
                }
            });
            btn.RegisterCallback<MouseLeaveEvent>(e =>
            {
                btn.style.scale = new Scale(Vector3.one);
                if (iconElem != null && normalTex != null)
                {
                    iconElem.style.backgroundImage = new StyleBackground(normalTex);
                }
            });
            btn.RegisterCallback<PointerDownEvent>(e =>
            {
                btn.style.scale = new Scale(new Vector3(0.92f, 0.92f, 1f));
            });
            btn.RegisterCallback<PointerUpEvent>(e =>
            {
                btn.style.scale = new Scale(new Vector3(1.12f, 1.12f, 1f));
            });
        }

        /// <summary>
        /// 纯代码绘制精致黄铜小锁 🔒（锁梁 + 锁体）
        /// </summary>
        public static VisualElement CreateLockIcon(Color brassColor)
        {
            var lockBox = new VisualElement();
            lockBox.style.width = 16; lockBox.style.height = 18;
            lockBox.style.alignItems = Align.Center;
            lockBox.pickingMode = PickingMode.Ignore;

            // 锁梁（拱门半圆）
            var shackle = new VisualElement();
            shackle.style.width = 10; shackle.style.height = 8;
            shackle.style.borderTopWidth = 2; shackle.style.borderLeftWidth = 2; shackle.style.borderRightWidth = 2;
            shackle.style.borderTopColor = brassColor; shackle.style.borderLeftColor = brassColor; shackle.style.borderRightColor = brassColor;
            shackle.style.borderTopLeftRadius = 5; shackle.style.borderTopRightRadius = 5;
            shackle.pickingMode = PickingMode.Ignore;
            lockBox.Add(shackle);

            // 锁体（长方块）
            var body = new VisualElement();
            body.style.width = 14; body.style.height = 10;
            body.style.backgroundColor = brassColor;
            body.style.borderTopLeftRadius = 2; body.style.borderTopRightRadius = 2;
            body.style.borderBottomLeftRadius = 2; body.style.borderBottomRightRadius = 2;
            body.style.alignItems = Align.Center; body.style.justifyContent = Justify.Center;
            body.pickingMode = PickingMode.Ignore;
            lockBox.Add(body);

            // 锁孔
            var keyhole = new VisualElement();
            keyhole.style.width = 2; keyhole.style.height = 4;
            keyhole.style.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            keyhole.pickingMode = PickingMode.Ignore;
            body.Add(keyhole);

            return lockBox;
        }

        /// <summary>
        /// 彻底定制并美化 ScrollView 滚动条
        /// </summary>
        public static void BeautifyScrollView(ScrollView scroll)
        {
            if (scroll == null) return;
            scroll.mode = ScrollViewMode.Vertical;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            if (scroll.horizontalScroller != null)
            {
                scroll.horizontalScroller.style.display = DisplayStyle.None;
                scroll.horizontalScroller.style.height = 0;
            }
            var hScroller = scroll.Q(className: "unity-scroll-view__horizontal-scroller");
            if (hScroller != null)
            {
                hScroller.style.display = DisplayStyle.None;
                hScroller.style.height = 0;
            }

            var scroller = scroll.verticalScroller;
            if (scroller == null) return;

            // 隐藏 Windows 95 风格的上下小箭头
            var lowBtn = scroller.Q(className: "unity-scroller__low-button");
            if (lowBtn != null) lowBtn.style.display = DisplayStyle.None;
            var highBtn = scroller.Q(className: "unity-scroller__high-button");
            if (highBtn != null) highBtn.style.display = DisplayStyle.None;

            // 滑轨变极细、背景透明
            scroller.style.width = 5;
            scroller.style.backgroundColor = Color.clear;
            scroller.style.borderLeftWidth = 0; scroller.style.borderRightWidth = 0;

            var slider = scroller.slider;
            if (slider != null)
            {
                slider.style.backgroundColor = Color.clear;
                slider.style.borderLeftWidth = 0; slider.style.borderRightWidth = 0;
                var tracker = slider.Q(className: "unity-base-slider__tracker");
                if (tracker != null)
                {
                    tracker.style.backgroundColor = new Color(1f, 1f, 1f, 0.05f);
                    tracker.style.borderLeftWidth = 0; tracker.style.borderRightWidth = 0;
                    tracker.style.width = 4;
                    tracker.style.borderTopLeftRadius = 2; tracker.style.borderTopRightRadius = 2;
                    tracker.style.borderBottomLeftRadius = 2; tracker.style.borderBottomRightRadius = 2;
                }
                var dragger = slider.Q(className: "unity-base-slider__dragger");
                if (dragger != null)
                {
                    dragger.style.backgroundColor = new Color(0.83f, 0.66f, 0.26f, 0.45f); // 琥珀金半透明
                    dragger.style.width = 4;
                    dragger.style.borderTopLeftRadius = 2; dragger.style.borderTopRightRadius = 2;
                    dragger.style.borderBottomLeftRadius = 2; dragger.style.borderBottomRightRadius = 2;
                    dragger.style.borderTopWidth = 0; dragger.style.borderBottomWidth = 0;
                    dragger.style.borderLeftWidth = 0; dragger.style.borderRightWidth = 0;
                }
            }
        }

        /// <summary>
        /// 创建四角金属铆钉/螺丝小件（具有凹陷与金属光泽感）
        /// </summary>
        public static VisualElement CreateRivet(float size = 4.5f)
        {
            var rivet = new VisualElement();
            rivet.style.width = size;
            rivet.style.height = size;
            rivet.style.borderTopLeftRadius = size * 0.5f;
            rivet.style.borderTopRightRadius = size * 0.5f;
            rivet.style.borderBottomLeftRadius = size * 0.5f;
            rivet.style.borderBottomRightRadius = size * 0.5f;
            rivet.style.backgroundColor = new Color(0.24f, 0.22f, 0.19f, 1f);
            rivet.style.borderTopWidth = 0.8f; rivet.style.borderLeftWidth = 0.8f;
            rivet.style.borderBottomWidth = 0.8f; rivet.style.borderRightWidth = 0.8f;
            rivet.style.borderTopColor = new Color(0.55f, 0.48f, 0.35f, 0.9f); // 金属上高光
            rivet.style.borderLeftColor = new Color(0.55f, 0.48f, 0.35f, 0.9f);
            rivet.style.borderBottomColor = new Color(0.08f, 0.08f, 0.10f, 0.9f); // 金属下暗影
            rivet.style.borderRightColor = new Color(0.08f, 0.08f, 0.10f, 0.9f);
            rivet.pickingMode = PickingMode.Ignore;
            return rivet;
        }

        /// <summary>
        /// 创建复古车载磁带盒/双卷盘微缩图标（代替单调方块，浓郁的铁路复古机械味）
        /// </summary>
        public static VisualElement CreateCassetteMini(float width = 46f, float height = 46f, bool isPlaying = false)
        {
            var box = new VisualElement();
            box.style.width = width; box.style.height = height;
            box.style.backgroundColor = new Color(0.11f, 0.11f, 0.14f, 1f);
            box.style.borderTopLeftRadius = 6; box.style.borderTopRightRadius = 6;
            box.style.borderBottomLeftRadius = 6; box.style.borderBottomRightRadius = 6;
            box.style.borderTopWidth = 1.2f; box.style.borderBottomWidth = 1.2f;
            box.style.borderLeftWidth = 1.2f; box.style.borderRightWidth = 1.2f;
            box.style.borderTopColor = isPlaying ? new Color(0.83f, 0.66f, 0.26f, 0.9f) : new Color(0.32f, 0.30f, 0.26f, 0.85f);
            box.style.borderBottomColor = isPlaying ? new Color(0.83f, 0.66f, 0.26f, 0.9f) : new Color(0.32f, 0.30f, 0.26f, 0.85f);
            box.style.borderLeftColor = isPlaying ? new Color(0.83f, 0.66f, 0.26f, 0.9f) : new Color(0.32f, 0.30f, 0.26f, 0.85f);
            box.style.borderRightColor = isPlaying ? new Color(0.83f, 0.66f, 0.26f, 0.9f) : new Color(0.32f, 0.30f, 0.26f, 0.85f);
            box.style.alignItems = Align.Center; box.style.justifyContent = Justify.Center;
            box.pickingMode = PickingMode.Ignore;

            // 磁带标签纸带
            var labelStripe = new VisualElement();
            labelStripe.style.width = Length.Percent(84);
            labelStripe.style.height = 14;
            labelStripe.style.backgroundColor = isPlaying ? new Color(0.83f, 0.66f, 0.26f, 0.3f) : new Color(0.20f, 0.20f, 0.24f, 1f);
            labelStripe.style.borderTopLeftRadius = 2; labelStripe.style.borderTopRightRadius = 2;
            labelStripe.style.borderBottomLeftRadius = 2; labelStripe.style.borderBottomRightRadius = 2;
            labelStripe.style.flexDirection = FlexDirection.Row;
            labelStripe.style.alignItems = Align.Center;
            labelStripe.style.justifyContent = Justify.SpaceAround;
            labelStripe.pickingMode = PickingMode.Ignore;
            box.Add(labelStripe);

            // 左齿轮孔
            var reelLeft = new VisualElement();
            reelLeft.style.width = 8; reelLeft.style.height = 8;
            reelLeft.style.borderTopLeftRadius = 4; reelLeft.style.borderTopRightRadius = 4;
            reelLeft.style.borderBottomLeftRadius = 4; reelLeft.style.borderBottomRightRadius = 4;
            reelLeft.style.backgroundColor = isPlaying ? new Color(0.96f, 0.82f, 0.45f, 1f) : new Color(0.40f, 0.38f, 0.35f, 1f);
            reelLeft.style.borderTopWidth = 1.5f; reelLeft.style.borderBottomWidth = 1.5f;
            reelLeft.style.borderLeftWidth = 1.5f; reelLeft.style.borderRightWidth = 1.5f;
            reelLeft.style.borderTopColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            reelLeft.style.borderBottomColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            reelLeft.style.borderLeftColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            reelLeft.style.borderRightColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            reelLeft.pickingMode = PickingMode.Ignore;
            labelStripe.Add(reelLeft);

            // 中间透明磁头视窗
            var centerWindow = new VisualElement();
            centerWindow.style.width = 12; centerWindow.style.height = 6;
            centerWindow.style.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 0.9f);
            centerWindow.style.borderTopLeftRadius = 1; centerWindow.style.borderTopRightRadius = 1;
            centerWindow.style.borderBottomLeftRadius = 1; centerWindow.style.borderBottomRightRadius = 1;
            centerWindow.pickingMode = PickingMode.Ignore;
            labelStripe.Add(centerWindow);

            // 右齿轮孔
            var reelRight = new VisualElement();
            reelRight.style.width = 8; reelRight.style.height = 8;
            reelRight.style.borderTopLeftRadius = 4; reelRight.style.borderTopRightRadius = 4;
            reelRight.style.borderBottomLeftRadius = 4; reelRight.style.borderBottomRightRadius = 4;
            reelRight.style.backgroundColor = isPlaying ? new Color(0.96f, 0.82f, 0.45f, 1f) : new Color(0.40f, 0.38f, 0.35f, 1f);
            reelRight.style.borderTopWidth = 1.5f; reelRight.style.borderBottomWidth = 1.5f;
            reelRight.style.borderLeftWidth = 1.5f; reelRight.style.borderRightWidth = 1.5f;
            reelRight.style.borderTopColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            reelRight.style.borderBottomColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            reelRight.style.borderLeftColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            reelRight.style.borderRightColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            reelRight.pickingMode = PickingMode.Ignore;
            labelStripe.Add(reelRight);

            // 底部磁带导带头梯形小窗
            var headTrap = new VisualElement();
            headTrap.style.width = 18; headTrap.style.height = 4;
            headTrap.style.backgroundColor = new Color(0.18f, 0.18f, 0.22f, 0.9f);
            headTrap.style.marginTop = 3;
            headTrap.style.borderTopLeftRadius = 1; headTrap.style.borderTopRightRadius = 1;
            headTrap.pickingMode = PickingMode.Ignore;
            box.Add(headTrap);

            return box;
        }

        /// <summary>
        /// 创建调频收音机/车载广播的刻度齿列（增强轨道的机械仪表刻度感）
        /// </summary>
        public static VisualElement CreateScaleTickMarks(int count = 28)
        {
            var ticksRow = new VisualElement();
            ticksRow.style.position = Position.Absolute;
            ticksRow.style.left = 0; ticksRow.style.right = 0;
            ticksRow.style.top = -4; ticksRow.style.bottom = -4;
            ticksRow.style.flexDirection = FlexDirection.Row;
            ticksRow.style.justifyContent = Justify.SpaceBetween;
            ticksRow.style.alignItems = Align.Center;
            ticksRow.pickingMode = PickingMode.Ignore;

            for (int i = 0; i < count; i++)
            {
                var tick = new VisualElement();
                bool isMajor = (i % 5 == 0);
                tick.style.width = 1f;
                tick.style.height = isMajor ? 8f : 4f;
                tick.style.backgroundColor = isMajor ? new Color(0.83f, 0.66f, 0.26f, 0.45f) : new Color(0.40f, 0.38f, 0.35f, 0.25f);
                tick.pickingMode = PickingMode.Ignore;
                ticksRow.Add(tick);
            }
            return ticksRow;
        }

        /// <summary>
        /// 创建复古收音机调谐红色指针游标（替换普通圆形点，浓郁工业电台感）
        /// </summary>
        public static VisualElement CreateTuningNeedle()
        {
            var needle = new VisualElement();
            needle.style.width = 6;
            needle.style.height = 18;
            needle.style.alignItems = Align.Center;
            needle.style.justifyContent = Justify.Center;
            needle.pickingMode = PickingMode.Ignore;

            // 垂直细红指针
            var bar = new VisualElement();
            bar.style.width = 2.2f;
            bar.style.height = 14;
            bar.style.backgroundColor = new Color(0.95f, 0.28f, 0.22f, 1f); // 鲜明赤红指示针
            bar.style.borderTopLeftRadius = 1; bar.style.borderTopRightRadius = 1;
            bar.style.borderBottomLeftRadius = 1; bar.style.borderBottomRightRadius = 1;
            bar.pickingMode = PickingMode.Ignore;
            needle.Add(bar);

            // 顶部金色微小三角头
            var cap = new VisualElement();
            cap.style.position = Position.Absolute;
            cap.style.top = -1;
            cap.style.width = 5;
            cap.style.height = 4;
            cap.style.backgroundColor = new Color(0.96f, 0.82f, 0.45f, 1f); // 亮金锁头
            cap.style.borderTopLeftRadius = 1; cap.style.borderTopRightRadius = 1;
            cap.pickingMode = PickingMode.Ignore;
            needle.Add(cap);

            return needle;
        }

        /// <summary>
        /// 创建车载电台真空管琥珀指示灯（点亮/熄灭）
        /// </summary>
        public static VisualElement CreatePilotLamp(bool isOn)
        {
            var lamp = new VisualElement();
            lamp.style.width = 10; lamp.style.height = 10;
            lamp.style.borderTopLeftRadius = 5; lamp.style.borderTopRightRadius = 5;
            lamp.style.borderBottomLeftRadius = 5; lamp.style.borderBottomRightRadius = 5;
            lamp.style.borderTopWidth = 1; lamp.style.borderBottomWidth = 1;
            lamp.style.borderLeftWidth = 1; lamp.style.borderRightWidth = 1;
            lamp.style.borderTopColor = new Color(0.35f, 0.30f, 0.24f, 1f);
            lamp.style.borderBottomColor = new Color(0.12f, 0.10f, 0.08f, 1f);
            lamp.style.borderLeftColor = new Color(0.35f, 0.30f, 0.24f, 1f);
            lamp.style.borderRightColor = new Color(0.12f, 0.10f, 0.08f, 1f);
            lamp.style.backgroundColor = isOn ? new Color(1f, 0.65f, 0.15f, 1f) : new Color(0.22f, 0.16f, 0.10f, 0.9f);
            lamp.style.alignItems = Align.Center; lamp.style.justifyContent = Justify.Center;
            lamp.pickingMode = PickingMode.Ignore;

            if (isOn)
            {
                var core = new VisualElement();
                core.style.width = 4; core.style.height = 4;
                core.style.borderTopLeftRadius = 2; core.style.borderTopRightRadius = 2;
                core.style.borderBottomLeftRadius = 2; core.style.borderBottomRightRadius = 2;
                core.style.backgroundColor = new Color(1f, 0.92f, 0.70f, 1f); // 极亮热芯
                core.pickingMode = PickingMode.Ignore;
                lamp.Add(core);
            }
            return lamp;
        }

        /// <summary>
        /// 应用工业实体物理按键质感（高光投影边框，彻底杜绝扁平塑料感）
        /// </summary>
        public static void ApplyIndustrialBevel(VisualElement el, bool isPressed = false, bool isBrassGold = false)
        {
            el.style.borderTopWidth = 1.5f; el.style.borderLeftWidth = 1.5f;
            el.style.borderBottomWidth = 1.8f; el.style.borderRightWidth = 1.8f;

            if (isPressed)
            {
                // 按下状态：反转阴影（上暗下亮，内陷感）
                el.style.borderTopColor = new Color(0.08f, 0.08f, 0.10f, 0.95f);
                el.style.borderLeftColor = new Color(0.08f, 0.08f, 0.10f, 0.95f);
                el.style.borderBottomColor = isBrassGold ? new Color(0.85f, 0.70f, 0.35f, 0.9f) : new Color(0.40f, 0.40f, 0.46f, 0.9f);
                el.style.borderRightColor = isBrassGold ? new Color(0.85f, 0.70f, 0.35f, 0.9f) : new Color(0.40f, 0.40f, 0.46f, 0.9f);
            }
            else
            {
                // 正常凸起状态：上亮下暗
                el.style.borderTopColor = isBrassGold ? new Color(0.96f, 0.85f, 0.50f, 1f) : new Color(0.48f, 0.49f, 0.58f, 0.9f);
                el.style.borderLeftColor = isBrassGold ? new Color(0.96f, 0.85f, 0.50f, 1f) : new Color(0.48f, 0.49f, 0.58f, 0.9f);
                el.style.borderBottomColor = new Color(0.08f, 0.08f, 0.10f, 1f);
                el.style.borderRightColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            }
        }

        private static Texture2D s_frostedGlassTex;
        private static Texture2D s_gaussianShadowTex;

        /// <summary>
        /// 获取高斯模糊/磨砂玻璃和纸微噪点平铺纹理（128×128，双线性平铺，具有微暖沙底与细纤纸纹理）
        /// </summary>
        public static Texture2D GetFrostedGlassTexture()
        {
            if (s_frostedGlassTex != null) return s_frostedGlassTex;

            const int S = 128;
            s_frostedGlassTex = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            uint seed = 20261018u;
            uint Next() { seed = seed * 1664525u + 1013904223u; return seed; }

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float n = ((Next() >> 16) & 0xFF) / 255f;
                    float sandSparkle = (((Next() >> 8) & 0xFF) > 250) ? 0.08f : 0f;
                    float alpha = 0.68f + n * 0.08f + sandSparkle;
                    float r = 0.12f + n * 0.02f + sandSparkle;
                    float g = 0.13f + n * 0.02f + sandSparkle;
                    float b = 0.16f + n * 0.02f;
                    s_frostedGlassTex.SetPixel(x, y, new Color(r, g, b, alpha));
                }
            }
            s_frostedGlassTex.Apply();
            return s_frostedGlassTex;
        }

        /// <summary>
        /// 纯代码生成带高斯衰减软阴影的 9-Slice 贴图（64×64，中心实心，向外高斯扩散羽化）
        /// </summary>
        public static Texture2D GetGaussianShadowTexture()
        {
            if (s_gaussianShadowTex != null) return s_gaussianShadowTex;

            const int S = 64;
            s_gaussianShadowTex = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            const float innerRadius = 14f;
            const float center = 31.5f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - center) - innerRadius);
                    float dy = Mathf.Max(0, Mathf.Abs(y - center) - innerRadius);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float sigma = 6.0f;
                    float alpha = Mathf.Exp(-(dist * dist) / (2f * sigma * sigma));
                    if (dist > 18f) alpha = 0f;

                    s_gaussianShadowTex.SetPixel(x, y, new Color(0.02f, 0.02f, 0.03f, alpha * 0.85f));
                }
            }
            s_gaussianShadowTex.Apply();
            return s_gaussianShadowTex;
        }

        /// <summary>
        /// 为 VisualElement 应用真实的自适应 9-Slice 高斯软阴影衬底（Drop Shadow Underlay）
        /// </summary>
        public static void ApplyCardShadow(VisualElement card, float offsetY = 4f, float blurSpread = 8f, float opacity = 0.65f)
        {
            if (card == null) return;

            var shadow = card.Q<VisualElement>("__card_drop_shadow");
            if (shadow == null)
            {
                shadow = new VisualElement { name = "__card_drop_shadow" };
                shadow.style.position = Position.Absolute;
                shadow.pickingMode = PickingMode.Ignore;
                shadow.style.backgroundImage = new StyleBackground(GetGaussianShadowTexture());
                shadow.style.unitySliceTop = 26;
                shadow.style.unitySliceBottom = 26;
                shadow.style.unitySliceLeft = 26;
                shadow.style.unitySliceRight = 26;
                card.Insert(0, shadow);
            }

            shadow.style.left = -blurSpread;
            shadow.style.right = -blurSpread;
            shadow.style.top = -blurSpread + offsetY;
            shadow.style.bottom = -blurSpread - offsetY;
            shadow.style.opacity = opacity;
        }

        /// <summary>
        /// 统一物理微交互注册（悬浮浮起 2px、边框亮金发光、按压下沉 1.5px、回弹、爽脆手感）
        /// </summary>
        public static void ApplyPhysicalMicroInteraction(
            VisualElement el,
            System.Action onClick = null,
            bool enableFloat = true,
            Color? normalBg = null,
            Color? hoverBg = null,
            Color? activeBg = null,
            bool isBrass = false)
        {
            if (el == null) return;

            Color baseBg = normalBg ?? new Color(0.16f, 0.17f, 0.21f, 0.85f);
            Color hBg = hoverBg ?? (isBrass ? new Color(0.28f, 0.24f, 0.16f, 1f) : new Color(0.21f, 0.22f, 0.27f, 1f));
            Color aBg = activeBg ?? (isBrass ? new Color(0.22f, 0.18f, 0.12f, 1f) : new Color(0.12f, 0.13f, 0.16f, 1f));

            bool isHovered = false;

            el.RegisterCallback<PointerEnterEvent>(_ =>
            {
                isHovered = true;
                if (enableFloat)
                {
                    el.style.top = -2f;
                }
                el.style.backgroundColor = hBg;
                el.style.borderTopColor = isBrass ? new Color(1f, 0.88f, 0.55f, 1f) : new Color(0.65f, 0.66f, 0.78f, 0.95f);
                el.style.borderLeftColor = el.style.borderTopColor;
            });

            el.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                isHovered = false;
                if (enableFloat)
                {
                    el.style.top = 0f;
                }
                el.style.backgroundColor = baseBg;
                ApplyIndustrialBevel(el, false, isBrass);
            });

            el.RegisterCallback<PointerDownEvent>(_ =>
            {
                if (enableFloat)
                {
                    el.style.top = 1.5f;
                }
                el.style.backgroundColor = aBg;
                ApplyIndustrialBevel(el, true, isBrass);
            });

            el.RegisterCallback<PointerUpEvent>(_ =>
            {
                if (enableFloat)
                {
                    el.style.top = isHovered ? -2f : 0f;
                }
                el.style.backgroundColor = isHovered ? hBg : baseBg;
                ApplyIndustrialBevel(el, false, isBrass);
            });

            if (onClick != null)
            {
                if (el is Button btn)
                {
                    btn.clicked += onClick;
                    btn.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
                }
                else
                {
                    el.RegisterCallback<ClickEvent>(evt =>
                    {
                        evt.StopPropagation();
                        onClick.Invoke();
                    });
                }
            }
        }

        /// <summary>
        /// 创建纯图形化 Spotify 风格随机播放按纽（🔀 纯矢量双折线，激活时亮金并带有底部微小圆点信标，绝无文字）
        /// </summary>
        public static VisualElement CreateSpotifyShuffleControl(bool isActive, System.Action onClick = null)
        {
            var btn = new Button();
            btn.style.width = 32; btn.style.height = 32;
            btn.style.backgroundColor = Color.clear;
            btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
            btn.style.alignItems = Align.Center; btn.style.justifyContent = Justify.Center;
            btn.style.position = Position.Relative;
            btn.style.paddingLeft = 0; btn.style.paddingRight = 0;
            btn.style.paddingTop = 0; btn.style.paddingBottom = 0;

            Color iconColor = isActive ? new Color(0.96f, 0.82f, 0.45f, 1f) : new Color(0.65f, 0.65f, 0.70f, 0.8f);

            var icon = CreateShuffleIcon(iconColor);
            icon.pickingMode = PickingMode.Ignore;
            btn.Add(icon);

            // 激活状态时底部小圆点信标（彻底对标 Spotify）
            var dot = new VisualElement();
            dot.style.position = Position.Absolute;
            dot.style.bottom = 2;
            dot.style.width = 3.5f; dot.style.height = 3.5f;
            dot.style.borderTopLeftRadius = 2; dot.style.borderTopRightRadius = 2;
            dot.style.borderBottomLeftRadius = 2; dot.style.borderBottomRightRadius = 2;
            dot.style.backgroundColor = new Color(0.96f, 0.82f, 0.45f, 1f);
            dot.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
            dot.pickingMode = PickingMode.Ignore;
            btn.Add(dot);

            if (onClick != null)
            {
                ApplyPhysicalMicroInteraction(btn, onClick: onClick, enableFloat: false,
                    normalBg: Color.clear, hoverBg: new Color(1f, 1f, 1f, 0.08f), activeBg: new Color(1f, 1f, 1f, 0.15f));
            }
            return btn;
        }

        /// <summary>
        /// 创建纯图形化 Spotify 风格循环播放按纽（🔁 纯矢量双箭头，全部循环亮金，单曲循环带数字 1，绝无文字）
        /// </summary>
        public static VisualElement CreateSpotifyRepeatControl(int mode, System.Action onClick = null)
        {
            // mode: 0 = 单曲循环(RepeatOne), 1 = 顺序/列表循环(RepeatAll), 2 = 随机(此时非循环)
            // 对标 Spotify: RepeatOne 亮金+数字1+小点; RepeatAll 亮金+小点; 关闭 灰白无小点
            bool isRepeatOne = (mode == 0);
            bool isRepeatAll = (mode == 1);
            bool isActive = isRepeatOne || isRepeatAll;

            var btn = new Button();
            btn.style.width = 32; btn.style.height = 32;
            btn.style.backgroundColor = Color.clear;
            btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
            btn.style.alignItems = Align.Center; btn.style.justifyContent = Justify.Center;
            btn.style.position = Position.Relative;
            btn.style.paddingLeft = 0; btn.style.paddingRight = 0;
            btn.style.paddingTop = 0; btn.style.paddingBottom = 0;

            Color iconColor = isActive ? new Color(0.96f, 0.82f, 0.45f, 1f) : new Color(0.65f, 0.65f, 0.70f, 0.8f);

            VisualElement icon = isRepeatOne ? CreateSingleRepeatIcon(iconColor) : CreateSequentialIcon(iconColor);
            icon.pickingMode = PickingMode.Ignore;
            btn.Add(icon);

            // 底部小圆点信标
            var dot = new VisualElement();
            dot.style.position = Position.Absolute;
            dot.style.bottom = 2;
            dot.style.width = 3.5f; dot.style.height = 3.5f;
            dot.style.borderTopLeftRadius = 2; dot.style.borderTopRightRadius = 2;
            dot.style.borderBottomLeftRadius = 2; dot.style.borderBottomRightRadius = 2;
            dot.style.backgroundColor = new Color(0.96f, 0.82f, 0.45f, 1f);
            dot.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
            dot.pickingMode = PickingMode.Ignore;
            btn.Add(dot);

            if (onClick != null)
            {
                ApplyPhysicalMicroInteraction(btn, onClick: onClick, enableFloat: false,
                    normalBg: Color.clear, hoverBg: new Color(1f, 1f, 1f, 0.08f), activeBg: new Color(1f, 1f, 1f, 0.15f));
            }
            return btn;
        }

        /// <summary>
        /// 创建类似 Spotify 经典浮动大播放绿/金色胶囊按纽
        /// </summary>
        public static VisualElement CreateSpotifyPlayPill(float size = 36f, bool isPlaying = false)
        {
            var btn = new VisualElement();
            btn.style.width = size; btn.style.height = size;
            btn.style.borderTopLeftRadius = size * 0.5f; btn.style.borderTopRightRadius = size * 0.5f;
            btn.style.borderBottomLeftRadius = size * 0.5f; btn.style.borderBottomRightRadius = size * 0.5f;
            btn.style.backgroundColor = isPlaying ? new Color(0.12f, 0.72f, 0.38f, 1f) : new Color(0.85f, 0.68f, 0.28f, 1f); // 亮绿或暖金
            btn.style.alignItems = Align.Center; btn.style.justifyContent = Justify.Center;
            btn.style.borderTopWidth = 1; btn.style.borderLeftWidth = 1;
            btn.style.borderBottomWidth = 2; btn.style.borderRightWidth = 2;
            btn.style.borderTopColor = new Color(1f, 1f, 1f, 0.5f);
            btn.style.borderLeftColor = new Color(1f, 1f, 1f, 0.5f);
            btn.style.borderBottomColor = new Color(0.08f, 0.08f, 0.10f, 0.8f);
            btn.style.borderRightColor = new Color(0.08f, 0.08f, 0.10f, 0.8f);
            btn.pickingMode = PickingMode.Ignore;

            if (isPlaying)
            {
                btn.Add(CreatePauseIcon(2.5f, size * 0.38f, 3.5f, Color.black));
            }
            else
            {
                btn.Add(CreatePlayIcon(size * 0.38f, Color.black));
            }
            return btn;
        }

        /// <summary>
        /// 创建右侧 Now Playing 视窗超大黑胶唱盘/磁带转盘（16-bit 像素发光唱片）
        /// </summary>
        public static VisualElement CreateVinylDiscLarge(float size = 180f)
        {
            var disc = new VisualElement();
            disc.style.width = size; disc.style.height = size;
            disc.style.borderTopLeftRadius = size * 0.5f; disc.style.borderTopRightRadius = size * 0.5f;
            disc.style.borderBottomLeftRadius = size * 0.5f; disc.style.borderBottomRightRadius = size * 0.5f;
            disc.style.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            disc.style.borderTopWidth = 2.5f; disc.style.borderBottomWidth = 2.5f;
            disc.style.borderLeftWidth = 2.5f; disc.style.borderRightWidth = 2.5f;
            disc.style.borderTopColor = new Color(0.83f, 0.66f, 0.26f, 0.85f); // 金圈外框
            disc.style.borderBottomColor = new Color(0.83f, 0.66f, 0.26f, 0.85f);
            disc.style.borderLeftColor = new Color(0.83f, 0.66f, 0.26f, 0.85f);
            disc.style.borderRightColor = new Color(0.83f, 0.66f, 0.26f, 0.85f);
            disc.style.alignItems = Align.Center; disc.style.justifyContent = Justify.Center;
            disc.pickingMode = PickingMode.Ignore;

            // 黑胶同心环槽 1
            var ring1 = new VisualElement();
            ring1.style.width = size * 0.82f; ring1.style.height = size * 0.82f;
            ring1.style.borderTopLeftRadius = size * 0.41f; ring1.style.borderTopRightRadius = size * 0.41f;
            ring1.style.borderBottomLeftRadius = size * 0.41f; ring1.style.borderBottomRightRadius = size * 0.41f;
            ring1.style.borderTopWidth = 1f; ring1.style.borderBottomWidth = 1f;
            ring1.style.borderLeftWidth = 1f; ring1.style.borderRightWidth = 1f;
            ring1.style.borderTopColor = new Color(0.22f, 0.22f, 0.26f, 0.6f);
            ring1.style.borderBottomColor = new Color(0.22f, 0.22f, 0.26f, 0.6f);
            ring1.style.borderLeftColor = new Color(0.22f, 0.22f, 0.26f, 0.6f);
            ring1.style.borderRightColor = new Color(0.22f, 0.22f, 0.26f, 0.6f);
            ring1.style.alignItems = Align.Center; ring1.style.justifyContent = Justify.Center;
            ring1.pickingMode = PickingMode.Ignore;
            disc.Add(ring1);

            // 黑胶同心环槽 2
            var ring2 = new VisualElement();
            ring2.style.width = size * 0.64f; ring2.style.height = size * 0.64f;
            ring2.style.borderTopLeftRadius = size * 0.32f; ring2.style.borderTopRightRadius = size * 0.32f;
            ring2.style.borderBottomLeftRadius = size * 0.32f; ring2.style.borderBottomRightRadius = size * 0.32f;
            ring2.style.borderTopWidth = 1f; ring2.style.borderBottomWidth = 1f;
            ring2.style.borderLeftWidth = 1f; ring2.style.borderRightWidth = 1f;
            ring2.style.borderTopColor = new Color(0.28f, 0.28f, 0.32f, 0.6f);
            ring2.style.borderBottomColor = new Color(0.28f, 0.28f, 0.32f, 0.6f);
            ring2.style.borderLeftColor = new Color(0.28f, 0.28f, 0.32f, 0.6f);
            ring2.style.borderRightColor = new Color(0.28f, 0.28f, 0.32f, 0.6f);
            ring2.style.alignItems = Align.Center; ring2.style.justifyContent = Justify.Center;
            ring2.pickingMode = PickingMode.Ignore;
            ring1.Add(ring2);

            // 中心黄铜轴心与唱片纸标
            var centerLabel = new VisualElement();
            centerLabel.style.width = size * 0.38f; centerLabel.style.height = size * 0.38f;
            centerLabel.style.borderTopLeftRadius = size * 0.19f; centerLabel.style.borderTopRightRadius = size * 0.19f;
            centerLabel.style.borderBottomLeftRadius = size * 0.19f; centerLabel.style.borderBottomRightRadius = size * 0.19f;
            centerLabel.style.backgroundColor = new Color(0.83f, 0.66f, 0.26f, 0.95f); // 亮金色圆标
            centerLabel.style.alignItems = Align.Center; centerLabel.style.justifyContent = Justify.Center;
            centerLabel.pickingMode = PickingMode.Ignore;
            ring2.Add(centerLabel);

            var spindleHole = new VisualElement();
            spindleHole.style.width = 10; spindleHole.style.height = 10;
            spindleHole.style.borderTopLeftRadius = 5; spindleHole.style.borderTopRightRadius = 5;
            spindleHole.style.borderBottomLeftRadius = 5; spindleHole.style.borderBottomRightRadius = 5;
            spindleHole.style.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);
            spindleHole.pickingMode = PickingMode.Ignore;
            centerLabel.Add(spindleHole);

            return disc;
        }

        /// <summary>
        /// 创建黑胶外圈 Folia 风格自适应流光光晕（Halo Aura Ring）
        /// </summary>
        public static VisualElement CreateAuraHaloRing(float size, Color glowColor)
        {
            var halo = new VisualElement();
            halo.style.position = Position.Absolute;
            halo.style.width = size; halo.style.height = size;
            halo.style.borderTopLeftRadius = size * 0.5f; halo.style.borderTopRightRadius = size * 0.5f;
            halo.style.borderBottomLeftRadius = size * 0.5f; halo.style.borderBottomRightRadius = size * 0.5f;
            halo.style.borderTopWidth = 8f; halo.style.borderBottomWidth = 8f;
            halo.style.borderLeftWidth = 8f; halo.style.borderRightWidth = 8f;
            halo.style.borderTopColor = glowColor; halo.style.borderBottomColor = glowColor;
            halo.style.borderLeftColor = glowColor; halo.style.borderRightColor = glowColor;
            halo.pickingMode = PickingMode.Ignore;

            // 内层柔光收拢环
            var innerGlow = new VisualElement();
            innerGlow.style.position = Position.Absolute;
            innerGlow.style.top = 4; innerGlow.style.left = 4; innerGlow.style.right = 4; innerGlow.style.bottom = 4;
            float innerSize = size - 8f;
            innerGlow.style.borderTopLeftRadius = innerSize * 0.5f; innerGlow.style.borderTopRightRadius = innerSize * 0.5f;
            innerGlow.style.borderBottomLeftRadius = innerSize * 0.5f; innerGlow.style.borderBottomRightRadius = innerSize * 0.5f;
            innerGlow.style.borderTopWidth = 4f; innerGlow.style.borderBottomWidth = 4f;
            innerGlow.style.borderLeftWidth = 4f; innerGlow.style.borderRightWidth = 4f;
            Color softColor = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * 0.45f);
            innerGlow.style.borderTopColor = softColor; innerGlow.style.borderBottomColor = softColor;
            innerGlow.style.borderLeftColor = softColor; innerGlow.style.borderRightColor = softColor;
            innerGlow.pickingMode = PickingMode.Ignore;
            halo.Add(innerGlow);

            return halo;
        }

        /// <summary>
        /// 双联先民动圈模拟电平表（VU Meter 控制句柄）
        /// </summary>
        public class VuMeterHandle
        {
            public VisualElement root;
            public VisualElement leftNeedle;
            public VisualElement rightNeedle;
            public float leftAngle = -26f;
            public float rightAngle = -26f;

            public void UpdateLevels(float lEnergy, float rEnergy, float dt)
            {
                // -26 deg ~ +26 deg 动圈阻尼摆动
                float targetL = Mathf.Lerp(-24f, 24f, Mathf.Clamp01(lEnergy));
                float targetR = Mathf.Lerp(-24f, 24f, Mathf.Clamp01(rEnergy));
                leftAngle = Mathf.Lerp(leftAngle, targetL, dt * 16f);
                rightAngle = Mathf.Lerp(rightAngle, targetR, dt * 16f);
                if (leftNeedle != null) leftNeedle.style.rotate = new Rotate(Angle.Degrees(leftAngle));
                if (rightNeedle != null) rightNeedle.style.rotate = new Rotate(Angle.Degrees(rightAngle));
            }
        }

        /// <summary>
        /// 纯代码构建先民列车双联真空管动圈电平表（VU Meter）
        /// </summary>
        public static VuMeterHandle CreateDualVuMeter(float width = 200f, float height = 44f)
        {
            var h = new VuMeterHandle();
            var meterBox = new VisualElement();
            meterBox.style.width = width;
            meterBox.style.height = height;
            meterBox.style.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 0.95f);
            meterBox.style.borderTopLeftRadius = 5; meterBox.style.borderTopRightRadius = 5;
            meterBox.style.borderBottomLeftRadius = 5; meterBox.style.borderBottomRightRadius = 5;
            meterBox.style.borderTopWidth = 1; meterBox.style.borderBottomWidth = 1;
            meterBox.style.borderLeftWidth = 1; meterBox.style.borderRightWidth = 1;
            meterBox.style.borderTopColor = new Color(0.35f, 0.32f, 0.26f, 0.8f);
            meterBox.style.borderBottomColor = meterBox.style.borderTopColor;
            meterBox.style.borderLeftColor = meterBox.style.borderTopColor;
            meterBox.style.borderRightColor = meterBox.style.borderTopColor;
            meterBox.style.flexDirection = FlexDirection.Row;
            meterBox.style.justifyContent = Justify.SpaceBetween;
            meterBox.style.alignItems = Align.Center;
            meterBox.style.paddingLeft = 6; meterBox.style.paddingRight = 6;
            meterBox.style.paddingTop = 4; meterBox.style.paddingBottom = 4;
            ApplyCardShadow(meterBox, 2f, 4f, 0.4f);
            h.root = meterBox;

            // 左声道表头
            var leftDial = BuildSingleVuDial("CH-L", out h.leftNeedle);
            meterBox.Add(leftDial);

            // 中间黄铜分隔铭牌
            var divider = new VisualElement();
            divider.style.width = 1; divider.style.height = height - 12;
            divider.style.backgroundColor = new Color(0.35f, 0.32f, 0.26f, 0.5f);
            meterBox.Add(divider);

            // 右声道表头
            var rightDial = BuildSingleVuDial("CH-R", out h.rightNeedle);
            meterBox.Add(rightDial);

            return h;
        }

        private static VisualElement BuildSingleVuDial(string label, out VisualElement needle)
        {
            var dial = new VisualElement();
            dial.style.flexGrow = 1; dial.style.height = 36;
            dial.style.backgroundColor = new Color(0.18f, 0.18f, 0.14f, 0.85f); // 暖黄复古表盘纸
            dial.style.borderTopLeftRadius = 3; dial.style.borderTopRightRadius = 3;
            dial.style.borderBottomLeftRadius = 3; dial.style.borderBottomRightRadius = 3;
            dial.style.overflow = Overflow.Hidden;
            dial.style.position = Position.Relative;
            dial.style.marginLeft = 2; dial.style.marginRight = 2;

            // 刻度线微缩指示
            var tagLbl = new Label(label);
            tagLbl.style.fontSize = 8;
            tagLbl.style.color = new Color(0.35f, 0.32f, 0.25f, 0.9f);
            tagLbl.style.position = Position.Absolute;
            tagLbl.style.top = 2; tagLbl.style.left = 4;
            dial.Add(tagLbl);

            var dbLbl = new Label("VU · -20 0 +3");
            dbLbl.style.fontSize = 7;
            dbLbl.style.color = new Color(0.40f, 0.38f, 0.30f, 0.85f);
            dbLbl.style.position = Position.Absolute;
            dbLbl.style.top = 2; dbLbl.style.right = 4;
            dial.Add(dbLbl);

            // 红区微标（+3dB 警示条）
            var redZone = new VisualElement();
            redZone.style.position = Position.Absolute;
            redZone.style.top = 10; redZone.style.right = 6;
            redZone.style.width = 16; redZone.style.height = 2;
            redZone.style.backgroundColor = new Color(0.85f, 0.25f, 0.20f, 0.85f);
            dial.Add(redZone);

            // 动圈指针旋转支点（位于底部中心）
            var pivot = new VisualElement();
            pivot.style.position = Position.Absolute;
            pivot.style.bottom = -4; pivot.style.left = Length.Percent(50);
            pivot.style.width = 6; pivot.style.height = 6;
            pivot.style.marginLeft = -3;
            pivot.style.borderTopLeftRadius = 3; pivot.style.borderTopRightRadius = 3;
            pivot.style.borderBottomLeftRadius = 3; pivot.style.borderBottomRightRadius = 3;
            pivot.style.backgroundColor = new Color(0.12f, 0.10f, 0.08f, 1f);
            dial.Add(pivot);

            // 黑色动圈指针
            var nd = new VisualElement();
            nd.style.position = Position.Absolute;
            nd.style.bottom = 0; nd.style.left = Length.Percent(50);
            nd.style.width = 1.5f; nd.style.height = 24;
            nd.style.marginLeft = -0.75f;
            nd.style.backgroundColor = new Color(0.82f, 0.24f, 0.20f, 1f); // 赤红指针
            nd.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
            nd.style.rotate = new Rotate(Angle.Degrees(-24f));
            nd.pickingMode = PickingMode.Ignore;
            dial.Add(nd);
            needle = nd;

            return dial;
        }

        /// <summary>
        /// 全局桌面/HUD 悬浮迷你随身听胶囊（Folia 风格 Floating Capsule）
        /// </summary>
        public class FloatingCapsuleHandle
        {
            public VisualElement root;
            public VisualElement miniDisc;
            public Label trackTitle;
            public VisualElement playBtn;
            public float rotation = 0f;

            public void UpdateRotation(bool isPlaying, float dt)
            {
                if (isPlaying && miniDisc != null)
                {
                    rotation = (rotation + dt * 160f) % 360f;
                    miniDisc.style.rotate = new Rotate(Angle.Degrees(rotation));
                }
            }
        }

        public static FloatingCapsuleHandle CreateFloatingCapsulePlayer(
            string currentTitle,
            bool isPlaying,
            System.Action onTogglePlay,
            System.Action onNext,
            System.Action onOpenFull)
        {
            var h = new FloatingCapsuleHandle();
            var capsule = new VisualElement();
            capsule.style.height = 36;
            capsule.style.backgroundColor = new Color(0.10f, 0.11f, 0.14f, 0.96f); // 深黑亚克力
            capsule.style.borderTopLeftRadius = 18; capsule.style.borderTopRightRadius = 18;
            capsule.style.borderBottomLeftRadius = 18; capsule.style.borderBottomRightRadius = 18;
            capsule.style.borderTopWidth = 1; capsule.style.borderBottomWidth = 1;
            capsule.style.borderLeftWidth = 1; capsule.style.borderRightWidth = 1;
            capsule.style.borderTopColor = new Color(0.83f, 0.66f, 0.26f, 0.6f); // 黄铜金边
            capsule.style.borderBottomColor = capsule.style.borderTopColor;
            capsule.style.borderLeftColor = capsule.style.borderTopColor;
            capsule.style.borderRightColor = capsule.style.borderTopColor;
            capsule.style.flexDirection = FlexDirection.Row;
            capsule.style.alignItems = Align.Center;
            capsule.style.paddingLeft = 6; capsule.style.paddingRight = 10;
            ApplyCardShadow(capsule, 3f, 8f, 0.5f);
            h.root = capsule;

            // 1. 微型黑胶唱盘
            var disc = CreateVinylDiscLarge(24);
            disc.style.marginRight = 8;
            capsule.Add(disc);
            h.miniDisc = disc;

            // 2. 标题文本（点击打开全屏 App）
            var titleBox = new VisualElement();
            titleBox.style.maxWidth = 110;
            titleBox.style.overflow = Overflow.Hidden;
            titleBox.style.marginRight = 8;
            titleBox.pickingMode = PickingMode.Position;
            capsule.Add(titleBox);

            var titleLbl = new Label(string.IsNullOrEmpty(currentTitle) ? "随身听待机中" : currentTitle);
            titleLbl.style.fontSize = 11;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.color = new Color(0.96f, 0.94f, 0.88f, 1f);
            titleLbl.style.whiteSpace = WhiteSpace.NoWrap;
            titleLbl.style.overflow = Overflow.Hidden;
            titleLbl.style.textOverflow = TextOverflow.Ellipsis;
            titleBox.Add(titleLbl);
            h.trackTitle = titleLbl;

            titleBox.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                onOpenFull?.Invoke();
            });

            // 3. 迷你播放/暂停按钮
            var pBtn = new VisualElement();
            pBtn.style.width = 24; pBtn.style.height = 24;
            pBtn.style.borderTopLeftRadius = 12; pBtn.style.borderTopRightRadius = 12;
            pBtn.style.borderBottomLeftRadius = 12; pBtn.style.borderBottomRightRadius = 12;
            pBtn.style.backgroundColor = new Color(0.83f, 0.66f, 0.26f, 1f);
            pBtn.style.alignItems = Align.Center; pBtn.style.justifyContent = Justify.Center;
            pBtn.style.marginRight = 6;
            pBtn.Add(isPlaying ? CreatePauseIcon(2f, 9f, 2.5f, Color.black) : CreatePlayIcon(9f, Color.black));
            ApplyPhysicalMicroInteraction(pBtn, onClick: onTogglePlay, enableFloat: true, isBrass: true);
            capsule.Add(pBtn);
            h.playBtn = pBtn;

            // 4. 迷你切下一首
            var nBtn = new VisualElement();
            nBtn.style.width = 22; nBtn.style.height = 22;
            nBtn.style.borderTopLeftRadius = 11; nBtn.style.borderTopRightRadius = 11;
            nBtn.style.borderBottomLeftRadius = 11; nBtn.style.borderBottomRightRadius = 11;
            nBtn.style.backgroundColor = new Color(0.18f, 0.19f, 0.23f, 1f);
            nBtn.style.alignItems = Align.Center; nBtn.style.justifyContent = Justify.Center;
            nBtn.Add(CreateTrackNavIcon(true, 8f, new Color(0.85f, 0.82f, 0.75f, 1f)));
            ApplyPhysicalMicroInteraction(nBtn, onClick: onNext, enableFloat: true, isBrass: false);
            capsule.Add(nBtn);

            return h;
        }

        // ==========================================================================================
        // FOLIA 级液态玻璃（Liquid Glass）与高级工业卡片微质感系统
        // ==========================================================================================

        /// <summary>
        /// 为卡片赋予 Folia 级“液态玻璃”质感（次表面半透、0.5px晶体高光内边缘、暗部折射厚度、物理微浮凸与柔和高斯阴影）
        /// </summary>
        public static void ApplyLiquidGlassCard(
            VisualElement card,
            bool isPlaying = false,
            bool isLocked = false,
            System.Action onClick = null,
            System.Action onHover = null)
        {
            if (card == null) return;

            card.pickingMode = PickingMode.Position;
            card.style.position = Position.Relative;
            card.style.overflow = Overflow.Visible;

            // 1. 底衬 9-Slice 自适应高斯软阴影
            ApplyCardShadow(card, offsetY: 3.5f, blurSpread: 9f, opacity: isPlaying ? 0.65f : 0.45f);

            // 2. 液态玻璃基底调色
            Color glassBaseBg = isPlaying 
                ? new Color(0.22f, 0.18f, 0.13f, 0.88f) // 播放中：深沉暖琥珀黑
                : (isLocked ? new Color(0.12f, 0.13f, 0.15f, 0.72f) : new Color(0.14f, 0.15f, 0.18f, 0.82f)); // 常规：深矿石液态半透

            Color glassHoverBg = isPlaying
                ? new Color(0.27f, 0.22f, 0.15f, 0.95f)
                : (isLocked ? new Color(0.16f, 0.17f, 0.20f, 0.88f) : new Color(0.19f, 0.20f, 0.25f, 0.92f));

            Color glassActiveBg = isPlaying
                ? new Color(0.18f, 0.14f, 0.10f, 0.98f)
                : new Color(0.10f, 0.11f, 0.13f, 0.95f);

            card.style.backgroundColor = glassBaseBg;

            // 3. 晶体切角与折射双层内边框（Liquid Glass Specular Light Rim）
            // 顶部与左侧：受光面晶体高光 line
            Color specTopLeft = isPlaying 
                ? new Color(0.96f, 0.82f, 0.45f, 0.85f) 
                : (isLocked ? new Color(0.40f, 0.42f, 0.48f, 0.35f) : new Color(1f, 1f, 1f, 0.18f));
            // 底部与右侧：背光面深折射投影 line
            Color specBotRight = isPlaying 
                ? new Color(0.35f, 0.25f, 0.10f, 0.90f) 
                : new Color(0.04f, 0.05f, 0.07f, 0.75f);

            card.style.borderTopWidth = 1f;
            card.style.borderLeftWidth = 1f;
            card.style.borderBottomWidth = 1.2f;
            card.style.borderRightWidth = 1.2f;

            card.style.borderTopColor = specTopLeft;
            card.style.borderLeftColor = specTopLeft;
            card.style.borderBottomColor = specBotRight;
            card.style.borderRightColor = specBotRight;

            // 4. 物理微交互状态响应
            bool isHovered = false;

            card.RegisterCallback<PointerEnterEvent>(_ =>
            {
                isHovered = true;
                card.style.top = -2.5f; // 优雅浮起
                card.style.backgroundColor = glassHoverBg;

                // 悬浮时高光增强
                card.style.borderTopColor = isPlaying ? new Color(1f, 0.88f, 0.55f, 1f) : new Color(1f, 1f, 1f, 0.32f);
                card.style.borderLeftColor = card.style.borderTopColor;

                // 软阴影外扩加深
                ApplyCardShadow(card, offsetY: 5.5f, blurSpread: 13f, opacity: isPlaying ? 0.80f : 0.60f);

                onHover?.Invoke();
            });

            card.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                isHovered = false;
                card.style.top = 0f;
                card.style.backgroundColor = glassBaseBg;

                card.style.borderTopColor = specTopLeft;
                card.style.borderLeftColor = specTopLeft;
                card.style.borderBottomColor = specBotRight;
                card.style.borderRightColor = specBotRight;

                ApplyCardShadow(card, offsetY: 3.5f, blurSpread: 9f, opacity: isPlaying ? 0.65f : 0.45f);
            });

            card.RegisterCallback<PointerDownEvent>(_ =>
            {
                card.style.top = 1.2f; // 实体按压微陷
                card.style.backgroundColor = glassActiveBg;
                ApplyCardShadow(card, offsetY: 1.5f, blurSpread: 5f, opacity: 0.40f);
            });

            card.RegisterCallback<PointerUpEvent>(_ =>
            {
                card.style.top = isHovered ? -2.5f : 0f;
                card.style.backgroundColor = isHovered ? glassHoverBg : glassBaseBg;
                ApplyCardShadow(card, offsetY: isHovered ? 5.5f : 3.5f, blurSpread: isHovered ? 13f : 9f, opacity: isHovered ? 0.70f : 0.50f);
            });

            if (onClick != null)
            {
                card.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    onClick.Invoke();
                });
            }
        }

        /// <summary>
        /// 创建工务级精致机械挂锁徽章（消灭粗糙字符，深暗金拉丝锁身 + 钢化圆弧锁梁 + 细钥匙孔）
        /// </summary>
        public static VisualElement CreateIndustrialLockBadge(bool mini = false)
        {
            float w = mini ? 16f : 20f;
            float h = mini ? 18f : 22f;

            var container = new VisualElement();
            container.style.width = w;
            container.style.height = h;
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.FlexEnd;
            container.pickingMode = PickingMode.Ignore;

            // 1. 顶部拱形硬化钢锁梁（U形回路）
            float bowW = mini ? 10f : 12f;
            float bowH = mini ? 9f : 11f;
            var bow = new VisualElement();
            bow.style.width = bowW;
            bow.style.height = bowH;
            bow.style.position = Position.Absolute;
            bow.style.top = 0;
            bow.style.borderTopWidth = mini ? 1.5f : 2f;
            bow.style.borderLeftWidth = mini ? 1.5f : 2f;
            bow.style.borderRightWidth = mini ? 1.5f : 2f;
            bow.style.borderBottomWidth = 0;
            bow.style.borderTopLeftRadius = bowW * 0.5f;
            bow.style.borderTopRightRadius = bowW * 0.5f;
            bow.style.borderTopColor = new Color(0.85f, 0.82f, 0.78f, 0.95f); // 镀铬精钢光泽
            bow.style.borderLeftColor = new Color(0.65f, 0.62f, 0.58f, 0.95f);
            bow.style.borderRightColor = new Color(0.45f, 0.42f, 0.38f, 0.95f);
            bow.pickingMode = PickingMode.Ignore;
            container.Add(bow);

            // 2. 梯形重装黄铜锁身
            float bodyW = mini ? 14f : 17f;
            float bodyH = mini ? 11f : 13f;
            var body = new VisualElement();
            body.style.width = bodyW;
            body.style.height = bodyH;
            body.style.backgroundColor = new Color(0.78f, 0.62f, 0.28f, 1f); // 工务黄铜
            body.style.borderTopLeftRadius = 2.5f;
            body.style.borderTopRightRadius = 2.5f;
            body.style.borderBottomLeftRadius = 2.5f;
            body.style.borderBottomRightRadius = 2.5f;
            body.style.borderTopWidth = 1f;
            body.style.borderLeftWidth = 1f;
            body.style.borderBottomWidth = 1.2f;
            body.style.borderRightWidth = 1.2f;
            body.style.borderTopColor = new Color(0.96f, 0.85f, 0.50f, 1f);
            body.style.borderLeftColor = new Color(0.96f, 0.85f, 0.50f, 1f);
            body.style.borderBottomColor = new Color(0.28f, 0.22f, 0.10f, 1f);
            body.style.borderRightColor = new Color(0.28f, 0.22f, 0.10f, 1f);
            body.style.alignItems = Align.Center;
            body.style.justifyContent = Justify.Center;
            body.pickingMode = PickingMode.Ignore;
            container.Add(body);

            // 3. 锁身中央微型钥匙孔（深黑点 + 垂立线槽）
            var holeCircle = new VisualElement();
            holeCircle.style.width = mini ? 2.5f : 3.2f;
            holeCircle.style.height = mini ? 2.5f : 3.2f;
            holeCircle.style.borderTopLeftRadius = 1.6f;
            holeCircle.style.borderTopRightRadius = 1.6f;
            holeCircle.style.borderBottomLeftRadius = 1.6f;
            holeCircle.style.borderBottomRightRadius = 1.6f;
            holeCircle.style.backgroundColor = new Color(0.12f, 0.10f, 0.08f, 1f);
            holeCircle.pickingMode = PickingMode.Ignore;
            body.Add(holeCircle);

            var holeSlot = new VisualElement();
            holeSlot.style.width = mini ? 1.2f : 1.6f;
            holeSlot.style.height = mini ? 3f : 4f;
            holeSlot.style.backgroundColor = new Color(0.12f, 0.10f, 0.08f, 1f);
            holeSlot.style.marginTop = -0.5f;
            holeSlot.pickingMode = PickingMode.Ignore;
            body.Add(holeSlot);

            return container;
        }

        /// <summary>
        /// 创建工务调频极简透明胶囊徽章（如 [FM 94.2] / [CR-04] / [先民原声]）
        /// </summary>
        public static VisualElement CreateFrequencyBadge(string text, Color? tint = null, FontDefinition fontDef = default)
        {
            var badge = new VisualElement();
            badge.style.flexDirection = FlexDirection.Row;
            badge.style.alignItems = Align.Center;
            badge.style.paddingLeft = 5;
            badge.style.paddingRight = 5;
            badge.style.paddingTop = 1.5f;
            badge.style.paddingBottom = 1.5f;
            badge.style.borderTopLeftRadius = 3;
            badge.style.borderTopRightRadius = 3;
            badge.style.borderBottomLeftRadius = 3;
            badge.style.borderBottomRightRadius = 3;

            Color accent = tint ?? new Color(0.96f, 0.82f, 0.45f, 1f);
            badge.style.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.75f);
            badge.style.borderTopWidth = 0.8f;
            badge.style.borderLeftWidth = 0.8f;
            badge.style.borderBottomWidth = 0.8f;
            badge.style.borderRightWidth = 0.8f;
            badge.style.borderTopColor = new Color(accent.r, accent.g, accent.b, 0.45f);
            badge.style.borderLeftColor = badge.style.borderTopColor;
            badge.style.borderBottomColor = badge.style.borderTopColor;
            badge.style.borderRightColor = badge.style.borderTopColor;
            badge.pickingMode = PickingMode.Ignore;

            // 左侧微型状态指示绿点/金点
            var dot = new VisualElement();
            dot.style.width = 4;
            dot.style.height = 4;
            dot.style.borderTopLeftRadius = 2;
            dot.style.borderTopRightRadius = 2;
            dot.style.borderBottomLeftRadius = 2;
            dot.style.borderBottomRightRadius = 2;
            dot.style.backgroundColor = accent;
            dot.style.marginRight = 3.5f;
            dot.pickingMode = PickingMode.Ignore;
            badge.Add(dot);

            var lbl = new Label(text);
            lbl.style.fontSize = 8;
            lbl.style.color = new Color(0.90f, 0.88f, 0.82f, 0.95f);
            if (fontDef.font != null || fontDef.fontAsset != null) lbl.style.unityFontDefinition = fontDef;
            lbl.pickingMode = PickingMode.Ignore;
            badge.Add(lbl);

            return badge;
        }

        /// <summary>
        /// 创建卡片悬浮时的快速起播金边微药丸按纽（Hover Quick Play Pill）
        /// </summary>
        public static VisualElement CreateLiquidPlayPill(bool isPlaying, System.Action onClick)
        {
            var btn = new VisualElement();
            btn.style.width = 28;
            btn.style.height = 28;
            btn.style.borderTopLeftRadius = 14;
            btn.style.borderTopRightRadius = 14;
            btn.style.borderBottomLeftRadius = 14;
            btn.style.borderBottomRightRadius = 14;
            btn.style.alignItems = Align.Center;
            btn.style.justifyContent = Justify.Center;

            Color bg = isPlaying ? new Color(0.96f, 0.82f, 0.45f, 1f) : new Color(0.18f, 0.19f, 0.24f, 0.95f);
            Color iconColor = isPlaying ? new Color(0.12f, 0.10f, 0.08f, 1f) : Color.white;

            btn.style.backgroundColor = bg;
            btn.style.borderTopWidth = 1f;
            btn.style.borderLeftWidth = 1f;
            btn.style.borderBottomWidth = 1.2f;
            btn.style.borderRightWidth = 1.2f;
            btn.style.borderTopColor = new Color(1f, 1f, 1f, 0.35f);
            btn.style.borderLeftColor = btn.style.borderTopColor;
            btn.style.borderBottomColor = new Color(0f, 0f, 0f, 0.5f);
            btn.style.borderRightColor = btn.style.borderBottomColor;

            btn.Add(isPlaying ? CreatePauseIcon(2.2f, 10f, 3f, iconColor) : CreatePlayIcon(10f, iconColor));
            ApplyPhysicalMicroInteraction(btn, onClick: onClick, enableFloat: false, isBrass: isPlaying);

            return btn;
        }

        /// <summary>
        /// 定制深色工业内嵌输入框样式（彻底消除刺眼纯白背景与浏览器默认边框）
        /// </summary>
        public static void StyleIndustrialTextField(TextField field, string placeholder = "", float height = 34f)
        {
            if (field == null) return;
            field.style.height = height;
            field.style.backgroundColor = new Color(0.06f, 0.06f, 0.05f, 0.90f); // 深炭黑
            field.style.borderTopLeftRadius = 6; field.style.borderTopRightRadius = 6;
            field.style.borderBottomLeftRadius = 6; field.style.borderBottomRightRadius = 6;
            field.style.borderTopWidth = 1; field.style.borderBottomWidth = 1;
            field.style.borderLeftWidth = 1; field.style.borderRightWidth = 1;
            Color normalBorder = new Color(0.83f, 0.61f, 0.21f, 0.25f); // 柔和黄铜暗金
            Color focusBorder = new Color(0.90f, 0.66f, 0.24f, 0.95f);  // 焦点高亮琥珀金
            field.style.borderTopColor = normalBorder; field.style.borderBottomColor = normalBorder;
            field.style.borderLeftColor = normalBorder; field.style.borderRightColor = normalBorder;
            field.style.paddingLeft = 10; field.style.paddingRight = 10;
            field.style.paddingTop = 0; field.style.paddingBottom = 0;
            field.style.color = new Color(0.90f, 0.87f, 0.78f, 1f); // #E6DEC8

            // 内部原生输入文本节点（双重兜底：构造时与挂载到面板时）
            Action applyInner = () =>
            {
                var innerInput = field.Q(className: "unity-base-text-field__input") ?? field.Q("unity-text-input");
                if (innerInput != null)
                {
                    innerInput.style.backgroundColor = Color.clear;
                    innerInput.style.borderTopWidth = 0; innerInput.style.borderBottomWidth = 0;
                    innerInput.style.borderLeftWidth = 0; innerInput.style.borderRightWidth = 0;
                    innerInput.style.color = new Color(0.90f, 0.87f, 0.78f, 1f);
                    innerInput.style.paddingLeft = 0; innerInput.style.paddingRight = 0;
                }
            };
            applyInner();
            field.RegisterCallback<AttachToPanelEvent>(_ => applyInner());

            field.RegisterCallback<FocusInEvent>(e =>
            {
                field.style.borderTopColor = focusBorder; field.style.borderBottomColor = focusBorder;
                field.style.borderLeftColor = focusBorder; field.style.borderRightColor = focusBorder;
                field.style.backgroundColor = new Color(0.09f, 0.08f, 0.07f, 0.98f);
            });
            field.RegisterCallback<FocusOutEvent>(e =>
            {
                field.style.borderTopColor = normalBorder; field.style.borderBottomColor = normalBorder;
                field.style.borderLeftColor = normalBorder; field.style.borderRightColor = normalBorder;
                field.style.backgroundColor = new Color(0.06f, 0.06f, 0.05f, 0.90f);
            });
        }

        /// <summary>
        /// 创建带有微弱呼吸发光阴影的拟物 LED 状态指示灯
        /// </summary>
        public static VisualElement CreateLedIndicatorLamp(Color lampColor, bool isBreathing = true)
        {
            var lampBox = new VisualElement();
            lampBox.style.width = 10; lampBox.style.height = 10;
            lampBox.style.alignItems = Align.Center; lampBox.style.justifyContent = Justify.Center;
            lampBox.style.marginRight = 7;
            lampBox.pickingMode = PickingMode.Ignore;

            var outerGlow = new VisualElement();
            outerGlow.style.width = 10; outerGlow.style.height = 10;
            outerGlow.style.borderTopLeftRadius = 5; outerGlow.style.borderTopRightRadius = 5;
            outerGlow.style.borderBottomLeftRadius = 5; outerGlow.style.borderBottomRightRadius = 5;
            outerGlow.style.backgroundColor = new Color(lampColor.r, lampColor.g, lampColor.b, 0.28f);
            outerGlow.style.position = Position.Absolute;
            outerGlow.pickingMode = PickingMode.Ignore;
            lampBox.Add(outerGlow);

            var coreDot = new VisualElement();
            coreDot.style.width = 5; coreDot.style.height = 5;
            coreDot.style.borderTopLeftRadius = 2.5f; coreDot.style.borderTopRightRadius = 2.5f;
            coreDot.style.borderBottomLeftRadius = 2.5f; coreDot.style.borderBottomRightRadius = 2.5f;
            coreDot.style.backgroundColor = lampColor;
            coreDot.pickingMode = PickingMode.Ignore;
            lampBox.Add(coreDot);

            return lampBox;
        }

        /// <summary>
        /// 发现主页紧凑胶囊徽章（不占满全行，右侧留白）
        /// </summary>
        public static VisualElement CreateCompactPillBadge(string text, Color dotColor, StyleFontDefinition fontDef)
        {
            var pill = new VisualElement();
            pill.style.height = 16;
            pill.style.flexDirection = FlexDirection.Row;
            pill.style.alignItems = Align.Center;
            pill.style.paddingLeft = 7; pill.style.paddingRight = 7;
            pill.style.backgroundColor = new Color(1f, 1f, 1f, 0.05f);
            pill.style.borderTopLeftRadius = 8; pill.style.borderTopRightRadius = 8;
            pill.style.borderBottomLeftRadius = 8; pill.style.borderBottomRightRadius = 8;
            pill.style.borderTopWidth = 1; pill.style.borderBottomWidth = 1;
            pill.style.borderLeftWidth = 1; pill.style.borderRightWidth = 1;
            pill.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
            pill.style.borderBottomColor = pill.style.borderTopColor;
            pill.style.borderLeftColor = pill.style.borderTopColor;
            pill.style.borderRightColor = pill.style.borderTopColor;
            pill.style.alignSelf = Align.FlexStart;
            pill.pickingMode = PickingMode.Ignore;

            var d = new VisualElement();
            d.style.width = 3.5f; d.style.height = 3.5f;
            d.style.borderTopLeftRadius = 2; d.style.borderTopRightRadius = 2;
            d.style.borderBottomLeftRadius = 2; d.style.borderBottomRightRadius = 2;
            d.style.backgroundColor = dotColor;
            d.style.marginRight = 4;
            d.pickingMode = PickingMode.Ignore;
            pill.Add(d);

            var l = new Label(text);
            l.style.fontSize = 8.5f;
            l.style.color = new Color(0.85f, 0.82f, 0.75f, 0.9f);
            l.style.unityFontDefinition = fontDef;
            l.pickingMode = PickingMode.Ignore;
            pill.Add(l);

            return pill;
        }

        /// <summary>
        /// 触发物理轻微抖动动画反馈 (Shake)
        /// </summary>
        public static void TriggerShakeFeedback(VisualElement elem)
        {
            if (elem == null) return;
            elem.schedule.Execute(() => elem.style.translate = new Translate(Length.Pixels(-4), Length.Pixels(0))).StartingIn(0);
            elem.schedule.Execute(() => elem.style.translate = new Translate(Length.Pixels(4), Length.Pixels(0))).StartingIn(40);
            elem.schedule.Execute(() => elem.style.translate = new Translate(Length.Pixels(-2), Length.Pixels(0))).StartingIn(80);
            elem.schedule.Execute(() => elem.style.translate = new Translate(Length.Pixels(2), Length.Pixels(0))).StartingIn(120);
            elem.schedule.Execute(() => elem.style.translate = new Translate(Length.Pixels(0), Length.Pixels(0))).StartingIn(160);
        }
    }
}
