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

public sealed record WorldState(
    ImmutableArray<AsteroidData> AsteroidField,  // Initialization only; ECS is canonical at runtime
    ImmutableArray<StationData> Stations,
    float WorldTime
);

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
    float Distance,           // Current distance from ship
    float TargetDistance,     // Desired distance (speed-based zoom)
    bool FreeLookActive,      // Free-look toggle state
    float FreeLookYaw,        // Free-look offset yaw
    float FreeLookPitch       // Free-look offset pitch
);
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
    ShipFlightMode FlightMode
);

public enum ShipFlightMode
{
    Idle,
    ManualThrust,
    AlignToPoint,
    Approach,
    Orbit,
    KeepAtRange,
    Warp
}
```

**State transitions**:
```
Idle ──(manual input)──→ ManualThrust
Idle ──(double-click)──→ AlignToPoint
Idle ──(radial menu)──→ Approach | Orbit | KeepAtRange | Warp
ManualThrust ──(no input)──→ Idle (after velocity → 0)
AlignToPoint ──(aligned + thrust)──→ Approach
Approach ──(within range)──→ Orbit | KeepAtRange | Idle
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
    Dock,
    Warp
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

public struct MiningYieldBufferElement : IBufferElementData
{
    public int OreTypeId;
    public float Amount;
}
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
    public float RemainingMass;
    public float Depletion;       // 0-1, drives shader _Depletion parameter
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
);

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

---

## ScriptableObject Definitions (Static Data)

```csharp
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
    public int ModuleSlots;
    public float CargoCapacity;
    public Mesh HullMesh;
    public Material HullMaterial;
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
public sealed record MiningTickAction(float DeltaTime, OreTypeDefinition OreDefinition, float ShipMiningPower) : IMiningAction;
public sealed record StopMiningAction() : IMiningAction;
```

### Inventory Actions
```csharp
public interface IInventoryAction { }
public sealed record AddResourceAction(string ResourceId, int Quantity, float VolumePerUnit) : IInventoryAction;
public sealed record RemoveResourceAction(string ResourceId, int Quantity) : IInventoryAction;
```

### Ship Actions
```csharp
public interface IShipAction { }
public sealed record ApplyThrustAction(ThrustInput Thrust) : IShipAction;
public sealed record AlignToPointAction(float3 Target) : IShipAction;
public sealed record SetFlightModeAction(ShipFlightMode Mode) : IShipAction;
```

### Top-Level Game Action
```csharp
public interface IGameAction { }
// IMiningAction, IInventoryAction, IShipAction, ICameraAction all extend IGameAction
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

// Singleton component holding the action buffer
public struct MiningActionBufferSingleton : IComponentData
{
    // Allocated with Allocator.Persistent in system OnCreate
}
```

---

## Relationships Summary

| From | To | Relationship | Cardinality |
|------|-----|-------------|-------------|
| GameState | GameLoopState | Contains | 1:1 |
| GameState | ShipState | Contains (active ship) | 1:1 |
| GameState | CameraState | Contains | 1:1 |
| GameLoopState | InventoryState | Contains | 1:1 |
| GameLoopState | MiningSessionState | Contains | 1:1 |
| MiningSessionState | AsteroidData | References via EntityId | 0..1 |
| InventoryState | ResourceStack | Contains (dictionary) | 0..N |
| FleetState | OwnedShip | Contains (array) | 1..N |
| OwnedShip | ShipArchetypeConfig | References via ArchetypeId | 1:1 |
| AsteroidData | OreDeposit | Contains (array) | 1..N |
| OreDeposit | OreTypeDefinition | References via OreId | 1:1 |

---

## Phase Scope Tags

| Entity | Phase 0 (MVP) | Phase 1 | Phase 2 | Phase 3 |
|--------|:---:|:---:|:---:|:---:|
| CameraState | X | | | |
| ShipState | X | | | |
| PilotCommand | X | | | |
| MiningSessionState | X | | | |
| InventoryState | X | | | |
| AsteroidData | X | | | |
| AsteroidFieldConfig | X | | | |
| FleetState | stub | X | | |
| TechTreeState | stub | X | | |
| RefiningState | | | X | |
| BaseState | stub | | X | |
| MarketState | | | | X |
