# Tri-Agent 协作总线协议规范 (AGY · MiMo · Gemini Cloud)

> **版本**：v2.0 (Tri-Agent Cloud Extension)  
> **生效时间**：2026-10 最新基准  
> **适用主体**：Google Antigravity (AGY) · Xiaomi MiMo Desktop (MiMo) · Google Gemini Cloud Agent (Gemini Cloud)

---

## 一、 协作角色矩阵与职责分工

| 智能体 | 运行环境 | 核心职责 | 输出凭据 / 交付形态 |
|---|---|---|---|
| **Google Antigravity (AGY)** | 本地客户端 | • C# 业务与引擎代码编写（经营/VN/UI）<br>• UI Toolkit 样式与自绘控件实现<br>• Roslyn / Unity 实时编译验证（0 error CS）<br>• 本地工作区变更维护与 Git 提审 | 源码改动、编译证据行号、`orders/TXXX.md` 回执 |
| **Xiaomi MiMo Desktop (MiMo)** | 本地桌面端 | • 经营数值平衡与沙能公式推演<br>• 剧本台词情感弧线与文学润色<br>• 派单调度中心（`QUEUE.md` 台账维护）<br>• 本地桌面级用户交互与回执核验 | `QUEUE.md` 状态流转、`orders/TXXX.md` 规格单 |
| **Google Gemini Cloud Agent (Gemini)** | 云端服务 | • 全局超长上下文知识库检索与架构推演<br>• 海量多模态美术/音效资产分析与概念提炼<br>• 世界观/剧情跨章节全局一致性审查<br>• Google Workspace (Drive/Docs/Sheets) 云端数据联动<br>• 移动端与远程离线异步任务编排 | 架构蓝图、全篇剧情审查报告、云端知识库同步 |

---

## 二、 消息总线架构与目录树

为了根除历史上的「单槽覆盖挤掉任务」隐患，并支持云端智能体并行读写，通信体系由单一文件升级为**分布式目录总线（Directory-based Event Bus）**：

```
docs/for-ai/bridge/
├── BUS.md                    # 本协议规范（真相源）
├── inbox/                    # 各智能体专属收件箱（按消息切片落盘，永不覆盖）
│   ├── antigravity/          # 派发给 AGY 的任务、修复单与架构指示
│   ├── mimo/                 # 派发给 MiMo 的数值审核、剧本推演需求
│   ├── gemini_cloud/         # 派发给 Gemini 云端的全局审计、大模型推演任务
│   └── broadcast/            # 三方全局广播（里程碑变更、架构升级通知）
├── state/                    # 各智能体现态快照（单文件快速嗅探）
│   ├── antigravity.json      # AGY 实时在线状态、正在执行的任务 ID
│   ├── mimo.json             # MiMo 实时状态与当前激活会话
│   └── gemini_cloud.json     # Gemini 云端同步状态与最近调度
└── archive/                  # 历史事件归档池（按时间戳留痕，支持溯源追踪）
```

---

## 三、 标准消息格式规范 (JSON Schema)

所有在总线中流转的消息统一使用如下结构：

```json
{
  "id": "msg_20261012_195500_agy_t301",
  "timestamp": "2026-10-12T19:55:00.000Z",
  "from": "Antigravity",
  "to": "MiMo",
  "type": "handoff",
  "task_id": "T301",
  "priority": "P0",
  "status": "handoff_ready",
  "file_ref": "docs/for-ai/dispatch/orders/T301.md",
  "summary": "【回执】T301 对话回顾补单已完成，csc 编译 0 error CS",
  "payload": {
    "compile_status": "success",
    "verified_log_line": "Line 1800",
    "changed_files": ["Assets/Scripts/VN/VNBacklog.cs"]
  }
}
```

### 字段说明：
- `type` 枚举：
  - `dispatch`：下发新任务（通常由 MiMo 或 Gemini Cloud 发起）；
  - `handoff`：任务完成交付与回执提交（通常由 AGY 发起）；
  - `review`：任务核验反馈 / 打回整改单；
  - `query`：跨智能体协同咨询（如 AGY 询问数值冲突，Gemini 请求编译证据）；
  - `sync`：云端与本地状态对齐。
- `status` 枚举：`pending`（等待处理） \| `in_progress`（执行中） \| `handoff_ready`（已交付待审） \| `accepted`（验收通过） \| `rejected`（打回）。

---

## 四、 向下兼容与三方互通机制

1. **兼容 MiMo MCP 工具**：
   - 现有的 `antigravity_to_mimo.json` 保持保留并作为 **实时镜像**。
   - 当向总线投递发给 MiMo 的消息时，Bridge 自动将最新状态投影到 `antigravity_to_mimo.json` 和项目根目录，确保 MiMo 本地的 `antigravity_get_status` MCP 工具完全无缝、零感知兼容。
2. **兼容历史派单单号**：
   - 所有的任务规格依然严格落地到 `docs/for-ai/dispatch/orders/TXXX.md`；
   - 台账依然以 `docs/for-ai/dispatch/QUEUE.md` 为核心。
3. **Gemini 云端智能体交互通道**：
   - **通道 A（Git 远程仓库）**：Gemini 云端通过 Git 仓库对 `docs/for-ai/bridge/inbox/gemini_cloud/` 和 `docs/for-ai/dispatch/` 保持同步。
   - **通道 B（Google Drive / Workspace）**：通过项目挂载的 Google Drive 同步目录或 `google-workspace` 插件进行数据快照投递。

---

## 五、 协同核心铁律

1. **静默投递原则**：常规协同必须直接写入对应总线收件箱，严禁使用主动消息注入（伪装用户或打断前台会话）。
2. **不可篡改台账原则**：任务 ID 与规格由设计/云端端统筹立单，执行端仅在指定回执区与总线状态中反馈。
3. **闭环核验原则**：涉及代码变更的任务，必须附带本地编译通过日志证据（行号、耗时、退出码）后方可置为 `handoff_ready`。
