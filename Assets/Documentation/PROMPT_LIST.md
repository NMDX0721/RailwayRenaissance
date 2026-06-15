# 铁路复兴：沙能冲击 - AI绘图提示词总表 v2.1

## 风格统一要求
- **风格**：精致像素风，16位复古游戏风格
- **背景**：透明背景（所有素材必须输出PNG with alpha channel）
- **色调**：温暖怀旧风，主色#8B4513（棕）、#CD853F（铜）、#DAA520（金）、#2F2F2F（深灰）
- **参考游戏**：《星露谷物语》《泰拉瑞亚》《八方旅人》
- **像素密度**：每像素清晰可见，不使用抗锯齿，边缘锐利
- **Unity画布**：1920×1080，所有图标必须在此分辨率下清晰可辨

---

## ✅ 已完成素材（无需再做）

| 素材 | 文件名 | 状态 |
|------|--------|------|
| 标题Logo | title_logo.png | ✅ 已完成 |
| 背景图 | sunset_railway.png | ✅ 已完成 |
| 蒸汽火车 | steam_train.png | ✅ 已完成 |
| 主角立绘 | sprite_pack_preview_41da6335.png | ✅ 已完成 |
| 主要按钮 | button_primary.png | ✅ 已完成 |

---

## 🔄 P0 废弃素材重做

### 1. 通用输入框背景
**用途**：用户名输入框、密码输入框、搜索框等所有文本输入场景
**尺寸**：1200x120px
**废弃原因**：原图input_field.png有垂直分隔线瑕疵
**泛用性**：不内嵌任何文字或图标，由代码叠加placeholder和输入文本

```
Pixel art game UI input field background sprite, horizontal rectangular shape with exact dimensions 1200 pixels wide by 120 pixels tall, outer border is dark brown wooden frame 4 pixels thick on all four sides with color #5C3317, inner fill area is solid light beige parchment color #F5DEB3 with very subtle paper grain texture (1-2 pixel noise variation), completely flat design with no 3D effects no gradients no shadows no emboss, corners are sharp 90-degree angles not rounded, no text no icons no dividers no decorative elements inside the fill area, the border has slight wood grain texture running horizontally, transparent background outside the 1200x120 pixel bounds, game interface asset suitable for text input fields, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 2. 通用面板背景
**用途**：登录面板、注册面板、设置面板、自动登录面板等所有功能面板
**尺寸**：1600x1200px
**废弃原因**：原图panel_bg.png为1254x1254正方形，放大后边框过粗
**泛用性**：不内嵌任何文字，由代码叠加标题文字

```
Pixel art game UI panel background sprite, horizontal rectangular shape with exact dimensions 1600 pixels wide by 1200 pixels tall, outer border is ornate wooden frame 12 pixels thick on all four sides with dark brown color #3E2723 and visible wood grain texture running horizontally, four corner decorations are circular metal rivets 16x16 pixels each with copper color #B87333 and subtle highlight on top-left quadrant, inner fill area is semi-transparent dark brown #4A3728 at 85% opacity with subtle vertical wood panel texture (thin lines every 40-50 pixels), the border top edge has a small rectangular plaque area 200x40 pixels centered horizontally 20 pixels below the top border for placing title text (this area is slightly lighter color #8B6914), completely flat design with no 3D effects no gradients no shadows, no text no icons no functional elements inside the fill area, transparent background outside the 1600x1200 pixel bounds, game interface asset suitable for function panels, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 3. 通用按钮背景
**用途**：登录按钮、注册按钮、确认按钮、取消按钮等所有按钮场景
**尺寸**：800x120px（主按钮）、600x100px（次按钮）- 同一素材拉伸使用
**泛用性**：不内嵌任何文字，由代码叠加按钮文本

