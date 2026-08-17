using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>站长日志合集面板：成就 / 鉴赏 / 图鉴（UI Toolkit 动态构建，中文 UI）。</summary>
public class TitleArchiveUI : MonoBehaviour
{
    /// <summary>CG 鉴赏条目。</summary>
    private class CgInfo
    {
        public string id;
        public string title;
        public string condition;
        public string imagePath; // Resources 路径（无扩展名），可为空
    }

    /// <summary>图鉴条目（角色/列车共用）。</summary>
    private class ArchiveInfo
    {
        public string id;
        public string name;
        public string type;   // 职务/分类
        public string intro;  // 简介
        public string condition;
        public string prefsKey;
    }

    private UIDocument uiDoc;
    private Font gameFont;
    private VisualElement overlay;
    private VisualElement panel;
    private ScrollView contentScroll;

    /// <summary>页签 key → 内容页。</summary>
    private readonly Dictionary<string, VisualElement> tabPages = new Dictionary<string, VisualElement>();
    /// <summary>页签 key → 按钮。</summary>
    private readonly Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();

    private string currentTabKey = "achievements";

    // 配色：与 TitleScreen 玻璃按钮风格一致（暖色调 + 金色边框）
    private readonly Color goldNormal = new Color(1f, 215f / 255f, 0f, 0.95f);
    private readonly Color goldHover = new Color(1f, 230f / 255f, 100f / 255f, 1f);
    private readonly Color borderNormal = new Color(1f, 220f / 255f, 150f / 255f, 0.55f);
    private readonly Color borderHover = new Color(1f, 230f / 255f, 170f / 255f, 0.85f);
    private readonly Color borderDim = new Color(0.5f, 0.4f, 0.3f, 0.35f);
    private readonly Color glassBg = new Color(40f / 255f, 25f / 255f, 15f / 255f, 0.35f);
    private readonly Color glassBgHover = new Color(40f / 255f, 25f / 255f, 15f / 255f, 0.55f);
    private readonly Color panelBg = new Color(0.10f, 0.06f, 0.04f, 0.97f);
    private readonly Color dimText = new Color(1f, 1f, 1f, 0.35f);
    private readonly Color grayText = new Color(0.6f, 0.6f, 0.6f, 0.8f);

    // 稀有度配色：普通灰 / 稀有蓝 / 史诗紫 / 传说金
    private readonly Color rarityCommon = new Color(0.75f, 0.75f, 0.75f, 1f);
    private readonly Color rarityRare = new Color(0.35f, 0.72f, 1f, 1f);
    private readonly Color rarityEpic = new Color(0.72f, 0.45f, 1f, 1f);
    private readonly Color rarityLegend = new Color(1f, 0.80f, 0.25f, 1f);

    private static readonly CgInfo[] Cgs =
    {
        new CgInfo { id = "cg_lab",       title = "实验室（平壤）", condition = "序章 Day 0 自动解锁", imagePath = "bg/lab" },
        new CgInfo { id = "cg_sunset",    title = "雾峰村夕阳",    condition = "序章 Day 4 到达" },
        new CgInfo { id = "cg_bridge",    title = "松桥站",        condition = "序章 Day 4 巡视" },
        new CgInfo { id = "cg_team",      title = "员工集合",      condition = "序章 Day 4 晚上" },
        new CgInfo { id = "cg_first_run", title = "首班车",        condition = "序章首班车" },
        new CgInfo { id = "cg_museum",    title = "铁路博物馆",    condition = "好感度 > 90 解锁" },
    };

