# Implementation Plan: VoidHarvest Master Vision & Architecture

**Branch**: `001-master-vision-architecture` | **Date**: 2026-02-26 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-master-vision-architecture/spec.md`

## Summary

Establish the foundational architecture for VoidHarvest: a functional/immutable-first, hybrid DOTS+MonoBehaviour game architecture for a 3D space mining simulator. This plan covers package installation, project scaffolding, core infrastructure (state store, event bus, reducer framework), and the Phase 0 MVP implementation path (camera, EVE-style controls, ship physics, mining, inventory, procedural asteroids, HUD).

The technical approach uses C# 9.0 `record` types for immutable domain state, `readonly struct` for Burst-compatible value types, DOTS Entities 1.3.x for simulation, VContainer for DI, UniTask Channels for zero-allocation event bus, and Cinemachine 3.1.x for the 3rd-person camera.

## Technical Context

**Language/Version**: C# 9.0 / .NET Standard 2.1 (Mono runtime, .NET Framework 4.7.1 target)
**Engine**: Unity 6 (6000.3.10f1), URP 17.3.0
**Primary Dependencies**:
- DOTS Entities 1.3.x + Entities Graphics 1.3.x (simulation/rendering)
- Burst 1.8.28 + Collections 2.6.2 (already present via URP)
- Cinemachine 3.1.x (camera system)
- Addressables 2.3.x (runtime asset loading)
- VContainer 1.16.x (dependency injection)
- UniTask 2.5.x (async + event bus)
- System.Collections.Immutable (immutable collections via NuGetForUnity)
- Input System 1.18.0 (already installed)

**Storage**: N/A for MVP (session-only state; save/load deferred)
**Testing**: NUnit + Unity Test Framework 1.6.0 (EditMode for pure logic, PlayMode for integration)
**Target Platform**: Standalone Windows 64-bit
**Project Type**: Unity game (3D space mining simulator)
**Performance Goals**: 60 FPS minimum on mid-range PC (GTX 1060 / RX 580); zero GC in hot loops; <2ms per reducer; field gen <100ms
**Constraints**: <500 asteroids in MVP; single-player only; C# 9.0 (no `record struct`)
**Scale/Scope**: Phase 0 MVP — 7 feature systems, ~80 source files, ~20 test files (91 tasks total)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Evidence |
|---|-----------|--------|----------|
| I | Functional & Immutable First | **PASS** | All domain state uses `record` types; changes via pure reducers `(State, Action) → State`; `with` expressions; ImmutableArray/ImmutableDictionary for collections; no mutable domain data |
| II | Predictability & Testability | **PASS** | Every system: `InputState → PureFunction → NewState`; no static mutable state; all deps injected via VContainer; TDD mandatory |
| III | Performance by Default | **PASS** | DOTS/ECS + Burst for simulation; Entities Graphics for batch rendering; NativeArray/NativeQueue for hot paths; 60 FPS target in MVP acceptance criteria |
| IV | Data-Oriented Design | **PASS** | ECS archetypes for entities; ScriptableObjects for static data; BlobAssets for Burst-accessible config; composition only, no inheritance hierarchies |
| V | Modularity & Extensibility | **PASS** | Feature-per-folder with assembly definitions; EventBus for cross-system communication; no cross-feature direct writes |
| VI | Explicit Over Implicit | **PASS** | VContainer explicit registration; no reflection DI; all data flow traceable; sync systems clearly documented |
| VP-Camera | 3rd-person only | **PASS** | Cinemachine OrbitalFollow Sphere mode; no 1st-person; free-look isolated from heading; speed-based zoom |
| VP-Controls | EVE-style | **PASS** | PilotCommand record with mouse targeting, radial menu, hotbar, keyboard thrust supplement |
| VP-CoreLoop | <2s feedback | **PASS** | <500ms mining yield, <200ms refining, <300ms tech unlock per spec |
| TDD | Mandatory | **PASS** | TDD strategy defined per system; MVP-10 requires 100% reducer coverage |
| MVP Scope | Respected | **PASS** | Phase 1-3 systems stubbed only; explicit out-of-scope list in spec |

### Known Constitution Deviations (Justified)

| Deviation | Justification | Comment Required |
|-----------|---------------|-----------------|
| ECS components are mutable structs | Unity DOTS requirement — IComponentData cannot be readonly. Reducer logic remains pure; ISystem is the only write point. | `// CONSTITUTION DEVIATION: ECS mutable shell` |
| EventBus bridge uses static reference | DOTS SystemBase cannot use constructor injection. Bridge singleton is the only practical way to access managed EventBus from ECS boundary. | `// CONSTITUTION DEVIATION: DOTS SystemBase cannot use constructor injection` |
| `record struct` unavailable | C# 9.0 limitation. Using `readonly struct` with manual equality for value types. `record` (reference type) for domain state. | No code comment needed — architectural decision |
| Direct `[SerializeField]` asset references in Phases 3-7 | `[SerializeField]` is NOT `Resources.Load`; constitution prohibits `Resources.Load`, not inspector references. T086 migrates to Addressables for runtime-loaded assets. | No code comment needed |

### Post-Design Re-Check

