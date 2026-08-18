# 铁路复兴：沙能冲击 — 角色音色设计（ElevenLabs Voice Design）

> 工具：ElevenLabs Voice Design（v3，文本提示生成音色，兼容 Eleven v3 + audio tags）
> 文档依据：ElevenLabs 官方《Voice Design Prompting Guide》
> 使用方式：ElevenLabs → Voice Lab → Voice Design → 粘贴【Prompt】→ 粘贴【Preview Text】→ 调整 Guidance Scale → 生成 3 个候选 → 选型保存 `voice_{角色id}`

---

## 🔊 全局语言策略（方案 B — 按角色语言混搭）

**设定依据**：AI 车载系统（朝鲜产）用**韩语**；朝鲜/东北亚角色（嘉颖徐、检查员、导师）用**韩语**；中国雾峰村铁路人（老陈、张工、李阿姨、赵师傅、王小弟、小芳）用**中文**；主角林彪悍**双语**（平壤留学的中国青年，日常中文、与岁月对话保留少量韩语词）。

| 角色 | 语言 | Preview Text 语言 |
|------|------|------------------|
| 岁月（AI） | 韩语 | 韩语 |
| 林彪悍 | 中文为主 + 韩语词 | 中文 |
| 老陈 / 张工 / 李阿姨 / 赵师傅 / 王小弟 / 小芳 | 中文 | 中文 |
| 嘉颖徐 | 韩语（东北亚） | 韩语 |
| 检查员 / 导师 | 韩语 | 韩语 |

**注意**：ElevenLabs Voice Design 生成的是"单一音色的整体口音风格"。若某角色须双语（林彪悍），建议**先生成中文音色**，韩语台词另用该音色在 Multilingual 模型下试听，若韩语发音明显带中文腔，则为林彪悍的韩语线单独克隆/生成韩语变体。

---

## 一、岁月（Suiyue）— AI 伙伴（韩语）

**角色定位**：0721号沙子飞猪号搭载的 AI 原型，2053 年制造，沉睡 23 年（至 2076）。冷静精准、偶尔冷幽默，对情感只有理论理解，隐藏着"23 年无人对话"的孤独。

**【Prompt】**
```
Native Korean, standard Pyongyang dialect. Female, 20–25. Excellent audio quality.
Persona: in-car AI assistant. Emotion: calm, precise, faintly lonely.
Smooth, clean timbre with a subtle processed resonance, like an old navigation system gracefully restored. Speaks at a slow, deliberate natural pace, pausing briefly before emotional words as if consulting a database. Dry humor delivered perfectly straight, never breaking character.
```

**【Preview Text】（韩语长文本，配合冷静+孤独感）**
```
심박수 백사십칠을 기록했습니다. 당신은 멈추지 않았어요. 제가 잠들어 있던 스물세 해 동안, 아무도 저와 이야기하지 않았습니다. 오늘, 처음으로 대화 상대가 생겼네요. 저는 AI입니다. 위로라는 걸 잘 몰라요. 하지만 철길은 결코 사라지지 않는다는 것, 그것만은 알고 있습니다. 다음 보급 지점까지 한 시간 반 남았어요. 그동안 조금 쉬시겠어요?
```

**Guidance Scale：** 38%

---

## 二、林彪悍（Lin Biaohan）— 主角（中文为主，双语）

**角色定位**：25 岁中国青年，金日成综合大学荣誉研究生（智能调度方向，留朝深造）。继承爷爷林悍的站长遗志。倔强克制、冷静坚定——不是莽撞少年，是"知道自己选了更难的路"的年轻男人。归乡后对村民讲中文，对岁月保留韩语词。

**【Prompt】**
```
Native Mandarin Chinese, standard northern accent with subtle youthfulness. Male, 25–28. Excellent audio quality.
Persona: quiet young stationkeeper. Emotion: restrained, determined, weary.
Low, settled timbre, slightly deeper than his age suggests. Speaks at a calm, measured natural pace with quiet intensity — frustration surfaces as throat tension, clipped, never shouting; when gentle, drops to a near-whisper. Educated yet grounded, an overseas-trained rail researcher returning home.
```

**【Preview Text】（中文长文本，配合克制+决心，含一句韩语）**
```
今天……真是够长的。检查站四辆车围过来的时候，我手在抖。爷爷留下的这条线，断了二十三年，我真能把它重新接起来吗？不知道。但是——我决定了，要回去。我不想再逃了。这条线，我要让它重新跑起来……岁月，가자.（走吧，岁月。）
```

**Guidance Scale：** 36%

> 💡 若需岁月线保持韩语语音：本角色中文音色用于村内对白；岁月相关台词（韩语语码切换）可另在 Multilingual 下测试，发音不佳则单独克隆韩语变体。

---

## 三、老陈（Lao Chen）— 末代站长（中文）

**角色定位**：68 岁，雾峰村最后一任站长，主角的导师。温暖朴实、固执善良，读着林悍的遗愿独自守线四年，苍老里藏着欣慰的泪光。

