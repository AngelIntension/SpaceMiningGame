# VoidHarvest

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

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
│   ├── Camera/              # 3rd-person orbiting follow camera, skybox
│   ├── Input/               # EVE-style controls, PilotCommand
│   ├── Ship/                # Ship state, physics, modules, 3 archetypes
│   ├── Fleet/               # Multi-ship ownership, swapping
│   ├── Mining/              # Beam targeting, yield, depletion visuals
│   ├── Resources/           # Resource / inventory system
│   ├── Procedural/          # Asteroid field generation, visual mapping
│   ├── HUD/                 # In-game UI, radial menus, hotbar
│   ├── Docking/             # Station docking state machine, snap, events
│   ├── StationServices/     # Refining, selling, repair, cargo transfer
│   ├── Targeting/           # Multi-target lock, reticle, target cards
│   ├── Base/                # Station presets and prefabs
│   ├── TechTree/            # Research / progression
│   ├── Economy/             # Market simulation
│   └── Tests/               # Cross-feature integration tests
├── Core/                    # Shared infrastructure
│   ├── Editor/              # Editor utilities
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
| **0 — MVP** | 3rd-person camera, EVE-style controls, 6DOF ship physics, mining, procedural asteroids, HUD, docking, station services, targeting | **Complete** |
| **1** | Ship fleet swapping, basic tech tree (3-4 tiers) | Planned |
| **2** | Hauling roles, outpost/base building | Planned |
| **3** | Dynamic economy simulation, deep base customization, multi-ship fleet management | Planned |

## What's Implemented (Phase 0 MVP)

- **Ship flight** — 6DOF physics with inertia, flight modes, Burst-compiled math; 3 ship archetypes (Starter, Medium, Heavy Mining Barge)
- **EVE-style controls** — mouse targeting, double-click align, radial context menus, 8-slot hotbar, keyboard thrust/strafe/roll
- **3rd-person camera** — orbiting follow camera with zoom (Cinemachine), dynamic nebula skybox with rotation
- **Data-driven ore system** — OreDefinition ScriptableObjects (Luminite, Ferrox, Auralite) with configurable yield, hardness, rarity, beam colors, and cargo volume; add new ores with zero code changes
- **Mining** — beam targeting, yield calculation, asteroid depletion with scale/destroy visuals, ore tint colors (ECS systems)
- **Configurable asteroid fields** — AsteroidFieldDefinition ScriptableObjects with per-field ore weights, visual mapping, and asteroid parameters; designers create distinct asteroid belts via Unity Inspector
- **Procedural asteroid field** — Burst-compiled job-based generation with ore-to-mesh visual mapping, multi-mesh premium asteroid variants
- **Resource inventory** — immutable state with pure reducers
- **HUD** — target info panel, warnings, selection outlines, radial menus
- **Station docking** — approach, magnetic snap, station services menu, undock sequence; 2 station presets (Small Mining Relay, Medium Refinery Hub)
- **Station services** — ore refining with deterministic yield, resource selling with credits, cargo transfer between ship and station, basic hull repair; configurable per-station via ScriptableObjects
- **In-flight targeting** — multi-target lock system with configurable lock time and max locks; screen-space reticle with corner brackets, off-screen directional indicator, lock acquisition progress ring; target card panel with live RenderTexture viewports from ship perspective, dismiss/re-select; radial menu integration for lock actions
- **ScriptableObject validation** — all config assets have OnValidate() guards with 50+ dedicated validation tests
- **Core infrastructure** — EventBus (UniTask), State Store, VContainer DI throughout (no FindObjectOfType); 225+ C# files, 520+ tests, 33 assembly definitions

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

This project is licensed under the [MIT License](LICENSE).
