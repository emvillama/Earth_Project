# Stage 1 — BEDROCK
**Goal:** Clean, correct, and performant foundation before adding content. Nothing
new gets built on shaky base scripts. Everything here also sets up mobile viability.

## Substages
- ☐ **1.1 Refactor & rename.** The `Voxel` class actually streams items, not voxels —
  rename to something honest (`WorldStreamer` / `ItemSpawner`). Remove dead code
  (unused `cubePos` dict, the stubbed `GenerateTerrain`). Split responsibilities.
- ☐ **1.2 Object pooling.** Replace `Instantiate`/`Destroy` churn (up to ~100 objects
  cycling) with a pool. Kills GC spikes — *mandatory* for mobile later.
- ☐ **1.3 Deterministic, frame-budgeted spawning.** Current loop tries to top up to
  `itemMax` every trigger with random gating. Rework to a steady per-frame budget with
  a hard cap; make density/radius/lifetime tunable.
- ☐ **1.4 Audio voice management.** Cap concurrent `AudioSource`s; prioritize
  nearest/loudest; recycle voices. Engines and phones choke past ~32 live voices.
- ☐ **1.5 Player controller polish.** Ground check, configurable sensitivity/speed,
  decide walk-vs-fly, sane collision with generated terrain.
- ☐ **1.6 Config via ScriptableObjects.** Move spawn density, radius, lifetimes, and
  audio params into inspector/SO configs for fast iteration.
- ☐ **1.7 Profiling baseline.** Record frame time, GC alloc, live-voice count in a
  reference scene. This is the yardstick for every later stage.

**Exit criteria:** stable frame time in-editor, no per-frame GC from spawning, capped
voices, tunable config, and a documented perf baseline.
