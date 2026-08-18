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

**角色定位**：0721号搭载的 AI 原型，2053 年制造，沉睡了 23 年。冷静、精准、偶尔冷幽默，对"情感"只有理论理解。

**Voice Design 提示词：**
```
A calm, precise Korean female AI voice, mid-20s, designed for in-car navigation and passenger assistance. Speaks slowly and deliberately, processing each word. Neutral tone with a faint underlying warmth — like a machine that has learned kindness. Slight pause before emotional words, as if consulting a database. Clean articulation, no regional accent, good at both Korean and Chinese. When being humorous, the humor is delivered perfectly straight, never breaking character.
```

**试听句：**
```
[Korean] 심박수 백사십칠을 기록했습니다. 당신은 멈추지 않았어요.
[Korean] 저는 AI입니다. 농담을 하지 않아요.
[Chinese] 我在陈述事实。
[Chinese] 所有矛盾都能用「过渡时期」来解释。
```

---

## 二、林彪悍（Lin Biaohan）— 主角

**角色定位**：28 岁（2076），金日成综合大学荣誉研究生，倔强、青涩但充满希望，继承爷爷的站长遗志。平时内敛，关键时刻坚定。

**Voice Design 提示词：**
```
A low, restrained Korean male voice, late 20s, a university graduate student who grew up between Pyongyang and a small border village. Speaks with quiet intensity — holds back emotions but they leak through. Slightly tired from a long journey, a hint of weariness after 23 years of family history. When angry, the anger is controlled, clipped, never shouting. When gentle, drops to a near-whisper. Neutral North Korean standard accent, clear but not polished. Good at both Korean and Chinese.
```

**试听句：**
```
[Korean] 오늘은 정말 길었어. 하지만 돌아가기로 했어.
[Korean] 나는 네 의견을 묻지 않았어! 가속해!
[Chinese] 爷爷，我回来了。
[Chinese] 这条线……我会让它重新跑起来。
```

---

## 三、老陈（Lao Chen）— 末代站长

**角色定位**：68 岁，雾峰村最后一任站长，主角的导师。温暖、朴实、固执而善良，守了 23 年废线。

**Voice Design 提示词：**
```
A warm, weathered North Chinese male voice, late 60s, a retired railway station master who has spent decades in a small mountain village. Rough but gentle, like gravel wrapped in cotton. Speaks slowly with a country accent, pauses to cough or sigh. Years of calling out train departures have given him a resonant chest voice, but age has softened it. Tired, yet stubbornly hopeful. Northern Chinese Mandarin with a rural tinge, minimal standard pronunciation training.
```

**试听句：**
```
[Chinese] 我守了四年，实在守不动了。
[Chinese] 铁轨还在，总会有人来的。
[Chinese] 你爷爷要是知道你回来了，一定很高兴……
```

---

## 四、嘉颖徐（Jiaying Xu）— 铁路大亨

**角色定位**：46 岁，东北亚铁路大亨，多国混血背景，爽朗、务实、有远见，欣赏有勇气的人。

**Voice Design 提示词：**
```
A confident, warm Korean-Chinese businesswoman in her mid-40s, a railway tycoon who rebuilt several abandoned electrified lines. Speaks with authority but not coldness — a woman who has made hard decisions. Clear, unhurried, generous with pauses. Laughs easily but laughs briefly. Beijing-standard Mandarin with a hint of Korean intonation. When giving advice, the tone shifts to almost motherly warmth. Decisive, direct, no hedging.
```

**试听句：**
```
[Chinese] 这不是送给你的，是借给你的。等你线路盈利了，连本带利还我。
[Chinese] 铁路这行，不是有钱就能活下来的。
```

---

## 五、配角音色速查

| 角色 | 年龄 | 一句定位 | 语音特征 |
|------|------|---------|---------|
| **张工** | 62 | 退休机械工程师 | 爽朗大嗓门，笑声贯穿，说话带修理车间回声感 |
| **李阿姨** | 55 | 社区热心居民 | 温暖絮叨，尾音上扬，像邻居阿姨 |
| **赵师傅** | 55 | 退休铁路工程师 | 沉稳寡言，说话节省，每句都有分量 |
| **王小弟** | 22 | 毕业生学员 | 活力满格，语速快，兴奋容易破音 |
| **小芳** | 45 | 志愿者 | 温柔耐心，句尾轻轻放轻 |
| **检查员** | 35 | 边境检查员 | 官方腔，公事公办，命令式短句 |

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