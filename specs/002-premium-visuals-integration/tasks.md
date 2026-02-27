# Tasks: Premium Visuals Asset Integration

**Input**: Design documents from `/specs/002-premium-visuals-integration/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: TDD is mandatory per constitution. Test tasks are included for all new pure logic (scale formula, threshold detection, fade-out timing). Visual/prefab work validated via playtest checkpoints.

**Organization**: Tasks grouped by user story (P1→P4) to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- All file paths relative to `Assets/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify all premium asset packs are URP-compatible. This unblocks all subsequent work.

- [ ] T001 Verify SF_Asteroids-M2 materials render correctly under URP 17.3.0 — open `Assets/SF_Asteroids-M2/Prefabs/Mineral_asteroid-01.prefab` through `06` in scene, check for pink/magenta. Run Render Pipeline Converter on `Assets/SF_Asteroids-M2/Materials/` if any materials are broken.
- [ ] T002 [P] Verify Nebula Skybox Pack materials render correctly — open `Assets/Nebula Skybox Pack Vol. II – 12x 8K HDRI Space Environments/HDRI/Materials/HDR_Nebula_2_Pro_1.mat` through `12`, apply to scene skybox, check for correct rendering.
- [ ] T003 [P] Verify Retora Modular Space Ship Pack materials render correctly — open `Assets/Retora - Modular Space Ship Pack/Prefabs/Ship1.prefab` through `Ship5.prefab` in scene, check for pink/magenta. Run Render Pipeline Converter on `Assets/Retora - Modular Space Ship Pack/Materials/` if needed.
- [ ] T004 [P] Verify Station_MS2 materials render correctly — open sample prefabs from `Assets/Station_MS2/Prefabs/` (e.g., MS2_Control_grey, MS2_Hangars_grey, MS2_Bridge_grey) in scene, check for pink/magenta. Run Render Pipeline Converter on `Assets/Station_MS2/Meshes/Materials/` if needed.

**Checkpoint**: All four asset packs render correctly under URP 17.3.0. Zero pink/magenta materials. All subsequent phases unblocked.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No foundational blocking tasks required. Each user story is independently implementable after material verification.

**⚠️ NOTE**: Phase 1 material verification MUST be complete before any user story work begins. Beyond that, all user stories can proceed independently.

---

## Phase 3: User Story 1 — Immersive Space Environment (Priority: P1) 🎯 MVP

**Goal**: Replace placeholder skybox with a designer-configured nebula HDRI skybox that rotates slowly with matched ambient lighting.

**Independent Test**: Load GameScene → verify nebula skybox renders at 8K resolution, rotates slowly, ambient lighting matches nebula color palette, ships/asteroids illuminated naturally. Swap SkyboxConfig to a different nebula variant and verify it changes correctly. Set SkyboxMaterial to null and verify fallback to SpaceSkybox.mat.

### Tests for User Story 1

- [X] T005 [P] [US1] Write EditMode test for SkyboxConfig validation (null material returns fallback, rotation speed clamped to reasonable range, exposure in valid range) in `Assets/Features/Camera/Tests/SkyboxConfigTests.cs`

### Implementation for User Story 1

- [X] T006 [P] [US1] Create `SkyboxConfig` ScriptableObject class in `Assets/Features/Camera/Data/SkyboxConfig.cs` — fields: SkyboxMaterial (Material), FallbackMaterial (Material), RotationSpeed (float, default 0.5), ExposureOverride (float, default 1.0, range 0.1–3.0). Per data-model.md.
- [X] T007 [US1] Create `SkyboxController` MonoBehaviour in `Assets/Features/Camera/Views/SkyboxController.cs` — reads SkyboxConfig reference, on Awake applies SkyboxMaterial to RenderSettings.skybox (fallback if null), sets ambient lighting mode, each frame rotates skybox via `_Rotation` shader property by RotationSpeed * Time.deltaTime. Per research.md R-002.
- [ ] T008 [US1] Create `GameSceneSkybox.asset` SkyboxConfig instance in `Assets/Features/Camera/Data/` — reference `HDR_Nebula_2_Pro_1.mat` as SkyboxMaterial, `SpaceSkybox.mat` as FallbackMaterial, RotationSpeed 0.5, Exposure 1.0.
- [ ] T009 [US1] Wire SkyboxController into GameScene — add SkyboxController component to a GameObject in `Assets/Scenes/GameScene.unity`, reference GameSceneSkybox.asset config. Verify ambient lighting matches nebula.

