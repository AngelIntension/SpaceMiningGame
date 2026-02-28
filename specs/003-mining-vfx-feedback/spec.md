# Feature Specification: Mining Loop VFX & Feedback

**Feature Branch**: `003-mining-vfx-feedback`
**Created**: 2026-02-28
**Status**: Draft
**Input**: Enhance the core mining loop with high-impact VFX, particle effects, synchronized HUD feedback, and audio cues to create a deeply satisfying, tactile, EVE Online-grade laser-mining experience. Builds on the premium asteroid depletion system (continuous scale lerp + 0.5s crumble pauses at 75/50/25% + final alpha-fade) and the three Retora MiningBarge variants from spec 002.

## Clarifications

### Session 2026-02-28

- Q: Are ore chunks that spawn on asteroid depletion functional (add to inventory) or purely cosmetic? → A: Purely cosmetic — visual reward animation only. Inventory is already credited continuously during mining via yield ticks. Chunks are "juice" that reinforces the reward beat without affecting the resource pipeline.
- Q: Ore chunk spawning should be continuous during mining (not just at depletion). What is the random spawn interval range? → A: 3-7 seconds, randomized per spawn event. Slower, weighty pacing where each chunk feels significant.
- Q: How many ore chunks per spawn event? → A: 2-5 chunks of various small sizes per event, not one. Creates a satisfying scatter/spray of ore debris each iteration.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visceral Mining Laser Beam (Priority: P1)

As a player activating my mining laser on a targeted asteroid, I see a glowing energy beam stretch from my barge's mining arm to the asteroid surface. The beam pulses with energy, colored to match the ore type I'm extracting. At the impact point, bright sparks spray outward in the ore's color, and a subtle heat shimmer rises from the mining arm. The beam tracks the asteroid in real time as my ship orbits or drifts. When I stop mining, the beam and all associated effects shut off cleanly.

**Why this priority**: The mining laser is the player's primary tool and the visual centerpiece of the core loop. Every mining session starts and ends with the beam — getting it right delivers the single biggest "game feel" upgrade. Without a satisfying beam, no amount of secondary polish matters.

**Independent Test**: Can be fully tested by targeting any asteroid and activating the mining laser. Delivers immediate visual impact: the beam, impact sparks, and heat haze are all visible and responsive within a single mining session.

**Acceptance Scenarios**:

1. **Given** the player has targeted an asteroid within range, **When** they activate the mining laser, **Then** a continuous glowing beam renders from the barge mining arm to the asteroid surface with no visible gaps or flickering.
2. **Given** the mining laser is active, **When** the player observes the beam, **Then** it is colored to match the ore type being mined (Veldspar = yellowish, Scordite = reddish, Pyroxeres = bluish).
3. **Given** the beam is hitting the asteroid, **When** the player looks at the impact point, **Then** sparks spray outward from the hit point in the ore's color palette.
4. **Given** the mining laser is active, **When** the player observes the barge mining arm, **Then** a subtle heat haze distortion effect is visible near the beam origin.
5. **Given** the ship is moving or orbiting while mining, **When** the beam is active, **Then** the beam, impact sparks, and heat haze all track the new positions every frame with no lag or discontinuity.
6. **Given** the player deactivates the mining laser or the target goes out of range, **When** mining stops, **Then** the beam, sparks, and heat haze all cease immediately with no lingering artifacts.

---

### User Story 2 - Asteroid Depletion Feedback (Priority: P2)

As a player mining an asteroid, I can visually gauge its remaining resources without checking the HUD. The asteroid's surface veins glow brighter as it becomes more depleted — a subtle luminosity at full health ramping to intense emission near depletion. At each crumble pause threshold (75%, 50%, 25% remaining), a bright flash and particle burst erupt from the asteroid surface, giving a satisfying "crack" moment. When the asteroid is fully depleted, it breaks apart in a final explosion of small rocky fragments before fading away.

**Why this priority**: The depletion visual feedback is the second most-viewed element during mining (the asteroid itself). Syncing vein glow with depletion percentage and adding crumble burst effects transforms passive resource extraction into an actively rewarding feedback loop. This is the core of "mining should feel precise and deliberate" from the constitution.

**Independent Test**: Can be fully tested by mining a single asteroid from full health to depletion. Each threshold crossing produces a visible burst, and the final depletion triggers the fragment explosion — all testable in one continuous mining session.

**Acceptance Scenarios**:

