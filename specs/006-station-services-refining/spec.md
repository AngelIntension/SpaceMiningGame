# Feature Specification: Station Services Menu & Data-Driven Refining

**Feature Branch**: `006-station-services-refining`
**Created**: 2026-03-01
**Status**: Draft
**Input**: User description: "Close the full mining-to-economy loop by implementing the Station Services Menu and data-driven refining system on top of Spec 005's ore system."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cargo Transfer Between Ship and Station (Priority: P1)

As a pilot docked at a station, I want to transfer ore and materials between my ship's cargo hold and the station's storage so that I can deposit resources for selling or refining and retrieve processed materials back to my ship.

**Why this priority**: Cargo Transfer is the gateway to all station economy actions. Without it, no other station service (selling, refining) can function because the station economy rule requires items to be in station storage first. This is the foundational slice that enables the entire economy loop.

**Independent Test**: Can be fully tested by docking at a station, opening the Station Services Menu, selecting Cargo Transfer, moving ore from ship to station (and back), and verifying quantities update correctly in both inventories.

**Acceptance Scenarios**:

1. **Given** a pilot docked at a station with 50 Luminite in ship cargo, **When** the player opens Cargo Transfer and transfers 30 Luminite to station storage, **Then** ship cargo shows 20 Luminite and station storage shows 30 Luminite.
2. **Given** station storage contains 10 Ferrox, **When** the player transfers 5 Ferrox back to ship cargo, **Then** station storage shows 5 Ferrox and ship cargo increases by 5 Ferrox.
3. **Given** ship cargo is at maximum volume capacity, **When** the player attempts to transfer materials from station to ship, **Then** the system prevents the transfer and displays a "Cargo Full" message.
4. **Given** a pilot docked at a station, **When** the player transfers any amount of ore from ship to station, **Then** the transfer always succeeds because station storage has unlimited capacity.
5. **Given** a pilot docked at a station, **When** the player opens Cargo Transfer, **Then** both ship cargo and station storage inventories are displayed side-by-side with item names, quantities, and volume information.

---

### User Story 2 - Sell Resources at Station (Priority: P2)

As a pilot docked at a station, I want to sell ore and raw materials from the station's storage for credits so that I can accumulate wealth and fund repairs and upgrades.

**Why this priority**: Selling is the simplest economic transaction and the first step in giving mined resources monetary value. It directly completes the mine-to-profit loop and provides the currency needed for other services (repair).

**Independent Test**: Can be tested by first transferring ore to station storage via Cargo Transfer, then opening Sell Resources, selecting a resource type and quantity, confirming the sale, and verifying credits increase and station storage decreases.

**Acceptance Scenarios**:

1. **Given** station storage contains 20 Luminite and the player has 0 credits, **When** the player selects Sell Resources, chooses 10 Luminite, and confirms the sale, **Then** station storage shows 10 Luminite, and the player's credit balance increases by the expected amount (10 x Luminite base value).
2. **Given** the player is on the Sell Resources screen with 20 Auralite in station storage, **When** the player adjusts the quantity slider, **Then** a live credit preview updates in real-time showing the total payout before confirmation.
3. **Given** station storage has 0 resources, **When** the player opens Sell Resources, **Then** the panel displays "No items available for sale" and the sell button is disabled.
4. **Given** the player selects a quantity and presses "Sell", **When** the confirmation dialog appears and the player confirms, **Then** a transaction visual effect and audio cue play, credits are awarded, and resources are removed from station storage.

---

### User Story 3 - Refine Ores Into Raw Materials (Priority: P3)

As a pilot docked at a station, I want to queue refining jobs that convert ore from station storage into raw materials over time so that I can produce higher-value materials for selling or future crafting.

**Why this priority**: Refining is the core economic depth mechanic that transforms raw ore into more valuable materials. It introduces time-based gameplay and strategic decisions (which ore to refine, when to check back). Depends on Cargo Transfer being functional first.

**Independent Test**: Can be tested by transferring ore to station storage, opening Refine Ores, selecting an ore type and quantity, starting a refining job, waiting for the job timer to complete, and verifying raw materials appear in station storage.

