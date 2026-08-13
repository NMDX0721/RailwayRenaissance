# 铁路复兴：沙能冲击 🚂

## Railway Renaissance: Sand Energy Impact

> A hardcore pixel-art railway management sim with Visual Novel storytelling — built with Unity 6.
> 一款硬核像素风铁路模拟经营 + 视觉小说游戏，使用 Unity 6 开发。

---

**English** | [中文](#chinese)

---

## English

### Overview

In a world where sand-powered flying vehicles (Sand-Tech) have rendered traditional railways obsolete, you inherit a abandoned railway line in the remote village of Wufeng. Your mission: restore the railway, win back the trust of the community, and fight against the encroaching Sand-Tech monopoly.

**Genre**: Simulation / Management / Visual Novel / Strategy  
**Platform**: Windows (primary), Android (planned)  
**Engine**: Unity 6000.4.6f1  
**Style**: 16-bit pixel art, warm palette  

### Key Features

- **Hardcore Economics**: Pure operations lose money in the early game. Story missions provide the subsidies that keep you alive. Every decision has weight.
- **Slow-Paced Gameplay**: The world runs continuously in the background. Players observe trends, make long-term strategic choices, and face irreversible consequences — not frantic click-fests.
- **5 Trend System**: Trust, Fiscal Strain, Sand Penetration, Political Pressure, Infrastructure Decay — five interconnected trends that shape your world.
- **Sand-Tech Rival**: An AI-driven competitor that actively campaigns, price-wars, and tries to acquire your lines.
- **Crew Management**: 5 unique characters with 4 skills each, fatigue system, training, and role assignments.
- **Tech Tree**: 3 mutually exclusive branches (Industrial / Ecological / Automation) — choose wisely.
- **Story-Driven**: Prologue acts as a Visual Novel, then seamlessly transitions into management gameplay. Story events continue to provide narrative depth throughout.
- **Random Events**: 10+ event templates covering economic shifts, weather, personnel, and Sand-Tech actions.
- **Political Cycles**: Local government shifts between Authoritarian, Market, and Welfare orientations, affecting your bottom line.
- **Term Glossary**: Yellow-highlighted terms in dialogue with popup explanations.

### Tech Stack

| Component | Technology |
|-----------|-----------|
| Engine | Unity 6000.4.6f1 (Unity 6) |
| UI | UI Toolkit (title/VN) + uGUI (login) |
| VN System | JSON-driven, data-driven dialogue |
| Save System | PlayerPrefs + JSON serialization |
| AI (future) | Template-based (phase 1), llama.cpp (phase 2) |

### Project Structure

```
Assets/
├── Scripts/           # C# source code
│   ├── VN/            # Visual Novel system
│   ├── GameData.cs    # Core economy simulation
│   ├── CrewManager.cs # Staff management
│   ├── EventManager.cs# Random events
│   ├── SandRivalManager.cs  # Sand-Tech AI
│   └── ...            # Other systems
├── Resources/
│   ├── Scripts/       # VN JSON scripts (prologue_01~10)
│   ├── events.json    # Event templates
│   └── characters/    # Character sprites
├── Scenes/            # Unity scenes
└── Documentation/     # Design docs
参考资料/                # Design documents (Chinese)
docs/compose/          # Specs, plans, reports
```

### Design Documents

Core design documents are in `参考资料/` (Chinese) and `docs/compose/specs/`:

| Document | Description |
|----------|-------------|
| `游戏开发文档.md` | Master GDD index |
| `经济系统.md` | Economy model v4.0 |
| `角色设定.md` | Character profiles |
| `世界观与车辆设定.md` | World lore & vehicles |
| `核心玩法循环.md` | Core gameplay loop design |
| `跨系统联动公式.md` | Cross-system formulas |
| `科技树设计.md` | Tech tree (3 branches) |
| `沙能竞争系统设计.md` | Sand-Tech rival system |

### How to Run

```bash
cd mimocode-desktop
npm start
```

Or open the project directly in Unity Editor:

1. Open Unity Hub
2. Add project from `D:\Unity Project\RailwayRenaissance`
3. Open with Unity 6000.4.6f1
4. Open `Scenes/Login.unity` or `Scenes/VN_Test.unity`

### Development Status

```
Phase 1: Design docs fix        ✅ Complete
Phase 2: Code implementation    ✅ Complete (11/11 tasks)
          - T1: Prologue JSON       ✅
          - T2: VN→Gameplay bridge  ✅
          - T3: Economy system      ✅
          - T4: Crew system         ✅
          - T5: Event system        ✅
          - T6: Sand rival system   ✅
          - T7: Tutorial system     ✅
          - T8: Save system         ✅
          - T9: Term glossary       ✅
Phase 3: Asset generation       ⏳ Pending
Phase 4: Integration & polish   ⏳ Pending
```

### License

Personal, non-commercial, open-source educational project. All trains, companies, and characters are fictional.

---

## <a name="chinese"></a>中文

### 概述

在一个沙能飞行器全面取代传统铁路的世界里，你继承了偏远村庄雾峰村的一条废弃铁路线。你的使命：修复铁路，赢回社区信任，对抗沙能科技公司的步步蚕食。

**类型**：模拟经营 / 视觉小说 / 策略  
**平台**：Windows（优先）、Android（计划）  
**引擎**：Unity 6000.4.6f1  
**风格**：16-bit 像素风，暖色调  

### 核心特色

- **硬核经济**：前期纯运营必然亏损，剧情补贴是生存关键。每个决策都有重量。
- **慢节奏玩法**：世界在后台持续运行，玩家观察趋势、做长期策略选择、面对不可逆后果——不是频繁点击。
- **五条趋势线**：信任度、财政压力、沙能渗透率、政治压力、设施老化——五条相互影响的趋势线塑造世界。
- **沙能竞争**：AI驱动的竞争对手，主动发起广告战、价格战、收购你的线路。
- **人员管理**：5名独特角色，每人4项技能，疲劳系统、培训、岗位分配。
- **科技树**：3条互斥分支（工业/生态/自动）——谨慎选择。
- **剧情驱动**：序章为视觉小说，平滑过渡到经营玩法。剧情事件贯穿始终。
- **随机事件**：10+事件模板，覆盖经济波动、天气、人事、沙能行动。
- **政治周期**：地方政府在威权/市场/福利三种倾向间周期性变化。
- **术语高亮**：对话中黄色高亮术语，点击弹出解释。

### 技术栈

| 组件 | 技术 |
|------|------|
| 引擎 | Unity 6000.4.6f1 (Unity 6) |
| UI | UI Toolkit（标题/VN）+ uGUI（登录） |
| VN系统 | JSON驱动，数据驱动对话 |
| 存档系统 | PlayerPrefs + JSON序列化 |
| AI（未来） | 模板化（阶段1），llama.cpp（阶段2） |

### 项目结构

```
Assets/
├── Scripts/           # C# 源代码
│   ├── VN/            # 视觉小说系统
│   ├── GameData.cs    # 核心经济模拟
│   ├── CrewManager.cs # 人员管理
│   ├── EventManager.cs# 随机事件
│   ├── SandRivalManager.cs  # 沙能竞争AI
│   └── ...            # 其他系统
├── Resources/
│   ├── Scripts/       # VN剧本JSON (prologue_01~10)
│   ├── events.json    # 事件模板
│   └── characters/    # 角色立绘
├── Scenes/            # Unity场景
└── Documentation/     # 设计文档
参考资料/                # 设计文档（中文）
docs/compose/          # 规格/计划/报告
```

### 设计文档

核心设计文档位于 `参考资料/` 和 `docs/compose/specs/`：

| 文档 | 说明 |
|------|------|
| 游戏开发文档.md | 主GDD索引 |
| 经济系统.md | 经济模型 v4.0 |
| 角色设定.md | 角色档案 |
| 世界观与车辆设定.md | 世界观与车辆 |
| 核心玩法循环.md | 核心玩法循环设计 |
| 跨系统联动公式.md | 跨系统联动公式 |
| 科技树设计.md | 科技树（3分支） |
| 沙能竞争系统设计.md | 沙能竞争系统 |

### 运行方式

```bash
cd mimocode-desktop
npm start
```

或直接在 Unity Editor 中打开：

1. 打开 Unity Hub
2. 添加项目 `D:\Unity Project\RailwayRenaissance`
3. 使用 Unity 6000.4.6f1 打开
4. 打开 `Scenes/Login.unity` 或 `Scenes/VN_Test.unity`

### 开发进度

```
Phase 1: 设计文档修复        ✅ 完成
Phase 2: 代码实现            ✅ 完成 (11/11 任务)
          - T1: 序章JSON         ✅
          - T2: VN→经营过渡      ✅
          - T3: 经济系统         ✅
          - T4: 人员系统         ✅
          - T5: 事件系统         ✅
          - T6: 沙能竞争系统     ✅
          - T7: 教程引导         ✅
          - T8: 统一存档         ✅
          - T9: 术语高亮         ✅
Phase 3: 资产生成            ⏳ 待开始
Phase 4: 整合打磨            ⏳ 待开始
```

### 许可

个人非营利开源教育项目。所有列车、公司和角色均为虚构。

---

*Made with ❤️ by a high school student developer. 一个高中生的独立游戏开发项目。*