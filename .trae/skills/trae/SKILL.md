---
name: trae
description: Use when working inside Trae IDE on this RailRevivalSim project, especially when tasks should be executed through the rail-revival-bridge-v2 MCP server. Follow the inbox -> read -> claim -> doing -> edit files -> outbox -> done/blocked workflow, keep Chinese-first output, and avoid freeform refactors when there is no active inbox task.
---

# Trae 协作技能（RailRevivalSim）

先按下面顺序工作，不要跳步。

## 1. 先确认协作入口
优先依赖这几个位置：

- `.trae/rules/rail-revival-collaboration.md`
- `agent-bridge/trae/context/project-context.md`
- `agent-bridge/trae/inbox/`
- `agent-bridge/trae/outbox/`

如果需要精确工具参数或完整流程，读取：
- `references/bridge-workflow.md`

## 2. 有 MCP 就走 MCP，不要只看文件夹
如果当前项目里能看到 `mcp_rail-revival-bridge-v2`，优先直接使用它的工具，而不是手动猜状态。

标准开工顺序：
1. `inspect_bridge_status`
2. `list_inbox_tasks`
3. 选择最新、最合适、状态为 `todo` 的任务
4. `read_inbox_task`
5. `claim_task`
6. `update_task_status(... doing ... )`
7. 修改项目文件
8. `write_outbox_result` 或 `write_blocked_report`
9. `update_task_status(... done/blocked ... )`

## 3. 任务选择规则
- 优先处理最新的 `todo` 任务。
- 如果最新任务明显是刚才做到一半的续作，可继续该任务。
- 如果没有明确任务，不要自行大范围发挥；等待新任务或先汇报当前状态。

## 4. 实现要求
- 中文优先。
- 只围绕当前任务落地，不擅自扩展到大地图、多线路、科技树、随机世界。
- 优先复用现有脚本和 UI 结构，避免无谓重构。
- 修改后要能说明：改了哪些文件、为什么改、结果如何验证。

## 5. 回写要求
完成时必须回写结构化结果，而不是只写聊天总结。

结果里至少写清：
- 状态
- 修改文件列表
- 做了什么
- 还缺什么（如果有）
- 下一步建议

阻塞时必须写清：
- 卡在哪
- 缺什么输入/资源
- 建议谁处理

## 6. 行为边界
- 不要因为“可以做更多”就扩任务。
- 不要跳过 claim / doing / done 协议。
- 不要把英文按钮、英文主提示塞进当前主 UI。
- 如果工具参数报错，先查看 MCP 工具 schema，再重试，不要瞎猜参数名。
