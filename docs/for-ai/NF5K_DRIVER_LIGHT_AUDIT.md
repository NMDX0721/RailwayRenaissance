# 驾驶位原场景光源审计（2026-10-29）

NF5KDriverLightAudit.Batch拍摄CabDaylightReview晴/阴/低角度太阳的正常司机眼点，不调用ConfigureInspectionLighting。真实Play回归在CabRailMotionReview输出司机前视、俯视灯箱及脚部照片；该场景窗外仍是日光背景，不可标成夜间。临时组件检查补光会提高环境光并增加无阴影平行光，不能作为驾驶环境验收。

本轮发现：实际脚部区偏暗；台面灯具/支杆在前视下遮挡部分灯箱与速度表。光源调整前必须解决灯具安装/视线关系，不能通过提亮遮挡来冒充改善。真实夜间、灯具移动/限界以及脚部照明尚未完成，本轮未修改正式场景光强。

证据：tools/compile/driver_light_audit_v1_stdout.log退出0，完整驾驶Play通过；有UnityEditor.Search启动索引异常。图片位于tools/cab_reference/interior_review/driver_light_*.png。数据只为视图审计，无人眼适应、照度计或眩光标准合规结论。原包文件已还原。

2026-10-29白天修正：提高CabDaylightEnvironment天空/水平/地面间接光输入（晴天水平由0.20/0.24/0.27改为0.36/0.40/0.43），不增加无来源检查平行光。审阅新增同预设开驾驶室顶灯的对照图；正式开关和供电逻辑不变。实际结果待本轮核验。

白天v2回执：driver_light_brightness_v2_stdout.log退出0，驾驶回归通过，有UnityEditor.Search启动索引异常。原光源对照显示：间接光提高后改善有限；顶灯开启明显提亮驾驶台，但部分表盘/文字偏亮，且台面灯具遮挡仍在。因此不能视为光照最终验收；需要继续平衡顶灯直射、表盘高光及下部可见度。

2026-10-29灯具收薄：NF5KCabLightRefinement撤除仪表灯高支杆，仪表灯下移100mm、向仪表侧移120mm，外壳收薄至16mm；顶灯直射候选强度由7.5调至4、扩大柔和过渡，保留原回路与间接补光。四场景迁移及原生成入口同步；运行和实景结果待验证。

灯具收薄首轮回执：cab_light_refinement_v1_final_stdout.log退出0，完整驾驶Play通过。早期迁移误用旧子件名称已修正；UnityEditor.Search索引异常仍在。人工查看司机开顶灯图driver_light_Clear_ceiling.png；本轮非夜间照度/眩光认证，脚部低位灯尚未新增。

## 2026-10-30顶棚及上部空间修正
用户明确要房间顶部变亮。CabDiffuseFill为司机室z<2.05m且接收器中心高度1.3–2.1m的区域增加平滑权重的间接漫反射：日光环境提供向下表面的回光；CabCeiling回路开启后增加顶棚方向的漫反射。仍是原生SH艺术近似，不是烘焙/光追GI，也不新增仪表台直射灯；不改变后端和回路开关。
NF5KUpperCabLightingReview使用固定向上眼点、正常供电与顶灯预设，在阴天和移除太阳/天空盒的独立夜间审阅中分别拍upperCabBounce=0/1配对图，不保存审阅环境到正式场景；实际结果待核对。

顶棚最终回执：首轮增益1导致白天过亮，已将默认/审阅增强值降至0.30。tools/compile/upper_cab_light_final_stdout.log退出0，NF5K_UPPER_CAB_RENDER_OK和完整驾驶Play通过。固定顶棚ROI(x150..1100,y60..280)截图灰度均值：白天161.0→199.46，夜间顶灯开启136.04→165.21，均为0–255图像读数，不是照度。人工查看cab_upper_day_after.png与cab_upper_night_after.png，顶棚变亮且未整片接近白色；侧壁/风扇暗部仍有改进空间。白天/夜间均使用审阅主电源与顶灯开启，未验夜间断电SH图像；原有驾驶/供电回归通过。未改仪表台直射光，新增项只作用于上部接收器。UnityEditor.Search启动索引异常仍有，非零异常日志。manifest/lock已恢复且无内容差异。

2026-10-30顶棚细化：NF5KCabCeilingDetail增加六块薄内衬板及真实分缝、下向防脱紧固件、两侧边缘收口和闭合检修盖。原结构顶板保留，新增饰面厚5mm靠结构层安装；材质独立，新增不透明面接入原回路控制的SH补光。检修盖尚无开启交互，未虚构通风或电气系统。四场景与昼夜图待核对。

