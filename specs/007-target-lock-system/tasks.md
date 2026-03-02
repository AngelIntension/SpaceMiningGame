# Tasks: In-Flight Targeting & Multi-Target Lock System

**Input**: Design documents from `/specs/007-target-lock-system/`
**Prerequisites**: plan.md, spec.md, data-model.md, research.md, quickstart.md, contracts/

**Tests**: TDD is mandatory per constitution. Tests are included for all pure logic (Red-Green-Refactor).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Unity project root: `Assets/`
- Feature code: `Assets/Features/Targeting/{Data,Systems,Views,Tests}/`
- Core: `Assets/Core/{State,EventBus,Extensions}/`
- Ship data: `Assets/Features/Ship/Data/`
- Input views: `Assets/Features/Input/Views/`
- HUD views: `Assets/Features/HUD/Views/`

---

## Phase 1: Setup

**Purpose**: Create directory structure, assembly definitions, and ScriptableObject infrastructure for the new Targeting feature module.

- [ ] T001 Create `Assets/Features/Targeting/` directory tree with `Data/`, `Systems/`, `Views/`, `Tests/` subdirectories
- [ ] T002 [P] Create assembly definition `Assets/Features/Targeting/VoidHarvest.Features.Targeting.asmdef` with references: Core.Extensions, Core.State, Core.EventBus, Features.Ship, Features.Mining, VContainer, UniTask, Unity.Mathematics, Unity.Entities, Unity.Collections, Unity.Transforms
- [ ] T003 [P] Create test assembly definition `Assets/Features/Targeting/Tests/VoidHarvest.Features.Targeting.Tests.asmdef` with references: VoidHarvest.Features.Targeting, Core.Extensions, Core.State, Core.EventBus, nunit.framework, UnityEngine.TestRunner, UnityEditor.TestRunner; Editor-only platform
- [ ] T004 [P] Add `BaseLockTime` (float, default 1.5f), `MaxTargetLocks` (int, default 3), `MaxLockRange` (float, default 5000f) fields to `Assets/Features/Ship/Data/ShipArchetypeConfig.cs`. Update all 3 existing ship archetype assets (StarterMiningBarge, MediumMiningBarge, HeavyMiningBarge) with default values.
- [ ] T005 [P] Create `Assets/Features/Targeting/Data/TargetingConfig.cs` — ScriptableObject: ReticlePadding (float, default 20), ReticleMinSize (float, default 40), ReticleMaxSize (float, default 300), LockProgressArcWidth (float, default 3), OffScreenIndicatorMargin (float, default 30), ViewportRenderSize (int, default 128), ViewportFOV (float, default 30), PreviewStageOffset (Vector3, default (0, -1000, 0)). CreateAssetMenu path "VoidHarvest/Targeting Config". Create default asset at `Assets/Features/Targeting/Data/Assets/TargetingConfig.asset`.
- [ ] T006 [P] Create `Assets/Features/Targeting/Views/TargetingAudioConfig.cs` — ScriptableObject: LockAcquiringClip (AudioClip), LockConfirmedClip (AudioClip), LockFailedClip (AudioClip), LockSlotsFullClip (AudioClip), TargetLostClip (AudioClip). CreateAssetMenu path "VoidHarvest/Targeting Audio Config". Create default asset with null clips (placeholder).
- [ ] T007 Add "TargetPreview" layer to Unity project via `Assets/Features/Targeting/Views/TargetingVFXConfig.cs` — ScriptableObject: LockFlashDuration (float, default 0.3f), ReticlePulseSpeed (float, default 2.0f). Also configure the "TargetPreview" layer in Tags & Layers settings via Unity MCP `manage_editor` add_layer action.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core interfaces, state records, reducer, pure math functions, DI wiring, and GameState integration. MUST complete before any user story.

**CRITICAL**: No user story work can begin until this phase is complete.

### Core Interfaces (Core/Extensions — zero dependencies)

- [ ] T008 [P] Create `Assets/Core/Extensions/ITargetable.cs` — interface: TargetId (int, get), DisplayName (string, get), TypeLabel (string, get), TargetType (TargetType, get). Namespace: VoidHarvest.Core.Extensions.
- [ ] T009 [P] Create `Assets/Core/Extensions/TargetInfo.cs` — readonly struct: TargetId (int), DisplayName (string), TypeLabel (string), TargetType (TargetType), IsValid (bool, derived: TargetId >= 0). Static None sentinel (TargetId=-1, TargetType=None, empty strings). Factory methods: From(ITargetable target), FromAsteroid(int entityIndex, string displayName, string oreTypeName). Namespace: VoidHarvest.Core.Extensions.

### State Data Types (all different files, parallelizable)

