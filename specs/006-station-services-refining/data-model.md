# Data Model: Station Services Menu & Data-Driven Refining

**Branch**: `006-station-services-refining` | **Date**: 2026-03-01

## State Records (Immutable Domain Data)

### StationServicesState (replaces RefiningState stub)

```
StationServicesState (sealed record)
├── Credits: int                                               # Player's credit balance (integer, no fractional credits)
├── StationStorages: ImmutableDictionary<int, StationStorageState>  # Per-station storage (key: station ID)
└── RefiningJobs: ImmutableDictionary<int, ImmutableArray<RefiningJobState>>  # Per-station job lists
    Static: Empty (Credits=0, empty dictionaries)
```

**Location**: `Assets/Features/StationServices/Data/StationServicesState.cs`
**Namespace**: `VoidHarvest.Features.StationServices.Data`

**Placement in hierarchy**:
```
GameState
└── Loop: GameLoopState
    └── StationServices: StationServicesState  (was: Refining: RefiningState)
```

---

### StationStorageState

```
StationStorageState (sealed record)
└── Stacks: ImmutableDictionary<string, ResourceStack>  # Resource ID → stack (unlimited capacity)
    Static: Empty (empty dictionary)
```

**Notes**: Reuses `ResourceStack` from `VoidHarvest.Core.State`. No `MaxSlots`/`MaxVolume` — capacity is unlimited per spec.

---

### RefiningJobState

```
RefiningJobState (sealed record)
├── JobId: string                                           # Unique job identifier (GUID or sequential)
├── OreId: string                                           # Input ore type ID
├── InputQuantity: int                                      # Units of ore consumed
├── StartTime: float                                        # Time.time when job started
├── TotalDuration: float                                    # Total processing time in seconds
├── CreditCostPaid: int                                     # Credits deducted at start (integer)
├── Status: RefiningJobStatus                               # Active or Completed
├── OutputConfigs: ImmutableArray<RefiningOutputConfig>      # Snapshot of ore's output params at start
└── GeneratedOutputs: ImmutableArray<MaterialOutput>         # Empty until completion, then final yields

Computed:
├── Progress(currentTime): float  # 0..1, saturated. 1.0 if Completed.
└── RemainingTime(currentTime): float  # seconds remaining, 0 if Completed.
```

**Lifecycle**: `Active` → `Completed` → Collected (removed from list via `CollectRefiningJobAction`)

---

### RefiningJobStatus (enum)

```
RefiningJobStatus
├── Active = 0      # Job is processing, occupies a slot
└── Completed = 1   # Job finished, slot freed, awaiting player review
```

---

### RefiningOutputConfig (readonly struct)

```
RefiningOutputConfig
├── MaterialId: string   # Raw material type ID
├── BaseYieldPerUnit: int  # Base output per input unit
├── VarianceMin: int       # Additive offset minimum (can be negative)
└── VarianceMax: int       # Additive offset maximum
```

**Notes**: Captured from `RefiningOutputEntry` at job creation. Decoupled from the live ScriptableObject so mid-flight SO changes don't affect running jobs.

---

### MaterialOutput (readonly struct)

```
MaterialOutput
├── MaterialId: string  # Raw material type ID
└── Quantity: int        # Final calculated quantity (floored at 0)
```

---

## ScriptableObject Types (Designer-Authored Data)

### OreDefinition (MODIFIED — existing type)

```
OreDefinition (ScriptableObject) — Assets/Features/Mining/Data/
├── OreId: string                         # (existing)
├── DisplayName: string                   # (existing)
├── RarityTier: OreRarityTier             # (existing)
├── Icon: Sprite                          # (existing)
├── BaseValue: int                        # (existing, type changed float→int) — sell price per unit
├── Description: string                   # (existing)
├── RarityWeight: float                   # (existing)
├── BaseYieldPerSecond: float             # (existing)
├── Hardness: float                       # (existing)
├── VolumePerUnit: float                  # (existing)
├── BeamColor: Color                      # (existing)
├── BaseProcessingTimePerUnit: float      # (existing, was unused — NOW ACTIVE)
├── RefiningOutputs: RefiningOutputEntry[] # NEW — list of produced materials
└── RefiningCreditCostPerUnit: int        # NEW — credit cost per unit refined (integer)
```

**Type change: `BaseValue` float→int**: All existing ore assets use whole-number values (Luminite: 10, Ferrox: 25, Auralite: 75). Unity handles float→int serialization migration automatically. The existing test (`OreDefinition_HasBaseValueField`) must be updated to use `int` literal.

**New field details**:
- `RefiningOutputs`: Inspector-editable array. Each entry references a `RawMaterialDefinition` SO.
- `RefiningCreditCostPerUnit`: Designer-tunable integer. Total job cost = this × input quantity.
- `BaseProcessingTimePerUnit`: Already exists (Spec 005). Total job duration = this × quantity ÷ station speed multiplier.

---

### RefiningOutputEntry (serializable struct)

