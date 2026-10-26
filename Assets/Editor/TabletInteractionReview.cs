using System;
using System.Reflection;
using RailwayRenaissance.UI.Tablet.Apps;
using RailwayRenaissance.UI.Tablet.SystemShell;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Deterministic checks for edge-gesture decisions and draft restoration.</summary>
public static class TabletInteractionReview
{
    public static void Batch()
    {
        try
        {
            CheckGestures();
            CheckDraftRestore();
            CheckBackAndRebind();
            CheckKeyboardRebind();
            CheckLiveTaskSwitch();
            Debug.Log("TABLET_INTERACTION_REVIEW_OK gestures=8 hold_drift=1 cancel=1 draft_callback=1 rebind_back=1 keyboard=1 lock=1 task_switch=1 eviction=1");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckLiveTaskSwitch()
    {
        const BindingFlags instance = BindingFlags.NonPublic | BindingFlags.Instance;
        const BindingFlags statics = BindingFlags.NonPublic | BindingFlags.Static;
        var filterField = typeof(TabletSettingsAppUI).GetField("settingsSearchFilter", statics);
        var categoryField = typeof(TabletSettingsAppUI).GetField("currentSettingsCategory", statics);
        object oldFilter = filterField.GetValue(null);
        object oldCategory = categoryField.GetValue(null);
        var previous = TabletSettingsAppUI.TabletRebindAction;
        var go = new GameObject("TabletTaskSwitchReview");
        go.SetActive(false);
        var window = ScriptableObject.CreateInstance<TabletReviewWindow>();
        window.Show();
        try
        {
            filterField.SetValue(null, "");
            categoryField.SetValue(null, 1);
            var host = go.AddComponent<GameMainUI>();
            host.unlocked = true;
            var root = window.rootVisualElement;
            var desktop = new VisualElement();
            root.Add(desktop);
            typeof(GameMainUI).GetField("root", instance).SetValue(host, root);
            typeof(GameMainUI).GetField("desktop", instance).SetValue(host, desktop);
            typeof(GameMainUI).GetMethod("BuildAppLayer", instance).Invoke(host, null);
            var launch = typeof(GameMainUI).GetMethod("LaunchApp", instance);
            var find = typeof(GameMainUI).GetMethod("FindTabletTask", instance);
            var activate = typeof(GameMainUI).GetMethod("ActivateTabletTask", instance);
            launch.Invoke(host, new object[] { "设置" });
            var settings = (TabletTaskSession)find.Invoke(host, new object[] { "设置" });
            var firstRoot = settings.Root;
            var search = firstRoot.Q<TextField>("settings-compact-search");
            Assert(search != null, "settings search exists");
            search.value = "声音";

            launch.Invoke(host, new object[] { "验收占位" });
            Assert(settings.Root == firstRoot && search.value == "声音" &&
                   firstRoot.style.display.value == DisplayStyle.None, "switch preserves live settings view");
            activate.Invoke(host, new object[] { settings });
            Assert(settings.Root == firstRoot && settings.Root.Q<TextField>("settings-compact-search").value == "声音",
                   "switch returns to settings state");

            launch.Invoke(host, new object[] { "验收占位" });
            typeof(GameMainUI).GetMethod("ReclaimInactiveTabletTasks", instance).Invoke(host, new object[] { false });
            Assert(settings.IsSuspended, "inactive settings view evicted");
            activate.Invoke(host, new object[] { settings });
            Assert(!settings.IsSuspended && settings.Root != firstRoot &&
                   settings.Root.Q<TextField>("settings-compact-search").value == "声音", "evicted settings rebuilt with state");

            typeof(GameMainUI).GetMethod("ReturnToTabletHome", instance).Invoke(host, null);
            Assert(find.Invoke(host, new object[] { "设置" }) != null, "home keeps task");
            typeof(GameMainUI).GetMethod("CloseTabletTask", instance).Invoke(host, new object[] { "设置" });
            Assert(find.Invoke(host, new object[] { "设置" }) == null, "close removes task");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            window.Close();
            filterField.SetValue(null, oldFilter);
            categoryField.SetValue(null, oldCategory);
            TabletSettingsAppUI.TabletRebindAction = previous;
        }
    }

    private static void CheckKeyboardRebind()
    {
        var previous = TabletSettingsAppUI.TabletRebindAction;
        var go = new GameObject("TabletKeyboardReview");
        go.SetActive(false);
        var window = ScriptableObject.CreateInstance<TabletReviewWindow>();
        window.Show();
        try
        {
            var host = go.AddComponent<GameMainUI>();
            typeof(GameMainUI).GetField("root", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(host, window.rootVisualElement);
            typeof(GameMainUI).GetMethod("BuildLockScreen", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(host, null);
            var state = (TabletShellState)typeof(GameMainUI)
                .GetField("tabletShellState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(host);
            KeyCode? captured = null;
            TabletSettingsAppUI.TabletRebindAction = key => captured = key;
            state.TogglePanel(TabletSystemPanel.Controls);
            var target = window.rootVisualElement.Q("tablet-lock");
            Assert(target != null, "keyboard event target");
            target.focusable = true;
            window.Focus();
            target.Focus();
            Debug.Log($"TABLET_KEYBOARD_FOCUS focused={target.panel?.focusController.focusedElement == target}");
            SendEscape(target);
            Debug.Log($"TABLET_KEYBOARD_REVIEW panel={state.Panel} captured={captured} pending={TabletSettingsAppUI.TabletRebindAction != null}");
            Assert(state.Panel == TabletSystemPanel.None && captured == null &&
                   TabletSettingsAppUI.TabletRebindAction != null, "keyboard closes panel before rebind");
            SendEscape(target);
            Assert(captured == null && TabletSettingsAppUI.TabletRebindAction != null,
                   "same-frame Escape does not cancel rebind twice");
            typeof(GameMainUI).GetMethod("RequestSystemBack", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(host, null);
            Assert(captured == KeyCode.Escape, "rebind remains cancellable after panel closes");

            var gesture = (TabletEdgeGesture)typeof(GameMainUI)
                .GetField("tabletEdgeGesture", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(host);
            host.unlocked = true;
            Assert(gesture.Begin(20, new Vector2(500, 990), 1000, 1000, 44, false), "lock gesture begin");
            gesture.Move(20, new Vector2(500, 900), 1f);
            typeof(GameMainUI).GetMethod("ShowLockScreen", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(host, null);
            Assert(!host.unlocked && gesture.Tick(2f) == TabletGestureAction.None, "lock clears pending gesture");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            window.Close();
            TabletSettingsAppUI.TabletRebindAction = previous;
        }
    }

    private static void SendEscape(VisualElement target)
    {
        using (KeyDownEvent evt = KeyDownEvent.GetPooled(Event.KeyboardEvent("escape")))
        {
            Debug.Log($"TABLET_KEYBOARD_EVENT key={evt.keyCode} attached={target.panel != null}");
            target.SendEvent(evt);
        }
    }

    private static void CheckBackAndRebind()
    {
        var previous = TabletSettingsAppUI.TabletRebindAction;
        var go = new GameObject("TabletInteractionReview");
        go.SetActive(false);
        try
        {
            var host = go.AddComponent<GameMainUI>();
            var stateField = typeof(GameMainUI).GetField("tabletShellState", BindingFlags.NonPublic | BindingFlags.Instance);
            var requestBack = typeof(GameMainUI).GetMethod("RequestSystemBack", BindingFlags.NonPublic | BindingFlags.Instance);
            var state = (TabletShellState)stateField.GetValue(host);
            KeyCode? captured = null;
            TabletSettingsAppUI.TabletRebindAction = key => captured = key;
            state.TogglePanel(TabletSystemPanel.Controls);
            requestBack.Invoke(host, null);
            Assert(state.Panel == TabletSystemPanel.None && captured == null &&
                   TabletSettingsAppUI.TabletRebindAction != null, "panel closes before rebind cancellation");
            requestBack.Invoke(host, null);
            Assert(captured == KeyCode.Escape && TabletSettingsAppUI.TabletRebindAction == null,
                   "screen back cancels rebind");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            TabletSettingsAppUI.TabletRebindAction = previous;
        }
    }

    private static void CheckGestures()
    {
        var g = new TabletEdgeGesture();
        Assert(g.Begin(1, new Vector2(500, 990), 1000, 1000, 44, false), "bottom begin");
        g.Move(1, new Vector2(500, 900), 1f);
        Assert(g.Tick(1.27f) == TabletGestureAction.None, "hold before threshold");
        Assert(g.Tick(1.29f) == TabletGestureAction.Recents, "hold opens recents before release");
        Assert(g.End(1, new Vector2(500, 900)) == TabletGestureAction.Consumed, "release after hold");

        Assert(g.Begin(2, new Vector2(500, 990), 1000, 1000, 44, false), "quick swipe begin");
        g.Move(2, new Vector2(500, 900), 2f);
        Assert(g.End(2, new Vector2(500, 900)) == TabletGestureAction.Home, "quick swipe home");

        Assert(g.Begin(3, new Vector2(100, 5), 1000, 1000, 44, false), "notification begin");
        Assert(g.End(3, new Vector2(100, 100)) == TabletGestureAction.Notifications, "notification swipe");
        Assert(g.Begin(4, new Vector2(900, 5), 1000, 1000, 44, false), "control begin");
        Assert(g.End(4, new Vector2(900, 100)) == TabletGestureAction.Controls, "control swipe");

        Assert(!g.Begin(5, new Vector2(5, 500), 1000, 1000, 44, true), "direct control blocks side back");
        Assert(g.Begin(6, new Vector2(5, 500), 1000, 1000, 44, false), "left edge begin");
        Assert(!g.Begin(7, new Vector2(995, 500), 1000, 1000, 44, false), "second pointer ignored");
        g.Cancel(7);
        Assert(g.End(6, new Vector2(100, 500)) == TabletGestureAction.Back, "inward left back");
        Assert(g.Begin(8, new Vector2(995, 500), 1000, 1000, 44, false), "right edge begin");
        Assert(g.End(8, new Vector2(1090, 500)) == TabletGestureAction.None, "outward right swipe ignored");
        Assert(g.Begin(9, new Vector2(995, 500), 1000, 1000, 44, false), "right edge retry");
        Assert(g.End(9, new Vector2(900, 500)) == TabletGestureAction.Back, "inward right back");

        Assert(g.Begin(10, new Vector2(500, 990), 1000, 1000, 44, false), "moving hold begin");
        g.Move(10, new Vector2(500, 900), 3f);
        g.Move(10, new Vector2(500, 880), 3.2f);
        Assert(g.Tick(3.4f) == TabletGestureAction.None, "continued motion resets hold timer");
        Assert(g.Tick(3.49f) == TabletGestureAction.Recents, "hold after motion opens recents");
        g.Cancel(10);
        Assert(g.End(10, new Vector2(500, 880)) == TabletGestureAction.None, "cancelled pointer has no action");
    }

    private static void CheckDraftRestore()
    {
        Type snapshotType = typeof(TabletTaskSession).Assembly.GetType(
            "RailwayRenaissance.UI.Tablet.SystemShell.TabletViewStateSnapshot", true);
        MethodInfo capture = snapshotType.GetMethod("Capture", BindingFlags.Public | BindingFlags.Static);
        MethodInfo restore = snapshotType.GetMethod("Restore", BindingFlags.Public | BindingFlags.Instance);
        var source = new VisualElement();
        source.Add(new TextField { value = "未保存的草稿" });
        object snapshot = capture.Invoke(null, new object[] { source });

        var window = ScriptableObject.CreateInstance<TabletReviewWindow>();
        window.Show();
        try
        {
            var target = new VisualElement();
            var field = new TextField();
            string model = null;
            field.RegisterValueChangedCallback(evt => model = evt.newValue);
            target.Add(field);
            window.rootVisualElement.Add(target);
            Assert(field.panel != null, "review field attached to a panel");
            restore.Invoke(snapshot, new object[] { target });
            Assert(field.value == "未保存的草稿" && model == field.value, "draft and model callback restored");
        }
        finally
        {
            window.Close();
        }
    }

    private static void Assert(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("Tablet interaction review failed: " + name);
    }
}

public sealed class TabletReviewWindow : EditorWindow { }
