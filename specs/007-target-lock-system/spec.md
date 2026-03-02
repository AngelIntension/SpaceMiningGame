# Feature Specification: In-Flight Targeting & Multi-Target Lock System

**Feature Branch**: `007-target-lock-system`
**Created**: 2026-03-02
**Status**: Draft
**Input**: User description: "In-Flight Targeting and Multi-Target Lock System with Time-to-Lock and Selection Name Display — adds click-to-select with corner reticles, timed target locking via radial menu, and persistent multi-target HUD cards with live viewports."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Select Target with Reticle Overlay (Priority: P1)

A player flying through an asteroid field left-clicks on an asteroid (or station). A rectangular reticle composed of four corner brackets immediately appears around the object in screen space. The target's name is displayed centered above the reticle, and the real-time range (in meters) is displayed centered below the reticle. Both labels update continuously as the ship moves. Clicking empty space or a different object clears or transfers the selection.

**Why this priority**: Selection with visual feedback is the foundation of the entire targeting system. Every subsequent feature (locking, multi-target cards) depends on selection working correctly. This also replaces the partially-wired target info panel with a proper world-space reticle, delivering immediate visual polish.

**Independent Test**: Can be fully tested by flying near asteroids/stations, clicking to select, observing reticle + name + range appear, and clicking elsewhere to deselect. Delivers the core "I can see what I'm targeting" value.

**Acceptance Scenarios**:

1. **Given** the player is in flight near an asteroid field, **When** they left-click on an asteroid, **Then** a four-corner bracket reticle appears around the asteroid in screen space with configurable padding, the asteroid's name and ore type display centered above the reticle, and the range in meters and mass percentage display centered below the reticle.
2. **Given** the player has an asteroid selected, **When** they left-click on a station, **Then** the reticle transfers to the station, displaying the station's name and updated range.
3. **Given** the player has a target selected, **When** they left-click on empty space, **Then** the reticle, name, and range display all disappear.
4. **Given** the player has a target selected, **When** the ship moves closer to or farther from the target, **Then** the range label updates in real time (formatted as whole meters, e.g., "1,247 m").
5. **Given** the player has a target selected, **When** the target moves off-screen, **Then** the reticle and labels are replaced by a tracking indicator (small directional triangle) clamped to the nearest screen edge, pointing toward the target's direction. The indicator continuously tracks as the relative position changes.
6. **Given** the player has a target selected with the tracking indicator visible at the screen edge, **When** the target returns to the viewport, **Then** the tracking indicator is replaced by the full reticle and labels.

---

### User Story 2 — Timed Target Lock via Radial Menu (Priority: P2)

A player right-clicks a selected target to open the radial menu, which now includes a "Lock Target" option. Choosing it begins a timed lock acquisition. During acquisition, the reticle visually pulses and a progress arc/ring fills around the reticle, with a small countdown timer displayed. A rising-tone audio cue plays throughout. Upon completion, the lock is confirmed with a distinct visual and audio cue. The lock can be cancelled if the player deselects or the target is destroyed — each triggering a failure sound and visual reset. Line-of-sight is not required for locking (future scan lists will allow locking targets without direct visibility).

**Why this priority**: Timed locking is the core gameplay mechanic that differentiates this from simple click-to-select. It adds deliberate pacing and tension, directly inspired by EVE Online. This must work before multi-target management is meaningful.

**Independent Test**: Can be fully tested by selecting a target, opening the radial menu, initiating lock, observing the progress animation/audio, and confirming the lock completes after the configured time. Also testable by cancelling mid-lock to verify failure feedback.

**Acceptance Scenarios**:

1. **Given** the player has a target selected, **When** they open the radial menu, **Then** a "Lock Target" segment is visible (for both asteroids and stations).
2. **Given** the player clicks "Lock Target" on the radial menu, **When** the lock timer begins, **Then** the reticle corners pulse, a progress arc fills around the reticle, an optional countdown timer is displayed, and a rising-tone audio cue plays.
3. **Given** the lock timer is running, **When** the configured lock time elapses without interruption, **Then** the target is marked as locked with a confirmation visual flash and audio cue.
4. **Given** the lock timer is running, **When** the player deselects the target (clicks elsewhere), **Then** the lock is cancelled with a failure sound, and the reticle and progress indicators reset.
5. **Given** the lock timer is running, **When** the target moves beyond the maximum lock range, **Then** the lock is cancelled with a failure sound and visual reset. Line-of-sight is not required — locking works regardless of occlusion or target visibility.
6. **Given** a target is already locked, **When** the player attempts to lock the same target again, **Then** the system ignores the duplicate lock attempt (no error, no action).

