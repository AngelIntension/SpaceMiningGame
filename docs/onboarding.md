# Onboarding Guide

## Welcome

Welcome to **VoidHarvest** -- a 3D space mining simulator inspired by EVE Online, built on Unity 6 (6000.3.10f1) with a hybrid DOTS/ECS + MonoBehaviour architecture. Players pilot ships in third-person, mine procedural asteroid fields, manage immutable resource inventories, research tech trees, dock at stations for refining and trading, and participate in a dynamic economy.

The codebase enforces a strict functional/immutable-first paradigm: all game state flows through pure reducers, domain data uses C# 9 `record` types and `ImmutableArray<T>`, and side effects are isolated to Unity lifecycle hooks. This guide will walk you through the documentation in the order that builds understanding most efficiently.

---

## For Developers

Follow this reading order. Each document builds on concepts introduced by the previous ones.

### 1. Learn the language

**[Glossary](glossary.md)** -- Standardized project terminology. Read this first so that terms like "reducer", "PilotCommand", "BlobAsset", and "OreDefinition" are familiar before you encounter them in architecture docs.

### 2. Understand the big picture

**[Architecture Overview](architecture/overview.md)** -- High-level diagram of the hybrid DOTS/MonoBehaviour architecture, all 12 Phase 0 features, communication patterns (EventBus, ECS event entities), and the input-to-render data flow.

### 3. Learn state management

**[State Management](architecture/state-management.md)** -- The pure reducer pattern (`(State, Action) -> State`), the `GameState` tree shape, action dispatch flow, and how `CompositeReducer` handles cross-cutting operations.

### 4. Understand the event system

**[Event System](architecture/event-system.md)** -- All 26 events across the codebase, publisher/subscriber mappings, the async subscription convention (`OnEnable` subscribe, `OnDisable` cancel, `OnDestroy` safety net), and the UniTask reactive EventBus.

### 5. Trace the data pipeline

**[Data Pipeline](architecture/data-pipeline.md)** -- The full lifecycle from ScriptableObject authoring through Baker conversion to BlobAsset creation, ECS System consumption, and MonoBehaviour View rendering. This is the backbone of the data-driven design.

### 6. Learn dependency injection

**[Dependency Injection](architecture/dependency-injection.md)** -- VContainer scopes (`RootLifetimeScope`, `SceneLifetimeScope`), registration patterns, constructor injection for non-MonoBehaviour systems, and how MonoBehaviours receive dependencies.

### 7. Map the module boundaries

**[Assembly Map](assembly-map.md)** -- All assembly definitions, their dependency layers, and what references what. Essential reading before adding `using` directives or creating new assemblies.

### 8. Read your feature area

Dive into the system doc for the feature you will be working on. Each follows a consistent 10-section structure (Purpose, Architecture Diagram, State Shape, Actions, SO Configs, ECS Components, Events, Assembly Dependencies, Key Types, Designer Notes).

| Category | System Docs |
|----------|-------------|
| Player Control | [Camera](systems/camera.md) | [Input](systems/input.md) | [Ship](systems/ship.md) |
| Core Gameplay | [Mining](systems/mining.md) | [Procedural](systems/procedural.md) | [Resources](systems/resources.md) |
| UI | [HUD](systems/hud.md) |
| Station | [Docking](systems/docking.md) | [Station Services](systems/station-services.md) | [Station](systems/station.md) |
| Combat / Utility | [Targeting](systems/targeting.md) |
| World | [World](systems/world.md) |

### 9. Know the gotchas

**[Troubleshooting](troubleshooting.md)** -- Must-read before writing any ECS or View code. Covers known pitfalls with per-instance material property overrides, FBX mesh scale, stale ECS positions, async subscription lifecycle, and more.

---

## For Designers (Non-Programmers)

These guides are written without code. They focus on Unity Editor workflows, asset paths, inspector fields, and step-by-step instructions.

### 1. Start here -- learn the configuration assets

**[Configuration Asset Catalog](designer-guide/scriptable-objects.md)** -- Complete catalog of all ScriptableObject configuration assets in the project. Lists every asset with its file path, inspector fields, valid ranges, and defaults. This is your reference for understanding what you can tune.

### 2. Add new content