**Acceptance Scenarios**:

1. **Given** station storage contains 100 Luminite, the station has available refining job slots, and the player has sufficient credits, **When** the player selects Refine Ores, chooses Luminite, sets quantity to 50, and starts the job, **Then** 50 Luminite is removed from station storage, the refining cost is deducted from the player's credit balance, and a new job appears in the active jobs list with a progress bar and estimated completion time.
2. **Given** a refining job is running for 50 Luminite, **When** the processing time elapses, **Then** the job transitions to "Completed" status in the job list (visually distinct from active jobs), a completion audio cue plays, and a notification appears if the player is docked. Materials are NOT yet added to station storage.
3. **Given** a completed refining job is visible in the job list, **When** the player clicks the completed job, **Then** a summary window opens showing all generated materials with names, quantities, and yield variance results.
4. **Given** the job summary window is open, **When** the player closes it, **Then** the generated materials are transferred to station storage and the job is removed from the list, freeing the slot.
5. **Given** all refining job slots are occupied, **When** the player attempts to start a new refining job, **Then** the "Start Job" button is disabled and a message indicates no available slots.
6. **Given** a refining job is in progress, **When** the player undocks and later re-docks, **Then** the job continues running in the background and its updated progress is accurately displayed.
7. **Given** the Refine Ores panel is open, **When** the player views the job list, **Then** each active job shows: ore type, quantity, progress bar, elapsed/remaining time, ETA, and expected output preview.
8. **Given** the player has insufficient credits to cover the refining job cost, **When** the player attempts to start a refining job, **Then** the "Start Job" button is disabled and a message indicates "Insufficient credits" along with the required cost.
9. **Given** the player is configuring a refining job, **When** the player adjusts the ore quantity, **Then** a live cost preview updates in real-time showing the total credit cost and expected outputs before confirmation. If the player's credit balance can only afford a partial quantity, the maximum affordable quantity is displayed as a hint.
10. **Given** the Refine Ores panel is open, **When** the player selects an ore type, **Then** the panel displays the ore's refining outputs (material names and base quantities per unit) as defined in the ore definition — no recipe selection is needed.

---

### User Story 4 - Basic Ship Repair (Priority: P4)

As a pilot docked at a station with a damaged hull, I want to pay credits to fully repair my ship so that I can return to mining at full integrity.

**Why this priority**: Repair is a straightforward credit sink that gives meaning to the credits earned from selling. It directly affects ship survivability and closes the repair loop. Simpler than refining but depends on the credit system from selling.

**Independent Test**: Can be tested by docking with a ship at reduced hull integrity, opening Basic Repair, viewing the cost, confirming repair, and verifying hull integrity returns to 100% and credits are deducted.

**Acceptance Scenarios**:

1. **Given** a pilot is docked with hull integrity at 60% and sufficient credits, **When** the player selects Basic Repair and confirms, **Then** hull integrity is restored to 100%, the repair cost is deducted from credits, and a repair visual effect and audio cue play.
2. **Given** hull integrity is already at 100%, **When** the player opens Basic Repair, **Then** the repair button is disabled and a message reads "Hull integrity is at maximum."
3. **Given** the player has insufficient credits for repair, **When** the player opens Basic Repair, **Then** the repair cost is displayed in red, and the confirm button is disabled with a message "Insufficient credits."

---

### User Story 5 - Station Services Menu Auto-Open and Undock (Priority: P5)

As a pilot completing magnetic docking at a station, I want the Station Services Menu to automatically open with all available services, and I want to undock via the menu to return to space.

**Why this priority**: This is the container and navigation shell for all other station services. The auto-open behavior and undock action are essential UI plumbing, but the individual service panels (above) deliver the actual gameplay value.

**Independent Test**: Can be tested by flying to a station, completing the docking sequence, verifying the menu appears automatically with all service buttons visible, then pressing Undock and verifying the ship is released and full flight control is restored.

**Acceptance Scenarios**:

