# ATO V3 控制核心

研究原型在给定路线和观测初速下，使用前向加速、反向减速速度包络及巡航速度二分构造参考；平稳、省气、准点三模式从九个参数组合中选择参考。控制器每五个物理 Tick 基于观测和已接受命令更新估计，筛选有限候选及三种制动响应情景，再按目标评分。无可行候选时请求零牵引与常用全制动并退出。有限情景不构成形式化安全保证，参考轨迹不保证实际准点。

接口为 `CabAtoControllerV3.Decide`、`AtoV3State` 与 `AtoTrajectoryPlanner`；测试入口 `research/ato-v3/AtoV3.csproj`。V2 和物理内核保留，研究结果及限制见 [实验报告](../research/ato_v3_results.md)。
