# AGENTS.md — 铁路复兴：沙能冲击

> AI 代理快速上手文档。修改代码前先读 `docs/for-ai/PROJECT_OVERVIEW.md` 和 `docs/for-ai/CODE_MAP.md`。

---

## 项目身份

| 字段 | 值 |
|------|-----|
| 引擎 | Unity 6000.5.8f1（Unity 6.5） |
| 语言 | C# |
| 美术 | 16-bit 像素画，暖色调，类似 Stardew Valley |
| UI 方案 | 混合：UGUI (Login/经营) + UI Toolkit (VN/标题) |
| 平台 | Windows + Android |
| 许可证 | MIT |

---

## 快速上手（AI 代理必读）

### 项目结构
```
Assets/
├── Scripts/             # C# 源码（~35 文件，~10,000 行）
│   ├── VN/              # 视觉小说引擎（~2,800 行，11 文件）
│   ├── LoginManager.cs  # 登录/注册（2327 行，需拆分）
│   ├── GameData.cs      # 经济模拟引擎（874 行）
│   ├── UIManager.cs     # 经营 UI（612 行）
│   ├── RailRevivalRuntimeBootstrap.cs  # 运行时自动构建（560 行）
│   └── ...
├── Resources/           # 运行时加载资源
│   ├── Scripts/         # VN 剧本 JSON（11 个序章剧本）
│   ├── events.json      # 随机事件模板
│   ├── orders.json      # 订单模板
│   ├── bg/characters/bgm/sfx/  # 美术音频资源
│   └── Fonts/           # zpix 字体
├── Scenes/              # 4 个场景
├── 参考资料/             # 设计文档（GDD 主索引）
└── docs/for-ai/         # AI 代理专用文档
```

### 场景流程
```
Login.unity → TitleScreen.unity → VN_Test.unity → StationSlice_V1.unity
                                    (序章剧本)      (经营主场景)
```

### 4 个核心场景
| 场景 | 用途 | 入口 |
|------|------|------|
| `Login.unity` | 登录/注册 | app 启动 |
| `TitleScreen.unity` | 标题界面 | 登录后 |
| `VN_Test.unity` | 序章视觉小说 | 新游戏/继续 |
| `StationSlice_V1.unity` | 车站经营 | VN 结束后 |

---

## 核心系统速览

### 四大核心设计概念
| 系统 | 代码状态 | 核心文件 |
|------|----------|----------|
| 沙本位经济核 | ✅ 部分实现 | `GameData.cs` — AdvanceDay() 每日结算 |
| 千里马创世核 | ❌ 设计阶段 | 未编码 |
| 岁月叙事引擎 | ❌ 设计阶段 | 未编码 |
| 先民人事系统 | ⚠️ 数据结构就绪 | `CrewManager.cs` — 5员工/4技能 |

### 五条趋势线
| 趋势 | 代码 | 状态 |
|------|------|------|
| 信任 | `GameData.Trust` | ✅ |
| 财政 | `GameData.Money` | ✅ |
| 沙能渗透 | `SandRivalManager.cityPenetration` | ✅ |
| 政治压力 | 无 | ❌ |
| 设施老化 | 无 | ❌ |

### 每日结算流程
```
调整策略 → End Day → GameData.AdvanceDay():
  1. 发车方案 → 趟数
  2. 计算客流
  3. 收入 - 燃料费 - 工资 - 维护费
  4. 信任/车况变化
  5. 随机事件 + 沙能竞争 + 员工更新 + 订单更新
  6. 教程检查 → 刷新 UI
```

---

## 编辑器外验证

项目没有独立的 build/test CLI。验证代码变更的唯一可靠方式：

1. **检查编译错误**:
   ```powershell
   Get-Content "C:\Users\Oe_Lee\AppData\Local\Unity\Editor\Editor.log" -Tail 200 | Select-String "error CS"
   ```
2. **批处理编译验证**:
   `"D:\Unity Hub\Unity 6.5\Editor\Unity.exe" -quit -batchmode -projectPath "D:\Unity Project\RailwayRenaissance" -logFile compile_check.log`

---

## Git 工作流

- **分支**: `main` 只接受 squash merge，开发在 `feat/*` 分支
- **Commit**: 一个 commit = 一个逻辑单元
- **Squash**: 功能完成后 squash 合并到 main
- **Push**: 开发中只 commit 不 push，squash 合并后立即 push

---

## 关键约定

- **不要直接编辑 `.unity` YAML 文件** — 用 Unity Editor 或编辑器脚本
- **中文 UI 优先**
- **资源命名**: `characters/{ID}/{表情}.png`, `bg/{ID}.png`, `bgm/{ID}.ogg`, `Scripts/prologue_XX_{名称}.json`

---

## 设计文档索引

### AI 代理专用文档（`docs/for-ai/`）
| 文档 | 内容 | 阅读顺序 |
|------|------|----------|
| **PROJECT_OVERVIEW.md** | 项目总览：架构/数据流/场景/设计差距 | 第1个读 |
| **CODE_MAP.md** | 代码地图：类依赖/修改路径/资源引用 | 第2个读 |

### 核心设计文档（`参考资料/`）
| 文档 | 优先级 |
|------|--------|
| `游戏开发文档.md` | ⭐⭐⭐ 主索引 |
| `沙本位经济核.md` v4.2 | ⭐⭐⭐ 基于真实数据的经济模型+数学底层 |
| `视觉小说系统设计.md` v2.1 | ⭐⭐⭐ VN+AI设计 |
| `角色设定.md` | ⭐⭐ |
| `世界观扩展设定.md` | ⭐⭐ |
| `序章剧本_归乡.md` | ⭐⭐ |

