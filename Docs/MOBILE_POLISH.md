# Mobile visual polish and 60 FPS budget

## The target, not a guarantee

“Perfect” mobile graphics must be defined per device tier. Velocity Rush targets a polished **60 FPS on profiled mid-range devices** with a crisp car, readable track, subtle post effects and stable frame pacing—not desktop visual settings forced onto a phone. Lock the supported-device matrix, make a release IL2CPP build, and profile the worst race view before approving a visual feature.

The bootstrap now creates a mobile-oriented URP Volume, procedural skybox, one mixed sun, a baked 128px reflection probe, post-enabled race camera, `MobileGraphicsController`, and `CinematicTimeController`. Generated values are starting values; bake all final scenes and profile them.

## Included polish scripts

| Script | Purpose | Attach / configure |
| --- | --- | --- |
| `Polish/MobileGraphicsController.cs` | Conservative dynamic render scale with a 60 FPS target and hysteresis. | Persistent services; set min/max scale per device tier. |
| `Polish/TimeOfDayWeatherController.cs` | Coarse-interval sun, ambient, sky exposure, fog and optional single rain particle/audio system. | One environment root per scene. Keep one mixed/baked sun. |
| `Polish/CarVisualPolish.cs` | URP/Lit metallic/smoothness/clear-coat property blocks plus baked-probe/light-probe usage. | Car root; assign only car-body renderers for best results. |
| `Polish/CinematicTimeController.cs` | Single owner for unscaled-time slow motion. | Persistent services; do not set `Time.timeScale` from other gameplay scripts. |
| `Polish/CarCinematicPolish.cs` | Slow motion on large landings and occasional nearby opponent pass. | Player car only; use conservative cooldowns. |
| `Polish/CameraPolishController.cs` | Post-follow speed/crash micro-shake. | Race camera, after `RaceCameraController`. |
| `Polish/CanvasPanelAnimator.cs` | Dependency-free CanvasGroup fade/scale panel animation. | A Canvas panel with CanvasGroup. |
| `Audio/MusicLoopController.cs` | One-source loop playlist. | Persistent AudioSource; assign optimized music loops. |
| `Progression/LocalLeaderboardService.cs` | PlayerPrefs top-ten fallback for time/distance boards. | Added by `GameManager`; replace with a UGS adapter for online boards. |

## URP recipe for a polished mobile look

### Renderer / lighting

- **Main light:** one Mixed directional sun, soft shadows, 1–2 cascades, 1024px shadow map on mid tier. Bake static buildings/roads/props and add light probes for cars.
- **Additional lights:** zero or one per-vertex additional light on mid tier. Use emissive materials for signs/neon; never rely on dozens of realtime point lights.
- **Reflections:** one or a few **baked** Reflection Probes (128–256 px) with Blend Probes on car renderers. Never use a realtime probe for every car.
- **Sky:** the generated procedural skybox is nearly free. For final art use a compressed cubemap (typically 512–1024) rather than a giant HDR panorama.
- **Time/weather:** update global values at 4–5 Hz, not every frame. Rain is one capped particle emitter near the camera; avoid particle collision, per-particle lights and screen-space water effects on mid tier.

### Post-processing

The generated `VP_MobilePolish` uses:

| Effect | Starting setting | Mobile rule |
| --- | --- | --- |
| Bloom | Intensity `0.18`, threshold `1.15` | Light bloom only; it should shape headlights/neon, not wash out the UI. |
| Color adjustments | Contrast `+4`, saturation `+3` | Grade in LUT/art direction after lighting bake. |
| Motion blur | Intensity `0.08`, clamp `0.03` | Keep subtle and disable on low tier or if it requires costly depth/opaque textures. |

Avoid depth of field, screen-space reflections, volumetric fog, heavy SSAO, realtime contact shadows, full-res film grain and multiple fullscreen passes by default. Enable them only after a captured device frame proves the budget.

## Car materials, particles and camera

### URP car paint

1. Use **URP/Lit**, enable clear coat only on body panels, and keep tyre/glass/paint as separate materials if their surface response differs.
2. Use a 1K–2K hero car texture only when it survives memory profiling; 1K is normally enough on a phone. Use ASTC 6×6/8×8.
3. Add `CarVisualPolish` and assign body renderers. It uses `MaterialPropertyBlock`, so six cars do not create six material copies.
4. Place baked Reflection Probes around garages/tunnels/city blocks, plus Light Probe Groups along tracks. Car renderers are configured for probe blending.

### VFX budget

`CarEffectsController` supports **exhaust, dust/drift, sparks and nitro** particle references. Configure these as GPU-friendly, world-space systems:

- Exhaust: 8–16 maximum particles, low rate, no collision/light.
- Drift dust: 32–64 max particles, enabled only on rear-wheel drift.
- Sparks: a short burst on impact; pool it and cap particles.
- Nitro: 20–40 particles, additive material, no light module.
- Disable all car effects and distant AI audio outside a camera distance budget. Never instantiate particles in `Update`.

### Camera / slow motion

- `RaceCameraController` owns chase/cinematic follow and speed FOV.
- `CameraPolishController` adds a small post-follow shake. Keep max speed shake below `0.05 m`; motion sickness is a greater risk than a lack of shake.
- `CarCinematicPolish` triggers only after a large airborne landing or a cooldown-limited near miss. Slow-mo uses unscaled restoration through `CinematicTimeController`, so physics timestep is adjusted safely.
- Test every slow-mo interaction with pause, results, restart and app suspend/resume flows.

