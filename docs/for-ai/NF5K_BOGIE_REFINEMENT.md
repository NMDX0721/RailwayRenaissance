# NF-5K 转向架细化

## 2026-10-22 使用痕迹与静态轮轨支承

旧展示平面顶面为-1.370m，轮缘最低约-1.390m，确有约20mm视觉穿地。`NF5KWheelTrackReview`生成与车辆分离的两条静态检查轨，内侧净距1435mm，轨头宽65mm；轨顶按当前锥形踏面在x=±0.750m的截面高度设为-1.361222m。轨下地面顶面改为-1.550m，检查8轮实际网格截面接触误差不大于0.5mm、轮缘不穿地。这不提供轮轨碰撞解算、线路运动或正式轨型认证。

轮体仍使用整体钢轮。表面区分银灰接触磨亮带、棕灰氧化肩部、暖灰褐辐板与局部深色积尘；圆周浅擦痕通过色差、光滑度和细法线表现，没有捏造裂纹、剥落或改变轮径。磨亮带沿截面弧长UV第8段约0.400–0.511定位，不把整个轮缘做成银色。

此处地面是静态检查环境。轨旁增加独立检查走道，顶面保留-1.370m登车基准，轨下地面降低，两者不能混用；`NF5KBoardingAudit`优先读取走道顶面，分别输出三场景报告并复核29cm首步、靴子净空和握持空间。该走道不是正式线路站台与收纳适配。`CabInteriorBlockoutSetup`全量生成末尾也安装检查轨，避免重生成恢复旧穿地平面。图片`wheel_wear_mounted.png`与`wheel_wear_isolated.png`，日志`tools/compile/cab_wheel_wear.log`。

本轮Unity检查：三个场景分别通过`NF5K_WHEEL_RAIL_STATIC_OK`与`NF5K_BOARDING_STATIC_OK`，随后通过`NF5K_CONTEXT_INTERACTION_OK`和`NF5K_SIMULATION_PLAYMODE_OK`，无C#编译错误。接触检查取轨头中心处踏面截面插值，不等同于整个接触斑或悬挂受载分析。

## 2026-10-22 钢轮表面专项

轮体尺寸与截面不变。修正原径向UV在恒定半径面退化为线的问题：周向参数沿接缝连续0–1，截面参数按累计弧长展开，并重新生成切线。独立`NF5KWheelSteelFinish`生成踏面与辐板各自的512²颜色、金属度/光滑度、细法线纹理，替换共用灰白纹理与遗留通用凹凸。

踏面使用高金属度与浅周向加工纹，光滑度约0.40–0.53；辐板使用钢底色与低金属度氧化斑、约0.17–0.30的光滑度，保留宽而柔和的裸钢反射。初次渲染发现高光与车纹过强，因此减浅法线及车纹色差、降低光滑度。无需自发光，也不增加轮体几何或装饰螺栓。材质为美术候选，没有模拟真实磨耗演化。沿用现有检查光照，不改变车灯和驾驶室反射系统。

专项检查入口为`NF5KBogieDetailReview.BatchSteel`，仅更新共享资源，不重建梯子或驾驶台；新截图`steel_finish_isolated.png`、`steel_finish_mounted.png`，日志`tools/compile/cab_wheel_steel_finish.log`。三个场景共用上述材质与轮网格。

## 2026-10-22 轮体截面与登车尺度复核

