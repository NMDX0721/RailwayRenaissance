# 铁路复兴：沙能冲击 — AI 项目总览

2026-10-21用户反馈修正：驾驶室两侧登车梯补连接底架的固定板、托臂与斜撑，踏步增加角托/折边/防滑筋；钢轮分区调整氧化层、踏面和加工纹理，修正暗色贴图叠乘。静态设计与力学校核边界见[转向架细化](NF5K_BOGIE_REFINEMENT.md)。

2026-10-21车底精修：两组转向架补轴箱导向、弹簧承座、弧形闸瓦与制动连接，分离轮对和电机外壳层级，同步既有夜间、白天和模拟审阅场景。仍为静态可视化候选，见[转向架细化](NF5K_BOGIE_REFINEMENT.md)。

2026-10-21补充：模拟审阅场景采用就近动作提示与可自定义主交互键，首次引导选择E/鼠标或其他键，右上角可重新设置；分步启动提示按实际设备状态推进。硬核实体操作与休闲小黄鼠协助的设计边界见[场景交互与驾驶教学](../../参考资料/系统/场景交互与驾驶教学设计.md)，小黄鼠执行与完整教学仍待实现。

2026-10-21驾驶交互首轮：独立CabSimulationReview场景已接牵引手轮、自动/独立制动和换向手柄的鼠标点击步进，原钥匙/启动链保留。档位动画读取控制状态；轨道行驶、完整操作可达性、驾驶记录/回顾/生涯仍待实现。见[驾驶台交互](NF5K_DRIVING_INTERACTION.md)。

NF-5K客舱已完成首轮独立座椅、行李架、内窗框/回口、暖风罩与门区内装，替换原座椅方块；仍缺乘客/行李交互、完整标识及疏散认证。布局与候选尺寸见[客舱内装说明](NF5K_PASSENGER_INTERIOR.md)。

NF-5K模型已从客室首段扩展为约18.15m完整头车候选，补后端贯通口与前后2组双轴动力转向架、樱花/旗帜/车号几何涂装。尺寸、轴式与驱动设备均为可视化设计推定，未接轮轨动力学；见[NF-5K基线](NF5K_HEADCAR_LAYOUT.md)最新条目。旧“首段灰模”描述仅记录历史阶段。

2026-10-20用户确认：NF-5K是驾驶与客运空间合造头车，非纯独立机车头。驾驶室后隔门通往客室，动力设备另行隔离；双端驾驶室按整列布局，不直接复刻DF4机车内双司机室。驾驶室及客室首段灰模见 [结构基线](NF5K_HEADCAR_LAYOUT.md)，其他旧“机车头/机械间”默认不覆盖这次确认。

驾驶范围确认：完整驾驶室、机车后端、轨道与窗外场景分阶段制作，见 [实施路线](LOCOMOTIVE_SIMULATION_ROADMAP.md)。本轮主台安装启动组件，接Input System鼠标射线动作链，其他控制件和高保真动力/制动/线路系统仍待实现。

最新：主驾驶台补入机油压力表，读取现有OilPressureMpa，主体前裙与可导出PBR细节同步细化；完整润滑/热管理和新增按钮交互仍未实现。见[油压表任务](dispatch/orders/CODEX-CAB-OIL-PRESSURE-20261020.md)。

驾驶台主体第二版：按用户反馈撤销办公桌式双柜，改成连续折板操纵台下壳、膝部退让凹腔和倾斜前裙；台面削角内收，仪表板补侧封与顶折边，速度表向中部收。沿用MainConsoleStudy独立场景与现有信号绑定，详见主体任务最新说明。

最新驾驶台进度：已完成主驾驶台主体第一版及现有组件试装，`MainConsoleStudy.prefab` / `CabMainConsoleReview.unity` 独立装配场景通过共用CabEngineState接线验证。控制件鼠标交互、速度表动态、油温/均衡风缸后端和完整人因验证仍待完成。见[主体任务](dispatch/orders/CODEX-CAB-MAIN-CONSOLE-20261020.md)。