---

### User Story 3 — Multi-Target Management (Priority: P3)

After locking a target, the player can lock additional targets (up to a per-ship maximum). Each locked target occupies a card in a HUD panel. The panel is positioned immediately to the left of the ship info display and grows leftward as targets are added. New cards appear on the right side of the panel; existing cards shift left. The player can unlock targets individually (e.g., by clicking a dismiss control on the card or via a keyboard shortcut). When the player docks at a station, all target locks are automatically cleared.

**Why this priority**: Multi-target management enables tactical gameplay where players track multiple asteroids or stations simultaneously. It depends on both selection (P1) and locking (P2) being complete.

**Independent Test**: Can be fully tested by locking 2–3 targets sequentially, observing cards appear and shift, then unlocking one and verifying the panel updates. Also testable by docking to confirm all locks clear.

**Acceptance Scenarios**:

1. **Given** the player has locked one target, **When** they lock a second target, **Then** a second card appears to the right of the first, and the first card shifts left.
2. **Given** the player has locked the maximum number of targets, **When** they attempt to lock an additional target, **Then** the system provides feedback that the lock limit has been reached (visual/audio cue) and the lock does not proceed.
3. **Given** the player has multiple locked targets, **When** they dismiss one target card, **Then** that card is removed and remaining cards reflow to close the gap.
4. **Given** the player has locked targets, **When** they dock at a station, **Then** all target locks are cleared and all cards are removed from the panel.
5. **Given** the player has locked targets, **When** they undock from a station, **Then** the target card panel is empty and ready for new locks.

---

### User Story 4 — Target Cards with Live Viewports (Priority: P4)

Each locked target's HUD card contains a live viewport showing only the targeted object (isolated from the surrounding scene), zoomed and framed to fill the card. Below the viewport, the card displays the target's name/type and a continuously-updating range. The cards have a thin rectangular sci-fi border and match the premium visual style of the existing HUD.

**Why this priority**: Live viewports are the visual polish layer. The core multi-target functionality (P3) works with name + range alone; the viewport cameras add premium quality but are the most performance-sensitive component.

**Independent Test**: Can be fully tested by locking a target and observing the card viewport shows a live, correctly-framed view of the target, with name and range updating as the ship moves.

**Acceptance Scenarios**:

