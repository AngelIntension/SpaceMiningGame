# Adding a New Station

This guide walks you through creating a brand-new station in VoidHarvest, from scratch to playable, using only the Unity Editor. No programming required.

---

## Before You Start

Make sure you have the Unity Editor open with the VoidHarvest project loaded. You will be working entirely in the **Project** window and **Inspector** panel.

It helps to look at the two stations that already ship with the game as examples:

| Existing Station | Station ID | Type | Location |
|---|---|---|---|
| Small Mining Relay | 1 | Mining Relay | `Assets/Features/Station/Data/Definitions/SmallMiningRelay.asset` |
| Medium Refinery Hub | 2 | Refinery Hub | `Assets/Features/Station/Data/Definitions/MediumRefineryHub.asset` |

You can click either of those assets and study their Inspector values at any point during this process.

---

## Step-by-Step Workflow

### Step 1 -- Create a Station Services Config

This asset controls the economic and service capabilities of your station: how fast it refines ore, how many refining jobs it can run at once, and whether it offers hull repair.

1. In the **Project** window, navigate to `Assets/Features/Station/Data/ServiceConfigs/`.
2. Right-click in the folder and choose **Create > VoidHarvest > Station > Station Services Config**.
3. Name the new file to match your station (for example, `LargeTradePostServices`).
4. Click the new asset to open it in the **Inspector**.
5. Configure the three fields:

| Field | What It Does | Default | Recommended Range |
|---|---|---|---|
| **Max Concurrent Refining Slots** | How many ores can be refined at the same time at this station. More slots let players queue more refining jobs simultaneously. | 3 | 1 -- 10 |
| **Refining Speed Multiplier** | How fast refining jobs complete. A value of 1.0 is normal speed. Set to 2.0 to make jobs finish in half the time. | 1.0 | 0.1 -- 5.0 |
| **Repair Cost Per HP** | How many credits the player pays for each point of hull damage repaired. Set to **0** to disable the repair service entirely at this station. | 100 | 0 -- 1000 |

**Examples from existing stations:**

- *Small Mining Relay* -- 2 refining slots, 1.0x speed, repair disabled (cost = 0)
- *Medium Refinery Hub* -- 4 refining slots, 1.5x speed, 100 credits per HP repair

---

### Step 2 -- Create a Station Definition

This is the main asset that defines everything about your station: its identity, position in space, what services it offers, and how ships dock with it.

1. In the **Project** window, navigate to `Assets/Features/Station/Data/Definitions/`.
2. Right-click in the folder and choose **Create > VoidHarvest > Station > Station Definition**.
3. Name the file after your station (for example, `LargeTradePost`).
4. Click the new asset to open it in the **Inspector**.
5. Fill in each section as described below.

#### Identity

| Field | What to Enter |
|---|---|
| **Station Id** | A unique whole number, greater than zero. Check the existing stations to avoid duplicates. The current stations use IDs 1 and 2, so your next station could be 3. |
| **Display Name** | The name players will see in the HUD and menus (for example, "Large Trade Post"). |
| **Description** | A short sentence or two describing the station. This appears in tooltips. |
| **Station Type** | Pick one from the dropdown: **MiningRelay**, **RefineryHub**, **TradePost**, or **ResearchStation**. This categorizes the station for future gameplay systems. |

#### World Placement

| Field | What to Enter |
|---|---|
| **World Position** | The X, Y, Z coordinates where the station sits in space. Use large values (hundreds or thousands) to spread stations apart. |
| **World Rotation** | The orientation of the station. Leave at the default (0, 0, 0, 1) for no rotation, or adjust the X/Y/Z Euler angles in the Inspector to face a particular direction. |

#### Services

| Field | What to Enter |
|---|---|
| **Available Services** | A list of service names this station provides. Add entries by clicking the **+** button. Valid service names are: **Cargo**, **Market**, **Refinery**, **Repair**. A station needs at least one service. |
| **Services Config** | Drag the Station Services Config asset you created in Step 1 into this slot. |

#### Docking

