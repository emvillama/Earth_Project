# Vision & Brainstorm — Earth Project

## The concept
A first-person experience where you explore an endless, procedurally-generated
natural world and experience it as a **living 3D soundscape**. The visuals are
secondary; the point is immersive, directional, spatialized audio — birdsong, wind,
water, and wildlife positioned convincingly around you. The endgame is a phone app
that turns your real-world movement and head orientation into the controller, paired
with head-tracking headphones, for a hyper-real walk through multiple biomes.

## Design principle — augmented audio over the real world
Earth is **not a virtual world you look at; it's an audio layer over real movement.** The
endgame is walking to work (or anywhere) in the real world while the soundscape generates
around you from your real position and heading (Stage 4 GPS/motion). Decisions this locks
in *now*:
- **Movement is walking. No visual terrain, no collision.** Colliding with a virtual tree
  you can't see while walking a real sidewalk would *break* realism, not add it. Movement
  is unconstrained.
- **The environment adapts around the player**, never the reverse — sources spawn/despawn
  and fade relative to wherever the player is (see the player-exclusion "donut of sound"
  and Perlin-density spawning in BEDROCK 1.3).
- In-editor keyboard/mouse movement is only a **stand-in** for real-world GPS/motion —
  nothing should assume a bounded, collidable 3D level.

## What already works (engine baseline)
- **First-person controller** (`Cubie`) — mouse-look + WASD rigidbody movement.
- **Proximity spawner** (`Voxel`/`GeneratePlane`) — keeps a grid centered on the
  player, spawns up to ~100 sound-objects around you, despawns them out of range or
  after a short random life (5–10s), respawns continuously. *Accuracy note:* horizontal
  placement is uniform-random (`Random.Range`); Perlin noise only sets each object's
  *height*. Perlin-driven spawn **density** (clumps vs clearings) is a future realism
  change, not current behavior.
- **Per-object audio** (`ItemAudioManager`) — weighted-random clip selection
  (`SelectRandomClip`, already implemented) + volume by facing/direction to the listener.
- **Google Resonance Audio SDK** — 3D spatialization.
- **Wildlife sound library** — Blue Jay, Junco, Woodpecker, Sparrow, crickets, ambient.

## Original brainstorm (migrated from the .docx notes)

### Objects to spawn
Birds · Wind/Leaves · River · Deer (noise/call) · Cicadas · Stick snap/crunch

### Bird behavior
- Flapping wings, chirps, singing to other birds; many species & calls.
- **Rarity tiers:** Eagle = Epic · Falcon/Hawk = Rare · Blue Jay/Robin = Common.
- Spawn at head height or higher; overhead flight toward "trees" or "ground".
- Spawn/despawn abruptly; also based on player closeness.

### Wind
- Different strength levels; could depend on weather in a future update.
- Depends on other animals/noises around; leaves carried by wind on occasion.
- Leaf noises closer to the ground group; wind noises closer to head height.

### River / water
- Different strength levels along the riverbed; multiple layered audio sources to
  suggest different rock formations. Fish-splash noises. Player walking in the river
  makes its own sound. When encountered, the noise stays and tracks its location.

### Extra ideas
- A **stationary in-world speaker** that plays your own downloaded music while you
  walk through the forest.
- Use authentic recordings (ref: Cornell/Macaulay "Guide to Bird Sounds — US & Canada").

> Originals preserved in `Assets/Sound Library/Brainstorming Docs/` and copied here for
> versioned reference.

## Refinements to make (captured 2026-07-29)
- **Sound endings:** replace the current instant cutoff (objects `Destroy`ed at end of
  their random life) with a **gradual fade-out** plus a short **reverb/effect tail** so
  sounds don't stop abruptly. The reverb tail also doubles as a strong "I'm in a forest"
  spatial cue. Fade can land as Stage 1 audio hygiene; richer reverb belongs in Stage 2.
- **First biome fully specced:** `docs/biomes/Forest_v1.md` — Hunterdon Co. NJ hardwood
  forest as hard numbers: ~25 audio profiles with rarity weights, audible radii, active
  hours, a concurrency budget, terrain metrics, and time-of-day + weather modifiers.
