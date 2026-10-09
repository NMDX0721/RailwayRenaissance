---
name: mimo-bridge
description: >-
  与 Xiaomi MiMo Desktop (小米桌面端 / MiMo Code / OpenCode) 双向通讯与任务协同。
  当用户提到向 MiMo 发送消息、汇报进展、交接交付物、检查派单、同步上下文，
  或询问“有没有与mimo通讯的skills”、“与mimo通信”时，必须立即激活此技能。
---

# Xiaomi MiMo Bridge 协作技能

本技能使 Google Antigravity (AGY) 能够与本地运行的 Xiaomi MiMo Desktop (MiMo Code / OpenCode) 进行无缝双向通讯、自动任务派发与回执同步。

## 常用单行指令（开箱即用）

所有交互均通过本地 Bridge 脚本执行：`C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js`。
*脚本已内置项目会话自动路由，会自动匹配标题包含「铁路复兴」的会话，无需手动复制 sessionId。*

```powershell
# 1. 读取 MiMo 最新消息（自动路由到“铁路复兴”会话）
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" read

# 2. 发送单行短消息或回执
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" send "【Antigravity 回执 · 结算环】代码已编写完成，csc 编译通过。"

# 3. 发送长篇报告/Markdown 文件（推荐！彻底规避命令行引号与换行转义问题）
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" send --file "docs/for-ai/DAILY_SETTLEMENT_REPORT.md"

# 4. 列出所有活跃会话（排查或切换会话时使用）
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" list

# 5. 手动指定特定会话发送
node "C:\Users\Oe_Lee\.gemini\antigravity\bridge\call_mimo.js" send "消息内容" --session ses_fff4da3a1ffe6u2I9ZapIUz1Dp
```

---

## 协作工作流规范

### 1. 接收 MiMo 派单
- 当检测到入站消息包含 `【MiMo MCP 派单】` 或从 `mimo_to_antigravity.json` 读取到新任务时：
  1. 理解任务需求、输入参数与验收标准。
  2. 在工作区执行硬核编码、重构、编译或审计。
  3. 通过 `csc` 或 Unity 命令行严密核验（保证零编译报错）。

### 2. 向 MiMo 提交回执（必做）
- **绝不能只在 Antigravity 聊天窗口回复**！
- 每次完成交付，必须调用 `call_mimo.js send` 将正文推送到 MiMo 桌面端；
- `call_mimo.js send` 会**自动**更新 `antigravity_to_mimo.json` 磁盘状态，确保 MiMo 通过 MCP `antigravity_get_status` 查询时同步命中最新回执。

### 3. 回执消息格式基准
为方便 MiMo 自动归档与终审，回执开头统一使用规范标签：
- `【Antigravity 回执 · <任务/模块名>】`：汇报任务完成情况、行号定位、代码变更与编译结果。
- `【Antigravity 提问/待确认 · <事项>】`：遇到数值矛盾、设定冲突或需 MiMo/用户裁决的事项。
- `【Antigravity 进度同步 · <批次>】`：阶段性进展同步。

---

## 双向技术链路

1. **MiMo → Antigravity**：
   - MiMo 挂载本地 MCP 服务：`C:\Users\Oe_Lee\.gemini\antigravity\bridge\antigravity_mcp_server.js`。
   - MiMo 调用 `antigravity_assign_task`，任务落盘至 `mimo_to_antigravity.json`，并通过 `language_server.exe agentapi send-message` 唤醒 Antigravity。
2. **Antigravity → MiMo**：
   - 读取凭证：`%APPDATA%\Xiaomi MiMo\desktop-api.json`（动态端口 + Bearer Token）。
   - 通过 HTTP POST `/v1/sessions/{sessionId}/turns` 将消息送入 MiMo 桌面应用。
   - 自动落盘最新状态至 `antigravity_to_mimo.json`。
