# Feature Specification: Data-Driven World Config

**Feature Branch**: `009-data-driven-world-config`
**Created**: 2026-03-03
**Status**: Draft
**Input**: Make all remaining hard-coded game entity configuration data-driven via ScriptableObjects. Create StationDefinition SOs, WorldDefinition SO, DockingConfigBlob, CameraConfig SO, InteractionConfig SO, inventory capacity from ship archetype, editor tooling, and asset reorganization.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Designer Configures Stations via ScriptableObjects (Priority: P1)

A game designer wants to add, modify, or remove stations from the game world without touching any C# code. They open the Unity Editor, create or edit a StationDefinition ScriptableObject asset, fill in the station's name, type, world position, available services, docking port offset, and visual references. They then add or remove that StationDefinition from a WorldDefinition asset. On entering Play mode, the game world reflects the updated station roster and configuration.

**Why this priority**: Station data is currently hard-coded in C#, making content iteration impossible for non-programmers. This is the single largest data-driving gap blocking designer workflows.

**Independent Test**: Can be fully tested by creating a new StationDefinition asset, adding it to a WorldDefinition, entering Play mode, and verifying the station appears at the correct position with correct services. Delivers designer-driven world authoring.

**Acceptance Scenarios**:

1. **Given** a WorldDefinition asset with two StationDefinition references, **When** the game initializes, **Then** `WorldState.Stations` contains exactly two entries matching the SO data (IDs, positions, names, services).
2. **Given** a designer adds a third StationDefinition to the WorldDefinition, **When** the game initializes, **Then** three stations exist in `WorldState.Stations` with no code changes required.
3. **Given** a StationDefinition with a ServicesConfig reference, **When** the player docks at that station, **Then** the station services menu displays the services listed in the StationDefinition's AvailableServices array with the linked ServicesConfig parameters.
4. **Given** a StationDefinition with DockingPortOffset and DockingPortRotation values, **When** the player docks, **Then** the ship snaps to the correct docking port position defined by the SO.

---

### User Story 2 - Docking Parameters Are Fully Data-Driven (Priority: P2)

A designer wants to tune docking behavior (snap duration, approach timeout, undock clearance distance, alignment thresholds) by editing a ScriptableObject, without recompiling the Burst-compiled docking system. They modify the DockingConfig SO fields and the values propagate through a blob asset pipeline into the ECS docking system at runtime.

**Why this priority**: Hard-coded constants inside the Burst-compiled DockingSystem prevent any designer tuning of docking feel. This is a correctness issue where the existing DockingConfig SO is partially ignored.

**Independent Test**: Can be tested by modifying DockingConfig SO values, entering Play mode, and verifying that docking snap duration, approach timeout, and alignment thresholds match the SO values rather than hard-coded defaults.

**Acceptance Scenarios**:

1. **Given** a DockingConfig SO with SnapDuration set to 1.5s (instead of the hard-coded 0.75s), **When** the player docks, **Then** the snap animation takes approximately 1.5 seconds.
2. **Given** a DockingConfig SO with ApproachTimeout set to 60s, **When** the player initiates docking but remains outside snap range, **Then** docking is cancelled after 60 seconds (not the hard-coded 120s).
3. **Given** the DockingConfigBlob is baked from the SO during initialization (managed SystemBase pattern), **When** the game runs, **Then** the DockingSystem reads all parameters from the blob singleton component with zero managed object access.

---

### User Story 3 - Camera Limits Are Designer-Tunable (Priority: P3)

A designer wants to adjust camera orbit limits (pitch range, zoom distance, sensitivity, cooldowns) without editing C# code. They edit a CameraConfig ScriptableObject and the camera system respects the new limits immediately on the next Play session.

**Why this priority**: Camera feel is critical for player experience, but limits are currently buried in code constants. Making these tunable enables rapid iteration during playtesting.

**Independent Test**: Can be tested by creating a CameraConfig SO with custom pitch/distance limits, assigning it in the scene, and verifying the camera clamps to the configured ranges during orbit and zoom.

**Acceptance Scenarios**:

