# Implementation Plan: Premium Visuals Asset Integration

**Branch**: `002-premium-visuals-integration` | **Date**: 2026-02-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-premium-visuals-integration/spec.md`

## Summary

Integrate four premium Unity Asset Store packs into VoidHarvest — Nebula Skybox Pack Vol. II (12 HDRI environments), SF MINING Asteroids M2 (6 mineral mesh variants), Modular Space Ship Pack by Retora (41 modular parts), and MODULAR Space Station #MS2 (56 station modules). Add asteroid depletion visual feedback (mass-proportional sizing, continuous shrink with crumble pauses, fade-out removal). Create three mining barge variants and two station presets. All changes preserve the functional/immutable architecture, DOTS pipeline, and 60 FPS target.

## Technical Context

**Language/Version**: C# 9.0 / .NET Framework 4.7.1
**Primary Dependencies**: Unity 6 (6000.3.10f1), URP 17.3.0, Entities 1.3.2, Entities Graphics 1.3.2, Burst, UniTask 2.5.10, VContainer 1.16.7, Input System 1.18.0
**Storage**: ScriptableObject configs (design-time), ECS components (runtime), Addressables (asset loading)
**Testing**: NUnit + Unity Test Framework (EditMode for pure logic, PlayMode for ECS integration)
**Target Platform**: Standalone Windows 64-bit
**Project Type**: Unity game (3D space mining simulator)
**Performance Goals**: 60 FPS minimum (mid-range PC, GTX 1060 / RX 580); asteroid field < 2 ms/frame; station scene < 5 ms/frame; zero GC in hot loops
**Constraints**: All domain data immutable (records/readonly structs); reducer pattern for state; DOTS/Burst for simulation; MonoBehaviour only for view layer
**Scale/Scope**: 300 asteroids max, 3 ship variants, 2 station presets, 12 skybox variants, 56 station modules available

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gate

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Functional & Immutable First | PASS | New ScriptableObject configs are read-only. AsteroidComponent fields are mutable ECS shell (existing pattern). New systems (AsteroidScaleSystem, AsteroidDestroySystem) are pure Burst-compiled transforms on ECS data — no state mutation outside ECS. SkyboxController is view-layer MonoBehaviour (allowed). |
| II. Predictability & Testability | PASS | AsteroidScaleSystem is deterministic: same depletion → same scale. Crumble thresholds are pure bitmask checks. SkyboxConfig is injected, not ambient. All new logic testable via EditMode tests on pure functions. |
| III. Performance by Default | PASS | New systems are Burst-compiled. LOD groups on asteroids. Entity destruction frees rendering budget. Performance budgets defined: 2 ms asteroids, 5 ms station. |
| IV. Data-Oriented Design | PASS | AsteroidVisualMappingConfig is ScriptableObject (static data). Runtime data in ECS components. No inheritance hierarchies. Composition via ECS archetypes. |
| V. Modularity & Extensibility | PASS | SkyboxConfig, AsteroidVisualMappingConfig, StationPresetConfig are all self-contained modules. No cross-feature field writes. New systems in existing feature assembly definitions. |
| VI. Explicit Over Implicit | PASS | Visual mapping is explicit config (designer assigns meshes to ore types). Skybox is explicit per-scene reference. No convention-based wiring. |

### Post-Design Gate

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Functional & Immutable First | PASS | AsteroidComponent.PristineTintedColor (float4), CrumbleThresholdsPassed (byte bitmask), CrumblePauseTimer (float), FadeOutTimer (float) — mutable ECS fields within existing mutable shell pattern. No new mutable domain records. |
| II. Predictability & Testability | PASS | Scale formula is pure: `Radius * lerp(0.3, 1.0, RemainingMass/InitialMass)`. Threshold detection is pure bitmask comparison. Crumble/fade timers are deterministic countdown. |
| III. Performance by Default | PASS | 4 new float fields (PristineTintedColor is float4 = 16 bytes) + 1 byte on AsteroidComponent = 29 bytes added per entity × 300 = 8.7 KB total. AsteroidScaleSystem and AsteroidDestroySystem are Burst jobs. Entity destruction reduces entity count over time. |
| IV. Data-Oriented Design | PASS | All new data in ECS components or ScriptableObjects. No MonoBehaviour state for game logic. |
| V. Modularity & Extensibility | PASS | AsteroidScaleSystem and AsteroidDestroySystem are independent systems in existing Mining assembly. SkyboxController is in Camera assembly. Station presets in Base assembly. |
| VI. Explicit Over Implicit | PASS | Crumble thresholds are explicit bitmask values. Fade-out duration is explicit config. No hidden state. |

**No violations. No complexity tracking entries needed.**

## Project Structure

### Documentation (this feature)

```text
specs/002-premium-visuals-integration/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 research output
├── data-model.md        # Phase 1 data model
├── quickstart.md        # Phase 1 quickstart guide
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
Assets/
├── Features/
│   ├── Camera/
│   │   ├── Data/
│   │   │   └── SkyboxConfig.cs              # NEW: ScriptableObject for per-scene skybox
│   │   │   └── GameSceneSkybox.asset         # NEW: SkyboxConfig instance for GameScene
│   │   └── Views/
│   │       └── SkyboxController.cs           # NEW: MonoBehaviour applying skybox + rotation
│   ├── Mining/
│   │   ├── Data/
│   │   │   └── MiningComponents.cs           # MODIFIED: 4 new fields on AsteroidComponent
│   │   ├── Systems/
│   │   │   ├── AsteroidDepletionSystem.cs    # MODIFIED: tint blending with ore color
│   │   │   ├── AsteroidScaleSystem.cs        # NEW: depletion shrink + crumble pauses
│   │   │   └── AsteroidDestroySystem.cs      # NEW: entity cleanup after fade-out
│   │   └── Tests/
│   │       ├── AsteroidScaleTests.cs         # NEW: scale formula + threshold tests
│   │       └── AsteroidDestroyTests.cs       # NEW: fade-out + removal tests
│   ├── Procedural/
│   │   ├── Data/
│   │   │   ├── AsteroidVisualMappingConfig.cs    # NEW: ScriptableObject ore→mesh+tint
│   │   │   └── AsteroidVisualMapping.asset       # NEW: config instance
│   │   ├── Systems/
│   │   │   └── AsteroidFieldSystem.cs            # MODIFIED: use visual mapping for mesh selection + tint
│   │   ├── Tests/
│   │   │   └── AsteroidFieldVisualMappingTests.cs  # NEW: visual mapping + cluster variety tests
│   │   └── Views/
│   │       └── AsteroidPrefabAuthoring.cs        # MODIFIED: support multi-prefab references
│   ├── Ship/
│   │   ├── Data/
│   │   │   ├── StarterMiningBarge.asset          # MODIFIED: DisplayName → "Small Mining Barge"
│   │   │   ├── MediumMiningBarge.asset           # NEW: ShipArchetypeConfig instance
│   │   │   └── HeavyMiningBarge.asset            # NEW: ShipArchetypeConfig instance
│   │   └── Prefabs/
│   │       ├── SmallMiningBarge.prefab           # NEW: Retora modular assembly
│   │       ├── MediumMiningBarge.prefab          # NEW: Retora modular assembly
│   │       └── HeavyMiningBarge.prefab           # NEW: Retora modular assembly
│   └── Base/
│       ├── Data/
│       │   ├── StationPresetConfig.cs            # NEW: ScriptableObject for presets
│       │   ├── SmallMiningRelay.asset            # NEW: StationPresetConfig instance
│       │   └── MediumRefineryHub.asset           # NEW: StationPresetConfig instance
│       └── Prefabs/
│           ├── SmallMiningRelay.prefab           # NEW: assembled station preset
│           └── MediumRefineryHub.prefab          # NEW: assembled station preset
├── Scenes/
│   └── TestScene_Station.unity                   # NEW: test scene for station presets
└── Settings/
    └── SpaceSkybox.mat                           # EXISTING: fallback skybox (unchanged)
