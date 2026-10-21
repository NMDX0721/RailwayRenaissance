---
feature: vn-episode-pacing
status: in-progress
updated: 2026-09-24
branch: main
commits:
---

# 视觉小说话末演出与节奏连贯

## Report

**What was built**（进行中）— 话末改为 BA 结构两段式（To Be Continued → Next Episode 横条），配色用本作暖金/深棕；plot-outline 写入每话 Journey；prologue_01/04/06–10 已补话末留钩与承接；JSON 全通过（954 行）。

## [S1] Problem

1. 一话（prologue_01–10 各文件）过短，切换频繁，不像连续视觉小说。
2. 话与话之间缺少情绪承接与过渡节奏（research：pacing 须贴合叙事事件；转场用 fade 表时间、cut 表紧张）。
3. 话末「下一话」演出偏简陋，用户要求对齐蔚蓝档案章节完成演出感——**仅结构模仿，配色必须本作暖金/琥珀/深棕**。

## [S2] Design

### 2.1 话末演出（BA 结构 + 本作配色）

- 阶段1：近黑暖棕遮罩 + 右下 To Be Continued…（~1.8s）
- 阶段2：深棕横条 + Next Episode + 暖金下划线 + 第N话标题（~2.7s）→ 标题卡
- 点击任意跳过；去掉重奖励窗（已实现）
- **禁止** BA 冷蓝白主色（见 UI设计参考_蔚蓝档案.md 配色纪律）

### 2.2 节奏与连贯（内容策略，不改引擎分叉）

Research 要点落地：
- **每话 journey**：开场钩子 → 中段推进/情绪 → 话末留钩（下一话动机），禁止无过渡硬切。
- **Show don't tell**：用内心 `[独白]`、环境反应、BGM/背景微调代替纯旁白交代。
- **转场**：跨天/跨地 fade；同场景内用对话连续；避免无 transition 的硬跳。
- **扩写原则**：不堆字数，在关键 beat 间补：犹豫、感官细节、角色小动作旁白、1–2 句回呼前话。

### 2.3 扩写优先级（JSON 内容）

| 优先 | 话 | 状态 |
|------|----|------|
| P0 | prologue_01 | ✅ 电话→办公室承接 |
| P0 | prologue_04 | ✅ 抵达前呼吸/紧张 |
| P1 | prologue_06–10 | ✅ 话末留钩 |
| P1 | prologue_05 | 已有巡视+老人，可再加环境 |
| P2 | prologue_02–03 | 主体已长，衔接抽检 |

每话目标：关键场景间至少 1 段承接；总行数相对现状 +20%～40%，不写水词。

### 2.4 明确不做

- 不改轮播/选择机制；不新增互斥分支。
- 不重做全局 UI 皮肤。

## [S3] Out of Scope

- 立绘/背景生成；配音。

## Tasks

- [x] T1: 话末 BA 结构两段 + 本作配色 — acceptance: To Be Continued + Next Episode 暖金横条 (covers: S2.1)
- [x] T2: 转场/连贯检查清单写入 plot-outline — acceptance: 每话 journey 写入 §10.7 (covers: S2.2)
- [x] T3: 扩写 prologue_01–02 承接段 — acceptance: 01 已补；02 主体已长 (covers: S2.3)
- [x] T4: 扩写 prologue_03–04 — acceptance: 04 抵达前呼吸已补 (covers: S2.3)
- [x] T5: 扩写 prologue_05–10 话末留钩 — acceptance: 06–10 有留钩；05 已有巡视密度 (covers: S2.3)
