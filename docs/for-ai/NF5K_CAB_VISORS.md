# 驾驶室双侧可调遮阳板（2026-10-23）

入口：`NF5KCabVisors.Build`，由 `NF5KCabFittings.Apply` 调用。移除原 `Driver_Sun_Visor_Reserved` 占位件，生成司机与副司机各一块遮阳板，不增加第二套操纵台。

## 结构和尺寸

- 挡板主体 800 × 180 × 12 mm，为游戏候选；轴心位于车体局部 y=1.97 m、z=-0.47 m。
- 采用哑光包覆面、深色包边、底部操作片、贯穿轴、两端摩擦端盖、双支撑臂及前窗上沿锚固板。锚固件与支撑臂相连；板顶轴套包住固定轴，避免悬空板片。
- 收起、半降、降下分别为局部 X 轴 -90°、-45°、0°；板片始终位于前窗内侧和车顶下方。
- 操纵档位以摩擦铰链保持姿态表现；没有摩擦力矩、振动滑落、热负荷或完整视野认证。不宣称其遮光性能经过真实太阳/气候验证。

## 操作

`CabSunVisor` 管理独立三档位置、2.4 m 距离限制和停用归位。`CabContextInteraction` 将真实板片碰撞体纳入准心/就近交互，提供“降低一档 / 抬高一档”，到达端点时显示原因。复用现有自定义主交互键，不新增快捷键。当前按档瞬时切换，没有手部动画和持续拖拽。

## 验证

`NF5KSafetyEquipmentReview` 通过板片真实射线、浮动动作执行、两侧独立状态、上下限、三种姿态的网格顶点保守包络、距离限制和停用归位检查。包络限定检查范围，不代替全车碰撞/人体触达或驾驶视野认证。

四份整车检查场景由 `NF5KCabFittings.BatchBuild` 保存。收起与降下截图以检查用补光生成，不改变场景持久车灯。

终轮证据：`tools/compile/cab_first_aid_prints_visors_delivery.log` 通过 CAB_VISORS、STORAGE_CABINET、CONTEXT_INTERACTION 和 SIMULATION_PLAYMODE；没有 C# 编译错误。检查图为 `cab_visors_stowed_delivery.png` 和 `cab_visors_lowered_delivery.png`。模拟核及快照编码哈希未变化。

同类件依据：[Adlake 铁路驾驶室遮阳板](https://www.adlake.com/curtains-and-visors/sun-visor) 确认安装式可调挡板在铁路驾驶室的用途；本项目尺寸、支撑构型及三档交互为原创候选，不冒充该产品的测绘尺寸、调节方式或认证指标。
