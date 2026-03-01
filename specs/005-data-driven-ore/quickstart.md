# Quickstart: Data-Driven Ore System

**Feature**: 005-data-driven-ore
**Date**: 2026-03-01

## Creating a New Ore Type

1. In the Unity Project window, right-click in `Assets/Features/Mining/Data/Ores/`.
2. Select **Create > VoidHarvest > Ore Definition**.
3. Name the asset (e.g., `Titanite`).
4. Fill in the Inspector fields:

| Field | What to set | Example |
|-------|-------------|---------|
| Ore Id | Unique lowercase identifier | `"titanite"` |
| Display Name | Name shown to the player | `"Titanite"` |
| Rarity Tier | Common, Uncommon, or Rare | `Uncommon` |
| Rarity Weight | Spawn frequency (0 = never, 1 = always) | `0.2` |
| Base Yield Per Second | Units extracted per second | `8.0` |
| Hardness | Difficulty multiplier (higher = slower) | `2.0` |
| Volume Per Unit | Cargo space per unit | `0.2` |
| Beam Color | Mining laser color | Teal |
| Base Value | Market price per unit (future) | `30.0` |
| Base Processing Time Per Unit | Refining time in seconds (future) | `4.0` |
| Icon | Sprite for inventory UI (future) | (leave empty) |
| Description | Flavor text (future) | `"A dense metallic ore..."` |

5. The ore is ready. Add it to an Asteroid Field Definition to see it spawn.

## Creating a New Asteroid Field

1. In the Unity Project window, right-click in `Assets/Features/Procedural/Data/Fields/`.
2. Select **Create > VoidHarvest > Asteroid Field Definition**.
3. Name the asset (e.g., `RichLuminiteBelt`).
4. Configure spatial parameters:

| Field | What to set | Example |
|-------|-------------|---------|
| Field Name | Descriptive name | `"Rich Luminite Belt"` |
| Asteroid Count | Total asteroids | `500` |
| Field Radius | Sphere radius (meters) | `2500` |
| Asteroid Size Min | Smallest asteroid | `2.0` |
| Asteroid Size Max | Largest asteroid | `6.0` |
| Rotation Speed Min | Slowest spin (deg/s) | `0` |
| Rotation Speed Max | Fastest spin (deg/s) | `20` |
| Seed | RNG seed (same seed = same field) | `12345` |
| Min Scale Fraction | Smallest scale at full depletion | `0.3` |

5. Add ore entries to the **Ore Entries** array:

For each ore in the field:
- Set **Ore Definition** to an OreDefinition asset
- Set **Weight** (any positive number — auto-normalized)
- Assign **Mesh Variant A** and **Mesh Variant B** (asteroid meshes)
- Set **Tint Color** for visual distinction

**Weight example**: Luminite=8, Ferrox=1, Auralite=1 → 80% Luminite, 10% each others.

## Placing a Field in a Scene

1. Open the scene (or SubScene) where you want asteroids.
2. Create an empty GameObject.
3. Add the **AsteroidFieldSpawner** component.
4. Drag your `AsteroidFieldDefinition` asset into the **Field Definition** slot.
5. Enter Play mode — asteroids spawn automatically.

To have **multiple fields** in one scene, create multiple GameObjects each with their own AsteroidFieldSpawner referencing different AsteroidFieldDefinition assets.

## Testing Your Changes

After creating or modifying ore/field definitions:

1. Enter Play mode in the Editor.
2. Fly to the asteroid field and target an asteroid.
3. Verify:
   - Correct ore name appears in the HUD target panel.
   - Mining beam color matches the ore's BeamColor.
   - Yield rate feels appropriate for the hardness setting.
   - Asteroid visuals match the configured tint colors.
   - Cargo updates correctly with the right volume per unit.

## Shipped Ore Types

| Ore | Rarity | Yield | Hardness | Volume | Beam Color |
|-----|--------|-------|----------|--------|------------|
| Luminite | Common | 10/s | 1.0 | 0.1 m³ | Ice-blue |
| Ferrox | Uncommon | 7/s | 1.5 | 0.15 m³ | Bronze-orange |
| Auralite | Rare | 5/s | 2.5 | 0.25 m³ | Violet |

## FAQ

**Q: Do I need to modify any code to add a new ore?**
A: No. Create an OreDefinition asset and add it to an AsteroidFieldDefinition's Ore Entries array. Zero code changes required.

**Q: Can I have different visual styles for the same ore in different fields?**
A: Yes. Each OreFieldEntry in an AsteroidFieldDefinition has its own mesh variants and tint color, independent of the OreDefinition.

**Q: What happens if I set a weight to 0?**
A: That ore won't spawn in the field. Only entries with Weight > 0 are included in the distribution.

**Q: Can I change the seed and see a different asteroid layout?**
A: Yes. The same seed always produces the same layout. Different seeds produce different layouts.
