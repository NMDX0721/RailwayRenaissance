# 俯视角探索层 · 美术资产规格

室内设备与室外主场景分别见 `EXPLORE_INTERIOR_ART_BRIEF.md`、`EXPLORE_MAIN_SCENE_ART_BRIEF.md`；两条资源线共同使用本文的网格和车辆口径。

> 2026-11-13 以当前 `ExploreMapView` 灰盒为准：横屏 2D、3/4 俯视，地图 40×24 格，每格 32×32 世界像素，总逻辑范围 1280×768。`Concept/` 内首批图片仅用于确认画风和构图，不是可直接导入的逐格成品。

风格：精细像素风（横屏 2D、3/4 俯视），暖色低饱和，雾峰村山区旧铁路小站。站房材质、站台、铁路设施和色彩参照 `Assets/Resources/bg/station_sunset.png`、`platform_morning.png`；列车采用其中的 NF-5K「耕牛」朝鲜原厂深蓝、红白波浪与樱花涂装。0721「沙子飞猪」是沙能飞行载具，不能画成地图上停靠的机车。不要现代霓虹、不要赛博朋克。

## 通用技术规则
- 网格：**32×32 px / 格**。当前代码将 `ExploreMapView.T` 固定为 32，角色显示为 32×48；改为 16px 需要同步修改地图、碰撞和绘制逻辑。
- 高清晰度像素口径（2026-11-17 用户确认）：**逻辑格仍为 32×32，纹理源图采用 128×128/格，Unity PPU 设为 128**；多格图按 4 倍源图尺寸导出。角色若按此口径制作，应导出 128×192、PPU128。旧 `Tiles/`、`Interiors/`、`Props/` 的 32px 技术样张保留作对照，不得与高清图混用为最终风格。
- 导出：PNG，透明背景，**无抗锯齿、无缩放模糊**，点采样（Point/No Filter）。
- 调色板：每张图 ≤ 32 色，同一地图所有图共用一套调色板（首批生成后锁定并回填本文档）。

调度室首批逐格样张（2026-11-14）使用以下 13 色，后续同区域室内件以此为起点：`#3A2010`、`#6B4423`、`#82552B`、`#B8843A`、`#7A6B5A`、`#948573`、`#564B44`、`#A5937D`、`#1A2A3A`、`#E8D8C0`、`#2D4861`、`#8B3A2A`、`#658086`。透明像素不计入。样张见 `Explore/Interiors/dispatch_room_contact_sheet.png`；此色板尚需与整张室外地图共同验收后定为全地图色板。

室外基础图块首批（2026-11-15）新增草地 `#5B7846/#708F4F/#405C36`、土路 `#9D7D55/#B89765/#775B41`、道砟/站台浅石 `#A5937D`、轨钢 `#262B32/#837F79`、枕木 `#523927/#7A5332`、站台黄线 `#D4A843`；灰石沿用室内色板 `#7A6B5A/#564B44`。单张图色数仍低于 32。正式全地图色板待与建筑、角色和 NF-5K 一起审定。
- 高清室外候选图块位于 `Explore/TilesHD/`，共 10 张；`Explore/PropsHD/` 共 22 张（调度室 14、站区 8），`Explore/InteriorsHD/` 共 16 张（含墙帽）。这批使用共用低饱和色板，单图 4–15 色、Point 采样、无 mipmap；仍需同游戏中雾峰站背景和 Unity 场景实际显示效果复核。
- **视觉质量更正（2026-11-17）**：用户确认上述 48 张程序绘制图虽然为 128px 源图，但仍是低细节技术样张，**不是高清晰度像素美术成品**。目录名中的 `HD` 只说明源图像素密度，不能作为视觉验收标记。imagegen 的 `Concept/wufeng_station_nf5k_hd_art_board_v3.png`、`Concept/dispatch_room_hd_art_direction_v2.png` 与 `Concept/nf5k_straight_left_hd_concept_v7.png` 更接近目标细节密度，但尚未拆分到精确格、确定锚点或经 Unity 场景验证，仍属概念图。NF-5K v7 按用户反馈修正了车头歪斜。
- 光照：统一左上光源，阴影向右下，2 格内软阴影用半透明深色像素。
- 命名：`类别_名称_变体_帧.png`，小写下划线，如 `tile_ground_dirt_01.png`、`char_player_walk_down_0.png`。

