# 水温／机油温度表组首版

## 材质定稿与Unity接线（2026-10-20）

验收完成：两模块各301位置/方向检查通过，最大模型位置误差水温6.144799e-8 m、油温6.211998e-8 m；66渲染器，包围盒约0.11×0.18×0.05 m。日志无C#编译错误，场景重开后完成截图，编辑器正常退出；证据 `tools/compile/cab_temperature_acceptance.json`、`temperature_review/unity_temperature_review.png`。

最终资产 `Assets/Models/Cab/water_oil_temperature_gauges_final.glb`；入口 `tools/cab_reference/finalize_temperature_gauges.py`，源文件 `temperature_review/temperature_gauges_final.blend`。玻璃IOR1.52、粗糙度0.028、轻微冷绿，柔光及低亮宽反射；发黑钢压框、金属螺丝与刻度底板分材质，底板法线强度0.08。

原生导出 `tools/cab_reference/export_temperature_gauges_unity.py` 输出 `Assets/Models/Cab/UnityTemperatureGauges/` FBX与PBR。编辑器入口 `CabTemperatureAssetSetup.BatchBuild` 生成 `Assets/Resources/Cab/TemperatureGaugesFinal.prefab` 和 `Assets/Scenes/CabTemperatureReview.unity`，不直接编辑场景YAML。

`Assets/Scripts/Cab/CabTemperatureGauge.cs` 每只表独立校准，读取共享JSON配置；水温绑定显式CabEngineState.WaterTempCelsius，油温尚无状态信号，绑定后HasValidInput=false、停在下限位置，不表示真实50 ℃。无绑定的预制体也为无效停放，SetTemperature支持未来外部温度通道输入。InputCelsius保留低温/超温原值；UnderRange、OverRange、InRedZone与HasValidInput独立。

Unity验收每只301位置/方向、低温-20 ℃、超量程180 ℃、红区边界、NaN/正负无穷、实时水温刷新及油温缺失状态；保存后重开场景截图。截图82/65 ℃仅作外观展示，不写回场景为真实信号。当前没有后端热网络、延时报警、显示阻尼或主驾驶台整体装配；InRedZone只是读数状态标志。

## 最新非零刻度设计（优先于下方历史记录）

水温改为40–120 ℃、2 ℃小格/10 ℃大格，红区100–120 ℃；油温改为50–150 ℃、5 ℃小格/10 ℃大格，红区110–150 ℃。数字与指示位置按各自量程重建，不仅替换文字。两个模块各361位置通过，水温41/油温21刻度基线通过；预览仍为82/65 ℃，对应52.5%/15%行程。

共享配置 `Assets/Resources/Cab/temperature_instrument_profile.json` 已由造型脚本消费，后续运行时应读取同一资源。后台低温/超温保留实际值、仅截断表显，测量故障和掉电另标状态。完整资料与后端热网络、报警回差/延时、种子初始化、存档迁移及验收计划见 [热管理设计](../../CAB_THERMAL_SIMULATION_DESIGN.md)。热模拟仍未实现，现有CabEngineState旧水温公式本轮未改。

## 用户批准的改装报警红区

不再受原车盘面限制，新增独立氧化红窄带：水温100–150 ℃、油温110–150 ℃。起点是可调整的游戏改装设计参数，并非东风4原车安全限值；在脚本 `start` 及审计 `alarm_design` 中明示。色带沿同一标定圆弧分60段，位于刻度基线左侧，保留黑色刻度与数字，不用满面红色遮盖读数。两个模块分别设置 `red_zone_start_C` 元数据。

本次只实现盘面红区；未增加运行时蜂鸣、停机保护或温度伤害逻辑，油温信号仍待补。最新refined模型/源文件/预览已覆盖，首版study保留。

## 科学校核与改进（2026-10-20）

- 同类结构参考：Weschler原厂 https://www.weschler.com/wp-content/uploads/2018/05/055.pdf 明确指示刃与刻度同圆弧、弯曲透明窗及变送器配套测量；https://www.weschler.com/wp-content/uploads/2018/05/international_instruments_edgewise_panel_meters_057.pdf 列出三角指针、定制刻度等选项。仅用于同类设计校核，不是DF4原车型号证据。其塑料壳不照搬，当前保留用户要求的金属外观。
- 贝太力原厂XQWB-3页面搜索索引 https://www.yxbtl.cn/m/view.php-aid%3D281.htm 支持机车油温/水温、温度转换电流后二次电测表显示、0–150 ℃范围；正文此次访问失败，因此仅作配套方案旁证，不能确认本车传感器型号、电流范围、线制或温度限值。
- 指示刃厚度方向缩至首版55%，读数仍沿相同标定圆弧；指示节点靠近刻度至0.25 mm名义层距，减小模型视差，但不宣称零视差或计量精度。数字放大10%，螺丝金属更哑光、玻璃轻微粗糙度，保留清晰刻度。
- 两模块各361位置包络、31条刻度基线检查通过，数值见 `temperature_gauges_refined_audit.json`；仅为几何校验，不包含结构强度或电气认证。尚未增加无来源的红区/报警温度。
- 新模型 `Assets/Models/Cab/water_oil_temperature_gauges_refined.glb`；入口 `tools/cab_reference/refine_temperature_gauges.py`；源文件与预览 `temperature_review/temperature_gauges_refined.blend`、`temperature_gauges_refined_reference.png`。仍未接Unity，油温独立信号仍缺失。

2026-10-20，接续转速表Unity验收。沿用窄长弧形窗口、哑光漆壳、发黑压框及玻璃分层，浅色背景柔光预览。

## 设计与证据边界

- 两只独立模块，分别标注冷却水温、机油温度；候选0–150 ℃，小格5 ℃、大格25 ℃，不是已确认的原车盘面。
- 原厂资料 https://qingfeng5585.com/en/cgqxl/312.html 明确铁路柴油机车油温/水温配套铂电阻温度传感器；不能由此断定Pt100、阻值曲线、插头针脚及报警阈值。
- 弧形导轨及指示端延续现有视觉设计，不宣称为原车仪表内部机构复刻；后部接口仍是通用外形，电气定义待选型。
- 预览82/65 ℃仅作不同读数展示，不代表正常温度限值。现有 `CabEngineState.WaterTempCelsius` 可供后续接线；无独立油温，不得套用水温或由油压推算。

## 交付

- 模型 `Assets/Models/Cab/water_oil_temperature_gauges_study.glb`，58网格、18377三角面，两个独立指示节点。
- 可复现入口 `tools/cab_reference/build_temperature_gauges.py`。
- 源文件、预览与审计 `tools/cab_reference/temperature_review/`。
- 本阶段为首版造型，尚未导入Unity或绑定温度状态；后续校验行程、优化材质并补独立油温状态，再做Unity接线。
