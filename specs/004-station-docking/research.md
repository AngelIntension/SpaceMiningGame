# Research: Station Docking & Interaction Framework

**Feature**: 004-station-docking | **Date**: 2026-02-28

## Technical Context Resolved

No NEEDS CLARIFICATION items existed in the Technical Context. All technology choices are established by the constitution and existing codebase. The following research documents decisions made during plan design.

---

### R-001: Docking State Location

**Decision**: `DockingState` record and `IDockingAction` interface live in `Core/State/` alongside existing state types. Concrete actions and docking-specific types live in `Features/Docking/`.

**Rationale**: Follows established pattern — `ShipState`, `FleetState`, `MiningSessionState`, `IShipAction`, `IFleetAction` etc. all reside in `Core/State/`. The `CompositeReducer` in `RootLifetimeScope` needs to pattern-match on action interfaces, which requires them in the shared state assembly.

**Alternatives considered**:
- All docking types in `Features/Docking/` — rejected because `GameLoopState` (in Core/State) references `DockingState`, creating a circular dependency.

---

### R-002: Cross-State Coordination (Dock → FleetState + DockingState)

**Decision**: Dispatch two separate actions sequentially when docking completes: `CompleteDockingAction` (→ `DockingReducer`) and `DockAtStationAction` (→ `FleetReducer`). Same pattern for undocking.

**Rationale**: The `CompositeReducer` uses C# pattern matching (`switch` expression) which matches only the first applicable branch. An action can only implement one action interface effectively. Two actions keeps each reducer pure and independent.

**Alternatives considered**:
- Single action implementing both interfaces — rejected because `switch` only matches one branch.
- Modify `CompositeReducer` to chain multiple branches — rejected because it changes shared infrastructure for a single feature.

---

### R-003: Docking ECS Architecture

**Decision**: Separate `DockingSystem : ISystem` runs `[UpdateBefore(typeof(ShipPhysicsSystem))]`. During approach phase, it writes to `PilotCommandComponent` to leverage existing approach autopilot. During snap/undock phases, it writes directly to `ShipPositionComponent` and `ShipFlightModeComponent`.

**Rationale**: Approach reuses proven autopilot code. Snap/undock need deterministic interpolation that bypasses physics integration. Running before `ShipPhysicsSystem` ensures snap writes aren't overwritten. The `Docked` flight mode causes `ShipPhysicsSystem` to skip force application entirely.

**Alternatives considered**:
- All docking logic inside `ShipPhysicsSystem` — rejected for separation of concerns (single responsibility).
- MonoBehaviour-based docking (no ECS) — rejected; violates Constitution III (Performance by Default) and existing hybrid pattern where all simulation runs in ECS.

---

### R-004: Radial Menu Dynamic Segments

**Decision**: Add `segment-dock` and `segment-undock` buttons to existing `RadialMenu.uxml`. `RadialMenuController.Open()` shows/hides segments based on target type and docking state.

**Rationale**: Minimal change to existing UI. Avoids multiple UXML files or dynamic element generation. Show/hide via `display: none` is standard UI Toolkit pattern with zero allocation.

**Alternatives considered**:
- Separate UXML per context — rejected for file proliferation and harder maintenance.
- Fully dynamic segment generation from code — rejected as over-engineering for 6 total options.

---

### R-005: Station Target Detection

**Decision**: Add `TargetType` enum to `Features/Input/Data/`. `InputBridge` sets `_selectedTargetType = Station` when `TryRaycastSelectable` hits an object on the `Selectable` layer with a `DockingPortComponent` (or `StationMarker` tag). Extend `RadialMenuRequestedEvent` to carry `TargetType`.

**Rationale**: `TryRaycastSelectable` already raycasts against the `Selectable` layer. Stations are GameObjects (not ECS entities), so physics raycast is the correct detection method. Carrying type in the event avoids RadialMenuController needing to query InputBridge.

**Alternatives considered**:
- Tag-based detection (e.g., "Station" tag) — viable but the `Selectable` layer + component check is more robust and extensible.
- ECS-based station entities — rejected for MVP; stations are low-count, visual-heavy GameObjects that don't benefit from ECS batching.

---

### R-006: Station Services Menu Technology

**Decision**: Unity Canvas (uGUI) for the Station Services Menu, not UI Toolkit.

**Rationale**: The services menu is a full-screen overlay panel with tabs, headers, and content areas. Canvas/uGUI is more mature for this use case (layout groups, button navigation, tab panels). UI Toolkit is used for the radial menu (lightweight, positional overlay) but the services menu is a traditional UI panel where uGUI excels. Also provides variety in the codebase for future reference.

**Alternatives considered**:
- UI Toolkit — viable but less mature for complex panel layouts with tabs and scroll views in Unity 6.

---

### R-007: Docking Port Placement on MS2 Stations

**Decision**: Add an empty child GameObject "DockingPort" to each station prefab at a manually chosen position. SmallMiningRelay: near the connector module. MediumRefineryHub: near the hangar module. Positions are set via MCP during implementation.

**Rationale**: The MS2 station models have no predefined docking ports. An empty GameObject child is the simplest marker — its transform defines the dock pose. Positioned near logical modules (connector/hangar) for visual coherence.

**Alternatives considered**:
- ScriptableObject-defined port offsets — rejected; coupling port position to the visual model is more intuitive and easier to adjust via MCP.
- Procedural port detection from mesh — massively over-engineered for 2 stations.

---

### R-008: FleetState.DockedAtStation Integration

**Decision**: Keep `FleetState.DockedAtStation` as the authoritative "is docked" flag for external system queries. `DockingState.Phase` tracks the detailed docking sequence. Both are updated via separate actions dispatched sequentially.

**Rationale**: `FleetState.DockedAtStation` already exists and is the natural query point for future specs (005+ services, fleet management). `DockingState` is internal to the docking sequence. External systems check `FleetState.DockedAtStation.IsSome` without needing to know about docking phases.

**Alternatives considered**:
- Remove `FleetState.DockedAtStation` in favor of `DockingState.IsDocked` — rejected because FleetState is the higher-level concept other systems will query, and it already exists.
