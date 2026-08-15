using System;
using System.Collections.Generic;

namespace Narrative
{
    /// <summary>
    /// 故事可能性评估器（N3.2）——遍历模板，按世界状态（GAP3：按世界状态，非纯时序）匹配 conditions，
    /// 计算加权匹配分数，过滤冷却期，按分数降序返回可触发的填充后事件。
    /// 条件维度：信任区间 / 渗透区间 / 疲劳触发 / 城市解锁 / 事件去重（excludedEventIds）。
    /// </summary>
    public static class StoryEvaluator
    {
        /// <summary>从世界状态评估哪些模板匹配，返回按匹配分数降序的填充后事件。</summary>
        public static List<NarrativeEvent> Evaluate(NarrativeContext context, List<NarrativeTemplate> templates)
        {
            var results = new List<NarrativeEvent>();
            if (templates == null) return results;

            var matched = new List<KeyValuePair<NarrativeTemplate, float>>();
            foreach (NarrativeTemplate template in templates)
            {
                if (template == null) continue;

                // N6.3：跳过冷却期内的模板（GAP4：冷却系统合并到 NarrativeRhythm）
                if (NarrativeRhythm.IsOnCooldown(template.id, template.cooldownDays, context.gameDay)) continue;

                // 检查 conditions 匹配（GAP3：按世界状态，非纯时序）
                float matchScore = CalculateMatchScore(template, context);
                if (matchScore > 0f)
                {
                    matched.Add(new KeyValuePair<NarrativeTemplate, float>(template, matchScore));
                }
            }

            // 按匹配分数（优先级 × 条件满足度）降序
            matched.Sort((a, b) => b.Value.CompareTo(a.Value));

            // 填充占位符生成最终事件（填充顺序即优先级顺序）
            foreach (var pair in matched)
            {
                results.Add(TemplateFiller.Fill(pair.Key, context));
            }
            return results;
        }

        /// <summary>记录模板触发日（N6.3 委托：NarrativeRhythm 管理冷却记录）。</summary>
        public static void RecordTriggered(string templateId, int gameDay)
        {
            NarrativeRhythm.RecordTrigger(templateId, gameDay);
        }

        /// <summary>清空冷却登记（N6.3 委托：新游戏/新会话时调用）。</summary>
        public static void ClearRecords()
        {
            NarrativeRhythm.ClearAllRecords();
        }

        /// <summary>冷却检查：距上次触发不足 cooldownDays 天则跳过（N6.3 委托）。</summary>
        private static bool IsOnCooldown(string templateId, int cooldownDays, int gameDay)
        {
            return NarrativeRhythm.IsOnCooldown(templateId, cooldownDays, gameDay);
        }

        /// <summary>
        /// 匹配分数计算（多条件加权）：硬性条件不满足返回 0，满足则
        /// 分数 = 优先级 + 情势加成（越阈/疲劳越严重、刚解锁新区，越偏向触发——世界状态越糟越要说话）。
        /// 注意：trust/penetration 区间与 NarrativeContext 同口径，为 0-1 归一化值，0 表示"未约束"。
        /// </summary>
        private static float CalculateMatchScore(NarrativeTemplate t, NarrativeContext ctx)
        {
            TemplateConditions c = t.conditions;
            if (c == null) return t.priority;

            // —— 数值区间条件：trust/penetration 0-1 归一化，0 表示未约束 ——
            if (c.trustMin > 0f && ctx.trustTrend < c.trustMin) return 0f;
            if (c.trustMax > 0f && ctx.trustTrend > c.trustMax) return 0f;
            if (c.penetrationMin > 0f && ctx.sandPenetration < c.penetrationMin) return 0f;
            if (c.penetrationMax > 0f && ctx.sandPenetration > c.penetrationMax) return 0f;

            // —— 疲劳条件（GAP7 联动）：需存在疲劳/忠诚越过阈值的 NPC ——
            if (c.fatigueMin > 0 && !HasAny(ctx.fatiguedNpcIds)) return 0f;

            // —— 城市解锁条件：所有 requiredCityIds 均须已解锁 ——
            if (c.requiredCityIds != null && c.requiredCityIds.Length > 0
                && !AllCitiesUnlocked(c.requiredCityIds, ctx.unlockedCityIds))
                return 0f;

            // —— 事件去重：excludedEventIds 中任一已触发过 → 不再触发 ——
            if (c.excludedEventIds != null)
            {
                foreach (string id in c.excludedEventIds)
                {
                    if (!string.IsNullOrEmpty(id) && NarrativeRhythm.HasBeenTriggered(id)) return 0f;
                }
            }

            // —— 语境约定（GAP1/GAP6 轻量挂钩，完整接线在 N4.5 区域解锁回调 / N4.6 铁龙行动联动）：——
            if (t.id != null)
            {
                // post_*：仅当本日刚解锁新区时触发
                if (t.id.StartsWith("post_") && !HasAny(ctx.justUnlockedRegionCityIds)) return 0f;
                // sand_action_*：仅当铁龙本日有行动时触发
                if (t.id.StartsWith("sand_action_") && string.IsNullOrEmpty(ctx.sandRivalActionType)) return 0f;
            }

            // 匹配分数 = 优先级 + 情势加成（GAP3）
            float score = t.priority;
            if (ctx.trustCrossedThreshold) score += 0.1f;
            if (ctx.fiscalCrossedThreshold) score += 0.1f;
            if (c.fatigueMin > 0 && ctx.fatiguedNpcIds != null) score += 0.05f * ctx.fatiguedNpcIds.Length;
            if (HasAny(ctx.justUnlockedRegionCityIds)) score += 0.2f;
            return score > 0f ? score : 0f;
        }

        private static bool AllCitiesUnlocked(string[] required, string[] unlocked)
        {
            if (unlocked == null) return false;
            foreach (string cityId in required)
            {
                bool found = false;
                foreach (string id in unlocked)
                {
                    if (id == cityId) { found = true; break; }
                }
                if (!found) return false;
            }
            return true;
        }

        private static bool HasAny(string[] arr)
        {
            return arr != null && arr.Length > 0;
        }
    }
}