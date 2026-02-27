# Master Specification: VoidHarvest — Vision & Architecture

**Feature Branch**: `001-master-vision-architecture`
**Created**: 2026-02-26
**Status**: Draft
**Constitution**: `.specify/memory/constitution.md` v1.1.0
**Input**: Master vision and architecture specification for VoidHarvest — 3D space mining simulator with EVE Online-inspired 3rd-person controls.

## Clarifications

### Session 2026-02-26

- Q: What is the mining beam maximum range? → A: Variable by mining laser module tier, ranging from 50m (Tier 1) to 250m (Tier 4). MVP ships use a default Tier 1 laser (50m range).
- Q: Which UI framework for HUD and menus? → A: UI Toolkit exclusively. No uGUI.
- Q: What type is EntityId? → A: `int` (incrementing counter). Burst-compatible, zero-alloc. Generation counter deferred to future if entity recycling needed.
- Q: What ore types for MVP? → A: 3 EVE-inspired ores — Veldspar (common), Scordite (uncommon), Pyroxeres (rare).
- Q: What are the Approach/Orbit/KeepAtRange distances? → A: All player-configurable via radial sub-menu at selection time. No hardcoded defaults — player sets distance when choosing the command.

---

## 1. Game Concept & Target Experience

VoidHarvest is a relaxing-yet-engaging 3D space mining simulator. Players pilot customizable ships from a cinematic 3rd-person perspective, harvest procedural asteroid fields using precision EVE Online-style targeting, manage immutable resource inventories, research branching tech trees, build personal bases, and participate in a fully simulated dynamic economy.

**Target audience**: Players who enjoy the strategic depth of EVE Online's systems but want a relaxing single-player experience without PvP pressure. Fans of satisfying resource loops (Satisfactory, Deep Rock Galactic) in a space setting.

**Emotional targets**:
- **Flow state** during mining — beam targeting, yield feedback, and asteroid depletion visuals create a meditative rhythm
- **Strategic satisfaction** from fleet composition, tech tree choices, and economic decisions
- **Discovery** from procedural asteroid fields with rare ore pockets and environmental hazards
- **Empire pride** from watching a personal base grow and economy flourish

**Core loop**: Explore → Mine → Refine → Expand → Survive

**Performance target**: 60 FPS minimum on mid-range PC (GTX 1060 / RX 580 class), scalable to VR/console later.

---

## 2. Core Gameplay Loop

Each stage of the core loop MUST deliver tactile feedback (visual, audio, UI confirmation) within 2 seconds of player action per Constitution Vision Pillars § Core Loop Satisfaction.

### Loop Stages & Success Metrics

| Stage | Player Action | Feedback (<2s) | Success Metric |
|-------|--------------|----------------|----------------|
| **Explore** | Warp/fly to asteroid field | Field populates visually; scanner overlay shows ore composition | Field renders at 60 FPS with <500 asteroids within 1s |
| **Mine** | Target asteroid, activate mining beam | Beam particle connects; yield numbers stream; asteroid surface erodes | First yield number appears <500ms after beam activation |
| **Refine** (Phase 2+) | Dock at station, initiate refining | Progress bar + particle effect; refined material count increments | Refining feedback begins <200ms after initiation |
| **Expand** (Phase 1+) | Research tech node / place base module | Tech tree node lights up; base module materializes with placement SFX | Visual confirmation <300ms after confirmation input |
| **Survive** (Phase 1+) | Evade hazard / manage hull integrity | Warning indicators, screen shake, hull bar updates | Hazard warning appears >2s before impact |

### Immutable Loop State

> **Canonical definitions**: See `data-model.md` § Root State for all record definitions (GameState, GameLoopState, ExploreState, WorldState, StationData). See `contracts/reducer-interfaces.md` § GameStateReducer for the root routing logic.

All state transitions are pure reducers. The **root** reducer operates on `GameState` (not `GameLoopState`) so it can route actions to all state slices — Camera and ShipState live at root level alongside the loop state. The root reducer delegates `IShipAction` to `ShipStateReducer`, `ICameraAction` to `CameraReducer`, and loop sub-actions to their respective sub-reducers.

> **MVP Note**: Only the **Explore** (field generation) and **Mine** (yield + inventory) stages are functional in Phase 0. `ExploreState.ScannerActive` is a non-functional stub; scanner mechanics are deferred to Phase 1. Refine, Expand, and Survive stages are placeholder entries for the full vision — see § 11 Full Development Phases.

### TDD Strategy — Core Loop

- **Unit tests**: Each sub-reducer tested in isolation with snapshot assertions (`state_before` → `action` → `assert state_after`).
- **Integration tests (PlayMode)**: Full loop cycle — spawn in field, mine one asteroid, verify inventory update, verify HUD reflects change.
- **Performance test**: Measure frame time during loop transitions; assert <2ms per reducer call.

---

## 3. Camera & 3rd-Person Controls

Per Constitution Vision Pillars § Perspective & Camera and § Controls.

### Camera System

**Strictly 3rd-person**. No 1st-person or top-down modes.

#### Immutable Camera Data

> **Canonical definition**: See `data-model.md` § Camera. Reproduced here for reducer context.

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

#### Camera Reducer

```csharp
public static class CameraReducer
{
    public static CameraState Reduce(CameraState state, ICameraAction action)
        => action switch
        {
            OrbitAction a => state with
            {
                OrbitYaw = state.OrbitYaw + a.DeltaYaw,
                OrbitPitch = Math.Clamp(state.OrbitPitch + a.DeltaPitch, -80f, 80f)
            },
            ZoomAction a => state with
            {
                TargetDistance = Math.Clamp(state.TargetDistance + a.Delta, 5f, 50f)
            },
            // Speed-zoom uses narrower band [10, 40] to preserve manual zoom extremes [5, 50]
            SpeedZoomAction a => state with
            {
                TargetDistance = Mathf.Lerp(10f, 40f, a.NormalizedSpeed)
            },
            ToggleFreeLookAction => state with
            {
                FreeLookActive = !state.FreeLookActive,
                FreeLookYaw = 0f,
                FreeLookPitch = 0f
            },
            FreeLookAction a when state.FreeLookActive => state with
            {
                FreeLookYaw = state.FreeLookYaw + a.DeltaYaw,
                FreeLookPitch = Math.Clamp(
                    state.FreeLookPitch + a.DeltaPitch, -80f, 80f)
            },
            _ => state
        };
}
```