```
Pixel art game UI button background sprite, horizontal rectangular shape with exact dimensions 800 pixels wide by 120 pixels tall, outer border is wooden frame 3 pixels thick on all four sides with dark brown color #5C3317, inner fill area has vertical gradient from top #CD853F (copper) to bottom #8B4513 (dark brown) creating subtle 3D push-button effect, top edge has 1 pixel highlight line #DAA520 (gold), bottom edge has 1 pixel shadow line #3E2723 (dark shadow), corners are slightly rounded with 2 pixel radius, completely flat 2D design with no emboss no bevel no texture, no text no icons no decorative elements inside the fill area, transparent background outside the 800x120 pixel bounds, game interface asset suitable for clickable buttons, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

## ⭐ P1 小图标精灵表（64x64px）

### 4. 天气+状态图标精灵表
**用途**：天气状态显示（晴/雨/雪/风）+ 操作状态提示（错误/成功/警告）+ 简单装饰
**尺寸**：256x128px（4×2网格，每个图标64x64px）
**设计理由**：64x64px足够简单图标清晰显示，8个图标打包成一张精灵表

```
Pixel art game small icon sprite sheet, rectangular grid with exact dimensions 256 pixels wide by 128 pixels tall, divided into 8 equal cells each 64x64 pixels arranged in 4 columns by 2 rows, row 1 column 1: sun icon, circle 24 pixels diameter centered at (32,32) with 8 rays 8 pixels long extending outward every 45 degrees, bright warm yellow #FFD700, row 1 column 2: rain icon, cloud shape 30 pixels wide 18 pixels tall centered at (32,22) with 3 diagonal rain drops 6 pixels long below at positions (20,50) (32,56) (44,50), blue-gray #708090 cloud with blue #4169E1 rain drops, row 1 column 3: snow icon, cloud shape 30 pixels wide 18 pixels tall centered at (32,22) with 3 snowflake dots 4 pixels diameter below at positions (20,52) (32,58) (44,52), light blue #ADD8E6 cloud with white #FFFFFF snowflakes, row 1 column 4: wind icon, 3 horizontal wavy lines 36 pixels long 2 pixels thick centered vertically at y=22 y=32 y=42, light gray #D3D3D3, row 2 column 1: error icon, dark red #8B0000 circle outline 36 pixels diameter 2 pixels thick centered at (32,32) with white #FFFFFF X-mark 16 pixels tall inside, row 2 column 2: success icon, dark green #006400 circle outline 36 pixels diameter 2 pixels thick centered at (32,32) with white #FFFFFF checkmark 16 pixels tall inside, row 2 column 3: warning icon, dark yellow #B8860B circle outline 36 pixels diameter 2 pixels thick centered at (32,32) with white #FFFFFF exclamation mark 16 pixels tall inside, row 2 column 4: gear icon, silver #C0C0C0 circle 28 pixels diameter centered at (32,32) with 8 teeth 4 pixels each extending outward, all icons flat 2D design with no gradients no shadows, edges crisp with no anti-aliasing, transparent background outside icon shapes, game interface sprite sheet, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 5. 输入框图标精灵表
**用途**：用户名输入框左侧用户标识、密码输入框左侧钥匙标识
**尺寸**：128x64px（2×1网格，每个图标64x64px）
**设计理由**：64x64px确保图标清晰，2个图标打包成一张精灵表

