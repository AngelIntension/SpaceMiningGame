# Data Model: Station Docking & Interaction Framework

**Feature**: 004-station-docking | **Date**: 2026-02-28

## New Types

### Enums

#### DockingPhase
```
None → Approaching → Snapping → Docked → Undocking → None
```

| Value | Description |
|-------|-------------|
| None | Ship is not in any docking sequence |
| Approaching | Ship autopilot flying toward docking port |
| Snapping | Magnetic snap animation playing (position/rotation interpolation) |
| Docked | Ship locked at docking port, physics suspended |
| Undocking | Ship moving along clearance vector away from port |

#### TargetType
| Value | Description |
|-------|-------------|
| None | No target selected |
| Asteroid | ECS entity with AsteroidComponent |
| Station | GameObject on Selectable layer with station marker |

### Immutable State Records (Core/State/)

#### DockingState (sealed record)

| Field | Type | Description |
|-------|------|-------------|
| Phase | DockingPhase | Current phase of docking sequence |
| TargetStationId | Option\<int\> | Station ID being docked at (matches WorldState.StationData.Id) |
| DockingPortPosition | Option\<float3\> | World-space target position for docking |
| DockingPortRotation | Option\<quaternion\> | Target orientation for docked ship |

**Static members**: `DockingState.Empty` (Phase=None, all fields default)

**Derived properties**:
- `IsDocked` → Phase == Docked
- `IsInProgress` → Phase is Approaching or Snapping

**State transitions**:

| From | Action | To |
|------|--------|----|
| None | BeginDockingAction | Approaching |
| Approaching | CompleteDockingAction | Docked |
| Approaching | CancelDockingAction | None |
| Docked | BeginUndockingAction | Undocking |
| Undocking | CompleteUndockingAction | None |

*Note: The Snapping phase is managed at the ECS level (DockingStateComponent.Phase) and does not correspond to a separate DockingState reducer transition. The reducer sees Approaching → Docked when the ECS system completes the full approach+snap sequence.*

### Actions (IDockingAction : IGameAction)

| Action Record | Fields | Purpose |
|---------------|--------|---------|
| BeginDockingAction | int StationId, float3 PortPosition, quaternion PortRotation | Start docking sequence |
| CompleteDockingAction | int StationId | Snap finished, ship is docked |
| CancelDockingAction | — | Player cancelled or target lost |
| BeginUndockingAction | — | Player initiated undock |
| CompleteUndockingAction | — | Ship reached clearance distance |

### Fleet Actions (IFleetAction : IGameAction)

| Action Record | Fields | Purpose |
|---------------|--------|---------|
| DockAtStationAction | int StationId | Set FleetState.DockedAtStation = Some(stationId) |
| UndockFromStationAction | — | Clear FleetState.DockedAtStation = None |

### Events (readonly struct)

| Event | Fields | Published When |
|-------|--------|----------------|
| DockingStartedEvent | int StationId | Ship begins docking approach |
| DockingCompletedEvent | int StationId | Ship successfully docked |
| UndockingStartedEvent | int StationId | Ship begins undocking |
| UndockCompletedEvent | int StationId | Ship returned to free flight |
| DockingCancelledEvent | — | Docking cancelled mid-approach |

### Modified Event

#### RadialMenuRequestedEvent
| Field | Type | Change |
|-------|------|--------|
| TargetId | int | Existing |
| TargetType | TargetType | **NEW** — type of the targeted object |

## Station Components (Features/Docking/Data/)

### DockingPortComponent : MonoBehaviour
*Attached to station prefab GameObjects. Read by InputBridge when targeting; data is copied into DockingStateComponent on the ship entity to initiate docking. NOT an ECS component — stations are regular GameObjects, not entities.*

