# Tasks: Station Docking & Interaction Framework

**Input**: Design documents from `/specs/004-station-docking/`
**Prerequisites**: plan.md, spec.md, data-model.md, research.md, quickstart.md

**Tests**: TDD is mandatory per project constitution. Test tasks are included for all user stories with pure logic.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Docking module structure, all shared type definitions, and structural modifications to existing types. No logic implementation.

- [ ] T001 Create assembly definition `Assets/Features/Docking/VoidHarvest.Features.Docking.asmdef` with references: VoidHarvest.Core.Extensions, VoidHarvest.Core.State, VoidHarvest.Core.EventBus, VoidHarvest.Features.Ship, VoidHarvest.Features.Base, Unity.Entities, Unity.Entities.Hybrid, Unity.Mathematics, Unity.Burst, Unity.Collections, Unity.Transforms, VContainer, UniTask; allowUnsafeCode=true
- [ ] T002 Create test assembly definition `Assets/Features/Docking/Tests/VoidHarvest.Features.Docking.Tests.asmdef` with references: VoidHarvest.Features.Docking, VoidHarvest.Core.State, VoidHarvest.Core.EventBus, VoidHarvest.Core.Extensions, Unity.Mathematics, UnityEngine.TestRunner, UnityEditor.TestRunner; includePlatforms=[Editor], overrideReferences=true, precompiledReferences=[nunit.framework.dll]
- [ ] T003 [P] Create DockingPhase enum (None=0, Approaching=1, Snapping=2, Docked=3, Undocking=4) and DockingState sealed record (DockingPhase Phase, Option\<int\> TargetStationId, Option\<float3\> DockingPortPosition, Option\<quaternion\> DockingPortRotation) with static Empty and derived properties IsDocked/IsInProgress in `Assets/Core/State/DockingState.cs`
- [ ] T004 [P] Create IDockingAction marker interface extending IGameAction in `Assets/Core/State/IDockingAction.cs`
- [ ] T005 [P] Add Docking=7 and Docked=8 values to ShipFlightMode enum in `Assets/Core/State/ShipFlightMode.cs`
- [ ] T006 [P] Create TargetType enum (None=0, Asteroid=1, Station=2) in `Assets/Features/Input/Data/TargetType.cs`
- [ ] T007 [P] Create docking event structs (DockingStartedEvent{int StationId}, DockingCompletedEvent{int StationId}, UndockingStartedEvent{int StationId}, UndockCompletedEvent{int StationId}, DockingCancelledEvent) as readonly structs in `Assets/Core/EventBus/Events/DockingEvents.cs`
- [ ] T008 Create concrete IDockingAction records: BeginDockingAction(int StationId, float3 PortPosition, quaternion PortRotation), CompleteDockingAction(int StationId), CancelDockingAction, BeginUndockingAction, CompleteUndockingAction in `Assets/Features/Docking/Data/DockingActions.cs`
- [ ] T009 Create IFleetAction records: DockAtStationAction(int StationId), UndockFromStationAction in a new file `Assets/Core/State/FleetActions.cs` (IFleetAction already exists at `Assets/Core/State/IFleetAction.cs`)
- [ ] T010a [P] Create DockingPortComponent : MonoBehaviour (float3 PortPosition, quaternion PortRotation, float DockingRange=500f, float SnapRange=30f, int StationId) in `Assets/Features/Docking/Data/DockingPortComponent.cs` — this is a regular MonoBehaviour on station prefab GameObjects (NOT an ECS component; stations are not entities). InputBridge reads it when targeting and copies data into DockingStateComponent on the ship entity.
- [ ] T010b [P] Create DockingStateComponent : IComponentData (DockingPhase Phase, float3 TargetPortPosition, quaternion TargetPortRotation, int TargetStationId, float SnapTimer, float3 StartPosition, quaternion StartRotation) and DockingEventFlags : IComponentData (bool DockCompleted, int DockStationId, bool UndockCompleted) singleton for Burst↔managed bridging, with CONSTITUTION DEVIATION: ECS mutable shell comment in `Assets/Features/Docking/Data/DockingComponents.cs`
- [ ] T011 [P] Create DockingConfig : ScriptableObject (float MaxDockingRange=500f, float SnapRange=30f, float SnapDuration=1.5f, float UndockClearanceDistance=100f, float UndockDuration=2f) in `Assets/Features/Docking/Data/DockingConfig.cs`
- [ ] T012 Add DockingState Docking field as 9th positional parameter to GameLoopState sealed record in `Assets/Core/State/GameLoopState.cs` and update CreateDefaultGameState() in `Assets/Core/RootLifetimeScope.cs` to pass DockingState.Empty
- [ ] T013 Extend RadialMenuRequestedEvent: add TargetType field to readonly struct constructor in `Assets/Core/EventBus/Events/RadialMenuRequestedEvent.cs` and update all publish sites in InputBridge
- [ ] T014 MCP verification: refresh Unity, check compilation succeeds, verify console has no errors

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core pure logic (TDD), reducer wiring, and config registration. MUST complete before any user story.

