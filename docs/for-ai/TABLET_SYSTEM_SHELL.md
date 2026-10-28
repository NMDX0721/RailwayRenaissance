# 平板系统层与协作计划（2026-10-23）

PR #7 笔记内存修复：任务回收或关闭时仅释放属于该任务视图的笔记静态控件引用；回收前仍先捕获草稿快照，恢复时重建并还原输入。笔记视图内部重建也先释放旧控件。

Unity 图形批处理 `TabletInteractionReview.Batch` 退出 0，`tools/compile/pr7_notes_reclaim.log` 包含 `TABLET_NOTES_RECLAIM_OK`：实际任务回收后静态控件引用为空，标题与正文草稿恢复，关闭后再次清空引用。此检查不测量 Android 真机内存或证明垃圾回收时机。

PR #7 后续修正：返回和同名子页重建移除完整窗口；全部关闭清理任务页面、状态及卡片引用；每次模糊采样前隐藏壳层并等待帧结束。图形缩放采样仍需实际画面验证。

验证：Unity 图形批处理 `TabletInteractionReview.Batch` 退出 0，日志 `tools/compile/pr7_pages_followup.log`；回归检查返回后父页面可见且无残留子页窗口。全部关闭后的垃圾回收和分辨率变化时的实际捕获画面仍待专项验证。

PR #7 后续：平板模糊 shader 作为 Resources 资源纳入工程；归途和 RDA 回到前台时刷新底层页面并保留页面栈与快照；通知存储加载失败后只读直到成功加载；术语重建委托只持有选中词条状态；最近任务接收焦点并阻挡后台键盘输入。实际图形与平台验证状态见本轮验收记录。

2026-10-28 验证：Unity 图形批处理 `TabletInteractionReview.Batch` 退出 0（`tools/compile/pr7_followup_tablet_final.log`），包括损坏通知文件不被保存覆盖、有效文件重新加载后恢复写入、shader 资源加载与实际黑白边界模糊，以及既有任务回收回归。归途进度变化、RDA 运营数据刷新、最近任务键盘输入和图鉴旧视图垃圾回收尚未分别做运行时专项验证；Android 真机和实际面板视觉效果待验。验证临时使用本机同版本 Burst 包，未改变依赖配置。

PR #7 修复：计算器引擎随任务 AppState 存活，视图回收后保留当前输入与待算运算，主动关闭任务才重置；界面卸载解除引擎事件订阅。V2 本机备份缺失结构或固定键映射时拒绝恢复且不写存储。见 [修复与验证](PR7_REVIEW_REPAIR.md)。

## 2026-10-27 系统导航与最近任务首轮实现

