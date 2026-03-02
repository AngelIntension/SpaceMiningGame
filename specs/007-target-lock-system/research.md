# Research: In-Flight Targeting & Multi-Target Lock System

**Branch**: `007-target-lock-system` | **Date**: 2026-03-02

## Decision 1: ITargetable Interface Location

**Decision**: Place `ITargetable` interface in `Core/Extensions/` assembly (alongside `TargetType`).

**Rationale**: `Core.Extensions` has zero dependencies and is referenced by every assembly in the project. Placing the interface here allows any feature assembly (Docking, Base, future NPC, etc.) to implement it without introducing circular dependencies. The interface only needs `TargetType` (already in `Core.Extensions`) and basic C# types — no `Unity.Mathematics` or ECS types required.

**Alternatives considered**:
- New `Core.Targeting` assembly: Unnecessary indirection. `ITargetable` is a single interface with no dependencies.
- `Features.Targeting.Data`: Would force all implementors to reference the Targeting feature assembly — wrong dependency direction.

## Decision 2: Hybrid Selection Architecture (ECS + ITargetable)

**Decision**: Use `ITargetable` for MonoBehaviour-based objects (stations, future NPCs) and retain ECS-specific queries for asteroid entities. A `TargetInfo` readonly struct acts as the common data contract.

**Rationale**: ECS entities cannot implement C# interfaces. The existing `InputBridge` already uses dual selection paths (Physics raycast for stations, ECS ray-sphere for asteroids). The `ITargetable` interface replaces the hard-coded `DockingPortComponent` check with a generic interface check on the Physics raycast path. ECS asteroids construct `TargetInfo` from `AsteroidComponent` + `AsteroidOreComponent` + `OreDisplayNames`.

**Alternatives considered**:
- Pure ECS approach (tag components): Would require stations to become entities, breaking the existing MonoBehaviour station services architecture.
- Pure MonoBehaviour approach: Would require wrapping every ECS asteroid in a GameObject, destroying DOTS performance benefits.

## Decision 3: TargetingState as a New GameLoopState Slice

**Decision**: Add `TargetingState` as a new slice in `GameLoopState`, managed by `TargetingReducer`.

**Rationale**: Follows the established pattern (DockingState, MiningSessionState, etc.). Selection, lock acquisition, and locked targets list are game state that must be deterministic and testable. The reducer pattern enables TDD and snapshot testing of all state transitions.

**Alternatives considered**:
- Store targeting state only in InputBridge (MonoBehaviour fields): Violates the constitution — MonoBehaviours NEVER hold game state.
- Store in ECS singleton component: Would make it inaccessible to UI layer without a bridge system. The targeting state is heavily read by HUD views.

## Decision 4: Isolated Target Viewport Rendering

**Decision**: Use a dedicated Unity layer ("TargetPreview") with per-target cameras rendering to low-resolution RenderTextures. Each locked target gets a lightweight visual clone positioned at an off-screen staging area.

**Rationale**: The spec requires rendering ONLY the targeted object in isolation. Using a separate culling layer ensures no other scene objects appear in the viewport. Cloning the visual (mesh + material) to a staging area avoids modifying the original entity's layer (which would break main camera rendering). Low-resolution RenderTextures (128x128) minimize GPU cost for up to 3 simultaneous viewports.

**Alternatives considered**:
- Render replacement shader: Would show silhouettes of all objects, not isolation.
- Screenshot capture (static image): Wouldn't update in real time as required.
- Single camera with multiple render passes: More complex, no performance advantage at 3 targets.

## Decision 5: Reticle Rendering Approach

**Decision**: Use UI Toolkit `VisualElement` overlay positioned via `Camera.WorldToScreenPoint`. Corner brackets, labels, and progress arc are UI Toolkit elements dynamically positioned each frame.

**Rationale**: The existing HUD uses UI Toolkit exclusively. Canvas/UGUI would introduce a second UI system. UI Toolkit supports absolute positioning, custom USS styling, and dynamic element manipulation. The reticle elements can be styled consistently with existing HUD panels using the established `.hud-panel` and `.hud-label` USS classes.

**Alternatives considered**:
- Canvas/UGUI: Would introduce a second UI system, inconsistent with the codebase.
- LineRenderer (world-space): Would scale with distance (wrong for screen-space brackets), and can't easily integrate with UI Toolkit labels.
- Shader-based (post-processing): Overly complex for corner brackets + text.

## Decision 6: Radial Menu "Lock Target" Segment Placement

**Decision**: Add "Lock Target" as a new segment at the top-left position of the radial menu, coexisting with all existing segments.

**Rationale**: The existing segments occupy top (Approach), right (Orbit), bottom (Mine), left (KeepAtRange), and bottom-right (Dock). Top-left is the natural open slot. "Lock Target" should be visible for ALL target types (asteroids and stations), unlike Mine (asteroid-only) and Dock (station-only).

**Alternatives considered**:
- Replace an existing position: Would displace a functional action.
- Nested submenu: Adds unnecessary complexity for a single action.
- Separate hotkey only (no radial): Would break the EVE-inspired "right-click for all target actions" pattern.

## Decision 7: Off-Screen Tracking Indicator

**Decision**: Use a UI Toolkit `VisualElement` with a rotated triangle indicator, clamped to screen edges using viewport-space math. The indicator's rotation tracks the direction from screen center to the target's projected position.

**Rationale**: Consistent with the UI Toolkit approach for the reticle. The math (project target to screen space, clamp to viewport bounds, calculate angle) is well-established in game dev. The indicator replaces the reticle when the target exits the viewport and smoothly transitions back when it re-enters.

**Alternatives considered**:
- Compass-style HUD element: More complex, better suited for navigation features.
- Edge-of-screen glow/pulse: Not directional enough to be useful.

## Decision 8: Lock Time Calculation Extensibility

**Decision**: Implement `LockTimeMath.CalculateLockTime(float baseLockTime, TargetInfo target)` as a pure static function. V1 returns `baseLockTime` directly. The `TargetInfo` parameter enables future factors without signature changes.

**Rationale**: Pure static function is testable, Burst-compatible, and follows the project's functional-first principle. Accepting `TargetInfo` (which contains target type, position, etc.) leaves room for distance-based modifiers, target-size scaling, and sensor upgrade multipliers in future versions.

**Alternatives considered**:
- Virtual method on a base class: Inheritance is prohibited by constitution (composition over inheritance).
- ScriptableObject-based formula: Overkill for v1; can be added later as a parameter to `CalculateLockTime` if needed.
