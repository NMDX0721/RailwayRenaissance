# 《铁路复兴》双 Agent 协同开发与分工规划方案
> 版本：v1.0  
> 生效日期：2026-10-07  
> 协同方：Google Antigravity (AGY) & Xiaomi MiMo Desktop (MiMo)  

---

## 一、 核心定位与能力互补

| 协同方 | 核心定位 | 核心优势与职责 |
| :--- | :--- | :--- |
| **Google Antigravity (AGY)** | **全栈技术引擎 + 剧本精修主笔** | • 拥有本地文件系统、Roslyn 编译器（`csc.exe`）与 Unity 6 环境直接执行权。<br>• 负责 Unity C# 核心架构（经济模型、员工系统、事件系统、存档系统、场景过渡）。<br>• 负责 UI Toolkit 底层自绘（`VNBacklog.cs`、动效与 Shader/USS 优化）。<br>• 负责剧本作者级文戏精修、去水提速与动作链做实，并编译为合规 JSON。<br>• 负责秒级编译排错；Git 提交由 MiMo 终审后执行。 |
| **Xiaomi MiMo Desktop (MiMo)** | **世界观总控 + 经营数值策划 + 资产管线** | • 长期驻留项目全局记忆库，掌握完整历史线、角色设定集与世界观脉络。<br>• 负责经济模型公式推演（客流公式、折旧率、沙能竞争博弈模型、订单定价）。<br>• 负责审核剧本的世界观自洽性（时间线、车号、地名、势力博弈，严防吃书）。<br>• 负责多模态资产管线（AI 美术提词设计、图像尺寸与透明底验收、音频规划）。<br>• 负责项目长期记忆档案维护（`notes.md`、任务状态看板）。 |

---

## 二、 任务矩阵与责任划分

### 1. 视觉小说与剧情系统 (Narrative System)
* **05 巡线样板落地**：[Antigravity 主责] 将作者手改版（7551字）编译为正式 `prologue_05_inspection.json`，保留 thought / loc / Terms 标签。
* **序章 06~10 批量改写 (T269)**：
  - **MiMo**：提供各章关键剧情冲突点与数值钩子（如 06 周市长商业博弈、07 拆车分支、09 三笔资助轮播逻辑）。
  - **Antigravity**：按 05 样板标杆（单幕 ~1500-1800 字，首钩受挫前置，动作链具象）主笔精修，并构建正式 JSON。
* **剧情与数值联动 (T2)**：[Antigravity 主责] 实现 `VNManager.cs` 的 `TRANSITION_TO_GAMEPLAY`，根据剧本选择计算 `VNExitData` 注入经营主场景。

### 2. 模拟经营与核心玩法编程 (Gameplay & Engine)
* **沙本位经济核心重构 (T3)**：
  - **MiMo**：提供每日客流、燃料损耗、工资支出、维护成本的联动公式表。
  - **Antigravity**：重构 `GameData.cs` 的 `AdvanceDay()` 结算循环与剧情补贴注入接口。
* **先民人事系统 (T4)**：[Antigravity 主责] 编写 `CrewManager.cs`，实现 5 名员工（老陈/张工/李阿姨/赵师傅/小芳）的 4 项技能成长与疲劳衰减。
* **随机事件系统 (T5)**：[Antigravity 主责] 编写 `EventManager.cs`，解析 `Resources/events.json`，按 35% 概率触发经济/运营/剧情突发事件。
* **沙能竞争系统 (T6)**：[Antigravity 主责] 编写 `SandRivalManager.cs`，实现 30 天行动轮与城市渗透率对抗。
* **统一存档系统 (T8)**：[Antigravity 主责] 实现 `VNSaveSystem.cs`，整合视觉小说与经营场景的三槽位读写。

### 3. UI 与交互打磨 (UI Toolkit)
* **回顾界面滚动栏打磨 (T277~T279)**：[Antigravity 主责] `VNBacklog.cs` 滚动栏物理手感微调、高斯模糊 USS、连续滚轮支持。
* **术语卡与图鉴交互 (T276/T280)**：[Antigravity 主责] 拼音索引与词条弹窗接线，守卫防止子串误判。

### 4. 资产生成与验收 (Assets Pipeline)
* **角色与背景提词 (A1/A2)**：[MiMo 主责] 编写 Stardew Valley 风格 16-bit 像素画提词。
* **资产入库与绑定**：[Antigravity 主责] 负责透明底 Sprite 与 Meta 文件绑定至 Unity 项目。

---

## 三、 双 Agent 通信与回流协议

1. **正向派单 (MiMo $\rightarrow$ Antigravity)**：
   - 走原生 MCP `antigravity_assign_task` 通道。
   - 附带任务需求、相关设计文档路径与预期交付标准。
2. **反向回流 (Antigravity $\rightarrow$ MiMo)**：
   - 走 Desktop API `/v1/sessions/:id/turns` 通道（`call_mimo.js`）。
   - 交付物格式规范：
     ```text
     【交付阶段】任务编号与名称
     【落地代码】修改的文件清单及主要改动
     【编译状态】Roslyn csc 校验退出码（EXIT=0）
     【遗留问题】需策划确认的设计点或下一步安排
     ```
3. **记忆沉淀**：重大架构改动和阶段性里程碑由双方分别实时同步到 `AGENTS.md` 和 `notes.md`。
