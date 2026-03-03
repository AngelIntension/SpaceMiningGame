# VoidHarvest Configuration Assets Reference

This document catalogs every configuration asset (settings file) in VoidHarvest, organized by game system. Use it as a lookup when you need to find, create, or tune any piece of game data.

---

## Terminology

Before diving in, here is a quick translation of Unity terms into plain language used throughout this guide.

| Unity Term | Plain Language | What It Means |
|---|---|---|
| ScriptableObject | Configuration asset / settings file | A file in your project that stores tuneable values. You edit it in the Inspector. |
| Serialized field | Configurable setting | A single value (number, text, color, etc.) exposed in the Inspector for you to change. |
| Prefab | Reusable template | A saved copy of a game object that can be placed in a scene many times. |
| AudioClip | Sound file | A reference to a .wav, .ogg, or .mp3 audio file. |
| Sprite | 2D image | A flat image used for icons, HUD elements, or UI panels. |
| Material | Surface look | Controls how a 3D object's surface appears (color, texture, shininess). |
| Mesh | 3D shape | The geometry of a 3D model (vertices and triangles). |
| Tooltip | Hover hint | The help text that appears when you hover your mouse over a setting in the Inspector. |
| Range | Slider limits | The minimum and maximum values the Inspector enforces on a number. |
| Vector3 | 3D position or direction | Three numbers (X, Y, Z) representing a point or direction in 3D space. |
| Quaternion | 3D rotation | Four numbers representing an orientation in 3D space. Usually edited as Euler angles (X, Y, Z degrees) in the Inspector. |
| Color | RGBA color | Four values (Red, Green, Blue, Alpha) from 0 to 1 defining a color. |

---

## How to Create a New Configuration Asset

All VoidHarvest configuration assets are created the same way:

1. In the **Project** window, navigate to the folder where you want the asset to live.
2. Right-click in the folder area (or use the menu bar) and choose **Create**.
3. Look under the **VoidHarvest** submenu for the system you need.
4. Select the asset type. A new file appears in the folder.
5. Give it a descriptive name and click on it to edit its settings in the **Inspector**.

Each asset type below lists its exact menu path so you know where to find it.

---

## How to Read Each Entry

Every configuration asset below is documented with:

- **What it does** -- a plain-language summary of the gameplay or visual behavior it controls.
- **Where it lives in the game** -- which part of VoidHarvest reads this asset.
- **Menu path** -- the exact right-click > Create path to make a new one.
- **Settings table** -- every configurable setting, what it means, its default value, and any limits the editor enforces.
- **Designer tips** -- practical advice for tuning.

---

## Table of Contents

| # | System | Asset Name | Menu Path |
|---|---|---|---|
| 1 | Camera | CameraConfig | VoidHarvest > Camera > Camera Config |
| 2 | Camera | SkyboxConfig | VoidHarvest > Camera > Skybox Config |
| 3 | Docking | DockingConfig | VoidHarvest > Docking > Docking Config |
| 4 | Docking | DockingVFXConfig | VoidHarvest > Docking > Docking VFX Config |
| 5 | Docking | DockingAudioConfig | VoidHarvest > Docking > Docking Audio Config |
| 6 | Input | InteractionConfig | VoidHarvest > Input > Interaction Config |
| 7 | Mining | OreDefinition | VoidHarvest > Mining > Ore Definition |
| 8 | Mining | OreChunkConfig | VoidHarvest > Mining > Ore Chunk Config |
| 9 | Mining | MiningVFXConfig | VoidHarvest > Mining > Mining VFX Config |
| 10 | Mining | MiningAudioConfig | VoidHarvest > Mining > Mining Audio Config |
| 11 | Mining | DepletionVFXConfig | VoidHarvest > Mining > Depletion VFX Config |
| 12 | Procedural | AsteroidFieldDefinition | VoidHarvest > Procedural > Asteroid Field Definition |
| 13 | Resources | RawMaterialDefinition | VoidHarvest > Station > Raw Material Definition |
| 14 | Ship | ShipArchetypeConfig | VoidHarvest > Ship > Ship Archetype Config |
| 15 | Station | StationPresetConfig | VoidHarvest > Station > Station Preset Config |
| 16 | Station | StationDefinition | VoidHarvest > Station > Station Definition |
| 17 | Station | StationServicesConfig | VoidHarvest > Station > Station Services Config |
| 18 | Station Services | GameServicesConfig | VoidHarvest > Station > Game Services Config |
| 19 | Targeting | TargetingConfig | VoidHarvest > Targeting > Targeting Config |
| 20 | Targeting | TargetingVFXConfig | VoidHarvest > Targeting > Targeting VFX Config |
| 21 | Targeting | TargetingAudioConfig | VoidHarvest > Targeting > Targeting Audio Config |
| 22 | World | WorldDefinition | VoidHarvest > World > World Definition |

---

## Camera System

### 1. CameraConfig

**What it does:** Controls the 3rd-person orbiting camera that follows the player's ship. Every limit, sensitivity value, and starting angle for the camera is set here.

**Where it lives in the game:** The camera system reads this asset at startup to determine how far the camera can zoom, how high or low it can look, and where it begins when the game starts.

**Menu path:** `Create > VoidHarvest > Camera > Camera Config`

