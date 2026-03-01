# Feature Specification: Data-Driven Ore System & Asteroid Spawning Refactor

**Feature Branch**: `005-data-driven-ore`
**Created**: 2026-03-01
**Status**: Draft
**Input**: Refactor the ore and asteroid spawning systems into a fully data-driven, editor-expandable ScriptableObject architecture. Mandatory migration from legacy hard-coded ore and asteroid logic (Spec 003). Replace all ores with new OreDefinition SOs (Luminite, Ferrox, Auralite). Replace all asteroid spawning with AsteroidFieldDefinition SOs. Delete all legacy ore code after migration.

---

## User Scenarios & Testing

### User Story 1 — Designer Creates New Ore Types in the Editor (Priority: P1)

A game designer opens the Unity Editor and creates a brand-new ore type entirely through the Inspector — no code changes required. They right-click in the Project window, select Create > VoidHarvest > Ore Definition, and configure all properties: display name, rarity tier, icon, base value, mining yield, hardness, processing time, beam color, and rarity weight. The new ore immediately appears in asteroid fields when referenced by an AsteroidFieldDefinition.

**Why this priority**: The entire feature's value proposition rests on designer-expandability. If ore types cannot be created and configured without code changes, the data-driven architecture has failed.

**Independent Test**: Create a fourth test ore type in the Editor, reference it in a field definition, enter Play mode, and verify asteroids spawn with the new ore — all without modifying any C# files.

**Acceptance Scenarios**:

1. **Given** the Unity Editor is open, **When** a designer creates a new OreDefinition asset and fills in all fields, **Then** the asset saves successfully and all fields persist between Editor sessions.
2. **Given** an OreDefinition asset exists with valid data, **When** it is referenced by an AsteroidFieldDefinition and the scene loads, **Then** asteroids of that ore type spawn with correct visual tinting and mining behavior.
3. **Given** an OreDefinition asset has a rarity weight of 0, **When** the field spawns asteroids, **Then** no asteroids of that ore type appear.

---

### User Story 2 — Designer Configures Asteroid Belt Composition (Priority: P1)

A game designer creates distinct asteroid belts by authoring AsteroidFieldDefinition ScriptableObjects. Each definition specifies which ores appear (with weighted probabilities), how many asteroids spawn, the field radius, asteroid size ranges, rotation speed ranges, and visual variant mappings. Multiple fields with different compositions can coexist in the same scene or across scenes.

**Why this priority**: Asteroid belt variety is the foundation for gameplay diversity. Without data-driven field composition, every asteroid field is identical, eliminating exploration incentive.

**Independent Test**: Create two AsteroidFieldDefinition assets with different ore distributions (one rich in Luminite, one rich in Auralite), place both in a test scene, enter Play mode, and visually confirm distinct compositions.

**Acceptance Scenarios**:

1. **Given** an AsteroidFieldDefinition with 3 ore entries (Luminite 70%, Ferrox 20%, Auralite 10%), **When** the field spawns 300 asteroids, **Then** the ore distribution approximately matches the configured weights (within statistical variance).
2. **Given** an AsteroidFieldDefinition with spawn count of 500 and field radius of 3000m, **When** the scene loads, **Then** exactly 500 asteroids spawn distributed within a 3000m sphere.
3. **Given** two different AsteroidFieldDefinition assets in the same scene, **When** the scene loads, **Then** each field spawns independently with its own composition, radius, and density.

---

### User Story 3 — Player Mines New Ore Types Seamlessly (Priority: P1)

A player targets an asteroid, activates their mining beam, and extracts ore. The mining beam color matches the ore being mined. Ore chunks fly toward the ship. The HUD displays the ore name, remaining mass percentage, and cargo updates. Depletion visuals (scale shrinkage, emission glow, crumble thresholds) work identically to the current system. The player's cargo inventory updates with the correct ore type and quantity. All existing Spec 003 VFX and feedback systems continue to function without regression.

**Why this priority**: The player-facing mining experience must be seamless. This is a backend refactor — players should notice improved ore variety, not broken mechanics.

**Independent Test**: Mine each of the three new ore types to depletion and verify beam color, yield rate, depletion visuals, ore chunk spawning, audio feedback, and inventory updates all work correctly.

**Acceptance Scenarios**:

1. **Given** a player targets a Luminite asteroid and activates their mining beam, **When** ore is extracted, **Then** the beam color matches Luminite's configured BeamColor, yield follows the formula `(miningPower * baseYield * dt) / (hardness * (1 + depth))`, and the HUD shows "Luminite" as the ore type.
2. **Given** a player mines an Auralite asteroid to 75% depletion, **When** the crumble threshold is reached, **Then** crumble VFX triggers, scale shrinks, and emission glow intensifies — identically to the current system behavior.
3. **Given** a player's cargo hold has 50 units of capacity remaining and Ferrox has a volume of 0.15 per unit, **When** mining continues, **Then** cargo fills correctly and warns when approaching capacity.

