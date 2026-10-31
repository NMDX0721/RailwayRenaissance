using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.SystemShell;
using RailwayRenaissance.UI.Tablet.Apps;

// Isolated real-tablet entry and UI event review; never saves a game scene.
[InitializeOnLoad]
public static class TabletSystemShellReview
{
    const string Pending = "TabletShellReview.Pending";
    const string Output = "tools/compile/tablet_presentation_review_v1";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static double deadline;
    static double nextCheck;
    static int stage, frame;
    static UIDocument document;
    static VisualElement root;
    static RenderTexture target;
    static TabletShellState state;
    static GameMainUI ui;
    static string presentationPath;
    static string notificationPath;

    static TabletSystemShellReview()
    {
        if (SessionState.GetBool(Pending, false)) StartChecking();
    }

    public static void Batch()
    {
        ValidateState();
        presentationPath = TabletPresentationStorageReview.Validate();
        notificationPath = ValidateNotificationStorage();
        SessionState.SetString("TabletPresentationReview.Path", presentationPath);
        SessionState.SetString("TabletNotificationReview.Path", notificationPath);
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Pending, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        StartChecking();
        EditorApplication.isPlaying = true;
    }

    static void StartChecking()
    {
        deadline = EditorApplication.timeSinceStartup + 180;
        EditorApplication.update -= Check;
        EditorApplication.update += Check;
    }

    static void Assert(bool ok, string reason)
    {
        if (!ok) throw new InvalidOperationException(reason);
    }

    static object Field(string name) => typeof(GameMainUI).GetField(name, Hidden).GetValue(ui);
    static VisualElement AppLayer => (VisualElement)Field("appLayer");
    static object Call(string name, params object[] args) => typeof(GameMainUI).GetMethod(name, Hidden).Invoke(ui, args);

    static void Submit(string name)
    {
        var button = root.Q<Button>(name);
        Assert(button != null && button.enabledInHierarchy, "Missing enabled button: " + name);
        using var evt = NavigationSubmitEvent.GetPooled();
        evt.target = button; button.SendEvent(evt);
    }

    static void Escape()
    {
        using var evt = KeyDownEvent.GetPooled(' ', KeyCode.Escape, EventModifiers.None);
        evt.target = root; root.SendEvent(evt);
    }

    static void ValidateState()
    {
        var sample = new TabletShellState(); int changed = 0;
        sample.Changed += () => changed++;
        Assert(sample.Notifications.Count == 0 && sample.Post(null, " ") == -1, "Blank/demo notifications");
        Assert(sample.UnreadCount == 0, "Initial unread count not zero");
        long first = sample.Post("first", "one");
        Assert(sample.UnreadCount == 1, "Unread count not incremented on post");
        for (int i = 0; i < 40; i++) sample.Post("item " + i, "body");
        Assert(sample.Notifications.Count == 32 && sample.Notifications[0].Title == "item 39", "Capacity/order");
        Assert(sample.UnreadCount == 32, "Capacity eviction unread count mismatch");
        Assert(!sample.Dismiss(first), "Evicted notification retained");
        Assert(sample.Dismiss(sample.Notifications[0].Id), "Dismiss failed");
        Assert(sample.UnreadCount == 31, "Dismiss did not decrement unread count");
        sample.TogglePanel(TabletSystemPanel.Notifications);
        Assert(sample.UnreadCount == 0, "Opening panel did not mark all as read");
        sample.TogglePanel(TabletSystemPanel.Controls);
        Assert(sample.Panel == TabletSystemPanel.Controls && sample.ClosePanel() && !sample.ClosePanel(), "Panel mutual exclusion/close consumption");
        sample.SetQuietMode(true); int before = changed; sample.SetQuietMode(true);
        Assert(changed == before, "Unchanged setting emitted event");
        sample.ClearNotifications(); Assert(sample.Notifications.Count == 0 && sample.UnreadCount == 0, "Clear failed");
        Debug.Log("TABLET_SHELL_STATE_OK: bounded real notifications, newest-first, unread tracking, dismissal, mutual exclusion, idempotent settings");
    }