1. **Given** a CameraConfig SO with MinPitch=-60 and MaxPitch=60, **When** the player orbits the camera, **Then** pitch is clamped to [-60, 60] instead of the previous [-80, 80].
2. **Given** a CameraConfig SO with MinZoomDistance=15 and MaxZoomDistance=35, **When** the player scrolls the mouse wheel, **Then** zoom is clamped within [15, 35].
3. **Given** a CameraConfig SO with ZoomCooldownDuration=3.0, **When** the player stops scrolling, **Then** the zoom cooldown lasts 3 seconds before smooth zoom interpolation resumes.

---

### User Story 4 - Inventory Capacity Derives from Ship Archetype (Priority: P4)

A designer configures cargo slots and volume on each ShipArchetypeConfig SO. When the game starts, the player's inventory capacity matches the starting ship's archetype values rather than hard-coded defaults.

**Why this priority**: Hard-coded inventory limits break the ship differentiation model where different ship archetypes should have different cargo capacities.

**Independent Test**: Can be tested by setting different CargoSlots and CargoCapacity values on a ShipArchetypeConfig, starting the game, and verifying InventoryState reflects those values.

**Acceptance Scenarios**:

1. **Given** a StartingShipArchetype with CargoSlots=15 and CargoCapacity=80, **When** the game initializes, **Then** InventoryState.MaxSlots=15 and InventoryState.MaxVolume=80.
2. **Given** a ShipArchetypeConfig with CargoSlots=0 (invalid), **When** the asset is saved in the editor, **Then** OnValidate logs a warning and clamps to a minimum of 1.

---

### User Story 5 - Input and Interaction Timing Is Configurable (Priority: P5)

A designer adjusts double-click window, radial menu drag threshold, and default approach/orbit/keep-at-range distances via an InteractionConfig SO. The input system and radial menu respect these values without code changes.

**Why this priority**: Input feel tuning requires rapid iteration. Hard-coded timing constants slow down UX polish.

**Independent Test**: Can be tested by modifying InteractionConfig SO values and verifying that double-click detection timing and radial menu default distances match the configured values.

**Acceptance Scenarios**:

1. **Given** an InteractionConfig with DoubleClickWindow=0.5, **When** the player clicks twice within 0.5s, **Then** a double-click action is registered.
2. **Given** an InteractionConfig with DefaultOrbitDistance=200, **When** the player selects "Orbit" from the radial menu, **Then** the orbit command uses 200m as the default distance.

---

### User Story 6 - Editor Validates Scene Configuration Completeness (Priority: P6)

A designer or developer opens the Scene Config Validator editor window and immediately sees which configuration fields are properly assigned and which are missing. Missing fields are highlighted with warnings. The WorldDefinition custom editor shows station completeness at a glance.

**Why this priority**: Missing SO references cause silent null-skip failures at runtime. Editor tooling catches misconfigurations before Play mode.

**Independent Test**: Can be tested by deliberately removing a config reference from SceneLifetimeScope, running the validator, and verifying it reports the missing field.

**Acceptance Scenarios**:

1. **Given** a SceneLifetimeScope with one null config field, **When** the designer runs VoidHarvest > Validate Scene Config, **Then** the validator reports the missing field by name with a warning.
2. **Given** a WorldDefinition with a StationDefinition that has a null ServicesConfig, **When** the designer clicks "Validate All" in the WorldDefinition inspector, **Then** a warning identifies the incomplete station.
3. **Given** all config fields are properly assigned, **When** the validator runs, **Then** all items show green/pass status.

---

### User Story 7 - Station Assets Are Organized Consistently (Priority: P7)

After migration, all station-related assets live under a consistent folder hierarchy. Designers can find station definitions, service configs, presets, and raw materials in predictable locations.

**Why this priority**: Asset discoverability is important for team productivity, but has lower urgency than functional gaps. This is organizational polish.

**Independent Test**: Can be tested by verifying folder structure matches the convention and all SO cross-references remain intact after asset moves.

**Acceptance Scenarios**:

1. **Given** the migration is complete, **When** a designer navigates to Assets/Features/Station/Data/Definitions/, **Then** they find SmallMiningRelay.asset and MediumRefineryHub.asset.
2. **Given** assets were moved, **When** the game enters Play mode, **Then** all SO references resolve correctly (no missing references).

---

### Edge Cases

