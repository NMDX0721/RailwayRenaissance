# 独立速度表：首版外形

## Unity资源接入

用户要求接入Unity后继续下一个组件。新增`tools/cab_reference/export_speedometer_unity.py`导出`Assets/Models/Cab/UnitySpeedometer/Speedometer_final.fbx`、显式材质清单和URP贴图；金属度R/光滑度A打包，基础色sRGB、法线/数据贴图线性。

新增`Assets/Editor/CabSpeedometerAssetSetup.cs`，菜单RailRevival/驾驶台/导入速度表定稿，生成`Assets/Resources/Cab/SpeedometerFinal.prefab`和`Assets/Scenes/CabSpeedometerReview.unity`。玻璃改为URP/Lit透明近似，不宣称等同Blender折射；保留独立指针与倾斜面框节点。当前批处理执行与验证记录在`tools/compile/cab_speedometer_import.log`，完成状态以日志成功标记为准。未接主驾驶台实际速度读数。

Unity批处理最终成功退出（return code 0），日志`CAB_SPEEDOMETER_IMPORT_OK`；导入包围盒约0.18×0.19×0.12米、31个Renderer，预制体及检查场景已存在。Unity渲染检查图`tools/cab_reference/speedometer_review/unity_speedometer_review.png`。批处理默认未保存空白场景采用Single新建，交互式编辑器仍采用附加方式保留原场景。无本轮C#编译错误。

## 用户授权的最终美术稿

新增`tools/cab_reference/finalize_speedometer.py`，保持已调整的后仰面框、收分后壳、薄底脚和分材质金属/旧漆。补底脚垫圈与固定螺钉、铭牌铆钉、窄面框密封线、后盖螺钉与线缆接头。后部服务细节属于用户允许发挥的设计，并非照片确认；不添加假厂家/序列号。合并静态刻线、数字、单位与面板螺钉槽为一个打印层网格，指针保持单独旋转节点。

最终资产`Assets/Models/Cab/speedometer_final.glb`；编辑源`tools/cab_reference/speedometer_review/speedometer_final.blend`；预览`speedometer_final_reference.png`；回执`speedometer_final_audit.json`。GLB无灯光/相机，贴图内嵌，顶点有限性/面数预算及独立指针检查通过。玻璃透射需查看器支持，后续Unity须单独配置URP玻璃。美术定稿不等于历史型号、7度倾角、150量程与工程尺寸已确认；尚未接Unity读数或场景。

## 箱体轮廓复核：后仰面框与收分后壳

用户指出箱体不是规则长方体。重新观察DF4-0001原图，当前直立、等厚长方体不能充分表达面框/后壳和薄底脚关系；但单张透视照片不足以量出真实倾角，不能把图像斜边直接视为工程斜度。

新增`tools/cab_reference/reshape_speedometer.py`，以小幅7度后仰作为候选：面板/表圈/玻璃/数字/指针归`Fixed_Inclined_Front_Assembly`整体倾斜，避免只斜表盘造成错层；后壳前面贴合该斜面，后部略收窄（宽度乘0.955）、后半深改0.036米；底脚厚度乘0.58并略缩外伸。独立指针继续保留。7度及后壳收分均为美术估计，并未从同类产品图纸证明其为本车型真实尺寸。

同类产品一手资料：Darpol自己的Hasler零件目录 https://darpol.bydgoszcz.pl/wp-content/uploads/Katalog-Hasler.pdf ，只用于对照旧式铁路仪表分体装配类别，不将其直接认定为原车型号或倾角依据。

新候选`Assets/Models/Cab/speedometer_tapered_refined.glb`；源`tools/cab_reference/speedometer_review/speedometer_tapered_refined.blend`；预览`speedometer_tapered_reference.png`和`speedometer_tapered_side.png`；回执`speedometer_tapered_audit.json`。旧版保留，未接Unity。

## 去塑料感：金属与细节第二次优化

