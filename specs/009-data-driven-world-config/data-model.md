# Data Model: Data-Driven World Config

**Branch**: `009-data-driven-world-config` | **Date**: 2026-03-03

## New Entities

### StationDefinition (ScriptableObject)

Single source of truth for one station's complete configuration.

| Field | Type | Constraints | Section |
|-------|------|-------------|---------|
| StationId | int | > 0, unique across WorldDefinition | Identity |
| DisplayName | string | Non-empty | Identity |
| Description | string (TextArea) | Optional | Identity |
| StationType | StationType (enum) | Required | Identity |
| WorldPosition | Vector3 | Any | World Placement |
| WorldRotation | Quaternion | Normalized | World Placement |
| AvailableServices | string[] | At least one entry | Services |
| ServicesConfig | StationServicesConfig | Non-null | Services |
| PresetConfig | StationPresetConfig | Optional (nullable) | Services |
| DockingPortOffset | Vector3 | Magnitude < 200 | Docking |
| DockingPortRotation | Quaternion | Normalized | Docking |
| SafeUndockDirection | Vector3 | Normalized | Docking |
| Prefab | GameObject | Optional (nullable) | Visuals |
| Icon | Sprite | Optional (nullable) | Visuals |

**OnValidate rules**: StationId > 0, DisplayName non-empty, ServicesConfig non-null, AvailableServices.Length >= 1, DockingPortOffset.magnitude < 200.

**CreateAssetMenu**: `VoidHarvest/Station/Station Definition`

**Assembly**: `VoidHarvest.Features.Station`

### StationType (Enum)

```
MiningRelay, RefineryHub, TradePost, ResearchStation
```

Extensible — new values added as new station types are designed.

**Assembly**: `VoidHarvest.Features.Station`

### WorldDefinition (ScriptableObject)

Defines the complete station roster for a game world plus player starting conditions.

| Field | Type | Constraints |
|-------|------|-------------|
| Stations | StationDefinition[] | All non-null, no duplicate StationIds, Length >= 1 |
| PlayerStartPosition | Vector3 | Any |
| PlayerStartRotation | Quaternion | Normalized |
| StartingShipArchetype | ShipArchetypeConfig | Non-null |

**OnValidate rules**: All Stations non-null, no duplicate StationIds, at least one station, StartingShipArchetype non-null.

**CreateAssetMenu**: `VoidHarvest/World/World Definition`

**Assembly**: `VoidHarvest.Features.World`

### DockingConfigBlob (BlobAsset)

Burst-compatible blob carrying all docking tuning parameters.

| Field | Type | Source (DockingConfig SO) |
|-------|------|--------------------------|
| MaxDockingRange | float | MaxDockingRange |
| SnapRange | float | SnapRange |
| SnapDuration | float | SnapDuration |
| UndockClearanceDistance | float | UndockClearanceDistance |
| UndockDuration | float | UndockDuration |
| ApproachTimeout | float | ApproachTimeout (NEW) |
| AlignTimeout | float | AlignTimeout (NEW) |
| AlignDotThreshold | float | AlignDotThreshold (NEW) |
| AlignAngVelThreshold | float | AlignAngVelThreshold (NEW) |

**Assembly**: `VoidHarvest.Features.Docking`

### DockingConfigBlobComponent (IComponentData, Singleton)

| Field | Type |
|-------|------|
| Value | BlobAssetReference\<DockingConfigBlob\> |

**Assembly**: `VoidHarvest.Features.Docking`

### CameraConfig (ScriptableObject)

| Field | Type | Range/Constraints | Default |
|-------|------|-------------------|---------|
| MinPitch | float | [-89, 0] | -80 |
| MaxPitch | float | [0, 89] | 80 |
| MinDistance | float | > 0 | 5 |
| MaxDistance | float | > MinDistance | 50 |
| MinZoomDistance | float | >= MinDistance | 10 |
| MaxZoomDistance | float | <= MaxDistance | 40 |
| ZoomCooldownDuration | float | >= 0 | 2.0 |
| DefaultYaw | float | Any | 0 |
| DefaultPitch | float | [MinPitch, MaxPitch] | 15 |
| DefaultDistance | float | [MinDistance, MaxDistance] | 25 |
| OrbitSensitivity | float | > 0 | 0.1 |

**OnValidate rules**: MinPitch < 0, MaxPitch > 0, MinDistance > 0, MaxDistance > MinDistance, MinZoomDistance >= MinDistance, MaxZoomDistance <= MaxDistance, ZoomCooldownDuration >= 0, OrbitSensitivity > 0, DefaultPitch within [MinPitch, MaxPitch], DefaultDistance within [MinDistance, MaxDistance].

**CreateAssetMenu**: `VoidHarvest/Camera/Camera Config`

**Assembly**: `VoidHarvest.Features.Camera`

### InteractionConfig (ScriptableObject)