## 1. Tileset（地面与结构，单图 tile 或 16 列图集）
| 类别 | 清单 |
|---|---|
| 地面 | 泥土 ×3 变体、碎石道砟、杂草、青苔石板、积水 |
| 铁轨 | 直轨(横/竖)、弯轨 ×4、道岔、车挡、枕木裸露段 |
| 站台 | 站台边缘(含黄线)、站台面、雨棚柱、长椅、站牌 |
| 建筑 | 站房外墙/屋顶(多格拼接)、调度室、候车室、车库(机车库门)、水塔、小卖部 |
| 植被 | 茶树垄、松树、灌木、野花、枯树、芦苇 |
| 围栏/杂物 | 木栅栏、铁丝网、油桶、木箱、茶筐、煤堆、自行车 |

## 2. 角色（主角 林彪悍）
- 4 向（下/上/左/右）× 行走 4 帧 + 待机 1 帧；尺寸 32×48（含头部）。
- 额外：拿工具 / 蹲下检修 / 举马灯 各 4 帧（下向即可）。
- 2026-11-17 用户新口径：探索地图男主保持**低辨识度、可代入**的普通成年男性轮廓，朴素深蓝工装、普通短深发、简化五官；不固定旧立绘的脸、发型、猪图案、怀表、背包、徽章或工牌等特征。`Concept/player_anonymous_four_direction_concept_v1.png` 是新四向造型参考，尚非独立动画帧。VN 不使用男主画像；旧 `Resources/characters/lin_biaohan*` 不作为新美术权威。
- 同日造型过程稿：保留 **25 岁成年男性**身份与地图用低辨识度要求，外观转为更纤细、柔和、带少年感的气质；`Concept/player_soft_youthful_four_direction_concept_v2.png` 替代 v1。性取向不能单凭外貌或刻板动作判断；此处只更新视觉造型，不擅改剧情关系。VN 仍不显示男主画像。
- 同日用户追加彩虹旗与表情方向：**当前四向参考为 `Concept/player_pride_four_direction_concept_v3.png`**，在原深蓝工装左胸增加小型六色彩虹旗布章；正面及侧面为柔和、略俏皮的微笑。v2 降为过程稿。该布章是用户指定的明确视觉元素；不根据表情推断性取向，也不恢复旧画像定脸或 VN 男主立绘。
- 用户反馈胸前彩虹旗像贴纸、表情仍不够到位：**当前四向参考改为 `Concept/player_pride_sleeve_four_direction_concept_v4.png`**，彩虹旗改缝在工装上臂外侧，增加暗色布边与缝线；衬衫偏浅紫、衣服略收身，正面与侧面采用温柔、自信、略带俏皮的神态。v3 降为过程稿。此图仍需在游戏目标显示尺寸检查布章是否可辨。
- **当前探索造型参考为 `Concept/player_photo_reference_four_direction_concept_v7.png`。** 按用户照片重绘短直黑发、深蓝绗缝马甲与浅灰长裤，再调整眼神、闭口微笑和左上臂六色彩虹布章。保留 25 岁角色设定，v5、v6 留作过程对比。

## 3. NPC（先做 6 个，同规格 4 向 × 4 帧）
老陈（司机）、张工（技师）、李阿姨（站务）、赵师傅（调度）、小芳（见习）、茶嫂（货主，常驻站外）。
旅客通用小人 ×6 种（老人/孩子/挑担/背包/学生/矿工），仅需 2 向 × 4 帧。

## 4. 机车与车辆
- 当前灰盒停靠编组总长 **7 格**：NF-5K「耕牛」头车 3 格、客厢各 2 格，沿横向轨道排列；正式美术须和地图的 3/4 俯视视角一致。行驶/冒烟为后续状态，当前仅使用停靠状态。
- 头车须体现既定的**驾驶室＋贯通客运空间**：前端驾驶室、隔离的动力设备区、后半部可见客室窗与客门。不能画成纯独立柴油机车。`Concept/nf5k_integrated_headcar_consist_concept_v8.png` 为当前外观参考；其红色宣传位暂留白，正式标语与铭牌应按车辆设定单独校对，不采信 AI 生成的伪文字。
- 客厢 ×2、货厢(敞车) ×1，可与机车拼接；货厢当前地图未显示。

## 5. 交互点与 UI 图标（16×16 / 24×24）
站台、调度室、候车室、车库、道口、茶摊：各一个高亮光圈 + 一个感叹号气泡；时间/天气图标各 4 种。

