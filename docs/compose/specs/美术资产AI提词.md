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
**用途**：序章 Day 0-Day 3 飞行器内部场景——岁月初次唤醒、旅途对话（9次使用）

**提示词**：
```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
flying vehicle interior, daytime, 2076 retro-futuristic,
KOREAN STYLE INTERIOR, NORTH KOREAN industrial design,
sand-energy vehicle cockpit, "Chollima" brand flying car:

SCENE: interior of a DPRK-made sand-energy flying vehicle during daytime flight, two seats (pilot + passenger), compact cabin, retro-futuristic North Korean design aesthetic blending 1970s Korean industrial design with near-future technology — ULTIMATE KOREAN ATMOSPHERE

CULTURAL CONTEXT — SIX LAYERS OF KOREANNESS:
- LAYER 1: Chollima brand — DPRK's premier vehicle manufacturer, named after the mythical thousand-li horse
- LAYER 2: Kim Il Sung University affiliation — university emblem, research institute stickers, academic decals
- LAYER 3: Juche ideology design language — self-reliance aesthetic, practical and functional, no foreign branding
- LAYER 4: Songun (military-first) influence — sturdy construction, military-grade switches, utilitarian design
- LAYER 5: Korean traditional arts — dancheong color patterns, maedeup knotwork, minhwa folk motifs
- LAYER 6: 2076 retro-futuristic — not too advanced, recognizable as Korean-made, slight Soviet-tech influence

COCKPIT LAYOUT:
- Pilot seat (left) and passenger seat (right), dark olive-green upholstery with DPRK emblem
- Dashboard with physical toggle switches and analog gauges (retro-futuristic, Korean-made, military-grade)
- Holographic display screen embedded in dashboard (岁月's interface, blue-tinted)
- Center console with sand-energy controls, navigation system, and propaganda radio
- Side windows on both sides showing sky and clouds (daytime, bright)
- Ceiling: padded headliner with small reading light, emergency handle
- Floor: dark rubber mat with raised ridges, utilitarian

KOREAN CULTURAL DETAILS — MAXIMUM DENSITY:
PATRIOTIC SYMBOLS:
- Small Korean flag (태극기) on dashboard, another on rearview mirror
- Portrait of Kim Il-sung and Kim Jong-il (small, pinned above dashboard) — IMPORTANT
- "위대한 령도자 김일성동지" (Great Leader Comrade Kim Il-sung) calligraphy plaque
- Chollima statue miniature (small bronze horse) on dashboard
- "100전 100승" (100 battles 100 victories) slogan sticker

TRADITIONAL KOREAN CRAFTS:
- Korean traditional knotwork (매듭 maedeup) in gold and red hanging from rearview mirror
- Dancheong-style color pattern (오방색 obangsaek: blue, red, yellow, white, black) on seat fabric trim
- Small minhwa folk painting (민화) of a tiger and magpie as decorative panel
- Celadon green (청자) color accent on dashboard trim
- Korean paper (한지) texture pattern on sun visor

TECHNOLOGY & CONTROLS:
- ALL labels in Korean (Hangul) — absolutely no English or Chinese
- Sand energy gauge: "모래 에너지 잔량" with sweeping needle, red zone at low
- Speedometer: "속도" with km/h markings in Hangul numerals
- Altitude display: "고도" with Hangul numerals
- Navigation screen: map of Korean peninsula labeled "조선반도" with Chinese border
- Radio: frequency dial with Korean station names (조선중앙방송, 평양FM)
- Warning stickers: "주의" (caution), "비상정지" (emergency stop)
- Engine temperature: "엔진 온도" with green-yellow-red zones
- Battery charge: "배터리 잔량" for auxiliary systems

PERSONAL ITEMS:
- Korean newspaper (로동신문) folded in door pocket
- Thermos with Korean text (김일성화) in cup holder
- Small Korean phrasebook and travel documents in side compartment
- University ID card (김일성대학교 학생증) clipped to sun visor
- Box of Korean matches (성냥) and ashtray (if smoking era)

WINDOW VIEW (DAYTIME):
- Bright blue sky with white clouds through windshield
- Mountainous landscape below — Korean-Chinese border region
- A few Chollima-brand sand-energy flying vehicles in distant sky
- Warm sunlight streaming through side windows
- Rice paddies and small villages visible far below

COLOR PALETTE (KOREAN TRADITIONAL + TECH):
- Primary: dark olive green (#4A5D23), instrument panel gray (#4a4a5a)
- Secondary: sky blue (#87CEEB), holographic cyan (#00BFFF)
- Accents: Korean red (#CD2626), celadon green (#7CB08A), traditional gold (#DAA520)
- Obangsaek five colors: blue (#2050A0), red (#CD2626), yellow (#FFD700), white (#F5F5F5), black (#1A1A1A)
- Leather: dark brown (#3B1F0B)

ATMOSPHERE:
- ULTIMATE KOREAN TECH AESTHETIC — unmistakably North Korean
- Safe and comfortable for long journey
- Sense of national pride and technological achievement
- Warm daylight illumination throughout cabin
- Compact but functional, every inch has a purpose
- Slight Soviet-industrial undertone mixed with Korean tradition

STYLE:
- PIXEL ART with clear pixels, STARDEW VALLEY style
- NORTH KOREAN INDUSTRIAL DESIGN LANGUAGE
- Every label and text in Korean (Hangul) — zero English
- Propaganda-era aesthetic blended with near-future technology
- Military-grade build quality visible in switches and materials

AVOID:
- Generic sci-fi interior (must be unmistakably NORTH KOREAN)
- Japanese or Chinese cultural elements
- English text anywhere in the cabin
- Modern minimalist aesthetic
- Too dark or gloomy
- Missing the all-important leader portraits
- Capitalist/Western design cues
```

