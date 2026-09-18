# Wiki 内容补充计划 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为桥洞 Wiki（`中转站/wiki/`）创建 60 个新页面（27 全新条目 × 中英双版 = 54 页 + 6 个英文补建页），并修复全部死链、链接名不匹配、索引漏录与图片问题。

**Architecture:** 纯 Markdown 内容创作任务，无代码。按 spec S6.1 分为 4 批并行（角色/城市+势力/系统+世界+载具+剧情/英文补建），每批由子代理执行，主代理收尾做全站死链扫描验证。所有内容以参考资料原文为唯一来源，不编造设定。

**Tech Stack:** Markdown（GitHub Wiki 语法：HTML `<img>` 控制尺寸，`![]()` 图片，wikitable 信息框，`[text](PageName)` 链接）

## Global Constraints

- Wiki 目录：`D:\Unity Project\RailwayRenaissance\中转站\wiki\`
- 数据来源（唯一，禁止编造）：`参考资料\` 目录 + `docs\compose\specs\` 系统设计 + `Assets\Resources\Seeds\seed_*.json` 城市数据
- 页面命名：中文页用中文文件名（如 `王小弟.md`），英文页用 ASCII 文件名（如 `Wang-Xiaodi.md`）
- 图片语法：仅用 `<img src="images/xxx.png" width="300" style="float:right">`，禁用 `{:width}` 与 `[[File:...]]`
- 英文页底部回链 `[Back to Home](Home)`，**禁止** `Home-EN`
- 中文页底部回链 `[返回…](中文维基)`（使用链接文案对应分类：角色图鉴/城市图鉴/势力图鉴/世界百科等）
- 立绘素材：仅老陈/林彪悍有立绘 PNG（images/ 下），其他角色不配图、不编造素材
- 每批完成后：子代理向主代理回报本批页面清单供验收
- 现有 25 页除 S5 规定的修改外不得改动

---

### Task 1: 批1 —— 角色 4 条目 × 中英 = 8 页

**Covers:** [S3.1, S4]

**Files:**
- Create: `中转站/wiki/王小弟.md`, `中转站/wiki/Wang-Xiaodi.md`, `中转站/wiki/赵铁山.md`, `中转站/wiki/Zhao-Tieshan.md`, `中转站/wiki/陈鹤年.md`, `中转站/wiki/Chen-Henian.md`, `中转站/wiki/林悍.md`, `中转站/wiki/Lin-Han.md`

**Interfaces:**
- Consumes: `参考资料/角色设定.md`（王小弟 §3.4/§三、林悍 §六）、`参考资料/世界观扩展设定.md`（陈鹤年 §1.5.6、林悍 §4.5、赵铁山 §5.3）
- Produces: 8 个角色页；中文页底部 `*[返回角色图鉴](中文维基)*`，英文页底部 `*[Back to Home](Home)*`

- [ ] **Step 1: 读取资料来源**

Read: `参考资料/角色设定.md`（§三 王小弟/§六 林悍）、`参考资料/世界观扩展设定.md`（§1.5.6 陈鹤年/§4.5 林悍/§5.3 赵铁山）
Expected: 提取各角色属性表（年龄/身份/性格/技能/剧情）

- [ ] **Step 2: 创建中文角色页（4 页）**

每个页面结构：wikitable 信息框（无立绘 → 文字信息框：角色类型/年龄/身份/性格/技能核心/登场）→ `# 角色名` → 简介 → 概要/技能表（游戏内）→ 关系表 → 剧情档案 → 底部回链。

内容要点（来自参考资料，不编造）：
- **王小弟**：22 岁，刚毕业大学生，性格阳光热血；技能：驾驶 1级(潜力5) / 维修 0(4) / 管理 0(3) / 服务 1(3)；岗位：未来司机；来自角色设定 §3.4
- **赵铁山**：铁路监督员（《铁路委托运营条例》执行者之一，与陈鹤年是条例的实际执行者与监督者）；无技能表，写监督职责
- **陈鹤年**：雾峰市长；派系立场"压力/理解"双角色；与周鼎铭有商业往来，宴会避开铁路话题的细节（世界观扩展 §1.5.6）；政治倾向影响补贴评估（§5.3）
- **林悍（爷爷）**：雾峰村末代站长；2070 年以个人名义担保线路；2071-09-13 沙能飞行器事故遇难；遗愿：守护雾峰村铁路线；两次拒绝铁龙基金会（2073/2074）；来源：角色设定 §六 + 世界观扩展 §4.5 + 铁龙计划 §2.7