顶棚细化回执：cab_ceiling_detail_final_stdout.log退出0，四场景生成，昼夜上部与后侧检修盖图已人工查看，完整驾驶Play（含风扇/灯光回归）通过。新增接收器范围限制在车内，已接ConfigureSurfaces重建入口。图cab_upper_day_after.png、cab_upper_night_after.png、cab_upper_rear_detail.png。检修盖无交互，饰面板不是新增结构承载件；未作顶棚完整碰撞/维修人因认证。UnityEditor.Search启动索引异常仍有；临时依赖文件恢复。

2026-10-30灯具与标识：顶灯增加端盖、压边固定螺钉及独立乳白罩材质/渐变发光图，光源强度与供电不改；检修盖新增小号双语‘顶棚检修口 · C-01 / CEILING ACCESS’，属于原创维护编号，无高压标志。参考Matrix608与LPA铁路灯具的金属壳/扩散罩组成，非实物复制。实际昼夜图待核验。

灯具与检修标识最终回执：ceiling_luminaire_detail_final_stdout.log退出0，完整驾驶Play通过；人工核对cab_upper_rear_detail.png灯罩渐变、端盖紧固件及朝下双语标识。首轮文字印面方向错误已修正。检修口不开启，灯具内部驱动和线路仍未建模；UnityEditor.Search索引异常仍有。

2026-10-30灯具v3：检修口标识改为280×105mm居中双行，中文主标题加大、编号并入英文行；灯罩两侧补连续压边条，端盖螺钉改为7mm头。审阅补近景文字图，实际结果待核对。

灯具v3验收：ceiling_detail_v3_stdout.log退出0，驾驶Play通过；人工核对后部总览及cab_access_label_close.png，中文标题与编号可辨。原有UnityEditor.Search启动索引异常仍在。检修盖交互、灯具内部线路未实现。

v3补正：近景发现标识从司机侧倒置，已在盖板平面旋转180度。ceiling_detail_v3_final_stdout.log退出0，驾驶Play通过；近景重新核对阅读方向。

2026-10-30灯具v4形体重整：隐藏原45mm厚黑盒及旧压条，新建22mm薄灰色壳体，8mm乳白罩上收27.5mm；端盖和压条重新对齐，固定头改6mm。保留原挂接位置/回路，光源位置同步到罩下方。重复迁移按实际网格尺寸取值。实际结果待验证。

灯具v4回执：ceiling_lamp_shape_v4_stdout.log退出0，完整驾驶Play通过；人工核对cab_upper_rear_detail.png，旧厚黑盒已隐藏，新灰色壳、乳白罩及端盖可辨。尚未进行内部驱动/线路建模或照度认证；UnityEditor.Search启动索引异常仍有。

灯具v4安装补正：用户指出穿入顶棚，原因是原倾斜灯具姿态未独立校正而灯罩又上收。现壳体平行内衬，安装原点车体y=2.095m，壳顶约2.1395m，饰面底约2.1425m，保留约3mm间隙；光束方向独立保留。新增实际壳体顶点间隙断言，待实景验证。

2026-10-30灯壳安装收尾：壳体姿态改平行顶棚后，新增四个3mm安装垫座连接壳顶与饰面底，新增侧面近景检查。上一轮ceiling_lamp_clearance_final_stdout.log退出0且驾驶回归通过；本轮待核对。

安装收尾回执：ceiling_lamp_mount_final_stdout.log退出0，实际壳体顶点间隙断言与完整驾驶Play通过，cab_lamp_mount_close.png侧面近景已人工查看，壳体位于顶棚下方、垫座连接安装间隙。仍有UnityEditor.Search启动索引异常；未验证内部紧固/电气连接或机械强度。

2026-10-30侧缘黑缝修复：原平顶内衬半宽1.30m，外壳顶侧带位于约1.45m，窄收边没有封闭二者之间的空腔。新增两侧闭合楔形内收板，连接内衬边缘与顶侧带，覆盖风扇背后的黑缝；保留风扇位置。补同部位近景cab_fan_ceiling_joint.png，待核对。

侧缘修复最终回执：首轮直线收口近景仍暴露前窗圆弧转角，因此增加沿车头圆弧及8度前窗倾斜轮廓的闭合前收口，与两侧楔形内衬相接。ceiling_cove_complete_verified_stdout.log对应Unity退出0；人工查看cab_fan_ceiling_joint.png，原黑带及三角露空已消除，风扇挂点保持不变。四个检查场景已保存，风扇交互/供电/顶棚接触及完整驾驶Play通过，无error CS。UnityEditor.Search索引异常仍存在，非零异常日志不可称全清；临时包配置已还原。沙箱内前次运行许可证初始化失败，后续沙箱外复核成功。
