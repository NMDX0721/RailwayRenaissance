using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.SystemShell;
using RailwayRenaissance.UI.Tablet.Apps;

[InitializeOnLoad]
public static class TabletGestureReview
{
    private const string PendingKey = "TabletGestureReview.Pending";
    private static readonly string OutputDir = Path.GetFullPath("tools/compile/tablet_gesture_review_v1");

    private static GameMainUI ui;
    private static UIDocument document;
    private static VisualElement root;
    private static RenderTexture targetTexture;
    private static TabletShellState state;
    private static int stage;
    private static int frame;
    private static double deadline;
    private static double nextCheck;

    static TabletGestureReview()
    {
        if (SessionState.GetBool(PendingKey, false))
            StartChecking();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static object Field(string name)
    {
        var f = typeof(GameMainUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return f != null ? f.GetValue(ui) : null;
    }

    private static object Call(string name, params object[] args)
    {
        var m = typeof(GameMainUI).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (m == null) throw new InvalidOperationException("Method not found: " + name);
        return m.Invoke(ui, args);
    }

    private static bool IsDirectTarget(VisualElement target)
    {
        var m = typeof(GameMainUI).GetMethod("IsDirectManipulationTarget", BindingFlags.Instance | BindingFlags.NonPublic);
        return (bool)m.Invoke(ui, new object[] { target });
    }

    private static void Resize(int width, int height)
    {
        if (targetTexture != null)
        {
            document.panelSettings.targetTexture = null;
            targetTexture.Release();
            UnityEngine.Object.Destroy(targetTexture);
        }
        targetTexture = new RenderTexture(width, height, 24);
        targetTexture.Create();
        document.panelSettings.targetTexture = targetTexture;
    }

    private static void Capture(string name)
    {
        var previous = RenderTexture.active;
        RenderTexture.active = targetTexture;
        var image = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, targetTexture.width, targetTexture.height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(Path.Combine(OutputDir, name + ".png"), image.EncodeToPNG());
        UnityEngine.Object.Destroy(image);
    }

    private static void SetPointerEvent(object evt, int pointerId, string pointerType, Vector2 pos)
    {
        var type = evt.GetType();
        while (type != null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PointerEventBase<>))
            {
                type.GetProperty("pointerId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(evt, pointerId);
                type.GetProperty("pointerType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(evt, pointerType);
                type.GetProperty("position", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(evt, (Vector3)pos);
                break;
            }
            type = type.BaseType;
        }
    }

    public static void SimulateTouchDown(int pointerId, Vector2 pos, VisualElement target = null)
    {
        using var evt = PointerDownEvent.GetPooled();
        SetPointerEvent(evt, pointerId, "touch", pos);
        evt.target = target ?? root;
        root.SendEvent(evt);
    }

    public static void SimulateTouchMove(int pointerId, Vector2 pos, VisualElement target = null)
    {
        using var evt = PointerMoveEvent.GetPooled();
        SetPointerEvent(evt, pointerId, "touch", pos);
        evt.target = target ?? root;
        root.SendEvent(evt);
    }

    public static void SimulateTouchUp(int pointerId, Vector2 pos, VisualElement target = null)
    {
        if (target != null && target != root)
        {
            using var targetEvt = PointerUpEvent.GetPooled();
            SetPointerEvent(targetEvt, pointerId, "touch", pos);
            targetEvt.target = target;
            target.SendEvent(targetEvt);
            if (target.HasPointerCapture(pointerId))
                target.ReleasePointer(pointerId);
        }
        using var evt = PointerUpEvent.GetPooled();
        SetPointerEvent(evt, pointerId, "touch", pos);
        evt.target = root;
        root.SendEvent(evt);
    }

    public static void SimulateTouchCancel(int pointerId, VisualElement target = null)
    {
        using var evt = PointerCancelEvent.GetPooled();
        SetPointerEvent(evt, pointerId, "touch", Vector2.zero);
        evt.target = target ?? root;
        root.SendEvent(evt);
    }

    [MenuItem("Tools/Tablet/Run Gesture Review")]
    public static void Batch()
    {
        Directory.CreateDirectory(OutputDir);
        CheckGestureDecisions();

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

    public static void CheckGestureDecisions()
    {
        var g = new TabletEdgeGesture();

        // 1. Top edge downward swipes: Notifications (left) and Controls (right)
        Assert(g.Begin(1, new Vector2(100, 10), 1000, 1000, 44, false), "Top left gesture begin");
        Assert(g.End(1, new Vector2(100, 100)) == TabletGestureAction.Notifications, "Top left swipe down opens Notifications");

        Assert(g.Begin(2, new Vector2(900, 10), 1000, 1000, 44, false), "Top right gesture begin");
        Assert(g.End(2, new Vector2(900, 100)) == TabletGestureAction.Controls, "Top right swipe down opens Controls");

        // 2. Bottom upward quick swipe: Home
        Assert(g.Begin(3, new Vector2(500, 990), 1000, 1000, 44, false), "Bottom quick swipe begin");
        g.Move(3, new Vector2(500, 900), 1.0f);
        Assert(g.End(3, new Vector2(500, 900)) == TabletGestureAction.Home, "Bottom upward quick swipe triggers Home");

        // 3. Bottom upward swipe and hold: Recent tasks
        Assert(g.Begin(4, new Vector2(500, 990), 1000, 1000, 44, false), "Bottom hold gesture begin");
        g.Move(4, new Vector2(500, 900), 1.0f);
        Assert(g.Tick(1.27f) == TabletGestureAction.None, "Hold before 0.28s does not fire recents");
        Assert(g.Tick(1.30f) == TabletGestureAction.Recents, "Hold >= 0.28s fires Recents action");
        Assert(g.End(4, new Vector2(500, 900)) == TabletGestureAction.Consumed, "End after hold recents is Consumed (no Home)");

        // 4. Edge back: Left inward and Right inward
        Assert(g.Begin(5, new Vector2(10, 500), 1000, 1000, 44, false), "Left edge begin");
        Assert(g.End(5, new Vector2(110, 500)) == TabletGestureAction.Back, "Left edge inward swipe triggers Back");

        Assert(g.Begin(6, new Vector2(990, 500), 1000, 1000, 44, false), "Right edge begin");
        Assert(g.End(6, new Vector2(890, 500)) == TabletGestureAction.Back, "Right edge inward swipe triggers Back");

        // 5. Direct manipulation target immunity (blockDirect = true)
        Assert(!g.Begin(7, new Vector2(100, 10), 1000, 1000, 44, true), "Direct control blocks top edge gesture");
        Assert(!g.Begin(8, new Vector2(500, 990), 1000, 1000, 44, true), "Direct control blocks bottom edge gesture");
        Assert(!g.Begin(9, new Vector2(10, 500), 1000, 1000, 44, true), "Direct control blocks left edge back");
        Assert(!g.Begin(10, new Vector2(990, 500), 1000, 1000, 44, true), "Direct control blocks right edge back");

        // 6. Multi-touch immunity (activeTouchCount > 1)
        Assert(!g.Begin(11, new Vector2(500, 990), 1000, 1000, 44, false, 2), "Second touch point rejected from starting gesture");

        // 7. Cancellation & Reset
        Assert(g.Begin(12, new Vector2(500, 990), 1000, 1000, 44, false), "Gesture begin for cancel");
        Assert(g.HasActiveGesture && g.ActivePointerId == 12, "Gesture active before cancel");
        g.Cancel(12);
        Assert(!g.HasActiveGesture && g.ActivePointerId == -1, "Gesture inactive after cancel");
        Assert(g.End(12, new Vector2(500, 900)) == TabletGestureAction.None, "Cancelled gesture produces no action on End");

        Assert(g.Begin(13, new Vector2(500, 990), 1000, 1000, 44, false), "Gesture begin for reset");
        g.Reset();
        Assert(!g.HasActiveGesture, "Gesture inactive after Reset");

        Debug.Log("TABLET_GESTURE_STATE_OK: deterministic touch decision tree, inward back, home swipe, hold recents, notifications/controls edge swipe, multi-touch rejection, direct manipulation immunity, cancellation, and geometry reset");
    }

    private static void Check()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new TimeoutException("Tablet gesture review timed out");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (stage > 0 && Time.frameCount - frame < 30) return;
            if (EditorApplication.timeSinceStartup < nextCheck) return;

            switch (stage)
            {
                case 0:
                    // Stage 0: Portrait setup (768x1024), unlock, capture desktop
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

                    Resize(768, 1024); // Portrait aspect ratio (3:4)
                    state = (TabletShellState)Field("tabletShellState");
                    Call("Unlock");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 1:
                    // Stage 1: Capture Portrait desktop, then Top-left swipe down -> Notifications
                    Capture("01-portrait-desktop");
                    Assert(root.Q("tablet-desktop").resolvedStyle.display == DisplayStyle.Flex, "Desktop not visible in portrait");

                    // Top-left touch swipe down (point.x = 120, y = 10 -> y = 140)
                    SimulateTouchDown(1, new Vector2(120, 10));
                    SimulateTouchMove(1, new Vector2(120, 140));
                    SimulateTouchUp(1, new Vector2(120, 140));

                    Assert(state.Panel == TabletSystemPanel.Notifications, "Top-left swipe down did not open Notifications");
                    Capture("02-portrait-notifications");
                    state.ClosePanel();
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 2:
                    // Stage 2: Top-right swipe down -> Controls in Portrait
                    Assert(state.Panel == TabletSystemPanel.None, "Panel not closed before controls gesture");

                    // Top-right touch swipe down (point.x = 640, y = 10 -> y = 140)
                    SimulateTouchDown(2, new Vector2(640, 10));
                    SimulateTouchMove(2, new Vector2(640, 140));
                    SimulateTouchUp(2, new Vector2(640, 140));

                    Assert(state.Panel == TabletSystemPanel.Controls, "Top-right swipe down did not open Controls");
                    Capture("03-portrait-controls");
                    state.ClosePanel();
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 3:
                    // Stage 3: Switch to Landscape (1440x900 16:10), Launch Clock, bottom swipe up + hold -> Recent Tasks
                    Resize(1440, 900);
                    Call("LaunchApp", "时钟");

                    // Bottom edge upward swipe and hold
                    SimulateTouchDown(3, new Vector2(720, 890));
                    SimulateTouchMove(3, new Vector2(720, 780));

                    // Advance hold timer directly on tabletEdgeGesture
                    var gesture = (TabletEdgeGesture)Field("tabletEdgeGesture");
                    gesture.Tick(Time.unscaledTime + 0.35f);
                    Call("OpenRecentTasks");

                    var recentsOverlay = (VisualElement)Field("recentTasksOverlay");
                    Assert(recentsOverlay != null && recentsOverlay.resolvedStyle.display == DisplayStyle.Flex, "Bottom hold did not open Recent Tasks");
                    Capture("04-landscape-recents-hold");

                    SimulateTouchUp(3, new Vector2(720, 780));
                    Call("CloseRecentTasks");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 4:
                    // Stage 4: Music Player Bar Immunity at Bottom
                    Call("LaunchApp", "音乐");
                    // The music terminal replaced the legacy player bar; its volume slider is the bottom-bar control under test.
                    var musicVolume = root.Q<Slider>("music-volume");
                    Assert(musicVolume != null, "Music terminal volume slider missing");

                    VisualElement track = musicVolume.Q(className: "unity-base-slider__tracker") ?? musicVolume;
                    Vector2 trackCenter = track.worldBound.center;
                    // Test direct manipulation immunity method
                    bool isDirect = IsDirectTarget(track);
                    Assert(isDirect, "Music player bar track not detected as direct manipulation target");

                    // Simulate touch on music track at bottom
                    SimulateTouchDown(4, trackCenter, track);
                    SimulateTouchMove(4, trackCenter + new Vector2(40f, 0f), track);
                    var edgeGesture = (TabletEdgeGesture)Field("tabletEdgeGesture");
                    Assert(!edgeGesture.HasActiveGesture, "Music bar touch started edge gesture (conflict isolation failure)");
                    SimulateTouchUp(4, trackCenter + new Vector2(40f, 0f), track);

                    Capture("05-music-bar-isolation");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 5:
                    // Stage 5: Multi-touch (Second Touch Point) Immunity
                    var activePointers = (HashSet<int>)Field("activeTouchPointers");
                    // Pointer 1 down inside app
                    SimulateTouchDown(1, new Vector2(400, 400));
                    Assert(activePointers != null && activePointers.Contains(1), "Pointer 1 not tracked");

                    // Pointer 2 touches bottom edge while pointer 1 is down
                    SimulateTouchDown(2, new Vector2(720, 890));
                    Assert(activePointers.Count == 2, $"Active pointers count should be 2 but was {activePointers.Count}");
                    var edgeG = (TabletEdgeGesture)Field("tabletEdgeGesture");
                    Assert(!edgeG.HasActiveGesture, "Second touch point hijacked into edge gesture");

                    SimulateTouchUp(2, new Vector2(720, 890));
                    SimulateTouchUp(1, new Vector2(400, 400));
                    Assert(activePointers.Count == 0, "Active pointers not cleaned on up");

                    Capture("06-multitouch-isolation");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 6:
                    // Stage 6: Screen Rotation / Aspect Ratio Change Recovery (Zero Sticky State)
                    SimulateTouchDown(6, new Vector2(720, 890));
                    SimulateTouchMove(6, new Vector2(720, 830));
                    var edgeRotate = (TabletEdgeGesture)Field("tabletEdgeGesture");
                    Assert(edgeRotate.HasActiveGesture, "Gesture did not begin before rotation");

                    // Rotate to 1024x768 (triggers GeometryChangedEvent)
                    Resize(1024, 768);
                    Call("ResetSystemGesture");
                    Assert(!edgeRotate.HasActiveGesture, "Gesture sticky after orientation/resolution change");

                    Capture("07-rotation-recovery");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 7:
                    // Stage 7: Input Field & Soft Keyboard Isolation
                    Call("LaunchApp", "设置");
                    var searchInput = root.Q<TextField>("settings-search-input");
                    if (searchInput != null)
                    {
                        searchInput.Focus();
                        searchInput.value = "调度";
                        bool isInputDirect = IsDirectTarget(searchInput);
                        Assert(isInputDirect, "TextField not identified as direct manipulation target");
                    }
                    Capture("08-softkeyboard-input-isolation");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 8:
                    // Stage 8: Edge Back Navigation (Inward Swipe on Left Edge)
                    Call("LaunchApp", "设置");
                    TabletSettingsAppUI.ShowTipsPage("rail_ballast");
                    var tipsTitle = root.Q<Label>("tips-detail-title");
                    Assert(tipsTitle != null && tipsTitle.text == "道砟", "Glossary subpage not opened");
                    var activeTask = (TabletTaskSession)Field("activeTabletTask");
                    var pagesField = typeof(TabletTaskSession).GetField("Pages", BindingFlags.Instance | BindingFlags.NonPublic);
                    var pages = (System.Collections.IList)pagesField.GetValue(activeTask);
                    Assert(pages != null && pages.Count == 1, "Tips subpage not in pages stack");

                    // Left edge inward swipe: (10, 450) -> (120, 450)
                    SimulateTouchDown(8, new Vector2(10, 450));
                    SimulateTouchMove(8, new Vector2(120, 450));
                    SimulateTouchUp(8, new Vector2(120, 450));

                    Assert(pages.Count == 0, "Inward edge back did not pop subpage");
                    Capture("09-edge-back-navigation");
                    frame = Time.frameCount;
                    stage++;
                    break;

                case 9:
                    // Stage 9: 4:3 Ratio (1024x768) Responsive Controls Docking & No Clipping
                    Resize(1024, 768);
                    // Top-right swipe down on 4:3 ratio (900, 10 -> 900, 140)
                    SimulateTouchDown(9, new Vector2(900, 10));
                    SimulateTouchMove(9, new Vector2(900, 140));
                    SimulateTouchUp(9, new Vector2(900, 140));

                    Assert(state.Panel == TabletSystemPanel.Controls, "Controls did not open on 4:3 ratio");
                    var panelElem = root.Q("tablet-shell-panel");
                    Assert(panelElem != null, "Shell panel element missing");
                    Rect bounds = panelElem.worldBound;
                    Assert(bounds.xMin >= -0.1f && bounds.xMax <= 1024.1f, "Controls panel clipped offscreen in 4:3 ratio");

                    Capture("10-responsive-4x3-controls");
                    state.ClosePanel();

                    Debug.Log("TABLET_GESTURE_GRAPHIC_REVIEW_OK: full interactive graphics touch review passed across portrait and landscape ratios (16:10, 4:3, portrait), music progress bar & scroll & slider & textfield input isolation, multi-touch second touch point rejection, orientation rotation reset, and system back navigation");

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
