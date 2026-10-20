# 柴油机转速表：首版

## 最终美术稿（2026-10-20）

用户要求质感、科学性优化后定稿。最终资产`Assets/Models/Cab/diesel_tachometer_final.glb`；入口`tools/cab_reference/finalize_diesel_tachometer.py`，源/预览/回执为`tools/cab_reference/tachometer_review/diesel_tachometer_final.blend`、`diesel_tachometer_final_reference.png`、`diesel_tachometer_final_audit.json`。新增贴图`Assets/Models/Cab/Textures/DieselTachometerFinal/`。

保留窄长弧形外观、0–1500 r/min、50细分/250数字及650展示值。新增独立发黑钢压框材质，保持外壳为非金属旧漆涂层；钢螺钉及电接头为缎面金属，黑色指示端与盘面印刷加深对比，数字及单位放大10%，纸白盘法线减弱，玻璃粗糙度0.035、IOR1.5。预览继续浅色背景和柔光，增加均匀环境照明，不加遮挡刻度的强光斑。

31条刻线的实际基线与转速映射逐一比对，361个全量程位置检查指示端顶点不越窗口。误差及最小侧隙记录在回执；不是完整三维碰撞、磨耗或计量试验。背面三触点保留1毫米凹入深度、触点之间约2.96毫米边缘距离，金属壳口补定位缺口和小倒角；这些是模型几何参数，不代表额定绝缘距离、连接器标准或已验证插配。

本稿定义为游戏美术与读数规格定稿。量程不是柴油机安全工作上限，不印未经核实的红线、精度或厂家标识；原车型号、内部表芯及接口电气制式仍未确认。若未来采用脉冲转速传感器，转换应使用`rpm = 60 × f_Hz / PPR`，PPR须由实际传感器确定；若采用测速发电机则需对应的标定/变换关系，不能混用。当前三触点不分配虚构针脚。

尚未接Unity与EngineRpm；现有仿真转速范围不匹配的问题仍保留为独立待办，本轮不修改动力学，也不把超量程夹取伪装成物理修复。

用户要求压力表完成Unity接入后继续柴油机转速表。入口`tools/cab_reference/build_diesel_tachometer.py`，输出`Assets/Models/Cab/diesel_tachometer_study.glb`；源、图、回执为`tools/cab_reference/tachometer_review/diesel_tachometer_study.blend`、`diesel_tachometer_reference.png`、`diesel_tachometer_audit.json`。

沿用原图窄长仪表组的黑漆框、向左弯刻度窗及内嵌玻璃，与已完成电流表形成同一外形系列；顶部加“柴油机转速”，盘面单位r/min，黑色指示端区别于电流表。0–1500 r/min为设计选型，每小格50、每250标数，展示650 r/min。29网格、8350三角面，指示节点独立为`Tachometer_Reading_Carriage`。使用浅灰背景、柔光，不添加遮住盘面的强光斑。

参考资料：

- [国家铁路局发布的DF4C段修规程](https://www.nra.gov.cn/jglz/fgzd/gfwj/201804/P020220801568207502170.pdf)搜索索引可核对最低430、最高1000 r/min及升降速要求。是同系列配置参考，不能直接认定早期DF4-0001参数，也不能把1000 r/min当成所有早期车型的红线。
- [舟山清风YDC-1电测仪表页](https://qingfeng5585.com/en/dclybxl/258.html)说明机车操纵台电测仪表可显示柴油机转速等参数。页面标题与正文单双针描述不一致，故不据此认定本资产型号、量程或内部表芯。

内部显示运动沿用已明确标记为自主设计的弧导轨，不声称复原YDC-1动圈机构。背面增加一只三触点凹式电连接外形，针脚定义、输入额定值、传感器类型及脉冲数均未确定，不冒充原车插头标准。接线原理后续需明确是转速传感器/测速发电机及匹配变换器，转速不是车速，不能从km/h直接当作r/min。

尚未绑定Unity；后续接`CabEngineState.EngineRpm`。现有仿真600怠速、档位最高可达1760 r/min，超出本盘面1500，也不吻合同系列430–1000参考范围。接线时应保留超量程标记，并单独校准机车动力参数，不能只修改显示数值掩盖差异。本轮没有改动原仿真。
