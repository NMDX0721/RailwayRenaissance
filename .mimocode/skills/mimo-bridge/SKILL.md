---
name: mimo-bridge
description: >-
  与 Xiaomi MiMo Desktop 及 Google Gemini 云端智能体进行 Tri-Agent 双向通讯与任务协同。
  当用户提到向 MiMo/Gemini 发送消息、汇报进展、交接交付物、检查派单、同步上下文，
  或询问“与mimo通信”、“云端智能体协作”时，必须立即激活此技能。
---

# Tri-Agent Bridge 协作技能 (AGY · MiMo · Gemini Cloud)

本技能使 Google Antigravity (AGY) 能够与本地运行的 Xiaomi MiMo Desktop (MiMo Code / OpenCode) 以及 **Google Gemini 云端智能体** 进行无缝多向通讯、去单点覆盖任务派发与分布式总线协同。

---

## 核心交互原则（必须严格遵守）

> **【铁律】一般情况下，绝对不使用消息注入（严禁向 MiMo 正在进行的会话注入消息，不伪装成会话），而是直接写到交接区与分布式总线！**

1. **总线与交接区交付（常规基准）**：
   - 所有的交付成果、详细技术报告与任务回执，直接写入对应的需求/派单文档（如 `docs/for-ai/dispatch/orders/TXXX.md` 末尾「## 回执」区块）；
   - 执行 `node call_mimo.js handoff`，系统将静默写入 **Tri-Agent 消息总线**（`docs/for-ai/bridge/inbox/mimo/`）并镜像更新 `antigravity_to_mimo.json`；
   - MiMo 在需要时通过本地 MCP 工具 `antigravity_get_status` 或直接读取文件获取交接详情，绝不打扰 MiMo 当前正在进行的会话工作流；
   - Gemini 云端智能体通过 Git 仓库或云盘同步直接读取 `docs/for-ai/bridge/` 及 `docs/for-ai/dispatch/`，实现跨端无缝协同。

2. **消息注入（特殊例外）**：
   - 只有在**用户明确下达要求**（如“向 MiMo 发消息/通知/打招呼”、“在会话里叫一下 MiMo”）或发生重大阻塞需强提醒且用户许可时，才允许使用 `send` 向会话注入消息。

---

## 常用单行指令（开箱即用）

所有交互均通过本地 Bridge 脚本执行：`C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js`。

```powershell
# 1. 【常规·推荐】交付物与回执写入交接区与总线（静默，绝不向会话注入消息）
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" handoff --file "docs/for-ai/dispatch/orders/T301.md"

# 2. 【多智能体总线】向 Gemini 云端智能体定向投递任务或审计请求
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" post --to gemini --task "ARCH-01" --file "docs/for-ai/PROJECT_OVERVIEW.md" --summary "请 Gemini 云端审查跨系统架构"

# 3. 【多智能体总线】拉取待阅消息 / 检查收件箱
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" poll antigravity

# 4. 【看板】查看 AGY、MiMo、Gemini Cloud 三方实时在线状态看板
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" status

# 5. 读取 MiMo 最新消息（自动路由到“铁路复兴”会话）
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" read

# 6. 列出所有活跃会话（排查或切换会话时使用）
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" list

# 7. 【特殊·仅用户明确要求时】向 MiMo 会话强制注入消息
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" send "【强提醒】..."
```

---

## 三方角色定位与协作流

| 角色 | 主体 | 职责定位 |
|---|---|---|
| **代码与运行核** | Google Antigravity (AGY) | 本地 C# 实现、UI Toolkit 样式、Unity/Roslyn 零报错编译、Git 变更提交 |
| **策划与数值核** | Xiaomi MiMo Desktop | 本地数值推演、剧本对白打磨、本地派单与台账流转（`QUEUE.md`） |
| **云端与智识核** | Google Gemini Cloud Agent | 全局长上下文知识检索、架构一致性审查、Google Workspace 联动、移动离线编排 |

### 规范流转：
1. **立单**：MiMo / Gemini 云端将规格落盘至 `docs/for-ai/dispatch/orders/TXXX.md`，并在总线派单；
2. **执行**：Antigravity 执行代码与编译排错；
3. **交付**：Antigravity 填写文档回执并通过 `call_mimo.js handoff` 静默写入总线；
4. **验收**：MiMo / 用户实测验收，核验后更新 `QUEUE.md`；Gemini 云端进行宏观一致性归档。

---

## 技术链路与总线文件架构

- **分布式总线结构**：`docs/for-ai/bridge/`
  - `BUS.md`：总线标准协议；
  - `inbox/`：`antigravity/`、`mimo/`、`gemini_cloud/`、`broadcast/`（去单点覆盖）；
  - `state/`：三方实时状态快照（`antigravity.json`, `mimo.json`, `gemini_cloud.json`）；
  - `archive/`：历史时序存档池。
- **向下兼容层**：自动镜像写入 `antigravity_to_mimo.json`，MiMo MCP 工具 `antigravity_get_status` 始终免配置可用。
