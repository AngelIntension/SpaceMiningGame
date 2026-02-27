# Tasks: VoidHarvest Master Vision & Architecture

**Input**: Design documents from `specs/001-master-vision-architecture/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: TDD is mandatory per Constitution § Testing & Quality Standards. Tests are written FIRST, must FAIL, then implementation makes them pass.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- Unity project: `Assets/Features/<Feature>/{Data,Systems,Views,Tests}/`
- Core infrastructure: `Assets/Core/{EventBus,State,Pools,Extensions}/`
- All namespaces: `VoidHarvest.Features.<Feature>.<Layer>` or `VoidHarvest.Core.<Module>`

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Install packages, create folder structure, configure project settings

- [X] T001 Update Packages/manifest.json with scopedRegistries (OpenUPM for jp.hadashikick, com.cysharp, com.github-glitchenzo) and dependencies (com.unity.entities 1.3.x, com.unity.entities.graphics 1.3.x, com.unity.cinemachine 3.1.x, com.unity.addressables 2.3.x, jp.hadashikick.vcontainer 1.16.x, com.cysharp.unitask 2.5.x, com.github-glitchenzo.nugetforunity 4.5.x) per specs/001-master-vision-architecture/research.md § R8
- [X] T002 Install System.Collections.Immutable via NuGetForUnity UI in Unity Editor (verify DLLs in Assets/Packages/)
- [X] T003 Create Assets/link.xml with preserve directives for System.Collections.Immutable, System.Memory, System.Runtime.CompilerServices.Unsafe
- [X] T004 [P] Create feature folder structure: Assets/Features/{Camera,Input,Ship,Mining,Resources,Procedural,HUD}/{Data,Systems,Views,Tests}/ and Assets/Core/{EventBus,State,Pools,Extensions}/ per plan.md § Project Structure
- [X] T005 [P] Create assembly definition files (.asmdef) per Constitution § V Modularity — one .asmdef per feature root (e.g., VoidHarvest.Features.Camera.asmdef, VoidHarvest.Features.Ship.asmdef, VoidHarvest.Features.Mining.asmdef, etc.) and one per Core module (VoidHarvest.Core.EventBus.asmdef, VoidHarvest.Core.State.asmdef, VoidHarvest.Core.Extensions.asmdef). Separate test asmdefs per feature (VoidHarvest.Features.Camera.Tests.asmdef, etc.) referencing Unity Test Framework. Explicit dependency declarations between feature asmdefs and Core asmdefs.
- [X] T006 [P] Replace Assets/InputSystem_Actions.inputactions with VoidHarvest-specific action maps: Player (Select, DoubleClickAlign, RadialMenu, Thrust, Strafe, Roll, Hotbar1-8, MousePosition), Camera (Orbit, Zoom, FreeLookToggle), and UI (Navigate, Submit, Cancel — preserve defaults from Unity template) per spec.md § 3 Input Actions tables and spec.md § 11 Implementation Notes
- [X] T007 Rename Assets/Scenes/SampleScene.unity to Assets/Scenes/GameScene.unity and update Build Settings
- [X] T008 [P] Configure URP for space: dark skybox, bloom post-processing in Assets/Settings/DefaultVolumeProfile.asset, set camera near=0.1 far=10000 in Assets/Settings/PC_RPAsset.asset
- [X] T008b [P] Configure Roslyn analyzers for immutability enforcement per Constitution § Testing & Quality Standards. Install Microsoft.CodeAnalysis.NetAnalyzers via NuGetForUnity. Add .editorconfig rules: `dotnet_diagnostic.CA1051.severity = error` (no public fields on non-SO types), plus custom .editorconfig entries flagging mutable fields on types with `State`, `Data`, `Command`, `Action` suffixes. If Unity-compatible Roslyn hosting is insufficient, create a standalone `dotnet build` validation script as CI fallback. Verify analyzers run in Unity Editor console and CI. Document any analyzer limitations in quickstart.md.

**Checkpoint**: Unity Editor opens error-free, all packages resolved, folder structure in place, Roslyn analyzers active

---

## Phase 2: Foundational (Core Infrastructure)

**Purpose**: Shared infrastructure that MUST be complete before ANY user story can be implemented

**CRITICAL**: No user story work can begin until this phase is complete

> **XML Doc Comments (MANDATORY PER-TASK)**: Every implementation task in this and subsequent phases that creates or modifies a public API (interfaces, reducer Reduce methods, action/state record types, public static functions) MUST include XML doc comments referencing the originating acceptance criterion (e.g., `/// <summary>See MVP-01: 6DOF Newtonian flight.</summary>`). This is a per-task responsibility, not deferred work. T085 (Phase 8) audits completeness and fills any gaps, but implementers should not rely on T085 as the primary mechanism.

### Tests for Foundational Infrastructure

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T009 [P] Write unit tests for Option<T> helper (Some, None, Map, Match, default equality) in Assets/Core/Extensions/Tests/OptionTests.cs
- [X] T010 [P] Write unit tests for UniTaskEventBus (publish struct event, subscribe receives it, multiple subscribers, cancellation stops subscription) in Assets/Core/EventBus/Tests/UniTaskEventBusTests.cs
- [X] T011 [P] Write unit tests for StateStore (dispatch action produces new state, version increments, OnStateChanged fires, Current returns latest state) in Assets/Core/State/Tests/StateStoreTests.cs
- [X] T012 [P] Write unit tests for GameStateReducer (operates on GameState root — delegates ICameraAction to CameraReducer via state.Camera, delegates IShipAction to ShipStateReducer via state.ActiveShipPhysics, delegates IMiningAction to MiningReducer via state.Loop.Mining, delegates IInventoryAction to InventoryReducer via state.Loop.Inventory, unknown action returns unchanged state) in Assets/Core/State/Tests/GameStateReducerTests.cs

### Implementation for Foundational Infrastructure

