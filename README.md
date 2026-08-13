# Railway Renaissance: Sand Energy Impact 🚂

> ***English** | [简体中文](README.zh-CN.md)*

**A hardcore railway revival sim meets visual novel — where the world abandoned trains for flying sand-cars, and you're the last one keeping the rails alive.**

Unity 6 · 2D pixel art · Simulation / Management / Visual Novel

---

## 🖼️ Opening Scene: April 18, 2076

![Laboratory Interior](Assets/Resources/bg/lab.jpg)

*Kim Il Sung University, Pyongyang — Laboratory of Intelligent Dispatch Systems, April 18, 2076*

The year the world gave up on railways.

Kim Il Sung University — ranked **first in the Juche University Evaluation System (JUES) for 23 straight years**, and simultaneously **#1 in the QS World University Rankings** — is the only institution on Earth leading two fundamentally different evaluation systems at once. Its Laboratory of Intelligent Dispatch Systems is a National Key Laboratory under the dual purview of the State Academy of Sciences and the Ministry of Railways.

The lab's research direction — dynamic path optimization on real-time passenger flow — has led the field globally since the 2060s. Its computational backbone is a simulation cluster originally built for the national railway dispatch network, repurposed for academia after the 2072 rail realignment.

Our protagonist, Lin Biaohan, is an honor graduate fellow whose work on the **Ri Dispatch Algorithm (RDA)** — adaptive scheduling under variable demand — has been cited by the International Institute of Transport Economics. The lab sits in the east wing of the Ri Sung-gi Memorial Research Complex, windows facing the Taedong River.

On the desk, beside the terminal, a worn railway engineering handbook. The flyleaf reads: *"For Biaohan — Grandpa."*

> **Three days later, he would abandon this room to revive a dead line in a mountain village called Wufeng — roughly 2,500 km and one impossible bet away.**

---

## 🎮 The Game

### The Hook

**2076.** Sand-powered flying vehicles killed the railways — 78% of passenger volume gone, 30 nations dissolved their state rail systems, and by 2072 the global network was effectively dead.

But sand-cars have six fatal flaws: no heavy freight, dies in storms, short range, weak at night, sand-hungry, and dependent on an air-traffic grid. The railways' six strengths are the mirror image.

You inherit grandpa's abandoned line in Wufeng — a misty tea village in China's central hills. **Pure operations lose money from day one. The story is your life-support.** Survive, rebuild, and decide what the railway becomes: a lifeline, a monument, or a weapon against the Sand-Tech monopoly.

### World

Sand energy was invented in Britain in 2050. The DPRK — holding the planet's largest sand reserves — bought the technology in 2051 and industrialized it. By 2076 the DPRK is the world's sole superpower: sand exports, a sand tank called "Kim Jong-un Great Sand Type," and a globe-spanning monopoly, the **United Sand Energy Technology (USET)** — formally the *United Sand Energy Technology Society*, publicly a joint venture, privately a state enterprise; internally known as the "first company."

Somewhere beneath it all, the railways wait.

---

## ⚙️ Core Systems (the hardcore part)

### Five Interlocking Trendlines

The world is driven by **five slowly-shifting trendlines**, hidden from the player until they cross their thresholds:

| Trendline | Meaning | What pushes it |
|-----------|---------|----------------|
| **公 Trust** | Public faith in your railway | Accidents tank it; punctual service restores it |
| **公 Fiscal** | Your financial health | Losses accumulate; story grants bail you out |
| **公 Sand Penetration** | USET's market share in each city | Ads and price wars raise it; your PR lowers it |
| **公 Political Pressure** | Government scrutiny | Accidents raise it; public goodwill lowers it |
| **公 Infrastructure Decay** | Wear on your line | Time raises it; maintenance lowers it |

Each one is a slow current — until it crosses a threshold, and **the world changes irreversibly**.

### Three-Layer Time Structure

