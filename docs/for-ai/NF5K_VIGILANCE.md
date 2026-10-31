# NF-5K 脚部区与司机警惕装置

## 设计和来源（2026-10-28）

采用原创周期确认踏板，不把持续踩压的失能装置、AWS/TPWS或机车信号拼成一个系统。FRA 49 CFR 229.140说明机车警惕器的手动确认、特定操纵动作复位、视觉预警、声音报警以及超时制动试验；其低速周期110–130秒、高速公式及适用车型范围只作研究。NF-5K没有照搬美国规则，亦未取得本车认证数据。
来源：https://www.law.cornell.edu/cfr/text/49/229.140
原始政府版本：https://www.govinfo.gov/content/pkg/CFR-2024-title49-vol4/pdf/CFR-2024-title49-vol4-sec229-141.pdf

## 本轮实现

NF5KVigilanceSetup替换旧Driver_Footrest/Vigilance_Pedal_Reserved，生成400×250mm底板、独立固定脚撑、105×170mm防滑踏面、后铰接外观、地板紧固件和保护走线槽。中心车体局部坐标(0.65,0,0.49)，踏板中心相对右偏100mm、高63mm。尺寸为候选，不是实车测绘或人体工学标准。顶面6mm短行程是按压表现，未模拟弹簧力/足部IK；底槽不是完整端子和隐藏线束。

CabVigilance经CabContextInteraction沿用玩家主交互键，单次按下执行踩下/松开确认的表现，不新增必须记忆的快捷键。实际输入确认时重置周期；不把普通鼠标移动、镜头转动、AI动作自动算作确认。当前仅手动踏板复位，不实现美国规则列出的多操纵件自动复位，也不是疲劳/意识识别。

车型候选：主控开启且速度绝对值≥0.14m/s才累计；周期120秒，其中最后5秒视觉提醒，之后声音报警，另8秒未确认则锁存保护。时间参数为设备配置，非存档经济初值。游戏和模拟暂停时固定步不推进。停稳且未介入时清空周期，介入后即使断主控或停稳仍保留。停车到0.028m/s以内、牵引归零、大闸置紧急位才能通过踏板复位。

CabSimulationDriver在真实50Hz步进读完操纵命令后调用Step；保护对有效输入强制Throttle=0、Brake=Emergency。使用现有紧急制动近似，不冒称真实警惕惩罚制动率。未修改原气路/牵引核或伪造手柄动作。复位不自动发车；物理手柄必须先归零/紧急，司机随后自行缓解。

Checkpoint v2增加可选vigilance JSON（内部version1）、vigilanceThrottle/vigilanceBrake，区分人工操纵位置和保护覆盖后的有效输入。旧检查点缺扩展时按新周期起点恢复；带扩展的检查点要求场景有对应组件。研究场景没有该组件时不加入保护，不据此宣称ATO控制器已验证警惕联动。

CabVigilance用原创880Hz间歇音、双语浮动状态提示，踏板复用中英上下文。尚无专用实体警惕灯、全局设置/休闲模式、小黄鼠代操作、脚部动画、完整线路保护、端子级布线或结构强度验收。声音仅为游戏提示，不是认证声压级。

## 验证

NF5KVigilanceReview检查唯一组件、旧占位移除、底板支撑、实际踏板射线和按压、固定步三阶段、供电切断后锁存、暂停/距离拒绝、真实Driver输入覆盖及检查点锁存恢复。四场景与完整驾驶Play结果待本轮实际日志补记，不以源代码检查当作运行通过。

额外守卫：蜂鸣器须有主控与电池/发电供电；失电静音但保护锁存不清除。提醒文字为游戏浮动提示，并非无电仍发光的实体灯。

### 首轮验收回执
最终证据tools/compile/vigilance_v1_delivery_stdout.log：Unity退出0，四场景重复生成、NF5K_VIGILANCE_PLAY_OK与NF5K_SIMULATION_PLAYMODE_OK通过。实际Context目标识别/单动作菜单/执行、踏板6mm按压、暂停/距离拒绝、失电静音且保留锁存通过；真实模拟从5m/s（18km/h）保护制动停车，行驶中拒绝复位，停稳归零/紧急位后复位不自动缓解，检查点恢复保留保护。人工核对vigilance_footwell_v1.png，采用仅截图存在的辅助补光，未给正式场景永久加灯。
首轮编辑器脚本误用GameObject局部变换；后续发现组件静止位置/owner未序列化造成Play归零，已修复并重验。早期失败日志不当通过证据。最终日志仍记录UnityEditor.Search.SearchDatabase启动索引ArgumentOutOfRangeException，非本次游戏代码堆栈；不是全日志零异常。临时同版Burst验证结束后manifest/lock还原；未验Android、实际足部动作或认证声压级。

