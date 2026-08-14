# 铁路复兴：沙能冲击 — AI 项目总览

> 给 AI 代理的快速上手文档。读完本文你应能理解：这个项目是什么、怎么跑、核心系统如何协作、哪里可以改。

---

## 一、项目身份卡

| 字段 | 值 |
|------|-----|
| 游戏名 | 铁路复兴：沙能冲击 (Railway Renaissance: Sand Energy Impact) |
| 引擎 | Unity 6000.4.6f1 |
| UI 方案 | 混合：UGUI (Login/经营场景) + UI Toolkit (VN/标题界面) |
| 平台 | Windows + Android |
| 许可证 | MIT |
| 项目路径 | `D:\Unity Project\RailwayRenaissance` |
| 开发阶段 | Phase 2 核心代码完成 ✅，Phase 3 资产生成 ⏳，Phase 4 整合打磨 ⏳ |

---

## 二、一篇话读懂世界观

**2076 年**。沙能（沙子转化为能源）让飞行器取代了铁路——全球客运量蒸发 78%，铁路网基本停运。垄断企业 **USET** 统治天空，朝鲜是世界唯一超级大国。

但沙能飞行器有六个致命弱点：拉不动重货、闯不过风暴、飞不远长途、夜间效率低、离不开沙子补给、受制于空管。

**你**（林彪悍，金日成综合大学研究生）继承爷爷在雾峰村的废弃线路，去证明铁轨依然有存在的理由。

---

## 三、四大核心系统（设计概念）

> 以下四个系统在 README 和设计文档中有完整定义，但代码只实现了**沙本位经济核**的部分。其余三个处于设计阶段，尚未编码。

### 3.1 沙本位经济核（Sand Standard Core）
- **现状**：✅ 部分实现（`GameData.cs` 874 行）
- **核心逻辑**：`GameData.AdvanceDay()` 每日结算
  - 客流 → 收入 → 扣除燃料/维护/工资 → 净收入
  - 信任 + 车况 + 竞争影响 → 更新状态
  - 见 `参考资料/沙本位经济核.md` v4.0（基于真实数据推算）
- **公式来源**：`docs/compose/specs/跨系统联动公式.md`（8 个公式）
- **代码中已实现**：客流公式、收入公式、燃料费公式、维护费公式、信任变化基础、沙能渗透
- **代码中未实现**：事故概率公式、员工疲劳公式、货运收入、天气影响

### 3.2 千里马创世核（Chollima Genesis Core）
- **现状**：❌ 设计阶段，未编码
- **设计**：`docs/compose/specs/核心玩法循环.md` §2
- **核心思想**：开局种子决定三样不可见结构——城市依赖图、资源分布逻辑、政治倾向
- **目标**：两局游戏体验不同，因为世界本身就是不同的世界

### 3.3 岁月叙事引擎（Suiyue Narrative Engine）
- **现状**：❌ 设计阶段，未编码
- **设计**：`docs/compose/specs/岁月叙事引擎.md` + `参考资料/视觉小说系统设计.md`
- **核心思想**：AI 分析世界状态生成事件，而非从模板随机挑选
- **三阶段路线**：模板化 AI（当前设计）→ 本地 LLM 服务（FastAPI + llama.cpp）→ 嵌入式推理（Unity Sentis）
- **岁月**：2053 年制造、沉睡 23 年的强 AI 原型，搭载于沙子飞猪号，是游戏中的 AI 角色

### 3.4 先民人事系统（Seonmin Personnel System）
- **现状**：⚠️ 基础数据结构已实现（`CrewManager.cs` 269 行），但隐藏机制未实现
- **已实现**：5 名初始员工、4 技能轨道、疲劳/经验增长、NPC 记忆
- **未实现**：疲劳/忠诚度不在 UI 显示（通过行为流露）、技能成长动画、人员招聘/流失

---

## 四、五条趋势线

