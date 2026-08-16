# 铁路复兴：沙能冲击 - AI绘图提示词总表 v3.3

## 统一风格要求
- **风格**：精致像素风，16位复古游戏风格
- **背景**：透明背景（所有素材必须输出PNG格式，保留透明通道）
- **色调**：温暖怀旧风，主色：棕色#8B4513、铜色#CD853F、金色#DAA520、深灰#2F2F2F
- **参考游戏**：《星露谷物语》《泰拉瑞亚》《八方旅人》
- **像素要求**：每个像素清晰可见，不使用抗锯齿，边缘锐利
- **Unity画布**：1920×1080，所有图标必须在此分辨率下清晰可辨
- **精灵表规范**：单元格之间无间距，紧密排列，便于Unity Sprite Editor切割

---

## ✅ 已完成素材（无需再做）

| 素材 | 文件名 | 尺寸 | 状态 |
|------|--------|------|------|
| 标题Logo | title_logo.png | 1361×340 | ✅ 已完成 |
| 背景图 | sunset_railway.png | 1920×1080 | ✅ 已完成 |
| 蒸汽火车 | steam_train.png | - | ✅ 已完成 |
| 主角立绘 | sprite_pack_preview_41da6335.png | 1024×2048 | ✅ 已完成 |
| 主要按钮 | button_primary.png | 800×120 | ✅ 已完成 |
| 通用面板 | panel_bg.png | 1600×1200 | ✅ 已完成 |
| 通用输入框 | input_field.png | 1200×120 | ✅ 已完成 |

---

## 🆕 P0-新 弹窗素材

### 4. 弹窗面板背景
**文件名**：`dialog_bg.png`
**尺寸**：700×400像素
**用途**：账号重置确认弹窗、退出确认弹窗、警告弹窗等所有弹窗场景
**要求**：与面板风格统一但尺寸更小，适合居中显示，内部区域完全空白供代码叠加内容

**中文提示词**：
```
像素风游戏UI弹窗面板背景素材，精确尺寸700像素宽×400像素高的横向矩形，外边框是精致木质框架16像素厚，颜色为深棕色#3E2723，边框有清晰的水平木纹纹理，内侧有2像素深色阴影线#1A1108，四个角落各有一个圆形金属铆钉装饰20×20像素，颜色为铜色#B87333，铆钉有明确的3D效果：左上象限1像素高光#DAA520，右下象限1像素阴影#5C3317，内部填充区域为深棕色#2A1F15，透明度95%，有清晰的不规则木板纹理（木纹方向交替水平和垂直，间距15-30像素随机），顶部中央有标题栏区域700×50像素，颜色为稍浅的棕色#3D2E1F，用于放置标题文字，内部区域完全空白无文字无图标无预留位置，弹窗外部为透明背景，游戏界面素材，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

### 5. 弹窗确认按钮（红色）
**文件名**：`button_confirm.png`
**尺寸**：280×60像素
**用途**：弹窗中的"确认"/"删除"/"重置"等危险操作按钮
**要求**：红色系，醒目提示危险操作

**中文提示词**：
```
像素风游戏UI确认按钮背景素材，精确尺寸280像素宽×60像素高的横向矩形，外边框是木质框架2像素厚，颜色为深红色#8B2500，内部填充区域有明确的垂直渐变效果，从顶部亮红色#CC3300渐变到底部深红色#8B1A00，形成微妙的3D push-button效果，顶部边缘有1像素高光线#FF6644，底部边缘有1像素阴影线#5C1100，四个角有2像素圆角，完全扁平化2D设计，内部区域无文字，按钮外部为透明背景，游戏界面素材，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

### 6. 弹窗取消按钮（灰色）
**文件名**：`button_cancel.png`
**尺寸**：280×60像素
**用途**：弹窗中的"取消"/"返回"等安全操作按钮
**要求**：灰色系，低调不抢眼

