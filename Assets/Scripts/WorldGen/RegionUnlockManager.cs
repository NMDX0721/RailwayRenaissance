using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorldGen
{
    /// <summary>
    /// 区域解锁管理器——根据运营天数、资金、信任等条件逐区域解锁。
    /// 对齐《区域解锁与政治系统设计.md》§1.1 六区域结构。
    /// 
    /// 区域0：雾峰村（初始解锁）      → 恒 true
    /// 区域1：青溪镇 → 运营>60天 + 资金>50000 + 剧情"老陈的请求"
    /// 区域2：云渡港 → 运营>120天 + 信任>65 + 剧情"市长的邀请"
    /// 区域3：白鹭洲 → 运营>200天 + 累计盈利>100000 + 剧情"铁路联盟"
    /// 区域4：枫林渡 → 运营>300天 + 科技树分支
    /// 区域5：望海港 → 运营>450天 + 主线剧情完成
    /// </summary>
    public static class RegionUnlockManager
    {
        // 区域→城市列表映射（从种子数据初始化）
        private static Dictionary<int, List<string>> regionCities = new Dictionary<int, List<string>>();

        // 已解锁区域集合
        private static HashSet<int> unlockedRegions = new HashSet<int>();

        // 解锁回调（供 Layer6 事件系统接入）
        private static Action<int> onRegionUnlocked;

        // 是否已初始化
        private static bool initialized = false;

        /// <summary>
        /// 从种子数据初始化区域城市映射。
        /// 遍历 seed.cities，按 unlockRegion 分组，存入区域→城市映射。
        /// 区域0恒解锁。
        /// </summary>
        public static void Initialize(WorldSeedData seed)
        {
            regionCities.Clear();
            unlockedRegions.Clear();

            if (seed == null || seed.cities == null)
            {
                Debug.LogWarning("[RegionUnlockManager] 种子数据为空，使用空映射");
                return;
            }

            // 遍历所有城市，按 unlockRegion 分组
            foreach (var kvp in seed.cities)
            {
                string cityId = kvp.Key;
                CityData city = kvp.Value;
                int region = city.unlockRegion;

                if (!regionCities.ContainsKey(region))
                {
                    regionCities[region] = new List<string>();
                }
                regionCities[region].Add(cityId);
            }

            // 区域0恒解锁（新手区）
            unlockedRegions.Add(0);

            initialized = true;
            Debug.Log($"[RegionUnlockManager] 初始化完成，共 {seed.cities.Count} 城市，{regionCities.Count} 区域");
        }

        /// <summary>
        /// 返回区域0-5当前解锁状态。
        /// </summary>
        /// <param name="region">区域编号 0-5</param>
        public static bool IsRegionUnlocked(int region)
        {
            if (unlockedRegions.Contains(region))
                return true;

            // 数值条件（剧情条件因剧本系统未接入，先用数值条件，留 TODO 注释）
            switch (region)
            {
                case 0:
                    // 初始区域，恒解锁
                    return true;

                case 1:
                    // 运营>60天 + 资金>50000
                    // TODO: 追加剧情条件 "老陈的请求"
                    return GameData.Day > 60 && GameData.Money > 50000;

                case 2:
                    // 运营>120天 + 信任>65
                    // TODO: 追加剧情条件 "市长的邀请"
                    return GameData.Day > 120 && GameData.Trust > 65;

                case 3:
                    // 运营>200天 + 累计盈利>100000
                    // 累计盈利暂无独立字段，用 Money > 100000 作代理
                    // TODO: 追加剧情条件 "铁路联盟"
                    return GameData.Day > 200 && GameData.Money > 100000;

                case 4:
                    // 运营>300天
                    // TODO: 追加科技树分支条件
                    return GameData.Day > 300;

                case 5:
                    // 运营>450天
                    // TODO: 追加主线剧情完成条件
                    return GameData.Day > 450;

                default:
                    Debug.LogWarning($"[RegionUnlockManager] 未知区域编号: {region}");
                    return false;
            }
        }

        /// <summary>
        /// 获取某区域包含的城市名称列表（来自种子数据）。
        /// </summary>
        /// <param name="region">区域编号 0-5</param>
        public static string[] GetRegionCities(int region)
        {
            if (regionCities.TryGetValue(region, out var cities))
            {
                return cities.ToArray();
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// 获取当前已解锁的所有城市ID（供 UI/订单/招募使用）。
        /// </summary>
        public static List<string> GetUnlockedCityIds()
        {
            var result = new List<string>();
            foreach (int region in unlockedRegions)
            {
                if (regionCities.TryGetValue(region, out var cities))
                {
                    result.AddRange(cities);
                }
            }
            return result;
        }

        /// <summary>
        /// 注册解锁回调（用于触发剧情事件，Layer6 事件系统接入点）。
        /// </summary>
        /// <param name="callback">回调参数为解锁的区域编号</param>
        public static void RegisterUnlockCallback(Action<int> callback)
        {
            onRegionUnlocked += callback;
        }

        /// <summary>
        /// 每日检查：遍历未解锁区域，条件满足则标记解锁并触发回调。
        /// 由 GameData.AdvanceDay 在 Day += 1 后调用。
        /// GameData 未初始化时静默返回，不崩溃。
        /// </summary>
        public static void DailyCheck()
        {
            if (!initialized)
            {
                // GameData 未初始化时静默返回，不崩溃
                return;
            }

            for (int region = 1; region <= 5; region++)
            {
                if (unlockedRegions.Contains(region))
                    continue;

                if (IsRegionUnlocked(region))
                {
                    unlockedRegions.Add(region);
                    Debug.Log($"[RegionUnlockManager] 区域 {region} 已解锁！");

                    // 触发解锁回调（供 UI 弹窗/剧情接入）
                    onRegionUnlocked?.Invoke(region);
                }
            }
        }
    }
}