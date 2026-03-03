# Research: Bugfix, Event Lifecycle & UI Polish

**Date**: 2026-03-02 | **Spec**: [spec.md](spec.md)

## R-001: UI State-Change Detection Logic

**Decision**: Fix all multi-slice panel controllers to use OR-skip logic (skip only when NO slice changed) instead of AND-skip.

**Findings**:
- **CargoTransferPanelController** (line 82): `if (ReferenceEquals(inv, _lastInventory) && ReferenceEquals(svc, _lastServices)) continue;` — BUG: requires BOTH unchanged to skip, but the `&&` means it skips if BOTH are unchanged. This is actually correct semantics (skip when NOTHING changed). **Re-analysis**: The `&&` is correct — it means "skip if inv unchanged AND svc unchanged" = "skip if nothing changed." The actual bug per the prompt is that RefineOresPanelController only checks one slice. Let me re-examine.
- Actually, re-reading: `if (both unchanged) continue` IS correct. The bug is that when only `StationServicesState` changes (but not `InventoryState`), the `&&` evaluates to `false` (since svc IS changed), so it does NOT skip. **Conclusion: CargoTransfer logic is correct as-is.**
- **RefineOresPanelController** (line 129): Only checks `StationServicesState`. **BUG**: Misses inventory changes that affect available ore quantities for refining. Needs to also check `InventoryState`.
- **SellResourcesPanelController** (line 85): Only checks `StationServicesState`. **BUG**: Misses inventory changes that affect what's available to sell from ship cargo.
- **BasicRepairPanelController** (line 75): Checks `StationServicesState` AND `ActiveShipPhysics`. Same AND-skip pattern as CargoTransfer — semantically correct.

**Resolution**: The core bug is that RefineOres and SellResources panels don't watch `InventoryState` changes. Add inventory slice tracking to both. CargoTransfer and BasicRepair patterns are already correct.

**Alternatives considered**: Always-refresh approach (no reference checks). Rejected: unnecessary work when no state has changed; immutable reference equality is an efficient optimization.

---

## R-002: Async Subscription Lifecycle Convention

**Decision**: Standardize on `OnEnable()` subscribe / `OnDisable()` cancel-dispose for all async event listeners.

**Findings**:
- **Current patterns** (3 distinct):
  1. `Start()`/`OnDestroy()` — RadialMenuController, TargetingAudioController, StationServicesMenuController
  2. `Initialize()`/`Cleanup()` — All 4 panel controllers, CreditBalanceIndicator (external caller manages lifecycle)
  3. None have `OnEnable()`/`OnDisable()` for async subscriptions

- **Problem with pattern 1**: If GameObject disabled (not destroyed), subscriptions leak. Re-enable creates duplicates.
- **Problem with pattern 2**: No `OnDestroy()` safety net. If parent destroyed before calling `Cleanup()`, subscription leaks.
- **RadialMenuController already has** `OnEnable()`/`OnDisable()` for UIElements callbacks but NOT for async EventBus subscriptions.

**Resolution**:
- Pattern 1 components (RadialMenu, TargetingAudio): Move async subscription to `OnEnable()`/`OnDisable()`.
- Pattern 2 components (panel controllers, CreditBalanceIndicator): Add `OnDestroy()` safety net calling `Cleanup()`. Keep `Initialize()`/`Cleanup()` since they're called by parent. The `Initialize()` method already cancels-and-recreates, so it's safe for repeated calls.
- StationServicesMenuController: Move own async subscriptions to `OnEnable()`/`OnDisable()`. Add `OnDestroy()` safety net.

---

## R-003: Silent Input Failures

**Decision**: Add defensive logging and selection clearing to all silent-failure paths in InputBridge.

**Findings**:
- `StartMining()` (line 457): Returns silently when `_selectedAsteroidEntity == Entity.Null || !_entityManager.Exists(...)`. Should clear selection + log.
- `InitiateDocking()` (line 534): Returns silently when `!_ecsReady || !_entityManager.Exists(_shipEntity) || port == null`. Should log.
- `OnHotbar1()` (line 443): Validates `_selectedTargetId >= 0` but not entity existence. Should validate entity.
- `TryInitializeECS()` (line 170): Zero logging on failure. Called every frame. Should log once after N frames (throttle).
- `SyncSelectionFromState()` (line 405): Only called from `OnRadialMenuRelease()`. Should also respond to `StateChangedEvent`.

**Resolution**: Add `Debug.LogWarning` with `[InputBridge]` prefix to all silent paths. For `TryInitializeECS`, use a frame counter to log once after ~60 frames of failure. For `SyncSelectionFromState`, subscribe to `StateChangedEvent` and call sync when `Selection.TargetId` changes.

---

## R-004: FindObjectOfType Replacement Strategy

**Decision**: Register view-layer MonoBehaviours in SceneLifetimeScope for DI injection. Add `Debug.LogWarning` fallbacks.

**Findings**:
- **9 total FindObjectOfType/FindFirstObjectByType/FindObjectsByType calls** across 5 files:
  - InputBridge: `CameraView` (Start), `TargetableStation[]` (SyncSelection)
  - RadialMenuController: `InputBridge` (Start, has warning), `TargetingController` (Start, NO warning)
  - TargetingController: `TargetPreviewManager` (Start), `CinemachineCamera` (Start), `TargetableStation[]` (runtime)
  - TargetPreviewManager: `TargetableStation[]` (runtime)
  - StationServicesMenuController: `InputBridge` (Start, NO warning)

- **Current DI setup**: Only `IEventBus` and `IStateStore` in `RootLifetimeScope`. `SceneLifetimeScope` registers SO configs only. No view-layer MBs registered.

