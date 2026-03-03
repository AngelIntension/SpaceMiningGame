# Spec 009: Data-Driven Station, World & Editor Config

Use this as the input to `/speckit.specify`:

---

## Feature Description

Make all remaining hard-coded game entity configuration data-driven via ScriptableObjects. This spec creates StationDefinition SOs (consolidating station identity, position, services, docking, and prefab references), CameraConfig SOs, InputConfig SOs, and inventory-from-ship derivation. It also adds editor tooling (Scene Config Inspector, OnValidate improvements, and consistent asset organization) to make the designer workflow intuitive and error-resistant.

This builds on the bugfix pass in Spec 008. Spec 008 MUST be completed first.

---

## Gap 1: Station World Definition is Hard-Coded (HIGHEST PRIORITY)

### Current State
Station data is embedded directly in C# code in `Assets/Core/RootLifetimeScope.cs` method `CreateDefaultGameState()`:
```csharp
new StationData(1, new float3(0f, 0f, 200f), "Small Mining Relay",
    ImmutableArray.Create("Refinery", "Cargo"))
new StationData(2, new float3(500f, 0f, 0f), "Medium Refinery Hub",
    ImmutableArray.Create("Refinery", "Market", "Repair", "Cargo"))
```

Station IDs, positions, names, and available service lists are all magic values. Adding a new station requires editing C# code, recompiling, and knowing the internal wiring.

### Existing Related Assets
- `StationPresetConfig` SO (`Assets/Features/Base/Data/`) — defines station module layout (prefab pieces, positions, roles) but NOT world placement or service capabilities
- `StationServicesConfig` SO (`Assets/Features/StationServices/Data/`) — defines per-station service parameters (refining slots, speed multiplier, repair cost)
- `StationServicesConfigMap` SO — maps station ID (int) → StationServicesConfig reference
- `WorldState.Stations` — `ImmutableArray<StationData>` in the game state, populated from hard-coded data

### Required: StationDefinition ScriptableObject
Create a new `StationDefinition` ScriptableObject that consolidates ALL station configuration:

```
StationDefinition : ScriptableObject
├── StationId (int) — unique identifier, used as key everywhere
├── DisplayName (string) — shown in UI
├── Description (string, TextArea) — tooltip/info panel text
├── StationType (enum: MiningRelay, RefineryHub, TradePost, ResearchStation, etc.)
├── WorldPosition (Vector3) — spawn position in world space
├── WorldRotation (Quaternion) — spawn orientation
├── AvailableServices (string[]) — service identifiers ("Refinery", "Market", "Repair", "Cargo")
├── ServicesConfig (StationServicesConfig reference) — refining slots, speed, repair cost
├── PresetConfig (StationPresetConfig reference) — physical module layout
├── DockingPortOffset (Vector3) — local-space offset for docking port
├── DockingPortRotation (Quaternion) — local-space rotation for docking port
├── Prefab (GameObject reference) — station prefab for scene instantiation
├── Icon (Sprite) — for HUD target cards and menus
└── SafeUndockDirection (Vector3) — preferred undock thrust vector
```

**Attributes**: `[CreateAssetMenu(menuName = "VoidHarvest/Station/Station Definition")]`

**Inspector UX**: Use `[Header]` sections ("Identity", "World Placement", "Services", "Docking", "Visuals"), `[Tooltip]` on every field, `[Range]` where applicable.

**OnValidate**: StationId > 0, DisplayName not empty, ServicesConfig not null, at least one service in AvailableServices, DockingPortOffset magnitude < 200 (sanity check).

### Required: WorldDefinition ScriptableObject
Create a `WorldDefinition` SO that holds the list of all stations in a scene/world:

```
WorldDefinition : ScriptableObject
├── Stations (StationDefinition[]) — all stations in this world
├── PlayerStartPosition (Vector3) — where the player ship spawns
├── PlayerStartRotation (Quaternion)
└── StartingShipArchetype (ShipArchetypeConfig reference) — which ship the player starts with
```

**Attributes**: `[CreateAssetMenu(menuName = "VoidHarvest/World/World Definition")]`

**OnValidate**: No duplicate StationIds, all StationDefinition references non-null, at least one station, StartingShipArchetype not null.