1. **Given** a pilot has just completed magnetic docking at a station, **When** the docking sequence finishes, **Then** the Station Services Menu opens automatically showing: Cargo Transfer, Sell Resources, Refine Ores, Basic Repair, Undock, and the player's current credit balance.
2. **Given** the Station Services Menu is open, **When** the player selects Undock, **Then** the menu closes, the magnetic lock releases, the ship moves to clearance distance, and full ship control is restored.
3. **Given** the Station Services Menu is open with an active refining job, **When** the player selects Undock, **Then** the menu closes, undocking proceeds normally, and the refining job continues running in the background.
4. **Given** the player is navigating within a service sub-panel (e.g., Cargo Transfer), **When** the player presses a back/close button, **Then** the view returns to the main Station Services Menu without undocking.
5. **Given** the player is viewing any sub-panel (Cargo Transfer, Sell Resources, Refine Ores, or Basic Repair), **When** a credit-affecting action occurs (sale, repair, or refining job start), **Then** the credit balance indicator updates immediately and remains visible at all times.

---

### User Story 6 - Refining Job Notifications (Priority: P6)

As a pilot who has queued refining jobs, I want to receive notifications when jobs complete so that I know when to return to the station to collect my refined materials.

**Why this priority**: Notifications enhance the refining loop by informing the player of job completion without requiring constant monitoring. Lower priority because the core loop functions without them — the player can check manually.

**Independent Test**: Can be tested by starting a refining job, undocking, and verifying a notification appears (in-world or HUD) when the job completes.

**Acceptance Scenarios**:

1. **Given** a refining job completes while the player is docked at the same station, **Then** a completion notification appears in the HUD prompting the player to review the completed job in the Refine Ores panel.
2. **Given** a refining job completes while the player is undocked or at a different station, **Then** a notification indicator appears in the HUD showing pending completed jobs awaiting review, and the jobs are reviewable upon re-docking at that station.

---

### Edge Cases

- What happens when the player tries to sell or refine with an empty station storage? The respective panels display "No items available" and action buttons are disabled.
- What happens when a refining job completes? The job transitions to "Completed" status and remains in the job list. Materials are held until the player reviews the job by clicking it and closing the summary window, at which point they are transferred to station storage (unlimited capacity, always succeeds).
- What happens if the player has multiple completed jobs? All completed jobs are shown in the job list with a distinct visual treatment. The player can review and collect them one at a time in any order.
- What happens if the player docks at a station with no refinery service? The Refine Ores button is disabled or hidden based on the station's configured available services.
- What happens if the player spams the transfer/sell/refine buttons rapidly? All state transitions go through pure reducers with sequential dispatch — double-processing is prevented by the immutable state pattern.
- What happens when a refining output produces fractional quantities or a negative result? Output quantities are floored at 0 (no negative materials) and rounded down to the nearest whole unit; fractional remainders are discarded.
- What happens if the player has exactly 0 credits and tries to repair? The repair confirm button is disabled with an "Insufficient credits" message. Partial repair is not supported — it is all-or-nothing to full integrity.
- What happens if a refining job was started and the game session ends? Job state must persist and resume correctly on the next session. (Note: Save/load system is currently out of scope for Phase 0; job persistence within a single game session across dock/undock cycles is required. Cross-session persistence will be addressed when save/load ships.)
- What happens when transferring a quantity that exceeds available stock? The system caps the transfer at the available quantity. The UI prevents selecting more than is available.
- What happens if the player has enough ore but not enough credits to refine? The "Start Job" button is disabled with an "Insufficient credits" message showing the required amount. The player must sell resources first to fund refining.
- What happens if the player has credits for a partial quantity but not the full selected amount? The UI shows the maximum affordable quantity as a hint (FR-057). The player can reduce the quantity to match their budget.

## Requirements *(mandatory)*

### Functional Requirements

**Station Services Menu Shell**

