# Tasks: Data-Driven Ore System & Asteroid Spawning Refactor

**Input**: Design documents from `/specs/005-data-driven-ore/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: TDD is mandatory per project constitution. Test tasks are included — write tests FIRST, verify they FAIL, then implement.

**Organization**: Tasks are grouped by user story. Dependency chain: Foundational → US1 → US2 → US3 → US4 → US5 → Polish.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Unity project root: `Assets/`
- Feature code: `Assets/Features/<System>/{Data,Systems,Views,Tests}/`
- Core shared code: `Assets/Core/`
- Documentation: repository root (`HOWTOPLAY.md`, `README.md`, `CHANGELOG.md`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create folder structure for new asset instances

- [ ] T001 Create `Assets/Features/Mining/Data/Ores/` folder for OreDefinition asset instances
- [ ] T002 [P] Create `Assets/Features/Procedural/Data/Fields/` folder for AsteroidFieldDefinition asset instances

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core data contracts that MUST be complete before ANY user story can be implemented

**CRITICAL**: No user story work can begin until this phase is complete. OreRarityTier and OreDefinition are referenced by all downstream systems (baking pipeline, field entries, mining views).

### Tests (TDD — write FIRST, verify FAIL)

- [ ] T003 [P] Write TDD tests for OreRarityTier enum values (Common, Uncommon, Rare exist as enum members) in `Assets/Features/Mining/Tests/OreDefinitionTests.cs`
- [ ] T004 Write TDD tests for OreDefinition ScriptableObject (all 12 fields exist with correct types; has CreateAssetMenu attribute with path "VoidHarvest/Ore Definition"; positive validation on BaseYieldPerSecond, Hardness, VolumePerUnit) in `Assets/Features/Mining/Tests/OreDefinitionTests.cs`

### Implementation

- [ ] T005 [P] Create OreRarityTier enum with values Common, Uncommon, Rare in namespace VoidHarvest.Features.Mining.Data in `Assets/Features/Mining/Data/OreRarityTier.cs`
- [ ] T006 Create OreDefinition ScriptableObject with all 12 fields (OreId string, DisplayName string, RarityTier OreRarityTier, Icon Sprite, BaseValue float, Description string [TextArea], RarityWeight float [Range(0,1)], BaseYieldPerSecond float, Hardness float, VolumePerUnit float, BeamColor Color, BaseProcessingTimePerUnit float) and `[CreateAssetMenu(menuName = "VoidHarvest/Ore Definition")]` in `Assets/Features/Mining/Data/OreDefinition.cs`
- [ ] T007 Verify compilation via Unity console (MCP read_console) — zero errors from new types, TDD tests now pass

**Checkpoint**: OreRarityTier and OreDefinition data contracts established. All user story phases can now proceed.

---

## Phase 3: User Story 1 — Designer Creates New Ore Types in the Editor (Priority: P1) :dart: MVP

**Goal**: Designers can create OreDefinition assets via the Editor Create menu. The BlobAsset baking pipeline consumes OreDefinition[] for zero-allocation runtime access. Mining views reference OreDefinition[] for beam colors and display names.

**Independent Test**: Create a fourth test OreDefinition asset in the Editor, reference it in a field definition, enter Play mode — verify asteroids spawn with the new ore without modifying any C# files.

### Tests for User Story 1 (TDD — write FIRST, verify FAIL)

- [ ] T008 [P] [US1] Write TDD tests for OreTypeBlob readonly struct containing only 3 float fields (BaseYieldPerSecond, Hardness, VolumePerUnit — no Tier, no Rarity) in `Assets/Features/Mining/Tests/OreDefinitionTests.cs`
- [ ] T009 [US1] Write TDD tests for OreTypeBlobBakingSystem baking OreDefinition[] into BlobAsset (verify blob entry count matches OreDefinition count, field values match, GetOreId reverse lookup returns correct OreId strings) in `Assets/Features/Mining/Tests/OreDefinitionTests.cs`

### Implementation for User Story 1

- [ ] T010 [US1] Modify OreTypeBlob readonly struct: remove Tier (int) and Rarity (float) fields, keep only BaseYieldPerSecond (float), Hardness (float), VolumePerUnit (float) in `Assets/Features/Mining/Data/OreTypeBlob.cs`; after modification, verify that `Assets/Features/Mining/Systems/MiningBeamSystem.cs` and `Assets/Features/Mining/Systems/MiningActionDispatchSystem.cs` compile without errors (they should only read BaseYieldPerSecond/Hardness/VolumePerUnit from blob — if they reference removed Tier or Rarity fields, update those usages)
- [ ] T011 [US1] Modify OreTypeBlobBakingSystem: change SetOreDefinitions() parameter from OreTypeDefinition[] to OreDefinition[], update baking loop to read BaseYieldPerSecond/Hardness/VolumePerUnit from OreDefinition, update GetOreId() to use OreDefinition.OreId in `Assets/Features/Mining/Systems/OreTypeBlobBakingSystem.cs`
- [ ] T012 [US1] Modify OreTypeDatabaseInitializer: change serialized field from OreTypeDefinition[] to OreDefinition[], update call to OreTypeBlobBakingSystem.SetOreDefinitions() in `Assets/Features/Mining/Views/OreTypeDatabaseInitializer.cs`
- [ ] T013 [US1] Modify MiningBeamView: change serialized OreTypeDefinition[] to OreDefinition[], update beam color lookup to use OreDefinition.BeamColor by matching OreDefinition.OreId in `Assets/Features/Mining/Views/MiningBeamView.cs`
- [ ] T014 [P] [US1] Create Luminite.asset OreDefinition instance (OreId="luminite", DisplayName="Luminite", RarityTier=Common, RarityWeight=0.6, BaseYieldPerSecond=10, Hardness=1.0, VolumePerUnit=0.1, BeamColor=(0.6,0.85,1.0,1), BaseValue=10, BaseProcessingTimePerUnit=2.0) via MCP manage_scriptable_object in `Assets/Features/Mining/Data/Ores/`
- [ ] T015 [P] [US1] Create Ferrox.asset OreDefinition instance (OreId="ferrox", DisplayName="Ferrox", RarityTier=Uncommon, RarityWeight=0.3, BaseYieldPerSecond=7, Hardness=1.5, VolumePerUnit=0.15, BeamColor=(0.8,0.5,0.2,1), BaseValue=25, BaseProcessingTimePerUnit=5.0) via MCP manage_scriptable_object in `Assets/Features/Mining/Data/Ores/`
- [ ] T016 [P] [US1] Create Auralite.asset OreDefinition instance (OreId="auralite", DisplayName="Auralite", RarityTier=Rare, RarityWeight=0.1, BaseYieldPerSecond=5, Hardness=2.5, VolumePerUnit=0.25, BeamColor=(0.7,0.2,0.9,1), BaseValue=75, BaseProcessingTimePerUnit=10.0) via MCP manage_scriptable_object in `Assets/Features/Mining/Data/Ores/`
- [ ] T017 [US1] Verify compilation and run OreDefinition + baking tests via MCP — all pass, zero console errors

**Checkpoint**: Three OreDefinition assets created. Baking pipeline reads OreDefinition[]. Mining views reference OreDefinition[]. Designer can create new ore types via Create > VoidHarvest > Ore Definition.

---

## Phase 4: User Story 2 — Designer Configures Asteroid Belt Composition (Priority: P1)

**Goal**: Designers can author distinct asteroid fields via AsteroidFieldDefinition SOs with weighted ore entries, visual mapping, spatial parameters, and deterministic seeding. Multiple fields can coexist in the same scene.

**Independent Test**: Create two AsteroidFieldDefinition assets with different ore distributions (one rich in Luminite, one rich in Auralite), place both in a test scene with AsteroidFieldSpawner components, enter Play mode — visually confirm distinct compositions.

### Tests for User Story 2 (TDD — write FIRST, verify FAIL)

- [ ] T018 [P] [US2] Write TDD tests for weight normalization pure function: arbitrary weights (7,2,1) normalize to (0.7,0.2,0.1); single entry normalizes to 1.0; zero-weight entries excluded; all-zero-weight returns empty/warns; null OreDefinition entries skipped in `Assets/Features/Procedural/Tests/AsteroidFieldDefinitionTests.cs`
- [ ] T019 [US2] Write TDD tests for AsteroidFieldDefinition validation: AsteroidCount > 0, FieldRadius > 0, SizeMin <= SizeMax (auto-swap if violated), MinScaleFraction clamped to [0.1, 0.5], zero OreEntries logs warning and spawns nothing in `Assets/Features/Procedural/Tests/AsteroidFieldDefinitionTests.cs`

### Implementation for User Story 2

- [ ] T020 [P] [US2] Create OreFieldEntry serializable struct with fields: OreDefinition (OreDefinition reference), Weight (float), MeshVariantA (Mesh), MeshVariantB (Mesh), TintColor (Color) in namespace VoidHarvest.Features.Procedural.Data; include `// CONSTITUTION DEVIATION: [Serializable] struct (not readonly) — Unity serialization requires mutable fields for Inspector editing` comment in `Assets/Features/Procedural/Data/OreFieldEntry.cs`
- [ ] T021 [US2] Create AsteroidFieldDefinition ScriptableObject with all fields (FieldName string, OreEntries OreFieldEntry[], AsteroidCount int, FieldRadius float, AsteroidSizeMin float, AsteroidSizeMax float, RotationSpeedMin float, RotationSpeedMax float, Seed uint, MinScaleFraction float [Range(0.1,0.5)]) with `[CreateAssetMenu(menuName = "VoidHarvest/Asteroid Field Definition")]` and a pure static weight normalization method in `Assets/Features/Procedural/Data/AsteroidFieldDefinition.cs`
- [ ] T022 [US2] Create AsteroidFieldSpawner authoring MonoBehaviour (FieldDefinition reference) with Baker in `Assets/Features/Procedural/Views/AsteroidFieldSpawner.cs`; Baker creates the following baked ECS types (define as nested types or in the same file): AsteroidFieldConfigComponent (IComponentData: count int, radius float, seed uint, sizeMin/sizeMax float, rotMin/rotMax float), AsteroidOreWeightElement (IBufferElementData: normalizedWeight float, oreTypeIndex int), AsteroidVisualMappingElement (IBufferElementData: meshEntityA/B Entity, tintColor float4); also bake MinScaleFraction into an existing or new singleton component
- [ ] T023 [US2] Modify AsteroidFieldGeneratorJob: replace hard-coded AsteroidFieldConfig parameters with baked component data (count, radius, seed from AsteroidFieldConfigComponent; ore weights from AsteroidOreWeightElement buffer); keep Burst compilation and deterministic seeded RNG in `Assets/Features/Procedural/Systems/AsteroidFieldGeneratorJob.cs`
- [ ] T024 [US2] Modify AsteroidFieldSystem: read baked AsteroidFieldSpawner components instead of AsteroidFieldConfig.MvpDefault; create asteroid entities using visual data from AsteroidVisualMappingElement buffer (meshes, tints) instead of AsteroidVisualMappingConfig; use RenderMeshUtility.AddComponents for entity creation in `Assets/Features/Procedural/Systems/AsteroidFieldSystem.cs`
- [ ] T025 [US2] Modify AsteroidPrefabAuthoring: update Baker to consume OreFieldEntry visual data (MeshVariantA, MeshVariantB, TintColor) from the AsteroidFieldDefinition via AsteroidFieldSpawner references, bake mesh prefab entities per ore entry in `Assets/Features/Procedural/Views/AsteroidPrefabAuthoring.cs`
- [ ] T026 [US2] Create DefaultField.asset AsteroidFieldDefinition instance (FieldName="Default Asteroid Field", AsteroidCount=300, FieldRadius=2000, AsteroidSizeMin=3, AsteroidSizeMax=5, RotationSpeedMin=0, RotationSpeedMax=15, Seed=42, MinScaleFraction=0.3; OreEntries: Luminite weight=6 tint=(1.0,1.2,1.4,1), Ferrox weight=3 tint=(1.4,0.9,0.3,1), Auralite weight=1 tint=(1.1,0.3,1.2,1) with SF_Asteroids-M2 mesh variants) via MCP in `Assets/Features/Procedural/Data/Fields/`
- [ ] T027 [US2] Verify compilation and run weight normalization + field definition tests via MCP — all pass, zero console errors
- [ ] T027b [US2] Validate FR-009/SC-009: create a second AsteroidFieldDefinition asset (e.g., RareAuraliteBelt with AsteroidCount=50, Auralite weight=9, Luminite weight=1), place two AsteroidFieldSpawner GameObjects in AsteroidsSubScene referencing different definitions, enter Play mode, verify both fields spawn independently with distinct compositions in `Assets/Features/Procedural/Data/Fields/` and `Assets/Scenes/`

