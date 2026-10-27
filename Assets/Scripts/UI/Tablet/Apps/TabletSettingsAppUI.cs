using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.SystemShell;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板应用：设置（严格照截图 24 分类 + 术语图鉴 TIPS + 备份/应用管理/开发者选项）
    /// 从 GameMainUI 剥离（T306 Batch 1-C）
    /// </summary>
    public static class TabletSettingsAppUI
    {
    // ============ 设置 App：严格照截图 24 分类 + 老设置全量迁入（T298 规范）============
    public static IKeyValueStorage StorageAdapter = new PlayerPrefsStorage();
    public const string BackupJsonKey = TabletLocalBackupService.BackupJsonKey;
    public const string BackupTimeKey = TabletLocalBackupService.BackupTimeKey;

    private static int currentSettingsCategory = 1; // 默认选中 1 (当前账号) 或 13 (显示)
    private static string settingsSearchFilter = "";
    private static int accessibilityTab = 0; // 0:常规 1:视觉 2:听觉 3:肢体
    private static bool isTabletDevModeUnlocked = false;
    private static int devModeClickCount = 0;

    private static readonly (int id, string name, string icon, string group)[] SettingsCategories = {
        (1, "当前账号", "👤", "account"),
        (2, "我的设备", "📱", "device"),
        (3, "平板特色功能", "🧩", "device"),
        (4, "WLAN", "📶", "connect"),
        (5, "蓝牙", "ᛒ", "connect"),
        (6, "互联互通", "🔗", "connect"),
        (7, "更多连接方式", "🌐", "connect"),
        (8, "小米超级AI", "🔮", "system"),
        (9, "个性化", "🎨", "system"),
        (10, "锁屏", "🔒", "system"),
        (11, "通知与状态栏", "🔔", "system"),
        (12, "桌面", "🏠", "system"),
        (13, "显示与亮度", "☀", "system"),
        (14, "声音与振动", "🔊", "system"),
        (15, "触控笔与键盘", "⌨", "system"),
        (16, "人脸与锁屏密码", "😊", "security"),
        (17, "隐私与安全", "🛡", "security"),
        (18, "应用", "▦", "security"),
        (19, "电池", "🔋", "security"),
        (20, "健康使用设备", "💚", "other"),
        (21, "儿童模式", "🧒", "other"),
        (22, "无障碍", "♿", "other"),
        (23, "更多设置", "⋯", "other"),
        (24, "反馈", "💬", "feedback")
    };

    private static Action<string> s_showToast;
    private static Action<TabletSystemPanel> s_openSystemPanel;
    public static Action<KeyCode> TabletRebindAction;

    public static void SetOpenSystemPanelHandler(Action<TabletSystemPanel> handler)
    {
        s_openSystemPanel = handler;
    }

    public static void ShowToast(string msg)
    {
        if (s_showToast != null) s_showToast(msg);
        else Debug.Log("[Settings] " + msg);
    }

    private static VisualElement BuildInnerPanel(string title, Action rebuild)
    {
        if (GameMainUI.Instance != null) return GameMainUI.Instance.BuildInnerPanel(title, rebuild);
        return null;
    }

    public static void BuildSettingsPage(VisualElement win, Action<string> showToast = null, Action<TabletSystemPanel> openPanel = null)
    {
        if (showToast != null)
        {
            s_showToast = showToast;
        }
        if (openPanel != null)
        {
            s_openSystemPanel = openPanel;
        }

        win.Clear();

        var rootRow = new VisualElement();
        rootRow.name = "settings-responsive-root";
        rootRow.style.flexDirection = FlexDirection.Row;
        rootRow.style.flexGrow = 1;
        win.Add(rootRow);

        var compactToolbar = new VisualElement { name = "settings-compact-toolbar" };
        compactToolbar.style.display = DisplayStyle.None;
        compactToolbar.style.height = 48;
        compactToolbar.style.flexDirection = FlexDirection.Row;
        compactToolbar.style.alignItems = Align.Center;
        compactToolbar.style.paddingLeft = 10;
        compactToolbar.style.paddingRight = 10;
        compactToolbar.style.backgroundColor = SurfaceCard;
        var compactCategory = new DropdownField { name = "settings-compact-category" };
        compactCategory.style.width = Length.Percent(48);
        compactCategory.style.height = 40;
        compactCategory.style.marginRight = 8;
        compactToolbar.Add(compactCategory);
        var compactSearch = new TextField { name = "settings-compact-search", value = settingsSearchFilter };
        compactSearch.style.flexGrow = 1;
        compactSearch.style.height = 38;
        compactSearch.style.fontSize = 13;
        compactSearch.style.unityFontDefinition = Fd();
        compactToolbar.Add(compactSearch);
        rootRow.Add(compactToolbar);

        // 左栏：300px 纯白/极浅灰导航条，含搜索框与 24 项
        var leftNav = new VisualElement();
        leftNav.name = "settings-category-rail";
        leftNav.style.width = 300;
        leftNav.style.flexShrink = 0;
        leftNav.style.backgroundColor = SurfaceCard;
        leftNav.style.borderRightWidth = 1;
        leftNav.style.borderRightColor = BorderDivider;
        leftNav.style.flexDirection = FlexDirection.Column;
        leftNav.style.paddingLeft = 14; leftNav.style.paddingRight = 14;
        leftNav.style.paddingTop = 16; leftNav.style.paddingBottom = 16;
        rootRow.Add(leftNav);

        var titleLbl = new Label("设置");
        titleLbl.style.fontSize = 24; titleLbl.style.color = InkPrimary;
        titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLbl.style.unityFontDefinition = Fd();
        titleLbl.style.marginBottom = 12;
        leftNav.Add(titleLbl);

        // 搜索框
        var searchBox = new VisualElement();
        searchBox.style.height = 36;
        searchBox.style.backgroundColor = SurfaceApp;
        searchBox.style.borderTopLeftRadius = 18; searchBox.style.borderTopRightRadius = 18;
        searchBox.style.borderBottomLeftRadius = 18; searchBox.style.borderBottomRightRadius = 18;
        searchBox.style.flexDirection = FlexDirection.Row;
        searchBox.style.alignItems = Align.Center;
        searchBox.style.paddingLeft = 12; searchBox.style.paddingRight = 12;
        searchBox.style.marginBottom = 12;
        leftNav.Add(searchBox);

        var searchIcon = new Label("🔍");
        searchIcon.style.fontSize = 13; searchIcon.style.color = InkTertiary;
        searchIcon.style.unityFontDefinition = Fd(); searchIcon.style.marginRight = 6;
        searchBox.Add(searchIcon);

        var searchInput = new TextField { value = settingsSearchFilter };
        searchInput.style.flexGrow = 1;
        searchInput.style.borderTopWidth = 0; searchInput.style.borderBottomWidth = 0;
        searchInput.style.borderLeftWidth = 0; searchInput.style.borderRightWidth = 0;
        searchInput.style.backgroundColor = Color.clear;
        searchInput.style.fontSize = 13; searchInput.style.color = InkPrimary;
        searchInput.style.unityFontDefinition = Fd();
        searchBox.Add(searchInput);

        // 右栏：内容详情区（隐藏滚动条）
        var rightPane = new ScrollView { name = "settings-right-pane" };
        rightPane.style.flexGrow = 1;
        rightPane.style.flexShrink = 1;
        rightPane.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        rightPane.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        rightPane.style.paddingLeft = 32; rightPane.style.paddingRight = 32;
        rightPane.style.paddingTop = 20; rightPane.style.paddingBottom = 30;
        rightPane.style.backgroundColor = SurfaceApp;
        rootRow.Add(rightPane);

        // 分类滚动列表（隐藏滚动条）
        var navScroll = new ScrollView();
        navScroll.style.flexGrow = 1;
        navScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        navScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        leftNav.Add(navScroll);

        Action populateCategories = () =>
        {
            navScroll.Clear();
            string previousRenderedGroup = null;

            for (int i = 0; i < SettingsCategories.Length; i++)
            {
                var cat = SettingsCategories[i];
                if (!string.IsNullOrEmpty(settingsSearchFilter) && !cat.name.Contains(settingsSearchFilter))
                    continue;

                if (previousRenderedGroup != null && cat.group != previousRenderedGroup)
                {
                    var div = new VisualElement();
                    div.style.height = 1; div.style.backgroundColor = BorderDivider;
                    div.style.marginTop = 6; div.style.marginBottom = 6;
                    navScroll.Add(div);
                }
                previousRenderedGroup = cat.group;

                int catId = cat.id;
                bool isSelected = (catId == currentSettingsCategory);

                var itemBtn = new Button(() =>
                {
                    currentSettingsCategory = catId;
                    BuildSettingsPage(win);
                });
                itemBtn.style.height = 42;
                itemBtn.style.flexDirection = FlexDirection.Row;
                itemBtn.style.alignItems = Align.Center;
                itemBtn.style.paddingLeft = 12; itemBtn.style.paddingRight = 12;
                itemBtn.style.marginBottom = 3;
                itemBtn.style.backgroundColor = isSelected ? new Color(0.93f, 0.93f, 0.95f, 1f) : Color.clear;
                itemBtn.style.borderTopLeftRadius = 10; itemBtn.style.borderTopRightRadius = 10;
                itemBtn.style.borderBottomLeftRadius = 10; itemBtn.style.borderBottomRightRadius = 10;
                itemBtn.style.borderTopWidth = 0; itemBtn.style.borderBottomWidth = 0;
                itemBtn.style.borderLeftWidth = 0; itemBtn.style.borderRightWidth = 0;

                var icLbl = new Label(cat.icon);
                icLbl.style.fontSize = 16; icLbl.style.marginRight = 10;
                icLbl.style.unityFontDefinition = Fd();
                itemBtn.Add(icLbl);

                var nameLbl = new Label(cat.name);
                nameLbl.style.fontSize = 14;
                nameLbl.style.color = isSelected ? AccentSystem : InkPrimary;
                if (isSelected) nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameLbl.style.unityFontDefinition = Fd();
                itemBtn.Add(nameLbl);

                navScroll.Add(itemBtn);
            }

            // 若已解锁开发者模式，渲染开发者选项入口
            bool devUnlocked = isTabletDevModeUnlocked || PlayerPrefs.GetInt("TabletDevModeUnlocked", 0) == 1;
            if (devUnlocked && (string.IsNullOrEmpty(settingsSearchFilter) || "开发者选项".Contains(settingsSearchFilter)))
            {
                var devDiv = new VisualElement();
                devDiv.style.height = 1; devDiv.style.backgroundColor = BorderDivider;
                devDiv.style.marginTop = 6; devDiv.style.marginBottom = 6;
                navScroll.Add(devDiv);

                bool isDevSelected = (currentSettingsCategory == 99);
                var devBtn = new Button(() =>
                {
                    currentSettingsCategory = 99;
                    BuildSettingsPage(win);
                });
                devBtn.style.height = 42;
                devBtn.style.flexDirection = FlexDirection.Row;
                devBtn.style.alignItems = Align.Center;
                devBtn.style.paddingLeft = 12; devBtn.style.paddingRight = 12;
                devBtn.style.marginBottom = 3;
                devBtn.style.backgroundColor = isDevSelected ? new Color(0.93f, 0.93f, 0.95f, 1f) : Color.clear;
                devBtn.style.borderTopLeftRadius = 10; devBtn.style.borderTopRightRadius = 10;
                devBtn.style.borderBottomLeftRadius = 10; devBtn.style.borderBottomRightRadius = 10;
                devBtn.style.borderTopWidth = 0; devBtn.style.borderBottomWidth = 0;
                devBtn.style.borderLeftWidth = 0; devBtn.style.borderRightWidth = 0;

                var devIc = new Label("🛠️");
                devIc.style.fontSize = 16; devIc.style.marginRight = 10;
                devIc.style.unityFontDefinition = Fd();
                devBtn.Add(devIc);

                var devName = new Label("开发者选项");
                devName.style.fontSize = 14;
                devName.style.color = isDevSelected ? AccentSystem : new Color(0.85f, 0.45f, 0.15f, 1f);
                if (isDevSelected) devName.style.unityFontStyleAndWeight = FontStyle.Bold;
                devName.style.unityFontDefinition = Fd();
                devBtn.Add(devName);

                navScroll.Add(devBtn);
            }
        };

        searchInput.RegisterValueChangedCallback(evt =>
        {
            settingsSearchFilter = evt.newValue;
            populateCategories();
            compactSearch.SetValueWithoutNotify(settingsSearchFilter);
        });

        populateCategories();
        var compactChoices = new List<string>();
        for (int i = 0; i < SettingsCategories.Length; i++) compactChoices.Add(SettingsCategories[i].name);
        bool devUnlockedForCompact = isTabletDevModeUnlocked || PlayerPrefs.GetInt("TabletDevModeUnlocked", 0) == 1;
        if (devUnlockedForCompact) compactChoices.Add("开发者选项");
        compactCategory.choices = compactChoices;
        compactCategory.value = currentSettingsCategory == 99
            ? "开发者选项"
            : GetSettingsCategoryName(currentSettingsCategory);
        compactCategory.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == "开发者选项")
                currentSettingsCategory = 99;
            else
            {
                for (int i = 0; i < SettingsCategories.Length; i++)
                {
                    if (SettingsCategories[i].name == evt.newValue)
                    {
                        currentSettingsCategory = SettingsCategories[i].id;
                        break;
                    }
                }
            }
            BuildSettingsPage(win);
        });
        compactSearch.RegisterValueChangedCallback(evt =>
        {
            settingsSearchFilter = evt.newValue;
            searchInput.SetValueWithoutNotify(settingsSearchFilter);
            populateCategories();
        });

        RenderSettingsDetail(rightPane, win);
        rootRow.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            float width = evt.newRect.width;
            if (width <= 0) return;
            bool compact = width < 760;
            rootRow.style.flexDirection = compact ? FlexDirection.Column : FlexDirection.Row;
            compactToolbar.style.display = compact ? DisplayStyle.Flex : DisplayStyle.None;
            leftNav.style.display = compact ? DisplayStyle.None : DisplayStyle.Flex;
            leftNav.style.width = width < 1020 ? 248 : 300;
            rightPane.style.paddingLeft = compact ? 14 : width < 1020 ? 22 : 32;
            rightPane.style.paddingRight = compact ? 14 : width < 1020 ? 22 : 32;
            rightPane.style.paddingTop = compact ? 14 : 20;
        });
    }

    private static string GetSettingsCategoryName(int id)
    {
        for (int i = 0; i < SettingsCategories.Length; i++)
            if (SettingsCategories[i].id == id) return SettingsCategories[i].name;
        return SettingsCategories[0].name;
    }

    private static void RenderSettingsDetail(ScrollView rightPane, VisualElement win)
    {
        switch (currentSettingsCategory)
        {
            case 1: RenderAccountSettings(rightPane); break;
            case 2: RenderMyDeviceSettings(rightPane, win); break;
            case 3: RenderTabletFeaturesSettings(rightPane, win); break;
            case 4: RenderWlanSettings(rightPane); break;
            case 5: RenderBluetoothSettings(rightPane); break;
            case 6: RenderInterconnectSettings(rightPane); break;
            case 7: RenderMoreConnectionsSettings(rightPane); break;
            case 8: RenderAiSettings(rightPane); break;
            case 9: RenderPresentationSettings(rightPane); break;
            case 10: RenderLockScreenSettings(rightPane); break;
            case 11: RenderNotificationAndStatusSettings(rightPane); break;
            case 12: RenderHomeScreenSettings(rightPane); break;
            case 13: RenderDisplaySettings(rightPane, win); break;
            case 14: RenderAudioSettings(rightPane, win); break;
            case 15: RenderControlSettings(rightPane, win); break;
            case 16: RenderLockSecuritySettings(rightPane); break;
            case 17: RenderPrivacySettings(rightPane); break;
            case 18: RenderAppsSettings(rightPane); break;
            case 19: RenderBatterySettings(rightPane); break;
            case 20: RenderDigitalWellbeingSettings(rightPane, win); break;
            case 21: RenderKidsModeSettings(rightPane); break;
            case 22: RenderAccessibilitySettings(rightPane, win); break;
            case 23: RenderAdditionalSettings(rightPane); break;
            case 24: RenderFeedbackSettings(rightPane); break;
            case 99: RenderDeveloperOptions(rightPane, win); break;
            default: RenderPlaceholderCategory(rightPane, currentSettingsCategory); break;
        }
    }

    // 形态 A · 个性化页
    private static void RenderPresentationSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "平板外观");
        var prefs = GameMainUI.Instance?.PresentationPreferences;
        if (prefs == null) { rightPane.Add(new Label("请从平板桌面进入外观设置。")); return; }
        rightPane.Add(new Label("调整壁纸、解锁动效与系统面板。"));
        new TabletPresentationSettingsView(rightPane, prefs, Resources.Load<Font>("Fonts/zpix"));
    }

    // 形态 A · 账号页（对照 xiaomi.account 截图）
    private static void RenderAccountSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "Xiaomi 账号");

        // 大头像卡
        var profileCard = BuildWhiteCard();
        profileCard.style.flexDirection = FlexDirection.Row;
        profileCard.style.alignItems = Align.Center;
        profileCard.style.justifyContent = Justify.SpaceBetween;
        profileCard.style.paddingTop = 18; profileCard.style.paddingBottom = 18;

        var userLeft = new VisualElement();
        userLeft.style.flexDirection = FlexDirection.Row;
        userLeft.style.alignItems = Align.Center;
        profileCard.Add(userLeft);

        var avatar = new Label("👤");
        avatar.style.fontSize = 32; avatar.style.width = 54; avatar.style.height = 54;
        avatar.style.backgroundColor = SurfaceApp;
        avatar.style.unityTextAlign = TextAnchor.MiddleCenter;
        avatar.style.borderTopLeftRadius = 27; avatar.style.borderTopRightRadius = 27;
        avatar.style.borderBottomLeftRadius = 27; avatar.style.borderBottomRightRadius = 27;
        avatar.style.unityFontDefinition = Fd();
        userLeft.Add(avatar);

        var userMeta = new VisualElement();
        userMeta.style.marginLeft = 16;
        userLeft.Add(userMeta);

        string curUser = PlayerPrefs.GetString("XiaomiAccount", "旅人 (先民特等铁道运营官)");
        var nameLbl = new Label(curUser);
        nameLbl.style.fontSize = 18; nameLbl.style.color = InkPrimary;
        nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold; nameLbl.style.unityFontDefinition = Fd();
        userMeta.Add(nameLbl);

        var idLbl = new Label("ID: 0721-DPRK · 阿里郎量子加密专线已连接 · 全功能已解锁");
        idLbl.style.fontSize = 12; idLbl.style.color = new Color(0.2f, 0.65f, 0.35f, 1f); idLbl.style.unityFontDefinition = Fd();
        idLbl.style.marginTop = 4;
        userMeta.Add(idLbl);

        var ar = new Label("›");
        ar.style.fontSize = 20; ar.style.color = InkTertiary; ar.style.unityFontDefinition = Fd();
        profileCard.Add(ar);
        rightPane.Add(profileCard);

        // 卡片网格（左大卡 + 右两小卡）
        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.justifyContent = Justify.SpaceBetween;
        grid.style.marginBottom = 14;
        rightPane.Add(grid);

        var cloudCard = BuildWhiteCard();
        cloudCard.style.width = new Length(48, LengthUnit.Percent);
        cloudCard.style.marginBottom = 0;
        cloudCard.Add(new Label("☁ 云空间") { style = { fontSize = 16, color = InkPrimary, unityFontDefinition = Fd() } });
        cloudCard.Add(new Label("已分配 512 GB 阿里郎卫星云存储 (本地量子加密缓存)") { style = { fontSize = 12, color = InkSecondary, unityFontDefinition = Fd(), marginTop = 6, whiteSpace = WhiteSpace.Normal } });
        grid.Add(cloudCard);

        var rightSubCards = new VisualElement();
        rightSubCards.style.width = new Length(48, LengthUnit.Percent);
        rightSubCards.style.flexDirection = FlexDirection.Column;
        grid.Add(rightSubCards);

        var familyCard = BuildWhiteCard();
        familyCard.style.marginBottom = 10;
        familyCard.Add(new Label("👨‍👩‍👧 家庭服务") { style = { fontSize = 15, color = InkPrimary, unityFontDefinition = Fd() } });
        familyCard.Add(new Label("已共享雾峰村先民铁道运营排班表") { style = { fontSize = 12, color = InkSecondary, unityFontDefinition = Fd(), marginTop = 4 } });
        rightSubCards.Add(familyCard);

        var findCard = BuildWhiteCard();
        findCard.style.marginBottom = 0;
        findCard.Add(new Label("📍 查找设备") { style = { fontSize = 15, color = InkPrimary, unityFontDefinition = Fd() } });
        findCard.Add(new Label("0721号机车车载终端实时在线 · 信号极佳") { style = { fontSize = 12, color = InkSecondary, unityFontDefinition = Fd(), marginTop = 4 } });
        rightSubCards.Add(findCard);

        // 分组列表
        AddSectionHeader(rightPane, "我的账号安全与权限");
        var listCard = BuildWhiteCard();
        listCard.Add(BuildWhiteRow("账号安全等级", "最高特级 · 物理硬件密匙与3D生物结构光已认证", () => ShowToast("账号处于先民军工特级保护状态")));
        listCard.Add(BuildWhiteRow("个人人事档案", "金日成综合大学荣誉毕业生 · 铁道部特聘工程师", () => ShowToast("档案已同步至人事与调度中心")));
        listCard.Add(BuildWhiteRow("系统权限授权", "已授权系统管理 22 个车载及内网应用", () => ShowToast("车载应用沙箱权限正常运行中")));
        rightPane.Add(listCard);
    }

    // 2 我的设备：并入老设置关于 8 条 + 真实设定硬件配置（Xiaomi Pad 36 Ultra DPRK Edition）
    private static void RenderMyDeviceSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "我的设备");

        var devCard = BuildWhiteCard();
        devCard.style.alignItems = Align.Center;
        devCard.style.paddingTop = 18; devCard.style.paddingBottom = 18;

        var dName = new Label("Xiaomi Pad 36 Ultra (DPRK Edition)");
        dName.style.fontSize = 20; dName.style.color = InkPrimary;
        dName.style.unityFontStyleAndWeight = FontStyle.Bold; dName.style.unityFontDefinition = Fd();
        devCard.Add(dName);

        var osName = new Label("Xiaomi HyperOS 32.0.4.1 (DPRK Edition · 先民军工特供版)");
        osName.style.fontSize = 13; osName.style.color = AccentSystem;
        osName.style.unityFontDefinition = Fd(); osName.style.marginTop = 4;
        devCard.Add(osName);

        var devCode = new Label("代号: chollima-ultra · 先民军工遗留巅峰旗舰 · 0721号机车专属配件");
        devCode.style.fontSize = 11; devCode.style.color = InkSecondary;
        devCode.style.unityFontDefinition = Fd(); devCode.style.marginTop = 3;
        devCard.Add(devCode);
        rightPane.Add(devCard);

        AddSectionHeader(rightPane, "硬件规格与军工系统参数");
        var specsCard = BuildWhiteCard();
        specsCard.Add(BuildWhiteRow("处理器 (CPU)", "先民超导纳米核 3.6GHz × 16 (32位精简指令集)", null, false));
        specsCard.Add(BuildWhiteRow("运行内存 / 存储", "64 GB LPDDR9 Ultra / 2 TB 先民固态量子闪存", null, false));
        specsCard.Add(BuildWhiteRow("屏幕规格", "14.4 英寸 Super AMOLED Ultra (3840×2160 · 144Hz · 防眩光涂层)", null, false));
        specsCard.Add(BuildWhiteRow("电池与供电", "24,000 mAh 双电芯超级固态沙能电池 (支持120W快充 / 机车逆变回充)", null, false));
        specsCard.Add(BuildWhiteRow("设备序列号", "SN-20530721-CHL-001 (0721号专属配件 · 军工固化)", null, false));
        specsCard.Add(BuildWhiteRow("安全防护固件", "白头疫病 SiliVaccine 8.2 (军工高权限固化)", null, false));
        specsCard.Add(BuildWhiteRow("卫星通讯基带", "阿里郎军工量子加密卫星专线 (全天候链路已连接)", null, false));
        rightPane.Add(specsCard);

        AddSectionHeader(rightPane, "项目与版本信息");
        var aboutCard = BuildWhiteCard();
        aboutCard.Add(BuildWhiteRow("游戏名称", "铁路复兴：沙能冲击", null, false));

        // 当前版本行：连续点击 5 次进入开发者选项
        aboutCard.Add(BuildWhiteRow("系统版本", LoginManager.GAME_VERSION + " · HyperOS 32.0.4.1 (点击 5 次进入开发者选项)", () =>
        {
            devModeClickCount++;
            if (devModeClickCount >= 5)
            {
                devModeClickCount = 0;
                isTabletDevModeUnlocked = true;
                PlayerPrefs.SetInt("TabletDevModeUnlocked", 1);
                PlayerPrefs.Save();
                currentSettingsCategory = 99;
                ShowToast("🎉 已进入开发者模式！已跳转至开发者选项。");
                BuildSettingsPage(win);
            }
            else
            {
                ShowToast($"再点击 {5 - devModeClickCount} 次开启开发者选项");
            }
        }));

        aboutCard.Add(BuildWhiteRow("引擎版本", "Unity 6000.5.8f1", null, false));
        aboutCard.Add(BuildWhiteRow("渲染管线", "Universal Render Pipeline", null, false));
        aboutCard.Add(BuildWhiteRow("开发状态", "核心系统已完成，资产生成进行中", null, false));
        aboutCard.Add(BuildWhiteRow("开源许可", "MIT License", null, false));
        aboutCard.Add(BuildWhiteRow("作者", "NMDX0721", null, false));
        aboutCard.Add(BuildWhiteRow("灵感来源", "爱上火车-Last Run!!- / Stardew Valley", null, false));
        aboutCard.Add(BuildWhiteRow("字体", "Zpix (最像素)", null, false));
        aboutCard.Add(BuildWhiteRow("底层技术", "Roslyn C# / UI Toolkit / UGUI 混合渲染", null, false));
        rightPane.Add(aboutCard);
    }

    // 3 平板特色功能：机车桌面小部件、系统面板与外观增强入口
    private static void RenderTabletFeaturesSettings(ScrollView rightPane, VisualElement win)
    {
        var page = new VisualElement { name = "settings-tablet-features-page" };
        AddCenterHeader(page, "平板特色功能");

        AddSectionHeader(page, "桌面小部件与常驻交互");
        var desktopCard = BuildWhiteCard();
        desktopCard.name = "settings-tablet-features-desktop-card";
        desktopCard.Add(BuildWhiteRow("岁月提示气泡", "桌面可显示本地预设提示；此页不提供固定、启停或位置控制", null, false));
        desktopCard.Add(BuildWhiteRow("多功能仪表小部件组", "包含时钟、天气概览、行车排班、相册快照与日历小部件", null, false));
        desktopCard.Add(BuildWhiteRow("液体玻璃 Dock 栏", "底部常驻半透明应用托盘，支持常用车载应用一键切换", null, false));
        page.Add(desktopCard);

        AddSectionHeader(page, "系统与外观增强");
        var systemCard = BuildWhiteCard();
        systemCard.name = "settings-tablet-features-system-card";
        systemCard.Add(BuildWhiteRow("系统控制与通知中心", "支持从桌面顶部呼出系统面板与车载快捷开关（分类 11）", () =>
        {
            currentSettingsCategory = 11;
            BuildSettingsPage(win);
        }, true));
        systemCard.Add(BuildWhiteRow("平板外观与个性化调优", "支持壁纸压暗、减少动效、高对比度与强调色配置（分类 9）", () =>
        {
            currentSettingsCategory = 9;
            BuildSettingsPage(win);
        }, true));
        systemCard.Add(BuildWhiteRow("本机与外部服务", "游戏主体可离线运行；音乐页的第三方账号、歌单与播放功能会按用户操作联网", null, false));
        page.Add(systemCard);

        rightPane.Add(page);
    }

    // 4 WLAN：车载内网沙箱只读说明
    private static void RenderWlanSettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-wlan-page" };
        AddCenterHeader(page, "WLAN");

        AddSectionHeader(page, "网络接入范围");
        var card = BuildWhiteCard();
        card.name = "settings-wlan-card";
        card.Add(BuildWhiteRow("车载网络范围", "本终端运行于车载内网沙箱，不提供公共 Wi-Fi 接入，也不显示可扫描的无线网络", null, false));
        card.Add(BuildWhiteRow("热点与共享", "此页面没有热点广播或网络共享入口", null, false));
        card.Add(BuildWhiteRow("连接状态", "无线链路状态不在此页显示；请以机车行车设备的指示为准", null, false));
        page.Add(card);

        rightPane.Add(page);
    }

    // 5 蓝牙：驾驶台硬连线隔离说明
    private static void RenderBluetoothSettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-bluetooth-page" };
        AddCenterHeader(page, "蓝牙");

        AddSectionHeader(page, "设备发现与配对");
        var card = BuildWhiteCard();
        card.name = "settings-bluetooth-card";
        card.Add(BuildWhiteRow("蓝牙设备", "本终端不提供蓝牙扫描、发现或配对入口", null, false));
        card.Add(BuildWhiteRow("驾驶台设备", "操纵与仪表设备不通过此页面进行无线配对", null, false));
        card.Add(BuildWhiteRow("附近设备", "未配对设备不会显示在此页面", null, false));
        page.Add(card);

        rightPane.Add(page);
    }

    // 6 互联互通：列车重联与编组物理总线说明
    private static void RenderInterconnectSettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-interconnect-page" };
        AddCenterHeader(page, "互联互通");

        AddSectionHeader(page, "列车运行协同");
        var card = BuildWhiteCard();
        card.name = "settings-interconnect-card";
        card.Add(BuildWhiteRow("机车与编组", "列车编组与机车协同由行车设备管理，此终端不提供编组控制操作", null, false));
        card.Add(BuildWhiteRow("列车状态", "行车状态请以驾驶台的机车仪表与提示为准", null, false));
        card.Add(BuildWhiteRow("账户与同步", "此终端不提供个人账户登录或跨设备云同步服务", null, false));
        page.Add(card);

        rightPane.Add(page);
    }

    // 7 更多连接方式：机车专用通信与维护接口说明
    private static void RenderMoreConnectionsSettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-more-connections-page" };
        AddCenterHeader(page, "更多连接方式");

        AddSectionHeader(page, "其它连接服务");
        var card = BuildWhiteCard();
        card.name = "settings-more-connections-card";
        card.Add(BuildWhiteRow("网络共享", "此终端不提供便携热点或网络共享入口", null, false));
        card.Add(BuildWhiteRow("显示扩展", "此终端不提供无线投屏或外接显示控制入口", null, false));
        card.Add(BuildWhiteRow("设备检修", "机车设备检修由机务检修流程负责，此页不显示调试接口状态", null, false));
        page.Add(card);

        rightPane.Add(page);
    }

    // 8 小米超级AI：本地先民人工智能原型机与对话机制说明
    private static void RenderAiSettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-ai-page" };
        AddCenterHeader(page, "小米超级AI");

        AddSectionHeader(page, "车载 AI 引擎与本地模型");
        var engineCard = BuildWhiteCard();
        engineCard.name = "settings-ai-engine-card";
        engineCard.Add(BuildWhiteRow("本地对话原型", "岁月对话目前由本地预设回复驱动；本页没有模型切换或云端推理控制", null, false));
        engineCard.Add(BuildWhiteRow("外部模型服务", "此对话入口未接入外部大模型服务", null, false));
        engineCard.Add(BuildWhiteRow("语音输入", "本应用当前没有语音唤醒或麦克风输入功能", null, false));
        page.Add(engineCard);

        AddSectionHeader(page, "交互与桌面展示");
        var interactionCard = BuildWhiteCard();
        interactionCard.name = "settings-ai-interaction-card";
        interactionCard.Add(BuildWhiteRow("桌面提示气泡", "由桌面界面按本地脚本显示；此行仅说明现状，不代表可配置开关", null, false));
        interactionCard.Add(BuildWhiteRow("多轮自适应对话", "当前为本地模板响应阶段；生成式多轮对话功能未接入在线算力", null, false));
        page.Add(interactionCard);

        rightPane.Add(page);
    }

    public static void ShowResetConfirmDialog(
        VisualElement host,
        string title,
        string message,
        string confirmBtnText,
        Action onConfirm)
    {
        if (host == null) return;

        var existing = host.Q("settings-confirm-overlay");
        if (existing != null) host.Remove(existing);

        var overlay = new VisualElement { name = "settings-confirm-overlay" };
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0;
        overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.60f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        overlay.pickingMode = PickingMode.Position;
        overlay.focusable = true;

        void Dismiss()
        {
            if (overlay.parent != null)
            {
                overlay.parent.Remove(overlay);
            }
        }

        overlay.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay)
            {
                Dismiss();
            }
        });

        overlay.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                evt.StopImmediatePropagation();
                Dismiss();
            }
        });

        var box = new VisualElement { name = "settings-confirm-box" };
        box.style.width = 460;
        box.style.backgroundColor = SurfaceCard;
        box.style.borderTopLeftRadius = 14; box.style.borderTopRightRadius = 14;
        box.style.borderBottomLeftRadius = 14; box.style.borderBottomRightRadius = 14;
        box.style.borderTopWidth = 1; box.style.borderBottomWidth = 1;
        box.style.borderLeftWidth = 1; box.style.borderRightWidth = 1;
        box.style.borderTopColor = BorderCard; box.style.borderBottomColor = BorderCard;
        box.style.borderLeftColor = BorderCard; box.style.borderRightColor = BorderCard;
        box.style.paddingTop = 22; box.style.paddingBottom = 20;
        box.style.paddingLeft = 24; box.style.paddingRight = 24;
        box.pickingMode = PickingMode.Position;
        box.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        overlay.Add(box);

        var titleLbl = new Label(title) { name = "settings-confirm-title" };
        titleLbl.style.fontSize = 17;
        titleLbl.style.color = new Color(0.85f, 0.25f, 0.20f, 1f);
        titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLbl.style.unityFontDefinition = Fd();
        titleLbl.style.marginBottom = 12;
        box.Add(titleLbl);

        var descLbl = new Label(message) { name = "settings-confirm-desc" };
        descLbl.style.fontSize = 13;
        descLbl.style.color = InkPrimary;
        descLbl.style.unityFontDefinition = Fd();
        descLbl.style.whiteSpace = WhiteSpace.Normal;
        descLbl.style.marginBottom = 20;
        box.Add(descLbl);

        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;
        btnRow.style.justifyContent = Justify.FlexEnd;
        btnRow.style.alignItems = Align.Center;
        box.Add(btnRow);

        var cancelBtn = new Button(Dismiss) { text = "取消", name = "settings-confirm-cancel-btn" };
        cancelBtn.style.height = 34;
        cancelBtn.style.paddingLeft = 18; cancelBtn.style.paddingRight = 18;
        cancelBtn.style.fontSize = 13;
        cancelBtn.style.unityFontDefinition = Fd();
        cancelBtn.style.backgroundColor = SurfaceApp;
        cancelBtn.style.color = InkPrimary;
        cancelBtn.style.borderTopLeftRadius = 8; cancelBtn.style.borderTopRightRadius = 8;
        cancelBtn.style.borderBottomLeftRadius = 8; cancelBtn.style.borderBottomRightRadius = 8;
        cancelBtn.style.borderTopWidth = 1; cancelBtn.style.borderBottomWidth = 1;
        cancelBtn.style.borderLeftWidth = 1; cancelBtn.style.borderRightWidth = 1;
        cancelBtn.style.borderTopColor = BorderCard; cancelBtn.style.borderBottomColor = BorderCard;
        cancelBtn.style.borderLeftColor = BorderCard; cancelBtn.style.borderRightColor = BorderCard;
        cancelBtn.style.marginRight = 12;
        btnRow.Add(cancelBtn);

        var confirmBtn = new Button(() =>
        {
            Dismiss();
            onConfirm?.Invoke();
        }) { text = confirmBtnText, name = "settings-confirm-ok-btn" };
        confirmBtn.style.height = 34;
        confirmBtn.style.paddingLeft = 18; confirmBtn.style.paddingRight = 18;
        confirmBtn.style.fontSize = 13;
        confirmBtn.style.unityFontDefinition = Fd();
        confirmBtn.style.backgroundColor = new Color(0.85f, 0.25f, 0.20f, 1f);
        confirmBtn.style.color = Color.white;
        confirmBtn.style.borderTopLeftRadius = 8; confirmBtn.style.borderTopRightRadius = 8;
        confirmBtn.style.borderBottomLeftRadius = 8; confirmBtn.style.borderBottomRightRadius = 8;
        confirmBtn.style.borderTopWidth = 0; confirmBtn.style.borderBottomWidth = 0;
        confirmBtn.style.borderLeftWidth = 0; confirmBtn.style.borderRightWidth = 0;
        btnRow.Add(confirmBtn);

        host.Add(overlay);
        overlay.Focus();
    }

    // 99 开发者选项：支持重置游戏数据与调试测试
    private static void RenderDeveloperOptions(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "开发者选项");

        var noticeCard = BuildWhiteCard();
        noticeCard.style.backgroundColor = new Color(0.99f, 0.95f, 0.90f, 1f);
        noticeCard.style.borderTopColor = AccentSystem; noticeCard.style.borderBottomColor = AccentSystem;
        noticeCard.style.borderLeftColor = AccentSystem; noticeCard.style.borderRightColor = AccentSystem;
        var noticeTitle = new Label("⚠️ 开发者调试模式已激活");
        noticeTitle.style.fontSize = 15; noticeTitle.style.color = AccentSystem;
        noticeTitle.style.unityFontStyleAndWeight = FontStyle.Bold; noticeTitle.style.unityFontDefinition = Fd();
        noticeCard.Add(noticeTitle);
        var noticeDesc = new Label("此页面提供存档重置、经济数据回滚与全剧情解锁等高权限调试指令，请谨慎操作。");
        noticeDesc.style.fontSize = 12; noticeDesc.style.color = InkSecondary;
        noticeDesc.style.unityFontDefinition = Fd(); noticeDesc.style.marginTop = 4;
        noticeCard.Add(noticeDesc);
        rightPane.Add(noticeCard);

        AddSectionHeader(rightPane, "主线剧情与存档重置");
        var storyResetCard = BuildWhiteCard();
        storyResetCard.Add(BuildWhiteRow("重置主线剧情进度", "清空所有已通关剧本与幕次记录，重置回序章第01幕", () =>
        {
            ShowResetConfirmDialog(
                win ?? rightPane,
                "⚠️ 重置主线剧情进度确认",
                "此操作将删除所有序章通关剧本与幕次记录（ArchiveStory_*、ArchiveScene_*）、彩蛋记录（VN_Got0721）、最后阅读记录（LastReadScript）以及调试全章节解锁标记（DebugUnlockAllChapters），将剧情重置回序章第 01 幕。\n\n影响边界说明：\n• 仅重置主线剧情与阅读记录（PlayerPrefs 对应键）；\n• 不影响资金、信任、车况等经济模拟数据；\n• 不会清除音乐库独立 JSON 与歌词数据；\n• 不会修改系统设置与按键映射。",
                "确认重置主线",
                () =>
                {
                    for (int i = 0; i < TabletReturnJourneyUI.PrologueEpisodes.Length; i++)
                    {
                        PlayerPrefs.DeleteKey("ArchiveStory_" + TabletReturnJourneyUI.PrologueEpisodes[i].scriptId);
                        PlayerPrefs.DeleteKey("ArchiveScene_" + TabletReturnJourneyUI.PrologueEpisodes[i].scriptId);
                    }
                    PlayerPrefs.DeleteKey("VN_Got0721");
                    PlayerPrefs.DeleteKey("LastReadScript");
                    PlayerPrefs.DeleteKey("DebugUnlockAllChapters");
                    PlayerPrefs.Save();
                    ShowToast("🔄 主线剧情进度已重置至序章第 01 幕！");
                });
        }));

        storyResetCard.Add(BuildWhiteRow("一键解锁全部剧情与CG (调试用)", "解锁序章全部 10 幕剧本及所有已收录 CG 资源", () =>
        {
            PlayerPrefs.SetInt("DebugUnlockAllChapters", 1);
            for (int i = 0; i < TabletReturnJourneyUI.PrologueEpisodes.Length; i++)
            {
                PlayerPrefs.SetInt("ArchiveStory_" + TabletReturnJourneyUI.PrologueEpisodes[i].scriptId, 1);
                PlayerPrefs.SetInt("ArchiveScene_" + TabletReturnJourneyUI.PrologueEpisodes[i].scriptId, 1);
            }
            PlayerPrefs.SetInt("VN_Got0721", 1);
            PlayerPrefs.Save();
            ShowToast("✨ 已解锁全部主线章节、剧幕与剧情 CG！");
        }));
        rightPane.Add(storyResetCard);

        AddSectionHeader(rightPane, "经营与经济核数据重置");
        var ecoResetCard = BuildWhiteCard();
        ecoResetCard.Add(BuildWhiteRow("重置经济模拟数据", "重置当前运行中的经营状态：资金按难度恢复，初始趋势依已加载种子生成，回到第 1 天", () =>
        {
            ShowResetConfirmDialog(
                win ?? rightPane,
                "⚠️ 重置经济模拟数据确认",
                "此操作会把当前运行中的经营模拟重置到第 1 天，资金按当前难度配置恢复；若已加载世界种子，信任、车况、政治压力和线路老化将恢复为该种子生成的初值，无种子时使用兼容默认值。趋势历史、监管程序、经营策略、短期目标和当日日事件也会重置。\n\n影响边界说明：\n• 不删除已有经营存档槽；重新载入旧存档会恢复槽内数据；\n• 不删除主线剧情通关与阅读记录，但会重置当前运行中的剧情补贴防重复状态；\n• 不清除音乐库独立 JSON 与歌词数据；\n• 不修改系统设置与按键映射。",
                "确认重置经济",
                () =>
                {
                    GameData.ResetState();
                    ShowToast("💰 当前经营模拟状态已重置为第 1 天！");
                });
        }));

        ecoResetCard.Add(BuildWhiteRow("注入调试测试资金", "为当前账务追加 100,000 沙币并恢复信任", () =>
        {
            GameData.AddMoney(100000);
            GameData.AddTrust(30);
            ShowToast("💳 已注资 100,000 沙币，信任已提升！");
        }));
        rightPane.Add(ecoResetCard);

        AddSectionHeader(rightPane, "开发者模式");
        var devDangerCard = BuildWhiteCard();
        devDangerCard.Add(BuildWhiteRow("退出开发者模式", "隐藏开发者选项并锁定调试入口", () =>
        {
            isTabletDevModeUnlocked = false;
            PlayerPrefs.DeleteKey("TabletDevModeUnlocked");
            PlayerPrefs.Save();
            currentSettingsCategory = 2;
            ShowToast("🔒 已退出开发者模式。");
            BuildSettingsPage(win);
        }));

        rightPane.Add(devDangerCard);
    }

    // 10 锁屏
    private static void RenderLockScreenSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "锁屏");

        AddSectionHeader(rightPane, "息屏与锁屏样式");
        var card = BuildWhiteCard();
        card.Add(BuildWhiteRow("锁屏时钟", "主体历 / 公历 双排时钟", null, false));
        card.Add(BuildWhiteRow("解锁方式", "轻触屏幕或按空格解锁", null, false));
        rightPane.Add(card);
    }

    private static void RequestOpenSystemPanel(TabletSystemPanel panel)
    {
        if (s_openSystemPanel != null)
        {
            s_openSystemPanel(panel);
            return;
        }

        if (GameMainUI.Instance != null)
        {
            GameMainUI.Instance.OpenSystemPanel(panel);
            return;
        }

        ShowToast(panel == TabletSystemPanel.Notifications
            ? "通知中心服务暂不可用（请从桌面状态栏打开）"
            : "控制面板服务暂不可用（请从桌面状态栏打开）");
    }

    // 11 通知与状态栏：系统面板快捷入口与运行机制
    private static void RenderNotificationAndStatusSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "通知与状态栏");

        bool hasService = s_openSystemPanel != null || GameMainUI.Instance != null;
        if (!hasService)
        {
            var fallbackCard = BuildWhiteCard();
            fallbackCard.name = "settings-notifications-fallback-card";
            fallbackCard.style.backgroundColor = new Color(0.98f, 0.94f, 0.88f, 0.95f);
            var fallbackLbl = new Label("⚠️ 当前环境未挂载系统面板服务（通知中心与控制面板入口将提示降级）。")
            {
                name = "settings-notifications-fallback-note"
            };
            fallbackLbl.style.fontSize = 12;
            fallbackLbl.style.color = new Color(0.65f, 0.35f, 0.05f, 1f);
            fallbackLbl.style.unityFontDefinition = Fd();
            fallbackCard.Add(fallbackLbl);
            rightPane.Add(fallbackCard);
        }

        AddSectionHeader(rightPane, "系统面板快捷入口");
        var panelCard = BuildWhiteCard();
        panelCard.name = "settings-notifications-panel-card";

        var notifRow = BuildWhiteRow("打开通知中心", "查看车载系统与各应用推送的提示记录（上限 32 条）", () =>
        {
            RequestOpenSystemPanel(TabletSystemPanel.Notifications);
        });
        notifRow.name = "settings-open-notifications-btn";
        panelCard.Add(notifRow);

        var ctrlRow = BuildWhiteRow("打开控制面板", "查看车载快捷控制、勿扰模式与随车调度开关", () =>
        {
            RequestOpenSystemPanel(TabletSystemPanel.Controls);
        });
        ctrlRow.name = "settings-open-controls-btn";
        panelCard.Add(ctrlRow);

        rightPane.Add(panelCard);

        AddSectionHeader(rightPane, "运行机制与说明");
        var infoCard = BuildWhiteCard();
        infoCard.name = "settings-notifications-info-card";
        infoCard.Add(BuildWhiteRow("通知收存机制", "通知均来自应用实际运行提示；最多收存 32 条，超限自动淘汰早期记录", null, false));
        infoCard.Add(BuildWhiteRow("勿扰模式策略", "开启勿扰后系统保留通知收存，仅静默屏蔽悬浮横幅；勿扰当前仅在进程内有效", null, false));
        infoCard.Add(BuildWhiteRow("系统连接说明", "车载平板处于内网沙箱运行；状态栏不虚拟 Wi-Fi / 蓝牙 / 电池连接状态", null, false));
        rightPane.Add(infoCard);
    }

    // 12 桌面：包含复古画风
    private static void RenderHomeScreenSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "桌面");

        AddSectionHeader(rightPane, "布局与渲染模式");
        var card = BuildWhiteCard();
        bool retroOn = PlayerPrefs.GetInt("RetroLowResBG", 0) == 1;
        card.Add(BuildToggleRow("复古画风（低颗粒背景）", "开启后降低背景分辨率，模拟复古像素质感", retroOn, v =>
        {
            PlayerPrefs.SetInt("RetroLowResBG", v ? 1 : 0);
            PlayerPrefs.Save();
            ShowToast(v ? "复古画风已开启，下次进入剧情生效" : "已恢复原始高清背景");
        }));
        card.Add(BuildWhiteRow("桌面图标排布", "双页网格 + 底部液体玻璃 Dock 栏", null, false));
        card.Add(BuildWhiteRow("岁月 AI 桌面小部件", "已固定于桌面右下角 · 闲暇交互气泡", () => ShowToast("小部件运行中")));
        rightPane.Add(card);
    }

    // 13 显示与亮度：老设置「显示」8条全量并入
    private static void RenderDisplaySettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "显示与亮度");

        AddSectionHeader(rightPane, "画面渲染");
        var card = BuildWhiteCard();

        card.Add(BuildToggleRow("全屏模式", "以全屏方式运行游戏", Screen.fullScreen, v => Screen.fullScreen = v));

        bool vsyncOn = QualitySettings.vSyncCount > 0;
        card.Add(BuildToggleRow("垂直同步", "与自定义帧率互斥，开启后帧率与显示器刷新率同步", vsyncOn, v =>
        {
            QualitySettings.vSyncCount = v ? 1 : 0;
            PlayerPrefs.SetInt("VSync", v ? 1 : 0);
            PlayerPrefs.SetInt("CustomFPS", v ? 0 : 1);
            PlayerPrefs.Save();
            if (v) Application.targetFrameRate = -1;
            else Application.targetFrameRate = PlayerPrefs.GetInt("TargetFPS", 60);
            BuildSettingsPage(win);
        }));

        bool customFpsOn = PlayerPrefs.GetInt("CustomFPS", 1) == 1;
        card.Add(BuildToggleRow("自定义帧率", "关闭垂直同步后手动限制帧率", customFpsOn, v =>
        {
            PlayerPrefs.SetInt("CustomFPS", v ? 1 : 0);
            if (v)
            {
                QualitySettings.vSyncCount = 0;
                PlayerPrefs.SetInt("VSync", 0);
                Application.targetFrameRate = PlayerPrefs.GetInt("TargetFPS", 60);
            }
            else
            {
                QualitySettings.vSyncCount = 1;
                PlayerPrefs.SetInt("VSync", 1);
                Application.targetFrameRate = -1;
            }
            PlayerPrefs.Save();
            BuildSettingsPage(win);
        }));

        card.Add(BuildSliderRow("帧率上限", 30, 240, PlayerPrefs.GetInt("TargetFPS", 60), v =>
        {
            Application.targetFrameRate = v;
            PlayerPrefs.SetInt("TargetFPS", v);
            PlayerPrefs.Save();
        }, " FPS", customFpsOn));

        bool showFpsOn = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
        card.Add(BuildToggleRow("显示帧率", "在角落显示当前 FPS", showFpsOn, v =>
        {
            PlayerPrefs.SetInt("ShowFPS", v ? 1 : 0);
            PlayerPrefs.Save();
            FPSDisplay.SetActive(v);
        }));

        card.Add(BuildSliderRow("文字速度", 1, 10, PlayerPrefs.GetInt("TextSpeed", 5), v =>
        {
            PlayerPrefs.SetInt("TextSpeed", v);
            PlayerPrefs.Save();
        }, " 档"));

        card.Add(BuildSliderRow("对话框透明度", 0, 100, PlayerPrefs.GetInt("DialogAlpha", 80), v =>
        {
            PlayerPrefs.SetInt("DialogAlpha", v);
            PlayerPrefs.Save();
        }, " %"));

        card.Add(BuildWhiteRow("界面缩放方案", "2.5K/4K 屏幕自适应对齐（ScaleMode=2）", null, false));

        var resetBtn = new Button(() =>
        {
            Screen.fullScreen = true;
            QualitySettings.vSyncCount = 0;
            PlayerPrefs.SetInt("VSync", 0);
            PlayerPrefs.SetInt("CustomFPS", 1);
            PlayerPrefs.SetInt("TargetFPS", 60);
            PlayerPrefs.SetInt("ShowFPS", 0);
            PlayerPrefs.SetInt("TextSpeed", 5);
            PlayerPrefs.SetInt("DialogAlpha", 80);
            PlayerPrefs.Save();
            Application.targetFrameRate = 60;
            FPSDisplay.SetActive(false);
            ShowToast("显示设置已恢复默认");
            BuildSettingsPage(win);
        }) { text = "恢复显示默认设置" };
        StyleActionBtn(resetBtn);
        card.Add(resetBtn);

        rightPane.Add(card);
    }

    // 14 声音与振动：老设置「音频」全量并入
    private static void RenderAudioSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "声音与振动");

        AddSectionHeader(rightPane, "音量调节");
        var card = BuildWhiteCard();

        card.Add(BuildSliderRow("主音量", 0, 100, Mathf.RoundToInt(GameData.MasterVolume * 100), v =>
        {
            GameData.MasterVolume = v / 100f;
        }, " %"));

        card.Add(BuildSliderRow("背景音乐", 0, 100, Mathf.RoundToInt(GameData.BGMVolume * 100), v =>
        {
            GameData.BGMVolume = v / 100f;
        }, " %"));

        card.Add(BuildSliderRow("音效", 0, 100, Mathf.RoundToInt(GameData.SFXVolume * 100), v =>
        {
            GameData.SFXVolume = v / 100f;
        }, " %"));

        card.Add(BuildSliderRow("打字机音效", 0, 100, Mathf.RoundToInt(GameData.TypewriterVolume * 100), v =>
        {
            GameData.TypewriterVolume = v / 100f;
        }, " %"));

        card.Add(BuildWhiteRow("铁道音效模拟", "已启用（先民柴油机车鸣笛与道岔金属撞击）", null, false));

        var resetBtn = new Button(() =>
        {
            GameData.MasterVolume = 1f;
            GameData.BGMVolume = 0.8f;
            GameData.SFXVolume = 1f;
            GameData.TypewriterVolume = 0.12f;
            ShowToast("音频设置已恢复默认");
            BuildSettingsPage(win);
        }) { text = "恢复默认音量设置" };
        StyleActionBtn(resetBtn);
        card.Add(resetBtn);

        rightPane.Add(card);
    }

    // 15 触控笔与键盘：老设置「操作」按键重绑定全量并入
    private static void RenderControlSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "触控笔与键盘");

        AddSectionHeader(rightPane, "按键映射（点击按钮后按下新按键更改，Escape 取消）");
        var card = BuildWhiteCard();

        foreach (KeyBindings.Action action in System.Enum.GetValues(typeof(KeyBindings.Action)))
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 10; row.style.paddingBottom = 10;
            row.style.borderBottomWidth = 1; row.style.borderBottomColor = BorderDivider;

            var lbl = new Label(KeyBindings.GetActionName(action));
            lbl.style.fontSize = 15; lbl.style.color = InkPrimary;
            lbl.style.unityFontDefinition = Fd();
            row.Add(lbl);

            Button keyBtn = null;
            var capturedAction = action;
            keyBtn = new Button(() => StartTabletRebind(capturedAction, keyBtn)) { text = KeyBindings.GetKeyName(action) };
            keyBtn.style.width = 130; keyBtn.style.height = 34; keyBtn.style.fontSize = 14;
            keyBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            keyBtn.style.unityFontDefinition = Fd();
            keyBtn.style.backgroundColor = SurfaceApp;
            keyBtn.style.color = AccentSystem;
            keyBtn.style.borderTopLeftRadius = 6; keyBtn.style.borderTopRightRadius = 6;
            keyBtn.style.borderBottomLeftRadius = 6; keyBtn.style.borderBottomRightRadius = 6;
            keyBtn.style.borderTopWidth = 1; keyBtn.style.borderBottomWidth = 1;
            keyBtn.style.borderLeftWidth = 1; keyBtn.style.borderRightWidth = 1;
            keyBtn.style.borderTopColor = BorderCard; keyBtn.style.borderBottomColor = BorderCard;
            keyBtn.style.borderLeftColor = BorderCard; keyBtn.style.borderRightColor = BorderCard;
            row.Add(keyBtn);

            card.Add(row);
        }

        var resetBtn = new Button(() =>
        {
            KeyBindings.ResetToDefaults();
            ShowToast("按键映射已恢复默认设置");
            BuildSettingsPage(win);
        }) { text = "恢复默认按键配置" };
        StyleActionBtn(resetBtn);
        card.Add(resetBtn);

        rightPane.Add(card);
    }

    private static void StartTabletRebind(KeyBindings.Action action, Button btn)
    {
        btn.text = "按下新键...";
        btn.style.color = new Color(0.9f, 0.45f, 0.1f, 1f);
        System.Action<KeyCode> onKey = (key) =>
        {
            if (key != KeyCode.None && key != KeyCode.Escape)
            {
                KeyBindings.SetKey(action, key);
                btn.text = KeyBindings.GetKeyName(action);
                btn.style.color = AccentSystem;
            }
            else
            {
                btn.text = KeyBindings.GetKeyName(action);
                btn.style.color = AccentSystem;
            }
            TabletRebindAction = null;
        };
        TabletRebindAction = onKey;
    }

    // 16 人脸与锁屏密码：屏幕锁定凭据与机车值班策略
    private static void RenderLockSecuritySettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-lock-security-page" };
        AddCenterHeader(page, "人脸与锁屏密码");

        AddSectionHeader(page, "屏幕锁定凭据");
        var credCard = BuildWhiteCard();
        credCard.name = "settings-lock-credential-card";
        credCard.Add(BuildWhiteRow("锁屏凭据", "当前版本没有密码凭据验证功能", null, false));
        credCard.Add(BuildWhiteRow("PIN 码与图案手势", "当前版本没有 PIN 或图案密码验证流程", null, false));
        credCard.Add(BuildWhiteRow("人脸与指纹验证", "当前版本没有接入生物识别验证；设备是否具备相关硬件不在此应用中检测", null, false));
        page.Add(credCard);

        AddSectionHeader(page, "解锁策略与安全说明");
        var policyCard = BuildWhiteCard();
        policyCard.name = "settings-lock-policy-card";
        policyCard.Add(BuildWhiteRow("当前屏幕行为", "本应用锁屏页可通过轻触屏幕或按空格键关闭；这不是凭据保护", null, false));
        page.Add(policyCard);

        rightPane.Add(page);
    }

    // 17 隐私与安全：本地存储边界与沙箱权限
    private static void RenderPrivacySettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-privacy-page" };
        AddCenterHeader(page, "隐私与安全");

        AddSectionHeader(page, "本地数据存储与保护");
        var storageCard = BuildWhiteCard();
        storageCard.name = "settings-privacy-storage-card";
        storageCard.Add(BuildWhiteRow("本机数据", "游戏存档、部分设置与音乐库使用本机 PlayerPrefs 或文件；具体范围由各模块管理", null, false));
        storageCard.Add(BuildWhiteRow("外部服务连接", "音乐页的部分账号、歌单与播放功能会按用户操作请求第三方服务；此页不代表其数据处理策略", null, false));
        storageCard.Add(BuildWhiteRow("统计与权限说明", "此应用页不提供系统权限审计或统一遥测开关；请勿据此推断操作系统权限状态", null, false));
        page.Add(storageCard);

        AddSectionHeader(page, "应用权限与系统沙箱");
        var sandboxCard = BuildWhiteCard();
        sandboxCard.name = "settings-privacy-sandbox-card";
        sandboxCard.Add(BuildWhiteRow("本机文件", "设置和部分用户数据写入本机存储；音乐功能还可能按用户操作访问网络", null, false));
        sandboxCard.Add(BuildWhiteRow("权限状态", "此页面不读取相机、麦克风、定位等操作系统权限授权状态", null, false));
        page.Add(sandboxCard);

        rightPane.Add(page);
    }

    // 18 应用：系统应用管控入口
    private static void RenderAppsSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "应用设置");

        AddSectionHeader(rightPane, "应用与组件管理");
        var card = BuildWhiteCard();
        card.Add(BuildWhiteRow("进入应用管控", "已禁用的 4 个系统组件与恢复彩蛋", ShowAppManagementPage));
        card.Add(BuildWhiteRow("默认应用设置", "默认浏览器：光明内网浏览器 · 默认播放器：随身听", null, false));
        card.Add(BuildWhiteRow("特殊权限设置", "全部 22 个应用沙箱隔离权限", () => ShowToast("权限运转正常")));
        rightPane.Add(card);
    }

    // 19 电池：真实系统电源状态与机车供电说明
    private static void RenderBatterySettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-battery-page" };
        AddCenterHeader(page, "电池");

        AddSectionHeader(page, "供电状态");
        var statusCard = BuildWhiteCard();
        statusCard.name = "settings-battery-status-card";

        float battLevel = SystemInfo.batteryLevel;
        BatteryStatus battStatus = SystemInfo.batteryStatus;
        string levelText = battLevel >= 0f ? $"{Mathf.RoundToInt(battLevel * 100f)}%" : "平台未提供电量读数";
        string statusText;
        switch (battStatus)
        {
            case BatteryStatus.Charging: statusText = "正在充电"; break;
            case BatteryStatus.Discharging: statusText = "电池供电放电中"; break;
            case BatteryStatus.Full: statusText = "电池已充满"; break;
            case BatteryStatus.NotCharging: statusText = "未在充电"; break;
            default: statusText = "平台未报告电池状态"; break;
        }

        statusCard.Add(BuildWhiteRow("当前电量", levelText, null, false));
        statusCard.Add(BuildWhiteRow("供电状态", statusText, null, false));
        statusCard.Add(BuildWhiteRow("数据来源", "仅显示 Unity 当前运行平台提供的设备电池状态，不代表机车蓄电池或辅助电源遥测", null, false));
        page.Add(statusCard);

        AddSectionHeader(page, "能耗管理说明");
        var policyCard = BuildWhiteCard();
        policyCard.name = "settings-battery-policy-card";
        policyCard.Add(BuildWhiteRow("能耗统计", "本应用不统计各页面或应用的后台耗电量", null, false));
        policyCard.Add(BuildWhiteRow("电源控制", "此页不控制操作系统省电模式或机车电源设备", null, false));
        page.Add(policyCard);

        rightPane.Add(page);
    }

    // 20 健康使用设备：屏幕时长与司乘值班警惕机制说明
    private static void RenderDigitalWellbeingSettings(ScrollView rightPane, VisualElement win)
    {
        var page = new VisualElement { name = "settings-digital-wellbeing-page" };
        AddCenterHeader(page, "健康使用设备");

        AddSectionHeader(page, "使用统计与时间管理");
        var statsCard = BuildWhiteCard();
        statsCard.name = "settings-digital-wellbeing-stats-card";
        statsCard.Add(BuildWhiteRow("屏幕使用时长统计", "未启用后台应用活跃度监测（不记录使用时长与亮屏频次）", null, false));
        statsCard.Add(BuildWhiteRow("应用使用限额与防沉迷", "车载专业设备不提供个人防沉迷与应用使用时间限制", null, false));
        page.Add(statsCard);

        AddSectionHeader(page, "值班疲劳提醒");
        var dutyCard = BuildWhiteCard();
        dutyCard.name = "settings-digital-wellbeing-duty-card";
        dutyCard.Add(BuildWhiteRow("行车安全数据", "此页不读取机车警惕装置、驾驶疲劳状态或行车联锁数据", null, false));
        dutyCard.Add(BuildWhiteRow("显示与亮度设置", "前往调整现有画面和对话显示选项；当前没有屏幕色温或滤蓝光控制", () =>
        {
            currentSettingsCategory = 13;
            BuildSettingsPage(win);
        }, true));
        page.Add(dutyCard);

        rightPane.Add(page);
    }

    // 21 儿童模式：机车操纵环境与权限说明
    private static void RenderKidsModeSettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-kids-mode-page" };
        AddCenterHeader(page, "儿童模式");

        AddSectionHeader(page, "儿童与监护模式状态");
        var statusCard = BuildWhiteCard();
        statusCard.name = "settings-kids-mode-status-card";
        statusCard.Add(BuildWhiteRow("儿童空间", "当前版本没有独立儿童账户或受限应用桌面", null, false));
        statusCard.Add(BuildWhiteRow("家长控制", "当前版本没有家长监护、使用时长限制或内容过滤功能", null, false));
        page.Add(statusCard);

        AddSectionHeader(page, "值班环境说明");
        var envCard = BuildWhiteCard();
        envCard.name = "settings-kids-mode-env-card";
        envCard.Add(BuildWhiteRow("模式状态", "本应用没有儿童模式开关或单独退出密码", null, false));
        page.Add(envCard);

        rightPane.Add(page);
    }

    // 形态 C · 无障碍页（4 胶囊 Tab + 开关行，对照 Accessibility 截图）
    private static void RenderAccessibilitySettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "无障碍");

        // 4 个胶囊 Tab
        var tabRow = new VisualElement();
        tabRow.style.flexDirection = FlexDirection.Row;
        tabRow.style.justifyContent = Justify.Center;
        tabRow.style.marginBottom = 16;
        rightPane.Add(tabRow);

        string[] tabs = { "常规", "视觉", "听觉", "肢体" };
        for (int i = 0; i < tabs.Length; i++)
        {
            int idx = i;
            var tabBtn = new Button(() =>
            {
                accessibilityTab = idx;
                BuildSettingsPage(win);
            }) { text = tabs[i] };
            tabBtn.style.height = 34; tabBtn.style.paddingLeft = 20; tabBtn.style.paddingRight = 20;
            tabBtn.style.fontSize = 14; tabBtn.style.unityFontDefinition = Fd();
            tabBtn.style.marginRight = 8;
            tabBtn.style.backgroundColor = (i == accessibilityTab) ? new Color(0.88f, 0.89f, 0.92f, 1f) : SurfaceCard;
            tabBtn.style.color = (i == accessibilityTab) ? InkPrimary : InkSecondary;
            tabBtn.style.borderTopLeftRadius = 17; tabBtn.style.borderTopRightRadius = 17;
            tabBtn.style.borderBottomLeftRadius = 17; tabBtn.style.borderBottomRightRadius = 17;
            tabBtn.style.borderTopWidth = 1; tabBtn.style.borderBottomWidth = 1;
            tabBtn.style.borderLeftWidth = 1; tabBtn.style.borderRightWidth = 1;
            tabBtn.style.borderTopColor = BorderCard; tabBtn.style.borderBottomColor = BorderCard;
            tabBtn.style.borderLeftColor = BorderCard; tabBtn.style.borderRightColor = BorderCard;
            tabRow.Add(tabBtn);
        }

        AddSectionHeader(rightPane, tabs[accessibilityTab] + "辅助服务");
        var card = BuildWhiteCard();
        card.Add(BuildWhiteRow("系统面板高对比度", "在平板外观中统一调整", () => { currentSettingsCategory = 9; BuildSettingsPage(win); }));
        card.Add(BuildWhiteRow("读屏服务", "未启用", null, false));
        rightPane.Add(card);
    }

    // 23 更多设置：老设置「游戏」6条全量 + 备份 + 术语图鉴
    private static void RenderAdditionalSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "更多设置");

        AddSectionHeader(rightPane, "游戏与推进行为（老设置游戏项）");
        var gameCard = BuildWhiteCard();
        gameCard.Add(BuildToggleRow("自动播放模式", "开场后自动推进对话", PlayerPrefs.GetInt("AutoPlay", 0) == 1, v =>
        {
            PlayerPrefs.SetInt("AutoPlay", v ? 1 : 0);
            PlayerPrefs.Save();
        }));
        gameCard.Add(BuildToggleRow("跳过已读文本", "已读过的对话自动跳过", PlayerPrefs.GetInt("SkipRead", 1) == 1, v =>
        {
            PlayerPrefs.SetInt("SkipRead", v ? 1 : 0);
            PlayerPrefs.Save();
        }));
        gameCard.Add(BuildToggleRow("点击对话框推进", "点击对话框区域触发下一句", PlayerPrefs.GetInt("ClickAdvance", 1) == 1, v =>
        {
            PlayerPrefs.SetInt("ClickAdvance", v ? 1 : 0);
            PlayerPrefs.Save();
        }));
        gameCard.Add(BuildToggleRow("确认对话框", "关闭游戏或退出章节时显示确认", PlayerPrefs.GetInt("ConfirmDialog", 1) == 1, v =>
        {
            PlayerPrefs.SetInt("ConfirmDialog", v ? 1 : 0);
            PlayerPrefs.Save();
        }));
        gameCard.Add(BuildSliderRow("自动播放间隔", 1, 10, PlayerPrefs.GetInt("AutoInterval", 3), v =>
        {
            PlayerPrefs.SetInt("AutoInterval", v);
            PlayerPrefs.Save();
        }, " 秒"));
        rightPane.Add(gameCard);

        AddSectionHeader(rightPane, "数据管理与知识图鉴");
        var dataCard = BuildWhiteCard();
        dataCard.Add(BuildWhiteRow("本地数据备份与恢复", "快照 VN 剧目存档、成就与书签记录（不含经营运行态）", () => ShowBackupRestorePage()));
        dataCard.Add(BuildWhiteRow("铁路百科与专有名词图鉴", "30 条完整术语词条 · 角色口吻释义", () => ShowTipsPage(null)));
        rightPane.Add(dataCard);
    }

    // 24 反馈：车载事务面板入口与离线反馈渠道说明
    private static void RenderFeedbackSettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-feedback-page" };
        AddCenterHeader(page, "反馈");

        AddSectionHeader(page, "反馈提交状态");
        var feedbackCard = BuildWhiteCard();
        feedbackCard.name = "settings-feedback-status-card";
        feedbackCard.Add(BuildWhiteRow("反馈表单", "当前版本未接入反馈收件端；此页面不会发送或保存反馈内容", null, false));
        feedbackCard.Add(BuildWhiteRow("提交渠道", "本机没有配置可验证的调度电话、工单地址或外部反馈入口", null, false));
        page.Add(feedbackCard);

        AddSectionHeader(page, "本机运行信息");
        var channelCard = BuildWhiteCard();
        channelCard.name = "settings-feedback-channel-card";
        channelCard.Add(BuildWhiteRow("应用版本", Application.version, null, false));
        channelCard.Add(BuildWhiteRow("日志与诊断", "本页面不提供日志导出或诊断包提交功能", null, false));
        page.Add(channelCard);

        rightPane.Add(page);
    }

    // 形态 B · 普通详情页 / 规范占位页
    private static void RenderPlaceholderCategory(ScrollView rightPane, int catId)
    {
        string catName = "设置项";
        foreach (var c in SettingsCategories) if (c.id == catId) { catName = c.name; break; }

        AddCenterHeader(rightPane, catName);
        AddSectionHeader(rightPane, catName + " 选项");

        var card = BuildWhiteCard();
        card.Add(BuildWhiteRow(catName + " 基本配置", "此功能尚未开放", () => ShowToast("此功能尚未开放")));
        card.Add(BuildWhiteRow("高级选项与网络策略", "此功能尚未开放", () => ShowToast("此功能尚未开放")));
        card.Add(BuildWhiteRow("权限与策略说明", "此功能尚未开放", () => ShowToast("此功能尚未开放")));
        rightPane.Add(card);
    }

    // 辅助控件工具函数
    private static void AddCenterHeader(VisualElement parent, string title)
    {
        var h = new Label(title);
        h.style.fontSize = 22; h.style.color = InkPrimary;
        h.style.unityFontStyleAndWeight = FontStyle.Bold; h.style.unityFontDefinition = Fd();
        h.style.alignSelf = Align.Center; h.style.marginBottom = 16;
        parent.Add(h);
    }

    private static void AddSectionHeader(VisualElement parent, string title)
    {
        var h = new Label(title);
        h.style.fontSize = 13; h.style.color = SectionHeaderColor;
        h.style.unityFontStyleAndWeight = FontStyle.Bold; h.style.unityFontDefinition = Fd();
        h.style.marginBottom = 8; h.style.marginLeft = 6;
        parent.Add(h);
    }

    private static VisualElement BuildWhiteCard()
    {
        var card = new VisualElement();
        card.style.backgroundColor = SurfaceCard;
        card.style.borderTopLeftRadius = 14; card.style.borderTopRightRadius = 14;
        card.style.borderBottomLeftRadius = 14; card.style.borderBottomRightRadius = 14;
        card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
        card.style.borderTopColor = BorderCard; card.style.borderBottomColor = BorderCard;
        card.style.borderLeftColor = BorderCard; card.style.borderRightColor = BorderCard;
        card.style.paddingLeft = 16; card.style.paddingRight = 16;
        card.style.paddingTop = 8; card.style.paddingBottom = 8;
        card.style.marginBottom = 14;
        return card;
    }

    private static VisualElement BuildWhiteRow(string title, string desc = null, System.Action onClick = null, bool hasArrow = true)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 10; row.style.paddingBottom = 10;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = BorderDivider;
        row.pickingMode = onClick != null ? PickingMode.Position : PickingMode.Ignore;

        if (onClick != null)
        {
            row.RegisterCallback<PointerEnterEvent>(_ => row.style.backgroundColor = SurfaceHover);
            row.RegisterCallback<PointerLeaveEvent>(_ => row.style.backgroundColor = Color.clear);
            row.RegisterCallback<ClickEvent>(_ => onClick());
        }

        var left = new VisualElement();
        left.style.flexGrow = 1;
        row.Add(left);

        var t = new Label(title);
        t.style.fontSize = 15; t.style.color = InkPrimary;
        t.style.unityFontDefinition = Fd();
        left.Add(t);

        if (!string.IsNullOrEmpty(desc))
        {
            var d = new Label(desc);
            d.style.fontSize = 12; d.style.color = InkSecondary;
            d.style.unityFontDefinition = Fd(); d.style.marginTop = 2;
            left.Add(d);
        }

        if (hasArrow)
        {
            var ar = new Label("›");
            ar.style.fontSize = 18; ar.style.color = InkTertiary; ar.style.unityFontDefinition = Fd();
            row.Add(ar);
        }
        return row;
    }

    private static VisualElement BuildToggleRow(string title, string desc, bool initialVal, System.Action<bool> onChanged)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 10; row.style.paddingBottom = 10;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = BorderDivider;

        var left = new VisualElement();
        left.style.flexGrow = 1;
        row.Add(left);

        var t = new Label(title);
        t.style.fontSize = 15; t.style.color = InkPrimary;
        t.style.unityFontDefinition = Fd();
        left.Add(t);

        if (!string.IsNullOrEmpty(desc))
        {
            var d = new Label(desc);
            d.style.fontSize = 12; d.style.color = InkSecondary;
            d.style.unityFontDefinition = Fd(); d.style.marginTop = 2;
            left.Add(d);
        }

        // 胶囊开关
        bool val = initialVal;
        var toggle = new VisualElement();
        toggle.style.width = 46; toggle.style.height = 26;
        toggle.style.borderTopLeftRadius = 13; toggle.style.borderTopRightRadius = 13;
        toggle.style.borderBottomLeftRadius = 13; toggle.style.borderBottomRightRadius = 13;
        toggle.style.backgroundColor = val ? AccentSystem : new Color(0.85f, 0.85f, 0.87f, 1f);
        toggle.style.justifyContent = Justify.Center;
        toggle.pickingMode = PickingMode.Position;
        row.Add(toggle);

        var thumb = new VisualElement();
        thumb.style.width = 20; thumb.style.height = 20;
        thumb.style.borderTopLeftRadius = 10; thumb.style.borderTopRightRadius = 10;
        thumb.style.borderBottomLeftRadius = 10; thumb.style.borderBottomRightRadius = 10;
        thumb.style.backgroundColor = Color.white;
        thumb.style.marginLeft = val ? 23 : 3;
        toggle.Add(thumb);

        toggle.RegisterCallback<ClickEvent>(_ =>
        {
            val = !val;
            toggle.style.backgroundColor = val ? AccentSystem : new Color(0.85f, 0.85f, 0.87f, 1f);
            thumb.style.marginLeft = val ? 23 : 3;
            if (onChanged != null) onChanged(val);
        });

        return row;
    }

    private static VisualElement BuildSliderRow(string title, int min, int max, int current, System.Action<int> onChanged, string unit = "", bool enabled = true)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 10; row.style.paddingBottom = 10;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = BorderDivider;

        var t = new Label(title);
        t.style.fontSize = 15; t.style.color = enabled ? InkPrimary : InkTertiary;
        t.style.unityFontDefinition = Fd();
        row.Add(t);

        var rightCol = new VisualElement();
        rightCol.style.flexDirection = FlexDirection.Row;
        rightCol.style.alignItems = Align.Center;
        row.Add(rightCol);

        var valLbl = new Label(current + unit);
        valLbl.style.fontSize = 13; valLbl.style.color = enabled ? AccentSystem : InkTertiary;
        valLbl.style.unityFontDefinition = Fd(); valLbl.style.width = 54;
        valLbl.style.unityTextAlign = TextAnchor.MiddleRight;
        rightCol.Add(valLbl);

        var slider = new SliderInt(min, max) { value = current };
        slider.style.width = 160; slider.style.marginLeft = 10;
        slider.SetEnabled(enabled);
        slider.RegisterValueChangedCallback(evt =>
        {
            valLbl.text = evt.newValue + unit;
            if (onChanged != null) onChanged(evt.newValue);
        });
        rightCol.Add(slider);

        return row;
    }

    private static void StyleActionBtn(Button btn)
    {
        btn.style.height = 36; btn.style.marginTop = 14;
        btn.style.backgroundColor = SurfaceApp;
        btn.style.color = AccentSystem;
        btn.style.fontSize = 14; btn.style.unityFontDefinition = Fd();
        btn.style.borderTopLeftRadius = 8; btn.style.borderTopRightRadius = 8;
        btn.style.borderBottomLeftRadius = 8; btn.style.borderBottomRightRadius = 8;
        btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
        btn.style.borderTopColor = BorderCard; btn.style.borderBottomColor = BorderCard;
        btn.style.borderLeftColor = BorderCard; btn.style.borderRightColor = BorderCard;
        btn.style.alignSelf = Align.Center;
        btn.style.paddingLeft = 20; btn.style.paddingRight = 20;
    }

    public static void ShowBackupRestorePage(VisualElement hostContainer = null)
    {
        VisualElement win;
        if (hostContainer != null)
        {
            win = new VisualElement { name = "backup-restore-page" };
            win.style.flexGrow = 1;
            hostContainer.Add(win);
        }
        else
        {
            win = BuildInnerPanel("备份与恢复", () => ShowBackupRestorePage());
        }
        if (win == null) return;
        win.name = "backup-restore-page";
        win.style.height = 540;

        var tip = new Label("本机档案备份系统：将主线剧目阅读进度、剧目存档、成就与书签快照备份至本机");
        tip.name = "backup-tip-label";
        tip.style.fontSize = 14;
        tip.style.color = GlassTextDim;
        tip.style.unityFontDefinition = Fd();
        tip.style.marginTop = 10;
        win.Add(tip);

        var scopeLabel = new Label("支持范围：主线剧目进度 (VN_Save_0..60)、故事槽位 (SaveSlot_0..5)、成就 (Achievements_Data) 与书签记录 (Bookmark_Auto_0..4、Bookmark_ManualIndex 及所列手动书签)。注意：不含金钱/天数等经营运行态、音乐、系统设置或账号资料；未在快照中的档案槽位与手动书签将在恢复时清理，恢复于相关剧目下次读取时生效。");
        scopeLabel.name = "backup-scope-label";
        scopeLabel.style.fontSize = 12;
        scopeLabel.style.color = GlassTextDim;
        scopeLabel.style.unityFontDefinition = Fd();
        scopeLabel.style.marginTop = 4;
        scopeLabel.style.whiteSpace = WhiteSpace.Normal;
        win.Add(scopeLabel);

        // 备份状态行
        var statusLabel = new Label(GetBackupStatusText());
        statusLabel.name = "backup-status-label";
        statusLabel.style.fontSize = 15;
        statusLabel.style.color = new Color(0.7f, 0.9f, 0.7f, 0.85f);
        statusLabel.style.unityFontDefinition = Fd();
        statusLabel.style.marginTop = 14;
        win.Add(statusLabel);

        // 确认面板容器（确认前隐藏，避免直接写档）
        var confirmBox = new VisualElement { name = "backup-confirm-box" };
        confirmBox.style.display = DisplayStyle.None;
        confirmBox.style.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 0.95f);
        confirmBox.style.borderTopLeftRadius = 8; confirmBox.style.borderTopRightRadius = 8;
        confirmBox.style.borderBottomLeftRadius = 8; confirmBox.style.borderBottomRightRadius = 8;
        confirmBox.style.borderTopWidth = 1; confirmBox.style.borderBottomWidth = 1;
        confirmBox.style.borderLeftWidth = 1; confirmBox.style.borderRightWidth = 1;
        confirmBox.style.borderTopColor = AccentSystem; confirmBox.style.borderBottomColor = AccentSystem;
        confirmBox.style.borderLeftColor = AccentSystem; confirmBox.style.borderRightColor = AccentSystem;
        confirmBox.style.paddingTop = 12; confirmBox.style.paddingBottom = 12;
        confirmBox.style.paddingLeft = 14; confirmBox.style.paddingRight = 14;
        confirmBox.style.marginTop = 12;

        var confirmTitle = new Label("⚠️ 恢复存档二次确认");
        confirmTitle.style.fontSize = 15; confirmTitle.style.color = AccentSystem;
        confirmTitle.style.unityFontStyleAndWeight = FontStyle.Bold; confirmTitle.style.unityFontDefinition = Fd();
        confirmBox.Add(confirmTitle);

        var confirmDesc = new Label("恢复将按快照精确还原主线剧目存档、故事槽位、成就与书签记录，未包含在快照中的档案槽位与当前多余手动书签将被清除。不影响金钱与经营运行态。若取消，档案不做任何变更。是否继续？");
        confirmDesc.name = "backup-confirm-desc";
        confirmDesc.style.fontSize = 12; confirmDesc.style.color = GlassText;
        confirmDesc.style.unityFontDefinition = Fd(); confirmDesc.style.marginTop = 4;
        confirmDesc.style.whiteSpace = WhiteSpace.Normal;
        confirmBox.Add(confirmDesc);

        var confirmBtnRow = new VisualElement();
        confirmBtnRow.style.flexDirection = FlexDirection.Row;
        confirmBtnRow.style.justifyContent = Justify.FlexEnd;
        confirmBtnRow.style.marginTop = 10;

        var btnCancel = new Button(() =>
        {
            confirmBox.style.display = DisplayStyle.None;
            ShowToast("已取消恢复操作，档案未作任何变更");
        }) { text = "取消", name = "btn-backup-cancel" };
        btnCancel.style.height = 32; btnCancel.style.paddingLeft = 16; btnCancel.style.paddingRight = 16;
        btnCancel.style.marginRight = 10;
        confirmBtnRow.Add(btnCancel);

        var btnConfirm = new Button(() =>
        {
            confirmBox.style.display = DisplayStyle.None;
            string rawBackup = StorageAdapter.GetString(BackupJsonKey, "");
            bool success = RestoreBackup(rawBackup, out string errorMsg, out int restoredCount);
            if (success)
            {
                ShowToast(errorMsg);
            }
            else
            {
                ShowToast("恢复失败：" + errorMsg);
            }
        }) { text = "确认恢复", name = "btn-backup-confirm" };
        btnConfirm.style.height = 32; btnConfirm.style.paddingLeft = 16; btnConfirm.style.paddingRight = 16;
        btnConfirm.style.backgroundColor = new Color(0.7f, 0.25f, 0.25f, 1f);
        btnConfirm.style.color = Color.white;
        confirmBtnRow.Add(btnConfirm);
        confirmBox.Add(confirmBtnRow);

        var btnBackup = new Button(() =>
        {
            SaveBackup();
            statusLabel.text = GetBackupStatusText();
            ShowToast("备份完成，已生成本机结构化快照 (Schema v2)");
        }) { text = "立即备份", name = "btn-backup-now" };
        StyleRowBtn(btnBackup);
        win.Add(btnBackup);

        var btnRestore = new Button(() =>
        {
            string rawBackup = StorageAdapter.GetString(BackupJsonKey, "");
            if (string.IsNullOrEmpty(rawBackup))
            {
                ShowToast("暂无备份可恢复");
                return;
            }
            confirmBox.style.display = DisplayStyle.Flex;
        }) { text = "恢复上次备份", name = "btn-backup-restore" };
        StyleRowBtn(btnRestore);
        win.Add(btnRestore);

        win.Add(confirmBox);

        var cloudRow = BuildWhiteRow("云备份", "阿里郎内网账户（未开放 · 仅支持本机备份）", null, false);
        win.Add(cloudRow);

        // 版本信息
        var verLabel = new Label("澎湃OS 32 · DPRK Edition");
        verLabel.style.fontSize = 12;
        verLabel.style.color = GlassTextMuted;
        verLabel.style.unityFontDefinition = Fd();
        verLabel.style.marginTop = 20;
        win.Add(verLabel);
    }

    // ============ 术语图鉴（TIPS）============
    [System.Serializable] private class TipsJsonData { public TipsTerm[] terms; }
    [System.Serializable] private class TipsTerm { public string id; public string name; public string py; public TipsExplanation[] explanations; }
    [System.Serializable] private class TipsExplanation { public string speaker; public string text; }

    // 说话者 → Resources/characters 立绘前缀（与 CharacterSpriteManager.SpeakerAlias 同源；无贴图的角色仅显示名字 chip）
    private static readonly System.Collections.Generic.Dictionary<string, string> TipsPortraitPrefix =
        new System.Collections.Generic.Dictionary<string, string>
        {
            { "男大学生", "lin_biaohan" },
            { "林彪悍", "lin_biaohan" },
            { "老陈", "laochen" },
            { "岁月", "suiyue" },
            { "嘉颖徐", "jiaying_xu" },
            { "张工", "zhanggong" },
        };

    /// <summary>术语图鉴入口（静态）：确保桌面/App层就绪、跳过锁屏直达图鉴。
    /// termId 非空 → 右侧详情定位该词条；null → 浏览模式（提示从左侧选择）。</summary>
    private static Texture2D LoadTipsPortrait(string speaker)
    {
        if (string.IsNullOrEmpty(speaker)) return null;
        if (!TipsPortraitPrefix.TryGetValue(speaker, out var prefix)) return null;
        return Resources.Load<Texture2D>("characters/" + prefix);
    }

    /// <summary>Terms.json 全量词条（带 py），按拼音首字母、再按名称排序。</summary>
    private static System.Collections.Generic.List<TipsTerm> LoadTipsTerms()
    {
        var list = new System.Collections.Generic.List<TipsTerm>();
        var asset = Resources.Load<TextAsset>("Terms");
        if (asset == null) return list;
        try
        {
            var data = JsonUtility.FromJson<TipsJsonData>(asset.text);
            if (data != null && data.terms != null) list.AddRange(data.terms);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Tips] Terms.json parse failed: " + ex.Message);
        }
        list.Sort((a, b) =>
        {
            string pa = string.IsNullOrEmpty(a.py) ? "~" : a.py;
            string pb = string.IsNullOrEmpty(b.py) ? "~" : b.py;
            int c = string.CompareOrdinal(pa, pb);
            return c != 0 ? c : string.CompareOrdinal(a.name, b.name);
        });
        return list;
    }

    /// <summary>左索引右详情（Maitetsu TIPS 式，平板玻璃风）。</summary>
    public static void ShowTipsPage(string termId = null)
    {
        string selectedTermId = termId;
        var win = BuildInnerPanel("术语图鉴", () => ShowTipsPage(selectedTermId));
        if (win == null) return;
        win.style.width = 900;
        win.style.height = 560;

        var terms = LoadTipsTerms();

        var body = new VisualElement();
        body.style.flexDirection = FlexDirection.Row;
        body.style.flexGrow = 1;
        body.style.marginTop = 12;
        win.Add(body);

        // 左：按拼音首字母分组的可滚动索引
        var indexScroll = new ScrollView { name = "tips-index" };
        indexScroll.style.width = 230;
        indexScroll.style.flexShrink = 0;
        indexScroll.style.marginRight = 16;
        indexScroll.style.backgroundColor = GlassBgSoft;
        indexScroll.style.borderTopLeftRadius = 10; indexScroll.style.borderTopRightRadius = 10;
        indexScroll.style.borderBottomLeftRadius = 10; indexScroll.style.borderBottomRightRadius = 10;
        indexScroll.style.paddingLeft = 8; indexScroll.style.paddingRight = 8;
        indexScroll.style.paddingTop = 8; indexScroll.style.paddingBottom = 8;
        body.Add(indexScroll);

        // 右：详情（可滚动）
        var detail = new ScrollView { name = "tips-detail" };
        detail.style.flexGrow = 1;
        detail.style.paddingLeft = 14; detail.style.paddingRight = 10;
        body.Add(detail);

        var placeholder = new Label("← 点击左侧词条，查看角色口吻详解");
        placeholder.style.fontSize = 14;
        placeholder.style.color = GlassTextMuted;
        placeholder.style.unityFontDefinition = Fd();
        placeholder.style.marginTop = 24;
        detail.Add(placeholder);

        Label selectedRow = null;
        TipsTerm initial = null;
        string curPy = null;
        foreach (var t in terms)
        {
            string py = string.IsNullOrEmpty(t.py) ? "#" : t.py;
            if (py != curPy)
            {
                curPy = py;
                var head = new Label(py);
                head.style.fontSize = 13;
                head.style.color = GlassAccent;
                head.style.unityFontDefinition = Fd();
                head.style.marginTop = 10;
                head.style.marginBottom = 2;
                indexScroll.Add(head);
            }

            var row = new Label(t.name) { name = "tips-term-" + t.id };
            row.style.fontSize = 15;
            row.style.color = GlassText;
            row.style.unityFontDefinition = Fd();
            row.style.paddingLeft = 10; row.style.paddingRight = 10;
            row.style.paddingTop = 6; row.style.paddingBottom = 6;
            row.style.borderTopLeftRadius = 6; row.style.borderTopRightRadius = 6;
            row.style.borderBottomLeftRadius = 6; row.style.borderBottomRightRadius = 6;
            var captured = t;
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (selectedRow != null) selectedRow.style.backgroundColor = StyleKeyword.Null;
                selectedRow = row;
                selectedTermId = captured.id;
                row.style.backgroundColor = GlassBgHover;
                detail.Clear();
                RenderTipsDetail(detail, captured);
            });
            indexScroll.Add(row);
            if (termId != null && t.id == termId)
            {
                initial = t;
                selectedRow = row;
                row.style.backgroundColor = GlassBgHover;
            }
        }

        if (initial != null)
        {
            RenderTipsDetail(detail, initial);
            var scrollTo = selectedRow;
            indexScroll.schedule.Execute(() => indexScroll.ScrollTo(scrollTo));
        }
    }

    /// <summary>右侧详情：词条名标题条 + 每段（立绘小图+角色 chip + 释义全文，文中其它术语包金）。</summary>
    private static void RenderTipsDetail(ScrollView detail, TipsTerm t)
    {
        var titleBar = new VisualElement();
        titleBar.style.flexDirection = FlexDirection.Row;
        titleBar.style.alignItems = Align.Center;
        titleBar.style.backgroundColor = new Color(0.16f, 0.10f, 0.06f, 0.9f);
        titleBar.style.borderTopLeftRadius = 6; titleBar.style.borderTopRightRadius = 6;
        titleBar.style.borderBottomLeftRadius = 6; titleBar.style.borderBottomRightRadius = 6;
        titleBar.style.paddingLeft = 12; titleBar.style.paddingRight = 12;
        titleBar.style.paddingTop = 6; titleBar.style.paddingBottom = 6;
        var title = new Label(t.name) { name = "tips-detail-title" };
        title.style.fontSize = 20;
        title.style.color = new Color(1f, 0.86f, 0.59f, 1f);
        title.style.unityFontDefinition = Fd();
        titleBar.Add(title);
        if (!string.IsNullOrEmpty(t.py))
        {
            var pyTag = new Label(t.py);
            pyTag.style.fontSize = 12;
            pyTag.style.color = new Color(1f, 0.86f, 0.59f, 0.7f);
            pyTag.style.unityFontDefinition = Fd();
            pyTag.style.marginLeft = 10;
            titleBar.Add(pyTag);
        }
        detail.Add(titleBar);

        if (t.explanations == null) return;
        foreach (var ex in t.explanations)
        {
            if (ex == null) continue;

            if (!string.IsNullOrEmpty(ex.speaker))
            {
                var chipRow = new VisualElement();
                chipRow.style.flexDirection = FlexDirection.Row;
                chipRow.style.alignItems = Align.Center;
                chipRow.style.marginTop = 12;
                chipRow.style.marginBottom = 4;

                var portrait = LoadTipsPortrait(ex.speaker);
                if (portrait != null)
                {
                    var img = new Image { image = portrait };
                    img.style.width = 28; img.style.height = 28;
                    img.style.borderTopLeftRadius = 14; img.style.borderTopRightRadius = 14;
                    img.style.borderBottomLeftRadius = 14; img.style.borderBottomRightRadius = 14;
                    img.style.borderTopWidth = 1; img.style.borderBottomWidth = 1;
                    img.style.borderLeftWidth = 1; img.style.borderRightWidth = 1;
                    img.style.borderTopColor = GlassBorderSoft; img.style.borderBottomColor = GlassBorderSoft;
                    img.style.borderLeftColor = GlassBorderSoft; img.style.borderRightColor = GlassBorderSoft;
                    img.style.marginRight = 8;
                    chipRow.Add(img);
                }

                var chip = new Label(ex.speaker);
                chip.style.fontSize = 13;
                chip.style.color = new Color(1f, 0.86f, 0.59f, 1f);
                chip.style.backgroundColor = new Color(0.16f, 0.10f, 0.06f, 0.9f);
                chip.style.paddingLeft = 8; chip.style.paddingRight = 8;
                chip.style.paddingTop = 3; chip.style.paddingBottom = 3;
                chip.style.borderTopLeftRadius = 4; chip.style.borderTopRightRadius = 4;
                chip.style.borderBottomLeftRadius = 4; chip.style.borderBottomRightRadius = 4;
                chip.style.unityFontDefinition = Fd();
                chipRow.Add(chip);
                detail.Add(chipRow);
            }

            var bodyText = new Label(TermHighlightSystem.HighlightTerms(ex.text));
            bodyText.style.fontSize = 14;
            bodyText.style.color = new Color(1f, 1f, 1f, 0.92f);
            bodyText.style.unityFontDefinition = Fd();
            bodyText.style.whiteSpace = WhiteSpace.Normal;
            bodyText.style.marginTop = 5;
            bodyText.style.marginBottom = 4;
            detail.Add(bodyText);
        }
    }

    private static void StyleRowBtn(Button btn)
    {
        btn.style.width = 220; btn.style.height = 44;
        btn.style.fontSize = 16; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.backgroundColor = new Color(0.2f, 0.35f, 0.2f, 0.55f);
        btn.style.color = new Color(0.85f, 1f, 0.85f, 0.9f);
        btn.style.borderTopLeftRadius = 10; btn.style.borderTopRightRadius = 10;
        btn.style.borderBottomLeftRadius = 10; btn.style.borderBottomRightRadius = 10;
        btn.style.marginTop = 16;
        btn.style.alignSelf = Align.Center;
    }

    private static string GetBackupStatusText()
    {
        string t = StorageAdapter.GetString(BackupTimeKey, "");
        return string.IsNullOrEmpty(t) ? "尚未备份" : "上次备份：" + t;
    }

    // ============ 结构化版本化本地备份模型 ============
    [System.Serializable]
    public class BackupItem
    {
        public string key;
        public string val;
    }

    [System.Serializable]
    public class TabletBackupData
    {
        public int version = 1;
        public string createdAt = "";
        public List<BackupItem> items = new List<BackupItem>();
    }

    public static readonly string[] WhitelistedBackupKeys = TabletLocalBackupService.FixedWhitelistKeys;

    public static string CreateBackupPayload()
    {
        return TabletLocalBackupService.CreateBackupPayload(StorageAdapter);
    }

    public static void SaveBackup()
    {
        string json = CreateBackupPayload();
        string timeStr = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm");
        StorageAdapter.SetString(BackupJsonKey, json);
        StorageAdapter.SetString(BackupTimeKey, timeStr);
        StorageAdapter.Save();
    }

    public static bool TryParseBackup(string rawData, out Dictionary<string, string> parsedPairs, out string error)
    {
        return TabletLocalBackupService.TryParseLegacyPairs(rawData, out parsedPairs, out error);
    }

    public static bool RestoreBackup(string rawData, out string errorMessage, out int restoredCount)
    {
        return TabletLocalBackupService.RestoreBackup(StorageAdapter, rawData, out errorMessage, out restoredCount);
    }

    public static void RestoreBackup()
    {
        string raw = StorageAdapter.GetString(BackupJsonKey, "");
        if (string.IsNullOrEmpty(raw)) return;
        RestoreBackup(raw, out _, out _);
    }

    // ============ 应用管理：系统管控彩蛋 ============
    public static void ShowAffairsPage()
    {
        // 事务页（简单占位）
        var panel = BuildInnerPanel("事务", ShowAffairsPage);
        if (panel == null) return;
        var p = new Label("事务功能开发中（绑定经营系统后开放）。");
        p.style.fontSize = 15; p.style.color = GlassTextDim;
        p.style.unityFontDefinition = Fd(); p.style.marginTop = 40;
        panel.Add(p);
    }

    public static void ShowStoryPage()
    {
        var panel = BuildInnerPanel("剧情", ShowStoryPage);
        if (panel == null) return;
        var btn = new Button(() => { if (GameMainUI.Instance != null) GameMainUI.Instance.gameObject.SetActive(true); MainStoryUI.Show(); }) { text = "打开 Main Story" };
        btn.style.width = 240; btn.style.height = 50;
        btn.style.fontSize = 18; btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.backgroundColor = new Color(0.15f, 0.3f, 0.5f, 0.55f);
        btn.style.color = GlassText;
        btn.style.borderTopLeftRadius = 12; btn.style.borderTopRightRadius = 12;
        btn.style.borderBottomLeftRadius = 12; btn.style.borderBottomRightRadius = 12;
        btn.style.marginTop = 50;
        btn.style.alignSelf = Align.Center;
        panel.Add(btn);
    }

    public static void ShowAppManagementPage()
    {
        var win = BuildInnerPanel("应用管理", ShowAppManagementPage);
        if (win == null) return;
        win.style.height = 480;

        // 说明文字
        var tip = new Label("已禁用的系统应用（4个）");
        tip.style.fontSize = 15;
        tip.style.color = GlassTextDim;
        tip.style.unityFontDefinition = Fd();
        tip.style.marginTop = 10;
        win.Add(tip);

        var tip2 = new Label("以下应用因系统管控已被替换。点击可尝试恢复原版。");
        tip2.style.fontSize = 13;
        tip2.style.color = GlassTextMuted;
        tip2.style.unityFontDefinition = Fd();
        tip2.style.marginTop = 4;
        win.Add(tip2);

        var apps = SystemAppsEasterEgg.Apps;
        for (int i = 0; i < apps.Length; i++)
        {
            int idx = i;
            var app = apps[i];
            bool restored = SystemAppsEasterEgg.IsAppRestored(idx);
            bool capsuleTriggered = SystemAppsEasterEgg.IsCapsuleTriggered();

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = GlassBgSoft;
            row.style.borderTopLeftRadius = 8; row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8; row.style.borderBottomRightRadius = 8;
            row.style.paddingLeft = 16; row.style.paddingRight = 16;
            row.style.paddingTop = 10; row.style.paddingBottom = 10;
            row.style.marginTop = 6;

            // 左侧：应用信息
            var left = new VisualElement();
            left.style.flexDirection = FlexDirection.Column;
            row.Add(left);

            var appName = new Label(app.OriginalName);
            appName.style.fontSize = 16;
            appName.style.color = restored
                ? new Color(0.5f, 0.8f, 0.5f, 0.9f)
                : GlassText;
            appName.style.unityFontDefinition = Fd();
            left.Add(appName);

            var appStatus = new Label(restored ? "已恢复" : $"当前：{app.ReplacedName}");
            appStatus.style.fontSize = 12;
            appStatus.style.color = GlassTextMuted;
            appStatus.style.unityFontDefinition = Fd();
            appStatus.style.marginTop = 2;
            left.Add(appStatus);

            // 右侧：操作按钮
            if (capsuleTriggered)
            {
                var locked = new Label("已重置");
                locked.style.fontSize = 13;
                locked.style.color = GlassTextMuted;
                locked.style.unityFontDefinition = Fd();
                row.Add(locked);
            }
            else if (restored)
            {
                var check = new Label("✓");
                check.style.fontSize = 20;
                check.style.color = new Color(0.5f, 0.8f, 0.5f, 0.85f);
                check.style.unityFontDefinition = Fd();
                row.Add(check);
            }
            else
            {
                var btn = new Button(() => OnRestoreApp(idx, win)) { text = "恢复" };
                btn.style.width = 76; btn.style.height = 34;
                btn.style.fontSize = 14;
                btn.style.unityTextAlign = TextAnchor.MiddleCenter;
                btn.style.unityFontDefinition = Fd();
                btn.style.backgroundColor = new Color(0.2f, 0.4f, 0.6f, 0.6f);
                btn.style.color = GlassAccent;
                btn.style.borderTopLeftRadius = 8; btn.style.borderTopRightRadius = 8;
                btn.style.borderBottomLeftRadius = 8; btn.style.borderBottomRightRadius = 8;
                row.Add(btn);
            }

            win.Add(row);
        }

        // 时间胶囊状态提示
        int restoredCount = SystemAppsEasterEgg.GetRestoredCount();
        if (restoredCount > 0 && !SystemAppsEasterEgg.IsCapsuleTriggered())
        {
            var hint = new Label($"已恢复 {restoredCount}/4 个应用");
            hint.style.fontSize = 14;
            hint.style.color = new Color(1f, 0.7f, 0.4f, 0.7f);
            hint.style.unityFontDefinition = Fd();
            hint.style.marginTop = 16;
            win.Add(hint);
        }
    }

    private static void OnRestoreApp(int index, VisualElement settingsWin)
    {
        var result = SystemAppsEasterEgg.RestoreApp(index);

        switch (result)
        {
            case SystemAppsEasterEgg.RestoreResult.Success:
                ShowToast($"已恢复 {SystemAppsEasterEgg.Apps[index].OriginalName}！");
                ShowAppManagementPage();
                break;
            case SystemAppsEasterEgg.RestoreResult.CapsuleTriggered:
                ShowToast("恭喜！所有系统应用已恢复！");
                ShowAppManagementPage();
                break;
            case SystemAppsEasterEgg.RestoreResult.AlreadyRestored:
                ShowToast("该应用已经恢复过了。");
                break;
            case SystemAppsEasterEgg.RestoreResult.InvalidIndex:
                ShowToast("恢复失败，权限不足。");
                break;
        }
    }
    }
}
