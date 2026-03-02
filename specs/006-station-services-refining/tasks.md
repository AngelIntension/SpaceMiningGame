# Tasks: Station Services Menu & Data-Driven Refining

**Input**: Design documents from `/specs/006-station-services-refining/`
**Prerequisites**: plan.md, spec.md, data-model.md, research.md, quickstart.md

**Tests**: TDD is mandatory per constitution. Tests are included for all pure logic (Red-Green-Refactor).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Unity project root: `Assets/`
- Feature code: `Assets/Features/StationServices/{Data,Systems,Views,Tests}/`
- Core: `Assets/Core/{State,EventBus,Extensions}/`
- Mining data: `Assets/Features/Mining/Data/`
- Resources data: `Assets/Features/Resources/Data/`
- SO assets: `Assets/Features/StationServices/Data/Assets/`

---

## Phase 1: Setup

**Purpose**: Create directory structure and assembly definitions for the new StationServices feature module.

- [ ] T001 Create `Assets/Features/StationServices/` directory tree with `Data/`, `Data/Assets/`, `Data/Assets/RawMaterials/`, `Data/Assets/StationConfigs/`, `Systems/`, `Views/`, `Tests/` subdirectories
- [ ] T002 [P] Create assembly definition `Assets/Features/StationServices/VoidHarvest.Features.StationServices.asmdef` with references: Core.Extensions, Core.State, Core.EventBus, Features.Mining, Features.Resources, Features.Docking, Features.Ship, VContainer, UniTask, Unity.Mathematics, System.Collections.Immutable
- [ ] T003 [P] Create test assembly definition `Assets/Features/StationServices/Tests/VoidHarvest.Features.StationServices.Tests.asmdef` with references: VoidHarvest.Features.StationServices, Core.State, Core.Extensions, Features.Mining, Features.Resources, nunit.framework, UnityEngine.TestRunner, UnityEditor.TestRunner; Editor-only platform
- [ ] T004 Add `VoidHarvest.Features.Resources` reference to `Assets/Features/Mining/VoidHarvest.Features.Mining.asmdef` (Mining→Resources dependency for RefiningOutputEntry referencing RawMaterialDefinition)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core state records, reducer infrastructure, pure math functions, DI wiring, menu stub migration, and SO types. MUST complete before any user story.

**CRITICAL**: No user story work can begin until this phase is complete.

### Data Types (all different files, parallelizable)

- [ ] T005 [P] Create `Assets/Features/StationServices/Data/RefiningJobStatus.cs` — enum: Active=0, Completed=1. Namespace: VoidHarvest.Features.StationServices.Data
- [ ] T006 [P] Create `Assets/Features/StationServices/Data/RefiningOutputConfig.cs` — readonly struct: MaterialId (string), BaseYieldPerUnit (int), VarianceMin (int), VarianceMax (int)
- [ ] T007 [P] Create `Assets/Features/StationServices/Data/MaterialOutput.cs` — readonly struct: MaterialId (string), Quantity (int)
- [ ] T008 [P] Create `Assets/Features/StationServices/Data/RefiningJobState.cs` — sealed record: JobId (string), OreId (string), InputQuantity (int), StartTime (float), TotalDuration (float), CreditCostPaid (int), Status (RefiningJobStatus), OutputConfigs (ImmutableArray\<RefiningOutputConfig\>), GeneratedOutputs (ImmutableArray\<MaterialOutput\>). Add computed methods: Progress(float currentTime) returns float 0..1 clamped, RemainingTime(float currentTime) returns float >= 0. Static Empty with defaults.
- [ ] T009 [P] Create `Assets/Features/StationServices/Data/StationStorageState.cs` — sealed record: Stacks (ImmutableDictionary\<string, ResourceStack\>). Static Empty with empty dictionary. Reuses ResourceStack from VoidHarvest.Core.State.
- [ ] T010 [P] Create `Assets/Features/StationServices/Data/StationServicesState.cs` — sealed record: Credits (int), StationStorages (ImmutableDictionary\<int, StationStorageState\>), RefiningJobs (ImmutableDictionary\<int, ImmutableArray\<RefiningJobState\>\>). Static Empty with Credits=0 and empty dictionaries.
- [ ] T011 [P] Create `Assets/Features/StationServices/Data/StationServicesActions.cs` — IStationServicesAction : IGameAction marker interface + all 11 action sealed records per data-model.md: SellResourceAction, StartRefiningJobAction (includes MaxActiveSlots: int field for slot validation in reducer), CompleteRefiningJobAction, CollectRefiningJobAction, AddToStationStorageAction, RemoveFromStationStorageAction, InitializeStationStorageAction, SetCreditsAction, TransferToStationAction, TransferToShipAction, RepairShipAction. Also RepairHullAction : IShipAction.
- [ ] T012 [P] Create `Assets/Features/StationServices/Data/StationServicesEvents.cs` — all 6 readonly struct events: RefiningJobStartedEvent, RefiningJobCompletedEvent, ResourcesSoldEvent, CargoTransferredEvent, ShipRepairedEvent, CreditsChangedEvent per data-model.md field definitions.