    private static readonly ArchiveInfo[] Characters =
    {
        new ArchiveInfo { id = "lin",        name = "林彪悍", type = "见习站长",       intro = "25岁 · 金日成综合大学荣誉研究生，倔强、青涩但充满希望，继承爷爷的站长遗志。", condition = "序章开始解锁", prefsKey = "ArchiveChar_lin" },
        new ArchiveInfo { id = "laochen",    name = "老陈",   type = "最后一任站长",   intro = "68岁 · 雾峰村的最后一任站长，主角的导师，温暖、朴实、固执而善良。",       condition = "序章 Day 4 见面", prefsKey = "ArchiveChar_laochen" },
        new ArchiveInfo { id = "zhanggong",  name = "张工",   type = "退休机械工程师", intro = "62岁 · 维修精通（初始3级），乐观开朗。",                                condition = "序章员工集合", prefsKey = "ArchiveChar_zhanggong" },
        new ArchiveInfo { id = "liaiyi",     name = "李阿姨", type = "社区热心居民",   intro = "55岁 · 服务热心（初始2级），是车站最暖的人。",                          condition = "序章员工集合", prefsKey = "ArchiveChar_liaiyi" },
        new ArchiveInfo { id = "wangxiaodi", name = "王小弟", type = "毕业生学员",     intro = "22岁 · 刚毕业大学生，阳光热血，驾驶潜力最高（上限5级）。",              condition = "序章员工集合", prefsKey = "ArchiveChar_wangxiaodi" },
        new ArchiveInfo { id = "zhaoshifu",  name = "赵师傅", type = "退休铁路工程师", intro = "55岁 · 管理熟练（初始2级），沉稳可靠。",                                condition = "序章员工集合", prefsKey = "ArchiveChar_zhaoshifu" },
        new ArchiveInfo { id = "xiaofang",   name = "小芳",   type = "志愿者",         intro = "45岁 · 性格热情，服务潜力大（上限4级）。",                              condition = "序章员工集合", prefsKey = "ArchiveChar_xiaofang" },
        new ArchiveInfo { id = "suiyue",     name = "岁月",   type = "AI 原型",        intro = "0721号沙子飞猪号搭载的AI原型，2053年制造，沉睡23年，正经精确、偶尔冷幽默。", condition = "序章 Day 0 领取载具", prefsKey = "ArchiveChar_suiyue" },
    };

    private static readonly ArchiveInfo[] Trains =
    {
        new ArchiveInfo { id = "nf5",  name = "NF-5 耕牛", type = "机车", intro = "柴油机车（原型东风4型货运内燃机车），最高速度80km/h，爷爷留下的老伙计。",      condition = "序章初始", prefsKey = "ArchiveTrain_nf5" },
        new ArchiveInfo { id = "sy22", name = "SY-22 灰雀", type = "客车", intro = "短途支线小型客车，可载客30人。",                                            condition = "序章初始", prefsKey = "ArchiveTrain_sy22" },
    };

    private FontDefinition Fd() => new FontDefinition { font = gameFont };

    /// <summary>初始化面板（由 TitleScreen 调用）。</summary>
    public void Init(UIDocument document)
    {
        uiDoc = document;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        AchievementManager.Initialize();
        BuildUI();
    }

    private void BuildUI()
    {
        var fontDef = Fd();
        var root = uiDoc.rootVisualElement;

        // ———— 全屏遮罩 ————
        overlay = new VisualElement { name = "archive-overlay" };
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0; overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
        overlay.style.display = DisplayStyle.None;
        overlay.RegisterCallback<ClickEvent>(evt =>
        {
            // 只有点击遮罩空白处才关闭
            if (ReferenceEquals(evt.target, overlay)) Hide();
        });
        root.Add(overlay);

        // ———— 居中容器 ————
        var center = new VisualElement();
        center.style.flexGrow = 1;
        center.style.alignItems = Align.Center;
        center.style.justifyContent = Justify.Center;
        overlay.Add(center);

        // ———— 面板 ————
        panel = new VisualElement { name = "archive-panel" };
        panel.style.flexDirection = FlexDirection.Column;
        panel.style.width = 960;
        panel.style.height = 640;
        panel.style.backgroundColor = panelBg;
        panel.style.borderTopWidth = 2; panel.style.borderBottomWidth = 2;
        panel.style.borderLeftWidth = 2; panel.style.borderRightWidth = 2;
        panel.style.borderTopColor = borderNormal; panel.style.borderBottomColor = borderNormal;
        panel.style.borderLeftColor = borderNormal; panel.style.borderRightColor = borderNormal;
        panel.style.borderTopLeftRadius = 10; panel.style.borderTopRightRadius = 10;
        panel.style.borderBottomLeftRadius = 10; panel.style.borderBottomRightRadius = 10;
        panel.style.paddingLeft = 26; panel.style.paddingRight = 26;
        panel.style.paddingTop = 18; panel.style.paddingBottom = 20;
        center.Add(panel);

        // ———— 标题栏 ————
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 12;
        panel.Add(header);

        var title = new Label("站长日志");
        title.style.fontSize = 34;
        title.style.color = goldNormal;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityFontDefinition = fontDef;
        title.style.flexGrow = 1;
        header.Add(title);

        var closeBtn = new Button(Hide) { text = "关闭" };
        closeBtn.style.width = 90;
        closeBtn.style.height = 40;
        closeBtn.style.fontSize = 18;
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = fontDef;
        StylizeTab(closeBtn);
        header.Add(closeBtn);

        // ———— 标签页按钮 ————
        var tabRow = new VisualElement();
        tabRow.style.flexDirection = FlexDirection.Row;
        tabRow.style.marginBottom = 14;
        panel.Add(tabRow);

        AddTabButton(tabRow, "achievements", "成就");
        AddTabButton(tabRow, "gallery", "鉴赏");
        AddTabButton(tabRow, "collection", "图鉴");

        // ———— 内容区 ————
        contentScroll = new ScrollView(ScrollViewMode.Vertical);
        contentScroll.name = "archive-scroll";
        contentScroll.style.flexGrow = 1;
        contentScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        panel.Add(contentScroll);

        RebuildPages();
        ShowTab(currentTabKey);
    }

