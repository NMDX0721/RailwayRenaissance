---
name: pixel-art-prompt-engineer
description: Generate pixel art asset prompts for RailwayRenaissance's 1920×1080 Unity canvas with precise specs, then guide the user through the AI-generate → verify → import workflow.
---

# Pixel Art Prompt Engineer

Generate high-quality pixel art prompts for Unity UI assets. Ensures every prompt meets the project's quality standards before the user takes it to their AI image generation tool.

## When to use

- User needs a new pixel art asset (panel, button, input field, icon, character portrait, background, logo, sprite sheet)
- User asks to revise or improve an existing prompt
- User has a generated image and needs guidance on importing it into Unity

## Canvas & style constraints

| Parameter | Value |
|-----------|-------|
| Resolution | 1920 × 1080 |
| Art style | Pixel art, 16-bit retro, Stardew Valley-like |
| Background | Pure transparent (PNG with alpha channel) |
| Icon sizes | Sprite sheet: 128×128 per icon; Input icons: 48×48; Button icons: 64×64 |
| Panel sizes | Login: 1600×1200; Button: 750×120 or 550×120; Input: 1200×400 |
| Border radius | All corners rounded, `set_corner_radius_all(8)` |

## Prompt quality checklist

Every prompt MUST pass ALL of these before being handed to the user:

1. **Length**: ≥ 200 characters
2. **Exact pixel dimensions**: specify width × height in pixels (e.g. "1600×1200 pixels")
3. **Specific color codes**: use hex codes (e.g. `#3E2723`), never vague terms like "warm tones"
4. **Transparent background**: explicitly state "Background: pure transparent, output as PNG with alpha channel"
5. **Pixel art style tag**: include "pixel art, 16-bit retro, Stardew Valley-like" or equivalent
6. **Element layout**: describe where each element sits on the canvas (e.g. "border centered at canvas edge, 24px thick")
7. **No fuzzy words**: ban "simple", "clean", "minimalist", "beautiful", "cute" — replace with concrete descriptions
8. **Reusable vs exclusive**: if the asset will be shared across multiple scenes, do NOT embed scene-specific text in the image — let Unity code overlay text

## Prompt template

```
[Asset name], pixel art, 16-bit retro style, Stardew Valley-like aesthetic.
Dimensions: [WIDTH]×[HEIGHT] pixels.
[Detailed description of each visual element with exact positions and sizes].
Colors: [element] = #[hex], [element] = #[hex], [element] = #[hex].
Border: [thickness]px, color #[hex], [style description].
[If applicable] Embedded text: "[Chinese text]" in [font style], centered at [position].
Background: pure transparent, output as PNG with alpha channel.
No anti-aliasing on edges — keep crisp pixel boundaries.
```

## Reusable vs exclusive asset decision

Before writing a prompt, determine:

| Type | Rule | Examples |
|------|------|----------|
| **Reusable** | Do NOT embed text. Keep generic. Code overlays text. | Panel bg, button, input field, icon sprite sheet |
| **Exclusive** | CAN embed text if it increases information density. | Password lock icon, specific status indicators |

When in doubt, make it reusable — reduces total asset count.

## Sprite sheet pattern

For icon collections, use sprite sheets instead of individual files:

```
A sprite sheet containing [N] icons in a [ROWS]×[COLS] grid.
Each icon is 128×128 pixels. Total canvas: [COLS×128]×[ROWS×128] pixels.
Icons: [list each icon with position in grid].
Pixel art, 16-bit retro style. Transparent background, PNG with alpha.
```

## Character portrait template

```
Full-body character portrait of [name], [age], [personality traits].
[Detailed appearance: face shape, hair, eyes, build, distinguishing features].
Clothing: [specific outfit description — NOT职业制服, must reflect personality].
Pose: standing, front-facing, full body including feet.
Style: pixel art, 16-bit retro, Stardew Valley-like aesthetic.
Canvas: 512×1024 pixels (portrait orientation).
Background: pure transparent, PNG with alpha channel.
Theme color: #[hex] (used for clothing accents).
```

## Import verification workflow

After the user generates an image, guide them through Unity import:

1. **Place file** in correct `Assets/Resources/` subdirectory
2. **Set .meta file**:
   - `textureType: 8` (Sprite)
   - `spriteMode: 1` (Single)
   - `alphaIsTransparency: 1`
3. **Code**: use `Resources.Load<Sprite>("path")` — no file extension
4. **Image component**: `Image.Type.Simple` + `preserveAspect = true` for pixel art (NOT Sliced unless spriteBorder is set)
5. **Verify in Play Mode** — Edit Mode may show blank for VideoPlayer-dependent scenes
6. **Common failures**:
   - Magenta/pink → material not assigned or `fileID: 0`
   - White background → alphaIsTransparency not set
   - Image not loading → textureType/spriteMode wrong in .meta
   - Blurry → FilterMode should be Point for pixel art

## Reference files

- `Assets/Documentation/PROMPT_LIST_CN.md` — current prompt list (v2.0, 23 assets)
- `Assets/Documentation/CHARACTER.md` — 8 character profiles with appearance details
- `Assets/Resources/UI/Login/` — login UI assets (panel, input, button, logo)

## Rules from MEMORY.md to enforce

- **面板提示词必须预留铭牌区域**: border top center, ~300×60px rectangle for title text overlay
- **角色立绘必须全身像**: "全身构图包含脚部" — no headshots
- **透明背景直接说transparent**: no chroma-key workflow needed
- **回答问题避免过分夸赞**: evaluate prompts critically, don't say "great prompt" — check against the quality checklist