- [ ] T010 [P] Create `Assets/Features/Targeting/Data/LockAcquisitionStatus.cs` — enum: None=0, InProgress=1, Completed=2, Cancelled=3. Namespace: VoidHarvest.Features.Targeting.Data.
- [ ] T011 [P] Create `Assets/Features/Targeting/Data/LockFailReason.cs` — enum: Deselected=0, OutOfRange=1, TargetDestroyed=2. Namespace: VoidHarvest.Features.Targeting.Data.
- [ ] T012 [P] Create `Assets/Features/Targeting/Data/SelectionData.cs` — readonly struct: TargetId (int), TargetType (TargetType), DisplayName (string), TypeLabel (string). HasSelection (bool, derived: TargetId >= 0). Static None sentinel (TargetId=-1, TargetType=None, empty strings). Constructor taking all 4 fields. Namespace: VoidHarvest.Features.Targeting.Data.
- [ ] T013 [P] Create `Assets/Features/Targeting/Data/LockAcquisitionData.cs` — readonly struct: TargetId (int), ElapsedTime (float), TotalDuration (float), Status (LockAcquisitionStatus). Progress (float, derived: TotalDuration > 0 ? Mathf.Clamp01(ElapsedTime / TotalDuration) : 0). IsActive (bool, derived: Status == InProgress). Static None sentinel (TargetId=-1, Status=None). Namespace: VoidHarvest.Features.Targeting.Data.
- [ ] T014 [P] Create `Assets/Features/Targeting/Data/TargetLockData.cs` — readonly struct: TargetId (int), TargetType (TargetType), DisplayName (string), TypeLabel (string). Constructor taking all 4 fields. Namespace: VoidHarvest.Features.Targeting.Data.
- [ ] T015 [P] Create `Assets/Features/Targeting/Data/TargetingState.cs` — sealed record: Selection (SelectionData), LockAcquisition (LockAcquisitionData), LockedTargets (ImmutableArray\<TargetLockData\>). Static Empty with Selection=SelectionData.None, LockAcquisition=LockAcquisitionData.None, LockedTargets=ImmutableArray\<TargetLockData\>.Empty. Namespace: VoidHarvest.Features.Targeting.Data.

### Actions & Events

- [ ] T016 [P] Create `Assets/Features/Targeting/Data/TargetingActions.cs` — ITargetingAction : IGameAction marker interface in `Features/Targeting/Data` (RootLifetimeScope already references all feature assemblies via using aliases — no circular dep). 8 sealed record action types per data-model.md: SelectTargetAction(int TargetId, TargetType TargetType, string DisplayName, string TypeLabel), ClearSelectionAction(), BeginLockAction(int TargetId, float Duration), LockTickAction(float DeltaTime), CompleteLockAction(), CancelLockAction(), UnlockTargetAction(int TargetId), ClearAllLocksAction(). Namespace: VoidHarvest.Features.Targeting.Data.
- [ ] T017 [P] Create `Assets/Features/Targeting/Data/TargetingEvents.cs` — 6 readonly struct events per data-model.md: TargetLockedEvent(int TargetId, string DisplayName), TargetUnlockedEvent(int TargetId), LockFailedEvent(int TargetId, LockFailReason Reason), LockSlotsFullEvent(), TargetLostEvent(int TargetId), AllLocksClearedEvent(). Namespace: VoidHarvest.Features.Targeting.Data.

### Core State Integration

- [ ] T018 Modify `Assets/Core/State/GameState.cs` — add `TargetingState Targeting` parameter to GameLoopState sealed record (after DockingState Docking). Add using for VoidHarvest.Features.Targeting.Data. Update any default construction to include TargetingState.Empty.
- [ ] T019 Modify `Assets/Core/RootLifetimeScope.cs` — add `ITargetingAction a => state with { Loop = state.Loop with { Targeting = TargetingReducer.Reduce(state.Loop.Targeting, a) } }` case in CompositeReducer switch expression. Add ClearAllLocksAction dispatch in CompleteDockingAction and CompleteUndockingAction cross-cutting handlers. Add using aliases for VoidHarvest.Features.Targeting.Data and VoidHarvest.Features.Targeting.Systems.

### Tests for Foundational Types (Red Phase — MUST FAIL)

- [ ] T020 [P] Write `Assets/Features/Targeting/Tests/TargetInfoTests.cs` — NUnit tests: TargetInfo.None has TargetId=-1 and IsValid=false, From(ITargetable) copies all fields correctly, FromAsteroid sets TargetType=Asteroid and TypeLabel to oreTypeName, IsValid returns true for TargetId >= 0, IsValid returns false for TargetId < 0, None sentinel has empty strings not null. Create a simple mock ITargetable for testing (private class in test file). Verifies ITargetable extensibility contract — any class implementing the interface produces valid TargetInfo. (FR-032, SC-008)
- [ ] T021 [P] Write `Assets/Features/Targeting/Tests/TargetingReducerTests.cs` — NUnit tests covering all 8 actions: SelectTargetAction sets Selection fields correctly, SelectTargetAction cancels active lock if target changed, ClearSelectionAction resets Selection to None and cancels active lock, BeginLockAction sets LockAcquisition with InProgress status, LockTickAction advances ElapsedTime by DeltaTime, LockTickAction does nothing when no active lock, CompleteLockAction appends to LockedTargets and resets LockAcquisition, CompleteLockAction ignored when status != Completed, CancelLockAction resets LockAcquisition to None, UnlockTargetAction removes specific target from LockedTargets, UnlockTargetAction returns unchanged when target not found, ClearAllLocksAction empties LockedTargets and resets Selection and LockAcquisition, duplicate lock prevention (BeginLock for already-locked target returns unchanged), LockedTargets maintains insertion order. (FR-001, FR-006, FR-007, FR-013, FR-015, FR-016, FR-021, FR-023, FR-024, FR-025)
- [ ] T022 [P] Write `Assets/Features/Targeting/Tests/LockTimeMathTests.cs` — NUnit tests: CalculateLockTime returns baseLockTime directly for v1, accepts TargetInfo parameter (extensibility), returns baseLockTime for TargetInfo.None (no crash), returns baseLockTime for asteroid TargetInfo, returns baseLockTime for station TargetInfo. (FR-017, FR-018)
- [ ] T023 [P] Write `Assets/Features/Targeting/Tests/TargetingMathTests.cs` — NUnit tests: FormatRange(1247) returns "1,247 m", FormatRange(523) returns "523 m", FormatRange(0) returns "0 m", IsInViewport returns true for center screen position with positive z, IsInViewport returns false for position behind camera (z < 0), IsInViewport returns false for position outside viewport bounds, ClampToScreenEdge returns clamped position and angle toward original for off-screen position. (FR-004, FR-005)

