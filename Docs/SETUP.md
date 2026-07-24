# Velocity Rush setup guide

## 1. Prerequisites

| Item | Recommended baseline |
| --- | --- |
| Unity | **2022.3.62f1 LTS** (the version in `ProjectVersion.txt`) |
| Rendering | Universal Render Pipeline **14.0.11** |
| Targets | Android Build Support (SDK/NDK/JDK) and/or iOS Build Support/Xcode on macOS |
| Input | Input System package is installed; set **Active Input Handling = Both** while using the included keyboard fallback |
| Orientation | Landscape left/right |

The project has no binary art dependencies. Unity will regenerate its `Library/` directory on import; do not commit it.

## 2. Import and generate the runnable prototype

1. Clone/open the repository from Unity Hub with Unity 2022.3.62f1.
2. Allow Package Manager to resolve packages from `Packages/manifest.json`. If the editor asks to update packages, keep URP on the Unity-2022-compatible 14.x line unless you intentionally migrate the project.
3. In **Edit → Project Settings → Player → Other Settings**, choose **Both** for **Active Input Handling**. Restart the editor when asked.
4. Select **Velocity Rush → Create Prototype Content**.
   - It creates a URP Pipeline Asset + Universal Renderer in `Assets/Settings/Rendering/` and makes it active.
   - It creates the six car assets, three track assets, ten campaign-level assets, primitive car/pickup/segment prefabs and generated scenes.
   - It adds `MainMenu`, `Garage`, `LevelSelect`, `DesertCircuit`, `CityCircuit`, `CoastCircuit`, and `EndlessRun` to Build Settings.
   - It may ask before replacing an existing generated scene; choose **Keep Existing** once you begin hand-authoring it.
5. Open `Assets/Scenes/MainMenu.unity`, press **Play**, and use the keyboard (`WASD`/arrows, Left Shift nitro, Space handbrake) or the touch controls in Device Simulator/a device.

## 3. What the generator creates

| Scene | Purpose |
| --- | --- |
| `MainMenu` | Play, Modes, Garage, Settings and Quit UI; persistent game/input/audio services begin here. |
| `Garage` | Browse/select/unlock six cars. Selection and coins persist through `PlayerPrefs`. |
| `LevelSelect` | Ten Campaign buttons driven by `CampaignLevelDefinition` assets. |
| `DesertCircuit`, `CityCircuit`, `CoastCircuit` | Lightweight rectangle-loop circuits with spawn grid, checkpoints, AI route, HUD and mobile controls. |
| `EndlessRun` | Infinite forward segment generator with coins, nitro and obstacles. |

For normal play start in `MainMenu`. `RuntimeBootstrap` creates the persistent services when a race scene is opened directly for construction; it uses fallback race settings because no menu session was selected.

## 4. Author a production car prefab

1. Duplicate `PF_PrototypeCar` into `Assets/Prefabs/Cars/`.
2. Keep the root at scale **(1,1,1)**; use **1 Unity unit = 1 metre**. Add one `Rigidbody`, one body collider, and four child `WheelCollider`s.
3. Name each wheel visual child `Visual`; `PlayerCarController` automatically discovers child `WheelCollider`s and updates this visual transform from `WheelCollider.GetWorldPose`.
4. Place front wheels at local `z > 0`; this controller uses that convention to steer them. Rear wheels drive by default; enable all-wheel drive in the inspector if desired.
5. Create/update a `CarDefinition` under `Assets/Resources/Data/Cars/`, assign the prefab, identity key, unlock cost, body color and physics values. The identity `id` is a save key—do not change it after shipping.
6. Tune on device: WheelCollider radius, suspension, centre of mass, mass, torque, grip and top speed interact. Avoid non-uniform parent scale.

### Recommended car tuning starting range

| Property | Suggested range |
| --- | --- |
| Mass | 1,000–1,450 kg |
| Wheel radius | 0.30–0.38 m |
| Suspension distance | 0.12–0.20 m |
| Top speed | 165–260 km/h |
| Motor torque | 2,100–3,700 |
| Brake torque | 4,000–5,500 |
| Steering | 26–34° |
| Grip | 0.9–1.35 |

