# Data Model: VoidHarvest Master Vision & Architecture

**Date**: 2026-02-26
**Spec**: `specs/001-master-vision-architecture/spec.md`
**Research**: `specs/001-master-vision-architecture/research.md`

---

## Entity Overview

```
GameState (root)
├── GameLoopState
│   ├── ExploreState
│   ├── MiningSessionState ──→ AsteroidData (via EntityId)
│   ├── InventoryState ──→ ResourceStack[]
│   ├── RefiningState (Phase 2+)
│   ├── TechTreeState ──→ TechNodeStatus[] (Phase 1+)
│   ├── FleetState ──→ OwnedShip[] (Phase 1+)
│   ├── BaseState (Phase 2+)
│   └── MarketState ──→ CommodityMarket[] (Phase 3)
├── ShipState (active ship physics)
├── CameraState
└── WorldState
    ├── AsteroidData[] (ECS canonical — record used for initialization only)
    └── StationData[]
```

---

## Identity Types

```csharp
// Burst-compatible entity identifier. Plain int, incrementing counter.
// Generation counter deferred to future if entity recycling needed.
using EntityId = System.Int32;
```

---

## Core Domain Entities

### Root State

```csharp
public sealed record GameState(
    GameLoopState Loop,
    ShipState ActiveShipPhysics,
    CameraState Camera,
    WorldState World
);

public sealed record StationData(
    EntityId Id,
    float3 Position,
    string Name,
    ImmutableArray<string> AvailableServices  // e.g., "Repair", "Trade"
);

public sealed record WorldState(
    ImmutableArray<AsteroidData> AsteroidField,  // Initialization only; ECS is canonical at runtime
    ImmutableArray<StationData> Stations,
    float WorldTime
);

public sealed record ExploreState(
    Option<EntityId> CurrentFieldId,
    bool ScannerActive
)
{
    public static readonly ExploreState Empty = new(default, false);
}

public sealed record GameLoopState(
    ExploreState Explore,
    MiningSessionState Mining,
    InventoryState Inventory,
    RefiningState Refining,
    TechTreeState TechTree,
    FleetState Fleet,
    BaseState Base,
    MarketState Market
);
```

### Camera

```csharp
public sealed record CameraState(
    float OrbitYaw,           // Horizontal orbit angle (degrees)
    float OrbitPitch,         // Vertical orbit angle, clamped [-80, 80]
    float TargetDistance,     // Desired distance (speed-based zoom)
    bool FreeLookActive,      // Free-look toggle state
    float FreeLookYaw,        // Free-look offset yaw
    float FreeLookPitch       // Free-look offset pitch
);
// Note: Actual camera distance (Cinemachine Radius) is NOT stored in state.
// CameraView handles smooth interpolation from current Cinemachine radius
// toward TargetDistance locally via Mathf.SmoothDamp (view-layer concern).
```

**Validation rules**:
- `OrbitPitch` clamped to [-80, 80]
- `TargetDistance` clamped to [5, 50]
- When `FreeLookActive` toggles off, `FreeLookYaw` and `FreeLookPitch` reset to 0

### Ship (Managed State — Reducer Layer)

```csharp
public sealed record ShipState(
    float3 Position,
    quaternion Rotation,
    float3 Velocity,
    float3 AngularVelocity,
    float Mass,
    float MaxThrust,
    float MaxSpeed,
    float RotationTorque,
    float LinearDamping,
    float AngularDamping,
    ShipFlightMode FlightMode,
    float HullIntegrity      // 0-1, 1.0 = full health
);

public enum ShipFlightMode
{
    Idle,
    ManualThrust,
    AlignToPoint,
    Approach,
    Orbit,
    KeepAtRange,
    Warp           // Phase 1+ — not implemented in MVP
}
```

