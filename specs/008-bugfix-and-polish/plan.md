# Implementation Plan: Bugfix, Event Lifecycle & UI Polish

**Branch**: `feature/008-bugfix-and-polish` | **Date**: 2026-03-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/008-bugfix-and-polish/spec.md`

## Summary

Systematic bugfix and hardening pass across all Phase 0 systems. Fixes 7 confirmed bug categories: UI state-change detection failures causing stale panel data, event subscription lifecycle leaks causing duplicate handlers, silent input failures with no player/developer feedback, fragile `FindObjectOfType` component discovery, incorrect time source for refining jobs, missing ScriptableObject edit-time validation, and inconsistent Create menu paths. No new gameplay mechanics — all changes are behavioral corrections and architectural hardening of existing code.

## Technical Context

**Language/Version**: C# 9.0 / .NET Framework 4.7.1
**Primary Dependencies**: Unity 6 (6000.3.10f1), UniTask 2.5.10, VContainer 1.16.7, Unity Entities 1.3.2, Input System 1.18.0
**Storage**: N/A (immutable in-memory state via reducer pattern)
**Testing**: NUnit + Unity Test Framework (EditMode), 465 existing tests across 29 assembly definitions
**Target Platform**: Windows 64-bit Standalone
**Project Type**: Unity game (3D space mining simulator)
**Performance Goals**: 60 FPS minimum on mid-range PC, zero GC allocations in hot loops
**Constraints**: All domain data immutable, pure reducer state management, no mutable globals
**Scale/Scope**: ~150 C# files, 7 bug categories, ~25 files modified, ~30+ new tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Functional & Immutable First | PASS | No new mutable state introduced. All fixes preserve immutable patterns. `OnValidate()` is Unity lifecycle (editor-only side effect, permitted). |
| II. Predictability & Testability | PASS | DI migration improves testability. Removing `FindObjectOfType` eliminates implicit initialization order. All new behavior is testable. |
| III. Performance by Default | PASS | Reference equality checks retained (efficient optimization). No new per-frame allocations. `Time.realtimeSinceStartup` is a direct property read. |
| IV. Data-Oriented Design | PASS | No changes to ECS or data architecture. ScriptableObjects remain single source of truth. |
| V. Modularity & Extensibility | PASS | DI migration strengthens module boundaries. Subscription lifecycle convention improves composability. |
| VI. Explicit Over Implicit | PASS | Replacing `FindObjectOfType` with explicit DI injection. Adding defensive logging makes failures visible. Subscription lifecycle is now explicit (`OnEnable`/`OnDisable`). |
| Editor Automation (MCP) | PASS | All script changes will be validated via MCP compilation check. Tests run via MCP. |
| Player Documentation | PASS | No player-facing behavior changes. HOWTOPLAY.md update not required. |

**Post-design re-check**: PASS — No constitution violations. All changes align with and strengthen existing principles.

## Project Structure

### Documentation (this feature)

```text
specs/feature/008-bugfix-and-polish/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research findings
├── data-model.md        # Phase 1 data model (no new types)
├── quickstart.md        # Phase 1 implementation quickstart
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (files modified — no new files except tests)

```text
Assets/
├── Features/
│   ├── StationServices/Views/
│   │   ├── CargoTransferPanelController.cs      # OnDestroy safety net
│   │   ├── RefineOresPanelController.cs         # + Inventory slice, OnDestroy, realtimeSinceStartup
│   │   ├── SellResourcesPanelController.cs      # + Inventory slice, OnDestroy
│   │   ├── BasicRepairPanelController.cs        # OnDestroy safety net
│   │   ├── CreditBalanceIndicator.cs            # OnDestroy safety net
│   │   ├── StationServicesMenuController.cs     # OnEnable/OnDisable lifecycle, DI inject
│   │   └── RefiningJobTicker.cs                 # realtimeSinceStartup
│   ├── StationServices/Tests/
│   │   └── [new test files for state-change + validation]
│   ├── HUD/Views/RadialMenu/
│   │   └── RadialMenuController.cs              # OnEnable/OnDisable lifecycle, DI inject
│   ├── Targeting/Views/
│   │   ├── TargetingAudioController.cs          # OnEnable/OnDisable lifecycle
│   │   └── TargetingController.cs               # DI inject
│   ├── Input/Views/
│   │   └── InputBridge.cs                       # Defensive logging, DI inject, state sync
│   ├── Mining/Data/
│   │   └── OreDefinition.cs                     # OnValidate
│   ├── Ship/Data/
│   │   └── ShipArchetypeConfig.cs               # OnValidate
│   ├── Procedural/Data/
│   │   └── AsteroidFieldDefinition.cs           # OnValidate
│   ├── Docking/Data/
│   │   ├── DockingConfig.cs                     # OnValidate
│   │   ├── DockingVFXConfig.cs                  # CreateAssetMenu path
│   │   └── DockingAudioConfig.cs                # CreateAssetMenu path
│   ├── Resources/Data/
│   │   └── RawMaterialDefinition.cs             # OnValidate
│   ├── Camera/Data/
│   │   └── SkyboxConfig.cs                      # CreateAssetMenu path only
│   └── [various SO files]                       # CreateAssetMenu path updates
├── Core/
│   └── SceneLifetimeScope.cs                    # Register view MBs for DI
└── [19 total CreateAssetMenu attribute updates across SO files]
```

**Structure Decision**: No new directories. All changes are in-place modifications to existing feature folders following the established `Data/`, `Systems/`, `Views/`, `Tests/` sub-structure. New test files added to existing `Tests/` directories.

## Complexity Tracking

No constitution violations. No complexity tracking needed.
