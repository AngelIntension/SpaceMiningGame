# Changelog

All notable changes to VoidHarvest are documented in this file.

## [Unreleased]

### Spec 005 — Data-Driven Ore System & Asteroid Spawning Refactor

**Migration**: Replaced hard-coded ore types (Veldspar, Scordite, Pyroxeres)
with a fully data-driven ScriptableObject architecture (Luminite, Ferrox,
Auralite).

#### Added
- `OreDefinition` ScriptableObject — configurable ore type with display name,
  rarity tier, base yield, hardness, beam color, cargo volume, ore ID, and
  visual tint. Designers add new ores via Create > VoidHarvest > Ore Definition
  with zero code changes.
- `OreRarityTier` enum (Common, Uncommon, Rare, Epic, Legendary) for future
  UI/loot integration.
- `AsteroidFieldDefinition` ScriptableObject — configurable asteroid field
  with per-field ore weights, visual tint mapping, asteroid count, radius,
  size range, rotation speed, seed, and min scale fraction. Multiple fields
  with distinct compositions supported simultaneously.
- `AsteroidFieldSpawner` authoring component — bakes field definition into
  ECS components for Burst-compiled asteroid generation.
- Three ore assets: Luminite (Common, ice-blue, fast yield, low hardness),
  Ferrox (Uncommon, bronze-orange, medium yield, medium hardness), Auralite
  (Rare, violet, slow yield, high hardness).
- DefaultField.asset — 300-asteroid field with weighted ore distribution
  (60% Luminite, 30% Ferrox, 10% Auralite).
- Addressable asset group `OreDefinitions` for runtime ore loading.

#### Changed
- `OreTypeBlobBakingSystem` now bakes `OreDefinition` ScriptableObjects into
  `OreTypeBlob` BlobAssets (previously baked from `OreTypeDefinition`).
- `AsteroidFieldSystem` reads field config from `AsteroidFieldConfigComponent`
  entities and mesh data from `AsteroidPrefabComponent` singleton (previously
  used hard-coded `AsteroidFieldConfig.MvpDefault`).
- `MiningBeamView` resolves beam colors from `OreDefinition[]` (previously
  from `OreTypeDefinition[]`).
- `OreTypeDatabaseInitializer` references `OreDefinition[]` (previously
  `OreTypeDefinition[]`).
- Player documentation (HOWTOPLAY.md) updated with new ore names, rarity
  tiers, and gameplay characteristics.

#### Removed
- `OreTypeDefinition` ScriptableObject class and all instances.
- `AsteroidFieldConfig` record with hard-coded `MvpDefault` field.
- `AsteroidVisualMappingConfig` ScriptableObject class and instance.
- Legacy ore assets: Veldspar.asset, Scordite.asset, Pyroxeres.asset.
- 4 tests dependent on deleted `AsteroidVisualMappingConfig` type.

#### Test Impact
- 360 tests pass (was 364; 4 removed with deleted types, new tests added for
  OreDefinition validation, blob baking, weight normalization, and field
  definition).