**Checkpoint**: GameScene loads with high-res nebula skybox, slow rotation visible, ambient lighting matches nebula palette, fallback works when material is null. US1 independently testable.

---

## Phase 4: User Story 2 — Detailed Mineral Asteroids (Priority: P2)

**Goal**: Replace placeholder asteroid geometry with premium SF MINING Asteroids M2 meshes, map ore types to dedicated mesh pairs + color tint, add mass-proportional sizing, continuous depletion shrink with crumble pauses, and fade-out removal on full depletion.

**Independent Test**: Enter play mode → asteroid field displays 6 mesh variants with ore-specific tinting → mine any asteroid → observe continuous shrink with crumble pauses at 75/50/25% → fully deplete → observe final crumble + fade-out → asteroid removed from field and untargetable. 300 asteroids render within 2 ms budget.

### Tests for User Story 2

- [X] T010 [P] [US2] Write EditMode tests for asteroid scale formula: verify `scale = radius * lerp(0.3, 1.0, remaining/initial)` produces correct values at 100%, 75%, 50%, 25%, 0% depletion in `Assets/Features/Mining/Tests/AsteroidScaleTests.cs`
- [X] T011 [P] [US2] Write EditMode tests for crumble threshold detection: verify bitmask correctly identifies each threshold crossing (75%, 50%, 25%, 0%) and does not re-trigger already-passed thresholds in `Assets/Features/Mining/Tests/AsteroidScaleTests.cs`
- [X] T012 [P] [US2] Write EditMode tests for fade-out timing: verify FadeOutTimer counts down correctly, alpha interpolates 1→0, entity is marked for destruction when timer expires in `Assets/Features/Mining/Tests/AsteroidDestroyTests.cs`
- [X] T042 [P] [US2] Write EditMode tests for AsteroidFieldSystem visual mapping: verify ore→mesh selection matches config, verify PristineTintedColor is set correctly (pristineGray × oreTint), verify FR-007 cluster variety constraint (no more than 3 identical meshes within 200-unit radius), verify null mesh fallback skips variant (EC3) in `Assets/Features/Procedural/Tests/AsteroidFieldVisualMappingTests.cs`

### Implementation for User Story 2

#### Asteroid Visual Mapping (FR-006, FR-007, FR-008)

- [X] T013 [P] [US2] Create `AsteroidVisualMappingConfig` ScriptableObject class in `Assets/Features/Procedural/Data/AsteroidVisualMappingConfig.cs` — contains array of `AsteroidVisualEntry` (OreId string, MeshVariantA Mesh, MeshVariantB Mesh, TintColor Color) and a `MinScaleFraction` float field (default 0.3, range 0.1–0.5). Per data-model.md.
- [ ] T014 [US2] Create `AsteroidVisualMapping.asset` config instance in `Assets/Features/Procedural/Data/` — map Veldspar→Mineral_asteroid-01+02 (tint tan/gold 0.82,0.71,0.55), Scordite→Mineral_asteroid-03+04 (tint from Scordite.BeamColor), Pyroxeres→Mineral_asteroid-05+06 (tint from Pyroxeres.BeamColor).
- [X] T015 [US2] Modify `AsteroidPrefabAuthoring` in `Assets/Features/Procedural/Views/AsteroidPrefabAuthoring.cs` — support referencing multiple mesh prefabs (one per ore-mesh pair) instead of a single prefab. Baker creates prefab entities for each variant.
- [X] T016 [US2] Modify `AsteroidFieldSystem` in `Assets/Features/Procedural/Systems/AsteroidFieldSystem.cs` — read AsteroidVisualMappingConfig to select correct mesh variant per ore type during instantiation. Apply tint color to AsteroidBaseColorOverride as multiplicative layer over pristine gray. Set AsteroidComponent.PristineTintedColor = pristineGray × oreTintColor for use by depletion system. Enforce FR-007 cluster variety via position-hash-based mesh assignment (hash asteroid world position to deterministically select variant A or B, ensuring spatial variety without neighbor queries in Burst job). If a mesh reference is null, skip that variant and use the remaining variant for the ore type (EC3 fallback).