| Field | Type | Description |
|-------|------|-------------|
| PortPosition | float3 | World-space docking port position |
| PortRotation | quaternion | Orientation ship should face when docked |
| DockingRange | float | Maximum range to initiate docking (default 500m) |
| SnapRange | float | Range where magnetic snap begins (default 30m) |
| StationId | int | Matches WorldState.StationData.Id |

## ECS Components (Features/Docking/Data/)

### DockingStateComponent : IComponentData
*Added to ship entity when docking starts, removed when undocking completes*

| Field | Type | Description |
|-------|------|-------------|
| Phase | DockingPhase | Current ECS-level docking phase |
| TargetPortPosition | float3 | Target dock pose position |
| TargetPortRotation | quaternion | Target dock pose rotation |
| TargetStationId | int | Station being docked at |
| SnapTimer | float | Elapsed time during snap animation |
| StartPosition | float3 | Ship position when snap began |
| StartRotation | quaternion | Ship rotation when snap began |

### DockingEventFlags : IComponentData
*Singleton component for Burst↔managed bridging. Written by DockingSystem (Burst), read and cleared by DockingEventBridgeSystem (managed). Preserves zero-GC guarantee in Burst hot path.*

| Field | Type | Description |
|-------|------|-------------|
| DockCompleted | bool | Set when Snapping→Docked transition completes |
| DockStationId | int | StationId for the completed dock |
| UndockCompleted | bool | Set when Undocking→None transition completes |

## ScriptableObject Configs (Features/Docking/Data/)

### DockingConfig

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| MaxDockingRange | float | 500f | Maximum range to initiate docking |
| SnapRange | float | 30f | Range where magnetic snap begins |
| SnapDuration | float | 1.5f | Duration of snap animation in seconds |
| UndockClearanceDistance | float | 100f | Distance ship moves on undock |
| UndockDuration | float | 2f | Duration of undock movement in seconds |

### DockingVFXConfig

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| AlignmentGuideEffect | GameObject | null | Particle/line effect during approach |
| SnapFlashEffect | GameObject | null | Flash effect on dock completion |
| UndockReleaseEffect | GameObject | null | Effect on undock detachment |
| ApproachGlowIntensity | float | 1.0f | Intensity of approach proximity glow |
| SnapFlashDuration | float | 0.5f | Duration of snap flash effect |

### DockingAudioConfig

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| ApproachHumClip | AudioClip | null | Ambient hum during approach |
| DockClampClip | AudioClip | null | Satisfying clamp sound on dock |
| UndockReleaseClip | AudioClip | null | Release/detach sound on undock |
| EngineStartClip | AudioClip | null | Engine restart sound after undock |
| MaxAudibleDistance | float | 200f | Spatial audio falloff distance |
| DockClampVolume | float | 0.8f | Volume of dock clamp sound |
| UndockReleaseVolume | float | 0.6f | Volume of undock release sound |

## Modified Existing Types

### ShipFlightMode (enum, Core/State/)
**Added values**: `Docking` (7), `Docked` (8)

### GameLoopState (sealed record, Core/State/)
**Added field**: `DockingState Docking` — new parameter at end of constructor

### CompositeReducer (RootLifetimeScope)
**Added branch**: `IDockingAction a => state with { Loop = state.Loop with { Docking = DockingReducer.Reduce(state.Loop.Docking, a) } }`

### FleetReducer
**Added cases**: `DockAtStationAction` → set DockedAtStation, `UndockFromStationAction` → clear DockedAtStation

## Entity Relationships

```
WorldState.StationData (Id) ←→ DockingPortComponent.StationId
                               ←→ DockingState.TargetStationId
                               ←→ FleetState.DockedAtStation
                               ←→ DockingStateComponent.TargetStationId

Station prefab (scene) → has DockingPortComponent (MonoBehaviour, permanent)
Ship entity (ECS) → has DockingStateComponent (IComponentData, transient, added/removed)
Singleton entity (ECS) → has DockingEventFlags (Burst→managed bridge, cleared each frame)
```
