# Data Model: In-Flight Targeting & Multi-Target Lock System

**Branch**: `007-target-lock-system` | **Date**: 2026-03-02

## Core Interface

### ITargetable (interface — Core/Extensions)

Cross-cutting contract for MonoBehaviour-based targetable objects (stations, future NPCs, etc.). ECS entities (asteroids) use adapter functions to produce `TargetInfo` instead.

```
ITargetable
├── TargetId: int              — unique identifier (GO instanceID or entity index)
├── DisplayName: string        — human-readable name (e.g., "Medium Refinery Hub")
├── TypeLabel: string          — category label (e.g., "Station", "Luminite")
└── TargetType: TargetType     — enum discriminator (Asteroid, Station, etc.)
```

**Implementations**:
- `TargetableStation : MonoBehaviour, ITargetable` — placed on station prefabs, reads from `StationData` via `StationId` match against `WorldState.Stations`

**Future implementors** (not in this spec): `TargetableNPC`, `TargetableDebris`, `TargetableCargoPod`, etc.

---

## Shared Value Types (Core/Extensions)

### TargetInfo (readonly struct)

Immutable snapshot of display data for any targetable object. Common output from both `ITargetable` MonoBehaviours and ECS asteroid queries.

```
TargetInfo
├── TargetId: int              — unique identifier
├── DisplayName: string        — name for reticle/card display
├── TypeLabel: string          — type for reticle/card display
├── TargetType: TargetType     — Asteroid or Station
├── IsValid: bool              — derived: TargetId >= 0
└── static None: TargetInfo    — sentinel for "no target"
```

**Construction paths**:
- From `ITargetable`: `TargetInfo.From(ITargetable target)`
- From asteroid ECS: `TargetInfo.FromAsteroid(int entityIndex, string displayName, string oreTypeName)`

---

## State Slice

### TargetingState (sealed record — Features/Targeting/Data)

New slice added to `GameLoopState`. Holds all targeting-related game state.

```
TargetingState
├── Selection: SelectionData           — currently selected (not locked) target
├── LockAcquisition: LockAcquisitionData — active lock-in-progress (if any)
├── LockedTargets: ImmutableArray<TargetLockData> — all completed locks
└── static Empty: TargetingState       — default sentinel
```

### SelectionData (readonly struct)

```
SelectionData
├── TargetId: int              — -1 = no selection
├── TargetType: TargetType
├── DisplayName: string
├── TypeLabel: string
├── HasSelection: bool         — derived: TargetId >= 0
└── static None: SelectionData — sentinel (-1, None, "", "")
```

### LockAcquisitionData (readonly struct)

```
LockAcquisitionData
├── TargetId: int              — target being locked (-1 = none)
├── ElapsedTime: float         — seconds elapsed since lock started
├── TotalDuration: float       — total lock time (from LockTimeMath)
├── Status: LockAcquisitionStatus — None / InProgress / Completed / Cancelled
├── Progress: float            — derived: ElapsedTime / TotalDuration [0..1]
├── IsActive: bool             — derived: Status == InProgress
└── static None                — sentinel (None status)
```

### LockAcquisitionStatus (enum)

```
None = 0, InProgress = 1, Completed = 2, Cancelled = 3
```

### TargetLockData (readonly struct)

```
TargetLockData
├── TargetId: int
├── TargetType: TargetType
├── DisplayName: string
└── TypeLabel: string
```

---

## Actions (ITargetingAction : IGameAction)

All actions are `sealed record` types implementing `ITargetingAction`.

| Action | Fields | Effect |
|--------|--------|--------|
| `SelectTargetAction` | `TargetId`, `TargetType`, `DisplayName`, `TypeLabel` | Sets Selection, cancels any active lock acquisition if target changed |
| `ClearSelectionAction` | _(none)_ | Clears Selection to None, cancels active lock acquisition |
| `BeginLockAction` | `TargetId`, `Duration` | Starts lock acquisition for selected target |
| `LockTickAction` | `DeltaTime` | Advances lock acquisition timer |
| `CompleteLockAction` | _(none)_ | Converts active acquisition to a TargetLockData, appends to LockedTargets |
| `CancelLockAction` | _(none)_ | Cancels active lock acquisition, resets to None |
| `UnlockTargetAction` | `TargetId` | Removes specific target from LockedTargets |
| `ClearAllLocksAction` | _(none)_ | Empties LockedTargets (used on dock/ship swap) |

---

## Events (readonly structs)

