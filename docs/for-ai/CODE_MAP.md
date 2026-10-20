# 铁路复兴：沙能冲击 — 代码地图

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
| `GameData` | `GameData.XXX` | AdvanceDay 调 InitializeIfNeeded；VN 入口 ApplyVNExitData 调 InitializeSubsystems 并标记 initialized |
| `CrewManager` | `CrewManager.XXX` | GameData.InitializeSubsystems 调 Initialize |
| `EventManager` | `EventManager.XXX` | `GameData.InitializeIfNeeded()` 中调用 |
| `SandRivalManager` | `SandRivalManager.XXX` | 同上 |
| `OrderManager` | `OrderManager.XXX` | GameData.InitializeSubsystems 调 Initialize；每日由 DailySettle 推进 |
| `Narrative.NarrativeEngine` | `NarrativeEngine.XXX` | CheckForEvent 懒初始化；新局清理完整性待核验 |
| `RegulatoryEventSystem` | 完整命名空间或 using | ResetState / RestoreFromSave 调 Initialize，读取全局持久化键；不等于槽位完整恢复 |

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
