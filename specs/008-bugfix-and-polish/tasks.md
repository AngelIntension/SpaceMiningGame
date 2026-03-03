# Tasks: Bugfix, Event Lifecycle & UI Polish

**Input**: Design documents from `specs/008-bugfix-and-polish/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Included — TDD is mandatory per constitution. Tests written first where applicable (OnValidate, state-change detection). Structural/lifecycle changes verified via compilation + existing test suite.

**Organization**: Tasks grouped by user story (7 stories). Each story is independently testable after its phase completes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US7)
- Exact file paths included in all descriptions

---

## Phase 1: Setup

**Purpose**: Verify baseline and establish shared infrastructure needed by multiple stories

- [X] T001 Run full test suite via Unity Test Runner to confirm all 465 tests pass as baseline before any changes
- [X] T002 Register `InputBridge` in `SceneLifetimeScope` via `RegisterComponentInHierarchy<InputBridge>()` in `Assets/Core/SceneLifetimeScope.cs` (blocks US3, US4)
- [X] T003 Register `CameraView` in `SceneLifetimeScope` via `RegisterComponentInHierarchy<CameraView>()` in `Assets/Core/SceneLifetimeScope.cs`
- [X] T004 Register `TargetingController` in `SceneLifetimeScope` via `RegisterComponentInHierarchy<TargetingController>()` in `Assets/Core/SceneLifetimeScope.cs`
- [X] T005 Register `TargetPreviewManager` in `SceneLifetimeScope` via `RegisterComponentInHierarchy<TargetPreviewManager>()` in `Assets/Core/SceneLifetimeScope.cs`
- [X] T006 Verify compilation clean after DI registrations — check Unity console for errors via MCP

**Checkpoint**: DI infrastructure ready. All user stories can now proceed.

---

## Phase 2: User Story 1 — Station Panel Responsiveness (Priority: P1)

**Goal**: All station panels refresh immediately when ANY relevant state slice changes. No stale data after refining job completion or cargo transfers.

**Independent Test**: Dock at station, start refining job, wait for completion. All panels (cargo, refine, sell) show updated data without menu re-open.

### Tests for US1

- [X] T007 [US1] Write all state-change detection tests in new file `Assets/Features/StationServices/Tests/PanelStateChangeDetectionTests.cs`: `RefineOresPanelRefreshesOnInventoryChange` (verifies refresh when only InventoryState changes), `SellResourcesPanelRefreshesOnInventoryChange` (same for sell panel), `CargoTransferPanelRefreshesOnSingleSliceChange` (verifies refresh when only StationServicesState changes — confirms existing logic correct), `BasicRepairPanelRefreshesOnSingleSliceChange` (verifies refresh when only ActiveShipPhysics changes), `PanelSkipsRefreshWhenNoSliceChanged` (verifies all panels skip refresh when no relevant slice changed)

### Implementation for US1

- [X] T012 [US1] Fix `RefineOresPanelController.ListenForStateChanges()` to track `_lastInventory` field and include `InventoryState` in state-change detection using AND-skip pattern in `Assets/Features/StationServices/Views/RefineOresPanelController.cs` (line ~124-134)
- [X] T013 [US1] Fix `SellResourcesPanelController.ListenForStateChanges()` to track `_lastInventory` field and include `InventoryState` in state-change detection using AND-skip pattern in `Assets/Features/StationServices/Views/SellResourcesPanelController.cs` (line ~80-90)
- [X] T014 [US1] Audit `CargoTransferPanelController.ListenForStateChanges()` and `BasicRepairPanelController.ListenForStateChanges()` — verify both use correct AND-skip logic (Inventory+Services and Services+Ship respectively). No code change expected; document findings in PR notes. Files: `Assets/Features/StationServices/Views/CargoTransferPanelController.cs`, `Assets/Features/StationServices/Views/BasicRepairPanelController.cs`
- [X] T016 [US1] Run US1 tests — confirm all pass after fixes. Run full test suite to verify no regressions

**Checkpoint**: All station panels correctly detect single-slice state changes. US1 independently testable.

---

## Phase 3: User Story 2 — Reliable Event Subscriptions (Priority: P1)

**Goal**: No duplicate event handlers after disable/enable or dock/undock cycles. Clean subscription lifecycle for all async MonoBehaviours.

**Independent Test**: Dock/undock/redock 3 times. No duplicate audio, no duplicate UI refreshes, no console errors.

**Verification note**: Subscription lifecycle changes are structural (MonoBehaviour hook wiring) and not unit-testable in EditMode without mocking the full Unity lifecycle. Verification is via: (1) compilation gate, (2) full regression test suite, (3) manual dock/undock/redock cycle per SC-002. PlayMode integration tests are out of scope for this spec.

### Implementation for US2

- [X] T017 [US2] Move `RadialMenuController` async EventBus subscription from `Start()` to `OnEnable()` and cancellation from `OnDestroy()` to `OnDisable()` in `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs`. Keep existing UIElements `RegisterCallback`/`UnregisterCallback` in `OnEnable()`/`OnDisable()` as-is.
- [X] T018 [US2] Move `TargetingAudioController` async EventBus subscriptions (4 listeners) from `Start()` to `OnEnable()` and cancellation from `OnDestroy()` to `OnDisable()` in `Assets/Features/Targeting/Views/TargetingAudioController.cs`
- [X] T019 [US2] Move `StationServicesMenuController` async EventBus subscriptions (2 listeners) from `Start()` to `OnEnable()` and cancellation from `OnDestroy()` to `OnDisable()` in `Assets/Features/StationServices/Views/StationServicesMenuController.cs`
- [X] T020 [P] [US2] Add `OnDestroy()` safety net to `CargoTransferPanelController` that calls `Cleanup()` if not already called in `Assets/Features/StationServices/Views/CargoTransferPanelController.cs`
- [X] T021 [P] [US2] Add `OnDestroy()` safety net to `RefineOresPanelController` that calls `Cleanup()` if not already called in `Assets/Features/StationServices/Views/RefineOresPanelController.cs`
- [X] T022 [P] [US2] Add `OnDestroy()` safety net to `SellResourcesPanelController` that calls `Cleanup()` if not already called in `Assets/Features/StationServices/Views/SellResourcesPanelController.cs`
- [X] T023 [P] [US2] Add `OnDestroy()` safety net to `BasicRepairPanelController` that calls `Cleanup()` if not already called in `Assets/Features/StationServices/Views/BasicRepairPanelController.cs`
- [X] T024 [P] [US2] Add `OnDestroy()` safety net to `CreditBalanceIndicator` that calls `Cleanup()` if not already called in `Assets/Features/StationServices/Views/CreditBalanceIndicator.cs`
- [X] T025 [US2] Verify compilation clean after all lifecycle changes — check Unity console via MCP
- [X] T026 [US2] Run full test suite to verify no regressions from subscription lifecycle changes

**Checkpoint**: All async subscriptions follow OnEnable/OnDisable convention. Safety nets prevent leaks. US2 independently testable.

---

## Phase 4: User Story 3 — Visible Input Feedback (Priority: P2)

**Goal**: All player input actions produce visible feedback — either the intended action or a logged warning. Zero silent failures in InputBridge.

**Independent Test**: Select depleted asteroid, press mining hotkey. Selection clears and warning logged.

### Implementation for US3

- [X] T027 [US3] Add defensive logging and `ClearSelectionAction` dispatch to `InputBridge.StartMining()` when `_selectedAsteroidEntity` doesn't exist — log `[InputBridge] StartMining: asteroid entity no longer exists, clearing selection` in `Assets/Features/Input/Views/InputBridge.cs` (line ~457)
- [X] T028 [US3] Add defensive logging to `InputBridge.InitiateDocking()` when `_ecsReady` is false (`[InputBridge] InitiateDocking: ECS not ready`) or docking port is null (`[InputBridge] InitiateDocking: no docking port found`) in `Assets/Features/Input/Views/InputBridge.cs` (line ~534)
- [X] T029 [US3] Add entity existence validation to `InputBridge.OnHotbar1()` — check `_entityManager.Exists(_selectedAsteroidEntity)` before dispatching mining actions, log warning and clear selection if entity gone in `Assets/Features/Input/Views/InputBridge.cs` (line ~443)
- [X] T030 [US3] Add throttled warning to `InputBridge.TryInitializeECS()` — add `_ecsInitFailCount` field, log `[InputBridge] ECS initialization failed after 60 frames` once when count reaches 60, then reset counter in `Assets/Features/Input/Views/InputBridge.cs` (line ~170)
- [X] T031 [US3] Subscribe `InputBridge` to `StateChangedEvent<GameState>` and call `SyncSelectionFromState()` when `Selection.TargetId` changes, keeping local selection fields in sync with state store. Add subscription to `OnEnable()`/cancel to `OnDisable()` per convention in `Assets/Features/Input/Views/InputBridge.cs`
- [X] T032 [US3] Add null guard for `_targetingController` in `RadialMenuController` that disables the "Lock Target" radial menu segment and logs `[RadialMenuController] TargetingController not found, Lock Target disabled`. Place guard in the segment-building logic (not in Start), so it survives the DI migration in T036. File: `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs`
- [X] T033 [US3] Verify compilation clean and run full test suite after input feedback changes

**Checkpoint**: All InputBridge failure paths produce logged warnings. US3 independently testable.

---

## Phase 5: User Story 4 — Robust Component Discovery (Priority: P2)

**Goal**: Cross-component references resolved via VContainer DI injection instead of fragile FindObjectOfType. Deterministic initialization regardless of script execution order.

**Independent Test**: Change script execution order in Project Settings. All references still resolve correctly.

### Implementation for US4

- [X] T034 [US4] Replace `FindFirstObjectByType<CameraView>()` in `InputBridge.Start()` with `[Inject]` method injection. Add `Construct` method or extend existing `[Inject]` method to accept `CameraView` parameter in `Assets/Features/Input/Views/InputBridge.cs` (line ~143)
- [X] T035 [US4] Replace `FindObjectOfType<InputBridge>()` in `RadialMenuController.Start()` with `[Inject]` method injection. Remove the FindObjectOfType call and its associated null-check-with-warning — VContainer's `RegisterComponentInHierarchy` will throw a clear resolution error if InputBridge is missing from the scene. File: `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs` (line ~83)
- [X] T036 [US4] Replace `FindObjectOfType<TargetingController>()` in `RadialMenuController.Start()` with `[Inject]` method injection. Retain the null guard + "Lock Target" disable logic added in T032 — it serves as a runtime safety net even with DI. File: `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs` (line ~89)
- [X] T037 [US4] Replace `FindObjectOfType<InputBridge>()` in `StationServicesMenuController.Start()` with `[Inject]` method injection in `Assets/Features/StationServices/Views/StationServicesMenuController.cs` (line ~128)
- [X] T038 [US4] Replace `FindObjectOfType<TargetPreviewManager>()` in `TargetingController.Start()` with `[Inject]` method injection in `Assets/Features/Targeting/Views/TargetingController.cs` (line ~68)
- [X] T039 [US4] Add `Debug.LogWarning` for `FindObjectOfType<CinemachineCamera>()` null result in `TargetingController.Start()` — log `[TargetingController] CinemachineCamera not found, ship tracking unavailable` in `Assets/Features/Targeting/Views/TargetingController.cs` (line ~71)
- [X] T040 [US4] Verify compilation clean and run full test suite after DI migration

**Checkpoint**: 5 FindObjectOfType calls replaced with DI. Remaining calls have null warnings. US4 independently testable.

---

## Phase 6: User Story 5 — Pause-Safe Refining (Priority: P3)

**Goal**: Refining jobs use `Time.realtimeSinceStartup` so they progress regardless of `Time.timeScale`.

**Independent Test**: Start refining job, set `timeScale = 0` in editor, wait real-time duration, restore. Job completes on schedule.

### Implementation for US5

- [X] T041 [US5] Change `Time.time` to `Time.realtimeSinceStartup` in `RefiningJobTicker.Update()` at line 32 in `Assets/Features/StationServices/Views/RefiningJobTicker.cs`
- [X] T042 [US5] Change `Time.time` to `Time.realtimeSinceStartup` in `RefineOresPanelController` `StartRefiningJobAction` dispatch at line 254 in `Assets/Features/StationServices/Views/RefineOresPanelController.cs`
- [X] T043 [US5] Change `Time.time` to `Time.realtimeSinceStartup` in `RefineOresPanelController.Update()` progress bar calculation (line ~88-105) — this method uses `Time.time` to compute elapsed/remaining for active job display. Must use same time source as ticker and dispatch. File: `Assets/Features/StationServices/Views/RefineOresPanelController.cs`
- [X] T044 [US5] Verify existing `RefiningJobLifecycleTests` still pass (they use `0f` start time, should be agnostic). Run full test suite.

**Checkpoint**: Refining uses real-time. US5 independently testable.

---

## Phase 7: User Story 6 — Designer Guardrails (Priority: P3)

**Goal**: All 8 ScriptableObject types produce `Debug.LogWarning` for invalid field configurations at edit time.

**Independent Test**: Create any SO, set an invalid value. Console shows a descriptive warning.

### Tests for US6

- [X] T045 [P] [US6] Write `OreDefinitionValidationTests` — test valid config produces no warnings; test each invalid field (BaseYieldPerSecond=0, Hardness=0, VolumePerUnit=0, BaseValue=-1, RarityWeight=2, empty OreId, empty DisplayName, null RefiningOutput Material, BaseYieldPerUnit=0, VarianceMin>VarianceMax) produces appropriate warning. Include at least one assertion verifying FR-027 format: warning string starts with `[{assetName}]` prefix. New file `Assets/Features/Mining/Tests/OreDefinitionValidationTests.cs`
- [X] T046 [P] [US6] Write `ShipArchetypeConfigValidationTests` — test valid config produces no warnings; test each invalid field (Mass=0, MaxThrust=0, CargoCapacity=0, BaseLockTime=0, MaxTargetLocks=0, empty ArchetypeId, empty DisplayName, negative damping) produces warning. New file `Assets/Features/Ship/Tests/ShipArchetypeConfigValidationTests.cs`
- [X] T047 [P] [US6] Write `AsteroidFieldDefinitionValidationTests` — test valid config produces no warnings; test AsteroidCount=0, FieldRadius=0, SizeMax<SizeMin, RotationMax<RotationMin, MinScaleFraction outside [0.1,0.5], empty OreEntries, null OreDefinition in entry, zero Weight. New file `Assets/Features/Procedural/Tests/AsteroidFieldValidationTests.cs`
- [X] T048 [P] [US6] Write `StationServicesConfigValidationTests` — test valid config produces no warnings; test MaxConcurrentRefiningSlots=0, RefiningSpeedMultiplier=0, RepairCostPerHP=-1. New file `Assets/Features/StationServices/Tests/StationServicesConfigValidationTests.cs`
- [X] T049 [P] [US6] Write `DockingConfigValidationTests` — test valid config produces no warnings; test MaxDockingRange=0, SnapRange=0, SnapRange>=MaxDockingRange, SnapDuration=0, UndockClearanceDistance=0, UndockDuration=0. New file `Assets/Features/Docking/Tests/DockingConfigValidationTests.cs`
- [X] T050 [P] [US6] Write `RawMaterialDefinitionValidationTests` — test valid config produces no warnings; test empty MaterialId, empty DisplayName. New file `Assets/Features/Resources/Tests/RawMaterialValidationTests.cs`
- [X] T051 [P] [US6] Write `StationServicesConfigMapValidationTests` — test valid config produces no warnings; test duplicate StationIds, null Config reference. New file `Assets/Features/StationServices/Tests/StationServicesConfigMapValidationTests.cs`
- [X] T052 [P] [US6] Write `GameServicesConfigValidationTests` — test valid config produces no warnings; test StartingCredits=-1. New file `Assets/Features/StationServices/Tests/GameServicesConfigValidationTests.cs`

### Implementation for US6

- [X] T053 [P] [US6] Add `OnValidate()` to `OreDefinition` with checks per FR-019: BaseYieldPerSecond>0, Hardness>0, VolumePerUnit>0, BaseValue>=0, RarityWeight in [0,1], BaseProcessingTimePerUnit>0, RefiningCreditCostPerUnit>=0, OreId not empty, DisplayName not empty, each RefiningOutputEntry non-null Material + BaseYieldPerUnit>0 + VarianceMin<=VarianceMax in `Assets/Features/Mining/Data/OreDefinition.cs`
- [X] T054 [P] [US6] Add `OnValidate()` to `ShipArchetypeConfig` with checks per FR-020: Mass>0, MaxThrust>0, MaxSpeed>0, RotationTorque>0, LinearDamping>=0, AngularDamping>=0, MiningPower>=0, ModuleSlots>=0, CargoCapacity>0, BaseLockTime>0, MaxTargetLocks>=1, MaxLockRange>0, ArchetypeId not empty, DisplayName not empty in `Assets/Features/Ship/Data/ShipArchetypeConfig.cs`
- [X] T055 [P] [US6] Add `OnValidate()` to `AsteroidFieldDefinition` with checks per FR-021: AsteroidCount>0, FieldRadius>0, AsteroidSizeMin>0, AsteroidSizeMax>=AsteroidSizeMin, RotationSpeedMax>=RotationSpeedMin, MinScaleFraction in [0.1,0.5], at least one OreEntry with non-null OreDefinition and Weight>0 in `Assets/Features/Procedural/Data/AsteroidFieldDefinition.cs`
- [X] T056 [P] [US6] Add `OnValidate()` to `StationServicesConfig` with checks per FR-022: MaxConcurrentRefiningSlots>=1, RefiningSpeedMultiplier>0, RepairCostPerHP>=0 in `Assets/Features/StationServices/Data/StationServicesConfig.cs`
- [X] T057 [P] [US6] Add `OnValidate()` to `DockingConfig` with checks per FR-023: MaxDockingRange>0, SnapRange>0, SnapRange<MaxDockingRange, SnapDuration>0, UndockClearanceDistance>0, UndockDuration>0 in `Assets/Features/Docking/Data/DockingConfig.cs`
- [X] T058 [P] [US6] Add `OnValidate()` to `RawMaterialDefinition` with checks per FR-024: MaterialId not empty, DisplayName not empty in `Assets/Features/Resources/Data/RawMaterialDefinition.cs`
- [X] T059 [P] [US6] Add `OnValidate()` to `StationServicesConfigMap` with checks per FR-025: no duplicate StationIds, all StationServicesConfig references non-null in `Assets/Features/StationServices/Data/StationServicesConfigMap.cs`
- [X] T060 [P] [US6] Add `OnValidate()` to `GameServicesConfig` with checks per FR-026: StartingCredits>=0 in `Assets/Features/StationServices/Data/GameServicesConfig.cs`
- [X] T061 [US6] Run all US6 validation tests — confirm tests pass (Green). Run full test suite to verify no regressions.

**Checkpoint**: All 8 SOs have edit-time validation. 8 new test files. US6 independently testable.

---

## Phase 8: User Story 7 — Consistent Create Menus (Priority: P3)

**Goal**: All VoidHarvest ScriptableObject Create menu paths follow `VoidHarvest/<System>/<AssetType>` pattern.

**Independent Test**: Navigate Create > VoidHarvest menu. All assets grouped by system with human-readable spacing.

### Implementation for US7

- [X] T062 [P] [US7] Update `CreateAssetMenu` on `OreDefinition` from `VoidHarvest/Ore Definition` to `VoidHarvest/Mining/Ore Definition` in `Assets/Features/Mining/Data/OreDefinition.cs`
- [X] T063 [P] [US7] Update `CreateAssetMenu` on `ShipArchetypeConfig` from `VoidHarvest/ShipArchetypeConfig` to `VoidHarvest/Ship/Ship Archetype Config` in `Assets/Features/Ship/Data/ShipArchetypeConfig.cs`
- [X] T064 [P] [US7] Update `CreateAssetMenu` on `AsteroidFieldDefinition` from `VoidHarvest/Asteroid Field Definition` to `VoidHarvest/Procedural/Asteroid Field Definition` in `Assets/Features/Procedural/Data/AsteroidFieldDefinition.cs`
- [X] T065 [P] [US7] Update `CreateAssetMenu` on `StationServicesConfig` from `VoidHarvest/Station Services Config` to `VoidHarvest/Station/Station Services Config` in `Assets/Features/StationServices/Data/StationServicesConfig.cs`
- [X] T066 [P] [US7] Update `CreateAssetMenu` on `RawMaterialDefinition` from `VoidHarvest/Raw Material Definition` to `VoidHarvest/Station/Raw Material Definition` in `Assets/Features/Resources/Data/RawMaterialDefinition.cs`
- [X] T067 [P] [US7] Update `CreateAssetMenu` on `GameServicesConfig` from `VoidHarvest/Game Services Config` to `VoidHarvest/Station/Game Services Config` in `Assets/Features/StationServices/Data/GameServicesConfig.cs`
- [X] T068 [P] [US7] Update `CreateAssetMenu` on `StationServicesConfigMap` from `VoidHarvest/Station Services Config Map` to `VoidHarvest/Station/Station Services Config Map` in `Assets/Features/StationServices/Data/StationServicesConfigMap.cs`
- [X] T069 [P] [US7] Update `CreateAssetMenu` on `DockingConfig` from `VoidHarvest/Docking/DockingConfig` to `VoidHarvest/Docking/Docking Config` in `Assets/Features/Docking/Data/DockingConfig.cs`
- [X] T070 [P] [US7] Update `CreateAssetMenu` on `DockingVFXConfig` from `VoidHarvest/DockingVFXConfig` to `VoidHarvest/Docking/Docking VFX Config` in `Assets/Features/Docking/Data/DockingVFXConfig.cs`
- [X] T071 [P] [US7] Update `CreateAssetMenu` on `DockingAudioConfig` from `VoidHarvest/DockingAudioConfig` to `VoidHarvest/Docking/Docking Audio Config` in `Assets/Features/Docking/Data/DockingAudioConfig.cs`
- [X] T072 [P] [US7] Update `CreateAssetMenu` on `MiningVFXConfig` from `VoidHarvest/Mining/MiningVFXConfig` to `VoidHarvest/Mining/Mining VFX Config` in `Assets/Features/Mining/Data/MiningVFXConfig.cs`
- [X] T073 [P] [US7] Update `CreateAssetMenu` on `MiningAudioConfig` from `VoidHarvest/Mining/MiningAudioConfig` to `VoidHarvest/Mining/Mining Audio Config` in `Assets/Features/Mining/Data/MiningAudioConfig.cs`
- [X] T074 [P] [US7] Update `CreateAssetMenu` on `OreChunkConfig` from `VoidHarvest/Mining/OreChunkConfig` to `VoidHarvest/Mining/Ore Chunk Config` in `Assets/Features/Mining/Data/OreChunkConfig.cs`
- [X] T075 [P] [US7] Update `CreateAssetMenu` on `DepletionVFXConfig` from `VoidHarvest/Mining/DepletionVFXConfig` to `VoidHarvest/Mining/Depletion VFX Config` in `Assets/Features/Mining/Data/DepletionVFXConfig.cs`
- [X] T076 [P] [US7] Update `CreateAssetMenu` on `TargetingConfig` from `VoidHarvest/Targeting Config` to `VoidHarvest/Targeting/Targeting Config` in `Assets/Features/Targeting/Data/TargetingConfig.cs`
- [X] T077 [P] [US7] Update `CreateAssetMenu` on `TargetingVFXConfig` from `VoidHarvest/Targeting VFX Config` to `VoidHarvest/Targeting/Targeting VFX Config` in `Assets/Features/Targeting/Data/TargetingVFXConfig.cs`
- [X] T078 [P] [US7] Update `CreateAssetMenu` on `TargetingAudioConfig` from `VoidHarvest/Targeting Audio Config` to `VoidHarvest/Targeting/Targeting Audio Config` in `Assets/Features/Targeting/Data/TargetingAudioConfig.cs`
- [X] T079 [P] [US7] Update `CreateAssetMenu` on `StationPresetConfig` from `VoidHarvest/StationPresetConfig` to `VoidHarvest/Station/Station Preset Config` in `Assets/Features/Base/Data/StationPresetConfig.cs`
- [X] T080 [P] [US7] Update `CreateAssetMenu` on `SkyboxConfig` from `VoidHarvest/SkyboxConfig` to `VoidHarvest/Camera/Skybox Config` in `Assets/Features/Camera/Data/SkyboxConfig.cs`
- [X] T081 [US7] Verify compilation clean after all CreateAssetMenu changes

**Checkpoint**: All 19 Create menu paths standardized. US7 independently testable.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation, and regression testing

- [X] T082 Run full test suite — all 465 original + new tests must pass. Target total: 495+
- [X] T083 Check Unity console via MCP — zero errors, zero project warnings
- [X] T084 Verify all existing ScriptableObject assets in project produce no OnValidate warnings (assets have valid data)
- [X] T085 Document async subscription lifecycle convention as a code comment block in `Assets/Core/SceneLifetimeScope.cs` per FR-032 — standard pattern: OnEnable subscribe / OnDisable cancel-dispose / OnDestroy safety net
- [X] T086 Manual verification: SC-002 — dock/undock/redock cycle 3+ times, verify zero duplicate event handlers and zero console errors (requires PlayMode; no automated test — PlayMode tests out of scope)
- [X] T087 Manual verification: SC-005 — start refining job, set `Time.timeScale = 0` in editor, wait real-time duration, verify job completes on schedule (requires PlayMode; no automated test)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **US1 (Phase 2)**: Depends on Setup (T001 baseline). No dependency on DI changes.
- **US2 (Phase 3)**: Depends on Setup (T001 baseline). No dependency on DI changes.
- **US3 (Phase 4)**: Depends on Setup (T002 DI registration for InputBridge inject in T031).
- **US4 (Phase 5)**: Depends on Setup (T002–T005 DI registrations).
- **US5 (Phase 6)**: Depends on Setup (T001 baseline). Independent of all other stories.
- **US6 (Phase 7)**: Depends on Setup (T001 baseline). Independent of all other stories.
- **US7 (Phase 8)**: Depends on Setup (T001 baseline). Independent of all other stories.
- **Polish (Phase 9)**: Depends on all user stories complete.

### User Story Dependencies

- **US1 (P1)**: Independent — can start after Phase 1
- **US2 (P1)**: Independent — can start after Phase 1. Can run parallel with US1.
- **US3 (P2)**: Depends on T002 (InputBridge DI registration) for T031 state sync subscription
- **US4 (P2)**: Depends on T002–T005 (all DI registrations)
- **US5 (P3)**: Independent — can start after Phase 1
- **US6 (P3)**: Independent — can start after Phase 1
- **US7 (P3)**: Independent — can start after Phase 1

### Within Each User Story

- Tests (T007, T045–T052) written FIRST — must FAIL before implementation
- Implementation tasks make tests pass (Green)
- Verification task confirms all tests pass and no regressions

### Parallel Opportunities

- **Phase 1**: T002–T005 all modify SceneLifetimeScope (sequential within file)
- **Phase 2**: T007 — single task creates all US1 tests in one file
- **Phase 3**: T020–T024 all [P] — OnDestroy safety nets in different files
- **Phase 5**: T034–T039 could be parallelized with care (different files)
- **Phase 7**: T045–T052 all [P] — 8 test files in parallel. T053–T060 all [P] — 8 SO implementations in parallel.
- **Phase 8**: T062–T080 all [P] — 19 CreateAssetMenu updates across different files
- **Cross-story**: US1+US2 can run in parallel. US5+US6+US7 can all run in parallel.

---

## Parallel Example: User Story 6

```
# Launch all 8 validation test files in parallel:
T045: OreDefinitionValidationTests.cs
T046: ShipArchetypeConfigValidationTests.cs
T047: AsteroidFieldValidationTests.cs
T048: StationServicesConfigValidationTests.cs
T049: DockingConfigValidationTests.cs
T050: RawMaterialValidationTests.cs
T051: StationServicesConfigMapValidationTests.cs
T052: GameServicesConfigValidationTests.cs

