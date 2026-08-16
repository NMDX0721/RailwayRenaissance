# VN系统素材需求规格文档

> 版本：v1.1
> 创建日期：2026-07-30（v1.1 更新：2026-08-13，表情矩阵对齐《美术资产AI提词.md》v2.0 与实际资源）
> 说明：序章"归乡"所有视觉/音频素材的规格要求

---

## 一、角色立绘

### 1.1 主要角色

| 角色 | 文件名 | 尺寸 | 格式 | 说明 |
|------|--------|------|------|------|
| 林彪悍 | `lin_biaohan`（扁平命名 `lin_biaohan_{表情}.png`） | 1024x2048 源图 | PNG | 主角，25岁，深蓝色工作夹克；**已有16表情**（见1.2） |
| 老陈 | `laochen`（`laochen/{表情}.png`） | 1024x2048 源图 | PNG | 68岁站长，朴实穿着；16表情 |
| 岁月 | `suiyue` | 1024x2048 源图 | PNG | AI角色，可在车内显示 |
| 导师 | `mentor` | 1024x2048 源图 | PNG | 中年男性，戴眼镜 |
| 张工 | `zhanggong` | 1024x2048 源图 | PNG | 62岁，乐观开朗；16表情 |
| 李阿姨 | `liayi` | 1024x2048 源图 | PNG | 55岁，热心；16表情 |
| 王小弟 | `wangxiaodi` | 1024x2048 源图 | PNG | 22岁，阳光热血；16表情 |
| 赵师傅 | `zhaoshifu` | 1024x2048 源图 | PNG | 55岁，沉稳；16表情 |
| 小芳 | `xiaofang` | 1024x2048 源图 | PNG | 45岁，热情；16表情 |

### 1.2 表情变体

**全部角色统一 16 种表情**（以主角已完成16表情为基准）：`normal, smile, sad, surprise, serious, curious, excited, worried, angry, bored, gentle, happy, shocked, shout, smug, wink`

| 角色 | 文件名 | 表情数 | 状态 |
|------|--------|--------|------|
| 林彪悍 | `lin_biaohan_{表情}.png`（扁平） | 16 | ✅ 已生成 |
| 老陈 `laochen` | `laochen/{表情}.png` | 16 | 待生成 |
| 张工 `zhanggong` | `zhanggong/{表情}.png` | 16 | 待生成 |
| 李阿姨 `liayi` | `liayi/{表情}.png` | 16 | 待生成 |
| 王小弟 `wangxiaodi` | `wangxiaodi/{表情}.png` | 16 | 待生成 |
| 赵师傅 `zhaoshifu` | `zhaoshifu/{表情}.png` | 16 | 待生成 |
| 小芳 `xiaofang` | `xiaofang/{表情}.png` | 16 | 待生成 |
| 岁月 `suiyue` | 界面头像 interface.png | 1 | 待生成 |
| 导师 `mentor` | 待定 | 16 | 待生成 |

> 注：v1.0 中的 determined/think/tired/nervous/confused/sympathy/amused/moved/grateful/sleepy 等表情已废弃，以 v2.0 统一 16 表情清单为准。

### 1.3 立绘规格

- **分辨率**：1024x2048像素（1:2比例），PNG with alpha通道
- **格式**：PNG with alpha通道
- **风格**：16-bit retro pixel art，暖色调（STARDEW VALLEY STYLE，与《美术资产AI提词.md》一致）
- **显示区域**：底部约30%被对话框遮挡，实际显示上半身
- **文件命名**：主角为扁平命名 `lin_biaohan_{表情ID}.png`（实际已如此）；其余角色为目录命名 `{角色ID}/{表情ID}.png`（CharacterSpriteManager 按 `characters/` 前缀 + 名字加载，见提词v2.0）
- **主图约束**：单人立绘，禁止参考图集/多人（提示词首行含 SINGLE CHARACTER, ONE PERSON ONLY，见提词v2.0 通用规范）
- **差分产出**：用提词 v2.0 §1.9 的"16表情差分图谱"一次性生成 4×4 网格（1024×1536，半身头到腰部），按格裁剪为 16 张单图

---

## 二、背景素材

### 2.1 场景列表

| 场景ID | 文件名 | 尺寸 | 说明 |
|--------|--------|------|------|
| `black` | 纯黑 | 1920x1080 | 开场/过渡 |
| `abandoned_station` | 废弃车站 | 1920x1080 | 开场旁白背景 |
| `lab` | 实验室 | 1920x1080 | 金日成综合大学实验室 |
| `professor_office` | 导师办公室 | 1920x1080 | 简朴办公室 |
| `hangar` | 停机坪 | 1920x1080 | 沙子飞猪号停放处 |
| `car_interior` | 车内（白天） | 1920x1080 | 沙子飞猪号内部 |
| `car_interior_night` | 车内（夜晚） | 1920x1080 | 夜间版本 |
| `border_town` | 边境小镇 | 1920x1080 | 中国边境补给站 |
| `supply_station` | 补给站 | 1920x1080 | 加沙子的地方 |
| `china_sky` | 中国上空 | 1920x1080 | 飞行中俯瞰 |
| `hebei_town` | 河北小镇 | 1920x1080 | 第二次补给 |
| `henan_town` | 河南小镇 | 1920x1080 | 第三次补给 |
| `wufeng_village` | 雾峰村 | 1920x1080 | 目的地，山谷村落 |
| `wufeng_station` | 雾峰村车站 | 1920x1080 | 破旧站台 |

