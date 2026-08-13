# Railway Renaissance: Sand Energy Impact

> ***English** | [简体中文](README.zh-CN.md)*

A hardcore pixel-art railway management sim with Visual Novel storytelling — built with Unity 6.

> 一个高中生开发的硬核像素风铁路模拟经营 + 视觉小说游戏。

---

## Overview

In a world where sand-powered flying vehicles (Sand-Tech) have rendered traditional railways obsolete, you inherit a abandoned railway line in the remote village of Wufeng. Your mission: restore the railway, win back the trust of the community, and fight against the encroaching Sand-Tech monopoly.

**Genre**: Simulation / Management / Visual Novel / Strategy  
**Platform**: Windows (primary), Android (planned)  
**Engine**: Unity 6000.4.6f1 (Unity 6)  
**Style**: 16-bit pixel art, warm palette  

---

## Features

| Feature | Description |
|---------|-------------|
| **Hardcore Economy** | Pure operations lose money. Story missions provide the subsidies that keep you alive. |
| **Slow-Paced Gameplay** | The world runs continuously in the background. Observe trends, make long-term choices, face irreversible consequences. |
| **5 Trend System** | Trust, Fiscal Strain, Sand Penetration, Political Pressure, Infrastructure Decay — interconnected trends that shape your world. |
| **Sand-Tech Rival** | An AI-driven competitor that campaigns, price-wars, and tries to acquire your lines. |
| **Crew Management** | 5 unique characters with 4 skills each, fatigue system, training, and role assignments. |
| **Tech Tree** | 3 mutually exclusive branches (Industrial / Ecological / Automation). |
| **Story-Driven** | Prologue as Visual Novel, then seamlessly transitions into management gameplay. |
| **Random Events** | 10+ event templates: economic shifts, weather, personnel, Sand-Tech actions. |
| **Political Cycles** | Local government shifts between Authoritarian, Market, and Welfare orientations. |
| **Term Glossary** | Yellow-highlighted terms in dialogue with popup explanations. |

---

## Getting Started

### Prerequisites

- Unity 6000.4.6f1 (Unity 6)
- Git

### Installation

```bash
git clone https://github.com/NMDX721/RailwayRenaissance.git
```

Open the project in Unity Hub:

1. Launch Unity Hub
2. Click **Add** → **Add project from disk**
3. Select the cloned directory
4. Open with Unity 6000.4.6f1

### Running

Open one of these scenes in Unity Editor:

| Scene | Description |
|-------|-------------|
| `Scenes/Login.unity` | Login screen (uGUI) |
| `Scenes/VN_Test.unity` | Visual Novel test (start of prologue) |
| `Scenes/StationSlice_V1.unity` | Station management gameplay |

---

## Project Structure

```
Assets/
├── Scripts/                    # C# source code
│   ├── VN/                     # Visual Novel system
│   │   ├── VNManager.cs        # Core VN controller
│   │   ├── VNExitData.cs       # VN→Gameplay bridge data
│   │   ├── VNSaveSystem.cs     # Save/load system
│   │   └── ...
│   ├── GameData.cs             # Core economy simulation
│   ├── CrewManager.cs          # Staff management
│   ├── EventManager.cs         # Random events
│   ├── SandRivalManager.cs     # Sand-Tech AI
│   ├── TutorialManager.cs      # Tutorial system
│   └── ...
├── Resources/
│   ├── Scripts/                # VN JSON scripts (prologue_01~10)
│   ├── events.json             # Event templates
│   └── characters/             # Character sprites
├── Scenes/                     # Unity scenes
└── Documentation/              # Design docs
```

---

## Design Documents

| Document | Description |
|----------|-------------|
| [Game Design Doc (GDD)](参考资料/游戏开发文档.md) | Master index for all design docs |
| [Economy System](参考资料/经济系统.md) | Economy model v4.0 |
| [Character Profiles](参考资料/角色设定.md) | Character bios & skill system |
| [World Lore & Vehicles](参考资料/世界观与车辆设定.md) | World setting & vehicle specs |
| [Core Gameplay Loop](docs/compose/specs/核心玩法循环.md) | Gameplay loop design |
| [Cross-System Formulas](docs/compose/specs/跨系统联动公式.md) | All formula definitions |
| [Tech Tree](docs/compose/specs/科技树设计.md) | 3-branch tech tree |
| [Sand-Tech Rival System](docs/compose/specs/沙能竞争系统设计.md) | Sand-Tech AI behavior |
| [VN→Gameplay Bridge](docs/compose/specs/VN与模拟经营对接文档.md) | Transition system design |

---

## Development Status

```
Phase 1: Design docs fix        ✅ Complete
Phase 2: Code implementation    ✅ Complete (11/11 tasks)
Phase 3: Asset generation       ⏳ Pending
Phase 4: Integration & polish   ⏳ Pending
```

### Phase 2 Tasks

| Task | Description | Status |
|------|-------------|--------|
| T1 | Prologue JSON files (scenes 07-10) | ✅ |
| T2 | VN→Gameplay transition system | ✅ |
| T3 | Economy simulation with formulas | ✅ |
| T4 | Crew management system | ✅ |
| T5 | Random event system | ✅ |
| T6 | Sand-Tech rival AI | ✅ |
| T7 | Tutorial system | ✅ |
| T8 | Unified save system | ✅ |
| T9 | Term glossary system | ✅ |

---

## License

[MIT](LICENSE) © 2026 NMDX721

This project is released under the MIT License — a permissive open-source license that allows anyone to use, copy, modify, merge, publish, distribute, sublicense, and sell copies of the software. The only requirement is that the copyright notice and permission notice are included in all copies or substantial portions.

**In short**: You can do almost anything with this code, as long as you keep the attribution. No warranty is provided — the software is "as is".

---

*Made with ❤️ by a high school student developer. Inspired by 「まいてつ」(Maitetsu) and Stardew Valley.*