using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet
{
    /// <summary>
    /// 桌面模拟时钟小组件：Painter2D 绘制刻度与时分秒针，读系统时间，每秒重绘。
    /// 数字 12/3/6/9 等由外层标签负责（字体渲染比矢量路径清晰）。
    /// </summary>
    public sealed class TabletAnalogClock : VisualElement
    {
        private static readonly Color Tick = new Color(0.30f, 0.30f, 0.34f, 0.75f);
        private static readonly Color Hand = new Color(0.12f, 0.12f, 0.15f, 1f);
        private static readonly Color Second = new Color(0.86f, 0.22f, 0.20f, 1f);

        public TabletAnalogClock()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0; style.top = 0; style.right = 0; style.bottom = 0;
            generateVisualContent += Draw;
            schedule.Execute(MarkDirtyRepaint).Every(1000);
        }

        private static Vector2 Polar(Vector2 c, float angleDeg, float radius)
        {
            float a = (angleDeg - 90f) * Mathf.Deg2Rad;
            return c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }

        private void Draw(MeshGenerationContext mgc)
        {
            Rect r = contentRect;
            if (r.width <= 0f || r.height <= 0f) return;
            var p = mgc.painter2D;
            Vector2 c = r.center;
            float radius = Mathf.Min(r.width, r.height) * 0.5f - 6f;

            p.lineCap = LineCap.Round;
            for (int i = 0; i < 60; i++)
            {
                bool major = i % 5 == 0;
                float outer = radius;
                float inner = radius - (major ? 5f : 2.5f);
                p.strokeColor = Tick;
                p.lineWidth = major ? 1.6f : 0.9f;
                p.BeginPath();
                p.MoveTo(Polar(c, i * 6f, inner));
                p.LineTo(Polar(c, i * 6f, outer));
                p.Stroke();
            }

            DateTime t = DateTime.Now;
            float sec = t.Second;
            float min = t.Minute + sec / 60f;
            float hour = (t.Hour % 12) + min / 60f;

            DrawHand(p, c, hour * 30f, radius * 0.50f, 3.6f, Hand);
            DrawHand(p, c, min * 6f, radius * 0.74f, 2.6f, Hand);

            p.strokeColor = Second;
            p.lineWidth = 1.3f;
            p.BeginPath();
            p.MoveTo(Polar(c, sec * 6f + 180f, radius * 0.16f));
            p.LineTo(Polar(c, sec * 6f, radius * 0.84f));
            p.Stroke();

            p.fillColor = Second;
            p.BeginPath();
            p.Arc(c, 3.2f, Angle.Degrees(0), Angle.Degrees(360));
            p.Fill();
        }

        private static void DrawHand(Painter2D p, Vector2 c, float angleDeg, float length, float width, Color color)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.BeginPath();
            p.MoveTo(Polar(c, angleDeg + 180f, length * 0.12f));
            p.LineTo(Polar(c, angleDeg, length));
            p.Stroke();
        }
    }
}
