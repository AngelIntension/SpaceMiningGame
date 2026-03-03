# Spec 008: Bugfix, Event Lifecycle & UI Polish

Use this as the input to `/speckit.specify`:

---

## Feature Description

Systematic bugfix and polish pass across all Phase 0 systems. This spec addresses confirmed bugs causing menus not responding, UI panels showing stale data, event subscription leaks, silent failures in input handling, and missing edit-time validation on ScriptableObjects.

This is NOT a new feature — it is a targeted refactor of existing code to fix defects and harden the architecture. No new gameplay mechanics are introduced.

---

## Bug Category 1: UI State-Change Detection Failures (CRITICAL)

### Problem
Multiple station-service panel controllers use `ReferenceEquals` to skip UI refreshes when a state slice reference hasn't changed. The `&&` logic means if ONLY ONE slice changes, the refresh is skipped entirely.

### Affected Files & Exact Locations
1. **`Assets/Features/StationServices/Views/CargoTransferPanelController.cs`** — `ListenForStateChanges()` method
   - Uses: `if (ReferenceEquals(inv, _lastInventory) && ReferenceEquals(svc, _lastServices)) continue;`
   - Bug: When a refining job completes, `StationServicesState` reference changes but `InventoryState` does NOT change. The `&&` means BOTH must change for UI to refresh. Result: cargo panel shows stale data after job completion.

2. **`Assets/Features/StationServices/Views/RefineOresPanelController.cs`** — `ListenForStateChanges()` method
   - Uses: `if (ReferenceEquals(svc, _lastServices)) continue;`
   - Bug: Less severe (single-slice check), but still skips refresh if an unrelated dispatch triggers `StateChangedEvent` without changing `StationServicesState`. This causes the refining panel to miss updates when inventory changes affect available ore quantities.

3. **`Assets/Features/StationServices/Views/SellResourcesPanelController.cs`** — same pattern if present.

4. **`Assets/Features/StationServices/Views/BasicRepairPanelController.cs`** — same pattern if present.

### Required Fix
Each panel should refresh when ANY of its relevant state slices change, using `||` (OR) instead of `&&` (AND):
```csharp
if (ReferenceEquals(inv, _lastInventory) && ReferenceEquals(svc, _lastServices))
    continue; // Skip only if NEITHER changed — this is correct
```
OR simply always refresh and let the UI diff handle no-ops. The immutable state guarantees mean reference equality is a valid optimization only when ALL relevant slices are checked with `&&` (both unchanged = skip). Verify the logic in each file individually.

---

## Bug Category 2: Event Subscription Lifecycle Leaks (HIGH)

### Problem
All async event listeners (`ListenForStateChanges`, `ListenForRadialMenuEvents`, etc.) are started in `Start()` and cancelled only in `OnDestroy()`. If a GameObject is **disabled** via `SetActive(false)` rather than destroyed, subscriptions keep running in the background. When re-enabled, NEW subscriptions are created without cancelling the old ones, causing duplicate handlers.

### Affected Files & Required Changes

1. **`Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs`**
   - Has: `OnDestroy()` cancels `_eventCts`
   - Missing: `OnDisable()` — subscription continues when menu is hidden
   - Fix: Move cancellation to `OnDisable()`, move subscription start to `OnEnable()`

2. **`Assets/Features/Targeting/Views/TargetingAudioController.cs`**
   - Has: `OnDestroy()` cancels `_eventCts`
   - Missing: `OnDisable()` — audio subscriptions leak, causing duplicate playback on scene reload
   - Fix: Same pattern — `OnDisable()` cancel, `OnEnable()` subscribe

3. **`Assets/Features/StationServices/Views/CargoTransferPanelController.cs`**
   - Has: Manual `Cleanup()` method called by parent
   - Missing: Own `OnDestroy()` fallback — if parent destroys first, subscription leaks
   - Fix: Add `OnDestroy() { Cleanup(); }` as safety net

4. **`Assets/Features/StationServices/Views/RefineOresPanelController.cs`** — same as above

5. **`Assets/Features/StationServices/Views/SellResourcesPanelController.cs`** — same as above

6. **`Assets/Features/StationServices/Views/BasicRepairPanelController.cs`** — same as above

7. **`Assets/Features/StationServices/Views/CreditBalanceIndicator.cs`** — same as above

