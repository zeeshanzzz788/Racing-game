# Velocity Rush CarController

`Assets/Scripts/Cars/CarController.cs` is the primary, test-ready WheelCollider vehicle implementation. It is realistic enough to read as a car—suspension, grip, weight transfer, wheel slip, braking, reverse and RPM—while adding deliberately forgiving mobile-racer assists: speed-sensitive steering, downforce, anti-roll, traction control, ABS and drift grip shaping.

`PlayerCarController` remains a thin compatibility subclass for older prefabs and integrations. Velocity Rush runtime systems now target the `CarController` base type, so new and existing car prefabs may use `CarController` directly.

## What it includes

- Four-wheel `WheelCollider` suspension, spring/damper preset setup, axle anti-roll and friction curves.
- FWD/RWD/AWD presets, torque-by-speed curve, torque split, speed limiter, brake bias, ABS and traction control.
- Accelerate, brake-to-reverse touch behaviour, explicit reverse input for AI, handbrake drift, slip-based skid/dust hooks and speed-sensitive steering.
- Nitrous capacity, drain, cooldown, passive refill, force boost and performance scaling.
- Persistent collision damage: paint/shader property damage value, optional smoke, engine/grip loss, repair API and severe/total-damage wreck transition.
- Simple AI-facing input API: `SetExternalInput(steer, throttle, brake, nitro, handbrake)`.
- Engine RPM, wheel slip, drift/nitro states and crash event consumed by `CarAudioController` and `CarEffectsController`.
- No allocations, scene lookups or object creation in the driving `FixedUpdate` path.

## Wheel setup: exact checklist

1. **Car root**
   - Create a root GameObject at scale **(1, 1, 1)**. One Unity unit must equal one metre.
   - Add a `Rigidbody`, body collider(s), and `CarController`. `PlayerCarController` is also supported only as a compatibility subclass.
   - Recommended Rigidbody starting values: mass comes from the `CarDefinition`; interpolation **Interpolate**; collision detection **Continuous Dynamic**. The controller applies these safe values at runtime.
   - Create an empty child at approximately `(0, 0.25–0.40, 0)` and assign it to **Center Of Mass**. A lower point makes an arcade car stable without freezing rotation.

2. **Wheel colliders**
   - Add four empty child objects: `Wheel_FL`, `Wheel_FR`, `Wheel_RL`, `Wheel_RR`.
   - Add one `WheelCollider` to each. Place its centre at the wheel hub, not at ground level.
   - Front wheels **must have local Z > 0**; this controller uses that to determine front/rear when auto roles are enabled.
   - Start with radius `0.33–0.37`, mass `20`, suspension distance `0.18`. Make the collider radius match the rendered tyre radius precisely.
   - Use normal, non-trigger road colliders. The road must be on a layer included by the default physics collision matrix.

3. **Wheel visuals**
   - Put the tyre/rim mesh below each WheelCollider and name the mesh transform **`Visual`**. The controller discovers and updates it with `GetWorldPose`.
   - Alternatively assign the `WheelSetup.visual` field manually in the inspector.
   - WheelCollider objects should not have MeshColliders. The car body needs simple box/capsule/convex colliders to hit barriers.

4. **Controller inspector**
   - Leave **Wheels** empty to auto-discover `WheelCollider` children at runtime, or fill each `WheelSetup` manually for more control.
   - Keep **Auto Assign Wheel Roles** on for normal cars. It assigns front steering, rear handbrake and drive wheels from `CarDefinition.driveLayout`.
   - If disabled, explicitly set each wheel's `steer`, `drive` and `handbrake` booleans.
   - Assign visual-body renderers or leave `Damage Renderers` empty to auto-find them. Assign optional damage smoke and per-wheel dust/trails only after the base car drives correctly.

5. **Data preset**
   - Create **Velocity Rush → Car Definition** under `Assets/Resources/Data/Cars/`.
   - Assign the prefab and set the stable `id`, then tune the fields described below.
   - Spawned Velocity Rush vehicles are initialized by `GameManager.Initialize(definition, isPlayer)`. For a standalone test car, assign **Assigned Preset** and enable **Initialize From Assigned Preset On Awake**.

