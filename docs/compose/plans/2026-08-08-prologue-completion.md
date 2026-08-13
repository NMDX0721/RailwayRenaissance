# 序章剧本JSON补全 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补全序章剧本中缺失的 Day 3 白天（新闻6/7/8）和 Day 4 上午（新闻9）场景，使序章JSON脚本完整覆盖原始剧本。

**Architecture:** 在现有 `prologue_03_journey.json` 中插入缺失的场景节点，遵循已有的JSON格式和对话条目结构。不修改任何C#代码。

**Tech Stack:** JSON (Unity JsonUtility), 参考原始剧本 `参考资料/序章剧本_归乡.md`

## Global Constraints

- 引擎: Unity 6000.4.6f1
- JSON格式遵循 `VNData.cs` 中定义的 `ScriptData` / `SceneData` / `DialogueEntry` 结构
- 对话条目字段: `t`(类型), `s`(说话者), `text`(文本), `e`(表情), `bg`(背景), `bgm`(音乐), `transition`(转场)
- 资源路径: `Assets/Resources/Scripts/`
- 不直接编辑 `.unity` YAML 文件

---

## File Structure

- **Modify:** `Assets/Resources/Scripts/prologue_03_journey.json` — 在 Day 2 夜晚（新闻4）场景之后、Day 3 傍晚补给场景之前，插入 Day 3 白天的3个新闻场景；在 Day 3 傍晚~Day 4 到达之间，插入 Day 4 上午的新闻9场景

---

### Task 1: 插入 Day 3 白天新闻场景（新闻6/7/8）

**Covers:** 补全世界观铺垫 — 朝鲜第二代载具、铁路客运量历史最低、国有铁路解体

**Files:**
- Modify: `Assets/Resources/Scripts/prologue_03_journey.json:159-160` (在 Day 2 夜晚新闻4场景结束后、Day 3 傍晚补给场景之前插入)

**Interfaces:**
- Consumes: 现有JSON结构（`SceneData` 数组）
- Produces: 3个新场景节点插入到 `scenes` 数组中

- [ ] **Step 1: 在 prologue_03_journey.json 中定位插入点**

找到 Day 2 夜晚新闻4场景（bg: "car_interior_night", bgm: "mystery" 的第二个场景，以 "比如...您爷爷守了一辈子的那条线。" 结尾）和 Day 3 傍晚补给场景（bg: "henan_town"）之间。

- [ ] **Step 2: 插入新闻6场景（朝中社 2063年 — 第二代沙能载具）**

在插入点添加以下场景节点：

```json
{
  "bg": "china_sky",
  "bgm": "travel",
  "d": [
    {"t": "n", "text": "【Day 3 白天】"},
    {"t": "n", "text": "【沙子飞猪号内部】"},
    {"t": "d", "s": "林彪悍", "text": "还有最后一天...看看新闻吧。", "e": "think"},
    {"t": "n", "text": "【手机屏幕显示：朝中社 2063年10月15日】"},
    {"t": "n", "text": "在伟大领袖金正恩同志的亲切关怀下，联合沙能科技公司与朝鲜沙能科技研究院共同研制第二代沙能载具成功"},
    {"t": "n", "text": "第二代载具包括四种型号：地面通勤车"沙驴号"、紧凑型"沙鸡号"、货运型"沙牛号"和SUV型"沙熊号"。"},
    {"t": "d", "s": "林彪悍", "text": "沙驴号...沙鸡号...沙牛号...这名字...真够接地气的。", "e": "amused"},
    {"t": "d", "s": "岁月", "text": "第二代载具的推出...是铁路衰落的关键节点。"},
    {"t": "d", "s": "岁月", "text": "尤其是'沙驴号'...售价仅为飞猪号的五分之一。普通人也买得起了。"},
    {"t": "d", "s": "林彪悍", "text": "所以...铁路就更没人坐了。"},
    {"t": "d", "s": "岁月", "text": "是的。当沙能载具的价格降到和一张铁路月票差不多时...铁路就彻底失去了竞争力。"}
  ]
},
```

- [ ] **Step 3: 插入新闻7场景（朝日新闻 2066年 — 铁路客运量历史最低）**

紧接新闻6场景之后添加：

```json
{
  "bg": "china_sky",
  "bgm": "travel",
  "d": [
    {"t": "n", "text": "【继续飞行，又刷到一篇新闻】"},
    {"t": "n", "text": "【手机屏幕显示：朝日新闻 2066年8月12日】"},
    {"t": "n", "text": "铁路客运量历史最低："铁路时代终结"的警钟再次敲响"},
    {"t": "n", "text": "全球铁路客运量已降至历史最低水平。与2050年相比，铁路客运量下降了78%，货运量下降了65%。"},
    {"t": "d", "s": "林彪悍", "text": "78%...客运量下降了78%。货运也下降了65%。", "e": "sad"},
    {"t": "d", "s": "岁月", "text": "是的。2066年...是铁路最黑暗的一年。之后...每年都在继续下降。"},
    {"t": "d", "s": "林彪悍", "text": "直到...2072年。"},
    {"t": "d", "s": "岁月", "text": "是的。二十二年...从兴盛到死亡。只用了二十二年。"}
  ]
},
```

