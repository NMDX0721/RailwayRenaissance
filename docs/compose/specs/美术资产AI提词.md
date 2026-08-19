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
- **表情集**：全部角色统一 16 种表情：`normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink`（以主角已完成16表情为基准，以下各节同）
- **人数约束（强制）**：主图必须是**单个人物**立绘——每条主图提示词首行已含 `SINGLE CHARACTER, ONE PERSON ONLY, no other people, no duplicates, no character sheet`。若生成工具仍输出参考图集/多人场景，在负面提示词外加 `multiple people, character sheet, reference sheet, turnarounds, duplicated character, multi-figure`。
- **差分工作流**：主图验收通过后，用"16表情差分图谱"提示词（见 1.9）一次产出 4×4 网格，再按需裁剪；勿逐表情单张生成。

---

### 1.1 林彪悍（主角）— 已有16表情，不需再生成

**文件**：`Resources/characters/lin_biaohan/{表情}.png`  
**状态**：✅ 已完成（16表情）

---

### 1.2 老陈（陈守正）

**文件**：`Resources/characters/laochen/{表情}.png`  
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词**：
```
pixel art, 16-bit retro style, SINGLE CHARACTER, ONE PERSON ONLY, full body character portrait, isolated on transparent background, no other people, no duplicates, no character sheet, STARDEW VALLEY STYLE PIXEL ART, elderly male, 68 years old.

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
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词**：
```
pixel art, 16-bit retro style, SINGLE CHARACTER, ONE PERSON ONLY, full body character portrait, isolated on transparent background, no other people, no duplicates, no character sheet, STARDEW VALLEY STYLE PIXEL ART, elderly male, 62 years old.

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
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词**：
```
pixel art, 16-bit retro style, SINGLE CHARACTER, ONE PERSON ONLY, full body character portrait, isolated on transparent background, no other people, no duplicates, no character sheet, STARDEW VALLEY STYLE PIXEL ART, middle-aged female, 55 years old.

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
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词**：
```
pixel art, 16-bit retro style, SINGLE CHARACTER, ONE PERSON ONLY, full body character portrait, isolated on transparent background, no other people, no duplicates, no character sheet, STARDEW VALLEY STYLE PIXEL ART, young male, 22 years old.

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
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词**：
```
pixel art, 16-bit retro style, SINGLE CHARACTER, ONE PERSON ONLY, full body character portrait, isolated on transparent background, no other people, no duplicates, no character sheet, STARDEW VALLEY STYLE PIXEL ART, middle-aged male, 55 years old.

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
**表情**：normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink（16种）

**主图提示词**：
```
pixel art, 16-bit retro style, SINGLE CHARACTER, ONE PERSON ONLY, full body character portrait, isolated on transparent background, no other people, no duplicates, no character sheet, STARDEW VALLEY STYLE PIXEL ART, middle-aged female, 45 years old.

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

