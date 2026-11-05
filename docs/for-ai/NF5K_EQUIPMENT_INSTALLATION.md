# 电池箱与燃油箱装回整车

2026-11-03。用户要求将独立细化设备放到列车上。`NF5KEquipmentInstallation.Batch` 从已保存 CabFuelTankReview 导出 `Resources/Cab/NF5K_Battery_Box.prefab`、`NF5K_Fuel_Tank.prefab`，安装到四个现有基线整车场景：CabInteriorBlockout、CabDaylightReview、CabSimulationReview、CabRailMotionReview。不覆盖ATO精修场景，也不宣称已接入另一套运行时Cab3DSceneController生成的车辆。

## 装配

车体下新增 `Underfloor_Detailed_Equipment`，两个模型作为共享预制体实例放置：电池箱局部(-1.17,-0.58,6.00)m，燃油箱(0,-0.70,10.70)m。电池保持前盖装回、托盘收纳，燃油箱保持完整外壳。两者随车体运动，不随轮轴自转。供回油、电气能量和检修交互未新增；独立细化场景与早期灰模基线保留。

原Running_Gear内Z≈10.10m的通用柜长度调整为0.65m、对应右侧检修盖长度0.53m，避免与燃油箱和加油颈实际相交；保留该柜的外观占位，不赋予新设备性能。缩短仅适用于通用占位柜，后续仍需按真实选型和检修空间重做其结构。

`NF5KEquipmentInstallation.Apply(body)` 同步接入 `CabInteriorBlockoutSetup.Build`，后续生成基线时恢复相同装车关系。缺少设备预制体时报错，不静默生成没有新设备的整车。幂等替换装配根，不积累重复设备。

## 验证与证据

四场景执行新增设备BoxCollider与车体内其他活动非触发Collider的外部干涉检查，2mm记录阈值；走行场景检查6个里程×正/静/反共18姿态，另三场景各检查当前姿态，总21姿态。检查后保存、重新加载，要求模型网格数量保留；不是工况全包络、内部机构或承载认证。

输出 `tools/cab_reference/equipment_installed/acceptance.json`、train.png、battery_on_train.png、fuel_on_train.png。图片在实际整车场景中拍摄，没有把独立设备图片贴到整车图上。临时检查补光与相机不保存。最后重开CabRailMotionReview并运行原NF5KSimulationPlayReview，检查已有启动、牵引、制动、运动及交互链。日志 `tools/compile/nf5k_equipment_installed_v1.log`。

实际结果：Unity正常退出0；四个NF5K_EQUIPMENT_INSTALLED_OK，21姿态外部干涉0，保存重载后设备网格保留；NF5K_RAIL_MOTION_PLAY_OK、NF5K_SIMULATION_PLAYMODE_OK及既有交互/联锁标记通过，无C#编译错误或检查异常。人工查看整车与两侧设备位置图：电池箱位于左侧中部，燃油箱及侧面加油盖在风缸前方；燃油侧图背光，仅用于安装位置核对，不作为其表面精修证明。未测Android性能或设备真实电气/燃油功能。
