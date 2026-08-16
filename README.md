# Railway Renaissance: Sand Energy Impact

[![Unity](https://img.shields.io/badge/Unity-6000.5.0f1-000000?logo=unity&logoColor=white)](https://unity.com)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android-4B5320)]()
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

> ***English** | [简体中文](README.zh-CN.md)*

**A railway revival simulation with visual-novel storytelling — a data-driven economy, a living rail network, and a world that responds to player choices.**

---

![Laboratory](Assets/Resources/bg/lab.jpg)

*The Origin of Every Miracle — Laboratory of Intelligent Dispatch Systems, Kim Il Sung University, April 18, 2076.*

The year the world gave up on railways, and the day something impossible quietly began.

Three days later, a researcher would leave this room to revive a railway on the brink of decommissioning in a mist-wrapped mountain village called Wufeng — 2,500 kilometres and one impossible bet away.

**Every great story needs a place where it begins. This must be it.**

---

## The World

It is 2076. Sand-powered flying vehicles ended the railway era: passenger volume fell 78%, 91 nations dissolved their state rail systems, and the global network effectively shut down by 2072. The sand-energy monopoly **USET** controls the skies, while the DPRK — holder of the world's largest sand reserves — stands as the sole superpower.

Sand vehicles have six critical weaknesses: limited heavy-freight capacity, vulnerability to storms, short operational range, poor nighttime efficiency, dependence on sand supply, and air-traffic restrictions. These are mirrored by the railways' six strengths. The player inherits a decommissioned line in Wufeng, a mist-wrapped tea village in central China, and works to prove that the iron road still has a place in the world.

*[World lore and timeline](参考资料/世界观扩展设定.md)*

## Game Structure

### Five Trendlines

Five interlinked trendlines operate beneath the surface. Invisible until they cross a threshold, each can trigger irreversible change:

| Trendline | Definition |
|-----------|------------|
| **Trust** | Public confidence in the railway |
| **Fiscal Health** | The player's financial standing |
| **Sand Penetration** | USET's market share in each town |
| **Political Pressure** | Degree of government oversight |
| **Infrastructure Decay** | Cumulative wear on the line |

### Core Systems

The game runs on five connected simulation engines:

- **Sand Standard Core** — the economic engine. Passenger flow, fuel cost, wages, maintenance, accident probability, and trust are each governed by documented formulas. Revenue minus expenditure produces a real figure rather than a scripted curve. The engine drives the five trendlines. [Formula reference](docs/compose/specs/跨系统联动公式.md)
- **Chollima Genesis Core** — world generation. A seed initialises the underlying structure at the start of each playthrough: supply-demand relationships, resource distribution, and political leanings. Two playthroughs differ because the underlying structure differs. [Specification](docs/compose/specs/千里马创世核.md)
- **Suiyue Narrative Engine** — event generation. The system analyses the current world state — seed-derived structure, trendline positions, and player-character relationships — and produces events that adapt to variables hardcoding cannot anticipate. [Specification](docs/compose/specs/岁月叙事引擎.md)
- **Seonmin Personnel System** — crew management. Each crew member has skill tracks, growth rates, and potential caps. Fatigue and loyalty are intentionally not displayed; they surface through behaviour — tone of voice, willingness to work overtime, and reactions after incidents. [Specification](docs/compose/specs/先民人事系统.md)
- **Iron Dragon Competition System** — the rival AI. USET maintains a per-city penetration value that grows naturally and accelerates through campaigns; every 30 days it takes an action. Its Iron Dragon Project, a "railway heritage protection" front, acquires financially weak lines. [Specification](docs/compose/specs/铁龙竞争系统.md)

### Narrative and Economy

Early pure operations are loss-making by design; story-driven grants provide the cash flow that sustains the railway during the survival phase.

The visual novel supports three dialogue modes (preset / free AI / hybrid), switchable between MiMo, GPT, or a local model. Characters carry personalities, favorability thresholds, and memory. A planned phone-based assistant, powered by the player's own API key, is narratively framed as a remote call to the university server running a modified RDA (Railway Decision Assistant). [Design spec](参考资料/视觉小说系统设计.md)

*[Core loop](docs/compose/specs/核心玩法循环.md) · [Cross-system formulas](docs/compose/specs/跨系统联动公式.md)*

## Crew

The player does not run the line alone. Crew members begin as a small group and grow with the world: new arrivals, retirements, and apprentices enter the narrative as it unfolds. [Character profiles](参考资料/角色设定.md)

## Progression

```
Prologue visual novel (Day 0–4) → Survival → Stability → Growth
→ Breakthrough → Expansion → National network
```

Political cycles affect subsidies. Random events — storms, oil price spikes, holiday crowds, USET advertising campaigns — vary the pressure across seasons.

---

## Getting Started

Requires [Unity 6000.5.0f1](https://unity.com) and Git.

```bash
git clone https://github.com/NMDX721/RailwayRenaissance.git
```

Open the folder in Unity Hub and run:

| Scene | Purpose |
|-------|---------|
| `Scenes/VN_Test.unity` | Prologue visual novel (Day 0–4) |
| `Scenes/StationSlice_V1.unity` | Station management gameplay |
| `Scenes/Login.unity` · `Scenes/TitleScreen.unity` | Entry screens |

---

## Documentation

Design specifications are stored in the repository alongside the code. Each system has a specification, formula references, and timeline consistency checks.

| Document | Contents |
|----------|----------|
| [Game Design Document](参考资料/游戏开发文档.md) | Master index of design documents |
| [Sand Standard Economy](参考资料/沙本位经济核.md) | Currency system, break-even analysis |
| [Core Loop & Trendlines](docs/compose/specs/核心玩法循环.md) | Three-layer time model, five trendlines |
| [Cross-System Formulas](docs/compose/specs/跨系统联动公式.md) | Interlinked formula set |
| [Tech Tree](docs/compose/specs/科技树设计.md) | Open research network |
| [VN AI Design](参考资料/视觉小说系统设计.md) | Three-mode dialogue, provider layer, character memory |
| [Suiyue Narrative Engine](docs/compose/specs/岁月叙事引擎.md) | Template-based AI roadmap |
| [World & Timeline](参考资料/世界观扩展设定.md) | USET, sand-standard currency, 2050–2076 lore |
| [Story Timeline](参考资料/故事线时间轴.md) | Continuity verification of world history |

---

## Project Layout

```
Assets/
├── Scripts/           # C# source
│   ├── VN/            # JSON-driven visual novel engine
│   ├── Narrative/     # Suiyue narrative engine
│   ├── WorldGen/      # Chollima world generation
│   ├── GameData.cs    # Economy simulation
│   ├── CrewManager.cs # Crew, skills, fatigue
│   ├── SandRivalManager.cs  # USET rival AI
│   └── ...
├── Resources/
│   ├── Scripts/       # VN scripts (prologue_01 ~ prologue_10)
│   ├── Seeds/         # World seed data
│   ├── events.json    # Event templates
│   ├── bg/            # Backgrounds
│   └── characters/    # Character sprites
├── Scenes/
└── Documentation/
```

---

## Development Status

```
Phase 1: Design documents & lore    ✅
Phase 2: Core systems (11/11)       ✅
Phase 3: Asset generation           ⏳ In progress
Phase 4: Integration & polish       ⏳ Planned
```

Implemented: prologue scripts · VN-to-gameplay bridge · economy simulation · crew system · random events · USET rival AI · tutorial · unified save/load · term glossary.

---

## Contributing

This is a personal, non-commercial, educational project. Contributions of any kind — art, balance values, story, code — are welcome. The [design documents](参考资料/游戏开发文档.md) are the source of truth; open an issue or pull request to propose changes.

---

## License

[MIT](LICENSE) © 2026 NMDX721 — free to use, copy, modify, and distribute with attribution. Provided without warranty.

---

*Inspired by Maitetsu and Stardew Valley.*