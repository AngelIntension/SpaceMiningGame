# Implementation Plan: Station Docking & Interaction Framework

**Branch**: `004-station-docking` | **Date**: 2026-02-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-station-docking/spec.md`

## Summary

Add context-sensitive radial menu for stations and automatic docking to MS2 station presets. When targeting a station the radial menu shows Approach/Keep at Distance/Orbit/Dock (undocked) or Undock (docked). Selecting "Dock" triggers a fully automatic approach → align → magnetic-snap sequence managed by a Burst-compiled `DockingSystem`. On dock the ship is locked, physics suspended, and a Station Services Menu skeleton (Canvas panel with placeholder tabs) opens. Undocking reverses the sequence. All state follows the pure reducer pattern; all feedback is designer-configurable via ScriptableObjects.

## Technical Context

**Language/Version**: C# 9.0 / .NET Framework 4.7.1
**Primary Dependencies**: Unity 6 (6000.3.10f1), URP 17.3.0, Entities 1.3.2, UniTask 2.5.10, VContainer 1.16.7, Input System 1.18.0
**Storage**: In-memory immutable state (`IStateStore`), ScriptableObjects for static config
**Testing**: NUnit + Unity Test Framework (EditMode for pure logic, PlayMode for ECS integration)
**Target Platform**: Windows 64-bit Standalone
**Project Type**: Game (desktop)
**Performance Goals**: 60 FPS, <5 ms docking frame budget, zero GC in hot loops
**Constraints**: <2 ms frame spikes in docking systems, Burst-compatible hot paths
**Scale/Scope**: 2 station presets, single player, MVP

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Functional & Immutable First | PASS | `DockingState` as `sealed record`, `DockingReducer` pure `(State, Action) → State`, all actions are immutable records |
| II. Predictability & Testability | PASS | All docking logic testable via pure reducers and pure math functions. No hidden state. |
| III. Performance by Default | PASS | `DockingSystem` is `[BurstCompile]` ISystem. Snap animation uses direct ECS component writes (no GC). |
| IV. Data-Oriented Design | PASS | ECS components for docking port data (`DockingPortComponent`, `DockingStateComponent`). ScriptableObjects for configs. |
| V. Modularity & Extensibility | PASS | New `Features/Docking/` with own asmdef. EventBus for cross-system communication. No direct field writes. |
| VI. Explicit Over Implicit | PASS | All DI via VContainer. Event subscriptions explicit. No reflection-based wiring. |
| Editor Automation (MCP) | PASS | MCP used for scene wiring, compilation checks, console monitoring, test execution at each phase. |

No violations. All gates pass.

## Project Structure

### Documentation (this feature)

```text
specs/004-station-docking/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Assets/
├── Core/
│   ├── State/
│   │   ├── DockingState.cs              # NEW: DockingPhase enum + DockingState record
│   │   ├── IDockingAction.cs            # NEW: IDockingAction : IGameAction
│   │   ├── GameLoopState.cs             # MODIFIED: add DockingState field
│   │   ├── ShipFlightMode.cs            # MODIFIED: add Docking, Docked enum values
│   │   ├── FleetActions.cs             # NEW: DockAtStationAction, UndockFromStationAction
│   │   └── GameStateReducer.cs        # MODIFIED: FleetReducer handles DockAtStation/UndockFromStation
│   ├── EventBus/Events/
│   │   ├── DockingEvents.cs             # NEW: DockingStarted/Completed/UndockCompleted events
│   │   └── RadialMenuRequestedEvent.cs  # MODIFIED: add TargetType field
│   ├── RootLifetimeScope.cs             # MODIFIED: wire DockingReducer, add DockingState initial
│   └── SceneLifetimeScope.cs            # MODIFIED: register docking configs
│
├── Features/
│   ├── Docking/                          # NEW FEATURE MODULE
│   │   ├── Data/
│   │   │   ├── DockingActions.cs         # Concrete action records (BeginDocking, Complete, Cancel, Undock)
│   │   │   ├── DockingPortComponent.cs   # MonoBehaviour: station docking port data (on station prefabs)
│   │   │   ├── DockingComponents.cs      # ECS: DockingStateComponent, DockingEventFlags (on ship entity)
│   │   │   ├── DockingConfig.cs          # ScriptableObject: ranges, timings
│   │   │   ├── DockingVFXConfig.cs       # ScriptableObject: VFX references + params
│   │   │   └── DockingAudioConfig.cs     # ScriptableObject: audio clips + params
│   │   ├── Systems/
│   │   │   ├── DockingReducer.cs         # Pure reducer (DockingState, IDockingAction) → DockingState
│   │   │   ├── DockingMath.cs            # Pure math: approach interpolation, snap curves, clearance
│   │   │   ├── DockingSystem.cs          # Burst ISystem: docking state machine (approach/snap/hold/undock)
│   │   │   └── DockingEventBridgeSystem.cs # Managed ISystem: reads DockingEventFlags, dispatches actions/events
│   │   ├── Views/
│   │   │   ├── StationServicesMenuController.cs  # Canvas UI: auto-open on dock, placeholder tabs
│   │   │   └── DockingFeedbackView.cs    # MonoBehaviour: VFX + audio playback
│   │   ├── Tests/
│   │   │   ├── DockingReducerTests.cs    # Unit tests for reducer
│   │   │   ├── DockingMathTests.cs       # Unit tests for pure math
│   │   │   └── DockingSystemTests.cs     # Integration tests for ECS system
│   │   └── VoidHarvest.Features.Docking.asmdef
│   │
│   ├── Input/
│   │   ├── Data/
│   │   │   └── TargetType.cs             # NEW: enum TargetType { None, Asteroid, Station }
│   │   └── Views/
│   │       └── InputBridge.cs            # MODIFIED: track TargetType, station targeting
│   │
│   ├── HUD/Views/RadialMenu/
│   │   ├── RadialMenuController.cs       # MODIFIED: context-sensitive segment visibility
│   │   └── RadialMenu.uxml              # MODIFIED: add segment-dock, segment-undock buttons
│   │
│   ├── Ship/Systems/
│   │   ├── ShipPhysicsMath.cs            # MODIFIED: DetermineFlightMode handles Docking/Docked
│   │   └── ShipPhysicsSystem.cs          # MODIFIED: Docked mode freezes ship
│   │
│   └── Base/
│       └── Prefabs/                      # MODIFIED via MCP: add colliders, Selectable layer, docking port markers
│
└── Scenes/
    └── TestScene_Station.unity           # MODIFIED via MCP: add ship, InputBridge, game systems
