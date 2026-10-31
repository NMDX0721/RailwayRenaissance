# NF-5K 照明琴键实施与验收

用户于2026-10-24批准：琴键集中照明/通行/检修照明；前照灯、标志灯、尾灯及行车灯光模式独立。燃油泵、启动、雨刷和风扇不放入此组。

首批七路真实接线：司机室顶灯、仪表照明、司机工作灯、副司机工作灯、通道灯、客室一区、客室二区。另三键为断开的备用位；以后安装踏步灯、检修灯再分配，不能通过假状态冒充已安装灯具。

实物标注嵌入琴键自身边框的加宽区：中文为主，小号英文，只写功能名称；不显示编号、总标题或“独立开关”等多余文字。不随游戏语言变换实体印刷。交互词条集中在资源配置，使用稳定ID和zh/en文本；组件提供语言切换入口，未来全局语言设置可调用。本批不新增全游戏语言菜单，不宣称其他驾驶UI已本地化。

## 实施计划

- [x] 检查导入琴键的逐键节点、坐标与转轴，加入配置完整性和缺件失败验证。
- [x] 七路开关复用CabInteriorLighting，仪表开关恢复原调光值；断电保持选择，外部预设同步外形。
- [x] 逐键近距离射线/上下文操作；备用位拒绝操作；禁用/远距不能提交。
- [x] 生成正式中英铭牌、编号和状态图例，检查模型重建幂等性和实际渲染。
- [x] Unity编译、实际接线/供电/调光/语言切换及既有驾驶回归；同步代码地图和概览。

此分配是NF-5K游戏改装设计，不是DF4原车键位考证。既有灯光供电与扩散近似沿用，不新增灯具负载或电气保护模拟。

## 实现与接口

`CabLightingKey`附在每个原始`Rocker_Key_*`上，十个源转轴按驾驶位观察的世界X从大到小编号L01–L10；保留原模型，±22°本地X表示断开/合上，以每秒360°的表现速度过渡。读取实际照明选择，不另存十份状态。断电不弹键；组件禁用拒绝指令但不切断已选择的灯路。仅声明机构动画，不声称接点力学模拟或已配机械音效。

`NF5KLightingKeysSetup.Build`随`NF5KCabFittings`在四个检查场景生成，重建删除旧组件和旧铭牌根。各键网格自己的BoxCollider提供单独射线目标，不增加覆盖整排的总碰撞盒。备用键仍可查看提示，但不会摆动或接通。`CabContextInteraction`复用玩家主交互绑定并提供开/关两项选择。

`CabLightingKey.SetLanguage("en-US")`或`"zh-CN"`切换该组件的名称、状态、动作、拒绝理由与底部操作提示；原始词条和印刷文本共用`Resources/Cab/lighting_keys_profile.json`。语言暂为进程内适配入口，未写入存档或接全局设置。实物固定双语印刷，无需在语言切换时替换材质；不是把全游戏提示都切成英文。

`CabInteriorLighting.SetInstrumentEnabled(bool)`复用原`lastInstrumentDimmer`，不把每次开灯重置为固定亮度。现有调光/场景预设/兼容状态变化由琴键的LateUpdate跟随。自动紧急照明仍由原供电规则接管，不绑定普通键。

## 验证证据

- `tools/compile/lighting_keys_red_stdout.log`：预期失败，缺少运行组件；同时记录实际导入节点、坐标、300倍层级单位转换与转轴角度。
- `tools/compile/lighting_keys_build_v2_stdout.log`：Unity退出0。`NF5K_LIGHTING_KEYS_OK`覆盖十个实际逐键射线、七路真实灯具、逐路独立性、三路断开的备用位、无供电保持、恢复供电、原调光恢复、上下文命令、外部灯路变更同步、中英词条、距离及禁用守卫；`NF5K_SIMULATION_PLAYMODE_OK`与既有组件回归通过。
- 首轮构建发现Unity缺失组件不能用C#空合并操作符判断，已改显式Unity空值判断；以实际通过的v2日志为准。

## 标识依据与适用边界

未查到可以支持“所有铁路设备一律中英双语”的普遍规定；本项目采用中英双语是可用性和设备风格约定，不宣称认证。美国49 CFR 229.85/229.87分别对高压入口和特定手动开关的警示内容有明确要求，不能据此推导所有普通灯键必须双语。欧铁资料中的运营语言也依基础设施管理者公布的语言确定，不等同于驾驶台铭牌语言规范。

