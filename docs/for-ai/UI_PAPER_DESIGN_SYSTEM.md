# RDA UI 设计系统「站台纸本」（2026-11-13）

## 1. 定位
- RDA（平板 App）主体 = 星露谷式 2D 俯视探索场景（`TabletExploreAppUI`）；调度/人员/订单/市场/财务是**独立的经营页**，不是一个大总页。
- 视觉统一为「站台纸本」：2076 年山区小站的纸质办公物。暖纸底、深褐墨字、铁路琥珀强调；母题为车票、工牌、时刻表、账页、公文+红章、轨道。浅色为主，**全部用 Painter2D 精绘，禁止纯色块堆砌**。
- 仅横屏；政治压力与线路老化只给定性措辞，不外显数值（见 `AGENTS.md` 数值两铁律）。

## 2. 代码位置（`Assets/Scripts/UI/Tablet/Paper/`）
| 文件 | 内容 |
|---|---|
| `PaperTokens.cs` | 颜色（PaperBg/PaperDeep/Card/Ink/InkSoft/InkMute/Line/Amber/Good/Warn/Bad/Stamp）、五个分类色（Dispatch 蓝/Crew 绿/Orders 琥珀/Market 紫/Finance 青）、圆角 10/16/24/30、间距 8/12/16/24/32、字号 11/13/15/18/22/32、淡入淡出时长 |
| `PaperDraw.cs` | Painter2D 绘制：`Deco`（内描边/高光/铆钉）、`Glossy`、`Medal`、`Slot`、`Perforation`、`Rail`、`TicketNotch`、`Stamp`、`Dial`、`Ruler`（单/双向）、`LedgerGrid` |
| `PaperUI.cs` | 组件工厂：`Panel/Card(selected)/Ticket/IconButton/Button(Primary·Secondary·Ghost·Danger)/Segmented/SideTab/Tag/Gauge/DeltaRuler/Dial/RailDivider/PerfDivider/Stamp/Sheet` + `Round/Border/Pad/Fade/Show/Hide/Text` |

规则：新页面/改页面只引用上述 token 与组件，不再自写颜色、圆角、字号魔法数。不要同时 `using static` PaperTokens 与 TabletUIHelper（`Ink` 冲突），用限定名。

## 3. 母题对照
| 页面 | 纸物 | 状态 |
|---|---|---|
| 顶部仪表 | 刻度环 `Dial` + 刻度条 | 已完成 |
| 左栏导航 | 书签式 `SideTab`（选中=分类色条） | 已完成 |
| 调度·发车方案 | 时刻表卡 + `DeltaRuler`；选中=分类色描边 | 已完成（盖章框待做） |
| 人员 | 工牌：相框头像、`Tag`、刻度条 | 部分（挂绳孔待做） |
| 订单 | 车票 `Ticket`：缺口+穿孔票根+倒计时 | 已完成 |
| 日报 | 公文：轨道分隔+红色印章 | 已完成（盖章动画待做） |
| 市场/财务/总览线路图 | 态势板/账页格线/铁路示意 | 待做（仍用旧 Rda* 组件） |
| 探索场景 HUD | 同一套 | 待迁移：`TabletExploreAppUI` 仍有私有 `StyleCard/DrawDeco/DrawSlot…` 重复 helper |

## 4. 信息架构原则（用户 2026-11-13 要求）
1. **不要把所有内容整合进一个页面/一个二级菜单**。按功能分区：每类经营功能独立成页，由各自入口进入。
2. **部分交互必须在地图指定位置进行**：到站点/建筑触发，而不是在任何地方从菜单点开。规划归属（探索场景 spots：hall/dispatch/waiting/platform/garage/tea/crossing）：
   - 调度室 → 发车方案、经营策略
   - 车库/检修 → 人员（休息、培训）与车况
   - 大厅/茶摊 → 订单接取与交付
   - 市场/公告栏 → 沙能动向与公关
   - 财务/办公桌 → 资金趋势、日报
   - 休息点 → 结束一天
   - 随身（菜单/物品栏）仅保留只读概览：状态、地图、系统、物品。
