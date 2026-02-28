# Data Model: Mining Loop VFX & Feedback

**Feature**: `003-mining-vfx-feedback` | **Date**: 2026-02-28

## New ScriptableObject Configs

### MiningVFXConfig

**Path**: `Assets/Features/Mining/Data/MiningVFXConfig.cs`
**Asset**: `Assets/Features/Mining/Data/MiningVFXConfig.asset`

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| BeamWidth | float | 0.15 | LineRenderer/Trail width in meters |
| BeamPulseSpeed | float | 3.0 | Pulse animation cycles per second |
| BeamPulseAmplitude | float | 0.3 | Width oscillation range [0,1] |
| SparkEmissionRate | int | 15 | Sparks per second at impact point |
| SparkLifetime | float | 0.4 | Spark particle lifetime in seconds |
| SparkSpeed | float | 3.0 | Initial outward velocity m/s |
| HeatHazeIntensity | float | 0.5 | Distortion quad opacity [0,1] |
| HeatHazeScale | float | 0.3 | Distortion quad size in meters |

### DepletionVFXConfig

**Path**: `Assets/Features/Mining/Data/DepletionVFXConfig.cs`
**Asset**: `Assets/Features/Mining/Data/DepletionVFXConfig.asset`

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| VeinGlowMinIntensity | float | 0.1 | Emission at 0% depletion |
| VeinGlowMaxIntensity | float | 3.0 | Emission at 100% depletion |
| VeinGlowColor | Color | (1, 0.8, 0.4, 1) | Base emission color (warm) |
| VeinGlowPulseSpeed | float | 1.5 | Glow pulse cycles per second |
| VeinGlowPulseAmplitude | float | 0.25 | Pulse intensity oscillation range [0,1] relative to current intensity |
| CrumbleBurstCountBase | int | 8 | Particles at 25% threshold |
| CrumbleBurstCountScale | float | 1.5 | Multiplier per threshold tier |
| CrumbleBurstSpeed | float | 5.0 | Outward velocity m/s |
| CrumbleBurstLifetime | float | 0.5 | Particle lifetime seconds |
| CrumbleFlashDuration | float | 0.3 | Flash intensity ramp seconds |
| FragmentCount | int | 12 | Fragments on final explosion [8-15] |
| FragmentSpeed | float | 4.0 | Outward velocity m/s |
| FragmentLifetime | float | 3.0 | Time to fade and disappear |
| FragmentScaleRange | Vector2 | (0.05, 0.2) | Min/max scale for fragments |

### OreChunkConfig

**Path**: `Assets/Features/Mining/Data/OreChunkConfig.cs`
**Asset**: `Assets/Features/Mining/Data/OreChunkConfig.asset`

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| SpawnIntervalMin | float | 3.0 | Minimum seconds between spawns |
| SpawnIntervalMax | float | 7.0 | Maximum seconds between spawns |
| ChunksPerSpawnMin | int | 2 | Min chunks per event |
| ChunksPerSpawnMax | int | 5 | Max chunks per event |
| ChunkScaleMin | float | 0.03 | Smallest chunk scale |
| ChunkScaleMax | float | 0.12 | Largest chunk scale |
| InitialDriftDuration | float | 0.75 | Seconds of outward drift |
| InitialDriftSpeed | float | 2.0 | Outward drift m/s |
| AttractionSpeed | float | 8.0 | Max attraction speed m/s |
| AttractionAcceleration | float | 3.0 | Attraction ramp-up m/s² |
| CollectionFlashDuration | float | 0.15 | Flash on barge arrival |
| MaxLifetime | float | 5.0 | Force-despawn safety net |
| GlowIntensity | float | 2.0 | Emission intensity on chunks |

### MiningAudioConfig