**CRITICAL**: No user story work can begin until this phase is complete.

### DockingReducer (TDD)

> **Write tests FIRST, ensure they FAIL before implementation**

- [ ] T015 [P] Write DockingReducer unit tests (Red): test None+BeginDockingAction→Approaching, Approaching+CompleteDockingAction→Docked, Approaching+CancelDockingAction→None, Docked+BeginUndockingAction→Undocking, Undocking+CompleteUndockingAction→None; test invalid transitions return unchanged state; test field values propagation in `Assets/Features/Docking/Tests/DockingReducerTests.cs`
- [ ] T016 Implement DockingReducer.Reduce(DockingState, IDockingAction) → DockingState as pure static function with switch expression pattern matching on action type (Green) in `Assets/Features/Docking/Systems/DockingReducer.cs`

### DockingMath (TDD)

> **Write tests FIRST, ensure they FAIL before implementation**

- [ ] T017 [P] Write DockingMath unit tests (Red): test IsWithinDockingRange (true/false boundary), IsWithinSnapRange, ComputeSnapProgress (0→0, 0.5→smoothstep, 1→1), InterpolateSnapPose (start/mid/end), ComputeApproachTarget, ComputeClearancePosition in `Assets/Features/Docking/Tests/DockingMathTests.cs`
- [ ] T018 Implement DockingMath pure static functions (Green): ComputeApproachTarget(float3 shipPos, float3 portPos, float approachOffset), ComputeSnapProgress(float elapsed, float duration), InterpolateSnapPose(float3 startPos, quaternion startRot, float3 targetPos, quaternion targetRot, float t), ComputeClearancePosition(float3 portPos, float3 portForward, float clearanceDistance), IsWithinDockingRange(float3 shipPos, float3 portPos, float maxRange), IsWithinSnapRange(float3 shipPos, float3 portPos, float snapRange) in `Assets/Features/Docking/Systems/DockingMath.cs`

### Wiring & Registration

- [ ] T019 Wire IDockingAction branch into CompositeReducer switch expression: `IDockingAction a => state with { Loop = state.Loop with { Docking = DockingReducer.Reduce(state.Loop.Docking, a) } }` in `Assets/Core/RootLifetimeScope.cs`
- [ ] T020 Implement FleetReducer: DockAtStationAction → return state with DockedAtStation=Some(action.StationId); UndockFromStationAction → return state with DockedAtStation=default (None); using switch expression in `Assets/Core/State/GameStateReducer.cs`
- [ ] T021 Create DockingConfig ScriptableObject asset instance via MCP (`manage_asset action=create`) and add serialized field + registration in `Assets/Core/SceneLifetimeScope.cs`
- [ ] T022 MCP verification: run EditMode tests for DockingReducer and DockingMath, verify all pass, console clean

**Checkpoint**: Core docking logic proven via TDD. All reducers wired. User story implementation can begin.

---

## Phase 3: User Story 1 — Context-Sensitive Station Radial Menu (P1) MVP

**Goal**: Radial menu shows station-appropriate options (Approach/KeepAtRange/Orbit/Dock) when targeting a station while undocked, Undock only when docked, and unchanged mining options for asteroids.

**Independent Test**: Target a station → radial shows station options. Target an asteroid → radial shows mining options. No regressions.

### Implementation for User Story 1

