using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>站长日志合集面板：成就 / CG鉴赏 / 音乐 / 故事 / 场景 / 图鉴 / 统计。
/// 独立于标题界面——自建 UI Document（DontDestroyOnLoad 单例），标题/VN/经营场景均可呼出。</summary>
public class TitleArchiveUI : MonoBehaviour
{
    /// <summary>全局单例（跨场景存活）。</summary>
    public static TitleArchiveUI Instance { get; private set; }

    /// <summary>面板关闭后触发（VN/经营场景订阅以恢复各自 BGM）。</summary>
    public event System.Action OnClosed;

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

    /// <summary>音乐鉴赏条目。</summary>
    private class MusicInfo
    {
        public string id;
        public string title;
        public string category;    // "BGM" 或 "歌曲"
        public string clipName;    // Resources/bgm 下的文件名（无扩展名）
        public string condition;
    }

    /// <summary>场景鉴赏条目。</summary>
    private class SceneInfo
    {
        public string id;
        public string title;
        public string condition;
        public string imagePath;   // Resources 路径（无扩展名），可为空
    }

    /// <summary>故事章节条目。</summary>
    private class ChapterInfo
    {
        public string id;
        public string title;
        public string summary;     // 章节简介
        public string scriptName;  // 回看时加载的 JSON 脚本
        public string condition;
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

    /* CG 清单：剧情高潮插画（Resources/cg/ 独立目录，区别于背景图 bg/）。
   生成进度：❌待生成 / ✅已绑图。剧情中 t:"cg" 条目展示即自动解锁。 */
    private static readonly CgInfo[] Cgs =
    {
        new CgInfo { id = "cg_day0_leave",   title = "启程·0721 升空",     condition = "启程时解锁",   imagePath = "cg/cg_day0_leave"   }, // ❌
        new CgInfo { id = "cg_tea_meet",     title = "初会·嘉颖徐",       condition = "会面嘉颖徐时解锁",   imagePath = "cg/cg_tea_meet"     }, // ❌
        new CgInfo { id = "cg_first_night",  title = "客舱·岁月初语",     condition = "启程首夜解锁",   imagePath = "cg/cg_first_night"  }, // ❌
        new CgInfo { id = "cg_chase",        title = "边境·三面合围",     condition = "边境危机时解锁",   imagePath = "cg/cg_chase"        }, // ❌
        new CgInfo { id = "cg_arrest",       title = "边境·引擎盖上",     condition = "边境被扣时解锁",   imagePath = "cg/cg_arrest"       }, // ❌
        new CgInfo { id = "cg_village",      title = "雾峰·初见黄昏",     condition = "抵达雾峰村时解锁",   imagePath = "cg/cg_village"      }, // ❌
        new CgInfo { id = "cg_team_night",   title = "旧人·灯下重逢",     condition = "团队集合时解锁",   imagePath = "cg/cg_team_night"   }, // ❌
        new CgInfo { id = "cg_first_run",    title = "首班车·驶离站台",   condition = "序章首班车",       imagePath = "cg/cg_first_run"    }, // ❌
        new CgInfo { id = "cg_museum",       title = "铁路博物馆",        condition = "好感度 > 90 解锁", imagePath = "cg/cg_museum"       }, // ❌
    };

    private static readonly ArchiveInfo[] Characters =
    {
        new ArchiveInfo { id = "lin",        name = "林彪悍", type = "见习站长",       intro = "25岁 · 金日成综合大学荣誉研究生，倔强、青涩但充满希望，继承爷爷的站长遗志。", condition = "序章开始解锁", prefsKey = "ArchiveChar_lin" },
        new ArchiveInfo { id = "laochen",    name = "老陈",   type = "最后一任站长",   intro = "68岁 · 雾峰村的最后一任站长，主角的导师，温暖、朴实、固执而善良。",       condition = "与老陈见面后解锁", prefsKey = "ArchiveChar_laochen" },
        new ArchiveInfo { id = "zhanggong",  name = "张工",   type = "退休机械工程师", intro = "62岁 · 维修精通（初始3级），乐观开朗。",                                condition = "序章员工集合", prefsKey = "ArchiveChar_zhanggong" },
        new ArchiveInfo { id = "liaiyi",     name = "李阿姨", type = "社区热心居民",   intro = "55岁 · 服务热心（初始2级），是车站最暖的人。",                          condition = "序章员工集合", prefsKey = "ArchiveChar_liaiyi" },
        new ArchiveInfo { id = "wangxiaodi", name = "王小弟", type = "毕业生学员",     intro = "22岁 · 刚毕业大学生，阳光热血，驾驶潜力最高（上限5级）。",              condition = "序章员工集合", prefsKey = "ArchiveChar_wangxiaodi" },
        new ArchiveInfo { id = "zhaoshifu",  name = "赵师傅", type = "退休铁路工程师", intro = "55岁 · 管理熟练（初始2级），沉稳可靠。",                                condition = "序章员工集合", prefsKey = "ArchiveChar_zhaoshifu" },
        new ArchiveInfo { id = "xiaofang",   name = "小芳",   type = "志愿者",         intro = "45岁 · 性格热情，服务潜力大（上限4级）。",                              condition = "序章员工集合", prefsKey = "ArchiveChar_xiaofang" },
        new ArchiveInfo { id = "suiyue",     name = "岁月",   type = "AI 原型",        intro = "0721号沙子飞猪号搭载的AI原型，2053年制造，沉睡23年，正经精确、偶尔冷幽默。", condition = "领取0721号载具后解锁", prefsKey = "ArchiveChar_suiyue" },
    };

    private static readonly ArchiveInfo[] Trains =
    {
        new ArchiveInfo { id = "nf5",  name = "NF-5 耕牛", type = "机车", intro = "柴油机车（原型东风4型货运内燃机车），最高速度80km/h，爷爷留下的老伙计。",      condition = "序章初始", prefsKey = "ArchiveTrain_nf5" },
        new ArchiveInfo { id = "sy22", name = "SY-22 灰雀", type = "客车", intro = "短途支线小型客车，可载客30人。",                                            condition = "序章初始", prefsKey = "ArchiveTrain_sy22" },
    };

    /// <summary>音乐鉴赏：13 首（BGM 9 + 歌曲 4），收录自 AI配乐提示词.md。</summary>
    private static readonly MusicInfo[] MusicEntries =
    {
        new MusicInfo { id = "iron_and_ash",    title = "Iron & Ash 铁与灰",          category = "BGM",  clipName = "iron_and_ash",    condition = "标题画面自动解锁" },
        new MusicInfo { id = "cloud_rail",      title = "Cloud-Rail 云轨",            category = "BGM",  clipName = "cloud_rail",      condition = "启程飞行途中解锁" },
        new MusicInfo { id = "embers",          title = "Embers 余烬",                category = "BGM",  clipName = "embers",          condition = "抵达雾峰站时解锁" },
        new MusicInfo { id = "night_cargo",     title = "Night Cargo 夜行货",         category = "BGM",  clipName = "night_cargo",     condition = "边境夜航时解锁" },
        new MusicInfo { id = "first_light",     title = "First Light 晨光",           category = "BGM",  clipName = "first_light",     condition = "飞越华北平原时解锁" },
        new MusicInfo { id = "platform",        title = "Platform 站台",              category = "BGM",  clipName = "platform",        condition = "到站后解锁" },
        new MusicInfo { id = "borderline",      title = "Borderline 国境线",          category = "BGM",  clipName = "borderline",      condition = "边境危机时解锁" },
        new MusicInfo { id = "wheels_joke",     title = "The Wheel's Joke 方向盘在笑", category = "BGM",  clipName = "wheels_joke",     condition = "统一便当店购物" },
        new MusicInfo { id = "train_through_keys", title = "Train Through Keys 旧曲", category = "BGM",  clipName = "train_through_keys", condition = "已废弃，保留试听" },
        new MusicInfo { id = "south_wind",      title = "남풍（南风）",               category = "歌曲", clipName = "south_wind",      condition = "统一便当店购物后解锁" },
        new MusicInfo { id = "starlit_rails",   title = "별빛 철길（星光铁轨）",      category = "歌曲", clipName = "starlit_rails",   condition = "边境危机时解锁夜航" },
        new MusicInfo { id = "chollima_ride",   title = "천리마 신시대에 달리다（千里马驰骋新时代）", category = "歌曲", clipName = "chollima_ride", condition = "边境途中播放新闻时解锁" },
        new MusicInfo { id = "sleepers",        title = "Sleepers（铁轨沉睡者）",     category = "歌曲", clipName = "sleepers",        condition = "片尾曲（待生成）" },
    };

