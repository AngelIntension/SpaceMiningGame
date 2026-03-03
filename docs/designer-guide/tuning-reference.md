# Tuning Reference

> Quick-reference for every designer-tunable parameter in VoidHarvest.
> All values are edited in the Unity Inspector by selecting the relevant asset file.
> For full asset descriptions, locations, and creation instructions, see the
> [Configuration Asset Catalog](scriptable-objects.md).

---

## How to Use This Document

1. **Find the game system** you want to adjust (Camera, Ship, Mining, etc.).
2. **Locate the parameter** in the table.
3. **Open the asset file** listed in the "Asset Type" column in the Unity Project panel.
4. **Change the value** in the Inspector. The "Valid Range" column tells you what values are safe.
5. **Save** (Ctrl+S) and enter Play mode to test.

> **Tip:** If you see a yellow warning in the Unity Console after changing a value,
> you have set something outside its valid range. Check the "Valid Range" column below.

---

## Camera System

Parameters that control the third-person orbiting camera and the space skybox.

### Camera Config

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Min Pitch | Camera Config | -80 | < 0 (degrees) | Lowest angle the camera can look down. More negative = can look further below the ship. |
| Max Pitch | Camera Config | 80 | > 0 (degrees) | Highest angle the camera can look up. Closer to 90 = nearly straight overhead. |
| Min Distance | Camera Config | 5 | > 0 (meters) | Closest the camera can zoom in to the ship. |
| Max Distance | Camera Config | 50 | > Min Distance (meters) | Farthest the camera can zoom out from the ship. |
| Min Zoom Distance | Camera Config | 10 | >= Min Distance (meters) | Camera distance at maximum ship speed. The camera auto-zooms to this when flying fast. |
| Max Zoom Distance | Camera Config | 40 | <= Max Distance (meters) | Camera distance when the ship is stationary. The camera auto-zooms out to this at rest. |
| Zoom Cooldown Duration | Camera Config | 2.0 | >= 0 (seconds) | After the player manually scrolls the zoom, how long before auto speed-zoom resumes. |
| Orbit Sensitivity | Camera Config | 0.1 | > 0 (degrees per pixel) | How fast the camera rotates when the player drags the mouse. Higher = faster orbiting. |
| Default Yaw | Camera Config | 0 | any (degrees) | Camera horizontal angle when the game starts. 0 = directly behind the ship. |
| Default Pitch | Camera Config | 15 | Min Pitch to Max Pitch (degrees) | Camera vertical angle when the game starts. Positive = slightly above the ship. |
| Default Distance | Camera Config | 25 | Min Distance to Max Distance (meters) | Camera distance from the ship when the game starts. |

**Asset location:** `Assets/Features/Camera/Data/DefaultCameraConfig.asset`

### Skybox Config

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Skybox Material | Skybox Config | *(assigned)* | any Material | The nebula skybox material. Must use the Skybox/Panoramic shader. |
| Fallback Material | Skybox Config | *(assigned)* | any Material | Backup skybox shown if the primary material fails to load. |
| Rotation Speed | Skybox Config | 0.5 | 0 to 5 (degrees/sec) | How fast the skybox slowly rotates. 0 = frozen sky. Higher = more noticeable drift. |
| Exposure Override | Skybox Config | 1.0 | 0.1 to 3.0 | Brightness of the skybox. 1.0 = normal. Lower = darker space. Higher = brighter nebula. |

**Asset location:** `Assets/Features/Camera/Data/GameSceneSkybox.asset`

---

## Ship System

Parameters that define how each ship archetype flies, mines, and locks targets.
There are three ship assets: Starter, Medium, and Heavy Mining Barge.

### Ship Archetype Config