### ScriptableObject Types

- [ ] T013 [P] Create `Assets/Features/StationServices/Data/StationServicesConfig.cs` — ScriptableObject: MaxConcurrentRefiningSlots (int, default 3), RefiningSpeedMultiplier (float, default 1.0f), RepairCostPerHP (int, default 100). CreateAssetMenu path "VoidHarvest/Station Services Config".
- [ ] T014 [P] Create `Assets/Features/StationServices/Data/GameServicesConfig.cs` — ScriptableObject: StartingCredits (int, default 0). CreateAssetMenu path "VoidHarvest/Game Services Config".
- [ ] T015 [P] Create `Assets/Features/Resources/Data/RawMaterialDefinition.cs` — ScriptableObject: MaterialId (string), DisplayName (string), Icon (Sprite), Description (string, TextArea), BaseValue (int), VolumePerUnit (float). CreateAssetMenu path "VoidHarvest/Raw Material Definition".

### OreDefinition Migration (R-013)

- [ ] T016 Change `BaseValue` field type from `float` to `int` in `Assets/Features/Mining/Data/OreDefinition.cs` and update test literal from `42f` to `42` in `Assets/Features/Mining/Tests/OreDefinitionTests.cs`. Verify existing ore assets (Luminite=10, Ferrox=25, Auralite=75) serialize correctly.

### Core State Integration

- [ ] T017 Modify `Assets/Core/State/GameState.cs` — replace `RefiningState Refining` with `StationServicesState StationServices` in GameLoopState sealed record. Add using for VoidHarvest.Features.StationServices.Data. Delete the empty RefiningState type (or leave if referenced elsewhere).
- [ ] T018 Modify `Assets/Core/State/GameStateReducer.cs` — update default/initial state construction to use StationServicesState.Empty instead of the old RefiningState().

### Tests for Foundational Reducers (Red Phase — MUST FAIL)

- [ ] T019 [P] Write `Assets/Features/StationServices/Tests/StationStorageReducerTests.cs` — NUnit tests: AddResource to empty storage creates stack, AddResource to existing stack increments quantity, RemoveResource partial decrements, RemoveResource all removes key, RemoveResource from empty returns unchanged state, RemoveResource exceeding quantity returns unchanged, multiple distinct resource types coexist. (FR-007, FR-008, FR-010)
- [ ] T020 [P] Write `Assets/Features/StationServices/Tests/StationServicesReducerTests.cs` — NUnit tests: SetCreditsAction replaces balance, InitializeStationStorageAction creates empty storage entry, AddToStationStorageAction adds resource via StationStorageReducer, RemoveFromStationStorageAction removes resource, operation on non-existent station returns unchanged state. (FR-009, FR-021)

### Reducer Implementation (Green Phase)

- [ ] T021 Create `Assets/Features/StationServices/Systems/StationStorageReducer.cs` — pure static class: AddResource(StationStorageState, string resourceId, int quantity, float volumePerUnit) returns new StationStorageState, RemoveResource(StationStorageState, string resourceId, int quantity) returns new StationStorageState. Uses ResourceStack with-expressions.
- [ ] T022 Create `Assets/Features/StationServices/Systems/StationServicesReducer.cs` — pure static Reduce(StationServicesState, IStationServicesAction) method. Initial handlers: SetCreditsAction, InitializeStationStorageAction, AddToStationStorageAction, RemoveFromStationStorageAction. Unknown actions return state unchanged.

### Pure Math Functions

- [ ] T023 Create `Assets/Features/StationServices/Systems/RepairMath.cs` — pure static class: CalculateRepairCost(float currentIntegrity, int repairCostPerHP) returns int via Mathf.CeilToInt((1.0f - currentIntegrity) * repairCostPerHP). At 100% → 0, at 0% → repairCostPerHP, at 99.5% with 100 → 1 (ceiling). Namespace: VoidHarvest.Features.StationServices.Systems.

### DI Wiring

- [ ] T024 Modify `Assets/Core/RootLifetimeScope.cs` — add IStationServicesAction case in CompositeReducer that delegates to StationServicesReducer.Reduce for single-slice actions. Update initial GameState construction to use StationServicesState with Credits=StartingCredits (from GameServicesConfig), initialize per-station StationStorageState for station IDs 1 and 2.
- [ ] T025 Modify `Assets/Core/SceneLifetimeScope.cs` — add [SerializeField] StationServicesConfig and GameServicesConfig fields, register both as VContainer instances.

### Menu Stub Migration (R-012)

