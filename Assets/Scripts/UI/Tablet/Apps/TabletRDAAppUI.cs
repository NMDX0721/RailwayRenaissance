using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;
using RailwayRenaissance.UI.Tablet.Paper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板应用：RDA 铁路行车调度助手 —— 经营主玩法入口（浅色主题，图形化）。
    /// 结构：顶部常驻 HUD（资金/信任/车况/客流）→ 左侧一级导航（总览/调度/人员/订单/市场/财务）
    ///       → 顶部二级分段（随一级变化）→ 页面；「结束一天」为全局悬浮按钮，不放进菜单。
    /// 政治压力与基建老化只给定性措辞，不显示数值。
    /// </summary>
    public static class TabletRDAAppUI
    {
        // ===== 浅色调色板 =====
        // 统一使用「站台纸本」token（见 Paper/PaperTokens.cs）
        private static readonly Color Ink = PaperTokens.Ink;
        private static readonly Color InkSoft = PaperTokens.InkSoft;
        private static readonly Color InkMuted = PaperTokens.InkMute;
        private static readonly Color CardBg = PaperTokens.Card;
        private static readonly Color CardLine = PaperTokens.LineSoft;
        private static readonly Color TrackBg = PaperTokens.PaperDeep;
        private static readonly Color Good = PaperTokens.Good;
        private static readonly Color Warn = PaperTokens.Warn;
        private static readonly Color Bad = PaperTokens.Bad;
        private static Color Accent => PaperTokens.Amber;

        // ===== 状态 =====
        private static Action<string> s_launchApp;
        private static Action s_openStory;
        private static Action s_openAffairs;
        private static Action<string> s_showToast;
        /// <summary>受限模式：在地图指定地点打开单一功能页——无一级导航、无“结束一天”（结束一天只在休息点）。</summary>
        public static bool Restricted;
        private static int s_l1;
        private static readonly int[] s_l2 = new int[6];
        private static DayLoopController.DayReport? s_pendingReport;

        private static readonly string[] L1Names = { "总览", "调度", "人员", "订单", "市场", "财务" };
        private static readonly TabletIconKind[] L1Icons =
        {
            TabletIconKind.Home, TabletIconKind.Train, TabletIconKind.Person,
            TabletIconKind.Note, TabletIconKind.Chart, TabletIconKind.Trophy
        };
        private static readonly string[][] L2Names =
        {
            new[] { "线路图", "值班简报" },
            new[] { "发车方案", "经营策略" },
            new[] { "在编人员" },
            new[] { "在办订单" },
            new[] { "沙能动向" },
            new[] { "资金趋势", "最近日报" },
        };

        /// <summary>直接打开某一级/二级页面（供探索层等外部入口使用）。</summary>
        public static void OpenPage(int l1, int l2 = 0, bool restricted = false)
        {
            Restricted = restricted;
            s_l1 = Mathf.Clamp(l1, 0, L1Names.Length - 1);
            s_l2[s_l1] = Mathf.Clamp(l2, 0, L2Names[s_l1].Length - 1);
        }

        /// <summary>外部结算一天后，下次构建 RDA 时弹出日报。</summary>
        public static void QueueReport(DayLoopController.DayReport report) { s_pendingReport = report; }

        /// <summary>RDA 内点“结束一天”后回调（探索场景据此重置当日时间与位置）。</summary>
        public static Action OnDayEnded;

        private static void ShowToast(string msg)
        {
            if (s_showToast != null) s_showToast(msg);
            else Debug.Log("[RDA] " + msg);
        }

        private static void Rebuild(VisualElement win)
        {
            win.Clear();
            BuildRDAApp(win);
        }

        // ================= 入口 =================
        public static void BuildRDAApp(VisualElement win, Action<string> launchApp = null, Action onOpenStory = null, Action onOpenAffairs = null, Action<string> showToast = null)
        {
            if (launchApp != null) s_launchApp = launchApp;
            if (onOpenStory != null) s_openStory = onOpenStory;
            if (onOpenAffairs != null) s_openAffairs = onOpenAffairs;
            if (showToast != null) s_showToast = showToast;

            win.style.flexDirection = FlexDirection.Column;
            win.style.paddingLeft = 16; win.style.paddingRight = 16;

            win.Add(BuildHud());

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;
            body.style.marginTop = 12;
            body.style.minHeight = 0;
            win.Add(body);

            if (!Restricted) body.Add(BuildNavRail(win));

            var right = new VisualElement();
            right.style.flexGrow = 1;
            right.style.flexDirection = FlexDirection.Column;
            right.style.marginLeft = Restricted ? 0 : 14;
            right.style.minWidth = 0;
            right.style.position = Position.Relative;
            body.Add(right);

            string[] l2 = L2Names[s_l1];
            if (l2.Length > 1) right.Add(BuildSegment(win, l2, s_l2[s_l1], i => { s_l2[s_l1] = i; Rebuild(win); }));

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.marginTop = l2.Length > 1 ? 12 : 0;
            scroll.contentContainer.style.paddingBottom = 96;
            right.Add(scroll);

            BuildPage(win, scroll.contentContainer);
            if (!Restricted) right.Add(BuildEndDayButton(win));

            if (s_pendingReport.HasValue)
            {
                var pending = s_pendingReport.Value;
                s_pendingReport = null;
                win.schedule.Execute(() => ShowDayReport(win, pending));
            }
        }

        // ================= 通用组件 =================
        private static VisualElement Card(float pad = 16) => PaperUI.Card(pad);

        private static void SetRadius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        private static void SetBorder(VisualElement e, float w, Color c)
        {
            e.style.borderTopWidth = w; e.style.borderBottomWidth = w; e.style.borderLeftWidth = w; e.style.borderRightWidth = w;
            e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c;
        }

        private static Label Lbl(string text, int size, Color color, bool bold = false, int top = 0)
        {
            var l = new Label(text);
            l.style.fontSize = size; l.style.color = color;
            l.style.unityFontDefinition = bold ? FdBold() : Fd();
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = top;
            return l;
        }

        private static VisualElement RowEl(Justify j = Justify.FlexStart)
        {
            var r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row;
            r.style.alignItems = Align.Center;
            r.style.justifyContent = j;
            return r;
        }

        private static VisualElement Bar(float v, Color fill, float h = 8f) => PaperUI.Gauge(v, fill, h);

        private static VisualElement PillBtn(string text, Color bg, Color fg, Action click, int height = 36)
        {
            bool primary = fg == Color.white;
            var b = PaperUI.Button(text, primary ? PaperUI.ButtonKind.Primary : PaperUI.ButtonKind.Secondary, click, null, height);
            b.style.marginLeft = 8;
            return b;
        }

        private static VisualElement BuildSegment(VisualElement win, string[] names, int selected, Action<int> onSelect)
            => PaperUI.Segmented(names, selected, onSelect);

        // ================= 一级导航 =================
        private static readonly Color[] L1Tints =
        {
            PaperTokens.Amber, PaperTokens.Dispatch, PaperTokens.Crew, PaperTokens.Orders, PaperTokens.Market, PaperTokens.Finance
        };

        private static VisualElement BuildNavRail(VisualElement win)
        {
            var rail = PaperUI.Panel(PaperTokens.RadiusL, 8, 10, false, false);
            rail.style.width = 92;
            rail.style.flexShrink = 0;
            rail.style.flexDirection = FlexDirection.Column;
            rail.style.alignItems = Align.Center;
            for (int i = 0; i < L1Names.Length; i++)
            {
                int idx = i;
                var item = PaperUI.SideTab(L1Icons[i], L1Names[i], L1Tints[i], i == s_l1, () => { s_l1 = idx; Rebuild(win); });
                item.name = "rda-nav-" + i;
                rail.Add(item);
            }
            return rail;
        }

        // ================= HUD =================
        private static VisualElement BuildHud()
        {
            var row = RowEl();
            row.style.height = 116;
            row.style.flexShrink = 0;

            var history = GameData.GetTrendHistory();
            var money = new List<float>();
            var trust = new List<float>();
            int from = Mathf.Max(0, history.Count - 14);
            for (int i = from; i < history.Count; i++) { money.Add(history[i].money); trust.Add(history[i].trust); }

            // 资金
            var c1 = HudCard();
            var l1 = RowEl();
            l1.Add(new TabletIconElement(TabletIconKind.Bolt, 22, Accent));
            l1.Add(Lbl("运营资金", 14, InkSoft));
            c1.Add(l1);
            c1.Add(Lbl(GameData.GetMoney().ToString("N0"), 28, Ink, true, 2));
            var d1 = RowEl();
            d1.Add(Delta(GameData.DailyMoneyChange));
            c1.Add(d1);
            if (money.Count > 1) c1.Add(new RdaSpark(money, Accent) { style = { position = Position.Absolute, right = 14, top = 28, width = 120, height = 44 } });
            row.Add(c1);

            // 信任
            row.Add(RingCard("民众信任", GameData.GetTrust(), 100, "/100", GameData.DailyTrustChange, TrustColor(GameData.GetTrust())));
            // 车况
            row.Add(RingCard("车况健康", GameData.GetTrainCondition(), 100, "%", GameData.DailyTrainConditionChange, TrustColor(GameData.GetTrainCondition())));

            // 客流
            var c4 = HudCard();
            var l4 = RowEl();
            l4.Add(new TabletIconElement(TabletIconKind.Person, 22, Accent));
            l4.Add(Lbl("预期客流", 14, InkSoft));
            c4.Add(l4);
            c4.Add(Lbl(GameData.GetExpectedPassengers() + " 人次/日", 24, Ink, true, 4));
            int cap = Mathf.Max(1, GameData.CarCount * 30 * (GameData.SelectedDispatchPlanIndex + 1)); // 近似：每节 30 座 × 当日趟数
            c4.Add(Bar(GameData.GetExpectedPassengers() / (float)cap, Accent, 8f));
            c4.Add(Lbl($"运力约 {cap} 座 · 第 {GameData.GetDay()} 天", 12, InkMuted, false, 4));
            row.Add(c4);
            return row;
        }

        private static VisualElement HudCard()
        {
            var c = Card(14);
            c.style.flexGrow = 1; c.style.flexBasis = 0;
            c.style.marginRight = 12;
            c.style.height = new Length(100, LengthUnit.Percent);
            c.style.position = Position.Relative;
            return c;
        }

        private static Color TrustColor(float v) => v >= 65 ? Good : (v >= 40 ? Warn : Bad);

        private static VisualElement Delta(int d)
        {
            var r = RowEl();
            if (d == 0) { r.Add(Lbl("较昨日持平", 12, InkMuted)); return r; }
            Color c = d > 0 ? Good : Bad;
            r.Add(Lbl((d > 0 ? "▲ " : "▼ ") + Mathf.Abs(d).ToString("N0"), 13, c, true));
            r.Add(Lbl(" 较昨日", 12, InkMuted));
            return r;
        }

        private static VisualElement RingCard(string title, float value, float max, string unit, int delta, Color color)
        {
            var c = HudCard();
            c.style.flexDirection = FlexDirection.Row;
            c.style.alignItems = Align.Center;
            var dial = PaperUI.Dial(value / max, color, Mathf.RoundToInt(value).ToString(), 76f);
            dial.style.flexShrink = 0;
            c.Add(dial);
            var col = new VisualElement();
            col.style.marginLeft = 12;
            col.Add(Lbl(title, 14, InkSoft));
            col.Add(Lbl(unit == "%" ? Mathf.RoundToInt(value) + "%" : Mathf.RoundToInt(value) + unit, 20, Ink, true, 2));
            col.Add(Delta(delta));
            c.Add(col);
            return c;
        }

        // ================= 页面分发 =================
        private static void BuildPage(VisualElement win, VisualElement page)
        {
            switch (s_l1)
            {
                case 0: if (s_l2[0] == 0) BuildRouteMapPage(win, page); else BuildBriefPage(win, page); break;
                case 1: if (s_l2[1] == 0) BuildDispatchPage(win, page); else BuildStrategyPage(win, page); break;
                case 2: BuildCrewPage(win, page); break;
                case 3: BuildOrdersPage(win, page); break;
                case 4: BuildMarketPage(win, page); break;
                default: if (s_l2[5] == 0) BuildFinancePage(win, page); else BuildReportPage(win, page); break;
            }
        }

        // ---------- 总览 · 线路图 ----------
        private static readonly (string id, string name, float km)[] Stations =
        {
            ("wufeng", "雾峰村", 0f), ("chashan", "茶山", 8f), ("songqiao", "松桥", 15f), ("kuangqu", "矿区", 23f)
        };

        private static string CityName(string id)
        {
            foreach (var s in Stations) if (s.id == id) return s.name;
            return id;
        }

        // 沿线中间站（茶山、松桥）没有独立的 city 数据：序章后两端都已解锁即视为整线通车。
        // TODO：中间站状态后续接入 WorldSeedData / 线路修复进度。
        private static bool StationOpen(string id)
        {
            var unlocked = WorldGen.RegionUnlockManager.GetUnlockedCityIds();
            if (unlocked == null) return false;
            if (id == "chashan" || id == "songqiao") return unlocked.Contains("wufeng") && unlocked.Contains("kuangqu");
            return unlocked.Contains(id);
        }

        private static void BuildRouteMapPage(VisualElement win, VisualElement page)
        {
            string first = null, last = null; float km = 0f;
            foreach (var s in Stations)
                if (StationOpen(s.id)) { if (first == null) first = s.name; last = s.name; km = Mathf.Max(km, s.km); }
            var card = Card(18);
            var head = RowEl(Justify.SpaceBetween);
            head.Add(Lbl($"当前运营线 · {(first ?? "雾峰村")} – {(last ?? "雾峰村")}线", 20, Ink, true));
            var trend = PillBtn("五线态势", new Color(Accent.r, Accent.g, Accent.b, 0.14f), Accent, () => s_launchApp?.Invoke("五线态势"), 32);
            head.Add(trend);
            card.Add(head);
            card.Add(Lbl($"已通运营段 {km:0.#} km · 全线规划 {Stations[Stations.Length - 1].km:0.#} km（实线=运营中，虚线=待修复/待开通）", 13, InkSoft, false, 2));

            var stNames = new List<string>(); var stKm = new List<float>(); var stOpen = new List<bool>();
            foreach (var s in Stations) { stNames.Add(s.name); stKm.Add(s.km); stOpen.Add(StationOpen(s.id)); }
            var map = new RdaRouteMap(stNames, stKm, stOpen, Accent, Ink, InkMuted);
            map.style.height = 190; map.style.marginTop = 6;
            card.Add(map);
            page.Add(card);

            // 三张小卡：机车 / 今日方案 / 快捷入口
            var row = RowEl();
            row.style.alignItems = Align.Stretch;
            row.style.marginTop = 12;
            page.Add(row);

            var train = Card(16); train.style.flexGrow = 1; train.style.flexBasis = 0; train.style.marginRight = 12;
            train.Add(Lbl("机车", 14, InkSoft));
            train.Add(Lbl("NF-5K「耕牛」", 18, Ink, true, 2));
            train.Add(Lbl($"朝鲜原厂涂装 · {GameData.CarCount} 节客厢", 13, InkSoft, false, 2));
            int cond = GameData.GetTrainCondition();
            var cr = RowEl(Justify.SpaceBetween); cr.style.marginTop = 10;
            cr.Add(Lbl("车况", 13, InkSoft)); cr.Add(Lbl(cond >= 70 ? "状态良好" : (cond >= 40 ? "注意损耗" : "亟待大修"), 13, TrustColor(cond), true));
            train.Add(cr);
            train.Add(Bar(cond / 100f, TrustColor(cond), 8f));
            row.Add(train);

            var plan = GameData.GetCurrentDispatchPlan();
            var today = Card(16); today.style.flexGrow = 1; today.style.flexBasis = 0; today.style.marginRight = 12;
            today.Add(Lbl("今日发车方案", 14, InkSoft));
            today.Add(Lbl(plan.Name, 18, Ink, true, 2));
            today.Add(Lbl(plan.Summary, 13, InkSoft, false, 2));
            var go = PillBtn("去调整", new Color(Accent.r, Accent.g, Accent.b, 0.14f), Accent, () => { OpenPage(1, 0); Rebuild(win); }, 32);
            go.style.marginLeft = 0; go.style.marginTop = 10; go.style.alignSelf = Align.FlexStart;
            today.Add(go);
            row.Add(today);

            var quick = Card(16); quick.style.flexGrow = 1; quick.style.flexBasis = 0;
            quick.Add(Lbl("快捷入口", 14, InkSoft));
            var qr = RowEl(); qr.style.marginTop = 8;
            qr.Add(QuickTile(TabletIconKind.Map, "巡线", new Color(0.20f, 0.55f, 0.36f), () => s_launchApp?.Invoke("巡线")));
            qr.Add(QuickTile(TabletIconKind.Flag, "剧情", new Color(0.25f, 0.45f, 0.78f), () => s_openStory?.Invoke()));
            qr.Add(QuickTile(TabletIconKind.Folder, "站务", new Color(0.55f, 0.45f, 0.35f), () => s_openAffairs?.Invoke()));
            quick.Add(qr);
            row.Add(quick);
        }

        private static VisualElement QuickTile(TabletIconKind icon, string label, Color color, Action click)
        {
            var t = new VisualElement();
            t.style.width = 72; t.style.height = 72; t.style.marginRight = 10;
            t.style.alignItems = Align.Center; t.style.justifyContent = Justify.Center;
            t.style.backgroundColor = new Color(color.r, color.g, color.b, 0.13f);
            SetRadius(t, 16);
            t.pickingMode = PickingMode.Position;
            t.Add(new TabletIconElement(icon, 26, color));
            t.Add(Lbl(label, 13, color, true, 3));
            t.RegisterCallback<ClickEvent>(_ => click());
            return t;
        }

        // ---------- 总览 · 值班简报 ----------
        private static void BuildBriefPage(VisualElement win, VisualElement page)
        {
            var goal = Card(18);
            var g = GameData.CurrentGoal;
            goal.Add(Lbl("当前目标", 14, InkSoft));
            goal.Add(Lbl(g != null ? g.Title : "暂无目标", 20, Ink, true, 2));
            if (g != null)
            {
                goal.Add(Lbl(g.Description, 13, InkSoft, false, 2));
                if (g.TargetMoney > 0) { goal.Add(ProgressRow("资金", GameData.GetMoney(), g.TargetMoney, "沙币")); }
                if (g.TargetTrust > 0) { goal.Add(ProgressRow("信任", GameData.GetTrust(), g.TargetTrust, "")); }
                if (g.TargetPassengers > 0) { goal.Add(ProgressRow("客流", GameData.GetExpectedPassengers(), g.TargetPassengers, "人次")); }
                if (g.TargetCondition > 0) { goal.Add(ProgressRow("车况", GameData.GetTrainCondition(), g.TargetCondition, "%")); }
            }
            page.Add(goal);

            var todo = Card(18); todo.style.marginTop = 12;
            todo.Add(Lbl("值班待办", 16, Ink, true));
            var list = GameData.Todos;
            if (list != null && list.Count > 0)
                foreach (var t in list)
                {
                    var r = RowEl(); r.style.marginTop = 8;
                    r.Add(new TabletIconElement(TabletIconKind.Check, 18, Accent));
                    var l = Lbl(t, 14, InkSoft); l.style.marginLeft = 8; r.Add(l);
                    todo.Add(r);
                }
            else todo.Add(Lbl("暂无待办。", 13, InkMuted, false, 6));
            page.Add(todo);

            var foot = Lbl("阿里郎内网 RDA-v4.2 认证通过 · 嘉颖徐铁路基金技术支持", 11, InkMuted, false, 12);
            page.Add(foot);
        }

        private static VisualElement ProgressRow(string name, float cur, float goal, string unit)
        {
            var wrap = new VisualElement(); wrap.style.marginTop = 10;
            var r = RowEl(Justify.SpaceBetween);
            r.Add(Lbl(name, 13, InkSoft));
            r.Add(Lbl($"{cur:N0} / {goal:N0} {unit}", 13, Ink, true));
            wrap.Add(r);
            wrap.Add(Bar(goal <= 0 ? 0 : cur / goal, cur >= goal ? Good : Accent, 8f));
            return wrap;
        }

        // ---------- 调度 · 发车方案 ----------
        private static void BuildDispatchPage(VisualElement win, VisualElement page)
        {
            var row = RowEl();
            row.style.alignItems = Align.Stretch;
            page.Add(row);
            for (int i = 0; i < GameData.DispatchPlans.Length; i++)
            {
                int idx = i;
                var plan = GameData.DispatchPlans[i];
                bool sel = GameData.SelectedDispatchPlanIndex == i;
                var c = Card(18);
                c.style.flexGrow = 1; c.style.flexBasis = 0;
                c.style.marginRight = i == GameData.DispatchPlans.Length - 1 ? 0 : 12;
                if (sel) c = PaperUI.Card(18, true, PaperTokens.Dispatch);
                c.style.flexGrow = 1; c.style.flexBasis = 0;
                c.style.marginRight = i == GameData.DispatchPlans.Length - 1 ? 0 : 12;
                c.pickingMode = PickingMode.Position;
                var head = RowEl(Justify.SpaceBetween);
                head.Add(Lbl(plan.Name, 20, Ink, true));
                if (sel) head.Add(new TabletIconElement(TabletIconKind.Check, 22, Accent));
                c.Add(head);
                // 班次示意：用车厢图标数表示趟数强弱
                var trains = RowEl(); trains.style.marginTop = 8;
                int n = i == 0 ? 1 : (i == 1 ? 2 : 3);
                for (int t = 0; t < 3; t++) trains.Add(new TabletIconElement(TabletIconKind.Train, 26, t < n ? Accent : InkMuted) { style = { marginRight = 6 } });
                c.Add(trains);
                c.Add(Lbl(plan.Summary, 13, InkSoft, false, 8));
                c.Add(EffectRow("资金", plan.MoneyDelta, 200));
                c.Add(EffectRow("车况", plan.TrainConditionDelta, 10));
                c.Add(EffectRow("口碑", plan.TrustDelta, 6));
                c.Add(EffectRow("客流", plan.PassengerDelta, 10));
                c.RegisterCallback<ClickEvent>(_ =>
                {
                    int guard = GameData.DispatchPlans.Length;
                    while (GameData.SelectedDispatchPlanIndex != idx && guard-- > 0) GameData.CycleDispatchPlan();
                    Rebuild(win);
                });
                row.Add(c);
            }
            page.Add(Lbl("点击卡片选择今日发车方案；确认无误后点右下角「结束一天」。", 12, InkMuted, false, 10));
        }

        private static VisualElement EffectRow(string name, int delta, int scale)
        {
            var r = RowEl(); r.style.marginTop = 8;
            var n = Lbl(name, 13, InkSoft); n.style.width = 40; r.Add(n);
            var ruler = PaperUI.DeltaRuler(Mathf.Clamp(delta / (float)scale, -1f, 1f), Good, Bad);
            ruler.style.flexGrow = 1;
            r.Add(ruler);
            var v = Lbl((delta > 0 ? "+" : "") + delta, 13, delta >= 0 ? Good : Bad, true); v.style.width = 52; v.style.unityTextAlign = TextAnchor.MiddleRight; r.Add(v);
            return r;
        }

        // ---------- 调度 · 经营策略 ----------
        private static void BuildStrategyPage(VisualElement win, VisualElement page)
        {
            TabletIconKind[] icons = { TabletIconKind.Person, TabletIconKind.Code, TabletIconKind.Heart, TabletIconKind.Link };
            for (int s = 0; s < 4; s++)
            {
                int sid = s;
                var card = Card(16);
                card.style.marginBottom = 12;
                var row = RowEl();
                var left = new VisualElement(); left.style.width = 210;
                var title = RowEl();
                title.Add(new TabletIconElement(icons[s], 24, Accent));
                var t = Lbl(DayLoopController.StrategyTitles[s], 18, Ink, true); t.style.marginLeft = 10; title.Add(t);
                left.Add(title);
                row.Add(left);

                int cur = DayLoopController.GetStrategyValue(s);
                string[] opts = DayLoopController.StrategyOptions(s);
                var seg = BuildSegment(win, opts, cur, v => { DayLoopController.SetStrategyValue(sid, v); Rebuild(win); });
                row.Add(seg);
                card.Add(row);
                card.Add(Lbl(DayLoopController.StrategyDescription(s, cur), 13, InkSoft, false, 8));
                page.Add(card);
            }
        }

        // ---------- 人员 ----------
        private static string RoleName(string role)
        {
            switch (role)
            {
                case "driver": return "司机";
                case "mechanic": return "技师";
                case "conductor": return "站务";
                case "dispatcher": return "调度";
                case "attendant": return "见习";
                default: return role;
            }
        }

        private static Color RoleColor(string role)
        {
            switch (role)
            {
                case "driver": return new Color(0.25f, 0.45f, 0.78f);
                case "mechanic": return new Color(0.55f, 0.45f, 0.35f);
                case "conductor": return new Color(0.78f, 0.40f, 0.48f);
                case "dispatcher": return new Color(0.36f, 0.55f, 0.50f);
                default: return new Color(0.50f, 0.62f, 0.38f);
            }
        }

        private static void BuildCrewPage(VisualElement win, VisualElement page)
        {
            var crew = CrewManager.GetAllCrew();
            if (crew == null || crew.Count == 0) { page.Add(Lbl("暂无在编人员。", 14, InkMuted)); return; }
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            page.Add(grid);
            foreach (var c in crew)
            {
                var m = c;
                var card = Card(16);
                card.style.width = new Length(48.8f, LengthUnit.Percent);
                card.style.marginRight = 12; card.style.marginBottom = 12;
                var top = RowEl();
                var avatar = new VisualElement();
                avatar.style.width = 52; avatar.style.height = 52; avatar.style.flexShrink = 0;
                avatar.style.alignItems = Align.Center; avatar.style.justifyContent = Justify.Center;
                avatar.style.backgroundColor = RoleColor(m.role); SetRadius(avatar, 26);
                SetBorder(avatar, 3, PaperTokens.PaperDeep);
                avatar.Add(Lbl(string.IsNullOrEmpty(m.name) ? "?" : m.name.Substring(0, 1), 24, Color.white, true));
                top.Add(avatar);
                var info = new VisualElement(); info.style.marginLeft = 12; info.style.flexGrow = 1;
                var nr = RowEl();
                nr.Add(Lbl(m.name, 19, Ink, true));
                var tag = PaperUI.Tag(RoleName(m.role), RoleColor(m.role));
                tag.style.marginLeft = 8;
                nr.Add(tag);
                info.Add(nr);
                info.Add(Lbl(CrewManager.GetCrewStatusText(m.id), 12, InkSoft, false, 2));
                top.Add(info);
                card.Add(top);

                var fr = RowEl(Justify.SpaceBetween); fr.style.marginTop = 12;
                fr.Add(Lbl("疲劳", 13, InkSoft)); fr.Add(Lbl(m.fatigue + (m.isResting ? "（休息中）" : ""), 13, Ink, true));
                card.Add(fr);
                float f = Mathf.Clamp01(m.fatigue / 100f);
                card.Add(Bar(f, f < 0.4f ? Good : (f < 0.7f ? Warn : Bad), 8f));
                var lr = RowEl(Justify.SpaceBetween); lr.style.marginTop = 8;
                lr.Add(Lbl("忠诚", 13, InkSoft)); lr.Add(Lbl(Mathf.RoundToInt(m.loyalty).ToString(), 13, Ink, true));
                card.Add(lr);
                card.Add(Bar(Mathf.Clamp01(m.loyalty / 100f), Accent, 8f));

                var btns = RowEl(); btns.style.marginTop = 12; btns.style.justifyContent = Justify.FlexEnd;
                btns.Add(PillBtn(m.isResting ? "取消休息" : "安排休息", new Color(0f, 0f, 0f, 0.06f), Ink, () => { CrewManager.SetResting(m.id, !m.isResting); Rebuild(win); }, 32));
                btns.Add(PillBtn("培训", new Color(Accent.r, Accent.g, Accent.b, 0.14f), Accent, () =>
                {
                    bool ok = CrewManager.Train(m.id);
                    ShowToast(ok ? $"{m.name} 开始培训" : $"{m.name} 暂时无法培训（冷却中或资金不足）");
                    Rebuild(win);
                }, 32));
                card.Add(btns);
                grid.Add(card);
            }
        }

        // ---------- 订单 ----------
        private static Color OrderColor(string type)
        {
            switch (type)
            {
                case "urgent": return Bad;
                case "story": return new Color(0.55f, 0.40f, 0.78f);
                default: return new Color(0.25f, 0.45f, 0.78f);
            }
        }

        private static string OrderTypeName(string type)
        {
            switch (type) { case "freight": return "货运"; case "urgent": return "加急"; case "story": return "故事"; default: return type; }
        }

        private static void BuildOrdersPage(VisualElement win, VisualElement page)
        {
            page.Add(Lbl($"已完成 {OrderManager.GetCompletedCount()} 单", 13, InkSoft));
            var orders = OrderManager.GetActiveOrders();
            if (orders.Count == 0) { page.Add(Lbl("暂无在办订单，结束一天后会补充新的委托。", 14, InkMuted, false, 10)); return; }
            foreach (var o in orders)
            {
                var order = o;
                Color tc = OrderColor(order.type);
                var card = PaperUI.Ticket(168f, PaperTokens.PaperBg, tc);
                card.style.marginTop = 10;
                card.style.flexDirection = FlexDirection.Row;
                var main = new VisualElement(); main.style.flexGrow = 1; main.style.minWidth = 0;
                var top = RowEl();
                top.Add(PaperUI.Tag(OrderTypeName(order.type), tc));
                var title = Lbl(order.title, 19, Ink, true); title.style.marginLeft = 10; top.Add(title);
                main.Add(top);

                // 路线示意：起点 ●—— 终点（轨道线）
                var route = RowEl(); route.style.marginTop = 10;
                route.Add(Lbl(order.origin, 15, Ink, true));
                var line = PaperUI.RailDivider(12f); line.style.flexGrow = 1; line.style.marginLeft = 10; line.style.marginRight = 10;
                route.Add(line);
                route.Add(new TabletIconElement(TabletIconKind.Flag, 18, tc));
                var dest = Lbl(order.destination, 15, Ink, true); dest.style.marginLeft = 6; route.Add(dest);
                main.Add(route);
                if (!string.IsNullOrEmpty(order.description)) main.Add(Lbl(order.description, 13, InkSoft, false, 6));
                card.Add(main);

                // 票根：酬劳 + 倒计时 + 交付
                var stub = new VisualElement(); stub.style.width = 168; stub.style.flexShrink = 0; stub.style.paddingLeft = 18;
                stub.style.justifyContent = Justify.Center;
                stub.Add(Lbl($"{order.reward:N0} 沙币", 18, Accent, true));
                stub.Add(Lbl($"信任 {(order.trustDelta >= 0 ? "+" : "")}{order.trustDelta}", 13, Good, true, 2));
                float frac = order.deadlineDays <= 0 ? 0f : Mathf.Clamp01(order.DaysRemaining / (float)order.deadlineDays);
                var tr = RowEl(Justify.SpaceBetween); tr.style.marginTop = 8;
                tr.Add(Lbl("剩余", 12, InkSoft)); tr.Add(Lbl($"{order.DaysRemaining} 天", 12, frac < 0.34f ? Bad : Ink, true));
                stub.Add(tr);
                stub.Add(Bar(frac, frac < 0.34f ? Bad : (frac < 0.67f ? Warn : Good), 6f));
                var deliver = PillBtn("交付", Accent, Color.white, () =>
                {
                    OrderManager.CompleteOrder(order.id);
                    ShowToast($"已交付：{order.title}，酬劳随当日结算入账");
                    Rebuild(win);
                }, 36);
                deliver.style.marginLeft = 0; deliver.style.marginTop = 8;
                stub.Add(deliver);
                card.Add(stub);
                page.Add(card);
            }
        }

        private static VisualElement Chip(string text, Color c)
        {
            var l = Lbl(text, 13, c, true);
            l.style.backgroundColor = new Color(c.r, c.g, c.b, 0.12f);
            l.style.paddingLeft = 10; l.style.paddingRight = 10; l.style.paddingTop = 3; l.style.paddingBottom = 3;
            l.style.marginLeft = 8; SetRadius(l, 12);
            return l;
        }

        // ---------- 市场 · 沙能 ----------
        private const int SandPRCost = 2000;

        private static void BuildMarketPage(VisualElement win, VisualElement page)
        {
            var cities = WorldGen.RegionUnlockManager.GetUnlockedCityIds();
            if (cities == null || cities.Count == 0) { page.Add(Lbl("沿线尚无可观察的城镇。", 14, InkMuted)); return; }
            foreach (string cid in cities)
            {
                string id = cid;
                float p = SandRivalManager.GetPenetration(id);
                var card = Card(16); card.style.marginBottom = 12;
                var top = RowEl(Justify.SpaceBetween);
                var title = RowEl();
                title.Add(new TabletIconElement(TabletIconKind.Pin, 22, Accent));
                var n = Lbl(CityName(id), 20, Ink, true); n.style.marginLeft = 8; title.Add(n);
                top.Add(title);
                top.Add(PillBtn($"公关宣传（{SandPRCost:N0}）", new Color(Accent.r, Accent.g, Accent.b, 0.14f), Accent, () =>
                {
                    if (GameData.GetMoney() < SandPRCost) { ShowToast("资金不足，无法开展公关宣传"); return; }
                    GameData.AddMoney(-SandPRCost);
                    SandRivalManager.PlayerPR(id, SandPRCost);
                    ShowToast($"已在 {CityName(id)} 开展公关宣传");
                    Rebuild(win);
                }, 34));
                card.Add(top);
                card.Add(Lbl(DayLoopController.DescribeSandPenetration(p), 14, InkSoft, false, 6));
                // 渗透温度计：只显示定性分段，不显示数值
                var meter = new RdaMeter(p, new[] { Good, Good, Warn, Bad });
                meter.style.height = 18; meter.style.marginTop = 10;
                card.Add(meter);
                var scale = RowEl(Justify.SpaceBetween); scale.style.marginTop = 4;
                scale.Add(Lbl("影响甚微", 11, InkMuted)); scale.Add(Lbl("开始露面", 11, InkMuted)); scale.Add(Lbl("分走客源", 11, InkMuted)); scale.Add(Lbl("声势很大", 11, InkMuted));
                card.Add(scale);
                page.Add(card);
            }
        }

        // ---------- 财务 · 资金趋势 ----------
        private static void BuildFinancePage(VisualElement win, VisualElement page)
        {
            var history = GameData.GetTrendHistory();
            var vals = new List<float>(); var trust = new List<float>();
            int from = Mathf.Max(0, history.Count - 30);
            for (int i = from; i < history.Count; i++) { vals.Add(history[i].money); trust.Add(history[i].trust); }

            var card = Card(18);
            var head = RowEl(Justify.SpaceBetween);
            head.Add(Lbl("资金走势（近 30 天）", 18, Ink, true));
            head.Add(PillBtn("运营统计", new Color(Accent.r, Accent.g, Accent.b, 0.14f), Accent,
                () => TabletHistoryUI.BuildStatistics(win, () => Rebuild(win)), 32));
            card.Add(head);
            if (vals.Count < 2) card.Add(Lbl("数据不足：再经营几天后这里会出现走势曲线。", 13, InkMuted, false, 10));
            else
            {
                var chart = new RdaLineChart(vals, Accent, "沙币");
                chart.style.height = 240; chart.style.marginTop = 8;
                card.Add(chart);
            }
            page.Add(card);

            var card2 = Card(18); card2.style.marginTop = 12;
            card2.Add(Lbl("信任走势", 18, Ink, true));
            if (trust.Count >= 2)
            {
                var c2 = new RdaLineChart(trust, Good, "");
                c2.style.height = 150; c2.style.marginTop = 8;
                card2.Add(c2);
            }
            else card2.Add(Lbl("数据不足。", 13, InkMuted, false, 10));
            page.Add(card2);
        }

        // ---------- 财务 · 最近日报 ----------
        private static void BuildReportPage(VisualElement win, VisualElement page)
        {
            if (!DayLoopController.HasReport)
            {
                var c = Card(24);
                c.Add(Lbl("还没有日报", 20, Ink, true));
                c.Add(Lbl("点击右下角「结束一天」结算后，这里会保留最近一次日报。", 14, InkSoft, false, 6));
                page.Add(c);
                return;
            }
            var r = DayLoopController.LastReport;
            page.Add(BuildReportCard(r));
        }

        private static VisualElement BuildReportCard(DayLoopController.DayReport r)
        {
            var card = Card(20);
            card.Add(Lbl($"第 {r.Day} 天 · 日报", 22, Ink, true));
            card.Add(PaperUI.RailDivider(12f));
            var stamp = PaperUI.Stamp("已结算", 92f);
            stamp.style.position = Position.Absolute; stamp.style.right = 24; stamp.style.top = 14;
            card.Add(stamp);
            var nums = RowEl(); nums.style.marginTop = 10;
            nums.Add(StatPill("资金", $"{(r.MoneyDelta >= 0 ? "+" : "")}{r.MoneyDelta:N0}", r.MoneyDelta >= 0 ? Good : Bad));
            nums.Add(StatPill("现有", $"{r.MoneyAfter:N0}", Ink));
            nums.Add(StatPill("车况", $"{(r.TrainConditionDelta >= 0 ? "+" : "")}{r.TrainConditionDelta}", r.TrainConditionDelta >= 0 ? Good : Bad));
            nums.Add(StatPill("客流", $"{(r.PassengerDelta >= 0 ? "+" : "")}{r.PassengerDelta}", r.PassengerDelta >= 0 ? Good : Bad));
            card.Add(nums);
            card.Add(ReportLine(TabletIconKind.Heart, "民情", r.TrustLine));
            card.Add(ReportLine(TabletIconKind.Bolt, "财务", r.FinanceLine));
            card.Add(ReportLine(TabletIconKind.Cloud, "沙能", r.SandLine));
            card.Add(ReportLine(TabletIconKind.Shield, "上级", r.PoliticalLine));
            card.Add(ReportLine(TabletIconKind.Rails, "线路", r.InfrastructureLine));
            if (!string.IsNullOrEmpty(r.EventTitle))
            {
                var ev = new VisualElement();
                ev.style.marginTop = 14; ev.style.paddingLeft = 14; ev.style.paddingRight = 14; ev.style.paddingTop = 10; ev.style.paddingBottom = 10;
                ev.style.backgroundColor = new Color(Accent.r, Accent.g, Accent.b, 0.10f); SetRadius(ev, 14);
                ev.Add(Lbl(r.EventTitle, 16, Accent, true));
                if (!string.IsNullOrEmpty(r.EventDescription)) ev.Add(Lbl(r.EventDescription, 13, InkSoft, false, 4));
                card.Add(ev);
            }
            return card;
        }

        private static VisualElement StatPill(string name, string value, Color c)
        {
            var p = new VisualElement();
            p.style.marginRight = 12; p.style.paddingLeft = 14; p.style.paddingRight = 14; p.style.paddingTop = 6; p.style.paddingBottom = 6;
            p.style.backgroundColor = new Color(0f, 0f, 0f, 0.04f); SetRadius(p, 12);
            p.Add(Lbl(name, 12, InkSoft));
            p.Add(Lbl(value, 18, c, true));
            return p;
        }

        private static VisualElement ReportLine(TabletIconKind icon, string label, string text)
        {
            var r = RowEl(); r.style.marginTop = 10;
            r.Add(new TabletIconElement(icon, 20, Accent));
            var l = Lbl(label, 14, InkSoft); l.style.width = 48; l.style.marginLeft = 8; r.Add(l);
            r.Add(Lbl(text, 15, Ink));
            return r;
        }

        // ================= 结束一天 =================
        private static VisualElement BuildEndDayButton(VisualElement win)
        {
            var b = new Button(() =>
            {
                var report = DayLoopController.EndDay();
                OnDayEnded?.Invoke();
                Rebuild(win);
                ShowDayReport(win, report);
            }) { name = "rda-end-day", text = $"结束第 {GameData.GetDay()} 天 · 结算" };
            b.style.position = Position.Absolute;
            b.style.right = 8; b.style.bottom = 14;
            b.style.height = 54; b.style.fontSize = 18; b.style.unityFontDefinition = FdBold();
            b.style.color = Color.white; b.style.backgroundColor = new Color(0.86f, 0.52f, 0.18f, 1f);
            b.style.paddingLeft = 28; b.style.paddingRight = 28;
            SetRadius(b, 27);
            SetBorder(b, 1, new Color(0.3f, 0.15f, 0.04f, 0.35f));
            b.generateVisualContent += ctx => PaperDraw.Glossy(ctx.painter2D, b.layout.width, b.layout.height, 27f);
            return b;
        }

        /// <summary>日报弹层。</summary>
        private static void ShowDayReport(VisualElement win, DayLoopController.DayReport rep)
        {
            var overlay = new VisualElement { name = "rda-day-report" };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.right = 0; overlay.style.top = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0.08f, 0.07f, 0.05f, 0.45f);
            overlay.style.alignItems = Align.Center; overlay.style.justifyContent = Justify.Center;
            overlay.pickingMode = PickingMode.Position;
            win.Add(overlay);

            var wrap = new VisualElement();
            wrap.style.width = new Length(62, LengthUnit.Percent);
            overlay.Add(wrap);
            wrap.Add(BuildReportCard(rep));
            var ok = PillBtn("知道了", Accent, Color.white, () => overlay.RemoveFromHierarchy(), 46);
            ok.style.marginLeft = 0; ok.style.marginTop = 12; ok.style.alignSelf = Align.FlexEnd; ok.style.paddingLeft = 36; ok.style.paddingRight = 36;
            wrap.Add(ok);
        }
    }

    // ===================================================================
    //  图形组件
    // ===================================================================

    /// <summary>环形仪表：轨道 + 进度弧 + 中心数字。</summary>
    public sealed class RdaRing : VisualElement
    {
        private readonly float fraction;
        private readonly Color color;
        private readonly Label center = new Label();
        public string CenterText { set { center.text = value; } }

        public RdaRing(float v, Color c)
        {
            fraction = Mathf.Clamp01(v); color = c;
            pickingMode = PickingMode.Ignore;
            center.style.position = Position.Absolute;
            center.style.left = 0; center.style.right = 0; center.style.top = 0; center.style.bottom = 0;
            center.style.unityTextAlign = TextAnchor.MiddleCenter;
            center.style.fontSize = 18; center.style.color = new Color(0.11f, 0.12f, 0.15f);
            center.style.unityFontDefinition = FdBold();
            Add(center);
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            float size = Mathf.Min(r.width, r.height);
            if (size <= 1f) return;
            var p = ctx.painter2D;
            Vector2 c = r.center;
            float rad = size / 2f - 7f;
            p.lineCap = LineCap.Round;
            p.lineWidth = 9f;
            p.strokeColor = new Color(0f, 0f, 0f, 0.08f);
            p.BeginPath(); p.Arc(c, rad, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
            if (fraction > 0.005f)
            {
                p.strokeColor = color;
                p.BeginPath(); p.Arc(c, rad, Angle.Degrees(-90), Angle.Degrees(-90 + 360f * fraction)); p.Stroke();
            }
        }
    }

    /// <summary>迷你折线（无坐标轴）。</summary>
    public sealed class RdaSpark : VisualElement
    {
        private readonly List<float> vals; private readonly Color color;
        public RdaSpark(List<float> v, Color c)
        {
            vals = v; color = c; pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (vals == null || vals.Count < 2 || r.width < 4f) return;
            float min = float.MaxValue, max = float.MinValue;
            foreach (float f in vals) { min = Mathf.Min(min, f); max = Mathf.Max(max, f); }
            if (Mathf.Approximately(min, max)) { min -= 1f; max += 1f; }
            var p = ctx.painter2D;
            var pts = new List<Vector2>();
            for (int i = 0; i < vals.Count; i++)
                pts.Add(new Vector2(r.width * i / (vals.Count - 1), r.height - (vals[i] - min) / (max - min) * (r.height - 6f) - 3f));
            p.fillColor = new Color(color.r, color.g, color.b, 0.14f);
            p.BeginPath(); p.MoveTo(new Vector2(pts[0].x, r.height));
            foreach (var pt in pts) p.LineTo(pt);
            p.LineTo(new Vector2(pts[pts.Count - 1].x, r.height)); p.ClosePath(); p.Fill();
            p.strokeColor = color; p.lineWidth = 2.2f; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            p.BeginPath(); p.MoveTo(pts[0]);
            for (int i = 1; i < pts.Count; i++) p.LineTo(pts[i]);
            p.Stroke();
        }
    }

    /// <summary>带网格与首尾标注的折线图。</summary>
    public sealed class RdaLineChart : VisualElement
    {
        private readonly List<float> vals; private readonly Color color;
        private readonly Label maxL = new Label(), minL = new Label(), lastL = new Label();

        public RdaLineChart(List<float> v, Color c, string unit)
        {
            vals = v; color = c; pickingMode = PickingMode.Ignore;
            float min = float.MaxValue, max = float.MinValue;
            foreach (float f in vals) { min = Mathf.Min(min, f); max = Mathf.Max(max, f); }
            Style(maxL, $"{max:N0}{(unit.Length > 0 ? " " + unit : "")}"); maxL.style.left = 4; maxL.style.top = 2;
            Style(minL, $"{min:N0}"); minL.style.left = 4; minL.style.bottom = 2;
            Style(lastL, $"现在 {vals[vals.Count - 1]:N0}"); lastL.style.right = 6; lastL.style.top = 2; lastL.style.color = c;
            Add(maxL); Add(minL); Add(lastL);
            generateVisualContent += Draw;
        }

        private static void Style(Label l, string t)
        {
            l.text = t; l.pickingMode = PickingMode.Ignore;
            l.style.position = Position.Absolute; l.style.fontSize = 12;
            l.style.color = new Color(0.45f, 0.47f, 0.52f); l.style.unityFontDefinition = Fd();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (vals == null || vals.Count < 2 || r.width < 10f) return;
            var p = ctx.painter2D;
            float padL = 8f, padT = 22f, padB = 14f;
            float w = r.width - padL - 8f, h = r.height - padT - padB;
            float min = float.MaxValue, max = float.MinValue;
            foreach (float f in vals) { min = Mathf.Min(min, f); max = Mathf.Max(max, f); }
            if (Mathf.Approximately(min, max)) { min -= 1f; max += 1f; }
            p.strokeColor = new Color(0f, 0f, 0f, 0.07f); p.lineWidth = 1f;
            for (int i = 0; i <= 4; i++)
            {
                float y = padT + h * i / 4f;
                p.BeginPath(); p.MoveTo(new Vector2(padL, y)); p.LineTo(new Vector2(padL + w, y)); p.Stroke();
            }
            var pts = new List<Vector2>();
            for (int i = 0; i < vals.Count; i++)
                pts.Add(new Vector2(padL + w * i / (vals.Count - 1), padT + h - (vals[i] - min) / (max - min) * h));
            p.fillColor = new Color(color.r, color.g, color.b, 0.13f);
            p.BeginPath(); p.MoveTo(new Vector2(pts[0].x, padT + h));
            foreach (var pt in pts) p.LineTo(pt);
            p.LineTo(new Vector2(pts[pts.Count - 1].x, padT + h)); p.ClosePath(); p.Fill();
            p.strokeColor = color; p.lineWidth = 3f; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            p.BeginPath(); p.MoveTo(pts[0]);
            for (int i = 1; i < pts.Count; i++) p.LineTo(pts[i]);
            p.Stroke();
            p.fillColor = Color.white; p.BeginPath(); p.Arc(pts[pts.Count - 1], 6f, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
            p.fillColor = color; p.BeginPath(); p.Arc(pts[pts.Count - 1], 4f, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
        }
    }

    /// <summary>分段温度计：定性展示渗透程度，不显示数值。</summary>
    public sealed class RdaMeter : VisualElement
    {
        private readonly float value; private readonly Color[] segs;
        public RdaMeter(float v, Color[] colors) { value = Mathf.Clamp01(v); segs = colors; pickingMode = PickingMode.Ignore; generateVisualContent += Draw; }

        private void Draw(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (r.width < 10f) return;
            var p = ctx.painter2D;
            float gap = 4f, sw = (r.width - gap * (segs.Length - 1)) / segs.Length;
            for (int i = 0; i < segs.Length; i++)
            {
                float x = i * (sw + gap);
                float segStart = i / (float)segs.Length, segEnd = (i + 1) / (float)segs.Length;
                float fill = Mathf.Clamp01((value - segStart) / (segEnd - segStart));
                p.fillColor = new Color(0f, 0f, 0f, 0.08f);
                p.BeginPath(); p.MoveTo(new Vector2(x, 0)); p.LineTo(new Vector2(x + sw, 0)); p.LineTo(new Vector2(x + sw, r.height)); p.LineTo(new Vector2(x, r.height)); p.ClosePath(); p.Fill();
                if (fill > 0f)
                {
                    p.fillColor = segs[i];
                    p.BeginPath(); p.MoveTo(new Vector2(x, 0)); p.LineTo(new Vector2(x + sw * fill, 0)); p.LineTo(new Vector2(x + sw * fill, r.height)); p.LineTo(new Vector2(x, r.height)); p.ClosePath(); p.Fill();
                }
            }
        }
    }

    /// <summary>线路示意图：站点节点 + 运营实线/待修复虚线 + 往返行驶的列车。</summary>
    public sealed class RdaRouteMap : VisualElement
    {
        private readonly List<string> names; private readonly List<float> km; private readonly List<bool> open;
        private readonly Color accent, ink, muted;
        private readonly List<Label> labels = new List<Label>();
        private readonly List<Label> kms = new List<Label>();

        public RdaRouteMap(List<string> n, List<float> k, List<bool> o, Color a, Color i, Color m)
        {
            names = n; km = k; open = o; accent = a; ink = i; muted = m;
            pickingMode = PickingMode.Ignore;
            for (int x = 0; x < names.Count; x++)
            {
                var l = new Label(names[x]); l.pickingMode = PickingMode.Ignore;
                l.style.position = Position.Absolute; l.style.width = 90; l.style.unityTextAlign = TextAnchor.MiddleCenter;
                l.style.fontSize = 16; l.style.color = open[x] ? ink : muted; l.style.unityFontDefinition = FdBold();
                Add(l); labels.Add(l);
                var kl = new Label($"{km[x]:0.#} km" + (open[x] ? "" : " · 待修复")); kl.pickingMode = PickingMode.Ignore;
                kl.style.position = Position.Absolute; kl.style.width = 120; kl.style.unityTextAlign = TextAnchor.MiddleCenter;
                kl.style.fontSize = 12; kl.style.color = muted; kl.style.unityFontDefinition = Fd();
                Add(kl); kms.Add(kl);
            }
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => Layout());
            schedule.Execute(() => { if (panel != null && !TabletTheme.ReduceMotion) MarkDirtyRepaint(); }).Every(50);
        }

        private float StationX(Rect r, int i)
        {
            float pad = 70f, max = km[km.Count - 1];
            return pad + (km[i] / max) * (r.width - pad * 2f);
        }

        private void Layout()
        {
            Rect r = contentRect;
            for (int i = 0; i < names.Count; i++)
            {
                float x = StationX(r, i);
                labels[i].style.left = x - 45f; labels[i].style.top = r.height * 0.62f;
                kms[i].style.left = x - 60f; kms[i].style.top = r.height * 0.62f + 24f;
            }
        }

        private void Draw(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (r.width < 50f) return;
            var p = ctx.painter2D;
            float y = r.height * 0.42f;
            p.lineCap = LineCap.Round;

            // 线路：相邻两站均开通 = 实线，否则虚线
            for (int i = 0; i < names.Count - 1; i++)
            {
                float x0 = StationX(r, i), x1 = StationX(r, i + 1);
                bool solid = open[i] && open[i + 1];
                if (solid)
                {
                    p.strokeColor = accent; p.lineWidth = 7f;
                    p.BeginPath(); p.MoveTo(new Vector2(x0, y)); p.LineTo(new Vector2(x1, y)); p.Stroke();
                    p.strokeColor = new Color(1f, 1f, 1f, 0.55f); p.lineWidth = 1.5f;
                    for (float sx = x0 + 10f; sx < x1 - 6f; sx += 16f) { p.BeginPath(); p.MoveTo(new Vector2(sx, y - 3f)); p.LineTo(new Vector2(sx, y + 3f)); p.Stroke(); }
                }
                else
                {
                    p.strokeColor = new Color(0f, 0f, 0f, 0.22f); p.lineWidth = 5f;
                    for (float sx = x0; sx < x1; sx += 18f)
                    {
                        p.BeginPath(); p.MoveTo(new Vector2(sx, y)); p.LineTo(new Vector2(Mathf.Min(sx + 9f, x1), y)); p.Stroke();
                    }
                }
            }

            // 站点
            for (int i = 0; i < names.Count; i++)
            {
                float x = StationX(r, i);
                p.fillColor = Color.white;
                p.BeginPath(); p.Arc(new Vector2(x, y), 14f, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
                p.strokeColor = open[i] ? accent : new Color(0f, 0f, 0f, 0.28f); p.lineWidth = 4f;
                p.BeginPath(); p.Arc(new Vector2(x, y), 14f, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
                if (open[i])
                {
                    p.fillColor = accent;
                    p.BeginPath(); p.Arc(new Vector2(x, y), 5f, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
                }
            }

            // 列车：在已通运营段内往返
            int lastOpen = 0;
            for (int i = 0; i < open.Count; i++) if (open[i]) lastOpen = i; else break;
            if (lastOpen > 0)
            {
                float a = StationX(r, 0), b = StationX(r, lastOpen);
                float t = TabletTheme.ReduceMotion ? 0.35f : 0.5f - 0.5f * Mathf.Cos(Time.realtimeSinceStartup * 0.5f);
                float x = Mathf.Lerp(a, b, t);
                float w = 62f, h = 22f;
                p.fillColor = new Color(0f, 0f, 0f, 0.18f);
                p.BeginPath(); p.MoveTo(new Vector2(x - w / 2, y - 22f + h + 4f)); p.LineTo(new Vector2(x + w / 2, y - 22f + h + 4f)); p.LineTo(new Vector2(x + w / 2, y - 22f + h + 8f)); p.LineTo(new Vector2(x - w / 2, y - 22f + h + 8f)); p.ClosePath(); p.Fill();
                p.fillColor = new Color(0.16f, 0.30f, 0.52f);
                p.BeginPath(); p.MoveTo(new Vector2(x - w / 2, y - 40f)); p.LineTo(new Vector2(x + w / 2, y - 40f)); p.LineTo(new Vector2(x + w / 2, y - 18f)); p.LineTo(new Vector2(x - w / 2, y - 18f)); p.ClosePath(); p.Fill();
                p.fillColor = new Color(0.86f, 0.93f, 0.98f);
                p.BeginPath(); p.MoveTo(new Vector2(x - w / 2 + 6, y - 36f)); p.LineTo(new Vector2(x - w / 2 + 20, y - 36f)); p.LineTo(new Vector2(x - w / 2 + 20, y - 26f)); p.LineTo(new Vector2(x - w / 2 + 6, y - 26f)); p.ClosePath();
                p.MoveTo(new Vector2(x - w / 2 + 26, y - 36f)); p.LineTo(new Vector2(x - w / 2 + 40, y - 36f)); p.LineTo(new Vector2(x - w / 2 + 40, y - 26f)); p.LineTo(new Vector2(x - w / 2 + 26, y - 26f)); p.ClosePath(); p.Fill();
            }
        }
    }
}
