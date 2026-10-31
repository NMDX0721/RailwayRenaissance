# NF-5K 停放制动选择器细化

本批细化已有座旁停放制动控制，不另造一套制动模型。保留CabDrivingTarget.ParkingBrake、65度旋转和原有停车/零牵引限制；CabContextInteraction继续提供长按施加/缓解。没有添加新快捷键。

NF5KParkingSelectorDetail替换旧方块外观，补共轴金属杆、轴套防尘连接、圆润深棕握柄、低对比指示线及固定限位柱；安装面四枚螺钉换为既有凹槽头。中英印刷整合在原底板，取消单独PARK/REL/SET的TextMesh简写。build_parking_selector_print.py生成原创透明印字。候选尺寸和外形为项目设计，不是实车尺寸测绘、强度验证或停车制动气路认证。

参考边界：Schaltbau铁路驾驶台开关资料说明铁路操纵元件可采用不同手柄、安装和分档形式；Knorr-Bremse铁路驾驶室控制件资料说明控制模块化。这些只作操纵器设计背景，不据此宣称本组件为任一实车型号复刻，也不拿公路车辆黄按钮规范套用铁路。
- https://schaltbau.com/en/product/electrics-for-rolling-stock/toggle-switches/
- https://rail.knorr-bremse.com/en/gb/portfolio/products-and-systems/braking-systems/interaction/

## 工作区恢复及验证

本轮切入codex/3d工作区后，已从迁移前stash的未跟踪树仅补入缺失的前置资源，未覆盖当前同名文件；源树SHA和恢复清单见tools/compile/cab_worktree_recovery.json。恢复资源属于原有基线，不是本批新做的功能。此前主工作区检查因空材质引用失败，不作通过证据。

最终日志tools/compile/parking_selector_final_stdout.log退出0，四场景生成与NF5K_SIMULATION_PLAYMODE_OK通过。NF5K_PARKING_GEOMETRY_OK验证重复生成不增加组件、垫层/轴套/防尘圈/止挡落座在0.2mm检查容差内、握把与金属杆/轴包络连接；这是几何接触检查，不是结构强度或接触力求解。NF5K_PARKING_SELECTOR_PLAY_OK及GUARD_OK验证真实MeshCollider射线施加/缓解后端命令，并拒绝移动状态操作。

早期检查因空材质或迁移时原有合成汽笛音频及场景引用丢失失败，不算完整通过。由既有生成器重建音频，并通过Editor重绑四场景缺失AudioSource.clip。v3组件通过，但旧ContextInteractionReview假设检查面板总是可见，导致全量回归误报；修正为测试时显式开启、finally恢复，不改变玩家运行面板状态。final日志全套通过。

人工核对parking_selector_detail_v3.png：双语印字贴在既有底板，薄轴套连接可辨，取消旧突起档位条，止挡有落座支承。仍无手部动画、力反馈、完整气路或认证尺寸保证。

额外的integrated_v2大小闸单独对照图生成被自动审批服务额度故障阻止，未执行，不作为新截图证据；大小闸材质已在本批四场景应用并通过Play检查。

## 2026-10-27 厂家细节复核 v4

新增实际查阅MAFELEC C22旋转开关页面与三页目录：
- https://www.mafelec.com/en/solution/rotary-knobs-switches-c22-range/
- https://www.mafelec.com/ct-ressource/document/catalogue-extract-rotary-knobs-switches-c22/

该产品包含22/30mm面板开孔适配、凸轮旋转接点、密封与防转安装形式；厂家资料支持紧凑轴座/面板安装的设计参考，不支持此前自行添加的两根裸露限位柱。它不是本车停放制动系统的指定型号，目录的22/30mm也不等于整个握柄尺寸，未直接将该数值套给本车。另查印度铁路Bombardier驾驶手册，停放制动缓解采用按钮，说明各车操作形式有差异，不存在必须照抄的通用旋钮外形。https://cr.indianrailways.gov.in/cris/uploads/files/1606117842553-8.%20BOMBARDIER%20HAND%20BOOK.pdf