- [ ] **Step 3: 创建英文角色页（4 页）**

Structure: infobox (Role/Age/Identity/Personality/Appears) → `# Name` → Profile → In-Game → Relations(if any) → `*[Back to Home](Home)*`。内容与中文页对应翻译。

- [ ] **Step 4: 死链验证本批**

Run: `git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" status --short`
Expected: 仅 8 个新文件 untracked，无其他改动。
Run: 全站死链扫描脚本（见 Task 7 Step 1 的脚本），确认 8 个新页名不再出现在死链列表
Expected: `王小弟/Wang-Xiaodi/赵铁山/Zhao-Tieshan/陈鹤年/Chen-Henian/林悍/Lin-Han` 从死链中消失

- [ ] **Step 5: Commit**

```bash
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" add 王小弟.md Wang-Xiaodi.md 赵铁山.md Zhao-Tieshan.md 陈鹤年.md Chen-Henian.md 林悍.md Lin-Han.md
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" commit -m "Wiki: add 4 characters (Wang Xiaodi/Zhao Tieshan/Chen Henian/Lin Han) EN+CN"
```

---

### Task 2: 批2 —— 城市 5 + 势力 2 条目 × 中英 = 14 页

**Covers:** [S3.2, S3.3, S4]

**Files:**
- Create（城市）: `中转站/wiki/青溪镇.md`, `青溪镇` 英文 `Clear-Stream-Town.md`, `云渡港.md`/`Cloud-Ferry-Port.md`, `白鹭洲.md`/`White-Egret-Islet.md`, `枫林渡.md`/`Maple-Forest-Crossing.md`, `望海港.md`/`Sea-View-Harbor.md`
- Create（势力）: `白头山动力总会社.md`/`Baekdu-Mountain-Power.md`, `国际铁路遗产保护基金会.md`/`Railway-Heritage-Foundation.md`

**Interfaces:**
- Consumes: `Assets/Resources/Seeds/seed_*.json`（5 城数据：类型/人口/距离/区域/沙能渗透/政治倾向）、`参考资料/世界观扩展设定.md` §1.2（白头山）/§2（基金会）、已有 `雾峰村.md`/`乌金岭.md` 的城市页格式（双语数据表）
- Produces: 14 个页面；城市中文页底部 `*[返回城市图鉴](中文维基)*`，势力中文页 `*[返回势力图鉴](中文维基)*`，英文页 `*[Back to Home](Home)*`

- [ ] **Step 1: 提取 5 城数据**

Run: `Get-Content "D:\Unity Project\RailwayRenaissance\Assets\Resources\Seeds\seed_*.json" -Raw` 并解析城市对象
Expected: 每条城市含 type/population/distance/region/sandPenetration/politicalLean 字段。若 JSON 字段名不同，以实际 JSON 为准。

- [ ] **Step 2: 创建中文城市页（5 页）**

结构沿用已有 `雾峰村.md` 双语格式：`# 城名 / English Name` → 数据表（类型/人口/距离/区域/沙能渗透/政治倾向）→ 地理 → 产业 → 剧情/游戏中的作用 → 底部回链。城市原始数据只来自 seed JSON，不得虚构。

- [ ] **Step 3: 创建英文城市页（5 页）**

Same data, English only: `# City Name` → Data table → Geography → Industry → Story → `*[Back to Home](Home)*`

- [ ] **Step 4: 创建中文势力页（2 页）**

- **白头山动力总会社**：朝方股东/合资方（登记49%）；实控 USET 的三重协议（管理/技术/沙子）；下设先军国防公社/沙能科学研究院/沙能保障总局；"一号会社"体系（世界观扩展 §1.2）
- **国际铁路遗产保护基金会**：铁龙计划的公开壳；非营利组织脸面；行动：铁路抢救小组/遗产保护巡回展/志愿者招募计划；伪造遗产档案伏笔（§2.4/§2.9）

- [ ] **Step 5: 创建英文势力页（2 页）**