```
Pixel art game input icon sprite sheet, horizontal strip with exact dimensions 128 pixels wide by 64 pixels tall, divided into 2 equal cells each 64x64 pixels, left cell: user icon, silhouette of person's head and shoulders, head is circle 20 pixels diameter centered at (32,22), shoulders are curved line 30 pixels wide centered at (32,42) extending to cell edges, single warm gold color #DAA520, right cell: key icon, old-fashioned skeleton key oriented vertically, bow is circle 14 pixels diameter centered at (32,18), shaft is vertical line 4 pixels wide 28 pixels tall centered at (32,32), bit is L-shape 10 pixels wide 6 pixels tall at bottom, single warm gold color #DAA520, all icons flat 2D design with no gradients no shadows, edges crisp with no anti-aliasing, transparent background outside icon shapes, game interface sprite sheet, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

## ⭐ P2 中图标精灵表（128x128px）

### 6. 资源图标精灵表
**用途**：资金/信任度/客流/车况四种核心资源显示图标
**尺寸**：256x256px（2×2网格，每个图标128x128px）
**设计理由**：资源图标是HUD核心元素，128x128px确保在各种UI位置清晰可辨

```
Pixel art game resource icon sprite sheet, square grid with exact dimensions 256 pixels wide by 256 pixels tall, divided into 4 equal cells each 128x128 pixels in 2x2 arrangement, top-left cell: gold coin icon for funds, circle 50 pixels diameter centered at (64,64) with embossed dollar symbol 20 pixels tall, warm golden yellow #DAA520 with subtle highlight #FFFACD at top-left quadrant creating 3D effect, 2 pixel dark brown #5C3317 outline, top-right cell: red heart icon for trust, heart shape 50 pixels tall centered at (64,64) with slight 3D effect, warm red #CC0000 with subtle highlight #FF6666 at top-left quadrant, 2 pixel dark red #8B0000 outline, bottom-left cell: people silhouette icon for passengers, 3 standing figures 40 pixels tall centered at (64,64) arranged in triangle formation, warm beige #F5DEB3 color with 1 pixel darker beige #D2B48C outline, bottom-right cell: wrench icon for condition, L-shaped wrench 50 pixels tall centered at (64,64), warm brown #8B4513 handle with copper #B87333 metal head, 1 pixel dark brown outline, all icons flat 2D design with minimal 3D effect through highlights, edges crisp with no anti-aliasing, transparent background outside icon shapes, game interface sprite sheet, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 7. 工具图标精灵表
**用途**：扫把/扳手/手电筒/安全帽四种员工工具图标
**尺寸**：256x256px（2×2网格，每个图标128x128px）
**设计理由**：工具图标在员工管理界面频繁使用，128x128px确保细节清晰

```
Pixel art game tool icon sprite sheet, square grid with exact dimensions 256 pixels wide by 256 pixels tall, divided into 4 equal cells each 128x128 pixels in 2x2 arrangement, top-left cell: broom icon for cleaning staff, vertical handle 10 pixels wide 50 pixels tall centered at x=64 from y=39 to y=89, bristle fan 40 pixels wide 20 pixels tall centered at (64,104), handle warm brown #8B4513 bristles tan #D2B48C with 1 pixel dark outline, top-right cell: wrench icon for repair staff, L-shaped wrench with 45 pixel handle 10 pixels wide and 25 pixel jaw 8 pixels wide centered at (64,64), silver #C0C0C0 metal with brown #8B4513 wooden grip section 15 pixels long, 1 pixel dark gray outline, bottom-left cell: flashlight icon for patrol staff, vertical cylinder 16 pixels wide 45 pixels tall centered at (64,74) with light cone 36 pixels wide 25 pixels tall above at (64,34), dark gray #2F2F2F body with bright yellow #FFD700 light cone fading to transparent, 1 pixel black outline on body, bottom-right cell: safety helmet icon for all staff, dome shape 55 pixels wide 30 pixels tall centered at (64,49) with brim 65 pixels wide 8 pixels tall below at y=79, bright yellow #FFD700 with orange #FF8C00 horizontal stripe 4 pixels tall at y=54, 1 pixel dark brown outline, all icons flat 2D design with minimal detail, edges crisp with no anti-aliasing, transparent background outside icon shapes, game interface sprite sheet, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 8. 道具图标精灵表
**用途**：车票/怀表/钥匙/日记四种重要道具图标
**尺寸**：256x256px（2×2网格，每个图标128x128px）
**设计理由**：道具图标在剧情和物品系统中使用，128x128px确保辨识度

```
Pixel art game item icon sprite sheet, square grid with exact dimensions 256 pixels wide by 256 pixels tall, divided into 4 equal cells each 128x128 pixels in 2x2 arrangement, top-left cell: train ticket icon, horizontal rectangle 55 pixels wide 28 pixels tall centered at (64,64) with perforated left edge (3 semicircles 5 pixels each), cream #FFF8DC color with brown #8B4513 printed horizontal lines 1 pixel thick every 6 pixels, 1 pixel dark brown outline, top-right cell: pocket watch icon for protagonist's heirloom, circle 42 pixels diameter centered at (64,52) with chain 5 pixels wide 28 pixels long extending downward to (64,92), golden brass #B87333 with round face and small black #1A1A1A hands showing 3:45, 1 pixel dark brown outline, bottom-left cell: old key icon, classic skeleton key 50 pixels tall centered at (64,64) with round bow 18 pixels diameter at top and simple bit 14 pixels at bottom, brass #B87333 with subtle highlight #DAA520 on bow, 1 pixel dark brown outline, bottom-right cell: diary icon, closed book 42 pixels wide 52 pixels tall centered at (64,64) with spine on left side and small brass clasp 4x4 pixels on right edge, dark brown #5C3317 cover with cream #FFF8DC pages visible 2 pixels at edges, 1 pixel black outline, all icons flat 2D design with minimal detail, edges crisp with no anti-aliasing, transparent background outside icon shapes, game interface sprite sheet, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 9. 信号灯精灵表
**用途**：铁路信号灯红/绿/黄三种状态显示
**尺寸**：384x128px（3×1网格，每个图标128x128px）
**设计理由**：信号灯在游戏事件和铁路系统中使用，三格横排便于状态切换

