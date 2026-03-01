<!--
  ========== Sync Impact Report ==========
  Version change: 1.2.0 → 1.3.0
  Bump rationale: MINOR — new mandatory Player Documentation section
    added under Development Workflow. Requires a HOWTOPLAY.md file at
    the project root to be maintained as a living document, updated
    whenever player-facing features are added or changed.

  Modified principles: None renamed or removed.

  Modified sections:
    - Development Workflow: Added "Player Documentation" subsection
      mandating HOWTOPLAY.md maintenance.

  Added sections:
    - Player Documentation (new subsection under Development Workflow):
        · HOWTOPLAY.md file requirement at project root
        · Mandatory update gate for player-facing feature changes
        · Required content structure (controls, mechanics, UI)
        · Version-aligned with development roadmap phases

  Removed sections: None

  Templates requiring updates:
    - .specify/templates/plan-template.md:         ✅ No update needed
      (Constitution Check is dynamic; player docs gate applies at
      implementation time, not plan time)
    - .specify/templates/spec-template.md:          ✅ No update needed
      (specs define features; player docs are a delivery artifact)
    - .specify/templates/tasks-template.md:         ✅ No update needed
      (task structure unchanged; player docs update is a
      cross-cutting workflow concern, not a task type — the
      existing "Documentation updates" sample task in the
      Polish phase already covers this pattern)
    - .specify/templates/checklist-template.md:     ✅ No update needed
    - .specify/templates/agent-file-template.md:    ✅ No update needed
    - .specify/templates/commands/:                 ✅ N/A (no files)

  Carried-forward TODOs: None.

  New TODOs (v1.3.0):
    - Create initial HOWTOPLAY.md at project root covering Phase 0
      features (ship controls, mining, inventory, HUD, station
      docking). This is a one-time bootstrap task.
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

