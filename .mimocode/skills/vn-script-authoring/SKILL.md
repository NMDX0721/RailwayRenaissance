---
name: vn-script-authoring
description: Write, edit, and validate VN dialogue JSON scripts for RailwayRenaissance. Use when creating new scene scripts (prologue_*.json, post_*.json), editing dialogue, adding choices, or checking script format.
---

# VN Script Authoring Guide

## Script Location

All scripts: `Assets/Resources/Scripts/`

- `prologue_*.json` — 序章剧本（按编号递增）
- `post_*.json` — 经营期事件（按编号递增）
- `test.json` — 测试用

## JSON Schema

```json
{
  "id": "script_id",
  "nextScript": "next_script_id",  // optional
  "scenes": [
    {
      "bg": "background_id",
      "bgm": "music_id",
      "transition": "fade|slideLeft|slideRight",  // optional
      "d": [ dialogue_entries ],
      "chars": [ character_slots ]  // optional
    }
  ]
}
```

## Dialogue Entry Types

### Narration (`t: "n"`)
```json
{ "t": "n", "text": "旁白文字" }
```

### Dialogue (`t: "d"`)
```json
{ "t": "d", "s": "角色名", "text": "对话内容", "e": "emotion", "reactions": ["char:emotion"] }
```

### Choice (`t: "c"`)
```json
{
  "t": "c",
  "text": "选择提示（optional）",
  "opts": [
    { "text": "选项文字", "next": scene_index, "setValue": "var=value" }
  ]
}
```

### CG (`t: "cg"`)
```json
{ "t": "cg", "text": "cg_image_id", "size": "full|half" }
```

### Scroll News (`t: "scroll"`)
```json
{ "t": "scroll", "text": "长文本新闻内容" }
```

### Special (`t: "special"`)
```json
{ "t": "special", "text": "TRANSITION_TO_GAMEPLAY" }
```

## Character Emotions (lin_biaohan)

neutral, serious, smile, curious, bored, sad, surprise, angry, excited, gentle, shocked, wink, happy

## Character Names (Internal IDs)

| Display Name | Internal ID |
|-------------|-------------|
| 林彪悍 | lin_biaohan |
| 岁月 | suiyue |
| 老陈 | laochen |
| 嘉颖徐 | jiaying_xu |

## Reactions Format

`"reactions": ["lin_biaohan:bored", "suiyue:normal"]`

## Key Rules

1. **内心活动 `[]`** 单独成句，蓝色斜体显示
2. **对话禁动作括号** — 不要在对话文本里写`（看了看窗外）`
3. **每个scene必须有bg和bgm**
4. **choice的next是scene数组的index（0-based）**
5. **setValue格式**: `"variable=value"`，用英文逗号分隔多个
6. **id命名规范**: `prologue_XX_name` 或 `post_XXX_name`

## Common Backgrounds

hangar, station_office, railway_track, depot_morning, platform_morning, platform_evening, station_night, station_sunset, songqiao_station, village_sunset, mountain_sky, aerial_view, train_inside, lab, profsessor_office, border_town, supply_station, black, office_modern, press_conference, news_broadcast

## Common BGMs

cloud_rail, first_light, calm, melancholy, determination, embers, platform, ambient_nature, train_ambient, news, silence, emotional, warm
