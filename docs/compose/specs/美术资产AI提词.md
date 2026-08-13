# 游戏美术资产 - AI绘图提示词

> 版本：v1.0  
> 用途：供AI绘图工具生成像素风游戏素材  
> 统一风格：16-bit retro pixel art，暖色调，类似 Stardew Valley  
> 统一输出：PNG 透明背景，1920×1080（背景）/ 1024×2048（角色）

---

## 目录

1. [角色立绘](#1-角色立绘)
2. [场景背景](#2-场景背景)
3. [列车与车辆](#3-列车与车辆)
4. [UI元素](#4-ui元素)
5. [图标与装饰](#5-图标与装饰)

---

## 注意事项

- 所有提示词均为 **中文 + 英文双语**，方便在不同AI绘画工具中使用
- 角色立绘需要 **全身**（含脚），**透明背景**
- 场景背景为 **1920×1080**，无透明
- 像素风格指定为 **16-bit retro pixel art**，参考 Stardew Valley / 星露谷物语

---

## 1. 角色立绘

### 1.1 林彪悍（主角）

**文件**：`Resources/characters/lin_biaohan/{表情}.png`  
**规格**：1024×2048，PNG透明背景，全身  
**表情**：normal, smile, serious, surprised, sad, angry, worried, curious, happy, wink, bored, gentle, smug, excited, shout, shocked（16种）

**中文提示词**：
```
像素艺术，16-bit复古风格，全身立绘，透明背景，年轻男性，25岁，椭圆脸，下巴柔和，大而圆的深棕色眼睛，蓬松黑色短发略带自然卷，白皙暖色调皮肤。身穿深海军蓝色工作夹克（从爷爷继承），白色圆领T恤，铜色怀表链从领口露出，深蓝色牛仔裤，旧棕色皮鞋。表情：【替换为具体表情】。像素画风格，类似星露谷物语，暖色调，全身，双脚可见。
```

**English prompt**:
```
Pixel art, 16-bit retro style, full body character portrait, transparent background, young male, 25 years old, slightly round oval face with soft jawline, large round dark brown eyes, fluffy black short hair with natural waves, warm pale skin tone. Wearing a deep navy blue work jacket (inherited from grandfather), white crew neck t-shirt, copper pocket watch chain visible at collar, dark blue jeans, worn brown leather shoes. Expression: [specific expression]. Pixel art style reminiscent of Stardew Valley, warm color palette, full body including feet.
```

**表情映射表**：

| 表情ID | 表情名称 | 特征描述 |
|--------|---------|---------|
| normal | 普通 | 自然表情，嘴角平直，眼神平静 |
| smile | 微笑 | 嘴角上扬，眼睛微微眯起，温暖 |
| serious | 严肃 | 嘴唇紧闭，眉毛微皱，目光坚定 |
| surprised | 惊讶 | 眼睛睁大，眉毛上扬，嘴巴微张 |
| sad | 悲伤 | 嘴角下垂，眼神暗淡，微微低头 |
| angry | 生气 | 眉毛紧皱，眼神锐利，嘴巴紧闭 |
| worried | 担忧 | 眉毛微蹙，嘴角略微下撇，眼神不安 |
| curious | 好奇 | 眼睛睁大，眉毛微抬，头微微倾斜 |
| happy | 开心 | 嘴巴大张笑，眼睛眯成月牙 |
| wink | 眨眼 | 一只眼闭着，嘴角上扬，俏皮 |
| bored | 无聊 | 眼神放空，嘴角平直，无精打采 |
| gentle | 温柔 | 微笑柔和，眼神温暖，嘴角微扬 |
| smug | 得意 | 嘴角一侧上扬，眼神自信，略抬下巴 |
| excited | 兴奋 | 眼睛发亮，嘴巴大张，充满期待 |
| shout | 呼喊 | 嘴巴大张呈O形，眉毛上扬，表情激动 |
| shocked | 震惊 | 眼睛瞪圆，嘴巴大张，眉毛极高 |

---

### 1.2 老陈（陈守正）

**文件**：`Resources/characters/laochen/{表情}.png`  
**规格**：1024×2048，PNG透明背景，全身  
**表情**：normal, smile, serious, sad, worried, happy, curious, gentle（8种）

**中文提示词**：
```
像素艺术，16-bit复古风格，全身立绘，透明背景，老年男性，68岁，方脸，深色皮肤布满皱纹但硬朗，深棕色眼睛，稀疏灰白眉毛，扁平鼻梁，圆润红色鼻头（冻伤），灰白短发梳理整齐。身穿白色长袖棉衬衫（领口磨损），深灰色V领毛线背心，老花镜挂在脖子上，深色宽松长裤，黑色布鞋。表情温暖朴实。像素画风格，类似星露谷物语，暖色调，全身，双脚可见。
```

**English prompt**:
```
Pixel art, 16-bit retro style, full body character portrait, transparent background, elderly male, 68 years old, square face, dark wrinkled but strong skin, deep brown eyes, sparse gray-white eyebrows, flat nose bridge, round red nose tip (frostbite), tidy gray-white short hair. Wearing a white long-sleeve cotton shirt (worn collar), dark gray V-neck wool vest, reading glasses hanging on a leather cord around neck, dark loose trousers, black cloth shoes. Warm and kind expression. Pixel art style reminiscent of Stardew Valley, warm color palette, full body including feet.
```

---

### 1.3 张工（张德厚）

**文件**：`Resources/characters/zhanggong/{表情}.png`  
**规格**：1024×2048，PNG透明背景，全身  
**表情**：normal, smile, serious, happy, curious, surprised（6种）

**中文提示词**：
```
像素艺术，16-bit复古风格，全身立绘，透明背景，老年男性，62岁，圆润福相脸，红润脸颊，小而明亮的眼睛，笑时眯成缝，稀疏不规则的灰白眉毛，圆润红色鼻头（酒糟鼻），灰白稀疏头发，头顶略秃。身穿棕色格子长袖衬衫（袖子卷到肘部），深棕色灯芯绒背心，口袋鼓鼓装着小零件，老花镜架在头顶，深色宽松长裤，黑色布鞋。表情乐观开朗。像素画风格，类似星露谷物语，暖色调，全身，双脚可见。
```

---

### 1.4 李阿姨（李桂芳）

**文件**：`Resources/characters/liayi/{表情}.png`  
**规格**：1024×2048，PNG透明背景，全身  
**表情**：normal, smile, happy, serious, worried, surprised（6种）

**中文提示词**：
```
像素艺术，16-bit复古风格，全身立绘，透明背景，中年女性，55岁，圆脸，略有双下巴，小而有神的眼睛，笑时弯成月牙，细弯眉（微褪色），齐耳烫发小卷，染棕色但发根有白色长出。身穿粉色碎花长袖衬衫，深蓝色棉质围裙（前口袋放手机），深色长裤，黑色布鞋，左手戴佛珠手串，右手金戒指。表情热情温暖。像素画风格，类似星露谷物语，暖色调，全身，双脚可见。
```

---

### 1.5 王小弟（王晨阳）

**文件**：`Resources/characters/wangxiaodi/{表情}.png`  
**规格**：1024×2048，PNG透明背景，全身  
**表情**：normal, smile, happy, curious, surprised, serious（6种）

**中文提示词**：
```
像素艺术，16-bit复古风格，全身立绘，透明背景，年轻男性，22岁，椭圆脸，有婴儿肥，大而明亮的眼睛，浓密自然眉毛，小巧上翘鼻子，黑色短发蓬松，左耳银色耳钉。身穿浅灰色连帽卫衣，内搭白色T恤（印有Transportation Engineering字样），脖子上挂黑色头戴式耳机，浅蓝色牛仔裤（膝盖处磨损），白色运动鞋。表情阳光热血。像素画风格，类似星露谷物语，暖色调，全身，双脚可见。
```

---

### 1.6 赵师傅（赵铁山）

**文件**：`Resources/characters/zhaoshifu/{表情}.png`  
**规格**：1024×2048，PNG透明背景，全身  
**表情**：normal, serious, smile, worried, surprised, sad（6种）

**中文提示词**：
```
像素艺术，16-bit复古风格，全身立绘，透明背景，中年男性，55岁，略瘦长脸，棱角分明，深棕色眼睛，严肃但不尖锐，黑色短发整齐利落，深色户外工作肤色，左脸颊有2cm旧伤疤。身穿军绿色utility夹克（拉链到胸口），深灰色高领针织衫，左腕军用机械手表，深色utility长裤配帆布腰带，黑色作战靴。表情沉稳。像素画风格，类似星露谷物语，暖色调，全身，双脚可见。
```

---

### 1.7 小芳

**文件**：`Resources/characters/xiaofang/{表情}.png`  
**规格**：1024×2048，PNG透明背景，全身  
**表情**：normal, smile, happy, serious, curious, surprised（6种）

**中文提示词**：
```
像素艺术，16-bit复古风格，全身立绘，透明背景，中年女性，45岁，圆脸，温和眼神，短发整齐，穿着朴素的工作服，深色长裤，平底布鞋，身上穿着志愿者马甲。表情热情友善。像素画风格，类似星露谷物语，暖色调，全身，双脚可见。
```

---

### 1.8 岁月（AI助手）

**文件**：`Resources/characters/suiyue/interface.png`  
**规格**：仅界面头像，256×256  
**说明**：岁月没有实体形象，仅作为AI助手界面头像

**中文提示词**：
```
像素艺术，16-bit复古风格，圆形头像图标，透明背景，蓝绿色AI助手头像，简约的机器人/人工智能风格，发光的蓝色眼睛，金属质感，圆润设计，暖色调，类似星露谷物语风格，256x256像素。
```

---

## 2. 场景背景

### 2.1 VN场景背景

所有背景规格：**1920×1080**，像素风，无透明

#### 2.1.1 开场新闻场景

**文件**：`Resources/bg/black.png`  
**说明**：纯黑背景，不需要生成（Unity中直接用Color.black）

#### 2.1.2 实验室

**文件**：`Resources/bg/lab.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，现代化实验室内部，各种屏幕和设备，电脑桌，复杂的算法模型显示在屏幕上，窗外是平壤的天际线，暖色调，温暖灯光，有科技感但不冰冷，类似星露谷物语的像素风格。
```

#### 2.1.3 导师办公室

**文件**：`Resources/bg/professor_office.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，简朴的大学办公室，木质书桌，墙上挂着各种学术证书和奖状，书架上有书籍，窗户透进自然光，暖色调，温馨氛围，类似星露谷物语的像素风格。
```

#### 2.1.4 停机坪

**文件**：`Resources/bg/helicopter_pad.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，大学屋顶停机坪，傍晚天空，停着几辆未来风格的沙能飞行车，深蓝色车身，远处是平壤城市天际线，夕阳余晖，暖色调，像素风格。
```

#### 2.1.5 边境小镇

**文件**：`Resources/bg/border_town.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，中国边境小镇，晨雾中，有补给站设施，类似加油机的设备，远处有山，清晨阳光，暖色调，类似星露谷物语的像素风格。
```

#### 2.1.6 雾峰村夕阳

**文件**：`Resources/bg/village_sunset.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，山谷中的小村庄，夕阳下，暖色调，有一条铁路线穿过村庄，远处有山，破旧但温馨的房屋，炊烟袅袅，金色阳光，类似星露谷物语的像素风格。
```

#### 2.1.7 车站夕阳

**文件**：`Resources/bg/station_sunset.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，破旧的小火车站，夕阳下，站台有长椅，轨道延伸向远方，暖色调，金色阳光，有怀旧感，杂草丛生但仍有生机，类似星露谷物语的像素风格。
```

#### 2.1.8 傍晚站台

**文件**：`Resources/bg/platform_evening.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，火车站台，傍晚时分，暖色调灯光，站台上有老人在等待，远处有列车进站，宁静的氛围，像素风格。
```

#### 2.1.9 铁路轨道

**文件**：`Resources/bg/railway_track.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，铁路轨道延伸向远方，两边有杂草和野花，夕阳下，有电线杆，暖色调，怀旧感，铁轨有些生锈，但仍有维护的痕迹，类似星露谷物语的像素风格。
```

#### 2.1.10 松桥站（废弃车站）

**文件**：`Resources/bg/songqiao_station.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，废弃的小火车站，站牌字迹模糊写着「松桥站」，站台上杂草丛生，长椅破旧不堪，有几位老人坐在站台上晒太阳，暖色调，夕阳，怀旧感伤氛围，类似星露谷物语的像素风格。
```

#### 2.1.11 夜晚车站

**文件**：`Resources/bg/station_night.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，夜晚的车站站台，暖色灯光照亮站台，星空，铁轨在月光下泛光，宁静而温暖，有几个人影在站台上，类似星露谷物语的像素风格。
```

#### 2.1.12 早晨机库

**文件**：`Resources/bg/depot_morning.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，老式机车库内部，清晨阳光从门口射入，一台老旧的柴油机车静静停着，工具散落，有工作台和零件，暖色调，略带灰尘感，类似星露谷物语的像素风格。
```

#### 2.1.13 车厢内部

**文件**：`Resources/bg/train_inside.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，老式火车车厢内部，木质座椅，窗户透进阳光，车厢内有几位乘客，暖色调，怀旧温馨氛围，类似星露谷物语的像素风格。
```

#### 2.1.14 站长办公室

**文件**：`Resources/bg/station_office.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，小车站的站长办公室，木质办公桌，墙上挂着铁路地图和时间表，老式电话，窗户可以看到站台，暖色调，朴素温馨，类似星露谷物语的像素风格。
```

#### 2.1.15 早晨站台

**文件**：`Resources/bg/platform_morning.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，清晨火车站台，阳光明媚，列车准备出发，有几位乘客在等车，站台干净整洁，暖色调，充满希望的氛围，类似星露谷物语的像素风格。
```

---

### 2.2 经营场景背景

**文件**：`Resources/bg/station_main.png`  
**中文提示词**：
```
像素艺术，16-bit复古风格，游戏场景背景，1920x1080，火车站主场景，俯视角，站台、轨道、列车、候车室、机库，暖色调，清晰的地图结构，适合模拟经营游戏，类似星露谷物语的像素风格，村庄场景，有绿色植被，木质建筑，铁轨贯穿。
```

---

## 3. 列车与车辆

### 3.1 NF-5 耕牛（内燃机车）

**文件**：`Resources/characters/train_nf5.png`  
**规格**：1024×256，PNG透明背景，侧视图

**中文提示词**：
```
像素艺术，16-bit复古风格，列车侧视图，透明背景，1024x256，老式柴油内燃机车，深绿色车身，黄色条纹，车头有圆形大灯，排障器，车顶有排气口，老旧但坚固的外形，参考东风4型机车，暖色调，像素风格，侧视图，完整机车。
```

### 3.2 SY-22 灰雀（客运车厢）

**文件**：`Resources/characters/train_carriage.png`  
**规格**：1024×128，PNG透明背景，侧视图

**中文提示词**：
```
像素艺术，16-bit复古风格，列车车厢侧视图，透明背景，1024x128，老式绿色客运车厢，有窗户，车顶有通风口，老旧但整洁，适合支线短途客运，暖色调，像素风格，侧视图。
```

### 3.3 沙子飞猪号（沙能飞行车）

**文件**：`Resources/characters/sand_flying_pig.png`  
**规格**：512×256，PNG透明背景

**中文提示词**：
```
像素艺术，16-bit复古风格，飞行汽车侧视图，透明背景，512x256，深蓝色车身，圆润设计，有飞行模式展开的机翼，金日成综合大学校徽在车身侧面，老旧的试验车型，暖色调，像素风格。
```

---

## 4. UI元素

### 4.1 按钮

**文件**：`Resources/UI/btn_primary.png` / `btn_secondary.png` / `btn_small.png`  
**规格**：200×64 / 200×64 / 120×40

**中文提示词**：
```
像素艺术，16-bit复古风格，游戏UI按钮，圆角矩形，暖色调，棕色/金色主题，边缘有像素风格边框，内填充色，适合铁路主题游戏，类似星露谷物语的UI风格，PNG透明背景。
```

### 4.2 面板背景

**文件**：`Resources/UI/panel_bg.png`  
**规格**：600×400

**中文提示词**：
```
像素艺术，16-bit复古风格，游戏UI面板背景，半透明，暖色调，深棕色到金色的渐变，边缘有复古花纹装饰，适合铁路主题，类似星露谷物语的UI风格。
```

### 4.3 图标

**文件**：`Resources/UI/icon_{名称}.png`  
**规格**：32×32

**需要图标清单**：
- `icon_money.png` — 钱币图标
- `icon_trust.png` — 信任/爱心图标
- `icon_train.png` — 列车图标
- `icon_passenger.png` — 乘客图标
- `icon_fuel.png` — 燃料图标
- `icon_maintenance.png` — 维修工具图标
- `icon_staff.png` — 员工图标
- `icon_news.png` — 新闻图标
- `icon_story.png` — 剧情图标
- `icon_settings.png` — 设置图标
- `icon_save.png` — 存档图标

**通用提示词**：
```
像素艺术，16-bit复古风格，游戏图标，32x32，暖色调，棕色/金色主题，简洁清晰，像素风格，类似星露谷物语的图标风格，PNG透明背景。
```

---

## 5. 音频资源

### 5.1 BGM（背景音乐）

| 文件 | 风格 | 说明 |
|------|------|------|
| `bgm/melancholy.ogg` | 忧郁、怀旧 | 序章主要BGM，回忆和思考场景 |
| `bgm/emotional.ogg` | 感人、温暖 | 重要对话场景，重逢时刻 |
| `bgm/determination.ogg` | 坚定、希望 | 转折点，主角下定决心 |
| `bgm/morning.ogg` | 清晨、宁静 | 白天场景，新的开始 |
| `bgm/warm.ogg` | 温暖、安心 | 团队场景，社区互动 |
| `bgm/news.ogg` | 严肃、播报 | 新闻展示场景 |
| `bgm/calm.ogg` | 平静、日常 | 日常对话，平静时刻 |
| `bgm/train_ambient.ogg` | 火车行驶环境 | 车厢场景，列车运行中 |
| `bgm/ambient_nature.ogg` | 自然环境 | 户外场景，铁轨巡视 |
| `bgm/silence.ogg` | 安静 | 过渡场景，仅环境音 |

### 5.2 SFX（音效）

| 文件 | 说明 |
|------|------|
| `sfx/click.ogg` | 按钮点击 |
| `sfx/bell.ogg` | 发车铃声 |
| `sfx/whistle.ogg` | 汽笛声 |
| `sfx/train_move.ogg` | 列车行驶声 |
| `sfx/typewriter.ogg` | 打字机音效（VN对话框） |

---

## 6. 生成优先级

| 优先级 | 资产 | 原因 |
|--------|------|------|
| **P0** | 林彪悍立绘（16表情） | VN系统核心，序章大量使用 |
| **P0** | 老陈立绘（8表情） | VN系统核心，序章大量使用 |
| **P0** | 场景背景：lab, professor_office, helicopter_pad, station_sunset, platform_evening, railway_track | 序章必须 |
| **P1** | 其他角色立绘（张工/李阿姨/王小弟/赵师傅/小芳） | 序章Day 4晚上需要 |
| **P1** | 场景背景：village_sunset, songqiao_station, station_night, depot_morning, train_inside, station_office | 序章后续需要 |
| **P1** | NF-5耕牛+SY-22车厢 | 游戏核心视觉 |
| **P2** | UI元素 | 经营界面需要 |
| **P2** | BGM/SFX | 体验提升 |
| **P3** | 其他场景背景 | 后续章节 |

---

*本文档供AI绘图工具使用，所有提示词可直接复制粘贴。*