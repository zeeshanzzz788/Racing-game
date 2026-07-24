# Asset import shortlist

The repository is intentionally runnable without third-party content. Treat this as a procurement checklist rather than a dependency list: **confirm the current price, license, platform support, render-pipeline support, update history and redistribution terms on the publisher page before purchase or import.** Asset Store availability and pricing change.

## Free / first-party starting points

| Asset/package | Use in Velocity Rush | Import notes |
| --- | --- | --- |
| **Universal RP Samples** (Unity Package Manager / Unity samples) | Reference scenes, URP renderer features, Shader Graph examples and mobile rendering patterns. | Import selected samples only; do not copy renderer features without profiling them. |
| **Unity Particle Pack** (Unity Asset Store) | Dust, sparks and small impact-effect source material. | Convert/test materials for URP; aggressively cap particle counts and remove light modules. |
| **Cinemachine** (Unity Package Manager) | Optional advanced chase/replay cameras. | The project includes a lightweight `RaceCameraController`, so add Cinemachine only if its workflow/replay value outweighs dependency cost. |
| **Input System** (Unity Package Manager) | A production action-map/remote-control replacement for the included direct mobile input facade. | Already declared. Add an `.inputactions` asset when adding gamepads, remapping, or more complex accessibility controls. |
| **ProBuilder** (Unity Package Manager) | Fast blockout of circuits, barriers and checkpoint lanes. | Use for authoring; convert/optimize final meshes and colliders before release. |

## Paid assets worth evaluating

| Publisher/category examples | Best use | Acceptance criteria |
| --- | --- | --- |
| **Synty POLYGON** city, nature, racer/vehicle packs | Cohesive low-poly roads, buildings, scenery and vehicles. | Confirm the specific pack’s license and that material setup is URP-friendly; create LODs/colliders rather than shipping all source meshes. |
| **Racing Game Kit / arcade racing controller kits** | Reference or acceleration for menus, race flow, AI or replay features. | Do **not** run two vehicle controllers on one car. Evaluate source quality, mobile profiling, URP support and active maintenance before integrating. |
| **Realistic Engine Sounds / vehicle sound libraries** | Layered engine, shift, skid, collision and nitro source clips. | Verify commercial/mobile licensing and convert to looping, memory-efficient clips; test CPU use on the lowest device. |
| **Easy Mobile Pro or a maintained mobile-services SDK** | Native sharing, ratings, notifications or platform service integration if the product requires them. | Require current Android/iOS support, privacy documentation and a narrow feature set. Avoid importing unused services. |
| **Gaia/Map Magic/road or terrain authoring tools** | Editor-only environment production. | Bake/export static mobile-ready meshes; do not assume runtime terrain generation is mobile-friendly. |
| **Mobile shader packs / stylized URP shader packs** | A small number of polished water, foliage, sky or car-paint effects. | Require URP + SRP Batcher compatibility, no hidden fullscreen passes, and a device GPU profile. |

## Suggested import order

1. Establish final visual direction with a small low-poly environment/vehicle kit and a test circuit.
2. Replace only the prototype car meshes/materials while retaining `PlayerCarController` and WheelCollider layout.
3. Add a vetted environment kit, generate LODs/colliders/lightmap UVs, then bake one representative circuit.
4. Add audio and a minimal VFX pack; profile with five cars and all effects visible.
5. Consider optional camera/mobile-service/editor tools only after the core race meets frame-time/memory budgets.

## Import hygiene

- Import into a staging branch/project first; inspect materials, render pipeline conversion, compile warnings, licenses, included packages and demo assets.
- Keep third-party source under `Assets/ThirdParty/<Publisher>/<Asset>/` and preserve its license/readme files.
- Never commit a paid asset to a public repository unless its license explicitly allows it.
- Avoid duplicate packages and packages that modify project-wide render/input settings without review.
- Audit shaders with Frame Debugger: transparent overdraw, depth copies, screen textures and realtime lights are common mobile regressions.