- **[Adding Ores](designer-guide/adding-ores.md)** -- Step-by-step guide to creating a new ore type with zero code changes. Covers creating an `OreDefinition` asset, setting rarity/yield/hardness/colors, adding refining outputs, and registering it in an `AsteroidFieldDefinition`.
- **[Adding Stations](designer-guide/adding-stations.md)** -- Step-by-step guide to creating a new station type. Covers `StationDefinition`, `StationPresetConfig`, `StationServicesConfig`, docking parameters, and wiring it into a `WorldDefinition`.

### 3. Tune gameplay parameters

**[Tuning Reference](designer-guide/tuning-reference.md)** -- Quick-reference table of every tunable parameter in the game, organized by system. Includes field names, which ScriptableObject holds them, default values, and suggested ranges for balancing.

---

## Project Governance

These documents define the rules and conventions the project operates under.

- **[Constitution](../.specify/memory/constitution.md)** -- The authoritative source for all architectural decisions, coding standards, and development workflow requirements. Read this when you need to understand *why* a pattern is enforced.
- **[CLAUDE.md](../CLAUDE.md)** -- AI assistant instructions and project conventions. Also serves as a concise project summary with architecture notes, naming conventions, and package versions.
- **Spec-Kit Pipeline** -- All non-trivial feature work follows a four-stage pipeline: `specify` (feature specification) -> `plan` (implementation plan with Constitution Check) -> `tasks` (task breakdown) -> `implement` (execution). Specs live in `.specify/specs/`.

---

## Quick Reference

All 25 documentation files organized by category.

### Architecture (5 docs)

| Doc | Description |
|-----|-------------|
| [Architecture Overview](architecture/overview.md) | High-level system diagram, feature map, communication patterns |
| [State Management](architecture/state-management.md) | Reducer pattern, GameState tree, action dispatch |
| [Event System](architecture/event-system.md) | 26 events, pub/sub map, async subscription convention |
| [Data Pipeline](architecture/data-pipeline.md) | SO -> Baker -> BlobAsset -> ECS -> View lifecycle |
| [Dependency Injection](architecture/dependency-injection.md) | VContainer scopes, registration, injection conventions |

### Systems (12 docs)

| Doc | Feature Area |
|-----|-------------|
| [Camera](systems/camera.md) | 3rd-person orbiting follow camera, skybox, CameraConfig |
| [Input](systems/input.md) | EVE-style controls, PilotCommand, InteractionConfig |
| [Ship](systems/ship.md) | Ship state, 6DOF physics, modules, 3 archetypes |
| [Mining](systems/mining.md) | Beam targeting, yield calculation, depletion visuals |
| [Procedural](systems/procedural.md) | Asteroid field generation, visual mapping, spawner |
| [Resources](systems/resources.md) | Immutable resource inventory, cargo slots |
| [HUD](systems/hud.md) | In-game UI, radial menus, hotbar, target info |
| [Docking](systems/docking.md) | Station docking state machine, snap, approach/undock |
| [Station Services](systems/station-services.md) | Refining, selling, repair, cargo transfer |
| [Targeting](systems/targeting.md) | Multi-target lock, reticle, lock acquisition, target cards |
| [Station](systems/station.md) | StationDefinition, StationPresetConfig, StationType |
| [World](systems/world.md) | WorldDefinition, world initialization, station population |

### Designer Guide (4 docs)

| Doc | Audience |
|-----|----------|
| [Configuration Asset Catalog](designer-guide/scriptable-objects.md) | All ScriptableObject assets with field descriptions |
| [Adding Ores](designer-guide/adding-ores.md) | Step-by-step new ore creation (no code) |
| [Adding Stations](designer-guide/adding-stations.md) | Step-by-step new station creation (no code) |
| [Tuning Reference](designer-guide/tuning-reference.md) | Every tunable parameter, organized by system |

### Supporting (4 docs)

| Doc | Description |
|-----|-------------|
| [Glossary](glossary.md) | Standardized project terminology |
| [Assembly Map](assembly-map.md) | Assembly dependency graph and layer rules |
| [Troubleshooting](troubleshooting.md) | Common pitfalls, known issues, solutions |
| [Onboarding](onboarding.md) | This document -- recommended reading order |
