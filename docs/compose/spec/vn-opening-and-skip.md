---
feature: vn-opening-and-skip
status: delivered
updated: 2026-09-25
branch: main
commits: de9ae4e..450c1e3
---

# 开场Logo / 关闭叉 / 跳过闸门

## Report

**What was built** — 序章开场：暖深棕分镜，有 Sprite logo 则只播 Logo→制作信息（无文字标题镜）；点击跳过；空格被 `openingLogoActive` 挡住。历史 ✕/面板点击 `StopImmediatePropagation` + `SuppressNextAdvance()`。跳过入口 `RequestSkip`（菜单双箭头）：`NeedsQuizForSkip` = 脚本名 `prologue_01*` 或 `VN_Got0721!=1` → 10 题 `VNPrologueQuiz`（≥7 → `prologue_02_day0`）；否则 **Summary 跳过确认**（总结框：第N话+台词预览）。**退出**（菜单门图标/ESC）独立弹窗：无总结，三键一行（取消/退出剧情/退出游戏），退出剧情先写自动书签再 `ReturnToTitle`。两弹窗及恢复/删档/恢复默认共用 `DialogStyle` 壳。新周目清 `VN_Got0721`。

**Verification** — 编辑器 `Logs/Editor.log` `error CS`=0 多次热重载 PASS。独立审查 T1–T4 曾全 PASS（早期实现）；后续跳过总结/退出三键/闸门改 `prologue_01` 为提交后迭代，待 Play 人工回归。

**Journey log**:
- 项目 Unity **6000.5.8f1**（`D:\Unity Hub\Unity 6.5`）；误用 6000.4.6 会包解析失败。
- 闸门不用 `XiaomiAccount`（会被兜底「旅人」）；初版曾用 `Username`，后改为 **序章01 必答题 + VN_Got0721**。
- 菜单：回顾/跳过(>>)/书签/退出；场景内快进改走 Shift+→（`SkipToNext` 已从菜单移除）。
- **总结框只属于跳过确认**；用户多次纠正：退出无总结、图片可能指 Menu 四键而非退出弹窗。
- Logo 必须 `Load<Sprite>`（textureType=8），`Load<Texture2D>` 会 null 走无图分支。
- `RequestSkip` ↔ `ShowConfirmDialog` 双向调用均受闸门与 quiz-open 守卫，无递归。
- 开场分镜需 `openingLogoActive` 挡键盘推进（审查发现的边角，已修）。

## [S1] Problem

1. 开场全屏 Logo 叠字、偏冷黑，不符合本作暖金/深棕设计（用户认可的参考：Logo 图 + 金标题 + 金线 + 制作信息，分镜或单屏干净展示）。
2. 历史对话面板点 **✕** 仍会推进下一句（点击冒泡到 root）。
3. 序章跳过未按设计分流：
   - **拿到0721 / 注册小米账号前** → 应进 **10题答题**（≥7/10 才可跳）；
   - **注册后标准跳过** → 确认弹窗，**结构仿用户参考图**（居中标题+金线+说明+双按钮），**配色用本作暖金/深棕**。

## [S2] Design

### 2.1 开场 Logo

- 全屏非 VN：暖深棕底 `~#0a0705`；**Logo 图 → 中文金标题+金线 → 制作信息** 分镜淡入淡出（或单屏堆叠但间距干净）。
- 无图 Logo 时仅文字；任意点击跳过；不出现「点击继续」以外的杂讯。
- 禁止冷蓝白 BA 主色（见 UI设计参考 配色纪律）。

### 2.2 历史 ✕

- `VNBacklog` 关闭按钮与面板：`PointerDown`/`ClickEvent` `StopImmediatePropagation`，并设与 VN 相同的 `clickedButtonThisFrame` 或确保 `vnBacklog.IsOpen` 在关闭后为 false 且同帧不触发推进。
- 关闭后同一次点击不得 `NextDialogue`。

### 2.3 跳过闸门

- `HasXiaomiAccount` / 是否已过 Day0（获得0721）判定：
  - **未注册/未拿0721**：Skip → **答题界面**（10题，答案见平板系统设计 §8；≥7 通过 → 跳到 prologue_02 或 Day0 起点；&lt;7 → 不可跳，提示重试/体验序章）。无附属奖励。
  - **已注册**：Skip → 确认弹窗（本作配色）：「跳过剧情？」+ 金线 + 说明 + 取消/确认；确认后现网行为（ReturnToTitle 或跳到下个安全点——与现 `ShowConfirmDialog` 一致，确认=跳过退出）。
- 答题 UI 用 UI Toolkit，暖金面板，单选四题…实际为单选10题逐题。

### 2.4 合约

- `VNPrologueQuiz.Show(callback pass/fail)`；数据硬编码在 C# 或 JSON `Resources/vn_quiz.json`。
- 判定键：`PlayerPrefs.HasKey("XiaomiAccount")` 或变量 `got_0721`/`savedata`。

## [S3] Out of Scope

- 答题音效/BGM；多语言；重做 Menu 子菜单图标本身。

## Tasks

- [x] T1: 开场 Logo 暖金分镜 — acceptance: 按参考分镜或干净单屏，无冷蓝、无空对话框 (covers: S2.1)
- [x] T2: Backlog ✕ 不推进 — acceptance: 关闭历史后对话 index 不变 (covers: S2.2)
- [x] T3: 未注册跳过→答题 — acceptance: 无 XiaomiAccount 时 Skip 打开10题；≥7 可跳 (covers: S2.3)
- [x] T4: 注册后跳过→暖金确认弹窗 — acceptance: 弹窗样式对齐参考结构+本作配色 (covers: S2.3)
