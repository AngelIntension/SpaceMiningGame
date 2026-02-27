# Data Model: Premium Visuals Asset Integration

**Branch**: `002-premium-visuals-integration` | **Date**: 2026-02-27

## New Entities

### SkyboxConfig (ScriptableObject)

Configuration asset for per-scene nebula skybox setup.

| Field | Type | Description |
|-------|------|-------------|
| SkyboxMaterial | Material | Reference to one of 12 Nebula HDRI materials (Skybox/Panoramic shader) |
| FallbackMaterial | Material | Fallback material if primary fails to load (default: SpaceSkybox.mat) |
| RotationSpeed | float | Skybox rotation speed in degrees/second (default: 0.5, range 0.0–5.0) |
| ExposureOverride | float | Exposure adjustment (default: 1.0, range 0.1–3.0) |

**Lifecycle**: Created once per scene by designer. Read-only at runtime.
**Relationships**: Referenced by SkyboxController (MonoBehaviour, view layer).

### AsteroidVisualMappingConfig (ScriptableObject)

Maps ore types to visual mesh/material variants.

| Field | Type | Description |
|-------|------|-------------|
| Entries | AsteroidVisualEntry[] | Array of ore-type-to-visual mappings |
| MinScaleFraction | float | Minimum scale multiplier at full depletion (default: 0.3, range 0.1–0.5). Prevents asteroids from becoming too small to see/target before removal. |

#### AsteroidVisualEntry (Serializable struct)

| Field | Type | Description |
|-------|------|-------------|
| OreId | string | Matches OreTypeDefinition.OreId (e.g., "veldspar"). Named OreId (not OreTypeId) to avoid confusion with AsteroidOreComponent.OreTypeId (int index at runtime). |
| MeshVariantA | Mesh | First mesh variant for this ore type |
| MeshVariantB | Mesh | Second mesh variant for this ore type |
| TintColor | Color | Subtle color tint matching the ore's beam color |

**Ore-to-mesh mapping** (from clarification):
- Veldspar → Mineral_asteroid-01 + 02, tint: tan/gold (0.82, 0.71, 0.55)
- Scordite → Mineral_asteroid-03 + 04, tint: determined by Scordite.BeamColor
- Pyroxeres → Mineral_asteroid-05 + 06, tint: determined by Pyroxeres.BeamColor

**Lifecycle**: Created once by designer. Read at spawn time by AsteroidFieldSystem.
**Relationships**: Referenced by AsteroidFieldSystem; OreIds must match OreTypeDefinition assets.

### StationPresetConfig (ScriptableObject)

Documents a station module assembly for prefab creation and future procedural generation.

| Field | Type | Description |
|-------|------|-------------|
| PresetName | string | Display name (e.g., "Small Mining Relay") |
| PresetId | string | Unique identifier |
| Description | string | Designer description of this station type |
| Modules | StationModuleEntry[] | Ordered list of module placements |

#### StationModuleEntry (Serializable struct)

| Field | Type | Description |
|-------|------|-------------|
| ModulePrefab | GameObject | Reference to a Station_MS2 prefab |
| LocalPosition | Vector3 | Position offset relative to station root |
| LocalRotation | Quaternion | Rotation relative to station root |
| ModuleRole | string | Functional role (e.g., "control", "storage", "energy") |

**Lifecycle**: Created once by designer. Currently informational — stations are pre-assembled prefabs. Will drive procedural generation in Phase 2+.
**Relationships**: References Station_MS2 prefab GameObjects.

## Modified Entities

### AsteroidComponent (ECS Component — extended)

Existing mutable ECS shell. New fields added for depletion visuals.

| Field | Type | Status | Description |
|-------|------|--------|-------------|
| Radius | float | Existing | Asteroid radius in meters |
| InitialMass | float | Existing | Mass at spawn (kg) |
| RemainingMass | float | Existing | Current mass after extraction (kg) |
| Depletion | float | Existing | Depletion fraction [0, 1] |
| **PristineTintedColor** | **float4** | **New** | Ore-tinted pristine color, set at spawn: pristineGray (0.314) × oreTintColor. Used by AsteroidDepletionSystem as the base color for depletion lerp instead of hardcoded constant. |
| **CrumbleThresholdsPassed** | **byte** | **New** | Bitmask tracking crossed thresholds: bit0=75%, bit1=50%, bit2=25%, bit3=0% |
| **CrumblePauseTimer** | **float** | **New** | Countdown timer for current crumble pause (0 = no pause active) |
| **FadeOutTimer** | **float** | **New** | Countdown timer for fade-out after final crumble (0 = no fade active; < 0 = ready for removal) |

**Threshold bitmask logic**:
- When depletion crosses 25% (RemainingMass/InitialMass ≤ 0.75): set bit0, start CrumblePauseTimer
- When depletion crosses 50%: set bit1, start CrumblePauseTimer
- When depletion crosses 75%: set bit2, start CrumblePauseTimer
- When depletion reaches 100%: set bit3, start CrumblePauseTimer → on expiry, start FadeOutTimer

