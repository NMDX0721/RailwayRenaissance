using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet
{
    public enum TabletIconKind { Moon, Sun, Speaker, SpeakerMuted, Bolt, Palette, Gear, Contrast, Bell, MusicNote, Search }

    /// <summary>
    /// 平板系统图标：用 Painter2D 绘制的矢量图标，替代 emoji。在 24×24 的设计网格内描述，按元素尺寸缩放，颜色可随状态更新。
    /// </summary>
    public sealed class TabletIconElement : VisualElement
    {
        private TabletIconKind _kind;
        private Color _color;

        public TabletIconKind Kind
        {
            get => _kind;
            set { _kind = value; MarkDirtyRepaint(); }
        }

        public Color IconColor
        {
            get => _color;
            set { _color = value; MarkDirtyRepaint(); }
        }

        public TabletIconElement(TabletIconKind kind, float size, Color color)
        {
            _kind = kind;
            _color = color;
            pickingMode = PickingMode.Ignore;
            style.width = size;
            style.height = size;
            style.flexShrink = 0;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            float size = Mathf.Min(contentRect.width, contentRect.height);
            if (size <= 0f) return;
            float k = size / 24f;
            var p = ctx.painter2D;
            p.fillColor = _color;
            p.strokeColor = _color;
            p.lineWidth = Mathf.Max(1.6f, 1.9f * k);
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;

            Vector2 P(float x, float y) => new Vector2(x * k, y * k);

            switch (_kind)
            {
                case TabletIconKind.Moon:
                    FillPolygon(p, Crescent(P(12, 12), 9f * k, P(16.5f, 8.5f), 7.6f * k));
                    break;

                case TabletIconKind.Sun:
                    p.BeginPath();
                    p.Arc(P(12, 12), 4.2f * k, Angle.Degrees(0), Angle.Degrees(360));
                    p.Fill();
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        p.BeginPath();
                        p.MoveTo(P(12, 12) + dir * 7.0f * k);
                        p.LineTo(P(12, 12) + dir * 9.6f * k);
                        p.Stroke();
                    }
                    break;

                case TabletIconKind.Speaker:
                case TabletIconKind.SpeakerMuted:
                    FillPolygon(p, new List<Vector2> { P(3.5f, 9.5f), P(7.5f, 9.5f), P(12.5f, 5f), P(12.5f, 19f), P(7.5f, 14.5f), P(3.5f, 14.5f) });
                    if (_kind == TabletIconKind.Speaker)
                    {
                        p.BeginPath();
                        p.Arc(P(12.5f, 12), 4.6f * k, Angle.Degrees(-50), Angle.Degrees(50));
                        p.Stroke();
                        p.BeginPath();
                        p.Arc(P(12.5f, 12), 8.2f * k, Angle.Degrees(-50), Angle.Degrees(50));
                        p.Stroke();
                    }
                    else
                    {
                        p.BeginPath();
                        p.MoveTo(P(16, 9.5f)); p.LineTo(P(21, 14.5f));
                        p.MoveTo(P(21, 9.5f)); p.LineTo(P(16, 14.5f));
                        p.Stroke();
                    }
                    break;

                case TabletIconKind.Bolt:
                    FillPolygon(p, new List<Vector2> { P(13.5f, 2.5f), P(5.5f, 13.5f), P(11, 13.5f), P(9.8f, 21.5f), P(18.5f, 10f), P(12.8f, 10f) });
                    break;

                case TabletIconKind.Palette:
                    // 圆盘上排布四个色点
                    p.BeginPath();
                    p.Arc(P(12, 12), 9f * k, Angle.Degrees(0), Angle.Degrees(360));
                    p.Stroke();
                    DrawDot(p, P(8.2f, 9.5f), 1.7f * k);
                    DrawDot(p, P(12.5f, 7.2f), 1.7f * k);
                    DrawDot(p, P(16.3f, 10.2f), 1.7f * k);
                    DrawDot(p, P(8.6f, 14.6f), 1.7f * k);
                    break;

                case TabletIconKind.Gear:
                    p.BeginPath();
                    p.Arc(P(12, 12), 5.6f * k, Angle.Degrees(0), Angle.Degrees(360));
                    p.Stroke();
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        p.BeginPath();
                        p.MoveTo(P(12, 12) + dir * 7.6f * k);
                        p.LineTo(P(12, 12) + dir * 10.2f * k);
                        p.lineWidth = Mathf.Max(2.4f, 3f * k);
                        p.Stroke();
                        p.lineWidth = Mathf.Max(1.6f, 1.9f * k);
                    }
                    break;

                case TabletIconKind.Bell:
                {
                    var pts = new List<Vector2>();
                    for (int i = 0; i <= 12; i++)   // 钟顶圆弧：180° → 360°
                    {
                        float ang = Mathf.PI + i * Mathf.PI / 12f;
                        pts.Add(P(12, 11) + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 6.2f * k);
                    }
                    pts.Add(P(18.2f, 15.2f)); pts.Add(P(20.4f, 17.6f));
                    pts.Add(P(3.6f, 17.6f)); pts.Add(P(5.8f, 15.2f));
                    FillPolygon(p, pts);
                    DrawDot(p, P(12, 20.2f), 1.8f * k);
                    DrawDot(p, P(12, 4.3f), 1.2f * k);
                    break;
                }

                case TabletIconKind.Search:
                    p.BeginPath();
                    p.Arc(P(10.5f, 10.5f), 6.5f * k, Angle.Degrees(0), Angle.Degrees(360));
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(P(15.4f, 15.4f)); p.LineTo(P(20.5f, 20.5f));
                    p.Stroke();
                    break;

                case TabletIconKind.MusicNote:
                    DrawDot(p, P(8.6f, 17.6f), 3.3f * k);
                    p.BeginPath();
                    p.MoveTo(P(11.6f, 17.4f)); p.LineTo(P(11.6f, 4.6f));
                    p.Stroke();
                    FillPolygon(p, new List<Vector2> { P(11.6f, 4.6f), P(18.6f, 7.2f), P(18.6f, 11.2f), P(11.6f, 8.6f) });
                    break;

                case TabletIconKind.Contrast:
                    p.BeginPath();
                    p.Arc(P(12, 12), 9f * k, Angle.Degrees(0), Angle.Degrees(360));
                    p.Stroke();
                    // 右半圆填充
                    p.BeginPath();
                    p.MoveTo(P(12, 3));
                    p.Arc(P(12, 12), 9f * k, Angle.Degrees(-90), Angle.Degrees(90));
                    p.ClosePath();
                    p.Fill();
                    break;
            }
        }

        private static void DrawDot(Painter2D p, Vector2 c, float r)
        {
            p.BeginPath();
            p.Arc(c, r, Angle.Degrees(0), Angle.Degrees(360));
            p.Fill();
        }

        private static void FillPolygon(Painter2D p, List<Vector2> pts)
        {
            if (pts == null || pts.Count < 3) return;
            p.BeginPath();
            p.MoveTo(pts[0]);
            for (int i = 1; i < pts.Count; i++) p.LineTo(pts[i]);
            p.ClosePath();
            p.Fill();
        }

        /// <summary>月牙：圆 A 去掉圆 B 覆盖的部分，用采样点拼出轮廓。</summary>
        private static List<Vector2> Crescent(Vector2 ca, float ra, Vector2 cb, float rb)
        {
            const int n = 72;
            var outerPts = new List<Vector2>();   // 圆 A 上位于圆 B 之外的点
            var innerPts = new List<Vector2>();   // 圆 B 上位于圆 A 之内的点
            var outerFlags = new bool[n];
            var innerFlags = new bool[n];
            var outerAll = new Vector2[n];
            var innerAll = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                outerAll[i] = ca + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ra;
                innerAll[i] = cb + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rb;
                outerFlags[i] = (outerAll[i] - cb).sqrMagnitude > rb * rb;
                innerFlags[i] = (innerAll[i] - ca).sqrMagnitude < ra * ra;
            }
            CollectRun(outerAll, outerFlags, outerPts);
            CollectRun(innerAll, innerFlags, innerPts);
            if (outerPts.Count == 0 || innerPts.Count == 0) return outerPts;

            // 内弧方向取与外弧末端更近的一端起笔，保证轮廓不交叉
            var end = outerPts[outerPts.Count - 1];
            if ((innerPts[0] - end).sqrMagnitude > (innerPts[innerPts.Count - 1] - end).sqrMagnitude)
                innerPts.Reverse();
            outerPts.AddRange(innerPts);
            return outerPts;
        }

        // 收集环上连续为 true 的一段（允许跨越 0 点）
        private static void CollectRun(Vector2[] pts, bool[] flags, List<Vector2> result)
        {
            int n = pts.Length;
            int start = -1;
            for (int i = 0; i < n; i++)
            {
                if (flags[i] && !flags[(i - 1 + n) % n]) { start = i; break; }
            }
            if (start < 0) return;
            for (int c = 0; c < n; c++)
            {
                int i = (start + c) % n;
                if (!flags[i]) break;
                result.Add(pts[i]);
            }
        }
    }
}