| Event | Fields | Published when |
|-------|--------|----------------|
| `TargetLockedEvent` | `TargetId: int`, `DisplayName: string` | Lock acquisition completes successfully |
| `TargetUnlockedEvent` | `TargetId: int` | Target manually dismissed from card |
| `LockFailedEvent` | `TargetId: int`, `Reason: LockFailReason` | Lock cancelled (deselect, out-of-range, destroyed) |
| `LockSlotsFullEvent` | _(none)_ | Player attempts lock at max capacity |
| `TargetLostEvent` | `TargetId: int` | Locked target destroyed (asteroid depleted) |
| `AllLocksCleared` | _(none)_ | All locks cleared (docking, ship swap) |

### LockFailReason (enum)

```
Deselected = 0, OutOfRange = 1, TargetDestroyed = 2
```

---

## Configuration (ScriptableObjects)

### ShipArchetypeConfig (modified — Ship/Data)

New fields added to existing SO:

```
+ BaseLockTime: float          — seconds to acquire a lock (default 1.5)
+ MaxTargetLocks: int          — max simultaneous locks (default 3)
+ MaxLockRange: float          — max range for lock acquisition in meters (default 5000)
```

### TargetingConfig (new SO — Targeting/Data)

Global targeting configuration (not per-ship).

```
TargetingConfig
├── ReticlePadding: float      — screen-space padding around target (pixels, default 20)
├── ReticleMinSize: float      — minimum reticle size (pixels, default 40)
├── ReticleMaxSize: float      — maximum reticle size (pixels, default 300)
├── LockProgressArcWidth: float — progress arc thickness (pixels, default 3)
├── OffScreenIndicatorMargin: float — margin from screen edge (pixels, default 30)
├── ViewportRenderSize: int    — RenderTexture resolution (default 128)
├── ViewportFOV: float         — viewport camera field of view (default 30)
├── PreviewStageOffset: Vector3 — world-space offset for preview staging area
```

---

## Pure Functions

### LockTimeMath (static class — Targeting/Systems)

```
CalculateLockTime(baseLockTime: float, target: TargetInfo) → float
  V1: returns baseLockTime directly
  Future: distance factor, target size factor, sensor upgrade multiplier
```

### TargetingMath (static class — Targeting/Systems)

```
CalculateScreenBounds(worldPosition: float3, visualRadius: float, camera: Camera) → Rect
  Projects world-space sphere bounds to screen-space rectangle

ClampToScreenEdge(screenPos: Vector2, screenSize: Vector2, margin: float) → (Vector2 position, float angle)
  Clamps a screen position to viewport edges, returns clamped position and angle toward original

IsInViewport(screenPos: Vector3) → bool
  Checks if screen position is within camera viewport (z > 0, x/y in bounds)

FormatRange(distanceMeters: float) → string
  Formats distance: "1,247 m", "523 m", etc.
```

---

## State Integration Points

### GameLoopState (modified)

```diff
  public sealed record GameLoopState(
      ExploreState Explore,
      MiningSessionState Mining,
      InventoryState Inventory,
      StationServicesState StationServices,
      TechTreeState TechTree,
      FleetState Fleet,
      BaseState Base,
      MarketState Market,
-     DockingState Docking
+     DockingState Docking,
+     TargetingState Targeting
  );
```

### CompositeReducer (RootLifetimeScope — modified)

New arm in switch expression:

```
ITargetingAction a => state with { Loop = state.Loop with {
    Targeting = TargetingReducer.Reduce(state.Loop.Targeting, a) } }
```

Cross-cutting: `CompleteDockingAction` and `CompleteUndockingAction` already handled — add `ClearAllLocksAction` dispatch alongside existing dock/undock handling.

---

## Assembly Dependencies

### New: VoidHarvest.Features.Targeting

```
References:
  - VoidHarvest.Core.Extensions  (ITargetable, TargetType, TargetInfo)
  - VoidHarvest.Core.State       (IStateStore, IGameAction, GameState)
  - VoidHarvest.Core.EventBus    (IEventBus)
  - VoidHarvest.Features.Ship    (ShipArchetypeConfig — for lock time/max targets)
  - VoidHarvest.Features.Mining   (AsteroidComponent, OreDisplayNames — for asteroid TargetInfo)
  - Unity.Mathematics
  - Unity.Entities
  - Unity.Collections
  - Unity.Transforms
  - UniTask
  - VContainer
```

### New: VoidHarvest.Features.Targeting.Tests

```
References:
  - VoidHarvest.Features.Targeting
  - VoidHarvest.Core.Extensions
  - VoidHarvest.Core.State
  - VoidHarvest.Core.EventBus
  - UnityEngine.TestRunner
  - UnityEditor.TestRunner
Platform: Editor only
```

### Modified: VoidHarvest.Features.HUD

```diff
  + VoidHarvest.Features.Targeting  (for TargetingState reads, TargetInfo)
```

### Modified: VoidHarvest.Features.Input

```diff
  + VoidHarvest.Features.Targeting  (for dispatching targeting actions)
```
