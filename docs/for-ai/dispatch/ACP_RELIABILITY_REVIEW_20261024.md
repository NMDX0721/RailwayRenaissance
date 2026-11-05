# ACP 派单可靠性与协作方案复核（2026-10-24）

## 结论

ACP 适合作为 Codex 到本地编码代理的会话/工具协议，但它本身不是可靠任务队列，也不保证模型服务可用、任务跨进程续跑、文件互斥或交付验收。最近两项 Antigravity 派单均已建立 ACP 会话、确认 `gemini-3.8-flash-medium`，随后在 `session/prompt` 阶段超时，工具事件反复返回模型服务 502，两个任务最终均 `changedFiles=[]`。这更像代理后端不可用叠加当前派单器缺少耐久任务层；不能据此归因于 ACP 协议或并行竞争。

建议近期**保留 ACP，不直接换 A2A/MCP**；先用任务/attempt 分离、工作树互斥、事件可见、协作式取消和人工验收形成可靠闭环，再评估 AHP-ACP Gateway 的独立试验。

## 首轮改造前的本地证据与根因边界

- `tools/compile/acp_tablet_dispatch.py` 的客户端以 `protocolVersion: 1` 握手，新建 ACP 会话后同步等待 `session/prompt`。超时后尝试发送 `session/cancel`，随后终止本地 ACP 子进程；没有恢复该会话、重新连接或检查远端会话结果的步骤（当前源码约第 30、84、96、102 行）。终止本地进程不证明服务端模型任务也已停止。
- 此客户端在开始 prompt 前就把结果状态设为 `sent`。若 prompt 抛错，状态仍是 `sent`，错误另记在 `error` 字段，状态名容易被误读为代理已接单。
- 所有运行都会覆盖 `tools/compile/acp_tablet_dispatch_result.json`；档案名只有任务单与代理名，重试会覆盖同一任务的上一份原始结果。并行运行还会争写共享的“最近结果”文件。这是派单器的确定性风险，但不是这次模型 502 的已证根因。
- 设置兼容重试 `ffd4817f-1bac-4f8f-95f3-fd03a24db780`：63 条模型服务 502 事件、10 条 pre-tool hook 拒绝、1 条 ACP 编辑请求拒绝，最终无文件变更。通知中心任务 `7a25ab6d-5031-4bb8-aac0-ba6354d73725`：40 条模型服务 502 事件、1 条编辑请求拒绝，最终无文件变更。拒绝的具体决策来源未从日志进一步确认，不能把它解释成模型故障。
- 2026-10-23 的本地诊断已建议暂时不要同时启动多个 Antigravity ACP 进程；本轮同时启动两项虽划分了不同文件，但违反了当时的保守运行建议。两项都出现 502，不能证明并行导致故障；串行回归试验应先于下一次并行写任务。
- 历史 10053、轨迹数据库权限失败、这次模型 502、编辑权限拒绝属于不同错误类别。现有证据不足以归为同一个根因。

原始本地记录：

- `tools/compile/acp_results/CODEX-AGY-TABLET-SETTINGS-COMPAT-20261024_antigravity.json`
- `tools/compile/acp_results/CODEX-AGY-TABLET-SHELL-POLISH-20261024_antigravity.json`
- `docs/for-ai/dispatch/reports/20261023_acp_parallel_session_diagnostic.md`

## 技术路线比较

