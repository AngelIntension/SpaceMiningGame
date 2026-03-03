# Quickstart: Data-Driven World Config

**Branch**: `009-data-driven-world-config` | **Date**: 2026-03-03

## Adding a New Station (Designer Workflow)

1. **Create a StationDefinition asset**: Right-click in Project window → Create → VoidHarvest → Station → Station Definition.
2. **Fill in fields**: Set a unique StationId, DisplayName, StationType, WorldPosition, AvailableServices, and assign a ServicesConfig reference.
3. **Add to WorldDefinition**: Open `Assets/Features/World/Data/DefaultWorld.asset`, expand the Stations array, and add your new StationDefinition.
4. **Enter Play mode**: The new station appears in the game world with the configured services.

## Tuning Docking Feel

1. **Open DockingConfig**: Navigate to `Assets/Features/Docking/Data/Configs/DockingConfig.asset`.
2. **Adjust values**: Modify SnapDuration, ApproachTimeout, AlignTimeout, or any other parameter.
3. **Enter Play mode**: The DockingSystem reads all values from the blob asset baked from this config.

## Tuning Camera Limits

1. **Open CameraConfig**: Navigate to `Assets/Features/Camera/Data/DefaultCameraConfig.asset`.
2. **Adjust values**: Modify pitch limits, distance limits, zoom ranges, cooldown, or sensitivity.
3. **Enter Play mode**: Camera behavior reflects the new limits.

## Tuning Input/Interaction Timing

1. **Open InteractionConfig**: Navigate to `Assets/Features/Input/Data/DefaultInteractionConfig.asset`.
2. **Adjust values**: Modify double-click window, drag threshold, or default distances.
3. **Enter Play mode**: Input and radial menu behavior reflects the new values.

## Validating Scene Setup

1. **Menu**: VoidHarvest → Validate Scene Config.
2. **Review**: Green items are properly configured. Yellow/red items need attention.
3. **Fix**: Assign missing SO references in the SceneLifetimeScope Inspector.

## Key Asset Locations

| Asset | Path |
|-------|------|
| Station Definitions | `Assets/Features/Station/Data/Definitions/` |
| Station Service Configs | `Assets/Features/Station/Data/ServiceConfigs/` |
| Station Presets | `Assets/Features/Station/Data/Presets/` |
| Raw Materials | `Assets/Features/Station/Data/RawMaterials/` |
| World Definition | `Assets/Features/World/Data/DefaultWorld.asset` |
| Camera Config | `Assets/Features/Camera/Data/DefaultCameraConfig.asset` |
| Interaction Config | `Assets/Features/Input/Data/DefaultInteractionConfig.asset` |
| Docking Config | `Assets/Features/Docking/Data/Configs/DockingConfig.asset` |
