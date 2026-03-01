# Implementation Plan: Data-Driven Ore System & Asteroid Spawning Refactor

**Branch**: `005-data-driven-ore` | **Date**: 2026-03-01 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-data-driven-ore/spec.md`

## Summary

Refactor VoidHarvest's ore and asteroid spawning systems from hard-coded definitions into a fully data-driven ScriptableObject architecture. Replace three legacy ores (Veldspar, Scordite, Pyroxeres) with three new ores (Luminite, Ferrox, Auralite) via OreDefinition SOs. Replace the hard-coded `AsteroidFieldConfig.MvpDefault` with editor-configurable AsteroidFieldDefinition SOs. Maintain full compatibility with all Spec 003 mining VFX/feedback and Spec 004 docking systems.

## Technical Context

**Language/Version**: C# 9.0 / .NET Framework 4.7.1
**Primary Dependencies**: Unity 6 (6000.3.10f1), Entities 1.3.2, Entities Graphics 1.3.2, Burst, Jobs, URP 17.3.0, UniTask 2.5.10, VContainer 1.16.7, System.Collections.Immutable
**Storage**: ScriptableObject assets (design-time), BlobAsset (runtime ECS), immutable records (state store)
**Testing**: NUnit + Unity Test Framework (EditMode + PlayMode), TDD mandatory
**Target Platform**: Standalone Windows 64-bit
**Project Type**: Unity game (hybrid DOTS/ECS + MonoBehaviour)
**Performance Goals**: 60 FPS minimum, zero GC in hot loops, single-frame spawning for 500 asteroids
**Constraints**: Functional/immutable-first, pure reducers, no mutable game state, no static singletons
**Scale/Scope**: ~15 files modified, ~8 new files, ~5 files deleted, 3 ore types, 1+ asteroid fields

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Principle | Status | Notes |
|------|-----------|--------|-------|
| G1 | I. Functional & Immutable First | PASS | OreDefinition is SO (read-only at runtime). AsteroidFieldDefinition is SO. State via pure reducers. BlobAsset is immutable. |
| G2 | II. Predictability & Testability | PASS | All spawn logic in Burst jobs (deterministic seeded RNG). Yield formula is pure function. Reducers are pure static. |
| G3 | III. Performance by Default | PASS | BlobAsset for zero-GC ore lookup. Burst-compiled spawn job. RenderMeshUtility for entity creation. NFR-001/002 enforced. |
| G4 | IV. Data-Oriented Design | PASS | ScriptableObjects for static data. ECS components for runtime. Composition, no inheritance. |
| G5 | V. Modularity & Extensibility | PASS | New types added to existing assembly definitions. EventBus for cross-system communication. Feature isolation maintained. |
| G6 | VI. Explicit Over Implicit | PASS | AsteroidFieldSpawner explicitly references AsteroidFieldDefinition. No magic wiring. Weight normalization is explicit. |
| G7 | Editor Automation (MCP) | PASS | Compilation verification, console monitoring, test execution, scene validation via MCP. |
| G8 | Player Documentation | PASS | HOWTOPLAY.md, README.md, CHANGELOG.md updates specified in FR-024/025/026. |
| G9 | TDD Workflow | PASS | Tests written first for all pure logic (weight normalization, ore lookup, yield formula with new ores). |

**Pre-Design Check**: ALL GATES PASS. No deviations required.

**Post-Design Re-Check (Phase 1 complete)**:
- G1 PASS: OreDefinition and AsteroidFieldDefinition are ScriptableObjects (immutable at runtime). OreFieldEntry is a serializable struct (value type). OreTypeBlob is readonly struct in BlobAsset. No mutable domain state introduced.
- G2 PASS: Weight normalization is a pure function. AsteroidFieldGeneratorJob remains Burst-compiled with seeded RNG. All existing pure reducers unchanged.
- G3 PASS: BlobAsset keeps only 3 float fields (lean). OreFieldEntry avoids managed references in Burst path. Entity creation via RenderMeshUtility preserved.
- G4 PASS: ScriptableObjects for all static data. ECS components for runtime. Composition via OreFieldEntry[], no inheritance.
- G5 PASS: New types in existing assemblies (Mining.Data, Procedural.Data, Procedural.Views). No new cross-assembly dependencies.
- G6 PASS: AsteroidFieldSpawner explicitly wires AsteroidFieldDefinition. Weight normalization is documented. No implicit behavior.
- G7 PASS: MCP verification gates apply to all new/modified scripts.
- G8 PASS: HOWTOPLAY.md, README.md, CHANGELOG.md updates in scope.
- G9 PASS: Tests planned for weight normalization, ore blob baking, field generation with new ores.

**Post-Design Result**: ALL GATES PASS. No deviations required.

## Project Structure

### Documentation (this feature)

```text
specs/005-data-driven-ore/
├── plan.md              # This file
├── research.md          # Phase 0 output - design decisions
├── data-model.md        # Phase 1 output - entity definitions
├── quickstart.md        # Phase 1 output - designer guide
├── contracts/           # Phase 1 output - interface contracts
│   ├── ore-definition.md
│   └── asteroid-field-definition.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Assets/
├── Features/
│   ├── Mining/
│   │   ├── Data/
│   │   │   ├── OreDefinition.cs              # NEW - replaces OreTypeDefinition.cs
│   │   │   ├── OreRarityTier.cs              # NEW - enum (Common, Uncommon, Rare)
│   │   │   ├── OreTypeBlob.cs                # MODIFIED - adapted for OreDefinition fields
│   │   │   ├── OreTypeDefinition.cs          # DELETE after migration
│   │   │   ├── Resources_moved/
│   │   │   │   ├── Veldspar.asset            # DELETE
│   │   │   │   ├── Scordite.asset            # DELETE
│   │   │   │   └── Pyroxeres.asset           # DELETE
│   │   │   ├── Ores/                         # NEW folder
│   │   │   │   ├── Luminite.asset            # NEW
│   │   │   │   ├── Ferrox.asset              # NEW
│   │   │   │   └── Auralite.asset            # NEW
│   │   │   ├── MiningVFXConfig.cs            # UNCHANGED
│   │   │   ├── DepletionVFXConfig.cs         # UNCHANGED
│   │   │   ├── OreChunkConfig.cs             # UNCHANGED
│   │   │   └── MiningAudioConfig.cs          # UNCHANGED
│   │   ├── Systems/
│   │   │   ├── OreTypeBlobBakingSystem.cs    # MODIFIED - bake OreDefinition[] instead
│   │   │   ├── MiningBeamSystem.cs           # MINIMAL CHANGE - same blob interface
│   │   │   ├── MiningActionDispatchSystem.cs # MINIMAL CHANGE - same dispatch pattern
│   │   │   └── MiningReducer.cs              # UNCHANGED - same yield formula
│   │   ├── Views/
│   │   │   ├── OreTypeDatabaseInitializer.cs # MODIFIED - references OreDefinition[]
│   │   │   └── MiningBeamView.cs             # MODIFIED - references OreDefinition[]
│   │   └── Tests/
│   │       ├── OreDefinitionTests.cs         # NEW - validate SO fields, blob baking
│   │       └── (existing tests)              # MODIFIED - update ore IDs
│   ├── Procedural/
│   │   ├── Data/
│   │   │   ├── AsteroidFieldDefinition.cs    # NEW - replaces AsteroidFieldConfig
│   │   │   ├── OreFieldEntry.cs              # NEW - per-ore entry in field definition
│   │   │   ├── AsteroidFieldConfig.cs        # DELETE after migration
│   │   │   ├── AsteroidVisualMappingConfig.cs # DELETE after migration (absorbed into AsteroidFieldDefinition)
│   │   │   ├── AsteroidVisualMapping.asset   # DELETE after migration
│   │   │   └── Fields/                       # NEW folder
│   │   │       └── DefaultField.asset        # NEW - AsteroidFieldDefinition instance
│   │   ├── Systems/
│   │   │   ├── AsteroidFieldGeneratorJob.cs  # MODIFIED - read from AsteroidFieldDefinition data
│   │   │   └── AsteroidFieldSystem.cs        # MODIFIED - consume AsteroidFieldSpawner/Definition
│   │   ├── Views/
│   │   │   ├── AsteroidFieldSpawner.cs       # NEW - authoring component referencing AsteroidFieldDefinition
│   │   │   └── AsteroidPrefabAuthoring.cs    # MODIFIED - consume OreFieldEntry visual data
│   │   └── Tests/
│   │       ├── AsteroidFieldDefinitionTests.cs # NEW - weight normalization, validation
│   │       └── (existing tests)              # MODIFIED - update ore IDs
│   ├── HUD/
│   │   └── Views/
│   │       └── TargetInfoPanel.cs            # UNCHANGED (receives display name string)
│   └── Resources/
│       └── Systems/
│           └── InventoryReducer.cs           # UNCHANGED (receives resource ID string)
└── Core/
    └── State/                                # UNCHANGED
