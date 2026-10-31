using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;

/// <summary>
/// 平板设置 App 车载连接去假化（分类 4–7：WLAN、蓝牙、互联互通、更多连接方式）自动化验收脚本。
/// 验证 4–7 各自渲染专属说明页与稳定 UI 名称、彻底消除通用三行占位模板、
/// 保持纯只读无误导箭头与假操作、文案符合车载内网沙箱事实且不虚构网络与设备、
/// 以及回归验证分类 11 的通知与控制面板入口保持完好。
/// </summary>
public static class TabletSettingsConnectivityReview
{
    private static GameObject hostGo;
    private static UIDocument hostDoc;

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("[TabletSettingsConnectivityReview Assert Failed] " + message);
        }
    }

    private static VisualElement SetupTestRoot()
    {
        if (hostGo == null)
        {
            hostGo = new GameObject("SettingsConnectivityTestRoot");
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

    private static void AssertNoPlaceholderStrings(VisualElement container, string categoryName)
    {
        var labels = container.Query<Label>().ToList();
        foreach (var l in labels)
        {
            if (string.IsNullOrEmpty(l.text)) continue;
            Assert(!l.text.Contains("此功能尚未开放"), $"Category '{categoryName}' must NOT contain '此功能尚未开放'. Found: {l.text}");
            Assert(!l.text.Contains("基本配置"), $"Category '{categoryName}' must NOT contain placeholder '基本配置'. Found: {l.text}");
            Assert(!l.text.Contains("高级选项与网络策略"), $"Category '{categoryName}' must NOT contain placeholder '高级选项与网络策略'. Found: {l.text}");
            Assert(!l.text.Contains("权限与策略说明"), $"Category '{categoryName}' must NOT contain placeholder '权限与策略说明'. Found: {l.text}");
        }
    }

    private static void AssertReadOnlyNoFakeOperations(VisualElement container, string categoryName)
    {
        // 1. 无任何可点击 Button
        var buttons = container.Query<Button>().ToList();
        Assert(buttons.Count == 0, $"Category '{categoryName}' must have NO buttons. Found {buttons.Count}");

        // 2. 无可交互控件（Toggle、Slider、TextField）
        Assert(container.Query<Toggle>().ToList().Count == 0, $"Category '{categoryName}' must have NO Toggle.");
        Assert(container.Query<Slider>().ToList().Count == 0, $"Category '{categoryName}' must have NO Slider.");
        Assert(container.Query<TextField>().ToList().Count == 0, $"Category '{categoryName}' must have NO TextField.");

        // 3. 所有行均无误导性箭头（›）
        var labels = container.Query<Label>().ToList();
        foreach (var l in labels)
        {
            if (l.text != null && l.text.Trim() == "›")
            {
                throw new InvalidOperationException($"Category '{categoryName}' must NOT contain arrow '›'. Found: {l.text}");
            }
        }
    }

    [MenuItem("Railway/Review/Tablet Settings Connectivity Review")]
    public static void Batch()
    {
        Debug.Log("==================================================");
        Debug.Log("[TabletSettingsConnectivityReview] Starting automated validation batch...");

        var catField = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic);
        int origCategory = (int)catField.GetValue(null);

        try
        {
            var root = SetupTestRoot();

            // 1. 分类 4: WLAN
            catField.SetValue(null, 4);
            TestCategory4_WLAN(root);

            // 2. 分类 5: 蓝牙
            catField.SetValue(null, 5);
            TestCategory5_Bluetooth(root);

            // 3. 分类 6: 互联互通
            catField.SetValue(null, 6);
            TestCategory6_Interconnect(root);

            // 4. 分类 7: 更多连接方式
            catField.SetValue(null, 7);
            TestCategory7_MoreConnections(root);

            // 5. 分类 11 通知与控制面板回归
            catField.SetValue(null, 11);
            TestCategory11_Regression(root);

            Debug.Log("==================================================");
            Debug.Log("TABLET_SETTINGS_CONNECTIVITY_REVIEW_OK: all automated acceptance criteria passed successfully.");
            Debug.Log("==================================================");
        }
        finally
        {
            catField.SetValue(null, origCategory);
            TeardownTestRoot();
        }
    }

    /// <summary>
    /// 用例 1: 分类 4 WLAN 专属页面、稳定 UI 标识、无占位模板、纯只读与文案事实。
    /// </summary>
    private static void TestCategory4_WLAN(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var page = win.Q("settings-wlan-page");
        Assert(page != null, "settings-wlan-page must exist for Category 4");

        var card = win.Q("settings-wlan-card");
        Assert(card != null, "settings-wlan-card must exist for Category 4");

        // 验证标题为 WLAN
        var header = page.Q<Label>();
        Assert(header != null && header.text == "WLAN", $"Header must be 'WLAN', got: {header?.text}");

        // 验证彻底消除占位模板
        AssertNoPlaceholderStrings(page, "WLAN");

        // 验证纯只读与无假操作/箭头
        AssertReadOnlyNoFakeOperations(page, "WLAN");

        // 验证已实现的接入边界，不把未证实的硬件/链路状态写成事实。
        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        Assert(text.Contains("车载内网沙箱"), "WLAN must retain the established vehicle intranet sandbox setting: " + text);
        Assert(text.Contains("不提供公共 Wi-Fi 接入"), "WLAN must state the terminal does not offer public Wi-Fi access: " + text);
        Assert(text.Contains("不显示可扫描的无线网络") && text.Contains("不在此页显示"), "WLAN must avoid implying live scan/status data: " + text);
        Assert(!text.Contains("已连接") && !text.Contains("信号强") && !text.Contains("5G Wi-Fi"), "WLAN must NOT claim fake connection/signal: " + text);

        Debug.Log("[TabletSettingsConnectivityReview] Test 1 Passed: Category 4 (WLAN) verified dedicated, read-only and fact-aligned.");
    }

    /// <summary>
    /// 用例 2: 分类 5 蓝牙专属页面、稳定 UI 标识、无占位模板、纯只读与文案事实。
    /// </summary>
    private static void TestCategory5_Bluetooth(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var page = win.Q("settings-bluetooth-page");
        Assert(page != null, "settings-bluetooth-page must exist for Category 5");

        var card = win.Q("settings-bluetooth-card");
        Assert(card != null, "settings-bluetooth-card must exist for Category 5");

        var header = page.Q<Label>();
        Assert(header != null && header.text == "蓝牙", $"Header must be '蓝牙', got: {header?.text}");

        AssertNoPlaceholderStrings(page, "蓝牙");
        AssertReadOnlyNoFakeOperations(page, "蓝牙");

        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        Assert(text.Contains("不提供蓝牙扫描、发现或配对入口"), "Bluetooth must explain that scan/discovery/pairing controls are absent: " + text);
        Assert(text.Contains("不通过此页面进行无线配对"), "Bluetooth must not imply a pairing action: " + text);
        Assert(!text.Contains("已配对") && !text.Contains("已连接") && !text.Contains("附近的设备"), "Bluetooth must NOT claim fake pairing/devices: " + text);

        Debug.Log("[TabletSettingsConnectivityReview] Test 2 Passed: Category 5 (Bluetooth) verified dedicated, read-only and fact-aligned.");
    }

    /// <summary>
    /// 用例 3: 分类 6 互联互通专属页面、稳定 UI 标识、无占位模板、纯只读与文案事实。
    /// </summary>
    private static void TestCategory6_Interconnect(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var page = win.Q("settings-interconnect-page");
        Assert(page != null, "settings-interconnect-page must exist for Category 6");

        var card = win.Q("settings-interconnect-card");
        Assert(card != null, "settings-interconnect-card must exist for Category 6");

        var header = page.Q<Label>();
        Assert(header != null && header.text == "互联互通", $"Header must be '互联互通', got: {header?.text}");

        AssertNoPlaceholderStrings(page, "互联互通");
        AssertReadOnlyNoFakeOperations(page, "互联互通");

        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        Assert(text.Contains("列车编组与机车协同由行车设备管理"), "Interconnect must place consist coordination with locomotive operating equipment: " + text);
        Assert(text.Contains("不提供个人账户登录或跨设备云同步服务"), "Interconnect must state no personal account/cloud sync service: " + text);
        Assert(!text.Contains("已同步") && !text.Contains("发现我的设备") && !text.Contains("多屏协同"), "Interconnect must NOT claim fake sync/devices: " + text);

        Debug.Log("[TabletSettingsConnectivityReview] Test 3 Passed: Category 6 (Interconnect) verified dedicated, read-only and fact-aligned.");
    }

    /// <summary>
    /// 用例 4: 分类 7 更多连接方式专属页面、稳定 UI 标识、无占位模板、纯只读与文案事实。
    /// </summary>
    private static void TestCategory7_MoreConnections(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var page = win.Q("settings-more-connections-page");
        Assert(page != null, "settings-more-connections-page must exist for Category 7");

        var card = win.Q("settings-more-connections-card");
        Assert(card != null, "settings-more-connections-card must exist for Category 7");

        var header = page.Q<Label>();
        Assert(header != null && header.text == "更多连接方式", $"Header must be '更多连接方式', got: {header?.text}");

        AssertNoPlaceholderStrings(page, "更多连接方式");
        AssertReadOnlyNoFakeOperations(page, "更多连接方式");

        var labels = page.Query<Label>().ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var l in labels) if (!string.IsNullOrEmpty(l.text)) sb.Append(l.text).Append(" ");
        string text = sb.ToString();

        Assert(text.Contains("不提供便携热点或网络共享入口"), "More connections must state no hotspot/tethering entry: " + text);
        Assert(text.Contains("不提供无线投屏或外接显示控制入口"), "More connections must state no wireless casting/external display control: " + text);
        Assert(!text.Contains("热点已开启") && !text.Contains("正在投屏"), "More connections must NOT claim fake hotspot/cast: " + text);

        Debug.Log("[TabletSettingsConnectivityReview] Test 4 Passed: Category 7 (More Connections) verified dedicated, read-only and fact-aligned.");
    }

    /// <summary>
    /// 用例 5: 分类 11 通知与控制面板回归验证。
    /// </summary>
    private static void TestCategory11_Regression(VisualElement root)
    {
        root.Clear();
        var win = new VisualElement { name = "test-settings-win" };
        root.Add(win);

        TabletSettingsAppUI.BuildSettingsPage(win);

        var notifBtn = win.Q("settings-open-notifications-btn");
        var ctrlBtn = win.Q("settings-open-controls-btn");
        var panelCard = win.Q("settings-notifications-panel-card");
        var infoCard = win.Q("settings-notifications-info-card");

        Assert(notifBtn != null, "settings-open-notifications-btn must still exist in Category 11");
        Assert(ctrlBtn != null, "settings-open-controls-btn must still exist in Category 11");
        Assert(panelCard != null, "settings-notifications-panel-card must still exist in Category 11");
        Assert(infoCard != null, "settings-notifications-info-card must still exist in Category 11");

        // 执行既有通知 Review；它会自行标明哪些子项需要独立的系统壳 Review。
        TabletSettingsNotificationReview.Batch();

        Debug.Log("[TabletSettingsConnectivityReview] Test 5 Passed: Category 11 entry points remain present; notification review ran and reports its own scope.");
    }
}