    static string ValidateNotificationStorage()
    {
        string directory = Path.GetFullPath("Library/TabletNotificationReview/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "notifications.json");

        var st = new TabletShellState(path);
        Assert(!File.Exists(path), "Read created unsolicited notifications file");
        Assert(st.UnreadCount == 0 && !st.QuietMode, "Default values mismatch");

        st.SetQuietMode(true);
        st.Post("notif_title_1", "notif_body_1");
        st.Post("notif_title_2", "notif_body_2");
        Assert(File.Exists(path), "Saving state failed to create notifications file");

        var restored = new TabletShellState(path);
        Assert(restored.QuietMode, "QuietMode did not round-trip");
        Assert(restored.Notifications.Count == 2, "Notification count did not round-trip");
        Assert(restored.Notifications[0].Title == "notif_title_2", "Notification order newest-first failed");
        Assert(restored.Notifications[1].Title == "notif_title_1", "Notification order newest-first failed");
        Assert(restored.UnreadCount == 2, "Unread count did not round-trip");

        restored.TogglePanel(TabletSystemPanel.Notifications);
        Assert(restored.UnreadCount == 0, "Mark all as read failed on restored instance");

        var recheck = new TabletShellState(path);
        Assert(recheck.UnreadCount == 0, "Read state did not persist");

        string savedJson = File.ReadAllText(path);

        // Damaged file test
        string damaged = Path.Combine(directory, "damaged.json");
        File.WriteAllText(damaged, "{ invalid json");
        var invalid = new TabletShellState(damaged);
        Assert(invalid.Storage != null && !string.IsNullOrEmpty(invalid.Storage.LastError), "Damaged file error not reported");
        Assert(File.ReadAllText(damaged) == "{ invalid json", "Damaged file was overwritten");
        Assert(!invalid.QuietMode && invalid.Notifications.Count == 0, "Damaged file fallback failed");

        // Future schema test
        string future = Path.Combine(directory, "future.json");
        File.WriteAllText(future, "{\"schemaVersion\":999,\"quietMode\":true,\"notifications\":[]}");
        var readOnly = new TabletShellState(future);
        Assert(readOnly.Storage.IsReadOnly, "Future schema not marked read-only");
        readOnly.SetQuietMode(false);
        readOnly.Post("future_test", "body");
        Assert(File.ReadAllText(future).Contains("\"schemaVersion\":999"), "Future schema file overwritten");

        // File locked write test
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            recheck.Post("locked_post", "locked_body");
            Assert(!string.IsNullOrEmpty(recheck.Storage.LastError), "Locked file write error not reported");
        }
        Assert(recheck.Notifications[0].Title == "locked_post", "In-memory state lost on write failure");

        Debug.Log("TABLET_NOTIFICATION_STORAGE_OK: independent JSON, round-trip, unread preservation, quiet mode persistence, damaged file protection, future schema read-only, locked file safety");
        return Path.Combine(directory, "runtime.json");
    }

    static void Resize(int width, int height)
    {
        if (target != null) { document.panelSettings.targetTexture = null; target.Release(); UnityEngine.Object.Destroy(target); }
        target = new RenderTexture(width, height, 24); target.Create();
        document.panelSettings.targetTexture = target;
    }

    static void Capture(string name)
    {
        var previous = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
        UnityEngine.Object.Destroy(image);
    }

    static void Check()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Tablet shell review timed out");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (stage > 0 && Time.frameCount - frame < 30) return;
            if (EditorApplication.timeSinceStartup < nextCheck) return;

