# Quickstart: Mining Loop VFX & Feedback

**Feature**: `003-mining-vfx-feedback` | **Date**: 2026-02-28

## Prerequisites

- Unity 6 (6000.3.10f1) with URP 17.3.0
- All packages from spec 002 installed (Entities 1.3.2, Entities Graphics 1.3.2, etc.)
- Premium assets in default import locations (SF_Asteroids-M2, Retora Modular Space Ship Pack)
- Unity MCP server running in Editor

## Implementation Order

1. **Phase 1**: ECS bridge + configs (no visual changes yet)
2. **Phase 2**: Mining laser beam VFX (P1)
3. **Phase 3**: Asteroid depletion feedback (P2)
4. **Phase 4**: Continuous ore chunk system (P3)
5. **Phase 5**: HUD progress bar (P4)
6. **Phase 6**: Spatial audio (P5)
7. **Phase 7**: Edge cases + profiling

## Key Architecture Decisions

- **ParticleSystem over VFX Graph**: VFX Graph not installed; ParticleSystem sufficient for ~25 concurrent particles.
- **NativeQueue bridge for threshold events**: Extends existing pattern from MiningBeamSystem. New queue in AsteroidScaleSystem drained by MiningActionDispatchSystem.
- **Depletion in managed state**: New `DepletionFraction` field in `MiningSessionState` so HUDView can read from StateStore (not ECS directly).
- **Object pooling for ore chunks**: `ObjectPool<OreChunkBehaviour>` with 15 capacity. Zero GC.
- **Procedural placeholder audio**: `AudioClip.Create()` with sine/noise waveforms. No external files.

## Files to Create (17 new)

| File | Layer | Purpose |
|------|-------|---------|
| `Mining/Data/MiningVFXConfig.cs` | Data | Beam config SO |
| `Mining/Data/DepletionVFXConfig.cs` | Data | Depletion config SO |
| `Mining/Data/OreChunkConfig.cs` | Data | Chunk config SO |
| `Mining/Data/MiningAudioConfig.cs` | Data | Audio config SO |
| `Mining/Data/AsteroidEmissionComponent.cs` | Data | ECS emission component |
| `Mining/Systems/AsteroidEmissionSystem.cs` | Systems | Emission from depletion |
| `Mining/Views/DepletionVFXView.cs` | Views | Crumble bursts + fragments |
| `Mining/Views/OreChunkController.cs` | Views | Chunk spawn loop + pool |
| `Mining/Views/OreChunkBehaviour.cs` | Views | Per-chunk bezier movement |
| `Mining/Views/MiningAudioController.cs` | Views | Spatial audio controller |
| `Mining/Views/ProceduralAudioGenerator.cs` | Views | Placeholder clip generator |
| `Mining/Tests/AsteroidEmissionTests.cs` | Tests | Emission formula tests |
| `Mining/Tests/OreChunkAttractionTests.cs` | Tests | Bezier math tests |
| `Mining/Tests/ThresholdEventTests.cs` | Tests | Event bridge tests |
| `Mining/Tests/MiningDepletionReducerTests.cs` | Tests | Reducer tests |
| `EventBus/Events/ThresholdCrossedEvent.cs` | Core | New event struct |
| `EventBus/Events/OreChunkCollectedEvent.cs` | Core | New event struct |

## Files to Modify (10 existing)

| File | Change |
|------|--------|
| `Mining/Data/NativeMiningActions.cs` | +NativeThresholdCrossedAction struct |
| `Mining/Systems/AsteroidScaleSystem.cs` | +Enqueue threshold events |
| `Mining/Systems/MiningActionDispatchSystem.cs` | +Drain threshold queue, +dispatch depletion tick |
| `Mining/Systems/MiningReducer.cs` | +MiningDepletionTickAction case |
| `Mining/Views/MiningBeamView.cs` | Upgrade beam + add sparks + heat shimmer |
| `Core/State/MiningState.cs` | +DepletionFraction field |
| `Core/State/MiningActions.cs` | +MiningDepletionTickAction record |
| `HUD/Views/HUDView.cs` | +Progress bar logic |
| `HUD/Views/HUD.uxml` | +Progress bar elements |
| `HUD/Views/HUD.uss` | +Progress bar styles |

## Verification Checklist

After each phase, run this MCP verification loop:

1. `refresh_unity(compile="request", wait_for_ready=true)`
2. `read_console(types=["error"])` → must be empty
3. `run_tests(mode="EditMode")` → `get_test_job(job_id=...)` → all pass
4. `manage_scene(action="screenshot")` → visual check