同上英文版，回链 `*[Back to Home](Home)*`

- [ ] **Step 6: 死链验证 + Commit**

Run: 死链扫描（Task 7 脚本），确认 10 个新页名消失
Run: 两个 commit（城市 10 页一组、势力 4 页一组）：
```bash
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" add 青溪镇.md Clear-Stream-Town.md 云渡港.md Cloud-Ferry-Port.md 白鹭洲.md White-Egret-Islet.md 枫林渡.md Maple-Forest-Crossing.md 望海港.md Sea-View-Harbor.md
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" commit -m "Wiki: add 5 cities (Qingxi/Yundu/Bailuzhou/Fengling/Wanghai) EN+CN"
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" add 白头山动力总会社.md Baekdu-Mountain-Power.md 国际铁路遗产保护基金会.md Railway-Heritage-Foundation.md
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" commit -m "Wiki: add 2 factions (Baekdu Power Corp / Railway Heritage Foundation) EN+CN"
```

---

### Task 3: 批3a —— 系统 5 条目 × 中英 = 10 页

**Covers:** [S3.6, S4]

**Files:**
- Create: `沙本位经济核.md`/`Sand-Standard-Economy.md`, `千里马创世核.md`/`Chollima-Genesis-Core.md`, `岁月叙事引擎.md`/`Suiyue-Narrative-Engine.md`, `先民人事系统.md`/`Seonmin-Personnel-System.md`, `铁龙竞争系统.md`/`Iron-Dragon-Competition.md`

**Interfaces:**
- Consumes: `参考资料/沙本位经济核.md`、`docs/compose/specs/千里马创世核.md`、`docs/compose/specs/岁月叙事引擎.md`、`docs/compose/specs/先民人事系统.md`、`docs/compose/specs/铁龙竞争系统.md`
- Produces: 10 个速览式系统页（定位/核心机制/主要数值/联动/详见）

- [ ] **Step 1: 为每个系统读取速览素材**

Read（并行）：5 个 spec 的关键章节（定位/核心机制/数值/联动表）
Expected: 每系统提取 1 段定位 + 机制表 3-6 行 + 3-8 个关键数值 + 联动清单

- [ ] **Step 2: 创建 5 个中文系统页（速览式）**

每页结构（S4 模板）：
```
# 系统名 / English Name

**定位**：1 段（在游戏中作用）

## 核心机制（速览）
| 机制 | 效果 | 数值节点 |

## 主要数值
- 3-8 个关键数值/阈值（bullet）

## 联动
- 与哪些系统交互（bullet）

## 详见
- [原始设计文档](../../docs/compose/specs/xxx.md)
```
底部 `*[返回系统设定](中文维基)*`

- [ ] **Step 3: 创建 5 个英文系统页**

内容与中文对应，底部 `*[Back to Home](Home)*`

- [ ] **Step 4: 死链验证 + Commit（10 页一次提交）**

Run: 死链扫描确认 10 个新页名消失
Expected: `沙本位经济核/Chollima-Genesis-Core` 等全部移除
```bash
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" add 沙本位经济核.md Sand-Standard-Economy.md 千里马创世核.md Chollima-Genesis-Core.md 岁月叙事引擎.md Suiyue-Narrative-Engine.md 先民人事系统.md Seonmin-Personnel-System.md 铁龙竞争系统.md Iron-Dragon-Competition.md
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" commit -m "Wiki: add 5 system overview pages (Economy/Genesis/Narrative/Personnel/Iron Dragon) EN+CN"
```

---

### Task 4: 批3b —— 世界 4 + 载具 3 条目 × 中英 = 14 页

**Covers:** [S3.4, S3.5, S4]

**Files:**
- Create（世界）: `沙本位制.md`/`Sand-Standard-Currency.md`, `铁路大废线.md`/`Great-Railway-Abandonment.md`, `铁路委托运营条例.md`/`Railway-Delegation-Act.md`, `五条趋势线.md`/`Five-Trendlines.md`
- Create（载具）: `沙子飞猪号.md`/`Sand-Flying-Pig-0721.md`, `NF-5耕牛.md`/`NF-5-Gengniu.md`, `沙驴号.md`/`Sand-Donkey.md`

