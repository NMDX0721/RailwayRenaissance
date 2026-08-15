using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narrative
{
    /// <summary>
    /// 模板化叙事引擎主入口（N3.1）——每日管线：WorldAnalyzer.Analyze → StoryEvaluator.Evaluate
    /// → TemplateFiller.Fill → ApplyEffects（N3.5，GAP2 修复）。
    /// 模板来源（N3.4）：Resources/Narrative/narrative_templates.json；文件缺失时回退内置硬编码默认模板（供测试）。
    /// </summary>
    public static class NarrativeEngine
    {
        private const string TemplateResourcePath = "Narrative/narrative_templates";
        private const int MaxEventsPerDay = 3; // 节奏控制：单日最多落地前 N 个（N6 将按游戏阶段调节密度）

        private static List<NarrativeTemplate> loadedTemplates;
        private static readonly List<string> narrativeLog = new List<string>(); // 已触发事件ID（防重复 + 调试，N6.4）

        /// <summary>已触发事件ID日志（N6.4 叙事日志 / 调试用）。</summary>
        public static List<string> NarrativeLog => narrativeLog;

        /// <summary>是否已初始化（N4.8 挂载 AdvanceDay 时先调用初始化）。</summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>N3.4 初始化：从 Resources/Narrative/ 加载模板（缺失则保持空，运行时回退默认模板）。</summary>
        public static void Initialize()
        {
            narrativeLog.Clear();
            StoryEvaluator.ClearRecords();

            loadedTemplates = LoadTemplatesFromResources();
            IsInitialized = true;
            Debug.Log($"[NarrativeEngine] 初始化完成：Resources 模板 {loadedTemplates.Count} 个" +
                      (loadedTemplates.Count == 0 ? "（将使用内置默认模板）" : ""));
        }

        /// <summary>
        /// N3.1 主入口：Analyze → Evaluate → Fill → ApplyEffects。
        /// 返回单日本次实际触发的 NarrativeEvent（效果已落地、已记日志）。
        /// </summary>
        public static List<NarrativeEvent> CheckForEvent()
        {
            NarrativeContext context = WorldAnalyzer.Analyze();
            List<NarrativeTemplate> templates = (loadedTemplates != null && loadedTemplates.Count > 0)
                ? loadedTemplates
                : GetDefaultTemplates();

            List<NarrativeEvent> candidates = StoryEvaluator.Evaluate(context, templates);

            // 取前 N 个（节奏控制：避免单日消息轰炸，密度/权重调节在 N6）
            var triggered = new List<NarrativeEvent>();
            int limit = Math.Min(MaxEventsPerDay, candidates.Count);
            for (int i = 0; i < limit; i++)
            {
                NarrativeEvent evt = candidates[i];
                ApplyEffects(evt);                         // GAP2 修复：叙事事件效果落地
                narrativeLog.Add(evt.templateId);
                StoryEvaluator.RecordTriggered(evt.templateId, context.gameDay); // 冷却登记（templateId + 触发日）
                triggered.Add(evt);
            }

            return triggered;
        }

        /// <summary>N3.5 事件效果应用（GAP2 修复）：直接落地 GameData 已有公开方法。</summary>
        public static void ApplyEffects(NarrativeEvent evt)
        {
            if (evt == null || evt.effects == null) return;

            GameData.AddMoney(evt.effects.moneyDelta);
            GameData.AddTrust(evt.effects.trustDelta);

            // 沙能渗透：仅对目标城市落地（GAP6 联动，复用 SandRivalManager 公开接口）
            if (Mathf.Abs(evt.effects.sandPenetrationDelta) > 0.0001f && !string.IsNullOrEmpty(evt.targetCityId))
            {
                float current = SandRivalManager.GetPenetration(evt.targetCityId);
                SandRivalManager.SetPenetration(evt.targetCityId, current + evt.effects.sandPenetrationDelta);
            }

            // trainConditionDelta / passengerDelta：GameData 暂无公开增量接口（N4.3 简报挂载时一并接入）
            // fatigueDelta：CrewManager 暂无公开增量接口（N5 角色系统接入）
        }

        // ===== 内部：模板加载 / 默认模板 =====

        /// <summary>N3.4：从 Resources/Narrative/ 加载模板 JSON；文件不存在或解析失败时返回空列表。</summary>
        private static List<NarrativeTemplate> LoadTemplatesFromResources()
        {
            TextAsset asset = Resources.Load<TextAsset>(TemplateResourcePath);
            if (asset == null) return new List<NarrativeTemplate>();

            try
            {
                var wrapper = JsonUtility.FromJson<TemplateCollection>(asset.text);
                return wrapper != null && wrapper.templates != null
                    ? wrapper.templates
                    : new List<NarrativeTemplate>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NarrativeEngine] 模板 JSON 解析失败：{e.Message}" +
                                 "（GAP9：N3.4 落地时按 §3.1 结构做映射）");
                return new List<NarrativeTemplate>();
            }
        }

        /// <summary>内置硬编码默认模板（3-5 个，供 N3 测试；N3.4 JSON 落地后被 Resources 覆盖）。</summary>
        private static List<NarrativeTemplate> GetDefaultTemplates()
        {
            return new List<NarrativeTemplate>
            {
                new NarrativeTemplate
                {
                    id = "news_trust_crisis",
                    type = "news",
                    priority = 0.8f,
                    cooldownDays = 14,
                    conditions = new TemplateConditions { trustMax = 0.40f },
                    templateText = "{city}铁路信任告急：{value}乘客开始改乘牛车，站长办公室的电话响个不停。{player}，口碑的雪崩总是从一声抱怨开始。",
                    parameters = new[] { "半数", "三成", "越来越多" },
                    effects = new NarrativeEffects { trustDelta = -2 }
                },
                new NarrativeTemplate
                {
                    id = "news_penetration_warning",
                    type = "news",
                    priority = 0.7f,
                    cooldownDays = 10,
                    conditions = new TemplateConditions { penetrationMin = 0.40f },
                    templateText = "铁龙集团在{city}的渗透率已至{value}，当地报摊上贴着他们的招工告示。{player}，这些沙子做的许诺，会把人心连根带走。",
                    parameters = new[] { "危险高位", "历史最高", "警戒水平" },
                    effects = new NarrativeEffects { trustDelta = -1, sandPenetrationDelta = 0.01f }
                },
                new NarrativeTemplate
                {
                    id = "dialogue_npc_fatigue",
                    type = "dialogue",
                    priority = 0.6f,
                    cooldownDays = 7,
                    conditions = new TemplateConditions { fatigueMin = 60 },
                    templateText = "{npc}（抹了把汗）这一趟下来，骨头缝里全是煤灰……{player}，要是能歇半天，下个月我还能接着跑。",
                    effects = new NarrativeEffects { fatigueDelta = -10 }
                },
                new NarrativeTemplate
                {
                    id = "post_region_unlocked",
                    type = "news",
                    priority = 0.9f,
                    cooldownDays = 0,
                    conditions = new TemplateConditions(), // post_*：仅刚解锁新区时触发（StoryEvaluator 约定，GAP1）
                    templateText = "连接{city}的铁路正式通车！车站里{value}。沿线商会送来贺匾，落款处隐约闻得到铁龙的味道。",
                    parameters = new[] { "挤满了看新鲜的乡民", "鞭炮与汽笛声混作一团", "货栈一夜之间堆满了待运的货物" },
                    effects = new NarrativeEffects { moneyDelta = 500, trustDelta = 2 }
                },
                new NarrativeTemplate
                {
                    id = "sand_action_resistance",
                    type = "news",
                    priority = 0.65f,
                    cooldownDays = 10,
                    conditions = new TemplateConditions(), // sand_action_*：仅铁龙本日有行动时触发（StoryEvaluator 约定，GAP6）
                    templateText = "铁龙集团在{city}推出{value}，摆明了要撬{player}的墙角。",
                    parameters = new[] { "一元票价广告", "免费试乘的噱头", "收购沿线货栈的动作" },
                    effects = new NarrativeEffects { trustDelta = -1, moneyDelta = -200 }
                }
            };
        }

        /// <summary>模板 JSON 顶层包装（N3.4 落地时按 §3.1 的 narrative_templates/news/dialogue 结构做映射）。</summary>
        [Serializable]
        private class TemplateCollection
        {
            public List<NarrativeTemplate> templates;
        }
    }
}