---

### User Story 4 — Legacy System Fully Replaced (Priority: P2)

After migration, no legacy ore code, assets, or hard-coded spawning parameters remain in the project. The three legacy ores (Veldspar, Scordite, Pyroxeres) are completely replaced by the three new ores (Luminite, Ferrox, Auralite). All scenes use the new AsteroidFieldDefinition-driven spawning. The old `AsteroidFieldConfig` record with hard-coded `MvpDefault` values is removed. All references to legacy ore IDs are purged from scripts, ScriptableObjects, and scene data.

**Why this priority**: Technical debt from legacy code creates maintenance burden and confusion. Clean migration ensures the data-driven system is the single source of truth.

**Independent Test**: Search the entire project for legacy ore IDs ("veldspar", "scordite", "pyroxeres") and the old `AsteroidFieldConfig.MvpDefault` — zero results should be found in any runtime code or assets.

**Acceptance Scenarios**:

1. **Given** the migration is complete, **When** searching the codebase for "veldspar", "scordite", or "pyroxeres", **Then** zero matches are found in any C# source file or ScriptableObject asset.
2. **Given** the migration is complete, **When** examining the `AsteroidFieldConfig` type (if retained), **Then** no hard-coded default ore distributions or field parameters exist.
3. **Given** the GameScene loads, **When** inspecting the asteroid field, **Then** it uses an AsteroidFieldDefinition asset (not hard-coded config) and all asteroids are Luminite, Ferrox, or Auralite.

---

### User Story 5 — Player Documentation Reflects New Ores (Priority: P2)

The HOWTOPLAY.md file and any in-game help text are updated to reference the new ore types (Luminite, Ferrox, Auralite) with their rarity tiers, descriptions, and gameplay characteristics. The README.md feature overview reflects the data-driven ore system. A changelog entry documents the migration.

**Why this priority**: Constitution v1.3.0 mandates player-facing documentation for all player-facing features. Outdated ore references (Veldspar, Scordite, Pyroxeres) in documentation would confuse players.

**Independent Test**: Read HOWTOPLAY.md and verify all ore references match the new system. Check README.md for updated feature description.

**Acceptance Scenarios**:

1. **Given** the migration is complete, **When** reading HOWTOPLAY.md, **Then** all ore type references are Luminite, Ferrox, and Auralite with correct rarity descriptions and gameplay tips.
2. **Given** the migration is complete, **When** reading README.md, **Then** the "What's Implemented" section mentions the data-driven ore system and asteroid field configuration.
3. **Given** the migration is complete, **When** checking the changelog, **Then** an entry documents the Spec 005 migration with a summary of changes.

---

### Edge Cases

- What happens when an AsteroidFieldDefinition has zero ore entries? The system logs a warning and spawns no asteroids.
- What happens when all ore weights in a field definition sum to zero? The system treats this as an error and logs a warning; no asteroids spawn.
- What happens when an OreDefinition referenced by a field definition is null or deleted? The system skips that entry, logs a warning, and distributes weight among remaining valid ores.
- What happens when an AsteroidFieldDefinition specifies a spawn count of zero? The system spawns no asteroids without error.
- What happens when two asteroid fields overlap spatially? Asteroids from both fields coexist without interference — each field manages its own entities.
- What happens when a player is actively mining an asteroid when the scene reloads? Mining session state clears gracefully as per existing behavior.
- What happens when an OreDefinition has zero baseYieldPerSecond? Mining produces no yield — the beam connects but extracts nothing.
- What happens when asteroid size range min exceeds max? The system swaps the values so min < max and logs a warning.

---

## Requirements

### Functional Requirements

#### OreDefinition ScriptableObject

- **FR-001**: System MUST provide an OreDefinition ScriptableObject type with the following fields: unique string identifier, display name, rarity tier (Common, Uncommon, Rare), icon (Sprite), base market value (float), description text, rarity weight (float), base yield per second (float), hardness (float), volume per unit (float), beam color (Color), and base processing time per unit (float, seconds).
- **FR-002**: System MUST ship three OreDefinition instances: Luminite (Common, high rarity weight, low hardness, fast processing), Ferrox (Uncommon, medium rarity weight, medium hardness, medium processing), and Auralite (Rare, low rarity weight, high hardness, slow processing).
- **FR-003**: System MUST allow designers to create new OreDefinition assets via the Unity Editor Create Asset menu without modifying code.
- **FR-004**: System MUST bake all OreDefinition data into a Burst-compatible BlobAsset at initialization for zero-allocation runtime access by ECS systems.
- **FR-005**: System MUST support an arbitrary number of OreDefinition assets — not limited to three.

