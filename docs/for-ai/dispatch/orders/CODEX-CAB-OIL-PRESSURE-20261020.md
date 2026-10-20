# 机油压力表与操纵台材质协调

Unity验收完成：主台461渲染器，包围盒1.55×1.30×0.73 m；301油压标定点与FBX坐标检查、1.2 MPa超量程、NaN无效、实时读取与停机归零均通过。保存/重开场景截图通过，无C#编译错误且编辑器正常退出。日志 `tools/compile/cab_main_console_revision4.log`，结构化结果 `cab_main_console_acceptance.json` revision=4。

0–1 MPa电测油压表，与既有窄框油水温/转速/电流表保持框体、字体、纸白盘面、黑指示刃与玻璃层次一致。小格0.02 MPa、大格0.1 MPa，参考预览0.45 MPa；零位用于停机无压和起动建压观察，不能把零当成故障。1 MPa=10 bar。

选型参考VDO原厂0–10 bar油压产品 https://vdo-instruments.com/product/10-bar-150-psi-oil-pressure-gauge-use-with-vdo-sender-24v-250-spade-connection/ ，只作为量程及电测仪表的同类依据，不宣称原车型号、针脚或传感器电阻匹配已经确定。原厂传感器示例 https://veratron.com/products/pressure-sensor-10bar-insulated-return-m10x1 说明需匹配标定；本项目不直接套其10–184 Ω接口。

## 文件

- `tools/cab_reference/build_oil_pressure_gauge.py`：窄框独立模型、51刻度、361行程检查。
- `Assets/Models/Cab/oil_pressure_gauge_final.glb`。
- `tools/cab_reference/oil_pressure_review/`：源blend、预览、审计。
- `Assets/Scripts/Cab/CabOilPressureGauge.cs`：读取显式绑定的CabEngineState.OilPressureMpa，0–1 MPa表显，保留原始值、异常及超量程状态。未改发动机油压简化公式。
- 主体安装x=0.055 m，与其他窄框表同一15°仪表板；主驾驶台Unity构建入口负责添加组件与同一发动机状态绑定。

## 本轮前裙与材质

驾驶台漆钢、酚醛台面、黑色面板、金属紧固件各有独立可导出PBR纹理，确定性生成并内嵌GLB。漆层按非金属表面处理，裸钢紧固件保持金属度；不为了显得金属而把整片油漆设为高金属度。法线细节克制，Unity显式导出BaseColor/Normal/MetallicSmoothness。纹理目录 `Assets/Models/Cab/Textures/MainConsole/`。

前裙补带槽紧固件、踢脚板压筋与四角固定、通风框和固定件；保留连续壳体与膝部凹腔。未以大面积随机锈迹或划痕遮盖结构问题。

## 后端边界与验收

低油压风险与转速、油温黏度、启动延时、油泵和油量有关。停机无压不应一直鸣警，传感器失效另标无效；本轮不添加未经标定的统一红区/保护阈值。现有油压模型仍是转速目标值趋近，没有真实油温/泵故障耦合。

单体361行程检查窗口包络；主台Unity每301点验证显示与FBX世界位置映射、1.2 MPa原值保留、NaN无效、实时信号一致和停机归零，重开场景截图。新油压表加入主台预制体，不把它误认为后端润滑系统已经完成。