#### Unity Integration — Cinemachine

- `CinemachineCamera` component on a dedicated camera rig GameObject.
- `CinemachineOrbitalFollow` for orbit behavior; `CinemachineRotationComposer` for look-at.
- A `CameraView` MonoBehaviour reads the immutable `CameraState` each frame and applies values to Cinemachine parameters. **CameraView NEVER mutates CameraState** — it only reads.
- Speed-based zoom: `CameraView` dispatches `SpeedZoomAction` with `ship.Velocity.magnitude / ship.MaxSpeed` each frame.
- **Zoom priority**: `SpeedZoomAction` is dispatched every frame as a baseline. Manual `ZoomAction` (scroll wheel) overrides by writing directly to `TargetDistance` within the full [5, 50] range. Since both write to the same field, the last-dispatched action wins per frame. In practice, `CameraView` dispatches `SpeedZoomAction` in `LateUpdate`; `InputBridge` dispatches `ZoomAction` in `Update` — so manual zoom is immediately overridden by speed-zoom next frame. To preserve manual zoom, `CameraView` MUST skip `SpeedZoomAction` dispatch for a **2.0 second cooldown** after a manual `ZoomAction` is detected. Implementation detail in T037; PlayMode test must verify cooldown behavior.
- Free-look toggle MUST NOT affect ship heading (Constitution requirement).

#### Input Actions (New Input System)

| Action | Binding | Type |
|--------|---------|------|
| `Camera/Orbit` | Mouse Delta (when RMB held) | Value (Vector2) |
| `Camera/Zoom` | Scroll Wheel | Value (float) |
| `Camera/FreeLookToggle` | Middle Mouse Button | Button |

### EVE-Style Controls

#### Immutable Command Data

> **Canonical definition**: See `data-model.md` § Input Commands. Reproduced here for controls context.

```csharp
public sealed record PilotCommand(
    Option<EntityId> SelectedTarget,      // Left-click selected entity
    Option<float3> AlignPoint,            // Double-click point in 3D space
    Option<RadialMenuChoice> RadialChoice,  // Right-click menu selection (with player-set distance)
    ThrustInput ManualThrust,             // WASD/QE keyboard input
    ImmutableArray<int> ActivatedModules  // Hotbar slots activated this frame
);

// C# 9.0 readonly struct (record struct unavailable)
public readonly struct ThrustInput
{
    public readonly float Forward;   // W/S: [-1, 1]
    public readonly float Strafe;    // A/D: [-1, 1]
    public readonly float Roll;      // Q/E: [-1, 1]
    public ThrustInput(float forward, float strafe, float roll)
    { Forward = forward; Strafe = strafe; Roll = roll; }
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

// Radial menu selection carries a player-specified distance for
// Approach, Orbit, and KeepAtRange (set via radial sub-menu slider).
// C# 9.0 readonly struct (record struct unavailable)
public readonly struct RadialMenuChoice
{
    public readonly RadialMenuAction Action;
    public readonly float DistanceMeters;  // Player-configured; required for Approach/Orbit/KeepAtRange
    public RadialMenuChoice(RadialMenuAction action, float distanceMeters)
    { Action = action; DistanceMeters = distanceMeters; }
}
```

#### Input → PilotCommand Flow

1. `InputBridge` (MonoBehaviour) reads Unity Input System callbacks.
2. `InputBridge` constructs an immutable `PilotCommand` record each tick.
3. `InputBridge` writes `PilotCommand` fields to the ECS `PilotCommandComponent` singleton via `EntityManager` (documented ECS mutable shell deviation).
4. `ShipPhysicsSystem` (Burst) reads `PilotCommandComponent`, calls `ShipPhysicsMath` pure functions, writes physics results to ECS components.
5. `EcsToStoreSyncSystem` dispatches `SyncShipPhysicsAction` to `ShipStateReducer` (pure) for HUD/view consumption.
6. **No game-state mutation from input handlers** — `InputBridge` only writes to the ECS input component (thin mutable shell). All physics logic is pure via `ShipPhysicsMath`.

#### Input Actions (New Input System)

| Action | Binding | Type |
|--------|---------|------|
| `Player/Select` | Left Mouse Button | Button |
| `Player/DoubleClickAlign` | Left Mouse Button (double-tap) | Button |
| `Player/RadialMenu` | Right Mouse Button | Button |
| `Player/Thrust` | W/S | Value (float) |
| `Player/Strafe` | A/D | Value (float) |
| `Player/Roll` | Q/E | Value (float) |
| `Player/Hotbar1-8` | 1-8 keys | Button |
| `Player/MousePosition` | Mouse Position | Value (Vector2) |

#### Radial Context Menu

- Right-click on a target entity opens a radial pie menu.
- Menu options are contextual: asteroid shows (Approach, Orbit, Mine, Keep-at-Range); station shows (Approach, Dock). **MVP Note**: No station entities are spawned in Phase 0 — only asteroid radial options are functional. Station interactions are Phase 1+.
- Selecting Approach, Orbit, or Keep-at-Range opens a distance sub-menu (slider or preset buttons) where the player sets the desired range in meters before confirming. Distance sub-menu range: **10m–500m** with 10m step increments. **Initial slider position** (not hardcoded physics defaults — player always explicitly confirms): 50m for Approach, 100m for Orbit, 50m for Keep-at-Range. Slider is continuous; preset buttons at 25m, 50m, 100m, 250m, 500m. The ship uses whatever distance the player confirms; there are no hardcoded distances at the physics/flight layer.
- Selection produces a `RadialMenuChoice` (action + player-specified distance) in the `PilotCommand`.
- HUD renders the radial menu and distance sub-menu via **UI Toolkit** — **view layer only**.

### TDD Strategy — Camera & Controls

- **Unit tests**: `CameraReducer` — orbit clamping, zoom bounds, free-look toggle isolation, speed-zoom interpolation.
- **Unit tests**: `PilotCommand` construction — verify immutability, verify all fields populated from mock input.
- **Integration (PlayMode)**: Mouse drag produces orbit; double-click produces `AlignPoint`; radial menu opens on right-click; free-look does not change ship heading.

---

## 4. Ship Movement & Physics

Per Constitution Principles § I, III, IV — functional state, DOTS performance, data-oriented design.

### Immutable Ship Data