v4删除外露限位柱，轴套外径55减至44mm，圆润握柄长68减至58mm、宽32mm保持，指示条减薄。原65度两端动作不改，印刷仅保留施加/缓解两个端位，去掉类似连续调节的密集小刻度；文字移出手柄主要扫掠区。内部档位由现有动作绑定表现，不宣称新增真实凸轮/限位力学模型。

补图首次启动因PackageManager缺失Burst本地缓存失败，未执行检查方法；从原工作区复制相同1.8.30包至本工作区Library/PackageCache，不改manifest/锁文件或绕过网络证书。最终验证以本节后续回执为准。

### v4最终验证回执

parking_selector_reference_final_stdout.log运行退出0，四场景重复生成/支承接触/握把连接检查通过，实际射线施加缓解与移动拒绝通过，NF5K_SIMULATION_PLAYMODE_OK及完整组件回归通过。最终比较两端渲染时发现上一处施加字样仍被旋转握柄遮挡，已再次将其移至版面上侧，parking_selector_detail_v4.png和parking_selector_other_detent_v4.png已分别人工复核。新大小闸对照图brake_finish_pair_integrated_v2.png、brake_console_integrated_v2.png已生成并检查，之前的额度阻塞已不再是本轮状态。

Windows验证使用临时本地Burst 1.8.30，来自本机原项目同版本包，未升级或降级。首次将本地包置于Library内导致API导入失败，改为.tmp.local-packages后通过实际编译与Play。原manifest.json与packages-lock.json在finally中逐字节恢复；正常注册表包下载中断的问题未据此声称永久解决。临时依赖目录在忽略区，不作为本批游戏资源交付。未进行Android设备验证。

## v5 两档标识与指针统一

用户指出v4施加文字移到角落后失去端位关联。v5取消握柄上的竞争指示条，在与轴同转的36mm指示盘上增加三角印记，指向握柄相反侧；施加/缓解两行双语文字按实际0/65度围绕该指针排布，以无中间刻度的短弧连接两端。标题缩小并移至版面顶端，文字不再靠远移回避遮挡。指示盘与原轴包络相接，三角面为高出盘面0.1mm的印刷几何，不是另一操作按钮。两档行为及联锁不变。

NF5KParkingSelectorDetail新增真实端位方向点积检查（>0.9999），两姿态均从检查相机向两处文字中心投射真实MeshCollider遮挡射线；这是文字中心及本检查视角的可见性检查，非所有玩家视角的逐字无遮挡保证。人工核对parking_selector_detail_v5.png和parking_selector_other_detent_v5.png，短弧、三角指针与两端词条对应，均能辨认。

tools/compile/parking_selector_legends_v5_stdout.log退出0，四场景NF5K_PARKING_POINTER_OK、NF5K_PARKING_LEGENDS_VISIBLE及完整NF5K_SIMULATION_PLAYMODE_OK通过。仍用同版本本地Burst完成Windows验证，结束后原manifest和锁文件已恢复。首次启动曾因审批服务网络中断未执行，重试后通过，不作为未解决阻塞。

## 2026-10-28 实物核对纠偏：v5不是已证实的还原方案

用户继续质疑外形。本轮不再用通用旋转开关资料代替具体停放制动实物，直接检查以下一手来源：

1. NYAB官方技术手册TG-56，2021-08-20 Rev.1，第1页文字明确主操作面板为施加/缓解两个按钮。该页照片为执行机构的链条、手轮及壳体，不能将照片误认为驾驶台按钮面板，更不能把它当成本车已实现的制动系统。
https://www.nyab.com/media/rail-services/locomotive-parking-brake/tg-56-loco-parking-brake.pdf
2. 新西兰TAIC RO-2021-103调查报告，2.68–2.70及图19（图片credit KiwiRail）：DFB柴油机车上侧面板中，PB APPLY红按钮在上、PB RELEASE绿按钮在下；标签直接邻接按钮，没有侧伸握柄或独立旋转圆盘。报告同时说明施加灯由压力开关反馈，缓解按钮不带灯。此布局是该车实例，不能推成全世界统一规范。
https://taic.org.nz/inquiry/ro-2021-103
https://taic.org.nz/sites/default/files/inline-images/image_603.png

