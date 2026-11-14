using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using static RailwayRenaissance.UI.Tablet.Paper.PaperTokens;

namespace RailwayRenaissance.UI.Tablet.Paper
{
    /// <summary>「站台纸本」组件工厂：全部返回 UI Toolkit 元素，样式只引用 PaperTokens，装饰由 PaperDraw 精绘。</summary>
    public static class PaperUI
    {
        public enum ButtonKind { Primary, Secondary, Ghost, Danger }

        // ================= 小工具 =================
        public static void Round(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        public static void Border(VisualElement e, float w, Color c)
        {
            e.style.borderTopWidth = w; e.style.borderBottomWidth = w; e.style.borderLeftWidth = w; e.style.borderRightWidth = w;
            e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c;
        }

        public static void Pad(VisualElement e, float x, float y)
        {
            e.style.paddingLeft = x; e.style.paddingRight = x; e.style.paddingTop = y; e.style.paddingBottom = y;
        }

        public static void Fade(VisualElement e, float seconds = FadeNormal)
        {
            e.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
            e.style.transitionDuration = new List<TimeValue> { new TimeValue(TabletTheme.ReduceMotion ? 0f : seconds, TimeUnit.Second) };
        }

        public static bool IsShown(VisualElement o) => o.style.display == DisplayStyle.Flex && o.style.opacity.value > 0.5f;

        public static void Show(VisualElement o)
        {
            o.style.display = DisplayStyle.Flex; o.style.opacity = 0f;
            o.schedule.Execute(() => o.style.opacity = 1f).ExecuteLater(10);
        }

        public static void Hide(VisualElement o)
        {
            if (o.style.display == DisplayStyle.None) return;
            o.style.opacity = 0f;
            o.schedule.Execute(() => { if (o.style.opacity.value < 0.05f) o.style.display = DisplayStyle.None; }).ExecuteLater(220);
        }

        public static Label Text(string text, int size = FsBody, Color? color = null, bool bold = false)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.fontSize = size; l.style.color = color ?? Ink;
            l.style.unityFontDefinition = bold ? TabletUIHelper.FdBold() : TabletUIHelper.Fd();
            return l;
        }

        // ================= 面板 / 卡片 =================
        /// <summary>给已有元素套上纸面面板样式（底色 + 描边 + 精绘装饰）。</summary>
        public static void StylePanel(VisualElement e, float radius, float padX, float padY, bool glass = true, bool rivets = false)
        {
            e.style.backgroundColor = glass ? PaperGlass : PaperBg;
            Pad(e, padX, padY);
            Round(e, radius);
            Border(e, 1f, Line);
            e.generateVisualContent += ctx => PaperDraw.Deco(ctx.painter2D, e.layout.width, e.layout.height, radius, rivets);
        }

        public static VisualElement Panel(float radius = RadiusL, float padX = S3, float padY = S2, bool glass = false, bool rivets = false)
        {
            var e = new VisualElement();
            StylePanel(e, radius, padX, padY, glass, rivets);
            return e;
        }

        /// <summary>内容卡片：比面板更亮一档（Card 色），用于页面内部分组。</summary>
        public static VisualElement Card(float pad = S3, bool selected = false, Color? accent = null)
        {
            var e = new VisualElement();
            e.style.backgroundColor = selected ? Color.Lerp(PaperTokens.Card, accent ?? Amber, 0.12f) : PaperTokens.Card;
            Pad(e, pad, pad);
            Round(e, RadiusM);
            Border(e, selected ? 1.6f : 1f, selected ? (accent ?? Amber) : LineSoft);
            e.generateVisualContent += ctx => PaperDraw.Deco(ctx.painter2D, e.layout.width, e.layout.height, RadiusM);
            return e;
        }

