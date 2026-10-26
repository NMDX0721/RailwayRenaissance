# 铁路复兴：沙能冲击 — 代码地图

2026-10-26 ATO V2：`CabAtoControllerV2`共用有限候选预测并切换三目标；`AtoRouteTask`保存相对起点/限速/时刻，`CabManualControlIntent`处理真实操作接管。研究检查点封装v2、暂停落盘恢复、精确环境与Tick调度已验；冻结失配实验仍有失败。详见[研究结果](../research/ato_v2_results.md)及[接口记录](ATO_V2_IMPLEMENTATION.md)。

2026-10-25 ATO第二版开始：`CabAtoCheckpoint`为冻结V1控制器提供精确兼容侧车；研究适配器新增完整检查点/暂停，物理适配器新增恢复通知与预验证。见[第二版实施记录](ATO_V2_IMPLEMENTATION.md)，路线/三模式状态按该记录后续验证。

2026-10-25研究指标：`research/ato/Program.cs`新增观测队列与冻结压力协议执行，`analyze_stress.py`核对配对、哈希与调控代价；未变更游戏控制器/内核。见[压力测试](../research/stress_metrics_review.md)。

2026-10-25研究后续：`CabAtoController`暴露切换惩罚/最短驻留参数，Unity适配器默认0.5秒；`research/ato/heldout-protocol-v1.json`冻结首次留出工况，`analyze.py`核对配对、哈希和实际切换间隔。见[本轮结果](../research/switching_and_holdout_review.md)。

2026-10-25研究原型：`Cab/Research/CabAtoController`为独立离散预测/有效响应估计核心，`CabAtoResearchDriver`通过`CabSimulationDriver.BeforeSimulationStep`每5步发送既有驾驶命令；人工接管与联锁保留。`NF5KAtoResearchReview`生成独立场景并做真实Play验收，`research/ato`保存开发对照。见[实现与限制](ATO_RESEARCH_IMPLEMENTATION.md)，未完成留出测试或实车标定。

2026-10-24 单驾驶室行车灯光面板已通过Unity生成和Play回归：左旋钮关/弱/强，右旋钮关/日行/夜行/调车/本端尾灯/手动；后端协调实际灯组并支持手动退出预设、断电保持与旧接口恢复。多国规则来源及版本限制见[NF5K_EXTERIOR_CONTROL_PANEL](NF5K_EXTERIOR_CONTROL_PANEL.md)。

2026-10-24 琴键v7：框架与上侧功能区整合成单一连续开口底板，双语印字改透明底消除色块分割；启机面板向仪表移35 mm、安装孔重算，检查两组实际间距。网络依据及限制见[NF5K_LIGHTING_KEYS](NF5K_LIGHTING_KEYS.md)。

2026-10-24 琴键文字v6：提高功能字明暗对比、加粗放大中英文、压低印刷面镜面高光；`NF5KLightingKeysReview.RefreshPrintsBatch`提供贴图/材质更新与实景预览。见[NF5K_LIGHTING_KEYS](NF5K_LIGHTING_KEYS.md)。

2026-10-24 操纵区布局：琴键功能名移至靠仪表的上侧边框；`NF5KConsoleRecessSetup`将钥匙/燃油泵/启机面板法兰贴近台面、接线盒下沉，台面生成真实安装开孔和孔壁，移除旧垫块。见[NF5K_LIGHTING_KEYS](NF5K_LIGHTING_KEYS.md)。

2026-10-24 琴键标注修正：取消台面独立牌、可见编号与总标题，功能名称整合进原组件前边框加宽区，沿用边框金属；中英词条和照明接线保持。见[NF5K_LIGHTING_KEYS](NF5K_LIGHTING_KEYS.md)。

2026-10-24 照明琴键：`CabLightingKey`逐键绑定原转轴/照明状态；`NF5KLightingKeysSetup`生成固定中英铭牌与独立碰撞体；`NF5KLightingKeysReview`验证射线/七路/供电/调光/中英文。`lighting_keys_profile.json`共用印刷与提示词条，`SetLanguage`为未来全局语言接口，非全游戏本地化。见[NF5K_LIGHTING_KEYS](NF5K_LIGHTING_KEYS.md)。

2026-10-24 双雨刷：`CabWiperMotion`为独立相位/停放状态，`CabWiperSystem`为几何/供电/近距指令适配，`NF5KWiperSetup`替换静态杆件并补内侧总成及实体控制；`NF5KWiperMotionReview`检查运动、接触和上下文交互，`build_wiper_prints.py`生成双语标签。见[接口与边界](NF5K_WIPERS.md)。

2026-10-24 平板设置按键恢复：`KeyBindings.ResetToDefaults()` 将全部操作映射恢复为默认键表并一次保存，`TabletSettingsAppUI.RenderControlSettings` 的恢复按钮调用该 API；源码审计见 `TABLET_SETTINGS_AUDIT.md`，Unity验收待做。

2026-10-24 `Cab3DSceneController.WipersOn/SetWipers/WiperStateChanged`分离刮雨选择与天气；`NF5KWiperStateReview`进入驾驶Play回归，实体动作待接。`NF5KCabCirculators.PanHead`生成真实槽底/槽壁及双子网格，见[雨刷](NF5K_WIPERS.md)和[风扇收尾](NF5K_CAB_CIRCULATORS.md)。

2026-10-24 风扇控制板v32：`NF5KCabCirculators`改3 mm板、浅旋钮与四角螺丝，`CabCirculationFan.Apply`以±12°翘板表示定向/摆动；`build_circulator_prints.py`输出V2刻度，回归测量旋钮高度及四角分布，见[控制板](NF5K_CAB_CIRCULATORS.md)。

风扇v28：`CabCirculationFan.Head/ConfigureMotion/ToggleOscillation`接竖轴、独立选择器和线缆余量段；上下文四项动作。`build_circulator_prints.py`生成厂徽、背面铭牌及台面刻度，`NF5KCabCirculators.Print`生成正向UV印面；详见[回归范围](NF5K_CAB_CIRCULATORS.md)。

2026-10-23 风扇v25：`NF5KCabCirculators`在固定中心盖上生成`Fan_Original_Wing_Rail_Logo`原创印层，独立于转子；见[中心标识](NF5K_CAB_CIRCULATORS.md)。

2026-10-23 风扇v24：`NF5KCabCirculators.PanHead/Yoke`生成圆头五金及连续折弯叉架网格；`NF5KCabCirculatorsReview`核对吊杆直径、叉架接触和四颗螺丝安装面，见[循环风扇](NF5K_CAB_CIRCULATORS.md)。

2026-10-23 风扇v23：`NF5KCabCirculators`生成V3连续护网、96边圆柱、后网固定筋、输出轴及随头部倾角定位的轴座。`NF5KCabCirculatorsReview`增加护网法线、轴座/壳体接触和两侧控制板实际网格高度检查；材质贴图与范围见[风扇文档](NF5K_CAB_CIRCULATORS.md)。

2026-10-23 风扇v21：`NF5KCabCirculators`生成独立V2圆弧叶片/连续扫掠护网和台面控制板；`CabCirculationFan.Control`以台面旋钮父节点作为操作距离基准，风扇头部移除交互碰撞体。`NF5KCabCirculatorsReview`核对台面高度、射线与扩大后的转子包络，见[循环风扇](NF5K_CAB_CIRCULATORS.md)。

2026-10-23 驾驶室补件：`NF5KCabCirculators`替换两个风扇占位件，生成150mm叶片、共用线材护网和连接顶棚的支架/走线；`CabCirculationFan`接现有上下文开关/低/高及供电状态。`NF5KCabCirculatorsReview`纳入驾驶回归，范围与真实参考见[循环风扇](NF5K_CAB_CIRCULATORS.md)。

2026-10-23 原声同步样板：`tools/music/align_ost_lyrics.py` 本机独立识别/受约束对齐并生成审阅证据；`Resources/Music/Lyrics/sleepers.txt` 打包 35 行逐句自动草稿，`sleepers_info.json` 明示待试听校准。`MusicTimedText` 读取内置来源标签，`MusicLyricsView` 处理韩文字体及紧凑间奏标记；`TabletMusicAppUI.SeekTabletMusic` 暂停时也按活动 clip 选择声道。其余三首候选未发布。见 [对齐样板与限制](MUSIC_FOLIA_CORE_REVIEW.md)。

2026-10-23 v16整门比例：`NF5KPassageDoorDetail`门扇0.86×1.87 m、窗0.68×0.75 m，紧凑把手底板；`AxisZ=1.85`与`AxisX=-0.44`由布局/基础生成器共享。`NF5KCabWorkstationReview`输出实际宽高、窗板和把手测量；`NF5KCabFittings`补完整视图/正交比例图，真实参考与边界见[通道门v16](NF5K_PASSAGE_DOOR.md)。

2026-10-23 v15比例修正：`NF5KPassageDoorDetail.HandleHeight=0.92 m`统一双面把手、轴和锁孔高度；长底板顶端降至1.05 m，与窗下横框留30 mm错层，文字44×78 mm。`NF5KCabWorkstationReview`新增把手高度及底板/窗框间隔核对，见[通道门](NF5K_PASSAGE_DOOR.md)。

2026-10-23 当前门五金：`NF5KPassageDoorDetail`改为140 mm握柄与不对称长底板直接标字，取消旁置推拉牌；`NF5KCabFittings`改50 mm圆扶手固定座。`NF5KCabWorkstationReview`核对新字面尺寸与位置。平板首批系统层由反重力通过ACP返回代码、Codex集成，接口及边界见[TABLET_SYSTEM_SHELL](TABLET_SYSTEM_SHELL.md)。

2026-10-23 随身听后续：`RailwayMusicTerminalView.UpdateSliderFill` 提供真实水平滑条填充，USS 统一圆点/滑轨中心与对称播控；减少动态选项覆盖界面过渡和唱片旋转。`tools/music/extract_ost_lyric_sheets.py` 从四首 MP3 提取真实内嵌歌词，打包到 `Music/LyricSheets`；`MusicTimedText` 无同步资源时回退到歌词阅读稿，`MusicLyricsView` 区分阅读与同步，不猜时间戳。参见 [Folia 体验与歌词来源](MUSIC_FOLIA_CORE_REVIEW.md)。

2026-10-23 门区可读性：`NF5KPassageDoorDetail` 将推拉牌移到把手同高的实体门板上并放大；`NF5KCabFittings` 补固定扶手法兰/回接及静态碰撞包络。见 [通道门](NF5K_PASSAGE_DOOR.md)。

`NF5KEquipmentPrint.Add` 现会在目标尺寸改变时更新已有印刷面网格，`NF5KCabWorkstationReview` 核对推牌实际尺寸，避免旧资产缓存使文字未随底板放大。

2026-10-23 隔门加宽：`NF5KPassageDoorDetail` 同步迁移隔板/门头/门体几何并使用 0.82 m 洞口、0.78 m 门扇、x=-0.40 m 铰轴；基础生成器保持相同尺寸。`passage_door_profile.json`、`build_passage_door_prints.py` 和 `NF5KEquipmentPrint` 提供双语门牌与玻璃字样。见 [通道门精修](NF5K_PASSAGE_DOOR.md)。

2026-10-23 随身听字幕与打磨：`MusicTimedText` 解析 LRC/增强 LRC/SRT/WebVTT、字幕来源与翻译导入；`MusicLyricsView` 负责沉浸字幕页、真实时钟高亮/词填充、点句定位和校准。偏好随 `MusicLibrarySession` 的独立 JSON 保存，旧字段兼容；播放器补播放历史与界面内快捷键，并保留曲库滚动/焦点。参考上游 Folia 0.7.21 但未复制其源码；见 [核心体验对照](MUSIC_FOLIA_CORE_REVIEW.md)。