- [ ] **Step 4: 插入新闻8场景（华尔街日报 2068年 — 国有铁路解体）**

紧接新闻7场景之后添加：

```json
{
  "bg": "china_sky",
  "bgm": "travel",
  "d": [
    {"t": "n", "text": "【继续飞行，又刷到一篇新闻】"},
    {"t": "n", "text": "【手机屏幕显示：华尔街日报 2068年2月28日】"},
    {"t": "n", "text": "国有铁路系统全球性解体：私有化能否拯救铁路？"},
    {"t": "n", "text": "过去两年中，已有超过30个国家宣布将国有铁路系统解体，将线路承包给私人企业或直接废弃。"},
    {"t": "d", "s": "林彪悍", "text": "30个国家...解体国有铁路系统。私有化...不是拯救，是加速死亡。", "e": "sad"},
    {"t": "d", "s": "岁月", "text": "是的。私有化之后...只保留了盈利的线路。"},
    {"t": "d", "s": "岁月", "text": "像雾峰村这样的偏远支线...几乎都被废弃了。"},
    {"t": "d", "s": "林彪悍", "text": "几乎？"},
    {"t": "d", "s": "岁月", "text": "是的。雾峰村的铁路...是少数被保留下来的。因为...有人在守护。"},
    {"t": "d", "s": "林彪悍", "text": "但我还是来了。", "e": "determined"},
    {"t": "d", "s": "岁月", "text": "是的。您还是来了。...这很特别。"}
  ]
},
```

- [ ] **Step 5: 验证JSON格式正确性**

检查插入后的JSON结构：
1. 所有场景节点之间用逗号分隔
2. `scenes` 数组的最后一个场景没有尾随逗号
3. 整体JSON可通过 `JsonUtility.FromJson<ScriptData>` 解析

- [ ] **Step 6: Commit**

```bash
git add "Assets/Resources/Scripts/prologue_03_journey.json"
git commit -m "feat(vn): add Day 3 news scenes (6/7/8) to prologue script"
```

---

### Task 2: 插入 Day 4 上午新闻场景（新闻9）

**Covers:** 补全世界观铺垫 — 铁路事故频发，大废线末期安全危机

**Files:**
- Modify: `Assets/Resources/Scripts/prologue_03_journey.json` (在 Day 3 傍晚补给场景之后、Day 4 到达场景之前插入)

**Interfaces:**
- Consumes: Task 1 完成后的JSON结构
- Produces: 1个新场景节点插入到 `scenes` 数组中

- [ ] **Step 1: 在 prologue_03_journey.json 中定位插入点**

找到 Day 3 傍晚补给后的情感对话场景（bg: "car_interior", 以 "有温度的人...通常会成功。" 和 "岁月...谢谢你。" 结尾）和 Day 4 到达场景（bg: "wufeng_village", 以 "林彪悍...醒醒。快到了。" 开头）之间。

- [ ] **Step 2: 插入新闻9场景（泰晤士报 2070年 — 铁路事故频发）**

在插入点添加以下场景节点：

```json
{
  "bg": "car_interior",
  "bgm": "melancholy",
  "d": [
    {"t": "n", "text": "【Day 4 上午】"},
    {"t": "n", "text": "【沙子飞猪号内部】"},
    {"t": "n", "text": "【手机屏幕显示：泰晤士报 2070年11月5日】"},
    {"t": "n", "text": "铁路事故频发：大废线末期的安全危机"},
    {"t": "n", "text": "过去一年中，全球已发生超过850起铁路事故，造成近千人伤亡。"},
    {"t": "n", "text": "专家指出，事故主要原因是铁路系统维护不到位、设备老化、人员不足。"},
    {"t": "d", "s": "林彪悍", "text": "850起事故...近千人伤亡。维护不足...设备老化...人员不足...", "e": "sad"},
    {"t": "d", "s": "岁月", "text": "是的。2070年...是铁路最危险的一年。很多线路...因为事故被强制停运。"},
    {"t": "d", "s": "岁月", "text": "雾峰村的铁路...也差点被停运。"},
    {"t": "d", "s": "林彪悍", "text": "差点？"},
    {"t": "d", "s": "岁月", "text": "是的。您爷爷...当时还在。他...用个人名义担保了线路的安全。才...没有被停运。"},
    {"t": "d", "s": "林彪悍", "text": "...", "e": "sad"},
    {"t": "d", "s": "岁月", "text": "您...在想什么？"},
    {"t": "d", "s": "林彪悍", "text": "我在想...爷爷当时...一定很害怕。害怕线路被停运。害怕...他守了一辈子的东西消失。"},
    {"t": "d", "s": "岁月", "text": "是的。他...一定很害怕。但他...没有放弃。"},
    {"t": "d", "s": "林彪悍", "text": "所以...我也不能放弃。", "e": "determined"},
    {"t": "d", "s": "岁月", "text": "是的。您...不会放弃的。"}
  ]
},
```

