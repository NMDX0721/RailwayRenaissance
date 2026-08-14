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
    public int loyalty;
    public SkillData[] skills;
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

    public static void DailyUpdate()
    {
        foreach (CrewMember member in crew)
        {
            // 每日疲劳+10
            member.fatigue += 10;

            // 在岗技能经验+1
            foreach (SkillData skill in member.skills)
            {
                if (skill.level < skill.maxLevel)
                {
                    skill.exp += 1;
                }
            }
        }

        Debug.Log("[CrewManager] 每日更新完成。");
    }
}