```
Pixel art game signal light sprite sheet, horizontal strip with exact dimensions 384 pixels wide by 128 pixels tall, divided into 3 equal cells each 128x128 pixels, left cell: red signal light, dark gray #2F2F2F vertical housing 36 pixels wide 72 pixels tall centered at (64,64) with 2 pixel black outline, circular light 28 pixels diameter centered at (64,40) with bright red #FF0000 fill and 2 pixel white #FFFFFF highlight at top-left quadrant, middle cell: green signal light, same housing design and position, circular light 28 pixels diameter centered at (64,40) with bright green #00FF00 fill and 2 pixel white highlight at top-left quadrant, right cell: yellow signal light, same housing design and position, circular light 28 pixels diameter centered at (64,40) with bright yellow #FFD700 fill and 2 pixel white highlight at top-left quadrant, all icons flat 2D design with minimal 3D effect through highlights, edges crisp with no anti-aliasing, transparent background outside icon shapes, game interface sprite sheet, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 10. 状态提示精灵表
**用途**：错误/成功/警告三种状态提示图标
**尺寸**：384x128px（3×1网格，每个图标128x128px）
**设计理由**：状态提示在所有UI反馈中使用，三格横排便于调用

```
Pixel art game status icon sprite sheet, horizontal strip with exact dimensions 384 pixels wide by 128 pixels tall, divided into 3 equal cells each 128x128 pixels, left cell: error icon, dark red #8B0000 circle outline 56 pixels diameter 3 pixels thick centered at (64,64) with white #FFFFFF X-mark 28 pixels tall inside centered at (64,64), middle cell: success icon, dark green #006400 circle outline 56 pixels diameter 3 pixels thick centered at (64,64) with white #FFFFFF checkmark 28 pixels tall inside centered at (64,64), right cell: warning icon, dark yellow #B8860B circle outline 56 pixels diameter 3 pixels thick centered at (64,64) with white #FFFFFF exclamation mark 28 pixels tall inside centered at (64,64), all icons flat 2D design with no gradients no shadows, edges crisp with no anti-aliasing, transparent background outside icon shapes, game interface sprite sheet, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

## ⭐ P3 大图标精灵表（256x256px）

### 11. 装饰元素精灵表
**用途**：铁轨分隔线、小火车、齿轮、烟雾四种装饰元素
**尺寸**：1024x256px（4×1网格，每个元素256x256px）
**设计理由**：装饰元素需要更多细节，256x256px确保视觉丰富度

