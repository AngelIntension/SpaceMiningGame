# Data Model: Bugfix, Event Lifecycle & UI Polish

**Date**: 2026-03-02 | **Spec**: [spec.md](spec.md)

This spec introduces no new domain data types. All changes are behavioral fixes to existing types and patterns.

## Modified Entities

### RefiningJobState (existing — no field changes)

```
RefiningJobState (sealed record)
├── JobId: string
├── OreId: string
├── InputQuantity: int
├── StartTime: float          ← Semantics change: now stores Time.realtimeSinceStartup
├── TotalDuration: float
├── CreditCostPaid: int
├── Status: RefiningJobStatus
├── OutputConfigs: ImmutableArray<RefiningOutputConfig>
└── GeneratedOutputs: ImmutableArray<MaterialOutput>
```

**Change**: `StartTime` previously stored `Time.time`. Now stores `Time.realtimeSinceStartup`. No type or field change — only the value domain changes.

### StartRefiningJobAction (existing — no field changes)

```
StartRefiningJobAction (sealed record) : IStationServicesAction
├── StationId: int
├── OreId: string
├── InputQuantity: int
├── TotalCost: int
├── TotalDuration: float
├── OutputConfigs: ImmutableArray<RefiningOutputConfig>
├── MaxActiveSlots: int
└── StartTime: float           ← Caller now passes Time.realtimeSinceStartup
```

## Behavioral Patterns (New Conventions)

### Async Subscription Lifecycle Convention

Standard pattern for all MonoBehaviours with UniTask EventBus subscriptions:

```
MonoBehaviour Lifecycle
├── OnEnable()
│   ├── _eventCts = new CancellationTokenSource()
│   └── ListenForEvents(_eventCts.Token).Forget()
├── OnDisable()
│   ├── _eventCts?.Cancel()
│   ├── _eventCts?.Dispose()
│   └── _eventCts = null
└── OnDestroy()  [safety net for externally-managed controllers]
    └── Cleanup()  [if not already called]
```

**Applies to**: RadialMenuController, TargetingAudioController, StationServicesMenuController, all panel controllers, CreditBalanceIndicator.

### State-Change Detection Convention

Multi-slice panel controllers must check ALL relevant slices with AND-logic for skip:

```
Pattern: Skip refresh only when NO relevant slice changed

if (ReferenceEquals(sliceA, _lastA) && ReferenceEquals(sliceB, _lastB))
    continue;   // Both unchanged → skip (correct)

// If ANY slice changed, fall through to refresh
```

**Applies to**: CargoTransferPanelController (Inventory + Services), RefineOresPanelController (Services + Inventory), SellResourcesPanelController (Services + Inventory), BasicRepairPanelController (Services + Ship).

## ScriptableObject Validation Rules

No new types. OnValidate() methods added to 8 existing SOs:

| SO Type | Field Count | Validation Type |
|---------|-------------|-----------------|
| OreDefinition | ~12 + per-output | Range, non-empty, non-null, cross-field |
| ShipArchetypeConfig | ~14 | Range, non-empty |
| AsteroidFieldDefinition | ~8 + per-entry | Range, cross-field, collection non-empty |
| StationServicesConfig | 3 | Range |
| DockingConfig | 5 | Range, cross-field |
| RawMaterialDefinition | 2 | Non-empty |
| StationServicesConfigMap | per-binding | Uniqueness, non-null |
| GameServicesConfig | 1 | Range |

## DI Registration Changes

### SceneLifetimeScope (modified)

New registrations (view-layer MonoBehaviours):

```
SceneLifetimeScope
├── [existing SO config registrations]
├── RegisterComponentInHierarchy<InputBridge>()          ← NEW
├── RegisterComponentInHierarchy<CameraView>()           ← NEW
├── RegisterComponentInHierarchy<TargetingController>()  ← NEW
└── RegisterComponentInHierarchy<TargetPreviewManager>() ← NEW
```

### Consumers (modified to use [Inject])

| Consumer | Dependency | Current Resolution | New Resolution |
|----------|-----------|-------------------|----------------|
| RadialMenuController | InputBridge | FindObjectOfType (Start) | [Inject] method |
| RadialMenuController | TargetingController | FindObjectOfType (Start) | [Inject] method |
| StationServicesMenuController | InputBridge | FindObjectOfType (Start) | [Inject] method |
| InputBridge | CameraView | FindFirstObjectByType (Start) | [Inject] method |
| TargetingController | TargetPreviewManager | FindObjectOfType (Start) | [Inject] method |

### Retained FindObjectOfType Calls

| File | Call | Reason |
|------|------|--------|
| TargetingController | FindObjectOfType<CinemachineCamera> | Third-party, not VContainer-managed |
| InputBridge | FindObjectsByType<TargetableStation> | Runtime query, multiple instances |
| TargetingController | FindObjectsByType<TargetableStation> | Runtime query, multiple instances |
| TargetPreviewManager | FindObjectsByType<TargetableStation> | Runtime query, multiple instances |
