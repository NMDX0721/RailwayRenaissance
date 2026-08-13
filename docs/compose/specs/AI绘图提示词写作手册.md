# AI绘图提示词写作手册

> 版本：v1.0  
> 适用：RailwayRenaissance 像素风游戏美术  
> 识图：mimo 2.5 | 执行任务：ds / ds0718

---

## 一、核心理念

AI绘图提示词不是"描述你想要什么"，而是**用精确的关键词构建一个AI能理解的视觉蓝图**。

好提示词的三要素：
1. **风格锁定** — 让AI知道"用什么画风画"
2. **内容清单** — 让AI知道"画什么"
3. **文化锚点** — 让AI知道"这是什么风格的文化背景"

---

## 二、提示词结构模板

```
[风格声明], [画布规格], [核心内容], [文化背景], [细节清单], [色彩方案], [氛围描述], [排除项]
```

### 分解说明

| 模块 | 作用 | 示例 |
|------|------|------|
| **风格声明** | 锁定画风 | `pixel art, 16-bit retro style, STARDEW VALLEY STYLE` |
| **画布规格** | 尺寸比例 | `2D background, 1920x1080, 16:9 aspect ratio` |
| **核心内容** | 一句话说清画什么 | `university laboratory room, 2076 retro-futuristic` |
| **文化背景** | 文化锚点 | `KOREAN STYLE INTERIOR, Kim Il Sung University, Pyongyang` |
| **细节清单** | 逐项列出要包含的元素 | 分段落：建筑元素、文字标识、装饰品、家具、道具 |
| **色彩方案** | 调色板 | `Primary: celadon green, warm brown; Secondary: cream white` |
| **氛围描述** | 整体感觉 | `warm, inviting, academic yet cultural, blend of tradition and modernity` |
| **排除项** | 不要什么 | `AVOID: generic Asian style, too modern/sterile, cluttered` |

---

## 三、最佳实践（来自实验室成功案例）

### 3.1 风格要"过度指定"

不要只说"像素风"，要说：
```
pixel art, STARDEW VALLEY STYLE PIXEL ART, clear pixels, 16-bit retro
```

### 3.2 文化背景要"层层叠加"

好的文化锚定不是一句话，而是多层渗透：

```
第一层：声明 → KOREAN STYLE INTERIOR
第二层：建筑 → KOREAN HANOK STYLE window frames, dancheong patterns
第三层：文字 → Korean text on wall: "지능형 지휘 시스템 연구실"
第四层：物品 → Korean celadon pottery, traditional tea set, kimchi jar
第五层：符号 → Korean flag, leader portraits, propaganda slogan
第六层：配色 → Korean traditional color scheme: celadon green, dancheong colors
```

### 3.3 细节要"分类列举"

不要一段话混在一起，要用**分类标题**分段：

```
KOREAN ARCHITECTURAL ELEMENTS:
- 条目1
- 条目2

KOREAN TEXT AND SIGNS:
- 条目1
- 条目2

WALL DECORATIONS:
- 条目1
- 条目2
```

### 3.4 用"大写关键词"强调重点

```
STARDEW VALLEY STYLE
KOREAN CULTURAL ELEMENTS
AVOID: generic Asian style
ULTIMATE KOREAN CULTURAL ELEMENTS
```

### 3.5 排除项同样重要

```
AVOID:
- Generic "Asian" style (must be specifically KOREAN)
- Too modern/sterile
- Cold or impersonal atmosphere
- Missing Korean cultural elements
```

---

## 四、角色立绘提示词写法

### 4.1 工作流

```
第1步：生成主图（半身到腿，详细全身）
  → 包含：服装、姿势、基本表情、完整背景
第2步：用主图生成表情差分
  → 保持服装、姿势、光照一致，只改表情
```

### 4.2 主图提示词模板

```
pixel art, 16-bit retro style, full body character portrait, transparent background,
[性别], [年龄], [面型], [眼睛], [发型], [肤色],
Wearing [外套], [内搭], [配饰], [裤子], [鞋子].
Expression: [表情].
Pixel art style reminiscent of Stardew Valley, warm color palette,
full body including feet, [文化风格] style.
```

### 4.3 差分提示词要点

- 使用同一张主图作为 **image2image** 的输入
- 只修改 `Expression: [新表情]` 部分
- 添加 `same clothing, same pose, same lighting` 保持一致性
- 设置 **denoising strength 0.2-0.3**（低重绘幅度）

---

## 五、场景背景提示词写法

### 5.1 模板

```
pixel art, 2D background, 1920x1080, 16:9 aspect ratio,
STARDEW VALLEY STYLE PIXEL ART,
[场景类型], [时代设定], [文化风格]:

[CULTURAL ARCHITECTURAL ELEMENTS]:
- 条目

[CULTURAL TEXT AND SIGNS]:
- 条目

[DECORATIONS]:
- 条目

[SPECIFIC ITEMS]:
- 条目

[VIEW]:
- 条目

COLOR PALETTE ([CULTURE] TRADITIONAL):
- Primary: [颜色]
- Secondary: [颜色]
- Accents: [颜色]

ATMOSPHERE:
- [氛围词1]
- [氛围词2]

STYLE:
- PIXEL ART with clear pixels
- [文化] CULTURAL IDENTITY very prominent

AVOID:
- [排除项1]
- [排除项2]
```

### 5.2 场景类型清单

| 场景类型 | 核心元素要求 |
|---------|------------|
| 室内（实验室/办公室） | 家具、灯光、墙面装饰、窗外景观 |
| 室外（车站/村庄） | 建筑风格、自然元素、天气、时间 |
| 交通工具内部 | 座位、窗户、内饰风格、乘客 |
| 工业/机库 | 机械、工具、照明、油污/灰尘感 |

---

## 六、注意事项

### 6.1 尺寸要求

| 资产类型 | 尺寸 | 格式 |
|---------|------|------|
| 场景背景 | 1920×1080 | JPG/PNG |
| 角色全身立绘 | 1024×2048 | PNG透明 |
| 列车Sprite | 1024×256 | PNG透明 |
| UI图标 | 32×32 | PNG透明 |
| 按钮 | 200×64 | PNG透明 |

### 6.2 生成后处理

- 背景图：检查尺寸 → 缩放到1920×1080 → 放入 `Resources/bg/`
- 角色图：抠图去背景 → 缩放到1024×2048 → 按角色ID分文件夹
- 所有图片：在Unity中设置Texture Type为Sprite(2D and UI)

### 6.3 风格一致性检查清单

- [ ] 所有角色使用相同的像素密度（pixel density）
- [ ] 所有背景使用相同的像素风格
- [ ] 角色与背景的光照方向一致
- [ ] 颜色调色板统一（暖色调为主）
- [ ] 像素边缘清晰，无模糊/抗锯齿混合

---

## 七、错误修正指南

| 问题 | 原因 | 修复 |
|------|------|------|
| 角色风格不一致 | 差分时denoising过高 | 降至0.2-0.3 |
| 背景太单调 | 文化元素不足 | 增加分类细节清单 |
| 颜色不对 | 缺少调色板定义 | 添加COLOR PALETTE段落 |
| 尺寸不对 | 没有指定规格 | 在提示词开头加尺寸 |
| 文化感不够 | 锚定不足 | 用6层文化锚定法 |

---

*本文档基于实验室背景提示词的成功经验编写，所有技巧来自实际验证。*