1. **Given** an asteroid is at full health, **When** the player observes it while mining begins, **Then** a faint glow is visible on the asteroid's surface veins.
2. **Given** mining is actively reducing asteroid mass, **When** the depletion fraction increases, **Then** the vein glow intensity ramps proportionally (brighter at higher depletion).
3. **Given** the asteroid crosses the 75% remaining threshold (25% depleted), **When** the crumble pause triggers, **Then** a bright flash and particle burst erupt from the asteroid surface.
4. **Given** the asteroid crosses the 50% remaining threshold, **When** the crumble pause triggers, **Then** a flash and particle burst occur, visibly stronger than the 25% depletion burst.
5. **Given** the asteroid crosses the 25% remaining threshold, **When** the crumble pause triggers, **Then** a flash and particle burst occur, the most intense of the three intermediate bursts.
6. **Given** the asteroid reaches 0% remaining resources, **When** the final crumble pause completes, **Then** a burst of 8-15 small rocky fragments explodes outward from the asteroid's position before the fade-out begins.
7. **Given** fragments have spawned from a destroyed asteroid, **When** a few seconds pass, **Then** the fragments drift outward and fade, disappearing within 3 seconds.

---

### User Story 3 - Continuous Ore Collection Feedback (Priority: P3)

As a player actively mining an asteroid, I periodically see a small spray of 2-5 glowing ore chunks of various sizes pop out from the asteroid's surface and drift toward my barge's collector. These bursts appear at irregular intervals every few seconds — not on a predictable timer — giving the feel of continuous ore extraction rather than a single end-of-mining reward. Each chunk floats briefly before being gently pulled toward the barge, and a brief visual flash and satisfying sound confirm each pickup. The steady stream of chunk bursts throughout the mining session creates a sustained sense of progress and reward.

**Why this priority**: The ore collection animation is the "reward beat" of the mining loop. While mining itself should feel powerful (P1) and depletion should be readable (P2), the continuous stream of ore chunks entering the cargo hold provides ongoing satisfaction throughout the mining session rather than a single payoff at the end. This transforms abstract inventory numbers into a visceral, sustained reward.

**Independent Test**: Can be fully tested by mining a single asteroid for 15-30 seconds. Bursts of 2-5 ore chunks spawn at random intervals (3-7 seconds), drift toward the barge, and are visually absorbed — all observable during active mining. Chunks are purely cosmetic; inventory was already credited during mining.

**Acceptance Scenarios**:

1. **Given** the player is actively mining an asteroid, **When** a randomized interval of 3-7 seconds elapses, **Then** 2-5 ore chunks of various small sizes spawn at the asteroid's surface and drift outward.
2. **Given** ore chunks have spawned during mining, **When** they first appear, **Then** each chunk uses one of the two mesh variants for the targeted asteroid's ore type, varies in size, and glows in the ore's color.
3. **Given** an ore chunk exists in space, **When** a brief moment passes (0.5-1.0 seconds), **Then** the chunk begins drifting gently toward the player's barge collector point.
4. **Given** an ore chunk is being attracted to the barge, **When** it moves, **Then** the attraction is smooth and organic (gentle curve, not a straight snap).
5. **Given** an ore chunk reaches the barge collector point, **When** it arrives, **Then** it disappears with a brief collection flash effect.
6. **Given** the player stops mining, **When** mining ceases, **Then** no new ore chunks spawn, but any chunks already in flight continue their journey to the barge and are collected normally.
7. **Given** the player is mining for an extended period, **When** multiple chunks are spawned over time, **Then** the intervals between spawns vary noticeably (not metronomic) to feel organic and natural.

---

### User Story 4 - Synchronized HUD Mining Feedback (Priority: P4)

As a player actively mining, the HUD mining panel shows a progress bar indicating how depleted the current asteroid is. The progress bar fill color shifts from the ore's color at full health toward a warning red as depletion nears 100%. A subtle pulse effect on the progress bar syncs with the asteroid's vein glow, creating visual harmony between the 3D world and the 2D interface. When crumble pauses occur, the progress bar briefly flashes to reinforce the threshold event.

