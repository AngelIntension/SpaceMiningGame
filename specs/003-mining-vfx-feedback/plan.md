# Implementation Plan: Mining Loop VFX & Feedback

**Branch**: `003-mining-vfx-feedback` | **Date**: 2026-02-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-mining-vfx-feedback/spec.md`

## Summary

Add high-impact visual effects, synchronized HUD feedback, and spatialized audio to the mining loop. Five priority tiers: P1 laser beam VFX (LineRenderer + ParticleSystem sparks + heat haze), P2 asteroid depletion feedback (ECS emission component + crumble bursts + fragment explosion), P3 continuous ore chunk collection (pooled MonoBehaviour chunks with bezier attraction), P4 HUD progress bar (UI Toolkit synced to depletion), P5 spatial audio (AudioSource-based with procedural placeholders). All new code follows immutable functional style with ScriptableObject configs. New ECS→managed bridge via NativeQueue for threshold crossing events.

## Technical Context

**Language/Version**: C# 9.0 / .NET Framework 4.7.1
**Primary Dependencies**: Unity 6 (6000.3.10f1), URP 17.3.0, Entities 1.3.2, Entities Graphics 1.3.2, Burst, UniTask 2.5.10, VContainer 1.16.7
**Storage**: N/A (runtime VFX, no persistence)
**Testing**: NUnit + Unity Test Framework (EditMode + PlayMode), MCP-assisted test execution
**Target Platform**: Windows 64-bit Standalone
**Project Type**: Unity game (3D space mining simulator)
**Performance Goals**: Total VFX+audio < 1.5 ms/frame; asteroid field < 2 ms/frame; 60 FPS minimum
**Constraints**: Zero GC in hot loops; immutable functional style; DOTS/ECS for simulation; MonoBehaviour for view-layer VFX
**Scale/Scope**: 1 active beam, ~10 ore chunks in flight, ~300 asteroids with emission, 6 audio cues

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Functional & Immutable First | PASS | New `MiningDepletionTickAction` follows reducer pattern. ScriptableObject configs are immutable data. Event structs are `readonly struct`. |
| II. Predictability & Testability | PASS | All VFX formulas are pure static functions (testable). View-layer effects are deterministic given state input. |
| III. Performance by Default | PASS | ParticleSystem with GPU instancing + culling. Object pooling for ore chunks. No GC in hot paths. Performance profiling in Phase 7. |
| IV. Data-Oriented Design | PASS | New `AsteroidEmissionComponent` is ECS component. Configs are ScriptableObjects. No MonoBehaviour game state. |
| V. Modularity & Extensibility | PASS | New systems in Mining feature folder with explicit asmdef dependencies. EventBus for cross-system communication. |
| VI. Explicit Over Implicit | PASS | All dependencies injected via VContainer. Event subscriptions explicit. No hidden wiring. |
| Editor Automation (MCP) | PASS | MCP verification loop after every script creation. Console monitoring. Test execution via MCP. |

**Constitution Deviations**:
- `AsteroidEmissionComponent` is a mutable ECS shell (same justified deviation as existing `AsteroidComponent` — Burst/cache performance).
- `OreChunkBehaviour` uses mutable fields for position interpolation (view-layer side effect, not game state — isolated to rendering boundary).

## Project Structure

### Documentation (this feature)

```text
specs/003-mining-vfx-feedback/
├── plan.md              # This file
├── research.md          # Technology decisions
├── data-model.md        # Entity/config definitions
├── quickstart.md        # Setup guide
└── checklists/
    └── requirements.md  # Spec quality validation
