# Velocity Rush

**Velocity Rush** is a Unity 2022.3 LTS / URP mobile racing-game foundation for Android and iOS. It contains an intentionally dependency-light, WheelCollider-driven playable prototype generator plus a production-ready folder and code architecture for replacing its primitive art with finished content.

> The repository contains source and setup tooling—not generated Unity Library files or third-party assets. On first import run **Velocity Rush → Create Prototype Content**. That creates the URP assets, nine scenes, six car definitions, five track definitions, ten campaign levels, modular prototype prefabs, UI, and Build Settings locally.

## Included gameplay foundation

- **Main menu:** Play, Modes, Garage, Settings, Quit, and mode panel.
- **Garage:** six unlockable cars with speed, acceleration, handling, coin costs, and persistent selection.
- **Modes:** Endless, 10-level Campaign, Time Trial with local best times, and 3–5 opponent Quick Race.
- **Mobile driving:** on-screen steering wheel + hold pedals/nitro, optional tilt, and editor keyboard fallback.
- **Racing systems:** WheelCollider car physics, waypoint AI/overtakes, grid/countdown, checkpoint/lap validation, modular fixed circuits, pooled procedural Endless generation, hazards, coins, nitro and repair pickups.
- **Presentation hooks:** chase/cinematic camera, jump/near-miss slow-mo, 256px minimap, URP car-paint/reflection polish, HUD/speedometer/timer/lap/score/nitro, modern Canvas animation helper, engine/tire/music audio hooks, and exhaust/dust/sparks/nitro particle hooks.
- **Mobile performance posture:** URP, one mixed/baked directional light, baked probes, light bloom/subtle motion blur, dynamic resolution scaling, low-poly geometry, no required paid dependencies, and explicit quality guidance.

## Quick start

1. Install **Unity 2022.3.62f1 LTS** with **Android Build Support** and/or **iOS Build Support**.
2. Open this repository as the Unity project. Let Package Manager resolve URP and Input System.
3. Set **Project Settings → Player → Active Input Handling** to **Both** (the included keyboard fallback uses legacy axes; mobile UI does not require them).
4. Run **Velocity Rush → Create Prototype Content**. Confirm scene replacement only if you want to regenerate prototype scenes.
5. Open `Assets/Scenes/MainMenu.unity` and press Play. The generator also puts all scenes into Build Settings.
6. Follow [`Docs/SETUP.md`](Docs/SETUP.md) before replacing the prototype art or making an Android/iOS build.

## Documentation

- [Setup and scene wiring](Docs/SETUP.md)
- [Full WheelCollider car controller, wheel setup, tuning and mobile optimisation](Docs/CAR_CONTROLLER.md)
- [Modular tracks, campaign examples, AI routes and pooled Endless generation](Docs/TRACK_SYSTEMS.md)
- [URP visual polish, progression, audio and the full mobile 60 FPS optimisation guide](Docs/MOBILE_POLISH.md)
- [Android/iOS, Quality, and URP recommendations](Docs/MOBILE_SETTINGS.md)
- [Asset import shortlist and licensing checklist](Docs/ASSET_SHORTLIST.md)
- [Architecture and extension notes](Docs/ARCHITECTURE.md)

## Project layout

```text
Assets/
  Art/                 # Models, materials, shaders, textures and VFX (replace prototype art here)
  Audio/               # Music, SFX and mixers
  Prefabs/             # Cars, environment, gameplay and UI source prefabs
  Resources/
    Data/              # Bootstrap-generated car, track and campaign ScriptableObjects
    Prefabs/           # Runtime endless segment, pickup and hazard prototypes
  Scenes/              # Generated menu, garage, campaign, race and endless scenes
  Scripts/
    Core/              # GameManager, session and scene/result types
    Cars/              # WheelCollider player vehicle and waypoint AI
    Input/             # Mobile wheel/pedals and editor fallback
    Race/              # Grid, checkpoints, laps, collectibles, hazards and waypoints
    Tracks/            # Modular TrackPiece metadata, fixed routes and pooled Endless TrackManager
    Endless/           # Endless-run scoring (legacy straight generator kept for compatibility)
    UI/                # HUD, menu, garage, level-select and settings controllers
    Audio/, Camera/, VFX/, Progression/, Data/, Editor/
  Settings/            # Generated URP settings and mobile profiles
Docs/                  # Setup, mobile settings, assets and architecture notes
Packages/manifest.json # URP, Input System, UGUI and Unity package dependencies
ProjectSettings/       # Unity version, tags/layers and build-settings seed
```

## Important production notes

- The generated scenes deliberately use simple primitives so this repository remains legal, compact, and runnable without external downloads. Replace them with licensed assets before release.
- Run a real-device profiling pass for every target device tier. The recommended settings are a baseline, not a substitute for profiling.
- `ProgressionService` currently uses `PlayerPrefs` for a local prototype. Replace/extend it with an encrypted, versioned cloud/local save adapter before launch.
- Add App Store privacy declarations, consent/analytics handling, store/restore purchase validation, and platform-specific signing in your release branch.