- `GameMainUI` 提供底部返回/主页/最近任务入口；返回优先关闭系统面板，再取消重绑定，随后关闭最近任务或把 Escape 派给当前焦点控件；未消费时退回应用内上一页，最后回桌面。重绑定捕获的键有同帧消费保护。回桌面保留任务。
- `TabletPageEntry` 保存应用内页面栈和重建委托；设置的备份、图鉴、事务、剧情和应用管理子页可返回上一层，回收后重建页面路径。同标题刷新替换当前页，避免重复入栈；缺少重建委托的子页暂不回收。图鉴重建委托保留用户最后选择的词条。
- `ITabletTaskLifecycle` 统一通知视图创建/恢复、前台、后台、视图回收和任务关闭。音乐回收/关闭时暂停界面调度并清除静态控件引用，保留播放与选页状态；后台音乐提示走系统通知通道。岁月对话用任务状态保留历史，主动关闭任务后清除；现有快捷回复不代表真实 AI 或实时运营分析。
- 最近任务卡切回原任务；用户关闭卡片才结束任务。内存回收按最近激活顺序处理非活动视图，回收前暂存 `TextField` 值与 `ScrollView.scrollOffset`；恢复文本通过普通 `value` setter 通知控件回调；恢复滚动会等待有效视口和目标范围，最多一秒后按当前范围限幅。Android `Application.memoryUsageChanged` 提供系统高/临界压力信号，Windows 另用 Unity 分配内存软预算轮询。该机制不承诺后台进程保活。
- `TabletEdgeGesture` 只跟踪一根从屏幕边缘开始的触点：上沿左右区分别下拉通知/控制面板；底沿上滑后停留 0.28 秒会在松手前打开最近任务，快速上滑回桌面；侧沿只接受向内滑动。监听器不截获指针，侧沿过滤按钮、文本框、滑块、开关和滚动区；锁屏清除手势状态且不接受新手势。需用 Windows 触屏与 Android 真机验证滚动/拖动误触和多指行为。
- 通知与控制面板互斥，面板按横竖屏和宽高自适应；设置页在 760px 以下切单列，宽屏保留侧栏。`KeyBindings.ResetToDefaults()` 删除 `KB_` 键位项并保存，不清除其他 `PlayerPrefs`。
- Unity 6000.5.8f1 编译及 `TabletInteractionReview.Batch` 通过，日志 `tools/compile/tablet_interaction_review_lifecycle_final_20261027.log`，进程退出码 0，包含 `TABLET_INTERACTION_REVIEW_OK`。覆盖手势、草稿回调、返回/重绑定、锁屏、设置任务与子页回收重建、图鉴词条/直达、岁月历史及生命周期/音乐界面引用释放。音乐检查没有播放真实音频。独立 `ScrollBatch` 在无图形批处理下视口为 NaN，返回 `TABLET_SCROLL_REVIEW_NOT_RUN`（`tools/compile/tablet_scroll_review_20261027.log`，退出码 2）。滚动位置、Play Mode、真实触屏、输入法和 Android 仍需验证。
- 此工作树原先缺少 36 个与已跟踪 `.meta` 对应的音视频文件；已从本机原工程复制并逐个核对 SHA-256。这些被 Git 忽略的媒体仅存在于当前工作树，其他工作树需要自行准备。

最新状态：用户要求以双AI分工完成整体体验，完整范围及验收见 [体验升级](TABLET_EXPERIENCE_UPGRADE.md)。反重力独立ACP实际文件读写及受控原生命令探针已通过，当前模糊工程任务已发送，不再限于文本交付；具体本单只有六份文件写入权限。下方仅文本/未验收说明为历史记录；外观消费者与存储目前已由顶部所列Unity日志验收。MiMo新单基于更新后源码复核设置/导航，避免沿用旧审计结论。

## 外观偏好接入（不是列车外形设计）

反重力返回`TabletPresentationPreferences`及`TabletPresentationSettingsView`，Codex审查后接入个性化分类9。独立文件`TabletSystem/presentation-v1.json`保存在应用持久化目录，覆盖壁纸压暗、解锁减少动效、系统面板高对比度与强调色；不修改3D场景光照、VN样式或音乐独立设置。高对比度目前明确限于系统面板/该外观页，不宣称所有App已经统一主题。默认恢复只处理此文件；新版本文件只读，坏版本/写入失败保留原文件。

审查修复：不接受先删原文件再移动临时文件的降级路径；缺失schemaVersion视为损坏；恢复默认允许显式修复默认值状态的坏文件；非法强调色/非有限压暗值拒绝；视图离开面板后取消订阅。Codex还移除了开发者页无确认`PlayerPrefs.DeleteAll`入口，锁屏设置改为真实的轻触/空格操作说明，去掉空回调唤醒开关；无障碍高对比度入口统一导航到外观设置。没有改动开发者经济调试函数。

`TabletPresentationStorageReview`与扩展`TabletSystemShellReview`提供独立路径/真实UI验证。`tablet_presentation_runtime_stdout.log`退出0，`TABLET_PRESENTATION_STORAGE_OK`和`TABLET_PRESENTATION_RUNTIME_OK`通过：JSON往返、写入失败原文件保留、非法值、缺版本恢复、未来版本只读、真实分类9控件、壁纸即时压暗、减少动效即时解锁、默认恢复取消/确认；原通知/控制面板回归同轮通过。截图位于`tools/compile/tablet_presentation_review_v1/`。外观模块来自ACP源码交付，Codex修正和集成不等同反重力直接写入工作区。

## 有工作量的完整模块派单