## 踏板外观v2（2026-10-28）
用户反馈首轮形体/质感怪异，新增NF5KPedalFinish专用原生圆角网格与独立材质，避免修改风扇/灯箱共享材质。底板由16mm减到6mm，壳体向下延伸仍与底板接触，踏板静止中心/6mm行程和交互不变。防滑筋由金属方条改为1.5mm低矮同色橡胶筋；固定脚撑亦补防滑筋；铰轴改为平滑圆柱。橡胶非金属、低光泽，漆面钢/轴件分别控制金属度和粗糙感；轻微鞋底接触区域亮度与细颗粒使用程序纹理，不是实拍PBR扫描。几何底板支承检查改读取实际网格Bounds，未以旧缩放值代替实际尺寸。四场景渲染与交互回归结果待本轮日志。

踏板v2最终回执：tools/compile/vigilance_finish_v2_stdout.log退出0，四场景、NF5K_VIGILANCE_PLAY_OK、完整NF5K_SIMULATION_PLAYMODE_OK通过。人工查看vigilance_footwell_v2.png：原亮银防滑条已消除，橡胶筋/微圆边、薄钢底板与独立材质可辨；踏面位置与交互按压保持。日志仍有UnityEditor.Search索引异常，未据此宣称全日志零异常。临时包文件已还原；未新增国别认证或实际结构承载结论。

## 连续15度脚撑台v3（2026-10-28）
按用户确认采用15度候选，朝车头升高；不把安装坡度与活动踏板转角混用。参考Schaltbau F290第5–6页的多种铁路脚撑台几何，不宣称本车已通过人因认证：https://2024.schaltbau.com/media/f290_en.pdf 。连续楔形钢盒顶面高程为0.055-tan(15°)×局部z，250mm水平深度约67mm高差，底部贴合6mm安装板。顶面在活动区域保留真实孔口；固定支撑改为连续面上的薄防滑垫，取消原独立厚方块。活动踏面厚8mm，与固定垫顶面高度差约8mm，后端铰轴、下方开关体与活动件分开。
CabVigilance增加可选hinge/restRotation，当前生成场景用脚跟侧铰轴转动-2度，实现脚尖端约6mm行程；旧无hinge场景保留平移兼容。固定底座与线槽不随踏面运动。警惕计时/制动/复位逻辑未变；回归改检查铰轴转角。最终图和实际日志待追加。

v3最终验收：tools/compile/vigilance_inclined_v3_final_stdout.log退出0，四场景重复生成、NF5K_VIGILANCE_PLAY_OK、NF5K_SIMULATION_PLAYMODE_OK通过。全行程0–2度共9姿态的实际踏面网格底角未穿入开关体，且保持在开孔范围，铰轴位置不变；原实际射线/上下文、失电静音锁存、保护停车/复位与检查点回归通过。补四枚可见顶盖检修螺钉，原底板锚固保留在可拆顶盖下。人工核对vigilance_footwell_v3.png。未进行角色鞋底/膝踝姿态或完整人体动作验收；15度仍为候选，不宣称最舒适或符合某国标准。日志有UnityEditor.Search启动索引异常，未将其表述为全日志零异常；manifest/lock已还原。

2026-10-28细节v4：铰轴两端补轴承座，活动踏板开孔两侧补薄橡胶边条，运动包络与原保护逻辑保持；Unity实际生成及回归待核验。

v4核验：vigilance_detail_v4_stdout.log退出0，四场景生成与警惕和驾驶Play回归通过；UnityEditor.Search启动索引仍有异常。现有CabVigilance及CabSimulationDriver保护覆盖属于本工作区候选实现，后端由研究会话负责评估与决定；建议接口包含踏板确认事件、警告阶段、锁存介入状态和复位条件，保持固定步和检查点一致性。

2026-10-29踏板v5：调整漆面与橡胶的色泽和光泽度；固定线槽增加进入盒体及台下端部的护口、两道压片和紧固件。线槽及接头为外观候选，内部导线/穿孔及电气连接尚未建模。用户已指定后端由研究会话负责，本轮未修改后端。Unity验证待补。

v5验收：vigilance_detail_v5_stdout.log退出0，警惕踏板及完整驾驶Play回归通过；实景图vigilance_footwell_v5.png。线槽内部接线、台下穿孔和机械强度未验证。

2026-10-29 v6：参考Schaltbau铁路脚撑金属防滑板，将固定大面积橡胶面改为缎面金属板及深色防滑筋；活动警惕踏面保留橡胶。来源：https://schaltbau.com/en/product/electrics-for-rolling-stock/footrests-for-rail-vehicles/ 。向下穿孔尚待实际地板和底板重建，现有线槽仍为旧候选。

