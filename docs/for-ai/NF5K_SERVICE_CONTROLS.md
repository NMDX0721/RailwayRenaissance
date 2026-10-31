# NF-5K 鸣笛与手动撒砂实体控制

2026-10-25，当前单驾驶室。`NF5KServiceControlSetup`经`NF5KCabFittings.Apply`进入四检查场景，隐藏原未接线`Horn_Push`及旧单语字，保留原件作定位来源。

## 参考和布局

RDSO/CAMTECH《Introductory Handbook WDG6G》（2022–23）明确描述撒砂按钮和按下期间鸣响的汽笛按钮；它证明电气按钮操纵方案真实存在，不表示其整套联锁/参数适用于NF-5K。官方资料：https://rdso.indianrailways.gov.in/uploads/files1/Introductory%20Handbook%20WDG6G.pdf

印度中央铁路ABB机车培训资料另有手动鸣笛杆和撒砂脚踏，说明不同车型操作形式不同。该例是电力机车，仅作人机控制形式对照，不移植其受流/电气系统。https://cr.indianrailways.gov.in/cris/uploads/files/1383198951757-ABB%20Loco%20in%20English.pdf

本车选择两颗弹簧复位电气按钮：鸣笛米灰帽，撒砂赭黄帽，金属安装圈、密封座和四颗凹槽固定螺丝，中文主字／英文副字。110×60×3 mm公共底板贴台面；沿原鸣笛位置横向排布，避开前方风扇调速板和台面前缘。生成时用实际台面网格射线检查四角支承，并检查风扇底板包络不相交。没有新增悬空厚盒。安装尺寸/间隙是本车候选布局，不宣称规范尺寸。

## 操作与守卫

`CabServiceControl`区分鸣笛/撒砂，沿用`CabContextInteraction`的主交互绑定，不新增必背快捷键。按住主交互键维持，松开、移开焦点、打开模态界面、失去应用焦点或禁用时释放；点击动作菜单发起0.5模拟秒短脉冲。帽实际下移2 mm，释放回位。

当前选择电气操纵气动执行的候选方案：距相机不超过2.4 m、详细模拟启用、Master且有电池/发电供电、MainKpa≥400才接受。400 kPa为本模型操作门限，非资料给出的实车额定值。失电、低风压或离开距离时释放，不维持隐藏撒砂开关。短脉冲使用游戏时间；暂停时保持动作和短脉冲均释放，避免暂停菜单中鸣笛/撒砂卡住。

鸣笛通过`CabEngineState.SetPhysicalHorn`的独立保持位，不反复重置旧`TriggerHorn`的0.5秒脉冲；`IsHornActive`合并两者，保留旧UI入口兼容。松开实体按钮不主动清除其他入口已经发出的旧短鸣请求。`CabHornAudio`读取同一状态，提供位于车前部的空间AudioSource和音量渐入/渐出。

撒砂直接使用现有`SandOn`，由`CabSimulationDriver`传给`SimulationInput.Sand`，继续作用于已有黏着上限；不新增一套速度/牵引写入器。当前没有模拟砂量消耗、喷口方向、气动流量/耗风和真实轮滑反馈，本批不伪装这些功能。

## 音源

`build_service_control_assets.py`生成原创311/370/440 Hz三音合成循环及其谐波，作为可替换的NF-5K汽笛音色，不是实车录音。PCM/44.1 kHz单声道保留采样率导入，循环不经过有损编码；AudioSource音量包络避免突然开关造成跳变。音频验证检查解码PCM、非零RMS、幅度界限及循环边界与内部采样步长关系，不冒充真人试听、声压级校准或驾驶室隔声模拟。

## 验收

`tools/compile/service_controls_v2_stdout.log`退出0，四场景生成及台面四角/风扇底板间隙检查通过，`NF5K_SERVICE_CONTROL_OK`和`NF5K_SIMULATION_PLAYMODE_OK`通过。首轮v1因默认音频编码下循环边界检查失败，不作为交付；后改PCM并用边界步长相对内部波形采样步长验证，实测44.1 kHz、RMS 0.2090、边界步长0.0884、内部最大步长0.1182。

`tools/compile/service_controls_final_stdout.log`退出0：独立重开后保留光学材质/导入设置，真实按钮射线、上下文动作、2 mm行程、保持/释放、短脉冲到期、断电/低风压/远距离/禁用/暂停、鸣笛播放渐变停止、撒砂进入Input.Sand及清除均通过，并完成既有驾驶回归。保持/释放使用实际组件方法和上下文执行路径检查，未声称进行真人按键手感验收或Android设备测试。

`tools/compile/service_controls_capture_v2_stdout.log`退出0，`NF5K_SERVICE_CONTROL_CAPTURE_OK`。人工核对`horn_sand_controls_v2.png`和`horn_sand_console_v2.png`，双语字朝向正常、底板落台面，当前按钮与风扇面板无重叠。初稿v1近景相机倒置，不作为终图。额外修正灯窗共享材质的静态默认发光为黑，保留URP发光标志，避免编辑器未运行时全白；独立重开材质关键字和实际Play发光仍通过。

未修改`CabSimulation.cs`和`CabSimulationCodec.cs`，其SHA256分别保持`523D13EDD4A56873917B08D6E282304FCD227026EDA28DCB24AEFF9577FF52AD`、`36E515D8B7C5F107309BF1688FCED3C9F37B1BE3505DF8F51AD12422AEC044FD`。

2026-10-25接触修正：旧底板x最大0.946 m与小闸安装耳包络起点0.940 m约重叠6 mm。新版生成时增加对大小闸零件的15 mm水平扩展包络检查；Play回归在实际驾驶姿态下再次核对。材质及实景验证见NF5K_BRAKE_REFINEMENT.md。
