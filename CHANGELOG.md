# Changelog

All notable changes to VoidHarvest are documented in this file.

## [Unreleased]

### Spec 006 — Station Services Menu & Data-Driven Refining

**Feature**: Full station economy loop — cargo transfer, resource selling,
time-based ore refining with yield variance, hull repair, and credits system.

#### Added
- `StationServicesState` sealed record in Core/State — credits (int), per-station
  storage (ImmutableDictionary), per-station refining jobs.
- `StationServicesReducer` pure static reducer handling 11 action types:
  cargo transfer, sell resources, start/complete/collect refining jobs,
  station storage CRUD, credit management, repair.
- `RefiningMath` pure static class — deterministic per-unit yield rolling
  with `Unity.Mathematics.Random`, job duration and cost calculations.
- `RepairMath` pure static class — ceiling-rounded repair cost from hull
  integrity percentage.
- `RefiningJobTicker` MonoBehaviour — tracks active refining job timers,
  dispatches `CompleteRefiningJobAction` with deterministic outputs on
  completion.
- `StationServicesMenuController` — UI Toolkit panel navigation with 4
  service tabs (Cargo Transfer, Sell Resources, Refine Ores, Basic Repair),
  per-station service enable/disable, credit balance header indicator.
- `CargoTransferPanelController` — bidirectional ship/station resource
  transfer with capacity enforcement.
- `SellResourcesPanelController` — sell resources from station storage for
  integer credits with live preview and confirmation.
- `RefineOresPanelController` — start time-based refining jobs, live cost
  preview, max affordable quantity hint, active/completed job lists.
- `RefiningJobSummaryController` — modal showing generated materials on job
  completion, dispatches collect action on close.
- `BasicRepairPanelController` — one-click hull repair for credits with
  ceiling-rounded cost display.
- `CreditBalanceIndicator` — persistent credit display across all menu
  panels, updates reactively on state change.
- `RefiningNotificationIndicator` + `RefiningNotificationTracker` — HUD
  badge showing count of pending completed refining jobs, visible when
  undocked.
- `StationServicesConfig` ScriptableObject — per-station max refining slots,
  speed multiplier, repair cost per HP.
- `GameServicesConfig` ScriptableObject — starting credits.
- `StationServicesConfigMap` ScriptableObject — maps station IDs to their
  service configs.
- `RawMaterialDefinition` ScriptableObject — material ID, display name,
  description, base value, volume per unit.
- 6 raw material assets: Luminite Ingots, Energium Dust, Ferrox Slabs,
  Conductive Residue, Auralite Shards, Quantum Essence.
- 2 station service config assets: SmallMiningRelayServices (2 slots, 1.0x
  speed, no repair), MediumRefineryHubServices (4 slots, 1.5x speed, repair).
- `RefiningOutputEntry` serializable struct on `OreDefinition` — per-ore
  refining output configuration with base yield and variance range.
- `RefiningCreditCostPerUnit` field on `OreDefinition` — cost per unit to
  refine each ore type.
- 6 station services events: RefiningJobStarted, RefiningJobCompleted,
  RefiningJobCollected, ResourcesSold, CargoTransferred, ShipRepaired,
  CreditsChanged — all zero-allocation readonly structs.
- `RepairHullAction` in Core/State — cross-assembly action for ship hull
  repair (avoids circular Ship→StationServices dependency).
- UXML/USS layouts for all 4 service panels plus refining job summary modal.
- Station Services section in HOWTOPLAY.md.

#### Changed
- `GameState.GameLoopState` — replaced empty `RefiningState` with
  `StationServicesState` containing credits, station storages, refining jobs.
- `CompositeReducer` in `RootLifetimeScope` — handles cross-cutting actions
  (TransferToStation, TransferToShip, RepairShip) that span multiple state
  slices atomically.
- `ShipStateReducer` — added `RepairHullAction` handler to set hull
  integrity.
- `OreDefinition` ScriptableObject — added `RefiningOutputs` array and
  `RefiningCreditCostPerUnit` int field. `BaseValue` changed from float to
  int.
- `StationServicesMenuController` — migrated from Docking/Views to
  StationServices/Views, rewritten with full panel management.
- Ore definition assets (Luminite, Ferrox, Auralite) — configured refining
  outputs and credit costs.

#### Test Impact
- 420 tests pass (was 360; 60 new tests for station services reducers,
  refining math, refining job lifecycle, repair math, cargo transfer,
  sell resources, and refining notifications).

### Spec 005 — Data-Driven Ore System & Asteroid Spawning Refactor

**Migration**: Replaced hard-coded ore types (Veldspar, Scordite, Pyroxeres)
with a fully data-driven ScriptableObject architecture (Luminite, Ferrox,
Auralite).

#### Added
- `OreDefinition` ScriptableObject — configurable ore type with display name,
  rarity tier, base yield, hardness, beam color, cargo volume, ore ID, and
  visual tint. Designers add new ores via Create > VoidHarvest > Ore Definition
  with zero code changes.
- `OreRarityTier` enum (Common, Uncommon, Rare, Epic, Legendary) for future
  UI/loot integration.
- `AsteroidFieldDefinition` ScriptableObject — configurable asteroid field
  with per-field ore weights, visual tint mapping, asteroid count, radius,
  size range, rotation speed, seed, and min scale fraction. Multiple fields
  with distinct compositions supported simultaneously.
- `AsteroidFieldSpawner` authoring component — bakes field definition into
  ECS components for Burst-compiled asteroid generation.
- Three ore assets: Luminite (Common, ice-blue, fast yield, low hardness),
  Ferrox (Uncommon, bronze-orange, medium yield, medium hardness), Auralite
  (Rare, violet, slow yield, high hardness).
- DefaultField.asset — 300-asteroid field with weighted ore distribution
  (60% Luminite, 30% Ferrox, 10% Auralite).
- Addressable asset group `OreDefinitions` for runtime ore loading.

#### Changed
- `OreTypeBlobBakingSystem` now bakes `OreDefinition` ScriptableObjects into
  `OreTypeBlob` BlobAssets (previously baked from `OreTypeDefinition`).
- `AsteroidFieldSystem` reads field config from `AsteroidFieldConfigComponent`
  entities and mesh data from `AsteroidPrefabComponent` singleton (previously
  used hard-coded `AsteroidFieldConfig.MvpDefault`).
- `MiningBeamView` resolves beam colors from `OreDefinition[]` (previously
  from `OreTypeDefinition[]`).
- `OreTypeDatabaseInitializer` references `OreDefinition[]` (previously
  `OreTypeDefinition[]`).
- Player documentation (HOWTOPLAY.md) updated with new ore names, rarity
  tiers, and gameplay characteristics.

#### Removed
- `OreTypeDefinition` ScriptableObject class and all instances.
- `AsteroidFieldConfig` record with hard-coded `MvpDefault` field.
- `AsteroidVisualMappingConfig` ScriptableObject class and instance.
- Legacy ore assets: Veldspar.asset, Scordite.asset, Pyroxeres.asset.
- 4 tests dependent on deleted `AsteroidVisualMappingConfig` type.

#### Test Impact
- 360 tests pass (was 364; 4 removed with deleted types, new tests added for
  OreDefinition validation, blob baking, weight normalization, and field
  definition).
