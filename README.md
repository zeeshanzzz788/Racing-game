# Velocity Rush

**Mobile 3D car racing** (Unity 2022.3 LTS + URP) — Android (and iOS-ready).

## Full feature list

- **Main menu** — Play, Modes, Garage, Settings, Quit  
- **Garage** — 6 unlockable cars, coin unlocks, **Engine / Handling / Nitro upgrades**  
- **Campaign** — 10 levels, stars, rewards  
- **Quick Race** — AI opponents, grid start, position  
- **Time Trial** — best times  
- **Endless** — modular streamed track, coins, nitro/repair pickups, obstacles, difficulty  
- **Driving** — WheelColliders, suspension, nitro, handbrake, reverse, damage  
- **Controls** — on-screen wheel / tilt / buttons (+ keyboard in Editor)  
- **Polish** — URP, bloom, time-of-day/weather, slow-mo jumps & near-miss, camera chase/cinematic, UI animation  
- **Audio** — engine + music hooks  
- **Progression** — coins, saves, local leaderboards  

## Playable content (no art pack required)

Open in Unity **2022.3.62f1** → menu **Velocity Rush → Create Prototype Content**  
→ generates scenes, cars, tracks, URP, Build Settings.

Or let **GitHub Actions** generate content during the APK build.

## Build APK (GitHub Actions)

See **[Docs/GITHUB_APK_BUILD.md](Docs/GITHUB_APK_BUILD.md)**.

1. Actions → **Request Unity activation file** → download `.alf`  
2. https://license.unity3d.com/manual → get `Unity_lic.ulf`  
3. `gh secret set UNITY_LICENSE < Unity_lic.ulf`  
4. Actions → **Android build** → download **velocity-rush-android** artifact  

```bash
adb install -r VelocityRush.apk
```

## Docs

| Doc | Topic |
|-----|--------|
| [Docs/SETUP.md](Docs/SETUP.md) | Local Unity setup |
| [Docs/GITHUB_APK_BUILD.md](Docs/GITHUB_APK_BUILD.md) | CI APK |
| [Docs/CAR_CONTROLLER.md](Docs/CAR_CONTROLLER.md) | Driving |
| [Docs/TRACK_SYSTEMS.md](Docs/TRACK_SYSTEMS.md) | Tracks / endless |
| [Docs/MOBILE_SETTINGS.md](Docs/MOBILE_SETTINGS.md) | Mobile URP |
| [Docs/MOBILE_POLISH.md](Docs/MOBILE_POLISH.md) | FX / graphics |