```
Layer 1 — Continuous world   Trains run, platforms fill, seasons pass. You can watch, or not.
Layer 2 — Hidden settlement   Every game-day: revenue, fuel, wear, fatigue, all 5 trendlines, thresholds.
Layer 3 — Decision windows    Rare, heavy: a news break, a policy shift, a bet you can't take back.
```

You don't click your way through. You *oversee*.

### Four Long-Term Strategies

Every line lives by a stance — switching costs 30 days and real money:

1. **Line posture** — 民生 Line (stable, watched by politics) / Commercial / Tourist / Marginal
2. **Maintenance policy** — Belt-tightening / Standard / Over-maintained
3. **Crew policy** — Train, outsource, or squeeze
4. **Stance toward USET** — Resist / Interconnect / Collaborate

### Irreversible Consequences

| Trigger | Consequence | How bad? |
|---------|-------------|----------|
| Trust < 30 for 30 days | **Generational rift**: new residents simply never ride | Nearly permanent |
| Sand penetration > 55% | USET takes your key line | Requires law/politics to undo |
| Political pressure > 70% | Government takes over operations | Needs trust rebuilt |
| Infrastructure decay > 80% | Line scrapes into scrap; rebuild costs 10× | Ruinously expensive |
| One missed conversation | A grant, an ally, a lever — gone | Never returns |

*The game remembers what you skip.*

### The Numbers (no hand-waving)

Every subsystem is a real formula, cross-linked in [the formula spec](docs/compose/specs/跨系统联动公式.md):

```
Daily passengers = population × 0.001 × (0.5 + trust × 0.005)
                   × season × (1 − sandPenetration) × (1 + conductorLvl × 0.03)

Daily fuel cost   = 58.5 L/100km × 92 km ÷ 100 × (1 + (100 − condition)/200) × 15 sand/L

Accident prob.    = 0.5% × ageFactor × driverSkillFactor × maintenanceFactor × weatherFactor

Trust delta       = +0.014/day (smooth ops) − 0.08 × severity (accident) − 0.03 (sand ads)
```

**Worked example — Wufeng line, month one (normal difficulty):**

| Item | Value |
|------|-------|
| Start capital | 40,000 sand |
| Passenger revenue | 12,600 sand/mo (~20% occupancy) |
| Freight + misc | 4,500 sand/mo |
| Salaries (5 crew) | 90,000 sand/mo ← **68% of all costs** |
| Fuel + maintenance | ~39,000 sand/mo |
| **Pure operating loss** | **~−107,000 sand/mo** |

You cannot out-earn that. You can only out-story it. Prologue grants inject ~63,000 sand; monthly story events add 5,000–15,000. Breakeven is a **5-car train at 75% occupancy** — roughly month twelve. Until then, every sand is a story told well or a railway that quietly dies.

---

## 🎭 Characters (5 crew, 4 skills each)

Retired railway people too stubborn to quit, a village too proud to forget:

| Crew | Age | Core skill | Why it matters |
|------|-----|-----------|----------------|
| **老陈** — last stationmaster | 68 | Driving (Lv5) | The mentor. Zero accident record. |
| **张工** — retired mechanical engineer | 62 | Repair (Lv5) | Keeps the locomotive alive. |
| **李阿姨** — village heart | 55 | Service (Lv2) | Passenger satisfaction multiplier. |
| **赵师傅** — retired railway engineer | 55 | Management (Lv4) | Keeps the schedule on time. |
| **小芳** — volunteer | 45 | Service (Lv1) | Raw potential, needs training. |

Crew aren't stat blocks: fatigue accumulates, skills grow slowly, and who you put on the seat changes your accident math and your bottom line. Your 王小弟 — the local kid fresh out of university — has the highest ceiling in driving, if you dare to let him near the throttle.

---

## 🗺️ Progression (the long game)

```
序章 Visual Novel (Day 0–4)  →  Survival (months 1–2)  →  Stability (3–4)
→ Growth (5–8)  →  Breakthrough (9–12, near-breakeven)  →  Development (12+)
→ unlock new lines, cities, and eventually the national network
```

