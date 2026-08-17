# 铁路复兴：沙能冲击 — AI 配乐提示词

> 生成工具：Suno AI v4.5+
> 提示词格式：Style 描述 / Lyrics 独立 / 结构标签 `[Verse]` `[Chorus]` `[Bridge]` `[Outro]`
> 字符限制：Style ≤ 1000 chars / Lyrics ≤ 3000 chars（约 40-60 行）

---

## 使用说明

**Suno 模式选择：**
- Custom Mode：Style 和 Lyrics 分开输入
- 纯音乐（Instrumental）：Style 中加 `instrumental, no vocals`，Lyrics 留空或只放结构标签

**提示词要点：**
- 不要用 `create/make/generate` 等命令式语言，直接描述音乐（"Acoustic folk ballad, female vocals..."）
- 最关键的要素（genre, mood, vocal type）放在 Style 开头，超过 1000 字符会被自动截断
- 使用 `[Verse]`、`[Chorus]`、`[Bridge]` 等标签控制歌曲结构
- 括号内标注演唱方式：`(whispered)`、`(spoken)`、`(belting)`、`(humming)`
- 可用 `No drums, no electric instruments` 等负面提示排除不需要的乐器

---

## 一、纯音乐配乐（Instrumental BGM）

### 1.1 主题曲·启程（Title Screen / 序章主题）

**用途**：标题界面、序章启程氛围
**场景**：News scroll、出发前

```
Style: Cinematic orchestral, warm and hopeful, piano melody, soft strings, gentle rising brass, Korean traditional elements (gayageum hint), nostalgic but forward-looking, instrumental, no vocals, 85 BPM
```

### 1.2 旅途·飞行（Travel / Journey）

**用途**：飞行旅途中的背景音乐（Day 1-3）
**场景**：prologue_03 大部分场景

```
Style: Ambient folk, spacious and contemplative, acoustic guitar arpeggios, warm pad, light percussion, open sky feeling, instrumental, no vocals, 72 BPM, evolving slowly
```

### 1.3 宁静·清晨（Morning）

**用途**："Day 1 上午"、"Day 2 下午" 等时间过渡
**场景**：prologue_03 场景切换

```
Style: Peaceful ambient, gentle piano, morning atmosphere, soft field recordings (birds, wind), minimal drone, instrumental, no vocals, 60 BPM
```

### 1.4 温馨·日常（Warm / Calm）

**用途**：员工集合、站台聊天、日常对话
**场景**：prologue_06 团队见面、prologue_05 巡视

```
Style: Acoustic folk, warm and cozy, fingerpicking acoustic guitar, soft cello, light brushed snare, intimate studio feel, instrumental, no vocals, 80 BPM
```

### 1.5 感伤·回忆（Melancholy）

**用途**：爷爷回忆、废弃铁路、告别
**场景**：prologue_01 废弃车站、prologue_04 抵达、老陈对话

```
Style: Melancholic piano, slow emotional strings, solo violin melody, sparse arrangement, rain-window atmosphere, instrumental, no vocals, 65 BPM, minor key
```

### 1.6 决心·出发（Determination）

**用途**：主角决定出发、首班车启动
**场景**：prologue_02 取车、prologue_08 首班车

```
Style: Cinematic orchestral, determined and rising, marching percussion, French horn, building strings, heroic undertone, instrumental, no vocals, 100 BPM
```

### 1.7 幽默·轻松（Fun）

**用途**：方向盘搞笑场景、统一便当店购物
**场景**：prologue_02 车内搞笑、prologue_03 音乐梗

```
Style: Playful folk, whimsical acoustic guitar, pizzicato strings, light percussion, quirky and cheerful, instrumental, no vocals, 110 BPM
```

### 1.8 悬念·紧张（Suspense）

**用途**：边境检查站追逐战
**场景**：prologue_03 边境检查

```
Style: Cinematic suspense, tense low strings, electronic pulse, building drums, staccato brass hits, urgent rhythm, instrumental, no vocals, 130 BPM, minor key, no melody
```

### 1.9 自然·环境（Ambient Nature）

**用途**：雾峰村野外、铁路沿线
**场景**：prologue_05 铁路巡视

```
Style: Nature ambient, open field recording vibe, soft wind drone, distant bird calls, sparse guitar notes, atmospheric, instrumental, no vocals, 50 BPM
```

### 1.10 伤心·夜幕（Emotional / Night）

**用途**：夜晚飞行、孤独独白
**场景**：prologue_03 夜晚、prologue_04 抵达前

```
Style: Emotional piano, slow and aching, solo cello countermelody, wide reverb, night sky atmosphere, instrumental, no vocals, 60 BPM, sad minor key
```