用户随后明确要求双方承担真正工程任务：`CODEX-AGY-TABLET-BLUR-MODULE-20261023`要求模糊服务、实际Gaussian Shader、独立UI捕获预览、验证入口及文档五项交付；`CODEX-MIMO-TABLET-FULL-AUDIT-20261023`附完整24类设置源码和消费者定位，要求逐项审计、状态矩阵、>=15验收用例及后续任务卡。ACP客户端增加30..1200s受限超时参数，长任务使用600s，超时保留partialReply并按任务ID独立保存结果，防止原结果被覆盖；工具权限未扩大。

MiMo全量正文约2.15万字符已交付并存入`dispatch/reports/20261023_mimo_tablet_full_audit.md`，是基于提供材料的文本审计而非运行验收。反重力约3.3万字符正文已返回，但最后的验证脚本在finally内截断，五项交付未收齐，不能标为完整模块或已验收；源码先留`dispatch/reports/20261023_agy_tablet_blur_module.md`，未将不完整脚本放入Assets。

补交回执`dispatch/reports/20261023_agy_tablet_blur_completion.md`约9500字符，验证脚本末尾仍未收齐，文档未到；状态保持待补全/审查，未启用真实模糊，也没有未结束的代理任务被当作后台持续开发。

## 用户最新角色调整（覆盖下方历史分工）

用户要求Codex只分配和协调任务，不再亲自编写游戏实现。Codex已停止外观模块本地编码；五份未验收变更、测试要求及剩余缺口交由Antigravity负责，见[实现接管单](dispatch/orders/CODEX-AGY-TABLET-IMPLEMENTATION-TAKEOVER-20261023.md)。MiMo的完整设置审计已返回，原文`dispatch/reports/20261023_mimo_tablet_full_audit.md`。Codex负责文件归属、派单台账和验收证据核对，不把代码正文视为已应用或已通过。

现有ACP客户端只交付文本，尚未开放对方工作区写入和终端能力；该执行接管须单独接通核验。当前代码未编译/运行验收，保持待验收状态。此前“Codex集成”的描述是角色调整前历史，后续工程由AGY接管。

## 第二批实际投递

用户明确要求“投递”后，已经原生ACP发送 `CODEX-AGY-TABLET-PRESENTATION-20261023`（反重力3.8 Flash / Medium）和 `CODEX-MIMO-TABLET-SETTINGS-MATRIX-20261023`（MiMo2.6 Flash），客户端分别输出`ACP_TASK_SENT`。配置在各自新会话中设置核验。

反重力负责外观偏好独立JSON/设置视图代码（减少动效、高对比度、强调色、壁纸压暗、局部恢复默认和失败保护）、具体消费端接入建议，以及真实背景捕获与高斯模糊技术方案。MiMo收到带行号的现有设置源码节选，负责区分真实效果/仅保存/无回调/占位，整理统一行为与验收矩阵。代码返回后仍需Codex审核集成，不能因为代码文本已返回就宣称偏好已在桌面生效；模糊方案交付也不等于模糊实现。两方都不得修改正在另一聊天推进的音乐模块及经济/驾驶模拟核。当前交付状态以[派单台账](dispatch/QUEUE.md)为准。

用户要求将平板作为跨应用的一体化系统推进。Codex负责总体接口、集成和Unity验收；Antigravity负责主要实现；MiMo负责交互审计、信息提炼及验收建议。经原生ACP分别投递 `CODEX-AGY-TABLET-SHELL-20261023` 和 `CODEX-MIMO-TABLET-AUDIT-20261023`。本轮客户端提供任务内源码事实与接口，不开放文件写入/终端：反重力返回代码，由Codex审查落盘，不宣称对端直接修改了工作区。

## 首批范围

通知/控制面板跨应用存在，面板互斥，外点关闭，Escape优先关闭系统面板；锁屏隐藏并关闭面板。通知使用实际投递的标题/正文/时间，容量32条，可逐条删除/清空；没有演示通知填充。控制首批只包含本机勿扰通知和现有设置入口，不声称连接真实Wi-Fi/蓝牙、同步账户或改变设备亮度。音乐模块及其独立存储不属本轮修改范围。

