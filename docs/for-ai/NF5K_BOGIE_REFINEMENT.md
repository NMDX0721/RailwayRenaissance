# NF-5K 转向架细化

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
