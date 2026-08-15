using System.Collections.Generic;
using UnityEngine;

namespace Narrative
{
    /// <summary>
    /// 岁月角色系统（Layer 5）——岁月 AI：好感度 / 记忆解锁 / 日常对话 / 经济核预测建议（GAP10）/ 双 AI 线（GAP11）。
    /// GAP10 修复：经济核季节回归模型（360 天制，每月 30 天，与 GameData.GetSeasonModifier 同构）→ 岁月运营建议。
    /// GAP11 修复：岁月（情感叙事）vs 手机 AI（RDA，数据分析）双线分流，按运营决策日/剧情场景决定谁说话。
    /// </summary>
    public static class SuiyueCharacter
    {
        // ===== 好感度阈值（N5.1：0-30 冷淡 / 31-60 正常 / 61-80 友好 / 81-100 亲密） =====
        private const int MinFavorability = 0;
        private const int MaxFavorability = 100;
        private const int InitialFavorability = 50;
        public const int FrozenUpper = 30;   // 冷淡档上限
        public const int NormalUpper = 60;   // 正常档上限
        public const int FriendlyUpper = 80; // 友好档上限

        /// <summary>岁月好感度（0-100，初始 50）。</summary>
        public static int Favorability { get; private set; } = InitialFavorability;

        /// <summary>已解锁的记忆片段 ID（幂等：同一 ID 只解锁一次；描述文本经 GetMemoryText 获取）。</summary>
        public static List<string> UnlockedMemories { get; private set; } = new List<string>();

        /// <summary>岁月记忆名册（N5.2）：记忆 ID → 记忆描述文本。</summary>
        private static readonly Dictionary<string, string> MemoryDatabase = new Dictionary<string, string>
        {
            { "memory_first_meeting",
              "（初次相遇）大雾弥漫的月台上，一辆深蓝色的火车报出编号：0721。她说她在这里等这一趟车，等了自己沉睡的二十三年。" },
            { "memory_grandfather",
              "（爷爷的故事）爷爷是第一代线路工。他说铁路是把人们的日子缝在一起的针脚，而这列深蓝色的老家伙，是他见过最倔的一列。" },
            { "memory_sand_truth",
              "（沙能真相）沙能从来不是燃料，是许诺——被风扬走的许诺，最后都会变成沙子，落回每一个相信过它的人的肩膀上。" }
        };

        // ===== 5.1 好感度系统 =====

        /// <summary>
        /// 调整岁月好感度（clamp 0-100）。数值实际变化后自动触发记忆解锁检查（N5.2）。
        /// 提升/降低触发点（接线）：剧情对话选项、日常与岁月聊天、完成岁月任务；反向：忽视建议/冷漠选项/长期不互动。
        /// </summary>
        public static void AddFavorability(int delta)
        {
            if (delta == 0) return;
            int before = Favorability;
            Favorability = Mathf.Clamp(Favorability + delta, MinFavorability, MaxFavorability);
            if (Favorability != before)
            {
                CheckMemoryUnlock();
            }
        }

        // ===== 5.2 记忆解锁（好感度阈值触发，幂等） =====

        /// <summary>按当前好感度检查记忆解锁：31-60 初次相遇 / 61-80 爷爷的故事 / 81-100 沙能真相。</summary>
        public static void CheckMemoryUnlock()
        {
            UnlockMemoryIfReached("memory_first_meeting", FrozenUpper + 1, NormalUpper);
            UnlockMemoryIfReached("memory_grandfather", NormalUpper + 1, FriendlyUpper);
            UnlockMemoryIfReached("memory_sand_truth", FriendlyUpper + 1, MaxFavorability);
        }

