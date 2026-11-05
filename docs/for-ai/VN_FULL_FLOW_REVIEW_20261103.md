# 序章连续推进与全部选项复核

日期：2026-11-03。承接 [美术与旅途选择复核](VN_JOURNEY_FLOW_REVIEW_20261102.md)。

## 本轮修复

1. 跨场景选择之前只改变场景/条目坐标，没有加载目标背景和BGM。`VNManager.SelectOption`现在在场景变化时同步目标背景、转场方式、BGM请求与清理旧立绘；场景内选择不重复切换音乐。沿用BGM最短驻留规则，不强制截断人声曲。
2. 章节结束页在每次展示和进入第二阶段时重复注册闭包。`VisualElement.Clear`只清空子元素，不会移除该元素上的回调。现在创建结束页时只绑定一次`OnEpisodeClearClick`，目标存于`pendingEpisodeScript`；点击和倒计时共用`ContinueAfterEpisode`，消费目标后清空，并阻止点击冒泡。
3. `StartScript`原本无条件清空变量，导致07维修选择在08开头的条件时间线失效。`StartScriptInternal`区分手动重开与自然续章：前者重置变量，后者保留。08中`repair_choice_zhang`对应Day6下午，否则上午；不改原剧情选择含义。

## 验证方法与边界

`VNPrologueFlowReview.Batch`强制在Library下的`VNCompiledReviewProject`运行，使用独立PlayerPrefs公司/产品名；通过实际`SetupCanvas`创建VN UI，2560×1440渲染目标与生产PanelSettings。测试管理器禁用自身Update，保留生产展示、条件判断、推进、按钮、章节动画与存档方法。普通对白加速，不宣称人工逐句通读/听音。

`prepare_flow_cases.py`用静态最短路径为每个可达选项生成前置选择序列。运行测试从各章开头推进到目标，经真实UI Toolkit按钮提交，并在选择前用专用槽60存读档、检查变量还原。另从01连续推进到10，章节结束页交替在第一/第二阶段点击，检查每次点击只创建一个话标题卡，并检查维修选择跨章保留。

经营交接目标由Editor API生成最小`StationSlice_V1`测试场景，挂载生产`UIManager`，用于检查实际异步加载与`VNExitData`消费，不代表主项目原始经营场景、完整经营UI或独立Player验收。账号采用测试服务创建的已注册状态，覆盖重玩时平板向导自动继续；不包含首次注册向导的端到端验收。开场Logo在此加速检查中跳过。

所有原始尝试日志保留。当前测试结果以最终证据及下方完成记录为准，未产生成功标记的启动尝试不能计为通过。

## 完成记录

证据冻结目录：`tools/vn/full_flow_acceptance_20261103/`。

- 当前187个运行时源码与2个Editor验收源码编译通过；冻结工具复核全部189个源文件哈希、十章JSON和隔离项目编译程序集一致。
- 25个选项全部经实际按钮提交；选择前保存/加载与目标背景检查通过。11个选择节点有2560×1440截图，已查看维修三选项、筹资顺序和条件选项截图，文字完整。
- 一条连续路线从01推进至10，九次章节点击均只创建一张标题卡；07维修选择跨章保留，08按相应条件推进。
- 结尾实际调用异步场景加载，最小经营接收场景内生产`UIManager`消费过渡数据；结果Day1、5名员工。此处没有验证主项目原始经营场景。
- 日志标记：`VN_ALL_25_OPTIONS_RUNTIME_OK`、`VN_TEN_CHAPTERS_RUNTIME_OK`、`VN_GAMEPLAY_RECEIVER_OK`。

**总进程结果仍为失败（exit 1），不能写成无错误或总验收通过。** 最终原始`results.json`保留`success:false`，唯一记录异常为Editor启动索引的`UnityEditor.Search.SearchDatabase.GetDefaultSearchDatabase`/`SearchInit.IndexationOnStartup`，堆栈无VN调用。复跑同样完成全部剧情检查，但该启动诊断发生在测试管理器创建之后，未被运行器的“创建前”诊断分流条件捕获。没有改写结果来制造通过。

`assessment.json`另行说明`runtime_checks_passed:true`及`overall_runner_success:false`。冻结工具逐条验证异常原文与完整Search启动堆栈，并拒绝包含VN调用的同类异常；其他错误或缺少任何运行成功标记都会阻止收集。原始日志为`full_flow_review_20261103_verified.log`，其文件名不意味着总进程成功。attempt1许可证IPC受限、attempt2空包项目UPM解析失败、attempt3运行检查完成但Search启动异常计入失败，均保留原日志。

复现：先运行`compile_review_sources.py --output tools/vn/source_compile_flow_20261103 --editor-source Assets/Editor/VNAssetCompletionReview.cs --editor-source Assets/Editor/VNPrologueFlowReview.cs`，再`prepare_compiled_review.py --compiled tools/vn/source_compile_flow_20261103`和`prepare_flow_cases.py`；为隔离项目补复制BGM/SFX、根JSON和Seeds资源。Unity使用`-batchmode -noUpm`、隔离项目路径及`-executeMethod VNPrologueFlowReview.Batch`，不加`-quit`。此机器沙箱内许可证IPC不可用，运行验证使用正常桌面权限。验收工具没有修改主项目场景YAML或用户存档。

## 仍需单独处理

- 主项目原始经营场景与Windows测试包实机验收；首次平板注册UI完整操作；音频试听。
- 日语配音仍未生成。
- 经营承接现有代码仍使用固定资金/信任/车况初值（`TransitionToGameplay`、`GameData.ApplyVNExitData`），与项目种子初值要求存在历史差距；本轮只修VN播放与选择链，没有据此宣称整体发布门槛通过。