`TabletShellState` 与 `TabletShellView`已由反重力返回完整代码，原文保存在 `dispatch/reports/20261023_agy_tablet_shell_reply.txt`，实际会话 `0cd2a13e-e892-4292-a95e-eb80015934aa`，`gemini-3.8-flash-medium`与`thinking_level=medium`经配置返回核验。Codex落盘后修正遮罩与入口层级、44px目标、只读通知集合、长标题和尺寸边界；`GameMainUI`保留48px系统栏区域，Escape先关面板，锁屏清除遮罩。原有应用提示接入通知列表，勿扰时收存而不弹出提示；勿扰只保存在本次进程内，不影响声音。旧固定信号格删除，铁路内网名称只是世界观标记，不代表已联网。

MiMo回执 `dispatch/reports/20261023_mimo_tablet_audit.md`，会话 `ses_ffe5e97cecaf3ffeGmyaUE2tok`，实际 `xiaomi-mimo-token-plan/mimo-v2.6-flash`。采纳面板互斥/外点关闭、Escape优先、锁屏清理、避免遮挡和勿扰真实行为五项检查。其意见基于任务附带事实，未独立全面读取源码或联网研究。

编译/运行检查由 `TabletSystemShellReview.Batch`执行：独立空场景、独立音乐存储、真实GameMainUI入口、UI事件与1440/1024布局截图。最终 `tools/compile/tablet_shell_v3_stdout.log`退出0，`TABLET_SHELL_STATE_OK`与`TABLET_SHELL_RUNTIME_OK`通过；包括面板切换的实际UI拾取、应用提示收存/勿扰、Escape分层、外点关闭、清空、设置入口、锁屏/再解锁、后台文字输入隔离与两种尺寸。截图 `tools/compile/tablet_shell_review_v3/`。使用合成UI事件，不代表Android真机触摸验收；通知跨进程持久化、实际业务生产者与沉浸模式仍未实现。

首轮检查在解锁420ms动画完成前判断，曾报“Unlock did not show shell”；改为等待真实时间后通过。图像复查发现应用提示条后加入根节点会盖住系统面板，现将其放于系统层下；打开面板保存/转移焦点，关闭时尝试恢复原焦点。为防后台输入，开放系统面板时根节点捕获并拦截来自后台的键盘事件。

由于系统栏改变所有应用的可用高度，另外执行现有 `RailwayMusicTerminalReview.Batch`，`tablet_shell_music_regression_stdout.log`退出0、`MUSIC_REVIEW_OK`通过：实际音乐入口、播放/恢复、歌词/输入框快捷键及1024布局未出现回归。未修改音乐源码。图像人工复查为系统层检查图，非真机实听验收。

## 后续分批

1. 系统导航与通知：首批基础模块；后续接实际订单/经营等生产者、分类与持久化策略，明确勿扰只控制通知呈现。
2. 设置与个性化：审计每个现有设置项，区分真实效果与占位；统一桌面/应用主题、动效开关和触摸导航，避免保存开关却不产生效果。反重力编码，MiMo整理状态矩阵。
3. 背景模糊与全域视觉：先验证UI背景捕获与渲染管线，再实现可调降采样模糊、遮挡层级与性能回退；覆盖Windows/Android目标。半透明色块不是高斯模糊完成证据。

## 验收重点

系统入口不得挡住App关闭按钮；关闭面板不得退出App；输入框/重绑定不得被快捷键抢占；锁屏与重新打开不能残留遮罩；重复构建/销毁不重复订阅。窗口缩放和触摸操作需要单独验证。实现状态以派单回执、源码与Unity日志为准。

## 已查阅的一手设计资料