**Checkpoint**: AsteroidFieldDefinition with DefaultField.asset created. Spawner bakes config into ECS. Field generation uses data-driven ore entries. Designer can create distinct asteroid belts via Create > VoidHarvest > Asteroid Field Definition.

---

## Phase 5: User Story 3 — Player Mines New Ore Types Seamlessly (Priority: P1)

**Goal**: Full mining experience works end-to-end with Luminite, Ferrox, Auralite. Beam colors, yield rates, depletion visuals, ore chunks, audio feedback, and HUD display all function correctly. Zero regression from Spec 003 VFX/feedback systems.

**Independent Test**: Mine each of the three new ore types to depletion — verify beam color matches configured BeamColor, yield rate follows formula `(miningPower * baseYield * dt) / (hardness * (1 + depth))`, depletion visuals trigger at thresholds, ore chunks spawn correctly, audio plays, and inventory updates with correct ore ID and volume.

### Implementation for User Story 3

- [ ] T028 [US3] Wire AsteroidFieldSpawner component into AsteroidsSubScene: add GameObject with AsteroidFieldSpawner referencing DefaultField.asset, remove or replace legacy AsteroidFieldConfig-based spawning initialization in `Assets/Scenes/` (AsteroidsSubScene)
- [ ] T029 [US3] Wire OreTypeDatabaseInitializer in GameScene or ShipSubScene: set OreDefinition[] serialized array to reference Luminite.asset, Ferrox.asset, Auralite.asset (replacing legacy OreTypeDefinition references) in `Assets/Scenes/`
- [ ] T030 [US3] Wire MiningBeamView in scene: set OreDefinition[] serialized array to reference Luminite.asset, Ferrox.asset, Auralite.asset for beam color lookups in `Assets/Scenes/`
- [ ] T031 [US3] Enter Play mode and verify full mining pipeline: asteroid spawning with correct ore distribution, mining beam colors match ore BeamColor, HUD target panel shows correct DisplayName, yield rates proportional to BaseYieldPerSecond/Hardness, depletion visuals (scale shrink, emission glow, crumble thresholds) trigger correctly, ore chunks spawn with correct tint, audio feedback plays, inventory updates with correct OreId and VolumePerUnit
- [ ] T032 [US3] Run full EditMode + PlayMode test suite via MCP run_tests — verify zero regressions across mining, VFX, depletion, inventory, and HUD systems

