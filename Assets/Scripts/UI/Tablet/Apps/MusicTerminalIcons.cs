using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // Native vector geometry: no font glyphs, emoji, external icon font or bitmap scaling.
    public static class MusicTerminalIcons
    {
        public static void Apply(Button button, string action, bool iconOnly = false)
        {
            string key = action + (iconOnly ? ":icon" : ":label");
            if (Equals(button.userData, key)) return;
            button.userData = key; button.tooltip = action; button.text = ""; button.Clear();
            button.EnableInClassList("music-icon-button", iconOnly);
            button.EnableInClassList("music-icon-active", action == "已藏" || action == "随机" || action == "单曲");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("music-vector-icon");
            icon.generateVisualContent += context => Draw(context.painter2D, action, icon.resolvedStyle.color);
            button.Add(icon);
            if (!iconOnly)
            {
                var label = new Label(action) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("music-button-label"); button.Add(label);
            }
        }
        static void Draw(Painter2D p, string action, Color ink)
        {
            p.strokeColor = ink; p.fillColor = ink; p.lineWidth = 1.6f;
            p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            void Line(params Vector2[] points)
            {
                p.BeginPath(); p.MoveTo(points[0]);
                for (int i = 1; i < points.Length; i++) p.LineTo(points[i]);
                p.Stroke();
            }
            void Circle(float x, float y, float radius, bool fill = false)
            {
                p.BeginPath(); p.Arc(new Vector2(x, y), radius, 0, 360); p.ClosePath(); if (fill) p.Fill(); else p.Stroke();
            }
            void Rect(float x, float y, float w, float h) => Line(new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h), new Vector2(x, y));
            void Arrow(float x, bool right)
            {
                float d = right ? 1 : -1;
                Line(new Vector2(x - d * 3, 5), new Vector2(x + d * 3, 10), new Vector2(x - d * 3, 15));
            }
            if (action == "播放" || action == "播放全部" || action == "播放这首")
            {
                p.BeginPath(); p.MoveTo(new Vector2(6, 3)); p.LineTo(new Vector2(16, 10)); p.LineTo(new Vector2(6, 17)); p.ClosePath(); p.Fill();
            }
            else if (action == "暂停") { Rect(5, 4, 3, 12); Rect(12, 4, 3, 12); }
            else if (action == "上一首" || action == "下一首")
            {
                bool next = action == "下一首"; Arrow(next ? 8 : 12, next); float x = next ? 16 : 4; Line(new Vector2(x, 4), new Vector2(x, 16));
            }
            else if (action == "收藏" || action == "已藏")
            {
                p.BeginPath(); p.MoveTo(new Vector2(10, 17));
                p.BezierCurveTo(new Vector2(-1, 10), new Vector2(1, 0), new Vector2(10, 6));
                p.BezierCurveTo(new Vector2(19, 0), new Vector2(21, 10), new Vector2(10, 17));
                p.ClosePath(); if (action == "已藏") p.Fill(); else p.Stroke();
            }
            else if (action.Contains("队列") || action == "下一首播放")
            {
                Line(new Vector2(3, 5), new Vector2(15, 5)); Line(new Vector2(3, 10), new Vector2(12, 10)); Line(new Vector2(3, 15), new Vector2(10, 15));
                Line(new Vector2(15, 12), new Vector2(18, 15), new Vector2(15, 18));
            }
            else if (action == "随机")
            {
                Line(new Vector2(2, 5), new Vector2(5, 5), new Vector2(14, 15), new Vector2(18, 15));
                Line(new Vector2(2, 15), new Vector2(5, 15), new Vector2(14, 5), new Vector2(18, 5));
                Line(new Vector2(15, 2), new Vector2(18, 5), new Vector2(15, 8));
            }
            else if (action == "顺序" || action == "单曲")
            {
                Line(new Vector2(4, 10), new Vector2(4, 5), new Vector2(16, 5));
                Line(new Vector2(13, 2), new Vector2(16, 5), new Vector2(13, 8));
                Line(new Vector2(16, 10), new Vector2(16, 15), new Vector2(4, 15));
                Line(new Vector2(7, 12), new Vector2(4, 15), new Vector2(7, 18));
                if (action == "单曲") Line(new Vector2(9, 9), new Vector2(11, 8), new Vector2(11, 12));
            }
            else if (action == "专注阅读" || action == "显示设置")
            {
                Line(new Vector2(3, 8), new Vector2(3, 3), new Vector2(8, 3));
                Line(new Vector2(12, 3), new Vector2(17, 3), new Vector2(17, 8));
                Line(new Vector2(3, 12), new Vector2(3, 17), new Vector2(8, 17));
                Line(new Vector2(12, 17), new Vector2(17, 17), new Vector2(17, 12));
                if (action == "显示设置") { Line(new Vector2(7, 8), new Vector2(13, 8)); Line(new Vector2(7, 12), new Vector2(13, 12)); }
            }
            else if (action == "静读" || action == "随行" || action == "舞台")
            {
                Line(new Vector2(3, 16), new Vector2(17, 16));
                Line(new Vector2(5, 12), new Vector2(15, 12));
                if (action == "静读") Line(new Vector2(5, 7), new Vector2(15, 7));
                else { Line(new Vector2(10, 3), new Vector2(10, 9)); Line(new Vector2(7, 6), new Vector2(10, 3), new Vector2(13, 6)); }
                if (action == "舞台") { Line(new Vector2(3, 3), new Vector2(4, 6)); Line(new Vector2(17, 3), new Vector2(16, 6)); }
            }
            else if (action.Contains("字幕") || action.Contains("翻译"))
            { Rect(2, 4, 16, 12); Line(new Vector2(5, 9), new Vector2(8, 9)); Line(new Vector2(11, 9), new Vector2(15, 9)); Line(new Vector2(5, 13), new Vector2(12, 13)); }
            else if (action.Contains("100ms"))
            { Arrow(action.Contains("提前") ? 13 : 7, action.Contains("提前")); Line(new Vector2(3, 10), new Vector2(17, 10)); }
            else if (action == "操作帮助")
            { Circle(10, 10, 8); Line(new Vector2(7, 7), new Vector2(9, 5), new Vector2(13, 6), new Vector2(13, 9), new Vector2(10, 11)); Circle(10, 15, .7f, true); }
            else if (action == "更多") { Circle(4, 10, 1, true); Circle(10, 10, 1, true); Circle(16, 10, 1, true); }
            else if (action == "关闭") { Line(new Vector2(5, 5), new Vector2(15, 15)); Line(new Vector2(15, 5), new Vector2(5, 15)); }
            else if (action.Contains("删除") || action == "移除")
            {
                Line(new Vector2(3, 5), new Vector2(17, 5)); Rect(7, 2, 6, 3); Line(new Vector2(5, 7), new Vector2(6, 17), new Vector2(14, 17), new Vector2(15, 7));
                Line(new Vector2(8, 8), new Vector2(8, 14)); Line(new Vector2(12, 8), new Vector2(12, 14));
            }
            else if (action == "返回桌面" || action == "收起") { Arrow(7, false); Line(new Vector2(4, 10), new Vector2(17, 10)); }
            else if (action == "↑" || action == "↓" || action.Contains("导入"))
            {
                bool up = action == "↑"; float top = up ? 4 : 15, bottom = up ? 16 : 3;
                Line(new Vector2(10, bottom), new Vector2(10, top));
                Line(new Vector2(6, up ? 8 : 11), new Vector2(10, top), new Vector2(14, up ? 8 : 11));
                if (action.Contains("导入")) Line(new Vector2(3, 13), new Vector2(3, 18), new Vector2(17, 18), new Vector2(17, 13));
            }
            else if (action == "≡") { for (int y = 5; y <= 15; y += 5) { Circle(7, y, .8f, true); Circle(13, y, .8f, true); } }
            else if (action.Contains("保存")) Line(new Vector2(3, 10), new Vector2(8, 15), new Vector2(17, 5));
            else if (action.Contains("新建")) { Line(new Vector2(10, 3), new Vector2(10, 17)); Line(new Vector2(3, 10), new Vector2(17, 10)); }
            else if (action == "重命名") { Line(new Vector2(4, 13), new Vector2(14, 3), new Vector2(17, 6), new Vector2(7, 16), new Vector2(3, 17), new Vector2(4, 13)); }
            else if (action == "刷新文件") { p.BeginPath(); p.Arc(new Vector2(10, 10), 6, 30, 310); p.Stroke(); Line(new Vector2(12, 2), new Vector2(16, 5), new Vector2(12, 6)); }
            else if (action.Contains("文件夹")) { Line(new Vector2(2, 16), new Vector2(2, 4), new Vector2(8, 4), new Vector2(10, 7), new Vector2(18, 7), new Vector2(18, 16), new Vector2(2, 16)); }
            else if (action == "定时" || action == "加载中") { Circle(10, 10, 7); Line(new Vector2(10, 5), new Vector2(10, 10), new Vector2(14, 12)); }
            else if (action == "音色")
            {
                for (int x = 4; x <= 16; x += 6) { Line(new Vector2(x, 3), new Vector2(x, 17)); Circle(x, x == 10 ? 13 : 7, 2); }
            }
            else if (action == "本地音乐") { Rect(2, 4, 16, 12); Circle(6, 9, 2); Circle(14, 9, 2); Line(new Vector2(5, 14), new Vector2(15, 14)); }
            else if (action == "云端")
            {
                p.BeginPath(); p.MoveTo(new Vector2(4, 15)); p.BezierCurveTo(new Vector2(-2, 12), new Vector2(2, 6), new Vector2(6, 8));
                p.BezierCurveTo(new Vector2(6, 0), new Vector2(17, 0), new Vector2(17, 9)); p.BezierCurveTo(new Vector2(22, 9), new Vector2(21, 15), new Vector2(17, 15)); p.ClosePath(); p.Stroke();
            }
            else if (action.Contains("原声")) { Rect(3, 3, 5, 14); Rect(11, 3, 5, 14); Line(new Vector2(5, 6), new Vector2(6, 6)); Line(new Vector2(13, 6), new Vector2(14, 6)); }
            else { Circle(10, 10, 7); Circle(10, 10, 2); Line(new Vector2(4, 10), new Vector2(7, 10)); }
        }
    }
}
