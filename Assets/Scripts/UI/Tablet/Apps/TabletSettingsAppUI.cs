using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.SystemShell;
using RailwayRenaissance.UI.Tablet.Account;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板应用：设置（严格照截图 24 分类 + 术语图鉴 TIPS + 备份/应用管理/开发者选项）
    /// 从 GameMainUI 剥离（T306 Batch 1-C）
    /// </summary>
    public static partial class TabletSettingsAppUI
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

    private static Action<int> s_switchCategory;
    private static Action s_refreshRight;
    private static Action s_refreshNav;
    private static Action<string> s_showToast;
    private static Action<TabletSystemPanel> s_openSystemPanel;
    private static TabletShellState s_shellState;
    public static Action<KeyCode> TabletRebindAction;

    public static void SetShellStateHandler(TabletShellState state)
    {
        s_shellState = state;
    }

    public static TabletShellState CurrentShellState => s_shellState ?? GameMainUI.Instance?.ShellState;

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

        s_settingsWin = win;
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
        leftNav.style.backgroundColor = SurfaceApp;   // 浅灰底，分组卡片用白色浮在上面（对齐 HyperOS 设置）
        leftNav.style.borderRightWidth = 1;
        leftNav.style.borderRightColor = BorderDivider;
        leftNav.style.flexDirection = FlexDirection.Column;
        leftNav.style.paddingLeft = 20; leftNav.style.paddingRight = 20;
        leftNav.style.paddingTop = 4; leftNav.style.paddingBottom = 0;
        rootRow.Add(leftNav);

        var titleLbl = new Label("设置");
        titleLbl.style.fontSize = 32; titleLbl.style.color = InkPrimary;
        titleLbl.style.unityFontDefinition = FdBold();
        titleLbl.style.marginTop = 6;
        titleLbl.style.marginBottom = 16;

        // 搜索框
        var searchBox = new VisualElement();
        searchBox.style.height = 44;
        searchBox.style.backgroundColor = new Color(0.90f, 0.90f, 0.92f, 1f);
        searchBox.style.borderTopLeftRadius = 22; searchBox.style.borderTopRightRadius = 22;
        searchBox.style.borderBottomLeftRadius = 22; searchBox.style.borderBottomRightRadius = 22;
        searchBox.style.flexDirection = FlexDirection.Row;
        searchBox.style.alignItems = Align.Center;
        searchBox.style.paddingLeft = 12; searchBox.style.paddingRight = 12;
        searchBox.style.marginBottom = 12;

        var searchIcon = new RailwayRenaissance.UI.Tablet.TabletIconElement(RailwayRenaissance.UI.Tablet.TabletIconKind.Search, 20f, InkTertiary);
        searchIcon.style.marginRight = 8;
        searchBox.Add(searchIcon);

        var searchInput = new TextField { value = settingsSearchFilter };
        searchInput.style.flexGrow = 1;
        searchInput.style.borderTopWidth = 0; searchInput.style.borderBottomWidth = 0;
        searchInput.style.borderLeftWidth = 0; searchInput.style.borderRightWidth = 0;
        searchInput.style.backgroundColor = Color.clear;
        searchInput.style.fontSize = 14; searchInput.style.color = InkPrimary;
        searchInput.style.unityFontDefinition = Fd();
        // 去掉输入框自带的边框与底色，只留文字（原来会露出一个灰色矩形）
        var searchField = searchInput.Q(className: "unity-base-text-field__input");
        if (searchField != null)
        {
            searchField.style.backgroundColor = Color.clear;
            searchField.style.borderTopWidth = searchField.style.borderBottomWidth = searchField.style.borderLeftWidth = searchField.style.borderRightWidth = 0;
            searchField.style.unityFontDefinition = Fd();
        }
        searchInput.textEdition.placeholder = "搜索设置";
        searchInput.textEdition.hidePlaceholderOnFocus = true;
        searchBox.Add(searchInput);

        // 右栏：内容详情区（隐藏滚动条）
        var rightPane = new ScrollView { name = "settings-right-pane" };
        rightPane.style.flexGrow = 1;
        rightPane.style.flexShrink = 1;
        rightPane.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        rightPane.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        rightPane.style.paddingLeft = 32; rightPane.style.paddingRight = 32;
        rightPane.style.paddingTop = 20; rightPane.style.paddingBottom = 0;
        rightPane.style.backgroundColor = SurfaceApp;
        // 页面标题固定在右栏顶部居中，内容滚动时不随之滚走（对照 HyperOS 设置）
        var rightColumn = new VisualElement { name = "settings-right-column" };
        rightColumn.style.flexGrow = 1; rightColumn.style.flexShrink = 1;
        rightColumn.style.flexDirection = FlexDirection.Column;
        var headerBar = new VisualElement { name = "settings-page-header" };
        headerBar.style.height = 64; headerBar.style.flexShrink = 0;
        headerBar.style.alignItems = Align.Center; headerBar.style.justifyContent = Justify.Center;
        headerBar.style.backgroundColor = SurfaceApp;
        headerBar.style.paddingTop = 4;
        rightPane.style.paddingTop = 6;
        rightColumn.Add(headerBar);
        rightColumn.Add(rightPane);
        rootRow.Add(rightColumn);

        // 分类滚动列表（隐藏滚动条）
        var navScroll = new ScrollView();
        navScroll.style.flexGrow = 1;
        navScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        navScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        leftNav.Add(navScroll);
        // 大标题与搜索框随列表一起滚动，滚过后折叠成顶部居中的小标题（对照 HyperOS 设置）
        navScroll.Add(titleLbl);
        navScroll.Add(searchBox);
        var navList = new VisualElement { name = "settings-category-list" };
        navScroll.Add(navList);
        navScroll.Add(new VisualElement { pickingMode = PickingMode.Ignore, style = { height = 36, flexShrink = 0 } });
        var navStickyBar = new VisualElement { name = "settings-nav-sticky-bar", pickingMode = PickingMode.Ignore };
        navStickyBar.style.position = Position.Absolute;
        navStickyBar.style.left = 0; navStickyBar.style.right = 0; navStickyBar.style.top = 0;
        navStickyBar.style.height = 64;
        navStickyBar.style.alignItems = Align.Center; navStickyBar.style.justifyContent = Justify.Center;
        navStickyBar.style.backgroundColor = new Color(SurfaceApp.r, SurfaceApp.g, SurfaceApp.b, 0f);
        var navStickyTitle = new Label("设置") { pickingMode = PickingMode.Ignore };
        navStickyTitle.style.fontSize = 20; navStickyTitle.style.color = InkPrimary;
        navStickyTitle.style.unityFontDefinition = FdBold();
        navStickyTitle.style.opacity = 0f;
        navStickyTitle.style.marginTop = 4;
        navStickyBar.Add(navStickyTitle);
        leftNav.Add(navStickyBar);
        navScroll.verticalScroller.valueChanged += v =>
        {
            float t = Mathf.Clamp01((v - 24f) / 56f);
            navStickyBar.style.backgroundColor = new Color(SurfaceApp.r, SurfaceApp.g, SurfaceApp.b, t);
            navStickyTitle.style.opacity = t;
            titleLbl.style.opacity = 1f - t;
        };

        Func<VisualElement> newCard = () =>
        {
            var card = new VisualElement();
            card.style.backgroundColor = Color.white;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 22;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 22;
            card.style.paddingTop = 6; card.style.paddingBottom = 6;
            card.style.paddingLeft = 6; card.style.paddingRight = 6;
            card.style.marginBottom = 12;
            navList.Add(card);
            return card;
        };

        Func<string, string, Color, string, bool, Action, Button> buildNavItem = (icon, name, badgeColor, labelOverride, selected, onClick) =>
        {
            var itemBtn = new Button(onClick);
            itemBtn.style.height = 62;
            itemBtn.style.flexDirection = FlexDirection.Row;
            itemBtn.style.alignItems = Align.Center;
            itemBtn.style.paddingLeft = 12; itemBtn.style.paddingRight = 12;
            itemBtn.style.marginTop = 0; itemBtn.style.marginBottom = 0;
            itemBtn.style.marginLeft = 0; itemBtn.style.marginRight = 0;
            itemBtn.style.backgroundColor = selected ? new Color(0.92f, 0.92f, 0.94f, 1f) : Color.clear;
            itemBtn.style.borderTopLeftRadius = itemBtn.style.borderTopRightRadius = 16;
            itemBtn.style.borderBottomLeftRadius = itemBtn.style.borderBottomRightRadius = 16;
            itemBtn.style.borderTopWidth = 0; itemBtn.style.borderBottomWidth = 0;
            itemBtn.style.borderLeftWidth = 0; itemBtn.style.borderRightWidth = 0;

            var badge = BuildIconBadge(icon, badgeColor, 40);
            badge.style.marginRight = 16;
            itemBtn.Add(badge);

            var nameLbl = new Label(labelOverride ?? name);
            nameLbl.style.fontSize = 18;
            nameLbl.style.color = InkPrimary;
            nameLbl.style.unityFontDefinition = Fd();
            itemBtn.Add(nameLbl);
            return itemBtn;
        };

        var navButtons = new Dictionary<int, Button>();
        Action<int> switchCategory = null;
        Action populateCategories = () =>
        {
            navList.Clear();
            navButtons.Clear();
            string previousRenderedGroup = null;
            VisualElement currentCard = null;

            for (int i = 0; i < SettingsCategories.Length; i++)
            {
                var cat = SettingsCategories[i];
                if (!string.IsNullOrEmpty(settingsSearchFilter) && !cat.name.Contains(settingsSearchFilter))
                    continue;

                // 每个分组一张白色圆角卡片，卡片之间留出间距
                if (currentCard == null || cat.group != previousRenderedGroup)
                    currentCard = newCard();
                previousRenderedGroup = cat.group;

                int catId = cat.id;
                bool isSelected = (catId == SubPageParent(currentSettingsCategory));
                var navItem = buildNavItem(cat.icon, cat.name, GetHyperOSCategoryColor(cat.id, cat.group), null, isSelected, () => switchCategory(catId));
                if (cat.id == 1) ConvertToAccountNavItem(navItem);
                navButtons[catId] = navItem;
                currentCard.Add(navItem);
            }

            // 若已解锁开发者模式，渲染开发者选项入口
            bool devUnlocked = isTabletDevModeUnlocked || PlayerPrefs.GetInt("TabletDevModeUnlocked", 0) == 1;
            if (devUnlocked && (string.IsNullOrEmpty(settingsSearchFilter) || "开发者选项".Contains(settingsSearchFilter)))
            {
                var devCard = newCard();
                bool isDevSelected = (currentSettingsCategory == 99);
                var devItem = buildNavItem("🛠", "开发者选项", GetHyperOSCategoryColor(99, "system"), null, isDevSelected, () => switchCategory(99));
                navButtons[99] = devItem;
                devCard.Add(devItem);
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
        compactCategory.SetValueWithoutNotify(currentSettingsCategory == 99
            ? "开发者选项"
            : GetSettingsCategoryName(currentSettingsCategory));
        compactCategory.RegisterValueChangedCallback(evt =>
        {
            if (string.IsNullOrEmpty(evt.newValue) || evt.newValue == evt.previousValue) return;
            int next = currentSettingsCategory;
            if (evt.newValue == "开发者选项")
                next = 99;
            else
            {
                for (int i = 0; i < SettingsCategories.Length; i++)
                {
                    if (SettingsCategories[i].name == evt.newValue)
                    {
                        next = SettingsCategories[i].id;
                        break;
                    }
                }
            }
            switchCategory(next);
        });
        compactSearch.RegisterValueChangedCallback(evt =>
        {
            settingsSearchFilter = evt.newValue;
            searchInput.SetValueWithoutNotify(settingsSearchFilter);
            populateCategories();
        });

        // 带大横幅的页面（我的设备）：标题先隐藏，横幅滚出后再渐显，对照 HyperOS
        bool heroPage = false;
        Action renderRight = () =>
        {
            headerBar.Clear();
            rightPane.Clear();
            RenderSettingsDetail(rightPane, win);
            rightPane.Add(new VisualElement { name = "settings-bottom-spacer", pickingMode = PickingMode.Ignore, style = { height = 36, flexShrink = 0 } });   // 末尾留出手势条的位置
            var pageTitle = rightPane.Q<Label>("settings-page-title");
            heroPage = rightPane.Q("settings-hyperos-hero") != null;
            if (pageTitle != null)
            {
                pageTitle.RemoveFromHierarchy();
                pageTitle.style.marginBottom = 0;
                pageTitle.style.opacity = heroPage ? 0f : 1f;
                headerBar.Add(pageTitle);
            }
            if (SubPageParent(currentSettingsCategory) != currentSettingsCategory)
            {
                var back = new VisualElement { name = "settings-subpage-back", pickingMode = PickingMode.Position };
                back.style.position = Position.Absolute;
                back.style.left = 22; back.style.top = 10; back.style.width = 44; back.style.height = 44;
                back.style.alignItems = Align.Center; back.style.justifyContent = Justify.Center;
                back.style.borderTopLeftRadius = back.style.borderTopRightRadius = 22;
                back.style.borderBottomLeftRadius = back.style.borderBottomRightRadius = 22;
                AddRowFeedback(back);
                back.Add(new TabletIconElement(TabletIconKind.Back, 28f, InkPrimary) { pickingMode = PickingMode.Ignore });
                back.RegisterCallback<ClickEvent>(_ => switchCategory(SubPageParent(currentSettingsCategory)));
                headerBar.Add(back);
            }
            if (currentSettingsCategory == 97)
            {
                var more = new VisualElement { name = "settings-subpage-menu", pickingMode = PickingMode.Position };
                more.style.position = Position.Absolute;
                more.style.right = 22; more.style.top = 10; more.style.width = 44; more.style.height = 44;
                more.style.alignItems = Align.Center; more.style.justifyContent = Justify.Center;
                more.style.borderTopLeftRadius = more.style.borderTopRightRadius = 22;
                more.style.borderBottomLeftRadius = more.style.borderBottomRightRadius = 22;
                AddRowFeedback(more);
                more.Add(new TabletIconElement(TabletIconKind.Dots, 26f, InkPrimary) { pickingMode = PickingMode.Ignore });
                more.RegisterCallback<ClickEvent>(_ => ShowToast("云空间设置：同步策略与存储配额由所属单位统一管理"));
                headerBar.Add(more);
            }
            rightPane.scrollOffset = Vector2.zero;
        };
        rightPane.verticalScroller.valueChanged += v =>
        {
            if (!heroPage) return;
            var t = headerBar.Q<Label>("settings-page-title");
            if (t != null) t.style.opacity = Mathf.Clamp01((v - 150f) / 60f);
        };
        switchCategory = id =>
        {
            if (id == currentSettingsCategory) return;
            currentSettingsCategory = id;
            foreach (var kv in navButtons)
                kv.Value.style.backgroundColor = kv.Key == SubPageParent(id) ? new Color(0.92f, 0.92f, 0.94f, 1f) : Color.clear;
            compactCategory.SetValueWithoutNotify(id == 99 ? "开发者选项" : GetSettingsCategoryName(id));
            renderRight();
        };
        s_switchCategory = switchCategory;
        s_refreshNav = populateCategories;
        s_refreshRight = () =>
        {
            var keep = rightPane.scrollOffset;
            renderRight();
            rightPane.schedule.Execute(() => rightPane.scrollOffset = keep);
        };
        renderRight();
        // 搜索态：点搜索框后左栏切到“搜索记录 / 结果”视图，右侧“取消”退出（对照 HyperOS 设置）
        var searchOverlay = new VisualElement { name = "settings-search-overlay" };
        searchOverlay.style.position = Position.Absolute;
        searchOverlay.style.left = 0; searchOverlay.style.right = 0; searchOverlay.style.top = 0; searchOverlay.style.bottom = 0;
        searchOverlay.style.backgroundColor = SurfaceApp;
        searchOverlay.style.paddingLeft = 20; searchOverlay.style.paddingRight = 20;
        searchOverlay.style.paddingTop = 16; searchOverlay.style.paddingBottom = 16;
        searchOverlay.style.display = DisplayStyle.None;
        leftNav.Add(searchOverlay);

        var sTop = new VisualElement();
        sTop.style.flexDirection = FlexDirection.Row; sTop.style.alignItems = Align.Center;
        sTop.style.marginTop = 6; sTop.style.marginBottom = 18; sTop.style.flexShrink = 0;
        searchOverlay.Add(sTop);
        var sBox = new VisualElement();
        sBox.style.flexGrow = 1; sBox.style.height = 44;
        sBox.style.backgroundColor = new Color(0.90f, 0.90f, 0.92f, 1f);
        sBox.style.borderTopLeftRadius = sBox.style.borderTopRightRadius = 22;
        sBox.style.borderBottomLeftRadius = sBox.style.borderBottomRightRadius = 22;
        sBox.style.flexDirection = FlexDirection.Row; sBox.style.alignItems = Align.Center;
        sBox.style.paddingLeft = 12; sBox.style.paddingRight = 12;
        sTop.Add(sBox);
        var sIcon = new RailwayRenaissance.UI.Tablet.TabletIconElement(RailwayRenaissance.UI.Tablet.TabletIconKind.Search, 20f, InkTertiary);
        sIcon.style.marginRight = 8;
        sBox.Add(sIcon);
        var sInput = new TextField();
        sInput.style.flexGrow = 1;
        sInput.style.borderTopWidth = 0; sInput.style.borderBottomWidth = 0;
        sInput.style.borderLeftWidth = 0; sInput.style.borderRightWidth = 0;
        sInput.style.backgroundColor = Color.clear;
        sInput.style.fontSize = 16; sInput.style.color = InkPrimary;
        sInput.style.unityFontDefinition = Fd();
        var sInner = sInput.Q(className: "unity-base-text-field__input");
        if (sInner != null)
        {
            sInner.style.backgroundColor = Color.clear;
            sInner.style.borderTopWidth = 0; sInner.style.borderBottomWidth = 0;
            sInner.style.borderLeftWidth = 0; sInner.style.borderRightWidth = 0;
            sInner.style.unityFontDefinition = Fd();
        }
        sInput.textEdition.placeholder = "搜索设置";
        sInput.textEdition.hidePlaceholderOnFocus = true;
        sBox.Add(sInput);
        var sCancel = new Label("取消") { pickingMode = PickingMode.Position };
        sCancel.style.fontSize = 20; sCancel.style.color = AccentSystem;
        sCancel.style.unityFontDefinition = Fd();
        sCancel.style.marginLeft = 18;
        sTop.Add(sCancel);
        var sBody = new ScrollView();
        sBody.style.flexGrow = 1;
        sBody.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        sBody.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        searchOverlay.Add(sBody);

        Action closeSearch = () =>
        {
            searchOverlay.style.display = DisplayStyle.None;
            sInput.SetValueWithoutNotify("");
        };
        Action renderSearchBody = null;
        renderSearchBody = () =>
        {
            sBody.Clear();
            string q = (sInput.value ?? "").Trim();
            if (q.Length == 0)
            {
                var hist = LoadSettingsSearchHistory();
                if (hist.Count == 0) return;
                var hl = new Label("搜索记录");
                hl.style.fontSize = 15; hl.style.color = SectionHeaderColor; hl.style.unityFontDefinition = Fd();
                hl.style.marginBottom = 10; hl.style.marginLeft = 4;
                sBody.Add(hl);
                var wrap = new VisualElement();
                wrap.style.flexDirection = FlexDirection.Row; wrap.style.flexWrap = Wrap.Wrap;
                sBody.Add(wrap);
                foreach (var h in hist)
                {
                    string term = h;
                    var chip = new Label(term) { pickingMode = PickingMode.Position };
                    chip.style.fontSize = 18; chip.style.color = InkPrimary; chip.style.unityFontDefinition = Fd();
                    chip.style.backgroundColor = Color.white;
                    chip.style.paddingLeft = 22; chip.style.paddingRight = 22; chip.style.paddingTop = 11; chip.style.paddingBottom = 11;
                    chip.style.marginRight = 10; chip.style.marginBottom = 10;
                    chip.style.borderTopLeftRadius = chip.style.borderTopRightRadius = 22;
                    chip.style.borderBottomLeftRadius = chip.style.borderBottomRightRadius = 22;
                    chip.RegisterCallback<ClickEvent>(_ => sInput.value = term);
                    wrap.Add(chip);
                }
                var clear = new Label("清除搜索记录") { pickingMode = PickingMode.Position };
                clear.style.fontSize = 16; clear.style.color = InkSecondary; clear.style.unityFontDefinition = Fd();
                clear.style.alignSelf = Align.Center; clear.style.marginTop = 30; clear.style.paddingTop = 8; clear.style.paddingBottom = 8;
                clear.RegisterCallback<ClickEvent>(_ => { SaveSettingsSearchHistory(new List<string>()); renderSearchBody(); });
                sBody.Add(clear);
                return;
            }

            var resCard = new VisualElement();
            resCard.style.backgroundColor = Color.white;
            resCard.style.borderTopLeftRadius = resCard.style.borderTopRightRadius = 22;
            resCard.style.borderBottomLeftRadius = resCard.style.borderBottomRightRadius = 22;
            resCard.style.paddingTop = 6; resCard.style.paddingBottom = 6; resCard.style.paddingLeft = 6; resCard.style.paddingRight = 6;
            bool any = false;
            for (int i = 0; i < SettingsCategories.Length; i++)
            {
                var cat = SettingsCategories[i];
                if (cat.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                int catId = cat.id;
                any = true;
                resCard.Add(buildNavItem(cat.icon, cat.name, GetHyperOSCategoryColor(cat.id, cat.group), null, catId == currentSettingsCategory, () =>
                {
                    AddSettingsSearchHistory(q);
                    closeSearch();
                    switchCategory(catId);
                }));
            }
            if (any) sBody.Add(resCard);
            else
            {
                var none = new Label("未找到相关设置");
                none.style.fontSize = 16; none.style.color = InkTertiary; none.style.unityFontDefinition = Fd();
                none.style.alignSelf = Align.Center; none.style.marginTop = 60;
                sBody.Add(none);
            }
        };
        sInput.RegisterValueChangedCallback(_ => renderSearchBody());
        sCancel.RegisterCallback<ClickEvent>(_ => closeSearch());
        searchInput.pickingMode = PickingMode.Ignore;
        searchInput.focusable = false;
        searchBox.pickingMode = PickingMode.Position;
        searchBox.RegisterCallback<PointerDownEvent>(_ =>
        {
            searchOverlay.style.display = DisplayStyle.Flex;
            renderSearchBody();
            sInput.schedule.Execute(() => sInput.Focus());
        });

        rootRow.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            float width = evt.newRect.width;
            if (width <= 0) return;
            bool compact = width < 760;
            rootRow.style.flexDirection = compact ? FlexDirection.Column : FlexDirection.Row;
            compactToolbar.style.display = compact ? DisplayStyle.Flex : DisplayStyle.None;
            leftNav.style.display = compact ? DisplayStyle.None : DisplayStyle.Flex;
            // 左右栏比例对照 HyperOS Pad 设置：左栏约 40%、右栏约 60%（宽屏下限 320px，上限 820px）
            leftNav.style.width = Mathf.Clamp(width * 0.40f, 320f, 820f);
            rightPane.style.paddingLeft = compact ? 14 : width < 1020 ? 22 : 32;
            rightPane.style.paddingRight = compact ? 14 : width < 1020 ? 22 : 32;
            rightPane.style.paddingTop = compact ? 8 : 6;
        });
    }

    /// <summary>从设置内部跳转到另一分类：只换右栏并更新选中态，保留左栏滚动位置。</summary>
    /// <summary>重新生成左栏分类列表（如开发者模式刚解锁时让“开发者选项”入口出现），保留滚动位置与当前页。</summary>
    public static void RefreshCategoryList()
    {
        if (s_refreshNav != null && s_settingsWin != null && s_settingsWin.panel != null) s_refreshNav();
    }

    /// <summary>就地重画当前分类的右栏并保留滚动位置（开关联动、恢复默认等场景）。</summary>
    /// <summary>直接切到某个分类（含子页），审查与外部入口使用。</summary>
    public static void ShowCategory(int id) => s_switchCategory?.Invoke(id);

    public static void RefreshCurrentPage()
    {
        if (s_refreshRight != null && s_settingsWin != null && s_settingsWin.panel != null) s_refreshRight();
        else if (s_settingsWin != null) BuildSettingsPage(s_settingsWin);
    }

    private static void NavigateToCategory(int id, VisualElement win)
    {
        if (s_switchCategory != null && s_settingsWin == win) { s_switchCategory(id); return; }
        currentSettingsCategory = id;
        BuildSettingsPage(win);
    }

    private const string SearchHistoryKey = "TabletSettingsSearchHistory";

    private static List<string> LoadSettingsSearchHistory()
    {
        var list = new List<string>();
        string raw = PlayerPrefs.GetString(SearchHistoryKey, "");
        if (!string.IsNullOrEmpty(raw))
            foreach (var part in raw.Split('|'))
                if (!string.IsNullOrWhiteSpace(part)) list.Add(part);
        return list;
    }

    private static void SaveSettingsSearchHistory(List<string> list)
    {
        PlayerPrefs.SetString(SearchHistoryKey, string.Join("|", list));
        PlayerPrefs.Save();
    }

    private static void AddSettingsSearchHistory(string term)
    {
        term = (term ?? "").Replace("|", " ").Trim();
        if (term.Length == 0) return;
        var list = LoadSettingsSearchHistory();
        list.RemoveAll(x => string.Equals(x, term, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, term);
        if (list.Count > 12) list.RemoveRange(12, list.Count - 12);
        SaveSettingsSearchHistory(list);
    }

    // 子页挂在父分类下：98 存储 → 我的设备(2)；97 云空间 → 账号(1)；96 系统导航 → 桌面(12)
    private static int SubPageParent(int id) => id == 98 ? 2 : id == 97 ? 1 : id == 96 ? 12 : id;

    private static string GetSettingsCategoryName(int id)
    {
        id = SubPageParent(id);
        for (int i = 0; i < SettingsCategories.Length; i++)
            if (SettingsCategories[i].id == id) return SettingsCategories[i].name;
        return SettingsCategories[0].name;
    }

    private static void RenderSettingsDetail(ScrollView rightPane, VisualElement win)
    {
        switch (currentSettingsCategory)
        {
            case 1: RenderAccountSettings(rightPane, win); break;
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
            case 12: RenderHomeScreenSettings(rightPane, win); break;
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
            case 96: RenderSystemNavSettings(rightPane, win); break;
            case 97: RenderCloudSettings(rightPane, win); break;
            case 98: RenderStorageSettings(rightPane, win); break;
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
        new TabletPresentationSettingsView(rightPane, prefs, RailwayRenaissance.UI.Tablet.TabletFonts.Ui());
    }

    // 形态 A · 账号页（对照 xiaomi.account 截图）
    private static void RenderAccountSettings(ScrollView rightPane, VisualElement win)
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

        var avatar = new Label(AccountService.IsRegistered ? AccountService.DisplayName.Substring(0, 1) : string.Empty);
        avatar.style.fontSize = 40; avatar.style.width = 76; avatar.style.height = 76;
        avatar.style.alignItems = Align.Center; avatar.style.justifyContent = Justify.Center;
        if (AccountService.IsRegistered)
        {
            avatar.style.color = Color.white;
            avatar.style.backgroundColor = AccountAvatarColors[Mathf.Clamp(AccountService.Profile.avatar, 0, AccountAvatarColors.Length - 1)];
        }
        else
        {
            avatar.style.backgroundColor = SurfaceApp;
            avatar.Add(new TabletIconElement(TabletIconKind.Person, 46, new Color(0.62f, 0.64f, 0.70f, 1f)));
        }
        avatar.style.unityTextAlign = TextAnchor.MiddleCenter;
        avatar.style.borderTopLeftRadius = 38; avatar.style.borderTopRightRadius = 38;
        avatar.style.borderBottomLeftRadius = 38; avatar.style.borderBottomRightRadius = 38;
        avatar.style.unityFontDefinition = Fd();
        userLeft.Add(avatar);

        var userMeta = new VisualElement();
        userMeta.style.marginLeft = 16;
        userLeft.Add(userMeta);

        string curUser = AccountService.IsRegistered ? AccountService.DisplayName : "登录小米账号";
        var nameLbl = new Label(curUser);
        nameLbl.style.fontSize = 24; nameLbl.style.color = InkPrimary;
        nameLbl.style.unityFontDefinition = FdBold();
        userMeta.Add(nameLbl);

        var idLbl = new Label(AccountService.IsRegistered
            ? "小米 ID：" + AccountService.Profile.xiaomiId + " · " + AccountService.MaskedPhone(AccountService.Profile)
            : "登录后使用云空间、查找设备与家庭服务");
        idLbl.style.fontSize = 14; idLbl.style.color = InkSecondary; idLbl.style.unityFontDefinition = Fd();
        idLbl.style.marginTop = 6;
        userMeta.Add(idLbl);

        var ar = new Label("›");
        ar.style.fontSize = 20; ar.style.color = InkTertiary; ar.style.unityFontDefinition = Fd();
        profileCard.Add(ar);
        profileCard.name = "account-profile-card";
        profileCard.pickingMode = PickingMode.Position;
        profileCard.RegisterCallback<ClickEvent>(_ => OpenAccountEntry());
        rightPane.Add(profileCard);

        // 卡片网格（左大卡 + 右两小卡）
        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.justifyContent = Justify.SpaceBetween;
        grid.style.marginBottom = 14;
        rightPane.Add(grid);

        var cloudCard = BuildWhiteCard();
        cloudCard.name = "account-cloud-card";
        cloudCard.style.width = new Length(48, LengthUnit.Percent);
        cloudCard.style.marginBottom = 0;
        cloudCard.style.overflow = Overflow.Hidden;
        cloudCard.style.justifyContent = Justify.SpaceBetween;
        cloudCard.style.paddingTop = 18; cloudCard.style.paddingBottom = 20;
        cloudCard.style.minHeight = 168;
        cloudCard.pickingMode = PickingMode.Position;
        bool cloudSignedIn = AccountService.IsRegistered;
        cloudCard.RegisterCallback<ClickEvent>(_ =>
        {
            if (AccountService.IsRegistered) NavigateToCategory(97, win);
            else { ShowToast("请先登录小米账号再使用云空间"); OpenAccountEntry(); }
        });

        // 蓝色水波 = 当前使用量：水面高度为已用 / 可用，水面缓缓起伏
        var wave = new VisualElement { name = "account-cloud-wave", pickingMode = PickingMode.Ignore };
        wave.style.position = Position.Absolute;
        wave.style.left = 0; wave.style.right = 0; wave.style.top = 0; wave.style.bottom = 0;
        float cloudFill = cloudSignedIn ? Mathf.Clamp((float)TabletStorageLedger.CloudUsedBytes / TabletStorageLedger.CloudUsableBytes, 0.04f, 1f) : 0f;
        float wavePhase = 0f;
        wave.generateVisualContent += mgc =>
        {
            var r = wave.contentRect;
            if (r.width < 2f || r.height < 2f || cloudFill <= 0f) return;
            var painter = mgc.painter2D;
            float baseY = r.height * (1f - cloudFill);
            for (int layer = 0; layer < 2; layer++)
            {
                float amp = layer == 0 ? 4.5f : 3f;
                float ph = wavePhase * (layer == 0 ? 1f : -0.7f) + layer * 1.9f;
                painter.fillColor = layer == 0
                    ? new Color(0.20f, 0.50f, 0.98f, 0.18f)
                    : new Color(0.20f, 0.50f, 0.98f, 0.12f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(0f, r.height));
                painter.LineTo(new Vector2(0f, baseY));
                for (float x = 0f; x <= r.width + 6f; x += 6f)
                    painter.LineTo(new Vector2(x, baseY + Mathf.Sin(x * 0.022f + ph) * amp));
                painter.LineTo(new Vector2(r.width, r.height));
                painter.ClosePath();
                painter.Fill();
            }
        };
        cloudCard.Add(wave);
        var waveTick = wave.schedule.Execute(() => { wavePhase += 0.09f; wave.MarkDirtyRepaint(); }).Every(40);
        wave.RegisterCallback<DetachFromPanelEvent>(_ => waveTick.Pause());

        var cloudHead = new VisualElement { pickingMode = PickingMode.Ignore };
        cloudHead.style.flexDirection = FlexDirection.Row;
        cloudHead.style.alignItems = Align.Center;
        var cloudTile = new VisualElement { pickingMode = PickingMode.Ignore };
        cloudTile.style.width = 34; cloudTile.style.height = 34;
        cloudTile.style.alignItems = Align.Center; cloudTile.style.justifyContent = Justify.Center;
        cloudTile.style.backgroundColor = new Color(0.20f, 0.50f, 0.98f, 1f);
        cloudTile.style.borderTopLeftRadius = cloudTile.style.borderTopRightRadius = 10;
        cloudTile.style.borderBottomLeftRadius = cloudTile.style.borderBottomRightRadius = 10;
        cloudTile.Add(new TabletIconElement(TabletIconKind.Cloud, 22, Color.white) { pickingMode = PickingMode.Ignore });
        cloudHead.Add(cloudTile);
        cloudHead.Add(new Label("云空间") { pickingMode = PickingMode.Ignore, style = { fontSize = 18, color = InkPrimary, unityFontDefinition = FdBold(), marginLeft = 12 } });
        cloudCard.Add(cloudHead);

        var cloudBottom = new VisualElement { pickingMode = PickingMode.Ignore };
        cloudBottom.Add(new Label(cloudSignedIn ? "已占用" : "未登录") { pickingMode = PickingMode.Ignore, style = { fontSize = 13, color = InkSecondary, unityFontDefinition = Fd() } });
        var cloudFigure = new VisualElement { pickingMode = PickingMode.Ignore };
        cloudFigure.style.flexDirection = FlexDirection.Row;
        cloudFigure.style.alignItems = Align.FlexEnd;
        if (cloudSignedIn)
        {
            cloudFigure.Add(new Label(TabletStorageLedger.Format(TabletStorageLedger.CloudUsedBytes)) { pickingMode = PickingMode.Ignore, style = { fontSize = 30, color = InkPrimary, unityFontDefinition = FdBold() } });
            cloudFigure.Add(new Label("/ " + TabletStorageLedger.Format(TabletStorageLedger.CloudUsableBytes)) { pickingMode = PickingMode.Ignore, style = { fontSize = 14, color = InkSecondary, unityFontDefinition = Fd(), marginLeft = 6, marginBottom = 5 } });
        }
        else
        {
            cloudFigure.Add(new Label("登录后使用") { pickingMode = PickingMode.Ignore, style = { fontSize = 22, color = InkSecondary, unityFontDefinition = FdBold() } });
        }
        cloudBottom.Add(cloudFigure);
        cloudCard.Add(cloudBottom);
        grid.Add(cloudCard);

        var rightSubCards = new VisualElement();
        rightSubCards.style.width = new Length(48, LengthUnit.Percent);
        rightSubCards.style.flexDirection = FlexDirection.Column;
        grid.Add(rightSubCards);

        var familyCard = BuildAccountMiniCard(TabletIconKind.Heart, new Color(0.20f, 0.50f, 0.98f, 1f), "家庭服务", "已共享运营排班表",
            () => ShowToast("家庭成员须经人事部门核验后方可共享"));
        familyCard.style.marginBottom = 10;
        rightSubCards.Add(familyCard);

        var findCard = BuildAccountMiniCard(TabletIconKind.Pin, new Color(0.20f, 0.78f, 0.40f, 1f), "查找设备", "0721号终端在线",
            () => ShowToast("0721号机车车载终端实时在线 · 信号极佳"));
        findCard.style.marginBottom = 0;
        rightSubCards.Add(findCard);

        // 分组列表
        AddSectionHeader(rightPane, "我的账号安全与权限");
        var listCard = BuildWhiteCard();
        listCard.Add(BuildWhiteRow("账号安全等级", "最高特级", () => ShowToast("最高特级 · 物理硬件密匙与3D生物结构光已认证；账号处于先民军工特级保护状态")));
        listCard.Add(BuildWhiteRow("个人人事档案", "已同步", () => ShowToast("金日成综合大学荣誉毕业生 · 铁道部特聘工程师；档案已同步至人事与调度中心")));
        listCard.Add(BuildWhiteRow("系统权限授权", "22 个应用", () => ShowToast("已授权系统管理 22 个车载及内网应用；车载应用沙箱权限正常运行中")));
        rightPane.Add(listCard);
    }

    // 2 我的设备：并入老设置关于 8 条 + 真实设定硬件配置（Xiaomi Pad 36 Ultra DPRK Edition）
    private static void RenderMyDeviceSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "我的设备");
        rightPane.Add(BuildHyperOSHero("32.0.4.1"));

        // 横幅下的三行摘要：设备名称 / 存储 / 系统版本（对照 HyperOS“我的设备”）
        var summaryCard = BuildWhiteCard();
        summaryCard.name = "settings-device-summary-card";
        summaryCard.Add(BuildInfoRow("设备名称", "Xiaomi Pad 36 Ultra", null, () => ShowToast("设备名称由先民军工固化，不可修改")));
        summaryCard.Add(BuildInfoRow("存储", TabletStorageLedger.Format(TabletStorageLedger.UserUsedBytes) + " / " + TabletStorageLedger.Format(TabletStorageLedger.UserPartitionBytes) + "（标称 " + TabletStorageLedger.Format(TabletStorageLedger.NominalTotalBytes) + "）", null, () => NavigateToCategory(98, win)));
        summaryCard.Add(BuildInfoRow("系统版本", "32.0.4.1.CHL.C11"));
        rightPane.Add(summaryCard);

        var devCard = BuildWhiteCard();
        devCard.style.alignItems = Align.Center;
        devCard.style.paddingTop = 18; devCard.style.paddingBottom = 18;

        var dName = new Label("Xiaomi Pad 36 Ultra (DPRK Edition)");
        dName.style.fontSize = 20; dName.style.color = InkPrimary;
        dName.style.unityFontDefinition = FdBold();
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
        // 四项主规格用两列“数值 + 说明”网格（对齐 HyperOS“我的设备”）；文字沿用原设定
        specsCard.Add(BuildSpecGrid(new[]
        {
            ("先民超导纳米核 3.6GHz × 16", "处理器 (CPU) · 32位精简指令集"),
            ("64 GB / 64 TB", "运行内存 LPDDR9 Ultra / 标称闪存容量（用户可用 256 GB）"),
            ("14.4 英寸", "Super AMOLED Ultra · 3840×2160 · 144Hz · 防眩光涂层"),
            ("24,000 mAh", "双电芯超级固态沙能电池 · 120W快充 / 机车逆变回充"),
        }));
        specsCard.Add(BuildWhiteRow("设备序列号", "SN-20530721-CHL-001 (0721号专属配件 · 军工固化)", null, false));
        specsCard.Add(BuildWhiteRow("安全防护固件", "白头疫病 SiliVaccine 8.2 (军工高权限固化)", null, false));
        specsCard.Add(BuildWhiteRow("卫星通讯基带", "阿里郎军工量子加密卫星专线 (全天候链路已连接)", null, false));
        rightPane.Add(specsCard);

        var deviceActions = BuildWhiteCard();
        deviceActions.Add(BuildWhiteRow("详细信息和规格", null, () => ShowToast("规格信息见上方硬件规格与项目版本信息")));
        deviceActions.Add(BuildWhiteRow("备份与恢复", null, () => ShowBackupRestorePage()));
        deviceActions.Add(BuildWhiteRow("修复模式", null, () => ShowToast("修复模式需连接机务检修终端，本机不提供")));
        deviceActions.Add(BuildWhiteRow("恢复出厂设置", null, () => ShowToast("设备由军工固件固化，出厂设置不可恢复")));
        rightPane.Add(deviceActions);

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
                ShowToast("已进入开发者模式！已跳转至开发者选项。");
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

    // “我的设备”顶部的 HyperOS 渐变横幅：薰衣草 → 粉 → 淡蓝，居中大字 Logo 与版本号（对照参考图）
    private static Texture2D s_hyperHeroTex;
    private static VisualElement BuildHyperOSHero(string version)
    {
        if (s_hyperHeroTex == null)
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color tl = new Color(0.88f, 0.86f, 1.00f), tr = new Color(1.00f, 0.90f, 0.95f);
            Color bl = new Color(0.99f, 0.84f, 0.91f), br = new Color(0.80f, 0.86f, 1.00f);
            Color bloom = new Color(0.98f, 0.76f, 0.88f);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f), v = y / (n - 1f);   // v=0 在底部
                Color top = Color.Lerp(tl, tr, u), bottom = Color.Lerp(bl, br, u);
                Color c = Color.Lerp(bottom, top, v);
                float d = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.38f, 0.42f)) * 1.8f);
                tex.SetPixel(x, y, Color.Lerp(c, bloom, d * 0.55f));
            }
            tex.Apply();
            s_hyperHeroTex = tex;
        }
        var hero = new VisualElement { name = "settings-hyperos-hero" };
        hero.style.height = 230;
        hero.style.marginBottom = 16;
        hero.style.overflow = Overflow.Hidden;
        hero.style.borderTopLeftRadius = hero.style.borderTopRightRadius = 24;
        hero.style.borderBottomLeftRadius = hero.style.borderBottomRightRadius = 24;
        hero.style.backgroundImage = Background.FromTexture2D(s_hyperHeroTex);
        hero.style.alignItems = Align.Center;
        hero.style.justifyContent = Justify.Center;
        var logo = new Label("Xiaomi HyperOS");
        logo.style.fontSize = 50; logo.style.color = new Color(0.55f, 0.12f, 0.36f, 1f);
        logo.style.unityFontDefinition = FdBold();
        hero.Add(logo);
        var ver = new Label(version);
        ver.style.fontSize = 18; ver.style.color = new Color(0.30f, 0.28f, 0.36f, 1f);
        ver.style.unityFontDefinition = Fd(); ver.style.marginTop = 10;
        hero.Add(ver);
        return hero;
    }

    // 两列“数值 + 说明”规格网格
    private static VisualElement BuildSpecGrid((string value, string caption)[] items)
    {
        var grid = new VisualElement { name = "settings-spec-grid" };
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        grid.style.paddingTop = 8; grid.style.paddingBottom = 4;
        foreach (var it in items)
        {
            var cell = new VisualElement();
            cell.style.width = new Length(50, LengthUnit.Percent);
            cell.style.paddingRight = 16;
            cell.style.marginBottom = 16;
            var v = new Label(it.value);
            v.style.fontSize = 17; v.style.color = InkPrimary;
            v.style.unityFontDefinition = FdBold();
            v.style.whiteSpace = WhiteSpace.Normal;
            var c = new Label(it.caption);
            c.style.fontSize = 12; c.style.color = InkTertiary;
            c.style.unityFontDefinition = Fd(); c.style.marginTop = 3;
            c.style.whiteSpace = WhiteSpace.Normal;
            cell.Add(v);
            cell.Add(c);
            grid.Add(cell);
        }
        return grid;
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
            NavigateToCategory(11, win);
        }, true));
        systemCard.Add(BuildWhiteRow("平板外观与个性化调优", "支持壁纸压暗、减少动效、高对比度与强调色配置（分类 9）", () =>
        {
            NavigateToCategory(9, win);
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

        AddSectionHeader(page, "岁月");
        var engineCard = BuildWhiteCard();
        engineCard.name = "settings-ai-engine-card";
        engineCard.Add(BuildInfoRow("岁月", "0721 车机", "核心运行在 0721 号车机上，通过平板投影与你交互；与平板共享设备所有权"));
        engineCard.Add(BuildInfoRow("桌面投影", "常驻", "桌面一角的浮窗，轻触即可开始对话"));
        engineCard.Add(BuildInfoRow("对话方式", "预设台词", "岁月目前的回应来自本地预设台词"));
        page.Add(engineCard);

        AddSectionHeader(page, "车载内网");
        var interactionCard = BuildWhiteCard();
        interactionCard.name = "settings-ai-interaction-card";
        interactionCard.Add(BuildInfoRow("在线推理", "未接入", "内网沙箱内没有外部大模型服务"));
        interactionCard.Add(BuildInfoRow("语音输入", "未提供", "平板没有语音唤醒或麦克风输入"));
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
        var noticeTitle = new Label("开发者调试模式已激活");
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
                "重置主线剧情进度确认",
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
                    ShowToast("主线剧情进度已重置至序章第 01 幕！");
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

        // ---- 成就与收藏调试（对应老调试面板里的成就/音乐解锁；危险的“清除全部 PlayerPrefs / 重置所有存档”不迁移）----
        AddSectionHeader(rightPane, "成就与收藏调试");
        var achCard = BuildWhiteCard();
        achCard.name = "settings-dev-achievement-card";
        int achTotal = AchievementManager.GetAll().Length;
        achCard.Add(BuildInfoRow("一键解锁全部成就", $"{AchievementManager.GetUnlockedCount()} / {achTotal}", "把所有成就标记为已解锁（会依次弹出成就提示）", () =>
        {
            var all = AchievementManager.GetAll();
            for (int i = 0; i < all.Length; i++) AchievementManager.Unlock(all[i].id);
            ShowToast("已解锁全部成就");
            BuildSettingsPage(win);
        }));
        achCard.Add(BuildInfoRow("重置全部成就", null, "清空成就的解锁状态与日期；不影响剧情进度、经营数据、存档与音乐库", () =>
        {
            ShowResetConfirmDialog(
                win ?? rightPane,
                "重置全部成就确认",
                "此操作会清空所有成就的解锁状态与解锁日期。不会影响剧情进度、经营数据、存档、音乐库与系统设置。",
                "确认重置成就",
                () =>
                {
                    AchievementManager.ResetAll();
                    ShowToast("成就已重置");
                    BuildSettingsPage(win);
                });
        }));
        achCard.Add(BuildInfoRow("一键解锁全部原声", null, "只写入原声收藏的解锁记录（ArchiveMusic_*），不改动音乐库与播放状态", () =>
        {
            foreach (var m in new[] { "iron_and_ash", "cloud_rail", "embers", "night_cargo", "first_light", "platform", "borderline", "wheels_joke", "train_through_keys", "south_wind", "starlit_rails", "chollima_ride", "sleepers" })
                PlayerPrefs.SetInt("ArchiveMusic_" + m, 1);
            PlayerPrefs.Save();
            ShowToast("已解锁全部原声");
        }));
        rightPane.Add(achCard);

        AddSectionHeader(rightPane, "单个成就（点击未解锁项直接解锁）");
        var achListCard = BuildWhiteCard();
        achListCard.name = "settings-dev-achievement-list";
        var achItems = AchievementManager.GetAll();
        for (int i = 0; i < achItems.Length; i++)
        {
            var ach = achItems[i];
            string id = ach.id;
            achListCard.Add(BuildInfoRow(ach.title, ach.unlocked ? "已解锁 " + ach.unlockedDate : "未解锁", ach.description,
                ach.unlocked ? (System.Action)null : () =>
                {
                    AchievementManager.Unlock(id);
                    BuildSettingsPage(win);
                }));
        }
        rightPane.Add(achListCard);

        AddSectionHeader(rightPane, "经营与经济核数据重置");
        var ecoResetCard = BuildWhiteCard();
        ecoResetCard.Add(BuildWhiteRow("重置经济模拟数据", "重置当前运行中的经营状态：资金按难度恢复，初始趋势依已加载种子生成，回到第 1 天", () =>
        {
            ShowResetConfirmDialog(
                win ?? rightPane,
                "重置经济模拟数据确认",
                "此操作会把当前运行中的经营模拟重置到第 1 天，资金按当前难度配置恢复；若已加载世界种子，信任、车况、政治压力和线路老化将恢复为该种子生成的初值，无种子时使用兼容默认值。趋势历史、监管程序、经营策略、短期目标和当日日事件也会重置。\n\n影响边界说明：\n• 不删除已有经营存档槽；重新载入旧存档会恢复槽内数据；\n• 不删除主线剧情通关与阅读记录，但会重置当前运行中的剧情补贴防重复状态；\n• 不清除音乐库独立 JSON 与歌词数据；\n• 不修改系统设置与按键映射。",
                "确认重置经济",
                () =>
                {
                    GameData.ResetState();
                    ShowToast("当前经营模拟状态已重置为第 1 天！");
                });
        }));

        ecoResetCard.Add(BuildWhiteRow("注入调试测试资金", "为当前账务追加 100,000 沙币并恢复信任", () =>
        {
            GameData.AddMoney(100000);
            GameData.AddTrust(30);
            ShowToast("已注资 100,000 沙币，信任已提升！");
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
            ShowToast("已退出开发者模式。");
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
            var fallbackLbl = new Label("当前环境未挂载系统面板服务（通知中心与控制面板入口将提示降级）。")
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

        AddSectionHeader(rightPane, "通知与免打扰策略");
        var policyCard = BuildWhiteCard();
        policyCard.name = "settings-notifications-policy-card";

        var shellState = CurrentShellState;
        bool quietOn = shellState != null && shellState.QuietMode;
        var quietToggle = BuildToggleRow("勿扰模式", "开启后静默屏蔽悬浮提示横幅，所有通知仍完整收存至通知中心", quietOn, v =>
        {
            if (shellState != null)
            {
                shellState.SetQuietMode(v);
                ShowToast(v ? "已开启勿扰模式" : "已关闭勿扰模式");
            }
            else
            {
                ShowToast("系统面板服务暂未挂载");
            }
        });
        quietToggle.name = "settings-quiet-toggle-row";
        policyCard.Add(quietToggle);

        int notifCount = shellState?.Notifications.Count ?? 0;
        var clearRow = BuildWhiteRow("清空所有通知", $"当前收存 {notifCount} 条通知记录", () =>
        {
            if (shellState != null)
            {
                shellState.ClearNotifications();
                ShowToast("已清空所有通知记录");
            }
        });
        clearRow.name = "settings-clear-notifications-btn";
        policyCard.Add(clearRow);

        rightPane.Add(policyCard);

        // 通知管理：按应用分组列出各渠道，开关读写 TabletNotify 渠道状态
        AddSectionHeader(rightPane, "通知管理");
        var channelCard = BuildWhiteCard();
        channelCard.name = "settings-notification-channels-card";
        var channels = new List<TabletNotifyChannel>(TabletNotify.Channels);
        channels.Sort((a, b) => string.CompareOrdinal(a.AppName, b.AppName));
        if (channels.Count == 0)
        {
            channelCard.Add(BuildInfoRow("暂无通知渠道", "应用首次发送通知时会在这里登记"));
        }
        foreach (var ch in channels)
        {
            var channel = ch;
            string imp = channel.Importance == TabletNotifyImportance.High ? "重要" : "普通";
            var row = BuildToggleRow(channel.AppName + " · " + channel.Name, imp + "通知", channel.Enabled, v =>
            {
                channel.Enabled = v;
                ShowToast((v ? "已允许 " : "已关闭 ") + channel.AppName + " 的" + channel.Name);
            });
            row.name = "settings-channel-" + channel.Id;
            channelCard.Add(row);
        }
        rightPane.Add(channelCard);

        AddSectionHeader(rightPane, "运行机制与说明");
        var infoCard = BuildWhiteCard();
        infoCard.name = "settings-notifications-info-card";
        infoCard.Add(BuildWhiteRow("通知收存机制", "通知均来自应用实际运行提示；最多收存 32 条，超限自动淘汰早期记录", null, false));
        infoCard.Add(BuildWhiteRow("勿扰模式策略", "开启勿扰后系统保留通知收存，仅静默屏蔽悬浮横幅；勿扰状态持久化保存", null, false));
        infoCard.Add(BuildWhiteRow("系统连接说明", "车载平板处于内网沙箱运行；状态栏不虚拟 Wi-Fi / 蓝牙 / 电池连接状态", null, false));
        rightPane.Add(infoCard);
    }

    // 12 桌面：包含复古画风
    private static void RenderHomeScreenSettings(ScrollView rightPane, VisualElement win)
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

        AddSectionHeader(rightPane, "其他");
        var otherCard = BuildWhiteCard();
        otherCard.name = "settings-home-other-card";
        var navRow = BuildInfoRow("系统导航", CurrentNavMode() == RailwayRenaissance.UI.Tablet.SystemShell.TabletNavigationMode.Gestures ? "手势" : "按钮", null, () => NavigateToCategory(96, win));
        navRow.name = "settings-home-system-nav-row";
        otherCard.Add(navRow);
        otherCard.Add(BuildToggleRow("最近任务模糊预览", "在最近任务中模糊应用预览", TabletNavigationOptions.BlurRecents, v => TabletNavigationOptions.BlurRecents = v));
        rightPane.Add(otherCard);
    }

    private static RailwayRenaissance.UI.Tablet.SystemShell.TabletNavigationMode CurrentNavMode()
    {
        var prefs = GameMainUI.Instance?.PresentationPreferences;
        return prefs != null
            ? prefs.NavigationMode
            : (RailwayRenaissance.UI.Tablet.SystemShell.TabletNavigationMode)PlayerPrefs.GetInt("TabletNavMode", 0);
    }

    private static void ApplyNavMode(RailwayRenaissance.UI.Tablet.SystemShell.TabletNavigationMode mode)
    {
        GameMainUI.Instance?.PresentationPreferences?.SetNavigationMode(mode);
        PlayerPrefs.SetInt("TabletNavMode", mode == RailwayRenaissance.UI.Tablet.SystemShell.TabletNavigationMode.Gestures ? 0 : 1);
        PlayerPrefs.Save();
    }

    // 子页：系统导航（挂在“桌面”下，对照 HyperOS System navigation）
    private static void RenderSystemNavSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "系统导航");
        var blue = new Color(0.20f, 0.50f, 0.98f, 1f);
        bool gestures = CurrentNavMode() == RailwayRenaissance.UI.Tablet.SystemShell.TabletNavigationMode.Gestures;

        // 两种导航方式的缩略预览，选中项描蓝边
        var picks = new VisualElement { name = "settings-nav-previews" };
        picks.style.flexDirection = FlexDirection.Row;
        picks.style.marginBottom = 16;
        picks.Add(BuildNavPreviewCard(false, gestures, blue, win));
        picks.Add(BuildNavPreviewCard(true, !gestures, blue, win));
        rightPane.Add(picks);

        if (gestures)
        {
            var card = BuildWhiteCard();
            card.name = "settings-nav-gesture-card";
            card.Add(BuildToggleRow("隐藏全面屏指示条", "使用全面屏手势时不显示底部指示条", TabletNavigationOptions.HideIndicator, v => TabletNavigationOptions.HideIndicator = v));
            card.Add(BuildToggleRow("手势确认", "在部分场景（如玩游戏、看视频）中，手势需重复操作一次才会生效", TabletNavigationOptions.GestureConfirm, v => TabletNavigationOptions.GestureConfirm = v));
            card.Add(BuildToggleRow("不响应三指和四指手势", "在部分场景（如玩游戏、看视频）中将忽略这些手势", TabletNavigationOptions.IgnoreMultiFinger, v => TabletNavigationOptions.IgnoreMultiFinger = v));
            rightPane.Add(card);

            AddSectionHeader(rightPane, "快捷操作");
            var shortcuts = BuildWhiteCard();
            shortcuts.Add(BuildShortcutRow("长按全面屏指示条", TabletNavigationOptions.Slot.IndicatorLongPress));
            shortcuts.Add(BuildShortcutRow("双击全面屏指示条", TabletNavigationOptions.Slot.IndicatorDoubleTap));
            rightPane.Add(shortcuts);

            AddSectionHeader(rightPane, "手势演示");
            var demos = BuildWhiteCard();
            demos.name = "settings-nav-gesture-demos";
            demos.Add(BuildGestureDemoRow(0, "回到桌面", "从屏幕底部向上滑"));
            demos.Add(BuildGestureDemoRow(1, "打开最近任务", "从屏幕底部向上滑并停顿"));
            demos.Add(BuildGestureDemoRow(2, "返回", "从屏幕两侧向屏幕中间滑"));
            demos.Add(BuildGestureDemoRow(3, "切换应用", "在全面屏指示条或屏幕底部左右滑动"));
            rightPane.Add(demos);
        }
        else
        {
            var card = BuildWhiteCard();
            card.name = "settings-nav-button-card";
            card.Add(BuildToggleRow("镜像按钮", "镜像返回键与菜单键的位置", TabletNavigationOptions.MirrorButtons, v => TabletNavigationOptions.MirrorButtons = v));
            rightPane.Add(card);

            AddSectionHeader(rightPane, "快捷操作");
            var shortcuts = BuildWhiteCard();
            shortcuts.Add(BuildShortcutRow("长按菜单键", TabletNavigationOptions.Slot.MenuLongPress));
            shortcuts.Add(BuildShortcutRow("长按主页键", TabletNavigationOptions.Slot.HomeLongPress));
            shortcuts.Add(BuildShortcutRow("长按返回键", TabletNavigationOptions.Slot.BackLongPress));
            rightPane.Add(shortcuts);
        }
    }

    private static VisualElement BuildNavPreviewCard(bool buttons, bool selected, Color blue, VisualElement win)
    {
        var col = new VisualElement { name = buttons ? "settings-nav-preview-buttons" : "settings-nav-preview-gestures", pickingMode = PickingMode.Position };
        col.style.flexGrow = 1; col.style.flexBasis = 0;
        col.style.alignItems = Align.Stretch;
        col.style.marginLeft = buttons ? 10 : 0; col.style.marginRight = buttons ? 0 : 10;

        var frame = new VisualElement { pickingMode = PickingMode.Ignore };
        frame.style.height = 178;
        frame.style.paddingLeft = 4; frame.style.paddingRight = 4; frame.style.paddingTop = 4; frame.style.paddingBottom = 4;
        frame.style.borderTopLeftRadius = frame.style.borderTopRightRadius = 26;
        frame.style.borderBottomLeftRadius = frame.style.borderBottomRightRadius = 26;
        var edge = selected ? blue : Color.clear;
        frame.style.borderTopWidth = frame.style.borderBottomWidth = frame.style.borderLeftWidth = frame.style.borderRightWidth = 3;
        frame.style.borderTopColor = frame.style.borderBottomColor = frame.style.borderLeftColor = frame.style.borderRightColor = edge;

        var screen = new VisualElement { pickingMode = PickingMode.Ignore };
        screen.style.flexGrow = 1;
        screen.style.backgroundColor = new Color(0.30f, 0.50f, 0.98f, 1f);
        screen.style.borderTopLeftRadius = screen.style.borderTopRightRadius = 20;
        screen.style.borderBottomLeftRadius = screen.style.borderBottomRightRadius = 20;
        screen.style.overflow = Overflow.Hidden;

        var lower = new VisualElement { pickingMode = PickingMode.Ignore };   // 下半部分略浅，模拟壁纸渐变
        lower.style.position = Position.Absolute;
        lower.style.left = 0; lower.style.right = 0; lower.style.bottom = 0; lower.style.height = Length.Percent(55);
        lower.style.backgroundColor = new Color(1f, 1f, 1f, 0.16f);
        screen.Add(lower);

        // 桌面小部件与图标示意
        void Tile(float l, float t, float w, float h, float a, float r)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.left = Length.Percent(l); e.style.top = Length.Percent(t);
            e.style.width = Length.Percent(w); e.style.height = Length.Percent(h);
            e.style.backgroundColor = new Color(1f, 1f, 1f, a);
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;
            screen.Add(e);
        }
        Tile(5, 9, 11, 17, 0.22f, 5); Tile(18, 9, 11, 17, 0.22f, 5); Tile(31, 9, 25, 17, 0.28f, 5); Tile(59, 9, 25, 17, 0.28f, 5);
        Tile(5, 30, 24, 12, 0.22f, 5);
        for (int i = 0; i < 6; i++) { Tile(5 + i * 15f, 50, 8, 11, 0.30f, 4); Tile(5 + i * 15f, 66, 8, 11, 0.30f, 4); }
        Tile(18, 80, 42, 10, 0.30f, 6);   // Dock

        if (!buttons)
        {
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.style.position = Position.Absolute;
            bar.style.left = Length.Percent(33); bar.style.width = Length.Percent(34);
            bar.style.bottom = 7; bar.style.height = 3;
            bar.style.backgroundColor = Color.white;
            bar.style.borderTopLeftRadius = bar.style.borderTopRightRadius = 2;
            bar.style.borderBottomLeftRadius = bar.style.borderBottomRightRadius = 2;
            screen.Add(bar);
        }
        else
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.position = Position.Absolute;
            row.style.left = 0; row.style.right = 0; row.style.bottom = 3; row.style.height = 18;
            row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.Center; row.style.alignItems = Align.Center;
            foreach (var kind in new[] { TabletIconKind.Dots, TabletIconKind.Circle, TabletIconKind.Back })
            {
                var ic = new TabletIconElement(kind, 13f, Color.white) { pickingMode = PickingMode.Ignore };
                ic.style.marginLeft = 16; ic.style.marginRight = 16;
                row.Add(ic);
            }
            screen.Add(row);
        }
        frame.Add(screen);
        col.Add(frame);

        var label = new Label(buttons ? "按钮" : "手势") { pickingMode = PickingMode.Ignore };
        label.style.fontSize = 18; label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.color = selected ? blue : InkPrimary; label.style.unityFontDefinition = Fd();
        label.style.marginTop = 10;
        col.Add(label);

        col.RegisterCallback<ClickEvent>(_ =>
        {
            if (selected) return;
            ApplyNavMode(buttons
                ? RailwayRenaissance.UI.Tablet.SystemShell.TabletNavigationMode.ThreeButton
                : RailwayRenaissance.UI.Tablet.SystemShell.TabletNavigationMode.Gestures);
            ShowToast(buttons ? "已切换为按钮导航" : "已切换为手势导航");
            RefreshCurrentPage();
        });
        return col;
    }

    // 快捷操作行：右侧显示当前动作，点一下轮换（真机是下拉选择）
    private static VisualElement BuildShortcutRow(string title, TabletNavigationOptions.Slot slot)
    {
        var row = BuildInfoRow(title, TabletNavigationOptions.GetActionName(slot), null, null);
        row.pickingMode = PickingMode.Position;
        AddRowFeedback(row);
        var value = (Label)((VisualElement)row[1])[0];
        var arrow = new Label("›");
        arrow.style.fontSize = 20; arrow.style.color = InkTertiary; arrow.style.unityFontDefinition = Fd(); arrow.style.marginLeft = 6;
        ((VisualElement)row[1]).Add(arrow);
        row.RegisterCallback<ClickEvent>(_ =>
        {
            value.text = TabletNavigationOptions.CycleAction(slot);
            ShowToast(title + "：" + value.text);
        });
        return row;
    }

    // 手势演示行：左侧小示意图（蓝点循环演示手势轨迹），右侧标题与说明
    private static VisualElement BuildGestureDemoRow(int kind, string title, string desc)
    {
        var row = new VisualElement { pickingMode = PickingMode.Position };
        row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
        row.style.paddingTop = 16; row.style.paddingBottom = 16; row.style.paddingLeft = 8; row.style.paddingRight = 8;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = InsetDivider;
        row.style.borderTopLeftRadius = row.style.borderTopRightRadius = 10;
        row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 10;
        AddRowFeedback(row);
        row.RegisterCallback<ClickEvent>(_ => ShowToast(title + "：" + desc));

        var line = new Color(0.76f, 0.78f, 0.88f, 1f);
        var shell = new VisualElement { pickingMode = PickingMode.Ignore };
        shell.style.width = 188; shell.style.height = 120; shell.style.flexShrink = 0; shell.style.marginRight = 28;
        shell.style.borderTopWidth = shell.style.borderBottomWidth = shell.style.borderLeftWidth = shell.style.borderRightWidth = 3;
        shell.style.borderTopColor = shell.style.borderBottomColor = shell.style.borderLeftColor = shell.style.borderRightColor = line;
        shell.style.borderTopLeftRadius = shell.style.borderTopRightRadius = 16;
        shell.style.borderBottomLeftRadius = shell.style.borderBottomRightRadius = 16;
        shell.style.backgroundColor = Color.white;
        shell.style.overflow = Overflow.Hidden;

        void Panel(float l, float t, float r, float b, float a)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.left = Length.Percent(l); e.style.top = Length.Percent(t);
            e.style.right = Length.Percent(r); e.style.bottom = Length.Percent(b);
            e.style.backgroundColor = new Color(0.86f, 0.87f, 0.94f, a);
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = 5;
            e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = 5;
            shell.Add(e);
        }
        if (kind == 0) Panel(24, 12, 24, 32, 1f);
        else if (kind == 1) { Panel(24, 12, 24, 32, 1f); Panel(0, 12, 82, 32, 0.8f); Panel(82, 12, 0, 32, 0.8f); }
        else if (kind == 3) { Panel(0, 14, 78, 34, 0.85f); Panel(28, 14, 0, 34, 1f); }
        if (kind == 2) Panel(0, 30, 93, 30, 1f);   // 左侧边缘的返回热区

        var bar = new VisualElement { pickingMode = PickingMode.Ignore };
        bar.style.position = Position.Absolute;
        bar.style.left = Length.Percent(32); bar.style.width = Length.Percent(36); bar.style.bottom = 6; bar.style.height = 3;
        bar.style.backgroundColor = line;
        bar.style.borderTopLeftRadius = bar.style.borderTopRightRadius = 2;
        bar.style.borderBottomLeftRadius = bar.style.borderBottomRightRadius = 2;
        shell.Add(bar);

        var trail = new VisualElement { pickingMode = PickingMode.Ignore };   // 蓝点身后的淡蓝拖尾
        trail.style.position = Position.Absolute;
        trail.style.backgroundColor = new Color(0.20f, 0.50f, 0.98f, 0.22f);
        trail.style.borderTopLeftRadius = trail.style.borderTopRightRadius = 7;
        trail.style.borderBottomLeftRadius = trail.style.borderBottomRightRadius = 7;
        trail.style.opacity = 0f;
        shell.Add(trail);
        var dot = new VisualElement { pickingMode = PickingMode.Ignore };
        dot.style.position = Position.Absolute;
        dot.style.width = 14; dot.style.height = 14;
        dot.style.backgroundColor = new Color(0.20f, 0.50f, 0.98f, 1f);
        dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = 7;
        dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 7;
        dot.style.opacity = 0f;
        shell.Add(dot);
        row.Add(shell);

        float t0 = kind * 0.35f;
        var anim = row.schedule.Execute(() =>
        {
            t0 += 0.033f;
            float k = Mathf.Repeat(t0, 2.6f) / 2.6f;                 // 0..1 一轮
            float move = Mathf.Clamp01(k / 0.65f);                   // 前 65% 位移，后面停顿淡出
            move = 1f - (1f - move) * (1f - move);                   // easeOut
            float alpha = k < 0.1f ? k / 0.1f : (k > 0.85f ? Mathf.Clamp01((1f - k) / 0.15f) : 1f);
            float x, y, sx = 0f, sy = 0f;
            switch (kind)
            {
                case 0: sx = x = 86f; sy = 92f; y = Mathf.Lerp(sy, 50f, move); break;
                case 1: sx = x = 86f; sy = 92f; y = Mathf.Lerp(sy, 58f, move); break;
                case 2: sx = 8f; sy = y = 62f; x = Mathf.Lerp(sx, 82f, move); break;
                default: sx = 64f; sy = y = 92f; x = Mathf.Lerp(sx, 122f, move); break;
            }
            dot.style.left = x; dot.style.top = y;
            dot.style.opacity = alpha;
            bool vertical = kind <= 1;
            trail.style.left = vertical ? x : sx;
            trail.style.top = vertical ? y : y;
            trail.style.width = vertical ? 14f : Mathf.Max(14f, x - sx + 14f);
            trail.style.height = vertical ? Mathf.Max(14f, sy - y + 14f) : 14f;
            trail.style.opacity = alpha;
        }).Every(33);
        row.RegisterCallback<DetachFromPanelEvent>(_ => anim.Pause());

        var text = new VisualElement { pickingMode = PickingMode.Ignore };
        text.style.flexGrow = 1; text.style.flexShrink = 1;
        text.Add(new Label(title) { pickingMode = PickingMode.Ignore, style = { fontSize = 19, color = InkPrimary, unityFontDefinition = Fd() } });
        text.Add(new Label(desc) { pickingMode = PickingMode.Ignore, style = { fontSize = 15, color = InkSecondary, unityFontDefinition = Fd(), marginTop = 4, whiteSpace = WhiteSpace.Normal } });
        row.Add(text);
        row.Add(new Label("›") { pickingMode = PickingMode.Ignore, style = { fontSize = 20, color = InkTertiary, unityFontDefinition = Fd(), marginLeft = 8 } });
        return row;
    }

    // 13 显示与亮度：老设置「显示」8条全量并入
    private static void RenderDisplaySettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "显示与亮度");

        // 外观模式（浅色 / 深色）：目前只保存偏好并广播 ThemeChanged，界面适配以后再接
        var modeRow = new VisualElement { name = "settings-theme-mode-row" };
        modeRow.style.flexDirection = FlexDirection.Row;
        modeRow.style.marginBottom = 16;
        rightPane.Add(modeRow);
        Action buildModeCards = null;
        buildModeCards = () =>
        {
            modeRow.Clear();
            var current = TabletDisplayPreferences.Theme;
            foreach (var mode in new[] { TabletThemeMode.Light, TabletThemeMode.Dark })
            {
                var m = mode;
                bool selected = current == m;
                var cell = new VisualElement { name = "settings-theme-" + (m == TabletThemeMode.Light ? "light" : "dark") };
                cell.style.flexGrow = 1; cell.style.flexBasis = 0;
                cell.style.alignItems = Align.Center;
                cell.style.marginLeft = m == TabletThemeMode.Dark ? 10 : 0;
                cell.style.marginRight = m == TabletThemeMode.Light ? 10 : 0;
                cell.pickingMode = PickingMode.Position;

                var preview = new VisualElement { pickingMode = PickingMode.Ignore };
                preview.style.alignSelf = Align.Stretch; preview.style.height = 150;
                preview.style.backgroundColor = new Color(0.30f, 0.52f, 0.99f, 1f);
                preview.style.borderTopLeftRadius = preview.style.borderTopRightRadius = 24;
                preview.style.borderBottomLeftRadius = preview.style.borderBottomRightRadius = 24;
                preview.style.justifyContent = Justify.Center; preview.style.alignItems = Align.Center;
                float bw = selected ? 4f : 0f;
                preview.style.borderTopWidth = preview.style.borderBottomWidth = preview.style.borderLeftWidth = preview.style.borderRightWidth = bw;
                preview.style.borderTopColor = preview.style.borderBottomColor = preview.style.borderLeftColor = preview.style.borderRightColor = AccentSystem;
                for (int k = 0; k < 2; k++)
                {
                    var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                    bar.style.width = Length.Percent(52); bar.style.height = 34;
                    bar.style.marginTop = k == 0 ? 0 : 10;
                    bar.style.backgroundColor = m == TabletThemeMode.Light
                        ? new Color(0.50f, 0.68f, 1f, 1f)
                        : new Color(0.17f, 0.31f, 0.72f, 1f);
                    bar.style.borderTopLeftRadius = bar.style.borderTopRightRadius = 10;
                    bar.style.borderBottomLeftRadius = bar.style.borderBottomRightRadius = 10;
                    preview.Add(bar);
                }
                cell.Add(preview);

                var lbl = new Label(m == TabletThemeMode.Light ? "浅色模式" : "深色模式") { pickingMode = PickingMode.Ignore };
                lbl.style.fontSize = 18; lbl.style.marginTop = 10;
                lbl.style.color = selected ? AccentSystem : InkPrimary;
                lbl.style.unityFontDefinition = Fd();
                cell.Add(lbl);

                cell.RegisterCallback<ClickEvent>(_ =>
                {
                    if (TabletDisplayPreferences.Theme == m) return;
                    TabletDisplayPreferences.SetTheme(m);
                    if (m == TabletThemeMode.Dark && !TabletDisplayPreferences.DarkThemeAdapted)
                        ShowToast("已记录深色偏好，界面的深色适配将在后续版本推出");
                    buildModeCards();
                });
                modeRow.Add(cell);
            }
        };
        buildModeCards();

        // 屏幕亮度：全局生效（整个画面叠一层黑色），默认 100% 即不压暗
        AddSectionHeader(rightPane, "亮度");
        var brightCard = BuildWhiteCard();
        brightCard.name = "settings-brightness-card";
        brightCard.Add(BuildSliderRow("屏幕亮度", Mathf.RoundToInt(TabletDisplayPreferences.MinBrightness * 100f), 100,
            Mathf.RoundToInt(TabletDisplayPreferences.Brightness * 100f),
            v => TabletDisplayPreferences.SetBrightness(v / 100f), " %", true, "☀"));
        rightPane.Add(brightCard);

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
            RefreshCurrentPage();
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
            RefreshCurrentPage();
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

        card.Add(BuildWhiteRow("界面缩放方案", "2.5K/4K 屏幕自适应对齐（ScaleMode=2）", null, false));
        rightPane.Add(card);

        // 剧情对话的显示设置放在“显示”里：带实时预览，改动立即看到效果，进入剧情后同样生效
        AddSectionHeader(rightPane, "对话与阅读");
        var readCard = BuildWhiteCard();
        readCard.name = "settings-dialogue-card";
        var preview = BuildDialoguePreview(PlayerPrefs.GetInt("DialogAlpha", 80), PlayerPrefs.GetInt("TextSpeed", 5));
        readCard.Add(preview.root);
        readCard.Add(BuildSliderRow("文字速度", 1, 10, PlayerPrefs.GetInt("TextSpeed", 5), v =>
        {
            PlayerPrefs.SetInt("TextSpeed", v);
            PlayerPrefs.Save();
            preview.replay(v);
        }, " 档"));
        readCard.Add(BuildSliderRow("对话框透明度", 0, 100, PlayerPrefs.GetInt("DialogAlpha", 80), v =>
        {
            PlayerPrefs.SetInt("DialogAlpha", v);
            PlayerPrefs.Save();
            preview.setAlpha(v);
        }, " %"));
        rightPane.Add(readCard);

        AddSectionHeader(rightPane, "剧情字幕语言");
        rightPane.Add(BuildSubtitleLanguageCard());

        AddSectionHeader(rightPane, "剧情配音与氛围");
        var voiceCard = BuildWhiteCard();
        voiceCard.name = "settings-vn-presentation";
        voiceCard.Add(BuildToggleRow("剧情信号灯", "用日式铁路信号表现剧情氛围，可随时关闭", VNPresentationSettings.StorySignal, VNPresentationSettings.SetSignal));
        var voiceNames = new List<string> { "关闭", "日本語", "中文", "English" };
        var voiceCodes = new[] { "none", "ja", "zh-Hans", "en" };
        int voiceIndex = System.Array.IndexOf(voiceCodes, VNVoicePlayer.Language);
        voiceCard.Add(BuildSubtitleLanguageRow("配音语言", "settings-vn-voice-language", voiceNames, voiceNames[Mathf.Max(0, voiceIndex)], value =>
        {
            PlayerPrefs.SetString("VNVoiceLanguage", voiceCodes[voiceNames.IndexOf(value)]);
            PlayerPrefs.Save();
            VNManager.Instance?.RefreshVoiceLanguage();
        }));
        voiceCard.Add(BuildSliderRow("配音音量", 0, 100, Mathf.RoundToInt(VNPresentationSettings.VoiceVolume * 100), value => VNPresentationSettings.SetVoiceVolume(value / 100f), " %"));
        rightPane.Add(voiceCard);


        var resetCard = BuildWhiteCard();
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
            VNSubtitleSettings.Reset();
            PlayerPrefs.Save();
            Application.targetFrameRate = 60;
            FPSDisplay.SetActive(false);
            ShowToast("显示设置已恢复默认");
            RefreshCurrentPage();
        }) { text = "恢复显示默认设置" };
        StyleActionBtn(resetBtn);
        resetBtn.style.marginTop = 6; resetBtn.style.marginBottom = 6;
        resetCard.Add(resetBtn);
        rightPane.Add(resetCard);
    }

    private static VisualElement BuildSubtitleLanguageCard()
    {
        var card = BuildWhiteCard();
        card.name = "settings-subtitle-language-card";
        Action render = null;
        render = () =>
        {
            card.Clear();
            string primary = VNSubtitleSettings.PrimaryLanguage;
            string secondary = VNSubtitleSettings.SecondaryLanguage;
            var languages = VNLocalization.Languages;
            var primaryNames = new List<string>();
            foreach (var language in languages) primaryNames.Add(language.name);
            string primaryName = System.Array.Find(languages, l => l.code == primary).name;
            card.Add(BuildSubtitleLanguageRow("主字幕语言", "settings-primary-subtitle-language",
                primaryNames, primaryName, value =>
                {
                    VNSubtitleSettings.SetPrimaryLanguage(System.Array.Find(languages, l => l.name == value).code);
                    render();
                }));
            var secondaryNames = new List<string> { "关闭" };
            foreach (var language in languages) if (language.code != primary) secondaryNames.Add(language.name);
            string secondaryName = secondary == VNSubtitleSettings.Off ? "关闭" : System.Array.Find(languages, l => l.code == secondary).name;
            card.Add(BuildSubtitleLanguageRow("副字幕语言", "settings-secondary-subtitle-language",
                secondaryNames, secondaryName, value =>
                {
                    VNSubtitleSettings.SetSecondaryLanguage(value == "关闭" ? VNSubtitleSettings.Off : System.Array.Find(languages, l => l.name == value).code);
                    render();
                }));
            string size = VNSubtitleSettings.SecondaryFontSize <= 16 ? "小" : VNSubtitleSettings.SecondaryFontSize >= 22 ? "大" : "标准";
            var sizeRow = BuildSubtitleLanguageRow("副字幕字号", "settings-secondary-subtitle-size",
                new List<string> { "小", "标准", "大" }, size, value =>
                {
                    VNSubtitleSettings.SetSecondaryFontSize(value == "小" ? 16 : value == "大" ? 22 : 18);
                    render();
                });
            sizeRow.SetEnabled(secondary != VNSubtitleSettings.Off);
            card.Add(sizeRow);
            var note = new Label("缺少译文的对白显示中文。语言设置仅用于剧情字幕。" ) { name = "settings-subtitle-language-note" };
            note.style.fontSize = 12; note.style.color = InkSecondary;
            note.style.unityFontDefinition = Fd(); note.style.whiteSpace = WhiteSpace.Normal;
            note.style.marginTop = 10; note.style.marginBottom = 10;
            card.Add(note);
            var preview = new VisualElement { name = "settings-subtitle-language-preview" };
            preview.style.backgroundColor = new Color(0.11f, 0.075f, 0.05f, 0.94f);
            preview.style.borderTopLeftRadius = preview.style.borderTopRightRadius = 10;
            preview.style.borderBottomLeftRadius = preview.style.borderBottomRightRadius = 10;
            preview.style.paddingTop = preview.style.paddingBottom = 14;
            preview.style.paddingLeft = preview.style.paddingRight = 16;
            var name = new Label("岁月");name.style.color = new Color(1f, 0.78f, 0.4f);
            name.style.fontSize = 13;name.style.unityFontDefinition = Fd();name.style.marginBottom = 6;preview.Add(name);
            const string zh = "前方五公里。快到车站了！";
            var previewLine = new VNLocalizedLine(zh);
            previewLine.Add("ja", new VNTranslationEntry { text = "この先五キロ。もうすぐ駅に着きます！", segments = new[] {
                new VNTranslationSegment { source = "前方五公里。", text = "この先五キロ。" },
                new VNTranslationSegment { source = "快到车站了！", text = "もうすぐ駅に着きます！" } } });
            previewLine.Add("en", new VNTranslationEntry { text = "Five kilometers ahead. We're almost at the station!", segments = new[] {
                new VNTranslationSegment { source = "前方五公里。", text = "Five kilometers ahead. " },
                new VNTranslationSegment { source = "快到车站了！", text = "We're almost at the station!" } } });
            var pairs = previewLine.Sentences();
            if (pairs.Count == 0) pairs.Add(new VNSubtitleSentence(previewLine.PrimaryText, null));
            var previewFlow = new VisualElement();previewFlow.style.flexDirection = FlexDirection.Row;
            previewFlow.style.flexWrap = Wrap.Wrap;previewFlow.style.alignItems = Align.FlexStart;preview.Add(previewFlow);
            for (int i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                var group = new VisualElement();group.style.marginBottom = 8;group.style.marginRight = 0;
                group.style.maxWidth = Length.Percent(100);group.style.flexShrink = 0;
                float preferredSub = 0;
                if (!string.IsNullOrEmpty(pair.Secondary))
                {
                    var sub = new Label(pair.Secondary) { name = "subtitle-preview-secondary-" + i };
                    sub.style.fontSize = VNSubtitleSettings.SecondaryFontSize * 0.7f;
                    sub.style.color = new Color(0.90f, 0.78f, 0.54f);sub.style.whiteSpace = WhiteSpace.Normal;
                    sub.style.unityTextAlign = TextAnchor.UpperCenter;
                    sub.style.unityFontDefinition = new FontDefinition { font = TabletFonts.Pixel() };sub.style.marginBottom = 4;group.Add(sub);
                    foreach (char c in pair.Secondary) preferredSub += (c > 0x2000 ? 1f : 0.6f) * VNSubtitleSettings.SecondaryFontSize * 0.7f;
                }
                var main = new Label(pair.Primary) { name = "subtitle-preview-primary-" + i };
                main.style.fontSize = 22;main.style.color = Color.white;main.style.whiteSpace = WhiteSpace.Normal;
                main.style.unityFontDefinition = new FontDefinition { font = TabletFonts.Pixel() };group.Add(main);
                float preferredMain = 0;
                foreach (char c in pair.Primary) preferredMain += (c > 0x2000 ? 1f : 0.6f) * 22;
                group.style.width = preferredMain;
                previewFlow.Add(group);
            }
            card.Add(preview);
        };
        render();
        return card;
    }

    private static VisualElement BuildSubtitleLanguageRow(string title, string name, List<string> choices, string current, Action<string> changed)
    {
        var row = new VisualElement();row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;row.style.justifyContent = Justify.SpaceBetween;
        row.style.minHeight = 58;row.style.paddingTop = row.style.paddingBottom = 8;
        row.style.borderBottomWidth = 1;row.style.borderBottomColor = BorderDivider;
        var label = new Label(title);label.style.fontSize = 15;label.style.color = InkPrimary;
        label.style.unityFontDefinition = Fd();label.style.flexShrink = 1;row.Add(label);
        var field = new DropdownField(choices, choices.IndexOf(current)) { name = name };
        field.style.width = 150;field.style.height = 40;field.style.flexShrink = 0;
        field.style.fontSize = 15;field.style.unityFontDefinition = Fd();
        var input = field.Q(className: "unity-base-popup-field__input") ?? field.Q(className: "unity-base-field__input");
        if (input != null)
        {
            input.style.backgroundColor = new Color(0.95f, 0.95f, 0.97f);
            input.style.borderTopLeftRadius = input.style.borderTopRightRadius = 12;
            input.style.borderBottomLeftRadius = input.style.borderBottomRightRadius = 12;
            input.style.borderTopWidth = input.style.borderRightWidth = input.style.borderBottomWidth = input.style.borderLeftWidth = 0;
            input.style.paddingLeft = input.style.paddingRight = 12;
            input.style.color = InkPrimary;
        }
        field.RegisterValueChangedCallback(evt => changed(evt.newValue));row.Add(field);
        return row;
    }

    // 对话框预览：模拟剧情对话框，按当前“文字速度”逐字打出示例句，按“透明度”调整底色；点击预览可重播
    private static (VisualElement root, System.Action<int> replay, System.Action<int> setAlpha) BuildDialoguePreview(int alphaSetting, int speedSetting)
    {
        const string sample = "雾峰村线的清晨，汽笛声从远处传来……";
        var root = new VisualElement { name = "settings-dialogue-preview" };
        root.style.height = 140;
        root.style.marginTop = 8; root.style.marginBottom = 8;
        root.style.paddingLeft = 12; root.style.paddingRight = 12; root.style.paddingBottom = 12;
        root.style.justifyContent = Justify.FlexEnd;
        root.style.overflow = Overflow.Hidden;
        root.style.borderTopLeftRadius = root.style.borderTopRightRadius = 18;
        root.style.borderBottomLeftRadius = root.style.borderBottomRightRadius = 18;
        root.style.backgroundColor = new Color(0.42f, 0.50f, 0.46f, 1f);   // 示意的场景底色，用来衬出对话框透明度

        var box = new VisualElement();
        box.style.paddingLeft = 14; box.style.paddingRight = 14;
        box.style.paddingTop = 10; box.style.paddingBottom = 10;
        box.style.minHeight = 66;
        box.style.borderTopLeftRadius = box.style.borderTopRightRadius = 12;
        box.style.borderBottomLeftRadius = box.style.borderBottomRightRadius = 12;
        root.Add(box);

        var speaker = new Label("旅人");
        speaker.style.fontSize = 12; speaker.style.color = new Color(0.83f, 0.66f, 0.26f, 1f);
        speaker.style.unityFontDefinition = Fd();
        box.Add(speaker);

        var text = new Label(string.Empty);
        text.style.fontSize = 15; text.style.color = new Color(0.93f, 0.87f, 0.72f, 1f);
        text.style.unityFontDefinition = Fd(); text.style.marginTop = 3;
        text.style.whiteSpace = WhiteSpace.Normal;
        box.Add(text);

        var hint = new Label("点击预览重播");
        hint.style.position = Position.Absolute;
        hint.style.right = 14; hint.style.top = 10;
        hint.style.fontSize = 11; hint.style.color = new Color(1f, 1f, 1f, 0.75f);
        hint.style.unityFontDefinition = Fd();
        hint.pickingMode = PickingMode.Ignore;
        root.Add(hint);

        int currentSpeed = speedSetting;
        IVisualElementScheduledItem job = null;
        System.Action<int> setAlpha = a =>
        {
            float alpha = Mathf.Clamp(0.92f * Mathf.Clamp(a, 0, 100) / 80f, 0.15f, 1f);   // 与剧情对话框使用同一换算
            box.style.backgroundColor = new Color(25f / 255f, 17f / 255f, 12f / 255f, alpha);
        };
        System.Action<int> replay = sp =>
        {
            currentSpeed = Mathf.Clamp(sp, 1, 10);
            job?.Pause();
            int shown = 0;
            text.text = string.Empty;
            // 与剧情打字机同一换算：默认档 5 为每字 0.03 秒
            float interval = Mathf.Clamp(0.03f * 5f / currentSpeed, 0.006f, 0.15f);
            job = root.schedule.Execute(() =>
            {
                if (shown < sample.Length) { shown++; text.text = sample.Substring(0, shown); }
                else job?.Pause();
            }).Every(System.Math.Max(1L, (long)(interval * 1000f)));
        };
        setAlpha(alphaSetting);
        replay(speedSetting);
        root.RegisterCallback<ClickEvent>(_ => replay(currentSpeed));
        return (root, replay, setAlpha);
    }

    // 14 声音与振动：老设置「音频」全量并入
    private static void RenderAudioSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "声音与振动");

        AddSectionHeader(rightPane, "调节音量");
        var card = BuildWhiteCard();
        card.name = "settings-volume-card";

        card.Add(BuildSliderRow("主音量", 0, 100, Mathf.RoundToInt(GameData.MasterVolume * 100), v =>
        {
            GameData.MasterVolume = v / 100f;
        }, " %", true, "🔊"));

        card.Add(BuildSliderRow("背景音乐", 0, 100, Mathf.RoundToInt(GameData.BGMVolume * 100), v =>
        {
            GameData.BGMVolume = v / 100f;
        }, " %", true, "🔊"));

        card.Add(BuildSliderRow("音效", 0, 100, Mathf.RoundToInt(GameData.SFXVolume * 100), v =>
        {
            GameData.SFXVolume = v / 100f;
        }, " %", true, "🔔"));

        card.Add(BuildSliderRow("打字机音效", 0, 100, Mathf.RoundToInt(GameData.TypewriterVolume * 100), v =>
        {
            GameData.TypewriterVolume = v / 100f;
        }, " %", true, "⌨"));
        rightPane.Add(card);

        AddSectionHeader(rightPane, "声音选项");
        var optCard = BuildWhiteCard();
        optCard.Add(BuildWhiteRow("铁道音效模拟", "已启用（先民柴油机车鸣笛与道岔金属撞击）", null, false));
        optCard.Add(BuildWhiteRow("恢复默认音量", null, () =>
        {
            GameData.MasterVolume = 1f;
            GameData.BGMVolume = 0.8f;
            GameData.SFXVolume = 1f;
            GameData.TypewriterVolume = 0.12f;
            ShowToast("音频设置已恢复默认");
            RefreshCurrentPage();
        }));
        rightPane.Add(optCard);
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
            RefreshCurrentPage();
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

        AddSectionHeader(page, "屏幕锁定");
        var credCard = BuildWhiteCard();
        credCard.name = "settings-lock-credential-card";
        credCard.Add(BuildInfoRow("锁屏方式", "轻触解锁", "轻触屏幕或按空格键进入桌面"));
        credCard.Add(BuildInfoRow("面容数据", "未录入", "此终端未录入任何生物识别数据"));
        credCard.Add(BuildInfoRow("PIN 码与图案", "未设置", "未设置锁屏密码"));
        page.Add(credCard);

        AddSectionHeader(page, "说明");
        var policyCard = BuildWhiteCard();
        policyCard.name = "settings-lock-policy-card";
        policyCard.Add(BuildInfoRow("安全等级", "无凭据保护", "锁屏页只负责展示时间与问候，不是凭据保护；终端的防护由白头疫苗与系统沙箱承担"));
        page.Add(policyCard);

        rightPane.Add(page);
    }

    // 子页：存储（挂在“我的设备”下）。数字全部来自 TabletStorageLedger
    private static VisualElement BuildSegmentBar(float height, (long value, Color color)[] segs)
    {
        var bar = new VisualElement();
        bar.style.flexDirection = FlexDirection.Row;
        bar.style.height = height;
        bar.style.overflow = Overflow.Hidden;
        bar.style.backgroundColor = new Color(0.93f, 0.93f, 0.95f, 1f);
        bar.style.borderTopLeftRadius = bar.style.borderTopRightRadius = height * 0.5f;
        bar.style.borderBottomLeftRadius = bar.style.borderBottomRightRadius = height * 0.5f;
        foreach (var sg in segs)
        {
            if (sg.value <= 0) continue;
            var seg = new VisualElement();
            seg.style.flexGrow = sg.value; seg.style.flexBasis = 0;
            seg.style.minWidth = 3;
            seg.style.backgroundColor = sg.color;
            bar.Add(seg);
        }
        return bar;
    }

    private static VisualElement BuildLegendRow((Color color, string text)[] items)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap;
        row.style.marginTop = 12;
        foreach (var it in items)
        {
            var d = BuildLegendDot(it.color, it.text);
            d.style.marginLeft = 0; d.style.marginRight = 22;
            row.Add(d);
        }
        return row;
    }

    // 子页：云空间（挂在“账号”下，对照 Xiaomi Cloud 页）。数字来自 TabletStorageLedger
    // 账号页右侧小卡：左侧彩色图标 + 标题 + 短副标题
    private static VisualElement BuildAccountMiniCard(TabletIconKind kind, Color tint, string title, string sub, Action onClick)
    {
        var card = BuildWhiteCard();
        card.style.flexGrow = 1;
        card.style.flexDirection = FlexDirection.Row;
        card.style.alignItems = Align.Center;
        card.pickingMode = PickingMode.Position;
        AddRowFeedback(card);
        card.RegisterCallback<ClickEvent>(_ => onClick?.Invoke());
        var tile = new VisualElement { pickingMode = PickingMode.Ignore };
        tile.style.width = 40; tile.style.height = 40; tile.style.flexShrink = 0; tile.style.marginRight = 14;
        tile.style.alignItems = Align.Center; tile.style.justifyContent = Justify.Center;
        tile.style.backgroundColor = tint;
        tile.style.borderTopLeftRadius = tile.style.borderTopRightRadius = 20;
        tile.style.borderBottomLeftRadius = tile.style.borderBottomRightRadius = 20;
        tile.Add(new TabletIconElement(kind, 22, Color.white) { pickingMode = PickingMode.Ignore });
        card.Add(tile);
        var col = new VisualElement { pickingMode = PickingMode.Ignore };
        col.style.flexShrink = 1;
        col.Add(new Label(title) { pickingMode = PickingMode.Ignore, style = { fontSize = 17, color = InkPrimary, unityFontDefinition = FdBold() } });
        col.Add(new Label(sub) { pickingMode = PickingMode.Ignore, style = { fontSize = 13, color = InkSecondary, unityFontDefinition = Fd(), marginTop = 2 } });
        card.Add(col);
        return card;
    }

    // 给设置行加一枚左侧线条图标（云空间权益等）
    private static VisualElement WithLeadingIcon(VisualElement row, TabletIconKind kind, Color tint)
    {
        var holder = new VisualElement { pickingMode = PickingMode.Ignore };
        holder.style.width = 30; holder.style.flexShrink = 0; holder.style.marginRight = 14;
        holder.style.alignItems = Align.Center; holder.style.justifyContent = Justify.Center;
        holder.Add(new TabletIconElement(kind, 26, tint) { pickingMode = PickingMode.Ignore });
        row.Insert(0, holder);
        return row;
    }

    private const string CloudAutoBackupKey = "TabletCloudAutoBackup";

    private static void RenderCloudSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "云空间");
        if (!AccountService.IsRegistered)
        {
            var lockCard = BuildWhiteCard();
            lockCard.name = "settings-cloud-login-required";
            lockCard.style.alignItems = Align.Center;
            lockCard.style.paddingTop = 40; lockCard.style.paddingBottom = 40;
            var lockIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            lockIcon.style.width = 64; lockIcon.style.height = 64; lockIcon.style.marginBottom = 18;
            lockIcon.style.alignItems = Align.Center; lockIcon.style.justifyContent = Justify.Center;
            lockIcon.style.backgroundColor = new Color(0.62f, 0.64f, 0.70f, 1f);
            lockIcon.style.borderTopLeftRadius = lockIcon.style.borderTopRightRadius = 20;
            lockIcon.style.borderBottomLeftRadius = lockIcon.style.borderBottomRightRadius = 20;
            lockIcon.Add(new TabletIconElement(TabletIconKind.Cloud, 36, Color.white) { pickingMode = PickingMode.Ignore });
            lockCard.Add(lockIcon);
            lockCard.Add(new Label("登录小米账号后才能使用云空间") { style = { fontSize = 20, color = InkPrimary, unityFontDefinition = FdBold() } });
            lockCard.Add(new Label("云同步、备份与云盘都需要账号") { style = { fontSize = 15, color = InkSecondary, unityFontDefinition = Fd(), marginTop = 8 } });
            var loginBtn = new Label("登录小米账号") { pickingMode = PickingMode.Position, name = "settings-cloud-login-btn" };
            loginBtn.style.fontSize = 17; loginBtn.style.color = Color.white; loginBtn.style.unityFontDefinition = Fd();
            loginBtn.style.backgroundColor = new Color(0.20f, 0.50f, 0.98f, 1f);
            loginBtn.style.paddingLeft = 32; loginBtn.style.paddingRight = 32; loginBtn.style.paddingTop = 12; loginBtn.style.paddingBottom = 12;
            loginBtn.style.borderTopLeftRadius = loginBtn.style.borderTopRightRadius = 24;
            loginBtn.style.borderBottomLeftRadius = loginBtn.style.borderBottomRightRadius = 24;
            loginBtn.style.marginTop = 22;
            loginBtn.RegisterCallback<ClickEvent>(_ => OpenAccountEntry());
            lockCard.Add(loginBtn);
            rightPane.Add(lockCard);
            return;
        }
        var cloudSub = new Label(AccountService.IsRegistered
            ? AccountService.DisplayName + "  " + AccountService.MaskedPhone(AccountService.Profile)
            : "未登录小米账号") { name = "settings-cloud-subtitle" };
        cloudSub.style.fontSize = 14; cloudSub.style.color = InkSecondary; cloudSub.style.unityFontDefinition = Fd();
        cloudSub.style.alignSelf = Align.Center; cloudSub.style.marginTop = 0; cloudSub.style.marginBottom = 12;
        rightPane.Add(cloudSub);
        var blue = new Color(0.20f, 0.50f, 0.98f, 1f);
        var trackCol = new Color(0.93f, 0.93f, 0.95f, 1f);
        long usable = TabletStorageLedger.CloudUsableBytes;
        long used = TabletStorageLedger.CloudUsedBytes;

        // 容量卡：大数字 + 进度条 + 国家征用说明
        var hero = BuildWhiteCard();
        hero.name = "settings-cloud-hero";
        hero.style.paddingTop = 20; hero.style.paddingBottom = 18;
        var top = new VisualElement();
        top.style.flexDirection = FlexDirection.Row; top.style.justifyContent = Justify.SpaceBetween; top.style.alignItems = Align.FlexEnd;
        var figure = new VisualElement();
        figure.style.flexDirection = FlexDirection.Row; figure.style.alignItems = Align.FlexEnd;
        var usedLbl = new Label(TabletStorageLedger.Format(used)) { name = "settings-cloud-used", style = { fontSize = 38, color = InkPrimary, unityFontDefinition = FdBold() } };
        figure.Add(usedLbl);
        figure.Add(new Label("| " + TabletStorageLedger.Format(usable)) { style = { fontSize = 18, color = InkSecondary, unityFontDefinition = Fd(), marginLeft = 8, marginBottom = 6 } });
        top.Add(figure);
        var details = new Label("详情 ›") { pickingMode = PickingMode.Position };
        details.style.fontSize = 15; details.style.color = InkTertiary; details.style.unityFontDefinition = Fd(); details.style.marginBottom = 6;
        details.RegisterCallback<ClickEvent>(_ => ShowToast("标称 " + TabletStorageLedger.Format(TabletStorageLedger.CloudNominalBytes) + "，其中 " + TabletStorageLedger.Format(TabletStorageLedger.CloudRequisitionedBytes) + " 已被国家征用"));
        top.Add(details);
        hero.Add(top);

        var bar = new VisualElement { name = "settings-cloud-bar" };
        bar.style.height = 10; bar.style.marginTop = 14; bar.style.overflow = Overflow.Hidden;
        bar.style.backgroundColor = trackCol;
        bar.style.borderTopLeftRadius = bar.style.borderTopRightRadius = 5;
        bar.style.borderBottomLeftRadius = bar.style.borderBottomRightRadius = 5;
        var barFill = new VisualElement { pickingMode = PickingMode.Ignore };
        barFill.style.height = 10; barFill.style.backgroundColor = blue;
        barFill.style.width = Length.Percent(Mathf.Clamp01((float)used / usable) * 100f);
        bar.Add(barFill);
        hero.Add(bar);
        bool firstUpload = TabletStorageLedger.CloudLastSyncUnix <= 0;   // 这个账号还没上传过任何数据

        var member = new VisualElement();
        member.style.flexDirection = FlexDirection.Row; member.style.alignItems = Align.Center;
        member.style.marginTop = 18;
        var badge = BuildIconBadge("🛡", new Color(0.46f, 0.52f, 0.64f, 1f), 44);
        badge.style.marginRight = 14;
        member.Add(badge);
        var mText = new VisualElement { style = { flexGrow = 1, flexShrink = 1 } };
        mText.Add(new Label("国家征用 " + TabletStorageLedger.Format(TabletStorageLedger.CloudRequisitionedBytes)) { style = { fontSize = 17, color = InkPrimary, unityFontDefinition = Fd() } });
        mText.Add(new Label("标称 " + TabletStorageLedger.Format(TabletStorageLedger.CloudNominalBytes) + " · 其余已划入国家储备云") { style = { fontSize = 14, color = InkSecondary, unityFontDefinition = Fd(), marginTop = 2, whiteSpace = WhiteSpace.Normal } });
        member.Add(mText);
        var apply = new Label("申请扩容") { pickingMode = PickingMode.Position, name = "settings-cloud-apply" };
        apply.style.fontSize = 16; apply.style.color = blue; apply.style.unityFontDefinition = Fd();
        apply.style.paddingLeft = 20; apply.style.paddingRight = 20; apply.style.paddingTop = 10; apply.style.paddingBottom = 10;
        apply.style.backgroundColor = new Color(0.90f, 0.94f, 1f, 1f);
        apply.style.borderTopLeftRadius = apply.style.borderTopRightRadius = 20;
        apply.style.borderBottomLeftRadius = apply.style.borderBottomRightRadius = 20;
        apply.style.flexShrink = 0;
        apply.RegisterCallback<ClickEvent>(_ => ShowToast(TabletStorageLedger.RegisterReservedAttempt()));
        member.Add(apply);
        hero.Add(member);
        rightPane.Add(hero);

        // 云同步：各项数量为设定值
        var syncCard = BuildWhiteCard();
        syncCard.name = "settings-cloud-sync-card";
        var syncHead = new VisualElement();
        syncHead.style.flexDirection = FlexDirection.Row; syncHead.style.justifyContent = Justify.SpaceBetween; syncHead.style.alignItems = Align.Center;
        syncHead.style.paddingTop = 14; syncHead.style.paddingBottom = 14; syncHead.style.paddingLeft = 8; syncHead.style.paddingRight = 8;
        syncHead.style.borderBottomWidth = 1; syncHead.style.borderBottomColor = InsetDivider;
        syncHead.Add(new Label("云同步") { style = { fontSize = 19, color = InkPrimary, unityFontDefinition = Fd() } });
        var syncMore = new Label("更多 ›") { pickingMode = PickingMode.Position };
        syncMore.style.fontSize = 15; syncMore.style.color = InkTertiary; syncMore.style.unityFontDefinition = Fd();
        syncMore.RegisterCallback<ClickEvent>(_ => ShowToast("更多可同步项目需经所属单位审批开通"));
        syncHead.Add(syncMore);
        syncCard.Add(syncHead);
        (string app, string count, string icon, Color col)[] syncItems =
        {
            ("相册", "1286 项", null, blue),
            ("笔记", "39 项", null, blue),
            ("密码", "34 项", "🔒", new Color(0.12f, 0.12f, 0.14f, 1f)),
            ("联系人", "16 项", "👤", blue),
        };
        foreach (var it in syncItems)
        {
            var item = it;
            var row = BuildInfoRow(item.app, "已开启", firstUpload ? "等待首次同步" : item.count, () => ShowToast(item.app + "云同步已开启 · 数据先存入本地量子缓存"));
            VisualElement ic = item.icon == null ? GameMainUI.BuildTabletAppIconTile(item.app, 44f) : null;
            if (ic == null) ic = BuildIconBadge(item.icon ?? "☁", item.col, 44);
            ic.style.flexShrink = 0; ic.style.width = 40; ic.style.height = 40; ic.style.marginRight = 16;
            ic.pickingMode = PickingMode.Ignore;
            row.Insert(0, ic);
            syncCard.Add(row);
        }
        rightPane.Add(syncCard);

        // 备份：点击后在行内显示进度条与百分比
        var backupCard = BuildWhiteCard();
        backupCard.name = "settings-cloud-backup-card";
        var backupRow = BuildInfoRow("备份", TabletStorageLedger.CloudLastSyncText(), "本设备 · Xiaomi Pad 36 Ultra", null);
        backupRow.name = "settings-cloud-backup-row";
        backupRow.pickingMode = PickingMode.Position;
        AddRowFeedback(backupRow);
        var backupLeft = backupRow[0];
        var backupSub = (Label)backupLeft[1];
        var backupValue = (Label)((VisualElement)backupRow[1])[0];
        var progTrack = new VisualElement { pickingMode = PickingMode.Ignore };
        progTrack.style.height = 5; progTrack.style.marginTop = 10; progTrack.style.display = DisplayStyle.None;
        progTrack.style.backgroundColor = trackCol;
        progTrack.style.borderTopLeftRadius = progTrack.style.borderTopRightRadius = 3;
        progTrack.style.borderBottomLeftRadius = progTrack.style.borderBottomRightRadius = 3;
        var progFill = new VisualElement { pickingMode = PickingMode.Ignore };
        progFill.style.height = 5; progFill.style.width = Length.Percent(0); progFill.style.backgroundColor = blue;
        progFill.style.borderTopLeftRadius = progFill.style.borderTopRightRadius = 3;
        progFill.style.borderBottomLeftRadius = progFill.style.borderBottomRightRadius = 3;
        progTrack.Add(progFill);
        backupLeft.Add(progTrack);
        var arrow = new Label("›");
        arrow.style.fontSize = 20; arrow.style.color = InkTertiary; arrow.style.unityFontDefinition = Fd(); arrow.style.marginLeft = 6;
        ((VisualElement)backupRow[1]).Add(arrow);
        bool syncing = false;
        IVisualElementScheduledItem syncItem = null;
        Action<bool> startSync = first =>
        {
            if (syncing) return;
            syncing = true;
            float t = 0f;
            float duration = first ? 5.5f : 3.2f;   // 首次上传要传整套数据，更久
            long fullUsed = TabletStorageLedger.CloudFullUsedBytes;
            progTrack.style.display = DisplayStyle.Flex;
            backupSub.text = first ? "首次同步：正在准备上传…" : "正在加密并上传…";
            syncItem = backupRow.schedule.Execute(() =>
            {
                t += 0.033f;
                float k = Mathf.Clamp01(t / duration);
                int pct = Mathf.RoundToInt(k * 100f);
                progFill.style.width = Length.Percent(k * 100f);
                backupValue.text = pct + "%";
                if (first)
                {
                    long cur = (long)(fullUsed * (double)k);
                    usedLbl.text = TabletStorageLedger.Format(cur);
                    barFill.style.width = Length.Percent(Mathf.Clamp01((float)cur / usable) * 100f);
                    backupSub.text = k < 0.15f ? "首次同步：正在建立加密通道…" : (k < 0.85f ? "首次同步：正在上传本机数据…" : "等待卫星过境确认…");
                }
                else backupSub.text = k < 0.85f ? "正在加密并上传…" : "等待卫星过境确认…";
                if (k >= 1f)
                {
                    syncItem.Pause();
                    syncing = false;
                    TabletStorageLedger.MarkCloudSynced();
                    progTrack.style.display = DisplayStyle.None;
                    backupSub.text = "本设备 · Xiaomi Pad 36 Ultra";
                    backupValue.text = TabletStorageLedger.CloudLastSyncText();
                    ShowToast(first ? "首次同步完成 · 数据已加密存入本地量子缓存，待卫星过境后同步" : "备份完成 · 数据已加密存入本地量子缓存，待卫星过境后同步");
                    if (first && currentSettingsCategory == 97) RefreshCurrentPage();   // 刷新同步项的数量
                }
            }).Every(33);
        };
        backupRow.RegisterCallback<ClickEvent>(_ => startSync(false));
        // 登录后第一次进来：自动开始首次上传
        if (firstUpload) backupRow.schedule.Execute(() => startSync(true)).StartingIn(600);
        backupCard.Add(backupRow);
        rightPane.Add(backupCard);

        var driveCard = BuildWhiteCard();
        driveCard.Add(BuildWhiteRow("云盘", null, () => ShowToast("云盘内暂无文件 · 上传需经所属单位保卫部门审批")));
        rightPane.Add(driveCard);

        var benefitCard = BuildWhiteCard();
        var benefitHead = new VisualElement();
        benefitHead.style.flexDirection = FlexDirection.Row; benefitHead.style.justifyContent = Justify.SpaceBetween; benefitHead.style.alignItems = Align.Center;
        benefitHead.style.paddingTop = 14; benefitHead.style.paddingBottom = 14; benefitHead.style.paddingLeft = 8; benefitHead.style.paddingRight = 8;
        benefitHead.style.borderBottomWidth = 1; benefitHead.style.borderBottomColor = InsetDivider;
        benefitHead.Add(new Label("云服务权益") { style = { fontSize = 19, color = InkPrimary, unityFontDefinition = Fd() } });
        benefitHead.Add(new Label("›") { style = { fontSize = 20, color = InkTertiary, unityFontDefinition = Fd() } });
        benefitCard.Add(benefitHead);
        benefitCard.Add(WithLeadingIcon(BuildWhiteRow("扩展云存储空间", "申请更大的云空间，保存更多照片与视频", () => ShowToast(TabletStorageLedger.RegisterReservedAttempt())), TabletIconKind.Cloud, blue));
        benefitCard.Add(WithLeadingIcon(BuildWhiteRow("找回已删除内容", "可找回 30 天内删除的项目", () => ShowToast("回收站为空")), TabletIconKind.Trash, blue));
        benefitCard.Add(WithLeadingIcon(BuildToggleRow("自动备份", "每天夜间自动备份本设备", PlayerPrefs.GetInt(CloudAutoBackupKey, 1) == 1, v =>
        {
            PlayerPrefs.SetInt(CloudAutoBackupKey, v ? 1 : 0);
            PlayerPrefs.Save();
            ShowToast(v ? "已开启自动备份" : "已关闭自动备份");
        }), TabletIconKind.Save, blue));
        benefitCard.Add(WithLeadingIcon(BuildWhiteRow("与家人共享云空间", "最多 8 位家庭成员", () => ShowToast("家庭成员须经人事部门核验后方可共享")), TabletIconKind.Heart, blue));
        rightPane.Add(benefitCard);

        var protectCard = BuildWhiteCard();
        protectCard.Add(BuildInfoRow("高级数据保护", "已开启", null, () => ShowToast("端到端加密已开启 · 密钥由保卫部门代为保管")));
        rightPane.Add(protectCard);
    }

    private static void RenderStorageSettings(ScrollView rightPane, VisualElement win)
    {
        AddCenterHeader(rightPane, "存储");
        long used = TabletStorageLedger.UserUsedBytes;
        long part = TabletStorageLedger.UserPartitionBytes;
        long game = TabletStorageLedger.GameDataBytes;
        long sysOther = 0, apps = 0;
        var entries = TabletStorageLedger.UserEntries();
        foreach (var e in entries)
        {
            if (e.isApp) apps += e.bytes;
            else if (!e.isReal) sysOther += e.bytes;
        }
        var blue = new Color(0.20f, 0.50f, 0.98f, 1f);
        var green = new Color(0.20f, 0.78f, 0.40f, 1f);
        var grey = new Color(0.62f, 0.64f, 0.70f, 1f);
        var red = new Color(0.78f, 0.22f, 0.22f, 1f);

        // 本游戏的真实占用在后台统计，没算完时先提示，算完自动刷新本页
        if (!TabletStorageLedger.RealReady)
        {
            var wait = BuildWhiteCard();
            wait.name = "settings-storage-measuring";
            wait.Add(new Label("正在统计本游戏占用的全部空间…") { style = { fontSize = 15, color = InkSecondary, unityFontDefinition = Fd(), paddingTop = 8, paddingBottom = 8 } });
            rightPane.Add(wait);
            IVisualElementScheduledItem poll = null;
            poll = rightPane.schedule.Execute(() =>
            {
                if (!TabletStorageLedger.RealReady) return;
                poll.Pause();
                if (currentSettingsCategory == 98) RefreshCurrentPage();
            }).Every(400);
        }

        var hero = BuildWhiteCard();
        hero.name = "settings-storage-hero";
        hero.style.paddingTop = 22; hero.style.paddingBottom = 20;
        var figure = new VisualElement();
        figure.style.flexDirection = FlexDirection.Row; figure.style.alignItems = Align.FlexEnd;
        figure.Add(new Label(TabletStorageLedger.Format(used)) { style = { fontSize = 40, color = InkPrimary, unityFontDefinition = FdBold() } });
        figure.Add(new Label("/ " + TabletStorageLedger.Format(part) + " 用户可用分区") { style = { fontSize = 16, color = InkSecondary, unityFontDefinition = Fd(), marginLeft = 10, marginBottom = 7 } });
        hero.Add(figure);
        var userBar = BuildSegmentBar(16, new[]
        {
            (apps, blue), (game, green), (sysOther, grey), (Math.Max(0L, part - used), new Color(0.93f, 0.93f, 0.95f, 1f)),
        });
        userBar.style.marginTop = 14;
        hero.Add(userBar);
        hero.Add(BuildLegendRow(new[] { (blue, "应用"), (green, "本游戏占用（实测）"), (grey, "系统与其他") }));

        var divider = new VisualElement();
        divider.style.height = 1; divider.style.backgroundColor = InsetDivider;
        divider.style.marginTop = 20; divider.style.marginBottom = 18;
        hero.Add(divider);

        hero.Add(new Label("标称总容量 " + TabletStorageLedger.Format(TabletStorageLedger.NominalTotalBytes)) { style = { fontSize = 16, color = InkPrimary, unityFontDefinition = Fd() } });
        var nominalBar = BuildSegmentBar(10, new[]
        {
            (TabletStorageLedger.ReservedBytes, red), (TabletStorageLedger.SystemBytes, grey), (part, blue),
        });
        nominalBar.style.marginTop = 10;
        hero.Add(nominalBar);
        hero.Add(BuildLegendRow(new[] { (red, "国家战略储备（锁定）"), (grey, "系统分区"), (blue, "用户可用分区") }));
        rightPane.Add(hero);

        AddSectionHeader(rightPane, "分区");
        var partCard = BuildWhiteCard();
        partCard.Add(BuildInfoRow("标称总容量", TabletStorageLedger.Format(TabletStorageLedger.NominalTotalBytes), "2053 年出厂 · 量子闪存阵列"));
        partCard.Add(BuildInfoRow("国家战略储备", TabletStorageLedger.Format(TabletStorageLedger.ReservedBytes),
            "归最高领袖所有 · 已锁定，不可读写", () => ShowToast(TabletStorageLedger.RegisterReservedAttempt())));
        partCard.Add(BuildInfoRow("系统分区", TabletStorageLedger.Format(TabletStorageLedger.SystemBytes), "只读"));
        partCard.Add(BuildInfoRow("用户可用分区", TabletStorageLedger.Format(part),
            "已用 " + TabletStorageLedger.Format(used) + " · 剩余 " + TabletStorageLedger.Format(TabletStorageLedger.UserFreeBytes)));
        rightPane.Add(partCard);

        AddSectionHeader(rightPane, "占用明细");
        var listCard = BuildWhiteCard();
        listCard.name = "settings-storage-list";
        long maxBytes = 1;
        foreach (var e in entries) if (e.bytes > maxBytes) maxBytes = e.bytes;
        foreach (var e in entries)
        {
            var entry = e;
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            row.style.paddingTop = 12; row.style.paddingBottom = 12; row.style.paddingLeft = 8; row.style.paddingRight = 8;
            row.style.borderBottomWidth = 1; row.style.borderBottomColor = InsetDivider;
            row.style.borderTopLeftRadius = row.style.borderTopRightRadius = 10;
            row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 10;
            row.pickingMode = PickingMode.Position;
            AddRowFeedback(row);
            row.RegisterCallback<ClickEvent>(_ => ShowToast(entry.name + " 占用 " + TabletStorageLedger.Format(entry.bytes) + (entry.isReal ? "（实测）" : "")));

            VisualElement icon = entry.isApp ? GameMainUI.BuildTabletAppIconTile(entry.name, 44f) : null;
            if (icon == null)
                icon = BuildIconBadge(entry.isReal ? "💾" : "▦", entry.isReal ? green : grey, 44);
            icon.style.flexShrink = 0; icon.style.width = 44; icon.style.height = 44;
            icon.style.marginRight = 16; icon.pickingMode = PickingMode.Ignore;
            row.Add(icon);

            var mid = new VisualElement { pickingMode = PickingMode.Ignore };
            mid.style.flexGrow = 1; mid.style.flexShrink = 1;
            var nm = new Label(entry.name + (entry.isReal ? "（实测）" : "")) { pickingMode = PickingMode.Ignore };
            nm.style.fontSize = 18; nm.style.color = InkPrimary; nm.style.unityFontDefinition = Fd();
            mid.Add(nm);
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.style.height = 6; track.style.marginTop = 8;
            track.style.backgroundColor = new Color(0.93f, 0.93f, 0.95f, 1f);
            track.style.borderTopLeftRadius = track.style.borderTopRightRadius = 3;
            track.style.borderBottomLeftRadius = track.style.borderBottomRightRadius = 3;
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.style.height = 6; fill.style.minWidth = 3;
            fill.style.width = Length.Percent(Mathf.Clamp(entry.bytes * 100f / maxBytes, 0.5f, 100f));
            fill.style.backgroundColor = entry.isReal ? green : blue;
            fill.style.borderTopLeftRadius = fill.style.borderTopRightRadius = 3;
            fill.style.borderBottomLeftRadius = fill.style.borderBottomRightRadius = 3;
            track.Add(fill);
            mid.Add(track);
            row.Add(mid);

            var size = new Label(TabletStorageLedger.Format(entry.bytes)) { pickingMode = PickingMode.Ignore };
            size.style.fontSize = 15; size.style.color = InkTertiary; size.style.unityFontDefinition = Fd();
            size.style.marginLeft = 16; size.style.minWidth = 80; size.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(size);
            listCard.Add(row);
        }
        rightPane.Add(listCard);

        AddSectionHeader(rightPane, "空间申请");
        var askCard = BuildWhiteCard();
        int tries = TabletStorageLedger.ReservedAttempts;
        askCard.Add(BuildInfoRow("申请使用国家储备空间", tries > 0 ? "已申请 " + tries + " 次" : "", "无需多言", () =>
        {
            ShowToast(TabletStorageLedger.RegisterReservedAttempt());
            RefreshCurrentPage();
        }));
        rightPane.Add(askCard);
    }

    // 17 隐私与安全：本地存储边界与沙箱权限
    private static void RenderPrivacySettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-privacy-page" };
        AddCenterHeader(page, "隐私与安全");

        AddSectionHeader(page, "终端防护");
        var storageCard = BuildWhiteCard();
        storageCard.name = "settings-privacy-storage-card";
        storageCard.Add(BuildInfoRow("终端防御", "白头疫苗", "安全服务由白头疫苗统一管理，可在“白头疫苗”应用中查看防护状态"));
        storageCard.Add(BuildInfoRow("外部网络", "受限", "车载内网沙箱默认不连接公共网络；音乐页的账号与在线曲库只在你操作时联网"));
        storageCard.Add(BuildInfoRow("应用权限", "沙箱隔离", "22 个应用运行在系统沙箱内"));
        page.Add(storageCard);

        AddSectionHeader(page, "本机数据");
        var sandboxCard = BuildWhiteCard();
        sandboxCard.name = "settings-privacy-sandbox-card";
        sandboxCard.Add(BuildInfoRow("存档与设置", "本机存储", "游戏存档、系统设置保存在本机"));
        sandboxCard.Add(BuildInfoRow("音乐库", "本机文件", "曲库与歌词保存在本机文件；登录在线账号后，歌单会按你的操作与账号同步"));
        sandboxCard.Add(BuildInfoRow("备份与恢复", "本地快照", "存档备份只保存在本机", () => ShowBackupRestorePage()));
        page.Add(sandboxCard);

        rightPane.Add(page);
    }

    // 18 应用：系统应用管控入口
    private static void RenderAppsSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "应用");

        // 搜索应用：药丸搜索框，实时过滤下面的真实应用列表
        var searchPill = new VisualElement { name = "settings-apps-search" };
        searchPill.style.height = 52;
        searchPill.style.backgroundColor = new Color(0.90f, 0.90f, 0.92f, 1f);
        searchPill.style.borderTopLeftRadius = searchPill.style.borderTopRightRadius = 26;
        searchPill.style.borderBottomLeftRadius = searchPill.style.borderBottomRightRadius = 26;
        searchPill.style.flexDirection = FlexDirection.Row; searchPill.style.alignItems = Align.Center;
        searchPill.style.paddingLeft = 18; searchPill.style.paddingRight = 18;
        searchPill.style.marginBottom = 14;
        searchPill.Add(new TabletIconElement(TabletIconKind.Search, 22f, InkTertiary) { style = { marginRight = 10 } });
        var appSearch = new TextField();
        appSearch.style.flexGrow = 1;
        appSearch.style.borderTopWidth = 0; appSearch.style.borderBottomWidth = 0;
        appSearch.style.borderLeftWidth = 0; appSearch.style.borderRightWidth = 0;
        appSearch.style.backgroundColor = Color.clear;
        appSearch.style.fontSize = 18; appSearch.style.color = InkPrimary; appSearch.style.unityFontDefinition = Fd();
        var appSearchInner = appSearch.Q(className: "unity-base-text-field__input");
        if (appSearchInner != null)
        {
            appSearchInner.style.backgroundColor = Color.clear;
            appSearchInner.style.borderTopWidth = 0; appSearchInner.style.borderBottomWidth = 0;
            appSearchInner.style.borderLeftWidth = 0; appSearchInner.style.borderRightWidth = 0;
            appSearchInner.style.unityFontDefinition = Fd();
        }
        appSearch.textEdition.placeholder = "搜索应用";
        appSearch.textEdition.hidePlaceholderOnFocus = true;
        searchPill.Add(appSearch);
        rightPane.Add(searchPill);

        // 顶部三个快捷入口方块（对照 HyperOS：应用锁 / 应用双开 / 自启动）
        var tiles = BuildWhiteCard();
        tiles.name = "settings-apps-tiles";
        tiles.style.flexDirection = FlexDirection.Row;
        tiles.style.paddingTop = 20; tiles.style.paddingBottom = 18;
        tiles.Add(BuildAppTile("▦", new Color(0.20f, 0.46f, 0.96f, 1f), "应用管控", ShowAppManagementPage));
        tiles.Add(BuildAppTile("🧩", new Color(0.99f, 0.66f, 0.12f, 1f), "默认应用", () => ShowToast("默认浏览器：光明内网浏览器 · 默认播放器：随身听")));
        tiles.Add(BuildAppTile("🛡", new Color(0.19f, 0.72f, 0.42f, 1f), "特殊权限", () => ShowToast("全部 22 个应用沙箱隔离权限运转正常")));
        rightPane.Add(tiles);

        // 平板桌面上真实存在的全部应用（与桌面同名同图标）
        var listHeader = new Label("按桌面顺序排列");
        listHeader.style.fontSize = 13; listHeader.style.color = SectionHeaderColor;
        listHeader.style.unityFontDefinition = FdBold();
        listHeader.style.marginBottom = 8; listHeader.style.marginLeft = 6;
        rightPane.Add(listHeader);
        var listCard = BuildWhiteCard();
        listCard.name = "settings-apps-list";
        rightPane.Add(listCard);

        Action fillList = () =>
        {
            listCard.Clear();
            string q = (appSearch.value ?? "").Trim();
            var names = GameMainUI.TabletAppNames;
            int shown = 0;
            for (int i = 0; i < names.Count; i++)
            {
                string appName = names[i];
                if (q.Length > 0 && appName.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                shown++;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
                row.style.paddingTop = 12; row.style.paddingBottom = 12; row.style.paddingLeft = 8; row.style.paddingRight = 8;
                row.style.borderBottomWidth = 1; row.style.borderBottomColor = InsetDivider;
                row.style.borderTopLeftRadius = row.style.borderTopRightRadius = 10;
                row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 10;
                row.pickingMode = PickingMode.Position;
                AddRowFeedback(row);
                row.RegisterCallback<ClickEvent>(_ => ShowToast(appName + " · 沙箱隔离运行，权限正常"));
                var icon = GameMainUI.BuildTabletAppIconTile(appName, 48f) ?? new VisualElement();
                icon.style.flexShrink = 0; icon.style.width = 48; icon.style.height = 48;
                icon.pickingMode = PickingMode.Ignore;
                icon.style.marginRight = 16;
                row.Add(icon);
                var nm = new Label(appName) { pickingMode = PickingMode.Ignore };
                nm.style.flexGrow = 1; nm.style.fontSize = 18; nm.style.color = InkPrimary; nm.style.unityFontDefinition = Fd();
                row.Add(nm);
                var ar = new Label("›") { pickingMode = PickingMode.Ignore };
                ar.style.fontSize = 20; ar.style.color = InkTertiary; ar.style.unityFontDefinition = Fd();
                row.Add(ar);
                listCard.Add(row);
            }
            if (shown == 0)
            {
                var none = new Label("未找到相关应用");
                none.style.fontSize = 16; none.style.color = InkTertiary; none.style.unityFontDefinition = Fd();
                none.style.alignSelf = Align.Center; none.style.paddingTop = 28; none.style.paddingBottom = 28;
                listCard.Add(none);
            }
        };
        appSearch.RegisterValueChangedCallback(_ => fillList());
        fillList();

        AddSectionHeader(rightPane, "应用与组件管理");
        var card = BuildWhiteCard();
        card.Add(BuildWhiteRow("进入应用管控", "已禁用的 4 个系统组件与恢复彩蛋", ShowAppManagementPage));
        card.Add(BuildWhiteRow("默认应用设置", "默认浏览器：光明内网浏览器 · 默认播放器：随身听", null, false));
        card.Add(BuildWhiteRow("特殊权限设置", "全部 22 个应用沙箱隔离权限", () => ShowToast("权限运转正常")));
        rightPane.Add(card);
    }

    private static VisualElement BuildAppTile(string iconKey, Color color, string label, System.Action onClick)
    {
        var tile = new VisualElement();
        tile.style.flexGrow = 1; tile.style.flexBasis = 0;
        tile.style.alignItems = Align.Center;
        tile.style.paddingTop = 6; tile.style.paddingBottom = 6;
        tile.style.borderTopLeftRadius = tile.style.borderTopRightRadius = 14;
        tile.style.borderBottomLeftRadius = tile.style.borderBottomRightRadius = 14;
        tile.pickingMode = PickingMode.Position;
        AddRowFeedback(tile);
        tile.RegisterCallback<ClickEvent>(_ => onClick());
        var badge = BuildIconBadge(iconKey, color, 52);
        badge.style.marginBottom = 10;
        tile.Add(badge);
        var l = new Label(label) { pickingMode = PickingMode.Ignore };
        l.style.fontSize = 16; l.style.color = InkPrimary; l.style.unityFontDefinition = Fd();
        tile.Add(l);
        return tile;
    }

    // 19 电池：真实系统电源状态与机车供电说明
    // 电量大色块：绿色填充宽度等于真实电量；平台没有读数时显示灰色并如实说明
    private static VisualElement BuildBatteryHero(float level, BatteryStatus status)
    {
        bool known = level >= 0f;
        var hero = new VisualElement { name = "settings-battery-hero" };
        hero.style.height = 200;
        hero.style.marginBottom = 16;
        hero.style.overflow = Overflow.Hidden;
        hero.style.borderTopLeftRadius = hero.style.borderTopRightRadius = 24;
        hero.style.borderBottomLeftRadius = hero.style.borderBottomRightRadius = 24;
        hero.style.backgroundColor = known ? new Color(0.76f, 0.93f, 0.82f, 1f) : new Color(0.89f, 0.89f, 0.91f, 1f);
        hero.style.justifyContent = Justify.Center;
        hero.style.paddingLeft = 36; hero.style.paddingRight = 36;

        if (known)
        {
            var fill = new VisualElement { pickingMode = PickingMode.Ignore, name = "settings-battery-hero-fill" };
            fill.style.position = Position.Absolute;
            fill.style.left = 0; fill.style.top = 0; fill.style.bottom = 0;
            fill.style.width = Length.Percent(Mathf.Clamp01(level) * 100f);
            fill.style.backgroundColor = new Color(0.20f, 0.82f, 0.40f, 1f);
            hero.Add(fill);
        }

        var valueRow = new VisualElement { pickingMode = PickingMode.Ignore };
        valueRow.style.flexDirection = FlexDirection.Row;
        valueRow.style.alignItems = Align.FlexEnd;
        var value = new Label(known ? Mathf.RoundToInt(level * 100f).ToString() : "--");
        // 数字压在绿色填充上用白字；电量低、数字落到浅色底上时改深绿，保证对比度
        var heroInk = !known ? InkSecondary : level >= 0.40f ? Color.white : new Color(0.08f, 0.32f, 0.16f, 1f);
        value.style.fontSize = 76; value.style.color = heroInk;
        value.style.unityFontDefinition = FdBold();
        value.pickingMode = PickingMode.Ignore;
        valueRow.Add(value);
        var unit = new Label("%");
        unit.style.fontSize = 26; unit.style.color = heroInk;
        unit.style.unityFontDefinition = FdBold();
        unit.style.marginLeft = 6; unit.style.marginBottom = 12;
        unit.pickingMode = PickingMode.Ignore;
        valueRow.Add(unit);
        hero.Add(valueRow);

        string caption = !known ? "平台未提供电量读数"
            : status == BatteryStatus.Charging ? "正在充电"
            : status == BatteryStatus.Full ? "电池已充满"
            : status == BatteryStatus.NotCharging ? "未在充电"
            : "电池供电放电中";
        var sub = new Label(caption);
        sub.style.fontSize = 18; sub.style.color = heroInk;
        sub.style.unityFontDefinition = Fd();
        sub.pickingMode = PickingMode.Ignore;
        hero.Add(sub);
        return hero;
    }

    // 电池曲线卡：数据是游戏运行期间每分钟读到的本机真实电量（见 TabletBatteryHistory），没运行的时段不画
    private static VisualElement BuildBatteryChartCard()
    {
        var card = BuildWhiteCard();
        card.name = "settings-battery-chart-card";
        card.style.paddingTop = 20; card.style.paddingBottom = 16;

        var head = new VisualElement();
        head.style.flexDirection = FlexDirection.Row;
        head.style.justifyContent = Justify.SpaceBetween;
        head.style.alignItems = Align.FlexEnd;
        card.Add(head);

        var left = new VisualElement();
        head.Add(left);
        var cap = new Label("本次运行");
        cap.style.fontSize = 15; cap.style.color = InkTertiary; cap.style.unityFontDefinition = Fd();
        left.Add(cap);
        int totalMin = Mathf.Max(0, Mathf.FloorToInt(Time.realtimeSinceStartup / 60f));
        var figure = new VisualElement();
        figure.style.flexDirection = FlexDirection.Row; figure.style.alignItems = Align.FlexEnd;
        figure.Add(new Label((totalMin / 60).ToString()) { style = { fontSize = 40, color = InkPrimary, unityFontDefinition = FdBold() } });
        figure.Add(new Label("小时") { style = { fontSize = 16, color = InkPrimary, unityFontDefinition = Fd(), marginLeft = 4, marginRight = 8, marginBottom = 7 } });
        figure.Add(new Label((totalMin % 60).ToString()) { style = { fontSize = 40, color = InkPrimary, unityFontDefinition = FdBold() } });
        figure.Add(new Label("分钟") { style = { fontSize = 16, color = InkPrimary, unityFontDefinition = Fd(), marginLeft = 4, marginBottom = 7 } });
        left.Add(figure);

        var legend = new VisualElement();
        legend.style.flexDirection = FlexDirection.Row; legend.style.alignItems = Align.Center;
        legend.style.marginBottom = 10;
        head.Add(legend);
        var blue = new Color(0.20f, 0.50f, 0.98f, 1f);
        var green = new Color(0.20f, 0.78f, 0.40f, 1f);
        legend.Add(BuildLegendDot(blue, "电池使用"));
        legend.Add(BuildLegendDot(green, "充电"));

        var samples = TabletBatteryHistory.Samples;
        long now = TabletBatteryHistory.NowUnix();
        long first = samples.Count > 0 ? samples[0].unix : now;
        long window = (long)Mathf.Clamp(now - first, 3600f, 86400f);
        long tStart = now - window;

        const float plotH = 200f, rightMargin = 64f;
        var area = new VisualElement { name = "settings-battery-chart" };
        area.style.height = plotH + 34f;
        area.style.marginTop = 18;
        card.Add(area);

        var plot = new VisualElement { pickingMode = PickingMode.Ignore };
        plot.style.position = Position.Absolute;
        plot.style.left = 0; plot.style.right = rightMargin; plot.style.top = 0; plot.style.height = plotH;
        area.Add(plot);

        plot.generateVisualContent += mgc =>
        {
            var r = mgc.visualElement.contentRect;
            float w = r.width, h = r.height;
            if (w <= 1f || h <= 1f) return;
            var p = mgc.painter2D;
            p.lineWidth = 1f;
            p.strokeColor = new Color(0.86f, 0.86f, 0.89f, 1f);
            for (int g = 0; g <= 4; g++)
            {
                float y = h * (1f - g / 4f);
                p.BeginPath(); p.MoveTo(new Vector2(0, y)); p.LineTo(new Vector2(w, y)); p.Stroke();
            }
            if (samples.Count < 2) return;
            Func<TabletBatteryHistory.Sample, Vector2> pt = sm =>
                new Vector2(Mathf.Clamp01((sm.unix - tStart) / (float)window) * w, h * (1f - sm.percent / 100f));
            for (int i = 1; i < samples.Count; i++)
            {
                if (samples[i].unix < tStart) continue;
                var a = pt(samples[i - 1]); var b = pt(samples[i]);
                var c = samples[i].charging ? green : blue;
                p.fillColor = new Color(c.r, c.g, c.b, 0.22f);
                p.BeginPath();
                p.MoveTo(new Vector2(a.x, h)); p.LineTo(a); p.LineTo(b); p.LineTo(new Vector2(b.x, h));
                p.ClosePath(); p.Fill();
                p.strokeColor = c; p.lineWidth = 3f; p.lineJoin = LineJoin.Round; p.lineCap = LineCap.Round;
                p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.Stroke();
            }
        };

        for (int g = 0; g <= 4; g++)
        {
            var yl = new Label((g * 25) + "%") { pickingMode = PickingMode.Ignore };
            yl.style.position = Position.Absolute; yl.style.right = 0; yl.style.width = rightMargin - 8f;
            yl.style.top = plotH * (1f - g / 4f) - 9f;
            yl.style.fontSize = 13; yl.style.color = InkTertiary; yl.style.unityFontDefinition = Fd();
            yl.style.unityTextAlign = TextAnchor.MiddleRight;
            area.Add(yl);
        }
        // 横轴：起点 / 中点 / 现在
        for (int k = 0; k <= 2; k++)
        {
            long t = tStart + window * k / 2;
            string text = k == 2 ? "现在" : DateTimeOffset.FromUnixTimeSeconds(t).ToLocalTime().ToString("HH:mm");
            var xl = new Label(text) { pickingMode = PickingMode.Ignore };
            xl.style.position = Position.Absolute; xl.style.top = plotH + 10f;
            xl.style.fontSize = 13; xl.style.color = InkTertiary; xl.style.unityFontDefinition = Fd();
            if (k == 0) xl.style.left = 0;
            else if (k == 1) xl.style.left = Length.Percent(46);
            else xl.style.right = rightMargin - 4f;
            area.Add(xl);
        }

        if (samples.Count < 2)
        {
            var note = new Label(GameMainUI.GetDeviceBatteryLevel01() < 0f
                ? "平台未提供电量读数，无法绘制曲线"
                : "采样积累中：游戏运行时每分钟记录一次本机电量，稍后即可看到曲线") { pickingMode = PickingMode.Ignore };
            note.style.position = Position.Absolute; note.style.left = 0; note.style.right = rightMargin;
            note.style.top = plotH * 0.5f - 12f;
            note.style.unityTextAlign = TextAnchor.MiddleCenter;
            note.style.fontSize = 14; note.style.color = InkTertiary; note.style.unityFontDefinition = Fd();
            note.style.whiteSpace = WhiteSpace.Normal;
            area.Add(note);
        }
        return card;
    }

    private static VisualElement BuildLegendDot(Color color, string text)
    {
        var item = new VisualElement();
        item.style.flexDirection = FlexDirection.Row; item.style.alignItems = Align.Center;
        item.style.marginLeft = 22;
        var dot = new VisualElement();
        dot.style.width = 9; dot.style.height = 9; dot.style.marginRight = 7;
        dot.style.backgroundColor = color;
        dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = 5;
        dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 5;
        item.Add(dot);
        var l = new Label(text);
        l.style.fontSize = 17; l.style.color = InkSecondary; l.style.unityFontDefinition = Fd();
        item.Add(l);
        return item;
    }

    private static void RenderBatterySettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-battery-page" };
        AddCenterHeader(page, "电池");
        page.Add(BuildBatteryHero(GameMainUI.GetDeviceBatteryLevel01(), GameMainUI.GetDeviceBatteryStatus()));
        TabletBatteryHistory.Record();
        page.Add(BuildBatteryChartCard());

        AddSectionHeader(page, "供电状态");
        var statusCard = BuildWhiteCard();
        statusCard.name = "settings-battery-status-card";

        float battLevel = GameMainUI.GetDeviceBatteryLevel01();
        BatteryStatus battStatus = GameMainUI.GetDeviceBatteryStatus();
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
        statusCard.Add(BuildWhiteRow("数据来源", "读取本机真实电池：Windows 取系统电源状态，Android 取系统电池接口；不代表机车蓄电池或辅助电源遥测", null, false));
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
        statsCard.Add(BuildInfoRow("屏幕使用时长", "不统计", "此终端不记录使用时长和亮屏频次"));
        statsCard.Add(BuildInfoRow("应用使用限额", "未启用", "车载专业设备不设置个人防沉迷与使用时间限制"));
        page.Add(statsCard);

        AddSectionHeader(page, "值班");
        var dutyCard = BuildWhiteCard();
        dutyCard.name = "settings-digital-wellbeing-duty-card";
        dutyCard.Add(BuildInfoRow("行车安全数据", "驾驶台", "机车警惕与疲劳相关数据由驾驶台管理，不在这里读取"));
        dutyCard.Add(BuildInfoRow("显示与亮度", null, "调整画面与对话显示", () =>
        {
            NavigateToCategory(13, win);
        }));
        page.Add(dutyCard);

        rightPane.Add(page);
    }

    // 21 儿童模式：机车操纵环境与权限说明
    private static void RenderKidsModeSettings(ScrollView rightPane)
    {
        var page = new VisualElement { name = "settings-kids-mode-page" };
        AddCenterHeader(page, "儿童模式");

        AddSectionHeader(page, "儿童与监护");
        var statusCard = BuildWhiteCard();
        statusCard.name = "settings-kids-mode-status-card";
        statusCard.Add(BuildInfoRow("儿童空间", "未开启", "此终端没有独立的儿童账户或受限桌面"));
        statusCard.Add(BuildInfoRow("家长控制", "未开启", "没有监护、时长限制或内容过滤功能"));
        page.Add(statusCard);

        AddSectionHeader(page, "值班环境");
        var envCard = BuildWhiteCard();
        envCard.name = "settings-kids-mode-env-card";
        envCard.Add(BuildInfoRow("机车操纵环境", "专业设备", "平板随 0721 号机车使用，按成年乘务人员的值班环境设计"));
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
                RefreshCurrentPage();
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
        card.Add(BuildWhiteRow("系统面板高对比度", "在平板外观中统一调整", () => { NavigateToCategory(9, win); }));
        card.Add(BuildWhiteRow("读屏服务", "未启用", null, false));
        rightPane.Add(card);
    }

    // 23 更多设置：老设置「游戏」6条全量 + 备份 + 术语图鉴
    private static void RenderAdditionalSettings(ScrollView rightPane)
    {
        AddCenterHeader(rightPane, "更多设置");

        AddSectionHeader(rightPane, "特色功能");
        var gameCard = BuildWhiteCard();
        gameCard.Add(BuildToggleRow("自动播放模式", "开场后自动推进对话", PlayerPrefs.GetInt("AutoPlay", 0) == 1, v =>
        {
            PlayerPrefs.SetInt("AutoPlay", v ? 1 : 0);
            PlayerPrefs.Save();
        }));
        gameCard.Add(BuildToggleRow("跳过已读文本", "剧情系统尚未记录已读状态，暂不可用", PlayerPrefs.GetInt("SkipRead", 1) == 1, v =>
        {
            PlayerPrefs.SetInt("SkipRead", v ? 1 : 0);
            PlayerPrefs.Save();
        }, false));
        gameCard.Add(BuildToggleRow("点击对话框推进", "点击对话框区域触发下一句", PlayerPrefs.GetInt("ClickAdvance", 1) == 1, v =>
        {
            PlayerPrefs.SetInt("ClickAdvance", v ? 1 : 0);
            PlayerPrefs.Save();
        }));
        gameCard.Add(BuildToggleRow("确认对话框", "剧情内的确认弹窗目前固定显示，暂不可关闭", PlayerPrefs.GetInt("ConfirmDialog", 1) == 1, v =>
        {
            PlayerPrefs.SetInt("ConfirmDialog", v ? 1 : 0);
            PlayerPrefs.Save();
        }, false));
        gameCard.Add(BuildSliderRow("自动播放间隔", 1, 10, PlayerPrefs.GetInt("AutoInterval", 2), v =>
        {
            PlayerPrefs.SetInt("AutoInterval", v);
            PlayerPrefs.Save();
        }, " 秒"));
        rightPane.Add(gameCard);

        AddSectionHeader(rightPane, "数据与图鉴");
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

        AddSectionHeader(page, "提交反馈");
        var feedbackCard = BuildWhiteCard();
        feedbackCard.name = "settings-feedback-status-card";
        feedbackCard.Add(BuildInfoRow("反馈表单", "未接入", "此终端没有反馈收件端，写下的内容不会被发送或保存"));
        feedbackCard.Add(BuildInfoRow("联系渠道", "暂无", "没有可验证的调度电话、工单地址或外部反馈入口"));
        page.Add(feedbackCard);

        AddSectionHeader(page, "本机信息");
        var channelCard = BuildWhiteCard();
        channelCard.name = "settings-feedback-channel-card";
        channelCard.Add(BuildInfoRow("应用版本", Application.version));
        channelCard.Add(BuildInfoRow("日志与诊断", "不提供", "此页不导出日志，也不提交诊断包"));
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
        h.name = "settings-page-title";
        h.style.fontSize = 24; h.style.color = InkPrimary;
        h.style.unityFontDefinition = FdBold();
        h.style.alignSelf = Align.Center; h.style.marginBottom = 16;
        parent.Add(h);
    }

    private static void AddSectionHeader(VisualElement parent, string title)
    {
        var h = new Label(title);
        h.style.fontSize = 13; h.style.color = SectionHeaderColor;
        h.style.unityFontDefinition = FdBold();
        h.style.marginBottom = 8; h.style.marginLeft = 6;
        parent.Add(h);
    }

    // 可点行的悬停 / 按下反馈（按下比悬停更深，抬起或移出恢复）
    private static void AddRowFeedback(VisualElement row)
    {
        var pressed = new Color(SurfaceHover.r * 0.93f, SurfaceHover.g * 0.93f, SurfaceHover.b * 0.93f, 1f);
        row.RegisterCallback<PointerEnterEvent>(_ => row.style.backgroundColor = SurfaceHover);
        row.RegisterCallback<PointerLeaveEvent>(_ => row.style.backgroundColor = Color.clear);
        row.RegisterCallback<PointerDownEvent>(_ => row.style.backgroundColor = pressed);
        row.RegisterCallback<PointerUpEvent>(_ => row.style.backgroundColor = SurfaceHover);
    }

    private static VisualElement BuildWhiteCard()
    {
        var card = new VisualElement();
        // 卡片最后一行不画分割线（HyperOS 分组卡片的末行无下划线）
        card.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            int n = card.childCount;
            if (n == 0) return;
            card.ElementAt(n - 1).style.borderBottomWidth = 0;
        });
        card.style.backgroundColor = SurfaceCard;
        card.style.borderTopLeftRadius = 24; card.style.borderTopRightRadius = 24;
        card.style.borderBottomLeftRadius = 24; card.style.borderBottomRightRadius = 24;
        card.style.paddingLeft = 22; card.style.paddingRight = 22;
        card.style.paddingTop = 8; card.style.paddingBottom = 8;
        card.style.marginBottom = 16;
        return card;
    }

    private static VisualElement BuildWhiteRow(string title, string desc = null, System.Action onClick = null, bool hasArrow = true)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 14; row.style.paddingBottom = 14;
        row.style.paddingLeft = 8; row.style.paddingRight = 8;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = InsetDivider;
        row.style.borderTopLeftRadius = 10; row.style.borderTopRightRadius = 10;
        row.style.borderBottomLeftRadius = 10; row.style.borderBottomRightRadius = 10;
        row.pickingMode = onClick != null ? PickingMode.Position : PickingMode.Ignore;

        if (onClick != null)
        {
            AddRowFeedback(row);
            row.RegisterCallback<ClickEvent>(_ => onClick());
        }

        var left = new VisualElement();
        left.style.flexGrow = 1;
        row.Add(left);

        var t = new Label(title);
        t.style.fontSize = 18; t.style.color = InkPrimary;
        t.style.unityFontDefinition = Fd();
        left.Add(t);

        // 短说明作为右侧取值显示（如“当前电量  86%”），长说明仍放在标题下方
        bool shortValue = !string.IsNullOrEmpty(desc) && desc.Length <= 12 && desc.IndexOf('\n') < 0;
        if (!string.IsNullOrEmpty(desc) && !shortValue)
        {
            var d = new Label(desc);
            d.style.fontSize = 14; d.style.color = InkSecondary;
            d.style.unityFontDefinition = Fd(); d.style.marginTop = 4;
            d.style.whiteSpace = WhiteSpace.Normal;
            left.Add(d);
        }

        if (shortValue)
        {
            var v = new Label(desc);
            v.style.fontSize = 15; v.style.color = InkTertiary;
            v.style.unityFontDefinition = Fd();
            v.style.marginLeft = 12; v.style.marginRight = hasArrow ? 6 : 0;
            v.style.flexShrink = 0;
            row.Add(v);
        }

        if (hasArrow)
        {
            var ar = new Label("›");
            ar.style.fontSize = 20; ar.style.color = InkTertiary; ar.style.unityFontDefinition = Fd();
            row.Add(ar);
        }
        return row;
    }

    // 取值行：左侧标题（可带一行说明），右侧灰色取值，可选箭头（对照 HyperOS “标题 … 取值 ›”）
    private static VisualElement BuildInfoRow(string title, string value, string sub = null, System.Action onClick = null)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = string.IsNullOrEmpty(sub) ? 14 : 9; row.style.paddingBottom = string.IsNullOrEmpty(sub) ? 14 : 9;
        row.style.paddingLeft = 8; row.style.paddingRight = 8;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = InsetDivider;
        row.style.borderTopLeftRadius = row.style.borderTopRightRadius = 10;
        row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 10;
        row.pickingMode = onClick != null ? PickingMode.Position : PickingMode.Ignore;
        if (onClick != null)
        {
            AddRowFeedback(row);
            row.RegisterCallback<ClickEvent>(_ => onClick());
        }

        var left = new VisualElement();
        left.style.flexGrow = 1; left.style.flexShrink = 1;
        row.Add(left);
        var t = new Label(title);
        t.style.fontSize = 18; t.style.color = InkPrimary; t.style.unityFontDefinition = Fd();
        left.Add(t);
        if (!string.IsNullOrEmpty(sub))
        {
            var d = new Label(sub);
            d.style.fontSize = 14; d.style.color = InkSecondary; d.style.unityFontDefinition = Fd();
            d.style.marginTop = 4; d.style.whiteSpace = WhiteSpace.Normal;
            left.Add(d);
        }

        var right = new VisualElement();
        right.style.flexDirection = FlexDirection.Row;
        right.style.alignItems = Align.Center;
        right.style.flexShrink = 0;
        row.Add(right);
        if (!string.IsNullOrEmpty(value))
        {
            var v = new Label(value);
            v.style.fontSize = 15; v.style.color = InkTertiary; v.style.unityFontDefinition = Fd();
            v.style.marginLeft = 12;
            right.Add(v);
        }
        if (onClick != null)
        {
            var ar = new Label("›");
            ar.style.fontSize = 20; ar.style.color = InkTertiary; ar.style.unityFontDefinition = Fd();
            ar.style.marginLeft = 6;
            right.Add(ar);
        }
        return row;
    }

    private static VisualElement BuildToggleRow(string title, string desc, bool initialVal, System.Action<bool> onChanged, bool enabled = true)
    {
        var row = new VisualElement();
        if (!enabled) row.style.opacity = 0.45f;   // 禁用态：灰显且点击不翻转
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 14; row.style.paddingBottom = 14;
        row.style.paddingLeft = 8; row.style.paddingRight = 8;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = InsetDivider;
        row.style.borderTopLeftRadius = row.style.borderTopRightRadius = 10;
        row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 10;

        var left = new VisualElement();
        left.style.flexGrow = 1; left.style.flexShrink = 1;
        row.Add(left);

        var t = new Label(title);
        t.style.fontSize = 18; t.style.color = InkPrimary;
        t.style.unityFontDefinition = Fd();
        left.Add(t);

        if (!string.IsNullOrEmpty(desc))
        {
            var d = new Label(desc);
            d.style.fontSize = 14; d.style.color = InkSecondary;
            d.style.unityFontDefinition = Fd(); d.style.marginTop = 4;
            d.style.whiteSpace = WhiteSpace.Normal;
            left.Add(d);
        }

        // 胶囊开关
        bool val = initialVal;
        var toggle = new VisualElement { name = "toggle-capsule" };
        toggle.style.width = 52; toggle.style.height = 30; toggle.style.flexShrink = 0; toggle.style.marginLeft = 16;
        toggle.style.borderTopLeftRadius = 15; toggle.style.borderTopRightRadius = 15;
        toggle.style.borderBottomLeftRadius = 15; toggle.style.borderBottomRightRadius = 15;
        toggle.style.backgroundColor = val ? AccentSystem : new Color(0.85f, 0.85f, 0.87f, 1f);
        toggle.style.justifyContent = Justify.Center;
        toggle.pickingMode = PickingMode.Position;
        row.Add(toggle);

        var thumb = new VisualElement();
        thumb.style.width = 24; thumb.style.height = 24;
        thumb.style.borderTopLeftRadius = 12; thumb.style.borderTopRightRadius = 12;
        thumb.style.borderBottomLeftRadius = 12; thumb.style.borderBottomRightRadius = 12;
        thumb.style.backgroundColor = Color.white;
        thumb.style.transitionProperty = new List<StylePropertyName> { "margin-left" };
        thumb.style.transitionDuration = new List<TimeValue> { new TimeValue(0.16f, TimeUnit.Second) };
        thumb.style.marginLeft = val ? 25 : 3;
        toggle.Add(thumb);

        // 整行可点：开关本体只有 46×26，手指和鼠标都难点中，改为点行内任意位置即切换
        toggle.pickingMode = PickingMode.Ignore;
        if (enabled)
        {
            row.pickingMode = PickingMode.Position;
            AddRowFeedback(row);
            row.RegisterCallback<ClickEvent>(_ =>
            {
                val = !val;
                toggle.style.backgroundColor = val ? AccentSystem : new Color(0.85f, 0.85f, 0.87f, 1f);
                thumb.style.marginLeft = val ? 25 : 3;
                if (onChanged != null) onChanged(val);
            });
        }

        return row;
    }

    // 药丸滑块（对照 HyperOS 亮度 / 音量）：粗圆角轨道，蓝色填充，白色圆形滑块贴在填充末端；支持点击与拖动
    private static VisualElement BuildPillSlider(int min, int max, int current, System.Action<int> onChanged, bool enabled)
    {
        var track = new VisualElement { name = "settings-pill-slider" };
        track.style.height = 28; track.style.flexShrink = 0;
        track.style.backgroundColor = new Color(0.94f, 0.94f, 0.95f, 1f);
        track.style.borderTopLeftRadius = track.style.borderTopRightRadius = 14;
        track.style.borderBottomLeftRadius = track.style.borderBottomRightRadius = 14;
        var fill = new VisualElement { pickingMode = PickingMode.Ignore };
        fill.style.position = Position.Absolute;
        fill.style.left = 0; fill.style.top = 0; fill.style.bottom = 0;
        fill.style.backgroundColor = enabled ? AccentSystem : new Color(0.78f, 0.78f, 0.82f, 1f);
        fill.style.borderTopLeftRadius = fill.style.borderTopRightRadius = 14;
        fill.style.borderBottomLeftRadius = fill.style.borderBottomRightRadius = 14;
        track.Add(fill);
        var thumb = new VisualElement { pickingMode = PickingMode.Ignore };
        thumb.style.position = Position.Absolute; thumb.style.top = 3;
        thumb.style.width = 22; thumb.style.height = 22;
        thumb.style.backgroundColor = Color.white;
        thumb.style.borderTopLeftRadius = thumb.style.borderTopRightRadius = 11;
        thumb.style.borderBottomLeftRadius = thumb.style.borderBottomRightRadius = 11;
        track.Add(thumb);

        int value = Mathf.Clamp(current, min, max);
        Action layout = () =>
        {
            float w = track.resolvedStyle.width;
            if (float.IsNaN(w) || w <= 0f) return;
            float t = (value - min) / (float)Mathf.Max(1, max - min);
            float fw = Mathf.Max(28f, t * w);
            fill.style.width = fw;
            thumb.style.left = fw - 25f;
        };
        track.RegisterCallback<GeometryChangedEvent>(_ => layout());
        if (!enabled) return track;

        bool dragging = false;
        Action<float> setFromX = x =>
        {
            float w = track.resolvedStyle.width;
            if (float.IsNaN(w) || w <= 0f) return;
            int nv = min + Mathf.RoundToInt(Mathf.Clamp01(x / w) * (max - min));
            if (nv == value) return;
            value = nv;
            layout();
            onChanged?.Invoke(value);
        };
        track.RegisterCallback<PointerDownEvent>(e =>
        {
            dragging = true;
            track.CapturePointer(e.pointerId);
            setFromX(e.localPosition.x);
            e.StopPropagation();
        });
        track.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (dragging) setFromX(e.localPosition.x);
        });
        track.RegisterCallback<PointerUpEvent>(e =>
        {
            if (!dragging) return;
            dragging = false;
            track.ReleasePointer(e.pointerId);
        });
        track.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
        return track;
    }

    // 滑块行：标题在上（右侧显示当前值），整宽药丸滑块在下（对照 HyperOS“屏幕亮度 / 音量”）
    private static VisualElement BuildSliderRow(string title, int min, int max, int current, System.Action<int> onChanged, string unit = "", bool enabled = true, string iconKey = null)
    {
        var row = new VisualElement();
        row.style.paddingTop = 18; row.style.paddingBottom = 20;
        row.style.paddingLeft = 8; row.style.paddingRight = 8;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = InsetDivider;

        var head = new VisualElement();
        head.style.flexDirection = FlexDirection.Row;
        head.style.justifyContent = Justify.SpaceBetween;
        head.style.alignItems = Align.Center;
        head.style.marginBottom = 14;
        row.Add(head);

        var titleBox = new VisualElement();
        titleBox.style.flexDirection = FlexDirection.Row; titleBox.style.alignItems = Align.Center;
        head.Add(titleBox);
        if (!string.IsNullOrEmpty(iconKey))
        {
            var ic = new TabletIconElement(CategoryIconKind(iconKey), 24f, InkTertiary);
            ic.style.marginRight = 14;
            titleBox.Add(ic);
        }
        var t = new Label(title);
        t.style.fontSize = 18; t.style.color = enabled ? InkPrimary : InkTertiary;
        t.style.unityFontDefinition = Fd();
        titleBox.Add(t);

        var valLbl = new Label(current + unit);
        valLbl.style.fontSize = 15; valLbl.style.color = enabled ? AccentSystem : InkTertiary;
        valLbl.style.unityFontDefinition = Fd();
        head.Add(valLbl);

        row.Add(BuildPillSlider(min, max, current, v =>
        {
            valLbl.text = v + unit;
            onChanged?.Invoke(v);
        }, enabled));
        return row;
    }

    // 分类图标：把原来的 emoji 键映射成代码绘制的矢量图标，放在彩色圆角方块里
    private static TabletIconKind CategoryIconKind(string key)
    {
        switch (key)
        {
            case "👤": return TabletIconKind.Person;
            case "📱": return TabletIconKind.Tablet;
            case "🧩": return TabletIconKind.Apps;
            case "📶": return TabletIconKind.Wifi;
            case "ᛒ": return TabletIconKind.Bluetooth;
            case "🔗": return TabletIconKind.Link;
            case "🌐": return TabletIconKind.Globe;
            case "🔮": return TabletIconKind.Sparkle;
            case "🎨": return TabletIconKind.Palette;
            case "🔒": return TabletIconKind.Lock;
            case "🔔": return TabletIconKind.Bell;
            case "🏠": return TabletIconKind.Home;
            case "☀": return TabletIconKind.Sun;
            case "🔊": return TabletIconKind.Speaker;
            case "⌨": return TabletIconKind.Keyboard;
            case "😊": return TabletIconKind.FaceId;
            case "🛡": return TabletIconKind.Shield;
            case "▦": return TabletIconKind.Cube;
            case "🔋": return TabletIconKind.Battery;
            case "💚": return TabletIconKind.Heart;
            case "🧒": return TabletIconKind.Sprout;
            case "♿": return TabletIconKind.Accessibility;
            case "⋯": return TabletIconKind.Dots;
            case "💬": return TabletIconKind.Chat;
            case "🛠": return TabletIconKind.Code;
            default: return TabletIconKind.Dots;
        }
    }

    private static VisualElement BuildIconBadge(string key, Color badgeColor, float size)
    {
        var badge = new VisualElement();
        badge.style.width = size; badge.style.height = size;
        badge.style.flexShrink = 0;
        badge.style.alignItems = Align.Center; badge.style.justifyContent = Justify.Center;
        badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius = size * 0.3f;
        badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = size * 0.3f;
        badge.style.backgroundColor = badgeColor;
        badge.pickingMode = PickingMode.Ignore;
        badge.Add(new TabletIconElement(CategoryIconKind(key), size * 0.58f, Color.white));
        return badge;
    }

    // 侧栏顶部的账号行：圆形头像 + 名称 + 一行说明（对照参考图的“小牛马 / Manage accounts…”）
    private static void ConvertToAccountNavItem(Button item)
    {
        item.style.height = 92;
        item.Clear();
        var avatar = new Label(AccountService.IsRegistered ? AccountService.DisplayName.Substring(0, 1) : string.Empty);
        avatar.style.alignItems = Align.Center; avatar.style.justifyContent = Justify.Center;
        if (AccountService.IsRegistered)
        {
            avatar.style.color = Color.white;
            avatar.style.backgroundColor = AccountAvatarColors[Mathf.Clamp(AccountService.Profile.avatar, 0, AccountAvatarColors.Length - 1)];
        }
        else avatar.Add(new TabletIconElement(TabletIconKind.Person, 36, new Color(0.62f, 0.64f, 0.70f, 1f)));
        avatar.style.width = 58; avatar.style.height = 58; avatar.style.flexShrink = 0;
        avatar.style.fontSize = 30; avatar.style.unityTextAlign = TextAnchor.MiddleCenter;
        if (!AccountService.IsRegistered) avatar.style.backgroundColor = SurfaceApp;
        avatar.style.borderTopLeftRadius = avatar.style.borderTopRightRadius = 29;
        avatar.style.borderBottomLeftRadius = avatar.style.borderBottomRightRadius = 29;
        avatar.style.unityFontDefinition = Fd();
        avatar.style.marginRight = 14;
        item.Add(avatar);
        var col = new VisualElement();
        col.style.flexShrink = 1;
        col.style.alignItems = Align.FlexStart;
        var name = new Label(AccountService.IsRegistered ? AccountService.DisplayName : "登录小米账号");
        name.style.fontSize = 19; name.style.color = InkPrimary; name.style.unityFontDefinition = FdBold();
        name.style.unityTextAlign = TextAnchor.UpperLeft;
        var sub = new Label(AccountService.IsRegistered ? "管理账号、云空间与家庭服务" : "登录后使用云空间与家庭服务");
        sub.style.fontSize = 14; sub.style.color = InkSecondary; sub.style.unityFontDefinition = Fd();
        sub.style.unityTextAlign = TextAnchor.UpperLeft;
        sub.style.marginTop = 2; sub.style.whiteSpace = WhiteSpace.Normal;
        col.Add(name); col.Add(sub);
        item.Add(col);
    }

    // HyperOS 风格滑条：细圆角轨道 + 强调色圆形滑块，去掉 Unity 默认的方块滑块与边框
    private static void StyleHyperSlider(BaseSlider<int> slider, bool enabled)
    {
        var tracker = slider.Q(className: "unity-base-slider__tracker");
        if (tracker != null)
        {
            tracker.style.height = 6;
            tracker.style.backgroundColor = new Color(0.86f, 0.86f, 0.89f, 1f);
            tracker.style.borderTopLeftRadius = tracker.style.borderTopRightRadius = 3;
            tracker.style.borderBottomLeftRadius = tracker.style.borderBottomRightRadius = 3;
            tracker.style.borderTopWidth = tracker.style.borderBottomWidth = tracker.style.borderLeftWidth = tracker.style.borderRightWidth = 0;
        }
        var dragger = slider.Q(className: "unity-base-slider__dragger");
        if (dragger != null)
        {
            dragger.style.width = 22; dragger.style.height = 22;
            dragger.style.backgroundColor = enabled ? AccentSystem : new Color(0.72f, 0.72f, 0.76f, 1f);
            dragger.style.borderTopLeftRadius = dragger.style.borderTopRightRadius = 11;
            dragger.style.borderBottomLeftRadius = dragger.style.borderBottomRightRadius = 11;
            dragger.style.borderTopWidth = dragger.style.borderBottomWidth = dragger.style.borderLeftWidth = dragger.style.borderRightWidth = 0;
        }
        var draggerBorder = slider.Q(className: "unity-base-slider__dragger-border");
        if (draggerBorder != null) draggerBorder.style.display = DisplayStyle.None;
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

        var confirmTitle = new Label("恢复存档二次确认");
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
    private sealed class TipsSelection { public string Id; }
    private static Action TipsRebuilder(TipsSelection selection) => () => ShowTipsPageWithSelection(selection);
    public static void ShowTipsPage(string termId = null) => ShowTipsPageWithSelection(new TipsSelection { Id = termId });
    private static void ShowTipsPageWithSelection(TipsSelection selection)
    {
        string termId = selection.Id;
        var win = BuildInnerPanel("术语图鉴", TipsRebuilder(selection));
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
                selection.Id = captured.id;
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
        var btn = new Button(() => { if (GameMainUI.Instance != null) GameMainUI.Instance.gameObject.SetActive(true); StoryReplayService.OpenLibrary(); }) { text = "打开归途" };
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