#### AsteroidFieldDefinition ScriptableObject

- **FR-006**: System MUST provide an AsteroidFieldDefinition ScriptableObject type with the following fields: field name (string), ore entries (list of OreDefinition references with per-ore weight overrides), total asteroid count (int), field radius (float, meters), asteroid size range (min/max float), asteroid rotation speed range (min/max float), random seed (uint), minimum scale fraction (float, clamped to [0.1, 0.5], default 0.3 — smallest asteroid scale at full depletion), and visual variant references (meshes per ore type with tint colors).
- **FR-007**: System MUST allow designers to create new AsteroidFieldDefinition assets via the Unity Editor Create Asset menu without modifying code.
- **FR-008**: System MUST normalize ore entry weights at spawn time so they sum to 1.0, enabling designers to use arbitrary weight values (e.g., 7:2:1 instead of 0.7:0.2:0.1).
- **FR-009**: System MUST support multiple AsteroidFieldDefinition instances in a single scene, each spawning independently.

#### Asteroid Spawning

- **FR-010**: System MUST provide a spawner component that references an AsteroidFieldDefinition and spawns asteroids when the scene loads.
- **FR-011**: System MUST generate asteroid positions using deterministic seeded random distribution within the configured field radius (spherical distribution).
- **FR-012**: System MUST assign ore types to asteroids using weighted random selection based on the AsteroidFieldDefinition's ore entry weights.
- **FR-013**: System MUST assign visual meshes and tint colors to spawned asteroids based on the ore type, using the visual variant mapping from the AsteroidFieldDefinition.
- **FR-014**: System MUST create asteroid entities with all required ECS components for full compatibility with existing mining, depletion, and VFX systems.

#### Mining Integration

- **FR-015**: System MUST maintain full compatibility with mining yield calculations using OreDefinition data (baseYieldPerSecond, hardness) via the BlobAsset database.
- **FR-016**: System MUST maintain full compatibility with mining beam color rendering using OreDefinition beam color.
- **FR-017**: System MUST maintain full compatibility with ore ID to inventory dispatching.
- **FR-018**: System MUST maintain full compatibility with all Spec 003 VFX systems: depletion visuals, ore chunk spawning, mining audio feedback, and spark coloring.
- **FR-019**: System MUST maintain full compatibility with HUD target info panel ore type display.

#### Migration

- **FR-020**: System MUST completely remove all legacy ore definitions (Veldspar, Scordite, Pyroxeres) — assets, C# references, and hard-coded ore IDs.
- **FR-021**: System MUST completely remove the hard-coded AsteroidFieldConfig default values and replace all usages with AsteroidFieldDefinition references.
- **FR-022**: System MUST update all scenes to use the new AsteroidFieldDefinition-driven spawning system.
- **FR-023**: System MUST ensure zero compilation errors and zero runtime errors after migration.

#### Documentation

- **FR-024**: System MUST update HOWTOPLAY.md to replace all Veldspar/Scordite/Pyroxeres references with Luminite/Ferrox/Auralite including correct rarity tiers and gameplay descriptions.
- **FR-025**: System MUST update README.md to reflect the data-driven ore system in the feature overview.
- **FR-026**: System MUST include a changelog entry documenting the Spec 005 migration.

### Non-Functional Requirements

- **NFR-001**: Asteroid spawning MUST complete within a single frame for up to 500 asteroids on mid-range hardware.
- **NFR-002**: Ore type lookup during mining MUST produce zero GC allocations per frame.
- **NFR-003**: Adding a new ore type MUST require zero C# code changes — only ScriptableObject asset creation and field definition updates.
- **NFR-004**: The BlobAsset baking pipeline MUST support any number of ore types without code modification.

### Key Entities

- **OreDefinition**: A ScriptableObject representing a single mineable ore type. Contains all static data for spawning, mining, display, and economy: identifier, display name, rarity tier, icon, base value, description, rarity weight, base yield, hardness, volume per unit, beam color, and processing time. Replaces the legacy OreTypeDefinition.
- **AsteroidFieldDefinition**: A ScriptableObject representing a complete asteroid field configuration. Contains ore composition (weighted references to OreDefinition assets), spatial parameters (count, radius, size range, rotation range, seed), and visual mapping (mesh variants and tint overrides per ore type). Replaces the legacy hard-coded AsteroidFieldConfig.
- **OreFieldEntry**: A serializable entry within AsteroidFieldDefinition linking one OreDefinition to a spawn weight and visual variant fields (mesh variants, tint color). Enables per-field ore composition tuning.
- **AsteroidFieldSpawner**: A scene component that references an AsteroidFieldDefinition and triggers asteroid entity creation at scene load. Replaces the hard-coded initialization path.