#### Asteroid LODs (FR-009)

- [ ] T017 [US2] Configure LOD groups on the 6 asteroid prefabs in `Assets/SF_Asteroids-M2/Prefabs/Mineral_asteroid-{01-06}.prefab` — 3 LOD levels: full detail, 50% triangles, simplified/billboard. Set transition distances appropriate for 2000m field radius.

#### Asteroid Depletion Visuals (FR-018, FR-019, FR-020, FR-021)

- [X] T018 [US2] Extend `AsteroidComponent` in `Assets/Features/Mining/Data/MiningComponents.cs` — add 4 new fields: PristineTintedColor (float4, ore-tinted pristine color set at spawn), CrumbleThresholdsPassed (byte bitmask), CrumblePauseTimer (float), FadeOutTimer (float). Per data-model.md.
- [X] T019 [US2] Modify `AsteroidDepletionSystem` in `Assets/Features/Mining/Systems/AsteroidDepletionSystem.cs` — replace hardcoded pristine color constant `(0.314, 0.314, 0.314, 1)` with per-entity `AsteroidComponent.PristineTintedColor` (set at spawn by T016). Depletion color formula becomes: `finalColor = lerp(asteroid.PristineTintedColor, depletedColor, visualDepletion)`. No managed object access needed — ore tint is baked into the ECS component at spawn time.
- [X] T020 [US2] Create `AsteroidScaleSystem` (Burst-compiled) in `Assets/Features/Mining/Systems/AsteroidScaleSystem.cs` — runs after AsteroidDepletionSystem in SimulationSystemGroup. Reads MinScaleFraction from AsteroidVisualMappingConfig (baked to singleton or BlobAsset at startup). Computes scale: `radius * lerp(MinScaleFraction, 1.0, RemainingMass/InitialMass)`. Detects threshold crossings via bitmask, starts CrumblePauseTimer (0.5s). While timer > 0: freeze scale, decrement timer. At 0% depletion after final crumble pause expires: start FadeOutTimer (0.5s). Scale and timer management only — fade-out visual and entity destruction handled by AsteroidDestroySystem (T021). Per data-model.md and research.md R-004.
- [X] T021 [US2] Create `AsteroidDestroySystem` (Burst-compiled) in `Assets/Features/Mining/Systems/AsteroidDestroySystem.cs` — runs after AsteroidScaleSystem in SimulationSystemGroup. When FadeOutTimer first becomes active (> 0): remove/disable targeting components so player cannot re-target during fade. While FadeOutTimer > 0: interpolate AsteroidBaseColorOverride.Value.w (alpha) from 1→0 and decrement timer. Requires Alpha Clipping enabled on asteroid materials (configured in T043). When FadeOutTimer < 0: destroy entity via EntityCommandBuffer. Per data-model.md.
- [X] T022 [US2] Update `AsteroidFieldSystem` initial spawning in `Assets/Features/Procedural/Systems/AsteroidFieldSystem.cs` — initialize new AsteroidComponent fields during asteroid instantiation: CrumbleThresholdsPassed = 0, CrumblePauseTimer = 0, FadeOutTimer = 0. Note: PristineTintedColor initialization is handled in T016 alongside the visual mapping logic.

#### Asteroid Material Configuration (FR-020 fade-out support)

- [ ] T043 [US2] Configure Alpha Clipping on all 6 asteroid materials in `Assets/SF_Asteroids-M2/Materials/` — enable Alpha Clipping in URP Lit material settings, set `_Cutoff` = 0.5. This allows AsteroidDestroySystem (T021) to fade asteroids via `_BaseColor.a` interpolation. Verify asteroids still render identically at full alpha (alpha 1.0 > cutoff 0.5, so all pixels pass). Smooth dissolve effects deferred to Phase 1.2 VFX.

