# NF-5K 十格状态／报警灯窗

2026-10-25。当前单驾驶室；替换原四颗未接线圆灯，不增加第二驾驶室。`NF5KAnnunciatorSetup`由`NF5KCabFittings.Apply`统一生成四检查场景。`CabAnnunciatorPanel`只读`CabSimulationDriver`，不改变动力、制动、保护或存档。未接详细模拟的静态场景默认熄灯，不伪造电源/故障。

## 实物研究和设计选择

- 印度中央铁路ZRTI/BSL的2013培训讲义，正文138页，给WDM2列出八格指示面板，含高温、低油压、接地、空转、辅助发电相关指示及备用。说明“四灯”没有普适充分性，应按车辆实际系统分配，非所有车统一十灯。官方PDF：https://cr.indianrailways.gov.in/cris/uploads/files/1418881285399-WDG-3A%20%20%28ENGLISH%29.pdf
- RDSO/CAMTECH 2022–23 WDG6G手册，正文74页，区分无法启动、启动但不能加载、停放制动、温度引起限载及具体状态消息。复杂故障适合详细诊断，不宜全部堆进主视线灯板。官方PDF：https://rdso.indianrailways.gov.in/uploads/files1/Introductory%20Handbook%20WDG6G.pdf
- EMD历史操作手册亦有低油压、过热、空转及接地等类别；本次未顺利取得该PDF全文，不以其具体阈值作为依据。上述外车型资料用于功能分类和布局研究，不冒充NF-5K实车电路或现行通用规程。

当前选择两排五格，先落实已有后端能提供的十项。未实现接地、断路器、真实轮滑速度、车门闭锁和防护系统，暂不增加对应“正常”灯。黏着受限是牵引力被静态黏着上限裁剪，不能称轮滑检测。

## 状态映射（从驾驶员视角每行左到右）

| 格 | 中英文 | 颜色 | 条件 |
|---|---|---|---|
| 1 | 机油压力低 / LOW OIL PRESS | 红 | Running持续3模拟秒后，OilMpa低于0.15进入，达到0.18清除；停机抑制 |
| 2 | 冷却水温高 / HIGH COOLANT | 红 | TripTemperatureC−20进入、低于TripTemperatureC−25清除；ThermalTrip保持点亮 |
| 3 | 总风压力低 / LOW MAIN AIR | 琥珀 | MainKpa低于600进入，达到650清除 |
| 4 | 辅助未发电 / AUX GEN OFF | 琥珀 | 辅助发电未就绪：非Running或Rpm低于IdleRpm×0.9。对应现有供电代理条件，不是充电电流传感器 |
| 5 | 紧急制动 / EMERGENCY | 红 | Input.Emergency或Brake==4；表示紧急请求，不声称气压已建立 |
| 6 | 停放制动 / PARK BRAKE | 琥珀 | Input.ParkingBrake |
| 7 | 空气制动 / AIR BRAKE | 琥珀 | 本机或列车制动缸大于20 kPa进入，大于10 kPa保持；与停放制动独立 |
| 8 | 黏着受限 / ADHESION LIMIT | 琥珀 | State.AdhesionLimited，不推断真实空转 |
| 9 | 柴油机启动 / CRANKING | 白 | State.Cranking，不等同于发动机运转 |
| 10 | 柴油机运转 / ENGINE RUN | 绿 | State.Running |

灯板供电：Master且（Battery>0.2或辅助发电就绪）；断电只熄显示，不写任何模拟选择。时间倒退时重置灯板迟滞及油压延时，避免旧检测历史残留。阈值是与当前候选模型配套的显示参数，非原车额定数据，也未新加停机保护。现有热保护仍由模拟核负责。

## 外形与材料

370×86 mm薄金属背板，十格金属压圈、黑色密封座、低于压圈表面的磨砂透光窗；四角真实凹槽螺丝。统一中文主字／英文副字印在透光窗内，文字透明贴层无独立投影。熄灯保留灰白可读字，点亮由透光面和字的受控自发光表达，不用十个点光源制造串光。小倒角沿用驾驶台工业件生成流程；使用现有低光泽材质体系。

`annunciator_profile.json`提供物理中英标签及色类；`build_annunciator_prints.py`生成十张原创建模印刷贴图。物理印刷固定双语，不随UI语言重印。

未实现报警声音、确认/消音、故障历史、灯泡寿命和电气故障。实体试灯见下节。无闪烁假报警，不把显示逻辑当成保护控制。

## 实体试灯按钮（2026-10-25）

报警板右侧余量内新增22 mm金属安装圈和17 mm灰绿帽按钮，帽的按压行程1.5 mm、弹回显示约0.16 s，固定中英“试灯 / LAMP TEST”印字。与原辅助板接触，不新增悬空台面小盒。

`CabLampTestButton`沿用玩家既有主交互键和上下文提示，不新增需要记忆的快捷键。点按触发2秒试灯继电器效果；`CabAnnunciatorPanel`暂时覆盖十格显示，保留真实状态读取。超时恢复实际信号，不消除报警或改变保护、发动机、制动、牵引输入。试灯中拒绝反复重置计时；2.4 m距离和禁用守卫；主控无供电不能试灯，试灯时断电立即终止、按钮回位，恢复供电不续上旧试灯。

这是灯窗显示检查，不声称检测实际传感器、继电器接点或灯泡故障。当前没有这些故障模型，也未添加假的“故障已排除”反馈。