2026-10-20补充：双针风压表Unity预制体及三路状态绑定已接入，均衡风缸独立仿真仍缺失；柴油机转速表已有窄长表首版，0–1500 r/min为设计候选。现有EngineRpm范围与同系列资料不一致，待独立校准，详见[组件计划](DF4_CAB_COMPONENT_PLAN.md)。

2026-10-20驾驶台单体进展：速度表及大小制动阀已有Unity预制体；窄长电流表新增`CabLinearCurrentGauge`及原生FBX/URP导入，单体验收场景绑定现有牵引电流。主3D操纵台尚未整组装配，动力学仍是简化实现。组件资产/原车考证边界/下一批仪表及信号缺口见[东风4驾驶台组件计划](DF4_CAB_COMPONENT_PLAN.md)。

> 给 AI 代理的快速上手文档。读完本文你应能理解：这个项目是什么、怎么跑、核心系统如何协作、哪里可以改。

> **核对日期：2026-10-18**。当前 `Assets/Scripts/` 为 99 个 C# 文件、约 46,895 行，包含未提交的驾驶台工作。以下「已实现」指代码与调用链存在，不代表本轮通过 Unity 运行验收。风险证据见 [本轮审查](dispatch/orders/CODEX-AUDIT-20261018.md)。

---

## 一、项目身份卡

| 字段 | 值 |
|------|-----|
| 游戏名 | 铁路复兴：沙能冲击 (Railway Renaissance: Sand Energy Impact) |
| 引擎 | Unity 6000.5.8f1（Unity 6.5） |
| UI 方案 | 混合：UGUI (Login/经营场景) + UI Toolkit (VN/标题界面) |
| 平台 | Windows + Android |
| 许可证 | MIT |
| 项目路径 | `D:\Unity Project\RailwayRenaissance` |
| 开发阶段 | 多个核心系统已接线，正处于整合阶段；开局种子接线、存档完整性和结算收尾一致性仍需修复与验收 |

---

## 二、一篇话读懂世界观

**2076 年**。沙能（沙子转化为能源）让飞行器取代了铁路——全球客运量蒸发 78%，铁路网基本停运。垄断企业 **USET** 统治天空，朝鲜是世界唯一超级大国。

但沙能飞行器有六个致命弱点：拉不动重货、闯不过风暴、飞不远长途、夜间效率低、离不开沙子补给、受制于空管。

**你**（林彪悍，金日成综合大学研究生）继承爷爷在雾峰村的废弃线路，去证明铁轨依然有存在的理由。

---

## 三、四大核心系统（设计概念）

> 四个系统均已有代码。经济核、创世核、人事与模板叙事已相互接线；完整生成式叙事、完整存档和部分剧情解锁条件仍未完成。

### 3.1 沙本位经济核（Sand Standard Core）
- **现状**：✅ 每日结算主链已实现（`GameData.cs`；本轮 1,757 行），仍有收尾一致性风险
- **核心逻辑**：`GameData.AdvanceDay()` 每日结算
  - 客流 → 收入 → 扣除燃料/维护/工资 → 净收入
  - 信任 + 车况 + 竞争影响 → 更新状态
  - 见 `参考资料/设定/沙本位经济核.md` 与 `docs/for-ai/DAILY_SETTLEMENT_FORMULAS.md`
- **公式来源**：`参考资料/系统/跨系统联动公式.md`；公式表第一、二节保留历史基线，当前核对见其第七节
- **代码中已实现**：客流/客货运收入/成本/补贴、订单结算、人事日更、月报、政治与老化演化、监管程序、区域检查及模板叙事
- **仍有缺口**：`sandPriceMultiplier` 未见消费端；`EventManager` 未消费老化事故倍率；天气机制未见经济结算消费端；日快照与后置效果不完整一致

