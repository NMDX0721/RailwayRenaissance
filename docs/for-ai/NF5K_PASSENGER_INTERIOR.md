# NF-5K 客舱内装第一轮 — 2026-10-21

## 布局候选

`NF5KPassengerInterior.Build`替换旧客舱座椅、内墙和重复的内侧玻璃灰模；外侧车体、驾驶室、车门、灯光接口不重建为另一种车型。原图没有可确认的客舱内景，以下属于结合同类客车的项目设计，不称为NF-5K原车还原。

- 12排、每排2+2，共48个独立座位。只是可视化布局数量，未写入经营运力、票务或乘客后端。
- 坐垫宽450mm，排距1100mm，坐垫顶约0.53m，靠背向后倾8°，候选最小过道约664mm（按扶手外缘）。未取得无障碍或铁路安全认证，后续需做人体、行李与疏散包络验证。
- 座位均朝向驾驶室方向。本轮未加旋转/翻转机构，也未因双驾驶室而在本车尾端添加另一套驾驶台。
- 后部约15.7m以后保留为门区/贯通通道；前端与驾驶室隔门之间留出通道。现有四扇侧门扫掠检查包含新增座椅及行李架固定件。

## 座椅与材质

每座拆分为低光泽织物坐垫、倾斜靠背衬垫、独立头枕、涂漆背壳、底盘和支撑。每组双座采用横梁、双支脚、底板及锚栓外形；扶手有金属支杆与暖棕HPL外观垫。靠背背面增加关闭状态托盘、铰链和锁扣，靠过道座位背顶增加抓握杆。

托盘/扶手均为静态外观，不暗示已经实现折叠交互。支架和连接方式为可视化候选，不提供真实碰撞强度或承载评级。

织物使用持久化微织纹贴图和低光泽，金属与搪漆/涂漆分材质；圆角网格按尺寸共享，避免把每个座椅做成独立大网格。软垫适度圆角，框架保持较小倒角。

## 行李架、窗台与门区

两侧开放式金属条架位于座位上方，带外侧挡边、横撑、三角支撑和窗间柱锚板。少量帆布包用来检查架面与顶棚间隙，不是可交互行李或已验证载荷。

内墙改为暖灰衬板、圆角内窗框、橡胶密封圈、窗台、窗间立柱、下方暖风罩及条形出风口。暖风罩仅外观，未接热管理。保留真实外侧玻璃，不再叠加旧灰模的另一片玻璃。

内窗洞增加约70mm渐缩回口，与外窗原有回口衔接；使用分离正反面顶点，避免双面共用法线抵消。窗洞尺寸依据现有外窗开口匹配，不在窗边粘贴浮空薄片。

后门区增加固定门洞回边、抓杆、耐磨地垫和门槛压条，不把静态内衬贴在旋转门扇的路径中。中央过道使用薄防滑铺层；无新增客室隔断堵住通路。

## 生成与验证

- 入口仍为`CabInteriorBlockoutSetup.BatchBuild`，通过Unity Editor保存场景。
- 网格/微织纹：`Assets/Models/Cab/NF5KBodyMeshes/`；材质：`Assets/Resources/Cab/Passenger_*.mat`。
- `ValidateReload`检查48个坐垫、扶手间过道和前后保留区；`NF5KDoorReview`检查四扇门与新增座椅及架体固定件的扫掠。未等同于完整碰撞/疏散仿真。
- 原有正常、夜间、应急和断电照明检查继续执行；新增客舱反向与座椅近景。
- 为改善仅有直接光时的黑色座椅背面，六组客舱顶灯各附一只低强度、短距离的局部Point反弹近似，亮灭与对应前/后分区同步，断电或应急状态不启用。这不是新增实体灯具，也不是光线追踪GI；不投阴影的补光仍有漏光限制。`Fixture.bounceIntensity`分别保存驾驶室与客舱的近似强度，避免两者共用固定倍率。
- 白天材质图为`passenger_daylight.png`、`passenger_seat_daylight.png`；另有纯夜间实际灯光图，分别标明，避免把白天环境当作夜间照明效果。
- 日志：`tools/compile/cab_passenger_interior.log`。预览：`passenger_lights_on.png`、`passenger_reverse_view.png`、`passenger_seat_detail.png`、`passenger_emergency.png`，目录为`tools/cab_reference/interior_review/`。

## 参考资料

- [KIEL MATCH G2官方区域列车座椅资料](https://kiel-seating.com/wp-content/uploads/2020/12/KIEL_Match_G2_en.pdf)：单/双座模块、可更换坐垫和头枕、扶手与背面托盘；其产品标注的扶手间宽450–500mm仅作设计参照，不将其认证转移给本模型。
- [GRAMMER区域列车座椅资料](https://www.grammer.co.uk/Attachment/DownloadFile?downloadId=102)：座椅底座、壁面适配、抓杆及背面托盘等组件关系。
- [Ellamp铁路内装](https://ellampsystems.com/train/)：内衬、灯光、行李架及阅读灯的集成方式。

剩余工作包括座位编号和必要标识、行李架真实承载、乘客与行李交互、完整客舱热通风、无障碍/疏散校核，以及车内声学和精细做旧。本轮没有擅自添加车号、虚构朝鲜语标语或人物宣传画。
# Seat/wall/floor clearance correction — 2026-10-21

Window sills previously projected to x=1.265m, overlapping the window-seat back (outer edge 1.335m). Sill depth is now 40mm centred at |x|=1.355m, with an inner edge at 1.335m. Window seats move inward 25mm to |x|=1.075m, leaving 25mm between the seat shell and sill. Outer armrests move to |x|=1.315m and clear the inner liner by about 19.5mm. Cushion width stays 450mm and the aisle stays 664mm. The two pans within each pair retain a 5mm lateral gap.

The crossbeam is raised 20mm to y=0.36m to clear the heater cover and contact the seat pan underside at y=0.3925m. Legs extend 10mm to meet it. Feet rest at y=0 without penetrating the floor. `ValidateReload` checks all relevant seat boxes against sill, lower liner and heater-cover colliders with narrow-phase penetration tests, checks the inner wall envelope, and checks all 48 feet against the floor plane. This scoped static check is not a complete passenger/body/seat collision simulation.

Validated in tools/compile/cab_seat_clearance_final.log: 48 grounded feet, no seat/fitting AABB overlap candidates, 48 seats and 664mm aisle retained; scene reload and Play Mode lighting regression passed. Inspection images: seat_wall_clearance.png and seat_floor_clearance.png under tools/cab_reference/interior_review/.
