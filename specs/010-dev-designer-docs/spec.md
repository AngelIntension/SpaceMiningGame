# Feature Specification: Developer & Designer Documentation Bootstrap

**Feature Branch**: `010-dev-designer-docs`
**Created**: 2026-03-03
**Status**: Draft
**Input**: User description: "Create initial developer and designer documentation per Constitution v1.4.0"

## User Scenarios & Testing

### User Story 1 - Architecture Documentation for Developers (Priority: P1)

A new developer joining the VoidHarvest project needs to understand the high-level architecture, state management approach, event system, dependency injection setup, and data pipeline before they can contribute code. They open the `docs/architecture/` directory and find five cross-cutting documents with Mermaid diagrams that explain how all systems fit together, how state flows through reducers, how events are published and consumed, how VContainer scopes are organized, and how ScriptableObjects are baked into ECS BlobAssets.

**Why this priority**: Without architecture docs, every onboarding requires synchronous knowledge transfer from existing contributors. This is the highest-leverage documentation to create first because it provides the conceptual framework for understanding everything else.

**Independent Test**: Can be validated by confirming all five architecture docs exist, each contains at least one Mermaid diagram, and each addresses the required content areas specified in the constitution.

**Acceptance Scenarios**:

1. **Given** a `docs/architecture/` directory does not exist, **When** the documentation is created, **Then** all five files exist: `overview.md`, `state-management.md`, `event-system.md`, `dependency-injection.md`, `data-pipeline.md`.
2. **Given** a developer reads `overview.md`, **When** they look for a system-level diagram, **Then** they find a Mermaid diagram showing all features, their communication paths (EventBus, DI wiring, ECS sync), and the hybrid DOTS/MonoBehaviour boundary.
3. **Given** a developer reads `state-management.md`, **When** they look for the state tree, **Then** they find a GameState tree diagram, reducer composition visualization, and action dispatch flow — all in Mermaid syntax.
4. **Given** a developer reads `event-system.md`, **When** they look for the event catalog, **Then** they find all 25 event types with publisher and subscriber mappings.
5. **Given** a developer reads `dependency-injection.md`, **When** they look for scope hierarchy, **Then** they find VContainer scope documentation, registration patterns, and the async subscription convention.
6. **Given** a developer reads `data-pipeline.md`, **When** they look for the baking pipeline, **Then** they find documentation covering SO authoring through Baker to BlobAsset to ECS System to View.

---

### User Story 2 - Per-System Documentation for Developers (Priority: P1)

A developer tasked with modifying or extending a specific feature (e.g., Mining, Docking, Targeting) needs to quickly understand that system's state shape, actions, ECS components, events, ScriptableObject configs, assembly dependencies, and key types. They navigate to `docs/systems/<feature>.md` and find a complete reference with Mermaid diagrams covering data flow.

**Why this priority**: Per-system docs are essential for any developer working on or near a specific feature. Without them, developers must reverse-engineer the codebase for every change. Tied with P1 because architecture docs provide context while system docs provide detail — both are needed.

**Independent Test**: Can be validated by confirming all 12 system docs exist with all 10 required sections per the constitution, each containing at least one Mermaid diagram.

**Acceptance Scenarios**:

1. **Given** the `docs/systems/` directory does not exist, **When** documentation is created, **Then** 12 files exist: `camera.md`, `input.md`, `ship.md`, `mining.md`, `procedural.md`, `resources.md`, `hud.md`, `docking.md`, `station-services.md`, `targeting.md`, `station.md`, `world.md`.
2. **Given** a developer opens any system doc, **When** they check for required sections, **Then** all 10 mandatory sections are present: Purpose, Architecture Diagram, State Shape, Actions, ScriptableObject Configs, ECS Components, Events, Assembly Dependencies, Key Types, Designer Notes.
3. **Given** a developer opens `mining.md`, **When** they look for ECS components, **Then** they find a table listing all Mining-related IComponentData types, blob assets, and singletons with descriptions.
4. **Given** a developer opens any system doc, **When** they look for a diagram, **Then** they find at least one Mermaid diagram showing data flow for that system.

---

### User Story 3 - Designer Guide for Non-Programmers (Priority: P2)

A game designer (non-programmer) wants to add a new ore type, configure a new station, or tune gameplay parameters without writing any code. They open the `docs/designer-guide/` directory and find plain-language guides with asset paths, field descriptions, and step-by-step workflows that explain how to accomplish these tasks entirely within the Unity Editor.

**Why this priority**: Designer guides multiply content creation velocity by enabling non-programmers to extend the game independently. Lower priority than developer docs because the project currently has a developer-first contributor base, but essential for scaling content creation.

**Independent Test**: Can be validated by confirming all four designer guide docs exist, contain no code samples or namespace references, and provide step-by-step workflows with field descriptions.

**Acceptance Scenarios**:

