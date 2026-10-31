# AI生成资产提词总集（v4.1 All-in-One）

> 版本：v4.1（2026-09-18 合并美术+配乐为统一文档；v4.1 已生成背景归档）
> 用途：所有AI生成资产的提词汇总——角色立绘、场景背景、BGM配乐、歌词歌曲
> 美术工具：mimo 2.5 | 执行任务：ds / ds718
> 音乐工具：Suno AI v4.5+（Custom Mode，Style ≤1000 chars / Lyrics ≤3000 chars）
> 四首有歌词歌曲的历史提词正文见 [AI配乐提示词_历史原稿](AI配乐提示词_历史原稿.md) §四；本文件下文的“纯音乐”说明只适用于 BGM 章节。历史原稿由 Git 版本追溯恢复，歌曲同步时间另行核对。
> 角色立绘风格：1024×1536 高清二次元动漫风格（已从16-bit像素画升级）
> 场景背景风格：16-bit retro pixel art，暖色调，类似 Stardew Valley

---

## 目录

### 一、角色立绘
- [通用规范](#通用规范)
- [1.1 林彪悍（主角）](#11-林彪悍主角) ✅ 11/16
- [1.2 老陈（陈守正）](#12-老陈陈守正) ❌ 16张待重绘
- [1.3 张工（张德厚）](#13-张工张德厚) ❌ 待升级
- [1.4 李阿姨（李桂芳）](#14-李阿姨李桂芳) ❌ 待升级
- [1.5 王小弟（王晨阳）](#15-王小弟王晨阳) ❌ 待升级
- [1.6 赵师傅（赵德山）](#16-赵师傅赵德山) ❌ 待升级
- [1.7 小芳](#17-小芳) ❌ 待升级
- [1.7.1 导师 daoshi](#171-导师daoshi--待绘制占位) ❌ 待生成
- [1.8 岁月（AI助手）](#18-岁月ai-助手界面头像) ✅ 17张
- [1.9 16表情差分图谱模板](#19-16表情差分图谱44网格铺满全画面)

### 二、场景背景

> **归档原则（2026-10-01）**：标 ✅ / 📦 的场景提词**已出图，冻结归档**——世界观/设定变更一律不回改其提词正文（需变更时重绘另起新版本）；❌ 待生成 / ⚠️ 待重绘的场景才按 `参考资料/设定/` 文档更新后出图。
- [2.0 前置 · NF-5 外构参考图](#20-前置--nf-5-外构参考图nf5_exterior_ref) ✅ 已收并定性（朝控局涂装）
- [2.0.1 C-55000 敞车外构](#201-c-55000-敞车外构c55000_exterior_ref--前置) ✅ 已收锁定
- [2.1 实验室](#21-实验室智能调度系统实验室) 📦 已归档
- [2.2 导师办公室](#22-导师办公室) 📦 已归档
- [2.3 驾驶舱·白天](#23-驾驶舱白天0721号前舱) 📦 已归档
- [2.4 驾驶舱·夜晚](#24-驾驶舱夜晚0721号前舱) 📦 已归档
- [2.5 客舱·白天](#25-客舱白天新绘0721号后舱) 📦 已归档
- [2.6 客舱·夜晚](#26-客舱夜晚新绘0721号后舱) 📦 已归档
- [2.7 停机坪](#27-停机坪) 📦 已归档
- [2.12 大同江茶馆 / 嘉颖徐茶室兼办公室](#212-大同江茶馆--嘉颖徐茶室兼办公室tea_house-标杆定稿) ✅ 标杆定稿
- [2.13 Wiki横幅](#213-wiki-横幅wiki-banner) 📦 已归档
- [2.14 群山天空](#214-群山天空mountain_sky) ✅ 已入库
- [2.15 空中俯瞰·铁轨](#215-空中俯瞰铁轨aerial_view) ✅ 已入库
- [2.16 雾峰村夕阳](#216-雾峰村夕阳village_sunset) ✅ 已生成
- [2.16.1 雾峰站站牌样式](#2161-雾峰站站牌样式wufeng_signage_ref--前置) ✅ 已锁定（2026-10-01）
- [2.17 车站夕阳](#217-车站夕阳station_sunset) ✅ 已入库（2026-10-01 重绘定稿）
- [2.17.1 雾峰站正面全景（station）](#2171-雾峰站正面全景stationpng-废弃--待重绘) ❌ 废弃/待重绘
- [2.17.2 田园铁路鸟瞰全景（railway_map_aerial）](#2172-田园铁路鸟瞰全景railway_map_aerialpng-待重绘) ❌ 待重绘
- [2.18 傍晚站台](#218-傍晚站台platform_evening) ✅ 已入库（定稿版）
- [2.19 边境小镇](#219-边境小镇border_town) ❌ 待生成
- [2.20 补给站](#220-补给站supply_station) ❌ 待生成
- [2.21 中国上空](#221-中国上空china_sky) ❌ 待生成
- [2.22 河北小镇](#222-河北小镇hebei_town) ❌ 待生成
- [2.23 河南小镇](#223-河南小镇henan_town) ❌ 待生成
- [2.24 车辆段·晨](#224-车辆段晨depot_morning-待重绘) ❌ 待重绘
- [2.25 线路巡视](#225-线路巡视railway_track) ✅ 已入库（定稿版）
- [2.26 松桥站](#226-松桥站songqiao_station) ✅ 已入库（定稿版）
- [2.27 车站·夜](#227-车站夜station_night) ✅ 已入库
- [2.28 站长办公室](#228-站长办公室station_office-待重绘) ❌ 待重绘
- [2.29 站台·晨](#229-站台晨platform_morning) ❌ 待生成
- [2.30 车厢内部](#230-车厢内部train_inside) ❌ 待生成
- [2.31 纯黑](#231-纯黑black) 📦 已归档
- [2.32 边境追击CG](#232-边境追击三面合围chase_sky--cg) ❌ 待生成
- [2.33 统一便当店](#233-统一便当店平壤后街shop) ❌ 待生成
- [2.34 现代咖啡厅](#234-现代咖啡厅office_modern) ❌ 待生成
- [2.35 新闻发布会](#235-新闻发布会press_conference) ❌ 待生成
- [2.36 全屏新闻画面](#236-全屏新闻画面news_broadcast) ❌ 待生成
- [2.37 序章开场·标题卡](#237-序章开场标题卡title_card) ❌ 待生成
- [2.38 序章新闻·沙能诞生](#238-序章新闻沙能诞生news_2050) ❌ 待生成
- [2.39 序章新闻·铁路衰落](#239-序章新闻铁路衰落news_2072) ❌ 待生成
- [2.40 列车 Sprite（NF-5K/SY-22 侧视）](#240-列车-spritenf-5ksy-22-侧视--规划-a3) ❌ 待生成（额度重置后出图；NF-5K 参考已锁，SY-22 外构待锁，轨道代码画）
- [2.41 夜路·道口（night_road）](#241-夜路道口night_road) ❌ 待生成

### 三、BGM配乐（Suno AI）
- [3.1 melancholy](#31-melancholyy忧郁怀旧) — 废弃铁路/怀旧 3:30
- [3.2 emotional](#32-emotional感人温暖) — 感人对话 3:30
- [3.3 determination](#33-determination坚定希望) — 维修/首班/揭露 3:30
- [3.4 morning](#34-morning清晨宁静) — 时间过渡 3:00
- [3.5 warm](#35-warm温暖安心) — 团队日常 3:30
- [3.6 news](#36-news新闻播报) — 新闻/背景 3:00
- [3.6.1 news_explosive](#361-news_explosive爆炸性新闻对主角极度不利) — 爆炸性反击新闻 3:00
- [3.7 calm](#37-calm平静日常) — 日常运营 3:30
- [3.8 train_ambient](#38-train_ambient火车行驶环境) — 车厢内 3:00
- [3.9 ambient_nature](#39-ambient_nature自然环境音) — 户外/山林 2:30
- [3.10 mystery](#310-mystery悬疑未知) — 岁月启动/边境 2:30
- [3.11 curiosity](#311-curiosity好奇探索) — 岁月性格 2:30
- [3.12 silence](#312-silence寂静留白) — 序章结束 1:30

### 四、UI元素
- [现有UI资产](#41-现有-ui-资产可直接复用无需生成)
- [缺失图标](#42-缺失的图标需生成11个)

### 五、生成优先级
- [完整优先级表](#五生成优先级)

### 六、待生成清单
- [背景图清单](#61-背景图已有提示词待生成)
- [CG插画清单](#62-cg-插画9-张提词已写)
- [已入库清单](#64-已入库背景图)

**序章裸跑缺图队列（2026-10-01 audit 全 10 幕；提词均已写好，出图即消除黑屏兜底）——按幕推进优先级排序：**

| 优先 | bg 名 | 引用幕（次数） | 提词节 |
|------|-------|---------------|--------|
| 1 | `platform_evening` | 04（2） | §2.18（✅已入库） |
| 2 | `railway_track` | 05（3） | §2.25（✅已入库） |
| 3 | `songqiao_station` | 05（2） | §2.26（✅已入库） |
| 4 | `night_road` | 05（1） | §2.41 |
| 5 | `depot_morning` | 07（5） | §2.24 |
| 6 | `station_night` | 06（4） | §2.27（✅已入库） |
| 7 | `platform_morning` | 08（2）+09（1） | §2.29 |
| 8 | `train_inside` | 08（2） | §2.30 |
| 9 | `station_office` | 09（3） | §2.28 |
| 10 | `china_sky` | 03（6） | §2.21 |
| 11 | `border_town` / `supply_station` / `hebei_town` / `henan_town` | 03 各1 | §2.19-2.23 |
| 12 | `shop`（平壤后街） | 02（1） | §2.33 |
| — | `black` / `news_2050` / `news_2072` | 01/10 | **非缺图**：设计即黑屏字卡，引擎 solid black 兜底为正解 |

### 七、已生成资产归档（见文件末尾）
- [已归档背景图](#七已生成资产归档) 📦 §2.1-2.7, §2.12-2.13, §2.31

---

## 一、角色立绘

### 通用规范

- **工作流**：第1步生成主图（1024×1536，锁定面部/服装/光照）→ 第2步用主图作 image reference，只发一句增量表情提示，denoising 0.2-0.3（同服装/同姿势/同光照）
- **设计文档引用原则**：设计文档不做去叙事化；本提词只给AI绘图用序号参考（§1.x / §2.x），不反向改写设定正文
- **绘图前置（Prerequisite refs）**：跨场景复用的实体先锁一张参考图，再画依赖它的 bg/CG；生成时挂 image reference，提词只写构图/光影/氛围差异。已锁：**NF-5K 外构**（`ExternalAssets/nf5k_reference.png`，2026-09-28 定稿，朝鲜升级版）、**雾峰站站牌**（`ExternalAssets/wufeng_signage_ref.png`，2026-10-01 锁定 → §2.16.1）、**C-55000 敞车**（§2.0.1）。NF-5 外构：已收并定性（`ExternalAssets/nf5_exterior_ref.png`＝朝控局涂装，世界观 §十三；提词 §2.0）。待锁：0721 外构（已知缺口）、SY-22 灰雀、千里马品牌标、**架空车辆族谱其余车型外构（NF-7/金策型/乌金型/SY-30 雨燕/金达莱卧铺/黎明客车组/G-17 罐车，提词均待写 → 车辆设定 §四族谱）**。角色已有立绘的，CG 一律挂立绘不从零描述外貌
- **站牌文字规范**：按 §2.16.1 锁定制式（主名雾峰/무봉/Wufeng＋邻站蓝带），禁止「終點站/始發/종착역」（对开往返车站不写死方向）；墙面禁止巨型手写站名，用门上小搪瓷站牌+宣传栏+玻璃橱窗时刻表
- **调色板与技术规格（AP调研同步 2026-10-03）**：主色 深棕 #3a2010 / 暖棕 #6b4423 / 琥珀 #b8843a；辅色 米白 #e8d8c0 / 灰棕 #7a6b5a / 暗红 #8b3a2a；点缀 金 #d4a843 / 深蓝 #1a2a3a（沙能冷色对位铁路暖棕）。风格定位 Narrative Pixel Art with Warm Cinematic Lighting；规格：角色 64×96 / 场景 320×180 / 整数缩放 Nearest Neighbor / 单图 8–16 色限定调色板。现实参考图（挂图用，位于 `E:\NMDX\NMDX-User Workspace\AP调研\02_视觉调研\`）：雾峰站台第一参考＝建水米轨小火车 `cn_07_jianshui_train.jpg`；NF-5 外观对照 DF4 `real_01_DF4_0127.jpg`；废弃站台 `real_04~06`；平壤地铁开场 `dprk_01/08`
- **出图迭代工作流（2026-10-01 学自用户实操流程，ChatGPT 出图通用）**：
  1. **首版四段式**：参考图分工（图一/二/三限定要素形制、图四只借布局氛围）＋ 3-4 句硬约束（朝向/件数/无人）＋ 素材库（明示"自行取舍添加"）＋ 收尾约束；**不要**五段长束缚提词（实测出图更差）
  2. **修正轮只发增量短句**：针对成图具体问题一两句（"光线不要太暗""机车太窄了，应该宽一点""站牌自然点"），不重发全提词——会话有上下文
  3. **中途插新参考图要加锁定声明**："站台布局进一步参考这张图（机车、敞车、站牌的要素不变）"——防改 A 坏 B
  4. **参考图分域授权**："站牌左上角的图标不要按我的参考图，保留你现在的"——一张图只借指定部分
  5. **文字/一致性要求挂限定行**："确保文字清晰""两个敞车涂装一模一样""铭牌与图二完全一致"——写进素材库会被忽略
  6. **正向表述优先**（"站牌自然点"＞"不要僵硬"）；物理细节点名到部位（"车顶喇叭在偏左侧""站牌靠近建筑、不在站台中间"）
- **尺寸**：1024×1536，PNG透明底，半身至膝（与suiyue一致）
- **风格**：二次元高清动漫风格，暖色调，与suiyue保持统一画风
- **表情集**：全部角色统一 19 种表情：`normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink` + 扩展 `cry, calm, shy`（以主角已完成16表情为基准，扩展3种见下；以下各节同）
- **扩展表情（2026-09-25 注册）**：`cry`（落泪/含泪——现有剧情已合理调用：prologue_06「爷爷...您看到了吗？」、prologue_04「陈爷爷，我回来了。我答应过您的。」）· `calm`（平静抿嘴——日常平静台词可用，现有剧情暂未指派）· `shy`（托腮脸红——害羞/难为情台词可用，现有剧情暂未指派）。**未来剧情占位**：哭戏/含泪句用 `e:"cry"`，平静日常句用 `e:"calm"`，脸红害羞句用 `e:"shy"`
- **人数约束（强制）**：主图必须是**单个人物**立绘——每条主图提示词首行已含 `SINGLE CHARACTER, ONE PERSON ONLY, no other people, no duplicates, no character sheet`。若生成工具仍输出参考图集/多人场景，在负面提示词外加 `multiple people, character sheet, reference sheet, turnarounds, duplicated character, multi-figure`。
- **差分工作流**：主图验收通过后，用"16表情差分图谱"提示词（见 1.9）一次产出 4×4 网格，再按需裁剪；勿逐表情单张生成。
- **表情入库进度（2026-09-25）**：林彪悍 16/16（像素仅剩 happy/wink 待补；angry/excited/shocked 已于 09-25 换新）+ 扩展 cry/calm/shy 已注册 · 老陈 16/16 · 嘉颖徐 **13/16**（缺 worried/shocked/shout/wink，剧本已引用的 smug/excited 已齐）· 岁月 16/16（`characters/suiyue/` 子目录 19 文件含 v2/no_pad 变体）

---

### 1.1 林彪悍（主角）

**文件**：`Resources/characters/lin_biaohan_*.png`（flat格式，CharacterSpriteManager兼容flat+子目录）  
**状态**：✅ 基本完成（14/16 高清 1024×1536，仅 happy/wink 仍为旧像素画待补；angry/excited/shocked 已换新 2026-09-25；另注册扩展 cry/calm/shy 三张）

**主图提示词（高清二次元版）**：
```
1024x1536, high resolution anime style, SINGLE CHARACTER, ONE PERSON ONLY, 
upper body to knees portrait, isolated on transparent background, 
no other people, no duplicates, no character sheet,
warm color palette, detailed shading,

CHARACTER: Lin Biaohan (林彪悍), 25-year-old male university student.

FACE:
- Slightly round oval face, soft jawline
- Large round deep brown eyes, determined but youthful
- Fluffy black short hair with natural slight curl, slightly messy
- Warm pale skin tone, light blush on cheeks
- Expressive eyebrows, thick but not heavy

CLOTHING:
- Dark navy blue work jacket (inherited from grandfather), slightly oversized
- White crew-neck T-shirt underneath
- Copper pocket watch chain visible at collar
- Dark blue jeans, slightly worn at knees
- Small leather backpack strap visible on one shoulder

ACCESSORIES:
- Copper pocket watch chain (distinctive character detail)
- Old leather backpack (partially visible)

COLOR PALETTE:
- Primary: navy blue （海军蓝）, warm white （暖白色）
- Secondary: copper （铜色）, denim blue （牛仔蓝）
- Accents: warm skin （暖肤色）, dark hair （深蓝黑色）

MOOD: Young, determined, slightly nervous but hopeful. A student about to embark on an impossible mission.

AVOID:
- Anime "bishonen" overly polished look (should look like a real student)
- Military or uniform appearance
- Glasses or modern accessories
- Too muscular or tall build
```

**表情差分工作流**：
1. 先生成主图（normal表情）
2. 用主图作为参考，逐个生成16种表情差分
3. 保持服装、发型、配饰完全一致，只改变面部表情和肢体语言

---

### 1.2 老陈（陈守正）

**文件**：`Resources/characters/laochen_*.png`（flat格式）
**状态**：❌ 全部16张需重新绘制（当前303×313像素画，升级为1024×1536高清二次元）
**叙事定位**：68岁的末代站长，独自守护雾峰村铁路线四年。他的存在就是铁路精神的化身——不是英雄式的壮举，而是日复一日的坚守。每个表情都应该让玩家感受到：这个人把一辈子都给了这条线。

**主图提示词（高清二次元版）**：
```
1024x1536, high resolution anime style, SINGLE CHARACTER, ONE PERSON ONLY,
upper body to knees portrait, isolated on transparent background,
no other people, no duplicates, no character sheet,
warm color palette with earthy tones, detailed shading, soft lighting from the side,

CHARACTER: Lao Chen (老陈/陈守正), 68-year-old man, the last stationmaster who guarded a railway line alone for four years. His body carries decades of outdoor work — thin but sturdy, not frail. Weather-beaten but dignified.

FACE (CRITICAL — this face tells a story):
- Square face structure, not round — strong jawline softened by age
- Dark tanned skin weathered by decades of sun and wind, deep wrinkles especially around eyes and forehead
- Deep brown eyes that squint into warm slits when smiling — but in neutral state they carry a quiet sadness, like looking at something that used to be there
- Sparse gray-white eyebrows, neatly trimmed with an old comb
- Flat nose bridge with slightly red nose tip — frostbite from years standing on cold railway platforms at dawn
- Gray-white short hair, neatly combed back with pomade, but one unruly tuft behind left ear that never stays put — a small rebellion against order
- Thick lips with slightly downturned corners, but when they smile, warm laugh lines appear that reach his eyes
- A small scar on his left cheek — from a railway accident years ago, he never mentions it

CLOTHING (every detail tells a story):
- White long-sleeve cotton shirt, collar slightly frayed from washing
- Second button re-sewn with mismatched red thread — his wife sewed it before she passed, he never replaced it
- Dark gray V-neck wool vest, slightly pilled at chest where he leans against things, darning marks at hem
- Reading glasses hanging on old leather cord around neck — always there, sometimes pushed up on forehead
- Dark loose trousers, ironed with sharp creases even though they're old — he respects the uniform of a stationmaster
- Black cloth shoes, soles worn thin from walking the 23km line

ACCESSORIES:
- Old leather cord with round reading glasses (signature element, always visible)
- A faded railway stationmaster badge pinned inside his vest (hidden, only visible when vest is open)

COLOR PALETTE:
- Primary: warm gray （暖灰） for skin, off-white （米白） for shirt
- Secondary: dark gray （深灰） for vest, worn brown （旧棕） for leather
- Accents: faded red thread （柔红） for the button, copper （铜色） for glasses
- Background influence: warm earth tones suggesting sunset light on old wood

MOOD: Warm, experienced, slightly weary but deeply kind. A man who has guarded the railway for four years alone, waiting for someone to come. His eyes carry the weight of memories — of trains that used to run, of a stationmaster (林悍) who was his best friend, of a line that refuses to die. When he smiles, it's because he's talking about trains or remembering the old days. The sadness never fully leaves his eyes, but neither does the hope.

AVOID:
- Too clean or polished — this man has been outdoors for four years, his clothes show it
- Cold or stern expression — he is the warmest character in the game
- Modern or fashionable clothing — everything is from the 2060s or older
- Overly muscular — thin but wiry, the build of someone who walks 23km regularly
- Generic "old man" face — his wrinkles are specific: railway platform cold, sun exposure, laughing lines from years of greeting passengers who no longer come
```

**16表情叙事驱动提示词**（每个表情绑定剧情瞬间）：

| 表情 | 剧情瞬间 | 差分提示词追加描述 |
|------|---------|-------------------|
| normal | 日常站务，巡视线路 | 默认站姿，目光平和望向远方铁轨，嘴角微平，像在听远处的汽笛 |
| smile | 首班车发车，看着林彪悍 | 眉毛舒展，眼睛弯成缝，嘴角上扬露出被烟熏黄的牙齿，整张脸亮了——像看到了等了四年的人终于来了 |
| sad | 回忆林悍站长 | 眼眶微红，嘴角下垂更深，一只手无意识地摸着胸口的旧 badge，目光失焦看向地面 |
| surprise | 林彪悍说出"我回来了" | 眼睛睁大，嘴巴微张，身体微微后倾——震惊中带着不敢相信，像做梦一样 |
| serious | 讨论铁龙计划威胁 | 眉头紧锁，目光锐利，嘴唇抿紧，整个人从温和变得坚硬——这是守了四年的老兵 |
| curious | 听林彪悍讲解新计划 | 头微微歪向一侧，眼睛眯起打量，手托下巴，像在评估一个老朋友的孙子是否靠谱 |
| excited | 机车重新启动 | 双眼放光，身体前倾，双手不自觉握拳——68岁的老人像个孩子一样激动 |
| worried | 线路安全问题 | 眉头皱起，嘴角向下，双手交叉在胸前，目光扫向铁轨的某个方向——他知道哪里有问题 |
| angry | 铁龙基金会再次上门 | 眉头深锁，嘴唇紧抿成一条线，眼神冰冷——这是他唯一一次展现攻击性 |
| bored | 等待发车的空闲时间 | 坐在长椅上，眼镜推到额头，打了个哈欠，一只手撑着脸——日常的无聊但不焦躁 |
| gentle | 对村民说话 | 声音柔软（通过面部表现），眼睛温柔地眯起，嘴角带笑，整个人像一团温暖的旧棉絮 |
| happy | 看着王小弟成长 | 眉毛挑起，笑容比smile更深更放松，眼角的皱纹都带着满足——像看到了传承的希望 |
| shocked | 听到沙鲸计划消息 | 瞪大眼睛，身体僵住，手指不自觉抓住椅扶手——不是害怕，是意识到对手比想象中更强 |
| shout | 在发布会上为铁路辩护 | 嘴巴大张，眉头紧锁，身体前倾——这是他第一次在公众面前大声说话，声音沙哑但有力 |
| smug | 想起年轻时的得意 | 嘴角一边翘起，眉毛挑高，眼神带着"我跟你说"的得意——像个想讲老故事的爷爷 |
| wink | 和林彪悍的秘密约定 | 眨一只眼，嘴角歪笑，像在说"这事只有我们知道"——老顽童的一面 |

---

### 1.3 张工（张德厚）

**文件**：`Resources/characters/zhanggong/{表情}.png`  
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词（高清二次元版）**：
```
1024x1536, high resolution anime style, SINGLE CHARACTER, ONE PERSON ONLY,
upper body to knees portrait, isolated on transparent background,
no other people, no duplicates, no character sheet,
warm color palette, detailed shading, soft side lighting,

CHARACTER: Zhang Degou (张工/张德厚), 62-year-old retired mechanical engineer, called back by Lao Chen to help save the railway. He repaired locomotives for 28 years — his optimism comes from the belief that the railway can still live. His hands carry old calluses, and there is always oil under his fingernails.

FACE (CRITICAL — 28年机车维修师的手和脸):
- Round, full face with rosy cheeks weathered by engine room heat
- Small bright eyes that squint into happy slits when smiling — genuine joy, not forced
- Sparse irregular gray-white eyebrows, a few extra long ones sticking out wildly
- Round red nose tip (rosacea from decades of hot/cold engine rooms)
- Gray-white sparse hair, slightly bald on top, fluffy on sides, never combed — always has a tuft sticking up
- Yellowish skin tone with a 5mm black oil stain on right cheek (he never notices it)
- Missing left front tooth (knocked out fixing a machine 20 years ago) — shows when he grins wide
- Deep laugh lines — this man laughs more than he frowns

CLOTHING (every detail tells the story of 28 years at the workshop):
- Brown plaid long-sleeve shirt, sleeves rolled to elbows — muscle memory from rolling them every morning for 28 years
- Three pens (red, blue, black) and a small screwdriver in left chest pocket — always ready to sketch a fix or tighten a bolt
- Dark brown corduroy vest, zipper broken, held with a safety pin — he'll fix the vest "tomorrow" but never does
- Pockets bulging with small parts, screws, electrical tape — a walking toolbox
- Reading glasses perched on top of head, temple wrapped in tape — lost the real case years ago
- Dark loose trousers with tool marks and faint grease stains
- Black cloth shoes, soles slightly worn from standing on concrete workshop floors

HAND DETAILS:
- 1cm old burn scar on left thumb web — from a welding accident, he wears it like a badge
- Hands with visible calluses, fingernails permanently stained with oil
- Fingers slightly thickened from decades of wrench work

COLOR PALETTE:
- Primary: warm brown （马鞍棕）, plaid red-brown （锈橙）
- Secondary: corduroy brown （灯芯绒棕）, faded denim blue
- Accents: bright red pen （红色）, blue pen, silver screwdriver （银色）

MOOD: Optimistic, slightly messy but brilliant. A man who spent 28 years keeping locomotives alive with duct tape and genius. His laughter fills the workshop. He's back because Lao Chen called, and he believes — truly believes — the railway can still run. The kind of mechanic who can fix anything but can't find his own glasses.

AVOID:
- Clean or professional mechanic appearance — his hands are always dirty, his clothes always have a grease spot
- Serious or gloomy expression — this is the most optimistic character in the game
- Impossibly neat workshop look — he thrives in organized chaos
- Modern or fashionable clothing — everything is work-worn and functional
- Generic "old man" look — his energy and grin make him feel younger than 62
- Missing the oil stain on his cheek — it's his signature
```

**叙事定位**：修了28年机车的老师傅，退休后被老陈叫回来帮忙。乐观是因为他相信铁路还能活。手上有老茧，指甲缝里永远有油渍。他的笑声是整个团队的精神支柱。

---

### 1.4 李阿姨（李桂芳）

**文件**：`Resources/characters/liayi/{表情}.png`  
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词（高清二次元版）**：
```
1024x1536, high resolution anime style, SINGLE CHARACTER, ONE PERSON ONLY,
upper body to knees portrait, isolated on transparent background,
no other people, no duplicates, no character sheet,
warm color palette, detailed shading, soft lighting,

CHARACTER: Li Guifang (李阿姨/李桂芳), 55-year-old community heart and soul of Wufeng Village Station. She handles money, manages affairs, and takes care of people. Her laughter is the warmest sound at the station. There are always snacks in her apron pocket.

FACE (CRITICAL — 这张脸要让人想叫一声"李阿姨"):
- Round face with a slight double chin — the face of someone who feeds everyone before herself
- Small but lively eyes that crescent into warm moons when smiling — eyes that have seen every villager's joy and sorrow
- Thin arched eyebrows (tattooed decades ago, now faded to a ghostly gray-blue)
- Small round nose tip, slightly pink from years over a hot stove
- Thin lips perpetually upturned at the corners — she's always about to say something
- Ear-length permed hair, dyed brown with 2cm white roots showing — she'll get to the salon "next week"
- Yellowish-white skin tone with warm undertones, fine wrinkles on neck from years of cooking over open flames

CLOTHING (every layer says "I take care of this place"):
- Pink floral long-sleeve shirt, freshly ironed — she irons everything, even work clothes
- Deep blue cotton apron, old phone in the front pocket — the apron is her command center
- Butterfly bow tied at the lower back (decorative) — a small vanity, she still wants to look nice
- Dark long pants, slightly worn at knees from kneeling to pick vegetables and scrub floors
- Black cloth shoes, comfortable for standing all day
- Dark brown Buddhist bead bracelet on left wrist, 12mm beads — she prays for the village
- Gold ring on right hand — a gift from her late husband, never takes it off

APRON POCKET DETAIL (signature character element):
- The apron pocket always contains snacks — hard candies, dried fruit, small cookies — for anyone who looks hungry

COLOR PALETTE:
- Primary: pink （粉色）, floral print
- Secondary: deep blue apron （藏蓝）, dark pants （暗灰绿）
- Accents: gold ring （丹青金）, brown beads （马鞍棕）

MOOD: Warm, talkative, the village's information hub and emotional anchor. She knows everyone's business and means well. Her laughter echoes through the station like a bell. She feeds people before they can say they're hungry, and she'll tell you exactly what she thinks whether you asked or not. The station would fall apart without her — not the rails, but the people.

AVOID:
- Too young or fashionable appearance — she's 55 and comfortable with it
- Quiet or reserved expression — this woman has never been quiet in her life
- Missing the apron (essential character marker — it's her uniform)
- Missing the apron pocket snacks (signature detail)
- Cold or distant expression — she radiates warmth like a kitchen stove
- Missing the gold ring or Buddhist beads (personal story elements)
```

**叙事定位**：社区里的热心肠，管钱管事管人。她的笑声是雾峰村车站最温暖的声音。围裙口袋里永远有零食。她是团队的粘合剂——当大家吵架时，她一碗热汤就能把所有人拉回桌前。

---

### 1.5 王小弟（王晨阳）

**文件**：`Resources/characters/wangxiaodi/{表情}.png`  
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词（高清二次元版）**：
```
1024x1536, high resolution anime style, SINGLE CHARACTER, ONE PERSON ONLY,
upper body to knees portrait, isolated on transparent background,
no other people, no duplicates, no character sheet,
warm color palette, detailed shading, soft lighting,

CHARACTER: Wang Chenyang (王小弟/王晨阳), 22-year-old fresh university graduate. He follows Lao Chen to learn the railway, like接过了一根接力棒. His eyes carry light — the kind that hasn't been beaten out of him yet.

FACE (CRITICAL — 这张脸要让人想起自己刚毕业时的样子):
- Oval face still carrying baby fat — he looks younger than 22
- Large bright eyes, clear black and white — eyes with light in them, the kind that believe the world can be changed
- Thick natural eyebrows, slightly messy but handsome — expressive, they jump when he's excited
- Small upturned nose, youthful and slightly sunburned from outdoor work
- Thick lips with an easy, unguarded smile showing straight white teeth — he smiles before he thinks
- Black short hair, 4cm fluffy, morning hand-styled look — he spends 30 seconds on hair and calls it done
- Fringe parted to reveal full forehead — open and honest
- Pale clean skin with a 3mm silver earring in left ear — a small rebellion against "be proper"
- Two or three light acne scars on chin — he's 22, it shows

CLOTHING (every item says "broke college grad with dreams"):
- Light gray hoodie (discount brand), uneven drawstrings — the kind you buy at a street market
- White t-shirt underneath, collar reads "Transportation Engineering 2026" — he's proud of his degree
- Black over-ear headphones around neck (installment payment "professional equipment") — his most prized possession
- Light blue jeans, 2cm white wear marks at both knees — he kneels a lot to inspect tracks
- White sneakers, slightly dirty toe — from walking the railway line with Lao Chen

COLOR PALETTE:
- Primary: light gray （晨雾灰）, light blue （天蓝色）
- Secondary: white （纯白色）, denim blue （钢蓝色）
- Accents: silver earring （银色）, black headphones （深灰黑）

MOOD: Energetic, eager to prove himself, slightly clumsy but sincere. The fresh graduate who still believes he can change the world. When he looks at the old railway, he doesn't see ruins — he sees possibility. He's in over his head but he won't quit. There is light in his eyes that the older characters have lost and are glad to see again.

AVOID:
- Too mature or serious appearance — he's 22 and acts like it
- Expensive or fashionable clothing — he's a broke grad, everything is budget
- Clean, unworn sneakers — they should show use from walking tracks
- Confident or experienced posture — he's still figuring things out
- Generic "anime protagonist" look — he should feel like a real engineering student
- Missing the headphones or silver earring (signature details)
```

**叙事定位**：刚毕业的大学生，热血但没经验。他跟着老陈学铁路，像是接过了一根接力棒。眼睛里有光。他是整个团队的未来——也是玩家在游戏中的情感投射。他的成长弧线就是铁路复兴的缩影。

---

### 1.6 赵师傅（赵铁山）

**文件**：`Resources/characters/zhaoshifu/{表情}.png`  
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词（高清二次元版）**：
```
1024x1536, high resolution anime style, SINGLE CHARACTER, ONE PERSON ONLY,
upper body to knees portrait, isolated on transparent background,
no other people, no duplicates, no character sheet,
warm color palette with earthy tones, detailed shading, soft side lighting,

CHARACTER: Zhao Deshan (赵师傅/赵德山), 55-year-old retired railway track maintenance engineer. A man of few words whose every word counts. He has walked 20 years of railway tracks — every sleeper plank knows him. Silent, steady, immovable like the rails themselves.

FACE (CRITICAL — 这张脸像铁路一样沉默但可靠):
- Slightly long, angular face with sharp features — a face carved by wind and distance
- Deep brown eyes, serious but not sharp — when relaxed, there is a quiet gentleness, like a still pond
- Average thickness eyebrows, natural shape, slightly furrowed from years of squinting into sunlight on tracks
- Average nose bridge, straight and unremarkable — like the man himself, functional
- Thin lips with straight corners — looks serious when not smiling, but when he does smile, it means everything
- Black short hair, 1.5cm, neat and tidy with clean sideburns — military precision, even in retirement
- Dark outdoor work complexion — sun-darkened, wind-worn, the face of a man who has spent decades outdoors
- 2cm old scar on left cheek (from military training) — he never talks about it

CLOTHING (every item is functional, nothing is decorative):
- Olive green utility jacket, zipped to chest — military surplus, still the best jacket he owns
- Zipper pull wrapped in black electrical tape (original broken) — he fixes what breaks, doesn't replace
- 1cm hole on left sleeve, unpatched — he noticed it, decided it doesn't affect function
- Dark gray high-neck knit sweater, form-fitting — warm and practical
- Left wrist vintage military mechanical watch, old leather strap, 3mm scratch on face — timekeeping is sacred to a track maintenance man
- Dark utility pants, canvas belt with brass buckle — sturdy, no-nonsense
- Black combat boots, scuffed toe — from walking every kilometer of the line

COLOR PALETTE:
- Primary: olive green （橄榄绿）, dark gray
- Secondary: black （深灰黑）, brass buckle （黄铜色）
- Accents: silver watch face （银色）, black electrical tape （深灰黑）

MOOD: Stern on the outside, warm on the inside. A man of few words whose actions speak louder. He has walked every kilometer of the 23km line, knows where the rails groan in winter, where the ballast shifts after rain. When he speaks, the others listen — not because he's loud, but because he's always right. His silence isn't coldness; it's the confidence of someone who has done the work.

AVOID:
- Soft or friendly first impression — he should look stern and reserved at first glance
- Missing the scar on left cheek (essential character detail)
- Clean, unworn combat boots — these boots have walked 20 years of track
- Talkative or expressive posture — he is reserved and still
- Modern or fashionable clothing — everything is military surplus or functional workwear
- Generic "tough guy" look — he's not aggressive, he's quietly dependable
```

**叙事定位**：沉默寡言的线路维护员。走了20年铁轨，每根枕木都认识他。说话少但每句都算数。他是团队的定海神针——当所有人都慌了，赵师傅还在检查铁轨。他的沉默不是冷漠，是20年巡线养成的习惯。

---

### 1.7 小芳

**文件**：`Resources/characters/xiaofang/{表情}.png`  
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词（高清二次元版）**：
```
1024x1536, high resolution anime style, SINGLE CHARACTER, ONE PERSON ONLY,
upper body to knees portrait, isolated on transparent background,
no other people, no duplicates, no character sheet,
warm color palette, detailed shading, soft lighting,

CHARACTER: Xiao Fang (小芳), 45-year-old volunteer dispatcher at Wufeng Village Station. The second youngest energy source on the team (after Wang Xiaodi). Enthusiastic, always ready, the person who keeps the whole operation running on time.

FACE (CRITICAL — 这张脸要让人觉得"有她在就放心了"):
- Round face with a natural warmth — the face of someone who genuinely enjoys helping
- Bright, alert eyes that are always scanning, always aware — a dispatcher's eyes
- Short neat hair, practical and easy to maintain — she doesn't waste time on vanity
- Warm, approachable expression with a hint of determination — she's not just nice, she's capable
- Subtle laugh lines around eyes — she smiles often and means it
- Light makeup, barely noticeable — she cares about presentation but not extravagance

CLOTHING (functional volunteer uniform with personal touches):
- Simple light-colored work shirt, collar neat — she takes pride in looking presentable
- Bright orange/yellow volunteer vest with reflective strips — she wears it like a badge of honor
- Dark long pants, practical and well-maintained
- Flat cloth shoes, comfortable for being on her feet all day
- Small radio/walkie-talkie clipped to vest shoulder — a dispatcher is always connected
- Pen tucked behind ear — she's always taking notes or filling forms

COLOR PALETTE:
- Primary: warm orange/yellow vest （深橙色）, warm skin tones
- Secondary: light shirt （浅米色）, dark practical pants （暗灰绿）
- Accents: reflective silver strips （银色）, pen cap blue

MOOD: Enthusiastic, competent, always showing up. She's the volunteer who arrived first and leaves last. Her energy is infectious — when she's excited about a project, everyone catches the bug. She runs the dispatch schedule with a clipboard and sheer willpower. Not the most experienced, but the most committed.

AVOID:
- Too polished or professional appearance — she's a volunteer, not a corporate executive
- Cold or distant expression — warmth is her defining trait
- Missing the volunteer vest (essential character marker)
- Missing the walkie-talkie or pen (dispatcher tools)
- Lazy or passive posture — she's always moving, always doing
- Generic "nice lady" look — she should feel capable and energetic, not just pleasant
```

**叙事定位**：志愿者，负责调度。热情洋溢，是团队里最年轻（除王小弟外）的活力来源。她用一个 clipboard 和满腔热情撑起了整个车站的调度系统。有她在，火车就不会误点——至少在她的管辖范围内。

---

### 1.7.1 导师（daoshi）— 待绘制占位

**文件**：`Resources/characters/daoshi_{表情}.png`（flat，与林彪悍同格式）  
**状态**：❌ 待生成（游戏内现为占位色块「立绘待绘制」）  
**角色定位**：金日成综合大学智能调度方向教授，林彪悍学业导师。中年学者，温和但有分寸；知晓老陈与雾峰村线，批准休学并安排0721交接。停机坪背景里的中年男人是**车队管理员**（非导师），剧情旁白已区分。

**主图提示词（高清二次元版）**：
```
1024x1536, high resolution anime style, SINGLE CHARACTER, ONE PERSON ONLY,
upper body to knees portrait, isolated on transparent background,
no other people, no duplicates, no character sheet,
warm color palette, detailed shading, soft lighting,

CHARACTER: University professor / mentor of Lin Biaohan, mid-40s male academic.

FACE:
- Rectangular glasses, thoughtful eyes behind slightly reflective lenses
- Neat side-parted dark hair with early greying at temples
- Calm, measured expression — the face of someone who grades papers at midnight
- Light stubble shadow optional, clean-shaven preferred

CLOTHING:
- Charcoal grey suit jacket over soft blue dress shirt, no tie (academic casual)
- University faculty pin on lapel
- Leather-strap watch, reading glasses in breast pocket
- Dark trousers, polished but worn shoes

POSE: standing, one hand in pocket, other holding a thin folder or tablet — mid-conversation posture

MOOD: Warm but professional. He believes in the student's talent and is about to let him walk away from it.

NEGATIVE: no military uniform, no North Korean party pin on suit (civilian academic), no other people
```

**16表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（先出 normal 再差分）

---

### 1.8 岁月（AI 助手界面头像）

**文件**：`Resources/characters/suiyue/interface.png`  
**尺寸**：256×256，PNG透明底

**提示词**：
```
pixel art, 16-bit retro style, transparent background, STARDEW VALLEY STYLE, AI assistant avatar.

DESIGN ELEMENTS:
- 13-year-old anime girl AI assistant (参考图：黑长直发系红色蝴蝶结、琥珀色大眼)
- Black zip-up jacket + white sailor school uniform + red bow tie
- Black pleated skirt with red stripe, backpack, holding a tablet
- Innocent curious student expression, slightly surprised open mouth
- Golden/warm outline to fit warm pixel palette
- 256x256 pixels

AVOID:
- Robot/mechanical appearance (this is a humanoid hologram girl)
- Cold or threatening appearance
- Oversized adult proportions (keep 13yo petite body)
```

### 1.8.1 岁月专属表情清单（13岁少女 AI 差分）

**文件**：`Resources/characters/suiyue/{表情}.png`  
**说明**：岁月使用 §1.9 通用 16 表情集（normal/smile/sad/surprise/serious/curious/worried/excited/angry/bored/gentle/happy/shocked/shout/smug/wink），在此额外标注岁月专属的**表情使用场景**与**高频口癖表情**（自夸得意→smug 高频、机械精确→serious、好奇→curious、撒娇→gentle/wink、被拆台→angry/bored）。

| 表情 | 岁月专属用法 | 高频台词示例 |
|------|-------------|-------------|
| smug 得意 | **自夸高频**——"我可是高性能AI！"标配表情 | "我可是高性能 AI！"（经常重复、昂下巴、歪嘴笑） |
| curious 好奇 | 遇见新事物/新食物/新按钮 | "咦？这个是新的！" |
| excited 兴奋 | 讲到铁路历史/数据抢答 | "我的数据库里全都有哦！" |
| serious 坚定 | 关键时刻承诺/严肃数据 | "放心，我会把您安全送到！" |
| gentle 温柔 | 陪伴安慰时刻 | "……但我会陪着您的。" |
| wink 眨眼 | 撒娇/求表扬 | "夸夸我嘛——" |
| sad 悲伤 | 提到 23 年孤独 | "……二十三年了。终于有人和我说话了。" |
| angry 生气 | 被说是普通 AI 时 | "我才不是普通导航！" |
| bored 无聊 | 等主角磨蹭时 | "还没好吗——" |
| surprise 惊讶 | 发现秘密数据 | "咦？！" |

**经典口癖**（写入剧情与 TTS）：
- 「我可是高性能 AI！」——自夸口头禅，频繁重复，语气自豪骄傲
- 「我的数据库里全都有哦！」「这种小问题，我三秒就算出来了！」
- 炫耀时眼睛发亮、挺直腰板、歪头；被戳穿时鼓起腮帮、嘟囔

---

### 1.9 16表情差分图谱（4×4网格，铺满全画面）

**用途**：基于已验收的主图（母图），一次生成某角色的全部16表情差分（每格只画半身：头到腰部，不画全身）。

**文件**：`Resources/characters/{角色}/{表情}.png`（差分后按格逐张裁切并命名）

**提示词**：
```
基于参考图生成16表情差分图谱。
严格保持参考图中角色的所有视觉特征：
- 相同的像素画风格和分辨率
- 相同的发型造型（逐角色注明：黑发/白发/寸头等）
- 相同的服装（包括口袋、缝线、褶皱细节）
- 相同的面部比例和五官位置
- 相同的配色方案和像素调色板
- 相同的肩膀宽度/头身比例，双肩完整显示，肩膀以下到腰部可见，不可裁掉肩部
只改变面部表情，身体姿态、服装、像素风格完全不变。

===== 姿态要求 =====
- 16格一律保持与母图相同的自然站立姿态，动作放松自然
- 不改变双臂/双手位置，不做大幅动作或夸张手势
- 头部角度可轻微变化（配合表情），但躯干肩膀保持不动
- 避免僵硬的正面证件照感，保留母图的生活化松弛感

===== 布局 =====
4行×4列网格，共16个表情，占满整张画面，不留空白边距。
每个表情256×384像素，总尺寸1024×1536。
透明背景，无文字无标签，各格子之间紧密拼接无缝隙。

===== 色深 =====
- 32-bit 真彩色（每像素RGBA各8位），保留完整渐变与中间色
- 禁止降为索引色/减少色板/产生色带；禁用低色深复古压缩感
- 与母图色彩深度完全一致

===== 16个表情（每格只画半身：头到腰部，不画全身） =====
第1行：
1. normal 平静 — 自然放松，嘴角微平，眼神温和
2. smile 微笑 — 嘴角上扬，眼睛弯成月牙
3. sad 悲伤 — 眉毛下垂，嘴角向下，眼神黯淡
4. surprise 惊讶 — 眉毛高挑，嘴巴微张，眼睛睁大
第2行：
5. serious 坚定 — 眉头紧锁，嘴唇紧闭，目光锐利
6. curious 思考 — 单眉上扬，眼神向上看
7. worried 紧张 — 额头微皱，眼神不安
8. excited 兴奋 — 咧嘴大笑，眼睛放光
第3行：
9. angry 愤怒 — 眉头紧锁，咬牙切齿
10. bored 无聊 — 神情平淡，眼神放空，嘴角平直
11. gentle 温柔 — 眼神柔和，轻微微笑
12. happy 开心 — 大笑，眉眼弯弯，神情明亮
第4行：
13. shocked 震惊 — 眼睛瞪大，嘴巴大张，眉毛高耸
14. shout 大喊 — 嘴巴张开呈呐喊状，眉头紧蹙
15. smug 得意 — 单边挑眉，嘴角歪斜上扬
16. wink 眨眼 — 单眼闭合，嘴角俏皮上扬

===== 绝对禁止 =====
- 改变像素画风格
- 改变服装或发型
- 变成动漫/插画/写实风格
- 添加背景色
- 任何文字或标签
- 格子之间留白或位置歪斜偏移
- 只画零星几个格子（必须16格全部有内容）
- 裁掉肩膀或仅露头部
- 降色深/索引色/色带/颜色断层
- 夸张动作、换姿势、换手势
```

> 注：表情id与第1.2节统一16表情集完全一致；母图是全身参考，差分图是半身（头到腰部）的表情特写——与主角林彪悍现有拆分图裁切保持一致。生成后按格裁剪为16个单张，命名 `{角色}/{表情id}.png`。

---

## 二、场景背景

### 通用规范

- **尺寸**：1920×1080，JPG/PNG
- **风格**：STARDEW VALLEY STYLE pixel art，16-bit retro
- **格式**：多层分类清单 + 色彩方案 + 氛围 + 排除项
- **颗粒度（2026-09-28，复古低颗粒方案）**：源图统一按**高颗粒**生成（不变，不重 roll）；低颗粒复古感 = **后处理** `python tools/pixelate_bg.py --factor 3`（最近邻÷3再放大，像素块×3，输出 `Resources/bg_low/`；--factor 2 为轻微档）。游戏内切换：设置 → 显示 → **「复古像素（低颗粒）」开关**，打开时 BackgroundManager 改加载 `bg_low/` 同名图（C# 侧待实现：注意 `BackgroundManager.cs` 存在未提交改动，动手前先确认）。仅处理像素风 bg/CG，高清立绘不参与。
- **⚠️ 人物排除（强制）**：场景背景图**严禁出现任何人物或载具**。即使SCENE描述中提到"乘客""老人""摊贩""飞行器"等，也仅作为环境氛围的文字说明，实际生成时**绝对不画人、不画车**。每个背景prompt末尾必须包含：`NO CHARACTERS, NO PEOPLE, NO VEHICLES, NO AIRCRAFT — this is a pure environment/scene background, empty of all human presence and man-made vehicles. The only man-made elements are the railway line, buildings, and infrastructure.`

---

### 2.0 前置 · NF-5 外构参考图（nf5_exterior_ref）

**文件**：`ExternalAssets/nf5_exterior_ref.png`（参考用图，不进 Resources，游戏不加载）
**用途**：全部含 NF-5 场景/CG 的**外构锁定源**——先生成本图，后续 `station_sunset`、`depot_morning`、`platform_morning` 等一律挂它作 image reference，提词只写构图/光影/氛围。
**状态**：✅ 已收并定性（2026-09-28）→ `ExternalAssets/nf5_exterior_ref.png`（绿色版，1536×1024）——挂旗+标语**非画错**，＝「被朝鲜实控的中国铁路局」涂装（世界观 §十三）。**用法**：本图锁 **NF-5 原版外构**（耕牛/支线遗存/朝控存量挂旗车）；**NF-5K（上代主力）场景用 `ExternalAssets/nf5k_reference.png`（已锁）**；**新一代柴油机车组场景另需前置（提词待写 → 待锁清单）**。涂装按场景走——朝控场景直用，原局场景挂图时附加 `ignore the flag and propaganda, plain Chinese green livery` 指令，或按需另出纯净版；过渡期亦可用 `station_sunset` 旧图裁切。
**前置**：现实**东风4型（DF4）货运内燃机车**照片 1–2 张（正前 3/4 角 + 侧视各一张），生成时作为附图挂入。

**尺寸**：1536×1024（3:2，容纳 3/4 视角全车）

**附图（现实参考图）用法**：
1. 挂 1–2 张 DF4 照片作为 image reference / 参考图（Midjourney `--cref`/`--sref`、即梦/豆包参考图、Gemini 附图均可）
2. 必须附加指令（防止抄现实涂装）：
   `Use the photo ONLY as structural proportion reference (short hood, cab position, bogie spacing, headlight layout). Do NOT copy its livery, colors, or markings — livery is deep forest green as specified.`
3. **只在本前置阶段挂现实照片**；下游场景一律改挂本参考图，不再挂照片（避免写实摄影风格串入像素画风）。

**提示词（v1 · 中文版）**：
```
像素风，载具参考图，星露谷风格像素画，清晰像素，16-bit复古，

NF-5「耕牛」内燃机车 —— 外构设计参考：

场景：单台内燃机车的干净外构参考图。观众左前 3/4 前斜视角，展示鼻端、一侧完整车身、车顶与车底走行部。孤立于纯深灰中性背景，仅底部一层柔和地面投影。无环境、无铁路风景、无天空、无车站、无前景道具。

机车（锁定设计 —— 所有后续场景必须与此一致）：
- 短罩走廊式内燃机车（road-switcher），1965 年代中国设计、东风4型比例 —— 不是现代高铁，不是蒸汽机车
- 深森林绿车体，风化锈橙色补丁与污渍流挂
- 鼻端纯绿：无人字纹、无 V 形条纹、无任何白色斜纹
- 鼻端：绿底白字「NF-5」小牌，其下方黄铜铭牌「耕牛 / 경우」（中文在上、韩文在下，样式同站牌雾峰/무봉 的双语规则）
- 一盏居中大圆灯 + 两盏小标志灯
- 红色缓冲梁，下方板条式排障器（牛栏）；微微上翘的牛角形车钩
- 驾驶室位于短罩之后；两侧走台板与扶手；车顶可见排气筒与散热风扇
- 两台三轴转向架（Co-Co 轴式），风化钢轮

车体文字（仅此两处，不得出现其他字形）：
- 鼻端牌：NF-5（绿底白字）
- 黄铜铭牌：耕牛 / 경우（中文+韩文双语）

无人物、无人影、无角色、无车站、无风景背景 —— 仅机车，孤立参考视角。
```

**生成后锁定动作**：
1. 选型（比例正确、涂装贴 LOCKED DESIGN、文字可读）→ 存 `ExternalAssets/nf5_exterior_ref.png`
2. 更新本节状态为 ✅ 已锁定，并把「一、通用规范 · 绘图前置」的 NF-5 锁定源从"旧图裁切"改为本图
3. 下游提词（2.17 / 2.24 / 2.29）的挂图路径统一改为本图
4. 若 `NF-5`/`耕牛` 文字渲染不准：优先保证比例与涂装，字体问题后期修图或重 roll，不因文字放弃比例正确的候选

---

### 2.0.1 C-55000 敞车外构（c55000_exterior_ref · 前置）

**文件**：`ExternalAssets/c55000_exterior_ref.png`（参考用图，不进 Resources）
**用途**：所有货运场景的**敞车锁定源**——`station_sunset` 车后两节煤车、矿区线编组等；生成后挂 image reference。
**状态**：✅ 已收并锁定（2026-09-30，提词 v2 实测"效果很好"）→ `ExternalAssets/c55000_exterior_ref.png`（透明底，日式铁路涂装 + 朝鲜国旗 + 两端挡板）
**现实原型**：中国铁路 **C70 型通用敞车**（载重 70 吨，全钢敞车；重载系列 C70E，前代 C64）——结构参考亦借用日本国铁 DD51 实车照（见 v2 附图）。
**游戏内名称**：**「C-55000 型运煤敞车」**（型号即名——货车不走机车的"牛"线、客车的鸟线绰号；2026-09-28 由 C-70 改号）。

**提示词（v2 · 用户实测版，2026-09-30 "效果很好"）**：
```
参考图一，透明背景，像素风，日本风格铁路涂装，星露谷风格像素画，C-55000 型运煤敞车。
无环境、无铁路风景、无天空、无站场、无相邻车辆，无中国铁道标识。
车辆：C-55000 型通用敞车形制，仅单节敞车，孤立参考视角。
加上朝鲜国旗和朝鲜元素（一个朝鲜国旗）。
像素画，透明背景，涂装是日本铁路风格；注意两边都有挡板，严格参考原图。
```
> **附图（v2 实测用）**：`中转站/Japanese-national-railways-DD51-842-20140518.jpg`（日本国铁 DD51 实车照——"严格参考原图"即指它）；成图＝透明背景 + 日式铁路涂装 + 朝鲜国旗 + 两端挡板。

**附图（现实参考图 · v1 初版方案）用法**：挂 1–2 张 C70 真车照片（3/4 前侧 + 侧视），附加：
`Use the photo ONLY as structural proportion reference (side ribs, bogie spacing, brake wheel). Do NOT copy its livery — livery is weathered brown-red as specified.`

**尺寸**：1536×1024（3:2）

**提示词（v1 · 中文版）**：
```
像素风，载具参考图，星露谷风格像素画，清晰像素，16-bit复古，

C-55000 型运煤敞车 —— 外构设计参考：

场景：单节敞口运煤货车的干净外构参考图。观众左前 3/4 前侧视角，展示端墙、一侧完整侧墙、敞口内部底板与走行部。孤立于纯深灰中性背景，仅底部一层柔和地面投影。无环境、无铁路风景、无天空、无站场、无相邻车辆。

车辆（锁定设计——所有货运场景必须与此一致）：
- C-55000 型通用敞车形制（同 C70 级）：全钢敞车，直立侧墙带竖向压筋与上沿横带，敞口无车顶
- 车内为空敞底板（可少量残留煤渣），可见端墙内侧与手闸立轮
- 两台两轴转向架，钢轮，侧架与弹簧可见；车端有车钩与缓冲装置
- 涂装：风化棕红色防锈漆，锈痕、磕碰掉漆、煤尘熏黑（车体下部更重）
- 一端端墙上装手闸轮；侧墙下缘有卸货底开门缝线

车辆文字（仅此一处，不得有其他字形）：
- 侧墙中部钢印车号：C55000 加一串数字（白色或浅黄，做旧）

无人物、无人、无角色、无环境背景、无棚车、无罐车 —— 仅单节敞车，孤立参考视角。
```

**生成后锁定动作**：状态改 ✅ → 更新待锁清单 → `station_sunset` 等货运场景挂 `ExternalAssets/c55000_exterior_ref.png`。

---

### 2.14 群山天空（mountain_sky）

**文件**：`Resources/bg/mountain_sky.png`
**用途**：序章 Day 4 下午——0721飞越边境后，进入中国境内群山之上。林彪悍第一次从高空俯瞰"小时候听爷爷讲的那片山"。
**状态**：✅ 已入库（2026-09-22 验收）——隧道「临江工무단」、层叠山脊、废弃站房均达标；源图已归档 `中转站/归档/`

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
群山天空 / 산하늘, afternoon aerial, warm-to-cool gradient light:

SCENE: 连绵不绝的长白山余脉从脚下延伸到天际线，层层叠叠的山脊像翻开的书页越远越淡。远处一条铁路线在山谷底部蜿蜒——锈迹斑斑的铁轨在午后阳光下偶尔闪过一丝铜色光泽。铁轨两侧是被森林吞噬的废弃站房，屋顶的瓦片已经塌了一半，墙角长满了白桦树苗。一个小型隧道口在最近的山谷中露出黑洞洞的入口，隧道上方依稀可辨水泥刻字：「临江工务段」（임강공무단）。山坡上有零星的梯田痕迹，早已荒废，灌木丛生。午后阳光从右侧斜射，把山脊的阴影投向左侧，形成明暗交替的层叠效果。远处天际线处，山色逐渐从深绿过渡到蓝灰再到淡紫，大气透视效果明显。纯风景，无人无车，只有山和铁轨。

LAYOUT:
- 下三分之二：层叠山脊（前景深绿 深森林绿，中景黄绿 黄绿色，远景蓝灰 蓝灰色，最远淡紫 淡紫色）
- 山谷底部：蜿蜒铁路线（锈棕色 锈棕色，偶尔有桥墩阴影）
- 一个小隧道口（黑色半圆，上方水泥刻字「临江工务段」模糊可辨）
- 上三分之一：晴朗午后天空（晴朗天蓝），散落积云（白色，底部浅蓝灰 浅蓝灰）

COLORS:
- 山脊色阶：深森林绿（深森林绿） → 黄绿（黄绿色） → 蓝灰（蓝灰色） → 淡紫（淡紫色） → 天际淡蓝白（天际淡蓝白）
- 铁轨：锈棕灰（锈棕色），桥墩段偏锈红（锈红）
- 隧道口：深灰黑（深灰黑），上方水泥灰（水泥灰）
- 天空：晴朗蓝（天蓝色），积云白（纯白色），云底蓝灰（浅蓝灰）
- 废弃站房：灰瓦（灰瓦色），塌陷处暗影（暗影色）
- 梯田荒地：枯黄（枯黄色）与灌木绿（灌木绿）交织

MOOD: 归途。辽阔、安静、带着希望的忧伤。层叠的山脊在午后阳光下安静排列，铁路线在山谷中细得像一根线，锈棕色在深绿山体中格外醒目。远处山色渐淡至淡紫，整体氛围安静而辽阔。

AVOID:
- 出现任何人物、角色、人类身影（NO CHARACTERS, NO PEOPLE, NO HUMAN FIGURES）
- 出现任何车辆、飞行器、载具（NO VEHICLES, NO AIRCRAFT, NO FLYING CARS — this is a pure landscape）
- 出现现代建筑或城市（这是原始山林）
- 铁路过于清晰或壮观（应该是"几乎看不见"的细线）
- 过于明亮的光线（午后应有温暖但不刺眼的质感）
- 缺少大气透视效果（远山必须渐淡）
- 冷色调为主（这是"进入中国"的第一幕，应偏暖）

NO CHARACTERS, NO PEOPLE, NO VEHICLES, NO AIRCRAFT — this is a pure environment/scene background, empty of all human presence and man-made vehicles. The only man-made element is the abandoned railway line and tunnel.
```

---

### 2.15 空中俯瞰·铁轨（aerial_view）

**文件**：`Resources/bg/aerial_view.png`
**用途**：序章 Day 4——空中俯瞰雾峰村铁路线全貌。
**状态**：✅ 已入库（2026-09-22 验收）——石拱桥/谷口村落炊烟/沿线小站/梯田均达标；轨道走向与提词镜像，剧情可用；源图已归档 `中转站/归档/`

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
aerial top-down view of a single-track mountain branch railway, late afternoon golden hour:

RAILWAY TRACK (visual spine):
- Single track, rust-brown rails with occasional silver polish marks on top surface
- Dark wooden sleepers (枕木), some cracked, some replaced with lighter new wood
- Grey gravel ballast (道砟) between tracks, patches of green weeds and yellow wildflowers growing through
- Track enters bottom frame at bottom-right, curves gently left, exits at top-left
- Track width from above: narrow, one track only
- Occasional short tangent sections where track runs straight, mostly gentle curves

STATIONS (3 along the line, roughly equal intervals):
- Each station: one grey-tiled building (about 20m×8m footprint), short concrete platform (10m long, 2m wide)
- Building walls: white-grey concrete, some with faded paint
- Platform edge: faded yellow safety line
- Station 1 (nearest, bottom third): platform on left side of track, building behind it
- Station 2 (middle third): platform on right side, small stone-arched culvert nearby
- Station 3 (top third): smallest of the three, building partially overgrown with vines

BRIDGE:
- Stone arch bridge (石拱桥) at center of frame, carrying track over a small stream
- Bridge width fits single track, grey stone construction
- Stream below: narrow, dark water, visible as a blue-grey ribbon under the arch
- Bridge abutments: moss-covered stone

VILLAGE (at valley mouth, bottom-left of frame):
- 8-12 small houses with grey tile roofs clustered together
- One taller chimney with light smoke rising straight up
- Dirt paths between houses, small vegetable plots
- Houses arranged loosely along a widening of the valley

SURROUNDING LANDSCAPE:
- Right side of track: winding dirt road running parallel, brown-red earth
- Both sides beyond road/stream: terraced fields (梯田) in gold-green, following hill contours
- Higher slopes: dense forest, deep green, fading to blue-grey with distance (atmospheric perspective)
- Far peaks: pale blue-purple silhouette layer, almost merging with sky
- Scattered individual trees in the fields, dark green canopies

LIGHTING:
- Late afternoon sun from upper-left, warm golden wash over everything
- Long shadows cast by buildings, trees, and bridge to the right
- Rails catch the light, creating thin silver-gold lines along the track
- Atmospheric haze in the valley, softening distant hills

COLORS:
- Rails: rust brown-grey with silver-gold highlights
- Sleepers: dark brown
- Ballast: grey with green weed patches
- Station buildings: grey-white walls, grey-blue tile roofs
- Bridge stone: grey with green moss
- Stream: dark blue-grey
- Fields: gold-green
- Forest: deep green fading to blue-grey
- Village roofs: grey
- Sky: pale warm blue with white clouds, golden haze near horizon

NO CHARACTERS, NO PEOPLE, NO VEHICLES — pure aerial landscape background.
```

---

### 2.16 雾峰村夕阳（village_sunset）

**文件**：`Resources/bg/village_sunset.png`
**用途**：序章 Day 4 抵达前——雾峰村全景，林彪悍的"老家"。村口的老槐树、土墙、炊烟，2076 年仍保持着铁路时代前的样子。
**状态**：✅ 已生成入库（2026-09-28 收图，1672×941）——门楼匾额繁体「霧峰村」**符合世界观**（正字运动·繁体复兴，见 `设定/世界观扩展设定.md` §十一），验收通过；尺寸略低于 1920×1080 规格（暂用，重绘时补足）
**文字规范（与站牌规范同源）**：村口标识只写村名「雾峰村 / 霧峰村」（**老村题字多为繁体**，依世界观 §十一 今文/古文并存规则），**禁止巨型墙面大字**；可有小水泥门牌/褪色春联，规模贴合自然村落

**提示词（v2）**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
雾峰村夕阳 / 무봉촌 석양, mountain village panorama at sunset, golden hour warmth:

SCENE: 山谷中的小村庄被夕阳染成金橙色——15-20栋房屋错落分布在谷底，灰色瓦片屋顶（东北传统瓦顶）在夕阳下泛着温暖的光泽。村口一棵巨大的老槐树，树冠覆盖了小半个晒场，树下有一条褪色的红砖凳。村庄入口处一块斑驳的小型水泥门柱/矮牌坊，仅嵌一块普通村名牌，上书「霧峰村」三个字（**繁体，古文派匾额制式**；正常门牌尺寸，非巨型标语墙；可有更小的무봉下方或旁侧无字），旁边贴着褪色的春联残片——NO giant painted wall characters, NO oversized slogans. 土墙围成的院落里晾晒着衣物；炊烟从至少五个烟囱同时升起，淡蓝灰色的烟丝在夕阳中被染成粉橙色——都是同一个方向，都是同一个晚饭时间。远处山坡上是梯田茶园，茶树丛呈深绿色的弧形条带层层叠叠。更远处，一条锈蚀的铁路线在山谷中隐约可见。夕阳从画面左侧斜射，把整个村庄笼罩在暖金色光线中，建筑物的阴影向右侧拉长。

LAYOUT:
- 中景：村庄横跨画面（15-20栋房屋聚集），老槐树在画面左前方（树冠占画面左侧1/4高度）
- 村口：小型水泥门柱+普通尺寸村名牌「霧峰村」（繁体，中景偏左，非巨型字），旁有褪色春联
- 远景山坡：梯田茶园（深绿弧形条带层叠上升）
- 最远背景：山谷中的铁路线（锈棕色细线）和一个微型站房（灰瓦顶）
- 天空：夕阳从左侧斜射，橙粉色云彩（橙粉色 → 深橙色），远处山脊剪影

TEXT ON SIGNS (exact only):
- Village entrance nameplate: 霧峰村 (traditional script — 古文派匾额; modest size, normal village sign)
- FORBIDDEN: giant hand-painted wall characters, oversized slogans, station-style 終點站 text

COLORS:
- 屋顶：暖灰，瓦片纹理在夕阳下偏金棕
- 土墙：白灰泛黄，斑驳处露出红砖底
- 槐树：深棕树干，树冠深绿偏金(金绿色，在夕阳下带金色高光)
- 梯田茶园：深苔绿，逆光处偏暗
- 炊烟：淡蓝灰，被夕阳染成粉橙
- 夕阳天空：橙 → 深蓝影
- 铁路线：锈棕（锈棕色），在远处山谷中几乎看不清
- 门柱/名牌：水泥灰（水泥灰），红字褪色(柔红色 → 褪色红)
- 春联残片：红纸褪色，金字模糊

MOOD: Mountain village at golden hour. Warm orange-pink sunset light, smoke rising from chimneys, quiet atmosphere. The railway line barely visible in the far valley, rust-brown contrasting with the village's warm gold.

NO CHARACTERS, NO PEOPLE, NO HUMAN FIGURES, NO VEHICLES, NO AIRCRAFT — pure environment/scene background.
```

---

### 2.16.1 雾峰站站牌样式（wufeng_signage_ref · 前置）

**文件**：`ExternalAssets/wufeng_signage_ref.png`（参考用图，不进 Resources）
**用途**：所有含雾峰站标识系统的场景锁定源——`station_sunset`、`platform_morning`、`platform_evening` 等；生成后挂 image reference，保证跨场景站牌制式一致。
**状态**：✅ 已生成并锁定（2026-10-01；v1 六件套陈列作废 → v2 双柱中国式作废 → v3 定形制 → v4 母图到位（`ExternalAssets/signage_gomen_source.png`）→ v5 线位关系定稿（两线交汇站）→ **v6 实操出图锁定**（用户精简提词；成图＝`ExternalAssets/wufeng_signage_ref.png`，透明底 1536×1024，含母图式两侧立柱已接受保留））
**规范依据**：§2.17 站名/文字规范（2026-09-22）+ 世界观 §十一 今文/古文并存——**站牌属官方标识一律简体**，中韩双语；禁終點站/始發/巨型墙字。左上角徽＝**岚川铁路局局徽**（定义见 `世界观扩展设定.md` §十三）。其余标识件（搪瓷门牌/时刻表/宣传栏/候车室/停車）由 station_sunset 主提词文本直接描述，不另出参考图。

**尺寸**：1536×1024（透明底，横牌居中，四周留边）
**挂图**：复现/改版时附母图 `ExternalAssets/signage_gomen_source.png`（JR 后免站实拍）作 image reference，只取形制，站名/文字全部替换为本提词内容。**下游站景（station_sunset 等）一律改挂成图 `ExternalAssets/wufeng_signage_ref.png` 锁制式。**

**提示词（v6 · 实操定稿＝用户精简版 ＋ 局徽定义，2026-10-01 出图）**：
```
参考图一，透明背景，单个物件，像素风，星露谷风格像素画，清晰像素，16-bit复古，
雾峰站 站名牌 —— 参照所附日本 JR 站名牌（駅名標）实拍母图的形制，改造为中韩双语：

① 左上角：岚川铁路局局徽——受朝鲜文化影响的翼星纹样（蓝色双翼+红星+轨道意象的抽象徽记，不写文字）
② 右上角：两枚白底圆形线路徽上下排列（样式同母图右上角）——
   上枚：紫红色圆环，圆内上行「雾」下行「1」，黑色字
   下枚：青绿色圆环，圆内上行「青」下行「1」，黑色字
③ 牌面上部居中：主站名「雾峰」（黑色粗体圆润黑体，占牌面最大面积）
④ 其下居中第二行：「무봉」（谚文，黑色，中号粗体）
⑤ 其下居中第三行：罗马字「Wufeng」（黑色，小号）
⑥ 中部蓝色横带（亮青蓝色，同母图）贯通牌面左右，带内文字全部白色：
   左端：白底圆徽（青绿色圆环，「青」上「2」下）+ 右侧站名「分水岭」（白色大字）其下「Fenshuiling」（白色小字）
   右端：对称布局，站名「茶山」（白色大字）其下「Chashan」（白色小字）+ 白底圆徽（紫红色圆环，「雾」上「2」下）
⑦ 底部窄白条居中：小号黑色括注「（雾峰村）」

透明背景。文字仅限：雾峰 / 무봉 / Wufeng / 分水岭 / Fenshuiling / 茶山 / Chashan / 雾 / 青 / 1 / 2 /（雾峰村）；
禁止：終點站、始發、종착역、ごめん、後免、JR、高知県、松桥、Songqiao、矿、其他任何字形。
1536×1024，物件居中，四周留边，横牌完整入画。
（实操结果：成图含母图同款两侧深色立柱，已接受为制式一部分）
```

**线路与站番号（2026-09-30 定稿，照母图站番号递增规则）**：雾峰村站为**两线交汇站**，两线均以本站为 0km 起点（母图本站 D40／左 D39／右 D41 的递增逻辑同款）：
- **雾峰村线**（＝雾峰村-乌金岭线，23km，sign 徽字「雾」）：雾1 雾峰村 → 雾2 茶山 → 雾3 松桥 → 雾4 矿区
- **青溪线**（＝雾峰村-青溪镇线，45km，sign 徽字「青」）：青1 雾峰村 → **青2 分水岭（外侧首站，约10km）** → … → 青溪镇

故站牌：右上双徽＝本站两线番号 **雾1／青1**；蓝带两头＝两线各自下一站 **青2 分水岭（左）／雾2 茶山（右）**，圆环颜色跟所属线（青绿＝青线、紫红＝雾线）。关联权威：车辆设定 §五、区域解锁 §1.1。若线位再调，三处同步改。

**锁定记录（2026-10-01）**：成图验收通过（透明底四角 alpha=0 无烧底 ✓、1536×1024 ✓、白名单文字全对 ✓、禁词零出现 ✓）→ 入库 `ExternalAssets/wufeng_signage_ref.png` → 待锁清单、§2.17 已同步指向本图。后续站景（station_sunset / platform_morning / platform_evening）生成时挂本图锁站牌制式。

---

### 2.17 车站夕阳（station_sunset）

**文件**：`Resources/bg/station_sunset.png`
**用途**：序章 Day 4/10 — 雾峰站全景，夕阳下的车站、站牌、月台、停着的 NF-5K。
**状态**：✅ 已重绘入库（2026-10-01）——成图《雾峰车站：樱花夕照下的列车.png》→ `Assets/Resources/bg/station_sunset.png`（补上 prologue_04/10 所引用的缺失文件；旧 `bg/station.png` 为图鉴用，保留）。子代理验收：站牌全制式且无禁字、铭牌「耕牛 / 경우」双语、站房小牌「雾峰」、无人物、布局合理。**已知偏差（用户拍板接受）**：车厢约 4 节（非"恰好两节"）、车头略偏右、画风为精细插画带像素颗粒而非纯像素。兼任 NF-5K 前斜视 + 雾峰站前置参考。
**前置**：NF-5K 外构（挂 `ExternalAssets/nf5k_reference.png`，已锁）、雾峰站站牌样式（挂 `ExternalAssets/wufeng_signage_ref.png`，✅ 2026-10-01 锁定 → §2.16.1）、C-55000 敞车（§2.0.1 已锁定 `c55000_exterior_ref.png`）——**三前置全齐，可重绘**

**站名/文字规范（2026-09-22）**：
- 站牌按 §2.16.1 锁定制式（雾峰/무봉/Wufeng 主名＋青2 分水岭/雾2 茶山 邻站蓝带），**禁止写「終點站」「종착역」「始发站」**——列车对开往返，终到只是车次方向，不写死在站牌上
- 墙面**禁止巨型手写站名**；站房外墙用小型站名牌 + 宣传栏 + 玻璃橱窗时刻表
- 站名统一「雾峰」（站牌/墙面），口语/剧本可写雾峰村站；勿混用「雾峰 — 終點站」

**挂图（2026-10-01 更新，三张全挂）**：
- 站牌：挂 `ExternalAssets/wufeng_signage_ref.png`（站牌制式锁定源，含两侧立柱）
- 机车：挂 `ExternalAssets/nf5k_reference.png`（现役涂装直接使用，不加任何 ignore 指令）
- 敞车：挂 `ExternalAssets/c55000_exterior_ref.png`（车后恰好两节）

**提示词（v6 · 用户风格版，2026-10-01：一行定调＋附图引用＋编号条目）**：
```
参考图三张，1920×1080，16:9，清晰像素风，星露谷风格像素画，16-bit复古，

雾峰站 夕阳全景 —— 站牌/机车/敞车的形制照三张附图（站牌 / NF-5K机车 / C-55000敞车），构图自左至右：

① 左缘：老式木电线杆，单盏灯，灯是灭的
② 中左站房：灰砖墙红陶瓦屋顶，门廊罩着入口；立面禁止巨型手写站名——门楣上方小搪瓷牌白底蓝字「雾峰」（普通尺寸，低调）；玻璃橱窗「列车时刻表 列차시각표」（纸页泛黄，有手写改点）；软木宣传栏「宣传栏 선전란」（褪色安全标语＋一张雾峰村—矿区线路的剥落告示）；门旁立式牌「候车室 대기실」
③ 前景左站牌（形制照附图站牌）：白底横牌＋两侧深色立柱；主名「雾峰」大字，其下「무봉」「Wufeng」；右上双徽＝紫红「雾1」＋青绿「青1」；中部亮青蓝带＝左端青绿徽「青2」＋「分水岭 Fenshuiling」白色字、右端「茶山 Chashan」白色字＋紫红徽「雾2」；底部窄白条「（雾峰村）」
④ 中央站台：磨损灰石板/混凝土（非木板）；木长椅上绿色保温壶＋劳保手套；木行李推车堆麻袋；站台边缘黄野花绿草、中央扫净
⑤ 右侧机车（形制照附图 NF-5K）：「耕牛·改」停在单线，朝观众左前方——深蓝车身，红白波浪条纹环绕，樱花图案，风化流痕，鼻端无白色V纹；红星翼徽＋蓝牌白字「NF-5K」＋黄铜铭牌「耕牛 / 경우」；车身朝鲜国旗＋红底放射光芒宣传画「더 밝은 미래를 위하여」；单盏居中大圆灯，红色缓冲梁，板条排障器，两盏标志灯；短罩走廊式，柴油机车，不是高铁
⑥ 车后恰好两节 C-55000 型运煤敞车（风化棕红色，形制照附图）
⑦ 最右：高耸锈蚀臂板信号机，臂板锁在水平停车位，白色小牌「停車 정지」
⑧ 背景：橙粉色晚霞压着层叠蓝紫色山影，散着紫色碎云；暖金色光自左侧射入，拉出长影

文字仅限：雾峰 / 무봉 / Wufeng / 分水岭 / Fenshuiling / 茶山 / Chashan / 雾1 / 青1 / 青2 / 雾2 /（雾峰村）/ 列车时刻表 / 列차시각표 / 宣传栏 / 선전란 / 候车室 / 대기실 / 停車 / 정지 / NF-5K / 耕牛 / 경우 / 더 밝은 미래를 위하여 / 告示小字「雾峰村—矿区线路」
禁止：終點站 / 종착역 / 始發 / 松桥 / Songqiao / ごめん / 後免 / 巨型手写墙字
无人物、无人、无角色、无飞行器——纯环境背景；机车与两节货车为有意保留。
氛围：黄金时刻的寂静山村小站，空站台，暖光，长影。
```

**实操定稿提词（2026-10-01 用户版 · 四图开放风格，出图即上文成图）**：
```
参考图四张，1920×1080，清晰像素风，星露谷风格像素画，
雾峰站 夕阳全景 —— 站牌/机车/敞车分别照图一/图二/图三；图四是乡间小站的场景样例，参考它的站景布置：站房、站牌、列车的空间关系，画面的干净精致和生活感细节；画风仍是像素风，光线是夕阳黄金时刻。其他元素适当发挥，布局参考现实中的乡村小站：

站牌（图一，立在站台边缘、平行于站台走向）

机车（按照图二）：「NF-5K」停在单线，车头朝向观众、略偏向画面左侧，车头黄铜铭牌与图二完全一致（耕牛 / 경우）；车后恰好两节 C-55000 型运煤敞车（参考图三），没有第三节

以下元素供参考，自行取舍添加，让画面像一座真实存在过的乡间小站：
- 站台：混凝土站台与黄色盲道、木长椅（放着保温壶和旧手套）、角落里行李推车
- 站房：灰砖红瓦小屋，门旁小搪瓷站名牌「雾峰」，玻璃橱窗里泛黄的时刻表，墙上宣传栏贴着旧告示
- 站台旁：木栅栏、高杆路灯、老式电线杆、锈蚀的臂板信号机
- 环境：路侧樱花树、稻田野花、错落的红顶民居、层叠远山、低垂落日与橙粉晚霞、电线横过天空
- 氛围：黄金时刻的暖光拉出长影，整个车站安静空旷

画面里不要有人，空站台
```

> 实操经验（2026-10-01）：参考图四＝《爱上火车》站景样例，只借站景布置与精致感，不搬画风光线；长束缚提词（v6 五段式）出图反而差，开放版＋少量硬约束＋素材库效果最好；铭牌/站名类小文字要挂在限定行里才不被忽略。

---

### 2.17.1 雾峰站正面全景（station.png）❌ 废弃 / 待重绘

**文件**：`Assets/Resources/bg/station.png`  
**规格现状**：3424×2416（比例 1.42 异形，文件体积 7.18 MB，仅 `test.json` 引用）。  
**资产状态**：❌ **废弃 / 待重绘**（2026-10-10 T295 核查确认）。画面细节与雾峰站正规设定不符，非 16:9 比例且文件过大。用户明确批示否决「雾峰站正面图片是错误的」。  
**处置建议**：原文件废止移入回收站候选，`test.json` 临时改接 `station_sunset`；如后续仍需正立面全景，使用以下 16:9 像素风标准提词重出：

**重绘提示词（16:9 标准比例 · 雾峰站站房正面全景 · 中文站牌）**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, warm cinematic lighting, crisp pixel details,
front exterior view of small rustic Chinese railway station building, afternoon sunlight:

STATION ARCHITECTURE:
- Low-rise single-story railway station building made of weathered grey bricks and reddish-brown clay roof tiles
- Modest entrance with a small covered porch and wooden pillars
- Small blue-and-white enamel station sign mounted centrally above entrance porch: 「雾峰」in clear Chinese characters
- Wooden ticket office window (售票处) and waiting room entrance (候车室) with wooden bench outside
- Bulletin board with yellowed railway safety posters and hand-drawn timetables

SURROUNDINGS & PLATFORM:
- In front of station: gravel plaza leading to a low concrete platform
- Single railway track running horizontally across foreground with weathered steel rails, wooden sleepers and ballast gravel
- Wooden utility pole with vintage electric streetlamp (unlit) and overhead telephone wires
- Surrounding scenery: rolling green and golden terraced hills, distant mountain ridges under soft clear sky
- Small flowerbeds with yellow wild chrysanthemums growing beside the stone steps

ATMOSPHERE & LIGHTING:
- Clear afternoon lighting with soft shadows, nostalgic rural railway atmosphere
- Quiet, peaceful, evoking the passage of decades

RESTRICTIONS:
STRICTLY NO CHARACTERS, NO HUMANS, NO FIGURES.
STRICTLY NO ENGLISH, NO LATIN LETTERS, NO KOREAN, NO HANGUL.
RATIO MUST BE STRICT 16:9 (1920x1080).
```

---

### 2.17.2 田园铁路鸟瞰全景（railway_map_aerial.png）❌ 待重绘

**文件**：`Assets/Resources/bg/railway_map_aerial.png`  
**规格现状**：1376×768（16:9，GameMainUI 相册索引图）。  
**资产状态**：❌ **待重绘**（2026-10-10 用户明确裁定）。现存图虽然描绘了山谷小站与铁轨，但属于斜 45 度等轴瓦片画风，缺乏景深与电影感光影层次，与全篇暖色调电影感像素风（如 `mountain_sky` / `station_sunset` / `railway_track`）画风脱节。用户明确批示：**「这个是有问题的画风不一样 需要重绘」**。  
**处置建议**：保留文件用于相册兼容，后续出图后无缝替换。

**重绘提示词（16:9 标准比例 · 电影感暖色调像素画风 · 山谷水乡铁道鸟瞰全景）**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, warm cinematic lighting, crisp pixel details,
high aerial panoramic view of a rural railway winding through mountain valley and countryside, golden morning sunlight:

SCENERY & COMPOSITION:
- High angle aerial panoramic view looking down across rolling green hills, terraced tea fields, and a river valley
- A single-track railway line gently snaking through dense pine forests and countryside from foreground to distant mountains
- An arched stone railway bridge crossing a crystal-clear mountain stream rushing over rocks
- Two small rural wooden railway halts / platforms nestled along the tracks with modest wooden station sheds
- Clustered rustic village houses with grey slate and orange-red tiled roofs, light smoke gently rising from brick chimneys
- Golden morning sunlight bathing the eastern valley slopes, casting soft warm long shadows across the landscape
- Far distance: layers of soft blue mountain ridges fading into a pale golden misty morning sky

COLOR PALETTE:
- Dominant warm green pine, golden ochre terraces, weathered stone grey, rust-brown rails, warm amber sunlight
- Unified with the cinematic warm nostalgic palette of Stardew Valley and Ghibli rural landscapes

RESTRICTIONS:
STRICTLY NO CHARACTERS, NO HUMANS, NO FIGURES.
STRICTLY NO MODERN VEHICLES, NO HIGHWAYS, NO MODERN SKYSCRAPERS.
CONSISTENT WARM CINEMATIC PIXEL ART STYLE, NOT FLAT ISOMETRIC TILES.
```

---

### 2.18 傍晚站台（platform_evening） ✅ 已入库

**文件**：`Assets/Resources/bg/platform_evening.png`（✅ 已定稿入库，1024×571 RGB PNG）  
**用途**：序章 Day 4 林彪悍下车踏上月台的近景——"到家了"的瞬间。与 station_sunset 的区别：本图为站台近景特写。  
**资产状态**：✅ 已定稿入库（用户定稿图）

**叙事锚点**：Day4——林彪悍踏上站台的瞬间。木板上还有爷爷当年钉的钉子头，锈成和木头一样的颜色。手套和水壶是老陈的——他刚巡完23公里线回来，还不知道今天会有人来。站台灯亮着，像一直在等。

**挂图（生成时附四张）**：
- 图一 `ExternalAssets/wufeng_signage_ref.png`——站台站牌制式锁定源
- 图二 `ExternalAssets/nf5k_reference.png` ＋ 图三 `ExternalAssets/c55000_exterior_ref.png`——若机车/车厢入画边缘则照此涂装
- 图四 `Assets/Resources/bg/station_sunset.png`——**同日同站全景定稿**：本图是它同一时刻的站台近景，光线、色调、氛围、物件制式全部向它对齐

**提示词（2026-10-01 · 最新标准版，四段式）**：
```
参考图四张，1920×1080，清晰像素风，星露谷风格像素画，
雾峰站 站台近景·傍晚 —— 图四是同一时刻的车站全景定稿，本图是站台上的近景：色调、光线、物件制式全部与图四一致；站牌照图一制式；其他元素适当发挥，布局参考现实中的乡村小站：

视角：站在站台上、面向站房的地面近景——刚下车的人的视线高度

以下元素供参考，自行取舍添加，让画面像一座有人默默守了四年的站台：
- 前景：磨损的站台铺面，板缝里长着杂草；木板上爷爷当年钉下的钉子头，锈成和木头一样的颜色
- 中景站房：灰砖红瓦小屋立面，门旁小搪瓷站名牌「雾峰」（白底蓝字，低调尺寸），玻璃橱窗里泛黄的时刻表，墙上宣传栏；两扇窗亮着暖黄的灯光——有人从不关灯
- 站台站牌（照图一制式）：白底横牌＋两侧立柱，主名「雾峰」大字，其下「무봉」「Wufeng」，右上双徽＝紫红「雾1」＋青绿「青1」，蓝带左端青绿徽「青2」＋「分水岭 Fenshuiling」、右端「茶山 Chashan」＋紫红徽「雾2」，底部（雾峰村）
- 站台长椅上：一双补过的皮手套＋一个凹瘪的军绿色保温壶——老陈刚巡完线放下的，人进屋倒茶去了
- 一盏亮着的站台灯，灯罩落灰；远处臂板信号机的绿色信号灯亮着
- 天空：暮色渐沉的橙紫晚霞，长影子拉过站台

文字仅限：雾峰 / 무봉 / Wufeng / 分水岭 / Fenshuiling / 茶山 / Chashan / 雾1 / 青1 / 青2 / 雾2 /（雾峰村）；站房小牌只写「雾峰」两个字
禁止：終點站 / 종착역 / 始發 / 松桥 / 矿 / 雾峰站 / 巨型手写墙字
近景站台留空（角色立绘要叠在上面，前景不要站人）；远景允许少量模糊的旅客剪影，不抢焦点；机车不做画面主体（至多轨道远处一点暗示），主体是站台、站房与那盏一直亮着的灯
```

---

### 2.19 边境小镇（border_town）

**文件**：`Resources/bg/border_town.png`
**用途**：序章 Day 1 上午——临江市（中朝边境边贸城）上空视野。0721 补沙前短暂停留。被四家单位追捕的紧张感。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
边境小镇 / 변경소계정, border town morning, tension beneath calm:

**叙事锚点**：Day1——0721降落在临江市附近，林彪悍第一次踏上中国土地。心脏还在狂跳——三分钟前他刚从边境检查站的栏杆下钻过去。这座中朝边境小镇看起来平静而普通，但他知道头顶上至少有两架巡逻机在搜索这架不应该出现在这里的蓝色飞行器。

SCENE: 林彪悍从低空俯瞰临江市边境小镇的全貌。这是一座典型的中朝边境小城——鸭绿江支流在画面上方横贯而过，江水灰绿色（灰绿色），水面上停着几艘锈迹斑斑的渡船。江北侧（朝鲜方向）稀疏灰暗，几栋政府灰色建筑上飘着旗帜；江南侧（中国方向）热闹拥挤，街道上人来人往。建筑风格是东北农村砖房与朝鲜风格的混搭——红砖墙（红砖色）配灰瓦顶（기와 giwa），部分建筑有朝鲜风格的白色灰泥外墙和木格窗棂。路牌和招牌全部使用中韩双语：最大的路牌写着「临江市 림강시」（蓝底白字），旁边的商店招牌写着「商店 가게」（红底黄字），一家饭馆的霓虹灯管拼出「饭店 식당」，还有一家「超市 슈퍼마켓」。街道中央有一个显眼的标志——沙能补给站的千里马牌logo（천리마 Chollima品牌，一匹奔跑的千里马剪影，下面写着「千里马沙能补给 천리마 모래에너지 보충소」）。画面右侧可以看到已关闭的边境检查站——一根生锈的红白相间栏杆横在路中间（已抬起），旁边一块褪色的绿色标志牌写着「边境检查 국경검문소」，标志牌下面的小字是「中华人民共和国 국경관리」。检查站旁边停着一辆沙能飞行器——民用版（不是0721），灰白色涂装，机身上有「中国民用航空 Civil Aviation」字样和注册号。建筑屋顶是东北风格的烟囱（红砖砌成，顶部有防雨帽）和灰瓦片屋顶。远处的一面墙上，有一条褪色的红色标语横幅：「欢迎来到中国 환영합니다」，旁边另一条更旧的写着「中朝友谊万岁 조중우의 만세」。晨光从画面右侧斜射，给小镇笼罩上一层清冷的蓝灰色调——看似平静，实则暗流涌动。

LAYOUT:
- 上方远景：鸭绿江支流（灰绿色水面，渡船剪影），对岸朝鲜方向灰色建筑
- 中景：中国侧街道横跨画面，中韩双语路牌「临江市 림강시」在画面中心偏左
- 街道两侧：东北砖房+朝鲜风格混搭建筑（红砖+灰瓦+白灰泥）
- 左下角：沙能补给站（千里马牌logo显著，加油岛+沙罐）
- 右侧中景：已关闭的边境检查站（栏杆抬起，褪色标志）
- 检查站旁：停放的民用沙能飞行器（灰白色，非0721）
- 远处墙面：褪色红色标语横幅「欢迎来到中国 환영합니다」
- 天空：清晨冷蓝色调（冷蓝色），薄云

COLORS:
- 建筑红砖：东北红砖（马鞍棕），部分风化为暗棕（灯芯绒棕）
- 灰瓦屋顶：东北传统灰瓦（灰瓦色），朝鲜风格灰瓦略深
- 白灰泥墙：朝鲜风格白墙，斑驳处露出红砖
- 鸭绿江水：灰绿，水面反射冷蓝
- 千里马logo：金黄（黄铜色）马剪影，红色底圆形标志
- 双语路牌：蓝底白字(深蓝色 / 纯白色)
- 商店招牌：红底黄字(红色 / 金色)
- 检查站栏杆：红白条纹褪色(浅褪色红 / 米黄色)
- 边境标志：绿底白字(深森林绿 / 纯白色)，已褪色
- 褪色标语横幅：红底金字模糊
- 民用飞行器：灰白涂装，蓝色机身线条
- 天空：清晨冷蓝，地平线偏灰白

MOOD: 两个世界的边境。一个什么都能买到的地方——包括沉默。林彪悍的心跳在0721降落时加速。下方，商贩在摆摊；上方，追捕在继续。这座小镇不知道自己是一场逃亡的舞台。它只觉得今天是星期二。空气中弥漫着沙子的气味——这种新型燃料像石油一样统治了2076年的交通，而这座边贸城就是它的交易节点。千里马的logo到处都是，像一种无声的权力宣告。

NO CHARACTERS, NO PEOPLE, NO HUMAN FIGURES — this is a pure environment/scene background, empty of all human presence. Even if the narrative mentions people (for context), the generated image must contain ZERO human figures.
```

---

### 2.20 补给站（supply_station）

**文件**：`Resources/bg/supply_station.png`
**用途**：序章 Day 1——临江补给站近景。0721 补充 1500kg 沙子的地方，员工那句"沙子便宜"的背景。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
补给站 / 보급소, sand-energy supply station close-up, daytime:

SCENE: 临江市沙能补给站的近景。标准化的加油岛（与中国2020年代加油站布局相似，但设备全部替换为沙能相关）——两个圆柱形沙子储存罐（高约3米，银灰色金属材质，外壁有白色刻度线标注容量：500kg/1000kg/1500kg/2000kg，顶部有压力阀和安全帽）。储存罐之间是加沙设备——一根粗大的软管连接着类似加油枪的装置（加沙枪，金属质感，前端有沙漏形漏斗），软管盘绕在地上。加油岛上方是波纹铁皮顶棚（生锈的橙棕色），顶棚正面挂着千里马牌的标准化标志：圆形红底（红色），中间金色千里马剪影（金色），下方白色文字「千里马沙能补给 천리마 모래에너지 보충소」。顶棚侧面有另一块标志牌：「USET保障总局 USET 보장총국」（蓝底白字，深蓝色 / 白色），表明这是国有保障系统下属的标准化设施。收费亭在加油岛右侧——小型玻璃窗亭子（约2x2米），里面坐着一个穿灰蓝色制服的收费员，桌上有一台老式计算器和一本手写账簿。收费亭玻璃窗上贴着价目表：「沙子 모래 0.85元/公斤」（白纸黑字手写），旁边有「千里马会员 천리마 회원 优惠5%」的红色贴纸。地面上散落着灰白色沙粒，几辆手推车停在旁边（金属框架，橡胶轮），车上还残留着沙子。远处可以看到一块沙能载具广告牌——画着一架流线型飞行器，广告语是「千里马·驰骋万里 천리마·만리를 달리다」。一个穿灰蓝色工作服的年轻工人正在操作加沙枪，沙子从漏斗中流出时有轻微的灰尘飞扬。

LAYOUT:
- 前景中心：两个圆柱形沙子储存罐（银灰色，白色刻度线）
- 储存罐之间：加沙设备（软管+加沙枪），工人正在操作
- 中景上方：波纹铁皮顶棚（生锈橙棕），正面千里马标志，侧面USET标志
- 右侧：小型收费亭（玻璃窗，收费员可见），价目表贴在窗上
- 左侧地面：手推车（金属框架），散落沙粒
- 远处背景：沙能载具广告牌（千里马·驰骋万里），小镇建筑轮廓

COLORS:
- 储存罐：银灰金属，刻度线白（纯白色），容量数字黑（深灰黑）
- 沙子：灰白偏黄，散落地面的沙粒
- 顶棚：生锈波纹铁皮，锈蚀处深棕
- 千里马标志：红底，金马（黄铜色），白字（纯白色）
- USET标志：蓝底（深蓝色），白字（纯白色）
- 收费亭：金属框架灰（银灰），玻璃浅蓝反光
- 价目表：白纸（米白），黑字（深灰黑）
- 工作服：灰蓝
- 手推车：金属银灰，橡胶轮黑
- 广告牌：蓝白配色(中蓝色 / 纯白色)，飞行器图案流线型

MOOD: 平凡的商业行为。沙子——2076年的石油——在这里按公斤出售，像买菜一样随意。价格牌上的手写字迹透露出一种日常感，千里马标志挂在顶棚上，标准化设施整洁而普通。地面散落着灰白色沙粒。

NO CHARACTERS, NO PEOPLE, NO HUMAN FIGURES — this is a pure environment/scene background, empty of all human presence. Even if the narrative mentions people (for context), the generated image must contain ZERO human figures.
```

---

### 2.21 中国上空（china_sky）

**文件**：`Resources/bg/china_sky.png`
**用途**：序章 Day 1-3 多次使用——飞越中国领空的云海/天空镜头，新闻阅读与闲聊的背景。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
中国上空 / 중국 상공, flying over China, warm transitional light:

SCENE: 从0721舷窗向外望去的中国上空全景。画面下方是无边无际的云海——白色积云层叠翻涌，像棉絮铺成的平原，云层顶部被午后阳光染成金白色，底部是柔和的蓝灰色阴影。云海缝隙中隐约可见地面——华北平原的农田拼图（金色麦田与绿色玉米地交错）、一条蜿蜒的河流（灰蓝色，可能是辽河支流）、以及远处的村庄灰点。最关键的细节：云层缝隙中隐约可见一条废弃铁路线在地面蜿蜒——锈棕色的细线穿过田野，在一个小镇处断开（道岔已拆），然后又在远处接上，从空中看细得像一根头发。光线有明显的色调过渡：画面左侧（朝鲜方向）偏冷蓝灰调（蓝灰色），右侧（中国内陆方向）偏暖金橙调（暖金橙），中间渐变融合。一架小型民用沙能飞行器的剪影在远处天际线（可选，用于空间比例感）。

LAYOUT:
- 下方80%：云海（翻涌积云，顶部金白，底部蓝灰阴影）
- 云海缝隙（画面中下方偏右）：隐约可见华北平原地面（农田拼图+河流+废弃铁路线）
- 废弃铁路线：锈棕色细线（锈棕色），从左下延伸至右上，在一个小镇处断开
- 上方20%：晴朗天空，色调从左冷蓝（蓝灰色）过渡到右暖金（暖金橙）
- 远处天际线：微型飞行器剪影（可选）

COLORS:
- 云层顶部：金白（金白云），阳光直射处纯白（纯白色）
- 云层底部阴影：蓝灰（浅蓝灰），深层
- 天空色调过渡：左侧冷蓝灰（蓝灰色） → 中间中性 → 右侧暖金橙（暖金橙）
- 地面农田：金色麦田（金麦田）与绿色玉米地（玉米绿）交错
- 河流：灰蓝（灰蓝河），在云隙中若隐若现
- 废弃铁路：锈棕（锈棕色），桥墩段偏锈红（锈红）
- 铁路断开处：道岔拆除后的泥土色

MOOD: 漫长的旅程，辽阔的天空。翻涌的云海顶部金白、底部蓝灰，光线从左侧冷蓝灰过渡到右侧暖金橙。云层缝隙中若隐若现的废弃铁路线穿过田野，安静而辽阔。
```

---

### 2.22 河北小镇（hebei_town）

**文件**：`Resources/bg/hebei_town.png`
**用途**：序章 Day 2 下午——飞越河北上空时俯瞰的华北平原小镇。

**叙事锚点**：Day2——从空中俯瞰华北平原。这是一片林彪悍从未踏足却因爷爷的讲述而熟悉得像故乡的土地。地面上偶尔能看到废弃铁路的痕迹——锈色的线条在绿色田野中格外显眼。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
aerial view of a North China plain town, afternoon:

SCENE: Flat farmland with a small town grid from high above. Wheat and corn plots in patchwork, a straight county road, grey-roofed village houses in tidy rows, a water tower, a middle school track, occasional trees. A small freight yard with abandoned sidings visible — the old railway line cutting through, rusted and quiet.

LAYOUT:
- Patchwork farmland dominates
- Town grid center, water tower landmark
- Old railway line diagonal with overgrown sidings
- Distant green hills

COLORS:
- Fields: golden wheat + green corn patches
- Houses: grey tile roofs
- Railway: rusty brown

MOOD: North China flatlands. Orderly, productive, the railway forgotten.

NO CHARACTERS, NO PEOPLE, NO HUMAN FIGURES — this is a pure environment/scene background, empty of all human presence. Even if the narrative mentions people (for context), the generated image must contain ZERO human figures.
```

---

### 2.23 河南小镇（henan_town）

**文件**：`Resources/bg/henan_town.png`
**用途**：序章 Day 3 傍晚——飞越河南，中原大地景象。

**叙事锚点**：Day3——旅途过半。窗外的景色从北方的平原逐渐变成中原的丘陵。林彪悍看着地图上越来越近的"雾峰村"三个字，心跳开始加速。爷爷说过的那些地名——茶山、松桥、矿区——一个个在地图上变大。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
aerial view of Central China plain at dusk, sloping light:

SCENE: The great Central China plain at long evening light. Villages with grey-tiled roofs, cypress windbreak lines, canals and irrigation ditches reflecting orange light, a highway with tiny trucks, distant Yellow River haze. Slightly warmer, dustier atmosphere than North China.

LAYOUT:
- Flat plain, village clusters
- Canal/ditch system reflecting sunset
- Long shadows
- Heat haze on horizon

COLORS:
- Fields: dry gold （干金色）
- Roofs: grey （灰瓦） with patina
- Water: orange reflect （夕阳橙）
- Sky: warm amber

MOOD: Endless flat land, the long road south.

NO CHARACTERS, NO PEOPLE, NO HUMAN FIGURES — this is a pure environment/scene background, empty of all human presence. Even if the narrative mentions people (for context), the generated image must contain ZERO human figures.
```

---

### 2.24 车辆段·晨（depot_morning）❌ 待重绘

**文件**：`Assets/Resources/bg/depot_morning.png`  
**用途**：序章 Day 5 早晨——雾峰站车辆段检修棚，张工拆修 NF-5K 喷油嘴的核心场地（`prologue_07_first_repair.json` 引用）。  
**资产状态**：❌ **待重绘**（2026-10-10 T295 核查确认）。现存图挂有韩文政治标语横幅、工具板悬挂枪械枪支、机车造型偏东风平头，严重脱离世界观设定，用户明确批示否决。**注意：因 prologue_07 强引用，在新图生成替换前严禁物理硬删，避免幕次黑屏。**

**叙事锚点**：四年来第一次有人在这个检修棚里动手修车。晨光穿过棚顶的裂缝照进来，灰尘在光柱中飘浮——像时间本身在呼吸。墙上「安全第一」的标语已经褪色，但今天它又有了意义。

**重绘提示词（16:9 标准像素风 · 纯中式检修棚 · 严禁枪支韩文）**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, warm cinematic lighting, crisp pixel details,
small Chinese railway depot maintenance shed, early morning golden light:

CHINESE RAILWAY WORKSHOP ELEMENTS (STRICTLY NO WEAPONS, NO GUNS, NO KOREAN):
- Wall slogan: 「安全第一 预防为主」faded red characters on aged white wooden board, paint cracking at edges
- Railway safety regulation notice: 「机车检修作业安全规程」yellowed paper pinned to wooden pillar
- Old timetable sheet: 「雾峰村—矿区 运行时刻表」tacked to wooden board with hand-drawn pencil corrections
- Shelves with hand-painted Chinese labels: 「工具柜 01」「配件柜 02」

NF-5K LOCOMOTIVE (dominant on left side, reference: ExternalAssets/nf5k_reference.png):
- NF-5K diesel locomotive, deep blue weathered body, red and white wave stripes
- Brass nameplate: 「耕牛」in clear embossed Chinese characters (NO Korean text)
- Engine hood panel lifted open, revealing diesel engine block, injection pump, fuel lines and mechanical details
- Buffer beam with rust-orange coupling hook, heavy steel chassis
- Morning sunbeams hitting the dusty blue metal surface

WORKBENCH AND MAINTENANCE TOOLS (right side):
- Heavy solid wooden workbench stained with dark lubricating oil, fitted with an iron bench vise
- Disassembled fuel injector components neatly laid on a clean canvas rag: injector body, needle valve, pressure spring, copper washers
- Mechanics tool pegboard: open-end wrenches, socket wrench set, screwdrivers, pliers (ABSOLUTELY NO GUNS, NO RIFLES, NO MILITARY GEAR)
- Vintage red metal oil can with long thin spout, green thermos flask with steam rising faintly
- Open maintenance handbook on bench: 「NF-5型内燃机车技术图册」with technical diagrams
- Inspection pit in concrete floor with wooden ladder, stained with dark oil puddles

ATMOSPHERE & LIGHTING:
- Corrugated iron roof with broken skylights letting in diagonal warm morning light beams
- Golden dust particles floating in the shafts of morning light
- Warm rustic morning atmosphere, revival of forgotten machinery

RESTRICTIONS:
STRICTLY NO CHARACTERS, NO HUMANS, NO FIGURES.
STRICTLY NO WEAPONS, NO GUNS, NO FIREARMS.
STRICTLY NO KOREAN, NO HANGUL, NO ENGLISH, NO GIBBERISH CHARACTERS.
ALL VISIBLE TEXT MUST BE CHINESE HANZI.
```

---

### 2.25 线路巡视（railway_track） ✅ 已入库

**文件**：`Assets/Resources/bg/railway_track.png`（已入库，1672×941 RGB PNG）
**用途**：序章 Day 4 傍晚——林彪悍沿雾峰线徒步巡视。铁轨近景，锈迹与杂草，道砟与枕木。
**资产状态**：✅ 已定稿入库（图样采纳用户定稿版）

**叙事锚点**：23公里的铁轨，每一寸都是老陈用脚丈量过的。铁轨生锈了但没有断——就像这条线本身，被世界遗忘但拒绝死亡。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
ground-level view along single-track railway, late afternoon golden hour, Chinese countryside:

RAILWAY TRACK DETAIL (foreground, dominant):
- Two rusted steel rails running into distance, rust orange-brown with polished silver top where wheels once ran
- Dark wooden sleepers (ties), some cracked and rotting, some replaced with lighter new wood (patches by 老陈)
- Grey ballast stones (碎石道砟) between sleepers, moss growing in gaps
- Wildflowers: yellow rapeseed (油菜花) and purple clover between tracks
- Tall grass growing through ballast, some knee-high along the rail edges

SIGNAL AND SIGNAGE:
- One old signal post ahead, leaning slightly, arm in "stop" position, paint faded to grey
- Kilometer post: 「K15」cast iron, rusted, partially hidden by grass
- Small concrete milepost: 「松桥 8km」hand-painted blue on white, chipped

LANDSCAPE (middle and background):
- Left side: terraced tea plantations climbing the hillside (梯田茶园), neat rows of tea bushes in spring green
- Right side: steep hillside with mixed forest (bamboo, pine, deciduous)
- Far distance: valley narrowing, mist hanging between ridges
- A small stream visible crossing under a stone culvert bridge 200m ahead
- One or two farm buildings with grey tile roofs (灰瓦) in the middle distance

LIGHTING:
- Late afternoon golden hour, sun low from left
- Long shadows stretching across the track from right
- Warm orange-gold light on the rusted rails, making them glow
- Haze in the valley creating atmospheric depth (远山如黛)

KOREAN CHINESE CULTURAL TOUCHES:
- A faded railway notice board at the track edge: 「铁路沿线禁止放牧」(partially hidden by weeds)
- Small stone marker: 「雾峰村铁路 1956」engraved date
- Power lines following the track, one pole leaning

MOOD: Walking through memory. 老陈走了这条路20年，每一根枕木都认识他。铁轨在生锈，但山谷里的野花还在开。
```

---

### 2.26 松桥站（songqiao_station） ✅ 已入库

**文件**：`Assets/Resources/bg/songqiao_station.png`（已入库，1920×1080 像素基准）  
**用途**：序章 Day 4 巡视途中——支线废弃小站“松桥”。几个老人坐在站台上等一辆永远不会来的车。  
**叙事锚点**：这些人每周都来站台，等一辆不再运行的车。松桥站是“等待的重量”的具象化。  
**资产状态**：✅ 已定稿入库（图样采纳用户定稿版）

#### 【母本基准 · 中文结构化提示词（标准学习范式）】
```text
松桥站 · 废弃小站的午后 —— 侧视站台视角，站房在左、空轨在右伸向远方，纯环境空景：

① 站房（画面左半，单间小站约十五平米）：
   白灰墙皮大面积剥落，露出底下红砖，剥落边缘不规则如地图；一扇蓝色木门，漆裂成碎块、门角磕碰露木，门微关；
   门楣上方一块白底蓝字小铁牌写着「松桥」，四角锈穿、蓝字掉漆露出铁锈底；木框窗户玻璃蒙灰，泛黄的棉窗帘半掩，看不见屋内；
   灰瓦屋顶缺了两三片瓦，瓦缝间生着青苔，屋檐木椽末端发黑腐朽；墙根一圈返潮的霉斑。

② 站牌（左前景）：
   原来的大站牌没有了，只剩一块低矮的蓝白小铁牌歪插在草丛里——「松桥」二字半掩在狗尾草后，牌面锈斑累累。

③ 站台（画面中，约八米长的短混凝土台）：
   台面布满裂缝、每条缝里长出杂草与小黄花；台沿磕损不平、缺角；一条褪色到几乎看不见的黄色安全线横贯台面；
   台尾一张旧木长椅——一条座板断掉斜挂着、另一条中间下塌、漆完全磨光露出灰木；
   长椅旁一台铸铁手压水泵，通体锈成橙色、压柄垂落；墙角一个死花坛，只剩一把枯茎（当年有人打理，停车后荒废）。

④ 售票口与细节（站房右墙）：
   铁栅栏小窗，窗上沿一块绿漆木牌写着「售票处」，绿漆剥落；窗内积灰的木柜台，柜上一只老算盘与几卷蒙尘的票卷；
   柜台旁墙上挂着日历，停在 2072——大废线那一年；
   站房侧墙一只玻璃橱窗，里面贴着「松桥站 列车时刻表」，纸面泛黄、多班次被红笔划掉、橱窗玻璃一道裂纹斜穿。

⑤ 铁轨与远景（画面右半）：
   单线铁轨锈蚀严重、轨顶只剩暗淡锈棕，枕木缝里草长得比道砟还高——废弃站没有车；
   铁轨顺畅延展无阻挡。右侧溪流上一座双孔石拱桥跨过小溪（桥下水色深绿映着树影），石拱桥为公路/人行桥，与铁道相交处形成铁路平交道口（立有警示交叉道口标杆）；
   铁轨穿过道口后延伸没入远处山脚松桥隧道黑洞洞的洞口方向；两侧山坡是梯田茶园，茶树深绿弧形条带层层叠叠，再远处山峦叠嶂、谷中薄雾如带。

⑥ 人物（环境剪影，允许）：
   两三位模糊老人——一个自然端坐在断板长椅座板上（严禁悬空与穿模）、一个立在台沿望向轨道尽头、一个蹲在泵边——只有姿态轮廓、没有面部细节，颜色与废墟融为一体（等一辆不再来的车）。

⑦ 光影与时段：
   午后低角度金光从画面左侧来，长影子横拉过空轨道与站台；空气中一层暖金薄霭，远山被雾分出层次、远山如黛；色调暖橙偏旧，像一张褪色的明信片。

禁止项（Negative Prompts）：
   机车与任何运行车辆、现代高铁动车、清晰人脸、密集人群、3D真实感、色条渐变、巨型标语墙字、桥梁与铁轨物理位置冲突阻挡、人物与长椅穿模悬空。
```

#### 【对应英文提示词（SD / Midjourney / DALL-E / MiMo 通用）】
```text
16-bit retro painterly anime landscape, 1920x1080, 16:9. Abandoned rural train station 'Songqiao' in warm golden sunset.
Left: weathered single-room brick station with peeling white plaster revealing red brick, old cracked blue wooden door, rusted iron sign '松桥' above door, ticket window with '售票处' wooden sign, glass showcase with faded train schedule crossed out with red pen, 2072 calendar on wall.
Platform: cracked concrete with yellow weeds, old broken wooden bench where an elderly man sits naturally without clipping or floating, rusted orange iron hand water pump.
Right: single rusted railway track overgrown with grass, extending smoothly without obstruction toward distant mountain tunnel. A two-arch stone pedestrian road bridge spans a deep green stream and intersects the railway as a level grade crossing with warning crossbuck signal.
Background: terraced green tea fields on rolling hills, layered misty mountains, low-angle golden sunset lighting, long dramatic shadows. Nostalgic postcard color palette.
Negative prompt: modern trains, locomotive, crowded people, sharp facial features, railway blocked by bridge, character clipping through bench.
```

#### 【AI 提词范式心得（小站场景标杆）】
1. **空间解耦与道口定义**：明确“石拱桥是跨水公路桥，与铁路在地面形成平交道口”，杜绝 AI 误将铁轨画在石桥顶上导致路线冲突被挡断。
2. **人物与道具重力接触做实**：明确写明人物“自然端坐在长椅座板上”，并在负向词补充“禁止穿模悬空”，避免生成悬空或身体嵌入长椅结构。
3. **岁月痕迹具象化**：具象描述“日历停在2072”、“红笔划掉车次”、“算盘与积灰票卷”、“墙皮露红砖如地图”，赋予场景强烈的废土温情与故事纵深。

---

### 2.27 车站·夜（station_night） ✅ 已入库

**文件**：`Assets/Resources/bg/station_night.png`（✅ 已入库 2026-10-10，1024×571 RGB PNG）
**资产状态**：✅ 由 `platform_evening.png` 复制注册（md5 一致，独立新 GUID）。原 `platform_evening` 保持不动，两处引用互不影响。
**选择理由**：06 幕 4 个 scene 均**无场景级 `chars` 字段**（纯对白+旁白，无角色立绘），人全靠背景剪影呈现——本图的聚首人影与「旧人重逢」叙事吻合，且暖光灯池/烟囱/木长椅/冷蓝山谷全符合下方提词。**原提词保留**，若后续认为图内人影与前景冲突可按提词重绘替换。
**用途**：序章 Day 4 夜晚——雾峰站旧人重逢（张工/李阿姨/王小弟等聚首）的夜景。

**叙事锚点**：一群被时代遗忘的人，在寒冷的夜晚聚在一起，决定重新开始。窗内的暖光与窗外的冷蓝形成对比——这是铁路精神的最后一批守护者。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
mountain railway station at night, warm gathering, cold mountain night:

STATION BUILDING (center):
- Small wooden station building, traditional Chinese mountain architecture
- Grey tile roof with slight curve at eaves
- Wooden walls painted dark brown, paint peeling in places
- 3 windows lit from inside with warm yellow-gold light （暖金）
- Front door open, warm light spilling onto platform
- Smoke rising from chimney (kettle boiling inside)
- 「雾峰村站」sign above door, white characters on dark wood, barely visible in darkness

PLATFORM:
- Concrete platform with one overhead lamp (old sodium vapor, warm orange glow casting a pool of light)
- Wooden bench (one person could sit)
- Edge painted yellow safety line, faded
- Railway track in foreground, rails catching moonlight (silver-blue glint)

DOORWAY WARMTH (no figures — 人的存在靠光和物件暗示):
- The front door open, warm gold light spilling across the platform
- Through the doorway: a small wooden table with a kettle and two cups, lit from above
- A chair pulled out from the table, as if someone just stood up
- Shadows of window frames cast onto the platform by the interior light
- Posture of the scene suggests conversation just happened, but the frame holds ZERO human figures

NIGHT SKY:
- Deep indigo blue （靛蓝） upper sky
- Stars beginning to show (scattered, not dense)
- Mountain silhouettes on horizon, darker than sky
- Slight atmospheric haze in valley (soft blue-grey)

CHINESE DETAILS:
- Faded 「安全第一」slogan barely visible on station wall
- Old railway lantern (unlit) hanging from a nail
- A wooden sign: 「值班室」next to the door
- NO KOREAN, NO HANGUL, NO ENGLISH, NO LATIN LETTERS anywhere in the frame

LIGHTING CONTRAST:
- Exterior: cold blue moonlight and dark shadows
- Interior through windows: warm gold light, inviting
- Platform lamp: single warm orange pool of light
- The contrast tells the story: cold world outside, warm community inside

COLORS:
- Night sky: deep indigo （靛蓝）
- Mountain silhouettes: near-black （近黑色）
- Window light: warm gold （暖金） to amber （琥珀暖光）
- Platform lamp: sodium orange （钠光橙）
- Rail glint: cool silver-blue （银蓝）
- Building: dark brown wood （深棕）

MOOD: 一群被时代遗忘的人，在寒冷的夜晚聚在一起，决定重新开始。窗外是零下十度的山风，窗内是滚烫的茶。

NO CHARACTERS, NO PEOPLE, NO HUMAN FIGURES — this is a pure environment/scene background, empty of all human presence. Even if the narrative mentions people (for context), the generated image must contain ZERO human figures.
```

---

### 2.28 站长办公室（station_office）❌ 待重绘

**文件**：`Assets/Resources/bg/station_office.png`  
**用途**：雾峰站站长办公室 / 调度指挥室。全工程最高频场景，共 9 个剧本引用（`prologue_09_funding` 及 post 篇 8 个剧本）。  
**资产状态**：❌ **待重绘**（2026-10-10 T295 核查确认）。现存图充斥 Namenessy / DISPATCH / LEDGER 等英文及西文乱码标牌，完全背离中式铁路水乡世界观，用户明确批示否决。**极度重要：因 9 个剧本强依赖此图，新图生成并验证通过前绝对禁止硬删，避免大量剧情幕次黑屏！**

**叙事锚点**：老陈守护四年的房间，每件物品都是时间的证据。账本、算盘、老电话泛着旧时光光泽，墙上的运行图留着红色停运标记。安静、凝固、充满时间的痕迹。

**重绘提示词（16:9 标准像素风 · 纯中式调度站长室 · 严禁任何西文英文）**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, warm cinematic lighting, crisp pixel details,
cramped Chinese railway station master's office, early morning golden light through window:

ROOM OVERVIEW:
- Cramped retro railway office (约10平米), worn wooden floorboards with smooth dark grain
- Single window on right wall facing the outdoor platform and rusty tracks
- Low wooden beam ceiling with hanging vintage wire and bulb
- Doorway with carved dark wooden doorplate: 「站长办公室」in Chinese characters

OFFICE DESK (heavy dark solid wood, centered slightly left):
- Old black rotary dial telephone (黑色老式拨盘电话) with spiral wire
- Stack of aged accounting ledgers with yellowed edges and handwritten records
- Traditional wooden abacus (十三档实木算盘)
- Green glass banker's desk lamp with brass stand, emitting a soft warm emerald glow
- Chinese enamel mug (白色搪瓷茶缸) with faded red star and faint tea stain rings
- Pack of vintage red cigarettes with an unlit cigarette in glass ashtray
- Desk stamp pad and wooden stamp: 「雾峰村站」in red ink
- Hand-carved wooden desk plate: 「站长」

WALL MOUNTED ELEMENTS (AUTHENTIC CHINESE RAILWAY DETAILS):
- Large yellowed hand-drawn railway map: 「雾峰村—矿区铁路运行示意图」
  Stations marked along line in clear Chinese: 雾峰村 (0km), 茶山 (8km), 松桥 (15km), 矿区 (23km)
  With red crayon diagonal cross-out marks indicating suspended sections (停运)
- Tear-off wall calendar stopped at: 「2072年9月」
- Old black-and-white framed group photo of 1990s Chinese railway workers
- Vintage railway safety slogan frame: 「人民铁道为人 user-facing 安全正点」faded red banner
- Vintage wall clock stopped at 03:47

FURNITURE & DETAILS:
- Worn brown leather armchair behind desk, upholstery slightly cracked
- Green metal filing cabinet in corner with file folders
- Traditional cast-iron coal heating stove with a metal kettle and stovepipe venting to ceiling
- Coat rack holding an old railway worker cap and dark uniform coat

ATMOSPHERE & LIGHTING:
- Clear morning sunbeams slanting through window panes, illuminating floating golden dust
- Quiet, contemplative atmosphere of a preserved historical railway office

RESTRICTIONS:
STRICTLY NO CHARACTERS, NO HUMANS, NO FIGURES.
STRICTLY NO ENGLISH, NO LATIN ALPHABET, NO Namenessy, NO DISPATCH, NO LEDGER, NO GIBBERISH LETTERS.
STRICTLY NO KOREAN, NO HANGUL, NO JAPANESE KANJI.
ALL TEXT IN THE SCENE MUST BE RECOGNIZABLE CHINESE HANZI.
```

---

### 2.29 站台·晨（platform_morning）

**文件**：`Resources/bg/platform_morning.png`
**用途**：序章 Day 8/09 首班发车破晓清晨——雾峰站月台晨光、空敞车整备待发、老者静候早班车的场景。
**状态**：✅ 已定稿入库（2026-10-09）——成图《雾峰车站：破晓晨台与整备列车.png》→ `Assets/Resources/bg/platform_morning.png`。站牌中朝英全制式、NF-5K「耕牛」深蓝海浪纹、C-55000 三节空敞车（无溢出煤矿）、长椅正常比例老者、晨雾水洼反光、无色条自然晨曦。

**前置参考图**：
- 图一（构图锁定源）：`Assets/Resources/bg/station_sunset.png`
- 图二（站牌制式）：`ExternalAssets/wufeng_signage_ref.png`
- 图三（机车外构）：`ExternalAssets/nf5k_reference.png`
- 图四（敞车外构）：`ExternalAssets/c55000_exterior_ref.png`

---

#### 方案一：【实操图生图简短版】（以图一为底图，增量修正）

```text
参考图四张，1920×1080，风格还原图一，清晰像素风，星露谷风格像素画。

雾峰站 清晨全景 —— 严格继承图一（station_sunset）的构图透视、站房建筑与机车轨道布局，保持原图结构完全不变，仅对以下细节与光影进行优化增量：

① 站牌（照图二制式）：左侧前景白色横立式站牌「雾峰 / 무봉 / Wufeng」，下方标有「分水岭 / 茶山」及青色/洋红色圆形线路徽标，底部标「（雾峰村）」，字迹工整清晰。
② 机车（照图三外构）：停靠在右侧单线上的深宝蓝色 NF-5K「耕牛」柴油机车，红白波浪饰条，车头鼻端黄铜铭牌「耕牛 / 경우」，前大灯亮起温和淡金晨光。
③ 货车车厢（照图四外构·清晨逻辑纠偏）：机车后方挂接的 C-55000 敞车为完全空载（上面敞开，车厢里面没有溢出来的煤矿，露出深色铁锈车斗内壁；涂装参考图四，三节车厢均有完整涂装、朝鲜国旗与车辆编号，涂装一致，恰好三节）。
④ 站台与人物：前景左侧木长椅上放着老式绿色保温水壶与手套；长椅边坐着一位身穿厚实深色棉服、身形比例正常自然的乡村老人，安静等待早班车发车；站台地面覆有清晨微弱露水反光与几片零星落樱。
⑤ 光影与时段调整：彻底移除下午干燥夕阳，转为清晨破晓（Sunrise 06:00 AM）。太阳刚越过山头，光线清透冷冽带有淡金高光，晨雾在山谷间半透明缭绕，天空由天顶冷蓝向东方地平线渐变至蜜桃粉金。注意天空不要色条，像素自然平滑过渡。

禁止元素：
禁止满载煤炭(no coal), 禁止下午夕阳晚霞(no sunset/dusk), 禁止满地模糊蚂蚁黑点小人(no crowded tiny blurry figures), 禁止现代高铁动车, 禁止平滑渐变或3D真实感。
```

---

#### 方案二：【独立文生图全要素超详细版】（全新冷启动出图）

```text
pixel art, 2D background, 1920x1080, 16:9 aspect ratio, STARDEW VALLEY STYLE PIXEL ART, clear pixels, early morning dawn sunrise (06:00 AM):

WUFENG RURAL STATION DAWN ENVIRONMENT:
- Layout: 3-point wide perspective matching mountain railway terminus. Station building and platform on left, curved single track on right with parked train, terraced village and pine mountain ridges in background.

STATION BUILDING & PLATFORM (Left):
- Traditional stone-brick station building with terracotta-tile roof, brick chimney, dark wood rafters, utility pole with wires, and curved gooseneck lamp.
- Foreground left features a prominent upright white bilingual station sign board (wufeng_signage_ref): "Wufeng / 雾峰 / 무봉", sub-routes to "Fenshuiling 分水岭" and "Cheshan 茶山" with cyan and magenta badges, bottom "(雾峰村)".
- Left weathered dark wooden bench with green thermos and work gloves; sitting calmly on the bench is one rural elder villager in padded dark winter coat, normal human pixel proportion matching the bench (NO crowded blurry ant-like figures).
- Concrete platform with tactile paving line, light morning dew water puddle reflections, and scattered pink cherry blossom petals.

LOCOMOTIVE & EMPTY FREIGHT TRAIN (Right Track):
- Deep royal blue NF-5K diesel locomotive parked on the single track: red and white wave stripes along lower body, brass nameplate "耕牛 / 경우" on nose, 3D wings star emblem, glowing golden headlights, thin white pre-heating steam wisp from roof chimney.
- HAULING THREE EMPTY C-55000 GONDOLA CARS: Exactly 3 open-top gondola cars with NO coal inside (clean empty rusty iron floor panels, waiting to be loaded at mines). All 3 cars feature full official blue liveries, national flag badge, and white stencil car numbers.

SURROUNDINGS, LIGHTING & SKY:
- Blooming pink sakura cherry trees framing the station on left.
- Distant rural mountain village with terraced fields and rolling pine mountain ridges shrouded in delicate volumetric morning valley mist.
- Early morning sun just cresting the eastern mountain ridge on upper-right, cool crisp morning air with low-angle pale golden sun rays, long shadows extending leftward.
- Sky: smoothly pixelated morning dawn transitioning from deep cool indigo at top to soft peach-gold near horizon, NO banding artifacts.

NO coal in cars, NO afternoon golden hour, NO sunset, NO dusk, NO crowded ant-like specks, NO modern bullet trains, clean crisp pixel art. --ar 16:9
```

---

### 2.30 车厢内部（train_inside）

**文件**：`Resources/bg/train_inside.png`
**用途**：序章 Day 5 首班车——SY-22 灰雀客车车厢内部，乘客乘坐的镜头。

**叙事锚点**：这节车厢四年没有载过一个乘客。座椅上的灰尘刚被擦掉，窗户上的水汽还没散。12个人坐在里面——这是铁路复活的第一个人。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
old Chinese branch-line passenger coach interior, daytime, first run in four years:

CARRIAGE INTERIOR (30-seat branch line car):
- Bench seats along both sides facing inward, dark green vinyl/wool upholstery （暗森林绿）, worn thin at edges where thousands of passengers once sat
- Wooden armrests between seats, dark brown, varnish worn away in patches
- Narrow aisle with dark linoleum floor, worn smooth in the center from decades of foot traffic
- Windows: wooden frames, glass slightly fogged with condensation from morning warmth mixing with cold glass
- Luggage rack above: iron pipe construction (铁管焊接), slightly bent, carrying woven竹篮 and cloth bundles
- Ceiling: cream panels （米白）, small round lights (4 of 6 working, 2 dark), one ceiling fan (not spinning, dusty blades)
- Door at far end to next carriage (半开), showing connecting corridor with accordion joint

PASSENGERS (8-12 figures, impressionistic but human):
- An old woman with a竹篮 (basket) full of tea leaves, looking out window
- A man in dark work clothes (coal dust on hands), leaning back with arms crossed
- A young couple sitting together, the girl resting her head on the boy's shoulder
- A child standing at window pointing at something outside
- One person near the door looking back at the departing platform
- Figures are warm-toned, showing life and humanity despite old surroundings

WINDOW VIEW (right side, motion blur):
- Green rice paddies and tea plantations rushing past
- Distant mountain ridges with morning haze
- A small village with grey-tiled roofs visible for a moment
- Power lines along the track, poles rhythmically passing

KOREAN CHINESE DETAILS:
- Wall poster: 「爱护公共设施 请勿卧铺躺卧」faded, 1990s railway poster style
- 「禁止吸烟」red circle sign between windows
- Small metal plaque: 「紧急制动阀」next to a red handle
- Luggage rack numbers: 「1」「2」「3」painted in white on metal
- Seat fabric: traditional railway green with subtle horizontal stripe pattern
- Small 「茶山站 8km」distance marker visible through window

FIRST-RUN ATMOSPHERE:
- Morning sunlight streaming through windows, creating warm golden patches on green seats
- Condensation on glass (train just started, interior warming up)
- Slight visual tilt suggesting gentle sway
- A thermos of hot water on the floor near the old woman
- Paper bags and lunch boxes visible (passengers brought food)
- Clean seats (recently wiped down) contrasting with aged walls

COLORS:
- Seats: dark green vinyl （暗森林绿） with lighter worn patches
- Wood: warm brown （旧棕）
- Floor: dark grey-brown linoleum （油渍水泥）
- Walls: aged cream （米白） with yellowing
- Window light: warm morning gold （晨光金）
- Metal fixtures: grey-silver （银灰）
- Passengers: varied warm clothing (reds, browns, blues)
- Outside: bright green countryside

MOOD: 四年后第一趟车——座椅上的灰尘刚被擦掉，窗户上的水汽还没散。12个人坐在里面，有人看着窗外的田野，有人在低声说话，有个孩子趴在窗边。这是铁路复活的第一个早晨。每一件旧物都在说：我们还在。

NO CHARACTERS, NO PEOPLE, NO HUMAN FIGURES — this is a pure environment/scene background, empty of all human presence. Even if the narrative mentions people (for context), the generated image must contain ZERO human figures.
```

---

### 2.40 列车 Sprite（NF-5K / SY-22 侧视 · 规划 A3）

**用途**：规划 Phase 3-A3 列车 Sprite——NF-5K 机车 1024×256、SY-22 车厢 1024×128（游戏内侧视贴图，经营/演示场景用）；轨道+枕木 512×64 可平铺。
**状态**：❌ 待生成（2026-10-04 记录；图像生成周额度已满，**出图待额度重置后按本节提词执行**，暂不生成）
**前置**：
- NF-5K 外构**已锁** `ExternalAssets/nf5k_reference.png`（2026-09-28 定稿，朝鲜升级版）——出图必须挂它作 image reference，形制以它为准。
- SY-22 灰雀外构**待锁**——本节 SY-22 提词兼作外构锁定源，出图选型后存 `ExternalAssets/sy22_reference.png` 并把本节状态改 ✅。
- 轨道+枕木平铺**非 AI**：代码绘制（像素填充即可，见 VNBacklog.cs MakeBallastTex/MakeRailTexture 同风格），不进本清单。

**规格**：纯侧视（车头朝右，完全正侧视无透视）、透明底 PNG、16-bit 像素风、孤立车辆无环境；AI 出图用 2048×768 等大画幅，再裁切缩放到 1024×256 / 1024×128。

**提词 · NF-5K 机车 sprite（挂 nf5k_reference.png）**：
```
像素风，16-bit复古，游戏 sprite素材，清晰大像素边缘：

一辆深蓝色内燃机车的纯侧视图，车头朝右，完全正侧视、无透视。
形制严格照附图 NF-5K 参考（短罩走廊式、比例、转向架、灯位一致）：
- 深蓝车身，下半红白相间波浪条纹 + 樱花图案（朝鲜原厂涂装）
- 深灰车顶，一排淡黄色小车窗
- 红色缓冲梁，板条式排障器，单盏居中大圆灯
- 黑色两轴转向架与钢轮，风化使用痕迹

透明背景，孤立车辆，无环境、无铁轨、无地面投影。
画面中不得出现任何文字、数字、车号、铭牌字样。
```

**提词 · SY-22 灰雀车厢（外构 + sprite 一并锁）**：
```
像素风，16-bit复古，载具外构参考图 + 游戏 sprite，清晰像素边缘：

一节短途支线客车车厢纯侧视图，车头朝右，完全正侧视、无透视：
- 墨绿色车身，奶油色（黄かっ色）窗带，一排均匀的深色车窗
- 浅灰圆顶车顶，车厢两端各一扇门
- 黑色两轴转向架，小巧圆润短车身（30人小车厢，明显短于普通客车）
- 旧漆风化，有使用痕迹

透明背景，孤立车厢，无环境、无铁轨，画面中无任何文字与数字。
（本图锁定 SY-22 灰雀外构：后续「车厢内部 §2.30」「首班车发车」场景一律挂它作 image reference）
```

**生成后锁定动作**：①NF-5K 选型入库 → 裁切缩放存 `Assets/Resources/Art/Vehicles/nf5k_loco.png`；②SY-22 选型存 `ExternalAssets/sy22_reference.png` 并更新待锁清单（§一 · 绘图前置）；③文字白名单：**本节素材一律无文字**——若模型自作车号，后期修掉或重 roll，不因文字放弃比例正确的候选。

---

## 三、BGM（Suno AI 音乐提示词）

> **状态（2026-09-22）**：26个mp3已全部就位于 `Assets/Resources/bgm/`，文件名与脚本ID一一对应，**不再优化/不再新增**。原始生成文件归档在 `资产/bgm-归档/`（*.mp3 不进git）。下表提词仅供重生成或补档时参考。

### 通用规范

- **风格**：纯音乐，无歌词，instrumental
- **平台**：Suno AI
- **格式**：风格描述 + 情绪 + 乐器 + 参考风格

---

### 3.1 melancholy（忧郁·怀旧）

**叙事功能**：这是整首游戏出现频率最高的BGM（20+次）。不是"悲伤"，是**一个被世界遗忘的地方仍然存在的证明**。它出现在废弃的松桥站、停运的铁轨旁、爷爷的旧照片前——每次都在说同一句话：这里曾经有人活过。

**Suno 提示词**：
```
Style: Ambient piano, lo-fi warmth, nostalgic instrumental
Mood: Bittersweet — loss is present but not crushing; hope leaks through the cracks
Instruments: Solo piano (slightly detuned, as if the strings have settled over years), soft cello drone, distant music box playing 3-4 notes on loop, ambient pad like morning fog
Tempo: Slow, 56 BPM — unhurried, like walking through an empty station
Reference: "To the Moon" opening, Stardew Valley winter theme, Joe Hisaishi "One Summer's Day"
Description: A piano plays alone in a room that hasn't been opened in years. The notes are slightly off — not wrong, just aged, like a clock that's lost a few minutes over decades. Underneath, a cello holds a single low note, barely audible, the way you can barely hear wind through broken windows. A music box enters — not the main melody, just 3-4 notes on repeat, as if someone wound it up and walked away. The piece doesn't resolve. It ends the way a memory does: mid-thought, still unfinished. This is the sound of a railway station where the last train left twenty years ago, and the platform is still waiting.
```

**Suno Style（≤1000字符）：**
> Style: Ambient piano lo-fi warmth nostalgic instrumental in D minor, bittersweet loss with hope leaking through cracks, solo slightly detuned upright piano felt dampers soft muffled tone half-pedal sustain notes bleeding together, gentle cello drone barely audible underneath, distant music box playing 3-4 fragile notes on repeat, ambient pad like morning mist through broken windows, slow 56 BPM unhurried walking pace gentle rubato never metronomic, reference To the Moon Stardew Valley winter Joe Hisaishi One Summer's Day, piano alone in abandoned room notes aged like clock lost minutes, cello holds long low note like wind through empty station, music box repeats as if wound up and walked away decades ago, sparse verse structure breathing spaces between phrases, emotional arc quiet acceptance to fragile hope then back to stillness, ends mid-thought mid-memory unfinished fading photograph, reverb like empty hall, gentle fade to silence, instrumental no vocals

**时长**：3:30（循环用，尾部渐弱可无缝loop） | **生成策略**：1次30s片段 → Extend 5-6次至全长

### 3.2 emotional（感人·温暖）

**Suno 提示词**：
```
Style: Emotional piano, cinematic strings, warm instrumental
Mood: Heartwarming, touching, hopeful
Instruments: Piano, string ensemble, soft horn
Tempo: Moderate, 70 BPM
Reference: Stardew Valley dance theme, Studio Ghibli soundtracks
Description: A warm, emotional piano piece with gentle string accompaniment. The melody rises and falls like a heartfelt conversation, perfect for reunion scenes and important dialogues.
```

**Suno Style（≤1000字符）：**
> Style: Emotional piano cinematic strings warm instrumental in G major, heartwarming touching hopeful reunion mood, solo piano melody rising gently over two octaves like arms opening wide, legato phrasing with soft pedal warmth in upper register, gentle repeating left-hand arpeggios right-hand melody like heartfelt conversation, string ensemble enters at 20s sustained warm shadow underneath, viola and cello weave countermelody beneath piano, soft French horn adds golden warmth at 45s counter-melody, moderate 70 BPM with slight rubato breathing room at emotional peaks, brief ritardando at climax returning to original tempo, reference Stardew Valley dance theme Studio Ghibli Hisaishi warm scores, arrangement builds from solo piano to chamber ensemble, outro thinning back to solo piano final notes hanging like unshed tears, reverb like small room with wooden walls, nostalgic forward-looking, ends resolved major chord lingering warmth, instrumental no vocals

**时长**：3:30（循环用） | **生成策略**：1次30s片段 → Extend 5-6次至全长

### 3.3 determination（坚定·希望）

**叙事功能**：出现8次，标记剧情的**转折点**——从被动到主动。第一次是修好耕牛那一刻，最后一次是揭露铁龙真相。不是"胜利音乐"，是"决定站起来的瞬间"。

**Suno 提示词**：
```
Style: Cinematic orchestral build, determined, bittersweet heroic
Mood: Not triumphant — determined. The difference is that triumph is after the battle, determination is the moment you decide to fight.
Instruments: Solo piano opening (hesitant, two notes), cello enters (reluctant but steady), French horn at midpoint (quiet confidence), full strings by the end (not loud — wide, like opening curtains)
Tempo: Gradual build, 58→85 BPM across 90 seconds
Reference: "Interstellar" docking scene, "Your Name" departure, "The Wind Rises" final scene
Description: Two piano notes, hesitant, like a question asked into an empty room. A cello answers — not with melody, but with presence, the way an old friend sits down next to you without speaking. French horn enters at the midpoint, playing a theme that sounds like it's been waiting patiently for its turn. The strings arrive last, not as a wall of sound but as an opening — the way dawn doesn't crash through a window but seeps in gradually. The piece peaks not with volume but with width. Then it pulls back to the piano. Same two notes. But now they sound different. Because you've heard what comes after them. This is the sound of a man who just decided to repair a locomotive with no money, no parts, and no guarantee anyone will ride it.
```

**Suno Style（≤1000字符）：**
> Style: Cinematic orchestral build bittersweet heroic instrumental in E minor, determined not triumphant moment you decide to fight, solo piano two hesitant notes like question into empty room, sustained cello enters reluctant but steady with slow bowed sustain, French horn midpoint quiet confidence muted then opening warm brass, full strings end wide like opening curtains, gradual build 58 to 85 BPM across 90 seconds, reference Interstellar docking Your Name departure Wind Rises finale, cello answers like old friend sitting without speaking, French horn plays melody waiting patiently, strings arrive like dawn seeping through window gradually, minor to major harmonic shift at emotional turning point, peaks with width not volume then pulls back to piano same two notes now different, sound of man deciding repair locomotive with no money parts guarantee, outro resolves sustained chord fading like breath released, reverb mountain valley, ends unresolved hanging in air, instrumental no vocals

### 3.4 morning（清晨·宁静）

**Suno 提示词**：
```
Style: Acoustic folk, calm morning, gentle instrumental
Mood: Peaceful, fresh, optimistic
Instruments: Acoustic guitar, soft flute, light percussion, piano
Tempo: Slow to moderate, 65 BPM
Reference: Stardew Valley spring theme, Animal Crossing morning theme
Description: A gentle, fresh morning piece with acoustic guitar and soft flute. The sound of a new day beginning, birds singing, sunlight streaming through windows.
```

**Suno Style（≤1000字符）：**
> Style: Acoustic folk calm morning gentle instrumental in C major, peaceful fresh optimistic new day mood, fingerpicked acoustic guitar simple repeating melody like birdsong alternating bass thumb pattern, soft flute enters 20s gentle counter-melody floating above like wind through pine trees, light brush percussion adding subtle pulse 30s soft shaker texture, piano joins midpoint warm chord voicings sustain pedal creating harmonic bed, slow to moderate 65 BPM unhurried sunrise pace, reference Stardew Valley spring theme Animal Crossing morning music Hisaishi My Neighbor Totoro morning, arrangement builds solo guitar to gentle quartet then settles back, brief pause at bridge like taking deep breath before day, each instrument enters like different bird species waking up one by one, reverb open valley morning mist, sound of new day beginning small mountain village, warm hopeful never urgent, ends sustained major chord fading nature silence, instrumental no vocals

### 3.5 warm（温暖·安心）

**Suno 提示词**：
```
Style: Cozy folk, warm acoustic, comforting instrumental
Mood: Safe, warm, homely
Instruments: Acoustic guitar, soft piano, gentle bass
Tempo: Moderate, 75 BPM
Reference: Stardew Valley summer theme, fireplace ambient
Description: A warm, cozy acoustic piece that feels like sitting by a fireplace with old friends. Perfect for community scenes and team gatherings.
```

**Suno Style（≤1000字符）：**
> Style: Cozy folk warm acoustic comforting instrumental in A major, safe warm homely fireplace mood, fingerpicked acoustic guitar simple 4-bar loop never develops like daily routine, soft piano enters every 8 bars playing 2-3 notes then retreating, gentle upright bass warm foundation underneath, occasional soft violin harmony color like embers glowing in fireplace, moderate 75 BPM heartbeat tempo steady and unhurried, reference Stardew Valley summer theme fireplace ambient Hisaishi Kiki delivery service warmth, arrangement like small room wooden walls crackling fire, no dramatic changes just steady comfort throughout, reverb station waiting room old wooden benches, wind chime occasionally catches breeze outside window, guitar tone warm slightly worn like played for years by old friend, breathes naturally comfortable silence between phrases, ends same comfortable loop fading gently, sound of community gathering everyone knows each other names, instrumental no vocals

### 3.6 news（新闻播报）

**叙事功能**：出现在5个场景中，传达"世界的官方声音"。在游戏里，新闻是铁龙计划的舆论武器——这段音乐从不中立，它总是站在权力一边。

**Suno 提示词**：
```
Style: State broadcast theme, authoritarian formal, mechanical precision
Mood: Not neutral — carrying the weight of an official story that may not be true
Instruments: Staccato strings (rigid, clockwork), low brass drone (authority), militaristic snare (discipline), single piano note at end (the silence after the broadcast)
Tempo: Rigid, 82 BPM — no rubato, no feeling, machine-precise
Reference: KCNA broadcast opening, CCTV Xinwen Lianbo, "The Death of Stalin" opening
Description: Staccato strings in perfect unison, mechanical, like a printing press producing newspapers. Low brass drones underneath — not menacing, just heavy, the weight of institutional authority. A snare pattern that sounds more like a schedule than music. The arrangement never breathes, never pauses, never lets you forget this is an announcement, not a conversation. At the very end, a single piano note hangs in the air — the silence after the broadcast ends, before you're allowed to think for yourself. This is the sound of a news anchor reading lines someone else wrote, in a country where the news is decided before it's reported.
```

**Suno Style（≤1000字符）：**
> Style: State broadcast theme authoritarian formal mechanical precision instrumental, weight of official story may not be true, staccato rigid strings perfect unison like printing press producing newspapers, low brass drone heavy institutional authority never wavering, militaristic snare more schedule than music counting rigid beats, trumpet fanfare opening like bulletin interrupting silence, single piano note at end silence before you think, rigid 82 BPM no rubato machine-precise locked tempo, reference KCNA broadcast CCTV Xinwen Lianbo Death of Stalin opening, strings play identical rhythm mechanical like propaganda loop, brass drones add weight without menace, brief woodwind passage midpoint official statement before returning march, snare marks time like factory whistle counting shifts, final piano note lonely exposed silence after broadcast ends, reverb large empty government hall echoing, ends abruptly cutting off mid-sentence transmission ending, instrumental no vocals

### 3.6.1 news_explosive（爆炸性新闻·对主角极度不利）

**叙事功能**：当铁龙计划的舆论反击开始，或者主角的行动被官方媒体定性为"破坏"时使用。比普通news更具攻击性和压迫感。

**脚本ID**：`news_explosive`
**文件**：`Assets/Resources/bgm/news_explosive.mp3`
**来源**：Suno生成 "Official Announcement (激情版)"

**适用场景**：
- post_007揭露铁龙后，USET官方反击声明
- 铁龙计划加速行动，对主角施压
- 任何"对主角极度不利的爆炸性新闻"（如被指控破坏铁路安全、涉嫌非法运营等）

---

### 3.7 calm（平静·日常）

**叙事功能**：游戏使用最多的BGM之一（15+次）。不是"无事发生"，而是**暴风雨之间的平静**。它出现在旅途、维修、团队讨论——那些"还没有被沙能威胁"的短暂安宁里。

**Suno 提示词**：
```
Style: Ambient folk, fingerpicked warmth, unhurried daily life
Mood: Peaceful but not naive — the calm of people who know trouble is coming but choose to keep working
Instruments: Fingerpicked acoustic guitar (simple 4-bar loop, never develops), soft piano (enters every 8 bars, plays 2-3 notes then retreats), distant wind chime, subtle room tone
Tempo: Slow, 56 BPM — the tempo of someone walking without a destination
Reference: "Minecraft" day music, Stardew Valley farm theme, "Animal Crossing" hourly music
Description: A guitar fingerpicks a simple melody that loops without variation — the musical equivalent of a daily routine. It doesn't develop, doesn't climax, doesn't go anywhere. That's the point. A piano enters every 8 bars, plays 2-3 notes like someone glancing up from work, then goes back to silence. A wind chime somewhere outside catches a breeze. The whole piece sounds like a Tuesday afternoon in a village where the biggest event is the tea harvest. But underneath the simplicity, there's a low ambient hum — not quite audible, more felt — like the distant thrum of a沙能 engine that never quite goes away. The calm is real, but it's borrowed time.
```

**Suno Style（≤1000字符）：**
> Style: Ambient folk fingerpicked warmth unhurried daily life instrumental in G major, peaceful not naive calm before storm, acoustic guitar simple 4-bar loop never develops never climaxes, piano enters every 8 bars 2-3 notes then retreats to silence, distant wind chime catches breeze random and unhurried, subtle room tone like house breathing afternoon air, very faint cello harmonic undertone barely perceptible, slow 56 BPM walking without destination, reference Minecraft day Stardew Valley farm Animal Crossing 2am, guitar loops without variation like daily routine, piano glances up from work every 8 bars returns to silence, wind chime outside catches breeze irregularly, sounds like Tuesday afternoon village biggest event tea harvest, low ambient hum distant thrum engine never quite goes away, arrangement stays constant refusing to climax or resolve, calm real but borrowed time, reverb small house mountain valley, ends same comfortable loop, instrumental no vocals

### 3.8 train_ambient（火车行驶环境）

**Suno 提示词**：
```
Style: Ambient field recording, train journey, rhythmic instrumental
Mood: Rhythmic, moving, contemplative
Instruments: Rhythmic percussion (train rhythm), soft drone, ambient pad
Tempo: Moderate, 90 BPM (matching train rhythm)
Reference: Train journey ambient, "Snowpiercer" soundtrack (ambient parts)
Description: The rhythmic sound of a train moving through the countryside. A steady, hypnotic rhythm that suggests movement and journey. Perfect for scenes inside the train carriage.
```

**Suno Style（≤1000字符）：**
> Style: Ambient field recording train journey rhythmic instrumental, rhythmic moving contemplative travel mood, soft percussion pattern mimicking wheels on rails clickety-clack steady heartbeat, gentle drone underneath like engine hum felt through carriage floor, ambient pad like landscape blurring past window golden fields, steel guitar harmonics adding traveling loneliness, moderate 90 BPM matching train cadence exactly, reference train journey ambient Snowpiercer soundtrack atmospheric sections, steady hypnotic rhythm train moving through mountain countryside, occasional distant horn echo suggesting other trains in same world, arrangement builds slightly then maintains constant like train never stopping, percussion layers shift suggesting landscape changing plains to mountains, reverb enclosed carriage space with gentle vibration, sense of forward motion without urgency like journey with no deadline, ends rhythm fading as train disappears into distance, instrumental no vocals

### 3.9 ambient_nature（自然环境音）

**叙事功能**：仅用于2个场景——巡视废弃线路。它是**大自然对人类遗忘的回应**：铁轨生锈了，但山谷里的鸟还在叫。不带情感色彩，纯粹是"世界在继续"。

**Suno 提示词**：
```
Style: Organic field recording, nature ambient, no-music soundscape
Mood: Indifferent beauty — nature doesn't care about human stories, and that's somehow comforting
Instruments: Wind through grass and leaves (constant, varying intensity), distant single bird call (irregular intervals), occasional water drip, very faint insect hum
Tempo: No tempo, no rhythm — free time, organic
Reference: Studio Ghibli valley ambience ("Princess Mononoke" forest), BBC "Planet Earth" landscape sequences
Description: Wind moves through tall grass and leafless branches — not gentle, not harsh, just present. A single bird calls somewhere in the valley, not a melody but an announcement: I'm here. Every 20-30 seconds, another call from a different direction. A water drip echoes from somewhere underground — rhythmic but not musical, like a clock counting geological time. The soundscape has no center, no focal point. It's the audio equivalent of a landscape painting where nothing is happening and everything is alive. The iron rails are rusting. The mountains don't notice. That's the point.
```

**Suno Style（≤1000字符）：**
> Style: Organic field recording nature ambient no-music soundscape, indifferent beauty nature doesn't care about human stories somehow comforting, wind through tall grass bare branches constant varying like breathing, distant bird call every 20-30 seconds different direction not melody just announcement, occasional water drip underground echoing rocks, faint insect hum barely audible threading through, no tempo free time organic rhythm, reference Studio Ghibli valley Princess Mononoke forest BBC Planet Earth landscape, wind moves not gentle not harsh just present like weather, bird calls each different species announcing territory across valley, water drip rhythmic not musical geological clock counting deep time, no center no focal point like landscape painting where nothing happens everything alive, iron rails rusting mountains don't notice that is the point, reverb open mountain valley morning mist, sound of world continuing without us patient and unhurried, instrumental no vocals

### 3.10 mystery（悬疑·未知）

**叙事功能**：4次使用，都与**岁月启动**和**边境危机**相关。不是"恐怖"，是**面对未知时的屏息**——0721号封存23年后突然启动，会发生什么？

**Suno 提示词**：
```
Style: Dark ambient, slow reveal, unsettling curiosity
Mood: Not scary — the unease of opening something that's been sealed for 23 years
Instruments: Low sustained drone (a machine waking up), reversed piano notes (time running backward), single high harmonic (a frequency that shouldn't exist), silence between sounds (the real instrument)
Tempo: Very slow, 50 BPM — each sound separated by deliberate silence
Reference: "Annihilation" shimmer sequences, "Outer Wilds" dark bramble, " Arrival" alien communication
Description: A low drone begins — not ominous, just present, like a machine remembering how to vibrate after two decades of silence. Between the drone pulses, silence. Real silence, not music-shaped silence. Then a piano note plays in reverse, as if time is briefly running backward. A high harmonic appears — thin, glass-like, a frequency that feels like it shouldn't exist in this acoustic space. The piece never resolves, never explains itself. It ends mid-drone, as if whatever was waking up decided to wait a little longer. This is the sound of a 23-year-old AI blinking its eyes for the first time, not knowing if the world outside is still the one it remembers.
```

**Suno Style（≤1000字符）：**
> Style: Dark ambient slow reveal unsettling curiosity instrumental, opening something sealed twenty-three years may have changed, low sustained drone machine remembering vibration after decades silence, reversed piano notes time running backward rewinding memory, single high harmonic glass-like frequency shouldn't exist, deliberate silence between sounds real instrument shaping tension, very slow 50 BPM each sound separated 3-4 seconds genuine silence, reference Annihilation shimmer Outer Wilds dark bramble Arrival alien, low drone not ominous just present computer booting after long sleep, between pulses real silence creating unease not music-shaped, piano enters reversed decaying into forward like time correcting, high harmonic appears suddenly unknown frequency response, subtle metallic resonance circuits clicking awake, never resolves never explains ends mid-drone if waking decided wait longer, reverb deep underground metallic reflections, instrumental no vocals

### 3.11 curiosity（好奇·探索）

**叙事功能**：待分配到后续章节。设计为**岁月的性格BGM**——当她发现新事物时的背景音乐。不是探索者的冒险感，是**一个23年没见过世界的AI第一次看到咖啡店时的反应**。

**Suno 提示词**：
```
Style: Playful ambient, childlike wonder, whimsical discovery
Mood: Curious, delighted, slightly overwhelmed — like a child in a toy store, but the child is an AI who's never seen a coffee shop
Instruments: Pizzicato strings (light, skipping), music box (8-note phrase, slightly off-rhythm as if being wound), soft xylophone (individual notes, like counting objects), bassoon (single note, comedic punctuation)
Tempo: Moderate, 72 BPM — the pace of someone walking and stopping to look at things
Reference: "Howl's Moving Castle" curious moments, "Celeste" exploration B-side, Studio Ghibli comedic interludes
Description: Pizzicato strings skip along like someone who can't decide which direction to look first. A music box plays 8 notes, pauses, plays them again slightly differently — as if learning the melody in real-time. Individual xylophone notes land like footsteps: this is new, and this, and this. A bassoon drops a single low note at unexpected moments — comedic punctuation, the musical equivalent of "wait, what?" The piece loops imperfectly each time, like an AI that's still figuring out how patterns work. It's the sound of 岁月 seeing a coffee shop for the first time in 23 years and being genuinely, thoroughly delighted by the concept of espresso.
```

**Suno Style（≤1000字符）：**
> Style: Playful ambient childlike wonder whimsical discovery instrumental, curious delighted AI seeing world first time after long sleep, pizzicato strings light skipping can't decide which direction to look first, music box 8-note phrase off-rhythm wound by unsteady hand, soft xylophone individual notes like footsteps discovering objects, bassoon single note comedic double-take punctuation, moderate 72 BPM walking stopping looking around, reference Howl's Moving Castle curious Celeste exploration B-side Ghibli comedic interludes, pizzicato strings skip and hesitate changing direction, music box plays pauses plays differently learning melody in real-time, xylophone like footsteps this is new and this and this, bassoon unexpected low note comedic wait what moment, loops imperfectly each time like AI still figuring out patterns, reverb small room full of objects waiting to be discovered, ends music box trailing mid-phrase like wandered off exploring, instrumental no vocals

### 3.12 silence（寂静·留白）

**叙事功能**：仅在prologue_10_transition结尾使用一次——序章结束、进入经营的那一刻。不是"没有音乐"，是**所有声音都停止后，你终于听见了自己的心跳**。

**Suno 提示词**：
```
Style: Near-silence, minimal drone, breathing space
Mood: Not empty — full. The silence after everything has been said.
Instruments: Single sustained sine tone (barely above hearing threshold), one distant chime at the 30-second mark (like a station clock), room tone that fades to nothing
Tempo: No tempo — pure time
Reference: Brian Eno "Music for Airports", "2001" monolith scenes, Arvo Pärt "Spiegel im Spiegel"
Description: A single tone, so quiet you're not sure if it's music or your own hearing. It sustains for 30 seconds without change. Then — one chime. Not a melody, just a single note, like a station clock marking an hour that no one is there to hear. After the chime, the tone continues for another 20 seconds, then fades to nothing. Not silence — the absence of sound. The difference matters. Silence is peaceful. Absence is what's left when the last person turns off the lights and walks away. This plays once, at the exact moment the prologue ends and the player realizes: from here, they're on their own.
```

**Suno Style（≤1000字符）：**
> Style: Near-silence minimal drone breathing space instrumental, not empty but full silence after everything said, single sustained sine tone barely above hearing threshold like distant singing bowl, one distant chime thirty seconds like station clock marking hour no one hears, room tone fades nothingness over final seconds, harmonic overtones shimmering below perception, no tempo pure time stretching unmarked, reference Brian Eno Music for Airports 2001 monolith Arvo Part Spiegel im Spiegel, single tone quiet not sure if music or hearing sustains thirty seconds unchanged, one chime single note breaking stillness last note in abandoned church, tone continues twenty seconds fades absence not silence what remains when last person walks away, final five seconds pure dead air, plays once at moment prologue ends player realizes alone with choices, reverb disappears infinite empty space, ends nothing not even room tone pure void, instrumental no vocals

---

## 四、UI 元素

### 通用规范

- **风格**：STARDEW VALLEY STYLE, 16-bit retro pixel art, 暖色调
- **格式**：PNG 透明背景

---

### 4.1 现有 UI 资产（可直接复用，无需生成）

| 文件 | 大小 | 用途 | 状态 |
|------|------|------|------|
| `UI/Login/title_logo.png` | 1.2MB | 标题横幅 | ✅ 已有 |
| `UI/Login/panel_bg.png` | 2.1MB | 面板背景 | ✅ 已有 |
| `UI/Login/button_primary.png` | 512KB | 主按钮 | ✅ 已有 |
| `UI/Login/input_field.png` | 530KB | 输入框 | ✅ 已有 |
| `UI/Dialog/dialog_bg.png` | 2.1MB | 对话框背景 | ✅ 已有 |
| `UI/Dialog/button_confirm.png` | 504KB | 确认按钮 | ✅ 已有 |
| `UI/Dialog/button_cancel.png` | 615KB | 取消按钮 | ✅ 已有 |
| `UI/AvatarFrame.png` | 1.4MB | 头像框 | ✅ 已有 |
| `UI/DefaultAvatar.png` | 1.4MB | 默认头像 | ✅ 已有 |

---

### 4.2 缺失的图标（需生成，11个）

**尺寸**：32×32，PNG透明底  
**风格**：STARDEW VALLEY STYLE, 16-bit retro pixel art, 暖色调, 1px dark outline

| 文件名 | 图标 | 视觉参考 |
|--------|------|---------|
| icon_money.png | 资金 | 沙币/金币，带沙粒装饰 |
| icon_trust.png | 信任 | 握手或爱心，暖色调 |
| icon_train.png | 列车 | 火车头侧面，像素风 |
| icon_passenger.png | 乘客 | 人物剪影 |
| icon_fuel.png | 燃料 | 油滴或火焰 |
| icon_maintenance.png | 维修 | 扳手或齿轮 |
| icon_staff.png | 员工 | 人物头像 |
| icon_news.png | 新闻 | 报纸或喇叭 |
| icon_story.png | 剧情 | 书本或对话气泡 |
| icon_settings.png | 设置 | 齿轮 |
| icon_save.png | 存档 | 软盘或磁盘 |

**通用图标提示词**：
```
pixel art, 16-bit retro style, game icon, 32x32 pixels, STARDEW VALLEY STYLE, transparent background.

STYLE:
- Simple, clear silhouette at 32x32 size
- Warm color palette (brown, gold, warm tones)
- 1px dark outline for visibility
- Pixel-perfect, no anti-aliasing

COLOR PALETTE:
- Primary: warm gold or warm brown
- Outline: dark brown （深棕）

ATMOSPHERE: Clean, readable at small size, fits railway station aesthetic.

AVOID:
- Too detailed (unreadable at 32px)
- Modern or glossy style
- Cold blue or gray tones
```

---

## 五、生成优先级

| 优先级 | 资产 | 原因 | 数量 |
|--------|------|------|------|
| **P0** | 老陈主图+16表情差分图谱 | 序章大量使用，MVP阻塞 | 1+16张 |
| **P0** | 张工主图+16表情差分图谱 | 序章员工集合场景 | 1+16张 |
| **P0** | 王小弟主图+16表情差分图谱 | 序章员工集合场景 | 1+16张 |
| **P0** | 导师办公室背景 | 序章 Day 0 必需 | 1张 |
| **P0** | 停机坪背景 | 序章 Day 0 必需 | 1张 |
| **P0** | 边境小镇背景 | 序章 Day 1 必需 | 1张 |
| **P0** | 雾峰村夕阳背景 | 序章 Day 4 必需 | 1张 |
| **P0** | 车站夕阳背景 | 序章 Day 4 必需 | 1张 |
| **P0** | 傍晚站台背景 | 序章 Day 4 必需 | 1张 |
| **P0** | 铁路轨道背景 | 序章 Day 4 必需 | 1张 |
| **P0** | 松桥站背景 | 序章 Day 4 必需 | 1张 |
| **P0** | 夜晚车站背景 | 序章 Day 4 必需 | 1张 |
| **P0** | 早晨机库背景 | 序章 Day 5 必需 | 1张 |
| **P0** | 车厢内部背景 | 序章首班车场景 | 1张 |
| **P0** | 站长办公室背景 | 序章剧情补贴事件 | 1张 |
| **P0** | 早晨站台背景 | 序章首班车场景 | 1张 |
| **P0** | BGM: melancholy | 序章主要BGM | 1首 |
| **P0** | BGM: emotional | 重逢场景 | 1首 |
| **P0** | BGM: determination | 转折点 | 1首 |
| **P0** | BGM: morning | 白天场景 | 1首 |
| **P1** | 李阿姨主图+16表情差分图谱 | 序章员工集合 | 1+16张 |
| **P1** | 赵师傅主图+16表情差分图谱 | 序章员工集合 | 1+16张 |
| **P1** | 小芳主图+16表情差分图谱 | 序章员工集合 | 1+16张 |
| **P1** | 岁月界面头像 | 序章 | 1张 |
| **P1** | BGM: warm/calm/news/train_ambient | 场景补充 | 4首 |

---

*本文档按最新标准（v2.0 分类清单版）重写，基于实验室成功提示词格式。*

---

### 2.32 边境追击·三面合围（chase_sky · CG）

**文件**：`Resources/bg/chase_sky.png`
**用途**：序章 Day 1 边境危机——0721 遭四家单位三面合围的空战瞬间。**既作为站长日志 CG 鉴赏插画（cg_chase），也保留为场景图备用**。风格应为"静态插画级"，可含主角机小像，构图强调被包围的压迫感。

**提示词**：
```
pixel art, 2D game CG illustration, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
aerial pursuit scene, dramatic composition, late morning:

SCENE: A deep blue 0721 sand flying vehicle (nose right, tiny) is the focal point at frame center, flying low over a sea of clouds. FOUR armored interceptors surround it: two green (Social Security emblem), one grey (border troops), one unmarked black (state security). They close in from left, right and front in a pincer. Electromagnetic interference waves arc between them. Below, cloud sea with a river valley glimpsed far below.

LAYOUT:
- 0721 center, small but clearly visible (3-4% of frame), deep blue with gold accents
- Enemy vehicles larger, surrounding wedge: 2 lower-left, 1 right, 1 top-center
- Interference arcs (blue lightning) between vehicles
- Motion lines and exhaust trails emphasizing speed
- Cloud sea lower half, valley/river seam far below

COLORS:
- 0721: navy blue （海军蓝） gold accents
- Enemy: military green （军绿）, grey, charcoal black
- Interference: electric blue （电蓝）
- Clouds: white with cool shadows
- Sky: pale blue stress-light

MOOD: Odds of escaping: 3%. Alone against four.

EXCLUSIONS:
- No dialogue
- No blood/violence (interceptors are unarmed-looking, blocking not shooting)
```


### 2.33 统一便当店·平壤后街（shop）

**文件**：Resources/bg/shop.png
**用途**：序章 Day 0——平壤大学后街的「통일 도시락」（统一便当店），一个灰色地带的杂货铺。韩文招牌与领袖画像共存，货架上摆满南方走私货。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
small convenience store interior, Pyongyang backstreet, warm afternoon light:

**叙事锚点**：这是两个世界碰撞的缝隙——墙上挂着领袖画像和「主体思想指引我们前进」的标语，标语下方就是装满南方走私饮料的冰柜。旁边贴着韩日二次元美少女海报。在这间店里，政治宣传、二次元文化和违禁商品共处一室，荒诞却真实。

SCENE: 狭小的「统一便当店」（통일 도시락）内部。金属货架从地板堆到天花板，塞满了南方走私商品。柜台后方的墙上：左侧是金日成半身官方肖像画（深棕色木框，略微褪色），右侧是「主体思想指引我们前进」红色标语（白底发黄），标语下方紧挨着一张日本动漫美少女海报（初音未来风格，与领袖画像形成荒诞对比）。柜台玻璃展示柜里摆着Hello Kitty钥匙扣、库洛米手办、动漫贴纸——走私品。柜台上方贴着手写价签（韩文+中文混合）。柜台后是店员的高脚凳。

货架商品（按位置描述）：
- 左侧货架上层：辛拉面（红色包装，6包堆叠）、韩国零食（彩色袋装）
- 左侧货架中层：香蕉牛奶（粉色利乐包，4排）、罐装咖啡（深棕色/金色，3种品牌混放）
- 左侧货架下层：虾条（橘色袋装，3包）、其他膨化食品
- 右侧货架：烧酒（绿色玻璃瓶，多品牌混放，约8瓶）、可乐（红色标签玻璃瓶）
- 冰柜（玻璃门，中右侧）：装满各种南方饮料——韩国汽水、果汁、牛奶，五颜六色
- 冰柜旁地面：两箱可乐（纸箱，红色）
- 柜台上方：主角购买的商品——三盒BBQ炸鸡（堆叠）、两瓶烧酒、一箱辛拉面（放在地上）、散装的虾条/香蕉牛奶/可乐/罐装咖啡

LAYOUT:
- 入口在画面左侧，下午暖光从门外照入
- 左侧：两层金属货架（商品密集）
- 中央：玻璃柜台+收银台+店员凳子
- 右侧：玻璃冰柜+更多货架
- 后墙：领袖画像+标语+美少女海报（三种文化共存）
- 地板上散放着几箱货物
- 空间狭窄（约6-8平米），但商品密度极高

COLORS:
- 室内光线：暖黄色（暖黄色光）下午阳光
- 货架：银灰色金属
- 商品包装：鲜艳的红（辛拉面/可乐）、粉（香蕉牛奶）、绿（烧酒）、橘（虾条）
- 后墙：米黄色（米黄色）老墙
- 领袖画像：深棕色木框+褪色照片色
- 标语：红色字（红色）白底发黄
- 美少女海报：亮粉色+天蓝色（与周围环境形成强烈反差）
- 冰柜内灯光：冷白色荧光

MOOD: 两个世界的碰撞。领袖画像与美少女海报共存，主体思想标语下卖着南方走私货。这间店本身就是一个时代的缩影——官方叙事与真实生活在同一面墙上交战。

NO CHARACTERS, NO PEOPLE, NO HUMAN FIGURES — this is a pure environment/scene background, empty of all human presence. Even if the narrative mentions people (for context), the generated image must contain ZERO human figures.
```

---

---


---

### 2.34 现代咖啡厅（office_modern）

**文件**：`Resources/bg/office_modern.png`
**用途**：post_006——运营第180天，白鹭洲，周鼎铭与林彪悍私下会面的高档咖啡厅。

**叙事锚点**：周鼎铭选择这个地方不是偶然——白鹭洲是权力中心，这间咖啡厅是他"自己的地方"。他说"你想证明的东西，我曾经也信"，这句话在这里说出来，比在任何其他地方都更有分量。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
modern upscale coffee shop interior, white egret city, evening:

INTERIOR:
- High-ceilinged room (4m+), exposed concrete ceiling with industrial pendant lights
- Floor-to-ceiling windows on the left wall, showing a modern city skyline at dusk
- Dark hardwood floor, polished, reflecting pendant light glow
- Round marble-topped tables (2-person), dark leather chairs
- A long wooden bar counter in the back with espresso machine, glass jars of beans
- Bookshelves on the right wall: railway history books, architecture monographs
- A single calligraphy scroll on the wall: 「静」(Jing, "Stillness") in black ink on rice paper, framed in dark wood
- Potted monstera plants near the windows, casting leaf shadows on the floor

CITY SKYLINE (through windows):
- Modern apartment buildings with warm evening lights
- A沙能补给站 logo visible on a distant building (subtle USET presence)
- Mountains silhouetted against purple-orange sunset sky
- Streetlights turning on below

TABLE ITEMS:
- Two cups of black coffee (steaming slightly)
- A small plate with two financiers (French almond cakes)
- An old railway engineering manual (周鼎铭 brought it as a conversation piece)
- A folded newspaper with headline about沙能 transport expansion

LIGHTING:
- Warm pendant lights creating pools of golden light on tables
- Dusk light through large windows, purple-orange gradient
- The city lights outside creating a blue-purple backdrop
- Contrast: warm intimate interior vs cold modern city outside

KOREAN CHINESE DETAILS:
- Menu board behind bar: 「今日特调 · 铁观音拿铁」(Iron Guan Yin Latte) + 「大同江气泡水」
- Small framed photo on bookshelf: old railway workers (周鼎铭 keeps this hidden in his private space)
- Coffee cup brand stamp: 「白鹭洲咖啡 · Baekro洲 Coffee」
- Book spine: 「中国铁路调度系统演进史」(Chinese Railway Dispatch System Evolution)

MOOD: 两个调度员的对话。一个用同一套知识建了沙能帝国，一个在同一套知识上复兴铁路。这间咖啡厅是他们唯一能平等对话的地方——不在公司，不在铁轨上，在一杯咖啡的时间里。
```

---

### 2.35 新闻发布会（press_conference）

**文件**：`Resources/bg/press_conference.png`
**用途**：post_007——运营第240天，林彪悍在雾峰村车站召开新闻发布会，揭露铁龙计划真相。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
press conference inside old railway station, morning light:

CONFERENCE SETUP (inside station building):
- The station office repurposed as a press room
- A makeshift podium: old wooden desk pushed forward, draped with a white cloth
- Behind the podium: the 23km railway line map pinned to the wall — deliberately chosen as backdrop
- A microphone on the podium (old but functional, wired to portable speaker)
- 2-3 tripods with cameras (small news cameras, not professional broadcast)
- Reporters sitting on wooden chairs and benches brought in from the platform (10-12 people)
- A few reporters standing along the walls

WALL DECORATIONS (deliberately visible behind speaker):
- The railway map with red marks (停运标记) — the visual evidence
- A framed black-and-white photo of old railway workers
- 「安全第一」faded slogan (visible but not the focus)
- A small 「雾峰村站」station sign propped against the wall

REPORTERS:
- Mix of small-town journalists and one or two bigger city reporters
- Notebooks, pens, small cameras (not phone cameras — 2076 era)
- One reporter in the front row taking notes intently
- Others looking between the speaker and the railway map

LIGHTING:
- Morning light streaming through the station window (right side)
- Natural light supplementing the indoor pendant lights
- Camera flash spots (small bright points suggesting flash photography)
- The speaker would be backlit by the window, creating a silhouette effect

ROOM DETAILS:
- Old wooden floor (same station office as station_office)
- The coal stove still in the corner (now cold, used as a plant stand)
- Railway technical manuals stacked on a shelf (background detail)
- A kettle on a side table (someone offered tea to reporters)

COLORS:
- Walls: aged cream （米白）
- Podium cloth: white （纯白色） slightly wrinkled
- Reporter notebooks: varied colors (blue, green, red)
- Camera equipment: black and silver
- Window light: warm morning gold
- The railway map: white paper with colored pencil marks

MOOD: 旧车站办公室改造的新闻发布间。晨光从窗户射入，讲台后是23公里铁路线运行图，记者席间有细小的闪光灯亮点。简朴、严肃、以证据为核心。

NO CHARACTERS, NO PEOPLE, NO HUMAN FIGURES — this is a pure environment/scene background, empty of all human presence. Even if the narrative mentions people (for context), the generated image must contain ZERO human figures.
```

---

### 2.36 全屏新闻画面（news_broadcast）

**文件**：`Resources/bg/news_broadcast.png`
**用途**：post_004/009——全屏滚动新闻格式，朝中社报道。不是真实场景，是游戏内的新闻播报画面。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
Korean state television news broadcast screen, formal propaganda aesthetic:

SCREEN LAYOUT:
- Top 20%: dark blue banner with「조선중앙통신」(KCNA / 朝中社) logo in white
- Below banner: red line separator
- Main area (60%): cream/off-white background for text content
- Bottom 20%: scrolling ticker bar (dark blue background, white text)

TOP BANNER:
- 「조선중앙통신」in bold white Hangul on dark navy blue （海军蓝）
- Small red star emblem (★) next to the text
- Date stamp: 「주체115년（2026년）」in smaller white text

TEXT AREA:
- Large headline in bold black Hangul: 「세계 철도 운송량 30% 회복」
- Sub-headline: 「오봉촌선, 림표한站长의 손자에 의해 부활」
- Body text in smaller black Hangul, formal newspaper style
- A small inset photo: railway tracks in a mountain valley (pixel art within pixel art)
- Photo caption: 「오봉촌-광구선 복구 현장」

BOTTOM TICKER:
- Scrolling white text on dark blue: 「련합사능과학기술회사·천리마 브랜드·세계 뉴스...」
- Small red indicator light blinking

KOREAN TEXT (full, no abbreviation):
- 「조선중앙통신」= 朝中社
- 「주체115년」= 主体115年
- 「세계 철도 운송량 30% 회복」= 全球铁路运输量恢复30%
- 「오봉촌선」= 雾峰村线
- 「림표한站长의 손자에 의해 부활」= 在林彪悍站长之孙手中复活
- 「련합사능과학기술회사」= 联合沙能科学技术公司
- 「천리마 브랜드」= 千里马品牌

COLORS:
- Banner: dark navy blue （海军蓝） with white text
- Red star: bright red （标语红）
- Text area: cream/off-white （米白）
- Headline: bold black （深灰黑）
- Ticker: dark blue （暗蓝黑色） with white text
- Red indicator: bright red （红色）

STYLE NOTES:
- Deliberately stiff, formal, state-media aesthetic
- No human figures visible (text-only broadcast)
- The layout is rigid and hierarchical (top-down authority)
- Contrast with the warm, organic village scenes — this is the "official voice"

MOOD: 正式的官方新闻播报美学。刻板、层级分明、宣传风格强烈，红蓝配色对比鲜明。
```

---

## 六、待生成清单（统一跟踪）

> 更新：2026-08-19
> 状态标记：❌ 待生成 / ⚠️ 提示词已备 / ✅ 已入库

### 6.1 背景图（已有提示词，待生成）

| 背景 | 对应场景 | 优先级 |
|------|---------|--------|
| `station_sunset` 车站夕阳 | prologue_04/10 | ⚠️ 待重绘（站牌/墙字） |
| `railway_track` 线路巡视 | prologue_05 | ✅ 已入库 |
| `depot_morning` 车辆段晨 | prologue_07 | 🔴 高 |
| `station_night` 车站夜景 | prologue_06 | ✅ 已入库 |
| `station_office` 站长办公室 | prologue_09 | 🔴 高 |
| `platform_morning` 站台晨光 | prologue_08/09 | 🔴 高 |
| `village_sunset` 雾峰村夕阳 | prologue_04 | ✅ 已生成 |
| `border_town` 边境小镇 | prologue_03 | 🟡 中 |
| `supply_station` 补给站 | prologue_03 | 🟡 中 |
| `platform_evening` 傍晚站台 | prologue_04 | 🟡 中 |
| `songqiao_station` 松桥站 | prologue_05 | 🟡 中 |
| `china_sky` 中国上空 | prologue_03 | 🟢 低 |
| `hebei_town` 河北小镇 | prologue_03 | 🟢 低 |
| `henan_town` 河南小镇 | prologue_03 | 🟢 低 |
| `train_inside` 车厢内部 | prologue_08 | 🟢 低 |
| ~~`mountain_sky`~~ 群山天空 | prologue_04 | ✅ 已入库 |
| ~~`aerial_view`~~ 空中俯瞰 | prologue_04 | ✅ 已入库 |

### 6.2 CG 插画（9 张，提词已写 ✅）

**⚠️ 工作流**：CG含角色时，不从零描述角色外貌——由用户提供角色参考图（已有立绘），作为 image reference 附加到绘图工具。提词只写**场景构图、光影、氛围、色调**，角色部分注明「见参考图」。

**角色参考图清单**（生成CG时附带）：
| 角色 | 参考图路径 | 备注 |
|------|-----------|------|
| 林彪悍 | `characters/lin_biaohan/normal.png` | 主角，25岁研究生，16/16 |
| 老陈 | `characters/laochen/normal.png` | 68岁末代站长，16/16 |
| 嘉颖徐 | `characters/jiaying_xu/normal.png`（flat: `jiaying_xu_normal.png`） | 24岁铁路大亨；**13/16**，缺 worried/shocked/shout/wink |
| 岁月 | `characters/suiyue/normal.png` | 13岁AI少女；约13/16，缺 happy/shocked/shout（surprise 已改名对齐） |
| 张工/李阿姨/赵师傅/小芳/王小弟 | 待绘制 | 序章员工团队 |

| CG 名 | 名称 | 剧情位置 | 角色参考 | 尺寸 |
|-------|------|---------|---------|------|
| `cg_day0_leave` | 启程·0721升空 | prologue_02 出发 | 林彪悍 | full |
| `cg_tea_meet` | 初会·嘉颖徐 | prologue_02 会面 | 林彪悍 + 嘉颖徐 | **small** |
| `cg_first_night` | 客舱·岁月初语 | prologue_02 夜航 | 林彪悍 + 岁月 | **small** |
| `cg_chase` | 边境·三面合围 | prologue_03 | 林彪悍（0721内，小像） | full |
| `cg_arrest` | 边境·引擎盖上 | prologue_03 | 林彪悍 | full |
| `cg_village` | 雾峰·初见黄昏 | prologue_04 | 无角色（风景） | full |
| `cg_team_night` | 旧人·灯下重逢 | prologue_06 | 全员6人 | full |
| `cg_first_run` | 首班车·驶离站台 | prologue_08 | 林彪悍 + 老陈 | full |
| `cg_museum` | 铁路博物馆 | 好感度>90 | 李阿姨 + 孩子们 | full |

---

#### cg_day0_leave — 启程·0721升空

**文件**：`Resources/cg/cg_day0_leave.png`
**场景**：prologue_02 场景8。0721号从金日成大学停机坪升空，主角在客舱回望校园。
**角色参考**：林彪悍（客舱内，回望姿态）

```
pixel art, 2D game CG illustration, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
departure scene, golden hour, emotional:

SCENE: Deep blue vintage sand-flying vehicle (0721) ascending diagonally from a small concrete landing pad at a Pyongyang university campus. Soviet-style campus buildings with faded murals shrink below. Landing gear still extended, sand particles streaming from exhaust. Through the rear cabin window, a figure looks back at the receding campus. Dashboard screen emits cyan holographic glow.

LAYOUT:
- 0721 ascending lower-left to upper-right, 30% of frame
- Campus buildings in lower-left quadrant, shrinking
- Cloud gaps revealing Pyongyang skyline far below
- Sand particle trail from exhaust, warm amber
- Golden-hour light from right, long shadows on campus

COLORS:
- 0721: navy blue （海军蓝） gold trim
- Campus: concrete grey, faded red murals
- Sky: golden-orange → pale blue gradient
- Sand trail: warm amber
- Holographic glow: cyan （青色）

MOOD: 2,500 km journey begins. A small vehicle against a vast sky. Hope and uncertainty in equal measure.

AVOID:
- No dialogue text, no UI
- Vehicle should look vintage/2053-era, not futuristic
- No other aircraft in frame (alone in the sky)
```

---

#### cg_tea_meet — 初会·嘉颖徐

**文件**：`Resources/cg/cg_tea_meet.png`
**场景**：prologue_02 场景4。大同江茶馆，嘉颖徐递出黑金卡。
**角色参考**：嘉颖徐（坐姿，递卡手势）、林彪悍（背对 viewer，伸手接卡）
**尺寸**：small（居中小插画，半透明底，60%宽）

```
pixel art, 2D game CG illustration, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
tea house meeting, evening, intimate power dynamic:

SCENE: Interior of an upscale Pyongyang tea house. Dark wooden desk, brass tea set with rising steam. Floor-to-ceiling windows show Pyongyang night skyline — Taedong River reflecting city lights. A single calligraphy scroll on the wall: "铁". Two figures across the desk: one extending a matte black card with gold chip, one reaching to receive.

LAYOUT:
- Desk center, tea set foreground, steam wisps
- Figure A (嘉颖徐) center-right, elegant posture, extending card
- Figure B (protagonist) lower-left, back to viewer, reaching
- Night cityscape through windows, bokeh reflections on water
- Warm amber interior lighting, desk lamp casting cone

COLORS:
- Interior: warm wood browns （马鞍棕）, brass gold （古铜色）
- Card: matte black with gold chip detail
- City lights: warm yellow/white on dark water
- Tea steam: soft white wisps
- Overall: amber-warm palette, evening intimacy

MOOD: A young tycoon sizes up the grandson of an old friend. The card is both gift and test. "This isn't a gift — it's a loan."

AVOID:
- No dialogue text
- No modern electronics on desk
- Tea house should feel traditional Korean, not generic Asian
```

---

#### cg_first_night — 客舱·岁月初语

**文件**：`Resources/cg/cg_first_night.png`
**场景**：prologue_02 场景12-13。夜航客舱，岁月投影第一次完整出现，两人初次对话。
**角色参考**：林彪悍（侧坐，面向屏幕）、岁月（投影形态，translucent）
**尺寸**：small（居中小插画）

```
pixel art, 2D game CG illustration, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
night flight, first meeting, holographic presence:

SCENE: 0721 rear cabin interior at night. A figure sits in the left passenger seat, turned toward the dashboard tablet. On the tablet screen, a small translucent holographic projection — a girl sitting cross-legged on the screen edge, glowing cyan-blue. Outside the window: starry sky with scattered sand-vehicle lights like fireflies. Cabin dimly lit by instrument panels.

LAYOUT:
- Figure in left seat, leaning toward screen, warm skin tones
- Hologram on tablet center-right, small but luminous, translucent
- Window showing deep indigo sky, pinprick vehicle lights
- Dashboard instruments: soft green/blue glow
- Cozy cramped cabin, warm shadows on walls

COLORS:
- Cabin: dark wood, navy upholstery, warm shadows
- Hologram: translucent cyan （荧光蓝）, blue core, ethereal
- Window: deep indigo sky （海军蓝）, white/yellow vehicle lights
- Instruments: soft green glow
- Overall: cool blue palette, warm skin accent

MOOD: Two strangers in a flying box at 3,000m. She slept 23 years. He's the first person she's ever talked to. Quiet wonder.

AVOID:
- Hologram must look ethereal/translucent, NOT solid
- No dialogue text
- Cabin should feel cramped and vintage, not spacious
```

---

#### cg_chase — 边境·三面合围

**文件**：`Resources/cg/cg_chase.png`
**场景**：prologue_03 场景。0721遭四家单位三面合围。
**角色参考**：无（0721为远景小像）
**已有提词**：§2.32（完整版，可直接使用）

> 完整提词见 §2.32 边境追击·三面合围（chase_sky · CG）。该提词已包含SCENE/LAYOUT/COLORS/MOOD/EXCLUSIONS全套，可直接用于生成。

---

#### cg_arrest — 边境·引擎盖上

**文件**：`Resources/cg/cg_arrest.png`
**场景**：prologue_03。主角趴在0721引擎盖上被搜查，黄昏。
**角色参考**：林彪悍（趴在引擎盖，脸朝下，双手背后）

```
pixel art, 2D game CG illustration, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
border arrest, tense standoff, purple dusk:

SCENE: 0721 landed on military inspection tarmac. A figure lies face-down on the vehicle's hood, hands behind back. A uniformed officer searches a backpack nearby. Two more officers stand guard with stun batons. Background: green military vehicles with flashing red lights, checkpoint barrier, mountains silhouetted against purple-orange dusk sky. Vehicle engine compartment open, sand-fuel components exposed.

LAYOUT:
- 0721 center, engine hood open, figure face-down on hood
- 3 officers: one searching bag (left), two standing guard (right)
- Military vehicles flanking, red emergency lights
- Mountains and purple-orange dusk sky in background
- Tarmac foreground, grey concrete with oil stains

COLORS:
- 0721: navy blue, dusty from flight
- Officers: dark green （军绿） uniforms
- Tarmac: grey concrete （银灰）
- Sky: purple-orange gradient (紫色 → 橙色)
- Emergency lights: flashing red
- Mountains: dark silhouette （深蓝黑色）

MOOD: 3% chance of escape. Cornered. But somewhere, a phone is ringing — and someone is about to answer.

AVOID:
- No blood, no weapons pointed at figure
- Officers professional, not brutal — this is bureaucracy, not violence
- No dialogue text
```

---

#### cg_village — 雾峰·初见黄昏

**文件**：`Resources/cg/cg_village.png`
**场景**：prologue_04 场景6。空中俯瞰雾峰村，黄昏，0721降落。
**角色参考**：无（纯风景）

```
pixel art, 2D game CG illustration, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
aerial village view, golden hour, mist and memory:

SCENE: Aerial view of a small Chinese mountain village at golden hour. Mist curls between green tea-covered hills. A rusted railway line winds through — tracks overgrown with weeds, station platform paint peeling. Traditional houses with curved tile roofs cluster near the station. Smoke rises from chimneys. In the far distance, a tiny blue vehicle descends through clouds toward the village. The last train stopped here 4 years ago.

LAYOUT:
- Aerial perspective, 30-degree downward angle
- Village center: old station and platform, railway curving through
- Tea terraces on hillsides, green gradients
- Mist in valleys, golden light from left
- Tiny 0721 (blue dot) in upper sky, descending
- Railway line: upper-left to lower-right curve

COLORS:
- Hills: deep green （翠绿色） tea terraces
- Village: warm wood tones, grey tile roofs （建筑灰）
- Railway: rust brown, weed green
- Mist: soft white with golden tinge
- Sky: golden-orange → pale blue
- 0721: small navy blue accent

MOOD: A place time forgot. Beautiful and sad. Someone is coming home to a railway that stopped running 4 years ago.

AVOID:
- No dialogue text
- No modern buildings, no power lines
- Village should feel Chinese mountain, not generic rural
```

---

#### cg_team_night — 旧人·灯下重逢

**文件**：`Resources/cg/cg_team_night.png`
**场景**：prologue_06 场景0。夜晚站台，老陈在灯下介绍团队成员。
**角色参考**：全员6人（老陈、林彪悍、张工、李阿姨、赵师傅、小芳、王小弟）

```
pixel art, 2D game CG illustration, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
night reunion, single lamplight, station platform:

SCENE: Old railway station platform at night. A single flickering fluorescent lamp hangs from the roof, casting a warm cone of light. Seven figures gather in a loose circle under the lamp. One elderly figure (left-center) gestures toward the group, introducing them. The old NF-5K locomotive is dimly visible in the deep background — dark, dormant, massive. Platform edge with rusted rail visible at bottom.

LAYOUT:
- Platform roof with single lamp, center-top, light cone downward
- Elderly figure left-center, gesturing
- Young figure center, facing group
- 5 others arranged in semi-circle right side
- NF-5K locomotive in deep background, silhouette only
- Platform edge with rusted rail at bottom frame

COLORS:
- Lamp: warm yellow-white, cone of light
- Outside light cone: deep blue-black shadows
- Characters: muted earth tones, varied clothing
- Elderly figure: faded blue railway uniform
- Platform: weathered concrete, rust stains
- Background: near-black with locomotive silhouette

MOOD: A ragtag team under one light. The old station feels like a stage. Something is about to begin.

AVOID:
- No dialogue text
- No modern elements (no phones, no LED screens)
- Lamp should feel fluorescent/industrial, not decorative
```

---

#### cg_first_run — 首班车·驶离站台

**文件**：`Resources/cg/cg_first_run.png`
**场景**：prologue_08。SY-22灰雀缓缓驶离站台，首班车。
**角色参考**：林彪悍（站台挥手）、老陈（含泪目送）

```
pixel art, 2D game CG illustration, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
first train departure, morning light, tears and hope:

SCENE: Small green passenger train (3 cars) pulling away from an old mountain station platform. Morning sunlight streams through gaps in the platform roof. Inside windows: passengers — old farmer with basket, child pressing hands against glass, miner dozing. On platform: two figures wave, a few villagers watch from distance. Handmade "首班车" paper banner hangs from station eave. Steam rises from the locomotive at front.

LAYOUT:
- Train moving left to right, lower 40% of frame
- Station platform in foreground, two figures waving
- Mountain backdrop with morning mist
- Paper banner on station eave, center-top
- Steam trail from locomotive rising into golden sky
- Sun rays through platform roof gaps, volumetric

COLORS:
- Train: forest green （鲜绿色） with cream stripe
- Platform: warm wood and concrete
- Morning light: golden shafts
- Mountains: blue-green mist （电蓝）
- Banner: white paper, red handwritten text
- Steam: soft white wisps

MOOD: The first train in 4 years. A line that was dead just came back to life. Tears and smiles. The platform feels like a threshold between past and future.

AVOID:
- No dialogue text
- Train old but functional, not pristine — this is a working machine
- No modern station elements
```

---

#### cg_museum — 铁路博物馆

**文件**：`Resources/cg/cg_museum.png`
**场景**：隐藏CG，好感度>90解锁。松桥站改建的铁路博物馆。
**角色参考**：李阿姨（讲解姿态）、孩子们（听众）

```
pixel art, 2D game CG illustration, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
railway museum, nostalgic preservation, afternoon light:

SCENE: Interior of a small railway museum converted from an old station. Glass display cases: old signal levers, brass conductor's whistle, vintage ticket punches, faded timetables. Model railway diorama in center, illuminated. Walls: black-and-white train photos, framed newspaper clippings. Afternoon light through dusty windows. A figure explains something to a group of children. A single preserved signal shows green.

LAYOUT:
- Museum interior, warm wood paneling
- Display cases along walls, artifacts visible inside
- Model railway in center, lit from above
- Photos and clippings on upper walls
- Figure and children gathered at right side
- Green signal light glowing in corner, vivid accent

COLORS:
- Interior: warm amber wood, soft cream walls （奶油白）
- Display cases: glass with warm interior lighting
- Photos: sepia/black-and-white
- Signal green: vivid glow — the accent color
- Afternoon light: golden through dusty glass
- Overall: warm, nostalgic, museum-quiet palette

MOOD: What was lost is being remembered. The railway's story isn't over — it's being told to a new generation.

AVOID:
- No dialogue text
- Museum small and humble, NOT grand or modern
- No electronic displays — all physical artifacts
```

### 6.3 可选新增（UI 纹理底图，非必需）

| 文件 | 用途 |
|------|------|
| `bg/texture_sleeper.png` | 枕木纹理底图（平铺，对应 UI设计参考_蔚蓝档案.md §8.5） |
| `bg/texture_rail.png` | 轨道透视线底图（平铺） |

### 6.4 已入库（背景图）

`cabin_interior` / `cabin_interior_night` / `car_interior` / `car_interior_night` / `hangar` / `lab` / `professor_office` / `station` / `tea_house` / `shop` / `mountain_sky` / `aerial_view`


---

## 七、已生成资产归档

> 以下prompt已用于生成图片，保留供参考和维护。

### 2.1 实验室（智能调度系统实验室）

**文件**：`Resources/bg/lab.png`  
**用途**：序章 Day 0 开场场景——金日成综合大学智能调度系统实验室

**提示词**：
```
pixel art, 2D background, 1920x1080 resolution, 16:9 aspect ratio, STARDOLL VALLEY STYLE PIXEL ART, university laboratory room, 2076 retro-futuristic design, KOREAN STYLE INTERIOR, Kim Il Sung University, Pyongyang, North Korea, ULTIMATE KOREAN CULTURAL ELEMENTS:

KOREAN ARCHITECTURAL ELEMENTS:
- Traditional KOREAN HANOK STYLE window frames with wooden lattice patterns
- Door frame with traditional Korean decorative patterns (단청 dancheong colorful patterns)
- Wall panels with subtle Korean traditional motifs
- Ceiling with traditional Korean wooden beams (modern interpretation)
- Korean-style floor heating (온돌 ondol) visible under desk

KOREAN TEXT AND SIGNS:
- Large Korean text on wall: "지능형 지휘 시스템 연구실" (Intelligent Dispatch System Research Lab)
- University emblem with Korean text: "김일성대학교"
- Korean flag (North Korean flag) prominently displayed
- Korean propaganda slogan on wall: "일심단결" (Single-hearted Unity) or "주체사상" (Juche Idea)
- Korean calendar on wall showing 2076 year with traditional Korean holidays marked
- Small Korean text labels on all equipment and furniture

WALL DECORATIONS (KOREAN LEADERS AND CULTURE):
- Large North Korean flag (clean, well-maintained)
- FRAMED PORTRAITS OF LEADERS on wall (金日成 and 金正日 portraits, side by side, respectful placement, formal frames)
- Vintage Korean railway poster with Korean text
- Map of Korean railway network with Korean labels
- Traditional Korean paper art (minhwa) frame with folk painting
- Korean traditional fan (bukcheong fansa) on wall
- Korean calligraphy artwork (한글 Hangeul calligraphy)

KOREAN TRADITIONAL ITEMS:
- Traditional Korean celadon pottery on shelf (고려청자 Goryeo celadon, green ceramic)
- Korean embroidered wall hanging with traditional pattern (자수 jasu)
- Korean traditional paper (한지 hanji) lamp on desk
- Small Korean flag on desk
- Korean traditional tea set (전통다구) with teapot and cups
- Korean traditional fan (부채 bukcheong fansa) on wall
- Korean traditional knotwork (매듭 maedeup) decoration

KOREAN FOOD AND DRINK ITEMS:
- Kimchi jar (김치단지) on shelf (traditional fermentation crock)
- Korean traditional rice bowl (밥그릇) on desk
- Korean tea canister (차통 chatong) with traditional design
- Small soju bottle (소주병) on shelf (optional, for atmosphere)
- Korean traditional snack container (과자그릇)

KOREAN MUSICAL INSTRUMENTS:
- Small Gayageum (가야금) model on shelf (12-string zither)
- Korean traditional drum (북 buk) as decoration
- Korean flute (대금 daegeum) on wall mount

KOREAN FURNITURE:
- Korean traditional wooden cabinet (장롱 jangnok) with brass fittings
- Korean-style desk with traditional joinery
- Korean traditional chair with curved backrest

KOREAN TECHNOLOGY (2076 RETRO-FUTURISTIC):
- "Chollima" brand computer (천리마 컴퓨터) - Korean retro computer design
- Korean-made monitor with Hangul keyboard
- Traditional Korean-style power outlet covers
- Korean-designed desk lamp with traditional patterns

WINDOW VIEW (PYONGYANG SKYLINE):
- Iconic PYONGYANG LANDMARKS visible: Ryugyong Hotel (려명거리 Hotel, pyramid shape)
- Juche Tower (주체사상탑) visible in distance
- Grand People's Study House (인민대학습당) with traditional Korean roof
- Mix of traditional Korean architecture and modern skyscrapers
- Korean-style rooflines on some buildings (기와 giwa tiles)
- Flying vehicles in distance (sand energy cars)
- Mountains with Korean pine trees (소나무 sonamu) in far distance
- Taedong River (대동강) visible if applicable

DESK ITEMS WITH KOREAN TOUCH:
- Korean traditional tea set (전통다구) with celadon teapot
- Korean calligraphy brush (붓 but) and ink stone (벼루 byeoru)
- Korean traditional notebook (한지공책) with Korean binding
- Copper pocket watch (grandfather's inheritance)
- Open railway engineering manual (Korean text visible)
- Korean-style pen holder with traditional design
- Small Korean traditional clock (시계) with hanja numbers

COLOR PALETTE (KOREAN TRADITIONAL):
- Primary: Korean celadon green （青瓷绿）, warm brown （马鞍棕）
- Secondary: Cream white （奶油白）, soft red （柔红）
- Accents: Gold （丹青金）, navy blue （藏蓝）
- Dancheong colors: Blue, Red （柔红）, Yellow （丹青金）, Green （丹青绿）
- Inspired by Korean traditional color schemes and dancheong patterns

ATMOSPHERE:
- KOREAN WARMTH AND HOSPITALITY (정 warm heart)
- Academic yet cultural
- Blend of tradition and modernity
- Respectful of heritage and leaders
- Hopeful for railway revival
- Strong national identity

STYLE:
- PIXEL ART with clear pixels
- KOREAN CULTURAL IDENTITY very prominent
- Warm, inviting colors
- Clean, organized space
- Mix of old and new (retro-futuristic)
- Traditional Korean patterns and motifs throughout

AVOID:
- Generic "Asian" style (must be specifically KOREAN)
- Too modern/sterile (should have traditional touches)
- Cold or impersonal atmosphere
- Cluttered or messy composition
- Disrespectful placement of leader portraits
- Missing Korean cultural elements
```

---


---

### 2.2 导师办公室

**文件**：`Resources/bg/professor_office.png`  
**用途**：序章 Day 0 导师办公室场景——林彪悍向导师申请学业暂停

**提示词**：
```
pixel art, 2D background, 1920x1080 resolution, 16:9 aspect ratio, STARDOLL VALLEY STYLE PIXEL ART, university professor's office, 2076 retro-futuristic design, KOREAN STYLE INTERIOR, Kim Il Sung University, Pyongyang, North Korea, modest academic setting, ULTIMATE KOREAN CULTURAL ELEMENTS, HEAVY NORTH KOREAN ATMOSPHERE:

KOREAN ARCHITECTURAL ELEMENTS:
- Traditional KOREAN HANOK STYLE window frames with wooden lattice patterns
- Door frame with traditional Korean decorative patterns (단청 dancheong)
- Ceiling with traditional Korean wooden beams, green dancheong accents
- Korean-style floor heating (온돌 ondol) visible under desk

KOREAN TEXT AND SIGNS (HEAVY EMPHASIS):
- Large Korean text on wall: "김일성대학교 교수 연구실" (Professor's Office)
- University emblem with Korean text: "김일성대학교" prominently displayed
- North Korean flag prominently on wall, well-lit
- Framed portrait of Kim Il Sung and Kim Jong Il on wall, side by side
- Propaganda slogan: "일심단결" (Single-hearted Unity) calligraphy plaque
- Korean calendar on wall showing 2076
- Nameplate on desk with professor's name in Korean
- Small Korean text labels on equipment

WALL DECORATIONS (ACADEMIC + KOREAN LEADERS):
- Framed university diploma with Korean text, government seal
- Academic certificates and awards on wall (Korean text, official stamps)
- Map of Korean peninsula on wall showing "Unified Korea"
- Korean calligraphy scroll (서예) with inspirational message
- Vintage Korean railway poster from 1950s Chollima era
- Group photo of faculty with leader (formal, framed)
- Traditional Korean paper art (minhwa) frame with folk painting

KOREAN TRADITIONAL ITEMS:
- Korean celadon pottery (고려청자 Goryeo celadon) on shelf
- Korean traditional paper (한지 hanji) desk lamp with warm glow
- Small Korean flag on desk in brass stand
- Korean traditional tea set (전통다구) with celadon teapot
- Korean traditional ink stone (벼루) and brush (붓) on desk
- Korean traditional knotwork (매듭 maedeup) decoration on shelf
- Korean traditional fan (부채 bukcheong fansa) on wall

KOREAN FURNITURE:
- Korean traditional wooden desk with traditional joinery, slightly worn
- Professor's wooden chair with curved backrest and cushion
- Korean traditional wooden cabinet (장롱 jangnok) with brass fittings
- Bookshelf with traditional Korean design

DESK ITEMS:
- Stack of student research papers with Korean text
- Open railway engineering textbook (Korean text, "철도공학")
- Copper reading glasses on desk
- Korean traditional clock (시계) with hanja numbers
- Half-empty cup of Korean barley tea (보리차)
- Chollima brand calculator (천리마 계산기) on desk
- Photo frame with university graduation photo

KOREAN TECHNOLOGY (2076 RETRO-FUTURISTIC):
- Chollima brand computer (천리마 컴퓨터) with transparent OLED display
- Korean-made holographic projection device on desk
- Virtual lecture screen with Korean text on wall
- Korean-designed desk lamp with traditional patterns, wireless charging
- Smart communication device with Hangul interface
- AI assistant interface on computer screen (Korean language)

BOOKSHELF CONTENTS:
- Railway engineering textbooks (Korean titles, "철도수송론")
- Research papers on intelligent dispatch systems
- Korean history books (조선력사)
- Kim Il Sung and Kim Jong Il collected works (red hardcover, prominent)
- Juche ideology study materials
- Vintage Korean steam locomotive model
- Traditional Korean ceramic pieces

WINDOW VIEW (PYONGYANG SKYLINE):
- Ryugyong Hotel (려명거리 Hotel, pyramid shape, distant)
- Juche Tower (주체사상탑) visible in distance with iconic flame
- Grand People's Study House (인민대학습당) with traditional Korean roof
- Korean pine trees (소나무 sonamu) visible
- Afternoon daylight, soft shadows through hanji-patterned windows
- Flying sand energy vehicles in distant sky

COLOR PALETTE (KOREAN TRADITIONAL + NORTH KOREAN OFFICIAL):
- Primary: warm brown （马鞍棕）, cream white （奶油白）
- Secondary: dark wood, soft beige
- Accents: gold frame （丹青金）, revolutionary red
- Dancheong: Blue, Red （柔红）
- North Korean official red for banners and flags

ATMOSPHERE:
- Warm, academic, modest but respected
- KOREAN WARMTH AND HOSPITALITY (정 warm heart)
- Serious yet caring — moment of a life-changing decision
- OFFICIAL NORTH KOREAN ACADEMIC SETTING
- State professor's office at the most prestigious university in the DPRK
```

---


---

### 2.3 驾驶舱·白天（0721号前舱）

**文件**：`Resources/bg/car_interior.png`  
**用途**：0721号前舱——岁月主控区，主角偶尔进入。朝鲜官方标配 × 岁月全息系统。23年未变，如博物馆般被保存。

**⚠️ 前两版图片问题总结（新版提示词已针对性修复）：**
1. 最终版选用无方向盘方案 → 操作面板 + 按钮/开关代替
2. 韩文有乱码 → 新版强调"ALL Korean text must be correct Hangul, no garbled text"
3. 领袖肖像不够像 → 新版强调"brass frames, symmetrical, immediately recognizable"
4. 座椅比例失调 → 新版明确座位尺寸和位置
5. 背景风格不一致 → 新版统一像素风
6. 驾驶舱是官方空间，不应有韩式消费品 → 移到客舱

**⚠️ 设计说明：0721号无方向盘。驾驶舱使用操作面板 + 全息控制系统。岁月AI全权操控飞行，物理控制面板是23年前的原始设计，积灰未用。**

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
flying vehicle COCKPIT interior, daytime, 2076 retro-futuristic,
NORTH KOREAN OFFICIAL AESTHETIC — max density,
"Chollima" brand flying car, "Sand Flying Pig" 0721:

SCENE: front cockpit of a DPRK-made sand-energy flying car — two seats, CONTROL PANEL with buttons and toggle switches, holographic dashboard, leader portraits in brass frames. This is the ORIGINAL 2053 interior, preserved like a museum: 23 years untouched, every official detail intact. The cockpit is a time capsule of DPRK industrial design. NO STEERING WHEEL — the vehicle is fully autonomous, controlled by the AI system 岁月.

CULTURAL CONTEXT — SIX LAYERS OF KOREANNESS:
- LAYER 1: Chollima brand — DPRK's premier vehicle manufacturer, named after the mythical thousand-li horse
- LAYER 2: Kim Il Sung University affiliation — university emblem, research institute stickers
- LAYER 3: Juche ideology design language — self-reliance aesthetic, practical and functional
- LAYER 4: Songun (military-first) influence — sturdy construction, military-grade switches
- LAYER 5: Korean traditional arts — dancheong color patterns, maedeup knotwork, minhwa folk motifs
- LAYER 6: 2076 retro-futuristic — not too advanced, recognizable as Korean-made, slight Soviet-tech influence

COCKPIT LAYOUT:
- Pilot seat (left) and passenger seat (right), dark olive-green upholstery, DPRK red star emblem on headrests
- NO STEERING WHEEL — replaced by a control panel with physical toggle switches and push buttons (military-grade, Korean-made, 23 years untouched)
- Holographic display screen in center of dashboard (岁月's navigation interface, blue-tinted, showing map and flight data)
- Center console with sand-energy controls, navigation system, and propaganda radio
- Side windows on both sides showing sky and clouds (daytime, bright)
- Ceiling: padded headliner with small reading light, emergency handle
- Floor: dark rubber mat with raised ridges, utilitarian

NORTH KOREAN OFFICIAL DETAILS — MAXIMUM DENSITY:
PATRIOTIC SYMBOLS:
- Framed portraits of Kim Il-sung and Kim Jong-il (brass frames, mounted on wall above dashboard, prominent) — ESSENTIAL
- Small Korean flag (태극기) on dashboard, another on rearview mirror
- "위대한 령도자 김일성동지" (Great Leader Comrade Kim Il-sung) calligraphy plaque
- "100전 100승" (100 battles 100 victories) slogan sticker
- Red star emblem on seat headrests

TRADITIONAL KOREAN CRAFTS:
- Korean traditional knotwork (매듭 maedeup) in gold and red hanging from rearview mirror
- Dancheong-style color pattern (오방색 obangsaek: blue, red, yellow, white, black) on seat fabric trim
- Small minhwa folk painting (민화) of a tiger and magpie as decorative panel
- Celadon green (청자) color accent on dashboard trim

TECHNOLOGY & CONTROLS:
- ALL labels in Korean (Hangul) — absolutely no English or Chinese
- Sand energy gauge: "모래 에너지 잔량" with sweeping needle, red zone at low
- Speedometer: "속도" with km/h markings in Hangul numerals
- Altitude display: "고도" with Hangul numerals
- Navigation screen: holographic map of Korean peninsula
- Radio: frequency dial with Korean station names (조선중앙방송, 평양FM)
- Warning stickers: "주의" (caution), "비상정지" (emergency stop)
- Engine temperature: "엔진 온도" with green-yellow-red zones
- "비행중" (in flight) sign above cockpit door, illuminated
- Control panel: physical toggle switches, push buttons, analog gauges — all Korean-made, military-grade, slightly dusty

CONTRAST ELEMENTS (HUMOROUS, SECONDARY — SHOWING THE PASSAGE OF TIME):
- "CASS" beer can wedged in door pocket (empty)
- Half-empty soju bottle (처음처럼) in side storage
- Seoul travel guide magazine on passenger seat (slightly dog-eared)
- University ID card (김일성대학교 학생증) clipped to sun visor
- A K-pop sticker on the control panel (partially peeling, the only splinter of modern culture in the official space)

WINDOW VIEW (DAYTIME):
- Bright blue sky with white clouds through windshield
- Mountainous landscape below — Korean-Chinese border region
- A few Chollima-brand sand-energy flying vehicles in distant sky
- Warm sunlight streaming through side windows
- Rice paddies and small villages visible far below

COLOR PALETTE:
- Primary: dark olive green, instrument panel gray
- Secondary: sky blue （天蓝色）, holographic cyan （亮天蓝）
- Accents: Korean red, celadon green, traditional gold
- Obangsaek five colors: blue （宝蓝色）, red, yellow （丹青金）, white, black （深灰黑）
- Leather: dark brown

ATMOSPHERE:
- PRESERVED MUSEUM PIECE — the cockpit as it was in 2053, untouched for 23 years
- The contrast between official propaganda decor and casual pop-culture leftovers tells a story
- Warm daylight, quiet and calm
- Compact but functional, every inch has a purpose
- Slight Soviet-industrial undertone blended with Korean tradition

STYLE:
- PIXEL ART with clear pixels, STARDEW VALLEY style
- NORTH KOREAN OFFICIAL AESTHETIC — unmistakably DPRK government design
- Every label and text in Korean (Hangul) — zero English
- Propaganda-era design blended with near-future technology
- Military-grade build quality visible in switches and materials

AVOID:
- Steering wheel (the vehicle has NO steering wheel — use control panel with buttons/switches instead)
- Generic sci-fi interior (must be unmistakably NORTH KOREAN)
- Japanese or Chinese cultural elements
- English text anywhere
- Modern minimalist aesthetic
- Missing the leader portraits (ESSENTIAL)
- Garbled or nonsensical Korean text (all Hangul must be correct)
- Art style inconsistency between interior and exterior
- Seat proportions that are too small or cut off awkwardly
- Clean/sterile interior (must feel preserved but slightly aged with 23 years of dust)
- K-pop or South Korean cultural items in the cockpit (they belong in the CABIN, not the cockpit — the cockpit is the OFFICIAL space)
```

---


---

### 2.4 驾驶舱·夜晚（0721号前舱）

**文件**：`Resources/bg/car_interior_night.png`  
**用途**：0721号前舱——夜晚飞行。使用图片 `ChatGPT Image 2026年8月17日 13_26_49.jpg`。无方向盘，完全靠全息控制系统飞行。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
flying vehicle interior at night, 2076 retro-futuristic,
KOREAN STYLE INTERIOR, NORTH KOREAN industrial design,
sand-energy vehicle cockpit nighttime, "Chollima" brand flying car:

SCENE: same DPRK-made sand-energy flying vehicle interior as daytime version, but at night — dark ambient lighting, instrument panel glow in amber and blue, stars visible through windows, intimate nighttime atmosphere

NIGHTTIME DIFFERENCES — SAME KOREAN INTERIOR, NOW AT NIGHT:
- Dark ambient lighting throughout cabin — only instrument panel and holographic display provide illumination
- Windows show dark night sky with stars, crescent moon, and distant clouds
- City lights of Pyongyang (or similar Korean city) visible far below — distinctive grid pattern
- Dashboard instruments glow softly in warm amber (analog gauges) and cool blue (digital displays)
- Holographic display (岁月's interface) casts cool blue light on cabin interior surfaces
- Small reading light above passenger seat — warm yellow incandescent glow
- Stars, moon visible through windshield — clear night sky
- Red-tinted "비행중" (in flight) sign above cockpit door

KOREAN CULTURAL ELEMENTS AT NIGHT — ALL PRESERVED:
- Korean flag (태극기) still visible in dim light on dashboard
- Leader portrait (Kim Il-sung, Kim Jong-il) subtly illuminated by gauge light
- Maedeup knotwork silhouette visible against window
- Dancheong pattern on seat fabric catching holographic blue light
- ALL labels in Korean (Hangul) — illuminated by instrument glow
- Sand energy gauge (모래 에너지 잔량) needle glowing amber
- Navigation map (조선반도) casting soft blue light
- Roadong newspaper visible in door pocket, dimly lit

COLOR PALETTE (NIGHT — KOREAN TRADITIONAL + DARK):
- Primary: deep dark blue （深蓝黑）, instrument glow amber （深橙色）
- Secondary: starry sky （靛蓝夜空）, holographic cyan （亮天蓝）, city lights warm （丹青金）
- Accents: Korean red on emergency labels, celadon green on dashboard trim
- Obangsaek five colors muted in darkness: blue （宝蓝色）, red, yellow （古铜色）

ATMOSPHERE:
- Quiet, intimate nighttime atmosphere — unmistakably Korean
- Sense of traveling through the dark over Korean landscape
- Peaceful and contemplative mood
- Stars and distant city lights create a sense of vast journey
- Safe and warm inside despite the dark outside
- Instrument glow creates a cozy cockpit environment

STYLE:
- PIXEL ART with clear pixels, STARDEW VALLEY style
- NORTH KOREAN TECH AESTHETIC at night
- Dark ambient lighting with warm instrument glow
- Nighttime atmosphere throughout
- Every label and text in Korean (Hangul)

AVOID:
- Too bright (must be clearly nighttime)
- Generic sci-fi night interior
- Missing Korean text on instruments (all Hangul must be correct, no garbled text)
- Japanese or Chinese cultural elements
- English text anywhere
- Missing leader portraits
- Art style inconsistency between interior and exterior
- K-pop or South Korean items in the cockpit (they belong in the cabin)
```

---


---

### 2.5 客舱·白天（新绘—0721号后舱）

**文件**：Resources/bg/cabin_interior.png  
**用途**：0721号后舱——主角旅途主要活动空间，从客舱门上车，第一眼看到的就是这里

**设计说明**：0721号只有一个侧门，打开就是客舱。驾驶舱在前面（通过隔断门进入），但主角几乎不去。后舱布局像小型火车包厢（不是汽车后座），沿墙长椅可放平成床，中央矮桌，大窗，地板下储物。无独立厕所，中途在补给站解决。

**提示词**：
`
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
flying vehicle PASSENGER CABIN interior, daytime, 2076 retro-futuristic,
NORTH KOREAN VEHICLE INTERIOR — like a small train compartment,
"Sand Flying Pig" 0721 rear cabin:

SCENE: rear passenger cabin of a small DPRK-made sand-energy flying vehicle — NOT a car back seat. Layout like a KOREAN TRAIN COMPARTMENT: bench seat along one wall, low table, panoramic window, underfloor storage. This is where the protagonist lives during the 4.5-day journey. The main vehicle door opens into this space — the cockpit is through a separate door forward.

CABIN LAYOUT:
- Bench seat along left wall (cushioned, beige/cream, dark green piping) — like a Korean train compartment seat, can fold flat to form a sleeping platform
- Small low table in center (like a Korean 밥상, 40cm high) — for eating / laptop / writing
- Floor is raised platform with underfloor storage compartments (like Korean heated floor ondol style)
- Large panoramic window on right wall — SKY ONLY, NO GROUND
- Small fold-down jump seat on opposite wall (for when bench is in bed mode)
- Overhead shelf running full length for luggage
- Reading light on wall above bench (warm yellow, adjustable)
- Sliding door at front leading to cockpit (simple, utilitarian, DPRK industrial style)
- Main vehicle door visible on right side of cabin (the way you enter)
- No toilet — vehicle too small, pit stops at supply stations

NORTH KOREAN BASE AESTHETIC:
- Dark green/olive walls and ceiling (standard DPRK color — like Pyongyang metro trains)
- Dancheong-patterned stripe along wall-ceiling joint (subtle, traditional)
- "금연" (No Smoking) sign in Korean on wall
- KIM IL SUNG UNIVERSITY emblem on cabin wall
- Chollima (천리마) brand emblem above cockpit door
- Official vehicle ID plaque on wall
- Functional, utilitarian — DPRK industrial standard, Soviet-railway influence

PERSONAL ITEMS (ACCUMULATED DURING JOURNEY):
- Open laptop on low table, dispatch algorithm code on screen
- Notebook and pen next to laptop
- KOREAN FRIED CHICKEN box (BBQ 치킨) on table, some crumbs
- Half-empty soju bottle (처음처럼) and one small cup
- Shrimp chips (새우깡) bag, banana milk (바나나우유) bottle
- Seoul travel guide propped against wall
- Duty-free shopping bag in corner
- K-pop photocards tucked into wall shelf
- Folded blanket and small pillow on bench
- Phone charger draped across table
- Water bottle on floor near table

WINDOW VIEW (DAYTIME — SKY ONLY):
- Bright blue sky with white clouds
- Occasional distant flying vehicle
- Warm sunlight streaming through window
- NO GROUND, NO MOUNTAINS, NO CITIES

COLOR PALETTE:
- Primary: warm beige, olive green, dark window frame （暗影色）
- Secondary: sky blue （天蓝色）, laptop screen glow
- Accents: food orange （深橙色）, soju green, banana milk yellow （奶油白）, Korean red

ATMOSPHERE:
- Cozy like a small train compartment on a long journey
- North Korean rail/vehicle design made personal by the occupant
- Warm daylight, comfortable for 4.5 days
- NOT a car back seat — this is a small living space
- The clutter tells a story: a student traveling home

STYLE:
- PIXEL ART with clear pixels, STARDEW VALLEY style
- DPRK VEHICLE INTERIOR — inspired by Pyongyang metro and Korean train compartments
- Every label in Korean (Hangul)
- Lived-in, not sterile

AVOID:
- Car back seat layout (this is a FLYING VEHICLE, not a car)
- Forward-facing seats like a car
- Car door handles or car door panels
- Ground or landscape visible through window
- Luxury or premium interior
- English text
- Japanese or Chinese elements
"


---

### 2.6 客舱·夜晚（新绘—0721号后舱）

**文件**：Resources/bg/cabin_interior_night.png  
**用途**：0721号后舱——夜晚飞行，主角休息

**提示词**：
`
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
flying vehicle PASSENGER CABIN at night, 2076 retro-futuristic,
NORTH KOREAN VEHICLE INTERIOR at night, cozy train-compartment style:

SCENE: same rear cabin as daytime — but at night. Reading light on, bench folded into bed mode with blanket and pillow. Table cleared except for a half-empty soju bottle. Window shows starry sky. Warm yellow reading light creates a cozy, intimate atmosphere. The protagonist is asleep or resting.

NIGHTTIME DIFFERENCES:
- Cabin dark except for reading light (warm yellow, wall-mounted above bench)
- Bench folded flat into bed mode with blanket and pillow
- Table cleared of food — only soju bottle and one cup remain
- Window shows dark night sky with stars and crescent moon — NO GROUND
- Laptop closed, charging light faintly glowing
- Snack bags dimly visible in overhead shelf
- Duty-free bag silhouette in corner
- Phone charger plugged in, faint blue charging light
- Cockpit door slightly ajar, faint blue glow from holographic display beyond

COLOR PALETTE (NIGHT):
- Primary: warm amber, dark blue
- Secondary: starry sky （靛蓝）, reading light warm （丹青金）
- Accents: laptop sleep light （亮天蓝）, charging LED

ATMOSPHERE:
- Quiet, intimate, restful — like a sleeper train cabin at night
- End of a long travel day
- Safe and warm inside despite the dark outside
- Contemplative — protagonist rests while 岁月 flies on through the night

AVOID:
- Too bright (must be clearly nighttime)
- Car back seat layout
- Ground or city lights visible through window
- English text
```

---


---

### 2.7 停机坪

**文件**：Resources/bg/hangar.png  
**用途**：序章 Day 0 领取载具——金日成综合大学楼顶停机坪，午后

**剧情对应**：林彪悍走向0721号——这是他作为荣誉研究生的专属载具，一辆2053年制造的第一代试验车，沉睡23年，搭载了AI原型“岁月”。车门感应到有人靠近自动打开。楼顶一侧，一名穿着深色制服的车队管理员站在车辆旁，正在做最后的交接检查——他不是导师，而是负责维护和保管这辆封存车辆的大学后勤人员。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
flat forward-facing perspective, eye-level view,
university rooftop helipad, Kim Il Sung University, Pyongyang, 2076, afternoon,
NORTH KOREAN OFFICIAL AESTHETIC,
"Chollima" brand sand-energy flying vehicles parked:

SCENE: rooftop helipad of Kim Il Sung University's main building in the afternoon, seen from the protagonist's eye level — standing on the rooftop looking forward. The deep blue 0721 Sand Flying Pig is parked prominently in the foreground facing RIGHT. This is a first-generation prototype from 2053, 23 years old, slightly worn. The cabin door on the RIGHT side of the vehicle is closed but will open when the protagonist approaches. The rooftop is large and spacious. A University maintenance officer in a dark uniform (Mao-style tunic, red lapel pin) stands beside the vehicle, clipboard in hand, making final checks — not a professor, but the depot keeper who has maintained this dormant vehicle for years. The rooftop is large and spacious. No city skyline visible below — the rooftop edge is at the bottom of the frame. The sky fills the upper two-thirds of the image.

LAYOUT:
- Large, spacious rooftop landing pad taking the lower third of the frame
- 0721 Sand Flying Pig parked in foreground, facing RIGHT, deep blue, university emblem on side
- Vehicle seen from the RIGHT side — the cabin door is visible on this side, open, with a boarding ramp/gangway extended
- The vehicle's surface reflects the warm afternoon sunlight
- 2-3 other Chollima sand-energy vehicles parked nearby in background (dark green, grey)
- University building facade visible on the left edge — Korean-style railings, dancheong roof eaves
- Kim Il-sung University emblem on the building wall
- Control booth / small structure on the rooftop with communication equipment
- Korean flag (태극기) on a pole on the rooftop, slightly waving
- The rooftop edge is at the bottom of the frame — the protagonist cannot see the ground below

NORTH KOREAN CULTURAL ELEMENTS:
- "김일성종합대학" (Kim Il Sung University) emblem on building wall
- "위대한 수령 김일성동지 만세" calligraphy on building facade
- Red star emblem on control booth
- Korean traditional dancheong pattern on roof eaves
- Korean flag (태극기) on a pole on the rooftop
- Juche-era architectural style (Soviet-Korean brutalist mixed with traditional elements)
- "천리마" (Chollima) branding on the vehicles

VIEW (SKY — NO GROUND VISIBLE):
- Wide afternoon sky taking up the upper two-thirds of the frame
- Bright blue sky with white clouds, warm afternoon light
- A few flying vehicles silhouetted against the sky
- No buildings below — the protagonist standing on the rooftop sees only sky and distant flying vehicles
- The rooftop is high enough that the city is hidden below the frame edge

DETAILS ON THE 0721 SAND FLYING PIG (RIGHT SIDE VIEW):
- Deep blue paint with gold university emblem
- Chollima winged horse badge on the side
- Vehicle ID "0721" in Korean numerals
- Slightly aged — 23 years old, some wear on the paint
- Cabin door on the right side, closed with a visible seam — a boarding ramp/gangway extends down to the rooftop
- The door is closed, but the vehicle feels alive — 23 years of waiting
- The vehicle is CLOSED (not open-top) — the roof is solid, no open hatch
- The cockpit is dark inside through the windshield
- Sand energy intake vents on the sides
- Running lights currently off (vehicle dormant)
- Vehicle faces RIGHT, door on RIGHT side

COLOR PALETTE:
- Primary: bright blue sky （天蓝色）, deep blue vehicle (藏蓝色, 海军蓝)
- Secondary: university emblem gold, concrete grey （银灰）
- Accents: Korean red, building white, roof tiles dark brown （深棕）
- Landing pad: concrete grey (银灰色, 建筑灰色)
- Building: traditional roof tiles dark brown （深棕）

ATMOSPHERE:
- Afternoon on a university rooftop - the last moment of campus life
- Sense of anticipation: a journey is about to begin
- Quiet and calm on the rooftop
- The warm light from the open cabin door contrasts with the cooling evening sky
- The vehicle is waiting, ready, door open — inviting the protagonist in
- Slight wind suggested by Korean flag movement

STYLE:
- PIXEL ART with clear pixels, STARDEW VALLEY style
- FLAT FORWARD-FACING PERSPECTIVE — eye level, not looking down
- NORTH KOREAN ARCHITECTURAL AESTHETIC — unmistakably DPRK university
- Every label in Korean (Hangul)
- Warm afternoon light, gentle shadows

AVOID:
- Looking down at the ground below (the protagonist is on the rooftop, not above it)
- Open-top or convertible vehicle (the 0721 is CLOSED)
- Vehicle door on the left side (the door is on the RIGHT side)
- Vehicle facing left (it faces RIGHT)
- Generic sci-fi cityscape
- English text
- Missing the Korean flag or university emblem
- Too dark or sunset-heavy (it is afternoon, not evening)
- Too small rooftop (must feel spacious)
```

---


---

### 2.12 大同江茶馆 / 嘉颖徐茶室兼办公室（tea_house）✅ 标杆定稿

**文件**：`Assets/Resources/bg/tea_house.png`  
**规格现状**：1536×1024（1.5:1，序章 `prologue_02_day0`、`prologue_03_journey` 引用）。  
**用途与设定定性**：**嘉颖徐（徐会长）的大同江畔奢华茶室兼私人办公室**（用户 2026-10-10 明确定性：「这个就是嘉颖徐的茶室兼办公室」）。融合了朝鲜顶级阶层的奢华红木装饰、水晶吊灯、青瓷茶具与大同江畔主体思想塔璀璨夜景。  
**资产状态**：✅ **标杆定稿合格资产**（画面完全契合会面场景设定，予以保留）。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
executive office interior, Pyongyang, 2076, afternoon,
NORTH KOREAN PRIVATE ENTERPRISE AESTHETIC — luxury inside a socialist city:

SCENE: A grand executive office in Pyongyang's Gangnam District. A massive dark mahogany desk dominates the center, with a high-backed burgundy leather chair behind it. Floor-to-ceiling glass-fronted bookshelves line the back and right walls. A golden Chollima winged horse statue stands on a pillar at right foreground. Through large windows on the left, the Pyongyang night skyline is visible with the Juche Tower lit up. This is the office of a railway tycoon — a space that blends traditional Korean elegance with corporate power.

LAYOUT:
- Large executive desk (dark mahogany, carved arched panels, gold trim) — center
- High-backed burgundy leather chair behind the desk
- Korean celadon tea set (청자 teapot + cups) on the desk — center
- Green banker's desk lamp on the desk — left side
- Nameplate with Korean text ("기업의 주체는 나!") — front of desk
- Floor-to-ceiling glass-fronted bookshelves covering back and right walls
- Densely packed with books, celadon vases, framed photos
- Large windows on left wall showing Pyongyang night skyline
- Juche Tower visible through the window with red star illuminated
- Ornate metal balcony railing visible outside the window
- 천리마 (Chollima) pedestal column at right foreground
- Golden winged horse statue on top of the pedestal
- Side table at left foreground with purple orchid in celadon vase
- Stacked 로동신문 (Rodong Sinmun) newspapers on side table
- Elaborate coffered ceiling with crystal chandelier
- Wall sconces providing ambient side-lighting

KOREAN CULTURAL ELEMENTS:
- "100전 100승" (100 battles, 100 victories) calligraphy scroll on wall
- "기업의 주체는 나!" (The master of the enterprise is me!) desk nameplate
- "천리마" (Chollima) on the pedestal column
- Celadon (청자) tea set — distinctly Korean ceramic style
- 로동신문 (Rodong Sinmun) — North Korea's state newspaper
- Purple orchid in celadon vase — traditional Korean elegance
- Golden winged horse (Cheollima) — mythical Korean symbol

COLOR PALETTE:
- Primary: deep mahogany brown （深棕）, dark wood, gold trim
- Secondary: window night sky （海军蓝）, warm amber light （丹青金）
- Accents: celadon green, burgundy leather, orchid purple, Korean red

ATMOSPHERE:
- Warm, authoritative, a seat of power
- Intimate evening lighting from chandelier and desk lamp
- Dramatic contrast between golden interior and cool blue night cityscape
- Private, opulent, slightly imposing
- A space for high-stakes business conversations

STYLE:
- PIXEL ART with clear pixels, STARDEW VALLEY style
- NORTH KOREAN PRIVATE SECTOR — luxury within a socialist system
- Every label in Korean (Hangul)
- Warm evening lighting, rich textures

AVOID:
- Generic Chinese or Japanese office
- Official government building feel
- Western-style corporate office
- Too bright or sterile
- Modern minimalist aesthetic
- Missing the Chollima statue or Korean text elements


---

### 2.13 Wiki 横幅（Wiki Banner）

**文件**：`images/wiki_banner.png`  
**用途**：Wiki 中文/英文主页顶部横幅，替代实验室背景图

**提示词**：
```
pixel art, wide banner, 1920x400 resolution, 16:9 aspect ratio, STARDOLL VALLEY STYLE PIXEL ART, panoramic landscape, 2076 retro-futuristic setting, KOREAN STYLE, Pyongyang, North Korea, ULTIMATE KOREAN CULTURAL ELEMENTS, banner with game title text:

GAME TITLE INTEGRATION (BILINGUAL):
- Large Korean/Hangeul text in banner: "철도 르네상스: 모래 에너지 충격" (Railway Renaissance: Sand Energy Impact)
- Smaller English subtitle: "Railway Renaissance: Sand Energy Impact"
- Korean text: "김일성종합대학" (Kim Il Sung University) as location label
- Game title positioned in upper or center area, styled like a retro game title banner

SCENE COMPOSITION (panoramic, wide):
- Misty mountain valley with a railway line cutting through
- Traditional Korean-style train station (hanok roof, giwa tiles) on the left
- Small steam locomotive (NF-5 style) pulling carriages across the scene
- Pyongyang skyline silhouettes in distant background: Ryugyong Hotel, Juche Tower
- Korean pine trees (소나무 sonamu) in foreground framing the scene
- Flying sand energy vehicles (Chollima brand) in distant sky, small scale
- Morning mist between mountains, soft golden sunlight

KOREAN ARCHITECTURAL ELEMENTS:
- Traditional Korean hanok roof lines on station building
- Dancheong colorful patterns (단청) on station eaves
- Korean stone pagoda (석탑) as landscape accent
- Traditional Korean fence (울타리) along the railway

KOREAN TEXT AND SIGNS:
- Station sign with Korean text: "철도 르네상스" (Railway Renaissance)
- Korean direction signpost: "평양 ← 2500km / 중국 →"
- Small Korean flag on station building
- Korean propaganda slogan: "일심단결" (Single-hearted Unity)

KOREAN TRADITIONAL ELEMENTS:
- Traditional Korean celadon piece (고려청자) in station window
- Korean paper lantern (한지 등) hanging from station eaves
- Korean traditional knotwork (매듭 maedeup) decoration on station

KOREAN NATURE:
- Wild chrysanthemums (들국화) along the tracks
- Korean pine trees (소나무) with characteristic shape
- Bamboo grove in background
- Misty mountain peaks with traditional Korean painting style

COLOR PALETTE (WARM KOREAN RETRO):
- Primary: warm gold （黄铜色）, brick red, pine green
- Secondary: cream white （奶油白）, navy blue （暗蓝色）
- Sky: soft dawn orange to pale blue gradient
- Train: dark green with gold trim (classic Korean railway colors)

ATMOSPHERE:
- Nostalgic, hopeful, epic
- The dawn of a new era — railway meets sand energy
- Blend of Korean tradition and 2076 retro-futurism
- A journey about to begin

EXCLUSIONS:
- No character close-ups
- No modern glass skyscrapers
- No bright neon
- No heavy industrial pollution
```


---

### 2.31 纯黑（black）

**文件**：无（引擎特殊色值，不留图片文件）
**用途**：序章开场黑屏/滚动新闻/章节转场。纯黑背景 + 字幕。

**说明**：`black` 不生成图片——引擎 `BackgroundManager` 对 `"black"` 应直接渲染纯黑（修复 `Resources.Load("bg/black")` 失败的警告路径）。若未来需要极简渐变可追加。

---

### 2.37 序章开场·标题卡（title_card）

**文件**：`Resources/bg/title_card.png`
**用途**：序章第一幕——游戏标题+创作者信息的展示背景。玩家启动游戏后看到的第一张图。

**叙事锚点**：这不是一个"地方"，是一个仪式——黑屏中浮现游戏名，告诉玩家：故事开始了。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
title card background, dark atmospheric, cinematic:

SCENE: 深色渐变背景，从底部的深棕黑色过渡到顶部的深靛蓝色。画面中央偏下位置有一条极细的水平线，像地平线上的铁轨延伸到远方，铁轨上有微弱的光点闪烁（暗示远方的信号灯）。画面左上角有极淡的旧地图纹理（铁路线路图的残影），几乎不可见但增加了质感层次。整体构图极其简洁——大量留白（深色留白），给标题文字留出空间。

LAYOUT:
- 背景：从底部#0A0508到顶部#0D1528的垂直渐变
- 中央偏下：一条极细的水平线（铁轨意象），从左延伸到右
- 水平线上2-3个微弱光点（远处信号灯）
- 左上角：极淡的铁路地图纹理（透明度约5%）
- 整体：暗色调，留白为主，给文字留空间

COLORS:
- 底部：深棕黑
- 顶部：深靛蓝
- 铁轨线：极暗的锈色
- 光点：微弱的暖橙

MOOD: 仪式感。黑幕拉开之前的一瞬间。安静、庄重、有分量。

NO CHARACTERS, NO PEOPLE, NO VEHICLES — pure abstract atmospheric background for title text overlay.
```

---

### 2.38 序章新闻·沙能诞生（news_2050）

**文件**：`Resources/bg/news_2050.png`
**用途**：序章开场新闻段落1——2050年5月，世界首辆沙能试验车在伦敦面世。

**叙事锚点**：一切的起点。一个改变世界的发明在伦敦诞生，但新闻播报的语气是客观的——当时没人知道这个技术会把铁路送进坟墓。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
2050 London technology unveiling, indoor exhibition hall:

SCENE: 伦敦的一个大型科技展览厅内部。中央展示台上停着一辆小型试验车「奇幻沙子」——造型圆润、银灰色车身，车顶有沙子入口，旁边堆放着几袋工业沙。展示台后方是大屏幕，屏幕上显示沙能原理的简化图示。周围是穿正装的各国专家和记者剪影（模糊，不清晰面部）。展厅两侧悬挂着标语条幅，隐约可见"Sand Energy"和"Future of Power"字样。冷色调的荧光灯照明，地面是光亮的大理石。整体氛围：2050年的科技发布会，克制、正式、充满期待。

LAYOUT:
- 中央：展示台上的试验车（画面焦点，约15%画面）
- 展示台后方：大屏幕（原理图示）
- 两侧：模糊的人群剪影
- 两侧上方：条幅标语
- 顶部：荧光灯带

COLORS:
- 展厅：冷白光照明
- 试验车：银灰车身
- 沙袋：暖土黄
- 人群剪影：深色
- 大屏幕：淡蓝光

MOOD: 一个时代的开端。新闻播报的语气是客观的，但画面里每个人都在见证历史。

NO CHARACTERS with detailed faces — only blurred silhouettes for crowd. NO aircraft.
```

---

### 2.39 序章新闻·铁路衰落（news_2072）

**文件**：`Resources/bg/news_2072.png`
**用途**：序章开场新闻段落3——2072年全球铁路几乎全面停运。

**叙事锚点**：从2050年伦敦展厅到2072年，22年。同一块屏幕上，新闻说"铁路全面停运"。画面里没有人物——只有空荡的车站和生锈的铁轨，像一座座墓碑。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
abandoned railway station, 2072, global railway shutdown:

SCENE: 一座废弃的铁路车站，黄昏时分。站台空无一人，杂草从混凝土裂缝中长出。远处的铁轨延伸到地平线，锈迹斑斑。站牌上的字迹已经模糊。候车室的玻璃碎了几块，门歪斜着半开。站台上散落着几张泛黄的旧车票和一份皱巴巴的报纸（头版可见"全球铁路停运"字样，但模糊）。天空是阴沉的灰橙色，像整个世界都在叹息。没有任何车辆、没有任何人——只有废弃的基础设施和自然的缓慢回收。

LAYOUT:
- 中央偏左：废弃车站建筑（半塌的候车室）
- 中央偏右：延伸到远方的铁轨（透视消失点）
- 站台：杂草丛生，散落旧物
- 天空：阴沉灰橙色
- 远处：更多废弃的铁路设施轮廓

COLORS:
- 天空：灰橙色阴沉
- 铁轨：锈棕红色
- 站台：灰色混凝土+绿色杂草
- 建筑：风化的灰白色
- 旧报纸：泛黄白色

MOOD: 一个时代的终结。没有人在等车了。铁轨还在，只是没人需要它了。安静到可以听见风穿过破碎玻璃的声音。

NO CHARACTERS, NO PEOPLE, NO VEHICLES — empty abandoned station, nature reclaiming human infrastructure.
```


---

### 2.41 夜路·道口（night_road）

**文件**：`Resources/bg/night_road.png`
**用途**：序章 05 巡线 scene3——茶嫂小货车沿河谷便道夜行、三公里半道口陷车段的背景。全章唯一纯夜户外线路景。

**叙事锚点**：山谷起薄雾，车灯照出去只有十米；三公里半的道口，路基被水掏空的沟口压着我傍晚搬来的三块石头，红布条被风抽得啪啪响；马灯是画面里唯一的暖光。

**提示词**：
```
1920×1080，清晰像素风，星露谷风格像素画，
雾峰村线 夜路·道口 —— 深夜山谷中的铁路道口，无人无车的空景：

视角：站在道口旁便道一侧、贴近地面的视角，沿泥辙望向与钢轨交汇处

以下元素供参考，自行取舍添加：
- 前景：被车辙压出泥坑的便道碎石，一盏刚放下的马灯搁在道口木板边，玻璃罩里一点暖黄火苗（近景留空，不画持灯人）
- 道口：横铺的旧枕木道口板，缝里积着碎石与草屑；两侧钢轨锈棕发亮，往两个方向没入雾中
- 沟口（关键叙事物）：路基边被水掏空的暗沟，沟口压着三块手掌大的石头，石头上系一条褪色的红布条，被风抽出一个角
- 中景：河谷便道与铁轨并行伸向雾里，谷底河水反着一点微光
- 远景：两侧山脊的墨色剪影，山口处透进两束很淡的暖黄灯光（画面外车辆的近光，不画车）
- 天空：无月深夜，星子稀薄，谷口一层青灰薄雾

文字：无
禁止：終點站 / 松桥 / 巨型手写墙字
NO CHARACTERS, NO PEOPLE, NO VEHICLES — 空景夜道口，只有马灯、红布条和雾
```