### Required: Wiring Changes
1. `RootLifetimeScope` receives `WorldDefinition` via `[SerializeField]` inspector assignment (or VContainer registration from SceneLifetimeScope)
2. `CreateDefaultGameState()` iterates `WorldDefinition.Stations` to build `WorldState.Stations` instead of hard-coded values
3. `StationServicesConfigMap` becomes OBSOLETE — the mapping is now embedded in `StationDefinition.ServicesConfig`
4. `StationServicesMenuController` resolves config via `WorldDefinition.FindStation(stationId).ServicesConfig` instead of `StationServicesConfigMap.GetConfig(stationId)`
5. Station GameObjects in the scene read their `StationDefinition` SO for position, rotation, and docking port setup
6. `TargetableStation` MonoBehaviour gets its `TargetId` from the `StationDefinition.StationId` field instead of a manually-assigned inspector int

### Migration
- Create 2 StationDefinition assets: `SmallMiningRelay.asset` and `MediumRefineryHub.asset` with all current values
- Create 1 WorldDefinition asset: `DefaultWorld.asset` referencing both stations
- Delete hard-coded station data from `RootLifetimeScope`
- Deprecate `StationServicesConfigMap` (remove after migration verified)
- Update all tests that reference station IDs or create mock station data

---

## Gap 2: Docking System Hard-Coded Overrides (MEDIUM PRIORITY)

### Current State
`DockingConfig` ScriptableObject exists with configurable values, but `Assets/Features/Docking/Systems/DockingSystem.cs` (Burst-compiled ISystem) has LOCAL HARD-CODED CONSTANTS that override the SO values:
```csharp
// Inside DockingSystem.OnUpdate():
float snapDuration = 0.75f;
float undockClearanceDistance = 100f;
float snapRange = 5f;
float approachTimeout = 120f;
float alignTimeout = 30f;
float alignDotThreshold = 0.999f;
float alignAngVelThreshold = 0.01f;
```

These were hard-coded because Burst-compiled systems cannot access managed ScriptableObjects directly.

### Required Fix: DockingConfigBlob BlobAsset
Create a blob asset pipeline (same pattern as `OreTypeBlobDatabase`):

```
DockingConfigBlob (BlobAsset)
├── MaxDockingRange (float)
├── SnapRange (float)
├── SnapDuration (float)
├── UndockClearanceDistance (float)
├── UndockDuration (float)
├── ApproachTimeout (float)
├── AlignTimeout (float)
├── AlignDotThreshold (float)
├── AlignAngVelThreshold (float)
└── ... any future docking params

DockingConfigBlobComponent (IComponentData, singleton)
└── BlobAssetReference<DockingConfigBlob>
```

**Baking pipeline**:
1. Add `DockingConfigAuthoring` MonoBehaviour that references `DockingConfig` SO
2. `DockingConfigBaker` converts SO fields → BlobAsset → `DockingConfigBlobComponent` singleton
3. `DockingSystem` reads `DockingConfigBlobComponent` instead of local constants
4. Add any new fields to `DockingConfig` SO that are currently only hard-coded (`ApproachTimeout`, `AlignTimeout`, `AlignDotThreshold`, `AlignAngVelThreshold`)

---

## Gap 3: Camera Limits Hard-Coded (MEDIUM PRIORITY)

### Current State
`Assets/Features/Camera/Systems/CameraReducer.cs` has:
```csharp
private const float MinPitch = -80f;
private const float MaxPitch = 80f;
private const float MinDistance = 5f;
private const float MaxDistance = 50f;
private const float MinZoomDistance = 10f;
private const float MaxZoomDistance = 40f;
```

`Assets/Features/Camera/Views/CameraView.cs` has:
```csharp
private const float ZoomCooldownDuration = 2.0f;
```

### Required: CameraConfig ScriptableObject
```
CameraConfig : ScriptableObject
├── MinPitch (float, Range -89 to 0)
├── MaxPitch (float, Range 0 to 89)
├── MinDistance (float, > 0)
├── MaxDistance (float, > MinDistance)
├── MinZoomDistance (float, >= MinDistance)
├── MaxZoomDistance (float, <= MaxDistance)
├── ZoomCooldownDuration (float, >= 0)
├── DefaultYaw (float)
├── DefaultPitch (float)
├── DefaultDistance (float)
└── OrbitSensitivity (float, > 0)
```

