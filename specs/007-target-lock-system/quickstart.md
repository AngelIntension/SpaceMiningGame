# Quickstart: In-Flight Targeting & Multi-Target Lock System

**Branch**: `007-target-lock-system` | **Date**: 2026-03-02

## Prerequisites

- Unity 6000.3.10f1 with URP 17
- All packages installed (see CLAUDE.md)
- Specs 001-006 complete — mining loop, docking, station services functional

## Testing the Feature

### Selection & Reticle (SC-001)

1. Open `GameScene` in Unity Editor
2. Enter Play mode
3. Fly ship toward the asteroid field
4. Left-click on an asteroid
5. Verify: four corner-bracket reticle appears around the asteroid
6. Verify: name + ore type displayed centered above the reticle
7. Verify: range (meters) + mass (%) displayed centered below the reticle
8. Move ship closer/farther — verify range updates in real time
9. Left-click on empty space — verify reticle disappears
10. Left-click on a station — verify reticle transfers with station name

### Off-Screen Tracking (SC-001 continued)

1. Select a target (asteroid or station)
2. Rotate ship until target exits the viewport
3. Verify: reticle is replaced by a small directional triangle at screen edge
4. Verify: triangle points toward the target and tracks continuously
5. Rotate back to face target — verify full reticle returns

### Timed Locking (SC-002)

1. Left-click to select a target
2. Right-click to open the radial menu
3. Verify: "Lock Target" segment visible at top-left position
4. Click "Lock Target"
5. Verify: progress arc/ring fills around reticle, corners pulse, rising-tone audio plays
6. Wait for lock time to elapse (default 1.5s)
7. Verify: confirmation flash + audio cue, target is now locked

### Lock Cancellation

1. Select a target, initiate lock via radial menu
2. During lock progress, left-click on empty space (deselect)
3. Verify: lock cancelled with failure sound, progress indicators reset
4. Select a target beyond 5000m, attempt lock
5. Verify: lock fails immediately with "out of range" feedback

### Multi-Target Management (SC-003)

1. Lock target #1 (asteroid)
2. Select and lock target #2 (different asteroid)
3. Verify: two target cards appear left of ship info, #1 shifted left, #2 on right
4. Lock target #3 (station)
5. Verify: three cards, rightmost is newest
6. Attempt to lock a 4th target
7. Verify: "lock slots full" feedback, lock does not begin

### Target Cards (SC-003, SC-004)

1. With locked targets, inspect each card
2. Verify: live viewport shows ONLY the targeted object (isolated, no background clutter)
3. Verify: name/type and updating range below viewport
4. Click the dismiss (X) control on a card
5. Verify: card removed, remaining cards reflow
6. Click a card body (not dismiss) — verify target becomes selected (reticle appears on it)

### Dock/Undock Clears Locks (SC-007)

1. Lock 2-3 targets
2. Approach station and dock
3. Verify: all target cards removed, targeting state cleared
4. Undock
5. Verify: target card panel empty, ready for new locks

### Different Lock Times (SC-002)

1. In Inspector, find the active ship's `ShipArchetypeConfig`
2. Set `BaseLockTime` to 3.0
3. Enter Play mode, select and lock a target
4. Verify: lock takes ~3.0 seconds (vs default 1.5)

## Key Files to Verify

| File | What to Check |
|------|--------------|
| `Assets/Core/Extensions/ITargetable.cs` | Interface with TargetId, DisplayName, TypeLabel, TargetType |
| `Assets/Core/Extensions/TargetInfo.cs` | Readonly struct with From(ITargetable), FromAsteroid(), None sentinel |
| `Assets/Features/Targeting/Systems/TargetingReducer.cs` | All 8 actions produce correct new state |
| `Assets/Features/Targeting/Systems/LockTimeMath.cs` | Returns baseLockTime, accepts TargetInfo param |
| `Assets/Features/Targeting/Systems/TargetingMath.cs` | Screen bounds, edge clamp, viewport check, range format |
| `Assets/Features/Targeting/Data/TargetingConfig.cs` | SO with reticle/card visual settings |
| `Assets/Features/Ship/Data/ShipArchetypeConfig.cs` | New fields: BaseLockTime, MaxTargetLocks, MaxLockRange |
| `Assets/Core/State/GameState.cs` | GameLoopState includes TargetingState |
| `Assets/Core/RootLifetimeScope.cs` | CompositeReducer routes ITargetingAction |
| `Assets/Features/Input/Views/InputBridge.cs` | ITargetable check on Physics raycast path |
| `Assets/Features/HUD/Views/RadialMenu/RadialMenuController.cs` | LockTarget segment at top-left |

## Running Tests

```bash
# EditMode tests (pure logic)
Unity -runTests -batchmode -projectPath . -testResults results.xml -testPlatform EditMode

# Or via Unity MCP:
run_tests mode=EditMode assembly_names=["VoidHarvest.Features.Targeting.Tests"]
```

### Expected Test Coverage

- `TargetingReducerTests`: All 8 action types, edge cases (duplicate lock, slots full, cancel when no lock active, clear all)
- `LockTimeMathTests`: Returns baseLockTime, accepts TargetInfo parameter (future extensibility)
- `TargetingMathTests`: Screen bounds calculation, edge clamping, viewport check, range formatting ("1,247 m", "523 m")
- `TargetInfoTests`: Construction from ITargetable, construction from asteroid data, None sentinel, IsValid checks
- `SelectionIntegrationTests`: Selection→lock→unlock full lifecycle, dock clears all locks

## Debug Tips

- **Skip lock timer**: Temporarily set `BaseLockTime` to 0.1 on ship config SO
- **Test max locks**: Set `MaxTargetLocks` to 1 to easily test "slots full" behavior
- **Infinite lock range**: Set `MaxLockRange` to 99999 to bypass range checks
- **Check state**: Inspect `StateStore.Current.Loop.Targeting` in debugger
- **Preview layer**: Verify "TargetPreview" layer exists in Tags & Layers settings
- **Console monitoring**: Check Unity console after every script change (compilation gate)
