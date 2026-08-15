using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class OrderManager
{
    private static List<GameOrder> activeOrders;
    private static int completedCount;
    private static List<GameOrder> templatePool;
    private static HashSet<string> completedOrderIds = new HashSet<string>();
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized) return;
        activeOrders = new List<GameOrder>();
        completedCount = 0;
        LoadTemplates();
        initialized = true;
    }

    private static void LoadTemplates()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("orders");
        if (jsonAsset == null)
        {
            Debug.LogWarning("[OrderManager] orders.json not found in Resources");
            templatePool = new List<GameOrder>();
            return;
        }

        OrderList orderList = JsonUtility.FromJson<OrderList>(jsonAsset.text);
        if (orderList != null && orderList.orders != null)
        {
            templatePool = new List<GameOrder>(orderList.orders);
            Debug.Log("[OrderManager] Loaded " + templatePool.Count + " order templates");
        }
        else
        {
            Debug.LogWarning("[OrderManager] Failed to parse orders.json");
            templatePool = new List<GameOrder>();
        }
    }

    public static void GenerateDailyOrders()
    {
        InitializeIfNeeded();

        // 清除过期订单（超过截止日期）
        activeOrders.RemoveAll(o => o.DaysRemaining <= 0);

        // 根据当前季节和趋势筛选可用模板
        float seasonModifier = GetSeasonModifier();
        string trendType = GetCurrentTrendType();

        // G10：从种子已解锁城市产业构建支持的订单类型池
        HashSet<string> supportedTypes = null;
        if (GameData.CurrentSeed != null)
        {
            supportedTypes = new HashSet<string>();
            var unlockedCityIds = WorldGen.RegionUnlockManager.GetUnlockedCityIds();
            foreach (string cityId in unlockedCityIds)
            {
                if (!GameData.CurrentSeed.cities.TryGetValue(cityId, out var city))
                    continue;
                foreach (string industry in city.industries)
                {
                    // 产业→订单类型映射：煤/铁/机械→货运；茶/旅游→剧情；港口/贸易→紧急
                    if (industry == "coal" || industry == "iron" || industry == "machinery")
                        supportedTypes.Add("freight");
                    else if (industry == "tea" || industry == "tourism")
                        supportedTypes.Add("story");
                    else if (industry == "shipping" || industry == "trade")
                        supportedTypes.Add("urgent");
                }
            }
        }

        List<GameOrder> candidates = new List<GameOrder>();
        foreach (GameOrder template in templatePool)
        {
            if (completedOrderIds.Contains(template.id)) continue;

            // 季节相关性：freight 在秋季优先，story 在春季优先
            if (template.type == "freight" && seasonModifier > 1.0f && Random.value < 0.3f) continue;
            if (template.type == "story" && seasonModifier < 0.95f && Random.value < 0.3f) continue;

            // G10：城市产业过滤——若模板类型与城市产业不匹配，50%概率跳过
            if (supportedTypes != null && !supportedTypes.Contains(template.type))
            {
                if (Random.value < 0.5f) continue;
            }

            candidates.Add(template);
        }

        // 生成今日订单：1-2个货运 + 0-1个紧急 + 0-1个剧情
        int freightCount = Mathf.Min(Random.Range(1, 3), candidates.Count(o => o.type == "freight"));
        int urgentCount = Mathf.Min(Random.Range(0, 2), candidates.Count(o => o.type == "urgent"));
        int storyCount = Mathf.Min(Random.Range(0, 2), candidates.Count(o => o.type == "story"));

        List<GameOrder> todayOrders = new List<GameOrder>();
        todayOrders.AddRange(PickFromPool(candidates, "freight", freightCount));
        todayOrders.AddRange(PickFromPool(candidates, "urgent", urgentCount));
        todayOrders.AddRange(PickFromPool(candidates, "story", storyCount));

        foreach (GameOrder order in todayOrders)
        {
            // 克隆模板并设置天数
            GameOrder instance = CloneOrder(order);
            instance.DaysRemaining = instance.deadlineDays;
            activeOrders.Add(instance);
        }
    }

    private static List<GameOrder> PickFromPool(List<GameOrder> pool, string type, int count)
    {
        List<GameOrder> filtered = pool.FindAll(o => o.type == type);
        List<GameOrder> picked = new List<GameOrder>();

        // 随机打乱后取前 count 个
        for (int i = filtered.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            GameOrder tmp = filtered[i];
            filtered[i] = filtered[j];
            filtered[j] = tmp;
        }

        for (int i = 0; i < Mathf.Min(count, filtered.Count); i++)
        {
            picked.Add(filtered[i]);
        }

        return picked;
    }

    private static GameOrder CloneOrder(GameOrder source)
    {
        return new GameOrder
        {
            id = source.id,
            type = source.type,
            title = source.title,
            description = source.description,
            origin = source.origin,
            destination = source.destination,
            reward = source.reward,
            deadlineDays = source.deadlineDays,
            trustDelta = source.trustDelta,
            chainId = source.chainId,
            DaysRemaining = source.deadlineDays
        };
    }

    public static void CompleteOrder(string orderId)
    {
        InitializeIfNeeded();

        GameOrder order = activeOrders.Find(o => o.id == orderId);
        if (order == null)
        {
            Debug.LogWarning("[OrderManager] Cannot complete order " + orderId + ": not found in active orders");
            return;
        }

        // 结算奖励
        GameData.AddMoney(order.reward);
        GameData.AddTrust(order.trustDelta);

        activeOrders.Remove(order);
        completedOrderIds.Add(orderId);
        completedCount++;

        Debug.Log("[OrderManager] Completed order: " + order.title + " (+" + order.reward + " money, +" + order.trustDelta + " trust)");
    }

    public static List<GameOrder> GetActiveOrders()
    {
        InitializeIfNeeded();
        return new List<GameOrder>(activeOrders);
    }

    public static int GetCompletedCount()
    {
        return completedCount;
    }

    public static void DailyTick()
    {
        InitializeIfNeeded();
        foreach (GameOrder order in activeOrders)
        {
            order.DaysRemaining--;
        }

        // 移除过期的订单
        int expired = activeOrders.RemoveAll(o => o.DaysRemaining <= 0);
        if (expired > 0)
        {
            Debug.Log("[OrderManager] " + expired + " orders expired due to deadline");
        }
    }

    /// <summary>根据当前季节获取订单权重修正因子。</summary>
    private static float GetSeasonModifier()
    {
        // 简单模拟：基于 GameData.Day 计算季节
        int month = (GameData.Day % 360) / 30;
        // 春夏秋冬
        if (month < 3) return 1.2f;  // 春季：旅游/故事倾向
        if (month < 6) return 1.15f; // 夏季：运输旺季
        if (month < 9) return 1.0f;  // 秋季：货运旺季
        return 0.9f;                 // 冬季：整体淡季
    }

    /// <summary>获取当前趋势类型（扩展用）。</summary>
    private static string GetCurrentTrendType()
    {
        // 预留：根据游戏状态返回趋势类型
        // 例如 Trust 高时倾向故事订单，Money 低时倾向货运订单
        if (GameData.Trust >= 80) return "story";
        if (GameData.Money < 5000) return "freight";
        return "balanced";
    }

    private static void InitializeIfNeeded()
    {
        if (!initialized) Initialize();
    }
}

[System.Serializable]
public class GameOrder
{
    public string id;
    public string type; // "freight" / "urgent" / "story"
    public string title;
    public string description;
    public string origin;
    public string destination;
    public int reward;
    public int deadlineDays;
    public int trustDelta;
    public string chainId;

    // 运行时字段（不序列化到JSON）
    [System.NonSerialized]
    public int DaysRemaining;
}

[System.Serializable]
public class OrderList
{
    public GameOrder[] orders;
}