```

### Source Code (repository root)

```text
Assets/Features/Mining/
├── Data/
│   ├── MiningVFXConfig.cs              # NEW: Beam VFX config SO
│   ├── DepletionVFXConfig.cs           # NEW: Depletion VFX config SO
│   ├── OreChunkConfig.cs               # NEW: Ore chunk config SO
│   ├── MiningAudioConfig.cs            # NEW: Audio config SO
│   ├── AsteroidEmissionComponent.cs    # NEW: ECS emission component
│   ├── MiningVFXConfig.asset           # NEW: Config instance
│   ├── DepletionVFXConfig.asset        # NEW: Config instance
│   ├── OreChunkConfig.asset            # NEW: Config instance
│   ├── MiningAudioConfig.asset         # NEW: Config instance
│   ├── NativeMiningActions.cs          # MODIFIED: +NativeThresholdCrossedAction
│   ├── MiningComponents.cs             # EXISTING (unchanged)
│   └── OreTypeDefinition.cs            # EXISTING (unchanged)
├── Systems/
│   ├── AsteroidEmissionSystem.cs       # NEW: Drives emission from depletion
│   ├── AsteroidScaleSystem.cs          # MODIFIED: Enqueue threshold events
│   ├── MiningActionDispatchSystem.cs   # MODIFIED: Drain threshold queue, dispatch depletion tick
│   └── MiningReducer.cs               # MODIFIED: Handle MiningDepletionTickAction
├── Views/
│   ├── MiningBeamView.cs              # MODIFIED: Replace LineRenderer with pulsing beam + sparks + haze
│   ├── DepletionVFXView.cs            # NEW: Crumble bursts + fragment explosion
│   ├── OreChunkController.cs          # NEW: Chunk spawning + pool management
│   ├── OreChunkBehaviour.cs           # NEW: Per-chunk bezier attraction
│   ├── MiningAudioController.cs       # NEW: Spatialized audio
│   └── ProceduralAudioGenerator.cs    # NEW: Placeholder audio clip generation
├── Tests/
│   ├── AsteroidEmissionTests.cs       # NEW: Emission formula tests
│   ├── OreChunkAttractionTests.cs     # NEW: Bezier attraction tests
│   ├── ThresholdEventTests.cs         # NEW: Event bridge tests
│   └── MiningDepletionReducerTests.cs # NEW: Reducer tests
└── Prefabs/                           # NEW folder
    └── (runtime-created, no static prefabs needed)

Assets/Features/HUD/
├── Views/
│   ├── HUDView.cs                     # MODIFIED: Add progress bar + pulse + flash
│   └── HUD.uxml                       # MODIFIED: Add progress bar elements
│   └── HUD.uss                        # MODIFIED: Progress bar styles
└── Tests/
    └── HUDMiningFeedbackTests.cs      # NEW: Progress bar sync tests

Assets/Core/EventBus/
└── Events/
    ├── ThresholdCrossedEvent.cs        # NEW
    └── OreChunkCollectedEvent.cs       # NEW

