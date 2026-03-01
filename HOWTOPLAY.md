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
| Veldspar | Common (60%) | Fast (10/s) | Low (1.0) | 0.1 m³/unit |
| Scordite | Uncommon (30%) | Medium (7/s) | Medium (1.5) | 0.15 m³/unit |
| Pyroxeres | Rare (10%) | Slow (5/s) | High (2.5) | 0.25 m³/unit |

- **Veldspar** is the easiest to mine and the most common. Great for
  filling your hold quickly.
- **Scordite** yields less per second and takes longer to extract, but
  is more valuable.
- **Pyroxeres** is rare and tough to mine, but the most valuable ore
  in the field.

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
  e.g., 100 Veldspar (0.1 m³ each) = 10 m³ used.
- When your cargo hold is full, mining stops automatically and a
  **"Cargo Full"** warning appears on screen.
- Your inventory can hold up to 20 different resource stacks.

### Example

With a Small Mining Barge (100 m³ capacity):

| Ore | Quantity | Volume Each | Total Volume |
|-----|----------|-------------|-------------|
| Veldspar | 500 | 0.1 m³ | 50.0 m³ |
| Scordite | 200 | 0.15 m³ | 30.0 m³ |
| Pyroxeres | 40 | 0.25 m³ | 10.0 m³ |
| **Total** | | | **90.0 m³** |

That leaves 10 m³ of free space — enough for 100 more Veldspar,
66 more Scordite, or 40 more Pyroxeres.

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

When docked, a menu appears with the station name and four service
tabs:

- **Refinery** — Process raw ore into refined materials (coming soon)
- **Market** — Buy and sell resources (coming soon)
- **Repair** — Restore hull integrity (coming soon)
- **Cargo** — Manage your inventory (coming soon)

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
6. **Repeat** with a new asteroid field

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