| Parameter | Asset Type | Default (Starter / Medium / Heavy) | Valid Range | Description |
|-----------|-----------|-------------------------------------|-------------|-------------|
| Archetype Id | Ship Archetype Config | starter-mining-barge | non-empty text | Unique internal name. Do not change on shipped assets. |
| Display Name | Ship Archetype Config | Small / Medium / Heavy Mining Barge | non-empty text | Name shown to the player in the HUD and menus. |
| Role | Ship Archetype Config | Mining Barge | dropdown | Ship class specialization (Mining Barge, Hauler, Combat Scout, Explorer, Refinery). |
| Mass | Ship Archetype Config | 1000 / 2500 / 5000 | > 0 (kg) | Ship weight. Heavier ships accelerate slower but feel more substantial. |
| Max Thrust | Ship Archetype Config | 5000 / 8000 / 12000 | > 0 (Newtons) | Maximum engine force. Higher = faster acceleration. Effective acceleration = Max Thrust / Mass. |
| Max Speed | Ship Archetype Config | 100 / 75 / 50 | > 0 (m/s) | Speed cap. The ship cannot exceed this speed regardless of thrust. |
| Rotation Torque | Ship Archetype Config | 50 / 35 / 20 | > 0 | How quickly the ship turns. Higher = more nimble. |
| Linear Damping | Ship Archetype Config | 0.5 / 0.5 / 0.5 | >= 0 | How fast the ship slows down when not thrusting. 0 = pure Newtonian drift. Higher = more braking. |
| Angular Damping | Ship Archetype Config | 2 / 2 / 2 | >= 0 | How fast the ship stops spinning after turning. 0 = spins forever. Higher = snappier stop. |
| Mining Power | Ship Archetype Config | 1.0 / 1.5 / 2.0 | > 0 | Multiplier on mining yield. 2.0 = mines twice as fast as 1.0. |
| Module Slots | Ship Archetype Config | 4 / 6 / 8 | >= 0 | Number of equipment slots. Reserved for future module system. |
| Cargo Capacity | Ship Archetype Config | 100 / 250 / 500 | > 0 (cubic meters) | Total cargo volume the ship can carry. |
| Cargo Slots | Ship Archetype Config | 20 | 1 to 100 | Number of distinct inventory slots in the cargo hold. |
| Base Lock Time | Ship Archetype Config | 1.5 | > 0 (seconds) | Time to acquire a target lock. Lower = faster lock-on. |
| Max Target Locks | Ship Archetype Config | 3 | >= 1 | How many targets the ship can lock simultaneously. |
| Max Lock Range | Ship Archetype Config | 5000 | > 0 (meters) | Maximum distance at which the ship can begin locking a target. |
| Hull Mesh | Ship Archetype Config | *(assigned)* | any Mesh | 3D model used for the ship hull. |
| Hull Material | Ship Archetype Config | *(assigned)* | any Material | Material applied to the hull mesh. |

**Asset locations:**
- `Assets/Features/Ship/Data/StarterMiningBarge.asset`
- `Assets/Features/Ship/Data/MediumMiningBarge.asset`
- `Assets/Features/Ship/Data/HeavyMiningBarge.asset`

---

## Mining System

Parameters that control ore extraction, beam visuals, chunk collection effects, asteroid depletion effects, and audio feedback.

### Ore Definition

One asset per ore type. Currently three ores: Luminite (Common), Ferrox (Uncommon), Auralite (Rare).

| Parameter | Asset Type | Default (Luminite / Ferrox / Auralite) | Valid Range | Description |
|-----------|-----------|----------------------------------------|-------------|-------------|
| Ore Id | Ore Definition | luminite / ferrox / auralite | non-empty text | Unique internal identifier. Do not change on shipped assets. |
| Display Name | Ore Definition | Luminite / Ferrox / Auralite | non-empty text | Name shown to the player in the HUD and inventory. |
| Rarity Tier | Ore Definition | Common / Uncommon / Rare | dropdown | Classification that affects spawn probability and display color coding. |
| Base Value | Ore Definition | 10 / 25 / 75 | >= 0 (credits) | Sell price per unit of raw ore. |
| Rarity Weight | Ore Definition | 0.6 / 0.3 / 0.1 | 0.0 to 1.0 | Default spawn probability weight. Higher = more common in asteroid fields. |
| Base Yield Per Second | Ore Definition | 10 / 7 / 5 | > 0 (units/sec) | How much ore the mining beam extracts per second before other modifiers. |
| Hardness | Ore Definition | 1.0 / 1.5 / 2.5 | > 0 | Extraction difficulty. Higher = slower mining. Divides the yield. |
| Volume Per Unit | Ore Definition | 0.1 / 0.15 / 0.25 | > 0 (cubic meters) | Cargo space each unit of ore takes up. |
| Beam Color | Ore Definition | Ice Blue / Bronze Orange / Violet | any color | Color of the mining laser beam when extracting this ore type. |
| Base Processing Time Per Unit | Ore Definition | 2 / 5 / 10 | > 0 (seconds) | Refining time per unit. Longer = rarer ores take more time at the refinery. |
| Refining Credit Cost Per Unit | Ore Definition | 5 / 15 / 40 | >= 0 (credits) | Credit fee charged per unit to refine this ore at a station. |
| Refining Outputs | Ore Definition | *(see below)* | at least 1 entry | List of raw materials produced when this ore is refined. Each entry has: |
| -- Material | *(sub-entry)* | *(assigned)* | any Raw Material Definition | Which raw material is produced. |
| -- Base Yield Per Unit | *(sub-entry)* | varies | > 0 | Base amount of this material produced per unit of ore refined. |
| -- Variance Min | *(sub-entry)* | varies | <= Variance Max | Minimum random offset added to yield (can be negative). |
| -- Variance Max | *(sub-entry)* | varies | >= Variance Min | Maximum random offset added to yield. |
| Icon | Ore Definition | *(none)* | any Sprite | Inventory icon. Reserved for future UI. |
| Description | Ore Definition | *(flavor text)* | any text | Tooltip description shown to the player. |