**State transitions**:
```
Normal → [threshold crossed] → CrumblePause → Normal
Normal → [threshold crossed] → CrumblePause → Normal → ... (repeat per threshold)
Normal → [0% reached] → FinalCrumblePause → FadeOut → Destroyed
```

### ShipArchetypeConfig (ScriptableObject — 2 new instances)

Existing config. Small Barge = existing StarterMiningBarge asset (renamed display name, same ID). Two new asset instances created:

**MediumMiningBarge.asset**:
| Field | Value |
|-------|-------|
| ArchetypeId | "medium-mining-barge" |
| DisplayName | "Medium Mining Barge" |
| Mass | 2500 |
| MaxThrust | 8000 |
| MaxSpeed | 75 |
| RotationTorque | 35 |
| MiningPower | 1.5 |
| ModuleSlots | 6 |
| CargoCapacity | 250 |

**HeavyMiningBarge.asset**:
| Field | Value |
|-------|-------|
| ArchetypeId | "heavy-mining-barge" |
| DisplayName | "Heavy Mining Barge" |
| Mass | 5000 |
| MaxThrust | 12000 |
| MaxSpeed | 50 |
| RotationTorque | 20 |
| MiningPower | 2.0 |
| ModuleSlots | 8 |
| CargoCapacity | 500 |

**StarterMiningBarge.asset** (existing, updated):
- DisplayName changed from "Starter Mining Barge" to "Small Mining Barge"
- ArchetypeId remains "starter-mining-barge" for backward compatibility
- All stat values unchanged

## New Systems

### SkyboxController (MonoBehaviour — View Layer)

| Responsibility | Detail |
|---------------|--------|
| Read SkyboxConfig | On scene start, apply skybox material to RenderSettings.skybox |
| Rotation | Each frame: increment _Rotation shader property by RotationSpeed * Time.deltaTime |
| Ambient Lighting | Set RenderSettings.ambientMode to match HDRI; apply exposure |
| Fallback | If SkyboxMaterial is null, use FallbackMaterial |

### AsteroidScaleSystem (Burst-compiled ECS System)

Runs in `SimulationSystemGroup` after `AsteroidDepletionSystem`.

| Responsibility | Detail |
|---------------|--------|
| Scale calculation | `scale = Radius * lerp(MinScaleFraction, 1.0, RemainingMass/InitialMass)` where MinScaleFraction is read from AsteroidVisualMappingConfig |
| Crumble threshold | Detect threshold crossings via bitmask; start CrumblePauseTimer |
| Crumble pause | While timer > 0: freeze scale, decrement timer |
| Start fade-out | At 0% depletion after final crumble pause expires: start FadeOutTimer |

**MinScaleFraction**: Defined on AsteroidVisualMappingConfig (default 0.3, range 0.1–0.5) — prevents asteroid from becoming too small to see/target.

### AsteroidDestroySystem (Burst-compiled ECS System)

Runs in `SimulationSystemGroup` after `AsteroidScaleSystem`.

| Responsibility | Detail |
|---------------|--------|
| Fade-out visual | While FadeOutTimer > 0: interpolate AsteroidBaseColorOverride.Value.w (alpha) from 1→0. Requires Alpha Clipping on asteroid materials (see note below). |
| Untargetable | When FadeOutTimer first becomes active (> 0): remove/disable targeting components so player cannot re-target during fade |
| Entity destruction | When FadeOutTimer < 0: destroy entity via EntityCommandBuffer |
| State update | Destroyed entities are implicitly removed from all queries |

**Alpha Clipping Note**: Asteroid materials must have Alpha Clipping enabled (URP Lit setting) with `_Cutoff` ≈ 0.5. The `_BaseColor.a` channel (driven by `AsteroidBaseColorOverride.Value.w`) controls visibility: values above `_Cutoff` render normally, values below are clipped. During fade-out, alpha interpolates 1→0, producing a clip-based vanish when it crosses the cutoff threshold. Smooth dissolve effects are deferred to Phase 1.2 VFX.

## Unchanged Systems

These systems require **no modifications**:

| System | Reason |
|--------|--------|
| MiningReducer | Pure reducer — operates on state records, not ECS components |
| MiningBeamSystem | Already handles depleted asteroids (caps yield at 0, enqueues DepletedAction) |
| MiningActionDispatchSystem | Already processes DepletedQueue and publishes MiningStoppedEvent |
| ShipPhysicsSystem | No change — ship physics independent of visual assets |
| ShipStateReducer | No change — flight model unchanged |
| GameStateReducer | No change — state records unchanged |
| AsteroidFieldGeneratorJob | No change — produces positions and ore IDs only; visual mapping is downstream |
