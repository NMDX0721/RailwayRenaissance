# 灭火器真实结构与标签重做（2026-10-22）

后续压力表、完整双语和旁置手册已更新，最终资源及验证入口见 [消防手册与表盘说明](NF5K_FIRE_MANUAL_AND_GAUGE.md)。旧括号分类标签已删除，分类含义仍通过中英文适用对象逐项定义。

厂商与正式资料更新：现采用原创白岳消防器材厂、守安系列BYF-23R。`safety_equipment_profile.json` 为信息页和瓶身标签的共同数据源；标签图更新为 `Extinguisher_Baiyue_Label.png`，旧ABC-2.27/R为早期配置名。厂商设定见 `参考资料/设定/白岳消防器材厂.md`。玩家页不含开发说明，本文仍保留真实来源与仿真边界。

本轮纠正旧胶囊体、圆杆压柄、悬空平板标签及占位横线。旧图与旧“完成细节”描述不再代表当前质量基线。

## 产品参照与选型

参照 [Amerex 官方 Portable Fire Extinguisher Catalog，第13页 B402 栏](https://amerex-fire.com/api/documents/download?a=en&d=import-amerex-portable-fire-extinguisher-catalog)及[官方零件册，第5页](https://amerex-fire.com/api/documents/download?a=en&d=import-amerex-fire-extinguisher-parts-book)。这是5 lb贮压式磷酸铵盐ABC干粉类型，不是二氧化碳或储气瓶式产品。目录容量5 lb约2.27 kg、标称工作压力195 psig约1.34 MPa、温度−65～120°F约−54～49℃；尺寸参考15.25英寸高、4.25英寸筒径，即约387×108 mm。

游戏配置代号为 ABC-2.27/R，不复制 Amerex 品牌、认证标志或实物检验记录。美制3-A:40-B:C作为道具选定的设计等级，瓶身明确写明“美制”；其中C指带电设备，不能混作国内C类气体火灾。未认定该选型已满足铁路整车消防配置要求。

## 几何与交互

新增 `NF5KExtinguisherModel`：128段旋转截面生成直筒、浅收底、底圈、收肩及缩颈；独立合金阀体、颈部环圈、扁平固定提把与压柄、铰点连接片、压力表、保险销环及黄色封签。32段曲线软管连接阀侧接头与独立喷嘴；喷嘴有存放夹及深色出口。约387 mm的整体高度和108 mm筒径匹配参照量级，阀头/挂架具体结构是原创近似，不是生产图复刻。

原双占位短条改为单独可开闭的车载弹性钢抱箍，配底托和背板支撑。取下时抱箍绕侧铰点打开105°，归位及禁用时恢复闭合；仍为即时姿态切换，没有手部动画/扣锁力学。持物时瓶体转向玩家显示标签。运行中的压力表仅为静态示意，并未连接实际瓶压或灭火剂余量。

## 完整瓶身信息

`tools/cab_reference/build_extinguisher_label.py` 用项目中文字体生成原创1536×2560印刷贴图；曲面标签宽约119 mm、高210 mm、半径54.25 mm，贴合直筒，未使用悬浮平板。内容包含：类型、型号、药剂、充装量、工作压力/温度、美制适用范围与等级、操作要点、压力/封签/软管检查项、编号及检修记录位置。

标签采用真实设备式措辞，代表道具选定的设计参数，不表示完成了对应灭火性能试验。资料来源和原创近似边界保留于本文；挂架编号分别为FX-01、FX-02。

操作内容依据 [Kidde 官方 PASS 说明](https://www.kidde.com/support/fire-extinguishers/fire-extinguisher-use)改写为中文，产品构件检查依据上述零件册。未填写未经核验的生产日期、序列号、试验压力或认证编号。瓶身文字可通过原图放大查看，浮动说明同步更新。

## 验证与资源

四份整车检查场景由Unity生成。日志 `tools/compile/extinguisher_service_complete.log`；保留驾驶/制动/上下文输入回归，新增抱箍取下打开及归位闭合断言，沿用三挂架射线、单件持有和禁用归位检查。未实现喷射、药剂消耗、火灾或真实消防设备检验。

检查图：`extinguisher_real_service_delivery.png`、`extinguisher_service_front_delivery.png`，位于 `tools/cab_reference/interior_review/`。完整标签图为 `Assets/Resources/Cab/Extinguisher_ABC_Service_Label.png`。
