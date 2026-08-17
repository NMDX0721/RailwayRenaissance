# 铁路复兴：沙能冲击 — AI 配乐提示词

> 生成工具：Suno AI v4.5+
> 提示词格式：Style 描述 / Lyrics 独立 / 结构标签
> 字符限制：Style ≤ 1000 chars / Lyrics ≤ 3000 chars

---

## 使用说明

**Suno 模式：** Custom Mode，Style 和 Lyrics 分开输入。纯音乐在 Style 中加 `instrumental, no vocals`。

**命名规则：** 配乐名称 = 场景/情绪 + 复用标记。`★` = 高频复用（预算优先），`○` = 中频复用，`·` = 特定场景。

---

## 一、核心复用配乐（★ 高频）

这些配乐贯穿多个场景，优先制作。

### ★ 1.1 主题曲·铁轨还在（Instrumental）

**脚本引用**：`melancholy`（12 次）、`calm`（9 次）  
**用途**：标题界面、序章启程、废弃铁路、回忆爷爷、抵达雾峰村  
**复用说明**：主旋律变奏，慢速钢琴 + 弦乐，可覆盖所有感伤/平静场景

```
Style: Cinematic orchestral, warm and nostalgic, piano melody, soft strings, gentle rising brass, Korean traditional undertone, instrumental, no vocals, 80 BPM
```

### ★ 1.2 旅途·飞行途中（Instrumental）

**脚本引用**：`travel`（6 次）、`adventure`（2 次）  
**用途**：飞行旅途中（Day 1-3）、补给站之间  
**复用说明**：中速开放感，吉他琶音 + 轻打击乐，4.5 天飞行场景通用

```
Style: Ambient folk, spacious and open, acoustic guitar arpeggios, warm pad, light percussion, open sky feeling, instrumental, no vocals, 72 BPM, evolving slowly
```

### ★ 1.3 主题曲·传承（Instrumental）

**脚本引用**：`determination`（6 次）  
**用途**：主角决定出发、首班车启动、坚定信念场景  
**复用说明**：进行曲式，铜管 + 打击乐，所有"决心"时刻通用

```
Style: Cinematic orchestral, determined and rising, marching percussion, French horn, building strings, heroic undertone, instrumental, no vocals, 100 BPM
```

### ★ 1.4 情感·夜幕（Instrumental）

**脚本引用**：`emotional`（10 次）  
**用途**：夜晚飞行、孤独独白、岁月与主角对话  
**复用说明**：慢速钢琴 + 大提琴，夜间情绪场景通用

```
Style: Emotional piano, slow and aching, solo cello countermelody, wide reverb, night sky atmosphere, instrumental, no vocals, 60 BPM, sad minor key
```

---

## 二、场景配乐（○ 中频复用）

### ○ 2.1 清晨·出发（Instrumental）

**脚本引用**：`morning`（4 次）、`peaceful`（1 次）  
**用途**：Day 1 上午、Day 2 下午等时间过渡，安静时刻

```
Style: Peaceful ambient, gentle piano, morning atmosphere, soft field recordings (birds, wind), minimal drone, instrumental, no vocals, 60 BPM
```

### ○ 2.2 日常·温馨（Instrumental）

**脚本引用**：`warm`（3 次）  
**用途**：员工集合、站台聊天、日常对话

```
Style: Acoustic folk, warm and cozy, fingerpicking acoustic guitar, soft cello, light brushed snare, intimate studio feel, instrumental, no vocals, 80 BPM
```

### ○ 2.3 悬疑·边境线（Instrumental）

**脚本引用**：`suspense`（1 次）  
**用途**：边境检查站追逐战（**复用潜力**：后续铁龙竞争对抗场景）

```
Style: Cinematic suspense, tense low strings, electronic pulse, building drums, staccato brass hits, urgent rhythm, instrumental, no vocals, 130 BPM, minor key, no melody
```

### ○ 2.4 搞笑·方向盘之歌（Instrumental）

