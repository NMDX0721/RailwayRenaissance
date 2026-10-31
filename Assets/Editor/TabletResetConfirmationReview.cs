using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Apps;

/// <summary>
/// 平板设置 App 开发者重置二次确认弹窗与文案规范自动化验收脚本。
/// 验证重置提示修正、二次确认 UI 结构、取消/遮罩/Escape 零副作用关闭、确认回调单次调用及生产行绑定。
/// </summary>
public static class TabletResetConfirmationReview
{
    private static GameObject hostGo;
    private static UIDocument hostDoc;

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("[TabletResetConfirmationReview Assert Failed] " + message);
        }
    }

    private static VisualElement SetupTestRoot()
    {
        if (hostGo == null)
        {
            hostGo = new GameObject("ResetConfirmationTestRoot");
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

    [MenuItem("Railway/Review/Tablet Reset Confirmation Review")]
    public static void Batch()
    {
        Debug.Log("==================================================");
        Debug.Log("[TabletResetConfirmationReview] Starting automated validation batch...");

        try
        {
            var root = SetupTestRoot();

            Test1_DialogElementsAndTextMatching(root);
            Test2_DismissPathsWithZeroSideEffect(root);
            Test3_ConfirmInvocationAndOverlayDeduplication(root);
            Test4_ProductionDeveloperOptionsWiring(root);

            Debug.Log("==================================================");
            Debug.Log("TABLET_RESET_CONFIRMATION_REVIEW_OK: all 4 automated acceptance criteria passed successfully.");
            Debug.Log("==================================================");
        }
        finally
        {
            TeardownTestRoot();
        }
    }

    private static void ClickButton(Button b)
    {
        Assert(b != null, "Button must not be null");
        var clickedField = typeof(Clickable).GetField("clicked", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        var action = clickedField?.GetValue(b.clickable) as Action;
        if (action != null)
        {
            action.Invoke();
        }
        else
        {
            using var evt = ClickEvent.GetPooled();
            evt.target = b;
            b.SendEvent(evt);
        }
    }

    private static void ClickOverlay(VisualElement overlay)
    {
        Assert(overlay != null, "Overlay must not be null");
        using var evt = ClickEvent.GetPooled();
        evt.target = overlay;
        overlay.SendEvent(evt);
    }

    private static void PressEscape(VisualElement overlay)
    {
        Assert(overlay != null, "Overlay must not be null");
        using var evt = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None);
        evt.target = overlay;
        overlay.SendEvent(evt);
    }

    private static void ClickRow(VisualElement row)
    {
        Assert(row != null, "Row must not be null");
        using var evt = ClickEvent.GetPooled();
        evt.target = row;
        row.SendEvent(evt);
    }

    /// <summary>
    /// 用例 1: 弹窗标题、影响说明、确认/取消按钮存在且文案匹配。
    /// </summary>
    private static void Test1_DialogElementsAndTextMatching(VisualElement root)
    {
        root.Clear();
        var host = new VisualElement { name = "test-host" };
        root.Add(host);

        int callCount = 0;
        string expectedTitle = "⚠️ 确认测试标题";
        string expectedDesc = "此操作将执行安全替身测试。\n影响边界说明：完全隔离不写入任何存档。";
        string expectedConfirmBtn = "确认测试执行";

        TabletSettingsAppUI.ShowResetConfirmDialog(host, expectedTitle, expectedDesc, expectedConfirmBtn, () => callCount++);

        var overlay = host.Q("settings-confirm-overlay");
        Assert(overlay != null, "settings-confirm-overlay must exist in host");

        var box = host.Q("settings-confirm-box");
        Assert(box != null, "settings-confirm-box must exist");

        var titleLbl = host.Q<Label>("settings-confirm-title");
        Assert(titleLbl != null, "settings-confirm-title label must exist");
        Assert(titleLbl.text == expectedTitle, $"Title mismatch. Expected: '{expectedTitle}', Got: '{titleLbl.text}'");

        var descLbl = host.Q<Label>("settings-confirm-desc");
        Assert(descLbl != null, "settings-confirm-desc label must exist");
        Assert(descLbl.text == expectedDesc, $"Description mismatch. Expected: '{expectedDesc}', Got: '{descLbl.text}'");

        var cancelBtn = host.Q<Button>("settings-confirm-cancel-btn");
        Assert(cancelBtn != null, "settings-confirm-cancel-btn must exist");
        Assert(cancelBtn.text == "取消", $"Cancel button text mismatch. Got: '{cancelBtn.text}'");

        var confirmBtn = host.Q<Button>("settings-confirm-ok-btn");
        Assert(confirmBtn != null, "settings-confirm-ok-btn must exist");
        Assert(confirmBtn.text == expectedConfirmBtn, $"Confirm button text mismatch. Expected: '{expectedConfirmBtn}', Got: '{confirmBtn.text}'");

        Assert(callCount == 0, "Showing dialog must not execute callback prematurely");

        Debug.Log("[TabletResetConfirmationReview] Test 1 Passed: Dialog structure, labels, and buttons text matching verified.");
    }

    /// <summary>
    /// 用例 2: 点击取消、点击遮罩、按 Escape 均关闭弹窗，确认回调计数为 0。
    /// </summary>
    private static void Test2_DismissPathsWithZeroSideEffect(VisualElement root)
    {
        root.Clear();
        var host = new VisualElement { name = "test-host" };
        root.Add(host);

        int callCount = 0;

        // 2.1 点击取消按钮
        TabletSettingsAppUI.ShowResetConfirmDialog(host, "T1", "M1", "OK1", () => callCount++);
        var cancelBtn = host.Q<Button>("settings-confirm-cancel-btn");
        ClickButton(cancelBtn);
        Assert(host.Q("settings-confirm-overlay") == null, "Overlay should be removed on cancel");
        Assert(callCount == 0, "Cancel button must not invoke onConfirm");

        // 2.2 点击背景遮罩
        TabletSettingsAppUI.ShowResetConfirmDialog(host, "T2", "M2", "OK2", () => callCount++);
        var overlay = host.Q("settings-confirm-overlay");
        Assert(overlay != null, "Overlay must exist before mask click");
        ClickOverlay(overlay);
        Assert(host.Q("settings-confirm-overlay") == null, "Overlay should be removed on mask click");
        Assert(callCount == 0, "Mask click must not invoke onConfirm");

        // 2.3 按 Escape 键
        TabletSettingsAppUI.ShowResetConfirmDialog(host, "T3", "M3", "OK3", () => callCount++);
        overlay = host.Q("settings-confirm-overlay");
        Assert(overlay != null, "Overlay must exist before escape key");
        PressEscape(overlay);
        Assert(host.Q("settings-confirm-overlay") == null, "Overlay should be removed on Escape key");
        Assert(callCount == 0, "Escape key must not invoke onConfirm");

        Debug.Log("[TabletResetConfirmationReview] Test 2 Passed: Dismiss paths (Cancel button, mask click, Escape key) have zero side effect.");
    }

    /// <summary>
    /// 用例 3: 点击确认只调用替身操作一次并关闭弹窗；重复打开/关闭不会残留遮罩或重复触发。
    /// </summary>
    private static void Test3_ConfirmInvocationAndOverlayDeduplication(VisualElement root)
    {
        root.Clear();
        var host = new VisualElement { name = "test-host" };
        root.Add(host);

        int callCount = 0;

        // 3.1 确认按钮调用一次回调并关闭弹窗
        TabletSettingsAppUI.ShowResetConfirmDialog(host, "T_Confirm", "M_Confirm", "OK_Confirm", () => callCount++);
        var okBtn = host.Q<Button>("settings-confirm-ok-btn");
        ClickButton(okBtn);
        Assert(callCount == 1, $"Confirm callback must be called exactly once. Got: {callCount}");
        Assert(host.Q("settings-confirm-overlay") == null, "Overlay should be removed after confirm");

        // 3.2 重复触发打开弹窗：旧遮罩应自动去重，不应产生多层堆叠残留
        int callCountA = 0;
        int callCountB = 0;
        TabletSettingsAppUI.ShowResetConfirmDialog(host, "Title_A", "Message_A", "Confirm_A", () => callCountA++);
        var overlayA = host.Q("settings-confirm-overlay");
        Assert(overlayA != null, "Overlay A must exist");

        // 再次打开新弹窗
        TabletSettingsAppUI.ShowResetConfirmDialog(host, "Title_B", "Message_B", "Confirm_B", () => callCountB++);
        var overlays = host.Query("settings-confirm-overlay").ToList();
        Assert(overlays.Count == 1, $"Only 1 overlay should remain after deduplication. Found: {overlays.Count}");

        var titleLbl = host.Q<Label>("settings-confirm-title");
        Assert(titleLbl.text == "Title_B", "Deduplicated overlay should show newest dialog content");

        // 取消新弹窗
        var cancelBtn = host.Q<Button>("settings-confirm-cancel-btn");
        ClickButton(cancelBtn);
        Assert(host.Q("settings-confirm-overlay") == null, "Overlay must be dismissed after cancel");
        Assert(callCountA == 0, "Replaced dialog callback A must never be invoked");
        Assert(callCountB == 0, "Cancelled dialog callback B must not be invoked");

        Debug.Log("[TabletResetConfirmationReview] Test 3 Passed: Confirm invocation exactly once, deduplication and cleanup verified.");
    }

    /// <summary>
    /// 用例 4: 静态/实际配置行挂载验证。
    /// 验证开发者选项面板下真实生成的卡片和按钮：
    /// 1. 经济重置行文案必须无固定 50,000、无 信任 62、无 车况 70，且清晰阐述难度与种子初值；
    /// 2. 真实点击行能打开二次确认弹窗；
    /// 3. 安全防护：测试过程中仅点击取消按钮关闭弹窗，坚决不触发真实 PlayerPrefs 删除或 GameData.ResetState()。
    /// </summary>
    private static void Test4_ProductionDeveloperOptionsWiring(VisualElement root)
    {
        root.Clear();
        // 备份原有状态
        var unlockedField = typeof(TabletSettingsAppUI).GetField("isTabletDevModeUnlocked", BindingFlags.Static | BindingFlags.NonPublic);
        var categoryField = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic);

        bool origUnlocked = (bool)unlockedField.GetValue(null);
        int origCategory = (int)categoryField.GetValue(null);

        try
        {
            unlockedField.SetValue(null, true);
            categoryField.SetValue(null, 99); // 99: 开发者选项

            var win = new VisualElement { name = "test-settings-win" };
            root.Add(win);

            TabletSettingsAppUI.BuildSettingsPage(win);

            var rows = win.Query<VisualElement>().ToList();
            VisualElement storyRow = null;
            VisualElement ecoRow = null;
            VisualElement unlockRow = null;
            VisualElement addMoneyRow = null;

            foreach (var r in rows)
            {
                var labels = r.Query<Label>().ToList();
                if (labels.Count >= 2)
                {
                    string t = labels[0].text;
                    if (t == "重置主线剧情进度") storyRow = r;
                    else if (t == "重置经济模拟数据") ecoRow = r;
                    else if (t == "一键解锁全部剧情与CG (调试用)") unlockRow = r;
                    else if (t == "注入调试测试资金") addMoneyRow = r;
                }
            }

            Assert(storyRow != null, "Missing row: 重置主线剧情进度");
            Assert(ecoRow != null, "Missing row: 重置经济模拟数据");
            Assert(unlockRow != null, "Missing row: 一键解锁全部剧情与CG (调试用)");
            Assert(addMoneyRow != null, "Missing row: 注入调试测试资金");

            // 验证经济重置行文案：必须无固定 50,000、无 信任 62、无 车况 70，且清晰阐述难度与种子初值
            var ecoLabels = ecoRow.Query<Label>().ToList();
            string ecoDesc = ecoLabels[1].text;
            Assert(!ecoDesc.Contains("50,000"), "Eco reset row description must NOT promise fixed 50,000: " + ecoDesc);
            Assert(!ecoDesc.Contains("信任 62"), "Eco reset row description must NOT promise fixed 信任 62: " + ecoDesc);
            Assert(!ecoDesc.Contains("车况 70"), "Eco reset row description must NOT promise fixed 车况 70: " + ecoDesc);
            Assert(ecoDesc.Contains("重置当前运行中的经营状态"), "Eco reset row must explain resetting current running simulation state: " + ecoDesc);
            Assert(ecoDesc.Contains("难度") || ecoDesc.Contains("种子"), "Eco reset row must mention difficulty or seed dynamics: " + ecoDesc);

            // 验证点击「重置主线剧情进度」行打开二次确认弹窗
            ClickRow(storyRow);
            var storyOverlay = win.Q("settings-confirm-overlay");
            Assert(storyOverlay != null, "Clicking story reset row must open confirmation dialog");
            var storyTitle = win.Q<Label>("settings-confirm-title")?.text;
            Assert(storyTitle == "⚠️ 重置主线剧情进度确认", "Story confirmation dialog title mismatch: " + storyTitle);
            var storyBtn = win.Q<Button>("settings-confirm-ok-btn")?.text;
            Assert(storyBtn == "确认重置主线", "Story confirmation button text mismatch: " + storyBtn);
            // 关键安全原则：立即点击取消，不得触发真实 PlayerPrefs 删除
            var cancelBtn = win.Q<Button>("settings-confirm-cancel-btn");
            ClickButton(cancelBtn);
            Assert(win.Q("settings-confirm-overlay") == null, "Story confirmation dialog must be dismissed after cancel");

            // 验证点击「重置经济模拟数据」行打开二次确认弹窗
            ClickRow(ecoRow);
            var ecoOverlay = win.Q("settings-confirm-overlay");
            Assert(ecoOverlay != null, "Clicking eco reset row must open confirmation dialog");
            var ecoTitle = win.Q<Label>("settings-confirm-title")?.text;
            Assert(ecoTitle == "⚠️ 重置经济模拟数据确认", "Eco confirmation dialog title mismatch: " + ecoTitle);
            var ecoConfirmBtn = win.Q<Button>("settings-confirm-ok-btn")?.text;
            Assert(ecoConfirmBtn == "确认重置经济", "Eco confirmation button text mismatch: " + ecoConfirmBtn);
            // 关键安全原则：立即点击取消，不得触发真实 GameData.ResetState()
            cancelBtn = win.Q<Button>("settings-confirm-cancel-btn");
            ClickButton(cancelBtn);
            Assert(win.Q("settings-confirm-overlay") == null, "Eco confirmation dialog must be dismissed after cancel");

            Debug.Log("[TabletResetConfirmationReview] Test 4 Passed: Production developer options rows verified (no fixed 50,000, confirmation dialogs safely wired with zero unintended side effect).");
        }
        finally
        {
            unlockedField.SetValue(null, origUnlocked);
            categoryField.SetValue(null, origCategory);
        }
    }
}