3. 页内仍可用二级分段，但只用于同一功能内的细分。
4. 已实现（2026-11-13）：`TabletRDAAppUI.OpenPage(l1,l2,restricted:true)` 受限模式（无一级导航、无结束一天）；探索场景 spots：dispatch→调度、garage→人员、hall/tea→订单、board(公告栏)→沙能、finance(账房)→财务/日报、rest(宿舍)→结束一天并弹日报。左侧栏为“前往”（自动寻路到地点并交互），总览只读。完整模式（`OpenRdaRequested`）仅供评审使用。待做：新增地点的地图标牌/文字标注、离开地点时的细节提示。

## 5. 评审与验证
- 画廊：`Tools/Tablet/Run Paper UI Gallery Review` → `tools/compile/paper_gallery_v1/`，日志以 `PAPER_GALLERY_OK` 结尾。
- RDA：`RdaDayLoopReview.Batch`（10 页 + 结束一天 + 日报）→ `tools/compile/rda_dayloop_review_v2/`，日志含 `RDA_DAYLOOP_REVIEW_OK`。
- 场景：`ExploreReview.Batch` → `tools/compile/explore_review_v1/`。
- 批处理用 Unity 6.5（`D:\Unity Hub\Unity 6.5\Editor\Unity.exe`），启动前确认没有其他 Unity 实例占用项目。

## 6. 待办
- 迁移 `TabletExploreAppUI` 到 Paper*，删私有 helper；NPC 名牌与头部重叠修复；罗盘/对话名牌丝带。
- 市场/财务/总览按母题换装；账页格线用 `PaperDraw.LedgerGrid`。
- 按 §4 把经营页拆到地图位置交互。
- 事件弹窗、线路名/里程数据化、存档新字段、`DEV_PLAN.md`/`DAILY_SETTLEMENT_FORMULAS.md` 同步。

## 7. Claude / GPT 分工与口径（防分歧）
| 范围 | 负责 | 唯一依据 |
|---|---|---|
| 位图/像素美术：Tileset、角色 4 向帧、NPC、机车车辆、图标样张 | GPT（imagegen） | `Assets/Resources/Art/EXPLORE_ASSET_SPEC.md` + `Art/Explore/Concept/README.md` |
| 代码与 UI：探索逻辑、PaperUI、Painter2D 绘制、经营页、接入美术 | Claude | 本文 + `AGENTS.md` |

统一口径（两边都不得另起一套）：
- 视角：横屏 2D、3/4 俯视；地图 40×24 格，每格 32×32 世界像素；角色 32×48，脚底锚点一致；PNG 透明、Point 采样、每图 ≤32 色、统一调色板（首批锁定后回填）。
- 车辆：停靠的是 NF-5K「耕牛」朝鲜原厂涂装（深蓝+红白波浪+樱花+翼星徽），编组 7 格（头车 3 + 客厢 2×2）；0721「沙子飞猪」是沙能飞行载具，不得作为停靠机车。
- 现用参考：Concept 的 v2（站区、NF-5K）；v1 的 0721 停靠图已否决。概念图均**不接入运行时**，不满足逐格交付；主角 v1 姿态不锁定面容。
- UI 色彩：UI 一律「站台纸本」（暖纸/深褐/琥珀）；像素美术的调色板用于地图与角色，不用于 UI 面板。GPT 若要出 UI 图标，须是 16×16/24×24 像素图标，且与 Painter2D 矢量图标并存时先和本文对齐。
- 接入：当前地块、建筑、列车由 `ExploreMapView.Draw` 程序绘制；换成位图须改绘制层，保留网格碰撞/寻路/交互坐标。角色走 `ExploreCharacters.TrySprite`（`char_{id}_{walk|idle}_{down|up|left|right}_{n}.png`）。
- 改口径的流程：先改文档（本文或美术规格），再改代码/出图；谁改谁在 Notion「AI Handoffs」留一条。
- 状态：GPT 的美术工作目前暂停（2026-11-13），Claude 可继续改代码与 UI；GPT 恢复前先读本节与美术规格。
