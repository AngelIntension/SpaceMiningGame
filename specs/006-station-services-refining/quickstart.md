# Quickstart: Station Services Menu & Data-Driven Refining

**Branch**: `006-station-services-refining` | **Date**: 2026-03-01

## Prerequisites

- Unity 6000.3.10f1 with URP 17
- All packages installed (see CLAUDE.md)
- Spec 005 (data-driven ore system) complete — 3 ore assets exist

## Testing the Feature

### Full Mine-to-Sell Loop (SC-001)

1. Open `GameScene` in Unity Editor
2. Enter Play mode
3. Fly ship to a station (Small Mining Relay at origin+200z, or Medium Refinery Hub at +500x)
4. Mine an asteroid to collect ore (left-click asteroid, activate mining beam)
5. Approach station and dock (right-click station → Dock)
6. Station Services Menu opens automatically
7. Select **Cargo Transfer** → transfer ore from ship to station
8. Select **Sell Resources** → sell ore for credits
9. Verify credit balance increases

### Refining Loop (SC-002)

1. After transferring ore to station storage (above)
2. Select **Refine Ores** → choose ore type, set quantity
3. Verify live cost preview shows credit cost and expected outputs
4. Click **Start Job** → ore removed from storage, credits deducted, job appears in list
5. Wait for job timer to complete (or use debug time acceleration)
6. Job transitions to "Completed" status (visually distinct)
7. Click completed job → summary window shows generated materials with yield variance
8. Close summary → materials transferred to station storage

### Repair Loop (SC-006)

1. Damage ship hull (e.g., via debug command or mining hazard)
2. Dock at Medium Refinery Hub (has repair service)
3. Select **Basic Repair** → view cost, confirm
4. Hull integrity restored to 100%, credits deducted

### Data-Driven Extensibility (SC-004)

1. In Unity Editor: `Create > VoidHarvest > Raw Material Definition`
2. Fill in: MaterialId, DisplayName, BaseValue, VolumePerUnit
3. In Unity Editor: `Create > VoidHarvest > Ore Definition`
4. Fill in ore fields + `RefiningOutputs` array referencing the new material
5. Add ore to an `AsteroidFieldDefinition`
6. Enter Play mode → mine, transfer, refine → new material appears in all panels

## Key Files to Verify

| File | What to Check |
|------|--------------|
| `Assets/Features/StationServices/Systems/StationServicesReducer.cs` | All actions produce correct new state |
| `Assets/Features/StationServices/Systems/RefiningMath.cs` | Per-unit rolling, floor at 0, deterministic with seed |
| `Assets/Features/StationServices/Views/StationServicesMenuController.cs` | Auto-open on dock, close on undock |
| `Assets/Features/Mining/Data/OreDefinition.cs` | New fields: RefiningOutputs[], RefiningCreditCostPerUnit |
| `Assets/Features/Resources/Data/RawMaterialDefinition.cs` | Create menu works, fields serialize |
| `Assets/Core/State/GameState.cs` | GameLoopState.StationServices replaces .Refining |
| `Assets/Core/RootLifetimeScope.cs` | CompositeReducer routes IStationServicesAction |

## Running Tests

```bash
# EditMode tests (pure logic)
Unity -runTests -batchmode -projectPath . -testResults results.xml -testPlatform EditMode

# Or via Unity MCP:
run_tests mode=EditMode assembly_names=["VoidHarvest.Features.StationServices.Tests"]
```

### Expected Test Coverage

- `StationServicesReducerTests`: All action types, edge cases (0 quantity, insufficient credits, empty storage)
- `StationStorageReducerTests`: Add/remove resources, unlimited capacity
- `RefiningMathTests`: Yield calculation with known seeds, per-unit rolling, floor at 0, variance bounds
- `CargoTransferTests`: Ship→station, station→ship, capacity rejection
- `SellResourcesTests`: Credit calculation, empty storage
- `RepairTests`: Cost calculation, already-full integrity, insufficient credits
- `RefiningJobLifecycleTests`: Start→complete→collect, slot management, background persistence

## Debug Tips

- **Fast-forward refining**: Temporarily set `BaseProcessingTimePerUnit` to 0.1 on ore assets
- **Free credits**: Set `StartingCredits` to 10000 in `GameServicesConfig`
- **Check state**: Inspect `StateStore.Current.Loop.StationServices` in debugger
- **Console monitoring**: Check Unity console after every script change (compilation gate)
