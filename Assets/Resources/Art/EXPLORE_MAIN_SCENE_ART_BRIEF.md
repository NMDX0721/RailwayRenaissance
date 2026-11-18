# 巡线主场景室外美术交付单（2026-11-14）

2026-11-18 室外扩为 56×24 格，保留原有 40×24 格站区，向东拼接 16×24 格道口与货运栈台。`Explore/Scenes/scene_wufeng_station_v2.png` 重绘站区，`scene_wufeng_east.png` 用于东段，沿黄线和四条钢轨的位置配准。停靠车辆使用 `Explore/Vehicles/train_nf5k_parked.png` 的 v8 外观，保留朝左的七格停靠位。男主位图高 51 世界像素。地图和系统入口在右上，底部保留九个可选工具栏位。

## 拼接和扩展

- 站区三栋建筑依钟面山墙、金属顶线路室、绿雨棚候车室区分。茶摊和车库之间增加铁路商店，入口位于 x19,y19，可查看柜台陈设。
- 东段道口在 x50,y13，货运栈台在 x53,y6。拼接先对齐黄线与钢轨，再用不足一格的窄带过渡材质。下一段往东可接村道和桥涵，往西可接进站信号和弯道。每张场景图单独作画，交付时记录两端各条钢轨、路面和可通行入口的实际位置。

本单与 `EXPLORE_INTERIOR_ART_BRIEF.md` 并行。雾峰站采用横屏 2D、3/4 俯视，每格 32 世界像素。下方的原始布局锚点描述站区块。

## 已确定的视觉基准

- 站房、站台、旧砖木材质、信号设施与晨光：`Assets/Resources/bg/station_sunset.png`、`platform_morning.png`，并参照 `ExternalAssets/wufeng_signage_ref.png` 的站牌制式。
- 停靠车辆：NF-5K「耕牛」**朝鲜原厂深蓝涂装**，红白波浪、樱花和翼星徽；以 `Explore/Concept/train_nf5k_korean_livery_concept_v2.png` 为俯视样张。0721「沙子飞猪」是沙能飞行载具，不放在铁轨上。
- 逐格输出遵守 `EXPLORE_ASSET_SPEC.md`：透明 PNG、Point 采样、单图不超过 32 色。室内首批 13 色暂作共用基色，室外扩色需先在规格里记录。

## 地图布局锚点（以当前灰盒为准）

| 区域 | 当前格坐标 | 美术要求 |
|---|---|---|
| 站务大厅 | x5–14，y3–8；门 x10,y8 | 外墙与红瓦屋顶分层，门口留可走格 |
| 调度室 | x18–23，y3–7；门 x20,y7 | 外形与室内入口对应 |
| 候车室 | x26–32，y3–7；门 x29,y7 | 与站房共用建材，但轮廓可辨 |
| 站台与双股道 | 站台 y10；两股道 y11、y12 | 站台边、磨损黄线、枕木和道砟可无缝衔接 |
| NF-5K 停靠位 | x8–14，y11，共 7 格 | 头车 3 格、客厢各 2 格；车辆视觉不能误导行走碰撞 |
| 茶田、茶摊 | 茶田 x2–11,y15–20；茶摊 x13–16,y16–18 | 茶垄重复图块、摊位入口和小路 |
| 车库 | x24–31,y15–19；门 x27,y15 | 大门朝北，预留车辆检修视觉 |
| 水塘与道口 | 水塘 x33–37,y18–21；道口小径 x35,y13–16 | 水岸转角、道口与双股道连通 |

## 制作顺序

1. **室外基础图块**：草地、泥路、碎石道砟、横向直轨、站台面、站台边与黄线；每类至少一个可无缝基本块和必要边角。交付目录 `Explore/Tiles/`。
2. **建筑外观**：站务大厅、调度室、候车室、车库、茶摊的屋顶、墙体与门分层；交付目录 `Explore/Buildings/`。先完成大厅和调度室两栋，与调度室室内样张形成同一视觉闭环。
3. **停靠车辆与站区设施**：NF-5K 头车、两节客厢、雾峰站牌、传统信号机、道口杆、长椅与公告栏；车辆在 `Explore/Vehicles/`，设施在 `Explore/Props/`。
4. **环境变化**：茶垄、松树、围栏、水岸、路面修补和少量樱花；以可拼接资源呈现，不把装饰画进碰撞格里。

## 接入验收

- 每批附 `*_contact_sheet.png` 和尺寸/色数/透明通道检查结果；概念图与正式图分目录。
- Claude 接入时保持现有地图碰撞、寻路与交互点坐标，并在 `ExploreReview.Batch` 的大厅、站台、车库、茶摊视角截图中检查门口与遮挡。
- 当前状态（2026-11-15）：`Explore/Tiles/` 已有 10 张 32×32 室外基础**技术样张**：草地 3、土路 3、道砟 1、横轨 1、站台面 1、黄线边缘 1；附 `outdoor_base_contact_sheet.png`。建筑、NF-5K 车辆和环境边角成品尚未制作。调度室 22 张逐格图也是室内技术样张；两批均未通过 Unity 场景验收，视觉精度还需对照现有背景精修。