- [X] T013 [P] Implement Option<T> readonly struct (Some/None, Map, Match, implicit conversions) in Assets/Core/Extensions/Option.cs — C# 9.0 readonly struct, not record struct
- [X] T014 [P] Implement IEventBus interface and UniTaskEventBus using Channel<T> per contracts/eventbus-interface.md in Assets/Core/EventBus/IEventBus.cs and Assets/Core/EventBus/UniTaskEventBus.cs
- [X] T014b [P] Implement event struct types per data-model.md § Event Types — one type per file per Constitution naming convention: MiningStartedEvent in Assets/Core/EventBus/Events/MiningStartedEvent.cs, MiningYieldEvent in Assets/Core/EventBus/Events/MiningYieldEvent.cs, MiningStoppedEvent in Assets/Core/EventBus/Events/MiningStoppedEvent.cs, StopReason enum in Assets/Core/EventBus/Events/StopReason.cs, TargetSelectedEvent in Assets/Core/EventBus/Events/TargetSelectedEvent.cs, StateChangedEvent<T> in Assets/Core/EventBus/Events/StateChangedEvent.cs
- [X] T015 [P] Implement IGameAction interface hierarchy per data-model.md § Action Types — one type per file per Constitution naming convention: IGameAction in Assets/Core/State/IGameAction.cs; ICameraAction in Assets/Core/State/ICameraAction.cs; IShipAction in Assets/Core/State/IShipAction.cs; IMiningAction in Assets/Core/State/IMiningAction.cs; IInventoryAction in Assets/Core/State/IInventoryAction.cs; IFleetAction in Assets/Core/State/IFleetAction.cs; ITechAction in Assets/Core/State/ITechAction.cs; IMarketAction in Assets/Core/State/IMarketAction.cs; IBaseAction in Assets/Core/State/IBaseAction.cs; SyncShipPhysicsAction in Assets/Core/State/SyncShipPhysicsAction.cs
- [X] T016 Implement IStateStore interface and StateStore class (dispatch, version counter, OnStateChanged callback notification, and publishes StateChangedEvent<GameState> via IEventBus after each dispatch that produces a new state reference) per contracts/state-store-interface.md in Assets/Core/State/IStateStore.cs and Assets/Core/State/StateStore.cs
- [X] T017 Implement GameStateReducer (root compositor operating on GameState — routes ICameraAction to state.Camera, IShipAction to state.ActiveShipPhysics, IMiningAction/IInventoryAction to state.Loop.* via pattern matching) per contracts/reducer-interfaces.md in Assets/Core/State/GameStateReducer.cs. **Includes pass-through stub reducers** for ALL sub-reducers referenced in the routing switch: CameraReducer, ShipStateReducer, MiningReducer, InventoryReducer (replaced by real implementations in Phases 3-5: T030, T031, T054, T055), **plus** FleetReducer, TechTreeReducer, MarketReducer, BaseReducer (remain as stubs through MVP — Phase 1-3 systems). Each stub returns unchanged state (`state => state`). Stubs are co-located in Assets/Core/State/GameStateReducer.cs (or separate files per naming convention).
- [X] T018 Implement all root and stub state records per data-model.md § Root State — one type per file per Constitution naming convention: GameState in Assets/Core/State/GameState.cs, GameLoopState in Assets/Core/State/GameLoopState.cs, WorldState in Assets/Core/State/WorldState.cs, ExploreState in Assets/Core/State/ExploreState.cs (stub with Empty default — ScannerActive is non-functional in MVP), StationData in Assets/Core/State/StationData.cs, plus stub records with Empty defaults required by GameLoopState in their respective feature Data/ folders — RefiningState in Assets/Features/Resources/Data/RefiningState.cs, TechTreeState in Assets/Features/TechTree/Data/TechTreeState.cs, FleetState in Assets/Features/Fleet/Data/FleetState.cs, BaseState in Assets/Features/Base/Data/BaseState.cs, MarketState in Assets/Features/Economy/Data/MarketState.cs. Minimal stub MiningSessionState in Assets/Features/Mining/Data/MiningState.cs and InventoryState in Assets/Features/Resources/Data/InventoryState.cs (replaced by full implementations in T052/T053). All record definitions match data-model.md exactly (including ShipState with HullIntegrity).
- [X] T019 Create ScriptableObject definitions: OreTypeDefinition in Assets/Features/Mining/Data/OreTypeDefinition.cs and ShipArchetypeConfig in Assets/Features/Ship/Data/ShipArchetypeConfig.cs per data-model.md § ScriptableObject Definitions
- [X] T020 [P] Create OreTypeDefinition ScriptableObject assets for MVP ores: Veldspar (common, Rarity=0.6, Hardness=1.0, BeamColor=tan, VolumePerUnit=0.1), Scordite (uncommon, Rarity=0.3, Hardness=1.5, BeamColor=amber, VolumePerUnit=0.15), Pyroxeres (rare, Rarity=0.1, Hardness=2.5, BeamColor=crimson, VolumePerUnit=0.25) in Assets/Features/Mining/Data/Resources/
- [X] T021 [P] Create ShipArchetypeConfig ScriptableObject asset for MVP starter ship (MiningBarge role, default stats, MiningPower=1.0f) in Assets/Features/Ship/Data/Resources/
- [X] T022 Create VContainer RootLifetimeScope registering IEventBus (Singleton), IStateStore (Singleton), GameStateReducer (Singleton) in Assets/Core/RootLifetimeScope.cs — attach to GameManager GameObject in GameScene
- [X] T023 Create VContainer SceneLifetimeScope (child of Root) as placeholder for scene-specific view bindings in Assets/Core/SceneLifetimeScope.cs

**Checkpoint**: Foundation ready — Option<T>, EventBus, StateStore, GameStateReducer all passing tests. VContainer wiring functional. User story implementation can now begin.

---

## Phase 3: User Story 1 — First Flight (Priority: P1) MVP

**Goal**: Player spawns in a ship and can fly with 6DOF Newtonian physics. Camera orbits ship smoothly with speed-based zoom. Keyboard thrust (WASD/QE) produces expected velocity. Ship drifts with inertia.

**Independent Test**: Launch GameScene, verify ship responds to all 6DOF inputs, camera follows and orbits correctly, speed-based zoom works, free-look does not affect heading.

**Acceptance Criteria**: MVP-01 (6DOF flight), MVP-02 (camera orbit + zoom)

### Tests for US1 (TDD — Write FIRST, Ensure FAIL)