本轮顺带静态复核：`CabDrivingTarget`实体操作覆盖牵引、自阀、单阀、换向、停放制动；`CabConsoleUI`仍有旧UI鸣笛/撒砂入口。尚未将鸣笛和撒砂补为当前3D上下文实体控制，适合作为后续操纵批次，不能把旧UI按钮等同于实体安装完成。

本批tools/compile/lamp_test_v1_stdout.log退出0，四场景重建，NF5K_LAMP_TEST_OK与NF5K_SIMULATION_PLAYMODE_OK通过；实际射线/上下文、十格发光、两秒截止时间、按钮行程、超时恢复、重复触发拒绝、断电与供电恢复、距离/禁用及前后checkpoint完全相同均已检查。计时到期分支由测试设置私有截止时间验证，不冒充人工等待测量。tools/compile/lamp_test_capture_v1_stdout.log退出0，NF5K_LAMP_TEST_CAPTURE_OK；人工核对annunciator_lamp_test_v1.png、annunciator_test_button_v1.png，实际通电按键触发全亮，取消后恢复冷车读数。截图只用于预览，未保存临时主控/钥匙/试灯状态入场景。

## 验收

### 灯箱材质v2（2026-10-25）

透光面改为独立UV平面与有厚度暗色窗体贴合，上方仍为印字层，均低于四边压圈。扩散发光贴图模拟浅灯箱内两处光源经磨砂片混合的亮度：中心柔亮、靠遮光边渐暗；不是均匀彩色自发光矩形。使用线性EmissionMap、轻微颗粒底色和细弱微法线，漫反射与高光独立保留；取消字面单独自发光，亮灯用深色遮字表现、熄灯保留灰白字。最终光强从试验1.8降到1.05，避免高亮琥珀窗冲淡文字。此为美术光学近似，不新增逐灯实时点光源、GI计算或全局Bloom依赖。逻辑阈值和模拟保护不变。

生成工具新增`Cab_Annunciator_Frost_V2`、`Diffusion_V2`、`MicroNormal_V2`，确定性噪声只用于材质，不影响世界种子或模拟状态。三图的TextureImporter带`CabAnnunciatorOpticalMap`标记，`SpriteImportFixer/FixSpriteImports`仅对这些标记资源跳过强制Sprite转换，保留线性扩散图、真实NormalMap、三线性过滤及mipmap。URP材质使用RealtimeEmissive标志保留发光shader变体；不请求DynamicGI更新，也未增加照亮周围物体的实时灯。MaterialPropertyBlock负责未选灯窗黑色发光覆盖。

本轮`annunciator_optics_v2_stdout.log`退出0，四场景和驾驶回归通过。`annunciator_optics_reload_stdout.log`退出0，独立新进程的`NF5K_ANNUNCIATOR_OPTICS_RELOAD_OK`验证材质关键字及三张贴图导入设置持久化，随后`NF5K_ANNUNCIATOR_OK/NF5K_SIMULATION_PLAYMODE_OK`通过。`annunciator_optics_capture_final_stdout.log`退出0，人工核对最终`annunciator_cold_v6.png`与`annunciator_dark_v6.png`，扩散面亮度渐变、深字和边缘遮光可辨；不采用之前导入故障或过亮的v3–v5试图作为终稿。暗图为检查灯减弱和环境光降低后的材质对比，不宣称完整驾驶室夜间照明已经重做。

`tools/compile/annunciator_v2_stdout.log`退出0：四场景生成，`NF5K_ANNUNCIATOR_OK`通过。包括十格印字实际网格射线无遮挡、冷车/启动/运转、低油压延时、压力和温度迟滞、紧急/停放/空气制动的区别、黏着上限真值、供电/断电、快照时间倒退、真实材质属性与禁用熄灯。`NF5K_SIMULATION_PLAYMODE_OK`及既有照明、雨刷、风扇等驾驶交互回归通过。

第一轮图发现实心压圈遮住字面，随后改为四条独立框边并加逐格射线测试，不能使用旧`annunciator_*_v1.png`作为最终效果。补拍时`GameMainUI`对设置页新增第三参数而接口未同步，导致`annunciator_capture_v2_stdout.log`编译失败；未修改该会话的平板文件，接口由对应工作补齐后继续。

`tools/compile/annunciator_capture_v3_stdout.log`退出0，`NF5K_ANNUNCIATOR_CAPTURE_OK`。人工检查`annunciator_cold_v2.png`与`annunciator_running_v2.png`：冷车总风低/辅助未发电/停放制动点亮；模型运行300秒后的怠速仅停放制动/柴油机运转点亮，油压无假报警。另有`annunciator_unpowered_v2.png`和`annunciator_console_v2.png`。截图由真实独立模型读数驱动，仅临时应用检查光照，未把截图状态保存入场景。

本轮未改`CabSimulation.cs`与`CabSimulationCodec.cs`；SHA256分别为`523D13EDD4A56873917B08D6E282304FCD227026EDA28DCB24AEFF9577FF52AD`、`36E515D8B7C5F107309BF1688FCED3C9F37B1BE3505DF8F51AD12422AEC044FD`。

鸣笛/撒砂批次补正：共享发光材质的默认EmissionColor设黑，RealtimeEmissive标志仍保留发光变体。避免未进入Play且尚未写入PropertyBlock时十格默认全白；`service_controls_final_stdout.log`独立重开与实际发光回归通过。见NF5K_SERVICE_CONTROLS.md。