Codex查阅[Android系统栏](https://developer.android.google.cn/design/ui/mobile/guides/foundations/system-bars?hl=en)：系统入口应跨应用可达，并检查各屏幕尺寸下控件不被遮挡。本项目采用独立系统栏区域，是游戏内模拟布局，不是原生Android WindowInsets实现。[AOSP设置设计指南](https://source.android.com/docs/core/settings/settings-guidelines)用于后续设置层级与反馈审计；不以参考文档存在推断本项目功能已经实现。


## 2026-10-23 preview compile compatibility

TabletBlurPreview review helpers now send a public NavigationSubmitEvent to each Button instead of calling inaccessible Clickable.Invoke. This removes CS0122 in Unity 6000.5.8f1; cab_fan_v25b_stdout.log compiles and exits 0. This is compile validation only for the blur preview, not proof of pointer routing, Gaussian quality or tablet integration.


## Settings compatibility repair during cab review

Restored the existing ShowAffairsPage/ShowStoryPage entry points from HEAD after a concurrent settings edit removed methods still called by GameMainUI. OnRestoreApp now matches actual SystemAppsEasterEgg.RestoreResult.CapsuleTriggered/InvalidIndex, replacing nonexistent AllRestored/Failed; feedback identifies capsule activation or invalid index. This is a narrow compile repair, not tablet runtime acceptance.


## 2026-10-24 系统面板真实背景高斯模糊接入完成 (Antigravity)

- **生产接入**：`TabletGaussianBlur` 二阶可分离 9-tap 归一化高斯模糊服务接入正式系统面板 (`TabletShellView` + `GameMainUI`)，彻底替换原纯色遮罩层占位。
- **捕获管线**：支持双路径捕获——离屏目标纹理模式 (`targetTexture != null`，用于批处理自动化核验及离屏管线，GPU 直接 blit 零延迟) 与屏幕空间模式 (`ScreenCapture.CaptureScreenshotIntoRenderTexture` 于 `WaitForEndOfFrame` 执行)。
- **防递归与防拖影**：面板打开瞬间先行 `PrepareForCapture()` 保证系统浮层透明度为 0，捕获完成后注入 `_blurBackground` 并恢复透明度，彻底杜绝浮层自身递归模糊与拖影。
- **资源管理与刷新策略**：采用按需刷新策略，仅在面板开启和视口分辨率发生变化时按需捕获重算，严禁每帧分配 RenderTexture；`OnDisable` 与 `OnDestroy` 显式释放 PingPong 与捕获缓冲。
- **降级与无障碍保证**：高对比度模式禁用模糊并启用实色深底遮罩；Shader 不支持或硬件受限时安全回退至扁平半透明 scrim；尊重 `ReduceMotion` 动效偏好。
- **验证证据**：Unity 6.5 batch 编译 0 `error CS`；`TabletSystemShellReview.Batch` Play Mode 自动化核验通过，包含面板打开模糊显现、面板关闭模糊清理、Escape 分层、勿扰、设置切换与 1440/1024 自适应；`TabletGaussianBlurReview.Batch` 算法验证通过。截图输出位于 `tools/compile/tablet_presentation_review_v1/`。


## 2026-10-25 设置页「通知与状态栏」接入系统面板 (Antigravity)

- **生产接入**：设置页（`TabletSettingsAppUI`）增加独立分类 11「通知与状态栏」详情页，彻底替换原三行通用占位。
- **系统面板入口**：通过 `GameMainUI.OpenSystemPanel` / `SetOpenSystemPanelHandler` 真实拉起系统面板：
  - 「打开通知中心」按钮（`settings-open-notifications-btn`）请求 `TabletSystemPanel.Notifications`；
  - 「打开控制面板」按钮（`settings-open-controls-btn`）请求 `TabletSystemPanel.Controls`；
  - 面板打开保留原有背景捕获模糊、半透明遮罩与 Escape 优先级。
- **安全降级处理**：未注入面板回调且无 `GameMainUI` 宿主时，页面展示降级说明标签（`settings-notifications-fallback-note`），点击按钮提供准确的未就绪 Toast 提示，不抛出任何异常。
- **运行事实对齐**：页面明确阐释已验证机制——通知来自应用实际提示、最多收存 32 条；开启勿扰时保留通知收存、仅屏蔽悬浮横幅；勿扰当前仅在进程内有效；内网沙箱运行、状态栏不虚拟 Wi-Fi/蓝牙/电池。
- **最终验证**：修正后的 `TabletSettingsNotificationReview.Batch` 日志 `tools/compile/tablet_settings_notifications_review_codex_20261025.log` 中前三项通过，第 4 项如实标记 `NOT RUN`，总标记为 `TABLET_SETTINGS_NOTIFICATIONS_REVIEW_PARTIAL`，退出码 0。随后独立运行 `TabletSystemShellReview.Batch`，日志 `tools/compile/tablet_system_shell_review_complete_20261025.log` 输出 `TABLET_SHELL_STATE_OK`、`TABLET_SHELL_RUNTIME_OK`、`TABLET_PRESENTATION_RUNTIME_OK`，Unity 退出码 0；验证通知状态、面板互斥/切换、遮罩关闭、Escape、勿扰收存、锁屏、模糊背景及 1440/1024 布局。设置分类 11 与系统壳回归验收完成；Android 实体设备触控 QA 未执行。


## 2026-10-26 通知中心未读与本地状态闭环 (Antigravity)

- **未读状态闭环**：
  - `TabletShellState` 引入精确 `UnreadCount` 统计与 `TabletNotification.IsRead` 标志。新通知通过 `Post` 产生时置为未读，UnreadCount 增加；
  - `TabletShellView` 通知入口按钮（`tablet-notifications-open`）新增绝对定位未读角标（`tablet-notifications-badge`）。未读数为 0 时隐藏，大于 0 时显式红底白字小圆标，上限格式化为 `99+`，配置有可访问提示（`tooltip = $"{unread}条未读通知"`），严格内嵌于入口按钮且不遮挡状态栏；
  - 打开通知面板（`TogglePanel(TabletSystemPanel.Notifications)`）后调用 `MarkAllAsRead()` 即时标记全部已读，角标隐藏；逐条关闭（`Dismiss`）、全部清除（`ClearNotifications`）及 32 条容量上限自动淘汰均准确同步未读计数与 UI。
- **勿扰偏好跨启动保留**：
  - 勿扰开关状态持久化于独立配置文件，重启后自动恢复；
  - 开启勿扰时，应用提示仍进入通知列表收存，但抑制浮层横幅 Toast；关闭勿扰时恢复横幅；不触碰音乐、游戏声音或行车系统。
- **独立本地存储与安全边界**：
  - 新增 `TabletNotificationStorage`（schemaVersion 1），持久化文件位于 `Application.persistentDataPath/TabletSystem/notifications-v1.json`；
  - 采用临时文件写入加原子替换机制（`.tmp` + `File.Replace` / `File.Move`）；
  - 坏文件保护：遇到格式损坏、空内容或缺少版本时不覆写原文件并回退默认值；高版本（`schemaVersion > 1`）仅只读，禁止覆盖写入；写入失败记录脱敏诊断日志（绝不输出通知标题或正文），内存操作依然可用；
  - 绝对不写入经营存档、`PlayerPrefs.DeleteAll` 键域或云端。
- **系统工具接线与生命周期**：
  - `TabletSystemToolsAppUI.SetToastHandler` 与 `GameMainUI.ShowBackupToast` 真实接线，时钟（闹钟/授时/秒表）、日历与气象监视的实际提示完整接入系统通知流；
  - `GameMainUI.OnDestroy` 时解绑回调（`SetToastHandler(null)`），避免旧实例引用；无 handler 时安全降级为 `Debug.Log`，不抛异常。
- **审查与验证结果**：
  - 运行命令：`& 'D:\Unity Hub\Unity 6.5\Editor\Unity.exe' -batchmode -projectPath 'D:\Unity Project\RailwayRenaissance' -executeMethod TabletSystemShellReview.Batch -logFile tools/compile/tablet_notification_state_review_20261026.log`
  - 日志输出确认：`TABLET_SHELL_STATE_OK`、`TABLET_PRESENTATION_STORAGE_OK`、`TABLET_NOTIFICATION_STORAGE_OK`、`TABLET_SHELL_RUNTIME_OK`、`TABLET_PRESENTATION_RUNTIME_OK`，Unity 进程退出码 0。
  - 截图输出更新位于 `tools/compile/tablet_presentation_review_v1/`。