**State transitions**:
```
Idle ──(manual input)──→ ManualThrust
Idle ──(double-click)──→ AlignToPoint
Idle ──(radial menu)──→ Approach | Orbit | KeepAtRange | Warp
ManualThrust ──(no input)──→ Idle (after velocity → 0)
AlignToPoint ──(aligned + thrust)──→ Approach
Approach ──(distance ≤ RadialDistance)──→ Orbit | KeepAtRange | Idle
Any ──(manual override)──→ ManualThrust
```

### Ship (ECS Components — Simulation Layer)

```csharp
// IComponentData structs — mutable by ECS requirement
// These are the "thin mutable shell" around the pure reducer core

public struct ShipPositionComponent : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
}

public struct ShipVelocityComponent : IComponentData
{
    public float3 Velocity;
    public float3 AngularVelocity;
}

public struct ShipConfigComponent : IComponentData
{
    public float Mass;
    public float MaxThrust;
    public float MaxSpeed;
    public float RotationTorque;
    public float LinearDamping;
    public float AngularDamping;
    public float MiningPower;      // MVP: 1.0f from ShipArchetypeConfig; Phase 1+: computed from modules
}

public struct ShipFlightModeComponent : IComponentData
{
    public ShipFlightMode Mode;
}

public struct PilotCommandComponent : IComponentData
{
    public float Forward;
    public float Strafe;
    public float Roll;
    public int SelectedTargetId;     // -1 = none
    public float3 AlignPoint;
    public bool HasAlignPoint;
    public int RadialAction;         // -1 = none
    public float RadialDistance;
}

// Tag component to identify player-controlled ship
public struct PlayerControlledTag : IComponentData { }
```

### Input Commands

```csharp
public sealed record PilotCommand(
    Option<EntityId> SelectedTarget,
    Option<float3> AlignPoint,
    Option<RadialMenuChoice> RadialChoice,
    ThrustInput ManualThrust,
    ImmutableArray<int> ActivatedModules
);

public readonly struct ThrustInput
{
    public readonly float Forward;   // W/S: [-1, 1]
    public readonly float Strafe;    // A/D: [-1, 1]
    public readonly float Roll;      // Q/E: [-1, 1]

    public ThrustInput(float forward, float strafe, float roll)
    {
        Forward = forward;
        Strafe = strafe;
        Roll = roll;
    }
}

public enum RadialMenuAction
{
    Approach,
    Orbit,
    Mine,
    KeepAtRange,
    Dock,          // Phase 1+ — not shown in MVP radial menu
    Warp           // Phase 1+ — not shown in MVP radial menu
}

public readonly struct RadialMenuChoice
{
    public readonly RadialMenuAction Action;
    public readonly float DistanceMeters;  // Player-configured; required for Approach/Orbit/KeepAtRange

    public RadialMenuChoice(RadialMenuAction action, float distanceMeters)
    {
        Action = action;
        DistanceMeters = distanceMeters;
    }
}
```

**Note**: `ThrustInput` and `RadialMenuChoice` are `readonly struct` (not `record struct`) because C# 9.0 does not support `record struct`. Manual constructors replace positional record syntax.

### Mining

```csharp
public sealed record MiningSessionState(
    Option<EntityId> TargetAsteroidId,
    Option<string> ActiveOreId,
    float BeamEnergy,
    float YieldAccumulator,
    float MiningDuration,
    float BeamMaxRange           // Module-dependent: 50m (T1) to 250m (T4)
)
{
    public static readonly MiningSessionState Empty = new(
        default, default, 0f, 0f, 0f, 50f
    );
}

public sealed record MiningYieldResult(
    string OreId,
    int WholeUnitsYielded,
    float RemainingFraction
);
```

**ECS components for mining simulation**:
```csharp
public struct MiningBeamComponent : IComponentData
{
    public Entity TargetAsteroid;
    public float BeamEnergy;
    public float MiningPower;
    public float MaxRange;
    public bool Active;
}
// Note: Mining yield transport uses NativeQueue<NativeMiningYieldAction> pattern
// (see § NativeQueue Action Structs below), not per-entity buffers.
```

### Asteroids