- [美国高压标识要求](https://www.law.cornell.edu/cfr/text/49/229.85)
- [美国特定手动开关标识要求](https://www.law.cornell.edu/cfr/text/49/229.87)
- [ERA运营语言参数说明](https://rinf.data.era.europa.eu/era-vocabulary/rinf-appGuide/)

后续驾驶设备宜沿用同一术语表、编号、图形和字号层级；本次没有批量改写其他组件的既有铭牌。


## 渲染复核

实景预览首轮近距离点补光冲淡文字，已将临时检查补光改为低强度方向光并关闭检查时的室内灯，不保存这些预览状态；原场景灯光未因此改暗。按实际驾驶视角修正L01至L10的从左向右顺序，英文副字由30至42像素放大。铭牌在原键前方独立固定，不随按键移动；与键一一对齐。

独立代码复核没有发现运行时阻断问题；已按意见增加“只允许预期灯路亮起”的灯具级断言，避免仅验证目标灯已亮而漏掉串路。原驾驶/雨刷/风扇等回归仍必须以最终日志为准。

最终验收：`tools/compile/lighting_keys_final_stdout.log`退出0，包含加强后的逐灯路隔离检查和完整驾驶回归；`tools/compile/lighting_keys_capture_final_stdout.log`退出0，`NF5K_LIGHTING_KEYS_CAPTURE_OK`。实际预览：`tools/cab_reference/interior_review/lighting_keys_bilingual_v3.png`及`lighting_keys_console_v3.png`。模拟核与快照编解码源码哈希保持不变。


## 用户修正：原件内整合标注（优先于上述首版历史记录）

2026-10-24按用户反馈撤销台面上十张独立浅色牌和“照明／独立开关”总标题。`NF5KLightingKeysSetup`直接沿原琴键前边框延伸30 mm的连续金属安装面，沿用原框材质和宽度；功能字嵌在该面内，保持逐键对应。只保留中英功能名称，不再显示L01–L10编号或外围独立牌。内部逻辑ID与七路接线、三备用位保持。`build_lighting_key_prints.py`生成`Integrated_V2`嵌入印刷资源，旧V1图片保留为历史资源但不再实例化。

本轮验收：`lighting_keys_integrated_stdout.log`包含`NF5K_LIGHTING_KEYS_OK`及`NF5K_SIMULATION_PLAYMODE_OK`，Unity退出0；`lighting_keys_integrated_capture_stdout.log`包含`NF5K_LIGHTING_KEYS_CAPTURE_OK`。已人工检查`tools/cab_reference/interior_review/lighting_keys_integrated_v4.png`：功能名称位于原组件边框延伸面，十张台面浅色牌及多余总标题已移除。


## 上侧标注与启机面板嵌装（2026-10-24，优先于旧版布局）

按用户要求，功能标注区改到琴键靠仪表的一侧（原组件较小Z侧），仍为原边框的连续30 mm加宽区；不再占用驾驶员侧台面。琴键与启机面板之间保留间隔，原开关对应关系不变。

`NF5KConsoleRecessSetup.Apply`将钥匙/燃油泵/启机独立面板整体下沉，以实际`Removable_Panel`顶面相对实际台面高1.5 mm定位；拆除旧的`Startup_Panel_Isolation_Mount`垫块。`Rear_Terminal_Enclosure`按实际外包围加每边1 mm间隙，对台面网格做贯穿矩形开孔并补连续孔壁。保留源网格副本，每次从原件重算，不累计裁切；仅实例替换生成网格，不改源FBX或手改场景YAML。面板法兰覆盖孔缘，接线盒位于台下；按键与钥匙跟随整个总成，交互逻辑沿用。

本次几何为游戏装配设计；未模拟实体紧固件承载、隔振、电缆接线和结构强度。

嵌装细化：台面外圈按面板法兰实际外包围每边留1 mm配合间隙，内圈按接线盒每边留1 mm。法兰底面处生成水平承托台阶，孔壁分外圈浅槽和内圈贯穿段，补台下环面；避免仅降模型导致法兰与桌板穿插。临时MeshCollider的两条独立射线分别验证中心贯穿与承托高度，验证后销毁，不新增玩家阻挡物。

本轮最终证据：`tools/compile/console_recess_final_stdout.log`退出0，四场景均包含`NF5K_STARTUP_RECESS_OK`（真实开孔与承托台阶射线通过），并有`NF5K_LIGHTING_KEYS_OK`及`NF5K_SIMULATION_PLAYMODE_OK`。`console_recess_capture_stdout.log`记录`NF5K_LIGHTING_KEYS_CAPTURE_OK`；已人工核对实际图片`lighting_keys_upper_recess_v5.png`及`lighting_keys_console_recess_v5.png`，标注位于琴键上侧，独立启机总成底盒不再露在台面上。


## 琴键标注对比度v6

按用户反馈将印字由灰米色提至近象牙白（#fffbed），底色由灰绿压低至#111815；中文主字88→104 px（长行84 px），英文42→56 px，并增加1 px同色描边与行距。只调整原边框内功能字，不添加标题或新牌。印刷面材质粗糙化（smoothness .08）并关闭镜面高光，降低反射冲淡细字的可能；保持受场景照明影响，不使用自发光冒充可读性。

生成入口仍为`build_lighting_key_prints.py`，`NF5KLightingKeysReview.RefreshPrintsBatch`仅同步贴图/印刷材质并渲染，不重建无关机构；`NF5KLightingKeysSetup`保持相同材质参数以保证以后重建一致。

验证：tools/compile/lighting_keys_contrast_v6_stdout.log 退出0，NF5K_LIGHTING_KEYS_CAPTURE_OK；人工核对 lighting_keys_contrast_v6.png，文字提亮加粗且保持原组件布局。


## 连续底板与功能分区v7（2026-10-24，当前布局）

按用户要求，撤销旧框与上侧加宽块的拼接方式：原四边框和功能标注区由同一原生贯穿开口网格`Cab_Lighting_Continuous_Bezel_V7.asset`替代；原边框渲染器隐藏但保留源几何用于重建尺寸，原琴键、转轴、固定螺钉和腔体保留。连续上表面与原边框共用材质，内孔下侧按原倾斜按键留退让。功能名称贴图改为透明底、保留抗锯齿覆盖率的alpha混合印字，不再形成十块异色矩形底牌。高对比中英字与七路交互保持。

启机面板沿台面向仪表方向移动35 mm，Z由约.060至.025 m；参考同一琴键安装点绝对定位，避免重建时累积偏移。台面嵌装开孔从保存的原始台面网格重算，旧孔不会残留。两组面板仍有独立功能身份；只统一琴键组件内部的底板。构建记录实际外缘间距，45 mm是本候选布局的回归下界，不是标准规定。

### 网络参考与采用范围

[EMD SD18原厂操作手册第二章](https://www.multimodalways.org/docs/railroads/RREquip/EMD/EMD%20SD18%20Operating%20Manual%20c1960s.pdf)将控制设备按控制板、发动机控制板、操纵器与空气制动区介绍，支持保留功能分区，未据此复制本项目的具体台面尺寸。

[BSI EN 16186-2:2017官方概览](https://knowledge.bsigroup.com/products/railway-applications-drivers-cab-integration-of-displays-controls-and-indicators)涵盖正常驾驶位置下的操纵件位置、视角、亮度/对比及反光；公开概览没有这两块面板的固定间距，因此本批间距是自主设计并以实际装配/射线验收，不称认证或符合标准全文。

四场景构建实测琴键统一底板与启机面板外缘相隔0.0630 m；`tools/compile/lighting_unified_v7_stdout.log`退出0并包含`NF5K_LIGHTING_LAYOUT_OK`、`NF5K_STARTUP_RECESS_OK`、`NF5K_LIGHTING_KEYS_OK`及`NF5K_SIMULATION_PLAYMODE_OK`。首轮预览`lighting_keys_unified_v7.png`验证连续板面与间距。印字后续改用alpha混合保留抗锯齿覆盖率，代替硬裁切；具体最终渲染结果以v8记录为准。

最终印字与预览：lighting_unified_capture_v8_retry_stdout.log 退出0，NF5K_LIGHTING_KEYS_CAPTURE_OK；人工核对 lighting_keys_unified_v8.png 与整体图 lighting_keys_console_unified_v8.png。底板连续、无标注色块，文字边缘保持抗锯齿。首个v8渲染被另一项平板编译占用阻止；未终止其进程，待其结束后重试成功。


v9文字位置微调：按用户要求，双语功能文字在原透明印字面内整体向琴键侧移动60贴图像素，对应约2.7 mm；保持字级、对比度、连续底板与两组面板间距。仅更新贴图，不移动机械组件。

验证：lighting_keys_text_lower_v9_stdout.log 的 NF5K_LIGHTING_KEYS_CAPTURE_OK；已检查实际渲染 lighting_keys_text_lower_v9.png，文字向琴键侧下移且留在连续底板内。


v10标注间距修正：上一轮仅在贴图内部微移不足，按用户反馈将整个透明印字面向按键侧推进9 mm，同时保留v9贴图排版。`NF5KLightingKeysSetup`与`NF5KLightingKeysReview.RepositionLegendsBatch`使用同一绝对定位，后者只通过Editor更新四检查场景标注位置并保存；底板和操作件不移动。

v11复核：透明印字面关闭独立投影与接收阴影，避免近距离叠面产生矩形暗块；实际文字仍由Lit材质接受照明。lighting_keys_text_close_v11_stdout.log包含四场景REPOSITION_OK及CAPTURE_OK，最终图lighting_keys_text_close_v11.png已人工检查。
