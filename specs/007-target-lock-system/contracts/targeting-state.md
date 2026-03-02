# Contract: TargetingState & Actions

**Feature**: 007-target-lock-system
**Date**: 2026-03-02

## Purpose

`TargetingState` is the public state contract for the targeting system. It is a new slice in `GameLoopState`, managed by `TargetingReducer`. All targeting state transitions flow through `ITargetingAction` records dispatched to `IStateStore`.

## TargetingState (Features/Targeting/Data)

```
TargetingState (sealed record)
├── Selection: SelectionData              — Currently selected (not locked) target
├── LockAcquisition: LockAcquisitionData  — Active lock-in-progress (if any)
├── LockedTargets: ImmutableArray<TargetLockData> — All completed locks
└── static Empty: TargetingState          — Default sentinel (no selection, no locks)
```

### Integration Point

```
GameLoopState (sealed record) — MODIFIED
├── ... (existing slices)
├── DockingState Docking
└── TargetingState Targeting     # NEW
```

## Actions (ITargetingAction : IGameAction)

All actions are dispatched via `IStateStore.Dispatch()`. The `CompositeReducer` routes `ITargetingAction` to `TargetingReducer.Reduce()`.

| Action | Fields | Effect on TargetingState |
|--------|--------|--------------------------|
| `SelectTargetAction` | TargetId, TargetType, DisplayName, TypeLabel | Sets Selection; cancels active lock if target changed |
| `ClearSelectionAction` | _(none)_ | Clears Selection to None; cancels active lock |
| `BeginLockAction` | TargetId, Duration | Starts lock acquisition for selected target |
| `LockTickAction` | DeltaTime | Advances lock acquisition timer |
| `CompleteLockAction` | _(none)_ | Converts acquisition to TargetLockData, appends to LockedTargets |
| `CancelLockAction` | _(none)_ | Cancels active lock acquisition |
| `UnlockTargetAction` | TargetId | Removes specific target from LockedTargets |
| `ClearAllLocksAction` | _(none)_ | Empties LockedTargets (dock/undock/ship swap) |

### Reducer Routing (CompositeReducer)

```
ITargetingAction a => state with { Loop = state.Loop with {
    Targeting = TargetingReducer.Reduce(state.Loop.Targeting, a) } }
```

Cross-cutting: `CompleteDockingAction` and `CompleteUndockingAction` handlers in `CompositeReducer` also dispatch `ClearAllLocksAction` to clear locks on dock/undock.

## Events (published via IEventBus)

| Event | Fields | Published When |
|-------|--------|----------------|
| `TargetLockedEvent` | TargetId, DisplayName | Lock acquisition completes successfully |
| `TargetUnlockedEvent` | TargetId | Target manually dismissed from card |
| `LockFailedEvent` | TargetId, Reason (LockFailReason) | Lock cancelled (deselect, out-of-range, destroyed) |
| `LockSlotsFullEvent` | _(none)_ | Player attempts lock at max capacity |
| `TargetLostEvent` | TargetId | Locked target destroyed (asteroid depleted) |
| `AllLocksCleared` | _(none)_ | All locks cleared (docking, ship swap) |

## State Query Patterns

| Query | How |
|-------|-----|
| Is anything selected? | `state.Loop.Targeting.Selection.HasSelection` |
| Is a lock in progress? | `state.Loop.Targeting.LockAcquisition.IsActive` |
| Lock progress [0..1] | `state.Loop.Targeting.LockAcquisition.Progress` |
| Number of locked targets | `state.Loop.Targeting.LockedTargets.Length` |
| Is target already locked? | `state.Loop.Targeting.LockedTargets.Any(t => t.TargetId == id)` |
| Can lock more targets? | `LockedTargets.Length < ship.MaxTargetLocks` |

## Invariants

1. `TargetingReducer.Reduce()` MUST be a pure static function with no side effects.
2. `LockedTargets` MUST NOT contain duplicate `TargetId` values.
3. `LockAcquisition.Status` MUST transition: None → InProgress → (Completed | Cancelled) → None.
4. `Selection` and `LockAcquisition` MUST be consistent: if `LockAcquisition.IsActive`, then `Selection.TargetId == LockAcquisition.TargetId`.
5. `CompleteLockAction` MUST only succeed if `LockAcquisition.Status == Completed` — the reducer ignores it otherwise.
6. `ClearAllLocksAction` MUST also reset Selection and LockAcquisition to their None sentinels.

## Versioning

This contract is introduced in Spec 007. Changes to action record fields or reducer behavior require a spec amendment. Adding new action types is non-breaking. Adding new fields to `TargetingState` is non-breaking (use `with` expressions for construction).
