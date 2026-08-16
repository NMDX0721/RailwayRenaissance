using System;
using System.Collections.Generic;
using UnityEngine;
using RailwayRenaissance.Core;

[Serializable]
public class CrewMember
{
    public string id;
    public string name;
    public int age;
    public string role;
    public int fatigue;
    public float loyalty;
    public SkillData[] skills;
    public SkillTreeNode[] skillTree;

    // === 隐藏属性 ===
    public float hiddenTalent;        // 0-100
    public float hiddenAmbition;      // 0-100
    public float hiddenPatience;      // 0-100
    public float hiddenIntelligence;  // 0-100

    // === 疲劳系统字段 ===
    public int consecutiveWorkDays;  // 连续工作天数
    public bool isResting;           // 是否休息日（由外部设置或强制休息触发）

    // === 培训系统字段 ===
    public int trainingCooldownDays; // 培训冷却剩余天数（0=可培训，Train 成功后设为7，每日-1）

    // === 惯性沉积历史基线（30天循环缓冲区） ===
    public int[] fatigueHistory;     // 疲劳历史，用于惯性沉积
    public int fatigueHistoryIndex;
    public float[] loyaltyHistory;   // 忠诚历史，用于惯性沉积
    public int loyaltyHistoryIndex;

    // === T6：师徒传承字段 ===
    public float teachingExperience; // 师傅累计教学经验值

    // === T6：工资字段 ===
    public int baseSalary = 1000;    // 基础工资（沙币/日）
}

[Serializable]
public class SkillData
{
    public string skillName;
    public int level;
    public int maxLevel;
    public float exp; // 经验值（浮点累加，按成长曲线升级）
}

[Serializable]
public class SkillTreeNode
{
    public string systemName;
    public float parentSkillLevel;
    public SubSkillData[] subSkills;

    /// <summary>计算父技能等级 = subSkills 等级的加权平均（等权平均）。</summary>
    public void RecalculateParentLevel()
    {
        if (subSkills == null || subSkills.Length == 0)
        {
            parentSkillLevel = 0f;
            return;
        }

        float total = 0f;
        for (int i = 0; i < subSkills.Length; i++)
        {
            total += subSkills[i].level;
        }
        parentSkillLevel = total / subSkills.Length;
    }
}

[Serializable]
public struct SubSkillData
{
    public string skillName;
    public float level;             // 0-100
    public bool isUnlocked;
    public float[] historicalAvg;   // 最近30天历史均值
    public int historyIndex;
}

[System.Serializable]
public class NpcMemory
{
    public string characterId;
    public List<string> recentTopics;
    public int lastPunctualityScore; // 0=准时, 1=晚点, 2=严重晚点
    public bool deliveredLastPackage;
    public int conversationCount;

    public NpcMemory(string characterId)
    {
        this.characterId = characterId;
        this.recentTopics = new List<string>();
        this.lastPunctualityScore = 0;
        this.deliveredLastPackage = false;
        this.conversationCount = 0;
    }
}

public static class CrewManager
{
    private static List<CrewMember> crew = new List<CrewMember>();
    private static Dictionary<string, NpcMemory> npcMemories = new Dictionary<string, NpcMemory>();

    // === 成长曲线：技能名 → 各等级升级所需天数（0→1, 1→2, 2→3, 3→4, 4→5） ===
    // 驾驶: 60, 90, 150, 300, 600    维修: 30, 60, 110, 250, 350
    // 管理: 20, 40, 90, 150, 300     服务: 15, 30, 75, 130, 250
    // 货运: 15, 35, 100, 200, 350
    private static readonly Dictionary<string, int[]> GrowthCurve = new Dictionary<string, int[]>
    {
        { "driving",    new[] { 60, 90, 150, 300, 600 } },
        { "repair",     new[] { 30, 60, 110, 250, 350 } },
        { "management", new[] { 20, 40, 90, 150, 300 } },
        { "service",    new[] { 15, 30, 75, 130, 250 } },
        { "freight",    new[] { 15, 35, 100, 200, 350 } }
    };

    // 技能名 → 等级称谓（0~7级，8级命名），等级越界时 clamp 到边界
    private static readonly Dictionary<string, string[]> RankNames = new Dictionary<string, string[]>
    {
        { "driving",    new[] { "未培训", "学习司机", "副司机", "司机", "指导司机", "高级指导司机", "首席指导司机", "特级司机" } },
        { "repair",     new[] { "未培训", "学徒工", "初级技工", "中级技工", "高级技工", "技师", "高级技师", "特级技师" } },
        { "management", new[] { "未培训", "係員", "主任", "師範", "助役", "職場長", "統括長", "本部長" } },
        { "service",    new[] { "未培训", "实习员", "列车员", "列车长", "乘务主任", "乘务队长", "首席乘务长", "乘务总长" } },
        { "freight",    new[] { "未培训", "装卸工", "货运员", "货运调度", "货运主管", "货运经理", "货运总监", "货运总长" } }
    };

    // === 岗位匹配：岗位 → 该岗位的核心技能 ===
    private static readonly Dictionary<string, string> CoreSkillByRole = new Dictionary<string, string>
    {
        { "driver",     "driving" },
        { "mechanic",   "repair" },
        { "conductor",  "service" },
        { "dispatcher", "management" },
        { "attendant",  "service" }
    };

    // 相关技能：岗位 → 相关技能列表（与核心技能一起构成匹配规则）
    private static readonly Dictionary<string, string[]> RelatedSkillsByRole = new Dictionary<string, string[]>
    {
        { "driver",     new[] { "freight" } },      // 司机：货运为相关
        { "mechanic",   new[] { "driving" } },      // 机械师：驾驶为相关
        { "conductor",  new[] { "management" } },   // 乘务员：管理为相关
        { "dispatcher", new[] { "driving", "freight" } }, // 调度员：驾驶/货运为相关
        { "attendant",  new[] { "management" } }    // 服务员：管理为相关
    };

    // === T6：工资标准表（技能1-5级 → 月薪，沙币/月） ===
    // 来源：《先民人事系统.md》§7.1 工资标准。CalculateSkillSalary 将 0-100 技能等级映射到此表。
    private static readonly Dictionary<string, int[]> SalaryTable = new Dictionary<string, int[]>
    {
        { "driving",    new[] { 18000, 22000, 28000, 35000, 45000 } },
        { "repair",     new[] { 14000, 17000, 22000, 28000, 35000 } },
        { "management", new[] { 12000, 15000, 18000, 22000, 28000 } },
        { "service",    new[] { 10000, 12000, 14000, 17000, 22000 } },
        { "freight",    new[] { 11000, 13000, 16000, 20000, 25000 } }
    };

    // === 师傅带徒：学徒ID → 师傅ID ===
    private static readonly Dictionary<string, string> MentorDictionary = new Dictionary<string, string>();

    // ===== FluctuationEngine 实例（Task 2：技能树成长） =====
    private static GlobalRules skillGrowthRules;
    private static FluctuationEngine fluctuationEngine;
    private static float skillGrowthTimeSeed = 0f;

    // ===== FluctuationEngine 实例（Task 7：招聘技能树生成） =====
    private static FluctuationEngine recruitFluctuationEngine;

    // ===== Task 7：5 系统子技能名称定义 =====
    private static readonly Dictionary<string, string[]> SubSkillNames = new Dictionary<string, string[]>
    {
        { "driving",    new[] { "acceleration_control", "braking_tech", "route_knowledge" } },
        { "repair",     new[] { "engine_repair", "electrical_systems", "body_maintenance" } },
        { "management", new[] { "scheduling", "budget_planning", "crew_management" } },
        { "service",    new[] { "customer_service", "cleaning_standards", "catering" } },
        { "freight",    new[] { "cargo_loading", "logistics_planning", "inventory_management" } }
    };

    // ===== T6：工资/谈判 波动种子与常量 =====
    private static float salaryTimeSeed = 0f;                 // 工资/谈判波动时间种子（每日递增）
    private const float MentorShareRate = 0.1f;               // 师傅获得徒弟当日收益的比例（10%）
    private const float MentorFatigueCost = 5f;               // 师傅带徒每日疲劳消耗
    private const float WageNegotiationSkillJump = 5f;        // 单日技能增长阈值（>5 点触发工资谈判）

    // 培训每次消耗沙币
    private const int TrainingCost = 200;

