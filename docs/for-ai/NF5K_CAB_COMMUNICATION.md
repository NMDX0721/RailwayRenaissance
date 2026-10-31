# NF-5K 辅助工作区与通信设备

2026-10-23。保留单套主操纵台和既有辅助台尺寸；台下有外侧落地支板、地板法兰和螺栓、前侧裙板、斜撑。本轮补承力横梁、端部连接板、线槽、检修紧固件。后侧腿部空间留空，中央通道不设置柜体。几何连接仅为可视化结构，未经材料强度或车辆振动试验。

## 内容与生成

- `NF5KCabCommunicationDetail` 接在 `NF5KCabWorkstationLayout` 后生成无线电、文件夹和听筒固定细节。电话听筒暂固定在托座，无摘挂通话。
- 原创松岭通信 SL-R8：八档预置频道、六档接收音量（0..5），不绑定真实频率或国家制式。壳体通风口、紧固脚、双语面板、实体旋钮索引和供电指示。
- CW-01《随车检查记录》：制动/风压、通信、车门/通道、安全器材及途中记录、交班栏。空白栏保留为表单，不伪造已完成检查。不是完整行车或制动试验规程，也不自动判定检查完成。
- 正式文本源 `Assets/Resources/Cab/workstation_equipment_profile.json`；`tools/cab_reference/build_workstation_prints.py` 生成两张原创标签，使用项目字体。`NF5KEquipmentPrint` 将标签作为受光印刷面，不使用发光文字。

## 交互及边界

`CabWorkstationEquipment` 提供无线电本机开关、上下频道、音量上下限，以及阅读资料。沿用 `CabContextInteraction` 自定义主交互键和就近动作列表，无新增必须记忆的快捷键。阅读复用资料模态框，阻断背景交互，Esc/关闭退出。

供电读取现有 `CabInteriorLighting.MainPower/BatteryPower`，与模拟供电输入一致；开关与旋钮允许断电操作，灯仅在开关开启且有供电时亮。掉电保留机械档位，组件禁用回到初始检查状态。暂无通信电池耗电、收发音频、调度对端、网络或存档字段；音量旋钮目前是本机控制状态，不代表已有真实接收音频。

`NF5KCabCommunicationReview` 纳入既有 Play Mode 回归：真实射线、上下文动作、电源/备用供电、断电档位保持、频道循环、音量端位、远距离拒绝、资料阅读与禁用复位。`NF5KCabWorkstationReview` 继续验证选定座椅、后侧通道及门扇扫掠。最终验收结果追加于下方。

首轮完整回归：`tools/compile/cab_communication_delivery.log` 已通过通信交互、既有器材/遮阳板/柜门、通用交互及驾驶启动/牵引/停车回归；四场景尺寸与门扫掠检查通过。近景发现纸面高光影响文字后，印刷纸材质关闭镜面和环境反射，保留受光漫反射；最终渲染与回归另记。

纸面曝光对照：`NF5KWorkstationLightAudit.BatchInspect` 不保存场景，分别关闭阅读灯、以 0.5 强度开启阅读灯出图。对照确认该位置可得到清晰表单；辅助阅读灯按新台面照射位置从 5 调至 0.5（美术单位，非照度认证），只改此灯，保留司机灯与其他照明回路。生成器和现有场景由同一布局入口更新。检查图使用临时柔和补光，不替代夜间照度/亮度测量。

最终验收：`tools/compile/cab_communication_final_stdout.log`，Unity 进程退出码 0，四份场景更新，四次 `NF5K_CAB_WORKSTATION_OK`，以及 `NF5K_COMMUNICATION_PLAY_OK`、`NF5K_CONTEXT_INTERACTION_OK`、`NF5K_SIMULATION_PLAYMODE_OK`。最终使用 D3D11 批处理；前几次启动提前退出没有执行验证，不作为验收证据。表单近景 `crew_record_close_v6.png` 已确认文字可读，台下近景 `aux_underdesk_support_v6.png` 展示实际支撑。大范围检查图的临时点补光仍可能让小纸面过亮，应使用低补光近景检查文字，勿用该补光作为运行时照明。主模拟核与快照编解码文件哈希保持不变；无调度收发/音频、听筒取放或填写记录玩法。