    /// <summary>场景鉴赏：当前已有背景图的场景，随序章进度解锁。</summary>
    private static readonly SceneInfo[] SceneEntries =
    {
        new SceneInfo { id = "hangar",             title = "金日成综合大学停机坪",  condition = "领取0721号载具后解锁",         imagePath = "bg/hangar" },
        new SceneInfo { id = "lab",                title = "金日成综合大学实验室",   condition = "进入序章时解锁",        imagePath = "bg/lab" },
        new SceneInfo { id = "professor_office",   title = "导师办公室",             condition = "见导师后解锁",           imagePath = "bg/professor_office" },
        new SceneInfo { id = "tea_house",          title = "大同江茶馆·嘉颖徐办公室", condition = "会面嘉颖徐时解锁嘉颖徐",        imagePath = "bg/tea_house" },
        new SceneInfo { id = "car_interior",       title = "0721 驾驶舱",            condition = "领取0721号载具后解锁",         imagePath = "bg/car_interior" },
        new SceneInfo { id = "car_interior_night", title = "0721 驾驶舱·夜",          condition = "第二日夜航时解锁",             imagePath = "bg/car_interior_night" },
        new SceneInfo { id = "cabin_interior",     title = "0721 客舱",              condition = "领取0721号载具后解锁",         imagePath = "bg/cabin_interior" },
        new SceneInfo { id = "cabin_interior_night", title = "0721 客舱·夜",          condition = "边境夜航时解锁",             imagePath = "bg/cabin_interior_night" },
        new SceneInfo { id = "station",            title = "雾峰站",                 condition = "抵达雾峰村时解锁",             imagePath = "bg/station" },
    };

    /// <summary>故事章节回看：10 个序章脚本，解锁后随时重播。</summary>
    private static readonly ChapterInfo[] ChapterEntries =
    {
        new ChapterInfo { id = "prologue_01_news",   title = "第一章 · 广播里的时代",   summary = "2050 年沙能革命，世界第一辆沙能车的诞生，与铁路的黄昏。",                    scriptName = "prologue_01_news",   condition = "序章开始解锁" },
        new ChapterInfo { id = "prologue_02_day0",   title = "第二章 · 启程之日",       summary = "领取 0721 号，会面嘉颖徐，统一便当店采购，与岁月的初遇。",                    scriptName = "prologue_02_day0",   condition = "启程出发后解锁" },
        new ChapterInfo { id = "prologue_03_journey",title = "第三章 · 边境危机",       summary = "边境迷航，四家单位联合追捕，嘉颖徐一个电话化解十年刑期。",                   scriptName = "prologue_03_journey",condition = "穿越边境后解锁" },
        new ChapterInfo { id = "prologue_04_arrival",title = "第四章 · 抵达雾峰",       summary = "穿越群山，抵达雾峰村，看见爷爷留下的老站。",                                  scriptName = "prologue_04_arrival", condition = "抵达雾峰后解锁" },
        new ChapterInfo { id = "prologue_05_inspection", title = "第五章 · 线路巡视",   summary = "沿 23 公里线路巡视，评估爷爷留下的家底。",                                   scriptName = "prologue_05_inspection", condition = "到达后巡视线路" },
        new ChapterInfo { id = "prologue_06_team",   title = "第六章 · 旧人重逢",       summary = "张工、李阿姨、王小弟……老伙计们聚在车站，等一个回来的站长。",                 scriptName = "prologue_06_team",   condition = "与老员工见面后解锁" },
        new ChapterInfo { id = "prologue_07_first_repair", title = "第七章 · 第一次检修", summary = "NF-5 耕牛的喷油嘴修好了，铁路有救了。",                                     scriptName = "prologue_07_first_repair", condition = "完成车辆检修后解锁" },
        new ChapterInfo { id = "prologue_08_first_run", title = "第八章 · 首班车",      summary = "雾气里第一趟班车驶出站台，铁路重新有了心跳。",                              scriptName = "prologue_08_first_run", condition = "首发班车后解锁" },
        new ChapterInfo { id = "prologue_09_funding", title = "第九章 · 三条来路",      summary = "市里、乡亲、村民协会——三条资金来路摆在眼前。",                              scriptName = "prologue_09_funding", condition = "序章融资" },
        new ChapterInfo { id = "prologue_10_transition", title = "第十章 · 序章落幕",   summary = "一周后，铁路复兴之旅正式开启。",                                           scriptName = "prologue_10_transition", condition = "序章完成" },
    };

    private FontDefinition Fd() => new FontDefinition { font = gameFont };

    /// <summary>初始化（自建独立 UIDocument，无需外部传入）。由单例确保仅创建一次。</summary>
    public void Init()
    {
        if (uiDoc != null) return;
        BuildDocument();
        BuildUI();
    }

    private void BuildDocument()
    {
        // 自建 Canvas + UIDocument（与 VNManager 同样方式），独立于标题界面
        var canvasObj = new GameObject("ArchiveCanvas");
        DontDestroyOnLoad(canvasObj);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // 高于标题界面/VN

        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var panelSettings = Resources.Load<PanelSettings>("UI/TitleScreenPanelSettings");
        uiDoc = canvasObj.AddComponent<UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.visualTreeAsset = null;
        uiDoc.rootVisualElement.pickingMode = PickingMode.Ignore;
    }

    /// <summary>创建/获取全局单例（幂等）。任何场景调用即可。</summary>
    public static TitleArchiveUI EnsureInstance()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("TitleArchiveUI");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<TitleArchiveUI>();
        Instance.Init();
        return Instance;
    }

    private void BuildUI()
    {
        gameFont = Resources.Load<Font>("Fonts/zpix");
        AchievementManager.Initialize();
        var fontDef = Fd();
        var root = uiDoc.rootVisualElement;

        // ———— 全屏独立页面（非弹窗） ————
        overlay = new VisualElement { name = "archive-overlay" };
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0; overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0.08f, 0.05f, 0.03f, 1f); // 接近黑色，全屏无弹窗感
        overlay.style.display = DisplayStyle.None;
        overlay.RegisterCallback<ClickEvent>(evt =>
        {
            // 只有点击遮罩空白处才关闭
            if (ReferenceEquals(evt.target, overlay)) Hide();
        });
        root.Add(overlay);

        // ———— 全屏面板（独立页面，非弹窗） ————
        panel = new VisualElement { name = "archive-panel" };
        panel.style.flexDirection = FlexDirection.Column;
        panel.style.position = Position.Absolute;
        panel.style.top = 0; panel.style.left = 0; panel.style.right = 0; panel.style.bottom = 0;
        panel.style.backgroundColor = panelBg;
        panel.style.paddingLeft = 40; panel.style.paddingRight = 40;
        panel.style.paddingTop = 28; panel.style.paddingBottom = 28;
        overlay.Add(panel);

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
        AddTabButton(tabRow, "gallery", "CG鉴赏");
        AddTabButton(tabRow, "music", "音乐");
        AddTabButton(tabRow, "stories", "故事");
        AddTabButton(tabRow, "scenes", "场景");
        AddTabButton(tabRow, "collection", "图鉴");
        AddTabButton(tabRow, "bookmarks", "书签");
        AddTabButton(tabRow, "stats", "统计");

        // ———— 内容区 ————
        contentScroll = new ScrollView(ScrollViewMode.Vertical);
        contentScroll.name = "archive-scroll";
        contentScroll.style.flexGrow = 1;
        contentScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        panel.Add(contentScroll);

        // ———— 底部悬浮音乐播放器栏 ————
        BuildMusicPlayerBar();

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