| 方案 | 主要解决什么 | 对当前本机桌面代理的适用性 |
|---|---|---|
| **ACP** | 编辑器/客户端与编码代理之间的会话、prompt、流式事件、工具和权限交互 | 仍是当前 MiMo 与 Antigravity 的已验证入口。保留作执行适配层；它不提供项目级队列、跨任务互斥、审阅门或独立于后端的可靠性。当前本机 client 使用 v1 握手。 |
| **AHP** | 代理会话的权威状态、多客户端同步、动作排序和断线后回放；AHP host 可把 ACP 当后端 | 概念上最接近“让多个界面安全地查看/接续同一会话”。官方 AHP 文档说明可在上游使用 AHP、下游连接 ACP；但不保证修复模型后端 502。Automation Catalogue/Run 仍标为早期开发。 |
| **AHP-ACP Gateway** | 第三方开源 gateway 为 ACP v1 子进程加 SQLite 会话记录、重连回放、进程生命周期和权限呈现 | 值得在临时工作区小规模试验；它不是 Microsoft 官方产品，文档称 VS Code Agent Window 是主要端到端客户端，并列出未内建沙箱、只读资源接口、缺少 MCP 转发等限制。尚未验证能否可靠承载本机 Antigravity Desktop ACP 与 MiMo Code 配置。 |
| **A2A** | 独立代理服务之间的有状态任务、状态查询/取消、流式进展或 webhook 更新 | 若 MiMo/Antigravity 提供网络 A2A endpoint，会适合服务到服务任务委派；目前没有验证到这两个已安装桌面代理提供 A2A 服务，因此不是 ACP 的即插即用替换。 |
| **MCP** | 让一个 LLM host 发现并调用工具/资源/提示 | 可把队列、状态查询、日志或只读项目索引提供给 Codex；本身不是长任务编排器，也不会替其他代理保存执行状态。 |
| **GitHub Issue/PR 或云端 coding agent** | 持久任务入口、隔离分支/云沙箱、可审阅的 PR 交付 | 可作为未来仓库级异步派单模式，但它把执行迁到云端和另一套代理/权限，不会让当前桌面 MiMo 与 Antigravity ACP 变可靠；Unity 本地资源与未提交工作也使其不适合当前任务直接迁移。 |

资料：

