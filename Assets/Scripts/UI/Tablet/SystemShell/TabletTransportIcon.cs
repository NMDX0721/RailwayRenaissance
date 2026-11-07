using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    public enum TabletTransportKind { Prev, Next, Pause, Play }

    /// <summary>播控按键的矢量图标：上一首 / 下一首（竖条 + 三角）、暂停、播放。超级岛与控制中心共用。</summary>
    public sealed class TabletTransportIcon : VisualElement
    {
        private readonly TabletTransportKind _kind;
        private readonly Color _color;

        public TabletTransportIcon(TabletTransportKind kind, float size, Color color)
        {
            _kind = kind; _color = color;
            pickingMode = PickingMode.Ignore;
            style.width = size; style.height = size; style.flexShrink = 0;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            float w = contentRect.width, h = contentRect.height;
            if (w <= 0f || h <= 0f) return;
            var p = ctx.painter2D;
            p.fillColor = _color;
            void Poly(params Vector2[] pts)
            {
                p.BeginPath(); p.MoveTo(pts[0]);
                for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]);
                p.ClosePath(); p.Fill();
            }
            switch (_kind)
            {
                case TabletTransportKind.Prev:
                    Poly(new Vector2(w * 0.10f, h * 0.18f), new Vector2(w * 0.24f, h * 0.18f), new Vector2(w * 0.24f, h * 0.82f), new Vector2(w * 0.10f, h * 0.82f));
                    Poly(new Vector2(w * 0.92f, h * 0.16f), new Vector2(w * 0.92f, h * 0.84f), new Vector2(w * 0.30f, h * 0.50f));
                    break;
                case TabletTransportKind.Next:
                    Poly(new Vector2(w * 0.90f, h * 0.18f), new Vector2(w * 0.76f, h * 0.18f), new Vector2(w * 0.76f, h * 0.82f), new Vector2(w * 0.90f, h * 0.82f));
                    Poly(new Vector2(w * 0.08f, h * 0.16f), new Vector2(w * 0.08f, h * 0.84f), new Vector2(w * 0.70f, h * 0.50f));
                    break;
                case TabletTransportKind.Pause:
                    Poly(new Vector2(w * 0.20f, h * 0.10f), new Vector2(w * 0.40f, h * 0.10f), new Vector2(w * 0.40f, h * 0.90f), new Vector2(w * 0.20f, h * 0.90f));
                    Poly(new Vector2(w * 0.60f, h * 0.10f), new Vector2(w * 0.80f, h * 0.10f), new Vector2(w * 0.80f, h * 0.90f), new Vector2(w * 0.60f, h * 0.90f));
                    break;
                default:
                    Poly(new Vector2(w * 0.22f, h * 0.08f), new Vector2(w * 0.22f, h * 0.92f), new Vector2(w * 0.90f, h * 0.50f));
                    break;
            }
        }
    }
}
