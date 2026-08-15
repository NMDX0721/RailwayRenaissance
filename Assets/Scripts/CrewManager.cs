using System;
using System.Collections.Generic;
using UnityEngine;

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

    // === 疲劳系统字段 ===
    public int consecutiveWorkDays;  // 连续工作天数
    public bool isResting;           // 是否休息日（由外部设置或强制休息触发）

    // === 培训系统字段 ===
    public int trainingCooldownDays; // 培训冷却剩余天数（0=可培训，Train 成功后设为7，每日-1）
}

[Serializable]
public class SkillData
{
    public string skillName;
    public int level;
    public int maxLevel;
    public float exp; // 经验值（浮点累加，按成长曲线升级）
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

    // 技能名 → 等级称谓（0~5级），等级越界时 clamp 到边界
    private static readonly Dictionary<string, string[]> RankNames = new Dictionary<string, string[]>
    {
        { "driving",    new[] { "未培训", "学习司机", "副司机", "司机", "指导司机", "高级指导司机" } },
        { "repair",     new[] { "未培训", "学徒工", "初级维修工", "中级维修工", "高级维修工", "技师" } },
        { "management", new[] { "未培训", "见习生", "站务员", "值班员", "副站长", "站长" } },
        { "service",    new[] { "未培训", "实习员", "乘务员", "列车长", "乘务主任", "乘务队长" } },
        { "freight",    new[] { "未培训", "装卸工", "货运员", "货运调度", "货运主管", "货运经理" } }
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

    // === 师傅带徒：学徒ID → 师傅ID ===
    private static readonly Dictionary<string, string> MentorDictionary = new Dictionary<string, string>();

    // 培训每次消耗沙币
    private const int TrainingCost = 200;

    // —— G11：城市 npc_pool → 招募角色池（key=城市ID，value=可招募角色ID列表） ——
    private static Dictionary<string, string[]> recruitingPools = new Dictionary<string, string[]>();
    private static HashSet<string> recruitedIds = new HashSet<string>();

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
                new SkillData { skillName = "driving",    level = 5, maxLevel = 5, exp = 0 },
                new SkillData { skillName = "repair",     level = 2, maxLevel = 3, exp = 0 },
                new SkillData { skillName = "management", level = 2, maxLevel = 3, exp = 0 },
                new SkillData { skillName = "service",    level = 1, maxLevel = 2, exp = 0 }
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
                new SkillData { skillName = "repair",     level = 5, maxLevel = 5, exp = 0 },
                new SkillData { skillName = "driving",    level = 1, maxLevel = 3, exp = 0 },
                new SkillData { skillName = "management", level = 1, maxLevel = 2, exp = 0 },
                new SkillData { skillName = "service",    level = 0, maxLevel = 1, exp = 0 }
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
                new SkillData { skillName = "service",    level = 2, maxLevel = 4, exp = 0 },
                new SkillData { skillName = "management", level = 1, maxLevel = 3, exp = 0 },
                new SkillData { skillName = "repair",     level = 0, maxLevel = 1, exp = 0 },
                new SkillData { skillName = "driving",    level = 0, maxLevel = 1, exp = 0 }
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
                new SkillData { skillName = "management", level = 4, maxLevel = 4, exp = 0 },
                new SkillData { skillName = "driving",    level = 2, maxLevel = 3, exp = 0 },
                new SkillData { skillName = "repair",     level = 1, maxLevel = 2, exp = 0 },
                new SkillData { skillName = "service",    level = 1, maxLevel = 2, exp = 0 }
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
                new SkillData { skillName = "service",    level = 1, maxLevel = 4, exp = 0 },
                new SkillData { skillName = "management", level = 0, maxLevel = 3, exp = 0 },
                new SkillData { skillName = "repair",     level = 0, maxLevel = 1, exp = 0 },
                new SkillData { skillName = "driving",    level = 0, maxLevel = 2, exp = 0 }
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

        // 创建员工（简单默认值：attendant、技能1级、疲劳0、忠诚50）
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
                new SkillData { skillName = "service",    level = 1, maxLevel = 4, exp = 0 },
                new SkillData { skillName = "management", level = 0, maxLevel = 3, exp = 0 },
                new SkillData { skillName = "repair",     level = 0, maxLevel = 1, exp = 0 },
                new SkillData { skillName = "driving",    level = 0, maxLevel = 2, exp = 0 }
            }
        };
        crew.Add(member);

        Debug.Log("[CrewManager] 招募成功：" + member.name + "（" + npcId + "）加入团队，来自城市 " + cityId + "。");
        return true;
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

        foreach (CrewMember member in crew)
        {
            // —— 强制休息：疲劳超过80自动设为休息日 ——
            if (member.fatigue > 80)
            {
                member.isResting = true;
            }

            if (member.isResting)
            {
                // 休息日：疲劳不增长，恢复30，连续工作天数归零
                member.fatigue = Mathf.Max(0, member.fatigue - 30);
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

            // ===== P3：技能成长 =====
            // 每日经验 = 基础1.0 × 岗位匹配系数 × 师傅系数（培训日另有加成）
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

                skill.exp += dailyGain;

                // 升级检查：exp >= 当前等级所需天数 → level++，exp 扣除对应天数
                while (skill.level < skill.maxLevel && skill.exp >= GetExpToNext(skill.skillName, skill.level))
                {
                    skill.exp -= GetExpToNext(skill.skillName, skill.level);
                    skill.level++;
                    Debug.Log("[CrewManager] " + member.name + " 的 " + skill.skillName + " 技能提升至 " + skill.level + " 级（" + GetRankName(skill.skillName, skill.level) + "）。");
                }
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
                member.loyalty -= 1.0f;
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

            // ===== P2：离职触发（在 clamp 之前，保留 <0 判断） =====
            bool shouldFire = false;
            if (member.loyalty < 0)
            {
                shouldFire = true; // 忠诚 < 0：立即离职
            }
            else if (member.loyalty < 30 && UnityEngine.Random.value < 0.1f)
            {
                shouldFire = true; // 忠诚 < 30：10% 概率离职
            }

            // 忠诚度 clamp 0-100（离职判断之后）
            member.loyalty = Mathf.Clamp(member.loyalty, 0, 100);

            if (shouldFire)
            {
                Debug.Log("[CrewManager] 员工 " + member.name + "（" + member.id + "）因忠诚度不足已离职。");
                firedToday.Add(member);
            }
        }

        // 从 crew 列表中移除离职员工
        foreach (CrewMember fired in firedToday)
        {
            crew.Remove(fired);
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

        return maxMentorLevel >= 4 ? 2.0f : 1.5f;
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

    /// <summary>根据技能名与等级返回等级称谓（如"司机"、"技师"），等级越界 clamp 到边界。</summary>
    public static string GetRankName(string skillName, int level)
    {
        if (!RankNames.TryGetValue(skillName, out string[] ranks))
        {
            return "未培训";
        }
        return ranks[Mathf.Clamp(level, 0, ranks.Length - 1)];
    }
}