**中文提示词**：
```
像素风游戏UI取消按钮背景素材，精确尺寸280像素宽×60像素高的横向矩形，外边框是木质框架2像素厚，颜色为深灰色#4A4A4A，内部填充区域有明确的垂直渐变效果，从顶部中灰色#6A6A6A渐变到底部深灰色#3A3A3A，形成微妙的3D push-button效果，顶部边缘有1像素高光线#8A8A8A，底部边缘有1像素阴影线#2A2A2A，四个角有2像素圆角，完全扁平化2D设计，内部区域无文字，按钮外部为透明背景，游戏界面素材，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

## ⭐ P1 小图标精灵表（64×64像素）

### 7. 天气+状态图标精灵表
**文件名**：`icon_small.png`
**尺寸**：256×128像素（4×2网格，每个图标64×64像素）
**用途**：天气状态显示（晴/雨/雪/风）+ 操作状态提示（错误/成功/警告）+ 齿轮装饰

**中文提示词**：
```
像素风游戏小图标精灵表，精确尺寸256像素宽×128像素高的矩形网格，分为8个相等单元格每个64×64像素，排列为4列2行，单元格之间无间距紧密排列，第1行第1列：太阳图标，圆形24像素直径居中于(32,32)，有8条射线8像素长向外延伸每45度一条，亮黄色#FFD700，2像素深棕色#5C3317轮廓，第1行第2列：雨天图标，云朵形状30像素宽16像素高居中于(32,24)，下方有3条对角雨滴5像素长，位置(22,44)(32,48)(42,44)，蓝灰色#708090云朵配蓝色#4169E1雨滴，2像素深色轮廓，第1行第3列：雪天图标，云朵形状30像素宽16像素高居中于(32,24)，下方有3个雪花点3像素直径，位置(22,44)(32,48)(42,44)，浅蓝色#ADD8E6云朵配白色#FFFFFF雪花，2像素深色轮廓，第1行第4列：大风图标，3条水平波浪线30像素长2像素厚，垂直居中于y=24 y=32 y=40，浅灰色#D3D3D3，2像素深色轮廓，第2行第1列：错误图标，深红色#8B0000圆形轮廓36像素直径2像素厚居中于(32,32)，内部白色#FFFFFF X标记16像素高，第2行第2列：成功图标，深绿色#006400圆形轮廓36像素直径2像素厚居中于(32,32)，内部白色#FFFFFF对勾标记16像素高，第2行第3列：警告图标，深黄色#B8860B圆形轮廓36像素直径2像素厚居中于(32,32)，内部白色#FFFFFF感叹号16像素高，第2行第4列：齿轮图标，银色#C0C0C0圆形28像素直径居中于(32,32)，有8个齿4像素长向外延伸，2像素深色轮廓，所有图标扁平化2D设计无渐变无阴影，边缘锐利无抗锯齿，图标外部为透明背景，游戏界面精灵表，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

### 8. 输入框图标精灵表
**文件名**：`icon_input.png`
**尺寸**：128×64像素（2×1网格，每个图标64×64像素）
**用途**：用户名输入框左侧用户标识、密码输入框左侧钥匙标识

