# AGENTS.md — 铁路复兴：沙能冲击

> AI 代理快速上手文档。修改代码前先读 `docs/for-ai/PROJECT_OVERVIEW.md` 和 `docs/for-ai/CODE_MAP.md`。

---

## 项目身份

| 字段 | 值 |
|------|-----|
| 引擎 | Unity 6000.4.6f1 |
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
   `"D:\Unity Hub\Unity 6000.4.6f1\Editor\Unity.exe" -quit -batchmode -projectPath "D:\Unity Project\RailwayRenaissance" -logFile compile_check.log`

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

### 模型选择
| 任务类型 | 模型 | 参数 |
|---------|------|------|
| 编码/模型任务 | ds0718（`sensenova0718/deepseek-v4-flash`） | `model: "sensenova0718/deepseek-v4-flash"` |
| 识图/视觉分析 | `xiaomi/mimo-v2.5` | `model: "xiaomi/mimo-v2.5"` |
| ❌ 禁止使用 | 默认模型（ds=deepseek-v4-flash） | 与主代理同模型会触发并发上限 |

### 原因
主代理使用 ds（deepseek-v4-flash），子代理必须用不同的模型（ds0718），否则并发超限。

### 并发规则
- 编码子代理最多同时 1 个
- 视觉分析任务可同时 2 个

### 任务绑定
- spawn 时若任务已被 task 工具追踪，传递 `task_id` 参数

---

*Changes to this project: update `docs/for-ai/` docs when modifying code, and update AGENTS.md when adding new major systems.*