```csharp
// Initialization / editor data (managed record)
public sealed record AsteroidData(
    EntityId Id,
    float3 Position,
    float Radius,
    ImmutableArray<OreDeposit> Deposits,
    float RemainingMass
);

public readonly struct OreDeposit
{
    public readonly string OreId;
    public readonly float Quantity;
    public readonly float Depth;

    public OreDeposit(string oreId, float quantity, float depth)
    {
        OreId = oreId;
        Quantity = quantity;
        Depth = depth;
    }
}

// ECS components (canonical at runtime)
public struct AsteroidComponent : IComponentData
{
    public float Radius;
    public float InitialMass;      // Set during baking; used for depletion ratio
    public float RemainingMass;
    public float Depletion;       // 0-1, drives shader _Depletion parameter = 1 - (RemainingMass / InitialMass)
}

public struct AsteroidOreComponent : IComponentData
{
    public int OreTypeId;         // Index into OreTypeBlob database
    public float Quantity;
    public float Depth;
}
```

### Asteroid Field Generation

```csharp
public sealed record AsteroidFieldConfig(
    int Seed,
    int MaxAsteroids,              // <500 per Constitution MVP
    float FieldRadius,
    ImmutableArray<OreDistribution> OreDistributions
)
{
    /// <summary>MVP default config: 300 asteroids, 2000m radius, seeded for determinism.</summary>
    public static readonly AsteroidFieldConfig MvpDefault = new(
        Seed: 42,
        MaxAsteroids: 300,
        FieldRadius: 2000f,
        OreDistributions: ImmutableArray.Create(
            new OreDistribution("veldspar",  0.6f, 50f, 200f),
            new OreDistribution("scordite",  0.3f, 30f, 150f),
            new OreDistribution("pyroxeres", 0.1f, 10f, 80f)
        )
    );
}

public readonly struct OreDistribution
{
    public readonly string OreId;
    public readonly float Weight;
    public readonly float MinDepositSize;
    public readonly float MaxDepositSize;

    public OreDistribution(string oreId, float weight, float minDepositSize, float maxDepositSize)
    {
        OreId = oreId;
        Weight = weight;
        MinDepositSize = minDepositSize;
        MaxDepositSize = maxDepositSize;
    }
}
```

### Inventory

```csharp
public sealed record InventoryState(
    ImmutableDictionary<string, ResourceStack> Stacks,
    int MaxSlots,
    float MaxVolume,
    float CurrentVolume
)
{
    public static readonly InventoryState Empty = new(
        ImmutableDictionary<string, ResourceStack>.Empty, 20, 100f, 0f
    );
}

public readonly struct ResourceStack
{
    public readonly string ResourceId;
    public readonly int Quantity;
    public readonly float VolumePerUnit;

    public ResourceStack(string resourceId, int quantity, float volumePerUnit)
    {
        ResourceId = resourceId;
        Quantity = quantity;
        VolumePerUnit = volumePerUnit;
    }
}
```

**Validation rules**:
- `CurrentVolume` must never exceed `MaxVolume`
- `Stacks.Count` must never exceed `MaxSlots`
- Adding resources that would exceed either limit is rejected (reducer returns unchanged state)
- Removing resources with insufficient quantity is rejected

### Fleet (Phase 1+)

```csharp
public sealed record FleetState(
    ImmutableArray<OwnedShip> Ships,
    string ActiveShipId,
    Option<EntityId> DockedAtStation  // Required for swapping
)
{
    /// <summary>MVP stub: single starter ship, not docked.</summary>
    public static readonly FleetState Empty = new(
        ImmutableArray<OwnedShip>.Empty, "", default
    );
}

public sealed record OwnedShip(
    string ShipId,            // Unique instance ID
    string ArchetypeId,
    ImmutableArray<ModuleSlot> EquippedModules,
    float MaxThrust,          // Base + module bonuses (computed)
    float MaxSpeed,           // Base + module bonuses (computed)
    float MiningPower,        // Base + module bonuses (computed)
    float HullIntegrity,      // 0-1
    InventoryState Cargo
);

// C# 9.0 readonly struct (record struct unavailable)
public readonly struct ModuleSlot
{
    public readonly int SlotIndex;
    public readonly Option<string> ModuleId;
    public readonly ModuleType Type;
    public ModuleSlot(int slotIndex, Option<string> moduleId, ModuleType type)
    { SlotIndex = slotIndex; ModuleId = moduleId; Type = type; }
}

public enum ModuleType
{
    MiningLaser, Shield, Scanner, Thruster, Weapon, Utility
}
```

