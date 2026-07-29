# Stage 3 — FLEDGE
**Goal:** Leave the Unity editor — ship a real cross-platform phone app (iOS + Android)
that runs on any modern device.

## Substages
- ☐ **3.1 Platform setup.** Configure Android + iOS build targets, player settings,
  min SDK/iOS version, orientation, icons.
- ☐ **3.2 Mobile performance pass.** Enforce pooling (1.2) + voice caps (1.4); quality
  tiers; profile on real devices for frame rate, battery, and thermals.
- ☐ **3.3 ⚠️ Swap the spatializer.** **Google Resonance is deprecated** and has no
  Apple-Silicon/modern mobile support (we already hit this). Evaluate + migrate to a
  maintained, headphone-HRTF-capable option (**Steam Audio**, Unity's built-in
  spatializer, or platform APIs). Doing this now de-risks Stage 4 + Final.
- ☐ **3.4 Interim touch controls.** On-screen joystick / tap-to-move as a placeholder
  controller until sensor control lands in Stage 4.
- ☐ **3.5 App shell & UX.** Minimal menu, scene/biome select, headphone prompt,
  microphone/motion/location permission handling.
- ☐ **3.6 Build pipeline.** Signing, TestFlight + Play internal testing, fast on-device
  deploy loop.

**Exit criteria:** installable build running the CANOPY soundscape on a real iPhone and
Android phone, at stable frame rate, with a maintained spatializer.
