# Stage 1 — BEDROCK
**Goal:** Clean, correct, and performant foundation before adding content. Nothing
new gets built on shaky base scripts. Everything here also sets up mobile viability.

> **Verified against the actual code 2026-07-29.** Gameplay = 3 scripts only
> (`GeneratePlane.cs`/`Voxel`, `ItemAudioManager.cs`, `Cubie Code.cs`); the rest is the
> Resonance SDK. Notes below mark what already exists so we don't rebuild it.

## Substages
- ☐ **1.0 Quick win — kill hot-path logging.** `ItemAudioManager.Update` calls
  `Debug.Log` **every frame for every object** (~100×/frame). Delete it. Zero design
  cost, immediate perf gain. Do this first.
- ☐ **1.1 Refactor & rename.** The `Voxel` class actually streams items, not voxels —
  rename to something honest (`WorldStreamer` / `ItemSpawner`). Remove dead code
  (unused `cubePos` dict, the stubbed `GenerateTerrain` that only calls `ManageItems`).
  Split responsibilities.
- ☐ **1.2 Object pooling.** Replace `Instantiate`/`Destroy` churn (up to ~100 objects
  cycling) with a pool. Kills GC spikes — *mandatory* for mobile, and a **prerequisite
  for 1.4 and the audio fade (1.8).**
- ☐ **1.3 Deterministic, frame-budgeted spawning.** Current loop tops up to `itemMax`
  (=100) each move; x,z are `Random.Range` and Perlin only sets height. Rework to a
  steady per-frame budget with a hard cap; make density/radius/lifetime tunable.
  **Design decision:** keep uniform-random placement or move to Perlin-driven *density*
  (clumps vs clearings) for realism — decide here, it's a behavior change, not a tweak.
- ☐ **1.4 Audio voice management.** *Partly exists* — `itemMax` is a crude global cap.
  Extend to cap concurrent `AudioSource`s, prioritize nearest/loudest, recycle voices
  (per-profile/per-layer limits land with Forest_v1). Phones choke past ~32 live voices.
- ☐ **1.5 Player controller polish.** Ground check, configurable sensitivity/speed,
  decide walk-vs-fly, sane collision. *Note:* `Cubie` reads `Input.GetAxis` directly —
  wrap input behind an interface now so Stage 4 (phone sensors) can swap it cleanly.
- ☐ **1.6 Config via ScriptableObjects.** Move the hand-typed "pre-load" inputs —
  `itemMax`, `itemChance`, `rndTime`, and `WeightedAudioClip.weight` — plus spawn
  density/radius/lifetimes into SO configs for fast iteration.
- ☐ **1.7 Profiling baseline.** Record frame time, GC alloc, live-voice count in a
  reference scene. This is the yardstick for every later stage. (Take it *after* 1.0 so
  the number is honest.)
- ☐ **1.8 Audio hygiene — fade-out.** Replace the instant `Destroy` cutoff with a
  gradual fade-out (envelope release). Rides on 1.2's despawn path. Richer reverb tail
  is Stage 2, but the fade belongs here. See `docs/biomes/Forest_v1.md` §6.

**Already implemented (reuse, don't rebuild):** weighted clip selection
(`ItemAudioManager.SelectRandomClip`), a global object cap (`itemMax`), engine-level
distance attenuation via Resonance rolloff.

**Exit criteria:** stable frame time in-editor, no per-frame GC from spawning, no
hot-path logging, capped voices, tunable SO config, fade-out on despawn, and a
documented perf baseline.

## Profiling baseline — captured 2026-07-29 (in-editor, SampleScene, steady state ~100 objects)
| Metric | Baseline | Target after Stage 1 |
|---|---|---|
| GC Alloc / frame (`PlayerLoop`) | **12.9 KB** | ~0 KB (pooling) |
| Game-loop CPU (`PlayerLoop`) | ~0.17 ms (trivial; frame dominated by EditorLoop overhead) | stay flat |
| Playing audio voices | **47** (99 total sources, 52 paused, 6 clips) | ~16–20 (voice mgmt) |
| Total Audio CPU / DSP | 4.4% / 3.2% | lower with voice cap |

**Read:** frame time is fine; the real costs are **per-frame GC (the spawn churn)** and
**voice count**. These two numbers are the yardstick for 1.2 (pooling) and 1.4 (voice mgmt).