- [ ] T023 [P] [US1] Add `segment-dock` and `segment-undock` button elements to `Assets/Features/HUD/Views/RadialMenu/RadialMenu.uxml` following existing segment-approach/segment-orbit/segment-mine/segment-keep-at-range pattern
- [ ] T024 [P] [US1] Add USS styling for segment-dock and segment-undock buttons in `Assets/Features/HUD/Views/RadialMenu/RadialMenu.uss` (or inline styles in UXML) matching existing segment visual style
- [ ] T025 [US1] Modify InputBridge: add `TargetType _selectedTargetType` field; set to Station when TryRaycastSelectable hits an object with DockingPortComponent or on Selectable layer, Asteroid when TryRaycastAsteroid hits, None on target clear; expose `SelectedTargetType` public property in `Assets/Features/Input/Views/InputBridge.cs`
- [ ] T026 [US1] Update RadialMenuRequestedEvent publishing in InputBridge to pass TargetType alongside TargetId when publishing via EventBus in `Assets/Features/Input/Views/InputBridge.cs`
- [ ] T027 [US1] Modify RadialMenuController.Open(): read TargetType from RadialMenuRequestedEvent and query DockingState.IsDocked from IStateStore; implement show/hide logic — Station+Undocked: show segment-approach/segment-keep-at-range/segment-orbit/segment-dock, hide segment-mine/segment-undock; Station+Docked: show segment-undock only, hide all others; Asteroid: show segment-approach/segment-orbit/segment-mine/segment-keep-at-range, hide segment-dock/segment-undock (unchanged behavior) in `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs`
- [ ] T028 [US1] Handle Dock segment click in RadialMenuController: call `_inputBridge.SetRadialChoice(4, 0f)` (RadialMenuAction.Dock=4), dispatch BeginDockingAction via IStateStore with station port data, publish DockingStartedEvent via EventBus in `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs`
- [ ] T029 [US1] Handle Undock segment click in RadialMenuController: dispatch BeginUndockingAction via IStateStore, publish UndockingStartedEvent via EventBus in `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs`
- [ ] T030 [US1] Add VoidHarvest.Features.Docking reference to `Assets/Features/HUD/VoidHarvest.Features.HUD.asmdef` and VoidHarvest.Features.Docking reference to `Assets/Features/Input/VoidHarvest.Features.Input.asmdef`
- [ ] T031 [US1] MCP: Ensure Selectable layer exists (manage_editor action=add_layer); add BoxCollider to SmallMiningRelay and MediumRefineryHub station prefabs; set both prefabs to Selectable layer; add empty child GameObject "DockingPort" with DockingPortComponent MonoBehaviour — SmallMiningRelay: position near connector module (per research R-007), MediumRefineryHub: position near hangar module (per research R-007)
- [ ] T032 [US1] MCP verification: compile check, verify new UXML segments exist, verify prefab hierarchy shows DockingPort child, run all HUD and Input tests

**Checkpoint US1**: Target station → radial shows Approach/KeepAtRange/Orbit/Dock. Target asteroid → Mine/Approach/Orbit/KeepAtRange (unchanged). FR-001 to FR-004, SC-002 satisfied.

---

## Phase 4: User Story 2 — Automatic Docking (P1)

**Goal**: Ship autopilot approach → align → magnetic snap → docked state with physics suspended. Manual thrust cancels docking.

**Independent Test**: Select Dock from radial → auto-approach → snap into docked state. Manual thrust during approach → cancel. Ship holds at docking port with no drift.

### Implementation for User Story 2