            switch (stage)
            {
                case 0:
                    typeof(GameMainUI).GetField("PresentationReviewStorage", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SessionState.GetString("TabletPresentationReview.Path", ""));
                    typeof(GameMainUI).GetField("NotificationReviewStorage", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SessionState.GetString("TabletNotificationReview.Path", ""));
                    notificationPath = SessionState.GetString("TabletNotificationReview.Path", "");
                    GameMainUI.Show(); ui = GameMainUI.Instance;
                    Assert(ui != null, "Actual tablet entry failed");
                    Call("EnsureDesktopBuiltSync");
                    document = (UIDocument)Field("uiDoc"); root = document.rootVisualElement;
                    var settings = UnityEngine.Object.Instantiate(document.panelSettings);
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize; settings.scale = 1;
                    settings.clearColor = true; settings.colorClearValue = new Color(.06f, .07f, .06f);
                    document.panelSettings = settings; Resize(1440, 900);
                    state = (TabletShellState)Field("tabletShellState");
                    Assert(root.Q("tablet-shell-root").style.display == DisplayStyle.None && state.Notifications.Count == 0, "Lockscreen leaked system shell");
                    var initBadge = root.Q<Label>("tablet-notifications-badge");
                    Assert(initBadge == null || initBadge.style.display == DisplayStyle.None, "Initial unread badge visible");
                    Call("Unlock"); break;

                case 1:
                    // 1. 桌面展示与应用启动、文本聚焦、最近任务后台输入隔离
                    Assert(root.Q("tablet-shell-root").resolvedStyle.display == DisplayStyle.Flex, "Unlock did not show shell");
                    Capture("01-desktop");
                    Call("LaunchApp", "时钟");
                    var input = new TextField { name = "shell-review-input" };
                    input.value = "init_clock_input";
                    root.Q("app-content").Add(input);
                    input.Focus();
                    Assert(root.panel?.focusController.focusedElement == input, "Clock input field focused");

                    // 点击闹钟行触发系统通知与角标
                    var alarmLabel = root.Query<Label>().Where(l => l.text != null && l.text.Contains("晨间出车早擎闹钟")).First();
                    Assert(alarmLabel != null, "Clock alarm row missing in Clock app");
                    var alarmRow = alarmLabel.parent?.parent;
                    Assert(alarmRow != null, "Clock alarm row element missing");
                    using (var clickEvt = ClickEvent.GetPooled()) { clickEvt.target = alarmRow; alarmRow.SendEvent(clickEvt); }
                    Assert(state.Notifications.Count == 1 && state.UnreadCount == 1, "Clock alarm generated notification");
                    var badge = root.Q<Label>("tablet-notifications-badge");
                    Assert(badge != null && badge.resolvedStyle.display == DisplayStyle.Flex && badge.text == "1", "Badge showing 1 unread notification");

                    // 打开最近任务，验证后台按键隔离
                    Call("OpenRecentTasks");
                    var recentsOverlay = root.Q("tablet-recent-tasks-overlay");
                    Assert(recentsOverlay != null && recentsOverlay.style.display == DisplayStyle.Flex, "Recent tasks overlay opened");

                    using (var typing = KeyDownEvent.GetPooled('z', KeyCode.Z, EventModifiers.None))
                    {
                        typing.target = input; input.SendEvent(typing);
                        Assert(input.value == "init_clock_input", "Typing into background app blocked while recent tasks is open");
                    }
                    Capture("02-recent-tasks");
                    break;

                case 2:
                    // 2. 最近任务关闭层级（Escape / 系统返回 / 点击外部）与焦点恢复、安全退回
                    var recents = root.Q("tablet-recent-tasks-overlay");
                    var appInput = root.Q<TextField>("shell-review-input");

                    // A. Escape 关闭与焦点恢复
                    Escape();
                    Assert(recents.style.display == DisplayStyle.None, "Escape closed recent tasks overlay");
                    Assert(root.panel?.focusController.focusedElement == appInput, "Focus restored to input field after recent tasks closed via Escape");

                    // B. 系统返回 (RequestSystemBack) 关闭
                    Call("OpenRecentTasks");
                    Assert(recents.style.display == DisplayStyle.Flex, "Recent tasks reopened for back test");
                    Call("RequestSystemBack");
                    Assert(recents.style.display == DisplayStyle.None, "RequestSystemBack closed recent tasks overlay");
                    Assert(root.panel?.focusController.focusedElement == appInput, "Focus restored to input field after system back");

                    // C. 点击外部 (Scrim) 关闭
                    Call("OpenRecentTasks");
                    Assert(recents.style.display == DisplayStyle.Flex, "Recent tasks reopened for scrim click test");
                    using (var clickEvt = ClickEvent.GetPooled()) { clickEvt.target = recents; recents.SendEvent(clickEvt); }
                    Assert(recents.style.display == DisplayStyle.None, "Clicking recent tasks scrim closed overlay");
                    Assert(root.panel?.focusController.focusedElement == appInput, "Focus restored to input field after scrim click");

                    // D. 焦点销毁后的安全退回
                    var tempInput = new TextField { name = "shell-temp-input" };
                    root.Q("app-content").Add(tempInput);
                    tempInput.Focus();
                    Assert(root.panel?.focusController.focusedElement == tempInput, "Temp input focused");
                    Call("OpenRecentTasks");
                    tempInput.RemoveFromHierarchy();
                    Call("CloseRecentTasks");
                    Assert(recents.style.display == DisplayStyle.None, "Recent tasks closed with destroyed previous focus");
                    Assert(root.panel?.focusController.focusedElement != tempInput, "Destroyed element safe fallback");

                    // 重新聚焦有效输入框并打开通知中心
                    appInput.Focus();
                    Submit("tablet-notifications-open");
                    break;

                case 3:
                    // 3. 通知中心真实模糊、32项容量、已读清除与互斥切换
                    Assert(state.Panel == TabletSystemPanel.Notifications && state.Notifications.Count == 1, "Notifications panel open");
                    Assert(state.UnreadCount == 0, "Opening notification panel marked all as read");
                    var readBadge = root.Q<Label>("tablet-notifications-badge");
                    Assert(readBadge == null || readBadge.style.display == DisplayStyle.None, "Badge hidden when notifications opened");

                    var blurBg = root.Q("tablet-shell-blur-bg");
                    Assert(blurBg != null && blurBg.style.display == DisplayStyle.Flex, "Blur background visible on notification open");
                    var shellView = (TabletShellView)Field("tabletShellView");
                    Assert(shellView != null && shellView.OverlayOpacity == 1f, "Shell overlay opacity restored after capture");
                    Assert(AppLayer.worldBound.yMin >= 47, "App occludes system controls");

                    Capture("03-notifications");

                    // 32 条容量与清空
                    long dismissId = state.Post("review_dismiss", "dismiss through rendered row");
                    Assert(root.Q<Button>("tablet-notification-dismiss-" + dismissId) != null, "Rendered dismiss action missing");
                    Submit("tablet-notification-dismiss-" + dismissId);
                    Assert(state.Notifications.Count == 1 && state.UnreadCount == 0, "Rendered dismiss did not update state");
                    for (int i = 0; i < 40; i++) state.Post("capacity_" + i, "capacity body");
                    var boundaryBadge = root.Q<Label>("tablet-notifications-badge");
                    int renderedRows = root.Query<Button>().Where(b => b.name != null && b.name.StartsWith("tablet-notification-dismiss-")).ToList().Count;
                    Assert(state.Notifications.Count == 32 && state.UnreadCount == 32, "32-item capacity state mismatch");
                    Assert(boundaryBadge.text == "32" && renderedRows == 32, "32-item capacity UI did not match state");
                    Submit("tablet-notifications-clear");
                    Assert(state.Notifications.Count == 0 && root.Query<Label>().Where(l => l.text == "暂无通知").First() != null, "Clear did not render empty state");

                    // 互斥切换至控制中心
                    Submit("tablet-controls-open");
                    break;

                case 4:
                    // 4. 控制中心展示、免打扰切换、按键隔离与关闭
                    Assert(state.Panel == TabletSystemPanel.Controls, "Panel mutual exclusion switched to Controls");
                    Capture("04-controls");

                    Submit("tablet-quiet-toggle"); Call("ShowBackupToast", "勿扰期间的应用提示");
                    Assert(state.QuietMode && state.Notifications.Count == 1 && ((Label)Field("backupToast")).style.display == DisplayStyle.None, "Quiet notification storage/presentation");
                    Submit("tablet-quiet-toggle"); Call("ShowBackupToast", "关闭勿扰后的应用提示");
                    Assert(!state.QuietMode && state.Notifications.Count == 2 && ((Label)Field("backupToast")).style.display == DisplayStyle.Flex, "Toast resumed after quiet mode ended");
                    ((Label)Field("backupToast")).style.display = DisplayStyle.None;
                    Submit("tablet-quiet-toggle");

                    using (var typing = KeyDownEvent.GetPooled('x', KeyCode.X, EventModifiers.None))
                    {
                        var backgroundInput = root.Q<TextField>("shell-review-input");
                        typing.target = backgroundInput; backgroundInput.SendEvent(typing);
                        Assert(backgroundInput.value == "init_clock_input", "Modal allowed typing into background app");
                    }

                    Escape();
                    break;

                case 5:
                    // 5. 控制中心关闭确认、设置跳转、点击外部遮罩关闭与系统面板焦点销毁退回
                    Assert(state.Panel == TabletSystemPanel.None && AppLayer.style.display == DisplayStyle.Flex, "Controls closed via Escape");
                    var closedBlurBg = root.Q("tablet-shell-blur-bg");
                    Assert(closedBlurBg == null || closedBlurBg.style.display == DisplayStyle.None, "Blur background hidden on close");

                    // 控制中心点击设置入口跳转设置
                    Submit("tablet-controls-open");
                    Submit("tablet-shell-settings");
                    Assert(state.Panel == TabletSystemPanel.None && AppLayer.style.display == DisplayStyle.Flex, "Settings transition from controls succeeded");

                    // 打开通知中心并通过点击外部遮罩关闭
                    Submit("tablet-notifications-open");
                    var scrim = root.Q("tablet-shell-scrim");
                    using (var evt = ClickEvent.GetPooled()) { evt.target = scrim; scrim.SendEvent(evt); }
                    Assert(state.Panel == TabletSystemPanel.None, "Outside scrim click closed notifications panel");

                    // 系统面板焦点销毁安全退回
                    var tempInput2 = new TextField { name = "shell-temp-input-2" };
                    AppLayer.Add(tempInput2);
                    tempInput2.Focus();
                    Assert(root.panel?.focusController.focusedElement == tempInput2, "Temp input 2 focused");
                    Submit("tablet-notifications-open");
                    tempInput2.RemoveFromHierarchy();
                    Escape();
                    Assert(state.Panel == TabletSystemPanel.None, "Panel closed after focus destroyed");
                    Assert(root.panel?.focusController.focusedElement != tempInput2, "Destroyed element safe fallback in system panel");
                    break;

                case 6:
                    // 6. 设置子页面返回、同名页替换和全部关闭后的可见层级
                    TabletSettingsAppUI.ShowTipsPage("rail_ballast");
                    var appLayer = AppLayer;
                    Assert(appLayer != null && appLayer.style.display == DisplayStyle.Flex, "App layer visible for settings subpage");
                    var tipsHeader = root.Query<Label>().Where(l => l.text != null && l.text.Contains("术语图鉴")).First();
                    Assert(tipsHeader != null, "Tips subpage opened");
                    Capture("05-settings-subpage");

                    // 同名页替换验证：再次请求同名子页，页面栈不重复入栈
                    TabletSettingsAppUI.ShowTipsPage("rail_expansion_joint");
                    var settingsTask = (TabletTaskSession)Call("FindTabletTask", "设置");
                    var pages = (System.Collections.IList)typeof(TabletTaskSession).GetField("Pages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(settingsTask);
                    Assert(settingsTask != null && pages.Count == 1, "Same-name subpage replaced top page without stacking duplicate layer");

                    // 子页返回验证：返回后父页面可见
                    Call("RequestSystemBack");
                    var remainingPages = (System.Collections.IList)typeof(TabletTaskSession).GetField("Pages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(settingsTask);
                    Assert(remainingPages.Count == 0, "Subpage returned via RequestSystemBack");
                    Capture("06-settings-parent-restored");

                    // 全部关闭 (CloseAllTabletTasks)：应用层隐藏，桌面完全显示
                    Call("CloseAllTabletTasks");
                    Assert(appLayer.style.display == DisplayStyle.None, "App layer hidden after CloseAllTabletTasks");
                    var desktop = root.Q("tablet-desktop");
                    Assert(desktop != null && desktop.style.display == DisplayStyle.Flex, "Desktop visible after CloseAllTabletTasks");
                    Capture("07-desktop-all-closed");
                    break;

                case 7:
                    // 7. 至少两种窗口比例下（1024x768 4:3 vs 1440x900 16:10）改变分辨率、采样前隐藏与恢复
                    Submit("tablet-controls-open");
                    Resize(1024, 768);
                    Call("TriggerShellBlurCapture");
                    var view1024 = (TabletShellView)Field("tabletShellView");
                    Assert(view1024 != null && view1024.OverlayOpacity == 1f, "Overlay opacity is 1f after resize capture");
                    Assert(root.Q("tablet-shell-panel").worldBound.xMin >= -.1f && root.Q("tablet-shell-panel").worldBound.xMax <= 1024.1f, "Panel clips at 1024 resolution");
                    Assert(!root.Q("tablet-shell-launcher").worldBound.Overlaps(root.Q("tablet-status-right").worldBound), "Small layout overlaps status");
                    Capture("08-controls-1024x768");

                    // 1024 分辨率下的通知切换互斥
                    Submit("tablet-notifications-open");
                    Assert(state.Panel == TabletSystemPanel.Notifications, "Switched to notifications at 1024 resolution");
                    Capture("09-notifications-1024x768");

                    Escape();
                    break;

                case 8:
                    // 8. 个性化设置回归 (高对比度、壁纸压暗、减少动效、高对比度禁用模糊)
                    typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, 9);
                    Call("LaunchApp", "设置");
                    Assert(root.Q("presentation-settings-root") != null, "Personalization category is not connected");
                    Capture("10-personalization");

                    root.Q<Slider>("slider-wallpaper-dim").value = .3f;
                    root.Q<Toggle>("toggle-reduce-motion").value = true;
                    root.Q<Toggle>("toggle-high-contrast").value = true;
                    Submit("btn-青绿");
                    Assert(ui.PresentationPreferences.Accent == TabletAccent.Teal && ui.PresentationPreferences.HighContrast && ui.PresentationPreferences.ReduceMotion, "Real preference controls disconnected");
                    var paper = root.Q("tablet-desktop-wallpaper").resolvedStyle.backgroundColor;
                    Assert(paper.r < .8f, "Wallpaper dim is not consumed");

                    // 高对比度下打开控制中心，验证模糊禁用且遮罩为实色
                    Submit("tablet-controls-open");
                    var hcBlurBg = root.Q("tablet-shell-blur-bg");
                    Assert(hcBlurBg == null || hcBlurBg.style.display == DisplayStyle.None, "High contrast mode disabled blurred background");
                    Escape();

                    // 默认值重置测试
                    Submit("btn-reset-defaults"); Submit("btn-cancel-reset"); Assert(ui.PresentationPreferences.ReduceMotion, "Cancelled defaults changed preferences");
                    Submit("btn-reset-defaults"); Submit("btn-confirm-reset"); Assert(!ui.PresentationPreferences.ReduceMotion && !ui.PresentationPreferences.HighContrast, "Confirmed scoped defaults failed");

                    Debug.Log("TABLET_SYSTEM_SHELL_GRAPHIC_REVIEW_OK: full interactive graphics review passed, recent tasks input isolation and focus restore, mutual exclusion, settings subpage back and replacement, multi-ratio blur resize, focus destroyed fallback, and personalization defaults");
                    Finish(0);
                    return;
            }
            stage++; frame = Time.frameCount; nextCheck = EditorApplication.timeSinceStartup + .8;
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }

    static void Finish(int code)
    {
        SessionState.SetBool(Pending, false); EditorApplication.update -= Check;
        if (target != null) { document.panelSettings.targetTexture = null; target.Release(); }
        EditorApplication.Exit(code);
    }
}