```
Pixel art game decoration sprite sheet, horizontal strip with exact dimensions 1024 pixels wide by 256 pixels tall, divided into 4 equal cells each 256x256 pixels, left cell: railway track segment, two parallel dark brown #5C3317 rails 6 pixels thick separated by 40 pixels running horizontally across cell with wooden sleepers #8B4513 every 80 pixels 6 pixels thick, track centered vertically at y=128, second cell: small steam train side view facing right, red body #C0392B locomotive 120 pixels wide 80 pixels tall centered at (128,176) with brass dome #DAA520 20 pixels diameter on top and black smokestack #1A1A1A 8 pixels wide 30 pixels tall puffing white #FFFFFF steam clouds 40 pixels wide above, third cell: gear/cog icon, silver #C0C0C0 circle 100 pixels diameter centered at (128,128) with 12 teeth 16 pixels each extending outward, subtle metallic highlight #E8E8E8 at top-left quadrant, fourth cell: smoke puff particle effect, light gray #D3D3D3 cloud shape 120 pixels wide 80 pixels tall centered at (128,128) with 3 smaller puffs 30 pixels each above fading to transparent at edges, all elements flat 2D design with minimal shading, edges crisp with no anti-aliasing, transparent background outside element shapes, game decoration sprite sheet, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 12. 密码强度条
**用途**：注册时密码强度可视化指示
**尺寸**：400x24px（独占素材，包含强度分区文字）
**独占理由**：信息密度需求高，弱/中/强文字必须内嵌确保可读性

```
Pixel art game UI progress bar for password strength, horizontal rectangular shape with exact dimensions 400 pixels wide by 24 pixels tall, outer border is dark brown #5C3317 wooden frame 2 pixels thick on all four sides, inner area divided into three equal sections each 130 pixels wide (minus borders), left section filled with solid red #CC0000 representing weak password with embedded Chinese text "弱" 8 pixels tall white #FFFFFF color centered horizontally and vertically, middle section filled with solid yellow #DAA520 representing medium password with embedded Chinese text "中" 8 pixels tall white #FFFFFF color centered horizontally and vertically, right section filled with solid green #00AA00 representing strong password with embedded Chinese text "强" 8 pixels tall white #FFFFFF color centered horizontally and vertically, each section has subtle 1-pixel highlight at top edge and 1-pixel shadow at bottom edge, completely flat 2D design with no gradients no 3D effects, transparent background outside the 400x24 pixel bounds, game interface asset, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

## ⭐ P4 角色立绘（全身像）

### 设计原则
- **立绘风格**：日系动漫风格，透明背景
- **尺寸**：1024x2048（全身高清）
- **关键要求**：每个细节都要有"故事"，衣物磨损反映生活状态
- **已有立绘**：林彪悍✅
- **角色详细档案**：见 `CHARACTER.md`

---

### 13. 陈守正（老陈）- 68岁，最后一任站长
**AI提示词**
```
Full body anime style illustration, elderly man age 68 named Chen Shouzheng, square face with dark weathered skin showing deep wrinkles especially around eyes and mouth, deep brown eyes with drooping outer corners that squint into warm slits when smiling but look distant when serious, sparse gray-white eyebrows neatly shaped above each eye, flat nose bridge with rounded reddish nose tip from years of cold weather exposure, thick lips with corners slightly downturned in resting position, neatly combed gray-white short hair 2cm length with one unruly tuft behind left ear sticking up 3cm, slightly enlarged knuckles on right index finger from decades of tightening bolts, wearing white long-sleeve cotton shirt with frayed collar edges and cuffs, second button fastened with mismatched red thread, dark gray V-neck sweater vest with visible pilling on left chest pocket area and darning stitch repair on lower hem, reading glasses with round metal frames hanging on old leather cord around neck resting on chest, dark loose-fitting trousers with crease from ironing, black cloth shoes with worn soles, expression warm kind and simple with hint of nostalgia, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned shoulder-width apart
```

---

### 14. 陈鹤年（陈市长）- 52岁，城市市长
**AI提示词**
```
Full body anime style illustration, middle-aged man age 52 named Chen Henian, square well-groomed face with fine wrinkles at outer eye corners from sleepless nights, deep black calm eyes with steady gaze, thick neatly trimmed eyebrows with clean arch, high nose bridge with thin nostrils, thin lips habitually pressed together in neutral position, jet-black short hair combed back immaculately with clean-shaven temples but 3-4 gray strands visible at hairline, fair indoor complexion with slight pallor, old signet ring with red stone on right ring finger, wearing dark charcoal tailored suit jacket with precise fit showing 1cm of shirt cuff at left sleeve, light blue cotton dress shirt with crisp collar first button undone deliberately for approachability no tie, simple silver cufflinks with wife's initial engraved, vintage mechanical watch with worn brown leather strap on left wrist showing 3:45, dark pressed dress trousers with sharp crease, black polished leather shoes, serious composed expression with hint of inner conflict in slightly furrowed brow, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned together formally
```

---