## 2026-11-17 高清像素进展

- 用户要求高清晰度像素。新增 `Explore/TilesHD/` 10 张 128×128 候选图块，保持原 32 世界像素格与地图坐标不变；预览为 `outdoor_hd_contact_sheet.png`。源图制作脚本为 `tools/art/build_explore_hd_tiles.py`，校验脚本为 `tools/art/verify_explore_hd_assets.py`。
- `Explore/Concept/wufeng_hd_material_study_v1.png` 根据游戏现有站景生成，只供材质密度与配色参照，不是无缝图块。旧 `Explore/Tiles/` 为低细节技术样张，暂不作为最终视觉验收基准。
- 待补：建筑外观分层、NF-5K 朝鲜涂装 7 格停靠编组、主角与 NPC 高清四向动作、水岸/道口边角、茶田/松树/围栏、站牌和信号设施。当前 10 张高清图块尚未接入 Unity 场景或验证 Android 平板。
- 同日追加 `Explore/PropsHD/` 站区 8 张：留白站牌（文字由 UI 叠加）、信号机红/绿、站台长椅、公告栏、道口杆、茶树垄和松树。预览 `outdoor_props_hd_contact_sheet.png`。上项中的站牌、信号设施与茶树/松树已形成候选图；建筑外观、NF-5K 编组、角色动作、水岸与道口地面仍缺。
- 视觉修正：以上 `TilesHD/` 与 `PropsHD/` 仅为 128px 技术样张，细节密度仍不足，不能标成最终高清像素美术。新 imagegen 主场景美术板 `Concept/wufeng_station_nf5k_hd_art_board_v3.png`、透明 NF-5K 三车编组 `Concept/nf5k_korean_consist_hd_concept_v3.png` 用作质量与涂装参考；后者已去除生成的伪文字，但尚未验证 7 格比例、独立分层、精确格线及真实透明边缘。两张都不能直接接入运行时。
- 方向修订：`Concept/nf5k_korean_consist_left_hd_concept_v4.png` 将头车改到左侧，与旧雾峰站 v2 构图方向一致；v3 右头车稿留作过程记录。v4 仍是单张透明概念，不是拆好的 3+2+2 格 Sprite。
- 用户再次强调车头不要歪。`Concept/nf5k_straight_left_hd_concept_v7.png` 为当前优先几何/画风参考：头车朝左，车头、车顶、侧窗与两节客厢沿水平轴线；图片为 2172×724 RGBA，画布四角 alpha=0。v5 正面过宽、v6 背景半透明、抠图试稿出现伪影，均不作为接入候选。v7 仍未按 3+2+2 格切片或验证车牌细节。
- imagegen 新增 `Concept/wufeng_buildings_hd_art_board_v1.png`，包含大厅、调度室、候车室、车库、茶摊五栋建筑独立外观；`Concept/lin_biaohan_four_direction_hd_concept_v2.png` 包含主角完整四向立姿。建筑未分屋顶/墙/门层，主角未提供行走帧及 128×192 独立 PNG；两者仍为美术参考。
- 2026-11-17 新修订：旧画像定脸的 `lin_biaohan_four_direction_hd_concept_v2.png` 已废弃；`Concept/player_anonymous_four_direction_concept_v1.png` 为低辨识度探索男主新参考，VN 不用男主画像。列车 `Concept/nf5k_integrated_headcar_consist_concept_v8.png` 按车辆设定补出头车客室窗/门，替代纯机车感较强的 v7；仍未制作精确 3+2+2 格成品。
- 同日造型再修订：`Concept/player_soft_youthful_four_direction_concept_v2.png` 替代普通成年轮廓 v1，呈现纤细、柔和、少年感较强但设定年龄仍为 25 岁的探索小人；不恢复旧画像定脸，VN 仍不用。
- 同日最终追加：当前主角四向外观参考为 `Concept/player_pride_four_direction_concept_v3.png`，增加用户指定的彩虹旗布章与柔和俏皮表情。v2 为过程稿；行走动画和逐帧尺寸仍待制作。
- 随后反馈修订：`Concept/player_pride_sleeve_four_direction_concept_v4.png` 取代胸前贴片 v3，使用上臂缝制布章、浅紫衬衫和更自然的柔和笑容；仍是概念，不是行走帧。
- 再修正：`Concept/player_pride_soft_youthful_concept_v5.png` 为当前优先参考，保留袖章，收敛表情与衣服造型以减少油腻感；v4 保留为过程稿。
