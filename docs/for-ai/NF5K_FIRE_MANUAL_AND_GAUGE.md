# 白岳消防器材：双语、压力表与随车手册（2026-10-22）

## 玩家文案与产品资料

玩家信息页采用正式可滚动的随车设备资料，支持关闭及Esc退出，打开时隔离后台实体操作。信息页、瓶身印刷和厂商资料共同读取 `Assets/Resources/Cab/safety_equipment_profile.json`。厂商为原创白岳消防器材厂，守安系列BYF-23R；不使用现实制造商商标及检验证明。

按用户要求去掉瓶身及信息页的“美制分类”括号。保留A/B/C各自的中英文具体定义，避免将其中C误读为气体火灾；参考来源采用的分类约定仍在开发文档中说明。型号、药剂、充装量、压力、温度、适用对象、操作、维护与厂商内容均中英配对。标签图1536×2700，并检查每行宽度及总高度，避免中英文溢出。

## 压力表及上部材质

旧彩色短刻度改为连续低压红区、允许范围绿区和高压红区，分区含中英标记。独立锥形指针、金属轴帽、卷边钢表圈、微凸透明表盖及印刷表盘分件；阀体采用较哑的合金质感，表圈与保险销使用较亮的钢材，压柄仍保留涂层。固定螺栓改浅头并贴合背板，移除原漂浮球状观感。

压力表为外观示意，未连接真实灭火剂瓶压或温度模型；绿色分区角度为原创视觉近似，不是厂家标定曲线。依据 [Amerex压力表技术说明](https://amerex-fire.com/learning-center/flashpoint/fire-extinguisher-pressure-gauges-temperature)，绿色范围与器材额定温度范围内的充压状态有关，不可由静态绿区图示推断药剂量或整机合格。

## 旁置手册

名称：《随车消防器材使用与维护手册》 / ONBOARD FIRE EQUIPMENT — OPERATION & MAINTENANCE。固定于前隔板的独立书袋，具备背板、支撑、前挡唇、纸页块、书脊与双语封面。它说明使用和维护要求，不是填写记录的《随车检修册》。

`CabSafetyEquipment.IsManual` 表示只读手册，浮动菜单仅提供阅读；不走灭火器/安全锤的取放命令。保留原来的三件可取设备，新增一个手册阅读目标。手册内容调用同一产品资料，尚无逐页翻书、纸张物理或记录填写。

## 生成及验证

`NF5KExtinguisherModel.BuildManual` 由既有补件入口生成；`build_extinguisher_label.py` 输出双语标签、压力表印刷和封面。四份整车审阅场景由Unity保存。

`tools/compile/extinguisher_manual_verified.log`：新增手册仅阅读动作、打开/关闭资料检查，保留资料页后台输入隔离、三个挂架取放/抱箍开闭、驾驶交互及模拟回归。

审阅图目录：`tools/cab_reference/interior_review/`。最终文件为 `extinguisher_gauge_valve_delivery.png`、`baiyue_fire_manual_delivery.png`、`extinguisher_bilingual_front_delivery.png`、`extinguisher_bilingual_mounted_delivery.png`。
# 2026-10-23 在役细节与固定结构

收尾：表盘 V6 所有文字统一 48 px，同一字体；左右状态采用相同双行间距，底部压力和英制压力沿中轴均匀排列。锁销改至压柄根部与加高的双侧固定铰座共同位置，避免只穿固定座却不能阻挡压柄。装配检查补锁销/压柄与铰座包络相交、避让铰轴，以及指针轴帽与玻璃的间隙。这些是游戏几何检查，不是机械功能或器材认证。

压柄与固定铰座内侧各留 0.5 mm 侧隙，压杆端部减去原来的明显穿入量。终轮检查 `tools/compile/cab_uniform_wall_equipment_verified.log` 通过几何及取放/驾驶回归；新近景为 `extinguisher_gauge_valve_uniform_verified.png`。表针偏角仍是静态外观，不因温度说明而宣称已实现热力学模型。

科学依据：[Amerex 维护手册](https://www.amerex-fire.com/upl/downloads/manuals/english/hand-portable-high-performance-extinguishers-faee309b.pdf) 要求保险销安装到位、封签完好、软管接头紧固，并指出温度可能造成表针轻微变化；核对仅引用这些通用结构/检查关系，不采用该高性能系列的 240 psi 参数替换本项目 195 psig 的既有规格。药剂量不能仅靠表盘判定。

排版与保险销修订：表盘 V5 将“正常 / CHARGED”恢复到顶部中轴，留出标题区域，不再偏向左侧。拉环采用连续圆管网格并补与销杆相接的短颈；取消原来的大块黄色吊牌，改小型封签锁头及连接拉环和柄根销孔区域的闭合细环。黄色件表示防拆封签，不是拉环本体。具体走线为原创装配表现，非厂家零件的精确复刻。参考 [Amerex 零件手册](https://www.amerex-fire.com/upl/downloads/library/parts-book.pdf) 中独立列出的不锈钢环销与黄色封签。

表盘可读性二次修订：V4 印刷放大中文状态、英文状态及压力单位，正常状态移至指针左侧空白区，避免针尖遮字及边缘刻度挤占。握把 V6 改连续折弯网格及硬表面法线，消除独立方条接缝；阀体增加双侧铰座、贯穿轴销与阀芯压杆。该部分仍为静态机械外观，未接喷射驱动动画。对应审阅图后缀 `_legible_handle_final.png`。`tools/compile/extinguisher_legible_handle_final.log` 已通过安装接触、器材取放、上下文交互和驾驶 Play Mode 回归；四份场景由 Unity 保存。

两具灭火器压力指针分别采用 +5°、−7° 的固定外观偏移，限制在绿色区内；这不是实时温度或瓶压模拟。表头补背壳和指针轴，避免接管与表壳脱离；指针轴帽与镜片留出间隙。挂架补两侧连接腹板和固定铰链，瓶底与底杯接触。

手册封面采用可复现的克制边角磨损、书脊擦痕与折线；纸页加入独立层次，文字保持可读。书袋增加承重底板，书封底边落在底板上，后板、侧折边、前挡板及墙面连接件形成连续支撑。

这些为静态安装与脚本取放模型；尚未模拟螺栓强度、车辆振动松脱、纸张软体或自由落体。检查入口仍为 `NF5KCabFittings.BatchBuild`，Play Mode 额外检查书册底部接触、双表针差异与绿色区角度，并回归取放和卡箍开合。

验证：`tools/compile/extinguisher_service_wear_checked.log` 已通过 SERVICE_MOUNTING、SAFETY_EQUIPMENT_PLAY、CONTEXT_INTERACTION、SIMULATION_PLAYMODE 检查。四份场景已由 Unity 保存；新图后缀为 `_service_wear_checked.png`。倒角将尺寸写入网格，因此接触检查使用 Renderer 实际边界，不使用变换缩放推算。
