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

## How the World Moves

**Five interlocking trendlines** run beneath the surface — hidden until they cross a threshold, and then the world changes irreversibly:

| Trendline | Meaning |
|-----------|---------|
| **Trust** | Public faith in your railway |
| **Fiscal Health** | Your balance sheet |
| **Sand Penetration** | USET's grip on each town's passengers |
| **Political Pressure** | How closely government is watching |
| **Infrastructure Decay** | Wear on your line, station by station |

## Core Systems

**A simulation that runs on its own** — trains move, platforms fill, seasons pass. Below the surface, a set of interconnected engines drives everything:

- **Sand Standard Core (沙本位经济核)** — the economic simulation engine: passenger flow, fuel, wages, maintenance, accident probability, and trust are each governed by a documented formula. Revenue minus cost is a real number, not a scripted curve. The engine drives five trendlines; cross a threshold, and the world changes irreversibly. The [formula sheet](docs/compose/specs/跨系统联动公式.md) ties every variable together.
- **Chollima Genesis Core (千里马创世核)** — at the start of a new game, the world seed initialises the entire underlying structure: supply-demand relationships, resource distribution, character leanings. Not a dice roll every turn, but a complete world grown from a seed. Two playthroughs differ because the worlds themselves are different.
- **Suiyue Narrative Engine (岁月叙事引擎)** — named after the in-world AI, this system analyses the current world state — the seed-derived structure, the five trendlines, the player's relationships with each character — and generates events that capture variables hardcoding could never anticipate. Every event is a product of that world at that moment.
- **Seonmin Personnel System (先民人事系统)** — each crew member has four skill tracks, a growth rate, and potential caps. But you never see the numbers: fatigue and loyalty aren't displayed on any screen. They surface through behaviour — tone of voice, extra shifts, reactions after an accident. The design reduces digital input, like real life.
- **Iron Dragon Competition System (铁龙竞争系统)** — USET runs the Iron Dragon Project, a "railway heritage protection" front that actually acquires lines at rock-bottom prices. In the open USET pushes sand penetration per city through its Chollima brand campaigns; in the dark it executes the Iron Dragon takeover. Every 30 days it picks an action; respond or lose ground.
- **Story as life-support** — pure operations lose money from the start. Narrative relief grants are the cash flow that keeps the railway breathing until you can stand on your own.

**Deeply embedded AI** — the visual novel offers three dialogue modes (preset / free AI / hybrid), switchable between MiMo, GPT, or a local model. Characters carry personalities, favorability thresholds, and memory — they remember what you've done, and how they feel changes what they say. Story beats can be generated on the fly. A planned phone-based assistant, powered by the player's own API key, is narratively wrapped as a remote call to the university server running a modified RDA — the Railway Decision Assistant.

*The 3-layer time model and threshold mechanics: [Core Loop & Trendlines](docs/compose/specs/核心玩法循环.md) · [AI System Plan](docs/compose/specs/岁月叙事引擎.md) · [VN AI Design v2.1](参考资料/视觉小说系统设计.md)*

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
| [Economy System](参考资料/沙本位经济核.md) | Sand-standard currency, break-even math |
| [Core Loop & Trendlines](docs/compose/specs/核心玩法循环.md) | The three-layer time model, the five trendlines |
| [Cross-System Formulas](docs/compose/specs/跨系统联动公式.md) | Every formula, cross-linked |
| [Tech & Research](docs/compose/specs/科技树设计.md) | The open research network |
| [VN AI Design](参考资料/视觉小说系统设计.md) | Three-mode dialogue, provider layer, character memory |
| [AI Implementation Plan](docs/compose/specs/岁月叙事引擎.md) | Template AI → llama.cpp roadmap |
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