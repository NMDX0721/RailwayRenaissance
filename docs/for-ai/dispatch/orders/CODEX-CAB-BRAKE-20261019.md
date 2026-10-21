# 驾驶台下一组件：原图左侧制动阀总成

## Unity正式资源导入

用户要求将有柄模型导入Unity并继续下一组件。已由当前运行的Unity 6000.5.8f1编辑器执行`CabBrakeAssetSetup.Build`并返回成功（`tools/compile/cab_brake_import_result.json`）。新增`tools/cab_reference/export_brake_unity.py`导出原生FBX与URP贴图，位于`Assets/Models/Cab/UnityBrake`；金属度/光滑度图按Unity通道约定打包，法线图标记NormalMap，基础色sRGB、数据贴图线性。

生成可直接拖入场景的`Assets/Resources/Cab/BrakeValvesWithHandles.prefab`和`BrakeValvesWithoutHandles.prefab`，材质为URP/Lit；附加方式创建并保存`Assets/Scenes/CabComponentReview.unity`后关闭，保留用户原场景。Unity检查有柄57个Renderer，包围盒约(0.19,0.22,0.24)米，无本轮C#编译错误。导入工具`Assets/Editor/CabBrakeAssetSetup.cs`支持菜单RailRevival/驾驶台/导入制动阀定稿；只在显式request文件存在时执行自动导入。

当前完成Unity资源导入与检查场景，不等于已替换主驾驶台UI或接入制动状态/操作动画。现有Cab3DSceneController主要生成窗外景物，不擅自把阀体加入其摄像机视野或修改经营/VN场景。下一组件为原照片右上方独立速度表。

Unity渲染检查图：`tools/cab_reference/brake_review/unity_brake_review.png`，由导入工具在附加检查场景中直接渲染，关闭后恢复原活动场景。`tools/Refresh-UnityAssets.ps1`可向本项目现有Unity窗口直接派发Refresh菜单命令，不依赖焦点或发送全局按键；批处理日志早期退出是因为用户编辑器已占用项目，最终导入由该现有编辑器完成。历史Blender定稿图与Unity内检查图分别保留。

Unity材质采用金属度图R/光滑度图A；为避免URP检查场景反光过强，贴图光滑度额外乘0.65。检查场景使用0.35方向光、0.35环境色和0.4反射强度。此为Unity显示调校，不改Blender定稿。后续整车装配时需结合统一驾驶室照明再校准。

检查场景额外使用`Assets/Models/Cab/UnityBrake/ReviewReflection.cubemap`中性常量反射，避免默认天空对金属高光的干扰；该资源仅供独立检查场景，不修改主场景反射设置。

附加检查场景的模型、相机和灯光使用31层。截图期间临时从其他已加载场景灯光的cullingMask排除31层，随后finally还原，避免原场景照明污染检查图；不保存其他场景改动。

隔离原场景灯光后最终检查照明改为主光1.2、补光0.7，环境/反射保持前述参数。前文0.35方向光为调试中间值，非最终检查场景配置。

## 最终美术交付：机械与配色收敛

用户要求最终版本、机械结构和质感配色优化，新增`tools/cab_reference/finalize_brake.py`。撤回上版两个独立小凸块的端面槽传动方案，改为一体圆轴颈+方形传动端、对应方孔轮毂、轴肩承托垫片、轴向紧固螺钉及头下垫片。方轴四侧面/轮毂方孔面负责传扭，轴肩与垫片负责承托，螺钉负责轴向防脱；名义方孔单侧间隙0.0002米。模型真实切出方孔、盲轴孔与垫片孔，保持可拆分旋转节点。螺纹仅为示意圆柱，未设计/验证啮合长度、防松、疲劳或强度，未将其称为合格实车制动机构。

继续保留两种区别明显的闸把、大/小不同初始展示姿态，以及已扩大的小阀控制圆盘。铸壳和旧钢颜色稍压暗、偏暖灰，保留加工面的亮度差和哑光颗粒，握柄为旧黑色；色调直接写入内嵌基础色贴图，避免依赖Blender专用节点。

最终资产：`Assets/Models/Cab/brake_valves_final_without_handles.glb`、`brake_valves_final_with_handles.glb`。源及图片位于`tools/cab_reference/brake_review/brake_final_without_handles.blend/.png`与`brake_final_with_handles.blend/.png`；回执`brake_final_audit.json`。原图和同类原厂图只用于美术参考，当前是用户授权设计的最终美术资产；历史接口真实性、操作档位、载荷/全行程和Unity运行均未验证。旧模型和历次候选保留，不自动替换场景。

