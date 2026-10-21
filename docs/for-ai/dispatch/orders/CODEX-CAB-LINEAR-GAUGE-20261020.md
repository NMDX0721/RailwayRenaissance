# 长条电气仪表组：单只模块首版

## Unity导入与读数绑定（2026-10-20）

新增导出入口 `tools/cab_reference/export_linear_gauge_unity.py`、编辑器入口 `Assets/Editor/CabLinearGaugeAssetSetup.cs`（菜单RailRevival/驾驶台/导入电流表定稿）。原生FBX、显式URP材质及贴图位于 `Assets/Models/Cab/UnityLinearGauge/`；目标预制体 `Assets/Resources/Cab/LinearCurrentGaugeFinal.prefab`，验收场景 `Assets/Scenes/CabLinearGaugeReview.unity`。

新增 `Assets/Scripts/Cab/CabLinearCurrentGauge.cs`，通过BindEngineState读取现有CabEngineState.TractionCurrentAmps，LateUpdate更新；支持SetCurrentAmps单体显示。独立轴向标定空节点处理导入坐标变换与手性，指示件的位置及倾角沿刻度圆弧变化。显示0–1500 A，保留原始电流和超量程标志；无效浮点信号回零并标记无效。未改变CabEngineState动力学。

编辑器入口核验301个读数的位置及转角、上下越量程、NaN，以及发动机状态绑定和后续帧刷新。验收场景保存对独立ReviewEngineState的引用；主驾驶台整组布局尚未建立，不能把此单体接入称为整台驾驶台已完成。玻璃使用URP透明近似，非Blender透射效果逐像素复现。

已有资产盘点与后续表类/操纵件计划见 `docs/for-ai/DF4_CAB_COMPONENT_PLAN.md`；下一优先为双针空气压力表组。

验收记录：最终日志 `tools/compile/cab_linear_gauge_acceptance.log`，301位置/转角、上下超量程、无效输入、状态绑定及刷新通过，最大导入坐标误差约6.13e-8米；23个Renderer，包围盒约0.05×0.18×0.04米。重载保存的验收场景后截图成功，图为 `tools/cab_reference/linear_gauge_review/unity_linear_gauge_review.png`。首次导入发生元数据写入失败与尚未稳定的预览；重导入后元数据完整。普通批处理在生成资源后退出阶段曾崩溃，日志另见Burst缓存Sharing violation；最后仅对验收进程使用`--burst-disable-compilation`，未再出现崩溃，未修改项目Burst设置。不能据此认定常规Burst退出路径已经修复。

## 最终美术稿：直流电流表设计选型

用户确认进行最终质感与科学性整理。最终资产 `Assets/Models/Cab/linear_gauge_final.glb`，入口 `tools/cab_reference/finalize_linear_gauge.py`，源/图/回执为 `tools/cab_reference/linear_gauge_review/linear_gauge_final.blend`、`linear_gauge_final_reference.png`、`linear_gauge_final_audit.json`；最终新增贴图位于 `Assets/Models/Cab/Textures/LinearGaugeFinal/`。23网格、6151三角面，保留独立运动节点。

黑绿旧漆外壳及独立压框使用非金属涂层响应、低强度微法线和轻微粗糙度差异，避免拉丝木纹观感和塑料亮面；螺钉与导轨为高金属度缎面钢，指示件为暗红哑光涂层，纸白盘面降低法线，玻璃保留真实厚度和边缘反射。漆层非金属参数不表示壳体为塑料，而是表面电介质涂层；最终贴图可随GLB导出。

功能身份定义为直流电流显示设计选型，0–1500 A；原照片的用途和量程依然未确认。刻度/运动几何继承前版301位置检查，位置与单位一致；本轮仅改材质，不改变通过检查的几何。建议测量链为一次电流→外部分流器或传感器→小信号表头输入，输入额定值/变比待选，不可将1500 A理解为流经窄型表头。模型未新增假定的电气接口，未声称已实现电磁驱动。日后显示输入夹取到0–1500 A，越量程诊断由模拟层处理，满刻度不等于安全连续电流或过载许可；不印未经验证的精度等级及红线。

本次定稿为游戏美术及读数规格定稿，内部弧轨是自主设计的显示运动方案，非原车动圈表芯考证或工程认证；未接Unity、未接仿真电流。此前候选文件保留以供回溯。

## 当前候选：有量纲刻度与框上固定螺钉

