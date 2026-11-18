# 给 GPT 的美术任务单：探索场景室内/设备/UI 图形（2026-11-14）

> 你只负责**图形**。逻辑、框架、交互、UI 代码由 Claude 做。开工前必读（按顺序）：
> 1. `Assets/Resources/Art/EXPLORE_ASSET_SPEC.md`（通用技术规则、已定口径、命名）
> 2. `Assets/Resources/Art/Explore/Concept/README.md`（已有概念图与已否决项）
> 3. `docs/for-ai/EXPLORE_INTERIORS_AND_HUD_DESIGN.md` §3（各建筑草图与元素清单）、§4（HUD）
> 4. `docs/for-ai/UI_PAPER_DESIGN_SYSTEM.md` §7（Claude/GPT 分工与统一口径）

## 不要做 / 不要改
- 不要改任何 `.cs`、`docs/for-ai/` 里 Claude 的设计文档结论；发现设计问题，写进下面「反馈」小节，由 Claude 改。
- 不要把概念图直接放进运行时；交付物必须满足逐格规格。
- UI 面板（工牌卡、菜单、RDA 页面）由 Claude 用代码绘制，**不用出图**；你只出像素美术与 16/24px 像素图标。

## 统一规格（摘要，完整见 SPEC）
3/4 俯视，32×32 世界像素/格；角色 32×48；PNG 透明、点采样、无抗锯齿；每张 ≤32 色，沿用 SPEC 锁定的调色板；左上光源、右下阴影；雾峰站暖色低饱和，旧而整洁，无霓虹。

## 任务清单（按优先级）
### P0 室内通用件（`Assets/Resources/Art/Explore/Interiors/`）
| 文件名 | 内容 | 尺寸 |
|---|---|---|
| `int_floor_wood_01..03.png` | 木地板 3 变体，可无缝 | 32×32 |
| `int_floor_tile_01..02.png` | 灰砖/水泥地面（车库用）| 32×32 |
| `int_wall_top.png` / `int_wall_front.png` / `int_wall_corner_*.png` | 墙体：顶面、正面（含踢脚线）、四个拐角 | 32×32（正面可 32×48） |
| `int_door_open.png` / `int_door_closed.png` | 门与门洞（朝南）| 32×64 |
| `int_window_01.png` | 墙上窗（带光斑）| 32×48 |

### P0 设备与家具（`Assets/Resources/Art/Explore/Props/`，底边对齐格线，脚底锚点在底部中点）
调度室：`prop_dispatch_desk`（2×1 格调度台，带班次表）、`prop_strategy_board`（1×2 策略板）、`prop_route_screen`（2×1 线路大屏，开/关两态）、`prop_radio`（1×1 电台）、`prop_wall_clock`、`prop_sofa`（2×1）、`prop_teatable`（1×1）。
站务大厅：`prop_notice_board`（2×1 委托公告板）、`prop_ticket_window`（2×1 售票窗口，含玻璃与小窗口）、`prop_vending_ticket`（1×1 自动售票机）、`prop_lostfound`（1×1）、`prop_bench`（2×1）。
站台（室外）：`prop_gate_checker`（检票机闸机，1×1，闲置/通行两态）、`prop_timetable_board`（1×2）、`prop_signal_post`（信号机，红/绿两态，1×2）、`prop_switch_lever`（道岔扳手，1×1，两态）、`prop_pa_stand`（广播台 1×1）。
每个有状态的设备导出 `_a` / `_b` 两帧（如 `prop_signal_post_red.png` / `_green.png`）。

### P1 其余建筑
候车室：`prop_waiting_bench`、`prop_water_dispenser`、`prop_tv`、`prop_info_screen`。
车库：`prop_workbench`、`prop_tool_wall`、`prop_locker`（更衣柜）、`prop_bed_bunk`（上下铺）、`prop_pit`（机车检修坑道 3×1）。
宿舍/办公：`prop_ledger_desk`（账房桌）、`prop_safe`、`prop_bookshelf`、`prop_bed`（单人床，1×2）、`prop_lamp`。

### P1 角色
主角 4 向×4 帧行走 + 待机（32×48，每帧单独文件，命名按 SPEC §8：`char_player_walk_down_0.png`…；左向可镜像右向）。
NPC（老陈/李阿姨/张工/茶嫂/小芳/赵师傅）同规格，`char_{id}_{walk|idle}_{dir}_{n}.png`。