**Asset locations:**
- `Assets/Features/Mining/Data/Ores/Luminite.asset`
- `Assets/Features/Mining/Data/Ores/Ferrox.asset`
- `Assets/Features/Mining/Data/Ores/Auralite.asset`

### Ore Chunk Config

Controls the cosmetic "ore chunk" particles that fly from asteroids toward the ship during mining.

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Spawn Interval Min | Ore Chunk Config | 3.0 | > 0 (seconds) | Shortest time between chunk spawn bursts. |
| Spawn Interval Max | Ore Chunk Config | 7.0 | >= Spawn Interval Min (seconds) | Longest time between chunk spawn bursts. |
| Chunks Per Spawn Min | Ore Chunk Config | 2 | >= 1 | Fewest chunks per burst. |
| Chunks Per Spawn Max | Ore Chunk Config | 5 | >= Chunks Per Spawn Min | Most chunks per burst. |
| Chunk Scale Min | Ore Chunk Config | 0.03 | > 0 | Smallest chunk size (as a fraction of ship scale). |
| Chunk Scale Max | Ore Chunk Config | 0.12 | >= Chunk Scale Min | Largest chunk size. |
| Initial Drift Duration | Ore Chunk Config | 0.75 | > 0 (seconds) | How long chunks float outward from the asteroid before being pulled toward the ship. |
| Initial Drift Speed | Ore Chunk Config | 2.0 | > 0 (m/s) | How fast chunks fly outward during the drift phase. |
| Attraction Speed | Ore Chunk Config | 8.0 | > 0 (m/s) | Maximum speed chunks move toward the ship. |
| Attraction Acceleration | Ore Chunk Config | 3.0 | > 0 (m/s squared) | How quickly chunks ramp up to attraction speed. |
| Collection Flash Duration | Ore Chunk Config | 0.15 | > 0 (seconds) | Length of the bright flash when a chunk reaches the ship. |
| Max Lifetime | Ore Chunk Config | 5.0 | > 0 (seconds) | Safety timeout -- chunks disappear after this long even if they haven't reached the ship. |
| Glow Intensity | Ore Chunk Config | 2.0 | > 0 | Brightness of the glow effect on each chunk. |

**Asset location:** `Assets/Features/Mining/Data/OreChunkConfig.asset`

### Mining VFX Config

Controls the visual appearance of the mining laser beam and impact effects.

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Beam Width | Mining VFX Config | 0.15 | > 0 (meters) | Thickness of the mining laser beam. |
| Beam Pulse Speed | Mining VFX Config | 3.0 | > 0 (cycles/sec) | How fast the beam width oscillates. Higher = more rapid pulsing. |
| Beam Pulse Amplitude | Mining VFX Config | 0.3 | 0.0 to 1.0 | How much the beam width changes during a pulse. 0 = no pulse. 1 = extreme wobble. |
| Spark Emission Rate | Mining VFX Config | 15 | >= 0 (sparks/sec) | Number of spark particles emitted per second at the beam impact point. |
| Spark Lifetime | Mining VFX Config | 0.4 | > 0 (seconds) | How long each spark particle lives before fading. |
| Spark Speed | Mining VFX Config | 3.0 | > 0 (m/s) | Initial outward velocity of spark particles. |
| Heat Haze Intensity | Mining VFX Config | 0.5 | 0.0 to 1.0 | Opacity of the heat shimmer distortion at the impact point. 0 = invisible. |
| Heat Haze Scale | Mining VFX Config | 0.3 | > 0 (meters) | Size of the heat shimmer distortion area. |

**Asset location:** `Assets/Features/Mining/Data/MiningVFXConfig.asset`

### Mining Audio Config

