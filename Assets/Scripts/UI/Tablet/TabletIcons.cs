using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet
{
    public enum TabletIconKind { Moon, Sun, Speaker, SpeakerMuted, Bolt, Palette, Gear, Contrast, Bell, MusicNote, Search,
        Person, Tablet, Apps, Cube, Wifi, Bluetooth, Link, Globe, Sparkle, Lock, Home, Keyboard, FaceId, Shield, Battery, Heart, Sprout, Accessibility, Dots, Chat, Code,
        Train, Hourglass, Cloud, Close, Drop, Eye, Camera, Folder, Image, Flag, Ruler, Mic,
        Note, Clock, Calendar, Calc, Map, Star,
        Rails, Bulb, Save, Rain, PartlyCloudy, Bookmark, Chart, Trophy, Check, Pin, Trash }

    /// <summary>
    /// 平板系统图标：用 Painter2D 绘制的矢量图标，替代 emoji。在 24×24 的设计网格内描述，按元素尺寸缩放，颜色可随状态更新。
    /// </summary>
    /// <summary>emoji / 符号字符 → 代码绘制的矢量图标。BuildSquircleBadge 等先查这张表，查不到才退回文字。</summary>
    public static class TabletGlyphMap
    {
        private static readonly Dictionary<string, TabletIconKind> Map = new Dictionary<string, TabletIconKind>
        {
            { "👤", TabletIconKind.Person }, { "📱", TabletIconKind.Tablet }, { "🧩", TabletIconKind.Apps },
            { "📶", TabletIconKind.Wifi }, { "ᛒ", TabletIconKind.Bluetooth }, { "🔗", TabletIconKind.Link },
            { "🌐", TabletIconKind.Globe }, { "🔮", TabletIconKind.Sparkle }, { "🎨", TabletIconKind.Palette },
            { "🔒", TabletIconKind.Lock }, { "🔔", TabletIconKind.Bell }, { "🏠", TabletIconKind.Home },
            { "☀", TabletIconKind.Sun }, { "☀️", TabletIconKind.Sun }, { "🔊", TabletIconKind.Speaker },
            { "⌨", TabletIconKind.Keyboard }, { "😊", TabletIconKind.FaceId }, { "🛡", TabletIconKind.Shield },
            { "▦", TabletIconKind.Cube }, { "🔋", TabletIconKind.Battery }, { "💚", TabletIconKind.Heart },
            { "🧒", TabletIconKind.Sprout }, { "♿", TabletIconKind.Accessibility }, { "⋯", TabletIconKind.Dots },
            { "💬", TabletIconKind.Chat }, { "🛠", TabletIconKind.Code },
            { "🚂", TabletIconKind.Train }, { "🚄", TabletIconKind.Train }, { "🛤", TabletIconKind.Rails },
            { "⚡", TabletIconKind.Bolt }, { "💧", TabletIconKind.Drop }, { "👁", TabletIconKind.Eye },
            { "🏁", TabletIconKind.Flag }, { "📐", TabletIconKind.Ruler }, { "📹", TabletIconKind.Camera },
            { "⚙️", TabletIconKind.Gear }, { "⚙", TabletIconKind.Gear }, { "💡", TabletIconKind.Bulb },
            { "📁", TabletIconKind.Folder }, { "🖼", TabletIconKind.Image }, { "🎵", TabletIconKind.MusicNote },
            { "💾", TabletIconKind.Save }, { "⛅", TabletIconKind.PartlyCloudy }, { "🌧", TabletIconKind.Rain },
            { "👨‍🎓", TabletIconKind.Person }, { "👷", TabletIconKind.Person }, { "🤖", TabletIconKind.Cube },
            { "🐉", TabletIconKind.Sparkle }, { "🏛", TabletIconKind.Home },
            { "📝", TabletIconKind.Note }, { "🔖", TabletIconKind.Bookmark }, { "📊", TabletIconKind.Chart },
            { "📋", TabletIconKind.Note }, { "🔧", TabletIconKind.Code }, { "🏆", TabletIconKind.Trophy },
            { "✅", TabletIconKind.Check }, { "📌", TabletIconKind.Pin }, { "🗑", TabletIconKind.Trash },
            { "🎙", TabletIconKind.Mic }, { "⏳", TabletIconKind.Hourglass }, { "☁", TabletIconKind.Cloud },
            { "📜", TabletIconKind.Note }, { "🔍", TabletIconKind.Search },
        };

        public static bool TryGet(string glyph, out TabletIconKind kind)
        {
            kind = TabletIconKind.Dots;
            if (string.IsNullOrEmpty(glyph)) return false;
            if (Map.TryGetValue(glyph, out kind)) return true;
            return Map.TryGetValue(glyph.Replace("️", string.Empty), out kind);
        }
    }

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



                case TabletIconKind.Train:
                    RoundRectPath(p, 5f * k, 3f * k, 14f * k, 14f * k, 3.4f * k);
                    p.Stroke();
                    RoundRectPath(p, 8f * k, 6.5f * k, 8f * k, 4.5f * k, 1f * k);
                    p.Fill();
                    DrawDot(p, P(9, 14), 1.1f * k);
                    DrawDot(p, P(15, 14), 1.1f * k);
                    p.BeginPath(); p.MoveTo(P(8, 17)); p.LineTo(P(5.5f, 21)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(16, 17)); p.LineTo(P(18.5f, 21)); p.Stroke();
                    break;

                case TabletIconKind.Hourglass:
                    p.BeginPath(); p.MoveTo(P(6, 3)); p.LineTo(P(18, 3)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(6, 21)); p.LineTo(P(18, 21)); p.Stroke();
                    p.BeginPath();
                    p.MoveTo(P(7.5f, 3)); p.LineTo(P(7.5f, 7)); p.LineTo(P(12, 12)); p.LineTo(P(7.5f, 17)); p.LineTo(P(7.5f, 21));
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(P(16.5f, 3)); p.LineTo(P(16.5f, 7)); p.LineTo(P(12, 12)); p.LineTo(P(16.5f, 17)); p.LineTo(P(16.5f, 21));
                    p.Stroke();
                    FillPolygon(p, new List<Vector2> { P(9.5f, 18.5f), P(14.5f, 18.5f), P(14.5f, 20.5f), P(9.5f, 20.5f) });
                    break;

                case TabletIconKind.Cloud:
                {
                    var cloud = new List<Vector2>();
                    Vector2[] cs = { P(8, 16), P(13, 11.5f), P(17, 14.5f) };
                    float[] rs = { 4.2f, 5.6f, 4.0f };
                    for (int c = 0; c < 3; c++)
                    {
                        for (int i = 0; i <= 14; i++)
                        {
                            float ang = Mathf.PI + i * Mathf.PI / 14f;
                            cloud.Add(cs[c] + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rs[c] * k);
                        }
                    }
                    cloud.Add(P(21, 18.5f)); cloud.Add(P(3.8f, 18.5f));
                    FillPolygon(p, cloud);
                    break;
                }

                case TabletIconKind.Close:
                    p.BeginPath(); p.MoveTo(P(6, 6)); p.LineTo(P(18, 18)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(18, 6)); p.LineTo(P(6, 18)); p.Stroke();
                    break;

                case TabletIconKind.Drop:
                {
                    var drop = new List<Vector2> { P(12, 2.5f) };
                    for (int i = 0; i <= 16; i++)
                    {
                        float ang = -0.9f + i * (Mathf.PI + 1.8f) / 16f;
                        drop.Add(P(12, 15) + new Vector2(Mathf.Sin(ang), Mathf.Cos(ang)) * 6.2f * k);
                    }
                    FillPolygon(p, drop);
                    break;
                }

                case TabletIconKind.Eye:
                {
                    var eye = new List<Vector2>();
                    for (int i = 0; i <= 16; i++) { float t = i / 16f; eye.Add(P(2.5f + 19f * t, 12 - 6.5f * Mathf.Sin(t * Mathf.PI))); }
                    for (int i = 16; i >= 0; i--) { float t = i / 16f; eye.Add(P(2.5f + 19f * t, 12 + 6.5f * Mathf.Sin(t * Mathf.PI))); }
                    p.BeginPath();
                    p.MoveTo(eye[0]);
                    for (int i = 1; i < eye.Count; i++) p.LineTo(eye[i]);
                    p.ClosePath();
                    p.Stroke();
                    DrawDot(p, P(12, 12), 3f * k);
                    break;
                }

                case TabletIconKind.Camera:
                    RoundRectPath(p, 3f * k, 7f * k, 18f * k, 12f * k, 2.8f * k);
                    p.Stroke();
                    p.BeginPath(); p.Arc(P(12, 13), 3.4f * k, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(8.5f, 7)); p.LineTo(P(10, 4.5f)); p.LineTo(P(14, 4.5f)); p.LineTo(P(15.5f, 7)); p.Stroke();
                    break;

                case TabletIconKind.Folder:
                    FillPolygon(p, new List<Vector2> { P(3, 6.5f), P(9, 6.5f), P(11, 9), P(21, 9), P(21, 19), P(3, 19) });
                    break;

                case TabletIconKind.Image:
                    RoundRectPath(p, 3f * k, 4f * k, 18f * k, 16f * k, 2.6f * k);
                    p.Stroke();
                    DrawDot(p, P(8.5f, 9.5f), 1.7f * k);
                    FillPolygon(p, new List<Vector2> { P(4, 18.5f), P(10, 12.5f), P(14, 16.5f), P(16.5f, 14), P(20, 18.5f) });
                    break;

                case TabletIconKind.Flag:
                    p.BeginPath(); p.MoveTo(P(6, 21)); p.LineTo(P(6, 3)); p.Stroke();
                    FillPolygon(p, new List<Vector2> { P(6, 4), P(19, 4), P(15.5f, 8.5f), P(19, 13), P(6, 13) });
                    break;

                case TabletIconKind.Ruler:
                    p.BeginPath(); p.MoveTo(P(3.5f, 18)); p.LineTo(P(20.5f, 6)); p.Stroke();
                    break;

                case TabletIconKind.Mic:
                    RoundRectPath(p, 9f * k, 3f * k, 6f * k, 11f * k, 3f * k);
                    p.Fill();
                    p.BeginPath(); p.Arc(P(12, 11.5f), 6.5f * k, Angle.Degrees(0), Angle.Degrees(180)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(12, 18)); p.LineTo(P(12, 21)); p.Stroke();
                    break;


                case TabletIconKind.Note:
                    RoundRectPath(p, 5f * k, 3f * k, 14f * k, 18f * k, 2.6f * k);
                    p.Stroke();
                    p.BeginPath(); p.MoveTo(P(8.5f, 8.5f)); p.LineTo(P(15.5f, 8.5f)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(8.5f, 12.5f)); p.LineTo(P(15.5f, 12.5f)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(8.5f, 16.5f)); p.LineTo(P(12.5f, 16.5f)); p.Stroke();
                    break;

                case TabletIconKind.Clock:
                    p.BeginPath(); p.Arc(P(12, 12), 9f * k, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(12, 6.5f)); p.LineTo(P(12, 12)); p.LineTo(P(16, 14.5f)); p.Stroke();
                    break;

                case TabletIconKind.Calendar:
                    RoundRectPath(p, 3.5f * k, 5f * k, 17f * k, 15.5f * k, 2.6f * k);
                    p.Stroke();
                    p.BeginPath(); p.MoveTo(P(3.5f, 10)); p.LineTo(P(20.5f, 10)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(8, 3)); p.LineTo(P(8, 6.5f)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(16, 3)); p.LineTo(P(16, 6.5f)); p.Stroke();
                    DrawDot(p, P(8.5f, 14.5f), 1.1f * k);
                    DrawDot(p, P(12, 14.5f), 1.1f * k);
                    DrawDot(p, P(15.5f, 14.5f), 1.1f * k);
                    break;

                case TabletIconKind.Calc:
                    RoundRectPath(p, 5f * k, 3f * k, 14f * k, 18f * k, 2.6f * k);
                    p.Stroke();
                    p.BeginPath(); p.MoveTo(P(8, 8)); p.LineTo(P(16, 8)); p.Stroke();
                    for (int r = 0; r < 3; r++)
                        for (int c = 0; c < 3; c++)
                            DrawDot(p, P(8.5f + c * 3.5f, 12.5f + r * 3.2f), 0.95f * k);
                    break;

                case TabletIconKind.Map:
                {
                    p.BeginPath();
                    p.MoveTo(P(3, 6)); p.LineTo(P(9, 4)); p.LineTo(P(15, 6)); p.LineTo(P(21, 4));
                    p.LineTo(P(21, 18)); p.LineTo(P(15, 20)); p.LineTo(P(9, 18)); p.LineTo(P(3, 20));
                    p.ClosePath();
                    p.Stroke();
                    p.BeginPath(); p.MoveTo(P(9, 4)); p.LineTo(P(9, 18)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(15, 6)); p.LineTo(P(15, 20)); p.Stroke();
                    break;
                }

                case TabletIconKind.Star:
                {
                    var star = new List<Vector2>();
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                        float rad = (i % 2 == 0 ? 9.5f : 4.1f) * k;
                        star.Add(P(12, 12.6f) + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad);
                    }
                    FillPolygon(p, star);
                    break;
                }


                case TabletIconKind.Rails:
                    p.BeginPath(); p.MoveTo(P(8, 3)); p.LineTo(P(5, 21)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(16, 3)); p.LineTo(P(19, 21)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(7, 7)); p.LineTo(P(17, 7)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(6.4f, 12)); p.LineTo(P(17.6f, 12)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(5.7f, 17)); p.LineTo(P(18.3f, 17)); p.Stroke();
                    break;

                case TabletIconKind.Bulb:
                    p.BeginPath(); p.Arc(P(12, 10), 6f * k, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(9.5f, 17.5f)); p.LineTo(P(14.5f, 17.5f)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(10.2f, 20.5f)); p.LineTo(P(13.8f, 20.5f)); p.Stroke();
                    break;

                case TabletIconKind.Save:
                    RoundRectPath(p, 3.5f * k, 3.5f * k, 17f * k, 17f * k, 2.4f * k);
                    p.Stroke();
                    RoundRectPath(p, 7.5f * k, 3.5f * k, 9f * k, 6f * k, 0.8f * k);
                    p.Fill();
                    RoundRectPath(p, 7.5f * k, 13f * k, 9f * k, 7.5f * k, 1f * k);
                    p.Stroke();
                    break;

                case TabletIconKind.Rain:
                {
                    var rc = new List<Vector2>();
                    Vector2[] rcs = { P(8, 11), P(13, 7.5f), P(17, 10.5f) };
                    float[] rrs = { 3.6f, 4.8f, 3.4f };
                    for (int c = 0; c < 3; c++)
                        for (int i = 0; i <= 14; i++)
                        {
                            float ang = Mathf.PI + i * Mathf.PI / 14f;
                            rc.Add(rcs[c] + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rrs[c] * k);
                        }
                    rc.Add(P(20.4f, 13.5f)); rc.Add(P(4.4f, 13.5f));
                    FillPolygon(p, rc);
                    p.BeginPath(); p.MoveTo(P(8, 16)); p.LineTo(P(7, 20)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(12.5f, 16)); p.LineTo(P(11.5f, 20)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(17, 16)); p.LineTo(P(16, 20)); p.Stroke();
                    break;
                }

                case TabletIconKind.PartlyCloudy:
                {
                    DrawDot(p, P(16, 8), 3.4f * k);
                    var pc = new List<Vector2>();
                    Vector2[] pcs = { P(7.5f, 17), P(11.5f, 13.5f), P(15, 16) };
                    float[] prs = { 3.4f, 4.4f, 3.2f };
                    for (int c = 0; c < 3; c++)
                        for (int i = 0; i <= 14; i++)
                        {
                            float ang = Mathf.PI + i * Mathf.PI / 14f;
                            pc.Add(pcs[c] + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * prs[c] * k);
                        }
                    pc.Add(P(18.2f, 19.5f)); pc.Add(P(4.1f, 19.5f));
                    FillPolygon(p, pc);
                    break;
                }

                case TabletIconKind.Bookmark:
                    FillPolygon(p, new List<Vector2> { P(6.5f, 3), P(17.5f, 3), P(17.5f, 21), P(12, 16.5f), P(6.5f, 21) });
                    break;

                case TabletIconKind.Chart:
                    RoundRectPath(p, 4f * k, 13f * k, 4f * k, 8f * k, 1f * k); p.Fill();
                    RoundRectPath(p, 10f * k, 8f * k, 4f * k, 13f * k, 1f * k); p.Fill();
                    RoundRectPath(p, 16f * k, 4f * k, 4f * k, 17f * k, 1f * k); p.Fill();
                    break;

                case TabletIconKind.Trophy:
                    FillPolygon(p, new List<Vector2> { P(7, 3.5f), P(17, 3.5f), P(16.2f, 10.5f), P(13, 13.5f), P(11, 13.5f), P(7.8f, 10.5f) });
                    p.BeginPath(); p.MoveTo(P(12, 13.5f)); p.LineTo(P(12, 18)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(8, 20.5f)); p.LineTo(P(16, 20.5f)); p.Stroke();
                    break;

                case TabletIconKind.Check:
                    p.BeginPath(); p.MoveTo(P(5, 12.5f)); p.LineTo(P(10, 17.5f)); p.LineTo(P(19, 7)); p.Stroke();
                    break;

                case TabletIconKind.Pin:
                    FillPolygon(p, new List<Vector2> { P(9, 3), P(15, 3), P(14, 9), P(17.5f, 13), P(6.5f, 13), P(10, 9) });
                    p.BeginPath(); p.MoveTo(P(12, 13)); p.LineTo(P(12, 21)); p.Stroke();
                    break;

                case TabletIconKind.Trash:
                    RoundRectPath(p, 6f * k, 8f * k, 12f * k, 13f * k, 2f * k);
                    p.Stroke();
                    p.BeginPath(); p.MoveTo(P(4, 6)); p.LineTo(P(20, 6)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(9, 6)); p.LineTo(P(9, 3.5f)); p.LineTo(P(15, 3.5f)); p.LineTo(P(15, 6)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(10, 11)); p.LineTo(P(10, 18)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(14, 11)); p.LineTo(P(14, 18)); p.Stroke();
                    break;

                case TabletIconKind.Person:
                {
                    DrawDot(p, P(12, 8), 4.2f * k);
                    var body = new List<Vector2>();
                    for (int i = 0; i <= 14; i++)
                    {
                        float ang = Mathf.PI + i * Mathf.PI / 14f;
                        body.Add(P(12, 21) + new Vector2(Mathf.Cos(ang) * 8.5f, Mathf.Sin(ang) * 7.5f) * k);
                    }
                    FillPolygon(p, body);
                    break;
                }

                case TabletIconKind.Tablet:
                    RoundRectPath(p, 4.5f * k, 3f * k, 15f * k, 18f * k, 3f * k);
                    p.Stroke();
                    p.BeginPath(); p.MoveTo(P(10, 17.5f)); p.LineTo(P(14, 17.5f)); p.Stroke();
                    break;

                case TabletIconKind.Apps:
                    RoundRectPath(p, 4f * k, 4f * k, 6.5f * k, 6.5f * k, 2f * k); p.Fill();
                    RoundRectPath(p, 13.5f * k, 4f * k, 6.5f * k, 6.5f * k, 2f * k); p.Fill();
                    RoundRectPath(p, 4f * k, 13.5f * k, 6.5f * k, 6.5f * k, 2f * k); p.Fill();
                    RoundRectPath(p, 13.5f * k, 13.5f * k, 6.5f * k, 6.5f * k, 2f * k); p.Fill();
                    break;

                case TabletIconKind.Cube:
                {
                    var hex = new List<Vector2> { P(12, 3), P(20, 7.5f), P(20, 16.5f), P(12, 21), P(4, 16.5f), P(4, 7.5f) };
                    p.BeginPath();
                    p.MoveTo(hex[0]);
                    for (int i = 1; i < hex.Count; i++) p.LineTo(hex[i]);
                    p.ClosePath();
                    p.Stroke();
                    p.BeginPath(); p.MoveTo(P(12, 12)); p.LineTo(P(12, 21)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(12, 12)); p.LineTo(P(4, 7.5f)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(12, 12)); p.LineTo(P(20, 7.5f)); p.Stroke();
                    break;
                }

                case TabletIconKind.Wifi:
                    DrawDot(p, P(12, 19), 1.9f * k);
                    for (int i = 0; i < 3; i++)
                    {
                        p.BeginPath();
                        p.Arc(P(12, 19), (5.2f + i * 4.2f) * k, Angle.Degrees(-135), Angle.Degrees(-45));
                        p.Stroke();
                    }
                    break;

                case TabletIconKind.Bluetooth:
                {
                    p.BeginPath();
                    p.MoveTo(P(7, 8)); p.LineTo(P(17, 16)); p.LineTo(P(12, 21)); p.LineTo(P(12, 3)); p.LineTo(P(17, 8)); p.LineTo(P(7, 16));
                    p.Stroke();
                    break;
                }

                case TabletIconKind.Link:
                    p.BeginPath(); p.Arc(P(9, 12), 5f * k, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
                    p.BeginPath(); p.Arc(P(15, 12), 5f * k, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
                    break;

                case TabletIconKind.Globe:
                {
                    p.BeginPath(); p.Arc(P(12, 12), 9f * k, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(3, 12)); p.LineTo(P(21, 12)); p.Stroke();
                    var ell = new List<Vector2>();
                    for (int i = 0; i < 28; i++)
                    {
                        float ang = i * Mathf.PI * 2f / 28f;
                        ell.Add(P(12, 12) + new Vector2(Mathf.Cos(ang) * 4.2f, Mathf.Sin(ang) * 9f) * k);
                    }
                    p.BeginPath();
                    p.MoveTo(ell[0]);
                    for (int i = 1; i < ell.Count; i++) p.LineTo(ell[i]);
                    p.ClosePath();
                    p.Stroke();
                    break;
                }

                case TabletIconKind.Sparkle:
                    FillPolygon(p, new List<Vector2> { P(12, 2), P(14.2f, 9.8f), P(22, 12), P(14.2f, 14.2f), P(12, 22), P(9.8f, 14.2f), P(2, 12), P(9.8f, 9.8f) });
                    break;

                case TabletIconKind.Lock:
                    RoundRectPath(p, 5f * k, 11f * k, 14f * k, 10f * k, 2.4f * k);
                    p.Fill();
                    p.BeginPath();
                    p.Arc(P(12, 11), 4.5f * k, Angle.Degrees(180), Angle.Degrees(360));
                    p.Stroke();
                    break;

                case TabletIconKind.Home:
                    FillPolygon(p, new List<Vector2> { P(12, 3), P(21.5f, 11.5f), P(18.5f, 11.5f), P(18.5f, 20.5f), P(5.5f, 20.5f), P(5.5f, 11.5f), P(2.5f, 11.5f) });
                    break;

                case TabletIconKind.Keyboard:
                    RoundRectPath(p, 2.5f * k, 6f * k, 19f * k, 12f * k, 2.6f * k);
                    p.Stroke();
                    for (int i = 0; i < 4; i++) DrawDot(p, P(6.5f + i * 3.6f, 10), 0.95f * k);
                    p.BeginPath(); p.MoveTo(P(7.5f, 14.3f)); p.LineTo(P(16.5f, 14.3f)); p.Stroke();
                    break;

                case TabletIconKind.FaceId:
                    p.BeginPath(); p.MoveTo(P(3, 8)); p.LineTo(P(3, 4)); p.LineTo(P(7, 4)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(17, 4)); p.LineTo(P(21, 4)); p.LineTo(P(21, 8)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(21, 16)); p.LineTo(P(21, 20)); p.LineTo(P(17, 20)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(7, 20)); p.LineTo(P(3, 20)); p.LineTo(P(3, 16)); p.Stroke();
                    DrawDot(p, P(9, 10), 1.1f * k);
                    DrawDot(p, P(15, 10), 1.1f * k);
                    p.BeginPath(); p.Arc(P(12, 12.5f), 4f * k, Angle.Degrees(35), Angle.Degrees(145)); p.Stroke();
                    break;

                case TabletIconKind.Shield:
                    FillPolygon(p, new List<Vector2> { P(12, 2.5f), P(20, 5.5f), P(19.6f, 13), P(15.5f, 19), P(12, 21.5f), P(8.5f, 19), P(4.4f, 13), P(4, 5.5f) });
                    break;

                case TabletIconKind.Battery:
                    RoundRectPath(p, 2.5f * k, 7f * k, 16.5f * k, 10f * k, 2.4f * k);
                    p.Stroke();
                    RoundRectPath(p, 20f * k, 10.5f * k, 2f * k, 3f * k, 0.8f * k); p.Fill();
                    RoundRectPath(p, 5f * k, 9.5f * k, 8.5f * k, 5f * k, 1f * k); p.Fill();
                    break;

                case TabletIconKind.Heart:
                {
                    var heart = new List<Vector2>();
                    for (int i = 0; i < 40; i++)
                    {
                        float t = i * Mathf.PI * 2f / 40f;
                        float hx = 16f * Mathf.Pow(Mathf.Sin(t), 3);
                        float hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                        heart.Add(P(12, 11) + new Vector2(hx, -hy) * 0.55f * k);
                    }
                    FillPolygon(p, heart);
                    break;
                }

                case TabletIconKind.Sprout:
                    p.BeginPath(); p.MoveTo(P(12, 21)); p.LineTo(P(12, 11)); p.Stroke();
                    FillPolygon(p, new List<Vector2> { P(12, 12), P(6.5f, 11.5f), P(4.5f, 6), P(11, 8) });
                    FillPolygon(p, new List<Vector2> { P(12, 11), P(17.5f, 10.5f), P(20, 4.5f), P(13, 7) });
                    break;

                case TabletIconKind.Accessibility:
                    DrawDot(p, P(12, 5), 2.3f * k);
                    p.BeginPath(); p.MoveTo(P(4, 9)); p.LineTo(P(20, 9)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(12, 9)); p.LineTo(P(12, 15)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(12, 15)); p.LineTo(P(8, 21)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(12, 15)); p.LineTo(P(16, 21)); p.Stroke();
                    break;

                case TabletIconKind.Dots:
                    DrawDot(p, P(5, 12), 2.1f * k);
                    DrawDot(p, P(12, 12), 2.1f * k);
                    DrawDot(p, P(19, 12), 2.1f * k);
                    break;

                case TabletIconKind.Chat:
                    RoundRectPath(p, 3f * k, 4f * k, 18f * k, 13f * k, 3.4f * k);
                    p.Stroke();
                    p.BeginPath(); p.MoveTo(P(8.5f, 17)); p.LineTo(P(8.5f, 21)); p.LineTo(P(13, 17)); p.Stroke();
                    DrawDot(p, P(8, 10.5f), 1.15f * k);
                    DrawDot(p, P(12, 10.5f), 1.15f * k);
                    DrawDot(p, P(16, 10.5f), 1.15f * k);
                    break;

                case TabletIconKind.Code:
                    p.BeginPath(); p.MoveTo(P(9, 7)); p.LineTo(P(4, 12)); p.LineTo(P(9, 17)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(15, 7)); p.LineTo(P(20, 12)); p.LineTo(P(15, 17)); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(13.5f, 5)); p.LineTo(P(10.5f, 19)); p.Stroke();
                    break;

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


        // 圆角矩形路径（坐标已按网格缩放），由调用方 Fill 或 Stroke
        private static void RoundRectPath(Painter2D p, float x, float y, float w, float h, float r)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x + r, y));
            p.LineTo(new Vector2(x + w - r, y));
            p.ArcTo(new Vector2(x + w, y), new Vector2(x + w, y + r), r);
            p.LineTo(new Vector2(x + w, y + h - r));
            p.ArcTo(new Vector2(x + w, y + h), new Vector2(x + w - r, y + h), r);
            p.LineTo(new Vector2(x + r, y + h));
            p.ArcTo(new Vector2(x, y + h), new Vector2(x, y + h - r), r);
            p.LineTo(new Vector2(x, y + r));
            p.ArcTo(new Vector2(x, y), new Vector2(x + r, y), r);
            p.ClosePath();
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