> **Canonical definition**: See `data-model.md` § Ship. Reproduced here for reducer context.

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
    float LinearDamping,     // Simulates drag for game-feel
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

### Ship Physics — Architecture

> **Architecture note**: The pure physics functions below are defined in `ShipPhysicsMath` — a static class with all methods operating on unmanaged types (`float3`, `quaternion`, `float`) for Burst compatibility. The store-level `ShipStateReducer` only handles `SyncShipPhysicsAction` (projecting ECS results into the store). Unit tests call `ShipPhysicsMath` directly. See `contracts/reducer-interfaces.md` § ShipPhysicsMath for the full function table.

```csharp
// ShipPhysicsMath — pure static class, all methods operate on unmanaged types
// Called by both ShipPhysicsSystem (ECS/Burst) and unit tests
public static class ShipPhysicsMath
{
    public static ShipFlightMode DetermineFlightMode(
        ShipFlightMode current, ThrustInput thrust,
        bool hasAlignPoint, bool hasRadialChoice) => ...;
    public static float3 ComputeThrust(
        float3 localForward, float3 localRight, float3 localUp,
        ThrustInput input, float maxThrust, ShipFlightMode mode) => ...;
    public static float3 ComputeTorque(
        float3 localForward, float3 localUp,
        ThrustInput input, float rotationTorque, ShipFlightMode mode) => ...;
    public static float3 ApplyForce(
        float3 vel, float3 force, float mass, float dt) => ...;
    public static float3 ApplyDamping(
        float3 vel, float damping, float dt) => ...;
    public static float3 ClampSpeed(float3 vel, float max) => ...;
    public static quaternion IntegrateRotation(
        quaternion rot, float3 angVel, float dt) => ...;
}

// ShipStateReducer — store-level, handles only SyncShipPhysicsAction
public static class ShipStateReducer
{
    public static ShipState Reduce(ShipState state, IShipAction action)
        => action switch
        {
            SyncShipPhysicsAction a => state with
            {
                Position = a.Position,
                Rotation = a.Rotation,
                Velocity = a.Velocity,
                AngularVelocity = a.AngularVelocity,
                FlightMode = a.FlightMode
            },
            _ => state
        };
}
```

### DOTS/ECS Integration

- `ShipPhysicsSystem` (ISystem, Burst-compiled) runs in `SimulationSystemGroup`.
- ECS components mirror the immutable record for cache-friendly layout:
  - `ShipPositionComponent` (IComponentData): Position, Rotation
  - `ShipVelocityComponent` (IComponentData): Velocity, AngularVelocity
  - `ShipConfigComponent` (IComponentData): Mass, MaxThrust, MaxSpeed, damping
  - `ShipFlightModeComponent` (IComponentData): current FlightMode
  - `PilotCommandComponent` (IComponentData): current frame's PilotCommand
- The System reads `PilotCommandComponent` + `ShipConfigComponent`, calls pure reducer logic (Burst-compatible static methods), writes new position/velocity.
- **CONSTITUTION DEVIATION note**: ECS components are mutable structs by Unity requirement. The reducer logic itself remains pure — the System is the only write point (thin mutable shell around pure core).

### 6DOF Newtonian Model

- Forward/reverse thrust along ship's local Z-axis.
- Strafe along local X-axis.
- Roll around local Z-axis.
- Linear damping simulates "space drag" for game-feel (not realistic but satisfying).
- No gravity in asteroid fields (micro-gravity zones near large bodies in future phases).
- Align-to-point: ship auto-rotates toward target point, then applies forward thrust.

### TDD Strategy — Ship Movement

- **Unit tests**: `ShipPhysicsMath` — ComputeThrust with forward input produces force along local Z; ApplyForce increases velocity proportional to force/mass; ApplyDamping reduces velocity over time; ClampSpeed enforces max speed; ComputeTorque produces angular velocity from roll input; IntegrateRotation normalizes output; DetermineFlightMode resolves AlignToPoint, Approach, Orbit, KeepAtRange correctly.
- **Unit tests**: `ShipPhysicsMath` edge cases — zero mass guard (ApplyForce returns unchanged); NaN velocity guard (clamp to zero); simultaneous manual + auto-pilot (manual overrides via DetermineFlightMode).
- **Unit tests**: `ShipStateReducer` — SyncShipPhysicsAction copies all fields correctly; unknown action returns unchanged state.
- **Performance tests**: 500 ships simulated via Burst job — assert <2ms total frame time.

---

## 5. Mining, Resources & Inventory

Per Constitution Principles § I (immutable), § III (Burst), § IV (data-oriented).

### Ore Types & Asteroid Data

> **Canonical definitions**: See `data-model.md` § Mining, § Asteroids, § Inventory, § Asteroid Field Generation for all record/struct definitions. Reproduced here for reducer context.

```csharp
// ScriptableObject — designer-authored, single source of truth
[CreateAssetMenu(menuName = "VoidHarvest/OreTypeDefinition")]
public class OreTypeDefinition : ScriptableObject
{
    public string OreId;
    public string DisplayName;
    public Color BeamColor;
    public float BaseYieldPerSecond;
    public float Hardness;          // Affects mining time
    public int Tier;                // Tech tier required to mine
    public float Rarity;            // 0-1, affects procedural placement
    public float VolumePerUnit;     // Cargo volume consumed per unit of ore
}

// MVP ore set (3 ScriptableObject assets):
//   Veldspar  — common,   Rarity=0.6, Hardness=1.0, Tier=1, BeamColor=tan
//   Scordite  — uncommon, Rarity=0.3, Hardness=1.5, Tier=1, BeamColor=amber
//   Pyroxeres — rare,     Rarity=0.1, Hardness=2.5, Tier=1, BeamColor=crimson

// Runtime immutable data (float3 for Burst/DOTS compatibility)
public sealed record AsteroidData(
    EntityId Id,
    float3 Position,
    float Radius,
    ImmutableArray<OreDeposit> Deposits,
    float RemainingMass          // 0 = fully depleted
);

// C# 9.0 readonly struct (record struct unavailable)
public readonly struct OreDeposit
{
    public readonly string OreId;
    public readonly float Quantity;
    public readonly float Depth;  // Distance from surface — affects yield
    public OreDeposit(string oreId, float quantity, float depth)
    { OreId = oreId; Quantity = quantity; Depth = depth; }
}
```

### Mining Session

