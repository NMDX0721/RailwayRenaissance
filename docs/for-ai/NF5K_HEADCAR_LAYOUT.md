# NF-5K驾驶客运合造头车：当前结构基线

2026-10-20用户确认优先于旧默认：NF-5K不是纯独立机车头，头车内整合一节客运空间。现有DF4参考用于操纵件/机电知识，不得据此把驾驶室后面全部做成机器间。

## 已确认与未确认

- 本端司机左侧一套操纵台；右侧副司机席和辅助区域尚待设计，不复制台面补满。
- 驾驶室后有隔门/通道，通往本头车客室。动力设备隔音、隔热、通风、检修通路与乘客区域分开；具体采用何种设备仓/动力布置未定，不宣称已经完成动力分散或动力集中选型。
- 双端驾驶室按整列编组安排，另一端保持各自左侧操纵，整体旋转180°，不能负缩放镜像。尚未决定编组、另一端所在车辆或总长度，本轮撤销在18 m短机车两头各放一个驾驶室的试排。
- 蓝色NF-5K参考图已实际打开确认为 `ExternalAssets/nf5_exterior_ref.png`；另一个 `nf5k_reference.png` 实际显示绿色图。旧文档的文件名/涂装描述相反，暂不重命名或覆盖，使用时必须核对图像内容。

## 实际交付灰模

灯具光学层不再沿用奶油漆材质：单独透明灯片、金属反射器和未点亮灯泡，未接照明开关/配光仿真。樱花、旗帜与车号视为印刷漆层，关闭其阴影投射，继续接收车体/扶手阴影，避免薄涂装像立体塑料贴片。

2026-10-21全车工业质感：新增`NF5KIndustrialFinish`，不以提高漆面金属度冒充钢材。大面漆层使用低幅度空间色差/粗糙度，车壳下缘少量路尘、屋顶轻微旧化，金属基材由独立裸钢五金/踏面体现。门板、门框、轴箱、转向架侧梁和设备柜添加最大4mm真实几何倒角，碰撞盒保持原体积；车门仍保留独立层级和开度。橡胶密封单独使用低反光中性黑材质，玻璃改预乘透明并保留镜面反射。

审阅场景调整为天光/地光分层、主光软阴影和弱辅光，加入明确命名`Review_Ground_Not_Vehicle`的中性审阅地面，帮助观察车体重量与接触关系；该地面不是线路/轨道资源或车型部件。涂装、车型标识及未分配车队编号保持不变。仍未实现折射、真实钢板厚度仿真、动态污损或轮轨物理。

2026-10-21门窗连接专项：`GlazedOpening`统一生成漆面圆角封板、5mm金属压边、橡胶密封圈、45mm深回口与内侧压条，玻璃置于外压框后约10mm；废弃薄片窗框与圆角外空洞。驾驶室侧窗外安装基准从x=±1.405移到±1.455m，门玻璃压框基准移到±1.477m，玻璃仍凹入，不再把整组窗悬在壳体内侧。客室窗间封板连续，门后补与窗带齐平的过渡面，门扇上的窗组仍随门开闭。

前风挡中心与中柱统一到8°斜面，`RakedZ/Shoulder`把车头两肩和上缘同步做成连续倾斜曲面，消除直立肩块与倾斜玻璃的错层；屋顶前眉下沿随之收口。以上均是可视化尺寸，未宣称符合实际车辆窗框截面标准。近景`door_window_junction_view.png`用于检查压边、窗角与接缝。

2026-10-21门与车号设计：驾驶室两侧门采用后缘铰链、向车内开至86°，打开后靠近后隔墙，避免外开侵占扶手/登车梯区域；后部两侧门同样独立内开，属于本车型设计推定，非原图可证实结构。`CabSideDoor.SetOpen(0..1)`支持脚本调用及Inspector/ContextMenu开关，门扇、窗、把手、内衬与涂装同层级转动；驾驶门锁舌在转动前退入，关门复位。主车体/内墙切出门洞，固定门框、扶手和梯子不随门移动。未接鼠标、门锁权限、人员感应或牵引联锁。

红白腰带选择连续到独立门扇：闭门时高度/颜色对齐，门缝处保留金属边；开门后门上的腰带随门移动，门框及灰绿内侧不延续外漆装饰。避免固定车壳背板或悬浮横条挡在开口内。`NF5KDoorReview`对四门各44个角度检查叶片BoxCollider与座椅/后隔墙/检修面板的穿插（2mm容差），并还原闭门；不等于整车所有结构/乘客/限界验收。

放大顶灯圆筒至0.42m外径，独立压圈与0.31m镜片；车型字牌加粗并独立网格，正面NF-5K字高0.11m、侧面0.17m。0721属于沙能载具，禁止用于NF-5K；本车只保留NF-5K车型标识，车队编号尚未指定，撤销误加的前/侧编号和附加小牌。新增开门图`door_open_view.png`，默认保存场景仍闭门。