| 趋势线 | 代码映射 | 状态 | 阈值行为 |
|--------|----------|------|----------|
| **信任** (Trust) | `GameData.Trust` (0-100) | ✅ 已实现，多策略影响 | <40 预警，<30 持续30天→代际断层 |
| **财政** (Money) | `GameData.Money` | ✅ 已实现，收支结算 | <0 破产保护（TutorialManager） |
| **沙能渗透** (Sand Penetration) | `SandRivalManager.cityPenetration` (0-1) | ✅ 已实现，城市级 | >0.40 预警，>0.55→关键线路被控制 |
| **政治压力** (Political Pressure) | 无代码映射 | ❌ 未实现 | >0.50 预警，>0.70→政府强制干预 |
| **设施老化** (Infrastructure Decay) | 无代码映射 | ❌ 未实现 | >0.60 预警，>0.80→线路报废风险 |

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
│    ├─ DialogueBox (UI Toolkit 对话框 + 打字机效果)              │
│    ├─ VNBacklog (对话回顾，500条上限，支持跳转)                  │
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
      1. 发车方案 → 趟数
      2. 计算客流
      3. 收入 = 客流 × 票价 × 趟数
      4. 燃料费 = 油耗 × 里程 × 油价 × 车况系数
      5. 工资 = 月薪/30
      6. 维护费 = 策略决定 × 车况系数 × 技能系数
      7. 净收入 = 收入 - 燃料 - 维护 - 工资
      8. 信任变化 (基础+1 + 策略影响)
      9. 车况变化 (维护策略决定)
      10. 随机事件 (EventManager.TryTriggerEvent)
      11. 沙能竞争 (SandRivalManager.DailyUpdate + CheckForAction)
      12. 员工更新 (CrewManager.DailyUpdate)
      13. 订单更新 (OrderManager.DailyTick)
      14. 教程检查 (TutorialManager.CheckForTip)
      15. 刷新 UI
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
  → UIManager.Start() → GameData.ResetState()
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
| **入口/登录** | `LoginManager.cs` | 2327 | 登录/注册/自动登录/UI 构建 |
| **入口/登录** | `AutoLoginUI.cs` | 224 | 自动登录界面 |
| **标题** | `TitleScreen.cs` | 285 | 标题界面（UI Toolkit） |
| **标题** | `TitleScreenVideoBg.cs` | 66 | 视频背景 |
| **标题** | `NewGameSetupUI.cs` | 269 | 新游戏设置（别名+难度+自定义参数） |
| **标题** | `TitleArchiveUI.cs` | 703 | 档案界面 |
| **VN 核心** | `VN/VNManager.cs` | 1006 | VN 单例，管理剧本执行 |
| **VN 核心** | `VN/VNData.cs` | 57 | 剧本数据结构 |
| **VN 核心** | `VN/JSONParser.cs` | 46 | JSON 加载 |
| **VN 核心** | `VN/VNExitData.cs` | 26 | VN→经营过渡数据 |
| **VN 对话** | `VN/DialogueBox.cs` | 200 | 对话框（UI Toolkit） |
| **VN 对话** | `VN/TypewriterEffect.cs` | 104 | 打字机效果 |
| **VN 对话** | `VN/VNBacklog.cs` | 245 | 对话回顾 |
| **VN 视觉** | `VN/BackgroundManager.cs` | 176 | 背景切换（淡入/滑入） |
| **VN 视觉** | `VN/CharacterSpriteManager.cs` | 138 | 角色立绘（左/中/右） |
| **VN 视觉** | `VN/FullScreenNews.cs` | 121 | 全屏滚动新闻 |
| **VN 音频** | `VN/AudioManager.cs` | 141 | VN BGM + SFX |
| **VN 存档** | `VN/VNSaveSystem.cs` | 136 | 存档系统（3槽位） |
| **VN 存档** | `VN/VNSaveLoadUI.cs` | 431 | 存读档 UI |
| **经济核心** | `GameData.cs` | 874 | 经济模拟引擎（静态类） |
| **经济核心** | `GameConfig.cs` | 88 | 配置持久化（PlayerPrefs） |
| **经营 UI** | `UIManager.cs` | 612 | 经营 UI 面板管理 |
| **经营 UI** | `ButtonController.cs` | 51 | 按钮回调 |
| **经营 UI** | `VisualAssetBinder.cs` | 115 | 视觉资源绑定 |
| **Bootstrap** | `RailRevivalRuntimeBootstrap.cs` | 560 | 运行时自动构建 UI |
| **员工** | `CrewManager.cs` | 269 | 员工数据 + NPC 记忆 |
| **对手 AI** | `SandRivalManager.cs` | 112 | USET 沙能渗透 |
| **事件** | `EventManager.cs` | 93 | 随机事件（events.json） |
| **订单** | `OrderManager.cs` | 216 | 订单系统（orders.json） |
| **引导** | `TutorialManager.cs` | 145 | 新手引导（按天解锁） |
| **成就** | `AchievementManager.cs` | 169 | 成就系统 |
| **其他** | `TermHighlightSystem.cs` | 77 | 术语高亮 |
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
| `经济系统.md` v4.0 | 基于真实数据的经济模型 | ⭐⭐⭐ |
| `角色设定.md` | 角色档案 + 技能系统 | ⭐⭐ |
| `世界观扩展设定.md` | 企业史/沙本位/雾峰村/岁月悬案 | ⭐⭐ |
| `视觉小说系统设计.md` v2.1 | VN 技术方案 + AI 对话 | ⭐⭐⭐ |
| `序章剧本_归乡.md` | 序章完整剧本 | ⭐⭐ |
| `故事线时间轴.md` | 世界历史时间线 | ⭐ |

