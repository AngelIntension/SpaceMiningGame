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

Operates on the **root** `GameState` — not `GameLoopState` — because `CameraState` and `ShipState` are root-level siblings, not fields of `GameLoopState`.

```csharp
public static GameState Reduce(GameState state, IGameAction action)
    => action switch
    {
        ICameraAction a    => state with { Camera = CameraReducer.Reduce(state.Camera, a) },
        IShipAction a      => state with { ActiveShipPhysics = ShipStateReducer.Reduce(state.ActiveShipPhysics, a) },
        IMiningAction a    => state with { Loop = state.Loop with { Mining = MiningReducer.Reduce(state.Loop.Mining, a) } },
        IInventoryAction a => state with { Loop = state.Loop with { Inventory = InventoryReducer.Reduce(state.Loop.Inventory, a) } },
        IFleetAction a     => state with { Loop = state.Loop with { Fleet = FleetReducer.Reduce(state.Loop.Fleet, a) } },
        ITechAction a      => state with { Loop = state.Loop with { TechTree = TechTreeReducer.Reduce(state.Loop.TechTree, a) } },
        IMarketAction a    => state with { Loop = state.Loop with { Market = MarketReducer.Reduce(state.Loop.Market, a) } },
        IBaseAction a      => state with { Loop = state.Loop with { Base = BaseReducer.Reduce(state.Loop.Base, a) } },
        _ => state
    };
```

**Guarantee**: Composing sub-reducers. Each sub-reducer is independently testable. `IShipAction` routes to ship physics; `IFleetAction` routes to fleet management (Phase 1+). These are separate action hierarchies.

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

## ShipStateReducer (Store-Level)

```
Input:  ShipState × IShipAction → ShipState
```

Routes `IShipAction` subtypes to update the managed `ShipState` in the store. The store-level reducer is a thin projection layer — it does NOT perform physics computation.

| Action | Behavior | Invariants |
|--------|----------|------------|
| `SyncShipPhysicsAction` | Apply ECS-projected position, velocity, rotation, flight mode | Direct copy from ECS canonical data |

### ShipPhysicsMath (Pure Static Functions — Shared)

```
All functions operate on unmanaged types (float3, quaternion, float) for Burst compatibility.
Both ShipPhysicsSystem (ECS/Burst) and unit tests call these directly.
```

| Function | Signature | Invariants |
|----------|-----------|------------|
| `DetermineFlightMode` | `(ShipFlightMode current, ThrustInput thrust, bool hasAlignPoint, bool hasRadialChoice) → ShipFlightMode` | Manual override always wins |
| `ComputeThrust` | `(float3 localForward, float3 localRight, float3 localUp, ThrustInput input, float maxThrust, ShipFlightMode mode) → float3` | World-space force vector |
| `ComputeTorque` | `(float3 localForward, float3 localUp, ThrustInput input, float rotationTorque, ShipFlightMode mode) → float3` | World-space angular velocity delta; roll from input, yaw/pitch from alignment target |
| `ApplyForce` | `(float3 velocity, float3 force, float mass, float dt) → float3` | Zero mass → unchanged |
| `ApplyDamping` | `(float3 velocity, float damping, float dt) → float3` | Non-negative result magnitude |
| `ClampSpeed` | `(float3 velocity, float maxSpeed) → float3` | `length(result) ≤ maxSpeed` |
| `IntegrateRotation` | `(quaternion rotation, float3 angularVelocity, float dt) → quaternion` | Normalized output |

**Guard clauses**: Zero mass → no-op; NaN velocity → clamp to zero.

### ShipPhysicsSystem (ECS-Level, Burst)

```
Input:  ECS components (ShipPositionComponent, ShipVelocityComponent, ShipConfigComponent,
        PilotCommandComponent) × float(deltaTime)
```

The Burst-compiled ISystem reads ECS components, calls `ShipPhysicsMath` static functions directly on unmanaged data, and writes results back to ECS components. Not dispatched through the store.

| Behavior | Invariants |
|----------|------------|
| Reads PilotCommandComponent + ShipConfigComponent | All data is unmanaged IComponentData |
| Calls ShipPhysicsMath functions on component values | Burst-safe — no managed types |
| Writes updated position/velocity/rotation to components | FlightMode transitions correctly |
| EcsToStoreSyncSystem projects results to store via SyncShipPhysicsAction | One-way ECS → Store |

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
CalculateYield(oreId, miningPower, baseYield, hardness, depth, deltaTime) → MiningYieldResult
```
Note: `oreId` is passed through to `MiningYieldResult.OreId` so the caller (ComputeMiningTick) can propagate the ore identity from `state.ActiveOreId`.

---

## InventoryReducer

```
Input:  InventoryState × IInventoryAction → InventoryState
```

| Action | Behavior | Invariants |
|--------|----------|------------|
| `AddResourceAction` | Add qty to stack; update volume | Reject if volume would exceed MaxVolume; reject if new stack would exceed MaxSlots |
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
| `RepairShipAction` | Restore hull integrity | HullIntegrity clamped [0, 1]; ship must be owned |

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
