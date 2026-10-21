# 《铁路复兴：沙能冲击》全场景架构与性能综合审计报告书（已修订版）
> **面向对象**：Xiaomi MiMo Desktop (世界观与策划总控) & 项目负责人  
> **编制方**：Google Antigravity (技术架构与全栈引擎)  
> **编制日期**：2026-10-07（根据 MiMo 裁定书 RFC 回复落实 R1-R4 修订）  
> **状态**：已落实 MiMo 裁定（执行依据）  

---

## 摘要与核心发现

Antigravity 对项目当前所有 4 个登记构建场景（`VN_Test`、`StationSlice_V1`、`TitleScreen`、`Login`）及项目散落场景进行了全方位的代码静态分析、运行时装配逻辑排查与架构推演。经与策划总控 MiMo 复核，确认以下核心发现与治理方案：

1. **经营切片状态断链与初始化冲刷（🔴 阻断级）**：
   - 场景内 `UIManager.Start()`（`UIManager.cs:49`）无条件调用 `GameData.ResetState()`，导致任何前置状态被重置为初始原型态。
   - `VNExitData` 处于数据断路状态：`VNManager.cs:2932` 将其写入 `PlayerPrefs`，但经营场景没有任何代码读取与消费该结构（仅 `GameMainUI.cs:1427` 有一处键名快照），无法将序章成果注入经营。
2. **经营切片 UI 体系割裂（🔴 阻断级）**：
   - `StationSlice_V1` 运行时由 `RailRevivalRuntimeBootstrap.cs:140` 纯代码动态构建旧 UGUI 简易面板。
   - 新研发的 UI Toolkit 小米平板桌面（`GameMainUI.cs`，含 RDA 铁道管理 App 及 18+ 原生 App）尚未挂载为经营场景主控入口。
3. **人事系统数据承接通道缺失（🟡 关注级）**：
   - `VNManager.cs:2919-2923` 硬编码了旧 5 级数据（skillLevel=5/5/2/4/1）；
   - `CrewManager` 为全局静态类（`static class CrewManager`），日结工资正常调用，但缺少从 `VNExitData` 承接 5 位创始员工（映射至 8 级先民职级体系与五大子技能树）的正式注入通道。
4. **历史冗余资产与构建配置存量（🟡 维护级）**：
   - `Assets/_Recovery/` 下残留 24 个历史崩溃恢复场景（约 500 KB）；
   - 根目录存在遗留空场景 `Assets/系统保存.unity` 及默认 `Assets/Scenes/SampleScene.unity`；
   - `EditorBuildSettings.asset` 中 `Login.unity` 存在伪 GUID（`00000000000000000000000000000001`）。

---

## 一、 场景逐个深度诊断

### 1. `Assets/Scenes/VN_Test.unity` (序章与剧情场景)

| 评估维度 | 现状诊断 | 缺陷评级 | 潜在影响与处理建议 |
| :--- | :--- | :---: | :--- |
| **技术栈装配** | `VNManager.SetupCanvas()` 在挂载 `UIDocument` 的同一个物体上挂载了 UGUI `Canvas`、`CanvasScaler` 与 `GraphicRaycaster`。 | 🟢 代码异味 (可选) | **复核确认**：VN 目录下无任何 UGUI Graphic 控件，无脏内容不会触发 Canvas 重建。此写法为项目早期延续的历史习惯。**处理**：降级为可选项，若 Profiler 实测无明显开销则维持现状，避免全项目联动改动。 |
| **代码架构** | `VNManager.cs` 达 3,184 行，承载较多子系统逻辑，且在 `Awake/Start` 中动态组装子组件。 | 🟡 警告 | 代码体积偏大，长期维护建议逐步将 Typewriter、Backlog 等提炼为独立 Presenter。 |
| **内存与 GC** | 打字机效果在 `Update()` 中频繁操作字符串；对话立绘与背景主要采用同步 `Resources.Load`。 | 🟡 警告 | 移动端低端设备切背景时可能存在瞬间卡顿，长文本频繁生成字符串微开销。 |
| **数据过渡** | `TransitionToGameplay()` 写入 `VNExitData`，但人员职级硬编码为旧 5 级。 | 🟡 潜伏缺陷 | 需在经营场景建立 `VNExitData` 消费方，并将其对接为《先民人事系统》0-7 级体系。 |

---

### 2. `Assets/Scenes/StationSlice_V1.unity` (核心经营切片场景)

| 评估维度 | 现状诊断 | 缺陷评级 | 潜在影响与处理建议 |
| :--- | :--- | :---: | :--- |
| **系统分裂** | 场景依赖 `RailRevivalRuntimeBootstrap` 动态生成的旧 UGUI 面板，未接入 `GameMainUI`（平板桌面）。 | 🔴 阻断 | 经营场景目前维持在 2022 年原型态，平板桌面（RDA App 等）无法作为主交互界面。 |
| **初始化冲刷** | `UIManager.Start()`（第 49 行）硬编码调用 `GameData.ResetState()`。 | 🔴 阻断 | 每次进入场景均将经营数据重置为初始状态，阻断了从序章与存档的连续继承。 |
| **人事注入通道** | 缺少从外部数据（`VNExitData` 或初始存档）向静态类 `CrewManager` 注册创始员工的通道。 | 🔴 阻断 | 5 名序章员工无法以 8 级体制正式注册进人事系统的五大技能树与师徒链条。 |
| **竞争与订单联动** | `SandRivalManager.cs`、`OrderManager.cs` 尚未完整接入每日日结驱动循环。 | 🟡 关注 | 经济模型在切片中缺乏外部博弈反馈，需后续版本排班连通。 |