实际通过正常TLS/现有7891代理读取官方PDF并渲染第1页，同时查看图19原图。仅供研究的本地文件位于tmp/pdfs，不作游戏纹理/模型素材。浏览器插件连接模块缺失后改用PDF渲染读取，未绕过登录或安全拦截。

结论：截至这次核对，没有找到支持现有“侧伸椭圆握柄+上置圆盘反向三角指针+弧线标识”完整组合的具体停放制动实物。v5来自自拟组合，其Play/几何通过只证明游戏交互及局部几何一致，不证明实车外形真实。之前以C22通用控件推导此组合的依据不足，撤回实物还原意义上的定稿判断。

后续建议（尚未实现）：选定一种具体架构，将此处重做为紧凑的施加/缓解双按钮面板，以两侧固定标签替代旋转标识；状态显示读取后端实际状态，不能按一次按钮就伪造实际制动成功。现有模型保持待替换状态，本轮仅研究纠偏，未修改控制代码、未新增Unity验收。气压执行机构/电机链条执行机构不能混装或混称；本车后端目前仍为简化ParkingBrake布尔输入，未因该研究新增实车级执行机构。

## 2026-10-28 v6双按钮替换实施

按用户明确要求开始：退役侧伸握柄/指示圆盘/弧线。NF5KParkingBrakePanelSetup生成160×240mm面板、3mm金属面、薄密封垫、两枚27mm圆润帽及金属套；红色施加、绿色缓解分别对应一个明确动作，固定双语印字邻接实体按钮。整体仍在原座旁立座上，不移动座椅/车壁。颜色/布局受TAIC图19启发，候选尺寸和横排中英字为本项目设计，非DFB测绘复刻。

CabParkingBrakeButton通过原CabDrivingTarget.ParkingBrake发命令；旧组件保留为无渲染/无碰撞的兼容命令入口，原停车0.1km/h与零牵引限制保留。新增按钮距离2.4m、启用态、显式主控/电池或发电供电、游戏及模拟暂停守卫；同指令不重复触发。按下帽短行程1.8mm、0.18秒弹回；断电/禁用恢复帽位置，不擅自缓解停车制动。CabContextInteraction每个帽只展示一个长按动作；CabStartupPointer的实际射线验证亦按命中的按钮固定执行对应命令。

parking_brake_profile.json共用物理印刷和运行提示中英词条。已有十格灯窗保持读取原模拟输入，未新增虚假执行成功灯。后端ParkingBrake仍是简化布尔控制，未增加真实电机/链条或弹簧制动压力反馈。本批不宣称执行机构确认、气路真实度或任何铁路认证。

NF5KParkingSelectorDetail保留旧调用入口并转接新Setup；旧旋转打印脚本不再被此组件使用，历史截图/资源不自动删除。NF5KParkingBrakePanelReview检查重复生成、面板/密封/套筒接触、旧件移除、真实双按钮射线/上下文长按标记/原后端输入，以及供电/距离/运动/暂停/禁用守卫。当前验收待实际Unity结果，后续在本节追加。

### v6最终回执

tools/compile/parking_buttons_final_stdout.log退出0：四场景重复生成、实际网格贴合/帽体包络/旧旋转件移除检查，NF5K_PARKING_BUTTON_PLAY_OK及完整NF5K_SIMULATION_PLAYMODE_OK通过。实际射线区分红/绿按钮；每帽只有一个deliberate上下文动作；原后端输入、1.8mm按压、禁用/失电弹回及距离/运动/暂停守卫、中英提示检查通过。

首轮审阅脚本误用了Editor不可访问的内部ApplySimulation接口，改为公开Advance(0)同步；旧面板有尺寸已烘入网格的场景，直接缩放会二次缩小，已显式替换为单位Cube网格后按候选尺寸设置。早期v6/v6b/v6c失败日志不作最终通过证据。

人工核对parking_brake_two_buttons_final_v6.png及parking_brake_cab_context_v6.png，面板融入原座旁立座，红施加/绿缓解及相邻中英字可辨。透明印字关闭旧材质残留的预乘/保留高光设置，去除黑色矩形底。原有状态灯窗保留，未把按钮颜色或短按动画当作制动执行成功反馈。Windows验证使用此前同版本临时本地Burst，manifest与锁文件已还原，未验Android。