1. **Given** the player locks a target, **When** the target card appears, **Then** it contains a live viewport image showing only the locked object (no other scene objects visible), zoomed and framed to fill the card.
2. **Given** the player has a target card visible, **When** the ship rotates or moves, **Then** the viewport updates in real time, continuing to show only the isolated target.
3. **Given** the player has multiple target cards, **When** viewing the HUD, **Then** each card's viewport shows its respective target independently.
4. **Given** the player has target cards visible, **When** observing frame rate, **Then** there is no perceptible performance degradation compared to having no target cards (within the project's 60 FPS target).
5. **Given** a locked target is destroyed (e.g., asteroid fully depleted), **When** the destruction occurs, **Then** the corresponding card is automatically removed from the panel.

---

### User Story 5 — Lock Time Computation (Priority: P5)

Each ship has a configurable base lock time (default 1.5 seconds). The system computes the actual lock duration through a dedicated calculation that, for this version, simply returns the base lock time. The calculation method is designed to accept the target as input so that future versions can factor in distance, target size, sensor upgrades, or other modifiers without restructuring.

**Why this priority**: This is a data-driven configuration concern. The default value works out of the box; the extensible calculation method is a design-for-the-future investment that requires minimal effort now.

**Independent Test**: Can be tested by configuring two different ships with different base lock times and verifying each takes the expected duration to lock the same target.

**Acceptance Scenarios**:

1. **Given** a ship with a base lock time of 1.5 seconds, **When** the player initiates a lock, **Then** the lock completes in exactly 1.5 seconds (within a ±0.1s tolerance).
2. **Given** a ship with a base lock time of 2.0 seconds, **When** the player initiates a lock on the same target type, **Then** the lock completes in exactly 2.0 seconds (within a ±0.1s tolerance).
3. **Given** any ship, **When** the lock time is calculated, **Then** the calculation accepts the target as an input parameter (enabling future extensibility without changing callers).

---

### User Story 6 — Player Documentation (Priority: P6)

All player-facing documentation is updated to reflect the new targeting and locking system. The HOWTOPLAY.md file includes a new "Targeting & Locking" section covering controls, lock times, and multi-target management. The changelog is updated with Spec 007 details.

**Why this priority**: Documentation is a delivery gate per the project constitution but does not block core functionality.

**Independent Test**: Can be verified by reading HOWTOPLAY.md and confirming all new controls and mechanics are documented clearly for a player audience.

**Acceptance Scenarios**:

1. **Given** the targeting system is implemented, **When** a player reads HOWTOPLAY.md, **Then** they find a complete section explaining how to select targets, initiate locks, manage multiple locked targets, and interpret the target card HUD.
2. **Given** the documentation is complete, **When** reviewed, **Then** it contains no code references, internal type names, or architecture jargon — only player-facing language.

---

### Edge Cases

- What happens when the player selects a target that is extremely far away (beyond practical lock range)? The system allows selection (reticle + name + range visible) but the lock attempt fails immediately if the target is beyond the ship's maximum lock range, with clear feedback ("Target out of range").
- What happens when a locked asteroid is fully depleted? The target lock is automatically released and the card is removed from the HUD with a brief "target lost" visual/audio cue.
- What happens when the player is already locking one target and clicks to select a different target? The in-progress lock is cancelled (with failure feedback), and the selection transfers to the new target. Only one lock acquisition can be in progress at a time.
- What happens when a target is occluded by another object during lock acquisition? Nothing — line-of-sight is not required for locking. The lock proceeds normally regardless of occlusion. This supports future scan-list locking where LOS is not guaranteed.
- What happens when the player tries to lock a target they already have locked? The system silently ignores the duplicate request — no error, no feedback, no action.
- What happens if the player opens the radial menu with no target selected? The "Lock Target" segment is not shown (consistent with existing behavior where context-sensitive segments are hidden).
- What happens when the reticle target is behind the camera? The reticle and labels are replaced by a tracking indicator (directional triangle) at the nearest screen edge, pointing toward the target. This applies whether the target is behind the camera or outside the viewport frustum.
- What happens on ship swap (future fleet feature)? All target locks are cleared on ship swap, same as docking behavior.

## Requirements *(mandatory)*

### Functional Requirements

**Selection & Reticle**

- **FR-001**: The system MUST allow the player to select any targetable object (asteroid or station) by left-clicking on it while in flight.
- **FR-002**: The system MUST display a rectangular reticle composed of four corner brackets around the selected target in screen space, with designer-configurable padding.
- **FR-003**: The system MUST display the target's name and type (e.g., ore type for asteroids) centered above the reticle, and the distance to the target (in meters, formatted with thousands separator) and mass percentage centered below the reticle.
- **FR-004**: The range display MUST update continuously in real time as the ship or target moves.
- **FR-005**: When the selected target moves off-screen or behind the camera, the system MUST replace the reticle and labels with a tracking indicator (small directional triangle) clamped to the nearest screen edge, pointing toward the target. The indicator MUST continuously update its position and orientation as the relative direction changes.
- **FR-005a**: When the target returns to the viewport, the tracking indicator MUST be replaced by the full reticle and labels.
- **FR-006**: The system MUST clear the selection when the player clicks on empty space.
- **FR-007**: The system MUST transfer the selection (and reticle) when the player clicks on a different targetable object.
- **FR-007a**: The new reticle MUST replace the existing target info panel. The previous standalone target info display (name, ore type, distance, mass) is removed; all target information is consolidated into the reticle labels.

**Locking**

- **FR-008**: The radial menu MUST include a "Lock Target" segment when a targetable object is selected (visible for all target types: asteroids and stations).
- **FR-009**: Activating "Lock Target" MUST begin a timed lock acquisition lasting the ship's calculated lock duration.
- **FR-010**: During lock acquisition, the system MUST display a progress indicator around the reticle (progress arc or ring) and pulse the reticle corners.
- **FR-011**: During lock acquisition, the system MUST play a rising-tone audio cue that completes on successful lock.
- **FR-012**: On successful lock completion, the system MUST play a confirmation audio cue and display a brief visual confirmation effect on the reticle.
- **FR-013**: The system MUST cancel the lock acquisition if the player deselects the target, the target is destroyed, or the target moves beyond lock range. Line-of-sight is NOT required for locking — locks proceed regardless of occlusion. Range checks apply only during acquisition — once a lock is established, it persists regardless of distance.
- **FR-014**: On lock cancellation, the system MUST play a failure audio cue and reset the reticle progress indicators.
- **FR-015**: The system MUST prevent duplicate lock attempts on an already-locked target (silent ignore).
- **FR-016**: Only one lock acquisition MAY be in progress at any time. Starting a new selection while locking cancels the in-progress lock.

**Lock Time**

- **FR-017**: Each ship MUST have a configurable base lock time (designer-editable, default 1.5 seconds).
- **FR-018**: The system MUST calculate the actual lock duration through a dedicated computation that accepts the target as input, returning the base lock time for this version.
- **FR-019**: The lock timer MUST be accurate to within ±0.1 seconds of the calculated duration.

**Multi-Target Management**

- **FR-020**: Each ship MUST have a configurable maximum number of simultaneous target locks (designer-editable, default 3).
- **FR-021**: On successful lock, the target MUST be added to the ship's active locked targets list.
- **FR-022**: When the locked targets list is full, the system MUST reject new lock attempts with a "lock slots full" visual/audio cue before the lock timer begins.
- **FR-023**: The player MUST be able to individually unlock (dismiss) any locked target via a control on the target card.
- **FR-023a**: Clicking a target card (outside the dismiss control) MUST select the corresponding locked target — the reticle moves to that target and it becomes the active selection for radial menu actions (Approach, Orbit, Mine, etc.).
- **FR-024**: All target locks MUST be automatically cleared when the player docks at a station.
- **FR-025**: A locked target that is destroyed (e.g., depleted asteroid) MUST be automatically removed from the locked targets list with a "target lost" notification.

**Target Cards HUD**

- **FR-026**: A target card panel MUST be displayed to the immediate left of the ship info HUD element.
- **FR-027**: Each locked target MUST be represented by a card containing: a live viewport image of the target, the target's name/type, and the continuously-updating range.
- **FR-028**: The live viewport MUST render only the targeted object (isolated from the surrounding scene — no background asteroids, stations, or other objects visible), zoomed and framed to fill the card.
- **FR-029**: New target cards MUST appear on the right side of the panel, shifting existing cards leftward.
- **FR-030**: When a target is unlocked or lost, its card MUST be removed and remaining cards MUST reflow to close the gap.
- **FR-031**: Target cards MUST visually match the existing premium HUD style (thin sci-fi border, consistent typography and color scheme).

**Integration**

- **FR-032**: The targeting system MUST support asteroids and stations as targetable objects and MUST be extensible to future object types without restructuring.
- **FR-033**: The reticle, name, and range MUST remain visible during the lock acquisition phase (not replaced by the progress indicator).
- **FR-034**: The "Lock Target" radial menu segment MUST coexist with existing segments (Approach, Orbit, Mine, KeepAtRange, Dock) without displacing them.
- **FR-035**: The targeting system MUST be inactive while the player is docked (no selection, no locking while at a station).

**Documentation**

- **FR-036**: HOWTOPLAY.md MUST include a "Targeting & Locking" section covering: selection controls, lock initiation, lock cancellation conditions, multi-target management, and target card interpretation.
- **FR-037**: The project changelog MUST be updated with Spec 007 feature details.

### Key Entities

- **Targetable Object**: Any in-world object the player can select and lock. Has a name (display string), a position (for range calculation), and a visual bounds (for reticle sizing). Currently: asteroids and stations. Extensible to future object types.
- **Selection State**: The currently-selected (but not necessarily locked) target. At most one selection at a time. Includes the target reference, target type, and selection timestamp.
- **Lock Acquisition**: A time-bounded process that converts a selection into a lock. Has a target reference, elapsed time, total duration, and status (in-progress / completed / cancelled).
- **Target Lock**: A confirmed lock on a specific target. Persists until explicitly dismissed, the target is destroyed, or the player docks. Contains the target reference, target type, name, and a live viewport source.
- **Locked Targets List**: An ordered collection of active target locks for the current ship. Maximum capacity defined per ship. New entries append to the end (displayed rightmost).
- **Target Card**: A HUD element representing one locked target. Contains a live viewport, name/type label, and range label. Position in the card panel corresponds to order in the locked targets list.
- **Targeting Configuration**: Per-ship settings including base lock time (seconds), maximum simultaneous locks (integer), and maximum lock range (meters). Designer-editable.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Players can select a target and see the reticle, name, and range appear within 0.1 seconds of clicking, with no perceptible input lag.
- **SC-002**: The lock acquisition timer is accurate to within ±0.1 seconds of the configured lock time across all ship types.
- **SC-003**: Players can lock up to the maximum configured targets (default 3) simultaneously, with each card displaying correct and independently-updating viewport, name, and range.
- **SC-004**: The game maintains 60 FPS with the maximum number of target cards active (3 by default), with no frame spikes exceeding 2 ms attributable to the targeting system.
- **SC-005**: Lock cancellation (deselect, target destroyed, out-of-range during acquisition) triggers within 1 second of the cancellation condition, with immediate visual/audio feedback.
- **SC-006**: 100% of targeting pure logic (lock time calculation, state transitions, multi-target list management) is covered by automated tests.
- **SC-007**: All target locks clear automatically on docking, with no stale cards or state remaining.
- **SC-008**: The targeting system adds zero new object types to the selection pathway — new targetable object types can be added by implementing a shared contract without modifying existing selection or locking logic.
- **SC-009**: HOWTOPLAY.md contains a complete "Targeting & Locking" section that a new player can follow to successfully select, lock, and manage targets without external guidance.
- **SC-010**: The reticle, progress indicators, and target cards are visually consistent with the existing HUD aesthetic — no jarring style differences when viewed alongside resource panel, ship info, and hotbar.

## Clarifications

### Session 2026-03-02

- Q: Does a completed lock persist if the target moves beyond lock range, or is there an ongoing range check? → A: Locks persist indefinitely once acquired. Range is only checked during lock acquisition, not after. Players can use locks as waypoints for return trips.
- Q: Does clicking a target card in the HUD select that locked target? → A: Yes. Clicking a card selects the locked target — reticle moves to it and it becomes the active selection for radial menu actions (Approach, Orbit, Mine, etc.).
- Q: What happens to the existing target info panel (name/ore/distance/mass)? → A: Merge into the reticle. Name + ore type above the reticle, range + mass below. The existing target info panel is removed.
- Q: What happens when a selected target moves off-screen? → A: A tracking indicator (small directional triangle) appears at the nearest screen edge, pointing toward the target, and continuously tracks as relative position changes.
- Q: Is line-of-sight required for target locking? → A: No. LOS is irrelevant for locking. Locks proceed regardless of occlusion. Future scan lists will allow locking targets without direct visibility.
- Q: What should the live viewport render? → A: Only the targeted object, isolated from the surrounding scene. No background asteroids, stations, or other objects visible.

## Assumptions

- **Lock range default**: Maximum lock range defaults to 5,000 meters (matching a reasonable sensor range for mining vessels). This is designer-configurable and can be tuned without code changes.
- **Viewport camera performance**: Each target card viewport uses a low-resolution render texture (e.g., 256x256 or lower) with a narrow field of view and reduced draw distance to minimize GPU cost. The exact resolution is an implementation detail to be tuned during development.
- **Card dismissal control**: A small "X" button or similar dismiss affordance on each target card. Keyboard shortcut (e.g., Ctrl+1/2/3) for power users may be added if time permits but is not required for this spec.
- **Audio assets**: Placeholder audio clips (rising tone, confirm beep, failure buzz) are acceptable for initial implementation. Final audio design is out of scope.
- **Reticle sizing**: The corner bracket reticle size adapts to the target's apparent screen-space bounding size (larger objects get larger reticles). The padding between the object edge and the bracket corners is configurable.
- **Ship swap**: Although fleet management is a future feature, the targeting system assumes all locks clear on ship swap, consistent with the docking-clears-locks behavior.
- **No lock-through-station**: The player cannot initiate or maintain locks while docked. The targeting system is strictly an in-flight feature.
