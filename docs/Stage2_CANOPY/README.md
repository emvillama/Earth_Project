# Stage 2 — CANOPY
**Goal:** Bring the forest to life. Diversify *what* spawns and *how it sounds* so the
world feels real, not like one repeating clip.

## Substages
- ☐ **2.0 Audio library acquisition.** Gather the forest clip set — see
  `docs/biomes/Forest_v1_audio_checklist.md` (have 5, need ~20). Plan: a long-haul
  download session pulling **CC0 / CC-BY** clips (xeno-canto, freesound, Sonniss bundle)
  rather than self-recording (mic constraints). Drop clips into
  `Assets/Sound Library/Environments/Forest/*`; the nightly cleaner auto-formats them.
  Long-lead + gating for everything below — run it in the background starting now.
- ☑ **2.1 Data-driven SoundObject system.** `SoundProfile` SO per species (clips, rarity
  weight, maxConcurrent, height band, distance, lifetime, layer) + `BiomeProfileSet`
  (`Forest`). ItemSpawner rolls the rarity table, respects per-profile caps, and
  configures each spawn from its profile. Authored BlueJay/Junco/Sparrow/Woodpecker.
  Adding a species = author an asset. (Assign `Forest` to ItemSpawner ▸ Biome to activate;
  unassigned = fallback.)
- ☐ **2.2 Bird system.** Species + **rarity tiers** (Common: Jay/Robin · Rare:
  Hawk/Falcon · Epic: Eagle). Spawn overhead, flight toward "trees"/ground, and
  behaviors: chirp, song, call-and-response between birds.
- ☐ **2.3 Wind & leaves.** Continuous ambient layer with variable strength; wind near
  head height, leaves near ground; fade in/out (no abrupt cut). Weather hook for later.
- ☐ **2.4 River / water.** Multiple layered river sources by "strength," fish splashes,
  player-in-water footsteps, location-anchored (stays put once encountered).
- ☐ **2.5 Player footsteps & foliage SFX.** Player movement drives sound: **leaf
  crunch** underfoot, occasional **stick/branch snap**, over terrain. Makes the player's
  own movement audibly part of the 3D space (pairs with 2.8). Plus cicadas / distant deer
  calls as occasional events.
- ☐ **2.6 Variety & anti-repetition.** Expand weighted selection; forbid immediate
  repeats; randomize pitch/gain slightly per play for naturalism.
- ☐ **2.7 Spatial mix polish.** Tune distance attenuation + reverb per area so space
  feels believable (pairs with the Stage 3 spatializer choice).
- ☐ **2.8 Wildlife awareness of the player.** Animals react to you: discrete vocal
  creatures (birds, mammals) **hush or flee** — optionally with a startle rustle — when
  you come within a per-species *wary radius*; insects/wind ignore you. Makes moving
  through the woods feel like the world notices you. (A simple no-spawn **player
  exclusion radius** lands earlier, in BEDROCK 1.3, as the foundation for this.)

## Checkpoints (build order)

Substages are numbered by topic; this is the order we actually build them, grouped into
shippable checkpoints. **2.0 (audio haul) runs in the background the whole time.**

- **C1 — Data foundation + living floor** → 2.1 + 2.3
  Build the SoundObject SO system (per-species data) and the continuous wind/insect BED
  layer. *Why first:* everything plugs into the data system, and the bed is what lets us
  push Perlin density up without dead-silent patches.
  *Done when:* spawns are driven by SO definitions and an always-on ambient floor plays under everything.

- **C2 — Birdlife** → 2.2 + 2.6
  Species with rarity tiers + basic behavior (chirp/song/call-response), plus
  anti-repetition (no immediate repeats, per-play pitch/gain jitter).
  *Done when:* varied, non-repeating birds with rarity read as alive, not looped.

- **C3 — The world reacts to you** → 2.8 + 2.5
  Wildlife awareness (animals hush/flee within a wary radius, startle rustle) and player
  footsteps/foliage (leaf crunch, branch snap).
  *Done when:* your presence and movement audibly change the world around you.

- **C4 — Water + final mix** → 2.4 + 2.7
  Layered river/water (location-anchored) and spatial-mix polish (attenuation + reverb per area).
  *Done when:* a full biome — birds + wind + water — holds up as believable on headphones.

**Exit criteria:** a walk through the scene surfaces varied, non-repeating, believable
directional audio across at least birds + wind + water.
