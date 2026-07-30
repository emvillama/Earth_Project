# Stage 1 — BEDROCK
**Goal:** Clean, correct, and performant foundation before adding content. Nothing
new gets built on shaky base scripts. Everything here also sets up mobile viability.

> **Verified against the actual code 2026-07-29.** Gameplay = 3 scripts only
> (now `ItemSpawner.cs`, `ItemAudioManager.cs`, `Cubie Code.cs`); the rest is the
> Resonance SDK. Notes below mark what already exists so we don't rebuild it.

## Substages
- ☑ **1.0 Quick win — kill hot-path logging.** Removed the per-frame `Debug.Log` from
  `ItemAudioManager.Update`.
- ☑ **1.1 Refactor & rename.** `Voxel`/`GeneratePlane.cs` → `ItemSpawner.cs` (class +
  file, GUID preserved). Removed dead code (`cubePos` dict, `yOffset`, `itemDivider`,
  the stubbed `GenerateTerrain`).
- ☑ **1.2 Object pooling.** Replaced `Instantiate`/`Destroy` churn with
  `UnityEngine.Pool.ObjectPool` (get/reposition on spawn, release on despawn).
- ☐ **1.3 Deterministic, frame-budgeted spawning.** Spawn cap reduced 100→40 (now the
  public `itemMax`, tunable). Still TODO: steady per-frame spawn budget, and the
  **design decision** — keep uniform-random placement or move to Perlin-driven *density*
  (clumps vs clearings). Behavior change, needs Harry's call.
- ☑ **1.4 Audio voice management.** `VoiceManager` caps ~20 concurrent voices,
  distance-priority pause/unpause every 0.25s. (Baseline was 47 voices.)
- ◐ **1.5 Player controller polish.** *Done:* walk speed halved (50→25). *Still TODO:*
  wrap `Cubie`'s direct `Input.GetAxis` behind an interface so Stage 4 (phone sensors)
  can swap it cleanly; ground check / walk-vs-fly / collision — partly design-dependent
  (terrain is currently stubbed, so collision is moot until we decide on ground).
- ☐ **1.6 Config via ScriptableObjects.** Move the hand-typed "pre-load" inputs —
  `itemMax`, `itemChance`, `rndTime`, and `WeightedAudioClip.weight` — plus spawn
  density/radius/lifetimes into SO configs for fast iteration. **This is the handoff into
  CANOPY.**
- ☑ **1.7 Profiling baseline.** Recorded 2026-07-29 (see table below): 12.9 KB/frame GC,
  47 voices — the yardstick for later stages.
- ☐ **1.8 Audio hygiene — fade-out.** Replace the instant despawn cutoff with a gradual
  fade-out (envelope release), and fade the voice-manager's cull pause/unpause so sounds
  ease in/out instead of snapping. Rides on 1.2's despawn path. Richer reverb tail is
  Stage 2. **← next up.**

**Progress (2026-07-30):** 1.0, 1.1, 1.2, 1.4, 1.7 done + pushed. Bonus done: all 5
runtime clips noise-gated/denoised + a nightly auto-clean pipeline. **Remaining: 1.3**
(needs the spawn-model design call), **1.5** (input abstraction + ground/collision,
partly design-dependent), **1.6** (ScriptableObject config), **1.8** (fade-out — next).

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