### Reducer & Math Implementation (Green Phase)

- [ ] T024 Create `Assets/Features/Targeting/Systems/TargetingReducer.cs` — pure static class: Reduce(TargetingState state, ITargetingAction action) returns TargetingState. Switch expression on action type handling all 8 actions per data-model.md. SelectTarget: set Selection, cancel active lock if TargetId changed. ClearSelection: reset Selection and LockAcquisition. BeginLock: set LockAcquisition to InProgress with TargetId + Duration. LockTick: advance ElapsedTime, auto-set Status=Completed when elapsed >= total. CompleteLock: convert acquisition to TargetLockData, append to LockedTargets, reset acquisition. CancelLock: reset LockAcquisition to None. UnlockTarget: filter LockedTargets removing matching TargetId. ClearAllLocks: reset all to empty/None sentinels. Unknown actions return state unchanged. Namespace: VoidHarvest.Features.Targeting.Systems.
- [ ] T025 [P] Create `Assets/Features/Targeting/Systems/LockTimeMath.cs` — pure static class: CalculateLockTime(float baseLockTime, TargetInfo target) returns float. V1 implementation: return baseLockTime. The TargetInfo parameter enables future factors (distance, size, sensors) without signature changes. Namespace: VoidHarvest.Features.Targeting.Systems.
- [ ] T026 [P] Create `Assets/Features/Targeting/Systems/TargetingMath.cs` — pure static class: CalculateScreenBounds(float3 worldPosition, float visualRadius, Camera camera) returns Rect — projects world-space sphere to screen-space rectangle. ClampToScreenEdge(Vector2 screenPos, Vector2 screenSize, float margin) returns (Vector2 position, float angle) — clamps position to viewport edges with margin, angle toward original. IsInViewport(Vector3 screenPos) returns bool — z > 0 and x/y within [0, Screen.width/height]. FormatRange(float distanceMeters) returns string — formatted with thousands separator and " m" suffix. Namespace: VoidHarvest.Features.Targeting.Systems.

### DI Registration

- [ ] T027 Modify `Assets/Core/SceneLifetimeScope.cs` — add [SerializeField] TargetingConfig field, register as VContainer instance. Add [SerializeField] TargetingAudioConfig and TargetingVFXConfig fields, register as instances. Assign assets in SceneLifetimeScope inspector in GameScene.

**Checkpoint**: Compile clean, foundational tests pass (T020-T023 green), GameState uses TargetingState, ITargetable interface available, TargetingReducer handles all 8 actions, math functions correct. CompositeReducer routes ITargetingAction.

---

## Phase 3: User Story 1 — Selection & Reticle (Priority: P1) 🎯 MVP

**Goal**: Left-click to select asteroids/stations with corner-bracket reticle, name/type above, range/mass below. Off-screen tracking indicator.

**Independent Test**: Fly near asteroids, left-click asteroid — reticle appears with corner brackets, name+type above, range+mass below. Click empty space — reticle disappears. Click station — reticle transfers. Rotate away — tracking triangle appears at screen edge.

### TargetableStation MonoBehaviour

- [ ] T028 [P] [US1] Create `Assets/Features/Targeting/Views/TargetableStation.cs` — sealed MonoBehaviour implementing ITargetable. [SerializeField] int StationId. On Awake or Start: look up StationData from WorldState.Stations by StationId. TargetId returns gameObject.GetInstanceID(). DisplayName returns StationData.Name. TypeLabel returns "Station". TargetType returns TargetType.Station. After creating the script, place the TargetableStation component on both station prefab GameObjects (SmallMiningRelay StationId=1, MediumRefineryHub StationId=2) alongside DockingPortComponent. Namespace: VoidHarvest.Features.Targeting.Views. (FR-001, FR-032)

### InputBridge Modification

- [ ] T029 [US1] Modify `Assets/Features/Input/Views/InputBridge.cs` — on Physics raycast hit path: check for ITargetable component (replaces hard-coded DockingPortComponent check for selection). If ITargetable found: construct TargetInfo via TargetInfo.From(targetable), dispatch SelectTargetAction with fields from TargetInfo. On ECS ray-sphere hit path (asteroids): construct TargetInfo via TargetInfo.FromAsteroid(entityIndex, displayName, oreTypeName) using AsteroidComponent + AsteroidOreComponent + OreDisplayNames lookup, dispatch SelectTargetAction. On empty space click: dispatch ClearSelectionAction. Preserve existing TargetSelectedEvent publishing for backward compatibility with radial menu.
- [ ] T030 [US1] Add `VoidHarvest.Features.Targeting` reference to `Assets/Features/Input/VoidHarvest.Features.Input.asmdef` — required for dispatching SelectTargetAction and ClearSelectionAction.

