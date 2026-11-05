using System;

public static class StoryArchiveCatalog
{
    public sealed class CgInfo
    {
        public string id;
        public string title;
        public string condition;
        public string imagePath; // Resources 路径（无扩展名），可为空
    }
    public sealed class ArchiveInfo
    {
        public string id;
        public string name;
        public string type;   // 职务/分类
        public string intro;  // 简介
        public string condition;
        public string prefsKey;
    }
    public sealed class MusicInfo
    {
        public string id;
        public string title;
        public string category;    // "BGM" 或 "歌曲"
        public string clipName;    // Resources/bgm 下的文件名（无扩展名）
        public string condition;
    }
    public sealed class SceneInfo
    {
        public string id;
        public string title;
        public string condition;
        public string imagePath;   // Resources 路径（无扩展名），可为空
    }
    public sealed class ChapterInfo
    {
        public string id;
        public string title;
        public string summary;     // 章节简介
        public string scriptName;  // 回看时加载的 JSON 脚本
        public string condition;
    }
    public static readonly CgInfo[] Cgs =
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
    public static readonly ArchiveInfo[] Characters =
    {
        new ArchiveInfo { id = "daoshi", name = "佯明远", type = "智能调度教授", intro = "金日成综合大学智能调度课题负责人，林彪悍的研究生指导教师。", condition = "序章会见佯教授", prefsKey = "ArchiveChar_daoshi" },
        new ArchiveInfo { id = "lin",        name = "林彪悍", type = "见习站长",       intro = "25岁 · 金日成综合大学荣誉研究生，倔强、青涩但充满希望，继承爷爷的站长遗志。", condition = "序章开始解锁", prefsKey = "ArchiveChar_lin" },
        new ArchiveInfo { id = "laochen",    name = "老陈",   type = "最后一任站长",   intro = "68岁 · 雾峰村的最后一任站长，主角的铁路前辈，温暖、朴实、固执而善良。",       condition = "与老陈见面后解锁", prefsKey = "ArchiveChar_laochen" },
        new ArchiveInfo { id = "zhanggong",  name = "张工",   type = "退休机械工程师", intro = "62岁 · 维修精通（初始3级），乐观开朗。",                                condition = "序章员工集合", prefsKey = "ArchiveChar_zhanggong" },
        new ArchiveInfo { id = "liaiyi",     name = "李阿姨", type = "社区热心居民",   intro = "55岁 · 服务热心（初始2级），是车站最暖的人。",                          condition = "序章员工集合", prefsKey = "ArchiveChar_liaiyi" },
        new ArchiveInfo { id = "wangxiaodi", name = "王小弟", type = "毕业生学员",     intro = "22岁 · 刚毕业大学生，阳光热血，驾驶潜力最高（上限5级）。",              condition = "序章员工集合", prefsKey = "ArchiveChar_wangxiaodi" },
        new ArchiveInfo { id = "zhaoshifu",  name = "赵师傅", type = "退休铁路工程师", intro = "55岁 · 管理熟练（初始2级），沉稳可靠。",                                condition = "序章员工集合", prefsKey = "ArchiveChar_zhaoshifu" },
        new ArchiveInfo { id = "xiaofang",   name = "小芳",   type = "志愿者",         intro = "45岁 · 性格热情，服务潜力大（上限4级）。",                              condition = "序章员工集合", prefsKey = "ArchiveChar_xiaofang" },
        new ArchiveInfo { id = "suiyue",     name = "岁月",   type = "AI 原型",        intro = "0721号沙子飞猪号搭载的AI原型，2053年制造，沉睡23年，正经精确、偶尔冷幽默。", condition = "领取0721号载具后解锁", prefsKey = "ArchiveChar_suiyue" },
        new ArchiveInfo { id = "jiaying",    name = "嘉颖徐", type = "铁路大亨",       intro = "24岁 · 东北亚铁路圈天才少女，手握多条电气化干线，爷爷林悍晚年忘年交。",     condition = "会面嘉颖徐时解锁",   prefsKey = "ArchiveChar_jiaying" },
    };
    public static readonly ArchiveInfo[] Trains =
    {
        new ArchiveInfo { id = "nf5",  name = "NF-5K 耕牛", type = "机车", intro = "雾峰村线的内燃动力头车，兼顾驾驶与客运空间，重开前需要完成检修。",      condition = "序章初始", prefsKey = "ArchiveTrain_nf5" },
        new ArchiveInfo { id = "sy22", name = "SY-22 灰雀", type = "客车", intro = "短途支线小型客车，可载客30人。",                                            condition = "序章初始", prefsKey = "ArchiveTrain_sy22" },
    };
    public static readonly MusicInfo[] MusicEntries =
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
        new MusicInfo { id = "south_wind",      title = "남풍 南风",                  category = "歌曲", clipName = "south_wind",      condition = "统一便当店购物后解锁" },
        new MusicInfo { id = "starlit_rails",   title = "별빛 철길 星光铁轨",         category = "歌曲", clipName = "starlit_rails",   condition = "边境危机时解锁夜航" },
        new MusicInfo { id = "chollima_ride",   title = "천리마 신시대에 달리다 千里马驰骋新时代", category = "歌曲", clipName = "chollima_ride", condition = "边境途中播放新闻时解锁" },
        new MusicInfo { id = "sleepers",        title = "Sleepers 铁轨沉睡者",        category = "歌曲", clipName = "sleepers",        condition = "片尾曲（待生成）" },
    };
    public static readonly SceneInfo[] SceneEntries =
    {
        new SceneInfo { id = "hangar",             title = "金日成综合大学停机坪",  condition = "领取0721号载具后解锁",         imagePath = "bg/hangar" },
        new SceneInfo { id = "lab",                title = "金日成综合大学实验室",   condition = "进入序章时解锁",        imagePath = "bg/lab" },
        new SceneInfo { id = "professor_office",   title = "佯教授办公室",             condition = "会见佯教授后解锁",           imagePath = "bg/professor_office" },
        new SceneInfo { id = "tea_house",          title = "大同江茶馆·嘉颖徐办公室", condition = "会面嘉颖徐时解锁嘉颖徐",        imagePath = "bg/tea_house" },
        new SceneInfo { id = "car_interior",       title = "0721 驾驶舱",            condition = "领取0721号载具后解锁",         imagePath = "bg/car_interior" },
        new SceneInfo { id = "car_interior_night", title = "0721 驾驶舱·夜",          condition = "第二日夜航时解锁",             imagePath = "bg/car_interior_night" },
        new SceneInfo { id = "cabin_interior",     title = "0721 客舱",              condition = "领取0721号载具后解锁",         imagePath = "bg/cabin_interior" },
        new SceneInfo { id = "cabin_interior_night", title = "0721 客舱·夜",          condition = "边境夜航时解锁",             imagePath = "bg/cabin_interior_night" },
        new SceneInfo { id = "station",            title = "雾峰站",                 condition = "抵达雾峰村时解锁",             imagePath = "bg/station" },
    };
    public static readonly ChapterInfo[] ChapterEntries =
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
}
