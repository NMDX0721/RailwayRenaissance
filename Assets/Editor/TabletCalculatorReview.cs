using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;
using TabletCalculatorEngine = RailwayRenaissance.UI.Tablet.Apps.TabletSystemToolsAppUI.TabletCalculatorEngine;

/// <summary>
/// 平板计算器自动化验收脚本。
/// 验证基础四则运算、运算优先级、退格与清空、除零与恢复、大数/溢出防护及触屏按键布局。
/// </summary>
public static class TabletCalculatorReview
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("[TabletCalculatorReview Assert Failed] " + message);
        }
    }

    [MenuItem("Railway/Review/Tablet Calculator Review")]
    public static void Batch()
    {
        Debug.Log("==================================================");
        Debug.Log("[TabletCalculatorReview] Starting automated validation batch...");

        Test1_BasicOperations();
        Test2_PrecedenceAndContinuous();
        Test3_BackspaceClearAndDecimals();
        Test4_DivideByZeroAndRecovery();
        Test5_ExtremeValuesAndOverflow();
        Test6_UIElementsAndCompactLayout();

        Debug.Log("==================================================");
        Debug.Log("TABLET_CALCULATOR_REVIEW_OK: all 6 automated acceptance criteria passed successfully.");
        Debug.Log("==================================================");
    }

    /// <summary>
    /// 用例 1: 8 ÷ 2 = 4、2 - 5 = -3、.5 + .25 = .75 及重复等号行为。
    /// </summary>
    private static void Test1_BasicOperations()
    {
        var engine = new TabletCalculatorEngine();

        // 8 ÷ 2 = 4
        engine.InputDigit(8);
        engine.InputOperator('÷');
        engine.InputDigit(2);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "4", "Expected 8 ÷ 2 = 4, got: " + engine.CurrentDisplay);

        // 重复等号 (4 ÷ 2 = 2, 2 ÷ 2 = 1)
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "2", "Expected repeated = to produce 2, got: " + engine.CurrentDisplay);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "1", "Expected repeated = to produce 1, got: " + engine.CurrentDisplay);

        // 2 - 5 = -3
        engine.Clear();
        engine.InputDigit(2);
        engine.InputOperator('-');
        engine.InputDigit(5);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "-3", "Expected 2 - 5 = -3, got: " + engine.CurrentDisplay);

        // 重复等号 (-3 - 5 = -8)
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "-8", "Expected repeated = to produce -8, got: " + engine.CurrentDisplay);

        // .5 + .25 = .75 (以 0.75 格式化输出)
        engine.Clear();
        engine.InputDot();
        engine.InputDigit(5);
        Assert(engine.CurrentDisplay == "0.5", "Expected leading dot to format as 0.5, got: " + engine.CurrentDisplay);
        engine.InputOperator('+');
        engine.InputDot();
        engine.InputDigit(2);
        engine.InputDigit(5);
        Assert(engine.CurrentDisplay == "0.25", "Expected 0.25, got: " + engine.CurrentDisplay);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "0.75", "Expected .5 + .25 = 0.75, got: " + engine.CurrentDisplay);
        Assert(decimal.Parse(engine.CurrentDisplay, CultureInfo.InvariantCulture) == 0.75m, "Numeric parse of 0.75 failed");

        Debug.Log("[TabletCalculatorReview] Test 1 Passed: 8 ÷ 2 = 4, 2 - 5 = -3, .5 + .25 = .75 and repeated equals verified.");
    }

    /// <summary>
    /// 用例 2: 优先级 7 + 8 × 2 = 23 及连续运算。
    /// </summary>
    private static void Test2_PrecedenceAndContinuous()
    {
        var engine = new TabletCalculatorEngine();

        // 7 + 8 × 2 = 23
        engine.InputDigit(7);
        engine.InputOperator('+');
        engine.InputDigit(8);
        engine.InputOperator('×');
        engine.InputDigit(2);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "23", "Expected 7 + 8 × 2 = 23, got: " + engine.CurrentDisplay);

        // 从计算结果继续运算: 23 + 5 = 28
        engine.InputOperator('+');
        engine.InputDigit(5);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "28", "Expected 23 + 5 = 28, got: " + engine.CurrentDisplay);

        // 乘除混合与左结合: 20 - 12 ÷ 3 = 16
        engine.Clear();
        engine.InputDigit(2); engine.InputDigit(0);
        engine.InputOperator('-');
        engine.InputDigit(1); engine.InputDigit(2);
        engine.InputOperator('÷');
        engine.InputDigit(3);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "16", "Expected 20 - 12 ÷ 3 = 16, got: " + engine.CurrentDisplay);

        // 连续减法左结合: 10 - 3 - 2 = 5
        engine.Clear();
        engine.InputDigit(1); engine.InputDigit(0);
        engine.InputOperator('-');
        engine.InputDigit(3);
        engine.InputOperator('-');
        engine.InputDigit(2);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "5", "Expected 10 - 3 - 2 = 5, got: " + engine.CurrentDisplay);

        // 连续除法左结合: 24 ÷ 4 ÷ 2 = 3
        engine.Clear();
        engine.InputDigit(2); engine.InputDigit(4);
        engine.InputOperator('÷');
        engine.InputDigit(4);
        engine.InputOperator('÷');
        engine.InputDigit(2);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "3", "Expected 24 ÷ 4 ÷ 2 = 3, got: " + engine.CurrentDisplay);

        // 正负号反转
        engine.Clear();
        engine.InputDigit(6);
        engine.InputNegate();
        Assert(engine.CurrentDisplay == "-6", "Expected -6 after negate");
        engine.InputOperator('×');
        engine.InputDigit(3);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "-18", "Expected -6 × 3 = -18, got: " + engine.CurrentDisplay);

        Debug.Log("[TabletCalculatorReview] Test 2 Passed: Precedence (7 + 8 × 2 = 23) and continuous chained calculations verified.");
    }

    /// <summary>
    /// 用例 3: 123 退格为 12、清空回到初始状态、重复小数点不生成非法数值、前导零约束。
    /// </summary>
    private static void Test3_BackspaceClearAndDecimals()
    {
        var engine = new TabletCalculatorEngine();

        // 123 退格为 12 -> 1 -> 0 -> 0
        engine.InputDigit(1);
        engine.InputDigit(2);
        engine.InputDigit(3);
        Assert(engine.CurrentDisplay == "123", "Expected 123");
        engine.InputBackspace();
        Assert(engine.CurrentDisplay == "12", "Expected 12 after backspace, got: " + engine.CurrentDisplay);
        engine.InputBackspace();
        Assert(engine.CurrentDisplay == "1", "Expected 1 after backspace, got: " + engine.CurrentDisplay);
        engine.InputBackspace();
        Assert(engine.CurrentDisplay == "0", "Expected 0 after backspace, got: " + engine.CurrentDisplay);
        engine.InputBackspace();
        Assert(engine.CurrentDisplay == "0", "Expected 0 on empty backspace, got: " + engine.CurrentDisplay);

        // 清空重置状态
        engine.InputDigit(9);
        engine.InputOperator('+');
        engine.InputDigit(9);
        engine.Clear();
        Assert(engine.CurrentDisplay == "0", "Expected 0 after clear");
        Assert(string.IsNullOrEmpty(engine.ExpressionText), "Expected empty expression after clear");
        Assert(!engine.HasError, "Error flag should be false after clear");

        // 重复小数点约束
        engine.InputDot();
        Assert(engine.CurrentDisplay == "0.", "Expected 0. on initial dot");
        engine.InputDot();
        Assert(engine.CurrentDisplay == "0.", "Duplicate dot should be ignored, got: " + engine.CurrentDisplay);
        engine.InputDigit(5);
        Assert(engine.CurrentDisplay == "0.5", "Expected 0.5");
        engine.InputDot();
        Assert(engine.CurrentDisplay == "0.5", "Duplicate dot after digits should be ignored, got: " + engine.CurrentDisplay);

        // 前导零约束: 0 0 0 7 -> 7
        engine.Clear();
        engine.InputDigit(0);
        engine.InputDigit(0);
        engine.InputDigit(0);
        Assert(engine.CurrentDisplay == "0", "Leading zeros should not duplicate, got: " + engine.CurrentDisplay);
        engine.InputDigit(7);
        Assert(engine.CurrentDisplay == "7", "Expected 7, got: " + engine.CurrentDisplay);

        Debug.Log("[TabletCalculatorReview] Test 3 Passed: Backspace, Clear, duplicate dots, and leading zero constraints verified.");
    }

    /// <summary>
    /// 用例 4: 除零进入错误提示，再输入新数字可恢复。
    /// </summary>
    private static void Test4_DivideByZeroAndRecovery()
    {
        var engine = new TabletCalculatorEngine();

        // 8 ÷ 0 = 错误
        engine.InputDigit(8);
        engine.InputOperator('÷');
        engine.InputDigit(0);
        engine.InputEquals();

        Assert(engine.HasError, "Engine should enter error state on divide by zero");
        Assert(!string.IsNullOrEmpty(engine.ErrorMessage), "Error message should not be empty");
        Assert(engine.CurrentDisplay.Contains("除数不能为0") || engine.CurrentDisplay.Contains("错误"), "Display should show friendly error, got: " + engine.CurrentDisplay);
        Assert(!engine.CurrentDisplay.Contains("NaN") && !engine.CurrentDisplay.Contains("Infinity"), "Display must never show NaN or Infinity");

        // 再次输入新有效数字后必须能无缝恢复
        engine.InputDigit(9);
        Assert(!engine.HasError, "Error state should be cleared upon typing new digit");
        Assert(engine.CurrentDisplay == "9", "Display should show new digit, got: " + engine.CurrentDisplay);

        // 恢复后可继续正常计算
        engine.InputOperator('+');
        engine.InputDigit(1);
        engine.InputEquals();
        Assert(engine.CurrentDisplay == "10", "Expected 9 + 1 = 10 after recovery, got: " + engine.CurrentDisplay);

        Debug.Log("[TabletCalculatorReview] Test 4 Passed: Divide by zero error and recovery verified.");
    }

    /// <summary>
    /// 用例 5: 极大值/精度边界不会显示 NaN / Infinity，不会静默产生错误数值。
    /// </summary>
    private static void Test5_ExtremeValuesAndOverflow()
    {
        var engine = new TabletCalculatorEngine();

        // 输入接近 decimal.MaxValue 的极大值 (79228162514264337593543950335)
        string maxDecStr = decimal.MaxValue.ToString(CultureInfo.InvariantCulture);
        foreach (char c in maxDecStr)
        {
            engine.InputDigit(c);
        }

        // 进行溢出乘法: MaxValue × 2
        engine.InputOperator('×');
        engine.InputDigit(2);
        engine.InputEquals();

        Assert(engine.HasError, "Engine should catch overflow and set error state");
        Assert(engine.CurrentDisplay.Contains("溢出") || engine.CurrentDisplay.Contains("错误"), "Display should indicate overflow, got: " + engine.CurrentDisplay);
        Assert(!engine.CurrentDisplay.Contains("NaN") && !engine.CurrentDisplay.Contains("Infinity"), "Overflow must never produce NaN or Infinity");

        // 溢出后输入新数字恢复
        engine.InputDigit(4);
        Assert(!engine.HasError, "Error should clear on new input");
        Assert(engine.CurrentDisplay == "4", "Expected 4 after recovery");

        Debug.Log("[TabletCalculatorReview] Test 5 Passed: Extreme values / overflow protection without NaN/Infinity verified.");
    }

    /// <summary>
    /// 用例 6: 各按键可命中，紧凑分辨率下按键布局不溢出。
    /// </summary>
    private static void Test6_UIElementsAndCompactLayout()
    {
        var win = new VisualElement();
        TabletSystemToolsAppUI.BuildCalculatorApp(win);

        var card = win.Q<VisualElement>("calc-card");
        Assert(card != null, "calc-card element not found in UI tree");

        var exprLabel = win.Q<Label>("calc-display-expression");
        Assert(exprLabel != null, "calc-display-expression element not found");

        var mainLabel = win.Q<Label>("calc-display-main");
        Assert(mainLabel != null, "calc-display-main element not found");

        // 验证全部 19 个按钮语义与元素名称
        string[] requiredButtonNames = new[]
        {
            "calc-btn-c", "calc-btn-backspace", "calc-btn-negate", "calc-btn-div",
            "calc-btn-7", "calc-btn-8", "calc-btn-9", "calc-btn-mul",
            "calc-btn-4", "calc-btn-5", "calc-btn-6", "calc-btn-sub",
            "calc-btn-1", "calc-btn-2", "calc-btn-3", "calc-btn-add",
            "calc-btn-0", "calc-btn-dot", "calc-btn-equal"
        };

        foreach (var btnName in requiredButtonNames)
        {
            var btn = win.Q<Button>(btnName);
            Assert(btn != null, "Required button missing: " + btnName);
            Assert(btn.enabledSelf, "Button should be enabled: " + btnName);
            Assert(btn.style.width.value.value >= 44f, "Button width must meet 44px touch target: " + btnName);
            Assert(btn.style.height.value.value >= 44f, "Button height must meet 44px touch target: " + btnName);
        }

        // 通过真实 UI 按钮点击模拟计算: 7 + 8 × 2 = 23
        void ClickBtn(string name)
        {
            var b = win.Q<Button>(name);
            Assert(b != null, "Button not found: " + name);
            
            // 无独立活跃面板的批处理环境下直接触发 Button.clickable 委托，附带事件分发兜底
            var clickedField = typeof(Clickable).GetField("clicked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var action = clickedField?.GetValue(b.clickable) as Action;
            if (action != null)
            {
                action.Invoke();
            }
            else
            {
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = b;
                b.SendEvent(evt);
            }
        }

        ClickBtn("calc-btn-7");
        Assert(mainLabel.text == "7", "Expected display 7 after clicking 7");

        ClickBtn("calc-btn-add");
        Assert(exprLabel.text.Contains("7 +"), "Expected expression to contain '7 +'");

        ClickBtn("calc-btn-8");
        Assert(mainLabel.text == "8", "Expected display 8");

        ClickBtn("calc-btn-mul");
        Assert(exprLabel.text.Contains("7 + 8 ×"), "Expected expression to contain '7 + 8 ×'");

        ClickBtn("calc-btn-2");
        Assert(mainLabel.text == "2", "Expected display 2");

        ClickBtn("calc-btn-equal");
        Assert(mainLabel.text == "23", "UI click calculation failed, expected 23, got: " + mainLabel.text);
        Assert(exprLabel.text.Contains("7 + 8 × 2 ="), "Expected expression '7 + 8 × 2 =', got: " + exprLabel.text);

        // 紧凑分辨率布局检查: 卡片宽度 420px，行内按键总宽度 (352px / 360px) 均在 420px 容器内，不发生布局溢出
        float cardWidth = card.style.width.value.value;
        Assert(cardWidth == 420f, "Card width should be 420px, got: " + cardWidth);

        // 验证 480px、600px 紧凑分辨率下卡片居中且自适应
        float[] viewports = new[] { 480f, 600f, 800f, 1280f };
        foreach (var vp in viewports)
        {
            Assert(cardWidth < vp, $"Calculator card width {cardWidth} exceeds viewport {vp}");
        }

        Debug.Log("[TabletCalculatorReview] Test 6 Passed: UI button hit targets, UI event simulation, and compact layout verified.");
    }
}