| Field | Type | Range | Default |
|-------|------|-------|---------|
| DoubleClickWindow | float | [0.1, 1.0] | 0.3 |
| RadialMenuDragThreshold | float | [1, 20] | 5 |
| DefaultApproachDistance | float | > 0 | 50 |
| DefaultOrbitDistance | float | > 0 | 100 |
| DefaultKeepAtRangeDistance | float | > 0 | 50 |
| MiningBeamMaxRange | float | > 0 | 50 |

**OnValidate rules**: All values within specified ranges.

**CreateAssetMenu**: `VoidHarvest/Input/Interaction Config`

**Assembly**: `VoidHarvest.Features.Input`

## Modified Entities

### DockingConfig (ScriptableObject) — Extended

New fields added to existing SO (all values currently hard-coded in DockingSystem):

| Field | Type | Default | Status |
|-------|------|---------|--------|
| MaxDockingRange | float | 500 | Existing |
| SnapRange | float | 30 | Existing |
| SnapDuration | float | 1.5 | Existing |
| UndockClearanceDistance | float | 100 | Existing |
| UndockDuration | float | 2 | Existing |
| ApproachTimeout | float | 120 | **NEW** |
| AlignTimeout | float | 30 | **NEW** |
| AlignDotThreshold | float | 0.999 | **NEW** |
| AlignAngVelThreshold | float | 0.01 | **NEW** |

### CameraState (sealed record) — Extended

New limit fields initialized from CameraConfig SO:

| Field | Type | Default | Status |
|-------|------|---------|--------|
| OrbitYaw | float | 0 | Existing |
| OrbitPitch | float | 15 | Existing |
| TargetDistance | float | 25 | Existing |
| FreeLookActive | bool | false | Existing |
| FreeLookYaw | float | 0 | Existing |
| FreeLookPitch | float | 0 | Existing |
| MinPitch | float | -80 | **NEW** |
| MaxPitch | float | 80 | **NEW** |
| MinDistance | float | 5 | **NEW** |
| MaxDistance | float | 50 | **NEW** |
| MinZoomDistance | float | 10 | **NEW** |
| MaxZoomDistance | float | 40 | **NEW** |

### ShipArchetypeConfig (ScriptableObject) — Extended

| Field | Type | Default | Status |
|-------|------|---------|--------|
| CargoSlots | int | 20 | **NEW** |
| ... (17 existing fields) | ... | ... | Existing |

**OnValidate addition**: CargoSlots >= 1.

### InventoryState (sealed record) — Initialization Change

No structural change. `MaxSlots` and `MaxVolume` are derived from `WorldDefinition.StartingShipArchetype` at initialization instead of hard-coded `Empty` defaults.

### StationServicesConfig (ScriptableObject) — Moved

Moved from `VoidHarvest.Features.StationServices` to `VoidHarvest.Features.Station`. No field changes.

## Deleted Entities

### StationServicesConfigMap (ScriptableObject) — Deleted

Replaced entirely by `StationDefinition.ServicesConfig` direct reference. Asset and class file deleted.

## Entity Relationships

```
WorldDefinition
├── Stations[] ──→ StationDefinition (1:N)
│   ├── .ServicesConfig ──→ StationServicesConfig (1:1)
│   └── .PresetConfig ──→ StationPresetConfig (1:1, optional)
└── StartingShipArchetype ──→ ShipArchetypeConfig (1:1)

DockingConfig SO ──[baked by]──→ DockingConfigBlob ──[stored in]──→ DockingConfigBlobComponent (singleton)

CameraConfig SO ──[init]──→ CameraState (limit fields)

InteractionConfig SO ──[injected]──→ InputBridge, RadialMenuController

ShipArchetypeConfig ──[init]──→ InventoryState (MaxSlots, MaxVolume)
```

## Assembly Dependency Changes

### New Assemblies
- `VoidHarvest.Features.Station` → `Core.Extensions`, `Core.State`, `Features.Base`
- `VoidHarvest.Features.World` → `Core.Extensions`, `Core.State`, `Features.Station`, `Features.Ship`
- `VoidHarvest.Features.Station.Tests` → `Features.Station` + test framework
- `VoidHarvest.Features.World.Tests` → `Features.World`, `Features.Station`, `Features.Ship` + test framework

### Modified Assemblies
- `VoidHarvest.Features.StationServices` → add `Features.Station`; remove `StationServicesConfig.cs`
- `VoidHarvest.Features.StationServices.Tests` → add `Features.Station`
- `VoidHarvest.Features.Targeting` → add `Features.Station`
- `VoidHarvest.Features.Targeting.Tests` → add `Features.Station`
- `VoidHarvest.Features.Docking` → add `Features.Station` (DockingPortComponent references StationDefinition for station ID derivation)
- `VoidHarvest.Features.Docking.Tests` → add `Unity.Entities`, `Unity.Collections` (blob baking tests); new blob baking tests