### Tech Tree (Phase 1+)

```csharp
public sealed record TechTreeState(
    ImmutableDictionary<string, TechNodeStatus> Nodes,
    ImmutableArray<string> RecentlyUnlocked  // For UI animation queue
)
{
    public static readonly TechTreeState Empty = new(
        ImmutableDictionary<string, TechNodeStatus>.Empty,
        ImmutableArray<string>.Empty
    );
}

public enum TechNodeStatus
{
    Locked, Available, Researching, Unlocked
}
```

### Refining (Phase 2+)

```csharp
public sealed record RefiningState()
{
    public static readonly RefiningState Empty = new();
}
// Concrete fields TBD in Phase 2 spec
```

### Base (Phase 2+)

```csharp
public sealed record BaseState(
    ImmutableArray<PlacedModule> Modules
)
{
    public static readonly BaseState Empty =
        new(ImmutableArray<PlacedModule>.Empty);
}

public sealed record PlacedModule(
    string ModuleId,
    float3 Position,
    quaternion Rotation,
    string ModuleTypeId
);
```

### Market (Phase 3)

```csharp
public sealed record MarketState(
    ImmutableDictionary<string, CommodityMarket> Commodities,
    float GlobalDemandMultiplier,
    int TickCount
)
{
    public static readonly MarketState Empty = new(
        ImmutableDictionary<string, CommodityMarket>.Empty, 1.0f, 0
    );
}

public sealed record CommodityMarket(
    string CommodityId,
    float BasePrice,
    float CurrentPrice,
    float Supply,
    float Demand,
    float PriceElasticity,
    ImmutableArray<MarketOrder> OpenOrders
);

public sealed record MarketOrder(
    string OrderId,
    string CommodityId,
    OrderSide Side,
    int Quantity,
    float PriceLimit,
    string IssuerId
);

public enum OrderSide { Buy, Sell }
```

---

## ScriptableObject Definitions (Static Data)

```csharp
public enum ShipRole
{
    MiningBarge, Hauler, CombatScout, Explorer, Refinery
}

[CreateAssetMenu(menuName = "VoidHarvest/OreTypeDefinition")]
public class OreTypeDefinition : ScriptableObject
{
    public string OreId;
    public string DisplayName;
    public Color BeamColor;
    public float BaseYieldPerSecond;
    public float Hardness;
    public int Tier;
    public float Rarity;            // 0-1
    public float VolumePerUnit;     // Cargo volume consumed per unit of ore
}

[CreateAssetMenu(menuName = "VoidHarvest/ShipArchetypeConfig")]
public class ShipArchetypeConfig : ScriptableObject
{
    public string ArchetypeId;
    public string DisplayName;
    public ShipRole Role;
    public float Mass;
    public float MaxThrust;
    public float MaxSpeed;
    public float RotationTorque;
    public float LinearDamping;
    public float AngularDamping;
    public float MiningPower;      // MVP: 1.0f for MiningBarge
    public int ModuleSlots;
    public float CargoCapacity;
    public Mesh HullMesh;
    public Material HullMaterial;
}

// Phase 1+ — Tech tree node template
[CreateAssetMenu(menuName = "VoidHarvest/TechNodeDefinition")]
public class TechNodeDefinition : ScriptableObject
{
    public string NodeId;
    public string DisplayName;
    public string Description;
    public int Tier;                                    // 1-4 for Phase 1
    public TechCategory Category;
    public ImmutableArray<string> PrerequisiteNodeIds;  // DAG edges
    public ImmutableArray<TechCost> Costs;
    public ImmutableArray<TechReward> Rewards;
}

public enum TechCategory
{
    Hulls, Mining, Refining, Modules, Economy, Base
}

// C# 9.0 readonly structs (record struct unavailable)
public readonly struct TechCost
{
    public readonly string ResourceId;
    public readonly int Quantity;
    public TechCost(string resourceId, int quantity)
    { ResourceId = resourceId; Quantity = quantity; }
}

public readonly struct TechReward
{
    public readonly TechRewardType Type;
    public readonly string TargetId;
    public readonly float Value;
    public TechReward(TechRewardType type, string targetId, float value)
    { Type = type; TargetId = targetId; Value = value; }
}

public enum TechRewardType
{
    UnlockShip, UnlockModule, StatBoost, UnlockOre, UnlockRecipe
}
```

