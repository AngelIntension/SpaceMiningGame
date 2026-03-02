# How to Play VoidHarvest

VoidHarvest is a 3D space mining simulator inspired by EVE Online.
Pilot your mining barge through procedural asteroid fields, harvest
valuable ores, dock at stations, and build your fortune in the void.

## Getting Started

Launch the game from the Unity Editor by opening the **GameScene** and
pressing Play. You start in a small mining barge floating in an
asteroid field near a mining relay station. Your goal: select
asteroids, mine them for ore, and fill your cargo hold.

The controls are mouse-driven like EVE Online. Left-click to select
targets, right-click for a radial context menu, and use the keyboard
for manual thrust when you need fine control.

## Controls

### Mouse

| Action | Input | Description |
|--------|-------|-------------|
| Select Target | Left-click | Click an asteroid or station to select it |
| Fly to Point | Double-click | Double-click empty space to auto-fly toward that point |
| Clear Selection | Left-click empty space | Deselect your current target |
| Radial Menu | Right-click (quick tap) | Open the context menu for your selected target |
| Orbit Camera | Right-click + drag | Hold right-click and move the mouse to rotate the camera around your ship |
| Free Look | Middle-click (hold) | Hold for unrestricted camera viewing |
| Zoom In/Out | Scroll wheel | Zoom the camera closer to or farther from your ship |

### Keyboard — Ship Thrust

| Action | Key | Description |
|--------|-----|-------------|
| Forward | W | Accelerate forward |
| Backward | S | Accelerate backward (brake/reverse) |
| Strafe Left | A | Slide left |
| Strafe Right | D | Slide right |
| Roll Left | Q | Rotate counter-clockwise |
| Roll Right | E | Rotate clockwise |

Keyboard thrust is a supplementary control layer. Using any thrust
key while auto-piloting (from double-click or radial menu commands)
will immediately cancel the auto-pilot and give you manual control.

### Hotbar

| Key | Module |
|-----|--------|
| 1 | Mining Laser (toggle on/off) |
| 2–8 | Reserved for future modules |

Press **1** with an asteroid selected to toggle your mining laser.

### Menu & UI

| Action | Key |
|--------|-----|
| Close Menu / Cancel | Escape |
| Confirm Selection | Enter or Space |
| Navigate Menus | WASD or Arrow Keys |
| Scroll | Scroll Wheel |

## Ship Piloting

Your ship uses Newtonian 6-degrees-of-freedom physics with inertia
and damping. When you thrust forward and release the key, your ship
will gradually slow down rather than stopping instantly.

### Movement Tips

- **Forward/backward** (W/S) controls your main engines.
- **Strafing** (A/D) lets you sideslip without changing your facing.
- **Rolling** (Q/E) rotates your ship around its forward axis.
- Your ship has linear and angular damping, so it will naturally slow
  and stop spinning when you release the controls.

### Auto-Pilot

You have two ways to engage auto-pilot:

1. **Double-click** in empty space to fly toward that point.
2. **Right-click** a selected target and choose **Approach**, **Orbit**,
   or **Keep at Range** from the radial menu.

Auto-pilot handles thrust and alignment automatically. Press any
thrust key to regain manual control at any time.

### Camera

The camera orbits your ship in 3rd-person and responds to your speed:

