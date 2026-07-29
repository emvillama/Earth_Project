# Stage 4 — WAYFINDER
**Goal:** The phone *is* the controller. Real-world motion, orientation, speed, and
location drive movement and looking in the 3D space.

## Substages
- ☐ **4.1 Orientation → look.** Gyro + compass drive camera facing (turn your
  body/phone → turn in-world).
- ☐ **4.2 Motion → movement.** Step detection / accelerometer or GPS delta moves the
  avatar (walking IRL → walking in-world). Decide model: pedometer vs GPS vs hybrid.
- ☐ **4.3 GPS → world position.** Map real location/movement to 3D translation, with
  scaling, drift handling, and an indoor/stationary fallback.
- ☐ **4.4 Sensor fusion & smoothing.** Fuse gyro/accel/compass/GPS; filter jitter and
  drift (complementary/Kalman); add a calibration flow.
- ☐ **4.5 Safety & modes.** Because attention is on audio (maybe eyes closed): a
  stay-in-place mode vs walk mode, boundaries, quick pause, "look up" safety.
- ☐ **4.6 Head-tracking bridge.** Abstract the orientation source so a head-worn IMU
  (Final stage) can replace phone-in-pocket cleanly.

**Exit criteria:** you can physically turn and walk (or walk-in-place) and the
soundscape re-localizes correctly around you, hands-free.
