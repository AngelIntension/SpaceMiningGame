# Feature Specification: Bugfix, Event Lifecycle & UI Polish

**Feature Branch**: `feature/008-bugfix-and-polish`
**Created**: 2026-03-02
**Status**: Draft
**Input**: Systematic bugfix and polish pass across all Phase 0 systems — UI state-change detection, event subscription lifecycle, silent input failures, FindObjectOfType fragility, time source correctness, ScriptableObject validation, and Create menu consistency.

## User Scenarios & Testing

### User Story 1 — Station Panel Responsiveness (Priority: P1)

A player docks at a station, opens the services menu, and starts a refining job. When the job completes, ALL station panels (cargo, refining, sell, repair) immediately reflect the updated state without requiring the player to close and reopen the menu.

**Why this priority**: This is a player-facing bug that causes visible stale data and makes the station services feel broken. Players lose trust in the UI when panels show incorrect cargo counts or outdated refining status.

**Independent Test**: Dock at a station, start a refining job, wait for completion. Verify cargo panel shows updated storage, refining panel shows job complete, and sell panel reflects new available resources — all without menu re-open.

**Acceptance Scenarios**:

1. **Given** a player is docked and viewing the cargo transfer panel, **When** a refining job completes (changing `StationServicesState` but NOT `InventoryState`), **Then** the cargo panel refreshes to show updated station storage.
2. **Given** a player is viewing the refining panel, **When** inventory changes due to a cargo transfer, **Then** the refining panel refreshes to show updated available ore quantities.
3. **Given** a player is viewing any station panel, **When** any single relevant state slice changes, **Then** the panel refreshes (the system does NOT require ALL slices to change simultaneously).

---

### User Story 2 — Reliable Event Subscriptions (Priority: P1)

A player docks, opens the station menu, closes it, undocks, docks again, and reopens the menu. Each time, the menu and all panels respond correctly — no duplicate event handlers fire, no stale subscriptions remain from previous activations.

**Why this priority**: Subscription leaks cause progressively worsening bugs (duplicate audio, duplicate UI updates, memory growth) that compound over a play session and are extremely hard for players to diagnose.

**Independent Test**: Dock/undock/redock cycle 3 times. Verify no duplicate audio cues play, no duplicate UI refreshes fire, and no console errors appear about disposed subscriptions.

**Acceptance Scenarios**:

1. **Given** a player docks and then undocks (station menu hidden via `SetActive(false)`), **When** the player docks again and the menu reappears, **Then** only ONE set of event subscriptions is active (no duplicates from previous activation).
2. **Given** a panel controller's parent is destroyed before the child, **When** the child's `OnDestroy` fires, **Then** the child safely cleans up its own subscriptions without errors.
3. **Given** a `RadialMenuController` is disabled and re-enabled, **When** it re-enables, **Then** old subscriptions are cancelled and new ones are created cleanly.

---

### User Story 3 — Visible Input Feedback (Priority: P2)

A player selects an asteroid and presses the mining hotkey, but the asteroid entity has been destroyed (depleted). Instead of nothing happening, the player sees a clear indication that the target is no longer valid, and the selection is cleared.

**Why this priority**: Silent failures erode player confidence. Even without fancy VFX, a console warning or selection clear gives the player (and developers) actionable feedback.

**Independent Test**: Select an asteroid, deplete it fully via mining, then attempt to mine again via hotkey. Verify that selection is cleared and a warning is logged.

**Acceptance Scenarios**:

1. **Given** a player has a selected asteroid that no longer exists in ECS, **When** the player activates mining via hotkey, **Then** the system clears the selection and logs a warning.
2. **Given** ECS is not yet initialized, **When** the player attempts to dock via `InputBridge`, **Then** a warning is logged indicating ECS is not ready.
3. **Given** a `TargetingController` is missing from the scene, **When** `RadialMenuController` starts, **Then** a warning is logged and the "Lock Target" menu segment is disabled rather than silently failing.

---

### User Story 4 — Robust Component Discovery (Priority: P2)

A designer rearranges GameObjects in the scene hierarchy or changes script execution order. The game still initializes correctly because components are resolved through dependency injection rather than fragile runtime searches.

**Why this priority**: `FindObjectOfType` creates hidden ordering dependencies that cause intermittent failures. DI resolution ensures deterministic initialization regardless of hierarchy or execution order.

**Independent Test**: Change script execution order for `InputBridge` and `RadialMenuController` in Project Settings. Verify all cross-component references still resolve correctly.

