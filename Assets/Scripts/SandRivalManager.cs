using System.Collections.Generic;
using UnityEngine;

public static class SandRivalManager
{
    private static Dictionary<string, float> cityPenetration;
    private static int daysSinceLastAction;

    public static void Initialize()
    {
        cityPenetration = new Dictionary<string, float>();
        cityPenetration["wufeng"] = 0.15f;
        cityPenetration["kuangqu"] = 0.20f;
        cityPenetration["qingshi"] = 0.25f;
        cityPenetration["hekou"] = 0.35f;
        daysSinceLastAction = 0;
    }

    /// <summary>从种子数据初始化城市渗透率，替代硬编码。</summary>
    public static void InitializeFromSeed(WorldGen.WorldSeedData seed)
    {
        cityPenetration = new Dictionary<string, float>();
        foreach (var kvp in seed.cities)
        {
            cityPenetration[kvp.Key] = kvp.Value.sandPenetrationBase;
        }
        daysSinceLastAction = 0;
    }

    public static void DailyUpdate()
    {
        List<string> cities = new List<string>(cityPenetration.Keys);
        foreach (string city in cities)
        {
            cityPenetration[city] += 0.0015f;
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

        string targetCity = null;
        float highestPen = 0;
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