**中文提示词**：
```
像素风游戏输入框图标精灵表，精确尺寸128像素宽×64像素高的横向条带，分为2个相等单元格每个64×64像素，单元格之间无间距紧密排列，左侧单元格：用户图标，人物头部和肩膀剪影，头部为圆形20像素直径居中于(32,22)，肩膀为弧线30像素宽居中于(32,42)延伸至单元格边缘，单一暖金色#DAA520，2像素深棕色#5C3317轮廓，右侧单元格：钥匙图标，老式骨架钥匙垂直放置，钥匙环为圆形14像素直径居中于(32,18)，钥匙杆为垂直线4像素宽28像素高居中于(32,32)，钥匙齿为L形10像素宽6像素高在底部，单一暖金色#DAA520，2像素深棕色#5C3317轮廓，所有图标扁平化2D设计无渐变无阴影，边缘锐利无抗锯齿，图标外部为透明背景，游戏界面精灵表，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

## ⭐ P2 中图标精灵表（128×128像素）

### 9. 资源图标精灵表
**文件名**：`icon_resource.png`
**尺寸**：256×256像素（2×2网格，每个图标128×128像素）
**用途**：资金/信任度/客流/车况四种核心资源显示图标

**中文提示词**：
```
像素风游戏资源图标精灵表，精确尺寸256像素宽×256像素高的正方形网格，分为4个相等单元格每个128×128像素，排列为2×2，单元格之间无间距紧密排列，左上单元格：金币图标代表资金，圆形50像素直径居中于(64,64)，有明确的浮雕美元符号20像素高，暖金色#DAA520配左上象限2像素高光#FFFACD创造3D效果，2像素深棕色#5C3317轮廓，右上单元格：红心图标代表信任，心形50像素高居中于(64,64)，有明确的3D效果，暖红色#CC0000配左上象限2像素高光#FF6666，2像素深红色#8B0000轮廓，左下单元格：人群剪影图标代表客流，3个站立人物40像素高居中于(64,64)呈三角形排列，暖米色#F5DEB3配2像素深米色#D2B48C轮廓，右下单元格：扳手图标代表车况，L形扳手50像素高居中于(64,64)，暖棕色#8B4513手柄配铜色#B87333金属头，2像素深棕色#5C3317轮廓，所有图标扁平化2D设计配明确的3D高光效果，边缘锐利无抗锯齿，图标外部为透明背景，游戏界面精灵表，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

### 10. 工具图标精灵表
**文件名**：`icon_tool.png`
**尺寸**：256×256像素（2×2网格，每个图标128×128像素）
**用途**：扫把/扳手/手电筒/安全帽四种员工工具图标

**中文提示词**：
```
像素风游戏工具图标精灵表，精确尺寸256像素宽×256像素高的正方形网格，分为4个相等单元格每个128×128像素，排列为2×2，单元格之间无间距紧密排列，左上单元格：扫把图标代表清洁工，垂直手柄10像素宽45像素高居中于x=64从y=44到y=89，刷毛扇形40像素宽18像素高居中于(64,102)，手柄暖棕色#8B4513刷毛棕褐色#D2B48C配2像素深棕色#5C3317轮廓，右上单元格：扳手图标代表检修工，L形扳手45像素手柄10像素宽和25像素钳口8像素宽居中于(64,64)，银色#C0C0C0金属配棕色#8B4513木质握把段15像素长，2像素深灰色#2F2F2F轮廓，左下单元格：手电筒图标代表巡道工，垂直圆柱体16像素宽40像素高居中于(64,72)，上方光锥36像素宽22像素高居中于(64,38)，深灰色#2F2F2F筒身配亮黄色#FFD700光锥渐变至透明，筒身2像素黑色#1A1A1A轮廓，右下单元格：安全帽图标代表所有员工，圆顶形状55像素宽28像素高居中于(64,48)，帽檐65像素宽8像素高在下方y=78，亮黄色#FFD700配橙色#FF8C00水平条纹4像素高在y=54，2像素深棕色#5C3317轮廓，所有图标扁平化2D设计配清晰轮廓和基本形状，边缘锐利无抗锯齿，图标外部为透明背景，游戏界面精灵表，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

### 11. 道具图标精灵表
**文件名**：`icon_item.png`
**尺寸**：256×256像素（2×2网格，每个图标128×128像素）
**用途**：车票/怀表/钥匙/日记四种重要道具图标

**中文提示词**：
```
像素风游戏道具图标精灵表，精确尺寸256像素宽×256像素高的正方形网格，分为4个相等单元格每个128×128像素，排列为2×2，单元格之间无间距紧密排列，左上单元格：火车票图标，水平矩形55像素宽28像素高居中于(64,64)，左侧有穿孔边缘（3个半圆5像素每个），奶油色#FFF8DC配棕色#8B4513印刷水平线1像素厚每6像素一条，2像素深棕色#5C3317轮廓，右上单元格：怀表图标代表主角传家宝，圆形42像素直径居中于(64,52)，链条5像素宽28像素长向下延伸至(64,92)，金色黄铜#B87333配圆形表面和黑色#1A1A1A小指针显示3:45，2像素深棕色#5C3317轮廓，左下单元格：老钥匙图标，经典骨架钥匙50像素高居中于(64,64)，顶部圆形钥匙环18像素直径，底部简单钥匙齿14像素，黄铜#B87333配钥匙环上1像素高光#DAA520，2像素深棕色#5C3317轮廓，右下单元格：日记图标，闭合书籍42像素宽52像素高居中于(64,64)，左侧书脊右侧小黄铜扣4×4像素，深棕色#5C3317封面配奶油色#FFF8DC书页边缘可见2像素，2像素黑色#1A1A1A轮廓，所有图标扁平化2D设计配清晰轮廓和基本形状，边缘锐利无抗锯齿，图标外部为透明背景，游戏界面精灵表，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

