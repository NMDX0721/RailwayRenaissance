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