```

**Structure Decision**: All new code goes into existing feature folders following the established Data/Systems/Views/Tests sub-structure. No new top-level folders. Station work begins populating the previously-skeleton Base feature folder. SkyboxController lives in Camera (environment rendering is a camera-adjacent concern).

## Implementation Order

The implementation follows user story priority (P1→P4) with shared prerequisites first:

### Phase A: Material Verification (FR-001)
Verify all four asset packs render correctly under URP 17.3.0. Run Render Pipeline Converter if needed. This unblocks all subsequent work.

### Phase B: Skybox System (P1 — FR-002 through FR-005)
1. Create SkyboxConfig ScriptableObject
2. Create SkyboxController MonoBehaviour
3. Create GameScene SkyboxConfig instance referencing a nebula material
4. Wire into GameScene
5. Test: load scene, verify skybox, rotation, ambient lighting, fallback

### Phase C: Asteroid Visuals + Depletion (P2 — FR-006 through FR-009, FR-018 through FR-021)
1. Create AsteroidVisualMappingConfig ScriptableObject
2. Create config instance with ore→mesh+tint mapping
3. Modify AsteroidFieldSystem to use visual mapping during spawn
4. Modify AsteroidPrefabAuthoring for multi-prefab support
5. Add LOD groups to asteroid prefabs
6. Extend AsteroidComponent with crumble/fade fields + PristineTintedColor
7. Modify AsteroidDepletionSystem to use per-entity PristineTintedColor for ore tint blending
8. Configure Alpha Clipping on asteroid materials (enables fade-out via alpha)
9. Create AsteroidScaleSystem (shrink + crumble pauses + start fade timer)
10. Create AsteroidDestroySystem (fade-out alpha clip + untargetable + entity removal)
11. Write tests for scale formula, threshold detection, fade-out timing, visual mapping
12. Test: spawn field, verify mesh variety, mine to depletion, verify shrink/crumble/removal

### Phase D: Mining Barge Variants (P3 — FR-010 through FR-012)
1. Update StarterMiningBarge.asset DisplayName to "Small Mining Barge"
2. Create MediumMiningBarge.asset and HeavyMiningBarge.asset configs
3. Assemble 3 barge prefabs from Retora modular parts in Unity Editor
4. Configure ShipAuthoring on each prefab
5. Add LOD groups to ship prefabs
6. Test: fly each variant, verify controls, run existing tests for regression

### Phase E: Station Presets (P4 — FR-013 through FR-014, FR-017)
1. Create StationPresetConfig ScriptableObject
2. Assemble SmallMiningRelay and MediumRefineryHub prefabs from Station_MS2 modules
3. Create config instances documenting compositions
4. Create TestScene_Station
5. Test: verify assembly quality, material rendering, performance budget

### Phase F: Performance Validation (FR-015 through FR-017)
1. Profile asteroid field with premium meshes (target: < 2 ms)
2. Profile station scene (target: < 5 ms)
3. Profile full GameScene with all premium visuals (target: 60 FPS)
4. Run all existing and new tests — zero regressions