**脚本引用**：`fun`（2 次）  
**用途**：方向盘搞笑场景、统一便当店购物

```
Style: Playful folk, whimsical acoustic guitar, pizzicato strings, light percussion, quirky and cheerful, instrumental, no vocals, 110 BPM
```

### ○ 2.5 自然·铁路沿线（Instrumental）

**脚本引用**：`ambient_nature`（2 次）、`train_ambient`（2 次）  
**用途**：雾峰村野外、铁路巡视

```
Style: Nature ambient, open field recording vibe, soft wind drone, distant bird calls, sparse guitar notes, atmospheric, instrumental, no vocals, 50 BPM
```

### ○ 2.6 新闻报道（Instrumental）

**脚本引用**：`news`（2 次）  
**用途**：滚动新闻播报

```
Style: Broadcast documentary, neutral piano motif, soft string pad, objective tone, subtle electronic texture, instrumental, no vocals, 70 BPM
```

---

## 三、特定场景配乐（· 单次）

### · 3.1 好奇·初探车厢（Instrumental）

**脚本引用**：`curious`（2 次）  
**用途**：主角进入客舱、探索车辆

```
Style: Light chamber music, curious and gentle, music box, clarinet, soft pizzicato, exploratory feel, instrumental, no vocals, 75 BPM
```

### · 3.2 神秘·岁月唤醒（Instrumental）

**脚本引用**：`mystery`（4 次）  
**用途**：岁月系统启动、AI 初次对话

```
Style: Sci-fi ambient, mysterious synth pads, glitchy electronic textures, soft bass drone, awakening feel, instrumental, no vocals, 70 BPM
```

### · 3.3 都市·平壤街景（Instrumental）

**脚本引用**：`city`（1 次）  
**用途**：平壤城市风貌、前往大同江茶馆的路上

```
Style: Chill electronic, urban atmosphere, lo-fi beat, warm synth chords, city ambience, Korean city night vibe, instrumental, no vocals, 88 BPM
```

---

## 四、歌词配乐（Vocal Songs）

所有歌词配乐采用 Suno Custom Mode 生成：
- **Style 字段**（≤1000 chars）：描述音乐风格、乐器、人声、节奏、变奏
- **Lyrics 字段**（≤3000 chars）：结构标签 + 歌词 + 演唱指示
- 括号 `(whispered)` `(belting)` `(spoken)` 标注演唱方式
- 每首歌附带中文翻译（供游戏内字幕/显示使用）

---

### 4.1 "Sleepers"（铁轨沉睡者）

**用途**：标题界面 / 片尾 credits  
**风格**：美式民谣叙事，温暖男声，中速  
**标题含义**："Sleepers"一语双关——铁路枕木，与沉睡二十三年的岁月  
**游戏内使用**：标题界面播放伴奏版，片尾播放完整版  
**中文翻译**：附后（供游戏内字幕使用）