- **Components to register in SceneLifetimeScope**: `InputBridge`, `CameraView`, `TargetingController`, `TargetPreviewManager`. These are scene-placed singletons.
- **Cannot replace**: `FindObjectsByType<TargetableStation>()` calls are runtime queries for dynamically-placed stations. Keep as-is with null checks.
- **CinemachineCamera**: Third-party component, not VContainer-managed. Keep `FindObjectOfType` with warning.

**Resolution**: Register `InputBridge`, `CameraView`, `TargetingController`, `TargetPreviewManager` in `SceneLifetimeScope` using `RegisterComponentInHierarchy<T>()`. Replace `FindObjectOfType` calls with `[Inject]` method injection. Keep `FindObjectsByType` for runtime station queries and `CinemachineCamera` with warning fallback.

---

## R-005: RefiningJobTicker Time Source

**Decision**: Switch to `Time.realtimeSinceStartup` in both ticker and job dispatch.

**Findings**:
- `RefiningJobTicker.Update()` line 32: `float currentTime = Time.time;`
- `RefineOresPanelController` line 254: Passes `Time.time` as `StartTime` to `StartRefiningJobAction`
- `RefiningJobState` record stores `StartTime` as float
- Ticker compares: `currentTime < job.StartTime + job.TotalDuration`
- Both sides must use the same time source for consistent comparison

**Resolution**: Change both to `Time.realtimeSinceStartup`. This is a two-point change (ticker + dispatcher). Existing tests pass `0f` as start time, so they're time-source-agnostic.

---

## R-006: ScriptableObject OnValidate Pattern

**Decision**: Add `OnValidate()` to all 8 SOs using `Debug.LogWarning` with asset name prefix. No clamping (warn only).

**Findings**:
- Only `SkyboxConfig` currently has `OnValidate()` (clamps values)
- 8 SOs need validation: OreDefinition, ShipArchetypeConfig, AsteroidFieldDefinition, StationServicesConfig, DockingConfig, RawMaterialDefinition, StationServicesConfigMap, GameServicesConfig
- Total ~40 field validations across all 8 SOs
- Each SO has clearly defined field types and expected ranges (see spec FR-019 through FR-026)

**Resolution**: Implement `OnValidate()` in each SO with `Debug.LogWarning($"[{name}] field ...")` pattern. Warn-only (no clamping) to avoid silently masking bad data. Tests will validate that valid assets produce no warnings and invalid fields produce appropriate warnings.

**Alternatives considered**: Clamping invalid values (as SkyboxConfig does). Rejected for most fields: silently correcting data hides design errors. Warn-only preserves designer intent while surfacing problems.

---

## R-007: Create Menu Path Standardization

**Decision**: Standardize all to `VoidHarvest/<System>/<AssetType>` format.

**Findings** — Current vs Target:

| SO | Current menuName | Target menuName |
|----|-----------------|-----------------|
| OreDefinition | `VoidHarvest/Ore Definition` | `VoidHarvest/Mining/Ore Definition` |
| ShipArchetypeConfig | `VoidHarvest/ShipArchetypeConfig` | `VoidHarvest/Ship/Ship Archetype Config` |
| AsteroidFieldDefinition | `VoidHarvest/Asteroid Field Definition` | `VoidHarvest/Procedural/Asteroid Field Definition` |
| StationServicesConfig | `VoidHarvest/Station Services Config` | `VoidHarvest/Station/Station Services Config` |
| RawMaterialDefinition | `VoidHarvest/Raw Material Definition` | `VoidHarvest/Station/Raw Material Definition` |
| GameServicesConfig | `VoidHarvest/Game Services Config` | `VoidHarvest/Station/Game Services Config` |
| StationServicesConfigMap | `VoidHarvest/Station Services Config Map` | `VoidHarvest/Station/Station Services Config Map` |
| DockingConfig | `VoidHarvest/Docking/DockingConfig` | `VoidHarvest/Docking/Docking Config` |
| DockingVFXConfig | `VoidHarvest/DockingVFXConfig` | `VoidHarvest/Docking/Docking VFX Config` |
| DockingAudioConfig | `VoidHarvest/DockingAudioConfig` | `VoidHarvest/Docking/Docking Audio Config` |
| MiningVFXConfig | `VoidHarvest/Mining/MiningVFXConfig` | `VoidHarvest/Mining/Mining VFX Config` |
| MiningAudioConfig | `VoidHarvest/Mining/MiningAudioConfig` | `VoidHarvest/Mining/Mining Audio Config` |
| OreChunkConfig | `VoidHarvest/Mining/OreChunkConfig` | `VoidHarvest/Mining/Ore Chunk Config` |
| DepletionVFXConfig | `VoidHarvest/Mining/DepletionVFXConfig` | `VoidHarvest/Mining/Depletion VFX Config` |
| TargetingConfig | `VoidHarvest/Targeting Config` | `VoidHarvest/Targeting/Targeting Config` |
| TargetingVFXConfig | `VoidHarvest/Targeting VFX Config` | `VoidHarvest/Targeting/Targeting VFX Config` |
| TargetingAudioConfig | `VoidHarvest/Targeting Audio Config` | `VoidHarvest/Targeting/Targeting Audio Config` |
| StationPresetConfig | `VoidHarvest/StationPresetConfig` | `VoidHarvest/Station/Station Preset Config` |
| SkyboxConfig | `VoidHarvest/SkyboxConfig` | `VoidHarvest/Camera/Skybox Config` |

**Resolution**: 19 `CreateAssetMenu` attributes need updating. Pure string changes — no logic impact.