```csharp
public sealed record MiningSessionState(
    Option<EntityId> TargetAsteroidId,
    Option<string> ActiveOreId,
    float BeamEnergy,            // Current beam charge level
    float YieldAccumulator,      // Fractional yield not yet added to inventory
    float MiningDuration,        // Time mining current target
    float BeamMaxRange           // Module-dependent: 50m (T1) to 250m (T4)
);

// MVP Mining Power: In Phase 0, the ship has a fixed MiningPower of 1.0f
// (hardcoded in ShipArchetypeConfig for the MiningBarge starter ship).
// MiningBeamSystem reads this from ShipConfigComponent. In Phase 1+,
// MiningPower becomes a computed stat from OwnedShip (base + module bonuses).

public sealed record MiningYieldResult(
    string OreId,
    int WholeUnitsYielded,
    float RemainingFraction
);
```

### Mining Reducer

```csharp
public static class MiningReducer
{
    public static MiningSessionState Reduce(
        MiningSessionState state, IMiningAction action)
        => action switch
        {
            BeginMiningAction a => state with
            {
                TargetAsteroidId = Option<EntityId>.Some(a.AsteroidId),
                ActiveOreId = Option<string>.Some(a.OreId),
                BeamEnergy = 1.0f,
                YieldAccumulator = 0f,
                MiningDuration = 0f
            },
            MiningTickAction a => ComputeMiningTick(
                state, a.DeltaTime, a.BaseYield, a.Hardness, a.Depth, a.ShipMiningPower),
            // Note: MiningTickAction.BaseYield is populated from OreTypeDefinition.BaseYieldPerSecond
            // (or equivalently, OreTypeBlob.BaseYieldPerSecond in Burst context).
            // MiningTickAction.ShipMiningPower comes from ShipConfigComponent.MiningPower (MVP: 1.0f).
            StopMiningAction => MiningSessionState.Empty,
            _ => state
        };

    // Pure yield calculation
    public static MiningYieldResult CalculateYield(
        string oreId,
        float miningPower,
        float baseYield,
        float hardness,
        float depth,
        float deltaTime)
    {
        var effectiveYield = (miningPower * baseYield)
            / (hardness * (1f + depth));
        var rawYield = effectiveYield * deltaTime;
        var whole = (int)Math.Floor(rawYield);
        return new MiningYieldResult(oreId, whole, rawYield - whole);
    }
}
```

### Inventory (Immutable Stacks)

> **Canonical definition**: See `data-model.md` § Inventory. Reproduced here for reducer context.

```csharp
public sealed record InventoryState(
    ImmutableDictionary<string, ResourceStack> Stacks,
    int MaxSlots,
    float MaxVolume,
    float CurrentVolume
);

// C# 9.0 readonly struct (record struct unavailable)
public readonly struct ResourceStack
{
    public readonly string ResourceId;
    public readonly int Quantity;
    public readonly float VolumePerUnit;
    public ResourceStack(string resourceId, int quantity, float volumePerUnit)
    { ResourceId = resourceId; Quantity = quantity; VolumePerUnit = volumePerUnit; }
}

public static class InventoryReducer
{
    public static InventoryState Reduce(
        InventoryState state, IInventoryAction action)
        => action switch
        {
            AddResourceAction a => AddResource(
                state, a.ResourceId, a.Quantity, a.VolumePerUnit),
            RemoveResourceAction a => RemoveResource(
                state, a.ResourceId, a.Quantity),
            // TransferAction deferred to Phase 2 (hauling roles)
            _ => state
        };

    private static InventoryState AddResource(
        InventoryState state, string id, int qty, float volPerUnit)
    {
        var newVolume = state.CurrentVolume + (qty * volPerUnit);
        if (newVolume > state.MaxVolume) return state; // Over capacity

        // C# 9: readonly struct doesn't support `with` — use constructor
        ResourceStack newStack;
        if (state.Stacks.TryGetValue(id, out var existing))
        {
            newStack = new ResourceStack(id, existing.Quantity + qty, volPerUnit);
        }
        else
        {
            if (state.Stacks.Count >= state.MaxSlots) return state; // Slot limit
            newStack = new ResourceStack(id, qty, volPerUnit);
        }

        return state with
        {
            Stacks = state.Stacks.SetItem(id, newStack),
            CurrentVolume = newVolume
        };
    }

    private static InventoryState RemoveResource(
        InventoryState state, string id, int qty)
    {
        if (!state.Stacks.TryGetValue(id, out var stack)
            || stack.Quantity < qty)
            return state; // Insufficient quantity

        var newQty = stack.Quantity - qty;
        // C# 9: readonly struct doesn't support `with` — use constructor
        var newStacks = newQty == 0
            ? state.Stacks.Remove(id)
            : state.Stacks.SetItem(id,
                new ResourceStack(id, newQty, stack.VolumePerUnit));

        return state with
        {
            Stacks = newStacks,
            CurrentVolume = state.CurrentVolume - (qty * stack.VolumePerUnit)
        };
    }
}
```

### Procedural Asteroid Field (Burst)

> **Canonical definition**: See `data-model.md` § Asteroid Field Generation. Reproduced here for generation context.

```csharp
public sealed record AsteroidFieldConfig(
    int Seed,
    int MaxAsteroids,           // <500 per Constitution MVP
    float FieldRadius,
    ImmutableArray<OreDistribution> OreDistributions
);

// C# 9.0 readonly struct (record struct unavailable)
public readonly struct OreDistribution
{
    public readonly string OreId;
    public readonly float Weight;           // Relative spawn probability
    public readonly float MinDepositSize;
    public readonly float MaxDepositSize;
    public OreDistribution(string oreId, float weight, float minDepositSize, float maxDepositSize)
    { OreId = oreId; Weight = weight; MinDepositSize = minDepositSize; MaxDepositSize = maxDepositSize; }
}
```

- **Generation**: `AsteroidFieldGeneratorJob` (IJobParallelFor, Burst-compiled) generates positions via Poisson disc sampling with seeded RNG (`Unity.Mathematics.Random`).
- **Ore assignment**: Second Burst job assigns deposits based on weighted distribution and noise-based clustering (rare ores cluster in "veins").
- **Rendering**: Entities Graphics renders asteroid meshes with LOD switching in a dedicated System.
- **Depletion visuals**: As `RemainingMass` decreases, a shader parameter (`_Depletion`) drives surface erosion effect.