**Path**: `Assets/Features/Mining/Data/MiningAudioConfig.cs`
**Asset**: `Assets/Features/Mining/Data/MiningAudioConfig.asset`

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| LaserHumClip | AudioClip | (procedural) | Looping beam sound |
| LaserHumBaseVolume | float | 0.6 | Base volume [0,1] |
| LaserHumPitchMin | float | 0.8 | Pitch at 0% depletion |
| LaserHumPitchMax | float | 1.4 | Pitch at 100% depletion |
| LaserHumFadeOutDuration | float | 0.3 | Fade-out seconds on stop |
| SparkCrackleClip | AudioClip | (procedural) | Impact sound |
| SparkCrackleVolume | float | 0.4 | Volume [0,1] |
| CrumbleRumbleClip | AudioClip | (procedural) | Threshold crossing sound |
| CrumbleRumbleVolume | float | 0.7 | Volume [0,1] |
| ExplosionClip | AudioClip | (procedural) | Final destruction sound |
| ExplosionVolume | float | 0.8 | Volume [0,1] |
| CollectionClinkClip | AudioClip | (procedural) | Ore chunk arrival sound |
| CollectionClinkVolume | float | 0.3 | Volume [0,1] |
| MaxAudibleDistance | float | 100.0 | 3D spatial rolloff distance |

## New ECS Components

### AsteroidEmissionComponent

**Purpose**: Per-entity emission color for vein glow (Entities Graphics material property override).

```
[MaterialProperty("_EmissionColor")]
struct AsteroidEmissionComponent : IComponentData
    float4 Value  // RGB = emission color, W unused (HDR intensity baked into RGB)
```

## Modified Records (Immutable State)

### MiningSessionState (extended)

**Added field**:

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| DepletionFraction | float | 0.0 | Current asteroid depletion [0,1] for HUD |

## New Event Structs

### ThresholdCrossedEvent

```
readonly struct ThresholdCrossedEvent
    int AsteroidId          // Entity index
    byte ThresholdIndex     // 0=25%, 1=50%, 2=75%, 3=100%
    float3 Position         // World position for VFX spawn
    float AsteroidRadius    // For scaling burst effects
```

### OreChunkCollectedEvent

```
readonly struct OreChunkCollectedEvent
    float3 Position         // Collection point (barge)
    string OreId            // For audio variation
```

## New Native Queue Struct

### NativeThresholdCrossedAction

```
struct NativeThresholdCrossedAction
    Entity Asteroid
    byte ThresholdIndex     // 0-3
    float3 Position         // Asteroid world position
    float Radius            // Asteroid radius
```

## New Reducer Action

### MiningDepletionTickAction

```
record MiningDepletionTickAction(float DepletionFraction) : IMiningAction
```

**Reducer behavior**: Updates `MiningSessionState.DepletionFraction` field.

## Entity Relationships

```
AsteroidComponent (existing)
  ├── URPMaterialPropertyBaseColor (existing, color lerp)
  ├── AsteroidEmissionComponent (NEW, vein glow)
  └── AsteroidOreComponent (existing, ore type)

MiningBeamComponent (existing, on ship)
  └── reads → AsteroidComponent.Depletion

StateStore.MiningSessionState (extended)
  └── DepletionFraction (NEW, drives HUD)

EventBus
  ├── ThresholdCrossedEvent (NEW) → VFX bursts, audio rumble, HUD flash
  ├── MiningYieldEvent (existing) → (unused by VFX)
  ├── MiningStartedEvent (existing) → beam VFX on, laser hum start
  ├── MiningStoppedEvent (existing) → beam VFX off, laser hum fade
  └── OreChunkCollectedEvent (NEW) → collection flash, clink audio
```

## Object Pool Entities

### OreChunkPool

- **Pool size**: 15 (accommodates ~10 in flight + buffer)
- **Prefab**: Runtime-created mesh + MeshRenderer + emission material
- **Components**: Transform, MeshFilter, MeshRenderer, OreChunkBehaviour (managed)
- **Lifecycle**: Activate on spawn → bezier attract → deactivate on collect