8. **`Assets/Features/StationServices/Views/StationServicesMenuController.cs`**
   - Has: `OnDestroy()` calls `CleanupControllers()`
   - Risk: If child controllers are destroyed before parent, `CleanupControllers()` calls `Cleanup()` on already-destroyed objects
   - Fix: Wrap each cleanup call in try-catch, or better: each child handles its own lifecycle

### Recommended Pattern
Establish a project-wide convention for async event subscriptions:
```csharp
private CancellationTokenSource _eventCts;

private void OnEnable()
{
    _eventCts = new CancellationTokenSource();
    ListenForEvents(_eventCts.Token).Forget();
}

private void OnDisable()
{
    _eventCts?.Cancel();
    _eventCts?.Dispose();
    _eventCts = null;
}
```
This ensures subscriptions are always paired with the component's active lifecycle.

---

## Bug Category 3: Silent Input Failures (MEDIUM)

### Problem
`InputBridge` and `RadialMenuController` silently fail when ECS isn't ready or when target entities no longer exist. The player clicks buttons/hotkeys and nothing happens — no feedback, no error, no indication of why.

### Affected Files & Required Changes

1. **`Assets/Features/Input/Views/InputBridge.cs`**
   - `StartMining()`: Returns silently if `_selectedAsteroidEntity` doesn't exist. Should dispatch `ClearSelectionAction` and log a warning.
   - `InitiateDocking()`: Returns silently if `_ecsReady` is false or docking port is null. Should log warning.
   - `OnHotbar1()`: Checks `_selectedTargetId >= 0` but not whether the entity still exists. Should validate entity existence.
   - `TryInitializeECS()`: No logging when ECS init fails repeatedly. Should log once after N frames.
   - `SyncSelectionFromState()`: Only called from `OnRadialMenuRelease()`. Should also be called when `StateChangedEvent` fires with a different `Selection.TargetId`, so InputBridge stays in sync with state store.

2. **`Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs`**
   - `Start()`: `_targetingController = FindObjectOfType<TargetingController>()` has no null warning. If TargetingController is missing, "Lock Target" silently fails.
   - Fix: Log warning if null, optionally disable lock segment in menu.

---

## Bug Category 4: FindObjectOfType Fragility (MEDIUM)

### Problem
Multiple controllers use `FindObjectOfType<T>()` in `Start()` to locate peer components. If the target hasn't enabled yet (Unity doesn't guarantee `Start()` order across GameObjects), it returns null. This creates fragile, order-dependent initialization.

### Affected Calls
- `RadialMenuController.Start()` → `FindObjectOfType<InputBridge>()`
- `RadialMenuController.Start()` → `FindObjectOfType<TargetingController>()`
- `StationServicesMenuController.Start()` → `FindObjectOfType<InputBridge>()`
- `TargetingController.Start()` → `FindObjectOfType<TargetPreviewManager>()`
- `TargetingController.Start()` → `FindObjectOfType<CinemachineCamera>()`
- `InputBridge.Start()` → `FindObjectOfType<CameraView>()`

### Required Fix
Replace with VContainer `[Inject]` constructor injection where possible. For components that can't be registered in VContainer (e.g., scene-placed MonoBehaviours not in the DI container), use `[Inject]` method injection or register them in `SceneLifetimeScope`. As a minimum, add `Debug.LogWarning` for all null results so failures are visible in the console.

---

## Bug Category 5: RefiningJobTicker Time Source (LOW)

### Problem
`Assets/Features/StationServices/Views/RefiningJobTicker.cs` uses `Time.time` which stops advancing when `Time.timeScale = 0`. If a pause menu is ever added, refining jobs freeze. The `StartRefiningJobAction` also records `Time.time` as the start time.

### Required Fix
Use `Time.realtimeSinceStartup` in both `RefiningJobTicker.Update()` and `RefineOresPanelController` where `StartRefiningJobAction` is dispatched. This ensures refining continues during pause (which is the expected real-time behavior for an EVE-style game where station processes are autonomous).

---

## Bug Category 6: ScriptableObject Edit-Time Validation (MEDIUM)

### Problem
Only `SkyboxConfig` has `OnValidate()`. All other ScriptableObjects allow invalid configurations (zero mass, negative yield, etc.) with no editor feedback.

### Required OnValidate() Additions

