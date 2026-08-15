namespace Narrative
{
    /// <summary>叙事模板（N1.3）——按世界状态匹配、参数化填充、携带效果的事件模板。</summary>
    [System.Serializable]
    public class NarrativeTemplate
    {
        public string id, type; // type: news/dialogue/order/event
        public float priority;
        public int cooldownDays;
        public TemplateConditions conditions;
        public string templateText; // 含 {city} {player} {value} 占位符
        public string[] parameters; // 参数数组
        public NarrativeEffects effects; // 事件效果
    }

    /// <summary>模板触发条件（N1.3）——按数值区间/城市解锁/事件去重判定。</summary>
    [System.Serializable]
    public class TemplateConditions
    {
        public float trustMin, trustMax;
        public float penetrationMin, penetrationMax;
        public int fatigueMin; // 触发疲劳对话
        public string[] requiredCityIds; // 需要已解锁的城市
        public string[] excludedEventIds; // 排除已触发的事件
    }

    /// <summary>叙事效果（N1.3）——事件触发后对游戏状态的数值变化。</summary>
    [System.Serializable]
    public class NarrativeEffects
    {
        public int moneyDelta;
        public int trustDelta;
        public int trainConditionDelta;
        public int passengerDelta;
        public float sandPenetrationDelta;
        public int fatigueDelta;
    }

    /// <summary>叙事事件产出（N1.4）——模板匹配+填充后的最终事件，交由目标系统消费。</summary>
    [System.Serializable]
    public class NarrativeEvent
    {
        public string templateId, eventType; // news/dialogue/order/event
        public string targetSystem; // VN/GameData/OrderManager
        public string content; // 填充后的文本
        public NarrativeEffects effects;
        public string targetCityId;
        public string targetNpcId;
    }
}