**Acceptance Scenarios**:

1. **Given** components that previously used `FindObjectOfType` to locate peers, **When** the scene loads in any execution order, **Then** all references are resolved via VContainer injection.
2. **Given** a required injected dependency is not registered in the DI container, **When** the scene loads, **Then** VContainer reports a clear resolution error at startup (not a silent null at runtime).

---

### User Story 5 — Pause-Safe Refining (Priority: P3)

A player starts a refining job and then pauses the game (or a future pause menu sets `timeScale = 0`). Refining continues using real-world time, consistent with EVE Online's convention that station processes are autonomous.

**Why this priority**: While no pause menu exists yet, using the correct time source now prevents a subtle regression when pause is added later. The fix is trivial and prevents future debugging.

**Independent Test**: Start a refining job, set `Time.timeScale = 0` via editor, wait the real-time duration, restore `timeScale`. Verify job completes.

**Acceptance Scenarios**:

1. **Given** a refining job is in progress, **When** `Time.timeScale` is set to 0, **Then** the job timer continues advancing based on real-world elapsed time.
2. **Given** a new refining job is started, **When** the start time is recorded, **Then** the recorded time uses `Time.realtimeSinceStartup` (not `Time.time`).

---

### User Story 6 — Designer Guardrails (Priority: P3)

A designer creates a new `OreDefinition` ScriptableObject and accidentally sets `BaseYieldPerSecond` to 0. The Unity Inspector immediately shows a warning in the console, and the value is clamped or flagged so the issue is caught before entering Play mode.

**Why this priority**: Edit-time validation catches configuration errors early, preventing hard-to-diagnose runtime bugs. This is a quality-of-life improvement for the development workflow.

**Independent Test**: Create an `OreDefinition` asset, set `BaseYieldPerSecond` to 0. Verify console shows a warning identifying the invalid field.

**Acceptance Scenarios**:

1. **Given** a designer sets an invalid value on any ScriptableObject (e.g., zero mass, negative yield), **When** the Inspector validates (`OnValidate`), **Then** a descriptive warning is logged identifying the asset name, field, and expected range.
2. **Given** all existing ScriptableObject assets in the project, **When** `OnValidate` runs on each, **Then** no warnings are produced (all current assets have valid configurations).

---

### User Story 7 — Consistent Create Menus (Priority: P3)

A designer wants to create a new Docking VFX Config asset. They navigate to Create > VoidHarvest > Docking > Docking VFX Config. All VoidHarvest assets follow the same `VoidHarvest/<System>/<AssetType>` hierarchy.

**Why this priority**: Inconsistent menu paths waste designer time and create confusion about which system owns which asset. This is a low-effort, high-tidiness improvement.

**Independent Test**: Navigate through every VoidHarvest Create menu path. Verify all follow the `VoidHarvest/<System>/<AssetType>` pattern with no flat or inconsistent entries.

**Acceptance Scenarios**:

1. **Given** the Unity Editor Create menu, **When** a designer navigates the VoidHarvest submenu, **Then** all assets are organized under `VoidHarvest/<System>/<AssetType>` with consistent spacing and naming.
2. **Given** the complete list of VoidHarvest ScriptableObjects, **When** each is checked, **Then** none use a flat path (e.g., `VoidHarvest/DockingVFXConfig` is replaced by `VoidHarvest/Docking/Docking VFX Config`).

---

### Edge Cases

- What happens when a state dispatch triggers `StateChangedEvent` but no state slice actually changes? (Panel should skip refresh via reference equality — no unnecessary work.)
- What happens when `OnDisable` is called but `OnEnable` was never called? (Should safely no-op — `_eventCts` is null.)
- What happens when `OnValidate` runs during asset import (not from Inspector)? (Should still log warnings but must not throw exceptions that block import.)
- What happens when VContainer fails to resolve an optional dependency? (Should degrade gracefully with a warning, not crash.)
- What happens when `Time.realtimeSinceStartup` wraps or resets? (Extremely unlikely in practice; document as known limitation.)

## Requirements

### Functional Requirements