### BlobAsset Equivalents (for Burst Access)

```csharp
// Baked from OreTypeDefinition at initialization
public struct OreTypeBlob
{
    public float BaseYieldPerSecond;
    public float Hardness;
    public int Tier;
    public float Rarity;
    public float VolumePerUnit;    // Needed by MiningActionDispatchSystem to construct AddResourceAction
}

public struct OreTypeBlobDatabase
{
    public BlobArray<OreTypeBlob> OreTypes;
}

// Singleton component holding the BlobAssetReference
public struct OreTypeDatabaseComponent : IComponentData
{
    public BlobAssetReference<OreTypeBlobDatabase> Database;
}
```

**OreId ↔ OreTypeId Mapping Convention**: The `int OreTypeId` used in ECS components (`AsteroidOreComponent.OreTypeId`, `NativeMiningYieldAction.OreTypeId`) is the **zero-based index** into `OreTypeBlobDatabase.OreTypes` BlobArray. The index order matches the order `OreTypeDefinition` ScriptableObjects are provided to the baking system (T057b). `MiningActionDispatchSystem` converts `OreTypeId` back to `string OreId` by maintaining a parallel `string[]` lookup (populated during baking from `OreTypeDefinition.OreId`).

---

## Action Types (Reducer Inputs)

### Camera Actions
```csharp
public interface ICameraAction { }
public sealed record OrbitAction(float DeltaYaw, float DeltaPitch) : ICameraAction;
public sealed record ZoomAction(float Delta) : ICameraAction;
public sealed record SpeedZoomAction(float NormalizedSpeed) : ICameraAction;
public sealed record ToggleFreeLookAction() : ICameraAction;
public sealed record FreeLookAction(float DeltaYaw, float DeltaPitch) : ICameraAction;
```

### Mining Actions
```csharp
public interface IMiningAction { }
public sealed record BeginMiningAction(EntityId AsteroidId, string OreId) : IMiningAction;
// Value fields instead of ScriptableObject reference — keeps actions pure and self-contained
public sealed record MiningTickAction(float DeltaTime, float BaseYield, float Hardness, float Depth, float ShipMiningPower) : IMiningAction;
// Mapping: BaseYield ← OreTypeDefinition.BaseYieldPerSecond (or OreTypeBlob.BaseYieldPerSecond)
//          ShipMiningPower ← ShipConfigComponent.MiningPower (MVP: 1.0f from ShipArchetypeConfig)
public sealed record StopMiningAction() : IMiningAction;
```

### Inventory Actions
```csharp
public interface IInventoryAction { }
public sealed record AddResourceAction(string ResourceId, int Quantity, float VolumePerUnit) : IInventoryAction;
public sealed record RemoveResourceAction(string ResourceId, int Quantity) : IInventoryAction;
```

### Ship Actions (Physics — routes to state.ActiveShipPhysics)
```csharp
public interface IShipAction { }
// Projected from ECS ShipPhysicsSystem via EcsToStoreSyncSystem
public sealed record SyncShipPhysicsAction(
    float3 Position, quaternion Rotation,
    float3 Velocity, float3 AngularVelocity,
    ShipFlightMode FlightMode) : IShipAction;
```