### 3.2 千里马创世核（Chollima Genesis Core）
- **现状**：⚠️ 生成算法与注入 API 已实现，正常新游戏入口尚有接线缺口（`Assets/Scripts/WorldGen/`）
- **设计**：`参考资料/引擎/核心玩法循环.md` §2
- **核心思想**：开局种子决定三样不可见结构——城市依赖图、资源分布逻辑、政治倾向
- **目标**：两局游戏体验不同，因为世界本身就是不同的世界
- **当前路径风险**：`NewGameSetupUI.OnConfirm` 改 `seedCode`，`GameData.InitializeSubsystems` 却按 `seedId` 加载资源；`ApplyVNExitData` 没有调用 `ApplySeedInitialValues`。不能据生成 API 存在就宣称正常新档已满足全部种子初值规则。
- **初值铁律（2026-10-17 用户拍板）**：**每个存档的全部趋势初值必须由创世核按种子生成，严禁固定数值**——`WorldGenerator` 对称抖动 `Jitter(base, ±0.05, r)`（信任0.62 / 财政0.30 / 沙能基线 / 政治0.20 / 老化0.30，同种子确定性可复现）。代码中的 0.20、0.30、Trust=62 等常量**仅作无种子与老档缺字段的兼容兜底**，不得出现在正常开局路径。

### 3.3 岁月叙事引擎（Suiyue Narrative Engine）
- **现状**：⚠️ 模板阶段已实现（`Assets/Scripts/Narrative/`），已挂接 `AdvanceDay`；生成式 LLM 阶段未核实实现
- **设计**：`参考资料/引擎/岁月叙事引擎.md`
- **核心思想**：AI 分析世界状态生成事件，而非从模板随机挑选
- **当前管线**：`WorldAnalyzer` → `StoryEvaluator` → `TemplateFiller` → 效果应用 → 简报/公告分发；VN 与临时订单输出仍有 TODO，车况/客流/疲劳叙事效果尚未全部落地
- **三阶段路线**：模板阶段（已有代码）→ 本地 LLM 服务 → 嵌入式推理；后两者属于后续设计
- **岁月**：2053 年制造、沉睡 23 年的强 AI 原型，搭载于沙子飞猪号，是游戏中的 AI 角色

### 3.4 先民人事系统（Seonmin Personnel System）
- **现状**：✅ 已深度重构实现（`CrewManager.cs` ~1740 行，先民人事系统 v3.0）
- **已实现**：五大系统子技能树（车机工电辆，0-100分制）、8 级真实职级体系（0-7级）、FluctuationEngine 波动成长曲线、师傅带徒（MentorDictionary）、工资谈判、NPC 记忆
- **设计哲学**：疲劳与忠诚不在 UI 显示具体数值，而是通过行为、状态与对话自然流露

---

## 四、五条趋势线

| 趋势线 | 代码映射 | 状态 | 阈值行为 |
|--------|----------|------|----------|
| **信任** (Trust) | `GameData.Trust` (0-100) | ✅ 已实现，多策略影响 | <40 预警，<30 持续30天→代际断层 |
| **财政** (Money) | `GameData.Money` | ✅ 已实现，收支结算 | 资金截断至非负；归零后触发多日破产程序，不能按旧立即注资描述判断 |
| **沙能渗透** (Sand Penetration) | `SandRivalManager.cityPenetration` (0-1) | ✅ 已实现，城市级 | >0.40 预警，>0.55→关键线路被控制 |
| **政治压力** (Political Pressure) | `PoliticalPressure` + `PoliticalTier` | ✅ 连续演化与监管程序已实现 | 罚则读离散档位，经多日程序迁移，不能由连续量直接切档 |
| **设施老化** (Infrastructure Decay) | `InfrastructureDecay` + 三分项 + 缓存评级 | ✅ 磨耗、维护及定期检定已实现 | 当前燃油倍率 `1+D×0.25`，客流吸引力 `1−D×0.18`；事故二次方放大尚未在 EventManager 中接入 |

