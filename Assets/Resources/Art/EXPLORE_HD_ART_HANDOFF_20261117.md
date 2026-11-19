# 探索地图高清像素素材交接（2026-11-17）

> 视觉质量更正：下表 48 张程序绘制图只是 128px 技术样张，用户已确认其细节不符合“高清晰度像素”目标。它们只能用于尺寸、路径和碰撞对齐测试；正式画风以本日 imagegen 概念图为新基准。

## 已交付候选图

| 目录 | 数量 | 内容 | 预览 |
|---|---:|---|---|
| `Explore/TilesHD/` | 10 | 室外草地 3、土路 3、道砟、横轨、站台面、黄线边 | `outdoor_hd_contact_sheet.png` |
| `Explore/InteriorsHD/` | 16 | 调度室木地板 3、灰砖 2、墙面/墙角 6、门 2、窗 1、墙帽 2 | `dispatch_core_hd_contact_sheet.png` |
| `Explore/PropsHD/` | 22 | 调度室原有设备 8、生活摆件 6、站区设施/植被 8 | `dispatch_additions_hd_contact_sheet.png`、`outdoor_props_hd_contact_sheet.png` |

另有 `Explore/Concept/wufeng_hd_material_study_v1.png`，依据游戏现有雾峰站背景生成，仅供材质密度与色调参考，不具备逐格接入条件。旧 `Tiles/`、`Interiors/`、`Props/` 的 32px 图仍保留，属于低细节技术样张。

新的 imagegen 视觉参考为 `Explore/Concept/wufeng_station_nf5k_hd_art_board_v3.png`（站房结构/车辆组合美术板）及 `Explore/Concept/nf5k_korean_consist_hd_concept_v3.png`（透明 NF-5K 三车编组，伪文字已去除）。这两张在局部材质与涂装上接近目标，但仍须按 40×24 地图重新拆层、对齐 7 格车长、检查真实俯视角和遮挡。
按用户“车头不要歪”修订的 `Explore/Concept/nf5k_straight_left_hd_concept_v7.png` 为车头方向过程稿；现已由 v8 构型稿取代。`Explore/Concept/dispatch_room_hd_art_direction_v2.png` 为室内整体画风参考。所有 imagegen 图仍属概念图，不能因透明底或视觉细节较高就直接接入地图。
主场景又补 `Explore/Concept/wufeng_buildings_hd_art_board_v1.png`（五栋建筑）和 `Explore/Concept/lin_biaohan_four_direction_hd_concept_v2.png`（主角四向立姿）作为拆图参考。仍缺独立建筑层、人物行走帧和正式 7 格车辆切片。
后续用户明确旧主角画像全部废弃、VN 不用男主画像，且探索男主不要过于特征化：上述 `lin_biaohan_four_direction_hd_concept_v2.png` 已撤销，改以 `Explore/Concept/player_anonymous_four_direction_concept_v1.png` 为低辨识度地图人形参考。列车 v7 外观复核发现头车偏纯动力车，与“驾驶室＋客运空间一体”设定不符；改用 `Explore/Concept/nf5k_integrated_headcar_consist_concept_v8.png` 作为当前构型参考。两者仍是概念图。

## Claude 接入注意

- 保持 40×24 地图和每格 32 世界像素；高清 PNG 的源图每格 128 像素，导入 PPU128、Point、无 mipmap。贴图显示尺寸与灰盒格一致。角色后续采用 128×192 源图时，世界显示尺寸仍为 32×48。
- 不要把 `Concept/` 参考图直接接入运行时。新高清文件放在独立目录，当前运行时代码并未自动切换到这些路径。先在独立场景/分支中替换图形读取路径，核对门格、脚底锚点、遮挡、横轨方向和地图交互点。
- 主场景目前有 10 张地面/轨道候选图、8 张站区物件候选图。建筑分层、NF-5K「耕牛」朝鲜涂装停靠编组、水岸边角、角色/NPC 动作仍缺，不应据此宣称主场景美术完工。站牌不嵌文字，交给 UI 叠加。0721「沙子飞猪」为沙能飞行载具，另行制作。

## 已核验与限制

`python tools/art/verify_explore_hd_assets.py` 检查了 48 张 PNG 的预期尺寸、每张 ≤32 色、Point/PPU128 元数据，以及 10 张室外图块横向边缘、地面图块与 5 张室内地板双轴边缘一致性。未运行 Unity Play、Windows Player 或 Android 平板验收。视觉表现仍为候选图，需对照游戏中雾峰站背景和场景截图精修。