### TDD Strategy — Mining & Inventory

- **Unit tests**: `MiningReducer.CalculateYield` — hardness reduces yield; depth reduces yield; mining power scales linearly; zero hardness guard.
- **Unit tests**: `InventoryReducer` — add increases quantity and volume; add rejects over capacity; remove decreases quantity; remove rejects insufficient stock; remove last stack clears entry.
- **Unit tests**: `AsteroidFieldGeneratorJob` — same seed produces same field; respects MaxAsteroids; all positions within FieldRadius; ore distributions match weights within statistical tolerance.
- **Performance tests**: Field generation <500 asteroids completes in <100ms via Burst.

---

## 6. Research/Tech Tree & Progression (Phase 1+)

Per Constitution Vision Pillars § Progression — immutable DAG, ScriptableObject-authored.

### Tech Tree Data Model

```csharp
// ScriptableObject — designer-authored
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
    public readonly string TargetId;  // e.g., ShipArchetype ID, OreType ID
    public readonly float Value;      // e.g., +15% mining yield
    public TechReward(TechRewardType type, string targetId, float value)
    { Type = type; TargetId = targetId; Value = value; }
}

public enum TechRewardType
{
    UnlockShip, UnlockModule, StatBoost, UnlockOre, UnlockRecipe
}
```

### Runtime State

```csharp
public sealed record TechTreeState(
    ImmutableDictionary<string, TechNodeStatus> Nodes,
    ImmutableArray<string> RecentlyUnlocked  // For UI animation queue
);

public enum TechNodeStatus
{
    Locked, Available, Researching, Unlocked
}

public static class TechTreeReducer
{
    public static TechTreeState Reduce(
        TechTreeState state, ITechAction action)
        => action switch
        {
            UnlockTechAction a => TryUnlock(
                state, a.NodeId, a.AvailableResources),
            RefreshAvailabilityAction => RefreshAvailable(state),
            _ => state
        };

    private static TechTreeState TryUnlock(
        TechTreeState state,
        string nodeId,
        ImmutableDictionary<string, int> resources)
    {
        if (state.Nodes[nodeId] != TechNodeStatus.Available)
            return state;
        // Cost validation is pure
        return state with
        {
            Nodes = state.Nodes.SetItem(
                nodeId, TechNodeStatus.Unlocked),
            RecentlyUnlocked = state.RecentlyUnlocked.Add(nodeId)
        };
    }

    // Recompute Available status based on DAG prerequisite edges
    private static TechTreeState RefreshAvailable(
        TechTreeState state) => ...;
}
```

### TDD Strategy — Tech Tree

- **Unit tests**: DAG validation — no cycles; all prerequisites exist; tier ordering consistent.
- **Unit tests**: `TechTreeReducer` — unlock succeeds when prerequisites met and resources sufficient; unlock fails when prerequisites unmet; unlock fails when resources insufficient; refresh correctly marks newly available nodes.
- **Unit tests**: Reward application — stat boosts compose correctly; unlock rewards are idempotent.

---

## 7. Ship Fleet & Swapping System (Phase 1+)

Per Constitution Vision Pillars § Ship Fleet System — immutable records, pure reducer swap.

### Fleet Data Model

```csharp
// ScriptableObject — designer-authored ship templates
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

public enum ShipRole
{
    MiningBarge, Hauler, CombatScout, Explorer, Refinery
}

// Runtime immutable ship instance
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

// Fleet state
public sealed record FleetState(
    ImmutableArray<OwnedShip> Ships,
    string ActiveShipId,
    Option<EntityId> DockedAtStation  // Required for swapping
);
```

### Fleet Reducer

```csharp
public static class FleetReducer
{
    public static FleetState Reduce(
        FleetState state, IFleetAction action)
        => action switch
        {
            SwapShipAction a => TrySwap(state, a.TargetShipId),
            EquipModuleAction a => EquipModule(
                state, a.ShipId, a.SlotIndex, a.ModuleId),
            RepairShipAction a => Repair(state, a.ShipId, a.Amount),
            AcquireShipAction a => state with
            {
                Ships = state.Ships.Add(a.NewShip)
            },
            _ => state
        };

    private static FleetState TrySwap(
        FleetState state, string targetId)
    {
        if (state.DockedAtStation.IsNone) return state;
        if (state.Ships.All(s => s.ShipId != targetId)) return state;
        return state with { ActiveShipId = targetId };
    }
}
```

### Unity Integration

- Active ship swap updates:
  1. Camera target entity changes → `CameraView` re-targets Cinemachine.
  2. HUD hotbar refreshes with new ship's module layout.
  3. ECS entity archetype swap: disable old ship rendering, enable new.
- Ship meshes loaded via Addressables keyed by `ArchetypeId`.

### TDD Strategy — Fleet

- **Unit tests**: `FleetReducer` — swap succeeds when docked; swap fails when undocked; swap fails for unowned ship; equip module updates stats; acquire ship adds to fleet.
- **Unit tests**: Stats recomputation — base stats + module bonuses compose correctly.

---

## 8. Base Building (Phase 2+, MVP Stub)

### MVP Stub (Phase 0)

No base building in MVP. The `BaseState` record exists as a placeholder for forward-compatible `GameLoopState` schema:

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

### Phase 2 — Full Base System

- Player places base modules in asteroid belts or Lagrange points.
- Modules: Storage Silo, Refinery, Hangar, Research Lab, Trading Post.
- Placement uses grid-snapping in 3D space relative to an anchor asteroid.
- All placement is a pure reducer: `(BaseState, PlaceModuleAction) → BaseState`.
- Base modules are immutable positional data within world state (Constitution requirement).
- Base data authored via `BaseModuleDefinition` ScriptableObjects.

### TDD Strategy — Base

- **Unit tests (stub)**: `BaseState.Empty` is valid; adding a module produces new state with module present.
- **Unit tests (Phase 2)**: Grid-snap calculation; collision detection between modules; module dependency validation (Refinery requires Storage Silo).

---

## 9. Simulated Economy (Phase 3)

Per Constitution Vision Pillars § Endgame — deterministic pure simulation on immutable `MarketState`.

### Market Data Model