**Attributes**: `[CreateAssetMenu(menuName = "VoidHarvest/Camera/Camera Config")]`

**Wiring**:
1. Register in `SceneLifetimeScope`
2. Inject into `CameraReducer` (requires making it non-static — convert from static class to injectable service, or pass config through the state/action)
3. Inject into `CameraView`
4. Since `CameraReducer` is a pure static function used in the `CompositeReducer`, the config values may need to be stored in `CameraState` itself (set once at init from CameraConfig SO) or passed as part of the reduce function signature

**Note**: The CameraReducer is currently a pure static method: `public static CameraState Reduce(CameraState state, ICameraAction action)`. To inject config, either:
- (A) Add config fields to `CameraState` itself (set at initialization), so the reducer reads limits from state
- (B) Change reducer signature to accept config: `Reduce(CameraState state, ICameraAction action, CameraConfig config)` and update CompositeReducer to pass it
- Option (A) is preferred — it keeps the reducer pure and the config becomes part of the initial state snapshot

---

## Gap 4: Inventory Capacity From Ship Archetype (LOW PRIORITY)

### Current State
`InventoryState.Empty` has hard-coded defaults:
```csharp
MaxSlots: 20, MaxVolume: 100f
```

These should derive from the active ship's `ShipArchetypeConfig.CargoCapacity` and a configurable slot count.

### Required Changes
1. Add `CargoSlots (int)` field to `ShipArchetypeConfig` (alongside existing `CargoCapacity` which is volume)
2. When `CreateDefaultGameState()` builds the initial state, read `WorldDefinition.StartingShipArchetype.CargoCapacity` and `.CargoSlots` for `InventoryState`
3. When fleet ship swap occurs (Phase 1+), update `InventoryState.MaxSlots` and `MaxVolume` from the new ship's archetype
4. Add `OnValidate` to `ShipArchetypeConfig`: `CargoSlots >= 1`, `CargoCapacity > 0`

---

## Gap 5: Input Timing Config (LOW PRIORITY)

### Current State
`Assets/Features/Input/Views/InputBridge.cs`:
```csharp
private const float DoubleClickWindow = 0.3f;
private const float RadialMenuDragThreshold = 5f;
```

`Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs`:
```csharp
private const float DefaultApproachDistance = 50f;
private const float DefaultOrbitDistance = 100f;
private const float DefaultKeepAtRangeDistance = 50f;
```

### Required: InteractionConfig ScriptableObject
```
InteractionConfig : ScriptableObject
├── DoubleClickWindow (float, Range 0.1 to 1.0, default 0.3)
├── RadialMenuDragThreshold (float, Range 1 to 20, default 5)
├── DefaultApproachDistance (float, > 0, default 50)
├── DefaultOrbitDistance (float, > 0, default 100)
├── DefaultKeepAtRangeDistance (float, > 0, default 50)
└── MiningBeamMaxRange (float, > 0, default 500) — currently implicit
```

**Attributes**: `[CreateAssetMenu(menuName = "VoidHarvest/Input/Interaction Config")]`

**Wiring**: Register in `SceneLifetimeScope`, inject into `InputBridge` and `RadialMenuController`.

---

## Gap 6: Editor Tooling — Scene Config Inspector (MEDIUM PRIORITY)

### Problem
`SceneLifetimeScope` has 11+ `[SerializeField]` config fields that must be manually assigned in the Inspector. Missing a field causes a silent null at runtime (the `if (config != null)` guard in `Configure()` skips registration). There's no editor-time warning.

### Required: SceneConfigValidator EditorWindow
Create `Assets/Core/Editor/SceneConfigValidator.cs`:
- Menu item: `VoidHarvest > Validate Scene Config`
- Finds all `SceneLifetimeScope` components in open scenes
- For each serialized field: checks if assigned, reports missing as warning
- Finds all `WorldDefinition` references and validates station completeness
- Finds all authoring components and validates SO references
- Output: Console log with clear warnings, or simple EditorWindow with green/red status per field

