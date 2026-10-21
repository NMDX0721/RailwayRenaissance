# NF5K lighting audit — 2026-10-21

## Natural-lighting research and implemented follow-up

- [Unity render pipeline lighting support](https://docs.unity.com/en-us/engine/6000.0/manual/render-pipelines/choose-a-render-pipeline/feature-comparison): URP supports baked GI, light probes/APV and reflection probes; these are different tools for diffuse indirect illumination and environmental specular reflection. No claim that point-light fills equal GI.
- [Unity rendering layers](https://docs.unity3d.com/cn/6000.0/Manual/urp/features/rendering-layers-introduction.html): separate influence by light/surface. Enabled light layers globally; NF5K opaque renderers use masks 1|2, transparent renderers use 1, approximation lights use 2. Real lamps retain mask 1. This removes fictitious point-source reflections from glass while preserving real lamp response. OpenGL/GLES do not support this feature, so Android API/performance acceptance remains open.
- [Unity tonemapping](https://docs.unity3d.com/ja/Packages/com.unity.render-pipelines.universal@14.0/manual/post-processing-tonemapping.html): Neutral remaps dynamic range with restrained colour alteration; ACES adds stronger contrast. A persistent NF5K-only Volume profile uses Neutral, with camera post-processing/HDR enabled and ARGBHalf screenshot targets before final PNG conversion. No bloom or automatic exposure is added; normal/night/power comparisons retain a fixed pipeline. This is SDR image tone mapping, not enabling HDR monitor output.

Natural-lighting priorities: readable lower surfaces, restrained highlights, plausible real-lamp shadows and no fake bulbs reflected in windows. Remaining work includes calibrated indirect lighting, scene-aware reflection probes, and a dedicated Linear 3D workflow without changing the existing Gamma 2D/UI project. Static baking alone would not solve independent lamp switching and a moving train; scenario/power-state handling must be designed before claiming that integration complete.

Final verification: tools/compile/cab_lighting_natural_final.log passed scene reload/render and actual Play Mode, including eight floor-fill disable bindings. Lower-image ROI (x150..1250, y650..1000, top-origin) grayscale mean rose from 52.11 to 100.74 on the 0..255 scale relative to dd7cdb9. This is a visibility comparison, not lux. Registered the named rendering layer through Unity Editor API; undefined mask bits would otherwise be filtered out. Side-window artificial white dots are removed in the reviewed normal passenger image. Seat-edge highlights remain too concentrated; tone mapping alone has not solved the point-source approximation and Gamma workflow limits.

## Follow-up: lower cabin visibility and shadows

The first correction still left the lower cabin too dark. Added eight floor-reflection approximation lights at 0.85m passenger / 0.65m cab height, synchronized with the existing ordinary ceiling circuits. Passenger upper/lower bounce intensities are now 1.8/1.2; cab 1.1/0.8. These are indirect-light approximations, not eight extra physical lamps. They shut down on normal-supply loss and do not brighten the independent emergency or night-instrument circuits.

The shared URP asset now supports additional-light shadows and soft shadows, with a 4096px additional shadow atlas. This capability is global: other scenes using this asset can also render shadows on lights that request them. NF5K still selects its own Forward+ renderer, leaving the default renderer selection intact. Direct spots cast shadows; approximate bounce points intentionally do not. This provides contact/occlusion from direct light while retaining lower-cabin readability in the absence of GI. GPU cost increases; Android performance has not been accepted. Earlier statements below about disabled shadows describe the pre-follow-up audit state.

## Findings and change

The active URP asset used Forward rendering with four additional lights per object. The car has 18 interior fixtures and eight local bounce approximations. Long walls, ceilings and fittings can therefore lose contributions even when all light components are enabled. NF5KLightingRendererSetup now installs a dedicated Forward+ renderer and selects it on the review camera. The shared default renderer remains unchanged for other scenes. The generator is repeatable and does not append duplicate renderer entries.

Passenger bounce approximation intensity changes from 0.12 to 0.95 and its range from 2.6 to 3.2 metres; cab ceiling bounce changes from 0.22 to 0.65. These aim to lift walls and the lower cabin without increasing the direct spot intensity or clipping seat highlights. Bounce lights remain synchronized with their source circuit, including power failure and component disable.

## Physical reference and limits

- [EU rolling-stock TSI, original 2014 text](https://eur-lex.europa.eu/eli/reg/2014/1302/oj/eng), section 4.2.9.1.8: desk-level general illumination above 75 lux and a separately controlled reading area adjustable above 150 lux. This is a historical engineering reference, not a claim of current certification or a universal passenger-compartment target.
- [SCHURTER railway interior lighting](https://www.schurter.com/en/products-and-solutions/railway-technology/interior-lighting): independent main/emergency inputs, reduced emergency output, multiple circuits and actual vehicle measurement. Normal passenger lighting should support comfortable visibility; emergency and night-driving modes deliberately have different purposes.
- [Unity Forward/Forward+ documentation](https://docs.unity3d.com/cn/6000.0/Manual/urp/rendering/forward-rendering-paths.html): Forward+ removes the Forward per-object additional-light limit; camera/platform limits and GPU cost still apply.

Project colour space remains Gamma to preserve the existing 2D/UI project. Additional-light shadows are globally disabled in the active URP asset, so requesting soft shadows on a spot does not enable them. No baked GI, photometric calibration, HDR tone-map review or Android performance acceptance is claimed. Local point bounce is an approximation and may produce specular spots on glass. PNG grayscale measurements are image comparisons, never lux. These are explicit remaining rendering tasks, not solved by raising lamp power.

## Verification

Rebuild with CabInteriorBlockoutSetup.BatchBuild; inspect tools/compile/cab_lighting_audit_final.log. Existing image checks compare normal/night, passenger on/off/emergency/blackout and instrument dimming. Play Mode checks verify circuit independence, legacy state compatibility, supply restoration and cleanup. Review images use identical framing and dark ambient settings for paired comparisons.

Final rebuild and Play Mode checks passed. Passenger on-image ROI mean grayscale increased from 0.09256079 to 0.1596134; cab normal from 0.1289486 to 0.1485535. Night cab remained 0.013693; passenger emergency remained 0.03592012 and blackout 0.0185026. These confirm selective brightening, not physical compliance. Baseline PNGs in tools/cab_reference/lighting_audit were extracted from HEAD before this change. Glass specular spots and concentrated seat highlights remain visible and require a later reflection/GI pass.

Passenger upper bounce is lowered to 1.25m with a 3.8m range; the lower fill is raised above the floor to spread illumination and reduce point-light hot spots. The first 0.38m placement was rejected after image review.