    private void AddTabButton(VisualElement parent, string key, string text)
    {
        var btn = new Button(() => ShowTab(key)) { text = text };
        btn.name = "tab-" + key;
        btn.style.width = 130;
        btn.style.height = 46;
        btn.style.marginRight = 10;
        btn.style.fontSize = 20;
        btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
        btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
        parent.Add(btn);
        tabButtons[key] = btn;
    }

    /// <summary>重建三个页面（每次 Show 时刷新解锁状态）。</summary>
    private void RebuildPages()
    {
        contentScroll.Clear();
        tabPages.Clear();
        BuildAchievementsPage();
        BuildGalleryPage();
        BuildCollectionPage();
    }

    // ================= 成就页 =================

    private void BuildAchievementsPage()
    {
        var page = new VisualElement { name = "page-achievements" };
        page.style.display = DisplayStyle.None;
        contentScroll.Add(page);
        tabPages["achievements"] = page;

        var all = AchievementManager.GetAll();
        var counter = new Label("已解锁 " + AchievementManager.GetUnlockedCount() + " / " + all.Length);
        counter.style.fontSize = 18;
        counter.style.color = goldNormal;
        counter.style.unityFontDefinition = Fd();
        counter.style.marginBottom = 10;
        page.Add(counter);

        for (int i = 0; i < all.Length; i++)
        {
            var info = all[i];
            if (info.unlocked)
            {
                page.Add(BuildUnlockedAchievementCard(info));
            }
            else
            {
                page.Add(BuildLockedAchievementCard());
            }
        }
    }

    private VisualElement BuildUnlockedAchievementCard(AchievementData info)
    {
        var card = new VisualElement();
        card.style.flexDirection = FlexDirection.Row;
        card.style.alignItems = Align.Center;
        card.style.paddingLeft = 16; card.style.paddingRight = 16;
        card.style.paddingTop = 10; card.style.paddingBottom = 10;
        card.style.marginBottom = 8;
        card.style.backgroundColor = glassBg;
        card.style.borderTopLeftRadius = 6; card.style.borderTopRightRadius = 6;
        card.style.borderBottomLeftRadius = 6; card.style.borderBottomRightRadius = 6;
        card.style.flexShrink = 0;

        var rarityColor = RarityColor(info.rarity);

        var rarityTag = new Label(RarityName(info.rarity));
        rarityTag.style.width = 64;
        rarityTag.style.height = 26;
        rarityTag.style.fontSize = 15;
        rarityTag.style.unityTextAlign = TextAnchor.MiddleCenter;
        rarityTag.style.unityFontDefinition = Fd();
        rarityTag.style.color = rarityColor;
        rarityTag.style.borderTopWidth = 1; rarityTag.style.borderBottomWidth = 1;
        rarityTag.style.borderLeftWidth = 1; rarityTag.style.borderRightWidth = 1;
        rarityTag.style.borderTopColor = rarityColor; rarityTag.style.borderBottomColor = rarityColor;
        rarityTag.style.borderLeftColor = rarityColor; rarityTag.style.borderRightColor = rarityColor;
        rarityTag.style.borderTopLeftRadius = 4; rarityTag.style.borderTopRightRadius = 4;
        rarityTag.style.borderBottomLeftRadius = 4; rarityTag.style.borderBottomRightRadius = 4;
        rarityTag.style.marginRight = 14;
        card.Add(rarityTag);

        var text = new VisualElement();
        text.style.flexGrow = 1;
        text.style.flexDirection = FlexDirection.Column;
        card.Add(text);

        var title = new Label(info.title);
        title.style.fontSize = 22;
        title.style.color = new Color(1f, 1f, 1f, 0.95f);
        title.style.unityFontDefinition = Fd();
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 4;
        text.Add(title);

        var desc = new Label(info.description);
        desc.style.fontSize = 17;
        desc.style.color = new Color(1f, 0.92f, 0.7f, 0.85f);
        desc.style.unityFontDefinition = Fd();
        desc.style.whiteSpace = WhiteSpace.Normal;
        text.Add(desc);

        if (!string.IsNullOrEmpty(info.unlockedDate))
        {
            var date = new Label("解锁于 " + info.unlockedDate);
            date.style.fontSize = 14;
            date.style.color = dimText;
            date.style.unityFontDefinition = Fd();
            date.style.marginRight = 8;
            card.Add(date);
        }

        return card;
    }

