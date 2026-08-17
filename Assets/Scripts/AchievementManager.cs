using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>成就稀有度。</summary>
public enum AchievementRarity
{
    Common, // 普通
    Rare,   // 稀有
    Epic,   // 史诗
    Legend  // 传说
}

/// <summary>单个成就数据（含运行时解锁状态）。</summary>
public class AchievementData
{
    public string id;
    public string title;
    public string description;
    public AchievementRarity rarity;
    public bool unlocked;
    public string unlockedDate;
}

/// <summary>成就存档记录（JsonUtility 可序列化）。</summary>
[Serializable]
public class AchievementSaveEntry
{
    public string id;
    public bool unlocked;
    public string unlockedDate;
}

/// <summary>成就存档容器。</summary>
[Serializable]
public class AchievementSaveData
{
    public List<AchievementSaveEntry> entries = new List<AchievementSaveEntry>();
}

/// <summary>成就系统：荣誉成就数据与持久化（数据来自 经济系统.md §9.5）。</summary>
public static class AchievementManager
{
    public const string SaveKey = "Achievements_Data";

    /// <summary>7个荣誉成就定义（保持此顺序展示，即文档顺序）。</summary>
    private static readonly AchievementData[] Definitions =
    {
        new AchievementData { id = "railway_newbie", title = "铁路新人",     description = "完成第一个月运营",             rarity = AchievementRarity.Common },
        new AchievementData { id = "safety_star",    title = "安全之星",     description = "连续90天无事故",               rarity = AchievementRarity.Rare },
        new AchievementData { id = "service_master", title = "服务大师",     description = "乘客满意度连续30天>90",        rarity = AchievementRarity.Rare },
        new AchievementData { id = "mentor_expert",  title = "培养专家",     description = "培养1名员工从1级到5级",        rarity = AchievementRarity.Epic },
        new AchievementData { id = "revivalist",     title = "铁路复兴者",   description = "首次月度盈利10万",             rarity = AchievementRarity.Epic },
        new AchievementData { id = "entrepreneur",   title = "铁路企业家",   description = "累计盈利100万",                rarity = AchievementRarity.Legend },
        new AchievementData { id = "legend",         title = "铁路传奇",     description = "完成所有终极目标",             rarity = AchievementRarity.Legend },
    };

    private static readonly List<AchievementData> items = new List<AchievementData>();
    private static readonly Dictionary<string, int> indexById = new Dictionary<string, int>();
    private static bool initialized;

    /// <summary>初始化成就表并加载存档。幂等，可重复调用。</summary>
    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;

        items.Clear();
        indexById.Clear();
        for (int i = 0; i < Definitions.Length; i++)
        {
            var def = Definitions[i];
            var copy = new AchievementData
            {
                id = def.id,
                title = def.title,
                description = def.description,
                rarity = def.rarity,
                unlocked = false,
                unlockedDate = null
            };
            items.Add(copy);
            indexById[copy.id] = i;
        }

        Load();
    }

    /// <summary>解锁指定成就（不存在返回 false；已解锁保持幂等并返回 true）。解锁后立即持久化，并触发右下角弹窗。</summary>
    public static bool Unlock(string id)
    {
        Initialize();
        if (!indexById.TryGetValue(id, out int idx)) return false;
        var data = items[idx];
        if (data.unlocked) return true;

        data.unlocked = true;
        data.unlockedDate = DateTime.Now.ToString("yyyy-MM-dd");
        Save();

        AchievementToast.ShowAchievement(data);
        return true;
    }

    /// <summary>查询指定成就是否已解锁。</summary>
    public static bool IsUnlocked(string id)
    {
        Initialize();
        return indexById.TryGetValue(id, out int idx) && items[idx].unlocked;
    }

    /// <summary>获取全部成就（按定义顺序，含解锁状态）。</summary>
    public static AchievementData[] GetAll()
    {
        Initialize();
        return items.ToArray();
    }

    /// <summary>已解锁成就数量。</summary>
    public static int GetUnlockedCount()
    {
        Initialize();
        int count = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].unlocked) count++;
        }
        return count;
    }

    /// <summary>将解锁状态写入 PlayerPrefs（JSON）。</summary>
    public static void Save()
    {
        var data = new AchievementSaveData();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            data.entries.Add(new AchievementSaveEntry
            {
                id = item.id,
                unlocked = item.unlocked,
                unlockedDate = item.unlockedDate
            });
        }
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    /// <summary>从 PlayerPrefs 读回解锁状态；数据损坏/缺失时保持当前表不变。</summary>
    public static void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var data = JsonUtility.FromJson<AchievementSaveData>(json);
            if (data == null || data.entries == null) return;

            for (int i = 0; i < data.entries.Count; i++)
            {
                var entry = data.entries[i];
                if (entry == null || entry.id == null) continue;
                if (!indexById.TryGetValue(entry.id, out int idx)) continue;
                items[idx].unlocked = entry.unlocked;
                items[idx].unlockedDate = entry.unlockedDate;
            }
        }
        catch (Exception)
        {
            // 存档损坏时忽略，保持内存中的默认值即可
        }
    }
}