```csharp
public sealed record MarketState(
    ImmutableDictionary<string, CommodityMarket> Commodities,
    float GlobalDemandMultiplier,
    int TickCount                    // Simulation tick counter
);

public sealed record CommodityMarket(
    string CommodityId,
    float BasePrice,
    float CurrentPrice,
    float Supply,                    // Units available
    float Demand,                    // Units demanded per tick
    float PriceElasticity,          // Price response to imbalance
    ImmutableArray<MarketOrder> OpenOrders
);

public sealed record MarketOrder(
    string OrderId,
    string CommodityId,
    OrderSide Side,                 // Buy or Sell
    int Quantity,
    float PriceLimit,
    string IssuerId                 // Player or NPC ID
);

public enum OrderSide { Buy, Sell }
```

### Market Reducer

```csharp
public static class MarketReducer
{
    public static MarketState Reduce(
        MarketState state, IMarketAction action)
        => action switch
        {
            MarketTickAction => SimulateTick(state),
            PlaceOrderAction a => PlaceOrder(state, a.Order),
            FillOrderAction a => FillOrder(
                state, a.OrderId, a.Quantity),
            _ => state
        };

    // Deterministic price resolution based on supply/demand
    private static MarketState SimulateTick(MarketState state)
    {
        var updated = state.Commodities.ToBuilder();
        foreach (var (id, market) in state.Commodities)
        {
            var ratio = market.Supply
                / Math.Max(market.Demand, 0.001f);
            var priceAdjust = (1f / ratio)
                * market.PriceElasticity;
            var newPrice = Math.Clamp(
                market.BasePrice * priceAdjust,
                market.BasePrice * 0.1f,
                market.BasePrice * 10f);
            updated[id] = market with { CurrentPrice = newPrice };
        }
        return state with
        {
            Commodities = updated.ToImmutable(),
            TickCount = state.TickCount + 1
        };
    }
}
```

### TDD Strategy — Economy

- **Unit tests**: `MarketReducer.SimulateTick` — high supply/low demand drives price down; low supply/high demand drives price up; price stays within 0.1x–10x base; deterministic (same state + same tick = same output).
- **Unit tests**: Order placement — buy order reduces player funds; sell order reduces player inventory; order matching at price limits.
- **Determinism test**: Run 10,000 ticks from identical seed state — assert final prices match bit-for-bit.

---

## 10. Technical Architecture & Data Model

### Core Identity Type

```csharp
// EntityId is a plain int (incrementing counter). Burst-compatible, zero-alloc.
// Generation counter may be added later if entity recycling requires it.
using EntityId = System.Int32;
```

### Root Game State

> **Canonical definitions**: See `data-model.md` § Root State for all record definitions (GameState, GameLoopState, ExploreState, WorldState, StationData). Definitions are not repeated here to avoid drift.

### Reducer Architecture

The root `GameStateReducer` operates on `GameState` (not `GameLoopState`), routing actions to the correct state slice. Camera and Ship physics live at the root level; loop sub-systems (mining, inventory, fleet, tech, market) are nested under `GameLoopState`.

```
InputBridge (MonoBehaviour — thin mutable shell)
  │
  ├──→ PilotCommand (immutable record)
  │      │
  │      ▼ ECS PilotCommandComponent (via EntityManager — documented ECS deviation)
  │      │
  │      ▼ ShipPhysicsSystem (Burst) → calls ShipPhysicsMath (pure static)
  │      │
  │      ▼ EcsToStoreSyncSystem → dispatches SyncShipPhysicsAction
  │
  ├──→ ICameraAction (dispatched to StateStore)
  │
  └──→ IMiningAction / IInventoryAction (via ECS NativeQueue → ActionDispatchSystem)

GameStateReducer (pure static — operates on GameState root)
  ├── CameraReducer           → state.Camera
  ├── ShipStateReducer        → state.ActiveShipPhysics (SyncShipPhysicsAction only)
  ├── MiningReducer           → state.Loop.Mining
  ├── InventoryReducer        → state.Loop.Inventory
  ├── FleetReducer (Phase 1+) → state.Loop.Fleet
  ├── TechTreeReducer (Phase 1+) → state.Loop.TechTree
  ├── MarketReducer (Phase 3) → state.Loop.Market
  └── BaseReducer (Phase 2+)  → state.Loop.Base  [IBaseAction/BaseReducer TBD in Phase 2 spec]
  │
  ▼ GameState (new immutable record)
  │
Views (MonoBehaviours — read-only, render state)
  ├── ShipView → applies position/rotation to Transform
  ├── CameraView → applies orbit to Cinemachine
  ├── HUDView → updates UI elements
  └── MiningBeamView → particle system parameters
```

### DOTS + Entities Graphics + Burst

| System | Layer | Burst | Purpose |
|--------|-------|-------|---------|
| `ShipPhysicsSystem` | DOTS | Yes | 6DOF movement, velocity integration |
| `AsteroidFieldSystem` | DOTS | Yes | Procedural generation, LOD management |
| `MiningBeamSystem` | DOTS | Yes | Beam targeting, yield calculation per tick |
| `ResourceEntitySystem` | DOTS | Yes | Floating resource pickup entities (Phase 1+ — not in MVP scope) |
| `AsteroidDepletionSystem` | DOTS | Yes | Mass tracking, visual depletion |
| `CameraSystem` | Hybrid | No | Reads CameraState, writes Cinemachine |
| `HUDSystem` | Hybrid | No | Reads GameState, writes UI |
| `InputSystem` | Hybrid | No | Reads Input System, produces PilotCommand |

### EventBus (UniTask)

> See `contracts/eventbus-interface.md` for the canonical `IEventBus` interface definition, guarantees, and DOTS-to-Managed bridge contract. See `data-model.md` § Event Types for all event struct definitions.

Key architectural points:
- EventBus bridges DOTS and MonoBehaviour layers via NativeQueue drain pattern.
- Systems publish events; Views subscribe and update visuals.
- UniTask `Channel<T>` provides allocation-free async enumeration for `struct` events.
- All events are `readonly struct` — no heap allocation, no boxing.

### Package Manifest (Required Installations)