**Checkpoint**: Asteroid field renders with 6 mesh variants, ore-specific tinting, varied distribution. Mining causes continuous shrink with crumble pauses at 75/50/25%. Depleted asteroids crumble, fade via alpha clip, and are removed. 300 asteroids within 2 ms. All existing mining tests pass.

---

## Phase 5: User Story 3 — Mining Barge Fleet Variants (Priority: P3)

**Goal**: Three visually distinct mining barge variants (Small, Medium, Heavy) assembled from Retora modular parts, each with a unique ShipArchetypeConfig and fully flyable with existing EVE-style controls.

**Independent Test**: Spawn each barge variant → fly through asteroid field with EVE-style controls (click-to-align, keyboard thrust, radial menu) → verify distinct silhouettes, correct physics (mass/thrust/speed differ per variant), camera orbits correctly, no visual artifacts. Run all 17 existing tests.

### Implementation for User Story 3

- [X] T023 [US3] Update `StarterMiningBarge.asset` DisplayName in `Assets/Features/Ship/Data/StarterMiningBarge.asset` — change DisplayName from "Starter Mining Barge" to "Small Mining Barge". Keep ArchetypeId as "starter-mining-barge" for backward compatibility. All other stats unchanged.
- [X] T024 [P] [US3] Create `MediumMiningBarge.asset` ShipArchetypeConfig in `Assets/Features/Ship/Data/` — ArchetypeId: "medium-mining-barge", Mass: 2500, MaxThrust: 8000, MaxSpeed: 75, RotationTorque: 35, MiningPower: 1.5, ModuleSlots: 6, CargoCapacity: 250. Per data-model.md.
- [X] T025 [P] [US3] Create `HeavyMiningBarge.asset` ShipArchetypeConfig in `Assets/Features/Ship/Data/` — ArchetypeId: "heavy-mining-barge", Mass: 5000, MaxThrust: 12000, MaxSpeed: 50, RotationTorque: 20, MiningPower: 2.0, ModuleSlots: 8, CargoCapacity: 500. Per data-model.md.
- [ ] T026 [US3] Assemble `SmallMiningBarge.prefab` in `Assets/Features/Ship/Prefabs/` — build from Retora modular parts (HullParts, DoorParts, MiscParts). Compact, nimble silhouette. Use Ship1-Ship5 as assembly reference. Add ShipAuthoring component referencing StarterMiningBarge.asset config.
- [ ] T027 [P] [US3] Assemble `MediumMiningBarge.prefab` in `Assets/Features/Ship/Prefabs/` — build from Retora modular parts. Balanced workhorse silhouette, visibly larger than Small. Add ShipAuthoring component referencing MediumMiningBarge.asset config.
- [ ] T028 [P] [US3] Assemble `HeavyMiningBarge.prefab` in `Assets/Features/Ship/Prefabs/` — build from Retora modular parts. Bulky industrial powerhouse silhouette, visibly largest. Add ShipAuthoring component referencing HeavyMiningBarge.asset config.
- [ ] T029 [US3] Configure LOD groups on all 3 barge prefabs — 2 LOD levels: full detail + simplified fallback. Set transition distances for camera follow distance range.
- [ ] T030 [US3] Wire SmallMiningBarge.prefab into GameScene ShipSubScene — replace current ship prefab reference in `Assets/Scenes/GameScene/ShipSubScene.unity` with SmallMiningBarge.prefab. Verify existing flight controls work identically.

**Checkpoint**: Three barge variants flyable with distinct silhouettes and different physics. SmallMiningBarge is default in GameScene. EVE-style controls work on all variants. All existing tests pass.

---

## Phase 6: User Story 4 — Modular Space Station (Priority: P4)

**Goal**: Two station presets (Small Mining Relay, Medium Refinery Hub) assembled from Station_MS2 modular components, rendered in a test scene within the 5 ms performance budget.

**Independent Test**: Open TestScene_Station → verify both presets render as cohesive assemblies with no gaps, floating parts, or z-fighting → verify materials look correct under nebula lighting → profile rendering under 5 ms.

### Implementation for User Story 4

