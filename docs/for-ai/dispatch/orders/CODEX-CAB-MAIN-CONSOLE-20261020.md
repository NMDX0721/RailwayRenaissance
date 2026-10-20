# 主驾驶台主体与组件试装第一版

## 第三版：双闸可辨识与辅助操纵区（最新）

静态测量：双阀主轴平面间距208.7 mm；两组移动几何高度区间相隔约50 mm。全转动平面圆包络有46.9 mm重叠，但高度分层；这个平面投影重叠不等于阀把实际穿模，也不能据此宣称戴手套操作余量合格。`inspect_console_layout.py` 输出实际装配包围盒与包络指标，正式阀位限角和手部空间仍待验证。

高低两只阀一直存在，本轮移动到更靠近司机的位置，小闸向前/右分开，主轴平面间距要求超过200 mm，增加大闸/列车制动、小闸/机车制动铭牌。补换向柄、鸣笛钮、带护栏的柴油机停机钮；辅助板新增四只警示灯镜片与通信安装预留。新增几何尚未接交互/报警驱动。

各国原厂/政府资料与逐项缺件/后端清单见 [设备完整性核对](../../CAB_EQUIPMENT_COMPLETENESS.md)。明确缺油压表、燃油/电压监测、主控使能/启动件、机车信号/防护、警惕、雨刷、通信、停车制动等；不能宣称全部组件齐全。

## 第二版：消除办公桌轮廓（最新）

第二版Unity复验完成：368渲染器，包围盒约1.55×1.30×0.73 m，共用发动机状态、缺失信号和停机检查通过，重开场景截图通过，无C#编译错误，编辑器正常退出。证据仍写入 `cab_main_console_acceptance.json`，日志为 `cab_main_console_revision2.log`；整体与驾驶视角预览已更新。

用户指出首版像办公桌，撤销双侧落地柜/中间贯通桌洞方案。替换为连续折板式操纵台下壳，后退的膝部凹腔、上部倾斜前裙、整体内缩底座；不再宣称0.99 m贯通腿洞。棕色台面改为带削角和中央内收轮廓的控制托盘，保留既有操纵件位置和琴键开孔。前裙改为窄维修板缝和防脱紧固件，取消办公柜门、把手与铰链。

仪表板新增侧封板和顶部折边，使其与后壳成为一体，而不是桌面后立一块黑板。速度表从x=0.56 m移到0.33 m，收进主要视野。保留原单体比例，重新生成GLB/FBX和Unity独立场景；交互与热模拟边界不变。

## Unity首轮验收结果

已生成预制体与独立验收场景；366渲染器，包围盒1.55×1.31×0.72 m；共用发动机状态、电流/转速/水温刷新、缺失油温与均衡风缸、停机转速归零通过。场景重开后截图完成，无C#编译错误，编辑器正常退出。证据 `tools/compile/cab_main_console_acceptance.json`、`console_review/unity_main_console_review.png`。

`render_console_driver_view.py` 从眼点(-0.10,-0.78,1.38)m输出 `main_console_driver_view.png`。20 mm镜头为较宽视野预览；右侧速度表在更窄视野下需转头，尚未完成最终位置/倾角优化，不能把广角截图当成人因合格证明。

2026-10-20，用户批准开始主体。采用DF4-0001照片的长台面、倾斜仪表板、台面控制器和制动阀布局语言，允许改装；不是原车精确复制。

## 交付与尺寸

- 主体宽1.55 m，台面顶0.839 m，深0.66 m，仪表板后倾15°。下部双侧维修柜，中间约0.99 m净宽；这些是工作估值，非人体工学认证。
- 深棕酚醛台面、灰绿漆钢柜、黑色可拆仪表板、金属包边。柜门缝、门锁、铰链、通风槽、面板螺钉、右侧预留盲板。
- 已试装真实组件几何：双针风压表、牵引电流表、柴油机转速表、油水温度表、速度表、三辐控制手轮、大小制动阀、十联琴键开关。未补造多余假仪表填满空位。
- 仪表板实际Boolean贯通安装孔，后壳退到仪表背后，避免用实心板穿过表体；琴键台面单独开孔。内部管路/线束尚未制作。
- 琴键源文件整组仅约76 mm宽且附带Desk预览片，装配时排除Desk对象并整体放大3倍至约228 mm，保持键比例。仅改本次安装，不覆盖用户认可的单体源模型；尺寸不是原车型号认证。

## 文件与再生成

- `tools/cab_reference/build_console_body.py` → `Assets/Models/Cab/main_console_study.glb`。
- `tools/cab_reference/console_review/main_console_study.blend`、`main_console_reference.png`、`console_layout_audit.json`。
- `tools/cab_reference/export_main_console_unity.py` → `Assets/Models/Cab/UnityMainConsole/` 原生FBX及URP材质通道。
- `Assets/Editor/CabMainConsoleAssetSetup.cs` → `Assets/Resources/Cab/MainConsoleStudy.prefab`、`Assets/Scenes/CabMainConsoleReview.unity`。这是独立装配验收场景，不替换现有游戏主场景或2D驾驶台。

## 信号及未完成边界

同一个CabEngineState供电流、转速、三路风压和水温读取；油温和均衡风缸仍明确缺失。控制手轮、闸把与琴键目前为装配几何，不宣称已经绑定鼠标操纵；速度表亦仍是已有静态资产。温度热网络另按热管理设计推进。

驾驶眼点/座位中心参考节点已放置；尚须第一人称视线、不同体型触达范围、闸把全行程扫掠、维修空间与线管走向验收。包围盒/静态间距不能代替这些检查。当前只给主体试装第一版，不标最终稿。

参考：本地 `tools/cab_reference/DF4_0001_Cab.jpg`；https://railroads.dot.gov/sites/fra.dot.gov/files/fra_net/15029/DOT-FRA-OPP-73-1-1972-Human%20Factors%20Survey%20of%20Locomotive%20Cabs.pdf 用作操纵台人因核对方向，未据其宣称本模型符合任何尺寸标准。