## Mobile input wiring

The project input facade is already in `InputManager`:

| Control | Component / action |
| --- | --- |
| Steering wheel | `TouchSteeringWheel` on a UI `Image`; it calls `InputManager.SetSteering`. |
| Swipe alternative | `SwipeSteering` on a transparent, non-pedal UI panel. |
| Accelerate | `MobileButton` with action **Accelerate**. |
| Brake / reverse | `MobileButton` with action **Brake**. It brakes when moving forward and reverses only below about 1.1 m/s. |
| Nitro | `MobileButton` with action **Nitro**. |
| Handbrake | Optional `MobileButton` with action **Handbrake**. |
| Tilt | Set `InputManager` steering mode to **Tilt**. Its calibrated accelerometer X input is multiplied by `tiltSensitivity`; expose `SettingsController.CalibrateTilt` so players can set neutral while holding the phone naturally. |

Use a Canvas `GraphicRaycaster` and one `EventSystem`. Keep Player **Active Input Handling = Both** while using the editor keyboard fallback. In the editor/standalone build, arrows/WASD steer and accelerate, Left Shift triggers nitro, and Space is handbrake.

## Preset tuning for fun mobile handling

Tune in this order, on an actual phone at the game frame rate. Do not compensate for a bad wheel position with extreme torque or grip.

| Feel | Speed / torque | Handling setup | Suggested preset changes |
| --- | --- | --- | --- |
| Easy starter | 165–180 km/h / 2,000–2,400 | AWD, grip 0.95–1.05 | Steering 31–33°, strong `frontBrakeBias` 0.64–0.68, drift rear grip 0.62. |
| Balanced sports | 185–215 / 2,500–3,000 | AWD or RWD, grip 1.05–1.15 | Steering 28–31°, drift rear grip 0.55–0.60, nitro force 1,400–1,650. |
| High-speed expert | 225–260 / 3,100–3,800 | AWD, grip 1.10–1.25 | Steering 25–28°, a little more downforce, torque curve falls earlier near top speed. |
| Drift-focused | 175–205 / 2,500–3,200 | RWD, grip 0.95–1.08 | Steering 32–36°, drift rear grip 0.42–0.55, handbrake only on rear wheels. |

### Starting numbers

| Field | Starter value | Notes |
| --- | ---: | --- |
| Mass | 1,150–1,300 | Lower mass is not automatically more fun; retune spring and torque if it changes greatly. |
| Suspension travel | 0.16–0.20 | Increase only for bumpy/off-road tracks. |
| Spring / damper | 36,000 / 4,500 | Raise spring for heavier cars; raise damper if it bounces. |
| Grip | 1.0 | Raise by 0.05 steps. Excessive grip causes abrupt snap turns. |
| Motor torque | 2,200 | Use the `torqueBySpeed` curve to shape launch vs top-end before raising this dramatically. |
| Brake torque | 4,200 | If lock-ups occur, check wheel radius/road friction before disabling ABS. |
| Anti-roll | 6,000–8,000 | Too high makes one-wheel bumps feel harsh; too low causes excessive roll. |
| Normal / drift lateral damping | 5.5 / 1.2 | The gap is what makes predictable but controllable drifts. |
| Nitro capacity / drain / cooldown | 4 / 1 / 1.25 | The capacity is seconds of boost. Passive refill keeps casual sessions moving. |
| Damage loss at full | 0.25–0.40 | Stay below 0.5 so a crash hurts but does not make a race unwinnable. |

### Fast diagnosis