---

## Scope Boundaries

### In Scope

- New OreDefinition ScriptableObject type with all specified fields
- Three default OreDefinition instances (Luminite, Ferrox, Auralite)
- New AsteroidFieldDefinition ScriptableObject type with all specified fields
- AsteroidFieldSpawner component consuming AsteroidFieldDefinition
- BlobAsset baking pipeline for new OreDefinition assets
- Full integration with existing mining, VFX, audio, depletion, and HUD systems
- Complete removal of legacy ores (Veldspar, Scordite, Pyroxeres)
- Complete removal of hard-coded spawning parameters
- Scene migration (GameScene, AsteroidsSubScene)
- Updated player documentation (HOWTOPLAY.md, README.md, changelog)
- Editor Create Asset menu items for both ScriptableObject types

### Out of Scope

- Ore processing/refining mechanics (future spec)
- Market/economy integration for ore values (Phase 3)
- Tech tree gating for ore tiers (Phase 1+)
- Asteroid belt discovery/exploration mechanics
- Dynamic asteroid respawning after depletion
- Networked/multiplayer asteroid synchronization
- New VFX or audio assets (reuses existing Spec 003 systems)
- Additional ore types beyond the initial three (designers can add post-migration)
- Asteroid collision physics between asteroids
- LOD system for distant asteroids

---

## Assumptions

- **Ore naming**: Luminite, Ferrox, and Auralite are final names. Luminite replaces Veldspar (common), Ferrox replaces Scordite (uncommon), Auralite replaces Pyroxeres (rare).
- **Processing time field**: `baseProcessingTimePerUnit` is stored for future use by refining systems but has no runtime effect in this spec.
- **Icon field**: The Sprite icon on OreDefinition is stored for future UI use (inventory, market) but has no runtime binding in this spec beyond data storage.
- **Base value field**: The `baseValue` float is stored for future economy integration but has no runtime effect in this spec.
- **Visual variants**: The existing SF_Asteroids-M2 mesh variants (2 per ore type) are reused. Tint colors are remapped to new ore identities.
- **Rarity tier enum**: A simple Common/Uncommon/Rare enum suffices initially and can be extended for future ores.
- **Seed determinism**: Asteroid field generation remains deterministic given the same seed, ensuring reproducible layouts.
- **Existing assembly structure**: New types are added to existing assembly definitions rather than creating new assemblies.
- **BlobAsset adaptation**: The existing OreTypeBlobBakingSystem pattern is adapted for the new OreDefinition type.
- **OreFieldEntry mutability**: `OreFieldEntry` is a `[Serializable] struct` (not `readonly struct`) because Unity's serialization system requires mutable backing fields for Inspector editing. This is a Constitution Principle I deviation justified by Unity engine limitation. A `// CONSTITUTION DEVIATION:` comment is required in the source file.

---

## Success Criteria

### Measurable Outcomes

- **SC-001**: A designer can create a new ore type and see it spawn in an asteroid field within 5 minutes, requiring zero code changes.
- **SC-002**: A designer can create a new asteroid field with unique ore composition and spatial parameters within 5 minutes, requiring zero code changes.
- **SC-003**: All three ore types (Luminite, Ferrox, Auralite) are mineable with correct beam colors, yield rates, depletion visuals, ore chunks, and audio feedback — matching the quality of the legacy system.
- **SC-004**: Zero legacy ore references (Veldspar, Scordite, Pyroxeres) exist in any runtime code, ScriptableObject asset, or scene file after migration.
- **SC-005**: Asteroid field spawning for 500 asteroids completes within one frame on mid-range hardware.
- **SC-006**: Mining operations produce zero GC allocations per frame.
- **SC-007**: All existing tests continue to pass after migration (zero regressions).
- **SC-008**: HOWTOPLAY.md accurately describes all three new ore types with correct names, rarity tiers, and gameplay characteristics.
- **SC-009**: Multiple asteroid fields with different configurations can coexist in a single scene without interference.
- **SC-010**: Adding a fourth ore type to an existing asteroid field requires only creating an OreDefinition asset and adding an entry to the AsteroidFieldDefinition — no code changes.

---

## Documentation Updates

Per Constitution v1.3.0 Player Documentation mandate:

- **HOWTOPLAY.md**: Replace all Veldspar/Scordite/Pyroxeres references with Luminite/Ferrox/Auralite. Update ore descriptions, rarity tiers, mining tips, and any ore-specific gameplay guidance.
- **README.md**: Update "What's Implemented" section to mention data-driven ore system and configurable asteroid fields.
- **CHANGELOG.md**: Add Spec 005 entry documenting the migration from legacy ores to the data-driven OreDefinition/AsteroidFieldDefinition system.