    /// <summary>重建页面（每次 Show 时刷新解锁状态）。</summary>
    private void RebuildPages()
    {
        contentScroll.Clear();
        tabPages.Clear();
        BuildAchievementsPage();
        BuildGalleryPage();
        BuildMusicPage();
        BuildStoriesPage();
        BuildScenesPage();
        BuildCollectionPage();
        BuildStatsPage();
        BuildBookmarksPage();
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
        grid.style.justifyContent = Justify.Center;
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
        card.style.width = 380;
        card.style.height = 280;
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
                // 点击放大查看 CG
                art.pickingMode = PickingMode.Position;
                art.RegisterCallback<ClickEvent>(evt => ShowCgFullscreen(tex, cg.title));
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

    // ================= 音乐页 =================

    private string musicFilter = "全部";

    private void BuildMusicPage()
    {
        var page = new VisualElement { name = "page-music" };
        page.style.display = DisplayStyle.None;
        contentScroll.Add(page);
        tabPages["music"] = page;

        // 播放器栏在 BuildUI 时已创建，不在此处触发自动播放
        // ShowPlayerBar() 仅在用户切换到音乐页签时调用（见 ShowTab）

        var topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.alignItems = Align.Center;
        topRow.style.marginBottom = 10;
        page.Add(topRow);

        var tip = new Label("已解锁的音乐可以在这里随时播放");
        tip.style.fontSize = 17;
        tip.style.color = dimText;
        tip.style.unityFontDefinition = Fd();
        tip.style.flexGrow = 1;
        topRow.Add(tip);

        // 子分类：全部 / BGM / 歌曲
        var filterRow = new VisualElement();
        filterRow.style.flexDirection = FlexDirection.Row;
        page.Add(filterRow);

        foreach (var cat in new[] { "全部", "BGM", "歌曲" })
        {
            var btn = new Button(() => { musicFilter = cat; RebuildPages(); ShowTab("music"); }) { text = cat };
            btn.name = "music-filter-" + cat;
            btn.style.width = 76;
            btn.style.height = 34;
            btn.style.marginRight = 8;
            btn.style.fontSize = 17;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.unityFontDefinition = Fd();
            StylizeTab(btn);
            if (cat == musicFilter)
            {
                btn.style.backgroundColor = glassBgHover;
                btn.style.color = goldHover;
                btn.style.borderTopWidth = 2; btn.style.borderBottomWidth = 2;
                btn.style.borderLeftWidth = 2; btn.style.borderRightWidth = 2;
                btn.style.borderTopColor = borderHover; btn.style.borderBottomColor = borderHover;
                btn.style.borderLeftColor = borderHover; btn.style.borderRightColor = borderHover;
            }
            filterRow.Add(btn);
        }

        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        // 与上方筛选行（全部/BGM/歌曲）左边缘对齐
        grid.style.justifyContent = Justify.FlexStart;
        grid.style.marginTop = 12;
        page.Add(grid);

        for (int i = 0; i < MusicEntries.Length; i++)
        {
            if (musicFilter != "全部" && MusicEntries[i].category != musicFilter) continue;
            grid.Add(BuildMusicCard(MusicEntries[i]));
        }
    }

    private VisualElement BuildMusicCard(MusicInfo m)
    {
        bool unlocked = PlayerPrefs.GetInt("ArchiveMusic_" + m.id, 0) == 1;

        var card = new VisualElement();
        card.style.width = 360;
        card.style.flexShrink = 0;
        card.style.height = 158; // 固定高度：所有卡片同高，按钮底部对齐（长标题也不偏上）
        card.style.marginRight = 12;
        card.style.marginBottom = 12;
        card.style.paddingLeft = 12; card.style.paddingRight = 12;
        card.style.paddingTop = 10; card.style.paddingBottom = 10;
        card.style.backgroundColor = new Color(0.05f, 0.03f, 0.02f, 0.9f);
        card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
        card.style.borderTopColor = unlocked ? borderNormal : borderDim;
        card.style.borderBottomColor = unlocked ? borderNormal : borderDim;
        card.style.borderLeftColor = unlocked ? borderNormal : borderDim;
        card.style.borderRightColor = unlocked ? borderNormal : borderDim;
        card.style.borderTopLeftRadius = 6; card.style.borderTopRightRadius = 6;
        card.style.borderBottomLeftRadius = 6; card.style.borderBottomRightRadius = 6;

        var catRow = new VisualElement();
        catRow.style.flexDirection = FlexDirection.Row;
        catRow.style.alignItems = Align.Center;
        catRow.style.marginBottom = 6;
        card.Add(catRow);

        var catTag = new Label(m.category);
        catTag.style.width = 44;
        catTag.style.height = 22;
        catTag.style.fontSize = 14;
        catTag.style.unityTextAlign = TextAnchor.MiddleCenter;
        catTag.style.unityFontDefinition = Fd();
        catTag.style.color = m.category == "BGM" ? rarityRare : rarityEpic;
        catTag.style.borderTopWidth = 1; catTag.style.borderBottomWidth = 1;
        catTag.style.borderLeftWidth = 1; catTag.style.borderRightWidth = 1;
        catTag.style.borderTopColor = m.category == "BGM" ? rarityRare : rarityEpic;
        catTag.style.borderBottomColor = m.category == "BGM" ? rarityRare : rarityEpic;
        catTag.style.borderLeftColor = m.category == "BGM" ? rarityRare : rarityEpic;
        catTag.style.borderRightColor = m.category == "BGM" ? rarityRare : rarityEpic;
        catTag.style.borderTopLeftRadius = 4; catTag.style.borderTopRightRadius = 4;
        catTag.style.borderBottomLeftRadius = 4; catTag.style.borderBottomRightRadius = 4;
        catRow.Add(catTag);

        var status = new Label(unlocked ? "已解锁" : "未解锁");
        status.style.fontSize = 14;
        status.style.color = unlocked ? new Color(1f, 0.9f, 0.5f, 1f) : dimText;
        status.style.unityFontDefinition = Fd();
        status.style.marginLeft = new StyleLength(StyleKeyword.Auto);
        catRow.Add(status);

        var title = new Label(unlocked ? m.title : "???");
        title.style.fontSize = 18;
        title.style.color = unlocked ? new Color(1f, 1f, 1f, 0.95f) : grayText;
        title.style.unityFontDefinition = Fd();
        title.style.whiteSpace = WhiteSpace.Normal;
        card.Add(title);

        if (unlocked)
        {
            // 精简卡片：仅显示播放/停止按钮（进度/时间/音量在底部悬浮栏）
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.marginTop = new StyleLength(StyleKeyword.Auto); // 吸底，与所有卡片按钮底部对齐
            card.Add(btnRow);

            var playBtn = new Button(() => PlayArchiveMusic(m.id, m.clipName, m.title)) { text = "播放" };
            playBtn.name = "music-play-" + m.id;
            playBtn.style.width = 80;
            playBtn.style.height = 30;
            playBtn.style.fontSize = 15;
            playBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            playBtn.style.unityFontDefinition = Fd();
            StylizeTab(playBtn);
            btnRow.Add(playBtn);
        }
        else
        {
            var cond = new Label("解锁条件：" + m.condition);
            cond.style.fontSize = 14;
            cond.style.color = grayText;
            cond.style.unityFontDefinition = Fd();
            cond.style.marginTop = 6;
            cond.style.whiteSpace = WhiteSpace.Normal;
            card.Add(cond);
        }

        return card;
    }

    // ———— 底部悬浮音乐播放器 ————

    private VisualElement playerBar;
    private Label playerTitle;
    private Slider playerProgress;
    private Label playerCurTime;
    private Label playerTotalTime;
    private Button playerPrevBtn;
    private Button playerPlayBtn;
    private Button playerNextBtn;
    private Button playerStopBtn;
    private Button playerModeBtn;
    private AudioSource playerSource;
    private AudioClip playerClip;
    private string currentTrackId;
    private string currentTrackTitle;
    private const float ProgressUpdateInterval = 0.25f;
    private float progressTimer;
    private bool isPlayerPlaying;
    private enum PlayMode { SingleRepeat, Sequential, Shuffle }
    private PlayMode playMode = PlayMode.SingleRepeat;
    private bool isUserPlaylistMode; // true=用户选曲轮播, false=默认氛围曲单曲循环
    private readonly System.Collections.Generic.Dictionary<string, AudioClip> musicClipCache = new System.Collections.Generic.Dictionary<string, AudioClip>();
    private VisualElement titleOuter; // 长名滚动容器
    private Label playerToast;          // 播放器临时提示
    private float marqueeX;           // 当前滚动偏移
    private bool marqueeActive;       // 当前标题是否超宽需要滚动
    private float marqueeTextWidth;   // 文本实际渲染宽度
    private bool progressScrubbing;   // 用户正拖拽进度条（Update 不覆盖）

    private void BuildMusicPlayerBar()
    {
        playerBar = new VisualElement { name = "music-player-bar" };
        playerBar.style.flexDirection = FlexDirection.Row;
        playerBar.style.alignItems = Align.Center;
        playerBar.style.backgroundColor = new Color(0.15f, 0.10f, 0.06f, 0.97f);
        playerBar.style.borderTopWidth = 1;
        playerBar.style.borderTopColor = borderNormal;
        playerBar.style.paddingLeft = 20;
        playerBar.style.paddingRight = 20;
        playerBar.style.paddingTop = 8;
        playerBar.style.paddingBottom = 8;
        playerBar.style.marginTop = 8;
        playerBar.style.flexShrink = 0;
        playerBar.style.display = DisplayStyle.Flex;
        panel.Add(playerBar);

        // 曲名（带专辑图标装饰）
        var nameIcon = new Label("♪");
        nameIcon.style.fontSize = 20;
        nameIcon.style.color = goldNormal;
        nameIcon.style.unityFontDefinition = Fd();
        nameIcon.style.marginRight = 8;
        playerBar.Add(nameIcon);

        playerTitle = new Label("未播放");
        playerTitle.style.fontSize = 18;
        playerTitle.style.color = goldNormal;
        playerTitle.style.unityFontDefinition = Fd();
        // 宽度 Auto + 不收缩：允许文本真实宽度(超200时滚动的判定依据)
        playerTitle.style.width = StyleKeyword.Auto;
        playerTitle.style.flexShrink = 0;
        playerTitle.style.whiteSpace = WhiteSpace.NoWrap;
        playerTitle.style.overflow = Overflow.Hidden;
        // 居中防裁切：Label 撑满外层（高度对齐），文本垂直居中
        playerTitle.style.height = new Length(100, LengthUnit.Percent);
        playerTitle.style.unityTextAlign = TextAnchor.MiddleLeft;
        playerTitle.style.paddingTop = 0;
        playerTitle.style.paddingBottom = 0;
        // 长名横向滚动：内容套一层，用 translate 平移，超出时滚动
        titleOuter = new VisualElement();
        titleOuter.style.width = 200;
        titleOuter.style.height = 30;
        titleOuter.style.overflow = Overflow.Hidden;
        titleOuter.style.marginRight = 16;
        titleOuter.style.flexShrink = 0;
        titleOuter.style.alignItems = Align.Center;
        titleOuter.Add(playerTitle);
        playerBar.Add(titleOuter);

        // 进度条 + 时间（紧凑）
        playerCurTime = new Label("0:00");
        playerCurTime.style.fontSize = 13;
        playerCurTime.style.color = dimText;
        playerCurTime.style.unityFontDefinition = Fd();
        playerCurTime.style.width = 36;
        playerCurTime.style.marginRight = 6;
        playerBar.Add(playerCurTime);

        playerProgress = new Slider(0f, 1f);
        playerProgress.style.flexGrow = 1;
        playerProgress.style.height = 16;
        playerProgress.style.marginRight = 6;
        // 点击/拖动进度条跳转：PointerDown 手动换算点击位置 → 立即 seek，Change 实时跟随
        playerProgress.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (playerSource == null || playerClip == null) return;
            progressScrubbing = true;
            SeekFromPointer(evt);
        });
        playerProgress.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (progressScrubbing && playerSource != null && playerClip != null)
                SeekFromPointer(evt);
        });
        playerProgress.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (progressScrubbing && playerSource != null && playerClip != null)
                SeekFromPointer(evt);
            progressScrubbing = false;
        });
        playerProgress.RegisterValueChangedCallback(evt =>
        {
            if (progressScrubbing && playerSource != null && playerClip != null)
                playerSource.time = evt.newValue * playerClip.length;
        });
        playerBar.Add(playerProgress);

        playerTotalTime = new Label("0:00");
        playerTotalTime.style.fontSize = 13;
        playerTotalTime.style.color = dimText;
        playerTotalTime.style.unityFontDefinition = Fd();
        playerTotalTime.style.width = 36;
        playerTotalTime.style.marginRight = 16;
        playerBar.Add(playerTotalTime);

        // 控制按钮组（像素图标）
        playerPrevBtn = new Button(PlayPrevTrack) { text = "" };
        playerPrevBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.PrevIcon());
        playerPrevBtn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
        StylePlayerBtn(playerPrevBtn);
        playerBar.Add(playerPrevBtn);

        playerPlayBtn = new Button(TogglePlayPause) { text = "" };
        playerPlayBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.PlayIcon());
        playerPlayBtn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
        playerPlayBtn.style.width = 44;
        playerPlayBtn.style.height = 36;
        playerPlayBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        playerPlayBtn.style.unityFontDefinition = Fd();
        playerPlayBtn.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        playerPlayBtn.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        playerPlayBtn.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        playerPlayBtn.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        StylizeTab(playerPlayBtn);
        playerBar.Add(playerPlayBtn);

        playerNextBtn = new Button(PlayNextTrack) { text = "" };
        playerNextBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.NextIcon());
        playerNextBtn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
        StylePlayerBtn(playerNextBtn);
        playerBar.Add(playerNextBtn);

        // 循环模式切换（像素图标）
        playerModeBtn = new Button(TogglePlayMode) { text = "" };
        playerModeBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.SingleRepeatIcon());
        playerModeBtn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
        playerModeBtn.style.width = 44;
        playerModeBtn.style.height = 36;
        playerModeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        playerModeBtn.style.unityFontDefinition = Fd();
        playerModeBtn.style.marginLeft = 8;
        playerModeBtn.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        playerModeBtn.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        playerModeBtn.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        playerModeBtn.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        StylizeTab(playerModeBtn);
        playerBar.Add(playerModeBtn);
    }

    /// <summary>根据指针在滑块上的位置换算目标播放点并立即跳转（点击/拖动通用）。</summary>
    private void SeekFromPointer(IPointerEvent evt)
    {
        if (playerSource == null || playerClip == null) return;
        float trackW = playerProgress.resolvedStyle.width;
        if (trackW <= 0) return;
        float ratio = Mathf.Clamp01(evt.localPosition.x / trackW);
        playerProgress.value = ratio;
        playerSource.time = ratio * playerClip.length;
        playerCurTime.text = FormatTime(playerSource.time);
    }

    private void StylePlayerBtn(Button btn)
    {
        btn.style.width = 32;
        btn.style.height = 32;
        btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.unityFontDefinition = Fd();
        btn.style.marginLeft = 4;
        btn.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        btn.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        btn.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        btn.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        StylizeTab(btn);
    }

    private void TogglePlayPause()
    {
        if (playerSource == null || playerClip == null) return;
        if (playerSource.isPlaying)
        {
            playerSource.Pause();
            playerPlayBtn.text = "";
            playerPlayBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.PlayIcon());
            playerPlayBtn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
        }
        else
        {
            playerSource.UnPause();
            playerPlayBtn.text = "";
            playerPlayBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.PauseIcon());
            playerPlayBtn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
        }
    }

    private void PlayNextTrack()
    {
        if (!isUserPlaylistMode || string.IsNullOrEmpty(currentTrackId))
        {
            // 氛围模式：从头播
            AutoPlayFirstUnlockedMusic();
            return;
        }
        // 随机模式：随机选一首（非当前曲）
        if (playMode == PlayMode.Shuffle)
        {
            PlayRandomTrack();
            return;
        }
        // 顺序/单曲循环模式：找下一首已解锁的
        int idx = -1;
        for (int i = 0; i < MusicEntries.Length; i++)
            if (MusicEntries[i].id == currentTrackId) { idx = i; break; }
        for (int i = idx + 1; i < MusicEntries.Length; i++)
        {
            if (PlayerPrefs.GetInt("ArchiveMusic_" + MusicEntries[i].id, 0) == 1)
            {
                PlayArchiveMusic(MusicEntries[i].id, MusicEntries[i].clipName, MusicEntries[i].title);
                return;
            }
        }
        // 循环到第一首
        for (int i = 0; i < idx; i++)
        {
            if (PlayerPrefs.GetInt("ArchiveMusic_" + MusicEntries[i].id, 0) == 1)
            {
                PlayArchiveMusic(MusicEntries[i].id, MusicEntries[i].clipName, MusicEntries[i].title);
                return;
            }
        }
    }

    private void PlayPrevTrack()
    {
        if (!isUserPlaylistMode || string.IsNullOrEmpty(currentTrackId))
        {
            AutoPlayFirstUnlockedMusic();
            return;
        }
        // 随机模式：随机选一首（非当前曲）
        if (playMode == PlayMode.Shuffle)
        {
            PlayRandomTrack();
            return;
        }
        int idx = -1;
        for (int i = 0; i < MusicEntries.Length; i++)
            if (MusicEntries[i].id == currentTrackId) { idx = i; break; }
        for (int i = idx - 1; i >= 0; i--)
        {
            if (PlayerPrefs.GetInt("ArchiveMusic_" + MusicEntries[i].id, 0) == 1)
            {
                PlayArchiveMusic(MusicEntries[i].id, MusicEntries[i].clipName, MusicEntries[i].title);
                return;
            }
        }
        // 循环到尾
        for (int i = MusicEntries.Length - 1; i > idx; i--)
        {
            if (PlayerPrefs.GetInt("ArchiveMusic_" + MusicEntries[i].id, 0) == 1)
            {
                PlayArchiveMusic(MusicEntries[i].id, MusicEntries[i].clipName, MusicEntries[i].title);
                return;
            }
        }
    }

    /// <summary>随机选一首已解锁的音乐（随机模式下下一首/上一首）。</summary>
    private void PlayRandomTrack()
    {
        var unlocked = new System.Collections.Generic.List<int>();
        for (int i = 0; i < MusicEntries.Length; i++)
            if (PlayerPrefs.GetInt("ArchiveMusic_" + MusicEntries[i].id, 0) == 1)
                unlocked.Add(i);
        if (unlocked.Count == 0) { AutoPlayFirstUnlockedMusic(); return; }
        // 至少 2 首时才避免重复当前曲
        int pick = unlocked[Random.Range(0, unlocked.Count)];
        if (unlocked.Count > 1 && MusicEntries[pick].id == currentTrackId)
        {
            // 重抽一次
            var other = unlocked.Find(i => MusicEntries[i].id != currentTrackId);
            if (other != -1) pick = other;
        }
        PlayArchiveMusic(MusicEntries[pick].id, MusicEntries[pick].clipName, MusicEntries[pick].title);
    }

    private void TogglePlayMode()
    {
        playMode = (PlayMode)(((int)playMode + 1) % 3);
        playerModeBtn.text = "";
        switch (playMode)
        {
            case PlayMode.SingleRepeat:
                playerModeBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.SingleRepeatIcon());
                break;
            case PlayMode.Sequential:
                playerModeBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.SequentialIcon());
                break;
            case PlayMode.Shuffle:
                playerModeBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.ShuffleIcon());
                break;
        }
        playerModeBtn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
    }

    private void PlayArchiveMusic(string id, string clipName, string displayTitle = null)
    {
        // AudioClip 缓存：避免每次点击都同步 Resources.Load 卡顿
        AudioClip clip;
        if (!musicClipCache.TryGetValue(clipName, out clip))
        {
            clip = Resources.Load<AudioClip>("bgm/" + clipName);
            if (clip != null) musicClipCache[clipName] = clip;
        }
        if (clip == null)
        {
            ShowPlayerToast("音频文件缺失：" + clipName);
            return;
        }

        // 首次播放前异步预解码，避免 Play() 卡顿
        if (!clip.loadInBackground && clip.loadState != AudioDataLoadState.Loaded)
            clip.LoadAudioData();

        UnlockMusic(id);
        StopArchiveMusic();

        if (playerSource == null)
        {
            playerSource = gameObject.AddComponent<AudioSource>();
            playerSource.loop = false; // 轮播不循环
            playerSource.playOnAwake = false;
        }
        else
        {
            playerSource.loop = false;
        }

        // 检测是否用户手动选曲（非氛围自动）→ 进入轮播模式
        if (id != "platform" || isUserPlaylistMode)
        {
            isUserPlaylistMode = true;
            playerSource.loop = false;
        }
        else
        {
            isUserPlaylistMode = false;
            playerSource.loop = true; // 氛围曲单曲循环
        }

        playerSource.clip = clip;
        playerSource.volume = 0.5f;
        playerSource.Play();
        currentTrackId = id;
        currentTrackTitle = displayTitle ?? clipName;
        playerClip = clip;

        SetPlayerTitle(currentTrackTitle);
        playerTotalTime.text = FormatTime(clip.length);
        playerPlayBtn.text = "";
        playerPlayBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.PauseIcon());
        playerPlayBtn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
        playerBar.style.display = DisplayStyle.Flex;
        isPlayerPlaying = true;
    }

    /// <summary>显示播放器栏（自动播放氛围曲）。</summary>
    private void ShowPlayerBar()
    {
        playerBar.style.display = DisplayStyle.Flex;
        // 如果已在播放且处于用户轮播模式，不打断
        if (isPlayerPlaying && isUserPlaylistMode) return;
        // 否则重播氛围曲
        AutoPlayFirstUnlockedMusic();
    }

    private void AutoPlayFirstUnlockedMusic()
    {
        // 氛围模式：单曲循环播放 platform
        if (PlayerPrefs.GetInt("ArchiveMusic_platform", 0) == 1)
        {
            for (int i = 0; i < MusicEntries.Length; i++)
            {
                if (MusicEntries[i].id == "platform")
                {
                    PlayArchiveMusic("platform", MusicEntries[i].clipName, MusicEntries[i].title);
                    isUserPlaylistMode = false;
                    return;
                }
            }
        }
        // 兜底
        for (int i = 0; i < MusicEntries.Length; i++)
        {
            var m = MusicEntries[i];
            if (PlayerPrefs.GetInt("ArchiveMusic_" + m.id, 0) == 1)
            {
                PlayArchiveMusic(m.id, m.clipName, m.title);
                return;
            }
        }
    }

    private void StopArchiveMusic()
    {
        if (playerSource != null && playerSource.isPlaying)
            playerSource.Stop();
        SetPlayerTitle("未播放");
        playerProgress.value = 0;
        playerCurTime.text = "0:00";
        playerTotalTime.text = "0:00";
        playerPlayBtn.text = "";
        playerPlayBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.PlayIcon());
        playerPlayBtn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
        playerClip = null;
        currentTrackId = null;
        currentTrackTitle = null;
        isPlayerPlaying = false;
    }

    /// <summary>CG 全屏放大查看（点击任意处关闭）。</summary>
    private void ShowCgFullscreen(Texture2D tex, string title)
    {
        var fs = new VisualElement { name = "cg-fullscreen" };
        fs.style.position = Position.Absolute;
        fs.style.top = 0; fs.style.left = 0;
        fs.style.right = 0; fs.style.bottom = 0;
        fs.style.backgroundColor = new Color(0, 0, 0, 0.97f);
        fs.style.alignItems = Align.Center;
        fs.style.justifyContent = Justify.Center;
        fs.pickingMode = PickingMode.Position;
        fs.RegisterCallback<ClickEvent>(evt => fs.RemoveFromHierarchy());

        var img = new VisualElement();
        img.style.flexGrow = 1;
        img.style.maxWidth = new Length(100, LengthUnit.Percent);
        img.style.maxHeight = new Length(100, LengthUnit.Percent);
        img.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
        img.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
        fs.Add(img);

        var cap = new Label("「" + title + "」 — 点击关闭");
        cap.style.position = Position.Absolute;
        cap.style.bottom = 24;
        cap.style.left = 0;
        cap.style.right = 0;
        cap.style.fontSize = 16;
        cap.style.color = new Color(1f, 1f, 1f, 0.4f);
        cap.style.unityTextAlign = TextAnchor.MiddleCenter;
        cap.style.unityFontDefinition = Fd();
        fs.Add(cap);

        uiDoc.rootVisualElement.Add(fs);
    }

    private string FormatTime(float seconds)
    {
        if (float.IsNaN(seconds) || seconds < 0) seconds = 0;
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return m + ":" + s.ToString("D2");
    }

    private void Update()
    {
        // 长名横向滚动：每帧推进（仅超宽时）
        UpdateMarquee();

        if (!isPlayerPlaying || playerSource == null || !playerSource.isPlaying || playerClip == null) return;

        progressTimer += Time.unscaledDeltaTime;
        if (progressTimer < ProgressUpdateInterval) return;
        progressTimer = 0;

        float t = playerSource.time;
        float len = playerClip.length;
        if (len > 0.01f && !progressScrubbing)
            playerProgress.value = Mathf.Clamp01(t / len);
        playerCurTime.text = FormatTime(t);
    }

    /// <summary>长音乐名横向滚动：检测超宽后往复平移（暂停时停下）。</summary>
    private void UpdateMarquee()
    {
        if (playerTitle == null || titleOuter == null || !marqueeActive) return;
        // 播放时才滚动，暂停时冻结
        if (!isPlayerPlaying || playerSource == null || !playerSource.isPlaying)
            return;
        marqueeX -= 40f * Time.unscaledDeltaTime;
        // 文本滚出左侧后重置（循环）
        float w = titleOuter.resolvedStyle.width;
        if (w <= 0) w = 200;
        if (marqueeX < -marqueeTextWidth - 40)
            marqueeX = w + 40;
        playerTitle.style.translate = new Translate(marqueeX, 0);
    }

    /// <summary>播放器临时提示（短 Toast）。</summary>
    private void ShowPlayerToast(string text)
    {
        if (playerToast == null)
        {
            playerToast = new Label("");
            playerToast.style.position = Position.Absolute;
            playerToast.style.top = 46;
            playerToast.style.left = 0; playerToast.style.right = 0;
            playerToast.style.fontSize = 16;
            playerToast.style.color = new Color(1f, 0.85f, 0.5f, 1f);
            playerToast.style.unityTextAlign = TextAnchor.MiddleCenter;
            playerToast.style.unityFontDefinition = Fd();
            playerToast.pickingMode = PickingMode.Ignore;
            playerToast.style.display = DisplayStyle.None;
            uiDoc.rootVisualElement.Add(playerToast);
        }
        playerToast.text = text;
        playerToast.style.display = DisplayStyle.Flex;
        playerToast.schedule.Execute(() =>
        {
            if (playerToast != null) playerToast.style.display = DisplayStyle.None;
        }).ExecuteLater(2500);
    }

    /// <summary>设置曲名并检测是否超宽（超宽启用跑马灯）。</summary>
    private void SetPlayerTitle(string text)
    {
        playerTitle.text = text;
        marqueeX = 0;
        marqueeActive = false;
        playerTitle.style.translate = new Translate(0, 0);
        // 监听 Label 自身的 GeometryChanged：文本内容变化时 Label 宽度重算才触发
        playerTitle.UnregisterCallback<GeometryChangedEvent>(OnTitleLayoutMeasured);
        playerTitle.RegisterCallback<GeometryChangedEvent>(OnTitleLayoutMeasured);
        // 兜底：若 Label 布局已稳定（无变化事件），延迟再测一次
        playerTitle.schedule.Execute(OnTitleLayoutMeasuredResize).ExecuteLater(120);
    }

    private void OnTitleLayoutMeasuredResize()
    {
        if (playerTitle == null || titleOuter == null) return;
        MeasureTitleWidth();
    }

    private void OnTitleLayoutMeasured(GeometryChangedEvent evt)
    {
        MeasureTitleWidth();
    }

    private void MeasureTitleWidth()
    {
        if (playerTitle == null || titleOuter == null) return;
        // 用 MeasureTextSize 求文本真实宽度（Label width=Auto 时 resolvedStyle 可能是裁剪后值）
        var size = playerTitle.MeasureTextSize(playerTitle.text, 0, UnityEngine.UIElements.MeasureMode.Undefined, 18, UnityEngine.UIElements.MeasureMode.Undefined);
        float textW = size.x;
        float boxW = titleOuter.resolvedStyle.width;
        if (boxW <= 0) return;
        marqueeTextWidth = textW;
        marqueeActive = textW > boxW + 2f;
        if (marqueeActive)
            marqueeX = boxW + 40;
        else
            playerTitle.style.translate = new Translate(0, 0);
        playerTitle.UnregisterCallback<GeometryChangedEvent>(OnTitleLayoutMeasured);
    }

    // ================= 故事章节页 =================

    private void BuildStoriesPage()
    {
        var page = new VisualElement { name = "page-stories" };
        page.style.display = DisplayStyle.None;
        contentScroll.Add(page);
        tabPages["stories"] = page;

        var tip = new Label("已解锁的章节可以重新回看");
        tip.style.fontSize = 17;
        tip.style.color = dimText;
        tip.style.unityFontDefinition = Fd();
        tip.style.marginBottom = 12;
        page.Add(tip);

        for (int i = 0; i < ChapterEntries.Length; i++)
        {
            page.Add(BuildChapterCard(ChapterEntries[i]));
        }
    }

    private VisualElement BuildChapterCard(ChapterInfo ch)
    {
        bool unlocked = PlayerPrefs.GetInt("ArchiveStory_" + ch.id, 0) == 1;

        var card = new VisualElement();
        card.style.flexDirection = FlexDirection.Row;
        card.style.alignItems = Align.Center;
        card.style.paddingLeft = 16; card.style.paddingRight = 16;
        card.style.paddingTop = 10; card.style.paddingBottom = 10;
        card.style.marginBottom = 8;
        card.style.borderTopLeftRadius = 6; card.style.borderTopRightRadius = 6;
        card.style.borderBottomLeftRadius = 6; card.style.borderBottomRightRadius = 6;
        card.style.flexShrink = 0;
        card.style.backgroundColor = unlocked ? glassBg : new Color(0f, 0f, 0f, 0.25f);
        if (!unlocked)
        {
            card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor = borderDim; card.style.borderBottomColor = borderDim;
            card.style.borderLeftColor = borderDim; card.style.borderRightColor = borderDim;
        }

        var text = new VisualElement();
        text.style.flexGrow = 1;
        text.style.flexDirection = FlexDirection.Column;
        text.style.marginRight = 12;
        card.Add(text);

        var title = new Label(unlocked ? ch.title : "???");
        title.style.fontSize = 20;
        title.style.color = unlocked ? new Color(1f, 1f, 1f, 0.95f) : grayText;
        title.style.unityFontDefinition = Fd();
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        text.Add(title);

        if (unlocked)
        {
            var summary = new Label(ch.summary);
            summary.style.fontSize = 15;
            summary.style.color = new Color(1f, 1f, 1f, 0.6f);
            summary.style.unityFontDefinition = Fd();
            summary.style.marginTop = 4;
            summary.style.whiteSpace = WhiteSpace.Normal;
            text.Add(summary);

            var replayBtn = new Button(() => ReplayChapter(ch.scriptName)) { text = "回看" };
            replayBtn.style.width = 90;
            replayBtn.style.height = 34;
            replayBtn.style.fontSize = 16;
            replayBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            replayBtn.style.unityFontDefinition = Fd();
            StylizeTab(replayBtn);
            card.Add(replayBtn);
        }
        else
        {
            var cond = new Label("解锁条件：" + ch.condition);
            cond.style.fontSize = 14;
            cond.style.color = grayText;
            cond.style.unityFontDefinition = Fd();
            cond.style.marginTop = 4;
            text.Add(cond);
        }

        return card;
    }

    /// <summary>回看指定章节：设置回放标记并跳转 VN 场景。</summary>
    private void ReplayChapter(string scriptName)
    {
        PlayerPrefs.SetString("VN_ReplayScript", scriptName);
        PlayerPrefs.SetInt("VN_AutoLoad", 0);
        PlayerPrefs.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene("VN_Test");
    }

    // ================= 场景页 =================

    private void BuildScenesPage()
    {
        var page = new VisualElement { name = "page-scenes" };
        page.style.display = DisplayStyle.None;
        contentScroll.Add(page);
        tabPages["scenes"] = page;

        var tip = new Label("随剧情推进解锁的场景回忆");
        tip.style.fontSize = 17;
        tip.style.color = dimText;
        tip.style.unityFontDefinition = Fd();
        tip.style.marginBottom = 12;
        page.Add(tip);

        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        grid.style.justifyContent = Justify.Center;
        page.Add(grid);

        for (int i = 0; i < SceneEntries.Length; i++)
        {
            grid.Add(BuildSceneCard(SceneEntries[i]));
        }
    }

    private VisualElement BuildSceneCard(SceneInfo sc)
    {
        bool unlocked = PlayerPrefs.GetInt("ArchiveScene_" + sc.id, 0) == 1;

        var card = new VisualElement();
        card.style.width = 380;
        card.style.height = 280;
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

        var art = new VisualElement();
        art.style.flexGrow = 1;
        art.style.marginBottom = 8;
        art.style.overflow = Overflow.Hidden;
        art.style.backgroundColor = unlocked
            ? new Color(0.25f, 0.16f, 0.10f, 0.9f)
            : new Color(0.10f, 0.10f, 0.12f, 0.9f);
        card.Add(art);

        if (unlocked)
        {
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(sc.imagePath))
                tex = Resources.Load<Texture2D>(sc.imagePath);
            if (tex != null)
            {
                art.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
                art.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            }
            else
            {
                var placeholder = new Label("「" + sc.title + "」场景图待补充");
                placeholder.style.width = new Length(100, LengthUnit.Percent);
                placeholder.style.height = new Length(100, LengthUnit.Percent);
                placeholder.style.fontSize = 20;
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

            var cond = new Label("解锁条件：" + sc.condition);
            cond.style.fontSize = 14;
            cond.style.color = grayText;
            cond.style.unityTextAlign = TextAnchor.MiddleCenter;
            cond.style.unityFontDefinition = Fd();
            lockRow.Add(cond);
        }

        var titleRow = new VisualElement();
        titleRow.style.flexDirection = FlexDirection.Row;
        titleRow.style.alignItems = Align.Center;
        card.Add(titleRow);

        var title = new Label(sc.title);
        title.style.fontSize = 17;
        title.style.color = unlocked ? goldNormal : grayText;
        title.style.unityFontDefinition = Fd();
        title.style.flexGrow = 1;
        title.style.whiteSpace = WhiteSpace.Normal;
        titleRow.Add(title);

        var status = new Label(unlocked ? "已解锁" : "未解锁");
        status.style.fontSize = 14;
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

    // ================= 统计页 =================

    private void BuildStatsPage()
    {
        var page = new VisualElement { name = "page-stats" };
        page.style.display = DisplayStyle.None;
        contentScroll.Add(page);
        tabPages["stats"] = page;

        var header = new Label("运营统计");
        header.style.fontSize = 20;
        header.style.color = goldNormal;
        header.style.unityFontDefinition = Fd();
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.marginTop = 6;
        header.style.marginBottom = 10;
        page.Add(header);

        AddStatRow(page, "累计游戏时长", StatsManager.FormatPlayTime(StatsManager.TotalPlaySeconds));
        AddStatRow(page, "累计运营天数", StatsManager.MaxDay + " 天");
        AddStatRow(page, "累计客运量", StatsManager.TotalPassengers + " 人次");
        AddStatRow(page, "累计运营收入", StatsManager.TotalRevenue + " 沙币");
        AddStatRow(page, "其中政府补贴", StatsManager.TotalSubsidy + " 沙币");
        AddStatRow(page, "累计运营支出", StatsManager.TotalExpense + " 沙币");
        long net = StatsManager.TotalRevenue - StatsManager.TotalExpense;
        AddStatRow(page, "累计净利润", (net >= 0 ? "+" : "-") + System.Math.Abs(net) + " 沙币",
            net >= 0 ? new Color(0.5f, 1f, 0.5f, 1f) : new Color(1f, 0.5f, 0.5f, 1f));

        var liveHeader = new Label("当前状态");
        liveHeader.style.fontSize = 20;
        liveHeader.style.color = goldNormal;
        liveHeader.style.unityFontDefinition = Fd();
        liveHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        liveHeader.style.marginTop = 18;
        liveHeader.style.marginBottom = 10;
        page.Add(liveHeader);

        AddStatRow(page, "当前运营天数", GameData.Day + " 天");
        AddStatRow(page, "当前资金", GameData.Money + " 沙币");
        AddStatRow(page, "当前信任", GameData.Trust + "%");
        AddStatRow(page, "机车状态", GameData.TrainCondition + "%");
        AddStatRow(page, "预期客流", GameData.ExpectedPassengers + " 人/日");

        var tip = new Label("统计每 5 秒自动保存，累计数据跨会话保留。");
        tip.style.fontSize = 14;
        tip.style.color = dimText;
        tip.style.unityFontDefinition = Fd();
        tip.style.marginTop = 16;
        page.Add(tip);
    }

    private void AddStatRow(VisualElement parent, string label, string value, Color? valueColor = null)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.paddingLeft = 16; row.style.paddingRight = 16;
        row.style.paddingTop = 8; row.style.paddingBottom = 8;
        row.style.marginBottom = 6;
        row.style.backgroundColor = glassBg;
        row.style.borderTopLeftRadius = 6; row.style.borderTopRightRadius = 6;
        row.style.borderBottomLeftRadius = 6; row.style.borderBottomRightRadius = 6;

        var lbl = new Label(label);
        lbl.style.fontSize = 19;
        lbl.style.color = new Color(1f, 1f, 1f, 0.8f);
        lbl.style.unityFontDefinition = Fd();
        lbl.style.flexGrow = 1;
        row.Add(lbl);

        var val = new Label(value);
        val.style.fontSize = 19;
        val.style.color = valueColor ?? new Color(1f, 0.92f, 0.7f, 0.95f);
        val.style.unityFontDefinition = Fd();
        val.style.unityFontStyleAndWeight = FontStyle.Bold;
        row.Add(val);

        parent.Add(row);
    }

    // ================= 书签页 =================

    private void BuildBookmarksPage()
    {
        var page = new VisualElement { name = "page-bookmarks" };
        page.style.display = DisplayStyle.None;
        contentScroll.Add(page);
        tabPages["bookmarks"] = page;

        var tip = new Label("自动书签随阅读推进，话完成自动清理；手动书签永久保留，点击跳转回看");
        tip.style.fontSize = 16;
        tip.style.color = dimText;
        tip.style.unityFontDefinition = Fd();
        tip.style.marginBottom = 12;
        page.Add(tip);

        // —— 自动书签 ——
        var autoHeader = new Label("自动书签");
        autoHeader.style.fontSize = 20;
        autoHeader.style.color = goldNormal;
        autoHeader.style.unityFontDefinition = Fd();
        autoHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        autoHeader.style.marginTop = 6;
        autoHeader.style.marginBottom = 8;
        page.Add(autoHeader);

        var autoList = BookmarkManager.GetAllAuto();
        if (autoList.Count == 0)
        {
            var empty = new Label("暂无自动书签");
            empty.style.fontSize = 16;
            empty.style.color = dimText;
            empty.style.unityFontDefinition = Fd();
            empty.style.marginBottom = 10;
            page.Add(empty);
        }
        else
        {
            for (int i = 0; i < autoList.Count; i++)
                page.Add(BuildBookmarkCard(autoList[i]));
        }

        // —— 手动书签 ——
        var manualHeader = new Label("手动书签");
        manualHeader.style.fontSize = 20;
        manualHeader.style.color = goldNormal;
        manualHeader.style.unityFontDefinition = Fd();
        manualHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        manualHeader.style.marginTop = 18;
        manualHeader.style.marginBottom = 8;
        page.Add(manualHeader);

        var manualList = BookmarkManager.GetAllManual();
        if (manualList.Count == 0)
        {
            var empty2 = new Label("在剧情中打开 Menu → ◉ 可添加书签");
            empty2.style.fontSize = 16;
            empty2.style.color = dimText;
            empty2.style.unityFontDefinition = Fd();
            page.Add(empty2);
        }
        else
        {
            for (int i = 0; i < manualList.Count; i++)
                page.Add(BuildBookmarkCard(manualList[i]));
        }
    }

    private VisualElement BuildBookmarkCard(BookmarkManager.Bookmark bm)
    {
        var card = new VisualElement();
        card.style.flexDirection = FlexDirection.Row;
        card.style.alignItems = Align.Center;
        card.style.paddingLeft = 14; card.style.paddingRight = 14;
        card.style.paddingTop = 10; card.style.paddingBottom = 10;
        card.style.marginBottom = 8;
        card.style.backgroundColor = bm.isAuto ? glassBg : new Color(0.16f, 0.11f, 0.07f, 0.8f);
        card.style.borderTopLeftRadius = 6; card.style.borderTopRightRadius = 6;
        card.style.borderBottomLeftRadius = 6; card.style.borderBottomRightRadius = 6;
        card.style.flexShrink = 0;

        // 类型标签
        var tag = new Label(bm.isAuto ? "自动" : "手动");
        tag.style.width = 40;
        tag.style.height = 22;
        tag.style.fontSize = 13;
        tag.style.unityTextAlign = TextAnchor.MiddleCenter;
        tag.style.unityFontDefinition = Fd();
        tag.style.color = bm.isAuto ? new Color(0.5f, 0.85f, 0.4f, 1f) : new Color(1f, 0.8f, 0.35f, 1f);
        tag.style.borderTopWidth = 1; tag.style.borderBottomWidth = 1;
        tag.style.borderLeftWidth = 1; tag.style.borderRightWidth = 1;
        tag.style.borderTopColor = tag.style.color;
        tag.style.borderBottomColor = tag.style.color;
        tag.style.borderLeftColor = tag.style.color;
        tag.style.borderRightColor = tag.style.color;
        tag.style.borderTopLeftRadius = 4; tag.style.borderTopRightRadius = 4;
        tag.style.borderBottomLeftRadius = 4; tag.style.borderBottomRightRadius = 4;
        tag.style.marginRight = 12;
        card.Add(tag);

        // 文本
        var textCol = new VisualElement();
        textCol.style.flexGrow = 1;
        card.Add(textCol);

        var name = new Label(bm.name);
        name.style.fontSize = 18;
        name.style.color = new Color(1f, 1f, 1f, 0.95f);
        name.style.unityFontDefinition = Fd();
        name.style.marginBottom = 3;
        textCol.Add(name);

        var preview = new Label(bm.previewText ?? "");
        preview.style.fontSize = 14;
        preview.style.color = new Color(0.7f, 0.7f, 0.65f, 0.8f);
        preview.style.unityFontDefinition = Fd();
        preview.style.whiteSpace = WhiteSpace.Normal;
        textCol.Add(preview);

        // 操作：跳转 / 删除
        if (bm.isAuto && bm.isCompleted)
        {
            var done = new Label("已完成");
            done.style.fontSize = 14;
            done.style.color = new Color(0.5f, 0.8f, 0.45f, 0.9f);
            done.style.unityFontDefinition = Fd();
            done.style.marginLeft = 8;
            card.Add(done);
        }
        else
        {
            var goBtn = new Button(() => { BookmarkManager.JumpToBookmark(bm); UnityEngine.SceneManagement.SceneManager.LoadScene("VN_Test"); }) { text = "跳转" };
            goBtn.style.width = 64; goBtn.style.height = 30;
            goBtn.style.fontSize = 15; goBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            goBtn.style.unityFontDefinition = Fd();
            goBtn.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.8f);
            goBtn.style.color = new Color(0.8f, 0.9f, 1f, 1f);
            goBtn.style.marginLeft = 8;
            card.Add(goBtn);
        }

        if (!bm.isAuto)
        {
            var delBtn = new Button(() => { BookmarkManager.RemoveManual(bm.id); RebuildPages(); ShowTab("bookmarks"); }) { text = "删" };
            delBtn.style.width = 44; delBtn.style.height = 30;
            delBtn.style.fontSize = 15; delBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            delBtn.style.unityFontDefinition = Fd();
            delBtn.style.backgroundColor = new Color(0.4f, 0.2f, 0.2f, 0.6f);
            delBtn.style.color = new Color(1f, 0.6f, 0.6f, 1f);
            delBtn.style.marginLeft = 6;
            card.Add(delBtn);
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

        // 播放器栏仅在音乐页签显示
        if (key == "music")
        {
            playerBar.style.display = DisplayStyle.Flex;
            if (!isPlayerPlaying)
                PlayArchiveAmbient();
        }
        else
        {
            playerBar.style.display = DisplayStyle.None;
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

    /// <summary>显示站长日志面板（重建页面以刷新解锁状态；入场播放舒缓 BGM）。</summary>
    public void Show()
    {
        if (overlay == null) return;

        // 隐藏标题界面根（共享 PanelSettings 时的混显/残留）
        HideTitleRoot();

        RebuildPages();
        ShowTab(currentTabKey);

        overlay.style.display = DisplayStyle.Flex;
        if (panel != null)
        {
            panel.tabIndex = 0;
            panel.Focus();
        }

        // ESC 关闭
        RegisterEscHandler();

        // 暂停全局 BGM，通过 playerBar 播放氛围曲
        PauseGlobalBGM();
        if (!isPlayerPlaying)
            PlayArchiveAmbient();
    }

    private VisualElement savedTitleRoot;
    private static bool titleRootHidden;

    /// <summary>隐藏标题界面 UIDocument 根（仅当存在于当前场景时）。</summary>
    private void HideTitleRoot()
    {
        if (titleRootHidden) return;
        var docs = Resources.FindObjectsOfTypeAll<UIDocument>();
        foreach (var doc in docs)
        {
            if (doc == null || doc.rootVisualElement == null) continue;
            if (doc.rootVisualElement.Q<Button>("btn-archive") != null)
            {
                savedTitleRoot = doc.rootVisualElement;
                savedTitleRoot.style.display = DisplayStyle.None;
                titleRootHidden = true;
                break;
            }
        }
    }

    private void RestoreTitleRoot()
    {
        if (titleRootHidden && savedTitleRoot != null)
        {
            savedTitleRoot.style.display = DisplayStyle.Flex;
            savedTitleRoot = null;
            titleRootHidden = false;
        }
    }

    /// <summary>场景切换时清理悬挂的标题 root 引用（防止跨场景恢复已销毁元素/静态状态残留）。</summary>
    private void OnDestroy()
    {
        savedTitleRoot = null;
        titleRootHidden = false;
        if (musicClipCache != null) musicClipCache.Clear();
    }

    /// <summary>ESC 键关闭面板。</summary>
    private void RegisterEscHandler()
    {
        // 防止重复注册
        uiDoc.rootVisualElement.UnregisterCallback<KeyDownEvent>(OnEscKey);
        uiDoc.rootVisualElement.RegisterCallback<KeyDownEvent>(OnEscKey);
    }

    private void OnEscKey(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Escape && overlay.style.display == DisplayStyle.Flex)
        {
            Hide();
            evt.StopPropagation();
        }
    }

    /// <summary>隐藏站长日志面板。</summary>
    public void Hide()
    {
        if (overlay != null) overlay.style.display = DisplayStyle.None;
        uiDoc.rootVisualElement.UnregisterCallback<KeyDownEvent>(OnEscKey);
        // 恢复被隐藏的标题界面根
        RestoreTitleRoot();
        // 始终停止播放器音乐（防止音乐被带到标题界面）
        playerBar.style.display = DisplayStyle.None;
        StopArchiveMusic();
        UnpauseGlobalBGM();
        OnClosed?.Invoke();
    }

    // ———— 全局 BGM 暂停/恢复 + 氛围曲播放 ————

    private const string ArchiveAmbientClip = "platform";
    private AudioSource bgSaver;

    private void PauseGlobalBGM()
    {
        var existing = GameObject.Find("BGM");
        if (existing != null)
        {
            var src = existing.GetComponent<AudioSource>();
            if (src != null && src.isPlaying)
            {
                bgSaver = src;
                src.Pause();
            }
        }
    }

    private void UnpauseGlobalBGM()
    {
        if (bgSaver != null)
        {
            bgSaver.UnPause();
            bgSaver = null;
        }
    }

    /// <summary>通过 playerBar 播放氛围曲（单曲循环，进度条更新）。</summary>
    private void PlayArchiveAmbient()
    {
        AudioClip clip;
        if (!musicClipCache.TryGetValue(ArchiveAmbientClip, out clip))
        {
            clip = Resources.Load<AudioClip>("bgm/" + ArchiveAmbientClip);
            if (clip != null) musicClipCache[ArchiveAmbientClip] = clip;
        }
        if (clip == null) return;

        if (playerSource == null)
        {
            playerSource = gameObject.AddComponent<AudioSource>();
            playerSource.playOnAwake = false;
        }
        if (playerSource.clip == clip && playerSource.isPlaying) return;

        playerSource.loop = true;
        isUserPlaylistMode = false;
        playerSource.clip = clip;
        playerSource.volume = 0.5f;
        playerSource.Play();
        isPlayerPlaying = true;
        playerClip = clip;
        currentTrackTitle = "Platform 站台";
        currentTrackId = "platform";
        SetPlayerTitle(currentTrackTitle);
        playerTotalTime.text = FormatTime(clip.length);
        playerPlayBtn.text = "";
        playerPlayBtn.style.backgroundImage = new StyleBackground(PixelIconHelper.PauseIcon());
        playerPlayBtn.style.unityBackgroundImageTintColor = new Color(1f, 0.86f, 0.59f, 0.9f);
        // 不在此设置 playerBar 可见性——由 ShowTab 根据当前页签控制
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
                UnlockMusic("iron_and_ash");
                UnlockStory("prologue_01_news");
                break;
            case "prologue_02_day0":
                UnlockArchive("ArchiveChar_suiyue"); // 领取 0721
                UnlockArchive("ArchiveTrain_nf5");
                UnlockMusic("cloud_rail");
                UnlockMusic("wheels_joke");
                UnlockMusic("south_wind");
                UnlockScene("hangar");
                UnlockScene("lab");
                UnlockScene("professor_office");
                UnlockScene("tea_house");
                UnlockScene("car_interior");
                UnlockScene("cabin_interior");
                UnlockStory("prologue_02_day0");
                break;
            case "prologue_03_journey":
                UnlockMusic("borderline");
                UnlockMusic("starlit_rails");
                UnlockMusic("chollima_ride");
                UnlockScene("car_interior_night");
                UnlockScene("cabin_interior_night");
                UnlockStory("prologue_03_journey");
                break;
            case "prologue_04_arrival":
                UnlockMusic("embers");
                UnlockMusic("platform");
                UnlockScene("station");
                UnlockStory("prologue_04_arrival");
                break;
            case "prologue_05_inspection":
                UnlockStory("prologue_05_inspection");
                break;
            case "prologue_06_team":
                UnlockArchive("ArchiveChar_zhanggong");
                UnlockArchive("ArchiveChar_liaiyi");
                UnlockArchive("ArchiveChar_wangxiaodi");
                UnlockArchive("ArchiveChar_zhaoshifu");
                UnlockArchive("ArchiveChar_xiaofang");
                UnlockStory("prologue_06_team");
                break;
            case "prologue_07_first_repair":
                UnlockArchive("ArchiveTrain_nf5");
                UnlockStory("prologue_07_first_repair");
                break;
            case "prologue_08_first_run":
                UnlockArchive("ArchiveTrain_sy22");
                UnlockMusic("first_light");
                UnlockStory("prologue_08_first_run");
                break;
            case "prologue_09_funding":
                UnlockStory("prologue_09_funding");
                break;
            case "prologue_10_transition":
                UnlockStory("prologue_10_transition");
                break;
        }
    }

    /// <summary>本次会话新增解锁项（供 VN 话结束时奖励弹窗消费）。由 UnlockXX 方法在首次解锁时填充。</summary>
    private static readonly List<string> pendingUnlocks = new List<string>();

    /// <summary>取走并清空新增解锁清单（奖励弹窗用）。</summary>
    public static string[] TakePendingUnlocks()
    {
        var result = pendingUnlocks.ToArray();
        pendingUnlocks.Clear();
        return result;
    }

    private static void MarkNewUnlock(string label)
    {
        if (!pendingUnlocks.Contains(label))
            pendingUnlocks.Add(label);
    }

    /// <summary>解锁音乐（幂等；首次解锁记录供奖励弹窗）。</summary>
    public static void UnlockMusic(string id)
    {
        if (PlayerPrefs.GetInt("ArchiveMusic_" + id, 0) == 1) return;
        PlayerPrefs.SetInt("ArchiveMusic_" + id, 1);
        PlayerPrefs.Save();
        MarkNewUnlock("音乐 · " + id);
    }

    /// <summary>解锁场景（幂等；首次解锁记录）。</summary>
    public static void UnlockScene(string id)
    {
        if (PlayerPrefs.GetInt("ArchiveScene_" + id, 0) == 1) return;
        PlayerPrefs.SetInt("ArchiveScene_" + id, 1);
        PlayerPrefs.Save();
        MarkNewUnlock("场景 · " + id);
    }

    /// <summary>解锁故事章节（幂等；首次解锁记录）。</summary>
    public static void UnlockStory(string id)
    {
        if (PlayerPrefs.GetInt("ArchiveStory_" + id, 0) == 1) return;
        PlayerPrefs.SetInt("ArchiveStory_" + id, 1);
        PlayerPrefs.Save();
        MarkNewUnlock("章节 · " + id);
    }
}