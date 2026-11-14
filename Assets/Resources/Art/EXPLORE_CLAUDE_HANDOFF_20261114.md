# 给 Claude 的巡线美术接入交接（2026-11-14）

范围：调度室第一批逐格图形、雾峰站室外主场景的美术边界。逻辑、场景区域切换、UI 与资源接入由 Claude 负责；本交接不要求直接采用概念图作运行时贴图。

## 已交付的逐格 PNG

- `Explore/Interiors/`：`int_floor_wood_01..03`（各 32×32）、`int_floor_tile_01..02`（各 32×32）、`int_wall_top`（32×32）、`int_wall_front`（32×48）、`int_wall_corner_{nw,ne,sw,se}`（各 32×32）、`int_door_{open,closed}`（各 32×64）、`int_window_01`（32×48），共 14 张。
- `Explore/Props/`：`prop_dispatch_desk`（64×32）、`prop_strategy_board`（32×64）、`prop_route_screen_{off,on}`（各 64×32）、`prop_radio`（32×32）、`prop_wall_clock`（32×32）、`prop_sofa`（64×32）、`prop_teatable`（32×32），共 8 张。
- `Explore/Interiors/dispatch_room_contact_sheet.png` 仅作人工总览。`Explore/Concept/dispatch_room_reference_v1.png` 是 imagegen 整体画风参考，不满足逐格尺寸，不能接入运行时。

已对 22 张独立 PNG 检查像素尺寸、色数（每张低于 32 色）和 22 个不同 GUID。对应 `.meta` 指定 Texture/Sprite、Point 采样、32 Pixels Per Unit、无 mipmap 和透明通道。地板与墙体内部为不透明像素；家具/门窗使用透明背景。色板记录在 `EXPLORE_ASSET_SPEC.md`。

## 接入时要保持的关系

1. 调度室布局以 `docs/for-ai/EXPLORE_INTERIORS_AND_HUD_DESIGN.md` §3.2 为准：北墙挂钟/线路屏/窗，中部调度台/电台/策略板，南侧沙发/茶几与出口。调度台打开发车方案，策略板打开经营策略；屏幕开/关两态是图形，不代表已实现状态逻辑。
2. 图形底边与格线对齐，角色/家具遮挡顺序、门格碰撞和区域出入口由接入层验证。现有代码尚没有使用这些文件，不能把“文件存在”算作室内完成。
3. 室外主场景继续推进：`EXPLORE_MAIN_SCENE_ART_BRIEF.md` 已列出 40×24 格布局、室外 Tileset、建筑分层、道口及 NF-5K 停靠编组。当前室外只有 v2 概念图，尚无逐格成品。NF-5K「耕牛」为朝鲜原厂深蓝涂装；0721「沙子飞猪」是沙能飞行载具。

## 验证和待精修

- 这批是可接入的**技术样张**：当前像素细节比游戏现有雾峰站背景简化，色板尚需同室外主场景一起审定。接入后请先截取调度室实际运行画面，反馈比例、遮挡与画风差异，再精修图片。
- 尚未进行 Unity 导入、Play、Windows Player 或 Android 平板显示验收。室内区域框架和门格切换的实现状态以 Claude 当前代码为准。
- 资源可由 `tools/art/build_explore_dispatch_assets.ps1` 重建；如要手工精修，请先以现有 PNG 为新版本输出，避免脚本覆盖已修图。