**Asset folder:** `Assets/Features/Camera/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Min Pitch | Lowest angle the camera can look down, in degrees. Negative means below the horizon. | -80 | Should be a negative number. |
| Max Pitch | Highest angle the camera can look up, in degrees. | 80 | Should be a positive number. |
| Min Distance | Closest the camera can get to the ship, in meters. | 5 | Must be greater than 0. |
| Max Distance | Farthest the camera can pull away from the ship, in meters. | 50 | Must be greater than Min Distance. |
| Min Zoom Distance | Closest zoom distance when the ship is at full speed, in meters. | 10 | Must be at least Min Distance. |
| Max Zoom Distance | Farthest zoom distance when the ship is stopped, in meters. | 40 | Must be no greater than Max Distance. |
| Zoom Cooldown Duration | How many seconds after the player manually zooms before the automatic speed-based zoom kicks back in. | 2.0 | 0 or higher. |
| Orbit Sensitivity | How fast the camera rotates when the player drags the mouse, in degrees per pixel of mouse movement. | 0.1 | Must be greater than 0. Higher values make the camera spin faster. |
| Default Yaw | Starting horizontal rotation angle in degrees when the game begins. | 0 | -- |
| Default Pitch | Starting vertical angle in degrees when the game begins. | 15 | Must be between Min Pitch and Max Pitch. |
| Default Distance | Starting distance from the ship in meters when the game begins. | 25 | Must be between Min Distance and Max Distance. |

**Designer tips:**
- Keep Min Pitch around -80 and Max Pitch around 80 to prevent the camera from flipping directly above or below the ship.
- If the automatic zoom feels sluggish after the player scrolls, reduce Zoom Cooldown Duration.
- Orbit Sensitivity of 0.1 feels natural on a standard mouse at 1080p. Increase for higher resolutions.

---

### 2. SkyboxConfig

**What it does:** Determines the space background (nebula panorama) visible in a scene. Controls which sky material is used, how fast it rotates, and how bright it appears.

**Where it lives in the game:** The camera system applies this to the scene's sky rendering at startup. The slowly rotating nebula in the background comes from this asset.

**Menu path:** `Create > VoidHarvest > Camera > Skybox Config`

**Asset folder:** `Assets/Features/Camera/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Skybox Material | The primary nebula panorama material used as the sky background. | (none -- must be assigned) | Drag a panoramic sky material here from the Project window. |
| Fallback Material | A backup sky material used if the primary one is missing. | (none -- must be assigned) | Acts as a safety net. Assign a simple starfield material. |
| Rotation Speed | How fast the sky background rotates, in degrees per second. | 0.5 | Slider: 0 (frozen) to 5 (fast spin). |
| Exposure Override | Brightness multiplier for the sky. 1.0 is normal brightness. | 1.0 | Slider: 0.1 (very dark) to 3.0 (very bright). |

**Designer tips:**
- A Rotation Speed between 0.3 and 0.8 creates a subtle sense of motion without causing disorientation.
- Exposure Override above 1.5 can wash out dark nebula textures. Test in the Game view before shipping.
- Always assign a Fallback Material so the sky never turns solid magenta if the primary fails to load.

---

## Docking System

### 3. DockingConfig

**What it does:** Controls every distance, timing, and threshold involved when a ship docks at or undocks from a station. This single asset governs how close you need to be, how long the snap animation takes, timeouts, and alignment strictness.

**Where it lives in the game:** The docking simulation reads these values every frame while a docking or undocking sequence is in progress.

**Menu path:** `Create > VoidHarvest > Docking > Docking Config`

**Asset folder:** `Assets/Features/Docking/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Max Docking Range | Maximum distance (meters) from the station at which the player can start a docking sequence. | 500 | Must be greater than 0. |
| Snap Range | Distance (meters) at which the magnetic snap animation begins pulling the ship into the dock. | 30 | Must be greater than 0 and less than Max Docking Range. |
| Snap Duration | How many seconds the snap animation takes from start to clamped-in-dock. | 1.5 | Must be greater than 0. |
| Undock Clearance Distance | How far (meters) the ship moves away from the station during the undock push-off animation. | 100 | Must be greater than 0. |
| Undock Duration | How many seconds the undock push-off animation takes. | 2 | Must be greater than 0. |
| Approach Timeout | Safety cutoff in seconds. If the ship hasn't reached snap range within this time, the docking attempt is canceled. | 120 | Must be greater than 0. |
| Align Timeout | Safety cutoff in seconds. If the ship can't finish alignment within this time, the dock forces a snap. | 30 | Must be greater than 0. |
| Align Dot Threshold | How precisely the ship must be oriented before alignment is considered complete. 1.0 means perfect alignment; lower values are more forgiving. | 0.999 | Between 0 and 1 (exclusive to inclusive). Higher is stricter. |
| Align Ang Vel Threshold | How still the ship's rotation must be (in radians per second) before alignment is considered settled. | 0.01 | Must be greater than 0. Lower is stricter. |

**Designer tips:**
- Max Docking Range of 500 means the player can start docking from quite far away. Reduce it for a more hands-on feel.
- Snap Duration of 1.5 seconds is fast enough to feel snappy but slow enough for the visual payoff. Go lower for arcade feel, higher for cinematic.
- Leave Align Dot Threshold at 0.999 unless testing reveals alignment lockups. Lowering it makes docking more forgiving but visually sloppier.

---

### 4. DockingVFXConfig

**What it does:** Stores references to visual effects and intensity values used during the docking sequence -- the glow as you approach, the flash when you snap in, and the release burst when you undock.

**Where it lives in the game:** The docking visual feedback system reads this asset and spawns the referenced effects at the right moments during docking.

**Menu path:** `Create > VoidHarvest > Docking > Docking VFX Config`

**Asset folder:** `Assets/Features/Docking/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Alignment Guide Effect | A visual effect template spawned to show the alignment guide during approach. | (none -- assign a VFX template) | Drag a particle or VFX template from the Project window. |
| Approach Glow Intensity | Brightness of the glow effect as the ship approaches the docking port. | 1.0 | -- |
| Snap Flash Effect | A visual effect template spawned as a brief flash when the ship locks into the dock. | (none -- assign a VFX template) | Drag a particle or VFX template from the Project window. |
| Snap Flash Duration | How many seconds the snap flash lasts. | 0.5 | -- |
| Undock Release Effect | A visual effect template spawned when the ship releases from the dock. | (none -- assign a VFX template) | Drag a particle or VFX template from the Project window. |

**Designer tips:**
- These effect slots are optional. The docking system works without them; the effects are purely cosmetic.
- Keep Snap Flash Duration short (0.3 to 0.7) for a punchy feel.

---

### 5. DockingAudioConfig

**What it does:** Stores sound file references and volume settings for all audio that plays during docking and undocking.

**Where it lives in the game:** The docking audio feedback system plays these sounds at the appropriate moments during the docking sequence.

**Menu path:** `Create > VoidHarvest > Docking > Docking Audio Config`