### 12. 信号灯精灵表
**文件名**：`icon_signal.png`
**尺寸**：384×128像素（3×1网格，每个图标128×128像素）
**用途**：铁路信号灯红/绿/黄三种状态显示

**中文提示词**：
```
像素风游戏信号灯精灵表，精确尺寸384像素宽×128像素高的横向条带，分为3个相等单元格每个128×128像素，单元格之间无间距紧密排列，左侧单元格：红色信号灯，深灰色#2F2F2F垂直灯壳36像素宽72像素高居中于(64,64)配2像素黑色#1A1A1A轮廓，圆形灯28像素直径居中于(64,40)，亮红色#FF0000填充配左上象限2像素白色#FFFFFF高光，中间单元格：绿色信号灯，相同灯壳设计和位置，圆形灯28像素直径居中于(64,40)，亮绿色#00FF00填充配左上象限2像素白色#FFFFFF高光，右侧单元格：黄色信号灯，相同灯壳设计和位置，圆形灯28像素直径居中于(64,40)，亮黄色#FFD700填充配左上象限2像素白色#FFFFFF高光，所有图标扁平化2D设计配明确的3D高光效果，边缘锐利无抗锯齿，图标外部为透明背景，游戏界面精灵表，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

### 13. 状态提示精灵表
**文件名**：`icon_status.png`
**尺寸**：384×128像素（3×1网格，每个图标128×128像素）
**用途**：错误/成功/警告三种状态提示图标

**中文提示词**：
```
像素风游戏状态图标精灵表，精确尺寸384像素宽×128像素高的横向条带，分为3个相等单元格每个128×128像素，单元格之间无间距紧密排列，左侧单元格：错误图标，深红色#8B0000圆形轮廓56像素直径3像素厚居中于(64,64)，内部白色#FFFFFF X标记28像素高居中于(64,64)，中间单元格：成功图标，深绿色#006400圆形轮廓56像素直径3像素厚居中于(64,64)，内部白色#FFFFFF对勾标记28像素高居中于(64,64)，右侧单元格：警告图标，深黄色#B8860B圆形轮廓56像素直径3像素厚居中于(64,64)，内部白色#FFFFFF感叹号28像素高居中于(64,64)，所有图标扁平化2D设计无渐变无阴影，边缘锐利无抗锯齿，图标外部为透明背景，游戏界面精灵表，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

## ⭐ P3 大图标精灵表（256×256像素）

### 14. 装饰元素精灵表
**文件名**：`icon_decoration.png`
**尺寸**：1024×256像素（4×1网格，每个元素256×256像素）
**用途**：铁轨分隔线、小火车、齿轮、烟雾四种装饰元素

