using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet;
using RailwayRenaissance.UI.Tablet.SystemShell;
using RailwayRenaissance.UI.Tablet.Apps;

namespace RailwayRenaissance.Editor
{
    [InitializeOnLoad]
    public static class TabletHyperOSAestheticReview
    {
        private const string PendingKey = "TabletHyperOSAestheticReview.Pending";
        private const string OutputDir = "tools/compile/tablet_hyperos_aesthetic_v1";
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

        static TabletHyperOSAestheticReview()
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
                throw new InvalidOperationException($"[TabletHyperOSAestheticReview] Assertion failed: {message}");
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
                UnityEngine.Object.DestroyImmediate(target);
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
            UnityEngine.Object.DestroyImmediate(image);
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

        [MenuItem("Tools/Tablet/Run HyperOS Aesthetic Review")]
        public static void Batch()
        {
            Directory.CreateDirectory(OutputDir);

            string reviewDir = Path.GetFullPath("Library/TabletHyperOSReview/" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(reviewDir);
            reviewPresentationPath = Path.Combine(reviewDir, "presentation.json");
            reviewNotificationPath = Path.Combine(reviewDir, "notifications.json");

            SessionState.SetString("TabletHyperOSReview.PresentationPath", reviewPresentationPath);
            SessionState.SetString("TabletHyperOSReview.NotificationPath", reviewNotificationPath);
            SessionState.SetBool(PendingKey, true);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void StartChecking()
        {
            stage = 0;
            frame = Time.frameCount;
            deadline = EditorApplication.timeSinceStartup + 90.0;
            nextCheck = 0;
            EditorApplication.update += Step;
        }

        private static void Step()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 0.05;

            if (EditorApplication.timeSinceStartup > deadline)
            {
                Teardown(false, $"Timed out at stage {stage}, frame {frame}");
                return;
            }

            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (stage > 0 && Time.frameCount - frame < 20) return;

            try
            {
                switch (stage)
                {
                    // ==================== Stage 0: 初始化桌面与超级岛 ====================
                    case 0:
                        reviewPresentationPath = SessionState.GetString("TabletHyperOSReview.PresentationPath", "");
                        reviewNotificationPath = SessionState.GetString("TabletHyperOSReview.NotificationPath", "");
                        typeof(GameMainUI).GetField("PresentationReviewStorage", BindingFlags.Static | BindingFlags.NonPublic)
                            ?.SetValue(null, reviewPresentationPath);
                        typeof(GameMainUI).GetField("NotificationReviewStorage", BindingFlags.Static | BindingFlags.NonPublic)
                            ?.SetValue(null, reviewNotificationPath);

                        GameMainUI.Show();
                        ui = GameMainUI.Instance;
                        Assert(ui != null, "GameMainUI.Show failed");
                        Call("EnsureDesktopBuiltSync");
                        document = (UIDocument)Field("uiDoc");
                        root = document.rootVisualElement;

                        var settings = UnityEngine.Object.Instantiate(document.panelSettings);
                        settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                        settings.scale = 1;
                        settings.clearColor = true;
                        settings.colorClearValue = new Color(0.06f, 0.07f, 0.06f);
                        document.panelSettings = settings;

                        Resize(1920, 1080);
                        state = (TabletShellState)Field("tabletShellState");
                        presentation = ui.PresentationPreferences;
                        GameData.MasterVolume = 1f;
                        Call("Unlock");

                        Assert(ui.SuperIsland != null, "SuperIsland instance must be non-null");
                        Capture("00_desktop_init");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 1: 车载音乐微胶囊 (Compact Pill) ====================
                    case 1:
                        TabletMusicAppUI.tabletIsPlaying = true;
                        TabletMusicAppUI.tabletCurrentTrackTitle = "归乡 · 晨曦微光";
                        ui.SuperIsland.RegisterSession(new TabletSuperIsland.IslandSession
                        {
                            AppType = TabletSuperIsland.IslandAppType.Music,
                            Priority = 20,
                            Title = "归乡 · 晨曦微光",
                            Subtitle = "车载随身听 · 正在播放",
                            Metric = "🎵 播放中",
                            AccentColor = TabletUIHelper.AccentSystem
                        });
                        ui.SuperIsland.Tick();

                        var pill = root.Q("super-island-pill");
                        Assert(pill != null, "super-island-pill element should exist");
                        Assert(pill.style.display == DisplayStyle.Flex, "super-island-pill must be visible when session is active");

                        Capture("01_super_island_music_compact");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 2: 展开车载音乐大岛 (Expanded Island) ====================
                    case 2:
                        ui.SuperIsland.SetExpanded(true);
                        var expanded = root.Q("super-island-expanded");
                        Assert(expanded != null, "super-island-expanded should exist");
                        Assert(expanded.style.display == DisplayStyle.Flex, "Expanded island must be displayed");

                        Capture("02_super_island_music_expanded");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 3: 双岛分化态 (Dual-Island Capsules) ====================
                    case 3:
                        ui.SuperIsland.SetExpanded(false);

                        // 注册计时器（优先级 60 > 音乐 20），形成主岛秒表 + 副岛音乐小气泡
                        ui.SuperIsland.RegisterSession(new TabletSuperIsland.IslandSession
                        {
                            AppType = TabletSuperIsland.IslandAppType.Timer,
                            Priority = 60,
                            Title = "区间行车测时秒表",
                            Subtitle = "雾峰至茶山单程区间时分",
                            Metric = "04:59",
                            AccentColor = new Color(0.35f, 0.75f, 1f, 1f)
                        });

                        var subBubble = root.Q("super-island-sub-bubble");
                        Assert(subBubble != null, "super-island-sub-bubble should exist");
                        Assert(subBubble.style.display == DisplayStyle.Flex, "Sub-bubble must be displayed in multi-app mode");

                        Capture("03_super_island_dual_capsules");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 4: 展开计时器大岛 ====================
                    case 4:
                        ui.SuperIsland.SetExpanded(true);
                        Assert(ui.SuperIsland.PrimaryType == TabletSuperIsland.IslandAppType.Timer, "Primary island should be Timer");

                        Capture("04_super_island_timer_expanded");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 5: 列控驾驶遥测大岛 (Train Cab ATO) ====================
                    case 5:
                        ui.SuperIsland.SetExpanded(false);
                        ui.SuperIsland.RegisterSession(new TabletSuperIsland.IslandSession
                        {
                            AppType = TabletSuperIsland.IslandAppType.TrainCab,
                            Priority = 90,
                            Title = "NF-5K 机车遥测",
                            AuxValue = 78.4f,
                            Metric = "78 km/h",
                            Subtitle = "限速 80 km/h · 自动巡航中",
                            Progress = 0.78f,
                            AccentColor = new Color(0.96f, 0.72f, 0.20f, 1f)
                        });

                        ui.SuperIsland.SetExpanded(true);
                        Assert(ui.SuperIsland.PrimaryType == TabletSuperIsland.IslandAppType.TrainCab, "Primary island should be TrainCab");

                        Capture("05_super_island_cab_expanded");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 6: 展开澎湃控制中心 (440px 亚克力面板) ====================
                    case 6:
                        ui.SuperIsland.SetExpanded(false);
                        ui.SuperIsland.ClearAllSessions();

                        ui.OpenSystemPanel(TabletSystemPanel.Controls);
                        var panel = root.Q("tablet-shell-panel");
                        Assert(panel != null, "tablet-shell-panel should exist");

                        // 验证面板宽度与圆角
                        Assert((panel.style.borderTopLeftRadius.value.value >= 18f || panel.resolvedStyle.borderTopLeftRadius >= 18f), "Control panel radius should be modern 20-24px");

                        Capture("06_control_center_hyperos_full");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 7: 控制中心交互 (双胶囊大卡、双纵向滑块、播控微件) ====================
                    case 7:
                        var quietBtn = root.Q<Button>("tablet-quiet-toggle");
                        var hcBtn = root.Q<Button>("tablet-high-contrast-toggle");
                        var dimBtn = root.Q<Button>("tablet-wallpaper-dim-toggle");
                        var muteBtn = root.Q<Button>("tablet-audio-mute-toggle");
                        var mediaCard = root.Q("tablet-media-card");
                        var offlineBox = root.Q("settings-hardware-offline-box");

                        Assert(quietBtn != null, "quiet toggle pill card should exist");
                        Assert(hcBtn != null, "high contrast pill card should exist");
                        Assert(dimBtn != null, "dim vertical pill slider should exist");
                        Assert(muteBtn != null, "mute vertical pill slider should exist");
                        Assert(mediaCard != null, "media player card should exist");
                        Assert(offlineBox != null, "hardware offline box should exist");

                        // 触发勿扰模式激活
                        ClickElement(quietBtn);
                        Assert(state.QuietMode, "Quiet mode should be toggled on");

                        Capture("07_control_center_active_interactions");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 8: 设置应用 Squircle 彩色图标分类 ====================
                    case 8:
                        state.ClosePanel();
                        Call("LaunchApp", "设置");

                        var categoryRail = root.Q("settings-category-rail");
                        Assert(categoryRail != null, "settings-category-rail should exist");

                        Capture("08_settings_squircle_sidebar");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 9: Inset Grouped 卡片与发丝级细分界线 ====================
                    case 9:
                        var rightPane = root.Q("settings-right-pane");
                        Assert(rightPane != null, "settings-right-pane should exist");

                        Capture("09_settings_inset_grouped_cards");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 10: 媒姬百科 Master-Detail 图鉴网格 ====================
                    case 10:
                        Call("LaunchApp", "媒姬百科");
                        var wikiGrid = root.Q("wiki-grid-container");
                        Assert(wikiGrid != null, "wiki-grid-container should exist in Wiki App");

                        Capture("10_app_wiki_knowledge_grid");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 11: 气象监视 Hero 与水文量规卡 ====================
                    case 11:
                        Call("LaunchApp", "天气");
                        Capture("11_app_weather_hero_telemetry");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 12: 机车遥测与米家 IoT 智能网关 ====================
                    case 12:
                        Call("LaunchApp", "米家");
                        Capture("12_app_mijia_locomotive_gateway");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 13: 文件管理多色分段存储条 ====================
                    case 13:
                        Call("LaunchApp", "文件管理");
                        Capture("13_app_file_manager_segmented_bar");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 14: 铁路路线双轨几何拓扑图 ====================
                    case 14:
                        Call("LaunchApp", "地图");
                        Capture("14_app_railway_route_topology");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 15: 响应式平板多比例测试 (16:10 / 4:3) ====================
                    case 15:
                        Resize(1920, 1200); // 16:10 平板比例
                        ui.OpenSystemPanel(TabletSystemPanel.Controls);
                        Capture("15_hyperos_responsive_tablet");

                        Teardown(true, "All 15 stages completed successfully");
                        break;
                }
            }
            catch (Exception ex)
            {
                Teardown(false, $"Exception at stage {stage}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void Teardown(bool success, string reason)
        {
            EditorApplication.update -= Step;
            SessionState.SetBool(PendingKey, false);

            if (target != null)
            {
                if (document != null && document.panelSettings != null)
                {
                    document.panelSettings.targetTexture = null;
                }
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                target = null;
            }

            EditorApplication.isPlaying = false;

            if (success)
            {
                Debug.Log($"[TABLET_HYPEROS_AESTHETIC_REVIEW_OK] {reason}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[TABLET_HYPEROS_AESTHETIC_REVIEW_FAIL] {reason}");
                EditorApplication.Exit(1);
            }
        }
    }
}
