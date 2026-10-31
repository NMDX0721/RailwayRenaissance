# NF-5K 单驾驶室行车灯光面板

2026-10-24。用户明确按单驾驶室推进，并要求灯光覆盖实际运行场景，不能只停留在关/弱/强。

## 当前设计与接口

复用现有仪表板Auxiliary_Control_Plate下半部备用区域，保留上部警示灯外观，隐藏Future_Radio_Location占位字。候选薄板320×94×2.5 mm贴原板安装；旋钮有金属锁紧座、实体索引、四角凹槽固定螺钉。中英档位与功能词来自Cab/exterior_light_control_profile.json，原生文字贴图由build_exterior_light_control_prints.py生成。

左侧三档前照灯：关/弱/强。右侧六档灯光工况：关/日行/夜行/调车/本端尾灯/手动。模式不是国家标准枚举，是NF-5K自定义操作配置。

| 工况 | 本端实际输出 | 说明 |
|---|---|---|
| 关 | 全灭 | 不把停止行驶自动解释为应关灯 |
| 日行 | 顶部主灯强光、两白标志灯，共3灯 | 日间可见性配置，非各国通用配光 |
| 夜行 | 顶部及左右主灯弱光、两白标志灯，共5灯 | 当前项目近/远辅助照明候选，非认证夜间照度 |
| 调车 | 两白标志灯，共2灯 | 本端游戏配置；不是英国红白混色调车信号，也不声称完整双端调车照明 |
| 本端尾灯 | 两红标志灯，共2灯 | 显式选择，本端必须实际作为列尾；不会因挂倒档自动切换 |
| 手动 | 保留当前电路选择，取消预设掩码 | 上下文增加标志灯关/白/红独立调整 |

左旋钮直接改灯时自动进入手动，防止面板还显示预设而输出已被更改。标志灯红与前照灯互斥：选红退出前照；开前照退出红。尾灯选择在上下文使用长按，不能将其当作自动端位判断。

CabExteriorLighting集中持有HeadlightLevel、MarkerAspect、OperatingMode及对应Set接口。旧SetMode五模式保留0/2/5/5/2灯组合；旧场景从序列化Mode迁移独立选择。CabEngineState.HeadlightOn关/开恢复先前完整选择及工况，不丢失日行灯组掩码。无效枚举拒绝，断电保留选择且真实光源全灭；恢复供电按请求恢复。

CabExteriorLightControl只读取后端，不另存灯路状态；旋钮索引跟随后端和旧接口变化。复用CabContextInteraction主交互绑定、距离/禁用守卫及CabLightingKey.SetLanguage的进程内zh/en适配。实物固定双语；全游戏语言菜单未接。实体面板接入后关闭旧L键循环及调试GUI，旧API仍可验收。

NF5KExteriorLightControlSetup随NF5KCabFittings生成四个检查场景。当前仅驾驶室一端五灯，没有增加第二驾驶室、后端灯具或自动列车端位。电池电压、灯具负载、保险丝/故障、真实眩光与认证配光另行实现。

## 各国检索与适用边界

1. 美国：2025版官方49 CFR 229.125区分本线与调车用前照灯、调光装置、特定公共道口条件下辅助灯；辅助灯可以常亮或按运营规则交替闪。不能据此认为所有国家、所有车型都必须安装美国式道口灯，也不能把现有NF-5K两只侧大灯直接宣称为已合规ditch lights。
   来源：https://www.govinfo.gov/content/pkg/CFR-2025-title49-vol4/pdf/CFR-2025-title49-vol4-sec229-125.pdf
2. 日本：国土交通省2026-03-31修订解釈基準，第119条相关内容区分前部/后部标识与调车动力车标识；普通铁路的昼夜及退行处理带条件，退行并不是简单交换红白。运营商还需制定具体实施标准。本批借鉴条件与角色分层，不复制日本全套规则。
   当前官方入口：https://www.mlit.go.jp/tetudo/tetudo_fr7_000036.html
   当前解釈基準：https://www.mlit.go.jp/common/001968198.pdf （PDF第124–125页，即正文123–124页）