- **FR-001**: System MUST automatically open the Station Services Menu when magnetic docking completes at any station.
- **FR-002**: The Station Services Menu MUST display exactly five top-level options: Cargo Transfer, Sell Resources, Refine Ores, Basic Repair, and Undock.
- **FR-050**: The Station Services Menu MUST display the player's current credit balance in a persistently visible indicator (header or footer area) that is always visible regardless of which sub-panel is active.
- **FR-051**: The credit balance indicator MUST update in real-time whenever credits change (from selling, repair costs, or refining job costs) without requiring the player to navigate away and back.
- **FR-003**: Each top-level option MUST open a dedicated sub-panel within the menu. A back/close action MUST return to the main menu without undocking.
- **FR-004**: The Undock option MUST close the menu, release the magnetic lock, and restore full ship control.
- **FR-005**: Station service options MUST be enabled or disabled based on the station's configured available services (e.g., a station without a refinery disables Refine Ores).
- **FR-006**: The Station Services Menu MUST close automatically when undocking begins (whether triggered by the Undock button or any other undocking mechanism).

**Station Storage**

- **FR-007**: Each station MUST maintain its own independent storage inventory, separate from the player's ship cargo.
- **FR-008**: Station storage MUST have unlimited capacity — no volume or slot limits. Items can always be transferred to or stored in station storage without capacity restrictions.
- **FR-009**: Station storage MUST persist across dock/undock cycles within a game session — items left in station storage remain there when the player undocks and returns.
- **FR-010**: Station storage MUST support the same item types as ship cargo (ores and raw materials).

**Cargo Transfer**

- **FR-011**: Cargo Transfer MUST display the ship cargo hold and station storage side-by-side in a bidirectional interface.
- **FR-012**: The player MUST be able to transfer any item type in any quantity (1 to all) from ship to station or station to ship.
- **FR-013**: Transfer quantity MUST be selectable via quantity slider or direct numeric input, capped at the source's available quantity.
- **FR-014**: Transfers from station to ship MUST be rejected with an appropriate message when the ship cargo lacks sufficient capacity (volume or slots). Transfers from ship to station always succeed (unlimited station capacity).
- **FR-015**: Both inventories MUST update immediately upon transfer confirmation, reflecting new quantities and volume usage.

**Sell Resources**

- **FR-016**: Sell Resources MUST only operate on items currently in station storage. Ship cargo items are not directly sellable.
- **FR-017**: The player MUST be able to select any ore or raw material type and any quantity from 1 to all available units in station storage.
- **FR-018**: A live credit preview MUST update in real-time as the player adjusts the sale quantity, showing the total payout before confirmation.
- **FR-019**: A confirmation dialog MUST appear before finalizing any sale, showing item type, quantity, and total credit value.
- **FR-020**: Upon confirmed sale, the system MUST remove the sold items from station storage, add credits to the player's balance, and play a transaction visual effect and audio cue.
- **FR-021**: The player MUST have a persistent credit balance that increases from sales and decreases from purchases (repair costs and refining job costs). The starting balance MUST be configurable via a designer-editable setting (default: 0).

**Refine Ores**

