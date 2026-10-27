# 平板整体体验升级与验收

## 2026-10-26 实施状态补记

新工作树已接入导航、互斥系统面板、最近任务、应用内页面栈、生命周期和内存压力视图回收；设置页支持宽度重排，图鉴与岁月入口接入任务。Unity 编译及手势/键盘、设置子页回收、图鉴选择/直达、岁月历史和音乐界面清理检查通过（`tools/compile/tablet_interaction_review_lifecycle_final_20261027.log`，退出码 0）。独立滚动布局检查因无图形批处理缺少有效布局标为 NOT_RUN；真实触屏、输入法、方向布局、其他应用恢复与真实音乐播放仍需验收。见 [系统外壳说明](TABLET_SYSTEM_SHELL.md)。

## 用户目标与分工

目标是让游戏内平板形成可用、一致、可配置的完整体验，不以通讯探针或单个原型完成替代整体完成。Codex只协调、审查和验收；Antigravity 3.8 Flash / Medium负责主要实际编码；MiMo 2.6 Flash负责信息整理、交互/数据边界审计和验收设计。音乐模块已有独立开发，当前派单不修改音乐源文件。用户取消桌面可见过程要求，以效率和可核验交付为准。

## 必须持续推进的范围

| 体验范围 | 当前证据 | 仍需完成的工作/完成证据 |
|---|---|---|
| 系统导航、通知/控制面板 | TabletSystemShellReview.Batch与旧日志通过1440/1024合成事件 | 保留面板互斥、Escape分层、锁屏清理、焦点恢复；设置分类11接真实控制，通知策略与具体业务生产者核对 |
| 设置与数据安全 | 外观独立JSON、坏文件/未来版本/保存失败保护与分类9实际UI已验收 | 24分类逐项治理；危险操作二次确认、备份范围准确、搜索/子页返回与键位恢复；经济/VN/音乐数据隔离 |
| 桌面与全域外观 | 壁纸压暗、减少解锁动效、系统面板强调色和对比度已检查 | 桌面实际可用个性化、主题消费边界与动效一致性；不把局部高对比度说成所有App已适配 |
| 真正背景模糊 | ACP真实读写/固定命令探针已通过，工程模块已投递 | 完整服务/Shader/真实UI捕获/输入/生命周期原型、Unity实际像素与UI测试；然后接入正式系统面板，处理降级与减少动效 |
| 内容与状态可信 | 禁止冒称真实联网、登录、云同步及硬件状态 | 占位不能冒充有效开关，不凭空写电池/无线连接；保留沉浸的世界观文案，读写态及错误提示与实际行为一致 |
| 验收与呈现 | 既有通知与外观日志含成功标记，非真机QA | 每批编译、真实平板入口回归；1440/1024布局/拾取、输入框、缩放、锁屏、重开/销毁；真实模糊前后图与测量。Android设备成本/触摸未验证必须明列 |

## 任务顺序

1. AGY实际写入模糊模块（六份白名单），MiMo用当前源码产下一批设置/导航任务卡；两者并行，无同文件编码冲突。
2. Codex检查交付完整性，运行Unity编译和模块验证；失败交AGY修复，不由协调者接替实现。
3. AGY按MiMo经源码复核的任务卡治理设置和导航/数据安全，同时将真模糊接入正式平板系统层。具体所有权与测试命令分单，不同时派两个编码者争写同一文件。
4. 完成桌面个性化/通知策略缺口、完整系统回归、实际效果截图和文档状态对齐；剩余设备限制不能冒称已通过。

每轮区分已发送、收到确认、真实落盘、编译通过、行为验收通过。不能用回复篇幅或ACK代替交付。

## 当前活动单

- `CODEX-AGY-ACP-BLUR-WRITE-20261023`：原生ACP真实文件任务，等待完整交付。
- `CODEX-MIMO-TABLET-UX-NEXT-20261023`：当前源码快照（约112KB）审查，要求两张可执行后续任务卡，不重复过时问题。

权威状态见 dispatch/QUEUE.md 与按任务保存的ACP原始结果；此计划不是功能完成声明。

## 2026-10-24 当前源码复核与新分工（覆盖上方旧活动单状态）

MiMo 已对当前工作树做独立只读审计，回复记录在 [复核报告](dispatch/reports/20261024_mimo_tablet_current_state_review.md)，ACP 原始 attempt 在 `tools/compile/acp_results/attempts/CODEX-MIMO-TABLET-CURRENT-STATE-REVIEW-20261024_f43e8202/`。本轮 prompt 在 240 秒超时后收到 `end_turn`；它是迟到的交付内容，不是干净的限时通过。源码结论已由 Codex复核：模糊 service/Shader/preview/review 已在工作树；正式 `TabletShellView._scrim` 仍为纯色，`GameMainUI` 与 shell 没有生产模糊引用。设置分类 9 已接入；分类 11 仍无 switch case；主线和经济重置回调仍缺二次确认。历史审计里“设置恢复出厂 DeleteAll”已过时，设置页当前用 JSON 白名单流程；勿复派旧修复。

新的执行顺序为：① Antigravity `CODEX-AGY-TABLET-SHELL-BLUR-INTEGRATION-20261024` 完成正式 shell 捕获/高斯接入与降级；② 独立核差异并进行 Unity 真实入口验收；③后续治理分类 11 / 危险重置、占位与跨尺寸回归。用户要求从 2026-10-25 起暂停 MiMo 派单但保留通道，因此后续任务目前由 Antigravity 承担。所有共享工作树 ACP 任务串行。

