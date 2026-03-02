# Research: Station Services Menu & Data-Driven Refining

**Branch**: `006-station-services-refining` | **Date**: 2026-03-01

## R-001: Credits State Placement

**Decision**: Add `int Credits` field to a new `StationServicesState` sealed record that replaces the empty `RefiningState` stub in `GameLoopState`. Credits are integer-typed — no fractional credits (see R-013).

**Rationale**: Credits are tightly coupled with station services (selling earns credits, refining/repair costs credits). Placing them in `StationServicesState` keeps the credit balance co-located with the operations that modify it, enabling atomic state transitions in a single reducer call.

**Alternatives considered**:
- **Separate `PlayerEconomyState`**: Over-engineers a simple int into its own state slice. No other economy state exists yet (Phase 3).
- **Field on `FleetState`**: Fleet tracks ship ownership and docking, not economy. Mixing concerns.
- **Field on `MarketState`**: MarketState is a Phase 3 stub for NPC/player market simulation. Credits are player-scoped, not market-scoped.

---

## R-002: Cross-Cutting Reducer Pattern

**Decision**: `CompositeReducer` in `RootLifetimeScope` handles three cross-cutting `IStationServicesAction` cases directly (transfer-to-station, transfer-to-ship, repair). All other station services actions route to `StationServicesReducer.Reduce(StationServicesState, action)` for single-slice updates.

**Rationale**: Three operations modify multiple state slices atomically:
1. **Transfer ship→station**: Removes from `InventoryState` (ship cargo) + adds to `StationStorageState`.
2. **Transfer station→ship**: Removes from `StationStorageState` + adds to `InventoryState`.
3. **Repair**: Deducts from credits (`StationServicesState`) + restores hull (`ShipState`).

Handling these in `CompositeReducer` ensures both slices update atomically in a single dispatch. The composite handler is still a pure function `(GameState, IStationServicesAction) → GameState`.

**Alternatives considered**:
- **Two-dispatch from view layer**: View dispatches `RemoveResourceAction` then `AddToStationStorageAction` separately. Risk: if first succeeds but second fails, state is inconsistent within the same frame. Rejected.
- **StationServicesReducer takes full `GameState`**: Gives the reducer access to read/write all state, breaking principle of least privilege. Rejected.
- **Result record with optional cross-slice updates**: Returns `StationServicesResult` with nullable updated slices. Over-complex for 3 cases. Rejected.

---

## R-003: Station Storage Design

**Decision**: Per-station `StationStorageState` using `ImmutableDictionary<string, ResourceStack>` (same `ResourceStack` type from `InventoryState`). No volume/slot limits. Keyed by station ID in `StationServicesState.StationStorages`.

**Rationale**: Reusing `ResourceStack` avoids duplicating the resource stack data structure. Unlimited capacity (per spec clarification) means no `MaxVolume`/`MaxSlots` fields needed, simplifying the state record.

**Alternatives considered**:
- **Reuse `InventoryState` directly**: `InventoryState` has `MaxSlots` and `MaxVolume` which would need to be set to `int.MaxValue`/`float.MaxValue` as workarounds. Inelegant and misleading. Rejected.
- **Station storage in `WorldState`**: `WorldState.Stations` holds static config (position, name, services). Mutable gameplay state (storage contents) belongs in `GameLoopState`. Rejected.

---

## R-004: Refining Job Timer Strategy

**Decision**: A lightweight `RefiningJobTicker` MonoBehaviour with `[Inject]` constructor injection. Checks active jobs against `Time.time` each frame. Dispatches `CompleteRefiningJobAction` when elapsed time exceeds duration.

**Rationale**: Refining jobs are purely state-store-based (not ECS entities). A MonoBehaviour is simpler than a managed `SystemBase` since no entity queries are needed. Frame-rate check is sufficient — timer accuracy is well within the 0.1s requirement (NFR-003) at 60 FPS (~16ms precision).

**Alternatives considered**:
- **Managed `SystemBase`**: Used by `DockingEventBridgeSystem`, but that system bridges ECS flags to managed events. Refining jobs don't involve ECS. Unnecessary complexity.
- **Coroutine with `WaitForSeconds`**: Less precise, harder to test, coroutine state is implicit. Rejected.
- **`WorldState.WorldTime`**: Not currently ticked by any system. Using `Time.time` directly is simpler and already available.

