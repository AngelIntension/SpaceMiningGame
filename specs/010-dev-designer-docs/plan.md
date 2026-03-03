# Implementation Plan: Developer & Designer Documentation Bootstrap

**Branch**: `010-dev-designer-docs` | **Date**: 2026-03-03 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-dev-designer-docs/spec.md`

## Summary

Bootstrap the `docs/` directory with 25 markdown files covering architecture (5), per-system references (12), designer guides (4), and supporting docs (4) — as mandated by Constitution v1.4.0. This is a documentation-only deliverable requiring no code changes. All content is derived from the existing codebase (241 C# files, 29 assemblies, 25 event types, 24 ScriptableObject types, 12 shipped features).

## Technical Context

**Language/Version**: Markdown with Mermaid diagram syntax
**Primary Dependencies**: None (static documentation files)
**Storage**: N/A (git-tracked markdown files at `docs/`)
**Testing**: Manual structural validation — file existence, section completeness, Mermaid syntax validity
**Target Platform**: GitHub rendering, VS Code markdown preview, any CommonMark-compatible viewer
**Project Type**: Documentation artifact for Unity 6 game project (VoidHarvest)
**Performance Goals**: N/A
**Constraints**: All diagrams MUST use Mermaid syntax; designer guide MUST contain zero C# code or namespace references; all content MUST accurately reflect the current codebase state
**Scale/Scope**: 25 markdown files, ~12 Mermaid diagrams minimum (1 per system doc + architecture docs)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Functional & Immutable First | N/A | No code changes — docs only |
| II. Predictability & Testability | N/A | No code changes — docs only |
| III. Performance by Default | N/A | No code changes — docs only |
| IV. Data-Oriented Design | N/A | No code changes — docs only |
| V. Modularity & Extensibility | PASS | Docs mirror the modular feature structure |
| VI. Explicit Over Implicit | PASS | All architectural decisions will be explicitly documented |
| Developer & Designer Documentation | PASS | This feature IS the constitutional requirement — bootstrapping the mandated `docs/` directory |
| Player Documentation (HOWTOPLAY.md) | OUT OF SCOPE | Separate constitution requirement; excluded per spec assumptions |
| Editor Automation (Unity MCP) | N/A | No scripts to compile; no scene changes |
| Testing & Quality Standards | N/A | No code — structural validation only |
| Git Conventions | PASS | Feature branch `010-dev-designer-docs`, Conventional Commits |

**Gate result**: PASS — no violations. This is a pure documentation deliverable that directly satisfies the constitutional mandate.

## Project Structure

### Documentation (this feature)

```text
specs/010-dev-designer-docs/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output (codebase survey reference)
├── data-model.md        # Phase 1 output (document taxonomy)
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
docs/
├── architecture/
│   ├── overview.md               # System-level Mermaid diagram, all features + boundaries
│   ├── state-management.md       # GameState tree, reducer composition, action dispatch
│   ├── event-system.md           # 25 event types, publisher/subscriber map
│   ├── dependency-injection.md   # VContainer scopes, registration, async convention
│   └── data-pipeline.md          # SO → Baker → BlobAsset → ECS System → View
├── systems/
│   ├── camera.md                 # CameraState, CameraReducer, Cinemachine integration
│   ├── input.md                  # InputBridge, PilotCommand, InteractionConfig
│   ├── ship.md                   # ShipState, ShipPhysicsSystem, 6DOF flight
│   ├── mining.md                 # MiningBeamSystem, ore blobs, depletion pipeline
│   ├── procedural.md             # AsteroidFieldSystem, Burst job spawning
│   ├── resources.md              # InventoryState, InventoryReducer, slots
│   ├── hud.md                    # HUDView, RadialMenu, TargetInfoPanel
│   ├── docking.md                # DockingSystem, state machine, blob config
│   ├── station-services.md       # StationServicesReducer, refining, selling, repair
│   ├── targeting.md              # TargetingState, lock system, preview cameras
│   ├── station.md                # StationDefinition, StationType, service configs
│   └── world.md                  # WorldDefinition, WorldState, data-driven init
├── designer-guide/
│   ├── scriptable-objects.md     # Full SO catalog (24 types)
│   ├── adding-ores.md            # Step-by-step: new OreDefinition + field config
│   ├── adding-stations.md        # Step-by-step: new StationDefinition + services
│   └── tuning-reference.md       # All tunable parameters, quick-reference table
├── glossary.md                   # Project terminology (~30 terms)
├── troubleshooting.md            # Known pitfalls (4+ from project memory)
├── onboarding.md                 # Recommended reading order
└── assembly-map.md               # 29 assemblies, Mermaid dependency graph
```

**Structure Decision**: Follows the exact directory layout mandated by Constitution v1.4.0 Section "Developer & Designer Documentation". No deviations.

## Complexity Tracking

No constitution violations. No complexity justifications needed.