> **外显红线（2026-10-17 用户拍板）**：**政治压力与线路老化禁止在任何 UI 显示数值/百分比**，只准定性呈现且**档名/反馈句严格用现实公文与工务用语**（监管五档：警示通报→约谈整改→挂牌督办→停产停业整顿；老化四档：状态合格→经常保养→临时补修→评定失格；见 `docs/for-ai/DAILY_SETTLEMENT_FORMULAS.md` §六）——延续 §3.4「疲劳与忠诚不显数值」的硬核哲学；**信任与财政（钱）可外显**，沙能保持现状。初值一律走 §3.2 创世核抖动，禁固定。

---

## 五、场景流程

```
Login.unity
  └─ 登录/注册 → 自动登录 → 跳转 TitleScreen
TitleScreen.unity
  ├─ 新游戏 → NewGameSetupUI（别名 + 难度选择）→ VN_Test
  ├─ 继续运营 → VN_AutoLoad=1 → VN_Test（加载最近存档）
  └─ 档案/设置/退出
VN_Test.unity
  ├─ 序章剧本链（prologue_01 ~ prologue_10, JSON 驱动）
  └─ 剧本结束 → TRANSITION_TO_GAMEPLAY 指令 → StationSlice_V1
StationSlice_V1.unity
  └─ 经营主场景（RailRevivalRuntimeBootstrap 自动构建 UI）
```

---

## 六、架构总览

```
┌─────────────────────────────────────────────────────────────────┐
│                       场景层 (Scene)                             │
│  Login.unity | TitleScreen.unity | VN_Test.unity | StationSlice │
└───────────────────────┬─────────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────────┐
│                     运行时 Bootstrapper                          │
│  RailRevivalRuntimeBootstrap (StationSlice_V1 加载时自动执行)     │
│  → 确保 Canvas/Camera/EventSystem → 构建 UI 布局 → 绑定管理器    │
└───────────────────────┬─────────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────────┐
│                   管理器层 (Managers)                             │
│  ┌─────────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐   │
│  │ UIManager   │ │AudioMgr │ │VNAudioMgr│ │TutorialMgr    │   │
│  └─────────────┘ └──────────┘ └──────────┘ └───────────────┘   │
│  ┌─────────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐   │
│  │OrderManager │ │CrewMgr   │ │EventMgr  │ │SandRivalMgr   │   │
│  └─────────────┘ └──────────┘ └──────────┘ └───────────────┘   │
└───────────────────────┬─────────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────────┐
│                   核心数据层 (Static Data)                        │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  GameData.cs (静态类) — 经济模拟引擎                      │   │
│  │  - 状态：Money/Trust/TrainCondition/ExpectedPassengers   │   │
│  │  - 策略：DispatchPlan/StaffAllocation/Maintenance/...   │   │
│  │  - 核心入口：AdvanceDay()                                │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │ GameConfig   │  │  VNExitData  │  │  GameDataSaveData    │  │
│  │ (PlayerPrefs)│  │  (PlayerPrefs)│  │  (PlayerPrefs存档)   │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────────┐
│                   视觉小说层 (VN Subsystem)                       │
│  VNManager (单例) → JSONParser → ScriptData → SceneData[]      │
│    ├─ BackgroundManager (背景切换：淡入/滑入)                    │
│    ├─ CharacterSpriteManager (左/中/右三槽位立绘)               │
│    ├─ DialogueBox (UI Toolkit 对话框 + 打字机效果 + 术语悬浮/下方卡片/65条专有词库) │
│    ├─ VNBacklog (有色半透明和纸透景回顾 + 铁道物理双态滚动条 + 动量平滑阻尼)    │
│    ├─ FullScreenNews (全屏滚动新闻)                              │
│    ├─ VNSaveSystem/VNSaveLoadUI (3槽位存档)                     │
│    └─ VNAudioManager (BGM淡入淡出 + SFX)                       │
└─────────────────────────────────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────────┐
│                   资源层 (Resources/)                             │
│  Scripts/ (11个序章剧本 JSON)   bg/ (背景图)                     │
│  characters/ (角色立绘)         bgm/ + sfx/ (音频)              │
│  events.json                    orders.json                      │
│  Fonts/zpix (像素字体)          UI/ (USS + UXML)                │
└─────────────────────────────────────────────────────────────────┘
```