Controls volume, pitch, and spatial range for all mining sound effects.

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Laser Hum Clip | Mining Audio Config | *(optional)* | any AudioClip | Looping laser beam sound. If empty, a placeholder is generated. |
| Laser Hum Base Volume | Mining Audio Config | 0.6 | 0.0 to 1.0 | Volume of the laser hum loop. |
| Laser Hum Pitch Min | Mining Audio Config | 0.8 | > 0 | Pitch of the laser at 0% asteroid depletion (low tone). |
| Laser Hum Pitch Max | Mining Audio Config | 1.4 | > Pitch Min | Pitch at 100% depletion (high tone). The beam gets higher-pitched as the asteroid runs out. |
| Laser Hum Fade Out Duration | Mining Audio Config | 0.3 | > 0 (seconds) | How quickly the laser hum fades when mining stops. |
| Spark Crackle Clip | Mining Audio Config | *(optional)* | any AudioClip | Impact spark sound. |
| Spark Crackle Volume | Mining Audio Config | 0.4 | 0.0 to 1.0 | Volume of the spark crackle. |
| Crumble Rumble Clip | Mining Audio Config | *(optional)* | any AudioClip | Sound when the asteroid crosses a depletion threshold (25%, 50%, 75%). |
| Crumble Rumble Volume | Mining Audio Config | 0.7 | 0.0 to 1.0 | Volume of the crumble rumble. |
| Explosion Clip | Mining Audio Config | *(optional)* | any AudioClip | Sound when the asteroid is fully depleted and breaks apart. |
| Explosion Volume | Mining Audio Config | 0.8 | 0.0 to 1.0 | Volume of the explosion. |
| Collection Clink Clip | Mining Audio Config | *(optional)* | any AudioClip | Sound when an ore chunk reaches the ship. |
| Collection Clink Volume | Mining Audio Config | 0.3 | 0.0 to 1.0 | Volume of the collection clink. |
| Max Audible Distance | Mining Audio Config | 100.0 | > 0 (meters) | Maximum distance at which any mining sound can be heard. |

**Asset location:** `Assets/Features/Mining/Data/MiningAudioConfig.asset`

### Depletion VFX Config

Controls the visual feedback as an asteroid is progressively mined and eventually destroyed.

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Vein Glow Min Intensity | Depletion VFX Config | 0.0 | >= 0 | Glow brightness at 0% depletion (fresh asteroid). Usually 0 for no glow. |
| Vein Glow Max Intensity | Depletion VFX Config | 0.6 | >= Min Intensity | Glow brightness at 100% depletion (almost gone). |
| Vein Glow Color | Depletion VFX Config | Warm Orange (1, 0.8, 0.4) | any color | Color of the depletion glow veins on the asteroid surface. |
| Vein Glow Pulse Speed | Depletion VFX Config | 1.5 | > 0 (cycles/sec) | How fast the glow pulses. Higher = more frantic near depletion. |
| Vein Glow Pulse Amplitude | Depletion VFX Config | 0.15 | 0.0 to 1.0 | Intensity of the glow pulsing. 0 = steady glow. |
| Crumble Burst Count Base | Depletion VFX Config | 8 | >= 1 | Number of rock particles at the first depletion threshold (25%). |
| Crumble Burst Count Scale | Depletion VFX Config | 1.5 | > 0 | Multiplier applied at each subsequent threshold. At 50% = base x scale, at 75% = base x scale x scale. |
| Crumble Burst Speed | Depletion VFX Config | 5.0 | > 0 (m/s) | How fast crumble fragments fly outward. |
| Crumble Burst Lifetime | Depletion VFX Config | 0.5 | > 0 (seconds) | How long crumble particles last before fading. |
| Crumble Flash Duration | Depletion VFX Config | 0.3 | > 0 (seconds) | Duration of the bright flash at each depletion threshold. |
| Fragment Count | Depletion VFX Config | 12 | 8 to 15 | Number of asteroid pieces when the asteroid is fully destroyed. |
| Fragment Speed | Depletion VFX Config | 4.0 | > 0 (m/s) | How fast destruction fragments fly outward. |
| Fragment Lifetime | Depletion VFX Config | 3.0 | > 0 (seconds) | How long destruction fragments are visible before disappearing. |
| Fragment Scale Range | Depletion VFX Config | 0.05 to 0.2 | > 0 (min, max) | Size range of destruction fragments as a fraction of the original asteroid. |

**Asset location:** `Assets/Features/Mining/Data/DepletionVFXConfig.asset`

---

## Docking System

Parameters that control how ships dock and undock from stations.

