# Research: Data-Driven Ore System & Asteroid Spawning Refactor

**Feature**: 005-data-driven-ore
**Date**: 2026-03-01

## Decision 1: OreDefinition Replaces OreTypeDefinition

**Decision**: Create a new `OreDefinition` ScriptableObject class that extends the field set of the legacy `OreTypeDefinition`, then delete the legacy type after migration.

**Rationale**: The spec requires new fields (RarityTier enum, Icon, BaseValue, Description, BaseProcessingTimePerUnit) that don't exist on the legacy type. A clean replacement avoids carrying dead fields (like `Tier` int) and renames ambiguous ones (`Rarity` → `RarityWeight`). Creating a new type also enables a clean Create Asset menu path (`Create > VoidHarvest > Ore Definition`).

**Alternatives Considered**:
- **Modify OreTypeDefinition in-place**: Rejected — would preserve the "OreType" prefix naming which doesn't match the spec's "OreDefinition" requirement, and would require careful field migration of existing .asset files with potential GUID breaks.
- **Inherit from OreTypeDefinition**: Rejected — inheritance contradicts Constitution Principle IV (composition over inheritance) and creates unnecessary coupling.

## Decision 2: AsteroidFieldDefinition Replaces AsteroidFieldConfig

**Decision**: Create a new `AsteroidFieldDefinition` ScriptableObject to replace the immutable record `AsteroidFieldConfig`. The SO is the single source of truth for all field parameters.

**Rationale**: The legacy `AsteroidFieldConfig` is a C# record with a hard-coded `MvpDefault` static field. This makes it impossible for designers to create new field configurations without code changes. A ScriptableObject enables Editor-based authoring while maintaining immutability at runtime (SOs are read-only during play).

**Alternatives Considered**:
- **Keep record + add SO wrapper**: Rejected — unnecessary indirection. The SO directly holds the data that was in the record.
- **JSON/YAML configuration files**: Rejected — ScriptableObjects are the project's standard for static data (Constitution Principle IV), provide Inspector editing, and integrate with Unity's asset pipeline.

## Decision 3: Visual Mapping Absorbed into OreFieldEntry

**Decision**: Embed mesh variant references and tint colors directly in `OreFieldEntry` (a serializable struct within `AsteroidFieldDefinition`), eliminating the separate `AsteroidVisualMappingConfig` ScriptableObject.

**Rationale**: The current system has a separate `AsteroidVisualMappingConfig` SO that maps OreId strings to mesh variants and tint colors. By embedding this data in `OreFieldEntry`, each `AsteroidFieldDefinition` is fully self-contained — one asset defines everything needed to spawn a field. This also enables different fields to have different visual styles for the same ore type (future belt variety).

**Alternatives Considered**:
- **Keep separate visual mapping SO**: Rejected — adds an extra asset that must be kept in sync with the field definition. The spec requires "visual variants" as part of the field definition.
- **Put visual data on OreDefinition itself**: Rejected — ore visual appearance in an asteroid field is a field-level concern (different belts may use different asteroid meshes), not an ore-level concern. OreDefinition holds gameplay data (yield, hardness); visual mapping is spatial.

## Decision 4: OreRarityTier Enum

**Decision**: Create a simple `OreRarityTier` enum with values: `Common`, `Uncommon`, `Rare`.

**Rationale**: The spec defines three rarity tiers matching the three initial ores. An enum provides type-safe categorization for future UI display (color-coding, sorting) and tech tree gating (Phase 1+). It replaces the legacy `Tier` int field which was less descriptive.

**Alternatives Considered**:
- **Keep int Tier**: Rejected — the spec explicitly asks for named rarity tiers (Common/Uncommon/Rare), not numeric tiers.
- **String-based rarity**: Rejected — enums provide compile-time safety and better Inspector dropdowns.

## Decision 5: AsteroidFieldSpawner Authoring Component

**Decision**: Create an `AsteroidFieldSpawner` authoring MonoBehaviour that references an `AsteroidFieldDefinition` and bakes spawn configuration into ECS. Place it in `Assets/Features/Procedural/Views/` (authoring components are view-layer per Constitution).

**Rationale**: The current system has `AsteroidFieldSystem` reading from `AsteroidFieldConfig.MvpDefault` — a hard-coded path. The spawner component enables designers to configure fields per-scene by placing GameObjects with different AsteroidFieldDefinition references. Multiple spawners in one scene enable multiple distinct asteroid fields (FR-009).

