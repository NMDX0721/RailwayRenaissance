# 铁路复兴：沙能冲击 - AI绘图提示词总表 v2.0

## 风格统一要求
- **风格**：精致像素风，16位复古游戏风格
- **背景**：透明背景（所有素材必须输出PNG with alpha channel）
- **色调**：温暖怀旧风，主色#8B4513（棕）、#CD853F（铜）、#DAA520（金）、#2F2F2F（深灰）
- **参考游戏**：《星露谷物语》《泰拉瑞亚》《八方旅人》
- **像素密度**：每像素清晰可见，不使用抗锯齿，边缘锐利

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

## ⭐ P0 已有素材（需确认）

### 4. 游戏标题Logo
**用途**：登录界面和标题画面的游戏名称图片
**尺寸**：900x220px（已在Unity中设置）
**状态**：✅ 已完成（title_logo.png），包含中英文双语

---

## ⭐ P1 游戏核心素材（经营界面）

### 5. 资源图标模板
**用途**：资金/信任度/客流/车况等资源显示图标
**尺寸**：32x32px（所有图标统一尺寸）
**泛用性**：一个模板生成多种图标，通过颜色和形状区分

```
Pixel art game resource icon template, square shape with exact dimensions 32 pixels wide by 32 pixels tall, icon content occupies central 24x24 pixel area with 4 pixel padding on all sides, style is simple flat silhouette with no gradients no shadows no 3D effects, color palette limited to 3 colors per icon (main color #DAA520 gold, shadow color #8B4513 brown, highlight color #FFFACD cream), edges are crisp with no anti-aliasing, each icon variant uses different shape: coin-circle for funds, heart-shape for trust, people-silhouette for passengers, wrench-shape for condition, transparent background outside the 32x32 pixel bounds, game interface icon asset, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 6. 状态提示图标模板
**用途**：错误/成功/警告等状态提示
**尺寸**：24x24px（所有状态图标统一尺寸）
**泛用性**：一个模板生成多种状态图标

```
Pixel art game status icon template, square shape with exact dimensions 24 pixels wide by 24 pixels tall, icon content occupies central 20x20 pixel area with 2 pixel padding on all sides, style is simple flat symbol inside circle outline, circle is 2 pixels thick with color varying by state: dark red #8B0000 for error, dark green #006400 for success, dark yellow #B8860B for warning, inner symbol is white #FFFFFF: X-mark for error, checkmark for success, exclamation for warning, edges are crisp with no anti-aliasing, transparent background outside the 24x24 pixel bounds, game interface icon asset, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 7. 天气图标模板
**用途**：晴天/雨天/雪天/大风等天气状态显示
**尺寸**：32x32px（所有天气图标统一尺寸）
**泛用性**：一个模板生成多种天气图标