- [ ] T026 Move `StationServicesMenuController.cs`, `StationServicesMenu.uxml`, `StationServicesMenu.uss` from `Assets/Features/Docking/Views/` to `Assets/Features/StationServices/Views/`. Update namespace to VoidHarvest.Features.StationServices.Views. Remove moved file references from Docking assembly. Update any scene GameObject references to the new paths. Verify StationServicesMenuController still receives DockingCompletedEvent and auto-opens.

### Station Config Integration

- [ ] T027 Add `StationServicesConfig ServicesConfig` field to `Assets/Features/Base/Data/StationPresetConfig.cs`. Create `SmallMiningRelayServices.asset` (MaxSlots=2, SpeedMultiplier=1.0, RepairCostPerHP=0) and `MediumRefineryHubServices.asset` (MaxSlots=4, SpeedMultiplier=1.5, RepairCostPerHP=100) in `Assets/Features/StationServices/Data/Assets/StationConfigs/`. Assign to existing SmallMiningRelay and MediumRefineryHub station preset assets.
- [ ] T028 Create `GameServicesConfig.asset` in `Assets/Features/StationServices/Data/Assets/` with StartingCredits=0. Assign in SceneLifetimeScope inspector.

**Checkpoint**: Compile clean, foundational tests pass (T019-T020 green), GameState uses StationServicesState, RepairMath available, menu stub in new location, station configs wired.

---

## Phase 3: User Story 1 — Cargo Transfer (Priority: P1) 🎯 MVP

**Goal**: Bidirectional transfer of ore and materials between ship cargo and station storage.

**Independent Test**: Dock at station, open Cargo Transfer, transfer 30 of 50 Luminite ship→station — verify ship=20, station=30. Transfer 5 back — verify ship=25, station=25. Attempt transfer to full ship — verify rejection.

### Tests (Red Phase)

- [ ] T029 [P] [US1] Write `Assets/Features/StationServices/Tests/CargoTransferTests.cs` — NUnit tests: TransferToStationAction removes quantity from InventoryState + adds to StationStorageState, TransferToShipAction removes from StationStorageState + adds to InventoryState, TransferToShip rejected when ship volume exceeded (state unchanged), transfer 0 quantity returns unchanged state, transfer more than source has returns unchanged state, both inventories update atomically in single dispatch, 10+ sequential transfers maintain inventory consistency (SC-003), transfer of raw material type (not just ore) succeeds in both directions (FR-036). (FR-011 through FR-015, FR-036)

### Implementation (Green Phase)