- [X] T024 [P] [US1] Write CameraReducer unit tests: OrbitAction adds deltas with pitch clamped [-80,80]; ZoomAction adjusts TargetDistance clamped [5,50]; SpeedZoomAction lerps distance from normalized speed; ToggleFreeLookAction toggles and resets offsets; FreeLookAction only applies when active — in Assets/Features/Camera/Tests/CameraReducerTests.cs. **Also write CameraView zoom cooldown PlayMode test** in Assets/Features/Camera/Tests/CameraViewZoomCooldownTests.cs: verify SpeedZoomAction is suppressed for 2.0s after a manual ZoomAction; verify SpeedZoomAction resumes after cooldown expires; verify rapid manual zooms reset the cooldown timer.
- [X] T025 [P] [US1] Write ShipPhysicsMath pure function unit tests: ComputeThrust with forward input produces force along local Z; ApplyForce increases velocity proportional to force/mass; ApplyDamping reduces velocity over time; ClampSpeed enforces max speed; zero mass guard returns unchanged velocity; NaN velocity guard clamps to zero; DetermineFlightMode resolves manual override over auto-pilot; also write ShipStateReducer tests: SyncShipPhysicsAction copies all fields correctly; unknown action returns unchanged state — in Assets/Features/Ship/Tests/ShipPhysicsMathTests.cs and Assets/Features/Ship/Tests/ShipStateReducerTests.cs
- [X] T026 [P] [US1] Write PilotCommand construction tests: verify immutability of record; all fields correctly populated from mock input; default empty command has no target/align/thrust — in Assets/Features/Input/Tests/PilotCommandTests.cs

### Implementation for US1

- [X] T027 [P] [US1] Implement CameraState sealed record and ICameraAction types (OrbitAction, ZoomAction, SpeedZoomAction, ToggleFreeLookAction, FreeLookAction) in Assets/Features/Camera/Data/CameraState.cs and Assets/Features/Camera/Data/CameraActions.cs per data-model.md § Camera
- [X] T028 [P] [US1] Implement ShipState sealed record (with HullIntegrity field, float3/quaternion types), ShipFlightMode enum, IShipAction types (including SyncShipPhysicsAction) in Assets/Features/Ship/Data/ShipState.cs and Assets/Features/Ship/Data/ShipActions.cs per data-model.md § Ship
- [X] T029 [P] [US1] Implement PilotCommand sealed record, ThrustInput readonly struct, RadialMenuAction enum, RadialMenuChoice readonly struct in Assets/Features/Input/Data/PilotCommand.cs per data-model.md § Input Commands
- [X] T030 [US1] Implement CameraReducer (pure static class) with all ICameraAction handling per contracts/reducer-interfaces.md § CameraReducer in Assets/Features/Camera/Systems/CameraReducer.cs
- [X] T031 [US1] Implement ShipPhysicsMath (pure static class, all methods operate on unmanaged types for Burst compatibility) with DetermineFlightMode, ComputeThrust, ComputeTorque, ApplyForce, ApplyDamping, ClampSpeed, IntegrateRotation — in Assets/Features/Ship/Systems/ShipPhysicsMath.cs per contracts/reducer-interfaces.md § ShipPhysicsMath. Also implement ShipStateReducer (pure static class) handling only SyncShipPhysicsAction → direct field copy — in Assets/Features/Ship/Systems/ShipStateReducer.cs per contracts/reducer-interfaces.md § ShipStateReducer
- [X] T032 [P] [US1] Create Ship ECS components: ShipPositionComponent, ShipVelocityComponent, ShipConfigComponent, ShipFlightModeComponent, PilotCommandComponent, PlayerControlledTag in Assets/Features/Ship/Data/ShipComponents.cs per data-model.md § Ship ECS Components
- [X] T033 [US1] Implement ShipPhysicsSystem (ISystem, [BurstCompile]) in SimulationSystemGroup — reads PilotCommandComponent + ShipConfigComponent, calls ShipPhysicsMath static functions directly on unmanaged component data (no managed ShipState), writes position/velocity/rotation back to ECS components — in Assets/Features/Ship/Systems/ShipPhysicsSystem.cs
- [X] T034 [US1] Implement StoreToEcsSyncSystem (ISystem, OrderFirst in SimulationSystemGroup) as **empty placeholder** — contains only the ISystem boilerplate (OnCreate/OnUpdate/OnDestroy stubs) and `[UpdateBefore(typeof(ShipPhysicsSystem))]` ordering attribute. OnUpdate is a no-op in MVP (returns immediately). Establishes correct system ordering so Phase 1+ fleet ship swap can inject store→ECS sync logic without reordering. — in Assets/Core/State/StoreToEcsSyncSystem.cs per research.md § R7
- [X] T035 [US1] Implement EcsToStoreSyncSystem (ISystem, OrderLast in SimulationSystemGroup) — projects ship physics (position, velocity) back into StateStore for HUD display in Assets/Core/State/EcsToStoreSyncSystem.cs per research.md § R7
- [X] T036 [US1] Implement InputBridge MonoBehaviour — reads Unity Input System callbacks for Thrust/Strafe/Roll, constructs PilotCommand record each tick, writes PilotCommand fields to ECS PilotCommandComponent singleton via EntityManager (not dispatched through StateStore — see data-model.md § Ship Actions pipeline note) in Assets/Features/Input/Views/InputBridge.cs (keyboard-only for US1; mouse targeting added in US2). **Also dispatches Camera input actions**: OrbitAction (from Camera/Orbit mouse delta), ZoomAction (from Camera/Zoom scroll wheel), ToggleFreeLookAction (from Camera/FreeLookToggle MMB), and FreeLookAction (from Camera/Orbit when free-look is active) — these are dispatched through StateStore as ICameraAction.
- [X] T037 [US1] Implement CameraView MonoBehaviour — reads CameraState from StateStore each LateUpdate, drives CinemachineOrbitalFollow (HorizontalAxis.Value, VerticalAxis.Value, Radius) via Mathf.SmoothDamp toward TargetDistance, and dispatches SpeedZoomAction based on ship velocity (NormalizedSpeed = velocity.magnitude / maxSpeed). **Zoom cooldown**: CameraView MUST skip SpeedZoomAction dispatch for a **2.0 second cooldown** after a manual ZoomAction is detected (InputBridge dispatches ZoomAction in Update; CameraView dispatches SpeedZoomAction in LateUpdate — manual zoom is immediately overridden by speed-zoom next frame without cooldown). Track last manual zoom timestamp; suppress SpeedZoomAction while `Time.time - lastManualZoomTime < 2.0f`. — in Assets/Features/Camera/Views/CameraView.cs per research.md § R6 and spec.md § 3 Camera System zoom priority
- [X] T038 [US1] Implement ShipView MonoBehaviour — reads ShipState from StateStore, applies Position/Rotation to Transform in Assets/Features/Ship/Views/ShipView.cs
- [X] T039 [US1] Setup GameScene: add CameraRig (CinemachineCamera + CinemachineOrbitalFollow Sphere mode + CinemachineRotationComposer), Main Camera (CinemachineBrain), Ship GameObject with placeholder mesh, GameManager with RootLifetimeScope, InputBridge, register views in SceneLifetimeScope
- [X] T040 [US1] Create SubScene for ECS entities — create ShipAuthoring MonoBehaviour (authoring component with fields matching ShipConfigComponent defaults, including MiningPower from ShipArchetypeConfig) and ShipBaker : Baker<ShipAuthoring> that adds ShipPositionComponent, ShipVelocityComponent, ShipConfigComponent (with MiningPower), ShipFlightModeComponent, PilotCommandComponent, PlayerControlledTag, and MiningBeamComponent (Active=false, zeroed fields — present on ship entity from bake so MiningBeamSystem can query it; activated at runtime by InputBridge in T062) during baking. Place ShipAuthoring on an entity in the SubScene. **Note**: The ship ECS entity is **simulation-only** (no RenderMesh components, not rendered by Entities Graphics). The visual ship is the separate GameObject with ShipView (T038) which reads ShipState from StateStore and applies to its own Transform. This dual representation (ECS for physics, GO for rendering) is the intended hybrid pattern.
- [X] T041 [US1] Wire full US1 pipeline (two paths): Ship physics: InputBridge → PilotCommand → ECS PilotCommandComponent (via EntityManager) → ShipPhysicsSystem (Burst, calls ShipPhysicsMath) → EcsToStoreSyncSystem → SyncShipPhysicsAction → ShipStateReducer → ShipView. Camera: InputBridge → ICameraAction → StateStore → CameraReducer → CameraState → CameraView. Verify in Play Mode that WASD/QE produces 6DOF flight with Newtonian inertia and camera orbits.

