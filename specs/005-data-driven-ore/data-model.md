# Data Model: Data-Driven Ore System & Asteroid Spawning Refactor

**Feature**: 005-data-driven-ore
**Date**: 2026-03-01

## Entity Definitions

### OreRarityTier (Enum)

Classification tier for ore rarity. Used for UI display, sorting, and future tech tree gating.

| Value | Description |
|-------|-------------|
| `Common` | High spawn frequency, low value, easy to mine |
| `Uncommon` | Medium spawn frequency, medium value, moderate difficulty |
| `Rare` | Low spawn frequency, high value, hard to mine |

**Location**: `Assets/Features/Mining/Data/OreRarityTier.cs`
**Namespace**: `VoidHarvest.Features.Mining.Data`

---

### OreDefinition (ScriptableObject)

Represents a single mineable ore type. Contains all static data needed for spawning, mining, display, and future economy integration. Replaces legacy `OreTypeDefinition`.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `OreId` | `string` | Unique identifier (e.g., "luminite") | Non-empty, unique across all instances |
| `DisplayName` | `string` | Human-readable name for HUD/UI | Non-empty |
| `RarityTier` | `OreRarityTier` | Rarity classification (Common/Uncommon/Rare) | Enum value |
| `Icon` | `Sprite` | Inventory/UI icon | Nullable (future use) |
| `BaseValue` | `float` | Base market value per unit | >= 0 |
| `Description` | `string` [TextArea] | Flavor text for tooltips | May be empty |
| `RarityWeight` | `float` [Range(0, 1)] | Default spawn probability weight | [0, 1] |
| `BaseYieldPerSecond` | `float` | Base extraction rate before modifiers | > 0 |
| `Hardness` | `float` | Extraction difficulty multiplier (denominator) | > 0 |
| `VolumePerUnit` | `float` | Cargo volume consumed per mined unit | > 0 |
| `BeamColor` | `Color` | Mining laser color when extracting this ore | Any valid Color |
| `BaseProcessingTimePerUnit` | `float` | Refining time per unit in seconds | > 0 |

**Create Menu**: `Create > VoidHarvest > Ore Definition`
**Location**: `Assets/Features/Mining/Data/OreDefinition.cs`
**Namespace**: `VoidHarvest.Features.Mining.Data`

**Relationships**:
- Referenced by `OreFieldEntry` within `AsteroidFieldDefinition`
- Baked into `OreTypeBlob` via `OreTypeBlobBakingSystem`
- Referenced by `OreTypeDatabaseInitializer` for scene initialization
- Referenced by `MiningBeamView` for beam color lookup

---

### OreFieldEntry (Serializable Struct)

A single ore entry within an `AsteroidFieldDefinition`. Links an `OreDefinition` to a spawn weight and visual mapping for that specific asteroid field.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `OreDefinition` | `OreDefinition` | Reference to the ore type SO | Non-null for valid entry |
| `Weight` | `float` | Relative spawn weight (normalized at runtime) | > 0 for active entry |
| `MeshVariantA` | `Mesh` | First mesh variant for visual variety | Non-null |
| `MeshVariantB` | `Mesh` | Second mesh variant for visual variety | Non-null |
| `TintColor` | `Color` | Ore-specific tint applied to asteroid material | Any valid Color |

**Location**: `Assets/Features/Procedural/Data/OreFieldEntry.cs`
**Namespace**: `VoidHarvest.Features.Procedural.Data`

**Weight Normalization**: At spawn time, all entry weights are summed and each is divided by the total. Designers can use any positive values (e.g., 7:2:1) — the system normalizes to probabilities (0.7, 0.2, 0.1).

---

### AsteroidFieldDefinition (ScriptableObject)

Defines a complete asteroid field configuration. Contains ore composition, spatial parameters, and visual mapping. Replaces legacy `AsteroidFieldConfig` record and `AsteroidVisualMappingConfig` SO.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `FieldName` | `string` | Human-readable field name | Non-empty |
| `OreEntries` | `OreFieldEntry[]` | Ore types with weights and visuals | At least 1 valid entry |
| `AsteroidCount` | `int` | Number of asteroids to spawn | > 0 |
| `FieldRadius` | `float` | Spherical field radius in meters | > 0 |
| `AsteroidSizeMin` | `float` | Minimum asteroid radius | > 0, <= AsteroidSizeMax |
| `AsteroidSizeMax` | `float` | Maximum asteroid radius | > 0, >= AsteroidSizeMin |
| `RotationSpeedMin` | `float` | Minimum rotation speed (deg/s) | >= 0 |
| `RotationSpeedMax` | `float` | Maximum rotation speed (deg/s) | >= 0 |
| `Seed` | `uint` | Deterministic RNG seed | Any value |
| `MinScaleFraction` | `float` [Range(0.1, 0.5)] | Minimum asteroid scale at full depletion | [0.1, 0.5] |

**Create Menu**: `Create > VoidHarvest > Asteroid Field Definition`
**Location**: `Assets/Features/Procedural/Data/AsteroidFieldDefinition.cs`
**Namespace**: `VoidHarvest.Features.Procedural.Data`

**Relationships**:
- Contains `OreFieldEntry[]` (composition)
- Referenced by `AsteroidFieldSpawner` authoring component
- Consumed by `AsteroidFieldGeneratorJob` (via baked ECS data)

---

### AsteroidFieldSpawner (Authoring MonoBehaviour)

Scene component that references an `AsteroidFieldDefinition` and triggers asteroid entity creation at bake/load time. Replaces the hard-coded initialization path in `AsteroidFieldSystem`.

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `FieldDefinition` | `AsteroidFieldDefinition` | Reference to field configuration SO | Non-null |
| `MeshVariantPrefabs` | `GameObject[]` | Optional prefab overrides for mesh extraction | Nullable |