- **Car rolls over:** lower the centre of mass first; then increase anti-roll slightly and ensure colliders are not oversized/tall.
- **Car hops or vibrates:** confirm WheelCollider radius/hub position, reduce spring or increase damper, and ensure fixed timestep is sensible.
- **Car spins too easily:** increase `driftRearGrip`, normal lateral damping, or front brake bias; lower rear-drive torque.
- **Car cannot drift:** reduce `driftRearGrip` incrementally, use RWD, and ensure the handbrake button actually reaches `InputManager`.
- **Top speed is never reached:** check torque curve last key, wheel radius, drag, road slope and damage multiplier before increasing motor torque.
- **Reverse feels wrong:** verify brake button state clears on pointer-up; the controller intentionally requires near-stop before using the brake button as reverse.

## Damage hooks

- `ApplyDamage(amount)`, `Repair(amount)` and `RepairFully()` are public for hazards, repair pickups and garage repair systems.
- Minor impacts reduce `PerformanceMultiplier`, which affects torque and steering; at 100% damage or a severe collision the car wrecks.
- `CarController` sets `_DamageAmount`, `_BaseColor`, and `_Color` through a `MaterialPropertyBlock`. A custom URP Shader Graph can consume `_DamageAmount` to blend scratches/dents without creating material instances.
- Keep collision particles/audio on a cooldown/pool. Do not deform a skinned mesh every collision on a low-end device.

## AI setup

`AICarController` uses `WaypointCircuit` and calls the same external-input API as a player. Assign waypoint transforms in track order and call `ai.SetCircuit(raceManager.Waypoints)`; `GameManager` already does this for generated Quick Race opponents.

The controller uses a single forward `SphereCast` to choose a temporary lateral overtake line and briefly reverses if it is stuck. Put vehicle body colliders on a collision layer included in **Vehicle Probe Mask**. Keep the AI field to 3–5 cars for predictable mobile CPU cost.

## Audio setup

Add `CarAudioController` to the root and assign pre-created `AudioSource`s plus loops:

- **Engine loop:** pitch is driven by `EngineRpm` from idle to redline.
- **Tyre loop:** volume follows `NormalizedSlip` / drift state.
- **Nitro loop:** fades only while `IsNitroActive` is true.
- **Impact one-shot:** listens to the controller crash event.

Use one engine loop per nearby car at most; use low-pass/volume attenuation or disable distant AI audio. Short mono SFX should be compressed in memory; long music should stream.

## Mobile optimisation rules

1. Use **four WheelColliders per active car**. Do not add extra probe wheels, duplicate physics chassis, or full suspension raycast systems on every AI.
2. Cap active opponents to 3–5 and set distant cars to cheaper behaviour/LOD. The included AI does one sphere cast per active AI physics tick.
3. Keep `Time.fixedDeltaTime` at `0.02` (50 Hz) as a starting point. Raising physics to 100 Hz is usually not a mobile fix; optimize colliders and force tuning first.
4. Use primitive/convex colliders for cars and simple road meshes. Avoid MeshCollider-vs-MeshCollider vehicle impacts and unnecessary Rigidbody props on track scenery.
5. The driving loop caches wheels, friction curves, renderers and property blocks. Preserve that pattern: do not call `GetComponents`, LINQ, `Find`, instantiate objects, or allocate strings inside `FixedUpdate`.
6. Pool dust, sparks, crash debris, pickups and endless segments. Keep particle collision/lights off unless a profile proves the cost is acceptable.
7. Turn off expensive AI particle/audio effects outside the camera range; keep shadows, material count and transparent overdraw low.
8. Profile release IL2CPP builds on low/mid/high target devices with five AI cars, drifting, nitro and a busy track visible before shipping.

## Smoke test

1. Run **Velocity Rush → Create Prototype Content** if the primitive content has not been generated.
2. Open `MainMenu`, choose Quick Race, and verify the car launches, brakes, reverses after stopping, steers and can finish laps.
3. Hold Space/handbrake during a 30+ km/h turn: rear grip should release, tyre audio/particles should react, and the car should recover after release.
4. Hold and release nitro: boost has a short cooldown after release or depletion, then passively refills.
5. Hit a barrier at moderate speed: damage rises and performance drops. A severe crash or full damage triggers the race crash flow.
6. Run a five-AI Quick Race on a target device and inspect Unity Profiler for physics, GC allocations and audio/particle spikes.
