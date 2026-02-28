# Quickstart: Station Docking & Interaction Framework

**Feature**: 004-station-docking | **Date**: 2026-02-28

## Prerequisites

- Unity 6 (6000.3.10f1) with the VoidHarvest project open
- Unity MCP bridge connected and responsive
- Branch `004-station-docking` checked out
- All existing tests passing (run via MCP `run_tests`)

## Implementation Entry Points

### Where to start coding

1. **Core state types first** — `Assets/Core/State/DockingState.cs` and `IDockingAction.cs`. These are leaf dependencies that everything else builds on.
2. **DockingReducer + tests** — `Assets/Features/Docking/Systems/DockingReducer.cs` and `Tests/DockingReducerTests.cs`. TDD: write tests first.
3. **DockingMath + tests** — `Assets/Features/Docking/Systems/DockingMath.cs` and `Tests/DockingMathTests.cs`. TDD: write tests first.

### Key files to read before modifying

| File | Why |
|------|-----|
| `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs` | Understand segment wiring, InputBridge interaction |
| `Assets/Features/Input/Views/InputBridge.cs` | Understand targeting flow, TryRaycastSelectable, pilot command updates |
| `Assets/Features/Ship/Systems/ShipPhysicsMath.cs` | Understand DetermineFlightMode and approach thrust |
| `Assets/Features/Ship/Systems/ShipPhysicsSystem.cs` | Understand ECS update loop and flight mode handling |
| `Assets/Core/State/FleetState.cs` | DockedAtStation field (already exists) |
| `Assets/Core/RootLifetimeScope.cs` | CompositeReducer wiring and initial state creation |

### Existing extension points

- `RadialMenuAction.Dock` — already defined in `PilotCommand.cs` (value 4), not wired
- `FleetState.DockedAtStation` — `Option<int>` field exists, FleetReducer is a stub
- `TryRaycastSelectable` in InputBridge — physics raycast against `Selectable` layer already implemented
- `ShipFlightMode.Warp` — placeholder enum value pattern to follow for `Docking`/`Docked`

## MCP Verification Loop

After every script creation or modification:

```
1. Wait for compilation     → poll editor_state.isCompiling
2. Check console for errors → read_console(types=["error"])
3. Run relevant tests       → run_tests(mode="EditMode") / get_test_job(job_id)
4. Verify scene if modified → manage_scene(action="get_hierarchy")
```

## Test Strategy

| Test File | Scope | Type |
|-----------|-------|------|
| DockingReducerTests.cs | All state transitions, edge cases | EditMode (pure logic) |
| DockingMathTests.cs | Approach interpolation, snap curves, range checks | EditMode (pure math) |
| DockingSystemTests.cs | ECS state machine, physics integration | PlayMode |

## Common Gotchas

- **ECS mutable shell**: All ECS `IComponentData` structs use mutable fields (existing Constitution deviation). Follow the `// CONSTITUTION DEVIATION: ECS mutable shell` pattern.
- **GameLoopState modification**: Adding `DockingState` field changes the record constructor. Update `RootLifetimeScope.CreateDefaultGameState()` and any test code that creates `GameLoopState`.
- **Selectable layer**: Verify the `Selectable` layer exists in Unity's Tag Manager before assigning it to station prefabs. Use `manage_editor(action="add_layer", layer_name="Selectable")` if needed.
- **asmdef references**: New `VoidHarvest.Features.Docking.asmdef` must be referenced by HUD and Input asmdefs for them to access docking types.