    // —— G11：城市 npc_pool → 招募角色池（key=城市ID，value=可招募角色ID列表） ——
    private static Dictionary<string, string[]> recruitingPools = new Dictionary<string, string[]>();
    private static HashSet<string> recruitedIds = new HashSet<string>();

    // —— P5：招聘渠道（社区/广告/猎头） ——

    // 中文字名池（招聘新员工随机取名）
    private static readonly string[] RecruitNames = new string[]
    {
        "王磊", "李华", "刘洋", "陈红", "赵明", "孙丽", "周强", "吴静", "郑伟", "王芳",
        "张敏", "杨光", "马涛", "高洁"
    };

    // 渠道冷却剩余天数（key=渠道名，value=剩余天数），每日递减，与培训冷却同款
    private static Dictionary<string, int> channelCooldowns = new Dictionary<string, int>();

    public static void Initialize()
    {
        crew.Clear();
        MentorDictionary.Clear();

        // 老陈 — 老司机
        crew.Add(new CrewMember
        {
            id = "laochen",
            name = "老陈",
            age = 68,
            role = "driver",
            fatigue = 0,
            loyalty = 50,
            skills = new SkillData[]
            {
                new SkillData { skillName = "driving",    level = 70, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "repair",     level = 25, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "management", level = 25, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "service",    level = 10, maxLevel = 100, exp = 0 }
            }
        });

        // 张工 — 退休机械工程师
        crew.Add(new CrewMember
        {
            id = "zhanggong",
            name = "张工",
            age = 62,
            role = "mechanic",
            fatigue = 0,
            loyalty = 50,
            skills = new SkillData[]
            {
                new SkillData { skillName = "repair",     level = 70, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "driving",    level = 10, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "management", level = 10, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "service",    level = 0,  maxLevel = 100, exp = 0 }
            }
        });

        // 李阿姨 — 社区热心居民
        crew.Add(new CrewMember
        {
            id = "liayi",
            name = "李阿姨",
            age = 55,
            role = "conductor",
            fatigue = 0,
            loyalty = 50,
            skills = new SkillData[]
            {
                new SkillData { skillName = "service",    level = 25, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "management", level = 10, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "repair",     level = 0,  maxLevel = 100, exp = 0 },
                new SkillData { skillName = "driving",    level = 0,  maxLevel = 100, exp = 0 }
            }
        });

        // 赵师傅 — 退休铁路工程师
        crew.Add(new CrewMember
        {
            id = "zhaoshifu",
            name = "赵师傅",
            age = 55,
            role = "dispatcher",
            fatigue = 0,
            loyalty = 50,
            skills = new SkillData[]
            {
                new SkillData { skillName = "management", level = 55, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "driving",    level = 25, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "repair",     level = 10, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "service",    level = 10, maxLevel = 100, exp = 0 }
            }
        });

        // 小芳 — 志愿者
        crew.Add(new CrewMember
        {
            id = "xiaofang",
            name = "小芳",
            age = 45,
            role = "attendant",
            fatigue = 0,
            loyalty = 50,
            skills = new SkillData[]
            {
                new SkillData { skillName = "service",    level = 10, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "management", level = 0,  maxLevel = 100, exp = 0 },
                new SkillData { skillName = "repair",     level = 0,  maxLevel = 100, exp = 0 },
                new SkillData { skillName = "driving",    level = 0,  maxLevel = 100, exp = 0 }
            }
        });

        Debug.Log("[CrewManager] 初始化完成，共 " + crew.Count + " 名员工。");
    }

    // ===== G11：城市 npc_pool → 招募角色池 =====

    /// <summary>从种子各城市 npc_pool 构建招募池（key=城市ID，value=角色ID列表）。</summary>
    public static void SetRecruitingPools(Dictionary<string, string[]> npcPools)
    {
        recruitingPools.Clear();
        if (npcPools == null) return;

        foreach (var kvp in npcPools)
        {
            recruitingPools[kvp.Key] = kvp.Value;
        }
        Debug.Log("[CrewManager] 招募池已更新，共 " + recruitingPools.Count + " 个城市。");
    }

    /// <summary>返回某城市当前可招募的角色ID列表（已剔除招募过的）。</summary>
    public static List<string> GetRecruitableFromCity(string cityId)
    {
        if (!recruitingPools.TryGetValue(cityId, out var pool))
        {
            return new List<string>();
        }

        List<string> result = new List<string>();
        foreach (string npcId in pool)
        {
            if (!recruitedIds.Contains(npcId))
            {
                result.Add(npcId);
            }
        }
        return result;
    }

    /// <summary>从某城市招募指定角色：从池中移除并创建 CrewMember（默认值）。成功返回 true。</summary>
    public static bool RecruitFromCity(string cityId, string npcId)
    {
        // 防御：城市不存在
        if (!recruitingPools.TryGetValue(cityId, out var pool))
        {
            Debug.LogWarning("[CrewManager] 城市 " + cityId + " 不在招募池中，无法招募。");
            return false;
        }

        // 防御：池中不存在该角色
        bool inPool = false;
        foreach (string id in pool)
        {
            if (id == npcId) { inPool = true; break; }
        }
        if (!inPool)
        {
            Debug.LogWarning("[CrewManager] 角色 " + npcId + " 不在城市 " + cityId + " 的招募池中。");
            return false;
        }

        // 防御：不重复招募同一 ID
        if (recruitedIds.Contains(npcId))
        {
            Debug.LogWarning("[CrewManager] 角色 " + npcId + " 已被招募，不可重复招募。");
            return false;
        }
        recruitedIds.Add(npcId);

        // 创建员工（简单默认值：attendant、技能10/0、疲劳0、忠诚50）
        CrewMember member = new CrewMember
        {
            id = npcId,
            name = char.ToUpper(npcId[0]) + npcId.Substring(1),
            age = 30,
            role = "attendant",
            fatigue = 0,
            loyalty = 50,
            skills = new SkillData[]
            {
                new SkillData { skillName = "service",    level = 10, maxLevel = 100, exp = 0 },
                new SkillData { skillName = "management", level = 0,  maxLevel = 100, exp = 0 },
                new SkillData { skillName = "repair",     level = 0,  maxLevel = 100, exp = 0 },
                new SkillData { skillName = "driving",    level = 0,  maxLevel = 100, exp = 0 }
            }
        };
        crew.Add(member);

        Debug.Log("[CrewManager] 招募成功：" + member.name + "（" + npcId + "）加入团队，来自城市 " + cityId + "。");
        return true;
    }

    // ===== P5：招聘渠道（社区/广告/猎头） =====

    /// <summary>通过招聘渠道招募一名新员工。渠道：community（社区推荐，免费）/ ad（广告招聘，500沙币）/ headhunter（猎头推荐，2000沙币）。</summary>
    /// <returns>招聘是否成功（未知渠道/冷却中/资金不足返回 false）。</returns>
    public static bool RecruitByChannel(string channel)
    {
        // 渠道冷却检查
        if (channelCooldowns.TryGetValue(channel, out int remaining) && remaining > 0)
        {
            Debug.Log("[CrewManager] 招聘渠道 " + GetChannelName(channel) + " 冷却中，剩余 " + remaining + " 天。");
            return false;
        }

        // 各渠道参数：费用 / 冷却天数 / 技能等级范围（0-100）/ 潜力（maxLevel）范围 / 年龄范围
        // Task 7: skill tree 范围对应 — community(10-30), ad(20-50), headhunter(30-70)
        int cost, cooldownDays, minSkill, maxSkill, minMaxLevel, maxMaxLevel, minAge, maxAge;
        float skillTreeBase, skillTreeVariance;
        bool hasRareSubSkills = false;
        switch (channel)
        {
            case "community":   // 社区推荐：免费，技能随机10-30，潜力较低
                cost = 0;     cooldownDays = 30; minSkill = 10; maxSkill = 30;
                minMaxLevel = 55; maxMaxLevel = 70; minAge = 30; maxAge = 55;
                skillTreeBase = 20f; skillTreeVariance = 0.5f;
                break;
            case "ad":          // 广告招聘：500沙币，技能20-50
                cost = 500;   cooldownDays = 15; minSkill = 20; maxSkill = 50;
                minMaxLevel = 70; maxMaxLevel = 85; minAge = 25; maxAge = 45;
                skillTreeBase = 35f; skillTreeVariance = 0.4286f;
                break;
            case "headhunter":  // 猎头推荐：2000沙币，技能30-70（含高潜力+稀有子技能）
                cost = 2000;  cooldownDays = 60; minSkill = 30; maxSkill = 70;
                minMaxLevel = 85; maxMaxLevel = 100; minAge = 30; maxAge = 50;
                skillTreeBase = 50f; skillTreeVariance = 0.4f;
                hasRareSubSkills = true;
                break;
            default:
                Debug.LogWarning("[CrewManager] 未知招聘渠道: " + channel);
                return false;
        }

        // 资金检查（社区免费，cost=0 直接通过）
        if (GameData.GetMoney() < cost)
        {
            Debug.Log("[CrewManager] 资金不足，无法通过" + GetChannelName(channel) + "招聘（需要 " + cost + " 沙币）。");
            return false;
        }
        if (cost > 0)
        {
            GameData.AddMoney(-cost);
        }

        // 生成 ID：recruit_<渠道>_<序号>，序号递增直至不与现有员工冲突
        int seq = 1;
        string id;
        do
        {
            id = "recruit_" + channel + "_" + seq;
            seq++;
        } while (GetCrew(id) != null);

        // 从名字池随机取名，创建新员工（含 skillTree 生成）
        CrewMember recruit = CreateRecruit(id, RecruitNames[UnityEngine.Random.Range(0, RecruitNames.Length)],
                                           minSkill, maxSkill, minMaxLevel, maxMaxLevel,
                                           skillTreeBase, skillTreeVariance, hasRareSubSkills);
        recruit.age = UnityEngine.Random.Range(minAge, maxAge + 1);
        crew.Add(recruit);

        // 设置该渠道冷却
        channelCooldowns[channel] = cooldownDays;

        Debug.Log("[CrewManager] 通过" + GetChannelName(channel) + "招募成功：" + recruit.name + "（" + id + "），年龄 " + recruit.age + "，花费 " + cost + " 沙币。");
        return true;
    }