```
[Style]
American folk ballad, warm male vocals with slight rasp, fingerpicking acoustic guitar, pedal steel guitar, soft brushed drums, upright bass, harmonica in bridge, gentle piano chords, open D tuning feel, crescendo strings in final chorus, sparse intro building to full band by bridge, then stripped back for outro, nostalgic and bittersweet but hopeful, 82 BPM, key of G major, natural reverb, no autotune, no synth

[Lyrics]
[Intro]
(fingerpicking guitar, soft)
G... C... G... D...

[Verse 1]
(gentle, soft)
A whistle cuts the morning haze
Across the hills where wild grass sways
My grandfather's voice, a faded trace
"The rails are still here, son — make your way"

I never knew how much those words would weigh
'Til I stood where the rust and silence lay
Twenty-three years, a frozen page
Waiting for someone to turn the page

[Chorus]
(warm, building slightly)
The rails remain — they're calling out your name
Through every storm, through every field of grain
The rails remain — the story's not in vain
The wheel turns on, again and again

[Verse 2]
(strumming, more motion)
The city lights of Pyongyang far behind
A flying car, an AI, a fractured timeline
I packed my bags with things I couldn't name
Fried chicken, soju, and a child's shame

The dashboard showed a map of what was lost
Every station, every bridge, every crossing crossed
My grandfather's handbook on the passenger seat
A faded note: "For Biaohan — keep the beat"

[Chorus]
(building, fuller)
The rails remain — they're calling out your name
Through every storm, through every field of grain
The rails remain — the story's not in vain
The wheel turns on, again and again

[Bridge]
(reduced, harmonica enters, spoken-sung)
(spoken) I don't know if I can do this alone.
(sung) But the iron doesn't lie, and the ties don't break
When you're standing on the shoulders of the ones who came before
(held) They're still here... waiting at the door

[Solo]
(fingerpicking guitar solo, 8 bars, building)

[Final Chorus]
(full band, crescendo, emotional)
The rails remain — they're calling out your name!
Through every storm, through every field of grain!
The rails remain — the story's not in vain!
The wheel turns on, again and again!

[Outro]
(stripped back, fingerpicking only, fading)
(whispered) Again and again...
(fading) The wheel turns on...
```

**中文翻译（游戏内字幕）：**

```
汽笛划破晨雾
穿过野草摇曳的山岗
爷爷的声音，淡淡的痕迹
「铁轨还在，孩子——去吧」

我从未想过这些话有多重
直到我站在锈迹和沉默之中
二十三年，一页冻结的篇章
等待一个人来翻动

铁轨还在——它们在呼唤你的名字
穿过每一场风暴，每一片麦田
铁轨还在——故事没有白费
车轮转动，一遍又一遍

平壤的灯火远远抛在身后
一辆飞车，一个AI，一段破碎的时间线
我打包了那些叫不出名字的东西
炸鸡、烧酒，和一个孩子的羞愧

仪表盘上显示着已失去的地图
每个车站，每座桥，每个被跨越的交叉口
爷爷的旧手册躺在副驾座上
一行褪色的字：「给彪悍——保持节奏」

我不知道我能不能一个人走下去
但铁不会说谎，枕木不会断裂
当你站在前人肩膀上
他们还在——在门口等着

车轮转动，一遍又一遍
```

---

### 4.2 "별빛 철길"（星光铁轨）

**用途**：边境检查站事件后 / 夜晚飞行  
**风格**：抒情流行摇滚，双人合唱（男 + 女 AI），中速  
**标题含义**：星光落在铁轨上——最黑暗的夜晚，也有光指引方向  
**游戏内使用**：边境检查站剧情后播放  
**中文翻译**：附后