---

## 七、关键数据流

### 7.1 游戏日循环

```
玩家调整策略（发车/人员/维护/服务/对外）
  → 点击 End Day (两次确认)
  → UIManager.AdvanceDayFlow()
  → GameData.AdvanceDay()
      1. 发车方案 + PoliticalTier → 趟数；停业档位旅客趟数为 0
      2. 单趟客流 × 老化吸引力 × 难度 → 容量约束
      3. 客运收入 + 货运收入 × cargoMultiplier
      4. OrderManager.DailySettle → 待奖励收入；订单倒计时只由此推进
      5. 燃料/工资/维护 + 按监管档位调整补贴
      6. CrewManager.AdvanceDay → 成长/疲劳等（工资已计算）
      7. 净收入 = round(日收入 × incomeMultiplier) + 订单 - round(三项成本 × costMultiplier) + 补贴
      8. 信任/车况策略效果 → 老化磨耗/维护 → 政治连续量演化
      9. 经济随机事件 → 沙能竞争（单次 DailyUpdate / CheckForAction）
      10. 主经营月账、赤字反馈、趋势快照、持久化、月报、日变化
      11. Day++ → 区域检查 → 教程 → RefreshBoards
      12. 监管程序 → 破产检查 → 模板叙事检查/分发
      13. 返回 DayResult，由调用方刷新 UI
  → 显示日结面板
```

### 7.2 VN 剧本执行流

```
VNManager.StartScript("prologue_01_news")
  → JSONParser.LoadScript() → ScriptData
  → 设置背景 + BGM
  → ShowCurrentDialogue()
    → 检查 condition（变量分支）
    → 应用 setValue
    → 按 t 类型分发：
      - "n"/"d" → DialogueBox.ShowDialogue() + 打字机效果
      - "c" → ShowOptions()（选项分支，跳转场景）
      - "scroll" → FullScreenNews.Show()
      - "special" → HandleSpecialCommand()
  → 点击/空格/回车 → NextDialogue()
  → 场景结束 → 自动加载 nextScript（序章链）
  → 全部结束 → EndScript() 或 TransitionToGameplay()
```

### 7.3 VN→经营过渡

```
VN 中执行 t:"special" + text:"TRANSITION_TO_GAMEPLAY"
  → VNManager.TransitionToGameplay()
  → 构建 VNExitData（员工数据 + 资金 + 标记）
  → 序列化到 PlayerPrefs
  → SceneManager.LoadSceneAsync("StationSlice_V1")
  → RailRevivalRuntimeBootstrap 自动执行
  → UIManager.Start() → 有 VNExitData 时 ApplyVNExitData；无数据或解析失败时 ResetState
```

---

## 八、剧本 JSON 格式