### 1.11 新闻·报道（News）

**用途**：滚动新闻播报
**场景**：prologue_01 新闻

```
Style: Broadcast documentary, neutral piano motif, soft string pad, objective tone, subtle electronic texture, instrumental, no vocals, 70 BPM
```

### 1.12 好奇·探索（Curious）

**用途**：主角探索车厢、发现新事物
**场景**：prologue_02 进入客舱

```
Style: Light chamber music, curious and gentle, music box, clarinet, soft pizzicato, exploratory feel, instrumental, no vocals, 75 BPM
```

### 1.13 神秘·未知（Mystery）

**用途**：岁月初次唤醒、系统启动
**场景**：prologue_02 岁月 boot

```
Style: Sci-fi ambient, mysterious synth pads, glitchy electronic textures, soft bass drone, awakening feel, instrumental, no vocals, 70 BPM
```

### 1.14 都市·平壤（City）

**用途**：平壤城市风貌、打车去茶馆
**场景**：prologue_02 嘉颖徐会面途中

```
Style: Chill electronic, urban atmosphere, lo-fi beat, warm synth chords, city ambience, Korean city night vibe, instrumental, no vocals, 88 BPM
```

---

## 二、歌词配乐（Vocal Songs）

### 2.1 主题曲《铁轨还在》

**用途**：标题界面 / 最终 credits
**风格**：民谣叙事，温暖男声，中速
**主题**：爷爷的铁路、传承、复兴

```
[Style]
Acoustic folk ballad, warm male vocals, Chinese folk influence, fingerpicking acoustic guitar, soft strings, gentle percussion, nostalgic and hopeful, 85 BPM

[Lyrics]
[Intro]
(fingerpicking guitar)

[Verse 1]
远方传来汽笛声
穿过晨雾和山岭
爷爷说铁轨还在
总会有人来

[Chorus]
铁轨还在 总会有人来
穿过荒草和等待
铁轨还在 故事还没完
新的车轮 就要转起来

[Verse 2]
二十三年的沉睡
在某个午后醒来
方向盘上落了灰
窗外风景已改

[Chorus]
铁轨还在 总会有人来
穿过荒草和等待
铁轨还在 故事还没完
新的车轮 就要转起来

[Bridge]
(spoken) 时代变了，岁月。
(gently) 但有些东西...
不会变。

[Chorus]
铁轨还在 总会有人来
穿过荒草和等待
铁轨还在 故事还没完
新的车轮 就要转起来

[Outro]
(fingerpicking fading)
新的车轮...就要转起来
```

### 2.2 《天黑了总会亮的》

**用途**：边境检查站事件后 / 夜晚飞行
**风格**：抒情流行，男女对唱，中慢速
**主题**：困境中的希望、岁与主角的默契

```
[Style]
Emotional pop ballad, dual vocals (male + female AI), piano driven, warm strings, soft electronic pad, intimate and hopeful, 78 BPM

[Lyrics]
[Verse 1 - 林彪悍]
天黑了
我说的是天气
也是今天的运气
边境线上 四辆车围过来
我手在抖 但脸上没表情

[Pre-Chorus - 岁月]
检测到您的心率
一百四十七
您在害怕
但您没有放弃

[Chorus - 合]
天黑了
总会亮的
就像铁轨
总会有人来的
天黑了
总会亮的
这条路上
你不是一个人

[Verse 2 - 岁月]
我是一台AI
但我知道什么是安慰
这句话百分之八十九
会被人类归类为真诚

[Chorus - 合]
天黑了
总会亮的
就像铁轨
总会有人来的
天黑了
总会亮的
这条路上
你不是一个人

[Bridge - 林彪悍]
(spoken) 你这句话，也是双关？
(岁月 spoken) 我是AI，我不会用双关。
(岁月 spoken) 但这句话...
(岁月 sung) 百分之八十九的概率
(humming) 会被人类归类为「安慰」

[Outro]
(humming fading)
星星一颗接一颗
亮了起来
```

### 2.3 《南边来的味道》

**用途**：统一便当店购物 / 朝鲜改革开放主题
**风格**：韩式民谣 + 轻快流行，女声，中速
**主题**：平壤街头的韩式消费文化，反差与幽默