```
[Style]
Korean emotional pop rock, dual vocals (male lead with warm mid-range, female harmony light and airy), piano driven throughout, clean electric guitar arpeggios in verses switching to power chords in chorus, building drums from brushed snare to full kit, string section swells in chorus, dynamic range from hushed intimate verse to explosive full-band chorus, male vocal is tense and restrained in verse releasing to full power in chorus, female harmony floats above in pre-chorus and chorus, bridge is spoken dialogue over soft guitar then female humming, 80 BPM, key of E minor, concert hall reverb, no rap, no electronic beats, no autotune, no synth pads, no pop production, organic rock arrangement, verse vocal is close-mic and breathy, chorus vocal is open and belted, pre-chorus rises gradually, final chorus is slower and more powerful with held notes, outro fades on piano and strings with female humming

[Lyrics]
[Intro]
(soft piano, single notes)
Em... C... G... D...
(female humming)

[Verse 1 - Male]
(quiet, tense)
검문소 불빛이
어둠을 가르고
네 대의 그림자
우리를 둘러쌌지

숨을 죽이고
손을 떨며
얼굴엔 아무것도
드러내지 않았어

[Verse 1 - Chinese translation]
检查站的灯光
划破黑暗
四道影子
围住了我们

屏住呼吸
手在颤抖
脸上却不露
任何表情

[Pre-Chorus - Female]
(hushed, then rising)
심박수 백사십칠
듣고 있어요
무서워도
멈추지 않았죠

[Pre-Chorus - Chinese]
心跳一百四十七
我听到了
即使害怕
你也没有停下

[Chorus - Both]
(belting, full band)
밤이 깊어도
다시 밝아와
저 철길처럼
끝까지 가는 거야

길이 험해도
우린 함께야
이 하늘 아래
혼자가 아니야

[Chorus - Chinese]
夜再深
也会亮起来
就像那条铁轨
一直走到尽头

路再难
我们在一起
在这片天空下
你不是一个人

[Verse 2 - Female]
(gentle, piano only)
이십삼 년의 잠
깨어난 오후
낯선 목소리
처음 듣는 이야기

나는 AI지만
알고 있어요
위로라는 게
뭔지 말이죠

[Verse 2 - Chinese]
二十三年的沉睡
醒来的午后
陌生的声音
第一次听到的故事

我虽然是AI
但我知道
什么是安慰

[Chorus - Both]
(belting, full band)
밤이 깊어도
다시 밝아와
저 철길처럼
끝까지 가는 거야

길이 험해도
우린 함께야
이 하늘 아래
혼자가 아니야

[Bridge - Male]
(spoken over soft guitar)
(spoken) 그 말도 중의적인 거야?
(Female spoken) 저는 AI예요. 중의적 표현은 안 써요.
(spoken) 하지만...
(Female sung) 팔십구 퍼센트...
(humming) 인간은 이걸 '위로'라고 부르죠

[Bridge - Chinese]
(spoken) 那句话也是双关吗？
(岁月 spoken) 我是AI，我不说双关语。
(spoken) 但…
(岁月 sung) 百分之八十九…
(humming) 人类把这叫做「安慰」

[Guitar Solo]
(electric guitar, 8 bars, emotional, building)

[Final Chorus]
(slower, more powerful, held notes)
밤이 깊어도——
다시 밝아와——
저 철길처럼——
끝까지 가는 거야——

[Outro]
(instrumental fade, piano + strings, female humming)
(humming) Mmm... mmm...
(whispered) 혼자가 아니야...
```

**完整中文翻译（游戏内字幕）：**

```
检查站的灯光划破黑暗
四道影子围住了我们
屏住呼吸，手在颤抖
脸上却不露任何表情

心跳一百四十七，我听到了
即使害怕，你也没有停下

夜再深，也会亮起来
就像那条铁轨，一直走到尽头
路再难，我们在一起
在这片天空下，你不是一个人

二十三年的沉睡，醒来的午后
陌生的声音，第一次听到的故事
我虽然是AI，但我知道什么是安慰

那句话也是双关吗？
我是AI，我不说双关语
但…百分之八十九
人类把这叫做「安慰」

夜再深——也会亮起来——
就像那条铁轨——一直走到尽头——
```

---

### 4.3 "남풍"（南风）

**用途**：统一便当店购物 / 朝鲜改革开放主题  
**风格**：韩式民谣流行，女声，中快速  
**标题含义**：南风——来自南方的风，带着禁忌的味道，轻轻吹过平壤的巷子  
**游戏内使用**：序章统一便当店场景  
**中文翻译**：附后

