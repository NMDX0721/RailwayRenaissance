# NF-5K 车头与外部照明 — 2026-10-21

## 外观基线

原图：`ExternalAssets/nf5_exterior_ref.png`。NF-5K是驾驶与客运空间合造头车；0721属于沙能载具，本车未分配车队编号。

- 头牌候选尺寸640×220mm、字高130mm。`NF5KLivery.Text`使用完整实心多边形与耳切三角化，替代线段拼字；为定制工业无衬线字形，不声称是历史铁路专用字体。
- 翼徽圆座直径由240增至320mm，红星与三层翼同步放大，中心高度940mm。风挡与色带位置保留。
- 雨刷增加主轴、主臂、平行连杆、连接点、刮片背骨和橡胶，按8°风挡倾角布置。雨刷暂为静态结构。
- 顶灯与左右大小双灯共五灯，增加反光碗、密封圈、压圈、紧固件、透明灯罩、发光灯芯与真正的Spot光源。碗形近似抛物面；不是光线追踪光学仿真。

## 运行接口

`Assets/Scripts/Cab/CabExteriorLighting.cs`：

| 接口/档位 | 行为 |
|---|---|
| `SetMode(ExteriorLightMode.Off)` | 所有实际光源和灯芯发光关闭 |
| `Marker` | 仅两只白色小标志灯 |
| `Dim` | 三主灯弱光，加两只白色标志灯 |
| `Bright` | 三主灯强光，加两只白色标志灯 |
| `Tail` | 仅两只红色小灯，主灯关闭 |
| `SetPowerAvailable(false)` | 所有光源与灯芯熄灭，保留所选档位；恢复供电后恢复该档位 |
| `CycleMode()` | 顺序切换五档 |

本端红白共用小灯为项目双色灯设计推定，不是原图能确认的实际电路。尾灯由上层在本端位于整列尾部时选择；当前未自动判断列车方向或端位。

照明电源独立于发动机转速，当前为手动输入；未接电池电压、保险丝故障或主控钥匙联锁。`CabEngineState.HeadlightOn`保留旧二值接口，外部关灯后再打开恢复上次非Off档位。档位、供电输入和上次档位均序列化；生成场景默认Off。

审阅场景提供中文面板与L键循环，可通过组件中的`reviewControls`关闭。正式驾驶台旋钮尚未接线，后续可复用上述接口。禁用组件会关闭光源并清除灯芯发光。

## 生成与渲染

`NF5KIndustrialFinish.LampOptics`调用`NF5KFrontLightingSetup.Build`，持久保存网格与材质至现有Cab资源目录。光源默认顶灯照远、侧大灯照近、侧小灯作标志灯。光强、射程、角度为游戏调光候选，尚未进行真实照度或防眩标定。

灯芯通过MaterialPropertyBlock同步颜色和发光。URP材质需保留`_EMISSION`变体：默认发光色为白，GI标志为RealtimeEmissive，关灯由属性块设黑；使用None会被当前URP材质导入验证器移除发光关键字。实际照明由Spot承担，不依赖烘焙GI，也不靠发光贴图假装道路照明。

没有体积雾时不添加悬浮光锥。审阅截图继续采用3倍SSAA，未改全局URP质量设置。

## 验收

`NF5KLightingReview`在保存场景重载后检查五档有效灯数（0/2/5/5/2）、尾灯颜色、强弱区别及断电隔离。暗场输出`tools/cab_reference/interior_review/lights_*.png`，检查灯面与不含车体灯面的前方地面亮度变化。

本轮结果：地面亮度平均值Off=0.04953、Bright=0.1180094；顶灯中心Off=0.02190981、Bright=1.0。数值是PNG像素灰度，不是lux。

`NF5KLightingPlayReview`实际进入Play Mode，验证模式API、电源隔离、旧开关关/开恢复，以及禁用清理。门扇扫掠、整车轮对及保存重载检查继续执行。日志：`tools/compile/cab_front_lighting.log`。鼠标实体驾驶台灯光旋钮、车辆电力后端和自动端位选择不在已完成范围内。

## 网络参考与适用边界

仅借鉴功能与灯具结构，不作为朝鲜NF-5K认证依据：

- [FRA 2020年机车LED头灯及辅助灯研究](https://rosap.ntl.bts.gov/view/dot/53429/dot_53429_DS1.pdf)：强/弱档、可见度与眩光。
- [RDSO双光束LED头灯规范修订草案](https://rdso.indianrailways.gov.in/uploads/files/Draft%20revised%20specification%20for%20LED%20head%20light.pdf)：2024年征求意见稿，含bright/dim测试；不是NF-5K现行标准。
- [LEDtronics铁路照明应用](https://web.ledtronics.com/markets/transportation_and_railroad)：铁路外部标志灯与安全灯用途。
