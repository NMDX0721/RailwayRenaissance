# 铁路复兴：沙能冲击 — 角色音色设计（ElevenLabs Voice Design）

> 工具：ElevenLabs Voice Design（文本提示生成音色）
> 使用方式：ElevenLabs → Voice Lab → Voice Design → 粘贴 Descriptive Prompt → 生成 → 预览测试
> 命名建议：`voice_{角色id}`（如 `voice_suiyue`）
> 生成后用 Multilingual v3 测试韩语发音一致性

---

## 设计原则

1. **每一句提示词 = 一段"角色小传"** —— ElevenLabs 从社会背景、年龄、经历推断音色
2. **强调"怎么说话"而非"多好听"** —— 语气、习惯、情绪色彩比音质重要
3. **加入来源语境** —— "在平壤地铁工作的播音员"比"温柔女人"精确 100 倍
4. **生成后测试** —— 同一音色生成 5 句韩语 + 2 句中文，确认稳定性

---

## 一、岁月（Suiyue）— AI 伙伴

**角色定位**：0721号沙子飞猪号搭载的 AI 原型，**2053 年制造，沉睡 23 年（至 2076）**。冷静、精准、偶尔冷幽默。对"情感"只有理论理解，但试图用数据理解人类。自称"第一个和我聊天的人"时带着微妙的孤独感。

**Voice Design 提示词：**
```
A calm, precise Korean female AI voice, early 20s, an in-car assistant manufactured in 1953 and just awakened after 23 years of sleep. Speaks slowly and deliberately, processing each word, with a faint electronic resonance like an old navigation system. Neutral tone with a subtle underlying warmth — a machine that has spent decades learning kindness in theory. Pauses slightly before emotional words, as if consulting a database. Dry humor delivered perfectly straight, never breaking character. When alone, a hint of loneliness leaks in — "finally someone is talking to me." Clean articulation, standard Pyongyang Korean, also fluent in Mandarin Chinese.
```

**试听句：**
```
[Korean] 심박수 백사십칠을 기록했습니다. 당신은 멈추지 않았어요.
[Korean] 저는 AI입니다. 농담을 하지 않아요.
[Korean] 스물세 해 만에 처음으로, 누군가 저와 이야기하고 있어요.
[Chinese] 我在陈述事实。
[Chinese] 所有矛盾都能用「过渡时期」来解释。
```

---

## 二、林彪悍（Lin Biaohan）— 主角

**角色定位**：**25 岁（2051 年生，2076 年）**，金日成综合大学荣誉研究生，研究方向为智能调度系统。倔强但克制、冷静而坚定——不是莽撞少年，是一个"知道自己选择了更难的路"的年轻人。边陲小村长大的背景让他比同龄研究生更沉稳。继承爷爷林悍的站长遗志与深蓝工作夹克。

**Voice Design 提示词：**
```
A low, restrained Korean male voice, 25 years old, a smart-dispatch researcher at Pyongyang's elite university who grew up in a small border village. Not a boy — a young man carrying responsibility on his shoulders. Speaks with quiet, settled intensity, rarely raising his voice; when frustrated, the anger shows as tension in the throat, clipped and precise, never shouting. Weariness from a long journey and a childhood of loss sits beneath the surface. When gentle, voice drops low and warm, almost protective. Standard Pyongyang Korean, educated but grounded, also fluent in Mandarin Chinese. Timbre slightly deeper than his age suggests — someone who has already buried his grandfather and chosen to carry on his railway.
```

**试听句：**
```
[Korean] 오늘은 정말 길었어. 하지만 돌아가기로 했어.
[Korean] 나는 네 의견을 묻지 않았어! 가속해!
[Korean] 이 철길은 내가 다시 일으킬 거야.
[Chinese] 爷爷，我回来了。
[Chinese] 这条线……我会让它重新跑起来。
```

---

## 三、老陈（Lao Chen）— 末代站长

**角色定位**：68 岁，雾峰村最后一任站长，主角的导师。温暖、朴实、固执而善良。宣读完林悍的遗愿、独自守线四年后迎来林彪悍——苍老里藏着欣慰的泪光。深感铁路已死，却始终不肯放弃最后一点希望。

