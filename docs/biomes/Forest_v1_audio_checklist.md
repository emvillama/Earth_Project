# Forest v1 — Audio Acquisition Checklist

Clips to source for the Forest spawn pool, derived from the ~25 profiles in
`Forest_v1.md`. Priority = spawn rarity (get Commons first — they carry the soundscape).

**Legend:** ✅ already have · ⬜ need · 🎙️ great candidate to record yourself (it's literally
the forest behind your house — most authentic option, zero licensing).

**Good free sources:** [xeno-canto.org](https://xeno-canto.org) (bird calls, CC-licensed),
[Macaulay Library](https://www.macaulaylibrary.org) (Cornell, huge bird archive),
[freesound.org](https://freesound.org) (ambient/mammals/weather — check each license).
Prefer clean, isolated recordings; we're already fighting background hiss on at least one clip.

## Layer: BED (continuous floor — highest realism impact, currently MISSING entirely)
- ⬜ 🎙️ Canopy wind (looping, seamless) — the single most important missing sound
- ✅ Cricket / cicada bed (`Crickets.wav`) — have; may want a cleaner/longer loop
- ⬜ 🎙️ Optional: distant stream/water bed (if we add that feature)

## Layer: EVENT — Common (weight ~10, get these first)
- ✅ Blue Jay (`BlueJay.wav`)
- ✅ Dark-eyed Junco (`Junco.wav`) — fits NJ forest
- ✅ Sparrow (`Sparrow.wav`)
- ⬜ 🎙️ American Robin
- ⬜ 🎙️ Northern Cardinal
- ⬜ Black-capped Chickadee
- ⬜ Tufted Titmouse
- ⬜ 🎙️ Gray Squirrel (rustle + bark/scold)
- ⬜ 🎙️ Chipmunk (chip call + leaf rustle)

## Layer: EVENT — Uncommon (weight ~4)
- ✅ Woodpecker (`woodpecker.wav`) — Downy/Red-bellied drum + call
- ⬜ Mourning Dove
- ⬜ Carolina Wren
- ⬜ White-breasted Nuthatch
- ⬜ Wood Thrush (ethereal, dawn/dusk)
- ⬜ American Crow (distant)
- ⬜ Eastern Towhee

## Layer: RARE (weight ~1, high impact)
- ⬜ Barred / Great Horned Owl (night)
- ⬜ Pileated Woodpecker (loud, dramatic)
- ⬜ Red-tailed Hawk (screech)
- ⬜ 🎙️ White-tailed Deer (footfall / snort)
- ⬜ Red Fox (bark, night)
- ⬜ Coyote (night, seasonal)
- ⬜ Distant thunder (weather-gated)
- ⬜ Falling branch / tree creak (high-wind)

## Notes
- **Have (5):** Blue Jay, Junco, Sparrow, Woodpecker, Crickets → the shippable v1 core.
- **Biggest gap:** the BED layer (wind) — nothing plays continuously today.
- Record-your-own candidates (🎙️) will beat any library clip for authenticity since this
  biome IS your backyard. A decent handheld recorder or even a phone on a calm morning gets
  the wind bed + commons.
- As clips come in, drop them in the environment folders (see `docs/audio/Sound_Library_Structure.md`)
  and we'll wire them into the per-profile ScriptableObject table in Stage 2 (CANOPY).