- [ ] T033 [P] [US2] Extend ShipPhysicsMath.DetermineFlightMode(): when current mode is Docked return Docked (locked); when Docking with manual input return ManualThrust (cancel docking); when Docking with no manual input return Docking (stay) in `Assets/Features/Ship/Systems/ShipPhysicsMath.cs`
- [ ] T034 [P] [US2] Extend ShipPhysicsSystem.OnUpdate: when ShipFlightMode==Docked zero out velocity and angular velocity and skip all force application; when Docking skip manual thrust application (handled by DockingSystem) in `Assets/Features/Ship/Systems/ShipPhysicsSystem.cs`
- [ ] T035 [US2] Create DockingSystem : ISystem with [BurstCompile], [UpdateInGroup(typeof(SimulationSystemGroup))], [UpdateBefore(typeof(ShipPhysicsSystem))]; implement state machine: Approaching phase sets ShipFlightModeComponent.Mode=Docking, sets PilotCommandComponent.AlignPoint to port position, and checks IsWithinSnapRange→transition to Snapping; Snapping phase keeps Mode=Docking, interpolates position/rotation via DockingMath.InterpolateSnapPose over SnapDuration→transition to Docked; Docked phase locks position/rotation to port and sets ShipFlightModeComponent.Mode=Docked; Undocking phase lerps along clearance vector→on complete sets Mode=Idle and removes DockingStateComponent in `Assets/Features/Docking/Systems/DockingSystem.cs`
- [ ] T036 [US2] Integrate docking initiation in InputBridge: when Dock radial choice received, get DockingPortComponent data from targeted station GameObject, add DockingStateComponent to ship entity with target port data, set _alignPoint to port position; when manual thrust detected during Docking flight mode, remove DockingStateComponent from ship entity, dispatch CancelDockingAction, publish DockingCancelledEvent in `Assets/Features/Input/Views/InputBridge.cs`
- [ ] T037 [US2] Implement Burst↔managed event bridging: DockingSystem (Burst) writes completion flags to DockingEventFlags singleton component (dock-completed with StationId, or undock-completed). Create companion DockingEventBridgeSystem : ISystem (managed, non-Burst, [UpdateAfter(typeof(DockingSystem))]) that reads DockingEventFlags each frame — when dock-completed flag is set, dispatches CompleteDockingAction + DockAtStationAction via IStateStore and publishes DockingCompletedEvent via IEventBus, then clears flag; when undock-completed flag is set, dispatches CompleteUndockingAction + UndockFromStationAction, publishes UndockCompletedEvent, clears flag. This preserves zero-GC in the Burst DockingSystem hot path. Files: `Assets/Features/Docking/Systems/DockingSystem.cs` and `Assets/Features/Docking/Systems/DockingEventBridgeSystem.cs`
- [ ] T038 [US2] Write DockingSystem integration tests (PlayMode): test approach→snap→docked full sequence, test cancel via manual thrust mid-approach returns to flight within 1 second (SC-005), test target loss during approach triggers cancel, test initiation beyond 500m triggers approach-first then dock-when-in-range (spec edge case >2000m / US2.5) in `Assets/Features/Docking/Tests/DockingSystemTests.cs`
- [ ] T039 [US2] MCP verification: compile check, run all EditMode + PlayMode tests, verify console clean

**Checkpoint US2**: Select Dock → auto-approach → snap → docked state. Manual thrust cancels. Target loss cancels. FR-005 to FR-008, FR-013, SC-001, SC-003, SC-005 satisfied.

---

## Phase 5: User Story 3 — Station Services Menu Skeleton (P2)

**Goal**: Canvas panel auto-opens on dock completion showing station name + preset type with placeholder tabs (Refinery/Market/Repair/Cargo with "Coming Soon"). Auto-closes on undock.

**Independent Test**: Dock at station → menu appears within 0.5s with correct station name/type and placeholder tabs. Undock → menu closes within 0.5s.

### Implementation for User Story 3

- [ ] T040 [US3] Create StationServicesMenuController : MonoBehaviour with Canvas-based UI: header panel (station name + preset type label), tab bar (4 buttons: Refinery/Market/Repair/Cargo), content area (Text showing "Coming Soon" per tab), Close/Undock button; initially hidden; VContainer [Inject] for IStateStore, IEventBus in `Assets/Features/Docking/Views/StationServicesMenuController.cs`
- [ ] T041 [US3] Subscribe StationServicesMenuController to DockingCompletedEvent (activate Canvas, query WorldState.Stations for station name/type by StationId), subscribe to UndockingStartedEvent (deactivate Canvas — spec US3.3: menu closes "before or as undock begins"); Undock button dispatches BeginUndockingAction + publishes UndockingStartedEvent in `Assets/Features/Docking/Views/StationServicesMenuController.cs`
- [ ] T042 [US3] Populate WorldState.Stations in CreateDefaultGameState() with StationData entries for SmallMiningRelay (Id matching DockingPortComponent.StationId) and MediumRefineryHub; include Name, Position, AvailableServices in `Assets/Core/RootLifetimeScope.cs`
- [ ] T043 [US3] Register StationServicesMenuController in `Assets/Core/SceneLifetimeScope.cs`; create Canvas GameObject hierarchy in TestScene_Station via MCP (manage_gameobject create Canvas with StationServicesMenuController)
- [ ] T044 [US3] MCP verification: compile check, verify Canvas hierarchy exists in scene, verify WorldState station data correct

**Checkpoint US3**: Dock → menu shows station name/type + Coming Soon tabs. Undock → menu closes. FR-009, FR-010, SC-006, SC-007 satisfied.

---

## Phase 6: User Story 4 — Undocking via Radial Menu (P2)

**Goal**: When docked, radial menu shows only "Undock". Selecting it triggers undock sequence: detach → clearance movement → return to idle flight.

**Independent Test**: Dock at station → right-click → only Undock visible → select → ship detaches and returns to free flight. Right-click station again → station options restored.

