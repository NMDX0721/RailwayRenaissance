using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narrative
{
    /// <summary>
    /// 叙事节奏控制器（Layer 6）——事件密度 / 类型分布 / 冷却系统 / 叙事日志。
    /// GAP4 修复：冷却系统从 N3.5 合并到此处，NarrativeEngine / StoryEvaluator 委托调用。
    /// </summary>
    public static class NarrativeRhythm
    {
        // ======================================================================
        // 6.1 事件密度控制器
        // ======================================================================

        /// <summary>
        /// 按游戏天数返回事件触发概率修正系数 [0, 1]，分 4 个阶段。
        /// 序章 (&lt;5) 高密度 → 生存 (&lt;90) 中等 → 发展 (&lt;180) 低 → 扩张 (180+) 稀疏。
        /// </summary>
        public static float GetEventProbability(int gameDay)
        {
            if (gameDay < 5) return 1.0f;   // 序章：高密度叙事引导
            if (gameDay < 90) return 0.6f;  // 生存期：中等密度
            if (gameDay < 180) return 0.4f; // 发展期：低密度
            return 0.3f;                     // 扩张期：稀疏叙事
        }

        // ======================================================================
        // 6.2 类型分布控制器
        // ======================================================================

        // 类型权重基值：运营(news) 40% / 角色(dialogue) 25% / 竞争(order) 20% / 政治(event) 15%
        private static readonly Dictionary<string, float> baseTypeWeights = new Dictionary<string, float>
        {
            { "news",     0.40f },
            { "dialogue", 0.25f },
            { "order",    0.20f },
            { "event",    0.15f }
        };

        /// <summary>
        /// 按趋势线 / 种子分配类型权重，通过加权轮盘选择一种类型，返回该类型下所有模板的 ID。
        /// 类型权重随世界状态动态调整：信任低→运营更多，渗透高→竞争更多，
        /// 政治压力高→政治事件更多，疲劳NPC→角色对话更多。
        /// </summary>
        public static string[] GetFilteredTemplateIds(List<NarrativeTemplate> all, NarrativeContext ctx)
        {
            if (all == null || all.Count == 0) return new string[0];

            float wNews     = baseTypeWeights["news"];
            float wDialogue = baseTypeWeights["dialogue"];
            float wOrder    = baseTypeWeights["order"];
            float wEvent    = baseTypeWeights["event"];

            // 按趋势线动态调整权重
            if (ctx != null)
            {
                // 信任低（<0.4）→ 运营(news)权重上升：世界更倾向于通过新闻说话
                if (ctx.trustTrend < 0.4f) wNews += 0.10f;
                // 渗透高（>0.5）→ 竞争(order)权重上升
                if (ctx.sandPenetration > 0.5f) wOrder += 0.10f;
                // 政治压力高（>0.6）→ 政治(event)权重上升
                if (ctx.politicalPressure > 0.6f) wEvent += 0.10f;
                // 有疲劳NPC → 角色(dialogue)权重上升
                if (ctx.fatiguedNpcIds != null && ctx.fatiguedNpcIds.Length > 0) wDialogue += 0.10f;
            }

            // 按 type 分组
            var buckets = new Dictionary<string, List<NarrativeTemplate>>();
            foreach (NarrativeTemplate t in all)
            {
                if (t == null || string.IsNullOrEmpty(t.type)) continue;
                if (!buckets.ContainsKey(t.type)) buckets[t.type] = new List<NarrativeTemplate>();
                buckets[t.type].Add(t);
            }

            // 加权随机轮盘：从四种类型中选择一种
            float total = wNews + wDialogue + wOrder + wEvent;
            if (total <= 0f) return new string[0];

            float roll = UnityEngine.Random.Range(0f, total);
            string chosenType;
            if (roll < wNews)                          chosenType = "news";
            else if (roll < wNews + wDialogue)         chosenType = "dialogue";
            else if (roll < wNews + wDialogue + wOrder) chosenType = "order";
            else                                        chosenType = "event";

            if (!buckets.TryGetValue(chosenType, out var chosenList) || chosenList.Count == 0)
                return new string[0];

            var result = new string[chosenList.Count];
            for (int i = 0; i < chosenList.Count; i++)
                result[i] = chosenList[i].id;
            return result;
        }

        // ======================================================================
        // 6.3 冷却系统
        // ======================================================================

        /// <summary>模板ID → 最近触发日。</summary>
        private static readonly Dictionary<string, int> cooldownRecords = new Dictionary<string, int>();

        /// <summary>类型默认冷却天数（模板自行指定时使用模板的 cooldownDays，兜底按此表）。</summary>
        private static readonly Dictionary<string, int> typeCooldownDays = new Dictionary<string, int>
        {
            { "news",     7  },
            { "dialogue", 14 },
            { "order",    7  },
            { "event",    30 }
        };

        /// <summary>简化版冷却检查：仅判断模板ID是否有过触发记录。</summary>
        public static bool IsOnCooldown(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return false;
            return cooldownRecords.ContainsKey(templateId);
        }

        /// <summary>
        /// 完整冷却检查：给定模板ID、冷却天数、当前天数，判断是否仍在冷却期内。
        /// cooldownDays ≤ 0 表示无冷却期。
        /// </summary>
        public static bool IsOnCooldown(string templateId, int cooldownDays, int currentDay)
        {
            if (cooldownDays <= 0) return false;
            if (!cooldownRecords.TryGetValue(templateId, out int lastDay)) return false;
            return currentDay - lastDay < cooldownDays;
        }

        /// <summary>记录模板触发当天。</summary>
        public static void RecordTrigger(string templateId, int gameDay)
        {
            if (string.IsNullOrEmpty(templateId)) return;
            cooldownRecords[templateId] = gameDay;
        }

        /// <summary>清除超过最长冷却期（30天）的记录，防止字典无限膨胀。</summary>
        public static void ClearExpired(int currentDay)
        {
            var expired = new List<string>();
            foreach (var kvp in cooldownRecords)
            {
                if (currentDay - kvp.Value >= 30)
                    expired.Add(kvp.Key);
            }
            foreach (string id in expired)
                cooldownRecords.Remove(id);
        }

        /// <summary>清空所有冷却记录（新游戏 / 新会话时调用）。</summary>
        public static void ClearAllRecords()
        {
            cooldownRecords.Clear();
        }

        /// <summary>检查模板ID是否有过触发记录（用于 excludedEventIds 去重判定）。</summary>
        public static bool HasBeenTriggered(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return false;
            return cooldownRecords.ContainsKey(templateId);
        }

        // ======================================================================
        // 6.4 叙事日志
        // ======================================================================

        private static readonly List<string> eventLog = new List<string>();
        private const int MaxLogEntries = 1000;

        /// <summary>记录一条叙事事件到日志，附加模板ID、内容和效果摘要。</summary>
        public static void LogEvent(string templateId, string content, NarrativeEffects effects)
        {
            int day = 0;
            try { day = GameData.GetDay(); } catch { /* GameData 可能尚未初始化 */ }

            string entry = $"[Day {day}] {templateId}: {content}";
            if (effects != null)
            {
                entry += $" (money:{effects.moneyDelta} trust:{effects.trustDelta}";
                if (Mathf.Abs(effects.sandPenetrationDelta) > 0.0001f)
                    entry += $" pen:{effects.sandPenetrationDelta:F2}";
                entry += ")";
            }
            eventLog.Add(entry);

            // 保持日志上限，FIFO 淘汰
            while (eventLog.Count > MaxLogEntries)
                eventLog.RemoveAt(0);
        }

        /// <summary>返回完整日志摘要文本（供调试 / UI 显示）。</summary>
        public static string GetLogSummary()
        {
            if (eventLog.Count == 0) return "暂无叙事日志。";
            return string.Join("\n", eventLog.ToArray());
        }

        /// <summary>获取原始日志列表引用（供 NarrativeEngine.NarrativeLog 委托）。</summary>
        public static List<string> GetEventLog() => eventLog;
    }
}