```

**Structure Decision**: New types are added to existing feature assembly definitions (`VoidHarvest.Features.Mining` and `VoidHarvest.Features.Procedural`). No new assemblies. OreDefinition lives in Mining/Data (same as legacy OreTypeDefinition). AsteroidFieldDefinition lives in Procedural/Data (same area as legacy AsteroidFieldConfig). Visual mapping is absorbed into OreFieldEntry within AsteroidFieldDefinition, eliminating the separate AsteroidVisualMappingConfig.

## Key Architectural Decisions

### 1. OreFieldEntry Absorbs Visual Mapping

The current system has visual mapping in a separate `AsteroidVisualMappingConfig` SO with entries keyed by OreId string. The new system embeds visual mapping directly in `OreFieldEntry` (inside `AsteroidFieldDefinition`). This means each asteroid field is fully self-contained — one SO defines everything needed to spawn that field. Different fields can use different visual styles for the same ore type.

### 2. OreDefinition Extends OreTypeDefinition

`OreDefinition` adds new fields (RarityTier enum, Icon, BaseValue, Description, BaseProcessingTimePerUnit) while keeping all existing fields (OreId, DisplayName, BeamColor, BaseYieldPerSecond, Hardness, VolumePerUnit). The `Tier` int field is replaced by `RarityTier` enum. The `Rarity` float field is renamed to `RarityWeight` for clarity.

### 3. BlobAsset Adaptation (Not Rewrite)

The `OreTypeBlob` struct and `OreTypeBlobBakingSystem` are adapted in-place. The blob struct retains only Burst-needed fields (BaseYieldPerSecond, Hardness, VolumePerUnit). The baking system reads from `OreDefinition[]` instead of `OreTypeDefinition[]`. The `GetOreId()` reverse lookup is preserved. This minimizes changes to `MiningBeamSystem` (Burst consumer).

### 4. AsteroidFieldSpawner Authoring Component

A new `AsteroidFieldSpawner` authoring MonoBehaviour replaces the hard-coded initialization in `AsteroidFieldSystem`. It references an `AsteroidFieldDefinition` SO and passes the configuration into ECS at bake time. Multiple spawners can exist in a scene (one per field). `AsteroidFieldSystem` reads the baked data instead of hard-coded `MvpDefault`.

### 5. Migration Strategy: Create-Then-Swap-Then-Delete

Phase order ensures no broken state:
1. Create new types (OreDefinition, AsteroidFieldDefinition, OreFieldEntry, AsteroidFieldSpawner)
2. Create new asset instances (Luminite, Ferrox, Auralite, DefaultField)
3. Update consuming systems to use new types
4. Update scenes to use new spawner
5. Delete legacy code, assets, and references
6. Update documentation

## Complexity Tracking

> No Constitution Check violations. No complexity justifications needed.

## Migration Mapping

| Legacy | New | Action |
|--------|-----|--------|
| `OreTypeDefinition` (SO class) | `OreDefinition` (SO class) | Replace — add fields, rename |
| `Veldspar.asset` | `Luminite.asset` | Replace — new SO instance |
| `Scordite.asset` | `Ferrox.asset` | Replace — new SO instance |
| `Pyroxeres.asset` | `Auralite.asset` | Replace — new SO instance |
| `AsteroidFieldConfig` (record) | `AsteroidFieldDefinition` (SO class) | Replace — config→SO |
| `OreDistribution` (readonly struct) | `OreFieldEntry` (serializable struct) | Replace — add visual fields |
| `AsteroidVisualMappingConfig` (SO) | Absorbed into `OreFieldEntry` | Delete — data embedded in field def |
| `AsteroidVisualMapping.asset` | Absorbed into `DefaultField.asset` | Delete |
| `AsteroidFieldConfig.MvpDefault` | `DefaultField.asset` (SO instance) | Delete code, create asset |
| `OreTypeDefinition.Tier` (int) | `OreDefinition.RarityTier` (enum) | Replace — int→enum |
| `OreTypeDefinition.Rarity` (float) | `OreDefinition.RarityWeight` (float) | Rename for clarity |

## New Ore Values

| Property | Luminite (Common) | Ferrox (Uncommon) | Auralite (Rare) |
|----------|-------------------|-------------------|-----------------|
| OreId | `"luminite"` | `"ferrox"` | `"auralite"` |
| DisplayName | `"Luminite"` | `"Ferrox"` | `"Auralite"` |
| RarityTier | Common | Uncommon | Rare |
| RarityWeight | 0.6 | 0.3 | 0.1 |
| BaseYieldPerSecond | 10.0 | 7.0 | 5.0 |
| Hardness | 1.0 | 1.5 | 2.5 |
| VolumePerUnit | 0.1 | 0.15 | 0.25 |
| BaseValue | 10.0 | 25.0 | 75.0 |
| BaseProcessingTimePerUnit | 2.0s | 5.0s | 10.0s |
| BeamColor | (0.6, 0.85, 1.0) ice-blue | (0.8, 0.5, 0.2) bronze-orange | (0.7, 0.2, 0.9) violet |
| TintColor (visual) | (1.0, 1.2, 1.4) cool blue | (1.4, 0.9, 0.3) warm amber | (1.1, 0.3, 1.2) purple |

## Default Field Definition Values

| Property | Value |
|----------|-------|
| FieldName | `"Default Asteroid Field"` |
| AsteroidCount | 300 |
| FieldRadius | 2000.0 |
| AsteroidSizeMin | 3.0 |
| AsteroidSizeMax | 5.0 |
| AsteroidRotationSpeedMin | 0.0 |
| AsteroidRotationSpeedMax | 15.0 |
| Seed | 42 |
| Ore Entries | Luminite (weight 6), Ferrox (weight 3), Auralite (weight 1) |
