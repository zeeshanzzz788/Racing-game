# Build Velocity Rush APK with GitHub Actions

This repo is a **complete Unity 2022.3 mobile racing game**. CI generates all prototype scenes/cars/tracks, then builds an Android APK.

## Features included in the CI build

| Area | Features |
|------|----------|
| Modes | Campaign (10 levels), Quick Race (AI), Time Trial, Endless (pooled segments) |
| Cars | 6 unlockable cars, stats, garage select/unlock |
| Upgrades | Engine / Handling / Nitro purchased with coins |
| Driving | WheelColliders, nitro, handbrake, reverse, damage |
| Controls | Steering wheel / tilt / buttons, keyboard in editor |
| AI | Waypoint follow + overtaking |
| Track | Modular pieces, checkpoints, coins, power-ups, hazards |
| Polish | URP, bloom volume, time-of-day/weather, slow-mo jumps/near-miss, camera FOV, UI panels |
| Audio | Engine/SFX hooks, music controller |
| Progression | Coins, XP-style rewards, local leaderboard, save |

Build entry point: `VelocityRush.EditorTools.AndroidBuild.Build`  
→ runs `CreatePrototypeContent()` then `BuildPipeline.BuildPlayer` (Android APK).

---

## One-time: Unity license secrets

Free **Personal** license works.

### A. Request activation file

1. GitHub → **Actions** → **Request Unity activation file** → **Run workflow**
2. Download artifact `UnityActivationFile` (`.alf`)

### B. Activate

1. Open [https://license.unity3d.com/manual](https://license.unity3d.com/manual)
2. Sign in with Unity ID
3. Upload the `.alf`, download `Unity_lic.ulf`

### C. Add secret

```bash
# from a machine with gh authenticated
gh secret set UNITY_LICENSE --repo zeeshanzzz788/Racing-game < /path/to/Unity_lic.ulf
```

Optional (some setups):

```bash
gh secret set UNITY_EMAIL --repo zeeshanzzz788/Racing-game
gh secret set UNITY_PASSWORD --repo zeeshanzzz788/Racing-game
```

---

## Build APK

```bash
gh workflow run "Android build" --repo zeeshanzzz788/Racing-game --ref main
# watch
gh run watch --repo zeeshanzzz788/Racing-game
# download
gh run download --repo zeeshanzzz788/Racing-game -n velocity-rush-android -D ./apk
```

Or: **Actions → Android build → Run workflow**.

Artifact name: **`velocity-rush-android`** → `VelocityRush.apk`

Install:

```bash
adb install -r VelocityRush.apk
```

---

## Local Unity build (optional)

1. Install Unity **2022.3.62f1** + Android modules  
2. Open this repo  
3. Menu **Velocity Rush → Create Prototype Content** (or rely on batch build)  
4. **Velocity Rush** build method / File → Build Settings → Android  

---

## Repo layout

```
Assets/Scripts/   # Full game code
Assets/Scripts/Editor/AndroidBuild.cs
Assets/Scripts/Editor/VelocityRushProjectBootstrapper.cs
Packages/manifest.json
ProjectSettings/
.github/workflows/android.yml
.github/workflows/request-activation.yml
```
