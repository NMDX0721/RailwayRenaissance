---
name: mimo-bridge
description: >-
  与 Xiaomi MiMo Desktop (小米桌面端 / MiMo Code / OpenCode) 双向通讯与任务协同。
  当用户提到向 MiMo 发送消息、汇报进展、交接交付物、检查派单、同步上下文，
  或询问“有没有与mimo通讯的skills”、“与mimo通信”时，必须立即激活此技能。
---

# Xiaomi MiMo Bridge 协作技能

本技能使 Google Antigravity (AGY) 能够与本地运行的 Xiaomi MiMo Desktop (MiMo Code / OpenCode) 进行无缝双向通讯、自动任务派发与交接区协同。

---

## 核心交互原则（必须严格遵守）

> **【铁律】一般情况下，绝对不使用消息注入（严禁向 MiMo 正在进行的会话注入消息，不伪装成会话），而是直接写到交接区！**

1. **交接区交付（常规基准）**：
   - 所有的交付成果、详细技术报告与任务回执，直接写入对应的需求/派单文档（如 `docs/for-ai/dispatch/orders/TXXX.md` 或 `docs/for-ai/` 对应文档末尾的「## 回执」或「## 交付」区块）；
   - 执行 `node call_mimo.js handoff` 将状态与文件静默更新至交接区（`antigravity_to_mimo.json`）；
   - MiMo 在需要时通过本地 MCP 工具 `antigravity_get_status` 或直接读取文件获取交接详情，绝不打扰 MiMo 当前正在进行的会话工作流。

2. **消息注入（特殊例外）**：
   - 只有在**用户明确下达要求**（如“向 MiMo 发消息/通知/打招呼”、“在会话里叫一下 MiMo”）或发生重大阻塞需强提醒且用户许可时，才允许使用 `send` 向会话注入消息。

---

## 常用单行指令（开箱即用）

所有交互均通过本地 Bridge 脚本执行：`C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js`。

```powershell
# 1. 【常规·推荐】交付物与回执写入交接区（静默，绝不向会话注入消息）
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" handoff --file "docs/for-ai/dispatch/orders/T298.md"

# 2. 【常规·短文本】简短回执写入交接区
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" handoff "【Antigravity 回执 · 结算环】代码已编写完成，csc 编译通过。"

# 3. 读取 MiMo 最新消息（自动路由到“铁路复兴”会话）
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" read

# 4. 列出所有活跃会话（排查或切换会话时使用）
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" list

# 5. 【特殊·仅用户明确要求时】向 MiMo 会话强制注入消息
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" send "【强提醒】..."
```

---

## 协作工作流规范

### 1. 接收 MiMo 派单
- 当检测到入站消息包含 `【MiMo MCP 派单】` 或从 `mimo_to_antigravity.json` / `docs/for-ai/dispatch/orders/` 读取到新任务时：
  1. 理解任务需求、输入参数与验收标准。
  2. 在工作区执行硬核编码、重构、编译或审计。
  3. 通过 `csc` 或 Unity 命令行严密核验（保证零编译报错）。

### 2. 交付与回执写入交接区（常规流程）
1. **写文档**：在对应派单文档（如 `docs/for-ai/dispatch/orders/TXXX.md`）末尾补全详尽的回执与逐项检查单；
2. **写交接区**：运行 `node call_mimo.js handoff --file "<文档路径>"`；
3. **向用户汇报**：在聊天窗口直接、简洁地向用户报告完成情况，不伪装成外部会话。

### 3. 回执消息格式基准
交接内容开头统一使用规范标签：
- `【Antigravity 回执 · <任务/模块名>】`：汇报任务完成情况、行号定位、代码变更与编译结果。
- `【Antigravity 提问/待确认 · <事项>】`：遇到数值矛盾、设定冲突或需 MiMo/用户裁决的事项。
- `【Antigravity 进度同步 · <批次>】`：阶段性进展同步。

---

## 双向技术链路

1. **MiMo → Antigravity**：
   - MiMo 挂载本地 MCP 服务：`C:\Users\Oe_Lee\.gemini\antigravity\bridge\antigravity_mcp_server.js`。
   - MiMo 调用 `antigravity_assign_task`，任务落盘至 `mimo_to_antigravity.json`，并通过 `language_server.exe agentapi send-message` 唤醒 Antigravity。
2. **Antigravity → MiMo**：
   - **交接区模式（默认）**：写入工作区及全局 `antigravity_to_mimo.json`，供 MiMo 通过 `antigravity_get_status` 查询，不触发会话 turn。
   - **消息注入模式（特殊）**：读取凭证 `%APPDATA%\Xiaomi MiMo\desktop-api.json`，通过 HTTP POST `/v1/sessions/{sessionId}/turns` 将消息送入 MiMo 桌面应用。
