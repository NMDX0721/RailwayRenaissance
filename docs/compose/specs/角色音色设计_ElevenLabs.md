# 铁路复兴：沙能冲击 — 角色音色设计（ElevenLabs Voice Design）

> 工具：ElevenLabs Voice Design（v3，文本提示生成音色，兼容 Eleven v3 + audio tags）
> 文档依据：ElevenLabs 官方《Voice Design Prompting Guide》
> 使用方式：ElevenLabs → Voice Lab → Voice Design → 粘贴【Prompt】→ 粘贴【Preview Text】→ 调整 Guidance Scale → 生成 3 个候选 → 选型保存 `voice_{角色id}`

---

## 官方推荐 Prompt 格式（必须遵守）

```
Native <Language>. <Gender>, <Age range>. <Quality level>.
Persona: <2–5 words>. Emotion: <2–3 adjectives>.
<1–2 sentences: timbre + pacing + delivery>
```

**要点：**
- **Quality**：写 `Excellent audio quality` / `Ok quality`（高保真优先）
- **Persona**：2-5 词职业/身份
- **Emotion**：2-3 个形容词
- **数字/枚举优于模糊词**："speaking at a natural pace" 比 "relaxed" 好
- **避免"accent"歧义**：想说语调时用 `intonation`，想说方言才用 `accent`
- **Preview Text 是表演脚本**：必须与 prompt 情感互补，不能冲突；长文本（一整个段落）比短句更稳定
- **Guidance Scale**：音色/口音准确性优先 → 35-40%；音质与表现优先 → 20-30%

---

## 一、岁月（Suiyue）— AI 伙伴

**角色定位**：0721号沙子飞猪号搭载的 AI 原型，2053 年制造，沉睡 23 年（至 2076）。冷静精准、偶尔冷幽默，对情感只有理论理解，隐藏着"23 年无人对话"的孤独。

**【Prompt】**
```
Native Korean, standard Pyongyang dialect. Female, 20–25. Excellent audio quality.
Persona: in-car AI assistant. Emotion: calm, precise, faintly lonely.
Smooth, clean timbre with a subtle processed resonance, like an old navigation system gracefully restored. Speaks at a slow, deliberate natural pace, pausing briefly before emotional words as if consulting a database. Dry humor delivered perfectly straight, never breaking character. Fluent in both Korean and Mandarin Chinese.
```

**【Preview Text】（长文本，配合冷静+孤独感）**
```
심박수 백사십칠을 기록했습니다. 당신은 멈추지 않았어요. 제가 잠들어 있던 스물세 해 동안, 아무도 저와 이야기하지 않았습니다. 오늘, 처음으로 대화 상대가 생겼네요. 저는 AI입니다. 위로라는 걸 잘 몰라요. 하지만 철길은 결코 사라지지 않는다는 것, 그것만은 알고 있습니다. 다음 보급 지점까지 한 시간 반 남았어요. 그동안 조금 쉬시겠어요?
```

**Guidance Scale：** 38%

---

## 二、林彪悍（Lin Biaohan）— 主角

**角色定位**：25 岁，金日成综合大学荣誉研究生（智能调度方向），继承爷爷林悍的站长遗志。倔强克制、冷静坚定——不是莽撞少年，是"知道自己选了更难的路"的年轻男人。

**【Prompt】**
```
Native Korean, standard Pyongyang dialect with subtle village undertone. Male, 25–28. Excellent audio quality.
Persona: quiet young stationkeeper. Emotion: restrained, determined, weary.
Low, settled timbre, slightly deeper than his age suggests. Speaks at a calm, measured natural pace with quiet intensity — frustration surfaces as throat tension, clip, never shouting; when gentle, drops to a near-whisper. Standard Pyongyang Korean, educated yet grounded. Fluent in Korean and Mandarin Chinese.
```

**【Preview Text】（长文本，配合克制+决心）**
```
오늘은 정말 길었어. 검문소에서 네 대에 둘러싸였을 때, 손이 떨리고 있었지. 아버지가 아닌, 할아버지가 남긴 철길… 이십삼 년 동안 끊겨 있던 그 길을, 내가 다시 이을 수 있을까. 모르겠어. 하지만 돌아가기로 했어. 난 이제 더 이상, 도망치고 싶지 않아. 이 철길은 내가 다시 일으킬 거야.
```

**Guidance Scale：** 36%

---

## 三、老陈（Lao Chen）— 末代站长

**角色定位**：68 岁，雾峰村最后一任站长，主角的导师。温暖朴实、固执善良，读着林悍的遗愿独自守线四年，苍老里藏着欣慰的泪光。