新增`tools/cab_reference/polish_speedometer.py`，在原图细化版上分别设置粗糙旧漆壳、缎面金属前板、发黑钢表圈与钢螺钉，全部无清漆；箱体缩小倒角。刻线收细并压至表盘表面附近，数字移回薄印刷层高度，黄色中心由厚圆片收为薄层，缩小中心轴帽，避免按钮/塑料圆牌观感。四套新PBR基础色、粗糙度与微法线图位于`Assets/Models/Cab/Textures/SpeedometerMetal`，与原有盘面贴图一起内嵌导出。

增加0.0007米厚保护玻璃（IOR1.46、Transmission=1、粗糙度0.10），作为材质表现设计而非原图玻璃厚度考证。GLB的透射玻璃需要支持相关扩展的查看器，未来Unity导入需单独转换为URP透明玻璃材质，不能假定默认导入保留效果。

新候选`Assets/Models/Cab/speedometer_metal_refined.glb`，源`tools/cab_reference/speedometer_review/speedometer_metal_refined.blend`，预览`speedometer_metal_reference.png`，回执`speedometer_metal_audit.json`。仍未确认历史型号与尺寸，未接Unity速度读数或场景。

## 2026-10-20 原图与同类实物对照细化

用户要求硬核风格、还原原图并参考网络同类件。原照片仍为DF4-0001：深灰绿方壳、浅色金属前面板、黑色圆表圈、米白盘、黄色中心和底部小铭牌位。新增`tools/cab_reference/refine_speedometer.py`，补前面板与接缝、四角一字螺钉、上沿包边、侧面服务堵头、几何数字和km/h单位，指针改为细尖轮廓，保持独立旋转节点。未增加不可辨认的厂家/型号文字，未增加无依据的电子显示或现代发光设计。

量程0–150及260度刻度弧是原图视觉估计；同类150 km/h表存在并不证明原图具体型号或标定。细小背面结构、螺钉位置和尺寸仍为美术近似。壳体采用深灰绿哑光漆，面板为偏暖旧金属，盘面略旧米白；九张512×512基础色/粗糙度/法线贴图内嵌GLB。

网上一手来源：
- FNM铁路博物馆自己的Hasler机械速度记录仪藏品页（库存0146，1928–1958）：https://museo.ferrovienord.it/en/header/archive-library-and/museum-industry-and-labour-saronno/mechanical-tachograph-speedometer/ 。该藏品量程为100 km/h，仅比较老式箱体/机械仪表构成，不复制为DF4原车型号。
- 保加利亚交通调查机构2022年原始报告第29页/图4.1、4.2说明Hasler RT9/A16及150 km/h范围：https://www.mtc.government.bg/sites/default/files/documents/2022-12/Final%20report%207623_NIB%20ENG.pdf 。已读文本；网页截图获取失败，不声称已从截图确认具体形体。

新候选`Assets/Models/Cab/speedometer_photo_refined.glb`；可编辑源`tools/cab_reference/speedometer_review/speedometer_photo_refined.blend`；预览`speedometer_refined_reference.png`、`speedometer_refined_front.png`；回执`speedometer_refined_audit.json`。未接Unity速度读数或场景，尚未定稿。

用户要求制动阀导入Unity后继续下一个组件。选取DF4-0001原图右上方的方壳圆盘式速度表。

新增`tools/cab_reference/build_speedometer_gray.py`，生成方壳、底脚、黑色表圈、米白表盘、黄色中心、刻线与独立指针节点`Speed_Needle_Pivot`。工程尺寸、刻度分布和针零位均为照片估计，刻度数字/单位、玻璃与铭牌文字暂未添加，避免把不清晰信息做成考证结果。

交付`Assets/Models/Cab/speedometer_photo_blockout.glb`，69网格；可编辑源与预览`tools/cab_reference/speedometer_review/speedometer_photo_blockout.blend`及`speedometer_gray_reference.png`。仅为首版轮廓，待进一步核对实物；未接入Unity速度读数或场景。