### Reticle UI (UI Toolkit)

- [ ] T031 [P] [US1] Create `Assets/Features/Targeting/Views/Targeting.uxml` — UI Toolkit layout containing: reticle-container (absolute position, holds 4 corner bracket VisualElements), target-name-label (above reticle), target-type-label (above reticle, below name), target-range-label (below reticle), target-mass-label (below reticle, below range), offscreen-indicator (rotatable triangle element), lock-progress-container (arc/ring overlay, initially hidden), target-cards-panel (absolute positioned, top-right area left of ship-info-panel). All initially hidden.
- [ ] T032 [P] [US1] Create `Assets/Features/Targeting/Views/Targeting.uss` — styles matching existing HUD aesthetic: `.reticle-corner { border-color: rgba(0, 200, 255, 0.8); }`, `.reticle-label { color: rgba(200, 230, 255, 0.9); font-size: 12px; }`, `.offscreen-indicator { width: 16px; height: 16px; background-color: rgba(0, 200, 255, 0.6); }`, `.target-card { ... }` styles. Use `.hud-panel` base class for card panel. Reticle corners use border-width on specific sides (top+left for top-left corner, etc.).

### ReticleView Controller

- [ ] T033 [US1] Create `Assets/Features/Targeting/Views/ReticleView.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore, Camera mainCamera, TargetingConfig config). In LateUpdate: read TargetingState.Selection from state. If HasSelection: find target world position (Physics raycast cache or ECS entity query by TargetId), call TargetingMath.CalculateScreenBounds to get screen rect, position 4 corner bracket elements at rect corners with config.ReticlePadding, set name/type labels from Selection.DisplayName and Selection.TypeLabel, compute range via Vector3.Distance and set range label via TargetingMath.FormatRange, show reticle container. If no selection: hide all. Handle min/max reticle size clamping per TargetingConfig. (FR-001 through FR-004, FR-007, FR-007a)

### Off-Screen Tracking Indicator

- [ ] T034 [US1] Create `Assets/Features/Targeting/Views/OffScreenIndicatorView.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore, Camera mainCamera, TargetingConfig config). In LateUpdate: if selection active, project target to screen space. If TargetingMath.IsInViewport returns false: hide reticle, show offscreen-indicator element, call TargetingMath.ClampToScreenEdge with config.OffScreenIndicatorMargin, position triangle at clamped position, rotate to returned angle. If in viewport: hide indicator, show reticle. (FR-005, FR-005a)

### HUD Integration

- [ ] T035 [US1] Modify `Assets/Features/HUD/Views/HUD.uxml` — remove `target-info-panel` element (replaced by reticle). Add `<ui:Instance template="Targeting.uxml" />` or a `targeting-overlay` container element for the targeting UXML to be loaded into. (FR-007a)
- [ ] T036 [US1] Modify `Assets/Features/HUD/Views/HUDView.cs` — remove target info panel wiring (labels for name, ore type, distance, mass that were reading from TargetSelectedEvent). The reticle now handles all target info display. Preserve any other HUD functionality.
- [ ] T037 [US1] Add `VoidHarvest.Features.Targeting` reference to `Assets/Features/HUD/VoidHarvest.Features.HUD.asmdef` — required for TargetingState reads and TargetInfo types.

### TargetingController (Orchestrator)

- [ ] T038 [US1] Create `Assets/Features/Targeting/Views/TargetingController.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore, IEventBus, Camera mainCamera). Responsible for: instantiating/managing ReticleView, OffScreenIndicatorView as child components, maintaining a cache of world positions for selected/locked targets (query ECS entities by TargetId for asteroids, find GameObjects for stations). Expose `GetTargetWorldPosition(int targetId)` method returning cached Vector3 — consumed by ReticleView, OffScreenIndicatorView, and TargetCardView for position lookups. Detect target destruction (asteroid depletion) and dispatch UnlockTargetAction + publish TargetLostEvent. Register in VContainer scope (SceneLifetimeScope). Wire Targeting.uxml into scene UI document. (FR-001, FR-016, FR-025)

### Tests (Red→Green verification)

- [ ] T039 [US1] Run compilation check via Unity MCP `read_console` — verify zero errors. Run `TargetInfoTests` and `TargetingReducerTests` via Unity MCP `run_tests` — verify selection-related tests pass. Verify in Play mode: left-click asteroid shows reticle, click empty space clears, click station transfers selection.

**Checkpoint**: US1 fully functional. Player can select any targetable object, see corner-bracket reticle with name/type above and range/mass below. Off-screen tracking works. Old target info panel removed.

---

## Phase 4: User Story 2 — Timed Target Lock (Priority: P2)

**Goal**: Radial menu "Lock Target" initiates timed lock acquisition with visual progress and audio feedback.

**Independent Test**: Select target, right-click for radial menu, click "Lock Target" — progress arc fills around reticle with rising audio, corners pulse. After 1.5s, lock confirmed with flash + audio. Cancel mid-lock by clicking elsewhere — failure sound, progress resets.

### Radial Menu Integration

- [ ] T040 [US2] Modify `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs` — add "Lock Target" segment at top-left position. Visible for all target types (asteroids and stations) when a target is selected. On activation: read ShipArchetypeConfig.BaseLockTime, call LockTimeMath.CalculateLockTime(baseLockTime, currentTargetInfo), check LockedTargets.Length < MaxTargetLocks (dispatch LockSlotsFullEvent if full), check target not already locked (FR-015 silent ignore), check target within MaxLockRange (dispatch LockFailedEvent with OutOfRange if beyond), dispatch BeginLockAction(targetId, calculatedDuration). (FR-008, FR-009, FR-013, FR-015, FR-022)

### Lock Progress View

- [ ] T041 [P] [US2] Create `Assets/Features/Targeting/Views/LockProgressView.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore, TargetingConfig config). In LateUpdate: read TargetingState.LockAcquisition. If IsActive: show lock-progress-container as an overlay on top of the existing reticle (the reticle, name, and range labels MUST remain visible underneath — FR-033), render progress arc/ring around reticle position using LockAcquisition.Progress [0..1], pulse reticle corner opacity/scale using sin wave at TargetingVFXConfig.ReticlePulseSpeed. If not active: hide progress indicators (do NOT hide reticle). On lock completion (status transitions to Completed): trigger brief flash effect for TargetingVFXConfig.LockFlashDuration. (FR-010, FR-012, FR-014, FR-033)