**Input→Physics pipeline**: `PilotCommand` does NOT dispatch through the store as an `IShipAction`.
Instead, `InputBridge` writes PilotCommand fields directly to the ECS `PilotCommandComponent`
singleton via `EntityManager`. This avoids a managed→unmanaged→managed round-trip and keeps
the physics path fully Burst-compatible. The store-level `ShipStateReducer` only receives
`SyncShipPhysicsAction` (one-way ECS→Store projection for HUD/views).

```
Pipeline: InputBridge → PilotCommandComponent (ECS) → ShipPhysicsMath (pure)
          → ShipPhysicsSystem (Burst) → EcsToStoreSyncSystem
          → SyncShipPhysicsAction → ShipStateReducer → ShipState (for views)
```

### Fleet Actions (Phase 1+ — routes to state.Loop.Fleet)
```csharp
public interface IFleetAction { }
public sealed record SwapShipAction(string TargetShipId) : IFleetAction;
public sealed record EquipModuleAction(string ShipId, int SlotIndex, string ModuleId) : IFleetAction;
public sealed record AcquireShipAction(OwnedShip NewShip) : IFleetAction;
public sealed record RepairShipAction(string ShipId, float Amount) : IFleetAction;
```

### Tech Actions (Phase 1+ — routes to state.Loop.TechTree)
```csharp
public interface ITechAction { }
public sealed record UnlockTechAction(string NodeId, ImmutableDictionary<string, int> AvailableResources) : ITechAction;
public sealed record RefreshAvailabilityAction() : ITechAction;
```

### Market Actions (Phase 3 — routes to state.Loop.Market)
```csharp
public interface IMarketAction { }
public sealed record MarketTickAction() : IMarketAction;
public sealed record PlaceOrderAction(MarketOrder Order) : IMarketAction;
public sealed record FillOrderAction(string OrderId, int Quantity) : IMarketAction;
```

### Base Actions (Phase 2+ — routes to state.Loop.Base)
```csharp
public interface IBaseAction { }
// Stub — concrete action types TBD in Phase 2 spec
```

### Top-Level Game Action
```csharp
public interface IGameAction { }
// ICameraAction, IShipAction, IMiningAction, IInventoryAction,
// IFleetAction, ITechAction, IMarketAction, IBaseAction all extend IGameAction
```

---

## Event Types (EventBus — Cross-System Communication)

```csharp
// All events are readonly struct — zero heap allocation
public readonly struct MiningStartedEvent
{
    public readonly EntityId AsteroidId;
    public readonly string OreId;
    public MiningStartedEvent(EntityId asteroidId, string oreId)
    { AsteroidId = asteroidId; OreId = oreId; }
}

public readonly struct MiningYieldEvent
{
    public readonly string OreId;
    public readonly int Quantity;
    public MiningYieldEvent(string oreId, int quantity)
    { OreId = oreId; Quantity = quantity; }
}

public readonly struct MiningStoppedEvent
{
    public readonly EntityId AsteroidId;
    public readonly StopReason Reason;
    public MiningStoppedEvent(EntityId asteroidId, StopReason reason)
    { AsteroidId = asteroidId; Reason = reason; }
}

public enum StopReason { PlayerStopped, OutOfRange, AsteroidDepleted, CargoFull }

public readonly struct TargetSelectedEvent
{
    public readonly EntityId TargetId;
    public TargetSelectedEvent(EntityId targetId)
    { TargetId = targetId; }
}

public readonly struct StateChangedEvent<T> where T : class
{
    public readonly T PreviousState;
    public readonly T CurrentState;
    public StateChangedEvent(T previous, T current)
    { PreviousState = previous; CurrentState = current; }
}

// Usage: StateChangedEvent<T> is published internally by StateStore
// (via IEventBus) whenever a dispatch produces a new state reference.
// Views can subscribe to StateChangedEvent<GameState> as an alternative
// to StateStore.OnStateChanged callback. Both mechanisms coexist.
```