- **FR-022**: Refine Ores MUST only operate on ore items currently in station storage. Ship cargo ore is not directly refinable.
- **FR-023**: Refining MUST be asynchronous and time-based, not instant. Processing time MUST equal quantity multiplied by the ore type's base processing time per unit, modified by the station's refining speed multiplier.
- **FR-024**: Each station MUST have a configurable maximum number of concurrent refining job slots.
- **FR-025**: The player MUST be able to select an ore type and quantity to start a refining job, but only when at least one job slot is available and the player has sufficient credits. Refining outputs are determined automatically by the ore definition — no recipe selection is required.
- **FR-026**: When a refining job starts, the input ore MUST be immediately removed from station storage and the refining cost MUST be immediately deducted from the player's credit balance.
- **FR-027**: Refining jobs MUST continue running after the player undocks. Jobs are station-resident, not player-resident.
- **FR-028**: Upon job completion, the job MUST transition to a "Completed" status and remain in the job list until the player reviews it. Output materials are NOT automatically added to station storage.
- **FR-052**: The player MUST be able to click a completed job to open a summary window displaying all generated materials (names, quantities, yield variance results).
- **FR-053**: When the player closes the job summary window, the generated materials MUST be transferred to station storage and the job MUST be removed from the job list.
- **FR-054**: A completed job MUST immediately free its refining slot upon completion, regardless of whether the player has reviewed it. New jobs can start even while completed jobs await review.
- **FR-029**: The Refine Ores panel MUST display: current/total active job slots used (completed jobs do not count toward slot usage), a list of active jobs with live progress bars (remaining time, ETA, expected output preview), and completed jobs awaiting review (visually distinct, clickable).
- **FR-030**: The "Start Job" button MUST be disabled when no job slots are available, the selected ore quantity is 0, or the player has insufficient credits to cover the job cost.
- **FR-031**: All refining parameters (processing time per ore, job slots, yields, variance ranges, and refining costs) MUST be 100% data-driven and editable in the editor with no code changes.
- **FR-047**: Each ore definition MUST define a configurable refining credit cost per unit. The total job cost MUST equal the cost per unit multiplied by the input quantity.
- **FR-048**: The Refine Ores panel MUST display a live cost preview that updates in real-time as the player adjusts the ore quantity, showing the total credit cost alongside the player's current balance.
- **FR-049**: The system MUST prevent starting a refining job when the player's credit balance is less than the total job cost.
- **FR-057**: When the player's credit balance cannot cover the full selected refining quantity, the Refine Ores panel MUST display the maximum affordable quantity as a hint (floor of credits / cost per unit).

**Ore Refining Outputs (embedded in Ore Definitions)**

- **FR-032**: Each ore definition MUST contain a list of refining outputs, where each output specifies: a raw material type, a base yield quantity per input unit, and a yield variance range (additive min/max offsets to the base yield per unit).
- **FR-033**: Output yield per output type per job MUST be calculated using per-unit rolling: for each input unit, independently compute (base yield per unit + a random additive offset within the configured varianceMin/varianceMax range), then sum the results across all input units. Each output type in the list is calculated independently. Final output quantities MUST be floored at 0 (no negative materials) and rounded down to the nearest whole number.
- **FR-034**: New ore types and raw materials MUST be addable by creating new editor assets with zero code changes. Adding a new ore with refining outputs requires only editing the ore definition asset — no separate recipe asset is needed.

**Mandatory Starter Content**

- **FR-055**: The following six raw material definitions MUST be created as ready-to-use ScriptableObject assets: Luminite Ingots, Energium Dust, Ferrox Slabs, Conductive Residue, Auralite Shards, Quantum Essence.
- **FR-056**: The existing three ore definitions (Luminite, Ferrox, Auralite) MUST be extended with the following refining output configurations:
  - **Luminite**: Luminite Ingots (baseYield: 4, varianceMin: -1, varianceMax: +2) and Energium Dust (baseYield: 2, varianceMin: 0, varianceMax: +1).
  - **Ferrox**: Ferrox Slabs (baseYield: 3, varianceMin: -1, varianceMax: +1) and Conductive Residue (baseYield: 3, varianceMin: 0, varianceMax: +2).
  - **Auralite**: Auralite Shards (baseYield: 2, varianceMin: 0, varianceMax: +1) and Quantum Essence (baseYield: 1, varianceMin: -1, varianceMax: +1).

**Raw Materials**

- **FR-035**: A raw material definition MUST exist as a configurable asset type, with at minimum: unique ID, display name, icon, description, base value, and volume per unit.
- **FR-036**: Raw materials MUST be storable in both station storage and ship cargo, using the same inventory mechanisms as ore.

**Basic Repair**

- **FR-037**: Basic Repair MUST restore the active ship's hull integrity to 100% in a single transaction.
- **FR-038**: Repair cost MUST be calculated as: damage amount (1.0 - current integrity) multiplied by the station's configurable repair cost per HP, using ceiling rounding to the nearest integer (no fractional credits).
- **FR-039**: The system MUST prevent repair when the player has insufficient credits or when hull integrity is already at 100%.
- **FR-040**: Upon confirmed repair, hull integrity MUST update immediately, credits MUST be deducted, and a visual effect and audio cue MUST play.