## 5. Build a polished circuit

1. Duplicate one generated circuit scene and update/create its `TrackDefinition.sceneName`.
2. Keep the one directional light **Mixed** or **Baked** and bake static scenery lighting. Mark buildings, terrain, barriers and props static where appropriate.
3. Add a `RaceManager` and assign:
   - `playerStart` and up to five `opponentStarts`;
   - `orderedCheckpoints`, in exact driving order, with the finish line at **index 0**;
   - one `WaypointCircuit`, with transforms ordered around the route.
4. Give checkpoint colliders `isTrigger = true`. Set their `Checkpoint.checkpointIndex` to the matching array index.
5. Use a Track layer for road colliders, low-poly meshes with LODs, occlusion-friendly city blocks and baked light probes for moving cars.
6. Attach a `RaceCameraController` to the gameplay camera and a `UIManager` to a HUD canvas. The generated scenes show all required field wiring.

## 6. Configure each mode

| Mode | Start call | Key systems |
| --- | --- | --- |
| Quick Race | `GameManager.StartQuickRace(track, 3–5, laps)` | `RaceManager`, `WaypointCircuit`, `AICarController` |
| Campaign | `GameManager.StartCampaign(level)` | Ten `CampaignLevelDefinition` assets; rewards/stars are saved on win |
| Time Trial | `GameManager.StartTimeTrial(track, laps)` | No AI; per-track `PlayerPrefs` best record |
| Endless | `GameManager.StartEndless(track)` | `EndlessRunManager`, `EndlessTrackGenerator`, pickup/hazard prefabs |

The endless generator’s prototype assumes forward +Z, fixed-length segments. For a shipping game, replace `Destroy/Instantiate` with a pool and use connector transforms/weighted segment sets if the course bends.

## 7. Wire touch controls and UI

The generated HUD includes a `TouchSteeringWheel`, `MobileButton` instances for brake/accelerate/nitro, speedometer, timer, score, lap, nitro bar, countdown and result panel.

- Set `InputManager` to **Wheel** for the steering image or **Tilt** for accelerometer X-axis input.
- `MobileButton` uses `IPointerDown`/`IPointerUp`, so it must be under a Canvas with `GraphicRaycaster` and a scene `EventSystem`.
- For a minimap, add a second orthographic camera on the **Minimap** layer, render it to a small RenderTexture (256–512 px), and show it in a `RawImage`. Exclude post-processing, shadows and expensive transparent effects from that camera.
- For safe areas/notches, add a safe-area panel wrapper to the HUD before release; test landscape left and right on actual devices.

## 8. Replace prototype VFX and audio

`CarEffectsController` exposes dust, sparks and nitro `ParticleSystem` references, while `CarAudioController` exposes engine/tire `AudioSource`/clip references. Assign mobile-safe effects:

- Dust: world-space, GPU-instanced-compatible particles, low emission, no collision.
- Sparks: short burst, capped max particles, no lights.
- Nitro: a short additive trail; avoid per-particle lights.
- Engine: one looping compressed clip with pitch driven by speed; tire loop only while skidding.
- Music/SFX: route through an AudioMixer and have `AudioManager` set exposed mixer parameters in a production replacement.

## 9. Verification checklist

- [ ] No red Console errors after prototype generation.
- [ ] Main menu starts each mode; Garage unlock/select persists after restart.
- [ ] Each circuit finishes only after checkpoints are crossed in order.
- [ ] Time Trial stores a best time; Campaign awards coins/stars.
- [ ] Endless continues beyond at least 10 generated segments and cleans old ones.
- [ ] Touch controls work in a real Android/iOS build, including release outside a button.
- [ ] Profile CPU, GPU, memory, draw calls and thermal behaviour on low/mid/high device tiers.
- [ ] Bake lighting and validate the final scene with URP Frame Debugger/RenderDoc before release.
