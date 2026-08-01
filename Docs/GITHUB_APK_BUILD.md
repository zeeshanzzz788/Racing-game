# Build the FULL Velocity Rush APK (GitHub Actions)

Repo: **https://github.com/zeeshanzzz788/Racing-game**

This is the complete Unity game (all modes + garage + endless + polish).  
CI runs `AndroidBuild.Build` → generates prototype content → compiles Android APK.

---

## What you get in the APK

| Feature | Included |
|---------|----------|
| Main menu (Play / Modes / Garage / Settings / Quit) | Yes |
| Garage — 6 cars, unlock with coins | Yes |
| Upgrades — engine / handling / nitro | Yes |
| Campaign — 10 levels, stars | Yes |
| Quick Race — AI opponents | Yes |
| Time Trial — best times | Yes |
| Endless — streamed modular track, coins, power-ups, obstacles | Yes |
| WheelCollider driving, nitro, handbrake, damage | Yes |
| Touch wheel / tilt / buttons | Yes |
| URP + bloom, time-of-day/weather, slow-mo polish | Yes |
| Local progression / leaderboard saves | Yes |

---

## One-time setup (free Unity Personal license)

GitHub cannot build Unity without a license. **Personal (free) works.**

### 1. Activate on any PC

1. Install [Unity Hub](https://unity.com/download)  
2. Log in with a free Unity account  
3. **Preferences → Licenses → Add → Get a free personal license**  
   (press Add even if something already lists)

### 2. Find `Unity_lic.ulf`

| OS | Path |
|----|------|
| Windows | `C:\ProgramData\Unity\Unity_lic.ulf` |
| macOS | `/Library/Application Support/Unity/Unity_lic.ulf` |
| Linux | `~/.local/share/unity3d/Unity/Unity_lic.ulf` |

### 3. Add three GitHub secrets

Repo → **Settings → Secrets and variables → Actions → New repository secret**

| Secret | Value |
|--------|--------|
| `UNITY_LICENSE` | **Entire text** of `Unity_lic.ulf` |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

CLI:

```bash
gh secret set UNITY_LICENSE  --repo zeeshanzzz788/Racing-game < Unity_lic.ulf
gh secret set UNITY_EMAIL    --repo zeeshanzzz788/Racing-game -b 'you@email.com'
gh secret set UNITY_PASSWORD --repo zeeshanzzz788/Racing-game -b 'your-password'
```

Reference: https://game.ci/docs/github/activation

---

## Build

1. Open https://github.com/zeeshanzzz788/Racing-game/actions  
2. Workflow **Android build** → **Run workflow** → branch `main`  
3. Wait ~20–60 minutes  
4. Download artifact **`velocity-rush-android`**  
5. Unzip → `VelocityRush.apk`

```bash
adb install -r VelocityRush.apk
```

Or CLI:

```bash
gh workflow run "Android build" --repo zeeshanzzz788/Racing-game --ref main
gh run watch --repo zeeshanzzz788/Racing-game
gh run download --repo zeeshanzzz788/Racing-game -n velocity-rush-android -D ./apk-out
```

---

## If the workflow says “License required”

Secrets are missing or empty. Complete the **One-time setup** above, then run **Android build** again.

---

## Local build (optional)

Unity **2022.3.62f1** + Android modules → open repo →  
**Velocity Rush → Create Prototype Content** → Build Settings → Android.
