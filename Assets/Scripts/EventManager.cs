using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    private static List<GameEvent> eventTemplates;
    private static GameEvent currentEvent;
    private static int eventCooldown;

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
            if (Random.Range(0f, 1f) < evt.probability)
            {
                currentEvent = evt;
                eventCooldown = Random.Range(3, 7);
                return evt;
            }
        }
        return null;
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