**Interfaces:**
- Consumes: `参考资料/世界观扩展设定.md` §3（沙本位制）/§5（委托运营条例）、`参考资料/世界观与车辆设定.md` §一（大废线）/§二/§三/§四（载具）、`docs/compose/specs/核心玩法循环.md`（五条趋势线）、`Assets/Resources/Seeds/seed_*.json`（城市渗透数据，趋势线相关）
- Produces: 14 页；世界页中文底 `*[返回世界百科](中文维基)*`，载具页 `*[返回载具图鉴](中文维基)*`

- [ ] **Step 1: 读取素材**

Read: 世界观扩展设定 §3/§5、世界观与车辆设定 §一/§二-四、核心玩法循环（趋势线部分）
Expected: 提炼 4 世界条目 + 3 载具条目的数据表

- [ ] **Step 2: 创建中文世界页（4 页）**

- **沙本位制**：1 沙币 = 10 公斤标准工业沙（国标SB-62）；发行方朝鲜人民银行；兑换关系 1500公斤=150沙币；大额万沙；金日成综合大学 §3.3 供应链（提炼权配额/沙之诺阴谋）
- **铁路大废线**：2068 年 91 国解体国有铁路系统 → 2072 年全球停运；法律背景（先解体重组、后立法放权）；国际对照表（朝鲜/日本/欧洲/英国）
- **铁路委托运营条例**（2073）：第一条/三/五/七/九条条款表；玩家身份法律基础；对应政治系统（补贴与运营评估挂钩）
- **五条趋势线**：从核心玩法循环 spec 提取五条趋势线定义与机制作用

- [ ] **Step 3: 创建中文载具页（3 页）**

- **沙子飞猪号**：第一代飞行汽车；30-40km/h；2人；续航1000km；补给1500kg=150沙币；0721 号特殊版（岁月AI/深蓝/校徽）
- **NF-5 耕牛**：原型东风4；2000kW；80km/h；320kN；初始状态70/100；SY-22灰雀客车 30人
- **沙驴号**：第二代通勤车；60km/h；4人；售价飞猪号1/5

- [ ] **Step 4: 创建英文页（7 页）**

英文版对应翻译，世界页底 `*[Back to Home](Home)*`，载具同

- [ ] **Step 5: 死链验证 + Commit**

Run: 死链扫描确认 14 个新页名消失
```bash
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" add 沙本位制.md Sand-Standard-Currency.md 铁路大废线.md Great-Railway-Abandonment.md 铁路委托运营条例.md Railway-Delegation-Act.md 五条趋势线.md Five-Trendlines.md
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" commit -m "Wiki: add 4 world lore pages (Sand Standard/Great Abandonment/Delegation Act/Five Trendlines) EN+CN"
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" add 沙子飞猪号.md Sand-Flying-Pig-0721.md NF-5耕牛.md NF-5-Gengniu.md 沙驴号.md Sand-Donkey.md
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" commit -m "Wiki: add 3 vehicles (Sand Flying Pig/NF-5 Gengniu/Sand Donkey) EN+CN"
```

---

### Task 5: 批3c —— 剧情 4 条目 × 中英 = 8 页

**Covers:** [S3.7, S4]

**Files:**
- Create: `序章-归乡.md`/`Prologue-Homecoming.md`, `大学篇.md`/`University-Chapters.md`, `旅途篇.md`/`Journey-Chapters.md`, `雾峰篇.md`/`Wufeng-Chapters.md`

**Interfaces:**
- Consumes: `参考资料/序章剧本_归乡.md`、`docs/compose/specs/序章后续剧情设计.md`、`docs/compose/specs/核心玩法循环.md`
- Produces: 8 页；中文底 `*[返回剧情档案](中文维基)*`，英文底 `*[Back to Home](Home)*`

- [ ] **Step 1: 读取剧本素材**

Read: 序章剧本_归乡.md（章节划分）、序章后续剧情设计.md（大学/旅途/雾峰三篇结构）
Expected: 提取各篇章的时间线/场景/关键事件

- [ ] **Step 2: 创建 4 个中文剧情页**

- **序章-归乡**：Day 0-4 完整流程（实验室→导师办公室→停机坪→旅途→雾峰村抵达），来自序章剧本
- **大学篇**：实验室电话→休学申请→领取0721，来自序章后续剧情设计
- **旅途篇**：三千里归途/九条新闻/三次补给/岁月初对话
- **雾峰篇**：抵达→巡视→团队集结→首班车

