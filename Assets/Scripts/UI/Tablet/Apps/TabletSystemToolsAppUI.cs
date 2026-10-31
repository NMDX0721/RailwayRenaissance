using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Globalization;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板轻量系统工具集（时钟、日历、天气、计算器、米家遥测、安全、网络、应用商店、地图与占位等）
    /// 从 GameMainUI 剥离（T306 Batch 1-D）
    /// </summary>
    public static class TabletSystemToolsAppUI
    {
    private static Action<string> s_showToast;

    private static void ShowToast(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        if (s_showToast != null) s_showToast(msg);
        else Debug.Log("[SystemTools] " + msg);
    }

    public static void SetToastHandler(Action<string> showToast)
    {
        s_showToast = showToast;
    }

    public static bool IsStopwatchRunning => s_isStopwatchRunning;
    private static bool s_isStopwatchRunning = false;
    private static float s_stopwatchElapsed = 0f;

    public static void ToggleStopwatch()
    {
        s_isStopwatchRunning = !s_isStopwatchRunning;
        if (s_isStopwatchRunning)
        {
            RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.Instance?.RegisterSession(new RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandSession
            {
                AppType = RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandAppType.Timer,
                Priority = 60,
                Title = "区间行车测时秒表",
                Subtitle = "雾峰至茶山单程区间时分",
                Metric = FormatTime(s_stopwatchElapsed),
                AccentColor = new Color(0.35f, 0.75f, 1f, 1f),
                OnPrimaryAction = () => ToggleStopwatch(),
                OnSecondaryAction = () => ResetStopwatch()
            });
            ShowToast("⏱ 秒表已在小米超级岛置顶启动");
        }
        else
        {
            RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.Instance?.RemoveSession(RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandAppType.Timer);
            ShowToast("⏱ 秒表已暂停");
        }
    }

    public static void ResetStopwatch()
    {
        s_stopwatchElapsed = 0f;
        RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.Instance?.UpdateSession(RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandAppType.Timer, s =>
        {
            s.Metric = "00:00";
        });
        ShowToast("⏱ 秒表已归零");
    }

    public static void UpdateStopwatchTick(float dt)
    {
        if (!s_isStopwatchRunning) return;
        s_stopwatchElapsed += dt;
        RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.Instance?.UpdateSession(RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandAppType.Timer, s =>
        {
            s.Metric = FormatTime(s_stopwatchElapsed);
        });
    }

    private static string FormatTime(float sec)
    {
        int s = (int)sec;
        return string.Format("{0:D2}:{1:D2}", s / 60, s % 60);
    }

    // ============ App: 时钟 ============
    public static void BuildClockApp(VisualElement win)
    {
        var body = BuildAppBody(win, "时钟", "阿里郎高精度授时中心 · 运行授时服务");

        // 大表盘卡片
        var clockCard = BuildAppCard();
        clockCard.style.alignItems = Align.Center;
        clockCard.style.paddingTop = 24; clockCard.style.paddingBottom = 24;

        var time = new Label(System.DateTime.Now.ToString("HH:mm:ss"));
        time.style.fontSize = 54; time.style.color = InkPrimary;
        time.style.unityFontStyleAndWeight = FontStyle.Bold;
        time.style.unityFontDefinition = Fd();
        clockCard.Add(time);

        var date = new Label($"运营第 {GameData.Day} 天 · 主体历 105 年 1 月 1 日");
        date.style.fontSize = 14; date.style.color = AccentSystem;
        date.style.unityFontDefinition = Fd(); date.style.marginTop = 8;
        clockCard.Add(date);
        body.Add(clockCard);

        // 分组卡片：功能Tab（闹钟/世界时钟/秒表/倒计时）
        var funcCard = BuildAppCard("时钟功能");
        funcCard.Add(BuildLightRow("⏰ 晨间出车早擎闹钟", "已设定 05:30（工作日）", () => ShowToast("闹钟已同步到先民排班系统")));
        funcCard.Add(BuildLightRow("🌐 平壤内网主时钟同步", "偏移量 +0.002s · 阿里郎1号卫星校准", () => ShowToast("授时服务器连接稳定")));
        funcCard.Add(BuildLightRow("⏱ 运行耗时秒表", "用于测定雾峰站至茶山站实际单程区间时分", () => ToggleStopwatch()));
        body.Add(funcCard);
    }

    // ============ App: 日历 ============
    public static void BuildCalendarApp(VisualElement win)
    {
        var body = BuildAppBody(win, "日历", "主体历 105 年 1 月（公历 2076 年 1 月）");

        var calCard = BuildAppCard("月度运行视图");
        calCard.style.alignSelf = Align.Center;
        calCard.style.width = 680;

        string[] days = { "一", "二", "三", "四", "五", "六", "日" };
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.marginBottom = 10;
        calCard.Add(header);
        foreach (var d in days)
        {
            var dl = new Label(d);
            dl.style.width = 85; dl.style.fontSize = 13; dl.style.color = InkSecondary;
            dl.style.unityFontDefinition = Fd(); dl.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(dl);
        }

        // 5周网格，完整呈现 31 天
        for (int w = 0; w < 5; w++)
        {
            var week = new VisualElement();
            week.style.flexDirection = FlexDirection.Row;
            week.style.justifyContent = Justify.SpaceBetween;
            week.style.marginTop = 6;
            calCard.Add(week);
            for (int d = 0; d < 7; d++)
            {
                int day = w * 7 + d + 1;
                var dl = new Label(day <= 31 ? day.ToString() : "");
                dl.style.width = 85; dl.style.height = 38; dl.style.fontSize = 14;
                dl.style.unityFontDefinition = Fd(); dl.style.unityTextAlign = TextAnchor.MiddleCenter;

                if (day <= 31)
                {
                    bool isCurrentDay = (day == GameData.Day || (GameData.Day > 31 && day == 1));
                    dl.style.color = isCurrentDay ? Color.white : InkPrimary;
                    if (isCurrentDay)
                    {
                        dl.style.backgroundColor = AccentSystem;
                        dl.style.borderTopLeftRadius = 8; dl.style.borderTopRightRadius = 8;
                        dl.style.borderBottomLeftRadius = 8; dl.style.borderBottomRightRadius = 8;
                    }
                }
                else
                {
                    dl.style.color = Color.clear;
                }
                week.Add(dl);
            }
        }
        body.Add(calCard);

        var scheduleCard = BuildAppCard("今日排程备忘");
        scheduleCard.Add(BuildLightRow("08:00 晨间第一班客运列车出库", "线路巡道工已完成万能道尺轨距巡检", null, false));
        scheduleCard.Add(BuildLightRow("12:30 乌金岭矿区沙币结算单核验", "嘉颖徐铁路基金派员进驻", null, false));
        body.Add(scheduleCard);
    }

    // ============ App: 天气 ============
    public static void BuildWeatherApp(VisualElement win)
    {
        var body = BuildAppBody(win, "气象监视", "雾峰村气象站 · 实时气象水文与轮轨物理环境");

        // 主气象卡片（Hero Weather Card，大号温度与气象徽章）
        var heroCard = BuildAppCard();
        heroCard.style.paddingTop = 20; heroCard.style.paddingBottom = 20;
        heroCard.style.paddingLeft = 24; heroCard.style.paddingRight = 24;
        heroCard.style.flexDirection = FlexDirection.Row;
        heroCard.style.alignItems = Align.Center;
        heroCard.style.justifyContent = Justify.SpaceBetween;

        var tempCol = new VisualElement();
        var tempRow = new VisualElement();
        tempRow.style.flexDirection = FlexDirection.Row;
        tempRow.style.alignItems = Align.Center;
        tempCol.Add(tempRow);

        var temp = new Label("18°C");
        temp.style.fontSize = 52; temp.style.color = InkPrimary;
        temp.style.unityFontStyleAndWeight = FontStyle.Bold;
        temp.style.unityFontDefinition = Fd();
        tempRow.Add(temp);

        var weatherBadge = BuildTagPill("⛅ 多云", new Color(0.23f, 0.51f, 0.96f, 0.15f), new Color(0.23f, 0.51f, 0.96f, 1f), 12f);
        weatherBadge.style.marginLeft = 14;
        tempRow.Add(weatherBadge);

        var cond = new Label("东北风 2级 (3.2 m/s) · 大气压 101.4 kPa · 体感舒适");
        cond.style.fontSize = 13; cond.style.color = InkSecondary;
        cond.style.unityFontDefinition = Fd(); cond.style.marginTop = 6;
        tempCol.Add(cond);
        heroCard.Add(tempCol);

        var tempRangeBox = new VisualElement();
        tempRangeBox.style.alignItems = Align.FlexEnd;
        var rangePill = BuildTagPill("↑ 21°C  ↓ 9°C", SurfaceHover, InkPrimary, 12f);
        tempRangeBox.Add(rangePill);
        var airQualityLbl = new Label("空气质量：优 (AQI 22)");
        airQualityLbl.style.fontSize = 12; airQualityLbl.style.color = new Color(0.15f, 0.70f, 0.42f, 1f);
        airQualityLbl.style.unityFontDefinition = Fd(); airQualityLbl.style.marginTop = 6;
        tempRangeBox.Add(airQualityLbl);
        heroCard.Add(tempRangeBox);
        body.Add(heroCard);

        // 铁路水文物理量规卡（2x2 网格）
        var gridHeader = new Label("铁路运营与沙能微观物理评估");
        gridHeader.style.fontSize = 14; gridHeader.style.color = SectionHeaderColor;
        gridHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        gridHeader.style.unityFontDefinition = Fd();
        gridHeader.style.marginBottom = 10;
        body.Add(gridHeader);

        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        grid.style.justifyContent = Justify.SpaceBetween;
        grid.style.marginBottom = 12;
        body.Add(grid);

        void AddTelemetryCard(string icon, Color badgeColor, string title, string metric, float barRatio, Color barColor, string desc)
        {
            var card = BuildAppCard();
            card.style.width = new Length(49, LengthUnit.Percent);
            card.style.marginBottom = 10;

            var hRow = new VisualElement();
            hRow.style.flexDirection = FlexDirection.Row;
            hRow.style.alignItems = Align.Center;
            hRow.style.justifyContent = Justify.SpaceBetween;
            card.Add(hRow);

            var left = new VisualElement();
            left.style.flexDirection = FlexDirection.Row;
            left.style.alignItems = Align.Center;
            left.Add(BuildSquircleBadge(icon, badgeColor, 28f));

            var tLbl = new Label(title);
            tLbl.style.fontSize = 14; tLbl.style.color = InkPrimary;
            tLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tLbl.style.unityFontDefinition = Fd();
            tLbl.style.marginLeft = 8;
            left.Add(tLbl);
            hRow.Add(left);

            var mPill = BuildTagPill(metric, new Color(barColor.r, barColor.g, barColor.b, 0.15f), barColor, 11f);
            hRow.Add(mPill);

            var bar = BuildProgressBar(barRatio, barColor, 6f);
            bar.style.marginTop = 10; bar.style.marginBottom = 6;
            card.Add(bar);

            var dLbl = new Label(desc);
            dLbl.style.fontSize = 12; dLbl.style.color = InkSecondary;
            dLbl.style.unityFontDefinition = Fd();
            card.Add(dLbl);

            grid.Add(card);
        }

        AddTelemetryCard("🛤", new Color(0.12f, 0.72f, 0.50f, 1f), "轨面附着系数", "μ = 0.38 · 峰值", 0.85f, new Color(0.12f, 0.72f, 0.50f, 1f), "钢轨干燥洁净，牵引与动力制动粘着性能峰值");
        AddTelemetryCard("⚡", new Color(0.23f, 0.51f, 0.96f, 1f), "沙能流转阻力", "0.12 · 阻力平稳", 0.24f, new Color(0.23f, 0.51f, 0.96f, 1f), "沙粒流速平缓，竞争对手沙能载具无异常运力加成");
        AddTelemetryCard("💧", new Color(0.05f, 0.68f, 0.88f, 1f), "大气相对湿度", "62% · 正常", 0.62f, new Color(0.05f, 0.68f, 0.88f, 1f), "道床与枕木含水率正常，电气回路绝缘稳定");
        AddTelemetryCard("👁", new Color(0.96f, 0.62f, 0.10f, 1f), "大气能见度", "8.5 km · 良好", 0.85f, new Color(0.96f, 0.62f, 0.10f, 1f), "明日午后有薄雾，建议提早开启0721机车大灯");

        // 3日气象胶囊卡
        var forecastCard = BuildAppCard("未来三日铁道气象预报");
        var forecastRow = new VisualElement();
        forecastRow.style.flexDirection = FlexDirection.Row;
        forecastRow.style.justifyContent = Justify.SpaceBetween;
        forecastCard.Add(forecastRow);

        void AddDayForecast(string day, string icon, string weather, string tempStr, string alert)
        {
            var dayBox = new VisualElement();
            dayBox.style.width = new Length(32, LengthUnit.Percent);
            dayBox.style.backgroundColor = SurfaceHover;
            dayBox.style.borderTopLeftRadius = RadiusCard; dayBox.style.borderTopRightRadius = RadiusCard;
            dayBox.style.borderBottomLeftRadius = RadiusCard; dayBox.style.borderBottomRightRadius = RadiusCard;
            dayBox.style.paddingLeft = 12; dayBox.style.paddingRight = 12;
            dayBox.style.paddingTop = 10; dayBox.style.paddingBottom = 10;
            dayBox.style.alignItems = Align.Center;

            var dLbl = new Label(day);
            dLbl.style.fontSize = 12; dLbl.style.color = InkTertiary; dLbl.style.unityFontDefinition = Fd();
            dayBox.Add(dLbl);

            var ic = new Label(icon);
            ic.style.fontSize = 24; ic.style.marginTop = 4; ic.style.unityFontDefinition = Fd();
            dayBox.Add(ic);

            var wLbl = new Label(weather);
            wLbl.style.fontSize = 13; wLbl.style.color = InkPrimary; wLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            wLbl.style.unityFontDefinition = Fd(); wLbl.style.marginTop = 2;
            dayBox.Add(wLbl);

            var tLbl = new Label(tempStr);
            tLbl.style.fontSize = 12; tLbl.style.color = InkSecondary; tLbl.style.unityFontDefinition = Fd();
            tLbl.style.marginTop = 2;
            dayBox.Add(tLbl);

            var aPill = BuildTagPill(alert, SurfaceCard, InkTertiary, 10f);
            aPill.style.marginTop = 6;
            dayBox.Add(aPill);

            forecastRow.Add(dayBox);
        }

        AddDayForecast("今日（周一）", "⛅", "多云", "18° / 9°", "线路畅通");
        AddDayForecast("明日（周二）", "☀️", "晴间多云", "21° / 11°", "适合巡检");
        AddDayForecast("后日（周三）", "🌧", "小雨转阴", "15° / 8°", "轨面湿滑注意制动");
        body.Add(forecastCard);
    }

    // ============ 平板四则计算器引擎 ============
    public sealed class TabletCalculatorEngine
    {
        private string _currentEntry = "0";
        private bool _isEnteringNumber = false;
        private bool _hasEvaluated = false;
        private bool _hasError = false;
        private string _errorMessage = "";

        private readonly List<decimal> _numbers = new List<decimal>();
        private readonly List<char> _operators = new List<char>();

        private char _lastRepeatedOp = '\0';
        private decimal _lastRepeatedOperand = 0m;

        public string CurrentDisplay => _hasError ? _errorMessage : _currentEntry;
        public string ExpressionText { get; private set; } = "";
        public bool HasError => _hasError;
        public string ErrorMessage => _errorMessage;

        public event Action OnStateChanged;

        public void InputDigit(int digit)
        {
            if (digit < 0 || digit > 9) return;
            InputDigit((char)('0' + digit));
        }

        public void InputDigit(char digit)
        {
            if (digit < '0' || digit > '9') return;

            if (_hasError || _hasEvaluated)
            {
                ClearInternal();
                _currentEntry = digit.ToString();
                _isEnteringNumber = true;
                _hasEvaluated = false;
            }
            else if (!_isEnteringNumber)
            {
                _currentEntry = digit.ToString();
                _isEnteringNumber = true;
            }
            else
            {
                if (_currentEntry == "0")
                {
                    _currentEntry = digit.ToString();
                }
                else if (_currentEntry == "-0")
                {
                    _currentEntry = "-" + digit.ToString();
                }
                else
                {
                    string pureDigits = _currentEntry.Replace("-", "").Replace(".", "");
                    if (pureDigits.Length < 29)
                    {
                        _currentEntry += digit;
                    }
                }
            }

            OnStateChanged?.Invoke();
        }

        public void InputDot()
        {
            if (_hasError || _hasEvaluated)
            {
                ClearInternal();
                _currentEntry = "0.";
                _isEnteringNumber = true;
                _hasEvaluated = false;
            }
            else if (!_isEnteringNumber)
            {
                _currentEntry = "0.";
                _isEnteringNumber = true;
            }
            else
            {
                if (!_currentEntry.Contains("."))
                {
                    _currentEntry += ".";
                }
            }

            OnStateChanged?.Invoke();
        }

        public void InputBackspace()
        {
            if (_hasError)
            {
                Clear();
                return;
            }

            if (_hasEvaluated)
            {
                _currentEntry = "0";
                _isEnteringNumber = false;
                OnStateChanged?.Invoke();
                return;
            }

            if (_isEnteringNumber)
            {
                if (_currentEntry.Length > 0)
                {
                    _currentEntry = _currentEntry.Substring(0, _currentEntry.Length - 1);
                }

                if (string.IsNullOrEmpty(_currentEntry) || _currentEntry == "-" || _currentEntry == "-0")
                {
                    _currentEntry = "0";
                    _isEnteringNumber = false;
                }
            }
            else
            {
                _currentEntry = "0";
            }

            OnStateChanged?.Invoke();
        }

        public void InputNegate()
        {
            if (_hasError) return;

            if (_currentEntry == "0" || _currentEntry == "0.")
            {
                return;
            }

            if (_currentEntry.StartsWith("-"))
            {
                _currentEntry = _currentEntry.Substring(1);
            }
            else
            {
                _currentEntry = "-" + _currentEntry;
            }

            if (_hasEvaluated)
            {
                _hasEvaluated = false;
                _isEnteringNumber = true;
                _numbers.Clear();
                _operators.Clear();
                ExpressionText = "";
            }

            OnStateChanged?.Invoke();
        }

        public void InputOperator(char op)
        {
            op = NormalizeOp(op);
            if (op != '+' && op != '-' && op != '×' && op != '÷') return;

            if (_hasError) return;

            decimal val;
            if (!decimal.TryParse(_currentEntry, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
            {
                SetError("输入错误");
                return;
            }

            if (_hasEvaluated)
            {
                _hasEvaluated = false;
                _numbers.Clear();
                _operators.Clear();
                _numbers.Add(val);
                _operators.Add(op);
                _isEnteringNumber = false;
                UpdateExpressionText();
                OnStateChanged?.Invoke();
                return;
            }

            if (_isEnteringNumber)
            {
                _numbers.Add(val);
                _operators.Add(op);
                _isEnteringNumber = false;
                UpdateExpressionText();
            }
            else
            {
                if (_operators.Count > 0)
                {
                    _operators[_operators.Count - 1] = op;
                    UpdateExpressionText();
                }
                else
                {
                    _numbers.Add(val);
                    _operators.Add(op);
                    UpdateExpressionText();
                }
            }

            OnStateChanged?.Invoke();
        }

        public void InputEquals()
        {
            if (_hasError) return;

            if (_hasEvaluated)
            {
                if (_lastRepeatedOp != '\0')
                {
                    decimal current;
                    if (!decimal.TryParse(_currentEntry, NumberStyles.Float, CultureInfo.InvariantCulture, out current))
                    {
                        SetError("输入错误");
                        return;
                    }

                    try
                    {
                        decimal res = ApplyOp(current, _lastRepeatedOp, _lastRepeatedOperand);
                        ExpressionText = $"{FormatDecimal(current)} {_lastRepeatedOp} {FormatDecimal(_lastRepeatedOperand)} =";
                        _currentEntry = FormatDecimal(res);
                        OnStateChanged?.Invoke();
                    }
                    catch (DivideByZeroException)
                    {
                        SetError("除数不能为0");
                    }
                    catch (OverflowException)
                    {
                        SetError("数值溢出");
                    }
                    catch (Exception)
                    {
                        SetError("运算错误");
                    }
                }
                return;
            }

            decimal finalVal;
            if (!decimal.TryParse(_currentEntry, NumberStyles.Float, CultureInfo.InvariantCulture, out finalVal))
            {
                SetError("输入错误");
                return;
            }

            if (_operators.Count == 0)
            {
                _currentEntry = FormatDecimal(finalVal);
                ExpressionText = $"{_currentEntry} =";
                _hasEvaluated = true;
                _isEnteringNumber = false;
                OnStateChanged?.Invoke();
                return;
            }

            _numbers.Add(finalVal);
            _isEnteringNumber = false;

            _lastRepeatedOp = _operators[_operators.Count - 1];
            _lastRepeatedOperand = _numbers[_numbers.Count - 1];

            ExpressionText = BuildExpressionString(_numbers, _operators) + " =";

            var numsCopy = new List<decimal>(_numbers);
            var opsCopy = new List<char>(_operators);

            if (EvaluateInfix(numsCopy, opsCopy, out decimal result, out string error))
            {
                _currentEntry = FormatDecimal(result);
                _hasEvaluated = true;
                _numbers.Clear();
                _operators.Clear();
            }
            else
            {
                SetError(error);
            }

            OnStateChanged?.Invoke();
        }

        public void Clear()
        {
            ClearInternal();
            OnStateChanged?.Invoke();
        }

        private void ClearInternal()
        {
            _currentEntry = "0";
            _isEnteringNumber = false;
            _hasEvaluated = false;
            _hasError = false;
            _errorMessage = "";
            _numbers.Clear();
            _operators.Clear();
            _lastRepeatedOp = '\0';
            _lastRepeatedOperand = 0m;
            ExpressionText = "";
        }

        private void SetError(string message)
        {
            _hasError = true;
            _errorMessage = message;
            _currentEntry = "0";
            _isEnteringNumber = false;
            _hasEvaluated = false;
            _numbers.Clear();
            _operators.Clear();
            _lastRepeatedOp = '\0';
            _lastRepeatedOperand = 0m;
            OnStateChanged?.Invoke();
        }

        private void UpdateExpressionText()
        {
            ExpressionText = BuildExpressionString(_numbers, _operators);
        }

        private static string BuildExpressionString(List<decimal> nums, List<char> ops)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < nums.Count; i++)
            {
                if (i > 0) sb.Append(" ");
                sb.Append(FormatDecimal(nums[i]));
                if (i < ops.Count)
                {
                    sb.Append(" ").Append(ops[i]);
                }
            }
            return sb.ToString();
        }

        private static bool EvaluateInfix(List<decimal> nums, List<char> ops, out decimal result, out string error)
        {
            result = 0m;
            error = null;
            try
            {
                // 优先级第一轮：乘除运算（左结合）
                int i = 0;
                while (i < ops.Count)
                {
                    char op = ops[i];
                    if (op == '×' || op == '÷')
                    {
                        decimal a = nums[i];
                        decimal b = nums[i + 1];
                        decimal r = ApplyOp(a, op, b);
                        nums[i] = r;
                        nums.RemoveAt(i + 1);
                        ops.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                // 优先级第二轮：加减运算（左结合）
                i = 0;
                while (i < ops.Count)
                {
                    char op = ops[i];
                    decimal a = nums[i];
                    decimal b = nums[i + 1];
                    decimal r = ApplyOp(a, op, b);
                    nums[i] = r;
                    nums.RemoveAt(i + 1);
                    ops.RemoveAt(i);
                }

                result = nums[0];
                return true;
            }
            catch (DivideByZeroException)
            {
                error = "除数不能为0";
                return false;
            }
            catch (OverflowException)
            {
                error = "数值溢出";
                return false;
            }
            catch (Exception)
            {
                error = "运算错误";
                return false;
            }
        }

        private static decimal ApplyOp(decimal a, char op, decimal b)
        {
            switch (op)
            {
                case '+': return checked(a + b);
                case '-': return checked(a - b);
                case '×':
                case '*': return checked(a * b);
                case '÷':
                case '/':
                    if (b == 0m) throw new DivideByZeroException();
                    return checked(a / b);
                default:
                    throw new InvalidOperationException("未知运算符: " + op);
            }
        }

        private static char NormalizeOp(char op)
        {
            if (op == '*') return '×';
            if (op == '/') return '÷';
            return op;
        }

        public static string FormatDecimal(decimal value)
        {
            string s = value.ToString("0.############################", CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(s) || s == "-0") s = "0";
            return s;
        }
    }

    // ============ App: 计算器 ============
    public static void BuildCalculatorApp(VisualElement win, TabletCalculatorEngine engine = null)
    {
        var body = BuildAppBody(win, "计算器", "基础四则运算");

        var card = BuildAppCard();
        card.name = "calc-card";
        card.style.width = 420;
        card.style.alignSelf = Align.Center;

        engine ??= new TabletCalculatorEngine();
        card.userData = engine;

        // 显示屏区域
        var displayBox = new VisualElement();
        displayBox.name = "calc-display-container";
        displayBox.style.backgroundColor = SurfaceApp;
        displayBox.style.borderTopLeftRadius = 8; displayBox.style.borderTopRightRadius = 8;
        displayBox.style.borderBottomLeftRadius = 8; displayBox.style.borderBottomRightRadius = 8;
        displayBox.style.paddingLeft = 12; displayBox.style.paddingRight = 12;
        displayBox.style.paddingTop = 8; displayBox.style.paddingBottom = 8;
        displayBox.style.marginBottom = 12;

        var exprLabel = new Label("");
        exprLabel.name = "calc-display-expression";
        exprLabel.style.fontSize = 13;
        exprLabel.style.color = InkSecondary;
        exprLabel.style.unityFontDefinition = Fd();
        exprLabel.style.height = 20;
        exprLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        displayBox.Add(exprLabel);

        var mainLabel = new Label("0");
        mainLabel.name = "calc-display-main";
        mainLabel.style.fontSize = 32;
        mainLabel.style.color = InkPrimary;
        mainLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        mainLabel.style.unityFontDefinition = Fd();
        mainLabel.style.height = 44;
        mainLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        displayBox.Add(mainLabel);

        card.Add(displayBox);

        void UpdateUI()
        {
            mainLabel.text = engine.CurrentDisplay;
            exprLabel.text = engine.ExpressionText;
            if (engine.HasError)
            {
                mainLabel.style.color = CrimsonSeal;
                mainLabel.style.fontSize = 20;
            }
            else
            {
                mainLabel.style.color = InkPrimary;
                int len = mainLabel.text != null ? mainLabel.text.Length : 0;
                mainLabel.style.fontSize = len > 16 ? 20 : (len > 10 ? 26 : 32);
            }
        }

        // A retained task engine must not retain a discarded view through its event.
        card.RegisterCallback<AttachToPanelEvent>(_ => { engine.OnStateChanged -= UpdateUI; engine.OnStateChanged += UpdateUI; UpdateUI(); });
        card.RegisterCallback<DetachFromPanelEvent>(_ => engine.OnStateChanged -= UpdateUI);
        UpdateUI();

        var buttonRows = new[]
        {
            new (string text, string name, Action action, float width, bool isAccent, bool isOperator)[]
            {
                ("C", "calc-btn-c", engine.Clear, 88f, false, false),
                ("⌫", "calc-btn-backspace", engine.InputBackspace, 88f, false, false),
                ("±", "calc-btn-negate", engine.InputNegate, 88f, false, false),
                ("÷", "calc-btn-div", () => engine.InputOperator('÷'), 88f, false, true),
            },
            new (string text, string name, Action action, float width, bool isAccent, bool isOperator)[]
            {
                ("7", "calc-btn-7", () => engine.InputDigit(7), 88f, false, false),
                ("8", "calc-btn-8", () => engine.InputDigit(8), 88f, false, false),
                ("9", "calc-btn-9", () => engine.InputDigit(9), 88f, false, false),
                ("×", "calc-btn-mul", () => engine.InputOperator('×'), 88f, false, true),
            },
            new (string text, string name, Action action, float width, bool isAccent, bool isOperator)[]
            {
                ("4", "calc-btn-4", () => engine.InputDigit(4), 88f, false, false),
                ("5", "calc-btn-5", () => engine.InputDigit(5), 88f, false, false),
                ("6", "calc-btn-6", () => engine.InputDigit(6), 88f, false, false),
                ("-", "calc-btn-sub", () => engine.InputOperator('-'), 88f, false, true),
            },
            new (string text, string name, Action action, float width, bool isAccent, bool isOperator)[]
            {
                ("1", "calc-btn-1", () => engine.InputDigit(1), 88f, false, false),
                ("2", "calc-btn-2", () => engine.InputDigit(2), 88f, false, false),
                ("3", "calc-btn-3", () => engine.InputDigit(3), 88f, false, false),
                ("+", "calc-btn-add", () => engine.InputOperator('+'), 88f, false, true),
            },
            new (string text, string name, Action action, float width, bool isAccent, bool isOperator)[]
            {
                ("0", "calc-btn-0", () => engine.InputDigit(0), 184f, false, false),
                (".", "calc-btn-dot", engine.InputDot, 88f, false, false),
                ("=", "calc-btn-equal", engine.InputEquals, 88f, true, false),
            }
        };

        foreach (var row in buttonRows)
        {
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.marginTop = 6;
            btnRow.style.justifyContent = Justify.SpaceBetween;
            card.Add(btnRow);

            foreach (var b in row)
            {
                var btn = new Button(b.action) { text = b.text };
                btn.name = b.name;
                btn.style.width = b.width;
                btn.style.height = 46;
                btn.style.minWidth = 44;
                btn.style.minHeight = 44;
                btn.style.fontSize = 18;
                btn.style.unityTextAlign = TextAnchor.MiddleCenter;
                btn.style.unityFontDefinition = Fd();
                btn.style.backgroundColor = b.isAccent ? AccentSystem : (b.isOperator ? SurfaceCardSoft : SurfaceHover);
                btn.style.color = b.isAccent ? Color.white : InkPrimary;
                btn.style.borderTopLeftRadius = RadiusCard; btn.style.borderTopRightRadius = RadiusCard;
                btn.style.borderBottomLeftRadius = RadiusCard; btn.style.borderBottomRightRadius = RadiusCard;
                btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
                btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
                btnRow.Add(btn);
            }
        }

        body.Add(card);
    }

        // ============ App: 米家（0721遥测）============
    public static void BuildMijiaApp(VisualElement win)
    {
        var body = BuildAppBody(win, "机车遥测", "米家物联网 · 0721型「沙子飞猪号」机车智能网关");

        // 3大核心动力指标胶囊带
        var bannerRow = new VisualElement();
        bannerRow.style.flexDirection = FlexDirection.Row;
        bannerRow.style.justifyContent = Justify.SpaceBetween;
        bannerRow.style.marginBottom = 14;
        body.Add(bannerRow);

        void AddTelemetryMetricCard(string icon, Color badgeColor, string title, string val, float progress01, Color barColor, string sub)
        {
            var card = BuildAppCard();
            card.style.width = new Length(32, LengthUnit.Percent);
            card.style.marginBottom = 0;

            var top = new VisualElement();
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems = Align.Center;
            top.Add(BuildSquircleBadge(icon, badgeColor, 28f));

            var titleCol = new VisualElement();
            titleCol.style.marginLeft = 8;
            var tLbl = new Label(title);
            tLbl.style.fontSize = 12; tLbl.style.color = InkSecondary; tLbl.style.unityFontDefinition = Fd();
            titleCol.Add(tLbl);
            var vLbl = new Label(val);
            vLbl.style.fontSize = 16; vLbl.style.color = InkPrimary; vLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            vLbl.style.unityFontDefinition = Fd();
            titleCol.Add(vLbl);
            top.Add(titleCol);
            card.Add(top);

            var bar = BuildProgressBar(progress01, barColor, 6f);
            bar.style.marginTop = 10; bar.style.marginBottom = 6;
            card.Add(bar);

            var subLbl = new Label(sub);
            subLbl.style.fontSize = 11; subLbl.style.color = InkTertiary; subLbl.style.unityFontDefinition = Fd();
            card.Add(subLbl);

            bannerRow.Add(card);
        }

        AddTelemetryMetricCard("⚡", new Color(0.96f, 0.62f, 0.10f, 1f), "沙能储箱余量", "1,200 / 1,500 kg", 0.80f, new Color(0.96f, 0.62f, 0.10f, 1f), "80% 储量充足 · 先民高纯度动力沙");
        AddTelemetryMetricCard("🔋", new Color(0.22f, 0.78f, 0.35f, 1f), "动力电池组蓄电量", "78% 正常放电", 0.78f, new Color(0.22f, 0.78f, 0.35f, 1f), "母线电压 650V · 辅机逆变器工作正常");
        AddTelemetryMetricCard("🏁", new Color(0.23f, 0.51f, 0.96f, 1f), "预估安全续航", "340 km", 0.68f, new Color(0.23f, 0.51f, 0.96f, 1f), "可支持雾峰至矿区重载往返 7.4 次");

        // 超级岛置顶挂载操作栏
        var actionRow = new VisualElement();
        actionRow.style.flexDirection = FlexDirection.Row;
        actionRow.style.justifyContent = Justify.SpaceBetween;
        actionRow.style.alignItems = Align.Center;
        actionRow.style.backgroundColor = SurfaceCard;
        actionRow.style.borderTopLeftRadius = RadiusCard; actionRow.style.borderTopRightRadius = RadiusCard;
        actionRow.style.borderBottomLeftRadius = RadiusCard; actionRow.style.borderBottomRightRadius = RadiusCard;
        actionRow.style.borderTopWidth = 1; actionRow.style.borderBottomWidth = 1;
        actionRow.style.borderLeftWidth = 1; actionRow.style.borderRightWidth = 1;
        actionRow.style.borderTopColor = BorderCard; actionRow.style.borderBottomColor = BorderCard;
        actionRow.style.borderLeftColor = BorderCard; actionRow.style.borderRightColor = BorderCard;
        actionRow.style.paddingLeft = 16; actionRow.style.paddingRight = 16;
        actionRow.style.paddingTop = 10; actionRow.style.paddingBottom = 10;
        actionRow.style.marginBottom = 14;

        var tipCol = new VisualElement();
        var tipTitle = new Label("小米超级岛 · 动力状态实时投屏");
        tipTitle.style.fontSize = 14; tipTitle.style.color = InkPrimary; tipTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        tipTitle.style.unityFontDefinition = Fd();
        tipCol.Add(tipTitle);
        var tipSub = new Label("将当前 0721 号机车的沙量、车速与动力电池状态置顶到平板顶部状态栏超级岛微胶囊。");
        tipSub.style.fontSize = 12; tipSub.style.color = InkSecondary; tipSub.style.unityFontDefinition = Fd();
        tipCol.Add(tipSub);
        actionRow.Add(tipCol);

        var pinBtn = new Button(() =>
        {
            RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.Instance?.RegisterSession(new RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandSession
            {
                AppType = RailwayRenaissance.UI.Tablet.SystemShell.TabletSuperIsland.IslandAppType.TrainCab,
                Priority = 85,
                Title = "0721 机车动力遥测",
                Subtitle = "沙能 80% · 电池 78% · 正常放电",
                Metric = "340 km 续航",
                Progress = 0.8f,
                AccentColor = new Color(0.96f, 0.62f, 0.10f, 1f)
            });
            ShowToast("🚀 机车动力遥测已置顶挂载到小米超级岛！");
        }) { text = "📌 挂载至超级岛" };
        pinBtn.style.height = 36;
        pinBtn.style.paddingLeft = 16; pinBtn.style.paddingRight = 16;
        pinBtn.style.fontSize = 13; pinBtn.style.unityFontDefinition = Fd();
        pinBtn.style.backgroundColor = AccentSystem;
        pinBtn.style.color = Color.white;
        pinBtn.style.borderTopLeftRadius = RadiusPill; pinBtn.style.borderTopRightRadius = RadiusPill;
        pinBtn.style.borderBottomLeftRadius = RadiusPill; pinBtn.style.borderBottomRightRadius = RadiusPill;
        pinBtn.style.borderTopWidth = 0; pinBtn.style.borderBottomWidth = 0;
        pinBtn.style.borderLeftWidth = 0; pinBtn.style.borderRightWidth = 0;
        actionRow.Add(pinBtn);
        body.Add(actionRow);

        // 4大机载智能节点（2x2 网格卡片）
        var nodeHeader = new Label("机载 IoT 智能节点在线状态");
        nodeHeader.style.fontSize = 14; nodeHeader.style.color = SectionHeaderColor;
        nodeHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        nodeHeader.style.unityFontDefinition = Fd();
        nodeHeader.style.marginBottom = 10;
        body.Add(nodeHeader);

        var nodeGrid = new VisualElement();
        nodeGrid.style.flexDirection = FlexDirection.Row;
        nodeGrid.style.flexWrap = Wrap.Wrap;
        nodeGrid.style.justifyContent = Justify.SpaceBetween;
        body.Add(nodeGrid);

        void AddNodeCard(string icon, Color badgeColor, string title, string statusText, Color statusColor, string desc, System.Action onClick)
        {
            var card = BuildAppCard();
            card.style.width = new Length(49, LengthUnit.Percent);
            card.style.marginBottom = 10;
            card.pickingMode = PickingMode.Position;

            var r1 = new VisualElement();
            r1.style.flexDirection = FlexDirection.Row;
            r1.style.alignItems = Align.Center;
            r1.style.justifyContent = Justify.SpaceBetween;
            card.Add(r1);

            var lGroup = new VisualElement();
            lGroup.style.flexDirection = FlexDirection.Row;
            lGroup.style.alignItems = Align.Center;
            lGroup.Add(BuildSquircleBadge(icon, badgeColor, 30f));

            var tLbl = new Label(title);
            tLbl.style.fontSize = 14; tLbl.style.color = InkPrimary; tLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tLbl.style.unityFontDefinition = Fd(); tLbl.style.marginLeft = 8;
            lGroup.Add(tLbl);
            r1.Add(lGroup);

            var sPill = BuildTagPill(statusText, new Color(statusColor.r, statusColor.g, statusColor.b, 0.15f), statusColor, 10f);
            r1.Add(sPill);

            var dLbl = new Label(desc);
            dLbl.style.fontSize = 12; dLbl.style.color = InkSecondary; dLbl.style.unityFontDefinition = Fd();
            dLbl.style.marginTop = 8;
            card.Add(dLbl);

            if (onClick != null)
            {
                card.RegisterCallback<ClickEvent>(_ => onClick());
            }

            nodeGrid.Add(card);
        }

        AddNodeCard("📐", new Color(0.12f, 0.72f, 0.50f, 1f), "万能道尺无线遥测模块", "已连接", new Color(0.12f, 0.72f, 0.50f, 1f), "采样精度 ±0.5mm · 采样频率 50Hz · 电池 92%", () => ShowToast("道尺蓝牙遥测信号满格"));
        AddNodeCard("📹", new Color(0.85f, 0.45f, 0.15f, 1f), "机车行车记录仪", "循环录像中", new Color(0.85f, 0.45f, 0.15f, 1f), "1080P 60FPS 编码 · 闪存剩余 18.4GB · 紧急碰撞自动锁定", () => ShowToast("行车记录视频保护已激活"));
        AddNodeCard("⚙️", new Color(0.55f, 0.32f, 0.95f, 1f), "轴承振动监视传感器", "全轴正常", new Color(0.55f, 0.32f, 0.95f, 1f), "全车 6 轴振动偏度 0.02mm · 润滑油温 52°C · 无偏磨", () => ShowToast("张工最新巡检记录已同步"));
        AddNodeCard("💡", new Color(0.96f, 0.72f, 0.15f, 1f), "机车照明与逆变电源", "常亮开启", new Color(0.96f, 0.72f, 0.15f, 1f), "主大灯 2.4kW · 照度 1200Lux · 辅机逆变器效率 94%", () => ShowToast("大灯系统工作正常"));
    }

    // ============ App: 安全中心 ============
    public static void BuildSecurityApp(VisualElement win)
    {
        var body = BuildAppBody(win, "安全中心", "阿里郎终端安全与内网防护系统");

        var scoreCard = BuildAppCard();
        scoreCard.style.alignItems = Align.Center;
        scoreCard.style.paddingTop = 16; scoreCard.style.paddingBottom = 16;

        var shield = new Label("🛡");
        shield.style.fontSize = 44; shield.style.unityFontDefinition = Fd();
        scoreCard.Add(shield);

        var score = new Label("100 分 · 系统处于安全状态");
        score.style.fontSize = 18; score.style.color = new Color(0.2f, 0.65f, 0.3f, 1f);
        score.style.unityFontStyleAndWeight = FontStyle.Bold;
        score.style.unityFontDefinition = Fd(); score.style.marginTop = 6;
        scoreCard.Add(score);

        var scanBtn = new Button(() => ShowToast("系统已全面扫描：未发现安全漏洞")) { text = "一键优化与垃圾清理" };
        scanBtn.style.marginTop = 12; scanBtn.style.height = 36;
        scanBtn.style.paddingLeft = 18; scanBtn.style.paddingRight = 18;
        scanBtn.style.backgroundColor = AccentSystem; scanBtn.style.color = Color.white;
        scanBtn.style.borderTopLeftRadius = 18; scanBtn.style.borderTopRightRadius = 18;
        scanBtn.style.borderBottomLeftRadius = 18; scanBtn.style.borderBottomRightRadius = 18;
        scanBtn.style.fontSize = 14; scanBtn.style.unityFontDefinition = Fd();
        scoreCard.Add(scanBtn);
        body.Add(scoreCard);

        var listCard = BuildAppCard("安全防御组件");
        listCard.Add(BuildLightRow("病毒与木马查杀", "病毒库版本：2076.01.10 最新", () => ShowToast("病毒库为最新版")));
        listCard.Add(BuildLightRow("应用权限防护", "22 个应用已通过安全沙箱审核", () => ShowToast("权限配置安全")));
        listCard.Add(BuildLightRow("骚扰拦截与通信防火墙", "外部未知频段电波已被屏蔽", () => ShowToast("防火墙运行中")));
        body.Add(listCard);
    }

    // ============ App: 浏览器 ============
    public static void BuildBrowserApp(VisualElement win)
    {
        var body = BuildAppBody(win, "光明浏览器", "阿里郎光明内网 · 铁路文库与门户");

        var searchCard = BuildAppCard();
        var searchRow = new VisualElement();
        searchRow.style.flexDirection = FlexDirection.Row;
        searchRow.style.alignItems = Align.Center;
        searchRow.style.backgroundColor = SurfaceApp;
        searchRow.style.paddingLeft = 12; searchRow.style.paddingRight = 12;
        searchRow.style.height = 38;
        searchRow.style.borderTopLeftRadius = 19; searchRow.style.borderTopRightRadius = 19;
        searchRow.style.borderBottomLeftRadius = 19; searchRow.style.borderBottomRightRadius = 19;
        searchCard.Add(searchRow);

        var searchTxt = new Label("🔒 https://jiayingxu.railway-foundation.org");
        searchTxt.style.fontSize = 13; searchTxt.style.color = InkSecondary; searchTxt.style.unityFontDefinition = Fd();
        searchRow.Add(searchTxt);
        body.Add(searchCard);

        var homeCard = BuildAppCard("推荐门户与馆藏");
        homeCard.Add(BuildLightRow("🏛 嘉颖徐铁路基金官方站点", "全球铁路文化遗产与沙能冲击研究论文库", () => ShowToast("已连接嘉颖徐基金知识库")));
        homeCard.Add(BuildLightRow("📖 金日成综合大学铁道系期刊", "0721型机车先民内燃机图纸与热效率分析", () => ShowToast("论文文献已加载")));
        homeCard.Add(BuildLightRow("🗺 雾峰至乌金岭矿区综合勘察志", "地质构造、废弃线路复旧方案纪实", () => ShowToast("勘察志阅读就绪")));
        body.Add(homeCard);
    }

    // ============ App: 应用商店 ============
    public static void BuildAppStoreApp(VisualElement win)
    {
        var body = BuildAppBody(win, "应用市场", "精选工具与经过审计的应用");

        var listCard = BuildAppCard("已上架应用");
        listCard.Add(BuildLightRow("铁路工程计算器 v2.1", "已安装 · 专用于道岔曲率与轨距计算", () => ShowToast("应用已是最新版")));
        listCard.Add(BuildLightRow("列车时刻表 v1.5", "已安装 · 雾峰村线专用发车排班表", () => ShowToast("应用已是最新版")));
        listCard.Add(BuildLightRow("轨道检测工具 v3.0", "已安装 · 万能道尺蓝牙配对应用", () => ShowToast("应用已是最新版")));
        listCard.Add(BuildLightRow("铁路英语词典 v1.0", "已安装 · 先民机车术语英汉对照", () => ShowToast("应用已是最新版")));
        body.Add(listCard);
    }

    // ============ App: 阿里郎商店 ============
    public static void BuildArirangStoreApp(VisualElement win)
    {
        var body = BuildAppBody(win, "阿里郎商店", "国营物资订购与官方数字签名应用市场");

        var listCard = BuildAppCard("认证应用列表");
        listCard.Add(BuildLightRow("阿里郎新闻 v1.0", "官方媒体每日通报 · ★★★★★", () => ShowToast("已核验数字证书")));
        listCard.Add(BuildLightRow("主体百科 v2.0", "先民百科与工农知识库 · ★★★★★", () => ShowToast("已核验数字证书")));
        listCard.Add(BuildLightRow("朝鲜音乐 v1.3", "革命进行曲与铁路工人联欢曲 · ★★★★★", () => ShowToast("已核验数字证书")));
        listCard.Add(BuildLightRow("白头教育 v3.0", "铁路工程技术培训与考试系统 · ★★★★★", () => ShowToast("已核验数字证书")));
        body.Add(listCard);
    }

    // ============ App: 未来网 ============
    public static void BuildMiraeNetApp(VisualElement win)
    {
        var body = BuildAppBody(win, "未来网", "mirae.kp · 科教文卫国家内网接入端");

        var listCard = BuildAppCard("国家网络节点");
        listCard.Add(BuildLightRow("金日成综合大学 · 校园网接入", "林彪悍的研究生学术档案与导师信箱", () => ShowToast("导师信箱当前无新邮件")));
        listCard.Add(BuildLightRow("沙能会社门户 · USET企业网", "沙本位能源每日挂牌交易牌价与行情", () => ShowToast("沙币牌价挂牌中")));
        listCard.Add(BuildLightRow("平壤铁道省技术总局", "铁路支线特许经营准入核验系统", () => ShowToast("特许准入证书有效")));
        body.Add(listCard);
    }

    // ============ App: 白头疫苗 ============
    public static void BuildSiliVaccineApp(VisualElement win)
    {
        var body = BuildAppBody(win, "白头疫苗", "内网终极防御系统 · 驱动级沙盒引擎");

        var statCard = BuildAppCard("防护引擎状态");
        statCard.Add(BuildLightRow("防护模式", "实时防御已开启（锁定不可关闭）", null, false));
        statCard.Add(BuildLightRow("特征码库大小", "0 KB（内网完全免疫未知外来攻击）", null, false));
        statCard.Add(BuildLightRow("扫描引擎校验", "通过 · 阿里郎OS系统级深度挂钩", () => ShowToast("防护引擎运转正常")));
        body.Add(statCard);
    }

    // ============ App: 文件管理 ============
    public static void BuildFileManagerApp(VisualElement win)
    {
        var body = BuildAppBody(win, "文件管理", "阿里郎本地闪存空间 · 存储卷分配与归档");

        // 存储总览大卡（多段色彩分段条 + 统计药丸）
        var storageCard = BuildAppCard();
        storageCard.style.paddingTop = 18; storageCard.style.paddingBottom = 18;

        var sTopRow = new VisualElement();
        sTopRow.style.flexDirection = FlexDirection.Row;
        sTopRow.style.alignItems = Align.Center;
        sTopRow.style.justifyContent = Justify.SpaceBetween;
        storageCard.Add(sTopRow);

        var sTitleCol = new VisualElement();
        var sTitle = new Label("闪存存储占用");
        sTitle.style.fontSize = 13; sTitle.style.color = InkSecondary; sTitle.style.unityFontDefinition = Fd();
        sTitleCol.Add(sTitle);
        var sNum = new Label("已用 35.6 GB / 共 128 GB");
        sNum.style.fontSize = 20; sNum.style.color = InkPrimary; sNum.style.unityFontStyleAndWeight = FontStyle.Bold;
        sNum.style.unityFontDefinition = Fd(); sNum.style.marginTop = 2;
        sTitleCol.Add(sNum);
        sTopRow.Add(sTitleCol);

        var freeTag = BuildTagPill("剩余可用 92.4 GB", new Color(0.12f, 0.72f, 0.50f, 0.15f), new Color(0.12f, 0.72f, 0.50f, 1f), 12f);
        sTopRow.Add(freeTag);

        // 分段色彩条（文档、图片、音频、存档、空闲）
        Color cDoc = new Color(0.23f, 0.51f, 0.96f, 1f);
        Color cImg = new Color(0.92f, 0.28f, 0.60f, 1f);
        Color cAudio = new Color(0.96f, 0.62f, 0.10f, 1f);
        Color cSave = new Color(0.12f, 0.72f, 0.50f, 1f);
        Color cFree = new Color(0f, 0f, 0f, 0.08f);

        var segBar = BuildSegmentedBar(14f, (12f, cDoc), (24f, cImg), (8f, cAudio), (2f, cSave), (82f, cFree));
        segBar.style.marginTop = 14; segBar.style.marginBottom = 12;
        storageCard.Add(segBar);

        // 图例说明行
        var legendRow = new VisualElement();
        legendRow.style.flexDirection = FlexDirection.Row;
        legendRow.style.justifyContent = Justify.SpaceBetween;
        storageCard.Add(legendRow);

        void AddLegendItem(string name, string size, Color color)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;

            var dot = new VisualElement();
            dot.style.width = 8; dot.style.height = 8;
            dot.style.borderTopLeftRadius = 4; dot.style.borderTopRightRadius = 4;
            dot.style.borderBottomLeftRadius = 4; dot.style.borderBottomRightRadius = 4;
            dot.style.backgroundColor = color; dot.style.marginRight = 6;
            item.Add(dot);

            var lbl = new Label($"{name} {size}");
            lbl.style.fontSize = 11; lbl.style.color = InkSecondary; lbl.style.unityFontDefinition = Fd();
            item.Add(lbl);

            legendRow.Add(item);
        }

        AddLegendItem("文档", "12GB", cDoc);
        AddLegendItem("相册", "24GB", cImg);
        AddLegendItem("音频", "8GB", cAudio);
        AddLegendItem("存档", "2GB", cSave);
        AddLegendItem("空闲", "92.4GB", new Color(0.5f, 0.5f, 0.5f, 0.6f));

        body.Add(storageCard);

        // 2x2 文件分类卡片
        var catHeader = new Label("文件目录与分类归档");
        catHeader.style.fontSize = 14; catHeader.style.color = SectionHeaderColor;
        catHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        catHeader.style.unityFontDefinition = Fd();
        catHeader.style.marginBottom = 10;
        body.Add(catHeader);

        var catGrid = new VisualElement();
        catGrid.style.flexDirection = FlexDirection.Row;
        catGrid.style.flexWrap = Wrap.Wrap;
        catGrid.style.justifyContent = Justify.SpaceBetween;
        body.Add(catGrid);

        void AddFileCategoryCard(string icon, Color badgeColor, string title, string count, string desc, System.Action onClick)
        {
            var card = BuildAppCard();
            card.style.width = new Length(49, LengthUnit.Percent);
            card.style.marginBottom = 10;
            card.pickingMode = PickingMode.Position;

            var r1 = new VisualElement();
            r1.style.flexDirection = FlexDirection.Row;
            r1.style.alignItems = Align.Center;
            r1.style.justifyContent = Justify.SpaceBetween;
            card.Add(r1);

            var lGroup = new VisualElement();
            lGroup.style.flexDirection = FlexDirection.Row;
            lGroup.style.alignItems = Align.Center;
            lGroup.Add(BuildSquircleBadge(icon, badgeColor, 32f));

            var tLbl = new Label(title);
            tLbl.style.fontSize = 14; tLbl.style.color = InkPrimary; tLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tLbl.style.unityFontDefinition = Fd(); tLbl.style.marginLeft = 8;
            lGroup.Add(tLbl);
            r1.Add(lGroup);

            var cTag = BuildTagPill(count, SurfaceHover, InkSecondary, 10f);
            r1.Add(cTag);

            var dLbl = new Label(desc);
            dLbl.style.fontSize = 12; dLbl.style.color = InkSecondary; dLbl.style.unityFontDefinition = Fd();
            dLbl.style.marginTop = 8;
            card.Add(dLbl);

            if (onClick != null) card.RegisterCallback<ClickEvent>(_ => onClick());
            catGrid.Add(card);
        }

        AddFileCategoryCard("📁", cDoc, "文书与站务日志", "12 个文件", "包含先民班组排班表、线路轨距图表与沙币结算单", () => ShowToast("文档目录已打开"));
        AddFileCategoryCard("🖼", cImg, "相册剧照与原画", "38 张相片", "历史车站旧影、雾峰晚霞、通车剪彩与相册高保真剧照", () => ShowToast("相册目录就绪"));
        AddFileCategoryCard("🎵", cAudio, "铁道广播与随身听", "8 首曲目", "机车鸣笛采音、晨曦广播与随身听数字母带", () => ShowToast("音频库加载完毕"));
        AddFileCategoryCard("💾", cSave, "本地存档快照卷", "6 个槽位", "全自动创世核种子、经营天数快照与剧情回顾标记", () => ShowToast("存档卷健康"));
    }

    // ============ App: 地图 ============
    public static void BuildMapApp(VisualElement win)
    {
        var body = BuildAppBody(win, "铁路地图", "雾峰支线全线拓扑图与周边干线规划图");

        // 真实道床双轨线路图卡片 (Graphic Schematic Route Canvas)
        var mapCard = BuildAppCard("雾峰支线行车拓扑路线图 (全长 23.0 km)");
        
        var trackCanvas = new VisualElement();
        trackCanvas.style.height = 110;
        trackCanvas.style.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 0.95f);
        trackCanvas.style.borderTopLeftRadius = RadiusCard; trackCanvas.style.borderTopRightRadius = RadiusCard;
        trackCanvas.style.borderBottomLeftRadius = RadiusCard; trackCanvas.style.borderBottomRightRadius = RadiusCard;
        trackCanvas.style.position = Position.Relative;
        trackCanvas.style.overflow = Overflow.Hidden;
        trackCanvas.style.marginTop = 6; trackCanvas.style.marginBottom = 14;
        mapCard.Add(trackCanvas);

        // 枕木带
        for (int x = 20; x < 950; x += 28)
        {
            var tie = new VisualElement();
            tie.style.position = Position.Absolute;
            tie.style.left = x; tie.style.top = 45;
            tie.style.width = 4; tie.style.height = 20;
            tie.style.backgroundColor = new Color(0.35f, 0.30f, 0.25f, 0.7f);
            trackCanvas.Add(tie);
        }

        // 琥珀金色双轨钢轨
        var railTop = new VisualElement();
        railTop.style.position = Position.Absolute;
        railTop.style.left = 16; railTop.style.right = 16; railTop.style.top = 49;
        railTop.style.height = 2; railTop.style.backgroundColor = AccentSystem;
        trackCanvas.Add(railTop);

        var railBottom = new VisualElement();
        railBottom.style.position = Position.Absolute;
        railBottom.style.left = 16; railBottom.style.right = 16; railBottom.style.top = 59;
        railBottom.style.height = 2; railBottom.style.backgroundColor = AccentSystem;
        trackCanvas.Add(railBottom);

        // 4大车站站点节点
        (string name, float leftPercent, string km, string role, Color color)[] stations = {
            ("雾峰村", 12f, "0.0 km", "始发站", new Color(0.15f, 0.75f, 0.40f, 1f)),
            ("茶山站", 36f, "8.0 km", "中间站", new Color(0.23f, 0.51f, 0.96f, 1f)),
            ("松桥站", 60f, "15.0 km", "交会站", new Color(0.05f, 0.68f, 0.88f, 1f)),
            ("乌金岭", 84f, "23.0 km", "终点站", new Color(0.96f, 0.62f, 0.10f, 1f))
        };

        foreach (var st in stations)
        {
            var stBox = new VisualElement();
            stBox.style.position = Position.Absolute;
            stBox.style.left = new Length(st.leftPercent, LengthUnit.Percent);
            stBox.style.top = 22;
            stBox.style.alignItems = Align.Center;
            stBox.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));

            // 站名
            var nameLbl = new Label(st.name);
            nameLbl.style.fontSize = 13; nameLbl.style.color = Color.white;
            nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.unityFontDefinition = Fd();
            stBox.Add(nameLbl);

            // 信号灯圆点
            var dot = new VisualElement();
            dot.style.width = 16; dot.style.height = 16;
            dot.style.borderTopLeftRadius = 8; dot.style.borderTopRightRadius = 8;
            dot.style.borderBottomLeftRadius = 8; dot.style.borderBottomRightRadius = 8;
            dot.style.backgroundColor = st.color;
            dot.style.borderTopWidth = 3; dot.style.borderBottomWidth = 3;
            dot.style.borderLeftWidth = 3; dot.style.borderRightWidth = 3;
            dot.style.borderTopColor = Color.white; dot.style.borderBottomColor = Color.white;
            dot.style.borderLeftColor = Color.white; dot.style.borderRightColor = Color.white;
            dot.style.marginTop = 4; dot.style.marginBottom = 4;
            stBox.Add(dot);

            // 里程与角色标签
            var kmLbl = new Label($"{st.km} · {st.role}");
            kmLbl.style.fontSize = 10; kmLbl.style.color = new Color(0.85f, 0.85f, 0.90f, 0.9f);
            kmLbl.style.unityFontDefinition = Fd();
            stBox.Add(kmLbl);

            trackCanvas.Add(stBox);
        }

        body.Add(mapCard);

        // 各区域沙能渗透监视 (2x2 卡片)
        var monitorHeader = new Label("沿线车站沙能渗透态势与客流分析");
        monitorHeader.style.fontSize = 14; monitorHeader.style.color = SectionHeaderColor;
        monitorHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        monitorHeader.style.unityFontDefinition = Fd();
        monitorHeader.style.marginBottom = 10;
        body.Add(monitorHeader);

        var monitorGrid = new VisualElement();
        monitorGrid.style.flexDirection = FlexDirection.Row;
        monitorGrid.style.flexWrap = Wrap.Wrap;
        monitorGrid.style.justifyContent = Justify.SpaceBetween;
        body.Add(monitorGrid);

        void AddStationMonitorCard(string name, string role, float penRatio, Color penColor, string desc)
        {
            var card = BuildAppCard();
            card.style.width = new Length(49, LengthUnit.Percent);
            card.style.marginBottom = 10;

            var r1 = new VisualElement();
            r1.style.flexDirection = FlexDirection.Row;
            r1.style.justifyContent = Justify.SpaceBetween;
            r1.style.alignItems = Align.Center;
            card.Add(r1);

            var tLbl = new Label(name);
            tLbl.style.fontSize = 15; tLbl.style.color = InkPrimary; tLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            tLbl.style.unityFontDefinition = Fd();
            r1.Add(tLbl);

            var pTag = BuildTagPill($"沙能渗透 {Mathf.RoundToInt(penRatio * 100)}%", new Color(penColor.r, penColor.g, penColor.b, 0.15f), penColor, 10f);
            r1.Add(pTag);

            var bar = BuildProgressBar(penRatio, penColor, 6f);
            bar.style.marginTop = 10; bar.style.marginBottom = 6;
            card.Add(bar);

            var dLbl = new Label(desc);
            dLbl.style.fontSize = 12; dLbl.style.color = InkSecondary; dLbl.style.unityFontDefinition = Fd();
            card.Add(dLbl);

            monitorGrid.Add(card);
        }

        AddStationMonitorCard("雾峰村 (始发站)", "客货两用", 0.15f, new Color(0.12f, 0.72f, 0.50f, 1f), "传统铁路基本盘坚固，茶农日常通勤主要依靠0721内燃客车");
        AddStationMonitorCard("茶山站 (高山梯田)", "观光通勤", 0.18f, new Color(0.23f, 0.51f, 0.96f, 1f), "茶园梯田风光秀美，茶农外运早茶主要货源集散节点");
        AddStationMonitorCard("松桥站 (技术维护)", "供水避让", 0.16f, new Color(0.05f, 0.68f, 0.88f, 1f), "拥有全线唯一的双线会车线与机车注水鹤管，避让重载列车");
        AddStationMonitorCard("乌金岭矿区站 (终点)", "重载货运", 0.20f, new Color(0.96f, 0.62f, 0.10f, 1f), "矿山企业倾向大运量原煤外运，沙能重载货车竞争逐渐加剧");

        // 规划延申线路提示卡
        var planCard = BuildAppCard("远期路网规划（待解锁）");
        planCard.Add(BuildLightRow("青溪镇联络线 (45.0 km 规划中)", "沙能渗透率 25% · 沙能公路客运竞争激烈，打通后客流翻倍", () => ShowToast("青溪镇路线正在前期勘测"), true));
        planCard.Add(BuildLightRow("云渡港出海干线 (80.0 km 远期目标)", "直通海运集装箱枢纽，重塑全球铁道货运骨干", () => ShowToast("港口干线需先民铁道总署特许批准"), true));
        body.Add(planCard);
    }

    // ============ App: 占位符 ============
    public static void BuildPlaceholderApp(VisualElement win, string name)
    {
        var body = BuildAppBody(win, name, "系统官方规划应用");
        var card = BuildAppCard();
        card.style.alignItems = Align.Center;
        card.style.paddingTop = 32; card.style.paddingBottom = 32;

        var icon = new Label("⏳");
        icon.style.fontSize = 40; icon.style.unityFontDefinition = Fd();
        card.Add(icon);

        var t = new Label($"{name} · 即将推出");
        t.style.fontSize = 18; t.style.color = InkPrimary;
        t.style.unityFontStyleAndWeight = FontStyle.Bold;
        t.style.unityFontDefinition = Fd(); t.style.marginTop = 10;
        card.Add(t);

        var sub = new Label("该模块正由阿里郎工程师适配中，请随着经营天数推进解锁。");
        sub.style.fontSize = 13; sub.style.color = InkSecondary;
        sub.style.unityFontDefinition = Fd(); sub.style.marginTop = 6;
        card.Add(sub);

        body.Add(card);
    }


    }
}
