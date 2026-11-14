# 俯视角探索层 · 美术资产规格

室内设备与室外主场景分别见 `EXPLORE_INTERIOR_ART_BRIEF.md`、`EXPLORE_MAIN_SCENE_ART_BRIEF.md`；两条资源线共同使用本文的网格和车辆口径。

> 2026-11-13 以当前 `ExploreMapView` 灰盒为准：横屏 2D、3/4 俯视，地图 40×24 格，每格 32×32 世界像素，总逻辑范围 1280×768。`Concept/` 内首批图片仅用于确认画风和构图，不是可直接导入的逐格成品。

风格：精细像素风（横屏 2D、3/4 俯视），暖色低饱和，雾峰村山区旧铁路小站。站房材质、站台、铁路设施和色彩参照 `Assets/Resources/bg/station_sunset.png`、`platform_morning.png`；列车采用其中的 NF-5K「耕牛」朝鲜原厂深蓝、红白波浪与樱花涂装。0721「沙子飞猪」是沙能飞行载具，不能画成地图上停靠的机车。不要现代霓虹、不要赛博朋克。

## 通用技术规则
- 网格：**32×32 px / 格**。当前代码将 `ExploreMapView.T` 固定为 32，角色显示为 32×48；改为 16px 需要同步修改地图、碰撞和绘制逻辑。
- 导出：PNG，透明背景，**无抗锯齿、无缩放模糊**，点采样（Point/No Filter）。
- 调色板：每张图 ≤ 32 色，同一地图所有图共用一套调色板（首批生成后锁定并回填本文档）。

调度室首批逐格样张（2026-11-14）使用以下 13 色，后续同区域室内件以此为起点：`#3A2010`、`#6B4423`、`#82552B`、`#B8843A`、`#7A6B5A`、`#948573`、`#564B44`、`#A5937D`、`#1A2A3A`、`#E8D8C0`、`#2D4861`、`#8B3A2A`、`#658086`。透明像素不计入。样张见 `Explore/Interiors/dispatch_room_contact_sheet.png`；此色板尚需与整张室外地图共同验收后定为全地图色板。
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
- 配色：深蓝旧制服 + 工牌；与立绘发型一致。

## 3. NPC（先做 6 个，同规格 4 向 × 4 帧）
老陈（司机）、张工（技师）、李阿姨（站务）、赵师傅（调度）、小芳（见习）、茶嫂（货主，常驻站外）。
旅客通用小人 ×6 种（老人/孩子/挑担/背包/学生/矿工），仅需 2 向 × 4 帧。

## 4. 机车与车辆
- 当前灰盒停靠编组总长 **7 格**：NF-5K「耕牛」头车 3 格、客厢各 2 格，沿横向轨道排列；正式美术须和地图的 3/4 俯视视角一致。行驶/冒烟为后续状态，当前仅使用停靠状态。
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
- 角色已有 `ExploreCharacters.TrySprite` 的单帧加载接口，路径为 `Characters/char_{id}_{walk|idle}_{down|up|left|right}_{n}.png`。左向缺图时可镜像右向。每张成品必须单独导出到 **32×48 px**，不能把概念横排图直接改名使用。
- 建筑请分屋顶、墙体/地面与门口等层，确保角色遮挡关系；小地图继续读取同一网格，不使用整图缩放代替导航数据。

## 9. 2026-11-13 首批概念样张

| 文件 | 实际尺寸 | 用途与限制 |
|---|---|
| `Explore/Concept/station_layout_concept_v1.png` | 1619×971 | 已被 v2 取代；不是 20×12 格无缝拼图。 |
| `Explore/Concept/player_walk_down_concept_v1.png` | 2048×768，透明 | 四个向下行走姿态参考；不是四张 32×48 帧。 |
| `Explore/Concept/train_0721_parked_concept_v1.png` | 2172×724，透明 | 身份错误的探索稿，不得用于正式制作。 |
| `Explore/Concept/station_layout_wufeng_nf5k_concept_v2.png` | 1620×971 | 对照游戏现有雾峰站重绘的当前构图参考。 |
| `Explore/Concept/train_nf5k_korean_livery_concept_v2.png` | 2172×724，透明 | 对照 NF-5K 外形与雾峰站朝鲜涂装重绘的当前车辆参考。 |

现用 v2 仍是构图/涂装样张，未满足逐格交付条件。主角 v1 四姿态也仅是动作参考，不锁定其面容。

首批可接入成品的验收点：地块边缘无缝、建筑门口与灰盒交互格一致、角色脚底锚点一致、停靠列车不超出当前 7 格阻挡范围；PNG 使用透明通道和 Point 采样。概念样张不满足这些接入条件。