1. **Given** the `docs/designer-guide/` directory does not exist, **When** documentation is created, **Then** four files exist: `scriptable-objects.md`, `adding-ores.md`, `adding-stations.md`, `tuning-reference.md`.
2. **Given** a designer reads `scriptable-objects.md`, **When** they look for information about a specific asset type, **Then** they find the Create menu path, all configurable fields, valid ranges and defaults, and which game systems use the asset.
3. **Given** a designer reads `adding-ores.md`, **When** they follow the step-by-step instructions, **Then** they can create a new ore type with zero code changes — only Unity Editor asset creation and configuration.
4. **Given** a designer reads `adding-stations.md`, **When** they follow the step-by-step instructions, **Then** they can create a new station type with zero code changes.
5. **Given** a designer reads `tuning-reference.md`, **When** they look for a specific parameter, **Then** they find a consolidated table of all designer-tunable parameters with valid ranges and defaults.
6. **Given** a non-programmer reads any file in `designer-guide/`, **When** they scan for code, **Then** they find no C# code samples, no namespace references, and no architectural jargon.

---

### User Story 4 - Supporting Documentation (Priority: P2)

A new team member (developer or designer) needs quick access to standardized terminology, common troubleshooting solutions, a recommended reading order, and a visual map of assembly dependencies. They find `glossary.md`, `troubleshooting.md`, `onboarding.md`, and `assembly-map.md` at the root of the `docs/` directory.

**Why this priority**: Supporting docs reduce friction for onboarding and daily development. They are reference material that complements the architecture and system docs rather than standing alone.

**Independent Test**: Can be validated by confirming all four supporting docs exist with required content: glossary defines all project-specific terms, troubleshooting covers known pitfalls, onboarding provides reading order, assembly-map includes a Mermaid dependency diagram.

**Acceptance Scenarios**:

1. **Given** the `docs/` directory exists but lacks supporting docs, **When** documentation is created, **Then** four files exist at the root: `glossary.md`, `troubleshooting.md`, `onboarding.md`, `assembly-map.md`.
2. **Given** a new developer reads `glossary.md`, **When** they encounter a project term (e.g., "Reducer", "BlobAsset", "Authoring", "Baking", "EventBus"), **Then** they find a clear definition.
3. **Given** a developer hits a known issue (e.g., per-instance material property overrides not working, FBX mesh scale issues, stale ECS ship position), **When** they check `troubleshooting.md`, **Then** they find the problem documented with its solution.
4. **Given** a new developer reads `onboarding.md`, **When** they look for guidance, **Then** they find a recommended reading order through the documentation set.
5. **Given** a developer reads `assembly-map.md`, **When** they look for the dependency graph, **Then** they find a Mermaid diagram showing all assembly definitions and their dependency relationships.

---

### Edge Cases

- What happens when a feature is skeleton-only (Fleet, TechTree, Economy)? System docs for skeleton features are not created. Their existence is noted in `overview.md` as future phases.
- What happens when a system has no ECS components (e.g., Resources, HUD)? The ECS Components section states "None — this system operates entirely in the managed/MonoBehaviour layer" rather than being omitted.
- What happens when a system has no events? The Events section states "None — this system does not publish or subscribe to events" rather than being omitted.
- How are cross-cutting concerns documented (e.g., CompositeReducer actions that span multiple state slices)? Cross-cutting reducers are documented in `state-management.md` and referenced from the relevant system docs.

## Requirements

### Functional Requirements