### Lock Tick System

- [ ] T042 [US2] Add lock acquisition tick logic to `Assets/Features/Targeting/Views/TargetingController.cs` — in Update: if LockAcquisition.IsActive, dispatch LockTickAction(Time.deltaTime). After tick, check if LockAcquisition.Status == Completed: dispatch CompleteLockAction, publish TargetLockedEvent. Check if target moved beyond MaxLockRange during acquisition: dispatch CancelLockAction, publish LockFailedEvent(OutOfRange). (FR-009, FR-013, FR-019)

### Lock Cancellation

- [ ] T043 [US2] Add lock cancellation logic to `Assets/Features/Targeting/Views/TargetingController.cs` — when TargetingState.Selection changes while LockAcquisition.IsActive (monitored via state subscription): dispatch CancelLockAction, publish LockFailedEvent(Deselected). When locked/acquiring target is destroyed (asteroid depleted): dispatch CancelLockAction, publish LockFailedEvent(TargetDestroyed). (FR-013, FR-016)

### Audio Feedback

- [ ] T044 [US2] Create `Assets/Features/Targeting/Views/TargetingAudioController.cs` — sealed MonoBehaviour with [Inject] Construct(IEventBus, TargetingAudioConfig config). Subscribe to events: TargetLockedEvent → play LockConfirmedClip, LockFailedEvent → play LockFailedClip, LockSlotsFullEvent → play LockSlotsFullClip, TargetLostEvent → play TargetLostClip. For lock acquisition audio: monitor TargetingState.LockAcquisition.IsActive — when active, play LockAcquiringClip (rising tone, looping or pitch-shifting based on Progress). Stop on cancel/complete. (FR-011, FR-012, FR-014)

### Tests (Red→Green verification)

- [ ] T045 [US2] Run compilation check via Unity MCP `read_console` — verify zero errors. Run full `TargetingReducerTests` via Unity MCP `run_tests` — verify lock-related tests pass (BeginLock, LockTick, CompleteLock, CancelLock). Verify in Play mode: radial menu shows Lock Target alongside all existing segments (Approach, Orbit, Mine, KeepAtRange, Dock — FR-034), lock progress animates while reticle+labels remain visible underneath (FR-033), lock completes after configured time, cancel on deselect works with near-instantaneous response (SC-005).

**Checkpoint**: US1+US2 functional. Player can select target, initiate timed lock via radial menu, see/hear progress, and confirm lock. Cancellation works correctly.

---

## Phase 5: User Story 3 — Multi-Target Management (Priority: P3)

**Goal**: Lock multiple targets (up to per-ship max), basic HUD target cards with name/range (no viewport yet), dismiss individual locks, auto-clear on dock.

**Independent Test**: Lock 3 targets sequentially — 3 cards appear left of ship info, newest rightmost. Attempt 4th lock — "slots full" feedback. Dismiss middle card — remaining cards reflow. Dock at station — all cards clear.

### Target Card Panel (basic — name + range, no viewport)

