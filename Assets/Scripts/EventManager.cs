using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    private static List<GameEvent> eventTemplates;
    private static GameEvent currentEvent;
    private static int eventCooldown;

    // G9: 资源分布 pattern 缓存（来自种子），用于事件概率修正
    private static string resourcePattern = "dispersed";

    public static void Initialize()
    {
        TextAsset json = Resources.Load<TextAsset>("events");
        if (json == null)
        {
            Debug.LogWarning("[EventManager] events.json not found at Resources/events.json");
            eventTemplates = new List<GameEvent>();
            return;
        }
        EventList wrapper = JsonUtility.FromJson<EventList>(json.text);
        if (wrapper != null && wrapper.events != null)
        {
            eventTemplates = new List<GameEvent>(wrapper.events);
        }
        else
        {
            eventTemplates = new List<GameEvent>();
        }
        eventCooldown = 0;
        resourcePattern = "dispersed";
    }

    /// <summary>G9: 从种子设置资源分布 pattern，影响后续事件概率修正。</summary>
    public static void SetPatternModifiers(WorldGen.WorldSeedData seed)
    {
        if (seed == null)
        {
            resourcePattern = "dispersed";
            return;
        }
        resourcePattern = seed.resourceDistribution.pattern;
    }

    public static GameEvent TryTriggerEvent()
    {
        if (eventTemplates == null || eventTemplates.Count == 0)
            return null;

        if (eventCooldown > 0)
        {
            eventCooldown--;
            return null;
        }

        foreach (GameEvent evt in eventTemplates)
        {
            // G9: 根据资源分布 pattern 修正事件触发概率
            float adjustedProb = evt.probability * GetPatternMultiplier(evt.type);
            if (Random.Range(0f, 1f) < adjustedProb)
            {
                currentEvent = evt;
                eventCooldown = Random.Range(3, 7);
                return evt;
            }
        }
        return null;
    }

    /// <summary>G9: 根据资源分布 pattern 和事件类型，返回概率修正系数。</summary>
    private static float GetPatternMultiplier(string eventType)
    {
        if (string.IsNullOrEmpty(eventType)) return 1.0f;

        string lowerType = eventType.ToLower();

        switch (resourcePattern)
        {
            case "concentrated":
                // 集中型：单点故障事件（accident/breakdown）概率 ×1.5
                if (lowerType.Contains("accident") || lowerType.Contains("breakdown"))
                    return 1.5f;
                return 1.0f;

            case "political":
                // 政治型：政府干预事件（policy/tax）概率 ×1.5
                if (lowerType.Contains("policy") || lowerType.Contains("tax"))
                    return 1.5f;
                return 1.0f;

            case "dispersed":
                // 分散型：所有事件概率 ×1.2（多线影响）
                return 1.2f;

            default:
                return 1.0f;
        }
    }

    public static GameEvent GetCurrentEvent()
    {
        return currentEvent;
    }

    public static void ClearEvent()
    {
        currentEvent = null;
    }
}

[System.Serializable]
public class GameEvent
{
    public string id;
    public string type;
    public string title;
    public string description;
    public float probability;
    public EventEffects effects;
    public int duration;
}

[System.Serializable]
public class EventEffects
{
    public int moneyDelta;
    public int trustDelta;
    public int trainConditionDelta;
    public int passengerDelta;
    public float fuelCostMultiplier = 1.0f;
    public float passengerMultiplier = 1.0f;
    public int efficiencyDelta;
}

[System.Serializable]
public class EventList
{
    public GameEvent[] events;
}