- What happens when a WorldDefinition has zero stations? The game initializes with an empty station list and logs a warning.
- What happens when two StationDefinitions have the same StationId? OnValidate on WorldDefinition flags the duplicate and logs an error.
- What happens when a StationDefinition references a null ServicesConfig? The station appears in the world but services are unavailable; a runtime warning is logged.
- What happens when CameraConfig has MinZoomDistance > MaxZoomDistance? OnValidate corrects or warns about the inconsistency.
- What happens when DockingConfig values produce degenerate behavior (e.g., SnapDuration=0)? OnValidate enforces minimum sensible values.
- What happens when ShipArchetypeConfig.CargoSlots is set to an extremely large value? OnValidate allows it (no hard upper limit) but logs an informational note above a soft threshold.
- What happens when the StationServicesConfigMap (deprecated) is still referenced somewhere after migration? It is deleted, so any remaining references cause compile errors that must be resolved.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a StationDefinition ScriptableObject that consolidates station identity (ID, name, description, type), world placement (position, rotation), services (available service list, services config reference), docking (port offset, port rotation, safe undock direction), and visuals (prefab, icon) into a single asset.
- **FR-002**: System MUST provide a WorldDefinition ScriptableObject that holds an array of StationDefinition references plus player start position, start rotation, and starting ship archetype reference.
- **FR-003**: Game initialization MUST build WorldState.Stations from the WorldDefinition asset instead of hard-coded C# values.
- **FR-004**: Station services resolution MUST use StationDefinition.ServicesConfig instead of the StationServicesConfigMap lookup.
- **FR-005**: System MUST provide a DockingConfigBlob blob asset that carries all docking parameters into the Burst-compiled DockingSystem, replacing hard-coded local constants.
- **FR-006**: The DockingConfig ScriptableObject MUST include all parameters currently hard-coded in DockingSystem (ApproachTimeout, AlignTimeout, AlignDotThreshold, AlignAngVelThreshold, SnapRange).
- **FR-007**: A DockingConfigBlobBakingSystem (managed SystemBase in InitializationSystemGroup, following the established OreTypeBlobBakingSystem pattern) MUST bake the DockingConfig SO into a DockingConfigBlobComponent singleton entity.
- **FR-008**: System MUST provide a CameraConfig ScriptableObject with pitch limits, distance limits, zoom distance limits, zoom cooldown, default orientation, and orbit sensitivity.
- **FR-009**: CameraReducer MUST read camera limits from state (initialized from CameraConfig SO) instead of compile-time constants.
- **FR-010**: CameraView MUST read zoom cooldown from CameraConfig instead of a compile-time constant.
- **FR-011**: ShipArchetypeConfig MUST include a CargoSlots (int) field alongside the existing CargoCapacity (volume) field.
- **FR-012**: Game initialization MUST derive InventoryState.MaxSlots and MaxVolume from the WorldDefinition.StartingShipArchetype config.
- **FR-013**: System MUST provide an InteractionConfig ScriptableObject with double-click window, radial menu drag threshold, default approach/orbit/keep-at-range distances, and mining beam max range.
- **FR-014**: InputBridge MUST read timing constants from InteractionConfig instead of compile-time constants.
- **FR-015**: RadialMenuController MUST read default distances from InteractionConfig instead of compile-time constants.
- **FR-016**: System MUST provide a SceneConfigValidator editor window (menu: VoidHarvest > Validate Scene Config) that checks all serialized config fields on SceneLifetimeScope and reports missing assignments.
- **FR-017**: System MUST provide a WorldDefinition custom editor that shows an inline station list with completeness validation and warning badges.
- **FR-018**: All new ScriptableObjects MUST include OnValidate methods that enforce field constraints and log warnings for invalid configurations.
- **FR-019**: All new ScriptableObjects MUST use CreateAssetMenu with paths following the VoidHarvest/System/Asset Type convention.
- **FR-020**: StationServicesConfigMap MUST be removed and its usage replaced by StationDefinition.ServicesConfig lookups.
- **FR-021**: Migration MUST create StationDefinition assets (SmallMiningRelay, MediumRefineryHub) and a WorldDefinition asset (DefaultWorld) with all current hard-coded values preserved.
- **FR-022**: All existing tests MUST continue to pass after migration.
- **FR-023**: Station-related assets MUST be reorganized into a consistent folder hierarchy under Assets/Features/Station/Data/ and Assets/Features/World/Data/.
- **FR-024**: TargetableStation MonoBehaviour MUST derive its TargetId from the associated StationDefinition.StationId field.
- **FR-025**: DockingPortComponent MonoBehaviour MUST derive its StationId from an associated StationDefinition ScriptableObject reference, replacing the manually-assigned integer field.
- **FR-026**: DockingPortComponent's DockingRange and SnapRange fields MUST be removed — the DockingSystem reads these values from the DockingConfigBlob singleton, making per-component fields redundant.
- **FR-027**: StationServicesConfig ScriptableObject MUST be moved from the StationServices assembly to the Station assembly to break the circular dependency between StationDefinition and StationServices consumers.