| Field | What to Enter |
|---|---|
| **Docking Port Offset** | The X, Y, Z position of the docking port relative to the station's center. For example, (0, 0, 50) places the docking port 50 units in front of the station. |
| **Docking Port Rotation** | The rotation of the docking port. Leave at default unless the port faces a non-standard direction. |
| **Safe Undock Direction** | The direction ships are pushed when they undock. Use a normalized direction like (0, 0, 1) for forward, (0, 1, 0) for up, etc. |

#### Visuals

| Field | What to Enter |
|---|---|
| **Prefab** | Drag a 3D model template from your project into this slot. This is the visual representation of the station in the game world. |
| **Icon** | Drag a 2D sprite image into this slot. This image appears next to the station name in the HUD and menus. |

---

### Step 3 -- (Optional) Create a Station Preset Config

Station Preset Configs define the modular visual layout of a station -- which 3D pieces go where. This is optional and primarily used for future procedural station generation.

1. In the **Project** window, navigate to `Assets/Features/Station/Data/Presets/`.
2. Right-click in the folder and choose **Create > VoidHarvest > Station > Station Preset Config**.
3. Name the file to match your station (for example, `LargeTradePost`).
4. Fill in the fields:

| Field | What to Enter |
|---|---|
| **Preset Name** | A human-readable name (for example, "Large Trade Post"). |
| **Preset Id** | A unique kebab-case identifier (for example, `large-trade-post`). |
| **Description** | A sentence describing the station layout. |
| **Modules** | A list of module entries. For each module, specify: |

Each module entry has four parts:

| Module Field | What to Enter |
|---|---|
| **Module Prefab** | Drag a station module 3D model into this slot (for example, from the Station_MS2 asset pack). |
| **Local Position** | X, Y, Z offset from the station center where this module sits. |
| **Local Rotation** | Orientation of this module relative to the station center. |
| **Module Role** | A label for what this module does: `control`, `storage`, `energy`, `communications`, `connector`, `refinery`, `hangar`, etc. |

5. Back on your **Station Definition** asset, drag the new Preset Config into the **Preset Config** slot.

---

### Step 4 -- Add Your Station to the World

The World Definition is the master list of all stations in the game. Your station will not appear in-game until it is registered here.

1. In the **Project** window, navigate to `Assets/Features/World/Data/`.
2. Click **DefaultWorld.asset** to open it in the **Inspector**.
3. Find the **Stations** list.
4. Click the **+** button at the bottom of the list to add a new entry.
5. Drag your new Station Definition asset into the empty slot.
6. Save the project (**Ctrl+S** or **File > Save Project**).

```
Stations list in DefaultWorld.asset after adding your station:

  [0]  SmallMiningRelay
  [1]  MediumRefineryHub
  [2]  YourNewStation        <-- your addition
```

---

### Step 5 -- Verify Your Station

After saving, the editor runs automatic validation checks. Watch the **Console** window (Window > General > Console) for yellow warning messages. Common warnings and how to fix them:

| Warning Message | What Went Wrong | How to Fix |
|---|---|---|
| "StationId must be > 0" | Station Id is set to 0 or a negative number. | Set Station Id to a positive whole number. |
| "DisplayName must not be empty" | The Display Name field is blank. | Type a name for the station. |
| "ServicesConfig must not be null" | No Station Services Config is assigned. | Drag a Services Config asset into the slot. |
| "AvailableServices must have at least one entry" | The services list is empty. | Add at least one service (Cargo, Market, Refinery, or Repair). |
| "DockingPortOffset magnitude must be < 200" | The docking port is too far from the station center. | Reduce the Docking Port Offset values so the distance is under 200 units. |
| "Duplicate StationId" | Another station in the World Definition uses the same ID. | Change your Station Id to a number not used by any other station. |

You can also use the **Scene Config Validator** window to run a full check across all stations at once: open it from the Unity menu bar at **Window > VoidHarvest > Scene Config Validator**.

---

## Station Definition -- Field-by-Field Reference