**中文提示词**：
```
像素风游戏装饰元素精灵表，精确尺寸1024像素宽×256像素高的横向条带，分为4个相等单元格每个256×256像素，单元格之间无间距紧密排列，左侧单元格：铁轨段，两条平行深棕色#5C3317铁轨6像素厚相隔40像素水平穿过单元格，木质枕木#8B4513每80像素一根6像素厚，铁轨垂直居中于y=128，2像素深色轮廓，第二个单元格：小型蒸汽火车侧视图朝右，红色车身#C0392B机车120像素宽80像素高居中于(128,176)，顶部铜色圆顶#DAA520直径20像素，黑色烟囱#1A1A1A8像素宽30像素高冒白色#FFFFFF蒸汽云40像素宽，2像素深色轮廓，第三个单元格：齿轮/轮齿图标，银色#C0C0C0圆形100像素直径居中于(128,128)，有12个齿16像素长向外延伸，左上象限有2像素金属高光#E8E8E8，2像素深色轮廓，第四个单元格：烟雾团效果，浅灰色#D3D3D3云朵形状120像素宽80像素高居中于(128,128)，上方3个小团30像素渐变至边缘透明，2像素深色轮廓，所有元素扁平化2D设计配1像素深色轮廓，边缘锐利无抗锯齿，元素外部为透明背景，游戏装饰精灵表，高质量像素艺术，16位复古风格，输出为带透明通道的PNG格式
```

---

## ⭐ P4 角色立绘（全身像）

### 设计原则
- **立绘风格**：日系动漫风格，透明背景
- **尺寸**：1024x2048（全身高清）
- **关键要求**：每个细节都要有"故事"，衣物磨损反映生活状态
- **已有立绘**：林彪悍✅
- **角色详细档案**：见 `CHARACTER.md`

### 角色信息表

| 角色 | 中文名 | 年龄 | 职业 | 性格特征 |
|------|--------|------|------|----------|
| Chen Shouzheng | 陈守正（老陈） | 68岁 | 最后一任站长 | 温和善良，怀旧 |
| Chen Henian | 陈鹤年（陈市长） | 52岁 | 城市市长 | 严肃沉稳，内心矛盾 |
| Zhao Tieshan | 赵铁山（赵监督） | 45岁 | 铁路安全监督员 | 严肃坚定，偶露温情 |
| Zhang Dehou | 张德厚（张工） | 62岁 | 退休机械工程师 | 开朗乐观，感染力强 |
| Li Guifang | 李桂芳（李阿姨） | 55岁 | 社区热心居民 | 热心健谈，母性光辉 |
| Wang Chenyang | 王晨阳（王小弟） | 22岁 | 刚毕业大学生 | 阳光热情，充满活力 |
| Zhou Dingming | 周鼎铭（沙能CEO） | 48岁 | 沙能科技CEO | 冷静专业，外冷内热 |

---

### 15. 陈守正（老陈）- 68岁，最后一任站长
```
Full body anime style illustration, elderly man age 68 named Chen Shouzheng (老陈), square face with dark weathered skin showing deep wrinkles especially around eyes and mouth, deep brown eyes with drooping outer corners that squint into warm slits when smiling but look distant when serious, sparse gray-white eyebrows neatly shaped above each eye, flat nose bridge with rounded reddish nose tip from years of cold weather exposure, thick lips with corners slightly downturned in resting position, neatly combed gray-white short hair 2cm length with one unruly tuft behind left ear sticking up 3cm, slightly enlarged knuckles on right index finger from decades of tightening bolts, wearing white long-sleeve cotton shirt with frayed collar edges and cuffs, second button fastened with mismatched red thread, dark gray V-neck sweater vest with visible pilling on left chest pocket area and darning stitch repair on lower hem, reading glasses with round metal frames hanging on old leather cord around neck resting on chest, dark loose-fitting trousers with crease from ironing, black cloth shoes with worn soles, expression warm kind and simple with hint of nostalgia, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned shoulder-width apart
```

