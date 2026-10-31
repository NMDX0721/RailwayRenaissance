using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;
using RailwayRenaissance.UI.Tablet.SystemShell;

/// <summary>
/// 平板设置 App 分类 11「通知与状态栏」自动化验收脚本。
/// 验证分类 11 专属页面渲染（无通用占位）、通知中心与控制面板入口分别请求与单次触发、
/// 未注入回调时的安全降级提示、未注入测试通知时的数据纯洁性、以及无虚假声音/删通知文案断言。
/// </summary>
public static class TabletSettingsNotificationReview
{
    private static GameObject hostGo;
    private static UIDocument hostDoc;

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("[TabletSettingsNotificationReview Assert Failed] " + message);
        }
    }

    private static VisualElement SetupTestRoot()
    {
        if (hostGo == null)
        {
            hostGo = new GameObject("SettingsNotificationTestRoot");
            hostDoc = hostGo.AddComponent<UIDocument>();
            var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
            hostDoc.panelSettings = panelSettings;
            hostDoc.visualTreeAsset = null;
        }
        var root = hostDoc.rootVisualElement;
        root.Clear();
        return root;
    }

    private static void TeardownTestRoot()
    {
        if (hostGo != null)
        {
            UnityEngine.Object.DestroyImmediate(hostGo);
            hostGo = null;
            hostDoc = null;
        }
    }

    private static void ClickElement(VisualElement el)
    {
        Assert(el != null, "Element must not be null");
        using var evt = ClickEvent.GetPooled();
        evt.target = el;
        el.SendEvent(evt);
    }

    [MenuItem("Railway/Review/Tablet Settings Notification Review")]
    public static void Batch()
    {
        Debug.Log("==================================================");
        Debug.Log("[TabletSettingsNotificationReview] Starting automated validation batch...");

        var catField = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic);
        int origCategory = (int)catField.GetValue(null);

        try
        {
            var root = SetupTestRoot();

            // 设置激活分类为 11（通知与状态栏）
            catField.SetValue(null, 11);

            Test1_Category11DedicatedPageRendering(root);
            Test2_EntryInvocationsAndDegradation(root);
            Test3_ShellStateInvariantsAndTextAccuracy(root);
            Test4_RegressionSuiteStatus();

            Debug.Log("==================================================");
            Debug.Log("TABLET_SETTINGS_NOTIFICATIONS_REVIEW_PARTIAL: criteria 1-3 passed; TabletSystemShellReview.Batch was not executed by this run.");
            Debug.Log("==================================================");
        }
        finally
        {
            catField.SetValue(null, origCategory);
            TabletSettingsAppUI.SetOpenSystemPanelHandler(null);
            TeardownTestRoot();
        }
    }

    /// <summary>
    /// 用例 1: 选择分类 11 显示专属页面，包含面板入口与运行说明，彻底消除三行「此功能尚未开放」占位。
    /// </summary>
    private static void Test1_Category11DedicatedPageRendering(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        // 验证标题与专属卡片
        var labels = win.Query<Label>().ToList();
        bool foundHeader = false;
        bool foundPlaceholder = false;

        foreach (var l in labels)
        {
            if (l.text == "通知与状态栏") foundHeader = true;
            if (l.text != null && l.text.Contains("此功能尚未开放")) foundPlaceholder = true;
            if (l.text != null && (l.text.Contains("基本配置") || l.text.Contains("高级选项与网络策略")))
            {
                throw new InvalidOperationException("Found placeholder row: " + l.text);
            }
        }

        Assert(foundHeader, "Category 11 header '通知与状态栏' must be rendered");
        Assert(!foundPlaceholder, "Category 11 must NOT contain generic placeholder '此功能尚未开放'");

        var notifBtn = win.Q("settings-open-notifications-btn");
        Assert(notifBtn != null, "Quick access entry 'settings-open-notifications-btn' must exist");

        var ctrlBtn = win.Q("settings-open-controls-btn");
        Assert(ctrlBtn != null, "Quick access entry 'settings-open-controls-btn' must exist");

        var panelCard = win.Q("settings-notifications-panel-card");
        Assert(panelCard != null, "Card 'settings-notifications-panel-card' must exist");

        var infoCard = win.Q("settings-notifications-info-card");
        Assert(infoCard != null, "Card 'settings-notifications-info-card' must exist");

        Debug.Log("[TabletSettingsNotificationReview] Test 1 Passed: Category 11 renders dedicated page with quick access entries, no placeholder.");
    }

    /// <summary>
    /// 用例 2: 两个入口分别请求 Notifications 与 Controls，且每次只触发一次；未注入入口回调时无异常并显示降级提示。
    /// </summary>
    private static void Test2_EntryInvocationsAndDegradation(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        // 2.1 注入真实/模拟回调测试：单次触发与目标面板准确性
        TabletSystemPanel requestedPanel = TabletSystemPanel.None;
        int invokeCount = 0;
        Action<TabletSystemPanel> mockHandler = p =>
        {
            requestedPanel = p;
            invokeCount++;
        };

        TabletSettingsAppUI.BuildSettingsPage(win, null, mockHandler);

        var notifBtn = win.Q("settings-open-notifications-btn");
        var ctrlBtn = win.Q("settings-open-controls-btn");
        Assert(notifBtn != null && ctrlBtn != null, "Buttons must exist for injection test");

        // 点击打开通知中心
        ClickElement(notifBtn);
        Assert(invokeCount == 1, $"Notifications button must invoke handler exactly once. Got: {invokeCount}");
        Assert(requestedPanel == TabletSystemPanel.Notifications, $"Expected TabletSystemPanel.Notifications, got {requestedPanel}");

        // 重置计数并点击打开控制面板
        invokeCount = 0;
        requestedPanel = TabletSystemPanel.None;
        ClickElement(ctrlBtn);
        Assert(invokeCount == 1, $"Controls button must invoke handler exactly once. Got: {invokeCount}");
        Assert(requestedPanel == TabletSystemPanel.Controls, $"Expected TabletSystemPanel.Controls, got {requestedPanel}");

        // 2.2 未注入回调测试：无异常并提供准确降级反馈
        TabletSettingsAppUI.SetOpenSystemPanelHandler(null);
        string lastToast = null;
        TabletSettingsAppUI.BuildSettingsPage(win, msg => lastToast = msg, null);

        // 验证降级说明标签存在
        var fallbackNote = win.Q<Label>("settings-notifications-fallback-note");
        Assert(fallbackNote != null, "Fallback note element 'settings-notifications-fallback-note' must exist when un-injected");
        Assert(fallbackNote.text.Contains("未挂载系统面板服务"), "Fallback note must inform about missing system panel service: " + fallbackNote.text);

        // 点击未注入的通知中心按钮：不抛出异常，给出降级 Toast
        notifBtn = win.Q("settings-open-notifications-btn");
        ClickElement(notifBtn);
        Assert(lastToast != null, "Clicking un-injected notifications button must produce toast message");
        Assert(lastToast.Contains("通知中心") && (lastToast.Contains("不可用") || lastToast.Contains("状态栏")), "Toast must provide accurate degradation guidance: " + lastToast);

        // 点击未注入的控制面板按钮：不抛出异常，给出降级 Toast
        lastToast = null;
        ctrlBtn = win.Q("settings-open-controls-btn");
        ClickElement(ctrlBtn);
        Assert(lastToast != null, "Clicking un-injected controls button must produce toast message");
        Assert(lastToast.Contains("控制面板") && (lastToast.Contains("不可用") || lastToast.Contains("状态栏")), "Toast must provide accurate degradation guidance: " + lastToast);

        Debug.Log("[TabletSettingsNotificationReview] Test 2 Passed: Entries invoke Notifications and Controls respectively once per click; degradation without handler verified.");
    }

    /// <summary>
    /// 用例 3: 未向 TabletShellState 注入测试通知时数据保持为空；UI 文案不声称勿扰会删除通知或改变声音。
    /// </summary>
    private static void Test3_ShellStateInvariantsAndTextAccuracy(VisualElement root)
    {
        // 3.1 状态数据纯洁性检查：未注入时不产生假通知
        var freshState = new TabletShellState();
        Assert(freshState.Notifications.Count == 0, $"Initial state must have 0 notifications. Found: {freshState.Notifications.Count}");
        Assert(!freshState.QuietMode, "Initial quiet mode must be false");

        // 3.2 UI 文案检查
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);
        TabletSettingsAppUI.BuildSettingsPage(win);

        var infoCard = win.Q("settings-notifications-info-card");
        Assert(infoCard != null, "Info card must exist");

        var labels = infoCard.Query<Label>().ToList();
        var textBuilder = new System.Text.StringBuilder();
        foreach (var l in labels)
        {
            if (!string.IsNullOrEmpty(l.text)) textBuilder.Append(l.text).Append(" ");
        }
        string fullText = textBuilder.ToString();

        // 验证说明了 32 条上限
        Assert(fullText.Contains("32"), "Must explain bounded capacity of 32 notifications: " + fullText);

        // 验证说明了勿扰模式下保留收存、仅屏蔽横幅
        Assert(fullText.Contains("保留通知收存") || fullText.Contains("保留"), "Must explain that notifications are retained under quiet mode: " + fullText);
        Assert(fullText.Contains("屏蔽悬浮横幅") || fullText.Contains("横幅"), "Must explain banner suppression under quiet mode: " + fullText);
        Assert(fullText.Contains("进程内有效"), "Must explain quiet mode is process-local: " + fullText);

        // 验证说明了内网沙箱、不虚拟连接状态
        Assert(fullText.Contains("内网") && fullText.Contains("不虚拟"), "Must explain sandbox nature and no virtualized status: " + fullText);

        // 严格断言：绝不声称会清空/删除通知、绝不声称改变/静音硬件声音
        Assert(!fullText.Contains("清空通知") && !fullText.Contains("删除通知"), "UI text must NOT claim quiet mode deletes notifications: " + fullText);
        Assert(!fullText.Contains("静音硬件") && !fullText.Contains("关闭系统声音"), "UI text must NOT claim quiet mode mutes hardware sound: " + fullText);

        Debug.Log("[TabletSettingsNotificationReview] Test 3 Passed: Shell state maintains empty notifications without injection; text accurately states retention and quiet mode policies.");
    }

    /// <summary>
    /// 用例 4: 明确标记既有系统壳回归未在本批次执行，避免把文档说明误报为自动化通过。
    /// </summary>
    private static void Test4_RegressionSuiteStatus()
    {
        Debug.LogWarning("[TabletSettingsNotificationReview] Test 4 NOT RUN: panel exclusivity, scrim dismissal and quiet-mode runtime regression require TabletSystemShellReview.Batch.");
    }
}
