# Implementation Plan: In-Flight Targeting & Multi-Target Lock System

**Branch**: `007-target-lock-system` | **Date**: 2026-03-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-target-lock-system/spec.md`

## Summary

Implement a complete in-flight targeting and multi-target lock system. Players left-click to select asteroids or stations, seeing corner-bracket reticles with name/type above and range/mass below. Timed locks are initiated via radial menu, producing persistent HUD target cards with isolated live viewports. An `ITargetable` interface in `Core/Extensions` provides extensible targeting for MonoBehaviour-based objects; ECS entities (asteroids) use adapter functions producing a shared `TargetInfo` readonly struct. All targeting state flows through a new `TargetingState` slice in `GameLoopState` via pure reducers. Off-screen tracking uses directional triangle indicators at screen edges.

## Technical Context

**Language/Version**: C# 9.0 / .NET Framework 4.7.1 (Unity 6000.3.10f1)
**Primary Dependencies**: Unity Entities 1.3.2, VContainer 1.16.7, UniTask 2.5.10, System.Collections.Immutable (via NuGetForUnity 4.5.0), UI Toolkit, Unity.Mathematics, Unity.Transforms
**Storage**: In-memory immutable state (StateStore singleton) + ScriptableObjects for designer data. No persistent storage.
**Testing**: NUnit + Unity Test Framework. EditMode for pure logic (reducers, math), PlayMode for UI integration. TDD mandatory (Red-Green-Refactor).
**Target Platform**: Windows 64-bit Standalone
**Project Type**: Unity 3D game (hybrid DOTS/ECS + MonoBehaviour)
**Performance Goals**: 60 FPS minimum on mid-range PC (GTX 1060 / RX 580). Zero GC in hot loops. <2ms frame spikes. Max 3 simultaneous RenderTextures at 128x128.
**Constraints**: C# 9.0 only (no `record struct`). Functional/immutable-first. No mutable globals or static singletons for game logic. UI Toolkit only (no Canvas/UGUI).
**Scale/Scope**: Single-player. Up to 3 simultaneous target locks. 2 station types + ~300 asteroids as targetable objects.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Functional & Immutable First | **PASS** | `TargetingState` is a `sealed record`. Sub-types (`SelectionData`, `LockAcquisitionData`, `TargetLockData`) are `readonly struct`. `LockedTargets` uses `ImmutableArray<TargetLockData>`. All state transitions via pure `TargetingReducer`. `TargetInfo` is a `readonly struct`. |
| II. Predictability & Testability | **PASS** | `TargetingReducer` is a pure static function. `LockTimeMath` and `TargetingMath` are pure static classes. No hidden mutable state. All dependencies explicitly injected via VContainer. TDD for all logic. |
| III. Performance by Default | **PASS** | Reticle rendering uses UI Toolkit (GPU-composited). RenderTextures are 128x128, max 3 concurrent. Preview clones use dedicated culling layer — no main camera impact. `TargetingMath` screen-space functions are lightweight per-frame. |
| IV. Data-Oriented Design | **PASS** | `ShipArchetypeConfig` SO extended with targeting fields. New `TargetingConfig` SO for global settings. `ITargetable` is composition (interface, not inheritance). ECS asteroids queried directly — no wrapping in GameObjects. |
| V. Modularity & Extensibility | **PASS** | New `VoidHarvest.Features.Targeting` assembly with explicit dependencies. `ITargetable` interface in `Core/Extensions` enables future implementors (NPC, debris, cargo pods) without modifying targeting code. Cross-system communication via EventBus. |
| VI. Explicit Over Implicit | **PASS** | VContainer DI with `[Inject]` method pattern. All event subscriptions explicit. `ITargetable` interface contract is explicit — no convention-based discovery. |
| Player Documentation | **PASS** | FR-036/FR-037 mandate HOWTOPLAY.md and changelog updates. Delivery gate enforced. |
| Unity MCP Verification | **PASS** | Compile-check after scripts, console monitoring, test execution via MCP during implementation. |

**Post-design re-check**: All 8 gates still PASS. The `ITargetable` interface in `Core/Extensions` has zero dependencies — no circular reference risk. The hybrid ECS/MonoBehaviour selection model preserves existing `InputBridge` patterns. No constitution deviations needed.

## Project Structure

### Documentation (this feature)

```text
specs/007-target-lock-system/
├── plan.md              # This file
├── research.md          # Phase 0: 8 architectural decisions
├── data-model.md        # Phase 1: complete data model
├── quickstart.md        # Phase 1: developer testing guide
├── contracts/           # Phase 1: interface contracts
│   ├── itargetable.md   # ITargetable interface contract
│   └── targeting-state.md # TargetingState public API contract
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Assets/
├── Core/
│   ├── Extensions/
│   │   ├── ITargetable.cs              # NEW: Cross-cutting targetable interface
│   │   ├── TargetInfo.cs               # NEW: Readonly struct — shared target snapshot
│   │   └── TargetType.cs               # MODIFIED: (already has Asteroid=1, Station=2 — no change needed)
│   ├── State/
│   │   ├── GameState.cs                # MODIFIED: +TargetingState in GameLoopState
│   │   └── GameStateReducer.cs         # MODIFIED: TargetingState.Empty default
│   └── RootLifetimeScope.cs            # MODIFIED: +ITargetingAction routing in CompositeReducer
│
├── Features/
│   ├── Targeting/                      # NEW: Targeting feature module
│   │   ├── Data/
│   │   │   ├── TargetingState.cs       # Sealed record: Selection + LockAcquisition + LockedTargets
│   │   │   ├── SelectionData.cs        # Readonly struct: current selection snapshot
│   │   │   ├── LockAcquisitionData.cs  # Readonly struct: lock-in-progress state
│   │   │   ├── LockAcquisitionStatus.cs # Enum: None/InProgress/Completed/Cancelled
│   │   │   ├── TargetLockData.cs       # Readonly struct: confirmed lock entry
│   │   │   ├── TargetingActions.cs     # ITargetingAction marker + all action sealed records
│   │   │   ├── TargetingEvents.cs      # Event readonly structs (TargetLockedEvent, etc.)
│   │   │   ├── LockFailReason.cs       # Enum: Deselected/OutOfRange/TargetDestroyed
│   │   │   └── TargetingConfig.cs      # NEW SO: global targeting visual config
│   │   ├── Systems/
│   │   │   ├── TargetingReducer.cs     # Pure static reducer for TargetingState
│   │   │   ├── LockTimeMath.cs         # Pure static: CalculateLockTime(baseLockTime, TargetInfo)
│   │   │   └── TargetingMath.cs        # Pure static: screen bounds, edge clamp, viewport check, range format
│   │   ├── Views/
│   │   │   ├── TargetingController.cs  # MonoBehaviour: orchestrates selection display, lock acquisition ticking
│   │   │   ├── ReticleView.cs          # UI Toolkit: corner brackets + name/type + range/mass labels
│   │   │   ├── LockProgressView.cs     # UI Toolkit: progress arc/ring around reticle
│   │   │   ├── OffScreenIndicatorView.cs # UI Toolkit: directional triangle at screen edge
│   │   │   ├── TargetCardPanelView.cs  # UI Toolkit: card container panel (left of ship info)
│   │   │   ├── TargetCardView.cs       # UI Toolkit: individual card (viewport + name + range + dismiss)
│   │   │   ├── TargetPreviewManager.cs # MonoBehaviour: manages preview clones + cameras + RenderTextures
│   │   │   ├── TargetingAudioController.cs # MonoBehaviour: rising tone, confirm, fail sounds
│   │   │   ├── Targeting.uxml          # Reticle + card panel layout
│   │   │   ├── Targeting.uss           # Targeting-specific styles
│   │   │   ├── TargetingAudioConfig.cs # SO: audio clip references
│   │   │   └── TargetingVFXConfig.cs   # SO: visual effect references (reticle pulse, lock flash)
│   │   ├── Tests/
│   │   │   ├── TargetingReducerTests.cs # All reducer action tests
│   │   │   ├── LockTimeMathTests.cs     # Lock time calculation tests
│   │   │   ├── TargetingMathTests.cs    # Screen-space math tests
│   │   │   ├── TargetInfoTests.cs       # TargetInfo construction + sentinel tests
│   │   │   └── SelectionIntegrationTests.cs # Cross-system integration tests
│   │   ├── VoidHarvest.Features.Targeting.asmdef
│   │   └── VoidHarvest.Features.Targeting.Tests.asmdef
│   │
│   ├── Ship/Data/
│   │   └── ShipArchetypeConfig.cs      # MODIFIED: +BaseLockTime, +MaxTargetLocks, +MaxLockRange
│   │
│   ├── Input/Views/
│   │   └── InputBridge.cs              # MODIFIED: ITargetable check on Physics path, TargetInfo dispatch
│   │
│   ├── HUD/Views/
│   │   ├── HUD.uxml                    # MODIFIED: remove target-info-panel, add targeting overlay container
│   │   ├── HUD.uss                     # MODIFIED: +targeting card panel styles
│   │   ├── HUDView.cs                  # MODIFIED: remove target info panel wiring
│   │   └── RadialMenu/
│   │       └── RadialMenuController.cs # MODIFIED: +LockTarget segment (top-left position)
│   │
│   └── Docking/
│       └── Views/
│           └── (existing)              # Cross-cutting: ClearAllLocksAction on dock/undock
```

**Structure Decision**: New `Features/Targeting/` directory with standard `Data/Systems/Views/Tests/` sub-structure. `ITargetable` and `TargetInfo` placed in `Core/Extensions/` (zero-dependency assembly referenced by all features) to enable any assembly to implement targeting without circular deps. `TargetableStation` MonoBehaviour placed on station prefabs in the Targeting assembly (reads StationData via WorldState). ECS asteroids produce `TargetInfo` via adapter function — no wrapper GameObjects needed.

### Assembly Dependency Changes

| Assembly | Change | New References |
|----------|--------|---------------|
| **VoidHarvest.Features.Targeting** | NEW | Core.Extensions, Core.State, Core.EventBus, Features.Ship, Features.Mining, Unity.Mathematics, Unity.Entities, Unity.Collections, Unity.Transforms, UniTask, VContainer |
| **VoidHarvest.Features.Targeting.Tests** | NEW | Features.Targeting, Core.Extensions, Core.State, Core.EventBus, nunit.framework, UnityEngine.TestRunner, UnityEditor.TestRunner |
| **VoidHarvest.Features.HUD** | MODIFIED | +Features.Targeting (for TargetingState reads, TargetInfo) |
| **VoidHarvest.Features.Input** | MODIFIED | +Features.Targeting (for dispatching targeting actions) |

## Complexity Tracking

No constitution violations. No complexity justifications needed.

The `ITargetable` interface in `Core/Extensions` is a new public contract — simple and zero-dependency. The hybrid ECS/ITargetable selection model follows the existing `InputBridge` dual-path pattern (Physics raycast for stations, ECS ray-sphere for asteroids). The isolated viewport rendering via dedicated culling layer + preview clones is the most complex visual component but is self-contained within `TargetPreviewManager`.
