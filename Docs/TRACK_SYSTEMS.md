# Modular tracks, campaign circuits and Endless generation

Velocity Rush now includes a reusable modular track stack under `Assets/Scripts/Tracks/`:

| Script | Role |
| --- | --- |
| `TrackPiece.cs` | Metadata on each straight, turn, hill, jump or chicane prefab: entry/exit anchors, AI route markers and content slots. |
| `TrackManager.cs` | Builds a fixed modular circuit route or a pooled Endless chain, ramps difficulty, spawns content and recycles passed pieces. |
| `PooledTrackObject.cs` | Runtime pool handle used by pickups/obstacles. |
| `PowerUpPickup.cs` | Reusable repair/nitro-style track power-up. |

The prototype bootstrap generates seven `TrackPiece` prefabs in `Assets/Resources/Prefabs/TrackPieces/`: straight, left/right curve, hill up/down, jump and chicane. They are intentionally primitive blockouts; replace the meshes/materials with licensed road art while preserving their anchors and marker transforms.

## Build a modular track piece

1. Make a prefab with root scale **(1,1,1)** and add `TrackPiece`.
2. Add child transforms **Entry** and **Exit**.
   - Their forward (blue Z) axes must point in the driving direction.
   - The entry is at the beginning of the road; exit is at the exact handoff point for the next piece.
   - Keep anchor rotation and road tangent continuous. TrackManager aligns the next root so its Entry exactly matches the previous Exit.
3. Add road/shoulder/barrier colliders and static render meshes. Use simple mesh or box colliders; set the road to the **Track** layer.
4. Add optional marker child transforms and assign them on `TrackPiece`:
   - `AiWaypoints` in driving order, 2–5 markers per piece.
   - `CollectibleSlots`, `PowerUpSlots`, `ObstacleSlots` just above the road surface.
5. Set `minimumDifficulty`, `selectionWeight`, and `specialPiece`.
   - Straight: minimum `0`, high weight (5–8).
   - Gentle curve: minimum `.05–.15`, medium weight (1.5–3).
   - Hill: minimum `.25–.40`, low-medium weight (1–2).
   - Jump/chicane: minimum `.45–.70`, low weight (`.5–1`) and **special** enabled.
6. Test the join in Scene view with `TrackPiece` selected; green is Entry, red is Exit, cyan/yellow/magenta/orange show marker types.

### Connector rules

- Never use non-uniform root scale. It breaks connector mathematics and WheelCollider scale assumptions.
- Match road width and surface height at every join. A visual seam is acceptable for a prototype; a collider step is not.
- Keep turns broad enough for the worst selected car at the intended difficulty. Use a banked mesh/art, not a hidden physics force, for high-speed curves.
- A jump should have a safe landing ramp and a normal exit tangent. Keep jump frequency low on mobile.

## TrackManager configuration

### Endless mode

1. Create an empty `TrackManager` GameObject at the first road connector and set **Mode = Endless**.
2. Assign all modular `TrackPiece` prefabs to **Modular Piece Prefabs**.
3. Assign coin/nitro prefabs to **Collectible Prefabs**, repair/boost prefabs to **Power Up Prefabs**, and barriers/traffic/cones to **Obstacle Prefabs**.
4. Start with 6–8 **Pieces Ahead**, recycle distance 30–45 m, and 120–180 seconds to max difficulty.
5. On `EndlessRunManager`, assign this `TrackManager`. It passes the spawned player to it automatically.

The manager retains a fixed number of active segments and returns passed pieces, pickups and hazards to pools. Difficulty raises the player speed multiplier, increases obstacle chance and unlocks the tighter/special pieces by their `minimumDifficulty` value. It avoids back-to-back special pieces when alternatives exist.

### Fixed modular circuit mode

1. Place `TrackPiece` scene instances in driving order, including a final piece that visually joins the first.
2. Create a `WaypointCircuit` and `TrackManager` on the track root. Set **Mode = Fixed Circuit**, assign the placed pieces to **Fixed Track Pieces**, and assign the `WaypointCircuit`.
3. On `RaceManager`, assign the `TrackManager`. At race start it calls `RefreshFixedCircuitWaypoints()` and uses the assembled route for `AICarController` opponents.
4. Place start grid transforms and trigger checkpoints separately. Checkpoint index `0` is the finish line; all subsequent checkpoint indices must follow driving order.