### Required: WorldDefinition Custom Editor
Create `Assets/Core/Editor/WorldDefinitionEditor.cs`:
- Shows station list with inline preview (name, position, service count)
- "Validate All" button that checks StationDefinition completeness
- Warning badges for missing ServicesConfig, PresetConfig, or Prefab references

---

## Gap 7: Consistent Asset Folder Organization (LOW PRIORITY)

### Current State
Station-related assets are scattered:
- Station presets: `Assets/Features/Base/Data/`
- Station service configs: `Assets/Features/StationServices/Data/Assets/StationConfigs/`
- Raw materials: `Assets/Features/StationServices/Data/Assets/RawMaterials/`
- Docking configs: `Assets/Features/Docking/Data/Configs/`

### Required Convention
After creating StationDefinition SOs, organize:
```
Assets/Features/Station/Data/
├── Definitions/           ← StationDefinition assets (SmallMiningRelay, MediumRefineryHub)
├── ServiceConfigs/        ← StationServicesConfig assets
├── Presets/               ← StationPresetConfig assets (moved from Base/Data)
└── RawMaterials/          ← RawMaterialDefinition assets (moved from StationServices)

Assets/Features/World/Data/
└── DefaultWorld.asset     ← WorldDefinition

Assets/Features/Camera/Data/
└── DefaultCameraConfig.asset  ← CameraConfig

Assets/Features/Input/Data/
└── DefaultInteractionConfig.asset  ← InteractionConfig
```

Note: Moving assets requires updating all SO references. Use Unity's asset move (right-click > Move) to preserve GUIDs, or update references manually.

---

## Scope Boundaries

### IN SCOPE
- StationDefinition & WorldDefinition ScriptableObjects with full inspector UX
- DockingConfigBlob blob asset pipeline
- CameraConfig ScriptableObject
- InteractionConfig ScriptableObject
- Inventory capacity from ShipArchetypeConfig
- SceneConfigValidator editor window
- WorldDefinition custom editor
- Migration of existing hard-coded station data to SO assets
- Deprecation of StationServicesConfigMap
- Asset folder reorganization
- Unit tests for all new SOs, blob baking, OnValidate, and world initialization
- Update HOWTOPLAY.md if any player-facing behavior changes

### OUT OF SCOPE
- New station types or services (use the new SO system to add them later)
- Station runtime spawning/despawning (stations remain static scene objects for MVP)
- Custom property drawers for nested types (future polish)
- Per-feature asset browser EditorWindows (future polish)
- Wizard dialogs for "Add New Station" (future polish)
- Dynamic world generation (Phase 2+)

### DEPENDENCIES
- Spec 008 (Bugfix & Polish) MUST be completed first — it fixes event lifecycle and UI bugs that this spec's new wiring depends on
- All 465 existing tests must continue to pass
- New tests required for: StationDefinition OnValidate, WorldDefinition OnValidate, DockingConfigBlob baking, CameraConfig integration, world initialization from WorldDefinition

---

## Existing Architecture Context

### State Store & Reducer Pattern
- `GameState` (sealed record) contains `WorldState` with `ImmutableArray<StationData>`
- `StationData` is a readonly struct: `(int Id, float3 Position, string Name, ImmutableArray<string> Services)`
- `RootLifetimeScope.CreateDefaultGameState()` builds initial state — this is where WorldDefinition replaces hard-coded data
- `CompositeReducer` routes actions to feature reducers — no changes needed to reducer architecture
- `DockingReducer` references `StationData.Id` — keep the same ID system

### ECS Baking Pattern (Established)
- `OreTypeBlobBakingSystem` converts `OreDefinition[]` → `BlobAssetReference<OreTypeBlobDatabase>` → singleton component
- Same pattern should be used for `DockingConfigBlob`
- Authoring component on SubScene entity → Baker creates blob → System reads singleton

### VContainer DI Pattern
- `RootLifetimeScope`: global singletons (StateStore, EventBus)
- `SceneLifetimeScope`: scene-specific configs (all SO references)
- MonoBehaviours use `[Inject] public void Construct(...)` for dependency injection
