# Feature Specification: Premium Visuals Asset Integration

**Feature Branch**: `002-premium-visuals-integration`
**Created**: 2026-02-27
**Status**: Draft
**Input**: Integrate four purchased premium Unity Asset Store packs (SF MINING Asteroids M2, Nebula Skybox Pack Vol. II, Modular Space Ship Pack, MODULAR Space Station #MS2) into VoidHarvest for EVE Online-grade industrial sci-fi visuals while preserving existing systems and performance targets.

## Clarifications

### Session 2026-02-27

- Q: How do the 3 new barge variants relate to the existing StarterMiningBarge? → A: Rename/upgrade — StarterMiningBarge becomes the Small Barge (same ID, new visuals, stats may be tuned). Medium and Heavy are new additions.
- Q: How should asteroid ore types be visually differentiated? → A: Mesh + tint — 2 dedicated meshes per ore type (Veldspar = meshes 1-2, Scordite = 3-4, Pyroxeres = 5-6) AND a subtle material color tint matching the ore's beam color.
- Q: How should the skybox nebula variant be selected per scene? → A: Designer-configured per scene — each scene references a specific nebula via its SkyboxConfig asset.
- Q: How should asteroid shrinking behave during depletion? → A: Continuous with steps — smooth scale interpolation proportional to remaining resources, with visible "crumble" pauses at 75%, 50%, and 25% depletion thresholds.
- Q: How should a fully depleted asteroid be removed? → A: Crumble-and-fade — final crumble pause at 0% triggers a brief fade-out before removal.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Immersive Space Environment (Priority: P1)

As a player entering the game, I see a rich, detailed nebula skybox surrounding the asteroid field. The environment feels vast and atmospheric, with proper lighting that makes ships and asteroids look like they belong in a deep-space mining operation. The skybox rotates slowly, creating a sense of scale and motion. Exposure and ambient lighting adjust to match the chosen nebula, ensuring consistent visual quality across all 12 available environments.

**Why this priority**: The skybox is the visual foundation — every other asset renders against it. Getting the environment right first establishes the visual tone and ensures all subsequent asset work (asteroids, ships, stations) looks cohesive from the start.

**Independent Test**: Can be fully tested by loading any scene and verifying the nebula skybox renders correctly with appropriate ambient lighting. Delivers immediate visual upgrade with zero impact on gameplay systems.

**Acceptance Scenarios**:

1. **Given** the game scene loads, **When** the player looks around, **Then** they see a high-resolution nebula skybox with no visible seams or compression artifacts.
2. **Given** a nebula skybox is active, **When** time passes, **Then** the skybox rotates slowly creating a sense of cosmic drift.
3. **Given** any of the 12 nebula variants is selected, **When** the scene renders, **Then** ambient lighting and exposure are consistent with the chosen nebula's color palette.
4. **Given** the skybox is rendering, **When** the player observes ships and asteroids, **Then** the lighting from the environment illuminates them naturally (no flat or mismatched lighting).

---

### User Story 2 - Detailed Mineral Asteroids (Priority: P2)

As a player flying through an asteroid field, I see detailed, textured mineral asteroids with visible surface features and specular highlights instead of placeholder geometry. When I target an asteroid for mining, the mineral veins and surface detail are clearly visible at mining range. Asteroids vary in size proportional to their mass — larger rocks have more to mine. As I mine, the asteroid visibly shrinks with occasional crumble pauses at depletion thresholds, giving tactile feedback that I'm making progress. Once fully depleted, the asteroid crumbles one final time, fades away, and is removed from the field.

**Why this priority**: Asteroids are the core gameplay object — the player stares at them constantly while mining. Replacing placeholder geometry with the premium SF MINING Asteroids M2 meshes delivers the single biggest visual quality improvement to the core loop.

**Independent Test**: Can be fully tested by spawning an asteroid field and visually inspecting asteroid meshes, materials, and level-of-detail transitions at various distances. Delivers immediate visual quality improvement to the mining experience.

**Acceptance Scenarios**:

1. **Given** an asteroid field spawns, **When** the player observes it, **Then** asteroids display detailed mineral textures with visible veins and specular highlights.
2. **Given** all 6 asteroid mesh variants exist, **When** the field generates, **Then** asteroids use a varied mix of mesh variants (no obvious repetition in nearby clusters).
3. **Given** an asteroid is at mining range, **When** the player targets it, **Then** mineral surface detail is clearly visible and visually distinct per ore type.
4. **Given** asteroids are at long range, **When** the player looks at the field, **Then** asteroids transition smoothly to lower detail levels without visible popping.
5. **Given** asteroids spawn with varying mass values, **When** the player observes the field, **Then** each asteroid's visual size is proportional to its mass — higher-mass asteroids appear larger.
6. **Given** a player is actively mining an asteroid, **When** resources are extracted, **Then** the asteroid smoothly shrinks in proportion to remaining resources, with a visible crumble pause at each 25% depletion threshold (75%, 50%, 25% remaining).
7. **Given** an asteroid reaches 0% remaining resources, **When** it becomes fully depleted, **Then** a final crumble pause occurs followed by a brief fade-out, after which the asteroid is removed from the field entirely.
8. **Given** 300 asteroids are spawned (current max), **When** the game is running, **Then** asteroid field rendering stays within the 2 ms performance budget.

---

### User Story 3 - Mining Barge Fleet Variants (Priority: P3)

As a player, I pilot one of three visually distinct mining barge variants (Small, Medium, Heavy) built from modular ship components. Each variant has a different silhouette reflecting its role — the small barge is nimble and compact, the medium is a balanced workhorse, and the heavy is a bulky industrial powerhouse. All three are immediately flyable with the existing EVE-style controls.

**Why this priority**: The player's ship is their primary avatar. Having three distinct visual variants establishes the fleet progression fantasy and demonstrates the modular ship system's potential, even before the full fleet swap mechanic (Phase 1) is implemented.

**Independent Test**: Can be fully tested by spawning each barge variant and flying it through the asteroid field using existing controls. Delivers visual ship variety and a sense of progression.

**Acceptance Scenarios**:

1. **Given** the game loads, **When** the player's ship spawns, **Then** it displays a detailed modular mining barge model (not placeholder geometry).
2. **Given** three barge variants exist (Small, Medium, Heavy), **When** compared side by side, **Then** each has a distinct silhouette and visual mass reflecting its role.
3. **Given** any barge variant is active, **When** the player uses EVE-style controls (click-to-align, keyboard thrust, radial menu), **Then** the ship responds identically to the existing flight model.
4. **Given** a barge variant is flying, **When** the camera orbits it, **Then** the model looks correct from all angles with no gaps, z-fighting, or misaligned modular parts.

---

### User Story 4 - Modular Space Station (Priority: P4)

As a player approaching a station, I see a large, detailed modular space station composed of interconnected modules (refinery, hangar, docking ring, habitat, power). The station establishes the industrial sci-fi atmosphere and provides a visible landmark in the asteroid field. Two station presets are available — a Small Mining Relay and a Medium Refinery Hub — each assembled from the modular station components with a distinct layout.

**Why this priority**: Stations ground the game world and provide a destination beyond asteroids. While gameplay interaction with stations is Phase 1+, their visual presence is critical for establishing the EVE Online-inspired industrial setting.

**Independent Test**: Can be fully tested by loading a test scene containing each station preset and visually verifying module assembly, material quality, and performance. Delivers environmental atmosphere and world-building.

**Acceptance Scenarios**:

1. **Given** a station preset is placed in a scene, **When** the player views it, **Then** the station displays as a coherent assembly of modular components with no gaps or floating parts.
2. **Given** two station presets exist, **When** compared, **Then** the Small Mining Relay is visibly compact (3-5 modules) while the Medium Refinery Hub is larger and more complex (8-12 modules).
3. **Given** a station is in the scene, **When** the player flies near it, **Then** station materials render correctly with proper lighting and no visual artifacts.
4. **Given** a populated station scene, **When** the game is running, **Then** the station renders within the 5 ms performance budget.

---

### Edge Cases

- What happens when a material fails to convert to the current rendering pipeline? The system must detect and report unconverted materials rather than rendering pink/magenta fallbacks silently.
- What happens when the skybox HDRI fails to load? The system must fall back to the existing `SpaceSkybox.mat` rather than rendering a black void.
- What happens when an asteroid mesh variant is missing or corrupted? The procedural spawner must skip the variant and use remaining variants rather than spawning invisible or error geometry.
- What happens when station modules are placed with slight misalignment? Snapping helpers must enforce grid-aligned placement to prevent visible gaps.
- What happens when all three barge variants are loaded simultaneously (e.g., for preview)? Memory usage must remain within acceptable bounds.
- What happens when a player stops mining an asteroid mid-depletion? The asteroid must remain at its current shrunken size and resume shrinking if mining resumes — it must not snap back to full size.
- What happens when an asteroid is at a crumble pause threshold and the player stops mining? The crumble pause completes its animation, then the asteroid holds at that size until mining resumes.
- What happens when a depleted asteroid is in the process of fading out and the player attempts to target it? The asteroid must not be targetable once fade-out begins.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: All premium asset materials MUST render correctly under the current rendering pipeline with no pink/magenta fallback materials visible.
- **FR-002**: The skybox system MUST support loading any of the 12 Nebula Vol. II HDRI environments and applying them as the scene skybox with matched ambient lighting. Each scene references a specific nebula via a designer-configured SkyboxConfig asset.
- **FR-003**: The skybox MUST rotate slowly at a configurable speed to create a sense of cosmic drift.
- **FR-004**: The skybox system MUST expose configurable parameters (rotation speed, exposure, nebula variant selection) via a per-scene designer-editable SkyboxConfig asset.
- **FR-005**: The skybox system MUST fall back to the existing SpaceSkybox material if the selected HDRI fails to load.
- **FR-006**: The asteroid field spawner MUST use the 6 SF MINING Asteroids M2 mesh/material variants when generating asteroid fields.
- **FR-007**: The asteroid spawner MUST distribute mesh variants with visual variety — no more than 3 identical meshes within a 200-unit radius cluster.
- **FR-008**: Asteroids MUST display distinct visual characteristics per ore type using dedicated mesh assignment (2 meshes per ore type: Veldspar = meshes 1-2, Scordite = 3-4, Pyroxeres = 5-6) combined with a subtle material color tint matching each ore's beam color, so players can visually identify ore by both silhouette and color at mining range.
- **FR-009**: Asteroids MUST use level-of-detail groups that transition smoothly at distance without visible popping artifacts.
- **FR-018**: Asteroid visual size MUST be proportional to the asteroid's mass — higher-mass asteroids render larger than lower-mass asteroids.
- **FR-019**: Asteroids MUST visibly shrink as they are depleted by mining. Shrinking MUST be continuous (scale proportional to remaining resources) with visible crumble pauses at 75%, 50%, and 25% remaining resource thresholds.
- **FR-020**: Fully depleted asteroids (0% remaining resources) MUST trigger a final crumble pause followed by a brief fade-out, after which the asteroid is removed from the field entirely.
- **FR-021**: Asteroid removal MUST update the field state so removed asteroids no longer occupy space, respond to targeting, or consume rendering budget.
- **FR-010**: Three mining barge ship variants (Small, Medium, Heavy) MUST be available, each assembled from Modular Space Ship Pack components with a distinct silhouette. The existing StarterMiningBarge archetype MUST be renamed/upgraded to become the Small Barge (preserving its ID for backward compatibility); Medium and Heavy are new additions.
- **FR-011**: Each mining barge variant MUST have a corresponding ship archetype configuration defining its mass, thrust, speed, module slots, and cargo capacity. The Small Barge inherits the existing StarterMiningBarge stats (with optional tuning); Medium and Heavy define new stat profiles.
- **FR-012**: All mining barge variants MUST be compatible with the existing flight model, camera system, and EVE-style controls with zero regressions.
- **FR-013**: Two station presets (Small Mining Relay, Medium Refinery Hub) MUST be available as pre-assembled configurations of modular station components.
- **FR-014**: Station modules MUST snap together on a consistent grid to prevent gaps and misalignment.
- **FR-015**: All new visual assets MUST maintain the 60 FPS performance target on mid-range hardware.
- **FR-016**: Asteroid field rendering MUST complete within 2 ms per frame.
- **FR-017**: Station scene rendering MUST complete within 5 ms per frame.

### Key Entities

- **SkyboxConfig**: Per-scene configuration for nebula skybox selection, rotation speed, and exposure. Each scene references a specific nebula variant chosen by the designer. References one of 12 HDRI environment variants.
- **AsteroidVisualMappingConfig**: Maps ore types (Veldspar, Scordite, Pyroxeres) to dedicated mesh pairs (2 meshes per ore type) and material color tints matching each ore's beam color, enabling visual ore identification by both silhouette and color.
- **ShipArchetype (Small/Medium/Heavy Barge)**: Three ship archetype configurations. The Small Barge is the existing StarterMiningBarge renamed and visually upgraded (same ID preserved); Medium and Heavy are new additions. Each has distinct stats (mass, thrust, speed, cargo, module slots) and visual prefab reference.
- **StationPreset**: Named assembly of station modules with relative positions and rotations. Defines which modules compose a station layout (e.g., Small Mining Relay = docking ring + control tower + 2 storage; Medium Refinery Hub = hangar + refinery modules + habitat + power + bridge).

## Assumptions

- The existing `SpaceSkybox.mat` in `Assets/Settings/` is the current active skybox material and serves as the fallback.
- The 6 asteroid mesh variants in `SF_Asteroids-M2/Prefabs/` are production-ready and only require material pipeline compatibility verification.
- The 5 pre-built ships in `Retora - Modular Space Ship Pack/` can serve as reference for assembling the 3 mining barge variants from modular parts. The Small Barge replaces StarterMiningBarge (same ID, `starter-mining-barge`); existing references and tests remain valid.
- The 56 station module prefabs in `Station_MS2/Prefabs/` use a consistent scale and can be snapped together on a uniform grid.
- LOD groups will be configured on asteroid and ship prefabs; station LODs are optional for this phase given their static nature and the 5 ms budget.
- The asteroid depletion system requires logic changes to support mass-proportional sizing, continuous shrinking with crumble pauses, and removal on full depletion. Mining reducers and ship physics remain unchanged.
- The four asset packs remain in their current import locations (`SF_Asteroids-M2/`, `Retora - Modular Space Ship Pack/`, `Station_MS2/`, `Nebula Skybox Pack Vol. II...`).

## Out of Scope

- Station interiors or walkable modules.
- Full ship customization UI or save/load system.
- Procedural station generation or NPC population.
- Use of Ultimate Spaceships Creator (deferred to Phase 1.2).
- Advanced VFX (mining laser visuals, dynamic station lights, particle effects) — reserved for Phase 1.2.
- Station gameplay interactions (docking, trading, ship swapping) — Phase 1.
- Sound design or audio integration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All scenes render with zero pink/magenta fallback materials visible — 100% of premium asset materials display correctly.
- **SC-002**: Players experience a visually rich space environment immediately on scene load — nebula skybox visible with matched ambient lighting within 1 second of scene start.
- **SC-003**: Asteroid fields display 6 distinct mesh variants with visible mineral detail at mining range — mineral veins and specular highlights clearly distinguishable at distances under 100 units.
- **SC-004**: The three mining barge variants are visually distinguishable at a glance — each has a unique silhouette and relative size difference visible at 200+ unit distance.
- **SC-005**: All barge variants respond to existing controls identically to the current StarterMiningBarge — zero input or flight model regressions across all existing tests.
- **SC-006**: Two station presets render as cohesive modular assemblies — no visible gaps, floating parts, or z-fighting between adjacent modules.
- **SC-007**: Game maintains 60 FPS on mid-range hardware (GTX 1060 / RX 580 class) with all premium visuals active — asteroid field rendering under 2 ms, station rendering under 5 ms.
- **SC-008**: All existing tests (17 at time of writing) and all new tests continue to pass with zero regressions after integration.
- **SC-009**: Asteroid size visually correlates with mass — a 2x mass difference produces a clearly visible size difference at 200+ unit distance.
- **SC-010**: Mining an asteroid produces visible, continuous shrinkage with noticeable crumble pauses at each 25% threshold — the player can gauge remaining resources by visual size alone.
- **SC-011**: Depleted asteroids are fully removed from the field within 2 seconds of reaching 0% resources (crumble + fade-out duration) and no longer respond to targeting or consume rendering budget.
