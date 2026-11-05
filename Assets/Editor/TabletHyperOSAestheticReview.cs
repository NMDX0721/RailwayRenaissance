using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Account;
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
        private static string settleName;
        private static int settleNext;
        private static int stage;
        private static bool reviewRegisteredAccount;
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

        // 截图延迟到下一次 Step（已等待 20 帧渲染）再读取，否则读到的是上一阶段的画面。
        private static string pendingCapture;
        private static int settingsSubPage;
        private static bool wizardDone, wizardRegistered;
        private static string wizardSms;
        private static string savedProfileJson, savedSetupDone, savedLegacyName;
        private static bool accountPrefsSaved;

        // 审查会在编辑器的 PlayerPrefs 里创建测试账号：先备份，结束时恢复，避免污染开发者自己的账号数据
        private static void SaveAccountPrefs()
        {
            if (accountPrefsSaved) return;
            accountPrefsSaved = true;
            savedProfileJson = PlayerPrefs.HasKey("XiaomiAccountProfile_v1") ? PlayerPrefs.GetString("XiaomiAccountProfile_v1") : null;
            savedSetupDone = PlayerPrefs.HasKey("TabletSetupDone_v1") ? PlayerPrefs.GetInt("TabletSetupDone_v1").ToString() : null;
            savedLegacyName = PlayerPrefs.HasKey("XiaomiAccount") ? PlayerPrefs.GetString("XiaomiAccount") : null;
        }

        private static void RestoreAccountPrefs()
        {
            if (!accountPrefsSaved) return;
            accountPrefsSaved = false;
            if (savedProfileJson != null) PlayerPrefs.SetString("XiaomiAccountProfile_v1", savedProfileJson); else PlayerPrefs.DeleteKey("XiaomiAccountProfile_v1");
            if (savedSetupDone != null) PlayerPrefs.SetInt("TabletSetupDone_v1", int.Parse(savedSetupDone)); else PlayerPrefs.DeleteKey("TabletSetupDone_v1");
            if (savedLegacyName != null) PlayerPrefs.SetString("XiaomiAccount", savedLegacyName); else PlayerPrefs.DeleteKey("XiaomiAccount");
            PlayerPrefs.Save();
            AccountService.ReloadFromPrefs();
        }

        private static void ClickButtonText(string text)
        {
            Button found = null;
            root.Query<Button>().ForEach(b => { if (found == null && b.text == text && b.resolvedStyle.display != DisplayStyle.None) found = b; });
            Assert(found != null, "wizard button missing: " + text);
            ClickElement(found);
        }

        private static void ClickRowContaining(string text)
        {
            VisualElement row = null;
            root.Query<Label>().ForEach(l => { if (row == null && l.text != null && l.text.Contains(text)) row = l.parent; });
            Assert(row != null, "wizard row missing: " + text);
            ClickElement(row);
        }

        private static TextField WizardField(string name)
        {
            var f = root.Q<TextField>(name);
            Assert(f != null, "wizard field missing: " + name);
            return f;
        }
        private static readonly string[] SettingsPagesToCapture = { "当前账号", "当前账号#登录", "当前账号#云空间", "当前账号#备份", "当前账号#退出", "显示与亮度", "电池", "我的设备", "我的设备#存储", "小米超级AI", "通知与状态栏", "通知与状态栏#通知管理", "桌面", "桌面#系统导航", "桌面#手势演示", "桌面#按钮导航", "桌面#恢复手势", "声音与振动", "人脸与锁屏密码", "隐私与安全", "健康使用设备", "儿童模式", "无障碍", "更多设置", "反馈", "开发者选项" };

        private static void Capture(string name)
        {
            FlushPendingCapture();
            pendingCapture = name;
        }

        private static void FlushPendingCapture()
        {
            if (pendingCapture == null) return;
            string name = pendingCapture;
            pendingCapture = null;
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
            FlushPendingCapture();

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
                        settings.clearColor = true;
                        settings.colorClearValue = new Color(0.06f, 0.07f, 0.06f);
                        document.panelSettings = settings;

                        Resize(2560, 1440);
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

                        // 日期淡出、岛浮现要约 0.4 秒：等落定再截图
                        settleName = "01_super_island_music_compact"; settleNext = stage + 1; nextCheck = EditorApplication.timeSinceStartup + 0.9; stage = 900;
                        frame = Time.frameCount;
                        break;

                    case 900:
                        Capture(settleName);
                        stage = settleNext;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 2: 展开车载音乐大岛 (Expanded Island) ====================
                    case 2:
                        ui.SuperIsland.SetExpanded(true);
                        var expanded = root.Q("super-island-expanded");
                        Assert(expanded != null, "super-island-expanded should exist");
                        Assert(expanded.style.display == DisplayStyle.Flex, "Expanded island must be displayed");

                        // 展开是 340ms 的变形动画：等它落定再截图，看到的才是最终形态
                        settleName = "02_super_island_music_expanded"; settleNext = stage + 1; nextCheck = EditorApplication.timeSinceStartup + 0.9; stage = 900;
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

                        // 展开是 340ms 的变形动画：等它落定再截图，看到的才是最终形态
                        settleName = "04_super_island_timer_expanded"; settleNext = stage + 1; nextCheck = EditorApplication.timeSinceStartup + 0.9; stage = 900;
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

                        // 展开是 340ms 的变形动画：等它落定再截图，看到的才是最终形态
                        settleName = "05_super_island_cab_expanded"; settleNext = stage + 1; nextCheck = EditorApplication.timeSinceStartup + 0.9; stage = 900;
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

                        // 本阶段重复进入：先截账号页，再依次点开“显示与亮度 / 电池 / 我的设备”各截一张
                        if (settingsSubPage == 0)
                        {
                            // 开发者选项入口只在解锁后出现；审查结束时会移除该键
                            PlayerPrefs.SetInt("TabletDevModeUnlocked", 1);
                            PlayerPrefs.Save();
                            TabletSettingsAppUI.RefreshCategoryList();
                            Capture("09_settings_inset_grouped_cards");
                            settingsSubPage = 1;
                            frame = Time.frameCount;
                            break;
                        }
                        if (settingsSubPage <= SettingsPagesToCapture.Length)
                        {
                            string pageName = SettingsPagesToCapture[settingsSubPage - 1];
                            if (pageName == "当前账号")
                            {
                                // 账号页是默认页，导航栏里是头像卡而不是文字项，直接截图
                                Capture("09_" + (char)('a' + settingsSubPage - 1) + "_settings_account");
                                settingsSubPage++;
                                frame = Time.frameCount;
                                break;
                            }
                            if (pageName.EndsWith("#通知管理"))
                            {
                                var chCard = root.Q("settings-notification-channels-card");
                                Assert(chCard != null, "settings-notification-channels-card missing");
                                (root.Q("settings-right-pane") as ScrollView)?.ScrollTo(chCard);
                                Capture("09_" + (char)('a' + settingsSubPage - 1) + "_settings_notification_channels");
                                settingsSubPage++;
                                frame = Time.frameCount;
                                break;
                            }
                            if (pageName == "桌面#系统导航")
                            {
                                var navRow = root.Q("settings-home-system-nav-row");
                                Assert(navRow != null, "settings-home-system-nav-row missing");
                                ClickElement(navRow);
                                Capture("09_" + (char)('a' + settingsSubPage - 1) + "_settings_system_navigation_gestures");
                                settingsSubPage++;
                                frame = Time.frameCount;
                                break;
                            }
                            if (pageName == "桌面#手势演示")
                            {
                                var demos = root.Q("settings-nav-gesture-demos");
                                Assert(demos != null, "settings-nav-gesture-demos missing");
                                (root.Q("settings-right-pane") as ScrollView)?.ScrollTo(demos);
                                Capture("09_" + (char)('a' + settingsSubPage - 1) + "_settings_system_navigation_demos");
                                settingsSubPage++;
                                frame = Time.frameCount;
                                break;
                            }
                            if (pageName == "桌面#按钮导航" || pageName == "桌面#恢复手势")
                            {
                                bool toButtons = pageName == "桌面#按钮导航";
                                var pick = root.Q(toButtons ? "settings-nav-preview-buttons" : "settings-nav-preview-gestures");
                                Assert(pick != null, "nav preview missing: " + pageName);
                                ClickElement(pick);
                                if (toButtons) Capture("09_" + (char)('a' + settingsSubPage - 1) + "_settings_system_navigation_buttons");
                                settingsSubPage++;
                                frame = Time.frameCount;
                                break;
                            }
                            if (pageName == "我的设备#存储")
                            {
                                TabletSettingsAppUI.ShowCategory(98);
                                Capture("09_" + (char)('a' + settingsSubPage - 1) + "_settings_storage");
                                settingsSubPage++;
                                frame = Time.frameCount;
                                break;
                            }
                            if (pageName == "当前账号#登录")
                            {
                                // 云空间需要登录：审查里临时注册一个账号，结束时（#退出）清掉
                                if (!RailwayRenaissance.UI.Tablet.Account.AccountService.IsRegistered)
                                {
                                    string regErr = RailwayRenaissance.UI.Tablet.Account.AccountService.Register("13800138000", "Passw0rd123", "测试员", 0);
                                    Assert(regErr == null, "review register failed: " + regErr);
                                    reviewRegisteredAccount = true;
                                }
                                TabletSettingsAppUI.RefreshCategoryList();
                                TabletSettingsAppUI.RefreshCurrentPage();
                                Capture("09_" + (char)('a' + settingsSubPage - 1) + "_settings_account_signed_in");
                                settingsSubPage++;
                                frame = Time.frameCount;
                                break;
                            }
                            if (pageName == "当前账号#退出")
                            {
                                if (reviewRegisteredAccount)
                                {
                                    TabletStorageLedger.ClearCloudSyncForCurrentAccount();
                                    RailwayRenaissance.UI.Tablet.Account.AccountService.ResetForTests();
                                    PlayerPrefs.Save();
                                    reviewRegisteredAccount = false;
                                }
                                TabletSettingsAppUI.RefreshCategoryList();
                                settingsSubPage++;
                                frame = Time.frameCount;
                                break;
                            }
                            if (pageName.EndsWith("#云空间"))
                            {
                                var cloudTop = root.Q("account-cloud-card");
                                Assert(cloudTop != null, "account-cloud-card missing");
                                ClickElement(cloudTop);
                                Capture("09_" + (char)('a' + settingsSubPage - 1) + "_settings_cloud_top");
                                settingsSubPage++;
                                frame = Time.frameCount;
                                break;
                            }
                            if (pageName.EndsWith("#备份"))
                            {
                                
                                // 点云空间卡片进入云空间子页，再点“备份”行截取同步中状态
                                var backupRow = root.Q("settings-cloud-backup-row");
                                Assert(backupRow != null, "settings-cloud-backup-row missing");
                                (root.Q("settings-right-pane") as ScrollView)?.ScrollTo(backupRow);
                                ClickElement(backupRow);
                                Capture("09_" + (char)('a' + settingsSubPage - 1) + "_settings_cloud_syncing");
                                settingsSubPage++;
                                frame = Time.frameCount;
                                break;
                            }
                            Button navButton = null;
                            root.Q("settings-category-rail").Query<Label>().ForEach(l =>
                            {
                                if (navButton == null && l.text == pageName) navButton = l.parent as Button;
                            });
                            Assert(navButton != null, "settings nav item missing: " + pageName);
                            ClickElement(navButton);
                            Capture("09_" + (char)('a' + settingsSubPage - 1) + "_settings_" + pageName);
                            settingsSubPage++;
                            frame = Time.frameCount;
                            break;
                        }
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
                        stage++;
                        frame = Time.frameCount;
                        break;

                    // ==================== Stage 16–20: 高对比度、减少动效与竖屏窄屏 ====================
                    case 16:
                        presentation.SetReduceMotion(true);   // 截图时关闭动画，避免截到淡入中途
                        presentation.SetHighContrast(true);
                        state.ClosePanel();
                        ui.OpenSystemPanel(TabletSystemPanel.Controls);
                        Capture("16_high_contrast_controls");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    case 17:
                        state.ClosePanel();
                        state.Post("应用提示", "闹钟已同步到先民排班系统");
                        state.Post("铁路广播", "雾峰村线临时限速 40 km/h，请注意行车");
                        ui.OpenSystemPanel(TabletSystemPanel.Notifications);
                        Capture("17_high_contrast_notifications");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    case 18:
                        presentation.SetHighContrast(false);
                        presentation.SetReduceMotion(true);
                        state.ClosePanel();
                        ui.OpenSystemPanel(TabletSystemPanel.Controls);
                        Capture("18_reduce_motion_controls");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    case 19:
                    {
                        // 减少动效开启时面板必须直接到位：不透明、无位移，不能停在动画起点
                        var motionPanel = root.Q("tablet-shell-panel");
                        Assert(motionPanel != null, "tablet-shell-panel missing under reduce motion");
                        Assert(motionPanel.resolvedStyle.opacity > 0.99f, "Panel must be fully opaque with reduce motion on");
                        Assert(Mathf.Abs(motionPanel.resolvedStyle.translate.y) < 0.5f, "Panel must not be offset with reduce motion on");
                        Resize(800, 1280); // 竖屏窄屏（仍保持减少动效，保证截图时面板已完整显示）
                        state.ClosePanel();
                        ui.OpenSystemPanel(TabletSystemPanel.Notifications);
                        Capture("19_portrait_notifications");
                        stage++;
                        frame = Time.frameCount;
                        break;
                    }

                    case 20:
                        state.ClosePanel();
                        ui.OpenSystemPanel(TabletSystemPanel.Controls);
                        Capture("20_portrait_controls");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    case 21:
                        PlayerPrefs.DeleteKey("TabletDevModeUnlocked");
                        PlayerPrefs.Save();
                        state.ClosePanel();
                        presentation.SetReduceMotion(false);
                        Resize(2560, 1440);
                        // ---- 注册向导全流程：开机激活 -> 协议 -> 网络 -> 注册 -> 验证码 -> 密码 -> 资料 -> 完成 ----
                        SaveAccountPrefs();
                        AccountService.ResetForTests();
                        wizardDone = false; wizardRegistered = false; wizardSms = null;
                        TabletSetupWizard.Show(root, TabletSetupWizard.Flow.Activation, true,
                            reg => { wizardRegistered = reg; wizardDone = true; },
                            (t, body) => wizardSms = body);
                        // 第一页允许跳过时，返回键位置是“取消”，系统返回会取消向导而不是落到下面的应用
                        Assert(TabletSetupWizard.Active != null, "Wizard must register itself as active");
                        var cancelBtn = root.Q<Button>("wizard-back");
                        Assert(cancelBtn != null && cancelBtn.text == "取消" && cancelBtn.resolvedStyle.visibility == Visibility.Visible, "First page must offer a cancel button when skipping is allowed");
                        Capture("21_wizard_welcome");
                        stage++;
                        frame = Time.frameCount;
                        break;

                    case 22:
                        ClickButtonText("开始");
                        Capture("22_wizard_agreement");
                        stage++; frame = Time.frameCount; break;

                    case 23:
                        ClickRowContaining("我已阅读并同意《小米用户协议》");
                        ClickButtonText("同意并继续");
                        Capture("23_wizard_network");
                        stage++; frame = Time.frameCount; break;

                    case 24:
                        ClickButtonText("下一步");
                        Capture("24_wizard_account");
                        stage++; frame = Time.frameCount; break;

                    case 25:
                        ClickButtonText("注册小米账号");
                        Capture("25_wizard_phone");
                        stage++; frame = Time.frameCount; break;

                    case 26:
                    {
                        // 先不填手机号就点获取验证码：必须报错并留在本页
                        ClickRowContaining("同意《小米账号用户协议》");
                        ClickButtonText("获取验证码");
                        var errLabel = root.Q<Label>("wizard-error");
                        Assert(errLabel != null && !string.IsNullOrEmpty(errLabel.text), "Empty phone must show an error");
                        WizardField("wizard-phone").value = "13812348848";
                        ClickButtonText("获取验证码");
                        Assert(!string.IsNullOrEmpty(wizardSms), "SMS code must be delivered through the sink");
                        Capture("26_wizard_code");
                        stage++; frame = Time.frameCount; break;
                    }

                    case 27:
                    {
                        // 错误验证码被拒绝，正确验证码通过
                        int at = wizardSms.IndexOf("是 ", StringComparison.Ordinal);
                        string code = wizardSms.Substring(at + 2, 6);
                        WizardField("wizard-code").value = code == "000000" ? "111111" : "000000";
                        ClickButtonText("下一步");
                        var errLabel = root.Q<Label>("wizard-error");
                        Assert(errLabel != null && errLabel.text.Contains("验证码"), "Wrong code must be rejected");
                        WizardField("wizard-code").value = code;
                        ClickButtonText("下一步");
                        Capture("27_wizard_password");
                        stage++; frame = Time.frameCount; break;
                    }

                    case 28:
                    {
                        // 验证码通过后从密码页退回：显示“手机号已验证”，不必再要新验证码就能回到密码页
                        ClickButtonText("‹ 返回");
                        bool sawVerified = false;
                        root.Query<Label>().ForEach(l => { if (l.text == "手机号已验证") sawVerified = true; });
                        Assert(sawVerified, "Going back to the code page after a successful check must show the verified state");
                        ClickButtonText("下一步");
                        WizardField("wizard-password").value = "abcdefgh";
                        WizardField("wizard-password-confirm").value = "abcdefgh";
                        ClickButtonText("下一步");
                        var errLabel = root.Q<Label>("wizard-error");
                        Assert(errLabel != null && errLabel.text.Contains("字母和数字"), "Letters-only password must be rejected");
                        WizardField("wizard-password").value = "Railway2053";
                        WizardField("wizard-password-confirm").value = "Railway2053";
                        Capture("28_wizard_password_filled");
                        stage++; frame = Time.frameCount; break;
                    }

                    case 29:
                        ClickButtonText("下一步");
                        Capture("29_wizard_profile");
                        stage++; frame = Time.frameCount; break;

                    case 30:
                        WizardField("wizard-nickname").value = "林彪悍";
                        ClickButtonText("完成注册");
                        Assert(AccountService.IsRegistered, "Account must exist after finishing the profile step");
                        Capture("30_wizard_done");
                        stage++; frame = Time.frameCount; break;

                    case 31:
                    {
                        var prof = AccountService.Profile;
                        Assert(prof != null && prof.nickname == "林彪悍" && prof.xiaomiId.Length == 10, "Profile data mismatch");
                        Assert(!prof.passwordHash.Contains("Railway"), "Password must be stored hashed");
                        Assert(AccountService.VerifyPassword("Railway2053") && !AccountService.VerifyPassword("railway2053"), "Password verification failed");
                        ClickButtonText("开始使用");
                        Assert(wizardDone && wizardRegistered, "Wizard must report success on finish");
                        Assert(AccountService.ChangePassword("wrong", "Another2054") == "当前密码不正确", "Wrong old password must be rejected");
                        Assert(AccountService.ChangePassword("Railway2053", "Railway2053") != null, "Same password must be rejected");
                        Assert(AccountService.ChangePassword("Railway2053", "Another2054") == null, "Valid password change failed");
                        Assert(AccountService.VerifyPassword("Another2054") && !AccountService.VerifyPassword("Railway2053"), "Old password must stop working");
                        Assert(AccountService.Rename("") != null && AccountService.Rename("旅人乙") == null && AccountService.DisplayName == "旅人乙", "Rename failed");
                        string resetCode = AccountService.IssueCode(prof.phone);
                        Assert(AccountService.VerifyCode(prof.phone, resetCode) == null, "Reset code must verify");
                        Assert(AccountService.ResetPassword("Reset2055") == null && AccountService.VerifyPassword("Reset2055"), "Reset password failed");
                        Call("ReturnToTabletHome");
                        stage++; frame = Time.frameCount;
                        break;
                    }

                    // ---- 桌面文件夹：缩略图 + 展开态 ----
                    case 32:
                        Call("OpenRailwayFolder");
                        Capture("32_folder_opened");
                        stage++; frame = Time.frameCount; break;

                    case 33:
                        Call("CloseRailwayFolder");
                        Capture("33_home_folder_thumbnail");
                        stage++; frame = Time.frameCount; break;

                    // ---- 状态栏字色随前台应用：浅色应用深色字，音乐（深色窗口）浅色字，复用已有任务时也要重设 ----
                    case 34:
                    {
                        var clock = (Label)Field("statusClock");
                        Call("ReturnToTabletHome");
                        Call("LaunchApp", "设置");
                        Assert(clock.style.color.value.r < 0.4f, "Light app must use dark status text");
                        Call("ReturnToTabletHome");
                        Call("LaunchApp", "音乐");
                        Assert(clock.style.color.value.r > 0.6f, "Music window must use light status text");
                        Call("ReturnToTabletHome");
                        Call("LaunchApp", "设置"); // 复用已有的设置任务
                        Assert(clock.style.color.value.r < 0.4f, "Reactivated light app must restore dark status text");
                        stage++; frame = Time.frameCount;
                        break;
                    }

                    // ---- 桌面按屏幕比例排布：4:3 与超宽屏都不能撑出屏外或留大空白 ----
                    case 35:
                        Call("ReturnToTabletHome");
                        Resize(1440, 1080);
                        Capture("35_home_4x3");
                        stage++; frame = Time.frameCount; break;

                    case 36:
                        Resize(2560, 1080);
                        Capture("36_home_ultrawide");
                        stage++; frame = Time.frameCount; break;

                    case 37:
                        Resize(2560, 1440);
                        Teardown(true, "All stages completed successfully");
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
            if (success) FlushPendingCapture();
            RestoreAccountPrefs();
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