用户要求刻度内容科学化，并纠正螺钉应位于压框上。入口 `tools/cab_reference/upgrade_linear_scale.py`，输出 `Assets/Models/Cab/linear_gauge_calibrated.glb`；源/预览/几何回执为 `tools/cab_reference/linear_gauge_review/linear_gauge_calibrated.blend`、`linear_gauge_calibrated_reference.png`、`linear_gauge_calibrated_audit.json`。此版取代下文上一候选的螺钉位置。

两颗螺钉落在凸起压框上下实体边，中心z=0.015/0.163米，头部与槽口前移至框前；检查头部半径完整落在实体边内，不进入玻璃开口。这表达压框紧固于后方框座的装配关系，尚未建螺纹及验证预紧力。

采用自主选型的直流电流刻度：0–1500 A，底部零、顶部满量程；每小格50 A、每250 A标数字，31条刻线、7组数字，补A与DC。刻线沿弧长均分，指示件位置同步使用同一角参数，展示读数450 A。301个全量程位置检查指示轮廓不越窗；数字网格顶点检查位于窗口内。分格数不代表测量准确度，不印未验证的精度等级、厂牌和过载红线。

选型参考：TE原厂CI-E243目录 https://www.te.com/en/product-CAT-AN243.html 列出0/1500 A等量程及直流电流表、分流器输入配置；Tsuruga https://www.tsuruga.co.jp/foreign/products/e/ 提供动圈窄型电气仪表依据。它们不证明照片仪表就是该量程或该型号。本模块可作为牵引电机电流显示候选，但未确认原车用途；大电流不得理解为流经表头，分流器/传感器及输入额定值尚未选型。内部弧轨仍属视觉机械设计，不是已考证的动圈表芯，尚未接Unity或实际信号。

## 当前候选：共享基线、凸起压框与嵌入玻璃

入口 `tools/cab_reference/refine_linear_bezel.py`；输出 `Assets/Models/Cab/linear_gauge_raised_bezel.glb`，源文件与预览为 `tools/cab_reference/linear_gauge_review/linear_gauge_raised_bezel.blend`、`linear_gauge_raised_reference.png`。

将原先分离的导槽移到刻度基线，移除重复基线，背面双导轨和连接杆同步移动。新增真实开孔的凸起金属压框，开口跟随左弯刻度窗；压框前表面比玻璃前表面突出约2.95毫米。玻璃厚1.2毫米，IOR 1.50、粗糙度0.045；预览增加条形反射灯，该灯不导出到模型。上端固定螺钉及槽口上移5.5毫米，避免被新压框遮住，名义边缘间隔2.4毫米；玻璃背面与指示件前侧名义间隔1.95毫米。

导出14个网格、5502个三角面。此前101位置指示轮廓检查的窗口曲线与宽度保持一致；以上间隔为设计尺寸计算，并非完整实体碰撞或机械认证。原车内部机构与驱动仍未考证，本版仍是自主设计候选，尚未接入Unity。

## 右向刻度、宽指示端、可达全量程的设计方案

用户要求刻度朝右、玻璃可辨、指示端加宽，并允许参照确定的同类件和自由设计但需物理合理。进一步核对Weschler原厂资料 https://www.weschler.com/wp-content/uploads/2018/05/055.pdf ：明确说明动圈式edgewise机构，以及指示边缘和刻度位于同一弧面。此证据不能把DF4原车认定为滑块式，更不能把资料中可抽出的机芯抽屉误认成读数滑块。

新增`tools/cab_reference/design_linear_indicator.py`，本版明确采用用户授权的自主设计：背面双弧形导轨约束的指示滑块，细连接杆穿过刻度板导槽，前侧宽锥形红指示端从左指向刻度基线，刻线位于右侧。模型包含真实导槽、双导轨和连接杆；外壳不需要容纳延伸到箱外的长摆臂。导轨驱动接口、驱动力、摩擦、装配公差及原车机构尚未验证，不称为实车完整电测机构。

指示件随z在0.026–0.152米移动，同时x=.001+bow(z)、绕本地Y转atan(slope(z))，读数尖端始终对齐弧形基线。对101个均匀位置解析检查指示端所有轮廓点都在刻度窗内、上下不越框且尖端误差为零；这是指定几何包络验证，不是全装配实体碰撞、动力或承载试验。连接杆与指示体加宽部分相接。

玻璃改为外侧直矩形厚0.0012米的独立透明保护片，边缘小倒角，IOR1.48、粗糙度0.065，用边缘高光显示玻璃存在；曲线仍位于内部刻度区。保留旧漆、米白盘和更醒目的暗红色指示端。