| Package | Source | Purpose |
|---------|--------|---------|
| `com.unity.entities` | Unity Registry | DOTS ECS runtime |
| `com.unity.entities.graphics` | Unity Registry | Entities Graphics (hybrid rendering) |
| `com.unity.burst` | Unity Registry | Burst compiler (likely already present) |
| `com.unity.collections` | Unity Registry | NativeArray, NativeHashMap (likely present) |
| `com.unity.cinemachine` | Unity Registry | Camera system |
| `com.unity.addressables` | Unity Registry | Runtime asset loading |
| `com.unity.inputsystem` | Unity Registry | New Input System (already present) |
| `com.cysharp.unitask` | OpenUPM / git | Allocation-free async, EventBus |
| `jp.hadashikick.vcontainer` | OpenUPM / git | Lightweight DI container |
| `System.Collections.Immutable` | NuGetForUnity | ImmutableArray, ImmutableDictionary |

### C# Language Level (Confirmed: C# 9.0)

Unity 6 (6000.3.10f1) supports **C# 9.0** / .NET Framework 4.7.1 (Mono runtime). `record struct` (C# 10+) is NOT available. Architectural adaptations:
- Use `sealed record` (reference type) for domain state in the reducer layer
- Use `readonly struct` with manual constructors for Burst-compatible value types
- Use `readonly struct` for event types (zero-alloc, no boxing)

### Dependency Injection (VContainer)

```csharp
// RootLifetimeScope — DontDestroyOnLoad, core infrastructure only
public class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Core infrastructure
        builder.Register<IEventBus, UniTaskEventBus>(Lifetime.Singleton);
        builder.Register<IStateStore, StateStore>(Lifetime.Singleton);

        // Reducers (stateless — singleton is fine)
        builder.Register<GameStateReducer>(Lifetime.Singleton);
    }
}

// SceneLifetimeScope — child of Root, per-scene view bindings
public class SceneLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Views (MonoBehaviour — resolved from scene hierarchy)
        builder.RegisterComponentInHierarchy<ShipView>();
        builder.RegisterComponentInHierarchy<CameraView>();
        builder.RegisterComponentInHierarchy<HUDView>();
        builder.RegisterComponentInHierarchy<InputBridge>();
    }
}
```

---

## 11. MVP Scope & Acceptance Criteria + Full Development Phases

### Phase 0 — MVP Acceptance Criteria

The MVP is complete when ALL of the following are true:

| ID | Criterion | Verification |
|----|-----------|-------------|
| MVP-01 | Player can fly a ship in 3rd-person with 6DOF Newtonian physics | PlayMode test: WASD/QE produces expected velocity; ship drifts with inertia |
| MVP-02 | Camera orbits ship smoothly with speed-based zoom | PlayMode test: distance increases with speed; orbit drag works |
| MVP-03 | Left-click selects asteroid; double-click aligns ship toward it | PlayMode test: PilotCommand contains correct SelectedTarget and AlignPoint |
| MVP-04 | Right-click on asteroid opens radial menu with Mine option | PlayMode test: RadialMenuChoice.Mine dispatched; menu renders |
| MVP-05 | Mining beam connects to asteroid; yield numbers appear <500ms | PlayMode test: MiningYieldEvent fires within 500ms of activation |
| MVP-06 | Mined resources appear in inventory with correct quantities | Unit test: InventoryReducer.AddResource produces expected state |
| MVP-07 | Asteroid visually depletes as resources are extracted | PlayMode test: _Depletion shader param increases as mass decreases |
| MVP-08 | Procedural field generates <500 asteroids at 60 FPS | Perf test: field gen <100ms; steady-state 60 FPS |
| MVP-09 | HUD shows resource counts, velocity, hull, mining target | PlayMode test: elements update within 1 frame of state change |
| MVP-10 | All pure reducers have 100% unit test coverage | Code coverage report via Unity Test Framework |
| MVP-11 | Zero GC allocations in gameplay hot loops | Unity Profiler deep profile: 0 GC in steady-state |
| MVP-12 | All state changes via immutable reducers | Code review + Roslyn analyzer |

### User Scenarios & Testing

#### US1 — First Flight (Priority: P1)

The player spawns in a ship floating near an asteroid field. They can look around with the orbiting camera (mouse drag), accelerate/brake (W/S), strafe (A/D), roll (Q/E), and experience Newtonian drift.

**Why P1**: Without flight, nothing else works. Validates the core input → reducer → physics → render pipeline.

**Independent Test**: Launch scene, verify ship responds to all 6DOF inputs, camera follows correctly.

**Acceptance Scenarios**:
1. **Given** player in ship at rest, **When** W pressed 2s then released, **Then** ship continues drifting (Newtonian inertia)
2. **Given** player in ship, **When** mouse dragged while orbiting, **Then** camera orbits without affecting ship heading
3. **Given** player at speed, **When** camera observes, **Then** zoom level has pulled back proportionally

---

#### US2 — Target & Approach (Priority: P2)

The player can left-click an asteroid to select it (highlighted), double-click to align and fly toward it, and right-click for a radial context menu.

**Why P2**: Targeting bridges flight and mining — the EVE-style interaction model.

**Independent Test**: Click asteroid, verify highlight; double-click, verify alignment; right-click, verify menu.

**Acceptance Scenarios**:
1. **Given** asteroid in view, **When** left-click, **Then** highlighted with target info on HUD
2. **Given** selected asteroid, **When** double-click, **Then** ship rotates toward it and accelerates
3. **Given** selected asteroid, **When** right-click, **Then** radial menu with Approach, Orbit, Mine, Keep-at-Range

---

#### US3 — Mine & Collect (Priority: P3)

The player selects "Mine" from the radial menu or activates mining laser via hotbar. A beam connects, yield numbers stream, resources accumulate in inventory on HUD.

**Why P3**: Completes core loop stages Explore + Mine. Validates mining reducer + inventory reducer integration.

**Independent Test**: Activate mining, verify resources in inventory, verify asteroid depletion.

**Acceptance Scenarios**:
1. **Given** ship in mining range, **When** Mine activated, **Then** beam VFX connects to asteroid surface
2. **Given** active mining beam, **When** 5s elapse, **Then** inventory shows mined ore quantity > 0
3. **Given** asteroid with limited resources, **When** fully depleted, **Then** beam stops, full depletion visual

---

#### US4 — Procedural Field & HUD (Priority: P4)

The player enters a procedurally generated asteroid field with varied ore compositions. HUD displays resource counts, velocity, hull integrity, mining target info.

**Why P4**: Validates procedural generation and HUD as primary information channel.

**Independent Test**: Load scene, verify field variety; verify HUD real-time updates.