最终检查：无柄版44网格/22564三角面、9张内嵌图、约2.81MB；有柄版57网格/33392三角面、12张内嵌图、约3.80MB。两版无预览桌面、相机节点，有限顶点与面数预算断言通过，双视角版本预览已检查。新增方轴与轮毂/垫片重算法线；模型检查通过不等于机械载荷验收。

## 原厂同类实物/剖面参考与质感迭代

本轮已实际下载并查看Westinghouse原厂图像：`brake_review/references/westinghouse_h6_parts.jpg`为3208=3目录H-6平面及剖面图，`westinghouse_s6_parts.jpg`为3207=3目录S-6实物插图封面。来源：https://www.sp3420.org/new-page-55 。从图可观察同类操纵件的上拱颈部、轴套厚度与细长握持端，但不能据此认定为DF4原图具体型号，也不将国外运行档位方位移植到当前模型。

交叉来源：Strasburg Rail Road自己的检修服务页 https://mechanical.strasburgrailroad.com/air-brakes-and-appliances/ 将6ET的H-6/S-6与14EL的K-14分别列出，因此这几类只作比较参考，不混称同一零件。

新增`tools/cab_reference/polish_brake_reference.py`：在连接设计双版本上缩扁并拉长小闸握头、降低短颈、抬高手柄端部，保留大/小闸区别。采用四种独立PBR表面：较哑光铸银、旧钢、略磨亮的加工银面、黑色握柄；色差/轻微污斑/粗糙度/微法线共同表达质感。加工轴套及圆盘与铸壳材质分开，未增加虚构铭牌或品牌文字。表面变化是美术设计，不能当作实物真实腐蚀分布。

新双版本`Assets/Models/Cab/brake_valves_reference_without_handles.glb`和`brake_valves_reference_with_handles.glb`；同名前缀BLEND/PNG及`brake_reference_audit.json`位于`tools/cab_reference/brake_review`。贴图位于`Assets/Models/Cab/Textures/BrakeReference`，并内嵌GLB。保留连接方案和前版源文件，不改Unity场景；载荷/运动行程/原车型号仍未验收。

## 用户授权设计连接结构与区分闸把

新增`tools/cab_reference/engineer_brake_connection.py`。本轮是基于机械逻辑的游戏模型设计，非原车工程复原：轴顶横向槽与套座内两个凸块传递扭矩，盲轴孔/贯穿套座孔/轴向螺钉负责防拔脱；套座内腔让开轴表面。名义径向间隙0.0003米、单侧槽隙0.00025米、底隙0.0005米，不宣称符合某一标准规格或已达到强度要求。参考通用轴毂键连接资料：https://www.ganternorm.com/fileadmin/user_upload/downloads/technischer%20anhang/technical_information/6885-1.pdf ，本设计并非DIN 6885平键的直接复刻。

大手柄改为较长、变宽折臂及横向握柄；小手柄改为短宽臂、抬高握头。两旋转节点分别绕Blender本地Z设55°和105°，使握持端向操作者方向展开；角度只定义展示/布局初始姿态，不能称为真实运转位。未验证阀位、全行程避碰、握持空间或力矩载荷。

短阀Short_Flange和Short_Cover径向放大24%，法兰螺栓同步外移，底座不同比例放大。这是按近照改善控制圆盘轮廓的视觉调整，未确认工程直径。

新双版本：`Assets/Models/Cab/brake_valves_connection_without_handles.glb`、`brake_valves_connection_with_handles.glb`。对应BLEND、PNG与`brake_connection_audit.json`位于`tools/cab_reference/brake_review`。原资产和前版保留，仍未接Unity。

静态交付检查：无柄版42网格/20644三角面，有柄版57网格/30812三角面；各6张内嵌贴图、无预览相机。自定义大手柄折臂执行法线重算。报告中的配合尺寸是设计名义值，不代表实际受力或运动模拟已经通过。

## 哑光双版本与中英文参考核对

用户指出上一版反光过强、卡槽不符，要求英文搜索并生成有/无闸把两版。新增`tools/cab_reference/build_brake_variants.py`，改用粗糙度基准0.60的银灰铸件和0.49的钢材，保留轻微PBR表面变化，取消清漆。贴图六张位于`Assets/Models/Cab/Textures/BrakeValvesMatte`并内嵌导出。

