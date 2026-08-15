using System.Collections.Generic;
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

            // G4：保存种子引用供经济核读取（客流基准/货运收入/中断检测）
            GameData.CurrentSeed = seed;

            // G3：初始化区域解锁映射（G10/G5 的已解锁城市名单依赖它）
            RegionUnlockManager.Initialize(seed);

            // 1. 初始化 SandRivalManager 城市渗透（从种子 cities 读取 sandPenetrationBase）
            SandRivalManager.InitializeFromSeed(seed);

            // 2. 覆盖 GameData 初始趋势值（信任、车况等）
            GameData.ApplySeedInitialValues(seed);

            // 3. 输出事件基准概率日志（供事件编辑器参考，不修改 EventManager 逻辑）
            float baseChance = seed.globalRules.incidentBaseChance;
            Debug.Log($"[WorldInitializer] 种子 {seed.seedId} 已注入各系统，事件基准概率 incidentBaseChance={baseChance}");

            // G9: 资源分布 pattern → 事件概率修正
            EventManager.SetPatternModifiers(seed);

            // G11：城市 npc_pool → 招募角色池
            Dictionary<string, string[]> npcPools = new Dictionary<string, string[]>();
            foreach (var kvp in seed.cities)
            {
                npcPools[kvp.Key] = kvp.Value.npcPool;
            }
            CrewManager.SetRecruitingPools(npcPools);
        }
    }
}