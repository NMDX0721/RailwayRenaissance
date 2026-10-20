# 双针空气压力表组：首版

用户要求先将已有驾驶台改动Git提交，再制作双针空气压力表组。此前工作已在`feat/cab-components`分支提交为`1f9835d`，未推送；其他桥接、技能、派单及经济公式等未完成改动保持原状。

## 参考与设计边界

外形依据`tools/cab_reference/DF4_0001_Cab.jpg`左侧圆形压力仪表区：嵌入倾斜面板、黑色圆框、浅色盘面。当前两表方案是功能选型，不宣称原图恰好两只，也未确认具体仪表型号、尺寸、针色分工或量程。

一手同类参考：

- [Ashcroft 1339A](https://www.ashcroft.com/products/pressure/pressure-gauges/1339a-duplex-pressure-gauge/)明确两套独立测压/传动机构、两根波登管、红黑双针，以及铁路制动用途；用于支持双独立通道结构，不照搬美式单位和外形。
- [Nagano GD1](https://products.naganokeiki.co.jp/products/110.html?lang=en)说明两个压力接头，可同时监视气缸与风缸；该产品建议稳压/脉动压力留量程余量。不能将这些说明当作东风4原厂配置或本资产计量认证。

首版为两只可分离模块：

| 模块 | 红针候选通道 | 黑针候选通道 | 量程 | 展示值 |
|---|---|---|---|---|
| MR_BC | 总风缸 | 制动缸 | 0–1600 kPa | 840 / 180 kPa |
| BP_ER | 列车管 | 均衡风缸 | 0–1000 kPa | 500 / 470 kPa |

两盘每小格20 kPa、每200 kPa标数字、100 kPa中刻线。满刻度不是允许持续工作压力；MR选1600 kPa为现有约840 kPa显示保留余量。展示值用于区分四针，不是接入仿真后的运行快照。采用kPa而非假造原车年代单位；后续若确认原图采用旧单位，应按正确换算重做刻度。

## 资产与结构

入口`tools/cab_reference/build_duplex_air_gauges.py`；输出`Assets/Models/Cab/duplex_air_gauges_study.glb`。源、预览与回执：`tools/cab_reference/pressure_review/duplex_air_gauges_study.blend`、`duplex_air_gauges_reference.png`、`duplex_air_gauges_audit.json`。PBR贴图在`Assets/Models/Cab/Textures/DuplexPressure/`。

每表黑漆钢壳、凸起圆框与窄金属内唇、橡胶座、米白盘、独立玻璃、四个面板固定螺钉、背面两只带真实开孔的六角黄铜接口。接口孔径和外形为美术选型，未宣称具体螺纹规格，不使用画黑圆代替孔。

四个运动节点分别为`MR_BC_Red_Pivot`、`MR_BC_Black_Pivot`、`BP_ER_Red_Pivot`、`BP_ER_Black_Pivot`。模型本地Y轴转动，统一`angle=-135°+270°×clamp(p/full,0,1)`，双针共用圆心、分层安装；表面同心轮毂仅表达套轴关系，未制作内部波登管、扇形齿轮和传动细节。

针尖半径44毫米小于盘面半径49.5毫米；红黑针几何层净间隔约0.76毫米，玻璃与中心帽名义净间隔约0.60毫米。以上是几何设计尺寸检查，未做完整实体碰撞/公差或承压试验。

首版导出核验：65个网格、47314个三角面，GLB约3.53 MB、6张内嵌贴图，四个独立指针节点均存在，不依赖外部贴图路径。修正针片封闭面法线；背面接口为有开口的几何孔穴，内部测压管路与螺纹仍未建模。

## 后续接线

现有`CabEngineState`有MainReservoirKpa、BrakeCylinderKpa、BrakePipeKpa；没有独立均衡风缸模型。第四针不可复制列车管值冒充真实均衡风缸压力。首版尚未导入Unity或绑定状态；先让用户审阅形体、盘面与针形，再确定下一轮细化。
