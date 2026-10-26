# NF5K proportions and daylight review

## Deferred requirement — user decision 2026-10-21

真实动态日照与天气暂缓实现，当前只保留检查预设，不继续扩展光照系统。后续需要太阳随游戏时间连续移动并落山，白天、黄昏、夜间自然过渡；天气、云量、雾雨应持续影响直射光、天空散射、能见度与室内受光。车内灯、仪表灯、头灯和应急灯仍独立于环境光控制。实现时统一时间、路线位置/朝向、天气状态与存档恢复，避免各模块各自计时或仅瞬间切换滤镜。届时需验证日落前后、阴晴变化及进入遮蔽区域的连续效果，并核对反射更新、性能和现有 Gamma/间接光近似的处理方案。

Status: deferred by user; no solar-motion, continuous weather or time simulation is implemented by this note.

## Scope

`Assets/Scenes/CabDaylightReview.unity` is an independent copy of the assembled car prepared by `NF5KDaylightReview.Prepare`. The original interior scene retains its paired night/emergency regression workflow. This scene supplies procedural sky, a directional sun with soft shadows and a large neutral ground for daylight review. It is a lighting test environment, not the finished route, railway terrain or weather simulation.

Open the scene in Unity. The `Daylight_Environment` component exposes Clear/Overcast/Low sun context menus. In Play Mode the bottom-centre Chinese panel switches the same three presets; existing interior controls independently switch lamps. Saved default is clear daylight with ordinary interior/exterior lamps off. Compare lamps off/on without changing the camera, tone mapping or exposure. No automatic exposure is used. Review camera far clip is 350m; the ground is 240m square. Review images use a 60-degree passenger field of view to reduce wide-angle distortion.

The same panel offers driver/passenger/exterior viewpoints (65/60/55 degree field of view). These can also be selected from the component's View context menus. This does not introduce a free-flight or walking controller.

## Measured proportion checks

`NF5KDaylightReview.AuditDimensions` reads renderer bounds from the reloaded scene and writes `tools/compile/nf5k_proportions.json`. Checks cover 48 cushions, 450mm cushion width, 1100mm row pitch, aisle at least 650mm, seat top 480–560mm and ceiling clearance at least 2100mm. These are project acceptance ranges, not legal/ergonomic certification. Actual aisle target is 664mm; usable width between inner liners is approximately 2705mm. The 2.8m structural inner floor width and 18.15m exterior length remain design estimates; this audit does not assert exact original NF5K dimensions or complete wheelchair/evacuation compliance. Driver eye is measured before screenshot camera movements.

Existing seat-wall/heater/floor tests continue to run. Seats remain 450mm wide; their repaired wall gap is preserved. Scale should be assessed from metric scene geometry and seated/standing viewpoints, rather than from perspective screenshots alone.

## Lighting limitations

Gamma colour space and authored diffuse SH remain in use. Day ambient is a broad approximation and does not solve window visibility or multi-bounce daylight penetration. Local reflection probes now track sun colour/intensity/rotation and ground ambient as well as the interior lights, so daylight preset changes request refreshed captures. The scene is suitable for controlled visual comparisons, not calibrated lux measurements. Clear/overcast/low-sun are authored presets; no geographical/time-of-day physical model is claimed.

## Generation and outputs

Menu: `RailRevival/Cab/Prepare Daylight Review Scene`. Full batch review generates the scene and seven images (`day_Clear_lamps_off/on`, `day_Overcast_lamps_off/on`, `day_LowSun_lamps_off/on`, `day_driver`) under `tools/cab_reference/interior_review/`, then restores the original scene before its Play Mode regressions. Log: `tools/compile/cab_daylight_review.log`.

Measured geometry: cushion width 0.450m, cushion upper envelope 0.540m, row pitch 1.100m, clear aisle 0.664m, inner liner clearance 2.705m, ceiling clearance 2.1475m and seated driver eye 1.380m. The seat height is the unloaded top envelope, not a measured occupied hip point. These preserve the current project scale; exact NF5K factory dimensions remain unverified.

Final validation log: `tools/compile/cab_daylight_review_verified.log`. Run `python tools/cab_reference/check_daylight_pairs.py` for fixed-camera lamp-on/off image comparison; results are saved to `tools/cab_reference/lighting_audit/daylight_pairs.json`. The full batch also checks seat/wall/floor clearance and original lighting Play Mode behaviour.