用户再次指出塑料感、轮体细节及梯子可用性。本轮按[日本制铁整体钢轮结构说明](https://www.nipponsteel.com/en/tech/report/nssmc/pdf/105-06.pdf)区分轮毂、轮辐板、厚轮辋、踏面与轮缘，改为18点实体凹形截面。保留原踏面/轮缘直径与横向安装位置，增加轮毂凸台与轮辐板内凹过渡；轮辐板局部连续法线、轮缘肩部明确分界。轮轴与轮毂、销轴和轴箱紧固件单独使用加工金属材质；没有把整体压装钢轮改成汽车螺栓轮毂。

旧梯最低一级相对审阅地面约0.67m，不能仅凭模型连接判断可攀登。现在改4级，顶级中心y=-0.26m，其余向下每级0.28m，最低中心y=-1.10m；相对当前审阅地面首脚约0.29m，地面高度变化时必须重新评价。踏面深0.24m、总宽0.66m，两端边梁不再凸出到顶级踏面上方。圆管扶手直径32mm、中心x=±1.59m，墙侧候选握持净空84mm。此为项目人体工学候选，不是法规符合性声明。

NF5KBoardingAudit检查两侧每级两个落脚位置、0/35/70mm抬脚高度，共48个靴子包络；候选靴长290mm、宽116mm、高110mm，落脚外移50mm后承托长度215mm。最初检查实际发现旧Worn_Step_Leading_Edge碰撞体和楼板边缘侵入，分别清理遗留装饰并修正落脚位置；不是忽略这些碰撞。扶手到候选肩部距离按650mm臂长上限检查，另输出1.75m比例人偶。此人偶不是林彪悍正式模型，不保存进车辆场景。

当前通过项只覆盖静态靴子和握持包络，未做完整攀登动画、身体全姿态碰撞或结构强度。它是面向车体抓扶手的攀登梯，不是普通步行楼梯。折叠/伸缩、行车收纳和站台接驳尚未实现，用户需求已另存[收纳与站台接驳](../../参考资料/系统/登车梯收纳与站台接驳.md)。

验证入口仍为NF5KBogieDetailReview.BatchBuild，新增静态检查后保存三份场景；结果以`tools/compile/cab_boarding_review_final.log`与`cab_boarding_static_audit.json`为准。新图：`wheel_profile_review_final.png`（单独轮体）和`boarding_175cm_review_final.png`（比例人偶）；另有整组及四级梯近景。最终视觉还需用户审阅，不能仅凭自动检查宣称消除所有塑料感。

## 2026-10-21 灰色走行部、格栅踏步与被动反光片

用户要求消除塑料感，参考日本铁路配色，不局限于深色，并补梯子细节及反光条。参考[日本制铁轮轴实物](https://www.nipponsteel.com/en/product/railway-automotive-machinery-parts/list/05.html)与[2025年转向架技术资料](https://www.nipponsteel.com/common/secure/en/tech/report/pdf/133-04.pdf)的构件区分；本项目选择灰绿主架、浅灰轴箱、暖灰设备壳体、石墨色弹簧及金属踏面的层次，并非“日本铁路统一标准色”。

NF5KRunningGearFinish为钢构漆层设置低金属度、低平滑度、低幅度色差和微法线，不能以钢材基体为由把漆面设成裸金属。轮辐板与踏面继续独立材质；新Profiled_Wheel_Hard_Shoulders保留截面尺寸，96段圆周方向平滑、截面肩部拆法线，消除跨轮辋/轮面大范围平滑形成的橡胶胎式高光。二者不是靠自发光提亮。

梯子3级踏面改为有真实间隙的开口条栅、周边框和中部横系条；保留完整踏步包络碰撞盒，尚非可踩空的精密网格碰撞。补六角螺栓、垫圈与窄赭黄边缘标识。每级前缘有白/琥珀色薄反光片，两侧总计12片，不参与碰撞、不充当灯具或阴影遮挡物。具体配色和位置是项目设计，不作为车辆反光标识标准引用。

CabRetroreflective.shader使用URP PBR基底与光照驱动的方向回归项：入射方向与观察方向接近时增强，乘光源强度、面朝向、距离衰减及阴影衰减；支持主光与附加点/聚光灯循环、Forward+，无自发光项。它是经验型回归反射近似，未标定真实反光膜角度曲线；多附加平行光的回归分量、严格能量守恒、Android低阶兼容及性能尚待评估。

三份既有审阅场景均更新，保留启动/驾驶交互。整车材质收尾再次调用走行部配色，避免被旧通用材质覆盖。颜色微纹理为固定美术资源，不修改任何存档初值。

图片：`japan_grey_running_gear_final.png`、`japan_grey_wheel_detail_final.png`、`ladder_grating_reflector_final.png`，位于`tools/cab_reference/interior_review/`。日志`tools/compile/cab_grey_grating_reflector.log`验证编译/着色器及原驾驶交互；NF5KReflectorReview.BatchBuild另外隔离比较关灯、近视线聚光和斜向聚光，输出`tools/compile/cab_reflector_acceptance.json`，结果以实际通过标记为准。

## 2026-10-21 梯子固定与车轮材质修正

用户指出梯子连接薄弱、车轮过黑。本轮新增NF5KAccessLadder，替换驾驶室两侧原细直边梁：每侧2块底架固定板、2根上托臂、2根斜撑及加粗倾斜边梁，固定板以4螺栓表现连接；每级踏步增加两端角托、前缘下折和低矮防滑筋。连接到Continuous_Underframe侧缘，不挂在门扇、薄外皮或转向架上。踏步仍为3级、约240mm高差；属于静态设计候选，未做载人强度、疲劳或车辆限界认证。未变更原侧门开向。

车轮原高金属度、暗底色与通用微表面叠乘造成背光黑盘。Wheel_Oxidized_Web改为中性氧化钢灰及较低金属度，保留扩散分量；Wheel_Machined_Tread单独采用较亮钢色与适中粗糙度。新增低幅度同心加工纹理、径向UV，取消轮用材质上的旧通用金属贴图叠乘，不增加自发光。FinishWheelMaterials在走行部与整车最终材质阶段调用，避免后续重建覆盖回旧参数。

沿用NF5KBogieDetailReview更新3份既有场景，保留驾驶交互；新图为`tools/cab_reference/interior_review/ladder_wheel_refined.png`、`ladder_wheel_closeup.png`，与上轮bogie图保留对照。验证日志`tools/compile/cab_ladder_wheel_finish_final.log`。机械设计仍需后续检查转向架转角、悬挂全行程及人员攀登姿态，当前模型固定连接不等于工程认证。

2026-10-21。本轮精修现有两组双轴动力转向架，不改变头车外形、涂装、客席布局或已接入的驾驶交互。

## 依据与边界

参考[SKF轴箱设计资料](https://cdn.skfmediahub.skf.com/api/public/0901d196801410ca/pdf_preview_medium/0901d196801410ca_pdf_preview_medium.pdf)的轴箱、轮对、构架载荷传递关系；[Knorr-Bremse执行机构介绍](https://rail.knorr-bremse.com/en/gb/portfolio/products-and-systems/braking-systems/actuation/)及[TreadAct Compact资料](https://rail.knorr-bremse.com/media/2000_products/2900d_product_broschures/p-1302_treadact_compact.pdf)核对踏面制动、制动缸和安装接口的分工。本项目不复刻厂商型号。

原参考图没有底盘工程图，本轮尺寸与构架方案均属项目可视化候选，无强度、疲劳、悬挂行程或制动力传递认证。

## 变化

- 连续方梁改为中段侧梁、抬高的轴端弹簧横梁与斜接腹板，显露弹簧与轴箱的连接关系。
- 16根一系弹簧各有上下承座，共32个；补轴箱弹簧翼座、导向角和端盖6螺栓。二系连接使用独立橡胶承垫和金属压板。
- 8个方块闸瓦替换为弧形踏面闸瓦，增加背托、吊架、横向支承、销轴、推杆、制动缸候选包络与支架。闸瓦内半径467mm、外半径505mm、覆盖36°，与约455–462mm踏面半径保留静态间隙；没有驱动闸瓦实际压紧。
- 电机和齿轮箱从Driven_Wheelset中分离，改为4个Non_Rotating_Axle_Drive同级组，避免以后转轮时将静止外壳一起旋转。车轴、钢轮和轮毂留在轮对下；未实现弹性悬挂或轮对动画。
- 钢构件增加毫米级倒角，分离低反光摩擦材料、橡胶、加工五金和旧化钢架。

## 接入与验收

NF5KRunningGear.Build为持续生成入口；NF5KBogieDetailReview.Prepare仅替换CabInteriorBlockout、CabDaylightReview及CabSimulationReview中的Running_Gear，并继承父节点渲染层，保留驾驶输入和灯光。

批处理NF5KBogieDetailReview.BatchBuild保存并重新加载场景，检查2转向架/4轮对/8钢轮、8闸瓦、32承座和4静态驱动组，之后运行现有驾驶Play Mode验收。柔光近景中的辅助光源不保存为车灯。

图片：`tools/cab_reference/interior_review/bogie_detail_refined.png`与`bogie_suspension_brake_detail.png`。结果以`tools/compile/cab_bogie_detail_final.log`中的NF5K_BOGIE_DETAIL_OK、NF5K_CONTEXT_INTERACTION_OK、NF5K_SIMULATION_PLAYMODE_OK及实际图像检查为准。

仍缺完整管路/风缸、撒砂装置、底盘设备功能定型、轮轨接触、悬挂动力学、轮对动画、全行程碰撞及LOD/实例化性能优化。不能从构件数量通过推断机械设计认证。