### Docking Config

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Max Docking Range | Docking Config | 500 | > 0 (meters) | How close the ship must be to start docking. The "Dock" option appears in the radial menu within this range. |
| Snap Range | Docking Config | 30 | > 0, < Max Docking Range (meters) | Distance at which the magnetic snap animation begins pulling the ship to the dock. |
| Snap Duration | Docking Config | 1.5 | > 0 (seconds) | How long the smooth snap-to-dock animation takes. |
| Undock Clearance Distance | Docking Config | 100 | > 0 (meters) | How far the ship is pushed away from the station when undocking. |
| Undock Duration | Docking Config | 2.0 | > 0 (seconds) | Length of the undock clearance animation. |
| Approach Timeout | Docking Config | 120 | > 0 (seconds) | Safety timer: if the ship hasn't reached snap range within this time, docking auto-cancels. |
| Align Timeout | Docking Config | 30 | > 0 (seconds) | Safety timer: if alignment doesn't complete in time, the ship force-snaps into position. |
| Align Dot Threshold | Docking Config | 0.999 | 0 to 1 | How precisely the ship must face the dock before snapping. 1.0 = perfect alignment required. 0.999 = very small tolerance. |
| Align Ang Vel Threshold | Docking Config | 0.01 | > 0 (radians/sec) | Ship must be spinning slower than this to complete alignment. Lower = stricter settling. |

**Asset location:** `Assets/Features/Docking/Data/Configs/DockingConfig.asset`

### Docking VFX Config

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Alignment Guide Effect | Docking VFX Config | *(optional)* | any Prefab | Particle effect shown during the approach phase. |
| Approach Glow Intensity | Docking VFX Config | 1.0 | > 0 | Brightness of the dock's glow during approach. |
| Snap Flash Effect | Docking VFX Config | *(optional)* | any Prefab | Particle effect played when the ship locks into the dock. |
| Snap Flash Duration | Docking VFX Config | 0.5 | > 0 (seconds) | Length of the snap flash. |
| Undock Release Effect | Docking VFX Config | *(optional)* | any Prefab | Particle effect played when the ship undocks. |

**Asset location:** `Assets/Features/Docking/Data/Configs/DockingVFXConfig.asset`

### Docking Audio Config

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Approach Hum Clip | Docking Audio Config | *(optional)* | any AudioClip | Ambient sound during the approach phase. |
| Dock Clamp Clip | Docking Audio Config | *(optional)* | any AudioClip | Sound when the ship locks into the dock. |
| Dock Clamp Volume | Docking Audio Config | 0.8 | 0.0 to 1.0 | Volume of the dock clamp sound. |
| Undock Release Clip | Docking Audio Config | *(optional)* | any AudioClip | Sound when the clamps release during undock. |
| Undock Release Volume | Docking Audio Config | 0.6 | 0.0 to 1.0 | Volume of the undock release sound. |
| Engine Start Clip | Docking Audio Config | *(optional)* | any AudioClip | Engine ignition sound played after undock. |
| Max Audible Distance | Docking Audio Config | 200 | > 0 (meters) | Maximum distance at which docking sounds can be heard. |

**Asset location:** `Assets/Features/Docking/Data/Configs/DockingAudioConfig.asset`

---

## Input System

Parameters that control mouse interaction timing and default command distances.

### Interaction Config

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Double Click Window | Interaction Config | 0.3 | 0.1 to 1.0 (seconds) | Time window for registering a double-click. Lower = requires faster clicking. |
| Radial Menu Drag Threshold | Interaction Config | 5 | 1 to 20 (pixels) | How far the mouse must move after clicking before the radial menu opens. Lower = more sensitive. |
| Default Approach Distance | Interaction Config | 50 | > 0 (meters) | How close the ship flies toward a target when using the "Approach" command. |
| Default Orbit Distance | Interaction Config | 100 | > 0 (meters) | Orbit radius when using the "Orbit" command on a target. |
| Default Keep At Range Distance | Interaction Config | 50 | > 0 (meters) | Distance maintained when using the "Keep at Range" command. |
| Mining Beam Max Range | Interaction Config | 50 | > 0 (meters) | Maximum distance at which the mining beam can activate. Target must be closer than this. |

**Asset location:** `Assets/Features/Input/Data/DefaultInteractionConfig.asset`

---

## Targeting System

Parameters that control on-screen target reticles, lock-on progress visuals, and targeting audio.

### Targeting Config

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Reticle Padding | Targeting Config | 20 | >= 0 (pixels) | Extra space around the target when drawing the selection reticle box. |
| Reticle Min Size | Targeting Config | 40 | > 0 (pixels) | Smallest the reticle can shrink (for very distant targets). |
| Reticle Max Size | Targeting Config | 300 | > Reticle Min Size (pixels) | Largest the reticle can grow (for very close targets). |
| Lock Progress Arc Width | Targeting Config | 3 | > 0 (pixels) | Thickness of the circular progress arc shown during lock acquisition. |
| Off Screen Indicator Margin | Targeting Config | 30 | >= 0 (pixels) | How far from the screen edge the off-screen target arrow sits. |
| Viewport Render Width | Targeting Config | 140 | > 0 (pixels) | Width of the small preview image on target info cards. |
| Viewport Render Height | Targeting Config | 100 | > 0 (pixels) | Height of the small preview image on target info cards. |
| Viewport FOV | Targeting Config | 30 | > 0 (degrees) | Field of view for the preview camera. Lower = more zoomed in on the target. |
| Preview Stage Offset | Targeting Config | (0, -1000, 0) | any position | World position where preview copies of targets are placed for rendering. Keep far away from gameplay. |

