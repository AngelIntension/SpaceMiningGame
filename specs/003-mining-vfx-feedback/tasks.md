# Tasks: Mining Loop VFX & Feedback

**Input**: Design documents from `/specs/003-mining-vfx-feedback/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: TDD is mandatory per project constitution. Test tasks are written FIRST and must FAIL before implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create all new data types, event structs, ECS components, and ScriptableObject config classes. These are dependency-free data definitions that all user stories build upon.

### Event Structs

- [x] T001 [P] Create ThresholdCrossedEvent readonly struct (AsteroidId, ThresholdIndex, Position, AsteroidRadius) in Assets/Core/EventBus/Events/ThresholdCrossedEvent.cs
- [x] T002 [P] Create OreChunkCollectedEvent readonly struct (Position, OreId) in Assets/Core/EventBus/Events/OreChunkCollectedEvent.cs

### ECS Data Types

- [x] T003 [P] Create AsteroidEmissionComponent IComponentData with [MaterialProperty("_EmissionColor")] float4 Value in Assets/Features/Mining/Data/AsteroidEmissionComponent.cs
- [x] T004 [P] Add NativeThresholdCrossedAction unmanaged struct (Entity, byte ThresholdIndex, float3 Position, float Radius) to Assets/Features/Mining/Data/NativeMiningActions.cs

### Managed State Extensions

- [x] T005 [P] Add DepletionFraction float field to MiningSessionState record in Assets/Core/State/MiningState.cs
- [x] T006 [P] Add MiningDepletionTickAction record implementing IMiningAction in Assets/Core/State/MiningActions.cs

### ScriptableObject Configs

- [x] T007 [P] Create MiningVFXConfig ScriptableObject (BeamWidth, BeamPulseSpeed, BeamPulseAmplitude, SparkEmissionRate, SparkLifetime, SparkSpeed, HeatHazeIntensity, HeatHazeScale) with [CreateAssetMenu] in Assets/Features/Mining/Data/MiningVFXConfig.cs
- [x] T008 [P] Create DepletionVFXConfig ScriptableObject (VeinGlowMinIntensity, VeinGlowMaxIntensity, VeinGlowColor, VeinGlowPulseSpeed, VeinGlowPulseAmplitude, CrumbleBurstCountBase, CrumbleBurstCountScale, CrumbleBurstSpeed, CrumbleBurstLifetime, CrumbleFlashDuration, FragmentCount, FragmentSpeed, FragmentLifetime, FragmentScaleRange) with [CreateAssetMenu] in Assets/Features/Mining/Data/DepletionVFXConfig.cs
- [x] T009 [P] Create OreChunkConfig ScriptableObject (SpawnIntervalMin/Max, ChunksPerSpawnMin/Max, ChunkScaleMin/Max, InitialDriftDuration, InitialDriftSpeed, AttractionSpeed, AttractionAcceleration, CollectionFlashDuration, MaxLifetime, GlowIntensity) with [CreateAssetMenu] in Assets/Features/Mining/Data/OreChunkConfig.cs
- [x] T010 [P] Create MiningAudioConfig ScriptableObject (LaserHumClip, LaserHumBaseVolume, LaserHumPitchMin/Max, LaserHumFadeOutDuration, SparkCrackleClip/Volume, CrumbleRumbleClip/Volume, ExplosionClip/Volume, CollectionClinkClip/Volume, MaxAudibleDistance) with [CreateAssetMenu] in Assets/Features/Mining/Data/MiningAudioConfig.cs

### Compile Verification

- [x] T011 MCP: refresh_unity(compile="request", wait_for_ready=true) then read_console(types=["error"]) — must be zero errors after all Phase 1 scripts. Verify asmdef coverage for new files (Core/EventBus/Events/ event structs, Mining/Data/ components and configs)

**Checkpoint**: All data types, events, configs, and state extensions compile cleanly. No behavioral changes yet.

---

## Phase 2: Foundational (ECS Bridge + Reducer)

**Purpose**: Wire the ECS-to-managed bridge for threshold crossing events and depletion fraction updates. This is the critical data pipeline that ALL user stories depend on.

**CRITICAL**: No user story work can begin until this phase is complete.

### Tests (TDD — write first, must FAIL)

- [x] T012 [P] Create MiningDepletionReducerTests: test MiningDepletionTickAction updates DepletionFraction, test boundary values (0.0, 0.5, 1.0), test state immutability in Assets/Features/Mining/Tests/MiningDepletionReducerTests.cs
- [x] T013 [P] Create ThresholdEventTests: test NativeThresholdCrossedAction enqueue/dequeue roundtrip, test threshold index values 0-3, test position/radius preservation in Assets/Features/Mining/Tests/ThresholdEventTests.cs

### Implementation

- [x] T014 Modify AsteroidScaleSystem to allocate NativeQueue<NativeThresholdCrossedAction> in OnCreate and enqueue on threshold crossing in DetectThresholdCrossing() in Assets/Features/Mining/Systems/AsteroidScaleSystem.cs
- [x] T015 Modify MiningActionDispatchSystem to drain NativeThresholdCrossedAction queue, publish ThresholdCrossedEvent on EventBus, and dispatch MiningDepletionTickAction with current depletion fraction each frame during active mining in Assets/Features/Mining/Systems/MiningActionDispatchSystem.cs
- [x] T016 Add MiningDepletionTickAction case to MiningReducer that returns new MiningSessionState with updated DepletionFraction in Assets/Features/Mining/Systems/MiningReducer.cs

### ScriptableObject Asset Creation

- [x] T017 MCP: Create four .asset instances via manage_asset(action="create") — MiningVFXConfig.asset, DepletionVFXConfig.asset, OreChunkConfig.asset, MiningAudioConfig.asset under Assets/Features/Mining/Data/ with default field values from data-model.md

### VContainer Registration

- [x] T017a Modify SceneLifetimeScope to register all new ScriptableObject configs — add [SerializeField] fields for MiningVFXConfig, DepletionVFXConfig, OreChunkConfig, MiningAudioConfig, and AsteroidVisualMappingConfig, then register each via container.RegisterInstance() in Configure(). Assign .asset references via MCP manage_components(action="set_property") on the SceneLifetimeScope GameObject in Assets/Core/SceneLifetimeScope.cs

### Verification

- [x] T018 MCP: refresh_unity(compile="request", wait_for_ready=true), read_console(types=["error"]), then run_tests(mode="EditMode") — all tests pass including new T012/T013 tests

**Checkpoint**: Foundation ready — ThresholdCrossedEvent fires on EventBus at each 25% threshold. MiningSessionState.DepletionFraction updates every frame during mining. All user story implementation can begin.

---

## Phase 3: User Story 1 — Visceral Mining Laser Beam (Priority: P1)

**Goal**: Replace basic LineRenderer beam with pulsing energy beam + ore-colored impact sparks + heat shimmer at mining arm. All effects track positions every frame and cease cleanly on stop.

**Independent Test**: Target any asteroid, activate mining laser. Beam pulses, sparks spray from asteroid surface in ore color, heat shimmer visible on mining arm. Stop mining — all effects cease immediately.

### Scene Setup (MCP)

- [x] T019 [P] [US1] MCP: Add MiningArmOrigin and CollectorPoint empty child GameObjects to SmallMiningBarge, MediumMiningBarge, and HeavyMiningBarge prefabs (Assets/Features/Ship/Prefabs/) via manage_gameobject(action="create") — position MiningArmOrigin at the forward turret/arm mount point (approximate local pos: 0, 0.2, 1.5), CollectorPoint at center-bottom cargo bay area (approximate local pos: 0, -0.3, 0). Adjust per barge variant proportionally to hull mesh scale
- [x] T020 [P] [US1] MCP: Create beam materials via manage_asset(action="create", asset_type="Material") — BeamCore (URP/Unlit, additive), BeamGlow (URP/Unlit, additive, softer), SparkParticle (URP/Particles/Unlit, additive), HeatHaze (URP/Unlit, scrolling UV) under Assets/Features/Mining/Materials/

### Tests (TDD — write first, must FAIL)

- [x] T020a [P] [US1] Create MiningBeamVFXTests: test beam pulse width calculation (sinusoidal given time/speed/amplitude), test spark color resolution (ore type → BeamColor), test heat shimmer opacity from config, test clean shutdown state reset in Assets/Features/Mining/Tests/MiningBeamVFXTests.cs

### Implementation

- [x] T021 [US1] Upgrade MiningBeamView with pulsing beam width (sinusoidal from MiningVFXConfig), impact ParticleSystem (cone emission, ore-colored, SparkEmissionRate), heat shimmer ParticleSystem (billboard quad at mining arm), clean shutdown on MiningStoppedEvent, inject MiningVFXConfig via VContainer in Assets/Features/Mining/Views/MiningBeamView.cs

### Verification

- [x] T022 [US1] MCP: refresh_unity(compile="request"), read_console(types=["error"]), manage_editor(action="play"), manage_scene(action="screenshot") — verify beam pulses, sparks visible at asteroid, heat shimmer at arm, clean stop behavior (Acceptance Scenarios US1.1-US1.6)

**Checkpoint**: User Story 1 fully functional. Mining beam is visually compelling with pulsing energy, ore-colored sparks, and heat shimmer.

---

## Phase 4: User Story 2 — Asteroid Depletion Feedback (Priority: P2)

**Goal**: Vein glow ramps with depletion. Bright flash + particle burst at each crumble threshold. Final fragment explosion on destruction.

**Independent Test**: Mine a single asteroid from full health to depletion. Vein glow brightens progressively. Flash+burst at 25/50/75% depleted (escalating intensity). Final 8-15 fragment explosion at 100%.

### Tests (TDD — write first, must FAIL)

- [x] T023 [P] [US2] Create AsteroidEmissionTests: test emission intensity lerp between min/max with sqrt curve, test boundary values (0%, 50%, 100% depletion), test HDR float4 output format, test pulse modulation (sinusoidal at VeinGlowPulseSpeed with VeinGlowPulseAmplitude) in Assets/Features/Mining/Tests/AsteroidEmissionTests.cs

### Implementation

- [x] T024 [US2] Create AsteroidEmissionSystem (Burst-compiled ISystem, SimulationSystemGroup, after AsteroidDepletionSystem) — queries AsteroidComponent + AsteroidEmissionComponent, calculates emission from depletion via sqrt ease-in, applies sinusoidal pulse modulation (VeinGlowPulseSpeed Hz, VeinGlowPulseAmplitude range), writes HDR float4 using DepletionVFXConfig values in Assets/Features/Mining/Systems/AsteroidEmissionSystem.cs
- [x] T025 [P] [US2] Modify AsteroidFieldSystem to add AsteroidEmissionComponent (initial float4(0,0,0,0)) when creating asteroid entities via RenderMeshUtility.AddComponents in Assets/Features/Procedural/Systems/AsteroidFieldSystem.cs
- [x] T026 [US2] MCP: Enable emission on asteroid materials via manage_material(action="set_material_shader_property") — set _EmissionColor enabled on SF_Asteroids-M2 material assets
- [x] T027 [US2] Create DepletionVFXView MonoBehaviour — subscribe to ThresholdCrossedEvent via EventBus, spawn crumble burst ParticleSystem (count scaled by CrumbleBurstCountBase * CrumbleBurstCountScale^thresholdIndex), flash billboard quad (CrumbleFlashDuration), fragment explosion on final threshold (FragmentCount 8-15, FragmentSpeed, FragmentLifetime), inject DepletionVFXConfig via VContainer in Assets/Features/Mining/Views/DepletionVFXView.cs

### Verification

- [x] T028 [US2] MCP: refresh_unity(compile="request"), run_tests(mode="EditMode") — AsteroidEmissionTests pass, then manage_editor(action="play"), manage_scene(action="screenshot") at 50% depletion — verify vein glow visible, burst at threshold, fragment explosion on destroy (Acceptance Scenarios US2.1-US2.7)

**Checkpoint**: User Story 2 fully functional. Asteroids visually communicate depletion state through glow, bursts, and final explosion.

---

## Phase 5: User Story 3 — Continuous Ore Collection Feedback (Priority: P3)

**Goal**: During active mining, 2-5 cosmetic ore chunks spawn at the asteroid every 3-7 seconds (randomized), drift outward briefly, then attract to barge collector with bezier curve. Collection flash on arrival.

**Independent Test**: Mine an asteroid for 15-30 seconds. Bursts of 2-5 ore chunks spawn at random intervals, use correct ore mesh+color, drift then curve toward barge, flash on collection. Chunks are purely cosmetic.

### Tests (TDD — write first, must FAIL)

- [x] T029 [US3] Create OreChunkAttractionTests: test bezier curve calculation (pure math), test chunk reaches target within MaxLifetime, test pool reclaim on collection, test drift phase duration and direction in Assets/Features/Mining/Tests/OreChunkAttractionTests.cs

### Implementation

- [x] T030 [US3] Create OreChunkBehaviour MonoBehaviour — drift phase (InitialDriftDuration, InitialDriftSpeed outward), attract phase (bezier curve toward CollectorPoint, AttractionSpeed/AttractionAcceleration), collect phase (distance threshold, flash, publish OreChunkCollectedEvent, return to pool), force-despawn at MaxLifetime, zero-GC (pre-allocated bezier control points, cached transforms) in Assets/Features/Mining/Views/OreChunkBehaviour.cs
- [x] T031 [US3] Create OreChunkController MonoBehaviour — subscribe to MiningStartedEvent/MiningStoppedEvent, spawn timer (Random.Range 3-7s), spawn 2-5 chunks per event (random mesh variant A/B from AsteroidVisualMappingConfig, random scale ChunkScaleMin-Max, ore-type glow color, GlowIntensity emission), ObjectPool<OreChunkBehaviour> (size 15), inject OreChunkConfig + AsteroidVisualMappingConfig + IEventBus + IStateStore via VContainer in Assets/Features/Mining/Views/OreChunkController.cs

### Verification

- [x] T032 [US3] MCP: refresh_unity(compile="request"), run_tests(mode="EditMode") — OreChunkAttractionTests pass, then manage_editor(action="play") for 20s active mining, manage_scene(action="screenshot") — verify multiple chunk bursts spawned, curved attraction toward barge, collection flash (Acceptance Scenarios US3.1-US3.7)

**Checkpoint**: User Story 3 fully functional. Continuous stream of ore chunk bursts creates sustained visual reward during mining.

---

## Phase 6: User Story 4 — Synchronized HUD Mining Feedback (Priority: P4)

**Goal**: Progress bar in mining panel shows depletion %. Color transitions ore-to-red. Pulses in sync with vein glow. Flashes white on threshold crossings.

**Independent Test**: Mine an asteroid while watching the HUD. Progress bar fills, color shifts from ore to red, pulses subtly, flashes white at each threshold crossing, shows 100% at depletion.

### Tests (TDD — write first, must FAIL)

- [x] T033 [US4] Create HUDMiningFeedbackTests: test color interpolation formula (ore color to red/orange), test progress bar percentage matches DepletionFraction input, test flash trigger on threshold event in Assets/Features/HUD/Tests/HUDMiningFeedbackTests.cs

### Implementation

- [x] T034 [P] [US4] Add progress bar elements to mining-info-panel: mining-progress-bar (container), mining-progress-fill (inner fill), mining-progress-flash (overlay) in Assets/Features/HUD/Views/HUD.uxml
- [x] T035 [P] [US4] Add progress bar styles: fixed height, rounded corners, fill width driven by percentage, color transitions, pulse animation, flash overlay in Assets/Features/HUD/Views/HUD.uss
- [x] T036 [US4] Modify HUDView to read gameState.Loop.Mining.DepletionFraction, set mining-progress-fill width percentage, lerp fill color (ore → red/orange), sinusoidal pulse opacity at DepletionVFXConfig.VeinGlowPulseSpeed Hz (matching vein glow pulse for visual sync), subscribe to ThresholdCrossedEvent for white flash overlay with 0.3s fade in Assets/Features/HUD/Views/HUDView.cs

### Verification

- [x] T037 [US4] MCP: refresh_unity(compile="request"), run_tests(mode="EditMode") — HUDMiningFeedbackTests pass, then manage_editor(action="play"), manage_scene(action="screenshot") at various depletion levels — verify bar fills, color shifts, pulses, flashes at thresholds (Acceptance Scenarios US4.1-US4.5)

**Checkpoint**: User Story 4 fully functional. HUD progress bar perfectly synchronized with 3D depletion effects.

---

## Phase 7: User Story 5 — Spatial Audio Feedback (Priority: P5)

**Goal**: 6 audio cues: laser hum (looped, pitch ramps with depletion), spark crackle (at impact), crumble rumble (at threshold), explosion (at destruction), collection clink (at barge), hum fade-out (on stop). All spatialized.

**Independent Test**: Mine an asteroid with audio enabled. Laser hum plays and pitch rises with depletion. Spark crackle at impact point. Rumble at each threshold. Explosion on destroy. Clink when chunks reach barge. Hum fades on stop (0.2-0.5s).

### Tests (TDD — write first, must FAIL)

- [x] T037a [US5] Create MiningAudioTests: test pitch interpolation formula (Lerp PitchMin/PitchMax at depletion 0.0, 0.5, 1.0), test volume scaling from config values, test fade-out volume curve over LaserHumFadeOutDuration in Assets/Features/Mining/Tests/MiningAudioTests.cs

### Implementation

- [x] T038 [US5] Create ProceduralAudioGenerator static utility — generate placeholder AudioClips via AudioClip.Create(): LaserHum (80Hz sine + harmonics, looping), SparkCrackle (white noise burst, 0.1s), CrumbleRumble (40-60Hz sine sweep, 0.5s), Explosion (white noise + low sine, 0.8s, amplitude envelope), CollectionClink (2kHz sine ping, 0.1s, fast decay). Cache on first access in Assets/Features/Mining/Views/ProceduralAudioGenerator.cs
- [x] T039 [US5] Create MiningAudioController MonoBehaviour — 3 AudioSources (HumSource looping 3D at beam midpoint, ImpactSource one-shot 3D at asteroid, EventSource one-shot 3D repositioned per event). Subscribe to MiningStartedEvent (start hum+crackle), MiningStoppedEvent (fade hum over LaserHumFadeOutDuration, stop crackle), ThresholdCrossedEvent (play rumble index 0-2 or explosion index 3), OreChunkCollectedEvent (play clink at barge). LateUpdate: lerp hum pitch from PitchMin-PitchMax keyed on DepletionFraction. Null-clip fallback: if any MiningAudioConfig AudioClip field is null, use the corresponding ProceduralAudioGenerator cached clip instead. Inject MiningAudioConfig + IEventBus + IStateStore via VContainer in Assets/Features/Mining/Views/MiningAudioController.cs

### Verification

- [x] T040 [US5] MCP: refresh_unity(compile="request"), read_console(types=["error"]), manage_editor(action="play") — verify all 6 audio cues play at correct spatial positions and timing (Acceptance Scenarios US5.1-US5.7)

**Checkpoint**: User Story 5 fully functional. Complete spatial audio layer reinforces all visual feedback.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Edge case verification, regression testing, performance profiling, and visual verification across all user stories.

- [x] T041 Edge case verification: target switch mid-beam (beam/sparks transition cleanly), chunks in flight on asteroid destroy (continue to barge), chunks in flight on range exit (complete journey), target switch resets chunk timer, crumble burst completes on stop, HUD hidden while VFX continues, camera away particles culled — test each via MCP playtest
- [x] T042 MCP: run_tests(mode="EditMode") and run_tests(mode="PlayMode") — verify all existing tests (21+) plus all new tests pass with zero regressions (SC-010)
- [x] T043 Performance profiling: manage_editor(action="play") in TestScene_MiningField with 300 asteroids, activate mining, verify via Unity Profiler — total VFX+audio < 1.5 ms (SC-008), asteroid field < 2 ms with emission (SC-009), zero GC allocations in gameplay frame, steady 60 FPS (SC-011). Verify off-screen particle culling: rotate camera away from mining operation, confirm GPU budget drops (FR-034). Note: multi-beam simultaneous mining performance validation deferred to future NPC mining spec (FR-035)
- [x] T044 [P] MCP: manage_scene(action="screenshot") at key moments — beam active with sparks, asteroid at 50% depletion (vein glow), crumble burst in progress, ore chunks in flight, HUD progress bar at various fill levels — visual verification archive

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories. Includes VContainer registration (T017a) required for all view-layer DI.
- **User Stories (Phase 3-7)**: All depend on Phase 2 completion (including T017a VContainer registration)
  - US1 (Phase 3): Independent — no dependency on other stories. TDD tests (T020a) before implementation (T021).
  - US2 (Phase 4): Independent — no dependency on other stories
  - US3 (Phase 5): Independent — requires CollectorPoint transform from T019 (shared with US1)
  - US4 (Phase 6): Independent — reads DepletionFraction from StateStore (established in Phase 2)
  - US5 (Phase 7): Independent — subscribes to events from Phase 2 + OreChunkCollectedEvent from US3. TDD tests (T037a) before implementation (T038-T039).
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2. Creates barge child transforms (T019) used by US3 and US5.
- **US2 (P2)**: Can start after Phase 2. No cross-story dependencies.
- **US3 (P3)**: Can start after Phase 2. Uses CollectorPoint from T019. Publishes OreChunkCollectedEvent consumed by US5.
- **US4 (P4)**: Can start after Phase 2. Reads DepletionFraction. Subscribes to ThresholdCrossedEvent.
- **US5 (P5)**: Can start after Phase 2. Uses spatial positions from all stories. Subscribes to OreChunkCollectedEvent (US3).

### Recommended Execution Order

1. Phase 1 (Setup) → Phase 2 (Foundational)
2. US1 (P1) — creates barge transforms needed by US3/US5
3. US2 (P2) and US3 (P3) in parallel (if capacity allows)
4. US4 (P4) — reads DepletionFraction, can overlap with US2/US3
5. US5 (P5) — benefits from all events being active
6. Phase 8 (Polish)

### Within Each User Story

- Tests (TDD) MUST be written and FAIL before implementation
- Data/config before systems
- Systems before views
- Core implementation before integration
- MCP verification at each phase boundary

### Parallel Opportunities

- **Phase 1**: T001-T010 are ALL parallelizable (different files, no dependencies)
- **Phase 2**: T012-T013 parallel (different test files); T014-T016 sequential (queue allocation → drain → reducer); T017a after T017 (needs .asset files created first)
- **Phase 3**: T019-T020 parallel (prefab transforms vs materials); T020a parallel with T019-T020 (different file); T021 after T019-T020a
- **Phase 4**: T023+T025 parallel (test + field system); T024 sequential after T023 (TDD); T034-T035 parallel (UXML vs USS)
- **Phase 5**: T030-T031 sequential (behaviour before controller)
- **Phase 7**: T037a before T038-T039 (TDD); T038-T039 sequential (generator before controller)
- **Phase 8**: T043-T044 parallel (profiling vs screenshots)

---

## Parallel Example: Phase 1 Setup

```
# Launch ALL Phase 1 tasks together (all different files):
T001: ThresholdCrossedEvent.cs
T002: OreChunkCollectedEvent.cs
T003: AsteroidEmissionComponent.cs
T004: NativeMiningActions.cs (modify)
T005: MiningState.cs (modify)
T006: MiningActions.cs (modify)
T007: MiningVFXConfig.cs
T008: DepletionVFXConfig.cs
T009: OreChunkConfig.cs
T010: MiningAudioConfig.cs
```

## Parallel Example: User Story 1

```
# Launch setup + test in parallel:
T019: MCP add barge child transforms (prefabs)
T020: MCP create beam materials
T020a: MiningBeamVFXTests.cs (TDD test)

