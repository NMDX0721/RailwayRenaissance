# 驾驶室查询与软管更新成本

2026-11-02。针对 `CabContextInteraction`、`CabFlexibleHose`、`CabRailMotion` 的 CPU 工作量修复。操作计数验证不能代替目标设备 Profiler，未宣称已证明掉帧根因或 FPS 提升幅度。

## 变化

- 界面遮挡组件由每次 OverOtherUI 执行四次 Find 改为 OnEnable 缓存、最多每秒刷新四引用。低频刷新允许迟创建、销毁或场景替换的面板恢复引用；新面板最多延迟一秒参与遮挡。禁用面板不拦截。OnEnable 清空候选体，避免反复启用累积重复项。
- 准星查询仍每次一次，优先于附近提示。附近候选先按可见性与 1.15m 距离筛选，仅保留最近四碰撞体，再最多四次遮挡射线，首次找到可见交互目标即停止；每个 Update 最多五射线。若最近四体均遮挡，不再扫描更远目标，玩家直接瞄准仍可选中。仍遍历候选体进行距离/投影，不声称候选扫描完全消失。
- 软管缓存端点在网格空间的位置、世界基向量在网格空间的变换、半径及目标网格身份。位置/基向量变化低于 1e-5 的比较阈值且网格未变时跳过顶点上传、法线和包围盒重算。缓存与上次实际重建状态比较，缓慢累积变化仍会触发更新。拓扑仅首次写入，每次变形不再 Clear 网格；法线仍在实际变形后重算。共同平移可以跳过，世界重力/截面方向变化或缩放变化仍会更新。OnDisable 与 Configure 失效缓存。
- `CabRailMotion.ApplyBackend` 在仿真时间、位移未变且非 reset 时跳过姿态投影。Apply 后仅标记驾驶室物理查询变脏，不再直接 SyncTransforms。`CabPhysicsQueries.Prepare` 在 Context/StartupPointer 实际射线前合并同步；没有待处理变更时不调用。侧门、隔门、驾驶手柄及器材取放/手持姿态变化也标记。自动同步已经启用时不额外手动同步，不修改项目 autoSyncTransforms 设置。

驾驶室持续移动且持续查询时，仍可能每帧需要一次 SyncTransforms。此处降低无查询、静止及同批多射线重复同步的成本，不宣称消除了 PhysX 同步成本，也不承诺所有其他外部脚本的变换都能自动纳入标记；新增移动交互碰撞体应调用 MarkDirty。

Unity 提供 SyncTransforms 来把 Transform 变更同步到物理空间；不能概括为“官方严禁每帧手动调用”。官方 API：[SyncTransforms](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics.SyncTransforms.html)、[autoSyncTransforms](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics-autoSyncTransforms.html)。本项目采用查询前按需同步以保留运动后射线正确性。

## 验证入口

`NF5KCabPerformanceReview.Batch` 打开 CabRailMotionReview 并进入 Play，不保存场景。检查真实软管 120 次静止调用不重建、移动端点更新及小于 1mm 端点误差；120 次干净 Prepare 不同步、变脏合并及移动碰撞体实际射线；构造 40 个可见近邻碰撞体，要求一次 Update 恰好五次射线。日志 `tools/compile/cab_performance_v1.log`。

另外运行 `NF5KContextInteractionReview.BatchPR6`，保留既有操作、UI 遮挡、按键绑定、启动/制动与运动后射线检查，日志 `tools/compile/cab_performance_regression.log`。这是功能/工作量回归，不是真实用户设备性能基准。

## 本轮结果

- `cab_performance_v1.log`：四软管与同步检查通过，射线压力测试期望五次却得到两次，失败退出。首次测试用无交互组件的近邻碰撞体，暴露了候选 Target 为 null 时与遮挡物 null 相等的问题；已补非空判断，并改用 40 个真实可解析交互目标及前方遮挡体制造最坏查询情况。保留失败，不记为通过。
- `cab_performance_v2.log`：Unity 正常退出 0，`CAB_PERF_HOSE_OK`、`CAB_PERF_SYNC_OK`、`CAB_PERF_RAY_BUDGET_OK` 均通过。四根实际软管各 120 次静止检查零重建，移动端点触发更新且误差小于 1mm；120 次干净 Prepare 零同步，变脏查询合并一次且实际射线命中移动后的碰撞体；40 个可见近邻受遮挡时恰好五条射线。
- `cab_performance_regression.log`：补齐器材同步标记后 Unity 正常退出 0；`NF5K_CONTEXT_INTERACTION_OK`、`PR6_LIGHTING_POINTER_OK`、`PR6_THROTTLE_INTERLOCK_OK`、`PR6_STOPPED_REVERSER_OK`、`NF5K_SAFETY_EQUIPMENT_PLAY_OK`、`NF5K_SIMULATION_PLAYMODE_OK` 均通过。未检出 C# 编译错误或测试异常，既有废弃 API/Burst/包管理器警告仍需与本轮功能通过区分。

此批次没有保存场景或改变驾驶参数。未完成 Android 设备或 Profiler CPU 帧时前后对照；不能据这些标记认定“主线程拉满”已测实或给出具体 FPS 收益。