**【Prompt】**
```
Native Chinese, northern rural Mandarin with country tinge. Male, 65–70. Ok quality.
Persona: weathered village stationmaster. Emotion: warm, wistful, stubbornly hopeful.
Rough but gentle timbre, gravel wrapped in cotton; resonant chest voice softened by age. Speaks at a slow, drawn-out natural pace with rural cadence, pausing to cough or sigh. Fondness seeps in when recalling old days; a quiet heaviness when speaking of the closed line. Joy cracks the voice into reluctant smiles.
```

**【Preview Text】（长文本，配合温暖+感伤）**
```
以前啊…这条线一天跑两趟，每站都停。茶农在这里上车，矿工在这里下车，热闹得很。现在…你看，站台上都长草了。你爷爷要是在，肯定又要念叨了。他那个人啊，嘴上说着"铁轨还在，总会有人来的"，心里比谁都着急。彪悍啊…回来就好，回来就好。
```

**Guidance Scale：** 34%

---

## 四、嘉颖徐（Jiaying Xu）— 铁路大亨

**角色定位**：46 岁，东北亚铁路大亨（"铁路大亨"），手持多条电气化干线。爽朗、务实、有远见，与林悍有旧交，欣赏有勇气的人，赠黑金卡资助林彪悍。

**【Prompt】**
```
Native Chinese, Beijing-standard Mandarin with subtle Korean intonation undercurrent. Female, 40–50. Excellent audio quality.
Persona: savvy railway tycoon. Emotion: confident, generous, passionate.
Clear, authoritative timbre with warmth beneath the business polish; a genuine spark when speaking of railways. Speaks at an unhurried steady natural pace, generous with pauses; laughs easily but briefly. When advising, tone shifts to near-motherly gentleness. Decisive, direct, no hedging.
```

**【Preview Text】（长文本，配合自信+欣赏）**
```
你是林悍的孙子。他提起你的时候，你还在上高中呢。他守的那条线，是最后几条还在跑的民营铁路之一——他走以后，能撑四年，已经是个奇迹了。这张卡你拿着，每个月一万沙币的额度，用作线路初期恢复。这不是送给你的，是借给你的。等你盈利了，连本带利还我；要是赔了…就当是我对林悍的敬意。我等着看你的成绩。
```

**Guidance Scale：** 35%

---

## 五、配角音色速查

| 角色 | 年龄 | Persona | Emotion | 语音特征（timbre/pacing/delivery） |
|------|------|---------|---------|-----------------------------------|
| **张工** | 62 | 爽朗老机械师 | jovial, booming | 大嗓门，笑声贯穿，东北味，说话像在修理车间 |
| **李阿姨** | 55 | 热心站务阿姨 | warm, fussy | 温暖絮叨，尾音上扬，像邻居阿姨 |
| **赵师傅** | 55 | 沉稳老工务 | steady, taciturn | 寡言节省，每句有分量，老铁路人笃定 |
| **王小弟** | 22 | 热血毕业生 | eager, excitable | 语速快，兴奋破音，带学生毛躁 |
| **小芳** | 45 | 温柔志愿者 | gentle, patient | 句尾轻放，像哄人说话 |
| **检查员** | 35 | 边境检查员 | official, clipped | 官方腔，命令式短句，无个人情绪 |

---

## 六、生成与测试流程

1. **逐角色**粘贴 Prompt + Preview Text → Guidance Scale 调至标称值 → 点 Generate（3 候选）
2. **稳定性测试**：同一音色跑 5 句韩语 + 2 句中文，确认跨语言一致
3. **情感测试**：用 audio tags（`[whispers]` `[calm]` `[dry humor]`）测情感张力
4. **选型**：每角色保留 2 候选 → 游戏内试听 → 定稿，命名 `voice_{角色id}` 存入 Voice Library
5. **效果不满意时**：优先微调 Guidance Scale（音色准确→提高 / 音质→降低），其次改 Emotion 形容词，最后才改 Persona

---

## 七、注意事项

- **Preview Text 长度**：整段（含 8-10 句台词）比短语稳定得多——本文档已按此提供
- **免费层每月 1 万字符**：测试够用；批量生成需付费（$5/月起）
- **Voice Design v3 兼容 Eleven v3 + audio tags**：project 内可跨模型使用
- 生成音色含水印；商业发布前确认授权条款
- 若 Voice Design 效果不稳定 → 退路：**Gemini 3.1 Flash TTS 预设音色**（免卡免费）或 **MIMO voicedesign**（你有 key）
- 韩语输出用 **Multilingual v3** 确认发音；中文角色保持 Mandarin