- [ ] T030 [US1] Add TransferToStationAction and TransferToShipAction cross-cutting handling in CompositeReducer in `Assets/Core/RootLifetimeScope.cs` — TransferToStation: call InventoryReducer.RemoveResource then StationServicesReducer.Reduce(AddToStationStorageAction). TransferToShip: validate ship capacity via InventoryReducer, then StationStorageReducer.RemoveResource + InventoryReducer.AddResource. Return unchanged state on validation failure.
- [ ] T031 [P] [US1] Create `Assets/Features/StationServices/Views/CargoTransferPanel.uxml` — UI Toolkit layout: side-by-side columns for ship cargo (left) and station storage (right), each showing item rows (name, quantity, volume), quantity input slider with min=1/max=available, directional transfer buttons (→ ship-to-station, ← station-to-ship), volume usage indicators.
- [ ] T032 [US1] Create `Assets/Features/StationServices/Views/CargoTransferPanelController.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore, IEventBus). Subscribe to IStateStore.OnStateChanged to reactively update both inventory displays. On transfer button click: read selected resource + quantity, dispatch TransferToStationAction or TransferToShipAction, publish CargoTransferredEvent on success. Show "Cargo Full" error label when ship capacity insufficient. Cap quantity selector at source available amount.
- [ ] T033 [US1] Wire CargoTransferPanelController into StationServicesMenuController tab navigation in `Assets/Features/StationServices/Views/StationServicesMenuController.cs` — "Cargo Transfer" button opens CargoTransferPanel, back button returns to main menu.
- [ ] T034 [US1] Register CargoTransferPanelController in VContainer scope (SceneLifetimeScope or via scene binding). Run compilation check + CargoTransferTests via Unity MCP — all tests must pass.

**Checkpoint**: US1 fully functional. Player can dock, transfer ore ship↔station, see quantities update. Ship capacity enforced.

---

## Phase 4: User Story 2 — Sell Resources (Priority: P2)

**Goal**: Sell ore and raw materials from station storage for integer credits.

**Independent Test**: Transfer 20 Luminite to station (via US1), open Sell Resources, select 10 Luminite, verify preview shows 100 credits (10×10), confirm sale — verify station=10 Luminite, credits=100.

### Tests (Red Phase)

- [ ] T035 [P] [US2] Write `Assets/Features/StationServices/Tests/SellResourcesTests.cs` — NUnit tests: SellResourceAction removes items from station storage + adds credits (int math: quantity × pricePerUnit), sell all removes key from storage, sell with empty storage returns unchanged, sell 0 quantity returns unchanged, credits are int (no fractional), CreditsChangedEvent fields correct, ResourcesSoldEvent fields correct. (FR-016 through FR-021)

### Implementation (Green Phase)

- [ ] T036 [US2] Add SellResourceAction handling in StationServicesReducer in `Assets/Features/StationServices/Systems/StationServicesReducer.cs` — remove Quantity of ResourceId from station's StationStorageState, add (Quantity × PricePerUnit) to Credits. All int arithmetic. Return unchanged if station storage lacks sufficient quantity.
- [ ] T037 [P] [US2] Create `Assets/Features/StationServices/Views/SellResourcesPanel.uxml` — resource list from station storage (item name, quantity, base value), quantity slider, live credit preview label showing "Total: {qty × baseValue} credits", current balance label, "Sell" button, confirmation overlay with "Confirm Sale" / "Cancel".
- [ ] T038 [US2] Create `Assets/Features/StationServices/Views/SellResourcesPanelController.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore, IEventBus). Reactive binding to StationServicesState for station storage + credits. Populate resource list from docked station's storage. Quantity slider with live preview (quantity × resource BaseValue). On "Sell" click: show confirmation dialog. On confirm: dispatch SellResourceAction, publish ResourcesSoldEvent + CreditsChangedEvent. Show "No items available for sale" when storage empty.
- [ ] T039 [US2] Create `Assets/Features/StationServices/Views/CreditBalanceIndicator.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore). Binds to a Label element in the menu header. Reads StationServicesState.Credits on state change. Formats as integer display "Credits: {amount}". Updates immediately on any credit change (FR-050, FR-051).
- [ ] T040 [US2] Wire SellResourcesPanelController + CreditBalanceIndicator into StationServicesMenuController in `Assets/Features/StationServices/Views/StationServicesMenuController.cs` — add credit indicator to menu header (always visible across all panels), "Sell Resources" tab opens SellResourcesPanel.
- [ ] T041 [US2] Register components in VContainer scope. Run compilation check + SellResourcesTests via Unity MCP — all tests must pass.

**Checkpoint**: US1+US2 functional. Full mine→dock→transfer→sell loop works. Credits visible in header, update in real-time.

---

## Phase 5: User Story 3 — Refine Ores (Priority: P3)

**Goal**: Time-based refining jobs converting ore from station storage into raw materials with per-unit yield variance.

**Independent Test**: Transfer 10 Luminite to station, open Refine Ores, select Luminite qty=10, verify cost preview, start job — verify ore removed + credits deducted + job in list. Wait for timer. Click completed job — verify summary shows materials. Close summary — verify materials in station storage.

### Data Types & OreDefinition Extension

- [ ] T042 [P] [US3] Create `Assets/Features/Mining/Data/RefiningOutputEntry.cs` — [Serializable] struct: Material (RawMaterialDefinition reference), BaseYieldPerUnit (int), VarianceMin (int), VarianceMax (int). Namespace: VoidHarvest.Features.Mining.Data.
- [ ] T043 [US3] Add `RefiningOutputs` (RefiningOutputEntry[]) and `RefiningCreditCostPerUnit` (int) fields to `Assets/Features/Mining/Data/OreDefinition.cs`. Both new fields with [SerializeField]. RefiningOutputs defaults to empty array, RefiningCreditCostPerUnit defaults to 0.

### Tests (Red Phase)

- [ ] T044 [P] [US3] Write `Assets/Features/StationServices/Tests/RefiningMathTests.cs` — NUnit tests: CalculateOutputs with known seed produces deterministic yields, per-unit rolling (10 units produces sum of 10 independent rolls), floor at 0 when base+variance is negative, 0 input quantity produces 0 outputs, multiple output configs calculated independently, CalculateJobDuration with speed multiplier (divides correctly, min 0.01), CalculateJobCost returns int (inputQty × costPerUnit). Use Unity.Mathematics.Random with fixed seeds. (FR-032, FR-033, FR-047)
- [ ] T045 [P] [US3] Write `Assets/Features/StationServices/Tests/RefiningJobLifecycleTests.cs` — NUnit tests: StartRefiningJobAction removes ore from storage + deducts credits + adds Active job with captured OutputConfigs, CompleteRefiningJobAction sets status=Completed + stores GeneratedOutputs + does NOT add to storage, CollectRefiningJobAction adds outputs to station storage + removes job from list, start with insufficient credits returns unchanged, start with insufficient ore returns unchanged, max concurrent active slots enforced via MaxActiveSlots field in action (completed don't count toward slot usage), completed job frees slot immediately. (FR-022 through FR-031, FR-048, FR-049, FR-054)

### Implementation (Green Phase)

- [ ] T046 [US3] Create `Assets/Features/StationServices/Systems/RefiningMath.cs` — pure static class: CalculateOutputs(ImmutableArray\<RefiningOutputConfig\> configs, int inputQuantity, ref Unity.Mathematics.Random random) returns ImmutableArray\<MaterialOutput\>. Per-unit rolling: for each unit, for each config, roll NextInt(varianceMin, varianceMax+1), add max(0, base+offset). Also: CalculateJobDuration(int qty, float timePerUnit, float speedMult) returns float. CalculateJobCost(int qty, int costPerUnit) returns int. Namespace: VoidHarvest.Features.StationServices.Systems.
- [ ] T047 [US3] Add StartRefiningJobAction, CompleteRefiningJobAction, CollectRefiningJobAction handling in StationServicesReducer in `Assets/Features/StationServices/Systems/StationServicesReducer.cs` — Start: validate ore quantity in storage + credits sufficient + active jobs at station < action.MaxActiveSlots, remove ore, deduct credits, add new RefiningJobState(Active). Complete: find job by ID, set Status=Completed, store GeneratedOutputs. Collect: find completed job, add each MaterialOutput to station storage, remove job from list.
- [ ] T048 [US3] Create `Assets/Features/StationServices/Views/RefiningJobTicker.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore, IEventBus). In Update(): iterate all stations' RefiningJobs, for each Active job where Time.time >= StartTime + TotalDuration: create Random with seed = (uint)job.JobId.GetHashCode() for deterministic per-job results, call RefiningMath.CalculateOutputs, dispatch CompleteRefiningJobAction with generated outputs, publish RefiningJobCompletedEvent.