---

## R-005: Yield Variance Calculation

**Decision**: Pure static `RefiningMath.CalculateOutputs` function. Per-unit rolling with `Unity.Mathematics.Random`. Each input unit gets an independent random offset roll per output type. Results summed and floored at 0.

**Rationale**: Per-unit rolling (spec clarification) requires iterating over each input unit. `Unity.Mathematics.Random` is deterministic given a seed, enabling reproducible test results. The function runs once per job completion (not a hot loop), so performance is not a concern.

**Implementation detail**: `Random.NextInt(min, max)` is `[min, max)` exclusive upper bound. For variance range `[-1, +2]`, call `NextInt(-1, 3)` to include `+2`.

**Alternatives considered**:
- **`System.Random`**: Not deterministic across platforms. Unity.Mathematics.Random is preferred for reproducibility.
- **Pre-computed lookup table**: Over-engineering for an infrequent operation (~1 call per job completion).

---

## R-006: Assembly Organization

**Decision**: New `VoidHarvest.Features.StationServices` assembly. `RawMaterialDefinition` in `Features.Resources`. `RefiningOutputEntry` in `Features.Mining`. Add Mining→Resources dependency.

**Rationale**:
- `RawMaterialDefinition` is a resource type used by multiple features (StationServices, potentially future crafting). `Features.Resources` is the natural home.
- `RefiningOutputEntry` is a serialized field on `OreDefinition` (in Mining). It references `RawMaterialDefinition` (in Resources), requiring Mining→Resources dependency. This is a natural dependency: mining produces resources.
- StationServices depends on Mining (OreDefinition), Resources (RawMaterialDefinition, InventoryReducer helpers), and Docking (events).

**Alternatives considered**:
- **RawMaterialDefinition in Core**: Breaks feature isolation. Core should not know about game-specific material types.
- **Everything in Mining**: Mining is about beam targeting and asteroid extraction. Station-based economy is a separate concern.
- **Shared Data assembly**: Over-engineering for a single type.

---

## R-007: OreDefinition Extension

**Decision**: Add two new fields to `OreDefinition` ScriptableObject:
1. `RefiningOutputEntry[] RefiningOutputs` — Array of outputs (material ref, baseYield, varianceMin, varianceMax)
2. `int RefiningCreditCostPerUnit` — Credit cost per unit of ore refined (integer)

**Rationale**: Spec clarification requires all refining behavior to be embedded in the ore definition (no separate recipe asset). `RefiningOutputEntry` is a `[Serializable]` struct for Unity inspector editing. Existing `BaseProcessingTimePerUnit` field is already present (added in Spec 005, currently unused).

**Migration note**: Existing 3 ore assets (Luminite, Ferrox, Auralite) will need their `RefiningOutputs` arrays populated with starter content per FR-056. New fields default to empty/0 so existing assets remain valid until configured.

---

## R-008: RawMaterialDefinition ScriptableObject

**Decision**: New `RawMaterialDefinition` ScriptableObject in `Features.Resources.Data` with fields: `MaterialId` (string), `DisplayName` (string), `Icon` (Sprite), `Description` (string), `BaseValue` (int), `VolumePerUnit` (float).

**Rationale**: Mirrors `OreDefinition` pattern. `MaterialId` is the canonical key used in `ResourceStack` and `MaterialOutput`. `BaseValue` enables selling refined materials (same mechanism as selling ore). `VolumePerUnit` integrates with the existing inventory volume system.

**Create Asset menu**: `VoidHarvest/Raw Material Definition` (consistent with `VoidHarvest/Ore Definition`).

---

## R-009: StationServicesConfig ScriptableObject

**Decision**: New `StationServicesConfig` ScriptableObject with: `MaxConcurrentRefiningSlots` (int), `RefiningSpeedMultiplier` (float), `RepairCostPerHP` (int). Referenced from `StationPresetConfig` (existing SO) to associate per-station service capabilities.

**Rationale**: FR-041/FR-042 require per-station configurable service tiers. Linking via `StationPresetConfig` leverages the existing station preset system. Available services list already exists in `StationData.AvailableServices`.

**Global config**: A separate `GameServicesConfig` SO holds `StartingCredits` (int, default 0) per spec clarification. Registered in `SceneLifetimeScope`.

