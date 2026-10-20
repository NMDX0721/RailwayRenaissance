# 柴油机转速表：首版

用户要求压力表完成Unity接入后继续柴油机转速表。入口`tools/cab_reference/build_diesel_tachometer.py`，输出`Assets/Models/Cab/diesel_tachometer_study.glb`；源、图、回执为`tools/cab_reference/tachometer_review/diesel_tachometer_study.blend`、`diesel_tachometer_reference.png`、`diesel_tachometer_audit.json`。

沿用原图窄长仪表组的黑漆框、向左弯刻度窗及内嵌玻璃，与已完成电流表形成同一外形系列；顶部加“柴油机转速”，盘面单位r/min，黑色指示端区别于电流表。0–1500 r/min为设计选型，每小格50、每250标数，展示650 r/min。29网格、8350三角面，指示节点独立为`Tachometer_Reading_Carriage`。使用浅灰背景、柔光，不添加遮住盘面的强光斑。

参考资料：

- [国家铁路局发布的DF4C段修规程](https://www.nra.gov.cn/jglz/fgzd/gfwj/201804/P020220801568207502170.pdf)搜索索引可核对最低430、最高1000 r/min及升降速要求。是同系列配置参考，不能直接认定早期DF4-0001参数，也不能把1000 r/min当成所有早期车型的红线。
- [舟山清风YDC-1电测仪表页](https://qingfeng5585.com/en/dclybxl/258.html)说明机车操纵台电测仪表可显示柴油机转速等参数。页面标题与正文单双针描述不一致，故不据此认定本资产型号、量程或内部表芯。

内部显示运动沿用已明确标记为自主设计的弧导轨，不声称复原YDC-1动圈机构。背面增加一只三触点凹式电连接外形，针脚定义、输入额定值、传感器类型及脉冲数均未确定，不冒充原车插头标准。接线原理后续需明确是转速传感器/测速发电机及匹配变换器，转速不是车速，不能从km/h直接当作r/min。

尚未绑定Unity；后续接`CabEngineState.EngineRpm`。现有仿真600怠速、档位最高可达1760 r/min，超出本盘面1500，也不吻合同系列430–1000参考范围。接线时应保留超量程标记，并单独校准机车动力参数，不能只修改显示数值掩盖差异。本轮没有改动原仿真。