撤回上版无依据的圆孔加径向开口及四条侧槽，从detailed源重新生成浅矩形凹座；矩形凹座仍是照片估计候选，并非搜索已证实的真实接口。不得将其表述为正确工程还原。新增独立套座、短金属臂与黑色握柄，两组手柄各归原旋转节点，套座有真实内腔；外形和尺寸、锁定/限位机构及动作行程均待核实。

交付：`Assets/Models/Cab/brake_valves_without_handles.glb`（42网格/18084三角面）和`brake_valves_with_handles.glb`（48网格/22556三角面）。对应源`tools/cab_reference/brake_review/brake_without_handles.blend`、`brake_with_handles.blend`；预览同名PNG；回执`brake_variants_audit.json`。两GLB均6张内嵌图、无相机；有柄版每个旋转节点包含原轴及3个新增手柄组件。未覆盖原始模型，未接Unity。

参考核对：

- 英文检索词：14 EL brake valve handle、H-6 brake valve handle、S-6 independent brake valve drawing。
- 找到Westinghouse原厂H-6/S-6零件目录扫描件，修复项目公开托管：https://www.sp3420.org/new-page-55 。目录可以作为同类零件结构参考，不能直接认定为本照片对应型号。
- GE 44-ton原厂操作手册（14-EL相关操作）：https://www.prrh.org/downloads/ge-44ton.pdf ，检索可见，直接打开超时；未将未看见的图纸作为接口依据。
- 中文视觉线索：https://www.mlzp.cc/portal.php?aid=5480&mobile=no&mod=view ，其中混有示意画、机车外观和其他型号照片，不能按图片顺序误认型号。参考图保存于`brake_review/references`，仅用于本地核对；brake_77.jpg是带操纵手柄的驾驶室实拍，支持采用水平臂/握柄轮廓的候选思路，但无法确认具体阀型。el14_handle.jpg实际为机车外观、el14_cab.jpg实际为卡通，文件名不作为内容依据。

当前为双版本美术候选，接口形状、闸把固定结构仍需更清晰的本型号资料；不宣称已考证完成。

## 顶端卡槽与整体金属材质

用户要求还原近照顶端卡槽并优化质感，新增`tools/cab_reference/finish_brake_slots.py`。两组外露轴新增实际布尔切削的顶面凹座、径向开口卡槽和侧壁竖槽，槽口边缘0.0003米/3段倒角。尺寸、深度及背面槽数是近照视觉估计，不能作为真实安装接口尺寸；未据此宣称手柄可以机械装配。重做轴部UV。

材质区分粗糙银灰铸件与稍光亮钢件，增加低强度斑驳色差、微表面与端部暗化，避免镜面镀铬观感；不是对真实油污/磨损的精确烘焙。六张512×512贴图位于`Assets/Models/Cab/Textures/BrakeValvesAged`并内嵌GLB。

交付`Assets/Models/Cab/brake_valves_photo_slotted.glb`；源`tools/cab_reference/brake_review/brake_valves_photo_slotted.blend`；预览`brake_slotted_reference.png`和`brake_slot_closeup.png`；结构回执`brake_slotted_audit.json`。42网格，保留两个轴节点，未含预览相机，贴图嵌入检查通过；未接Unity、未确定型号或运行手柄外形。

## 用户近照细化版

参考用户1000268232.png，新增`tools/cab_reference/detail_brake.py`：加宽此前过于细长的高阀下壳体，补铸造底脚加强筋、法兰凸耳、肩部凸台、前侧凸部和短阀安装耳；外露轴改为可见棱面的形体。上述细节为近照驱动的美术近似，背面结构、尺寸和加强筋数量尚未证实，未增加不可辨认的标识。

新增`Assets/Models/Cab/brake_valves_photo_detailed.glb`；源与预览`tools/cab_reference/brake_review/brake_valves_photo_detailed.blend`及`brake_detailed_reference.png`，回执`brake_detailed_audit.json`。沿用银灰材质与两个独立轴节点，不替换旧资产、不接入Unity。