微法线使用原生程序生成的`Subtle_Surface_Normal.png`，显式按NormalMap类型导入；漆面与金属使用不同强度，避免把微表面做成砂粒。资源随模型保存，不依赖外部图片生成服务。

2026-10-21材质细化：`NF5KSurfaceFinish`为持久车体网格补平面UV，为漆面与金属生成可重复的低幅度明度/粗糙度纹理，区分低金属度涂层、铸钢、加工踏面和五金。固定纹理只用于美术微表面，不属于游戏存档初值，不引入随机状态。前灯盒、门铰链、号牌和侧梁补紧固件，踏步前缘补有限露钢磨耗。普通透明URP玻璃提高轻微蓝灰吸收色与平滑度，增加柔和渐变环境立方体反射；并非实景反射探针、光线追踪、折射或物理夹层玻璃。涂装图案/车型尺寸/动力系统保持不变。

2026-10-21涂装纠偏：对照同一蓝色原图，将前上部红白带由浅折线改为从两肩向中央号牌下降的弧形带（中心高度约0.57m，两侧约1.04m），下裙补宽奶油色围带及红色细线。樱花废弃圆柱圆片堆叠与正弦散点，改为共享缺口花瓣平面网格、五瓣组合、细花蕊、非等大花簇和主枝/分枝；花朵为贴合漆面的薄层，避免立体糖果观感。前脸采用明确的左右枝条布局，侧面按不同位置展开簇群；仍属基于原图的重绘，未完成像素级复刻或人物宣传画。

2026-10-21整车轮廓与走行部：撤销只展示到z=6.1m的首段截断，车体连续延长到z=17.5m（前端约z=-0.65m，总长估值18.15m，不含车钩/风挡），补完整地板、拱顶、12组客室窗、后端墙/贯通门与风挡框、后部侧门区域。未在本车后端复制第二驾驶室。全车长度、内部座位排列、后侧门及转向架配置均为项目设计估值；原图为艺术参考，不能据其遮挡部分认定准确轴数。客席暂沿用重复占位，未做容量/通道验收。

新增`NF5KRunningGear`：暂选两组双轴动力转向架（4轮对、8钢轮），沿横向X布置整体车轴与独立轮对枢轴；车轮采用64段旋转截面，区分内侧轮缘、带轻微锥度的踏面、轮辋/轮辐板和轮毂，车轮名义直径0.92m、轮缘外径0.98m、轮心高y=-0.90m。轴箱、一系螺旋弹簧、侧架、摇枕/中心支承、制动块、齿轮箱及牵引电机包络可见。动力转向架为当前可视化候选，不因此宣称已确定整车电传动拓扑、悬挂计算、轮轨接触或驱动动画。它不是蒸汽机车外置连杆动轮。无负缩放镜像轮缘。