---

### 16. 陈鹤年（陈市长）- 52岁，城市市长
```
Full body anime style illustration, middle-aged man age 52 named Chen Henian (陈市长), square well-groomed face with fine wrinkles at outer eye corners from sleepless nights, deep black calm eyes with steady gaze, thick neatly trimmed eyebrows with clean arch, high nose bridge with thin nostrils, thin lips habitually pressed together in neutral position, jet-black short hair combed back immaculately with clean-shaven temples but 3-4 gray strands visible at hairline, fair indoor complexion with slight pallor, old signet ring with red stone on right ring finger, wearing dark charcoal tailored suit jacket with precise fit showing 1cm of shirt cuff at left sleeve, light blue cotton dress shirt with crisp collar first button undone deliberately for approachability no tie, simple silver cufflinks with wife's initial engraved, vintage mechanical watch with worn brown leather strap on left wrist showing 3:45, dark pressed dress trousers with sharp crease, black polished leather shoes, serious composed expression with hint of inner conflict in slightly furrowed brow, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned together formally
```

---

### 17. 赵铁山（赵监督）- 45岁，铁路安全监督员
```
Full body anime style illustration, middle-aged man age 45 named Zhao Tieshan (赵监督), slightly thin long face with sharp jawline suggesting disciplined lifestyle, deep brown serious eyes that become gentle when relaxed, average straight eyebrows not too thick or thin, ordinary nose bridge not high or low, thin lips with straight corners looking serious when not smiling, short neat black hair 1.5cm length with clean-cut temples showing scalp, dark tanned skin from outdoor work with faint old scar 2cm long on left cheek from military training, long strong fingers with neatly trimmed nails, wearing military-green utility jacket zipped to chest level with zipper pull wrapped in black electrical tape where original broke, small tear 1cm on left sleeve cuff not repaired, dark gray turtleneck knit sweater fitted to body showing torso shape, vintage military mechanical watch with old leather strap and 3mm scratch on crystal on left wrist, dark utility trousers with military canvas belt and brass buckle, black combat boots with worn toes, serious determined expression with occasional glimpse of warmth in eyes, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned firmly apart in stable stance
```

---

### 18. 张德厚（张工）- 62岁，退休机械工程师
```
Full body anime style illustration, elderly man age 62 named Zhang Dehou (张工), round plump face with rosy cheeks giving lucky fortune-teller appearance, small bright eyes that squint into happy slits when smiling even in neutral expression they carry mirth, sparse irregular gray-white eyebrows with a few extra-long strands sticking out, round reddish nose tip with visible rosacea broken capillaries, thick lips with corners perpetually turned up in natural smile showing gap where left front tooth missing (chipped while repairing machinery 20 years ago), thinning gray-white hair with slightly balding crown but fluffy unruly sides never combed 3cm length, yellowish skin with small black oil stain 5mm on right cheek and old burn scar 1cm on left hand between thumb and index finger, wearing brown plaid long-sleeve shirt with sleeves rolled to elbows revealing sturdy forearms, left chest pocket holding three pens (red blue black) and small screwdriver, dark brown corduroy vest with broken zipper fastened with safety pin, pockets bulging with small parts screws and roll of electrical tape, reading glasses with taped temples perched on top of head, dark loose-fitting trousers with tool marks, black cloth shoes, radiant cheerful expression with infectious grin showing missing tooth, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned slightly apart in relaxed stance
```

---

