# Research: Premium Visuals Asset Integration

**Branch**: `002-premium-visuals-integration` | **Date**: 2026-02-27

## R-001: URP Material Compatibility for Third-Party Assets

**Decision**: Verify materials at import time; use Unity's built-in Render Pipeline Converter for bulk conversion if needed.

**Rationale**: All four asset packs were purchased for use with Unity's standard pipeline. Unity 6 URP 17.3.0 includes automatic material upgrade support. The SF_Asteroids-M2 and Station_MS2 packs use standard Specular/Metallic workflows that map directly to URP Lit shader. Nebula skybox materials use the Skybox/Panoramic shader which is pipeline-agnostic. Retora ship materials use standard PBR which converts cleanly.

**Alternatives considered**:
- Manual shader rewrite: Rejected — unnecessary overhead; standard conversions handle all cases.
- Custom URP shader: Rejected — would break when asset packs update; standard URP Lit is sufficient.

## R-002: Skybox System Architecture — HDRI Loading and Rotation

**Decision**: Create a `SkyboxConfig` ScriptableObject referenced per-scene. A `SkyboxController` MonoBehaviour (view layer) reads the config, applies the material to `RenderSettings.skybox`, sets ambient lighting mode to Trilinear/Custom, and rotates the skybox via `_Rotation` shader property each frame.

**Rationale**: The Nebula Skybox Pack provides 12 pre-made materials (Skybox/Panoramic shader with HDRI texture). Each material already has correct exposure. Per-scene configuration via ScriptableObject aligns with the constitution's data-oriented design (Principle IV) and Addressables strategy. Rotation via shader property is zero-GC and Burst-friendly. Fallback to `SpaceSkybox.mat` is a simple null-check.

**Alternatives considered**:
- Runtime HDRI texture loading via Addressables: Deferred — adds async complexity for no benefit when materials are already pre-configured. Can retrofit later.
- Procedural skybox generation: Rejected — the purchased HDRI pack provides superior quality at zero development cost.

## R-003: Asteroid Mesh-to-OreType Visual Mapping

**Decision**: Create an `AsteroidVisualMappingConfig` ScriptableObject that maps each ore type to 2 dedicated mesh prefab references + a tint color override. The `AsteroidFieldSystem` uses this mapping during instantiation to select the correct mesh for each ore type and apply tint via `AsteroidBaseColorOverride`.

**Rationale**: The 6 asteroid prefabs (`Mineral_asteroid-01` through `06`) divide evenly into 3 pairs. The existing `AsteroidBaseColorOverride` material property component already supports per-entity color — extending it to blend the ore's beam color as a tint is zero-cost. This approach is fully data-driven (designer can reassign meshes in the inspector) and requires no code changes to the existing depletion color system — the tint is a multiplicative layer.

**Alternatives considered**:
- Separate material instances per ore type: Rejected — breaks SRP batching; per-entity color override is cheaper.
- Shader keyword for ore type: Rejected — adds shader complexity; per-entity color is simpler and already proven.

## R-004: Asteroid Mass-Proportional Sizing and Depletion Shrink

**Decision**:
- **Initial size**: `AsteroidFieldSystem` already computes `radius = rng.NextFloat(3f, 5f)` and `mass = radius³ * 10f`. This naturally produces mass-proportional size. No change needed for initial sizing — the existing relationship is preserved.
- **Depletion shrink**: Add an `AsteroidScaleSystem` (Burst-compiled, runs after `AsteroidDepletionSystem`) that computes `scale = initialRadius * lerp(MinScaleFraction, 1.0, RemainingMass / InitialMass)`. The `MinScaleFraction` (e.g., 0.3) prevents asteroids from becoming invisible before depletion.
- **Crumble pauses**: Track depletion thresholds (75%, 50%, 25%, 0%) via a bitmask on `AsteroidComponent`. When a threshold is crossed, freeze scale for a configurable duration (e.g., 0.5s) to create the "crumble" visual beat.
- **Fade-out**: At 0% depletion, after crumble pause completes, interpolate alpha from 1→0 over ~0.5s via `AsteroidBaseColorOverride` alpha channel, then destroy the entity. **Alpha Clipping required**: asteroid materials must have Alpha Clipping enabled (URP Lit setting, `_Cutoff` ≈ 0.5) so that `_BaseColor.a` changes are visible. Standard opaque rendering ignores alpha. This produces a clip-based vanish rather than smooth transparency; smooth dissolve deferred to Phase 1.2 VFX.
- **Per-entity pristine color**: Add `PristineTintedColor` (float4) to `AsteroidComponent`, set at spawn as `pristineGray × oreTintColor`. This allows `AsteroidDepletionSystem` to read per-entity tinted pristine color from ECS data rather than requiring managed ScriptableObject access in a Burst job.