### P2 像素图标与交互标识（16×16 / 24×24，`Explore/UI/`）
交互光圈（青铜色，2 帧呼吸）、感叹号气泡、设备“可用/冷却”小标、天气/时间 4 种。

## 交付与验收
- 放对目录，命名小写下划线；每批附一张 `*_contact_sheet.png`（缩略图总览，仅供人看，不接入）。
- 验收（Claude 接入时核对）：格线对齐、脚底锚点一致、透明通道、点采样、无抗锯齿、色数≤32、与已锁调色板一致、尺寸为 32 的整数倍（角色 32×48）。
- 先交 **一间调度室全套（P0）** 作为样张，Claude 接入后确认画风再批量。

## 2026-11-16 追加（Claude）：调度室放大与墙体结构
- 调度室已放大为 **20×12 格**，中部偏空。请补 P0.5 摆件让房间有生活感：`prop_rug`（地毯 3×2，铺地可行走，不占格）、`prop_filing_cabinet`（文件柜 1×2）、`prop_shelf_docs`（资料架 2×1）、`prop_plant`（盆栽 1×1）、`prop_wall_map`（墙挂线路图 2×1）、`prop_stove`（小煤炉 1×1）。
- 墙体：北墙是正面 32×48 + 顶帽；**侧墙与南墙在 3/4 视角下只露一圈薄墙帽**（代码里程序绘制 12px 米白帽 + 深褐内沿），请 `int_wall_cap_h.png` / `int_wall_cap_v.png`（32×12 / 12×32）按同色系出图，接入后替换程序绘制。
- 地板与墙面请直接按**米白/浅橡木**出（现为运行时叠罩提亮，`FloorLift`），以后可去掉叠罩。

## 反馈（GPT 填写，Claude 处理）
- 2026-11-17：用户要求高清晰度像素。新增 `Explore/PropsHD/` 中 6 件 P0.5 摆件与 `Explore/InteriorsHD/` 横竖墙帽，源图按 4 倍绘制、PPU128，对应 32 世界像素/格。旧 32px 调度室主体仍是技术样张，新旧图混用会有明显细节差；请在接入截图后决定主体整套高清重绘顺序。图形脚本 `tools/art/build_explore_hd_room.py`；尚未做 Unity Play 或 Android 视觉验收。
- 同日续做：旧调度室 14 张基础室内件、8 张原有设备家具也补齐高清候选版，现 `InteriorsHD/` 16 张、`PropsHD/` 14 张，均附 contact sheet。上述“旧主体待高清重绘”的缺口已补图；是否可正式采用，仍以 Unity 场景视角、入口与遮挡验收为准。
- 发现草图尺寸/元素冲突、需要新增设备或某元素无法按规格表现，写在这里并在 Notion「AI Handoffs」留一条（Source Agent: ChatGPT）。

## 2026-11-17 追加（Claude）：站务大厅已接入，请出专用图
- 大厅（`BuildHall`，16×11，瓷砖地）已接入，家具暂借调度室图占位。请按下表出专用图，**文件名与占格不变，Claude 直接替换 `Tex()` 名即可**：
  | 位置（格） | 现用占位 | 请出 | 占格 |
  |---|---|---|---|
  | (3,3) 北墙下 | `prop_strategy_board` | `prop_notice_board` 委托公告板（挂满委托单，可见红色封条） | 1×2（竖）；若做成 2×1 请告知，Claude 调整碰撞 |
  | (10,4) | `prop_dispatch_desk` | `prop_ticket_window` 售票窗口（玻璃 + 小窗口 + 票价牌） | 2×1 |
  | (14,3) | `prop_radio` | `prop_pa_stand` 广播机/台 | 1×1 |
  | (2,8)(11,8) | `prop_sofa` | `prop_bench` 候车长椅（木条，深褐） | 2×1 |
  | (4,8)(10,8) | `prop_teatable` | 可复用，或 `prop_lostfound`（失物招领箱） | 1×1 |
  | 北墙 (4,1) | `prop_route_screen_off` | `prop_info_screen` 信息屏（开/关两帧） | 2×1 墙挂 |
- 瓷砖地板：现用 `int_floor_tile_01/02`，运行时叠罩提亮；请按**浅灰米色**出，之后去掉叠罩。
- 看实际截图：`tools/compile/explore_review_v1/02b-explore-hall.png`（运行 `ExploreReview.Batch` 生成）。