```
RefiningOutputEntry [Serializable]  — Assets/Features/Mining/Data/
├── Material: RawMaterialDefinition  # Reference to raw material SO
├── BaseYieldPerUnit: int            # Base output per input unit
├── VarianceMin: int                 # Additive offset min
└── VarianceMax: int                 # Additive offset max
```

**Validation**: `VarianceMin <= VarianceMax`. `BaseYieldPerUnit >= 0`. Combined `BaseYieldPerUnit + VarianceMin` can be < 0 (floored at 0 per output).

---

### RawMaterialDefinition (NEW ScriptableObject)

```
RawMaterialDefinition (ScriptableObject) — Assets/Features/Resources/Data/
├── MaterialId: string         # Unique identifier (e.g., "luminite_ingots")
├── DisplayName: string        # Human-readable name (e.g., "Luminite Ingots")
├── Icon: Sprite               # Inventory/UI icon
├── Description: string        # Flavor text [TextArea]
├── BaseValue: int             # Sell price per unit (integer credits)
└── VolumePerUnit: float       # Cargo volume per unit
```

**Create menu**: `VoidHarvest/Raw Material Definition`

---

### StationServicesConfig (NEW ScriptableObject)

```
StationServicesConfig (ScriptableObject) — Assets/Features/StationServices/Data/
├── MaxConcurrentRefiningSlots: int   # Max active refining jobs (default: 3)
├── RefiningSpeedMultiplier: float    # Duration divisor (default: 1.0, higher = faster)
└── RepairCostPerHP: int              # Credits per HP of hull damage (default: 100, integer)
```

**Create menu**: `VoidHarvest/Station Services Config`

**Association**: Referenced from `StationPresetConfig` (add new field). Each station preset links to its service capabilities.

---

### GameServicesConfig (NEW ScriptableObject)

```
GameServicesConfig (ScriptableObject) — Assets/Features/StationServices/Data/
└── StartingCredits: int  # Credits new players start with (default: 0, integer)
```

**Create menu**: `VoidHarvest/Game Services Config`

**Registration**: Registered as instance in `SceneLifetimeScope`. Read by `RootLifetimeScope` during initial state creation.

---

## Starter Content Assets (FR-055, FR-056)

### Raw Material Definitions

| Asset | MaterialId | DisplayName | BaseValue | VolumePerUnit |
|-------|-----------|-------------|-----------|---------------|
| LuminiteIngots.asset | luminite_ingots | Luminite Ingots | TBD | TBD |
| EnergiumDust.asset | energium_dust | Energium Dust | TBD | TBD |
| FerroxSlabs.asset | ferrox_slabs | Ferrox Slabs | TBD | TBD |
| ConductiveResidue.asset | conductive_residue | Conductive Residue | TBD | TBD |
| AuraliteShards.asset | auralite_shards | Auralite Shards | TBD | TBD |
| QuantumEssence.asset | quantum_essence | Quantum Essence | TBD | TBD |

BaseValue (int) and VolumePerUnit to be set during implementation based on game balance (designer tunable).

### Ore Refining Output Configurations

**Luminite** (OreId: luminite):
| Output | BaseYieldPerUnit | VarianceMin | VarianceMax |
|--------|-----------------|-------------|-------------|
| Luminite Ingots | 4 | -1 | +2 |
| Energium Dust | 2 | 0 | +1 |

**Ferrox** (OreId: ferrox):
| Output | BaseYieldPerUnit | VarianceMin | VarianceMax |
|--------|-----------------|-------------|-------------|
| Ferrox Slabs | 3 | -1 | +1 |
| Conductive Residue | 3 | 0 | +2 |

**Auralite** (OreId: auralite):
| Output | BaseYieldPerUnit | VarianceMin | VarianceMax |
|--------|-----------------|-------------|-------------|
| Auralite Shards | 2 | 0 | +1 |
| Quantum Essence | 1 | -1 | +1 |

### Station Services Configs

| Asset | Station | MaxSlots | SpeedMultiplier | RepairCostPerHP |
|-------|---------|----------|-----------------|-----------------|
| SmallMiningRelayServices.asset | Small Mining Relay | 2 | 1.0 | N/A (no repair) |
| MediumRefineryHubServices.asset | Medium Refinery Hub | 4 | 1.5 | 100 |

---

## Actions (IStationServicesAction)

All actions implement `IStationServicesAction : IGameAction`.

### Single-Slice Actions (handled by StationServicesReducer)

```
SellResourceAction
├── StationId: int
├── ResourceId: string
├── Quantity: int
└── PricePerUnit: int          # Sell price per unit (integer credits)

StartRefiningJobAction
├── StationId: int
├── OreId: string
├── InputQuantity: int
├── TotalCost: int             # Total credit cost (integer)
├── TotalDuration: float
├── OutputConfigs: ImmutableArray<RefiningOutputConfig>
└── StartTime: float

CompleteRefiningJobAction
├── StationId: int
├── JobId: string
└── GeneratedOutputs: ImmutableArray<MaterialOutput>

CollectRefiningJobAction
├── StationId: int
└── JobId: string

AddToStationStorageAction
├── StationId: int
├── ResourceId: string
├── Quantity: int
└── VolumePerUnit: float

RemoveFromStationStorageAction
├── StationId: int
├── ResourceId: string
└── Quantity: int

InitializeStationStorageAction
├── StationId: int

SetCreditsAction
└── NewBalance: int            # New credit balance (integer)
```