        /// <summary>车票：左右缺口 + 距右 stub 宽度处的穿孔撕口。back 为缺口露出的底色。</summary>
        public static VisualElement Ticket(float stub, Color back, Color? accent = null)
        {
            var e = Card(S3, false, accent);
            e.generateVisualContent += ctx =>
            {
                float w = e.layout.width, h = e.layout.height;
                PaperDraw.Perforation(ctx.painter2D, new Vector2(w - stub, 10f), new Vector2(w - stub, h - 10f), Line);
                PaperDraw.TicketNotch(ctx.painter2D, w, h, 9f, back, LineSoft);
            };
            return e;
        }

        // ================= 按钮 =================
        public static VisualElement IconButton(TabletIconKind icon, string tip, Action click, float size = 52f, Color? tint = null)
        {
            var b = new VisualElement { tooltip = tip };
            b.style.width = size; b.style.height = size;
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            StylePanel(b, size / 2f, 0, 0);
            b.Add(new TabletIconElement(icon, size * 0.5f, tint ?? Ink));
            b.RegisterCallback<ClickEvent>(_ => click?.Invoke());
            b.RegisterCallback<PointerEnterEvent>(_ => b.style.backgroundColor = new Color(0.93f, 0.88f, 0.78f, 0.97f));
            b.RegisterCallback<PointerLeaveEvent>(_ => b.style.backgroundColor = PaperGlass);
            return b;
        }

        public static VisualElement Button(string text, ButtonKind kind, Action click, TabletIconKind? icon = null, float height = 44f)
        {
            Color bg, fg, bgHover;
            switch (kind)
            {
                case ButtonKind.Primary: bg = new Color(0.86f, 0.52f, 0.18f); bgHover = new Color(0.92f, 0.58f, 0.22f); fg = Color.white; break;
                case ButtonKind.Danger: bg = Bad; bgHover = new Color(0.82f, 0.28f, 0.22f); fg = Color.white; break;
                case ButtonKind.Secondary: bg = PaperDeep; bgHover = new Color(0.90f, 0.85f, 0.74f); fg = Ink; break;
                default: bg = new Color(0, 0, 0, 0); bgHover = new Color(0.45f, 0.32f, 0.16f, 0.10f); fg = Ink; break;
            }
            var b = new VisualElement();
            b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            b.style.height = height; Pad(b, S3 + 2f, 0);
            b.style.backgroundColor = bg;
            Round(b, height / 2f);
            if (kind != ButtonKind.Ghost) Border(b, 1f, kind == ButtonKind.Secondary ? Line : new Color(0.3f, 0.15f, 0.04f, 0.35f));
            if (kind == ButtonKind.Primary || kind == ButtonKind.Danger)
                b.generateVisualContent += ctx => PaperDraw.Glossy(ctx.painter2D, b.layout.width, b.layout.height, height / 2f);
            if (icon.HasValue) { var ic = new TabletIconElement(icon.Value, 20, fg); ic.style.marginRight = 8; b.Add(ic); }
            b.Add(Text(text, FsBody + 1, fg, true));
            b.RegisterCallback<ClickEvent>(_ => click?.Invoke());
            b.RegisterCallback<PointerEnterEvent>(_ => b.style.backgroundColor = bgHover);
            b.RegisterCallback<PointerLeaveEvent>(_ => b.style.backgroundColor = bg);
            return b;
        }