# Then launch all 8 OnValidate implementations in parallel:
T053: OreDefinition.OnValidate()
T054: ShipArchetypeConfig.OnValidate()
T055: AsteroidFieldDefinition.OnValidate()
T056: StationServicesConfig.OnValidate()
T057: DockingConfig.OnValidate()
T058: RawMaterialDefinition.OnValidate()
T059: StationServicesConfigMap.OnValidate()
T060: GameServicesConfig.OnValidate()
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete Phase 1: Setup (DI registrations + baseline)
2. Complete Phase 2: US1 — Station Panel Responsiveness
3. Complete Phase 3: US2 — Reliable Event Subscriptions
4. **STOP and VALIDATE**: Both P1 stories fix critical player-facing bugs
5. The two most impactful bugs are now resolved

### Incremental Delivery

1. Setup → US1 + US2 (P1 critical bugs) → Validate
2. Add US3 + US4 (P2 developer experience) → Validate
3. Add US5 + US6 + US7 (P3 polish) → Validate
4. Polish phase → Final regression test → Done

### Parallel Strategy

With parallel execution capacity:
1. Complete Setup (sequential — single file)
2. US1 + US2 in parallel (independent, both P1)
3. US3 + US5 + US6 + US7 in parallel (all independent of each other)
4. US4 after Setup DI registrations complete
5. Polish after all stories complete

---

## Notes

- All file paths are relative to `Assets/` in the Unity project root
- [P] tasks = different files, no dependencies on incomplete tasks in same phase
- [Story] label maps every task to its user story for traceability
- TDD approach: Tests (Red) → Implementation (Green) → Verify no regressions
- Commit after each phase or logical group of tasks
- MCP compilation check required after every phase (T006, T025, T033, T040, T044, T061, T081, T083)
- All OnValidate tests use `LogAssert.Expect(LogType.Warning, ...)` for warning verification
