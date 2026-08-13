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

## Features

- **Five interlocking trendlines** — public trust, fiscal health, sand penetration, political pressure, infrastructure decay. Each is a slow current beneath the surface — until it crosses a threshold, and the world changes irreversibly.
- **A world that runs on its own** — trains move, platforms fill, seasons pass. Decisions arrive rarely, and weigh heavily.
- **A data-driven economy** — ticket prices, fuel, wages, and subsidies all derive from a sand-standard currency model. No hand-waving; every number traces back to a documented formula.
- **An open research network** — infrastructure, locomotives, dispatch, and community programs advance in parallel and combine freely. No locked branches, no regretted forks.
- **A rival that plays the long game** — USET, the sand-energy monopoly, campaigns, price-wars, and buys out your lines if you let it.
- **Story as life-support** — pure operations lose money early. The narrative — and the grants it brings — keeps the railway breathing until you can stand on your own.
- **Five crew with four skills each** — fatigue, growth, and who sits in which seat changes your accident math and your bottom line.

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