```
📁 Assets/Resources/Scripts/
├── prologue_01_news.json       → 序章1：新闻播报（沙能历史）
├── prologue_02_day0.json       → 序章2：Day 0 实验室
├── prologue_03_journey.json    → 序章3：旅途
├── prologue_04_arrival.json    → 序章4：抵达雾峰村
├── prologue_05_inspection.json → 序章5：检查线路
├── prologue_06_team.json       → 序章6：组建团队
├── prologue_07_first_repair.json → 序章7：首次维修
├── prologue_08_first_run.json  → 序章8：首班运行
├── prologue_09_funding.json    → 序章9：资金
├── prologue_10_transition.json → 序章10：过渡到经营
└── test.json

JSON 结构：
{
  "id": "prologue_01_news",
  "nextScript": "prologue_02_day0",     // 自动加载下一个剧本
  "scenes": [{
    "bg": "black",                       // 背景ID
    "bgm": "melancholy",                 // BGM ID
    "transition": "fade",                // 过渡效果
    "chars": [{"name":"laochen","pos":"center"}],  // 场景级默认立绘
    "e": "normal",                       // 场景级默认表情
    "d": [{
      "t": "n|d|c|scroll|special",      // 对话类型
      "s": "老陈",                       // 说话者
      "text": "台词内容",                // 文本
      "e": "smile",                      // 表情
      "condition": "var_name",           // 条件（!var_name=取反）
      "setValue": "var_name=true",       // 设置变量
      "opts": [{"text":"选项","next":5,"condition":"","setValue":""}],  // 选项
      "chars": [{"name":"...","pos":"..."}]  // 条目级立绘（覆盖场景级）
    }]
  }]
}
```

---

## 九、脚本概览（按功能分组）

| 组 | 文件 | 行数 | 职责 |
|----|------|------|------|
| **入口/登录** | `LoginManager.cs` | 2336 | 登录/注册/自动登录/UI 构建 |
| **入口/登录** | `AutoLoginUI.cs` | 245 | 自动登录界面 |
| **标题** | `TitleScreen.cs` | 546 | 标题界面（UI Toolkit） |
| **标题** | `NewGameSetupUI.cs` | 417 | 新游戏设置（别名+难度+自定义参数） |
| **标题** | `TitleArchiveUI.cs` | 2351 | 档案界面 |
| **VN 核心** | `VN/VNManager.cs` | 3179 | VN 单例，管理剧本执行 |
| **VN 核心** | `VN/VNData.cs` | 61 | 剧本数据结构 |
| **VN 核心** | `VN/JSONParser.cs` | 46 | JSON 加载 |
| **VN 核心** | `VN/VNExitData.cs` | 26 | VN→经营过渡数据 |
| **VN 对话** | `VN/DialogueBox.cs` | 836 | 对话框 + 术语悬浮下划线 + 下方卡片弹窗 + 角色头像框 + 排版防御 |
| **VN 对话** | `VN/TypewriterEffect.cs` | 137 | 打字机效果 |
| **VN 对话** | `VN/VNBacklog.cs` | 1958 | 对话回顾（有色半透明和纸沙质图层背景 + 铁道物理双态滚动条 + 滚轮惯性阻尼 + 暖木纹缎带排版） |
| **VN 视觉** | `VN/BackgroundManager.cs` | 295 | 背景切换（淡入/滑入） |
| **VN 视觉** | `VN/CharacterSpriteManager.cs` | 266 | 角色立绘（左/中/右） |
| **VN 视觉** | `VN/FullScreenNews.cs` | 122 | 全屏滚动新闻 |
| **VN 音频** | `VN/AudioManager.cs` | 237 | VN BGM + SFX |
| **VN 存档** | `VN/VNSaveSystem.cs` | 138 | 存档系统（3槽位） |
| **VN 存档** | `VN/VNSaveLoadUI.cs` | 556 | 存读档 UI |
| **经济核心** | `GameData.cs` | 1757 | 经济模拟引擎（静态类） |
| **经济核心** | `GameConfig.cs` | 94 | 配置持久化（PlayerPrefs） |
| **经营 UI** | `UIManager.cs` | 634 | 经营 UI 面板管理 |
| **经营 UI** | `ButtonController.cs` | 53 | 按钮回调 |
| **经营 UI** | `VisualAssetBinder.cs` | 119 | 视觉资源绑定 |
| **Bootstrap** | `RailRevivalRuntimeBootstrap.cs` | 567 | 运行时自动构建 UI |
| **员工** | `CrewManager.cs` | 1784 | 员工数据 + NPC 记忆 |
| **对手 AI** | `SandRivalManager.cs` | 182 | USET 沙能渗透 |
| **事件** | `EventManager.cs` | 145 | 随机事件（events.json） |
| **订单** | `OrderManager.cs` | 296 | 订单系统（orders.json） |
| **引导** | `TutorialManager.cs` | 145 | 新手引导（按天解锁） |
| **成就** | `AchievementManager.cs` | 183 | 成就系统 |
| **其他** | `TermHighlightSystem.cs` | 221 | 术语高亮匹配器（驱动 Terms.json 65条工业术语） |
| **其他** | `SceneSetup.cs` | 40 | 场景设置 |
| **其他** | `TrainPlaceholderLoader.cs` | 26 | 火车占位符 |
| **其他** | `CloudSeaTrainBackground.cs` | 65 | 云海背景 |
| **其他** | `AudioManager.cs` | 64 | 全局音效 |
| **编辑器** | `Editor/FixSpriteImports.cs` | - | 导入修复 |
| **编辑器** | `Editor/SpriteImportFixer.cs` | - | 导入修复 |
| **编辑器** | `Editor/VNFinalSetup.cs` | - | VN 最终设置 |
| **编辑器** | `Editor/TitleScreenSetup.cs` | - | 标题界面设置 |
| **编辑器** | `Editor/CloudSeaSetup.cs` | - | 云海场景设置 |
| **编辑器** | `Editor/RailRevivalMvpSetup.cs` | - | MVP 场景设置 |