- **FR-001**: Station panel controllers MUST refresh when ANY relevant state slice changes, not only when ALL slices change simultaneously.
- **FR-002**: The `CargoTransferPanelController` MUST use OR-logic (`||`) for state-change detection across `InventoryState` and `StationServicesState` slices: skip refresh only when NEITHER slice has changed.
- **FR-003**: The `RefineOresPanelController` MUST detect changes to both `StationServicesState` AND `InventoryState` so that ore quantity changes from cargo transfers are reflected.
- **FR-004**: All station panel controllers (`CargoTransfer`, `RefineOres`, `SellResources`, `BasicRepair`) MUST follow the same state-change detection pattern for consistency.
- **FR-005**: All async event subscription MonoBehaviours MUST start subscriptions in `OnEnable()` and cancel/dispose them in `OnDisable()`, following the project-standard lifecycle pattern.
- **FR-006**: Each station panel controller MUST implement `OnDestroy()` as a safety net that calls its cleanup logic, ensuring subscriptions are cancelled even if the parent destroys the child unexpectedly.
- **FR-007**: `RadialMenuController` MUST move subscription start from `Start()` to `OnEnable()` and cancellation from `OnDestroy()` to `OnDisable()`.
- **FR-008**: `TargetingAudioController` MUST move subscription start from `Start()` to `OnEnable()` and cancellation from `OnDestroy()` to `OnDisable()`.
- **FR-009**: `InputBridge.StartMining()` MUST dispatch `ClearSelectionAction` and log a warning when the selected asteroid entity no longer exists.
- **FR-010**: `InputBridge.InitiateDocking()` MUST log a warning when ECS is not ready or docking port is null.
- **FR-011**: `InputBridge.OnHotbar1()` MUST validate that the target entity still exists before dispatching mining actions.
- **FR-012**: `InputBridge.TryInitializeECS()` MUST log a warning once after repeated initialization failures (not every frame).
- **FR-013**: `InputBridge.SyncSelectionFromState()` MUST be called when `StateChangedEvent` fires with a changed `Selection.TargetId`, keeping InputBridge in sync with the state store.
- **FR-014**: `RadialMenuController` MUST log a warning and disable the "Lock Target" segment if `TargetingController` is not found.
- **FR-015**: All `FindObjectOfType<T>()` calls MUST be replaced with VContainer `[Inject]` where the target is registered in the DI container. For components not in the container, register them in `SceneLifetimeScope`.
- **FR-016**: As a minimum fallback, any remaining `FindObjectOfType` calls (if DI is infeasible) MUST include a `Debug.LogWarning` when the result is null.
- **FR-017**: `RefiningJobTicker` MUST use `Time.realtimeSinceStartup` instead of `Time.time` so refining progresses regardless of `Time.timeScale`.
- **FR-018**: `StartRefiningJobAction` dispatch MUST record `Time.realtimeSinceStartup` as the job start time.
- **FR-019**: `OreDefinition` MUST implement `OnValidate()` with checks for: BaseYieldPerSecond > 0, Hardness > 0, VolumePerUnit > 0, BaseValue >= 0, RarityWeight in [0,1], BaseProcessingTimePerUnit > 0, RefiningCreditCostPerUnit >= 0, OreId not empty, DisplayName not empty, and each RefiningOutputEntry having non-null Material and BaseYieldPerUnit > 0 with VarianceMin <= VarianceMax.
- **FR-020**: `ShipArchetypeConfig` MUST implement `OnValidate()` with checks for: Mass > 0, MaxThrust > 0, MaxSpeed > 0, RotationTorque > 0, LinearDamping >= 0, AngularDamping >= 0, MiningPower >= 0, ModuleSlots >= 0, CargoCapacity > 0, BaseLockTime > 0, MaxTargetLocks >= 1, MaxLockRange > 0, ArchetypeId not empty, DisplayName not empty.
- **FR-021**: `AsteroidFieldDefinition` MUST implement `OnValidate()` with checks for: AsteroidCount > 0, FieldRadius > 0, AsteroidSizeMin > 0, AsteroidSizeMax >= AsteroidSizeMin, RotationSpeedMax >= RotationSpeedMin, MinScaleFraction in [0.1, 0.5], and at least one OreEntry with non-null OreDefinition and Weight > 0.
- **FR-022**: `StationServicesConfig` MUST implement `OnValidate()` with checks for: MaxConcurrentRefiningSlots >= 1, RefiningSpeedMultiplier > 0, RepairCostPerHP >= 0.
- **FR-023**: `DockingConfig` MUST implement `OnValidate()` with checks for: MaxDockingRange > 0, SnapRange > 0 and < MaxDockingRange, SnapDuration > 0, UndockClearanceDistance > 0, UndockDuration > 0.
- **FR-024**: `RawMaterialDefinition` MUST implement `OnValidate()` with checks for: MaterialId not empty, DisplayName not empty.
- **FR-025**: `StationServicesConfigMap` MUST implement `OnValidate()` with checks for: no duplicate StationIds and all StationServicesConfig references non-null.
- **FR-026**: `GameServicesConfig` MUST implement `OnValidate()` with checks for: StartingCredits >= 0.
- **FR-027**: All `OnValidate()` implementations MUST use `Debug.LogWarning($"[{name}] FieldX ...")` format and MUST NOT throw exceptions.
- **FR-028**: All VoidHarvest ScriptableObject Create menu paths MUST follow the pattern `VoidHarvest/<System>/<AssetType>` with consistent human-readable spacing.
- **FR-029**: All existing 465 tests MUST continue to pass after all changes.
- **FR-030**: New unit tests MUST be added for all `OnValidate()` logic verifying both valid and invalid configurations.
- **FR-031**: New unit tests MUST verify that state-change detection in panel controllers correctly refreshes when any single relevant slice changes.
- **FR-032**: The project MUST establish and document a standard async subscription lifecycle convention (`OnEnable` subscribe / `OnDisable` cancel-dispose) for all current and future MonoBehaviours with async event listeners.

