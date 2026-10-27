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
            CheckMusicViewRelease();
            CheckBackupCompleteness();
            CheckDamagedNotificationStorage();
            Assert(Resources.Load<Shader>("UI/TabletGaussianBlur") != null, "blur shader included in Resources");
            CheckBlurRendering();
            Debug.Log("TABLET_INTERACTION_REVIEW_OK gestures=8 hold_drift=1 cancel=1 draft_callback=1 rebind_back=1 keyboard=1 lock=1 task_switch=1 eviction=1 subpage=1 tips=1 deep_link=1 suiyue_history=1 lifecycle=1 music_view=1 calculator=1 backup_completeness=1");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private sealed class ReviewStorage : IKeyValueStorage
    {
        internal readonly System.Collections.Generic.Dictionary<string, string> Values = new();
        internal int Writes;
        public bool HasKey(string key) => Values.ContainsKey(key);
        public string GetString(string key, string fallback = "") => Values.TryGetValue(key, out var value) ? value : fallback;
        public void SetString(string key, string value) { Writes++; Values[key] = value; }
        public void DeleteKey(string key) { Writes++; Values.Remove(key); }
        public void Save() { Writes++; }
    }

    private static void CheckBlurRendering()
    {
        var input = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var source = new RenderTexture(32, 32, 0);
        var readback = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) input.SetPixel(x, y, x < 16 ? Color.black : Color.white);
            input.Apply(); source.Create(); Graphics.Blit(input, source);
            using (var blur = new UI.Tablet.Rendering.TabletGaussianBlur { Downsample = 1 })
            {
                Assert(blur.IsSupported, "blur shader supported: " + blur.LastError);
                var result = blur.Process(source);
                Assert(result != null && result != source, "blur creates a distinct result");
                RenderTexture.active = result; readback.ReadPixels(new Rect(0, 0, 32, 32), 0, 0); readback.Apply();
                float left = readback.GetPixel(15, 16).grayscale, right = readback.GetPixel(16, 16).grayscale;
                Assert(left > .05f && right < .95f && right > left, "blur blends across a sharp edge");
            }
        }
        finally
        {
            RenderTexture.active = previous; source.Release();
            UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(input); UnityEngine.Object.DestroyImmediate(readback);
        }
    }

    private static void CheckDamagedNotificationStorage()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tablet-notification-review-" + Guid.NewGuid() + ".json");
        const string damaged = "{\"schemaVersion\":1,\"notifications\":";
        try
        {
            System.IO.File.WriteAllText(path, damaged);
            var storage = new TabletNotificationStorage(path);
            Assert(!storage.Load(out _, out _, out _) && storage.IsReadOnly, "damaged notification file enters read-only mode");
            Assert(!storage.Save(false, null) && System.IO.File.ReadAllText(path) == damaged, "automatic save preserves damaged file");
            System.IO.File.WriteAllText(path, "{\"schemaVersion\":1,\"quietMode\":false,\"notifications\":[]}");
            Assert(storage.Load(out _, out _, out _) && !storage.IsReadOnly && storage.Save(true, null), "successful reload releases read-only mode");
        }
        finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    }

    private static void CheckBackupCompleteness()
    {
        var storage = new ReviewStorage();
        storage.Values["VN_Save_0"] = "save";
        storage.Values["Achievements_Data"] = "achievements";
        storage.Values[TabletLocalBackupService.ManualIndexKey] = "bm_20261027000000000";
        storage.Values[TabletLocalBackupService.ManualKeyPrefix + "bm_20261027000000000"] = "bookmark";
        string valid = TabletLocalBackupService.CreateBackupPayload(storage);
        var partial = JsonUtility.FromJson<TabletLocalBackupService.BackupPayloadV2>(valid);
        partial.fixedItems.RemoveAt(0);
        string missingKey = JsonUtility.ToJson(partial);
        int invalidCase = 0;
        foreach (string invalid in new[] { "{\"schemaVersion\":2}", missingKey,
            valid.Replace("\"manualBookmarks\"", "\"missingBookmarks\""),
            valid.Replace("\"manualIndex\"", "\"missingIndex\""),
            valid.Replace("\"fixedItems\"", "\"missingFixed\"") })
        {
            Assert(!TabletLocalBackupService.RestoreBackup(storage, invalid, out _, out _), "incomplete backup rejected case " + invalidCase);
            Assert(storage.Writes == 0 && storage.Values.Count == 4 && storage.Values["VN_Save_0"] == "save",
                   "invalid backup cannot mutate saves, achievements or bookmarks");
            invalidCase++;
        }
        Assert(TabletLocalBackupService.RestoreBackup(storage, valid, out _, out _), "complete backup round trip");
        var empty = new ReviewStorage();
        Assert(TabletLocalBackupService.RestoreBackup(storage, TabletLocalBackupService.CreateBackupPayload(empty), out _, out _),
               "explicit complete empty snapshot remains supported");
        Assert(!storage.HasKey("VN_Save_0") && !storage.HasKey("Achievements_Data") && !storage.HasKey(TabletLocalBackupService.ManualIndexKey),
               "complete empty snapshot retains exact deletion semantics");
    }

    public static void ScrollBatch()
    {
        try { StartScrollRestoreCheck(); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    private sealed class ReviewLifecycle : ITabletTaskLifecycle
    {
        internal int Foreground, Background, Reclaimed, Restored, Closed;
        public void OnViewCreated(VisualElement view, bool restored) { if (restored) Restored++; }
        public void OnForeground(VisualElement view) => Foreground++;
        public void OnBackground(VisualElement view) => Background++;
        public void OnViewReclaimed(VisualElement view) => Reclaimed++;
        public void OnClosed(VisualElement view) => Closed++;
    }

    private static void CheckMusicViewRelease()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        Type music = typeof(TabletMusicAppUI);
        FieldInfo active = music.GetField("s_activeWindow", flags);
        Assert(active.GetValue(null) == null, "music view review isolated");
        bool oldPlaying = TabletMusicAppUI.tabletIsPlaying;
        var oldView = TabletMusicAppUI.s_currentView;
        var toast = music.GetField("s_showToast", flags);
        object oldToast = toast.GetValue(null);
        var close = music.GetField("s_onClose", flags);
        object oldClose = close.GetValue(null);
        var root = new VisualElement();
        var view = new VisualElement();
        root.Add(view);
        bool notificationReceived = false;
        try
        {
            active.SetValue(null, view);
            music.GetField("tabletProgressFill", flags).SetValue(null, new VisualElement());
            music.GetField("s_toastPill", flags).SetValue(null, new VisualElement());
            music.GetField("s_toastLabel", flags).SetValue(null, new Label());
            toast.SetValue(null, new Action<string>(_ => notificationReceived = true));
            TabletMusicAppUI.tabletIsPlaying = true;
            TabletMusicAppUI.s_currentView = MusicSidebarNavView.MusicViewId.Ost;
            TabletMusicAppUI.SetTaskViewForeground(root, false);
            TabletMusicAppUI.ShowToast("review");
            Assert(notificationReceived, "background music prompt uses system channel");
            TabletMusicAppUI.ReleaseTaskView(root);
            Assert(active.GetValue(null) == null && music.GetField("tabletProgressFill", flags).GetValue(null) == null,
                   "music view references released");
            Assert(TabletMusicAppUI.tabletIsPlaying && TabletMusicAppUI.s_currentView == MusicSidebarNavView.MusicViewId.Ost,
                   "music playback and page state retained");
            TabletMusicAppUI.CloseTaskView(root);
            Assert(toast.GetValue(null) == null && close.GetValue(null) == null, "closed music task releases host callbacks");
        }
        finally
        {
            TabletMusicAppUI.ReleaseTaskView(root);
            TabletMusicAppUI.tabletIsPlaying = oldPlaying;
            TabletMusicAppUI.s_currentView = oldView;
            toast.SetValue(null, oldToast);
            close.SetValue(null, oldClose);
        }
    }

    private static void StartScrollRestoreCheck()
    {
        var window = ScriptableObject.CreateInstance<TabletReviewWindow>();
        window.position = new Rect(0, 0, 600, 400);
        window.Show();
        var sourceRoot = new VisualElement();
        var source = BuildReviewScroll();
        sourceRoot.Add(source);
        window.rootVisualElement.Add(sourceRoot);
        Type snapshotType = typeof(TabletTaskSession).Assembly.GetType(
            "RailwayRenaissance.UI.Tablet.SystemShell.TabletViewStateSnapshot", true);
        MethodInfo capture = snapshotType.GetMethod("Capture", BindingFlags.Public | BindingFlags.Static);
        MethodInfo restore = snapshotType.GetMethod("Restore", BindingFlags.Public | BindingFlags.Instance);
        ScrollView target = null;
        int stage = 0;
        int targetFrames = 0;
        double started = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction update = null;
        update = () =>
        {
            try
            {
                window.Repaint();
                if (EditorApplication.timeSinceStartup - started > 20)
                {
                    EditorApplication.update -= update;
                    Debug.LogWarning($"TABLET_SCROLL_REVIEW_NOT_RUN: no usable layout; stage={stage}, viewport={source.contentViewport.layout}, content={source.contentContainer.layout}, attached={source.panel != null}");
                    window.Close();
                    EditorApplication.Exit(2);
                    return;
                }
                if (stage == 0 && source.contentViewport.layout.height > 0 &&
                    source.contentContainer.layout.height > source.contentViewport.layout.height + 120)
                {
                    source.scrollOffset = new Vector2(0, 120);
                    Assert(Mathf.Abs(source.scrollOffset.y - 120) < 1, "source scroll positioned");
                    object snapshot = capture.Invoke(null, new object[] { sourceRoot });
                    sourceRoot.RemoveFromHierarchy();
                    var targetRoot = new VisualElement();
                    target = BuildReviewScroll();
                    targetRoot.Add(target);
                    window.rootVisualElement.Add(targetRoot);
                    restore.Invoke(snapshot, new object[] { targetRoot });
                    stage = 1;
                }
                else if (stage == 1 && ++targetFrames >= 8 && target.contentViewport.layout.height > 0)
                {
                    Assert(Mathf.Abs(target.scrollOffset.y - 120) < 1, "scroll offset restored after layout");
                    EditorApplication.update -= update;
                    window.Close();
                    Debug.Log("TABLET_SCROLL_REVIEW_OK offset=120");
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception ex)
            {
                EditorApplication.update -= update;
                window.Close();
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        };
        EditorApplication.update += update;
    }

    private static ScrollView BuildReviewScroll()
    {
        var scroll = new ScrollView();
        scroll.style.width = 250;
        scroll.style.height = 100;
        for (int i = 0; i < 20; i++)
        {
            var row = new Label("row " + i);
            row.style.height = 24;
            row.style.flexShrink = 0;
            scroll.Add(row);
        }
        return scroll;
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
        var instanceProperty = typeof(GameMainUI).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        object previousInstance = instanceProperty.GetValue(null);
        var go = new GameObject("TabletTaskSwitchReview");
        go.SetActive(false);
        var window = ScriptableObject.CreateInstance<TabletReviewWindow>();
        window.Show();
        try
        {
            filterField.SetValue(null, "");
            categoryField.SetValue(null, 1);
            var host = go.AddComponent<GameMainUI>();
            instanceProperty.GetSetMethod(true).Invoke(null, new object[] { host });
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
            var lifecycle = new ReviewLifecycle();
            typeof(TabletTaskSession).GetProperty("Lifecycle", instance).SetValue(settings, lifecycle);
            var firstRoot = settings.Root;
            var search = firstRoot.Q<TextField>("settings-compact-search");
            Assert(search != null, "settings search exists");
            search.value = "声音";
            Action openSubpage = null;
            openSubpage = () =>
            {
                VisualElement page = host.BuildInnerPanel("验收子页", openSubpage);
                page.Add(new TextField { name = "review-subpage-draft" });
            };
            openSubpage();
            firstRoot.Q<TextField>("review-subpage-draft").value = "子页草稿";

            launch.Invoke(host, new object[] { "验收占位" });
            Assert(settings.Root == firstRoot && search.value == "声音" &&
                   firstRoot.style.display.value == DisplayStyle.None, "switch preserves live settings view");
            activate.Invoke(host, new object[] { settings });
            Assert(settings.Root == firstRoot && settings.Root.Q<TextField>("review-subpage-draft").value == "子页草稿",
                   "switch returns to settings state");

            launch.Invoke(host, new object[] { "验收占位" });
            typeof(GameMainUI).GetMethod("ReclaimInactiveTabletTasks", instance).Invoke(host, new object[] { false });
            Assert(settings.IsSuspended, "inactive settings view evicted");
            activate.Invoke(host, new object[] { settings });
            Assert(!settings.IsSuspended && settings.Root != firstRoot &&
                   settings.Root.Q<TextField>("settings-compact-search").value == "声音" &&
                   settings.Root.Q<TextField>("review-subpage-draft").value == "子页草稿", "evicted settings subpage rebuilt with draft");
            typeof(GameMainUI).GetMethod("RequestSystemBack", instance).Invoke(host, null);
            Assert(settings.Root.Q<TextField>("review-subpage-draft") == null &&
                   settings.Root.Q<TextField>("settings-compact-search").value == "声音", "back returns to prior app page");

            TabletSettingsAppUI.ShowTipsPage(null);
            var pages = (System.Collections.IList)typeof(TabletTaskSession).GetField("Pages", instance).GetValue(settings);
            Assert(pages.Count == 1, "real tips subpage opens");
            var term = settings.Root.Q<Label>("tips-term-rail_ballast");
            Assert(term != null, "glossary term exists");
            bool clickDelivered = false;
            term.RegisterCallback<ClickEvent>(_ => clickDelivered = true);
            using (ClickEvent click = ClickEvent.GetPooled()) { click.target = term; term.SendEvent(click); }
            Debug.Log($"TABLET_TIPS_CLICK attached={term.panel != null} delivered={clickDelivered} title={settings.Root.Q<Label>("tips-detail-title")?.text}");
            Assert(settings.Root.Q<Label>("tips-detail-title")?.text == "道砟", "glossary selection opens");
            launch.Invoke(host, new object[] { "验收占位" });
            typeof(GameMainUI).GetMethod("ReclaimInactiveTabletTasks", instance).Invoke(host, new object[] { false });
            Assert(settings.IsSuspended, "real tips subpage evicted");
            activate.Invoke(host, new object[] { settings });
            Assert(!settings.IsSuspended && pages.Count == 1 &&
                   settings.Root.Q<Label>("tips-detail-title")?.text == "道砟", "real tips selection rebuilt");
            typeof(GameMainUI).GetMethod("RequestSystemBack", instance).Invoke(host, null);
            Assert(pages.Count == 0, "real tips back returns to settings");

            host.BuildInnerPanel("不可重建子页");
            launch.Invoke(host, new object[] { "验收占位" });
            typeof(GameMainUI).GetMethod("ReclaimInactiveTabletTasks", instance).Invoke(host, new object[] { false });
            Assert(!settings.IsSuspended, "unrebuildable page is not evicted");
            activate.Invoke(host, new object[] { settings });

            var gesture = (TabletEdgeGesture)typeof(GameMainUI).GetField("tabletEdgeGesture", instance).GetValue(host);
            Assert(gesture.Begin(21, new Vector2(500, 990), 1000, 1000, 44, false), "home gesture begin");
            gesture.Move(21, new Vector2(500, 900), 1f);
            typeof(GameMainUI).GetMethod("ReturnToTabletHome", instance).Invoke(host, null);
            Assert(gesture.Tick(2f) == TabletGestureAction.None, "home clears pending gesture");
            Assert(find.Invoke(host, new object[] { "设置" }) != null, "home keeps task");
            typeof(GameMainUI).GetMethod("CloseTabletTask", instance).Invoke(host, new object[] { "设置" });
            Assert(find.Invoke(host, new object[] { "设置" }) == null, "close removes task");
            Assert(lifecycle.Foreground > 0 && lifecycle.Background > 0 && lifecycle.Reclaimed > 0 &&
                   lifecycle.Restored > 0 && lifecycle.Closed == 1, "task lifecycle callbacks connected");

            typeof(GameMainUI).GetField("desktopBuilt", instance).SetValue(host, true);
            GameMainUI.OpenTipsPage("rail_ballast");
            var linkedSettings = (TabletTaskSession)find.Invoke(host, new object[] { "设置" });
            Assert(linkedSettings != null && linkedSettings.Root.Q<Label>("tips-detail-title")?.text == "道砟",
                   "glossary deep link opens a task and selected term");
            typeof(GameMainUI).GetMethod("ReturnToTabletHome", instance).Invoke(host, null);
            typeof(GameMainUI).GetMethod("CloseTabletTask", instance).Invoke(host, new object[] { "设置" });
            typeof(GameMainUI).GetMethod("ShowSuiyueDialogue", instance).Invoke(host, null);
            var sui = (TabletTaskSession)find.Invoke(host, new object[] { "岁月 · AI对话" });
            Assert(sui != null && sui.Root != null,
                   "desktop dialogue opens its own task");
            object dialogueState = typeof(TabletTaskSession).GetProperty("AppState", instance).GetValue(sui);
            VisualElement chat = sui.Root.Q("suiyue-chat-area");
            typeof(GameMainUI).GetMethod("AppendSuiyueLine", instance).Invoke(host,
                new object[] { dialogueState, chat, "林彪悍", "验收消息", Color.white });
            launch.Invoke(host, new object[] { "验收占位" });
            typeof(GameMainUI).GetMethod("ReclaimInactiveTabletTasks", instance).Invoke(host, new object[] { false });
            Assert(sui.IsSuspended, "dialogue view evicted");
            activate.Invoke(host, new object[] { sui });
            Assert(sui.Root.Q("suiyue-chat-area").Query<Label>("suiyue-message").ToList()
                .Exists(label => label.text == "验收消息"), "dialogue history survives view eviction");
            typeof(GameMainUI).GetMethod("ReturnToTabletHome", instance).Invoke(host, null);
            typeof(GameMainUI).GetMethod("CloseTabletTask", instance).Invoke(host, new object[] { "岁月 · AI对话" });
            typeof(GameMainUI).GetMethod("ShowSuiyueDialogue", instance).Invoke(host, null);
            var reopened = (TabletTaskSession)find.Invoke(host, new object[] { "岁月 · AI对话" });
            Assert(!reopened.Root.Q("suiyue-chat-area").Query<Label>("suiyue-message").ToList()
                .Exists(label => label.text == "验收消息"), "closing task clears dialogue session");

            launch.Invoke(host, new object[] { "计算器" });
            var calculator = (TabletTaskSession)find.Invoke(host, new object[] { "计算器" });
            var engine = calculator.Root.Q("calc-card").userData as TabletSystemToolsAppUI.TabletCalculatorEngine;
            engine.InputDigit('1'); engine.InputDigit('2'); engine.InputOperator('+'); engine.InputDigit('3');
            string beforeDisplay = engine.CurrentDisplay, beforeExpression = engine.ExpressionText;
            launch.Invoke(host, new object[] { "验收占位" });
            typeof(GameMainUI).GetMethod("ReclaimInactiveTabletTasks", instance).Invoke(host, new object[] { true });
            Assert(calculator.IsSuspended, "calculator view reclaimed under critical pressure");
            activate.Invoke(host, new object[] { calculator });
            Assert(ReferenceEquals(calculator.Root.Q("calc-card").userData, engine), "calculator retained engine reused");
            Assert(calculator.Root.Q<Label>("calc-display-main").text == beforeDisplay &&
                   calculator.Root.Q<Label>("calc-display-expression").text == beforeExpression, "calculator display restored");
            engine.InputEquals();
            Assert(engine.CurrentDisplay == "15" && calculator.Root.Q<Label>("calc-display-main").text == "15",
                   "pending calculator operation and rebuilt event subscription survive reclamation");
            typeof(GameMainUI).GetMethod("CloseTabletTask", instance).Invoke(host, new object[] { "计算器" });
            launch.Invoke(host, new object[] { "计算器" });
            calculator = (TabletTaskSession)find.Invoke(host, new object[] { "计算器" });
            Assert(calculator.Root.Q<Label>("calc-display-main").text == "0", "closed calculator starts a fresh task");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            window.Close();
            instanceProperty.GetSetMethod(true).Invoke(null, new[] { previousInstance });
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
