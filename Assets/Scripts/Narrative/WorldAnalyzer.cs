using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narrative
{
    /// <summary>
    /// 世界状态分析器（Layer 2）——读取 GameData/SandRivalManager/CrewManager/创世核/区域解锁
    /// 的当前运行状态，输出完整 NarrativeContext（叙事引擎的输入快照）。
    /// 覆盖 N2.1-N2.8，含 GAP1（区域解锁）/GAP6（铁龙行动）/GAP7（疲劳触发）修复。
    /// </summary>
    public static class WorldAnalyzer
    {
        // —— 阈值常量（核心玩法循环 §4）——
        private const float TrustWarning = 0.40f;          // 信任 <40% → 预警
        private const float TrustIrreversible = 0.30f;     // 信任 <30% → 不可逆
        private const float SandWarning = 0.40f;           // 沙能渗透 >40% → 预警
        private const float SandIrreversible = 0.55f;      // 沙能渗透 >55% → 不可逆
        private const float PressureWarning = 0.60f;       // 财政/政治/设施衰减 >60% → 预警（默认防御值，待 §4 对齐）
        private const float PressureIrreversible = 0.80f;  // 财政/政治/设施衰减 >80% → 不可逆
        private const int FatigueTrigger = 60;             // 疲劳 >60 → 触发 NPC 对话（GAP7）
        private const int LoyaltyTrigger = 30;             // 忠诚 <30 → 触发 NPC 对话（GAP7）

        // GAP1：上一次分析的区域快照，差分出"刚解锁"城市（首调用作基线，不误报）
        private static string[] lastUnlockedCityIds;

        /// <summary>读取当前世界状态，输出完整 NarrativeContext。</summary>
        public static NarrativeContext Analyze()
        {
            var ctx = new NarrativeContext();

            // 2.1 趋势线读取：GameData 有的直接读，没有的回退 seed.initialTrends / 默认值
            ctx.gameDay = GameData.Day;
            ctx.trustTrend = GameData.Trust / 100f; // int 0-100 → float 0-1（与 seed.initialTrends.trust 同口径）
            ctx.sandPenetration = GetSandPenetration();
            ctx.fiscalTrend = GetFiscalTrend();
            ctx.politicalPressure = GameData.CurrentSeed != null
                ? GameData.CurrentSeed.initialTrends.politicalPressure
                : 0.20f;
            // 设施衰减：GameData 无独立字段，用车况反向推导（(100-车况)/100，初始 70 → 0.30 与种子默认一致）
            ctx.infrastructureDecay = (100f - GameData.TrainCondition) / 100f;

            // 2.2 阈值检测
            ctx.trustCrossedThreshold = ctx.trustTrend < TrustWarning;
            ctx.fiscalCrossedThreshold = ctx.fiscalTrend > PressureWarning;

            // 2.3 角色状态读取（CrewManager 疲劳/忠诚 + 好感度占位，后续由 N5 更新）
            ctx.characterStates = ReadCharacterStates();

            // 2.4 种子数据读取（G4 已写入 GameData.CurrentSeed）
            ctx.seed = GameData.CurrentSeed;

            // 2.5 区域解锁读取（GAP1）
            List<string> unlocked = WorldGen.RegionUnlockManager.GetUnlockedCityIds();
            ctx.unlockedCityIds = unlocked != null ? unlocked.ToArray() : Array.Empty<string>();
            ctx.justUnlockedRegionCityIds = GetJustUnlockedCityIds(ctx.unlockedCityIds);

            // 2.6 铁龙行动读取（GAP6）
            // 注意：CheckForAction() 在间隔期满足时会重置 daysSinceLastAction=0（消费掉本次行动），
            // 因此 Analyze 应在 AdvanceDay 的沙能竞争逻辑之后调用，避免重复消费。
            SandAction sandAction = SandRivalManager.CheckForAction();
            ctx.sandRivalActionType = sandAction != null ? sandAction.type : null;

            // 2.7 疲劳触发检测（GAP7）
            ctx.fatiguedNpcIds = DetectFatiguedNpcs();

            // 2.8 最近事件：EventManager 日志未接入，留接口（防重复/冷却由 N6 处理）
            ctx.recentEventIds = new List<string>();

            return ctx;
        }

        /// <summary>
        /// 阈值检测：逐条趋势线检测是否越过阈值，返回当前最严重的越阈事件（每线至多一条）。
        /// 信任/财政为低优（Falling），渗透/政治/衰减为高恶（Rising）。
        /// </summary>
        public static List<ThresholdEvent> DetectThresholdCrossings()
        {
            var events = new List<ThresholdEvent>();

            // 信任：越低越危险
            float trust = GameData.Trust / 100f;
            if (trust < TrustIrreversible)
                events.Add(NewThreshold("trust", TrustIrreversible, TrendDirection.Falling, "irreversible"));
            else if (trust < TrustWarning)
                events.Add(NewThreshold("trust", TrustWarning, TrendDirection.Falling, "warning"));

            // 沙能渗透：越高越危险
            float sand = GetSandPenetration();
            if (sand > SandIrreversible)
                events.Add(NewThreshold("sand", SandIrreversible, TrendDirection.Rising, "irreversible"));
            else if (sand > SandWarning)
                events.Add(NewThreshold("sand", SandWarning, TrendDirection.Rising, "warning"));

            // 财政压力：越高越危险
            float fiscal = GetFiscalTrend();
            if (fiscal > PressureIrreversible)
                events.Add(NewThreshold("fiscal", PressureIrreversible, TrendDirection.Rising, "irreversible"));
            else if (fiscal > PressureWarning)
                events.Add(NewThreshold("fiscal", PressureWarning, TrendDirection.Rising, "warning"));

            // 政治压力
            float political = GameData.CurrentSeed != null
                ? GameData.CurrentSeed.initialTrends.politicalPressure
                : 0.20f;
            if (political > PressureIrreversible)
                events.Add(NewThreshold("political", PressureIrreversible, TrendDirection.Rising, "irreversible"));
            else if (political > PressureWarning)
                events.Add(NewThreshold("political", PressureWarning, TrendDirection.Rising, "warning"));

            // 设施衰减
            float decay = (100f - GameData.TrainCondition) / 100f;
            if (decay > PressureIrreversible)
                events.Add(NewThreshold("decay", PressureIrreversible, TrendDirection.Rising, "irreversible"));
            else if (decay > PressureWarning)
                events.Add(NewThreshold("decay", PressureWarning, TrendDirection.Rising, "warning"));

            return events;
        }

        /// <summary>趋势变化方向：对比当前值与上一周期值。</summary>
        public static TrendDirection GetTrendDirection(float current, float previous)
        {
            const float epsilon = 0.001f;
            if (current > previous + epsilon) return TrendDirection.Rising;
            if (current < previous - epsilon) return TrendDirection.Falling;
            return TrendDirection.Stable;
        }

        /// <summary>疲劳/忠诚越过阈值的员工 ID（GAP7）：疲劳&gt;60 或 忠诚&lt;30。</summary>
        public static string[] DetectFatiguedNpcs()
        {
            var result = new List<string>();
            List<CrewMember> crew = CrewManager.GetAllCrew();
            if (crew == null) return result.ToArray();

            foreach (CrewMember member in crew)
            {
                if (member == null) continue;
                if (member.fatigue > FatigueTrigger || member.loyalty < LoyaltyTrigger)
                {
                    result.Add(member.id);
                }
            }
            return result.ToArray();
        }

        // ===== 内部辅助 =====

        /// <summary>沙能渗透率：已解锁城市均值，未解锁城市场景回退种子初始渗透 / 主线城市。</summary>
        private static float GetSandPenetration()
        {
            List<string> unlocked = WorldGen.RegionUnlockManager.GetUnlockedCityIds();
            if (unlocked != null && unlocked.Count > 0)
            {
                float sum = 0f;
                foreach (string cityId in unlocked)
                {
                    sum += SandRivalManager.GetPenetration(cityId);
                }
                return sum / unlocked.Count;
            }

            if (GameData.CurrentSeed != null)
                return GameData.CurrentSeed.initialTrends.sandPenetration;
            return SandRivalManager.GetPenetration("wufeng");
        }

        /// <summary>财政压力：种子初始值 + 当月深度亏损上浮（口径同 GameData.AdvanceDay 财政压力告警）。</summary>
        private static float GetFiscalTrend()
        {
            float fiscal = GameData.CurrentSeed != null
                ? GameData.CurrentSeed.initialTrends.fiscalPressure
                : 0.30f;

            if (GameData.CurrentMonth != null && GameData.CurrentMonth.NetProfit < -50000)
                fiscal = Mathf.Min(1f, fiscal + 0.2f);

            return fiscal;
        }

        private static List<CharacterState> ReadCharacterStates()
        {
            var result = new List<CharacterState>();
            List<CrewMember> crew = CrewManager.GetAllCrew();
            if (crew == null) return result;

            foreach (CrewMember member in crew)
            {
                if (member == null) continue;
                result.Add(new CharacterState
                {
                    npcId = member.id,
                    name = member.name,
                    fatigue = member.fatigue,
                    loyalty = member.loyalty,
                    favorability = 50, // 初始 50，由 N5 好感度系统更新
                    emotionalState = GetEmotionalState(member),
                    unlockedMemories = Array.Empty<string>(),
                    recentDialogue = new List<string>(),
                    fatigueTriggered = member.fatigue > FatigueTrigger
                });
            }
            return result;
        }

        private static string GetEmotionalState(CrewMember member)
        {
            if (member.fatigue > FatigueTrigger) return "疲惫";
            if (member.loyalty < LoyaltyTrigger) return "不满";
            return "正常";
        }

        /// <summary>区域解锁差分（GAP1）：当前已解锁 - 上次已解锁 = 刚解锁城市（首调用作基线）。</summary>
        private static string[] GetJustUnlockedCityIds(string[] current)
        {
            if (lastUnlockedCityIds == null)
            {
                lastUnlockedCityIds = current;
                return Array.Empty<string>();
            }

            var justUnlocked = new List<string>();
            foreach (string cityId in current)
            {
                bool alreadySeen = false;
                foreach (string prev in lastUnlockedCityIds)
                {
                    if (prev == cityId) { alreadySeen = true; break; }
                }
                if (!alreadySeen)
                {
                    justUnlocked.Add(cityId);
                }
            }

            lastUnlockedCityIds = current;
            return justUnlocked.ToArray();
        }

        private static ThresholdEvent NewThreshold(string name, float value, TrendDirection direction, string severity)
        {
            return new ThresholdEvent
            {
                trendName = name,
                thresholdValue = value,
                direction = direction,
                severity = severity
            };
        }
    }
}