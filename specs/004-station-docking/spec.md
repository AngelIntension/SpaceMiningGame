# Feature Specification: Station Docking & Interaction Framework

**Feature Branch**: `004-station-docking`
**Created**: 2026-02-28
**Status**: Draft
**Input**: User description: "Add context-sensitive radial menu for stations and automatic docking to MS2 station presets (SmallMiningRelay + MediumRefineryHub). Foundation for all future station interaction."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Context-Sensitive Station Radial Menu (Priority: P1)

A player flying near a station left-clicks on it to target it, then right-clicks to open the radial menu. Instead of the mining-oriented options (Mine, Approach, Orbit, Keep at Distance), the menu shows station-appropriate options: **Approach**, **Keep at Distance**, **Orbit**, and **Dock**. The "Mine" option is absent because mining a station makes no sense. Meanwhile, targeting an asteroid still shows the original mining-focused radial menu with no change in behavior.

**Why this priority**: The radial menu is the primary interaction gateway for everything in VoidHarvest. Without context-sensitive options, no station interaction is possible. This is the prerequisite for all subsequent docking and station features.

**Independent Test**: Can be fully tested by targeting a station and verifying the radial menu shows Approach/Keep at Distance/Orbit/Dock options, then targeting an asteroid and verifying the original Mine/Approach/Orbit/Keep at Distance options still appear. Delivers meaningful value: players can now distinguish stations from asteroids in the targeting system.

**Acceptance Scenarios**:

1. **Given** the player is flying undocked and a station is in view, **When** the player left-clicks on the station, **Then** the station becomes the active target with a visual indicator (highlight or bracket).
2. **Given** the player has a station targeted while undocked, **When** the player right-clicks (radial menu), **Then** the menu displays four options: Approach, Keep at Distance, Orbit, Dock.
3. **Given** the player has an asteroid targeted, **When** the player right-clicks (radial menu), **Then** the menu displays the unchanged mining options: Approach, Orbit, Mine, Keep at Distance.
4. **Given** the player has a station targeted, **When** the player selects "Approach" from the radial menu, **Then** the ship begins autopilot approach toward the station (same behavior as asteroid approach).

---

### User Story 2 - Automatic Docking (Priority: P1)

A player targets a station, opens the radial menu, and selects "Dock." The ship enters a docking sequence: it automatically aligns toward the station's docking port, flies toward it under autopilot, and performs a magnetic snap into position. During the approach, the player sees visual feedback (alignment guides, proximity indicators). On successful dock, the ship locks into place, engines power down, and satisfying audio/visual feedback confirms the dock. The player's ship is now "docked" — physics forces cease, and the ship is held at the docking port.

**Why this priority**: Docking is the core deliverable of this spec. It transforms stations from scenery into interactive destinations. Without docking, the Station Services Menu (P2) and docked radial menu (P3) have no trigger.

**Independent Test**: Can be fully tested by selecting Dock from the radial menu and observing the complete approach-align-snap sequence, verifying the ship reaches a stable docked state with appropriate feedback.

**Acceptance Scenarios**:

1. **Given** the player has a station targeted and is undocked, **When** the player selects "Dock" from the radial menu, **Then** the ship enters a docking flight mode and begins automatic approach toward the station's docking port.
2. **Given** the ship is in docking flight mode and approaching a station, **When** the ship reaches close proximity to the docking port, **Then** a magnetic snap animation plays and the ship locks into the docked position with audio and visual feedback.
3. **Given** the ship has successfully docked, **Then** the ship's physics simulation is suspended (no drift, no thrust response), the ship is held at the docking port, and the flight mode indicates "Docked."
4. **Given** the ship is in docking approach, **When** the player issues a manual thrust command (overriding autopilot), **Then** the docking sequence is cancelled and the ship returns to manual flight mode.
5. **Given** the player selects "Dock" from the radial menu, **When** the ship is already beyond maximum docking initiation range (500m), **Then** the ship first approaches to within docking range before beginning the docking alignment sequence.

---

### User Story 3 - Station Services Menu Skeleton (Priority: P2)

When the player's ship completes docking, a Station Services Menu panel automatically appears on screen. This is a clean, themed UI panel that displays the station's name and preset type (e.g., "Small Mining Relay" or "Medium Refinery Hub"). The panel contains placeholder tabs or sections for future services (e.g., "Refinery," "Market," "Repair," "Cargo"). These tabs are visible but non-functional, giving the player a preview of what will come in future updates. When the player undocks, the menu automatically closes.

**Why this priority**: The services menu is the visual payoff of docking — it proves the docking system works end-to-end and sets up the UI framework for spec 005. However, it has no gameplay impact yet (all placeholder), so it's secondary to the docking mechanics themselves.

