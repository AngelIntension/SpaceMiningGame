# Research: Data-Driven World Config

**Branch**: `009-data-driven-world-config` | **Date**: 2026-03-03

## R1: Assembly Placement for StationDefinition and WorldDefinition

### Decision
Create two new assembly definitions:
- `VoidHarvest.Features.Station` — contains `StationDefinition`, `StationType` enum, and `StationServicesConfig` (moved from StationServices).
- `VoidHarvest.Features.World` — contains `WorldDefinition`.

### Rationale
`StationDefinition` must reference both `StationPresetConfig` (Features.Base) and `StationServicesConfig` (currently Features.StationServices). Placing them in the same assembly as `StationServicesConfig` would prevent `StationServicesMenuController` from accessing `StationDefinition` without a circular dependency. Moving `StationServicesConfig` to a new lower-level `Features.Station` assembly breaks the cycle:

- `Features.Station` → `Core.Extensions`, `Core.State`, `Features.Base` (for StationPresetConfig)
- `Features.World` → `Core.Extensions`, `Core.State`, `Features.Station`, `Features.Ship` (for ShipArchetypeConfig)
- `Features.StationServices` → adds `Features.Station` (for StationDefinition + moved StationServicesConfig)
- `Features.Targeting` → adds `Features.Station` (for TargetableStation SO reference)

`StationServicesConfig` is a trivial data SO (3 fields, zero dependencies) — moving it is safe.

### Alternatives Considered
1. **Put StationDefinition in Assembly-CSharp** — breaks assembly isolation pattern used by all features.
2. **Use untyped `ScriptableObject` reference** for ServicesConfig — loses Inspector type safety and OnValidate capability.
3. **Put StationDefinition in Features.Base** — would require Features.Base to reference Features.StationServices (creates Base → StationServices → Docking → Base cycle).

---

## R2: DockingConfigBlob Baking Pattern

### Decision
Use the established managed `SystemBase` pattern (same as `OreTypeBlobBakingSystem`), not SubScene authoring+baker.

### Rationale
The ore type blob baking system is the only existing blob baking pattern in the codebase. It uses:
1. Static `Set*()` method called from managed code during initialization
2. `SystemBase` in `InitializationSystemGroup` builds blob in `OnUpdate()`
3. Creates singleton entity with `IComponentData` holding `BlobAssetReference<T>`
4. Self-disables after initialization; disposes in `OnDestroy()`

Following this established pattern ensures consistency and avoids introducing a second baking paradigm.

### Alternatives Considered
1. **SubScene authoring + Baker** — more "standard" DOTS pattern but not established in this codebase; would require a SubScene entity for docking config.
2. **ISystem with managed static** — Burst-incompatible for the baking step; SystemBase is the correct choice for managed code that builds blobs.

---

## R3: DockingConfig Value Mismatches

### Decision
Resolve the value conflict between DockingConfig SO defaults and DockingSystem hard-coded constants by adopting the SO values as canonical, then tuning as needed.

### Findings
| Parameter | DockingConfig SO | DockingSystem Hard-coded | Resolution |
|-----------|-----------------|-------------------------|------------|
| SnapDuration | 1.5f | 0.75f | Use SO value (1.5f) — designer intent |
| SnapRange | 30f | 5f | Use SO value (30f) — designer intent |
| UndockClearanceDistance | 100f | 100f | Match (100f) |
| MaxDockingRange | 500f | (not used) | Keep in SO (500f) |
| UndockDuration | 2f | (not used) | Keep in SO for future use |
| ApproachTimeout | (missing) | 120f | Add to SO with default 120f |
| AlignTimeout | (missing) | 30f | Add to SO with default 30f |
| AlignDotThreshold | (missing) | 0.999f | Add to SO with default 0.999f |
| AlignAngVelThreshold | (missing) | 0.01f | Add to SO with default 0.01f |

### Rationale
The DockingConfig SO was created with intentional values (Spec 004) but the Burst system couldn't read them. The SO values represent the designer's tuned parameters. The hard-coded values were acknowledged as temporary placeholders (code comment: "via blob or hardcoded defaults for Burst compatibility").