- All domain data types MUST use `sealed record` for reference types
  and `readonly struct` for value types (C# 9.0) to enforce value semantics and
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

## Game Vision Pillars

These pillars define the game's identity and constrain all design
decisions. Every feature spec MUST demonstrate alignment with the
relevant pillars below.

### Perspective & Camera

Strictly 3rd-person. Smooth orbiting follow camera centered on the
active ship. Cinematic, relaxing feel with speed-based zoom and
optional free-look toggle.

- Camera MUST orbit the active ship; no 1st-person or top-down
  modes in the core experience.
- Zoom level MUST respond dynamically to ship velocity (pull back
  at high speed, close in when stationary/mining).
- Free-look toggle MUST NOT affect ship heading or flight path.

### Controls

Heavily inspired by EVE Online — mouse-driven targeting,
double-click / click-to-align in 3D space, radial context menus
(Approach, Orbit, Mine, Keep-at-Range), hotbar modules.

- **Mouse**: Left-click select target, double-click to align/fly
  toward point in 3D space, right-click for radial context menu.
- **Keyboard thrust** (power-user layer): W/S accelerate/brake,
  A/D strafe, Q/E roll. These are supplements, not primary.
- **Hotbar**: Module activation slots (mining lasers, shields,
  scanners) bound to number keys or custom bindings.
- All input MUST flow through an immutable `PilotCommand` record
  into a pure `ShipStateReducer`. No direct state mutation from
  input handlers.

### Ship Fleet System

Player owns multiple specialized ships (Mining Barge, Hauler,
Combat Scout, etc.). Instant swap at outposts/stations or via
recall beacon.

- Ships are immutable data records:
  `ShipArchetype + CurrentModules + ShipStats`.
- Ship swapping MUST be a pure state reducer operation
  (`(FleetState, SwapAction) → FleetState`).
- Active ship selection determines camera target and available
  module hotbar.

### Progression

Deep, branching research/tech tree unlocking better hulls, mining
lasers, refineries, base modules, and economic multipliers.

- Tech nodes MUST be immutable graph data (DAG structure).
- Unlocking a node MUST be a pure reducer:
  `(TechTreeState, UnlockAction) → TechTreeState`.
- Tech tree data MUST be authored via ScriptableObjects for
  designer iteration.

### Core Loop Satisfaction

Explore procedural asteroid fields → precision EVE-style mining →
haul/refine → research/upgrade fleet → expand personal empire via
base building.

- Each loop stage MUST deliver tactile feedback (visual, audio,
  UI confirmation) within 2 seconds of player action.
- Mining MUST feel precise and deliberate, not passive — beam
  targeting, yield variance based on technique, asteroid depletion
  visuals.

### Endgame

Player-built bases in asteroid belts or Lagrange points + fully
simulated economy (NPC + player-driven prices based on real-time
supply/demand of ores, refined goods, modules).

- Economy simulation MUST be a pure system operating on immutable
  `MarketState` records with deterministic price resolution.
- Base placement MUST persist as immutable positional data within
  the world state.

## Technical Standards & Coding Style

### Functional / Immutable Practices (Enforced)

These practices apply to ALL code outside Unity engine boundaries
(lifecycle hooks, rendering callbacks):

| Practice | Rule |
|----------|------|
| Data types | `sealed record` / `readonly struct` for all domain data |
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

### Editor Automation (Unity MCP)

The Unity MCP bridge provides programmatic access to the Unity
Editor for automated verification, testing, and scene management.
The following gates and practices are MANDATORY when Unity MCP is
available during development.

**Compilation Verification Gate**:
- After ANY script creation or modification, the Unity console
  MUST be checked for compilation errors before proceeding with
  further work. This is a hard gate — no new scripts, components,
  or scene modifications may proceed until the console is clean.
- Poll the editor state's `isCompiling` field to confirm domain
  reload is complete before reading console output.

**Console Monitoring**:
- The Unity console MUST be monitored after each significant
  development action (script edits, asset imports, scene changes).
- Console errors are blocking — they MUST be resolved before
  moving to the next task.
- Console warnings SHOULD be investigated and resolved unless
  they originate from third-party packages outside project control.

**Scene & Asset Validation**:
- After structural scene changes (adding/removing GameObjects,
  reparenting, component configuration), the scene hierarchy
  MUST be verified via MCP to confirm the intended structure.
- Screenshots MAY be captured via MCP for visual verification
  of rendering changes, material assignments, and UI layout.

**Script Management**:
- Scripts created or edited via MCP tools MUST be validated
  (`validate_script`) before relying on new types or components.
- Prefer MCP structured edit operations (`script_apply_edits`)
  over raw text edits for method-level changes, as they provide
  safer boundary handling.

**Test Execution**:
- EditMode and PlayMode tests MUST be runnable via MCP
  (`run_tests` / `get_test_job`) as part of the development loop.
- Test results MUST be checked programmatically — a passing
  test suite is a prerequisite for marking any task complete.
- Failed tests MUST be reported with details (use
  `include_failed_tests` flag) for diagnosis.

**Rationale**: Automated editor verification via MCP eliminates
the class of bugs where code compiles in an IDE but fails in Unity
(assembly definition mismatches, missing references, serialization
errors). Programmatic test execution reinforces TDD discipline by
making the Red-Green-Refactor cycle executable without manual
editor interaction.

### Project Structure

```text
Assets/
├── Features/                # One folder per major system
│   ├── Camera/              # 3rd-person orbiting follow camera
│   ├── Input/               # EVE-style controls, PilotCommand
│   ├── Ship/                # Ship state, physics, modules
│   ├── Fleet/               # Multi-ship ownership, swapping
│   ├── Mining/              # Beam targeting, yield reducers
│   │   ├── Data/            # ScriptableObjects, records, components
│   │   ├── Systems/         # Pure logic, reducers, ECS systems
│   │   ├── Views/           # MonoBehaviours, UI bindings
│   │   └── Tests/           # Unit + integration tests
│   ├── Resources/           # Resource / inventory system
│   ├── Procedural/          # Asteroid field generation
│   ├── HUD/                 # In-game UI, radial menus, hotbar
│   ├── TechTree/            # Research/progression (Phase 1+)
│   ├── Economy/             # Market simulation (Phase 3+)
│   └── Base/                # Base building (Phase 2+)
├── Core/                    # Shared infrastructure
│   ├── EventBus/
│   ├── State/               # Reducer framework, state store
│   ├── Pools/               # ObjectPool<T> implementations
│   └── Extensions/          # C# extension methods, utilities
├── Settings/                # URP configs, volume profiles
└── Scenes/
```

Each feature folder follows the `Data/`, `Systems/`, `Views/`,
`Tests/` sub-structure shown under Mining above.

### Naming Conventions

- **Namespaces**: `VoidHarvest.Features.<System>.<Layer>`
  (e.g., `VoidHarvest.Features.Mining.Systems`).
- **Files**: PascalCase matching type name. One type per file.
- **Records / Data**: Suffix with `Data` or `State`
  (e.g., `AsteroidData`, `InventoryState`).
- **Reducers**: Suffix with `Reducer`
  (e.g., `InventoryReducer`, `ShipStateReducer`).
- **ECS Systems**: Suffix with `System`
  (e.g., `MiningBeamSystem`).
- **ScriptableObjects**: Suffix with `Config` or `Definition`
  (e.g., `OreTypeDefinition`, `ShipConfig`).
- **Commands / Actions**: Suffix with `Command` or `Action`
  (e.g., `PilotCommand`, `SwapShipAction`, `UnlockTechAction`).

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
- **Automated Test Execution**: When Unity MCP is available, tests
  MUST be executed programmatically via MCP after each
  implementation task. Manual Test Runner usage is acceptable as
  a fallback but not preferred.
- **Console-Clean Gate**: The Unity console MUST report zero errors
  before any test execution or task completion. Warnings from
  project code MUST also be resolved.
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
- **Unity MCP verification loop**: During `/speckit.implement`,
  each task that creates or modifies scripts MUST follow this
  sequence:
  1. Create/edit script(s).
  2. Wait for compilation (poll `isCompiling`).
  3. Check console for errors — resolve before continuing.
  4. Run relevant tests via MCP.
  5. Verify scene integrity if scene was modified.
  6. Mark task complete only when console is clean and tests pass.
- **Git conventions**:
  - Feature branches: `feature/<kebab-case-spec-name>`.
  - Conventional Commits (e.g., `feat:`, `fix:`, `refactor:`).
  - Main branch is protected. No direct pushes.
- **Review gate**: Human (project lead) always does final
  architectural sign-off, especially on any immutability
  trade-offs or DOTS/hybrid boundary decisions.

### Player Documentation

A `HOWTOPLAY.md` file MUST exist at the project root and serve as
the authoritative player-facing instructions for the game. This is
a living document that evolves alongside the game.

**Maintenance Rules**:
- `HOWTOPLAY.md` MUST be created before the first playable build
  is delivered and MUST remain up-to-date thereafter.
- Any feature that adds, removes, or changes player-facing behavior
  (controls, mechanics, UI interactions, game systems) MUST include
  a corresponding update to `HOWTOPLAY.md` as part of the same PR.
  This is a delivery gate — a feature is not complete until the
  player instructions reflect it.
- Stale or missing instructions for shipped features are treated as
  bugs and MUST be resolved with the same priority as functional
  defects.

**Required Content** (sections MUST exist once the relevant feature
ships):
- **Getting Started** — How to launch and begin playing.
- **Controls** — Complete control reference (mouse, keyboard,
  hotbar) matching the current input bindings.
- **Ship Piloting** — Movement, alignment, thrust, camera controls.
- **Mining** — How to target asteroids, activate mining beams,
  collect resources.
- **Inventory & Resources** — How to view and manage collected
  resources.
- **HUD & UI** — Explanation of all HUD elements, radial menus,
  and hotbar usage.
- **Station Docking** — How to approach, dock, undock, and access
  station services.
- Additional sections MUST be added as new systems ship (fleet
  management, tech tree, base building, economy, etc.).

**Style Guidelines**:
- Written for players, not developers — no code references,
  internal type names, or architecture jargon.
- Use clear, concise language. Prefer short paragraphs and bullet
  lists over dense prose.
- Include default key bindings in a reference table format.
- MAY include ASCII diagrams or references to in-game screenshots
  where they aid comprehension.

**Rationale**: Players cannot enjoy a game they do not understand.
Maintaining player instructions as a first-class project artifact
ensures discoverability of features, reduces onboarding friction,
and forces developers to articulate gameplay from the player's
perspective — often surfacing UX issues in the process.

## Initial Scope Guardrails (MVP)

The MVP (Phase 0) scope is strictly limited to validate the core
loop with the EVE-style 3rd-person experience:

1. **3rd-person camera** — Orbiting follow camera with speed-based
   zoom on the active ship.
2. **EVE-style controls** — Mouse targeting, click-to-align,
   radial context menus, keyboard thrust supplement. All input →
   immutable `PilotCommand` → pure `ShipStateReducer`.
3. **Ship movement** — 6DOF flight with inertia in an asteroid
   field (functional state, DOTS physics).
4. **Mining** — Target asteroid → mining beam → resource extraction
   (pure reducer for yield calculation).
5. **Resource inventory** — Immutable inventory state with
   add/remove/query operations.
6. **Procedural asteroid field** — Small-scale procedural generation
   (< 500 asteroids) via Burst jobs.
7. **Simple HUD** — Resource counts, ship status, mining target
   info, radial context menu, module hotbar.

**Explicitly OUT of scope for MVP (Phase 0)**:
- Ship fleet swapping (Phase 1)
- Tech tree / research (Phase 1)
- Refining / hauling roles (Phase 2)
- Base building (Phase 2)
- Dynamic economy simulation (Phase 3)
- Multiplayer / networking
- Save / load system
- Complex AI / enemies
- Sound design (placeholder only)

These features MUST NOT be started until the MVP is rock-solid,
fully functional/immutable, and passing all tests at 60 FPS.

## Development Roadmap

All phases MUST preserve the functional/immutable core. No phase
may introduce mutable game state patterns without a constitution
deviation approval.

### Phase 0 — MVP (Current)

3rd-person EVE-style controls + basic mining loop + immutable
resource inventory + small procedural asteroid field + simple HUD.

**Exit criteria**: Core loop playable end-to-end at 60 FPS with
100% reducer test coverage. Player can fly, target, mine, and
see resources accumulate.

### Phase 1 — Fleet & Progression

Ship swapping between specialized hulls (Mining Barge, Hauler,
Combat Scout). Basic tech tree with 3–4 tiers unlocking hulls,
modules, and mining efficiency upgrades.

**Exit criteria**: Player can own multiple ships, swap at stations,
and unlock tech nodes. All fleet/tech state managed via pure
reducers.

### Phase 2 — Refining & Bases

Refining mechanics (ore → processed materials), hauling roles
with cargo capacity constraints, outpost/base building stub
(place, store, basic functionality).

**Exit criteria**: Full resource pipeline from mining → refining →
storage. Base placement functional with immutable world state.

### Phase 3 — Economy & Endgame

Fully simulated dynamic economy (NPC + player-driven supply/demand
pricing). Deep base customization. Multi-ship fleet management
with simultaneous NPC crew operations.

**Exit criteria**: Market prices respond deterministically to
supply/demand. Bases are fully customizable. Fleet operations
run as autonomous ECS systems.

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

**Version**: 1.3.0 | **Ratified**: 2026-02-26 | **Last Amended**: 2026-03-01