---

## NativeQueue Action Structs (DOTS → Store Bridge)

```csharp
// Unmanaged action structs for Burst-compatible ECS → Store communication
public struct NativeMiningYieldAction
{
    public Entity SourceAsteroid;
    public int OreTypeId;
    public float Amount;
}

public struct NativeAsteroidDepletedAction
{
    public Entity Asteroid;
}

public struct NativeMiningStopAction
{
    public Entity SourceAsteroid;
    public int Reason;            // Cast from StopReason enum (unmanaged int for Burst)
}

// Singleton component — tag only. The NativeQueue is NOT stored in the component
// (managed type restriction). Instead, MiningBeamSystem creates and owns a
// NativeQueue<NativeMiningYieldAction> with Allocator.Persistent in OnCreate,
// and passes its ParallelWriter to jobs. MiningActionDispatchSystem accesses the
// queue via World.GetExistingSystem<MiningBeamSystem>().
//
// Pattern: MiningBeamSystem.OnCreate allocates the queue and creates a singleton
// entity with MiningActionBufferSingleton as a tag. The queue itself is stored as
// a field on MiningBeamSystem (ISystem state via SystemState/unmanaged field).
// MiningActionDispatchSystem retrieves it via system reference.
public struct MiningActionBufferSingleton : IComponentData { }  // Tag only
```

---

## Relationships Summary

| From | To | Relationship | Cardinality |
|------|-----|-------------|-------------|
| GameState | GameLoopState | Contains | 1:1 |
| GameState | ShipState | Contains (active ship) | 1:1 |
| GameState | CameraState | Contains | 1:1 |
| GameState | WorldState | Contains | 1:1 |
| WorldState | StationData | Contains (array) | 0..N |
| GameLoopState | ExploreState | Contains | 1:1 |
| GameLoopState | InventoryState | Contains | 1:1 |
| GameLoopState | MiningSessionState | Contains | 1:1 |
| MiningSessionState | AsteroidData | References via EntityId | 0..1 |
| InventoryState | ResourceStack | Contains (dictionary) | 0..N |
| FleetState | OwnedShip | Contains (array) | 1..N |
| OwnedShip | ShipArchetypeConfig | References via ArchetypeId | 1:1 |
| OwnedShip | ModuleSlot | Contains (array) | 0..N |
| GameLoopState | TechTreeState | Contains | 1:1 |
| TechTreeState | TechNodeStatus | Contains (dictionary) | 0..N |
| TechNodeDefinition | TechCost | Contains (array) | 0..N |
| TechNodeDefinition | TechReward | Contains (array) | 0..N |
| GameLoopState | BaseState | Contains | 1:1 |
| BaseState | PlacedModule | Contains (array) | 0..N |
| GameLoopState | MarketState | Contains | 1:1 |
| MarketState | CommodityMarket | Contains (dictionary) | 0..N |
| CommodityMarket | MarketOrder | Contains (array) | 0..N |
| AsteroidData | OreDeposit | Contains (array) | 1..N |
| OreDeposit | OreTypeDefinition | References via OreId | 1:1 |

---

## Phase Scope Tags

| Entity | Phase 0 (MVP) | Phase 1 | Phase 2 | Phase 3 |
|--------|:---:|:---:|:---:|:---:|
| CameraState | X | | | |
| ShipState | X | | | |
| PilotCommand | X | | | |
| ExploreState | stub | X | | |
| MiningSessionState | X | | | |
| InventoryState | X | | | |
| AsteroidData | X | | | |
| AsteroidFieldConfig | X | | | |
| StationData | stub | X | | |
| FleetState | stub | X | | |
| OwnedShip | | X | | |
| ModuleSlot | | X | | |
| TechTreeState | stub | X | | |
| TechNodeStatus | | X | | |
| RefiningState | | | X | |
| BaseState | stub | | X | |
| PlacedModule | | | X | |
| MarketState | | | | X |
| CommodityMarket | | | | X |
| MarketOrder | | | | X |