- [ ] **Step 3: 创建 4 个英文剧情页**

对应翻译。数据以设计文档/剧本为准，不新增剧情。

- [ ] **Step 4: 死链验证 + Commit**

```bash
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" add 序章-归乡.md Prologue-Homecoming.md 大学篇.md University-Chapters.md 旅途篇.md Journey-Chapters.md 雾峰篇.md Wufeng-Chapters.md
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" commit -m "Wiki: add 4 story arc pages (Prologue/University/Journey/Wufeng) EN+CN"
```

---

### Task 6: 批4 —— 英文补建 6 页 + 中文索引链接修正

**Covers:** [S3.8, S5.1, S5.3, S5.4]

**Files:**
- Create: `中转站/wiki/USET.md`, `Iron-Dragon-Project.md`, `Wufeng-Village.md`, `Black-Gold-Ridge.md`, `Sand-Energy-Technology.md`, `Timeline.md`
- Modify: `中转站/wiki/USET（联合沙能科技）.md`（仅读取,不改）、`中转站/wiki/中文维基.md`（S5.3 链接名 3 处 + S5.4 漏录 2 处）

**Interfaces:**
- Consumes: 6 个既有合并页的英文段（USET（联合沙能科技）.md/铁龙计划.md/雾峰村.md/乌金岭.md/沙能技术.md/时间线.md）
- Produces: 6 个纯英文页；Home.md 的 6 处死链（USET/Iron-Dragon-Project/Wufeng-Village/Black-Gold-Ridge/Sand-Energy-Technology/Timeline）被解析

- [ ] **Step 1: 读取 6 个合并页英文内容**

Read（并行）：USET（联合沙能科技）.md、铁龙计划.md、雾峰村.md、乌金岭.md、沙能技术.md、时间线.md
Expected: 提取各页英文段落

- [ ] **Step 2: 创建 6 个英文页**

结构：`# English Name` → 英文内容（源自合并页英文段，可补充参考资料细节，不编造）→ `*[Back to Home](Home)*`

- [ ] **Step 3: 修改中文维基.md 链接名（3 处）**

Edit `中文维基.md`：
- `[《铁路委托运营条例》](《铁路委托运营条例》)` → `[《铁路委托运营条例》](铁路委托运营条例)`
- `[NF-5 耕牛](NF-5-耕牛)` → `[NF-5 耕牛](NF-5耕牛)`
- `[林悍（爷爷）](林悍（爷爷）)` → `[林悍（爷爷）](林悍)`

- [ ] **Step 4: 修改中文维基.md 索引漏录（2 处）**

Edit `中文维基.md`：
- 角色图鉴「雾峰村站务团队」区域追加：`- **[王小弟](王小弟)** — 刚毕业大学生 · 未来司机`
- 世界百科区域追加：`- [金日成综合大学](金日成综合大学) — 全球第一学府，RDA 调度算法发源地`

- [ ] **Step 5: 死链验证 + Commit**

Run: 死链扫描，确认 USET/Iron-Dragon-Project/Wufeng-Village/Black-Gold-Ridge/Sand-Energy-Technology/Timeline 移除；`《铁路委托运营条例》`/`NF-5-耕牛`/`林悍（爷爷）` 不再出现
```bash
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" add USET.md Iron-Dragon-Project.md Wufeng-Village.md Black-Gold-Ridge.md Sand-Energy-Technology.md Timeline.md 中文维基.md
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" commit -m "Wiki: add 6 English-only pages (USET/Iron Dragon/Wufeng/Black Gold/Sand Tech/Timeline) + fix CN index links"
```

---

### Task 7: 收尾 —— 链接修复、图片清理、全站验证

**Covers:** [S5.2, S5.5, S7]

**Files:**
- Modify: 8 个英文角色页（回链修复）、`雾峰村.md`（图片引用）、`images/`（删除 2 副本）
- Delete: `中转站/wiki/images/chen_sheet.png`, `中转站/wiki/images/lin_card.png`（**移入回收站，禁止永久删除**，按 CLAUDE.md 规则）