```
[Style]
Korean folk pop, female vocals, bright acoustic guitar, light percussion, Korean traditional scale hint, cheerful and slightly ironic, 95 BPM

[Lyrics]
[Verse 1]
平壤的巷子里
有一家小店
褪色的领袖画像旁边
贴着韩文的招牌

排队的学生
手里拎着塑料袋
里面装着炸鸡和烧酒
还有香蕉牛奶

[Chorus]
这是南边来的味道
偷偷摸摸却越来越热闹
时代在变 悄悄在变
连领袖画像都假装没看到

[Verse 2]
店员压低声音说
同学新到的南方货
我笑了笑说放心
我是留学生不懂规矩

[Chorus]
这是南边来的味道
偷偷摸摸却越来越热闹
时代在变 悄悄在变
连领袖画像都假装没看到

[Bridge]
(spoken) 先富带动后富
上面默许的那批人
先走一步

[Outro]
(acoustic guitar fade)
통일 도시락...
奇怪的地方
但又莫名亲切
```

### 2.4 《千里马奔驰在新时代》

**用途**：序章 Day 3 音乐梗场景（主角听歌）
**风格**：朝鲜革命歌曲式，管弦乐 + 合唱，听感上"官方但好笑"
**主题**：游戏内虚构的朝鲜革命歌曲，带幽默感

```
[Style]
DPRK revolutionary song, full orchestra, male choir, patriotic brass, marching drums, grandiose and slightly over-the-top, 120 BPM

[Lyrics]
[Verse 1]
千里马在奔驰
奔驰在新时代
主体思想的指引下
我们向着未来

[Chorus]
啊 千里马
啊 新时代
领袖的教导永不忘
千里马奔驰在新时代

[Verse 2]
沙子变成能源
飞车飞向天空
在伟大领袖的关怀下
我们创造奇迹

[Chorus]
啊 千里马
啊 新时代
领袖的教导永不忘
千里马奔驰在新时代

[Bridge]
(spoken, dramatic)
100전 100승!
(100 battles, 100 victories!)

[Outro]
(choir fading)
千里马...
奔驰在新时代...
```

---

## 三、BGM 引用对照表

| 脚本引用名 | 对应配乐 | 类型 | 用途场景 |
|-----------|---------|------|---------|
| `melancholy` | 1.5 感伤·回忆 | 纯音乐 | 爷爷回忆、废弃铁路、抵达 |
| `travel` | 1.2 旅途·飞行 | 纯音乐 | 飞行途中 |
| `morning` | 1.3 宁静·清晨 | 纯音乐 | 时间过渡 |
| `warm` | 1.4 温馨·日常 | 纯音乐 | 团队见面、日常对话 |
| `determination` | 1.6 决心·出发 | 纯音乐 | 首班车、出发 |
| `fun` | 1.7 幽默·轻松 | 纯音乐 | 搞笑场景、购物 |
| `suspense` | 1.8 悬念·紧张 | 纯音乐 | 边境检查 |
| `ambient_nature` | 1.9 自然·环境 | 纯音乐 | 野外巡视 |
| `emotional` | 1.10 伤心·夜幕 | 纯音乐 | 夜晚独白 |
| `news` | 1.11 新闻·报道 | 纯音乐 | 新闻滚动 |
| `curious` | 1.12 好奇·探索 | 纯音乐 | 探索车厢 |
| `mystery` | 1.13 神秘·未知 | 纯音乐 | 岁月启动 |
| `city` | 1.14 都市·平壤 | 纯音乐 | 平壤街景 |
| `calm` | 1.4 温馨·日常 | 纯音乐 | 平静对话 |
| `peaceful` | 1.3 宁静·清晨 | 纯音乐 | 安静时刻 |
| `adventure` | 1.2 旅途·飞行 | 纯音乐 | 旅程开始 |
| `train_ambient` | 1.9 自然·环境 | 纯音乐 | 铁路环境 |
| `silence` | 无 | 静音 | 特殊场景 |

---

## 四、优先级

| 优先级 | 配乐 | 说明 |
|-------|------|------|
| **P0** | 1.1 主题曲·启程 | 标题界面 + 序章必需 |
| **P0** | 1.5 感伤·回忆 | 第一章大量使用 |
| **P0** | 1.6 决心·出发 | 首班车场景 |
| **P1** | 2.1 主题曲《铁轨还在》 | 标题界面/credits |
| **P1** | 1.2 旅途·飞行 | 第三章大量使用 |
| **P1** | 1.8 悬念·紧张 | 边境检查站 |
| **P1** | 2.2 《天黑了总会亮的》 | 边境检查后场景 |
| **P2** | 1.4 温馨·日常 | 团队/日常对话 |
| **P2** | 2.3 《南边来的味道》 | 统一便当店 |
| **P2** | 1.7 幽默·轻松 | 搞笑场景 |
| **P3** | 其余 instrumental | 氛围/过渡 |
| **P3** | 2.4 千里马革命歌曲 | 音乐梗 |