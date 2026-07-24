# Mobile rendering, Quality and Player settings

These are starting points for **Velocity Rush**, not universal values. Lock your supported device matrix early and profile release IL2CPP builds on physical hardware.

## URP renderer baseline

Create/use `VelocityRush_URP.asset` and `VelocityRush_Renderer.asset` (the bootstrap creates them) and configure them as follows.

| URP setting | Low tier (30 fps) | Mid tier (60 fps target) | High tier (60 fps target) | Why |
| --- | ---: | ---: | ---: | --- |
| Render scale | 0.75–0.85 | 0.90–1.0 | 1.0 | Largest GPU quality/performance control. |
| MSAA | Off / 2× | 2× | 2× / 4× after profiling | Prefer MSAA over expensive full-screen AA on mobile. |
| HDR | Off | Off by default | Optional | Enable only if tone mapping genuinely improves your art. |
| Main-light shadows | Hard, 512–1024 | Soft, 1024 | Soft, 2048 | One sun only; use baked shadows for scenery. |
| Shadow cascades | 1 | 1–2 | 2 | More cascades cost bandwidth and CPU. |
| Additional lights | Per-vertex, max 0–1 | Per-vertex, max 1 | Per-pixel, max 2 | Favor baked emissive/probes for neon and street lights. |
| Opaque texture | Off | Off | Only if shader requires it | It is a full-screen copy. |
| Depth texture | Off | Off | Only if effect requires it | It is a full-screen copy. |
| Post processing | Off | Bloom or color adjustment only | Bloom + color grading, profile | Keep effects subtle and limited. |

### Lighting rules

- Use **one Mixed/Baked directional sun** per race scene and bake static environment lighting. A second realtime light is an exceptional, profiled art direction choice—not a default.
- Bake lightmaps for roads, buildings, barriers and props; use light probes for cars and moving traffic.
- For bright signs/neon, use emissive materials plus bloom rather than many point lights.
- Disable shadows for small pickups, distant props, particles and most AI vehicles. Use blob/projected shadows if vehicle shadows are too expensive.
- Keep reflection probes sparse, baked and low resolution; use one skybox/ambient setup per environment.

## Quality levels

Set up three levels in **Project Settings → Quality** and map device tiers with a short benchmark, GPU/vendor rules, or a first-run setting screen. Avoid changing rendering paths every frame.

| Setting | `Mobile Low` | `Mobile Medium` | `Mobile High` |
| --- | ---: | ---: | ---: |
| Target frame rate | 30 | 60 | 60 |
| VSync | Off | Off | Off |
| Texture quality | Half | Full | Full |
| Anisotropic textures | Disabled | Per texture | Per texture |
| LOD bias | 0.65 | 0.9 | 1.1 |
| Max LOD | 1 | 0 | 0 |
| Particle max quality | Low | Medium | High (capped) |
| Terrain/detail distance | 45–60% | 75% | 100% after profiling |

Set `Application.targetFrameRate` from a small runtime performance settings service. On iOS with ProMotion displays and Android devices with high-refresh panels, do not automatically target 90/120 fps unless your content and thermal budget prove it sustainable.

## Assets and texture import

| Asset kind | Mobile recommendation |
| --- | --- |
| Vehicle/environment mesh | Low-poly base mesh, LOD Group (2–3 levels), combine static props carefully; avoid very high material counts. |
| Texture size | 512–1024 for routine props; 1024–2048 only for hero car/large unique terrain after checking memory. |
| Compression Android | ASTC 6×6 or 8×8 baseline; ETC2 fallback where required. |
| Compression iOS | ASTC 6×6 or 8×8. |
| Normal maps | Use only when visual impact warrants the bandwidth; reduce far-LOD normal maps. |
| Materials | URP/Lit or a small approved Shader Graph set; reuse materials and enable SRP Batcher compatibility. |
| Audio | Mono where appropriate, Vorbis/ADPCM according to CPU/memory test, streaming for long music, compressed-in-memory for short SFX. |

## Android Player settings

Configure these in **Project Settings → Player → Android**. Values marked as product-specific are placeholders.

- **Package name:** replace `com.yourstudio.velocityrush` with your registered identifier before upload.
- **Scripting backend:** IL2CPP for release.
- **Target architectures:** ARM64 required for Play; include ARMv7 only if your device/support policy needs it.
- **Target API level:** use the current Google Play-required level at release time; do not rely on this template for policy compliance.
- **Minimum API:** select from device analytics (API 26+ is a common modern baseline, subject to audience requirements).
- **Graphics APIs:** Vulkan first with OpenGLES3 fallback only after device testing, or OpenGLES3 only for a simpler compatibility matrix.
- **Texture compression:** ASTC with ETC2 fallback.
- **Build format:** AAB for Google Play; enable split by ABI/texture only if your delivery strategy supports it.
- **Stripping:** Managed stripping Medium, then test every scene/mode/purchase/notification path. Preserve reflection-referenced types with `link.xml` as needed.
- **Permissions:** request only what shipped features use. Do not add storage, location, microphone or contacts permissions by habit.

## iOS Player settings

- **Bundle Identifier:** replace `com.yourstudio.velocityrush` with the App ID registered in Apple Developer.
- **Scripting backend:** IL2CPP (required).
- **Architecture:** ARM64.
- **Target minimum iOS:** set from your audience/device analytics and current SDK requirements.
- **Graphics API:** Metal only; remove unsupported legacy APIs.
- **Texture compression:** ASTC.
- **Frame pacing:** test 30/60 fps choices on real iPhones/iPads; test thermal behavior after a 15–20 minute session.
- **Xcode:** build on macOS, configure signing/team/provisioning there or through your CI secret store; never commit signing credentials.
- Add only justified entries to `Info.plist` privacy usage descriptions.

## Build, profiling and release process

1. Build **Development + Autoconnect Profiler** to a low, mid and high representative device.
2. Measure in realistic races: 5 AI cars, particles, pickups, longest camera views, menu transitions and 10+ minutes of Endless mode.
3. Use Unity Profiler/Memory Profiler, URP Rendering Debugger, Frame Debugger and platform tools (Android GPU Inspector/Perfetto; Xcode Instruments/Metal tools).
4. Record median and 1% low frame time, CPU/GPU frame time, thermal state, battery drain, draw calls, triangles, texture memory and GC allocations.
5. Tune render scale, shadows, LODs, texture compression and particles first. Avoid solving mobile performance by simply lowering every visual control.
6. Make a **non-development IL2CPP release build**, test fresh install/upgrade/save behavior, then validate store requirements, privacy disclosures, age rating and screenshots.
