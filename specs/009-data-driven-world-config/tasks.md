# Tasks: Data-Driven World Config

**Input**: Design documents from `/specs/009-data-driven-world-config/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: TDD is mandatory per project constitution. Tests are written first, verified failing, then implementation follows.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (New Assembly Scaffolding)

**Purpose**: Create new Feature assemblies for Station and World modules

- [X] T001 [P] Create `Assets/Features/Station/` directory structure (`Data/`, `Tests/`) and assembly definition `Assets/Features/Station/VoidHarvest.Features.Station.asmdef` referencing `Core.Extensions`, `Core.State`, `Features.Base`
- [X] T002 [P] Create `Assets/Features/World/` directory structure (`Data/`, `Tests/`) and assembly definition `Assets/Features/World/VoidHarvest.Features.World.asmdef` referencing `Core.Extensions`, `Core.State`, `Features.Station`, `Features.Ship`
- [X] T003 [P] Create test assembly `Assets/Features/Station/Tests/VoidHarvest.Features.Station.Tests.asmdef` referencing `Features.Station`, `Features.Base`, `Core.State`, NUnit, UnityEngine.TestRunner, UnityEditor.TestRunner
- [X] T004 [P] Create test assembly `Assets/Features/World/Tests/VoidHarvest.Features.World.Tests.asmdef` referencing `Features.World`, `Features.Station`, `Features.Ship`, `Core.State`, NUnit, UnityEngine.TestRunner, UnityEditor.TestRunner

---

## Phase 2: Foundational (Assembly Restructuring)

**Purpose**: Move shared types and update assembly references so all user stories can compile against the new assemblies

**CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 [P] Create `StationType` enum (`MiningRelay`, `RefineryHub`, `TradePost`, `ResearchStation`) in `Assets/Features/Station/Data/StationType.cs`
- [X] T006 Move `StationServicesConfig.cs` from `Assets/Features/StationServices/Data/` to `Assets/Features/Station/Data/StationServicesConfig.cs` — update namespace to `VoidHarvest.Features.Station.Data`, preserve GUID (FR-027)
- [X] T007 Update `Assets/Features/StationServices/VoidHarvest.Features.StationServices.asmdef` to add `VoidHarvest.Features.Station` reference
- [X] T008 [P] Update `Assets/Features/StationServices/Tests/VoidHarvest.Features.StationServices.Tests.asmdef` to add `VoidHarvest.Features.Station` reference
- [X] T009 [P] Update `Assets/Features/Targeting/VoidHarvest.Features.Targeting.asmdef` to add `VoidHarvest.Features.Station` reference
- [X] T010 [P] Update `Assets/Features/Targeting/Tests/VoidHarvest.Features.Targeting.Tests.asmdef` to add `VoidHarvest.Features.Station` reference
- [X] T011 [P] Update `Assets/Features/Docking/VoidHarvest.Features.Docking.asmdef` to add `VoidHarvest.Features.Station` reference (required for DockingPortComponent to reference StationDefinition — FR-025)
- [X] T012 Verify compilation succeeds after assembly restructuring — check Unity console for errors via `read_console`

**Checkpoint**: Assembly graph is clean. All existing 521 tests still compile and pass.

---

## Phase 3: User Story 1 — Designer Configures Stations via ScriptableObjects (Priority: P1) MVP

**Goal**: Replace hard-coded station data in `RootLifetimeScope` with `StationDefinition` and `WorldDefinition` ScriptableObjects. Designers can add/modify/remove stations via asset editing alone.

**Independent Test**: Create a new StationDefinition asset, add it to WorldDefinition, enter Play mode, verify the station appears with correct services and docking config.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T013 [P] [US1] Write StationDefinition OnValidate tests (StationId > 0, DisplayName non-empty, ServicesConfig non-null, AvailableServices.Length >= 1, DockingPortOffset.magnitude < 200) in `Assets/Features/Station/Tests/StationDefinitionTests.cs`
- [X] T014 [P] [US1] Write WorldDefinition OnValidate tests (all Stations non-null, no duplicate StationIds, at least one station, StartingShipArchetype non-null) in `Assets/Features/World/Tests/WorldDefinitionTests.cs`
- [X] T015 [P] [US1] Write WorldDefinition → WorldState mapping tests (station count, IDs, positions, names, services match SO data) in `Assets/Features/World/Tests/WorldDefinitionTests.cs`

### Implementation for User Story 1

- [X] T016 [US1] Create `StationDefinition` ScriptableObject with all fields from data-model.md (Identity, World Placement, Services, Docking, Visuals sections) and OnValidate in `Assets/Features/Station/Data/StationDefinition.cs`
- [X] T017 [US1] Verify StationDefinition OnValidate tests pass (T013)
- [X] T018 [US1] Create `WorldDefinition` ScriptableObject with Stations array, PlayerStartPosition, PlayerStartRotation, StartingShipArchetype fields and OnValidate in `Assets/Features/World/Data/WorldDefinition.cs`
- [X] T019 [US1] Add public helper method to WorldDefinition for building WorldState.Stations from the Stations array (maps StationDefinition fields → StationData records)
- [X] T020 [US1] Verify WorldDefinition OnValidate and mapping tests pass (T014, T015)
- [X] T021 [P] [US1] Create `SmallMiningRelay.asset` StationDefinition in `Assets/Features/Station/Data/Definitions/` with values matching current hard-coded data (StationId=1, position=(500,0,0), services=["Sell","Refine"], ServicesConfig=SmallMiningRelayServices)
- [X] T022 [P] [US1] Create `MediumRefineryHub.asset` StationDefinition in `Assets/Features/Station/Data/Definitions/` with values matching current hard-coded data (StationId=2, position=(-800,200,600), services=["Sell","Refine","Repair"], ServicesConfig=MediumRefineryHubServices)
- [X] T023 [US1] Create `DefaultWorld.asset` WorldDefinition in `Assets/Features/World/Data/` referencing both StationDefinitions and current player start values and StartingShipArchetype
- [X] T024 [US1] Add `[SerializeField] private WorldDefinition worldDefinition` to `Assets/Core/RootLifetimeScope.cs` and modify `CreateDefaultGameState()` to build WorldState from WorldDefinition instead of hard-coded values
- [X] T025 [US1] Register WorldDefinition in VContainer via `Assets/Core/RootLifetimeScope.cs` so downstream consumers can inject it
- [X] T026 [US1] Delete `Assets/Features/StationServices/Data/StationServicesConfigMap.cs` and `Assets/Features/StationServices/Data/Assets/StationServicesConfigMap.asset`
- [X] T027 [US1] Modify `StationServicesMenuController` to resolve `StationServicesConfig` from `WorldDefinition`/`StationDefinition` instead of `StationServicesConfigMap` lookup in `Assets/Features/StationServices/Views/StationServicesMenuController.cs`
- [X] T028 [P] [US1] Modify `RefineOresPanelController` to use `StationDefinition.ServicesConfig` via injected WorldDefinition in `Assets/Features/StationServices/Views/RefineOresPanelController.cs`
- [X] T029 [P] [US1] Modify `BasicRepairPanelController` to use `StationDefinition.ServicesConfig` via injected WorldDefinition in `Assets/Features/StationServices/Views/BasicRepairPanelController.cs`
- [X] T030 [US1] Modify `TargetableStation` to add `[SerializeField] StationDefinition stationDefinition` and derive `stationId` from it in `Assets/Features/Targeting/Views/TargetableStation.cs` (FR-024)
- [X] T031 [US1] Modify `DockingPortComponent` to add `[SerializeField] StationDefinition stationDefinition` and derive `StationId` from it in `Awake()`, removing the manually-assigned int field, in `Assets/Features/Docking/Data/DockingPortComponent.cs` (FR-025)
- [X] T032 [US1] Remove `StationServicesConfigMap` serialized field from `Assets/Core/SceneLifetimeScope.cs` and update any remaining config wiring
- [X] T033 [US1] Verify compilation succeeds, run full test suite — all 520+ tests must pass

**Checkpoint**: Stations are fully data-driven. Designers can add stations by creating StationDefinition assets and adding them to WorldDefinition. Zero hard-coded station data remains in C#.

---

## Phase 4: User Story 2 — Docking Parameters Are Fully Data-Driven (Priority: P2)

**Goal**: Replace hard-coded constants in the Burst-compiled DockingSystem with a DockingConfigBlob baked from the DockingConfig ScriptableObject. All docking tuning is designer-editable.

**Independent Test**: Modify DockingConfig SO values (e.g., SnapDuration), enter Play mode, verify docking uses the SO values.

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T034 [P] [US2] Write tests for new DockingConfig fields OnValidate (ApproachTimeout > 0, AlignTimeout > 0, AlignDotThreshold in (0,1], AlignAngVelThreshold > 0) in `Assets/Features/Docking/Tests/DockingConfigTests.cs`
- [X] T035 [P] [US2] Write DockingConfigBlob baking tests (blob fields match SO values, singleton created, self-disables after init) in `Assets/Features/Docking/Tests/DockingConfigBlobTests.cs` — NOTE: `VoidHarvest.Features.Docking.Tests.asmdef` may need `Unity.Entities` and `Unity.Collections` references added for blob/EntityManager test APIs

### Implementation for User Story 2

- [X] T036 [US2] Add `ApproachTimeout`, `AlignTimeout`, `AlignDotThreshold`, `AlignAngVelThreshold` fields with defaults and OnValidate to `Assets/Features/Docking/Data/DockingConfig.cs`
- [X] T037 [US2] Verify DockingConfig OnValidate tests pass (T034)
- [X] T038 [US2] Create `DockingConfigBlob` struct and `DockingConfigBlobComponent` (IComponentData with BlobAssetReference) in `Assets/Features/Docking/Data/DockingConfigBlob.cs`
- [X] T039 [US2] Create `DockingConfigBlobBakingSystem` (managed SystemBase in InitializationSystemGroup, follows OreTypeBlobBakingSystem pattern: static Set → OnUpdate blob build → singleton entity → self-disable) in `Assets/Features/Docking/Systems/DockingConfigBlobBakingSystem.cs`
- [X] T040 [US2] Verify DockingConfigBlob baking tests pass (T035)
- [X] T041 [US2] Modify `DockingSystem` to read all 9 parameters from `DockingConfigBlobComponent` singleton via `SystemAPI.GetSingleton<>()` instead of hard-coded constants in `Assets/Features/Docking/Systems/DockingSystem.cs`
- [X] T042 [US2] Wire DockingConfig SO into `DockingConfigBlobBakingSystem.Set()` call during initialization in `Assets/Core/SceneLifetimeScope.cs` (DockingConfig is already a serialized field — add the Set() call in Configure())
- [X] T043 [US2] Remove duplicate `DockingRange` and `SnapRange` fields from `DockingPortComponent` in `Assets/Features/Docking/Data/DockingPortComponent.cs` (FR-026 — system reads from blob, not MonoBehaviour fields)
- [X] T044 [US2] Update `DockingConfig.asset` to include new field values (ApproachTimeout=120, AlignTimeout=30, AlignDotThreshold=0.999, AlignAngVelThreshold=0.01) at `Assets/Features/Docking/Data/Configs/`
- [X] T045 [US2] Verify compilation succeeds, run full test suite

**Checkpoint**: All docking parameters flow from DockingConfig SO → DockingConfigBlob → DockingSystem. Zero hard-coded docking constants remain.

---

## Phase 5: User Story 3 — Camera Limits Are Designer-Tunable (Priority: P3)

**Goal**: Replace hard-coded camera constants in CameraReducer and CameraView with a CameraConfig ScriptableObject. Camera limits flow through CameraState for pure reducer compatibility.

**Independent Test**: Create a CameraConfig SO with custom pitch/distance limits, start the game, verify camera clamps to configured ranges.

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T046 [P] [US3] Write CameraConfig OnValidate tests (MinPitch < 0, MaxPitch > 0, MinDistance > 0, MaxDistance > MinDistance, zoom range within distance range, OrbitSensitivity > 0, defaults within limits) in `Assets/Features/Camera/Tests/CameraConfigTests.cs`
- [X] T047 [P] [US3] Write CameraState limit field initialization tests (state created from CameraConfig has correct limit values) and CameraReducer tests (reduce respects state limits instead of constants) in `Assets/Features/Camera/Tests/CameraConfigTests.cs`

### Implementation for User Story 3

- [X] T048 [US3] Create `CameraConfig` ScriptableObject with all fields from data-model.md (MinPitch, MaxPitch, MinDistance, MaxDistance, MinZoomDistance, MaxZoomDistance, ZoomCooldownDuration, DefaultYaw, DefaultPitch, DefaultDistance, OrbitSensitivity) and OnValidate in `Assets/Features/Camera/Data/CameraConfig.cs`
- [X] T049 [US3] Verify CameraConfig OnValidate tests pass (T046)
- [X] T050 [US3] Extend `CameraState` sealed record with `MinPitch`, `MaxPitch`, `MinDistance`, `MaxDistance`, `MinZoomDistance`, `MaxZoomDistance` fields (defaults matching current constants) in `Assets/Core/State/CameraState.cs`
- [X] T051 [US3] Modify `CameraReducer` to read limits from `CameraState` fields instead of compile-time constants in `Assets/Features/Camera/Systems/CameraReducer.cs`
- [X] T052 [US3] Verify CameraReducer limit tests pass (T047)
- [X] T053 [US3] Modify `CameraView` to inject `CameraConfig` for `ZoomCooldownDuration` and `OrbitSensitivity` in `Assets/Features/Camera/Views/CameraView.cs`
- [X] T054 [US3] Initialize `CameraState` limit fields from `CameraConfig` SO in `RootLifetimeScope.CreateDefaultGameState()` in `Assets/Core/RootLifetimeScope.cs`
- [X] T055 [US3] Create `DefaultCameraConfig.asset` with current default values in `Assets/Features/Camera/Data/`
- [X] T056 [US3] Register CameraConfig as `[SerializeField]` and VContainer binding in `Assets/Core/SceneLifetimeScope.cs`
- [X] T057 [US3] Verify compilation succeeds, run full test suite

**Checkpoint**: Camera limits are fully data-driven. CameraReducer remains pure static. Designers tune feel via a single SO.

---

## Phase 6: User Story 4 — Inventory Capacity Derives from Ship Archetype (Priority: P4)

**Goal**: Inventory slot count and volume derive from `WorldDefinition.StartingShipArchetype` instead of hard-coded defaults.

**Depends on**: US1 (WorldDefinition must exist for initialization)

**Independent Test**: Set different CargoSlots/CargoCapacity on ShipArchetypeConfig, start game, verify InventoryState matches.

### Tests for User Story 4

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T058 [P] [US4] Write ShipArchetypeConfig CargoSlots OnValidate tests (CargoSlots >= 1, and soft threshold warning logged when CargoSlots > 100) in `Assets/Features/Ship/Tests/ShipArchetypeConfigTests.cs`
- [X] T059 [P] [US4] Write InventoryState initialization test (MaxSlots and MaxVolume derived from ShipArchetypeConfig) in `Assets/Features/World/Tests/WorldDefinitionTests.cs`

### Implementation for User Story 4

- [X] T060 [US4] Add `CargoSlots` (int, default=20) field with OnValidate (>= 1, log informational note above soft threshold of 100) to `Assets/Features/Ship/Data/ShipArchetypeConfig.cs`
- [X] T061 [US4] Verify ShipArchetypeConfig OnValidate test passes (T058)
- [X] T062 [US4] Modify InventoryState initialization in `RootLifetimeScope.CreateDefaultGameState()` to derive `MaxSlots` from `StartingShipArchetype.CargoSlots` and `MaxVolume` from `StartingShipArchetype.CargoCapacity` in `Assets/Core/RootLifetimeScope.cs`
- [X] T063 [US4] Verify InventoryState initialization test passes (T059) and run full test suite

**Checkpoint**: Inventory capacity matches ship archetype. Different ships = different cargo capacity.

---

## Phase 7: User Story 5 — Input and Interaction Timing Is Configurable (Priority: P5)

**Goal**: Replace hard-coded input timing constants with an InteractionConfig ScriptableObject injected into InputBridge and RadialMenuController.

**Independent Test**: Modify InteractionConfig SO values, enter Play mode, verify double-click timing and radial menu distances match.

### Tests for User Story 5

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T064 [P] [US5] Write InteractionConfig OnValidate tests (DoubleClickWindow in [0.1, 1.0], RadialMenuDragThreshold in [1, 20], all distances > 0, MiningBeamMaxRange > 0) in `Assets/Features/Input/Tests/InteractionConfigTests.cs`

### Implementation for User Story 5

- [X] T065 [US5] Create `InteractionConfig` ScriptableObject with fields from data-model.md (DoubleClickWindow=0.3, RadialMenuDragThreshold=5, DefaultApproachDistance=50, DefaultOrbitDistance=100, DefaultKeepAtRangeDistance=50, MiningBeamMaxRange=50) and OnValidate in `Assets/Features/Input/Data/InteractionConfig.cs`
- [X] T066 [US5] Verify InteractionConfig OnValidate tests pass (T064)
- [X] T067 [US5] Modify `InputBridge` to inject InteractionConfig and replace `DoubleClickWindow`, `RadialMenuDragThreshold`, `MiningBeamMaxRange` constants in `Assets/Features/Input/Views/InputBridge.cs`
- [X] T068 [US5] Modify `RadialMenuController` to inject InteractionConfig and replace `DefaultApproachDistance`, `DefaultOrbitDistance`, `DefaultKeepAtRangeDistance` constants in `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs`
- [X] T069 [US5] Create `DefaultInteractionConfig.asset` with current default values in `Assets/Features/Input/Data/`
- [X] T070 [US5] Register InteractionConfig as `[SerializeField]` and VContainer binding in `Assets/Core/SceneLifetimeScope.cs`
- [X] T071 [US5] Verify compilation succeeds, run full test suite

**Checkpoint**: All input timing and interaction distances are designer-tunable via a single SO.

---

## Phase 8: User Story 6 — Editor Validates Scene Configuration Completeness (Priority: P6)

**Goal**: Editor tooling catches missing SO references at edit time, preventing silent null-skip failures at runtime.

**Depends on**: US1 (WorldDefinitionEditor needs WorldDefinition type)

**Independent Test**: Remove a config reference from SceneLifetimeScope, run validator, verify it reports the missing field.

### Tests for User Story 6

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T072 [P] [US6] Write SceneConfigValidator pure validation logic tests (detects null fields, reports all fields when valid, identifies specific missing references) in `Assets/Core/Editor/Tests/SceneConfigValidatorTests.cs` — extract validation logic as static pure functions testable in EditMode; create `Assets/Core/Editor/Tests/VoidHarvest.Core.Editor.Tests.asmdef` if needed (Editor platform only)

### Implementation for User Story 6

- [X] T073 [US6] Create `SceneConfigValidator` editor window with menu item `VoidHarvest > Validate Scene Config` that inspects all serialized config fields on SceneLifetimeScope and RootLifetimeScope, reporting missing/null assignments with green/yellow/red status in `Assets/Core/Editor/SceneConfigValidator.cs`
- [X] T074 [US6] Verify SceneConfigValidator tests pass (T072)
- [X] T075 [US6] Create `WorldDefinitionEditor` custom inspector showing inline station list with completeness badges (null checks on ServicesConfig, AvailableServices, StationId uniqueness) and "Validate All" button in `Assets/Core/Editor/WorldDefinitionEditor.cs`
- [X] T076 [US6] Verify editor tooling compiles and run full test suite

**Checkpoint**: Editor catches misconfigurations before Play mode. WorldDefinition inspector shows station health at a glance.

---

## Phase 9: User Story 7 — Station Assets Are Organized Consistently (Priority: P7)

**Goal**: All station-related assets live under a consistent folder hierarchy for designer discoverability.

**Depends on**: US1 (Station directory structure must exist)

**Independent Test**: Verify folder structure matches convention and all SO cross-references remain intact after moves.

- [X] T077 [P] [US7] Move StationServicesConfig `.asset` files from `Assets/Features/StationServices/Data/Assets/StationConfigs/` to `Assets/Features/Station/Data/ServiceConfigs/` — use Unity asset move to preserve GUIDs
- [X] T078 [P] [US7] Move StationPresetConfig `.asset` files from `Assets/Features/Base/Data/` (SmallMiningRelay.asset, MediumRefineryHub.asset) to `Assets/Features/Station/Data/Presets/`
- [X] T079 [P] [US7] Move RawMaterialDefinition `.asset` files from `Assets/Features/StationServices/Data/Assets/RawMaterials/` to `Assets/Features/Station/Data/RawMaterials/`
- [X] T080 [US7] Verify all SO cross-references resolve correctly after asset moves — check for missing references in Inspector and console
- [X] T081 [US7] Verify compilation succeeds, run full test suite

**Checkpoint**: Asset hierarchy matches quickstart.md conventions. All references intact.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, cleanup, and cross-cutting quality checks

- [X] T082 Run complete test suite — verify all 520+ tests pass with no regressions
- [X] T083 [P] Verify all `CreateAssetMenu` paths on new SOs follow `VoidHarvest/<System>/<Asset Type>` convention (FR-019)
- [X] T084 [P] Verify zero hard-coded station data remains in C# source — search for old station IDs, positions, names (SC-008)
- [X] T085 [P] Check Unity console for warnings or errors after all changes via `read_console`
- [X] T086 Validate quickstart.md designer workflows: add station, tune docking, tune camera, tune input, run validator

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational — MVP delivery target
- **US2 (Phase 4)**: Depends on Foundational only — can run in parallel with US1
- **US3 (Phase 5)**: Depends on Foundational only — can run in parallel with US1
- **US4 (Phase 6)**: Depends on US1 (WorldDefinition.StartingShipArchetype)
- **US5 (Phase 7)**: Depends on Foundational only — can run in parallel with US1
- **US6 (Phase 8)**: Depends on US1 (WorldDefinitionEditor needs WorldDefinition)
- **US7 (Phase 9)**: Depends on US1 (Station/ directory and assets must exist)
- **Polish (Phase 10)**: Depends on all desired user stories being complete

### User Story Dependency Graph

```
Phase 1 (Setup)
    │
