---
feature: vn-character-scene-fixes
status: in-progress
updated: 2026-09-24
branch: main
commits:
---

# VN 人物场景错位与交互修复

## Report

## [S1] Problem

序章播放中普遍出现：
1. **人物错位**：对话框说话者与屏上立绘不一致（如导师说话时只有林彪悍、教室里错挂嘉颖徐/岁月）。
2. **缺立绘仍占位错误**：`导师` 无贴图时不应把表情算到别人头上；应占位注册人物，禁止 fallback 到林彪悍等。
3. **场景错误**：应出现驾驶舱的对话仍在停机坪等错误 bg。
4. **人物过早登场**：场景不该出现的角色提前进 `chars`。
5. **背景人物无说明**：停机坪背景里的管理员需在剧情中交代身份。
6. **CG「待生成」点不掉**：点击 placeholder 不关闭。
7. **操作问题**：空格误亮 Auto；Auto 点击仍推进；Menu 点不动；解锁奖励弹窗过丑且不该在剧情中弹出；长按空格应加速（Shift 为跳过式快进）。

## [S2] Design

### 2.1 立绘匹配契约（引擎）

- `SpeakerAlias`：显示名 → 贴图 ID；**未映射名不得落到已有角色贴图**。
- 加载失败时：**不写入任何角色 sprite**；显示占位色块+「{显示名} 立绘待绘制」（如 `daoshi`）。
- `isSpeaker` 仅当 `spriteId == speakerId` 时为 true。

### 2.2 场景 chars 与 bg（JSON）

- 每个 dialogue 场景的 `chars` 只包含**本场实际在场**角色。
- `prologue_02` 教室：去掉错误的 `jiaying_xu`；`daoshi` 占位在场。
- 上车后独立 `cabin_interior` 场景；停机坪仅交接段。
- 停机坪成年男子 = **大学车队管理员**（非导师）；旁白写明；`导师` 单独占位立绘。

### 2.3 空格 / Auto / Menu

- **短按 Space**：推进/跳过打字；**不**切换 Auto。
- **长按 Space（≥0.35s）**：加速推进（~0.25s/句）；松开结束。
- **Auto**：`focusable=false` + `ClickEvent` StopPropagation；手动推进关闭 Auto 高亮。
- **Menu**：同 Auto；打开只暂停协程，关闭时若 Auto 仍开则重启。
- Shift+→/← 仍为跳过式快进/回退。

### 2.4 解锁弹窗

- 剧情话末**不再**弹「解锁完成！」；解锁静默入档后直接节过渡。

### 2.5 CG 待生成

- `ClickEvent` 对 cgScreen **任意子元素**关闭并 `StopImmediatePropagation`。

## [S3] Out of Scope

- 导师/车队管理员完整 16 表情绘制。
- 序章开场叙事重写（电话 vs 导师带消息）。
- 美术资产批量重生成。

## Tasks

- [x] T1: CharacterSpriteManager 占位与 speaker 匹配 — acceptance: speaker=导师时林彪悍不继承 emotion；缺图显示占位 (covers: S2.1)
- [x] T2: 修正 prologue_02 chars/bg — acceptance: 教室无嘉颖徐；上车后 cabin_interior；停机坪挂 daoshi (covers: S2.2)
- [x] T3: 旁白写明车队管理员 — acceptance: 停机坪段出现管理员身份说明 (covers: S2.2)
- [x] T4: Space 短按推进/长按加速 — acceptance: 短按不亮 Auto；长按加速 (covers: S2.3)
- [x] T5: Auto/Menu ClickEvent 隔离 — acceptance: focusable=false + ClickEvent 阻断冒泡 (covers: S2.3)
- [x] T6: 剧情中禁用重奖励弹窗 — acceptance: TakePendingUnlocks 后直接 ShowEpisodeClear (covers: S2.4)
- [x] T7: CG placeholder 点击关闭 — acceptance: 任意子元素点击关闭 (covers: S2.5)

