# Tuning Reference — every adjustable knob

_Living doc: add a row whenever a new tunable value is introduced. High = what it does for
the experience; Low = what it changes in code._

## SpawnConfig asset (`Assets/SpawnConfig.asset` → WorldGenerator ▸ ItemSpawner ▸ Config)

| Knob | High level | Low level |
|---|---|---|
| `itemMax` | How busy the world feels — the size of the sound-object field around you. | Max pooled objects alive at once; `itemsToSpawn = itemMax − active`. |
| `itemChance` | Base likelihood any given spot sprouts a sound. | Per-attempt percent threshold vs `Random.Range(0,100)`. |
| `spawnRadius` | How far out the world keeps generating (beyond earshot). | Half-size of the spawn grid centered on the player (`length`). |
| `rndTimeMin` / `rndTimeMax` | How long a given sound lingers before moving on. | Random lifetime seconds per object before despawn. |
| `maxVoices` | Density of the *audible* soundscape; mobile safety ceiling. | Max simultaneously-playing `AudioSource`s (nearest win). |
| `fadeInDuration` | Whether sounds pop in or ease in. | Envelope rise time (s); kept short to preserve call attacks. |
| `fadeOutDuration` | How gently sounds trail off vs cut. | Envelope fall time (s) on despawn/voice-cull. |
| `minDistance` | The "right next to me" zone of full loudness. | AudioSource `minDistance` (no attenuation within it). |
| `audibleRadius` | How big your bubble of hearing is; sets the sense of depth. | AudioSource `maxDistance`; keep < `spawnRadius` so sounds fade in as you approach. |
| `playerExclusionRadius` | The hush right around you — animals keep their distance. | No discrete spawns within this horizontal distance of the player. |
| `densityContrast` | Clumps-and-clearings vs even spread (realism of patchiness). | Blends spawn probability toward a Perlin field; 0 = uniform, 1 = strong clustering. |
| `densityScale` | Size of the lively/quiet pockets you walk through. | World-unit scale of the density Perlin noise (bigger = broader pockets). |
| `spawnIntervalMin`/`Max` | Pacing — how often new sounds appear; higher = calmer with more lulls. | Random seconds between paced single spawns (one per interval, up to itemMax). |

## Player (Cubie component on the player object)

| Knob | High level | Low level |
|---|---|---|
| `playerSpeed` | Walking pace. | Multiplier on movement input → Rigidbody velocity. |
| `mouseSens` | Look/turn speed (stand-in for real heading later). | Multiplier on look input for yaw/pitch. |

## Per-clip (ItemAudioManager on the Item prefab)

| Knob | High level | Low level |
|---|---|---|
| `audioClips[].weight` | How often each specific sound is picked relative to the others. | Weight in the cumulative weighted-random `SelectRandomClip`. |

## SoundProfile assets (`Assets/SoundProfiles/*` — one per species, listed in `Forest`)

| Knob | High level | Low level |
|---|---|---|
| `clips[]` + weights | The sound(s) this species makes, and how often each variant plays. | Weighted-random clip set handed to the spawned `ItemAudioManager`. |
| `spawnWeight` | Rarity — how common this species is vs the others. | Weight in the spawner's rarity table when choosing what to spawn. |
| `maxConcurrent` | How many of this species can call at once (a chorus vs a lone bird). | Hard cap on simultaneous live instances of the profile. |
| `minHeight`/`maxHeight` | Where in space it lives — birds overhead, rustles at ground. | Random Y offset band above ground at spawn. |
| `minDistance`/`audibleRadius` | How near/far this species can be heard (a crow carries, a rustle doesn't). | Per-source 3D rolloff distances, set at spawn from the profile. |
| `lifetimeMin`/`lifetimeMax` | How long a call/event lasts before it moves on. | Random despawn time per instance. |
| `layer` | Whether it's a discrete event or part of the ambient floor. | `Bed` profiles are skipped by discrete spawning (handled by 2.3). |

## BiomeProfileSet asset (`Forest` → ItemSpawner ▸ Biome)

| Knob | High level | Low level |
|---|---|---|
| `profiles[]` | The full cast of species for this biome. | Array the spawner's rarity table draws from; add a species by adding an entry. |
| `bedClip` | The continuous ambient floor (wind/insects) for this biome. | Clip the `AmbientBed` crossfade-loops; empty = no bed. |
| `bedVolume` | How loud the floor sits under the discrete sounds. | AmbientBed target volume (2D). |
| `bedCrossfade` | Seamlessness of the loop. | Crossfade seconds at the loop point. |

## VoiceManager (auto-created at runtime)

| Knob | High level | Low level |
|---|---|---|
| `evaluateInterval` | How responsively the mix re-focuses as you move. | Seconds between distance re-ranking passes (default 0.25). |
| `maxVoices` | (mirrors SpawnConfig) audible-voice ceiling. | Set from `SpawnConfig.maxVoices` in `ItemSpawner.ApplyConfig`. |

## AudioSource (Item prefab, mostly config-driven now)

| Knob | High level | Low level |
|---|---|---|
| Rolloff curve | Shape of how loudness falls with distance. | `rolloffMode`/custom curve; min/max distances come from SpawnConfig at spawn. |
| `Spatialize` | Whether directional 3D processing is applied. | Routes the source through the spatializer plugin; `spatialBlend` pinned to 1 in code. |
