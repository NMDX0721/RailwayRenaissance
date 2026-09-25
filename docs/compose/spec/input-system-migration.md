---
feature: input-system-migration
status: in-progress
updated: 2026-09-26
branch: main
commits:
---

# Input System 迁移规划

## Report

## [S1] Problem

Unity 已标记 Input Manager（`UnityEngine.Input`）为弃用。项目约 15 处直接调用 `Input.GetKey*` / `Input.GetMouse*`，集中在 VN 推进、快捷键、设置改键。若不迁移，后续升级 Unity 版本可能失去输入支持。

## [S2] Design

### 2.1 策略：双轨并行，先包一层

不一次性替换全部调用点。先加 Input System 包，把 `KeyBindings` 与少量散点收口到适配层，`Active Input Handling = Both` 作为过渡，跑稳后再切 `Input System Package Only`。

### 2.2 阶段

| 阶段 | 内容 | 验收 |
|------|------|------|
| P1 | manifest 加 `com.unity.inputsystem`；ProjectSettings 切 `ActiveInputHandler = Both` | 编译通过，现有 Input 调用仍工作 |
| P2 | 新建 `LegacyInputAdapter`：`IsKeyDown/GetKey/GetMouseButtonDown` 内部先走旧 API | 无行为变化 |
| P3 | `KeyBindings.IsDown/IsHeld` 改为读适配层（PlayerPrefs 改键逻辑不动） | 改键设置页正常 |
| P4 | 替换散点：`VNManager.Update`、`TypewriterEffect`、`FullScreenNews`、`StationBulletinUI` 改键捕获 | 空格推进/Shift 加速/ESC/F5/右键隐藏 与迁移前一致 |
| P5 | （可选）新增 `.inputactions` 映射 Advance/Skip/Menu；适配层切到新 API | 行为回归通过 |
| P6 | `ActiveInputHandler = Input System Package Only`；删旧调用 | 无 `UnityEngine.Input` 引用（`Input.mousePosition` 等一并处理） |

### 2.3 合约

- `LegacyInputAdapter.GetKeyDown(KeyCode)` / `GetKey` / `GetMouseButtonDown`
- 鼠标坐标：UI Toolkit 走 `RuntimePanelUtils.ScreenToPanel`，不依赖旧 Input 的也可保留
- `KeyBindings` 的 PlayerPrefs 键名 `KB_*` 不变，避免玩家改键丢失

### 2.4 不迁移范围

- UI Toolkit 的 `PointerDown/ClickEvent`（已是独立事件系统）
- 新游戏功能若只用新 API，直接用 Input System

## [S3] Out of Scope

- 手柄/触屏映射（迁移稳定后再加）
- 立即删除旧 Input（P6 前保持 Both）

## Tasks

- [ ] T1: 加包 + ActiveInputHandler=Both — acceptance: 编译 0 error，游戏输入无回归 (covers: S2.2 P1)
- [ ] T2: LegacyInputAdapter + KeyBindings 收口 — acceptance: 改键设置仍生效 (covers: S2.3; depends: T1)
- [ ] T3: 替换 VN/打字机/新闻/设置 散点 Input 调用 — acceptance: 空格/Shift/ESC/F5/右键行为与迁移前一致 (covers: S2.2 P4; depends: T2)
- [ ] T4: 切 Only + 全局搜旧 API 清零 — acceptance: `Input.GetKey` 等旧调用为 0 (covers: S2.2 P6; depends: T3)
