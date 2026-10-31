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
        presentationPath=TabletPresentationStorageReview.Validate();
        notificationPath=ValidateNotificationStorage();
        SessionState.SetString("TabletPresentationReview.Path",presentationPath);
        SessionState.SetString("TabletNotificationReview.Path",notificationPath);
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Pending, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        StartChecking();
        EditorApplication.isPlaying = true;
    }
    static void StartChecking()
    {
        deadline = EditorApplication.timeSinceStartup + 150;
        EditorApplication.update -= Check;
        EditorApplication.update += Check;
    }
    static void Assert(bool ok, string reason)
    {
        if (!ok) throw new InvalidOperationException(reason);
    }
    static object Field(string name) => typeof(GameMainUI).GetField(name, Hidden).GetValue(ui);
    static void Call(string name, params object[] args) => typeof(GameMainUI).GetMethod(name, Hidden).Invoke(ui, args);
    static void Submit(string name)
    {
        var button = root.Q<Button>(name);
        Assert(button != null && button.enabledInHierarchy, "Missing enabled button: " + name);
        using var evt = NavigationSubmitEvent.GetPooled();
        evt.target = button; button.SendEvent(evt);
    }
    static void Escape()
    {
        using var evt = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None);
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
            if (stage > 0 && Time.frameCount - frame < 45) return;
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            switch (stage)
            {
                case 0:
                    typeof(GameMainUI).GetField("PresentationReviewStorage",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,SessionState.GetString("TabletPresentationReview.Path",""));
                    typeof(GameMainUI).GetField("NotificationReviewStorage",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,SessionState.GetString("TabletNotificationReview.Path",""));
                    notificationPath = SessionState.GetString("TabletNotificationReview.Path","");
                    MusicLibrarySession.UseReviewStorage(Path.GetFullPath("Library/TabletShellReview/music.json"));
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
                    Assert(root.Q("tablet-shell-root").resolvedStyle.display == DisplayStyle.Flex, "Unlock did not show shell");
                    Capture("01-desktop"); Call("LaunchApp", "时钟");
                    var input = new TextField { name = "shell-review-input" };
                    root.Q("app-content").Add(input); input.Focus();

                    // Click clock alarm row:
                    var alarmLabel = root.Query<Label>().Where(l => l.text != null && l.text.Contains("晨间出车早擎闹钟")).First();
                    Assert(alarmLabel != null, "Clock alarm row missing in Clock app");
                    var alarmRow = alarmLabel.parent?.parent;
                    Assert(alarmRow != null, "Clock alarm row element missing");
                    using (var clickEvt = ClickEvent.GetPooled()) { clickEvt.target = alarmRow; alarmRow.SendEvent(clickEvt); }

                    Assert(state.Notifications.Count == 1 && state.Notifications[0].Body == "闹钟已同步到先民排班系统", "System tools ShowToast did not route to shell notifications");
                    Assert(state.UnreadCount == 1, "Unread count not incremented on tool toast");

                    var badge = root.Q<Label>("tablet-notifications-badge");
                    Assert(badge != null && badge.resolvedStyle.display == DisplayStyle.Flex && badge.text == "1", "Badge not showing 1 unread notification");
                    Assert(!string.IsNullOrEmpty(badge.tooltip) && badge.tooltip.Contains("1条未读"), "Badge accessible tooltip missing");

                    Submit("tablet-notifications-open"); break;
                case 2:
                    Assert(state.Panel == TabletSystemPanel.Notifications && state.Notifications.Count == 1, "Real app feedback missing");
                    Assert(state.UnreadCount == 0, "Opening notification panel did not mark all as read");
                    var readBadge = root.Q<Label>("tablet-notifications-badge");
                    Assert(readBadge == null || readBadge.style.display == DisplayStyle.None, "Badge still visible when notifications opened");

                    var blurBg = root.Q("tablet-shell-blur-bg");
                    Assert(blurBg != null && blurBg.style.display == DisplayStyle.Flex, "Blur background missing on panel open");
                    Assert(root.Q("tablet-app-layer").worldBound.yMin >= 47, "App occludes system controls");
                    var launcher = root.Q("tablet-shell-launcher");
                    Assert(!launcher.worldBound.Overlaps(root.Q("tablet-app-close").worldBound), "Launcher blocks app close");
                    Assert(!launcher.worldBound.Overlaps(root.Q("tablet-status-right").worldBound), "Launcher blocks battery/network label");
                    var controls = root.Q<Button>("tablet-controls-open");
                    var picked = root.panel.Pick(controls.worldBound.center);
                    Assert(picked == controls || controls.Contains(picked), "Scrim blocks panel switching");
                    Capture("02-notifications");

                    // Exercise the rendered row dismissal and the visible 32-item boundary.
                    long dismissId = state.Post("review_dismiss", "dismiss through rendered row");
                    Assert(root.Q<Button>("tablet-notification-dismiss-" + dismissId) != null, "Rendered dismiss action missing");
                    Submit("tablet-notification-dismiss-" + dismissId);
                    Assert(state.Notifications.Count == 1 && state.UnreadCount == 0, "Rendered dismiss did not update state");
                    Assert(root.Q<Label>("tablet-notifications-badge").style.display == DisplayStyle.None, "Rendered dismiss left stale unread badge");
                    for (int i = 0; i < 40; i++) state.Post("capacity_" + i, "capacity body");
                    var boundaryBadge = root.Q<Label>("tablet-notifications-badge");
                    int renderedRows = root.Query<Button>().Where(b => b.name != null && b.name.StartsWith("tablet-notification-dismiss-")).ToList().Count;
                    Assert(state.Notifications.Count == 32 && state.UnreadCount == 32, "32-item capacity state mismatch");
                    Assert(boundaryBadge.text == "32" && renderedRows == 32, "32-item capacity UI did not match state");
                    Submit("tablet-notifications-clear");
                    Assert(state.Notifications.Count == 0 && root.Query<Label>().Where(l => l.text == "暂无通知").First() != null, "Clear did not render empty state");

                    Submit("tablet-controls-open");
                    Assert(state.Panel == TabletSystemPanel.Controls, "Panel switch failed");
                    Submit("tablet-quiet-toggle"); Call("ShowBackupToast", "勿扰期间的应用提示");
                    Assert(state.QuietMode && state.Notifications.Count == 1 && ((Label)Field("backupToast")).style.display == DisplayStyle.None, "Quiet notification storage/presentation");
                    Submit("tablet-quiet-toggle"); Call("ShowBackupToast", "关闭勿扰后的应用提示");
                    Assert(!state.QuietMode && state.Notifications.Count == 2 && ((Label)Field("backupToast")).style.display == DisplayStyle.Flex, "Toast did not resume after quiet mode ended");
                    ((Label)Field("backupToast")).style.display = DisplayStyle.None;
                    Submit("tablet-quiet-toggle");
                    using (var typing = KeyDownEvent.GetPooled('x', KeyCode.X, EventModifiers.None))
                    {
                        var backgroundInput = root.Q<TextField>("shell-review-input");
                        typing.target = backgroundInput; backgroundInput.SendEvent(typing);
                        Assert(string.IsNullOrEmpty(backgroundInput.value), "Modal allowed typing into background app");
                    }
                    break;
                case 3:
                    Capture("03-controls"); Escape();
                    Assert(state.Panel == TabletSystemPanel.None && root.Q("tablet-app-layer").style.display == DisplayStyle.Flex, "Escape fell through to app");
                    var closedBlurBg = root.Q("tablet-shell-blur-bg");
                    Assert(closedBlurBg == null || closedBlurBg.style.display == DisplayStyle.None, "Blur background retained after panel close");
                    Escape(); Assert(root.Q("tablet-app-layer").style.display == DisplayStyle.None, "Second Escape failed to close app");
                    Submit("tablet-controls-open"); Submit("tablet-shell-settings");
                    Assert(state.Panel == TabletSystemPanel.None && root.Q("tablet-app-layer").style.display == DisplayStyle.Flex, "Settings transition failed");
                    Submit("tablet-notifications-open");
                    var scrim = root.Q("tablet-shell-scrim");
                    using (var evt = ClickEvent.GetPooled()) { evt.target = scrim; scrim.SendEvent(evt); }
                    Assert(state.Panel == TabletSystemPanel.None, "Outside click failed");
                    Submit("tablet-notifications-open"); Submit("tablet-notifications-clear");
                    Assert(state.Notifications.Count == 0 && state.UnreadCount == 0, "UI clear failed");

                    // Verify runtime storage file persisted quietMode on disk:
                    Assert(File.Exists(notificationPath), "Runtime notification storage missing on disk");
                    string savedJson = File.ReadAllText(notificationPath);
                    Assert(savedJson.Contains("\"quietMode\": true"), "Quiet mode not saved to runtime storage file: " + savedJson);

                    // Verify orphan callback safety when handler is released:
                    TabletSystemToolsAppUI.SetToastHandler(null);
                    var showToastMethod = typeof(TabletSystemToolsAppUI).GetMethod("ShowToast", BindingFlags.Static | BindingFlags.NonPublic);
                    showToastMethod.Invoke(null, new object[] { "orphan safe test" });
                    TabletSystemToolsAppUI.SetToastHandler(msg => Call("ShowBackupToast", msg));

                    Call("ShowLockScreen");
                    Assert(state.Panel == TabletSystemPanel.None && root.Q("tablet-shell-root").style.display == DisplayStyle.None, "Lock retains overlay");
                    Call("Unlock"); Resize(1024, 768); break;
                case 4:
                    Submit("tablet-controls-open"); break;
                case 5:
                    Assert(root.Q("tablet-shell-panel").worldBound.xMin >= -.1f && root.Q("tablet-shell-panel").worldBound.xMax <= 1024.1f, "Panel clips at smaller resolution");
                    Assert(!root.Q("tablet-shell-launcher").worldBound.Overlaps(root.Q("tablet-status-right").worldBound), "Small layout overlaps status");
                    Capture("04-controls-1024");
                    Assert(!TabletMusicAppUI.tabletIsPlaying, "Shell started audio");
                    Debug.Log("TABLET_SHELL_RUNTIME_OK: real tablet entry, real background blur, app inset, panel switching/picking, app feedback, quiet storage, Escape hierarchy, scrim, clear, settings, lock/reunlock, 1440/1024 layout; synthetic events, not device touch QA");
                    typeof(TabletSettingsAppUI).GetField("currentSettingsCategory",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,9);
                    Call("LaunchApp","设置");break;
                case 6:
                    Assert(root.Q("presentation-settings-root")!=null,"Personalization category is not connected");
                    root.Q<Slider>("slider-wallpaper-dim").value=.3f;
                    root.Q<Toggle>("toggle-reduce-motion").value=true;
                    root.Q<Toggle>("toggle-high-contrast").value=true;
                    Submit("btn-青绿");
                    Assert(ui.PresentationPreferences.Accent==TabletAccent.Teal&&ui.PresentationPreferences.HighContrast&&ui.PresentationPreferences.ReduceMotion,"Real preference controls disconnected");
                    var paper=root.Q("tablet-desktop-wallpaper").resolvedStyle.backgroundColor;
                    Assert(paper.r<.8f,"Wallpaper dim is not consumed");
                    Call("ShowLockScreen");Call("Unlock");
                    Assert(root.Q("tablet-lock").style.display==DisplayStyle.None,"Reduced-motion unlock waited for animation");
                    Call("LaunchApp","设置");break;
                case 7:
                    Capture("05-personalization");
                    Submit("btn-reset-defaults");Submit("btn-cancel-reset");Assert(ui.PresentationPreferences.ReduceMotion,"Cancelled defaults changed preferences");
                    Submit("btn-reset-defaults");Submit("btn-confirm-reset");Assert(!ui.PresentationPreferences.ReduceMotion&&!ui.PresentationPreferences.HighContrast,"Confirmed scoped defaults failed");
                    Debug.Log("TABLET_PRESENTATION_RUNTIME_OK: real settings category, live wallpaper dim, system accent/contrast, reduced-motion unlock and confirmed/cancelled independent defaults");
                    Finish(0);return;
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