### 设计文档（docs/compose/specs/）
| 文档 | 内容 | 阅读优先级 |
|------|------|-----------|
| `核心玩法循环.md` v2.0 | 三层时间结构 + 五条趋势线 | ⭐⭐⭐ |
| `跨系统联动公式.md` v1.0 | 8 个联动公式 | ⭐⭐⭐ |
| `科技树设计.md` v2.0 | 4 领域开放研发网 | ⭐⭐ |
| `AI系统实现计划.md` v2.0 | 三阶段 AI 策略 | ⭐⭐ |
| `千里马对手AI.md` | 沙能 AI 行为模型 | ⭐⭐ |
| `区域解锁与政治系统.md` | 6 区域 + 政治周期 | ⭐⭐ |
| `序章后续剧情设计.md` | prologue_04~10 剧本设计 | ⭐⭐ |
| `教程与新手引导设计.md` | 剧情驱动引导 | ⭐⭐ |
| `Mod系统设计.md` | 数据驱动框架 | ⭐ |

---

## 十二、设计-实现差距（待办清单）

### P0 — 核心玩法缺失
- [ ] **政治压力趋势线**：代码无映射，作用在财政/事件上
- [ ] **设施老化趋势线**：代码无映射，线路级磨损
- [ ] **五条趋势线可视化**：UI 上显示趋势图

### P1 — 系统性重构
- [ ] `LoginManager.cs` 拆分（2308行 → 3-4文件）
- [ ] `GameData.cs` 静态类 → 非静态 Singleton/ScriptableObject
- [ ] 硬编码常量抽取到 JSON/ScriptableObject

### P2 — 玩法扩展
- [ ] 科技树系统（4 领域 28 节点）
- [ ] 员工疲劳/忠诚度隐藏 UI
- [ ] 月/年时间模型
- [ ] 区域解锁系统
- [ ] 事故概率公式实现

### P3 — 高级系统
- [ ] VN AI 三模式对话（预设/自由/混合）
- [ ] 千里马创世核（种子世界生成）
- [ ] 岁月叙事引擎（AI 事件生成）
- [ ] 手机 AI 助手（玩家 API Key 驱动）
- [ ] 更多剧本内容

---

*本文档由 AI 代理于 2026-08-14 阅读全项目后自动生成，供 AI 代理快速上手。*