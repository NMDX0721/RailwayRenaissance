# 双针空气压力表组：首版

## Unity导入与三路风压绑定（2026-10-20）

新增 `tools/cab_reference/export_pressure_gauges_unity.py` 导出原生FBX及URP贴图至`Assets/Models/Cab/UnityPressureGauges/`。编辑器入口`Assets/Editor/CabPressureGaugeAssetSetup.cs`，菜单RailRevival/驾驶台/导入双针压力表定稿；输出预制体`Assets/Resources/Cab/DuplexPressureGaugesFinal.prefab`与验收场景`Assets/Scenes/CabPressureGaugeReview.unity`。

新增运行时组件`Assets/Scripts/Cab/CabDuplexPressureGauges.cs`：BindEngineState读取总风缸、制动缸、列车管三路状态，LateUpdate刷新；均衡风缸不复制列车管值。缺失/无效输入时，保留实物指针回零停放，同时ValidChannels=false及HasEqualizingReservoirSignal=false；此零位不作为有效测量值。未来可通过不绑定状态的SetPressures显式提供四通道，或扩展真实均衡风缸接口。越量程夹取并保留OutOfRange标记。没有新增均衡风缸仿真、没有修改原动力学或制动公式。

原生FBX独立轴标定用于四针转角映射；导入入口执行四通道各361个转角位置检查，另验上下越量程、NaN/Infinity、三路状态绑定及刷新。场景保存对独立ReviewEngineState的绑定；截图使用840/180/500/470 kPa作为明确的模型展示值，Play状态读取三路实际仿真，第四针停放。玻璃为URP透明近似。预览输出`tools/cab_reference/pressure_review/unity_pressure_gauges_review.png`，浅色背景。

验收使用进程参数`--burst-disable-compilation`，不改变项目设置；日志位于`tools/compile/cab_pressure_gauges_final.log`。主3D驾驶台整组装配仍未完成。

最终验收通过：四针共1444个转角样本、越量程与无效输入、实时三路风压刷新、保存场景重载及渲染通过；0个C#编译错误、0个元数据写入失败，日志无崩溃并完成关闭。77个Renderer，约0.28×0.15×0.09米。汇总回执`tools/compile/cab_pressure_gauges_acceptance.json`。

## 最终美术稿（2026-10-20）

预览补光修正：用户指出左表右下角似乎缺螺钉。复核GLB每模块均有4颗Mount_Screw，共8颗；左表右下角位于表壳投影中，原预览过暗。新增仅用于预览的下部面积补光，保持固定件位置和模型结构；补光不导出GLB。

用户要求质感与科学性优化后定稿。当前最终资产`Assets/Models/Cab/duplex_air_gauges_final.glb`，入口`tools/cab_reference/finalize_duplex_air_gauges.py`；源/图/回执为`tools/cab_reference/pressure_review/duplex_air_gauges_final.blend`、`duplex_air_gauges_final_reference.png`、`duplex_air_gauges_final_audit.json`，新增材质贴图位于`Assets/Models/Cab/Textures/DuplexPressureFinal/`。

保留非金属旧漆层的钢壳/面板，压圈改为独立发黑钢材质；缎面钢紧固件、略暗的黄铜接口、暗红哑光针、米白盘与低粗糙度玻璃分开处理。按用户最新要求，预览改用浅灰背景、均匀环境光及大面积柔光，移除条形强反射，避免遮挡盘面；预览灯不导出。入口`tools/cab_reference/pressure_preview_lighting.py`，最终生成脚本复用同一配置。kPa标识移开近竖直指针，避免经常被遮挡。

机械细化：黑针中心增加孔口与环形外套，红针使用独立内轴；旧实心公共前轴帽改为环形轴承盖，避免实体封死两轴。外套内径与内轴名义径向间隙0.20毫米；每个背面接头补对应针色的识别环及中文通道名，保持两路独立，不建相通腔。内部波登管、齿轮传动及完整轴承座未建模，不能将外部装配细化称为完整测压机械认证。

四通道各361个角位置检查针片全顶点径向包络，最小盘内余量约5.50毫米；双针层间及玻璃间隙保持，内轴到玻璃名义净间隙0.50毫米，轴帽到玻璃0.60毫米。该检查不替代全装配三维碰撞、公差、强度或计量检定。最终77网格、52931三角面。

孔口复核发现首版布尔切孔工具未继承表模块的父级变换，未真正切中接头；最终版已将接头重建为六角外壁/圆形内孔的实体环形网格，并修正首版生成脚本的父级坐标与修改器顺序。`tools/cab_reference/inspect_duplex_air_gauges.py`检查四个孔中心射线可通过、偏心射线击中壁面，独立输出背面预览`duplex_air_gauges_final_rear.png`；背面通道字样按背向视角设置正立朝向。原首版文件保留供回溯，不再依据首版“有孔”标记作结论。

量程、针色和kPa单位继续作为明确的游戏设计选型；满量程不当作安全压力，未印未经证实的精度等级、原厂商标或报警红线。已复核Nagano GD1资料的量程余量建议及Ashcroft双独立机构说明，来源仍为下列同类参考。均衡风缸仿真信号依然缺失，四针当前为静态展示值；最终美术定稿尚未导入Unity。

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