---

### 2.4 驾驶舱·夜晚（0721号前舱）

**文件**：`Resources/bg/car_interior_night.png`  
**用途**：序章 Day 2 夜晚飞行器内部场景（4次使用）

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
- Missing Korean text on instruments
- Japanese or Chinese cultural elements
- English text anywhere
- Western/ capitalist design cues
```

---

### 2.5 客舱·白天（新绘—0721号后舱）

**文件**：Resources/bg/cabin_interior.png  
**用途**：0721号后舱——主角旅途主要活动空间，对话、进食、休息、睡眠

**设计说明**：0721号沙子飞猪号（约4.5m×1.8m，轿车大小）无独立厕所，中途在补给站解决。后舱2个座椅可完全放平形成简易床铺，供4.5天旅程中轮换休息。岁月全程自动驾驶，乘客只需吃喝睡。

**提示词**：
`
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
flying vehicle PASSENGER CABIN interior, daytime, 2076 retro-futuristic,
NORTH KOREAN VEHICLE INTERIOR — personal space in a DPRK-made vehicle,
"Sand Flying Pig" 0721 rear cabin:

SCENE: rear passenger cabin of a small DPRK-made sand-energy flying car — 2 comfortable seats (reclining, can form a bed), foldable table between them, large window showing only sky, personal items scattered around. This is where the protagonist actually lives during the 4.5-day journey. The base tone is North Korean vehicle design, but the occupant has made it their own.

CABIN LAYOUT:
- Two reclining seats (beige/cream upholstery, dark green piping), KIM IL SUNG UNIVERSITY emblem on headrests
- Seats can recline to form a flat sleeping surface — a thin blanket and small pillow on one seat
- Foldable table between seats, deployed during meal times, stowed for sleeping
- Large side window, rounded rectangular, dark frame — SKY ONLY, NO GROUND
- Small storage compartments under window and below seats
- Small netted pocket on seatback for magazines/documents
- Overhead reading light (warm yellow, individual)
- Small air vent above window, utilitarian Korean design
- No toilet — vehicle too small, pit stops at supply stations

NORTH KOREAN BASE AESTHETIC (PRIMARY TONE):
- Dark green/olive trim along cabin walls (standard DPRK vehicle color)
- Dancheong-patterned stripe along ceiling edge (subtle, traditional)
- "금연" (No Smoking) / "좌석벨트" (Fasten Seat Belt) signs in Korean
- KIM IL SUNG UNIVERSITY emblem on headrests
- Official vehicle ID plaque on door frame
- Chollima (천리마) brand emblem on cabin wall
- Functional, utilitarian design — North Korean industrial standard