- **3-branch tech tree** — Industrial / Ecological / Automation, mutually exclusive. Pick carefully; the road not taken locks forever.
- **Political cycles** — local government swings between Authoritarian / Market / Welfare, each changing your subsidy math and your room to maneuver.
- **10+ random events** — oil spikes, storms, staff illness, holiday crowds, USET ad blitzes.

---

## 🚀 Getting Started

### Prerequisites

- Unity 6000.4.6f1 (Unity 6)
- Git

### Installation

```bash
git clone https://github.com/NMDX721/RailwayRenaissance.git
```

Open in Unity Hub → **Add project from disk** → select the folder → Open with Unity 6000.4.6f1.

### Scenes

| Scene | What it is |
|-------|-----------|
| `Scenes/Login.unity` | Login screen |
| `Scenes/TitleScreen.unity` | Title screen (video background) |
| `Scenes/VN_Test.unity` | Visual Novel prologue (Day 0–4) |
| `Scenes/StationSlice_V1.unity` | Station management gameplay |

---

## 📁 Project Structure

```
Assets/
├── Scripts/           # C# source
│   ├── VN/            # Visual Novel engine (JSON-driven)
│   ├── GameData.cs    # Economy simulation (all formulas)
│   ├── CrewManager.cs # Crew, skills, fatigue
│   ├── EventManager.cs# Random events + events.json
│   ├── SandRivalManager.cs  # USET AI (penetration, campaigns)
│   ├── TutorialManager.cs   # Progressive onboarding
│   └── ...
├── Resources/
│   ├── Scripts/       # VN scripts (prologue_01 ~ prologue_10)
│   ├── events.json    # Event templates
│   ├── bg/            # Backgrounds (1920×1080)
│   └── characters/    # Character sprites
├── Scenes/            # Unity scenes
└── Documentation/     # Design docs
```

---

## 🛠 Tech Stack

| Component | Technology |
|-----------|-----------|
| Engine | Unity 6000.4.6f1 (Unity 6) |
| UI | UI Toolkit (VN/Title) + uGUI (Login) |
| VN System | JSON-driven, fully data-driven dialogue |
| Save System | PlayerPrefs + JSON serialization |
| AI (future) | Template-based (phase 1) → llama.cpp (phase 2) |
| Platforms | Windows (primary) · Android (planned) |

### Design Documents

| Document | Description |
|----------|-------------|
| [Game Design Doc](参考资料/游戏开发文档.md) | Master index of all design docs |
| [Economy System v4.0](参考资料/经济系统.md) | Real-data-based economy, break-even math |
| [World Lore & Timeline](参考资料/世界观扩展设定.md) | USET history, sand-standard currency, the timeline |
| [Character Profiles](参考资料/角色设定.md) | Bios, skill trees, salary tables |
| [Core Gameplay Loop](docs/compose/specs/核心玩法循环.md) | The 3-layer time model & trendlines |
| [Cross-System Formulas](docs/compose/specs/跨系统联动公式.md) | Every formula, cross-linked |
| [Tech Tree](docs/compose/specs/科技树设计.md) | 3 exclusive branches |
| [Sand-Tech Rival](docs/compose/specs/沙能竞争系统设计.md) | USET's campaign AI |
| [Story Timeline](参考资料/故事线时间轴.md) | 2050–2076, verified continuity |

---

## 📊 Development Status

```
Phase 1: Design docs & world-lore ✅ Complete
Phase 2: Code (11/11 tasks)        ✅ Complete
Phase 3: Asset generation          ⏳ In progress
Phase 4: Integration & polish      ⏳ Planned
```

**Implemented so far:** prologue JSON scripts (scenes 01–10) · VN→gameplay bridge · full economy simulation · crew system · random events · USET rival AI · tutorial · unified save/load · term glossary.

---

## 📄 License

[MIT](LICENSE) © 2026 NMDX721

Free to use, copy, modify, distribute — keep the copyright notice. No warranty.

---

*Inspired by 「まいてつ」(Maitetsu) and Stardew Valley. Built by a high-school student with unreasonable ambitions.*