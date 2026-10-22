# NF-5K pneumatic equipment — 2026-10-22

## Scope and generation

`Assets/Editor/NF5KPneumaticEquipment.cs` adds visual pneumatic equipment through `Apply`, called after the anime detail pass by `CabInteriorBlockoutSetup`. `BatchBuild` updates CabInteriorBlockout, CabDaylightReview, CabSimulationReview and CabRailMotionReview using Unity scene APIs. Existing running-gear transforms and motion bindings are preserved.

Equipment: two main reservoirs, one auxiliary reservoir, twin dryer canisters, a valve manifold, eight sandboxes/nozzles, retaining bands, saddles, drain cocks, pipe clamps and four flexible body/bogie connections. Muted grey-blue pipes, warm enamel sandboxes and localized steel fittings follow the anime 3D art direction.

## Mechanical arrangement

Main-reservoir supply (MR), brake pipe (BP) and brake-cylinder output (BC) have separate visual routes. Dryer input uses a proposed interface on an existing generic equipment cabinet; its internal compressor has not been modeled. Dryer output feeds MR, not BP. The manifold has distinct supply/control/output ports. Reservoir saddles reach the underframe; drains have transverse closed-position levers.

Sand delivery tubes route around the wheel envelope; their outlets sit 55 mm above the review rail datum, aimed toward the wheel/rail nip. Metal nozzle sections stop at the outlet. Sandboxes and their supports belong to each bogie.

`CabFlexibleHose` runs after `CabRailMotion` (execution orders 150 and 100). A dynamic cubic tube follows serialized body/bogie endpoint transforms in world space. The static baseline meshes persist in NF5KBodyMeshes; runtime meshes are separately allocated and destroyed. Cached geometry buffers avoid per-update script array creation. These curves are visual approximations, not hose stress or minimum bend-radius solvers.

## Validation

`tools/compile/cab_pneumatic_verified.log` records four-scene generation and Play Mode passes. Reservoir/sandbox bounds are checked against selected wheels, motors, gearcases and existing cabinets. Sand nozzle/tube vertices are checked against conservative wheel envelopes: minimum measured gap approximately **14.67 mm**. This is not an exhaustive dynamic collision check.

Play Mode verifies moved hose endpoints within 1 mm, backend-driven rail motion, contextual input/rebinding, startup, traction, interlocks, checkpoint restoration and emergency braking. Inspection renders: `pneumatic_reservoir_verified.png` and `sanding_equipment_verified.png` under `tools/cab_reference/interior_review/`.

## Simulation boundary and remaining work

No changes to CabSimulation or its codec. Equipment does not add airflow, pressure-network coupling, sanding discharge/particles, sand consumption or adhesion effects. Visible main-reservoir external envelopes are about 250 L each and the auxiliary about 30 L; these are not calibrated internal capacities and do not replace the backend's lumped 1000 L / 300 L parameters. Capacity reconciliation requires a later simulation/layout design pass. No pressure-vessel or mounting-strength certification is implied.

Fuel tank, battery enclosure, recognizable powerpack/cooling equipment and exhaust remain separate missing-component work. Compressor cabinet internals, sanding valves and maintenance interaction also remain pending.

## Product references

- Knorr-Bremse railway air supply: https://rail.knorr-bremse.com/en/se/portfolio/products-and-systems/braking-systems/supply/
- Knorr-Bremse sanding systems: https://www.knorr-bremse.us/en/products/braking-systems/sanding-systems/

References support the equipment categories and compressed-air sanding principle; this fictional NF-5K arrangement is not a reproduction of a certified product installation.