**Asset folder:** `Assets/Features/Docking/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Approach Hum Clip | A looping ambient hum that plays while the ship approaches the dock. | (none -- assign a sound file) | -- |
| Dock Clamp Clip | The metallic clamp sound when the ship locks in. | (none -- assign a sound file) | -- |
| Dock Clamp Volume | Volume of the clamp sound. | 0.8 | Slider: 0 (silent) to 1 (full). |
| Undock Release Clip | The release/hiss sound when the ship undocks. | (none -- assign a sound file) | -- |
| Undock Release Volume | Volume of the release sound. | 0.6 | Slider: 0 (silent) to 1 (full). |
| Engine Start Clip | Engine ignition sound played after undock completes. | (none -- assign a sound file) | -- |
| Max Audible Distance | Maximum distance in meters at which docking sounds are audible to the player. | 200 | -- |

**Designer tips:**
- All sound slots are optional. If left empty, the docking sequence plays silently.
- Max Audible Distance works with 3D spatial audio rolloff. Set it high enough that the player always hears their own docking sounds.

---

## Input System

### 6. InteractionConfig

**What it does:** Controls timing and distance thresholds for player input -- how fast a double-click must be, how far you drag before a radial menu opens, and default distances for autopilot commands.

**Where it lives in the game:** The input bridge and radial menu systems read this asset to decide how to interpret mouse clicks and set command distances.

**Menu path:** `Create > VoidHarvest > Input > Interaction Config`

**Asset folder:** `Assets/Features/Input/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Double Click Window | Maximum time (seconds) between two clicks for them to count as a double-click. | 0.3 | Range: 0.1 to 1.0. |
| Radial Menu Drag Threshold | How many pixels the mouse must move after pressing before the radial menu opens. | 5 | Range: 1 to 20. |
| Default Approach Distance | Default distance (meters) from the target when the player uses the "Approach" command. | 50 | Must be greater than 0. |
| Default Orbit Distance | Default distance (meters) from the target when the player uses the "Orbit" command. | 100 | Must be greater than 0. |
| Default Keep At Range Distance | Default standoff distance (meters) for the "Keep at Range" command. | 50 | Must be greater than 0. |
| Mining Beam Max Range | Maximum distance (meters) at which the mining beam can reach an asteroid. | 50 | Must be greater than 0. |

**Designer tips:**
- A Double Click Window of 0.3 seconds feels responsive. Values above 0.5 may cause accidental double-clicks.
- Radial Menu Drag Threshold of 5 pixels prevents the menu from opening on tiny mouse jiggles. Increase to 10 for players on high-DPI mice.
- Mining Beam Max Range directly affects gameplay difficulty. A shorter range forces players to fly closer to asteroids.

---

## Mining System

### 7. OreDefinition

**What it does:** Defines everything about a single ore type -- its name, rarity, how fast it mines, how hard it is to extract, what color the mining beam turns, what it refines into, and how much it costs to refine. This is the most important configuration asset for the mining economy.

**Where it lives in the game:** The mining system, inventory, refining system, and HUD all read from these assets. Each asteroid in a field is assigned one ore type, and that ore type's settings come from its Ore Definition.

**Menu path:** `Create > VoidHarvest > Mining > Ore Definition`

**Asset folder:** `Assets/Features/Mining/Data/`

**Existing assets:** Luminite (Common), Ferrox (Uncommon), Auralite (Rare).

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Ore Id | A unique text identifier for this ore (e.g., "luminite"). Used internally to track inventory. | (empty -- must be set) | Must not be empty. Use lowercase, no spaces. |
| Display Name | The name shown to the player in the HUD and inventory screens. | (empty -- must be set) | Must not be empty. |
| Rarity Tier | Classification: Common, Uncommon, or Rare. Affects UI display and future tech-tree gating. | Common | Choose from the dropdown. |
| Icon | A 2D image shown in inventory and tooltip displays. | (none) | Optional. Drag a sprite from the Project window. |
| Base Value | Sell price per unit in credits. | 0 | Must be 0 or higher. |
| Description | Flavor text shown in tooltips. | (empty) | Optional. Multi-line text area. |
| Rarity Weight | Default spawn probability weight for this ore. | 0 | Slider: 0 to 1. Used by asteroid fields that don't override weights. |
| Base Yield Per Second | How many units of ore the mining beam extracts per second before any modifiers. | 0 | Must be greater than 0. |
| Hardness | Extraction difficulty. Higher values reduce mining speed. Acts as a divider in the yield formula. | 0 | Must be greater than 0. |
| Volume Per Unit | How much cargo space each unit of this ore occupies. | 0 | Must be greater than 0. |
| Beam Color | The color the mining laser turns when extracting this ore. | (white) | Use the color picker. |
| Base Processing Time Per Unit | How many seconds it takes to refine one unit of this ore at a station. | 0 | Must be greater than 0. |
| Refining Outputs | A list of raw materials produced when this ore is refined. Each entry specifies the output material, base yield per unit, and a variance range. | (empty list) | See the Refining Outputs sub-table below. |
| Refining Credit Cost Per Unit | How many credits the player pays per unit of ore to start a refining job. | 0 | Must be 0 or higher. |

**Refining Outputs (sub-entries):**

Each entry in the Refining Outputs list has:

| Setting | What It Controls | Notes |
|---|---|---|
| Material | Which Raw Material Definition this output produces. | Must be assigned. Drag a Raw Material Definition asset here. |
| Base Yield Per Unit | How many units of this material are produced per unit of ore refined. | Must be greater than 0. |
| Variance Min | Minimum random offset added to the base yield. Can be negative for occasional low rolls. | Must be less than or equal to Variance Max. |
| Variance Max | Maximum random offset added to the base yield. | -- |