- [X] T031 [P] [US4] Create `StationPresetConfig` ScriptableObject class in `Assets/Features/Base/Data/StationPresetConfig.cs` — fields: PresetName (string), PresetId (string), Description (string), Modules array of StationModuleEntry (ModulePrefab GameObject, LocalPosition Vector3, LocalRotation Quaternion, ModuleRole string). Per data-model.md.
- [ ] T032 [US4] Assemble `SmallMiningRelay.prefab` in `Assets/Features/Base/Prefabs/` — compose from Station_MS2 grey variants: MS2_Control_grey + MS2_Storage_grey ×2 + MS2_Antennas_grey + MS2_Connect_grey connectors. Snap-align all modules on a consistent grid. Compact layout (3-5 functional modules).
- [ ] T033 [US4] Assemble `MediumRefineryHub.prefab` in `Assets/Features/Base/Prefabs/` — compose from Station_MS2 grey variants: MS2_Bridge_grey + MS2_Hangars_grey + MS2_Modules_grey ×2 + MS2_Storage_grey ×2 + MS2_Energy_grey + MS2_Habitat_grey + MS2_Tower_grey + MS2_Connect_grey connectors. Larger, more complex layout (8-12 functional modules).
- [ ] T034 [P] [US4] Create `SmallMiningRelay.asset` StationPresetConfig instance in `Assets/Features/Base/Data/` — document the module composition, positions, and rotations matching the assembled prefab.
- [ ] T035 [P] [US4] Create `MediumRefineryHub.asset` StationPresetConfig instance in `Assets/Features/Base/Data/` — document the module composition, positions, and rotations matching the assembled prefab.
- [ ] T036 [US4] Create `TestScene_Station.unity` in `Assets/Scenes/` — place both station presets in scene with a nebula skybox (reuse SkyboxConfig from US1). Add a simple camera for inspection.

**Checkpoint**: Both station presets render correctly with no visual artifacts. Modules snap-aligned, no gaps. Materials correct under nebula lighting. Rendering within 5 ms budget.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Performance validation across all stories, regression testing, final quality checks.

- [ ] T037 Profile asteroid field rendering with premium meshes in Unity Profiler — target: < 2 ms/frame with 300 asteroids (FR-016)
- [ ] T038 [P] Profile station scene rendering in Unity Profiler — target: < 5 ms/frame per station preset (FR-017)
- [ ] T039 Profile full GameScene with all premium visuals active — target: 60 FPS on mid-range hardware (FR-015)
- [ ] T040 Run all existing and new tests via Unity Test Runner — verify zero regressions across all existing tests plus all new tests pass (SC-008)
- [ ] T041 Run quickstart.md validation — execute all test procedures from `specs/002-premium-visuals-integration/quickstart.md` and verify each passes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T001-T004 can run in parallel.
- **Foundational (Phase 2)**: N/A — no blocking foundational tasks beyond Phase 1.
- **User Stories (Phase 3-6)**: All depend on Phase 1 material verification completion.
  - US1 (skybox) and US3 (ships) have NO inter-story dependencies — can run in parallel.
  - US2 (asteroids) has NO dependencies on other stories — can run in parallel with US1/US3.
  - US4 (stations) benefits from US1 completion (reuses SkyboxConfig for test scene lighting) but can proceed independently.
- **Polish (Phase 7)**: Depends on all user stories being complete.

### User Story Dependencies

- **US1 (P1 — Skybox)**: Can start after Phase 1. No dependencies on other stories.
- **US2 (P2 — Asteroids)**: Can start after Phase 1. No dependencies on other stories.
- **US3 (P3 — Ships)**: Can start after Phase 1. No dependencies on other stories.
- **US4 (P4 — Stations)**: Can start after Phase 1. Optionally uses SkyboxConfig from US1 for test scene, but can use SpaceSkybox.mat as fallback.

### Within Each User Story

- Tests (T005, T010-T012, T042) MUST be written and FAIL before implementation
- ScriptableObject config classes before config instances
- Config instances before systems that consume them
- Systems in dependency order (AsteroidDepletionSystem before AsteroidScaleSystem before AsteroidDestroySystem)

### Parallel Opportunities