### Implementation for User Story 4

- [ ] T045 [US4] Wire complete undock flow end-to-end: verify radial Undock click (T029) → BeginUndockingAction dispatch → DockingReducer (Docked→Undocking) → DockingSystem undocking phase (clearance movement) → CompleteUndockingAction + UndockFromStationAction → FleetReducer clears DockedAtStation → UndockCompletedEvent → services menu closes; ensure DockingState.Phase returns to None and ShipFlightMode returns to Idle in `Assets/Features/Docking/Systems/DockingSystem.cs` and `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs`
- [ ] T046 [US4] Verify docked radial context: when docked, right-click opens radial with only Undock visible; after undock completes, right-click on same station shows full station options (Approach/KeepAtRange/Orbit/Dock) — manual verification via MCP playtest
- [ ] T047 [US4] MCP verification: run all tests, verify undock flow works in TestScene_Station

**Checkpoint US4**: Docked + right-click → Undock only. Undock → ship detaches, clears, returns to idle. Post-undock → station options restored. FR-003, FR-007 satisfied.

---

## Phase 7: User Story 5 — Docking Audio & Visual Feedback (P3)

**Goal**: Configurable VFX and audio for approach, dock snap, and undock release. All designer-tunable via ScriptableObjects.

**Independent Test**: Dock/undock cycle produces visible VFX and audible audio. Config values change feedback without code changes.

### Implementation for User Story 5

- [ ] T048 [P] [US5] Create DockingVFXConfig : ScriptableObject (GameObject AlignmentGuideEffect, GameObject SnapFlashEffect, GameObject UndockReleaseEffect, float ApproachGlowIntensity=1.0f, float SnapFlashDuration=0.5f) in `Assets/Features/Docking/Data/DockingVFXConfig.cs`
- [ ] T049 [P] [US5] Create DockingAudioConfig : ScriptableObject (AudioClip ApproachHumClip, AudioClip DockClampClip, AudioClip UndockReleaseClip, AudioClip EngineStartClip, float MaxAudibleDistance=200f, float DockClampVolume=0.8f, float UndockReleaseVolume=0.6f) in `Assets/Features/Docking/Data/DockingAudioConfig.cs`
- [ ] T050 [US5] Create DockingFeedbackView : MonoBehaviour — VContainer [Inject] DockingVFXConfig + DockingAudioConfig; subscribe to events via EventBus: DockingStartedEvent → start approach VFX (alignment guide, proximity glow), DockingCompletedEvent → play snap flash VFX + dock clamp audio, DockingCancelledEvent → stop approach VFX (cleanup on cancel), UndockingStartedEvent → play engine start audio, UndockCompletedEvent → play release VFX + undock release audio; all values from injected configs in `Assets/Features/Docking/Views/DockingFeedbackView.cs`
- [ ] T051 [US5] Create DockingVFXConfig and DockingAudioConfig ScriptableObject asset instances via MCP (manage_asset action=create), add serialized fields and register in `Assets/Core/SceneLifetimeScope.cs`
- [ ] T052 [US5] MCP verification: compile check, verify config assets exist, verify DockingFeedbackView responds to events

**Checkpoint US5**: Approach → alignment guide VFX. Dock snap → clamp sound + flash. Undock → engine sound + release VFX. All configurable. FR-011 satisfied.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Edge case hardening, full scene integration, MCP-assisted playtest, performance validation.

- [ ] T053 Implement edge case handling in DockingSystem: target lost mid-approach (check target entity/GameObject existence → dispatch CancelDockingAction + DockingCancelledEvent), rapid target switching (cancel current docking before starting new), no valid docking port (default to station center position) in `Assets/Features/Docking/Systems/DockingSystem.cs`
- [ ] T054 MCP: Set up TestScene_Station for full playtest — add PlayerShip prefab instance, add InputBridge + SceneLifetimeScope + game systems, ensure DockingPortComponent authoring on both station prefabs, set stations on Selectable layer with colliders, add Station Services Menu Canvas, add DockingFeedbackView to scene
- [ ] T055 MCP playtest: enter Play mode in TestScene_Station, verify full dock/undock loop (target station → radial → Dock → approach → snap → docked → services menu → Undock → clearance → resume flight), verify both SmallMiningRelay and MediumRefineryHub work correctly
- [ ] T056 Verify asteroid mining is unchanged: target asteroid → radial → Mine → mining beam works with no regressions (FR-004, SC-004)
- [ ] T057 Performance validation: verify 60 FPS throughout docking sequence, verify <5ms docking frame budget via profiler, verify zero GC allocations in DockingSystem hot loops (SC-003)
- [ ] T058 MCP verification: final console clean check (no errors/warnings), all EditMode + PlayMode tests passing, screenshots at key moments (approach, docked, services menu, undock)

