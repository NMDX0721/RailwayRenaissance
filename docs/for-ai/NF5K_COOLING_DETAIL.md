# NF-5K 车底冷却模块

冷却模块是电池箱和燃油箱之后的第三件精细化动力设备。独立审阅场景以 CabFuelTankReview 为底，使用原冷却灰模中心 (0, −0.65, 9.90) m 和 1.80 × 0.58 × 1.00 m 的 X/Y/Z 安装包络。

## 结构

- 左侧倾斜进气百叶、折边侧框、铝色散热芯叠片及两端集液室。叠片用一个合并网格保留片间开口。
- 右侧双抽风组件各有方形法兰、圆形导风圈、电机、轮毂、七片有后掠和扭转的叶片、同心防护圈及径向支撑。
- 上下及端部导风板、底架吊挂脚和隔振垫。两只带法兰和密封圈的冷却液接口朝柴油机侧，另有排气螺塞和放液螺塞。

采用左进右排的概念布置。柴油机侧软管、贯通到车裙边的外侧风道、热风再循环、温控与转速均待后续处理。当前结构用于外形和安装审阅。

## 生成和检查

Unity 菜单 Railway → NF-5K → Build Cooling Review 或批处理入口 `NF5KCoolingReview.Batch`。生成器将工作保存到 `Assets/Scenes/CabCoolingReview.unity`，检查所有网格边界落在安装包络内，以安装包络对车体有效非触发碰撞体做 18 个收纳姿态抽样。采用 2 mm 外部干涉阈值，随后重开场景检查持久化网格。输出到 `tools/cab_reference/cooling_detail/`，包括车底位置、风扇侧、进风侧和揭开风道的近景。抽样仅覆盖收纳位置。

## 实物资料

- [mtu Rail Solution Guide](https://www.mtu-solutions.com/content/dam/mtu/download/applications/rail/16120356_Rail_Solutionguide.pdf/_jcr_content/renditions/original./16120356_Rail_Solutionguide.pdf) 给出铁路 PowerPack 的车底冷却布置和电驱风机选项。
- [AKG Railway Cooling](https://www.akg-america.com/markets/railway/) 介绍铁路换热器集液箱和集成部件。
- [铁路车辆车底冷却风道专利 US11173928B2](https://patents.google.com/patent/US11173928B2/en) 提供换热器、风扇和车侧排风的结构参考。本件造型和尺寸由 NF-5K 包络推导。

2026-11-04，Unity 批处理 v5 退出码 0，重载保留 124 个网格部件。外形约 1.703 × 0.525 × 0.942 m，18 个收纳姿态外部干涉为 0。实拍中风扇罩边仍有条纹状高光。证据为 `tools/compile/nf5k_cooling_v5.log` 和 `tools/cab_reference/cooling_detail/acceptance.json`。四个整车基线场景仍使用原有配置。

### 表面质感修订

壳体使用低金属度的灰绿粉末喷涂，百叶、护网与紧固件使用带方向性细纹的金属材质，扇叶和橡胶件分别调整为低反光表面。罩壳端面增加翻边和铆钉。风扇法兰的平面、圆孔内壁和外缘分别设置法线，圆环补齐用于表面纹理的 UV。对比图使用同一机位，原图为 `fan_side_before_finish.png`。

同机位复核中，法兰原有的放射状条纹高光已消除。批处理 `nf5k_cooling_finish_v2.log` 保存重开后保留 148 个网格部件，18 个收纳姿态外部干涉为 0。

### 风扇转速与材质实物化

`CabSimulation.CoolingFanTargetRpm` 读取既有 83 °C 开、78 °C 关的水温滞回和冷却故障输入，输出 0 或 1450 rpm。1450 rpm 为暂定额定转速。既有热交换系数仍由该启停状态选取，行车风和故障时的散热参数沿用原模型。转速由当前状态派生，无新增快照字段。驾驶审阅面板显示目标转速，冷却审阅场景的双叶轮沿本地 X 轴随目标加减速。视觉加减速参数为表现用，散热系数仍按既有两态模型切换。

风扇罩壳改为中性灰工业喷涂，叶片用石墨色复材观感，钢制护网用黑色涂层，紧固件用银灰金属色。裸露金属和接口参考 [AKG 散热器](https://www.akg-america.com/products/radiators/)，叶轮及护网用料参考 [ebm-papst AxiEco](https://www.ebmpapst.com/us/en/products/axial-fans/p/S3G500ZM03I1.html)。本轮以独立场景验证，风量、功耗及风扇惯性仍待标定。

Unity 批处理 `nf5k_cooling_fan_v4.log` 验证冷机停转、高温 1450 rpm、81 °C 滞回、降温停转、两组叶轮轴向转动和护网固定，以及故障后目标为零、叶轮实际减速至停止。场景重载保留 148 个网格部件，18 个采样姿态外部干涉为 0。
