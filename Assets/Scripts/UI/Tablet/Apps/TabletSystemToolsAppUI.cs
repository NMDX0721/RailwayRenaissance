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
        funcCard.Add(BuildLightRow("⏱ 运行耗时秒表", "用于测定雾峰站至茶山站实际单程区间时分", () => ShowToast("秒表功能就绪")));
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
        var body = BuildAppBody(win, "气象监视", "雾峰村气象站 · 实时气象与水文参数");

        var weatherCard = BuildAppCard();
        weatherCard.style.alignItems = Align.Center;
        weatherCard.style.paddingTop = 20; weatherCard.style.paddingBottom = 20;

        var temp = new Label("18°C");
        temp.style.fontSize = 54; temp.style.color = InkPrimary;
        temp.style.unityFontStyleAndWeight = FontStyle.Bold;
        temp.style.unityFontDefinition = Fd();
        weatherCard.Add(temp);

        var cond = new Label("☁ 多云 · 东北风 2级 · 湿度 62%");
        cond.style.fontSize = 15; cond.style.color = InkSecondary;
        cond.style.unityFontDefinition = Fd(); cond.style.marginTop = 6;
        weatherCard.Add(cond);
        body.Add(weatherCard);

        var impactCard = BuildAppCard("铁路运营与沙能影响评估");
        impactCard.Add(BuildLightRow("轨面附着系数", "良好 · 干燥无沙尘暴影响", null, false));
        impactCard.Add(BuildLightRow("沙能载具影响", "沙粒流转阻力 0.12 · 竞争对手运力平稳", null, false));
        impactCard.Add(BuildLightRow("未来24小时预警", "明日午后有薄雾，建议提早开启0721机车大灯", null, false));
        body.Add(impactCard);

        var forecastCard = BuildAppCard("多日天气预报");
        forecastCard.Add(BuildLightRow("周一（今日）", "多云 18°C / 9°C · 优", null, false));
        forecastCard.Add(BuildLightRow("周二（明日）", "晴间多云 21°C / 11°C · 优", null, false));
        forecastCard.Add(BuildLightRow("周三（后日）", "小雨转阴 15°C / 8°C · 轨面湿滑注意制动", null, false));
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
                btn.style.borderTopLeftRadius = 8; btn.style.borderTopRightRadius = 8;
                btn.style.borderBottomLeftRadius = 8; btn.style.borderBottomRightRadius = 8;
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
        var body = BuildAppBody(win, "机车遥测", "米家物联网 · 0721型「沙子飞猪号」机车网关");

        var statusCard = BuildAppCard("动力单元遥测总览");
        statusCard.Add(BuildLightRow("沙能储箱余量", "1,200 / 1,500 kg (80%)", null, false));
        statusCard.Add(BuildLightRow("动力电池组蓄电量", "78% · 正常放电", null, false));
        statusCard.Add(BuildLightRow("预估安全运行里程", "340 km", null, false));
        statusCard.Add(BuildLightRow("本月累计沙耗", "312 kg 先民高纯度动力沙", null, false));
        body.Add(statusCard);

        var deviceCard = BuildAppCard("机载智能节点");
        deviceCard.Add(BuildLightRow("万能道尺无线遥测模块", "已连接 · 精度 ±0.5mm", () => ShowToast("道尺信号正常")));
        deviceCard.Add(BuildLightRow("机车行车记录仪", "循环录像中 · 存储剩余 18.4GB", () => ShowToast("录像文件保护已开启")));
        deviceCard.Add(BuildLightRow("轴承振动监视传感器", "全轴正常 · 无偏磨", () => ShowToast("机修工张工巡检记录已同步")));
        body.Add(deviceCard);
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
        var body = BuildAppBody(win, "文件管理", "内部存储空间 128GB · 剩余可用 92.4GB");

        var catCard = BuildAppCard("文件分类");
        catCard.Add(BuildLightRow("📁 文档 (12 项)", "包含站务日志、道尺检修表与班组排班表", () => ShowToast("文档目录就绪")));
        catCard.Add(BuildLightRow("🖼 图片 (38 项)", "车站风景、通车剪彩与相册剧照", () => ShowToast("图片目录就绪")));
        catCard.Add(BuildLightRow("🎵 音频 (8 项)", "机车鸣笛声、铁道广播与随身听曲目", () => ShowToast("音频目录就绪")));
        catCard.Add(BuildLightRow("💾 存档 (6 槽位)", "本地游戏进度快照与剧情回顾标记", () => ShowToast("存档目录正常")));
        body.Add(catCard);
    }

    // ============ App: 地图 ============
    public static void BuildMapApp(VisualElement win)
    {
        var body = BuildAppBody(win, "铁路地图", "雾峰支线与周边干线走向规划图");

        var mapCard = BuildAppCard("铁路线路总览");
        var map = new Label(
            "  雾峰村 ─── 8km ─── 茶山站 ─── 7km ─── 松桥站 ─── 8km ─── 矿区站\n" +
            "  │\n" +
            "  └── 45km ─── 青溪镇（规划中）\n" +
            "       └── 80km ─── 云渡港（待解锁）");
        map.style.fontSize = 13; map.style.color = InkPrimary;
        map.style.unityFontDefinition = Fd();
        map.style.whiteSpace = WhiteSpace.Pre;
        map.style.paddingTop = 10; map.style.paddingBottom = 10;
        mapCard.Add(map);
        body.Add(mapCard);

        var infoCard = BuildAppCard("区域沙能渗透监视");
        infoCard.Add(BuildLightRow("雾峰村", "沙能渗透率 15% · 传统铁路基本盘坚固", null, false));
        infoCard.Add(BuildLightRow("乌金岭矿区", "沙能渗透率 20% · 矿商倾向重载货运", null, false));
        infoCard.Add(BuildLightRow("青溪镇", "沙能渗透率 25% · 沙能公路客运竞争激烈", null, false));
        body.Add(infoCard);
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