### Key Entities

- **StationDefinition**: A ScriptableObject that is the single source of truth for all configuration of one station — identity, world placement, services, docking port, and visuals. Referenced by WorldDefinition and used at initialization and runtime.
- **WorldDefinition**: A ScriptableObject that defines the complete station roster for a game world, plus player starting conditions. Used by game initialization to build the immutable WorldState.
- **DockingConfigBlob**: A blob asset that carries docking tuning parameters into Burst-compiled ECS systems. Baked from the existing DockingConfig SO via a managed SystemBase baking system (following the OreTypeBlobBakingSystem pattern).
- **CameraConfig**: A ScriptableObject holding all camera orbit and zoom limits. Injected into the camera system at initialization to replace compile-time constants.
- **InteractionConfig**: A ScriptableObject holding input timing and default interaction distances. Injected into InputBridge and RadialMenuController.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A designer can add a new station to the game world by creating a StationDefinition asset and adding it to the WorldDefinition — zero C# code changes required.
- **SC-002**: All docking behavior parameters are editable via the DockingConfig ScriptableObject with changes reflected in gameplay after re-entering Play mode.
- **SC-003**: Camera orbit and zoom limits are adjustable via a single CameraConfig asset, with changes reflected in-game on the next Play session.
- **SC-004**: Inventory capacity (slots and volume) automatically matches the starting ship's archetype configuration.
- **SC-005**: Input timing and default interaction distances are tunable via InteractionConfig with no code changes.
- **SC-006**: The Scene Config Validator detects 100% of null/missing config references on SceneLifetimeScope before entering Play mode.
- **SC-007**: All existing tests pass without modification to test logic (test data setup may be updated to use new SO types).
- **SC-008**: Zero hard-coded station data remains in C# source files after migration.
- **SC-009**: New unit tests cover all OnValidate logic, blob baking, and world initialization from WorldDefinition, achieving full coverage of new code paths.

## Assumptions

- Option (A) for CameraReducer config injection is used: camera limits are embedded in CameraState at initialization, keeping the reducer pure static.
- StationServicesConfigMap is deleted (not just deprecated) once all references are migrated.
- Asset moves use Unity's built-in move mechanism to preserve GUIDs, avoiding broken references.
- The DockingConfigBlob follows the same baking pattern as OreTypeBlobDatabase (established in Spec 005).
- StationDefinition.StationType enum starts with the types mentioned in the prompt (MiningRelay, RefineryHub, TradePost, ResearchStation) and can be extended later.
- The existing StationData readonly struct in WorldState is retained but populated from StationDefinition SO data instead of hard-coded values.

## Dependencies

- **Spec 008** (Bugfix & Polish): MUST be completed first — fixes event lifecycle and UI bugs that this spec's wiring depends on. (Status: COMPLETE)
- **Existing test suite**: 521 tests (520 pass, 1 pre-existing screen-res-dependent) must remain stable.

## Scope Boundaries

### In Scope
- StationDefinition and WorldDefinition ScriptableObjects with full inspector UX
- DockingConfigBlob blob asset pipeline
- CameraConfig ScriptableObject
- InteractionConfig ScriptableObject
- Inventory capacity from ShipArchetypeConfig
- SceneConfigValidator editor window
- WorldDefinition custom editor
- Migration of hard-coded station data to SO assets
- Removal of StationServicesConfigMap
- Asset folder reorganization
- Unit tests for all new SOs, blob baking, OnValidate, and world initialization

### Out of Scope
- New station types or services beyond current two
- Station runtime spawning/despawning
- Custom property drawers for nested types
- Per-feature asset browser editor windows
- "Add New Station" wizard dialogs
- Dynamic world generation
