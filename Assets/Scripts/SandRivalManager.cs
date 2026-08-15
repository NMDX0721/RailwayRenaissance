using System.Collections.Generic;
using UnityEngine;

public static class SandRivalManager
{
    private static Dictionary<string, float> cityPenetration;
    private static int daysSinceLastAction;

    // G7: 关键节点城市列表（来自种子资源分布），铁龙收购优先目标
    private static string[] criticalNodes = System.Array.Empty<string>();

    // G8: 城市政治倾向表（来自种子城市数据），用于渗透增速倍率
    private static Dictionary<string, string> politicalLeanByCity = new Dictionary<string, string>();

    public static void Initialize()
    {
        cityPenetration = new Dictionary<string, float>();
        cityPenetration["wufeng"] = 0.15f;
        cityPenetration["kuangqu"] = 0.20f;
        cityPenetration["qingshi"] = 0.25f;
        cityPenetration["hekou"] = 0.35f;
        daysSinceLastAction = 0;
        criticalNodes = System.Array.Empty<string>();
        politicalLeanByCity.Clear();
    }

    /// <summary>从种子数据初始化城市渗透率，替代硬编码。</summary>
    public static void InitializeFromSeed(WorldGen.WorldSeedData seed)
    {
        cityPenetration = new Dictionary<string, float>();
        politicalLeanByCity = new Dictionary<string, string>();
        foreach (var kvp in seed.cities)
        {
            cityPenetration[kvp.Key] = kvp.Value.sandPenetrationBase;
            // G8: 记录城市政治倾向供渗透增速使用
            politicalLeanByCity[kvp.Key] = kvp.Value.politicalLean;
        }
        // G7: 从种子资源分布设置关键节点
        SetCriticalNodes(seed.resourceDistribution.criticalNodes);
        daysSinceLastAction = 0;
    }

    // ——— G7：关键节点管理 ———

    /// <summary>设置关键节点城市列表（铁龙收购优先目标）。</summary>
    public static void SetCriticalNodes(string[] cityIds)
    {
        criticalNodes = cityIds ?? System.Array.Empty<string>();
    }

    // ——— G8：政治倾向增速倍率 ———

    /// <summary>根据城市政治倾向返回渗透增速倍率。</summary>
    private static float GetPoliticalGrowthMultiplier(string cityId)
    {
        if (politicalLeanByCity.TryGetValue(cityId, out string lean))
        {
            switch (lean)
            {
                case "market":         return 1.3f; // 市场型：增速 +30%
                case "authoritarian":  return 0.8f; // 威权型：增速 -20%
                case "welfare":        return 0.9f; // 福利型：增速 -10%
                default:               return 1.0f; // 中立/未知：不变
            }
        }
        return 1.0f;
    }

    public static void DailyUpdate()
    {
        List<string> cities = new List<string>(cityPenetration.Keys);
        foreach (string city in cities)
        {
            // G8: 渗透增速按政治倾向调整（市场型快、威权型慢）
            float growth = 0.0015f * GetPoliticalGrowthMultiplier(city);
            cityPenetration[city] += growth;
            cityPenetration[city] = Mathf.Clamp01(cityPenetration[city]);
        }
        daysSinceLastAction++;
    }

    public static float GetPenetration(string cityId)
    {
        if (cityPenetration.ContainsKey(cityId))
            return cityPenetration[cityId];
        return 0.15f;
    }

    public static void SetPenetration(string cityId, float value)
    {
        cityPenetration[cityId] = Mathf.Clamp01(value);
    }

    public static SandAction CheckForAction()
    {
        if (daysSinceLastAction < 30)
            return null;

        daysSinceLastAction = 0;

        // G7: 关键节点城市优先，再按渗透率降序——合并候选列表
        string targetCity = null;
        float highestPen = 0;

        // 第一遍：仅遍历关键节点（已解锁的），取渗透率最高者
        foreach (string nodeId in criticalNodes)
        {
            if (cityPenetration.TryGetValue(nodeId, out float pen) && pen > highestPen)
            {
                highestPen = pen;
                targetCity = nodeId;
            }
        }

        // 第二遍：遍历所有城市，若普通城市渗透率更高则覆盖
        foreach (var kvp in cityPenetration)
        {
            if (kvp.Value > highestPen)
            {
                highestPen = kvp.Value;
                targetCity = kvp.Key;
            }
        }

        SandAction action = new SandAction();
        action.cityId = targetCity;

        if (highestPen < 0.30f)
        {
            action.type = "ad_campaign";
            action.title = "沙能广告攻势";
            action.description = "沙能公司在" + targetCity + "发起广告宣传，乘客信任度下降";
            action.penetrationDelta = 0.05f;
            action.trustDelta = -3;
        }
        else if (highestPen < 0.50f)
        {
            action.type = "free_trial";
            action.title = "沙能免费试乘";
            action.description = "沙能公司在" + targetCity + "推出免费试乘活动，部分乘客流失";
            action.penetrationDelta = 0.03f;
            action.trustDelta = -5;
        }
        else
        {
            action.type = "price_war";
            action.title = "沙能价格战";
            action.description = "沙能公司在" + targetCity + "大幅降价，铁路收入受到冲击";
            action.penetrationDelta = 0.02f;
            action.trustDelta = -2;
            action.incomeMultiplier = 0.85f;
        }

        return action;
    }

    public static void PlayerPR(string cityId, int cost)
    {
        if (cityPenetration.ContainsKey(cityId))
        {
            cityPenetration[cityId] -= cost * 0.0001f;
            cityPenetration[cityId] = Mathf.Clamp01(cityPenetration[cityId]);
        }
    }
}

[System.Serializable]
public class SandAction
{
    public string type;
    public string cityId;
    public string title;
    public string description;
    public float penetrationDelta;
    public int trustDelta;
    public float incomeMultiplier = 1.0f;
}