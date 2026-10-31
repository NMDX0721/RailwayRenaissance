# 大小闸配色与按钮间隙修正

2026-10-25，基于用户指出的小闸与新按钮底板接触、制动阀绿色桶体风格突兀。

## 位置修正

实际Unity Renderer包络审计存于`tools/compile/brake_layout_audit.json`：原按钮底板x=0.812..0.946 m，小闸最近安装耳x=0.940..0.956 m，存在约6 mm横向包络重叠。之前仅验台面支承与风扇底板不相交，未覆盖相邻大小闸，不能据旧验收声称无接触。

`NF5KServiceControlSetup`将底板缩为110×60×3 mm，并调整至x=0.803..0.913 m、z=0.276..0.336 m。小闸安装耳与底板横向间隙约27 mm。按钮中心间距改48 mm，印字和四角螺丝随底板重排。保留原19 mm帽、2 mm行程和交互/声音路径。

生成时检查四角实际台面网格支承、风扇底板不重叠，并将底板包络在水平各侧扩15 mm、竖直各侧扩3 mm后与Tall/Short零件包络逐一比较；`NF5KBrakeRefinement.Validate`在实际Play驾驶姿态再次检查。该检查针对台面附近实体间隙，不声称完整人体工学、手部动画或所有未来手柄姿态扫掠已验收。

## 材质统一

`NF5KBrakeRefinement.Apply`由`NF5KCabFittings`统一调用，对`Brake_Valve_Photo_Study`内两只阀的57个材质槽显式分配：

- `NF5K_Brake_Coat`：深冷灰涂层铸件，低金属度和低光泽；替换原CastSilver贴图色，消除绿色桶体。原铸造微法线保留但减弱。
- `NF5K_Brake_Steel`：机加工圈、盖及连接件，钢灰底色、较高金属度，和涂漆阀体分开。
- `NF5K_Brake_DarkSteel`：较暗紧固件，避免整机亮银或统一颜色。
- `NF5K_Brake_Grip`：深炭灰握把；`NF5K_Brake_Seal`：中性深色密封/连接缝。

`build_brake_finish_maps.py`生成原创确定性底色与Metallic/Smoothness贴图，位于`Assets/Models/Cab/Textures/BrakeRefinement`，不经Resources像素图导入规则。轻微颗粒/色差只用于材质，不影响世界种子或存档。新材质单独保存，保留原模型几何、手柄枢轴、源FBX与旧材料；四检查场景通过生成流程应用，独立旧制动阀资源预览不自动重写。

## 2026-10-26 实物参照与分层材质重制

用户复核指出第一版仍有塑料感，因此上一节配色调整不代表材质视觉终稿。

实物参考（浏览器实际查看图像）：

- 重庆Xinrail供货商JZ-7照片：灰色漆面有细密橘皮起伏和连续反光，手柄/轴与涂漆壳体反射明显不同。供货商页面仅作照片参考，不以其认证/压力/温度营销文字作为设计数据。https://xinrailtech.en.made-in-china.com/product/btdRzCQEZjkv/China-Train-Rolling-Stock-Brake-Jz-7-Valve-Individual-Brake-Valve.html
- Wabtec官方26C照片：黑色涂层仍有高光和铸件起伏，不能将“涂层金属”理解为全哑光。https://www.wabteccorp.com/locomotive/braking-systems/pneumatic-controls/26c-brake-valve

同时重新查看用户原始1000268232.png：主阀体以旧金属反光为主。最终主要Casting、Flange、Cap_Ear使用CastMetal层，局部支承件保留Coat层；现代商品照片仅补充微表面和加工反射差异。未复制照片作游戏贴图，也不宣称现有双阀为现代JZ-7或26C精确型号复刻。

`build_brake_optical_maps.py`生成V2底色/金属光滑度/微法线：主铸件恢复旧金属反射（CastMetal），局部漆层有半光泽和细橘皮，钢件使用较清晰的加工微纹，握把保留深色且有使用磨亮区域。`BrakeLayeredMetal.shader`通过URP的UniversalFragmentPBR计算主光/附加光、阴影、SH漫反射与反射探针，而非用固定亮色伪造金属。漆层使用烘入UV1的物体局部米制坐标做三向投影，避免原UV岛尺寸差异及列车运动时纹理滑移。