2026-10-23 通道门：`NF5KPassageDoorDetail` 补框架/铰链/双面把手，`CabPassageDoor` 接现有交互键、限位转动与候选阻挡检测，`NF5KPassageDoorReview` 纳入 Play Mode。见 [通道门](NF5K_PASSAGE_DOOR.md)。真实玩家胶囊与门锁声学未接入。

2026-10-23 通信与资料：`NF5KCabCommunicationDetail` 细化辅助台承力/走线和固定听筒，`CabWorkstationEquipment` 接本机开关、八频道和音量档位及资料阅读，`NF5KCabCommunicationReview` 验证射线、供电和上下文命令；双语文本/印刷共用 `workstation_equipment_profile.json`。见 [辅助台通信](NF5K_CAB_COMMUNICATION.md)。调度收发与音频未实现。

2026-10-23 随身听改版：`TabletMusicAppUI.BuildMusicApp` 接入 `RailwayMusicTerminalView` 与 `Resources/UI/MusicTerminal.uss`；`MusicLibrarySession`/`MusicLibraryStore` 管理跨页面曲库、队列、收藏、歌单和独立 JSON 偏好，`MusicSessionDriver` 持续驱动音频，帧号去重兼容原 GameMainUI 调用。`MusicTerminalIcons` 绘制图形按钮，`MusicCoverImportSettings` 导入 9 张新绘铁路唱片封套，停止用场景截图作封面；云端入口明确配置与真实连接的边界。见 [铁路音乐终端](MUSIC_TERMINAL.md)。

2026-10-23 布局：`NF5KCabLayoutAudit` 已完成修改前只读网格测量。`NF5KCabWorkstationLayout` 生成与主台同高的辅助工作台、前缘连接段及通信/资料区，并将副司机席内移 20 cm；`NF5KCabWorkstationReview` 验证尺寸、净空和门扇扫掠。见 [左右工作区复核](NF5K_CAB_LAYOUT_REVIEW.md)。音乐模块缺失接口补齐见 [接口兼容](MUSIC_PLAYBACK_API_COMPATIBILITY.md)。

2026-10-23：`NF5KEquipmentPrint` 与 `build_first_aid_prints.py` 将急救柜和三类封装用品改为完整原创双语标签，内容源 `Cab/first_aid_label_profile.json`。`NF5KCabVisors` 替换遮阳板占位件，`CabSunVisor` 通过现有浮动交互提供两侧独立三档调节；网格顶点包络与交互回归纳入 `NF5KSafetyEquipmentReview`。见 [标签/柜体](NF5K_WALL_EQUIPMENT.md) 与 [遮阳板](NF5K_CAB_VISORS.md)。

2026-10-22动漫3D细节：`NF5KAnimeDetailPass`为四门补充随动五金、全车导水沿并收敛主车漆细噪点；从既有生成入口调用，独立BatchBuild同步四个检查场景并执行门扇/驾驶回归。视觉基线为双像素VN、动漫3D、硬核机制，见[细节轮次说明](NF5K_ANIME_DETAIL_PASS.md)。

2026-10-22走行部运动：`CabReviewTrack`提供独立检查线路；`CabRailMotion`只读后端有符号位移，驱动轮对转角、车体/转向架循迹及有限悬挂表现；`CabRailReviewViews`提供跟随视角。`NF5KRailMotionReview`生成CabRailMotionReview并验证直线/曲线/倒车/制动；原静态场景保留。不是轮轨接触力求解器，见[运动说明](NF5K_RAIL_MOTION.md)。

2026-10-22钢轮分层修正：`NF5KWheelGeometry`外侧辐板改连续弧面；`NF5KWheelSteelFinish`统一非接触区基础材质，积尘按局部分布；`NF5KBogieDetailReview`出图临时增加双侧补光和中性反射围场，不改变保存的车灯。见[走行部说明](NF5K_BOGIE_REFINEMENT.md)。

2026-10-22钢轮实拍精修：`NF5KWheelGeometry`提供带圆角的V3共享轮体和截面采样；`NF5KWheelSteelFinish`按截面位置生成克制的细锈与磨亮带；`NF5KWheelTrackReview`替换现有轮网格并扫描截面验证支承。参考图存于`中转站/NF5K_Wheel_References/`，见[走行部说明](NF5K_BOGIE_REFINEMENT.md)。

2026-10-22轮轨与使用痕迹：`NF5KWheelTrackReview`保存三份检查场景的静态支承轨、降低的轨下地面和保留登车基准的轨旁走道，核对8轮踏面接触与轮缘净空；`NF5KWheelSteelFinish`沿截面分配磨亮带/氧化肩部/积尘；`NF5KBoardingAudit`优先采用走道顶面。检查轨不属于线路后端，见[走行部复核](NF5K_BOGIE_REFINEMENT.md)。

2026-10-22钢轮表面：`NF5KWheelSteelFinish`分离踏面与氧化辐板的颜色、金属度/光滑度和细法线纹理；`NF5KRunningGear.WheelUV`改用截面累计弧长展开并生成切线，消除圆柱面UV退化。轮体几何保持不变。见[走行部复核](NF5K_BOGIE_REFINEMENT.md)。

2026-10-22登车/轮体复核：`NF5KBoardingAudit`检查四级梯两侧靴子/抬脚与扶手距离，输出JSON和临时比例人偶；`NF5KAccessLadder`改四级、外移圆管扶手并清理旧磨耗条碰撞体，`NF5KSurfaceFinish`停止重建旧装饰。`NF5KRunningGear`采用18点凹形轮体截面，细节见[走行部复核](NF5K_BOGIE_REFINEMENT.md)。收纳、桥板及站台联锁仍未实现。

2026-10-21走行部灰色系：`NF5KRunningGearFinish`分配灰绿漆架/浅灰轴箱/暖灰设备材质；钢轮改用截面硬肩法线网格。`NF5KAccessLadder`生成开口格栅、六角螺栓/垫圈与12块反光片，`Assets/Shaders/CabRetroreflective.shader`提供受光方向回归近似；`NF5KReflectorReview`独立验证关灯与不同照射角。见[转向架细化](NF5K_BOGIE_REFINEMENT.md)。

2026-10-21梯子/钢轮修正：`NF5KAccessLadder.BuildSide`由车体持续生成入口和转向架独立更新入口共用，构建固定板、托臂、斜撑及踏步连接；`NF5KRunningGear.FinishWheelMaterials/WheelUV`提供轮辐板/踏面分区及同心加工纹理，`NF5KIndustrialFinish.Build`收尾复用以防旧材质覆盖。见[NF5K转向架细化](NF5K_BOGIE_REFINEMENT.md)。

2026-10-21转向架精修：`NF5KRunningGear`补轴箱导向、弹簧承座与弧形踏面闸瓦，将电机/齿轮箱移到非旋转驱动组；`NF5KIndustrialFinish.FinishRunningGear`补钢构小倒角。`NF5KBogieDetailReview`更新三份既有审阅场景并出柔光近景，不重建驾驶台。见[转向架细化](NF5K_BOGIE_REFINEMENT.md)。

2026-10-21场景交互首版：`CabContextInteraction`接管模拟审阅场景的聚焦、近邻提示、多动作、InputAction改键与首次设置/状态引导；`CabStartupPointer`在其存在时停用旧逐帧输入，保留射线验证接口。`NF5KContextInteractionReview`验证绑定切换与设置输入隔离。规格见[场景交互与驾驶教学](../../参考资料/系统/场景交互与驾驶教学设计.md)。小黄鼠委托、全局快捷键冲突管理和完整线路教学尚未实现。

2026-10-21驾驶交互：`CabDrivingTarget`通过`CabStartupPointer`射线分发牵引手轮、大小闸及换向手柄动作；`NF5KDrivingInteractionSetup`由`NF5KSimulationReview.Prepare`在模拟审阅场景中安装网格碰撞体与校准转轴。操作沿用CabEngineState联锁，F2隐藏测试面板。见[NF5K驾驶交互](NF5K_DRIVING_INTERACTION.md)。

NF-5K全车工业材质：`NF5KIndustrialFinish.Build`在SurfaceFinish后应用区域漆面、独立橡胶、玻璃预乘反射及毫米级倒角，保留BoxCollider体积；专用贴图/倒角网格保存于`NF5KBodyMeshes`。`CabInteriorBlockoutSetup`增加分层天光、主光软阴影、弱补光和独立审阅地面，默认仍保存闭门场景。

NF-5K门窗专项：`NF5KReferenceBody.GlazedOpening`统一封板/压边/密封/玻璃/回口；`RakedZ/Shoulder`连接倾斜风挡与车壳；驾驶侧窗安装基准外移、客室窗间与门后过渡面闭合。渲染新增`door_window_junction_view.png`，独立门扇控制和扫掠检查保留。

NF-5K侧门：运行组件`CabSideDoor`提供开度与锁舌退入，编辑器`NF5KDoorReview`检查4门扫掠；`NF5KReferenceBody`切分门洞并将连续腰带挂在门扇上，`NF5KLivery`按字牌名称持久化独立粗体车型标识。0721属于沙能载具，NF-5K车队编号未指定。`CabInteriorBlockoutSetup`保存闭门场景，并另渲染开门预览。门控输入/牵引联锁仍未接。

NF-5K材质：`NF5KSurfaceFinish.Build`在场景保存前补UV、生成Paint/Metal微表面纹理与Soft_Studio_Environment立方体，调整漆面/金属/玻璃材质并补紧固件及踏步磨耗。入口仍`CabInteriorBlockoutSetup.BatchBuild`；纹理与环境资源持久保存至`NF5KBodyMeshes`，未新增运行时依赖或物理玻璃模型。

NF-5K涂装纠偏：`FrontChevron`降低中心并加深弧线，`BuildNoseDetails`补奶油色下裙围带；`NF5KLivery.PetalMesh/Flower/Branch`使用共享缺口花瓣、细蕊与枝条构图替代圆片散点。生成入口及整车/轮对接线不变。

NF-5K完整车体候选：`NF5KReferenceBody`延长至z=17.5并封闭后端；`NF5KRunningGear`生成2组双轴动力转向架及持久车轮截面/螺旋弹簧网格，`ValidateReload`检查2架/4轴/8轮及后贯通门；`NF5KLivery`生成樱花、旗帜及几何车号，字牌使用普通深度测试，避免默认TextMesh透视显示。仍是静态模型，不接轮轨/驱动仿真。见[NF-5K结构基线](NF5K_HEADCAR_LAYOUT.md)最新条目。

NF-5K前脸第二轮：`NF5KReferenceBody.BuildNoseDetails`生成内收下裙、通风口、翼徽与车钩预留包络，`Cylinder`生成圆筒顶灯及压圈；侧门补固定门框。车钩暂无运动/受力功能。

2026-10-21：驾驶客运头车外壳由`Assets/Editor/NF5KReferenceBody.cs`生成圆弧肩部、拱顶、圆角窗框及钢门/扶手/登车梯，入口仍为`CabInteriorBlockoutSetup.BatchBuild`；持久网格位于`Assets/Models/Cab/NF5KBodyMeshes/`，新增外观参考视角。设计推定与未完成项见[NF-5K结构基线](NF5K_HEADCAR_LAYOUT.md)。

驾驶客运合造头车灰模：`CabInteriorBlockoutSetup.BatchBuild`生成CabInteriorBlockout场景，左侧单台、侧门/前风挡、顶棚占位、客室隔门与三排客席首段。用户明确NF-5K头车内含客运空间，撤销“18 m短机车内双司机室”试排；另一端随整列编组安排。权威说明见 [NF-5K结构基线](NF5K_HEADCAR_LAYOUT.md)。

主台启动鼠标链：`CabStartupClickTarget`按Key/FuelPump/Start/Stop派发，`CabStartupPointer`使用Input System鼠标和限定层Physics射线；主台编辑器构建安装StartupControlsWithoutKey并绑定同一CabEngineState。左键插钥匙/转主控/开关/启动停机，右键拔钥匙。完整驾驶室/后端/线路/场景路线见 [仿真实施路线](LOCOMOTIVE_SIMULATION_ROADMAP.md)，这些扩展尚未实现。