---

## 十、关键约定

### 10.1 命名规范
- 角色立绘：`characters/{角色ID}/{表情ID}.png`
- 背景：`bg/{场景ID}.png`
- 音乐：`bgm/{音乐ID}.ogg`
- 音效：`sfx/{音效ID}.ogg`
- VN 剧本：`Scripts/prologue_XX_{名称}.json`

### 10.2 编码规范
- 4 空格缩进
- PascalCase 函数名，camelCase 变量名
- 中文 UI 优先
- 不直接编辑 `.unity` YAML 文件

### 10.3 存档机制
- VN 存档：`PlayerPrefs` key `VN_Save_{0-2}`，JSON 格式
- 经营存档：`PlayerPrefs` key `SaveSlot_{0-2}`，`GameDataSaveData` 格式
- 配置：`PlayerPrefs` key `RailGameConfig`，`GameConfig` 格式
- 过渡数据：`PlayerPrefs` key `VNExitData`，`VNExitData` 格式

### 10.4 难度系统
| 难度 | 代码名 | 初始资金 | 收入倍率 | 成本倍率 |
|------|--------|---------|---------|---------|
| 司炉 | easy | 50,000 | 1.3x | 0.8x |
| 副司机 | normal | 40,000 | 1.0x | 1.0x |
| 司机 | hard | 30,000 | 0.8x | 1.2x |
| 指导司机 | custom | 滑块 | 滑块 | 滑块 |

---

## 十一、设计文档索引

### 核心设计（参考资料/）
| 文档 | 内容 | 阅读优先级 |
|------|------|-----------|
| `游戏开发文档.md` | 项目主索引（本文档的上级） | ⭐⭐⭐ |
| `沙本位经济核.md` v4.2 | 基于真实数据的经济模型 + 数学底层 | ⭐⭐⭐ |
| `角色设定.md` | 角色档案 + 技能系统 | ⭐⭐ |
| `世界观扩展设定.md` | 企业史/沙本位/雾峰村/岁月悬案 | ⭐⭐ |
| `视觉小说系统设计.md` v2.1 | VN 技术方案 + AI 对话 | ⭐⭐⭐ |
| `序章剧本_归乡.md` | 序章完整剧本 | ⭐⭐ |
| `故事线时间轴.md` | 世界历史时间线 | ⭐ |

