# Tasks: Developer & Designer Documentation Bootstrap

**Input**: Design documents from `/specs/010-dev-designer-docs/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md

**Tests**: No test tasks — this is a documentation-only deliverable. Validation is structural (file existence, section completeness, Mermaid syntax).

**Organization**: Tasks grouped by user story. Each story produces an independently verifiable documentation subset.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

All documentation files live under `docs/` at the repository root. Source files for reference live under `Assets/`.

---

## Phase 1: Setup

**Purpose**: Create the `docs/` directory structure

- [ ] T001 Create docs/ directory structure with subdirectories: `docs/architecture/`, `docs/systems/`, `docs/designer-guide/`

**Checkpoint**: Directory structure exists, ready for content creation.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No blocking foundational tasks beyond directory creation. All user stories can begin immediately after Phase 1.

**Checkpoint**: Foundation ready — user story implementation can now begin in parallel.

---

## Phase 3: User Story 1 — Architecture Documentation for Developers (Priority: P1)

**Goal**: Create 5 cross-cutting architecture docs with Mermaid diagrams covering the entire VoidHarvest system.

**Independent Test**: Verify all 5 files exist in `docs/architecture/`, each contains at least one Mermaid diagram, and each addresses the content areas in FR-004 through FR-008.

### Implementation for User Story 1

- [ ] T002 [P] [US1] Write architecture overview in docs/architecture/overview.md — system-level Mermaid flowchart (`graph TD`) showing all 12 shipped features, communication paths (EventBus, DI, ECS sync), hybrid DOTS/MonoBehaviour boundary, and skeleton features (Fleet/TechTree/Economy) as future phases. Reference source: `Assets/Core/`, `Assets/Features/` directory structure, all assembly definitions.

- [ ] T003 [P] [US1] Write state management doc in docs/architecture/state-management.md — GameState tree diagram (Mermaid `graph TD`), reducer composition visualization showing GameStateReducer dispatching to 8 feature reducers, action dispatch sequence diagram, CompositeReducer cross-cutting actions (TransferToStation, TransferToShip, RepairShip). Reference source: `Assets/Core/State/GameState.cs`, `Assets/Core/State/GameStateReducer.cs`, all `*State.cs` and `*Actions.cs` files.

- [ ] T004 [P] [US1] Write event system doc in docs/architecture/event-system.md — complete catalog of all 25 event types with Mermaid publisher→event→subscriber flow diagram (`graph LR`). Organize by source: Core/EventBus (12), StationServices (7), Targeting (6). Map each event to its publishing system(s) and subscribing view(s)/controller(s). Reference source: `Assets/Core/EventBus/Events/`, `Assets/Features/StationServices/Data/StationServicesEvents.cs`, `Assets/Features/Targeting/Data/TargetingEvents.cs`.

- [ ] T005 [P] [US1] Write dependency injection doc in docs/architecture/dependency-injection.md — VContainer scope hierarchy diagram (Mermaid `graph TD`) showing RootLifetimeScope and SceneLifetimeScope, registration patterns for MonoBehaviour views and pure services, async subscription convention (OnEnable subscribe → OnDisable cancel/dispose/null → OnDestroy safety net). Reference source: `Assets/Core/DI/` (RootLifetimeScope, SceneLifetimeScope), `Assets/Features/*/Views/` for subscription patterns.

- [ ] T006 [P] [US1] Write data pipeline doc in docs/architecture/data-pipeline.md — full lifecycle sequence diagram (Mermaid `sequenceDiagram`) from SO authoring → Baker/Authoring component → BlobAsset baking → ECS System consumption → View layer rendering. Document two concrete pipelines: OreDefinition→OreTypeBlobBakingSystem→OreTypeBlobDatabase→MiningBeamSystem, and DockingConfig→DockingConfigBlobBakingSystem→DockingConfigBlob→DockingSystem. Reference source: `Assets/Features/Mining/Data/OreTypeBlob.cs`, `Assets/Features/Mining/Systems/OreTypeBlobBakingSystem.cs`, `Assets/Features/Docking/Data/DockingConfigBlob.cs`, `Assets/Features/Docking/Systems/DockingConfigBlobBakingSystem.cs`, `Assets/Features/Procedural/Views/AsteroidFieldSpawner.cs`.

**Checkpoint**: All 5 architecture docs exist with Mermaid diagrams. A developer can understand the full system architecture.

---

## Phase 4: User Story 2 — Per-System Documentation for Developers (Priority: P1)

**Goal**: Create 12 system docs, each with 10 mandatory sections and at least one Mermaid diagram.

**Independent Test**: Verify all 12 files exist in `docs/systems/`, each contains all 10 sections (Purpose, Architecture Diagram, State Shape, Actions, SO Configs, ECS Components, Events, Assembly Dependencies, Key Types, Designer Notes), and each has at least one Mermaid diagram.

### Implementation for User Story 2

- [ ] T007 [P] [US2] Write Camera system doc in docs/systems/camera.md — 10 sections covering CameraState (Yaw, Pitch, Distance, zoom/pitch limits), CameraReducer (5 actions: Orbit, Zoom, SpeedZoom, ToggleFreeLook, FreeLook), CameraConfig + SkyboxConfig SOs, Cinemachine integration via CameraView, no ECS components (managed layer), no events. Mermaid: data flow from Input→CameraReducer→CameraState→CameraView→Cinemachine. Reference source: `Assets/Features/Camera/{Data,Systems,Views}/`.

- [ ] T008 [P] [US2] Write Input system doc in docs/systems/input.md — 10 sections covering PilotCommand record, InputBridge (Unity InputSystem→action dispatch), InteractionConfig SO (configurable timing/distances), ITargetable raycast detection, Physics raycasts, docked-state guards. Mermaid: flow from InputSystem→InputBridge→PilotCommand→ShipStateReducer + SelectTargetAction. No owned state record (dispatches to other reducers). Reference source: `Assets/Features/Input/{Data,Views}/`, `Assets/Core/Extensions/ITargetable.cs`.

- [ ] T009 [P] [US2] Write Ship system doc in docs/systems/ship.md — 10 sections covering ShipState record, ShipStateReducer, ShipArchetypeConfig SO (mass, thrust, speed, cargo, lock time, mining range), 6 ECS components (ShipPositionComponent, ShipVelocityComponent, ShipConfigComponent, ShipFlightModeComponent, PilotCommandComponent, PlayerControlledTag), ShipPhysicsSystem (Burst-compiled 6DOF), EcsToStoreSyncSystem, StoreToEcsSyncSystem. Mermaid: state diagram showing flight modes or data flow PilotCommand→ECS→ShipPhysics→SyncBack. Reference source: `Assets/Features/Ship/{Data,Systems,Views}/`.

- [ ] T010 [P] [US2] Write Mining system doc in docs/systems/mining.md — 10 sections covering MiningSessionState, MiningReducer (4 actions), 5 SOs (OreDefinition, OreChunkConfig, MiningVFXConfig, MiningAudioConfig, DepletionVFXConfig), 8 ECS components (MiningBeamComponent, AsteroidComponent, AsteroidOreComponent, AsteroidVisualMappingSingleton, AsteroidEmissionComponent, AsteroidGlowFadeComponent, OreTypeDatabaseComponent, MiningActionBufferSingleton), OreTypeBlob BlobAsset, 5 Burst ISystem (MiningBeamSystem, AsteroidDepletionSystem, AsteroidScaleSystem, AsteroidEmissionSystem, AsteroidDestroySystem), 2 managed systems (MiningActionDispatchSystem, OreTypeBlobBakingSystem), 5 events (MiningStarted/Stopped/Yield, OreChunkCollected, ThresholdCrossed). Mermaid: mining beam data flow from target selection through yield calculation to inventory. Reference source: `Assets/Features/Mining/{Data,Systems,Views}/`.

- [ ] T011 [P] [US2] Write Procedural system doc in docs/systems/procedural.md — 10 sections covering AsteroidFieldDefinition SO (field params + OreFieldEntry[]), AsteroidFieldConfigComponent + AsteroidPrefabComponent + AsteroidOreWeightElement + AsteroidMeshPrefabElement + AsteroidVisualMappingElement (ECS), AsteroidFieldSystem (Burst job spawning), AsteroidFieldSpawner + AsteroidPrefabAuthoring (authoring/baking). No owned state record, no reducer, no events. Mermaid: baking pipeline from AsteroidFieldDefinition→Authoring→Baker→ECS components→AsteroidFieldSystem→entity spawning. Reference source: `Assets/Features/Procedural/{Data,Systems,Views}/`.

- [ ] T012 [P] [US2] Write Resources system doc in docs/systems/resources.md — 10 sections covering InventoryState (Slots ImmutableArray, MaxSlots), InventoryReducer (AddResource, RemoveResource), RawMaterialDefinition SO. No ECS components (managed layer only), no events published. Mermaid: inventory action flow from MiningYield→AddResourceAction→InventoryReducer→InventoryState. Reference source: `Assets/Features/Resources/{Data,Systems}/`.

- [ ] T013 [P] [US2] Write HUD system doc in docs/systems/hud.md — 10 sections covering HUDView (root controller), RadialMenuController (context-sensitive segments: Dock/Undock for stations, Mine for asteroids, Lock Target for all), TargetInfoPanel, HUDWarningView, SelectionOutlineFeature (URP ScriptableRendererFeature). No owned state, no reducer, no ECS components (managed layer), RadialMenuRequestedEvent consumed. Mermaid: HUD data flow from GameState subscriptions→UI updates. Reference source: `Assets/Features/HUD/Views/`.

- [ ] T014 [P] [US2] Write Docking system doc in docs/systems/docking.md — 10 sections covering DockingState (sealed record with DockingPhase enum: None→Approaching→Docked→Undocking→None), DockingReducer (5 actions), DockingConfig SO + DockingVFXConfig + DockingAudioConfig, DockingStateComponent + DockingEventFlags + DockingConfigBlobComponent (ECS), DockingConfigBlob BlobAsset, DockingSystem (Burst ISystem state machine), DockingEventBridgeSystem (Burst→managed bridge), DockingConfigBlobBakingSystem, 4 events (DockingStarted/Completed, UndockingStarted, UndockCompleted), DockingMath. Mermaid: state diagram (`stateDiagram-v2`) showing docking phase transitions. Reference source: `Assets/Features/Docking/{Data,Systems,Views}/`.

- [ ] T015 [P] [US2] Write Station Services system doc in docs/systems/station-services.md — 10 sections covering StationServicesState (Credits, StationStorages, RefiningJobs), StationServicesReducer (11 actions) + StationStorageReducer + CompositeReducer cross-cutting actions, GameServicesConfig SO, no ECS components (managed layer), 7 events, RefiningMath + RepairMath, RefiningJobTicker (realtimeSinceStartup timer), 9 view controllers (menu + 4 panels + credit indicator + refining notification). Mermaid: refining pipeline flow from StartRefiningJob→timer→Complete→Collect. Reference source: `Assets/Features/StationServices/{Data,Systems,Views}/`.

- [ ] T016 [P] [US2] Write Targeting system doc in docs/systems/targeting.md — 10 sections covering TargetingState (Selection, LockAcquisition, LockedTargets ImmutableArray), TargetingReducer (8 actions), TargetingConfig + TargetingVFXConfig + TargetingAudioConfig SOs, no ECS components (managed layer with Physics raycasts), 6 events, LockTimeMath + TargetingMath, TargetingController (orchestrator) + 6 sub-views (Reticle, OffScreenIndicator, LockProgress, TargetCardPanel, TargetCard, TargetPreview), docked-state guard (FR-035). Mermaid: lock acquisition flow from SelectTarget→BeginLock→tick→CompleteLock. Reference source: `Assets/Features/Targeting/{Data,Systems,Views}/`.

- [ ] T017 [P] [US2] Write Station system doc in docs/systems/station.md — 10 sections covering StationDefinition SO (station identity, type, services config refs), StationServicesConfig SO (per-station: slots, multiplier, repair flag), StationPresetConfig SO (from Base feature — documented here as Base's single type), StationType enum, no owned state record (Station data flows into WorldState.Stations), no reducer (data-driven via WorldDefinition), no ECS components, no events. Designer Notes must explicitly state that the Base feature (StationPresetConfig) is documented within this doc due to its single-type scope. Mermaid: data flow from StationDefinition→WorldDefinition→WorldState.Stations. Reference source: `Assets/Features/Station/Data/`, `Assets/Features/Base/Data/StationPresetConfig.cs`.

- [ ] T018 [P] [US2] Write World system doc in docs/systems/world.md — 10 sections covering WorldDefinition SO (references StationDefinition[]), WorldState record (Stations ImmutableArray of StationData), BuildWorldStations() initialization, no reducer (state built at startup), no ECS components, no events. Mermaid: initialization flow from WorldDefinition.BuildWorldStations()→WorldState→GameState. Reference source: `Assets/Features/World/Data/WorldDefinition.cs`, `Assets/Core/State/WorldState.cs`.

**Checkpoint**: All 12 system docs exist with 10 sections each and Mermaid diagrams. A developer can look up any Phase 0 feature.

---

## Phase 5: User Story 3 — Designer Guide for Non-Programmers (Priority: P2)

**Goal**: Create 4 designer guide docs with zero code, step-by-step workflows, and a complete SO catalog.

**Independent Test**: Verify all 4 files exist in `docs/designer-guide/`, contain no C# code or namespace references, and provide actionable workflows.

### Implementation for User Story 3

- [ ] T019 [US3] Write ScriptableObject catalog in docs/designer-guide/scriptable-objects.md — catalog all 24 SO types organized by game system. For each SO: Create menu path (e.g., `Create > VoidHarvest > Mining > Ore Definition`), all configurable fields with plain-language descriptions, default values, valid ranges, and which game systems consume the asset. Use non-programmer language throughout. Include mini-glossary mapping Unity terms to plain language. Reference source: all SO `.cs` files listed in research.md ScriptableObject catalog.

- [ ] T020 [P] [US3] Write adding-ores guide in docs/designer-guide/adding-ores.md — step-by-step numbered workflow for creating a new ore type entirely in the Unity Editor: (1) create OreDefinition asset via Create menu, (2) configure display name, rarity tier, yield, hardness, beam color, volume, tint, ore ID, (3) configure refining outputs (RawMaterialDefinition refs) and credit cost, (4) update AsteroidFieldDefinition with new OreFieldEntry weight, (5) assign visual mapping (tint + mesh variants). Include field-by-field reference table with defaults and valid ranges. Zero code. Reference source: `Assets/Features/Mining/Data/OreDefinition.cs`, `Assets/Features/Procedural/Data/AsteroidFieldDefinition.cs`.

- [ ] T021 [P] [US3] Write adding-stations guide in docs/designer-guide/adding-stations.md — step-by-step numbered workflow for creating a new station type: (1) create StationDefinition asset, (2) configure station name, type (Mining/Refinery/Trade), (3) create or assign StationServicesConfig (slots, refining multiplier, repair flag), (4) optionally create StationPresetConfig for visual preset, (5) add to WorldDefinition's station list, (6) configure docking port on station prefab. Include field-by-field reference table. Zero code. Reference source: `Assets/Features/Station/Data/StationDefinition.cs`, `Assets/Features/Station/Data/StationServicesConfig.cs`, `Assets/Features/World/Data/WorldDefinition.cs`.

- [ ] T022 [P] [US3] Write tuning reference in docs/designer-guide/tuning-reference.md — consolidated quick-reference table of ALL designer-tunable parameters across all 24 ScriptableObject types. Columns: Parameter Name, Asset Type, Default Value, Valid Range, Description. Organize by game system (Camera, Ship, Mining, Docking, Targeting, Station, etc.). Include tips for common tuning scenarios (e.g., "Making mining faster", "Increasing docking range"). Zero code. Reference source: all SO `.cs` files.

**Checkpoint**: All 4 designer guide docs exist. A non-programmer can add ores, add stations, and look up any tunable parameter.

---

## Phase 6: User Story 4 — Supporting Documentation (Priority: P2)

**Goal**: Create glossary, troubleshooting, onboarding, and assembly map docs.

**Independent Test**: Verify all 4 files exist at `docs/` root, glossary defines ~30 terms, troubleshooting covers 4+ known pitfalls, onboarding provides reading order, assembly-map has Mermaid dependency graph covering all 38 assemblies.

### Implementation for User Story 4

- [ ] T023 [P] [US4] Write glossary in docs/glossary.md — alphabetically sorted table of ~30 project-specific terms. Must include at minimum: Action, Assembly Definition, Authoring, Baker/Baking, BlobAsset, Burst, CompositeReducer, DOTS, ECS, Entity, EventBus, IComponentData, ImmutableArray, ISystem, MonoBehaviour, NativeArray, PilotCommand, Reducer, Record (sealed), ScriptableObject, State Slice, SystemBase, UniTask, URP, VContainer, WorldState. Format: Term | Definition | Where Used. Reference source: constitution.md, project codebase patterns.

- [ ] T024 [P] [US4] Write troubleshooting guide in docs/troubleshooting.md — document all known pitfalls in Problem→Cause→Solution format: (1) per-instance material property overrides fail with SubScene-baked prefabs, (2) SF_Asteroids-M2 FBX mesh scale (cm vs m), (3) stale ECS ship position (LocalTransform vs Cinemachine tracking target), (4) ECS entity-gone race conditions in InputBridge, (5) async subscription lifecycle (OnEnable/OnDisable convention), (6) FindObjectOfType replacement with VContainer DI, (7) Time.time vs realtimeSinceStartup for pause-safe timers. Reference source: MEMORY.md DOTS/ECS Gotchas section, Spec 008 bugfix notes.

- [ ] T025 [P] [US4] Write assembly map in docs/assembly-map.md — Mermaid dependency graph (`graph TD`) showing all 38 assembly definitions (9 Core + 29 Features) grouped by layer (Core, Features, Tests). Show edges for assembly references. Highlight the Core→Features dependency direction and note that Features never reference other Features except through Core/Extensions (with documented exceptions like Mining→Resources, StationServices→multiple). Reference source: all `*.asmdef` files.

- [ ] T026 [US4] Write onboarding guide in docs/onboarding.md — recommended reading order for new developers with relative links to all docs. Structure: (1) Start with glossary for terminology, (2) Read architecture/overview for big picture, (3) Read state-management + event-system for core patterns, (4) Read data-pipeline for SO→ECS flow, (5) Read dependency-injection for DI patterns, (6) Read assembly-map for module boundaries, (7) Read system doc for assigned feature area, (8) Read troubleshooting for known gotchas, (9) For designers: read designer-guide/ instead of systems/. Include links to constitution.md and CLAUDE.md as project governance references. Reference source: all docs/ files (must be written last to link everything).

**Checkpoint**: All 4 supporting docs exist. A new team member has a clear onboarding path.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate documentation set consistency and completeness

- [ ] T027 Validate all cross-references between docs — check that every relative markdown link in all 25 docs resolves to an existing file. Check that section anchors used in links match actual heading IDs. Fix any broken links.

- [ ] T028 Validate structural completeness — confirm each of the 12 system docs has exactly 10 section headings (Purpose, Architecture Diagram, State Shape, Actions, ScriptableObject Configs, ECS Components, Events, Assembly Dependencies, Key Types, Designer Notes). Confirm each system doc and architecture doc has at least 1 Mermaid code block. Confirm designer guide docs contain no C# code patterns (namespace, sealed record, readonly struct, public class, etc.).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: N/A — no blocking prerequisites
- **US1 Architecture (Phase 3)**: Depends on Phase 1 (directories). All 5 docs can be written in parallel.
- **US2 Systems (Phase 4)**: Depends on Phase 1 (directories). All 12 docs can be written in parallel. Benefits from US1 being complete (for cross-referencing) but not strictly blocked.
- **US3 Designer Guide (Phase 5)**: Depends on Phase 1. T019 (SO catalog) should precede T020/T021 (step-by-step guides reference it). T022 (tuning ref) depends on T019 for field data.
- **US4 Supporting (Phase 6)**: Depends on Phase 1. T026 (onboarding) MUST be last — links to all other docs. T023/T024/T025 can be parallel.
- **Polish (Phase 7)**: Depends on all user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 1 — no dependencies on other stories
- **US2 (P1)**: Can start after Phase 1 — benefits from US1 for cross-references but independently testable
- **US3 (P2)**: Can start after Phase 1 — benefits from US2 (system docs inform SO field descriptions) but independently testable
- **US4 (P2)**: Can start after Phase 1 — T026 (onboarding) must be written last after all other docs exist

### Within Each User Story

- US1: All 5 architecture docs are independent — can be written in parallel
- US2: All 12 system docs are independent — can be written in parallel
- US3: SO catalog (T019) first → step-by-step guides + tuning reference (T020/T021/T022) all parallel
- US4: Glossary/troubleshooting/assembly-map parallel (T023/T024/T025) → onboarding last (T026)

### Parallel Opportunities

Within US1: T002, T003, T004, T005, T006 — all 5 architecture docs can be written in parallel
Within US2: T007 through T018 — all 12 system docs can be written in parallel
Within US3: T020, T021, T022 can run in parallel (after T019)
Within US4: T023, T024, T025 can run in parallel
Across stories: US1 and US2 can run in parallel; US3 and US4 can run in parallel

---

## Parallel Example: User Story 2

```text
# All 12 system docs can be launched in parallel (different files, no dependencies):
Task: T007 "Write Camera system doc in docs/systems/camera.md"
Task: T008 "Write Input system doc in docs/systems/input.md"
Task: T009 "Write Ship system doc in docs/systems/ship.md"
Task: T010 "Write Mining system doc in docs/systems/mining.md"
Task: T011 "Write Procedural system doc in docs/systems/procedural.md"
Task: T012 "Write Resources system doc in docs/systems/resources.md"
Task: T013 "Write HUD system doc in docs/systems/hud.md"
Task: T014 "Write Docking system doc in docs/systems/docking.md"
Task: T015 "Write Station Services system doc in docs/systems/station-services.md"
Task: T016 "Write Targeting system doc in docs/systems/targeting.md"
Task: T017 "Write Station system doc in docs/systems/station.md"
Task: T018 "Write World system doc in docs/systems/world.md"
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup (create directories)
2. Complete Phase 3: US1 Architecture Docs (5 files)
3. **STOP and VALIDATE**: A developer can understand the full system architecture
4. Proceed to US2 for per-system detail

### Incremental Delivery

1. Phase 1 (Setup) → directories ready
2. US1 (Architecture) → big-picture understanding ✓
3. US2 (Systems) → per-feature detail ✓
4. US3 (Designer Guide) → non-programmer extensibility ✓
5. US4 (Supporting) → onboarding + reference ✓
6. Polish → consistency validated ✓

### Parallel Strategy

With parallel agents:
- Agent A: US1 Architecture docs (T002-T006)
- Agent B: US2 System docs, batch 1 (T007-T012)
- Agent C: US2 System docs, batch 2 (T013-T018)
- After US1+US2 complete:
  - Agent A: US3 Designer Guide (T019-T022)
  - Agent B: US4 Supporting Docs (T023-T026)
- Final: Polish (T027-T028)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each task writes one markdown file — self-contained and independently verifiable
- Source files listed in each task description provide the reference material for content
- Commit after each phase or logical group of parallel tasks
- All content must accurately reflect the codebase state post-Spec 009
- Mermaid diagrams must be valid syntax renderable by GitHub