启动组件：`build_startup_controls.py`生成with_key/without_key/key_only GLB与FBX；`CabStartupAssetSetup.BatchBuild`生成带钥匙、空锁与独立MasterKey三预制体。`CabStartupControls`提供插拔/旋转/燃油泵/启动停机接口与状态灯，显式绑定CabEngineState；局部联锁非全局发动机守卫，鼠标射线及背包尚未接。见启动组件任务。

机油压力表：`build_oil_pressure_gauge.py`生成0–1 MPa窄框表；`CabOilPressureGauge.cs`读取OilPressureMpa。主台构建加入OilPressure标定节点和运行组件，301点校验世界位置、异常/超量程及停机状态。主体PBR纹理由build_console_body.py生成到Textures/MainConsole，统一导出URP。见油压表任务。

主体第三版在 `build_console_body.py` 中分离Short系列制动阀部件安装位置，并增加辅助控制/铭牌几何；审计断言双阀主轴平面间距>0.20 m。完整性与各国参考见 [设备核对](CAB_EQUIPMENT_COMPLETENESS.md)，新增按钮/换向柄/警示镜片尚未实现交互驱动。

主体第二版仍由 `build_console_body.py` 生成：`prism` 挤出非矩形控制台截面与台面轮廓，连续壳体替代左右桌柜；渲染/FBX/Unity重建流程及资源路径不变。具体结构变更见主体任务第二版条目。

主驾驶台试装入口：`tools/cab_reference/build_console_body.py`，原生导出 `export_main_console_unity.py`，Unity编辑器入口 `CabMainConsoleAssetSetup.BatchBuild`。输出MainConsoleStudy预制体与CabMainConsoleReview场景；电流/转速/风压/水温共用一个CabEngineState，油温/均衡风缸缺失，控制件尚未交互绑定。参见[主体试装任务](dispatch/orders/CODEX-CAB-MAIN-CONSOLE-20261020.md)。

温度表Unity入口：`Assets/Editor/CabTemperatureAssetSetup.cs`，运行组件 `Assets/Scripts/Cab/CabTemperatureGauge.cs`；原生资源 `UnityTemperatureGauges`、预制体 `TemperatureGaugesFinal.prefab`、场景 `CabTemperatureReview.unity`。各表读取共享配置，水温接WaterTempCelsius、油温缺失标无效，保留原值及量程/红区状态；后端热模拟未实现。最终源/预览及验证边界见温度表任务。

最新温度配置：`Assets/Resources/Cab/temperature_instrument_profile.json`，模型脚本读取水温40–120/油温50–150 ℃及各自红区；41/21刻度基线与各361行程点检查。后端计划见 [热管理设计](CAB_THERMAL_SIMULATION_DESIGN.md)，尚未替换CabEngineState旧水温公式；未新增油温运行信号。

温度表科学校核入口：`tools/cab_reference/refine_temperature_gauges.py`，输出 `Assets/Models/Cab/water_oil_temperature_gauges_refined.glb` 及温度表目录内审计/源文件/柔光预览；各361行程点和31刻度基线检查。油温信号及Unity接线尚未实现，参见温度表任务。

柴油机转速表最终美术稿：`Assets/Models/Cab/diesel_tachometer_final.glb`，入口`tools/cab_reference/finalize_diesel_tachometer.py`。0–1500 r/min设计选型、50细分/250数字，31条刻度基线与361位置包络检查；旧漆/发黑钢/玻璃分材质，凹式三触点接头加定位口。内部表芯及电气接口仍未考证，尚未接Unity；现有EngineRpm范围需另校准。见[定稿说明](dispatch/orders/CODEX-CAB-TACHOMETER-20261020.md)。

压力表Unity接线：`Assets/Scripts/Cab/CabDuplexPressureGauges.cs`读取三路现有风压，四针独立、缺失均衡风缸信号回零停放并标无效；入口`Assets/Editor/CabPressureGaugeAssetSetup.cs`，预制体`Assets/Resources/Cab/DuplexPressureGaugesFinal.prefab`，验收场景`Assets/Scenes/CabPressureGaugeReview.unity`。四针各361位置/异常输入/状态刷新随导入核验。下一件柴油机转速表首版`Assets/Models/Cab/diesel_tachometer_study.glb`，入口`tools/cab_reference/build_diesel_tachometer.py`，0–1500 r/min候选、黑色指示端，尚未接Unity；见[转速表任务](dispatch/orders/CODEX-CAB-TACHOMETER-20261020.md)。

双针空气压力表组最终美术稿：`Assets/Models/Cab/duplex_air_gauges_final.glb`，入口`tools/cab_reference/finalize_duplex_air_gauges.py`。发黑钢压圈/漆壳/玻璃分材质，独立内轴与黑针外套、背面通道标识；四针各361位置径向检查，四孔口中心/壁面射线复核，77网格/52931三角面。量程和针色仍是设计选型，均衡风缸信号缺失，尚未接Unity。见[最终稿说明](dispatch/orders/CODEX-CAB-PRESSURE-20261020.md)。

双针空气压力表组首版：`Assets/Models/Cab/duplex_air_gauges_study.glb`，入口`tools/cab_reference/build_duplex_air_gauges.py`；两只模块、四个独立指针节点、背面四只开孔接口。总风/制动缸候选0–1600 kPa，列车管/均衡风缸候选0–1000 kPa；量程及针色为设计选型，均衡风缸信号尚未实现，未接Unity。见[压力表任务](dispatch/orders/CODEX-CAB-PRESSURE-20261020.md)。

电流表Unity接口：`Assets/Scripts/Cab/CabLinearCurrentGauge.cs`读取显式绑定的`CabEngineState.TractionCurrentAmps`，支持0–1500 A弧形指示、超量程/无效输入标志。导入入口`Assets/Editor/CabLinearGaugeAssetSetup.cs`，FBX/PBR资源在`Assets/Models/Cab/UnityLinearGauge`，预制体`Assets/Resources/Cab/LinearCurrentGaugeFinal.prefab`及验收场景`Assets/Scenes/CabLinearGaugeReview.unity`；单体已绑定，尚未组装主3D操纵台。301位置/转角及状态绑定检查随导入执行。下一批组件与信号缺口见[东风4组件计划](DF4_CAB_COMPONENT_PLAN.md)。

长条电流表最终美术稿：`Assets/Models/Cab/linear_gauge_final.glb`，入口 `tools/cab_reference/finalize_linear_gauge.py`；23网格、6151三角面，独立指示节点，最终贴图 `Assets/Models/Cab/Textures/LinearGaugeFinal/`。0–1500 A直流选型及301位置几何检查继承前版，旧漆/缎面钢/玻璃材质分离；原车用途、量程、测量接口与电磁驱动未确认，尚未接Unity。见[任务定稿说明](dispatch/orders/CODEX-CAB-LINEAR-GAUGE-20261020.md)。

长条仪表最新候选：`Assets/Models/Cab/linear_gauge_calibrated.glb`，入口 `tools/cab_reference/upgrade_linear_scale.py`。0–1500 A直流刻度为自主选型，50 A细分/250 A数字，弧长刻度与运动映射一致；301位置窗口包络及数字边界检查通过。螺钉改在压框上下实体边。原车用途/量程、表芯、分流器接口仍未确认，未接Unity；详见[模块任务](dispatch/orders/CODEX-CAB-LINEAR-GAUGE-20261020.md)。

长条仪表当前候选：`Assets/Models/Cab/linear_gauge_raised_bezel.glb`，入口 `tools/cab_reference/refine_linear_bezel.py`。导槽与右向刻度共用基线，新增随弧形窗口开孔的凸起压框与嵌入玻璃；上端螺钉避让压框。14网格、5502三角面，尚未接Unity，内部机构仍属自主设计候选。预览、尺寸及验证范围见[模块任务](dispatch/orders/CODEX-CAB-LINEAR-GAUGE-20261020.md)。

长条仪表自主机构候选：`linear_gauge_mechanism_refined.glb`，入口`tools/cab_reference/design_linear_indicator.py`。刻线朝右、宽红指示端、独立玻璃；弧形导轨/连接杆/指示件按101个位置验证尖端可达与窗口包络。并非原车机构考证，驱动力/摩擦/全装配碰撞未验收，未接Unity。见模块任务。

窄长仪表弧线方向已按用户纠正为向左（-0.006米视觉弓高），连续圆弧配合法向刻线，同名`linear_gauge_curved_refined.glb`与预览已更新；曲率仍未考证，同类Jewell/Tsuruga资料见模块任务。

窄长仪表近照弧线版：`Assets/Models/Cab/linear_gauge_curved_refined.glb`，入口`tools/cab_reference/curve_linear_gauge.py`；刻度板/玻璃轮廓/窗框开口/刻线同步改弧线，外壳保持直线。弓高与机构未考证，未接Unity，见[模块任务](dispatch/orders/CODEX-CAB-LINEAR-GAUGE-20261020.md)。

窄长仪表单模块细化：`Assets/Models/Cab/linear_gauge_photo_refined.glb`，入口`tools/cab_reference/refine_linear_gauge.py`，真实凹窗、薄玻璃、旧漆PBR；撤销滑块机构假定，独立指示件仅作视觉占位。量程/单位/机构待核实，未接Unity，见[模块任务](dispatch/orders/CODEX-CAB-LINEAR-GAUGE-20261020.md)。

速度表Unity接入验证完成：`Assets/Resources/Cab/SpeedometerFinal.prefab`及`Assets/Scenes/CabSpeedometerReview.unity`已生成，批处理成功退出，31个Renderer，约0.18×0.19×0.12米；未接主驾驶台实际速度读数。下一件长条电气仪表单模块灰模已生成。

速度表Unity导入入口`Assets/Editor/CabSpeedometerAssetSetup.cs`，原生FBX/贴图在`Assets/Models/Cab/UnitySpeedometer`，目标预制体`Assets/Resources/Cab/SpeedometerFinal.prefab`，玻璃使用URP透明近似。下一个长条电气仪表已生成单只灰模`linear_gauge_photo_blockout.glb`，机构/范围未核实，见[模块任务](dispatch/orders/CODEX-CAB-LINEAR-GAUGE-20261020.md)。

速度表最终美术稿：`Assets/Models/Cab/speedometer_final.glb`，入口`tools/cab_reference/finalize_speedometer.py`；保留后仰/收分轮廓与金属旧漆材质，补装配细节、合并静态打印层，独立指针保留。背部细节为用户授权设计，尺寸/型号未考证，未接Unity，见[速度表任务](dispatch/orders/CODEX-CAB-SPEEDOMETER-20261019.md)。

速度表箱体轮廓候选：`Assets/Models/Cab/speedometer_tapered_refined.glb`，入口`tools/cab_reference/reshape_speedometer.py`，前面框总成小幅后仰、后壳收分、底脚收薄；7度为视觉估计，并非原车型号考证。新增侧视预览，未接Unity。见[速度表任务](dispatch/orders/CODEX-CAB-SPEEDOMETER-20261019.md)。

速度表去塑料感候选：`Assets/Models/Cab/speedometer_metal_refined.glb`，入口`tools/cab_reference/polish_speedometer.py`；区分旧漆壳/缎面金属板/发黑钢圈，收薄刻线与黄色中心，新增独立保护玻璃。透射材质未来需单独转换Unity，未接场景。见[速度表任务](dispatch/orders/CODEX-CAB-SPEEDOMETER-20261019.md)。

2026-10-20速度表细化：`Assets/Models/Cab/speedometer_photo_refined.glb`，入口`tools/cab_reference/refine_speedometer.py`，补旧金属前面板/螺钉/几何数字/单位与尖细指针，深灰绿哑光漆壳。量程与尺寸仍为原图视觉估计，同类Hasler资料只作对照；未接Unity。见[速度表任务](dispatch/orders/CODEX-CAB-SPEEDOMETER-20261019.md)。

