# 驾驶台与副司机区域补件（2026-10-22）

入口：`Assets/Editor/NF5KCabFittings.cs` 的 Apply / BatchBuild。持续生成由 CabInteriorBlockoutSetup 调用；独立入口更新四份整车审阅场景，不重建既有操纵台。保留单侧主驾驶台和另一侧辅助席，不复制驾驶控制台。

## 本轮组件

- 司机座旁停放制动选择器：独立立柱、黑色面框、红色旋柄。CabDrivingAction 在枚举末尾追加 ParkingBrake，保留既有序列化枚举值。旋柄读取同一 CabSimulationDriver.ParkingBrake 状态，测试面板变化也同步其姿态。
- 场景交互显示“施加／缓解停放制动”，两项均要求持续确认。执行入口再次检查车速不超过 0.1 km/h、牵引归零及模拟后端存在；不增加空气制动或停车力公式。这是停稳操作选择器，不是运行中的紧急制动控制件。
- 副司机辅助区：托架式工作搁板、无线电外壳／扬声器格栅／旋钮、带托座话筒与导线、夹板及分隔资料袋。未加第二套牵引或制动操纵台。

无线电、广播、话筒、资料夹仅为外观件；没有通信、声音或文档交互功能。搁板与资料袋的支撑关系是候选几何，未做人因触达、结构承载或全场景碰撞认证。警惕踏板仍是既有预留，未接入虚构的警惕保护后端。岁月投影、小黄鼠和客舱安全设备另批实现。

## 验证

独立 Unity BatchBuild 输出 `tools/compile/cab_service_fittings_final.log`，截图位于 `tools/cab_reference/interior_review/assistant_station_fittings_final.png` 和 `parking_selector_fittings_final.png`。NF5KSimulationPlayReview 对有选择器的场景检查同一后端施加／缓解状态，并保留启动、牵引、制动、随轨运动及上下文输入回归。旧的独立驾驶场景可不含该新增选择器。

后续顺序：警惕／信号报警系统的功能设计与实体件 → 现有鸣笛撒砂雨刷控制接线 → 驾驶室后壁设备 → 客舱门区、扶手和紧急标识。驾驶日志／回顾／生涯与辅助角色设计继续保留，不由外观补件冒充功能完成。