**Asset location:** `Assets/Features/Targeting/Data/Assets/TargetingConfig.asset`

### Targeting VFX Config

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Lock Flash Duration | Targeting VFX Config | 0.3 | > 0 (seconds) | Length of the bright flash when a target lock is confirmed. |
| Reticle Pulse Speed | Targeting VFX Config | 2.0 | > 0 (cycles/sec) | How fast the reticle corners pulse while acquiring a lock. Higher = more urgent feel. |

**Asset location:** `Assets/Features/Targeting/Views/TargetingVFXConfig.asset`

### Targeting Audio Config

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Lock Acquiring Clip | Targeting Audio Config | *(optional)* | any AudioClip | Rising tone played while locking onto a target. |
| Lock Confirmed Clip | Targeting Audio Config | *(optional)* | any AudioClip | Confirmation sound when a lock completes. |
| Lock Failed Clip | Targeting Audio Config | *(optional)* | any AudioClip | Sound when a lock attempt is cancelled or fails. |
| Lock Slots Full Clip | Targeting Audio Config | *(optional)* | any AudioClip | Warning sound when all lock slots are occupied. |
| Target Lost Clip | Targeting Audio Config | *(optional)* | any AudioClip | Sound when a previously locked target is destroyed or goes out of range. |

**Asset location:** `Assets/Features/Targeting/Views/TargetingAudioConfig.asset`

---

## Procedural System

Parameters that control how asteroid fields are generated.

### Asteroid Field Definition

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Field Name | Asteroid Field Definition | Default Asteroid Field | non-empty text | Human-readable name for this asteroid field. |
| Asteroid Count | Asteroid Field Definition | 300 | > 0 | Total number of asteroids spawned in this field. More = denser field, higher performance cost. |
| Field Radius | Asteroid Field Definition | 2000 | > 0 (meters) | Radius of the spherical volume where asteroids are placed. |
| Asteroid Size Min | Asteroid Field Definition | 3 | > 0 | Smallest asteroid scale. |
| Asteroid Size Max | Asteroid Field Definition | 5 | >= Size Min | Largest asteroid scale. |
| Rotation Speed Min | Asteroid Field Definition | 0 | >= 0 (degrees/sec) | Slowest asteroid rotation. 0 = some asteroids don't spin. |
| Rotation Speed Max | Asteroid Field Definition | 15 | >= Speed Min (degrees/sec) | Fastest asteroid rotation. |
| Seed | Asteroid Field Definition | 42 | any whole number | Random seed for deterministic generation. Same seed = same field every time. Change this to get a different layout. |
| Min Scale Fraction | Asteroid Field Definition | 0.3 | 0.1 to 0.5 | Smallest an asteroid can shrink to when fully depleted. 0.3 = shrinks to 30% of its original size. |
| Ore Entries | Asteroid Field Definition | *(3 entries)* | at least 1 | List of ore types in this field. Each entry has: |
| -- Ore Definition | *(sub-entry)* | Luminite / Ferrox / Auralite | any Ore Definition asset | Which ore type this asteroid can contain. |
| -- Weight | *(sub-entry)* | 6 / 3 / 1 | > 0 | Relative spawn chance. Weights are normalized, so 6/3/1 means 60%/30%/10% of asteroids. |
| -- Mesh Variant A | *(sub-entry)* | *(assigned)* | any Mesh | First 3D model variant for visual variety. |
| -- Mesh Variant B | *(sub-entry)* | *(assigned)* | any Mesh | Second 3D model variant for visual variety. |
| -- Tint Color | *(sub-entry)* | *(per ore)* | any color | Color applied to asteroids of this ore type. |

**Asset location:** `Assets/Features/Procedural/Data/Fields/DefaultField.asset`

---

## Station System

Parameters for station service capabilities and raw material definitions.

### Station Services Config

Per-station settings that control refining speed, capacity, and repair costs.
Currently two station configs: Small Mining Relay and Medium Refinery Hub.

| Parameter | Asset Type | Default (Small / Medium) | Valid Range | Description |
|-----------|-----------|--------------------------|-------------|-------------|
| Max Concurrent Refining Slots | Station Services Config | 2 / 4 | >= 1 | How many refining jobs can run at the same time at this station. |
| Refining Speed Multiplier | Station Services Config | 1.0 / 1.5 | > 0 | Speed factor for refining. 1.5 = 50% faster refining. Higher = quicker jobs. |
| Repair Cost Per HP | Station Services Config | 0 / 100 | >= 0 (credits) | Credit cost per hit point repaired. 0 = this station does not offer repairs. |

