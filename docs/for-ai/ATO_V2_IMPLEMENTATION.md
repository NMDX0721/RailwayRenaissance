# ATO第二版：实施与验证记录

2026-10-25，按用户确认方案分逻辑单元实施；保留原控制器、原物理内核及所有旧结果。

## 第一单元：完整检查点

`CabAtoCheckpoint`以V1兼容侧车读取冻结控制器的明确私有字段，通过原精确标量Codec保存参数、有效减速度、制动/采样阶段、切换时刻和最后观察时刻。侧车使用白名单反射，不改变原控制算法；字段名是V1兼容契约，.NET与Unity Mono需验证，Android/IL2CPP尚不作保证。

研究适配器增加暂停、CaptureResearchCheckpoint和RestoreResearchCheckpoint。封装包含物理checkpoint、精确控制器状态、适配器调度和任务参数。要求暂停操作；先验证版本、数值与控制器/物理Tick关系，再调用物理恢复并赋研究状态。原纯物理checkpoint恢复会退出自动状态，旧接口仍可使用。

`CabSimulationDriver`增加显式暂停及CheckpointRestoring通知；纯物理恢复前先验证Config/Input/State和余量。暂停时Advance不消耗物理或控制时间。无暂停/恢复操作时原运行路径保留。

独立入口：`dotnet run --project research/ato-v2/AtoV2.csproj`。执行四个捕获时点的精确回放、控制器状态一致及非法版本拒绝检查。Unity入口仍为NF5KAtoResearchReview.Batch，增加暂停不前进、损坏封装不改物理状态及恢复后连续轨迹一致检查。实际完成状态以对应日志与后续段落为准，不能将设计条目视作通过。

AI辅助：本轮代码、检查和文档由Codex实现及执行，学生独立复核另行记录。三模式与路线任务尚在本轮后续逻辑单元中实施。

第一单元实际验证：独立.NET精确回放通过；Unity日志 `tools/compile/ato_checkpoint_play.log` 包含 `ATO_PLAY_ACCEPTED`，暂停、损坏封装拒绝和恢复续跑断言已执行，名义停车误差约-0.119m。原始V1控制器与物理内核字节哈希未变。保存检查点、恢复和继续仍需明确的暂停操作；尚未接入全局游戏槽位。