```

**Structure Decision**: New `Features/Docking/` module with own asmdef following existing `Data/Systems/Views/Tests/` pattern. Shared types (`DockingState`, `IDockingAction`, `DockingPhase`) live in `Core/State/` alongside existing state records. Docking-specific types live in `Features/Docking/`.

## Implementation Phases

### Phase 1 — Data Layer, State & Radial Menu Context (P1)

**Goal**: Foundation types, docking state management, and context-sensitive radial menu.

**Prerequisite**: MCP connectivity verified.

#### 1.1 Core State Types
- Add `DockingPhase` enum and `DockingState` sealed record to `Core/State/`
- Add `IDockingAction : IGameAction` interface to `Core/State/`
- Add `Docking` and `Docked` values to `ShipFlightMode` enum
- Add `DockingState` field to `GameLoopState` record
- Create `DockingActions.cs` in `Features/Docking/Data/` with concrete action records
- Create `DockingEvents.cs` in `Core/EventBus/Events/`
- Wire `DockingReducer` into `RootLifetimeScope.CompositeReducer`
- **MCP**: Compile check + console clean gate

#### 1.2 DockingReducer (TDD)
- Write `DockingReducerTests.cs` — Red: test all state transitions (None→Approaching, Approaching→Docked, Docked→Undocking, etc.)
- Implement `DockingReducer.cs` — Green: pure `(DockingState, IDockingAction) → DockingState`
- **MCP**: Run EditMode tests, verify all pass

#### 1.3 Target Type Detection
- Create `TargetType` enum in `Features/Input/Data/`
- Modify `InputBridge`: add `_selectedTargetType` field, set to `Station` on `TryRaycastSelectable` hit, `Asteroid` on `TryRaycastAsteroid` hit, `None` on clear
- Expose `SelectedTargetType` property
- Extend `RadialMenuRequestedEvent` with `TargetType` field
- **MCP**: Compile check

#### 1.4 Context-Sensitive Radial Menu
- Add `segment-dock` and `segment-undock` buttons to `RadialMenu.uxml`
- Add `segment-dock` and `segment-undock` USS styling
- Modify `RadialMenuController`: on Open(), query `TargetType` from the event and `DockingState.IsDocked` from `IStateStore`
  - Target=Station, Undocked → show Approach, KeepAtRange, Orbit, Dock; hide Mine, Undock
  - Target=Station, Docked → show Undock only; hide all others
  - Target=Asteroid → show Approach, Orbit, Mine, KeepAtRange; hide Dock, Undock (unchanged behavior)
- Handle `Dock` segment click: dispatch `BeginDockingAction` + set radial choice
- Handle `Undock` segment click: dispatch `BeginUndockingAction`
- **MCP**: Compile check, run all HUD tests

#### 1.5 Station Prefab Setup (MCP)
- Add BoxCollider (or compound collider) to SmallMiningRelay and MediumRefineryHub prefabs
- Set both to `Selectable` layer
- Add empty child GameObject "DockingPort" at appropriate position on each station
- **MCP**: Verify prefab hierarchy, verify Selectable layer

**Verification Checkpoint — Spec Acceptance Scenarios US1.1-1.4, FR-001 to FR-004**:
- Target station → radial shows station options
- Target asteroid → radial shows mining options (unchanged)
- No regressions in existing mining/radial behavior

---

### Phase 2 — Automatic Docking Sequence (P1)

**Goal**: Ship autopilot approach → align → magnetic snap → docked state.

#### 2.1 DockingMath (TDD)
- Write `DockingMathTests.cs` — Red: test approach interpolation, snap curve, clearance vector, alignment angle
- Implement `DockingMath.cs` — Green: pure static functions
  - `ComputeApproachTarget(shipPos, portPos, approachOffset) → float3`
  - `ComputeSnapProgress(elapsed, duration) → float` (ease-in-out)
  - `InterpolateSnapPose(startPos, startRot, targetPos, targetRot, t) → (float3, quaternion)`
  - `ComputeClearancePosition(portPos, portForward, clearanceDistance) → float3`
  - `IsWithinDockingRange(shipPos, portPos, maxRange) → bool`
  - `IsWithinSnapRange(shipPos, portPos, snapRange) → bool`
- **MCP**: Run EditMode tests, verify all pass

#### 2.2 DockingComponents
- Create `DockingPortComponent : MonoBehaviour` — PortPosition (float3), PortRotation (quaternion), DockingRange (float), SnapRange (float), StationId (int). Lives on station prefab GameObjects (not ECS entities). InputBridge reads this MonoBehaviour when targeting and copies data into DockingStateComponent on the ship entity.
- Create `DockingStateComponent : IComponentData` — Phase, TargetPortPosition, TargetPortRotation, TargetStationId, SnapTimer, StartPosition, StartRotation. Added to ship entity when docking starts, removed on undock completion. // CONSTITUTION DEVIATION: ECS mutable shell
- **MCP**: Compile check

#### 2.3 ShipPhysics Extension
- Extend `ShipPhysicsMath.DetermineFlightMode()`:
  - `current == Docked` → return `Docked` (locked until undock action)
  - `current == Docking` + manual input → return `ManualThrust` (cancel)
  - `current == Docking` + no manual input → return `Docking` (stay)
- Extend `ShipPhysicsSystem.OnUpdate`:
  - `Docked` mode: zero velocity, zero angular velocity, skip force application
  - `Docking` mode: handled by DockingSystem (ShipPhysicsSystem applies existing approach logic via AlignPoint)
- **MCP**: Compile check, run Ship tests

#### 2.4 DockingSystem (ECS)
- Create `DockingSystem : ISystem` with `[BurstCompile]`, `[UpdateInGroup(typeof(SimulationSystemGroup))]`, `[UpdateBefore(typeof(ShipPhysicsSystem))]`
- State machine logic per frame:
  - **Approaching**: DockingSystem sets `PilotCommandComponent.AlignPoint` to port position, `RadialAction` to Approach. ShipPhysicsSystem handles approach thrust. DockingSystem checks `IsWithinSnapRange()` → transition to Snapping.
  - **Snapping**: DockingSystem takes over position/rotation directly. Lerps ship pose via `InterpolateSnapPose()` over `SnapDuration`. On completion → transition to Docked.
  - **Docked**: DockingSystem locks ship position/rotation to docking port. Sets `ShipFlightModeComponent.Mode = Docked`. Writes completion flags to a `DockingEventFlags` singleton component.
  - **Undocking**: DockingSystem moves ship along clearance vector via lerp. On reaching clearance distance → complete, set `ShipFlightModeComponent.Mode = Idle`. Remove `DockingStateComponent`. Writes undock-complete flag.
- **Burst↔Managed bridging**: A companion `DockingEventBridgeSystem` (managed, non-Burst, `[UpdateAfter(typeof(DockingSystem))]`) reads the `DockingEventFlags` singleton each frame. When flags are set, it dispatches managed actions (`CompleteDockingAction` + `DockAtStationAction`, or `CompleteUndockingAction` + `UndockFromStationAction`) via `IStateStore` and publishes events via `IEventBus`, then clears the flags. This preserves zero-GC in the Burst hot path while allowing managed interop.
- **MCP**: Compile check

#### 2.5 DockingConfig
- Create `DockingConfig : ScriptableObject` with: MaxDockingRange (500f), SnapRange (30f), SnapDuration (1.5f), UndockClearanceDistance (100f), UndockDuration (2f)
- Create asset instance, register in `SceneLifetimeScope`
- **MCP**: Verify asset exists, compile check

#### 2.6 InputBridge Docking Integration
- When "Dock" selected from radial: get station's docking port position from scene, call `SetRadialChoice(4, 0)`, set `_alignPoint` to port position, set `_hasAlignPoint = true`
- Add `DockingStateComponent` to ship entity with target port data
- When manual thrust detected during Docking: remove `DockingStateComponent`, dispatch `CancelDockingAction`
- When docked: disable target selection and radial menu actions except through the docked menu
- **MCP**: Compile check

**Verification Checkpoint — Spec Acceptance Scenarios US2.1-2.5, FR-005 to FR-008, FR-013**:
- Select "Dock" → auto-approach → snap → docked state
- Manual thrust during approach → cancel docking
- Beyond 500m → approach first, then dock
- Target lost during approach → cancel gracefully

---

### Phase 3 — Station Services Menu Skeleton (P2)

**Goal**: Canvas panel auto-opens on dock, shows station info + placeholder tabs, closes on undock.

#### 3.1 StationServicesMenuController
- Create Canvas-based UI panel with:
  - Header: station name + preset type (e.g., "Small Mining Relay — Alpha Station")
  - Tab bar: "Refinery", "Market", "Repair", "Cargo" buttons
  - Content area: "Coming Soon" placeholder text per tab
  - Close/Undock button
- Controller subscribes to `DockingCompletedEvent` → open menu, query `WorldState.Stations` for station name/type
- Controller subscribes to `UndockingStartedEvent` → close menu (spec US3.3: menu closes "before or as undock begins")
- Undock button dispatches `BeginUndockingAction` + publishes `UndockingStartedEvent`
- Register in `SceneLifetimeScope`
- **MCP**: Create Canvas hierarchy, verify layout, compile check

#### 3.2 WorldState Station Data
- Populate `WorldState.Stations` in `RootLifetimeScope.CreateDefaultGameState()` with entries for SmallMiningRelay and MediumRefineryHub
- Station IDs match `DockingPortComponent.StationId` on scene objects
- **MCP**: Compile check

**Verification Checkpoint — Spec Acceptance Scenarios US3.1-3.5, FR-009 to FR-010**:
- Dock → menu appears with correct station name/type
- Placeholder tabs visible with "Coming Soon"
- Undock → menu closes
- Different stations show different names

---

### Phase 4 — Undocking via Radial Menu (P2)

**Goal**: Docked radial menu shows only "Undock", undocking sequence reverses dock.

#### 4.1 Undock Flow
- Radial menu "Undock" click → dispatch `BeginUndockingAction` → publish `UndockingStartedEvent` → close services menu
- `DockingReducer` transitions: Docked → Undocking
- `DockingSystem` handles Undocking phase: move ship along clearance vector, restore `ShipFlightModeComponent.Mode = Idle`
- On clearance complete: dispatch `CompleteUndockingAction` → `DockingReducer` transitions: Undocking → None
- Dispatch `UndockFromStationAction` → `FleetReducer` clears `DockedAtStation`
- Publish `UndockCompletedEvent`
- **MCP**: Compile check, run all tests

**Verification Checkpoint — Spec Acceptance Scenarios US4.1-4.4, FR-003, FR-007**:
- Docked + right-click → Undock only
- Undock → ship detaches, clears, returns to idle flight
- After undock, right-click station → station options restored

---

### Phase 5 — Audio & Visual Feedback (P3)

**Goal**: Configurable VFX/audio for dock/undock sequences.

#### 5.1 DockingVFXConfig + DockingAudioConfig
- Create `DockingVFXConfig : ScriptableObject` — alignment guide, snap flash, undock release effect references + params
- Create `DockingAudioConfig : ScriptableObject` — approach hum, dock clamp, undock release, engine start clips + volumes
- Create asset instances, register in `SceneLifetimeScope`
- **MCP**: Verify assets, compile check

#### 5.2 DockingFeedbackView
- MonoBehaviour subscribed to docking events via EventBus
- On `DockingStartedEvent`: start approach visual feedback (proximity glow/guide line)
- On `DockingCompletedEvent`: play snap flash VFX + dock clamp audio
- On `UndockingStartedEvent`: play engine start audio
- On `UndockCompletedEvent`: play release VFX + release audio
- All effects read config from injected ScriptableObjects — zero hardcoded values
- **MCP**: Compile check

**Verification Checkpoint — Spec Acceptance Scenarios US5.1-5.4, FR-011**:
- Approach → visual feedback visible
- Dock snap → sound + flash
- Undock → engine sound + release effect
- All tweakable via config

---

### Phase 6 — Edge Cases, Integration & Playtest (Final)

**Goal**: Handle all edge cases, wire TestScene_Station, full MCP-assisted playtest.

#### 6.1 Edge Case Hardening
- Target lost mid-approach: `DockingSystem` checks if target entity still exists → cancel if missing
- Rapid target switching: Cancel current docking before starting new sequence
- Already docked + Dock: Prevented by radial menu context (Dock not shown when docked)
- No valid docking port: Default to station center position
- Beyond 2000m: Approach first (existing behavior), docking triggers at 500m range

#### 6.2 TestScene_Station Setup (MCP)
- Add PlayerShip prefab instance to TestScene_Station
- Add InputBridge, SceneLifetimeScope, game systems
- Add `DockingPortComponent` (via MonoBehaviour authoring) to both station prefabs
- Set stations on `Selectable` layer with colliders
- Populate `WorldState.Stations` with scene station positions
- Add Station Services Menu Canvas to scene
- **MCP**: Full scene hierarchy verification

#### 6.3 MCP-Assisted Playtest
- Enter Play mode via MCP
- Verify targeting both stations (left-click)
- Verify radial menu context (right-click on station vs asteroid)
- Fly to station → Dock → verify approach + snap + docked state
- Verify services menu appears with correct station info
- Undock → verify clearance + return to flight
- Verify 60 FPS throughout
- Verify asteroid mining still works unchanged
- **MCP**: Screenshots at key moments, console clean check, performance log

**Verification Checkpoint — All spec acceptance scenarios, all FRs, all edge cases, SC-001 to SC-007**

---

### Assembly Definition Dependencies

```
VoidHarvest.Features.Docking.asmdef:
  References:
  - VoidHarvest.Core.Extensions
  - VoidHarvest.Core.State
  - VoidHarvest.Core.EventBus
  - VoidHarvest.Features.Ship
  - VoidHarvest.Features.Base
  - Unity.Entities
  - Unity.Entities.Hybrid
  - Unity.Mathematics
  - Unity.Burst
  - Unity.Collections
  - Unity.Transforms
  - VContainer
  - UniTask
  allowUnsafeCode: true

VoidHarvest.Features.Docking.Tests.asmdef:
  References:
  - VoidHarvest.Features.Docking
  - VoidHarvest.Core.State
  - VoidHarvest.Core.EventBus
  - VoidHarvest.Core.Extensions
  - Unity.Mathematics
  - UnityEngine.TestRunner
  - UnityEditor.TestRunner
  includePlatforms: [Editor]
  overrideReferences: true
  precompiledReferences: [nunit.framework.dll]
```

**Existing asmdef modifications:**
- `VoidHarvest.Features.HUD.asmdef`: add reference to `VoidHarvest.Features.Docking` (for DockingState query)
- `VoidHarvest.Features.Input.asmdef`: add reference to `VoidHarvest.Features.Docking` (for DockingStateComponent management)

## Complexity Tracking

No constitution violations. All patterns align with existing architecture.