    /// <summary>通用招聘生成：创建新员工（attendant 岗位、疲劳0、忠诚50、4条技能按等级范围随机、skillTree 5系统生成），供各招聘渠道复用。</summary>
    private static CrewMember CreateRecruit(string id, string name, int minSkill, int maxSkill, int minMaxLevel, int maxMaxLevel,
                                             float skillTreeBase, float skillTreeVariance, bool hasRareSubSkills)
    {
        // 生成 4 条技能：service/management/repair/driving
        string[] skillNames = { "service", "management", "repair", "driving" };
        SkillData[] skills = new SkillData[skillNames.Length];
        for (int i = 0; i < skillNames.Length; i++)
        {
            int level = UnityEngine.Random.Range(minSkill, maxSkill + 1);
            skills[i] = new SkillData { skillName = skillNames[i], level = level, maxLevel = 100, exp = 0 };
        }

        // Task 7: 生成 skillTree（5 系统，使用 FluctuationEngine.Simple() 随机化）
        SkillTreeNode[] skillTree = GenerateSkillTree(skillTreeBase, skillTreeVariance, hasRareSubSkills);

        return new CrewMember
        {
            id = id,
            name = name,
            age = 30, // 默认年龄，由各渠道调用后按渠道范围覆盖
            role = "attendant",
            fatigue = 0,
            loyalty = 50,
            skills = skills,
            skillTree = skillTree
        };
    }

    /// <summary>Task 7：生成 5 系统技能树，父技能等级通过 FluctuationEngine.Simple() 在渠道范围内随机，子技能等权分布。</summary>
    private static SkillTreeNode[] GenerateSkillTree(float baseValue, float variance, bool hasRareSubSkills)
    {
        // 延迟初始化 recruitFluctuationEngine
        if (recruitFluctuationEngine == null)
        {
            var rules = new GlobalRules();
            // 确保 recruitment 权重表存在
            bool hasEntry = false;
            for (int i = 0; i < rules.fluctuationWeightsList.Count; i++)
            {
                if (rules.fluctuationWeightsList[i].formulaName == "recruitment")
                {
                    hasEntry = true;
                    break;
                }
            }
            if (!hasEntry)
            {
                rules.fluctuationWeightsList.Add(new GlobalRules.WeightTable
                {
                    formulaName = "recruitment",
                    weights = new float[] { 1.0f }
                });
            }
            recruitFluctuationEngine = new FluctuationEngine(rules, 42, 1.0f);
        }

        string[] systemNames = { "driving", "repair", "management", "service", "freight" };
        SkillTreeNode[] tree = new SkillTreeNode[systemNames.Length];

        for (int s = 0; s < systemNames.Length; s++)
        {
            string system = systemNames[s];

            // 父技能等级 = FluctuationEngine.Simple() 在渠道范围内随机
            float parentLevel = recruitFluctuationEngine.Simple(baseValue, variance);
            parentLevel = Mathf.Clamp(parentLevel, 0f, 100f);

            // 获取该系统对应的子技能名列表
            string[] subNames;
            if (!SubSkillNames.TryGetValue(system, out subNames))
            {
                subNames = new[] { system + "_sub1", system + "_sub2", system + "_sub3" };
            }

            // 生成子技能
            SubSkillData[] subSkills = new SubSkillData[subNames.Length];
            for (int si = 0; si < subNames.Length; si++)
            {
                float subLevel = parentLevel + UnityEngine.Random.Range(-15f, 15f);
                subLevel = Mathf.Clamp(subLevel, 0f, 100f);

                subSkills[si] = new SubSkillData
                {
                    skillName = subNames[si],
                    level = subLevel,
                    isUnlocked = true,
                    historicalAvg = new float[30],
                    historyIndex = 0
                };
            }

            // 猎头渠道：稀有子技能概率
            if (hasRareSubSkills && UnityEngine.Random.value < 0.4f)
            {
                // 随机选 1-2 个子技能提升
                int rareCount = UnityEngine.Random.Range(1, Mathf.Min(3, subSkills.Length + 1));
                for (int r = 0; r < rareCount; r++)
                {
                    int rareIdx = UnityEngine.Random.Range(0, subSkills.Length);
                    float boost = recruitFluctuationEngine.Simple(20f, 0.5f); // 约 10-30 额外等级
                    subSkills[rareIdx].level = Mathf.Clamp(subSkills[rareIdx].level + boost, 0f, 100f);
                }
            }

            // 构建 SkillTreeNode
            SkillTreeNode node = new SkillTreeNode
            {
                systemName = system,
                subSkills = subSkills
            };
            node.RecalculateParentLevel(); // 等权平均计算父技能等级

            tree[s] = node;
        }

        return tree;
    }

    /// <summary>返回各招聘渠道的冷却剩余天数（供 UI 查询）。</summary>
    public static Dictionary<string, int> GetChannelCooldowns()
    {
        return new Dictionary<string, int>(channelCooldowns);
    }

    /// <summary>渠道名 → 中文显示名。</summary>
    private static string GetChannelName(string channel)
    {
        switch (channel)
        {
            case "community":  return "社区推荐";
            case "ad":         return "广告招聘";
            case "headhunter": return "猎头推荐";
            default:           return channel;
        }
    }

    public static CrewMember GetCrew(string id)
    {
        return crew.Find(c => c.id == id);
    }

    public static List<CrewMember> GetAllCrew()
    {
        return new List<CrewMember>(crew);
    }