**Checkpoint**: US1 complete — Ship flies with 6DOF physics, camera orbits with speed-based zoom, free-look works without affecting heading. All CameraReducer, ShipPhysicsMath, and ShipStateReducer tests pass green.

---

## Phase 4: User Story 2 — Target & Approach (Priority: P2)

**Goal**: Player can left-click an asteroid to select it (highlighted), double-click to align and fly toward it, and right-click for a radial context menu with Approach/Orbit/Mine/Keep-at-Range options.

**Independent Test**: Place placeholder asteroid cubes in scene. Click asteroid → highlight + HUD target info. Double-click → ship rotates and flies toward it. Right-click → radial menu opens.

**Acceptance Criteria**: MVP-03 (select + align), MVP-04 (radial menu)

### Tests for US2 (TDD — Write FIRST, Ensure FAIL)

- [X] T042 [P] [US2] Write ShipPhysicsMath flight mode tests: DetermineFlightMode transitions to AlignToPoint on align input; ComputeThrust produces rotation toward target in AlignToPoint mode; Approach mode applies forward thrust when aligned within tolerance; Orbit mode maintains player-specified distance via lateral thrust; KeepAtRange maintains distance without orbiting (forward/reverse only) — in Assets/Features/Ship/Tests/ShipFlightModeTests.cs
- [X] T043 [P] [US2] Write target selection tests: left-click raycast populates PilotCommand.SelectedTarget; double-click populates PilotCommand.AlignPoint with 3D world position; right-click sets RadialChoice; rapid target switching stress test — dispatch 100 target selections in sequence, verify final PilotCommand.SelectedTarget matches last selection — in Assets/Features/Input/Tests/TargetSelectionTests.cs

### Implementation for US2

- [X] T044 [US2] Extend InputBridge with mouse-based target selection: left-click raycast (Physics.Raycast from camera through mouse position, filtered by "Selectable" layer — asteroids only in MVP), double-click detection (Multi-Tap interaction), right-click context menu trigger — in Assets/Features/Input/Views/InputBridge.cs. Configure "Selectable" layer in Project Settings > Tags and Layers; assign to asteroid GameObjects/entities.
- [X] T045 [US2] Implement AlignToPoint, Approach, Orbit, KeepAtRange flight mode behaviors in ShipPhysicsMath (pure static functions on unmanaged types) and update ShipPhysicsSystem to call them — auto-rotate toward target then thrust; maintain distance for Orbit/KeepAtRange using player-specified distance from PilotCommandComponent.RadialDistance — in Assets/Features/Ship/Systems/ShipPhysicsMath.cs and Assets/Features/Ship/Systems/ShipPhysicsSystem.cs
- [X] T046 [US2] Create radial context menu UI using UI Toolkit — UXML layout with pie segments, USS styling, distance sub-menu per spec.md § 3 Radial Context Menu: range 10m–500m with 10m step increments, preset buttons at 25m/50m/100m/250m/500m, continuous slider, defaults 50m (Approach), 100m (Orbit), 50m (KeepAtRange) — in Assets/Features/HUD/Views/RadialMenu/ (RadialMenu.uxml, RadialMenu.uss, RadialMenuController.cs)
- [X] T047 [US2] Implement target highlight visualization — selected asteroid gets inverted-hull outline effect (URP Renderer Feature, 2px white outline, rendered via second pass with front-face culling and vertex extrusion); target info panel on HUD (UI Toolkit) shows asteroid name, ore type, distance, remaining mass percentage — in Assets/Features/HUD/Views/TargetInfoPanel.cs and Assets/Features/HUD/Views/SelectionOutlineFeature.cs
- [X] T048 [US2] Wire US2 pipeline: click → raycast → PilotCommand.SelectedTarget → highlight + TargetSelectedEvent; double-click → PilotCommand.AlignPoint → ECS PilotCommandComponent → ShipPhysicsMath.DetermineFlightMode(AlignToPoint) → ShipPhysicsSystem; right-click → RadialMenu → RadialMenuChoice → ECS PilotCommandComponent → ShipPhysicsMath.DetermineFlightMode(Approach/Orbit/KeepAtRange) → ShipPhysicsSystem. Verify in Play Mode.