## Modern UI without a hard tween dependency

The generated UGUI HUD remains intentionally lightweight. Add `CanvasPanelAnimator` to panels and call `Show`, `Hide`, or `Toggle` from buttons/menu controllers for unscaled CanvasGroup fade/scale animation.

For a production menu that needs sequences, spring transitions and analytics-friendly state handling:

1. Import **DOTween** or **LeanTween** only after license/package review.
2. Wrap calls in a project `MenuTransitionService`; do not scatter tween calls through all buttons.
3. Kill/reuse tweens when panels close, animate `CanvasGroup`/`RectTransform` rather than layout rebuild-heavy hierarchies, and use unscaled time for menus.
4. Keep one Canvas for static HUD and separate canvases for frequently animated panels to limit rebuilds.

## Progression and leaderboards

- `ProgressionService` now supports per-car Engine, Handling and Nitro upgrades purchased with coins. `CarController` reads local upgrades on spawn and applies multipliers without mutating ScriptableObject assets.
- Existing car unlocks, coins, campaign stars and time records remain PlayerPrefs-backed for offline prototypes.
- `LocalLeaderboardService` stores top-ten local time boards (`time.<trackId>`, lower is better) and Endless distance (`endless.distance`, higher is better).
- For online leaderboards, add a narrow adapter around **Unity Gaming Services Authentication + Leaderboards**:
  1. Initialize UGS after consent/privacy flow.
  2. Sign in anonymously or through a platform account.
  3. Submit only validated finished-race time/distance values.
  4. Load around-player/top-N pages asynchronously and retain `LocalLeaderboardService` as offline fallback.
  5. Never place service secrets in the client or trust client-reported purchase/rank data for competitive rewards.

## Audio

- Use `MusicLoopController` with loop-ready background tracks. Long music should stream; short engine/impact SFX should be compressed in memory and usually mono.
- `CarAudioController` already maps engine pitch to RPM, tyre volume to slip, and nitro to boost state.
- Use an AudioMixer in final production: music/SFX/ambient groups, a pause snapshot, low-pass for menus/tunnels, and a voice limit for distant AI.
- Do not run an engine loop for every off-screen opponent. Prioritize player, nearest AI, then ambience.

## Full optimisation guide

### Resolution and frame pacing

| Device tier | Target FPS | URP render scale | MSAA | Notes |
| --- | ---: | ---: | ---: | --- |
| Low | 30 | 0.75–0.85 | Off/2× | Disable motion blur and most post; reduce shadows/LOD. |
| Mid | 60 | 0.90–1.00 | 2× | Main Velocity Rush target; light bloom, one sun, baked world. |
| High | 60 | 1.00 | 2×/4× only after profile | Optional color grade and subtle motion blur. |

`MobileGraphicsController` changes scale by `0.05` only after a multi-second sample and uses hysteresis. Do not use it as a substitute for correct content budgets. Test with thermal throttling, not a cold 30-second run.

### Texture memory / compression

- Android: ASTC 6×6 or 8×8, ETC2 fallback where audience requires it. iOS: ASTC 6×6 or 8×8.
- 512–1024 textures for normal props; 1024–2048 only for hero car/large unique landscape after memory profiling.
- Turn off Read/Write on shipped textures/meshes, strip unused mipmaps only where safe, and use mipmaps for 3D world textures.
- Share atlases/materials for environment kits. Avoid duplicate normal maps and uncompressed UI textures.

### Draw calls, batching and geometry

- Enable **SRP Batcher**; use a small, consistent URP/Lit or Shader Graph material set.
- Static batch stable scenery per track piece when it reduces draw calls without causing giant culling bounds. Dynamic batch is not a cure for high-material meshes.
- Use GPU instancing for repeated barriers, cones, foliage and crowd props with the same material.
- One material per simple prop whenever possible. Split transparent objects carefully; transparent overdraw is often a bigger issue than triangle count.
- Use 2–3 LODs, billboards for distant foliage, and disable shadows on far/small LODs.
- Target a measured draw-call budget per device, not a universal number. Capture the worst city/harbor view in Frame Debugger.

### Culling, physics and effects

- Bake occlusion culling for dense authored city/harbor layouts with real walls/buildings. Do not expect meaningful benefit in open desert or constantly recycled Endless sections.
- Restrict camera culling masks; the minimap camera should render only needed layers and never post-process.
- Keep active opponents to 3–5, four WheelColliders each, and simple colliders. Pool Endless pieces, coins, power-ups, hazards, particles and audio emitters.
- Cap rain, sparks and dust; avoid particle collision and realtime particle lights.
- Check Unity Profiler, Memory Profiler, Frame Debugger and platform tools in non-development IL2CPP builds.

## Shipping visual acceptance checklist

- [ ] 15–20 minute mid-tier device race stays near target frame pacing without thermal collapse.
- [ ] Worst view is profiled: five AI, city/harbor props, rain off/on, drift/nitro, HUD/minimap and post active.
- [ ] Static lighting is baked; car Light Probes/Reflection Probes look stable with no realtime-probe spikes.
- [ ] Bloom/motion blur are subtle, accessibility-tested and disabled in Low quality.
- [ ] No runtime material copies, `Instantiate` particle spikes, GC allocations in driving loops or runaway pooled object counts.
- [ ] Android/iOS texture compression, quality tier selection, safe-area UI and audio voice limits verified on physical devices.
