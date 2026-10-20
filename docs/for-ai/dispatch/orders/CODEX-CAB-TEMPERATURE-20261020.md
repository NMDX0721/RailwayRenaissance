# 水温／机油温度表组首版

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