**Checkpoint**: US2 complete — Clicking selects asteroids with visual feedback; double-click aligns ship; radial menu opens with distance sub-menu. All flight mode tests pass green.

---

## Phase 5: User Story 3 — Mine & Collect (Priority: P3)

**Goal**: Player selects "Mine" from radial menu or activates mining laser via hotbar. Beam connects to asteroid, yield numbers stream, resources accumulate in inventory. Asteroid visually depletes.

**Independent Test**: Approach asteroid, activate mining, verify resources appear in inventory within 5 seconds, verify asteroid depletion visual.

**Acceptance Criteria**: MVP-05 (beam + yield <500ms), MVP-06 (inventory correct), MVP-07 (depletion visual)

### Tests for US3 (TDD — Write FIRST, Ensure FAIL)

- [X] T049 [P] [US3] Write MiningReducer unit tests: BeginMiningAction sets target and resets accumulators; MiningTickAction computes yield via CalculateYield formula; StopMiningAction resets to Empty; out-of-range stops mining; mining with inventory full still processes yield (cargo-full guard tested at inventory layer) — in Assets/Features/Mining/Tests/MiningReducerTests.cs. **Note**: Depleted-asteroid (RemainingMass=0) behavior is an ECS-layer concern — tested in T074 integration test (MiningBeamSystem writes NativeAsteroidDepletedAction when mass reaches 0).
- [X] T050 [P] [US3] Write CalculateYield pure function tests: hardness reduces yield; depth reduces yield; miningPower scales linearly; zero hardness guard; deltaTime scales output — in Assets/Features/Mining/Tests/MiningYieldTests.cs
- [X] T051 [P] [US3] Write InventoryReducer unit tests: AddResource increases quantity and volume; AddResource rejects when volume exceeds MaxVolume (cargo full — returns unchanged state); AddResource rejects when Stacks.Count would exceed MaxSlots; RemoveResource decreases quantity; RemoveResource rejects insufficient stock; remove last unit of stack clears entry from dictionary — in Assets/Features/Resources/Tests/InventoryReducerTests.cs

### Implementation for US3

