# Research: Developer & Designer Documentation Bootstrap

**Date**: 2026-03-03
**Feature**: 010-dev-designer-docs

## Overview

This is a documentation-only feature. No technology choices or external integrations are involved. Research focuses on cataloging the exact codebase artifacts that must be documented.

## Decision 1: Document Scope — Which Systems Get Full Docs

**Decision**: 12 shipped Phase 0 features get full system docs; 3 skeleton features (Fleet, TechTree, Economy) are excluded.

**Rationale**: Skeleton features contain only assembly definitions with no C# files. Documenting empty modules would create misleading docs that need immediate updating. The constitution says "one doc per feature module" — these are not yet feature modules.

**Alternatives considered**:
- Include skeleton stubs with "Coming in Phase X" content — rejected because it creates maintenance burden and the overview.md already covers roadmap.

## Decision 2: Mermaid Diagram Types Per Document Category

**Decision**: Use the following Mermaid diagram types:

| Document | Diagram Type | Rationale |
|----------|-------------|-----------|
| overview.md | `graph TD` (flowchart) | Best for showing feature modules and communication paths |
| state-management.md | `graph TD` + `sequenceDiagram` | Tree for state shape, sequence for action dispatch |
| event-system.md | `graph LR` (flowchart) | Publisher → Event → Subscriber flows |
| dependency-injection.md | `graph TD` | Scope hierarchy tree |
| data-pipeline.md | `sequenceDiagram` | Lifecycle flow: SO → Baker → Blob → System → View |
| System docs (each) | `graph TD` or `stateDiagram-v2` | Data flow or state machine as appropriate |
| assembly-map.md | `graph TD` | Dependency graph |

**Rationale**: Mermaid flowcharts (`graph`) are the most versatile and widely rendered. State diagrams are used where the system has an explicit state machine (Docking, Mining). Sequence diagrams show temporal flows (action dispatch, baking pipeline).

**Alternatives considered**:
- Class diagrams for data model — rejected because record types don't map cleanly to UML classes and the state shape tables are more readable.
- ER diagrams for entities — rejected because ECS components aren't relational and the table format is clearer.

## Decision 3: Designer Guide Terminology Approach

**Decision**: Designer guide docs use plain-language equivalents for all technical terms. A mapping table in each guide translates common terms (e.g., "ScriptableObject" → "configuration asset", "serialized field" → "configurable setting").

**Rationale**: The constitution mandates "non-programmer audience" and "no architectural jargon." However, Unity Editor UI uses terms like "ScriptableObject" in menus (e.g., `Create > VoidHarvest > Mining > Ore Definition`), so some Unity-specific terms must appear in asset path references. These are treated as proper nouns (the name of a menu item) rather than architectural jargon.

**Alternatives considered**:
- Completely avoid all Unity terms — rejected because designers use the Unity Editor and need to recognize menu paths.
- Include a mini-glossary in each guide — accepted as part of the approach.

## Decision 4: Cross-Reference Convention

**Decision**: Use relative markdown links for cross-references between docs (e.g., `[State Management](../architecture/state-management.md)`). Section anchors use GitHub-compatible auto-generated IDs.

**Rationale**: Relative links work in GitHub, VS Code, and most markdown renderers. Absolute paths would break if the repo is cloned to a different location.

**Alternatives considered**:
- No cross-references — rejected because the constitution requires self-consistency and the onboarding doc needs a reading order with links.

## Codebase Artifact Catalog

### Assemblies (38 total: 9 Core + 29 Features)

**Core (9)**: Extensions, Extensions.Tests, EventBus, EventBus.Tests, Pools, State, State.Tests, Editor, Editor.Tests

**Features (29)**: Base, Camera (+Tests), Docking (+Tests), Economy, Fleet, HUD (+Tests), Input (+Tests), Mining (+Tests), Procedural (+Tests), Resources (+Tests), Ship (+Tests), Station (+Tests), StationServices (+Tests), Targeting (+Tests), TechTree, Tests (cross-feature), World (+Tests)

### Event Types (25 total)

**Core/EventBus (12)**: StateChangedEvent\<T\>, DockingStartedEvent, DockingCompletedEvent, UndockingStartedEvent, UndockCompletedEvent, MiningStartedEvent, MiningStoppedEvent, MiningYieldEvent, OreChunkCollectedEvent, RadialMenuRequestedEvent, TargetSelectedEvent, ThresholdCrossedEvent

**StationServices (7)**: RefiningJobStartedEvent, RefiningJobCompletedEvent, RefiningJobCollectedEvent, ResourcesSoldEvent, CargoTransferredEvent, ShipRepairedEvent, CreditsChangedEvent

**Targeting (6)**: TargetLockedEvent, TargetUnlockedEvent, LockFailedEvent, LockSlotsFullEvent, TargetLostEvent, AllLocksClearedEvent

### ScriptableObject Types (24 total)

Camera (2): CameraConfig, SkyboxConfig
Docking (3): DockingConfig, DockingVFXConfig, DockingAudioConfig
Input (1): InteractionConfig
Mining (5): OreDefinition, OreChunkConfig, MiningVFXConfig, MiningAudioConfig, DepletionVFXConfig
Procedural (1): AsteroidFieldDefinition
Resources (1): RawMaterialDefinition
Ship (1): ShipArchetypeConfig
Base (1): StationPresetConfig
Station (2): StationDefinition, StationServicesConfig
StationServices (1): GameServicesConfig
Targeting (3): TargetingConfig, TargetingVFXConfig, TargetingAudioConfig
World (1): WorldDefinition

### Reducers (9 total)

GameStateReducer, CameraReducer, DockingReducer, InventoryReducer, MiningReducer, ShipStateReducer, StationServicesReducer, StationStorageReducer, TargetingReducer

### State Records (24+ total)

Root: GameState (composed)
Slices: GameLoopState, ShipState, FleetState, OwnedShip, InventoryState, MiningSessionState, MiningYieldResult, DockingState, CameraState, TargetingState, StationServicesState, StationStorageState, RefiningJobState, RefiningState, WorldState, StationData, ExploreState, BaseState, PlacedModule, TechTreeState, MarketState, CommodityMarket, MarketOrder

### ECS Components (19 IComponentData + 3 IBufferElementData + 3 BlobAssets)

See spec survey for full enumeration.

### Known Troubleshooting Items (4+)

1. Per-instance material property overrides don't work with SubScene-baked prefabs — use RenderMeshUtility.AddComponents instead
2. SF_Asteroids-M2 FBX meshes modeled in cm (~4600 units) — set globalScale: 0.01 in .meta files
3. ECS ship position (LocalTransform on PlayerControlledTag entity) is stale during gameplay — use Cinemachine tracking target Transform instead
4. ECS entity-gone race conditions in InputBridge — defensive guards with logging added in Spec 008