| Field Name | Description | Default | Valid Range / Constraints |
|---|---|---|---|
| **Station Id** | Unique integer identifying this station across the entire game world. | None (must be set) | Greater than 0. Must be unique across all stations in the World Definition. |
| **Display Name** | The station name shown to players in the HUD, menus, and tooltips. | None (must be set) | Any non-empty text. |
| **Description** | A short description of the station shown in tooltips and UI panels. | Empty | Any text. Optional but recommended. |
| **Station Type** | Categorizes the station for gameplay systems and UI grouping. | MiningRelay | One of: MiningRelay, RefineryHub, TradePost, ResearchStation. |
| **World Position** | The X, Y, Z coordinates where the station appears in the game world. | (0, 0, 0) | Any 3D coordinates. Use large values (hundreds to thousands) to space stations apart. |
| **World Rotation** | The orientation of the station in the game world. | No rotation (identity) | Any valid rotation. Edited as X, Y, Z degree angles in the Inspector. |
| **Available Services** | List of service names the station offers to docked players. | Empty (must add at least one) | One or more of: Cargo, Market, Refinery, Repair. |
| **Services Config** | Reference to a Station Services Config asset controlling refining slots, speed, and repair cost. | None (must be set) | Must point to a valid Station Services Config asset. |
| **Preset Config** | Reference to a Station Preset Config asset for modular visual layout. | None | Optional. Used for future procedural station generation. |
| **Docking Port Offset** | Position of the docking port relative to the station's center point. | (0, 0, 0) | Distance from center must be less than 200 units. |
| **Docking Port Rotation** | Rotation of the docking port relative to the station. | No rotation (identity) | Any valid rotation. |
| **Safe Undock Direction** | The direction ships are pushed when they undock from this station. | Forward (0, 0, 1) | Should be a normalized direction (values between -1 and 1 that together describe a direction). |
| **Prefab** | The 3D model template used to display the station in the game world. | None | Optional. Drag any station prefab from the project. |
| **Icon** | A 2D image displayed next to the station name in the HUD. | None | Optional. Drag any sprite asset from the project. |

---

## Tips

- **Copy an existing station to get started faster.** Select an existing Station Definition asset in the Project window, press **Ctrl+D** to duplicate it, rename it, and then change the fields you need. Do the same for the Services Config. Just remember to give the new station a unique Station Id.

- **Keep Station IDs sequential.** While any positive number works, using sequential IDs (1, 2, 3, 4...) makes it easier to keep track of your stations and avoids accidental duplicates.

- **Service names are case-sensitive.** Type them exactly as shown: `Cargo`, `Market`, `Refinery`, `Repair`. Misspelled service names will not be recognized by the game.

- **Test docking port placement in the Scene view.** After setting the Docking Port Offset, enter Play mode and fly to the station. If ships dock at an odd angle or clip through the station model, adjust the offset and rotation values.

- **Use the Safe Undock Direction to avoid collisions.** Point it away from the station's geometry so ships do not fly through the station model when undocking. For most stations, (0, 0, 1) -- straight forward -- works well.

- **Disabling repair is as simple as setting cost to zero.** If you set Repair Cost Per HP to 0 in the Services Config, the repair panel will not appear at that station. You do not need to remove "Repair" from the Available Services list, but doing so keeps things tidy.

- **The Scene Config Validator is your friend.** Run it any time you change station data. It checks every station in the world for missing references, duplicate IDs, and invalid values -- all at once.

- **Station types do not currently restrict services.** You can assign any combination of services to any station type. The Station Type field is used for categorization and future gameplay features, not for limiting which services are available.

- **World Position uses the same coordinate system as the Scene view.** You can position a temporary object in the Scene view, read its Transform position, and copy those numbers into the World Position field.

---

## Quick-Reference: File Locations

| Asset Type | Folder |
|---|---|
| Station Definitions | `Assets/Features/Station/Data/Definitions/` |
| Station Services Configs | `Assets/Features/Station/Data/ServiceConfigs/` |
| Station Preset Configs | `Assets/Features/Station/Data/Presets/` |
| World Definition | `Assets/Features/World/Data/DefaultWorld.asset` |
| Raw Material Definitions | `Assets/Features/Station/Data/RawMaterials/` |

---

## See Also

- [Configuration Asset Catalog](scriptable-objects.md) -- Complete reference for all configuration assets in VoidHarvest.