**Location**: `Assets/Features/Procedural/Views/AsteroidFieldSpawner.cs`
**Namespace**: `VoidHarvest.Features.Procedural.Views`

**Baking**: The baker creates ECS components from the AsteroidFieldDefinition:
- `AsteroidFieldConfigComponent` — baked spatial parameters (count, radius, seed, sizes)
- `AsteroidOreWeightElement` buffer — baked ore weights per entry
- `AsteroidVisualMappingElement` buffer — baked mesh indices and tint colors
- `AsteroidVisualMappingSingleton` — MinScaleFraction

**Relationships**:
- References `AsteroidFieldDefinition` (1:1)
- Bakes into ECS components consumed by `AsteroidFieldSystem`
- Lives in scene (AsteroidsSubScene)

---

### OreTypeBlob (Burst-Compatible Struct) — Modified

Runtime ore data accessible from Burst-compiled ECS systems. Baked from `OreDefinition` ScriptableObjects.

| Field | Type | Description |
|-------|------|-------------|
| `BaseYieldPerSecond` | `float` | Mining yield rate |
| `Hardness` | `float` | Extraction difficulty |
| `VolumePerUnit` | `float` | Cargo volume per unit |

**Removed Fields** (post-migration):
- `Tier` (int) — replaced by `OreRarityTier` enum on SO, not needed in Burst
- `Rarity` (float) — spawn weight is now per-field (in `AsteroidFieldDefinition`), not per-ore

**Location**: `Assets/Features/Mining/Data/OreTypeBlob.cs` (modified in place)

---

## ECS Components (Existing — No Changes Required)

These components remain unchanged. Listed for completeness:

| Component | Purpose |
|-----------|---------|
| `AsteroidComponent` | Asteroid runtime state (mass, depletion, tint, crumble) |
| `AsteroidOreComponent` | Ore type ID (int index), quantity, depth |
| `OreTypeDatabaseComponent` | Singleton holding `BlobAssetReference<OreTypeBlobDatabase>` |
| `URPMaterialPropertyBaseColor` | Per-instance material color |
| `AsteroidEmissionComponent` | Per-instance emission for depletion glow |
| `MiningBeamComponent` | Active mining beam state |

---

## State Records (Existing — No Changes Required)

| Record | Impact |
|--------|--------|
| `MiningSessionState` | `ActiveOreId` will contain "luminite"/"ferrox"/"auralite" instead of legacy IDs |
| `InventoryState` | `ResourceStack.ResourceId` will use new ore IDs |
| `GameState`, `GameLoopState`, `WorldState` | No structural changes |

---

## Data Flow (Post-Migration)

```
DESIGN-TIME:
  Designer creates OreDefinition assets (Luminite, Ferrox, Auralite)
  Designer creates AsteroidFieldDefinition asset (DefaultField)
    └── OreFieldEntry[] with ore refs, weights, meshes, tints
  Scene: AsteroidFieldSpawner references DefaultField asset
  Scene: OreTypeDatabaseInitializer references OreDefinition[] array

BAKE-TIME (SubScene):
  AsteroidFieldSpawner.Baker → bakes config into ECS components
  AsteroidPrefabAuthoring.Baker → bakes mesh prefab entities + visual mapping buffer

INITIALIZATION:
  OreTypeDatabaseInitializer.Awake() → OreTypeBlobBakingSystem.SetOreDefinitions()
  OreTypeBlobBakingSystem.OnUpdate() → builds BlobAsset, creates singleton

RUNTIME (SPAWNING):
  AsteroidFieldSystem reads baked config components
  AsteroidFieldGeneratorJob (Burst) → deterministic positions + ore assignments
  Entity creation via RenderMeshUtility.AddComponents()
    └── AsteroidComponent, AsteroidOreComponent, visual components

RUNTIME (MINING):
  MiningBeamSystem (Burst) → reads OreTypeBlob from BlobAsset → yield formula
  MiningActionDispatchSystem → OreTypeId→OreId via GetOreId() → inventory dispatch
  MiningBeamView → OreId→BeamColor via OreDefinition[] lookup → render
```

---

## Default Asset Instances

### Luminite.asset (OreDefinition)
- OreId: "luminite", DisplayName: "Luminite", RarityTier: Common
- RarityWeight: 0.6, BaseYield: 10, Hardness: 1.0, Volume: 0.1
- BeamColor: (0.6, 0.85, 1.0) ice-blue
- BaseValue: 10, ProcessingTime: 2.0s

### Ferrox.asset (OreDefinition)
- OreId: "ferrox", DisplayName: "Ferrox", RarityTier: Uncommon
- RarityWeight: 0.3, BaseYield: 7, Hardness: 1.5, Volume: 0.15
- BeamColor: (0.8, 0.5, 0.2) bronze-orange
- BaseValue: 25, ProcessingTime: 5.0s

### Auralite.asset (OreDefinition)
- OreId: "auralite", DisplayName: "Auralite", RarityTier: Rare
- RarityWeight: 0.1, BaseYield: 5, Hardness: 2.5, Volume: 0.25
- BeamColor: (0.7, 0.2, 0.9) violet
- BaseValue: 75, ProcessingTime: 10.0s

### DefaultField.asset (AsteroidFieldDefinition)
- FieldName: "Default Asteroid Field", Seed: 42
- AsteroidCount: 300, FieldRadius: 2000
- SizeRange: [3, 5], RotationRange: [0, 15], MinScaleFraction: 0.3
- OreEntries: Luminite(w=6, blue tint), Ferrox(w=3, amber tint), Auralite(w=1, purple tint)
