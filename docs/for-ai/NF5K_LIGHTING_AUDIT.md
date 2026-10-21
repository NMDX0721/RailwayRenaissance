# NF5K lighting audit — 2026-10-21

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