交付`Assets/Models/Cab/linear_gauge_mechanism_refined.glb`，源和预览`tools/cab_reference/linear_gauge_review/linear_gauge_mechanism_refined.blend`、`linear_gauge_mechanism_reference.png`，回执`linear_gauge_mechanism_audit.json`。未接Unity，未确认原车型号、量程或读数机构。

## 弧线方向纠正及同类件复核

用户指出上一版方向反了；当前按用户近照要求改为向左弯，最大弓高-0.006米，并从抛物线占位改为连续圆弧，刻线沿弧线法向排列。刻度板、开口、玻璃轮廓与指示件同步调整，重生成同名curved候选与预览。实际曲率/机构仍未考证，之前右弯图不作为当前版本。

同类原厂资料新增Jewell Emico Series 13 https://jewellinstruments.com/products/meters-avionics/analog-panel-meters/emico-series-13/ ：产品区分曲面/平面前罩以及竖装/横装，可帮助区分“弧线刻度轮廓”和“罩面空间曲率”，不能直接证明原车玻璃形状。Tsuruga https://www.tsuruga.co.jp/foreign/products/e/ 提供窄型动圈/红色指示端等同类结构依据。保留旧式铁路原图的黑色表框、米白面与细指示端，不照搬现代产品外观。

## 用户近照：弧线刻度窗轮廓

用户提供1000268275.png，白色刻度区域的长边可见弧线，前版直矩形窗不贴近该轮廓。新增`tools/cab_reference/curve_linear_gauge.py`：刻度板、表框开口、玻璃轮廓及刻线共同采用轻微弧线，指示件随所在高度调整横向位置，保留外壳直线。最大弓高0.006米为视觉估计，不宣称真实曲率或将其理解成已确认的内部运动轨迹；该版调整的是正视轮廓，不是证明玻璃表面具有空间曲率。

交付`Assets/Models/Cab/linear_gauge_curved_refined.glb`，源`tools/cab_reference/linear_gauge_review/linear_gauge_curved_refined.blend`，预览`linear_gauge_curved_reference.png`，回执`linear_gauge_curved_audit.json`。量程/单位/机构仍待核实，旧候选保留，未接Unity。

## 用户要求开始后的实物对照与细化

一手同类资料：Beemet EW系列 https://beemet.com/ew-series/ 明确为动圈、指针偏转的edgewise仪表；Tsuruga产品页 https://www.tsuruga.co.jp/foreign/products/e/ 有竖装/横装、红色指示端的窄型仪表。它们支持“窄长外观不等于内部滑块”的判断，不能直接证明DF4原图型号或机构。当前撤销Reading_Pointer_Carriage的机械命名，改为中性`Reading_Indicator_Study`，不定义真实运行运动学。

新增`tools/cab_reference/refine_linear_gauge.py`，把实心前框布尔切出真实窗口，将刻度板与印刷层压回表框之后，细化刻线和指示端，补一字螺钉槽、薄玻璃及旧黑漆/旧米白材质。刻度仍不写未经确认的量程/单位，不默认整组用途一致。

新候选`Assets/Models/Cab/linear_gauge_photo_refined.glb`；编辑源与预览`tools/cab_reference/linear_gauge_review/linear_gauge_photo_refined.blend`、`linear_gauge_refined_reference.png`；回执`linear_gauge_refined_audit.json`。11网格、936三角面，6张内嵌PBR贴图与透射玻璃；未接Unity、未确认型号及内部机构。

用户要求速度表接入Unity后继续下一个组件。依据DF4-0001原照片中部窄长仪表组，先制作单只模块，未确认整组数量、仪表名称、量程、单位或内部测量机构。

新增`tools/cab_reference/build_linear_gauge_gray.py`，输出`Assets/Models/Cab/linear_gauge_photo_blockout.glb`；编辑源与预览位于`tools/cab_reference/linear_gauge_review/linear_gauge_photo_blockout.blend`及`linear_gauge_gray_reference.png`。

模型包含长条黑色表框、后壳、米白刻度窗、两端固定螺钉和独立指示件`Reading_Pointer_Carriage`。平移指示件仅为当前视觉占位，未确认原表内部是旋转指针、光带或其他机构；刻线未写数值和单位。尺寸为视觉估计。后续先核对原图/同类实物，再组合整组与制作玻璃材质；未接Unity读数。