## 6. 首批样张（先交这些，确认画风再批量）
1. 一张 20×12 格的站区俯视拼图预览（站房、两股道、站台、茶树坡）；这是画风确认图，不作为地图背景直接接入。
2. 主角 down 方向 4 帧行走。
3. NF-5K「耕牛」朝鲜原厂涂装停靠图。0721 飞行载具另立资源任务。

## 7. GPT 提示词模板
> "Detailed pixel art, top-down 3/4 view like Stardew Valley, 32x32 tile grid, no anti-aliasing, limited 32-color warm muted palette, light from top-left, small rural Korean mountain railway station in 2076, worn but tidy, no neon. Output: transparent-background PNG sprite sheet, tiles aligned exactly to grid."

## 8. 代码对接约定（开发侧）
- 资源放 `Assets/Resources/Art/Explore/{Tiles,Characters,Props,Vehicles,UI}/`。
- 当前地块、建筑、列车均由 `TabletExploreAppUI.ExploreMapView.Draw` 程序绘制；正式 Tileset/车辆图接入需修改绘制层，保留原网格碰撞、寻路和交互坐标，不能只覆盖同名文件。
- 角色通过 `ExploreCharacters.TrySprite` 读取 `Characters/char_{id}_{walk|idle}_{down|up|left|right}_{n}.png`，逻辑显示尺寸为 **32×48 世界像素**。高清角色使用独立 **128×192 PNG**。有单侧服饰标记的角色须提供独立左右帧。
- 2026-11-18 已放入男主四向待机 4 张、行走 16 张。待机源为 v7，行走源为 `Concept/player_photo_reference_walk_concept_v1.png`。导出时以头部中心稳定横向位置，统一鞋底基线，Point 采样。可用 `tools/art/export_explore_player_frames.ps1` 向空目录重导出。
- Unity 6000.6.9f1 的 `ExploreReview.Batch` 已通过，截图 `tools/compile/explore_review_v1/01-explore-start.png` 中可见站房门前的新男主。四向步态连贯性与平板显示仍需实机观察。
- 地图男主位图按 34×51 世界像素等比绘制，脚底居中锚定。其他位图角色沿用 32×48 的基准尺寸。动画源图、格线碰撞和遮挡排序沿用现有约定。
- 雾峰站室外使用 imagegen 绘制的 `Scenes/scene_wufeng_station_v2.png` 和 `Scenes/scene_wufeng_east.png`。站区 40×24 格，东段 16×24 格。渲染时配准四条钢轨与站台黄线，人物与列车分别叠绘。
- 停靠编组使用 v8 裁切图 `Vehicles/train_nf5k_parked.png`。整列显示宽 224、高约 66 世界像素，车轮沿重绘场景的钢轨锚定，碰撞沿用七格停靠位。
- 建筑请分屋顶、墙体/地面与门口等层，确保角色遮挡关系；小地图继续读取同一网格，不使用整图缩放代替导航数据。

## 9. 2026-11-13 首批概念样张

| 文件 | 实际尺寸 | 用途与限制 |
|---|---|
| `Explore/Concept/station_layout_concept_v1.png` | 1619×971 | 已被 v2 取代；不是 20×12 格无缝拼图。 |
| `Explore/Concept/player_walk_down_concept_v1.png` | 2048×768，透明 | 已废弃的旧主角探索稿；不再作为男主造型或动画参考。 |
| `Explore/Concept/train_0721_parked_concept_v1.png` | 2172×724，透明 | 身份错误的探索稿，不得用于正式制作。 |
| `Explore/Concept/station_layout_wufeng_nf5k_concept_v2.png` | 1620×971 | 对照游戏现有雾峰站重绘的当前构图参考。 |
| `Explore/Concept/train_nf5k_korean_livery_concept_v2.png` | 2172×724，透明 | 对照 NF-5K 外形与雾峰站朝鲜涂装重绘的当前车辆参考。 |

站区与 NF-5K 概念图仍是构图/涂装样张，未满足逐格交付条件。主角旧稿均已废弃，采用上文低辨识度新口径。

首批可接入成品的验收点：地块边缘无缝、建筑门口与灰盒交互格一致、角色脚底锚点一致、停靠列车不超出当前 7 格阻挡范围；PNG 使用透明通道和 Point 采样。概念样张不满足这些接入条件。