### 15. 赵铁山（赵监督）- 45岁，铁路安全监督员
**AI提示词**
```
Full body anime style illustration, middle-aged man age 45 named Zhao Tieshan, slightly thin long face with sharp jawline suggesting disciplined lifestyle, deep brown serious eyes that become gentle when relaxed, average straight eyebrows not too thick or thin, ordinary nose bridge not high or low, thin lips with straight corners looking serious when not smiling, short neat black hair 1.5cm length with clean-cut temples showing scalp, dark tanned skin from outdoor work with faint old scar 2cm long on left cheek from military training, long strong fingers with neatly trimmed nails, wearing military-green utility jacket zipped to chest level with zipper pull wrapped in black electrical tape where original broke, small tear 1cm on left sleeve cuff not repaired, dark gray turtleneck knit sweater fitted to body showing torso shape, vintage military mechanical watch with old leather strap and 3mm scratch on crystal on left wrist, dark utility trousers with military canvas belt and brass buckle, black combat boots with worn toes, serious determined expression with occasional glimpse of warmth in eyes, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned firmly apart in stable stance
```

---

### 16. 张德厚（张工）- 62岁，退休机械工程师
**AI提示词**
```
Full body anime style illustration, elderly man age 62 named Zhang Dehou, round plump face with rosy cheeks giving lucky fortune-teller appearance, small bright eyes that squint into happy slits when smiling even in neutral expression they carry mirth, sparse irregular gray-white eyebrows with a few extra-long strands sticking out, round reddish nose tip with visible rosacea broken capillaries, thick lips with corners perpetually turned up in natural smile showing gap where left front tooth missing (chipped while repairing machinery 20 years ago), thinning gray-white hair with slightly balding crown but fluffy unruly sides never combed 3cm length, yellowish skin with small black oil stain 5mm on right cheek and old burn scar 1cm on left hand between thumb and index finger, wearing brown plaid long-sleeve shirt with sleeves rolled to elbows revealing sturdy forearms, left chest pocket holding three pens (red blue black) and small screwdriver, dark brown corduroy vest with broken zipper fastened with safety pin, pockets bulging with small parts screws and roll of electrical tape, reading glasses with taped temples perched on top of head, dark loose-fitting trousers with tool marks, black cloth shoes, radiant cheerful expression with infectious grin showing missing tooth, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned slightly apart in relaxed stance
```

---

### 17. 李桂芳（李阿姨）- 55岁，社区热心居民
**AI提示词**
```
Full body anime style illustration, middle-aged woman age 55 named Li Guifang, round face with slight double chin suggesting prosperity, small but lively eyes that curve into crescent moons when smiling, thin arched eyebrows tattooed in youth now slightly faded to gray-blue, small rounded nose tip, thin lips with corners perpetually upturned speaking rapidly, ear-length permed hair in small curls dyed brown with 2cm white roots growing in, yellowish-pale skin with neck wrinkles from years of cooking over hot stoves, short thick fingers with slightly deformed joints from textile factory work, wearing pink floral long-sleeve blouse freshly washed with iron crease lines still visible, dark blue cotton apron with front pocket holding old-fashioned mobile phone, apron bow tied at back waist for aesthetics, dark trousers with slight wear at knees, black cloth shoes, dark brown Buddhist bead bracelet 12mm on left wrist and gold ring on right hand, warm motherly expression with kind smile and perpetual chatter look suggested by slightly open mouth, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned close together in feminine stance
```

---

### 18. 王晨阳（王小弟）- 22岁，刚毕业大学生
**AI提示词**
```
Full body anime style illustration, young man age 22 named Wang Chenyang, oval face with slight baby fat giving student appearance, large bright eyes with black iris and white sclera clear like stars with visible light reflection, thick natural eyebrows untidy but attractive with one strand crossing upward, small upturned nose with youthful charm, thick lips corners turned up showing straight white teeth, black short hair fluffy 4cm length styled by morning hand-tug with parting bangs revealing full forehead, fair clear skin with small silver earring 3mm on left earlobe and two or three fading acne marks on chin, wearing light gray hooded sweatshirt from discount brand with uneven hoodie drawstrings one 2cm longer than other and faded chest logo, white t-shirt underneath with "Transportation Engineering 2024" printed on collar (graduation commemorative shirt), black over-ear headphones around neck bought on installment plan as professional equipment, light blue jeans with two white wear marks 2cm each on knees from squatting to fix things, white sneakers with slightly dirty toes, expression sunny radiant full of energy and enthusiasm shown by wide smile and bright eyes, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned naturally apart in casual stance
```

