using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet
{
    /// <summary>
    /// 媒姬百科的应用图标：「媒姬」——百科的拟人形象，造型参考「岁月」（黑紫色长发、侧边大红蝴蝶结、水手领红领巾、红滚边黑夹克），
    /// 头上别一枚书形发夹。画法是日系头像：大头特写、带笔压的细描线、赛璐璐平涂加一层阴影、带星形高光的大眼睛、分缕的刘海。
    /// 全部用 Painter2D 在 100×100 网格里画出，不依赖图片资源。尺寸小的时候放大脸部并省掉细节。
    /// 外层元素负责圆角裁切，内层画布负责绘制。
    /// </summary>
    public sealed class TabletMediaMascot : VisualElement
    {
        public static readonly Color Paper = new Color(0.97f, 0.985f, 0.98f, 1f);
        private static readonly Color Wash = new Color(0.72f, 0.90f, 0.86f, 1f);
        private static readonly Color WashLight = new Color(0.83f, 0.95f, 0.92f, 1f);
        private static readonly Color Line = new Color(0.16f, 0.13f, 0.22f, 1f);
        private static readonly Color Hair = new Color(0.28f, 0.24f, 0.37f, 1f);
        private static readonly Color HairMid = new Color(0.35f, 0.30f, 0.47f, 1f);
        private static readonly Color HairDeep = new Color(0.19f, 0.16f, 0.26f, 1f);
        private static readonly Color HairLight = new Color(0.66f, 0.61f, 0.84f, 1f);
        private static readonly Color Skin = new Color(1f, 0.968f, 0.95f, 1f);
        private static readonly Color SkinShade = new Color(0.985f, 0.87f, 0.85f, 1f);
        private static readonly Color Red = new Color(0.93f, 0.27f, 0.31f, 1f);
        private static readonly Color RedDeep = new Color(0.72f, 0.15f, 0.22f, 1f);
        private static readonly Color RedLight = new Color(1f, 0.58f, 0.57f, 1f);
        private static readonly Color Jacket = new Color(0.22f, 0.21f, 0.28f, 1f);
        private static readonly Color JacketLight = new Color(0.34f, 0.33f, 0.43f, 1f);
        private static readonly Color JacketDeep = new Color(0.15f, 0.14f, 0.20f, 1f);
        private static readonly Color Navy = new Color(0.23f, 0.26f, 0.43f, 1f);
        private static readonly Color NavyDeep = new Color(0.16f, 0.18f, 0.32f, 1f);
        private static readonly Color PageShade = new Color(0.85f, 0.88f, 0.95f, 1f);
        private static readonly Color IrisRim = new Color(0.20f, 0.16f, 0.31f, 1f);
        private static readonly Color IrisTop = new Color(0.31f, 0.23f, 0.52f, 1f);
        private static readonly Color IrisMid = new Color(0.52f, 0.44f, 0.82f, 1f);
        private static readonly Color IrisGlow = new Color(0.79f, 0.76f, 0.99f, 1f);
        private static readonly Color Teal = new Color(0.26f, 0.66f, 0.60f, 1f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.40f, 1f);

        private float _s, _zk = 1f, _zx, _zy, _sqK = 1f, _sqY;
        private const float EyeSquash = 0.8f, EyePivot = 62.4f, EyeScale = 0.74f;
        private bool _fine, _rich;
        private Painter2D _p;

        public TabletMediaMascot(float size, bool background = true)
        {
            pickingMode = PickingMode.Ignore;
            style.width = size; style.height = size; style.flexShrink = 0;
            if (background)
            {
                style.backgroundColor = Paper; style.overflow = Overflow.Hidden;
                float r = size * 0.22f;
                style.borderTopLeftRadius = r; style.borderTopRightRadius = r; style.borderBottomLeftRadius = r; style.borderBottomRightRadius = r;
            }
            var canvas = new VisualElement { pickingMode = PickingMode.Ignore };
            canvas.style.position = Position.Absolute; canvas.style.left = 0; canvas.style.top = 0; canvas.style.width = size; canvas.style.height = size;
            canvas.generateVisualContent += ctx => Draw(ctx, size);
            Add(canvas);
        }

        // ---------- 路径：自己采样曲线，只用 MoveTo/LineTo ----------
        private sealed class Pts
        {
            public readonly List<Vector2> L = new List<Vector2>();
            private Vector2 _c;
            public Pts M(float x, float y) { _c = new Vector2(x, y); L.Add(_c); return this; }
            public Pts To(float x, float y) { _c = new Vector2(x, y); L.Add(_c); return this; }
            public Pts Q(float cx, float cy, float x, float y)
            {
                var a = _c; var c = new Vector2(cx, cy); var b = new Vector2(x, y);
                for (int i = 1; i <= 10; i++) { float t = i / 10f, u = 1 - t; L.Add(u * u * a + 2 * u * t * c + t * t * b); }
                _c = b; return this;
            }
            public Pts C(float c1x, float c1y, float c2x, float c2y, float x, float y)
            {
                var a = _c; var c1 = new Vector2(c1x, c1y); var c2 = new Vector2(c2x, c2y); var b = new Vector2(x, y);
                for (int i = 1; i <= 16; i++) { float t = i / 16f, u = 1 - t; L.Add(u * u * u * a + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * b); }
                _c = b; return this;
            }
            public Pts Shift(float dx, float dy)
            {
                var o = new Pts(); foreach (var v in L) o.L.Add(new Vector2(v.x + dx, v.y + dy)); return o;
            }
        }

        private static Pts Oval(float cx, float cy, float rx, float ry, int n = 28)
        {
            var o = new Pts();
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                o.L.Add(new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry));
            }
            return o;
        }

        private static Pts Star(float x, float y, float r, float inner = 0.3f)
        {
            var st = new Pts();
            for (int i = 0; i < 8; i++)
            {
                float a = (i * 45f - 90f) * Mathf.Deg2Rad, rr = i % 2 == 0 ? r : r * inner;
                st.L.Add(new Vector2(x + Mathf.Cos(a) * rr, y + Mathf.Sin(a) * rr));
            }
            return st;
        }

        private void Trace(List<Vector2> l, bool closed)
        {
            _p.BeginPath();
            for (int i = 0; i < l.Count; i++)
            {
                var q = l[i]; q.y = _sqY + (q.y - _sqY) * _sqK; // 眼睛用：纵向压扁
                var v = (q * _zk + new Vector2(_zx, _zy)) * _s;
                if (i == 0) _p.MoveTo(v); else _p.LineTo(v);
            }
            if (closed) _p.ClosePath();
        }

        private void Fill(Pts s, Color c) { _p.fillColor = c; Trace(s.L, true); _p.Fill(); }

        /// <summary>等宽描线（w 为网格单位）。</summary>
        private void Stroke(Pts s, float w, Color c, bool closed = false)
        {
            _p.strokeColor = c; _p.lineWidth = Mathf.Max(_rich ? 0.7f : 0.9f, w * _zk * _s);
            Trace(s.L, closed); _p.Stroke();
        }

        private void Shape(Pts s, Color fill, float w = 0.62f) { Fill(s, fill); Stroke(s, w, Line, true); }

        /// <summary>带笔压的线：中间粗、两头收细（t0/t1 是起笔、收笔处相对的粗细）。</summary>
        private void Pen(Pts s, float w, Color c, float t0 = 0.12f, float t1 = 0.12f)
        {
            if (w * _zk * _s < 1.15f) { Stroke(s, w * 0.75f, c); return; }
            var l = s.L;
            if (l.Count < 2) return;
            if (l.Count < 8)
            {
                var d = new List<Vector2>();
                for (int i = 0; i < l.Count - 1; i++)
                    for (int k = 0; k < 6; k++) d.Add(Vector2.Lerp(l[i], l[i + 1], k / 6f));
                d.Add(l[l.Count - 1]); l = d;
            }
            int n = l.Count;
            var left = new List<Vector2>(n * 2); var right = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                var dir = (l[Mathf.Min(i + 1, n - 1)] - l[Mathf.Max(i - 1, 0)]).normalized;
                var nrm = new Vector2(-dir.y, dir.x);
                float k = Mathf.Lerp(t0, 1f, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.4f))) * Mathf.Lerp(t1, 1f, Mathf.SmoothStep(0, 1, Mathf.Clamp01((1 - t) / 0.4f)));
                left.Add(l[i] + nrm * (w * 0.5f * k)); right.Add(l[i] - nrm * (w * 0.5f * k));
            }
            for (int i = n - 1; i >= 0; i--) left.Add(right[i]);
            _p.fillColor = c; Trace(left, true); _p.Fill();
        }

        private static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private void Draw(MeshGenerationContext ctx, float size)
        {
            if (size <= 0f) return;
            _s = size / 100f; _fine = size >= 52f; _rich = size >= 88f; _p = ctx.painter2D;
            _p.lineCap = LineCap.Round; _p.lineJoin = LineJoin.Round;

            _zk = 1f; _zx = 0f; _zy = 0f;
            Fill(Oval(50, 50, 44, 44, 56), Wash);
            Fill(Oval(50, 46, 35, 34, 48), WashLight);

            // 小尺寸：把脸放大一点
            if (!_rich) { _zk = 1.14f; _zx = -7f; _zy = -5f; }

            DrawBackHair();
            DrawBody();
            DrawFace();
            // 眼睛按参考图的比例：连睫毛宽约占脸宽四分之一。以眼心为中心整体缩小，并往中间靠一点
            foreach (int d in new[] { -1, 1 })
            {
                float zk = _zk, zx = _zx, zy = _zy;
                _zx = zx + zk * ((50 + d * 12.8f) * (1 - EyeScale) - d * 0.5f);
                _zy = zy + zk * EyePivot * (1 - EyeScale);
                _zk = zk * EyeScale;
                _sqK = EyeSquash; _sqY = EyePivot; DrawEye(d); _sqK = 1f;
                _zk = zk; _zx = zx; _zy = zy;
            }
            DrawMouthAndBlush();
            DrawFrontHair();
            DrawBow();
            DrawBookPin();

            _zk = 1f; _zx = 0f; _zy = 0f;
            if (_fine)
            {
                Fill(Star(88, 15, 3.4f), Color.white); Fill(Star(93, 26, 1.6f), Color.white);
                Fill(Star(8.5f, 56, 2.2f), Color.white); Fill(Star(92.5f, 52, 2f), Gold);
            }
        }

        // ---------------------------------------------------------------- 后发
        private void DrawBackHair()
        {
            var back = new Pts().M(20.5f, 44).C(16, 5, 84, 5, 79.5f, 44)
                .C(86, 55, 84, 65, 88, 75).C(91.5f, 85, 87, 93, 90, 102)
                .To(10, 102).C(13, 93, 8.5f, 85, 12, 75).C(16, 65, 14, 55, 20.5f, 44);
            Shape(back, Hair, 0.7f);
            // 脸后面的头发在阴影里
            Fill(new Pts().M(27, 60).To(73, 60).C(77, 76, 76, 90, 78, 102).To(22, 102).C(24, 90, 23, 76, 27, 60), HairDeep);
            if (!_fine) return;
            foreach (int d in new[] { -1, 1 })
            {
                // 外缘波浪的暗面
                Fill(new Pts().M(50 + d * 36.4f, 76).C(50 + d * 40.6f, 84, 50 + d * 37.4f, 93, 50 + d * 39.4f, 102).To(50 + d * 35.6f, 102).C(50 + d * 34.4f, 93, 50 + d * 37.6f, 85, 50 + d * 36.4f, 76), HairDeep);
                // 波浪上的高光片
                Fill(new Pts().M(50 + d * 32.6f, 50).C(50 + d * 36.4f, 56, 50 + d * 35.8f, 64, 50 + d * 37.4f, 71).C(50 + d * 34.8f, 65, 50 + d * 34.6f, 57, 50 + d * 32.6f, 50), A(HairLight, 0.7f));
                Fill(new Pts().M(50 + d * 33.6f, 80).C(50 + d * 35.6f, 86, 50 + d * 33.8f, 91, 50 + d * 34.6f, 96).C(50 + d * 32.8f, 91, 50 + d * 34f, 86, 50 + d * 33.6f, 80), A(HairLight, 0.5f));
                // 分缕的发丝
                Pen(new Pts().M(50 + d * 29.5f, 48).C(50 + d * 33.4f, 62, 50 + d * 30.6f, 80, 50 + d * 33.6f, 101), 0.55f, A(Line, 0.85f));
                if (!_rich) continue;
                Pen(new Pts().M(50 + d * 31.6f, 60).C(50 + d * 36.4f, 72, 50 + d * 33.4f, 86, 50 + d * 36.6f, 101), 0.42f, A(Line, 0.7f));
                Pen(new Pts().M(50 + d * 27.4f, 70).C(50 + d * 29.6f, 80, 50 + d * 28.4f, 90, 50 + d * 30.4f, 101), 0.4f, A(Line, 0.6f));
                Pen(new Pts().M(50 + d * 35f, 58).C(50 + d * 38f, 66, 50 + d * 37f, 74, 50 + d * 39.4f, 82), 0.4f, A(HairLight, 0.55f));
            }
        }

        // ---------------------------------------------------------------- 身体
        private void DrawBody()
        {
            // 脖子 + 下巴投影
            Fill(new Pts().M(45.6f, 74).To(54.4f, 74).To(55f, 87).To(45f, 87), Skin);
            Fill(new Pts().M(45.6f, 77).To(54.4f, 77).To(54.7f, 81.6f).Q(50, 85.6f, 45.3f, 81.6f), SkinShade);
            Pen(new Pts().M(45.6f, 78.4f).To(45.1f, 85.6f), 0.6f, Line, 0.5f, 0.2f);
            Pen(new Pts().M(54.4f, 78.4f).To(54.9f, 85.6f), 0.6f, Line, 0.5f, 0.2f);

            // 夹克
            var torso = new Pts().M(21.5f, 102).C(22.5f, 92, 30, 87.4f, 41.5f, 85.4f).To(58.5f, 85.4f).C(70, 87.4f, 77.5f, 92, 78.5f, 102);
            Shape(torso, Jacket, 0.7f);
            if (_fine)
            {
                foreach (int d in new[] { -1, 1 })
                {
                    // 肩头亮面、腋下暗面
                    Fill(new Pts().M(50 + d * 15.6f, 87.6f).C(50 + d * 21.6f, 88.8f, 50 + d * 25.4f, 92, 50 + d * 27f, 97).C(50 + d * 23.6f, 92.6f, 50 + d * 20f, 90.2f, 50 + d * 15.6f, 89.2f), JacketLight);
                    Fill(new Pts().M(50 + d * 14f, 92).C(50 + d * 16.4f, 95, 50 + d * 16.6f, 98, 50 + d * 16f, 102).To(50 + d * 13.6f, 102), JacketDeep);
                }
            }
            // 衬衫
            Fill(new Pts().M(44.6f, 85.2f).To(55.4f, 85.2f).To(57, 102).To(43, 102), Color.white);
            if (_fine) Fill(new Pts().M(44.6f, 85.2f).To(55.4f, 85.2f).To(53.6f, 88.4f).Q(50, 90.6f, 46.4f, 88.4f), PageShade);
            foreach (int d in new[] { -1, 1 })
            {
                // 水手领（两道白边）
                var flap = new Pts().M(50 + d * 4.6f, 85.2f).To(50 + d * 13.6f, 88.4f).To(50 + d * 7.6f, 100.4f).To(50, 94.4f);
                Shape(flap, Navy, 0.6f);
                if (_fine)
                {
                    Fill(new Pts().M(50 + d * 4.6f, 85.2f).To(50 + d * 8.6f, 86.6f).To(50 + d * 2.4f, 93.2f).To(50, 94.4f), NavyDeep);
                    Stroke(new Pts().M(50 + d * 12.2f, 89.2f).To(50 + d * 7.2f, 98.6f), 0.42f, Color.white);
                    Stroke(new Pts().M(50 + d * 10.9f, 88.7f).To(50 + d * 6.1f, 97.6f), 0.42f, Color.white);
                }
                // 夹克立领（红滚边）
                var stand = new Pts().M(50 + d * 5.6f, 82.6f).Q(50 + d * 11.4f, 83f, 50 + d * 16f, 87.6f).To(50 + d * 13f, 91.2f).Q(50 + d * 9.4f, 87.2f, 50 + d * 5f, 86.4f);
                Shape(stand, Jacket, 0.6f);
                Stroke(new Pts().M(50 + d * 6f, 83.4f).Q(50 + d * 11.2f, 83.9f, 50 + d * 15.2f, 87.8f), 0.7f, Red);
                if (!_fine) continue;
                // 开襟线、肩上的红条、衣褶
                Pen(new Pts().M(50 + d * 13f, 91.2f).To(50 + d * 13.6f, 102), 0.55f, Line, 0.6f, 0.6f);
                Stroke(new Pts().M(50 + d * 20.5f, 89.6f).C(50 + d * 24.4f, 92, 50 + d * 26.2f, 96, 50 + d * 26.6f, 102), 1f, Red);
                if (!_rich) continue;
                Stroke(new Pts().M(50 + d * 18.8f, 90.2f).C(50 + d * 22.4f, 92.6f, 50 + d * 24f, 96.4f, 50 + d * 24.3f, 102), 0.32f, RedLight);
                Pen(new Pts().M(50 + d * 16.4f, 91.4f).Q(50 + d * 18.6f, 94.6f, 50 + d * 18f, 98.6f), 0.4f, A(Line, 0.8f));
                Pen(new Pts().M(50 + d * 14.6f, 94f).Q(50 + d * 15.8f, 96.4f, 50 + d * 15.2f, 99.4f), 0.35f, A(Line, 0.6f));
            }
            // 红领巾
            var tie = new Pts().M(48.4f, 96.6f).To(51.6f, 96.6f).To(53.2f, 102).To(46.8f, 102);
            Shape(tie, Red, 0.6f);
            if (_fine) Fill(new Pts().M(50.2f, 96.8f).To(51.6f, 96.6f).To(53.2f, 102).To(50.6f, 102), RedDeep);
            var knot = new Pts().M(47.2f, 93.6f).Q(50, 92.4f, 52.8f, 93.6f).Q(53.4f, 95.6f, 52.2f, 97.4f).Q(50, 98.3f, 47.8f, 97.4f).Q(46.6f, 95.6f, 47.2f, 93.6f);
            Shape(knot, Red, 0.6f);
            if (_rich)
            {
                Pen(new Pts().M(48.2f, 94.5f).Q(50, 93.7f, 51.6f, 94.4f), 0.5f, A(Color.white, 0.85f));
                Shape(Star(64.6f, 94.6f, 1.9f, 0.42f), Gold, 0.35f);
            }
        }

        // ---------------------------------------------------------------- 脸
        private void DrawFace()
        {
            var face = new Pts().M(24.5f, 30).To(24.5f, 47).C(23.4f, 66f, 36, 77.6f, 50, 79.4f).C(64, 77.6f, 76.6f, 66f, 75.5f, 47).To(75.5f, 30);
            Fill(face, Skin);
            // 刘海投在额头上的影子
            Fill(Fringe().Shift(0, 2.2f), SkinShade);
            var jaw = new Pts().M(24.5f, 47).C(23.4f, 66f, 36, 77.6f, 50, 79.4f).C(64, 77.6f, 76.6f, 66f, 75.5f, 47);
            if (_fine) Pen(jaw, 0.85f, Line, 0.75f, 0.75f); else Stroke(jaw, 0.8f, Line);
        }

        private void DrawEye(int d)
        {
            float ex = 50 + d * 12.8f; const float ey = 61.6f;
            // 眼白 + 上眼睑的投影（两层，越靠上越深）
            Fill(Oval(ex, ey, 6.5f, 7.5f, 32), Color.white);
            if (_fine)
            {
                Fill(Oval(ex, ey - 3.4f, 5f, 3.4f, 24), new Color(0.91f, 0.90f, 0.97f, 1f));
                Fill(Oval(ex, ey - 4.4f, 4.7f, 2.3f, 24), new Color(0.82f, 0.80f, 0.92f, 1f));
            }
            // 虹膜：深色外圈，里面由上到下七层渐变
            Fill(Oval(ex, ey + 0.5f, 5.7f, 6.9f, 36), IrisRim);
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                var c = t < 0.5f ? Color.Lerp(IrisTop, IrisMid, t * 2f) : Color.Lerp(IrisMid, IrisGlow, t * 2f - 1f);
                Fill(Oval(ex, ey + 0.9f + t * 3.6f, Mathf.Lerp(5.1f, 2.7f, t), Mathf.Lerp(6.2f, 1.6f, t), 30), c);
            }
            if (_fine)
            {
                // 底部的月牙反光
                Fill(new Pts().M(ex - 3.5f, ey + 4.7f).Q(ex, ey + 7.9f, ex + 3.5f, ey + 4.7f).Q(ex, ey + 6.5f, ex - 3.5f, ey + 4.7f), new Color(1f, 0.90f, 0.97f, 0.9f));
            }
            if (_rich)
            {
                // 虹膜下半的细纤维：受光的浅色短线
                for (int i = 0; i < 11; i++)
                {
                    float a = (22 + i * 13.6f) * Mathf.Deg2Rad;
                    Pen(new Pts().M(ex + Mathf.Cos(a) * 3f, ey + 0.9f + Mathf.Sin(a) * 3.7f).To(ex + Mathf.Cos(a) * 4.3f, ey + 0.9f + Mathf.Sin(a) * 5.2f), 0.26f, A(Color.white, i % 2 == 0 ? 0.5f : 0.28f), 0.1f, 0.1f);
                }
            }
            // 瞳孔：外面一圈柔和的暗晕
            if (_fine) Fill(Oval(ex, ey + 0.6f, 2.8f, 3.7f, 24), A(IrisRim, 0.5f));
            Fill(Oval(ex, ey + 0.5f, 1.9f, 2.8f, 22), new Color(0.10f, 0.07f, 0.17f, 1f));
            if (_rich) Fill(Oval(ex, ey + 1.5f, 1.05f, 1.3f, 14), new Color(0.24f, 0.17f, 0.38f, 1f));
            // 上眼睑压在虹膜上的暗影
            if (_fine) Fill(new Pts().M(ex - 5.2f, ey - 2.6f).Q(ex, ey - 0.9f, ex + 5.2f, ey - 2.6f).Q(ex + 4f, ey - 6.6f, ex, ey - 6.6f).Q(ex - 4f, ey - 6.6f, ex - 5.2f, ey - 2.6f), A(IrisRim, 0.55f));
            // 高光：一颗带光晕的大星、一个圆点、一颗小星
            // 星形高光不跟着压扁
            _sqK = 1f;
            Fill(Star(ex - 2.3f, EyePivot + (ey - 2.6f - EyePivot) * EyeSquash, 2.5f, 0.3f), Color.white);
            _sqK = EyeSquash;
            if (_fine) Fill(Oval(ex + 2.5f, ey + 3.3f, 0.9f, 0.9f, 12), Color.white);
            if (_rich)
            {
                _sqK = 1f;
                Fill(Star(ex + 3f, EyePivot + (ey - 3.3f - EyePivot) * EyeSquash, 1.05f, 0.34f), A(Color.white, 0.95f));
                _sqK = EyeSquash;
                Fill(Oval(ex + 1.1f, ey + 4.6f, 0.4f, 0.4f, 8), A(Color.white, 0.85f));
            }
            // 上睫毛：内眼角细、往眼尾渐厚，眼尾翘起
            Fill(new Pts().M(ex - d * 7.5f, ey - 0.6f).Q(ex - d * 5.6f, ey - 8.3f, ex + d * 0.6f, ey - 8.8f).Q(ex + d * 6.8f, ey - 8.7f, ex + d * 9.3f, ey - 3.4f)
                .To(ex + d * 7.6f, ey - 2f).Q(ex + d * 5.6f, ey - 6.2f, ex + d * 0.6f, ey - 6.7f).Q(ex - d * 4.6f, ey - 7.3f, ex - d * 7.1f, ey - 0.3f), Line);
            Fill(new Pts().M(ex + d * 6.6f, ey - 6.4f).Q(ex + d * 9f, ey - 6.5f, ex + d * 11f, ey - 7.9f).Q(ex + d * 10.2f, ey - 4.9f, ex + d * 8.3f, ey - 3.6f), Line);
            if (!_fine) return;
            Fill(new Pts().M(ex + d * 8f, ey - 4.2f).Q(ex + d * 9.8f, ey - 3.9f, ex + d * 11f, ey - 4.7f).Q(ex + d * 10.1f, ey - 2.3f, ex + d * 8.5f, ey - 2.2f), Line);
            Fill(new Pts().M(ex - d * 6.3f, ey - 4.2f).Q(ex - d * 7.3f, ey - 5.4f, ex - d * 7.6f, ey - 6.6f).Q(ex - d * 6f, ey - 6f, ex - d * 5.2f, ey - 5.7f), Line);
            // 睫毛上的一线反光
            if (_rich) Pen(new Pts().M(ex - d * 1.6f, ey - 7.8f).Q(ex + d * 3.6f, ey - 8f, ex + d * 7.2f, ey - 5.2f), 0.36f, A(HairLight, 0.75f));
            // 眼尾、下眼线、下睫毛、双眼皮
            Pen(new Pts().M(ex + d * 8f, ey - 2.4f).Q(ex + d * 7.9f, ey + 2.2f, ex + d * 6.3f, ey + 5f), 0.5f, Line, 0.9f, 0.1f);
            Pen(new Pts().M(ex - d * 2.2f, ey + 7.6f).Q(ex + d * 2.8f, ey + 8f, ex + d * 5.7f, ey + 6f), 0.55f, Line);
            if (_rich)
            {
                Pen(new Pts().M(ex + d * 5.2f, ey + 6.5f).To(ex + d * 6.3f, ey + 7.6f), 0.36f, Line, 0.9f, 0.1f);
                Pen(new Pts().M(ex + d * 3.8f, ey + 7.4f).To(ex + d * 4.5f, ey + 8.5f), 0.32f, Line, 0.9f, 0.1f);
            }
            Pen(new Pts().M(ex - d * 4.4f, ey - 10.1f).Q(ex + d * 1.6f, ey - 11.4f, ex + d * 7f, ey - 9f), 0.42f, A(Line, 0.6f));
        }

        private void DrawMouthAndBlush()
        {
            foreach (int d in new[] { -1, 1 })
            {
                Fill(Oval(50 + d * 13.4f, 69.4f, 3.6f, 2f, 22), new Color(1f, 0.62f, 0.66f, 0.16f));
                Fill(Oval(50 + d * 13.4f, 69.4f, 2.8f, 1.5f, 20), new Color(1f, 0.58f, 0.63f, 0.20f));
                Fill(Oval(50 + d * 13.4f, 69.4f, 1.9f, 0.95f, 16), new Color(1f, 0.55f, 0.60f, 0.20f));
                if (!_rich) continue;
                for (int i = -1; i <= 1; i++)
                    Pen(new Pts().M(50 + d * 13.4f + i * 1.3f + 0.4f, 68.6f).To(50 + d * 13.4f + i * 1.3f - 0.4f, 70.2f), 0.3f, new Color(0.95f, 0.45f, 0.52f, 0.7f));
            }
            // 鼻子
            if (_fine) Fill(Oval(50, 69.4f, 0.42f, 0.36f, 8), new Color(0.84f, 0.58f, 0.56f, 1f));
            // 小小的张口笑
            var mouth = new Pts().M(47.6f, 72.3f).Q(50, 73.3f, 52.4f, 72.3f).Q(52f, 75.8f, 50, 76f).Q(48f, 75.8f, 47.6f, 72.3f);
            Fill(mouth, new Color(0.84f, 0.38f, 0.45f, 1f));
            if (_fine) Fill(new Pts().M(48.7f, 75.1f).Q(50, 74.1f, 51.3f, 75.1f).Q(50, 76.2f, 48.7f, 75.1f), new Color(1f, 0.70f, 0.72f, 1f));
            Stroke(mouth, 0.5f, Line, true);
            if (_fine) Pen(new Pts().M(46.8f, 71.9f).Q(50, 73.5f, 53.2f, 71.9f), 0.6f, Line);
        }

        // ---------------------------------------------------------------- 前发
        // 刘海下缘：发尖(T) 和 发簇之间的凹口(V) 交替，从右到左
        private static readonly float[] FringePts =
        {
            73.2f, 45.5f, 68.8f, 53.6f, 66.2f, 44.6f, 62.2f, 52f, 59.6f, 43.2f, 55.4f, 50.4f,
            50.2f, 40.6f,
            45f, 50.6f, 41.2f, 43.2f, 38f, 52f, 34.4f, 44.6f, 31.4f, 53.6f, 27f, 45.5f, 23.4f, 61f,
        };

        private static Pts Fringe()
        {
            var f = new Pts().M(20.5f, 46).C(16, 6, 84, 6, 79.5f, 46).Q(79.4f, 54, 76.6f, 61f);
            float px = 76.6f, py = 61f; bool tip = true;
            for (int i = 0; i < FringePts.Length; i += 2)
            {
                float x = FringePts[i], y = FringePts[i + 1];
                if (tip) f.Q(px + (x - px) * 0.55f, py - (py - y) * 0.3f, x, y);      // 发尖 → 凹口
                else f.Q(x - (x - px) * 0.12f, py + (y - py) * 0.7f, x, y);          // 凹口 → 发尖
                px = x; py = y; tip = !tip;
            }
            return f.Q(20.6f, 54, 20.5f, 46);
        }

        private void DrawFrontHair()
        {
            // 垂在脸侧的鬓发
            foreach (int d in new[] { -1, 1 })
            {
                var lk = new Pts().M(50 + d * 24f, 44).C(50 + d * 31.5f, 54, 50 + d * 26.5f, 68, 50 + d * 30f, 80)
                    .C(50 + d * 31f, 86, 50 + d * 28.6f, 91, 50 + d * 26f, 95.5f)
                    .C(50 + d * 26.6f, 86, 50 + d * 23f, 78, 50 + d * 24.4f, 68).C(50 + d * 25.2f, 60, 50 + d * 22.6f, 54, 50 + d * 23f, 46);
                Shape(lk, Hair, 0.62f);
                if (!_fine) continue;
                Fill(new Pts().M(50 + d * 27.4f, 56).C(50 + d * 29f, 62, 50 + d * 27.4f, 68, 50 + d * 28.6f, 75).C(50 + d * 26.4f, 68, 50 + d * 27.4f, 62, 50 + d * 27.4f, 56), A(HairLight, 0.8f));
                Pen(new Pts().M(50 + d * 25.4f, 52).C(50 + d * 27.4f, 62, 50 + d * 24.8f, 74, 50 + d * 27.2f, 90), 0.4f, A(Line, 0.75f));
            }

            var fringe = Fringe();
            Fill(fringe, Hair);
            if (_fine)
            {
                // 每簇发片靠里一侧压一条暗面
                for (int i = 0; i + 3 < FringePts.Length; i += 4)
                {
                    float vx = FringePts[i], vy = FringePts[i + 1], tx = FringePts[i + 2], ty = FringePts[i + 3];
                    if (Mathf.Abs(vx - 50.2f) < 0.1f) continue;
                    Fill(new Pts().M(vx, vy).Q(tx - (tx - vx) * 0.12f, vy + (ty - vy) * 0.7f, tx, ty).Q(vx + (tx - vx) * 0.2f, vy + (ty - vy) * 0.2f, vx + (tx - vx) * 0.1f, vy - 6f), A(HairDeep, 0.7f));
                }
                // 天使环：一排长短不一的高光细片
                float[] len = { 2.2f, 4.2f, 3f, 5.6f, 2.6f, 4.8f, 3.4f, 6.2f, 2.8f, 4.4f, 3.6f };
                for (int i = 0; i <= 24; i++)
                {
                    float x = 27.6f + i * 1.87f, off = x - 50f;
                    if (Mathf.Abs(off) < 1.2f) continue;
                    float b = 26.4f + off * off * 0.0165f, ln = len[i % len.Length];
                    Pen(new Pts().M(x - off * 0.02f, b - ln * 0.8f).To(x + off * 0.035f, b + ln * 1.1f), 0.95f, A(HairLight, 0.8f), 0.05f, 0.05f);
                }
            }
            // 只描刘海的下缘：上缘和后发的轮廓重合
            var edge = new Pts(); for (int i = 17; i < fringe.L.Count - 10; i++) edge.L.Add(fringe.L[i]);
            Stroke(edge, 0.62f, Line);

            if (_fine)
            {
                // 每簇发片一根发丝，从发旋顺到发尖；凹口处向上收一小笔
                for (int i = -2; i < FringePts.Length; i += 4)
                {
                    float tx = i < 0 ? 76.6f : FringePts[i], ty = i < 0 ? 61f : FringePts[i + 1], sgn = tx > 50 ? 1 : -1;
                    float rx = 50 + (tx - 50) * 0.16f;
                    Pen(new Pts().M(rx, 17.4f).C(50 + (tx - 50) * 0.8f, 16, tx + sgn * 2f, 34, tx - sgn * 0.7f, ty - 5f), 0.42f, A(Line, 0.8f), 0.3f, 0.05f);
                    if (!_rich) continue;
                    Pen(new Pts().M(rx + sgn * 2.4f, 19f).C(50 + (tx - 50) * 0.95f, 19, tx + sgn * 3.6f, 33, tx + sgn * 1.6f, ty - 10f), 0.3f, A(Line, 0.55f), 0.3f, 0.05f);
                    Pen(new Pts().M(tx - sgn * 2.6f, ty - 15f).Q(tx - sgn * 1.4f, ty - 9f, tx - sgn * 1.7f, ty - 4f), 0.34f, A(HairLight, 0.7f));
                }
                for (int i = 0; i < FringePts.Length; i += 4)
                {
                    float vx = FringePts[i], vy = FringePts[i + 1], sgn = vx > 50 ? 1 : -1;
                    Pen(new Pts().M(vx, vy).Q(vx - sgn * 0.6f, vy - 6f, vx - sgn * 2.6f, vy - 12f), 0.4f, A(Line, 0.85f), 0.9f, 0.05f);
                }
                // 发缝
                Pen(new Pts().M(50.2f, 40.6f).Q(49.5f, 28, 50.4f, 16.6f), 0.45f, A(Line, 0.9f), 0.9f, 0.1f);
            }

            // 眉毛：透过刘海也看得见
            foreach (int d in new[] { -1, 1 })
            {
                float ex = 50 + d * 12.3f;
                Pen(new Pts().M(ex - d * 3.4f, 53f).Q(ex + d * 0.6f, 51.8f, ex + d * 4.2f, 52.9f), 0.6f, A(Line, 0.8f), 0.5f, 0.1f);
            }

            // 呆毛
            var ahoge = new Pts().M(48.6f, 11.8f).C(43.6f, 6.4f, 49.6f, 1.8f, 56f, 3.6f).C(51.6f, 4.6f, 50f, 8f, 52.4f, 11.4f).Shift(0, 4.2f);
            Shape(ahoge, Hair, 0.6f);
            if (_fine) Pen(new Pts().M(48.4f, 12.4f).Q(47.6f, 8.6f, 51.6f, 7.2f), 0.4f, A(HairLight, 0.85f));
        }

        // ---------------------------------------------------------------- 蝴蝶结
        private void DrawBow()
        {
            var tails = new Pts().M(23.6f, 28.6f).C(18, 34, 15.4f, 40, 13.6f, 47.6f).To(18.6f, 44.4f).To(21.6f, 49.4f).C(23.2f, 41, 25.4f, 35, 28.4f, 30);
            Shape(tails, RedDeep, 0.62f);
            var loopL = new Pts().M(25.4f, 26.4f).C(19, 14, 8.6f, 13.6f, 6.6f, 22.6f).C(5.6f, 30.6f, 16, 32.6f, 25.4f, 30);
            var loopR = new Pts().M(26.6f, 26.4f).C(28, 14, 36, 9.6f, 41.6f, 14.6f).C(45.4f, 20, 36, 27.6f, 26.6f, 30);
            Shape(loopL, Red, 0.62f); Shape(loopR, Red, 0.62f);
            if (_fine)
            {
                // 环内侧的暗面、上缘的高光、褶线
                Fill(new Pts().M(25.4f, 27.4f).C(20, 23, 14, 22.6f, 10, 25.6f).C(14.6f, 27.6f, 20, 29.4f, 25.4f, 29.6f), RedDeep);
                Fill(new Pts().M(26.6f, 27.4f).C(30, 21.6f, 34.6f, 18.4f, 39, 18.6f).C(35.6f, 23, 31, 27, 26.6f, 29.6f), RedDeep);
                Fill(new Pts().M(9.6f, 19f).C(12.4f, 15.2f, 16.8f, 15.4f, 20f, 18.6f).C(16.6f, 17f, 12.8f, 17.2f, 9.6f, 19f), RedLight);
                Fill(new Pts().M(31f, 15f).C(34f, 11.8f, 37.8f, 12f, 40.2f, 14.8f).C(37.2f, 13.6f, 34f, 13.8f, 31f, 15f), RedLight);
                Pen(new Pts().M(23.8f, 26.8f).Q(17.6f, 19.4f, 11.4f, 21f), 0.36f, A(Line, 0.7f), 0.9f, 0.05f);
                Pen(new Pts().M(28.4f, 26.2f).Q(32.4f, 18.6f, 39f, 16.4f), 0.36f, A(Line, 0.7f), 0.9f, 0.05f);
                Pen(new Pts().M(25.4f, 32f).C(22.6f, 36, 21f, 40, 20f, 44.4f), 0.36f, A(Line, 0.6f), 0.9f, 0.05f);
                Fill(new Pts().M(15.6f, 43.4f).To(18.6f, 44.4f).To(17.4f, 40.6f), A(RedLight, 0.7f));
            }
            var knot = new Pts().M(23.4f, 25.6f).Q(26, 24.2f, 28.6f, 25.6f).Q(29.6f, 28.4f, 28.4f, 31).Q(26, 32.2f, 23.6f, 31).Q(22.4f, 28.4f, 23.4f, 25.6f);
            Shape(knot, Red, 0.6f);
            if (_fine)
            {
                Fill(new Pts().M(26.6f, 25f).Q(29.4f, 26f, 28.4f, 31).Q(26.6f, 32f, 25.6f, 31.6f).Q(27.6f, 28.4f, 26.6f, 25f), RedDeep);
                Fill(Oval(25f, 27.2f, 0.9f, 0.7f, 10), RedLight);
            }
        }

        // ---------------------------------------------------------------- 书形发夹
        private void DrawBookPin()
        {
            var book = new Pts().M(61.6f, 21.6f).Q(66, 21.4f, 69.6f, 24.2f).Q(73.2f, 21.4f, 77.6f, 21.6f).To(77.6f, 31.2f).Q(73.2f, 31, 69.6f, 33.8f).Q(66, 31, 61.6f, 31.2f);
            if (_fine) Fill(book.Shift(0.7f, 1.5f), A(HairDeep, 0.9f));
            Shape(book, Color.white, 0.6f);
            if (_fine) Fill(new Pts().M(69.6f, 24.2f).Q(71.4f, 22.6f, 73, 22).To(73, 31.4f).Q(71.4f, 32, 69.6f, 33.8f), PageShade);
            Stroke(new Pts().M(69.6f, 24.2f).To(69.6f, 33.8f), 0.5f, Line);
            if (!_fine) return;
            Stroke(new Pts().M(63.6f, 24.6f).To(67.6f, 25.8f), 0.45f, Teal);
            Stroke(new Pts().M(63.6f, 27f).To(67.6f, 28.2f), 0.45f, Teal);
            Stroke(new Pts().M(71.6f, 25.8f).To(75.6f, 24.6f), 0.45f, Red);
            if (!_rich) return;
            var grey = new Color(0.66f, 0.70f, 0.78f, 1f);
            Stroke(new Pts().M(63.6f, 29.2f).To(66.4f, 30f), 0.34f, grey);
            Stroke(new Pts().M(71.6f, 28.2f).To(75.6f, 27f), 0.34f, grey);
            Stroke(new Pts().M(71.6f, 30.4f).To(74.6f, 29.5f), 0.34f, grey);
        }
    }
}