Unity制动阀资源已导入：`Assets/Resources/Cab/BrakeValvesWithHandles.prefab`、`BrakeValvesWithoutHandles.prefab`及`Assets/Scenes/CabComponentReview.unity`，URP材质/原生FBX位于`Assets/Models/Cab/UnityBrake`；编辑器入口`Assets/Editor/CabBrakeAssetSetup.cs`。Unity执行成功，有柄57个Renderer，约0.19×0.22×0.24米；未接主驾驶台交互。下一组件`speedometer_photo_blockout.glb`首版外形已生成，见[速度表任务](dispatch/orders/CODEX-CAB-SPEEDOMETER-20261019.md)。

制动阀最终美术资产：`Assets/Models/Cab/brake_valves_final_without_handles.glb` / `brake_valves_final_with_handles.glb`，复现入口`tools/cab_reference/finalize_brake.py`。最终连接改为方形传动端/方孔轮毂、轴肩承托垫片、轴向螺钉及头下垫片；统一暖银灰哑光配色。作为游戏美术设计定稿，非真实制动机构工程认证，未接Unity。源、预览和限制见[任务记录](dispatch/orders/CODEX-CAB-BRAKE-20261019.md)。

制动阀原厂参考质感版：`Assets/Models/Cab/brake_valves_reference_without_handles.glb` / `brake_valves_reference_with_handles.glb`；入口`tools/cab_reference/polish_brake_reference.py`，对照H-6剖面图与S-6实物插图优化小闸握头/颈部，并区分哑光铸壳、加工银面、旧钢及黑色握柄。国外同类结构仅供比较，未确认原车型号，未接Unity，见[任务记录](dispatch/orders/CODEX-CAB-BRAKE-20261019.md)。

制动阀连接设计双版本：`Assets/Models/Cab/brake_valves_connection_without_handles.glb` / `brake_valves_connection_with_handles.glb`，入口`tools/cab_reference/engineer_brake_connection.py`。槽壁/双凸块传扭、轴向螺钉防脱，大/小闸把分形并分设展示角，短阀圆盘扩大24%；属于用户授权的模型设计，非实车型号复原，未做载荷/全行程或Unity验收。见[任务记录](dispatch/orders/CODEX-CAB-BRAKE-20261019.md)。

制动阀哑光双版本：`Assets/Models/Cab/brake_valves_without_handles.glb`（42网格/18084三角面）与`brake_valves_with_handles.glb`（48网格/22556三角面），各6张内嵌图，入口`tools/cab_reference/build_brake_variants.py`。上版圆孔/侧开槽已撤回；当前浅矩形接口和可拆闸把仍为未考证候选，未接Unity。中英文参考与边界见[任务记录](dispatch/orders/CODEX-CAB-BRAKE-20261019.md)。

制动阀卡槽材质版：`Assets/Models/Cab/brake_valves_photo_slotted.glb`，42网格、6张内嵌PBR图，外露轴新增真实切削凹座/卡槽/竖槽，材质改为粗糙银灰铸件和旧钢材；入口`tools/cab_reference/finish_brake_slots.py`。槽尺寸及背面槽数仍为照片估计，未接Unity，见[任务记录](dispatch/orders/CODEX-CAB-BRAKE-20261019.md)。

制动阀近照细化：`Assets/Models/Cab/brake_valves_photo_detailed.glb`，生成入口`tools/cab_reference/detail_brake.py`；按用户近照加宽壳体并补铸造筋/法兰凸耳，保留独立轴，制动手柄外形待核实。仍为候选，见[任务记录](dispatch/orders/CODEX-CAB-BRAKE-20261019.md)。

制动阀银灰候选：`Assets/Models/Cab/brake_valves_photo_refined.glb`，25网格/14888三角面、6张内嵌PBR图；修复两处装配空隙，生成入口`tools/cab_reference/refine_brake.py`。仍是照片估计，未定稿/未接Unity，见[制动阀任务](dispatch/orders/CODEX-CAB-BRAKE-20261019.md)。

制动阀首版灰模：`Assets/Models/Cab/brake_valves_photo_blockout.glb`，25网格/14888三角面，包含高低两组立式阀体与独立轴节点；生成入口`tools/cab_reference/build_brake_gray.py`，源与预览在`tools/cab_reference/brake_review/`。具体型号、尺寸与细部仍为待核实占位，未接Unity。见[制动阀任务](dispatch/orders/CODEX-CAB-BRAKE-20261019.md)。

下一组件制动阀：已通过`tools/cab_reference/inspect_brake.py`检查旧`jz7_brake_valve.glb`（单网格327864三角面），外形与原图左侧银色立式阀组不符，保留旧件；新模型尚未生成，型号待核对。见[制动阀任务](dispatch/orders/CODEX-CAB-BRAKE-20261019.md)。

控制器美术定稿：`Assets/Models/Cab/controller_handwheel_final.glb`，保留17网格/20628三角面/7个可动子件，增加克制磨亮、擦痕与边缘积灰，12张内嵌PBR图；复现入口`tools/cab_reference/finalize_controller.py`，源与预览位于`tools/cab_reference/controller_review/`。未接入Unity，三辐与工程尺寸仍未经考证。详见[控制器记录](dispatch/orders/CODEX-CAB-CONTROLLER-20261019.md)。

控制器黑漆候选：`Assets/Models/Cab/controller_handwheel_refined.glb`，17网格/20628三角面，12张内嵌PBR图；生成入口`tools/cab_reference/refine_controller.py`，保留分件旋转节点与原灰模，未接入Unity。形体尺寸和三辐仍为待考证估计，详见控制器建模记录。

控制器灰模第二版：暂保留三辐，将手轮旋转节点降低0.012米，轮毂减薄并缩小，同步调整固定轴与中心盖；尺寸仍为照片估计，交付路径不变，未接入Unity。详见下方控制器建模记录。

2026-10-19 驾驶台下一件：新增controller_handwheel_blockout.glb分件灰模（17网格、11412三角面），可动轮缘/轮毂/暂定三辐归Handwheel_Pivot，底板及轴座固定。原throttle_controller.glb外形不符，保留未替换；具体型号、辐条布局与行程待核实，未接入Unity交互。见[控制器建模记录](dispatch/orders/CODEX-CAB-CONTROLLER-20261019.md)。

> 每个脚本的职责、依赖关系、修改影响范围。供 AI 代理快速定位代码。

> **2026-10-18 静态核对**：`Assets/Scripts` 为 99 个 C# 文件。下图表示入口关系，新增核心模块见下表；存在调用链不等于运行验收通过。具体风险见 [审查回执](dispatch/orders/CODEX-AUDIT-20261018.md)。

| 模块 | 定位入口 | 当前接线 / 限制 |
|---|---|---|
| 世界生成 | `WorldGen/WorldGenerator.cs`、`WorldSeedData.cs`、`WorldInitializer.cs` | 生成/注入 API 已有；新游戏只写 seedCode，经营入口却读 seedId，正常路径需整合 |
| 区域解锁 | `WorldGen/RegionUnlockManager.cs` | `GameData.AdvanceDay` 调用 DailyCheck；剧情/科技门槛仍有 TODO，依赖种子初始化 |
| 模板叙事 | `Narrative/NarrativeEngine.cs`、`WorldAnalyzer.cs`、`StoryEvaluator.cs`、`TemplateFiller.cs`、`NarrativeRhythm.cs` | 每日结算末尾检查并分发；部分效果、VN 与临时订单消费尚未接入 |
| 政治/老化 | `GameData.cs`、`Systems/RegulatoryEventSystem.cs` | 连续量与外显离散评级分离；监管/检定/破产程序推进后保存全局状态 |
| 趋势看板 | `UI/TabletTrendDashboardUI.cs`、`UI/Tablet/Apps/TabletRDAAppUI.cs` | 已有趋势与定性评级展示；不可在政治/老化 UI 添加读数 |
| 人事 | `CrewManager.cs`、`Core/FluctuationEngine.cs` | 工资先计算，随后 CrewManager.AdvanceDay 更新成长/疲劳；维护力量也供老化演化读取 |
| 订单 | `OrderManager.cs` | CompleteOrder 累计 pendingDailyIncome；DailySettle 返回并清零，GameData 统一入账 |
| 登录服务 | `Core/Auth/UserAuthService.cs`、`LoginManager.cs` | 独立认证服务已存在，但大型登录 UI 类仍未完成拆分 |
| 驾驶台 | `Cab/CabConsoleUI.cs`、`CabEngineState.cs`、`Cab3DSceneController.cs`、`CabPalette.cs` | 已有状态机、UI 和 3D 控制；当前工作区存在未提交修改，本轮未改动 |

2026-10-19 模型补充：旧 `Assets/Models/Cab/piano_switches_8gang.glb` 实为10键。当前十键候选为凸形实心块体（视觉估计底长0.0255、宽0.0064、高0.0107），上缘前后各内收0.0025、棱线倒角0.00030/5段；整体仍±22°，端面收口独立调整，安装轴下移后高点约0.0066。压框内侧采用与端面匹配的退让斜面，外轮廓覆盖完整桌面安装孔。逐三角形校验带倒角键体与膨胀0.00015的真实凸压框实体不相交，输出 frame_clearance_check.json；仅验证当前两种摆放，未验收中间动画行程。结构/参数仍为照片对照假设。另有 alignment_check.json、正面/俯视/侧视检查图及final_material_reference.png参考视角预览。未替换旧模型、未接入运行场景。任务与交付见 [琴键建模记录](dispatch/orders/CODEX-CAB-SWITCHES-20261019.md)。

主结算顺序：收入/订单/成本/补贴 → 人事日更 → 净收入及策略效果 → 老化/政治连续量 → 随机事件/沙能 → 月账与日快照 → Day++ / 区域检查 / 教程 → RefreshBoards → 监管及破产程序 → 模板叙事。后置效果可能未进入先前快照与日变化，修复前不能宣称所有口径一致。

---

## 一、类依赖图

```
LoginManager ──────────→ TitleScreen ──────────→ NewGameSetupUI
  │                       │ 依赖:                   │ 依赖:
  │ 依赖:                  │  UIDocument            │  UIDocument
  │  Resources/*.png      │  VideoPlayer           │  GameConfig
  │  PlayerPrefs          │  Resources/UI/         │
  │  auth.json            │  LoginManager          │
  │                       │  NewGameSetupUI        │
  │                       │  TitleArchiveUI        │
  │                       ▼                        │
  │                  VN_Test (VNManager)            │
  │                    │ 依赖:                      │
  │                    │  JSONParser                │
  │                    │  DialogueBox               │
  │                    │  BackgroundManager         │
  │                    │  CharacterSpriteManager    │
  │                    │  VNBacklog                 │
  │                    │  VNSaveSystem              │
  │                    │  VNSaveLoadUI              │
  │                    │  FullScreenNews            │
  │                    │  VNAudioManager            │
  │                    │  GameConfig                │
  │                    │                            │
  │                    ▼                            │
  │              StationSlice_V1                     │
  │              [RuntimeInitializeOnLoadMethod]    │
  │              RailRevivalRuntimeBootstrap        │
  │                    │ 构建 UI 布局               │
  │                    ▼                            │
  │              UIManager ───── GameData ──── EventManager
  │                 │            │                  │ 依赖:
  │                 │            │ 依赖:             │  Resources/events.json
  │                 │            │  GameConfig      │
  │                 │            │  EventManager    │
  │                 │            │  SandRivalManager│
  │                 │            │  TutorialManager │
  │                 │            │                  │
  │                 ├── ButtonController           │
  │                 ├── VisualAssetBinder          │
  │                 ├── AudioManager               │
  │                 │                              │
  │                 ▼                              ▼
  │          OrderManager ──── CrewManager ──── SandRivalManager
  │             │                 │                  │
  │             │ 依赖:           │ 依赖:             │ 依赖: 无
  │             │  Resources/    │  Resources/       │ (纯数据)
  │             │  orders.json   │  (无外部资源)      │
  │             │  GameData      │                   │
  │             │                 │                   │
  │             ▼                 ▼                   ▼
  │          TutorialManager   AchievementManager
  │             │                 │
  │             │ 依赖:           │ 依赖:
  │             │  GameData       │  GameData
  │             │  AudioManager   │
  └─────────────┴─────────────────┘
```

