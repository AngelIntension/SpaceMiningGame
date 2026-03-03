# Implementation Plan: Data-Driven World Config

**Branch**: `009-data-driven-world-config` | **Date**: 2026-03-03 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-data-driven-world-config/spec.md`

## Summary

Replace all remaining hard-coded game entity configuration with data-driven ScriptableObjects. This creates StationDefinition and WorldDefinition SOs to consolidate station data, a DockingConfigBlob pipeline to feed docking parameters into Burst-compiled ECS, CameraConfig and InteractionConfig SOs for designer-tunable camera/input, and inventory capacity derivation from ship archetype. Editor tooling (SceneConfigValidator, WorldDefinition custom editor) catches misconfiguration at edit time. Asset folder reorganization standardizes station asset locations.

## Technical Context

**Language/Version**: C# 9.0 / .NET Framework 4.7.1
**Primary Dependencies**: Unity 6 (6000.3.10f1), URP 17.3.0, Entities 1.3.2, Burst, VContainer 1.16.7, UniTask 2.5.10
**Storage**: ScriptableObject assets (serialized YAML), BlobAssets (ECS runtime)
**Testing**: NUnit + Unity Test Framework (EditMode preferred for pure logic)
**Target Platform**: Standalone Windows 64-bit
**Project Type**: Unity 3D game (hybrid DOTS/MonoBehaviour)
**Performance Goals**: 60 FPS on mid-range PC, zero GC in hot loops
**Constraints**: Burst-compiled systems cannot access managed objects; blob assets required for config
**Scale/Scope**: 2 stations (extensible), 3 ship archetypes, 33 assembly definitions, 521 existing tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Functional & Immutable First | PASS | All new SOs are read-only config. CameraState extended with immutable limit fields. StationData/WorldState remain sealed records. No mutable state introduced. |
| II. Predictability & Testability | PASS | CameraReducer stays pure static. All new OnValidate logic is unit-testable. Blob baking follows established deterministic pattern. |
| III. Performance by Default | PASS | DockingConfigBlob eliminates managed object access in Burst system. No new GC allocations in hot paths. BlobAsset read is cache-friendly. |
| IV. Data-Oriented Design | PASS | This spec IS the data-oriented principle in action — converting hard-coded values to ScriptableObject data. Blob assets for ECS. |
| V. Modularity & Extensibility | PASS | Two new feature assemblies (Station, World) with explicit dependencies. New station types require only asset creation. |
| VI. Explicit Over Implicit | PASS | All wiring is explicit VContainer registration. StationDefinition is the single source of truth replacing 4 manual sync points. |
| Editor Automation (MCP) | PASS | Compilation verification and console monitoring gates apply to all script changes. |
| Player Documentation | PASS | This spec does not change player-facing behavior (config values preserved). No HOWTOPLAY.md update required. |

**No constitution violations. No complexity tracking needed.**

## Post-Design Constitution Re-Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Functional & Immutable First | PASS | CameraState extended with readonly limit fields via `with` expression at init. StationServicesConfig moved between assemblies with no field changes. |
| II. Predictability & Testability | PASS | DockingConfigBlobBakingSystem follows OreTypeBlobBakingSystem pattern. All new types fully testable in EditMode. |
| III. Performance by Default | PASS | DockingConfigBlob is a flat struct read via singleton query — zero overhead vs hard-coded constants. |
| IV. Data-Oriented Design | PASS | Five new ScriptableObjects, one new BlobAsset. All static data authored via SOs. |
| V. Modularity & Extensibility | PASS | `Features.Station` assembly at layer 2 (low coupling). `Features.World` at layer 3. Clean dependency graph with no cycles. |
| VI. Explicit Over Implicit | PASS | Station ID flows from StationDefinition SO → all consumers. No implicit wiring. |

## Project Structure

### Documentation (this feature)

```text
specs/009-data-driven-world-config/
├── plan.md              # This file
├── research.md          # Phase 0 output — assembly placement, blob pattern, value mismatches
├── data-model.md        # Phase 1 output — entity definitions, relationships, assembly changes
├── quickstart.md        # Phase 1 output — designer workflows
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Assets/
├── Features/
│   ├── Station/                    # NEW — station definition types
│   │   ├── Data/
│   │   │   ├── StationDefinition.cs
│   │   │   ├── StationType.cs
│   │   │   ├── StationServicesConfig.cs    # MOVED from StationServices/Data/
│   │   │   ├── Definitions/
│   │   │   │   ├── SmallMiningRelay.asset  # NEW
│   │   │   │   └── MediumRefineryHub.asset # NEW
│   │   │   ├── ServiceConfigs/             # MOVED from StationServices/Data/Assets/StationConfigs/
│   │   │   ├── Presets/                    # MOVED from Base/Data/ (.asset files only)
│   │   │   └── RawMaterials/               # MOVED from StationServices/Data/Assets/RawMaterials/
│   │   ├── Tests/
│   │   │   └── StationDefinitionTests.cs
│   │   └── VoidHarvest.Features.Station.asmdef
│   │
│   ├── World/                      # NEW — world definition
│   │   ├── Data/
│   │   │   ├── WorldDefinition.cs
│   │   │   └── DefaultWorld.asset  # NEW
│   │   ├── Tests/
│   │   │   └── WorldDefinitionTests.cs
│   │   └── VoidHarvest.Features.World.asmdef
│   │
│   ├── Docking/
│   │   ├── Data/
│   │   │   ├── DockingConfig.cs            # MODIFIED — 4 new fields
│   │   │   ├── DockingConfigBlob.cs        # NEW — blob asset + singleton component
│   │   │   └── DockingComponents.cs        # EXISTING
│   │   ├── Systems/
│   │   │   ├── DockingSystem.cs            # MODIFIED — read from blob instead of constants
│   │   │   └── DockingConfigBlobBakingSystem.cs  # NEW
│   │   └── Tests/
│   │       └── DockingConfigBlobTests.cs   # NEW
│   │
│   ├── Camera/
│   │   ├── Data/
│   │   │   ├── CameraConfig.cs             # NEW
│   │   │   └── DefaultCameraConfig.asset   # NEW
│   │   ├── Systems/
│   │   │   └── CameraReducer.cs            # MODIFIED — read limits from state
│   │   ├── Views/
│   │   │   └── CameraView.cs               # MODIFIED — inject CameraConfig
│   │   └── Tests/
│   │       └── CameraConfigTests.cs        # NEW
│   │
│   ├── Input/
│   │   ├── Data/
│   │   │   ├── InteractionConfig.cs        # NEW
│   │   │   └── DefaultInteractionConfig.asset  # NEW
│   │   ├── Views/
│   │   │   └── InputBridge.cs              # MODIFIED — inject InteractionConfig
│   │   └── Tests/
│   │       └── InteractionConfigTests.cs   # NEW
│   │
│   ├── HUD/
│   │   └── Views/RadialMenu/
│   │       └── RadialMenuController.cs     # MODIFIED — inject InteractionConfig
│   │
│   ├── Ship/
│   │   └── Data/
│   │       └── ShipArchetypeConfig.cs      # MODIFIED — add CargoSlots field
│   │
│   ├── StationServices/
│   │   ├── Data/
│   │   │   ├── StationServicesConfig.cs    # DELETED (moved to Station/)
│   │   │   └── StationServicesConfigMap.cs # DELETED
│   │   └── Views/
│   │       ├── StationServicesMenuController.cs    # MODIFIED — use WorldDefinition
│   │       ├── RefineOresPanelController.cs        # MODIFIED — use StationDefinition
│   │       └── BasicRepairPanelController.cs       # MODIFIED — use StationDefinition
│   │
│   ├── Targeting/
│   │   └── Views/
│   │       └── TargetableStation.cs        # MODIFIED — derive from StationDefinition
│   │
│   └── Base/
│       └── Data/
│           └── StationPresetConfig.cs      # EXISTING (stays, .cs file unchanged)
│
├── Core/
│   ├── State/
│   │   ├── CameraState.cs                  # MODIFIED — add limit fields
│   │   └── InventoryState.cs               # EXISTING (initialization change only)
│   ├── Editor/
│   │   ├── SceneConfigValidator.cs         # NEW
│   │   └── WorldDefinitionEditor.cs        # NEW
│   ├── RootLifetimeScope.cs                # MODIFIED — read from WorldDefinition
│   └── SceneLifetimeScope.cs               # MODIFIED — register new configs
```

**Structure Decision**: Two new feature assemblies (`Station`, `World`) follow the established feature isolation pattern. `Station` sits at layer 2 (same as `Base`), `World` at layer 3 (same as `Ship`). Editor tooling goes in `Core/Editor/` (existing Editor-only assembly). The `StationServicesConfig` type moves to `Features.Station` to resolve the circular dependency between StationDefinition and StationServices.