**Station Configuration**

- **FR-041**: Each station MUST be configurable via an editor asset defining: maximum concurrent refining job slots, refining speed multiplier, and repair cost per HP (station capabilities). Service availability (which menu buttons are enabled) is determined by the pre-existing `StationData.AvailableServices` field using the string mapping: `"Refinery"` → Refine Ores, `"Market"` → Sell Resources, `"Repair"` → Basic Repair, `"Cargo"` → Cargo Transfer.
- **FR-042**: Station configuration MUST be assignable per station instance, allowing different stations to offer different service tiers.

**Visual and Audio Feedback**

- **FR-043**: Each station service action (transfer, sell, refine start, refine complete, repair) MUST have accompanying visual effects and audio cues consistent with the existing sci-fi HUD aesthetic.
- **FR-044**: Refining job completion while docked MUST trigger both a visual notification and audio alert.

**Player Documentation**

- **FR-045**: The HOWTOPLAY.md file MUST be updated to cover all new station services: Cargo Transfer, Sell Resources, Refine Ores, and Basic Repair.
- **FR-046**: A changelog entry MUST document all features delivered in this spec.

### Key Entities

- **StationStorage**: A per-station inventory holding ore and raw materials, with unlimited capacity. Independent from ship cargo. Persists across dock/undock within a session.
- **PlayerCredits**: A numeric balance representing the player's currency, increased by selling resources and decreased by repair and refining costs. Starting balance is configurable via a designer-editable setting (default: 0).
- **RefiningJob**: A time-based process with lifecycle: Active → Completed → Collected (removed). Tracks: input ore type, input quantity, start time, total duration, progress, generated outputs (calculated on completion with yield variance), and credit cost paid. Belongs to a specific station. Completed jobs free their slot immediately but hold generated materials until the player reviews and collects them via the summary window. Outputs are derived from the ore definition's refining output list.
- **OreDefinition (extended)**: The existing ore definition asset (from Spec 005) extended with refining parameters: a list of refining outputs (each specifying a raw material type, base yield per unit, and yield variance min/max), a refining credit cost per unit, and the existing base processing time per unit. All refining behavior for an ore is fully described by its definition — no separate recipe asset exists.
- **RawMaterialDefinition**: A designer-authored asset defining a processed material type: unique ID, display name, icon, description, base market value, volume per unit. Six starter assets required: Luminite Ingots, Energium Dust, Ferrox Slabs, Conductive Residue, Auralite Shards, Quantum Essence.
- **StationServicesConfig**: A designer-authored asset defining a station's service capabilities: available services list, max concurrent refining slots, refining speed multiplier, and repair cost per HP.

## Non-Functional Requirements

- **NFR-001**: All station service state transitions MUST use pure reducer functions with immutable state records. No direct mutation of game state.
- **NFR-002**: Station service UI MUST render at 60 FPS with no frame hitches above 2ms during panel transitions or inventory display updates.
- **NFR-003**: Refining job timers MUST be accurate to within 0.1 seconds of the configured processing time.
- **NFR-004**: The Station Services Menu visual design MUST be consistent with the existing sci-fi HUD aesthetic (dark panels, cyan accents, monospace typography) established in Spec 003.
- **NFR-005**: All new ScriptableObject types MUST be creatable via Unity's Create Asset menu with descriptive category paths.
- **NFR-006**: Zero regressions on existing features: ship controls, mining, camera, docking, asteroid generation, HUD, and VFX.
- **NFR-007**: All pure reducers, refining math, and inventory operations MUST have unit test coverage.

## Assumptions