Phase 2 (Foundational)
    │
    ├─── US1 (P1) ─┬── US4 (P4)
    │               ├── US6 (P6)
    │               └── US7 (P7)
    ├─── US2 (P2) ──────────────── (independent)
    ├─── US3 (P3) ──────────────── (independent)
    └─── US5 (P5) ──────────────── (independent)
                        │
                   Phase 10 (Polish)
```

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD)
- ScriptableObject types before assets
- Data types before consumers
- OnValidate before asset creation
- Consumer modifications after core types compile
- Verification checkpoint at end of each story

### Parallel Opportunities

**After Phase 2 completes, these can run simultaneously:**
- US1 (stations + world config)
- US2 (docking blob pipeline)
- US3 (camera config)
- US5 (interaction config)

**After US1 completes:**
- US4 (inventory from ship archetype)
- US6 (editor tooling)
- US7 (asset reorganization)

---

## Parallel Examples

### Phase 1 — All Setup Tasks

```
Parallel: T001, T002, T003, T004 (independent directory + asmdef creation)
```

### Phase 2 — Foundational

```
Parallel: T005, T006 (StationType enum + StationServicesConfig move)
Then parallel: T007, T008, T009, T010, T011 (asmdef reference updates)
Then: T012 (compilation verification)
```

### US1 — Tests Phase

```
Parallel: T013, T014, T015 (three independent test files)
```

### US1 — Asset Creation

```
Parallel: T021, T022 (two independent StationDefinition assets)
Then: T023 (WorldDefinition referencing both)
```

### Post-Foundation Independent Stories

```
Parallel: US2 (T034-T045), US3 (T046-T057), US5 (T064-T071)
These three stories touch completely different assemblies/files.
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: US1
4. **STOP and VALIDATE**: Test station configuration end-to-end
5. This alone delivers designer-driven world authoring — the highest value feature