**Checkpoint**: Full player mining experience works with Luminite, Ferrox, Auralite. All Spec 003 VFX/feedback systems function without regression. Scene loads and plays correctly.

---

## Phase 6: User Story 4 — Legacy System Fully Replaced (Priority: P2)

**Goal**: All legacy ore code, assets, and hard-coded spawning parameters are removed. Zero references to Veldspar, Scordite, Pyroxeres, OreTypeDefinition, AsteroidFieldConfig.MvpDefault, or AsteroidVisualMappingConfig remain in runtime code or assets.

**Independent Test**: Search the entire project for "veldspar", "scordite", "pyroxeres", "OreTypeDefinition", "AsteroidFieldConfig", "AsteroidVisualMappingConfig" — zero results in any C# source file or ScriptableObject asset.

### Implementation for User Story 4

- [ ] T033 [P] [US4] Delete legacy OreTypeDefinition ScriptableObject class file `Assets/Features/Mining/Data/OreTypeDefinition.cs` and its .meta file
- [ ] T034 [P] [US4] Delete legacy ore assets (Veldspar.asset, Scordite.asset, Pyroxeres.asset) and their .meta files from `Assets/Features/Mining/Data/` (locate exact paths via Glob search)
- [ ] T035 [P] [US4] Delete legacy AsteroidFieldConfig record file (and OreDistribution struct if defined in the same file, or its own file if separate) `Assets/Features/Procedural/Data/AsteroidFieldConfig.cs` and any associated .meta files; verify via Glob that no OreDistribution type remains
- [ ] T036 [P] [US4] Delete legacy AsteroidVisualMappingConfig ScriptableObject class file `Assets/Features/Procedural/Data/AsteroidVisualMappingConfig.cs` and its .meta file
- [ ] T037 [P] [US4] Delete legacy AsteroidVisualMapping.asset and its .meta file from `Assets/Features/Procedural/Data/` (locate exact path via Glob search)
- [ ] T038 [US4] Update all existing test files: replace legacy ore IDs ("veldspar", "scordite", "pyroxeres") with new IDs ("luminite", "ferrox", "auralite"); remove references to deleted types (OreTypeDefinition, AsteroidFieldConfig, AsteroidVisualMappingConfig) in `Assets/Features/Mining/Tests/` and `Assets/Features/Procedural/Tests/`
- [ ] T039 [US4] Search entire codebase (Grep for veldspar|scordite|pyroxeres|OreTypeDefinition|AsteroidFieldConfig|AsteroidVisualMappingConfig|MvpDefault) and remove all remaining references in any C# files, .asset files, or scene files
- [ ] T040 [US4] Verify compilation — zero errors; run full test suite — all pass; confirm zero legacy references via codebase-wide Grep search

