# 游戏美术资产 - AI绘图提示词（v2.0 分类清单版）

> 版本：v2.0  
> 用途：供AI绘图工具生成像素风游戏素材  
> 格式标准：多层分类清单 + 色彩方案 + 氛围 + 排除项（基于实验室成功案例）  
> 统一风格：16-bit retro pixel art，暖色调，类似 Stardew Valley  
> 识图：mimo 2.5 | 执行任务：ds / ds0718

---

## 目录

1. [角色立绘](#1-角色立绘)
2. [场景背景](#2-场景背景)
3. [BGM（Suno AI 音乐提示词）](#3-bgmsuno-ai-音乐提示词)
4. [生成优先级](#4-生成优先级)

---

## 一、角色立绘

### 通用规范

- **工作流**：第1步生成主图（半身到腿，详细全身）→ 第2步用主图生成表情差分（denoising 0.2-0.3，同服装/同姿势/同光照）
- **尺寸**：1024×2048，PNG透明底，全身包括脚
- **风格**：STARDEW VALLEY STYLE，16-bit retro pixel art，暖色调

---

### 1.1 林彪悍（主角）— 已有16表情，不需再生成

**文件**：`Resources/characters/lin_biaohan/{表情}.png`  
**状态**：✅ 已完成（16表情）

---

### 1.2 老陈（陈守正）

**文件**：`Resources/characters/laochen/{表情}.png`  
**表情**：normal, smile, serious, sad, worried, happy, curious, gentle（8种）

**主图提示词**：
```
pixel art, 16-bit retro style, full body character portrait, transparent background, STARDEW VALLEY STYLE PIXEL ART, elderly male, 68 years old.

FACE CHARACTERISTICS:
- Square face, dark wrinkled but strong skin
- Deep brown eyes, squint into warm slits when smiling
- Sparse gray-white eyebrows, neatly trimmed
- Flat nose bridge, round red nose tip (frostbite)
- Gray-white short hair, neatly combed, one unruly tuft behind left ear
- Thick lips, slightly downturned corners

CLOTHING:
- White long-sleeve cotton shirt, worn collar and cuffs
- Second button re-sewn with mismatched red thread
- Dark gray V-neck wool vest, pilled at chest pocket, darning marks at hem
- Reading glasses hanging on old leather cord around neck
- Dark loose trousers, ironed crease
- Black cloth shoes, worn soles

HAND DETAILS:
- Right index finger joint slightly enlarged from years of wrenching bolts

COLOR PALETTE:
- Primary: warm gray (#8B8682), off-white (#F5F0E8)
- Secondary: dark gray (#4A4A4A), worn brown (#6B4226)
- Accents: faded red thread (#CD5C5C), copper glasses frame (#B87333)

ATMOSPHERE: Warm, experienced, slightly weary but kind. A man who has seen the railway through its best and worst days.

AVOID:
- Too clean or polished appearance (should show years of work)
- Cold or distant expression
- Modern or fashionable clothing
```

---

### 1.3 张工（张德厚）

**文件**：`Resources/characters/zhanggong/{表情}.png`  
**表情**：normal, smile, serious, happy, curious, surprised（6种）

**主图提示词**：
```
pixel art, 16-bit retro style, full body character portrait, transparent background, STARDEW VALLEY STYLE PIXEL ART, elderly male, 62 years old.

FACE CHARACTERISTICS:
- Round, full face, rosy cheeks
- Small bright eyes, squint into happy slits when smiling
- Sparse irregular gray-white eyebrows, a few extra long
- Round red nose tip (rosacea)
- Gray-white sparse hair, slightly bald on top, fluffy on sides, never combed
- Yellowish skin tone, 5mm black oil stain on right cheek
- Missing left front tooth (knocked out fixing a machine 20 years ago)

CLOTHING:
- Brown plaid long-sleeve shirt, sleeves rolled to elbows
- Three pens (red, blue, black) and small screwdriver in left chest pocket
- Dark brown corduroy vest, zipper broken, held with safety pin
- Pockets bulging with small parts, screws, electrical tape
- Reading glasses perched on top of head, temple wrapped in tape
- Dark loose trousers with tool marks
- Black cloth shoes

HAND DETAILS:
- 1cm old burn scar on left thumb web

COLOR PALETTE:
- Primary: warm brown (#8B4513), plaid red-brown (#A0522D)
- Secondary: corduroy brown (#6B3A2A), faded denim blue (#4A7C9B)
- Accents: bright red pen (#FF0000), blue pen (#0000FF), silver screwdriver (#C0C0C0)

ATMOSPHERE: Optimistic, slightly messy but brilliant. The kind of mechanic who can fix anything but can't find his own glasses.

AVOID:
- Clean or professional mechanic appearance
- Serious or gloomy expression
- Impossibly neat workshop look
```

---

### 1.4 李阿姨（李桂芳）

**文件**：`Resources/characters/liayi/{表情}.png`  
**表情**：normal, smile, happy, serious, worried, surprised（6种）

**主图提示词**：
```
pixel art, 16-bit retro style, full body character portrait, transparent background, STARDEW VALLEY STYLE PIXEL ART, middle-aged female, 55 years old.

FACE CHARACTERISTICS:
- Round face, slight double chin
- Small but lively eyes, crescent-shaped when smiling
- Thin arched eyebrows (tattooed, now faded to gray-blue)
- Small round nose tip
- Thin lips, perpetually upturned corners, speaks like a machine gun
- Ear-length permed hair, dyed brown with 2cm white roots showing
- Yellowish-white skin tone, wrinkles on neck from years of cooking

CLOTHING:
- Pink floral long-sleeve shirt, freshly ironed
- Deep blue cotton apron, old phone in front pocket
- Butterfly bow tied at lower back (decorative)
- Dark long pants, slightly worn at knees
- Black cloth shoes
- Dark brown Buddhist bead bracelet on left wrist, 12mm beads
- Gold ring on right hand

COLOR PALETTE:
- Primary: pink (#FFB6C1), floral print (#FF69B4)
- Secondary: deep blue apron (#000080), dark pants (#2F4F4F)
- Accents: gold ring (#FFD700), brown beads (#8B4513)

ATMOSPHERE: Warm, talkative, the village's information hub. She knows everyone's business and means well.

AVOID:
- Too young or fashionable appearance
- Quiet or reserved expression
- Missing the apron (essential character marker)
```

---

### 1.5 王小弟（王晨阳）

**文件**：`Resources/characters/wangxiaodi/{表情}.png`  
**表情**：normal, smile, happy, curious, surprised, serious（6种）

**主图提示词**：
```
pixel art, 16-bit retro style, full body character portrait, transparent background, STARDEW VALLEY STYLE PIXEL ART, young male, 22 years old.

FACE CHARACTERISTICS:
- Oval face, still has baby fat
- Large bright eyes, clear black and white
- Thick natural eyebrows, slightly messy but handsome
- Small upturned nose, youthful
- Thick lips, easy smile showing straight white teeth
- Black short hair, 4cm fluffy, morning hand-styled look
- Fringe parted to reveal full forehead
- Pale clean skin, 3mm silver earring in left ear
- Two or three light acne scars on chin

CLOTHING:
- Light gray hoodie (discount brand), uneven drawstrings
- White t-shirt underneath, collar reads "Transportation Engineering 2026"
- Black over-ear headphones around neck (installment payment "professional equipment")
- Light blue jeans, 2cm white wear marks at both knees
- White sneakers, slightly dirty toe

COLOR PALETTE:
- Primary: light gray (#D3D3D3), light blue (#87CEEB)
- Secondary: white (#FFFFFF), denim blue (#4682B4)
- Accents: silver earring (#C0C0C0), black headphones (#1A1A1A)

ATMOSPHERE: Energetic, eager to prove himself, slightly clumsy but sincere. The fresh graduate who still believes he can change the world.

AVOID:
- Too mature or serious appearance
- Expensive or fashionable clothing
- Clean, unworn sneakers (should show use)
```

---

### 1.6 赵师傅（赵铁山）

**文件**：`Resources/characters/zhaoshifu/{表情}.png`  
**表情**：normal, serious, smile, worried, surprised, sad（6种）

**主图提示词**：
```
pixel art, 16-bit retro style, full body character portrait, transparent background, STARDEW VALLEY STYLE PIXEL ART, middle-aged male, 55 years old.

FACE CHARACTERISTICS:
- Slightly long, angular face, sharp features
- Deep brown eyes, serious but not sharp, gentle when relaxed
- Average thickness eyebrows, natural shape
- Average nose bridge
- Thin lips, straight corners, looks serious when not smiling
- Black short hair, 1.5cm, neat and tidy, clean sideburns
- Dark outdoor work complexion
- 2cm old scar on left cheek (from military training)

CLOTHING:
- Olive green utility jacket, zipped to chest
- Zipper pull wrapped in black electrical tape (original broken)
- 1cm hole on left sleeve, unpatched
- Dark gray high-neck knit sweater, form-fitting
- Left wrist vintage military mechanical watch, old strap, 3mm scratch on face
- Dark utility pants, canvas belt with brass buckle
- Black combat boots, scuffed toe

COLOR PALETTE:
- Primary: olive green (#4B5320), dark gray (#36454F)
- Secondary: black (#1A1A1A), brass buckle (#D4A017)
- Accents: silver watch face (#C0C0C0), black electrical tape (#1A1A1A)

ATMOSPHERE: Stern on the outside, warm on the inside. A man of few words whose actions speak louder.

AVOID:
- Soft or friendly first impression
- Missing the scar (essential character detail)
- Clean, unworn combat boots
```

---

### 1.7 小芳

**文件**：`Resources/characters/xiaofang/{表情}.png`  
**表情**：normal, smile, happy, serious, curious, surprised（6种）

**主图提示词**：
```
pixel art, 16-bit retro style, full body character portrait, transparent background, STARDEW VALLEY STYLE PIXEL ART, middle-aged female, 45 years old.

FACE CHARACTERISTICS:
- Round face, gentle eyes
- Short neat hair
- Warm, approachable expression

CLOTHING:
- Simple work uniform
- Volunteer vest
- Dark long pants
- Flat cloth shoes

COLOR PALETTE:
- Primary: warm tones, volunteer vest color
- Secondary: practical dark colors

ATMOSPHERE: Enthusiastic, willing to learn, the volunteer who shows up every day.

AVOID:
- Too polished or professional appearance
- Cold or distant expression
```

---

### 1.8 岁月（AI 助手界面头像）

**文件**：`Resources/characters/suiyue/interface.png`  
**尺寸**：256×256，PNG透明底

**提示词**：
```
pixel art, 16-bit retro style, circular avatar icon, transparent background, STARDEW VALLEY STYLE, AI assistant interface.

DESIGN ELEMENTS:
- Blue-green AI assistant avatar
- Simple robot/AI style
- Glowing blue eyes
- Metallic texture, rounded design
- 256x256 pixels
- Warm color palette

AVOID:
- Too complex or detailed (should work as small icon)
- Cold or threatening appearance
- Human-like features
```

---

## 二、场景背景

### 通用规范

- **尺寸**：1920×1080，JPG/PNG
- **风格**：STARDEW VALLEY STYLE pixel art，16-bit retro
- **格式**：多层分类清单 + 色彩方案 + 氛围 + 排除项

---

### 2.1 导师办公室

**文件**：`Resources/bg/professor_office.png`  
**用途**：序章 Day 0 导师办公室场景

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio, STARDEW VALLEY STYLE PIXEL ART, university professor's office, Kim Il Sung University, Pyongyang, 2076, modest academic setting.

ARCHITECTURAL ELEMENTS:
- Simple office with wooden desk
- Wall covered with academic certificates and awards
- Bookshelf with technical books
- Window letting in natural light
- Traditional Korean wooden floor panels

FURNITURE AND ITEMS:
- Wooden desk with traditional Korean joinery
- Office chair, slightly worn
- Bookshelf with railway engineering texts
- Desk lamp, traditional Korean design
- Korean calligraphy brush and ink stone on desk

WALL DECORATIONS:
- University diploma with Korean text
- Academic awards and certificates
- Korean calendar showing 2076
- Traditional Korean paper art (minhwa) frame

COLOR PALETTE:
- Primary: warm brown (#8B4513), cream white (#FFFDD0)
- Secondary: dark wood (#5C3317), soft beige (#F5DEB3)
- Accents: gold frame (#FFD700), navy blue (#000080)

ATMOSPHERE: Warm, academic, modest but respected. The office of a professor who values substance over show.

AVOID:
- Too modern or luxurious
- Cold or impersonal atmosphere
- Generic "Asian" style (must be specifically KOREAN)
```

---

### 2.2 停机坪

**文件**：`Resources/bg/helicopter_pad.png`  
**用途**：序章 Day 0 领取载具场景

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio, STARDEW VALLEY STYLE PIXEL ART, university rooftop helipad, Kim Il Sung University, Pyongyang, 2076, evening.

ARCHITECTURAL ELEMENTS:
- University rooftop helipad
- Several sand-energy flying vehicles parked
- Deep blue sand vehicles with university emblem
- Control tower in background
- Korean-style rooflines visible below

VIEW (PYONGYANG SKYLINE):
- Ryugyong Hotel in distance (pyramid shape)
- Juche Tower visible
- Grand People's Study House with traditional Korean roof
- Mix of traditional Korean architecture and modern buildings
- Mountains with Korean pine trees in far distance
- Taedong River visible
- Flying vehicles in distance

COLOR PALETTE:
- Primary: evening sky gradient (#FF6B35 to #1A1A2E)
- Secondary: deep blue vehicles (#000080), warm sunset (#FFD700)
- Accents: university emblem colors, landing pad lights

ATMOSPHERE: Transition from day to night, sense of departure. The last moment before a long journey begins.

AVOID:
- Too bright or cheerful (should be evening)
- Missing the specific landmarks
- Generic city skyline (must be specifically Pyongyang)
```

---

### 2.3 边境小镇

**文件**：`Resources/bg/border_town.png`  
**用途**：序章 Day 1 第一次补给

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio, STARDEW VALLEY STYLE PIXEL ART, Chinese border town morning, 2076, small supply station.

ARCHITECTURAL ELEMENTS:
- Small border town in morning fog
- Supply station with fuel-pump-like equipment
- Simple buildings, provincial Chinese style
- Mountains in background
- Morning mist

COLOR PALETTE:
- Primary: morning mist gray (#D3D3D3), warm dawn (#FFDAB9)
- Secondary: earthy brown (#8B7355), building gray (#696969)
- Accents: supply station sign colors

ATMOSPHERE: Quiet border town morning, the first stop on a long journey. Simple, functional, slightly sleepy.

AVOID:
- Too modern or developed
- Busy or crowded scene
- Nighttime or dark setting
```

---

### 2.4 雾峰村夕阳

**文件**：`Resources/bg/village_sunset.png`  
**用途**：序章 Day 4 到达雾峰村

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio, STARDEW VALLEY STYLE PIXEL ART, mountain village at sunset, Wufeng village, central China, 2076, misty tea village.

ARCHITECTURAL ELEMENTS:
- Mountain valley village
- Small houses with traditional Chinese roofs
- Terraced tea gardens on hillsides
- Railway line running through village
- Smoke rising from chimneys
- Distant mountains with mist

VIEW:
- Entire village visible from above
- Railway line as central feature
- Tea terraces on surrounding hills
- Small station building visible
- Winding road connecting to outside

COLOR PALETTE:
- Primary: golden sunset (#FFD700), warm orange (#FF8C00)
- Secondary: mountain mist blue (#87CEEB), tea green (#556B2F)
- Accents: house roof gray (#808080), chimney smoke (#D3D3D3)

ATMOSPHERE: Warm, nostalgic, the home that has been waiting. A village that time forgot, beautiful in its isolation.

AVOID:
- Too bright or modern
- Missing the tea terraces (essential character)
- Cold or unwelcoming atmosphere
```

---

### 2.5 车站夕阳

**文件**：`Resources/bg/station_sunset.png`  
**用途**：序章 Day 4 到达车站

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio, STARDEW VALLEY STYLE PIXEL ART, old railway station at sunset, Wufeng village, 2076.

ARCHITECTURAL ELEMENTS:
- Small old railway station
- Platform with benches
- Tracks extending into distance
- Weeds growing between tracks
- Station building with Chinese rural style
- Signal post

COLOR PALETTE:
- Primary: golden sunset (#FFD700), warm orange (#FF8C00)
- Secondary: station building gray (#696969), rusted tracks (#8B4513)
- Accents: weeds green (#556B2F), sky pink (#FFB6C1)

ATMOSPHERE: Nostalgic, slightly melancholic but hopeful. The railway station that has seen better days, waiting for someone to bring it back to life.

AVOID:
- Too clean or well-maintained
- Busy or crowded station
- Modern or renovated appearance
```

---

### 2.6 傍晚站台

**文件**：`Resources/bg/platform_evening.png`  
**用途**：序章 Day 4 老陈等候

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio, STARDEW VALLEY STYLE PIXEL ART, station platform at evening, Wufeng village, 2076.

ARCHITECTURAL ELEMENTS:
- Railway platform at evening
- Warm platform lighting
- Old man waiting on platform
- Train approaching in distance
- Evening sky

COLOR PALETTE:
- Primary: evening blue (#191970), warm light (#FFD700)
- Secondary: platform gray (#808080), train dark (#1A1A1A)
- Accents: station lamp glow (#FFA500)

ATMOSPHERE: Quiet, expectant, the moment of reunion after years apart.

AVOID:
- Too bright or cheerful
- Empty or abandoned feeling
- Missing the warm lighting
```

---

## 三、BGM（Suno AI 音乐提示词）

### 通用规范

- **风格**：纯音乐，无歌词，instrumental
- **平台**：Suno AI
- **格式**：风格描述 + 情绪 + 乐器 + 参考风格

---

### 3.1 melancholy（忧郁·怀旧）

**Suno 提示词**：
```
Style: Ambient piano, lo-fi, melancholic, nostalgic instrumental
Mood: Bittersweet, reflective, gentle sadness
Instruments: Solo piano, soft strings, ambient pad
Tempo: Slow, 60 BPM
Reference: Stardew Valley winter theme, "To the Moon" soundtrack
Description: A slow, contemplative piano piece with soft string pads. The melody carries a sense of loss but also quiet hope. Perfect for reflective moments and memories of the past.
```

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

### 3.3 determination（坚定·希望）

**Suno 提示词**：
```
Style: Cinematic orchestral, uplifting, determined instrumental
Mood: Hopeful, resolute, inspiring
Instruments: Orchestra, brass, percussion, piano
Tempo: Moderate, 80 BPM
Reference: "Interstellar" main theme, "The Last of the Mohicans" theme
Description: A determined orchestral piece that builds gradually. Starts with a single piano note, then layers in strings, brass, and percussion. The feeling of setting out on an impossible journey.
```

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

### 3.6 news（新闻播报）

**Suno 提示词**：
```
Style: News broadcast, serious, informative instrumental
Mood: Serious, formal, anticipatory
Instruments: Electronic pads, low bass, subtle percussion, minimal piano
Tempo: Moderate, 80 BPM
Reference: Classic news theme music, "Black Mirror" soundtrack
Description: A serious, slightly tense news broadcast theme. The music signals that important information is being delivered. Not alarmist, but weighty.
```

### 3.7 calm（平静·日常）

**Suno 提示词**：
```
Style: Ambient, calm, everyday instrumental
Mood: Peaceful, neutral, unobtrusive
Instruments: Soft piano, ambient pad, gentle guitar
Tempo: Slow, 60 BPM
Reference: "Minecraft" ambient music, Stardew Valley farm theme
Description: A calm, unobtrusive ambient piece that creates a peaceful atmosphere without demanding attention. Perfect for everyday management scenes and thinking time.
```

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

---

## 四、生成优先级

| 优先级 | 资产 | 原因 | 数量 |
|--------|------|------|------|
| **P0** | 老陈主图+8表情 | 序章大量使用，MVP阻塞 | 1+8张 |
| **P0** | 张工主图+6表情 | 序章员工集合场景 | 1+6张 |
| **P0** | 王小弟主图+6表情 | 序章员工集合场景 | 1+6张 |
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
| **P1** | 李阿姨主图+6表情 | 序章员工集合 | 1+6张 |
| **P1** | 赵师傅主图+6表情 | 序章员工集合 | 1+6张 |
| **P1** | 小芳主图+6表情 | 序章员工集合 | 1+6张 |
| **P1** | 岁月界面头像 | 序章 | 1张 |
| **P1** | BGM: warm/calm/news/train_ambient | 场景补充 | 4首 |

---

*本文档按最新标准（v2.0 分类清单版）重写，基于实验室成功提示词格式。*