---

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
- Primary: Korean celadon green (#8FBC8F), warm brown (#8B4513)
- Secondary: Cream white (#FFFDD0), soft red (#CD5C5C)
- Accents: Gold (#FFD700), navy blue (#000080)
- Dancheong colors: Blue (#4169E1), Red (#CD5C5C), Yellow (#FFD700), Green (#228B22)
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
- Primary: warm brown (#8B4513), cream white (#FFFDD0)
- Secondary: dark wood (#5C3317), soft beige (#F5DEB3)
- Accents: gold frame (#FFD700), revolutionary red (#CC0000)
- Dancheong: Blue (#4169E1), Red (#CD5C5C)
- North Korean official red (#C41E3A) for banners and flags

ATMOSPHERE:
- Warm, academic, modest but respected
- KOREAN WARMTH AND HOSPITALITY (정 warm heart)
- Serious yet caring — moment of a life-changing decision
- OFFICIAL NORTH KOREAN ACADEMIC SETTING
- State professor's office at the most prestigious university in the DPRK
```

KOREAN ARCHITECTURAL ELEMENTS:
- Traditional KOREAN HANOK STYLE window frames with wooden lattice patterns
- Door frame with traditional Korean decorative patterns (단청 dancheong colorful patterns)
- Wall panels with subtle Korean traditional motifs
- Ceiling with traditional Korean wooden beams (modern interpretation)
- Korean-style floor heating (온돌 ondol) visible under desk

KOREAN TEXT AND SIGNS:
- Korean text on wall: "김일성대학교 교수 연구실" (Professor's Office)
- University emblem with Korean text: "김일성대학교"
- Korean flag (North Korean flag) on wall
- Small nameplate on desk with Korean text
- Korean calendar on wall showing 2076

WALL DECORATIONS (ACADEMIC):
- Framed university diploma with Korean text
- Several academic certificates and awards on wall (Korean text)
- Map of Korean peninsula on wall
- Bookshelf filled with Korean technical books and railway engineering texts
- Traditional Korean paper art (minhwa) frame with folk painting
- Korean calligraphy scroll (서예) on wall with inspirational message

KOREAN TRADITIONAL ITEMS:
- Korean celadon pottery (고려청자 Goryeo celadon) on shelf
- Korean traditional paper (한지 hanji) desk lamp with warm glow
- Small Korean flag on desk
- Korean traditional tea set (전통다구) with celadon teapot and cups
- Korean traditional ink stone (벼루 byeoru) and brush (붓 but) on desk

KOREAN FURNITURE:
- Korean traditional wooden desk with traditional joinery, slightly worn
- Professor's wooden chair with curved backrest and cushion
- Korean traditional wooden cabinet (장롱 jangnok) with brass fittings
- Bookshelf with traditional Korean design

DESK ITEMS:
- Stack of student research papers with Korean text
- Open railway engineering textbook (Korean text, worn pages)
- Copper reading glasses on desk
- Korean traditional clock (시계) with hanja numbers
- Half-empty cup of Korean barley tea (보리차)
- Korean-style pen holder with traditional design
- Small photo frame with university graduation photo

BOOKSHELF CONTENTS:
- Railway engineering textbooks (Korean titles)
- Research papers on intelligent dispatch systems
- Korean history books
- Vintage train model
- Traditional Korean ceramic pieces

WINDOW VIEW (PYONGYANG SKYLINE):
- Ryugyong Hotel (려명거리 Hotel, pyramid shape, distant)
- Juche Tower (주체사상탑) visible in distance
- Grand People's Study House (인민대학습당) with traditional Korean roof
- Mix of traditional Korean architecture and modern buildings
- Korean pine trees (소나무 sonamu) visible
- Afternoon daylight, soft shadows through hanji-patterned windows

COLOR PALETTE (KOREAN TRADITIONAL):
- Primary: warm brown (#8B4513), cream white (#FFFDD0)
- Secondary: dark wood (#5C3317), soft beige (#F5DEB3)
- Accents: gold frame (#FFD700), navy blue (#000080)
- Dancheong accent colors: Blue (#4169E1), Red (#CD5C5C)

ATMOSPHERE:
- Warm, academic, modest but respected
- KOREAN WARMTH AND HOSPITALITY (정 warm heart)
- Serious yet caring — the moment of a life-changing decision
- Blend of tradition and academic modernity
- Respectful atmosphere with a sense of gravity

STYLE:
- PIXEL ART with clear pixels
- KOREAN CULTURAL IDENTITY very prominent
- Warm, inviting academic colors
- Clean, organized space
- Mix of traditional Korean aesthetics and university setting

AVOID:
- Generic "Asian" style (must be specifically KOREAN)
- Too modern or luxurious
- Cold or impersonal atmosphere
- Cluttered or messy composition
- Missing Korean cultural elements
- Too bright or cheerful (should be warm but serious)
```

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
- Primary: dark olive green (#4A5D23), instrument panel gray (#4a4a5a)
- Secondary: sky blue (#87CEEB), holographic cyan (#00BFFF)
- Accents: Korean red (#CD2626), celadon green (#7CB08A), traditional gold (#DAA520)
- Obangsaek five colors: blue (#2050A0), red (#CD2626), yellow (#FFD700), white (#F5F5F5), black (#1A1A1A)
- Leather: dark brown (#3B1F0B)

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
- Primary: deep dark blue (#0a0a1e), instrument glow amber (#FF8C00)
- Secondary: starry sky (#1a1a3e), holographic cyan (#00BFFF), city lights warm (#FFD700)
- Accents: Korean red (#CD2626) on emergency labels, celadon green (#7CB08A) on dashboard trim
- Obangsaek five colors muted in darkness: blue (#2050A0), red (#8B0000), yellow (#B8860B)

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
- Primary: warm beige (#D4C5A9), olive green (#4A5D23), dark window frame (#3A3A3A)
- Secondary: sky blue (#87CEEB), laptop screen glow (#E0F0FF)
- Accents: food orange (#FF8C00), soju green (#4CAF50), banana milk yellow (#FFFDD0), Korean red (#CD2626)

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
- Primary: warm amber (#8B6914), dark blue (#0A0A1E)
- Secondary: starry sky (#1A1A3E), reading light warm (#FFD700)
- Accents: laptop sleep light (#00BFFF), charging LED (#32CD32)

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
"
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
- Primary: bright blue sky (#87CEEB), deep blue vehicle (#000080, #1A237E)
- Secondary: university emblem gold (#DAA520), concrete grey (#808080)
- Accents: Korean red (#CD2626), building white (#F5F5F5), roof tiles dark brown (#3E2723)
- Landing pad: concrete grey (#808080, #696969)
- Building: traditional roof tiles dark brown (#3E2723)

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

### 2.8 边境小镇

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

### 2.9 雾峰村夕阳

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

### 2.10 车站夕阳

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

### 2.11 傍晚站台

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

### 2.12 大同江茶馆（嘉颖徐会面室内）

**文件**：`Resources/bg/tea_house.png`  
**用途**：序章嘉颖徐会面场景——平壤江南区高级会员制茶馆/私人办公室

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
- Primary: deep mahogany brown (#3E2723), dark wood (#5D4037), gold trim (#DAA520)
- Secondary: window night sky (#1A237E), warm amber light (#FFD700)
- Accents: celadon green (#8BC34A), burgundy leather (#800020), orchid purple (#9C27B0), Korean red (#CD2626)

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
- Primary: warm gold (#D4A017), brick red (#8B2500), pine green (#2F4F2F)
- Secondary: cream white (#FFFDD0), navy blue (#1B2A4A)
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

### 2.14 群山天空（mountain_sky）

**文件**：`Resources/bg/mountain_sky.png`
**用途**：序章 Day 4 下午——0721飞越边境后，进入中国境内群山之上。林彪悍第一次从高空俯瞰"小时候听爷爷讲的那片山"。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
aerial view high above mountain ranges, afternoon,
NO MAN-MADE OBJECTS visible except a distant railway line carved into the valley:

SCENE: Seen from a flying vehicle high above. Endless green mountain ranges stretch to the horizon, layered ridges fading into atmospheric haze. A thin railway line snakes along one valley floor far below — the only sign of human presence. Afternoon light, bright and clear.

LAYOUT:
- Lower two-thirds: layered mountain ridges (foreground ridges darker green, distant ones fading to blue-grey)
- A snaking railway line with small tunnels visible in the nearest valley
- Upper third: clear afternoon sky with scattered cumulus clouds
- A few distant flying vehicles as tiny silhouettes (optional, for scale)

COLORS:
- Ridges: deep forest green (#2E5D3A) → misty blue-grey (#7A8B99)
- Railway: dark grey line with occasional rust-brown bridge sections
- Sky: clear blue (#5FA8E0), white clouds
- Atmosphere haze: pale blue-white (#DCE8F0)

MOOD: The journey home. Vast, quiet, hopeful.
```

---

### 2.15 空中俯瞰·铁轨（aerial_view）

**文件**：`Resources/bg/aerial_view.png`
**用途**：序章 Day 4——"那是……铁路！"林彪悍在航线上第一次看见雾峰线的身影。这条线是爷爷守了一辈子的东西，是"归乡"的视觉锚点。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
aerial view looking down at a narrow mountain valley railway, late afternoon:

SCENE: Aerial view of a 23km mountain branch line cutting through forested foothills. Two or three tiny stations along the line (tiles roofs, small platforms). A stream crosses under a stone arch bridge. The line is old, single-track with rusted rails still gleaming in low sun. A tiny haze of cooking smoke rises from a village at the valley mouth — WuFeng village in the distance.

LAYOUT:
- Railway line as the visual spine: enters bottom frame, curves left, exits top
- 2-3 small stations along the line, each with a short platform and one building
- Stone arch bridge carrying track over a stream
- Village cluster at valley mouth (grey-roof houses, one tall chimney)
- Surrounding: forested hills, terraced fields, a winding dirt road

COLORS:
- Rails: rust brown-grey (#6B5B4F) with warm highlights
- Station roofs: grey-blue tiles (#4F6F7A)
- Fields: gold-green (#8FA94E)
- Late afternoon warm wash over everything

MOOD: Seeing it again after years. A line that should be dead, still alive.
```

---

### 2.16 雾峰村夕阳（village_sunset）

**文件**：`Resources/bg/village_sunset.png`
**用途**：序章 Day 4 抵达前——雾峰村全景，林彪悍的"老家"。村口的老槐树、土墙、炊烟，2076 年仍保持着铁路时代前的样子。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
mountain village panorama at sunset, seen from a low hill above:

SCENE: A small mountain village cradled in a valley. Grey-tiled house roofs with whitewashed walls, an old tree at the village entrance, evening cooking smoke rising from several chimneys. A flagged dirt road winds into the village. Behind it, terraced fields climb the foothills. Warm sunset light paints everything gold-orange.

LAYOUT:
- Mid-distance village spread across the frame (15-20 houses clustered)
- Old entrance tree (willow/elm) at frame left, villagers' washing lines
- Terraced fields behind, dark green
- Mountain walls closing in both sides
- Evening sky with warm clouds

COLORS:
- Roofs: warm grey (#8A7F7A), walls: white-washed with patina (#D9CBB8)
- Fields: dark moss green (#4C6B3E)
- Sunset: orange (#E8974A) → deep blue shadow (#3D5175)
- Smoke: pale lavender-grey

MOOD: Home, unchanged. The village time forgot.
```

---

### 2.17 车站夕阳（station_sunset）

**文件**：`Resources/bg/station_sunset.png`
**用途**：序章 Day 4 降落与 Day 7 收尾共用——雾峰站全景。老站房、站牌、月台、停着的 NF-5 耕牛。这条线"最后的车站"。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
small mountain railway station at sunset, wide establishing shot:

SCENE: A humble single-platform railway station at dusk. A low station building with a faded "雾峰" sign (Chinese characters), a wooden platform bench, signal post with rusty arm, and the NF-5 diesel locomotive waiting on the single track (dark green, weathered). Platform overgrown at edges with wild grass. Warm low sun from the left.

LAYOUT:
- Station building left-center (grey brick, red-tile roof, faded painted characters)
- Wooden platform edge running across mid-frame
- NF-5 locomotive on track right, nose toward viewer-left
- Freight cars (2) behind it, weathered
- Old luggage cart, stacked sacks by platform
- Chinese-language signs only

COLORS:
- Building: grey brick (#9A8F84), roof: faded red (#B4684D)
- Platform: worn concrete warm grey (#A8998B)
- Locomotive: dark green (#2F4A38) with rust accents
- Sky: orange-pink sunset

MOOD: The last station on a dying line — but tonight, someone came back.
```

---

### 2.18 傍晚站台（platform_evening）

**文件**：`Resources/bg/platform_evening.png`
**用途**：序章 Day 4 林彪悍下车踏上月台的近景——"到家了"的瞬间。与 station_sunset 的区别：本图为站台近景特写。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
close view standing ON the platform at dusk, facing the station building:

SCENE: Ground-level platform view. Weathered wooden platform surface in foreground, station building facing the viewer (faded 雾峰 characters, two windows with warm lamplight inside), a green signal lamp glowing. Evening shadows long. A pair of patched leather work gloves and a thermos left on the platform bench — someone was here recently.

LAYOUT:
- Foreground: platform planks, growing weeds between boards
- Mid: station building facade (4-5m wide), warm window light
- Left: signal post with lit green lamp
- Bench with forgotten gloves & thermos
- Background: fading sky

COLORS:
- Planks: worn brown (#7A6248), weeds: grey-green (#68754B)
- Building: whitewash patched grey (#C0B5A5), lamplight warm yellow (#F2C86B)
- Gloves: worn leather brown

MOOD: Stepping onto home ground. The station is old, poor, and still lit.
```

---

### 2.19 边境小镇（border_town）

**文件**：`Resources/bg/border_town.png`
**用途**：序章 Day 1 上午——临江市（中朝边境边贸城）上空视野。0721 补沙前短暂停留。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
border trade town seen from a low flying vehicle, morning:

SCENE: A small border-commerce town straddling both sides of a river (the Yalu-style river in the far background). North side: formal, grey, sparse. A market street with Russian-Korean-Chinese trilingual shop signs, bales of goods, hand carts. Grey river with ferry boats. Morning light.

LAYOUT:
- Town market street across mid-frame
- River and far bank in upper background
- Sand-energy supply station (fuel-pump-like sand dispensers) bottom-left corner
- Vehicles parked, porters with bundles

COLORS:
- Buildings: mixed grey/brick/white
- Signs: faded red/blue letters
- River: pale grey-green (#86A88A)
- Morning sky: cool blue

MOOD: The border between two worlds. A place where everything is for sale.
```

---

### 2.20 补给站（supply_station）

**文件**：`Resources/bg/supply_station.png`
**用途**：序章 Day 1——临江补给站近景。0721 补充 1500kg 沙子的地方，员工那句"沙子便宜"的背景。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
small sand-energy refuel station, day:

SCENE: A modest refueling station in a border town. Several sand-dispenser machines (like oversized fuel pumps but dispensing grey sand from hoppers) under a simple corrugated-iron canopy. A young worker in a worn uniform. A handwritten price board: 「沙 150沙币/1500kg」. Dusty ground, parked hand trolleys.

LAYOUT:
- 2-3 sand dispensers with hoppers in foreground
- Price board with Chinese handwriting
- Worker figure + owner standing by
- Background: town wall, distant buildings

COLORS:
- Dispensers: faded military green / rust orange
- Sand: pale grey-tan (#C9BFAC)
- Canopy: rusty corrugated (orange-brown)
- Chinese text only

MOOD: Mundane commerce. Sand, the new oil, sold by weight.
```

---

### 2.21 中国上空（china_sky）

**文件**：`Resources/bg/china_sky.png`
**用途**：序章 Day 1-3 多次使用——飞越中国领空的云海/天空镜头，新闻阅读与闲聊的背景。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
view from a flying vehicle above a sea of clouds, daytime:

SCENE: Abundant white cloud sea stretching to the horizon under clear blue sky. Rolling cumulus clouds below, sunlight sparkling. A tiny bit of terrain (rivers/plains) barely visible through gaps. No vehicles visible (or one tiny silhouette optionally).

LAYOUT:
- 80% cloud sea below horizon line
- Clear blue sky above
- Sun flare option, light haze
- Subtle motion feel

COLORS:
- Clouds: bright white (#FFFFFF) shadows soft blue-grey (#B8C6DB)
- Sky: deep blue (#4A90D9)
- Terrain glimpses: hazy green-grey

MOOD: Open sky, long journey ahead. Neutral, contemplative.
```

---

### 2.22 河北小镇（hebei_town）

**文件**：`Resources/bg/hebei_town.png`
**用途**：序章 Day 2 下午——飞越河北上空时俯瞰的华北平原小镇。

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
```

---

### 2.23 河南小镇（henan_town）

**文件**：`Resources/bg/henan_town.png`
**用途**：序章 Day 3 傍晚——飞越河南，中原大地景象。

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
- Fields: dry gold (#C8A24B)
- Roofs: grey (#8B8B8B) with patina
- Water: orange reflect (#E0804A)
- Sky: warm amber

MOOD: Endless flat land, the long road south.
```

---

### 2.24 车辆段·晨（depot_morning）

**文件**：`Resources/bg/depot_morning.png`
**用途**：序章 Day 5 早晨——雾峰站车辆段，张工拆修 NF-5 喷油嘴的检修棚。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
small railway depot maintenance shed, early morning:

SCENE: An open-sided maintenance shed in the depot yard. One inspection pit, workbench with tools (wrenches, oily rags, a flashlight, parts trays), a hoist chain, oil-stained concrete floor. NF-5's front end pokes into the shed. Morning sun slants through the shed opening, dust motes in light beams. A faded slogan on the wall: 「安全第一」.

LAYOUT:
- Shed structure framing the scene
- Inspection pit + workbench right
- NF-5 locomotive nose left, hood open
- Morning light beams
- Tool cart, oil drums

COLORS:
- Shed: rusted steel frame, patched roof
- Bench tools: worn metal blue/red handles
- Light beams: warm white with dust
- Floor: oil-darkened concrete

MOOD: Old hands, old tools, old machine. The first repair in years.
```

---

### 2.25 线路巡视（railway_track）

**文件**：`Resources/bg/railway_track.png`
**用途**：序章 Day 4 傍晚——林彪悍沿雾峰线徒步巡视。铁轨近景，锈迹与杂草，道砟与枕木。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
ground-level view along an abandoned single-track railway, late evening:

SCENE: Standing ON the railway line looking along it (low angle, rail perspective line vanishing to distance). Rusted rails on dark wooden sleepers, ballast stones overgrown with weeds and wildflowers, a sagging signal post ahead, distant station silhouette. Long evening shadows across the track.

LAYOUT:
- Rails converging to vanishing point (visual focus)
- Sleepers slightly rotten, some replaced with patched wood
- Weeds: tall grass, yellow wildflowers between tracks
- Signal post leaning, arm down
- Distant village roofs / station building

COLORS:
- Rails: rust orange-brown (#8A5A3B), patched sleepers dark brown
- Ballast: grey with moss
- Sky: orange-pink dusk
- Wildflowers: gold & lavender

MOOD: Walking through memory. The line is dead — but the iron remembers.
```

---

### 2.26 松桥站（songqiao_station）

**文件**：`Resources/bg/songqiao_station.png`
**用途**：序章 Day 4 巡视途中——支线小站"松桥"（铁路博物馆候选站），破旧但仍立着。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
tiny abandoned station on a branch line, late evening:

SCENE: A one-room station building, paint flaking, the name board「松桥」faded. A single short platform with grass growing through cracks. A dead flower bed, a broken water pump, an old bench. Beyond the platform, a stone-arched bridge over a stream carrying the railway. Everything old, quiet, dignified.

LAYOUT:
- Station building mid-frame (small, one window ajar)
- Platform edge with weeds
- Stone arch bridge behind carrying the line over water
- Overgrown ticket office window, mossy sign

COLORS:
- Building: peeling whitewash over brick
- Sign: faded green characters on white
- Bridge: grey stone with moss
- Stream: dark jade

MOOD: A station that remembers being loved. (Museum candidate — keep intact.)
```

---

### 2.27 车站·夜（station_night）

**文件**：`Resources/bg/station_night.png`
**用途**：序章 Day 4 夜晚——雾峰站旧人重逢（张工/李阿姨/王小弟等聚首）的夜景。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
mountain station at night, lit windows, gathering atmosphere:

SCENE: Same station building as station_sunset but at night. Warm lamplight from the station master's office window, a light hanging over the platform, people silhouettes gathered by the door (no close-ups). Cool blue night outside, warm gold inside. A kettle steam plume from the chimney.

LAYOUT:
- Station building with 2-3 warm lit windows
- Platform lamp casting a pool of light
- 4-5 dim human silhouettes (heads/backs only) by the entrance
- Tea table visible through window (kettle, cups)
- Night sky with stars beginning to show

COLORS:
- Night: deep blue (#22314F), stars faint
- Window light: warm gold (#F5C36B)
- Silhouettes: dark with warm rim light

MOOD: The old team, home again. Warmth against the cold line.
```

---

### 2.28 站长办公室（station_office）

**文件**：`Resources/bg/station_office.png`
**用途**：序章 Day 6（首班车次日）——车站办公室，融资洽谈（市里扶持基金/乡亲集资）的室内。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
station master's office, morning:

SCENE: A cramped but homey office inside the station building. A heavy wooden desk with a green banker's lamp, an old rotary phone, a wall map of the line (23km, stations marked), framed group photo, a kettle on a coal stove, shelves with ledgers. A window overlooking the platform. Paperwork, a worn leather chair.「站长办公室」wooden sign by the door.

LAYOUT:
- Desk center (viewer side: papers, ink, stamp)
- Wall map of雾峰线 with red pen marks
- Rotary phone + telegram/cashbox
- Stove with kettle, tea smell implied
- Window to platform, morning light

COLORS:
- Wood: dark aged (#6B4A2F)
- Lamp light: warm green-glass glow
- Map: aged paper yellow
- Fabric: worn green leather

MOOD: The nerve center of the line. Old paperwork, new hope.
```

---

### 2.29 站台·晨（platform_morning）

**文件**：`Resources/bg/platform_morning.png`
**用途**：序章 Day 5 下午/首班车——雾峰站月台晨光，乘客等车的场景。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
station platform in morning light, first train day:

SCENE: The雾峰 platform under clear morning light. A few early passengers (villagers with baskets, a child on father's shoulders) gathered near the platform edge. Mist still lifting off the rails. NF-5 idling with soft diesel chug, steam wisps. The platform brushed clean, flower pots newly watered. Fresh, hopeful mood.

LAYOUT:
- Platform across mid-frame
- Passengers as small distant figures (no close-ups)
- NF-5 nose right, steam wisps
- Mist over rails
- Clean platform, flower pots, new「首班车」paper banner

COLORS:
- Morning gold light
- Rails gleaming, mist white-grey
- Passengers: colored cloth (red scarf, blue jacket)
- Sky: fresh blue

MOOD: The first run in years. Nervous, bright, alive.
```

---

### 2.30 车厢内部（train_inside）

**文件**：`Resources/bg/train_inside.png`
**用途**：序章 Day 5 首班车——SY-22 灰雀客车车厢内部，乘客乘坐的镜头。

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
interior of an old branch-line passenger coach, daytime:

SCENE: Interior of a small old railway coach (30-seat branch line car). Green padded bench seats along both sides facing inward, a narrow aisle, luggage racks with woven baskets and cloth bundles, ceiling fans, windows showing passing village scenery. Carriage walls aged cream with painted slogans「爱护公共设施」. Light sway implied.

LAYOUT:
- Aisle perspective, benches both sides
- Passengers' belongings in racks (baskets, umbrellas, bundles)
- Open windows, scenery blur outside
- Conductor strap hanging, bell pull

COLORS:
- Benches: worn green vinyl (#3E6B4A)
- Walls: cream patched (#E3D5B8)
- Racks: grey with colorful baskets
- Outside windows: bright green fields

MOOD: Ordinary people, ordinary journey. The railway alive again.
```

---

### 2.31 纯黑（black）

**文件**：无（引擎特殊色值，不留图片文件）
**用途**：序章开场黑屏/滚动新闻/章节转场。纯黑背景 + 字幕。

**说明**：`black` 不生成图片——引擎 `BackgroundManager` 对 `"black"` 应直接渲染纯黑（修复 `Resources.Load("bg/black")` 失败的警告路径）。若未来需要极简渐变可追加。

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
- Primary: warm gold (#DAA520) or warm brown (#8B6914)
- Outline: dark brown (#3E2723)

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
- 0721: navy blue (#1A237E) gold accents
- Enemy: military green (#3F4D2E), grey, charcoal black
- Interference: electric blue (#4FC3F7)
- Clouds: white with cool shadows
- Sky: pale blue stress-light

MOOD: Odds of escaping: 3%. Alone against four.

EXCLUSIONS:
- No dialogue
- No blood/violence (interceptors are unarmed-looking, blocking not shooting)
```

---


