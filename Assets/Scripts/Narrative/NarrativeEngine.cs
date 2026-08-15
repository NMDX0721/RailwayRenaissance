using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narrative
{
    /// <summary>
    /// 模板化叙事引擎主入口（N3.1）——每日管线：WorldAnalyzer.Analyze → StoryEvaluator.Evaluate
    /// → TemplateFiller.Fill → ApplyEffects（N3.5，GAP2 修复）。
    /// 模板来源（N3.4）：Resources/Narrative/narrative_templates.json；文件缺失时回退内置硬编码默认模板（供测试）。
    /// 输出集成（Layer 4）：CheckForEvent 产出 NarrativeEvent 后，由 DispatchAll/DispatchEvent 按
    /// targetSystem 分发到 GameData.Briefing/Notices（GAP8），并挂接区域解锁回调（GAP1）、
    /// 铁龙行动（GAP6）、疲劳触发（GAP7）。GAP5：EventManager 保留经济随机事件，本引擎只处理叙事事件，两者并行。
    /// </summary>
    public static class NarrativeEngine
    {
        private const string TemplateResourcePath = "Narrative/narrative_templates";
        private const int MaxEventsPerDay = 3; // 节奏控制：单日最多落地前 N 个（N6 将按游戏阶段调节密度）

        private static List<NarrativeTemplate> loadedTemplates;
        private static readonly List<string> narrativeLog = new List<string>(); // 已触发事件ID（防重复 + 调试，N6.4）

        /// <summary>
        /// 待落地事件缓冲（N4.5/4.6/4.7）：区域解锁回调 / 铁龙行动 / 疲劳触发在本日板面
        /// （RefreshBoards）重建前生成事件，先缓存在此，由下一次 DispatchAll 统一写入
        /// Briefing/Notices —— 保证叙事文本不被板面重建覆盖（GAP8）。
        /// </summary>
        private static readonly List<NarrativeEvent> pendingDispatch = new List<NarrativeEvent>();

        private static bool regionUnlockCallbackRegistered; // GAP1：防重复注册区域解锁回调

        /// <summary>已触发事件ID日志（N6.4 叙事日志 / 调试用）。</summary>
        public static List<string> NarrativeLog => narrativeLog;

        /// <summary>是否已初始化（N4.8 挂载 AdvanceDay 时先调用初始化）。</summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>N3.4 初始化：从 Resources/Narrative/ 加载模板（缺失则保持空，运行时回退默认模板）。</summary>
        public static void Initialize()
        {
            narrativeLog.Clear();
            StoryEvaluator.ClearRecords();
            pendingDispatch.Clear();

            loadedTemplates = LoadTemplatesFromResources();
            // N4.5（GAP1 修复）：区域解锁回调 → 触发 post_* 叙事事件
            RegisterRegionUnlockCallback();
            IsInitialized = true;
            Debug.Log($"[NarrativeEngine] 初始化完成：Resources 模板 {loadedTemplates.Count} 个" +
                      (loadedTemplates.Count == 0 ? "（将使用内置默认模板）" : ""));
        }

        /// <summary>
        /// N3.1 主入口：Analyze → Evaluate → Fill → ApplyEffects。
        /// 返回单日本次实际触发的 NarrativeEvent（效果已落地、已记日志）。
        /// 首次调用时若未初始化则自动初始化（保证区域解锁回调注册，GAP1）。
        /// </summary>
        public static List<NarrativeEvent> CheckForEvent()
        {
            if (!IsInitialized) Initialize(); // 懒初始化：AdvanceDay 挂载点无需单独调 Initialize

            NarrativeContext context = WorldAnalyzer.Analyze();
            return RunEvaluation(context, null);
        }

        // ===== Layer 4：叙事输出集成 =====

        /// <summary>
        /// N4 输出分发：按 targetSystem 将单个叙事事件交给对应消费系统。
        /// 关键实现细节：FullScreenNews/DialogueBox 需要 UIDocument 且依赖 VN 场景，叙事引擎不
        /// 直接访问场景——统一把叙事文本写入 GameData.Briefing/Notices（N4.3 GAP8），由 UIManager 显示。
        /// </summary>
        public static void DispatchEvent(NarrativeEvent evt)
        {
            if (evt == null || string.IsNullOrEmpty(evt.content)) return;

            switch (evt.targetSystem)
            {
                case "VN":
                    // N4.1 新闻事件 → FullScreenNews / N4.2 对话事件 → DialogueBox
                    // 简化实现：写入 Notices（GAP8）。TODO(N4.2)：VN 场景在途时可调用
                    // FullScreenNews.Show(evt.content) 或 VNManager.StartScript(...)，VN 场景可能不在当前场景。
                    GameData.Notices.Add(GetTypePrefix(evt) + evt.content);
                    break;

                case "GameData":
                    // N4.3 运营事件 → Briefing/Notices（GAP8：UI 简报输出）
                    GameData.Notices.Add(GetTypePrefix(evt) + evt.content);
                    GameData.Briefing.Add(GetTypePrefix(evt) + evt.content);
                    break;

                case "OrderManager":
                    // N4.4 任务事件（预留）：OrderManager 暂无生成临时订单接口，先写入简报。
                    // TODO(N4.4)：扩展 OrderManager.AddOrder 并在这里生成 GameOrder。
                    GameData.Notices.Add(GetTypePrefix(evt) + evt.content);
                    break;

                default:
                    GameData.Notices.Add(GetTypePrefix(evt) + evt.content);
                    break;
            }

            Debug.Log($"[NarrativeEngine] 分发事件 {evt.templateId}（{evt.eventType} → {evt.targetSystem}）：{evt.content}");
        }

        /// <summary>N4 批量分发：先落板回调/钩子期间缓冲的事件（板面已重建），再分发本轮事件。</summary>
        public static void DispatchAll(List<NarrativeEvent> events)
        {
            // 缓冲事件先落地：区域解锁/铁龙行动/疲劳触发在本日 RefreshBoards 前生成，
            // 若立即写入会被 RefreshBoards 重建列表覆盖，故在板面重建后统一追加（GAP8）
            if (pendingDispatch.Count > 0)
            {
                foreach (NarrativeEvent pending in pendingDispatch)
                {
                    DispatchEvent(pending);
                }
                pendingDispatch.Clear();
            }

            if (events == null) return;
            foreach (NarrativeEvent evt in events)
            {
                DispatchEvent(evt);
            }
        }

        /// <summary>
        /// N4.5（GAP1 修复）：订阅 RegionUnlockManager 解锁回调，新区解锁时触发生成对应 post_* 叙事事件。
        /// 由 Initialize() 调用注册；防重复订阅。
        /// </summary>
        public static void RegisterRegionUnlockCallback()
        {
            if (regionUnlockCallbackRegistered) return;
            regionUnlockCallbackRegistered = true;
            WorldGen.RegionUnlockManager.RegisterUnlockCallback(OnRegionUnlocked);
            Debug.Log("[NarrativeEngine] 已注册区域解锁回调（GAP1：解锁 → post_* 叙事事件）");
        }

        /// <summary>区域解锁回调：生成叙事事件并缓存，待板面重建后统一分发（GAP1）。</summary>
        private static void OnRegionUnlocked(int region)
        {
            Debug.Log($"[NarrativeEngine] 区域 {region} 解锁，触发叙事检查");
            pendingDispatch.AddRange(RunEvaluation(WorldAnalyzer.Analyze(),
                t => t.id != null && t.id.StartsWith("post_")));
        }

        /// <summary>
        /// N4.6（GAP6 修复）：铁龙行动（广告/试乘/价格战/收购/接触）发生时由 GameData.AdvanceDay
        /// 沙能竞争块调用，注入本日行动类型并生成 sand_action_* 叙事事件。
        /// </summary>
        public static void OnSandRivalAction(string actionType)
        {
            if (string.IsNullOrEmpty(actionType)) return;

            NarrativeContext ctx = WorldAnalyzer.Analyze();
            // Analyze 内 CheckForAction 已在沙能竞争块消费过本日行动（daysSinceLastAction 已重置），
            // 行动类型由调用方显式注入，保证 sand_action_* 模板能匹配（GAP6）
            ctx.sandRivalActionType = actionType;
            pendingDispatch.AddRange(RunEvaluation(ctx, t => t.id != null && t.id.StartsWith("sand_action_")));
            Debug.Log($"[NarrativeEngine] 铁龙行动 {actionType} → 叙事事件（GAP6）");
        }

        /// <summary>
        /// N4.7（GAP7 修复）：员工疲劳/忠诚越过阈值时由 CrewManager 调用（N5 接线），
        /// 把该 NPC 注入疲劳列表并生成对话事件。
        /// </summary>
        public static void OnNpcFatigueTriggered(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return;

            NarrativeContext ctx = WorldAnalyzer.Analyze();
            bool hasNpc = false;
            string[] existing = ctx.fatiguedNpcIds ?? new string[0];
            foreach (string id in existing)
            {
                if (id == npcId) { hasNpc = true; break; }
            }
            if (!hasNpc)
            {
                string[] merged = new string[existing.Length + 1];
                System.Array.Copy(existing, merged, existing.Length);
                merged[existing.Length] = npcId;
                ctx.fatiguedNpcIds = merged;
            }
            pendingDispatch.AddRange(RunEvaluation(ctx, t => t.type == "dialogue"));
            Debug.Log($"[NarrativeEngine] 疲劳触发 {npcId} → NPC 对话事件（GAP7）");
        }

        /// <summary>
        /// 评估管线（N3.1 体）：给定上下文 → 模板过滤（可选）→ StoryEvaluator.Evaluate →
        /// 取前 N 个 → ApplyEffects → 记日志/冷却。返回实际触发的已填充事件。
        /// </summary>
        private static List<NarrativeEvent> RunEvaluation(NarrativeContext context, System.Predicate<NarrativeTemplate> templateFilter)
        {
            List<NarrativeTemplate> templates = (loadedTemplates != null && loadedTemplates.Count > 0)
                ? loadedTemplates
                : GetDefaultTemplates();

            if (templateFilter != null)
            {
                var filtered = new List<NarrativeTemplate>();
                foreach (NarrativeTemplate t in templates)
                {
                    if (t != null && templateFilter(t)) filtered.Add(t);
                }
                templates = filtered;
            }

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

        /// <summary>公告前缀（与 GameData.Notices 现有表情符号风格一致）。</summary>
        private static string GetTypePrefix(NarrativeEvent evt)
        {
            if (evt == null) return "";
            switch (evt.eventType)
            {
                case "news":     return "📰 ";
                case "dialogue": return "💬 ";
                case "order":    return "📋 ";
                default:         return "📌 ";
            }
        }

        /// <summary>
        /// N3.5 事件效果应用（GAP2 修复）：直接落地 GameData 已有公开方法。
        /// N4.9（GAP5 修复）：与 EventManager 并行分工——EventManager 保留经济随机事件
        /// （纯数值，在 GameData.AdvanceDay 原检查点触发，本方法不触碰、不改变）；
        /// 叙事引擎只处理叙事事件（有剧情）的效果。两类效果各自只落地自己的 delta，互不覆盖；
        /// 同一日可以"经济随机事件 + 叙事事件"同时生效（并行）。
        /// </summary>
        public static void ApplyEffects(NarrativeEvent evt)
        {
            if (evt == null || evt.effects == null) return;

            // 仅落地叙事事件的数值变化；经济随机事件由 EventManager 独立处理（GAP5，并行不冲突）
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