- **FR-001**: The project MUST contain a `docs/` directory at the project root with the structure mandated by Constitution v1.4.0.
- **FR-002**: `docs/architecture/` MUST contain five files: `overview.md`, `state-management.md`, `event-system.md`, `dependency-injection.md`, `data-pipeline.md`.
- **FR-003**: Each architecture doc MUST contain at least one cross-system Mermaid diagram.
- **FR-004**: `overview.md` MUST include a system-level Mermaid diagram showing all features, communication paths (EventBus, DI wiring, ECS sync), and the hybrid DOTS/MonoBehaviour boundary.
- **FR-005**: `state-management.md` MUST include a GameState tree diagram, reducer composition visualization, and action dispatch flow.
- **FR-006**: `event-system.md` MUST include a complete event catalog with publisher/subscriber mappings for all 25 event types.
- **FR-007**: `dependency-injection.md` MUST document VContainer scope hierarchy, registration patterns, and the async subscription convention.
- **FR-008**: `data-pipeline.md` MUST document the full lifecycle from SO authoring through Baker to BlobAsset to ECS System to View.
- **FR-009**: `docs/systems/` MUST contain one doc per shipped Phase 0 feature: `camera.md`, `input.md`, `ship.md`, `mining.md`, `procedural.md`, `resources.md`, `hud.md`, `docking.md`, `station-services.md`, `targeting.md`, `station.md`, `world.md`.
- **FR-010**: Each system doc MUST contain all 10 sections mandated by the constitution: Purpose, Architecture Diagram (Mermaid), State Shape, Actions, ScriptableObject Configs, ECS Components, Events, Assembly Dependencies, Key Types, Designer Notes.
- **FR-011**: Each system doc MUST contain at least one Mermaid diagram showing data flow for that system.
- **FR-012**: Skeleton-only features (Fleet, TechTree, Economy) MUST NOT have system docs until they contain implementation. Their existence is noted in `overview.md` as future phases.
- **FR-013**: `docs/designer-guide/` MUST contain four files: `scriptable-objects.md`, `adding-ores.md`, `adding-stations.md`, `tuning-reference.md`.
- **FR-014**: Designer guide docs MUST contain no C# code samples, no namespace references, and no architectural jargon. Written entirely for non-programmer audience.
- **FR-015**: `scriptable-objects.md` MUST catalog all ScriptableObject types with: Create menu path, all serialized fields, valid ranges and defaults, and consuming systems.
- **FR-016**: `adding-ores.md` MUST provide step-by-step instructions for creating a new ore type with zero code changes.
- **FR-017**: `adding-stations.md` MUST provide step-by-step instructions for creating a new station type with zero code changes.
- **FR-018**: `tuning-reference.md` MUST provide a consolidated quick-reference table of all designer-tunable parameters across all ScriptableObjects.
- **FR-019**: `docs/glossary.md` MUST define all project-specific terminology including but not limited to: Reducer, BlobAsset, Authoring, Baking, EventBus, ScriptableObject, ImmutableArray, PilotCommand, ECS, DOTS, Burst, MonoBehaviour, Assembly Definition.
- **FR-020**: `docs/troubleshooting.md` MUST document all known pitfalls from development, including per-instance material property overrides, FBX mesh scale, stale ECS ship position, and ECS entity-gone race conditions.
- **FR-021**: `docs/onboarding.md` MUST provide a recommended reading order through the documentation set for new developers.
- **FR-022**: `docs/assembly-map.md` MUST include a Mermaid diagram showing all assembly definitions and their dependency relationships.
- **FR-023**: All Mermaid diagrams MUST use valid Mermaid syntax (flowcharts, state diagrams, sequence diagrams, class diagrams, or ER diagrams as appropriate).
- **FR-024**: Systems and architecture docs MUST target developers — type names, namespace references, code patterns, and architectural rationale are expected.
- **FR-025**: Designer guide docs MUST target non-programmers — plain language, asset paths, Unity Editor workflows, and visual aids only.

### Key Entities

- **System Document**: A markdown file documenting a single feature module with 10 mandatory sections and at least one Mermaid diagram.
- **Architecture Document**: A markdown file documenting a cross-cutting architectural concern with Mermaid diagrams.
- **Designer Guide**: A markdown file providing step-by-step instructions for non-programmers with no code references.
- **Supporting Document**: A reference document (glossary, troubleshooting, onboarding, assembly map) that aids navigation and comprehension of the documentation set.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All 25 documentation files exist in the correct directory structure under `docs/` (5 architecture + 12 systems + 4 designer guide + 4 supporting).
- **SC-002**: 100% of system docs contain all 10 mandatory sections as defined by the constitution.
- **SC-003**: 100% of system and architecture docs contain at least one valid Mermaid diagram each.
- **SC-004**: Designer guide docs contain zero instances of C# code, namespace references, or architectural jargon.
- **SC-005**: A new developer can determine the recommended reading order within 1 minute by opening `onboarding.md`.
- **SC-006**: A designer can find the Create menu path and field documentation for any ScriptableObject within 2 minutes using the designer guide.
- **SC-007**: All 25 event types are documented in the event catalog with publisher and subscriber information.
- **SC-008**: All 29 assembly definitions are represented in the assembly dependency map.
- **SC-009**: All known development pitfalls (at least 4 documented in project memory) appear in `troubleshooting.md`.
- **SC-010**: The documentation set is self-consistent — no broken cross-references, no contradictions between system docs and architecture docs.

## Assumptions

- This spec covers the one-time bootstrap of the `docs/` directory for all existing Phase 0 features. Future features will maintain docs as part of their delivery gate per the constitution.
- The 12 system docs correspond to the 12 shipped Phase 0 feature modules: Camera, Input, Ship, Mining, Procedural, Resources, HUD, Docking, StationServices, Targeting, Station, World.
- Skeleton-only features (Fleet, TechTree, Economy) are excluded from system docs and noted as future phases in the overview.
- The `HOWTOPLAY.md` player documentation (a separate constitution requirement from v1.3.0) is out of scope for this spec and will be addressed separately.
- No code changes are required — this is a documentation-only deliverable.
- Mermaid diagrams will be authored in markdown fenced code blocks and are renderable by GitHub, VS Code, and standard markdown preview tools.
