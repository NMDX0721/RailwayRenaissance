# Railway Renaissance: Sand Energy Impact 🚂

> ***English** | [简体中文](README.zh-CN.md)*

A hardcore pixel-art railway management sim with Visual Novel storytelling — built with Unity 6 by a high school school student developer.

---

### 🖼️ Laboratory of Intelligent Dispatch Systems

![Laboratory Interior](Assets/Resources/bg/lab.jpg)

**Kim Il Sung University, Pyongyang — Laboratory of Intelligent Dispatch Systems**

The Laboratory of Intelligent Dispatch Systems is the university's flagship research facility for next-generation railway scheduling algorithms. The laboratory is equipped with modern computing equipment and display terminals, with windows overlooking the Pyongyang skyline. It is here that Lin Biaohan, an honor graduate student of Kim Il Sung University, conducts his research on intelligent dispatch systems.

His work focuses on dynamic path optimization — specifically, the integration of Dijkstra's algorithm with real-time passenger flow data to create adaptive scheduling models. The field of intelligent dispatch is one in which Kim Il Sung University has established itself as a global leader.

On his desk, beside the terminal displaying complex scheduling algorithm models, lies a worn copy of a railway engineering handbook. The cover page bears a faded handwritten inscription: *"For Biaohan — Grandpa."*

---

## 🎮 The Game

### Story

> 2076. Sand-powered flying vehicles have rendered railways obsolete. You inherit a abandoned line in the remote village of Wufeng from your grandfather. Restore the railway, win back the community's trust, and fight against the Sand-Tech monopoly.

### Gameplay

- **Hardcore Economy** — Operations lose money at first. Story missions keep you alive. Every decision matters.
- **Slow-Paced Strategy** — The world runs continuously. You observe trends, make long-term choices, face irreversible consequences.
- **Sand-Tech Rival** — An AI-driven competitor that actively campaigns, price-wars, and tries to acquire your lines.
- **Crew Management** — 5 unique characters with skills, fatigue, training, and role assignments.
- **Tech Tree** — 3 mutually exclusive branches (Industrial / Ecological / Automation).
- **Political Cycles** — Local government shifts between Authoritarian, Market, and Welfare orientations.
- **Story-Driven** — Prologue as Visual Novel, then transitions into management gameplay with ongoing narrative events.
- **Random Events** — 10+ event templates: economic shifts, weather, personnel, Sand-Tech actions.

### Screenshots

*(Coming soon — game is in active development)*

---

## 🚀 Getting Started

### Prerequisites

- Unity 6000.4.6f1 (Unity 6)
- Git

### Installation

```bash
git clone https://github.com/NMDX721/RailwayRenaissance.git
```

Open in Unity Hub:

1. Launch Unity Hub → **Add** → **Add project from disk**
2. Select the cloned directory
3. Open with Unity 6000.4.6f1

### Scenes

| Scene | Description |
|-------|-------------|
| `Scenes/Login.unity` | Login screen |
| `Scenes/TitleScreen.unity` | Title screen (video background) |
| `Scenes/VN_Test.unity` | Visual Novel prologue |
| `Scenes/StationSlice_V1.unity` | Station management gameplay |

---

## 📁 Project Structure

```
Assets/
├── Scripts/           # C# source code
│   ├── VN/            # Visual Novel system
│   ├── GameData.cs    # Economy simulation
│   ├── CrewManager.cs # Staff management
│   ├── EventManager.cs# Random events
│   ├── SandRivalManager.cs  # Sand-Tech AI
│   └── ...
├── Resources/
│   ├── Scripts/       # VN JSON scripts (prologue_01~10)
│   ├── events.json    # Event templates
│   └── characters/    # Character sprites
├── Scenes/            # Unity scenes
└── Documentation/     # Design docs
```

---

## 🛠 Technical Details

### Tech Stack

| Component | Technology |
|-----------|-----------|
| Engine | Unity 6000.4.6f1 (Unity 6) |
| UI | UI Toolkit (VN/Title) + uGUI (Login) |
| VN System | JSON-driven, data-driven dialogue |
| Save System | PlayerPrefs + JSON serialization |
| AI (future) | Template-based (phase 1), llama.cpp (phase 2) |

### Design Documents

| Document | Description |
|----------|-------------|
| [Game Design Doc](参考资料/游戏开发文档.md) | Master index for all design docs |
| [Economy System](参考资料/经济系统.md) | Economy model v4.0 |
| [Character Profiles](参考资料/角色设定.md) | Character bios & skill system |
| [World Lore & Vehicles](参考资料/世界观与车辆设定.md) | World setting & vehicle specs |
| [Core Gameplay Loop](docs/compose/specs/核心玩法循环.md) | Gameplay loop design |
| [Cross-System Formulas](docs/compose/specs/跨系统联动公式.md) | All formula definitions |
| [Tech Tree](docs/compose/specs/科技树设计.md) | 3-branch tech tree |
| [Sand-Tech Rival System](docs/compose/specs/沙能竞争系统设计.md) | Sand-Tech AI behavior |

### Key Formulas

- **Daily passenger flow**: `population × 0.001 × (0.5 + trust × 0.005) × (1 - sandPenetration) × (1 + conductorLevel × 0.03)`
- **Daily fuel cost**: `58.5 × 92km / 100 × (1 + (100 - trainCondition) / 200) × 15 sand/升`
- **Accident probability**: `0.5% × ageFactor × driverSkillFactor × maintenanceFactor × weatherFactor`
- **Trust change**: `normalOps(+0.014/day) - accident(0.08×severity) - sandAds(0.03)`

---

## 📊 Development Status

```
Phase 1: Design docs fix        ✅ Complete
Phase 2: Code implementation    ✅ Complete (11/11 tasks)
Phase 3: Asset generation       ⏳ Pending
Phase 4: Integration & polish   ⏳ Pending
```

### Phase 2 Tasks

| Task | Status |
|------|--------|
| Prologue JSON scripts (scenes 07-10) | ✅ |
| VN→Gameplay transition system | ✅ |
| Economy simulation with formulas | ✅ |
| Crew management system | ✅ |
| Random event system (10 events) | ✅ |
| Sand-Tech rival AI | ✅ |
| Tutorial system | ✅ |
| Unified save/load system | ✅ |
| Term glossary system | ✅ |

---

## 📄 License

[MIT](LICENSE) © 2026 NMDX721

A permissive open-source license: you can use, copy, modify, and distribute this software freely, as long as you keep the copyright notice. No warranty provided.

---

*Inspired by 「まいてつ」(Maitetsu) and Stardew Valley.*