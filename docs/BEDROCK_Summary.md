# BEDROCK — Stage 1 Complete
### What we built, and why it matters

_Earth Project · Stage 1 (BEDROCK) · completed 2026-07-30_

BEDROCK was about turning a year-old prototype into a **clean, performant, mobile-ready
foundation** before adding any new content. Below is everything we changed, grouped by
theme, in plain terms.

---

## 0. Getting eyes on the real code
- Re-established access from the Pi to your Mac over Tailscale (the "Pi→Mac bridge") and
  made a local clone, so changes are read from your *actual* source, not guesswork.
- Found the truth: your entire game is **3 gameplay scripts** — the other 50+ were the
  Resonance SDK. Everything below targets those real scripts.

## 1. Performance foundation (the mobile-critical work)
- **Profiling baseline.** Measured the starting point: **12.9 KB of garbage per frame**
  and **47 simultaneous audio voices** — both fine on desktop, both mobile-killers.
- **Object pooling.** The spawner used to create and destroy ~100 objects constantly,
  generating that per-frame garbage. Now it recycles a fixed pool — near-zero allocation.
  *This is the single biggest mobile win.*
- **Voice management.** A new `VoiceManager` caps how many sounds play at once (~20,
  nearest-to-you win) instead of 47. Phones choke past ~32, so this keeps it safe and
  cuts audio CPU roughly in half.
- **Spawn cap 100 → 40.** Most of the 100 objects never became audible before despawning;
  40 keeps a full field around you with far less waste.
- **Killed hot-path logging.** Removed a `Debug.Log` that ran every frame for every
  object — free performance.

## 2. Code cleanup
- **Honest naming.** The core class was misleadingly named `Voxel` in a file called
  `GeneratePlane.cs`; renamed both to **`ItemSpawner`** (references preserved).
- Removed dead code (unused dictionaries, an empty terrain stub, orphaned fields).

## 3. Audio quality
- **Fade in/out.** Sounds used to cut off instantly (clicks/pops). Now they ease in
  fast (~0.05s, preserving crisp bird attacks) and fade out smoothly (~0.25s) on despawn
  and when a distant voice is culled — the soundscape "breathes" instead of snapping.
- **Realism pass — distance, not facing.** The old code turned sounds *down when you
  looked away*, which is unrealistic (real ears don't do that). Removed it entirely;
  loudness now comes purely from **distance** (true 3D rolloff), direction from
  spatialization. A bird behind you is as present as one in front.

## 4. Audio content fixes
- **Noise cleanup.** Your clips had background hiss. Built a noise-gate + denoise pass
  (birds gated, continuous textures like crickets denoise-only) and cleaned them.
- **Fixed the wrong-clips bug.** The game was actually loading **UNFINISHED mp3s and a
  junk file** from the Test folder — not your finished clips. That was the persistent
  white noise. Repointed the prefab to the finished, cleaned set.
- **Removed Resonance.** The Resonance Audio SDK was unused (your spatializer is Meta XR
  Audio, zero references to Resonance) and only throwing deprecation warnings. Deleted it
  — script count dropped from 55 to 4.

## 5. Spatial design — "the donut of sound"
- **Decoupled two radii:** things now *spawn* out to 150 units but are only *audible*
  within ~80. So sounds **emerge ahead of you and fade behind** as you move — the world
  continues past your earshot.
- **Player exclusion radius (~10).** Nothing spawns right on top of you — animals keep
  their distance, so there's a natural hush at your feet.
- **Perlin-density spawning.** Life now **clusters into pockets** (thickets vs clearings)
  instead of spreading evenly — you walk through lively patches and quiet stretches, the
  way real woods actually sound. Currently gentle by default; we'll crank it up once the
  ambient wind bed exists to fill the quiet.

## 6. Tunability & future-proofing
- **`SpawnConfig` asset.** Every knob — spawn cap, chance, radius, lifetime, voices,
  fades, distances, exclusion, density — now lives on **one asset you tweak live**, no
  code edits. (See `Tuning_Reference.md` for what each does.)
- **Input abstraction.** The player controller now reads input through an `IInputSource`
  interface. Today it's keyboard/mouse; in Stage 4 a GPS/motion source drops in with zero
  controller changes — the groundwork for "walk to work while listening."
- **Locked design principle:** Earth is **augmented audio over real movement** — walking,
  no virtual terrain, no collision; the environment adapts around *you*.

## 7. Automation & docs
- **Nightly audio auto-cleaner.** New clips dropped into the environment folders get
  noise-cleaned automatically (idempotent, backs up originals). Runs nightly + on demand.
- **Living docs:** full roadmap, the Hunterdon-forest audio spec + acquisition checklist,
  environment folder structure, and the tuning reference — all versioned alongside the code.

---

## By the numbers
| | Before | After |
|---|---|---|
| Garbage / frame | 12.9 KB | ~0 (pooled) |
| Simultaneous voices | 47 | ~20 (managed) |
| Gameplay scripts | 3 real + 52 SDK cruft | 7 clean, purpose-built |
| Audio | hissy, wrong clips, instant cutoffs, fake ducking | cleaned, faded, distance-realistic |
| Tuning | hardcoded constants | one live config asset |

**Bottom line:** the foundation went from a desktop-only prototype that would stutter and
hiss on a phone, to a clean, efficient, realistic, data-driven base ready for content.
Next stop: **CANOPY** — diversifying the soundscape (birds, wind, water) toward
indistinguishable-from-real.