All decisions from Phase 0 research and Phase 1 design maintain constitution compliance:
- Split canonical authority (ECS for simulation, central store for player-domain) preserves both immutability (Principle I) and performance (Principle III)
- NativeQueue action buffer enables zero-GC ECS-to-reducer communication (Principle III)
- Managed-Unmanaged-Managed sandwich update order ensures deterministic frame processing (Principle II)

## Project Structure

### Documentation (this feature)

```text
specs/001-master-vision-architecture/
├── plan.md                          # This file
├── research.md                      # Phase 0: Technology decisions
├── data-model.md                    # Phase 1: All entity types and relationships
├── quickstart.md                    # Phase 1: Setup and first-run guide
├── contracts/
│   ├── reducer-interfaces.md        # Reducer contracts for all systems
│   ├── eventbus-interface.md        # IEventBus contract
│   └── state-store-interface.md     # IStateStore contract
└── tasks.md                         # Phase 2: Task breakdown (via /speckit.tasks)
```

### Source Code (repository root)

```text
Assets/
├── Features/                        # One folder per major system
│   ├── Camera/                      # 3rd-person orbiting follow camera
│   │   ├── Data/                    # CameraState record, ICameraAction types
│   │   ├── Systems/                 # CameraReducer (pure static)
│   │   ├── Views/                   # CameraView (drives Cinemachine)
│   │   └── Tests/                   # CameraReducer unit tests
│   ├── Input/                       # EVE-style controls, PilotCommand
│   │   ├── Data/                    # PilotCommand, ThrustInput, RadialMenuChoice
│   │   ├── Systems/                 # (none for MVP — input is MonoBehaviour only)
│   │   ├── Views/                   # InputBridge (MonoBehaviour)
│   │   └── Tests/                   # PilotCommand construction tests
│   ├── Ship/                        # Ship state, physics, modules
│   │   ├── Data/                    # ShipState record, ShipFlightMode, ECS components
│   │   ├── Systems/                 # ShipStateReducer, ShipPhysicsSystem (ISystem)
│   │   ├── Views/                   # ShipView (applies position to Transform)
│   │   └── Tests/                   # ShipStateReducer unit tests, physics perf tests
│   ├── Mining/                      # Beam targeting, yield reducers
│   │   ├── Data/                    # MiningSessionState, MiningYieldResult, OreTypeDefinition
│   │   ├── Systems/                 # MiningReducer, MiningBeamSystem (ISystem)
│   │   ├── Views/                   # MiningBeamView (particle system)
│   │   └── Tests/                   # MiningReducer + CalculateYield unit tests
│   ├── Resources/                   # Resource / inventory system
│   │   ├── Data/                    # InventoryState, ResourceStack
│   │   ├── Systems/                 # InventoryReducer
│   │   ├── Views/                   # (HUD displays inventory — see HUD/)
│   │   └── Tests/                   # InventoryReducer unit tests
│   ├── Procedural/                  # Asteroid field generation
│   │   ├── Data/                    # AsteroidFieldConfig, AsteroidData, OreDistribution
│   │   ├── Systems/                 # AsteroidFieldGeneratorJob (Burst), AsteroidFieldSystem
│   │   ├── Views/                   # (Entities Graphics handles rendering)
│   │   └── Tests/                   # Determinism tests, perf tests
│   └── HUD/                        # In-game UI (UI Toolkit)
│       ├── Data/                    # HUD data bindings
│       ├── Systems/                 # HUD state projection
│       ├── Views/                   # HUDView (UIDocument bindings)
│       └── Tests/                   # HUD update tests
├── Core/                            # Shared infrastructure
│   ├── EventBus/                    # IEventBus + UniTaskEventBus implementation
│   ├── State/                       # IStateStore + GameStateReducer + store implementation
│   ├── Pools/                       # ObjectPool<T> (if needed)
│   └── Extensions/                  # C# extension methods, Option<T> helper
├── Settings/                        # URP configs, volume profiles (existing)
└── Scenes/
    └── GameScene.unity              # Main gameplay scene (renamed from SampleScene)
```

**Structure Decision**: Unity feature-per-folder structure per constitution § Project Structure. Each feature has Data/Systems/Views/Tests sub-structure. Assembly definitions enforce dependency boundaries. Core infrastructure is shared.

## Complexity Tracking

| Deviation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| ECS mutable components alongside immutable records | Unity DOTS requires mutable IComponentData for Burst/Jobs. Cannot use `record` in ECS. | Making everything ECS-only loses reducer pattern and testability. Making everything records-only loses Burst performance. Hybrid split is the minimum viable approach. |
| NativeQueue action buffer for ECS→Store bridge | Burst jobs cannot call managed code. Need an unmanaged buffer to collect actions during simulation, then drain on main thread. | Direct dispatch from ECS impossible (Burst limitation). Event entities add one-frame latency. NativeQueue is the standard DOTS pattern for this. |
| EventBus bridge singleton | DOTS SystemBase has no constructor injection support. VContainer cannot register DOTS systems. | Could use a managed singleton ECS component, but that is also global state. The bridge singleton is simpler, documented, and serves the same purpose. |