### Cross-Cutting Actions (handled in CompositeReducer)

```
TransferToStationAction
├── StationId: int
├── ResourceId: string
├── Quantity: int
└── VolumePerUnit: float
    Effect: Remove from InventoryState (ship) + Add to StationStorageState

TransferToShipAction
├── StationId: int
├── ResourceId: string
├── Quantity: int
└── VolumePerUnit: float
    Effect: Remove from StationStorageState + Add to InventoryState (ship)

RepairShipAction
├── Cost: int                  # Repair cost (integer credits)
└── NewIntegrity: float (always 1.0)
    Effect: Deduct credits from StationServicesState + Update ShipState.HullIntegrity
```

### New Ship Action

```
RepairHullAction : IShipAction
└── NewIntegrity: float
    Effect: Updates ShipState.HullIntegrity via ShipStateReducer
```

---

## Events (Struct-based, zero-allocation)

```
RefiningJobStartedEvent
├── StationId: int
└── JobId: string

RefiningJobCompletedEvent
├── StationId: int
└── JobId: string

ResourcesSoldEvent
├── ResourceId: string
├── Quantity: int
└── TotalCredits: int          # Total credits earned (integer)

CargoTransferredEvent
├── ResourceId: string
├── Quantity: int
└── ToStation: bool  # true = ship→station, false = station→ship

ShipRepairedEvent
├── Cost: int                  # Repair cost paid (integer)
└── NewIntegrity: float

CreditsChangedEvent
├── OldBalance: int            # Previous balance (integer)
└── NewBalance: int            # New balance (integer)
```

**Published by**: View layer (after successful dispatch) or RefiningJobTicker (on completion).
**Consumed by**: VFX/audio feedback views, credit balance indicator, notification system.

---

## Pure Functions

### RefiningMath.CalculateOutputs

```
Input:
  outputConfigs: ImmutableArray<RefiningOutputConfig>
  inputQuantity: int
  random: Unity.Mathematics.Random (by ref)

Output:
  ImmutableArray<MaterialOutput>

Algorithm (per output config):
  totalYield = 0
  for each input unit (1..inputQuantity):
    offset = random.NextInt(varianceMin, varianceMax + 1)  # [min, max] inclusive
    unitYield = max(0, baseYieldPerUnit + offset)
    totalYield += unitYield
  result.Add(MaterialOutput(materialId, totalYield))

Post-processing:
  Floor all quantities at 0 (already handled per-unit)
  Round down to nearest integer (already integer math)
```

### RefiningMath.CalculateJobDuration

```
Input:
  inputQuantity: int
  baseProcessingTimePerUnit: float
  speedMultiplier: float

Output:
  float (total seconds)

Formula:
  (inputQuantity * baseProcessingTimePerUnit) / max(speedMultiplier, 0.01)
```

### RefiningMath.CalculateJobCost

```
Input:
  inputQuantity: int
  creditCostPerUnit: int

Output:
  int (total credits)

Formula:
  inputQuantity * creditCostPerUnit
```

### RepairMath.CalculateRepairCost

```
Input:
  currentIntegrity: float (0..1)
  repairCostPerHP: int

Output:
  int (total credits)

Formula:
  (int)Mathf.CeilToInt((1.0f - currentIntegrity) * repairCostPerHP)

Notes:
  Ceiling rounding ensures the player always pays for any fractional HP.
  At 100% integrity, cost is 0. At 0% integrity, cost equals repairCostPerHP.
```

---

## State Transition Diagrams

### Refining Job Lifecycle

```
[New Job Started]
    │
    ▼
  Active ──(timer elapses)──► Completed ──(player collects)──► [Removed]
    │                              │
    │ occupies slot                │ slot freed immediately
    │ ore consumed                 │ outputs calculated & stored
    │ credits deducted             │ awaiting player review
    │                              │
    │                              └──(player clicks job)──► Summary Window
    │                                                            │
    │                                                            └──(close window)──► materials → storage
    │                                                                                  job removed
```

### Credit Balance Flow

```
  Starting Balance (configurable int, default 0)
      │
      ├──(sell resources)──► +credits (quantity × baseValue)        [int × int = int]
      ├──(start refining)──► -credits (quantity × costPerUnit)      [int × int = int]
      └──(repair ship)────► -credits (ceil((1-integrity) × costPerHP))  [ceil(float × int) = int]
```

### Cargo Transfer Flow

```
  Ship Cargo ◄──────────────────────► Station Storage
      │          TransferToStation         │
      │     (validate ship has stock)      │
      │     (always succeeds - unlimited)  │
      │                                    │
      │          TransferToShip            │
      │   (validate station has stock)     │
      │   (validate ship has capacity)     │
      └────────────────────────────────────┘
```
