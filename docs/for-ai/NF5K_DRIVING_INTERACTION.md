# NF-5K 驾驶台交互首轮

2026-10-21。范围为独立 `CabSimulationReview.unity`，不修改原夜间和白天光照审阅场景。

## 接线与操作

原钥匙、主控、油泵、启动/停机按钮沿用 CabStartupControls 与 CabStartupClickTarget。新增 CabDrivingTarget，通过同一个 CabStartupPointer 的 Input System 鼠标射线处理牵引手轮、自动制动大闸、独立制动小闸和换向手柄。

鼠标左键增加一档／施加制动，右键减少一档／缓解；换向从后退经中立到前进。牵引0–8档，大闸沿用5位，小闸以25%步进。悬停显示部件名称与当前状态。F2收起/展开后端测试面板，避免面板遮住可点击部件。钥匙左键插入或转动主控，右键拔出；钥匙拔出沿用停机/主控关闭守卫。

停放制动暂由测试面板控制，尚无独立机械操作件。换向初始中立，停放制动初始施加；启动充风并缓解空气制动后，解除停放制动、置前进，再加牵引。没有压缩等待时间或跳过实际充风。

## 实现边界

- NF5KDrivingInteractionSetup 在模拟审阅场景生成时绑定4个可动节点：Handwheel_Pivot、Tall_Spindle_Pivot、Short_Spindle_Pivot、Reverser_Handle。
- 可动网格使用静态非凸 MeshCollider，保留部件所在交互层；手轮孔隙不使用覆盖整圈的大包围盒。没有新增 Rigidbody，不能将此碰撞用途等同于刚体机械求解。
- 牵引及闸把转轴由模型 Calibration_AxisZ 转为节点局部方向，避免假定导入前后轴向相同。手轮240°、大闸100°、小闸65°、换向25°为可视化候选行程，不是原车机械标定；完整手柄扫掠和人因验收仍待做。
- 所有操作调用现有 CabEngineState 接口，后端继续是唯一物理状态写入者。动画每帧读取已接受的指令位置；联锁拒绝不会形成虚假的新档位。动画显示手柄指令位置，不将气压建立延迟伪装成瞬时到位。
- 当前为点击步进，尚无连续拖拽、触屏手势、角色手部动画。可复用动作接口，但玩家/辅助来源归因和驾驶日志仍未实现。
- 场景车体仍不沿轨道行驶；行程任务、回顾及生涯待下一阶段。

## 验证

NF5KSimulationReview.BatchBuild 编译、重建并保存场景，Play Mode 验证4个可动对象的射线命中与命令分发，覆盖冷车拒绝牵引、启动充风、加档加速、移动中换向拒绝、小闸只建立本机缸压、大闸紧急切牵引停车，并复测15项内核检查及检查点恢复。

日志：`tools/compile/cab_driving_interaction_verified.log`；成功标记为 NF5K_DRIVING_INTERACTION_OK 与 NF5K_SIMULATION_PLAYMODE_OK。射线验证从网格附近探测可命中表面，不等价于驾驶坐姿下所有档位的可达性验收。