- [ ] T046 [P] [US3] Create `Assets/Features/Targeting/Views/TargetCardPanelView.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore, IEventBus). Manages a horizontal container of TargetCardView instances positioned immediately left of ship-info-panel. In LateUpdate or on state change: read TargetingState.LockedTargets, create/destroy card views to match LockedTargets count. New cards appear on right side, existing shift left. Cards reflow when a target is removed. (FR-026, FR-029, FR-030)
- [ ] T047 [P] [US3] Create `Assets/Features/Targeting/Views/TargetCardView.cs` — sealed MonoBehaviour managing a single card VisualElement. Displays: card border (thin sci-fi style per `.hud-panel`), viewport-placeholder (solid dark rectangle, to be replaced with RenderTexture in US4), target name/type label, continuously-updating range label. Dismiss button ("X") dispatches UnlockTargetAction and publishes TargetUnlockedEvent. Card body click (outside dismiss) dispatches SelectTargetAction for this locked target (FR-023a). (FR-027, FR-023, FR-023a, FR-031)

### Card Panel UXML & USS

- [ ] T048 [P] [US3] Add target card panel styles to `Assets/Features/Targeting/Views/Targeting.uss` — `.target-card-panel` MUST be positioned dynamically relative to the ship-info-panel (not a hardcoded pixel offset): place both panels in a shared flex container with `flex-direction: row-reverse` so the card panel is always immediately left of ship info regardless of ship-info-panel width. `.target-card-panel { flex-direction: row-reverse; margin-right: 8px; }`. `.target-card { width: 140px; height: 180px; margin-left: 8px; }`, `.target-card-viewport { width: 100%; height: 100px; background-color: rgba(8, 12, 24, 0.9); }`, `.target-card-dismiss { position: absolute; top: 2px; right: 2px; width: 16px; height: 16px; }`, `.target-card-info { padding: 4px; }`. Match existing `.hud-panel` border style. (FR-026)

### Dock/Undock Integration

- [ ] T049 [US3] Verify `Assets/Core/RootLifetimeScope.cs` — confirm ClearAllLocksAction dispatch added in T019 fires correctly on CompleteDockingAction and CompleteUndockingAction. Test by docking with active locks — all cards should clear, no stale cards or state remaining (SC-007). Publish AllLocksClearedEvent from TargetingController when ClearAllLocksAction dispatches. (FR-024, SC-007)

### Target Destruction Handling

- [ ] T050 [US3] Add asteroid depletion detection to `Assets/Features/Targeting/Views/TargetingController.cs` — in Update: for each locked target with TargetType=Asteroid, query ECS to check if entity still exists and RemainingMass > 0. If depleted: dispatch UnlockTargetAction, publish TargetLostEvent. For stations: no destruction in current spec. (FR-025)

### Slots Full Check

- [ ] T051 [US3] Verify slots-full logic in RadialMenuController (T040) — when LockedTargets.Length >= ShipArchetypeConfig.MaxTargetLocks, the "Lock Target" segment activation should dispatch LockSlotsFullEvent and abort before starting BeginLockAction. Verify audio cue plays via TargetingAudioController. (FR-022)

### Targeting Inactive While Docked

- [ ] T052 [US3] Add docked-state guard to `Assets/Features/Targeting/Views/TargetingController.cs` and `Assets/Features/Input/Views/InputBridge.cs` — when DockingState.Phase != None (player is docked), suppress all targeting: no selection on click, no lock initiation, hide reticle and cards. Re-enable on undock. (FR-035)

### Tests (Red→Green verification)

- [ ] T053 [US3] Write `Assets/Features/Targeting/Tests/SelectionIntegrationTests.cs` — NUnit tests: full lifecycle test (select→lock→lock→lock→verify 3 LockedTargets with each card displaying correct name/range), unlock middle target removes it and maintains order of remaining, ClearAllLocksAction resets everything, duplicate lock attempt on already-locked target returns unchanged, slots full prevents new lock (LockedTargets.Length unchanged). (FR-015, FR-020, FR-021, FR-022, FR-023, FR-024, SC-003)
- [ ] T054 [US3] Run compilation check + full test suite via Unity MCP `run_tests` — verify all targeting tests pass. Verify in Play mode: multi-lock works, cards appear/reflow, dock clears all, dismiss removes individual card.

**Checkpoint**: US1+US2+US3 functional. Full select→lock→multi-target management loop. Cards show name/range (no viewport yet). Dock clears locks.

---

## Phase 6: User Story 4 — Target Cards with Live Viewports (Priority: P4)

**Goal**: Each target card shows a live RenderTexture viewport rendering only the isolated targeted object. Premium visual quality.

**Independent Test**: Lock a target — card viewport shows ONLY the targeted object (no background asteroids/stations). Lock 3 targets — each viewport shows its respective target. Rotate ship — viewports update live. Performance: maintain 60 FPS.

### Preview Infrastructure

- [ ] T055 [P] [US4] Create `Assets/Features/Targeting/Views/TargetPreviewManager.cs` — sealed MonoBehaviour with [Inject] Construct(TargetingConfig config). Manages up to MaxTargetLocks preview slots. Each slot: (1) clone of the target's visual mesh+material positioned at PreviewStageOffset + slot index offset, (2) a dedicated Camera on "TargetPreview" culling layer rendering to a RenderTexture at ViewportRenderSize×ViewportRenderSize, (3) the camera framed to fill the target in viewport using config.ViewportFOV. For asteroids: clone MeshFilter.sharedMesh + MeshRenderer.sharedMaterial from the ECS-rendered entity's visual representation. For stations: clone the station GameObject's visual hierarchy. All clones placed on "TargetPreview" layer. Main camera's culling mask must exclude "TargetPreview" layer. Expose GetRenderTexture(int targetId) for TargetCardView. Clean up clones + RenderTextures on target unlock. (FR-028)
- [ ] T056 [US4] Configure main camera culling mask to exclude "TargetPreview" layer — modify the main camera's culling mask in scene or via script at initialization. Ensure "TargetPreview" layer objects are invisible to main camera but visible to preview cameras.

### Card Viewport Integration

- [ ] T057 [US4] Modify `Assets/Features/Targeting/Views/TargetCardView.cs` — replace viewport-placeholder with actual RenderTexture display. On card creation: request RenderTexture from TargetPreviewManager.GetRenderTexture(targetId). Assign to a UI Toolkit Image element or VisualElement background. On card destruction: notify TargetPreviewManager to release the preview slot. Update live — RenderTexture auto-updates as preview camera renders each frame. (FR-027, FR-028)

### Preview Update Loop

- [ ] T058 [US4] Add preview camera update to `Assets/Features/Targeting/Views/TargetPreviewManager.cs` — in LateUpdate: for each active preview slot, update clone position/rotation to match the live target's current transform (so the preview reflects real-time state). For asteroids: query ECS LocalTransform. For stations: read GameObject transform. Camera looks at the clone from a fixed offset, framing to fill. Handle target destruction by cleaning up the preview slot immediately. (FR-028)

### Performance Verification

- [ ] T059 [US4] Profile target card viewports under Unity Profiler — lock 3 targets, verify frame time contribution from preview cameras + RenderTextures is < 1ms. Verify no GC allocations from preview system in steady state. If performance exceeds budget: reduce ViewportRenderSize, reduce preview camera update frequency (every 2nd frame), or use Camera.Render() manual rendering instead of continuous. (SC-004)

### Tests

- [ ] T060 [US4] Run compilation check + full test suite via Unity MCP `run_tests` — verify all tests pass. Verify in Play mode: each card shows isolated live viewport of its target, viewports update as ship moves, 3 simultaneous viewports at 60 FPS, no visual artifacts.

**Checkpoint**: US1-US4 functional. Full targeting system with premium live viewports. Cards show isolated targets with live rendering.

---

## Phase 7: User Story 5 — Lock Time Computation (Priority: P5)

**Goal**: Per-ship configurable lock time, extensible calculation method, timing accuracy ±0.1s.

**Independent Test**: Set StarterMiningBarge BaseLockTime=1.5s — lock takes ~1.5s. Set HeavyMiningBarge BaseLockTime=3.0s — lock takes ~3.0s. Both accept TargetInfo for future extensibility.

### Lock Time Configuration Verification

- [ ] T061 [US5] Verify lock time configuration end-to-end — in Play mode, select target with StarterMiningBarge (BaseLockTime=1.5), initiate lock, time the acquisition. Verify ±0.1s accuracy. If the project supports ship swapping in inspector, test with HeavyMiningBarge (BaseLockTime=2.0) and verify different duration. (SC-002, FR-017, FR-018, FR-019)

### Lock Time Extensibility Test

- [ ] T062 [US5] Verify LockTimeMathTests pass confirming extensibility — CalculateLockTime accepts TargetInfo parameter, future modifications can use target.TargetType, target.DisplayName, or extend TargetInfo with distance/size fields. Run LockTimeMathTests via Unity MCP to confirm. (FR-018, SC-008)

**Checkpoint**: Lock time computation verified. Per-ship config works. Extensibility path confirmed.

---

## Phase 8: User Story 6 — Player Documentation (Priority: P6)

**Goal**: Complete player-facing documentation for the targeting and locking system.

**Independent Test**: Read HOWTOPLAY.md — find complete "Targeting & Locking" section. A new player can follow instructions to select, lock, and manage targets.

### Documentation

- [ ] T063 [US6] Update `HOWTOPLAY.md` at project root — add "Targeting & Locking" section covering: how to select targets (left-click), reticle display (name/type above, range/mass below), off-screen tracking indicator, how to initiate lock (radial menu → Lock Target), lock progress feedback (visual + audio), lock cancellation conditions (deselect, out-of-range during acquisition, target destroyed), multi-target management (up to 3 simultaneous), target card HUD (viewport, name, range, dismiss), card click to re-select, auto-clear on docking. Include default values (1.5s lock time, 3 max locks, 5000m lock range). No code references or type names. (FR-036, SC-009)
- [ ] T064 [P] [US6] Add changelog entry for Spec 007 in project changelog or HOWTOPLAY.md — document: targeting system, selection reticle, timed locking, multi-target HUD cards with live viewports, off-screen tracking, ITargetable extensibility, radial menu "Lock Target" segment. (FR-037)

**Checkpoint**: Documentation complete. Player instructions cover all new mechanics.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Full regression, performance verification, quickstart validation, code quality.

- [ ] T065 Run full EditMode test suite via Unity MCP `run_tests` — verify zero regressions across all assemblies: ship controls, mining, camera, docking, station services, asteroid generation, HUD, and new targeting tests. All tests must pass. Enumerate all pure static classes in Targeting assembly (TargetingReducer, LockTimeMath, TargetingMath) and confirm each has a corresponding test file with comprehensive coverage (SC-006).
- [ ] T066 [P] Profile full targeting system under Unity Profiler — verify < 2ms frame spike contribution from targeting system with 3 locked targets active + reticle + off-screen indicator. Verify zero GC allocations in targeting hot loops (LateUpdate, Update). Measure selection input latency (click to reticle visible) — must be < 0.1s (SC-001). (SC-001, SC-004)
- [ ] T067 [P] Code review and cleanup — verify all new types follow naming conventions (State/Data/Reducer/Config/Action suffixes), XML doc comments on public API methods (ITargetable, TargetInfo, TargetingReducer, LockTimeMath, TargetingMath), no unused imports, no dead code, all [Inject] patterns consistent with existing codebase.
- [ ] T068 Execute quickstart.md validation — run through all 8 test scenarios: selection & reticle, off-screen tracking, timed locking, lock cancellation, multi-target management, target cards, dock/undock clears locks, different lock times. Capture screenshot of target cards alongside existing HUD and verify visual consistency with resource panel, ship info, and hotbar aesthetic (SC-010).
- [ ] T069 Verify TargetableStation MonoBehaviour placed on both station prefabs (SmallMiningRelay StationId=1, MediumRefineryHub StationId=2). Verify ITargetable detected on Physics raycast in InputBridge. (This should already be done in T028 — this is a final confirmation.)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **BLOCKS all user stories**
- **US1 Selection & Reticle (Phase 3)**: Depends on Phase 2
- **US2 Timed Locking (Phase 4)**: Depends on Phase 3 (reticle must exist for progress overlay)
- **US3 Multi-Target Mgmt (Phase 5)**: Depends on Phase 4 (lock completion creates LockedTargets entries)
- **US4 Target Card Viewports (Phase 6)**: Depends on Phase 5 (card panel must exist to add viewports)
- **US5 Lock Time Computation (Phase 7)**: Depends on Phase 4 (lock mechanism must work)
- **US6 Documentation (Phase 8)**: Depends on Phases 3–6 (all features documented)
- **Polish (Phase 9)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: Independent after Phase 2 — core selection & display
- **US2 (P2)**: Depends on US1 — lock progress overlays the reticle
- **US3 (P3)**: Depends on US2 — multi-target requires lock completion
- **US4 (P4)**: Depends on US3 — viewport rendering extends existing cards
- **US5 (P5)**: Depends on US2 — verifies lock timing accuracy
- **US6 (P6)**: Depends on US1–US4 — documents all functionality

### Within Each User Story (TDD Flow)

1. Tests written FIRST → must FAIL (Red)
2. Data types / MonoBehaviour stubs created
3. Reducer / math / logic implemented → tests pass (Green)
4. UI created (UXML/USS layout then controller logic)
5. Wired into scene + VContainer
6. Compilation check + test verification via Unity MCP

### Parallel Opportunities

**Phase 1 — Setup (T002–T007)**: All 6 tasks in parallel (different files/assets)
**Phase 2 — Core interfaces (T008–T009)**: Both files in parallel
**Phase 2 — Data types (T010–T017)**: All 8 files in parallel
**Phase 2 — Tests (T020–T023)**: All 4 test files in parallel
**Phase 2 — Implementation (T025–T026)**: LockTimeMath + TargetingMath in parallel
**US1 — Views (T031–T032)**: UXML + USS in parallel
**US1 — TargetableStation (T028)**: Parallel with UXML/USS creation
**US3 — Card views (T046–T048)**: PanelView + CardView + USS in parallel
**US4 — TargetPreviewManager (T055)**: Parallel with camera config (T056)

---

## Parallel Example: Phase 2 Data Types

```text
# Launch all data type creations together (8 files, no deps):
T010: LockAcquisitionStatus.cs
T011: LockFailReason.cs
T012: SelectionData.cs
T013: LockAcquisitionData.cs
T014: TargetLockData.cs
T015: TargetingState.cs
T016: TargetingActions.cs
T017: TargetingEvents.cs
```

## Parallel Example: Phase 2 Tests

```text
# Launch all test files together (4 files, no deps):
T020: TargetInfoTests.cs
T021: TargetingReducerTests.cs
T022: LockTimeMathTests.cs
T023: TargetingMathTests.cs
```

## Parallel Example: US1 Views

```text
# Launch UI creation together (3 files, no deps):
T028: TargetableStation.cs
T031: Targeting.uxml
T032: Targeting.uss
```

---

## Implementation Strategy

### MVP First (Phase 1 + 2 + US1)

1. Complete Phase 1: Setup (7 tasks)
2. Complete Phase 2: Foundational (20 tasks)
3. Complete Phase 3: US1 Selection & Reticle (12 tasks)
4. **STOP and VALIDATE**: Player can select targets with full reticle display
5. This is the minimum viable targeting experience

### Incremental Delivery

1. Phase 1+2 → Foundation ready (compile clean, core tests pass)
2. +US1 → Selection & reticle working (MVP — "I can see what I'm targeting")
3. +US2 → Timed locking working (core mechanic — deliberate pacing)
4. +US3 → Multi-target cards working (tactical depth — track multiple targets)
5. +US4 → Live viewports working (premium polish — isolated target rendering)
6. +US5 → Lock time config verified (data-driven — per-ship tuning)
7. +US6 → Documentation complete (delivery gate)
8. +Polish → Regression check, profiling, code quality

---

## Notes

- TDD is mandatory per constitution — tests MUST be written and FAIL before implementation
- [P] tasks = different files, no dependencies
- [Story] labels map tasks to user stories for traceability
- ITargetable interface in Core/Extensions — zero dependency, any assembly can implement
- TargetInfo readonly struct — common contract for ECS entities and MonoBehaviours
- All targeting state flows through pure TargetingReducer — no mutable MonoBehaviour state
- RenderTextures are 128×128, max 3 concurrent — budget ~1ms GPU for all preview cameras
- "TargetPreview" layer isolates preview clones from main camera rendering
- UI Toolkit only — no Canvas/UGUI (matches existing HUD)
- Range only checked during lock acquisition — locks persist indefinitely once established
- LOS not required for locking — supports future scan-list targeting
- ClearAllLocksAction dispatched on both dock and undock
- Compile-check via Unity MCP `read_console` after every script creation/modification
- Console must be clean before proceeding (constitution gate)
- Run relevant tests via Unity MCP `run_tests` at each checkpoint
- Commit after each task or logical group using conventional commits