---

## 二、静态类 vs MonoBehaviours

### 静态类（无 GameObject，全局访问）
| 类 | 访问方式 | 初始化时机 |
|----|----------|-----------|
| `GameData` | `GameData.XXX` | AdvanceDay 调 InitializeIfNeeded；`ResetState()` 清空监管在途持久状态、按当前种子重应用趋势初值并同步首日快照/全局兼容键；VN 入口 ApplyVNExitData 调 InitializeSubsystems 并标记 initialized |
| `CrewManager` | `CrewManager.XXX` | GameData.InitializeSubsystems 调 Initialize |
| `EventManager` | `EventManager.XXX` | `GameData.InitializeIfNeeded()` 中调用 |
| `SandRivalManager` | `SandRivalManager.XXX` | 同上 |
| `OrderManager` | `OrderManager.XXX` | GameData.InitializeSubsystems 调 Initialize；每日由 DailySettle 推进 |
| `Narrative.NarrativeEngine` | `NarrativeEngine.XXX` | CheckForEvent 懒初始化；新局清理完整性待核验 |
| `RegulatoryEventSystem` | 完整命名空间或 using | `GameData.ResetState()` 调 `RegulatoryEventSystem.ResetState()` 清除旧在途程序及旧键；`RestoreFromSave` 仍调 `Initialize()` 读取当前全局持久化状态；不等于槽位完整恢复 |

### 单例
| 类 | Instance | 创建时机 |
|----|----------|----------|
| `VNManager` | `VNManager.Instance` | VN_Test 场景中 Awake() |
| `VNAudioManager` | `VNAudioManager.Instance` | VNManager.Awake() 创建 |
| `AudioManager` | `AudioManager.Instance` | Bootstrap 创建 |
| `MusicCloudSyncEngine` | `MusicCloudSyncEngine.Instance` | 随身听多源聚合与云端流媒体服务（单例） |
| `TutorialManager` | `TutorialManager.Instance` | 场景中挂载 |

---

## 三、关键修改路径

### 3.1 要改经济系统参数
```
GameData.cs (常量)           ← 直接改
  ↓
GameConfig.cs (PlayerPrefs)  ← 运行时改
```

初始趋势必须走创世核种子；修改结算入口还需核对 OrderManager、人事日更、监管程序、叙事后置效果和槽位存档。公式与已知差异见 `DAILY_SETTLEMENT_FORMULAS.md` §七，不能据上述常量入口绕过种子初值规则。

### 3.2 要加新的 VN 剧本
```
1. 创建 Resources/Scripts/prologue_XX_xxx.json
2. 在上一本最后一句改 nextScript 指向新剧本
3. 或修改 VNManager.Start() 中的默认启动剧本
```

### 3.3 要加新的随机事件
```
1. 编辑 Resources/events.json
2. 按格式添加事件模板
3. EventManager 自动加载
```

### 3.4 要加新的订单类型
```
1. 编辑 Resources/orders.json
2. 按格式添加订单模板
3. OrderManager 自动加载
```

### 3.5 要改 UI 布局
```
StationSlice_V1 → RailRevivalRuntimeBootstrap.cs (560行)
  → LayoutRootPanels() 控制面板位置
  → EnsureUi() 控制所有 UI 元素创建
  → 所有 UI 用代码创建（没有预制体）
```

### 3.6 要改 VN 对话框与术语系统 (DialogueBox.cs & Terms.json)
```
VN/DialogueBox.cs (1,061行) — BuildUI()、ShowDialogue()、TermClickSystem
  → 术语悬浮交互：默认不交互时不显示下划线，鼠标悬停时基线动态高亮下划线，移出消失
  → 术语弹出卡片（Bottom Dock）：正下方展开、单人词典释义+角色头像框展示，弹性盒排版防御（防图鉴按钮被挤飞）
  → 术语词库：Resources/Terms.json（全篇 65 条核心工业术语，覆盖动力总成、机车空气制动、工务保线等，346处高亮）
  → 样式：Resources/UI/VN/DialogueBox.uss
```

### 3.7 要改 VN 对话回顾与铁道滚动条 (VNBacklog.cs)
```
VN/VNBacklog.cs (1,959行)
  → 有色半透明背景：MakeBacklogWashiTex() 128×128 双线性平铺，包含造纸横向经纬丝、32px菱形方格暗纹、悬浮金箔/沙能微斑；透光度 65%，底层场景/立绘清晰可见
  → 顶部渐隐与磨砂：MakeBacklogFadeMaskTex() 与 Resources/UI/frosted.uss，暖沙褐过渡
  → 第一阶段（驻留景）：道砟平齐轨顶(top=0)防遮挡、列车加长至104px、完备段(200px)内自由拖拽并就地停驻（Park）、无反弹拖影
  → 第二阶段（正常滚动）：SbThumbHeight() 上下文自适应弹性缩放（最高达轨长70%）、拦截原生大刻度步进、16ms动量指数阻尼平滑滑行(* 0.88f)
  → 条目排版：暖木纹姓名横向缎带、高反差象牙白台词、柔和缩进旁白、纤细暖金分割线、黑底金边圆角「跳转」药丸按钮
```

### 3.8 要改标题界面
```
TitleScreen.cs — 使用 UI Toolkit
  → 布局在 Resources/UI/TitleScreen.uxml
  → 样式在 Resources/UI/TitleScreenStyles.uss
  → PanelSettings 在 Resources/UI/TitleScreenPanelSettings.asset
```

### 3.9 平板系统架构 (Tablet UI & Apps)

2026-10-19 核对：下列播放器特性描述仅表示入口或代码存在。云端 Apple/SoundCloud 存在固定示例返回，网易云有模拟歌单兜底，B站有虚构凭据兜底；不能将其视为真实平台功能已验收。音效时间由 GameMainUI.Update 与窗口定时器重复推进的风险待修。详情见 [近期任务复核](dispatch/reports/20261019_codex_recent_tasks_review.md)。
```
GameMainUI.cs (Tablet OS Shell, ~2,000行)
  ├── BuildFullScreenAppContainer: 彻底消除白色“狗皮膏药”，全面升级为 Folia-Major 暗黑视听沉浸容器（深墨黑 #14171C、亚克力毛玻璃顶栏、细黄铜金线高光、实体工业暗色微交互胶囊），所有平板 App 告别刺眼白框
  ├── BuildHudMusicCapsule: Folia 风格随身听 Mini 胶囊（位于状态栏时间右侧，未播放自动隐藏、仅播放中显现；微型黑胶旋转 + 曲名 + 极简播控）
  ├── TabletUIHelper.cs (色系定义/字体定义/统一样式卡片骨架)
  └── Assets/Scripts/UI/Tablet/Apps/
        ├── TabletReturnJourneyUI.cs   (归途：主线剧情时间轴/全幕回溯)
        ├── TabletMusicAppUI.cs        (随身听主壳：全屏沉浸视听舞台 + Spotify 架构 + Folia-Major 视听动效 · 单一明确状态机 s_currentView 路由分流彻底终结串台错位；内置 26 轨真实时长映射表杜绝写死 02:50；收藏视图采用复古车票空状态；私人磁带夹黄铜虚线框与Win32文件快捷导入；未解锁项绑定轻微抖动 Shake 与信标电报 Toast)
        ├── MusicSidebarNavView.cs     (随身听左侧导航栏：统一状态枚举 MusicViewId[Discover/Ost/Cloud/Favorites/LocalTape/RadioAuto/Scenery/Ambient]，上下半区播单均纳入单一高亮状态管理，左侧3px黄铜金光标、底部 FM-88 铁道铭牌)
        ├── MusicPlayerBarView.cs      (随身听专业三段式底部控制台：SDF亚像素抗锯齿无边框通透播控5键[纯矢量平滑渲染消灭方块错位]；细黄铜细轨+8px圆珠音量条[彻底去除粗白十字滑块]；百分比对齐进度轨；UpdateControlsState零GC局部复用更新)
        ├── MusicAlbumArtResolver.cs   (16-bit 像素实体工业复古封套与环境光色板解析：消灭塑料黄同心圆灾难、全量 26 首曲目专属世界观图元生成[站台/机车/云海/暴雪/沙能等]、左上角工务调频钢印与右下角黑胶防伪金箔印记、Folia风格自适应背景氛围光晕色彩提取)
        ├── MusicNowPlayingSidebarView.cs (随身听右侧视窗：140x140像素实体封套内嵌16段复古琥珀LED实时频谱条、flexGrow自适应歌词视窗、加粗高亮金黄歌词消除挤压粘连、标题右侧⛶展开全屏歌词按键、下一首队列预览卡片)
        ├── MusicAudioEffectsEngine.cs (随身听声音工程与调频滤镜引擎：Folia 平滑交叉淡入淡出 Crossfade + Wow & Flutter 磁带音调物理微晃 + 荒原电台调频质感拨钮[原声/机车/先民] + 停站自熄睡眠定时器)
        ├── MusicPonderArchiveView.cs  (随身听 Ponder 深度叙事视窗：借鉴 Folia PonderFocusFrame，章节关键帧时间轴 Chapter Timeline + 先民工务局绝密留声档案下钻 + 点击节点跳播)
        ├── MusicVinylShelfView.cs     (随身听黑胶展架模式：借鉴 Folia Vinyl Wall，26 首原声化作工务调度室展架上的实体唱片盒，液态玻璃次表面晶体折射，未解锁精密机械挂锁徽章与已解锁金章)
        ├── MusicIconHelper.cs         (纯代码矢量与16-bit工业像素组件库：SDF抗锯齿矢量生成器[GetShuffleTexture/GetRepeatTexture/GetTrackNavTexture]、深炭黑工业内嵌输入框[StyleIndustrialTextField]、微呼吸LED状态指示灯[CreateLedIndicatorLamp]、紧凑胶囊徽章[CreateCompactPillBadge]、物理轻抖反馈[TriggerShakeFeedback]、Folia级液态玻璃卡片体系与真空管VU表盘)
        ├── LocalMusicFileImporter.cs  (本地音频一键多选导入桥接器：呼出Win32原生文件对话框并自动拷贝至CustomMusic)
        ├── MusicCloudSyncEngine.cs    (云端多源对齐引擎：全量支持Folia 4大阵营9大平台/协议[网易云扫码、B站音频、Spotify开放生态、QQ音乐绿钻、酷狗音乐KRC歌词、波点音乐极简流媒体、YouTube Music全球Remix、Navidrome/Subsonic私人NAS无损云、AMLL社区逐字歌词引擎及万能直链穿透]，所有平台表单全面升级为深炭黑工业风+琥珀金焦点反馈)
        ├── SimpleQrCodeGenerator.cs   (纯C#轻量级调频二维码矩阵生成器：脱机高对比度渲染 + 在线高清通道兜底)
        ├── TabletGalleryAppUI.cs      (相册：回忆画廊/全屏大图灯箱)
        ├── TabletSettingsAppUI.cs     (设置：24截图分类/TIPS图鉴/按键映射/开发者)
        ├── TabletRDAAppUI.cs          (RDA：调度助手/线路车况/T304五线态势联动)
        ├── TabletNotesAppUI.cs        (百科与便签：媒姬百科/三栏自动化备忘)
        └── TabletSystemToolsAppUI.cs  (系统工具集：时钟/日历/天气/计算器/米家/安全等)
```

---

## 四、资源引用关系