    private VisualElement BuildLockedAchievementCard()
    {
        var card = new VisualElement();
        card.style.flexDirection = FlexDirection.Row;
        card.style.alignItems = Align.Center;
        card.style.paddingLeft = 16; card.style.paddingRight = 16;
        card.style.paddingTop = 10; card.style.paddingBottom = 10;
        card.style.marginBottom = 8;
        card.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
        card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
        card.style.borderTopColor = borderDim; card.style.borderBottomColor = borderDim;
        card.style.borderLeftColor = borderDim; card.style.borderRightColor = borderDim;
        card.style.borderTopLeftRadius = 6; card.style.borderTopRightRadius = 6;
        card.style.borderBottomLeftRadius = 6; card.style.borderBottomRightRadius = 6;
        card.style.flexShrink = 0;

        var unknown = new Label("???");
        unknown.style.fontSize = 22;
        unknown.style.color = grayText;
        unknown.style.unityFontDefinition = Fd();
        unknown.style.flexGrow = 1;
        unknown.style.unityTextAlign = TextAnchor.MiddleLeft;
        card.Add(unknown);

        return card;
    }

    // ================= 鉴赏页 =================

    private void BuildGalleryPage()
    {
        var page = new VisualElement { name = "page-gallery" };
        page.style.display = DisplayStyle.None;
        contentScroll.Add(page);
        tabPages["gallery"] = page;

        var tip = new Label("已解锁的 CG 回忆会收藏在这里");
        tip.style.fontSize = 17;
        tip.style.color = dimText;
        tip.style.unityFontDefinition = Fd();
        tip.style.marginBottom = 12;
        page.Add(tip);

        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        page.Add(grid);

        for (int i = 0; i < Cgs.Length; i++)
        {
            grid.Add(BuildCgCard(Cgs[i]));
        }
    }

    private VisualElement BuildCgCard(CgInfo cg)
    {
        bool unlocked = PlayerPrefs.GetInt("ArchiveCG_" + cg.id, 0) == 1;

        var card = new VisualElement();
        card.style.width = 290;
        card.style.height = 210;
        card.style.marginRight = 12;
        card.style.marginBottom = 12;
        card.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.9f);
        card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
        card.style.borderTopColor = unlocked ? borderNormal : borderDim;
        card.style.borderBottomColor = unlocked ? borderNormal : borderDim;
        card.style.borderLeftColor = unlocked ? borderNormal : borderDim;
        card.style.borderRightColor = unlocked ? borderNormal : borderDim;
        card.style.borderTopLeftRadius = 6; card.style.borderTopRightRadius = 6;
        card.style.borderBottomLeftRadius = 6; card.style.borderBottomRightRadius = 6;
        card.style.paddingTop = 8; card.style.paddingBottom = 8;
        card.style.paddingLeft = 8; card.style.paddingRight = 8;
        card.style.flexShrink = 0;

        // 图片区
        var art = new VisualElement();
        art.style.flexGrow = 1;
        art.style.marginBottom = 8;
        art.style.backgroundColor = unlocked
            ? new Color(0.25f, 0.16f, 0.10f, 0.9f)
            : new Color(0.10f, 0.10f, 0.12f, 0.9f);
        art.style.overflow = Overflow.Hidden;
        card.Add(art);

