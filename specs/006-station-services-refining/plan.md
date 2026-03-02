# Implementation Plan: Station Services Menu & Data-Driven Refining

**Branch**: `006-station-services-refining` | **Date**: 2026-03-01 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-station-services-refining/spec.md`

## Summary

Implement the Station Services Menu and data-driven refining system to close the mine-to-economy loop. Players dock at stations to transfer cargo, sell resources for credits, queue time-based refining jobs (ore → raw materials), and repair hulls. All state flows through pure reducers with immutable records. OreDefinition ScriptableObjects are extended with embedded refining outputs. A new `StationServicesState` replaces the `RefiningState` stub in `GameLoopState`, housing player credits, per-station storage inventories, and per-station refining job queues.

**Credits are integer-typed (`int`)** — no fractional credits. All prices (BaseValue, RefiningCreditCostPerUnit, RepairCostPerHP) are also `int`. Repair cost calculation uses ceiling rounding from the float intermediate.

## Technical Context

**Language/Version**: C# 9.0 / .NET Framework 4.7.1 (Unity 6000.3.10f1)
**Primary Dependencies**: Unity Entities 1.3.2, VContainer 1.16.7, UniTask 2.5.10, System.Collections.Immutable (via NuGetForUnity 4.5.0), UI Toolkit
**Storage**: In-memory immutable state (StateStore singleton) + ScriptableObjects for designer data. No persistent storage (save/load out of scope).
**Testing**: NUnit + Unity Test Framework. EditMode for pure logic, PlayMode for UI integration. TDD mandatory (Red-Green-Refactor).
**Target Platform**: Windows 64-bit Standalone
**Project Type**: Unity 3D game (hybrid DOTS/ECS + MonoBehaviour)
**Performance Goals**: 60 FPS minimum on mid-range PC (GTX 1060 / RX 580). Zero GC in hot loops. <2ms frame spikes.
**Constraints**: C# 9.0 only (no `record struct`). Functional/immutable-first. No mutable globals or static singletons for game logic.
**Scale/Scope**: Single-player. 2 stations, 3 ore types, 6 raw material types, ~3 concurrent refining jobs per station.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Functional & Immutable First | **PASS** | All new state types are `sealed record`. Collections use `ImmutableArray`/`ImmutableDictionary`. State transitions via pure reducers. |
| II. Predictability & Testability | **PASS** | All reducers are pure static functions. RefiningMath is a pure static class. No hidden mutable state. TDD for all logic. |
| III. Performance by Default | **PASS** | Station services are UI-layer (MonoBehaviour + UI Toolkit). Refining tick is lightweight (check timers, not hot loop). No Burst needed — infrequent operations. |
| IV. Data-Oriented Design | **PASS** | OreDefinition, RawMaterialDefinition, StationServicesConfig are ScriptableObjects. Runtime data in immutable records. Composition over inheritance. |
| V. Modularity & Extensibility | **PASS** | New `VoidHarvest.Features.StationServices` assembly with explicit dependencies. Cross-system communication via EventBus. New data types addable via Create Asset menu. |
| VI. Explicit Over Implicit | **PASS** | VContainer DI with `[Inject]` method pattern. No magic wiring. All event subscriptions explicit. |
| Player Documentation | **PASS** | FR-045 mandates HOWTOPLAY.md update. Delivery gate enforced. |
| Unity MCP Verification | **PASS** | Compile-check after scripts, console monitoring, test execution via MCP during implementation. |

**Cross-cutting reducer pattern**: `StationServicesReducer` handles most actions on its own state slice. Three cross-cutting operations (transfer ship↔station, repair) are coordinated in `CompositeReducer` by calling into both `StationServicesReducer` and `InventoryReducer`/`ShipStateReducer`. This preserves atomicity while keeping individual reducers focused. The composite coordination is a pure function — no constitution deviation required.

## Project Structure

### Documentation (this feature)

```text
specs/006-station-services-refining/
├── plan.md              # This file
├── research.md          # Phase 0: design decisions
├── data-model.md        # Phase 1: complete data model
├── quickstart.md        # Phase 1: developer testing guide
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Assets/
├── Features/
│   ├── StationServices/                    # NEW: Station economy feature
│   │   ├── Data/
│   │   │   ├── StationServicesState.cs     # Sealed record: credits + storages + jobs
│   │   │   ├── StationStorageState.cs      # Per-station unlimited inventory
│   │   │   ├── RefiningJobState.cs         # Job lifecycle record
│   │   │   ├── RefiningJobStatus.cs        # Enum: Active, Completed
│   │   │   ├── RefiningOutputConfig.cs     # Readonly struct: captured output params
│   │   │   ├── MaterialOutput.cs           # Readonly struct: materialId + quantity
│   │   │   ├── StationServicesActions.cs   # IStationServicesAction + all action records
│   │   │   ├── StationServicesEvents.cs    # Event structs for UI feedback
│   │   │   ├── StationServicesConfig.cs    # SO: per-station service capabilities
│   │   │   └── GameServicesConfig.cs       # SO: global settings (starting credits)
│   │   ├── Systems/
│   │   │   ├── StationServicesReducer.cs   # Pure reducer for station services state
│   │   │   ├── StationStorageReducer.cs    # Pure helper for storage add/remove
│   │   │   └── RefiningMath.cs             # Pure yield calculation with per-unit rolling
│   │   ├── Views/
│   │   │   ├── StationServicesMenuController.cs  # Main menu (moved from Docking, expanded)
│   │   │   ├── CargoTransferPanelController.cs   # Bidirectional transfer UI
│   │   │   ├── SellResourcesPanelController.cs   # Sell with live preview
│   │   │   ├── RefineOresPanelController.cs      # Job queue, start job, progress
│   │   │   ├── BasicRepairPanelController.cs     # One-click repair
│   │   │   ├── RefiningJobSummaryController.cs   # Completed job review window
│   │   │   ├── CreditBalanceIndicator.cs         # Persistent credit display
│   │   │   ├── RefiningJobTicker.cs              # MonoBehaviour: ticks job timers
│   │   │   ├── StationServicesMenu.uxml          # Main menu layout (moved, expanded)
│   │   │   ├── StationServicesMenu.uss           # Main menu styles (moved, expanded)
│   │   │   ├── CargoTransferPanel.uxml
│   │   │   ├── SellResourcesPanel.uxml
│   │   │   ├── RefineOresPanel.uxml
│   │   │   ├── BasicRepairPanel.uxml
│   │   │   └── RefiningJobSummary.uxml
│   │   ├── Tests/
│   │   │   ├── StationServicesReducerTests.cs
│   │   │   ├── StationStorageReducerTests.cs
│   │   │   ├── RefiningMathTests.cs
│   │   │   ├── CargoTransferTests.cs
│   │   │   ├── SellResourcesTests.cs
│   │   │   ├── RepairTests.cs
│   │   │   └── RefiningJobLifecycleTests.cs
│   │   └── VoidHarvest.Features.StationServices.asmdef
│   │
│   ├── Mining/Data/
│   │   ├── OreDefinition.cs                # MODIFIED: +RefiningOutputs[], +RefiningCreditCostPerUnit (int), BaseValue float→int
│   │   └── RefiningOutputEntry.cs          # NEW: [Serializable] struct for ore refining config
│   │
│   ├── Resources/Data/
│   │   └── RawMaterialDefinition.cs        # NEW: ScriptableObject for processed materials (BaseValue: int)
│   │
│   └── Docking/Views/
│       ├── StationServicesMenuController.cs  # REMOVED (moved to StationServices)
│       ├── StationServicesMenu.uxml          # REMOVED (moved to StationServices)
│       └── StationServicesMenu.uss           # REMOVED (moved to StationServices)
│
├── Core/
│   ├── State/
│   │   ├── GameState.cs                    # MODIFIED: GameLoopState.Refining → .StationServices
│   │   └── GameStateReducer.cs             # MODIFIED: RefiningState → StationServicesState defaults
│   ├── RootLifetimeScope.cs                # MODIFIED: CompositeReducer + IStationServicesAction routing
│   └── SceneLifetimeScope.cs               # MODIFIED: +StationServicesConfig, +GameServicesConfig registration
│
└── Features/StationServices/Data/Assets/   # ScriptableObject instances
    ├── RawMaterials/
    │   ├── LuminiteIngots.asset
    │   ├── EnergiumDust.asset
    │   ├── FerroxSlabs.asset
    │   ├── ConductiveResidue.asset
    │   ├── AuraliteShards.asset
    │   └── QuantumEssence.asset
    ├── StationConfigs/
    │   ├── SmallMiningRelayServices.asset
    │   └── MediumRefineryHubServices.asset
    └── GameServicesConfig.asset
```

**Structure Decision**: New `Features/StationServices/` directory with standard `Data/Systems/Views/Tests/` sub-structure. RawMaterialDefinition placed in `Features/Resources/Data/` since raw materials are resource types used by multiple features. RefiningOutputEntry placed in `Features/Mining/Data/` alongside OreDefinition since it's a serialized field on that SO. The existing station menu stub in `Features/Docking/Views/` is moved to the new assembly.

### Assembly Dependency Changes

| Assembly | Change | New References |
|----------|--------|---------------|
| **VoidHarvest.Features.StationServices** | NEW | Core.Extensions, Core.State, Core.EventBus, Features.Mining, Features.Resources, Features.Docking, Features.Ship, VContainer, UniTask, Unity.Mathematics |
| **VoidHarvest.Features.Mining** | MODIFIED | +Features.Resources (for RawMaterialDefinition ref in RefiningOutputEntry) |
| **VoidHarvest.Features.StationServices.Tests** | NEW | StationServices, Core.State, Core.Extensions, Features.Mining, Features.Resources, nunit.framework |

## Complexity Tracking

No constitution violations. No complexity justifications needed.