```
[Style]
Korean indie folk pop, female vocals light and playful, bright acoustic guitar fingerpicking in verses switching to strumming in chorus, melodic walking bass, light percussion with shakers and brushed snare, accordion enters in bridge, traditional Korean Pyeongjo scale hint, warm and cheerful with slight ironic undertone, vocal delivery is bright and conversational in verses like telling a story, opening to fuller sweeter tone in chorus, playful vibrato on held notes, verse one is spoken-story style, verse two switches to sung, bridge is spoken then gentle sung, 98 BPM, key of A major, room reverb, no heavy drums, no rap, no synth, no electric guitar, no modern pop, acoustic folk arrangement, street performance feel, intimate recording, verse vocal is close and conspiratorial, chorus opens up warm and inviting, bridge has accordion and thoughtful pause, outro fades on single acoustic guitar

[Lyrics]
[Intro]
(acoustic guitar, cheerful picking)
A... D... A... E...

[Verse 1]
(playful, bright)
평양의 뒷골목
작은 간판 하나
빛바랜 초상화 옆에
"통일 도시락" — 한글로

학생들이 줄 서서
봉지에 뭐가 들었나
치킨 냄새가 풍기고
소주 병이 보이네

[Verse 1 - Chinese]
平壤的后巷
一块小小的招牌
褪色的领袖画像旁边
「统一便当店」——用韩文写着

学生们排着队
袋子里装着什么
炸鸡的香味飘来
还能看到烧酒瓶

[Chorus]
(cheerful, swing feel)
남쪽에서 온 맛
몰래몰래 퍼져가
시대가 변해
조용히 변해
초상화도 모른 척

남쪽에서 온 맛
점점 더 가까워져
이상한 나라
하지만 왠지
따뜻한 이 느낌

[Chorus - Chinese]
来自南边的味道
悄悄地蔓延
时代在变
安静地变
连画像也假装没看见

来自南边的味道
越来越靠近
奇怪的国家
但不知为何
这温暖的感觉

[Verse 2]
(spoken-story style)
점원이 속삭였죠
"학생, 새로 들어온 건데"
"걱정 마요, 저는 유학생"
"이 동네 규칙을 몰라요"

(switching to sung, brighter)
캔커피, 바나나우유
새우깡, 신라면
계산대 위에 쌓인
남쪽의 풍경

[Verse 2 - Chinese]
店员压低声音说
「同学，新到的货」
「放心，我是留学生」
「不懂这儿的规矩」

罐装咖啡、香蕉牛奶
虾条、辛拉面
收银台上堆满的
南边的风景

[Chorus]
(cheerful, swing feel)
남쪽에서 온 맛
몰래몰래 퍼져가
시대가 변해
조용히 변해
초상화도 모른 척

[Bridge]
(accordion enters, slower, thoughtful)
(spoken) 선부유 후부...
(spoken) 먼저 잘 사는 사람들
(sung, gently) 한 걸음 더 앞서
가는 것뿐이야

[Bridge - Chinese]
(spoken) 先富带动后富...
(spoken) 先富起来的那批人
(sung) 只不过是
比我们多走了一步

[Guitar Solo]
(acoustic guitar, folk style, 8 bars)

[Outro]
(soft, acoustic guitar fade)
통일 도시락...
이상한 곳이지만
왠지...
따뜻해

[Outro - Chinese]
统一便当店……
奇怪的地方
但不知为何……
有点温暖
```

**完整中文翻译（游戏内字幕）：**

```
平壤的后巷，一块小小的招牌
褪色的领袖画像旁边
「统一便当店」——用韩文写着

学生们排着队，袋子里装着什么
炸鸡的香味飘来，还能看到烧酒瓶

来自南边的味道，悄悄地蔓延
时代在变，安静地变
连画像也假装没看见
来自南边的味道，越来越靠近
奇怪的国家，但不知为何这温暖的感觉

店员压低声音说：「同学，新到的货」
「放心，我是留学生，不懂这儿的规矩」
罐装咖啡、香蕉牛奶、虾条、辛拉面
收银台上堆满的南边的风景

先富带动后富…先富起来的那批人
只不过是比我们多走了一步

统一便当店……奇怪的地方
但不知为何……有点温暖
```

---

### 4.4 "천리마 신시대에 달리다"（千里马驰骋新时代）

**用途**：序章 Day 3 音乐梗（主角听革命歌曲）  
**风格**：朝鲜革命歌曲，合唱 + 管弦乐，中速  
**标题含义**：游戏内虚构的朝鲜革命歌曲，千里马运动与新时代的官方叙事  
**游戏内使用**：序章主角手机播放的音乐梗，短片段循环  
**中文翻译**：附后