**Asset locations:**
- `Assets/Features/Station/Data/ServiceConfigs/SmallMiningRelayServices.asset`
- `Assets/Features/Station/Data/ServiceConfigs/MediumRefineryHubServices.asset`

### Station Definition

Per-station identity, placement, and docking configuration.

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Station Id | Station Definition | 1 or 2 | > 0, unique | Unique ID number for this station. Must be different for every station in the world. |
| Display Name | Station Definition | *(per station)* | non-empty text | Name shown to the player in the HUD. |
| Description | Station Definition | *(optional)* | any text | Designer notes. Not shown to the player. |
| Station Type | Station Definition | dropdown | Mining Relay, Refinery Hub, Trade Post, Research Station | Classification that may affect future game logic. |
| World Position | Station Definition | *(per station)* | any position (X, Y, Z) | Where the station is placed in the game world. |
| World Rotation | Station Definition | identity | any rotation | Station facing direction. |
| Available Services | Station Definition | *(per station)* | at least 1 entry | List of service names: "Sell", "Refine", "Repair", "Cargo". |
| Services Config | Station Definition | *(assigned)* | any Station Services Config asset | Link to the station's service parameters (refining slots, speed, repair cost). |
| Preset Config | Station Definition | *(optional)* | any Station Preset Config asset | Link to station module layout. Reserved for Phase 2 procedural generation. |
| Docking Port Offset | Station Definition | *(per station)* | magnitude < 200 (X, Y, Z) | Position of the docking port relative to the station center. |
| Docking Port Rotation | Station Definition | identity | any rotation | Direction the docking port faces. |
| Safe Undock Direction | Station Definition | forward (0, 0, 1) | any direction | Direction the ship is pushed when undocking. Should point away from the station. |
| Prefab | Station Definition | *(optional)* | any Prefab | 3D model of the station. |
| Icon | Station Definition | *(optional)* | any Sprite | Station icon for HUD and UI panels. |

**Asset locations:**
- `Assets/Features/Station/Data/Definitions/SmallMiningRelay.asset`
- `Assets/Features/Station/Data/Definitions/MediumRefineryHub.asset`

### Raw Material Definition

Defines a processed material that comes out of refining. Currently six materials.

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Material Id | Raw Material Definition | *(per material)* | non-empty text | Unique internal name (e.g., "luminite_ingots"). |
| Display Name | Raw Material Definition | *(per material)* | non-empty text | Name shown in inventory and sell panels (e.g., "Luminite Ingots"). |
| Base Value | Raw Material Definition | *(per material)* | >= 0 (credits) | Sell price per unit. |
| Volume Per Unit | Raw Material Definition | *(per material)* | > 0 (cubic meters) | Cargo space each unit consumes. |
| Icon | Raw Material Definition | *(optional)* | any Sprite | Inventory icon. |
| Description | Raw Material Definition | *(optional)* | any text | Flavor text for tooltips. |

**Asset locations:** `Assets/Features/Station/Data/RawMaterials/` (6 assets: LuminiteIngots, EnergiumDust, FerroxSlabs, ConductiveResidue, AuraliteShards, QuantumEssence)

### Game Services Config

Global economy settings.

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Starting Credits | Game Services Config | 0 | >= 0 (credits) | How many credits the player begins with. 0 = start broke, must mine to earn. |

**Asset location:** `Assets/Features/StationServices/Data/Assets/GameServicesConfig.asset`

---

## World System

Parameters that define the overall game world setup.

### World Definition

| Parameter | Asset Type | Default | Valid Range | Description |
|-----------|-----------|---------|-------------|-------------|
| Stations | World Definition | 2 entries | at least 1 | List of all stations in the world. Drag Station Definition assets here. Each must have a unique Station Id. |
| Player Start Position | World Definition | (0, 0, 0) | any position (X, Y, Z) | Where the player's ship appears when starting the game. |
| Player Start Rotation | World Definition | identity | any rotation | Direction the player faces at game start. |
| Starting Ship Archetype | World Definition | *(Starter Mining Barge)* | any Ship Archetype Config asset | Which ship the player begins with. |

**Asset location:** `Assets/Features/World/Data/DefaultWorld.asset`

---

## Common Tuning Scenarios

Quick recipes for common design changes. Each scenario lists which parameter(s) to adjust and on which asset.

### Making Mining Faster

