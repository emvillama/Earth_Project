# Stage 2 — CANOPY
**Goal:** Bring the forest to life. Diversify *what* spawns and *how it sounds* so the
world feels real, not like one repeating clip.

## Substages
- ☐ **2.1 Data-driven SoundObject system.** ScriptableObject definitions per object
  type (clips, weights, spawn rules, lifetime, movement, height band). Everything below
  becomes data, not new classes.
- ☐ **2.2 Bird system.** Species + **rarity tiers** (Common: Jay/Robin · Rare:
  Hawk/Falcon · Epic: Eagle). Spawn overhead, flight toward "trees"/ground, and
  behaviors: chirp, song, call-and-response between birds.
- ☐ **2.3 Wind & leaves.** Continuous ambient layer with variable strength; wind near
  head height, leaves near ground; fade in/out (no abrupt cut). Weather hook for later.
- ☐ **2.4 River / water.** Multiple layered river sources by "strength," fish splashes,
  player-in-water footsteps, location-anchored (stays put once encountered).
- ☐ **2.5 Ground & foliage SFX.** Stick snaps / crunch tied to player movement over
  terrain; cicadas, deer calls as occasional distant events.
- ☐ **2.6 Variety & anti-repetition.** Expand weighted selection; forbid immediate
  repeats; randomize pitch/gain slightly per play for naturalism.
- ☐ **2.7 Spatial mix polish.** Tune Resonance distance attenuation, reverb probes/
  rooms per area so space feels believable.

**Exit criteria:** a walk through the scene surfaces varied, non-repeating, believable
directional audio across at least birds + wind + water.