**Checkpoint**: Legacy ore system completely removed. Project compiles cleanly with only the new data-driven system. Zero legacy references remain.

---

## Phase 7: User Story 5 — Player Documentation Reflects New Ores (Priority: P2)

**Goal**: All player-facing documentation references the new ore types (Luminite, Ferrox, Auralite) with correct rarity tiers, descriptions, and gameplay characteristics. Per Constitution v1.3.0 mandate.

**Independent Test**: Read HOWTOPLAY.md — verify all ore references are Luminite/Ferrox/Auralite with correct rarity descriptions (Common/Uncommon/Rare). Read README.md — verify updated feature overview. Check CHANGELOG.md for Spec 005 entry.

### Implementation for User Story 5

- [ ] T041 [P] [US5] Update HOWTOPLAY.md: replace all Veldspar/Scordite/Pyroxeres references with Luminite (Common, ice-blue, high yield, low hardness) / Ferrox (Uncommon, bronze-orange, medium yield, medium hardness) / Auralite (Rare, violet, low yield, high hardness); update mining tips and ore-specific gameplay guidance in `HOWTOPLAY.md`
- [ ] T042 [P] [US5] Update README.md "What's Implemented" section: mention data-driven ore system (OreDefinition ScriptableObjects, 3 ore types), configurable asteroid fields (AsteroidFieldDefinition ScriptableObjects), and designer-expandable architecture (zero code changes to add ores/fields) in `README.md`
- [ ] T043 [P] [US5] Add Spec 005 changelog entry documenting migration from legacy ores (Veldspar/Scordite/Pyroxeres) to data-driven OreDefinition system (Luminite/Ferrox/Auralite), replacement of hard-coded AsteroidFieldConfig with AsteroidFieldDefinition SOs, and designer-expandable architecture in `CHANGELOG.md`

