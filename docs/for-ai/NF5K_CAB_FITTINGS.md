# 驾驶台与副司机区域补件（2026-10-22）

后续更新：消防设备已由静态模型升级为查看铭牌、取下检查及原挂架归位；以下“静态”描述保留为前轮历史。当前接口、限制及验证见 [NF5K_SAFETY_INTERACTION.md](NF5K_SAFETY_INTERACTION.md)。

入口：`Assets/Editor/NF5KCabFittings.cs` 的 Apply / BatchBuild。持续生成由 CabInteriorBlockoutSetup 调用；独立入口更新四份整车审阅场景，不重建既有操纵台。保留单侧主驾驶台和另一侧辅助席，不复制驾驶控制台。

## 本轮组件

- 司机座旁停放制动选择器：独立立柱、黑色面框、红色旋柄。CabDrivingAction 在枚举末尾追加 ParkingBrake，保留既有序列化枚举值。旋柄读取同一 CabSimulationDriver.ParkingBrake 状态，测试面板变化也同步其姿态。
- 场景交互显示“施加／缓解停放制动”，两项均要求持续确认。执行入口再次检查车速不超过 0.1 km/h、牵引归零及模拟后端存在；不增加空气制动或停车力公式。这是停稳操作选择器，不是运行中的紧急制动控制件。
- 副司机辅助区：托架式工作搁板、无线电外壳／扬声器格栅／旋钮、带托座话筒与导线、夹板及分隔资料袋。未加第二套牵引或制动操纵台。

无线电、广播、话筒、资料夹仅为外观件；没有通信、声音或文档交互功能。搁板与资料袋的支撑关系是候选几何，未做人因触达、结构承载或全场景碰撞认证。警惕踏板仍是既有预留，未接入虚构的警惕保护后端。岁月投影、小黄鼠和客舱安全设备另批实现。

## 验证

独立 Unity BatchBuild 输出 `tools/compile/cab_service_fittings_final.log`，截图位于 `tools/cab_reference/interior_review/assistant_station_fittings_final.png` 和 `parking_selector_fittings_final.png`。NF5KSimulationPlayReview 对有选择器的场景检查同一后端施加／缓解状态，并保留启动、牵引、制动、随轨运动及上下文输入回归。旧的独立驾驶场景可不含该新增选择器。

后续顺序：警惕／信号报警系统的功能设计与实体件 → 现有鸣笛撒砂雨刷控制接线 → 驾驶室后壁设备 → 客舱门区、扶手和紧急标识。驾驶日志／回顾／生涯与辅助角色设计继续保留，不由外观补件冒充功能完成。

## 精修与下一批（2026-10-22）

新增专用低光泽灰绿烤漆、暖纸色及红色安全设备漆，避免修改共用车底材料。选择器手柄从140 mm收至105 mm，增底部锚固法兰、检修盖、轴套和 PARK / SET / REL 铭字；夹板改170×230 mm，纸张150×210 mm。通信旋钮改圆柱体并加索引，搁板补安装板、紧固件及挡边，所有方形件复用持久倒角网格。尺寸为游戏候选，非实车测量。

功能检查改为通过真实网格射线调用既有 CabStartupPointer，检查选择器施加和缓解。运行中还检查有牵引及牵引归零两种情况下均拒绝停放制动命令；不直接修改模拟核或绕过浮动交互的长按要求。原有0.1 km/h门槛和同一后端状态保持。

下一批已开始落地：客室前隔板附近和后端门区各一具带支架、压柄、保险销和软管的灭火器，前贯通门两侧扶手，以及后门区安全锤托板。这些是静态模型，不支持取用、灭火或破窗；也不代表已通过疏散/消防认证。现阶段不添加无后端支撑的警惕/信号功能。

新增静态校核选定的选择器立柱、搁板和灭火器包络与座椅碰撞体无交叠，尚非全场景碰撞/触达验证。精修日志 `tools/compile/cab_fittings_delivery_verified.log`；检查图为 `assistant_station_refined_verified.png`、`parking_selector_refined_verified.png`、`passenger_safety_fittings_verified.png`，位于既有 interior_review 目录。截图补光仅用于检查，不保存到场景。

倒角工具 NF5KIndustrialFinish.FinishRunningGear 跳过没有 MeshFilter 的文字 Renderer，避免新增铭牌导致生成中断。

世界空间铭字使用 CabWorldLabel.shader 的 LEqual 深度测试，避免透过隔板显示。CabWorldLabel 通过 MaterialPropertyBlock 绑定动态字体图集，并在 Font.textureRebuilt 后刷新，不将动态图集保存为项目贴图。