- [X] T052 [P] [US3] Implement MiningSessionState sealed record, MiningYieldResult sealed record, IMiningAction types (BeginMiningAction, MiningTickAction, StopMiningAction) in Assets/Features/Mining/Data/MiningState.cs and Assets/Features/Mining/Data/MiningActions.cs per data-model.md § Mining
- [X] T053 [P] [US3] Implement InventoryState sealed record, ResourceStack readonly struct, IInventoryAction types (AddResourceAction, RemoveResourceAction) in Assets/Features/Resources/Data/InventoryState.cs and Assets/Features/Resources/Data/InventoryActions.cs per data-model.md § Inventory
- [X] T054 [US3] Implement MiningReducer (pure static) with Reduce and CalculateYield methods per contracts/reducer-interfaces.md § MiningReducer in Assets/Features/Mining/Systems/MiningReducer.cs
- [X] T055 [US3] Implement InventoryReducer (pure static) with AddResource and RemoveResource per contracts/reducer-interfaces.md § InventoryReducer in Assets/Features/Resources/Systems/InventoryReducer.cs
- [X] T056 [P] [US3] Create Mining ECS components: MiningBeamComponent (lives on the **ship entity** — baked in T040 with Active=false; activated at runtime by InputBridge), AsteroidComponent (with InitialMass for depletion ratio), AsteroidOreComponent in Assets/Features/Mining/Data/MiningComponents.cs per data-model.md § Mining ECS components
- [X] T057 [P] [US3] Create OreTypeBlob and OreTypeBlobDatabase BlobAsset structs + OreTypeDatabaseComponent singleton in Assets/Features/Mining/Data/OreTypeBlob.cs for Burst-accessible ore data per data-model.md § BlobAsset Equivalents
- [X] T057b [US3] Implement OreTypeBlobBakingSystem (managed SystemBase or authoring+baker pattern) that converts OreTypeDefinition ScriptableObject[] into OreTypeBlobDatabase BlobAsset and creates singleton entity with OreTypeDatabaseComponent. OreTypeDefinition SOs injected via VContainer or serialized on an authoring component. Baking order: index in BlobArray<OreTypeBlob> matches the order SOs are provided (this defines the OreTypeId ↔ OreId mapping). Also maintains a managed string[] OreId lookup table for MiningActionDispatchSystem to convert OreTypeId back to string OreId. — in Assets/Features/Mining/Systems/OreTypeBlobBakingSystem.cs. Must complete before T058 (MiningBeamSystem) can read ore data.
- [X] T058 [US3] Implement MiningBeamSystem (ISystem, [BurstCompile]) — each tick: checks distance between ship (PlayerControlledTag entity position) and target asteroid; if distance > MiningBeamComponent.MaxRange, sets MiningBeamComponent.Active = false and writes NativeMiningStopAction(Reason=OutOfRange) to NativeQueue; if in range and active, computes yield per tick using BlobAsset ore data (OreTypeBlobDatabase), subtracts yield from AsteroidComponent.RemainingMass, writes NativeMiningYieldAction to NativeQueue; if RemainingMass ≤ 0 after subtraction, writes NativeAsteroidDepletedAction to NativeQueue — in Assets/Features/Mining/Systems/MiningBeamSystem.cs
- [X] T059 [US3] Implement NativeQueue mining action buffer (MiningActionBufferSingleton tag + NativeQueue<NativeMiningYieldAction> owned by MiningBeamSystem) and MiningActionDispatchSystem (managed SystemBase in PresentationSystemGroup) that drains mining yields from the queue. **Yield accumulation flow**: For each NativeMiningYieldAction drained: (1) dispatch MiningTickAction to StateStore — MiningReducer.ComputeMiningTick adds the float yield amount to YieldAccumulator and advances MiningDuration; (2) read back the new MiningSessionState from StateStore; (3) compute whole units from yield by calling MiningReducer.CalculateYield (or reading accumulated whole units from the updated state) — **only dispatch AddResourceAction to InventoryReducer when WholeUnitsYielded > 0** (NativeMiningYieldAction.Amount is float; AddResourceAction.Quantity is int; fractional amounts accumulate across ticks in MiningSessionState.YieldAccumulator until they reach whole units); (4) if AddResourceAction dispatched, publish MiningYieldEvent via IEventBus with the integer quantity yielded. Also drains NativeMiningStopAction for out-of-range and NativeAsteroidDepletedAction for depleted asteroids. Detects cargo-full rejection (unchanged state after AddResource) and dispatches StopMiningAction + publishes MiningStoppedEvent(StopReason.CargoFull). Detects asteroid depletion and dispatches StopMiningAction + publishes MiningStoppedEvent(StopReason.AsteroidDepleted). Detects out-of-range stop and dispatches StopMiningAction + publishes MiningStoppedEvent(StopReason.OutOfRange). — in Assets/Features/Mining/Systems/MiningActionDispatchSystem.cs per research.md § R7 and data-model.md § NativeQueue Action Structs.
- [X] T060 [US3] Implement MiningBeamView MonoBehaviour — LineRenderer or particle system connecting ship to asteroid, activate/deactivate based on MiningSessionState. Beam color: reads ActiveOreId from MiningSessionState, looks up OreTypeDefinition SO from a VContainer-injected OreTypeDefinition[] registry (same SO list used by T057b baking), extracts BeamColor. This is view-layer only — no BlobAsset access needed. — in Assets/Features/Mining/Views/MiningBeamView.cs
- [X] T061 [US3] Implement asteroid depletion shader — URP Shader Graph with _Depletion float parameter (0=pristine, 1=fully depleted) driving: (a) surface darkening via color lerp from base albedo to charcoal gray, (b) alpha erosion using noise mask (progressive transparency from edges inward), (c) emission pulse on depletion milestones (25%, 50%, 75%). No mesh deformation in MVP. AsteroidDepletionSystem (ISystem) updates _Depletion as `1 - (RemainingMass / InitialMass)` from AsteroidComponent — in Assets/Features/Mining/Systems/AsteroidDepletionSystem.cs and Assets/Features/Procedural/Views/AsteroidDepletion.shadergraph
- [X] T062 [US3] Implement hotbar module activation — number keys 1-8 in InputBridge dispatch ActivatedModules in PilotCommand; mining laser activation triggers **both**: (1) `BeginMiningAction` dispatched to StateStore (updates MiningSessionState via MiningReducer), and (2) direct write of `MiningBeamComponent` on the ship entity via `EntityManager` (sets Active=true, TargetAsteroid=selected entity, MiningPower from ShipConfigComponent, MaxRange from ShipArchetypeConfig). This dual-write mirrors the InputBridge→PilotCommandComponent pattern (T036) where managed input bridges directly to ECS without StoreToEcsSyncSystem. StopMining likewise: sets MiningBeamComponent.Active=false via EntityManager and dispatches StopMiningAction to StateStore. — in Assets/Features/Input/Views/InputBridge.cs (extend existing)
- [X] T062b [US3] Implement cargo-full HUD warning — HUDView subscribes to MiningStoppedEvent via EventBus; when StopReason is CargoFull, display "Cargo Full" warning indicator (UI Toolkit label, red text, auto-dismiss after 3 seconds) in Assets/Features/HUD/Views/HUDView.cs (extend existing). Also handle OutOfRange and AsteroidDepleted stop reasons with appropriate warning text.
- [X] T063 [US3] Wire US3 pipeline (two parallel paths, same as US1 ship pattern): **Store path**: radial Mine or hotbar → InputBridge → BeginMiningAction → MiningReducer → MiningSessionState (tracks target, ore, duration, yield accumulation). **ECS path**: InputBridge → EntityManager writes MiningBeamComponent (Active=true, TargetAsteroid, MiningPower) on ship entity → MiningBeamSystem (Burst, per-tick yield/range/depletion) → NativeQueue → MiningActionDispatchSystem → MiningTickAction/AddResourceAction → StateStore → InventoryReducer → HUD. StoreToEcsSyncSystem remains a no-op in MVP — InputBridge is the sole Store→ECS bridge for mining initiation (Phase 1+ will move this to StoreToEcsSyncSystem for fleet ship swap support). Add MiningStartedEvent/MiningYieldEvent/MiningStoppedEvent to EventBus for view notifications. Verify in Play Mode: beam connects <500ms, yield appears in inventory, depletion visual progresses.

**Checkpoint**: US3 complete — Mining beam connects, yield accumulates in inventory, asteroid depletes visually. All MiningReducer, CalculateYield, and InventoryReducer tests pass green.

---

## Phase 6: User Story 4 — Procedural Field & HUD (Priority: P4)

**Goal**: Player enters a procedurally generated asteroid field with varied ore compositions (<500 asteroids at 60 FPS). HUD displays resource counts, velocity, hull integrity, mining target info in real-time.

**Independent Test**: Load scene — field generates with 3+ ore types. Same seed produces identical fields. HUD updates within 1 frame of state change. Steady-state 60 FPS.

**Acceptance Criteria**: MVP-08 (field gen <100ms, 60 FPS), MVP-09 (HUD updates), MVP-10 (reducer coverage), MVP-11 (zero GC)

### Tests for US4 (TDD — Write FIRST, Ensure FAIL)

- [X] T064 [P] [US4] Write AsteroidFieldGeneratorJob determinism tests: same seed produces identical positions and ore assignments; respects MaxAsteroids cap; all positions within FieldRadius; ore distribution weights match within statistical tolerance; zero-radius asteroid guard produces valid entity; zero-mass asteroid guard prevents division-by-zero in depletion calculations (AsteroidComponent.Depletion stays 0 when RemainingMass is 0) — in Assets/Features/Procedural/Tests/AsteroidFieldGeneratorTests.cs
- [X] T065 [P] [US4] Write AsteroidFieldGeneratorJob performance test: <500 asteroids generated in <100ms via Burst — in Assets/Features/Procedural/Tests/AsteroidFieldPerfTests.cs

### Implementation for US4

