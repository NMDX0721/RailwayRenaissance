using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
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
    private const string BackupJsonKey = "SUIYUE_Backup_Json";
    private const string BackupTimeKey = "SUIYUE_Backup_Time";

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
    public static Action<KeyCode> TabletRebindAction;

    private static void ShowToast(string msg)
    {
        if (s_showToast != null) s_showToast(msg);
        else Debug.Log("[Settings] " + msg);
    }

    private static VisualElement BuildInnerPanel(string title)
    {
        if (GameMainUI.Instance != null) return GameMainUI.Instance.BuildInnerPanel(title);
        return null;
    }

    public static void BuildSettingsPage(VisualElement win, Action<string> showToast = null)
    {
        win.Clear();

        var rootRow = new VisualElement();
        rootRow.style.flexDirection = FlexDirection.Row;
        rootRow.style.flexGrow = 1;
        win.Add(rootRow);

        // 左栏：300px 纯白/极浅灰导航条，含搜索框与 24 项
        var leftNav = new VisualElement();
        leftNav.style.width = 300;
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
        searchInput.RegisterValueChangedCallback(evt =>
        {
            settingsSearchFilter = evt.newValue;
            BuildSettingsPage(win);
        });
        searchBox.Add(searchInput);

        // 分类滚动列表（隐藏滚动条）
        var navScroll = new ScrollView();
        navScroll.style.flexGrow = 1;
        navScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        navScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        leftNav.Add(navScroll);

        string lastGroup = "";
        for (int i = 0; i < SettingsCategories.Length; i++)
        {
            var cat = SettingsCategories[i];
            if (!string.IsNullOrEmpty(settingsSearchFilter) && !cat.name.Contains(settingsSearchFilter))
                continue;

            if (cat.group != lastGroup && i > 0)
            {
                lastGroup = cat.group;
                var div = new VisualElement();
                div.style.height = 1; div.style.backgroundColor = BorderDivider;
                div.style.marginTop = 6; div.style.marginBottom = 6;
                navScroll.Add(div);
            }
            else if (i == 0) lastGroup = cat.group;

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

        // 右栏：内容详情区（隐藏滚动条）
        var rightPane = new ScrollView();
        rightPane.style.flexGrow = 1;
        rightPane.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        rightPane.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        rightPane.style.paddingLeft = 32; rightPane.style.paddingRight = 32;
        rightPane.style.paddingTop = 20; rightPane.style.paddingBottom = 30;
        rightPane.style.backgroundColor = SurfaceApp;
        rootRow.Add(rightPane);

        RenderSettingsDetail(rightPane, win);
    }

    private static void RenderSettingsDetail(ScrollView rightPane, VisualElement win)
    {
        switch (currentSettingsCategory)
        {
            case 1: RenderAccountSettings(rightPane); break;
            case 2: RenderMyDeviceSettings(rightPane, win); break;
            case 10: RenderLockScreenSettings(rightPane); break;
            case 12: RenderHomeScreenSettings(rightPane); break;
            case 13: RenderDisplaySettings(rightPane, win); break;
            case 14: RenderAudioSettings(rightPane, win); break;
            case 15: RenderControlSettings(rightPane, win); break;
            case 18: RenderAppsSettings(rightPane); break;
            case 22: RenderAccessibilitySettings(rightPane, win); break;
            case 23: RenderAdditionalSettings(rightPane); break;
            case 99: RenderDeveloperOptions(rightPane, win); break;
            default: RenderPlaceholderCategory(rightPane, currentSettingsCategory); break;
        }
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
        specsCard.Add(BuildWhiteRow("安全防护固件", "白头疫苗 SiliVaccine 8.2 (军工高权限固化)", null, false));
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
        ecoResetCard.Add(BuildWhiteRow("重置经济模拟数据", "恢复初始状态：资金 50,000 沙币，信任 62，车况 70，第 1 天", () =>
        {
            GameData.ResetState();
            ShowToast("💰 经济核与车况数据已重置为初始运营状态！");
        }));

        ecoResetCard.Add(BuildWhiteRow("注入调试测试资金", "为当前账务追加 100,000 沙币并恢复信任", () =>
        {
            GameData.AddMoney(100000);
            GameData.AddTrust(30);
            ShowToast("💳 已注资 100,000 沙币，信任已提升！");
        }));
        rightPane.Add(ecoResetCard);

        AddSectionHeader(rightPane, "出厂重置与开发者模式");
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

        devDangerCard.Add(BuildWhiteRow("恢复出厂设置 (清空全部本地数据)", "清空所有 PlayerPrefs 数据并重新加载系统", () =>
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            isTabletDevModeUnlocked = false;
            currentSettingsCategory = 1;
            ShowToast("⚠️ 已清除所有本地存储，恢复系统出厂设置。");
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
        card.Add(BuildWhiteRow("锁屏时钟样式", "主体历 / 公历 双排时钟", () => ShowToast("已使用默认时钟样式")));
        card.Add(BuildWhiteRow("人脸数据录入", "已通过 3D 结构光特征认证", () => ShowToast("面部特征已注册")));
        card.Add(BuildToggleRow("抬起唤醒", "拿起平板时自动点亮屏幕", true, null));
        card.Add(BuildToggleRow("双击亮屏与熄屏", "在锁屏界面空白处双击熄屏", true, null));
        rightPane.Add(card);
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
        card.Add(BuildWhiteRow("桌面图标排布", "双页网格 + 底部液态玻璃 Dock 栏", null, false));
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
            foreach (KeyBindings.Action action in System.Enum.GetValues(typeof(KeyBindings.Action)))
                KeyBindings.SetKey(action, KeyBindings.GetKey(action));
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
        card.Add(BuildToggleRow("无障碍快捷方式", "同时按住音量加减键 3 秒开启", false, _ => ShowToast("此功能尚未开放")));
        card.Add(BuildToggleRow("高对比度文本", "增强浅色界面的文字辨识度", false, _ => ShowToast("此功能尚未开放")));
        card.Add(BuildWhiteRow("TalkBack 语音助手", "屏幕内容读屏朗读服务（未安装语言包）", () => ShowToast("此功能尚未开放")));
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
        dataCard.Add(BuildWhiteRow("本地数据备份与恢复", "快照经营数据与剧目存档（参考小米备份）", ShowBackupRestorePage));
        dataCard.Add(BuildWhiteRow("铁路百科与专有名词图鉴", "30 条完整术语词条 · 角色口吻释义", () => ShowTipsPage(null)));
        rightPane.Add(dataCard);
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

    public static void ShowBackupRestorePage()
    {
        var win = BuildInnerPanel("备份与恢复");
        win.style.height = 480;

        var tip = new Label("参考小米备份系统：将阅读进度与经营数据备份到本机");
        tip.style.fontSize = 14;
        tip.style.color = GlassTextDim;
        tip.style.unityFontDefinition = Fd();
        tip.style.marginTop = 10;
        win.Add(tip);

        // 备份状态行
        var statusLabel = new Label(GetBackupStatusText());
        statusLabel.style.fontSize = 15;
        statusLabel.style.color = new Color(0.7f, 0.9f, 0.7f, 0.85f);
        statusLabel.style.unityFontDefinition = Fd();
        statusLabel.style.marginTop = 14;
        win.Add(statusLabel);

        var btnBackup = new Button(() =>
        {
            SaveBackup();
            statusLabel.text = GetBackupStatusText();
            ShowToast("备份完成");
        }) { text = "立即备份" };
        StyleRowBtn(btnBackup);
        win.Add(btnBackup);

        var btnRestore = new Button(() =>
        {
            if (string.IsNullOrEmpty(PlayerPrefs.GetString(BackupJsonKey, "")))
            {
                ShowToast("暂无备份可恢复");
                return;
            }
            RestoreBackup();
            ShowToast("恢复完成");
        }) { text = "恢复上次备份" };
        StyleRowBtn(btnRestore);
        win.Add(btnRestore);

        var cloudRow = BuildWhiteRow("云备份", "阿里郎内网账户（未开放）", null, false);
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
        var win = BuildInnerPanel("术语图鉴");
        win.style.width = 900;
        win.style.height = 560;

        var terms = LoadTipsTerms();

        var body = new VisualElement();
        body.style.flexDirection = FlexDirection.Row;
        body.style.flexGrow = 1;
        body.style.marginTop = 12;
        win.Add(body);

        // 左：按拼音首字母分组的可滚动索引
        var indexScroll = new ScrollView();
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
        var detail = new ScrollView();
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

            var row = new Label(t.name);
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
        var title = new Label(t.name);
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
            if (ex == null || string.IsNullOrEmpty(ex.text)) continue;
            if (!string.IsNullOrEmpty(ex.speaker))
            {
                var chipRow = new VisualElement();
                chipRow.style.flexDirection = FlexDirection.Row;
                chipRow.style.alignItems = Align.Center;
                chipRow.style.marginTop = 12;

                var tex = LoadTipsPortrait(ex.speaker);
                if (tex != null)
                {
                    // 立绘小图：按原始宽高比给宽，避免拉伸变形
                    var img = new VisualElement();
                    int h = 44;
                    int w = Mathf.Clamp(Mathf.RoundToInt(h * tex.width / (float)tex.height), 24, 72);
                    img.style.width = w; img.style.height = h;
                    img.style.backgroundImage = new StyleBackground(tex);
                    img.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 1);
                    img.style.borderTopLeftRadius = 6; img.style.borderTopRightRadius = 6;
                    img.style.borderBottomLeftRadius = 6; img.style.borderBottomRightRadius = 6;
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
        string t = PlayerPrefs.GetString(BackupTimeKey, "");
        return string.IsNullOrEmpty(t) ? "尚未备份" : "上次备份：" + t;
    }

    private static void SaveBackup()
    {
        // 收集所有存档键（VN 存档 + 经营存档 + 书签），快照到单一备份 JSON
        var snapshot = new System.Text.StringBuilder();
        var keys = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 61; i++) keys.Add("VN_Save_" + i);
        for (int i = 0; i < 6; i++) keys.Add("SaveSlot_" + i);
        keys.Add("Achievements_Data");
        keys.Add("Bookmarks_Data");
        keys.Add("VN_AutoLoad");
        keys.Add("VNExitData");

        foreach (var k in keys)
        {
            if (PlayerPrefs.HasKey(k))
                snapshot.Append(k).Append("=").Append(PlayerPrefs.GetString(k)).Append("\n");
        }
        PlayerPrefs.SetString(BackupJsonKey, snapshot.ToString());
        PlayerPrefs.SetString(BackupTimeKey, System.DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
        PlayerPrefs.Save();
    }

    private static void RestoreBackup()
    {
        string json = PlayerPrefs.GetString(BackupJsonKey, "");
        if (string.IsNullOrEmpty(json)) return;
        foreach (var line in json.Split('\n'))
        {
            if (string.IsNullOrEmpty(line)) continue;
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            string k = line.Substring(0, eq);
            string v = line.Substring(eq + 1);
            if (k.StartsWith("VN_Save_") || k.StartsWith("SaveSlot_"))
            {
                PlayerPrefs.SetString(k, v);
            }
        }
        PlayerPrefs.Save();
    }

    // ============ 应用管理：系统管控彩蛋 ============
    public static void ShowAppManagementPage()
    {
        var win = BuildInnerPanel("应用管理");
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
            case SystemAppsEasterEgg.RestoreResult.AlreadyRestored:
                ShowAppToast("该应用已恢复");
                break;

            case SystemAppsEasterEgg.RestoreResult.Success:
                // 播放闪现动画 → 弹窗 → 跳转
                if (GameMainUI.Instance != null) GameMainUI.Instance.StartCoroutine(PlayRestoreAnimation(index, settingsWin));
                break;

            case SystemAppsEasterEgg.RestoreResult.CapsuleTriggered:
                // 时间胶囊触发
                if (GameMainUI.Instance != null) GameMainUI.Instance.StartCoroutine(PlayTimeCapsule(settingsWin));
                break;
        }
    }

    private static IEnumerator PlayRestoreAnimation(int index, VisualElement settingsWin)
    {
        var app = SystemAppsEasterEgg.Apps[index];

        // 第一阶段：闪现原版Logo 0.3秒
        ShowAppToast($"[闪现] {app.FlashText}...");
        yield return new WaitForSecondsRealtime(0.3f);

        // 第二阶段：黑屏0.5秒
        ShowAppToast("");
        yield return new WaitForSecondsRealtime(0.5f);

        // 第三阶段：弹窗
        if (app.JumpTarget != null)
        {
            ShowAppToast($"⚠ {app.PopupMessage}\n→ 跳转至『{app.JumpTarget}』");
        }
        else
        {
            ShowAppToast($"⚠ {app.PopupMessage}\n（无可用服务）");
        }
        yield return new WaitForSecondsRealtime(2f);

        // 刷新应用管理页面
        ShowAppToast("");
        ShowAppManagementPage();
    }

    private static IEnumerator PlayTimeCapsule(VisualElement settingsWin)
    {
        // 黑屏
        ShowAppToast("");
        yield return new WaitForSecondsRealtime(1f);

        // 时间胶囊第一行字
        ShowAppToast(SystemAppsEasterEgg.TimeCapsule_Line1);
        yield return new WaitForSecondsRealtime(3f);

        // 第二行字
        ShowAppToast(SystemAppsEasterEgg.TimeCapsule_Line2);
        yield return new WaitForSecondsRealtime(3f);

        // 恢复出厂设置
        SystemAppsEasterEgg.FactoryReset();
        ShowAppToast("系统已恢复出厂设置。所有应用已被重新替换。");
        yield return new WaitForSecondsRealtime(2f);

        ShowAppToast("");
        // 刷新页面
        ShowAppManagementPage();
    }

    private static void ShowAppToast(string msg) => ShowToast(msg);

    public static void ShowAffairsPage()
    {
        // 事务页（简单占位）
        var panel = BuildInnerPanel("事务");
        var p = new Label("事务功能开发中（绑定经营系统后开放）。");
        p.style.fontSize = 15; p.style.color = GlassTextDim;
        p.style.unityFontDefinition = Fd(); p.style.marginTop = 40;
        panel.Add(p);
    }

    public static void ShowStoryPage()
    {
        var panel = BuildInnerPanel("剧情");
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


    }
}