- **Phase 1**: T001-T004 all run in parallel (independent asset packs)
- **US1**: T005 and T006 run in parallel (test + config class); T007 depends on T006
- **US2**: T010-T012 and T042 run in parallel with T013 (tests + config class); T015, T017, and T043 in parallel; T020 and T021 sequential (scale before destroy)
- **US3**: T024 and T025 in parallel (config assets); T026, T027, T028 — T027 and T028 in parallel after T026 establishes assembly pattern
- **US4**: T031 can parallel with T032 start; T034 and T035 in parallel after prefab assembly
- **Cross-story**: US1, US2, US3, US4 can all proceed in parallel after Phase 1

---

## Parallel Example: User Story 2

```text
# Wave 1 — Tests + Config (all parallel):
T010: "EditMode test for scale formula in Assets/Features/Mining/Tests/AsteroidScaleTests.cs"
T011: "EditMode test for threshold detection in Assets/Features/Mining/Tests/AsteroidScaleTests.cs"
T012: "EditMode test for fade-out timing in Assets/Features/Mining/Tests/AsteroidDestroyTests.cs"
T013: "Create AsteroidVisualMappingConfig in Assets/Features/Procedural/Data/AsteroidVisualMappingConfig.cs"
T042: "EditMode test for visual mapping in Assets/Features/Procedural/Tests/AsteroidFieldVisualMappingTests.cs"

# Wave 2 — Config instance + authoring + materials (parallel after T013):
T014: "Create AsteroidVisualMapping.asset config instance"
T015: "Modify AsteroidPrefabAuthoring for multi-prefab support"
T017: "Configure LOD groups on asteroid prefabs"
T043: "Configure Alpha Clipping on asteroid materials"

# Wave 3 — Component + Systems (sequential, depends on T014-T015):
T018: "Extend AsteroidComponent with crumble/fade/PristineTintedColor fields"
T016: "Modify AsteroidFieldSystem for visual mapping + PristineTintedColor + EC3 fallback (depends on T018)"
T019: "Modify AsteroidDepletionSystem to use per-entity PristineTintedColor"

# Wave 4 — Depletion systems (sequential after T018, T043):
T020: "Create AsteroidScaleSystem (scale + thresholds + timers)"
T021: "Create AsteroidDestroySystem (fade-out alpha clip + untargetable + destruction)"
T022: "Update AsteroidFieldSystem initial spawning (initialize all new fields)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Material verification (T001-T004)
2. Complete Phase 3: US1 — Skybox (T005-T009)
3. **STOP and VALIDATE**: Load GameScene, verify nebula skybox, rotation, ambient lighting
4. Immediate visual upgrade with zero gameplay risk

### Incremental Delivery

1. Phase 1 (Setup) → Material verification → All packs URP-compatible
2. US1 (Skybox) → Immersive environment → Test independently → **MVP!**
3. US2 (Asteroids) → Premium meshes + depletion shrink → Test independently
4. US3 (Ships) → Three barge variants → Test independently
5. US4 (Stations) → Station presets → Test independently
6. Phase 7 (Polish) → Performance validation + regression → Ship it

### Parallel Team Strategy

With multiple developers:

1. Team completes Phase 1 material verification together
2. Once Phase 1 is done:
   - Developer A: US1 (Skybox) + US4 (Stations)
   - Developer B: US2 (Asteroids — largest scope)
   - Developer C: US3 (Ships)
3. All stories integrate independently, no merge conflicts

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- TDD mandatory per constitution: write tests first (T005, T010-T012, T042), verify they fail, then implement
- Prefab assembly tasks (T026-T028, T032-T033) require Unity Editor — cannot be done via code alone
- LOD configuration tasks (T017, T029) require Unity Editor for LOD distance tuning
- Material configuration tasks (T043) require Unity Editor for Alpha Clipping toggle
- ScriptableObject .asset creation tasks (T008, T014, T023-T025, T034-T035) require Unity Editor
- Commit after each task or logical group using conventional commits (feat:, refactor:, test:)
- Per constitution V, all new public types (SkyboxConfig, AsteroidVisualMappingConfig, StationPresetConfig, AsteroidScaleSystem, AsteroidDestroySystem) MUST include XML documentation comments referencing acceptance criteria from spec.md. Add XML docs as part of each implementation task, not as a separate pass.