---

## R4: CameraReducer Config Injection Strategy

### Decision
Option (A): Embed config limits in `CameraState` record. Add limit fields to `CameraState`, initialize from `CameraConfig` SO at game start. Reducer reads limits from state.

### Rationale
This preserves the pure static `Reduce(CameraState, ICameraAction)` signature unchanged. The reducer reads `state.MinPitch` instead of `const MinPitch`. No changes needed to `CompositeReducer` routing. Config becomes part of the state snapshot, enabling deterministic replay.

### New CameraState Fields
- `MinPitch`, `MaxPitch`, `MinDistance`, `MaxDistance`, `MinZoomDistance`, `MaxZoomDistance`
- `ZoomCooldownDuration` stays separate (view-only concern, injected into CameraView directly)

### Alternatives Considered
1. **Option (B): Change reducer signature** to accept config — requires updating CompositeReducer and every call site.
2. **Injectable CameraReducer service** — requires converting from static class to instance class; breaks established reducer pattern.

---

## R5: StationData Struct — Extend or Retain?

### Decision
Retain the existing `StationData` readonly struct shape. Do not add new fields.

### Rationale
`StationData` in `WorldState` serves as the runtime state representation. Additional StationDefinition fields (Description, StationType, DockingPortOffset, Rotation, Prefab, Icon) are editor/view-layer concerns not needed in the immutable game state. These fields are accessed directly from the `StationDefinition` SO by MonoBehaviours and editor tools.

The mapping `StationData.Id` ↔ `StationDefinition.StationId` provides the bridge when a view needs SO data.

---

## R6: Station ID Fragility Resolution

### Decision
`StationDefinition.StationId` becomes the single source of truth. All other station ID assignments (`DockingPortComponent.StationId`, `TargetableStation.stationId`) derive from the StationDefinition SO reference on the same station GameObject.

### Current Fragility (4 manual sync points)
1. Hard-coded in `RootLifetimeScope.CreateDefaultGameState()` — replaced by WorldDefinition
2. `DockingPortComponent.StationId` inspector field — derived from StationDefinition
3. `TargetableStation.stationId` inspector field — derived from StationDefinition
4. `StationServicesConfigMap.Bindings[]` — eliminated entirely

### Implementation
Each station prefab/GameObject gets a `StationDefinitionRef` component (or the existing MBs read from a shared StationDefinition reference on the same GameObject). `DockingPortComponent` and `TargetableStation` read `.stationId` from the SO in `Awake()`/`Start()` rather than storing a separate serialized int.

---

## R7: DockingPortComponent Duplicate Fields

### Decision
`DockingPortComponent.DockingRange` and `DockingPortComponent.SnapRange` should be removed and derived from the StationDefinition's docking config (or the global DockingConfig SO). Per-station docking overrides are out of scope for this spec.

### Rationale
The DockingSystem already ignores these MonoBehaviour fields (uses hard-coded values). Once the DockingConfigBlob is in place, the system reads from the blob. Per-station overrides can be added in a future spec if needed.

---

## R8: InteractionConfig Scope

### Decision
Only externalize the constants explicitly listed in the spec prompt. Do not migrate inline sensitivity multipliers or UI layout constants.

### Constants to Externalize
- `DoubleClickWindow` (0.3f) — InputBridge
- `RadialMenuDragThreshold` (5f) — InputBridge
- `DefaultApproachDistance` (50f) — RadialMenuController
- `DefaultOrbitDistance` (100f) — RadialMenuController
- `DefaultKeepAtRangeDistance` (50f) — RadialMenuController
- `MiningBeamMaxRange` (currently implicit 50f in InputBridge) — InputBridge

### Constants NOT to Externalize (kept inline)
- Orbit/zoom sensitivity multipliers (0.1f, 2f) — internal tuning, not designer-facing
- UI layout constants (300f, 530f, 150f) — screen dimensions, not gameplay
- Dead zone (0.01f) — internal threshold
- ECS init log frame count (60) — debugging aid

### Rationale
The spec prompt explicitly listed which constants to move. Migrating additional constants would expand scope and risk introducing regressions in well-tuned internal values.
