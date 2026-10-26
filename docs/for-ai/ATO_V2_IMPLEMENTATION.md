# ATO第二版：实施与验证记录

2026-10-25，按用户确认方案分逻辑单元实施；保留原控制器、原物理内核及所有旧结果。

## 第一单元：完整检查点

`CabAtoCheckpoint`以V1兼容侧车读取冻结控制器的明确私有字段，通过原精确标量Codec保存参数、有效减速度、制动/采样阶段、切换时刻和最后观察时刻。侧车使用白名单反射，不改变原控制算法；字段名是V1兼容契约，.NET与Unity Mono需验证，Android/IL2CPP尚不作保证。

研究适配器增加暂停、CaptureResearchCheckpoint和RestoreResearchCheckpoint。封装包含物理checkpoint、精确控制器状态、适配器调度和任务参数。要求暂停操作；先验证版本、数值与控制器/物理Tick关系，再调用物理恢复并赋研究状态。原纯物理checkpoint恢复会退出自动状态，旧接口仍可使用。

`CabSimulationDriver`增加显式暂停及CheckpointRestoring通知；纯物理恢复前先验证Config/Input/State和余量。暂停时Advance不消耗物理或控制时间。无暂停/恢复操作时原运行路径保留。

独立入口：`dotnet run --project research/ato-v2/AtoV2.csproj`。执行四个捕获时点的精确回放、控制器状态一致及非法版本拒绝检查。Unity入口仍为NF5KAtoResearchReview.Batch，增加暂停不前进、损坏封装不改物理状态及恢复后连续轨迹一致检查。实际完成状态以对应日志与后续段落为准，不能将设计条目视作通过。

AI辅助：本轮代码、检查和文档由Codex实现及执行，学生独立复核另行记录。三模式与路线任务尚在本轮后续逻辑单元中实施。

第一单元实际验证：独立.NET精确回放通过；Unity日志 `tools/compile/ato_checkpoint_play.log` 包含 `ATO_PLAY_ACCEPTED`，暂停、损坏封装拒绝和恢复续跑断言已执行，名义停车误差约-0.119m。原始V1控制器与物理内核字节哈希未变。保存检查点、恢复和继续仍需明确的暂停操作；尚未接入全局游戏槽位。

## 第二单元：任务与控制权
独立AtoRouteTask保存900m分段限速、120秒到达目标及±5秒容差，具备校验和与版本。实体驾驶操作在写档位前发布真实操作意图，因此相同档位仍可接管；调试面板仅在实际操作时通知，不在刷新时通知。任务尚待第三单元接入新控制器。
实际验证：独立任务往返编码、边界和提前降速检查通过；Unity ato_route_ownership_play.log出现ATO_PLAY_ACCEPTED，包含相同档位实体操作退出自动驾驶检查。

## 第三单元：三目标控制及实验（2026-10-26）

新增 `CabAtoControllerV2`，沿用原物理内核与原驾驶接口。共用候选、预测限速、停车窗、驻留和退出规则；三目标只改变目标函数/速度参考代价，不改变车辆参数。当前实现与公式、协议及失败数据见 `docs/research/ato_v2_results.md`。冻结V1+明确路线适配、V2固定、V2自适应按相同工况对照，全部原始结果保留。

### 研究接口

| API | 契约 |
|---|---|
| `AtoRouteTask.Standard(origin)` | 相对起点900m、12/18/10m/s分段、120s±5s；Encode/Decode带SHA256与格式版本 |
| `CabAtoControllerV2.Decide(observation)` | 在非递减物理时间上每5Tick调用；需严格递增观测Tick；返回驾驶命令与实际选择理由 |
| `RequestMode(mode)` | 仅改变PendingMode，下次Decide生效；保存估计、任务进度、采样及LastSwitch |
| `Snapshot` / `Restore(task,state,physicsTick)` | 精确标量克隆，校验版本、范围与时间关系；不读取隐藏物理参数 |
| `CabAtoResearchDriver.SelectMode(mode)` | 界面模式选择；由既有Tick调度送入V2 |
| `SetPaused(paused)` | 暂停物理与控制调度；人工操作意图仍可立即退出自动状态 |
| `CaptureResearchCheckpoint()` / `RestoreResearchCheckpoint(json)` | 必须暂停；研究封装v2先验证物理/任务/控制器/调度，再替换状态；接受旧研究v1 |
| `SaveResearchFile(path)` / `LoadResearchFile(path)` | 完整JSON落盘，临时文件及旧文件`.bak`；研究面板使用独立AtoResearch路径 |
| 原 `CabSimulationDriver.RestoreCheckpoint` | 旧纯物理v2恢复退出自动控制；不重新启用ATO |

物理适配器补齐旧恢复缺陷：此前任意RailGrip会在下一次Advance量化成0.55，GradePermille由double转float；现保存精确恢复覆盖值，直到用户实际改变对应调试设置。未改变内核或既有无恢复路径的物理规则。

### 已执行验证

- .NET：加速、采样、停车制动、停稳驻留四阶段的精确控制器/物理重放；续跑至最终停稳，逐周期命令与最终状态一致。30/60/144 FPS同Tick命令及状态一致，模式切换保留估计与驻留。越站、预测无可行/当前越速及非法V2状态均有明确退出/拒绝断言。
- Unity Mono：三模式实际驾驶接口完成名义900m任务；四阶段检查点暂停恢复续跑一致；研究文件写入/读取、损坏任务拒绝且物理不变、精确黏着/坡度恢复、旧物理检查点保持人工模式。既有冷车、真实牵引、同档位实体接管、人工紧急接管和面板点击守卫回归保留。
- 正式实验：protocol-v1哈希冻结后84次执行，42组重复指纹完全一致。控制器及内核未因测试结果再调参；所有保护退出、短时越速与提前停稳保留。完整报告明确区分名义工程通过与失配可靠性未通过。

验证期间还发现另一任务未跟踪脚本 `TabletSettingsPlaceholderReview.cs` 缺少System.Linq，补齐单行引用以解除Unity编译阻塞；该文件不纳入ATO逻辑提交。一个最终验收回合的行为断言通过，但固定旧结果JSON被外部占用导致IO1224，因此回执写入改成带UTC时间的新文件，保留该失败日志并重新运行；不得以单个成功标记代替整次进程退出码。

所有代码和分析由Codex辅助完成，学生人工复核、Android/IL2CPP、真实车型校准和更广泛未知工况尚未完成。研究场景轨道外推不是完整线路；省气目标不等同实测节气结论。

最终工程回归：`tools/compile/ato_v2_verified_play.log` 含两项通过标记且Unity进程退出码0；`tools/compile/ato_v2_exact_replay_verified.log` 为独立重放检查输出。该最终Unity回合包含文件落盘、损坏路线拒绝、精确环境恢复和三模式任务。Python分析脚本重新核对协议哈希与全部42组成对指纹，无改写原始结果。
