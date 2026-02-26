# Contract: Reducer Interfaces

All game state transitions MUST go through these pure reducer interfaces.
Signature: `(State, Action) → State` — no side effects, no exceptions, deterministic.

---

## IReducer<TState, TAction> (Generic Pattern)

```csharp
// Not a formal C# interface (static methods can't be in interfaces in C# 9),
// but all reducers MUST follow this contract:
//
//   public static TState Reduce(TState state, TAction action)
//
// Properties:
//   - Pure: same input always produces same output
//   - Total: handles all action variants (returns unchanged state for unknown)
//   - Non-throwing: never throws exceptions; invalid actions return unchanged state
```

---

## GameStateReducer

```csharp
public static GameLoopState Reduce(GameLoopState state, IGameAction action)
    => action switch
    {
        IMiningAction a    => state with { Mining = MiningReducer.Reduce(state.Mining, a) },
        IInventoryAction a => state with { Inventory = InventoryReducer.Reduce(state.Inventory, a) },
        IShipAction a      => state with { /* delegate to ShipStateReducer */ },
        ITechAction a      => state with { TechTree = TechTreeReducer.Reduce(state.TechTree, a) },
        IMarketAction a    => state with { Market = MarketReducer.Reduce(state.Market, a) },
        _ => state
    };
```

**Guarantee**: Composing sub-reducers. Each sub-reducer is independently testable.

---

## CameraReducer

```
Input:  CameraState × ICameraAction → CameraState
```

| Action | Behavior | Invariants |
|--------|----------|------------|
| `OrbitAction(DeltaYaw, DeltaPitch)` | Add deltas to orbit angles | Pitch clamped [-80, 80] |
| `ZoomAction(Delta)` | Adjust TargetDistance | Clamped [5, 50] |
| `SpeedZoomAction(NormalizedSpeed)` | Lerp TargetDistance from speed | NormalizedSpeed in [0, 1] |
| `ToggleFreeLookAction` | Toggle FreeLookActive; reset offsets | FreeLookYaw/Pitch → 0 on toggle |
| `FreeLookAction(DeltaYaw, DeltaPitch)` | Adjust free-look offsets | Only when FreeLookActive; pitch clamped |

---

## ShipStateReducer

```
Input:  ShipState × PilotCommand × float(deltaTime) → ShipState
```

| Behavior | Invariants |
|----------|------------|
| Manual thrust applies force along local axes | Velocity clamped to MaxSpeed |
| Damping reduces velocity over time | Linear and angular independently |
| AlignToPoint rotates toward target, then thrusts | FlightMode transitions correctly |
| Approach/Orbit/KeepAtRange maintain relative position | Player-specified distance from RadialMenuChoice |

**Guard clauses**: Zero mass → no-op; NaN velocity → clamp to zero.

---

## MiningReducer

```
Input:  MiningSessionState × IMiningAction → MiningSessionState
```

| Action | Behavior | Invariants |
|--------|----------|------------|
| `BeginMiningAction` | Set target, ore, reset accumulators | BeamEnergy → 1.0 |
| `MiningTickAction` | Compute yield; accumulate | Yield formula: `(power × baseYield) / (hardness × (1 + depth))` |
| `StopMiningAction` | Reset to Empty | All fields zeroed |

**Yield function** (pure, separately testable):
```
CalculateYield(miningPower, baseYield, hardness, depth, deltaTime) → MiningYieldResult
```

---

## InventoryReducer

```
Input:  InventoryState × IInventoryAction → InventoryState
```

| Action | Behavior | Invariants |
|--------|----------|------------|
| `AddResourceAction` | Add qty to stack; update volume | Reject if volume would exceed MaxVolume |
| `RemoveResourceAction` | Subtract qty from stack | Reject if insufficient; remove stack entry if qty → 0 |

**Capacity invariant**: `CurrentVolume ≤ MaxVolume` always holds after any reduction.

---

## FleetReducer (Phase 1+)

```
Input:  FleetState × IFleetAction → FleetState
```

| Action | Behavior | Invariants |
|--------|----------|------------|
| `SwapShipAction` | Change active ship | Requires DockedAtStation; ship must be owned |
| `EquipModuleAction` | Change module in slot | Slot must exist on ship |
| `AcquireShipAction` | Add ship to fleet | Ship added to Ships array |

---

## TechTreeReducer (Phase 1+)

```
Input:  TechTreeState × ITechAction → TechTreeState
```

| Action | Behavior | Invariants |
|--------|----------|------------|
| `UnlockTechAction` | Unlock node if available + resources sufficient | Prerequisites must all be Unlocked |
| `RefreshAvailabilityAction` | Recompute Available status | DAG traversal; nodes with all prereqs unlocked → Available |

---

## MarketReducer (Phase 3)

```
Input:  MarketState × IMarketAction → MarketState
```

| Action | Behavior | Invariants |
|--------|----------|------------|
| `MarketTickAction` | Deterministic price update | Price clamped [0.1×, 10×] base |
| `PlaceOrderAction` | Add order to commodity | Validates issuer funds/inventory |
| `FillOrderAction` | Execute order match | Quantity must not exceed order |

**Determinism**: Same state + same tick count → identical prices (bit-for-bit).