**Checkpoint**: All player-facing documentation reflects the new ore system. No references to legacy ores remain in docs.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, performance verification, and quickstart scenario testing

- [ ] T044 Verify Unity console shows zero errors and zero warnings after full migration via MCP read_console
- [ ] T045 Run full EditMode + PlayMode test suite via MCP run_tests — all tests pass with zero failures; include edge case regression: zero baseYieldPerSecond ore produces no yield (beam connects but extracts nothing)
- [ ] T046 Performance validation: create a test AsteroidFieldDefinition with AsteroidCount=500, spawn in scene, verify single-frame completion (NFR-001) via Unity Profiler or frame timing
- [ ] T047 Performance validation: mine ore continuously and verify zero GC allocations per frame in hot path (NFR-002) via Unity Profiler
- [ ] T048 Validate quickstart.md scenarios end-to-end: create a 4th OreDefinition asset (e.g., Titanite), add it to DefaultField.asset's OreEntries, enter Play mode, verify it spawns and is mineable — all with zero code changes (NFR-003, SC-001, SC-010)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational — OreDefinition class must exist and compile
- **US2 (Phase 4)**: Depends on US1 — OreDefinition asset instances must exist for OreFieldEntry references and DefaultField.asset
- **US3 (Phase 5)**: Depends on US1 + US2 — both ore definitions and field spawning must be functional
- **US4 (Phase 6)**: Depends on US3 — all new systems must be wired and validated before deleting legacy code
- **US5 (Phase 7)**: Depends on US4 — legacy cleanup must be complete before documenting final state
- **Polish (Phase 8)**: Depends on all previous phases

### User Story Dependencies