WP1 已投递一次，ACP 确认模型为 `gemini-3.8-flash-medium` 并实际写过 `TabletShellView.cs`；但 600 秒后确认取消，期间多次模型/工具调用 502，无最终回执。正式系统层尚未完成捕获与模糊调用，Review/文档/Unity验收也未交付；当前 `TabletShellView.cs` 为未跟踪文件，改动需先核验。发现终态 attempt 留下孤儿 ACP/harness 进程，清理后 54326 的旧 socket 已释放；其后 Antigravity ACP 能开始执行，但仍遇到模型后端 502 和 Google 请求 `unexpected EOF`。代理 `127.0.0.1:7891` 的基本 HTTP 访问正常，不能据此确认模型流稳定。不要盲目重投完整 WP1；先解决/短测 Antigravity 后端，再继续实现。平板升级仍未完成。


## 2026-10-24 桌面端 Antigravity 会话实现交付与验收 (DESKTOP-AGY-TABLET-SHELL-BLUR-IMPLEMENT-20261024)

- **执行完成**：由 Antigravity (Gemini 3.8 Flash, Medium) 在当前 Desktop 会话完成真实背景模糊落地与验收，未依赖不稳定代理通道。
- **改动范围**：
  1. `Assets/Scripts/UI/Tablet/SystemShell/TabletShellView.cs`：接入 `OnRequestCapture` 回调、`ApplyBlurredBackground` 模糊纹理挂载、`PrepareForCapture` 0透明度防递归捕获、高对比度模式自动禁用模糊回退至深色实底遮罩。
  2. `Assets/Scripts/GameMainUI.cs`：引入 `TabletGaussianBlur` 实例管理与按需刷新调度，双路径支持（`targetTexture != null` 离屏直接 blit；`targetTexture == null` 屏幕空间 `ScreenCapture.CaptureScreenshotIntoRenderTexture` 于 `WaitForEndOfFrame` 执行）；窗口缩放自适应尺寸检测；`OnDisable`/`OnDestroy` 释放 RenderTexture。
  3. `Assets/Scripts/UI/Tablet/Rendering/TabletGaussianBlur.cs`：修复未预创建 RenderTexture 传入时的安全自动 `source.Create()` 支持。
  4. `Assets/Editor/TabletSystemShellReview.cs`：在 Stage 2 与 Stage 3 增加背景模糊激活与关闭的严格断言。
- **核验证据**：
  1. Unity 6.5 batch 编译：退出码 0，`compile_check_final.log` 零 `error CS`。
  2. `TabletSystemShellReview.Batch`：Play Mode 自动化全流程通过，包含真实平板入口、真实背景模糊激活/关闭、通知勿扰、Escape 优先级、设置跳转、锁屏再解锁与 1440/1024 双尺寸自适应。输出日志 `TABLET_SHELL_RUNTIME_OK` 与 `TABLET_PRESENTATION_RUNTIME_OK`。
  3. `TabletGaussianBlurReview.Batch`：算法核验通过，标准高斯权重归一化与对比度衰减通过。
  4. 审查截图：`tools/compile/tablet_presentation_review_v1/` 输出 `01-desktop.png`、`02-notifications.png`、`03-controls.png`、`04-controls-1024.png`、`05-personalization.png`。
- **未验证项**：Android 实体设备 Mali/Adreno 架构 GPU Profiler 实际每帧带宽开销未测量；多指手势与电磁触控笔悬停 QA 仍待后续真机手动验收。


## 2026-10-25 车载平板连接设置去假化完成 (DESKTOP-AGY-TABLET-CONNECTIVITY-SETTINGS-20261025)

- **执行完成**：由 Antigravity (Gemini 3.8 Flash, High) 在当前 Desktop 会话完成分类 4–7 连接设置去假化与专属说明页建设。
- **改动范围**：
  1. `Assets/Scripts/UI/Tablet/Apps/TabletSettingsAppUI.cs`：为分类 4「WLAN」、5「蓝牙」、6「互联互通」、7「更多连接方式」分别实现专属详情页（`RenderWlanSettings`、`RenderBluetoothSettings`、`RenderInterconnectSettings`、`RenderMoreConnectionsSettings`），替换通用三行占位模板。
  2. 消除误导交互：所有说明行采用 `BuildWhiteRow(..., null, false)`，去除了假点击回调、假 Toast 提示与可点击箭头（`›`），`pickingMode` 设为 `Ignore`。
  3. 文案边界：仅说明此终端不提供公共 Wi-Fi、蓝牙配对、个人账户同步、热点/投屏设置入口，不显示实时连接状态；移除无项目设定依据的硬件与线路拓扑断言。
  4. `Assets/Editor/TabletSettingsConnectivityReview.cs`：建立分类 4–7 自动化验收套件，并核查分类 11 入口、调用通知 Review；系统壳更广的面板交互回归仍由独立 Review 负责。
- **核验证据**：
  1. Unity 6.5 batch 编译：退出码 0，`tablet_settings_connectivity_review_20261025.log` 零 `error CS`。
  2. `TabletSettingsConnectivityReview.Batch`：Test 1–4 分类页用例通过。Test 5 检查分类 11 入口并调用通知 Review；通知 Review 的 1–3 项通过，但第 4 项系统壳运行回归明确 `NOT RUN`。主任务日志输出 `TABLET_SETTINGS_CONNECTIVITY_REVIEW_OK`，但这不代表系统壳完整回归。
- **未验证项**：Android 实体平板真机上的电磁兼容性、多点触控滚动与外接键盘导航未在物理硬件上手动 QA。