---

## R-010: UI Architecture

**Decision**: Expand the existing UI Toolkit-based station menu. Main `StationServicesMenuController` manages tab navigation and sub-panel lifecycle. Each service gets its own UXML/controller pair. Credit balance indicator is a persistent element in the menu header.

**Rationale**: The existing stub (UXML + USS + Controller) provides the shell. Expanding it preserves the established dark-panel/cyan-accent aesthetic (NFR-004). UI Toolkit supports dynamic element creation and data binding without GC pressure.

**Sub-panel controllers**: Each panel (CargoTransfer, SellResources, RefineOres, BasicRepair) is a sealed MonoBehaviour with `[Inject]` for `IStateStore` and `IEventBus`. They read state reactively via `IStateStore.OnStateChanged` and dispatch actions for user interactions.

**Refining job summary**: Modal overlay within the station menu. Opens on completed job click, shows generated materials, closes to trigger collection (FR-052/FR-053).

---

## R-011: Refining Job State Self-Containment

**Decision**: `RefiningJobState` captures `ImmutableArray<RefiningOutputConfig>` at creation time (snapshot of ore's refining outputs). On completion, `RefiningMath.CalculateOutputs` uses the captured config, not a live OreDefinition lookup.

**Rationale**: If a designer changes an OreDefinition while a job is running, the job should use the config from when it was started. This is consistent with FR-026 (ore consumed on start) — the "contract" is locked in at job creation.

**Alternatives considered**:
- **Live lookup on completion**: Requires ticker to access OreDefinition SOs, adding a managed dependency. Also allows mid-job balance changes. Rejected.

---

## R-012: Moving Stub from Docking to StationServices

**Decision**: Move `StationServicesMenuController.cs`, `StationServicesMenu.uxml`, and `StationServicesMenu.uss` from `Features/Docking/Views/` to `Features/StationServices/Views/`. Update scene references.

**Rationale**: The Docking assembly owns the docking state machine (approach/snap/dock/undock). The station services menu is a consumer of docking events, not part of docking itself. Moving it to StationServices gives clean assembly boundaries.

**Impact**: Scene references to the UXML and MonoBehaviour will need updating. The DockingFeedbackView stays in Docking (it handles dock/undock VFX, unrelated to station services).

---

## R-013: Credits Type — int, Not float

**Decision**: All credit-related values use `int` (C# 32-bit signed integer), not `float`. This includes: `StationServicesState.Credits`, all action fields carrying credit amounts (`TotalCost`, `PricePerUnit`, `Cost`, `NewBalance`), all event fields (`TotalCredits`, `OldBalance`, `NewBalance`), SO fields (`BaseValue` on OreDefinition and RawMaterialDefinition, `RefiningCreditCostPerUnit`, `RepairCostPerHP`, `StartingCredits`), and pure function outputs (`CalculateJobCost`, `CalculateRepairCost`).

**Rationale**: User preference — no fractional credits. Integer arithmetic eliminates floating-point precision issues (e.g., 0.1 + 0.2 ≠ 0.3), makes credit displays trivial (no formatting/rounding), and aligns with common game economy patterns (EVE Online uses ISK with implicit decimal but whole-number display). `int` (max 2,147,483,647) is more than sufficient for a single-player mining game where ore sells for 10–75 credits per unit.

**Migration impact**: `OreDefinition.BaseValue` changes from `float` to `int`. Existing ore assets have whole-number values (Luminite: 10, Ferrox: 25, Auralite: 75). Unity handles float→int serialization migration by truncation. The existing test `OreDefinition_HasBaseValueField` must update its literal from `42f` to `42`.

**RepairMath rounding**: `RepairMath.CalculateRepairCost` computes `(1.0f - currentIntegrity) * repairCostPerHP` which yields a float intermediate. Result is converted via `Mathf.CeilToInt()` — ceiling ensures the player always pays for any fractional HP of damage.

**Alternatives considered**:
- **`long` (64-bit)**: Max 9.2 × 10^18. Overkill for single-player. Unity inspector handles `long` less elegantly than `int`. Rejected.
- **`decimal`**: Not supported by Unity serialization. Rejected.
- **Keep `float`**: User explicitly requested integer credits. Float precision issues are a known headache in game economies. Rejected.
