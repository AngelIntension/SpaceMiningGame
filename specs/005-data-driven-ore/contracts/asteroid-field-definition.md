# Contract: AsteroidFieldDefinition ScriptableObject

**Feature**: 005-data-driven-ore
**Date**: 2026-03-01

## Purpose

AsteroidFieldDefinition is the public data contract for configuring asteroid fields in VoidHarvest. Each instance defines a complete asteroid field: which ores appear, their relative frequencies, spatial distribution, and visual appearance. Designers create fields entirely in the Editor without code changes.

## Interface

### ScriptableObject Fields (Inspector-Editable)

```
AsteroidFieldDefinition : ScriptableObject
├── FieldName          : string           — Human-readable identifier
├── OreEntries         : OreFieldEntry[]  — Ore composition with visuals
├── AsteroidCount      : int              — Total asteroids to spawn (> 0)
├── FieldRadius        : float            — Spherical radius in meters (> 0)
├── AsteroidSizeMin    : float            — Min asteroid radius (> 0)
├── AsteroidSizeMax    : float            — Max asteroid radius (>= SizeMin)
├── RotationSpeedMin   : float            — Min rotation deg/s (>= 0)
├── RotationSpeedMax   : float            — Max rotation deg/s (>= 0)
├── Seed               : uint             — Deterministic RNG seed
└── MinScaleFraction   : float            — Minimum scale at full depletion [0.1, 0.5]

OreFieldEntry (Serializable Struct)
├── OreDefinition      : OreDefinition    — Reference to ore type SO
├── Weight             : float            — Relative spawn weight (> 0)
├── MeshVariantA       : Mesh             — First mesh variant
├── MeshVariantB       : Mesh             — Second mesh variant
└── TintColor          : Color            — Ore-specific asteroid tint
```

### Create Menu

`Create > VoidHarvest > Asteroid Field Definition`

### Asset Location

`Assets/Features/Procedural/Data/Fields/`

## Consumers

| System | Fields Used | Access Pattern |
|--------|-------------|----------------|
| AsteroidFieldSpawner (Baker) | All fields | Baked into ECS components at SubScene bake |
| AsteroidFieldGeneratorJob | Count, Radius, Seed, Weights | Burst job reads NativeArrays |
| AsteroidFieldSystem | All (via baked components) | Creates asteroid entities |
| AsteroidPrefabAuthoring (Baker) | MeshVariants (via spawner) | Creates mesh prefab entities |

## Weight Normalization Contract

Ore entry weights are **automatically normalized** at spawn time:

1. Collect all `OreFieldEntry` entries where `OreDefinition != null` and `Weight > 0`.
2. Sum all valid weights: `totalWeight = sum(entry.Weight)`.
3. Each entry's probability = `entry.Weight / totalWeight`.
4. If `totalWeight == 0` or no valid entries exist, log warning and spawn zero asteroids.

**Example**: Weights (7, 2, 1) → Probabilities (0.7, 0.2, 0.1).

Designers can use any positive numbers — the system normalizes to probabilities.

## Invariants

1. `OreEntries` MUST contain at least one valid entry (non-null OreDefinition, Weight > 0) for asteroids to spawn.
2. `AsteroidCount` MUST be > 0 for any spawning to occur.
3. `FieldRadius` MUST be > 0.
4. `AsteroidSizeMin` MUST be <= `AsteroidSizeMax`. If violated, values are swapped with a warning.
5. `Seed` value `0` is valid — it produces a specific deterministic layout.
6. `MinScaleFraction` defaults to 0.3 and is clamped to [0.1, 0.5].
7. Same `Seed` + same parameters MUST produce identical asteroid field layout (determinism guarantee).
8. Null `OreDefinition` entries are silently skipped during weight normalization.

## Determinism Guarantee

Given identical `AsteroidFieldDefinition` values:
- Same `Seed` → identical asteroid positions
- Same `Seed` → identical ore type assignments
- Same `OreEntries` order + weights → identical distribution

This is enforced by the Burst-compiled `AsteroidFieldGeneratorJob` using per-index seeded RNG: `seed = FieldSeed + asteroidIndex + 1`.

## Versioning

This contract is introduced in Spec 005. Changes to field names or types require a spec amendment. Adding new fields is non-breaking.
