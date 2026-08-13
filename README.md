# Railway Renaissance: Sand Energy Impact

[![Unity](https://img.shields.io/badge/Unity-6000.4.6f1-000000?logo=unity&logoColor=white)](https://unity.com)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android-4B5320)]()
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

> ***English** | [简体中文](README.zh-CN.md)*

**A railway revival sim with visual-novel storytelling — data-driven economy, a living network, and a world that remembers everything you skip.**

---

![Laboratory](Assets/Resources/bg/lab.jpg)

*The Origin of Every Miracle — Laboratory of Intelligent Dispatch Systems, Kim Il Sung University, April 18, 2076.*

The year the world gave up on railways, and the day something impossible quietly began.

Three days later, a researcher would leave this room to revive a railway on the brink of decommissioning in a mist-wrapped mountain village called Wufeng — 2,500 kilometres and one impossible bet away.

**Every great story needs a place where it begins. This must be it.**

---

## The World

It is 2076. Sand-powered flying vehicles killed the railways — passenger volume down 78%, 91 nations dissolving their state rail systems, the global network effectively dead by 2072. The sand-energy monopoly **USET** rules the skies; the DPRK, holding the planet's largest sand reserves, is the sole superpower.

But sand-cars have six fatal flaws — no heavy freight, no storms, no long haul, no night runs, thin on fuel, hostage to an air-traffic grid. The railways' six strengths are their mirror image. You inherit grandpa's abandoned line in Wufeng, a mist-wrapped tea village in China's central hills, and set out to prove the iron road still has a reason to exist.

*Full world lore, the USET monopoly, and the sand-standard currency: [World & Timeline](参考资料/世界观扩展设定.md)*

*Full world lore, the USET monopoly, and the sand-standard currency: [World & Timeline](参考资料/世界观扩展设定.md)*

## How the World Moves

**Five interlocking trendlines** run beneath the surface — hidden until they cross a threshold, and then the world changes irreversibly:

| Trendline | Meaning |
|-----------|---------|
| **Trust** | Public faith in your railway |
| **Fiscal Health** | Your balance sheet |
| **Sand Penetration** | USET's grip on each town's passengers |
| **Political Pressure** | How closely government is watching |
| **Infrastructure Decay** | Wear on your line, station by station |

## Methods & Architecture

**A world that runs on its own** — trains move, platforms fill, seasons pass. Decisions arrive rarely, and weigh heavily — because the simulation beneath is real, not cosmetic:

- **Five interlocking trendlines** — trust, fiscal health, sand penetration, political pressure, infrastructure decay — shift slowly behind the scenes. Each is cross-linked to the others; cross a threshold, and the world changes irreversibly.
- **A data-driven sand-standard economy** — every price, wage, and subsidy traces back to a documented formula; the [formula sheet](docs/compose/specs/跨系统联动公式.md) ties it all together.
- **A self-developed dispatch algorithm** — the **Ri Dispatch Algorithm (RDA)** is a dynamic path optimization method developed at Kim Il Sung University's Laboratory of Intelligent Dispatch Systems, coupling real-time passenger flow with line load. It serves as the underlying logic for the game's scheduling and economy systems: passenger forecasting, capacity allocation, and punctuality calculations all derive from its core approach.
- **An open research network** — infrastructure, locomotives, dispatch, and community programs grow in parallel and combine freely. No locked branches, no regretted forks.
- **A rival that plays the long game** — USET campaigns, price-wars, and buys out your lines if you let it.
- **Story as life-support** — pure operations lose money from the start; narrative relief is what keeps the railway breathing.

**Deeply embedded AI** — the visual novel runs in three modes: preset dialogue, free-form AI conversation, and a hybrid of both. A configurable provider layer (MiMo / GPT / local model) routes requests; each character carries its own personality, speaking style, favorability thresholds, and memory. Characters don't just react — they remember what you've done, and how they feel about you changes what they'll say. story beats can be generated on the fly, grounded in your world state.

*The 3-layer time model and threshold mechanics: [Core Loop & Trendlines](docs/compose/specs/核心玩法循环.md) · [AI Integration](docs/compose/specs/AI系统实现计划.md) · [VN AI Design v2.1](参考资料/视觉小说系统设计.md)*

## The People

You never run the line alone. Crew — furrowed with skills, fatigue meters, and wills of their own — start as a stubborn handful and grow as the world does: new arrivals, retirees, apprentices pulled into stories as they're told.

*Full crew profiles and the growth system: [Character Profiles](参考资料/角色设定.md)*

## The Long Game

```
Prologue visual novel (Day 0–4) → Survival → Stability → Growth
→ Breakthrough → Development → the national network
```

Political cycles swing your subsidies. Random events — storms, oil spikes, holiday crowds, USET ad blitzes — keep every season tense. The story remembers everything you skip.

---

## Getting Started

Requires [Unity 6000.4.6f1](https://unity.com) and Git.

```bash
git clone https://github.com/NMDX721/RailwayRenaissance.git
```

Open the folder in Unity Hub, then run:

| Scene | What it is |
|-------|-----------|
| `Scenes/VN_Test.unity` | The prologue visual novel (Day 0–4) |
| `Scenes/StationSlice_V1.unity` | Station management gameplay |
| `Scenes/Login.unity` · `Scenes/TitleScreen.unity` | Entry screens |

---

## Documentation

The design lives in the repo, not just in the code — every system is specified, formula-linked, and verified against the story timeline.

| Doc | Contents |
|-----|----------|
| [Game Design Doc](参考资料/游戏开发文档.md) | Master index of all design docs |
| [Economy System](参考资料/经济系统.md) | Sand-standard currency, break-even math |
| [Core Loop & Trendlines](docs/compose/specs/核心玩法循环.md) | The three-layer time model, the five trendlines |
| [Cross-System Formulas](docs/compose/specs/跨系统联动公式.md) | Every formula, cross-linked |
| [Tech & Research](docs/compose/specs/科技树设计.md) | The open research network |
| [VN AI Design](参考资料/视觉小说系统设计.md) | Three-mode dialogue, provider layer, character memory |
| [AI Implementation Plan](docs/compose/specs/AI系统实现计划.md) | Template AI → llama.cpp roadmap |
| [World & Timeline](参考资料/世界观扩展设定.md) | USET, the sand-standard, 2050–2076 lore |
| [Story Timeline](参考资料/故事线时间轴.md) | Verified continuity of the world's history |

---

## Project Layout

```
Assets/
├── Scripts/           # C# source
│   ├── VN/            # JSON-driven visual novel engine
│   ├── GameData.cs    # Economy simulation (all formulas)
│   ├── CrewManager.cs # Crew, skills, fatigue
│   ├── EventManager.cs# Random events + events.json
│   ├── SandRivalManager.cs  # USET AI
│   └── ...
├── Resources/
│   ├── Scripts/       # VN scripts (prologue_01 ~ prologue_10)
│   ├── events.json    # Event templates
│   ├── bg/            # Backgrounds
│   └── characters/    # Sprites
├── Scenes/
└── Documentation/
```

---

## Development Status

```
Phase 1: Design docs & lore    ✅
Phase 2: Core code (11/11)     ✅
Phase 3: Asset generation      ⏳ In progress
Phase 4: Integration & polish  ⏳ Planned
```

Implemented: prologue scripts · VN→gameplay bridge · economy simulation · crew system · random events · USET rival AI · tutorial · unified save/load · term glossary.

---

## Contributing

This is a personal, non-commercial, educational project by a high-school developer. Contributions of all kinds — art, balance numbers, story, code — are welcome. The [design docs](参考资料/游戏开发文档.md) are the source of truth; open an issue or PR to discuss changes.

---

## License

[MIT](LICENSE) © 2026 NMDX721 — free to use, copy, modify, distribute, with attribution. No warranty.

---

*Inspired by 「まいてつ」(Maitetsu) and Stardew Valley.*