---

### 19. 周鼎铭（沙能CEO）- 48岁，沙能科技CEO
**AI提示词**
```
Full body anime style illustration, middle-aged man age 48 named Zhou Dingming, lean long face well-maintained middle-aged appearance looking 42, ordinary-sized eyes with calm gaze that has professional distance but reveals softness when lost in thought (eyes slightly unfocused), neatly trimmed eyebrows not exaggerated with clean arch, ordinary nose bridge not high or low, thin lips with straight corners not much expression person who doesn't smile much (lips slightly parted 1mm), jet-black short hair 2cm length neatly styled with clean temples showing precise hairline, fair well-maintained skin with subtle moisturizer sheen looking few years younger than actual age, long slender fingers with neatly trimmed nails no rings, wearing dark three-piece suit not flashy precise tailoring with subtle pinstripe 1mm apart, light blue silk dress shirt collar neat no tie top button undone, simple silver cufflinks with company logo engraved, smartwatch with black band on left wrist showing notifications, dark pressed dress trousers with sharp crease, black polished leather shoes with mirror shine, calm composed expression with businessman's professional distance but not cold suggested by slight head tilt and relaxed shoulders, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned together in formal power stance
```

---

## 📐 屏幕画布规范（必须遵守）

### Unity画布参数
- **CanvasScaler referenceResolution**：1920×1080
- **Screen Space Overlay模式**：UI直接渲染在屏幕上
- **像素坐标系**：(0,0)在屏幕中心，X向右为正，Y向上为正
- **屏幕范围**：X: -960 ~ +960, Y: -540 ~ +540

### 精灵表切割规范
- **Unity Sprite Editor**：用于切割精灵表为单独图标
- **Pixel Per Unit**：100（1像素=0.01单位）
- **Filter Mode**：Point（像素风无抗锯齿）
- **Compression**：None（保持像素清晰）

### 素材尺寸层级
| 层级 | 单图标尺寸 | 用途 | 每张表包含 |
|------|-----------|------|-----------|
| 小图标 | 64x64px | 天气、状态、简单装饰 | 8个（4×2） |
| 中图标 | 128x128px | 资源、工具、道具、信号灯 | 4个（2×2或4×1） |
| 大图标 | 256x256px | 复杂装饰、角色头像 | 4个（4×1） |
| 立绘 | 1024x2048px | 角色全身像 | 1个 |

---

## 使用说明

1. **优先级**：P0 > P1 > P2 > P3 > P4，先做P0/P1保证游戏能跑
2. **生成方式**：将英文提示词输入AI绘图工具（Midjourney/Stable Diffusion/DALL-E）
3. **参数建议**：Midjourney加 `--style raw --no blur`，Stable Diffusion用pixel art LoRA
4. **导出处理**：用PS/GIMP去除背景，保留透明通道，确保无杂边
5. **命名规范**：`类别_名称.png`，如 `icon_weather_status.png`、`icon_resource.png`
6. **验收标准**：检查像素清晰度、颜色准确度、尺寸精确度、透明背景完整性
7. **精灵表切割**：导入Unity后使用Sprite Editor切割，设置Pixels Per Unit=100

---

## 统计

| 优先级 | 素材 | 数量 | 状态 |
|--------|------|------|------|
| ✅ 已完成 | 标题Logo等 | 5个 | 不用做 |
| 🔄 P0 重做 | 输入框/面板/按钮 | 3个 | 通用模板 |
| ⭐ P1 小图标 | 天气状态/输入框图标 | 2张精灵表 | 10个图标 |
| ⭐ P2 中图标 | 资源/工具/道具/信号灯/状态 | 5张精灵表 | 20个图标 |
| ⭐ P3 大图标 | 装饰元素 | 1张精灵表 | 4个图标 |
| ⭐ P4 角色 | 7个角色立绘 | 7张独立图 | 待生成 |
| **总计** | - | **28个图标+7个立绘** | - |