- [ ] **Step 3: 验证JSON格式正确性**

检查插入后的JSON结构：
1. 所有场景节点之间用逗号分隔
2. `scenes` 数组的最后一个场景没有尾随逗号
3. 整体JSON可通过 `JsonUtility.FromJson<ScriptData>` 解析

- [ ] **Step 4: 在 Unity Editor 中验证加载**

打开 Unity Editor，进入 VN_Test 场景，运行游戏。确认：
1. 序章从头播放时，Day 3 白天的3条新闻正常显示
2. Day 4 上午的新闻9正常显示
3. 所有场景转场流畅，无卡顿
4. 打字机效果正常工作

- [ ] **Step 5: Commit**

```bash
git add "Assets/Resources/Scripts/prologue_03_journey.json"
git commit -m "feat(vn): add Day 4 morning news scene (9) to prologue script"
```

---

## 序章完整场景时间线（补全后）

| 序号 | 场景 | JSON文件 | bg |
|------|------|---------|-----|
| 1 | 开场新闻1+2 | prologue_01_news.json | black |
| 2 | 旁白 | prologue_01_news.json | black |
| 3 | 破败车站 | prologue_01_news.json | abandoned_station |
| 4 | Day 0 实验室 | prologue_02_day0.json | lab |
| 5 | Day 0 电话 | prologue_02_day0.json | lab |
| 6 | Day 0 老陈诉说 | prologue_02_day0.json | lab |
| 7 | Day 0 决心 | prologue_02_day0.json | lab |
| 8 | Day 0 导师办公室 | prologue_02_day0.json | professor_office |
| 9 | Day 0 导师对话 | prologue_02_day0.json | professor_office |
| 10 | Day 0 导师批准 | prologue_02_day0.json | professor_office |
| 11 | Day 0 停机坪 | prologue_02_day0.json | hangar |
| 12 | Day 0 岁月介绍 | prologue_02_day0.json | hangar |
| 13 | Day 0 岁月启动 | prologue_02_day0.json | hangar |
| 14 | Day 0 岁月初见 | prologue_02_day0.json | car_interior |
| 15 | Day 0 出发 | prologue_02_day0.json | car_interior |
| 16 | Day 0 车速对话 | prologue_02_day0.json | car_interior |
| 17 | Day 0 岁月独白 | prologue_02_day0.json | car_interior |
| 18 | Day 0 夜晚 | prologue_02_day0.json | car_interior_night |
| 19 | Day 0 入睡 | prologue_02_day0.json | car_interior_night |
| 20 | Day 1 边境补给 | prologue_03_journey.json | border_town |
| 21 | Day 1 补给站 | prologue_03_journey.json | supply_station |
| 22 | Day 1 起飞+新闻3 | prologue_03_journey.json | china_sky |
| 23 | Day 1 新闻3详情 | prologue_03_journey.json | china_sky |
| 24 | Day 1 新闻5 | prologue_03_journey.json | china_sky |
| 25 | Day 2 河北补给 | prologue_03_journey.json | hebei_town |
| 26 | Day 2 深度对话1 | prologue_03_journey.json | car_interior |
| 27 | Day 2 深度对话2 | prologue_03_journey.json | car_interior |
| 28 | Day 2 深度对话3 | prologue_03_journey.json | car_interior |
| 29 | Day 2 夜晚 新闻4 | prologue_03_journey.json | car_interior_night |
| 30 | Day 2 新闻4讨论 | prologue_03_journey.json | car_interior_night |
| **31** | **Day 3 白天 新闻6** | **prologue_03_journey.json** | **china_sky** |
| **32** | **Day 3 白天 新闻7** | **prologue_03_journey.json** | **china_sky** |
| **33** | **Day 3 白天 新闻8** | **prologue_03_journey.json** | **china_sky** |
| 34 | Day 3 傍晚补给 | prologue_03_journey.json | henan_town |
| 35 | Day 3 接近目的地 | prologue_03_journey.json | car_interior |
| **36** | **Day 4 上午 新闻9** | **prologue_03_journey.json** | **car_interior** |
| 37 | Day 4 到达 | prologue_03_journey.json | wufeng_village |
| 38 | Day 4 新闻10 | prologue_03_journey.json | wufeng_village |
| 39 | Day 4 降落 | prologue_03_journey.json | wufeng_village |
| 40 | Day 4 重逢 | prologue_03_journey.json | wufeng_village |
| 41 | Day 4 巡视1 | prologue_03_journey.json | wufeng_village |
| 42 | Day 4 巡视2 | prologue_03_journey.json | wufeng_village |
| 43 | Day 4 巡视3 | prologue_03_journey.json | wufeng_village |
| 44 | Day 4 员工集合 | prologue_03_journey.json | wufeng_station |
| 45 | Day 4 员工对话 | prologue_03_journey.json | wufeng_station |
| 46 | Day 4 林彪悍讲话 | prologue_03_journey.json | wufeng_station |
| 47 | Day 4 结尾 | prologue_03_journey.json | wufeng_station |
