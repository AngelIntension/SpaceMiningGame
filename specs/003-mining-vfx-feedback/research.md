# Research: Mining Loop VFX & Feedback

**Feature**: `003-mining-vfx-feedback` | **Date**: 2026-02-28

## R1: VFX Technology — VFX Graph vs Legacy Particle System

**Decision**: Use legacy **ParticleSystem + TrailRenderer** for beam and sparks; defer VFX Graph installation.

**Rationale**:
- `com.unity.visualeffectgraph` is **not installed** in the project. Adding it introduces a new package dependency and compile-time cost for the entire project.
- ParticleSystem is fully supported in URP 17.3.0 with GPU instancing, culling, and sub-emitter support — sufficient for all spec requirements (sparks, bursts, fragments, ore chunk glow).
- VFX Graph's primary advantage (GPU compute-based simulation) is overkill for the particle counts in this spec (~15 burst particles, ~10 chunks in flight). The performance budget (1.5 ms) is easily achievable with CPU ParticleSystem.
- TrailRenderer provides the pulsing energy beam effect (FR-006) more naturally than ParticleSystem trails.
- Constitution says "prefer VFX Graph for Burst/DOTS compatibility where possible" — the view-layer VFX are MonoBehaviour-based (not DOTS systems), so DOTS compatibility is not a factor here.

**Alternatives Considered**:
- VFX Graph: Would require package install, VFX Graph asset creation pipeline, and learning curve. Better suited for high-particle-count effects (thousands of particles). Deferred to spec 003.1 if needed.
- Custom GPU instanced particle system: Over-engineering for ~25 simultaneous particles.

## R2: ECS-to-Managed Bridge for Threshold Crossings

**Decision**: Extend existing NativeQueue bridge pattern — add `NativeThresholdCrossedAction` queue enqueued by `AsteroidScaleSystem`, drained by `MiningActionDispatchSystem`, published as `ThresholdCrossedEvent` via EventBus.

**Rationale**:
- Threshold crossings are currently **ECS-internal only** (bitmask on `AsteroidComponent`). No events reach the managed layer.
- The NativeQueue→EventBus pattern is already proven in `MiningBeamSystem`→`MiningActionDispatchSystem` for yield/depletion/stop actions.
- Adding a new queue follows the established architecture with minimal risk.
- Alternative (polling ECS state from MonoBehaviour): Would require per-asteroid tracking in managed code, adding GC pressure and complexity. Rejected.

**Alternatives Considered**:
- Direct ECS polling from MonoBehaviour (like MiningBeamView): Works for continuous data (beam position) but poor for one-shot events (threshold crossings can be missed between frames).
- Managed system reading bitmask changes: Same polling problem; requires per-entity state tracking dictionary in managed code.

## R3: Depletion Data for HUD Progress Bar

**Decision**: Add `float DepletionFraction` field to `MiningSessionState` record. Updated via new `MiningDepletionTickAction` dispatched from `MiningActionDispatchSystem` each frame during active mining.

**Rationale**:
- `MiningSessionState` currently has no depletion fraction — only `YieldAccumulator`. The HUD needs depletion for the progress bar (FR-019).
- Adding to managed state follows the reducer pattern and allows HUDView to read from `StateStore.Current` (same pattern as existing velocity/hull/inventory display).
- Alternative (HUD reads ECS directly): Would break the View→StateStore→Reducer pattern mandated by the constitution. Views read StateStore, not ECS.

## R4: Asteroid Vein Glow Implementation

**Decision**: Use a new ECS component `AsteroidEmissionComponent` (float EmissionIntensity) alongside existing `URPMaterialPropertyBaseColor`. The emission intensity is driven by depletion fraction in a new `AsteroidEmissionSystem`.

**Rationale**:
- Current asteroids use `URPMaterialPropertyBaseColor` for per-instance color. URP Entities Graphics supports additional per-instance material property overrides.
- Adding `[MaterialProperty("_EmissionColor")]` via a custom `URPMaterialPropertyEmissionColor` component enables per-entity emission without shader changes — URP Lit shader already has `_EmissionColor` property.
- The `_EmissionColor` property is built into URP/Lit shader — no Shader Graph work needed.
- Emission intensity ramps with `sqrt(depletion)` (same ease-in curve as color darkening) for visual consistency.

**Alternatives Considered**:
- Custom Shader Graph: Over-engineering; URP Lit already supports emission.
- Modifying `_BaseColor` alpha channel: Alpha is already used for fade-out by `AsteroidDestroySystem`.

## R5: Heat Haze Effect

**Decision**: Use a simple particle-based approach with a transparent, UV-scrolling distortion texture on a ParticleSystem quad near the mining arm. Not screen-space post-processing.

**Rationale**:
- Full screen-space distortion requires a custom URP Renderer Feature — high complexity for a subtle effect.
- A localized particle quad with a scrolling normal-mapped distortion texture achieves "heat shimmer" at minimal cost.
- Can be toggled/intensity-controlled via ParticleSystem emission rate, fitting the ScriptableObject config pattern.
- Explicitly out of scope: "Screen-space post-processing effects specific to mining."

## R6: Audio Infrastructure

**Decision**: Create lightweight `MiningAudioController` MonoBehaviour using Unity's built-in `AudioSource` components. No AudioMixer needed for this phase. Placeholder audio clips generated via AudioClip.Create() with procedural sine/noise waveforms.

**Rationale**:
- No audio infrastructure exists in the project. Starting with raw `AudioSource` components is the simplest viable approach.
- AudioMixer adds complexity (asset creation, routing) with marginal benefit for 6 sound cues. Defer mixer to a dedicated audio spec.
- Procedural placeholder clips avoid the need for external audio files while proving the spatial audio pipeline.
- All clips referenced via `MiningAudioConfig` ScriptableObject for easy replacement with real assets later.

## R7: Ore Chunk Attraction Physics

**Decision**: Managed-code `MonoBehaviour.Update()` with simple bezier-curve interpolation. Not DOTS physics.

**Rationale**:
- Peak ~10 chunks in flight simultaneously. DOTS physics is massive overkill.
- Bezier curves provide the "smooth, organic gentle curve" required by FR-016 with zero physics engine overhead.
- Chunks are view-layer objects (cosmetic only) — constitutional alignment: side effects isolated to view layer.
- Object pooling via `ObjectPool<T>` prevents GC allocations from chunk spawning/despawning.
