# VoidHarvest

A relaxing-yet-engaging 3D space mining simulator built in Unity 6. Pilot customizable ships, harvest procedural asteroid fields, manage resources, upgrade your fleet, and survive cosmic hazards.

**Core Loop**: Explore → Mine → Refine → Expand → Survive

## Vision

- **3rd-person perspective** with a smooth orbiting follow camera and speed-based zoom
- **EVE Online-inspired controls** — mouse-driven targeting, click-to-align, radial context menus, hotbar modules
- **Ship fleet system** — own and swap between specialized ships (Mining Barge, Hauler, Combat Scout)
- **Deep tech tree** — branching research unlocking hulls, lasers, refineries, and economic multipliers
- **Player-built bases** in asteroid belts with a fully simulated supply/demand economy

## Tech Stack

| | |
|---|---|
| Engine | Unity 6 (6000.3.10f1) |
| Rendering | Universal Render Pipeline (URP) 17.3.0 |
| Platform | Windows 64-bit (VR/console planned) |
| Language | C# 9.0 / .NET Framework 4.7.1 |
| Architecture | Hybrid DOTS/ECS (simulation) + MonoBehaviour (UI/views) |
| State Management | Functional/immutable — pure reducers, record types |
| DI Framework | VContainer 1.16.7 |
| Async / EventBus | UniTask 2.5.10 |

## Getting Started

1. Install **Unity 6** (6000.3.10f1) via Unity Hub
2. Clone the repository
3. Open the project in Unity Editor
4. Solution files are auto-generated — do not commit `*.sln` / `*.csproj`

### Running Tests

Via Unity Test Runner (Window > General > Test Runner) or CLI:

```
Unity -runTests -batchmode -projectPath . -testResults results.xml -testPlatform EditMode
```

## Project Structure

```
Assets/
├── Features/                # One folder per major system
│   ├── Camera/              # 3rd-person orbiting follow camera
│   ├── Input/               # EVE-style controls, PilotCommand
│   ├── Ship/                # Ship state, physics, modules
│   ├── Fleet/               # Multi-ship ownership, swapping
│   ├── Mining/              # Beam targeting, yield reducers
│   ├── Resources/           # Resource / inventory system
│   ├── Procedural/          # Asteroid field generation
│   ├── HUD/                 # In-game UI, radial menus, hotbar
│   ├── TechTree/            # Research / progression
│   ├── Economy/             # Market simulation
│   └── Base/                # Base building
├── Core/                    # Shared infrastructure
│   ├── EventBus/            # UniTask-based reactive messaging
│   ├── State/               # Reducer framework, state store
│   ├── Pools/               # ObjectPool<T> implementations
│   └── Extensions/          # C# extension methods, utilities
├── Settings/                # URP configs, volume profiles
└── Scenes/
```

Each feature folder uses `Data/`, `Systems/`, `Views/`, `Tests/` sub-folders.

## Roadmap

| Phase | Focus | Status |
|-------|-------|--------|
| **0 — MVP** | 3rd-person camera, EVE-style controls, 6DOF ship physics, mining beam, procedural asteroid field, HUD | **Complete** |
| **1** | Ship fleet swapping, basic tech tree (3–4 tiers) | Planned |
| **2** | Refining, hauling roles, outpost/base building | Planned |
| **3** | Dynamic economy simulation, deep base customization, multi-ship fleet management | Planned |

## What's Implemented (Phase 0 MVP)

- **Ship flight** — 6DOF physics with inertia, flight modes, Burst-compiled math
- **EVE-style controls** — mouse targeting, double-click align, radial context menus, 8-slot hotbar, keyboard thrust/strafe/roll
- **3rd-person camera** — orbiting follow camera with zoom (Cinemachine)
- **Mining** — beam targeting, yield calculation, asteroid depletion (ECS systems)
- **Procedural asteroid field** — Burst-compiled job-based generation
- **Resource inventory** — immutable state with pure reducers
- **HUD** — target info panel, warnings, selection outlines, radial menus
- **Core infrastructure** — EventBus (UniTask), State Store, VContainer DI, 17 tests

## Architecture Principles

This project follows a strict **functional/immutable-first** architecture. See [`.specify/memory/constitution.md`](.specify/memory/constitution.md) for the full project constitution.

Key rules:

- All game state managed via **pure reducers**: `(State, Action) → State`
- Domain data uses **immutable records** — no mutable game state in MonoBehaviours
- **DOTS/ECS + Burst/Jobs** for simulation-heavy systems
- **TDD mandatory** — tests written before implementation
- **60 FPS minimum** on mid-range hardware; zero GC allocs in hot loops

## Development Workflow

All non-trivial work follows the Spec-Kit pipeline:

1. `/speckit.specify` — feature specification
2. `/speckit.plan` — implementation plan
3. `/speckit.tasks` — task breakdown
4. `/speckit.implement` — execution

**Git conventions**: feature branches (`feature/<name>`), Conventional Commits, main branch protected.

## License

All rights reserved.
