using UnityEngine;

namespace WorldGen
{
    /// <summary>
    /// 千里马创世核 Layer 5 注入点。
    /// 将 WorldSeedData 中的参数注入各运行时系统，代替硬编码初始值。
    /// </summary>
    public static class WorldInitializer
    {
        /// <summary>将种子数据注入各运行时系统。</summary>
        public static void ApplySeed(WorldSeedData seed)
        {
            if (seed == null)
            {
                Debug.LogWarning("[WorldInitializer] seed is null, skipping injection");
                return;
            }

            // 1. 初始化 SandRivalManager 城市渗透（从种子 cities 读取 sandPenetrationBase）
            SandRivalManager.InitializeFromSeed(seed);

            // 2. 覆盖 GameData 初始趋势值（信任、车况等）
            GameData.ApplySeedInitialValues(seed);

            // 3. 输出事件基准概率日志（供事件编辑器参考，不修改 EventManager 逻辑）
            float baseChance = seed.globalRules.incidentBaseChance;
            Debug.Log($"[WorldInitializer] 种子 {seed.seedId} 已注入各系统，事件基准概率 incidentBaseChance={baseChance}");
        }
    }
}