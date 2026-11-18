using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 词条正文里的「图表块」：一行以 @ 开头的指令，代替成段文字。
    /// 格式：@种类 标题 :: 项|项|项，项为 键=值（链接 [[id|文字]] 里的竖线不会被当成分隔符）。
    /// 种类：stats 数字卡 / bars 条形图 / ring 环形占比 / split 分段占比 / route 线路图 / timeline 时间轴 / flow 流程 / rel 关系 / vs 对照。
    /// </summary>
    public static partial class TabletWikiAppUI
    {
        private static readonly Color[] ChartColors =
        {
            new Color(0.12f, 0.52f, 0.46f, 1f), new Color(0.36f, 0.62f, 0.86f, 1f),
            new Color(0.95f, 0.62f, 0.40f, 1f), new Color(0.78f, 0.36f, 0.30f, 1f),
            new Color(0.55f, 0.45f, 0.72f, 1f),
        };
        private static readonly Color Track = new Color(0f, 0f, 0f, 0.07f);
        private static readonly Regex NumRx = new Regex(@"^\s*(-?[\d.]+)(?:\s*/\s*([\d.]+))?\s*(.*)$", RegexOptions.Compiled);

        private static List<string> SplitTop(string s, char sep)
        {
            var res = new List<string>(); int depth = 0, start = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (i + 1 < s.Length && s[i] == '[' && s[i + 1] == '[') { depth++; i++; }
                else if (i + 1 < s.Length && s[i] == ']' && s[i + 1] == ']') { depth = Math.Max(0, depth - 1); i++; }
                else if (s[i] == sep && depth == 0) { res.Add(s.Substring(start, i - start).Trim()); start = i + 1; }
            }
            res.Add(s.Substring(start).Trim());
            res.RemoveAll(x => x.Length == 0);
            return res;
        }

        private static List<KeyValuePair<string, string>> ParseKv(string body)
        {
            var list = new List<KeyValuePair<string, string>>();
            foreach (var it in SplitTop(body, '|'))
            {
                int eq = -1, depth = 0;
                for (int i = 0; i < it.Length && eq < 0; i++)
                {
                    if (i + 1 < it.Length && it[i] == '[' && it[i + 1] == '[') { depth++; i++; }
                    else if (i + 1 < it.Length && it[i] == ']' && it[i + 1] == ']') { depth = Math.Max(0, depth - 1); i++; }
                    else if (it[i] == '=' && depth == 0) eq = i;
                }
                if (eq > 0) list.Add(new KeyValuePair<string, string>(it.Substring(0, eq).Trim(), it.Substring(eq + 1).Trim()));
                else list.Add(new KeyValuePair<string, string>(it, string.Empty));
            }
            return list;
        }

        private static bool Num(string s, out float v, out float max, out string rest)
        {
            v = 0; max = 0; rest = s;
            var m = NumRx.Match(s ?? string.Empty);
            if (!m.Success) return false;
            float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
            if (m.Groups[2].Success) float.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out max);
            rest = m.Groups[3].Value.Trim();
            return true;
        }

        /// <summary>解析一行 @ 指令；无法识别时返回 null（调用方按普通文字处理）。</summary>
        private static VisualElement BuildBlock(string line, string selfId)
        {
            int sp = line.IndexOf(' ');
            string kind = sp < 0 ? line.Substring(1) : line.Substring(1, sp - 1);
            string rest = sp < 0 ? string.Empty : line.Substring(sp + 1).Trim();
            string title = null, body = rest;
            int sep = rest.IndexOf("::", StringComparison.Ordinal);
            if (sep >= 0) { title = rest.Substring(0, sep).Trim(); body = rest.Substring(sep + 2).Trim(); }
            var items = ParseKv(body);
            VisualElement content;
            switch (kind)
            {
                case "stats": content = BlockStats(items); break;
                case "bars": content = BlockBars(items); break;
                case "ring": content = BlockRings(items); break;
                case "split": content = BlockSplit(items); break;
                case "route": content = BlockRoute(items, selfId); break;
                case "timeline": content = BlockTimeline(items, selfId); break;
                case "flow": content = BlockFlow(items, selfId); break;
                case "rel": content = BlockRel(items, selfId, title); title = null; break;
                case "vs": content = BlockVs(items, selfId); break;
                case "talk": content = BlockTalk(items, selfId); break;
                case "draw": content = BlockDraw(body); break;
                default: return null;
            }
            if (content == null) return null;
            var frame = new VisualElement { name = "wiki-block-" + kind };
            frame.style.backgroundColor = new Color(0.955f, 0.975f, 0.97f, 1f); Radius(frame, 20);
            Pad(frame, 20, 16, 20, 18);
            frame.style.marginTop = 4; frame.style.marginBottom = 14;
            if (!string.IsNullOrEmpty(title))
            {
                var t = Text(title, 12, InkTertiary, true);
                t.style.marginBottom = 12;
                frame.Add(t);
            }
            frame.Add(content);
            return frame;
        }

        // ---------- 数字卡 ----------
        private static VisualElement BlockStats(List<KeyValuePair<string, string>> items)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap;
            int i = 0;
            foreach (var kv in items)
            {
                var tile = new VisualElement();
                tile.style.flexGrow = 1; tile.style.flexBasis = 130; tile.style.minWidth = 120;
                tile.style.backgroundColor = Color.white; Radius(tile, 16);
                Pad(tile, 18, 14, 18, 14);
                tile.style.marginRight = 10; tile.style.marginBottom = 10;
                string v = kv.Value, unit = string.Empty;
                int cut = v.IndexOf(' ');
                if (cut > 0) { unit = v.Substring(cut + 1); v = v.Substring(0, cut); }
                var line = new VisualElement(); line.style.flexDirection = FlexDirection.Row; line.style.alignItems = Align.FlexEnd;
                var big = Text(v, 30, ChartColors[i % 2 == 0 ? 0 : 1], true);
                line.Add(big);
                if (unit.Length > 0) { var u = Text(unit, 13, InkSecondary); u.style.marginLeft = 4; u.style.marginBottom = 4; line.Add(u); }
                tile.Add(line);
                var lab = Text(kv.Key, 12, InkSecondary); lab.style.marginTop = 2;
                tile.Add(lab);
                row.Add(tile); i++;
            }
            return row;
        }

        // ---------- 条形图 ----------
        private static VisualElement BlockBars(List<KeyValuePair<string, string>> items)
        {
            var host = new VisualElement();
            float autoMax = 0;
            foreach (var kv in items) if (Num(kv.Value, out var v, out var m, out _)) autoMax = Mathf.Max(autoMax, m > 0 ? m : v);
            int i = 0;
            foreach (var kv in items)
            {
                if (!Num(kv.Value, out var v, out var m, out var unit)) continue;
                float max = m > 0 ? m : autoMax;
                float pct = max > 0 ? Mathf.Clamp01(v / max) * 100f : 0f;
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                row.style.marginBottom = 10;
                var lab = Text(kv.Key, 13, Ink); lab.style.width = 150; lab.style.flexShrink = 0;
                row.Add(lab);
                var track = new VisualElement(); track.style.flexGrow = 1; track.style.height = 14; Radius(track, 7); track.style.backgroundColor = Track;
                var fill = new VisualElement(); fill.style.height = 14; Radius(fill, 7);
                fill.style.width = new Length(pct, LengthUnit.Percent);
                fill.style.backgroundColor = ChartColors[i % ChartColors.Length];
                track.Add(fill); row.Add(track);
                string shown = m > 0 ? (v.ToString("0.##", CultureInfo.InvariantCulture) + " / " + m.ToString("0.##", CultureInfo.InvariantCulture)) : v.ToString("0.##", CultureInfo.InvariantCulture);
                var val = Text(shown + (unit.Length > 0 ? " " + unit : string.Empty), 13, InkSecondary, true);
                val.style.width = 110; val.style.flexShrink = 0; val.style.marginLeft = 12; val.style.unityTextAlign = TextAnchor.MiddleRight;
                row.Add(val);
                host.Add(row); i++;
            }
            return host;
        }

        // ---------- 环形占比 ----------
        private sealed class RingElement : VisualElement
        {
            private readonly float pct; private readonly Color color;
            public RingElement(float pct, Color color)
            {
                this.pct = Mathf.Clamp(pct, 0f, 100f); this.color = color;
                style.width = 104; style.height = 104;
                generateVisualContent += Draw;
            }
            private void Draw(MeshGenerationContext mgc)
            {
                var p = mgc.painter2D;
                var c = new Vector2(52, 52);
                p.lineWidth = 12; p.lineCap = LineCap.Round;
                p.strokeColor = Track; p.BeginPath(); p.Arc(c, 42, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
                if (pct <= 0f) return;
                p.strokeColor = color; p.BeginPath();
                p.Arc(c, 42, Angle.Degrees(-90), Angle.Degrees(-90 + 360f * pct / 100f));
                p.Stroke();
            }
        }

        private static VisualElement BlockRings(List<KeyValuePair<string, string>> items)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap;
            int i = 0;
            foreach (var kv in items)
            {
                if (!Num(kv.Value, out var v, out _, out var cap)) continue;
                var cell = new VisualElement(); cell.style.alignItems = Align.Center; cell.style.marginRight = 28; cell.style.marginBottom = 6;
                var holder = new VisualElement(); holder.style.width = 104; holder.style.height = 104;
                holder.Add(new RingElement(v, ChartColors[i % ChartColors.Length]));
                var center = Text(v.ToString("0.#", CultureInfo.InvariantCulture) + "%", 20, Ink, true);
                center.style.position = Position.Absolute; center.style.left = 0; center.style.right = 0; center.style.top = 0; center.style.bottom = 0;
                center.style.unityTextAlign = TextAnchor.MiddleCenter; center.pickingMode = PickingMode.Ignore;
                holder.Add(center);
                cell.Add(holder);
                var lab = Text(kv.Key, 13, Ink, true); lab.style.marginTop = 6; cell.Add(lab);
                if (cap.Length > 0) { var c = Text(cap, 11, InkTertiary); c.style.maxWidth = 130; c.style.unityTextAlign = TextAnchor.UpperCenter; cell.Add(c); }
                row.Add(cell); i++;
            }
            return row;
        }

        // ---------- 分段占比 ----------
        private static VisualElement BlockSplit(List<KeyValuePair<string, string>> items)
        {
            var host = new VisualElement();
            float total = 0;
            foreach (var kv in items) if (Num(kv.Value, out var v, out _, out _)) total += v;
            if (total <= 0) return null;
            var bar = new VisualElement(); bar.style.flexDirection = FlexDirection.Row; bar.style.height = 38; Radius(bar, 19); bar.style.overflow = Overflow.Hidden;
            int i = 0;
            foreach (var kv in items)
            {
                if (!Num(kv.Value, out var v, out _, out _)) continue;
                var seg = new VisualElement(); seg.style.flexGrow = v; seg.style.flexBasis = 0;
                seg.style.backgroundColor = ChartColors[i % ChartColors.Length];
                seg.style.alignItems = Align.Center; seg.style.justifyContent = Justify.Center;
                seg.style.marginRight = i < items.Count - 1 ? 2 : 0;
                seg.Add(Text((v / total * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%", 14, Color.white, true));
                bar.Add(seg); i++;
            }
            host.Add(bar);
            var legend = new VisualElement(); legend.style.flexDirection = FlexDirection.Row; legend.style.flexWrap = Wrap.Wrap; legend.style.marginTop = 10;
            i = 0;
            foreach (var kv in items)
            {
                var it = new VisualElement(); it.style.flexDirection = FlexDirection.Row; it.style.alignItems = Align.Center; it.style.marginRight = 18;
                var sw = new VisualElement(); sw.style.width = 10; sw.style.height = 10; Radius(sw, 5); sw.style.backgroundColor = ChartColors[i % ChartColors.Length]; sw.style.marginRight = 6;
                it.Add(sw); it.Add(Text(kv.Key, 12, InkSecondary));
                legend.Add(it); i++;
            }
            host.Add(legend);
            return host;
        }

        // ---------- 线路图 ----------
        private static VisualElement BlockRoute(List<KeyValuePair<string, string>> items, string selfId)
        {
            float maxKm = 0;
            foreach (var kv in items) if (Num(kv.Value, out var v, out _, out _)) maxKm = Mathf.Max(maxKm, v);
            if (maxKm <= 0) return null;
            var host = new VisualElement(); host.style.height = 118; host.style.marginLeft = 40; host.style.marginRight = 40;
            var line = new VisualElement(); line.style.position = Position.Absolute; line.style.left = 0; line.style.right = 0; line.style.top = 52; line.style.height = 6;
            Radius(line, 3); line.style.backgroundColor = TintDeep;
            host.Add(line);
            foreach (var kv in items)
            {
                if (!Num(kv.Value, out var km, out _, out var tag)) continue;
                bool off = tag.Contains("废") || tag.Contains("停");
                var node = new VisualElement(); node.style.position = Position.Absolute; node.style.top = 0; node.style.height = 118;
                node.style.left = new Length(km / maxKm * 100f, LengthUnit.Percent);
                node.style.width = 0; node.style.alignItems = Align.Center;
                var kmLabel = Text(km.ToString("0.#", CultureInfo.InvariantCulture) + " km", 11, InkTertiary, true);
                kmLabel.style.position = Position.Absolute; kmLabel.style.top = 20; kmLabel.style.whiteSpace = WhiteSpace.NoWrap; kmLabel.style.width = 80; kmLabel.style.left = -40; kmLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                node.Add(kmLabel);
                var dot = new VisualElement(); dot.style.position = Position.Absolute; dot.style.top = 45; dot.style.width = 20; dot.style.height = 20; dot.style.left = -10; Radius(dot, 10);
                dot.style.backgroundColor = off ? Color.white : Accent; Border(dot, 4, off ? InkTertiary : Accent);
                if (!off) dot.style.borderTopColor = dot.style.borderBottomColor = dot.style.borderLeftColor = dot.style.borderRightColor = Tint;
                node.Add(dot);
                var name = Rich(kv.Key, 13, off ? InkTertiary : Ink, selfId);
                name.style.position = Position.Absolute; name.style.top = 74; name.style.width = 110; name.style.left = -55; name.style.unityTextAlign = TextAnchor.UpperCenter;
                node.Add(name);
                if (tag.Length > 0) { var tg = Text(tag, 11, off ? RedLink : InkTertiary); tg.style.position = Position.Absolute; tg.style.top = 94; tg.style.width = 110; tg.style.left = -55; tg.style.unityTextAlign = TextAnchor.UpperCenter; node.Add(tg); }
                host.Add(node);
            }
            return host;
        }

        // ---------- 时间轴 ----------
        private static VisualElement BlockTimeline(List<KeyValuePair<string, string>> items, string selfId)
        {
            var host = new VisualElement();
            for (int i = 0; i < items.Count; i++)
            {
                string when = items[i].Key; bool key = when.StartsWith("*"); if (key) when = when.Substring(1);
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
                var y = Text(when, 13, key ? new Color(0.62f, 0.45f, 0.08f, 1f) : Accent, true);
                y.style.width = 92; y.style.flexShrink = 0; y.style.unityTextAlign = TextAnchor.UpperRight; y.style.marginTop = 1;
                row.Add(y);
                var spine = new VisualElement(); spine.style.width = 28; spine.style.flexShrink = 0; spine.style.alignItems = Align.Center;
                var rail = new VisualElement(); rail.style.position = Position.Absolute; rail.style.width = 2; rail.style.left = 13; rail.style.backgroundColor = TintDeep;
                rail.style.top = i == 0 ? 10 : 0;
                if (i == items.Count - 1) rail.style.height = 10; else rail.style.bottom = 0;
                spine.Add(rail);
                var dot = new VisualElement(); dot.style.width = key ? 14 : 10; dot.style.height = key ? 14 : 10; Radius(dot, 7); dot.style.marginTop = key ? 3 : 5;
                dot.style.backgroundColor = key ? Amber : Accent;
                spine.Add(dot);
                row.Add(spine);
                var txt = Rich(items[i].Value.Length > 0 ? items[i].Value : string.Empty, s_bodySize, Ink, selfId);
                txt.style.flexGrow = 1; txt.style.flexShrink = 1; txt.style.minWidth = 0; txt.style.paddingBottom = 10;
                row.Add(txt);
                host.Add(row);
            }
            return host;
        }

        // ---------- 流程 ----------
        private static VisualElement BlockFlow(List<KeyValuePair<string, string>> items, string selfId)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.alignItems = Align.Stretch;
            for (int i = 0; i < items.Count; i++)
            {
                var card = new VisualElement(); card.style.flexGrow = 1; card.style.flexBasis = 120; card.style.minWidth = 110;
                card.style.backgroundColor = Color.white; Radius(card, 16); Pad(card, 14, 12, 14, 12);
                card.style.marginBottom = 8;
                var num = Text((i + 1).ToString(), 12, Color.white, true);
                num.style.width = 22; num.style.height = 22; Radius(num, 11); num.style.backgroundColor = Accent; num.style.unityTextAlign = TextAnchor.MiddleCenter; num.style.marginBottom = 8;
                card.Add(num);
                card.Add(Rich(items[i].Key, 14, Ink, selfId));
                if (items[i].Value.Length > 0) { var d = Rich(items[i].Value, 12, InkSecondary, selfId); d.style.marginTop = 4; card.Add(d); }
                row.Add(card);
                if (i < items.Count - 1)
                {
                    var arrow = Text("›", 24, InkTertiary, true);
                    arrow.style.alignSelf = Align.Center; arrow.style.marginLeft = 6; arrow.style.marginRight = 6; arrow.style.marginBottom = 8;
                    row.Add(arrow);
                }
            }
            return row;
        }

        // ---------- 关系 ----------
        private static VisualElement BlockRel(List<KeyValuePair<string, string>> items, string selfId, string center)
        {
            var host = new VisualElement(); host.style.alignItems = Align.Center;
            if (!string.IsNullOrEmpty(center))
            {
                var c = Text(center, 15, Color.white, true); Pad(c, 22, 9, 22, 9); Radius(c, 22); c.style.backgroundColor = Accent;
                host.Add(c);
                var stem = new VisualElement(); stem.style.width = 2; stem.style.height = 14; stem.style.backgroundColor = TintDeep; host.Add(stem);
            }
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.justifyContent = Justify.Center;
            foreach (var kv in items)
            {
                var card = new VisualElement(); card.style.alignItems = Align.Center; card.style.minWidth = 120;
                card.style.backgroundColor = Color.white; Radius(card, 16); Pad(card, 16, 10, 16, 12);
                card.style.marginLeft = 5; card.style.marginRight = 5; card.style.marginBottom = 10;
                var rel = Text(kv.Value, 11, new Color(0.62f, 0.45f, 0.08f, 1f), true); rel.style.marginBottom = 4; rel.style.unityTextAlign = TextAnchor.UpperCenter;
                card.Add(rel);
                var who = Rich(kv.Key, 15, Ink, selfId); who.style.unityTextAlign = TextAnchor.UpperCenter;
                card.Add(who);
                row.Add(card);
            }
            host.Add(row);
            return host;
        }

        // ---------- 对照 ----------
        private static VisualElement BlockVs(List<KeyValuePair<string, string>> items, string selfId)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap;
            int i = 0;
            foreach (var kv in items)
            {
                var col = new VisualElement(); col.style.flexGrow = 1; col.style.flexBasis = 220; col.style.minWidth = 200;
                col.style.backgroundColor = Color.white; Radius(col, 16);
                col.style.marginRight = i < items.Count - 1 ? 10 : 0; col.style.marginBottom = 8; col.style.overflow = Overflow.Hidden;
                var head = Text(kv.Key, 14, ChartColors[i % ChartColors.Length], true); Pad(head, 16, 11, 16, 5);
                col.Add(head);
                foreach (var li in kv.Value.Split('，'))
                {
                    string t = li.Trim(); if (t.Length == 0) continue;
                    var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; Pad(r, 16, 7, 16, 7);
                    var d = new VisualElement(); d.style.width = 6; d.style.height = 6; Radius(d, 3); d.style.backgroundColor = ChartColors[i % ChartColors.Length]; d.style.marginRight = 10; d.style.flexShrink = 0;
                    r.Add(d); r.Add(Rich(t, 13, Ink, selfId));
                    col.Add(r);
                }
                row.Add(col); i++;
            }
            return row;
        }

        // ---------- 说法（对话气泡）----------
        private static Color SpeakerColor(string who)
        {
            switch (who)
            {
                case "老陈": return ChartColors[3];
                case "张工": return ChartColors[0];
                case "岁月": return ChartColors[1];
                default: return ChartColors[4];
            }
        }

        private static VisualElement BlockTalk(List<KeyValuePair<string, string>> items, string selfId)
        {
            var host = new VisualElement();
            for (int i = 0; i < items.Count; i++)
            {
                var kv = items[i]; if (kv.Value.Length == 0) continue;
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.FlexStart;
                row.style.marginBottom = i < items.Count - 1 ? 10 : 0;
                var c = SpeakerColor(kv.Key);
                var av = Text(kv.Key.Length > 0 ? kv.Key.Substring(0, 1) : "·", 14, Color.white, true);
                av.style.width = 36; av.style.height = 36; Radius(av, 18); av.style.backgroundColor = c; av.style.unityTextAlign = TextAnchor.MiddleCenter;
                av.style.flexShrink = 0; av.style.marginRight = 12;
                row.Add(av);
                var col = new VisualElement(); col.style.flexGrow = 1; col.style.flexShrink = 1; col.style.minWidth = 0;
                var who = Text(kv.Key, 12, c, true); who.style.marginBottom = 4; col.Add(who);
                var bubble = new VisualElement(); bubble.style.backgroundColor = Color.white; Radius(bubble, 16); bubble.style.borderTopLeftRadius = 4; Pad(bubble, 16, 11, 16, 12);
                bubble.Add(Rich(kv.Value, 14, Ink, selfId));
                col.Add(bubble);
                row.Add(col);
                host.Add(row);
            }
            return host;
        }

        // ---------- 阴影（UI Toolkit 没有 box-shadow：用几圈越来越淡的外环叠出柔和的投影）----------
        private static void SoftShadow(VisualElement e, float radius, float strength = 1f)
        {
            float[] alpha = { 0.038f, 0.030f, 0.023f, 0.017f, 0.012f, 0.008f };
            const float w = 2f;
            for (int k = 0; k < alpha.Length; k++)
            {
                float o = w * (k + 1);
                var ring = new VisualElement { pickingMode = PickingMode.Ignore };
                ring.style.position = Position.Absolute;
                ring.style.left = -o; ring.style.right = -o; ring.style.top = -o + 1.5f; ring.style.bottom = -o - 1.5f;
                var c = new Color(0.06f, 0.20f, 0.17f, alpha[k] * strength);
                ring.style.borderTopWidth = w; ring.style.borderBottomWidth = w; ring.style.borderLeftWidth = w; ring.style.borderRightWidth = w;
                ring.style.borderTopColor = c; ring.style.borderBottomColor = c; ring.style.borderLeftColor = c; ring.style.borderRightColor = c;
                Radius(ring, radius + o);
                e.Insert(0, ring);
            }
        }

        // ---------- 手绘插图（矢量，随界面缩放；浅色填充 + 青绿描边）----------
        private static readonly Color ArtLine = new Color(0.12f, 0.52f, 0.46f, 1f);
        private static readonly Color ArtSoft = new Color(0.875f, 0.945f, 0.925f, 1f);
        private static readonly Color ArtDeep = new Color(0.78f, 0.90f, 0.86f, 1f);
        private static readonly Color ArtStone = new Color(0.90f, 0.91f, 0.89f, 1f);
        private static readonly Color ArtWood = new Color(0.95f, 0.90f, 0.80f, 1f);
        private static readonly Color ArtNote = new Color(0.45f, 0.50f, 0.48f, 1f);

        private sealed class LineArt : VisualElement
        {
            public LineArt(float w, float h, Action<Painter2D> draw)
            {
                style.width = w; style.height = h;
                generateVisualContent += mgc => { var p = mgc.painter2D; p.lineJoin = LineJoin.Round; p.lineCap = LineCap.Round; draw(p); };
            }
        }

        private static void Ln(Painter2D p, params float[] xy)
        {
            p.BeginPath(); p.MoveTo(new Vector2(xy[0], xy[1]));
            for (int i = 2; i + 1 < xy.Length; i += 2) p.LineTo(new Vector2(xy[i], xy[i + 1]));
            p.Stroke();
        }

        /// <summary>闭合形状：先填色再描边。</summary>
        private static void Shape(Painter2D p, Color fill, params float[] xy)
        {
            p.BeginPath(); p.MoveTo(new Vector2(xy[0], xy[1]));
            for (int i = 2; i + 1 < xy.Length; i += 2) p.LineTo(new Vector2(xy[i], xy[i + 1]));
            p.ClosePath();
            p.fillColor = fill; p.Fill(); p.Stroke();
        }

        private static void Box(Painter2D p, Color fill, float x, float y, float w, float h) => Shape(p, fill, x, y, x + w, y, x + w, y + h, x, y + h);

        private static void Disc(Painter2D p, Color fill, float x, float y, float r)
        {
            p.BeginPath(); p.Arc(new Vector2(x, y), r, Angle.Degrees(0), Angle.Degrees(360)); p.ClosePath();
            p.fillColor = fill; p.Fill(); p.Stroke();
        }

        private static void Pen(Painter2D p, Color c, float w) { p.strokeColor = c; p.lineWidth = w; }

        private static void Arrow(Painter2D p, float x1, float y1, float x2, float y2)
        {
            Ln(p, x1, y1, x2, y2);
            float dx = x2 - x1, dy = y2 - y1, l = Mathf.Sqrt(dx * dx + dy * dy); if (l < 1f) return;
            dx /= l; dy /= l;
            Ln(p, x2 - dx * 7 - dy * 4, y2 - dy * 7 + dx * 4, x2, y2, x2 - dx * 7 + dy * 4, y2 - dy * 7 - dx * 4);
        }

        /// <summary>双线钢轨：沿折线上下各偏 3px。</summary>
        private static void Rails(Painter2D p, params float[] xy)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                var q = (float[])xy.Clone();
                for (int i = 1; i < q.Length; i += 2) q[i] += s * 3f;
                Ln(p, q);
            }
        }

        private static void Tag(VisualElement host, string t, float x, float y, float w = 90, TextAnchor a = TextAnchor.MiddleLeft)
        {
            var l = Text(t, 12, InkSecondary);
            l.style.position = Position.Absolute; l.style.left = x; l.style.top = y; l.style.width = w;
            l.style.unityTextAlign = a; l.style.whiteSpace = WhiteSpace.NoWrap;
            host.Add(l);
        }

        private static VisualElement BlockDraw(string kind)
        {
            kind = kind.Trim();
            VisualElement canvas;
            switch (kind)
            {
                case "track":
                    canvas = new LineArt(560, 210, p =>
                    {
                        Pen(p, ArtLine, 1.5f);
                        Shape(p, ArtStone, 50, 192, 510, 192, 436, 126, 124, 126);                       // 道砟
                        Pen(p, new Color(0.12f, 0.52f, 0.46f, 0.45f), 1f);
                        float[] st = { 112, 184, 160, 176, 205, 186, 250, 178, 300, 187, 350, 177, 398, 185, 446, 180, 188, 160, 262, 154, 338, 158, 414, 156, 226, 140, 300, 138, 372, 142 };
                        for (int i = 0; i + 1 < st.Length; i += 2) { p.BeginPath(); p.Arc(new Vector2(st[i], st[i + 1]), 3f, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke(); }
                        Pen(p, ArtLine, 1.5f);
                        Box(p, ArtWood, 150, 104, 260, 22);                                              // 枕木
                        Pen(p, new Color(0.12f, 0.52f, 0.46f, 0.35f), 1f);
                        for (int i = 1; i < 6; i++) Ln(p, 150 + i * 43.3f, 104, 150 + i * 43.3f, 126);
                        Pen(p, ArtLine, 1.5f);
                        foreach (float x in new[] { 232f, 328f })                                         // 钢轨
                        {
                            Box(p, ArtDeep, x - 20, 101, 40, 3);                                         // 垫板
                            Shape(p, Color.white, x - 18, 101, x - 18, 97, x - 6, 97, x - 6, 72, x - 11, 67, x - 11, 50, x + 11, 50, x + 11, 67, x + 6, 72, x + 6, 97, x + 18, 97, x + 18, 101);
                            Pen(p, new Color(0.12f, 0.52f, 0.46f, 0.5f), 1f); Ln(p, x - 7, 55, x + 7, 55); Pen(p, ArtLine, 1.5f);
                            Disc(p, ArtLine, x - 12, 115, 1.6f); Disc(p, ArtLine, x + 12, 115, 1.6f);   // 道钉
                        }
                        Pen(p, ArtNote, 1f);
                        Ln(p, 243, 50, 243, 22); Ln(p, 317, 50, 317, 22);                                // 轨距标注
                        Arrow(p, 280, 30, 243, 30); Arrow(p, 280, 30, 317, 30);
                        Ln(p, 341, 58, 426, 58); Ln(p, 410, 114, 446, 114); Ln(p, 452, 163, 486, 163);
                        Disc(p, ArtNote, 341, 58, 2f); Disc(p, ArtNote, 410, 114, 2f); Disc(p, ArtNote, 452, 163, 2f);
                    });
                    Tag(canvas, "轨距 1435 mm", 230, 4, 100, TextAnchor.MiddleCenter);
                    Tag(canvas, "钢轨", 432, 49); Tag(canvas, "枕木", 452, 105); Tag(canvas, "道砟", 492, 154, 60);
                    break;
                case "loco":
                    canvas = new LineArt(560, 210, p =>
                    {
                        Pen(p, ArtLine, 1.5f);
                        Rails(p, 20, 168, 540, 168);                                                      // 钢轨与枕木
                        Pen(p, new Color(0.12f, 0.52f, 0.46f, 0.35f), 1f);
                        for (float x = 26; x < 540; x += 18) Ln(p, x, 162, x, 176);
                        Pen(p, ArtLine, 1.5f);
                        Box(p, ArtDeep, 56, 138, 14, 8); Box(p, ArtDeep, 492, 138, 14, 8);                // 车钩
                        Box(p, ArtDeep, 92, 126, 150, 16); Box(p, ArtDeep, 330, 126, 140, 16);            // 转向架构架
                        Shape(p, ArtSoft, 70, 126, 70, 84, 84, 72, 372, 72, 372, 126);                    // 机器间
                        Shape(p, ArtDeep, 372, 126, 372, 42, 452, 42, 492, 62, 492, 126);                 // 驾驶室
                        Box(p, Color.white, 398, 56, 52, 30);                                             // 车窗
                        Ln(p, 424, 56, 424, 86);
                        Box(p, ArtDeep, 116, 62, 44, 10);                                                 // 顶部风道
                        Pen(p, new Color(0.83f, 0.66f, 0.26f, 1f), 1.5f); Ln(p, 70, 110, 492, 110);       // 腰线
                        Pen(p, ArtLine, 1.5f);
                        foreach (float x in new[] { 150f, 220f })                                         // 散热风扇
                        {
                            Disc(p, Color.white, x, 94, 16);
                            Pen(p, new Color(0.12f, 0.52f, 0.46f, 0.55f), 1f);
                            Ln(p, x - 11, 94, x + 11, 94); Ln(p, x, 83, x, 105); Ln(p, x - 8, 86, x + 8, 102); Ln(p, x - 8, 102, x + 8, 86);
                            Pen(p, ArtLine, 1.5f);
                        }
                        Disc(p, new Color(0.96f, 0.84f, 0.45f, 1f), 486, 96, 5f);                          // 前灯
                        foreach (float x in new[] { 110f, 160f, 210f, 350f, 400f, 450f })                  // 车轮
                        {
                            Disc(p, Color.white, x, 153, 15);
                            Disc(p, ArtDeep, x, 153, 5);
                        }
                        Pen(p, ArtNote, 1f);
                        Ln(p, 255, 72, 255, 28, 290, 28); Ln(p, 440, 42, 440, 28, 472, 28); Ln(p, 185, 142, 185, 190, 214, 190);
                        Disc(p, ArtNote, 255, 72, 2f); Disc(p, ArtNote, 440, 42, 2f); Disc(p, ArtNote, 185, 142, 2f);
                    });
                    Tag(canvas, "柴油机", 296, 19); Tag(canvas, "驾驶室", 478, 19); Tag(canvas, "转向架", 220, 181);
                    break;
                case "siding":
                    canvas = new LineArt(560, 170, p =>
                    {
                        Pen(p, ArtLine, 1.5f);
                        Rails(p, 20, 70, 170, 70); Rails(p, 390, 70, 540, 70);                              // 主线
                        Rails(p, 170, 70, 205, 124, 355, 124, 390, 70);                                    // 会让线
                        Rails(p, 170, 70, 390, 70);
                        Shape(p, Color.white, 240, 44, 240, 28, 280, 14, 320, 28, 320, 44);                // 站房
                        Box(p, ArtDeep, 270, 32, 20, 12);
                        Shape(p, ArtSoft, 40, 40, 130, 40, 130, 62, 40, 62);                               // 先到的列车
                        Box(p, Color.white, 52, 46, 14, 9); Box(p, Color.white, 76, 46, 14, 9); Box(p, Color.white, 100, 46, 14, 9);
                        Disc(p, Color.white, 56, 64, 4.5f); Disc(p, Color.white, 114, 64, 4.5f);
                        Shape(p, ArtDeep, 235, 94, 325, 94, 325, 116, 235, 116);                           // 避让的列车
                        Box(p, Color.white, 247, 100, 14, 9); Box(p, Color.white, 271, 100, 14, 9); Box(p, Color.white, 295, 100, 14, 9);
                        Disc(p, Color.white, 251, 118, 4.5f); Disc(p, Color.white, 309, 118, 4.5f);
                        Pen(p, ArtNote, 1f); Arrow(p, 140, 51, 184, 51);
                    });
                    Tag(canvas, "先到的列车通过", 30, 14, 130); Tag(canvas, "车站", 326, 22, 50); Tag(canvas, "区间只容一列车", 410, 40, 130);
                    Tag(canvas, "会让线：另一列在站内避让", 190, 134, 190, TextAnchor.MiddleCenter);
                    break;
                default: return null;
            }
            var holder = new VisualElement(); holder.style.alignItems = Align.Center;
            holder.Add(canvas);
            return holder;
        }
    }
}
