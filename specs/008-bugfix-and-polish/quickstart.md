# Quickstart: Bugfix, Event Lifecycle & UI Polish

**Date**: 2026-03-02 | **Branch**: `feature/008-bugfix-and-polish`

## Prerequisites

- Unity 6 (6000.3.10f1) with project open
- All 465 existing tests passing (verify via Test Runner or MCP)

## Implementation Order

This spec modifies existing files only — no new features, no new domain types.

### Phase 1: State-Change Detection Fixes (Critical)
1. Fix `RefineOresPanelController.ListenForStateChanges()` — add `InventoryState` slice tracking
2. Fix `SellResourcesPanelController.ListenForStateChanges()` — add `InventoryState` slice tracking
3. Verify `CargoTransferPanelController` and `BasicRepairPanelController` logic is already correct
4. Write tests verifying each panel refreshes when any single slice changes

### Phase 2: Subscription Lifecycle Migration (Critical)
1. `RadialMenuController` — move async EventBus subscription from `Start()` to `OnEnable()`, cancellation from `OnDestroy()` to `OnDisable()`
2. `TargetingAudioController` — same migration
3. `StationServicesMenuController` — same migration
4. All panel controllers + `CreditBalanceIndicator` — add `OnDestroy()` safety net
5. Write tests verifying no duplicate subscriptions after disable/enable cycles

### Phase 3: Silent Input Failures (Medium)
1. `InputBridge.StartMining()` — add `ClearSelectionAction` dispatch + warning
2. `InputBridge.InitiateDocking()` — add warning logging
3. `InputBridge.OnHotbar1()` — add entity existence validation
4. `InputBridge.TryInitializeECS()` — add throttled warning logging
5. `InputBridge.SyncSelectionFromState()` — subscribe to `StateChangedEvent`
6. `RadialMenuController` — add null warning for `TargetingController`

### Phase 4: FindObjectOfType → DI (Medium)
1. Register `InputBridge`, `CameraView`, `TargetingController`, `TargetPreviewManager` in `SceneLifetimeScope`
2. Add `[Inject]` methods to consumers
3. Remove replaced `FindObjectOfType` calls
4. Add `Debug.LogWarning` to remaining `FindObjectOfType` calls
5. Add warning for `CinemachineCamera` null result

### Phase 5: Time Source Fix (Low)
1. `RefiningJobTicker.Update()` — change `Time.time` to `Time.realtimeSinceStartup`
2. `RefineOresPanelController` — change `Time.time` to `Time.realtimeSinceStartup` in dispatch
3. Update existing test fixtures if they hard-code `Time.time` comparisons

### Phase 6: ScriptableObject Validation (Low)
1. Add `OnValidate()` to all 8 SO types (OreDefinition, ShipArchetypeConfig, AsteroidFieldDefinition, StationServicesConfig, DockingConfig, RawMaterialDefinition, StationServicesConfigMap, GameServicesConfig)
2. Write tests for each: valid config → no warnings, invalid config → appropriate warnings

### Phase 7: Create Menu Standardization (Low)
1. Update all 19 `CreateAssetMenu` attributes to `VoidHarvest/<System>/<AssetType>` format

### Phase 8: Regression & Documentation
1. Run full test suite — all 465+ tests must pass
2. Update HOWTOPLAY.md if any player-facing behavior changed (unlikely for this spec)

## Verification

After implementation:
- Zero compilation errors in Unity console
- All existing tests pass + new tests pass (target: 495+ total)
- Dock/undock/redock cycle shows no duplicate events
- Station panels refresh correctly on any single state slice change
- No silent failures in InputBridge (all paths log or act)
