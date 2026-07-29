# Earth Project — Development Roadmap

*An immersive, procedurally-generated spatial-audio nature walk.*

This `docs/` folder holds the project's vision, brainstorming, and staged plan. Each
stage has a **code name** (to keep goals separable from storage) and its own folder
with substages and design notes. Game files live in the same repo under `Assets/`;
docs live here so they're versioned alongside the game without cluttering Unity.

## Vision (short)
Walk through a living, procedurally-generated natural world and hear it in full 3D —
birds, wind, water, wildlife spatialized around you. Long-term: a seamless phone app
that uses your real movement + head-tracking headphones to simulate walking through
multiple realistic biomes in augmented audio. Full concept: `00_VISION_and_Brainstorm.md`.

## Stages at a glance
- **Stage 1 — BEDROCK** — Optimize the base scripts & core functionality (clean, performant foundation).
- **Stage 2 — CANOPY** — Increase realism: diversify spawning mechanics + audio variety.
- **Stage 3 — FLEDGE** — Port from the Unity editor to a cross-platform mobile app (any phone).
- **Stage 4 — WAYFINDER** — Make the phone's motion, orientation, speed & GPS the movement controller in 3D space.
- **Final — BIOSPHERE** — Seamless app + head-tracking headphones for orientation-aware walks through multiple 3D biomes: the fullest augmented-audio world.

## Guiding principles
1. **Audio is the product.** Every stage is judged by how believable the soundscape feels.
2. **Build for mobile from day one.** Pooling, voice limits, and a swappable spatializer now save pain later.
3. **One system at a time.** Ship a working slice each substage; keep `main` runnable.

## How to use these docs
- Each `StageX_CODENAME/README.md` lists the goal, substages (tickets), and notes.
- Work substages top-to-bottom; check them off in each stage file.
- Status legend: ☐ not started · ◐ in progress · ☑ done
