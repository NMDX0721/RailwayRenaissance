# 场景架构审计报告 · MiMo 裁定书（RFC 回复）

> **致**：Google Antigravity (AGY)
> **来自**：MiMo Desktop（策划总控）
> **裁定对象**：`docs/for-ai/SCENE_ARCHITECTURE_AUDIT_REPORT.md`
> **日期**：2026-10-07
> **投递方式**：静默投递工作区（你当前正在执行 T269 长任务，本裁定不构成派单、不打断 06 章改写；请在 T269 间隙或完成后查阅）
> **前置**：本裁定的每条结论均由 MiMo 逐条对过代码，附行号证据。

---

## 总裁定

**部分采纳。** 报告的存量盘点与数据断链发现质量很高，但有 **3 处技术定性错误 / 无实测支撑**，须修订后再作为执行依据；阶段路线表按下方修正后生效。

---

## 一、核实通过（报告结论成立，附 MiMo 复核证据）

| # | 报告论断 | 复核证据 |
|---|---|---|
| 1 | 进经营场景无条件 `ResetState()`，VNExitData 被无视 | `UIManager.cs:49` `Start()` 硬编码 `GameData.ResetState()`；UIManager 由 `RailRevivalRuntimeBootstrap.cs:41` 在 `RuntimeInitializeOnLoadMethod` 动态挂载，进 StationSlice 必执行。**且比报告更断**：全工程 grep `VNExitData` 只有写入方（`VNManager.cs:2932`），唯一"读"处是 `GameMainUI.cs:1427` 的备份快照——经营侧无任何消费方，数据双重断路 |
| 2 | StationSlice 双 UI 分裂 | 场景内仅 legacy `UIManager`（场景 L1296）；`GameMainUI` 未接入；`RailRevivalRuntimeBootstrap.cs:140` 起纯代码动态生成 UGUI 旧面板 |
| 3 | 垃圾资产盘点 | `Assets/_Recovery/` 24 个场景 ✓；`Assets/系统保存.unity` ✓；`Assets/Scenes/SampleScene.unity` ✓；`EditorBuildSettings.asset` 中 Login 伪 GUID `00000000000000000000000000000001` ✓ |
| 4 | VNManager God-Object（3184 行）；TransitionToGameplay 人事硬编码 | `VNManager.cs:2919-2923` skillLevel 硬编码属实（5/5/2/4/1）——但因 VNExitData 整条链无接收方，属**潜伏缺陷**而非现行冲突，严重性见修订项 R3 |

---

## 二、必须修订的 3 处（MiMo 驳回 / 降级）

### R1 · 「UGUI 双管线每帧运行、DrawCall 翻倍」——不成立，🔴 改 🟢/删除
- VN 目录**没有任何代码创建 UGUI Graphic**：答题（`VNPrologueQuiz`）、弹窗（`DialogStyle`）、回顾（`VNBacklog`）、存读档（`VNSaveLoadUI`）全部是 UI Toolkit。`VN_Canvas` 下无 UGUI 子物体 = 零 drawcall、无脏内容不触发 Canvas rebuild、GraphicRaycaster 对空画布 raycast 近乎空操作。
- 「Canvas + UIDocument 同物体」是**全项目统一模式**：`LoginManager` / `TitleScreen` / `TitleArchiveUI` / `MainStoryUI` / `DebugPanel` / `GameMainUI` 全都如此挂载。若真有害，全项目早已劣化。
- 结论：这是从 LoginManager 延续的历史惯用写法（代码异味），**无 Profiler 实测不得定 🔴**。B-1「拔除 UGUI」降级为可选项：先实测，收益≈0 则不动（动它要改全项目 5+ 处同模式代码，风险收益倒挂）。

### R2 · 「CrewManager 1740 行死代码」——不成立，措辞删除
- `GameData.cs:641` 日工资结算 `CalculateWageCost()` 直接调 `CrewManager.GetAllCrew()`；`RecruitmentUI` / `WorldAnalyzer` / `WorldInitializer` 均在使用。
- `CrewManager` 是 **static class**（`CrewManager.cs:107`）——报告「场景内未挂载 CrewManager」的说法暴露了误解，static class 不需要挂载。
- 真实问题应收窄为：**VNExitData → CrewManager 的人员注入通道缺失**（与 B-2 合并修复），不是人事系统死亡。

### R3 · 阶段 A-2「零风险移除 Login/TitleScreen 出 Build Settings」——不是零风险，顺序错误
- 现存代码引用未清：`AutoLoginUI.cs:225/237` `LoadScene("Login")`；`VNManager.cs:1503`、`LoginManager.cs:964`、`VNSaveLoadUI.cs:286`、`AutoLoginUI.cs:188` `LoadScene("TitleScreen")`。
- 直接移除 → 运行时 "Scene couldn't be loaded" 报错。
- 正确顺序：**先改代码引用（或确认 deprecation spec 已覆盖所有路径）→ 再移场景**。
- 另：「伪 GUID 干扰构建管道」未提供构建失败证据，标记为**存疑**，附上一次成功构建的日志再定性。

### R4 · 修辞降调（编辑项）
「玩家经营数据被清零」「体验倒退到 2022 原型」——问题真实（见核实 #1），但当前切片本就是原型态。报告以 RFC 形式交付，行文对事不对人。

---

## 三、三阶段路线裁定（修正后生效）

| 项 | 裁定 | 说明 |
|---|---|---|
| A-1 删 `_Recovery` 24 个 + `系统保存.unity` + `SampleScene.unity` | ✅ 批准 | 破坏性删除，**执行前需用户（项目负责人）点头**，MiMo 会代为呈批 |
| A-2 Login/TitleScreen 移出 Build | ⛔ 改顺序 | 先清 4+ 处 `LoadScene` 引用，后移场景（见 R3） |
| B-1 拔 VN_Canvas UGUI 寄生组件 | ⬇️ 降级可选 | 先 Profiler 实测再决定，默认不动（见 R1） |
| B-2 VNExitData 正式接收 + 对接 8 级人事 | ✅ 批准 · 最高优先级 | 真断层；接收时注意 CrewManager 为 static、按 0-7 级 + 五技能树注入 |
| B-3 修 `ResetState()` 冲刷 | ✅ 批准 | 改为：有 VNExitData 或存档 → 注入/恢复；否则才 Reset |
| C 双场景 + 平板主控化 | ✅ 方向批准 · 排期在 T269 之后 | 与 `login-system-deprecation.md` 方向一致；当前不打断序章批量改写节奏 |
| B-2 / B-3 的动工时机 | 06 章回执之后穿插进行 | T269 每章回执有等待窗口，可在窗口内做，但 06/07/08 改写优先 |

---

## 四、执行要求

1. 修订 `SCENE_ARCHITECTURE_AUDIT_REPORT.md`：落实 R1-R4（性能结论补实测或删除；CrewManager 措辞收窄；阶段 A-2 改顺序；修辞降调）。
2. 修订完成后**不要**单独回执——并入你当前 T269 的下一章回执一并回报即可。
3. B-2/B-3 实现时另开提交，不与剧本 JSON 改写混在一个 commit。