- **Increase** `Base Yield Per Second` on the relevant **Ore Definition** asset. This directly increases how much ore the beam extracts per second.
- **Decrease** `Hardness` on the **Ore Definition**. Hardness divides the yield, so lower values mean faster extraction.
- **Increase** `Mining Power` on the **Ship Archetype Config**. This multiplies all mining yield for that ship.

### Making Mining Slower or Harder

- **Increase** `Hardness` on the **Ore Definition**.
- **Decrease** `Base Yield Per Second` on the **Ore Definition**.
- **Decrease** `Mining Power` on the **Ship Archetype Config** to nerf a specific ship.

### Increasing Docking Range

- **Increase** `Max Docking Range` on the **Docking Config** asset. The "Dock" radial menu option becomes available from further away.
- Make sure `Snap Range` stays well below `Max Docking Range`.

### Making Lock-On Faster

- **Decrease** `Base Lock Time` on the **Ship Archetype Config**. Lower = faster targeting. A value of 0.5 is very quick; 3.0 feels deliberate.

### Allowing More Simultaneous Locks

- **Increase** `Max Target Locks` on the **Ship Archetype Config**.

### Adding More Asteroids

- **Increase** `Asteroid Count` on the **Asteroid Field Definition** asset.
- If the field feels too sparse, also **decrease** `Field Radius` to pack asteroids closer together.
- Watch for performance -- test in Play mode after large increases.

### Making a Rarer or More Common Ore

- Adjust the `Weight` values in the **Asteroid Field Definition**'s `Ore Entries` list. Weights are relative to each other. Setting Luminite to 10 and Auralite to 1 means Luminite is 10 times more common.
- Also update `Rarity Weight` on the **Ore Definition** to keep the assets internally consistent.

### Making Refining More Profitable

- **Increase** `Base Yield Per Unit` in the **Ore Definition**'s `Refining Outputs` entries. This gives more raw materials per unit of ore refined.
- **Widen** the `Variance Max` on refining outputs for more generous random bonus yields.
- **Decrease** `Refining Credit Cost Per Unit` on the **Ore Definition** to lower the refining fee.

### Making Refining Faster

- **Increase** `Refining Speed Multiplier` on the **Station Services Config** for a specific station.
- **Decrease** `Base Processing Time Per Unit` on the **Ore Definition** to make that ore refine faster everywhere.

### Changing Starting Credits

- Set `Starting Credits` on the **Game Services Config** asset. A value of 1000 gives new players a financial cushion. 0 forces them to mine immediately.

### Making Ships Feel Heavier or Lighter

- **Increase** `Mass` for a heavier, more sluggish feel. **Decrease** for a lighter, more responsive ship.
- Adjust `Max Thrust` alongside Mass to keep acceleration (Thrust / Mass) at a desired level.
- **Lower** `Rotation Torque` for slower turning. **Raise** it for nimble maneuvering.
- **Increase** `Linear Damping` for stronger auto-braking. **Decrease** for a more Newtonian, drifty feel.

### Adjusting the Camera

- For a tighter, more intimate view: **decrease** `Max Zoom Distance` and `Default Distance`.
- For a wider, more strategic view: **increase** `Max Zoom Distance` and `Max Distance`.
- For faster camera orbiting: **increase** `Orbit Sensitivity`.
- To prevent extreme angles: **narrow** the gap between `Min Pitch` and `Max Pitch`.

### Making a New Station Offer Repairs

- On the station's **Station Services Config**, set `Repair Cost Per HP` to a value greater than 0 (e.g., 100 credits per HP).
- On the station's **Station Definition**, add "Repair" to the `Available Services` list.

### Changing the Skybox Brightness

- **Increase** `Exposure Override` on the **Skybox Config** for a brighter nebula backdrop.
- **Decrease** it for a darker, more subdued space.

---

## Parameter Count Summary

| Game System | Asset Types | Total Parameters |
|-------------|-------------|------------------|
| Camera | 2 (Camera Config, Skybox Config) | 15 |
| Ship | 1 (Ship Archetype Config) | 17 |
| Mining | 5 (Ore Definition, Ore Chunk, Mining VFX, Mining Audio, Depletion VFX) | 52 |
| Docking | 3 (Docking Config, Docking VFX, Docking Audio) | 21 |
| Input | 1 (Interaction Config) | 6 |
| Targeting | 3 (Targeting Config, Targeting VFX, Targeting Audio) | 16 |
| Procedural | 1 (Asteroid Field Definition) | 9 + per-entry |
| Station | 4 (Station Services, Station Definition, Raw Material, Game Services) | 22 |
| World | 1 (World Definition) | 4 |
| **Total** | **21 asset types** | **162+** |

---

*Last updated: 2026-03-03*