**Rationale**: This approach leverages the existing `AsteroidComponent` fields (`InitialMass`, `RemainingMass`, `Depletion`) and `AsteroidBaseColorOverride` — minimal new component data (PristineTintedColor adds 16 bytes per entity, 4.8 KB for 300 asteroids). The crumble pause bitmask is a cheap way to track which thresholds have been crossed without allocations. The lerp with `MinScaleFraction` prevents the asteroid from becoming too small to see/target before it's actually depleted. Entity destruction frees rendering budget immediately.

**Alternatives considered**:
- Particle effect on crumble: Out of scope (Phase 1.2 VFX).
- Replace mesh at thresholds: Rejected — we don't have pre-broken mesh variants; smooth scale is better.
- Shrink to zero without minimum: Rejected — asteroid becomes untargetable at tiny scales, frustrating the player.

## R-005: Mining Barge Variant Assembly from Modular Ship Pack

**Decision**: Assemble 3 barge prefabs manually in the Unity Editor from Retora modular parts (HullParts, DoorParts, PowerSystemParts, MiscParts). Use the 5 pre-built ships (Ship1-Ship5) as reference for part placement. Each barge is a single root GameObject with child part meshes, then configured as an ECS entity via `ShipAuthoring`.

**Rationale**: The Retora pack has 41 component prefabs in clear categories (9 hulls, 8 doors, 5 power systems, 13 misc). Manual assembly in-editor is the fastest path for 3 variants and ensures designer control over silhouette. Each assembled prefab gets a `ShipArchetypeConfig` ScriptableObject defining its stats. The `ShipAuthoring` baker already handles conversion to ECS — no new baking code needed.

**Ship variant stat profiles** (based on StarterMiningBarge as Small baseline):
- **Small Barge** (StarterMiningBarge renamed): Mass 1000, Thrust 5000, Speed 100, Mining 1.0, Slots 4, Cargo 100
- **Medium Barge**: Mass 2500, Thrust 8000, Speed 75, Mining 1.5, Slots 6, Cargo 250
- **Heavy Barge**: Mass 5000, Thrust 12000, Speed 50, Mining 2.0, Slots 8, Cargo 500

**Alternatives considered**:
- Procedural ship assembly from config: Rejected — over-engineering for 3 variants; manual is faster and gives designer full control.
- Using Ship1-Ship5 directly: Rejected — they're generic sci-fi, not mining-industrial themed; custom assembly from parts creates the right aesthetic.

## R-006: Station Module Assembly and Preset Architecture

**Decision**: Create station presets as root prefabs with positioned child module instances. Use a `StationPresetConfig` ScriptableObject to document the module composition (for future procedural generation). The 56 station modules follow a consistent naming pattern: `MS2_{Type}_{Color} Variant.prefab`. Use grey color variants as the default industrial palette.

**Module categories** (14 types × 4 colors):
- Structural: Antennas, Bridge, Conexion, Connect, Control, Tower, TowerB
- Functional: Energy, Habitat, Hangars, ModuleB, Modules, Science, Storage

**Preset compositions**:
- **Small Mining Relay** (~4 modules): Control_grey + Storage_grey × 2 + Antennas_grey + Connect_grey (connectors)
- **Medium Refinery Hub** (~10 modules): Bridge_grey + Hangars_grey + Modules_grey × 2 + Storage_grey × 2 + Energy_grey + Habitat_grey + Tower_grey + Connect_grey × 2 (connectors)

**Rationale**: Manual prefab assembly ensures visual quality and proper module alignment. Grey color provides industrial cohesion matching the CGPitbull style anchor. StationPresetConfig documents the composition for future procedural generation (Phase 2+) without building that system now.

**Alternatives considered**:
- Runtime assembly from config: Out of scope — stations are static visual landmarks, not dynamic structures yet.
- All 4 color variants: Rejected — grey establishes the industrial tone; other colors available for future faction theming.

## R-007: LOD Strategy for Premium Assets

**Decision**: Configure LOD groups on asteroid prefabs (3 levels: full, 50% triangles, billboard/point). Ship barge prefabs get 2 LOD levels (full detail, simplified). Station modules skip LODs this phase — they're single large objects at fixed positions with the 5ms budget.

**Rationale**: Asteroids are the highest-count objects (300) at various distances — LODs have the biggest performance impact there. Ships are always nearby (camera follows), so only a simplified fallback is needed for multi-ship preview. Stations are few, large, and often viewed at distance — the 5ms budget accommodates them without LODs.

**Alternatives considered**:
- Unity HLOD (Hierarchical LOD): Over-engineering — standard LODGroup is sufficient for 300 entities.
- Impostors for distant asteroids: Deferred — standard LODs first; impostors if profiling shows need.