PERSONAL ITEMS (ACCUMULATED DURING JOURNEY):
- Open laptop on table (Korean-made, 2076 model, dispatch algorithm code on screen)
- Notebook and pen next to laptop
- KOREAN FRIED CHICKEN box (BBQ 치킨) on table, some crumbs
- Half-empty soju bottle (처음처럼) and one small cup
- Shrimp chips (새우깡) bag, banana milk (바나나우유) bottle
- Seoul travel guide on the seat
- Duty-free shopping bag on floor, clothes peeking out
- K-pop photocards tucked into seat pocket
- Folded blanket and small pillow on one seat
- Phone charger cable draped across the table
- Water bottle in cup holder

WINDOW VIEW (DAYTIME — SKY ONLY):
- Bright blue sky with white clouds
- Occasional distant flying vehicle
- Warm sunlight streaming through window
- NO GROUND, NO MOUNTAINS, NO CITIES

COLOR PALETTE:
- Primary: warm beige (#D4C5A9), olive green (#4A5D23), dark window frame (#3A3A3A)
- Secondary: sky blue (#87CEEB), laptop screen glow (#E0F0FF)
- Accents: food packaging orange (#FF8C00), soju green (#4CAF50), banana milk yellow (#FFFDD0), Korean red (#CD2626)

ATMOSPHERE:
- Cozy, lived-in — this is where the protagonist actually spends 4.5 days
- North Korean base design made personal by the occupant
- Warm daylight, comfortable for long journey
- The clutter tells a story: a student traveling home, mixing work (laptop) with pleasure (snacks)

STYLE:
- PIXEL ART with clear pixels, STARDEW VALLEY style
- DPRK VEHICLE INTERIOR — North Korean design is the base, personal items are the overlay
- Every label in Korean (Hangul)
- Lived-in, not sterile

AVOID:
- Ground or landscape visible through window
- Too clean or empty (must look lived-in after 4 days)
- Luxury or premium interior
- English text
- Japanese or Chinese elements
`

### 2.6 客舱·夜晚（新绘—0721号后舱）

**文件**：Resources/bg/cabin_interior_night.png  
**用途**：0721号后舱——夜晚飞行，主角休息，岁月独白场景

**提示词**：
`
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART, clear pixels,
flying vehicle PASSENGER CABIN at night, 2076 retro-futuristic,
NORTH KOREAN VEHICLE INTERIOR at night, cozy personal space:

SCENE: same rear cabin as daytime — but at night. Reading light on, laptop closed, table cleared. One seat reclined into bed mode with blanket and pillow. Window shows starry sky. Warm yellow reading light creates a cozy, intimate atmosphere. The protagonist is asleep, or resting quietly.

NIGHTTIME DIFFERENCES:
- Cabin dark except for reading light (warm yellow, individual spotlight)
- Table cleared of food — only laptop (closed), notebook, water bottle remain
- One seat reclined flat with blanket and pillow (bed mode)
- Window shows dark night sky with stars and crescent moon — NO GROUND
- Snack bags dimly visible in storage compartments
- K-pop photocards on seat catching reading light
- Duty-free bag silhouette in corner
- Phone charger plugged in, faint blue charging light
- Empty soju bottle and chicken box in trash bag

COLOR PALETTE (NIGHT):
- Primary: warm amber (#8B6914), dark blue (#0A0A1E)
- Secondary: starry sky (#1A1A3E), reading light warm (#FFD700)
- Accents: laptop sleep mode light (#00BFFF), charging LED (#32CD32)

ATMOSPHERE:
- Quiet, intimate, restful
- End of a long travel day
- Safe and warm inside despite the dark outside
- Contemplative mood — protagonist rests while 岁月 flies on through the night
- The cabin feels smaller and more personal in the dark

AVOID:
- Too bright (must be clearly nighttime)
- Ground or city lights visible through window
- Too clean or sterile
- English text
### 2.7 停机坪

**文件**：`Resources/bg/hangar.png`  
**用途**：序章 Day 0 领取载具场景（脚本引用"hangar"）

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

### 2.12 Wiki 横幅（Wiki Banner）

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