        /// <summary>好感度落在 [min,max] 且未解锁时写入 UnlockedMemories（已解锁不重复解锁）。</summary>
        private static void UnlockMemoryIfReached(string memoryId, int min, int max)
        {
            if (Favorability < min || Favorability > max) return;
            foreach (string id in UnlockedMemories)
            {
                if (id == memoryId) return;
            }
            UnlockedMemories.Add(memoryId);
            Debug.Log($"[Suiyue] 记忆解锁 {memoryId}（好感度 {Favorability}）：{GetMemoryText(memoryId)}");
        }

        /// <summary>返回记忆片段的描述文本；未知 ID 时原样返回 ID。</summary>
        public static string GetMemoryText(string memoryId)
        {
            string text;
            if (memoryId == null) return null;
            return MemoryDatabase.TryGetValue(memoryId, out text) ? text : memoryId;
        }

        // ===== 5.3 岁月日常对话（按好感度四档语气） =====

        public static string GetDailyDialogue()
        {
            if (Favorability <= FrozenUpper) return "……（沉默）";
            if (Favorability <= NormalUpper) return "今天运营数据正常。";
            if (Favorability <= FriendlyUpper) return "你爷爷也曾经这样看着窗外。";
            return "0721 号……我其实记得很多东西。";
        }

        // ===== 5.4 经济核预测 → 岁月建议（GAP10 修复） =====

        /// <summary>
        /// 读取经济核季节回归模型（360 天制，每月 30 天，与 GameData.GetSeasonModifier 同构），结合
        /// GameData 实时信任 / 主线城市沙能渗透（有种子时叠加创世核 initialTrends 初始趋势作为参照）
        /// 给出运营建议——岁月以世界状态说话，而非空泛寒暄。
        /// </summary>
        public static string GetEconomicAdvice()
        {
            var advice = new List<string>();
            int month = ((GameData.Day - 1) % 360) / 30;

            if (month >= 2 && month <= 4)           // 春·采茶季
                advice.Add("采茶季客流高峰，建议加开一班。");
            else if (month >= 8 && month <= 10)     // 秋·货运旺季
                advice.Add("货运旺季，注意车况维护。");
            else if (month >= 11 || month <= 1)     // 冬·淡季
                advice.Add("淡季建议保守运营。");

            if (GameData.Trust < 40)                // 信任偏低（经济核信任系数雪崩区）
                advice.Add("信任偏低，建议提升服务质量。");

            float penetration = SandRivalManager.GetPenetration("wufeng"); // 主线城市实时渗透
            if (GameData.CurrentSeed != null && GameData.CurrentSeed.initialTrends != null)
            {
                // 有种子时以创世核初始渗透率为背景参照（GAP10：经济核数据驱动建议）
                penetration = Mathf.Max(penetration, GameData.CurrentSeed.initialTrends.sandPenetration);
            }
            if (penetration > 0.40f)
                advice.Add("沙能渗透率偏高，建议公关应对。");

            return advice.Count > 0 ? string.Join("", advice) : "本月运营节奏平稳，建议保持现速。";
        }

        // ===== 5.5 双 AI 线交互（GAP11 修复） =====

        /// <summary>剧情事件进行中标志（岁月主导；当前由剧情状态机/叙事接线置位，默认 false）。</summary>
        public static bool PlotEventActive { get; set; }

        /// <summary>
        /// 当前应激活的 AI 线：
        /// 剧情事件进行中 → "suiyue"（岁月主导）；
        /// 运营决策日（Day%5==0 操作日）→ "rdm"（手机 AI 提运营建议）；
        /// 日常 → "suiyue"（岁月日常对话）。
        /// </summary>
        public static string GetActiveAI()
        {
            if (PlotEventActive)
                return "suiyue";
            if (GameData.Day > 0 && GameData.Day % 5 == 0)
                return "rdm";
            return "suiyue";
        }

        /// <summary>手机 AI（RDA）运营建议文本（N5.6 远程接口预留前的占位实现）。</summary>
        public static string GetRdmDialogue()
        {
            return "根据数据，建议您关注下月秋季客流。";
        }
    }
}