- [X] T066 [P] [US4] Implement AsteroidFieldConfig sealed record, AsteroidData sealed record, OreDistribution readonly struct in Assets/Features/Procedural/Data/AsteroidFieldConfig.cs per data-model.md § Asteroid Field Generation
- [X] T067 [US4] Implement AsteroidFieldGeneratorJob (IJobParallelFor, [BurstCompile]) — Poisson disc sampling with Unity.Mathematics.Random seeded RNG for positions; second pass for ore assignment with weighted distribution and noise-based clustering — in Assets/Features/Procedural/Systems/AsteroidFieldGeneratorJob.cs per spec.md § 5 Procedural Asteroid Field
- [X] T068 [US4] Implement AsteroidFieldSystem (ISystem) — orchestrates AsteroidFieldGeneratorJob using AsteroidFieldConfig.MvpDefault (Seed=42, MaxAsteroids=300, FieldRadius=2000f — see data-model.md § Asteroid Field Generation), creates ECS entities with AsteroidComponent + AsteroidOreComponent, sets up Entities Graphics rendering (RenderMeshArray + MaterialMeshInfo) with LOD — in Assets/Features/Procedural/Systems/AsteroidFieldSystem.cs
- [X] T069 [P] [US4] Create placeholder asteroid mesh (3-4 LOD levels: LOD0 ~1000-2000 tris, LOD1 ~500, LOD2 ~200, LOD3 ~50; irregular rocky shape, 3-5m radius range) and URP material with _Depletion parameter support; configure for DOTS instancing (DOTS_INSTANCING_ON keyword) — in Assets/Features/Procedural/Views/
- [X] T070 [US4] Implement HUD UXML layout (UI Toolkit) — resource count panel, velocity indicator, hull integrity bar, mining target info panel, module hotbar — in Assets/Features/HUD/Views/HUD.uxml and Assets/Features/HUD/Views/HUD.uss
- [X] T071 [US4] Implement HUDView MonoBehaviour — subscribes to StateStore.OnStateChanged via UniTask, updates UI Toolkit elements from InventoryState (resource counts), ShipState (velocity magnitude, ShipState.HullIntegrity), MiningSessionState (target info, beam active) — in Assets/Features/HUD/Views/HUDView.cs
- [X] T072 [US4] Create SubScene with AsteroidFieldSystem seed + config, place in GameScene, verify Entities Graphics renders <500 asteroids with correct ore coloring and LOD switching
- [X] T073 [US4] Wire US4 pipeline: AsteroidFieldSystem generates field on scene load → Entities Graphics renders → HUDView reads StateStore → displays all panels. Verify in Play Mode: field generates, HUD updates within 1 frame, steady-state 60 FPS.

**Checkpoint**: US4 complete — Procedural field generates deterministically, HUD shows all real-time data. All field generator and performance tests pass green.

---

## Phase 7: Integration & Acceptance

**Purpose**: Full end-to-end validation of all user stories working together; MVP acceptance criteria verification

- [X] T074 Write PlayMode integration test: full loop — spawn in field, fly to asteroid (US1), select and approach (US2), mine and collect resources (US3), verify field + HUD (US4). **Also verify**: depleted asteroid (RemainingMass reaches 0) produces NativeAsteroidDepletedAction → MiningActionDispatchSystem dispatches StopMiningAction → MiningStoppedEvent(StopReason.AsteroidDepleted) published; and reducer latency <2ms per dispatch (spec.md §2). — in Assets/Features/Tests/FullLoopIntegrationTest.cs
- [X] T075 Write performance test: 500 ships simulated via Burst — assert <2ms total frame time — in Assets/Features/Ship/Tests/ShipPhysicsPerfTests.cs
- [X] T076 Run Unity Profiler deep profile: verify zero GC allocations in steady-state gameplay hot loops (MVP-11); fix any allocations found
- [X] T077 Verify 60 FPS minimum on mid-range hardware with <500 asteroids, active mining, full HUD (MVP-08); profile and optimize if needed
- [X] T078 Verify 100% unit test coverage on all pure reducers (CameraReducer, ShipStateReducer, MiningReducer, InventoryReducer) via Unity Test Framework code coverage (MVP-10)
- [X] T079 Validate all MVP acceptance criteria MVP-01 through MVP-12 per spec.md § 11 — document pass/fail in a results table. Additionally validate Constitution VP-CoreLoop: each core loop stage (select target → approach → activate mining → first yield visible → inventory update visible) delivers tactile feedback within 2 seconds of player action.
- [X] T079b Validate MVP-12 (all state changes via immutable reducers) specifically: (a) verify T008b Roslyn analyzer output shows zero immutability violations; (b) run project-wide grep for direct field assignment on record/State types outside of `with` expressions; (c) verify no MonoBehaviour holds game state (search for mutable fields on View classes); (d) document any CONSTITUTION DEVIATION comments found and confirm each has matching justification in plan.md § Known Deviations.

**Checkpoint**: All MVP acceptance criteria pass. Core loop playable end-to-end at 60 FPS.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Scene polish, cleanup, documentation