**All Acceptance Scenarios, FR-001 to FR-014, SC-001 to SC-007, and all edge cases validated.**

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — Radial menu context
- **US2 (Phase 4)**: Depends on Phase 3 — needs Dock action dispatched from radial menu
- **US3 (Phase 5)**: Depends on Phase 4 — needs docking completion events to trigger menu
- **US4 (Phase 6)**: Depends on Phase 4 — needs DockingSystem undock phase operational
- **US5 (Phase 7)**: Depends on Phase 4 — needs docking events to subscribe to; can run parallel with US3/US4
- **Polish (Phase 8)**: Depends on all user story phases

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational only — first to start
- **US2 (P1)**: Depends on US1 (Dock action wired in radial menu)
- **US3 (P2)**: Depends on US2 (docking events trigger menu); can parallel with US4
- **US4 (P2)**: Depends on US2 (undock sequence in DockingSystem); can parallel with US3
- **US5 (P3)**: Depends on US2 (docking events); can parallel with US3 and US4

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD Red-Green)
- Type definitions before logic
- Pure logic before ECS systems
- ECS systems before view integration
- MCP verification after each logical group

### Parallel Opportunities

- Phase 1: T003-T007, T010-T011 can all run in parallel (different files, no dependencies)
- Phase 2: T015 (reducer tests) and T017 (math tests) can run in parallel
- Phase 3: T023-T024 (UXML/USS) can run in parallel with T025 (InputBridge)
- Phase 4: T033 (ShipPhysicsMath) and T034 (ShipPhysicsSystem) can run in parallel
- Phase 7: T048 and T049 (VFX/Audio configs) can run in parallel
- US3, US4, and US5 can run in partial parallel once US2 completes

---

## Parallel Example: Phase 1 Setup

```
# Launch all independent type definitions in parallel:
Task T003: "Create DockingPhase + DockingState in Assets/Core/State/DockingState.cs"
Task T004: "Create IDockingAction in Assets/Core/State/IDockingAction.cs"
Task T005: "Add Docking/Docked to ShipFlightMode in Assets/Core/State/ShipFlightMode.cs"
Task T006: "Create TargetType enum in Assets/Features/Input/Data/TargetType.cs"
Task T007: "Create DockingEvents in Assets/Core/EventBus/Events/DockingEvents.cs"
```

## Parallel Example: Phase 2 Foundational

```
# Launch TDD test suites in parallel:
Task T015: "DockingReducer tests (Red) in Assets/Features/Docking/Tests/DockingReducerTests.cs"
Task T017: "DockingMath tests (Red) in Assets/Features/Docking/Tests/DockingMathTests.cs"
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete Phase 1: Setup (type definitions, asmdefs)
2. Complete Phase 2: Foundational (TDD for reducer + math, wiring)
3. Complete Phase 3: US1 (radial menu distinguishes stations vs asteroids)
4. Complete Phase 4: US2 (automatic docking works end-to-end)
5. **STOP and VALIDATE**: Full dock loop functional, all P1 acceptance scenarios pass
6. Demo: target station → radial → Dock → approach → snap → docked state

### Incremental Delivery

1. Setup + Foundational → Core proven via TDD tests
2. + US1 → Radial menu shows station vs asteroid context (deliverable)
3. + US2 → Full docking works → Demo MVP (major deliverable)
4. + US3 → Services menu appears on dock → Visual payoff
5. + US4 → Full dock/undock loop → Complete interaction cycle
6. + US5 → Polish VFX/audio → Final quality pass
7. Each story adds value without breaking previous stories

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- TDD mandatory: write tests first (Red), implement (Green), refactor
- MCP verification after every compilation-changing task
- Commit after each phase or logical group
- Stop at any checkpoint to validate story independently
- All pure logic tested via EditMode; ECS integration tested via PlayMode
- RadialMenuAction.Dock already exists as value 4 in PilotCommand.cs — reuse, don't recreate
- FleetReducer stub exists in GameStateReducer.cs — implement in place
- SetRadialChoice takes raw int (matching RadialMenuAction ordinal) — Dock=4