**Independent Test**: Can be fully tested by completing a dock and verifying the services menu appears with the correct station name and placeholder tabs, then undocking and verifying it closes. Delivers value: players see the promise of station interaction.

**Acceptance Scenarios**:

1. **Given** the player's ship has just completed docking at a station, **When** the dock state is confirmed, **Then** the Station Services Menu panel opens automatically showing the station's name and type.
2. **Given** the Station Services Menu is open, **Then** placeholder tabs/sections are visible (at minimum: "Refinery," "Market," "Repair," "Cargo") but display a "Coming Soon" or equivalent inactive state.
3. **Given** the Station Services Menu is open and the player is docked, **When** the player initiates undocking, **Then** the menu closes before or as the undock sequence begins.
4. **Given** the player docks at a SmallMiningRelay, **Then** the menu header shows "Small Mining Relay" and the station's name.
5. **Given** the player docks at a MediumRefineryHub, **Then** the menu header shows "Medium Refinery Hub" and the station's name.

---

### User Story 4 - Undocking via Radial Menu (Priority: P2)

While docked at a station, the player right-clicks on the station (or uses a menu button) to open the radial menu. Instead of the undocked station options, the menu shows only **Undock**. Selecting "Undock" plays an undocking sequence: the ship detaches from the docking port, moves to a safe clearance distance from the station, and returns to normal flight mode. Audio and visual feedback accompany the undock.

**Why this priority**: Undocking completes the dock/undock loop. Without it, the player is permanently stuck at the station. It's essential but simpler than docking (reverse sequence).

**Independent Test**: Can be fully tested by docking at a station, then right-clicking and selecting Undock, verifying the ship detaches and returns to free flight.

**Acceptance Scenarios**:

1. **Given** the player is docked at a station, **When** the player right-clicks to open the radial menu, **Then** the menu shows only "Undock" (no Approach, Orbit, Keep at Distance, Dock, or Mine).
2. **Given** the player is docked and selects "Undock" from the radial menu, **Then** the ship detaches from the docking port, moves to a safe clearance position, and transitions to normal idle flight mode.
3. **Given** the undock sequence is playing, **Then** appropriate audio and visual feedback accompany the departure (engine power-up, release effect).
4. **Given** the player has undocked, **When** the player right-clicks the same station, **Then** the radial menu reverts to the undocked station options (Approach, Keep at Distance, Orbit, Dock).

---

### User Story 5 - Docking Audio & Visual Feedback (Priority: P3)

The docking and undocking sequences are accompanied by satisfying, themed audio and visual effects that make the experience feel polished and rewarding. The docking approach shows alignment guides or tractor effects. The snap moment has a satisfying clunk/lock sound and a brief visual flash or particle effect. Undocking has engine-start and release sounds. All feedback is configurable by designers without code changes.

**Why this priority**: Feedback is a polish layer. The docking system is fully functional without it, but the user experience is significantly better with it. Deferred to P3 because it can be added incrementally after the core mechanics work.

**Independent Test**: Can be tested by performing a dock/undock cycle and evaluating the audio/visual feedback quality.

**Acceptance Scenarios**:

1. **Given** the ship is in docking approach, **Then** visual feedback (e.g., alignment guide, proximity glow, or tractor beam effect) is visible to the player.
2. **Given** the ship snaps into docked position, **Then** a docking confirmation sound plays and a brief visual effect marks the successful dock.
3. **Given** the player undocks, **Then** an engine power-up sound and a release/detach visual effect play.
4. **Given** a designer wants to adjust feedback intensity, timing, or assets, **Then** they can do so through configuration without modifying any logic.

---

### Edge Cases