- [ACP v2 Overview](https://github.com/agentclientprotocol/agent-client-protocol/blob/main/docs/protocol/v2/overview.mdx)
- [Microsoft AHP 与 ACP 的职责对照](https://microsoft.github.io/agent-host-protocol/guide/ahp-and-acp.html)
- [Microsoft AHP 协议仓库及实现清单](https://github.com/microsoft/agent-host-protocol)
- [AHP 稳定性分级](https://github.com/microsoft/agent-host-protocol/blob/main/docs/specification/versioning.md)
- [第三方 AHP-ACP Gateway](https://github.com/formulahendry/ahp-acp-gateway)
- [A2A v1.0.1 任务与更新机制](https://a2a-protocol.org/v1.0.1/specification/)
- [MCP SDK 对 Host / Client / Server 与工具资源的说明](https://py.sdk.modelcontextprotocol.io/get-started/first-steps/)
- [GitHub Copilot Cloud Agent 的隔离环境与 PR 流程](https://docs.github.com/en/copilot/concepts/agents/cloud-agent/about-cloud-agent)
- [Durable Task 的检查点、恢复与协调能力](https://learn.microsoft.com/en-us/azure/durable-task/common/what-is-durable-task)

## 建议的派单流程

### 立刻采用

1. **同一工作树只运行一个 ACP 任务**，不区分代理，也先不让只读审计与写入并行，以免读到混合文件版本。要并行时使用彼此独立的 Git worktree。
2. 每个派单分配稳定 `task_id` 和独立 `attempt_id`。每次尝试保存独立 JSONL 事件日志、结果 JSON、ACP session ID、模型、权限决定、错误类别、变更文件及规范文件哈希；不再让“最近结果”文件成为唯一真相。
3. 将状态明确分为 `queued → dispatching → prompt_accepted → running → input_required → result_received → review → accepted`，失败分 `retry_wait / failed / canceled`。ACP 的 prompt 已发送与 prompt 已接受分开记录；不要用单一 `sent` 表示两者。
4. 将错误分类后再决定重试：短暂 502 可在退避和一次轻量健康验证后新建 attempt；权限拒绝进入 `input_required` 等用户处理；10053/进程退出先查服务端会话；超时先核对文件差异并尝试已声明可用的 session resume，不能盲目重复执行写任务。
5. 同一文件只分配一个写入者。确需并行实现时，为每个 ACP worker 建独立 Git worktree/分支，交付 diff/commit，由 Codex 集成；同一共享 checkout 不做并行只读审计。
6. 把回执验收拆成代理文本、文件变更、C# 编译、Unity Play Mode 四个门。每个任务单须明确需要通过哪几门，避免“模型说完成”代替真实验证。

### 分阶段验证

1. **串行基线**：用一次只读 ACP 小任务检查“会话创建 → prompt 接受 → 最终回复 → 原始事件归档”。记录连续 5 次的成功率、等待时间、错误分类。
2. **单文件写入**：在临时 worktree 中做可逆单文件改动，验证授权请求、差异读取、取消与重试不会丢失或覆盖 attempt。
3. **恢复**：在 prompt 运行中断开本地客户端，验证服务器是否保留会话、`session/list` / `session/load` 或 `session/resume` 是否真实可用；没有实测就不承诺恢复。
4. **并发**：先在同一 ACP 进程创建两个独立 session，再单独测试两个 ACP 进程；记录数据库、Windows 10053、模型 502、工具拒绝。完成前不同时派发两个 Antigravity 写任务。
5. **AHP Gateway 试点**：仅在上述流程先稳定后，在临时目录配置自定义 ACP provider，检查 Antigravity ACP 1.4.1 和 MiMo ACP 是否能启动、认证、编辑、重连回放及权限确认；任何一步不兼容即停止，不触碰主工作树。

### 暂不建议

- 不把 A2A 当作桌面代理的即插即用协议替换。
- 不为了“耐久”立即引入 Azure Durable Task/Temporal 服务。先在本机 SQLite 实现任务状态、原始事件追加和互斥；只有任务跨天/跨设备、需要可靠定时或远程 worker 后，再评估 durable workflow 服务。
- 不让 ACP 失败后自动回退到旧文件信箱、自动 UI 操作或无审批终端；回退通道必须经过新一轮授权与安全设计。

## 本次结论边界

这是基于当前 ACP 结果和公开官方协议资料的设计建议，不是 AHP Gateway 已兼容本机代理的证明。本轮没有安装第三方 gateway、测试会话恢复或重跑 Unity。

## 2026-10-24 派单器首轮可靠性改造

在 `tools/compile/acp_tablet_dispatch.py` 外围加入轻量本地控制：

- 同一共享工作树使用一把 Windows 字节区间锁，不区分代理；锁覆盖 ACP 子进程全生命周期，重复启动会记录 `blocked_concurrent` 并退出，不创建 ACP 会话。这样 MiMo/Antigravity 也不能意外并发写同一 checkout。锁文件放在系统临时目录，不写进工作树；任务要并行需各自在独立 worktree。
- 每次启动生成 UUID `attemptId`，结果独立写入 `tools/compile/acp_results/attempts/{taskId}/`，不再覆盖共享 `acp_tablet_dispatch_result.json` 或覆盖此前同名任务的归档。
- 状态记录区分 `dispatching`、`process_started`、`session_created`、`prompt_in_flight`、`reply_received`、`result_received_no_text_reply`，错误分类为 `timed_out`、`provider_error`、`transport_error`、`permission_denied`、`failed`。运行阶段会原子更新 attempt JSON，保留 session、模型、权限工具审计、变更文件和错误阶段。
- 每次调用仍只尝试一次；没有自动重试、会话恢复或自动回退，避免写任务在结果不明时重复执行。收到代理回复/文件变更也仍需 Codex 审阅和相应工程验证后才能接受。
- `tools/compile/test_acp_dispatch_state.py` 覆盖唯一 ID、路径安全、错误分类和双进程互斥/释放；本地四项通过。尚未做真实 ACP 锁竞争、双代理并行或后端恢复试验。
- `--research` 现在把文件工具限制为只读，并按 MiMo/Antigravity 与审阅/实现任务提供不同角色提示；此前该开关未影响权限，且统一提示误称 MiMo 为 Antigravity。
- 派单状态现在同时保留一个不含对话正文的 JSONL 事件轨迹；每次 ACP 请求/会话更新写入事件，最长每5秒刷新一次状态快照。attempt 记录我方启动的子进程 PID、启动时间和最终是否确认退出。`acp_status.py` 按最近落盘状态/事件给出近期活动或陈旧告警；独立桌面 ACP 进程仅作为进程存在，不会被显示成任务进展。
- 初始化连接超时可配置，默认45秒。prompt 超时会进入 `cancelling`，发送 ACP `session/cancel` 并等待默认15秒的原始 prompt 响应；只有收到 `stopReason=cancelled` 才记 `timed_out_cancelled`。收到其他结束原因记 `completed_after_timeout`，没有最终响应记 `cancel_unconfirmed`。所有这些状态都禁止自动重派写任务。

因此这能避免本机同代理重复启动和历史结果互相覆盖，并改善失败可见性；它不能修复模型服务 502，也不证明超时后服务端任务已终止。2026-10-24 对只读按键复核单做串行基线时，首个 attempt 暴露线程启动早于子进程的客户端缺陷，未进入初始化；修正后第二个 attempt 在 ACP `initialize` 阶段 60 秒超时，无 session ID、无 prompt、无文件变化。另发现一个 10:03 启动的 AGY ACP 服务进程仍在本机，但它并非本次派单器管理的子进程；无 session/prompt/工具事件或文件交付，**不代表代理正在工作**。记录位于 `tools/compile/acp_results/attempts/CODEX-AGY-KEYBIND-RESET-REVIEW-20261024/`。当前仍未达到“串行基线通过”；不要据此判断任务内容或并发导致问题。

## 2026-10-24 超时与进展可观测性设计

### 官方/成熟系统采用的做法

- ACP v1 把 prompt 响应作为本轮结束信号；取消是协作式的：客户端发送 `session/cancel` 后，代理应在清理完成后以 `stopReason=cancelled` 回复。只结束客户端等待或杀掉本地子进程都不能当作远端取消确认。[ACP v1 Prompt Turn](https://github.com/agentclientprotocol/agent-client-protocol/blob/main/docs/protocol/v1/prompt-turn.mdx)
- ACP v2 将 prompt 入列确认与运行结束拆开，运行态通过 `state_update` 表达，取消后等待 idle/cancelled 作为确认。当前本地适配器仍用 v1；是否切 v2 要先实测 AGY/MiMo 兼容，不能只改版本号。[ACP v1→v2迁移说明](https://github.com/agentclientprotocol/agent-client-protocol/blob/main/docs/protocol/v2/migration.mdx)
- AHP 为多客户端会话提供权威状态、序列号、重连和事件重放，并可把 ACP 当后端；这解决客户端断线观察/接续层，不会让底层模型恢复，也不等同跨代理自动协作。[Microsoft AHP/ACP职责对照](https://microsoft.github.io/agent-host-protocol/guide/ahp-and-acp.html)
- Temporal 将总执行时长、单次尝试时长和 heartbeat 间隔拆开；heartbeat 要报告可证实进度，超时和重试策略分开。它提醒我们：没有进展事件时，只能标记 stale，不能伪装成“仍在运行”。[Temporal超时与重试](https://docs.temporal.io/evaluate/features/timeouts-and-retries), [Temporal Activity失败检测](https://docs.temporal.io/encyclopedia/detecting-activity-failures)
- SQS 用 visibility lease 防止工作被同时领取，处理中的长任务通过 heartbeat 延长；由于至少一次投递仍可能重复，要求任务幂等，并以 DLQ/隔离区收敛反复失败。[AWS SQS visibility timeout](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-visibility-timeout.html)

### 本地采用方式

我们不为两款桌面代理先引入 Temporal/SQS 服务。现在先用本机 attempt 快照 + 无正文事件日志 + 每共享工作树互斥锁实现相同的关键界限：任务与尝试分开编号、进展只由实际 ACP 事件证明、长任务时间到后请求协作取消并等确认、未确认时隔离而非自动重派。用 `python tools/compile/acp_status.py` 查看最新状态；`STALE` 表示没有新状态/事件，process PID 仅代表由该 attempt 启动的子进程。

后续升级顺序：①反重力/MiMo 各完成串行只读基线；②可逆小写任务验证权限、取消确认和改动审计；③再测单代理多任务/跨代理独立任务；④若需要多界面接续同一会话，再用临时工作区评估 AHP host；⑤只有任务跨机器、排队规模和耐久需求出现后，再上外部 workflow/queue 服务。

## 2026-10-24 Antigravity Harness 提案复核

### 可采纳的思路

- 把模型调用和任务运行时分开：运行时负责任务身份、超时、取消、权限、工具事件、验证门与结果归档。OpenClaw 的公开 agent-loop 文档也把上下文组装、模型推理、工具执行、流式事件、持久化和超时分别作为运行时职责；其 Harness 插件接口仍标为实验性，说明 Harness 是一种架构模式，不是已经统一定型的工业协议。[OpenClaw Agent Loop](https://docs.openclaw.ai/agent-loop)、[OpenClaw Harness 插件](https://docs.openclaw.ai/plugins/sdk-agent-harness)
- 在本机桌面 ACP 场景继续用 stdio 子进程，避免额外的 HTTP 中转层；OpenCode 官方 ACP 接入方式也是启动 `opencode acp` 子进程，通过 stdin/stdout 交换 JSON-RPC。[OpenCode ACP](https://dev.opencode.ai/docs/acp/)
- 任务完成后设置确定性、白名单验证门有价值，但应由本地调度器明确执行，而不是默认放开代理终端并无限自动修复。

### 对提案中根因说法的核对

- 当前 `acp_tablet_dispatch.py` 已直接用 `subprocess.Popen` 启动本机 `agy_acp_server.exe` 或 `mimo.exe acp`，并通过 stdin/stdout JSON-RPC 通信；没有 Codex→ACP 的 HTTP 代理或公网反向代理。因此“这几次 502 是 ACP HTTP 网关空闲超时/缓冲”的解释与本机客户端路径不符。模型服务请求仍由代理进程向上游发出，现有 ACP 事件报告的是模型服务 502；确切上游节点、请求超时或服务端故障仍需 Antigravity 后端日志才能定因。
- ACP v1 官方流程描述 stdio 子进程会话与流式 `session/update`，取消通过 `session/cancel`；这支持本机 stdio 作为当前桌面代理的合适通道，但不能单凭“stdio”推导零延迟、永不 502、自动展示在 Codex 桌面，或服务端取消必定成功。[ACP v1 会话建立](https://github.com/agentclientprotocol/agent-client-protocol/blob/main/docs/protocol/v1/session-setup.mdx)、[ACP v1 Prompt Turn](https://github.com/agentclientprotocol/agent-client-protocol/blob/main/docs/protocol/v1/prompt-turn.mdx)
- ACP/stdio 只描述客户端到本地代理进程的控制链路；代理进程访问模型服务仍可能经过网络，故本地 stdio 不能消除上游模型 502。
- 当前工具配置故意不给代理终端执行权限；若要编译/测试，应由调度器在受控步骤运行项目明确允许的命令，并记录退出码和产物。不能声称目前已有“报错自动回喂直到通过”的闭环。
- 502、初始化超时、审批拒绝是不同证据类别。ACP 规范没有规定“传输失败即自动拒批”；应按实际权限事件和服务错误分别记录，不把故障归因于安全策略。

### 对本项目的决定

不重建“Local stdio ACP”：它已经存在。下一阶段优先做**一次串行只读初始化基线**，分别保存启动、initialize、authenticate、session/new 的耗时和错误类别；初始化未成功时不发 prompt。基线通过后，再以单个临时 worktree 的可逆小改动验证写权限和结果审计。

确定性验收按任务单显式列出的白名单命令运行；只允许一次有预算的修复回合，且仅在首次检查失败、改动负责人仍在同一隔离 worktree、取消状态明确时启用。修复后重跑同一验收命令，仍失败就交给 Codex 审阅，不再自动循环。Unity 类任务的编译门仍使用项目允许的 Unity 批处理验证，不伪造通用 CLI 编译能力。

需要恢复/多客户端查看时，再评估 AHP host；它解决会话状态与断线回放，不解决模型服务 502。用户可见的 ACP 事件记录也不能等同于 Codex 桌面聊天面板实时显示。

## 2026-10-24 MiMo Code 对话 API 补充核验

MiMo Code 除 ACP 外还有本地 HTTP server/attach 路径。官方 README 展示 `mimo serve` 与 `mimo attach http://127.0.0.1:<port>` 的连接方式；官方仓库 issue #2360 中提供了 `POST /session`、`POST /session/<id>/message` 的 API 示例。后者是仓库 issue 记录，不是独立版本化 API 规范，稳定性承诺未知。

本机以 MiMo Code 0.1.15 实测：`mimo serve --pure --hostname 127.0.0.1 --port 0` 只绑定 loopback；`/global/health` 返回健康且版本为 0.1.15；按 provider `xiaomi-mimo-token-plan`、model `mimo-v2.6-flash` 建立临时 session 并发送一条无工具短 prompt，收到精确回复 `MIMO_HTTP_ROUNDTRIP_OK`。测试 session 已删除；服务已停止并确认 64380 无监听。

**结论**：Codex→MiMo Code Server 的独立会话双向消息已在本机验证，适合短只读咨询或复核，且避开 MiMo ACP 在项目 `.git\\info` 上的启动错误。它与 ACP 是两种不同接入路径；不能据此声称已接管 MiMo Desktop 当前打开的聊天。官方 README 说 MiMo Desktop 以 MiMo Code 为核心引擎，但没有据此确认桌面现有会话的外部消息注入 API。若要接近桌面旧会话，需先找到官方支持的会话持久化/导入/attach 接口并做只读实测。

当前路线选择：HTTP session 先用于独立、短、只读协作任务；ACP 保留给需要标准工具权限/会话事件审计的任务。不得让 HTTP channel 自动执行仓库写入，亦不得因 ACP 超时而自动用 HTTP 重投同一写任务。后续如要正式纳入派单器，需增加独立 session ID、超时/取消观察、原始事件与结果归档，并验证会话恢复和异常退出语义。