**Acceptance Scenarios**:
1. **Given** new game session, **When** field generates, **Then** <500 asteroids with 3+ ore types
2. **Given** player flying, **When** speed changes, **Then** HUD velocity updates within 1 frame
3. **Given** same seed, **When** field generates twice, **Then** positions and ores identical

### Edge Cases

- **Mining at max distance**: Beam disconnects when ship exceeds `BeamMaxRange` (50–250m by module tier; MVP default 50m). Mining stops with "Out of Range" HUD feedback.
- **Inventory full**: Mining stops when cargo is full. InventoryReducer returns unchanged state (rejection), MiningActionDispatchSystem detects the rejection (state unchanged after AddResourceAction), dispatches `StopMiningAction` to reset `MiningSessionState` to Empty, and publishes `MiningStoppedEvent(StopReason.CargoFull)` via EventBus. HUDView subscribes and displays a "Cargo Full" warning indicator.
- **Zero-mass asteroid**: Guard against division by zero in depletion calculations.
- **Rapid target switching**: PilotCommand handles rapid clicks without race conditions (immutable by design).
- **Camera at orbit limits**: Pitch clamped [-80, 80]; zoom clamped [5, 50].

### Full Development Phases

| Phase | Scope | Key Systems | Exit Criteria |
|-------|-------|-------------|---------------|
| **0 — MVP** | Camera, controls, flight, mining, inventory, procedural field, HUD | ShipStateReducer, CameraReducer, MiningReducer, InventoryReducer, AsteroidFieldGeneratorJob | All MVP-01 through MVP-12 pass |
| **1 — Fleet & Tech** | Ship swapping, tech tree (3-4 tiers), module equipment | FleetReducer, TechTreeReducer, ModuleStatsComposer | Player owns/swaps ships, unlocks tech |
| **2 — Refining & Bases** | Ore refining, hauling, base placement | RefiningReducer, BaseReducer, CargoTransferReducer | Full mine→refine→store; bases placeable |
| **3 — Economy & Endgame** | Dynamic market, deep base customization, fleet management | MarketReducer, EconomyTickSystem, NPCTraderSystem | Deterministic prices; bases customizable |

All phases MUST preserve the functional/immutable core per Constitution § Governance.

---

## Constitution Compliance Checklist (v1.1.0)

| # | Principle | Status | Evidence |
|---|-----------|--------|----------|
| 1 | Functional & Immutable First | PASS | All state is `record` types; changes via pure reducers; `with` expressions; no mutable domain data |
| 2 | Predictability & Testability | PASS | Every system: `InputState → PureFunction → NewState`; no static mutable state; all deps injected |
| 3 | Performance by Default | PASS | DOTS/ECS + Burst for simulation; 60 FPS target explicit; zero GC alloc in MVP criteria |
| 4 | Data-Oriented Design | PASS | ECS archetypes; ScriptableObjects for static data; no inheritance hierarchies; composition only |
| 5 | Modularity & Extensibility | PASS | Feature-per-folder; assembly definitions; EventBus communication; no cross-feature direct writes |
| 6 | Explicit Over Implicit | PASS | VContainer explicit registration; no reflection DI; all data flow traceable |
| 7 | Camera (3rd-person only) | PASS | No 1st-person mode; free-look isolated from heading; speed-based zoom specified |
| 8 | Controls (EVE-style) | PASS | PilotCommand with mouse targeting, radial menu, hotbar, keyboard thrust |
| 9 | Ship Fleet (immutable swap) | PASS | OwnedShip is sealed record; FleetReducer.TrySwap is pure; dock required |
| 10 | Tech Tree (immutable DAG) | PASS | ScriptableObject-authored; ImmutableDictionary state; pure unlock reducer |
| 11 | Core Loop (<2s feedback) | PASS | <500ms mining yield, <200ms refining, <300ms tech unlock |
| 12 | Economy (deterministic) | PASS | MarketReducer.SimulateTick pure; same input = same output |
| 13 | TDD mandatory | PASS | TDD strategy per system; MVP-10 requires 100% reducer coverage |
| 14 | MVP scope respected | PASS | Phase 1-3 stubbed; explicit out-of-scope list |

---

## Implementation Notes

### 1. Install Pending Packages

Edit `Packages/manifest.json` or use Package Manager:

```json
{
  "dependencies": {
    "com.unity.entities": "1.3.x",
    "com.unity.entities.graphics": "1.3.x",
    "com.unity.cinemachine": "3.1.x",
    "com.unity.addressables": "2.3.x"
  },
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "jp.hadashikick",
        "com.cysharp",
        "com.github-glitchenzo"
      ]
    }
  ]
}
```

Install NuGetForUnity (`com.github-glitchenzo.nugetforunity`) via the OpenUPM scoped registry above, then install `System.Collections.Immutable` via NuGetForUnity UI.

### 2. Create InputSystem_Actions Asset

Replace or update `InputSystem_Actions.inputactions` at project root:
- **Player** map: Select, DoubleClickAlign, RadialMenu, Thrust, Strafe, Roll, Hotbar1-8, MousePosition
- **Camera** map: Orbit, Zoom, FreeLookToggle
- **UI** map: Navigate, Submit, Cancel (default Unity UI)

### 3. C# Language Level (Confirmed: C# 9.0)

Unity 6 (6000.3.10f1) supports C# 9.0 / .NET Framework 4.7.1. `record struct` (C# 10+) is NOT available. Use `readonly struct` with manual equality for value types. `record` (reference type) for domain state. No `csc.rsp` changes needed.

### 4. Initial Scene Setup

1. Create `Assets/Scenes/GameScene.unity`
2. Add: `GameManager` (LifetimeScope), `InputBridge`, `CameraRig` (Cinemachine), `HUDDocument` (UIDocument for UI Toolkit)
3. Create empty feature folders per constitution project structure
4. Add `.asmdef` files for each feature folder
5. Configure URP for space (dark skybox, bloom post-processing)

### Assumptions

- Single-player only for all phases (multiplayer out of scope per constitution)
- No persistent save system in MVP — session-only state
- Placeholder audio in MVP
- VR/console scalability is future concern, not actively targeted in MVP
- NPC traders in Phase 3 are simple scripted agents, not ML-driven
- Warp flight mode mechanics deferred to Phase 1 (enum value present for forward compatibility)
- Dock interaction and station docking deferred to Phase 1 (enum value present for forward compatibility)
- ResourceEntitySystem (floating resource pickups) deferred to Phase 1