- What happens when the player targets a station that is behind them or very far away (>2000m) and selects "Dock"? The ship should approach first, then dock when in range.
- What happens if the player selects "Dock" while already docked? The option should not appear in the radial menu (replaced by "Undock").
- What happens if the player loses targeting on the station mid-docking approach (e.g., the station is destroyed or despawned)? The docking sequence should cancel gracefully, returning the ship to idle flight mode.
- What happens if two stations are close together and the player rapidly switches targets? The docking sequence should cancel and not leave the ship in an undefined state.
- What happens if the player opens the radial menu while in mid-docking-approach? They should be able to cancel the docking by selecting a different action or by issuing manual thrust.
- What happens if there is no valid docking port on the station? The system should select a default attachment point (e.g., center of the station) for MVP purposes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST detect whether the player's current target is a station or an asteroid and display the appropriate radial menu options for each target type.
- **FR-002**: System MUST display "Approach," "Keep at Distance," "Orbit," and "Dock" in the radial menu when a station is targeted and the player is undocked.
- **FR-003**: System MUST display only "Undock" in the radial menu when the player is docked at a station.
- **FR-004**: System MUST preserve the existing radial menu behavior (Approach, Orbit, Mine, Keep at Distance) when an asteroid is targeted, with zero regressions.
- **FR-005**: System MUST support automatic docking initiated by selecting "Dock" from the radial menu, including automatic approach, alignment, and magnetic snap to the station's docking port.
- **FR-006**: System MUST transition the ship to a "docked" state upon successful docking, suspending ship physics (no drift, no thrust response) and holding the ship at the docking port.
- **FR-007**: System MUST support undocking initiated by selecting "Undock" from the radial menu, including detachment, clearance movement, and return to normal flight mode.
- **FR-008**: System MUST cancel the docking sequence if the player issues manual thrust or selects a different radial action during approach.
- **FR-009**: System MUST display a Station Services Menu panel automatically upon successful docking, showing the station's name, preset type, and placeholder service tabs.
- **FR-010**: System MUST close the Station Services Menu automatically when the player undocks.
- **FR-011**: System MUST provide configurable audio and visual feedback for the docking snap, undocking release, and docking approach.
- **FR-012**: System MUST track dock/undock state so that other systems (inventory, economy, future specs) can query whether the player is currently docked and at which station.
- **FR-013**: System MUST handle the docking sequence gracefully if the target station is lost (destroyed/despawned) mid-approach, cancelling the sequence and returning to idle flight mode.
- **FR-014**: System MUST make both MS2 station presets (SmallMiningRelay and MediumRefineryHub) targetable and dockable in the existing test scene.

### Key Entities

- **DockingState**: Represents the current docking status of the player's ship — whether undocked, in docking approach, docked, or undocking. Includes reference to the target station and approach progress.
- **StationTarget**: Extends the existing targeting system to distinguish station targets from asteroid targets. Includes station identity, docking port position, and docking range.
- **Station Services Menu**: A UI panel entity that displays station information and placeholder service sections. Tied to the docked station's identity and preset type.
- **Docking Feedback Configuration**: Designer-tunable settings for all audio and visual effects during dock/undock sequences (sounds, particle effects, timing, intensity).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Players can complete a full target-station → open-radial → select-Dock → auto-approach → snap-dock → see-menu → Undock → resume-flight loop within 30 seconds at normal proximity (<500m).
- **SC-002**: The radial menu correctly reflects context 100% of the time: station options when targeting a station while undocked, "Undock" only when docked, mining options when targeting an asteroid.
- **SC-003**: The entire docking sequence (from radial selection to docked state) completes without frame rate dropping below 60 FPS on mid-range hardware.
- **SC-004**: Zero regressions in existing asteroid mining, ship physics, or radial menu behavior — all pre-existing gameplay loops function identically.
- **SC-005**: Docking can be cancelled mid-approach 100% of the time by issuing manual thrust, returning the ship to normal flight within 1 second.
- **SC-006**: The Station Services Menu appears within 0.5 seconds of dock completion and closes within 0.5 seconds of undock initiation.
- **SC-007**: Both station presets (SmallMiningRelay and MediumRefineryHub) are dockable and display their correct name/type in the Station Services Menu.

## Assumptions

- Each station has a single docking port for MVP. Multi-port docking is deferred to a future spec.
- The docking port position defaults to a reasonable location relative to the station model (e.g., near the hangar module if present, otherwise station center). Exact placement is a designer decision during implementation.
- Maximum docking initiation range is 500m. If the player is beyond this range, the ship approaches first.
- The magnetic snap zone (where autopilot hands off to the snap animation) is approximately 25-50m from the docking port.
- The Station Services Menu placeholder tabs are: "Refinery," "Market," "Repair," "Cargo." These are non-functional labels only.
- Docking is single-player only for this spec — no concurrent docking conflicts.
- The "Approach," "Orbit," and "Keep at Distance" options for stations use the same distance submenu and autopilot behavior as for asteroids.

## Scope Boundaries

### In Scope

- Context-sensitive radial menu (station vs asteroid target detection)
- Automatic docking sequence (approach, align, magnetic snap)
- Dock/undock state management
- Undocking via radial menu
- Station Services Menu skeleton (opens on dock, closes on undock, placeholder tabs)
- Audio and visual feedback for dock/undock (configurable)
- Both MS2 station presets made targetable and dockable

### Out of Scope (deferred to spec 005+)

- Any functional station services (selling, refining, refueling, repairing, cargo transfer)
- Data-driven economy system
- Ore/resource migration or inventory changes on dock/undock
- Multi-port docking or docking queue systems
- NPC ship docking
- Station construction or module placement