### Incremental Delivery

1. Complete Setup + Foundational → Assembly graph ready
2. Add US1 → Test independently → **MVP! Designers can author worlds**
3. Add US2, US3, US5 in parallel → Each independently testable
4. Add US4 → Test inventory derivation (depends on US1)
5. Add US6 → Editor tooling validates configuration
6. Add US7 → Asset organization polish
7. Polish phase → Final validation

### Single Developer Strategy (Recommended)

Execute sequentially in priority order:

1. Phase 1 + Phase 2 (setup) → ~12 tasks
2. US1 (stations/world) → ~21 tasks → **Checkpoint: MVP**
3. US2 (docking blob) → ~12 tasks → **Checkpoint: Docking tunable**
4. US3 (camera config) → ~12 tasks → **Checkpoint: Camera tunable**
5. US4 (inventory) → ~6 tasks → **Checkpoint: Inventory from ship**
6. US5 (interaction) → ~8 tasks → **Checkpoint: Input tunable**
7. US6 (editor tooling) → ~5 tasks → **Checkpoint: Validation**
8. US7 (asset reorg) → ~5 tasks → **Checkpoint: Clean hierarchy**
9. Phase 10 (polish) → ~5 tasks → **Done!**

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable (except US4/US6/US7 which depend on US1)
- TDD is mandatory: write tests, verify they fail, then implement
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- SceneLifetimeScope is modified by US2, US3, US5 — execute those tasks sequentially within their stories
- RootLifetimeScope is modified by US1, US3, US4 — execute those tasks sequentially within their stories
- All asset moves must use Unity GUID-preserving mechanisms to avoid broken references