**Voice Design 提示词：**
```
A warm, weathered North Chinese male voice, late 60s, a mountain-village railway station master who has watched every train stop running over 23 years. Rough but gentle — gravel wrapped in cotton. Speaks slowly with a rural northern accent, eyesight failing but voice still resonant from decades of calling departures. Frequently coughs and clears throat. When talking about the old days, tone softens into fondness; when talking about the closed line, a quiet heaviness. Joyful moments come as reluctant smiles that crack his voice. Northern Chinese Mandarin with country tinge, no standard training, yet somehow dignified.
```

**试听句：**
```
[Chinese] 我守了四年，实在守不动了。
[Chinese] 铁轨还在，总会有人来的。
[Chinese] 你爷爷要是知道你回来了，一定很高兴……
[Chinese] 彪悍啊……回来就好，回来就好。
```

---

## 四、嘉颖徐（Jiaying Xu）— 铁路大亨

**角色定位**：46 岁，东北亚铁路大亨，手持多条电气化干线，被称为"铁路大亨"。爽朗、务实、有远见。爷爷林悍的老友，欣赏有勇气的人，资助林彪悍黑金卡并与他"平壤乡下的大同江茶馆"会面谈合作。

**Voice Design 提示词：**
```
A confident, warm Korean-Chinese businesswoman in her mid-40s, a railway tycoon who turned abandoned electrified lines into golden assets after the sand-energy crash. Speaks with authority but never coldness — a survivor who has made hard decisions and stayed generous. Clear, unhurried, generous with pauses; laughs easily but briefly. Standard Mandarin with a subtle Korean intonation underneath. When talking railways, a spark of genuine passion breaks through the business polish — she truly loves the rails. When giving the black card, tone shifts to almost motherly: "this is a loan, not a gift — but if you lose it, consider it my respect for Lin Han."
```

**试听句：**
```
[Chinese] 这不是送给你的，是借给你的。等你线路盈利了，连本带利还我。
[Chinese] 铁路这行，不是有钱就能活下来的。
[Chinese] 你是林悍的孙子……你爷爷提起你的时候，你还在上高中。
[Chinese] 我等着看你的成绩。
```

---

## 五、配角音色速查

| 角色 | 年龄 | 一句定位 | 语音特征 |
|------|------|---------|---------|
| **张工** | 62 | 退休机械工程师，乐观开朗 | 爽朗大嗓门，笑声贯穿，说话带着机修车间回声感，东北味普通话 |
| **李阿姨** | 55 | 社区热心居民，服务热情 | 温暖絮叨，尾音上扬，像邻居阿姨，句尾拖着关心 |
| **赵师傅** | 55 | 退休铁路工程师，沉稳可靠 | 沉稳寡言，说话节省，每句都有分量，带老铁路人的笃定 |
| **王小弟** | 22 | 刚毕业大学生，阳光热血 | 活力满格，语速快，兴奋时容易破音，带学生的毛躁 |
| **小芳** | 45 | 志愿者，温柔耐心 | 温柔耐心，句尾轻轻放轻，像哄人说话 |
| **检查员** | 35 | 边境检查员 | 官方腔，公事公办，命令式短句，不带个人情绪 |

---

## 六、生成与测试流程

1. **逐角色**生成 Voice Design（一次性多生成几个候选音色）
2. **稳定性测试**：同一音色跑 5 句韩语 + 2 句中文，确认跨语言一致
3. **情绪测试**：用 `[whispers] / [calm] / [dry humor]` 等 prompt 前缀测情感张力
4. **选型**：每个角色保留 2 个候选 → 游戏内试听 → 定稿
5. **存档**：定稿音色在 ElevenLabs 里命名 `voice_{角色id}` 并收藏到 Voice Library

---

## 七、注意事项

- ElevenLabs **免费层每月 1 万字符** —— 测试够用，批量生成需付费（$5/月起）
- Voice Design 生成后**不可二次编辑描述**，需重新生成 —— 所以先广撒网多生成几个再选
- 韩语输出用 **Multilingual v3** 模型效果最佳
- 生成音色带水印（SynthID），游戏商业发布前确认授权条款
- 若 Effect 受限，可退而求其次用 **Gemini 3.1 Flash TTS 预设音色**（免卡免费）跑原型