        // ================= 导航 =================
        /// <summary>索引标签式分段控件。</summary>
        public static VisualElement Segmented(string[] names, int selected, Action<int> onSelect)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; row.style.alignSelf = Align.FlexStart;
            row.style.backgroundColor = PaperDeep; Pad(row, 4, 4); Round(row, RadiusM);
            Border(row, 1f, LineSoft);
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i; bool on = i == selected;
                var t = new VisualElement();
                t.style.height = 34; Pad(t, S3, 0); t.style.alignItems = Align.Center; t.style.justifyContent = Justify.Center;
                Round(t, RadiusS + 2f);
                if (on)
                {
                    t.style.backgroundColor = Amber;
                    t.generateVisualContent += ctx => PaperDraw.Glossy(ctx.painter2D, t.layout.width, t.layout.height, RadiusS + 2f);
                }
                t.Add(Text(names[i], FsBody, on ? Color.white : InkSoft, on));
                if (!on) t.RegisterCallback<ClickEvent>(_ => onSelect?.Invoke(idx));
                row.Add(t);
            }
            return row;
        }

        /// <summary>书签式侧栏标签：选中时底色铺满 + 左侧分类色条。</summary>
        public static VisualElement SideTab(TabletIconKind icon, string label, Color tint, bool selected, Action click)
        {
            var t = new VisualElement();
            t.style.width = 76; t.style.height = 68; t.style.marginBottom = 6;
            t.style.alignItems = Align.Center; t.style.justifyContent = Justify.Center;
            Round(t, RadiusM);
            if (selected) t.style.backgroundColor = Tint(tint, 0.14f);
            t.generateVisualContent += ctx =>
            {
                if (!selected) return;
                var p = ctx.painter2D;
                p.fillColor = tint;
                p.BeginPath(); PaperDraw.RoundRectPath(p, new Rect(0f, 14f, 4f, t.layout.height - 28f), 2f); p.Fill();
            };
            var medal = new VisualElement { pickingMode = PickingMode.Ignore };
            medal.style.width = 34; medal.style.height = 34; medal.style.alignItems = Align.Center; medal.style.justifyContent = Justify.Center;
            StyleMedal(medal, tint);
            medal.Add(new TabletIconElement(icon, 20, selected ? tint : InkSoft));
            t.Add(medal);
            var l = Text(label, FsSmall - 1, selected ? Ink : InkSoft, selected); l.style.marginTop = 3;
            t.Add(l);
            if (!selected)
            {
                t.RegisterCallback<PointerEnterEvent>(_ => t.style.backgroundColor = new Color(0.45f, 0.32f, 0.16f, 0.08f));
                t.RegisterCallback<PointerLeaveEvent>(_ => t.style.backgroundColor = new Color(0, 0, 0, 0));
            }
            t.RegisterCallback<ClickEvent>(_ => click?.Invoke());
            return t;
        }

        public static void StyleMedal(VisualElement m, Color tint)
        {
            m.style.backgroundColor = Tint(tint, 0.14f);
            Round(m, 20);
            m.generateVisualContent += ctx => PaperDraw.Medal(ctx.painter2D, m.layout.width, m.layout.height, tint);
        }

        // ================= 数据组件 =================
        public static VisualElement Tag(string text, Color color)
        {
            var t = new VisualElement();
            t.style.alignSelf = Align.FlexStart; t.style.flexDirection = FlexDirection.Row; t.style.alignItems = Align.Center;
            Pad(t, 9, 2); t.style.backgroundColor = Tint(color, 0.14f); Round(t, 9);
            Border(t, 1f, Tint(color, 0.45f));
            t.Add(Text(text, FsSmall - 1, Darker(color, 0.85f), true));
            return t;
        }

        /// <summary>分段刻度条（0..1）：滑槽 + 刻度 + 光泽填充。</summary>
        public static VisualElement Gauge(float value01, Color color, float height = 12f)
        {
            var e = new VisualElement();
            e.style.height = height + 10f;
            e.generateVisualContent += ctx => PaperDraw.Ruler(ctx.painter2D, 2f, e.layout.width - 2f, 7f, value01, color, color, false);
            return e;
        }

        /// <summary>双向刻度尺（-1..1，用于效果增减）。</summary>
        public static VisualElement DeltaRuler(float value, Color good, Color bad)
        {
            var e = new VisualElement();
            e.style.height = 24;
            e.generateVisualContent += ctx => PaperDraw.Ruler(ctx.painter2D, 2f, e.layout.width - 2f, 8f, Mathf.Clamp(value, -1f, 1f), good, bad, true);
            return e;
        }

        /// <summary>刻度环仪表：中心数字 + 下方标签。</summary>
        public static VisualElement Dial(float value01, Color color, string centerText, float size = 84f)
        {
            var e = new VisualElement();
            e.style.width = size; e.style.height = size; e.style.alignItems = Align.Center; e.style.justifyContent = Justify.Center;
            e.generateVisualContent += ctx => PaperDraw.Dial(ctx.painter2D, new Vector2(e.layout.width / 2f, e.layout.height / 2f), size / 2f - 3f, value01, color);
            e.Add(Text(centerText, FsBody + 1, Ink, true));
            return e;
        }

        public static VisualElement RailDivider(float height = 12f)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.height = height;
            e.generateVisualContent += ctx => PaperDraw.Rail(ctx.painter2D, 0f, e.layout.width, e.layout.height / 2f, Line);
            return e;
        }

        public static VisualElement PerfDivider()
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.height = 8;
            e.generateVisualContent += ctx => PaperDraw.Perforation(ctx.painter2D, new Vector2(2f, 4f), new Vector2(e.layout.width - 2f, 4f), Line);
            return e;
        }

        /// <summary>印章：红色双圈 + 文字，略微倾斜。</summary>
        public static VisualElement Stamp(string text, float size = 96f)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.width = size; e.style.height = size; e.style.alignItems = Align.Center; e.style.justifyContent = Justify.Center;
            e.style.rotate = new Rotate(Angle.Degrees(-12f));
            e.generateVisualContent += ctx => PaperDraw.Stamp(ctx.painter2D, new Vector2(e.layout.width / 2f, e.layout.height / 2f), size / 2f - 3f, Stamp_);
            e.Add(Text(text, FsSub, Stamp_, true));
            return e;
        }
        private static readonly Color Stamp_ = PaperTokens.Stamp;

        // ================= 浮层 =================
        /// <summary>大面板：遮罩 + 纸面卡片 + 标题 + 关闭键。返回 overlay，body 为内容容器。默认隐藏。</summary>
        public static VisualElement Sheet(string title, string subtitle, out VisualElement body, Action onClose, float widthPct = 94f, float heightPct = 90f)
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.right = 0; overlay.style.top = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0.08f, 0.07f, 0.05f, 0.42f);
            overlay.style.alignItems = Align.Center; overlay.style.justifyContent = Justify.Center;
            Fade(overlay, FadeNormal);
            overlay.style.display = DisplayStyle.None; overlay.style.opacity = 0f;

            var card = new VisualElement();
            card.style.width = Length.Percent(widthPct); card.style.height = Length.Percent(heightPct); card.style.marginTop = 24;
            card.style.flexDirection = FlexDirection.Column; card.style.overflow = Overflow.Hidden;
            card.style.backgroundColor = PaperBg; Round(card, RadiusXL - 2f); Border(card, 1f, Line);
            card.generateVisualContent += ctx => PaperDraw.Deco(ctx.painter2D, card.layout.width, card.layout.height, RadiusXL - 2f, true);
            overlay.Add(card);

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
            head.style.height = 56; head.style.paddingLeft = S4 + 2f; head.style.paddingRight = S3;
            head.Add(Text(title, FsTitle, Ink, true));
            var sub = Text(subtitle, FsSmall, InkSoft); sub.style.marginLeft = 10; sub.style.flexGrow = 1; head.Add(sub);
            head.Add(IconButton(TabletIconKind.Close, "关闭", onClose, 38f));
            card.Add(head);
            head.Add(RailLine());

            body = new VisualElement();
            body.style.flexGrow = 1; body.style.minHeight = 0; body.style.paddingBottom = 14;
            card.Add(body);
            overlay.RegisterCallback<ClickEvent>(e => { if (e.target == overlay) onClose?.Invoke(); });
            return overlay;
        }

        private static VisualElement RailLine()
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute; e.style.left = 24; e.style.right = 24; e.style.bottom = 0; e.style.height = 8;
            e.generateVisualContent += ctx => PaperDraw.Rail(ctx.painter2D, 0f, e.layout.width, 4f, LineSoft);
            return e;
        }
    }
}
