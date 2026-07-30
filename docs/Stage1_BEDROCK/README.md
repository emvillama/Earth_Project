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
- ☑ **1.3 Spawn model.** Spawn cap 100→40; **player exclusion radius** (10) + **decoupled
  audible radius** (80 < spawnRadius 150, so sounds fade in as you approach); **Perlin-
  driven density** (`densityContrast`/`densityScale`) so life clusters into pockets
  instead of spreading evenly. (Optional steady per-frame spawn budget deferred — not
  needed at current scale.)
- ☑ **1.4 Audio voice management.** `VoiceManager` caps ~20 concurrent voices,
  distance-priority pause/unpause every 0.25s. (Baseline was 47 voices.)
- ☑ **1.5 Player controller / input.** Movement = **walking**; **no terrain/collision**
  (non-goals per VISION principle). Speed 50→25. Input wrapped behind `IInputSource`
  (`KeyboardMouseInputSource` now, auto-added; Stage 4 swaps in a GPS/motion source with
  zero controller changes).
- ☑ **1.6 Config via ScriptableObjects.** `SpawnConfig` SO + `SpawnConfig.asset` hold
  every knob: itemMax, itemChance, spawnRadius, rndTime range, maxVoices, fade in/out,
  min/audible distance, exclusion radius. `ItemSpawner` reads it (falls back to defaults
  if unassigned). Drag the asset onto WorldGenerator→ItemSpawner to tune live.
- ☑ **1.7 Profiling baseline.** Recorded 2026-07-29 (see table below): 12.9 KB/frame GC,
  47 voices — the yardstick for later stages.
- ☑ **1.8 Audio hygiene — fade-out.** `ItemAudioManager` envelope: fast fade-in (0.05s,
  preserves attacks) + smooth fade-out (0.25s) on despawn and voice-cull. Confirmed good.

**Progress (2026-07-30):** 1.0, 1.1, 1.2, 1.4, 1.6, 1.7, 1.8 done + pushed; 1.3 mostly
done. Bonus: audio clips noise-gated/denoised + nightly auto-clean pipeline; **fixed Item
prefab loading UNFINISHED/junk clips**; **removed unused Resonance SDK**; **realism pass**
(dropped fake facing-volume — loudness is distance now); **player exclusion + audible
radius** ("donut of sound" that travels with you). **All substages done — BEDROCK COMPLETE (2026-07-30).**
Next: Stage 2 CANOPY (2.0 audio-library haul is the long-lead starter). Every tunable
value is catalogued in `docs/Tuning_Reference.md`.

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
