# Quickstart: VoidHarvest Master Vision & Architecture

**Date**: 2026-02-26
**Spec**: `specs/001-master-vision-architecture/spec.md`

---

## Prerequisites

- Unity 6 (6000.3.10f1) installed
- Git with LFS configured
- IDE: Rider 2024.x+ or Visual Studio 2022 17.x+ with Unity plugin

## 1. Clone & Open

```bash
git clone <repo-url>
cd SpaceMiningGame
git checkout 001-master-vision-architecture
```

Open in Unity Hub → Add → select project folder → Open with Unity 6000.3.10f1.

## 2. Install Packages

### Unity Registry packages

Add to `Packages/manifest.json` in the `"dependencies"` block:

```json
"com.unity.entities": "1.3.2",
"com.unity.entities.graphics": "1.3.2",
"com.unity.cinemachine": "3.1.2",
"com.unity.addressables": "2.3.1"
```

### OpenUPM packages

Add `"scopedRegistries"` block above `"dependencies"`:

```json
"scopedRegistries": [
  {
    "name": "OpenUPM",
    "url": "https://package.openupm.com",
    "scopes": [
      "jp.hadashikick",
      "com.cysharp",
      "com.github-glitchenzo"
    ]
  }
]
```

Add to `"dependencies"`:

```json
"jp.hadashikick.vcontainer": "1.16.7",
"com.cysharp.unitask": "2.5.10",
"com.github-glitchenzo.nugetforunity": "4.5.0"
```

**Note**: Verify latest versions via Unity Package Manager / OpenUPM before committing.

### NuGet packages (via NuGetForUnity)

After Unity resolves packages:
1. Unity Editor → NuGet → Manage NuGet Packages
2. Search `System.Collections.Immutable` → Install latest stable
3. Verify DLLs appear in `Assets/Packages/`

### IL2CPP safeguard

Create `Assets/link.xml`:

```xml
<linker>
  <assembly fullname="System.Collections.Immutable" preserve="all" />
  <assembly fullname="System.Memory" preserve="all" />
  <assembly fullname="System.Runtime.CompilerServices.Unsafe" preserve="all" />
</linker>
```

## 3. Create Project Structure

```
Assets/
├── Features/
│   ├── Camera/
│   │   ├── Data/
│   │   ├── Systems/
│   │   ├── Views/
│   │   └── Tests/
│   ├── Input/
│   │   ├── Data/
│   │   ├── Systems/
│   │   ├── Views/
│   │   └── Tests/
│   ├── Ship/
│   │   ├── Data/
│   │   ├── Systems/
│   │   ├── Views/
│   │   └── Tests/
│   ├── Mining/
│   │   ├── Data/
│   │   ├── Systems/
│   │   ├── Views/
│   │   └── Tests/
│   ├── Resources/
│   │   ├── Data/
│   │   ├── Systems/
│   │   ├── Views/
│   │   └── Tests/
│   ├── Procedural/
│   │   ├── Data/
│   │   ├── Systems/
│   │   ├── Views/
│   │   └── Tests/
│   └── HUD/
│       ├── Data/
│       ├── Systems/
│       ├── Views/
│       └── Tests/
├── Core/
│   ├── EventBus/
│   ├── State/
│   ├── Pools/
│   └── Extensions/
├── Settings/          (already exists)
└── Scenes/            (already exists)
```

## 4. Create Assembly Definitions

Each feature folder and Core subfolder needs a `.asmdef`:

| Assembly | Path | References |
|----------|------|------------|
| `VoidHarvest.Core.EventBus` | `Assets/Core/EventBus/` | UniTask |
| `VoidHarvest.Core.State` | `Assets/Core/State/` | System.Collections.Immutable |
| `VoidHarvest.Features.Camera.Data` | `Assets/Features/Camera/Data/` | Core.State |
| `VoidHarvest.Features.Camera.Systems` | `Assets/Features/Camera/Systems/` | Camera.Data, Core.State |
| `VoidHarvest.Features.Camera.Views` | `Assets/Features/Camera/Views/` | Camera.Data, Core.State, Unity.Cinemachine |
| `VoidHarvest.Features.Camera.Tests` | `Assets/Features/Camera/Tests/` | Camera.Data, Camera.Systems, nunit |
| ... | (same pattern per feature) | ... |

**Convention**: Assembly names follow `VoidHarvest.Features.<Feature>.<Layer>`.

## 5. Initial Scene Setup

1. Rename `Assets/Scenes/SampleScene.unity` → `Assets/Scenes/GameScene.unity`
2. Add GameObjects:
   - `GameManager` — attach `RootLifetimeScope` (VContainer)
   - `InputBridge` — attach `InputBridge` MonoBehaviour + `PlayerInput` component
   - `CameraRig` — attach `CinemachineCamera` + `CinemachineOrbitalFollow` + `CinemachineRotationComposer`
   - `Main Camera` — attach `Camera` + `CinemachineBrain` + `UniversalAdditionalCameraData`
   - `HUDCanvas` — UI Toolkit `UIDocument` for HUD
3. Create a SubScene for ECS entities (asteroids, ship entity)

## 6. Configure Input Actions

Replace `Assets/InputSystem_Actions.inputactions` with VoidHarvest-specific maps:

**Player map**:
| Action | Binding | Type |
|--------|---------|------|
| `Select` | Left Mouse Button | Button |
| `DoubleClickAlign` | Left Mouse Button (Multi-Tap x2) | Button |
| `RadialMenu` | Right Mouse Button | Button |
| `Thrust` | W/S (1D Axis) | Value (float) |
| `Strafe` | A/D (1D Axis) | Value (float) |
| `Roll` | Q/E (1D Axis) | Value (float) |
| `Hotbar1`-`Hotbar8` | 1-8 keys | Button |
| `MousePosition` | Mouse Position | Value (Vector2) |

**Camera map**:
| Action | Binding | Type |
|--------|---------|------|
| `Orbit` | Mouse Delta (when RMB held) | Value (Vector2) |
| `Zoom` | Scroll Wheel Y | Value (float) |
| `FreeLookToggle` | Middle Mouse Button | Button |

**UI map**: Keep Unity default (Navigate, Submit, Cancel, Point, Click).

## 7. URP Configuration for Space

1. Open `Assets/Settings/PC_RPAsset.asset`
2. Set HDR: On
3. Post-processing: Bloom (high threshold for star/beam glow), Color Grading (cool tones)
4. Open `Assets/Settings/DefaultVolumeProfile.asset` — configure space-appropriate post-processing
5. Skybox: solid black or procedural starfield material

## 8. Verify Setup

1. **Package resolution**: Unity Editor console should be error-free after package import
2. **Assembly definitions**: All `.asmdef` files compile without errors
3. **Input actions**: Action asset compiles; Player map shows all custom actions
4. **Cinemachine**: CameraRig orbits a placeholder cube with mouse drag
5. **ECS**: A simple test system (`ISystem` with `[BurstCompile]`) runs without errors
6. **Test runner**: Window → General → Test Runner → EditMode → all (0) tests pass (none written yet)

## First Implementation Target

After setup, the first implementation work follows TDD:

1. Write `CameraReducer` unit tests (orbit clamping, zoom bounds, free-look toggle)
2. Write `ShipStateReducer` unit tests (thrust, damping, speed clamp)
3. Write `InventoryReducer` unit tests (add, remove, capacity)
4. Implement reducers to pass tests
5. Wire up InputBridge → PilotCommand → Reducers → Views
