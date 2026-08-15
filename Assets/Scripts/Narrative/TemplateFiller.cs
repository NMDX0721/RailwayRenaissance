using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narrative
{
    /// <summary>
    /// 模板填充器（N3.3）——替换占位符并构建完整 NarrativeEvent（N1.4）。
    /// 占位符：{city}/{cityName} → 已解锁城市随机名；{player} → GameConfig 玩家别名（回退"林彪悍"）；
    /// {value} → 从模板 parameters 随机选一个；{npc} → 疲劳触发 NPC 名（对话模板）。
    /// </summary>
    public static class TemplateFiller
    {
        /// <summary>替换占位符并构建 NarrativeEvent。</summary>
        public static NarrativeEvent Fill(NarrativeTemplate template, NarrativeContext context)
        {
            var evt = new NarrativeEvent
            {
                templateId = template.id,
                eventType = template.type,
                targetSystem = ResolveTargetSystem(template.type),
                content = FillText(template, context),
                effects = template.effects,
                targetCityId = PickTargetCityId(template, context),
                targetNpcId = PickTargetNpcId(context)
            };
            return evt;
        }

        /// <summary>占位符替换主流程（长占位符先替换，避免 {city} 截断 {cityName}）。</summary>
        private static string FillText(NarrativeTemplate template, NarrativeContext context)
        {
            string text = template.templateText ?? string.Empty;

            // {cityName}/{city} → 已解锁城市随机名（种子数据优先，缺失回退 ID / "雾峰村"）
            string cityName = PickCityName(context);
            text = text.Replace("{cityName}", cityName);
            text = text.Replace("{city}", cityName);

            // {player} → 玩家别名（设置过别名的用别名，否则回退"林彪悍"）
            text = text.Replace("{player}", GameConfig.Load().PlayerDisplayName);

            // {npc} → 疲劳/忠诚越阈 NPC 名（疲劳对话模板；无则替换为空）
            text = text.Replace("{npc}", PickNpcName(context));

            // {value} → 从 parameters 随机选一个
            if (template.parameters != null && template.parameters.Length > 0)
            {
                string value = template.parameters[UnityEngine.Random.Range(0, template.parameters.Length)];
                text = text.Replace("{value}", value);
            }

            return text;
        }

        /// <summary>目标系统映射：news/dialogue → VN；order → OrderManager；其余（event 等）→ GameData。</summary>
        private static string ResolveTargetSystem(string type)
        {
            if (type == "order") return "OrderManager";
            if (type == "news" || type == "dialogue") return "VN";
            return "GameData";
        }

        /// <summary>目标城市：post_* 取刚解锁城市，否则随机已解锁城市。</summary>
        private static string PickTargetCityId(NarrativeTemplate template, NarrativeContext context)
        {
            if (template != null && template.id != null && template.id.StartsWith("post_")
                && context.justUnlockedRegionCityIds != null && context.justUnlockedRegionCityIds.Length > 0)
            {
                return context.justUnlockedRegionCityIds[
                    UnityEngine.Random.Range(0, context.justUnlockedRegionCityIds.Length)];
            }
            if (context.unlockedCityIds != null && context.unlockedCityIds.Length > 0)
            {
                return context.unlockedCityIds[UnityEngine.Random.Range(0, context.unlockedCityIds.Length)];
            }
            return null;
        }

        /// <summary>目标 NPC：随机取一个疲劳/忠诚越阈的 NPC（对话模板用）。</summary>
        private static string PickTargetNpcId(NarrativeContext context)
        {
            if (context.fatiguedNpcIds != null && context.fatiguedNpcIds.Length > 0)
            {
                return context.fatiguedNpcIds[UnityEngine.Random.Range(0, context.fatiguedNpcIds.Length)];
            }
            return null;
        }

        private static string PickCityName(NarrativeContext context)
        {
            // 已解锁城市优先（种子 cities 字典的 key 即城市ID，值为显示名）
            if (context.unlockedCityIds != null && context.unlockedCityIds.Length > 0)
            {
                string id = context.unlockedCityIds[UnityEngine.Random.Range(0, context.unlockedCityIds.Length)];
                string name = LookupCityDisplayName(context, id);
                return !string.IsNullOrEmpty(name) ? name : id;
            }

            // 无已解锁 → 回退种子首个城市显示名
            if (context.seed != null && context.seed.cities != null && context.seed.cities.Count > 0)
            {
                foreach (var kvp in context.seed.cities)
                {
                    return kvp.Value != null ? kvp.Value.name : "雾峰村";
                }
            }
            return "雾峰村";
        }

        private static string LookupCityDisplayName(NarrativeContext context, string cityId)
        {
            if (context.seed == null || context.seed.cities == null) return null;
            if (context.seed.cities.TryGetValue(cityId, out var city))
            {
                return city != null ? city.name : null;
            }
            return null;
        }

        private static string PickNpcName(NarrativeContext context)
        {
            string npcId = PickTargetNpcId(context);
            if (npcId == null) return string.Empty;

            if (context.characterStates != null)
            {
                foreach (CharacterState cs in context.characterStates)
                {
                    if (cs != null && cs.npcId == npcId && !string.IsNullOrEmpty(cs.name)) return cs.name;
                }
            }
            return npcId;
        }
    }
}