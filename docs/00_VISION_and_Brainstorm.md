# Vision & Brainstorm — Earth Project

## The concept
A first-person experience where you explore an endless, procedurally-generated
natural world and experience it as a **living 3D soundscape**. The visuals are
secondary; the point is immersive, directional, spatialized audio — birdsong, wind,
water, and wildlife positioned convincingly around you. The endgame is a phone app
that turns your real-world movement and head orientation into the controller, paired
with head-tracking headphones, for a hyper-real walk through multiple biomes.

## What already works (engine baseline)
- **First-person controller** (`Cubie`) — mouse-look + WASD rigidbody movement.
- **Proximity spawner** (`Voxel`/`GeneratePlane`) — keeps a grid centered on the
  player, spawns up to ~100 sound-objects around you via Perlin noise, despawns them
  out of range or after a short random life, respawns continuously.
- **Per-object audio** (`ItemAudioManager`) — weighted-random clip selection + volume
  by facing/direction to the listener.
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