3. 英国：RSSB公开TW1 Issue18（2022）PDF明确标注DRAFT，分正线、库/场、停放、昼夜光束、尾灯与调车章节。可支持操作场景应分层的设计研究，但本次没有取得其现行正式全文，不宣称这是2026现行操作规程。另一个2023公开征求意见文件有调车红白组合描述，仅作差异例证。
   草案：https://consultations.rssb.co.uk/_entity/sharepointdocumentlocation/eda0ea01-c2c7-ec11-a7b6-0022489f4736/2ab10dab-d681-4911-b881-cc99413f07b6?file=09_b_GERT8000_TW1_compressed.pdf
4. 中国：国家铁路局所载2006年铁道部令第29号历史文本包含本线、单机、反向与调车标志组合。它是历史资料，不是本批确认的现行普速规则，不用于硬编码当前中国全网行为。
   来源：https://source.nra.gov.cn/xxgk/gkml/ztjg/gfzd/bmgz/202206/t20220624_327439.shtml
5. 德国补充：DB官方Ril301 INB2027版已公开，整本标明2026-12-13生效，晚于本次检索日期。含Zg/Fz不同标志类别，只作为待生效对照，不混称当前规则。
   来源：https://www.dbinfrago.com/resource/blob/13699534/f8aa2f00d7c281a066d7846bbeb1e41a/Ril-301-INB-2027-data.pdf

结论采用两层：运行情形请求协调灯组；独立电路仍可手动操纵。后续若扩展具体国家线路，应另设带来源版本的规则配置，不能让不同国家的红白/调车/闪灯组合同时默认生效。

## 实际验收

### 2026-10-25 仪表支架与冗余标题

移除旧辅助板上的“辅助控制 / 状态”总标题，保留具体警示灯及控制功能标签。速度表旧实心方块在场景生成时隐藏，替换为4 mm上下安装板和双侧腹板组成的开敞金属支架，沿用原支承上下接触面与涂漆材质，四个底脚螺丝落在底板上。原模型和速度表位置保持可追溯；这是几何连接表达，不代表结构强度认证。由NF5KExteriorLightControlSetup统一作用于四检查场景。Unity验收：tools/compile/console_bracket_v1_stdout.log退出0，四场景生成记录支架包络190×60×120 mm，NF5K_EXTERIOR_CONTROL_OK与NF5K_SIMULATION_PLAYMODE_OK通过。tools/compile/console_bracket_capture_v3_stdout.log退出0；人工检查exterior_light_control_night_v3.png及console_v3整体视图，旧标题消失，现有灯光标签未被支架遮挡。speedometer_bracket_v2.png补拍上侧安装关系；从此角度下部腹板仍受仪表横梁遮挡，不作为完整结构可视验收。

原计算器缺失依赖的编译阻塞已由负责会话补齐，本批保留其工作。exterior_control_v1_stdout.log记录旧阻塞，v2记录另一个Unity占用；不以这两个失败日志作为交付证明。

tools/compile/exterior_control_operating_v3_stdout.log退出0，NF5K_EXTERIOR_CONTROL_OK覆盖实际旋钮射线、三/六档上下文与索引、五旧模式、日行/夜行/调车/尾灯真实灯数、手动退出预设、红灯与前照互斥、断电保持/恢复、旧开关精确恢复工况、室内灯隔离及距离/禁用守卫。NF5K_SIMULATION_PLAYMODE_OK及既有琴键、雨刷、风扇等回归通过。

实际渲染由NF5KExteriorLightControlReview.CaptureBatch生成，检查刻度朝向、功能字、警示灯区/台面间距及整体布局；只保存预览文件，不保存临时检查光照。

最终预览：tools/compile/exterior_control_capture_final_stdout.log 退出0，NF5K_EXTERIOR_CONTROL_CAPTURE_OK。已人工核对 exterior_light_control_v2.png、exterior_light_control_night_v2.png 和 exterior_day_profile_v2.png / exterior_night_profile_v2.png；夜行时实体指针与实际5灯输出一致，日行保留顶灯+两标志灯。CabSimulation.cs / CabSimulationCodec.cs 的SHA256与本批前一致，未改动力/制动核心。