    public static int GetSkillLevel(string crewId, string skillName)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null)
        {
            Debug.LogWarning("[CrewManager] 未找到员工: " + crewId);
            return -1;
        }

        SkillData skill = Array.Find(member.skills, s => s.skillName == skillName);
        if (skill == null)
        {
            Debug.LogWarning("[CrewManager] 员工 " + crewId + " 无技能: " + skillName);
            return -1;
        }

        return skill.level;
    }

    public static void AssignRole(string crewId, string newRole)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null)
        {
            Debug.LogWarning("[CrewManager] 未找到员工，无法分配岗位: " + crewId);
            return;
        }

        member.role = newRole;
        Debug.Log("[CrewManager] 员工 " + member.name + " 岗位已变更为: " + newRole);
    }

    public static void RecordConversation(string characterId, string topic)
    {
        if (!npcMemories.ContainsKey(characterId))
        {
            npcMemories[characterId] = new NpcMemory(characterId);
        }

        NpcMemory mem = npcMemories[characterId];
        mem.recentTopics.Add(topic);
        mem.conversationCount++;
        Debug.Log($"[CrewManager] 记录对话 - {characterId}: {topic} (第{mem.conversationCount}次)");
    }

    public static void RecordPunctuality(string characterId, int score)
    {
        if (!npcMemories.ContainsKey(characterId))
        {
            npcMemories[characterId] = new NpcMemory(characterId);
        }

        npcMemories[characterId].lastPunctualityScore = score;
        string label = score == 0 ? "准时" : (score == 1 ? "晚点" : "严重晚点");
        Debug.Log($"[CrewManager] 记录准点 - {characterId}: {label}");
    }

    public static string GetMemoryInfluence(string characterId)
    {
        if (!npcMemories.ContainsKey(characterId))
        {
            return "无记录";
        }

        NpcMemory mem = npcMemories[characterId];

        // 如果送过包裹 → 先检查这个，因为包裹交付是正面事件
        if (mem.deliveredLastPackage)
        {
            return "感谢";
        }

        // 如果最近有迟到记录
        if (mem.lastPunctualityScore >= 1)
        {
            return "抱怨";
        }

        // 如果对话次数≥3且最近一次准点 → 连续准点建立信任
        if (mem.conversationCount >= 3 && mem.lastPunctualityScore == 0)
        {
            return "信任";
        }

        // 有对话记录但不足以建立信任
        if (mem.conversationCount > 0)
        {
            return "中立";
        }

        return "无记录";
    }

    /// <summary>
    /// 每日更新：疲劳/技能经验 + 忠诚度变化 + 离职触发。
    /// </summary>
    /// <param name="wagePaidToday">工资是否按时发放（默认 true）</param>
    /// <param name="accidentCrewId">发生事故的员工 ID（没有则 null）</param>
    /// <param name="trainedCrewId">获得培训的员工 ID（没有则 null）</param>
    /// <returns>当日离职的员工列表（可能为空）</returns>
    public static List<CrewMember> DailyUpdate(bool wagePaidToday = true, string accidentCrewId = null, string trainedCrewId = null)
    {
        List<CrewMember> firedToday = new List<CrewMember>();

        // —— T6：工资/谈判波动时间种子（每日递增，供 CalculateSkillSalary / ProcessWageNegotiation 使用） ——
        salaryTimeSeed += 1f;

        // —— P5：招聘渠道冷却每日递减（与培训冷却同款） ——
        List<string> channelKeys = new List<string>(channelCooldowns.Keys);
        foreach (string channelKey in channelKeys)
        {
            int days = channelCooldowns[channelKey] - 1;
            if (days <= 0)
            {
                channelCooldowns.Remove(channelKey);
            }
            else
            {
                channelCooldowns[channelKey] = days;
            }
        }

        // T6: 追踪学徒每日经验获得（学徒ID → 当日总经验获得）
        Dictionary<string, float> apprenticeDailyGains = new Dictionary<string, float>();
        // T6: 追踪各成员旧技能等级总和（用于判断是否触发工资谈判）
        Dictionary<string, int> oldSkillLevelSums = new Dictionary<string, int>();

        foreach (CrewMember member in crew)
        {
            // T6: 记录该成员当日开始前的技能等级总和
            int levelSum = 0;
            foreach (SkillData s in member.skills) levelSum += s.level;
            oldSkillLevelSums[member.id] = levelSum;
            // T6: 追踪该成员当日技能经验获得
            float totalDailyGain = 0f;

            // —— 强制休息：疲劳超过80自动设为休息日 ——
            if (member.fatigue > 80)
            {
                member.isResting = true;
            }

            if (member.isResting)
            {
                // 休息日：疲劳不增长，恢复30，连续工作天数归零
                int fatigueRecovery = 30;
                // 惯性（Step 3）：如果历史疲劳持续偏高（>50 超过10天），恢复减慢
                if (member.fatigueHistory != null)
                {
                    int highFatigueDays = 0;
                    for (int hi = 0; hi < 30; hi++)
                    {
                        if (member.fatigueHistory[hi] > 50) highFatigueDays++;
                    }
                    if (highFatigueDays >= 10)
                        fatigueRecovery = 20; // 累积疲劳导致恢复减慢
                }
                member.fatigue = Mathf.Max(0, member.fatigue - fatigueRecovery);
                member.consecutiveWorkDays = 0;
                member.isResting = false; // 重置休息标记，下一天需重新设置
            }
            else
            {
                // 工作日：计算疲劳增长
                int fatigueIncrease = 10; // 基础值

                // 连续工作超过7天：额外+5
                if (member.consecutiveWorkDays > 7)
                {
                    fatigueIncrease += 5;
                }

                // 司机岗位：额外+3
                if (member.role == "driver")
                {
                    fatigueIncrease += 3;
                }

                member.fatigue += fatigueIncrease;
                member.consecutiveWorkDays++;
            }

            // 疲劳值限制在 0-100
            member.fatigue = Mathf.Clamp(member.fatigue, 0, 100);

            // 惯性沉积：记录疲劳历史（30天循环缓冲区）
            if (member.fatigueHistory == null) member.fatigueHistory = new int[30];
            member.fatigueHistory[member.fatigueHistoryIndex % 30] = member.fatigue;
            member.fatigueHistoryIndex++;

            // ===== P3：技能成长 =====
            // 每日经验 = 基础1.0 × 岗位匹配系数 × 师傅系数（培训日另有加成）
            float totalExpGainToday = 0f;      // 当日 exp 总收益（T6 师徒收益基数）
            int maxSkillLevelJumpToday = 0;    // 当日单技能最大升级点数（T6 工资谈判触发判断）
            float maxSubSkillGainToday = 0f;   // 当日技能树单子技能最大增长（T6 工资谈判触发判断）
            foreach (SkillData skill in member.skills)
            {
                if (skill.level >= skill.maxLevel)
                {
                    continue;
                }

                float dailyGain = 1.0f * GetMatchCoefficient(member.role, skill.skillName)
                                  * GetMentorCoefficient(member.id);

                // 培训日加成：驾驶 +3.0 天经验 / 维修 +4.0 天经验
                if (member.id == trainedCrewId)
                {
                    if (skill.skillName == "driving")
                    {
                        dailyGain += 3.0f;
                    }
                    else if (skill.skillName == "repair")
                    {
                        dailyGain += 4.0f;
                    }
                }

                int skillLevelBefore = skill.level;

                AddSkillExp(member, skill, dailyGain);
                totalExpGainToday += dailyGain;

                int skillLevelJump = skill.level - skillLevelBefore;
                if (skillLevelJump > maxSkillLevelJumpToday)
                {
                    maxSkillLevelJumpToday = skillLevelJump;
                }
            }

            // ===== P3b：技能树成长（FluctuationEngine） =====
            // 新技能树与旧 skills 数组并存，互不干扰
            if (member.skillTree != null && member.skillTree.Length > 0)
            {
                // 首次使用时初始化 FluctuationEngine（默认 GlobalRules + 各公式权重表）
                EnsureFluctuationEngine();

                foreach (SkillTreeNode node in member.skillTree)
                {
                    if (node.subSkills == null || node.subSkills.Length == 0) continue;

                    // 用 for 循环（struct 数组需要索引修改）
                    for (int si = 0; si < node.subSkills.Length; si++)
                    {
                        SubSkillData subSkill = node.subSkills[si];
                        if (!subSkill.isUnlocked) continue;
                        if (subSkill.level >= 100f) continue;

                        // GapBonus = max(0, (parentSkillLevel - subSkill.level) / parentSkillLevel) * 0.5f
                        float parentLvl = node.parentSkillLevel > 0f ? node.parentSkillLevel : 50f;
                        float gapBonus = Mathf.Max(0f, (parentLvl - subSkill.level) / parentLvl) * 0.5f;

                        // BaseGain = rules.baseLearningRate * (1 + GapBonus)
                        float baseGain = skillGrowthRules.baseLearningRate * (1f + gapBonus);

                        // 构建 WeightedFactor[]：{岗位匹配度, 疲劳度, 忠诚度, 有师傅, 培训}
                        WeightedFactor[] factors = new WeightedFactor[]
                        {
                            new WeightedFactor("岗位匹配度", GetMatchCoefficient(member.role, subSkill.skillName)),
                            new WeightedFactor("疲劳度", Mathf.Clamp01(member.fatigue / 100f)),
                            new WeightedFactor("忠诚度", Mathf.Clamp01(member.loyalty / 100f)),
                            new WeightedFactor("有师傅", GetMentor(member.id) != null ? 1f : 0f),
                            new WeightedFactor("培训", member.id == trainedCrewId ? 1f : 0f)
                        };

                        // dailyGain = engine.Weighted(BaseGain, factors, "skill_growth", timeSeed)
                        float dailyGain = fluctuationEngine.Weighted(baseGain, factors, "skill_growth", skillGrowthTimeSeed);

                        // 非线性阈值
                        if (subSkill.level > 80f)
                            dailyGain *= skillGrowthRules.skillCeilingMultiplier;   // 0.5x
                        else if (subSkill.level < 20f)
                            dailyGain *= skillGrowthRules.skillNewbieMultiplier;    // 1.5x

                        // 更新等级（clamp 0-100）
                        float subLevelBefore = subSkill.level;
                        float newLevel = subLevelBefore + dailyGain;
                        node.subSkills[si].level = Mathf.Clamp(newLevel, 0f, 100f);
                        float subLevelGain = node.subSkills[si].level - subLevelBefore;
                        if (subLevelGain > maxSubSkillGainToday)
                        {
                            maxSubSkillGainToday = subLevelGain;
                        }
                    }

                    // 子技能更新完后重新计算父技能等级
                    node.RecalculateParentLevel();
                }

                skillGrowthTimeSeed++;
            }

            // Step 1: Update historical baseline (30-day circular buffer)
            if (member.skillTree != null)
            {
                foreach (var parent in member.skillTree)
                {
                    for (int si = 0; si < parent.subSkills.Length; si++)
                    {
                        SubSkillData sub = parent.subSkills[si];
                        if (sub.historicalAvg == null)
                            sub.historicalAvg = new float[30];
                        sub.historicalAvg[sub.historyIndex % 30] = sub.level;
                        sub.historyIndex++;
                        parent.subSkills[si] = sub; // write back (struct copy)
                    }
                }
            }

            // ===== T6：师徒收益 =====
            // 徒弟有师傅：成长加成已由 GetMentorCoefficient 提供（×1.5 / ×2.0）。
            // 此处补充：师傅获得徒弟当日 exp 收益的 10%（经 FluctuationEngine 加权波动），并在带徒日疲劳 +5。
            if (totalExpGainToday > 0f)
            {
                string mentorId = GetMentor(member.id);
                if (mentorId != null)
                {
                    CrewMember mentor = GetCrew(mentorId);
                    if (mentor != null)
                    {
                        EnsureFluctuationEngine();

                        // 师傅收益基数 = 徒弟当日 exp 收益 × 10%
                        float mentorBase = totalExpGainToday * MentorShareRate;

                        // 等级差：师傅最高技能 - 徒弟最高技能（0-100）
                        float mentorMaxLevel = 0f;
                        foreach (SkillData ms in mentor.skills)
                        {
                            if (ms.level > mentorMaxLevel) mentorMaxLevel = ms.level;
                        }
                        float apprenticeMaxLevel = 0f;
                        foreach (SkillData ad in member.skills)
                        {
                            if (ad.level > apprenticeMaxLevel) apprenticeMaxLevel = ad.level;
                        }

                        // 加权波动：{等级差, 师傅耐心, 徒弟天赋} → mentorship_weights
                        WeightedFactor[] mentorFactors = new WeightedFactor[]
                        {
                            new WeightedFactor("等级差", Mathf.Clamp01((mentorMaxLevel - apprenticeMaxLevel) / 100f)),
                            new WeightedFactor("师傅耐心", Mathf.Clamp01(mentor.hiddenPatience / 100f)),
                            new WeightedFactor("徒弟天赋", Mathf.Clamp01(member.hiddenTalent / 100f))
                        };
                        float mentorGain = fluctuationEngine.Weighted(mentorBase, mentorFactors, "mentorship_weights", salaryTimeSeed);

                        // 师傅收益并入其岗位核心技能
                        AddSkillExp(mentor, FindSkill(mentor, GetCoreSkillForRole(mentor.role)), mentorGain);

                        // 带徒疲劳 +5
                        mentor.fatigue = Mathf.Clamp(mentor.fatigue + MentorFatigueCost, 0, 100);

                        Debug.Log("[CrewManager] 师徒收益：" + mentor.name + " 带徒 " + member.name + "，获得 " + mentorGain.ToString("F2") + " 技能经验，疲劳 +5。");
                    }
                }
            }

            // ===== T6：工资谈判 =====
            // 单日技能增长 > 5 点（技能树子技能或旧技能等级）→ 触发加薪谈判。
            // 忠诚度变化由 FluctuationEngine 计算（wage_negotiation 权重表），不硬编码数值。
            // 自动判定：工资按时发放视为接受加薪（工资已随技能等级上调，见 GameData.CalculateWageCost）。
            if (maxSkillLevelJumpToday > WageNegotiationSkillJump || maxSubSkillGainToday > WageNegotiationSkillJump)
            {
                ProcessWageNegotiation(member, wagePaidToday);
            }

            // 培训冷却：每日递减
            if (member.trainingCooldownDays > 0)
            {
                member.trainingCooldownDays--;
            }

            // ===== P2：忠诚度每日变化 =====

            // 工资按时发放：+0.1
            if (wagePaidToday)
            {
                member.loyalty += 0.1f;
            }

            // 连续工作 > 10天无休息：-1.0
            if (member.consecutiveWorkDays > 10 && !member.isResting)
            {
                float loyaltyDrop = 1.0f;
                // 惯性（Step 3）：如果忠诚度历史持续偏低（<40 超过7天），下降更快
                if (member.loyaltyHistory != null)
                {
                    int lowLoyaltyDays = 0;
                    for (int hi = 0; hi < 30; hi++)
                    {
                        if (member.loyaltyHistory[hi] < 40) lowLoyaltyDays++;
                    }
                    if (lowLoyaltyDays >= 7)
                        loyaltyDrop = 1.5f; // 累积不满导致忠诚加速下降
                }
                member.loyalty -= loyaltyDrop;
            }

            // 该员工涉及事故：-5.0
            if (member.id == accidentCrewId)
            {
                member.loyalty -= 5.0f;
            }

            // 获得培训：+2.0
            if (member.id == trainedCrewId)
            {
                member.loyalty += 2.0f;
            }

            // ===== T9：社会对比效应 =====
            // 员工发现同事工资更高时，忠诚度下降
            float memberSalary = CalculateSkillSalary(member);
            foreach (CrewMember other in crew)
            {
                if (other.id == member.id) continue;
                float otherSalary = CalculateSkillSalary(other);
                if (otherSalary > memberSalary * 1.1f) // 比自己的10%还高
                {
                    float gap = (otherSalary - memberSalary) / memberSalary;
                    member.loyalty -= gap * 0.1f * (member.hiddenPatience > 0 ? (1f - member.hiddenPatience / 100f) : 1f);
                    break; // 只对比一次，减少性能开销
                }
            }

            // 惯性沉积：记录忠诚历史（30天循环缓冲区）
            if (member.loyaltyHistory == null) member.loyaltyHistory = new float[30];
            member.loyaltyHistory[member.loyaltyHistoryIndex % 30] = member.loyalty;
            member.loyaltyHistoryIndex++;

            // Step 2: Nonlinear threshold effects
            float quitProbabilityMultiplier = 1.0f;
            if (member.fatigue > 80)
            {
                // Double accident probability, reduce learning efficiency
                // (the actual effect is applied in the FluctuationEngine call)
            }
            if (member.loyalty < 30)
            {
                // Increase quit probability by 1.5x
                quitProbabilityMultiplier = 1.5f;
            }

            // ===== P2：离职触发（在 clamp 之前，保留 <0 判断） =====
            bool shouldFire = false;
            if (member.loyalty < 0)
            {
                shouldFire = true; // 忠诚 < 0：立即离职
            }
            else if (member.loyalty < 30 && UnityEngine.Random.value < 0.1f * quitProbabilityMultiplier)
            {
                shouldFire = true; // 忠诚 < 30：10% 概率离职（非线性阈值下 1.5x → 15%）
            }

            // ===== P5：年龄 > 65 退休（1%/月，以 0.001/日近似） =====
            if (!shouldFire && member.age > 65 && UnityEngine.Random.value < 0.001f)
            {
                shouldFire = true; // 年龄超限：按退休离职
            }

            // 忠诚度 clamp 0-100（离职判断之后）
            member.loyalty = Mathf.Clamp(member.loyalty, 0, 100);

            if (shouldFire)
            {
                string reason = member.loyalty < 30 ? "忠诚度不足" : "年龄超过65岁";
                Debug.Log("[CrewManager] 员工 " + member.name + "（" + member.id + "）因" + reason + "已离职。");
                firedToday.Add(member);
            }
        }

        // 从 crew 列表中移除离职员工（统一走 FireCrew）
        foreach (CrewMember fired in firedToday)
        {
            FireCrew(fired.id);
        }

        if (firedToday.Count > 0)
        {
            Debug.Log("[CrewManager] 当日共 " + firedToday.Count + " 名员工离职。");
        }

        Debug.Log("[CrewManager] 每日更新完成。");
        return firedToday;
    }

    /// <summary>根据疲劳值 + 忠诚度获取效率倍率（0.0~1.0）。</summary>
    public static float GetEfficiency(string crewId)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null) return 1.0f;

        // —— 疲劳效率 ——
        float fatigueRatio;
        if (member.fatigue <= 30)          fatigueRatio = 1.0f;
        else if (member.fatigue <= 60)     fatigueRatio = 0.9f;
        else if (member.fatigue <= 80)     fatigueRatio = 0.75f;
        else                                fatigueRatio = 0.5f;

        // —— 忠诚效率 ——
        float loyaltyFactor;
        if (member.loyalty >= 90)          loyaltyFactor = 1.1f;
        else if (member.loyalty >= 70)     loyaltyFactor = 1.0f;
        else if (member.loyalty >= 50)     loyaltyFactor = 0.9f;
        else if (member.loyalty >= 30)     loyaltyFactor = 0.75f;
        else                                loyaltyFactor = 0.5f;

        return fatigueRatio * loyaltyFactor;
    }

    /// <summary>根据疲劳值返回对话文本（行为流露，不显示数字）。</summary>
    public static string GetFatigueDialogue(string crewId)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null) return "";

        if (member.fatigue <= 30)      return "";           // 正常，无特殊对话
        else if (member.fatigue <= 60) return "有点累了...";         // 轻度疲劳
        else if (member.fatigue <= 80) return "不太舒服，想休息一下"; // 中度疲劳
        else                           return "......";             // 重度疲劳（沉默）
    }

    /// <summary>根据忠诚度返回对话文本（行为流露，不显示数字）。</summary>
    public static string GetLoyaltyDialogue(string crewId)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null) return "";

        if (member.loyalty >= 90)      return "放心，有我在。";         // 忠诚
        else if (member.loyalty >= 70) return "";                       // 正常，无特殊对话
        else if (member.loyalty >= 50) return "嗯。";                   // 冷淡/敷衍
        else if (member.loyalty >= 30) return "工资什么时候发？";       // 不满
        else                           return "我不干了。";             // 敌对
    }

    // ===== P4：行为流露系统（疲劳/忠诚 → 综合对话/情绪标签/状态提示，只读查询，不改状态） =====

    /// <summary>综合对话：优先疲劳流露（疲劳>60），其次忠诚流露（忠诚<70 或有极端值），两者都正常返回空。</summary>
    public static string GetCrewDialogue(string crewId)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null) return "";

        // 优先疲劳对话：仅中度/重度疲劳（>60）才优先流露
        if (member.fatigue > 60)
        {
            return GetFatigueDialogue(crewId);
        }

        // 其次忠诚对话：忠诚 <70（冷淡/不满/敌对）或有极端值（≥90 忠诚）
        if (member.loyalty < 70 || member.loyalty >= 90)
        {
            return GetLoyaltyDialogue(crewId);
        }

        // 两者都正常 → 无特殊对话
        return "";
    }

    /// <summary>情绪状态标签（无数字，供经营 UI / VN 使用）。</summary>
    public static string GetCrewEmotionalState(string crewId)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null) return "正常";

        if (member.fatigue > 80) return "极度疲惫";
        if (member.fatigue > 60) return "疲惫";
        if (member.loyalty < 30) return "不满";
        if (member.loyalty < 50) return "冷淡";
        if (member.loyalty > 85) return "忠诚";
        return "正常";
    }

    /// <summary>VN 变量注入：返回剧本可用的对话状态标记（角色特征特定标记 + 通用 crew_&lt;id&gt;_* 标记）。</summary>
    public static Dictionary<string, bool> GetCrewDialogueFlags(string crewId)
    {
        CrewMember member = GetCrew(crewId);
        Dictionary<string, bool> flags = new Dictionary<string, bool>();

        bool tired = member != null && member.fatigue > 60;
        bool angry = member != null && member.loyalty < 40;
        bool loyal = member != null && member.loyalty > 85;
        bool normal = !tired && !angry && !loyal;

        // —— 角色特征特定标记 ——
        flags["laochen_tired"] = tired && member != null && member.id == "laochen";     // 老陈：老司机，疲劳流露最明显
        flags["liayi_angry"] = angry && member != null && member.id == "liayi";         // 李阿姨：性子直，不满直接流露
        flags["zhanggong_loyal"] = loyal && member != null && member.id == "zhanggong"; // 张工：退休工程师，重情重义

        // —— 通用标记 ——
        string id = member != null ? member.id : "";
        flags["crew_" + id + "_tired"] = tired;
        flags["crew_" + id + "_angry"] = angry;
        flags["crew_" + id + "_loyal"] = loyal;
        flags["crew_" + id + "_normal"] = normal;

        return flags;
    }

    /// <summary>经营 UI 状态提示（无数字，疲劳+忠诚综合描述，带角色名字）。</summary>
    public static string GetCrewStatusText(string crewId)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null) return "";

        string name = member.name;

        // 疲劳优先：重度疲劳
        if (member.fatigue > 80)
        {
            return name + "今天不太对劲，建议休息。";
        }
        if (member.fatigue > 60)
        {
            return name + "有点累，但还在坚持。";
        }

        // 忠诚：低忠诚不满提示
        if (member.loyalty < 30)
        {
            return name + "似乎对现状很不满，务必留意。";
        }
        if (member.loyalty < 50)
        {
            return name + "似乎对现状不满。";
        }

        // 高忠诚：干劲十足
        if (member.loyalty > 85)
        {
            return name + "今天干劲十足，精神不错！";
        }

        // 完全正常
        if (member.fatigue <= 30)
        {
            return name + "今天精神不错。";
        }

        return name + "状态平稳。";
    }

    /// <summary>行为流露采样（供日记/日志）：从疲劳/忠诚对话池随机选一条非空对话，无流露返回空。</summary>
    public static string GetRandomBehaviorLine(string crewId)
    {
        if (GetCrew(crewId) == null) return "";

        // 收集当前状态流露出的非空对话（疲劳池 + 忠诚池）
        List<string> pool = new List<string>();

        string fatigueText = GetFatigueDialogue(crewId);
        if (fatigueText != "") pool.Add(fatigueText);

        string loyaltyText = GetLoyaltyDialogue(crewId);
        if (loyaltyText != "") pool.Add(loyaltyText);

        if (pool.Count == 0) return "";
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    /// <summary>外部调用：强制解雇某员工，返回被解雇的员工信息。</summary>
    public static CrewMember FireCrew(string crewId)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null)
        {
            Debug.LogWarning("[CrewManager] 未找到员工，无法解雇: " + crewId);
            return null;
        }

        crew.Remove(member);
        Debug.Log("[CrewManager] 员工 " + member.name + "（" + member.id + "）已被解雇。");
        return member;
    }

    /// <summary>根据疲劳值获取事故风险修正系数。</summary>
    public static float GetAccidentRiskModifier(string crewId)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null) return 1.0f;

        if (member.fatigue <= 30)      return 1.0f;
        else if (member.fatigue <= 60) return 1.5f;
        else if (member.fatigue <= 80) return 2.5f;
        else                           return 4.0f;
    }

    /// <summary>设置员工的休息状态（供外部调用）。</summary>
    public static void SetResting(string crewId, bool resting)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null)
        {
            Debug.LogWarning("[CrewManager] 未找到员工，无法设置休息状态: " + crewId);
            return;
        }
        member.isResting = resting;
        Debug.Log("[CrewManager] 员工 " + member.name + " 休息状态已设置为: " + resting);
    }

    // ===== P3：技能成长曲线 =====

    /// <summary>获取升级到下一级所需经验天数（成长曲线表）。</summary>
    /// <param name="skillName">技能名（driving/repair/management/service/freight）</param>
    /// <param name="level">当前等级（0-4，升到下一级所需天数）；表外技能/越界返回极大值（不再升级）。</param>
    public static int GetExpToNext(string skillName, int level)
    {
        if (!GrowthCurve.TryGetValue(skillName, out int[] curve))
        {
            return int.MaxValue;
        }
        if (level < 0 || level >= curve.Length)
        {
            return int.MaxValue;
        }
        return curve[level];
    }

    /// <summary>计算岗位匹配系数：核心技能 ×1.0 / 相关技能 ×0.5 / 不相关 ×0.2。</summary>
    public static float GetMatchCoefficient(string role, string skillName)
    {
        // 核心技能匹配
        if (CoreSkillByRole.TryGetValue(role, out string coreSkill) && coreSkill == skillName)
        {
            return 1.0f;
        }

        // 相关技能匹配
        if (RelatedSkillsByRole.TryGetValue(role, out string[] relatedSkills))
        {
            foreach (string s in relatedSkills)
            {
                if (s == skillName)
                {
                    return 0.5f;
                }
            }
        }

        // 不相关
        return 0.2f;
    }

    /// <summary>设置师徒关系：学徒 → 师傅。师傅必须是已登记员工，且不能是自己。</summary>
    public static void SetMentor(string apprenticeId, string mentorId)
    {
        if (GetCrew(apprenticeId) == null)
        {
            Debug.LogWarning("[CrewManager] 未找到学徒员工: " + apprenticeId);
            return;
        }
        if (GetCrew(mentorId) == null)
        {
            Debug.LogWarning("[CrewManager] 未找到师傅员工: " + mentorId);
            return;
        }
        if (apprenticeId == mentorId)
        {
            Debug.LogWarning("[CrewManager] 师傅与学徒不能是同一人: " + apprenticeId);
            return;
        }

        MentorDictionary[apprenticeId] = mentorId;
        Debug.Log("[CrewManager] 师徒关系已建立: " + apprenticeId + " 拜 " + mentorId + " 为师。");
    }

    /// <summary>查询某员工的师傅ID（无师傅返回 null）。</summary>
    public static string GetMentor(string crewId)
    {
        if (MentorDictionary.TryGetValue(crewId, out string mentorId))
        {
            return mentorId;
        }
        return null;
    }

    /// <summary>计算员工技能工资。基于技能等级平均值，使用 FluctuationEngine 计算。</summary>
    public static float CalculateSkillSalary(CrewMember member)
    {
        if (member == null) return 0f;
        float totalLevel = 0f;
        int count = 0;
        if (member.skills != null)
        {
            for (int i = 0; i < member.skills.Length; i++)
            {
                totalLevel += member.skills[i].level;
                count++;
            }
        }
        if (member.skillTree != null)
        {
            foreach (var node in member.skillTree)
            {
                if (node.subSkills != null)
                {
                    for (int i = 0; i < node.subSkills.Length; i++)
                    {
                        totalLevel += node.subSkills[i].level;
                        count++;
                    }
                }
            }
        }
        float avgLevel = count > 0 ? totalLevel / count : 0f;
        float baseSal = member.baseSalary > 0 ? member.baseSalary : 1000;
        return baseSal * (1f + avgLevel / 100f * 0.5f);
    }

    /// <summary>计算师傅系数：师傅等级≥4级 ×2.0 / 有师傅 ×1.5 / 无师傅 ×1.0。</summary>
    /// <param name="crewId">学徒ID</param>
    /// <param name="skillName">按哪个技能判断师傅等级（null 则取师傅最高等级技能）。</param>
    public static float GetMentorCoefficient(string crewId, string skillName = null)
    {
        if (!MentorDictionary.TryGetValue(crewId, out string mentorId))
        {
            return 1.0f; // 无师傅
        }

        CrewMember mentor = GetCrew(mentorId);
        if (mentor == null)
        {
            return 1.0f; // 师傅已离职，视为无师傅
        }

        // 判断师傅等级：优先指定技能，其次师傅全部技能中的最高等级
        int maxMentorLevel = 0;
        foreach (SkillData s in mentor.skills)
        {
            if (!string.IsNullOrEmpty(skillName) && s.skillName != skillName)
            {
                continue;
            }
            if (s.level > maxMentorLevel)
            {
                maxMentorLevel = s.level;
            }
        }

        return maxMentorLevel >= 55 ? 2.0f : 1.5f;
    }

    /// <summary>
    /// 培训系统：消耗 200 沙币，冷却 7 天。
    /// 培训日（DailyUpdate 传入 trainedCrewId）额外获得经验：驾驶 +3.0 / 维修 +4.0。
    /// </summary>
    /// <returns>培训是否成功预定（资金不足或冷却中返回 false）。</returns>
    public static bool Train(string crewId)
    {
        CrewMember member = GetCrew(crewId);
        if (member == null)
        {
            Debug.LogWarning("[CrewManager] 未找到员工，无法培训: " + crewId);
            return false;
        }

        if (member.trainingCooldownDays > 0)
        {
            Debug.Log("[CrewManager] " + member.name + " 培训冷却中，剩余 " + member.trainingCooldownDays + " 天。");
            return false;
        }

        if (GameData.GetMoney() < TrainingCost)
        {
            Debug.Log("[CrewManager] 资金不足，无法培训 " + member.name + "（需要 " + TrainingCost + " 沙币）。");
            return false;
        }

        GameData.AddMoney(-TrainingCost);
        member.trainingCooldownDays = 7;
        Debug.Log("[CrewManager] " + member.name + " 已安排培训（下次可在 7 天后再次培训）。");
        return true;
    }

    // ===== T6：师徒传承 + 技能↔工资平衡 =====

    /// <summary>T6：确保 FluctuationEngine 就绪（默认 GlobalRules + 各公式权重表，缺失时回退默认权重）。</summary>
    private static void EnsureFluctuationEngine()
    {
        if (fluctuationEngine != null)
        {
            return;
        }

        skillGrowthRules = new GlobalRules();
        EnsureWeightTable("skill_growth",       new float[] { 0.30f, 0.20f, 0.20f, 0.15f, 0.15f });
        EnsureWeightTable("salary_weights",     new float[] { 1.0f, 0.5f, 0.3f, 0.2f });
        EnsureWeightTable("mentorship_weights", new float[] { 1.0f, 0.5f, 0.3f });
        EnsureWeightTable("wage_negotiation",   new float[] { 1.0f, 0.5f, 0.3f, 0.2f });
        fluctuationEngine = new FluctuationEngine(skillGrowthRules, 42, 1.0f);
    }

    /// <summary>若权重表中不存在指定公式，则追加带默认权重的表项（与 skill_growth 同款回退逻辑）。</summary>
    private static void EnsureWeightTable(string formulaName, float[] weights)
    {
        for (int i = 0; i < skillGrowthRules.fluctuationWeightsList.Count; i++)
        {
            if (skillGrowthRules.fluctuationWeightsList[i].formulaName == formulaName)
            {
                return;
            }
        }
        skillGrowthRules.fluctuationWeightsList.Add(new GlobalRules.WeightTable
        {
            formulaName = formulaName,
            weights = weights
        });
    }

    /// <summary>给指定技能追加经验，并处理升级（exp >= 成长曲线天数 → level++）。</summary>
    private static void AddSkillExp(CrewMember member, SkillData skill, float expGain)
    {
        if (member == null || skill == null)
        {
            return;
        }
        if (skill.level >= skill.maxLevel)
        {
            return;
        }

        skill.exp += expGain;

        // 升级检查：exp >= 当前等级所需天数 → level++，exp 扣除对应天数
        while (skill.level < skill.maxLevel && skill.exp >= GetExpToNext(skill.skillName, skill.level))
        {
            skill.exp -= GetExpToNext(skill.skillName, skill.level);
            skill.level++;
            Debug.Log("[CrewManager] " + member.name + " 的 " + skill.skillName + " 技能提升至 " + skill.level + " 级（" + GetRankName(skill.skillName, skill.level) + "）。");
        }
    }

    /// <summary>岗位 → 核心技能名（未命中时回退 service）。</summary>
    private static string GetCoreSkillForRole(string role)
    {
        if (CoreSkillByRole.TryGetValue(role, out string coreSkill))
        {
            return coreSkill;
        }
        return "service";
    }

    /// <summary>按技能名查找员工技能（找不到返回 null）。</summary>
    private static SkillData FindSkill(CrewMember member, string skillName)
    {
        if (member == null || member.skills == null)
        {
            return null;
        }
        return Array.Find(member.skills, s => s.skillName == skillName);
    }

    /// <summary>员工岗位核心技能的等级比例（0-1）。</summary>
    private static float GetCoreSkillLevelRatio(CrewMember member)
    {
        SkillData skill = FindSkill(member, GetCoreSkillForRole(member.role));
        return skill != null ? Mathf.Clamp01(skill.level / 100f) : 0f;
    }

    /// <summary>
    /// T6：按技能等级计算员工月薪（沙币/月）。
    /// 基准 = 岗位核心技能等级映射工资表（0-100 → 1-5级），再经 FluctuationEngine 按 salary_weights 加权波动。
    /// </summary>
    public static int CalculateSkillSalary(CrewMember member)
    {
        if (member == null)
        {
            return 0;
        }

        EnsureFluctuationEngine();

        // 岗位核心技能等级 0-100 → 工资表索引（1-5 级，索引 0-4）
        string coreSkill = GetCoreSkillForRole(member.role);
        SkillData skill = FindSkill(member, coreSkill);
        int level = skill != null ? Mathf.Clamp(skill.level, 0, 100) : 0;

        if (!SalaryTable.TryGetValue(coreSkill, out int[] table) || table == null || table.Length == 0)
        {
            return 0;
        }
        int tableIndex = Mathf.Clamp(Mathf.RoundToInt(level / 100f * (table.Length - 1)), 0, table.Length - 1);
        float baseMonthly = table[tableIndex];

        // 加权波动：{技能等级, 岗位匹配, 忠诚度, 疲劳度} → salary_weights
        WeightedFactor[] factors = new WeightedFactor[]
        {
            new WeightedFactor("技能等级", level / 100f),
            new WeightedFactor("岗位匹配", GetMatchCoefficient(member.role, coreSkill)),
            new WeightedFactor("忠诚度", Mathf.Clamp01(member.loyalty / 100f)),
            new WeightedFactor("疲劳度", 1f - Mathf.Clamp01(member.fatigue / 100f))
        };

        float fluctuatedMonthly = fluctuationEngine.Weighted(baseMonthly, factors, "salary_weights", salaryTimeSeed);
        return Mathf.RoundToInt(fluctuatedMonthly);
    }

    /// <summary>
    /// T6：工资谈判——单日技能增长 > 5 点时触发。
    /// 忠诚度变化由 FluctuationEngine 计算（wage_negotiation 权重表），不硬编码具体数值。
    /// </summary>
    /// <param name="member">谈判员工。</param>
    /// <param name="raiseAccepted">公司是否接受加薪（true=接受→忠诚上升；false=拒绝→忠诚下降）。</param>
    /// <returns>本次谈判导致的忠诚度变化量（可正可负）。</returns>
    public static float ProcessWageNegotiation(CrewMember member, bool raiseAccepted)
    {
        if (member == null)
        {
            return 0f;
        }

        EnsureFluctuationEngine();

        // 基准 = 员工当前月薪换算的忠诚变化量级（工资越高，谈判牵动越大）
        float baseLoyaltyDelta = CalculateSkillSalary(member) / 5000f;

        WeightedFactor[] factors = new WeightedFactor[]
        {
            new WeightedFactor("加薪接受度", raiseAccepted ? 1f : 0f),
            new WeightedFactor("技能等级", GetCoreSkillLevelRatio(member)),
            new WeightedFactor("忠诚度", Mathf.Clamp01(member.loyalty / 100f)),
            new WeightedFactor("疲劳度", Mathf.Clamp01(member.fatigue / 100f))
        };

        // 接受加薪 → 正忠诚变化；拒绝 → 负忠诚变化（量级由波动引擎决定）
        float delta = fluctuationEngine.Weighted(baseLoyaltyDelta, factors, "wage_negotiation", salaryTimeSeed)
                      * (raiseAccepted ? 1f : -1f);
        member.loyalty = Mathf.Clamp(member.loyalty + delta, 0, 100);

        Debug.Log("[CrewManager] 工资谈判：" + member.name + "（" + member.id + "）" +
                  (raiseAccepted ? "接受加薪" : "拒绝加薪") +
                  "，忠诚度变化 " + delta.ToString("F2") + "，当前忠诚 " + member.loyalty.ToString("F1") + "。");
        return delta;
    }

    // ===== Task 8: 技能协同效应 =====

    // 互补岗位协同矩阵：岗位A → { (岗位B, 匹配度) }
    private static readonly Dictionary<string, Dictionary<string, float>> ComplementaryMatrix = new Dictionary<string, Dictionary<string, float>>
    {
        { "driver",     new Dictionary<string, float> { { "dispatcher", 0.8f }, { "mechanic", 0.6f } } },
        { "mechanic",   new Dictionary<string, float> { { "driver", 0.6f } } },
        { "conductor",  new Dictionary<string, float> { { "dispatcher", 0.5f } } },
        { "dispatcher", new Dictionary<string, float> { { "driver", 0.8f }, { "conductor", 0.5f }, { "attendant", 0.4f } } },
        { "attendant",  new Dictionary<string, float> { { "dispatcher", 0.4f } } }
    };

    private static FluctuationEngine synergyEngine;
    private static float synergyCoefficient = 0.2f;
    private static bool synergyInitialized = false;

    /// <summary>计算全队协同效率倍率。遍历所有互补岗位对，按双方核心技能等级累计加成。
    /// 公式：synergy = 1.0 + sum(complementMatch * bothLevels / 100) * 系数，系数通过 FluctuationEngine 计算。</summary>
    public static float CalculateSynergy()
    {
        if (crew.Count < 2) return 1.0f;

        // 一次性初始化协同引擎
        if (!synergyInitialized)
        {
            var rules = new GlobalRules();
            bool hasEntry = false;
            for (int i = 0; i < rules.fluctuationWeightsList.Count; i++)
            {
                if (rules.fluctuationWeightsList[i].formulaName == "synergy")
                {
                    hasEntry = true;
                    break;
                }
            }
            if (!hasEntry)
            {
                rules.fluctuationWeightsList.Add(new GlobalRules.WeightTable
                {
                    formulaName = "synergy",
                    weights = new float[] { 1.0f }
                });
            }
            synergyEngine = new FluctuationEngine(rules, 42, 1.0f);
            synergyCoefficient = synergyEngine.Compound(0.2f, new WeightedFactor[0], "synergy");
            synergyInitialized = true;
        }

        float totalBonus = 0f;

        for (int i = 0; i < crew.Count; i++)
        {
            for (int j = i + 1; j < crew.Count; j++)
            {
                CrewMember a = crew[i];
                CrewMember b = crew[j];

                // 检查a的岗位是否与b互补
                if (ComplementaryMatrix.TryGetValue(a.role, out var matches) && matches.TryGetValue(b.role, out float match))
                {
                    // bothLevels = 双方核心技能等级的平均值
                    string coreA = CoreSkillByRole.TryGetValue(a.role, out var ca) ? ca : "driving";
                    string coreB = CoreSkillByRole.TryGetValue(b.role, out var cb) ? cb : "driving";

                    float levelA = 0f, levelB = 0f;
                    foreach (var s in a.skills) { if (s.skillName == coreA) { levelA = s.level; break; } }
                    foreach (var s in b.skills) { if (s.skillName == coreB) { levelB = s.level; break; } }

                    float bothLevels = (levelA + levelB) / 2f;
                    totalBonus += match * bothLevels / 100f;
                }
            }
        }

        return 1.0f + totalBonus * synergyCoefficient;
    }

    /// <summary>
    /// 根据技能名与等级（0-100）返回等级称谓（如"司机"、"技师"），
    /// 将 0-100 区间映射到 0-7 级命名数组，越界时 clamp 到边界。
    /// </summary>
    public static string GetRankName(string skillName, int level)
    {
        if (!RankNames.TryGetValue(skillName, out string[] ranks))
        {
            return "未培训";
        }
        // 将 0-100 等级映射到 0-(ranks.Length-1) 的索引
        int rankIndex = Mathf.Clamp(Mathf.RoundToInt(level / 100f * (ranks.Length - 1)), 0, ranks.Length - 1);
        return ranks[rankIndex];
    }
}