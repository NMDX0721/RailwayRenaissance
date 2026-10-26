using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI
{
    /// <summary>
    /// [T304] 平板系统宏观走势面板（五条趋势线独立看板）。
    /// 支持信任/财政/沙能/政治/老化五线同屏对比，具备日/月/年三档聚合视图，
    /// 遵循复古像素与小米澎湃平板 UI 视觉风格，零外部图片依赖。
    /// </summary>
    public static class TabletTrendDashboardUI
    {
        public enum ViewSpan
        {
            Daily,   // 近 30 天
            Monthly, // 近 12 个月聚合
            Yearly   // 360 天全周期
        }

        private static ViewSpan currentSpan = ViewSpan.Daily;
        private static Font gameFont;

        // 五线统一色谱（复古高对比像素配色）
        private static readonly Color ColorTrust = new Color(0.25f, 0.90f, 0.65f, 1f);       // 青绿：信任
        private static readonly Color ColorMoney = new Color(1.00f, 0.82f, 0.28f, 1f);       // 金黄：财政
        private static readonly Color ColorSand = new Color(0.85f, 0.40f, 0.95f, 1f);        // 紫粉：沙能
        private static readonly Color ColorPolitics = new Color(1.00f, 0.35f, 0.25f, 1f);    // 橙红：政治
        private static readonly Color ColorDecay = new Color(0.55f, 0.70f, 0.85f, 1f);       // 铁蓝：老化

        private static readonly Color ColorBgDark = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        private static readonly Color ColorCardBg = new Color(0.14f, 0.14f, 0.20f, 0.85f);
        private static readonly Color ColorBorder = new Color(0.25f, 0.25f, 0.35f, 0.60f);
        private static readonly Color ColorTextDim = new Color(0.65f, 0.65f, 0.75f, 1f);

        private static FontDefinition Fd()
        {
            if (gameFont == null)
                gameFont = Resources.Load<Font>("Fonts/zpix");
            return new FontDefinition { font = gameFont };
        }

        /// <summary>入口：挂载到 GameMainUI 的 App 窗口容器中。</summary>
        public static void Build(VisualElement container)
        {
            container.Clear();
            container.style.flexDirection = FlexDirection.Column;
            container.style.flexGrow = 1;
            container.style.backgroundColor = ColorBgDark;
            container.style.paddingLeft = 18;
            container.style.paddingRight = 18;
            container.style.paddingTop = 14;
            container.style.paddingBottom = 14;

            // 1. 顶部控制栏（标题 + 档位切换 + 实时指标摘要）
            var header = BuildHeaderBar(container);
            container.Add(header);

            // 2. 状态速览卡片栏（政治压力制裁评级 + 线路三分项老化详情）
            var statusCards = BuildStatusOverview();
            container.Add(statusCards);

            // 3. 主图表区域（图例 + 折线坐标系 + 数据气泡）
            var chartContainer = BuildChartCanvas();
            container.Add(chartContainer);
        }

        private static VisualElement BuildHeaderBar(VisualElement rootContainer)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 12;

            var titleCol = new VisualElement();
            var title = new Label("宏观五线态势研判看板 · 动力学演化监控");
            title.style.fontSize = 18;
            title.style.color = Color.white;
            title.style.unityFontDefinition = Fd();
            titleCol.Add(title);

            var subtitle = new Label("基于千里马创世核周期波形 · 威布尔轨道物理损伤 · 诺德豪斯非对称政治博弈");
            subtitle.style.fontSize = 11;
            subtitle.style.color = ColorTextDim;
            subtitle.style.unityFontDefinition = Fd();
            subtitle.style.marginTop = 2;
            titleCol.Add(subtitle);

            row.Add(titleCol);

            // 视图切换按钮组
            var btnGroup = new VisualElement();
            btnGroup.style.flexDirection = FlexDirection.Row;
            btnGroup.style.backgroundColor = ColorCardBg;
            btnGroup.style.borderTopLeftRadius = 8; btnGroup.style.borderTopRightRadius = 8;
            btnGroup.style.borderBottomLeftRadius = 8; btnGroup.style.borderBottomRightRadius = 8;
            btnGroup.style.paddingLeft = 4; btnGroup.style.paddingRight = 4;
            btnGroup.style.paddingTop = 4; btnGroup.style.paddingBottom = 4;

            btnGroup.Add(CreateSpanButton("日视图 (30天)", ViewSpan.Daily, rootContainer));
            btnGroup.Add(CreateSpanButton("月走势 (12月)", ViewSpan.Monthly, rootContainer));
            btnGroup.Add(CreateSpanButton("年周期 (360天)", ViewSpan.Yearly, rootContainer));

            row.Add(btnGroup);
            return row;
        }

        private static Button CreateSpanButton(string label, ViewSpan span, VisualElement rootContainer)
        {
            var btn = new Button();
            btn.text = label;
            btn.style.unityFontDefinition = Fd();
            btn.style.fontSize = 12;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.paddingTop = 4;
            btn.style.paddingBottom = 4;
            btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
            btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;

            bool isSelected = currentSpan == span;
            btn.style.backgroundColor = isSelected ? new Color(0.28f, 0.45f, 0.70f, 0.90f) : Color.clear;
            btn.style.color = isSelected ? Color.white : ColorTextDim;

            btn.clicked += () =>
            {
                currentSpan = span;
                Build(rootContainer);
            };
            return btn;
        }

        private static VisualElement BuildStatusOverview()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 12;

            // 卡片 1：政治监管状态
            var cardPolitics = CreateCard("🏛 政治监管制裁态势", 32);
            var tier = GameData.GetSanctionTierName();
            var polVal = GameData.GetPoliticalPressure();
            // 外显红线（2026-10-17 用户拍板）：政治压力只显监管评级档，禁止显示指数数值/百分比
            var polLabel = new Label($"[{tier}]");
            polLabel.style.fontSize = 13;
            polLabel.style.color = polVal >= 0.70f ? ColorPolitics : Color.white;
            polLabel.style.unityFontDefinition = Fd();
            cardPolitics.Add(polLabel);

            // 描述句严格对齐现实监管反馈公文语（警示→约谈→挂牌督办→停产停业整顿，见 DAILY_SETTLEMENT_FORMULAS.md §六）
            var polDesc = new Label(polVal >= 0.95f ? "责令停产停业整顿，启动运营许可听证：全线停运，接管程序进行中。" :
                                   polVal >= 0.85f ? "重大隐患挂牌督办：减班限速运行，运营补贴核减50%。" :
                                   polVal >= 0.70f ? "市政府约谈负责人：责令限期提交整改方案，运营补贴核减25%。" :
                                   polVal >= 0.45f ? "予以安全生产警示通报，责令限期消除设备隐患。" :
                                   "安全生产形势平稳，考核优良：运营补贴上浮10%。");
            polDesc.style.fontSize = 11;
            polDesc.style.color = ColorTextDim;
            polDesc.style.unityFontDefinition = Fd();
            polDesc.style.marginTop = 3;
            cardPolitics.Add(polDesc);
            row.Add(cardPolitics);

            // 卡片 2：线路基础设施分项老化
            var cardDecay = CreateCard("🛤 全线路网物理损伤分项", 48);
            var totalDecay = GameData.GetInfrastructureDecay();
            // 外显红线：老化只显定性档位词（工务定期检定评级），禁显百分比，禁实时翻牌（T308 §8.4）
            var decaySummary = new Label($"综合线网物理状态: {GameData.GetDecayGradeWord()}");
            decaySummary.style.fontSize = 13;
            decaySummary.style.color = totalDecay >= 0.60f ? ColorPolitics : ColorDecay;
            decaySummary.style.unityFontDefinition = Fd();
            cardDecay.Add(decaySummary);

            var decaySub = new Label($"路基: {GameData.GetDecayGradeRoadbedWord()}  |  桥隧: {GameData.GetDecayGradeBridgeWord()}  |  信号: {GameData.GetDecayGradeSignalWord()}");
            decaySub.style.fontSize = 11;
            decaySub.style.color = ColorTextDim;
            decaySub.style.unityFontDefinition = Fd();
            decaySub.style.marginTop = 3;
            cardDecay.Add(decaySub);
            row.Add(cardDecay);

            // 卡片 3：五线瞬时指标表
            var cardInstant = CreateCard("📊 经营指标中枢", 20);
            var instText = new Label($"民意信任: {GameData.Trust} / 100\n可用资金: {GameData.Money} 沙币");
            instText.style.fontSize = 12;
            instText.style.color = Color.white;
            instText.style.unityFontDefinition = Fd();
            cardInstant.Add(instText);
            row.Add(cardInstant);

            return row;
        }

        // 现实用语（《铁路线路修理规则》评定/修程：状态合格→经常保养→临时补修→评定失格）；外显红线：禁数值；阈值必须与 DAILY_SETTLEMENT_FORMULAS.md §六 一致
        private static string DecayLevelWord(float d)
        {
            if (d >= 0.75f) return "评定失格";
            if (d >= 0.55f) return "临时补修";
            if (d >= 0.30f) return "经常保养";
            return "状态合格";
        }

        private static VisualElement CreateCard(string title, float widthPercent)
        {
            var card = new VisualElement();
            card.style.width = new Length(widthPercent, LengthUnit.Percent);
            card.style.backgroundColor = ColorCardBg;
            card.style.borderTopLeftRadius = 10; card.style.borderTopRightRadius = 10;
            card.style.borderBottomLeftRadius = 10; card.style.borderBottomRightRadius = 10;
            card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor = ColorBorder; card.style.borderBottomColor = ColorBorder;
            card.style.borderLeftColor = ColorBorder; card.style.borderRightColor = ColorBorder;
            card.style.paddingLeft = 12; card.style.paddingRight = 12;
            card.style.paddingTop = 8; card.style.paddingBottom = 8;
            card.style.marginRight = 8;

            var t = new Label(title);
            t.style.fontSize = 11;
            t.style.color = ColorTextDim;
            t.style.unityFontDefinition = Fd();
            t.style.marginBottom = 4;
            card.Add(t);
            return card;
        }

        private static VisualElement BuildChartCanvas()
        {
            var box = new VisualElement();
            box.style.flexGrow = 1;
            box.style.backgroundColor = ColorCardBg;
            box.style.borderTopLeftRadius = 12; box.style.borderTopRightRadius = 12;
            box.style.borderBottomLeftRadius = 12; box.style.borderBottomRightRadius = 12;
            box.style.borderTopWidth = 1; box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1; box.style.borderRightWidth = 1;
            box.style.borderTopColor = ColorBorder; box.style.borderBottomColor = ColorBorder;
            box.style.borderLeftColor = ColorBorder; box.style.borderRightColor = ColorBorder;
            box.style.paddingLeft = 14; box.style.paddingRight = 14;
            box.style.paddingTop = 10; box.style.paddingBottom = 10;
            box.style.flexDirection = FlexDirection.Column;

            // 1. 图例栏
            var legend = new VisualElement();
            legend.style.flexDirection = FlexDirection.Row;
            legend.style.justifyContent = Justify.FlexEnd;
            legend.style.marginBottom = 8;

            legend.Add(CreateLegendItem("● 民众信任 (0-100)", ColorTrust));
            legend.Add(CreateLegendItem("● 财政资金 (标度化)", ColorMoney));
            legend.Add(CreateLegendItem("● 沙能渗透率 (0-100%)", ColorSand));
            legend.Add(CreateLegendItem("● 政治压力（态势）", ColorPolitics));
            legend.Add(CreateLegendItem("● 线路老化（趋势）", ColorDecay));
            box.Add(legend);

            // 2. 坐标图区域
            var graphArea = new VisualElement();
            graphArea.style.flexGrow = 1;
            graphArea.style.position = Position.Relative;
            graphArea.style.overflow = Overflow.Hidden;

            // 悬停提示气泡（当前为死元素；外显红线：若未来启用，只准显示日期/评级档，禁显政治压力与老化数值）
            var tooltip = new Label("");
            tooltip.style.position = Position.Absolute;
            tooltip.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.90f);
            tooltip.style.color = Color.white;
            tooltip.style.fontSize = 11;
            tooltip.style.unityFontDefinition = Fd();
            tooltip.style.paddingLeft = 8; tooltip.style.paddingRight = 8;
            tooltip.style.paddingTop = 4; tooltip.style.paddingBottom = 4;
            tooltip.style.borderTopLeftRadius = 4; tooltip.style.borderTopRightRadius = 4;
            tooltip.style.borderBottomLeftRadius = 4; tooltip.style.borderBottomRightRadius = 4;
            tooltip.style.display = DisplayStyle.None;
            graphArea.Add(tooltip);

            // 绘制网格与折线
            graphArea.generateVisualContent += ctx => DrawChart(ctx, tooltip);
            box.Add(graphArea);

            return box;
        }

        private static VisualElement CreateLegendItem(string label, Color color)
        {
            var l = new Label(label);
            l.style.fontSize = 11;
            l.style.color = color;
            l.style.unityFontDefinition = Fd();
            l.style.marginLeft = 14;
            return l;
        }

        private static void DrawChart(MeshGenerationContext ctx, Label tooltip)
        {
            var painter = ctx.painter2D;
            var w = ctx.visualElement.contentRect.width;
            var h = ctx.visualElement.contentRect.height;
            if (w < 50 || h < 50) return;

            // 绘制背景标线 (25%, 50%, 75%, 100%)
            painter.strokeColor = new Color(0.2f, 0.2f, 0.28f, 0.5f);
            painter.lineWidth = 1f;

            for (int i = 0; i <= 4; i++)
            {
                float y = h - (h * (i / 4.0f));
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(w, y));
                painter.Stroke();
            }

            // 获取过滤聚合后的数据点列表
            var rawHistory = GameData.GetTrendHistory();
            if (rawHistory == null || rawHistory.Count == 0)
            {
                // 若空则生成当前状态基准单点
                rawHistory = new List<GameData.TrendSnapshot>
                {
                    new GameData.TrendSnapshot
                    {
                        day = GameData.Day,
                        trust = GameData.Trust,
                        money = GameData.Money,
                        sandPenetration = 0.15f,
                        politicalPressure = GameData.PoliticalPressure,
                        infrastructureDecay = GameData.InfrastructureDecay,
                        politicalTier = GameData.PoliticalTier,
                        decayGrade = GameData.DecayGrade
                    }
                };
            }

            var points = AggregateData(rawHistory, currentSpan);
            int count = points.Count;
            if (count == 0) return;

            // 绘制五条折线
            DrawSingleLine(painter, w, h, points, p => p.trust / 100f, ColorTrust);
            DrawSingleLine(painter, w, h, points, p => Mathf.Clamp01(p.money / 100000f), ColorMoney);
            DrawSingleLine(painter, w, h, points, p => p.sandPenetration, ColorSand);
            DrawCategoricalLine(painter, w, h, points, p => p.politicalTier, 4, ColorPolitics);
            DrawCategoricalLine(painter, w, h, points, p => p.decayGrade, 3, ColorDecay);
        }

        private static void DrawCategoricalLine(Painter2D painter, float w, float h, List<GameData.TrendSnapshot> list, Func<GameData.TrendSnapshot, int> levelSelector, int maxLevel, Color color)
        {
            if (list.Count == 0 || maxLevel <= 0) return;
            painter.strokeColor = color;
            painter.lineWidth = 2.2f;
            painter.BeginPath();

            float previousY = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                float x = list.Count == 1 ? w * 0.5f : (w / (list.Count - 1)) * i;
                float level = Mathf.Clamp(levelSelector(list[i]), 0, maxLevel) / (float)maxLevel;
                float y = h - (level * (h - 20f)) - 10f;
                if (i == 0)
                {
                    painter.MoveTo(new Vector2(x, y));
                }
                else
                {
                    // 阶梯只表示离散监管/工务档位，不把内部连续量映射到图形位置。
                    painter.LineTo(new Vector2(x, previousY));
                    painter.LineTo(new Vector2(x, y));
                }
                previousY = y;
            }

            if (list.Count == 1)
                painter.LineTo(new Vector2(w, previousY));
            painter.Stroke();
        }

        private static void DrawSingleLine(Painter2D painter, float w, float h, List<GameData.TrendSnapshot> list, Func<GameData.TrendSnapshot, float> valSelector, Color color)
        {
            if (list.Count == 0) return;
            painter.strokeColor = color;
            painter.lineWidth = 2.2f;
            painter.BeginPath();

            if (list.Count == 1)
            {
                float y = h - Mathf.Clamp01(valSelector(list[0])) * (h - 10f) - 5f;
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(w, y));
                painter.Stroke();
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                float x = (w / (list.Count - 1)) * i;
                float norm = Mathf.Clamp01(valSelector(list[i]));
                float y = h - (norm * (h - 20f)) - 10f;

                if (i == 0)
                    painter.MoveTo(new Vector2(x, y));
                else
                    painter.LineTo(new Vector2(x, y));
            }
            painter.Stroke();
        }

        private static List<GameData.TrendSnapshot> AggregateData(List<GameData.TrendSnapshot> source, ViewSpan span)
        {
            if (source.Count <= 1) return source;

            switch (span)
            {
                case ViewSpan.Daily:
                    // 取最近 30 天
                    int takeCount = Mathf.Min(30, source.Count);
                    return source.GetRange(source.Count - takeCount, takeCount);

                case ViewSpan.Monthly:
                    // 按 30 天为一个自然月做加权均值聚合
                    var monthly = new List<GameData.TrendSnapshot>();
                    int chunkSize = 30;
                    for (int i = 0; i < source.Count; i += chunkSize)
                    {
                        int len = Mathf.Min(chunkSize, source.Count - i);
                        float tSum = 0, mSum = 0, sSum = 0, pSum = 0, dSum = 0;
                        int lastDay = source[i + len - 1].day;
                        for (int j = 0; j < len; j++)
                        {
                            var s = source[i + j];
                            tSum += s.trust;
                            mSum += s.money;
                            sSum += s.sandPenetration;
                            pSum += s.politicalPressure;
                            dSum += s.infrastructureDecay;
                        }
                        monthly.Add(new GameData.TrendSnapshot
                        {
                            day = lastDay,
                            trust = tSum / len,
                            money = Mathf.RoundToInt(mSum / len),
                            sandPenetration = sSum / len,
                            politicalPressure = pSum / len,
                            infrastructureDecay = dSum / len,
                            politicalTier = source[i + len - 1].politicalTier,
                            decayGrade = source[i + len - 1].decayGrade
                        });
                    }
                    return monthly;

                case ViewSpan.Yearly:
                default:
                    // 年视图：全量返回或以 15 天步长下采样
                    if (source.Count <= 24) return source;
                    var yearly = new List<GameData.TrendSnapshot>();
                    int step = Mathf.Max(1, source.Count / 24);
                    for (int i = 0; i < source.Count; i += step)
                    {
                        yearly.Add(source[i]);
                    }
                    if (!yearly.Contains(source[source.Count - 1]))
                        yearly.Add(source[source.Count - 1]);
                    return yearly;
            }
        }
    }
}