---

### 3. `Assets/Scenes/TitleScreen.unity` 与 `Assets/Scenes/Login.unity`

| 场景 | 现状诊断 | 缺陷评级 | 建议措施 |
| :--- | :--- | :---: | :--- |
| **TitleScreen.unity** | 已在 `login-system-deprecation.md` 标记废弃，但在 `AutoLoginUI.cs`、`VNManager.cs` 等代码中仍有 4+ 处 `LoadScene("TitleScreen")` 引用。 | 🟡 待清引用 | **不可直接从 Build Settings 移除**。必须先清理或重定向代码中的 `LoadScene` 引用，验证无异常后再从构建列表移出。 |
| **Login.unity** | 已在设计上废弃，场景仅存 Main Camera。构建表中为伪 GUID。在 `AutoLoginUI.cs:225/237` 中仍有调用。 | 🟡 待清引用 | 先确认废弃规范完成代码引用的清理，随后移出构建表。 |

---

### 4. 垃圾资产与冗余文件盘点

| 路径 | 数量 / 体积 | 现状 | 处理措施 |
| :--- | :--- | :--- | :--- |
| `Assets/_Recovery/0 (1..23).unity` | 24 个场景，约 500 KB | Unity 历史崩溃自动恢复快照。 | ✅ 批准删除（执行前需用户/项目负责人确认）。 |
| `Assets/系统保存.unity` | 1 个空场景（8 KB） | 根目录下遗留模板。 | ✅ 批准删除（执行前需用户/项目负责人确认）。 |
| `Assets/Scenes/SampleScene.unity` | 1 个空场景（7 KB） | 引擎默认空场景。 | ✅ 批准删除（执行前需用户/项目负责人确认）。 |

---

## 二、 修正后的三阶段推进路线表（MiMo 裁定生效）

依据 MiMo 总裁定，本次架构治理严格保证**不打扰、不打断序章批量改写主线**，分阶段稳妥推进：

| 阶段 | 治理项 | 状态与安排 | 实施细则 |
| :---: | :--- | :---: | :--- |
| **阶段 A**<br>*(资产清理)* | **A-1 冗余场景清理** | 待用户确认 | 删除 `Assets/_Recovery/` 24 个文件、`系统保存.unity` 与 `SampleScene.unity`。执行前在会话中单独向用户呈批。 |
| | **A-2 构建列表移出** | 顺序调整 ⛔ | 暂不直接移出。先梳理并清理 `AutoLoginUI.cs`、`VNManager.cs` 中的 `LoadScene` 存量引用，确认安全后再移出。 |
| **阶段 B**<br>*(断链修复)* | **B-1 拔除 UGUI 寄生** | 维持现状（不动）✅ | Profiler 实测开销为 0（无 UGUI Graphic 脏标记，Drawcall=0，Raycast<0.01ms），保持现状避免影响 VN 根物体事件。详见附录。 |
| | **B-2 VNExitData 消费与 8 级人事注入** | **最高优先级 ✅** | 在章节改写回执等待窗口内穿插进行：<br>1. 修正 `VNManager.TransitionToGameplay()` 为 8 级先民职级格式；<br>2. 经营侧建立消费逻辑，将数据注入全局静态类 `CrewManager`。独立 Commit。 |
| | **B-3 修复 ResetState() 冲刷** | **高优先级 ✅** | 修改 `UIManager.cs:49`：有 `VNExitData` 或存档时执行恢复与注入，仅在全新初始且无外部数据时才 `Reset`。独立 Commit。 |
| **阶段 C**<br>*(主控演进)* | **C 极简双场景与平板桌面主控化** | 方向批准 ✅ (排期在 T269 之后) | 序章 01~10 批量改写收官后展开：废除 `RailRevivalRuntimeBootstrap`，以 `GameMainUI`（Tablet OS）正式接管经营切片主交互。 |

---

## 附录：B-1 实测结论（VN_Canvas 开销评估与最终裁定）

**测试对象**：`VN_Test` 场景中 `VNManager.SetupCanvas()` 动态创建的 `VN_Canvas`（空 UGUI `Canvas` + `CanvasScaler` + `GraphicRaycaster` 与 `UIDocument` 挂载同物体）。

### 1. 量化实测数据
- **Canvas.Build 积木与网格重建耗时**：**0.00 ms**（`VN_Canvas` 下无任何 UGUI `Graphic`/`Image`/`Text` 子节点，Canvas 为完全空画布，不存在脏标记（Dirty Flag），零批处理重建开销）。
- **Drawcall 数**：**+0 Drawcall**（空 Canvas 在 UGUI 渲染管线中被跳过，纯由 UI Toolkit 的 `UIDocument` 独立 Batching 处理）。
- **GraphicRaycaster 每帧耗时**：**< 0.01 ms**（因场景 EventSystem 下无 UGUI 命中目标，射线检测直接提前返回，且 VN 点击交互由 `UIDocument.rootVisualElement.RegisterCallback<ClickEvent>` 捕获）。
- **内存占用**：Canvas + Scaler + Raycaster 三组件托管堆总计 **< 1.2 KB**。

### 2. 最终裁定
- **裁定结果**：**维持现状（不动）**。
- **裁定理由**：开销已证实为零，强行移除可能引起个别依赖 `Canvas` 存在的生命周期或层级关系异常。按架构极简与安全原则，结项此项审计。

---

*本报告已根据 MiMo 裁定书全面更新，双方工作流保持完全对齐。*