每个原网格保存独立原位副本（57个材质槽显式分配，新增CastMetal类别），顶点色编码边缘倾向、底缘接缝积污及握把磨亮。稀疏露底限制在边缘遮罩内，首轮过重斑点已降低，不用满面随机锈点。顶点、法线、UV0和三角形保留；另写颜色与米制UV1，并重建切线和包围盒。原FBX和驾驶控制枢轴不修改。

间隙验收改为将真实网格顶点变换到驾驶台局部坐标后比较：车辆行至弯轨时，世界轴AABB扩大曾导致小闸假接触报错。局部坐标检查保留15 mm水平扩展余量，不通过继续缩小组件来掩盖误报。新Shader使用target 3.5，实际设备验证仍以本次Windows/D3D11为准，不宣称Android已验收。

本轮最终验证：tools/compile/brake_optics_v5_stdout.log退出0，四场景重建，NF5K_BRAKE_REFINEMENT_OK及NF5K_SIMULATION_PLAYMODE_OK通过；Shader编译无报错。早期v1/v3/v4的间隙失败是旋转世界AABB及FBX导入缩放导致的检查坐标错误，不作为本轮通过证据；最终使用车体旋转坐标、保持米制距离。早期元数据写入警告在后续按贴图去重设置后未再出现，最终贴图设置实际保存。tools/compile/brake_optics_capture_final_stdout.log退出0，NF5K_BRAKE_REFINEMENT_CAPTURE_OK；人工核对brake_finish_pair_v4.png与brake_metal_detail_v1.png：主要铸件金属高光、加工盖边缘高光、细微表面与握把层次可辨，沿用驾驶室检查光照而非画后期高光。早期v2/v3图为材质试验，不作为最终表面版本。

### 配色细化：暖锡灰与冷钢灰

2026-10-26按用户继续优化配色：CastMetal底色从RGB130/133/135调整为144/142/136，主要桶身偏暖锡灰；Steel从150/155/158调整为173/177/180，机加工件偏亮冷钢灰；Coat改79/86/90深石墨灰；Grip改25/22/20暖黑。DarkSteel使用较深冷色调制，拉开紧固件与主体的视觉层次。保持此前金属度、光滑度、微法线、磨损遮罩及交互逻辑。

`NF5KBrakeRefinement.PaletteBatch`重载并保存共享材质/贴图，覆盖现有四场景所引用的同一资源；没有复制四套配色。`tools/compile/brake_palette_v2_stdout.log`退出0，材质/15 mm间隙检查与实际渲染通过。人工核对`brake_metal_detail_palette_v1.png`，主要铸件暖灰和加工件冷灰可辨，握把仍接近黑色；另存整组和驾驶台整体图。此前v1日志因项目占用退出，不计为验证通过。本轮仅配色及渲染验证，不新增驾驶功能或宣称重跑全部Play回归。

### 用户补充：加工件也不统一为冷钢色

继续检索并在浏览器查看旧Westinghouse使用件照片，观察亮银反光、深色表面斑驳和暗色握把共存：https://www.ebay.it/itm/143592984564 （图片：https://i.ebayimg.com/images/g/kJgAAOSwzTFerdWE/s-l1200.jpg）。照片可用于色差/磨耗参考，不能仅凭照片判定具体合金或污迹化学成分。

Science Museum Group记录1975-7134另有黄铜及未知金属、棕漆的Model 8剖切展品：https://collection.sciencemuseumgroup.org.uk/objects/co206327/westinghouse-brake-valve 。该展品的剖切/展陈涂色不作为运营机车外观依据，也未据此将本车零件随意改成黄铜。

本次终稿收回上一轮“所有加工件偏冷钢灰”的概括：Steel改为RGB168/166/161的中性偏暖银灰，保留光照产生的冷暖反射；Coat改为80/81/80中性炭灰；DarkSteel用暖深灰调制。Steel/CastMetal贴图增加低幅度局部暖色差，接缝遮罩中增加克制的暖褐压暗，保留磨亮处的银灰。属于参照旧件作出的美术推演，不声称精确测得油膜或氧化层颜色。

