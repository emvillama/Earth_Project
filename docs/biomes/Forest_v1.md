# Forest v1 — Biome Spec (Hunterdon County, NJ hardwood)

_Biome: Eastern deciduous, oak–hickory–maple–beech, piedmont edge. Reference: the forest
behind Harry's home, Hunterdon Co. NJ. Drafted 2026-07-29. Numbers are v1 targets, tune on-device._

---

## 1. Concurrency budget (voices running at once)

| Layer | What it is | Typical active | Notes |
|---|---|---|---|
| BED (floor) | continuous ambient (wind canopy, insect drone) | 1–2 | always on, wide/non-spatialized |
| EVENT | localizable point sources (birds, ground animals) | 4–8 | placed sound-objects, spatialized |
| RARE | occasional high-impact | 0–1 | owl, deer, thunder |
| **Global cap** | mobile HRTF voice ceiling | **16–20 max** | typical steady state 6–10 |

Today the build caps *object* count at `itemMax = 100` — a crude global limiter. This budget
replaces that single number with layer/voice-aware limits.

---

## 2. Audio profile table (the loot table)

Weight = relative spawn probability. Radius = audible/attenuation distance. Hours = active
window (local sim time). Max = max concurrent instances of this profile.

### Common (weight 10)
| Profile | Layer | Weight | Radius (m) | Active hours | Max | Notes |
|---|---|---|---|---|---|---|
| American Robin | EVENT | 10 | 120 | 05:00–20:00 | 2 | peaks dawn/dusk |
| Northern Cardinal | EVENT | 10 | 100 | 05:30–20:00 | 2 | edge-favoring |
| Blue Jay | EVENT | 9 | 150 | 06:00–19:00 | 2 | loud, periodic |
| Black-capped Chickadee | EVENT | 10 | 60 | 06:00–19:00 | 3 | |
| Tufted Titmouse | EVENT | 9 | 70 | 06:00–19:00 | 2 | |
| Gray Squirrel (rustle/bark) | EVENT-ground | 8 | 20 | 06:00–19:00 | 2 | leaf-litter timbre |
| Chipmunk (chip) | EVENT-ground | 8 | 25 | 07:00–18:00 | 2 | |
| Canopy wind | BED | — | global | all (weather-driven) | 1 | intensity from weather |
| Cricket / cicada | BED | — | global | cicada 09–18 / cricket 18–23 | 1 | summer only |

### Uncommon (weight 4)
| Profile | Layer | Weight | Radius (m) | Active hours | Max | Notes |
|---|---|---|---|---|---|---|
| Downy/Red-bellied Woodpecker | EVENT | 4 | 150 | 06:00–18:00 | 1 | drum + call |
| Mourning Dove | EVENT | 4 | 120 | 06:00–19:00 | 2 | |
| Carolina Wren | EVENT | 4 | 90 | 06:00–19:00 | 1 | loud for size |
| White-breasted Nuthatch | EVENT | 4 | 70 | 06:00–18:00 | 1 | |
| Wood Thrush | EVENT | 4 | 130 | 05:00–07:00, 18:30–20:30 | 1 | ethereal, dawn/dusk |
| Crow (distant) | EVENT | 4 | 300 | 06:00–19:00 | 2 | |
| Eastern Towhee | EVENT | 3 | 80 | 06:00–18:00 | 1 | |

### Rare (weight 1, high impact)
| Profile | Layer | Weight | Radius (m) | Active hours | Max | Notes |
|---|---|---|---|---|---|---|
| Barred / Great Horned Owl | RARE | 1 | 200 | 20:00–05:00 | 1 | night |
| Pileated Woodpecker | RARE | 1 | 200 | 06:00–18:00 | 1 | loud, dramatic |
| Red-tailed Hawk (screech) | RARE | 1 | 200 | 09:00–17:00 | 1 | |
| White-tailed Deer (footfall/snort) | RARE | 1 | 30 | dawn/dusk + night | 1 | |
| Red Fox (bark) | RARE | 1 | 120 | 20:00–04:00 | 1 | night |
| Coyote | RARE | 1 | 250 | 21:00–04:00 | 1 | seasonal, night |
| Distant thunder | RARE (weather) | — | global | storm only | 1 | |
| Falling branch / tree creak | RARE (weather) | 1 | 50 | high-wind only | 1 | |

