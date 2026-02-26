<!--
  ========== Sync Impact Report ==========
  Version change: 0.0.0 (unfilled template) → 1.0.0
  Bump rationale: MAJOR — initial ratification of project
    constitution. All principles, standards, and governance
    rules established for the first time.

  Modified principles: N/A (initial ratification)

  Added sections:
    - Project Vision
    - Core Principles (6 principles):
        I.   Functional & Immutable First
        II.  Predictability & Testability
        III. Performance by Default
        IV.  Data-Oriented Design
        V.   Modularity & Extensibility
        VI.  Explicit Over Implicit
    - Technical Standards & Coding Style
        - Functional / Immutable Practices
        - Unity-Specific Architecture
        - Project Structure
        - Naming Conventions
    - Testing & Quality Standards
    - Development Workflow
    - Initial Scope Guardrails (MVP)
    - Governance (amendment procedure, versioning, compliance)

  Removed sections: None (initial creation)

  Templates requiring updates:
    - .specify/templates/plan-template.md:         ✅ No update needed
      (Constitution Check is dynamic, filled at plan time)
    - .specify/templates/spec-template.md:          ✅ No update needed
      (generic structure supports functional data shape callouts)
    - .specify/templates/tasks-template.md:         ✅ No update needed
      (TDD enforcement supported by optional test phases)
    - .specify/templates/checklist-template.md:     ✅ No update needed
      (generic, dynamically generated)
    - .specify/templates/agent-file-template.md:    ✅ No update needed
      (auto-populated from plans)
    - .specify/templates/commands/:                 ✅ N/A (no files)

  Follow-up TODOs:
    - TODO(C#_VERSION): Verify Unity 6 actual C# language level.
      `record struct` requires C# 10+; CLAUDE.md states C# 9.0.
      If C# 9.0 confirmed, restrict to `record` (class) and
      `readonly struct` separately.
    - TODO(PACKAGES): Install required packages not yet present:
        · com.unity.addressables (Addressables)
        · Cysharp/UniTask (via git URL or OpenUPM)
        · DI framework: Zenject or VContainer (via git/OpenUPM)
        · System.Collections.Immutable (via NuGetForUnity or
          manual DLL)
        · com.unity.entities + com.unity.rendering.entities
          (DOTS / Entities Graphics, if not already present)
    - TODO(ROSLYN): Configure Roslyn analyzers for immutability
      enforcement (no public mutable fields on data types).
  ========================================
-->

# VoidHarvest Constitution

**Project Vision**: A relaxing-yet-engaging 3D space mining simulator
where players pilot customizable ships, harvest procedural asteroid
fields, manage resources, upgrade bases, and survive cosmic hazards.
Target: 60 FPS on mid-range PCs, scalable to VR/console later.
Core loop = Explore → Mine → Refine → Expand → Survive.

## Core Principles

### I. Functional & Immutable First

Maximize pure functions, immutable data, and value semantics. State
changes occur ONLY via pure reducers that return new state. Side
effects MUST be isolated to the absolute minimum boundary (Unity
lifecycle hooks, I/O, rendering).

- All domain data types MUST use `record` (or `record struct` once
  C# 10+ is confirmed — see TODO) to enforce value semantics and
  immutability.
- Prefer `readonly struct`, `in` parameters, and
  `return new ...` patterns for all state transitions.
- Collections MUST use `ImmutableArray<T>`,
  `ImmutableDictionary<K,V>`, or `NativeArray`/`BlobAsset` in DOTS
  contexts. No mutable `List<T>` or `Dictionary<K,V>` for domain
  state.
- State management follows the Reducer pattern:
  `(State, Action) → State`. No direct mutation of MonoBehaviour
  fields for game logic.
- Avoid `ref`/`out` and mutable classes for domain data. Use `with`
  expressions for state derivation.
- Optional: LanguageExt (for `Option<T>`/`Result<T>`/`Either<L,R>`)
  or custom lightweight equivalents if bundle size is a concern.

**Rationale**: Deterministic, reproducible game state enables
time-travel debugging, replay systems, networking state sync, and
trivial unit testing of all game logic.

### II. Predictability & Testability

Every core game system MUST be expressible as
`InputState → PureFunction → NewState`. No hidden mutable globals.

- Game logic MUST NOT depend on static mutable state, ambient
  context, or implicit service locators.
- All dependencies MUST be explicitly injected (constructor or
  method parameter).
- Systems MUST produce identical output for identical input,
  enabling deterministic replay and snapshot testing.

**Rationale**: Predictable systems are debuggable systems.
Eliminating hidden state removes entire classes of bugs (race
conditions, order-of-initialization, stale state).

### III. Performance by Default

Use Unity DOTS/ECS + Burst/Jobs for all simulation-heavy logic
(asteroids, mining beams, resource entities, physics interactions).
Hybrid approach ONLY where editor ergonomics demand it.

- 60 FPS minimum on target hardware (mid-range PC, GTX 1060 /
  RX 580 class).
- Zero GC allocations in hot loops. Use `ObjectPool<T>`, Native
  containers, and Burst-compiled jobs.
- Unity Profiler MUST be clean (no warnings, no frame spikes >
  2 ms in gameplay systems) before any feature is marked "Done".
- Batch rendering via Entities Graphics for all procedural asteroid
  fields and resource entities.

**Rationale**: A space mining game with potentially thousands of
asteroids and particles MUST be architecturally performant from
day one. Retrofitting performance is orders of magnitude harder.

### IV. Data-Oriented Design

Data is king. Logic is stateless functions that operate on data.
Prefer composition over inheritance in all cases.

- NO inheritance hierarchies for game entities. Use ECS archetypes
  and component composition.
- ScriptableObjects serve as the single source of truth for all
  static / designer-authored data (ore types, ship stats, recipes).
- Runtime data lives in ECS components (DOTS) or immutable records
  (hybrid layer). NEVER in MonoBehaviour fields for game state.

**Rationale**: Data-oriented design aligns with both the functional
paradigm and CPU cache efficiency. Composition enables emergent
gameplay without brittle class trees.

### V. Modularity & Extensibility

Every system MUST be a self-contained, composable module. Plan for
future modding support (ScriptableObject data + runtime assembly
loading).

- Feature isolation: each major system lives in its own assembly
  definition (`Features/Mining/`, `Features/Ship/`, etc.) with
  explicit dependency declarations.
- Systems communicate via EventBus (UniTask-based reactive streams)
  or DOTS event entities — NEVER via direct field writes or
  `GetComponent` chains across features.
- Public APIs MUST be documented with XML comments referencing
  acceptance criteria from the originating spec.

**Rationale**: Modular architecture enables parallel development,
independent testing, and lays the groundwork for modding support
without requiring architectural rewrites.

### VI. Explicit Over Implicit

No magic. All behavior MUST be traceable from specs → code → tests.

- No hidden convention-based wiring, reflection-based auto-DI, or
  implicit initialization order.
- Every system startup, event subscription, and data flow MUST be
  explicitly visible in code.
- Prefer verbose clarity over clever brevity.

**Rationale**: In a complex simulation, implicit behavior creates
debugging nightmares. Explicit wiring makes the system
comprehensible to any team member at any time.

## Technical Standards & Coding Style

### Functional / Immutable Practices (Enforced)

These practices apply to ALL code outside Unity engine boundaries
(lifecycle hooks, rendering callbacks):

| Practice | Rule |
|----------|------|
| Data types | `record` / `record struct` for all domain data |
| Structs | `readonly struct` with `in` parameters |
| Collections | `ImmutableArray<T>`, `ImmutableDictionary<K,V>`, or `NativeArray`/`BlobAsset` (DOTS) |
| State changes | Reducer pattern: `(State, Action) → State` |
| Mutation | No `ref`/`out` for domain data; use `with` expressions |
| MonoBehaviour | View-layer only; NEVER holds game state |
| Functional helpers | LanguageExt or lightweight custom `Option`/`Result` |

### Unity-Specific Architecture

**Hybrid Architecture**:
- **DOTS/ECS + Entities Graphics**: ALL simulation systems (mining,
  physics, AI, procedural generation, resource processing).
- **GameObject/MonoBehaviour + ScriptableObjects**: UI, player
  input bridging, editor tools, and lightweight view layers.
- Systems communicate via EventBus (UniTask + reactive) or DOTS
  event entities — NEVER direct field writes across systems.

**Asset Strategy**:
- ScriptableObject = single source of truth for all static data.
- Addressables for all runtime-loaded assets. No direct
  `Resources.Load` calls.

**Dependency Injection**:
- Zenject or VContainer for MonoBehaviour-layer DI.
- Pure constructor injection for all non-MonoBehaviour systems.
- NO static singletons for game logic. Service locator pattern
  is prohibited.

### Project Structure

```text
Assets/
├── Features/                # One folder per major system
│   ├── Mining/
│   │   ├── Data/            # ScriptableObjects, records, components
│   │   ├── Systems/         # Pure logic, reducers, ECS systems
│   │   ├── Views/           # MonoBehaviours, UI bindings
│   │   └── Tests/           # Unit + integration tests
│   ├── Ship/
│   ├── Resources/           # Resource / inventory system
│   ├── Procedural/          # Asteroid field generation
│   └── HUD/
├── Core/                    # Shared infrastructure
│   ├── EventBus/
│   ├── State/               # Reducer framework, state store
│   ├── Pools/               # ObjectPool<T> implementations
│   └── Extensions/          # C# extension methods, utilities
├── Settings/                # URP configs, volume profiles
└── Scenes/
```

### Naming Conventions

- **Namespaces**: `VoidHarvest.Features.<System>.<Layer>`
  (e.g., `VoidHarvest.Features.Mining.Systems`).
- **Files**: PascalCase matching type name. One type per file.
- **Records / Data**: Suffix with `Data` or `State`
  (e.g., `AsteroidData`, `InventoryState`).
- **Reducers**: Suffix with `Reducer`
  (e.g., `InventoryReducer`).
- **ECS Systems**: Suffix with `System`
  (e.g., `MiningBeamSystem`).
- **ScriptableObjects**: Suffix with `Config` or `Definition`
  (e.g., `OreTypeDefinition`, `ShipConfig`).

## Testing & Quality Standards

- **Unit Tests** (NUnit + Unity Test Framework): 100% coverage on
  ALL pure reducers, mining logic, resource math, and procedural
  generators. Tests MUST run outside Unity Editor where possible
  (EditMode tests preferred over PlayMode for pure logic).
- **Integration / PlayMode Tests**: Cover hybrid system boundaries
  and UI flows. Required for any system that bridges DOTS and
  MonoBehaviour layers.
- **TDD Workflow** (NON-NEGOTIABLE): For every new feature:
  1. Tests are written FIRST.
  2. Tests MUST fail (Red).
  3. Implementation makes tests pass (Green).
  4. Refactor while keeping tests green.
- **Static Analysis**: Roslyn analyzers + Unity built-in analysis.
  Custom rules enforcing immutability (e.g., no public mutable
  fields on data types). Configure as errors, not warnings.

## Development Workflow

- All non-trivial work starts with a spec (`/speckit.specify`).
- Specs MUST explicitly call out functional transformations and
  immutable data shapes for any game logic.
- Implementation follows:
  `/speckit.plan` → `/speckit.tasks` → `/speckit.implement`.
- Every PR links to its spec + test results.
- **Git conventions**:
  - Feature branches: `feature/<kebab-case-spec-name>`.
  - Conventional Commits (e.g., `feat:`, `fix:`, `refactor:`).
  - Main branch is protected. No direct pushes.
- **Review gate**: Human (project lead) always does final
  architectural sign-off, especially on any immutability
  trade-offs or DOTS/hybrid boundary decisions.

## Initial Scope Guardrails (MVP)

The MVP scope is strictly limited to validate the core loop:

1. **Ship movement** — 6DOF flight with inertia in an asteroid
   field (functional state, DOTS physics).
2. **Mining** — Target asteroid → mining beam → resource extraction
   (pure reducer for yield calculation).
3. **Resource inventory** — Immutable inventory state with
   add/remove/query operations.
4. **Procedural asteroid field** — Small-scale procedural generation
   (< 500 asteroids) via Burst jobs.
5. **Simple HUD** — Resource counts, ship status, mining target
   info.

**Explicitly OUT of scope for MVP**:
- Multiplayer / networking
- Save / load system
- Base building or station management
- Trading / economy
- Complex AI / enemies
- Sound design (placeholder only)

These features MUST NOT be started until the MVP is rock-solid,
fully functional/immutable, and passing all tests at 60 FPS.

## Governance

This constitution is the supreme authority for all architectural
and process decisions in the VoidHarvest project. It supersedes
all other practices, conventions, or preferences.

### Amendment Procedure

1. Any deviation from functional/immutable style MUST be:
   - Explicitly justified in the relevant spec (performance
     constraint, Unity engine limitation, etc.).
   - Documented with a `// CONSTITUTION DEVIATION:` comment block
     in code.
   - Reviewed and approved by the project lead.
2. Constitution amendments require:
   - A version bump following semantic versioning (see below).
   - Approval from both team members (project lead + AI agent).
   - Update via `/speckit.constitution` command.

### Versioning Policy

- **MAJOR** (X.0.0): Backward-incompatible principle removals or
  fundamental redefinitions.
- **MINOR** (0.X.0): New principles added, sections materially
  expanded, or significant new guidance.
- **PATCH** (0.0.X): Clarifications, wording improvements, typo
  fixes, non-semantic refinements.

### Compliance Review

- All PRs MUST be verified against active constitution principles
  before merge.
- The plan-template Constitution Check section MUST gate every
  feature plan.
- Per-milestone constitution review to incorporate lessons learned.

**Version**: 1.0.0 | **Ratified**: 2026-02-26 | **Last Amended**: 2026-02-26