# Then sequential:
T021: Upgrade MiningBeamView.cs (needs T019 transforms + T020a tests failing)
T022: MCP verification
```

## Parallel Example: User Story 2

```
# Launch test + independent system in parallel:
T023: AsteroidEmissionTests.cs (TDD test, includes pulse tests)
T025: AsteroidFieldSystem.cs (modify, independent)

# Then sequential (TDD: tests must fail before implementation):
T024: AsteroidEmissionSystem.cs (new system, includes pulse modulation)
T026: MCP enable emission on materials (needs T024/T025 compiled)
T027: DepletionVFXView.cs (needs events wired)
T028: MCP verification
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (all data types + configs)
2. Complete Phase 2: Foundational (ECS bridge + reducer)
3. Complete Phase 3: User Story 1 (beam VFX)
4. **STOP and VALIDATE**: Test beam independently — pulsing, sparks, heat shimmer, clean stop
5. This alone delivers the single biggest "game feel" upgrade

### Incremental Delivery

1. Setup + Foundational → Data pipeline ready
2. Add US1 (Beam VFX) → Test independently → Most impactful visual upgrade
3. Add US2 (Depletion Feedback) → Test independently → Asteroids communicate health
4. Add US3 (Ore Chunks) → Test independently → Continuous reward loop
5. Add US4 (HUD Progress) → Test independently → Interface completes feedback loop
6. Add US5 (Spatial Audio) → Test independently → Full sensory experience
7. Polish → Edge cases, profiling, regression → Production-ready

Each story adds value without breaking previous stories.

---

## Notes

- [P] tasks = different files, no dependencies — safe to execute in parallel
- [Story] label maps task to specific user story for traceability
- All ScriptableObject field values are defined in data-model.md
- All MCP commands follow the verification loop from quickstart.md: compile → console check → tests → screenshot
- Constitution deviations documented in plan.md: AsteroidEmissionComponent (mutable ECS shell), OreChunkBehaviour (mutable view-layer), MiningAudioController (stateful AudioSource)
- Total new files: 20 (.cs) + 4 (.asset) | Total modified files: 12 (11 from plan + SceneLifetimeScope)
- Performance budget: total VFX+audio < 1.5 ms, asteroid field < 2 ms, 60 FPS steady