结构参考采用厂商资料：[SKF轴箱设计](https://cdn.skfmediahub.skf.com/api/public/0901d196801410ca/pdf_preview_medium/0901d196801410ca_pdf_preview_medium.pdf)说明轮对、轴箱与弹簧/导向结构的关联；[Voith轨道传动](https://www.voith.com/corp-en/drives-transmissions/gear-units.html)列举柴油动车使用的轮轴齿轮传动与安装形式。这些仅支持结构关系，不提供本虚构车型的轴数或尺寸认证。

新增`NF5KLivery`：调制漆面金属度/粗糙度，按原图布局添加樱花几何贴饰、旗帜、车号。装饰是独立可替换几何，不把原图整体投射到车体；人物宣传画、韩文铭牌与精细旧化尚未完成。全车预览`exterior_reference_view.png`，驾驶端近景`front_detail_view.png`、动力转向架`powered_bogie_view.png`、完整后端`rear_complete_view.png`。

2026-10-21第二轮：按同一原图补前脸内收下裙（三高度截面）、成组通风口、车钩安装垫/预留外包络、前扶手、红星翼徽几何与号牌底板；顶灯由方盒改为圆筒灯壳和独立金属压圈，侧门增加固定门框厚度。车钩只是布局占位，没有锁闭机构、受力或编组连接实现。翼徽为按图重建的简化几何，尚未加入文字/樱花等贴花；仍只交付头车首段。

2026-10-21参考图修形：`NF5KReferenceBody.Build`替换驾驶室盒状外壳。以蓝色原图可见特征为依据，正面保持较平、两肩以12段圆弧转向侧壁，32段拱顶贯通客室首段；分离蓝色腰带、奶油/红色细线、前双风挡与雨刷、侧窗分隔条。侧门改为窄长钢门，上部圆角小窗采用真实开口，下部钢板、门缝边框、铰链、门锁、两侧长扶手与三级登车梯分别建模。早期仅闭门；现已补独立开门及局部扫掠检查，范围见最新条目。左侧门直接参照原图，右侧对称设计与内隔门仍属推定。所有长度由原图比例估计，并非实车尺寸。

新增外观预览`tools/cab_reference/interior_review/exterior_reference_view.png`与门细节`door_reference_view.png`。曲面网格保存至`Assets/Models/Cab/NF5KBodyMeshes/`，通过Unity编辑器脚本生成场景；保留现有驾驶台、启动接线与客室首段。转向架/完整底架、樱花、徽章、宣传画与整车定长尚未完成，不能把本次车体首段当作整车定稿。

`Assets/Editor/CabInteriorBlockoutSetup.cs` 生成 `Assets/Scenes/CabInteriorBlockout.unity`：左侧操纵台、两张乘务座椅、脚踏与警惕预留；左右侧门与分件枢轴；前风挡8°工作倾角、黑色密封框、透明玻璃；顶棚灯/风扇/遮阳板预留；后壁改为客室隔门与独立检修板。后方加入三排客席/侧窗的客室首段，展示一体贯通关系，不是整节车最终座位定员或完整长度。

暂定净宽2.8 m、驾驶室地板深2.4 m、本轮拱顶中部高约2.34 m（边缘约2.10 m）、司机眼点(.65,1.38,.78)m均为工作估值，未取得NF-5K真实工程图。中车DF4B同类车外形宽3309 mm不能当成驾驶室净宽；头车客室截面还需结合真实车体结构设计。灰模已做局部门扇扫掠检查，仍未完成全部结构扫掠、通道宽度/逃生、人体触达、座椅调整或车辆限界认证。

玻璃阶段仅验证开口、框/密封条、透明可视与倾角；URP普通透明材质不能宣称具有物理折射、夹层结构、除雾加热、雨刷水膜或完整反射。外壳已有圆弧肩部、拱顶与蓝/奶油/红条分色；樱花、翼徽、宣传画、完整下裙与底架仍未完成。

## 真实资料的边界

- 中车DF4B原厂产品页 https://www.crrcgc.cc/crrcgc/2015-12/07/article_F589ACBC35A2460BAAE2CEAC75811CAB.html 明确其为双司机室内走廊机车，并列外形21100×3309×4500 mm。只用于对照，不能直接作为NF-5K合造头车布局。
- DF4-0001原始摄影 https://commons.wikimedia.org/wiki/File:China_Railways_DF4_0001_Cab_201110_128.jpg 用于左侧操纵与可见门窗/表面结构。
- 本次搜索发现DF4B教学转载提到左右侧门和后壁双机器间门，但未取得可核对原车门位的工程图，不当成NF-5K的后门配置依据。模型当前客室隔门来自用户整车理念，是明确的项目设计。
- 屋顶风扇、遮阳板、检修板位置仍是同类设备的设计占位，不声称复刻某车号。

预览输出：`tools/cab_reference/interior_review/driver_view.png`、`cutaway_view.png`、`rear_wall_view.png`。Unity批处理重新加载已保存场景并渲染，检查现有驾驶台状态与启动面板仍在；此项不等于通过运行时交互或整车工程验收。

下一步先统一头车纵剖面：驾驶室—隔门/过渡区—客室—动力设备隔离/检修路径，再做外形与窗位对照；整列双端与换端控制随后接，不能把尚未决定的编组硬塞到现有DF4模型。


### 2026-10-21 ????????
???????????????????????????????????????6mm???????????????????NF-5K?????????`CabInteriorBlockoutSetup.Shot`?4200?3000????`NF5KReviewDownsample.shader`??????1400?1000?????????????????????SSAA??????????????????


### 2026-10-21 NF-5K????
`NF5KLivery.Text`??????????????????????????????5?????????125mm????????????????????????????????????????????????????????


### 2026-10-21 NF-5K front identity and functional lighting
Solid polygon lettering on a 640x220mm plate; 320mm wing badge; static articulated wiper geometry; five optical assemblies with Off/Marker/Dim/Bright/Tail modes. Runtime and dark-scene acceptance passed. See [exterior lighting specification](NF5K_EXTERIOR_LIGHTING.md) for interfaces, assumptions, sources and verification.


### 2026-10-21 NF-5K mechanical and interior-lighting scope
Front placeholder coupler replaced with static cast parts, independent knuckle, carrier and sill; closed angle cock and parked hose; service skirt details. Coupling forces and brake-pipe continuity are not simulated. Driver/task, instrument and passenger lighting remain separate planned circuits. See [scope and references](NF5K_FRONT_MECHANICAL.md).


### 2026-10-21 NF-5K cabin and passenger lighting
CabInteriorLighting and NF5KInteriorLightingSetup add independent cab ceiling/task, dimmable instrument, two passenger zones and automatic emergency transfer from explicit normal/backup supply inputs. NF5KInteriorLightingReview checks renders and NF5KLightingPlayReview checks runtime. See [interior lighting specification](NF5K_INTERIOR_LIGHTING.md). Passenger interior remains a blockout; physical glass reflections and battery simulation are unverified.