用户询问正常运行是否安装“扳手”：应称制动手柄/闸把。原始技术文献中的自阀、单阀均有操作手柄，且存在手柄取出状态；资料来源为原始专利说明 https://patents.google.com/patent/CN101537830A/zh 以及铁道部《铁路机车操作规则》扫描件 https://upload.wikimedia.org/wikipedia/commons/a/ab/China_Ministry_of_Railways_Operational_Rules_of_Railway_Locomotives.pdf 。这些资料说明操纵手柄的用途，但不能据此确认本照片具体阀型和手柄形状。照片看不到完整闸把，仅可推测被拆除，不作为事实断言；本版未凭空添加工作手柄，需补可靠实物参考后建独立可拆件。

## 第二版银灰金属候选

新增`tools/cab_reference/refine_brake.py`，从灰模BLEND读取分件。修复高阀下铸件与接缝之间0.002米空隙，以及顶部外露轴下缘0.0005米空隙；其余外形比例保留，未将视觉估计当作工程尺寸。灰模的法兰与主体为分件相交装配，未做完整碰撞或运动验收。

加入银灰粗糙金属主体与稍深色钢制紧固件，使用低强度色差、粗糙度变化和微法线表达表面使用感；不包含真实腐蚀分布的复原。铸件采用圆柱UV、端面平面UV。两套512×512基础色/粗糙度/法线图位于`Assets/Models/Cab/Textures/BrakeValves`，共6张内嵌图。

交付`Assets/Models/Cab/brake_valves_photo_refined.glb`；源`tools/cab_reference/brake_review/brake_valves_photo_refined.blend`；预览`brake_material_reference.png`和`brake_material_reverse.png`。静态检查25网格、14888三角面、6张内嵌图，无相机；仍保留两个外露轴节点。型号、螺栓布置、尺寸待核实；尚未定稿或接入Unity。

用户在手轮美术定稿后要求推进下一个任务。本轮选取DF4-0001原图左侧银色立式阀组，先做参考与旧资产核对；具体型号未确认，不以旧文件名作鉴定。

## 已完成检查

- 原图：`tools/cab_reference/DF4_0001_Cab.jpg`，左侧可见高低两个竖直阀体/操纵轴轮廓；部分操纵件可能缺失，不能直接据照片补造工作手柄。
- `tools/cab_reference/inspect_brake.py`对旧`Assets/Models/Cab/jz7_brake_valve.glb`做只读灰模检查，输出`tools/cab_reference/brake_review/existing_model_clay.png`和`existing_model_audit.json`。
- 旧资产为单网格327864三角面。检查图显示横置鼓形主体及顶部两个操纵件，整体轮廓与原图银色立式阀组不符；不直接复用为目标组件，不覆盖原文件。
- 网上参考方向：国家铁路局公开的《东风4C型内燃机车段修规程》含EL-14制动机条目，但这不能证明DF4-0001照片中的阀型。来源：https://www.nra.gov.cn/jglz/fgzd/gfwj/201804/P020220801568207502170.pdf 。JZ-7、EL-14相关资料仅作为进一步区分线索。

## 后续实施

以原图可见的银色高低阀体、法兰、紧固件和台面接口为外形基准，先做独立低面数分件灰模。优先核对制动阀与单独制动阀外观及年代差异，未确定的手柄、铭牌、档位不补成已考证细节。外形确认后再做金属材质与岁月感，保留可动节点；交互和Unity运行验收单独处理。

## 首版分件灰模

用户要求开始后，新增`tools/cab_reference/build_brake_gray.py`。以原图可见高低两组竖直轮廓生成阶梯铸件、法兰、接缝、紧固件与外露操纵轴。两组中心相对位置及所有尺寸均为照片估计；隐藏结构、螺栓数量和轴截面是灰模占位，尚未经实物核实，不属于工程复原。未补画原图不可辨认的操作手柄、文字或档位。

交付`Assets/Models/Cab/brake_valves_photo_blockout.glb`，25网格、14888三角面、约389KB；外露两轴分别归Tall_Spindle_Pivot与Short_Spindle_Pivot，尚无交互或机械行程定义。导出不含桌面/相机/灯光；顶点有限性、面数预算、GLB节点与两个旋转节点静态检查通过。

可编辑源：`tools/cab_reference/brake_review/brake_valves_photo_blockout.blend`；预览`brake_gray_reference.png`、`brake_gray_reverse.png`；回执`brake_blockout_audit.json`。旧资产保持不变；当前为首版外形灰模，未做最终材质与Unity运行验收。
