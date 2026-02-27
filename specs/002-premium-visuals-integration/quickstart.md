# Quickstart: Premium Visuals Asset Integration

**Branch**: `002-premium-visuals-integration` | **Date**: 2026-02-27

## What This Feature Does

Integrates four purchased premium asset packs into VoidHarvest to replace placeholder visuals with EVE Online-grade industrial sci-fi aesthetics. Adds asteroid depletion shrink/crumble feedback, three mining barge variants, and two station presets.

## Key Files to Understand

### Existing (modified)
- `Assets/Features/Procedural/Systems/AsteroidFieldSystem.cs` — Asteroid spawning; extended to use visual mapping config
- `Assets/Features/Mining/Systems/AsteroidDepletionSystem.cs` — Depletion color; extended with scale/crumble/fade logic
- `Assets/Features/Mining/Data/MiningComponents.cs` — ECS components; AsteroidComponent gets 3 new fields
- `Assets/Features/Procedural/Views/AsteroidPrefabAuthoring.cs` — Authoring; updated for multi-mesh prefab support
- `Assets/Features/Ship/Data/StarterMiningBarge.asset` — Renamed display to "Small Mining Barge"

### New
- `Assets/Features/Procedural/Data/AsteroidVisualMappingConfig.cs` — ScriptableObject mapping ore→mesh+tint
- `Assets/Features/Procedural/Data/AsteroidVisualMappingConfig.asset` — Config instance
- `Assets/Features/Mining/Systems/AsteroidScaleSystem.cs` — Depletion shrink + crumble pauses
- `Assets/Features/Mining/Systems/AsteroidDestroySystem.cs` — Entity cleanup after fade-out
- `Assets/Features/Camera/Data/SkyboxConfig.cs` — ScriptableObject for per-scene skybox
- `Assets/Features/Camera/Views/SkyboxController.cs` — MonoBehaviour applying skybox
- `Assets/Features/Ship/Data/MediumMiningBarge.asset` — New ship archetype
- `Assets/Features/Ship/Data/HeavyMiningBarge.asset` — New ship archetype
- `Assets/Features/Ship/Prefabs/SmallMiningBarge.prefab` — Retora modular assembly
- `Assets/Features/Ship/Prefabs/MediumMiningBarge.prefab` — Retora modular assembly
- `Assets/Features/Ship/Prefabs/HeavyMiningBarge.prefab` — Retora modular assembly
- `Assets/Features/Base/Data/StationPresetConfig.cs` — ScriptableObject for station presets
- `Assets/Features/Base/Prefabs/SmallMiningRelay.prefab` — Station preset
- `Assets/Features/Base/Prefabs/MediumRefineryHub.prefab` — Station preset

### Asset Packs (unchanged, referenced)
- `Assets/SF_Asteroids-M2/Prefabs/Mineral_asteroid-{01-06}.prefab`
- `Assets/Nebula Skybox Pack Vol. II .../HDRI/Materials/HDR_Nebula_2_Pro_{1-12}.mat`
- `Assets/Retora - Modular Space Ship Pack/Prefabs/{HullParts,DoorParts,etc.}.prefab`
- `Assets/Station_MS2/Prefabs/MS2_{Type}_{Color} Variant.prefab`

## How to Test

1. **Skybox**: Open GameScene → verify nebula renders, rotates slowly, ambient matches
2. **Asteroids**: Enter play mode → fly to asteroid field → verify mesh variety, ore tinting, LOD transitions
3. **Depletion shrink**: Mine any asteroid → observe continuous shrink with crumble pauses at 75/50/25% → observe fade-out and removal at 0%
4. **Ships**: Swap ship config to Medium/Heavy in ShipAuthoring → play → verify flight controls work identically
5. **Stations**: Open TestScene_Station → verify both presets render correctly, no gaps
6. **Performance**: Unity Profiler → asteroid field < 2ms, station < 5ms, overall 60 FPS
7. **Regression**: Run all 17 existing tests via Test Runner → zero failures