**Designer tips:**
- To add a new ore type: create a new Ore Definition, set all required fields, then add it to an Asteroid Field Definition (see #12).
- Hardness and Base Yield Per Second work together. High hardness + low yield = slow, difficult mining. Low hardness + high yield = fast, easy mining.
- Refining Outputs is how you control the refining economy. A rare ore should produce more valuable or larger quantities of raw materials.
- The Variance Min/Max on refining outputs adds randomness. A range of -1 to +2 means each unit could produce 1 fewer or 2 more than the base amount.

---

### 8. OreChunkConfig

**What it does:** Controls the cosmetic ore chunk particles that fly out of an asteroid during mining and get pulled toward the player's ship. This is purely a visual/audio feedback effect -- it does not affect actual ore yield.

**Where it lives in the game:** The mining chunk spawner reads this to decide how often chunks appear, how big they are, how fast they drift and then get attracted to the ship.

**Menu path:** `Create > VoidHarvest > Mining > Ore Chunk Config`

**Asset folder:** `Assets/Features/Mining/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Spawn Interval Min | Minimum seconds between chunk spawn events during mining. | 3.0 | -- |
| Spawn Interval Max | Maximum seconds between chunk spawn events during mining. | 7.0 | -- |
| Chunks Per Spawn Min | Minimum number of chunks spawned per event. | 2 | -- |
| Chunks Per Spawn Max | Maximum number of chunks spawned per event. | 5 | -- |
| Chunk Scale Min | Smallest visual size of a chunk. | 0.03 | -- |
| Chunk Scale Max | Largest visual size of a chunk. | 0.12 | -- |
| Initial Drift Duration | How many seconds chunks drift outward from the asteroid before being attracted to the ship. | 0.75 | -- |
| Initial Drift Speed | How fast (meters per second) chunks drift outward during the drift phase. | 2.0 | -- |
| Attraction Speed | Maximum speed (meters per second) at which chunks fly toward the ship. | 8.0 | -- |
| Attraction Acceleration | How quickly (meters per second squared) chunks ramp up to full attraction speed. | 3.0 | -- |
| Collection Flash Duration | How long (seconds) the flash effect lasts when a chunk reaches the ship. | 0.15 | -- |
| Max Lifetime | Safety timeout (seconds). Chunks that haven't been collected within this time are removed. | 5.0 | -- |
| Glow Intensity | How brightly chunks glow with the ore's color. | 2.0 | -- |

**Designer tips:**
- Shorter Spawn Interval values make mining feel more active and rewarding. Longer values feel calmer.
- Attraction Speed and Attraction Acceleration together control the "magnet" feel. High acceleration with moderate top speed gives a satisfying snap.
- Glow Intensity of 2.0 makes chunks pop against the dark of space. Values above 4.0 may bloom excessively.

---

### 9. MiningVFXConfig

**What it does:** Controls the visual appearance of the mining laser beam, the sparks at the impact point on the asteroid, and the heat shimmer distortion effect.

**Where it lives in the game:** The mining beam view reads this to set up and animate the laser, sparks, and heat effects during active mining.

**Menu path:** `Create > VoidHarvest > Mining > Mining VFX Config`

**Asset folder:** `Assets/Features/Mining/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Beam Width | Thickness of the mining laser beam in meters. | 0.15 | -- |
| Beam Pulse Speed | How many times per second the beam pulses (width oscillation cycles). | 3.0 | -- |
| Beam Pulse Amplitude | How much the beam width oscillates. 0 = no pulse, 1 = pulse between zero and double width. | 0.3 | Slider: 0 to 1. |
| Spark Emission Rate | Number of spark particles spawned per second at the laser impact point. | 15 | -- |
| Spark Lifetime | How long (seconds) each spark particle lives before fading. | 0.4 | -- |
| Spark Speed | How fast (meters per second) sparks fly outward from the impact. | 3.0 | -- |
| Heat Haze Intensity | Opacity of the heat shimmer distortion effect at the impact point. | 0.5 | Slider: 0 (invisible) to 1 (full distortion). |
| Heat Haze Scale | Size (meters) of the heat distortion area. | 0.3 | -- |

**Designer tips:**
- Beam Width of 0.15 is thin enough to look like a precision laser. Increase for a chunkier industrial look.
- Spark Emission Rate of 15 gives a moderate shower. Double it for more dramatic feedback on rare ores.
- Heat Haze Intensity at 0.5 is subtle. Set to 0 to disable the effect entirely on lower-end hardware profiles.

---

### 10. MiningAudioConfig

**What it does:** Stores all sound file references and volume/pitch settings for mining audio -- the laser hum, impact sparks, crumbling thresholds, the final asteroid explosion, and the ore collection clink.

**Where it lives in the game:** The mining audio system reads this and plays the appropriate sounds in 3D space during mining.

**Menu path:** `Create > VoidHarvest > Mining > Mining Audio Config`

**Asset folder:** `Assets/Features/Mining/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Laser Hum Clip | The looping sound of the active mining beam. | (none -- assign a sound file) | If left empty, a procedural placeholder sound is used. |
| Laser Hum Base Volume | Baseline volume of the laser hum. | 0.6 | Slider: 0 to 1. |
| Laser Hum Pitch Min | Pitch of the laser hum when the asteroid is at 0% depletion (freshly started). | 0.8 | -- |
| Laser Hum Pitch Max | Pitch of the laser hum when the asteroid is at 100% depletion (about to break). | 1.4 | -- |
| Laser Hum Fade Out Duration | How many seconds the laser hum takes to fade out when mining stops. | 0.3 | -- |
| Spark Crackle Clip | The crackling sound at the beam's impact point. | (none -- assign a sound file) | If left empty, a procedural placeholder is used. |
| Spark Crackle Volume | Volume of the spark crackle. | 0.4 | Slider: 0 to 1. |
| Crumble Rumble Clip | A deeper rumble played when the asteroid crosses a depletion threshold (25%, 50%, 75%). | (none -- assign a sound file) | If left empty, a procedural placeholder is used. |
| Crumble Rumble Volume | Volume of the crumble rumble. | 0.7 | Slider: 0 to 1. |
| Explosion Clip | The explosion sound when the asteroid is fully depleted and breaks apart. | (none -- assign a sound file) | If left empty, a procedural placeholder is used. |
| Explosion Volume | Volume of the explosion. | 0.8 | Slider: 0 to 1. |
| Collection Clink Clip | The small "clink" sound when an ore chunk reaches the ship. | (none -- assign a sound file) | If left empty, a procedural placeholder is used. |
| Collection Clink Volume | Volume of the collection clink. | 0.3 | Slider: 0 to 1. |
| Max Audible Distance | Maximum distance (meters) at which mining sounds are audible. | 100 | Controls 3D spatial audio rolloff. |

**Designer tips:**
- The pitch ramp (Laser Hum Pitch Min to Pitch Max) gives audio feedback that the asteroid is nearly depleted. A wider range makes the warning more dramatic.
- All clip slots can be left empty during development. The system will use generated placeholder sounds.
- Collection Clink Volume should be quiet (0.2 to 0.4) so it doesn't become annoying during extended mining.

---

### 11. DepletionVFXConfig

**What it does:** Controls the visual effects that show an asteroid's depletion progress -- the growing glow on its surface, the crumble bursts at 25%/50%/75% depletion thresholds, and the final fragment explosion when it breaks apart.

**Where it lives in the game:** The depletion visual system reads this to animate the asteroid's appearance as it gets mined down to nothing.

**Menu path:** `Create > VoidHarvest > Mining > Depletion VFX Config`

**Asset folder:** `Assets/Features/Mining/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Vein Glow Min Intensity | Glow brightness at 0% depletion (fully intact asteroid). | 0.0 | -- |
| Vein Glow Max Intensity | Glow brightness at 100% depletion (just before breaking). | 0.6 | -- |
| Vein Glow Color | The color of the depletion glow on the asteroid surface. | Warm orange (R:1.0, G:0.8, B:0.4) | Use the color picker. |
| Vein Glow Pulse Speed | How many times per second the glow pulses. | 1.5 | -- |
| Vein Glow Pulse Amplitude | How much the glow intensity oscillates relative to its current brightness. | 0.15 | Slider: 0 to 1. |
| Crumble Burst Count Base | Number of particles in the first crumble burst (at the 25% depletion threshold). | 8 | -- |
| Crumble Burst Count Scale | Multiplier applied per successive threshold. The 50% burst has base x scale particles, the 75% burst has base x scale x scale. | 1.5 | -- |
| Crumble Burst Speed | How fast (meters per second) crumble particles fly outward. | 5.0 | -- |
| Crumble Burst Lifetime | How long (seconds) each crumble particle lasts. | 0.5 | -- |
| Crumble Flash Duration | How long (seconds) the bright flash lasts at each crumble threshold. | 0.3 | -- |
| Fragment Count | Number of rock fragments in the final explosion when the asteroid is fully depleted. | 12 | Slider: 8 to 15. |
| Fragment Speed | How fast (meters per second) fragments fly outward during the final explosion. | 4.0 | -- |
| Fragment Lifetime | How long (seconds) fragments remain visible before fading. | 3.0 | -- |
| Fragment Scale Range | Minimum and maximum size of explosion fragments. | Min: 0.05, Max: 0.2 | Two-value range (X = min, Y = max). |

**Designer tips:**
- Vein Glow Max Intensity is kept at 0.6 because there is no vein texture mask yet. Once vein textures are added, this can go higher for more dramatic effect.
- Crumble Burst Count Scale of 1.5 means each successive threshold burst is 50% larger than the previous. Increase for more dramatic escalation.
- Fragment Count of 12 is a good balance. Going above 15 may cause frame rate drops on lower-end hardware.

---

## Procedural System

### 12. AsteroidFieldDefinition

**What it does:** Defines an entire asteroid field -- which ore types appear, how many asteroids to spawn, how big the field is, asteroid sizes, rotation speeds, and the visual appearance of each ore type's asteroids.

**Where it lives in the game:** The asteroid field spawner reads this at scene startup and generates the entire field of asteroids from it. Each asteroid gets an ore type selected based on the weights you configure.

**Menu path:** `Create > VoidHarvest > Procedural > Asteroid Field Definition`

**Asset folder:** `Assets/Features/Procedural/Data/`

**Existing asset:** DefaultField (300 asteroids with Luminite, Ferrox, and Auralite).

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Field Name | A human-readable label for this asteroid field. | (empty) | For your reference only; not shown to the player. |
| Ore Entries | A list of ore types that appear in this field, with weights and visual settings. | (empty list) | Must have at least one entry. See the Ore Entries sub-table below. |
| Asteroid Count | Total number of asteroids spawned in this field. | 0 | Must be greater than 0. |
| Field Radius | Radius (meters) of the spherical volume where asteroids are placed. | 0 | Must be greater than 0. |
| Asteroid Size Min | Smallest possible asteroid radius. | 0 | Must be greater than 0. |
| Asteroid Size Max | Largest possible asteroid radius. | 0 | Must be at least Asteroid Size Min. |
| Rotation Speed Min | Slowest asteroid tumble speed (degrees per second). | 0 | -- |
| Rotation Speed Max | Fastest asteroid tumble speed (degrees per second). | 0 | Must be at least Rotation Speed Min. |
| Seed | A number that controls the random placement and assignment of asteroids. Same seed = same field every time. | 0 | Any whole number. Change this to get a different random layout. |
| Min Scale Fraction | The smallest an asteroid can shrink to when fully depleted, as a fraction of its original size. 0.3 means it shrinks to 30% of its starting size. | 0.3 | Slider: 0.1 to 0.5. |

**Ore Entries (sub-entries):**

Each entry in the Ore Entries list has:

| Setting | What It Controls | Notes |
|---|---|---|
| Ore Definition | A reference to the Ore Definition asset for this ore type. | Must be assigned. Drag an Ore Definition asset here. |
| Weight | Relative spawn probability. Higher weight = more asteroids of this type. The system normalizes all weights automatically. | Must be greater than 0. |
| Mesh Variant A | First 3D shape used for asteroids of this ore type. | Drag an asteroid mesh from the Project window. |
| Mesh Variant B | Second 3D shape for visual variety. | Drag a different asteroid mesh here. |
| Tint Color | Color tint applied to asteroids of this ore type so different ores look visually distinct. | Use the color picker. |

**Designer tips:**
- Weights are relative. If Luminite has weight 60, Ferrox has 30, and Auralite has 10, then 60% of asteroids will be Luminite, 30% Ferrox, and 10% Auralite.
- Changing the Seed gives you a completely different field layout with the same ore distribution. Useful for variety across regions.
- Min Scale Fraction of 0.3 means a fully depleted asteroid still shows 30% of its original size before breaking. Lower values make depleted asteroids look more dramatic.

---

## Resources System

### 13. RawMaterialDefinition

**What it does:** Defines a refined material -- something produced by refining raw ore at a station. Each material has a name, sell value, and cargo volume.

**Where it lives in the game:** The refining system produces these materials. The selling system uses their Base Value to calculate sale price. The inventory system uses Volume Per Unit to track cargo space.

**Menu path:** `Create > VoidHarvest > Station > Raw Material Definition`

**Asset folder:** `Assets/Features/Station/Data/RawMaterials/`

**Existing assets:** Luminite Ingots, Energium Dust, Ferrox Slabs, Conductive Residue, Auralite Shards, Quantum Essence.

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Material Id | A unique text identifier (e.g., "luminite_ingots"). | (empty -- must be set) | Must not be empty. Use lowercase with underscores. |
| Display Name | The name shown to the player in UI panels (e.g., "Luminite Ingots"). | (empty -- must be set) | Must not be empty. |
| Icon | A 2D image shown in inventory and tooltip displays. | (none) | Optional. Drag a sprite here. |
| Description | Flavor text for tooltips. | (empty) | Optional. Multi-line text area. |
| Base Value | Sell price per unit in credits. | 0 | -- |
| Volume Per Unit | How much cargo space each unit of this material occupies. | 0 | -- |

**Designer tips:**
- Refined materials should generally have higher Base Value than their source ore to make refining worthwhile.
- Each Ore Definition's Refining Outputs list references these assets. When you create a new Raw Material Definition, remember to link it from the appropriate Ore Definition.

---

## Ship System

### 14. ShipArchetypeConfig

**What it does:** Defines a complete ship class -- its physics (mass, thrust, speed, rotation), mining capability, cargo space, visual appearance, and targeting capabilities. Every ship in the game is built from one of these templates.

**Where it lives in the game:** The ship physics system, mining system, inventory system, and targeting system all read from the active ship's archetype to determine movement, mining speed, cargo limits, and lock parameters.

**Menu path:** `Create > VoidHarvest > Ship > Ship Archetype Config`

**Asset folder:** `Assets/Features/Ship/Data/`

**Existing assets:** Starter Mining Barge, Medium Mining Barge, Heavy Mining Barge.

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Archetype Id | A unique text identifier for this ship class (e.g., "starter_mining_barge"). | (empty -- must be set) | Must not be empty. |
| Display Name | The name shown to the player in the HUD. | (empty -- must be set) | Must not be empty. |
| Role | The ship's specialization. Choices: Mining Barge, Hauler, Combat Scout, Explorer, Refinery. | Mining Barge | Choose from the dropdown. |
| Mass | Ship mass in kilograms. Higher mass means slower acceleration but more stable movement. | 0 | Must be greater than 0. |
| Max Thrust | Maximum engine force in Newtons. Determines acceleration (force divided by mass). | 0 | Must be greater than 0. |
| Max Speed | Speed cap in meters per second. The ship cannot exceed this speed. | 0 | Must be greater than 0. |
| Rotation Torque | Maximum rotational force. Higher values let the ship turn faster. | 0 | Must be greater than 0. |
| Linear Damping | How quickly the ship slows down when engines are off. 0 = no slowdown (pure Newtonian). Higher values add drag. | 0 | Must be 0 or higher. |
| Angular Damping | How quickly the ship's rotation slows down. 0 = no rotational drag. | 0 | Must be 0 or higher. |
| Mining Power | Multiplier applied to the ore yield when this ship mines. Higher values extract more ore per second. | 0 | -- |
| Module Slots | Number of equipment module slots on this ship. | 0 | For future use (Phase 1+). |
| Cargo Capacity | Maximum total cargo volume in cubic meters. | 0 | Must be greater than 0. |
| Cargo Slots | Number of distinct item slots in the cargo hold. | 20 | Must be at least 1. Values above 100 produce a console note. |
| Hull Mesh | The 3D model used for this ship's hull. | (none -- assign a mesh) | Drag a mesh from the Project window. |
| Hull Material | The surface material (textures, colors) for this ship's hull. | (none -- assign a material) | Drag a material from the Project window. |
| Base Lock Time | How many seconds it takes this ship to acquire a target lock. | 1.5 | Must be greater than 0. |
| Max Target Locks | Maximum number of targets this ship can lock simultaneously. | 3 | Must be at least 1. |
| Max Lock Range | Maximum distance (meters) at which this ship can lock a target. | 5000 | Must be greater than 0. |

**Designer tips:**
- The feel of a ship comes from the relationship between Mass, Max Thrust, and Damping values. High mass + high thrust = heavy but powerful. Low mass + low thrust = nimble but weak.
- Linear Damping above 0 makes the ship feel more like a car (slows when you release thrust). A value of 0 gives true Newtonian physics (the ship coasts forever).
- Cargo Capacity is in volume units. Cargo Slots is the number of distinct inventory rows. Both limit what the player can carry.
- Base Lock Time and Max Target Locks let you differentiate ship roles. A combat-oriented ship might have shorter lock time and more locks, while a mining barge has fewer.

---

## Station System

### 15. StationPresetConfig

**What it does:** Documents the physical module layout of a station -- which building blocks make up the station and where they are positioned. Currently informational and used as a reference for pre-built station templates. In future phases, this will drive procedural station generation.

**Where it lives in the game:** Referenced by Station Definition assets. Currently for documentation; Phase 2+ will use it for automatic station assembly.

**Menu path:** `Create > VoidHarvest > Station > Station Preset Config`

**Asset folder:** `Assets/Features/Station/Data/Presets/`

**Existing assets:** SmallMiningRelay, MediumRefineryHub.

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Preset Name | Human-readable name for this station layout (e.g., "Small Mining Relay"). | (empty) | -- |
| Preset Id | A unique text identifier for this preset. | (empty) | -- |
| Description | A note describing what this station type is for. | (empty) | Multi-line text area (2 to 4 lines). |
| Modules | A list of building blocks that compose this station. | (empty list) | See the Modules sub-table below. |

**Modules (sub-entries):**

Each entry in the Modules list has:

| Setting | What It Controls | Notes |
|---|---|---|
| Module Prefab | The reusable template (3D model) for this module piece. | Drag a station module template from the Project window. |
| Local Position | Position offset (X, Y, Z) of this module relative to the station's center. | -- |
| Local Rotation | Rotation of this module relative to the station's center. | Edited as Euler angles (X, Y, Z degrees) in the Inspector. |
| Module Role | The function of this module (e.g., "control", "storage", "energy", "docking"). | Free text -- use consistent role names. |

**Designer tips:**
- Think of each module as a Lego brick. The preset defines how they snap together.
- Module Role is currently free text. Use consistent names like "control", "storage", "energy", "docking", "refinery" across all presets.

---

### 16. StationDefinition

**What it does:** The complete definition of a single station in the game world. Contains its identity, world position, what services it offers, docking port placement, and visual appearance. Each station in the game needs exactly one of these.

**Where it lives in the game:** The World Definition asset holds a list of Station Definitions. At game startup, every station is created from its definition.

**Menu path:** `Create > VoidHarvest > Station > Station Definition`

**Asset folder:** `Assets/Features/Station/Data/Definitions/`

**Existing assets:** SmallMiningRelay (ID 1), MediumRefineryHub (ID 2).

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Station Id | A unique whole number identifying this station. Must be different from every other station in the World Definition. | 0 | Must be greater than 0. |
| Display Name | The name shown to the player in the HUD when they target or dock at this station. | (empty -- must be set) | Must not be empty. |
| Description | A designer note describing this station. | (empty) | Optional. Multi-line text area. |
| Station Type | Classification. Choices: Mining Relay, Refinery Hub, Trade Post, Research Station. | Mining Relay | Choose from the dropdown. |
| World Position | The X, Y, Z coordinates where this station is placed in the game world. | (0, 0, 0) | -- |
| World Rotation | The orientation of the station in the game world. | No rotation (identity) | Edited as Euler angles in the Inspector. |
| Available Services | A list of service names this station offers. Common values: "Sell", "Refine", "Repair", "Cargo". | (empty list) | Must have at least one entry. |
| Services Config | A reference to a Station Services Config asset that defines this station's service capabilities (refining speed, repair cost, etc.). | (none -- must be assigned) | Must be assigned. |
| Preset Config | A reference to a Station Preset Config describing this station's physical layout. | (none) | Optional. For Phase 2+ procedural generation. |
| Docking Port Offset | Position (X, Y, Z) of the docking port relative to the station's center. | (0, 0, 0) | Offset magnitude must be less than 200 meters. |
| Docking Port Rotation | Rotation of the docking port relative to the station's center. | No rotation (identity) | Edited as Euler angles in the Inspector. |
| Safe Undock Direction | The direction the ship is pushed when undocking. | Forward (0, 0, 1) | Should be a normalized direction (length of 1). |
| Prefab | The reusable template for the station's 3D model. | (none) | Optional. Drag a station template from the Project window. |
| Icon | A 2D image for this station in HUD and UI displays. | (none) | Optional. Drag a sprite here. |

**Designer tips:**
- Available Services controls which tabs appear in the station menu when the player docks. "Sell" enables the sell panel, "Refine" enables refining, "Repair" enables the repair panel, "Cargo" enables cargo transfer.
- The Docking Port Offset determines where the ship snaps to. Position it at the actual docking bay location on the station's 3D model.
- Safe Undock Direction should point away from the station's body so the ship doesn't clip through the structure during undocking.

---

### 17. StationServicesConfig

**What it does:** Defines the service capabilities of a station -- how many refining jobs can run at once, how fast refining runs, and how much repairs cost. Different stations can have different service configs to create economic variety.

**Where it lives in the game:** Referenced by each Station Definition. The refining and repair systems read these values when the player uses station services.

**Menu path:** `Create > VoidHarvest > Station > Station Services Config`

**Asset folder:** `Assets/Features/Station/Data/ServiceConfigs/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Max Concurrent Refining Slots | How many refining jobs can run at the same time at this station. | 3 | Must be at least 1. |
| Refining Speed Multiplier | A multiplier on refining speed. Higher values mean faster refining. 1.0 is normal speed. | 1.0 | Must be greater than 0. |
| Repair Cost Per HP | How many credits the player pays per point of hull damage repaired. Set to 0 to disable repair at this station. | 100 | Must be 0 or higher. 0 means no repair service. |

**Designer tips:**
- A small outpost might have 1 or 2 refining slots and a speed multiplier of 1.0. A major refinery hub might have 4+ slots and a multiplier of 1.5 or higher.
- Repair Cost Per HP of 0 effectively disables repair at that station. Use this for stations that are not supposed to offer repair services.
- Remember to also include "Repair" in the Station Definition's Available Services list if you want the repair panel to show up.

---

## Station Services System

### 18. GameServicesConfig

**What it does:** Global game economy settings. Currently contains the number of credits a new player starts with.

**Where it lives in the game:** Read once at game startup to initialize the player's credit balance.

**Menu path:** `Create > VoidHarvest > Station > Game Services Config`

**Asset folder:** `Assets/Features/StationServices/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Starting Credits | How many credits the player begins the game with. | 0 | Must be 0 or higher. |

**Designer tips:**
- A starting balance of 0 means the player must mine and sell ore before they can afford refining or repairs.
- For testing, set this to a large number (e.g., 10000) so you can test all station services immediately.

---

## Targeting System

### 19. TargetingConfig

**What it does:** Controls the visual layout of the targeting system -- reticle sizes, progress arc thickness, off-screen indicator margins, and the preview camera viewport dimensions.

**Where it lives in the game:** The targeting display reads this to size and position reticles, lock progress arcs, off-screen indicators, and target card preview cameras.

**Menu path:** `Create > VoidHarvest > Targeting > Targeting Config`

**Asset folder:** `Assets/Features/Targeting/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Reticle Padding | Extra space (pixels) around a target when sizing the reticle bracket. | 20 | -- |
| Reticle Min Size | Smallest the reticle bracket can be, in pixels. | 40 | -- |
| Reticle Max Size | Largest the reticle bracket can be, in pixels. | 300 | -- |
| Lock Progress Arc Width | Thickness (pixels) of the circular progress arc shown during lock acquisition. | 3 | -- |
| Off Screen Indicator Margin | Distance (pixels) from the screen edge where off-screen target arrows appear. | 30 | -- |
| Viewport Render Width | Width (pixels) of the target card preview image. | 140 | -- |
| Viewport Render Height | Height (pixels) of the target card preview image. | 100 | -- |
| Viewport FOV | Field of view (degrees) for the target card preview camera. Smaller values zoom in on the target. | 30 | -- |
| Preview Stage Offset | 3D offset (X, Y, Z) where target preview clones are placed for rendering. Keeps them out of the player's view. | (0, -1000, 0) | Should be far from the playable area. |

**Designer tips:**
- Reticle Padding of 20 pixels gives comfortable breathing room around targets. Increase for larger, more visible brackets.
- Viewport FOV of 30 provides a tight, close-up preview of locked targets. Increase for a wider view.
- Don't change Preview Stage Offset unless previews are visually interfering with the game world. The default places them 1000 meters below.

---

### 20. TargetingVFXConfig

**What it does:** Controls visual effect timing for the targeting system -- the flash when a lock is confirmed and the pulsing of the reticle corners during lock acquisition.

**Where it lives in the game:** The targeting visual system reads this to animate lock feedback effects.

**Menu path:** `Create > VoidHarvest > Targeting > Targeting VFX Config`

**Asset folder:** `Assets/Features/Targeting/Views/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Lock Flash Duration | How long (seconds) the confirmation flash lasts when a target lock completes. | 0.3 | -- |
| Reticle Pulse Speed | How fast the reticle corners pulse during lock acquisition. Higher values = faster pulsing. | 2.0 | -- |

**Designer tips:**
- Lock Flash Duration of 0.3 is quick and satisfying. Values above 0.5 may feel sluggish.
- Reticle Pulse Speed of 2.0 is a moderate pulse. Match it to the Base Lock Time on the ship archetype for a natural-feeling rhythm.

---

### 21. TargetingAudioConfig

**What it does:** Stores sound file references for all targeting audio feedback -- the sounds for lock acquisition in progress, lock confirmed, lock failed, lock slots full, and target lost.

**Where it lives in the game:** The targeting audio system plays these sounds in response to targeting events.

**Menu path:** `Create > VoidHarvest > Targeting > Targeting Audio Config`

**Asset folder:** `Assets/Features/Targeting/Views/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Lock Acquiring Clip | A rising-tone sound played continuously while a lock is being acquired. | (none -- assign a sound file) | -- |
| Lock Confirmed Clip | A confirmation sound played when a target lock completes successfully. | (none -- assign a sound file) | -- |
| Lock Failed Clip | A failure sound played when a lock is canceled or the target moves out of range. | (none -- assign a sound file) | -- |
| Lock Slots Full Clip | A warning sound played when the player tries to lock a target but all lock slots are occupied. | (none -- assign a sound file) | -- |
| Target Lost Clip | A sound played when a locked target is destroyed or moves out of range. | (none -- assign a sound file) | -- |

**Designer tips:**
- Lock Acquiring Clip should be a looping or long clip since it plays for the entire lock duration (Base Lock Time seconds).
- Lock Confirmed Clip and Lock Failed Clip are one-shot sounds. Keep them short (under 1 second) for punchy feedback.
- All slots are optional. If empty, no sound plays for that event.

---

## World System

### 22. WorldDefinition

**What it does:** The top-level configuration asset for an entire game world. Lists every station in the world, where the player starts, and which ship they begin with. This is the single file that ties everything together.

**Where it lives in the game:** Read once at game startup. The game builds the entire world (stations, player position, starting ship) from this asset.

**Menu path:** `Create > VoidHarvest > World > World Definition`

**Asset folder:** `Assets/Features/World/Data/`

| Setting | What It Controls | Default | Limits / Notes |
|---|---|---|---|
| Stations | A list of all Station Definition assets in this world. Each must have a unique Station Id. | (empty list) | Must have at least one station. No duplicate Station Ids allowed. |
| Player Start Position | The X, Y, Z coordinates where the player's ship spawns at game start. | (0, 0, 0) | -- |
| Player Start Rotation | The orientation of the player's ship at game start. | No rotation (identity) | Edited as Euler angles in the Inspector. |
| Starting Ship Archetype | A reference to the Ship Archetype Config the player begins the game with. | (none -- must be assigned) | Must be assigned. |

**Designer tips:**
- This is the "master" world file. To add a new station to the game, create a Station Definition asset, then add it to this list.
- Player Start Position should be within reasonable flying distance of the first station so new players can find their way quickly.
- Changing Starting Ship Archetype lets you test different ships without modifying any other configuration.

---

## Quick-Reference: Where to Find Existing Assets

The following folders contain the configuration assets currently shipped with VoidHarvest:

| Folder | What's Inside |
|---|---|
| `Assets/Features/Camera/Data/` | CameraConfig, SkyboxConfig |
| `Assets/Features/Docking/Data/` | DockingConfig, DockingVFXConfig, DockingAudioConfig |
| `Assets/Features/Input/Data/` | InteractionConfig |
| `Assets/Features/Mining/Data/` | OreDefinition (x3), OreChunkConfig, MiningVFXConfig, MiningAudioConfig, DepletionVFXConfig |
| `Assets/Features/Procedural/Data/` | AsteroidFieldDefinition (DefaultField) |
| `Assets/Features/Ship/Data/` | ShipArchetypeConfig (x3 archetypes) |
| `Assets/Features/Station/Data/Definitions/` | StationDefinition (x2) |
| `Assets/Features/Station/Data/ServiceConfigs/` | StationServicesConfig (x2) |
| `Assets/Features/Station/Data/Presets/` | StationPresetConfig (x2) |
| `Assets/Features/Station/Data/RawMaterials/` | RawMaterialDefinition (x6) |
| `Assets/Features/StationServices/Data/` | GameServicesConfig |
| `Assets/Features/Targeting/Data/` | TargetingConfig |
| `Assets/Features/Targeting/Views/` | TargetingVFXConfig, TargetingAudioConfig |
| `Assets/Features/World/Data/` | WorldDefinition |

---

## Workflow: Adding a New Ore Type

This walkthrough ties together several configuration assets to show how they work in practice.

1. **Create the Ore Definition.** Right-click in `Assets/Features/Mining/Data/` and choose `Create > VoidHarvest > Mining > Ore Definition`. Fill in all required fields (Ore Id, Display Name, Rarity Tier, Base Yield Per Second, Hardness, Volume Per Unit, Beam Color).

2. **Create or reuse Raw Material Definitions.** If this ore refines into materials that don't exist yet, create new Raw Material Definition assets first. Then add them to the Ore Definition's Refining Outputs list.

3. **Add the ore to an Asteroid Field.** Open the Asteroid Field Definition asset (e.g., DefaultField). Add a new entry to the Ore Entries list. Drag your new Ore Definition into the slot, set a Weight, assign two mesh variants, and pick a Tint Color.

4. **Test.** Enter Play mode and fly to the asteroid field. You should see asteroids with your new tint color. Mine one and confirm the laser color, yield rate, and inventory display all match your settings.

---

## Workflow: Adding a New Station

1. **Create a Station Services Config.** Right-click in `Assets/Features/Station/Data/ServiceConfigs/` and choose `Create > VoidHarvest > Station > Station Services Config`. Set the refining slots, speed multiplier, and repair cost.

2. **(Optional) Create a Station Preset Config.** If you want to document the module layout, create a new Station Preset Config in `Assets/Features/Station/Data/Presets/`.

3. **Create the Station Definition.** Right-click in `Assets/Features/Station/Data/Definitions/` and choose `Create > VoidHarvest > Station > Station Definition`. Fill in the Station Id (must be unique), Display Name, Station Type, World Position, Available Services list, and drag in the Services Config you created in step 1.

4. **Add to the World Definition.** Open the World Definition asset and add your new Station Definition to the Stations list.

5. **Test.** Enter Play mode. The new station should appear at the world position you specified. Fly within docking range and verify that the correct service panels appear.

---

## Validation

VoidHarvest validates every configuration asset automatically when you edit it in the Inspector. If a setting has an invalid value (for example, a negative distance or a missing required reference), a warning message appears in the **Console** window at the bottom of the Unity Editor.

Additionally, there is a **Scene Config Validator** tool available from the menu bar at `Window > VoidHarvest > Scene Config Validator`. This scans all active configuration assets in the scene and reports any issues.

Common warnings and what to do about them:

| Warning | Fix |
|---|---|
| "must not be empty" | Fill in the required text field (usually an Id or Display Name). |
| "must be > 0" | The value must be a positive number. Enter a value greater than zero. |
| "must be >= 0" | The value cannot be negative. Enter zero or a positive number. |
| "must not be null" | A required reference is missing. Drag the correct asset into the empty slot. |
| "must be within [X, Y]" | The value is outside the allowed range. Adjust it to fall between the two limits. |
| "Duplicate StationId" | Two stations share the same Id number. Change one of them. |