**Why this priority**: HUD feedback reinforces the 3D depletion visuals with precise numeric/bar information. Players who prefer UI-centric gameplay (common in EVE Online's audience) get the same quality information as players watching the asteroid. This completes the feedback loop across both viewport and interface.

**Independent Test**: Can be fully tested by mining an asteroid while watching the HUD panel. The progress bar fills, pulses, and flashes at thresholds — all independently verifiable alongside or without the 3D effects.

**Acceptance Scenarios**:

1. **Given** the player begins mining an asteroid, **When** the mining panel appears, **Then** a progress bar is visible showing 0% depletion (full bar in ore color).
2. **Given** mining is actively depleting the asteroid, **When** the depletion fraction increases, **Then** the progress bar fill advances proportionally from left to right.
3. **Given** the progress bar is filling, **When** depletion exceeds 50%, **Then** the bar color begins transitioning from the ore's color toward a warning red/orange.
4. **Given** a crumble pause threshold is crossed, **When** the event fires, **Then** the progress bar briefly flashes white before resuming its fill.
5. **Given** the asteroid is fully depleted, **When** mining stops, **Then** the progress bar shows 100% fill momentarily before the mining panel hides.

---

### User Story 5 - Spatial Audio Feedback (Priority: P5)

As a player mining an asteroid, I hear a continuous laser hum that increases in pitch as the asteroid becomes more depleted. Impact sparks produce a crackling sound at the hit point. Each crumble pause triggers a deep rumble. The final asteroid destruction produces a satisfying explosion sound. When ore chunks reach the barge, each collection plays a metallic clink. All sounds are spatialized — the laser hum comes from the beam, the sparks from the asteroid, and the clinks from the barge.

**Why this priority**: Audio is the final sensory layer. While VFX delivers immediate visual satisfaction, audio provides subconscious feedback that makes the experience feel "complete." Spatialized audio reinforces the 3D environment. However, audio is additive — the mining loop is fully functional and satisfying with visuals alone, making this the lowest-priority enhancement.

**Independent Test**: Can be fully tested by mining an asteroid with audio enabled. Each sound is independently verifiable: laser hum (continuous), impact crackling (at hit point), crumble rumble (at thresholds), explosion (at depletion), and collection clinks (at barge).

**Acceptance Scenarios**:

1. **Given** the mining laser activates, **When** the beam begins firing, **Then** a continuous laser hum sound plays, spatialized at the beam midpoint.
2. **Given** the laser hum is playing, **When** the asteroid's depletion increases, **Then** the hum's pitch rises gradually (higher depletion = higher pitch).
3. **Given** the beam is hitting the asteroid, **When** impact sparks are visible, **Then** a crackling/sizzling sound plays at the impact point, spatialized to the asteroid.
4. **Given** a crumble pause threshold is crossed, **When** the pause begins, **Then** a deep rumble sound plays at the asteroid's position.
5. **Given** the asteroid is fully depleted, **When** the fragment explosion occurs, **Then** an explosion sound plays at the asteroid's last position.
6. **Given** an ore chunk reaches the barge collector, **When** the chunk is absorbed, **Then** a metallic clink sound plays at the barge's position.
7. **Given** the player stops mining (manually or out of range), **When** the beam shuts off, **Then** the laser hum fades out over 0.2-0.5 seconds rather than cutting abruptly.

---

### Edge Cases

- What happens when the player starts mining one asteroid and switches target mid-beam? All VFX (beam, sparks, heat haze) must cleanly transition to the new target within one frame. No lingering particles from the old target.
- What happens when the asteroid is destroyed while ore chunks are still in flight? The chunks already spawned must continue their attraction trajectory toward the barge and be collected normally. No new chunks spawn after depletion.
- What happens when the player moves out of range while ore chunks are in flight? The chunks must still complete their journey to the barge and be collected. Range only affects the beam and new chunk spawning, not chunks already in flight.
- What happens when the player switches targets mid-mining? The chunk spawn timer resets for the new target. Any chunks already in flight from the previous asteroid continue to the barge normally.
- What happens when the asteroid is at a crumble pause and the player stops mining? The crumble burst VFX and rumble audio complete their animations naturally. No abrupt cutoff.
- What happens when the HUD mining panel is hidden (e.g., UI toggle)? The 3D VFX and audio must continue playing independently of HUD visibility.
- What happens when 10+ asteroids are being mined simultaneously by future NPC miners? VFX must degrade gracefully — reduce particle counts or skip distant effects. Performance must not exceed the budget.
- What happens when the camera is looking away from the mining operation? Audio should still play (spatialized), but particle systems should be culled to save GPU budget.

## Requirements *(mandatory)*

### Functional Requirements

**Mining Laser Beam VFX**

- **FR-001**: The mining laser MUST render as a continuous energy beam from the barge's mining arm origin point to the asteroid's surface hit point, updating position every frame.
- **FR-002**: The beam MUST be colored to match the ore type of the targeted asteroid, using the existing `BeamColor` from the ore type configuration.
- **FR-003**: Impact sparks MUST emit from the beam's hit point on the asteroid surface, colored to match the ore type.
- **FR-004**: A heat haze distortion effect MUST be visible near the beam origin on the mining arm when the laser is active.
- **FR-005**: All beam-related effects (beam, sparks, heat haze) MUST cease immediately when mining stops (target lost, out of range, player deactivation, or asteroid depleted).
- **FR-006**: The beam MUST visually pulse or shimmer to convey energy flow, not appear as a static line.

**Asteroid Depletion VFX**

- **FR-007**: Asteroid surface vein glow intensity MUST ramp proportionally with the depletion fraction — faint at 0% depleted, intense at 100% depleted.
- **FR-008**: At each crumble pause threshold crossing (25%, 50%, 75% depleted), a bright flash and outward particle burst MUST emit from the asteroid surface.
- **FR-009**: Crumble burst intensity MUST escalate with depletion — the 75% depletion burst is visibly stronger than the 25% depletion burst.
- **FR-010**: On final depletion (100%), a burst of 8-15 small rocky fragment particles MUST explode outward from the asteroid's position.
- **FR-011**: Rock fragments from the final explosion MUST drift outward and fade, fully disappearing within 3 seconds.
- **FR-012**: Vein glow and crumble effects MUST be synchronized with the existing depletion systems — updates must reflect the current depletion fraction every frame with no visible lag.

**Ore Collection VFX**

- **FR-013**: During active mining, purely cosmetic ore chunks MUST spawn from the asteroid's surface at randomized intervals of 3-7 seconds. Each spawn event produces 2-5 chunks of various small sizes. Chunks do NOT affect inventory — resources are already credited continuously during mining via yield ticks.
- **FR-014**: Ore chunks MUST use mesh variants matching the targeted asteroid's ore type (from the existing 2-mesh-per-ore-type visual mapping).
- **FR-015**: Ore chunks MUST glow in the ore type's color.
- **FR-016**: After a brief initial drift (0.5-1.0 seconds), ore chunks MUST be attracted toward the player's barge collector point with smooth, curved motion.
- **FR-017**: When an ore chunk reaches the barge collector, it MUST disappear with a brief collection flash.
- **FR-018**: Each ore chunk MUST complete its journey (spawn → drift → attract → collect) within 5 seconds.
- **FR-018a**: The spawn interval MUST be randomized per event (not fixed) to produce an organic, non-metronomic cadence.
- **FR-018b**: When mining stops, no new chunks MUST spawn, but any chunks already in flight MUST continue to the barge and be collected normally.

**HUD Mining Feedback**

- **FR-019**: The mining HUD panel MUST display a progress bar showing the current asteroid's depletion percentage.
- **FR-020**: The progress bar fill color MUST transition from the ore's color (low depletion) to warning red/orange (high depletion).
- **FR-021**: The progress bar MUST pulse subtly in sync with the asteroid vein glow.
- **FR-022**: When a crumble pause threshold is crossed, the progress bar MUST briefly flash to reinforce the event.
- **FR-023**: When the asteroid is fully depleted, the progress bar MUST show 100% momentarily before the mining panel hides.

**Audio Feedback**

- **FR-024**: A continuous, looping laser hum MUST play while the mining laser is active, spatialized at the beam midpoint.
- **FR-025**: The laser hum pitch MUST increase gradually as the asteroid becomes more depleted.
- **FR-026**: Impact sparks MUST produce a crackling/sizzling sound spatialized at the asteroid's surface.
- **FR-027**: Each crumble pause threshold crossing MUST trigger a deep rumble sound at the asteroid's position.
- **FR-028**: The final asteroid destruction MUST trigger an explosion sound at the asteroid's last position.
- **FR-029**: Each ore chunk collection at the barge MUST trigger a metallic clink sound at the barge's position.
- **FR-030**: When mining stops, the laser hum MUST fade out over 0.2-0.5 seconds rather than cutting abruptly.

**Performance & Compatibility**

- **FR-031**: All new VFX and audio MUST run within a combined budget of 1.5 ms per frame during active mining scenes.
- **FR-032**: The existing 2 ms asteroid field rendering budget MUST NOT be exceeded with new depletion VFX active.
- **FR-033**: All new effects MUST be compatible with the existing DOTS/ECS asteroid and mining systems with zero regressions.
- **FR-034**: Particle systems MUST cull when off-screen to save GPU budget.
- **FR-035**: VFX MUST support simultaneous mining of multiple asteroids (future-proofing) without exceeding performance budgets.

### Key Entities

- **MiningVFXConfig**: Configuration for all mining laser visual effects — beam width, pulse speed, spark emission rate, heat haze intensity, and ore-type color overrides. Designer-editable.
- **DepletionVFXConfig**: Configuration for asteroid depletion effects — vein glow ramp curve, crumble burst particle count per threshold, fragment count on final explosion, fragment lifetime. Designer-editable.
- **OreChunkConfig**: Configuration for ore collection chunks — spawn interval range (3-7 seconds), chunks per spawn (2-5), chunk size variance range, initial drift duration, attraction speed curve, collection flash duration. Designer-editable.
- **MiningAudioConfig**: Configuration for all mining audio cues — references to audio clips (laser hum, spark crackle, crumble rumble, explosion, collection clink), pitch ramp range for laser hum, fade-out duration. Designer-editable.

## Assumptions

- The existing `MiningBeamView` LineRenderer will be replaced or augmented with the new beam VFX. The LineRenderer may be retained as a fallback or debug visualization.
- The barge prefab has (or will have) a clearly defined "mining arm origin" transform that serves as the beam start point. If not, a child transform will be added to each barge variant.
- The barge prefab has (or will have) a "collector point" transform for ore chunk attraction targets. If not, a child transform will be added to each barge variant.
- Audio clips for all sound effects (laser hum, spark crackle, rumble, explosion, clink) will be created as placeholder procedural sounds or sourced from Unity's built-in audio tools. No paid audio asset purchases are required for this spec.
- The existing crumble pause bitmask (`CrumbleThresholdsPassed`) on `AsteroidComponent` provides sufficient event detection for triggering VFX bursts — no new ECS components are needed for threshold detection itself.
- VFX Graph is preferred for particle effects where DOTS/Burst compatibility is beneficial. Fallback to legacy Particle System is acceptable where VFX Graph integration proves impractical.
- The ore chunk attraction system uses simple managed-code movement (MonoBehaviour `Update` or similar) rather than DOTS physics, since at most ~10 chunks are in flight simultaneously (2-5 spawn every 3-7 seconds, each lives ~5 seconds) and chunks are short-lived view-layer objects.
- The vein glow effect will use emission intensity modulation on the asteroid's existing material (via `URPMaterialPropertyBaseColor` alpha channel or a new emission property), not a separate overlay mesh.
- All new ScriptableObject configs will be created under `Assets/Features/Mining/Data/` following existing naming conventions.

## Out of Scope

- New paid VFX or audio asset packs — all effects use built-in Unity tools and procedural/placeholder sounds.
- Station interaction VFX or docking effects.
- Full inventory animation or cargo bay interior visuals.
- Advanced Shader Graph work beyond emission intensity modulation and simple alpha tweaks.
- NPC mining VFX — this spec covers player-controlled mining only.
- Screen-space post-processing effects (bloom, lens flare) specific to mining — these are global rendering settings.
- Controller haptic/vibration feedback — deferred to future input enhancement spec.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Mining laser beam, impact sparks, and heat haze are all visible within 1 frame of mining activation — zero perceptible delay between action and visual response.
- **SC-002**: Players can visually identify asteroid depletion state at a glance — vein glow intensity clearly distinguishes a 25%-depleted asteroid from a 75%-depleted one at mining range (under 50 units).
- **SC-003**: Each crumble pause threshold crossing produces a visible flash-and-burst effect lasting 0.3-0.5 seconds, clearly noticeable without watching the HUD.
- **SC-004**: Final asteroid destruction produces a satisfying fragment explosion of 8-15 particles that drift and fade within 3 seconds.
- **SC-005**: Ore chunks spawn continuously during active mining at random intervals of 3-7 seconds. Each individual chunk completes its journey (spawn → drift → attract → collect) within 5 seconds.
- **SC-006**: HUD mining progress bar updates every frame in sync with asteroid depletion — no visible desync between 3D vein glow and 2D progress bar.
- **SC-007**: All 6 audio cues (laser hum, spark crackle, crumble rumble, explosion, collection clink, hum fade-out) play at correct spatial positions and timing.
- **SC-008**: Total VFX + audio overhead stays under 1.5 ms per frame during peak mining activity (1 active beam + 1 depleting asteroid + up to 10 ore chunks in flight simultaneously).
- **SC-009**: Asteroid field rendering remains under 2 ms with depletion VFX active on up to 10 simultaneously visible asteroids.
- **SC-010**: All existing tests (21 at time of writing) pass with zero regressions. No pink materials, no GC spikes in hot paths.
- **SC-011**: The complete mining feedback loop (beam on, mine, threshold bursts, depletion explosion, ore collection) runs at a steady 60 FPS on mid-range hardware.
