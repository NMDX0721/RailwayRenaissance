using System;
using System.IO;
using System.Reflection;
using RailwayRenaissance.UI.Tablet.SystemShell;
using RailwayRenaissance.UI.Tablet.Apps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

// Play-mode capture: each mutation is followed by rendered frames before sampling.
[InitializeOnLoad]
public static class TabletT317VisualReview
{
    private const string Pending = "T317.Visual.Pending";
    private const string Output = "tools/compile/t317_codex_visual_final";
    private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    private static GameMainUI host;
    private static UIDocument document;
    private static VisualElement root;
    private static RenderTexture target;
    private static TabletShellState state;
    private static int stage, lastFrame;
    private static double deadline;
    private static byte[] first, second;
    private static TextField input;

    static TabletT317VisualReview() { if (SessionState.GetBool(Pending, false)) StartChecking(); }

    public static void Run()
    {
        Directory.CreateDirectory(Output);
        SessionState.SetString("T317.Visual.Temp", Path.Combine(Path.GetTempPath(), "t317-" + Guid.NewGuid().ToString("N")));
        SessionState.SetBool(Pending, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        StartChecking(); EditorApplication.isPlaying = true;
    }

    private static void StartChecking()
    {
        deadline = EditorApplication.timeSinceStartup + 180;
        EditorApplication.update -= Check; EditorApplication.update += Check;
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static object Call(string name, params object[] args) => typeof(GameMainUI).GetMethod(name, Hidden).Invoke(host, args);
    private static object Field(string name) => typeof(GameMainUI).GetField(name, Hidden).GetValue(host);

    private static void Resize(int width, int height)
    {
        if (target != null) { document.panelSettings.targetTexture = null; target.Release(); UnityEngine.Object.Destroy(target); }
        target = new RenderTexture(width, height, 24); target.Create(); document.panelSettings.targetTexture = target;
    }

    private static byte[] Capture(string name)
    {
        var previous = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
        var pixels = image.GetPixels32(); int bright = 0;
        foreach (var p in pixels) if (p.r > 12 || p.g > 12 || p.b > 12) bright++;
        Require(bright > pixels.Length / 100, name + " is black or blank");
        byte[] bytes = image.EncodeToPNG(); File.WriteAllBytes(Path.Combine(Output, name + ".png"), bytes);
        RenderTexture.active = previous; UnityEngine.Object.Destroy(image); return bytes;
    }

    private static void Check()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("T317 visual review timed out");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || (stage > 0 && Time.frameCount - lastFrame < 12)) return;
            switch (stage)
            {
                case 0:
                    string tmp = SessionState.GetString("T317.Visual.Temp", ""); Directory.CreateDirectory(tmp);
                    typeof(GameMainUI).GetField("PresentationReviewStorage", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, Path.Combine(tmp, "presentation.json"));
                    typeof(GameMainUI).GetField("NotificationReviewStorage", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, Path.Combine(tmp, "notifications.json"));
                    GameMainUI.Show(); host = GameMainUI.Instance; Require(host != null, "tablet entry");
                    Call("EnsureDesktopBuiltSync"); document = (UIDocument)Field("uiDoc"); root = document.rootVisualElement;
                    var settings = UnityEngine.Object.Instantiate(document.panelSettings); settings.scaleMode = PanelScaleMode.ConstantPixelSize; settings.scale = 1;
                    settings.clearColor = true; settings.colorClearValue = new Color(.06f, .07f, .06f); document.panelSettings = settings;
                    Resize(1440, 900); state = (TabletShellState)Field("tabletShellState"); Call("Unlock"); break;
                case 1: first = Capture("01-desktop-1440"); state.TogglePanel(TabletSystemPanel.Controls); break;
                case 2:
                    Require(root.Q("tablet-shell-overlay").resolvedStyle.opacity > .99f, "overlay not restored");
                    Require(((TabletShellView)Field("tabletShellView")).IsBlurActive, "blur not active after opening");
                    second = Capture("02-controls-1440"); Require(!Same(first, second), "controls frame identical to desktop");
                    Call("TriggerShellBlurCapture"); Require(root.Q("tablet-shell-overlay").style.opacity.value < .01f, "overlay not hidden for recapture"); break;
                case 3:
                    Require(root.Q("tablet-shell-overlay").resolvedStyle.opacity > .99f, "overlay not restored after recapture");
                    Capture("03-controls-recaptured-1440"); Resize(1024, 768); break;
                case 4: Call("TriggerShellBlurCapture"); break;
                case 5:
                    Require(root.Q("tablet-shell-overlay").resolvedStyle.opacity > .99f, "overlay not restored after resize");
                    first = Capture("04-controls-1024"); Call("TriggerShellBlurCapture"); break;
                case 6:
                    Require(root.Q("tablet-shell-overlay").resolvedStyle.opacity > .99f, "overlay not restored after second recapture");
                    Capture("05-controls-recaptured-1024"); state.TogglePanel(TabletSystemPanel.Notifications); break;
                case 7:
                    second = Capture("06-notifications-1024"); Require(!Same(first, second), "notifications frame identical to controls");
                    Require(state.Panel == TabletSystemPanel.Notifications, "notifications panel not selected");
                    state.ClosePanel(); state.TogglePanel(TabletSystemPanel.Controls);
                    Require(state.Panel == TabletSystemPanel.Controls, "controls did not reopen after close");
                    state.ClosePanel(); Call("LaunchApp", "设置"); break;
                case 8:
                    var content = root.Q("app-content"); Require(content != null, "settings content missing");
                    input = new TextField { name = "t317-background-input", value = "draft" }; content.Add(input); input.Focus();
                    Require(root.panel.focusController.focusedElement == input, "input focus setup");
                    Call("OpenRecentTasks");
                    Require(root.Q("tablet-recent-tasks-overlay").style.display == DisplayStyle.Flex, "recents did not open");
                    Require(root.panel.focusController.focusedElement == root.Q("tablet-recent-tasks-overlay"), "focus did not enter recents");
                    bool backgroundReceived = false;
                    input.RegisterCallback<KeyDownEvent>(_ => backgroundReceived = true);
                    using (var typing = KeyDownEvent.GetPooled('z', KeyCode.Z, EventModifiers.None)) { typing.target = input; input.SendEvent(typing); }
                    Require(!backgroundReceived && input.value == "draft", "typing reached background input"); break;
                case 9:
                    Capture("07-recents-1024");
                    var recents = root.Q("tablet-recent-tasks-overlay");
                    using (var escape = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None)) { escape.target = recents; recents.SendEvent(escape); }
                    Require(recents.style.display == DisplayStyle.None && root.panel.focusController.focusedElement == input, "Escape focus restore");
                    Call("OpenRecentTasks"); Call("RequestSystemBack");
                    Require(recents.style.display == DisplayStyle.None && root.panel.focusController.focusedElement == input, "system back focus restore");
                    Call("OpenRecentTasks");
                    using (var click = ClickEvent.GetPooled()) { click.target = recents; recents.SendEvent(click); }
                    Require(recents.style.display == DisplayStyle.None && root.panel.focusController.focusedElement == input, "outside click focus restore");
                    Call("OpenRecentTasks"); input.RemoveFromHierarchy(); Call("CloseRecentTasks");
                    Require(root.panel.focusController.focusedElement == root.Q("tablet-desktop"), "destroyed focus fallback");
                    TabletSettingsAppUI.ShowTipsPage("rail_ballast"); break;
                case 10:
                    first = Capture("08-glossary-1024");
                    var task = (TabletTaskSession)typeof(GameMainUI).GetMethod("FindTabletTask", Hidden).Invoke(host, new object[] { "设置" });
                    var pages = (System.Collections.IList)typeof(TabletTaskSession).GetField("Pages", Hidden).GetValue(task);
                    Require(pages.Count == 1, "glossary page missing");
                    TabletSettingsAppUI.ShowTipsPage("rail_expansion_joint"); Require(pages.Count == 1, "same-title page stacked");
                    Call("RequestSystemBack"); Require(pages.Count == 0 && task.Root.childCount == 1 && task.Root[0].style.display == DisplayStyle.Flex, "settings parent not restored"); break;
                case 11:
                    second = Capture("09-settings-parent-1024"); Require(!Same(first, second), "parent frame identical to glossary");
                    Call("CloseAllTabletTasks"); break;
                case 12:
                    first = Capture("10-desktop-after-close-1024"); Require(!Same(first, second), "desktop frame identical to settings");
                    Debug.Log("T317_VISUAL_CAPTURE_OK: rendered frames, both ratios, panel states and recapture"); Finish(0); return;
            }
            stage++; lastFrame = Time.frameCount;
        }
        catch (Exception ex) { Debug.LogException(ex); Finish(1); }
    }

    private static bool Same(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    private static void Finish(int code)
    {
        SessionState.SetBool(Pending, false); EditorApplication.update -= Check;
        if (target != null && document != null) { document.panelSettings.targetTexture = null; target.Release(); }
        EditorApplication.Exit(code);
    }
}