**Profile count: ~10 common + 7 uncommon + 8 rare ≈ 25.** Shippable v1 = the 10 commons
(matches the existing sound library: Blue Jay, Junco, Woodpecker, Sparrow, crickets, ambient).

---

## 3. Terrain / geography metrics (→ 3D)

| Metric | Value |
|---|---|
| Canopy height | 18–28 m |
| Trunk density | 150–400 trunks / hectare |
| Avg trunk spacing | 5–8 m (drives occlusion + reflection) |
| Elevation variation | ±10–30 m over walkable area |
| Understory | mountain laurel, spicebush, saplings (muffles low sources) |
| Ground | leaf litter (+ shale/traprock outcrops) |
| Zones | EDGE (near home: open, windy, robins/cardinals) → INTERIOR (quiet, reverberant, thrush/woodpeckers) |
| Optional feature | small stream/drainage → water BED layer |

---

## 4. Time-of-day activity multipliers

| Window | Bird activity | Adds |
|---|---|---|
| Dawn 05:00–07:00 | ×1.5 (dawn chorus) | wood thrush |
| Morning 07:00–11:00 | ×1.0 | |
| Midday 11:00–15:00 | ×0.5 (lull) | cicada bed (summer) |
| Afternoon 15:00–18:00 | ×0.8 | |
| Dusk 18:00–20:00 | ×1.3 | owls begin, wood thrush |
| Night 20:00–05:00 | ×0.1 | owl, fox, coyote, cricket bed |

---

## 5. Weather states (modulate the tables)

| State | Effect |
|---|---|
| Clear | baseline |
| Windy | canopy BED +; EVENT radius ×0.7 (occlusion); enable falling-branch |
| Rain | rain BED on; bird activity ×0.4 |
| Storm | heavy rain BED; enable thunder; bird activity ×0.2 |
| Snow (winter) | muffled/quiet floor; events sparse |

---

## 6. Audio playback fixes (from brainstorm)

- Replace **instant cutoff** (current: `Destroy` at end of random life) with **gradual
  fade-out** (envelope release).
- Add **reverb / tail** on despawn — also a core "I'm in a forest" cue.
- Target HRTF backend: **Steam Audio** (Resonance deprecated).

---

## 7. Current build vs needed — verified against code (2026-07-29)

Gameplay logic is **3 scripts**: `GeneratePlane.cs` (class `Voxel`, spawn manager),
`ItemAudioManager.cs` (per-object audio), `Cubie Code.cs` (player). Everything else is the
Resonance SDK.

### Already present (reuse / extend, don't rebuild)
- **Weighted selection** — `ItemAudioManager.SelectRandomClip()` already does cumulative-weight
  random pick. Gap: it chooses a *clip* on one prefab via Inspector weights; needs to choose a
  *profile/species* from a ScriptableObject table.
- **Global count cap** — `itemMax = 100`. Gap: not per-profile / per-layer / voice-aware.
- **Distance attenuation** — handled by the AudioSource/Resonance 3D rolloff at engine level
  (the script's own volume tweak is *facing-based*, item.forward vs listener — reconsider it).
- **Continuous respawn** — `ManageItems` tops the field back up to the cap each move.

### Not present — must be scripted (net-new)
1. **Profile/species selector from data** (extend `SelectRandomClip` up a level, into an SO table).
2. **Concurrency manager** — per-profile Max + per-layer budget + global voice cap. *Prereq: pooling.*
3. **Time-of-day gate** — no game clock exists.
4. **Occlusion** — raycast listener→source through trunks + low-pass (the one real perf cost).
5. **Continuous BED layer** — no looping ambient floor exists today.
6. **Fade-out + reverb tail on despawn** — `Destroy` is an instant cut. *Prereq: pooling.*
7. **Weather state machine.**
8. **Seasonality modifier.**
9. **Behavioral correlation** (call-and-response) — later.

### Hard prerequisites / cleanup found in code (before the above)
- **Object pooling** — replace the `Instantiate`/`Destroy` churn in `ManageItems`; #2 and #6 depend on it.
- **Delete the per-frame `Debug.Log`** in `ItemAudioManager.Update` (runs ~100×/frame) — free win.
- **Decide spawn model** — placement is uniform-random x,z today; realistic clustering (Perlin
  *density*) is a design change, not existing behavior.
- **Data migration** — `itemMax`, `itemChance`, `rndTime`, `WeightedAudioClip.weight` → ScriptableObjects.