**Alternatives Considered**:
- **Scene-level SO reference via MonoBehaviour without baking**: Rejected — would require managed system to read SO at runtime, breaking Burst compatibility for the generator job.
- **Extend AsteroidPrefabAuthoring**: Rejected — conflates prefab setup (mesh/material references) with field configuration (ore composition, spatial parameters). Separation of concerns.

## Decision 6: BlobAsset Adaptation Strategy

**Decision**: Modify the existing `OreTypeBlob` struct and `OreTypeBlobBakingSystem` to read from `OreDefinition[]` instead of `OreTypeDefinition[]`. Keep the blob struct lean (only Burst-needed fields).

**Rationale**: `MiningBeamSystem` (Burst-compiled) needs BaseYieldPerSecond, Hardness, and VolumePerUnit for yield calculation. These fields exist on both old and new types. The baking system's `SetOreDefinitions()` and `GetOreId()` API patterns work well — just change the input type. No need to rewrite the entire baking pipeline.

**Blob struct fields** (post-migration):
- `BaseYieldPerSecond` (float) — kept
- `Hardness` (float) — kept
- `VolumePerUnit` (float) — kept
- `Rarity` (float) — removed (not needed by Burst systems; weight is per-field, not per-ore)
- `Tier` (int) — removed (replaced by RarityTier enum on SO, not needed in Burst)

**Alternatives Considered**:
- **Full rewrite with new type names**: Rejected — unnecessary churn. The blob/baking pattern is sound; only the input type changes.
- **Include all OreDefinition fields in blob**: Rejected — Icon (Sprite), Description (string), BaseValue (float), BaseProcessingTimePerUnit (float) are not needed by Burst systems. Keep blob minimal.

## Decision 7: Migration Order

**Decision**: Create-then-swap-then-delete strategy executed in this order:

1. **Create new types** (OreDefinition, OreRarityTier, AsteroidFieldDefinition, OreFieldEntry, AsteroidFieldSpawner) — no breakage, additive only
2. **Create new asset instances** (Luminite, Ferrox, Auralite, DefaultField) — no breakage, additive only
3. **Update baking pipeline** (OreTypeBlobBakingSystem reads OreDefinition[]) — swaps input source
4. **Update consuming systems** (MiningBeamView, OreTypeDatabaseInitializer, AsteroidFieldSystem, AsteroidPrefabAuthoring) — point to new types
5. **Update scenes** (AsteroidsSubScene, GameScene) — wire new spawner, new ore references
6. **Update tests** — point to new ore IDs, validate new types
7. **Delete legacy** (OreTypeDefinition.cs, old .asset files, AsteroidFieldConfig.cs, AsteroidVisualMappingConfig.cs, AsteroidVisualMapping.asset)
8. **Update documentation** (HOWTOPLAY.md, README.md, CHANGELOG.md)

**Rationale**: Additive-first ensures no broken intermediate states. Each step can be compiled and tested independently. Deletion happens last when all references have been migrated.

## Decision 8: Ore Value Mapping

**Decision**: Map legacy ore characteristics to new ores while preserving gameplay balance:

| Legacy → New | Yield | Hardness | Volume | Weight |
|-------------|-------|----------|--------|--------|
| Veldspar → Luminite | 10 | 1.0 | 0.1 | 0.6 |
| Scordite → Ferrox | 7 | 1.5 | 0.15 | 0.3 |
| Pyroxeres → Auralite | 5 | 2.5 | 0.25 | 0.1 |

**Rationale**: Preserving exact gameplay balance (yield, hardness, volume, spawn weight) ensures zero player-facing regression. Only names, colors, and additional metadata change. New fields (BaseValue, BaseProcessingTimePerUnit) are set to reasonable defaults for future use.

## Decision 9: BeamColor and TintColor Values

**Decision**: Assign distinct, thematic colors to each new ore:
- **Luminite** (Common): Ice-blue beam (0.6, 0.85, 1.0), cool-blue tint (1.0, 1.2, 1.4) — evokes glowing crystal
- **Ferrox** (Uncommon): Bronze-orange beam (0.8, 0.5, 0.2), warm-amber tint (1.4, 0.9, 0.3) — evokes metallic ore
- **Auralite** (Rare): Violet beam (0.7, 0.2, 0.9), purple tint (1.1, 0.3, 1.2) — evokes precious/exotic crystal

**Rationale**: Colors are visually distinct from each other and from the legacy ores. Rarity progression goes from cool (common) to warm (uncommon) to exotic (rare), providing instant visual feedback about ore value. Tint colors use values > 1.0 for HDR emission effect, matching the existing asteroid rendering approach.