```
[Style]
DPRK revolutionary march, full symphony orchestra, male choir in unison, triumphant brass fanfares with trumpets and French horns, military snare drums, booming timpani, accordion and traditional Korean instruments (janggu, daegeum, taepyeongso) mixed with western orchestra, grandiose and slightly over-the-top, choir is powerful and straight-toned no vibrato, vocal delivery is declamatory and heroic, unison singing throughout, no harmony, no solo vocals, spoken bridge with dramatic echo, snare drum roll into final chorus, 112 BPM, key of C major, Soviet-style production with wide stereo, large hall reverb, no pop elements, no female vocals, no modern instruments, no synthesizers, no autotune, vintage recording feel, intro is snare drums and brass fanfare only, verses are choir with brass and percussion, chorus adds full strings and timpani, bridge drops to spoken word over drum roll, final chorus is slower and grander with held high notes, outro ends abruptly with brass stab and snare drum

[Lyrics]
[Intro]
(military snare drums, brass fanfare, 4 bars)

[Verse 1]
(choir, powerful, unison)
천리마가 달린다
신시대를 달린다
주체의 길 따라
우리는 앞으로!

[Verse 1 - Chinese]
千里马在奔驰
奔驰在新时代
沿着主体的道路
我们向前进！

[Chorus]
(full orchestra, triumphant)
아—— 천리마!
아—— 신시대!
위대한 령도자
가르침 따라
천리마 신시대에
달린다!

[Chorus - Chinese]
啊——千里马！
啊——新时代！
沿着伟大领袖
的教导
千里马在新时代
奔驰！

[Verse 2]
(brass and choir, building)
모래가 에너지로
비행차가 하늘로
수령님 은덕으로
기적을 창조한다!

[Verse 2 - Chinese]
沙子变成能源
飞车飞向天空
在领袖的恩德下
我们创造奇迹！

[Chorus]
(full orchestra, triumphant)
아—— 천리마!
아—— 신시대!
위대한 령도자
가르침 따라
천리마 신시대에
달린다!

[Bridge]
(dramatic pause, spoken)
(spoken, dramatic, with echo)
100전 100승!
백전백승!
(100 battles, 100 victories!)

[Final Chorus]
(slower, grander, held high notes)
아—— 천리마——
아—— 신시대——
달린다——!

[Outro]
(snare drum roll, brass stab, sudden stop)
```

**完整中文翻译（游戏内字幕）：**

```
千里马在奔驰，奔驰在新时代
沿着主体的道路，我们向前进

啊——千里马！啊——新时代！
沿着伟大领袖的教导
千里马在新时代奔驰！

沙子变成能源，飞车飞向天空
在领袖的恩德下，我们创造奇迹！

百战百胜！

啊——千里马——啊——新时代——
奔驰——！
```

---

## 五、Suno 变奏与生成技巧

### 5.1 结构标签速查

| 标签 | 效果 | 使用场景 |
|------|------|---------|
| `[Verse]` | 主歌段落，通常 8 小节 | 叙事部分 |
| `[Chorus]` | 副歌，重复主题 | 高潮记忆点 |
| `[Bridge]` | 过渡段，和声/情绪变化 | 转折点 |
| `[Solo]` | 乐器独奏 | 器乐展示 |
| `[Intro]` | 前奏 | 歌曲开头 |
| `[Outro]` | 尾奏 | 歌曲结束 |
| `(spoken)` | 说话式演唱 | 戏剧性段落 |
| `(belting)` | 强力高音 | 高潮 |
| `(whispered)` | 低语 | 安静时刻 |
| `(humming)` | 哼唱 | 过渡/淡出 |

### 5.2 变奏指令

