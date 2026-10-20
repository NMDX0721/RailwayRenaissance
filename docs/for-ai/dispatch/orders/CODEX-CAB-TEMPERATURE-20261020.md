# 水温／机油温度表组首版

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