The fixed-circuit mode intentionally does not spawn/recycle pieces. It is an authoring helper that gathers per-piece AI markers into the existing race system.

## Generated example content

After **Velocity Rush → Create Prototype Content**, Build Settings contains these playable prototype environments:

| Scene / track asset | Suggested campaign use | Contents |
| --- | --- | --- |
| `DesertCircuit` | Levels 1–2 | Beginner circuit, wide visual read, checkpoints and grid. |
| `CityCircuit` | Levels 3–4 | Medium-speed circuit, AI opponents, checkpoints. |
| `CoastCircuit` | Levels 5–6 | Fast, open circuit, Time Trial candidate. |
| `MountainSprint` | Levels 7–8 | Higher difficulty visual theme; replace blockout with hills/tighter modular pieces. |
| `HarborRun` | Levels 9–10 | End-game visual theme; use traffic/obstacles and narrow routes. |
| `EndlessRun` | Unlimited | Pooled modular chain, dynamic difficulty, coins/nitro/repair pickups and hazards. |

The initial campaign's ten level assets cycle through the five track definitions. Change lap count, AI count, star requirements, reward and target time in each `CampaignLevelDefinition` to produce the intended progression.

## AI race integration

- `RaceManager` keeps its checkpoint/lap authority and grid spawning.
- `AICarController` reads `WaypointCircuit`, follows the modular marker path, slows for corners, makes a cheap forward sphere-cast overtake attempt, and has a reverse recovery path.
- Use 2–5 AI cars on mobile. Put vehicle body colliders on layers included in each AI's **Vehicle Probe Mask**.
- Markers should be centred in the safe driving lane; at corners place one before entry, one through apex, and one after exit. Do not place markers on barriers or directly over jumps unless the landing is safe.

## Free road/blockout assets

No third-party models are committed to this project. Recommended free choices before buying a final environment kit:

- **ProBuilder** (free Unity Package Manager package): use it to create clean, grid-aligned road, barrier, ramp and bridge modules that preserve the TrackPiece anchor workflow.
- **Unity URP Samples** (free first-party samples): useful for URP material, lighting and Shader Graph references when converting a road kit.
- Asset Store free road/low-poly environment packs: import into a staging project, then verify the current license, URP material compatibility, lightmap UVs, LODs and mobile draw-call count before copying selected meshes into `Assets/ThirdParty/`.

Do not ship unverified demo scenes or paid assets in this repository. See `Docs/ASSET_SHORTLIST.md` for general import hygiene.

## Mobile optimisation checklist

1. **Pool everything that repeats.** `TrackManager` already pools sections and spawned content. Keep pickup scripts pool-aware; do not call `Destroy` for runtime Endless content.
2. Use **6–8 active sections**, not an ever-growing map. Keep each section's mesh/material count small and combine truly static decoration per piece where profiling proves it helps.
3. Make 2–3 **LOD levels** for buildings, foliage, barriers and distant props. Disable shadow casting on far LODs and tiny set dressing.
4. Bake lightmaps/light probes for track scenery. Keep one mixed/baked sun and avoid per-piece realtime lights.
5. Use **occlusion culling** only in dense, authored city/harbor sections with meaningful occluders. It is rarely useful for open desert, hills or constantly changing Endless segments; profile before enabling it.
6. Prefer primitive/convex colliders for barriers and one simple road collider per module. Avoid moving Rigidbody scenery and MeshCollider-vs-MeshCollider impacts.
7. Keep transparent effects, traffic, particle collisions and audio emitters within a camera-distance budget. Turn them off before the segment is recycled.
8. Profile long Endless runs on a low-tier phone: inspect GC allocations, active pooled object count, physics time, overdraw, texture memory and thermal throttling.

## Test checklist

- [ ] All Entry/Exit gizmo directions align and no car catches on joins.
- [ ] A new Endless segment appears before the active route ends; passed sections, pickups and hazards deactivate and return to pools.
- [ ] After 2–3 minutes, speed/obstacle density/tighter-piece frequency increase without impossible joins.
- [ ] Coins, nitro and repair power-ups reset correctly after pooled reuse.
- [ ] Fixed modular circuit creates a valid waypoint route and 3–5 AI cars complete laps/checkpoints.
- [ ] City/harbor content is profiled with LODs, baked lighting and realistic target-device visibility.
