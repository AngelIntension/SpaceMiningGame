# Contract: ITargetable Interface & TargetInfo

**Feature**: 007-target-lock-system
**Date**: 2026-03-02

## Purpose

`ITargetable` is the cross-cutting contract for any MonoBehaviour-based object that can be selected and locked by the player's targeting system. `TargetInfo` is the shared readonly struct that unifies data from both `ITargetable` MonoBehaviours and ECS entity queries into a single display format.

## ITargetable Interface (Core/Extensions)

```
ITargetable (interface)
├── TargetId: int              — Unique identifier (typically GameObject.GetInstanceID())
├── DisplayName: string        — Human-readable name for reticle/card display
├── TypeLabel: string          — Category label (e.g., "Station", "Luminite")
└── TargetType: TargetType     — Enum discriminator (Asteroid, Station, etc.)
```

### Location

`Assets/Core/Extensions/ITargetable.cs`

### Assembly

`VoidHarvest.Core.Extensions` — zero dependencies, referenced by all feature assemblies.

### Implementors

| Type | Assembly | Source of Data |
|------|----------|----------------|
| `TargetableStation` | Features.Targeting | `StationData` via `WorldState.Stations` matched by `StationId` |
| *(future)* `TargetableNPC` | Features.NPC | NPC definition SO |
| *(future)* `TargetableDebris` | Features.Procedural | Debris component |
| *(future)* `TargetableCargoPod` | Features.Logistics | Cargo pod data |

### Non-Implementors (ECS Entities)

ECS entities (asteroids) cannot implement C# interfaces. Instead, asteroid `TargetInfo` is constructed via the static factory method `TargetInfo.FromAsteroid(int entityIndex, string displayName, string oreTypeName)` using data from `AsteroidComponent` + `AsteroidOreComponent` + `OreDisplayNames`.

## TargetInfo Readonly Struct (Core/Extensions)

```
TargetInfo (readonly struct)
├── TargetId: int              — Unique identifier
├── DisplayName: string        — Name for reticle/card display
├── TypeLabel: string          — Type for reticle/card display
├── TargetType: TargetType     — Asteroid or Station
├── IsValid: bool              — Derived: TargetId >= 0
└── static None: TargetInfo    — Sentinel for "no target" (TargetId = -1)
```

### Construction Paths

| Source | Factory Method | Example |
|--------|---------------|---------|
| ITargetable MonoBehaviour | `TargetInfo.From(ITargetable target)` | Station selected via Physics raycast |
| ECS asteroid entity | `TargetInfo.FromAsteroid(int entityIndex, string displayName, string oreTypeName)` | Asteroid selected via ECS ray-sphere |

### Location

`Assets/Core/Extensions/TargetInfo.cs`

## Consumers

| System | Fields Used | Access Pattern |
|--------|-------------|----------------|
| InputBridge | All (constructs TargetInfo from selection) | Physics raycast → `ITargetable` check → `TargetInfo.From()` |
| TargetingReducer | TargetId, TargetType, DisplayName, TypeLabel | Receives via action records |
| ReticleView | DisplayName, TypeLabel | Reads from TargetingState.Selection |
| TargetCardView | DisplayName, TypeLabel | Reads from TargetLockData |
| TargetPreviewManager | TargetId, TargetType | Finds object for clone creation |

## Invariants

1. `TargetId` MUST be unique across all targetable objects in the scene at any given time. MonoBehaviours use `GetInstanceID()`; ECS entities use entity index.
2. `DisplayName` MUST NOT be null or empty — use a fallback like "Unknown" if data is unavailable.
3. `TargetInfo.None` MUST be used as the sentinel value for "no target" — never use `default(TargetInfo)`.
4. `ITargetable` implementations MUST be placed on the same GameObject that has a Collider (for Physics raycast hit detection).
5. `TargetType` enum values MUST match between `ITargetable.TargetType` and `TargetInfo.TargetType` — the factory methods enforce this.

## Versioning

This contract is introduced in Spec 007. Changes to `ITargetable` method signatures require a spec amendment. Adding new properties to `ITargetable` is a breaking change (all implementors must update). Adding new `TargetType` enum values is non-breaking for existing code but may require UI updates.
