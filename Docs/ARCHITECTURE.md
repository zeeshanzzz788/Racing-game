# Architecture and extension map

## Runtime flow

```text
MainMenu
  └─ GameManager (persistent)
       ├─ ProgressionService (PlayerPrefs prototype)
       ├─ InputManager (wheel / tilt / editor fallback)
       └─ AudioManager

Start mode → RaceSession → load track scene
  ├─ RaceManager → spawn selected player + 3–5 waypoint AI → grid/countdown/checkpoints/laps
  ├─ EndlessRunManager → spawn selected player → EndlessTrackGenerator → score/difficulty
  ├─ RaceCameraController → follows GameManager.PlayerCar
  └─ UIManager → speed/timer/score/lap/nitro/result presentation
```

## Core C# scripts

| Script | Responsibility | Extension seam |
| --- | --- | --- |
| `Core/GameManager.cs` | Persistent app/session coordinator, catalog selection, scene loading, player/AI spawn and result settlement. | Replace direct `Resources` catalogs with Addressables/catalog service for a content-heavy release. |
| `Cars/CarController.cs` (`PlayerCarController` compatibility subclass) | WheelCollider suspension, drivetrain, drift, damage, nitro, RPM and player/AI input selection. | Replace with a tested custom vehicle model only behind the same public input/stat API. |
| `Input/InputManager.cs` | Wheel/tilt/pedal input values independent of vehicle code. | Swap internals for Input System action maps, gamepad support, remapping and accessibility presets. |
| `UI/UIManager.cs` | Defensive HUD/result display facade. | Replace legacy UGUI `Text` with TMP/UIToolkit while preserving public display methods. |
| `Race/RaceManager.cs` | Countdown, checkpoint sequence, laps and timer. | Add positions, false-starts, race rules, replay hooks and network authority here. |
| `Tracks/TrackManager.cs` + `TrackPiece.cs` | Modular fixed-route marker aggregation or pooled Endless connector chain, dynamic difficulty and content spawning. | Add biome/traffic rules, designer piece constraints and pooled VFX. `EndlessTrackGenerator` remains a legacy straight-run fallback. |
| `Progression/ProgressionService.cs` | Coins, car unlocks, stars, selections and best times. | Wrap encrypted/versioned local storage and cloud sync; migrate PlayerPrefs on first production build. |

## Scriptable data

- `CarDefinition`: identity/save key, visual prefab/color, unlock cost, garage bars and physical tuning.
- `TrackDefinition`: identity, display data, scene and default laps.
- `CampaignLevelDefinition`: level number, track, laps, AI count, target time, rewards and star gate.

Keep player-facing content data out of menu scripts. A larger project should move these to Addressables and a remote-config-safe catalog version.

## Production backlog

1. Replace generated primitives with licensed, optimized art and complete each track with boundaries, off-road friction, LODs, occlusion and baked lighting.
2. Add a vehicle damage/health model rather than relying only on impact threshold crashes.
3. Pool every recurring object: endless segments, pickups, particles, traffic and UI popups.
4. Add robust race placement, AI recovery/unstuck, catch-up tuning, ghost/replay support and anti-cheat rules if leaderboards are added.
5. Migrate UI to TextMeshPro and design-device-safe layouts; add haptics, accessibility, localization and offline/first-run flows.
6. Add unit/play-mode tests for progression migrations, mode/session selection, checkpoint order and save corruption recovery.
7. Add analytics/consent/IAP/adapters only after privacy/legal review and only for features that ship.