### 4.1 Resources（运行时加载）
```
路径                              被谁加载
Resources/Scripts/*.json          JSONParser → VNManager
Resources/events.json             EventManager
Resources/orders.json             OrderManager
Resources/bg/*.png                BackgroundManager
Resources/characters/*.png        CharacterSpriteManager
Resources/bgm/*.ogg               VNAudioManager
Resources/sfx/*.ogg               VNAudioManager
Resources/Fonts/zpix              LoginManager, VNManager, DialogueBox 等
Resources/UI/Login/*.png          LoginManager
Resources/UI/TitleScreen*         TitleScreen
Resources/UI/VN/DialogueBox.uss   VNManager
Resources/Cursors/*.png           LoginManager, VNManager
```

### 4.2 PlayerPrefs 键值
```
Key                             类型          用途
VN_Save_{0-2}                   JSON string   VN 存档
SaveSlot_{0-2}                  JSON string   经营存档
RailGameConfig                  JSON string   游戏配置
VNExitData                      JSON string   VN→经营过渡
VN_AutoLoad                     int           自动加载标记
Username                        string        用户名
```

---

## 五、各场景必须的组件

### Login.unity
- `LoginManager` (脚本)
- 自动创建：Canvas, EventSystem, Camera, AudioSource

### TitleScreen.unity
- `UIDocument` (组件)
- `TitleScreen` (脚本)
- `VideoPlayer` (可选，视频背景)
- 自动创建：Camera, EventSystem, BGM AudioSource

### VN_Test.unity
- `VNManager` (脚本)
- `TutorialManager` (脚本，可选)
- 自动创建：Canvas (UI Toolkit), EventSystem, VNAudioManager

### StationSlice_V1.unity
- 无必需组件（Bootstrap 自动创建一切）
- 可选：`TrainPlaceholder_Legacy` / `TrainCandidate_01` / `Train`, `Ground` (Tilemap)

---

## 六、存档/读档路径

### VN 存档
```
VNManager 菜单 → 存档 → VNSaveLoadUI.OpenSavePanel()
  → VNSaveSystem.SaveGame(slot, scriptName, sceneIndex, dialogueIndex, bgName, bgmName)
  → PlayerPrefs.SetString("VN_Save_" + slot, JsonUtility.ToJson(data))
  → PlayerPrefs.Save()
```

### VN 读档
```
VNManager 菜单 → 读档 → VNSaveLoadUI.OpenLoadPanel()
  → VNSaveSystem.LoadGame(slot)
  → VNManager.LoadFromSave(data)
  → 恢复场景/背景/BGM/对话位置
```

### 自动加载（"继续运营"）
```
TitleScreen.OnContinue()
  → PlayerPrefs.SetInt("VN_AutoLoad", 1)
  → SceneManager.LoadScene("VN_Test")
  → VNManager.Start() 检测 VN_AutoLoad=1
  → LoadLatestSave() → 加载最近存档
```

### 经营存档
```
VNSaveSystem.SaveGameplayData(slotIndex) — 保存 Money/Trust/TrainCondition/Passengers/Day
VNSaveSystem.LoadGameplayData(slotIndex) — 恢复上述数据
```

**当前限制**：槽位 JSON 实际仅有 money、trust、trainCondition、expectedPassengers、day、carCount 六项。政治/老化与监管程序从全局 PlayerPrefs 恢复，种子、员工、订单待收入、叙事冷却及趋势历史未由该槽位保存。读取旧槽位可能混入当前全局状态；RestoreFromSave 未建立完整子系统/初始化状态。不要把上述方法描述为「完整世界存档」。

---

## 七、编辑器脚本

### 编辑器脚本（Assets/Editor/）
| 脚本 | 用途 |
|------|------|
| `FixSpriteImports.cs` | 修复精灵导入设置 |
| `SpriteImportFixer.cs` | 精灵导入修复 |
| `VNFinalSetup.cs` | VN 场景最终设置 |
| `TitleScreenSetup.cs` | 标题界面场景设置 |
| `CloudSeaSetup.cs` | 云海场景设置 |
| `RailRevivalMvpSetup.cs` | MVP 场景设置 |

---

## 八、常见问题排查

### 8.1 编译错误
```
Unity Editor 日志: C:\Users\Oe_Lee\AppData\Local\Unity\Editor\Editor.log
命令行验证: Unity.exe -quit -batchmode -projectPath "D:\Unity Project\RailwayRenaissance"
```

### 8.2 VN 剧本不显示
- 检查 `Resources/Scripts/{name}.json` 是否存在
- 检查 JSON 格式是否匹配 `ScriptData` 结构
- 检查 `VNManager.Start()` 中启动的剧本名

### 8.3 经营场景不显示 UI
- `RailRevivalRuntimeBootstrap` 需 `[RuntimeInitializeOnLoadMethod]`
- 检查 `StationSlice_V1` 是否在 Build Settings 中
- 检查 `Resources/UI/TitleScreenPanelSettings.asset` 是否存在

### 8.4 资源加载失败
- 所有资源必须放在 `Resources/` 目录下
- 路径名需匹配（`Resources/bg/lab` → `bg/lab` 无扩展名）
- 资源类型需匹配（`Resources.Load<Texture2D>` 读图片，`Resources.Load<AudioClip>` 读音频）

---

*本文档由 AI 代理于 2026-08-14 自动生成，修改代码后请同步更新。*
2026-10-20 最新接线：`CabDieselTachometer` 读取显式绑定的 `CabEngineState.EngineRpm`，`CabTachometerAssetSetup.BatchBuild` 生成 `DieselTachometerFinal.prefab` / `CabTachometerReview.unity`。301 点、异常输入、发动机启停与超量程通过；表显上限1500 r/min，保留原始输入和超量程标记，不改发动机物理。下一件 `build_temperature_gauges.py` 输出水温/油温双模块首版，油温独立信号缺失，尚未绑定。详见对应组件任务。



### 2026-10-21 ????????
???????????????????????????????????????6mm???????????????????NF-5K?????????`CabInteriorBlockoutSetup.Shot`?4200?3000????`NF5KReviewDownsample.shader`??????1400?1000?????????????????????SSAA??????????????????


### 2026-10-21 NF-5K????
`NF5KLivery.Text`??????????????????????????????5?????????125mm????????????????????????????????????????????????????????


### 2026-10-21 NF-5K exterior lighting
NF5KIndustrialFinish calls NF5KFrontLightingSetup, which binds CabExteriorLighting to CabEngineState. NF5KLightingReview verifies dark renders and NF5KLightingPlayReview verifies real Play Mode switching and cleanup. See [lighting specification](NF5K_EXTERIOR_LIGHTING.md).


### 2026-10-21 NF-5K front mechanical fittings
NF5KReferenceBody.BuildNoseDetails calls NF5KFrontMechanical.Build for cast coupler parts, support/draft-sill geometry, parked brake hose and skirt details. ValidateReload checks persistence and nominal hose clearance. See [mechanical specification and planned interior lighting](NF5K_FRONT_MECHANICAL.md).


### 2026-10-21 NF-5K interior lighting
CabInteriorLighting and NF5KInteriorLightingSetup add independent cab ceiling/task, dimmable instrument, two passenger zones and automatic emergency transfer from explicit normal/backup supply inputs. NF5KInteriorLightingReview checks renders and NF5KLightingPlayReview checks runtime. See [interior lighting specification](NF5K_INTERIOR_LIGHTING.md). Passenger interior remains a blockout; physical glass reflections and battery simulation are unverified.


### 2026-10-21 Cab lighting real-reference revision
Vossloh DE18 documents two ceiling lights, dimmable instruments/reading lights and access lighting. NF-5K now has 18 interior fixtures: two cab ceiling, independent driver/assistant task lights, two access footlights, two instrument lights, six passenger and four emergency. Normal, night and service views are rendered separately. New APIs: SetAssistantTask and SetAccess. See NF5K_INTERIOR_LIGHTING.md.


### 2026-10-21 Passenger interior fittings
NF5KPassengerInterior replaces passenger seat/wall blocks with 48 individual seats, supported luggage racks, framed inner windows, heater covers and vestibule details. Native rounded meshes and woven texture are persisted/shared. ValidateReload checks candidate aisle and reserved zones; NF5KDoorReview now includes new seat/rack colliders. See NF5K_PASSENGER_INTERIOR.md for references and unverified certification/interaction limits.
# NF5K lighting audit (2026-10-21)

`CabDiffuseFill` supplies smooth circuit-dependent diffuse SH to opaque interior receivers; approximation point-light bindings are culled from rendering. `CabInteriorReflections.RefreshIfChanged` refreshes diffuse coefficients before capturing reflection probes. See the audit for approximation limits.

`Assets/Scripts/Cab/CabInteriorReflections.cs` refreshes four local reflection probes on lighting-signature changes. `NF5KLightingRendererSetup` creates the probes; review capture refreshes them before screenshots. Reflection improves environmental specular response and does not implement diffuse GI.

`NF5KLightingRendererSetup.ConfigureSurfaces` separates approximate bounce from transparent surfaces through rendering layers and installs an isolated Neutral tone-map Volume. Review screenshots use HDR intermediate render targets before SDR PNG conversion.

Follow-up: renderer setup enables shared additional-light soft shadows; `CabInteriorLighting.Fixture.lowerBounce` binds floor-reflection fill to the same circuit and power/disable lifecycle as its parent fixture.

`Assets/Editor/NF5KLightingRendererSetup.cs` installs the camera-specific Forward+ renderer during `CabInteriorBlockoutSetup.Build`. See [NF5K_LIGHTING_AUDIT.md](NF5K_LIGHTING_AUDIT.md) for measured-render checks, reference sources and unresolved shadow/Gamma/GI limitations.
# Passenger seat clearance follow-up (2026-10-21)

`NF5KPassengerInterior.ValidateReload` now checks seat/wall-envelope clearance, floor grounding and seat intersections with window sills, lower liners and heater covers, in addition to aisle/vestibule checks. See `NF5K_PASSENGER_INTERIOR.md` for corrected dimensions.
# NF5K daylight and proportion review (2026-10-21)

`NF5KDaylightReview` measures reloaded geometry, generates `CabDaylightReview.unity` and captures three daylight lamp-off/on pairs. Runtime `CabDaylightEnvironment` provides the same presets in Chinese. See `NF5K_DAYLIGHT_REVIEW.md` for scene usage and approximation boundaries.
# NF5K cab fittings (2026-10-22)

Gauge/manual follow-up: `NF5KExtinguisherModel` adds a continuous-zone printed pressure dial, tapered pointer, rolled bezel and convex lens; `BuildManual` builds an adjacent readable manual in a fixed pocket. `CabSafetyEquipment.IsManual` exposes only reading, while the three removable items retain their existing path. Catalog and labels now use full Chinese/English pairs without the parenthetical classification label. See [NF5K_FIRE_MANUAL_AND_GAUGE.md](NF5K_FIRE_MANUAL_AND_GAUGE.md).

Formal equipment dossier: `CabSafetyCatalog` loads shared `safety_equipment_profile.json`; Python label generation reads the same manufacturer/model/specification fields. `CabContextInteraction` presents a scrollable modal dossier with Escape/close and background-input isolation, checked by `NF5KContextInteractionReview`. The fictional manufacturer is 白岳消防器材厂 and the product is 守安 BYF-23R; development limitations stay in documentation.

Extinguisher rebuild: `NF5KExtinguisherModel` replaces capsule bodies with a drawn-cylinder profile, separate valve/flat handles/gauge/pin/seal and a curved discharge hose. `build_extinguisher_label.py` produces a complete Chinese reference label mapped to a curved mesh. `CabSafetyEquipment` now opens/closes retaining bands and orients held bottles toward the player. See [NF5K_EXTINGUISHER_REFERENCE.md](NF5K_EXTINGUISHER_REFERENCE.md).

