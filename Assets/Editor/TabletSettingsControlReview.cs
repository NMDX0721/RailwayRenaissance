using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.SystemShell;
using RailwayRenaissance.UI.Tablet.Apps;

[InitializeOnLoad]
public static class TabletSettingsControlReview
{
    private const string PendingKey = "TabletSettingsControlReview.Pending";
    private const string OutputDir = "tools/compile/tablet_settings_review_v1";
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private static double deadline;
    private static double nextCheck;
    private static int stage;
    private static int frame;
    private static UIDocument document;
    private static VisualElement root;
    private static RenderTexture target;
    private static TabletShellState state;
    private static TabletPresentationPreferences presentation;
    private static GameMainUI ui;
    private static string reviewPresentationPath;
    private static string reviewNotificationPath;

    static TabletSettingsControlReview()
    {
        if (SessionState.GetBool(PendingKey, false))
        {
            StartChecking();
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"[TabletSettingsControlReview] Assertion failed: {message}");
        }
    }

    private static object Field(string name)
    {
        FieldInfo fi = typeof(GameMainUI).GetField(name, Hidden);
        return fi?.GetValue(ui);
    }

    private static object Call(string name, params object[] args)
    {
        MethodInfo mi = typeof(GameMainUI).GetMethod(name, Hidden);
        return mi?.Invoke(ui, args);
    }

    private static void Resize(int width, int height)
    {
        if (target != null)
        {
            document.panelSettings.targetTexture = null;
            target.Release();
            UnityEngine.Object.Destroy(target);
        }
        target = new RenderTexture(width, height, 24);
        target.Create();
        document.panelSettings.targetTexture = target;
    }

    private static void Capture(string name)
    {
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;

        string filePath = Path.Combine(OutputDir, name + ".png");
        File.WriteAllBytes(filePath, image.EncodeToPNG());
        UnityEngine.Object.Destroy(image);
    }

    private static void ClickElement(VisualElement element)
    {
        Assert(element != null, "Target element for click is null");
        if (element is Button btn)
        {
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = btn;
            btn.SendEvent(submit);
        }
        else
        {
            using var clickEvt = ClickEvent.GetPooled();
            clickEvt.target = element;
            element.SendEvent(clickEvt);
        }
    }

    [MenuItem("Tools/Tablet/Run Settings & Control Review")]
    public static void Batch()
    {
        Directory.CreateDirectory(OutputDir);
        CheckStateDecisions();

        string reviewDir = Path.GetFullPath("Library/TabletSettingsReview/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(reviewDir);
        reviewPresentationPath = Path.Combine(reviewDir, "presentation.json");
        reviewNotificationPath = Path.Combine(reviewDir, "notifications.json");

        SessionState.SetString("TabletSettingsReview.PresentationPath", reviewPresentationPath);
        SessionState.SetString("TabletSettingsReview.NotificationPath", reviewNotificationPath);
        SessionState.SetBool(PendingKey, true);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        StartChecking();
        EditorApplication.isPlaying = true;
    }

    private static void StartChecking()
    {
        stage = 0;
        frame = 0;
        deadline = EditorApplication.timeSinceStartup + 180.0;
        EditorApplication.update -= Check;
        EditorApplication.update += Check;
    }

    public static void CheckStateDecisions()
    {
        string dir = Path.GetFullPath("Library/TabletSettingsStateReview/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        // 1. Notification Storage & Quiet Mode Round-trip
        string notifPath = Path.Combine(dir, "notif_test.json");
        var notifStorage = new TabletNotificationStorage(notifPath);
        var notifState = new TabletShellState(notifStorage);
        Assert(!notifState.QuietMode, "Default QuietMode must be false");
        notifState.SetQuietMode(true);
        Assert(notifState.QuietMode, "QuietMode must be true after set");

        var restoredNotifState = new TabletShellState(notifPath);
        Assert(restoredNotifState.QuietMode, "QuietMode must persist across reloads");

        // 2. Corrupt Notification File Protection
        string corruptNotif = Path.Combine(dir, "corrupt_notif.json");
        File.WriteAllText(corruptNotif, "{invalid_json");
        var corruptNotifStorage = new TabletNotificationStorage(corruptNotif);
        bool loadOk = corruptNotifStorage.Load(out bool q, out var list, out long nextId);
        Assert(!loadOk, "Corrupt file load must report failure");
        Assert(File.ReadAllText(corruptNotif) == "{invalid_json", "Corrupt file must not be overwritten or destroyed");

        // 3. Presentation Storage & Full Round-trip
        string presPath = Path.Combine(dir, "pres_test.json");
        var pres = new TabletPresentationPreferences(presPath);
        pres.SetReduceMotion(true);
        pres.SetHighContrast(true);
        pres.SetAccent(TabletAccent.Teal);
        pres.SetWallpaperDim(0.35f);

        var restoredPres = new TabletPresentationPreferences(presPath);
        Assert(restoredPres.ReduceMotion, "ReduceMotion must persist");
        Assert(restoredPres.HighContrast, "HighContrast must persist");
        Assert(restoredPres.Accent == TabletAccent.Teal, "Accent must persist");
        Assert(Mathf.Abs(restoredPres.WallpaperDim - 0.35f) < 0.001f, "WallpaperDim must persist");

        // 4. Invalid Float & Future Version Protection
        restoredPres.SetWallpaperDim(float.NaN);
        Assert(Mathf.Abs(restoredPres.WallpaperDim - 0.35f) < 0.001f, "NaN must not corrupt WallpaperDim");
        string futurePres = Path.Combine(dir, "future_pres.json");
        File.WriteAllText(futurePres, "{\"schemaVersion\":999,\"highContrast\":true}");
        var futureObj = new TabletPresentationPreferences(futurePres);
        Assert(futureObj.IsReadOnly, "Future schema version must be read-only");
        futureObj.SetHighContrast(false);
        Assert(futureObj.HighContrast, "Edits to future schema must be rejected");

        // 5. Audio Mute State Logic
        GameData.MasterVolume = 0.8f;
        Assert(GameData.MasterVolume > 0.001f, "Initial volume positive");
        GameData.MasterVolume = 0f;
        Assert(GameData.MasterVolume <= 0.001f, "Muted volume is 0");
        GameData.MasterVolume = 1f;

        Debug.Log("TABLET_SETTINGS_CONTROL_STATE_OK: persistence round-trip, cold restart verification, corrupt JSON retention, read-only protection, and mute volume logic passed");
    }

    private static void Check()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new TimeoutException("Tablet settings & control review timed out");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (stage > 0 && Time.frameCount - frame < 30) return;
            if (EditorApplication.timeSinceStartup < nextCheck) return;

            switch (stage)
            {
                case 0:
                    // Stage 0: Setup, unlock, resize to 1440x900 landscape
                    reviewPresentationPath = SessionState.GetString("TabletSettingsReview.PresentationPath", "");
                    reviewNotificationPath = SessionState.GetString("TabletSettingsReview.NotificationPath", "");
                    typeof(GameMainUI).GetField("PresentationReviewStorage", BindingFlags.Static | BindingFlags.NonPublic)
                        ?.SetValue(null, reviewPresentationPath);
                    typeof(GameMainUI).GetField("NotificationReviewStorage", BindingFlags.Static | BindingFlags.NonPublic)
                        ?.SetValue(null, reviewNotificationPath);

                    GameMainUI.Show();
                    ui = GameMainUI.Instance;
                    Assert(ui != null, "Actual tablet entry failed");
                    Call("EnsureDesktopBuiltSync");
                    document = (UIDocument)Field("uiDoc");
                    root = document.rootVisualElement;

                    var settings = UnityEngine.Object.Instantiate(document.panelSettings);
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    settings.scale = 1;
                    settings.clearColor = true;
                    settings.colorClearValue = new Color(0.06f, 0.07f, 0.06f);
                    document.panelSettings = settings;

                    Resize(1440, 900);
                    state = (TabletShellState)Field("tabletShellState");
                    presentation = ui.PresentationPreferences;
                    GameData.MasterVolume = 1f;
                    Call("Unlock");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 1:
                    // Stage 1: Open Control Center, verify initial tiles and sandbox offline status
                    ui.OpenSystemPanel(TabletSystemPanel.Controls);
                    Assert(state.Panel == TabletSystemPanel.Controls, "Control center panel failed to open");

                    var quietBtn = root.Q<Button>("tablet-quiet-toggle");
                    var hcBtn = root.Q<Button>("tablet-high-contrast-toggle");
                    var rmBtn = root.Q<Button>("tablet-reduce-motion-toggle");
                    var accentBtn = root.Q<Button>("tablet-accent-cycle");
                    var dimBtn = root.Q<Button>("tablet-wallpaper-dim-toggle");
                    var muteBtn = root.Q<Button>("tablet-audio-mute-toggle");
                    var offlineBox = root.Q("settings-hardware-offline-box");

                    Assert(quietBtn != null, "tablet-quiet-toggle tile missing");
                    Assert(hcBtn != null, "tablet-high-contrast-toggle tile missing");
                    Assert(rmBtn != null, "tablet-reduce-motion-toggle tile missing");
                    Assert(accentBtn != null, "tablet-accent-cycle tile missing");
                    Assert(dimBtn != null, "tablet-wallpaper-dim-toggle tile missing");
                    Assert(muteBtn != null, "tablet-audio-mute-toggle tile missing");
                    Assert(offlineBox != null, "settings-hardware-offline-box indicator missing");

                    Capture("01-control-center-initial");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 2:
                    // Stage 2: Activate all Control Center quick tiles and verify state mutation
                    var btnQ = root.Q<Button>("tablet-quiet-toggle");
                    ClickElement(btnQ);
                    Assert(state.QuietMode, "tablet-quiet-toggle click failed to toggle QuietMode");

                    var btnHC = root.Q<Button>("tablet-high-contrast-toggle");
                    ClickElement(btnHC);
                    Assert(presentation.HighContrast, "tablet-high-contrast-toggle click failed to toggle HighContrast");

                    var btnRM = root.Q<Button>("tablet-reduce-motion-toggle");
                    ClickElement(btnRM);
                    Assert(presentation.ReduceMotion, "tablet-reduce-motion-toggle click failed to toggle ReduceMotion");

                    var btnAcc = root.Q<Button>("tablet-accent-cycle");
                    ClickElement(btnAcc); // Amber -> Teal
                    Assert(presentation.Accent == TabletAccent.Teal, "tablet-accent-cycle click failed to switch to Teal");

                    var btnDim = root.Q<Button>("tablet-wallpaper-dim-toggle");
                    ClickElement(btnDim); // 0f -> 0.3f
                    Assert(presentation.WallpaperDim > 0.2f, "tablet-wallpaper-dim-toggle click failed to set WallpaperDim");

                    var btnMute = root.Q<Button>("tablet-audio-mute-toggle");
                    ClickElement(btnMute);
                    Assert(GameData.MasterVolume <= 0.001f, "tablet-audio-mute-toggle click failed to mute MasterVolume");

                    Capture("02-control-center-activated");
                    ui.OpenSystemPanel(TabletSystemPanel.None);
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 3:
                    // Stage 3: Launch Settings App, switch to Category 11, verify bidirectional sync with QuietMode
                    Call("LaunchApp", "设置");
                    var categoryField = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic);
                    categoryField?.SetValue(null, 11); // Category 11: 通知与状态栏
                    var appWin = root.Q("tablet-app-layer-设置") ?? root.Q("tablet-app-layer");
                    TabletSettingsAppUI.BuildSettingsPage(appWin);

                    var quietRow = root.Q("settings-quiet-toggle-row");
                    Assert(quietRow != null, "settings-quiet-toggle-row missing in Settings Category 11");
                    var capsule = quietRow.Q("toggle-capsule");
                    Assert(capsule != null, "toggle-capsule missing in quiet toggle row");

                    Assert(state.QuietMode, "QuietMode must reflect true from Control Center activation");

                    // Mutate from Settings: click capsule to toggle off
                    ClickElement(capsule);
                    Assert(!state.QuietMode, "Clicking capsule toggle in Settings must sync QuietMode to false");

                    // Click capsule again to toggle back on
                    ClickElement(capsule);
                    Assert(state.QuietMode, "Clicking capsule toggle back must sync QuietMode to true");

                    Capture("03-settings-sync-category11");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 4:
                    // Stage 4: Switch to Category 9 (Appearance), verify presentation sync & switch to Blue accent
                    var catField9 = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic);
                    catField9?.SetValue(null, 9); // Category 9: 个性化
                    var appWin9 = root.Q("tablet-app-layer-设置") ?? root.Q("tablet-app-layer");
                    TabletSettingsAppUI.BuildSettingsPage(appWin9);

                    var hcToggle = root.Q<Toggle>("toggle-high-contrast");
                    var rmToggle = root.Q<Toggle>("toggle-reduce-motion");
                    var dimSlider = root.Q<Slider>("slider-wallpaper-dim");
                    var blueBtn = root.Q<Button>("btn-蓝色");

                    Assert(hcToggle != null && hcToggle.value, "toggle-high-contrast must reflect true");
                    Assert(rmToggle != null && rmToggle.value, "toggle-reduce-motion must reflect true");
                    Assert(dimSlider != null && dimSlider.value > 0.2f, "slider-wallpaper-dim must reflect 0.3");

                    if (blueBtn != null)
                    {
                        ClickElement(blueBtn);
                        Assert(presentation.Accent == TabletAccent.Blue, "Settings click on Blue accent button must update Accent to Blue");
                    }

                    Capture("04-settings-sync-category9");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 5:
                    // Stage 5: Switch to Category 4 (WLAN), verify clear unavailable sandboxed state (NOT_RUN)
                    var catField4 = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", BindingFlags.Static | BindingFlags.NonPublic);
                    catField4?.SetValue(null, 4); // Category 4: WLAN
                    var appWin4 = root.Q("tablet-app-layer-设置") ?? root.Q("tablet-app-layer");
                    TabletSettingsAppUI.BuildSettingsPage(appWin4);

                    var wlanPage = root.Q("settings-wlan-page");
                    Assert(wlanPage != null, "settings-wlan-page missing");
                    var labels = wlanPage.Query<Label>().ToList();
                    bool foundSandbox = labels.Exists(l => l.text != null && l.text.Contains("车载内网沙箱"));
                    Assert(foundSandbox, "WLAN page must state clear unavailable sandboxed hardware status");

                    Capture("05-settings-hardware-unavailable-cat4");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 6:
                    // Stage 6: Compact Single Column Layout (< 760px, e.g. 640x900)
                    Resize(640, 900);
                    var appWinCompact = root.Q("tablet-app-layer-设置") ?? root.Q("tablet-app-layer");
                    TabletSettingsAppUI.BuildSettingsPage(appWinCompact);

                    var compactToolbar = root.Q("settings-compact-toolbar");
                    var categoryRail = root.Q("settings-category-rail");
                    var compactCategory = root.Q<DropdownField>("settings-compact-category");

                    Assert(compactToolbar != null, "settings-compact-toolbar missing in compact layout");
                    Assert(compactCategory != null, "settings-compact-category dropdown missing");

                    // Select Category 13 ("显示与亮度") via compact dropdown
                    compactCategory.value = "显示与亮度";
                    var headerLbl = root.Query<Label>().Where(l => l.text != null && l.text.Contains("显示与亮度")).First();
                    Assert(headerLbl != null, "Switching category via compact dropdown must render Display category");

                    Capture("06-settings-compact-single-column");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 7:
                    // Stage 7: Subpage Navigation & Depth (Tips glossary)
                    TabletSettingsAppUI.ShowTipsPage("rail_ballast");
                    var tipsTitle = root.Q<Label>("tips-detail-title");
                    Assert(tipsTitle != null && tipsTitle.text == "道砟", "Subpage glossary '道砟' not opened");

                    var activeTask = (TabletTaskSession)Field("activeTabletTask");
                    var pagesField = typeof(TabletTaskSession).GetField("Pages", BindingFlags.Instance | BindingFlags.NonPublic);
                    var pages = (IList)pagesField?.GetValue(activeTask);
                    Assert(pages != null && pages.Count == 1, "Subpage entry not pushed to pages stack");

                    Capture("07-settings-subpage-navigation");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 8:
                    // Stage 8: Subpage Back Recovery & Category Preservation
                    Call("RequestSystemBack");
                    var curTask = (TabletTaskSession)Field("activeTabletTask");
                    var curPagesField = typeof(TabletTaskSession).GetField("Pages", BindingFlags.Instance | BindingFlags.NonPublic);
                    var pagesAfter = (IList)curPagesField?.GetValue(curTask);
                    Assert(pagesAfter != null && pagesAfter.Count == 0, "Subpage not popped after RequestSystemBack");

                    var tipsAfter = root.Q<Label>("tips-detail-title");
                    Assert(tipsAfter == null, "Subpage view leaked after back navigation");

                    Capture("08-settings-subpage-back-recovery");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 9:
                    // Stage 9: Cold Restart Simulation from saved disk files
                    var restoredState = new TabletShellState(reviewNotificationPath);
                    Assert(restoredState.QuietMode, "Cold restart must restore QuietMode from notifications storage");

                    var restoredPresentation = new TabletPresentationPreferences(reviewPresentationPath);
                    Assert(restoredPresentation.HighContrast, "Cold restart must restore HighContrast");
                    Assert(restoredPresentation.ReduceMotion, "Cold restart must restore ReduceMotion");
                    Assert(restoredPresentation.Accent == TabletAccent.Blue, "Cold restart must restore Accent (Blue)");
                    Assert(restoredPresentation.WallpaperDim > 0.2f, "Cold restart must restore WallpaperDim");

                    Capture("09-cold-restart-restored");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 10:
                    // Stage 10: Corrupt Config Fallback & Safe Diagnostics
                    string badFile = Path.Combine(Path.GetDirectoryName(reviewPresentationPath), "damaged_runtime.json");
                    File.WriteAllText(badFile, "{corrupt_syntax");
                    var badPrefs = new TabletPresentationPreferences(badFile);

                    Assert(!badPrefs.HighContrast && !badPrefs.ReduceMotion, "Corrupted config must safely fallback to defaults");
                    Assert(badPrefs.Accent == TabletAccent.Amber, "Corrupted config must fallback to Amber accent");
                    Assert(!string.IsNullOrEmpty(badPrefs.LastError), "Corrupted config must provide clear diagnostic LastError");
                    Assert(File.ReadAllText(badFile) == "{corrupt_syntax", "Corrupted config original file must be preserved");

                    var fallbackWin = new VisualElement();
                    new TabletPresentationSettingsView(fallbackWin, badPrefs, Resources.Load<Font>("Fonts/zpix"));
                    var errLabel = fallbackWin.Q<Label>("label-error");
                    Assert(errLabel != null && errLabel.style.display == DisplayStyle.Flex, "Error label must be visible for corrupted config");

                    Capture("10-error-fallback-safe-state");

                    Debug.Log("TABLET_SETTINGS_CONTROL_GRAPHIC_REVIEW_OK: full interactive review passed: control center quick tiles grid, bidirectional settings sync, cold restart recovery, responsive compact single-column layout, subpage back navigation, and corrupt config error fallback");

                    SessionState.SetBool(PendingKey, false);
                    EditorApplication.isPlaying = false;
                    EditorApplication.Exit(0);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SessionState.SetBool(PendingKey, false);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(1);
        }
    }
}