- [X] T080 [P] Configure space skybox material (procedural starfield or dark cubemap) and URP post-processing (bloom for beam/star glow, color grading for cool space tones) in Assets/Settings/
- [X] T081 [P] Add placeholder audio sources (engine hum, mining beam, UI clicks) — no sound design in MVP, just AudioSource stubs with placeholder clips
- [X] T082 Remove TutorialInfo folder (Assets/TutorialInfo/) — default Unity template content not needed
- [ ] T083 Run quickstart.md validation — verify a fresh clone can follow all setup steps and reach a working state
- [X] T084 Update CLAUDE.md and constitution.md to resolve TODO(C#_VERSION): document that C# 9.0 is confirmed, `record struct` unavailable, use `readonly struct` for value types
- [X] T085 Add XML doc comments to all public APIs in Core/ and Features/ per Constitution § V Modularity: IStateStore, IEventBus, all reducer Reduce methods, all action/state record types. Each doc comment MUST reference the originating acceptance criterion (e.g., "/// See MVP-01: 6DOF Newtonian flight").
- [X] T086 [P] Configure Unity Addressables groups per Constitution § Unity-Specific Architecture: create Addressables groups for OreTypeDefinition assets, ShipArchetypeConfig assets, asteroid meshes, and materials. Mark all runtime-loaded assets as addressable. Verify zero Resources.Load calls via project-wide grep. Update any direct asset references to use Addressables.LoadAssetAsync<T>.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — no dependencies on other stories
- **US2 (Phase 4)**: Depends on Phase 2 + partial US1 (needs InputBridge, ShipStateReducer, scene setup from T036-T040)
- **US3 (Phase 5)**: Depends on Phase 2 — needs asteroids from US4 for full integration but mining reducer/inventory are independently testable
- **US4 (Phase 6)**: Depends on Phase 2 — field generation and HUD are independently testable
- **Integration (Phase 7)**: Depends on ALL user stories (Phases 3-6) being complete
- **Polish (Phase 8)**: Can start after Phase 2; most tasks are independent of user stories

### User Story Dependencies

```
Phase 1 (Setup)
  └──→ Phase 2 (Foundational) ──→ BLOCKS ALL BELOW
          ├──→ Phase 3 (US1: First Flight) ──→ Enables full US2 testing
          ├──→ Phase 4 (US2: Target & Approach) ──→ Can start reducers independently
          ├──→ Phase 5 (US3: Mine & Collect) ──→ Can start reducers independently
          └──→ Phase 6 (US4: Field & HUD) ──→ Can start generation independently
                      └──→ Phase 7 (Integration) ──→ All stories required
                                └──→ Phase 8 (Polish) ──→ Optional ordering
```

### Within Each User Story

1. Tests MUST be written and FAIL before implementation (TDD Red phase)
2. Data types (records, structs) before reducers
3. Reducers before ECS systems
4. ECS systems before MonoBehaviour views
5. Views before scene wiring
6. Wire and verify in Play Mode last

### Parallel Opportunities

**Phase 1** (5 [P]): T004, T005, T006, T008, T008b can all run in parallel (different files)

**Phase 2** (9 [P]): T009-T012 (tests) can all run in parallel; T013-T015 (Option, EventBus, Actions) can run in parallel; T020-T021 (ScriptableObject assets) can run in parallel

**Phase 3 (US1)** (7 [P]): T024-T026 (tests) in parallel; T027-T029 (data types) in parallel; T032 (ECS components) parallel with T030-T031 (reducers)

**Phase 4 (US2)** (2 [P]): T042-T043 (tests) in parallel

**Phase 5 (US3)** (7 [P]): T049-T051 (tests) in parallel; T052-T053 (data types) in parallel; T056-T057 (ECS + Blob) in parallel

**Phase 6 (US4)** (4 [P]): T064-T065 (tests) in parallel; T066+T069 (data + mesh) in parallel

**Phase 8** (3 [P]): T080-T081 (skybox + audio) + T086 (Addressables) in parallel

**Cross-phase**: Once Phase 2 completes, US1 data type tasks and US3/US4 reducer tests can begin in parallel on different files.

---

## Parallel Example: User Story 1

```
# Launch all tests together (TDD Red):
Agent 1: T024 — CameraReducer unit tests
Agent 2: T025 — ShipStateReducer unit tests
Agent 3: T026 — PilotCommand construction tests

# Launch all data types together:
Agent 1: T027 — CameraState + CameraActions
Agent 2: T028 — ShipState + ShipActions
Agent 3: T029 — PilotCommand + ThrustInput

# Launch ECS components parallel with reducers:
Agent 1: T030 — CameraReducer implementation
Agent 2: T031 — ShipStateReducer implementation
Agent 3: T032 — Ship ECS components
```

## Parallel Example: User Story 3

```
# Launch all tests together (TDD Red):
Agent 1: T049 — MiningReducer tests
Agent 2: T050 — CalculateYield tests
Agent 3: T051 — InventoryReducer tests

# Launch data types together:
Agent 1: T052 — MiningState + MiningActions
Agent 2: T053 — InventoryState + InventoryActions

# Launch ECS + BlobAssets together:
Agent 1: T056 — Mining ECS components
Agent 2: T057 — OreTypeBlob structs
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: US1 — First Flight
4. **STOP and VALIDATE**: Ship flies, camera orbits, all reducer tests green
5. This is the minimum playable prototype

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 (First Flight) → Test independently → Playable ship flight (MVP!)
3. Add US2 (Target & Approach) → Test independently → EVE-style targeting
4. Add US3 (Mine & Collect) → Test independently → Core mining loop
5. Add US4 (Field & HUD) → Test independently → Full procedural world + UI
6. Integration (Phase 7) → Full loop validation
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers/agents:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Agent A: US1 (blocking — needed for full US2)
   - Agent B: US3 reducer tests + data types (independent of US1)
   - Agent C: US4 field generator tests + data types (independent of US1)
3. After US1 scene setup:
   - Agent A: US2 (needs InputBridge + scene from US1)
   - Agent B: US3 ECS + views (needs asteroids from US4 for full test)
   - Agent C: US4 ECS + HUD views
4. Integration after all stories complete

---

## Summary

| Metric | Value |
|--------|-------|
| **Total tasks** | 91 (includes lettered sub-tasks: T008b, T014b, T057b, T062b, T079b) |
| **Phase 1 (Setup)** | 9 tasks (added T008b: Roslyn analyzers) |
| **Phase 2 (Foundational)** | 16 tasks (4 test + 12 impl; added T014b: event structs) |
| **Phase 3 (US1: First Flight)** | 18 tasks (3 test + 15 impl) |
| **Phase 4 (US2: Target & Approach)** | 7 tasks (2 test + 5 impl) |
| **Phase 5 (US3: Mine & Collect)** | 17 tasks (3 test + 14 impl; added T057b: BlobAsset baking, T062b: cargo-full warning) |
| **Phase 6 (US4: Field & HUD)** | 10 tasks (2 test + 8 impl) |
| **Phase 7 (Integration)** | 7 tasks (added T079b: MVP-12 validation) |
| **Phase 8 (Polish)** | 7 tasks (added T085: XML docs, T086: Addressables config) |
| **Parallel opportunities** | 39 tasks marked [P] |
| **Test tasks** | 14 total (TDD Red phase) |
| **Suggested MVP scope** | Phase 1 + 2 + 3 (US1 only) = 43 tasks |

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- TDD is MANDATORY: write tests first, ensure they fail, then implement
- Each user story should be independently completable and testable after Phase 2
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Constitution deviations documented in plan.md § Complexity Tracking
- All file paths are relative to repository root (D:\Projects\github.com\AngelIntension\SpaceMiningGame\)
