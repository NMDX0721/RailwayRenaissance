using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public enum CharDir { Down, Up, Left, Right }
    public enum HairStyle { Short, Bun, Ponytail, Scarf, Cap }

    /// <summary>角色外观数据：与《EXPLORE_ASSET_SPEC》角色清单一一对应（id 即贴图文件名里的角色段）。</summary>
    public sealed class CharLook
    {
        public string id;
        public Color skin = new Color(0.96f, 0.80f, 0.66f);
        public Color hair = new Color(0.16f, 0.12f, 0.10f);
        public Color top, bottom, accent, shoe = new Color(0.16f, 0.12f, 0.10f);
        public HairStyle style = HairStyle.Short;
        public bool apron, glasses, badge, tie, basket, wrench, lantern;
    }

    /// <summary>
    /// 角色绘制。32×48 像素规格（含头部）· 4 向 × 行走 4 帧 + 待机；左向由右向镜像。
    /// 真图（Resources/Art/Explore/Characters/char_{id}_{walk|idle}_{down|up|left|right}_{n}.png）存在则优先使用，
    /// 否则用下面的程序精绘占位，二者共用同一套帧与朝向逻辑，替换时零代码改动。
    /// </summary>
    public static class ExploreCharacters
    {
        private static readonly Dictionary<string, CharLook> Looks = new Dictionary<string, CharLook>();
        private static readonly Dictionary<string, Texture2D> SpriteCache = new Dictionary<string, Texture2D>();

        static ExploreCharacters()
        {
            Looks["player"] = new CharLook { id = "player", top = C(0.17f, 0.27f, 0.45f), bottom = C(0.13f, 0.18f, 0.30f), accent = C(0.85f, 0.70f, 0.30f), badge = true, lantern = false, style = HairStyle.Short };
            Looks["laochen"] = new CharLook { id = "laochen", top = C(0.38f, 0.40f, 0.44f), bottom = C(0.26f, 0.27f, 0.30f), accent = C(0.80f, 0.68f, 0.32f), hair = C(0.78f, 0.78f, 0.80f), style = HairStyle.Cap, skin = C(0.90f, 0.72f, 0.58f) };
            Looks["zhanggong"] = new CharLook { id = "zhanggong", top = C(0.27f, 0.42f, 0.62f), bottom = C(0.20f, 0.30f, 0.46f), accent = C(0.85f, 0.55f, 0.20f), wrench = true, style = HairStyle.Short };
            Looks["liayi"] = new CharLook { id = "liayi", top = C(0.78f, 0.44f, 0.50f), bottom = C(0.40f, 0.30f, 0.36f), accent = C(0.95f, 0.92f, 0.86f), apron = true, style = HairStyle.Bun, hair = C(0.30f, 0.22f, 0.20f) };
            Looks["zhaoshifu"] = new CharLook { id = "zhaoshifu", top = C(0.34f, 0.50f, 0.44f), bottom = C(0.22f, 0.30f, 0.28f), accent = C(0.75f, 0.30f, 0.28f), glasses = true, tie = true, style = HairStyle.Short, hair = C(0.40f, 0.38f, 0.38f) };
            Looks["xiaofang"] = new CharLook { id = "xiaofang", top = C(0.60f, 0.78f, 0.55f), bottom = C(0.32f, 0.42f, 0.30f), accent = C(0.95f, 0.78f, 0.35f), style = HairStyle.Ponytail, skin = C(0.98f, 0.82f, 0.70f) };
            Looks["chasao"] = new CharLook { id = "chasao", top = C(0.85f, 0.60f, 0.25f), bottom = C(0.45f, 0.30f, 0.20f), accent = C(0.95f, 0.88f, 0.70f), style = HairStyle.Scarf, basket = true, hair = C(0.25f, 0.18f, 0.14f) };
        }

        private static Color C(float r, float g, float b) => new Color(r, g, b);
        public static CharLook Get(string id) => Looks.TryGetValue(id, out var l) ? l : Looks["player"];

        // ================= 真图（可选）=================
        public static Texture2D TrySprite(string id, bool moving, CharDir dir, int frame, out bool mirror)
        {
            mirror = false;
            string state = moving ? "walk" : "idle";
            int f = moving ? frame : 0;
            string d = dir == CharDir.Down ? "down" : dir == CharDir.Up ? "up" : dir == CharDir.Left ? "left" : "right";
            var t = Load($"char_{id}_{state}_{d}_{f}");
            if (t == null && dir == CharDir.Left) { t = Load($"char_{id}_{state}_right_{f}"); mirror = t != null; }
            return t;
        }

        private static Texture2D Load(string name)
        {
            if (SpriteCache.TryGetValue(name, out var t)) return t;
            t = Resources.Load<Texture2D>("Art/Explore/Characters/" + name);
            if (t != null) { t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp; }
            SpriteCache[name] = t;
            return t;
        }

        // ================= 程序精绘 =================
        private static readonly Color Ink = new Color(0.10f, 0.07f, 0.06f, 0.55f);

        /// <summary>feet=脚底中心的屏幕坐标；u=每个精灵像素对应的屏幕像素。</summary>
        public static void Draw(Painter2D p, CharLook L, Vector2 feet, float u, CharDir dir, int frame, bool moving, float time)
        {
            bool side = dir == CharDir.Left || dir == CharDir.Right;
            float mx = dir == CharDir.Left ? -1f : 1f;
            float swing = moving ? (frame == 1 ? 1f : frame == 3 ? -1f : 0f) : 0f;
            float bob = moving ? ((frame & 1) == 1 ? -1.5f : 0f) : Mathf.Sin(time * 2.2f) * 0.5f;

            Vector2 P(float x, float y) => feet + new Vector2(x * mx * u, y * u);

            // 地面投影
            Ellipse(p, P(0, -1f), 11f * u, 3.6f * u, new Color(0f, 0f, 0f, 0.26f));

            // ---- 腿 ----
            if (!side)
            {
                Leg(p, P(-3.6f, 0), u, swing * 2.2f, L.bottom, L.shoe, 4.6f);
                Leg(p, P(3.6f, 0), u, -swing * 2.2f, L.bottom, L.shoe, 4.6f);
            }
            else
            {
                Vector2 back = P(-swing * 4f, 0), front = P(swing * 4f, 0);
                Leg(p, back, u, 0f, Darker(L.bottom, 0.8f), Darker(L.shoe, 0.9f), 5f, true);
                Leg(p, front, u, 0f, L.bottom, L.shoe, 5f, true);
            }

            // 背篮 / 扳手等背部道具
            if (L.basket && (dir == CharDir.Up || side)) Rect(p, P(side ? -8f : 0f, -26f + bob), side ? 7f * u : 14f * u, 12f * u, new Color(0.62f, 0.44f, 0.24f), true);

            // ---- 手臂（后排）----
            float armSwing = swing * 2.4f;
            if (!side)
            {
                Arm(p, P(-9.2f, -27f + bob), u, armSwing, L.top, L.skin);
            }
            else Arm(p, P(-armSwing * 1.2f, -27f + bob), u, 0f, Darker(L.top, 0.82f), L.skin, true);

            // ---- 躯干 ----
            float tw = side ? 5.2f : 7.6f;
            Poly(p, new[] { P(-tw, -28f + bob), P(tw, -28f + bob), P(tw + 0.6f, -12f + bob), P(-tw - 0.6f, -12f + bob) }, L.top, true);
            // 腰带
            Rect(p, P(0, -13f + bob), (tw * 2f + 1.2f) * u, 2.2f * u, Darker(L.bottom, 0.7f), false);
            if (dir == CharDir.Down)
            {
                // 领口 + 纽扣
                Poly(p, new[] { P(-2.8f, -28f + bob), P(2.8f, -28f + bob), P(0, -23.5f + bob) }, Lighter(L.skin, 0.0f), false);
                Disc(p, P(0, -20f + bob), 0.8f * u, L.accent);
                Disc(p, P(0, -16.5f + bob), 0.8f * u, L.accent);
                if (L.badge) Rect(p, P(3.6f, -23f + bob), 3.2f * u, 2.2f * u, new Color(0.95f, 0.92f, 0.85f), false);
                if (L.tie) Poly(p, new[] { P(-0.9f, -25f + bob), P(0.9f, -25f + bob), P(1.3f, -17f + bob), P(0f, -15.5f + bob), P(-1.3f, -17f + bob) }, L.accent, false);
                if (L.apron) Poly(p, new[] { P(-5.4f, -22f + bob), P(5.4f, -22f + bob), P(6.2f, -11f + bob), P(-6.2f, -11f + bob) }, L.accent, true);
                if (L.wrench) { Rect(p, P(5.5f, -15f + bob), 1.6f * u, 6f * u, new Color(0.74f, 0.76f, 0.80f), false); }
            }
            else if (dir == CharDir.Up && L.apron)
                Rect(p, P(0, -13f + bob), 4f * u, 7f * u, L.accent, false); // 围裙系带

            // ---- 手臂（前排）----
            if (!side) Arm(p, P(9.2f, -27f + bob), u, -armSwing, L.top, L.skin);
            else Arm(p, P(armSwing * 1.2f, -27f + bob), u, 0f, L.top, L.skin, true);

            // ---- 头 ----
            Vector2 hc = P(0, -36.5f + bob);
            Ellipse(p, hc, 8.2f * u, 8.6f * u, L.skin, true);
            if (side) Ellipse(p, P(-1.4f, -36f + bob), 1.7f * u, 2.2f * u, Darker(L.skin, 0.9f)); // 耳朵

            if (dir == CharDir.Down)
            {
                Ellipse(p, P(-3.2f, -36f + bob), 1.2f * u, 1.5f * u, new Color(0.12f, 0.09f, 0.08f));
                Ellipse(p, P(3.2f, -36f + bob), 1.2f * u, 1.5f * u, new Color(0.12f, 0.09f, 0.08f));
                Ellipse(p, P(-5.2f, -33.4f + bob), 1.6f * u, 1.0f * u, new Color(0.95f, 0.55f, 0.50f, 0.45f));
                Ellipse(p, P(5.2f, -33.4f + bob), 1.6f * u, 1.0f * u, new Color(0.95f, 0.55f, 0.50f, 0.45f));
                Rect(p, P(0, -32.2f + bob), 2.6f * u, 0.8f * u, new Color(0.55f, 0.28f, 0.25f), false);
                if (L.glasses) { Ring(p, P(-3.2f, -36f + bob), 2.4f * u, new Color(0.15f, 0.12f, 0.12f)); Ring(p, P(3.2f, -36f + bob), 2.4f * u, new Color(0.15f, 0.12f, 0.12f)); }
            }
            else if (side)
            {
                Ellipse(p, P(4.2f, -36f + bob), 1.1f * u, 1.5f * u, new Color(0.12f, 0.09f, 0.08f));
                Ellipse(p, P(7.8f, -34.4f + bob), 1.3f * u, 1.0f * u, Darker(L.skin, 0.92f)); // 鼻尖
                if (L.glasses) Ring(p, P(4.2f, -36f + bob), 2.4f * u, new Color(0.15f, 0.12f, 0.12f));
            }

            // ---- 头发 / 帽 ----
            DrawHair(p, L, P, hc, u, dir, bob);

            // 提灯
            if (L.lantern && dir != CharDir.Up)
            {
                Vector2 lc = P(side ? 9f : 12f, -14f + bob);
                Rect(p, lc, 5f * u, 6f * u, new Color(0.95f, 0.75f, 0.25f), true);
                Disc(p, lc + new Vector2(0, -3f * u), 7f * u, new Color(1f, 0.9f, 0.5f, 0.18f));
            }
        }

        private static void DrawHair(Painter2D p, CharLook L, System.Func<float, float, Vector2> P, Vector2 hc, float u, CharDir dir, float bob)
        {
            bool side = dir == CharDir.Left || dir == CharDir.Right;
            Color h = L.hair;
            switch (L.style)
            {
                case HairStyle.Cap:
                    Ellipse(p, P(0, -41f + bob), 8.8f * u, 5.2f * u, L.top, true);
                    if (dir == CharDir.Down) Rect(p, P(0, -38.2f + bob), 14f * u, 2f * u, Darker(L.top, 0.75f), false);
                    else if (side) Rect(p, P(7.5f, -38.2f + bob), 6f * u, 1.8f * u, Darker(L.top, 0.75f), false);
                    Disc(p, P(0, -43f + bob), 1.2f * u, L.accent);
                    break;
                case HairStyle.Scarf:
                    Ellipse(p, P(0, -41f + bob), 9f * u, 5.6f * u, L.accent, true);
                    Poly(p, new[] { P(-8.8f, -41f + bob), P(-5f, -37.5f + bob), P(-6f, -33f + bob), P(-9.4f, -36f + bob) }, L.top, false);
                    if (dir == CharDir.Up) Ellipse(p, hc, 8.4f * u, 8.8f * u, L.accent, true);
                    break;
                default:
                    if (dir == CharDir.Up) Ellipse(p, hc, 8.6f * u, 8.9f * u, h, true);
                    else
                    {
                        Ellipse(p, P(0, -41.4f + bob), 8.8f * u, 5.4f * u, h, true);
                        if (dir == CharDir.Down)
                        {
                            Poly(p, new[] { P(-8.4f, -39f + bob), P(-3f, -37f + bob), P(0f, -38.6f + bob), P(4f, -37f + bob), P(8.4f, -39f + bob), P(8.4f, -42f + bob), P(-8.4f, -42f + bob) }, h, false);
                        }
                        else Rect(p, P(-3f, -37.2f + bob), 9f * u, 4.6f * u, h, false);
                    }
                    if (L.style == HairStyle.Bun) Disc(p, P(side ? -4f : 0f, -47f + bob), 3.6f * u, h, true);
                    if (L.style == HairStyle.Ponytail)
                    {
                        if (dir == CharDir.Up) Poly(p, new[] { P(-1.6f, -38f + bob), P(1.6f, -38f + bob), P(2.4f, -26f + bob), P(0f, -23f + bob), P(-2.4f, -26f + bob) }, h, true);
                        else if (side) Poly(p, new[] { P(-6f, -40f + bob), P(-9f, -34f + bob), P(-10.5f, -28f + bob), P(-7f, -30f + bob), P(-5.5f, -36f + bob) }, h, true);
                        else Disc(p, P(7.8f, -39f + bob), 2.2f * u, h);
                    }
                    break;
            }
        }

        // ---------- 基本形 ----------
        private static Color Darker(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
        private static Color Lighter(Color c, float k) => c;

        private static void Fill(Painter2D p, Color c, bool outline)
        {
            p.fillColor = c; p.Fill();
        }

        private static void Poly(Painter2D p, Vector2[] pts, Color c, bool outline)
        {
            p.fillColor = c;
            p.BeginPath();
            p.MoveTo(pts[0]);
            for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]);
            p.ClosePath();
            p.Fill();
            if (outline)
            {
                p.strokeColor = Ink; p.lineWidth = 1.1f;
                p.BeginPath();
                p.MoveTo(pts[0]);
                for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]);
                p.ClosePath();
                p.Stroke();
            }
        }

        private static void Rect(Painter2D p, Vector2 center, float w, float h, Color c, bool outline)
        {
            Poly(p, new[] { center + new Vector2(-w / 2f, -h / 2f), center + new Vector2(w / 2f, -h / 2f), center + new Vector2(w / 2f, h / 2f), center + new Vector2(-w / 2f, h / 2f) }, c, outline);
        }

        private static void Ellipse(Painter2D p, Vector2 c, float rx, float ry, Color col, bool outline = false)
        {
            const int N = 22;
            var pts = new Vector2[N];
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2f / N;
                pts[i] = c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
            }
            Poly(p, pts, col, outline);
        }

        private static void Disc(Painter2D p, Vector2 c, float r, Color col, bool outline = false) => Ellipse(p, c, r, r, col, outline);

        private static void Ring(Painter2D p, Vector2 c, float r, Color col)
        {
            p.strokeColor = col; p.lineWidth = 1f;
            p.BeginPath();
            p.Arc(c, r, Angle.Degrees(0), Angle.Degrees(360));
            p.Stroke();
        }

        private static void Leg(Painter2D p, Vector2 hip, float u, float lift, Color pants, Color shoe, float w, bool side = false)
        {
            // hip = 脚底中心；腿从 y=-12 到 0，lift 为前后摆动造成的上下位移
            Vector2 b = hip + new Vector2(0, -lift * u);
            Poly(p, new[] { b + new Vector2(-w / 2f * u, -12f * u), b + new Vector2(w / 2f * u, -12f * u), b + new Vector2(w / 2f * u, -3f * u), b + new Vector2(-w / 2f * u, -3f * u) }, pants, true);
            float sw = side ? w + 1.6f : w + 0.8f;
            Poly(p, new[] { b + new Vector2(-w / 2f * u, -3.2f * u), b + new Vector2((side ? sw - w / 2f : w / 2f + 0.4f) * u, -3.2f * u), b + new Vector2((side ? sw - w / 2f : w / 2f + 0.4f) * u, 0f), b + new Vector2(-w / 2f * u, 0f) }, shoe, true);
        }

        private static void Arm(Painter2D p, Vector2 shoulder, float u, float swing, Color sleeve, Color skin, bool side = false)
        {
            Vector2 s = shoulder + new Vector2(0, -swing * u * 0f);
            float len = 10.5f;
            Vector2 hand = s + new Vector2(0, (len - swing) * u);
            Poly(p, new[] { s + new Vector2(-1.8f * u, 0), s + new Vector2(1.8f * u, 0), hand + new Vector2(1.6f * u, -2f * u), hand + new Vector2(-1.6f * u, -2f * u) }, sleeve, true);
            Disc(p, hand, 1.9f * u, skin, true);
        }
    }
}