```
Setup → Foundational → US1 → US2 → US3 → US4 → US5 → Polish
                        │      │      │
                     (ore    (field  (scene
                     types)  defs)   wiring)
```

- **US1 (P1)**: Starts after Foundational — establishes ore baking pipeline and ore assets
- **US2 (P1)**: Starts after US1 — needs OreDefinition assets for OreFieldEntry references
- **US3 (P1)**: Starts after US2 — needs field spawning for scene integration
- **US4 (P2)**: Starts after US3 — must verify everything works before deleting legacy
- **US5 (P2)**: Starts after US4 — document final state after cleanup

### Within Each User Story

1. TDD tests written FIRST and verified to FAIL (Red)
2. Data types before systems (types must compile before use)
3. Systems before views (logic before presentation)
4. Code compiles before assets created (SO class must exist for .asset creation)
5. Compilation verification after code changes (MCP read_console)
6. Test verification after implementation (Green)

### Parallel Opportunities

**Phase 1**: T001 ‖ T002 (independent folders)
**Phase 2**: T003 then T004 (same file, sequential); T005 [P] with T003/T004 (independent source file); T006 after T005 (depends on OreRarityTier)
**US1**: T008 then T009 (same file, sequential); T014 ‖ T015 ‖ T016 (asset creation — after code compiles)
**US2**: T018 then T019 (same file, sequential); T020 parallel with tests (independent file)
**US4**: T033 ‖ T034 ‖ T035 ‖ T036 ‖ T037 (all deletions independent)
**US5**: T041 ‖ T042 ‖ T043 (all doc files independent)

---

## Parallel Example: User Story 1

```text
# TDD Red phase — write tests in parallel:
T008: "TDD tests for OreTypeBlob struct in Mining/Tests/OreDefinitionTests.cs"
T009: "TDD tests for OreTypeBlobBakingSystem in Mining/Tests/OreDefinitionTests.cs"

# After T010-T013 implemented (Green phase), create assets in parallel:
T014: "Create Luminite.asset in Mining/Data/Ores/"
T015: "Create Ferrox.asset in Mining/Data/Ores/"
T016: "Create Auralite.asset in Mining/Data/Ores/"
```

## Parallel Example: User Story 4

```text
# All legacy deletions run in parallel:
T033: "Delete OreTypeDefinition.cs"
T034: "Delete Veldspar/Scordite/Pyroxeres assets"
T035: "Delete AsteroidFieldConfig.cs"
T036: "Delete AsteroidVisualMappingConfig.cs"
T037: "Delete AsteroidVisualMapping.asset"
# Then sequential cleanup:
T038-T040: "Update tests, search+remove references, verify"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (create folders)
2. Complete Phase 2: Foundational (OreRarityTier + OreDefinition class)
3. Complete Phase 3: US1 (baking pipeline + ore assets + view updates)
4. **STOP and VALIDATE**: OreDefinition assets exist, bake correctly, mining views reference new types
5. This alone delivers the "designer creates ore types" capability

### Incremental Delivery

1. Setup + Foundational → Data contracts established
2. Add US1 → Ore types work in baking pipeline → Validate independently
3. Add US2 → Asteroid fields use data-driven spawning → Validate independently
4. Add US3 → Full player mining experience verified → Validate independently
5. Add US4 → Legacy completely removed → Validate independently
6. Add US5 → Documentation updated → Validate independently
7. Polish → Performance + quickstart validated → Ship

### Migration Safety (Create-Then-Swap-Then-Delete)

- **Phases 1-3 (US1)**: Additive only — new types and assets created alongside legacy. No breaking changes.
- **Phase 4 (US2)**: Systems updated to consume new types. Old AsteroidFieldConfig usage replaced.
- **Phase 5 (US3)**: Scenes wired to new system. New pipeline fully active.
- **Phase 6 (US4)**: Legacy deletion — only after new system is validated end-to-end.
- Each phase compiles and passes tests before proceeding to the next.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in the current phase
- [Story] label maps task to specific user story for traceability
- TDD is mandatory: write tests FIRST (Red), verify FAIL, implement (Green), refactor
- Verify Unity compilation (MCP read_console) after every code change phase
- ScriptableObject .asset instances can only be created AFTER their C# class compiles successfully
- Asset creation uses MCP manage_scriptable_object or Unity Editor Inspector
- Commit after each completed phase
- Stop at any checkpoint to validate the current increment independently