### 19. 李桂芳（李阿姨）- 55岁，社区热心居民
```
Full body anime style illustration, middle-aged woman age 55 named Li Guifang (李阿姨), round face with slight double chin suggesting prosperity, small but lively eyes that curve into crescent moons when smiling, thin arched eyebrows tattooed in youth now slightly faded to gray-blue, small rounded nose tip, thin lips with corners perpetually upturned speaking rapidly, ear-length permed hair in small curls dyed brown with 2cm white roots growing in, yellowish-pale skin with neck wrinkles from years of cooking over hot stoves, short thick fingers with slightly deformed joints from textile factory work, wearing pink floral long-sleeve blouse freshly washed with iron crease lines still visible, dark blue cotton apron with front pocket holding old-fashioned mobile phone, apron bow tied at back waist for aesthetics, dark trousers with slight wear at knees, black cloth shoes, dark brown Buddhist bead bracelet 12mm on left wrist and gold ring on right hand, warm motherly expression with kind smile and perpetual chatter look suggested by slightly open mouth, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned close together in feminine stance
```

---

### 20. 王晨阳（王小弟）- 22岁，刚毕业大学生
```
Full body anime style illustration, young man age 22 named Wang Chenyang (王小弟), oval face with slight baby fat giving student appearance, large bright eyes with black iris and white sclera clear like stars with visible light reflection, thick natural eyebrows untidy but attractive with one strand crossing upward, small upturned nose with youthful charm, thick lips corners turned up showing straight white teeth, black short hair fluffy 4cm length styled by morning hand-tug with parting bangs revealing full forehead, fair clear skin with small silver earring 3mm on left earlobe and two or three fading acne marks on chin, wearing light gray hooded sweatshirt from discount brand with uneven hoodie drawstrings one 2cm longer than other and faded chest logo, white t-shirt underneath with "Transportation Engineering 2024" printed on collar (graduation commemorative shirt), black over-ear headphones around neck bought on installment plan as professional equipment, light blue jeans with two white wear marks 2cm each on knees from squatting to fix things, white sneakers with slightly dirty toes, expression sunny radiant full of energy and enthusiasm shown by wide smile and bright eyes, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned naturally apart in casual stance
```

---