### Starter Content Assets (FR-055, FR-056)

- [ ] T049 [P] [US3] Create 6 RawMaterialDefinition assets in `Assets/Features/StationServices/Data/Assets/RawMaterials/` via Unity MCP manage_asset: LuminiteIngots (luminite_ingots, BaseValue=20, VolumePerUnit=0.5), EnergiumDust (energium_dust, BaseValue=15, VolumePerUnit=0.3), FerroxSlabs (ferrox_slabs, BaseValue=50, VolumePerUnit=1.0), ConductiveResidue (conductive_residue, BaseValue=30, VolumePerUnit=0.4), AuraliteShards (auralite_shards, BaseValue=100, VolumePerUnit=0.8), QuantumEssence (quantum_essence, BaseValue=200, VolumePerUnit=0.2). All values are designer-tunable placeholders.
- [ ] T050 [US3] Configure refining outputs on existing ore definition assets via Unity MCP — Luminite: add RefiningOutputs [LuminiteIngots(4,-1,+2), EnergiumDust(2,0,+1)], set RefiningCreditCostPerUnit. Ferrox: [FerroxSlabs(3,-1,+1), ConductiveResidue(3,0,+2)]. Auralite: [AuraliteShards(2,0,+1), QuantumEssence(1,-1,+1)]. Per FR-056 and data-model.md.

### UI