Safety follow-up: `CabSafetyEquipment` supplies inspection pickup/return for two extinguishers and one hammer via existing `CabContextInteraction`. `NF5KSafetyEquipmentReview` validates dock rays, menu choices, near-dock return and single-item/disable guards. The fittings generator adds Chinese identification plates and mount details. See [NF5K_SAFETY_INTERACTION.md](NF5K_SAFETY_INTERACTION.md); spraying and window breakage remain unimplemented.

Refinement: dedicated satin enamel/paper/safety materials, a 105 mm parking grip, mount/cover/legend details and proportioned clipboard. The same generator adds front/rear passenger fire-extinguisher models, front passage grab rails and a rear emergency-hammer mounting panel. Safety equipment remains static. Selected seat-envelope checks and parking mesh-ray/moving-command regressions are included; see the specification below.

`CabWorldLabel` binds dynamic font atlases to a depth-tested URP `CabWorldLabel.shader` through property blocks; font rebuilds refresh the binding, and physical labels are occluded by opaque cab walls.

`NF5KCabFittings` adds a driver-side parking selector and auxiliary shelf/radio/handset/map pockets in four whole-car review scenes. `CabDrivingTarget.ParkingBrake` feeds the existing simulation driver with stopped/zero-throttle guards; `CabContextInteraction` exposes held apply/release choices. Auxiliary equipment remains visual only. See [NF5K_CAB_FITTINGS.md](NF5K_CAB_FITTINGS.md).

# NF5K pneumatic equipment (2026-10-22)

`NF5KPneumaticEquipment.Apply` generates reservoirs, dryer/manifold, separate MR/BP/BC visual piping and eight sanding assemblies in all four review scenes. `CabFlexibleHose` follows body/bogie ports after `CabRailMotion`; `NF5KSimulationPlayReview` checks moved endpoints. This is visual equipment, without new pressure or sanding simulation. See [NF5K_PNEUMATIC_EQUIPMENT.md](NF5K_PNEUMATIC_EQUIPMENT.md).

# NF5K simulation backend (2026-10-21)

`CabSimulation.cs` is the pure 50Hz diesel-electric/air-brake/longitudinal kernel; `CabSimulationCodec.cs` preserves exact checkpoint scalars. `CabSimulationDriver` is an opt-in writer through `CabEngineState.UpdatePhysics`, projecting to existing gauges and lighting supply inputs. `NF5KSimulationReview` generates `CabSimulationReview.unity` and validates the kernel plus actual Play Mode. Legacy scenes keep the old physics path. See `NF5K_SIMULATION_BACKEND.md` for formulas, sources, candidate parameters and unimplemented systems.
# 2026-10-23 消防器材在役细节

下一批：`NF5KWallEquipment` 在同一补件入口生成后壁检修面板细节和客舱前隔板急救柜；`CabStorageCabinet` 提供就近开合与停用归位，`CabContextInteraction` 注册柜体和门片碰撞体并复用用户按键。`NF5KSafetyEquipmentReview` 覆盖真实门片射线、命令、角度、距离和恢复。见 [后壁设备](NF5K_WALL_EQUIPMENT.md)。

表盘 V5 将顶部正常状态文字居中；`NF5KExtinguisherModel` 生成连续环销、短连接颈及细闭合封签环和小锁头，替换原来的分段拉环与大吊牌。审阅图后缀 `_seal_aligned.png`。

表头/握把修订：`NF5KExtinguisherModel.BentHandle` 生成带斜接转角的连续折弯条截面，持握柄与压柄分别保留；阀体补双侧铰座和压杆。`build_extinguisher_label.py` 输出 V4 大字表盘，文字避开指针路径。

`NF5KExtinguisherModel.Build` 接受固定指针偏角；生成表头背壳、轴、手册底托及纸页细节。`build_extinguisher_label.py` 生成固定随机种子的磨损封面并跳过相同 PNG 写入。`NF5KSafetyEquipmentReview` 验证书册接触与表针范围；装配仍为静态安装加脚本取放，非结构强度求解。

2026-10-24 音乐：MusicLyricsView 增加 lyricMotion 三种表现，真实词时间驱动舞台浮起/辉光；MusicTerminal.uss 隔离水平 Slider，修正竖向胶囊短轨越界；RailwayMusicTerminalReview 检查滚动条几何与模式存储。详见 MUSIC_FOLIA_CORE_REVIEW.md。

音乐最终验收补记：RailwayMusicTerminalReview增加首/中/尾胶囊边界、三种歌词模式、真实词浮起与减少动态覆盖、跨进程lyricMotion恢复检查；MusicLyricsView限定纵向滚动，修正舞台模式小窗口白色横向条。music_motion_verified/restore均MUSIC_REVIEW_OK、退出0。

2026-10-24 原声证据工具：tools/music/align_ost_lyrics.py新增--asr-only；compare_ost_asr.py输出原稿逐行与独立ASR匹配疑点；summarize_alignment_review.py汇总历史候选。均不自动发布字幕、不等同听校。

2026-10-24 原声原稿纠正：check_ost_windows.py提供重叠区间独立ASR；align_ost_lyrics.py拒绝已撤回输入、记录显式演唱稿增删。南风44行原稿保持不变；自动漏识别不能支持删词，详情见MUSIC_FOLIA_CORE_REVIEW.md。

2026-10-24 音乐交互升级：RailwayMusicTerminalView增加清空搜索、带数量队列、窄窗抽屉遮罩/Esc、焦点返回与入场；MusicLyricsView增加专注舞台、句时间进度和音频时钟背景，USS热区40/42/52。TabletMusicAppUI.TryGetCurrentPosition使MusicLibrarySession.Save避免音源销毁后将进度写0。完整工作树受其他Calculator验收缺类影响；隔离快照完整音乐与跨进程检查通过。详见MUSIC_TERMINAL.md。

2026-10-24原声原稿追溯：recover_ost_prompt_source.py从固定Git版本恢复历史AI配乐文档和四首阅读正文，保留ID3；extract_ost_lyric_sheets.py避免重提取覆盖原稿；align_ost_lyrics.py --variant隔离来源候选。音乐阅读页39/47/49行在隔离快照验收通过，时间轴仍未全部听校。

2026-10-24音频律动舞台：MusicReactiveStageView独立绘制每曲渐变光场与曲线，封面取色缓存+歌曲稳定种子/艺术配色；真实输出RMS或当前解压PCM窗驱动脉冲，音频时间驱动流动，支持暂停/减少动态，30Hz上限。MusicLyricsView复用背景实现切歌配色过渡。隔离真实入口MUSIC_REACTIVE_STAGE_OK通过，产物music_reactive_stage_review。

2026-10-24歌词校时工具：recheck_ost_alignment.py做双模型上下文局部对齐；separate_ost_vocals.py生成保持时间的本地人声参考；download_vocal_model.py验证官方模型包；build_calibration_packet.py保留原稿并生成疑点原曲片段/复核报告。align_ost_lyrics.py支持带原曲哈希与0偏移元数据的--audio变体，保留原ASR。未修改运行时。

2026-10-25：NF5KExteriorLightControlSetup同步隐藏旧Auxiliary_Plate_Title，并将Speedometer_Stand替换为开敞双腹板金属支架；见NF5K_EXTERIOR_CONTROL_PANEL.md。

2026-10-25音乐：align_ost_phonemes.py新增MMS_FA音素对齐、跨停顿修正与可追溯ASR约束；publish_phonetic_draft.py严格检查后打包南风39句LRC草稿及不可变证据哈希。MusicLyricsView专注标题保留待听校，RailwayMusicTerminalReview验实际原曲尾句点选与高亮。南风隔离运行及跨进程通过；其余新候选仍被门槛拒绝。

2026-10-25：十格状态/报警灯窗由NF5KAnnunciatorSetup替换四颗未接线圆灯；CabAnnunciatorPanel只读详细模拟，包含供电、启动低油压延时、压力/温度迟滞及中英印刷。来源、状态定义和验证见docs/for-ai/NF5K_ANNUNCIATOR.md。


2026-10-25 歌词桥段联合校准复核：千里马最新候选仍有3项未通过（句17/42/44），星光仍有弱发音和哼唱时间疑点，均未发布；新增带原曲时间及哈希的疑点试听包。南风39句及沉睡者35行继续标注自动草稿待听校。工具、实测及外部查询授权边界见[MUSIC_FOLIA_CORE_REVIEW](MUSIC_FOLIA_CORE_REVIEW.md)。


2026-10-25 获准仅发送两首ID后完成Suno匿名来源查询：公开歌词与星光47/千里马49行提词原稿一致；v2/v3时间接口均401，未取得时间轴。离线证据工具audit_suno_public_evidence.py及完整限制见docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md；未改Assets字幕或在线接入。
2026-10-25 灯窗材质v2：独立磨砂光学面、线性扩散贴图、微法线与深色遮字；光学贴图用CabAnnunciatorOpticalMap标记避免被Sprite规则覆盖，URP发光变体跨进程持久化及驾驶回归通过。见NF5K_ANNUNCIATOR.md。


2026-10-25 本地歌词审阅工具修复空时间当0导致的校验漏洞，并补输入标签/播放错误反馈；语法与校验fixture通过，浏览器实际试听因本机访问受阻仍未验收。见MUSIC_FOLIA_CORE_REVIEW.md。


2026-10-25 本地歌词继续复核：新增弱主歌/副歌整段候选及带来源哈希的人声窗口识别。千里马两版仍3项未通过，分离人声识别失败证据保留，未改正式字幕或放宽发布门槛。详见docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md。


2026-10-25 本地校准新增边界RMS图（6个争议句，非时间真值）和带语言/设备追溯的独立窗口识别；medium续传中尚无推理结果，CUDA缺库已保留失败证据。见MUSIC_FOLIA_CORE_REVIEW.md。
2026-10-25：CabLampTestButton为灯窗右侧实体试灯按钮，复用主交互绑定，点按触发2秒十格检查及短行程弹回；断电终止、超时恢复真实指示，未改保护或驾驶指令。四场景、实际射线/上下文、供电/禁用守卫和checkpoint不变已通过。见NF5K_ANNUNCIATOR.md。


2026-10-25 medium已完成权重校验和8个原曲窗口识别，归一化人声另3窗亦完成；仍有错词、无识别及时间争议，未发布新字幕。归一化、来源哈希和失败证据见MUSIC_FOLIA_CORE_REVIEW.md；准确同步需听校或原始时轴支持。
2026-10-25：NF5KServiceControlSetup在原鸣笛位置生成横向双按钮和贴台面薄底板；CabServiceControl接入主交互键保持/释放及旧SandOn后端，CabHornAudio播放原创合成汽笛。四场景生成、实际射线/上下文、供电/风压/距离/禁用守卫、PCM播放和底板支承已检查，详见NF5K_SERVICE_CONTROLS.md。


2026-10-25 页面内插件已真实导出两首LRC/SRT，audit_suno_timed_exports.py核对：千里马49句含28秒异常长句，星光53条与47行原稿顺序不符且桥段时间坍缩。原始文件与审计在tools/music/alignment_review/suno_exports；未替换正式字幕。详见docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md。


2026-10-25 refine_suno_context.py新增导出/声学交叉支持的局部候选，星光第21句错误早起点修到105.682秒；严格发布仍9项未通过，千里马仍3项。正式字幕未替换，最新疑点包已更新。见MUSIC_FOLIA_CORE_REVIEW.md。


2026-10-25 边界冲突候选新增星光16句79.622秒、20句92.642秒收敛；严格发布仍7项未通过，千里马仍3项。最新provider.boundaries候选及疑点包保留，正式字幕未替换。见MUSIC_FOLIA_CORE_REVIEW.md。
2026-10-25：NF5KBrakeRefinement统一大小闸冷灰涂层/裸钢/深色握把，按钮底板缩至110×60 mm并移位，消除与小闸安装耳约6 mm旧包络重叠，新横向间隙约27 mm。生成及实际Play姿态检查15 mm扩展包络，交互回归通过。见NF5K_BRAKE_REFINEMENT.md。


