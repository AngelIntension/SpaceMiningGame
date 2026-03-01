# Contract: OreDefinition ScriptableObject

**Feature**: 005-data-driven-ore
**Date**: 2026-03-01

## Purpose

OreDefinition is the public data contract for all ore types in VoidHarvest. Any system that needs ore properties (mining, inventory, HUD, VFX, future economy/tech tree) reads from OreDefinition instances.

## Interface

### ScriptableObject Fields (Inspector-Editable)

```
OreDefinition : ScriptableObject
├── OreId           : string          — Unique identifier (lowercase, no spaces)
├── DisplayName     : string          — Human-readable name for UI
├── RarityTier      : OreRarityTier   — Enum: Common | Uncommon | Rare
├── Icon            : Sprite          — Nullable, for future inventory UI
├── BaseValue       : float           — Base market price (>= 0)
├── Description     : string          — Flavor text (TextArea)
├── RarityWeight    : float           — Default spawn weight [0, 1]
├── BaseYieldPerSecond : float        — Mining yield rate (> 0)
├── Hardness        : float           — Extraction difficulty (> 0)
├── VolumePerUnit   : float           — Cargo space per unit (> 0)
├── BeamColor       : Color           — Mining laser color
└── BaseProcessingTimePerUnit : float — Refining time in seconds (> 0)
```

### Create Menu

`Create > VoidHarvest > Ore Definition`

### Asset Location

`Assets/Features/Mining/Data/Ores/`

## Consumers

| System | Fields Used | Access Pattern |
|--------|-------------|----------------|
| OreTypeBlobBakingSystem | BaseYieldPerSecond, Hardness, VolumePerUnit | Baked into BlobAsset at init |
| MiningBeamView | OreId, BeamColor | Array lookup by OreId string |
| OreTypeDatabaseInitializer | All (passes array to baking system) | Serialized Inspector reference |
| MiningActionDispatchSystem | (indirect via OreId string from blob system) | GetOreId() reverse lookup |
| HUD TargetInfoPanel | DisplayName (via string passthrough) | Receives string, no direct SO ref |
| OreFieldEntry | Reference to entire SO | Serialized in AsteroidFieldDefinition |

## Invariants

1. `OreId` MUST be unique across all OreDefinition instances in the project.
2. `OreId` MUST be lowercase, alphanumeric, no spaces (e.g., "luminite", "ferrox").
3. `BaseYieldPerSecond`, `Hardness`, and `VolumePerUnit` MUST be > 0 (zero values produce degenerate mining behavior).
4. `BeamColor` alpha channel is ignored by the beam renderer (always full opacity).
5. `Icon`, `BaseValue`, `Description`, and `BaseProcessingTimePerUnit` have no runtime consumers in this spec — stored for future use.

## Versioning

This contract is introduced in Spec 005. Changes to field names or types require a spec amendment. Adding new fields is non-breaking.
