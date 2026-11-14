using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Paper
{
    /// <summary>
    /// 「站台纸本」精绘函数库（Painter2D）。所有装饰都在这里：
    /// 圆角矩形路径、纸面内描边/高光、按钮高光、徽章、凹陷格、穿孔线、轨道分隔、车票缺口、印章、刻度环、刻度尺、账页格线。
    /// 约定：坐标原点为元素左上角（border-box），尺寸取 element.layout。
    /// </summary>
    public static class PaperDraw
    {
        public static void RoundRectPath(Painter2D p, Rect r, float rad)
        {
            rad = Mathf.Max(0.5f, Mathf.Min(rad, Mathf.Min(r.width, r.height) / 2f));
            p.MoveTo(new Vector2(r.xMin + rad, r.yMin));
            p.LineTo(new Vector2(r.xMax - rad, r.yMin));
            p.ArcTo(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMin + rad), rad);
            p.LineTo(new Vector2(r.xMax, r.yMax - rad));
            p.ArcTo(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - rad, r.yMax), rad);
            p.LineTo(new Vector2(r.xMin + rad, r.yMax));
            p.ArcTo(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMax - rad), rad);
            p.LineTo(new Vector2(r.xMin, r.yMin + rad));
            p.ArcTo(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + rad, r.yMin), rad);
            p.ClosePath();
        }

        public static void Circle(Painter2D p, Vector2 c, float r, Color fill)
        {
            p.fillColor = fill;
            p.BeginPath(); p.Arc(c, r, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
        }

        public static void CircleLine(Painter2D p, Vector2 c, float r, Color col, float w)
        {
            p.strokeColor = col; p.lineWidth = w;
            p.BeginPath(); p.Arc(c, r, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
        }

        public static void Line(Painter2D p, Vector2 a, Vector2 b, Color col, float w)
        {
            p.strokeColor = col; p.lineWidth = w;
            p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.Stroke();
        }

        /// <summary>纸面卡片装饰：内描边、顶部高光、底部暗线；rivets=大卡片四角铆钉。</summary>
        public static void Deco(Painter2D p, float w, float h, float radius, bool rivets = false)
        {
            if (w < 12f || h < 12f) return;
            p.strokeColor = new Color(0.42f, 0.32f, 0.20f, 0.20f); p.lineWidth = 1f;
            p.BeginPath(); RoundRectPath(p, new Rect(3.5f, 3.5f, w - 7f, h - 7f), Mathf.Max(2f, radius - 3.5f)); p.Stroke();
            Line(p, new Vector2(radius * 0.8f, 1.2f), new Vector2(w - radius * 0.8f, 1.2f), new Color(1f, 1f, 1f, 0.85f), 1.2f);
            Line(p, new Vector2(radius * 0.8f, h - 1.2f), new Vector2(w - radius * 0.8f, h - 1.2f), new Color(0.30f, 0.20f, 0.10f, 0.14f), 1.2f);
            if (rivets)
                foreach (var c in new[] { new Vector2(11f, 11f), new Vector2(w - 11f, 11f), new Vector2(11f, h - 11f), new Vector2(w - 11f, h - 11f) })
                    Circle(p, c, 2.2f, new Color(0.42f, 0.32f, 0.20f, 0.32f));
        }

        /// <summary>按钮高光：上半部白色柔光 + 下沿暗线。</summary>
        public static void Glossy(Painter2D p, float w, float h, float radius)
        {
            if (w < 8f || h < 8f) return;
            p.fillColor = new Color(1f, 1f, 1f, 0.20f);
            p.BeginPath(); RoundRectPath(p, new Rect(2f, 2f, w - 4f, h * 0.46f), radius); p.Fill();
            Line(p, new Vector2(radius, h - 1f), new Vector2(w - radius, h - 1f), new Color(0.30f, 0.15f, 0.04f, 0.28f), 1.5f);
        }

        /// <summary>圆形徽章：色环 + 高光弧。</summary>
        public static void Medal(Painter2D p, float w, float h, Color tint)
        {
            var c = new Vector2(w / 2f, h / 2f);
            CircleLine(p, c, w / 2f - 1.6f, new Color(tint.r, tint.g, tint.b, 0.55f), 1.6f);
            p.strokeColor = new Color(1f, 1f, 1f, 0.85f); p.lineWidth = 1.6f;
            p.BeginPath(); p.Arc(c, w / 2f - 4f, Angle.Degrees(200), Angle.Degrees(260)); p.Stroke();
        }

        /// <summary>凹陷格：左上暗、右下亮的内斜面。</summary>
        public static void Slot(Painter2D p, float w, float h)
        {
            p.lineWidth = 2f;
            p.strokeColor = new Color(0.30f, 0.20f, 0.10f, 0.22f);
            p.BeginPath(); p.MoveTo(new Vector2(4f, h - 5f)); p.LineTo(new Vector2(4f, 4f)); p.LineTo(new Vector2(w - 5f, 4f)); p.Stroke();
            p.strokeColor = new Color(1f, 1f, 1f, 0.9f);
            p.BeginPath(); p.MoveTo(new Vector2(w - 4f, 5f)); p.LineTo(new Vector2(w - 4f, h - 4f)); p.LineTo(new Vector2(5f, h - 4f)); p.Stroke();
        }

        /// <summary>穿孔线：等距小圆点（车票撕口 / 账页分隔）。</summary>
        public static void Perforation(Painter2D p, Vector2 a, Vector2 b, Color col, float spacing = 8f, float r = 1.3f)
        {
            float len = Vector2.Distance(a, b);
            if (len < 1f) return;
            Vector2 d = (b - a) / len;
            p.fillColor = col;
            p.BeginPath();
            for (float t = 0f; t <= len; t += spacing)
            {
                Vector2 c = a + d * t;
                p.MoveTo(c + new Vector2(r, 0f));
                p.Arc(c, r, Angle.Degrees(0), Angle.Degrees(360));
            }
            p.Fill();
        }

        /// <summary>轨道分隔线：双轨 + 枕木。</summary>
        public static void Rail(Painter2D p, float x0, float x1, float y, Color col, float tie = 12f)
        {
            Line(p, new Vector2(x0, y - 2f), new Vector2(x1, y - 2f), col, 1.2f);
            Line(p, new Vector2(x0, y + 2f), new Vector2(x1, y + 2f), col, 1.2f);
            for (float x = x0 + 3f; x < x1; x += tie)
                Line(p, new Vector2(x, y - 4f), new Vector2(x, y + 4f), new Color(col.r, col.g, col.b, col.a * 0.7f), 1.6f);
        }

        /// <summary>车票缺口：左右边中线处的半圆切口 + 弧线（底色需与背后一致）。</summary>
        public static void TicketNotch(Painter2D p, float w, float h, float r, Color back, Color line)
        {
            foreach (float x in new[] { 0f, w })
            {
                var c = new Vector2(x, h / 2f);
                Circle(p, c, r, back);
                CircleLine(p, c, r, line, 1.2f);
            }
        }

        /// <summary>印章：双圈 + 内圈刻度，文字由组件层叠 Label。</summary>
        public static void Stamp(Painter2D p, Vector2 c, float r, Color col)
        {
            CircleLine(p, c, r, col, 3f);
            CircleLine(p, c, r - 6f, col, 1.3f);
            p.strokeColor = col; p.lineWidth = 1.3f;
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                p.BeginPath(); p.MoveTo(c + d * (r - 3f)); p.LineTo(c + d * (r - 1f)); p.Stroke();
            }
        }

        /// <summary>刻度环：270° 刻度 + 数值弧 + 指针。value 0..1。</summary>
        public static void Dial(Painter2D p, Vector2 c, float r, float value, Color color)
        {
            value = Mathf.Clamp01(value);
            const float start = 135f, sweep = 270f;
            p.strokeColor = new Color(0.42f, 0.32f, 0.20f, 0.30f); p.lineWidth = 1.2f;
            for (int i = 0; i <= 40; i++)
            {
                float a = (start + sweep * i / 40f) * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float len = i % 10 == 0 ? 7f : 4f;
                p.BeginPath(); p.MoveTo(c + d * r); p.LineTo(c + d * (r - len)); p.Stroke();
            }
            p.strokeColor = new Color(0.42f, 0.32f, 0.20f, 0.14f); p.lineWidth = 6f; p.lineCap = LineCap.Round;
            p.BeginPath(); p.Arc(c, r - 12f, Angle.Degrees(start), Angle.Degrees(start + sweep)); p.Stroke();
            if (value > 0.001f)
            {
                p.strokeColor = color; p.lineWidth = 6f;
                p.BeginPath(); p.Arc(c, r - 12f, Angle.Degrees(start), Angle.Degrees(start + sweep * value)); p.Stroke();
            }
            float na = (start + sweep * value) * Mathf.Deg2Rad;
            Circle(p, c + new Vector2(Mathf.Cos(na), Mathf.Sin(na)) * (r - 12f), 3.6f, Color.white);
            CircleLine(p, c + new Vector2(Mathf.Cos(na), Mathf.Sin(na)) * (r - 12f), 3.6f, color, 1.6f);
            p.lineCap = LineCap.Butt;
        }

        /// <summary>刻度尺：横向分段刻度；center 为 0 点（双向效果条），value -1..1。</summary>
        public static void Ruler(Painter2D p, float x0, float x1, float y, float value, Color pos, Color neg, bool bidirectional)
        {
            float w = x1 - x0;
            p.strokeColor = new Color(0.42f, 0.32f, 0.20f, 0.30f); p.lineWidth = 1f;
            int n = 20;
            for (int i = 0; i <= n; i++)
            {
                float x = x0 + w * i / n; float len = i % 5 == 0 ? 7f : 4f;
                p.BeginPath(); p.MoveTo(new Vector2(x, y + 6f)); p.LineTo(new Vector2(x, y + 6f + len)); p.Stroke();
            }
            p.fillColor = new Color(0.42f, 0.32f, 0.20f, 0.10f);
            p.BeginPath(); RoundRectPath(p, new Rect(x0, y - 4f, w, 8f), 4f); p.Fill();
            if (bidirectional)
            {
                float mid = x0 + w / 2f; float len = Mathf.Abs(value) * w / 2f;
                if (len > 0.5f)
                {
                    p.fillColor = value >= 0f ? pos : neg;
                    p.BeginPath(); RoundRectPath(p, new Rect(value >= 0f ? mid : mid - len, y - 4f, len, 8f), 4f); p.Fill();
                }
                Line(p, new Vector2(mid, y - 8f), new Vector2(mid, y + 8f), new Color(0.17f, 0.14f, 0.11f, 0.6f), 1.5f);
            }
            else if (value > 0.001f)
            {
                p.fillColor = pos;
                p.BeginPath(); RoundRectPath(p, new Rect(x0, y - 4f, w * Mathf.Clamp01(value), 8f), 4f); p.Fill();
            }
        }

        /// <summary>账页：横格线 + 左侧红色边线。</summary>
        public static void LedgerGrid(Painter2D p, float w, float h, float rowH = 28f, float marginX = 36f)
        {
            for (float y = rowH; y < h; y += rowH)
                Line(p, new Vector2(8f, y), new Vector2(w - 8f, y), new Color(0.35f, 0.50f, 0.65f, 0.16f), 1f);
            Line(p, new Vector2(marginX, 6f), new Vector2(marginX, h - 6f), new Color(0.75f, 0.30f, 0.28f, 0.25f), 1.3f);
        }
    }
}