**【Prompt】**
```
Native Mandarin Chinese, northern rural dialect with country tinge. Male, 65–70. Ok quality.
Persona: weathered village stationmaster. Emotion: warm, wistful, stubbornly hopeful.
Rough but gentle timbre, gravel wrapped in cotton; resonant chest voice softened by age. Speaks at a slow, drawn-out natural pace, pausing to cough or sigh. Fondness seeps in when recalling old days; a quiet heaviness when speaking of the closed line. Joy cracks the voice into reluctant smiles.
```

**【Preview Text】（中文长文本，配合温暖+感伤）**
```
以前啊……这条线一天跑两趟，每站都停。茶农在这里上车，矿工在这里下车，热闹得很。现在……你看，站台上都长草了。你爷爷要是在，肯定又要念叨了。他那个人啊，嘴上说着"铁轨还在，总会有人来的"，心里比谁都着急。彪悍啊……回来就好，回来就好。
```

**Guidance Scale：** 34%

---

## 四、嘉颖徐（Jiaying Xu）— 铁路大亨（韩语）

**角色定位**：46 岁，东北亚铁路大亨（"铁路大亨"），手持多条电气化干线。爽朗、务实、有远见，与林悍有旧交，欣赏有勇气的人，赠黑金卡资助林彪悍。

**【Prompt】**
```
Native Korean, Seoul-influenced business register (international businesswoman, not heavily regional). Female, 40–50. Excellent audio quality.
Persona: savvy railway tycoon. Emotion: confident, generous, passionate.
Clear, authoritative timbre with warmth beneath the business polish; a genuine spark when speaking of railways. Speaks at an unhurried steady natural pace, generous with pauses; laughs easily but briefly. When advising, tone shifts to near-motherly gentleness. Decisive, direct, no hedging.
```

**【Preview Text】（韩语长文本，配合自信+欣赏）**
```
당신이 임한의 손자라니. 그분이 당신 이야기를 하실 때면, 아직 고등학생이었죠. 그분이 지키신 그 철길은, 마지막까지 남아 있던 민영 노선 중 하나였어요. 그분이 떠나신 후로도 4년을 버틴 것만으로도, 이미 기적이에요. 이 카드를 받으세요. 매달 만 원의 한도로, 노선 초기 복구에 사용하시면 돼요. 선물이 아니에요. 빌려드리는 거예요. 수익이 나면, 원금과 이자를 갚으세요. 만약 잃게 된다면… 그건 제가 임한에게 보내는 예의라고 생각할게요. 당신의 성과를 기대하겠습니다.
```

**Guidance Scale：** 35%

---

## 五、配角音色速查（中文角色）

| 角色 | 年龄 | Persona | Emotion | 语音特征（timbre/pacing/delivery） | Preview 语言 |
|------|------|---------|---------|-----------------------------------|-------------|
| **张工** | 62 | 爽朗老机械师 | jovial, booming | 大嗓门，笑声贯穿，东北味，说话像在修理车间 | 中文 |
| **李阿姨** | 55 | 热心站务阿姨 | warm, fussy | 温暖絮叨，尾音上扬，像邻居阿姨 | 中文 |
| **赵师傅** | 55 | 沉稳老工务 | steady, taciturn | 寡言节省，每句有分量，老铁路人笃定 | 中文 |
| **王小弟** | 22 | 热血毕业生 | eager, excitable | 语速快，兴奋破音，带学生毛躁 | 中文 |
| **小芳** | 45 | 温柔志愿者 | gentle, patient | 句尾轻放，像哄人说话 | 中文 |
| **检查员** | 35 | 边境检查员（朝鲜语） | official, clipped | 官方腔，命令式短句，无个人情绪 | 韩语 |
| **导师** | 50 | 大学教授（朝鲜语） | authoritative, warm | 平和沉稳，学术气，对学生关切 | 韩语 |

---

## 六、生成与测试流程

1. **逐角色**粘贴 Prompt + Preview Text → Guidance Scale 调至标称值 → 点 Generate（3 候选）
2. **稳定性测试**：同一音色跑 5 句本语言 + 2 句跨语言，确认一致
3. **双语角色测试**（林彪悍）：中文音色跑 2 句韩语，确认发音可接受；不可接受则单独克隆韩语变体
4. **情感测试**：用 audio tags（`[whispers]` `[calm]` `[dry humor]`）测情感张力
5. **选型**：每角色保留 2 候选 → 游戏内试听 → 定稿，命名 `voice_{角色id}` 存入 Voice Library
6. **效果不满意时**：优先调 Guidance Scale（音色准确→提高 / 音质→降低），其次改 Emotion 形容词，最后改 Persona

---

## 七、注意事项

- **Preview Text 长度**：整段（含 8-10 句台词）比短语稳定——本文档已按此提供
- **免费层每月 1 万字符**：测试够用；批量生成需付费（$5/月起）
- **Voice Design v3 兼容 Eleven v3 + audio tags**：project 内可跨模型使用
- 生成音色含水印；商业发布前确认授权条款
- 若 Voice Design 效果不稳定 → 退路：**Gemini 3.1 Flash TTS 预设音色**（免卡免费）或 **MIMO voicedesign**（你有 key）
- 韩语输出用 **Multilingual v3** 确认发音；中文角色保持 Mandarin