- [ ] T051 [P] [US3] Create `Assets/Features/StationServices/Views/RefineOresPanel.uxml` — ore type selector dropdown (populated from station storage ores), quantity input slider, live preview panel (credit cost, expected outputs per ore definition), "Start Job" button (with disabled states), active jobs list section (progress bars, remaining time, ETA), completed jobs list section (visually distinct, clickable).
- [ ] T052 [US3] Create `Assets/Features/StationServices/Views/RefineOresPanelController.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore, IEventBus). Populate ore selector from station storage ores. On quantity change: compute live cost (qty × ore.RefiningCreditCostPerUnit) and expected outputs preview. When credits can only afford partial quantity, display max affordable quantity hint (floor(credits / costPerUnit)) per FR-057. Disable "Start Job" when: no slots, 0 qty, insufficient credits. On start: build OutputConfigs from OreDefinition.RefiningOutputs, read MaxConcurrentRefiningSlots from docked station's StationServicesConfig, dispatch StartRefiningJobAction (passing MaxActiveSlots), publish RefiningJobStartedEvent. Render job list with live progress (current time vs start+duration). On completed job click: open summary.
- [ ] T053 [P] [US3] Create `Assets/Features/StationServices/Views/RefiningJobSummary.uxml` — modal overlay: job details header (ore type, quantity), material output list (material name, quantity per row), "Close" button.
- [ ] T054 [US3] Create `Assets/Features/StationServices/Views/RefiningJobSummaryController.cs` — sealed MonoBehaviour. Open method takes RefiningJobState with Completed status. Populates material list from GeneratedOutputs. On close: dispatch CollectRefiningJobAction for the job's station+jobId, close overlay.
- [ ] T055 [US3] Wire RefineOresPanelController, RefiningJobSummaryController, RefiningJobTicker into StationServicesMenuController tab navigation + VContainer scope. Register RefiningJobTicker as scene-lifetime MonoBehaviour (active even when menu closed). Run compilation check + RefiningMathTests + RefiningJobLifecycleTests via Unity MCP — all tests must pass.

**Checkpoint**: US1+US2+US3 functional. Full mine→transfer→sell/refine→collect loop. Refining math deterministic with known seeds. Job timer accurate.

---

## Phase 6: User Story 4 — Basic Repair (Priority: P4)

**Goal**: One-click hull repair for integer credits using ceiling rounding.

**Independent Test**: Dock with hull at 60%, open Basic Repair, verify cost = ceil(0.4 × 100) = 40 credits. Confirm repair — verify hull=100%, credits deducted by 40. Open again — verify "Hull at maximum" disabled state.

### Tests (Red Phase)

- [ ] T056 [P] [US4] Write `Assets/Features/StationServices/Tests/RepairTests.cs` — NUnit tests: RepairMath.CalculateRepairCost (from T023) at 60% integrity with costPerHP=100 returns 40, at 0% returns 100, at 100% returns 0, at 99.5% returns ceil(0.005×100)=1 (ceiling test). RepairShipAction in CompositeReducer deducts Cost from credits + dispatches RepairHullAction. RepairHullAction sets ShipState.HullIntegrity to 1.0f. Repair with insufficient credits returns unchanged state. Repair at 100% integrity returns unchanged state. (FR-037 through FR-040)

### Implementation (Green Phase)

- [ ] T057 [US4] Add RepairShipAction cross-cutting handling in CompositeReducer in `Assets/Core/RootLifetimeScope.cs` — validate Credits >= Cost, deduct Cost from StationServicesState.Credits via SetCreditsAction, apply RepairHullAction to ShipState. Return unchanged if insufficient credits.
- [ ] T058 [US4] Add RepairHullAction handling in `Assets/Features/Ship/Systems/ShipStateReducer.cs` — set HullIntegrity to NewIntegrity value using `with` expression.
- [ ] T059 [P] [US4] Create `Assets/Features/StationServices/Views/BasicRepairPanel.uxml` — hull integrity bar (current %), repair cost label "Cost: {amount} credits", current balance label, "Repair to 100%" button. Disabled states: "Hull integrity is at maximum" when 100%, "Insufficient credits" in red when can't afford.
- [ ] T060 [US4] Create `Assets/Features/StationServices/Views/BasicRepairPanelController.cs` — sealed MonoBehaviour with [Inject] Construct(IStateStore, IEventBus). Reads ShipState.HullIntegrity + StationServicesState.Credits on state change. Computes cost via RepairMath.CalculateRepairCost. Disables button when integrity=1.0 or credits < cost. On confirm: dispatch RepairShipAction(cost, 1.0f), publish ShipRepairedEvent + CreditsChangedEvent.
- [ ] T061 [US4] Wire BasicRepairPanelController into StationServicesMenuController tab navigation in `Assets/Features/StationServices/Views/StationServicesMenuController.cs` — "Basic Repair" tab opens BasicRepairPanel. Only enable at stations where ServicesConfig.RepairCostPerHP > 0 (Medium Refinery Hub has repair, Small Mining Relay does not). Run compilation check + RepairTests via Unity MCP — all tests must pass.

**Checkpoint**: US1-US4 functional. Full economy loop: mine→transfer→sell/refine→repair. All credit operations use int arithmetic.

---

## Phase 7: User Story 5 — Menu Shell & Navigation Polish (Priority: P5)

**Goal**: Polished menu shell with all service tabs, enable/disable per station, credit indicator, and proper navigation.

**Independent Test**: Dock at Small Mining Relay — verify Cargo Transfer + Sell + Refine enabled, Repair disabled. Dock at Medium Refinery Hub — all enabled. Navigate to any sub-panel → back returns to main menu. Undock from any panel works.

### Tests (Red Phase)

- [ ] T062 [US5] Write `Assets/Features/StationServices/Tests/MenuNavigationTests.cs` — PlayMode integration tests: service enable/disable based on StationServicesConfig (FR-005), menu auto-opens on DockingCompletedEvent (FR-001), menu auto-closes on undocking (FR-006), back button from any sub-panel returns to main menu without undocking (FR-003), credit indicator visible across all panels (FR-050).

### Implementation (Green Phase)

- [ ] T063 [US5] Expand `Assets/Features/StationServices/Views/StationServicesMenu.uxml` — replace all "Coming Soon" placeholder content with real panel container slots for CargoTransfer, SellResources, RefineOres, BasicRepair. Add credit balance indicator element in header bar (persistent across all views). Add service button enabled/disabled visual states.
- [ ] T064 [US5] Expand `Assets/Features/StationServices/Views/StationServicesMenu.uss` — styles for all sub-panels following existing dark-panel/cyan-accent aesthetic (NFR-004). Tab active/inactive states, disabled service button styling (grayed out with tooltip), credit indicator formatting (monospace, right-aligned), sub-panel transitions.
- [ ] T065 [US5] Implement service enable/disable logic in `Assets/Features/StationServices/Views/StationServicesMenuController.cs` — on dock, read docked station's StationData.AvailableServices and map to panel visibility: `"Refinery"` → Refine Ores, `"Market"` → Sell Resources, `"Repair"` → Basic Repair, `"Cargo"` → Cargo Transfer. Disable buttons for services not in the station's AvailableServices list. Also read StationPresetConfig.ServicesConfig for capability parameters (slot count, speed multiplier, repair cost). (FR-005, FR-041)
- [ ] T066 [US5] Verify and fix auto-open on DockingCompletedEvent and auto-close on undocking in `Assets/Features/StationServices/Views/StationServicesMenuController.cs` — ensure behavior preserved after stub migration, menu shows all 5 buttons + credit balance on open (FR-001, FR-006).
- [ ] T067 [US5] Implement consistent sub-panel back/close navigation — each panel's back button hides the panel and shows the main menu without undocking (FR-003). Undock button from any view closes entire menu + dispatches BeginUndockingAction (FR-004). Active refining jobs continue after undock (FR-027). Run compilation check + MenuNavigationTests via Unity MCP — all tests must pass.

**Checkpoint**: All 5 service panels accessible with proper tab navigation, service enable/disable per station config, credit indicator always visible, undock works from any view.

---

## Phase 8: User Story 6 — Refining Job Notifications (Priority: P6)

**Goal**: HUD notifications when refining jobs complete, whether docked or undocked.

**Independent Test**: Start refining job, undock, wait for completion — verify HUD indicator shows "1 completed job". Redock, review job — indicator clears.

### Tests (Red Phase)

- [ ] T068 [US6] Write `Assets/Features/StationServices/Tests/RefiningNotificationTests.cs` — NUnit tests: notification count increments on RefiningJobCompletedEvent, count decrements on CollectRefiningJobAction event, count resets to 0 when all jobs collected, indicator visibility: hidden when count=0, visible when count>0. (FR-044)

### Implementation (Green Phase)

- [ ] T069 [US6] Create HUD notification for refining job completion while docked — subscribe to RefiningJobCompletedEvent in a new HUD element or extend existing HUD, display temporary notification popup "Refining complete!" with station name, auto-dismiss after 5 seconds or on click (FR-044).
- [ ] T070 [US6] Create persistent HUD notification indicator for pending completed jobs while undocked — small icon/badge showing count of unreviewed completed jobs across all stations. Subscribe to RefiningJobCompletedEvent to increment, CollectRefiningJobAction to decrement. Visible only when count > 0.
- [ ] T071 [US6] Verify notification lifecycle end-to-end — start job, undock, job completes (indicator appears), redock at same station, review job via summary (indicator decrements), all jobs reviewed (indicator hidden). Run compilation check + RefiningNotificationTests via Unity MCP — all tests must pass.

**Checkpoint**: Full notification loop working. Player always informed of refining completions.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: VFX/audio feedback, documentation, regression verification.

- [ ] T072 [P] Add VFX and audio cues for all station service actions in `Assets/Features/StationServices/Views/` — create audio/visual feedback for: cargo transfer (whoosh/slide), sell confirmation (cash register/coin), refine start (furnace ignite), refine complete (ding/sparkle), repair (wrench/weld). Follow existing DockingVFXConfig/DockingAudioConfig pattern. Placeholder clips acceptable. (FR-043)
- [ ] T073 Update `HOWTOPLAY.md` at project root — add "Station Services" section covering: how to access (dock at station), Cargo Transfer (ship↔station), Sell Resources (earn credits), Refine Ores (time-based jobs, yield variance), Basic Repair (spend credits). Include credit system explanation. (FR-045)
- [ ] T074 Add changelog entry documenting all Spec 006 features in project changelog or HOWTOPLAY.md. (FR-046)
- [ ] T075 Run full EditMode + PlayMode test suite via Unity MCP `run_tests` to verify zero regressions across all assemblies — ship controls, mining, camera, docking, asteroid generation, HUD, and VFX must pass. Profile station menu panel transitions under Unity Profiler to verify <2ms frame spikes (NFR-002). (NFR-006)
- [ ] T076 Execute quickstart.md validation — manually run through all 4 test scenarios: mine-to-sell loop (SC-001), refining loop (SC-002), repair loop (SC-006), data-driven extensibility (SC-004).
- [ ] T077 Code review and cleanup — verify all new types follow naming conventions (State/Reducer/Config/Action suffixes), XML doc comments on public API methods, no unused imports, no dead code, all [Inject] patterns consistent with existing codebase.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **BLOCKS all user stories**
- **US1 Cargo Transfer (Phase 3)**: Depends on Phase 2
- **US2 Sell Resources (Phase 4)**: Depends on Phase 2 (uses US1 for practical testing but no code dependency)
- **US3 Refine Ores (Phase 5)**: Depends on Phase 2
- **US4 Basic Repair (Phase 6)**: Depends on Phase 2 (RepairMath created in Phase 2 T023 — no US3 dependency)
- **US5 Menu Shell (Phase 7)**: Depends on Phases 3–6 (all sub-panel controllers must exist)
- **US6 Notifications (Phase 8)**: Depends on Phase 5 (refining job events)
- **Polish (Phase 9)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: Independent after Phase 2 — no dependency on other stories
- **US2 (P2)**: Independent after Phase 2 — uses US1 for practical testing (transfer ore first) but no code dependency
- **US3 (P3)**: Independent after Phase 2 — creates RefiningMath.cs
- **US4 (P4)**: Independent after Phase 2 — RepairMath.cs is foundational (T023), no US3 dependency
- **US5 (P5)**: Requires all sub-panel controllers from US1–US4
- **US6 (P6)**: Requires refining job events from US3

### Within Each User Story (TDD Flow)

1. Tests written FIRST → must FAIL (Red)
2. Data types / SO extensions created (if needed)
3. Reducer / math logic implemented → tests pass (Green)
4. UI created (UXML layout then controller logic)
5. Wired into menu + VContainer
6. Compilation check + test verification via Unity MCP

### Parallel Opportunities

**Phase 2 — Data types (T005–T012)**: All 8 files in parallel (different files, no interdependencies beyond type references resolved at compile)
**Phase 2 — SO types (T013–T015)**: All 3 files in parallel
**Phase 2 — Tests (T019–T020)**: Both test files in parallel
**US1–US4 can run in parallel** after Phase 2 (different panels, different reducer sections, different test files — RepairMath is now foundational)
**Within US3**: T044+T045 (test files in parallel), T042 (RefiningOutputEntry) parallel with T051+T053 (UXML layouts)

---

## Parallel Example: Phase 2 Data Types

```text
# Launch all data type creations together (8 files, no deps):
T005: RefiningJobStatus.cs
T006: RefiningOutputConfig.cs
T007: MaterialOutput.cs
T008: RefiningJobState.cs
T009: StationStorageState.cs
T010: StationServicesState.cs
T011: StationServicesActions.cs
T012: StationServicesEvents.cs
```

## Parallel Example: User Stories After Phase 2

```text
# After Phase 2 checkpoint, these can start in parallel:
Phase 3 (US1): Cargo Transfer — different files, CompositeReducer section
Phase 4 (US2): Sell Resources — different files, StationServicesReducer section
Phase 5 (US3): Refine Ores — different files, new math + ticker + panels
Phase 6 (US4): Basic Repair — different files, RepairMath already exists (T023)
```

---

## Implementation Strategy

### MVP First (Phase 1 + 2 + US1 + US2)

1. Complete Phase 1: Setup (4 tasks)
2. Complete Phase 2: Foundational (24 tasks)
3. Complete Phase 3: US1 Cargo Transfer (6 tasks)
4. Complete Phase 4: US2 Sell Resources (7 tasks)
5. **STOP and VALIDATE**: Mine→dock→transfer→sell loop, credits visible
6. This is the minimum viable economy loop

### Incremental Delivery

1. Phase 1+2 → Foundation ready (compile clean, core tests pass)
2. +US1 → Cargo transfer working (MVP slice 1)
3. +US2 → Selling + credits working (MVP slice 2 — economy loop closed!)
4. +US3 → Refining working (economic depth + starter content)
5. +US4 → Repair working (credit sink)
6. +US5 → Menu polish (UX quality)
7. +US6 → Notifications (polish)
8. +Polish → Documentation, VFX, regression check

---

## Notes

- TDD is mandatory per constitution — tests MUST be written and FAIL before implementation
- [P] tasks = different files, no dependencies
- [Story] labels map tasks to user stories for traceability
- All credits use `int` type — no fractional credits (R-013)
- All prices (BaseValue, RefiningCreditCostPerUnit, RepairCostPerHP) are `int`
- Repair cost uses `Mathf.CeilToInt()` for ceiling rounding from float intermediate
- RepairMath is foundational (T023) — not coupled to RefiningMath or US3
- StartRefiningJobAction carries MaxActiveSlots from StationServicesConfig for reducer validation
- RefiningJobTicker uses `(uint)jobId.GetHashCode()` as deterministic seed for yield calculation
- Compile-check via Unity MCP `read_console` after every script creation/modification
- Console must be clean before proceeding (constitution gate)
- Run relevant tests via Unity MCP `run_tests` at each checkpoint
- Commit after each task or logical group using conventional commits