### 21. 周鼎铭（沙能CEO）- 48岁，沙能科技CEO
```
Full body anime style illustration, middle-aged man age 48 named Zhou Dingming (沙能CEO), lean long face well-maintained middle-aged appearance looking 42, ordinary-sized eyes with calm gaze that has professional distance but reveals softness when lost in thought (eyes slightly unfocused), neatly trimmed eyebrows not exaggerated with clean arch, ordinary nose bridge not high or low, thin lips with straight corners not much expression person who doesn't smile much (lips slightly parted 1mm), jet-black short hair 2cm length neatly styled with clean temples showing precise hairline, fair well-maintained skin with subtle moisturizer sheen looking few years younger than actual age, long slender fingers with neatly trimmed nails no rings, wearing dark three-piece suit not flashy precise tailoring with subtle pinstripe 1mm apart, light blue silk dress shirt collar neat no tie top button undone, simple silver cufflinks with company logo engraved, smartwatch with black band on left wrist showing notifications, dark pressed dress trousers with sharp crease, black polished leather shoes with mirror shine, calm composed expression with businessman's professional distance but not cold suggested by slight head tilt and relaxed shoulders, transparent background, high quality anime illustration, detailed linework, full body composition including feet positioned together in formal power stance
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
- **单元格间距**：无间距，紧密排列

### 素材尺寸层级
| 层级 | 单图标尺寸 | 用途 | 每张表包含 |
|------|-----------|------|-----------|
| 小图标 | 64x64px | 天气、状态、简单装饰 | 8个（4×2） |
| 中图标 | 128x128px | 资源、工具、道具、信号灯 | 4个（2×2或3×1） |
| 大图标 | 256x256px | 复杂装饰、角色头像 | 4个（4×1） |
| 立绘 | 1024x2048px | 角色全身像 | 1个 |

---

## 生成顺序建议

| 优先级 | 素材 | 文件名 | 尺寸 | 生成后放入 | 状态 |
|--------|------|--------|------|-----------|------|
| ~~P0~~ | ~~通用面板~~ | ~~panel_bg.png~~ | ~~1600×1200~~ | ~~Assets/Resources/UI/Login/~~ | ✅ 已完成 |
| ~~P0~~ | ~~输入框~~ | ~~input_field.png~~ | ~~1200×120~~ | ~~Assets/Resources/UI/Login/~~ | ✅ 已完成 |
| ~~P0~~ | ~~按钮~~ | ~~button_primary.png~~ | ~~800×120~~ | ~~Assets/Resources/UI/Login/~~ | ✅ 已完成 |
| **P0-新** | 弹窗面板 | dialog_bg.png | 700×400 | Assets/Resources/UI/Dialog/ | 待生成 |
| **P0-新** | 确认按钮 | button_confirm.png | 280×60 | Assets/Resources/UI/Dialog/ | 待生成 |
| **P0-新** | 取消按钮 | button_cancel.png | 280×60 | Assets/Resources/UI/Dialog/ | 待生成 |
| P1 | 输入框图标 | icon_input.png | 128×64 | Assets/Resources/UI/Icons/ | 待生成 |
| P1 | 天气状态图标 | icon_small.png | 256×128 | Assets/Resources/UI/Icons/ | 待生成 |
| P2 | 资源图标 | icon_resource.png | 256×256 | Assets/Resources/UI/Icons/ | 待生成 |
| P2 | 工具图标 | icon_tool.png | 256×256 | Assets/Resources/UI/Icons/ | 待生成 |
| P2 | 道具图标 | icon_item.png | 256×256 | Assets/Resources/UI/Icons/ | 待生成 |
| P2 | 信号灯 | icon_signal.png | 384×128 | Assets/Resources/UI/Icons/ | 待生成 |
| P2 | 状态提示 | icon_status.png | 384×128 | Assets/Resources/UI/Icons/ | 待生成 |
| P3 | 装饰元素 | icon_decoration.png | 1024×256 | Assets/Resources/UI/Icons/ | 待生成 |

---

## 透明背景要求（重要！）

**所有AI生成的素材必须满足以下要求**：
1. 背景必须是透明的（不是白色、灰色或棋盘格）
2. 输出格式为PNG with alpha channel
3. 如果AI工具不支持透明背景，需要后处理去除背景
4. 验证方法：在图片查看器中打开，背景应显示为黑色或透明

---

## 使用说明

1. **优先级**：P0 > P1 > P2 > P3 > P4，先做P0保证游戏能跑
2. **生成方式**：将中文提示词输入AI绘图工具（Midjourney/Stable Diffusion/DALL-E）
3. **参数建议**：Midjourney加 `--style raw --no blur`，Stable Diffusion用pixel art LoRA
4. **导出处理**：用PS/GIMP去除背景，保留透明通道，确保无杂边
5. **命名规范**：`类别_名称.png`，如 `icon_weather_status.png`、`icon_resource.png`
6. **验收标准**：检查像素清晰度、颜色准确度、尺寸精确度、透明背景完整性
7. **精灵表切割**：导入Unity后使用Sprite Editor切割，设置Pixels Per Unit=100

---

## 统计

| 优先级 | 素材 | 数量 | 状态 |
|--------|------|------|------|
| ✅ 已完成 | 标题Logo/背景图/火车/立绘/按钮/面板/输入框 | 7个 | 不用做 |
| 🆕 P0-新 | 弹窗面板/确认按钮/取消按钮 | 3个 | 弹窗专用 |
| ⭐ P1 小图标 | 天气状态/输入框图标 | 2张精灵表 | 10个图标 |
| ⭐ P2 中图标 | 资源/工具/道具/信号灯/状态 | 5张精灵表 | 20个图标 |
| ⭐ P3 大图标 | 装饰元素 | 1张精灵表 | 4个图标 |
| ⭐ P4 角色 | 7个角色立绘 | 7张独立图 | 待生成 |
| **总计** | - | **34个图标+7个立绘** | - |