### v6位置复核：现落地立座仍是待改布局

用户询问座旁落地位置是否合适。核对当前生成源码：司机坐垫中心x0.65/y0.49/z0.93、宽深0.46m；立座中心x0.99/z0.75，主体宽0.16m、高0.68m，底法兰宽0.20m；新按钮安装面y0.685，主台工作面文档候选y0.839，按钮面低约154mm。按这套源码候选，底法兰与坐垫外缘只余约10mm横向投影差。这是源码关系，不是本轮现场网格/人体通行测量，也不能称为人体触达验收。

现有Play通过仅证明控件/后端操作及所列局部几何。原Fittings只排查选定座椅碰撞包络交叠，不包含膝腿/衣物、手臂触达、座椅调节、起坐动作或完整疏散。当前为两枚按钮单设的立柱没有明确必要，且低于主台、不在正常前视区域；因此不视为最终安装位置。

后续优先评估主台靠司机外侧的侧翼/小斜面，将双按钮嵌入并撤除独立立座；需先核对大小闸/撒砂鸣笛等已占用区、膝部及座椅调节包络，再定安装点。TAIC图19中DFB将同类按钮放在上侧面板，说明不存在必须放座旁立柱的要求，亦不据此强制照搬顶部位置。本次只读复核，未移动模型。

## 2026-10-28 v7 主台斜面安装完成

撤除座旁独立立座、地板法兰及旧紧固件，双按钮移入主台外侧的紧凑斜面模块。此处是安装于原桌面的模块，并未切割原桌面。安装基准（车体局部米）为(1.16,0.89,0.30)，倾角30度；按钮面126×90×3mm，外框142×104mm。面板有真实22mm安装孔和后侧开关体，接触垫依据实际桌面三角形生成；外壳沿用主台涂层。红施加/绿缓解、双语印字及原命令联锁保持。

NF5KParkingConsoleMount负责原生网格与桌面接触；NF5KCabFittings重新构建时只保留隐藏命令入口，不再重建立柱或为隐藏入口恢复碰撞体。Geometry检查旧件移除、孔洞、接触垫及现有大小闸/鸣笛撒砂包络。首轮与小闸安装耳间隙不足，检查主动失败；向外移35mm后通过，不将首轮失败日志当作验收。

最终证据：tools/compile/parking_console_v7_final_stdout.log，Unity退出0，四场景生成、NF5K_PARKING_BUTTON_PLAY_OK及NF5K_SIMULATION_PLAYMODE_OK通过。包括坐姿候选视点到两帽的真实碰撞射线、原上下文动作及供电/距离/运动/暂停/禁用守卫。CabDaylightReview实际网格包络最小大小闸间隙48.5mm；15组自拟座椅位置候选最小间隙402mm，膝部候选234mm。上述为选定包络检查，非人体触达、完整起坐/疏散或认证。未模拟真实执行机构反馈。

人工查看parking_brake_console_mount_v7.png与parking_brake_removed_pedestal_v7.png：文字可辨，安装模块与主台衔接，原落地立座消失。截图为Unity实际场景，并非生成概念图。tools/compile/run_parking_console_review.ps1临时使用同版本Burst 1.8.30本地缓存，结束后原manifest/lock已恢复；依赖文件无内容差异。Android未验证。

### v7 封闭外壳修正（2026-10-28）

用户指出底部敞口；检查发现原侧围板法线向内，外侧渲染被剔除，且缺少底面。NF5KParkingConsoleMount修正侧围与接触垫朝向，并增加贴桌底面。顶部四颗紧固件的面板作为可拆检修面，不新增虚假的设备反馈。此轮仅完成外壳封闭；内部端子、线束与穿桌接头尚未建模，不宣称完整接线安装或防护等级。

本轮最终日志parking_console_enclosed_final_stdout.log：Unity退出0，四场景几何、驻车按钮Play及完整驾驶Play通过；人工查看更新截图，前/侧围板已正常显示、底部不再敞开。过程中UnityEditor.Search.SearchDatabase启动索引出现一次ArgumentOutOfRangeException，栈不在游戏代码，随后检查完成；不将本轮表述为全日志零异常。manifest及锁文件已恢复且无内容差异。