### Key Entities

- **Panel State-Change Detector**: The logic within each station panel controller that determines whether to refresh the UI based on state slice reference changes. Uses reference equality on immutable state records.
- **Async Subscription Lifecycle**: The `CancellationTokenSource` pattern tied to `OnEnable`/`OnDisable` for managing UniTask-based event subscriptions.
- **ScriptableObject Validator**: The `OnValidate()` method on each SO that enforces field constraints at edit time via `Debug.LogWarning`.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All station service panels update within one frame of any relevant state change — zero instances of stale data visible to the player after state mutations.
- **SC-002**: A dock/undock/redock cycle repeated 10 times produces zero duplicate event handler invocations and zero subscription-related console errors.
- **SC-003**: All player input actions (mining, docking, hotbar, targeting) produce visible feedback — either the intended action or a logged warning. Zero silent failures.
- **SC-004**: All cross-component references resolve deterministically regardless of script execution order. Zero `NullReferenceException` from unresolved `FindObjectOfType` calls.
- **SC-005**: Refining jobs complete on schedule when `Time.timeScale = 0`, with timing deviation under 0.1 seconds from expected real-time duration.
- **SC-006**: All 8 ScriptableObject types with `OnValidate()` produce warnings for every invalid field configuration. Zero false negatives (invalid config with no warning) and zero false positives (valid config with spurious warning).
- **SC-007**: 100% of VoidHarvest Create menu paths follow the `VoidHarvest/<System>/<AssetType>` pattern.
- **SC-008**: All 465 existing tests pass. Total test count increases by at least 30 new tests covering validation, state-change detection, and subscription lifecycle.

## Assumptions

- All station panel controllers currently follow a similar `ListenForStateChanges` pattern with reference-equality checks. Each will be verified individually during implementation.
- `FindObjectOfType` replacements assume that all target components can be registered in VContainer's `SceneLifetimeScope`. If a component cannot be registered (e.g., dynamically spawned), a fallback with `Debug.LogWarning` will be used.
- The `OnEnable`/`OnDisable` subscription lifecycle pattern is compatible with all existing MonoBehaviour activation patterns in the project.
- No station panel controllers are currently instantiated at runtime via code (they are scene-placed or UI Toolkit-managed). If any are runtime-created, the lifecycle pattern still applies.
- `Time.realtimeSinceStartup` is monotonically increasing and does not reset during normal gameplay sessions.

## Scope Boundaries

### In Scope

- Fix UI state-change detection logic in all station panel controllers
- Migrate async event subscriptions to `OnEnable`/`OnDisable` lifecycle
- Add defensive logging for silent input failures in `InputBridge` and `RadialMenuController`
- Replace `FindObjectOfType` with VContainer DI injection where feasible
- Switch `RefiningJobTicker` and job start time to `Time.realtimeSinceStartup`
- Add `OnValidate()` to 8 ScriptableObject types
- Standardize all Create menu paths to `VoidHarvest/<System>/<AssetType>`
- Unit tests for all new logic
- Document async subscription lifecycle convention

### Out of Scope

- New gameplay features or mechanics
- Data-driven station/world configuration (Spec 009)
- Custom editor windows or property drawers (Spec 009)
- PlayMode integration tests
- VFX or audio polish
- Performance optimization passes
