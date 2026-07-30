# Sound Library — Folder Structure

## The convention (environment-first, for scaling to many biomes)

New home for shippable clips, organized by environment then by audio role:

```
Assets/Sound Library/Environments/
  Forest/
    AmbientBeds/   ← continuous loops (wind, insect bed, water)
    Birds/         ← EVENT bird calls
    Insects/       ← cricket/cicada one-shots (non-bed)
    Mammals/       ← squirrel, chipmunk, deer, fox, coyote
    Weather/       ← thunder, rain, wind gusts, branch falls
    RareEvents/    ← owl, hawk, pileated, high-impact one-shots
  <NextEnvironment>/   ← copy Forest's subfolder layout (e.g. Wetland, Meadow, Coast)
```

To add a new environment later: duplicate the `Forest/` subfolder layout under a new
environment name. Each environment's clips then map to that biome's ScriptableObject
profile table (Stage 2 / CANOPY).

## Current state (2026-07-29) — for reference, migrate gradually

- **Runtime pool:** `Assets/Resources/AudioClips/` — BlueJay, Crickets, Junco, Sparrow,
  woodpecker (the 5 clips actually spawning today). Leave these until we move clip
  references into ScriptableObjects, so we don't break the prefab's assigned array.
- **Legacy library:** `Assets/Sound Library/{STEREO,MONO,UNFINISHED,Test}` — raw
  recordings, duplicates, and scratch files. `Test/` contains non-project junk (a music
  remix, a foghorn) that should not ship — safe to delete or move out of Assets.

## Migration notes (do in-editor to preserve references)
- Move/rename audio **inside the Unity Project window**, not on disk — Unity keeps the
  `.meta` GUID so any prefab/ScriptableObject references survive. Renaming on disk breaks them.
- New empty folders created outside Unity will get `.meta` files auto-generated the next
  time Unity has focus; that's normal, just commit them.
