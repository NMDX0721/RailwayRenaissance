# 司机控制器手轮总成：首版分件灰模

## 用户要求定稿：克制岁月感

当前美术定稿资产为`Assets/Models/Cab/controller_handwheel_final.glb`。用户要求增加岁月感后最终优化、定稿；保留已接受的轮廓和暂定三辐，不再改动外形。新增`tools/cab_reference/finalize_controller.py`，从refined.blend可复现生成：轮圈不均匀握持磨亮、少量浅擦痕、底板边缘轻微漆损、轴座与板边低强度灰尘。没有大面积锈蚀或未经依据的文字标识。磨损是美术处理，并非原车逐点复原。

底板采用整体XY坐标映射，轴座使用连续圆柱映射，避免逐面重复污痕；轮圈沿原有环形UV处理。三个老化材质分别输出基础色、粗糙度和金属度贴图，保留微法线。GLB静态复核：17网格、20628三角面、7个可动子件；4个表面材质均具基础色/金属粗糙度/法线贴图，另有孔内暗面材质；12张内嵌贴图，无相机，约3.17MB。顶点有限性与面数预算断言通过。

源文件：`tools/cab_reference/controller_review/controller_handwheel_final.blend`；预览`controller_final_reference.png`、`controller_final_top.png`；回执`final_audit.json`；新生成的岁月感贴图位于`Assets/Models/Cab/Textures/ControllerFinal`，基础微法线与钢材图仍来自Controller目录并内嵌GLB。历史候选保留。美术定稿不等于历史型号、尺寸及辐条数量得到考证；尚未进行Unity导入、交互行程或碰撞验收。

## 黑漆材质候选版

新增tools/cab_reference/refine_controller.py，以第二版BLEND为输入，修正自定义旋转体及其他网格法线方向，为轴座台阶、轮毂与孔口增加0.00045米/3段小倒角。三辐、轮圈和总高维持第二版；未增加原图不可辨认的铭牌或换向手柄。

轮圈使用较低粗糙度黑色非金属表面，轴座和底板采用稍粗糙黑漆，紧固件为钢材；贴图仅提供轻微色差、粗糙度和微法线变化，不包含经照片定位的真实磨损。四套512×512 PBR贴图共12张位于Assets/Models/Cab/Textures/Controller，并内嵌于Assets/Models/Cab/controller_handwheel_refined.glb。GLB静态检查：17网格、20628三角面、12张内嵌图、4个完整贴图材质，无相机；旋转节点保留7个可动子件。GLB约3.39MB。

可编辑源：tools/cab_reference/controller_review/controller_handwheel_refined.blend；检查图controller_material_reference.png和controller_material_top.png；结构回执refined_audit.json。灰模和旧资产保留。具体车型、尺寸、辐条布局与机械行程仍未确认，未进行Unity运行验收。

## 第二版比例调整

用户接受按常见结构暂保留三辐方案；这不代表原车辐条数量已考证。通用三辐产品依据：https://www.wixroyd.com/wx/artnr/77100w0306 。对照原图，将旋转节点高度从0.086降至0.074米，轮毂厚度从0.020收至0.014米、最大半径从0.034收至0.030米，同步缩短固定轴并下移中心盖与紧固件。尺寸仍为视觉估计。重新输出同名GLB、BLEND及双视角灰模预览，继续保留原始throttle_controller.glb；未接入Unity。

负责人：Codex。2026-10-19 用户要求推进下一个3D组件，当前选择琴键旁的手轮/控制器总成；具体零件型号尚未确认，不将名称当成实物型号鉴定。

## 已核对的依据

- 实际灰模检查结果：existing_model_clay.png显示原资产是另一种机械总成外形，缺少原图的大手轮，主体轮廓也不一致；不据此鉴定其真实机械名称。当前决定不精修/直接复用该单网格，转为依照片重新搭建分件灰模。文件名不作为型号或形体正确的证据。

- 外观基准：tools/cab_reference/crop_levers_piano.jpg，对应DF4-0001驾驶室实拍；和已认可琴键使用同一原图。原始摄影页面：https://commons.wikimedia.org/wiki/File:China_Railways_DF4_0001_Cab_201110_128.jpg 。
- 已有throttle_controller.glb为单网格、422384三角面、无内嵌贴图，约10.14MB。先生成灰模检查图判断是否可用，不直接覆写。
- jz7_brake_valve.glb同为单网格（327864三角面），作为后续另一组件候选，不与当前控制器混用。
- crop_brake_area.jpg实际显示屏幕与杂物，不是可靠的制动阀细节参考；不能按文件名直接开模。

## 本轮实施顺序

1. 通过inspect_controller.py生成现有模型灰模与结构回执，输出controller_review/existing_model_clay.png及existing_model_audit.json。
2. 对照照片核对手轮轮廓、轮缘截面、轮毂、轴座、安装板及空间比例；具体型号、遮挡结构和工程尺寸继续核实。
3. 先完成可分件的低面数灰模，将可动手轮与固定底座分离，确定旋转轴；不沿用单网格作为已完成交互的依据。
4. 外形稳定后再复用琴键的黑色漆面/钢材PBR工作流；保留原始资产，新增候选，不调用付费生成，不向外部AI发送项目素材。

## 当前交付

- 本地脚本：tools/cab_reference/build_controller_gray.py，可重新生成灰模、GLB、BLEND和两张检查图。
- 新候选：Assets/Models/Cab/controller_handwheel_blockout.glb；旧throttle_controller.glb原样保留。
- 编辑源：tools/cab_reference/controller_review/controller_handwheel_blockout.blend。
- 检查图：controller_gray_reference.png（低位斜视）、controller_gray_top.png（俯视）。
- 结构回执：blockout_audit.json；共17个网格、11412三角面。
- Handwheel_Pivot统一管理轮缘、轮毂、辐条、中心盖与紧固件；固定底板、轴座、轴、套环和右侧空孔座单独保留。GLB仅导出组件，不含预览桌面、灯光和相机。
- 照片无法清楚证明辐条布局，本轮暂用三辐并标记spokes_provisional，不能将其当作已考证事实。形体、工程尺寸与操作行程均待核对；各独立组件的估计尺寸也不代表统一驾驶台工程比例，组装时需再校准。

当前完成首版灰模，尚未做最终贴图材质、Unity导入或实际交互接线；继续在当前对话对照原图迭代。
