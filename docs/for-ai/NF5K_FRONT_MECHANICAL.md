# NF-5K 车头下部机械结构 — 2026-10-21

## 本轮范围

`NF5KReferenceBody.BuildNoseDetails`调用`NF5KFrontMechanical.Build`，替换原车钩占位方块，并为下裙板中间让出实际开口。原图仍为`ExternalAssets/nf5_exterior_ref.png`。

模型包括：铸钢钩头、独立钩舌、竖向销轴、接触面、钩柄、承托座与磨耗垫、牵引轭和纵向梁外形、提钩杆及支座。可见受力路径通向车架，不依赖装饰裙板。缓冲装置仅有外形包络；不是经强度验证的真实车钩设计，也不声称可与AAR或其他标准实车互换。

钩中心高度为模型坐标y=-0.42m，距本模型名义轨面y=-1.36m约0.94m；这是可视化候选，尚未针对最终车型和线路标准标定。

制动管路包含硬管、固定夹、折角塞门、横置关闭手柄、压接套、下垂软管、接头密封圈/锁耳及收纳支架。前端未连挂时塞门保持关闭、软管停放在支架，避免表现为敞开制动管直接放风。没有接入实际制动管压力或气路连通仿真。

下裙板补检修接缝、紧固件、百叶内挡板、底部折边及车钩洞口回边。软管与名义轨面留有间隙；仍需随未来轮轨、悬挂和曲线连挂设计重新验算动态包络。

所有新增部件为静态外观，尚未实现连挂/解钩、钩舌开闭、缓冲受力、车间软管连接或自动制动贯通。此处不改变现有CabEngineState制动公式。

## 生成与验收

- 入口：`CabInteriorBlockoutSetup.BatchBuild`，仍由Unity编辑器生成场景。
- 原生网格：`Assets/Models/Cab/NF5KBodyMeshes/`；材质：`Assets/Resources/Cab/`。
- `NF5KFrontMechanical.ValidateReload`检查占位件移除、关键零件重载、软管名义轨面间隙；这是静态模型检查，不是机械仿真验收。
- 预览：`tools/cab_reference/interior_review/front_mechanical_view.png`。
- 已有外部灯光的暗场及Play Mode检查继续运行。

## 后续：驾驶室与客舱照明

用户已要求后续考虑，暂不在本轮加入未验证的室内光源：

1. 司机工作/阅读灯、仪表照明、客舱顶灯分独立回路，复用外部照明的“控制状态—供电状态—真实光源/灯面”分离方式。
2. 司机室夜间默认低亮度，工作灯避开前风挡，验玻璃反射与对前方观察的影响；仪表亮度独立可调。
3. 客舱顶灯分区布置，预留门区/通道灯及应急照明；应急回路未来接电池供电，不简单依赖柴油机是否运行。
4. 外部照明、司机工作灯、客舱顶灯分别控制；保留既有`InstrumentLightOn`信号，避免新系统覆盖它。
5. 先完善客舱内部和顶棚结构，再决定灯具间距、遮光及照度。验收应包括司机视角夜景、客舱开关对窗外及前窗的影响、断电/应急状态。

## 参考资料

- [Amsted Rail车端系统](https://www.amstedrail.com/products/transit-rail-offerings/couplers-and-draft-gears/)：钩头、钩舌、牵引轭、缓冲装置分工；产品存在客车H型与机车E/F型区别。本项目仅借鉴可识别结构，不据此认定NF-5K钩型。
- [Wabtec折角塞门](https://www.wabteccorp.com/locomotive/braking-systems/pneumatic-components/angle-cocks)：车辆端部制动管关闭、软管连接以及开/关位置锁定功能。