1. **`OreDefinition`** — Validate: BaseYieldPerSecond > 0, Hardness > 0, VolumePerUnit > 0, BaseValue >= 0, RarityWeight in [0,1], BaseProcessingTimePerUnit > 0, RefiningCreditCostPerUnit >= 0, OreId not empty, DisplayName not empty. Each RefiningOutputEntry: Material not null, BaseYieldPerUnit > 0, VarianceMin <= VarianceMax.

2. **`ShipArchetypeConfig`** — Validate: Mass > 0, MaxThrust > 0, MaxSpeed > 0, RotationTorque > 0, LinearDamping >= 0, AngularDamping >= 0, MiningPower >= 0, ModuleSlots >= 0, CargoCapacity > 0, BaseLockTime > 0, MaxTargetLocks >= 1, MaxLockRange > 0, ArchetypeId not empty, DisplayName not empty.

3. **`AsteroidFieldDefinition`** — Validate: AsteroidCount > 0, FieldRadius > 0, AsteroidSizeMin > 0, AsteroidSizeMax >= AsteroidSizeMin, RotationSpeedMax >= RotationSpeedMin, MinScaleFraction in [0.1, 0.5], at least one OreEntry with non-null OreDefinition and Weight > 0.

4. **`StationServicesConfig`** — Validate: MaxConcurrentRefiningSlots >= 1, RefiningSpeedMultiplier > 0, RepairCostPerHP >= 0.

5. **`DockingConfig`** — Validate: MaxDockingRange > 0, SnapRange > 0 and < MaxDockingRange, SnapDuration > 0, UndockClearanceDistance > 0, UndockDuration > 0.

6. **`RawMaterialDefinition`** — Validate: MaterialId not empty, DisplayName not empty.

7. **`StationServicesConfigMap`** — Validate: No duplicate StationIds, all StationServicesConfig references non-null.

8. **`GameServicesConfig`** — Validate: StartingCredits >= 0.

### Pattern
Use `Debug.LogWarning($"[{name}] FieldX must be > 0, got {FieldX}")` with clamping where safe, or just warnings for fields that can't be auto-corrected.

---

## Bug Category 7: Create Menu Path Consistency (LOW)

### Problem
Create asset menu paths are inconsistently nested:
- Some use sub-menus: `VoidHarvest/Mining/MiningVFXConfig`
- Others are flat: `VoidHarvest/DockingVFXConfig`, `VoidHarvest/DockingAudioConfig`
- Some group by system, others don't

### Required Fix
Standardize all Create menus to: `VoidHarvest/<System>/<AssetType>`:
- `VoidHarvest/Mining/Ore Definition`
- `VoidHarvest/Mining/Mining VFX Config`
- `VoidHarvest/Mining/Mining Audio Config`
- `VoidHarvest/Mining/Ore Chunk Config`
- `VoidHarvest/Mining/Depletion VFX Config`
- `VoidHarvest/Ship/Ship Archetype Config`
- `VoidHarvest/Procedural/Asteroid Field Definition`
- `VoidHarvest/Docking/Docking Config`
- `VoidHarvest/Docking/Docking VFX Config`
- `VoidHarvest/Docking/Docking Audio Config`
- `VoidHarvest/Station/Station Services Config`
- `VoidHarvest/Station/Station Services Config Map`
- `VoidHarvest/Station/Game Services Config`
- `VoidHarvest/Station/Station Preset Config`
- `VoidHarvest/Station/Raw Material Definition`
- `VoidHarvest/Targeting/Targeting Config`
- `VoidHarvest/Targeting/Targeting Audio Config`
- `VoidHarvest/Targeting/Targeting VFX Config`
- `VoidHarvest/Camera/Skybox Config`

---

## Scope Boundaries

### IN SCOPE
- Fix all bugs listed above
- Add OnValidate to all SOs
- Standardize Create menu paths
- Add defensive logging for silent failures
- Replace FindObjectOfType with DI injection where feasible
- Establish and document async subscription lifecycle convention
- Unit tests for all OnValidate logic
- Unit tests verifying state-change detection correctness

### OUT OF SCOPE
- New gameplay features
- Data-driven station/world config (separate spec 009)
- Custom editor windows or property drawers (separate spec 009)
- PlayMode integration tests (deferred)
- VFX/audio polish (deferred)
- Performance optimization (deferred)

---

## Existing Test Baseline
- 465 tests currently pass across 29 assembly definitions
- All existing tests MUST continue to pass after these changes
- New tests required for: OnValidate logic, state-change detection fix verification, subscription lifecycle correctness