```
Pixel art game weather icon template, square shape with exact dimensions 32 pixels wide by 32 pixels tall, icon content occupies central 28x28 pixel area with 2 pixel padding on all sides, style is simple flat silhouette with no gradients no shadows, color palette limited to 2 colors per icon (main color and white #FFFFFF highlight), sun variant: circle 12 pixels diameter centered at (16,16) with 8 rays 2 pixels long extending outward, rain variant: cloud shape 16 pixels wide at top with 3 diagonal rain drops 2 pixels each below, snow variant: cloud shape 16 pixels wide at top with 3 snowflake dots 2 pixels each below, wind variant: 3 horizontal wavy lines 2 pixels thick stacked vertically, edges are crisp with no anti-aliasing, transparent background outside the 32x32 pixel bounds, game interface icon asset, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 8. 工具图标模板
**用途**：扫把/扳手/手电筒等工作工具显示
**尺寸**：32x32px（所有工具图标统一尺寸）
**泛用性**：一个模板生成多种工具图标

```
Pixel art game tool icon template, square shape with exact dimensions 32 pixels wide by 32 pixels tall, icon content occupies central 24x28 pixel area with 4 pixel horizontal padding and 2 pixel vertical padding, style is simple flat silhouette with no gradients no shadows, color palette limited to 3 colors per icon (handle color #8B4513 brown, metal color #C0C0C0 silver, accent color #DAA520 gold), broom variant: vertical handle 4 pixels wide 20 pixels tall at center, bristle fan 12 pixels wide 8 pixels tall at bottom, wrench variant: L-shape with 16 pixel handle and 8 pixel jaw, flashlight variant: vertical cylinder 6 pixels wide 16 pixels tall with light cone 12 pixels wide 8 pixels tall at top, edges are crisp with no anti-aliasing, transparent background outside the 32x32 pixel bounds, game interface icon asset, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 9. 道具图标模板
**用途**：车票/怀表/钥匙等道具显示
**尺寸**：32x32px（所有道具图标统一尺寸）
**泛用性**：一个模板生成多种道具图标

```
Pixel art game item icon template, square shape with exact dimensions 32 pixels wide by 32 pixels tall, icon content occupies central 24x24 pixel area with 4 pixel padding on all sides, style is simple flat silhouette with no gradients no shadows, color palette limited to 3 colors per icon (main color, shadow color #8B4513 brown, highlight color #FFFACD cream), ticket variant: horizontal rectangle 18 pixels wide 10 pixels tall with perforated left edge (3 semicircles 2 pixels each), pocket watch variant: circle 14 pixels diameter with chain 2 pixels wide 8 pixels long extending downward, key variant: classic skeleton key shape 16 pixels tall with round bow 6 pixels diameter at top and simple bit 4 pixels at bottom, edges are crisp with no anti-aliasing, transparent background outside the 32x32 pixel bounds, game interface icon asset, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

## ⭐ P2 游戏功能素材

### 10. 信号灯图标模板
**用途**：铁路信号灯状态显示（红/绿/黄）
**尺寸**：24x24px（所有信号灯统一尺寸）
**泛用性**：一个模板生成三种颜色信号灯

```
Pixel art game signal light icon template, square shape with exact dimensions 24 pixels wide by 24 pixels tall, icon content occupies central 20x22 pixel area with 2 pixel horizontal padding and 1 pixel vertical padding, style is simple flat design with minimal detail, housing is dark gray #2F2F2F vertical rectangle 10 pixels wide 18 pixels tall centered at (12,12), light circle is 8 pixels diameter centered at (12,8) inside the housing, red variant: light color #FF0000 with 1 pixel white #FFFFFF highlight at top-left, green variant: light color #00FF00 with 1 pixel white highlight at top-left, yellow variant: light color #FFD700 with 1 pixel white highlight at top-left, edges are crisp with no anti-aliasing, transparent background outside the 24x24 pixel bounds, game interface icon asset, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 11. 装饰元素模板
**用途**：铁轨分隔线、小火车、齿轮等装饰性元素
**尺寸**：根据用途自定义（铁轨400x16px、小火车128x64px、齿轮32x32px）
**泛用性**：一个模板生成多种装饰元素

```
Pixel art game decoration element template, various dimensions depending on variant, style is simple flat silhouette with no gradients no shadows, railway track variant: horizontal rectangle 400 pixels wide 16 pixels tall, two parallel dark brown #5C3317 rails 2 pixels thick separated by 8 pixels, wooden sleepers #8B4513 every 20 pixels 2 pixels thick, train variant: side-view steam locomotive 100 pixels wide 40 pixels tall centered in 128x64 canvas, red body #C0392B with brass dome #DAA520 and black smokestack #1A1A1A puffing white steam #FFFFFF, gear variant: circle 20 pixels diameter with 8 teeth 3 pixels each extending outward, silver #C0C0C0 color, all edges crisp with no anti-aliasing, transparent background, game decoration asset, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

### 12. 密码强度条
**用途**：注册时密码强度可视化指示
**尺寸**：400x24px（比原版更大更清晰）
**独占素材**：包含强度分区文字，信息密度需求高

```
Pixel art game UI progress bar for password strength, horizontal rectangular shape with exact dimensions 400 pixels wide by 24 pixels tall, outer border is dark brown #5C3317 wooden frame 2 pixels thick on all four sides, inner area divided into three equal sections each 130 pixels wide (minus borders), left section filled with solid red #CC0000 representing weak password, middle section filled with solid yellow #DAA520 representing medium password, right section filled with solid green #00AA00 representing strong password, each section has subtle 1-pixel highlight at top edge and 1-pixel shadow at bottom edge, small text labels embedded: "弱" in left section 8px white text centered, "中" in middle section 8px white text centered, "强" in right section 8px white text centered, completely flat 2D design with no gradients no 3D effects, transparent background outside the 400x24 pixel bounds, game interface asset, high quality pixel art, 16-bit retro style, output as PNG with alpha channel
```

---

## ⭐ P3 角色立绘（全身像）

### 设计原则
- **立绘风格**：日系动漫风格，透明背景
- **尺寸**：1024x2048（全身高清）
- **关键要求**：每个细节都要有"故事"，衣物磨损反映生活状态
- **已有立绘**：林彪悍✅

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

### 面板尺寸规划
- **最大面板**：1600×1200（占屏幕83%×111%，需要滚动或分页）
- **推荐面板**：1400×900（占屏幕73%×83%）
- **面板居中时**：顶部Y=高度/2，底部Y=-高度/2
- **Logo位置**：必须Y > 面板顶部Y + 50像素

### 元素间距规范
- **输入框间距**：垂直80-120像素
- **按钮间距**：垂直40-60像素
- **按钮与输入框间距**：垂直60-80像素
- **元素与面板边缘间距**：水平80-120像素，垂直60-100像素

### 字体规范
- **标题字体**：Microsoft YaHei, 36-48px, 颜色#F0D060（金色）
- **正文字体**：Microsoft YaHei, 24-32px, 颜色#E0D0B0（暖白）
- **提示字体**：Microsoft YaHei, 18-22px, 颜色#B0A090（暖灰）
- **按钮字体**：Microsoft YaHei, 28-36px, 颜色#FFF0D0（亮金）

---

## 使用说明

1. **优先级**：P0 > P1 > P2 > P3，先做P0/P1保证游戏能跑
2. **生成方式**：将英文提示词输入AI绘图工具（Midjourney/Stable Diffusion/DALL-E）
3. **参数建议**：Midjourney加 `--style raw --no blur`，Stable Diffusion用pixel art LoRA
4. **导出处理**：用PS/GIMP去除背景，保留透明通道，确保无杂边
5. **命名规范**：`类别_名称.png`，如 `input_field.png`、`icon_weather_sun.png`
6. **验收标准**：检查像素清晰度、颜色准确度、尺寸精确度、透明背景完整性

---

## 统计

| 优先级 | 数量 | 状态 |
|--------|------|------|
| ✅ 已完成 | 5个 | 不用做 |
| 🔄 P0 重做 | 3个 | 通用模板 |
| ⭐ P1 核心 | 5个 | 模板+角色 |
| ⭐ P2 功能 | 3个 | 模板 |
| ⭐ P3 装饰 | 7个 | 角色立绘 |
| **总计** | **23个** | - |
