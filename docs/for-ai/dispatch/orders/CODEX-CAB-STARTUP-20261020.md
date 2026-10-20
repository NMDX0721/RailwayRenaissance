# 启动与主控使能组件：独立钥匙、带钥匙与空锁版本

验收完成：带钥匙31网格、空锁26网格、独立钥匙5网格；GLB结构确认钥匙与旋转锁芯共用转轴。Unity两版插拔/使能/燃油泵/启动停机联锁通过，无C#编译错误，编辑器正常退出。证据 `tools/compile/cab_startup_acceptance.json`、`cab_startup_final.log`。独立预制体阶段，尚未安装到主驾驶台。

用户明确要求可插拔物件不能合并死，制作三个独立资产：

- `Assets/Models/Cab/startup_controls_with_key.glb`：已插钥匙、关闭档。
- `Assets/Models/Cab/startup_controls_without_key.glb`：空锁孔、关闭档。
- `Assets/Models/Cab/startup_controls_key_only.glb`：独立钥匙，保留原装配坐标供插入复用。

源文件/预览在 `tools/cab_reference/startup_review/`，入口 `build_startup_controls.py`；原生FBX/材质位于 `Assets/Models/Cab/UnityStartupControls/`。Unity入口 `CabStartupAssetSetup.BatchBuild` 生成 `StartupControlsWithKey.prefab`、`StartupControlsWithoutKey.prefab`、`MasterKey.prefab`。

## 结构

240×115 mm黑色可拆面板，后部通用接线壳。独立钥匙黑色胶木头、黄铜片和浅定位槽，钥匙槽比片宽0.5 mm、厚0.8 mm的名义总余量。锁芯与钥匙一起绕面板法线转动，外锁圈为圆形孔给旋转锁芯让位；锁芯本身保留矩形钥匙通道。锁槽底部暗腔与钥匙片有深度关系，不在表面画假槽。

这是游戏改装设计，不宣称某铁路钥匙标准、真实锁齿/锁簧、接线针脚或防盗等级。启动按钮、燃油泵旋钮、电源/运行指示灯与中文铭牌同组；朝鲜文尚未核验，本轮不随意翻译。国旗/其他朝鲜元素待室内布置阶段。

## 交互与现有后端

`CabStartupControls` 显式绑定同一 `CabEngineState`，通过 InsertKey / RemoveKey / SetMaster / SetFuelPump / TryStartEngine / StopEngine 方法操作。空锁版本按需实例化独立钥匙预制体。插拔与转动状态分离；关闭且发动机停机才允许拔钥匙，运行中拒绝关闭主控，需先停机。只有钥匙在位、主控开启、燃油泵开启、发动机尚未运行时可启动。

锁芯与钥匙联动，燃油泵旋钮反映当前FuelPumpOn，电源/运行镜片按状态设置独立材质属性。没有把插拔动画轨迹、手部抓取、碰撞与鼠标射线派发伪称已经完成；目前是可调用接口和状态切换。钥匙拔出只隐藏或移除视觉在位状态，不代表已接背包物品系统。

这些守卫属于新组件接口，不是对整个项目StartEngine所有调用的全局强制封锁。既有2D界面仍可能直接调用CabEngineState；全局启动程序/电池/预润滑/燃油建压和油压延时未实现。本次不修改发动机物理和种子初值。

## 验收

两个版本分别验证：主控未开拒绝启动、无钥匙拒绝使能、插入后可开主控、开启档拒绝拔钥匙、关泵拒绝启动、开泵可启动、运行中拒绝拔钥匙或关闭主控、停机关闭后可拔出。测试使用克隆实例，不污染待保存的带钥匙/空锁初始版本。
