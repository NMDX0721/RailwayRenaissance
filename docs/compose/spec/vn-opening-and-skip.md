---
feature: vn-opening-and-skip
status: delivered
updated: 2026-09-24
branch: main
commits: de9ae4e..de9ae4e
---

# 开场Logo / 关闭叉 / 跳过闸门

## Report

**What was built** — 序章开场改为暖深棕底三镜分镜（Logo 图 → 金标题+金线 → 制作信息），逐条淡入淡出，点击任意处跳过；「点击继续」仅在 credits 站稳后淡入。历史对话 ✕/面板点击经 `StopImmediatePropagation` + `VNManager.SuppressNextAdvance()` 吞掉同帧 root 推进。故事跳过入口（菜单 ReturnBar / ESC → `RequestSkip`）按闸门分流：Day0 前（无 `VN_Got0721` 且无 `Username`）弹 10 题答题（≥7 通过 → `prologue_02_day0` 并写标记）；已注册/已过 Day0 → 暖金确认弹窗，确认=`ReturnToTitle`。新周目清除 `VN_Got0721`；开场分镜期间 `AdvanceBlockedByPanels` 挡空格穿透。

**Verification** — Unity 6000.5.8f1 batchmode：`error CS` = 0，`Mono: successfully reloaded assembly` PASS。`Failed to resolve packages` 为 PRE-EXISTING（Package Manager path undefined，与本改动无关）。独立审查 T1–T4 全 PASS，无 CRITICAL。

**Journey log**:
- 曾误用 Unity 6000.4.6f1 编译导致包解析失败；项目实际为 6000.5.8f1（`D:\Unity Hub\Unity 6.5`）。
- `HasPassedDay0Gate` 不用 `XiaomiAccount`（GameMainUI 会兜底成「旅人」）；以 `VN_Got0721` 或非空 `Username` 判定。
- 菜单双箭头保持场景内快进 `SkipToNext`；故事跳过走 `ReturnBar → RequestSkip`。
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