2026-10-25：本地歌词复核页改为真实波形＋可拖动起止/整体范围、连续试听、可调尾部、部分JSON保存恢复；build_alignment_review.py → review_ui.html，逻辑回归入口check_review_ui.cjs。仅模拟DOM/音频逻辑检查通过，实际浏览器试听/布局待验；未更新正式字幕。GitHub歌曲对齐项目选型及证据限制见docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md同日章节。

2026-10-25自动校准后续：独立Qwen韩语对齐环境及32窗口实跑完成，8个疑点仅1个四向稳定候选且仍与MMS终点冲突，未更新正式字幕。新增download_qwen_aligner/check_qwen_boundaries/summarize_qwen_evidence；用户不需韩语听校。LyricSmith两曲上传已获授权，但网页对齐面板未响应，尚未上传或导出。证据和限制见MUSIC_FOLIA_CORE_REVIEW.md同日章节。

2026-10-25：LyricSmith星光首次上传/47句对齐成功；audit_lyricsmith_ui.py核对原文及108词DOM渲染证据。原生导出未获，不把CSS动画时长当声学终点；6句起点与本地结果存在>0.25秒分歧，未写正式字幕。见MUSIC_FOLIA_CORE_REVIEW.md同日回执。

2026-10-26：audit_lyricsmith_exports.py归档4份原生导出，发现18/32句与SRT重复记录；用已有网页证据恢复47句108词独立候选，未写Assets。refine_suno_context.py新增哈希及原生LRC/SRT约束的lyricsmith来源，实跑0接收/6拒绝，未放宽门槛。恢复稿往返/顺序、工具语法及试听页逻辑验证通过；准确同步仍未全部通过，详见MUSIC_FOLIA_CORE_REVIEW.md同日记录。
2026-10-26大小闸材质v2：原图旧金属为主、现代产品图补充参考；BrakeLayeredMetal使用URP PBR，分铸件/加工面/涂层/握把，局部米制三向投影、网格磨损/积污遮罩；间隙检查改为旋转车体米制坐标。四场景及驾驶回归通过，见NF5K_BRAKE_REFINEMENT.md。
2026-10-26大小闸配色细化：暖锡灰铸件、较亮冷钢灰加工件、深石墨支承及暖黑握把；PaletteBatch保存共享材质并完成同角度渲染/间隙检查。见NF5K_BRAKE_REFINEMENT.md。
2026-10-26旧件实拍配色复核：加工件改中性偏暖银灰，局部暖色氧化/接缝压暗，支承件中性炭灰；不把全部零件统一成冷钢色。brake_palette_reference_v1检查与渲染通过，见NF5K_BRAKE_REFINEMENT.md。

2026-10-26无人工听校后续：check_qwen_asr_windows.py完成15次独立无歌词提示原曲识别；audit_blind_consensus.py为星光第4/41句建立逐词起点/行终点交叉支持，第26句仅行级，14/46仍不足。六项负例/分层测试通过；旧发布器/正式字幕未改，详见MUSIC_FOLIA_CORE_REVIEW.md同日记录。

2026-10-26：新增korean_ctc_path/check_korean_ctc及4项路径/断线续传测试；韩文模型下载仍待完整验证，尚无该模型歌声结果。analyze_vocal_activity已产生真实尾段周期性活动诊断，不能当语义歌词边界，未改正式字幕。详见MUSIC_FOLIA_CORE_REVIEW.md同日记录。

2026-10-26 校准后续：韩文CTC权重已校验并实际执行12个窗口，但口白对照也失败，未采用；Qwen1.7B无提示识别8窗口、单句强制对齐8次与MMS编码对照16次仍未解决全部边界。Suno可见星光歌词与47句原稿完全一致。工具新增模型/单句选项、独立报告和流式权重校验；正式歌词及Unity运行时未变。详细证据与未完成范围见 docs/for-ai/MUSIC_FOLIA_CORE_REVIEW.md 的“后续自动校准实测”。

2026-10-26 当前主工程Unity编译、RailwayMusicTerminalReview.Batch及第二进程BatchRestore均通过；本次不再是隔离快照。实际几何/舞台/队列/字幕交互/暂停恢复证据及源码哈希归档tools/compile/music_acceptance_20261026，详见MUSIC_FOLIA_CORE_REVIEW.md。尚未验证独立Windows Player；完整逐词歌词准确性仍未完成，不能以界面测试代替。

2026-10-26 第二曲在线对齐准备：重新打开LyricSmith，选择Korean并填入千里马49句原稿；页面Start alignment仍禁用，音频尚未选择，不能报告上传/对齐完成。已请用户仅操作本机文件选择，无需听校。audit_lyricsmith_ui.py新增--id支持两曲，比较前校验本地报告的曲目ID、实际MP3/原稿SHA256与行数，防止旧音频/旧原文交叉核验。星光47句108词重新审计通过，起点冲突仍为1/2/16/25/29/45，不放宽精度标准。test_lyricsmith_comparison.py验证正常身份及错误ID/音频哈希/原稿哈希/实际音频更换拒绝，运行通过；仅工具改动，不改正式字幕或Unity运行时代码。

2026-10-26 用户提供Edge下载文件chollima_ride (标准).json（11,509字节）：audit_lyrictimestamps.py实际归档原文件到alignment_review/lyrictimestamps/native，核对授权音频/提交歌词/当前原稿和本地报告SHA256，49句97词逐字覆盖，无负区间/逆序/重叠。输出chollima_ride.native-audit.json，保留提供方confidence但明确不是准确率、后端独立性未验证、未听校未发布。结构正确不能等同精确同步。

原曲/人声MMS比较中，行起点均不相符的仅43/44；第43句网站164.47–172.66秒，与本地约170.84秒起点冲突（网站confidence仍0.906）；第44句网站198.10秒，本地人声189.302秒，且首词仅80ms、confidence0.496。第17句网站67.62–73.49秒与本地起点接近但词尾更长，不能单凭对比宣称网站或本地谁正确。多句词尾有差异，需区分持音与对齐漂移。所有数据保留，不修改Assets正式歌词、不根据高confidence直接放行。submission更新nativeExportObtained=true。

test_lyrictimestamps_audit.py检查有效输入/真实起点冲突标记，以及缺行、错词、NaN、重叠、零长度、越界confidence/index拒绝，2项测试通过。此为工具与证据改动，没有新增Unity运行验证。

2026-10-26 大小闸动漫配色：NF5KBrakeRefinement按功能分配灰青主体、暖米灰支承、银灰加工件及红棕握把，降低微表面噪声；CastMetal材质名保留但已改涂层参数。共享资源/渲染/间隙检查通过，见NF5K_BRAKE_REFINEMENT.md。

2026-10-26 使用千里马原生JSON约束再对齐：refine_suno_context.py新增--provider lyrictimestamps，校验原生文件哈希、音频与原稿身份，保留原有窗口佐证/质量/分数门槛和独立输出。实际运行--include-boundary-conflicts，0项接收/5项拒绝：17句原曲音素碎裂反增，42句未改善或分数下降，44句导出窗口缺乏佐证；没有放宽门槛或修改正式歌词。

此外通过网站原生“比较各个时间轴版本”对话框，逐词点击Next，读取97个词各四个直接显示的起止时间；未选radio、未提交自定义选择、未播放或下载额外版本。保存chollima_ride.visible-comparison.json及variants-audit.json。每个可见词计数/总数及标准版时间均与原生JSON逐项核对（显示精度0.01秒）。该对话框直接展示数据，不读取隐藏状态、网络或受阻blob。行引导/字对齐/共识版本均未解决关键冲突：43句标准与字对齐同为164.47–172.66，行引导174.65起、共识167.86起；44句首词分别198.10、180.66、196.09、191.62起，差异极大。共识是网站给出的版本名称，不代表独立方法准确验证。候选仍未发布。

2026-10-26 大小闸integrated_v1取代浅青/米白试配：主体/支承统一现有深灰绿家族，暖深棕握把、克制银灰加工件；整台渲染及间隙通过，见NF5K_BRAKE_REFINEMENT.md。

2026-10-26 重复副歌声学对照：新增probe_chorus_dtw.py，对哈希验证/零偏移的人声使用librosa 0.11.0，16kHz/20ms帧，MFCC与chroma分别做限斜率子序列DTW和独立固定速率偏移比较。前两段已知重复副歌为正对照，桥段与第二段主歌为无关对照（桥段并非保证无声/无歌词）。两特征的固定偏移正对照均67.20秒，最后副歌分别143.98/144.00秒；最后副歌相隔至少0.5秒的次优偏移代价明显更差，无关片段偏移优势弱。映射首个既有“아”候选到196.14/196.16秒。完整报告及8组特征/片段结果位于chorus_dtw/chollima_ride.json。自由DTW首词映射MFCC199.22 vs chroma196.54仍不一致，不能隐去这一局部不稳定或将整段固定偏移当逐词准确认证。

无提示原曲Qwen0.6B对照：188–195.5秒返回“음악”，195.5–198.2秒返回“아.”，198.2–200.8秒返回“설립하”（非原文确切匹配）。第一窗口完成并落盘后，旧工具因Windows重定向stdout的GBK编码异常退出1；check_qwen_asr_windows.py修复stdout为UTF-8，随后独立标签只补跑剩余两窗口并退出0，未将未执行片段报完成。两个JSON标签final-onset-control/final-onset-followup分别留档。

新增probe_chorus_phonetics.py，以两特征偏移及非邻近备选代价为前置检查，在196.15秒附近分别用0.35/0.7秒边距重算原曲/人声，4次均将아对到198.10–198.12秒，捕获的可能只是长元音末端；不把20ms路径当真实整词起止。另对194–201秒运行pyin/RMS，既有高阈值未检出可信区间（空结果），未由音高数据证明语义人声起点。结果共同支持继续检查196秒附近，但尚未达到精确首音确认，boundaryTransferVerified=false、humanListeningVerified=false、published=false。未改Assets歌词、未改Unity运行时。所有新增/修改工具语法检查通过。

DTW来源为librosa官方Music Synchronization与sequence.dtw文档：https://librosa.org/doc/0.10.2/generated/librosa.sequence.dtw.html 。声学匹配不等于歌词语义识别。

### 2026-10-26 直接导入逐词JSON，保留真实词尾

MusicTimedText新增.json数组格式：每项word/start/end/line_index，line_index从0连续分组；按原始词顺序保留每个词的显式起止和词间空白，不均分、不插值、不将confidence当准确证明。拒绝缺字段、空数组、错序/重叠、跳行索引、非有限/非正区间；沿用1MB与5000行限制，另限10000词。Windows字幕选择器及本地同名字幕伴随导入支持.json，已有LRC/VTT/SRT优先级不变。导入只是用户选定的字幕数据，不改变内置原稿/解锁规则，也不自动导入当前未校准的千里马候选。

归一化词尾逻辑改为同时取现有词尾、下一词起点和行尾的最小值，避免把JSON已知词尾拉长到下一词。LRC原有按相邻词时间推导的行为保持，JSON可准确表达词间静默；当前不支持任意其他网站JSON封装。界面沿用现有导入字幕入口与本机独立音乐存储。

当前完整工程RailwayMusicTerminalReview.Batch重新编译/运行通过，日志tools/compile/music_json_runtime.log含MUSIC_NATIVE_JSON_OK（真实下载49句97词，14.44词尾与14.46下一词起点分别保留）、MUSIC_JSON_IMPORT_OK（导入/读取与坏文件保护）、MUSIC_REVIEW_OK。新增缺start/line_index、错序重叠、缺行索引、错误封装/空数组负例均由Unity实际解析检查。原有舞台/滚动/暂停/字幕交互回归通过。此轮未再运行第二进程恢复或独立Windows Player；不将原生数据解析成功称为歌词时间校准成功。