- At high speed, it pulls back for a wider view.
- When stationary or mining, it moves closer.
- Hold **right-click + drag** to manually orbit the camera.
- Use the **scroll wheel** to zoom in or out.
- Hold **middle-click** for free-look mode (does not affect your
  ship's heading).

After manually zooming, the camera waits about 2 seconds before
resuming speed-based auto-zoom.

## Mining

Mining is the core gameplay loop. You extract ore from asteroids
using your ship's mining laser.

### How to Mine

1. **Left-click** an asteroid to select it.
2. Either press **1** to toggle the mining laser, or **right-click**
   and choose **Mine** from the radial menu.
3. Your mining beam fires automatically. Ore accumulates in your
   cargo hold as you mine.
4. Mining stops when:
   - The asteroid is fully depleted
   - You move beyond beam range (50m)
   - Your cargo hold is full
   - You press **1** again or manually stop

### Ore Types

Three ore types exist in the asteroid fields, each with different
properties:

| Ore | Rarity | Yield Rate | Hardness | Cargo Volume |
|-----|--------|------------|----------|--------------|
| Luminite | Common (60%) | Fast (10/s) | Low (1.0) | 0.1 m³/unit |
| Ferrox | Uncommon (30%) | Medium (7/s) | Medium (1.5) | 0.15 m³/unit |
| Auralite | Rare (10%) | Slow (5/s) | High (2.5) | 0.25 m³/unit |

- **Luminite** is an ice-blue common ore — the easiest to mine and
  the most abundant. Great for filling your hold quickly.
- **Ferrox** is a bronze-orange uncommon ore. It yields less per
  second and takes longer to extract, but is more valuable.
- **Auralite** is a rare violet ore, tough to mine but the most
  valuable resource in the field.

### Mining Beam

- **Range**: 50 meters maximum. Move closer for reliable extraction.
- The beam color matches the ore type being mined.
- The beam pulses visually while active, with sparks at the impact
  point.

### Yield and Depletion

Your effective mining rate depends on your ship's mining power, the
ore's base yield, and its hardness. As you mine, the asteroid
gradually depletes:

- It **shrinks** in size as mass is removed (down to 30% of its
  original size).
- Its **color shifts** toward red-orange as it nears depletion.
- At 25%, 50%, 75%, and 100% depletion, the asteroid briefly
  **crumbles and pauses** with particle effects and audio cues.
- A fully depleted asteroid fades out and disappears.

Small ore chunks periodically break off and drift toward your ship as
visual feedback during mining.

## Inventory & Resources

Your cargo hold stores all mined ore. Each ship has a maximum cargo
volume that limits how much you can carry.

### Cargo Capacity by Ship

| Ship | Cargo Volume | Module Slots |
|------|-------------|--------------|
| Small Mining Barge | 100 m³ | 4 |
| Medium Mining Barge | 250 m³ | 6 |
| Heavy Mining Barge | 500 m³ | 8 |

### How Cargo Works

- Each ore type is stored as a separate stack showing the type and
  quantity.
- Total cargo used is the sum of all ore volumes:
  e.g., 100 Luminite (0.1 m³ each) = 10 m³ used.
- When your cargo hold is full, mining stops automatically and a
  **"Cargo Full"** warning appears on screen.
- Your inventory can hold up to 20 different resource stacks.

### Example

With a Small Mining Barge (100 m³ capacity):

| Ore | Quantity | Volume Each | Total Volume |
|-----|----------|-------------|-------------|
| Luminite | 500 | 0.1 m³ | 50.0 m³ |
| Ferrox | 200 | 0.15 m³ | 30.0 m³ |
| Auralite | 40 | 0.25 m³ | 10.0 m³ |
| **Total** | | | **90.0 m³** |

That leaves 10 m³ of free space — enough for 100 more Luminite,
66 more Ferrox, or 40 more Auralite.

## HUD & UI

### On-Screen Display

- **Top-left**: Ship velocity (m/s) and hull integrity bar.
  - Hull bar changes color: green (healthy) → yellow (damaged) →
    red (critical).
- **Left side**: Hotbar module slots. Press the corresponding number
  key to activate a module.
- **Lower-left**: Resource inventory showing ore types and quantities.
- **Lower-center** (during mining): Mining progress bar showing
  asteroid depletion, ore type being mined, and yield counter.
  - The progress bar pulses and flashes white at depletion milestones.

### Floating Warnings

When mining stops unexpectedly, a warning appears near the center of
the screen for a few seconds:

- **CARGO FULL** — Your hold is at capacity. Dock at a station or
  jettison cargo.
- **OUT OF RANGE** — You've moved beyond the 50m mining beam range.
- **ASTEROID DEPLETED** — The asteroid has been fully mined out.

### Radial Context Menu

Right-click with a target selected to open the radial menu. The
available options depend on what you've selected:

**Asteroid selected:**
- **Approach** — Auto-pilot toward the asteroid
- **Orbit** — Circle the asteroid at a set distance
- **Mine** — Start mining immediately
- **Keep at Range** — Maintain a set distance

**Station selected:**
- **Approach** — Auto-pilot toward the station
- **Orbit** — Circle the station at a set distance
- **Dock** — Begin the docking sequence
- **Keep at Range** — Maintain a set distance

For Approach, Orbit, and Keep at Range, a **distance submenu**
appears with preset distances: **25m, 50m, 100m, 250m, 500m**, plus
a slider for custom distances. Your last-used distance is remembered
for each action.

The radial menu is disabled while you are docked at a station.

**Lock Target** is available for all target types (asteroids and
stations). See Targeting & Locking below for details.

## Targeting & Locking

The targeting system lets you select objects in space and lock onto
multiple targets simultaneously for tracking.

### Selecting Targets

Left-click any asteroid or station to select it. A corner-bracket
reticle appears around the target showing:

- **Above**: Target name and type (e.g., "Luminite Asteroid")
- **Below**: Distance in meters (updates live)

Click empty space to deselect. Click a different object to transfer
selection.

### Off-Screen Tracking

When your selected target moves off-screen, a directional triangle
indicator appears at the edge of the screen pointing toward the
target. The triangle rotates to show the direction you need to turn.

### Locking Targets

To lock a target for persistent tracking:

1. **Left-click** an asteroid or station to select it.
2. **Right-click** to open the radial menu.
3. Click **Lock Target** to begin lock acquisition.

During acquisition (default 1.5 seconds):

- A progress ring fills around the reticle.
- The reticle corners pulse.
- An acquiring audio tone rises in pitch.
- The reticle, name, and range labels remain visible underneath.

When complete, you hear a confirmation sound and a brief flash
appears. A **target card** is added to the HUD.

### Lock Cancellation

Lock acquisition is cancelled if:

- You click a different target or empty space (deselection).
- The target moves beyond maximum lock range (5,000 meters) during
  acquisition.
- The target is destroyed (e.g., asteroid fully depleted).

A failure sound plays when acquisition is cancelled.

### Target Cards

Each locked target gets a card displayed in the upper-right area of
the HUD. Each card shows:

- A **live viewport** rendering the target object in isolation.
- The target's **name**.
- **Range** in meters (updates continuously).
- A **dismiss button** (X) to unlock the target.

Click a card's body (not the dismiss button) to re-select that locked
target, transferring your selection to it.

Cards reflow automatically when a target is unlocked — remaining
cards shift to fill gaps.

### Multi-Target Limits

Each ship has a maximum number of simultaneous target locks:

| Ship | Max Locks | Lock Time | Lock Range |
|------|-----------|-----------|------------|
| Small Mining Barge | 3 | 1.5s | 5,000m |
| Medium Mining Barge | 3 | 1.5s | 5,000m |
| Heavy Mining Barge | 3 | 2.0s | 5,000m |

Attempting to lock a fourth target when all slots are full produces a
"slots full" audio cue. Attempting to lock a target that is already
locked is silently ignored.

### Auto-Clear on Docking

When you dock at a station, all target locks are cleared and target
cards are removed. Targeting is disabled while docked — you cannot
select targets or initiate locks until you undock.

## Station Docking

Two stations are present in the game world:

- **Small Mining Relay** — A compact forward operating base for
  mining operations.
- **Medium Refinery Hub** — A larger facility for mineral processing
  and trading.

### How to Dock

1. **Left-click** a station to select it.
2. **Right-click** to open the radial menu and choose **Dock**.
3. Your ship auto-pilots toward the station's docking port.
4. Within 30 meters, a magnetic snap guides your ship into position
   over about 1.5 seconds.
5. Once docked, the **Station Services Menu** opens automatically.

You can also fly within 500 meters of a station and dock via the
radial menu.

### Station Services Menu

When docked, a menu appears with the station name, your credit
balance, and four service tabs. Available services depend on the
station type.

#### Cargo Transfer

Move ore and materials between your ship's cargo hold and the
station's storage. Select an item, choose a quantity with the slider,
then click the transfer arrow. Ship capacity is enforced — attempting
to transfer more than your cargo hold can fit is rejected.

#### Sell Resources (Market)

Sell items from station storage for credits. Select a resource, set
the quantity, and review the credit preview before confirming. Credits
use integer arithmetic — no fractional credits. If you can't afford
the full amount, a hint shows the maximum you can sell.

#### Refine Ores (Refinery)

Convert raw ore into refined materials through time-based refining
jobs. Select an ore type and quantity, review the credit cost, and
start a job. Each station has a limit on concurrent active refining
slots. Active jobs show progress with remaining time. When a job
completes, click it to review the generated materials, then collect
them into station storage.

**Refining outputs per ore type:**

| Ore | Output Materials | Credit Cost/Unit |
|-----|-----------------|-----------------|
| Luminite | Luminite Ingots, Energium Dust | 5 |
| Ferrox | Ferrox Slabs, Conductive Residue | 15 |
| Auralite | Auralite Shards, Quantum Essence | 40 |

Yields vary per unit due to built-in variance — results are
deterministic per job but differ between jobs.

#### Basic Repair

Restore your ship's hull integrity to 100% for credits. The cost
scales with damage: `ceil((1 - currentHP) × RepairCostPerHP)`. Not
all stations offer repair — the Small Mining Relay has no repair bay.

#### Credits

Your credit balance is shown in the header and updates in real time.
Earn credits by selling resources. Spend credits on refining jobs and
hull repairs. You start with 0 credits.

#### Station Capabilities

| Station | Cargo | Market | Refinery | Repair | Refining Slots | Speed |
|---------|-------|--------|----------|--------|---------------|-------|
| Small Mining Relay | Yes | No | Yes | No | 2 | 1.0x |
| Medium Refinery Hub | Yes | Yes | Yes | Yes | 4 | 1.5x |

At the bottom of the menu is the **Undock** button.

### Undocking

Click the **Undock** button in the Station Services Menu. Your ship
thrusts away from the station to a safe distance of 100 meters over
about 2 seconds, then returns to normal flight mode. The hotbar and
ship HUD panels reappear once undocking completes.

### Docking Tips

- The maximum docking range is 500 meters — you must be within this
  distance to initiate docking.
- Manual thrust input during the approach phase cancels the docking
  sequence.
- While docked, your ship is locked in place and the radial menu is
  disabled.

## Ships

Three mining barge variants are available, each suited to different
stages of play:

### Small Mining Barge (Starter)

Your starting ship. Fast and nimble, but with limited cargo space and
mining power.

| Stat | Value |
|------|-------|
| Mass | 1,000 kg |
| Max Speed | 100 m/s |
| Max Thrust | 5,000 N |
| Rotation Torque | 50 Nm |
| Mining Power | 1.0x |
| Module Slots | 4 |
| Cargo Capacity | 100 m³ |

### Medium Mining Barge

A balanced mid-tier vessel with better mining capability and more
cargo space at the cost of some speed and agility.

| Stat | Value |
|------|-------|
| Mass | 2,500 kg |
| Max Speed | 75 m/s |
| Max Thrust | 8,000 N |
| Rotation Torque | 35 Nm |
| Mining Power | 1.5x |
| Module Slots | 6 |
| Cargo Capacity | 250 m³ |

### Heavy Mining Barge

The industrial workhorse. Slowest to maneuver but mines twice as fast
as the starter ship and carries five times the cargo.

| Stat | Value |
|------|-------|
| Mass | 5,000 kg |
| Max Speed | 50 m/s |
| Max Thrust | 12,000 N |
| Rotation Torque | 20 Nm |
| Mining Power | 2.0x |
| Module Slots | 8 |
| Cargo Capacity | 500 m³ |

### Ship Progression

Heavier ships trade speed and maneuverability for raw mining power
and cargo capacity. Choose the right ship for the job:

- **Small Barge**: Quick repositioning, good for scattered small
  asteroids.
- **Medium Barge**: Solid all-rounder for extended mining sessions.
- **Heavy Barge**: Maximum throughput for dense asteroid fields.

Ship swapping at stations is planned for a future update.

## Quick Reference

### Core Gameplay Loop

1. **Select** an asteroid (left-click)
2. **Approach** via radial menu (right-click → Approach) or
   double-click nearby space
3. **Mine** (press 1 or right-click → Mine)
4. **Watch** your cargo fill and the asteroid deplete
5. **Dock** at a station when your hold is full (right-click station
   → Dock)
6. **Transfer** ore from ship to station via Cargo Transfer
7. **Sell** resources for credits, or **Refine** ore into valuable
   raw materials
8. **Repair** your hull if damaged (at stations with repair bays)
9. **Undock** and repeat

### Key Bindings at a Glance

| Action | Binding |
|--------|---------|
| Select | Left-click |
| Radial Menu | Right-click |
| Orbit Camera | Right-click + drag |
| Zoom | Scroll wheel |
| Free Look | Middle-click |
| Fly to Point | Double-click |
| Forward/Back | W / S |
| Strafe | A / D |
| Roll | Q / E |
| Mining Laser | 1 |
| Cancel / Close | Escape |