### 设计文档（按 `参考资料/INDEX.md` 定位，旧 docs/compose 路径仅作历史线索）
| 文档 | 内容 | 阅读优先级 |
|------|------|-----------|
| `核心玩法循环.md` v2.0 | 三层时间结构 + 五条趋势线 | ⭐⭐⭐ |
| `跨系统联动公式.md` v1.0 | 8 个联动公式 | ⭐⭐⭐ |
| `科技树设计.md` v2.0 | 4 领域开放研发网 | ⭐⭐ |
| `AI系统实现计划.md` v2.0 | 三阶段 AI 策略 | ⭐⭐ |
| `铁龙竞争系统.md` | 沙能 AI 行为模型 | ⭐⭐ |
| `区域解锁与政治系统.md` | 6 区域 + 政治周期 | ⭐⭐ |
| `序章后续剧情设计.md` | prologue_04~10 剧本设计 | ⭐⭐ |
| `教程与新手引导设计.md` | 剧情驱动引导 | ⭐⭐ |
| `Mod系统设计.md` | 数据驱动框架 | ⭐ |

---

## 十二、设计-实现差距（待办清单）

### 优先整合问题（2026-10-18 静态审查）
- [ ] 正常开局把种子码、资源种子、趋势注入串为唯一入口，并核对 VN 过渡的初值规则
- [ ] 按槽位保存/恢复完整世界、监管、人事、订单与叙事状态，避免旧基础状态与全局新状态混装
- [ ] 统一结算后置效果、日变化、快照、公告和日结展示的时序
- [x] 政治压力、设施老化及趋势看板已有代码；保留政治/老化只准定性呈现的要求，运行验收另行完成

### P1 — 系统性重构
- [ ] `LoginManager.cs` 拆分（2308行 → 3-4文件）
- [ ] `GameData.cs` 静态类 → 非静态 Singleton/ScriptableObject
- [ ] 硬编码常量抽取到 JSON/ScriptableObject

### P2 — 玩法扩展
- [ ] 科技树系统（4 领域 28 节点）
- [ ] 疲劳/忠诚的行为反馈体验验收（人事日更已有实现）
- [ ] 月/年玩法扩展（30天月报、360天季节周期已有实现）
- [ ] 区域解锁补齐剧情/科技门槛（数值解锁与每日检查已有实现）
- [ ] 老化事故倍率接入与天气影响实现；核对沙价倍率消费端

### P3 — 高级系统
- [ ] VN AI 三模式对话（预设/自由/混合）
- [ ] 创世核正常入口与完整世界存档验收（生成算法已有）
- [ ] 岁月叙事生成式阶段、VN/订单输出和未落地效果（模板阶段已有）
- [ ] 手机 AI 助手（玩家 API Key 驱动）
- [ ] 更多剧本内容

---

*本文档由 AI 代理于 2026-08-14 阅读全项目后自动生成，供 AI 代理快速上手。*
2026-10-20 最新进展：柴油机转速表已完成原生 Unity 导入与 EngineRpm 绑定，预制体及独立验收场景可用；主驾驶台尚未整体装配。水温/机油温度表组首版已出，现有仿真只有水温，油温暂不接线。接线与验证见转速表任务，温度表的候选量程及证据边界见温度表任务。

# Daylight review environment (2026-10-21)

`Assets/Scenes/CabDaylightReview.unity` provides an independent NF5K daylight inspection environment with clear/overcast/low-sun presets and independent interior-lamp controls. Measured proportions and usage are documented in `NF5K_DAYLIGHT_REVIEW.md`; this is not a completed route or calibrated lighting simulation.
# NF5K backend prototype (2026-10-21)

An opt-in fixed-step backend now models delayed diesel start, finite-air automatic/independent brakes, power/adhesion-limited traction, grade, fuel, battery and thermal protection. `CabSimulationReview.unity` connects it to the existing cockpit state/gauges through `CabSimulationDriver`; other scenes retain legacy demonstration physics. This is a lumped prototype with candidate NF5K short-consist parameters, not calibrated original-vehicle dynamics. Full route movement, 3D handle interaction and global save-slot integration remain pending. Research/formulas/validation: `NF5K_SIMULATION_BACKEND.md`.