### 2.2 背景规格

- **分辨率**：1920x1080像素（16:9）
- **格式**：PNG或JPG（无透明度要求）
- **风格**：2D像素风/手绘风格
- **文件命名**：`{场景ID}.png`

---

## 三、BGM音乐

### 3.1 音乐列表

| 音乐ID | 文件名 | 时长 | 情绪 | 使用场景 |
|--------|--------|------|------|----------|
| `melancholy` | melancholy.ogg | 2-3分钟 | 忧伤 | 废弃铁路、历史回顾 |
| `emotional` | emotional.ogg | 2-3分钟 | 感动 | 对话、离别、重逢 |
| `calm` | calm.ogg | 2-3分钟 | 平静 | 日常对话、办公室 |
| `adventure` | adventure.ogg | 2-3分钟 | 冒险 | 出发、新旅程 |
| `mystery` | mystery.ogg | 2-3分钟 | 神秘 | AI初遇、未知 |
| `peaceful` | peaceful.ogg | 2-3分钟 | 宁静 | 夜晚、休息 |
| `travel` | travel.ogg | 2-3分钟 | 旅途 | 飞行中、旅途 |
| `morning` | morning.ogg | 2-3分钟 | 清晨 | 新的一天开始 |
| `hope` | hope.ogg | 2-3分钟 | 希望 | 问题解决、前景光明 |
| `warm` | warm.ogg | 2-3分钟 | 温暖 | 团聚、友好氛围 |

### 3.2 音乐规格

- **格式**：OGG Vorbis（Unity推荐）
- **采样率**：44.1kHz
- **比特率**：128-192kbps
- **循环**：支持无缝循环
- **文件命名**：`{音乐ID}.ogg`
- **存放路径**：`Assets/Resources/bgm/`

---

## 四、音效素材

### 4.1 音效列表

| 音效ID | 文件名 | 时长 | 说明 |
|--------|--------|------|------|
| `button_click` | button_click.ogg | 0.1秒 | 按钮点击 |
| `typewriter` | typewriter.ogg | 循环 | 打字机效果（可选） |
| `page_turn` | page_turn.ogg | 0.3秒 | 翻页效果 |
| `phone_ring` | phone_ring.ogg | 2秒 | 手机响起 |
| `car_start` | car_start.ogg | 1秒 | 车辆启动 |
| `car_fly` | car_fly.ogg | 循环 | 飞行中环境音 |
| `wind` | wind.ogg | 循环 | 风声 |
| `crowd` | crowd.ogg | 循环 | 人群嘈杂（小镇） |

### 4.2 音效规格

- **格式**：OGG Vorbis
- **采样率**：44.1kHz
- **比特率**：128kbps
- **文件命名**：`{音效ID}.ogg`
- **存放路径**：`Assets/Resources/sfx/`

---

## 五、素材优先级

### P0 — 必须有（可运行）

| 素材 | 数量 | 说明 |
|------|------|------|
| 林彪悍立绘 | 16张 | 全部表情（已生成） |
| 老陈立绘 | 16张 | 统一16表情集 |
| 岁月立绘 | 1张 | interface.png |
| 背景 | 5个 | black, lab, car_interior, wufeng_village, wufeng_station |
| BGM | 3首 | calm, emotional, hope |
| 音效 | 2个 | button_click, typewriter |

### P1 — 应该有（完整体验）

| 素材 | 数量 | 说明 |
|------|------|------|
| 所有角色立绘 | 9人×平均5张 | 完整表情系统 |
| 所有背景 | 14个 | 完整场景 |
| 所有BGM | 10首 | 完整音乐 |
| 所有音效 | 8个 | 完整音效 |

### P2 — 锦上添花

| 素材 | 数量 | 说明 |
|------|------|------|
| 角色立绘动画 | 各角色 | 呼吸、眨眼等微动画 |
| 环境音效 | 多种 | 更沉浸的氛围 |
| 过渡特效 | 多种 | 更丰富的转场 |

---

## 六、素材存放路径

```
Assets/Resources/
├── characters/          # 角色立绘
│   ├── lin_biaohan_normal.png   # 主角：扁平命名（已有16表情）
│   ├── lin_biaohan_smile.png
│   ├── ...                      # lin_biaohan_{表情}.png
│   ├── laochen/                 # 其余角色：目录命名（待生成）
│   │   ├── normal.png
│   │   └── ...
│   ├── zhanggong/
│   └── ...
├── bg/                  # 背景
│   ├── lab.png
│   ├── car_interior.png
│   └── ...
├── bgm/                 # 背景音乐
│   ├── calm.ogg
│   └── ...
├── sfx/                 # 音效
│   ├── button_click.ogg
│   └── ...
└── Scripts/             # JSON剧本
    ├── prologue_01_news.json
    ├── prologue_02_day0.json
    └── prologue_03_journey.json
```

---

## 七、制作建议

### 角色立绘
- 建议使用AI生成工具（如Midjourney、Stable Diffusion）生成基础立绘
- 统一风格：温暖色调、柔和线条
- 注意表情一致性：同一角色不同表情保持面部特征一致

### 背景
- 可使用AI生成或手绘
- 注意透视一致性
- 色调与游戏整体风格统一

### 音乐
- 可使用免费音乐库（如FreeMusicArchive、Incompetech）
- 或使用AI音乐生成工具（如Suno、AIVA）
- 注意循环点处理

---

*本文档随素材制作进度持续更新*
