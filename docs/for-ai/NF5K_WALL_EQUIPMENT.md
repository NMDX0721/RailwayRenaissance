# NF-5K 后壁设备与急救用品柜（2026-10-23）

## 印刷标签精修

柜门及包装标签改用原创印刷纹理，替换旧的微小 TextMesh 叠字。柜门有设备编号、识别十字、主标题、副标题及取用通道提示；内部三类包装有中文品名、英文品名、规格、包装数量、保存提示与补给编号，遵循统一网格和留白。三种色带仅区分品类；轻微磨损仅作用于外标签边缘，不表现封装破损。

内容源为 `Assets/Resources/Cab/first_aid_label_profile.json`；生成器 `tools/cab_reference/build_first_aid_prints.py` 输出四张原创标签，并校核文字横向边界。`NF5KEquipmentPrint` 创建带三线性/mipmap/各向异性过滤的哑光印刷平面。包装约束带移至右侧，避让正文。标签中的规格与单包装数量是设计常量，不作为存档物资初值或游戏库存。

终轮标签为柜门 V3 和包装 V2；约束带中心外移至包装中心右侧 44 mm、宽 7 mm，保护斜视角下的印刷正文。`tools/compile/cab_first_aid_prints_visors_delivery.log` 已通过柜门/锁舌开合与驾驶回归。原生 Unity 检查图：`first_aid_cabinet_closed_print_delivery.png`、`first_aid_cabinet_open_print_delivery.png`、`first_aid_pack_print_closeup.png`；均位于 `tools/cab_reference/interior_review/`。

参考 [HARTMANN 包扎产品目录](https://www.hartmann.info/en/products/first-aid/other-first-aid-products/standard-dressings/hartmann-standard-dressings) 的品名/规格层级，以及同厂 [产品分类](https://www.hartmann.info/en-gb/products) 对敷料和固定绷带的区分；不复制真实商标、医疗认证或具体产品疗效。当前标签的规格为原创道具候选，没有治疗和用药指引。

入口：`Assets/Editor/NF5KWallEquipment.cs`，由 `NF5KCabFittings.Apply` 调用。保留驾驶室既有后壁检修盖位置；客舱侧在前隔板左侧安装独立急救用品柜，避免贯通门及消防器材。

## 装配与视觉

- 后壁辅助电器面板：凹入密封垫、折边盖板、四角紧固件、带罩通风条和 EC-01 中英铭字。保留原来的底盖；新增件仅表示外壳，无虚构的断路器状态或电气功能。
- 急救用品柜：380 × 320 × 96 mm 候选包络，固定背板、上下及侧折边、隔板、墙面锚固件、双铰链、门封和闭锁件。白色十字符号配绿色识别底板，不使用红十字标志。
- 内部为封装用品包的外观，分上下层并用约束带固定；不表示医用品认证、实际库存或可用数量。没有医疗操作、取药、消耗或补货逻辑。
- 尺寸和布局均为原创候选，不冒充实车测绘或强度认证。柜底与隔板承担包体，约束带限制移动；没有自由落体、车辆振动载荷或布带形变求解。

## 交互

`CabStorageCabinet` 保存原始门与锁舌姿态，经既有 `CabContextInteraction` 提供“打开柜门 / 关闭柜门”。继承用户已选交互键，不添加快捷键。开门时锁舌转 90°解除与固定挡座的约束，门绕实体铰链转 100°；关门复位并回锁。当前为瞬时姿态切换，尚无时间连续动画。操作距离不超过 2.4 m；组件停用时精确归位。存储柜单独识别，不伪装成灭火器或消防手册，不改变原三件可取器材。

## 验证

`NF5KSafetyEquipmentReview` 新增柜门真实射线、浮动命令执行、开合角度、选定座椅包络净空、远程操作拒绝及停用归位检查。检查不等于全车人因或碰撞认证。`NF5KCabFittings.BatchBuild` 同步四份整车检查场景并生成柜门开/关及驾驶室侧面板近景。

灭火器收尾同步检查销杆与压柄/双侧铰座包络相交、销杆避让转轴，以及指针轴帽与玻璃的几何间隙；采用包络与尺寸检查，不是阀门运动、碰撞应力或压力容器认证。

终轮证据：`tools/compile/cab_uniform_wall_equipment_verified.log` 通过 STORAGE_CABINET、EXTINGUISHER_ASSEMBLY、SERVICE_MOUNTING、SAFETY_EQUIPMENT_PLAY、CONTEXT_INTERACTION 和 SIMULATION_PLAYMODE。柜体检查包含锁舌释放/回锁；四份场景已由 Unity 保存。检查图为 `first_aid_cabinet_closed_verified.png`、`first_aid_cabinet_open_verified.png`、`rear_electrical_cover_verified.png`，位于 `tools/cab_reference/interior_review/`；检查用补光不保存到场景。模拟核与快照编码文件哈希保持原值。