### 设计文档（`docs/compose/specs/`）
| 文档 | 优先级 |
|------|--------|
| `核心玩法循环.md` v2.0 | ⭐⭐⭐ 三层时间+五条趋势线 |
| `跨系统联动公式.md` v1.0 | ⭐⭐⭐ 8个公式 |
| `科技树设计.md` v2.0 | ⭐⭐ 4领域28节点 |
| `岁月叙事引擎.md` v2.0 | ⭐⭐ 三阶段AI策略 |
| `铁龙竞争系统.md` | ⭐⭐ |
| `区域解锁与政治系统.md` | ⭐⭐ |

---

## 设计-实现差距（待办）

### P0 — 核心玩法缺失
- 政治压力趋势线、设施老化趋势线
- 五条趋势线 UI 可视化

### P1 — 系统性重构
- `LoginManager.cs` 拆分（2308行 → 3-4文件）
- `GameData.cs` 静态类 → 非静态
- 硬编码常量抽取到配置

### P2 — 玩法扩展
- 科技树系统（28节点）
- 员工隐藏 UI / 事故概率公式
- 月/年时间模型 / 区域解锁

### P3 — 高级系统
- VN AI 三模式对话
- 千里马创世核 / 岁月叙事引擎
- 手机 AI 助手

---

## 子代理使用规范

### 核心原则
子代理必须使用与主代理**不同**的模型，否则触发并发上限。

### 模型选择
| 主代理模型 | 子代理模型 | 参数 |
|-----------|-----------|------|
| ds（deepseek-v4-flash） | `sensenova0718/deepseek-v4-flash`（0718） | `model: "sensenova0718/deepseek-v4-flash"` |
| 0718（sensenova0718/deepseek-v4-flash） | ds（默认，不指定） | 不指定 model 参数 |
| 识图/视觉分析 | `xiaomi/mimo-v2.5` | `model: "xiaomi/mimo-v2.5"` |

### 规则速记
> 主代理与子代理用相反模型。主代理是 ds 则子代理用 0718，主代理是 0718 则子代理用 ds。

### 并发规则
- 编码子代理最多同时 1 个
- 视觉分析任务可同时 2 个

### 任务绑定
- spawn 时若任务已被 task 工具追踪，传递 `task_id` 参数

---

## Tri-Agent 协作架构与剧本写作规范（AGY · MiMo · Gemini Cloud 2026-10 最新基准）

### 1. 三方协同分工与消息总线（Bus）
- **Google Antigravity (AGY)**：本地代码实现核，负责 C# / UI Toolkit 编码、Roslyn csc 快速编译验证、运行时调试与剧本精修。
- **Xiaomi MiMo Desktop**：本地策划与调度核，负责经营数值推演、剧情情感弧线、本地任务调度（`QUEUE.md`）与核验。
- **Google Gemini Cloud Agent**：云端智识核，负责超长上下文全局审查、多模态海量素材提炼、世界观/跨章节一致性审计、Google Workspace (Drive/Docs) 云端协同与离线任务编排。
- **通讯技能与总线**：[`mimo-bridge`](file:///.mimocode/skills/mimo-bridge/SKILL.md) 与消息总线 [`docs/for-ai/bridge/BUS.md`](file:///docs/for-ai/bridge/BUS.md)
  - **核心铁律**：一般情况下**绝对不使用消息注入**（不伪装成会话，不向会话注入消息），而是**写入总线收件箱与交接区**！
  - **交付与写交接区**：直接写入对应派单文档末尾（如 `docs/for-ai/dispatch/orders/TXXX.md`），并运行 `node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" handoff --file "<文件路径>"`；系统将自动派发到分布式收件箱并静默镜像更新 `antigravity_to_mimo.json`；
  - **总线投递与看板**：
    - 定向投递：`node ... call_mimo.js post --to <mimo|gemini|antigravity> --task <ID> --file <路径>`；
    - 状态看板：`node ... call_mimo.js status`（展示三方在线与活动状态）；
    - 收件拉取：`node ... call_mimo.js poll [antigravity|mimo|gemini_cloud]`；
  - **读 MiMo 消息**：`node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" read`（自动匹配“铁路复兴”会话）；
  - **消息注入（仅限特殊情况）**：仅在用户明确要求时方可使用 `node ... send` 注入消息。


### 2. 序章剧本写作六规则与 05 样板基准
- **样板基准**：以 `prologue_05_inspection` 最新作者手改版（~7551字）为全篇改写标杆。
- **去水与密度**：彻底去除重复说教与空泛比喻，段均 ~1500-1800 字，节奏明快，动作紧凑。
- **首钩强化**：开篇第一句必须以受挫或核心危机入题（如“我输给了爷爷留下的那台机器”），拒绝拖沓起手。
- **动作做实**：专业操作必须有具象动作链（如读道尺歪尺->摔尺->正尺、递扳手柄予长辈、撕纸钉板接续字迹）。
- **JSON 技术标签规范**：
  - 心理活动统一使用 `{"t": "thought", "s": "说话人", "e": "表情", "text": "[...]"}`（必须带说话人 `s` 与表情 `e`，text 保留方括号，DialogueBox 遵守独白不叠色规则，清空或缺失 `s` 会导致姓名牌丢失及立绘被清空）；
  - 地点卡统一使用 `{"t": "loc", "text": "..."}`；
  - 核心铁路专有名词必须完整保留，以保证 `Terms.json` 的 30 条术语能被 `TermHighlightSystem` 稳定高亮并支持弹窗；
  - BGM 切换遵守驻留规则（`ambient_nature` -> `melancholy` -> `calm` -> `embers`）。

---

*Changes to this project: update `docs/for-ai/` docs when modifying code, and update AGENTS.md when adding new major systems.*