**Interfaces:**
- Consumes: Task 6 完成的 60 页、S5 修复清单
- Produces: 满足 S7 全部完成标准的最终 Wiki 状态

- [ ] **Step 1: 修复 8 个英文角色页回链**

Edit（×8）：`Lin-Biaohan.md/Old-Chen.md/Suiyue.md/Zhou-Dingming.md/Zhang-Gong.md/Li-Ayi.md/Zhao-Shifu.md/Xiao-Fang.md` 末尾：`*[Back to Character Index](Home-EN)*` → `*[Back to Home](Home)*`

- [ ] **Step 2: 修复雾峰村.md 图片引用**

Edit `雾峰村.md`：`src="images/station.png"` → `src="images/lab.jpg"`

- [ ] **Step 3: 删除重复大图（进回收站）**

Run: `Move-Item "中转站\wiki\images\chen_sheet.png" "$env:USERPROFILE\...` （将文件移入系统回收站。PowerShell 无原生回收站命令，使用 Shell.Application COM 或确认后 `Remove-Item` —— 按 CLAUDE.md 项目规则：删除必须走回收站。若脚本受限，移动到一个 `_trash` 临时夹并报告）

- [ ] **Step 4: 全站死链扫描（最终验证）**

Run（PowerShell 扫描脚本，与检查阶段相同）：
```powershell
$wikiDir = "D:\Unity Project\RailwayRenaissance\中转站\wiki"
$existing = Get-ChildItem $wikiDir -Filter *.md | ForEach-Object { $_.BaseName }
$broken = @()
Get-ChildItem $wikiDir -Filter *.md | ForEach-Object {
    $content = Get-Content $_.FullName -Raw -Encoding UTF8
    $matches = [regex]::Matches($content, '\[[^\]]*\]\(([^)]+)\)')
    foreach ($m in $matches) {
        $target = $m.Groups[1].Value
        if ($target -match '^(https?://|#|mailto:)') { continue }
        $target = $target -replace '#.*$',''
        $base = ($target.Trim() -split '\.')[0]
        if ($base -eq '') { continue }
        if (-not ($existing -contains $base)) { $broken += "$($_.Name) -> [$target]" }
    }
}
$broken | Sort-Object -Unique
```
Expected: **空输出**（无死链）

- [ ] **Step 5: 图片重复校验**

Run: 对比 images/ 下所有 PNG 字节数，`Group-Object Length | Where Count -gt 1`
Expected: 无重复（之前 chen_portrait/chen_sheet、lin_card/lin_portrait 两对中副本已删）

- [ ] **Step 6: 索引完整性核对**

Grep `中文维基.md` 与 `Home.md` 的所有 `](xxx)` 目标，对照 wiki 目录实际文件名
Expected: 全部可解析；王小弟/金日成综合大学在中文索引中

- [ ] **Step 7: Commit**

```bash
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" add -A
git -C "D:\Unity Project\RailwayRenaissance\中转站\wiki" commit -m "Wiki: fix EN backlinks, station image ref, remove duplicate sheets"
```

- [ ] **Step 8: 回写 spec 完成状态**

Edit `docs/compose/specs/Wiki内容补充计划.md`：状态改为 `已完成`，追加验收结果摘要（页面数/死链数/图片数）

---

## Self-Review 记录

**Spec coverage:**
- S1 问题 → Task 1-7 覆盖（每 Task 的 Covers）
- S2 解决概览 → 60 页分配：Task1(8)+Task2(14)+Task3(10)+Task4(14)+Task5(8)+Task6(6)=60 ✓
- S3.1-S3.8 → Task 1-6 ✓
- S4 模板 → 每 Task 内容结构说明 ✓
- S5.1-S5.5 → Task 6（S5.1/S5.3/S5.4）+ Task 7（S5.2/S5.5）✓
- S6 执行方式 → 批次对应 Task 1-6，收尾 Task 7 ✓
- S7 完成标准 → Task 7 Step 4-6 验证 ✓

**Placeholder scan:** 无 TBD/TODO；每步含具体文件/命令/预期输出 ✓

**Type consistency:** 页面命名与 spec S3 表完全一致（含 NF-5耕牛/铁路委托运营条例/林悍 的链接名修正）✓

---

*本计划为 Wiki 内容补充 spec v1.1 的执行方案。*