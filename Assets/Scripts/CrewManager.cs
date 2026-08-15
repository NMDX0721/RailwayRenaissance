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
}

[Serializable]
public class SkillData
{
    public string skillName;
    public int level;
    public int maxLevel;
    public int exp;
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

    // —— G11：城市 npc_pool → 招募角色池（key=城市ID，value=可招募角色ID列表） ——
    private static Dictionary<string, string[]> recruitingPools = new Dictionary<string, string[]>();
    private static HashSet<string> recruitedIds = new HashSet<string>();

    public static void Initialize()
    {
        crew.Clear();

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

            // 在岗技能经验+1
            foreach (SkillData skill in member.skills)
            {
                if (skill.level < skill.maxLevel)
                {
                    skill.exp += 1;
                }
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
}