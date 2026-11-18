---
name: railway-context
description: Navigate the RailwayRenaissance Unity project, find authoritative design and implementation guidance, and identify the right validation path before making changes.
---

# RailwayRenaissance project guide

Use this skill to find the current source of truth quickly. It is an index, not a second copy of changing project facts.

## Project root and runtime

- Unity project: `D:/Unity Project/RailwayRenaissance`
- C# gameplay and UI: `Assets/Scripts/`
- Editor reviews and asset builders: `Assets/Editor/`
- Runtime data and art: `Assets/Resources/`
- Scenes and project settings: `Assets/Scenes/`, `ProjectSettings/`
- Project-specific agent instructions: `AGENTS.md`
- AI implementation map and verification notes: `docs/for-ai/`
- Game design, setting, narrative, art, and production references: `参考资料/`
- Art briefs and runtime asset specifications: `Assets/Resources/Art/`

Read `AGENTS.md` first. Before code changes, read `docs/for-ai/PROJECT_OVERVIEW.md` and `docs/for-ai/CODE_MAP.md`, as required by the project instructions. Read the feature-specific design and verification document linked from those files.

Read the current editor version from `ProjectSettings/ProjectVersion.txt`. Do not copy a version number into this guide.

## Find the implementation

| Area | Starting point |
|------|----------------|
| Tablet desktop, navigation, task lifecycle, app entry | `Assets/Scripts/GameMainUI.cs` |
| RDA statistics and management pages | `Assets/Scripts/UI/Tablet/Apps/TabletRDAAppUI.cs` |
| RDA top-down exploration map | `Assets/Scripts/UI/Tablet/Apps/TabletExploreAppUI.cs` |
| Paper UI drawing system | `Assets/Scripts/UI/Tablet/Paper/` |
| Daily game economy and state | `Assets/Scripts/GameData.cs`, then follow the call sites in `CODE_MAP.md` |
| VN engine and dialogue presentation | `Assets/Scripts/VN/` |
| Music tablet app | `Assets/Scripts/UI/Tablet/Apps/` files beginning with `RailwayMusicTerminalView`, `TabletMusicAppUI`, and `Music` |
| Cab and train simulation | `Assets/Scripts/Cab/` and the relevant NF-5K or ATO note in `docs/for-ai/` |
| Art assets and importer settings | `Assets/Resources/Art/` and matching Unity `.meta` files |

Use `rg` to find the live launcher, resource path, caller, and review entry point before editing. Names in this table are navigation hints. Confirm that a file and API still exist on the current branch.

## Find the authoritative guidance

- Start with `docs/for-ai/PROJECT_OVERVIEW.md` and `docs/for-ai/CODE_MAP.md` for architecture and call paths.
- Use `参考资料/INDEX.md` to navigate design references in `参考资料/`.
- Use the relevant document under `docs/for-ai/` for subsystem behavior, constraints, and verification limits.
- For RDA information architecture, interaction rules, and the top-down exploration layer, read `docs/for-ai/UI_PAPER_DESIGN_SYSTEM.md`.
- For exploration image dimensions, composition, naming, import settings, and runtime asset boundaries, read `Assets/Resources/Art/EXPLORE_ASSET_SPEC.md` and its current scene brief.
- For pixel-art prompt creation and art import workflow, use the `pixel-art-prompt-engineer` skill.
- For VN JSON authoring, use the `vn-script-authoring` skill.
- For MiMo or Gemini task handoff, use the `mimo-bridge` skill and follow the project message-bus rules in `AGENTS.md`.

These documents have different purposes. `AGENTS.md` defines working constraints, `docs/for-ai/` explains implementation and evidence, and `参考资料/` holds game design and narrative references. Follow newer, feature-specific decisions when older overview text conflicts, and update the owning document when a code change changes documented behavior.

## Safe implementation and verification

- Check `git branch --show-current` and `git status --short` before editing. Preserve user changes and stage only files owned by the task.
- Do not edit Unity `.unity` YAML directly. Use the Unity Editor or an editor script.
- Follow the verification route in the relevant feature document. A successful compilation or review batch only proves the checks it actually ran. Keep Unity editor, standalone player, Android device, visual review, and real-account results distinct.
- For art changes, inspect the source image, importer settings, runtime loading path, and in-game composition. A concept image is not runtime integration.
- Update the relevant `docs/for-ai/` or design reference when code changes behavior, formulas, architecture, or acceptance criteria. For substantial architecture work, update `AGENTS.md` and `CODE_MAP.md` as required there.

## Keeping this guide current

Keep this file focused on stable navigation and process. Do not add character biographies, timeline facts, app inventories, feature status, current branch status, or test-result claims here. Put those details in their owning project documents, then add or update a link here only when it makes the right source easier to find.