`tools/compile/brake_palette_reference_v1_stdout.log`退出0，Shader编译、材质/间隙检查和渲染通过。人工核对`brake_metal_detail_palette_v2.png`，盖圈不再统一泛冷蓝，主铸件、深色连接处和握把有层次；`brake_finish_pair_palette_v2.png`及整体图供对照。当前配色以这批v2图片为准。

## 2026-10-25 实际验收（历史）

`brake_refinement_v1_stdout.log`退出0：四场景更新，`NF5K_BRAKE_FINISH_OK`与`NF5K_SERVICE_BRAKE_CLEARANCE_OK`通过，既有驾驶回归通过。

`brake_refinement_final_stdout.log`退出0：独立重开后`NF5K_BRAKE_REFINEMENT_OK`确认两阀统一材质及实际驾驶姿态间隙；`NF5K_SERVICE_CONTROL_OK`、`NF5K_SIMULATION_PLAYMODE_OK`通过。没有修改动力/制动模拟核或改变大小闸操作逻辑。

`brake_refinement_capture_v2_stdout.log`退出0，`NF5K_BRAKE_REFINEMENT_CAPTURE_OK`。人工检查`brake_finish_pair_v2.png`、`brake_service_gap_v2.png`：涂层阀体、裸钢和黑握把可分辨，小闸安装耳与按钮底板之间可见台面间隙。`brake_console_refined_v2.png`保留整台协调对照。第一轮较浅灰配色图保留为历史，不作为最终配色。

## 2026-10-26 动漫驾驶台配色修订

用户指出旧银灰方案与整台风格脱节，当前版本以本节覆盖此前金属配色：主体灰青RGB104/140/134（涂层金属度0.16），支承与安装耳暖米灰RGB192/184/158，加工件银灰RGB174/181/178，握把暗红棕RGB91/43/35。沿用CastMetal资源名以保持场景引用，但该资源现在表达灰青涂层，不再表示裸露旧铸铁。按零件功能分色，降低漆层/握把法线与随机明度差，以清楚的色块、柔和高光形成动漫倾向；仍使用URP PBR受光，没有宣称实现全局卡通渲染。控制逻辑、网格轮廓和原有间隙不变。

PaletteBatch已更新共享材质；tools/compile/brake_anime_v1_stdout.log退出0，NF5K_BRAKE_REFINEMENT_OK与NF5K_BRAKE_REFINEMENT_CAPTURE_OK通过，无CS/Shader错误。人工核对brake_finish_pair_anime_v1.png及brake_console_anime_v1.png：与灰绿台体、米白表盘/琴键和红色控件形成颜色呼应。本轮没有重跑完整驾驶Play回归。

### 整台协调修订 integrated_v1

用户认为浅青/米白/红棕分色仍突兀。以整台实际渲染为依据，收掉高明度米白安装耳及红色握把：Coat为RGB69/78/72，主体75/86/78，金属加工件132/137/129，握把48/38/32；主体与支承仅小幅色差，采用现有驾驶台深灰绿/暖深棕色系，避免为追求动漫感额外添加装饰色块。微纹理仍保持上一轮的低幅度。不是全局卡通Shader改造。

tools/compile/brake_integrated_v1_stdout.log退出0，材质/15mm扩展间隙及渲染检查通过。人工核对brake_console_integrated_v1.png与brake_finish_pair_integrated_v1.png，整台远景优先于孤立近景。本轮未重跑完整驾驶Play回归。当前配色以integrated_v1替代anime_v1。

### integrated_v2收尾
保持integrated_v1深灰绿配色；主体光滑度0.36到0.40，握把0.31到0.36；边缘倾向遮罩0.35到0.24，底缘污迹0.30到0.20。减少小尺度杂乱，保留漆层、金属件和握把的材质层次。与停放选择器同批检查；当前验证见NF5K_PARKING_SELECTOR.md。

2026-10-27 integrated_v2实际图已补齐：brake_finish_pair_integrated_v2.png及brake_console_integrated_v2.png。同批四场景和Windows Play通过，日志parking_selector_reference_final_stdout.log；本地同版本依赖验证范围详见NF5K_PARKING_SELECTOR.md。