- **Credit system scope**: Credits are a simple numeric balance for this spec. Advanced economy features (dynamic pricing, NPC trading, market orders) are deferred to Phase 3 / Spec for Economy system.
- **Sale prices**: Items sell at their `BaseValue` defined in OreDefinition or RawMaterialDefinition. Dynamic pricing based on supply/demand is out of scope.
- **Station storage capacity**: Station storage is unlimited — no overflow scenario exists. This simplifies the economy loop and removes storage management friction. Capacity limits may be introduced in a future iteration if gameplay balance demands it.
- **Partial repair**: Not supported. Repair is all-or-nothing to 100% integrity. Partial repair adds UI complexity for minimal gameplay benefit at this stage.
- **Job cancellation**: Players cannot cancel in-progress refining jobs in this spec. The input ore is consumed upon job start. Job cancellation may be added in a future iteration.
- **Cross-session persistence**: Refining jobs persist within a single game session (across dock/undock). Cross-session persistence (save/load) is out of scope until the save system is implemented.
- **Single-player only**: All station storage and refining jobs are player-scoped. No shared/contested storage.

## Clarifications

### Session 2026-03-01

- Q: What credit balance does a new player start with? → A: Configurable via a designer-editable setting (default: 0).
- Q: Does a refining recipe accept single or multiple ore types as input? → A: Single ore type per recipe (one ore in, one or more materials out). Multi-input recipes deferred to future crafting system.
- Q: Should station storage have a capacity limit? → A: No — station storage is unlimited. Removes overflow complexity and storage management friction.
- Q: Are refining recipes separate selectable assets or embedded in ore definitions? → A: Embedded in ore definitions. Each ore defines its own refining outputs (produced materials, base quantities, yield variance, cost per unit). No separate recipe entity exists — no recipe selection UI needed.
- Q: What happens when a refining job finishes processing? → A: Job transitions to "Completed" status and stays in the job list. Player clicks to open a summary window showing generated materials. Closing the summary transfers materials to station storage and removes the job.
- Q: Does a completed-but-unreviewed job still occupy a refining slot? → A: No — slots are freed immediately on completion. New jobs can start while completed jobs await review.
- Q: What are the mandatory starter raw materials and refining configurations? → A: 6 raw materials (Luminite Ingots, Energium Dust, Ferrox Slabs, Conductive Residue, Auralite Shards, Quantum Essence) with concrete per-ore refining yields and additive variance offsets. Variance model is additive (base + offset), not multiplicative.
- Q: How is yield variance applied — single roll per job or per-unit? → A: Per-unit rolling. Each input unit gets an independent random offset roll; results are summed. Larger batches trend toward average yields (EVE-style).
- Q: Should credits be integer or floating-point? → A: Integer (`int`). No fractional credits. All prices (sell values, refining costs, repair costs) are also whole numbers. Repair cost uses ceiling rounding from the float integrity calculation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Players can complete the full mine-to-sell loop (mine asteroid, dock, transfer ore to station, sell for credits) in under 5 minutes on a fresh session.
- **SC-002**: Players can start a refining job and observe correct completion with output materials appearing in station storage, with processing time matching the configured ore parameters within 0.1 second accuracy.
- **SC-003**: Players can perform at least 10 sequential cargo transfers (ship-to-station and station-to-ship) without any inventory desync, lost items, or UI staleness.
- **SC-004**: Designers can add a completely new ore type (with embedded refining outputs) and raw material by creating editor assets alone — zero code changes required, and the new content appears correctly in all station service panels.
- **SC-005**: Players can undock with an active refining job, fly and mine, re-dock at the same station, and see accurate job progress reflecting elapsed time.
- **SC-006**: Players can repair a damaged ship in under 3 interactions (open repair, view cost, confirm) with immediate hull integrity feedback.
- **SC-007**: All station service interactions provide visual and audio feedback within 0.5 seconds of the triggering action.
- **SC-008**: The Station Services Menu renders and transitions between sub-panels at 60 FPS with no perceptible frame hitches on mid-range hardware.
- **SC-009**: All existing features (ship flight, mining, camera, docking, asteroid field, HUD) continue to function identically with zero regressions.
- **SC-010**: Player-facing documentation (HOWTOPLAY.md) accurately describes all new station services, enabling a new player to use every station feature without external guidance.