Assets/Core/State/
├── MiningState.cs                     # MODIFIED: +DepletionFraction field
└── MiningActions.cs                   # MODIFIED: +MiningDepletionTickAction
```

**Structure Decision**: All new code integrates into existing feature folders following the established `Data/Systems/Views/Tests/` pattern. Two new event structs in `Core/EventBus/Events/`. No new top-level folders.

---

## Implementation Phases

### Phase 1: Foundation — ECS Bridge + Config ScriptableObjects

**Goal**: Establish the data pipeline from ECS to managed layer. Create all config ScriptableObjects. Add threshold crossing events and depletion fraction to managed state.

**Checkpoint**: ThresholdCrossedEvent fires on EventBus when mining depletes an asteroid past each 25% threshold. MiningSessionState.DepletionFraction updates every frame during mining.

#### 1.1 New ECS Component: AsteroidEmissionComponent

**File**: `Assets/Features/Mining/Data/AsteroidEmissionComponent.cs`

ECS component for per-entity emission color override. Uses `[MaterialProperty("_EmissionColor")]` attribute from Unity.Rendering to drive URP Lit shader emission via Entities Graphics batch metadata.

**MCP Verification**: After creating script, poll `isCompiling`, then `read_console` for errors.

#### 1.2 New Native Queue: NativeThresholdCrossedAction

**File**: `Assets/Features/Mining/Data/NativeMiningActions.cs` (modify)

Add new unmanaged struct with Entity, ThresholdIndex (byte 0-3), Position (float3), Radius (float). This carries threshold crossing data from Burst-compiled ECS to managed event dispatch.

#### 1.3 New Events: ThresholdCrossedEvent + OreChunkCollectedEvent

**Files**: `Assets/Core/EventBus/Events/ThresholdCrossedEvent.cs`, `Assets/Core/EventBus/Events/OreChunkCollectedEvent.cs`

Readonly struct events published on EventBus. ThresholdCrossedEvent carries asteroid ID, threshold index, position, and radius. OreChunkCollectedEvent carries collection position and ore ID.

#### 1.4 Modify AsteroidScaleSystem: Enqueue Threshold Events

**File**: `Assets/Features/Mining/Systems/AsteroidScaleSystem.cs` (modify)

When `DetectThresholdCrossing()` returns true, enqueue a `NativeThresholdCrossedAction` on a shared NativeQueue. The queue must be allocated in `OnCreate` and exposed as a property for `MiningActionDispatchSystem` to drain.

**Key constraint**: `AsteroidScaleSystem` is Burst-compiled. The NativeQueue must be allocated with `Allocator.Persistent` and accessed via `SystemAPI.GetSingleton` or direct system reference.

#### 1.5 Modify MiningActionDispatchSystem: Drain Threshold Queue + Dispatch Depletion

**File**: `Assets/Features/Mining/Systems/MiningActionDispatchSystem.cs` (modify)

Add fourth queue drain loop for `NativeThresholdCrossedAction`. Publish `ThresholdCrossedEvent` on EventBus for each dequeued action. Also dispatch `MiningDepletionTickAction` with current depletion fraction each frame during active mining (read from `AsteroidComponent.Depletion` via EntityManager).

#### 1.6 Extend MiningSessionState + Reducer

**Files**: `Assets/Core/State/MiningState.cs` (modify), `Assets/Core/State/MiningActions.cs` (modify), `Assets/Features/Mining/Systems/MiningReducer.cs` (modify)

Add `DepletionFraction` (float) to `MiningSessionState` record. Add `MiningDepletionTickAction` record implementing `IMiningAction`. Add reducer case that returns new state with updated depletion.

#### 1.7 All Four ScriptableObject Configs

**Files**: `MiningVFXConfig.cs`, `DepletionVFXConfig.cs`, `OreChunkConfig.cs`, `MiningAudioConfig.cs` under `Assets/Features/Mining/Data/`

Create as ScriptableObjects with `[CreateAssetMenu]` attributes. Field definitions per data-model.md. All fields are serialized with sensible defaults.

**MCP**: Create `.asset` instances via `manage_asset(action="create")` after scripts compile.

#### 1.8 Tests: Reducer + Threshold Bridge

**Files**: `MiningDepletionReducerTests.cs`, `ThresholdEventTests.cs`

- Test `MiningDepletionTickAction` produces new state with updated depletion.
- Test threshold crossing formula with known inputs (existing `DetectThresholdCrossing` is already tested; new tests verify the queue/event bridge integration).

**MCP**: Run EditMode tests via `run_tests` + `get_test_job`.

---

### Phase 2: Mining Laser Beam VFX (P1)

**Goal**: Replace basic LineRenderer beam with pulsing energy beam + ore-colored impact sparks + heat haze at mining arm. All effects track positions every frame and cease cleanly on stop.

**Checkpoint**: Acceptance Scenarios US1.1-US1.6 pass. Beam pulses, sparks spray from asteroid, heat haze visible on arm.

#### 2.1 Upgrade MiningBeamView

**File**: `Assets/Features/Mining/Views/MiningBeamView.cs` (major modification)

Retain LineRenderer for the beam core but add:
- **Pulsing width**: Sinusoidal oscillation of `startWidth`/`endWidth` driven by `MiningVFXConfig.BeamPulseSpeed` and `BeamPulseAmplitude`.
- **Trail overlay**: Second LineRenderer (or TrailRenderer child) with additive shader for glow trail effect.
- **Impact ParticleSystem**: Child ParticleSystem at beam end position. Cone emission, ore-colored particles, `SparkEmissionRate` from config. Positioned at asteroid surface (entity position offset by radius toward ship).
- **Heat haze ParticleSystem**: Child ParticleSystem at beam origin (mining arm transform). Single billboard quad with scrolling distortion texture, opacity from config.
- **Clean shutdown**: On `MiningStoppedEvent`, stop all emission, clear particles, disable renderers.

**New child transforms on barge prefabs**: Add `MiningArmOrigin` and `CollectorPoint` empty GameObjects to SmallMiningBarge, MediumMiningBarge, HeavyMiningBarge prefabs.

**MCP**: Use `manage_gameobject(action="create")` to add child transforms to each barge prefab. Use `manage_components(action="add")` for ParticleSystem components.

#### 2.2 Beam Materials

Create simple materials for beam effects:
- **BeamCore**: URP Unlit, additive blend, tint by ore color at runtime via MaterialPropertyBlock.
- **BeamGlow**: URP Unlit, additive blend, wider/softer, lower opacity.
- **HeatHaze**: URP Unlit with scrolling UV distortion (normal map-based refraction approximation).
- **SparkParticle**: URP Particles/Unlit, additive blend, small circular texture.

**MCP**: Create materials via `manage_asset(action="create", asset_type="Material")`.

---

### Phase 3: Asteroid Depletion Feedback (P2)

**Goal**: Vein glow ramps with depletion. Bright flash + particle burst at each crumble threshold. Final fragment explosion on destruction.

**Checkpoint**: Acceptance Scenarios US2.1-US2.7 pass.

#### 3.1 AsteroidEmissionSystem

**File**: `Assets/Features/Mining/Systems/AsteroidEmissionSystem.cs`

New Burst-compiled ISystem in SimulationSystemGroup, after AsteroidDepletionSystem. Queries all entities with `AsteroidComponent` + `AsteroidEmissionComponent`. Calculates emission intensity from depletion fraction using config values (lerp between min/max intensity, sqrt ease-in curve). Writes to `AsteroidEmissionComponent.Value` as HDR float4.

**Key**: Must add `AsteroidEmissionComponent` to asteroid entities at spawn time. Modify `AsteroidFieldSystem.cs` to include emission component in entity archetype (alongside existing `URPMaterialPropertyBaseColor`).

#### 3.2 Modify AsteroidFieldSystem: Add Emission Component

**File**: `Assets/Features/Procedural/Systems/AsteroidFieldSystem.cs` (modify)

When creating asteroid entities via `RenderMeshUtility.AddComponents`, also add `AsteroidEmissionComponent` with initial value `float4(0,0,0,0)` (no emission at full health).

**Important**: Verify that URP Lit material on asteroids has `_EmissionColor` property enabled. May need to toggle emission on the asteroid material asset.

**MCP**: Use `manage_material(action="set_material_shader_property")` to enable emission on asteroid material.

#### 3.3 DepletionVFXView

**File**: `Assets/Features/Mining/Views/DepletionVFXView.cs`

New MonoBehaviour subscribed to `ThresholdCrossedEvent` via EventBus. On threshold event:
1. Instantiate/activate pooled crumble burst ParticleSystem at asteroid position.
2. Scale burst particle count by `CrumbleBurstCountBase * (CrumbleBurstCountScale ^ thresholdIndex)` — escalating intensity per spec (FR-009).
3. Brief flash: spawn an additive billboard quad scaled to asteroid radius, fade over `CrumbleFlashDuration`.

On final threshold (index 3): spawn fragment explosion ParticleSystem with `FragmentCount` particles, random directions, scale variance from config. Fragments use asteroid mesh pieces (simplified) or simple rock-like quads.

**Injection**: `[Inject] IEventBus eventBus, DepletionVFXConfig config`

#### 3.4 Tests: Emission Formula

**File**: `Assets/Features/Mining/Tests/AsteroidEmissionTests.cs`

Test emission intensity calculation: verify lerp between min/max, sqrt curve, boundary values (0%, 50%, 100%).

---

### Phase 4: Continuous Ore Chunk System (P3)

**Goal**: During active mining, 2-5 cosmetic ore chunks spawn at the asteroid every 3-7 seconds (randomized), drift outward briefly, then attract to barge collector with bezier curve. Collection flash on arrival.

**Checkpoint**: Acceptance Scenarios US3.1-US3.7 pass.

#### 4.1 OreChunkController

**File**: `Assets/Features/Mining/Views/OreChunkController.cs`

MonoBehaviour managing the ore chunk spawn loop. Injected with `IStateStore`, `IEventBus`, `OreChunkConfig`, `AsteroidVisualMappingConfig` (for mesh variants).

**Logic**:
- Subscribe to `MiningStartedEvent` → reset spawn timer with random interval.
- In `Update()`: decrement timer. On expiry, spawn 2-5 chunks at asteroid position (read from ECS via EntityManager, same pattern as MiningBeamView).
- Each chunk: pick random mesh variant (A or B for ore type), random scale in range, random outward direction.
- Subscribe to `MiningStoppedEvent` → stop spawning (in-flight chunks continue).
- Randomize next interval: `Random.Range(SpawnIntervalMin, SpawnIntervalMax)`.

**Object pooling**: Use `UnityEngine.Pool.ObjectPool<OreChunkBehaviour>` (Unity built-in, zero-GC). Pool size 15.

#### 4.2 OreChunkBehaviour

**File**: `Assets/Features/Mining/Views/OreChunkBehaviour.cs`

MonoBehaviour on each pooled chunk GameObject. Manages the chunk lifecycle:

1. **Drift phase** (0-0.75s): Move outward from asteroid in random direction at `InitialDriftSpeed`.
2. **Attract phase** (0.75s-4.5s): Bezier curve interpolation toward barge `CollectorPoint` transform. Control point offset perpendicular to direct path for organic curve (FR-016).
3. **Collect phase**: When distance to collector < threshold, trigger collection flash, publish `OreChunkCollectedEvent`, return to pool.
4. **Safety**: Force-despawn after `MaxLifetime` seconds.

**Zero-GC**: No allocations — pre-allocated bezier control points, cached transforms.

#### 4.3 Tests: Bezier Attraction

**File**: `Assets/Features/Mining/Tests/OreChunkAttractionTests.cs`

Test bezier curve calculation (pure math), test that chunk reaches target within MaxLifetime, test pool reclaim.

---

### Phase 5: HUD Mining Feedback (P4)

**Goal**: Progress bar in mining panel shows depletion %. Color transitions ore→red. Pulses in sync with vein glow. Flashes white on threshold crossings.

**Checkpoint**: Acceptance Scenarios US4.1-US4.5 pass.

#### 5.1 Modify HUD.uxml + HUD.uss

Add progress bar elements to mining-info-panel:
- `mining-progress-bar` (VisualElement, container)
- `mining-progress-fill` (VisualElement, inner fill)
- `mining-progress-flash` (VisualElement, overlay for flash effect)

CSS for progress bar: fixed height, rounded corners, fill width driven by percentage, smooth transitions.

**MCP**: Read current UXML/USS to verify structure before editing.

#### 5.2 Modify HUDView.cs

**File**: `Assets/Features/HUD/Views/HUDView.cs` (modify)

Add progress bar logic in `LateUpdate()`:
- Read `gameState.Loop.Mining.DepletionFraction` (new field from Phase 1).
- Set `mining-progress-fill` width as percentage of container.
- Interpolate fill color: ore color → red/orange using `Color.Lerp` keyed on depletion.
- Pulse effect: sinusoidal opacity modulation on fill bar, frequency matching vein glow.
- Subscribe to `ThresholdCrossedEvent` via EventBus: on event, set flash overlay visible with white background, fade over 0.3s using USS transitions.

#### 5.3 Tests: HUD Sync

**File**: `Assets/Features/HUD/Tests/HUDMiningFeedbackTests.cs`

Test color interpolation formula (pure function). Test that progress bar percentage matches depletion fraction input.

---

### Phase 6: Spatial Audio Feedback (P5)

**Goal**: 6 audio cues: laser hum (looped, pitch ramps), spark crackle (at impact), crumble rumble (at threshold), explosion (at destruction), collection clink (at barge), hum fade-out (on stop).

**Checkpoint**: Acceptance Scenarios US5.1-US5.7 pass.

#### 6.1 ProceduralAudioGenerator

**File**: `Assets/Features/Mining/Views/ProceduralAudioGenerator.cs`

Static utility class generating placeholder AudioClips via `AudioClip.Create()`:
- **LaserHum**: Low-frequency sine wave (80Hz) + harmonics, looping.
- **SparkCrackle**: White noise burst, 0.1s duration.
- **CrumbleRumble**: Low sine sweep (40-60Hz), 0.5s duration.
- **Explosion**: White noise + low sine, 0.8s duration, amplitude envelope.
- **CollectionClink**: High sine ping (2kHz), 0.1s, fast decay.

All clips are runtime-generated on first access and cached. No asset files needed.

#### 6.2 MiningAudioController

**File**: `Assets/Features/Mining/Views/MiningAudioController.cs`

MonoBehaviour managing all mining audio. Injected with `IEventBus`, `IStateStore`, `MiningAudioConfig`.

**AudioSource setup**: 3 pooled AudioSources (one looping for hum, two one-shot for SFX):
- **HumSource**: Looping, 3D spatial blend, positioned at beam midpoint. Pitch driven by `DepletionFraction` (lerp between PitchMin/PitchMax).
- **ImpactSource**: One-shot, 3D, positioned at asteroid. Plays spark crackle continuously while mining.
- **EventSource**: One-shot, 3D, repositioned per event. Plays rumble/explosion/clink.

**Event subscriptions**:
- `MiningStartedEvent` → start hum loop, start crackle.
- `MiningStoppedEvent` → fade out hum over `LaserHumFadeOutDuration`, stop crackle.
- `ThresholdCrossedEvent` → play rumble (index 0-2) or explosion (index 3) at asteroid position.
- `OreChunkCollectedEvent` → play clink at barge position.

**Pitch ramping**: In `LateUpdate()`, read `DepletionFraction` from StateStore, lerp pitch.

---

### Phase 7: Edge Cases + Integration Testing + Performance Profiling

**Goal**: Verify all edge cases from spec. Full playtest in TestScene_MiningField. Performance profiling.

**Checkpoint**: All SC-001 through SC-011 pass. All existing tests pass (SC-010).

#### 7.1 Edge Case Verification

| Edge Case | Verification Method |
|-----------|-------------------|
| Target switch mid-beam | MCP playtest: switch target, verify beam/sparks transition cleanly |
| Chunks in flight on asteroid destroy | Mine to depletion, verify in-flight chunks reach barge |
| Chunks in flight on range exit | Fly away during mining, verify chunks complete journey |
| Target switch resets chunk timer | Switch target, verify chunk timer restarts |
| Crumble burst completes on stop | Stop mining during crumble pause, verify burst animation finishes |
| HUD hidden, VFX continues | Toggle UI, verify 3D effects and audio persist |
| Camera away, particles culled | Rotate camera, verify GPU budget drops (profiler) |

#### 7.2 Performance Profiling

1. Open TestScene_MiningField with 300 asteroids.
2. Activate mining on nearest asteroid.
3. Open Unity Profiler (CPU + GPU modules).
4. Verify: Mining VFX total < 1.5 ms (SC-008).
5. Verify: Asteroid field rendering < 2 ms with emission active (SC-009).
6. Verify: No GC allocations in gameplay frame (Profiler → GC Alloc column).
7. Verify: Steady 60 FPS throughout full mining cycle (SC-011).

**MCP**: Use `manage_editor(action="play")` to enter play mode. Use `read_console` to check for runtime errors.

#### 7.3 Regression Testing

Run all existing tests via MCP: `run_tests(mode="EditMode")` and `run_tests(mode="PlayMode")`. Verify zero failures (SC-010).

#### 7.4 Visual Verification

**MCP**: Use `manage_scene(action="screenshot")` to capture visual state at key moments:
- Beam active with sparks
- Asteroid at 50% depletion (vein glow visible)
- Crumble burst in progress
- Ore chunks in flight
- HUD progress bar at various fill levels

---

## Complexity Tracking

| Deviation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| `AsteroidEmissionComponent` mutable ECS shell | Burst-compiled system needs direct component write for per-frame emission update | Managed-side emission control would require per-entity managed objects (GC pressure) and break Entities Graphics batching |
| `OreChunkBehaviour` mutable fields | View-layer position interpolation for cosmetic chunks requires per-frame mutable state | Immutable approach (new record per frame per chunk) would create ~600 allocations/second across 10 chunks at 60 FPS |
| `MiningAudioController` stateful AudioSource management | Unity AudioSource requires imperative Play/Stop/pitch-set API calls | No functional alternative exists for Unity's audio system; isolation to view layer satisfies constitutional boundary |