在 Style 中描述变奏：
- `quiet verse to explosive chorus dynamic` — 从安静主歌到爆发副歌
- `sparse intro building to full band by bridge` — 稀疏前奏逐渐推进到全乐队
- `stripped back for outro, fingerpicking only` — 尾奏收束到独奏
- `crescendo strings in final chorus` — 最后副歌弦乐渐强
- `reduced instrumentation in bridge, harmonica enters` — 桥段乐器减少，口琴加入

### 5.3 生成策略

- 每首歌词生成多个版本（用 `Create` 按钮多次生成）
- 选择最佳版本后用 `Extend` 功能延长至完整长度
- 纯音乐配乐优先使用 `Instrumental` 模式或 Lyrics 留空 + 结构标签
- 歌词配乐可以用 Suno 的 `Persona` 功能记忆音色，跨歌曲保持一致

---

## 五、BGM 引用对照表

| 脚本引用 | 配乐名称 | 复用 | 使用次数 |
|---------|---------|------|---------|
| `melancholy` | ★1.1 主题曲·铁轨还在 | 高频 | 12 次 |
| `calm` | ★1.1 主题曲·铁轨还在 | 高频 | 9 次 |
| `emotional` | ★1.4 情感·夜幕 | 高频 | 10 次 |
| `travel` | ★1.2 旅途·飞行途中 | 高频 | 6 次 |
| `adventure` | ★1.2 旅途·飞行途中 | 高频 | 2 次 |
| `determination` | ★1.3 主题曲·传承 | 高频 | 6 次 |
| `morning` | ○2.1 清晨·出发 | 中频 | 4 次 |
| `peaceful` | ○2.1 清晨·出发 | 中频 | 1 次 |
| `warm` | ○2.2 日常·温馨 | 中频 | 3 次 |
| `suspense` | ○2.3 悬疑·边境线 | 中频 | 1 次 |
| `fun` | ○2.4 搞笑·方向盘之歌 | 中频 | 2 次 |
| `ambient_nature` | ○2.5 自然·铁路沿线 | 中频 | 2 次 |
| `train_ambient` | ○2.5 自然·铁路沿线 | 中频 | 2 次 |
| `news` | ○2.6 新闻报道 | 中频 | 2 次 |
| `curious` | ·3.1 好奇·初探车厢 | 单次 | 2 次 |
| `mystery` | ·3.2 神秘·岁月唤醒 | 单次 | 4 次 |
| `city` | ·3.3 都市·平壤街景 | 单次 | 1 次 |
| `silence` | 静音 | — | 1 次 |

---

## 六、优先级

| 优先级 | 配乐 | 理由 |
|-------|------|------|
| **P0** | ★1.1 主题曲·铁轨还在 | 标题界面 + 21 次场景引用，覆盖最广 |
| **P0** | ★1.3 主题曲·传承 | 6 次决心场景 |
| **P0** | ★1.4 情感·夜幕 | 10 次夜间情感场景 |
| **P1** | ★1.2 旅途·飞行途中 | 8 次飞行场景 |
| **P1** | 4.1 主题曲《铁轨还在》 | 标题界面/credits，与 P0 同旋律 |
| **P1** | ○2.3 悬疑·边境线 | 边境检查站必需 |
| **P1** | 4.2 《天黑了总会亮的》 | 边境检查后场景 |
| **P2** | ○2.1 清晨·出发 | 时间过渡 |
| **P2** | ○2.2 日常·温馨 | 团队/日常对话 |
| **P2** | 4.3 《南边来的味道》 | 统一便当店 |
| **P2** | ○2.4 搞笑·方向盘之歌 | 搞笑场景 |
| **P3** | ○2.5 自然·铁路沿线 | 环境氛围 |
| **P3** | ·3.1 好奇·初探车厢 | 单次使用 |
| **P3** | ·3.2 神秘·岁月唤醒 | 4 次可复用 |
| **P3** | ·3.3 都市·平壤街景 | 单次使用 |
| **P3** | ○2.6 新闻报道 | 2 次新闻 |
| **P3** | 4.4 《千里马奔驰在新时代》 | 音乐梗，可用现成素材替 |