v6回执：vigilance_detail_v6_stdout.log退出0，警惕Play与驾驶Play通过；UnityEditor.Search仍有启动索引异常。vigilance_footwell_v6.png为实际截图。

2026-10-29 v7：根据用户要求撤除地面外露线槽、护口与压片；拟采用底部隐蔽出线。Schaltbau F290展示S293电缆接头及ZL290电缆出口，实际接线随产品/安装方案而异，不可断言均直接穿地：https://2024.schaltbau.com/media/f290_en.pdf 。目前底板和车厢地板尚未开孔，内部线束及底部接头尚未建模，不视为实际完成穿地连接。

v7回执：vigilance_no_floor_conduit_v7_stdout.log退出0，警惕与驾驶Play通过；图片vigilance_footwell_v7.png已检查。UnityEditor.Search仍有启动索引异常。

2026-10-29警惕提示灯v1：NF5KVigilanceIndicatorSetup在既有灯箱上方生成独立中英琥珀色灯窗，CabVigilanceIndicator仅观察候选警惕Phase与现有灯箱供电/试灯，正常关闭、提醒/保护亮起；保护逻辑属研究会话，未修改。本轮Unity运行及实际安装视野尚待验证。

警惕提示灯v1回执：tools/compile/vigilance_indicator_v1_stdout.log退出0，四场景生成，NF5K_VIGILANCE_INDICATOR_PLAY_OK和NF5K_SIMULATION_PLAYMODE_OK通过；检查正常/提醒/保护/断电/试灯状态，实景vigilance_indicator_v1.png显示灭灯外观与相邻仪表位置。运行时发光的实际截图、坐姿完整视野/遮挡和固定强度仍未验收；UnityEditor.Search索引有异常。底板和车厢地板真实穿孔仍未做，非本提示灯批次。

警惕提示灯v2：用户反馈独立上方灯窗突兀，扩展原灯箱底板、重排十格并将警惕窗放入同一上排第六格，试灯按钮向外移；材料与原灯箱一致。效果与碰撞仍待Unity验证。

v2回执：vigilance_indicator_v2_stdout.log退出0，警惕灯状态及驾驶Play通过，vigilance_indicator_v2.png检查了灯窗的上排对齐。试灯按钮外移后与底板的视觉关系、亮灯截图及完整坐姿视野仍需进一步核对；UnityEditor.Search启动索引异常仍存在。

警惕灯v3：统一已有灯窗边框、光学面和文字尺寸；试灯按钮靠近扩展灯箱。美国FRA人因指南将红/黄/绿作为危险/警告/正常的一般关联，并强调设计语境；不据此宣称所有车型必须按颜色划固定区域。NF5K仍依每项状态定义颜色，警惕提示为琥珀，具体策略待研究会话决定。https://railroads.dot.gov/sites/fra.dot.gov/files/fra_net/3291/Reports2011-2013.pdf 。验证待补。

v3回执：vigilance_indicator_v3_stdout.log退出0，指示状态及完整驾驶Play通过；人工查看vigilance_indicator_v3.png确认警惕灯边框与同排视觉一致。画面为灭灯，亮灯光照和完整驾驶坐姿视野仍待验证；日志仍有UnityEditor.Search启动索引异常。

警惕灯v4：在第六列下排加入无功能的备用盲板，补足试灯按钮背后的连续框架，统一模块支承；新增真实Play试灯截图，状态与控制仍只读。运行及图片待核对。

v4最终：vigilance_indicator_v4_final_stdout.log退出0，警惕灯/完整驾驶Play通过；真实Play试灯截图vigilance_indicator_v4_test.png经人工检查，警惕文字亮灯时与原灯窗一样使用深色以保持对比。备用位不发光，试灯背框为独立延伸件；实际螺钉固定、完整坐姿视野尚未验收。UnityEditor.Search启动索引异常仍存在。

警惕灯v5：取消独立试灯背框，以496×86mm连续底板承载灯窗与试灯；四角紧固件重新落在整板边缘，备用盲板改为同框漆面。新增全灯箱构图的灭灯/真实Play试灯图，待本轮验证。

v5验收：vigilance_indicator_v5_stdout.log退出0，警惕指示/试灯及完整驾驶Play通过；人工检查vigilance_indicator_v5_test.png，完整显示11灯窗、备用盲板、同板试灯与角螺钉。仍有UnityEditor.Search启动索引异常。完整司机姿态、整板机械安装及遮挡包络待进一步验证。