        if (unlocked)
        {
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(cg.imagePath))
                tex = Resources.Load<Texture2D>(cg.imagePath);
            if (tex != null)
            {
                art.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
                art.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            }
            else
            {
                var placeholder = new Label("「" + cg.title + "」CG 图待补充");
                placeholder.style.width = new Length(100, LengthUnit.Percent);
                placeholder.style.height = new Length(100, LengthUnit.Percent);
                placeholder.style.fontSize = 22;
                placeholder.style.color = goldNormal;
                placeholder.style.unityFontDefinition = Fd();
                placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
                art.Add(placeholder);
            }
        }
        else
        {
            var lockRow = new VisualElement();
            lockRow.style.flexGrow = 1;
            lockRow.style.alignItems = Align.Center;
            lockRow.style.justifyContent = Justify.Center;
            art.Add(lockRow);

            // 锁标记（zpix 兼容的汉字符号）
            var lockBadge = new Label("锁");
            lockBadge.style.width = 40;
            lockBadge.style.height = 40;
            lockBadge.style.fontSize = 20;
            lockBadge.style.unityTextAlign = TextAnchor.MiddleCenter;
            lockBadge.style.unityFontDefinition = Fd();
            lockBadge.style.color = grayText;
            lockBadge.style.backgroundColor = new Color(0f, 0f, 0f, 0.4f);
            lockBadge.style.borderTopLeftRadius = 20;
            lockBadge.style.borderTopRightRadius = 20;
            lockBadge.style.borderBottomLeftRadius = 20;
            lockBadge.style.borderBottomRightRadius = 20;
            lockBadge.style.marginBottom = 12;
            lockRow.Add(lockBadge);

            var cond = new Label("解锁条件：" + cg.condition);
            cond.style.fontSize = 15;
            cond.style.color = grayText;
            cond.style.unityTextAlign = TextAnchor.MiddleCenter;
            cond.style.unityFontDefinition = Fd();
            lockRow.Add(cond);
        }

        // 标题行
        var titleRow = new VisualElement();
        titleRow.style.flexDirection = FlexDirection.Row;
        titleRow.style.alignItems = Align.Center;
        card.Add(titleRow);

        var title = new Label(cg.title);
        title.style.fontSize = 18;
        title.style.color = unlocked ? goldNormal : grayText;
        title.style.unityFontDefinition = Fd();
        title.style.flexGrow = 1;
        titleRow.Add(title);

        var status = new Label(unlocked ? "已解锁" : "未解锁");
        status.style.fontSize = 15;
        status.style.color = unlocked ? new Color(1f, 0.9f, 0.5f, 1f) : dimText;
        status.style.unityFontDefinition = Fd();
        titleRow.Add(status);

        return card;
    }

    // ================= 图鉴页 =================

    private void BuildCollectionPage()
    {
        var page = new VisualElement { name = "page-collection" };
        page.style.display = DisplayStyle.None;
        contentScroll.Add(page);
        tabPages["collection"] = page;

        var charHeader = new Label("角色档案");
        charHeader.style.fontSize = 20;
        charHeader.style.color = goldNormal;
        charHeader.style.unityFontDefinition = Fd();
        charHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        charHeader.style.marginTop = 6;
        charHeader.style.marginBottom = 8;
        page.Add(charHeader);

        for (int i = 0; i < Characters.Length; i++)
        {
            page.Add(BuildArchiveCard(Characters[i]));
        }

        var trainHeader = new Label("列车档案");
        trainHeader.style.fontSize = 20;
        trainHeader.style.color = goldNormal;
        trainHeader.style.unityFontDefinition = Fd();
        trainHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        trainHeader.style.marginTop = 18;
        trainHeader.style.marginBottom = 8;
        page.Add(trainHeader);

        for (int i = 0; i < Trains.Length; i++)
        {
            page.Add(BuildArchiveCard(Trains[i]));
        }
    }

    private VisualElement BuildArchiveCard(ArchiveInfo info)
    {
        bool collected = PlayerPrefs.GetInt(info.prefsKey, 0) == 1;

        var card = new VisualElement();
        card.style.flexDirection = FlexDirection.Column;
        card.style.paddingLeft = 16; card.style.paddingRight = 16;
        card.style.paddingTop = 10; card.style.paddingBottom = 10;
        card.style.marginBottom = 8;
        card.style.borderTopLeftRadius = 6; card.style.borderTopRightRadius = 6;
        card.style.borderBottomLeftRadius = 6; card.style.borderBottomRightRadius = 6;
        card.style.flexShrink = 0;

        if (collected)
        {
            card.style.backgroundColor = glassBg;
        }
        else
        {
            card.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
            card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor = borderDim; card.style.borderBottomColor = borderDim;
            card.style.borderLeftColor = borderDim; card.style.borderRightColor = borderDim;
        }

        var nameRow = new VisualElement();
        nameRow.style.flexDirection = FlexDirection.Row;
        nameRow.style.alignItems = Align.Center;
        card.Add(nameRow);

        var name = new Label(collected ? info.name : "???");
        name.style.fontSize = 21;
        name.style.color = collected ? new Color(1f, 1f, 1f, 0.95f) : grayText;
        name.style.unityFontDefinition = Fd();
        name.style.unityFontStyleAndWeight = FontStyle.Bold;
        name.style.marginRight = 12;
        nameRow.Add(name);

        var type = new Label(info.type);
        type.style.fontSize = 15;
        type.style.color = collected ? new Color(1f, 0.92f, 0.7f, 0.8f) : grayText;
        type.style.unityFontDefinition = Fd();
        type.style.marginRight = 8;
        nameRow.Add(type);

        if (collected)
        {
            var status = new Label("已收集");
            status.style.fontSize = 14;
            status.style.color = new Color(1f, 0.9f, 0.5f, 1f);
            status.style.unityFontDefinition = Fd();
            status.style.marginLeft = new StyleLength(StyleKeyword.Auto);
            nameRow.Add(status);
        }

        if (collected)
        {
            var intro = new Label(info.intro);
            intro.style.fontSize = 16;
            intro.style.color = new Color(1f, 1f, 1f, 0.75f);
            intro.style.unityFontDefinition = Fd();
            intro.style.marginTop = 6;
            intro.style.whiteSpace = WhiteSpace.Normal;
            card.Add(intro);
        }
        else
        {
            var cond = new Label("解锁条件：" + info.condition);
            cond.style.fontSize = 15;
            cond.style.color = grayText;
            cond.style.unityFontDefinition = Fd();
            cond.style.marginTop = 4;
            card.Add(cond);
        }

        return card;
    }

    // ================= 工具 =================

    private string RarityName(AchievementRarity rarity)
    {
        switch (rarity)
        {
            case AchievementRarity.Common: return "普通";
            case AchievementRarity.Rare: return "稀有";
            case AchievementRarity.Epic: return "史诗";
            case AchievementRarity.Legend: return "传说";
            default: return "";
        }
    }

    private Color RarityColor(AchievementRarity rarity)
    {
        switch (rarity)
        {
            case AchievementRarity.Common: return rarityCommon;
            case AchievementRarity.Rare: return rarityRare;
            case AchievementRarity.Epic: return rarityEpic;
            case AchievementRarity.Legend: return rarityLegend;
            default: return Color.white;
        }
    }

    /// <summary>玻璃按钮样式（标题栏按钮 / 关闭按钮共用），主调：金色描边 + 暖色玻璃。</summary>
    private void StylizeTab(Button btn)
    {
        btn.style.backgroundColor = glassBg;
        btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
        btn.style.borderTopColor = borderNormal; btn.style.borderBottomColor = borderNormal;
        btn.style.borderLeftColor = borderNormal; btn.style.borderRightColor = borderNormal;
        btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
        btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
        btn.style.color = goldNormal;

        btn.RegisterCallback<PointerEnterEvent>(evt =>
        {
            btn.style.backgroundColor = glassBgHover;
            btn.style.borderTopColor = borderHover;
            btn.style.borderRightColor = borderHover;
            btn.style.borderBottomColor = borderHover;
            btn.style.borderLeftColor = borderHover;
            btn.style.color = goldHover;
        });
        btn.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            btn.style.backgroundColor = glassBg;
            btn.style.borderTopColor = borderNormal;
            btn.style.borderRightColor = borderNormal;
            btn.style.borderBottomColor = borderNormal;
            btn.style.borderLeftColor = borderNormal;
            btn.style.color = goldNormal;
            ApplyTabState(btn);
        });
    }

    /// <summary>页签切换：显示对应页面，并高亮当前页签按钮。</summary>
    private void ShowTab(string key)
    {
        if (!tabPages.ContainsKey(key)) return;

        currentTabKey = key;
        foreach (var kv in tabPages)
        {
            bool active = kv.Key == key;
            kv.Value.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }
        foreach (var kv in tabButtons)
        {
            ApplyTabState(kv.Value, kv.Key == key);
        }
    }

    private void ApplyTabState(Button btn)
    {
        // 由页签重新计算状态；非页签按钮（关闭按钮）直接使用基本样式
        if (btn.name != null && btn.name.StartsWith("tab-"))
        {
            string key = btn.name.Substring("tab-".Length);
            ApplyTabState(btn, key == currentTabKey);
        }
    }

    private void ApplyTabState(Button btn, bool active)
    {
        if (!active)
        {
            btn.style.backgroundColor = glassBg;
            btn.style.color = goldNormal;
            btn.style.borderTopWidth = 1; btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1; btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = borderNormal; btn.style.borderBottomColor = borderNormal;
            btn.style.borderLeftColor = borderNormal; btn.style.borderRightColor = borderNormal;
        }
        else
        {
            btn.style.backgroundColor = glassBgHover;
            btn.style.color = goldHover;
            btn.style.borderTopWidth = 2; btn.style.borderBottomWidth = 2;
            btn.style.borderLeftWidth = 2; btn.style.borderRightWidth = 2;
            btn.style.borderTopColor = borderHover; btn.style.borderBottomColor = borderHover;
            btn.style.borderLeftColor = borderHover; btn.style.borderRightColor = borderHover;
        }
    }

    /// <summary>显示站长日志面板（重建页面以刷新解锁状态）。</summary>
    public void Show()
    {
        if (overlay == null) return;

        RebuildPages();
        ShowTab(currentTabKey);

        overlay.style.display = DisplayStyle.Flex;
        if (panel != null)
        {
            panel.tabIndex = 0;
            panel.Focus();
        }
    }

    /// <summary>隐藏站长日志面板。</summary>
    public void Hide()
    {
        if (overlay != null) overlay.style.display = DisplayStyle.None;
    }

    // ================= 解锁绑定（供游戏循环调用） =================

    private const string CgKeyPrefix = "ArchiveCG_";

    /// <summary>解锁 CG（幂等）。</summary>
    public static void UnlockCG(string id)
    {
        PlayerPrefs.SetInt(CgKeyPrefix + id, 1);
        PlayerPrefs.Save();
    }

    /// <summary>解锁图鉴条目（角色/列车，幂等）。</summary>
    public static void UnlockArchive(string prefsKey)
    {
        PlayerPrefs.SetInt(prefsKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>按序章脚本名自动解锁对应 CG / 角色 / 列车。由 VNManager 在进入脚本时调用。</summary>
    public static void AutoUnlock(string scriptName)
    {
        switch (scriptName)
        {
            case "prologue_01_news":
                UnlockArchive("ArchiveChar_lin");
                UnlockArchive("ArchiveChar_laochen"); // 老陈来电
                break;
            case "prologue_02_day0":
                UnlockCG("cg_lab");
                UnlockArchive("ArchiveChar_suiyue"); // 领取 0721
                UnlockArchive("ArchiveTrain_nf5");
                break;
            case "prologue_04_arrival":
                UnlockCG("cg_sunset");
                break;
            case "prologue_05_inspection":
                UnlockCG("cg_bridge");
                break;
            case "prologue_06_team":
                UnlockCG("cg_team");
                UnlockArchive("ArchiveChar_zhanggong");
                UnlockArchive("ArchiveChar_liaiyi");
                UnlockArchive("ArchiveChar_wangxiaodi");
                UnlockArchive("ArchiveChar_zhaoshifu");
                UnlockArchive("ArchiveChar_xiaofang");
                break;
            case "prologue_07_first_repair":
                UnlockArchive("ArchiveTrain_nf5");
                break;
            case "prologue_08_first_run":
                UnlockCG("cg_first_run");
                UnlockArchive("ArchiveTrain_sy22");
                break;
        }
    }
}